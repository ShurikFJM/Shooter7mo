using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.VFX;
using UnityEngine.VFX.Utility;
using UnityEngine.Animations;
using UnityEngine.Playables;
using UnityEngine.Events;
using INab.CommonVFX;

#if UNITY_EDITOR
using Unity.EditorCoroutines.Editor;
using UnityEditor;
#endif

namespace INab.Common
{
    /// <summary>
    /// Serializable settings for a weapon trail related to a specific animation clip.
    /// Configures times, fade durations, and trail lifetime.
    /// </summary>
    [Serializable]
    public class TrailPresetSettings
    {
        /// <summary>
        /// Animation clip associated with this trail preset.
        /// </summary>
        public AnimationClip clipAsset;

        /// <summary>
        /// Whether the trail effect is enabled for this clip.
        /// </summary>
        [Tooltip("Enable or disable the trail effect for this animation clip.")]
        public bool enableTrail = false;

        /// <summary>
        /// Start time in seconds within the animation clip when trail starts.
        /// </summary>
        [Tooltip("Start time of the trail within the animation clip.")]
        public float startTime = 0f;

        /// <summary>
        /// End time in seconds when the trail stops.
        /// </summary>
        [Tooltip("End time of the trail within the animation clip.")]
        public float endTime = 100f;

        /// <summary>
        /// Fade-in duration slider normalized to 0–1 multiplier of trail length.
        /// </summary>
        [Tooltip("Fade-in duration as a proportion of the trail length (0 to 1).")]
        [Range(0, 1)]
        public float fadeInDuration_slider = 0.2f;

        /// <summary>
        /// Computed fade-in duration in seconds.
        /// </summary>
        public float fadeInDuration => fadeInDuration_slider * (endTime - startTime);

        /// <summary>
        /// Duration in seconds to fade out the trail after the end time.
        /// </summary>
        [Tooltip("Fade-out duration in seconds after trail ends.")]
        public float fadeOutDuration = 0.05f;

        /// <summary>
        /// Parameter controlling the trail length / lifetime in the VFX graph.
        /// </summary>
        [Tooltip("Trail lifetime length parameter for the VFX graph.")]
        public float trailLengthLifetime = 0.35f;

        /// <summary>
        /// Constructor assigning animation clip.
        /// </summary>
        public TrailPresetSettings(AnimationClip clip)
        {
            clipAsset = clip;
        }
    }

    /// <summary>
    /// Main class controlling weapon trail visual effects.
    /// </summary>
    [ExecuteInEditMode]
    public class WeaponTrailEffect : MonoBehaviour
    {
        #region Enums and Constants

        /// <summary>
        /// Default folder path to search for trail trailPrefab assets.
        /// </summary>
        public static string DefaultPrefabPath = "Assets/INab Studio/Vfx Assets/Weapon FX Series/Weapon Trails FX/Trail Prefabs/";

        /// <summary>
        /// List of valid visual effect asset names accepted for trail prefabs.
        /// </summary>
        private List<string> visualEffectAssetNames = new List<string> { "Fast Weapon Trail Template", "Smooth Weapon Trail Template" }; 

        /// <summary>
        /// States for trail effect, whether it is on or off.
        /// </summary>
        public enum EffectState { On, Off }

        /// <summary>
        /// Modes controlling how the trail is used: manual triggers or animator-driven.
        /// </summary>
        public enum TrailUsageType { Manual, Animator }

        /// <summary>
        /// Animation playback modes for the preview system.
        /// </summary>
        public enum AnimationPlaybackMode { FullClip, TrailSegment, FullClipLoop }

        #endregion

        #region Setup Properties

        /// <summary>
        /// Prefab game object containing the weapon trail visual effect.
        /// </summary>
        [Tooltip("Trail prefab game object.")]
        public GameObject trailPrefab;

        /// <summary>
        /// Returns the name of the assigned trail prefab or "None".
        /// </summary>
        public string PrefabName => trailPrefab ? trailPrefab.name : "None";

#if UNITY_EDITOR
        /// <summary>
        /// Returns the asset path of the assigned prefab in the Unity project. Editor only.
        /// </summary>
        public string PrefabAssetPath => trailPrefab ? AssetDatabase.GetAssetPath(trailPrefab) : "None";
#else
        public string PrefabAssetPath => "None";
#endif

        /// <summary>
        /// Instantiated prefab instance currently active.
        /// </summary>
        [Tooltip("Instantiated trail prefab game object.")]
        public GameObject instantiatedTrailPrefab;

        /// <summary>
        /// Enable debug logging for development and troubleshooting.
        /// </summary>
        [Tooltip("Enable debug mode for detailed logs.")]
        public bool debugMode = false;

        /// <summary>
        /// User-defined trail name for display and identification.
        /// </summary>
        [Tooltip("User defined name for the trail.")]
        public string trailName = "Trail 1";

        /// <summary>
        /// Toggle to enable or disable drawing of gizmos in the editor.
        /// </summary>
        [Tooltip("Draw gizmos to visualize trail tip and bottom.")]
        public bool enableGizmos = true;

        /// <summary>
        /// Enable root motion during animation preview.
        /// </summary>
        [Tooltip("Apply root motion in animation preview.")]
        public bool rootMotion = false;

