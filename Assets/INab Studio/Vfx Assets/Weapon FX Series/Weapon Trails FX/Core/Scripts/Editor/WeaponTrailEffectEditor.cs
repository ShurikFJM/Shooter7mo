using UnityEditor;
using UnityEngine;
using System.Linq;
using static INab.Common.EditorUtilties;

namespace INab.Common
{
    [CustomEditor(typeof(WeaponTrailEffect))]
    [CanEditMultipleObjects]
    public class WeaponTrailEffectEditor : Editor
    {
        #region Properties

        private SerializedProperty fadeInDuration_ManualTesting;
        private SerializedProperty fadeOutDuration_ManualTesting;

        private SerializedProperty trailLengthLifetime_ManualTesting;


        // Prefab & References
        private SerializedProperty debugMode;
        private SerializedProperty trailName;
        private SerializedProperty enableGizmos;
        private SerializedProperty rootMotion;
        private SerializedProperty trailPrefab;
        private SerializedProperty instantiatedTrailPrefab;
        private SerializedProperty trailUsageType;
        private SerializedProperty lineTipTransform;
        private SerializedProperty weaponMountTransform;
        private SerializedProperty lineBottomTransform;
        private SerializedProperty useEvents;
        private SerializedProperty onTrailStartEvent;
        private SerializedProperty onTrailEndEvent;

        // Animator Setup
        private SerializedProperty playbackSpeed;
        private SerializedProperty animationPlaybackMode;
        private SerializedProperty rangeOffset;
        private SerializedProperty isPlayingLoop;
        private SerializedProperty pausePreviewEnabled;
        private SerializedProperty pausePreviewFactor;
        private SerializedProperty autoPreviewEnabled;
        
        // Trail Settings
        private SerializedProperty selectedTrailPresetIndex;
        private SerializedProperty selectedClipIndex;


        private WeaponTrailEffect ourTarget;

        #endregion

        private void OnEnable()
        {
            fadeInDuration_ManualTesting = serializedObject.FindProperty("fadeInDuration_ManualTesting");
            fadeOutDuration_ManualTesting = serializedObject.FindProperty("fadeOutDuration_ManualTesting");
            trailLengthLifetime_ManualTesting = serializedObject.FindProperty("trailLengthLifetime_ManualTesting");

            // Prefab & References
            trailName = serializedObject.FindProperty("trailName");
            debugMode = serializedObject.FindProperty("debugMode");
            enableGizmos = serializedObject.FindProperty("enableGizmos");
            rootMotion = serializedObject.FindProperty("rootMotion");
            trailPrefab = serializedObject.FindProperty("trailPrefab");
            instantiatedTrailPrefab = serializedObject.FindProperty("instantiatedTrailPrefab");
            trailUsageType = serializedObject.FindProperty("trailUsageType");
            lineTipTransform = serializedObject.FindProperty("lineTipTransform");
            useEvents = serializedObject.FindProperty("useEvents");
            onTrailStartEvent = serializedObject.FindProperty("onTrailStartEvent");
            onTrailEndEvent = serializedObject.FindProperty("onTrailEndEvent");
            weaponMountTransform = serializedObject.FindProperty("weaponMountTransform");
            lineBottomTransform = serializedObject.FindProperty("lineBottomTransform");

            // Animator Setup
            playbackSpeed = serializedObject.FindProperty("playbackSpeed");
            animationPlaybackMode = serializedObject.FindProperty("animationPlaybackMode");
            rangeOffset = serializedObject.FindProperty("rangeOffset");
            isPlayingLoop = serializedObject.FindProperty("isPlayingLoop");
            pausePreviewEnabled = serializedObject.FindProperty("pausePreviewEnabled");
            pausePreviewFactor = serializedObject.FindProperty("pausePreviewFactor");
            autoPreviewEnabled = serializedObject.FindProperty("autoPreviewEnabled");

            // Trail Settings
            selectedTrailPresetIndex = serializedObject.FindProperty("selectedTrailPresetIndex");
            selectedClipIndex = serializedObject.FindProperty("selectedClipIndex");

            ourTarget = (WeaponTrailEffect)target;
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            using (var foldout = new FoldoutHeaderScope(ref ourTarget._Foldout_4, "Setup"))
            {
                if (foldout.IsExpanded)
                {
                    Setup();
                }
            }

            using (var foldout = new FoldoutHeaderScope(ref ourTarget._Foldout_1, "Effect Prefab"))
            {
                if (foldout.IsExpanded)
                {
                    EffectsLoading();
                }
            }

            string TestingAnimationLabel = ourTarget.trailUsageType == WeaponTrailEffect.TrailUsageType.Animator ? "Trail Preview" : "API Testing";
            using (var foldout = new FoldoutHeaderScope(ref ourTarget._Foldout_2, TestingAnimationLabel))
            {
                if (foldout.IsExpanded)
                {
                    TestingAnimation();
                }
            }

            if (ourTarget.trailUsageType == WeaponTrailEffect.TrailUsageType.Animator)
            {
                using (var foldout = new FoldoutHeaderScope(ref ourTarget._Foldout_3, "Trail Settings"))
                {
                    if (foldout.IsExpanded)
                    {
                        TrailPresetSettings();
                    }
                }
            }

            //DrawDefaultInspector();

            serializedObject.ApplyModifiedProperties();
        }

