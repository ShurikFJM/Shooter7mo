using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace CoInspector
{

    internal class HierarchyPopup : EditorWindow
    {
        private GameObject selectedGameObject;
        static private GUIStyle labelStyle;
        static private GUIStyle foldoutStyle;
        static Vector2 lastPosition = Vector2.zero;
        private Vector2 scrollPosition;
        private Vector2 startPosition;
        private float maxX = 0;
        private float countUntilTarget = 0;
        private int indentSize = 13;
        private int hoveredIndent = -1;
        private bool reachedTarget = false;
        private bool resizedOnStart = false;
        private bool firstScroll = false;
        private bool hoveringIndent = false;
        private CoInspectorWindow owner;
        private Dictionary<GameObject, bool> expandedObjects = new Dictionary<GameObject, bool>();
        private Dictionary<GameObject, int> pendingLines = new Dictionary<GameObject, int>();
        private Color colorSelected = new Color(0.58f, 0.58f, 0.90f, 0.30f);
        private Color lineColor;
        bool colorGrid = false;
        private float maxWidth = 0;
        private float maxHeight = 0;
        GameObject root;


        internal static void ShowWindow(GameObject gameObject, CoInspectorWindow _owner, Vector2 mousePosition)
        {
            PopUpTip.Hide();
            if (gameObject == null)
            {
                return;
            }
            InitializeStyles();
            HierarchyPopup window = GetWindow<HierarchyPopup>(true, "Local Hierarchy");
            window.InitializeHierarchy(gameObject, _owner, mousePosition);
        }

        internal static void InitializeStyles()
        {
            labelStyle = new GUIStyle(CustomGUIStyles.RichLabel);
            labelStyle.stretchWidth = false;
            labelStyle.margin = new RectOffset(0, 0, 0, 0);
            labelStyle.padding = new RectOffset(0, 0, 0, 0);
            labelStyle.fixedHeight = 16;
            labelStyle.contentOffset = new Vector2(2, 2);
            foldoutStyle = new GUIStyle(EditorStyles.foldout);
            foldoutStyle.margin.top = 0;
            foldoutStyle.margin.left = 4;
            foldoutStyle.fixedHeight = 16;
            foldoutStyle.fixedWidth = 1;
        }

        internal void InitializeHierarchy(GameObject gameObject, CoInspectorWindow _owner, Vector2 mousePosition)
        {
            lineColor = CustomColors.SimpleShadow;
            if (EditorGUIUtility.isProSkin)
            {
                lineColor = new Color(0.4f, 0.4f, 0.4f, 1f);
            }
            startPosition = new Vector2(_owner.position.x, _owner.position.y);
            startPosition.x += mousePosition.x;
            startPosition.y += mousePosition.y + 80;
            if (lastPosition != Vector2.zero)
            {
                startPosition = lastPosition;
            }
            maxX = _owner.position.xMax;
            selectedGameObject = gameObject;
            owner = _owner;
            root = FindContextualRoot(gameObject);
            titleContent = new GUIContent("Local Hierarchy of '" + gameObject.name + "'");
            if (gameObject.transform.parent == null)
            {
                float width = CustomGUIStyles.WrapLabelStyle.CalcSize(new GUIContent("<i>Root-level siblings are not shown!</i>")).x + 40;
                minSize = new Vector2(width, 100);
            }
            Focus();
            ExpandPath(gameObject);
        }

        GameObject FindContextualRoot(GameObject gameObject)
        {
            Transform current = gameObject.transform;
            while (current.parent != null)
            {
                current = current.parent;
            }
            return current.gameObject;
        }

        float ScrollToPosition(float _height, float targetY)
        {
            targetY -= 18;
            float centeredPosition = targetY - (_height / 2);
            return centeredPosition;
        }

        void OnGUI()
        {
            GUI.backgroundColor *= 0.97f;
            if (selectedGameObject == null || root == null)
            {
                EditorGUILayout.LabelField("<i>No GameObject selected!</i>");
                return;
            }
            pendingLines.Clear();
            pendingLines.Add(root, 0);
            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);
            EditorGUILayout.BeginHorizontal();
            GUILayout.Space(3);
            EditorGUILayout.BeginVertical();
            GUILayout.Space(3);
            bool changed = false;
            if (Event.current.type == EventType.Repaint)
            {
                hoveringIndent = false;
            }
            EditorGUI.BeginChangeCheck();
            DrawGameObject(root, 0);
            if (EditorGUI.EndChangeCheck())
            {
                changed = true;
            }
            EditorGUILayout.EndVertical();
            EditorGUILayout.EndHorizontal();
            if (expandedObjects != null && expandedObjects.ContainsKey(root) && (expandedObjects.Count == 1 || expandedObjects[root] == false))
            {
                CustomGUIStyles.InfoLabel("<i>Root-level siblings are not shown!</i>");
            }
            EditorGUILayout.EndScrollView();
            if (!resizedOnStart)
            {
                Resize();
            }
            if (changed)
            {
                ResetSize();
            }
            if (!hoveringIndent)
            {
                hoveredIndent = -1;
            }
        }

        private void ResetSize()
        {
            resizedOnStart = false;
            maxHeight = 0;
            maxWidth = 0;
            Repaint();
        }

        private void Resize()
        {
            if (reachedTarget)
            {
                float height = 500;
                if (maxHeight < height)
                {
                    height = maxHeight;
                }
                if (height > maxHeight)
                {
                    height = maxHeight;
                }
                Rect newRect;
                if (firstScroll)
                {
                    newRect = new Rect(position.x, position.y, maxWidth, height + 40);
                }
                else
                {
                    newRect = new Rect(startPosition.x, startPosition.y, maxWidth, height + 40);
                    if (lastPosition == Vector2.zero)
                    {
                        newRect.x -= maxWidth / 2;
                    }

                }
                if (newRect.xMax > maxX)
                {
                    newRect.x = maxX - newRect.width;
                }
                if (newRect.x < 0)
                {
                    newRect.x = 0;
                }
                if (!firstScroll && newRect.height > 400)
                {
                    newRect.height = 400;
                }
                if (newRect.width < 200)
                {
                    newRect.width = 200;
                }
                if (!firstScroll && lastPosition == Vector2.zero)
                {
                    newRect.x -= 60;
                }
                newRect.width += 25;
                newRect.height -= 30;
                newRect.x = position.x;
                newRect.y = position.y;
                position = newRect;
                resizedOnStart = true;
                if (!firstScroll)
                {
                    firstScroll = true;
                    scrollPosition.y = ScrollToPosition(newRect.height, countUntilTarget);
                }
                Repaint();
            }
        }

        private void OnDestroy()
        {
            lastPosition = position.position;
        }

        private void ShowContextMenu(GameObject obj)
        {
            GenericMenu menu = new GenericMenu();
            menu.AddItem(new GUIContent("Open in current Tab"), false, () =>

            {
                owner.SetTargetGameObject(obj);
                Close();
            });
            menu.AddItem(new GUIContent("Open in new Tab"), false, () =>
            {
                owner.AddTabNext();
                owner.SetTargetGameObject(obj);
                Close();
            });
            menu.AddSeparator("");
            menu.AddItem(new GUIContent("Select in Hierarchy"), false, () =>
            {
                owner.ignoreNextSelection = true;
                Selection.activeGameObject = obj;
                Close();
            }
            );
            menu.AddSeparator("");
            menu.AddItem(new GUIContent("Ping in Hierarchy"), false, () => EditorGUIUtility.PingObject(obj));
            menu.AddItem(new GUIContent("Frame on Scene View"), false, () => EditorUtils.FocusOnSceneView(obj));
            menu.ShowAsContext();
        }

        private void OnLostFocus()
        {
            Close();
        }

        void DrawGameObject(GameObject obj, int indentLevel, int childIndex = 0)
        {
            if ((obj.hideFlags & HideFlags.HideInHierarchy) != 0)
            {
                return;
            }
            bool hasChildren = obj.transform.childCount > 0;
            if (!resizedOnStart)
            {
                maxHeight += 18;
            }
            if (!reachedTarget)
            {
                countUntilTarget += 18;
            }
            GUILayout.BeginHorizontal(CustomGUIStyles.InspectorButtonStyle, GUILayout.Height(18));
            GUILayout.Space(indentLevel * indentSize);
            Texture2D icon = EditorUtils.GetBestFittingIconForGameObject(obj);
            string _name = obj.name;
            if (obj == selectedGameObject)
            {
                _name = "<b>" + _name + "</b>";
            }
            GUIContent content = new GUIContent(" " + _name, icon);
            bool isExpanded = expandedObjects.ContainsKey(obj) && expandedObjects[obj];
            bool drawFoldout = hasChildren;
            GUIStyle _labelStyle = labelStyle;
            GUIStyle _foldoutStyle = foldoutStyle;

            if (drawFoldout)
            {
                drawFoldout = false;
                foreach (Transform child in obj.transform)
                {
                    if ((child.gameObject.hideFlags & HideFlags.HideInHierarchy) == 0)
                    {
                        drawFoldout = true;
                        break;
                    }
                }
            }
            if (drawFoldout)
            {
                isExpanded = EditorGUILayout.Foldout(isExpanded, "", false, _foldoutStyle);
                expandedObjects[obj] = isExpanded;
            }
            GUILayout.EndHorizontal();
            Rect rect = GUILayoutUtility.GetLastRect();
            Rect savedRect = new Rect(rect);

            if (obj == selectedGameObject)
            {
                Rect rect1 = new Rect(rect);
                rect1.height = 20;
                //rect1.y += 2;
                EditorGUI.DrawRect(rect1, colorSelected);
            }
            if (selectedGameObject == obj)
            {
                reachedTarget = true;
            }
            else if (selectedGameObject.transform.parent == null)
            {
                reachedTarget = true;
            }
            Rect clickRect = new Rect(rect);
            bool hovered = false;
            bool partiallyHovered = false;
            if (clickRect.Contains(Event.current.mousePosition))
            {
                partiallyHovered = true;
                if (Event.current.mousePosition.x > indentLevel * indentSize + 16)
                {
                    hovered = true;
                    if (Event.current.type == EventType.MouseDown)
                    {
                        Event.current.Use();
                        if (Event.current.button == 0)
                        {
                            owner.SetTargetGameObject(obj);
                            owner.SetScrollPosition();
                            Close();
                        }
                        else if (Event.current.button == 1)
                        {
                            ShowContextMenu(obj);
                        }
                        else if (Event.current.button == 2)
                        {
                            owner.AddTabNext();
                            owner.SetTargetGameObject(obj);
                            Close();
                        }
                    }
                }
            }
            if (hovered || obj == selectedGameObject)
            {
                _labelStyle.normal.textColor = Color.white;
            }
            else
            {
                _labelStyle.normal.textColor = GUI.skin.label.normal.textColor;
            }
            GUIContent content1 = new GUIContent(obj.name);
            _labelStyle.padding.left = 14 + (indentLevel * indentSize);
            _labelStyle.hover.textColor = _labelStyle.normal.textColor;
            float width = _labelStyle.CalcSize(content1).x + 25;
            if (width > maxWidth)
            {
                maxWidth = width;
            }
            rect.width = width;
            rect.x += 2;
            rect.y -= 2;
            GUILayoutUtility.GetRect(width, 0);
            GUI.Label(rect, content.image, _labelStyle);
            rect.x += 15;
            GUI.Label(rect, content.text, _labelStyle);
            colorGrid = !colorGrid;
            DrawTreeLines(savedRect, obj, indentLevel, hasChildren, partiallyHovered);
            int childCount = obj.transform.childCount;
            if (isExpanded)
            {
                int lastIndex = obj.transform.childCount - 1;
                if (hasChildren)
                {
                    pendingLines.Add(obj.transform.GetChild(lastIndex).gameObject, indentLevel + 1);
                }
                for (int i = 0; i < obj.transform.childCount; i++)
                {
                    Transform child = obj.transform.GetChild(i);
                    DrawGameObject(child.gameObject, indentLevel + 1, lastIndex - i);
                }
            }
        }

        void DrawTreeLines(Rect originalRect, GameObject obj, int indentLevel, bool hasChildren, bool partiallyHovered)
        {
            if (indentLevel > 0)
            {
                Rect rect = new Rect(originalRect);
                rect.height = 1;
                rect.width = 7;
                rect.y += 7;
                rect.x = indentLevel * indentSize + 1;
                Color _lineColor = lineColor;
                if (partiallyHovered || obj == selectedGameObject)
                {
                    _lineColor = Color.white * 0.65f;
                }
                EditorUtils.DrawLineUnderRect(rect, _lineColor);
                if (!hasChildren)
                {
                    rect.x += 9;
                    rect.width = 7;
                    EditorUtils.DrawLineUnderRect(rect, _lineColor);
                }


                foreach (var item in pendingLines)
                {
                    rect = new Rect(originalRect);

                    rect.x = item.Value * indentSize;
                    rect.width = 1;

                    if (item.Key == obj)
                    {
                        rect.height = 8;
                    }
                    else
                    {
                        // rect.y += 3;
                        rect.height = 18;
                    }
                    Rect rect2 = new Rect(rect);
                    rect.width = 7;
                    rect.x -= 2;
                    EditorGUIUtility.AddCursorRect(rect, MouseCursor.Link);
                    {
                        if (rect.Contains(Event.current.mousePosition))
                        {
                            hoveredIndent = item.Value;
                            hoveringIndent = true;
                            if (Event.current.type == EventType.MouseDown)
                            {
                                expandedObjects[item.Key.transform.parent.gameObject] = false;
                                GUI.changed = true;
                            }
                        }
                    }
                    EditorGUI.DrawRect(rect2, _lineColor);

                }
                if (pendingLines.ContainsKey(obj))
                {
                    pendingLines.Remove(obj);
                }
            }
        }

        void ExpandPath(GameObject gameObject)
        {
            Transform current = gameObject.transform;
            while (current != null)
            {
                expandedObjects[current.gameObject] = true;
                current = current.parent;
            }
        }
    }
}