        /// <summary>
        /// How the trail system is controlled: manual API or animator event binding.
        /// </summary>
        [Tooltip("Select manual or animator-driven trail usage.")]
        public TrailUsageType trailUsageType = TrailUsageType.Animator;

        /// <summary>
        /// Transform representing the trail tip (start).
        /// </summary>
        [Tooltip("Transform marking the trail tip.")]
        public Transform lineTipTransform;

        /// <summary>
        /// Transform representing the trail bottom (end).
        /// </summary>
        [Tooltip("Transform marking the trail bottom.")]
        public Transform lineBottomTransform;

        /// <summary>
        /// Mount transform for the weapon, parent of trail tip and bottom.
        /// </summary>
        [Tooltip("Mount transform for weapon, parent of trail transforms.")]
        public Transform weaponMountTransform;

        /// <summary>
        /// Enable Unity events to signal trail start and stop.
        /// </summary>
        [Tooltip("Enable UnityEvents on trail start and stop.")]
        public bool useEvents = false;

        /// <summary>
        /// Event triggered when the trail starts.
        /// </summary>
        public UnityEvent onTrailStartEvent;

        /// <summary>
        /// Event triggered when the trail ends.
        /// </summary>
        public UnityEvent onTrailEndEvent;

        /// <summary>
        /// Reference to VisualEffect component controlling the trail VFX.
        /// </summary>
        public VisualEffect vfxComponent;

        /// <summary>
        /// Reference to the VFXPropertyBinder managing bindings for VFX parameters.
        /// </summary>
        public VFXPropertyBinder vfxBinder;

        /// <summary>
        /// Current effect state whether On or Off.
        /// </summary>
        public EffectState currentEffectState = EffectState.Off;

#if UNITY_EDITOR
        private EditorCoroutine effectCoroutineEditor;
#endif
        private Coroutine effectCoroutineRuntime;

        #endregion

        #region Clip Preset Management

        /// <summary>
        /// Associate an AnimationClip with TrailPresetSettings.
        /// </summary>
        [Serializable]
        public class ClipPreset
        {
            /// <summary>The clip this preset configures.</summary>
            public AnimationClip clip;

            /// <summary>The trail preset settings for this clip.</summary>
            public TrailPresetSettings preset;
        }

        /// <summary>List of clip presets for various clips.</summary>
        public List<ClipPreset> clipPresets = new List<ClipPreset>();

        /// <summary>List of all animation clips on this object.</summary>
        public List<AnimationClip> animationClipList = new List<AnimationClip>();

        public TrailPresetSettings SelectedTrailPreset
            => GetOrCreatePresetForClip(SelectedClip);

        /// <summary>Index of the currently selected animation clip.</summary>
        public int selectedClipIndex = 0;

        /// <summary>Count of animation clips.</summary>
        public int animationClipsCount => animationClipList.Count;

        /// <summary>The currently selected animation clip or null.</summary>
        public AnimationClip SelectedClip =>
            (selectedClipIndex >= 0 && selectedClipIndex < animationClipList.Count) ? animationClipList[selectedClipIndex] : null;

        /// <summary>Array of clip names for UI display.</summary>
        public string[] animationClipsNames;

        /// <summary>Find preset index for a given clip, or -1 if none.</summary>
        private int FindEntryIndex(AnimationClip clip)
        {
            if (clip == null) return -1;
            for (int i = 0; i < clipPresets.Count; i++)
                if (clipPresets[i].clip == clip) return i;
            return -1;
        }

        /// <summary>
        /// Get existing or create new preset for a given animation clip.
        /// </summary>
        public TrailPresetSettings GetOrCreatePresetForClip(AnimationClip clip)
        {
            if (clip == null) return null;

            int idx = FindEntryIndex(clip);
            if (idx >= 0) return clipPresets[idx].preset;

            var preset = new TrailPresetSettings(clip)
            {
                startTime = 0f,
                endTime = clip.length,
                fadeInDuration_slider = 0.2f,
                fadeOutDuration = 0.05f,
                trailLengthLifetime = 0.35f,
            };

            clipPresets.Add(new ClipPreset { clip = clip, preset = preset });
            return preset;
        }

        /// <summary>
        /// Ensure all clips in the animationClipList have presets assigned.
        /// </summary>
        public void EnsurePresetsForAllClips()
        {
            foreach (var clip in animationClipList)
                GetOrCreatePresetForClip(clip);
        }

#if UNITY_EDITOR
        /// <summary>
        /// Refresh the animation clip list and assign presets (Editor only).
        /// </summary>
        public void _RefreshAnimationClipList()
        {
            animationClipList = AnimationUtility.GetAnimationClips(gameObject)
                .Where(c => c != null)
                .Distinct()
                .ToList();

            animationClipsNames = animationClipList.Select(c => c.name).ToArray();

            EnsurePresetsForAllClips();

            if (!animationClipList.Contains(SelectedClip))
                selectedClipIndex = Mathf.Clamp(selectedClipIndex, 0, animationClipList.Count - 1);
        }
#endif

        #endregion

        #region Animation Playback State