        void OnSceneGUI()
        {
            ourTarget.DrawHandles();
        }

        #region Setup and Saving and Prefabs

        private void Setup()
        {
            using (new LabeledSectionScope("General"))
            {
                EditorGUILayout.PropertyField(enableGizmos);
                if(ourTarget.trailUsageType == WeaponTrailEffect.TrailUsageType.Animator) EditorGUILayout.PropertyField(rootMotion);
                EditorGUILayout.PropertyField(debugMode);
                EditorGUILayout.PropertyField(useEvents);
                if (useEvents.boolValue)
                {
                    EditorGUILayout.PropertyField(onTrailStartEvent);
                    EditorGUILayout.PropertyField(onTrailEndEvent);
                }
                EditorGUILayout.PropertyField(trailName);
                EditorGUILayout.PropertyField(trailUsageType);
                if (ourTarget.trailUsageType == WeaponTrailEffect.TrailUsageType.Animator)
                {
                    if (ourTarget.GetComponent<Animator>() == null && ourTarget.GetComponent<Animation>() == null)
                        EditorGUILayout.HelpBox("You have selected Animator usage but there is no Animator or Animation component on this GameObject!", MessageType.Error);
                }
            }

            using (new LabeledSectionScope("Trail Transforms Settings"))
            {
                if (ourTarget.lineTipTransform == null || ourTarget.lineBottomTransform == null)
                {
                    EditorGUILayout.PropertyField(weaponMountTransform);

                    if (ourTarget.weaponMountTransform == null)
                    {
                        EditorGUILayout.HelpBox("Assign weapon mount for the trail.", MessageType.Warning);
                    }
                    else
                    {
                        if (GUILayout.Button("Setup Line Transforms", EditorUtilties.IndentedButtonStyleDouble))
                        {
                            ourTarget._CreateDefaultLineTransforms();
                        }
                        EditorGUILayout.HelpBox("Setup line transforms for the weapon mount.", MessageType.Warning);
                    }
                }
                else
                {
                    EditorGUI.BeginChangeCheck();
                    EditorGUILayout.PropertyField(lineTipTransform);
                    EditorGUILayout.PropertyField(lineBottomTransform);
                    if (EditorGUI.EndChangeCheck())
                    {
                        serializedObject.ApplyModifiedProperties();
                        ourTarget.ConfigureVFXBinders();
                    }
                }
            }
        }

