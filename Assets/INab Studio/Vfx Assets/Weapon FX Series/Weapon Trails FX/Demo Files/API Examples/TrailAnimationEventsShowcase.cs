using UnityEngine;
using INab.Common;

namespace INab.Demo
{
    /// <summary>
    /// Demonstrates triggering WeaponTrailEffect methods via Animation Events.
    /// </summary>
    public class TrailAnimationEventsShowcase : MonoBehaviour
    {
        [Header("Trail Effect Reference")]
        public WeaponTrailEffect trailEffect;

        [Header("Trail Settings")]
        [Tooltip("Trail length used when starting trail from animation event.")]
        public float trailLength = 0.4f;

        /// <summary>
        /// Starts the trail effect with both fade-in duration and specified trail length.
        /// This method can be assigned to an animation event (float parameter only).
        /// </summary>
        /// <param name="fadeInDuration">Duration to fade in the trail effect.</param>
        public void CallStartTrail(float fadeInDuration)
        {
            if (trailEffect != null)
                trailEffect.StartTrailWithLength(fadeInDuration, trailLength);
        }

        /// <summary>
        /// Ends the trail effect with a given fade-out duration.
        /// This method can be assigned to an animation event (float parameter only).
        /// </summary>
        /// <param name="fadeOutDuration">Duration to fade out the trail effect.</param>
        public void CallEndTrail(float fadeOutDuration)
        {
            if (trailEffect != null)
                trailEffect.StopTrail(fadeOutDuration);
        }

        // Optional: If your workflow requires setting length from the event,
        // Uncomment and use this method in your animation events instead:
        /*
        public void CallSetTrailLength(float length)
        {
            if (trailEffect != null)
                trailEffect.SetTrailLength(length);
        }
        */
    }
}