        /// <summary>Playback speed for animation preview.</summary>
        [Range(0f, 1f)]
        public float playbackSpeed = 1f;

        /// <summary>Selected animation playback mode.</summary>
        public AnimationPlaybackMode animationPlaybackMode = AnimationPlaybackMode.TrailSegment;

        /// <summary>Loop playback toggle.</summary>
        public bool isPlayingLoop = true;

        /// <summary>Range offset time for trail segment playback.</summary>
        [Range(0f, 1f)]
        public float rangeOffset = 0.2f;

        /// <summary>Enable pause during preview playback.</summary>
        public bool pausePreviewEnabled = true;

        private bool useStop = true;

        /// <summary>Pause factor to modulate playback duration before pause.</summary>
        [Range(0f, 1f)]
        public float pausePreviewFactor = 0.8f;

        /// <summary>Automatically start preview playback when trail loads.</summary>
        public bool autoPreviewEnabled = true;

        private bool _trailEventsAdded = false;

        /// <summary>Playback time for the trail segment.</summary>
        public float trailSegmentAnimationPlaybackTime = 0f;

        /// <summary>Playback time for the full animation clip.</summary>
        public float fullClipAnimationPlaybackTime = 0f;

        private bool isPlayingFullClip = false;

        /// <summary>Flag indicating trail segment playback is active.</summary>
        public bool isPlayingTrailSegment = false;

        private float elapsedPlaybackTime = 0f;

        /// <summary>Total playback duration for current preview.</summary>
        public float playbackDuration = 0f;

        /// <summary>Playback duration until pause is triggered.</summary>
        public float playbackDuration_PreviewStop = 0f;

        /// <summary>Flag indicating playback is currently paused.</summary>
        public bool hasPausedPreview = false;

        /// <summary>Flag to track if trail start event has fired.</summary>
        private bool hasStartedTrail = false;

        /// <summary>Flag to track if trail stop event has fired.</summary>
        private bool hasEndedTrail = false;

        #endregion

        #region Editor Only Properties

        // --------------------------------------------------------
        // Playable Graph
        // --------------------------------------------------------

        private PlayableGraph playableGraph;
        private AnimationClipPlayable clipPlayable;
        private AnimationClip currentlyPlayingClip;
        private Animator currentlyUsedAnimator;

        private bool isPlayableGraphInitialized = false;

        // --------------------------------------------------
        // Inspector Foldouts 
        // --------------------------------------------------

        [HideInInspector] public bool _Foldout_1 = true;
        [HideInInspector] public bool _Foldout_2 = true;
        [HideInInspector] public bool _Foldout_3 = true;
        [HideInInspector] public bool _Foldout_4 = true;
        [HideInInspector] public bool _Foldout_5 = true;

        // --------------------------------------------------
        // Testing Helpers
        // --------------------------------------------------

        public float fadeInDuration_ManualTesting = 0.05f;
        public float fadeOutDuration_ManualTesting = 0.05f;

        public float trailLengthLifetime_ManualTesting = 0.35f;
        #endregion

        // ---------------------------------------------------
        // Methods
        // ---------------------------------------------------

        #region Animation Events

        public void EventSetTrailLength(TrailEventData data)
        {
            data.target.SetProperty_Length(data.value);

            //Debug.Log("Set Length " + data.value);
        }

        public void EventStartTrail(TrailEventData data)
        {
            data.target.StartTrail(data.value);

            //Debug.Log("Start " + data.value);
        }

        public void EventStopTrail(TrailEventData data)
        {
            data.target.StopTrail(data.value);

            //Debug.Log("Stop " + data.value);
        }

        [System.Serializable]
        public class TrailEventData : ScriptableObject
        {
            public float value;
            public WeaponTrailEffect target;
        }

        public void AddTrailEventsAtStart()
        {
            if (Application.isPlaying == false) return;

            // TODO does it work? currently we have no option to delete presets when animations were deleted from animator
            clipPresets.RemoveAll(item => item.clip == null);

            foreach (var item in clipPresets)
            {
                var clip = item.clip;
                if (clip == null)
                {
                    continue;
                }

                var preset = item.preset;

                if (preset.enableTrail == false) continue;

                var updateLengthEventData = ScriptableObject.CreateInstance<TrailEventData>();
                updateLengthEventData.value = preset.trailLengthLifetime;
                updateLengthEventData.target = this;

                var updateLengthEvent = new AnimationEvent
                {
                    time = preset.startTime,
                    //functionName = nameof(SetTrailLength),
                    //floatParameter = preset.trailLengthLifetime,
                    messageOptions = SendMessageOptions.DontRequireReceiver,
                    objectReferenceParameter = updateLengthEventData,
                    functionName = nameof(EventSetTrailLength),
                };

                var startEventData = ScriptableObject.CreateInstance<TrailEventData>();
                startEventData.value = preset.fadeInDuration;
                startEventData.target = this;

                var startEvent = new AnimationEvent
                {
                    time = preset.startTime,
                    //functionName = nameof(StartTrail),
                    //floatParameter = preset.fadeInDuration,
                    messageOptions = SendMessageOptions.DontRequireReceiver,
                    objectReferenceParameter = startEventData,
                    functionName = nameof(EventStartTrail),
                };

                var stopEventData = ScriptableObject.CreateInstance<TrailEventData>();
                stopEventData.value = preset.fadeOutDuration;
                stopEventData.target = this;

                var endEvent = new AnimationEvent
                {
                    time = preset.endTime,
                    //functionName = nameof(StopTrail),
                    //floatParameter = preset.fadeOutDuration,
                    messageOptions = SendMessageOptions.DontRequireReceiver,
                    objectReferenceParameter = stopEventData,
                    functionName = nameof(EventStopTrail),
                };

                var filteredEvents = clip.events.Where(e => e.functionName != nameof(SetTrailLength) && e.functionName != nameof(StartTrail) && e.functionName != nameof(StopTrail)).ToArray();
                clip.events = filteredEvents;

                //if (!HasEvent(clip, nameof(SetTrailLength)))
                clip.AddEvent(updateLengthEvent);

                //if (!HasEvent(clip, nameof(StartTrail)))
                clip.AddEvent(startEvent);

                //if (!HasEvent(clip, nameof(StopTrail)))
                clip.AddEvent(endEvent);
            }
        }

