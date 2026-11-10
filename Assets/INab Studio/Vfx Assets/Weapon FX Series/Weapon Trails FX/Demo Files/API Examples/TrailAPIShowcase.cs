using UnityEngine;
using INab.Common;

namespace INab.Demo
{
    /// <summary>
    /// Demonstrates how to use the Weapon Trail API in manual mode
    /// to control procedural trail effects at runtime.
    /// </summary>
    public class TrailAPIShowcase : MonoBehaviour
    {
        [Header("Trail Effect Reference")]
        public WeaponTrailEffect trailEffect;

        [Header("Trail Settings")]
        [Tooltip("Default length of the weapon trail.")]
        public float trailLength = 0.4f;

        [Tooltip("Fade-in time when trail starts.")]
        public float fadeInDuration = 0.1f;

        [Tooltip("Fade-out time when trail ends.")]
        public float fadeOutDuration = 0.4f;

        [Tooltip("Multiplier applied to trail length per instance.")]
        public float lengthMultiplier = 1f;

        [Header("Trail Prefabs")]
        public GameObject trailPrefab1;
        public GameObject trailPrefab2;

        /// <summary>
        /// Sets the trail length from a UI slider value.
        /// </summary>
        /// <param name="newValue"></param>
        public void SetLengthPropertyWithSlider(float newValue)
        {
            trailLength = newValue;
            SetTrailLength();
        }

        /// <summary>
        /// Sets the trail length before starting the trail.
        /// </summary>
        public void SetTrailLength()
        {
            if (trailEffect != null)
                trailEffect.SetTrailLength(trailLength);
        }

        /// <summary>
        /// Starts the trail effect with the defined fade-in duration.
        /// </summary>
        public void StartTrail()
        {
            // We need to specify the trail length before starting the trail.
            SetTrailLength();

            if (trailEffect != null)
                trailEffect.StartTrail(fadeInDuration);
        }

        /// <summary>
        /// Stops the trail effect using the fade-out duration.
        /// </summary>
        public void EndTrail()
        {
            if (trailEffect != null)
                trailEffect.StopTrail(fadeOutDuration);
        }

        /// <summary>
        /// Updates the trail's length multiplier at runtime.
        /// 
        /// Note:
        /// - Affects only the current instance of the VFX Graph.
        /// - Does not override the base length (SetTrailLength).
        /// - Should be applied after loading a new trail prefab.
        /// </summary>
        public void ChangeLengthMultiplier()
        {
            if (trailEffect != null)
                trailEffect.SetLengthMultiplier(lengthMultiplier);
        }

        /// <summary>
        /// Switches the trail effect to a new prefab at runtime.
        /// </summary>
        /// <param name="newPrefab">The new trail prefab to apply.</param>
        public void SetNewTrailPrefab(GameObject newPrefab)
        {
            if (trailEffect != null && newPrefab != null)
                trailEffect.SetNewTrailPrefab(newPrefab);
        }

        /// <summary>
        /// Applies the first demo trail prefab.
        /// </summary>
        public void SetTrailPrefab1()
        {
            SetNewTrailPrefab(trailPrefab1);
            StartTrail();
        }

        /// <summary>
        /// Applies the second demo trail prefab.
        /// </summary>
        public void SetTrailPrefab2()
        {
            SetNewTrailPrefab(trailPrefab2);
            StartTrail();
        }
    }
}