        private void EffectsLoading()
        {
          
            //using (new LabeledSectionScope("Effect Prefabs"))
            {
                if (ourTarget.trailPrefab == null)
                {
                    EditorGUILayout.HelpBox("Choose effect from effect prefabs: " + WeaponTrailEffect.DefaultPrefabPath, MessageType.Info);
                }
                EditorGUILayout.PropertyField(trailPrefab);

                GUI.enabled = false;
                EditorGUILayout.PropertyField(instantiatedTrailPrefab, new GUIContent("Prefab Instance"));
                GUI.enabled = true;

                if (!ourTarget._CheckSelectedPrefab())
                {
                    EditorGUILayout.HelpBox("You have wrong prefab selected!", MessageType.Error);
                    EditorGUILayout.HelpBox("Choose effect from effect prefabs: " + WeaponTrailEffect.DefaultPrefabPath, MessageType.Info);
                }

                float totalWidth = EditorGUIUtility.currentViewWidth - 60;
                float halfWidth = totalWidth / 2f;
                bool loadNew = false;
                bool saveAsNewButton = false;

                bool saveAsNew = true, loadAsNew = true, save = true, load = true;

                if (ourTarget.trailPrefab == null)
                {
                    saveAsNew = true;
                    loadAsNew = true;

                    save = false;
                    load = false;
                }

                if (ourTarget.instantiatedTrailPrefab == null)
                {
                    saveAsNew = false;
                    loadAsNew = true;

                    save = false;
                    load = true;
                }

                if(ourTarget.trailPrefab == null && ourTarget.instantiatedTrailPrefab == null)
                {
                    saveAsNew = false;
                    loadAsNew = true;

                    save = false;
                    load = false;
                }

                using (var scope = new EditorGUILayout.HorizontalScope())
                {
                    GUI.enabled = saveAsNew;
                    saveAsNewButton = GUILayout.Button("Save As New", EditorUtilties.IndentedButtonStyleDouble, GUILayout.Width(halfWidth));
                   
                    GUI.enabled = true;

                    GUI.enabled = save;
                    if (GUILayout.Button("Save (Override Prefab)", EditorUtilties.DefaultButtonStyle, GUILayout.Width(halfWidth)))
                    {
                        Undo.RecordObjects(new Object[] { ourTarget, ourTarget.trailPrefab }, "Saved Prefab");
                        ourTarget._ApplyPrefabChanges();
                        EditorUtility.SetDirty(ourTarget);
                    }
                    GUI.enabled = true;
                }

                using (var scope = new EditorGUILayout.HorizontalScope())
                {
                    GUI.enabled = loadAsNew;
                    loadNew = GUILayout.Button("Load New", EditorUtilties.IndentedButtonStyleDouble, GUILayout.Width(halfWidth));
                    GUI.enabled = true;

                    GUI.enabled = load;
                    if (GUILayout.Button("Load (Override Instance)", EditorUtilties.DefaultButtonStyle, GUILayout.Width(halfWidth)))
                    {
                        ourTarget._InstantiateTrailPrefab();
                        EditorUtility.SetDirty(ourTarget);
                    }
                    GUI.enabled = true;
                }

                // Helps with weird editor scope error after loading a prefab
                if (loadNew)
                {
                    ourTarget._LoadPrefab();
                    EditorUtility.SetDirty(ourTarget);
                }

                if (saveAsNewButton)
                {
                    ourTarget._SaveAsNewPrefab();
                }


                EditorGUILayout.PropertyField(autoPreviewEnabled, new GUIContent("Auto Preview On Loading")); 

            }

        }

        #endregion

        #region Animations and Testing Buttons etc.

