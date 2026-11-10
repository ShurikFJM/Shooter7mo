using System;
using UnityEditor;
using UnityEngine;

namespace INab.Common
{

    public static class EditorUtilties
    {
        public struct LabeledSectionScope : IDisposable
        {
            public LabeledSectionScope(string label)
            {
                EditorGUILayout.LabelField(label, EditorStyles.boldLabel);
                EditorGUI.indentLevel++;
            }

            public void Dispose()
            {
                EditorGUI.indentLevel--;
                EditorGUILayout.Space();
            }
        }

        private static readonly float IndentWidth = 15f;

        public struct LabeledSectionScopeBox : IDisposable
        {
            public LabeledSectionScopeBox(string label)
            {
                EditorGUILayout.LabelField(label, EditorStyles.boldLabel);

                EditorGUILayout.BeginHorizontal();
                GUILayout.Space(EditorGUI.indentLevel * IndentWidth);

                EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            }

            public void Dispose()
            {
                EditorGUILayout.EndVertical();
                EditorGUILayout.EndHorizontal();
                EditorGUILayout.Space();
            }
        }


        public struct FoldoutHeaderScope : IDisposable
        {
            private bool isExpanded;

            public bool IsExpanded => isExpanded;

            public FoldoutHeaderScope(ref bool foldoutState, string label)
            {
                foldoutState = EditorGUILayout.BeginFoldoutHeaderGroup(foldoutState, label, EditorStyles.foldoutHeader);
                isExpanded = foldoutState;
                EditorGUI.indentLevel++;
            }

            public void Dispose()
            {
                EditorGUI.indentLevel--;
                EditorGUILayout.EndFoldoutHeaderGroup();
            }
        }


        private static GUIStyle _indentedFoldoutHeader;
        public static GUIStyle IndentedFoldoutHeader
        {
            get
            {
                if (_indentedFoldoutHeader == null)
                {
                    _indentedFoldoutHeader = new GUIStyle(EditorStyles.foldoutHeader)
                    {
                        margin = new RectOffset(35, 0, 0, 0),
                    };
                }

                return _indentedFoldoutHeader;
            }
        }

        private static GUIStyle _marginButtonStyle;
        public static GUIStyle MarginButtonStyle
        {
            get
            {
                if (_marginButtonStyle == null)
                {
                    _marginButtonStyle = new GUIStyle(EditorStyles.miniButton)
                    {
                        margin = new RectOffset(22, 0, 0, 0),

                    };
                }

                return _marginButtonStyle;
            }
        }

        private static GUIStyle _indentedButtonStyle;
        public static GUIStyle IndentedButtonStyle
        {
            get
            {
                if (_indentedButtonStyle == null)
                {
                    _indentedButtonStyle = new GUIStyle(EditorStyles.miniButton)
                    {
                        margin = new RectOffset(35, 0, 0, 0),
                        //fontStyle = FontStyle.Bold
                    };
                }

                return _indentedButtonStyle;
            }
        }

        private static GUIStyle _indentedButtonStyleDouble;
        public static GUIStyle IndentedButtonStyleDouble
        {
            get
            {
                if (_indentedButtonStyleDouble == null)
                {
                    _indentedButtonStyleDouble = new GUIStyle(EditorStyles.miniButton)
                    {
                        margin = new RectOffset(48, 0, 0, 0)
                    };
                }

                return _indentedButtonStyleDouble;
            }
        }

        private static GUIStyle _defaultButtonStyle;
        public static GUIStyle DefaultButtonStyle
        {
            get
            {
                if (_defaultButtonStyle == null)
                {
                    _defaultButtonStyle = new GUIStyle(EditorStyles.miniButton)
                    {
                        margin = new RectOffset(0, 0, 0, 0),

                    };
                }

                return _defaultButtonStyle;
            }
        }

        private static GUIStyle _centeredBoldLabel;
        public static GUIStyle CenteredBoldLabel
        {
            get
            {
                if (_centeredBoldLabel == null)
                {
                    _centeredBoldLabel = new GUIStyle(EditorStyles.boldLabel)
                    {
                        alignment = TextAnchor.MiddleCenter
                    };
                }

                return _centeredBoldLabel;
            }
        }


        public static bool GetFoldoutState(string key, UnityEngine.Object gameObject)
        {
            string fullKey = key + "_" + gameObject.GetHashCode();
            return SessionState.GetBool(fullKey, false);
        }

        public static void SetFoldoutState(string key, UnityEngine.Object gameObject, bool value)
        {
            string fullKey = key + "_" + gameObject.GetHashCode();
            SessionState.SetBool(fullKey, value);
        }

        //==============================================================================
        // Particle Effects
        //==============================================================================

        public static bool FoldoutGeneral(UnityEngine.Object gameObject)
        {
            return GetFoldoutState("FoldoutGeneral", gameObject);
        }

        public static void SetFoldoutGeneral(UnityEngine.Object gameObject, bool value)
        {
            SetFoldoutState("FoldoutGeneral", gameObject, value);
        }

        public static bool FoldoutEditorTesting(UnityEngine.Object gameObject)
        {
            return GetFoldoutState("FoldoutEditorTesting", gameObject);
        }

        public static void SetFoldoutEditorTesting(UnityEngine.Object gameObject, bool value)
        {
            SetFoldoutState("FoldoutEditorTesting", gameObject, value);
        }

        public static bool FoldoutEffectSettings(UnityEngine.Object gameObject)
        {
            return GetFoldoutState("FoldoutEffectSettings", gameObject);
        }

        public static bool AnimatorEffectSettings(UnityEngine.Object gameObject)
        {
            return GetFoldoutState("AnimatorEffectSettings", gameObject);
        }

        public static void SetFoldoutEffectSettings(UnityEngine.Object gameObject, bool value)
        {
            SetFoldoutState("FoldoutEffectSettings", gameObject, value);
        }

        public static void SetAnimatorEffectSettings(UnityEngine.Object gameObject, bool value)
        {
            SetFoldoutState("AnimatorEffectSettings", gameObject, value);
        }

        public static bool FoldoutMaterialsProperties(UnityEngine.Object gameObject)
        {
            return GetFoldoutState("FoldoutMaterialsProperties", gameObject);
        }

        public static void SetFoldoutMaterialsProperties(UnityEngine.Object gameObject, bool value)
        {
            SetFoldoutState("FoldoutMaterialsProperties", gameObject, value);
        }

        //==============================================================================

    }
}