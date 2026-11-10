//pipelinedefine
#define H_URP

#if UNITY_EDITOR

using System.IO;
using HTraceSSGI.Scripts.Data.Private;
using UnityEditor;
using UnityEngine;

using HTraceSSGI.Scripts.Infrastructure.URP;

namespace HTraceSSGI.Scripts.Editor
{
    [CustomEditor(typeof(HTraceSSGI))]
    public class HTraceSSGIEditor : UnityEditor.Editor
    {
        private SerializedProperty _profile;
        
        UnityEditor.Editor _cachedProfileEditor;

        static GUIStyle _boxStyle;
        
        private void OnEnable()
        {
            _profile = serializedObject.FindProperty("Profile");
        }

        private void OnDisable()
        {
            DestroyCachedProfileEditor();
        }

        private void DestroyCachedProfileEditor()
        {
            if (_cachedProfileEditor != null)
            {
                DestroyImmediate(_cachedProfileEditor);
                _cachedProfileEditor = null;
            }
        }

        public override void OnInspectorGUI()
        {
            if (_boxStyle == null) 
            {
                _boxStyle = new GUIStyle(GUI.skin.box);
                _boxStyle.padding = new RectOffset(15, 10, 5, 5);
            }

            if (HTraceSSGIRendererFeature.IsUseVolumes == true)
            {
                EditorGUILayout.HelpBox("\"Use Volumes\" checkbox in the HTrace SSGI Renderer feature is enabled, use the HTraceSSGI volume override in your scenes.", MessageType.Warning, wide: true);
                return;
            }
            
            EditorGUILayout.PropertyField(_profile);
            
            EditorGUILayout.Space(5);
            
            HTraceSSGIProfile profile = _profile.objectReferenceValue as HTraceSSGIProfile;
            if (profile != null)
            {
                CreateCachedEditor(profile, null, ref _cachedProfileEditor);

                EditorGUILayout.BeginVertical();
                if (_cachedProfileEditor != null)
                    _cachedProfileEditor.OnInspectorGUI();

                EditorGUILayout.Separator();

                if (GUILayout.Button("Save As New Profile")) 
                {
                    ExportProfile();
                }
                EditorGUILayout.EndVertical();
            }
            else 
            {
                DestroyCachedProfileEditor();
                EditorGUILayout.HelpBox("Create or assign a profile.", MessageType.Info);
                if (GUILayout.Button("New Profile")) 
                {
                    CreateProfile();
                }
            }

            serializedObject.ApplyModifiedProperties();
        }
        
        void CreateProfile() {

            var fp = CreateInstance<HTraceSSGIProfile>();
            fp.name = "New HTrace SSGI Profile";

            string path = "Assets";
            foreach (Object obj in Selection.GetFiltered(typeof(Object), SelectionMode.Assets)) {
                path = AssetDatabase.GetAssetPath(obj);
                if (File.Exists(path)) {
                    path = Path.GetDirectoryName(path);
                }
                break;
            }

            string fullPath = path + "/" + fp.name + ".asset";
            fullPath = AssetDatabase.GenerateUniqueAssetPath(fullPath);

            AssetDatabase.CreateAsset(fp, fullPath);
            AssetDatabase.SaveAssets();
            _profile.objectReferenceValue = fp;
            EditorGUIUtility.PingObject(fp);
        }

        void ExportProfile() {
            var fp = (HTraceSSGIProfile)_profile.objectReferenceValue;
            var newProfile = Instantiate(fp);

            string path = AssetDatabase.GetAssetPath(fp);
            string fullPath = path;
            if (string.IsNullOrEmpty(path)) {
                path = "Assets";
                foreach (Object obj in Selection.GetFiltered(typeof(Object), SelectionMode.Assets)) {
                    path = AssetDatabase.GetAssetPath(obj);
                    if (File.Exists(path)) {
                        path = Path.GetDirectoryName(path);
                    }
                    break;
                }
                fullPath = path + "/" + fp.name + ".asset";
            }
            fullPath = AssetDatabase.GenerateUniqueAssetPath(fullPath);
            AssetDatabase.CreateAsset(newProfile, fullPath);
            AssetDatabase.SaveAssets();
            _profile.objectReferenceValue = newProfile;
            EditorGUIUtility.PingObject(fp);
        }
    }
}
#endif