        //bool HasEvent(AnimationClip clip, string functionName)
        //{
        //    foreach (var e in clip.events)
        //    {
        //        if (e.functionName == functionName)
        //            return true;
        //    }
        //    return false;
        //}

        #endregion

        #region Internal event invocations

        private void InvokeStartTrailEvent()
        {
            if (SelectedTrailPreset.enableTrail == false) return;

            SetProperty_Length(SelectedTrailPreset.trailLengthLifetime);
            StartTrail(SelectedTrailPreset.fadeInDuration);
        }

        private void InvokeStopTrailEvent()
        {
            if (SelectedTrailPreset.enableTrail == false) return;

            StopTrail(SelectedTrailPreset.fadeOutDuration);
        }

        #endregion

        #region Preview playback loop 

        public void AutoPreviewStart()
        {
            if (autoPreviewEnabled == false) return;

            switch (animationPlaybackMode)
            {
                case AnimationPlaybackMode.FullClip:
                    PlayFullClipPreview();
                    break;
                case AnimationPlaybackMode.TrailSegment:
                    PlayTrailSegmentPreview(true);
                    break;
            }
        }

        private void UpdatePreviewPlayback()
        {
            if (SelectedClip == null)
                return;

            float deltaTime = Time.deltaTime * playbackSpeed;

            if (isPlayingFullClip)
            {
                elapsedPlaybackTime += deltaTime;
                fullClipAnimationPlaybackTime = Mathf.Clamp(fullClipAnimationPlaybackTime + deltaTime, 0f, SelectedClip.length);
                trailSegmentAnimationPlaybackTime = Mathf.Clamp(fullClipAnimationPlaybackTime, SelectedTrailPreset.startTime, SelectedTrailPreset.endTime);

                _PreviewPoseAtTime(fullClipAnimationPlaybackTime);
                if (fullClipAnimationPlaybackTime > SelectedTrailPreset.startTime && !hasStartedTrail)
                {
                    hasStartedTrail = true;
                    InvokeStartTrailEvent();

                }

                if (fullClipAnimationPlaybackTime > SelectedTrailPreset.endTime && !hasEndedTrail)
                {
                    hasEndedTrail = true;

                    InvokeStopTrailEvent();
                }

                if (elapsedPlaybackTime >= playbackDuration)
                {
                    isPlayingFullClip = false;
                }
            }

            if (isPlayingTrailSegment)
            {
                elapsedPlaybackTime += deltaTime;
                trailSegmentAnimationPlaybackTime = Mathf.Clamp(trailSegmentAnimationPlaybackTime + deltaTime, SelectedTrailPreset.startTime - rangeOffset, SelectedTrailPreset.endTime + rangeOffset);
                fullClipAnimationPlaybackTime = trailSegmentAnimationPlaybackTime;

                _PreviewPoseAtTime(trailSegmentAnimationPlaybackTime);
                if (fullClipAnimationPlaybackTime > SelectedTrailPreset.startTime && !hasStartedTrail)
                {
                    hasStartedTrail = true;
                    InvokeStartTrailEvent();
                }

                if (fullClipAnimationPlaybackTime > SelectedTrailPreset.endTime && !hasEndedTrail)
                {
                    hasEndedTrail = true;

                    InvokeStopTrailEvent();
                }

                if (pausePreviewEnabled && useStop)
                {
                    if (elapsedPlaybackTime >= playbackDuration_PreviewStop)
                    {
                        isPlayingTrailSegment = false;
                        hasPausedPreview = true;

                        //vfxComponent.playRate = 0f;
                        vfxComponent.pause = true;
                        //SetProperty_EffectActive(false);

                        useStop = false;
                    }
                }

                if (elapsedPlaybackTime >= playbackDuration)
                {
                    isPlayingTrailSegment = false;
                    StopTrail(SelectedTrailPreset.fadeOutDuration);

                    useStop = false;
                }
            }

            if (isPlayingLoop && animationPlaybackMode == AnimationPlaybackMode.FullClipLoop)
            {
                if (isPlayingFullClip == false) PlayFullClipPreview();
            }
        }