        private void TestingAnimation()
        {
            if (ourTarget.trailUsageType == WeaponTrailEffect.TrailUsageType.Manual)
            {
                using (new LabeledSectionScopeBox("Start Trail"))
                {
                    EditorGUILayout.PropertyField(fadeInDuration_ManualTesting, new GUIContent("Fade In Duration"));
                    EditorGUILayout.PropertyField(trailLengthLifetime_ManualTesting, new GUIContent("Trail Length / Lifetime"));

                    if (GUILayout.Button("Play Effect", EditorUtilties.MarginButtonStyle))
                        ourTarget.StartTrailWithLength(ourTarget.fadeInDuration_ManualTesting, ourTarget.trailLengthLifetime_ManualTesting);


                }
                using (new LabeledSectionScopeBox("End Trail"))
                {
                    EditorGUILayout.PropertyField(fadeOutDuration_ManualTesting, new GUIContent("Fade Out Duration"));
                    if (GUILayout.Button("Stop Effect", EditorUtilties.MarginButtonStyle))
                        ourTarget.StopTrail(ourTarget.fadeOutDuration_ManualTesting);
                }

            }
            else
            {
                using (new LabeledSectionScopeBox("Choose Animation Clip"))
                {
                    //EditorGUILayout.PropertyField(selectedClipIndex);
                    if(ourTarget.SelectedClip != null)
                    {
                        float trailLengthLifetime = ourTarget.SelectedClip?.length ?? 0f;
                        EditorGUILayout.LabelField("Clip Length", trailLengthLifetime.ToString("F2"));
                    }
                    else
                    {
                        ourTarget._RefreshAnimationClipList();
                    }

                    EditorGUI.BeginChangeCheck();

                    string[] clipNames = ourTarget.animationClipList.Select(c => c.name).ToArray();
                    selectedClipIndex.intValue = EditorGUILayout.Popup("Current Clip Used", selectedClipIndex.intValue, clipNames);

                    if (EditorGUI.EndChangeCheck())
                    {
                        serializedObject.ApplyModifiedProperties(); 

                        ResetAnimationState(ourTarget.SelectedTrailPreset);
                    }

                    if (GUILayout.Button("Refresh Animation Clips", EditorUtilties.MarginButtonStyle))
                    {
                        ourTarget._RefreshAnimationClipList();
                    }

                    if (debugMode.boolValue)
                    {
                        AnimationEvent[] events = AnimationUtility.GetAnimationEvents(ourTarget.SelectedClip);

                        EditorGUILayout.LabelField("Animation Events", EditorStyles.boldLabel);

                        if (events.Length == 0)
                        {
                            EditorGUILayout.LabelField("No events found.");
                        }
                        else
                        {
                            foreach (AnimationEvent evt in events)
                            {
                                using (new EditorGUILayout.VerticalScope("box"))
                                {
                                    EditorGUILayout.LabelField("Time", evt.time.ToString("F2"));
                                    EditorGUILayout.LabelField("Function", evt.functionName);
                                    EditorGUILayout.LabelField("Float Parameter", evt.floatParameter.ToString("F2"));
                                }
                            }
                        }
                    }

                }

                using (new LabeledSectionScopeBox("Playback Settings"))
                {

                    EditorGUILayout.PropertyField(animationPlaybackMode);
                    EditorGUILayout.PropertyField(playbackSpeed);

                    if (ourTarget.animationPlaybackMode == WeaponTrailEffect.AnimationPlaybackMode.TrailSegment)
                    {
                        EditorGUILayout.PropertyField(rangeOffset);

                    }
                    else if(ourTarget.animationPlaybackMode == WeaponTrailEffect.AnimationPlaybackMode.FullClipLoop)
                    {
                        //EditorGUILayout.PropertyField(isPlayingLoop);
                    }
                }

                var settings = ourTarget.SelectedTrailPreset;
                if (settings != null)
                {
                    float totalWidth = EditorGUIUtility.currentViewWidth - 60;
                    float halfWidth = totalWidth / 2f;

                    SerializedProperty trailSegmentTime = serializedObject.FindProperty("trailSegmentAnimationPlaybackTime");
                    SerializedProperty fullClipTime = serializedObject.FindProperty("fullClipAnimationPlaybackTime");


                    if (ourTarget.animationPlaybackMode == WeaponTrailEffect.AnimationPlaybackMode.FullClip)
                    {
                        using (new LabeledSectionScopeBox("Full Clip Controls"))
                        {
                            EditorGUI.BeginChangeCheck();
                            float trailLengthLifetime = ourTarget.SelectedClip?.length ?? 0f;
                            EditorGUILayout.Slider(fullClipTime, 0f, trailLengthLifetime, new GUIContent("Full Clip Playback"));
                            if (EditorGUI.EndChangeCheck())
                            {
                                ourTarget.hasPausedPreview = false;

                                //serializedObject.ApplyModifiedProperties();
                                ourTarget.trailSegmentAnimationPlaybackTime = Mathf.Clamp(ourTarget.trailSegmentAnimationPlaybackTime, settings.startTime, settings.endTime);
                                ourTarget._PreviewPoseAtTime(ourTarget.fullClipAnimationPlaybackTime);

                                trailSegmentTime.floatValue = Mathf.Clamp(fullClipTime.floatValue, settings.startTime, settings.endTime);
                            }
                            if (GUILayout.Button("Play Full Clip", EditorUtilties.MarginButtonStyle))
                            {
                                ourTarget.hasPausedPreview = false;
                                ourTarget.PlayFullClipPreview();
                            }
                        }
                    }
                    else if(ourTarget.animationPlaybackMode == WeaponTrailEffect.AnimationPlaybackMode.TrailSegment)
                    {
                        using (new LabeledSectionScopeBox("Trail Segment Conrols"))
                        {
                            EditorGUI.BeginChangeCheck();

                            EditorGUILayout.Slider(trailSegmentTime, settings.startTime, settings.endTime, new GUIContent("Trail Segment Playback"));

                            if (EditorGUI.EndChangeCheck())
                            {
                                ourTarget.hasPausedPreview = false;
                                //serializedObject.ApplyModifiedProperties();
                                ourTarget.trailSegmentAnimationPlaybackTime = Mathf.Clamp(ourTarget.trailSegmentAnimationPlaybackTime, settings.startTime, settings.endTime);
                                ourTarget._PreviewPoseAtTime(ourTarget.trailSegmentAnimationPlaybackTime);

                                fullClipTime.floatValue = trailSegmentTime.floatValue;
                            }
                            if (GUILayout.Button("Play", EditorUtilties.MarginButtonStyle))
                            {
                                ourTarget.hasPausedPreview = false;
                                ourTarget.PlayTrailSegmentPreview(false);
                            }
                        }

                        EditorGUILayout.Space();

                        using (new LabeledSectionScopeBox("Trail Segment With Pause"))
                        {
                            EditorGUILayout.PropertyField(pausePreviewEnabled);
                            if (pausePreviewEnabled.boolValue)
                            {
                                if (ourTarget.pausePreviewEnabled)
                                {
                                    EditorGUILayout.PropertyField(pausePreviewFactor);
                                }
                                if (GUILayout.Button("Play With Pause", EditorUtilties.MarginButtonStyle))
                                {
                                    ourTarget.hasPausedPreview = false;
                                    ourTarget.PlayTrailSegmentPreview(true);
                                }
                            }

                            if (ourTarget.hasPausedPreview == true)
                            {
                                if (GUILayout.Button("Resume", EditorUtilties.MarginButtonStyle))
                                {
                                    ourTarget.playbackDuration_PreviewStop = ourTarget.playbackDuration + 1;
                                    ourTarget.isPlayingTrailSegment = true;
                                    ourTarget.hasPausedPreview = false;

                                    //ourTarget.vfxComponent.playRate = 1f;
                                    ourTarget.vfxComponent.pause = false;
                                    //ourTarget.SetProperty_EffectActive(true);

                                }
                            }
                        }
                    }
                    else
                    {
                        using (new LabeledSectionScopeBox("Full Clip Loop Controls"))
                        {
                            EditorGUI.BeginChangeCheck();
                            float trailLengthLifetime = ourTarget.SelectedClip?.length ?? 0f;
                            EditorGUILayout.Slider(fullClipTime, 0f, trailLengthLifetime, new GUIContent("Full Clip Playback"));
                            if (EditorGUI.EndChangeCheck())
                            {
                                ourTarget.hasPausedPreview = false;

                                //serializedObject.ApplyModifiedProperties();
                                ourTarget.trailSegmentAnimationPlaybackTime = Mathf.Clamp(ourTarget.trailSegmentAnimationPlaybackTime, settings.startTime, settings.endTime);
                                ourTarget._PreviewPoseAtTime(ourTarget.fullClipAnimationPlaybackTime);

                                trailSegmentTime.floatValue = Mathf.Clamp(fullClipTime.floatValue, settings.startTime, settings.endTime);
                            }
                            EditorGUILayout.PropertyField(isPlayingLoop);

                        }
                    }
                }
            }
        }

