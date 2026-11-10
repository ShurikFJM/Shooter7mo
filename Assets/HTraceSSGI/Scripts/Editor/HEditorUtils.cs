#if UNITY_EDITOR
using System;
using UnityEditor;
using UnityEditor.AnimatedValues;
using UnityEngine;

namespace HTraceSSGI.Scripts.Editor
{
	public static class HEditorUtils
	{
        private static GUIStyle s_linkStyle;
        private static GUIStyle s_separatorStyle;

        public static void DrawLinkRow(params (string label, Action onClick)[] links)
        {
            if (s_linkStyle == null)
            {
                s_linkStyle = new GUIStyle(GUI.skin.label)
                {
                    fontStyle = FontStyle.Bold,
                    fontSize = 10,
                    alignment = TextAnchor.MiddleLeft,
                    normal = { textColor = new Color(0.35f, 0.55f, 0.75f) },
                    hover = { textColor = EditorGUIUtility.isProSkin ? Color.white : Color.black },
                    // padding = new RectOffset(0, 0, 0, 0),
                    // margin = new RectOffset(0, 0, 0, 0)
                };
            }

            if (s_separatorStyle == null)
            {
                s_separatorStyle = new GUIStyle(GUI.skin.label)
                {
                    fontSize = 12,
                    alignment = TextAnchor.MiddleCenter,
                    normal = { textColor = new Color(0.9f, 0.9f, 0.9f,1f) },
                    padding = new RectOffset(0, 0, 1, 0),
                    margin = new RectOffset(0, 0, 0, 0)
                };
            }

            float maxWidth = EditorGUIUtility.currentViewWidth - 40; // scroll
            float currentLineWidth = 0;

            GUILayout.BeginVertical();
            GUILayout.BeginHorizontal();

            for (int i = 0; i < links.Length; i++)
            {
                var content = new GUIContent(links[i].label);
                Vector2 size = s_linkStyle.CalcSize(content);
                float neededWidth = size.x + 8; // text + |

                // new line
                if (currentLineWidth + neededWidth > maxWidth)
                {
                    GUILayout.EndHorizontal();
                    GUILayout.BeginHorizontal();
                    currentLineWidth = 0;
                }

                if (DrawClickableButton(links[i].label, onClick: links[i].onClick))
                {
                    // nothing here
                }
                currentLineWidth += size.x;

                if (i < links.Length - 1)
                {
                    GUILayout.Space(8);
                    // GUILayout.Label("|", s_separatorStyle, GUILayout.Width(12));
                    // GUILayout.Space(2);

                    currentLineWidth += 8; // width |
                }
            }

            GUILayout.EndHorizontal();
            GUILayout.EndVertical();
        }

        public static bool DrawClickableButton(string text, Action onClick = null, GUIStyle baseStyle = null)
        {
            if (s_linkStyle == null)
            {
                s_linkStyle = new GUIStyle(baseStyle ?? GUI.skin.label)
                {
                    fontStyle = FontStyle.Bold,
                    fontSize = 10,
                    alignment = TextAnchor.MiddleLeft,
                    normal = { textColor = new Color(0.35f, 0.55f, 0.75f) },
                    hover = { textColor = EditorGUIUtility.isProSkin ? Color.white : Color.black }
                };
            }

            Rect rect = GUILayoutUtility.GetRect(new GUIContent(text), s_linkStyle, GUILayout.ExpandWidth(false));
            bool clicked = GUI.Button(rect, text, s_linkStyle);

            EditorGUIUtility.AddCursorRect(rect, MouseCursor.Link);

            if (clicked)
                onClick?.Invoke();

            return clicked;
        }

        public static bool DrawClickableLink(string text, string url, bool useEmoji = false, GUIStyle baseStyle = null)
        {
            if (s_linkStyle == null)
            {
                s_linkStyle = new GUIStyle(baseStyle ?? GUI.skin.label)
                {
                    fontStyle = FontStyle.Bold,
                    fontSize = 10,
                    //normal = { textColor = new Color(0.20f, 0.50f, 0.80f) },
                    normal = { textColor = new Color(0.35f, 0.55f, 0.75f) },
                    hover = { textColor = EditorGUIUtility.isProSkin ? Color.white : Color.black }
                };
            }

            if (useEmoji)
                text += " \U0001F517"; //\U0001F310
            bool clicked = GUILayout.Button(text, s_linkStyle, GUILayout.ExpandWidth(false));
            EditorGUIUtility.AddCursorRect(GUILayoutUtility.GetLastRect(), MouseCursor.Link);

            if (clicked)
            {
                Application.OpenURL(url);
            }

            return clicked;
        }

        public readonly struct FoldoutScope : IDisposable
        {
            private readonly bool wasIndent;

            public FoldoutScope(AnimBool value, out bool shouldDraw, string label, bool indent = true, SerializedProperty toggle = null)
            {
                value.target = Foldout(value.target, label, toggle);
                shouldDraw = EditorGUILayout.BeginFadeGroup(value.faded);
                if (shouldDraw && indent)
                {
                    Indent();
                    wasIndent = true;
                }
                else
                {
                    wasIndent = false;
                }
            }

            public void Dispose()
            {
                if (wasIndent)
                    EndIndent();
                EditorGUILayout.EndFadeGroup();
            }
        }

        public static void HorizontalLine(float height = 1, float width = -1, Vector2 margin = new Vector2())
        {
            GUILayout.Space(margin.x);

            var rect = EditorGUILayout.GetControlRect(false, height);
            if (width > -1)
            {
                var centerX = rect.width / 2;
                rect.width = width;
                rect.x += centerX - width / 2;
            }

            Color color = EditorStyles.label.active.textColor;
            color.a = 0.5f;
            EditorGUI.DrawRect(rect, color);

            GUILayout.Space(margin.y);
        }

        public static bool Foldout(bool value, string label, SerializedProperty toggle = null)
        {
            bool _value;
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.BeginHorizontal();

            if (toggle != null && !toggle.boolValue)
            {
                EditorGUI.BeginDisabledGroup(true);
                _value = EditorGUILayout.Toggle(value, EditorStyles.foldout);
                EditorGUI.EndDisabledGroup();

                _value = false;
            }
            else
            {
                _value = EditorGUILayout.Toggle(value, EditorStyles.foldout);
            }

            if (toggle != null)
            {
                EditorGUI.BeginChangeCheck();
                EditorGUILayout.PropertyField(toggle, GUIContent.none, GUILayout.Width(20));
                if (EditorGUI.EndChangeCheck() && toggle.boolValue)
                    _value = true;
            }

            EditorGUILayout.EndHorizontal();
            EditorGUILayout.EndVertical();

            var rect = GUILayoutUtility.GetLastRect();
            rect.x += 20;
            rect.width -= 20;

            if (toggle != null && !toggle.boolValue)
            {
                EditorGUI.BeginDisabledGroup(true);
                EditorGUI.LabelField(rect, label, EditorStyles.boldLabel);
                EditorGUI.EndDisabledGroup();
            }
            else
            {
                EditorGUI.LabelField(rect, label, EditorStyles.boldLabel);
            }

            return _value;
        }

        public static void Indent()
        {
            EditorGUILayout.BeginHorizontal();
            GUILayout.Space(16);
            EditorGUILayout.BeginVertical();
        }

        public static void EndIndent()
        {
            GUILayout.Space(10);
            EditorGUILayout.EndVertical();
            EditorGUILayout.EndHorizontal();
        }
    }
}
#endif