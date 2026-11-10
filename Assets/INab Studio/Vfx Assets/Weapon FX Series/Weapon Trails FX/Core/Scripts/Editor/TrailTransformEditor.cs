using UnityEditor;
using UnityEngine;

namespace INab.Common
{
    [CustomEditor(typeof(TrailTransform))]
    [CanEditMultipleObjects]
    public class TrailTransformEditor : Editor
    {
        void OnSceneGUI()
        {
            TrailTransform ourTarget = (TrailTransform)target;
            var weaponTrailEffect = ourTarget.weaponTrailEffect;
            weaponTrailEffect.DrawHandles();

        }
    }
}