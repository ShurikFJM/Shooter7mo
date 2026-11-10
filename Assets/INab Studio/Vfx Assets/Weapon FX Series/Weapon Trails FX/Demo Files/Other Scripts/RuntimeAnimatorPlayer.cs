using System.Collections.Generic;
using System.Linq;
using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif
using TMPro;

#if UNITY_EDITOR
using UnityEditor;
#endif

using INab.Common;

namespace INab.Demo
{
    /// <summary>
    /// Controls runtime playback of animations with selectable weapon trail prefabs.
    /// Supports switching animations, trail length, speed, and cameras via input and UI.
    /// </summary>
    [ExecuteAlways]
    public class RuntimeAnimatorPlayer : MonoBehaviour
    {
        [Header("Trail Settings")]
        [Range(0f, 4f)]
        [Tooltip("Multiplier for the length of the weapon trail.")]
        public float trailLengthMultiplier = 1f;

        [Range(0f, 1f)]
        [Tooltip("Playback speed multiplier for the animation.")]
        public float animationSpeed = 1f;

        [Header("Trail Prefabs & Animator")]
        [Tooltip("List of trail prefab GameObjects.")]
        public List<GameObject> trailPrefabs;

        [Tooltip("Index of the currently selected trail prefab.")]
        public int selectedTrailPrefab = 0;

        [Tooltip("Reference to the WeaponTrailEffect component controlling trails.")]
        public WeaponTrailEffect weaponTrailEffect;

        [Tooltip("Animator used for playback of selected clips.")]
        public Animator currentlyUsedAnimator;

        [Header("Animation Clips")]
        
        public bool useAnimations = false;

        [Tooltip("List of animation clips available from the animator.")]
        [SerializeField] public List<AnimationClip> animationClipList = new List<AnimationClip>();

        [Tooltip("Index of the currently selected animation clip.")]
        [SerializeField] public int selectedClipIndex = 0;

        /// <summary>
        /// Provides the count of available animation clips.
        /// </summary>
        public int AnimationClipsCount => animationClipList.Count;

        /// <summary>
        /// Holds the names of clips for display/UI purposes.
        /// </summary>
        public string[] animationClipsNames;

        /// <summary>
        /// Returns the currently selected animation clip or null if index is out of range.
        /// </summary>
        public AnimationClip SelectedClip =>
            (selectedClipIndex >= 0 && selectedClipIndex < animationClipList.Count)
            ? animationClipList[selectedClipIndex]
            : null;

        [Header("UI References")]
        public TextMeshProUGUI clipNameText;
        public TextMeshProUGUI trailsPrefabName;

        [Header("Cameras")]
        [Tooltip("Cameras for different animations, activated based on selected clip.")]
        public List<GameObject> cameras;

        public bool useCameras = false;

        private void Start()
        {
            FindAnimations();
        }

        private void OnEnable()
        {
            FindAnimations();
        }

        public void FindAnimations()
        {
#if UNITY_EDITOR

            // Fetch distinct animation clips from the Animator's gameObject
            animationClipList = AnimationUtility.GetAnimationClips(currentlyUsedAnimator.gameObject)
                                   .Where(c => c != null)
                                   .Distinct()
                                   .ToList();

            animationClipsNames = animationClipList.Select(c => c.name).ToArray();
#endif
        }

        /// <summary>
        /// Plays the selected animation clip on the Animator,
        /// replacing the original clip with an override controller.
        /// </summary>
        public void PlaySelected()
        {
            if (SelectedClip == null || currentlyUsedAnimator == null)
                return;

            var overrideController = new AnimatorOverrideController(currentlyUsedAnimator.runtimeAnimatorController);
            var originalClip = overrideController.animationClips[0];
            overrideController[originalClip.name] = SelectedClip;

            currentlyUsedAnimator.runtimeAnimatorController = overrideController;
            currentlyUsedAnimator.speed = animationSpeed;
            currentlyUsedAnimator.Play(originalClip.name, 0, 0f);
        }

        /// <summary>
        /// Called by UI slider to update trail length multiplier.
        /// </summary>
        public void ChangedSlider(float value)
        {
            trailLengthMultiplier = value;
        }

        /// <summary>
        /// Called by UI slider to update animation playback speed.
        /// </summary>
        public void ChangedAnimationSpeedSlider(float value)
        {
            animationSpeed = value;
        }

        private static bool WasKeyPressed(KeyCode keyCode)
        {
    #if ENABLE_INPUT_SYSTEM
            if (Keyboard.current == null) return false;

            if (keyCode == KeyCode.Q) return Keyboard.current.qKey.wasPressedThisFrame;
            if (keyCode == KeyCode.E) return Keyboard.current.eKey.wasPressedThisFrame;
            if (keyCode == KeyCode.P) return Keyboard.current.pKey.wasPressedThisFrame;
            if (keyCode == KeyCode.A) return Keyboard.current.aKey.wasPressedThisFrame;
            if (keyCode == KeyCode.D) return Keyboard.current.dKey.wasPressedThisFrame;
            return false;
    #else
            return Input.GetKeyDown(keyCode);
    #endif
        }

        private void Update()
        {
            // Update weapon trail length multiplier every frame
            weaponTrailEffect.SetLengthMultiplier(trailLengthMultiplier);

            // Update UI text with selected clip and trail prefab names
            if (clipNameText != null && SelectedClip != null)
                clipNameText.text = SelectedClip.name;

            if (trailsPrefabName != null && trailPrefabs.Count > 0)
                trailsPrefabName.text = trailPrefabs[selectedTrailPrefab].name;

            // Input handling for cycling through trail prefabs (Q/E)
            if (WasKeyPressed(KeyCode.Q))
                selectedTrailPrefab = (selectedTrailPrefab - 1 + trailPrefabs.Count) % trailPrefabs.Count;

            if (WasKeyPressed(KeyCode.E))
                selectedTrailPrefab = (selectedTrailPrefab + 1) % trailPrefabs.Count;

            // Assign new trail prefab and play animation (P)
            if (WasKeyPressed(KeyCode.P))
            {
                weaponTrailEffect.SetNewTrailPrefab(trailPrefabs[selectedTrailPrefab]);
                PlaySelected();
            }

            if (!useAnimations) return;

            // Cycle through animation clips and activate corresponding camera (A/D)
            if (WasKeyPressed(KeyCode.A))
            {
                selectedClipIndex = (selectedClipIndex - 1 + AnimationClipsCount) % AnimationClipsCount;
                SetActiveCamera(selectedClipIndex);
            }

            if (WasKeyPressed(KeyCode.D))
            {
                selectedClipIndex = (selectedClipIndex + 1) % AnimationClipsCount;
                SetActiveCamera(selectedClipIndex);
            }
        }

        /// <summary>
        /// Enables the camera at index and disables all others.
        /// </summary>
        private void SetActiveCamera(int index)
        {
            if (useCameras == false) return;

            for (int i = 0; i < cameras.Count; i++)
            {
                cameras[i].SetActive(i == index);
            }
        }
    }
}