        public void PlayTrailSegmentPreview(bool useStop = false)
        {
            if (vfxComponent == null || SelectedTrailPreset == null) return;

            hasStartedTrail = false;
            hasEndedTrail = false;

            // Reset VFX state
            vfxComponent.Reinit();
            SetProperty_EffectActive(false);
            SetProperty_EffectAlive(0);
            SendStopEvent();
            currentEffectState = EffectState.Off;

            // Playback setup
            playbackDuration = (SelectedTrailPreset.endTime - SelectedTrailPreset.startTime + 2 * rangeOffset);

            this.useStop = useStop;
            if (pausePreviewEnabled && useStop)
            {
                //playbackDuration = (SelectedTrailPreset.endTime - SelectedTrailPreset.startTime) * pausePreviewFactor + rangeOffset;
                playbackDuration_PreviewStop = (SelectedTrailPreset.endTime - SelectedTrailPreset.startTime) * pausePreviewFactor + rangeOffset;
            }

            elapsedPlaybackTime = 0f;
            isPlayingTrailSegment = true;
            isPlayingFullClip = false;

            trailSegmentAnimationPlaybackTime = SelectedTrailPreset.startTime - rangeOffset;

            _PreviewPoseAtTime(trailSegmentAnimationPlaybackTime);
            vfxComponent.playRate = playbackSpeed;
        }

        public void PlayFullClipPreview()
        {
            if (vfxComponent == null) return;

            hasStartedTrail = false;
            hasEndedTrail = false;

            // Reset VFX state
            vfxComponent.Reinit();
            SetProperty_EffectActive(false);
            SetProperty_EffectAlive(0);
            SendStopEvent();
            currentEffectState = EffectState.Off;

            // Playback setup
            var clip = SelectedClip;
            if (clip == null) return;

            playbackDuration = clip.length;
            elapsedPlaybackTime = 0f;

            isPlayingFullClip = true;
            isPlayingTrailSegment = false;

            fullClipAnimationPlaybackTime = 0f;

            _PreviewPoseAtTime(fullClipAnimationPlaybackTime);
            vfxComponent.playRate = playbackSpeed;
        }

        #endregion

        #region Playables animation preview 

        public void EvaluatePreviewPose(Animator targetAnimator, AnimationClip clip, float time)
        {
            if (targetAnimator == null || clip == null)
            {
                return;
            }

            if (!playableGraph.IsValid())
            {
                //if (DEBUG) Debug.Log("is valid == false");
            }

            if (!isPlayableGraphInitialized || currentlyUsedAnimator != targetAnimator || currentlyPlayingClip != clip || !playableGraph.IsValid())
            {
                //if (DEBUG) Debug.Log("initialize playables");

                //DisposePreviewGraph();

                currentlyUsedAnimator = targetAnimator;
                currentlyPlayingClip = clip;

                playableGraph = PlayableGraph.Create();
                playableGraph.SetTimeUpdateMode(DirectorUpdateMode.Manual);

                var output = AnimationPlayableOutput.Create(playableGraph, "Animation", currentlyUsedAnimator);
                clipPlayable = AnimationClipPlayable.Create(playableGraph, currentlyPlayingClip);
                //clipPlayable.SetApplyFootIK(false);
                //clipPlayable.SetApplyPlayableIK(false);

                output.SetSourcePlayable(clipPlayable);
                playableGraph.Play();
                isPlayableGraphInitialized = true;
            }

            clipPlayable.SetTime(Mathf.Clamp(time, 0f, currentlyPlayingClip.length));
            playableGraph.Evaluate();
        }

        private void DisposePreviewGraph()
        {
            if (!isPlayableGraphInitialized) return;

            if (playableGraph.IsValid()) playableGraph.Destroy();
            currentlyUsedAnimator = null;
            currentlyPlayingClip = null;
            isPlayableGraphInitialized = false;
        }

        public void _PreviewPoseAtTime(float time)
        {
            Animator currentAnimator = GetComponent<Animator>();
            bool wasAnimatorDisabled = false;
            if (currentAnimator.enabled == false)
            {
                wasAnimatorDisabled = true;
                currentAnimator.enabled = true;
            }

            if (currentAnimator != null)
            {
                var myClip = SelectedClip;

                bool previusRootMotion = currentAnimator.applyRootMotion;
                currentAnimator.applyRootMotion = rootMotion;
                EvaluatePreviewPose(currentAnimator, myClip, time);
                currentAnimator.applyRootMotion = previusRootMotion;

                if (wasAnimatorDisabled)
                {
                    currentAnimator.enabled = false;
                }

            }

            var clip = SelectedClip;
            Animation animation = GetComponent<Animation>();
            if (animation != null)
            {
                if (animation.GetClip(clip.name) == null)
                    animation.AddClip(clip, clip.name);

                animation.Play(clip.name);
                animation[clip.name].time = Mathf.Clamp(time, 0f, clip.length);
                animation.Sample();
                animation.Stop();
            }
        }

        #endregion

        #region Editor Utilities: Prefab Management + Line Transforms

#if UNITY_EDITOR

