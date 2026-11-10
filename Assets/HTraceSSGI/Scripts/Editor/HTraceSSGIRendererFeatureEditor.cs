//pipelinedefine
#define H_URP

#if UNITY_EDITOR
using HTraceSSGI.Scripts.Infrastructure.URP;
using UnityEditor;
using UnityEditor.Rendering.Universal;

namespace HTraceSSGI.Scripts.Editor
{
    [CustomEditor(typeof(HTraceSSGIRendererFeature))]
    public class HTraceSSGIRendererFeatureEditor : ScriptableRendererFeatureEditor
    {
        SerializedProperty useVolumes;

        private void OnEnable()
        {
            useVolumes = serializedObject.FindProperty("UseVolumes");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            
            EditorGUILayout.PropertyField(useVolumes);

            serializedObject.ApplyModifiedProperties();
        }
    }
}
#endif