        #endregion

        #region Trail Settings

        private void ResetAnimationState(TrailPresetSettings settings)
        {
            ourTarget._PreviewPoseAtTime(0);

            SerializedProperty trailSegmentTime = serializedObject.FindProperty("trailSegmentAnimationPlaybackTime");
            SerializedProperty fullClipTime = serializedObject.FindProperty("fullClipAnimationPlaybackTime");

            fullClipTime.floatValue = 0;
            trailSegmentTime.floatValue = 0;
        }


        private void TrailPresetSettings()
        {

            var settings = ourTarget.SelectedTrailPreset;
            if (settings == null) return;

            PresetSettingsSection(settings);
        }

        private void PresetSettingsSection(TrailPresetSettings settings)
        {
            var clip = ourTarget.SelectedClip;
            if (clip == null)
            {
                EditorGUILayout.HelpBox("No Animation Clip selected.", MessageType.Info);
                return;
            }

            var preset = ourTarget.GetOrCreatePresetForClip(clip); // never null for valid clip

            // Draw fields via SerializedProperty for safety:
            var listProp = serializedObject.FindProperty("clipPresets");
            int idx = -1;
            for (int i = 0; i < listProp.arraySize; i++)
            {
                var e = listProp.GetArrayElementAtIndex(i);
                var clipProp = e.FindPropertyRelative("clip");
                if (clipProp.objectReferenceValue == clip) { idx = i; break; }
            }

            if (idx >= 0)
            {
                var entryProp = listProp.GetArrayElementAtIndex(idx);
                var presetProp = entryProp.FindPropertyRelative("preset");

                var enableTrailProp = presetProp.FindPropertyRelative("enableTrail");

                EditorGUILayout.PropertyField(enableTrailProp);
                if(enableTrailProp.boolValue)
                {
                    EditorGUILayout.PropertyField(presetProp.FindPropertyRelative("fadeInDuration_slider"), new GUIContent("Fade In Duration (% of trail)"));
                    EditorGUILayout.PropertyField(presetProp.FindPropertyRelative("fadeOutDuration"), new GUIContent("Fade Out Duration (sec after trail end)"));
                    EditorGUILayout.PropertyField(presetProp.FindPropertyRelative("trailLengthLifetime"), new GUIContent("Length / Lifetime"));

                    float len = clip.length;
                    var startProp = presetProp.FindPropertyRelative("startTime");
                    var endProp = presetProp.FindPropertyRelative("endTime");

                    EditorGUI.BeginChangeCheck();


                    // Default
                    float start = startProp.floatValue;
                    float end = endProp.floatValue;
                    float newStart = Mathf.Clamp(EditorGUILayout.FloatField("Start Time", start), 0f, end);
                    float newEnd = Mathf.Clamp(EditorGUILayout.FloatField("End Time", end), newStart, len);

                    // Detect individual changes to start/end float fields
                    bool startChanged = !Mathf.Approximately(newStart, start);
                    bool endChanged = !Mathf.Approximately(newEnd, end);

                    start = newStart;
                    end = newEnd;

                    float min = start;
                    float max = end;

                    EditorGUILayout.MinMaxSlider("Time Range", ref min, ref max, 0f, len);

                    // Detect if MinMaxSlider changed min/max
                    bool minChanged = !Mathf.Approximately(min, start);
                    bool maxChanged = !Mathf.Approximately(max, end);

                    // Apply if anything changed
                    if (EditorGUI.EndChangeCheck())
                    {
                        startProp.floatValue = min;
                        endProp.floatValue = max;

                        //serializedObject.ApplyModifiedProperties();

                        // Use your booleans to handle logic
                        //if (minChanged || startChanged)
                        //    ourTarget.trailSegmentAnimationPlaybackTime = min;
                        //if (maxChanged || endChanged)
                        //    ourTarget.trailSegmentAnimationPlaybackTime = max;

                        // Apply changes to serialized object
                        serializedObject.ApplyModifiedProperties();

                        // Update target values
                        if (minChanged)
                            ourTarget.trailSegmentAnimationPlaybackTime = min;
                        if (maxChanged)
                            ourTarget.trailSegmentAnimationPlaybackTime = max;

                        ourTarget.trailSegmentAnimationPlaybackTime = Mathf.Clamp(ourTarget.trailSegmentAnimationPlaybackTime, min, max);
                        ourTarget._PreviewPoseAtTime(ourTarget.trailSegmentAnimationPlaybackTime);
                    }
                }
                
            }

        }

        #endregion

    }
}