        public void DrawHandles()
        {
            if (!enableGizmos) return;

            if (lineTipTransform == null || lineBottomTransform == null) return;

            Transform bottom = lineBottomTransform;
            Transform tip = lineTipTransform;

            Vector3 start = bottom.position;
            Vector3 end = tip.position;
            Vector3 direction = (end - start).normalized;
            float length = Vector3.Distance(start, end);
            float arrowHeadLength = 0.035f;

            Handles.color = new Color(0.15f, 0.75f, 0.65f, 1);

            Handles.DrawLine(start, end, 6);


            // Desired spacing between arrows
            float targetSpacing = 0.25f;

            // Calculate how many arrows to place
            int arrowCount = Mathf.Max(1, Mathf.FloorToInt(length / targetSpacing));

            // Always leave arrows equally distributed along full line
            for (int i = 0; i <= arrowCount; i++)
            {
                float t = (float)i / arrowCount; // normalized [0..1]
                Vector3 pos = Vector3.Lerp(start, end, t);


                Handles.ConeHandleCap(
                    0,
                    pos,
                    Quaternion.LookRotation(direction),
                    arrowHeadLength,
                    EventType.Repaint
                );
            }



            //GUIStyle iconStyle = new GUIStyle();
            //iconStyle.normal.background = null;
            //iconStyle.alignment = TextAnchor.MiddleCenter;

            //Handles.Label(bottom.position, EditorGUIUtility.IconContent("DotSelection"), iconStyle);
            //Handles.Label(tip.position, EditorGUIUtility.IconContent("DotSelection"), iconStyle);

        }
#endif

        public bool _CheckSelectedPrefab()
        {
            if (trailPrefab == null) return true;
            var vfx = trailPrefab.GetComponent<VisualEffect>();
            if (vfx == null) return false;


            if (vfx.visualEffectAsset != null &&
                visualEffectAssetNames.Contains(vfx.visualEffectAsset.name))
            {
                return true;
            }

            return false;
        }

        public bool _SaveAsNewPrefab()
        {
#if UNITY_EDITOR
            // Ask user where to save
            string path = EditorUtility.SaveFilePanelInProject(
                "Save Prefab As",            // Window title
                "Weapon Trail",              // Default file name
                "prefab",                    // Extension
                "Choose where to save the prefab",
                DefaultPrefabPath
            );

            if (string.IsNullOrEmpty(path))
                return false;

            PrefabUtility.UnpackPrefabInstance(instantiatedTrailPrefab, PrefabUnpackMode.Completely, InteractionMode.UserAction);
            trailPrefab = PrefabUtility.SaveAsPrefabAsset(instantiatedTrailPrefab, path, out bool success);

            _InstantiateTrailPrefab();

            return success;
#else   
            return false;
#endif
        }

        public void _ApplyPrefabChanges()
        {
#if UNITY_EDITOR
            PrefabUtility.ApplyPrefabInstance(instantiatedTrailPrefab, InteractionMode.UserAction);
#endif
            // AutoPreviewStart();
        }

        public bool _LoadPrefab()
        {
#if UNITY_EDITOR
            string path = EditorUtility.OpenFilePanelWithFilters(
                "Select Prefab",
                DefaultPrefabPath,
                new string[] { "Prefab files", "prefab" }
            );
            if (string.IsNullOrEmpty(path))
                return false;

            string relativePath = "Assets" + path.Substring(Application.dataPath.Length);
            var loaded = AssetDatabase.LoadAssetAtPath<GameObject>(relativePath);
            if (loaded == null) return false;

            int group = Undo.GetCurrentGroup();
            Undo.SetCurrentGroupName("Load and Instantiate Trail Prefab");

            Undo.RecordObject(this, "Load Trail Prefab Asset");
            trailPrefab = loaded;
            EditorUtility.SetDirty(this);

            _InstantiateTrailPrefab();

            Undo.CollapseUndoOperations(group);

            return true;
#else
    return false;
#endif
        }


        public bool _InstantiateTrailPrefab()
        {
#if UNITY_EDITOR
            Undo.IncrementCurrentGroup();
            int group = Undo.GetCurrentGroup();
            Undo.SetCurrentGroupName("Instantiate Trail Prefab");

            if (instantiatedTrailPrefab != null)
            {
                var toDestroy = instantiatedTrailPrefab;
                Undo.DestroyObjectImmediate(toDestroy);
                Undo.RecordObject(this, "Clear Trail Instance Ref");
                instantiatedTrailPrefab = null;
                EditorUtility.SetDirty(this);
            }

            if (trailPrefab != null)
            {
                var instance = PrefabUtility.InstantiatePrefab(trailPrefab, transform) as GameObject;
                if (instance != null)
                {
                    Undo.RegisterCreatedObjectUndo(instance, "Create Trail Instance");

                    var t = instance.transform;
                    Undo.RecordObject(t, "Setup Trail Transform");
                    t.localPosition = Vector3.zero;
                    t.localRotation = Quaternion.identity;
                    t.localScale = Vector3.one;
                    instance.name = trailPrefab.name + " (Instance) [" + trailName + "]";

                    Undo.RecordObject(this, "Assign Trail Instance & VFX refs");
                    instantiatedTrailPrefab = instance;
                    vfxComponent = instance.GetComponent<VisualEffect>();
                    vfxBinder = instance.GetComponent<VFXPropertyBinder>();
                    EditorUtility.SetDirty(this);

                    ConfigureVFXBinders();
                    if (vfxComponent) EditorUtility.SetDirty(vfxComponent);
                    if (vfxBinder) EditorUtility.SetDirty(vfxBinder);
                }
            }

            Undo.CollapseUndoOperations(group);
            AutoPreviewStart();

#else

            if (instantiatedTrailPrefab != null)
            {
                Destroy(instantiatedTrailPrefab);
                instantiatedTrailPrefab = null;
            }

            if (trailPrefab != null)
            {
                var instance = Instantiate(trailPrefab, transform);
                if (instance != null)
                {
                    var t = instance.transform;
                    t.localPosition = Vector3.zero;
                    t.localRotation = Quaternion.identity;
                    t.localScale = Vector3.one;
                    instance.name = trailPrefab.name + " (Instance) [" + trailName + "]";

                    instantiatedTrailPrefab = instance;
                    vfxComponent = instance.GetComponent<VisualEffect>();
                    vfxBinder = instance.GetComponent<VFXPropertyBinder>();

                    ConfigureVFXBinders();
                }
            }


#endif

            return true;
        }


        public void _CreateDefaultLineTransforms()
        {
            GameObject tip = new GameObject("Line Tip");
            var trailTransTip = tip.AddComponent<TrailTransform>();
            trailTransTip.weaponTrailEffect = this;
            tip.transform.SetParent(weaponMountTransform);
            tip.transform.localPosition = new Vector3(0, 0.7f, 0);
            tip.transform.localRotation = Quaternion.identity;
            tip.transform.localScale = Vector3.one;
            lineTipTransform = tip.transform;

            GameObject bottom = new GameObject("Line Bottom");
            var trailTransBottom = bottom.AddComponent<TrailTransform>();
            trailTransBottom.weaponTrailEffect = this;
            bottom.transform.SetParent(weaponMountTransform);
            bottom.transform.localPosition = new Vector3(0, 0, 0);
            bottom.transform.localRotation = Quaternion.identity;
            bottom.transform.localScale = Vector3.one;
            lineBottomTransform = bottom.transform;

            ConfigureVFXBinders();
        }

        #endregion

        #region Visual Effect Graph Utilities

        public void ConfigureVFXBinders()
        {
            if (vfxBinder == null) return;

#if UNITY_EDITOR
            if (!Application.isPlaying)
            {
                Undo.RecordObject(vfxBinder, "Configure VFX Binders");

                var binders = vfxBinder.GetPropertyBinders<VFXLossyTransformBinder>().ToList();
                while (binders.Count < 2)
                {
                    var b = Undo.AddComponent<VFXLossyTransformBinder>(vfxBinder.gameObject);
                    binders.Add(b);
                }

                var tip = binders[0];
                var bot = binders[1];

                Undo.RecordObject(tip, "Bind Line Tip");
                tip.Target = lineTipTransform;
                tip.Property = "Line Tip";

                Undo.RecordObject(bot, "Bind Line Bottom");
                bot.Target = lineBottomTransform;
                bot.Property = "Line Bottom";

                EditorUtility.SetDirty(vfxBinder);
                EditorUtility.SetDirty(tip);
                EditorUtility.SetDirty(bot);
                return;
            }
#endif

            {
                var binders = vfxBinder.GetPropertyBinders<VFXLossyTransformBinder>().ToList();
                while (binders.Count < 2)
                    binders.Add(vfxBinder.AddPropertyBinder<VFXLossyTransformBinder>());

                binders[0].Target = lineTipTransform;
                binders[0].Property = "Line Tip";
                binders[1].Target = lineBottomTransform;
                binders[1].Property = "Line Bottom";
            }
        }

        public void SetProperty_EffectAlive(float value)
        {
            if (vfxComponent == null) return;

            value = Mathf.Clamp01(value);

            if (vfxComponent && vfxComponent.HasFloat("Effect Value"))
                vfxComponent.SetFloat("Effect Value", value);
        }

        public void SetProperty_EffectActive(bool isActive)
        {
            if (vfxComponent == null) return;

            if (vfxComponent && vfxComponent.HasBool("Effect Active"))
                vfxComponent.SetBool("Effect Active", isActive);
        }

        public void SetProperty_Length(float value)
        {
            if (vfxComponent == null) return;

            if (vfxComponent && vfxComponent.HasFloat("Length"))
                vfxComponent.SetFloat("Length", value);
        }

        public void SendPlayEvent()
        {
            if (vfxComponent == null) return;
            vfxComponent.Play();
        }

        public void SendStopEvent()
        {
            if (vfxComponent == null) return;
            vfxComponent.Stop();
        }


        #endregion

        #region IEnumerators & Coroutines

        private void StartEffectCoroutine(IEnumerator enumerator)
        {
#if UNITY_EDITOR
            if (!Application.isPlaying)
            {
                // Stop any existing editor coroutine
                if (effectCoroutineEditor != null)
                    EditorCoroutineUtility.StopCoroutine(effectCoroutineEditor);

                // Start new editor coroutine
                effectCoroutineEditor = EditorCoroutineUtility.StartCoroutine(enumerator, this);
                return;
            }
#endif

            // Runtime path
            if (effectCoroutineRuntime != null)
                StopCoroutine(effectCoroutineRuntime);

            effectCoroutineRuntime = StartCoroutine(enumerator);
        }

        private IEnumerator PlayEffectEnumerator(float fadeInDuration)
        {
            if (vfxComponent != null) vfxComponent.pause = false;


            SendPlayEvent();
            SetProperty_EffectActive(true);
            SetProperty_EffectAlive(1);
            currentEffectState = EffectState.On;

            float elapsedTime = 0f;
            while (elapsedTime < fadeInDuration)
            {
                elapsedTime += Time.deltaTime * playbackSpeed;
                float effectAmount = elapsedTime / fadeInDuration;
                SetProperty_EffectAlive(effectAmount);
                yield return null;
            }
        }

        private IEnumerator StopEffectEnumerator(float fadeOutDuration)
        {
            float elapsedTime = 0f;
            while (elapsedTime < fadeOutDuration)
            {
                elapsedTime += Time.deltaTime * playbackSpeed;
                float effectAmount = elapsedTime / fadeOutDuration;
                SetProperty_EffectAlive(1 - effectAmount);
                yield return null;
            }

            SetProperty_EffectActive(false);
            SetProperty_EffectAlive(0);
            SendStopEvent();
            currentEffectState = EffectState.Off;
        }

        #endregion

        #region Unity Lifecycle

        private void OnDisable() => DisposePreviewGraph();
        private void OnDestroy() => DisposePreviewGraph();


        private void OnEnable()
        {
#if UNITY_EDITOR
            if (!Application.isPlaying) _RefreshAnimationClipList();
#endif

            bool isNull = false;

            if (vfxBinder == null) return;
            var list = vfxBinder.GetPropertyBinders<VFXLossyTransformBinder>();
            foreach (var item in list)
            {
                if (item.Target == null) isNull = true;
            }

            if (isNull)
            {
                if (lineTipTransform != null && lineBottomTransform != null) ConfigureVFXBinders();
            }
        }



        private void Start()
        {
            if (!_trailEventsAdded)
            {
                AddTrailEventsAtStart();
                _trailEventsAdded = true;
            }
        }

        private void Update()
        {
            // --------------------------------------------------------------------------------------------------------

            if (!Application.isPlaying && trailUsageType == TrailUsageType.Animator) UpdatePreviewPlayback();

            // --------------------------------------------------------------------------------------------------------


            if (lineTipTransform != null && lineBottomTransform != null)
            {
                Vector3 direction = (lineTipTransform.position - lineBottomTransform.position).normalized;

                lineTipTransform.rotation = Quaternion.LookRotation(direction);
                lineBottomTransform.rotation = Quaternion.LookRotation(direction);
            }
        }

        /*
#if UNITY_EDITOR
        private void OnDrawGizmos()
        {
            if (!enableGizmos) return;
            if (!lineTipTransform || !lineBottomTransform) return;

            GUIStyle iconStyle = new GUIStyle();
            iconStyle.contentOffset = new Vector2(-9.5f, -9.5f);

            Handles.Label(lineTipTransform.position, EditorGUIUtility.IconContent("d_back@2x"), iconStyle);
            Handles.Label(lineBottomTransform.position, EditorGUIUtility.IconContent("back@2x"), iconStyle); // d_PlayButton@2x

            Handles.
        }
#endif
        */

        #endregion

        #region Public API Methods

        /// <summary>Set length multiplier for the trail lifetime parameter in VFX graph.</summary>
        public void SetLengthMultiplier(float newLengthMultiplier)
        {
            if (vfxComponent != null)
                vfxComponent.SetFloat("Lifetime/Length Multiplier", newLengthMultiplier);
        }

        /// <summary>Change trail prefab to the given GameObject and instantiate it.</summary>
        public void SetNewTrailPrefab(GameObject newTrailPrefab)
        {
            StopTrail(0.001f);

            trailPrefab = newTrailPrefab;
            _InstantiateTrailPrefab();
        }

        /// <summary>Set the trail length lifetime property.</summary>
        public void SetTrailLength(float trailLengthLifetime)
        {
            SetProperty_Length(trailLengthLifetime);
        }

        /// <summary>Start trail effect with given fade-in duration and length.</summary>
        public void StartTrailWithLength(float fadeInDuration, float trailLengthLifetime)
        {
            SetTrailLength(trailLengthLifetime);
            StartTrail(fadeInDuration);
        }

        /// <summary>Start the trail effect, triggering fade-in and play.</summary>
        public void StartTrail(float fadeInDuration)
        {
            if (useEvents && onTrailStartEvent != null)
                onTrailStartEvent.Invoke();

            StartEffectCoroutine(PlayEffectEnumerator(fadeInDuration));
        }

        /// <summary>Stop the trail effect, triggering fade-out and stop.</summary>
        public void StopTrail(float fadeOutDuration)
        {

            if (useEvents && onTrailEndEvent != null)
            {
                onTrailEndEvent.Invoke();
            }

            StartEffectCoroutine(StopEffectEnumerator(fadeOutDuration));
        }

        #endregion
    }
}