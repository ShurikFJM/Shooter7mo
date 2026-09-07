using System.Collections.Generic;
using UnityEngine.SceneManagement;
using UnityEngine;
using System.Reflection;
using UnityObject = UnityEngine.Object;
using System;
using System.Text;
using System.Linq;
using UnityEditor;
using UnityEngine.UI;
using UnityEngine.Video;
using UnityEngine.Audio;
using UnityEngine.UIElements;
using System.Runtime.InteropServices;
using UnityEditor.UIElements;
using UnityEditor.SceneManagement;


namespace CoInspector
{
    internal static class EditorUtils
    {
#if UNITY_EDITOR_WIN
        [DllImport("user32.dll")]
        private static extern bool SetCursorPos(int X, int Y);
#elif UNITY_EDITOR_OSX
        [DllImport("/System/Library/Frameworks/ApplicationServices.framework/ApplicationServices")]
        private static extern void CGWarpMouseCursorPosition(Vector2 newPosition);
#endif

        public static void MoveCursor(int x, int y)
        {
#if UNITY_EDITOR_WIN
            SetCursorPos(x, y);
#elif UNITY_EDITOR_OSX
            CGWarpMouseCursorPosition(new Vector2(x, y));
#endif
        }

        internal static bool IsLightSkin()
        {
            return !EditorGUIUtility.isProSkin;
        }
        internal static bool EditorScaleIsOne()
        {
            return EditorGUIUtility.pixelsPerPoint == 1;
        }

        internal static bool NotDragging()
        {
            return DragAndDrop.objectReferences == null || DragAndDrop.objectReferences.Length == 0;
        }
        internal static bool AreWeDragging()
        {
            return !NotDragging();
        }
        internal static bool AreWeDraggingThis(Component component)
        {
            if (AreWeDragging() && DragAndDrop.objectReferences.Length == 1)
            {
                return DragAndDrop.objectReferences[0] == component;
            }
            return false;
        }
        private static readonly string[] InvalidEditorToolTypeNames = new string[]
        {
            "UnityEngine.ParticleSystem",
           // "UnityEngine.ArticulationBody",
            "UnityEngine.LightAnchor",
            "UnityEngine.EdgeCollider2D",
            "UnityEngine.Rendering.Universal.Light2D",
            "UnityEngine.Rendering.Universal.ShadowCaster2D"
        };
        private static readonly string[] OverrideWithComponentTypeName = new string[]
        {
            "VF.Model.VRCFury"
        };
        private static readonly string[] ProblematicScrollComponentNames = new string[]
{
            "VF.Model.VRCFury"
};

        private static readonly string[] ImportOverridesInspector = new string[]
       {
            "UnityEditor.U2D.SpriteAtlasInspector"
       };

        internal static bool ImportEditorOverridesInspector(Editor editor)
        {
            if (ImportOverridesInspector.Contains(editor.GetType().ToString()))
            {
                return true;
            }
            return false;
        }

        internal static bool HasProblematicScrollComponent(GameObject gameObject)
        {
            if (!gameObject)
            {
                return false;
            }
            Component[] components = gameObject.GetComponents<Component>();
            if (components == null || components.Length == 0)
            {
                return false;
            }
            foreach (var comp in components)
            {
                if (ProblematicScrollComponentNames.Contains(comp.GetType().ToString()))
                {
                    return true;
                }
            }
            return false;
        }
        internal static bool HaveProblematicScrollComponent(GameObject[] gameObjects)
        {
            if (gameObjects == null || gameObjects.Length == 0)
            {
                return false;
            }
            foreach (var go in gameObjects)
            {
                bool res = HasProblematicScrollComponent(go);
                if (res)
                {
                    return true;
                }
            }
            return false;
        }

        internal static VisualElement OverrideComponentName(Component component, VisualElement componentBar, VisualElement componentBox, VisualElement componentInspector)
        {
            if (!component || componentBar == null)
            {
                return null;
            }
            if (OverrideWithComponentTypeName.Contains(component.GetType().ToString()))
            {

                FieldInfo contentField = component.GetType().GetField("content", BindingFlags.Public | BindingFlags.Instance);
                if (contentField != null)
                {
                    object contentValue = contentField.GetValue(component);
                    if (contentValue != null)
                    {
                        VisualElement overlayTitle = new VisualElement();
                        overlayTitle.style.position = Position.Absolute;
                        overlayTitle.style.top = 2;
                        overlayTitle.style.left = 40;
                        overlayTitle.style.width = componentBar.style.width;
                        overlayTitle.style.height = componentBar.style.height;

                        VisualElement horizontalContainer = new VisualElement();
                        horizontalContainer.style.flexDirection = FlexDirection.Row;
                        horizontalContainer.style.paddingLeft = 0;
                        horizontalContainer.style.paddingTop = 0;
                        Label vrcfuryLabel = new Label("VRCFury");
                        vrcfuryLabel.style.height = 20;
                        vrcfuryLabel.style.unityFontStyleAndWeight = FontStyle.BoldAndItalic;
                        vrcfuryLabel.style.color = new Color(0.9f, 0.4f, 0f);
                        vrcfuryLabel.style.backgroundColor = new Color(0.1f, 0.1f, 0.1f);
                        vrcfuryLabel.style.marginTop = 0;
                        vrcfuryLabel.style.paddingTop = 3;
                        vrcfuryLabel.style.paddingLeft = 7;
                        vrcfuryLabel.style.paddingRight = 7;

                        Label contentLabel = new Label(ObjectNames.NicifyVariableName(contentValue.GetType().Name));
                        contentLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
                        contentLabel.style.paddingLeft = 8;
                        contentLabel.style.paddingRight = 8;
                        contentLabel.style.paddingTop = -1;
                        contentLabel.style.height = 19;
                        contentLabel.style.unityTextAlign = TextAnchor.MiddleLeft;
                        contentLabel.style.backgroundColor = new Color(0.245f, 0.245f, 0.245f);
                        componentBar.RegisterCallback<MouseEnterEvent>((evt) =>
                        {
                            contentLabel.style.backgroundColor = new Color(0.28f, 0.28f, 0.28f);
                        });

                        componentBar.RegisterCallback<MouseLeaveEvent>((evt) =>
                        {
                            contentLabel.style.backgroundColor = new Color(0.245f, 0.245f, 0.245f);
                        });

                        contentLabel.style.minWidth = 65;

                        horizontalContainer.Add(vrcfuryLabel);
                        horizontalContainer.Add(contentLabel);
                        overlayTitle.Add(horizontalContainer);
                        horizontalContainer.pickingMode = PickingMode.Ignore;
                        overlayTitle.pickingMode = PickingMode.Ignore;
                        contentLabel.pickingMode = PickingMode.Ignore;
                        vrcfuryLabel.pickingMode = PickingMode.Ignore;

                        componentBox.Add(overlayTitle);
                        componentInspector.style.paddingBottom = 7;
                        VisualElement header = componentInspector.HideFirstChildOfClass(".vrcfHeader");

                        if (header == null)
                        {
                            componentBox.ReplaceCallback<GeometryChangedEvent>((evt) =>
                            {

                                if (header == null)
                                {
                                    header = componentInspector.GetFirstChildOfClass(".vrcfHeader");
                                    if (header != null)
                                    {
                                        GetVRCFuryLabel(header, contentLabel);
                                    }
                                }
                            });
                        }
                        else
                        {
                            GetVRCFuryLabel(header, contentLabel);
                        }
                    }
                }
            }
            return null;
        }

        private static void GetVRCFuryLabel(VisualElement header, Label contentLabel)
        {
            SetElementVisible(header, false);
            header.TryUntilDone(() =>
            {
                var label = header.Q<Label>();
                if (label != null)
                {
                    contentLabel.text = label.text;
                    return true;
                }
                return false;
            });
        }

        private static readonly string[] InvisibleComponentTypeNames = new string[]
        {
            "UnityEngine.ParticleSystemRenderer",
        };
#if UNITY_6000_4_OR_NEWER
        internal static EntityId GetObjectId(UnityObject obj)
        {
            return obj.GetEntityId();
        }

#else
        internal static int GetObjectId(UnityObject obj)
        {
            return obj.GetInstanceID();
        }
#endif

#if UNITY_6000_4_OR_NEWER
        internal static UnityObject IdToObject(EntityId id)
        {
            return EditorUtility.EntityIdToObject(id);

        }

#else
        internal static UnityObject IdToObject(int id)
        {
#pragma warning disable CS0618
            return EditorUtility.InstanceIDToObject(id);
#pragma warning restore CS0618

        }
#endif


        public static bool IsValidEditorToolType(Component component)
        {
            string componentTypeName = component.GetType().FullName;
            return !InvalidEditorToolTypeNames.Contains(componentTypeName);
        }
        public static bool IsInvisibleComponent(Component component, bool debug)
        {
            if (Reflected.IsComponentHidden(component, debug))
            {
                return true;
            }
            string componentTypeName = component.GetType().FullName;
            return InvisibleComponentTypeNames.Contains(componentTypeName);
        }
        internal static bool IsBuiltInComponent(Component component)
        {
            if (component == null)
                return false;

            string namespaceName = component.GetType().Namespace;
            return !string.IsNullOrEmpty(namespaceName) && namespaceName.StartsWith("UnityEngine");
        }
        internal static bool HasVisibleFields(Editor editor)
        {
            if (editor == null || editor.serializedObject == null)
            {
                return false;
            }
            if (!CoInspectorWindow.hideEmptyComponents && editor.targets != null && editor.targets.Length >= 1)
            {
                if (editor.targets[0] is Component)
                {
                    bool builtIn = IsBuiltInComponent(editor.targets[0] as Component);
                    if (!builtIn)
                    {
                        return true;
                    }
                }
            }

            SerializedProperty property = editor.serializedObject.GetIterator();

            if (property.NextVisible(true))
            {

                return true;

            }
            return false;
        }

        internal static bool HasAnyVisibleFields(Editor editor)
        {
            if (editor == null || editor.serializedObject == null)
            {
                return false;
            }

            SerializedProperty property = editor.serializedObject.GetIterator();

            while (property.NextVisible(true))
            {
                if (property.name.StartsWith("m_") || property.name == "serializedVersion")
                {
                    continue;
                }
                return true;
            }

            return false;
        }

        internal static void SetElementVisible(VisualElement element, bool visible)
        {
            if (element == null)
            {
                return;
            }
            if (visible)
            {
                element.style.display = DisplayStyle.Flex;
            }
            else
            {
                element.style.display = DisplayStyle.None;
            }
        }
        internal static VisualElement CreatePreviewToolbar(Editor[] previewEditors, TabInfo tab)
        {
            return VisualElementExtensions.CreatePreviewToolbar(previewEditors, tab);
        }
        internal static bool IsScrollbarVisible(ScrollView scrollView)
        {
            return scrollView.verticalScroller.resolvedStyle.display == DisplayStyle.Flex;
        }
        internal static VisualElement HorizontalLine(Color color = default, float thickness = 1)
        {
            return VisualElementExtensions.HorizontalLine(color, thickness);
        }
        internal static void UpdateScrollVisibilityOnce(ScrollView scrollView, int threshold)
        {
            if (scrollView == null)
            {
                return;
            }
            scrollView.UpdateScrollbarVisibility(threshold);
            scrollView.schedule.Execute(() =>
            {
                scrollView.UpdateScrollbarVisibility(threshold);
                scrollView.MarkDirtyRepaint();
            });
            scrollView.MarkDirtyRepaint();
        }


        public static T[] Concat<T>(this T[] array, T item, bool canRepeat = true)
        {
            if (array == null)
            {
                return new T[] { item };
            }
            if (canRepeat && array.Contains(item))
            {
                return array;
            }

            Array.Resize(ref array, array.Length + 1);
            array[array.Length - 1] = item;
            return array;
        }
        public static T[] AddRange<T>(this T[] array, T[] items)
        {
            if (array == null)
            {
                return items?.Clone() as T[] ?? new T[0];
            }

            if (items == null || items.Length == 0)
            {
                return array;
            }

            int originalLength = array.Length;
            Array.Resize(ref array, array.Length + items.Length);
            Array.Copy(items, 0, array, originalLength, items.Length);
            return array;
        }


        public static string SplitCamelCase(string input)
        {
            return System.Text.RegularExpressions.Regex.Replace(input, "([A-Z])", " $1", System.Text.RegularExpressions.RegexOptions.Compiled).Trim();
        }
        internal static void SetElementVisible(VisualElement element, VisualElement element2)
        {
            if (element == null || element2 == null)
            {
                return;
            }
            element.style.display = element2.style.display;
        }

        internal static void SetStaticElement(VisualElement element)
        {
            if (element == null)
            {
                return;
            }
            element.style.flexGrow = 0;
            element.style.flexShrink = 0;
        }


        /*
          internal static string DebugTabInfos(List<TabInfo> tabInfos)
          {
              StringBuilder sb = new StringBuilder();
              for (int i = 0; i < tabInfos.Count; i++)
              {
                  sb.AppendLine($"Tab {i + 1}:");
                  sb.AppendLine(DebugSingleTabInfo(tabInfos[i]));
                  sb.AppendLine();
              }
              return sb.ToString();
          } */

        internal static string DebugSingleTabInfo(TabInfo tabInfo)
        {
            StringBuilder sb = new StringBuilder();

            sb.AppendLine($"  Name: {tabInfo.name}");
            sb.AppendLine($"  Total ComponentMaps: {tabInfo.componentMaps.Count}");
            sb.AppendLine($"  Foldouts open: {tabInfo.componentMaps.Count(cm => cm.foldout)}");

            foreach (var componentMap in tabInfo.componentMaps)
            {
                string componentName = componentMap.component != null ? componentMap.component.GetType().Name : "Null";
                sb.AppendLine($"    {componentName} ({(componentMap.foldout ? "open" : "closed")})");
            }

            return sb.ToString();
        }
        internal static bool CheckEditorsChanged(Editor[] editors)
        {
            if (editors == null)
            {
                return false;
            }
            foreach (var editor in editors)
            {
                if (editor && CheckEditorChanged(editor))
                {
                    return true;
                }
            }
            return false;
        }
        internal static bool CheckEditorChanged(Editor editor)
        {
            if (editor == null)
            {
                return false;
            }
            if (editor.target == null || editor.serializedObject == null)
            {
                UnityEngine.Object.DestroyImmediate(editor, true);
                return false;
            }
            if (editor.serializedObject != null)
            {
                if (editor.serializedObject.UpdateIfRequiredOrScript())
                {
                    editor.serializedObject.ApplyModifiedProperties();
                    if (CoInspectorWindow.MainCoInspector)
                    {
                        CoInspectorWindow.MainCoInspector.Repaint();
                    }
                    return true;
                }
            }
            return false;
        }
        internal static bool AreRectsOverlapping(Rect rect1, Rect rect2)
        {
            if (rect1 == Rect.zero || rect2 == Rect.zero)
            {
                return false;
            }
            if (rect1.x < rect2.x + rect2.width && rect1.x + rect1.width > rect2.x)
            {
                return true;
            }
            return false;
        }
        internal static bool IsCtrlHeld()
        {
            if (Event.current == null)
            {
                return false;
            }
            if (Application.platform == RuntimePlatform.OSXEditor)
            {
                if (Event.current.command)
                {
                    return true;
                }
            }
            else
            {
                if (Event.current.control)
                {
                    return true;
                }
            }

            return false;
        }

        internal static bool HaveBoolArraysChanged(bool[] array1, bool[] array2)
        {
            if (array1.Length != array2.Length)
            {
                return true;
            }
            for (int i = 0; i < array1.Length; i++)
            {
                if (array1[i] != array2[i])
                {
                    return true;
                }
            }
            return false;
        }

        internal static bool CompareArrays(GameObject[] a, GameObject[] b)
        {
            if (a == null || b == null)
            {
                return false;
            }
            if (a.Length != b.Length)
            {
                return false;
            }
            for (int i = 0; i < a.Length; i++)
            {
                if (a[i] != b[i])
                {
                    return false;
                }
            }
            return true;
        }
        internal static bool ContainsArray(List<GameObject[]> list, GameObject[] array)
        {
            if (list == null || array == null)
            {
                return false;
            }

            foreach (GameObject[] _array in list)
            {
                if (CompareArrays(_array, array))
                {
                    return true;
                }
            }
            return false;
        }

        public static bool SameRemovedComponents(IList<UnityEditor.SceneManagement.RemovedComponent> list1, IList<UnityEditor.SceneManagement.RemovedComponent> list2)
        {

            if (ReferenceEquals(list1, list2))
            {
                return true;
            }

            if (list1 == null || list2 == null)
            {
                return false;
            }

            if (list1.Count != list2.Count)
            {
                return false;
            }

            if (list1.Count == 0)
            {
                return true;
            }

            int count = list1.Count;

            for (int i = 0; i < count; i++)
            {
                if (!ReferenceEquals(list1[i].assetComponent, list2[i].assetComponent))
                {
                    return false;
                }
            }

            return true;
        }
        internal static GameObject LoadGameObject(string combinedPath, bool prefabMode = false)
        {
            if (string.IsNullOrEmpty(combinedPath))
            {
                return null;
            }
            string sceneGUID = "";
            string path = "";
            Scene scene = SceneManager.GetActiveScene();


            string[] sections = combinedPath.Split(',');
            if (sections.Length < 2)
            {
                path = combinedPath;
                //return null;
            }
            else
            {
                string guidSection = sections[0].Split(':')[1].Trim().Replace("\"", "");
                string pathSection = sections[1].Split(':')[1].Trim().Replace("\"", "").Replace("}", "");
                sceneGUID = guidSection;
                path = pathSection;
            }
            if (sceneGUID != "")
            {
                string scenePath = AssetDatabase.GUIDToAssetPath(sceneGUID);
                if (string.IsNullOrEmpty(scenePath))
                {
                    // Debug.LogWarning($"Scene with GUID '{sceneGUID}' not found.");
                    return null;
                }
                scene = SceneManager.GetSceneByPath(scenePath);
            }

            if (!scene.IsValid() || !scene.isLoaded)
            {
                //Debug.LogWarning($"Scene is not valid or not loaded. Scene: {scenePath} ({sceneGUID})");
                return null;
            }

            Transform root = null;
            if (prefabMode)
            {
                root = CoInspectorWindow.GetPrefabStageRoot()?.transform;
                if (root == null)
                {
                    return null;
                }
                string rootName = "Prefab Mode in Context[0]/";
                if (path.Contains(rootName))
                {
                    path = path.Replace(rootName, "");
                }
            }

            string[] pathComponents = path.Split('/');
            if (pathComponents.Length == 0)
            {
                pathComponents = new string[] { path };
            }

            Transform current = root;

            for (int i = 0; i < pathComponents.Length; i++)
            {
                string component = pathComponents[i];
                if (string.IsNullOrEmpty(component))
                {
                    continue;
                }

                string name = component;
                int index = 0;

                int indexStart = component.LastIndexOf('[');
                if (indexStart >= 0)
                {
                    int indexEnd = component.LastIndexOf(']');
                    if (indexEnd > indexStart)
                    {
                        string indexStr = component.Substring(indexStart + 1, indexEnd - indexStart - 1);
                        if (int.TryParse(indexStr, out int parsedIndex))
                        {
                            name = component.Substring(0, indexStart);
                            index = parsedIndex;
                        }
                    }
                }

                if (prefabMode)
                {
                    if (i == 0)
                    {
                        if (name != root.name)
                        {
                            return null;
                        }
                        if (pathComponents.Length == 1)
                        {
                            return root.gameObject;
                        }
                    }
                    else
                    {
                        current = FindChildByIndex(current, name, index);
                    }
                }
                else
                {
                    if (current == null)
                    {
                        current = FindRootByIndex(name, index, scene);
                    }
                    else
                    {
                        current = FindChildByIndex(current, name, index);
                    }
                }

                if (current == null)
                {
                    return null;
                }
            }
            return current?.gameObject;
        }

        internal static Transform FindChildByIndex(Transform parent, string name, int index)
        {
            int currentIndex = 0;
            foreach (Transform child in parent)
            {
                if (child.name == name)
                {
                    if (currentIndex == index)
                    {
                        return child;
                    }
                    currentIndex++;
                }
            }
            return null;
        }

        internal static Transform FindRootByIndex(string name, int index, Scene scene)
        {
            int currentIndex = 0;
            foreach (GameObject rootObj in scene.GetRootGameObjects())
            {
                if (rootObj.name == name)
                {
                    if (currentIndex == index)
                    {
                        return rootObj.transform;
                    }
                    currentIndex++;
                }
            }
            return null;
        }

        internal static bool IsAPrefabAsset(GameObject gameObject)
        {
            if (gameObject == null)
            {
                return false;
            }
            if (AssetDatabase.GetAssetPath(gameObject) == "")
            {
                return false;
            }
            PrefabAssetType prefabAssetType = PrefabUtility.GetPrefabAssetType(gameObject);
            return prefabAssetType != PrefabAssetType.NotAPrefab;
        }

        internal static bool IsAnyAPrefabAsset(GameObject[] gameObjects)
        {
            foreach (var gameObject in gameObjects)
            {
                if (IsAPrefabAsset(gameObject))
                {
                    return true;
                }
            }
            return false;
        }

        internal static void AddIfNotPresent<T>(this List<T> list, T item)
        {
            if (item == null || list == null)
            {
                return;
            }
            if (!list.Contains(item))
            {
                list.Add(item);
            }
        }
        internal static void RemoveIfPresent<T>(this List<T> list, T item)
        {
            if (item == null || list == null)
            {
                return;
            }
            if (list.Contains(item))
            {
                list.Remove(item);
            }
        }

        internal static bool _AreAllPrefabs(GameObject[] gameObjects)
        {
            if (gameObjects == null)
            {
                return false;
            }
            foreach (var gameObject in gameObjects)
            {
                if (!PrefabUtility.IsAnyPrefabInstanceRoot(gameObject))
                {
                    return false;
                }
            }
            return true;
        }

        internal static (GameObject instance, string path, UnityObject prefabObject) GetPrefabReferences(GameObject item)
        {
            if (item == null)
            {
                return (null, null, null);
            }

            GameObject instanceRoot = PrefabUtility.GetNearestPrefabInstanceRoot(item);
            if (instanceRoot == null)
            {
                return (null, null, null);
            }

            string path = PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(instanceRoot);
            if (string.IsNullOrEmpty(path))
            {

                return (instanceRoot, null, null);
            }
            return (instanceRoot, path, AssetDatabase.LoadAssetAtPath<UnityObject>(path));
        }
        internal static List<string[]> HistoryPathsToList(List<HistoryPaths> history)
        {
            if (history == null)
            {
                return null;
            }
            List<string[]> historyList = new List<string[]>();
            foreach (var paths in history)
            {
                historyList.Add(paths.paths);
            }
            return historyList;
        }
        internal static string GatherGameObjectPath(GameObject obj)
        {
            return GatherGameObjectPath(obj, null);
        }

        internal static string GatherGameObjectPath(GameObject obj, bool isPrefabAsset)
        {
            return GatherGameObjectPath(obj, null, isPrefabAsset);
        }

        internal static string GatherGameObjectPath(GameObject obj, GameObject relativeRoot = null, bool isPrefabAsset = false)
        {
            if (obj == null)
            {
                return "";
            }

            List<string> pathComponents = new List<string>();
            Transform current = obj.transform;
            bool breakNext = false;
            while (current != null)
            {
                string name = current.name;
                Transform parent = current.parent;

                if (parent != null)
                {
                    int siblingIndex = GetSiblingIndex(parent, current);
                    name += $"[{siblingIndex}]";
                }
                else
                {
                    int rootSiblingIndex = GetRootSiblingIndex(current, isPrefabAsset);
                    name += $"[{rootSiblingIndex}]";
                }
                pathComponents.Insert(0, name);
                current = parent;
                if (breakNext)
                {
                    current = null;
                }
                if (relativeRoot)
                {

                    if (parent == relativeRoot.transform)
                    {
                        breakNext = true;
                    }
                }
            }
            string sceneGUID = "";

            if (!relativeRoot && obj.scene != null && obj.scene.path != null && obj.scene.path != "")
            {
                sceneGUID = AssetDatabase.AssetPathToGUID(obj.scene.path);
            }
            string gameObjectPath = string.Join("/", pathComponents);
            return "{" + $"\"sceneGUID\":\"{sceneGUID}\",\"path\":\"{gameObjectPath}\"" + "}";
        }

        internal static int GetSiblingIndex(Transform parent, Transform child)
        {
            int index = 0;
            foreach (Transform sibling in parent)
            {
                if (sibling == child)
                {
                    return index;
                }
                if (sibling.name == child.name)
                {
                    index++;
                }
            }
            return index;
        }

        internal static int GetRootSiblingIndex(Transform current, bool isPrefabAsset = false)
        {
            if (isPrefabAsset)
            {
                return 0;
            }
            int index = 0;
            foreach (GameObject rootObj in current.gameObject.scene.GetRootGameObjects())
            {
                if (rootObj.transform == current)
                {
                    return index;
                }
                if (rootObj.name == current.name)
                {
                    index++;
                }
            }
            return index;
        }
#if UNITY_6000_4_OR_NEWER
        internal static GameObject LoadSingleTabGameObject(string path, EntityId id, bool prefab)
        {
            if (string.IsNullOrEmpty(path))
            {
                return null;
            }
            GameObject go = null;
            if (id.IsValid())
            {
                go = IdToObject(id) as GameObject;
                //string _goPath = GatherGameObjectPath(go);
                if (go != null && path.Contains(go.name))
                {
                    return go;
                }
            }
            go = LoadGameObject(path, prefab);
            return go;
        }
#else
        internal static GameObject LoadSingleTabGameObject(string path, int id, bool prefab)
        {
            if (string.IsNullOrEmpty(path))
            {
                return null;
            }
            GameObject go = null;
            if (id != 0)
            {
                go = IdToObject(id) as GameObject;
                //string _goPath = GatherGameObjectPath(go);
                if (go != null && path.Contains(go.name))
                {
                    return go;
                }
            }
            go = LoadGameObject(path, prefab);
            return go;
        }
#endif


        internal static GameObject LoadTabGameObject(TabInfo tab)
        {
            if (tab == null || string.IsNullOrEmpty(tab.path))
            {
                return null;
            }
            return LoadSingleTabGameObject(tab.path, tab.id, tab.prefab);
        }

        internal static GameObject[] LoadTabGameObjects(TabInfo tab)
        {
            if (tab == null)
            {
                return null;
            }
            if (tab.paths != null || (tab.paths != null && tab.ids != null && tab.paths.Length == tab.ids.Length))
            {
                List<GameObject> gameObjects = new List<GameObject>();
                if (tab.ids == null)
                {
#if UNITY_6000_4_OR_NEWER
                    tab.ids = new EntityId[tab.paths.Length];

#else
                    tab.ids = new int[tab.paths.Length];

#endif
                }
                for (int i = 0; i < tab.paths.Length; i++)
                {
                    GameObject go = LoadSingleTabGameObject(tab.paths[i], tab.ids[i], tab.prefab);
                    if (go != null)
                    {
                        gameObjects.Add(go);
                    }
                }
                return gameObjects.Count > 0 ? gameObjects.ToArray() : null;
            }
            return null;
        }

        internal static void TabPathsToGameObjects(TabInfo tab)
        {
            if (tab == null)
            {
                return;
            }
            if (!string.IsNullOrEmpty(tab.path))
            {
                tab.target = LoadTabGameObject(tab);
            }
            if (tab.target == null && tab.paths != null && tab.paths.Length > 0)
            {
                tab.targets = LoadTabGameObjects(tab);
            }
            if (tab.historyPaths != null)
            {
                tab.history = GetHistory(tab);
            }
        }
        internal static void ShowPasteFailedMessage()
        {
            EditorUtility.DisplayDialog("Operation Failed", "This Type cannot be added twice to the same GameObject!", "Ah, OK");
        }

        internal static void ShowSceneToolsMessage()
        {
            if (CoInspectorWindow.MainCoInspector && CoInspectorWindow.MainCoInspector.settingsData)
            {
                if (CoInspectorWindow.showSceneToolsMessage)
                {
                    EditorUtility.DisplayDialog("Scene Tools Override", "When CoInspector Scene Tools are active, Move, Rotate, Scale and Transform Tool will try to target your Active Tab.\n\nThis is an experimental feature, so it may not be perfect at times.", "Got it!");
                    CoInspectorWindow.showSceneToolsMessage = false;
                    CoInspectorWindow.MainCoInspector.settingsData.showSceneToolsMessage = false;
                    CoInspectorWindow.MainCoInspector.SaveSettings();
                }
            }
        }
        internal static void ShowPrefabFailedMessage(GameObject targetPrefab = null)
        {
            if (targetPrefab == null)
            {
                EditorUtility.DisplayDialog("Cannot restructure Prefab instance", "Children of a Prefab instance cannot be deleted or moved, and components cannot be reordered.\n\nYou can open the Prefab in Prefab Mode to restructure the Prefab Asset itself, or unpack the Prefab instance to remove its Prefab connection. ", "Ah, OK");
            }
            else
            {
                if (EditorUtility.DisplayDialog("Cannot restructure Prefab instance", "Children of a Prefab instance cannot be deleted or moved, and components cannot be reordered.\n\nYou can open the Prefab in Prefab Mode to restructure the Prefab Asset itself, or unpack the Prefab instance to remove its Prefab connection. ", "Open Prefab", "Ah, OK"))
                {
                    string path = AssetDatabase.GetAssetPath(targetPrefab);
                    if (!string.IsNullOrEmpty(path))
                    {
                        CoInspectorWindow.DoOpenPrefab(path);
                    }
                }
            }

        }
#if UNITY_2021_2_OR_NEWER

        internal static void OpenGameObjectInPrefabIsolation(GameObject target)
        {
            EnterPrefabModeFromGameObject(target, PrefabStage.Mode.InIsolation);
        }

        internal static void OpenGameObjectInPrefabContext(GameObject target)
        {
            EnterPrefabModeFromGameObject(target, PrefabStage.Mode.InContext);
        }


        internal static void EnterPrefabModeFromGameObject(GameObject target, PrefabStage.Mode preferredMode)
        {
            if (target == null || !PrefabUtility.IsPartOfAnyPrefab(target))
            {
                return;
            }
            string prefabAssetPathOfNearestInstanceRoot = PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(target);
            if (!string.IsNullOrEmpty(prefabAssetPathOfNearestInstanceRoot) && prefabAssetPathOfNearestInstanceRoot.EndsWith(".prefab", StringComparison.OrdinalIgnoreCase))
            {
                PrefabStage.Mode prefabStageMode = preferredMode;
                GameObject gameObject = null;
                if (preferredMode == PrefabStage.Mode.InContext)
                {
                    gameObject = ((!EditorUtility.IsPersistent(target)) ? target : null);
                    prefabStageMode = ((gameObject != null) ? PrefabStage.Mode.InContext : PrefabStage.Mode.InIsolation);
                }
                PrefabStageUtility.OpenPrefab(prefabAssetPathOfNearestInstanceRoot, target, prefabStageMode);
                //TargetRelativeGameObjectInPrefabMode(target);
            }
        }
#endif
        internal static GameObject GetRelativeGameObjectInPrefabMode(GameObject sceneTarget)
        {
            if (sceneTarget == null)
            {
                return null;
            }

            string openPrefabPath = PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(sceneTarget);
            if (string.IsNullOrEmpty(openPrefabPath))
            {
                return null;
            }

            GameObject assetTarget = PrefabUtility.GetCorrespondingObjectFromSourceAtPath(sceneTarget, openPrefabPath);
            GameObject assetRoot = AssetDatabase.LoadAssetAtPath<GameObject>(openPrefabPath);

            if (assetTarget != null && assetRoot != null)
            {
                string relativePath = EditorUtils.GatherGameObjectPath(assetTarget, assetRoot, true);
                GameObject relativeTarget = LoadGameObject(relativePath, true);
                if (relativeTarget)
                {
                    return relativeTarget;
                }
            }

            return null;
        }

        internal static GameObject TargetRelativeGameObjectInPrefabMode(GameObject sceneTarget)
        {
            GameObject relativeTarget = GetRelativeGameObjectInPrefabMode(sceneTarget);
            if (relativeTarget)
            {
                EditorApplication.delayCall += () =>
                {
                    Selection.activeGameObject = relativeTarget;
                    EditorGUIUtility.PingObject(relativeTarget);
                };
                //CoInspectorWindow.MainCoInspector?.SetTargetGameObject(relativeTarget);
            }
            return relativeTarget;
        }
        internal static void TargetGameObject(GameObject sceneTarget)
        {
            if (sceneTarget)
            {
                EditorApplication.delayCall += () =>
                {
                    Selection.activeGameObject = sceneTarget;
                    EditorGUIUtility.PingObject(sceneTarget);
                };
            }
        }
        internal static void ShowPasteFailedMessageError()
        {
            EditorUtility.DisplayDialog("Operation Failed", "This component cannot be added to the target GameObject.", "OK");
        }
        internal static void MultiEditFooter(bool differentComponents, VisualElement root, bool prefab = false)
        {
            if (!differentComponents)
            {
                return;
            }
            if (!prefab)
            {
                EditorGUILayout.Space(1);
            }
            else
            {
                EditorGUILayout.Space(0);
            }
            CustomGUIStyles.InfoLabel("<i>Only components present on <b>all selected objects</b> can be multi-edited.</i>", root);
            Rect rect = GetLastLineRect();
            if (prefab)
            {
                rect.x = 0;
                rect.width = root.layout.width;
                EditorGUILayout.Space(0);
            }

        }
        internal static bool CanDrawHeader()
        {
            bool unityVersionChecks = true;
#if !UNITY_2022_1_OR_NEWER
            unityVersionChecks = !CoInspectorWindow.ueePresent;
#endif
            return unityVersionChecks;
        }
        internal static void HeaderMessage()

        {
            EditorGUILayout.Space(5);
            CustomGUIStyles.HelpBox("You're using <b>Unity 2021</b> and <b>Ultimate Editor Enhancer</b>. CoInspector <i>(and any other tool)</i> cannot display Headers correctly with this setup.\n\nPlease, uninstall <b>Ultimate Editor Enhancer</b> or update to <b>Unity 2022</b>.");
            EditorGUILayout.Space(10);
        }
        internal static void NoResultsLabel(VisualElement root)
        {

            EditorGUILayout.Space(1);
            EditorGUILayout.BeginHorizontal();
            GUIStyle wrapStyle = CustomGUIStyles.WrapLabelStyle;
            int indent = EditorGUI.indentLevel;
            EditorGUI.indentLevel = 2;
            EditorGUILayout.LabelField("<i>No matches <b>found!</b></i>", wrapStyle);
            EditorGUI.indentLevel = indent;
            EditorGUILayout.EndHorizontal();
            Rect rect = GetLastLineRect();
            rect.width = root.layout.width;
            EditorUtils.DrawLineOverRect(rect, 2);
            EditorUtils.DrawLineOverRect(rect, CustomColors.MediumShadow, 3);
            rect.x += 12;
            GUI.enabled = true;
            GUI.Label(rect, CustomGUIContents.SearchButtonImage);
        }

        internal static bool IsValidAssetType(UnityObject asset, GameObject gameObject, out bool repeatingAsset)
        {
            repeatingAsset = false;
            if (!asset)
            {
                return false;
            }
            if (asset is MonoScript)
            {
                MonoScript monoScript = asset as MonoScript;
                if (!typeof(MonoBehaviour).IsAssignableFrom(monoScript.GetClass()))
                {
                    return false;
                }
            }
            bool isUIObject = IsAnUIObject(gameObject);
            bool hasGraphic = GameObjectHasGraphic(gameObject);
            bool hasRenderer = GameObjectHasRenderer(gameObject);
            bool hasMeshRenderer = GameObjectHasMeshRenderer(gameObject);
            if (asset is Material)
            {
                if (!hasMeshRenderer)
                {
                    return true;
                }
                repeatingAsset = true;
                return false;
            }
            if (asset is MonoScript || asset is AudioClip || asset is AnimationClip || asset is VideoClip || asset is AudioMixerGroup || asset is UnityEditor.Animations.AnimatorController)
            {
                return true;
            }
            if (asset is Texture2D)
            {
                if (isUIObject)
                {
                    repeatingAsset = hasGraphic;
                    return !hasGraphic;
                }
                else
                {
                    repeatingAsset = hasRenderer;
                    return !gameObject.GetComponent<SpriteRenderer>();
                }
            }
            if (asset is Font)
            {
                repeatingAsset = isUIObject && hasGraphic;
                return isUIObject && !hasGraphic;
            }
            if (asset is Sprite)
            {
                repeatingAsset = hasRenderer;
                if (isUIObject)
                {
                    repeatingAsset = hasGraphic;
                }
                return isUIObject ? !hasGraphic : !gameObject.GetComponent<SpriteRenderer>();
            }
            return false;
        }
        internal static int GetAddedComponentIndex(Component[] components, List<ComponentMap> maps)
        {
            for (int i = 0; i < components.Length; i++)
            {
                if (components[i] == null) continue;

                bool foundInMaps = false;
                foreach (ComponentMap map in maps)
                {
                    if (map.component == components[i])
                    {
                        foundInMaps = true;
                        break;
                    }
                }
                if (!foundInMaps)
                {
                    if (i == components.Length - 1)
                    {
                        return -1;
                    }
                    return i;
                }
            }

            return -1;
        }

        internal static bool DraggingAComponent()
        {
            if (DragAndDrop.objectReferences == null || DragAndDrop.objectReferences.Length == 0)
            {
                return false;
            }
            if (DragAndDrop.objectReferences[0] is Component)
            {
                return true;
            }
            return false;
        }
        internal static bool CanAddMultipleTimes(Component T, GameObject target)
        {
            return CanAddMultipleTimes(T.GetType(), target);
        }

        internal static bool CanAddMultipleTimes(Type T, GameObject target)
        {

            if (T == null || target == null)
            {
                return false;
            }
            Type componentType = T;
            object[] attributes = componentType.GetCustomAttributes(typeof(DisallowMultipleComponent), inherit: true);
            if (attributes.Length > 0)
            {
                return false;
            }

            if (typeof(Transform).IsAssignableFrom(componentType))
            {
                return false;
            }

            if (typeof(Renderer).IsAssignableFrom(componentType))
            {
                if (GameObjectHasRenderer(target))
                {
                    return !target.GetComponent(componentType);
                }
            }
            if (typeof(Graphic).IsAssignableFrom(componentType))
            {
                if (GameObjectHasGraphic(target))
                {
                    return false;
                }
            }

            if (typeof(Canvas).IsAssignableFrom(componentType))
            {
                if (target.GetComponent<Canvas>() != null)
                {
                    return false;
                }
            }

            if (typeof(AudioListener).IsAssignableFrom(componentType))
            {
                if (target.GetComponent<AudioListener>() != null)
                {
                    return false;
                }
            }

            return true;
        }

        internal static bool IsAnUIObject(GameObject gameObject)
        {
            if (gameObject == null)
            {
                return false;
            }
            return gameObject.GetComponent<RectTransform>() != null;
        }
        internal static bool GameObjectHasMeshRenderer(GameObject gameObject)
        {
            if (gameObject == null)
            {
                return false;
            }
            return gameObject.GetComponent<MeshRenderer>() != null;
        }
        internal static bool GameObjectHasRenderer(GameObject gameObject)
        {
            if (gameObject == null)
            {
                return false;
            }
            Renderer[] renderers = gameObject.GetComponents<Renderer>();
            foreach (Renderer renderer in renderers)
            {
                if (!(renderer is SkinnedMeshRenderer) || !(renderer is ParticleSystemRenderer))
                {
                    return true;
                }
            }
            return false;
        }
        internal static bool GameObjectHasGraphic(GameObject gameObject)
        {
            if (gameObject == null)
            {
                return false;
            }
            return gameObject.GetComponent<Graphic>() != null;
        }

        internal static void DrawMaterials(MaterialMap materialMap, Component component, bool isPrefab = false)
        {
            if (materialMap.materials != null && materialMap.materials.Count > 0)
            {
                List<Material> materials = materialMap.materials;
                bool debug = false;
                if (CoInspectorWindow.MainCoInspector != null)
                {
                    if (isPrefab)
                    {
                        debug = CoInspectorWindow.MainCoInspector.debugAsset;
                    }
                    else
                    {
                        debug = CoInspectorWindow.MainCoInspector.ActiveTabInDebugMode();
                    }
                }
                if (materialMap != null && materialMap.editors != null && materialMap.editors.Count != 0 && materials.Count == materialMap.editors.Count)
                {
                    for (int i = 0; i < materialMap.editors.Count; i++)
                    {
                        if (materialMap.editors[i] != null)
                        {
                            GUILayout.BeginVertical();
                            if (i == 0)
                            {
                                GUILayout.Space(5);
                            }
                            float currentLabelWidth = EditorGUIUtility.labelWidth;
                            GUILayout.BeginVertical();
                            DoDrawHeader(materialMap.editors[i]);
                            GUILayout.EndVertical();
                            if (debug || UnityEditorInternal.InternalEditorUtility.GetIsInspectorExpanded(materials[i]))
                            {
                                GUI.enabled = !EditorUtils.IsAssetBuiltIn(AssetDatabase.GetAssetPath(materials[i]));
                                GUILayout.BeginVertical(CustomGUIStyles.CollapsedCompStyle);
                                if (!debug)
                                {
                                    materialMap.editors[i].OnInspectorGUI();
                                }
                                else
                                {
                                    materialMap.editors[i].DrawDefaultInspector();
                                }
                                GUILayout.EndVertical();
                            }
                            GUI.enabled = true;
                            EditorGUIUtility.labelWidth = currentLabelWidth;
                            GUILayout.EndVertical();
                            if (i == 0)
                            {
                                EditorUtils.DrawLineOverRect(-5, 1);
                            }
                            else
                            {
                                EditorUtils.DrawLineOverRect(0, 1);
                            }
                        }
                    }
                    GUI.enabled = true;
                }
            }
        }
        static void DoDrawHeader(Editor editor)
        {
            if (CanDrawHeader())
            {
                Reflected.OnHeaderGUI(editor);
                //editor.DrawHeader();
            }
            else
            {
                Reflected.OnHeaderGUI(editor);
                //HeaderMessage();
            }
        }
        internal static void DrawAssetBundleNameGUI(UnityObject[] objects, bool showAssetLabels)
        {
            if (!showAssetLabels)
            {
                return;
            }
            if (objects == null || objects.Length == 0)
            {
                return;
            }

            if (Reflected.GetShowAssetBundleNameMethod() != null)
            {
                if (Reflected.assetBundleNameGUIInstance == null)
                {
                    Reflected.assetBundleNameGUIInstance = Activator.CreateInstance(Reflected.GetAssetBundleNameType());
                }
                if (Reflected.assetBundleNameGUIInstance != null)
                {
                    IEnumerable<UnityEngine.Object> assetsEnumerable = objects.Cast<UnityEngine.Object>();
                    EditorGUILayout.BeginHorizontal();
                    Reflected.GetShowAssetBundleNameMethod().Invoke(Reflected.assetBundleNameGUIInstance, new object[] { assetsEnumerable });
                    EditorGUILayout.EndHorizontal();
                }
                else
                {
                    Debug.LogWarning("AssetBundleNameGUI instance is null");
                }
            }
        }
        internal static void DrawAssetLabelGUI(UnityObject _object, bool showAssetLabels, bool assetsCollapsed, bool assetOnlyMode)
        {
            if (!showAssetLabels)
            {
                return;
            }
            if (_object == null)
            {
                return;
            }
            DrawAssetLabelGUI(new UnityObject[] { _object }, showAssetLabels, assetsCollapsed, assetOnlyMode);
        }
        internal static void DrawAssetLabelGUI(UnityObject[] _objects, bool showAssetLabels, bool assetsCollapsed, bool assetOnlyMode)
        {
            if (!showAssetLabels || assetsCollapsed)
            {
                return;
            }
            if (_objects == null || _objects.Length == 0)
            {
                return;
            }
            bool textAsset = false;
            foreach (var obj in _objects)
            {
                if (obj is TextAsset || obj is Shader)
                {
                    textAsset = true;
                    break;
                }
            }
            if (Reflected.GetShowAssetLabelMethod() != null)
            {
                if (Reflected.labelGUIInstance == null)
                {
                    Reflected.labelGUIInstance = Activator.CreateInstance(Reflected.GetAssetLabelsType());
                }
                if (Reflected.labelGUIInstance != null)
                {
                    if (assetOnlyMode)
                    {
                        GUILayout.FlexibleSpace();
                    }

                    EditorGUILayout.BeginHorizontal(CustomGUIStyles.InspectorSectionStyle);
                    GUILayout.Space(5);
                    EditorGUILayout.BeginVertical();
                    if (textAsset)
                    {
                        EditorGUILayout.LabelField("Asset Labels", CustomGUIStyles.BoldLabel);
                    }

                    Reflected.GetShowAssetLabelMethod().Invoke(Reflected.labelGUIInstance, new object[] { _objects });

                }
            }
            if (!textAsset)
            {
                DrawAssetBundleNameGUI(_objects, showAssetLabels);
            }
            GUILayout.Space(5);
            EditorGUILayout.EndVertical();
            GUILayout.Space(5);
            EditorGUILayout.EndHorizontal();
            EditorUtils.DrawLineOverRect(CustomColors.SimpleShadow, 1);
            EditorUtils.DrawLineOverRect(0);
        }

        internal static bool IsAcceptedMaterialComponent(Component comp)
        {
            if (comp == null)
            {
                return false;
            }
            string typeName = comp.GetType().ToString();
            if (typeName.Contains("UnityEngine."))
            {
                return true;
            }
            if (typeName.Contains("TextMeshProUGUI"))
            {
                return true;
            }
            return false;
        }


        internal static bool IsMaterialComponent(Component comp)
        {
            return comp is Renderer or Graphic or Mask;
        }
        internal static T CloneFrom<T>(object comp, T other) where T : class
        {
            Type type = comp.GetType();
            if (type != other.GetType()) return null;
            BindingFlags flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Default | BindingFlags.DeclaredOnly;
            PropertyInfo[] pInfos = type.GetProperties(flags);
            foreach (var pInfo in pInfos)
            {
                if (pInfo.CanWrite)
                {
                    try
                    {
                        pInfo.SetValue(comp, pInfo.GetValue(other, null), null);
                    }
                    catch { }
                }
            }
            FieldInfo[] fInfos = type.GetFields(flags);
            foreach (var fInfo in fInfos)
            {
                fInfo.SetValue(comp, fInfo.GetValue(other));
            }
            return comp as T;
        }

        internal static List<GameObject[]> GetHistory(TabInfo tab)
        {
            if (tab == null || tab.historyPaths == null)
            {
                return null;
            }
            List<GameObject[]> history = new List<GameObject[]>();

            for (int i = 0; i < tab.historyPaths.Count; i++)
            {
                var paths = tab.historyPaths[i];
                if (paths != null)
                {
                    if (paths.paths != null && (paths.instances == null || paths.instances.Length != paths.paths.Length))
                    {
#if UNITY_6000_4_OR_NEWER
                        paths.instances = new EntityId[paths.paths.Length];

#else
                        paths.instances = new int[paths.paths.Length];

#endif

                    }
                    List<GameObject> gameObjects = new List<GameObject>();
                    for (int j = 0; j < paths.paths.Length; j++)
                    {
                        var go = LoadSingleTabGameObject(paths.paths[j], paths.instances[j], paths.prefab);
                        if (go != null)
                        {
                            gameObjects.Add(go);
                        }
                    }
                    history.Add(gameObjects.ToArray());
                }
            }
            return history;
        }

        internal static List<TabInfo> RebuildTabs(List<TabInfo> tabs, CoInspectorWindow reference, bool rebuild = true)
        {
            List<TabInfo> newTabs = new List<TabInfo>();

            foreach (var tab in tabs)
            {
                if (tab == null /*|| tab.newTab*/)
                {
                    continue;
                }
                RebuildTab(tab, reference, false);
                if (tab != null && tab.IsTabValid())
                {
                    newTabs.Add(tab);
                }
            }
            if (rebuild)
            {
                reference.ReinitializeComponentEditors();
            }
            return newTabs;
        }


        internal static void RebuildTab(TabInfo tab, CoInspectorWindow reference, bool single = true)
        {
            if (tab == null)
            {
                return;
            }
            tab.owner = reference;
            TabPathsToGameObjects(tab);
            tab.AutoSortTargets();
            if (!tab.IsTabValid())
            {
                tab.TrySetValidHistoryTarget();
            }

            if (!tab.newTab && (tab.target == null) && (tab.targets == null || tab.targets.Length == 0))
            {
                return;
            }
            if (tab.multiEditMode && tab.targets != null)
            {
                var validTargets = tab.targets.Where(target => target != null).ToArray();

                if (validTargets.Length > 1)
                {
                    tab.targets = validTargets;
                }
                else if (validTargets.Length == 1)
                {
                    tab.target = validTargets[0];
                    tab.targets = null;
                    tab.multiEditMode = false;
                }
                else
                {
                    return;
                }
            }
            else if (!tab.newTab && (tab.target == null || tab.target != null))
            {
                if (tab.target == null)
                {
                    return;
                }
            }
            if (tab != null)
            {
                tab.owner = reference;
                FixHistory(tab);
                FixComponentMaps(tab);
                if (single)
                {
                    if (tab == reference.GetActiveTab())
                    {
                        reference.ReinitializeComponentEditors();
                    }
                    else
                    {
                        reference.RefreshAllTabNames();
                        reference.RefreshAllIcons();
                        reference.UpdateAllTabPaths();
                    }
                }
            }
        }
        internal static bool AreArraysIdentical(GameObject[] a, GameObject[] b)
        {
            if (ReferenceEquals(a, b))
            {
                return true;
            }
            if (a == null || b == null)
            {
                return false;
            }
            if (a.Length != b.Length)
            {
                return false;
            }

            for (int i = 0; i < a.Length; i++)
            {
                if (a[i] != b[i])
                {
                    return false;
                }
            }
            return true;
        }
        public static bool AreObjectsInSelection(UnityEngine.Object[] objectsToCheck)
        {
            var currentSelection = Selection.objects;
            if (objectsToCheck == null || currentSelection == null || objectsToCheck.Length > currentSelection.Length)
                return false;

            for (int i = 0; i < objectsToCheck.Length; i++)
            {
                var found = false;
                var current = objectsToCheck[i];

                for (int j = 0; j < currentSelection.Length; j++)
                {
                    if (currentSelection[j] == current)
                    {
                        found = true;
                        break;
                    }
                }

                if (!found) return false;
            }
            return true;
        }
        public static bool DoArraysShareContent(GameObject[] selection1, GameObject[] selection2)
        {
            if (ReferenceEquals(selection1, selection2))
            {
                return true;
            }
            if (selection1 == null || selection2 == null)
            {
                return false;
            }
            if (selection1.Length != selection2.Length)
            {
                return false;
            }
            int validCount = selection1.Count(x => x != null);
            if (validCount != selection2.Count(x => x != null))
            {
                return false;
            }
            foreach (var obj1 in selection1)
            {
                if (obj1 == null)
                {
                    continue;
                }
                bool found = false;
                var id1 = GetObjectId(obj1);
                foreach (var obj2 in selection2)
                {
                    if (obj2 == null)
                    {
                        continue;
                    }
                    if (id1 == GetObjectId(obj2))
                    {
                        found = true;
                        break;
                    }
                }
                if (!found)
                {
                    return false;
                }
            }
            return true;
        }

        internal static Editor FindMatchingEditor(Component componentToMatch, Editor[] editorArray)
        {
            if (componentToMatch == null || editorArray == null)
            {
                return null;
            }

            foreach (Editor editor in editorArray)
            {
                if (GetEditorWithTarget(componentToMatch, editor))
                {
                    return editor;
                }
            }

            return null;
        }

        internal static Editor GetEditorWithTarget(Component target, Editor editor)
        {
            if (target == null || editor == null)
            {
                return null;
            }
            if (target == null || editor.target == null)
            {
                return null;
            }
            if (target == editor.target)
            {
                return editor;
            }
            return null;
        }

        internal static void FixComponentMaps(TabInfo tab)
        {
            if (tab == null)
            {
                return;
            }
            if (tab.newTab || tab.componentMaps == null)
            {
                tab.componentMaps = new List<ComponentMap>();
                return;
            }
            Component[] components;
            if (tab.IsValidMultiTarget())
            {
                GameObject lastGameObject = tab.MultiTrackingTarget();
                int indexOfLast = Array.IndexOf(tab.targets, lastGameObject);
                components = tab.targets[indexOfLast].GetComponents<Component>();
            }
            else if (tab.target != null)
            {
                components = tab.target.GetComponents<Component>();
            }
            else
            {
                return;
            }
            if (components == null || components.Length == 0)
            {
                return;
            }
            if (components.Length != tab.componentMaps.Count)
            {
                if (components.Length > tab.componentMaps.Count)
                {
                    for (int i = tab.componentMaps.Count; i < components.Length; i++)
                    {
                        tab.componentMaps.Add(new ComponentMap());
                    }
                }
            }
            for (int i = 0; i < components.Length; i++)
            {
                tab.componentMaps[i].component = components[i];
            }
        }

        internal static void FixHistory(TabInfo tab)
        {
            if (tab == null || tab.history == null)
            {
                return;
            }
            tab.FixNulls();
        }

        internal static bool SceneExists(string sceneGUID)
        {
            return !string.IsNullOrEmpty(AssetDatabase.GUIDToAssetPath(sceneGUID));
        }
        internal static bool ContainsMask(Component component)
        {
            if (component == null)
            {
                return false;
            }
            return ContainsMask(component.gameObject);
        }
        internal static bool ContainsMask(GameObject obj)
        {
            if (obj == null)
            {
                return false;
            }
            if (obj.GetComponent<SpriteMask>())
            {
                return true;
            }
            return false;
        }

        internal static bool IsShiftOrAltHeldInWindow()
        {
            if (Event.current == null || CoInspectorWindow.MainCoInspector == null)
            {
                return false;
            }
            if (EditorWindow.focusedWindow == CoInspectorWindow.MainCoInspector || EditorWindow.mouseOverWindow == CoInspectorWindow.MainCoInspector)
            {
                if (Event.current.shift || Event.current.alt)
                {
                    return true;
                }
            }
            return false;
        }

#if UNITY_6000_4_OR_NEWER
        internal static GameObject GetLastCreatedGameObject(GameObject[] gameObjects)
        {
            if (gameObjects == null || gameObjects.Length == 0) return null;

            GameObject lastCreated = null;
            EntityId highest = EntityId.None;

            foreach (var go in gameObjects)
            {
                if (go == null) continue;
                var id = go.GetComponents<Component>()[0].GetEntityId();
                if (!highest.IsValid() || id.CompareTo(highest) > 0)
                {
                    highest = id;
                    lastCreated = go;
                }
            }
            return lastCreated;
        }
#else

        internal static GameObject GetLastCreatedGameObject(GameObject[] gameObjects)
        {
            if (gameObjects == null || gameObjects.Length == 0)
            {
                return null;
            }
            int newestInstance = 0;
            GameObject lastCreated = null;

            foreach (var go in gameObjects)
            {
                Component firstComponent = go.GetComponents<Component>()[0];
                if (go == null) continue;

                int instanceAbs = GetObjectId(firstComponent);
                if (instanceAbs < 0)
                {
                    instanceAbs = -instanceAbs;
                }
                if (instanceAbs > newestInstance)
                {
                    newestInstance = instanceAbs;
                    lastCreated = go;
                }
            }
            return lastCreated;
        }
#endif
        internal static bool ObjectIsMonoBehaviourOrScriptableObjectWithoutScript(System.Object obj)
        {
            if ((bool)obj)
            {
                return obj.GetType() == typeof(MonoBehaviour) || obj.GetType() == typeof(ScriptableObject);
            }
            return obj is MonoBehaviour || obj is ScriptableObject;
        }


        internal static List<KeyValuePair<Type, List<List<Component>>>> OrderedComponentMap(
        GameObject[] gameObjects, CoInspectorWindow window, bool prefab = false)
        {
            var orderedComponents = new List<KeyValuePair<Type, List<List<Component>>>>();

            if (gameObjects == null || gameObjects.Length == 0)
            {
                return orderedComponents;
            }
            var gameObjectComponents = new Dictionary<GameObject, Component[]>();
            foreach (var go in gameObjects)
            {
                if (go == null) continue;
                gameObjectComponents[go] = go.GetComponents<Component>();
            }
            GameObject lastGameObject = null;
            /*  if (!prefab)
              {
                  lastGameObject = window.GetActiveTab().MultiTrackingTarget();
              }
             else*/
            {
                lastGameObject = gameObjects[0];
            }
            if (lastGameObject == null)
            {
                return orderedComponents;
            }
            Component[] lastComponents = gameObjectComponents[lastGameObject];
            var lastGameObjectTypes = lastComponents
                .Where(comp => comp != null)
                .Select(comp => comp.GetType())
                .ToList();

            var commonTypes = new HashSet<Type>(lastGameObjectTypes);
            if (window != null)
            {
                int firstComponentCount = gameObjectComponents[gameObjects[0]].Length;

                foreach (var go in gameObjects)
                {
                    if (go == null) continue;

                    int componentCount = gameObjectComponents[go].Length;
                    if (componentCount != firstComponentCount)
                    {
                        if (prefab)
                            window.differentPrefabComponents = true;
                        else
                            window.differentComponents = true;
                    }
                }
            }
            foreach (var go in gameObjects)
            {
                if (go == null) continue;

                var currentTypes = new HashSet<Type>(gameObjectComponents[go]
                    .Where(comp => comp != null)
                    .Select(comp => comp.GetType()));
                commonTypes.IntersectWith(currentTypes);
            }

            foreach (var comp in lastComponents)
            {
                if (comp == null) continue;

                Type type = comp.GetType();
                if (commonTypes.Contains(type))
                {
                    var componentInstancesPerGameObject = new List<List<Component>>();
                    foreach (var go in gameObjects)
                    {
                        componentInstancesPerGameObject.Add(new List<Component>());
                    }
                    orderedComponents.Add(new KeyValuePair<Type, List<List<Component>>>(type, componentInstancesPerGameObject));
                    commonTypes.Remove(type);
                }
            }

            var typeToIndexMap = orderedComponents
                .Select((kvp, idx) => new { kvp.Key, Index = idx })
                .ToDictionary(x => x.Key, x => x.Index);

            for (int i = 0; i < gameObjects.Length; i++)
            {
                GameObject go = gameObjects[i];
                if (go == null) continue;

                Component[] components = gameObjectComponents[go];
                foreach (var comp in components)
                {
                    if (comp == null) continue;

                    Type type = comp.GetType();
                    if (typeToIndexMap.TryGetValue(type, out int index))
                    {
                        orderedComponents[index].Value[i].Add(comp);
                    }
                }
            }
            for (int idx = orderedComponents.Count - 1; idx >= 0; idx--)
            {
                var entry = orderedComponents[idx];
                int minInstanceCount = entry.Value.Min(list => list.Count);

                if (minInstanceCount > 0)
                {
                    for (int i = 0; i < entry.Value.Count; i++)
                    {
                        var list = entry.Value[i];
                        if (list.Count > minInstanceCount)
                        {
                            list.RemoveRange(minInstanceCount, list.Count - minInstanceCount);
                        }
                    }
                }
                else
                {
                    orderedComponents.RemoveAt(idx);
                }
            }
            return orderedComponents;
        }
        internal static bool AreAllTargetsPrefabs(UnityEngine.Object[] targets)
        {
            if (targets == null)
            {
                return false;
            }
            foreach (var target in targets)
            {
                if (target)
                {
                    if (!IsAPrefabAsset(target))
                    {
                        return false;
                    }
                }
            }
            return true;
        }
        internal static Texture2D GetIconForComponent(Component component)
        {
            if (component == null)
            {
                return null;
            }

            return AssetPreview.GetMiniThumbnail(component);
        }

        internal static bool CanMovePrefabComponents(Component moved, Component moveTarget)
        {
            return !ComponentIsPartOfOriginalPrefab(moved) && !ComponentIsPartOfOriginalPrefab(moveTarget);
        }

        internal static bool ComponentIsPartOfOriginalPrefab(Component component)
        {
            if (!component)
            {
                return false;
            }
            return PrefabUtility.GetCorrespondingObjectFromSource(component) != null;
        }

        internal static Texture2D GetBestFittingIconForGameObject(GameObject gameObject)
        {

            if (PrefabUtility.IsPartOfPrefabInstance(gameObject))
            {
                if (PrefabUtility.IsAnyPrefabInstanceRoot(gameObject))
                {

                    UnityEngine.Object _root = PrefabUtility.GetCorrespondingObjectFromOriginalSource(gameObject);
                    if (_root != null && PoolCache.IsAnImportedObject(_root))
                    {
                        return CustomGUIContents.ImportedIcon.image as Texture2D;
                    }
                }

                var root = PrefabUtility.GetCorrespondingObjectFromOriginalSource(gameObject);

                if (root && PoolCache.IsAnImportedObject(root))
                {
                    return CustomGUIContents.ImportedIcon.image as Texture2D;
                }
                return CustomGUIContents.PrefabIcon.image as Texture2D;
            }
            Component[] components = gameObject.GetComponents<Component>();

            if (components.Length == 1)
            {
                if (components[0] is RectTransform)
                {
                    return CustomGUIContents.EmptyRectTransformContent.image as Texture2D;
                }
                return CustomGUIContents.EmptyGameObjectContent.image as Texture2D;
            }
            List<Texture2D> allIcons = new List<Texture2D>();
            Texture2D priorityIcon = null;
            for (int i = 0; i < components.Length; i++)
            {
                if (i == 0)
                {
                    continue;
                }
                var component = components[i];
                if (component == null)
                {
                    if (components.Length == 2)
                    {
                        allIcons.Add(GetIconForComponent(components[0]));
                    }
                    continue;
                }
                Texture2D icon = GetIconForComponent(component);
                if (icon != null)
                { allIcons.Add(icon); }
                if (!priorityIcon && prioritizedComponentTypes.Contains(component.GetType().Name))
                {
                    priorityIcon = icon;
                }
            }
            if (priorityIcon)
            {
                return priorityIcon;
            }
            foreach (var icon in allIcons)
            {
                if (!icon.name.Contains("cs Script Icon"))
                {
                    return icon;
                }
            }
            if (!allIcons.Any())
            {
                return allIcons[0];
            }
            return CustomGUIContents.EmptyGameObjectContent.image as Texture2D;

        }
#if UNITY_6000_4_OR_NEWER
        public static EntityId GetMissingComponentID(SerializedObject gameObjectSerialized, int index)
        {
            var componentArray = gameObjectSerialized.FindProperty("m_Component");
            if (componentArray == null || !componentArray.isArray || index >= componentArray.arraySize) { return EntityId.None; }
            var element = componentArray.GetArrayElementAtIndex(index);
            var componentReference = element.FindPropertyRelative("component");
            var entityID = componentReference.objectReferenceEntityIdValue;
            return entityID;
        }
#else
        public static int GetMissingComponentID(SerializedObject gameObjectSerialized, int index)
        {
            var componentArray = gameObjectSerialized.FindProperty("m_Component");
            if (componentArray == null || !componentArray.isArray || index >= componentArray.arraySize) { return 0; }
            var element = componentArray.GetArrayElementAtIndex(index);
            var componentReference = element.FindPropertyRelative("component");
            var instanceID = componentReference.objectReferenceInstanceIDValue;
            return instanceID;
        }
#endif
        internal static readonly string[] prioritizedComponentTypes =
           {
        "Animator", "Renderer", "AudioSource", "Light", "Camera",
        "ParticleSystem", "UnityEngine.Video.VideoPlayer", "UnityEngine.UI.Image", "UnityEngine.UI.Text"
    };

        internal static bool AreAllTargetsImportedObjects(UnityEngine.Object[] targets)
        {
            if (targets == null)
            {
                return false;
            }
            foreach (var target in targets)
            {
                if (target)
                {
                    if (!IsAnImportedObject(target))
                    {
                        return false;
                    }
                }
            }
            return true;
        }
        internal static bool IsAssetBuiltIn(string assetPath)
        {
            return !assetPath.StartsWith("Assets/");
        }
        internal static bool IsAnImportedObject(UnityObject _object)
        {
            if (_object == null)
            {
                return false;
            }
            string _extension = System.IO.Path.GetExtension(AssetDatabase.GetAssetPath(_object)).ToLower();
            if (_extension == "")
            {
                return false;
            }
            return _extension != ".prefab" && _object is GameObject;
        }
        internal static bool IsAPrefabAsset(UnityObject _object)
        {
            if (_object == null)
            {
                return false;
            }
            string _extension = System.IO.Path.GetExtension(AssetDatabase.GetAssetPath(_object)).ToLower();
            return _extension == ".prefab";
        }
        internal static bool AssetAlreadyTarget(UnityEngine.Object obj, CoInspectorWindow window)
        {
            if (window == null)
            {
                return false;
            }
            if (obj == null)
            {
                return false;
            }
            if (window.targetObject)
            {
                if (window.targetObject == obj)
                {
                    return true;
                }
            }
            if (window.targetObjects != null && window.targetObjects.Length > 0)
            {
                if (window.targetObjects.Contains(obj))
                {
                    return true;
                }
            }
            return false;
        }
        internal static bool AssetsAlreadyTargets(UnityEngine.Object[] assets, CoInspectorWindow window)
        {
            if (window == null)
            {
                return false;
            }

            if (assets == null || assets.Length == 0)
            {
                return false;
            }
            if (MissingScriptManager.IsActive())
            {
                return false;
            }

            if (assets.Length == 1 && window.targetObjects != null && window.targetObjects.Length > 1)
            {
                return false;
            }
            if (assets.Length == 1 && window.targetObject != null)
            {
                return AssetAlreadyTarget(assets[0], window);
            }
            if (window.targetObjects == null || window.targetObjects.Length == 0)
            {
                return false;
            }
            if (assets.Length != window.targetObjects.Length)
            {
                return false;
            }
            for (int i = 0; i < assets.Length; i++)
            {
                if (!window.targetObjects.Contains(assets[i]))
                {
                    return false;
                }
            }
            return true;
        }
        internal static bool IsMainAsset(UnityObject unityObject)
        {
            if (unityObject == null)
            {
                return false;
            }
            return !AssetDatabase.IsSubAsset(unityObject);
            /*
            if (unityObject is Sprite)
            {
                return false;
            }
            string path = AssetDatabase.GetAssetPath(unityObject);
            if (unityObject == AssetDatabase.LoadMainAssetAtPath(path))
            {
                Debug.Log("Main Asset " + unityObject.name + " at path " + path);
                return true;
            }
            return false;*/
        }

        internal static List<SerializableTransform> CloneTransformList(List<SerializableTransform> list)
        {
            List<SerializableTransform> newList = new List<SerializableTransform>();
            if (list == null)
            {
                return newList;
            }
            foreach (var transform in list)
            {
                newList.Add(new SerializableTransform(transform));
            }
            return newList;
        }

        internal static List<TabInfo> CloneTabList(List<TabInfo> list)
        {
            List<TabInfo> newList = new List<TabInfo>();
            if (list == null)
            {
                return newList;
            }
            foreach (var tab in list)
            {
                newList.Add(new TabInfo(tab));
            }
            return newList;
        }

        internal static bool IsSelection(GameObject ob)
        {
            if (Selection.gameObjects != null && Selection.gameObjects.Length == 1)
            {
                if (Selection.gameObjects[0] == ob)
                {
                    return true;
                }
            }
            return false;
        }
        internal static void DrawBrokenPrefabMessage(bool multi = false)
        {
            if (!multi)
            {
                EditorGUILayout.HelpBox("    This Prefab seems to be broken, so it can't be opened!", MessageType.Warning);
            }
            else
            {
                EditorGUILayout.HelpBox("    One or more of the selected Prefabs seem to be broken, so they can't be opened!", MessageType.Warning);
            }
            GUILayout.Space(7);
        }

        internal static int ShowUnappliedImportSettings(Editor editor)
        {
            if (editor == null)
            {
                return 1;
            }
            if (editor.targets == null)
            {
                return 1;
            }
            int count = editor.targets.Length;
            if (count == 0)
            {
                return 1;
            }
            string message = "";
            string path = AssetDatabase.GetAssetPath(editor.target);
            if (count == 1)
            {
                message = "Unapplied import settings for '" + path + "'";
            }
            else
            {
                message = "Unapplied import settings for '" + count + "' files";
            }
            return EditorUtility.DisplayDialogComplex("Unapplied import settings", message, "Apply", "Cancel", "Revert");

        }

        internal static void SaveAsset(UnityObject asset, Editor assetEditor, Editor importSettingsEditor)
        {

            if (asset == null)
            {
                return;
            }
            if (assetEditor != null)
            {
                Reflected.ApplyChanges(assetEditor);
            }
            if (importSettingsEditor != null)
            {
                Reflected.ApplyChanges(importSettingsEditor);
            }
#if UNITY_2020_3_OR_NEWER
            AssetDatabase.SaveAssetIfDirty(asset);
#else
            EditorUtility.SetDirty(asset);
            AssetDatabase.SaveAssets();
#endif
            string path = AssetDatabase.GetAssetPath(asset);
            if (path == null || path.Length == 0)
            {
                return;
            }
            AssetDatabase.ImportAsset(AssetDatabase.GetAssetPath(asset));
        }
        internal static void SaveAssets(UnityObject[] assets, Editor assetEditor, Editor importSettingsEditor)
        {

            if (assets == null || assets.Length == 0)
            {
                return;
            }
            if (assetEditor != null)
            {
                Reflected.ApplyChanges(assetEditor);
            }
            if (importSettingsEditor != null)
            {
                Reflected.ApplyChanges(importSettingsEditor);
            }
            foreach (UnityObject asset in assets)
            {
                if (asset == null)
                {
                    continue;
                }
#if UNITY_2020_3_OR_NEWER
                AssetDatabase.SaveAssetIfDirty(asset);
#else
               EditorUtility.SetDirty(asset);
#endif
            }
#if !UNITY_2020_3_OR_NEWER
            AssetDatabase.SaveAssets();
#endif
            foreach (UnityObject asset in assets)
            {
                if (asset == null)
                {
                    continue;
                }
                AssetDatabase.ImportAsset(AssetDatabase.GetAssetPath(asset));
            }
        }
        internal static Rect GetLastLineRect()
        {
            return GetLastLineRect(GUILayoutUtility.GetLastRect());
        }
        internal static Rect GetLastLineRect(Rect rect)
        {
            if (rect == null)
            {
                rect = GUILayoutUtility.GetLastRect();
            }
            float x = rect.x;
            rect.x = 0;
            rect.width += x;
            return rect;
        }

        internal static void DrawLineOverRect(Color color, int padding = 0, int thickness = 1)
        {
            Rect rect = GUILayoutUtility.GetLastRect();
            DrawLineOverRect(rect, color, padding, thickness);
        }
        internal static void DrawLineUnderRect(Color color, int padding = 0, int thickness = 1)
        {
            Rect rect = GUILayoutUtility.GetLastRect();
            DrawLineUnderRect(rect, color, padding, thickness);
        }
        internal static void DrawLineOverRect(int padding = 0, int thickness = 1)
        {
            Rect rect = GUILayoutUtility.GetLastRect();
            DrawLineOverRect(rect, CustomColors.SimpleBright, padding, thickness);
        }
        internal static void DrawLineUnderRect(int padding = 0, int thickness = 1)
        {
            Rect rect = GUILayoutUtility.GetLastRect();
            DrawLineUnderRect(rect, CustomColors.SimpleShadow, padding, thickness);
        }
        internal static void DrawUnderLastComponent()
        {
            EditorUtils.DrawLineUnderRect();
            EditorUtils.DrawLineUnderRect(CustomColors.SimpleBright, 1);
        }
        internal static void DrawLineOverRect(Rect rect, int padding = 0, int thickness = 1)
        {
            DrawLineOverRect(rect, CustomColors.SimpleBright, padding, thickness);
        }
        internal static void DrawLineUnderRect(Rect rect, int padding = 0, int thickness = 1)
        {
            DrawLineUnderRect(rect, CustomColors.SimpleShadow, padding, thickness);
        }
        internal static void DrawLineOverRect(Rect rect, Color color, int padding = 0, int thickness = 1)
        {
            EditorGUI.DrawRect(new Rect(rect.x, rect.yMin - padding, rect.width, thickness), color);
        }
        internal static void DrawLineUnderRect(Rect rect, Color color, int padding = 0, int thickness = 1)
        {
            EditorGUI.DrawRect(new Rect(rect.x, rect.y + rect.height + padding, rect.width, thickness), color);
        }
        internal static void DrawFadeToLeft(Rect rect)
        {
            DrawFadeToLeft(rect, CustomColors.GradientShadow);
        }
        internal static void DrawFadeToLeft(Rect rect, Color color, float limit = 0)
        {
            float pixelsPerPoint = EditorGUIUtility.pixelsPerPoint;

            float fadeLimit = (limit <= 0 || limit > rect.width) ? rect.width : limit;
            fadeLimit *= pixelsPerPoint;

            float alphaStep = color.a / fadeLimit;

            for (int x = 0; x < fadeLimit; x++)
            {
                float alpha = color.a - (alphaStep * x);
                Color fadedColor = new Color(color.r, color.g, color.b, alpha);
                float scaledX = rect.xMax - (x / pixelsPerPoint) - 1f / pixelsPerPoint;
                EditorGUI.DrawRect(new Rect(scaledX, rect.y, 1f / pixelsPerPoint, rect.height), fadedColor);
            }
        }
        internal static void DrawFadeToRight(Rect rect)
        {
            DrawFadeToRight(rect, CustomColors.GradientShadow);
        }
        internal static void DrawFadeToRight(Rect rect, Color color, float limit = 0)
        {
            float pixelsPerPoint = EditorGUIUtility.pixelsPerPoint;
            float fadeLimit = (limit <= 0 || limit > rect.width) ? rect.width : limit;
            fadeLimit *= pixelsPerPoint;
            float alphaStep = color.a / fadeLimit;

            for (int x = 0; x < fadeLimit; x++)
            {
                float alpha = color.a - (alphaStep * x);
                Color fadedColor = new Color(color.r, color.g, color.b, alpha);
                float scaledX = rect.x + (x / pixelsPerPoint);
                EditorGUI.DrawRect(new Rect(scaledX, rect.y, 1f / pixelsPerPoint, rect.height), fadedColor);
            }
        }
        internal static void DrawFadeToTop(Rect rect)
        {
            DrawFadeToTop(rect, CustomColors.GradientShadow);
        }
        internal static void DrawFadeToTop(Rect rect, Color color, float limit = 0)
        {
            float pixelsPerPoint = EditorGUIUtility.pixelsPerPoint;
            float fadeLimit = (limit <= 0 || limit > rect.height) ? rect.height : limit;
            fadeLimit *= pixelsPerPoint;
            float alphaStep = color.a / fadeLimit;

            for (int y = 0; y < fadeLimit; y++)
            {
                float alpha = color.a - (alphaStep * y);
                Color fadedColor = new Color(color.r, color.g, color.b, alpha);
                float scaledY = rect.yMax - (y / pixelsPerPoint) - 1f / pixelsPerPoint;
                EditorGUI.DrawRect(new Rect(rect.x, scaledY, rect.width, 1f / pixelsPerPoint), fadedColor);
            }
        }
        internal static void DrawFadeToBottom(Rect rect)
        {
            DrawFadeToBottom(rect, CustomColors.GradientShadow);
        }
        internal static void DrawFadeToBottom(Rect rect, Color color, float limit = 0)
        {
            float pixelsPerPoint = EditorGUIUtility.pixelsPerPoint;
            float fadeLimit = (limit <= 0 || limit > rect.height) ? rect.height : limit;
            fadeLimit *= pixelsPerPoint;
            float alphaStep = color.a / fadeLimit;

            for (int y = 0; y < fadeLimit; y++)
            {
                float alpha = color.a - (alphaStep * y);
                Color fadedColor = new Color(color.r, color.g, color.b, alpha);
                float scaledY = rect.y + (y / pixelsPerPoint);
                EditorGUI.DrawRect(new Rect(rect.x, scaledY, rect.width, 1f / pixelsPerPoint), fadedColor);
            }
        }
        internal static void DrawOutsideRectBorder(Rect rect, Color color, int thickness = 1)
        {
            float scaledThickness = thickness;

            float borderLeft = rect.x - scaledThickness;
            float borderTop = rect.y - scaledThickness;
            float borderRight = rect.x + rect.width;
            float borderBottom = rect.y + rect.height;

            EditorGUI.DrawRect(new Rect(borderLeft, borderTop, rect.width + 2 * scaledThickness, scaledThickness), color);
            EditorGUI.DrawRect(new Rect(borderLeft, borderTop, scaledThickness, rect.height + 2 * scaledThickness), color);
            EditorGUI.DrawRect(new Rect(borderLeft, borderBottom, rect.width + 2 * scaledThickness, scaledThickness), color);
            EditorGUI.DrawRect(new Rect(borderRight, borderTop, scaledThickness, rect.height + 2 * scaledThickness), color);
        }
        internal static void DrawRectBorder(Rect rect, Color color, int thickness = 1)
        {
            float scaledThickness = thickness;

            EditorGUI.DrawRect(new Rect(rect.x, rect.y, rect.width, scaledThickness), color);
            EditorGUI.DrawRect(new Rect(rect.x, rect.y, scaledThickness, rect.height), color);
            EditorGUI.DrawRect(new Rect(rect.x, rect.y + rect.height - scaledThickness, rect.width, scaledThickness), color);
            EditorGUI.DrawRect(new Rect(rect.x + rect.width - scaledThickness, rect.y, scaledThickness, rect.height), color);
        }
        static Color blue = new Color(0.6f, 0.6f, 0.9f, 1);
        static Color darkerBlue = new Color(0.475f, 0.475f, 0.75f, 1);
        public static void DrawSelectedLineDecorator(Rect rect, Color backgroundColor, bool active = true, bool isLast = false)
        {

            Color blueLine = active ? blue : darkerBlue;
            rect.height = 2;
            rect.width += 0.5f;
            if (isLast)
            {
                rect.width += 1;
            }

            Rect lineRect = new Rect(rect);
            //rect.width += 1;
            // rect.x -= 0.5f;
            EditorGUI.DrawRect(rect, blueLine);

            Rect leftSide = new Rect(
                Mathf.Floor(lineRect.x - 1),
                Mathf.Floor(lineRect.y + 1),
                1,
                2
            );
            EditorGUI.DrawRect(leftSide, blueLine);

            Rect leftTop = new Rect(leftSide.x, leftSide.y - 1, 1, 1);
            EditorGUI.DrawRect(leftTop, backgroundColor);

            Rect rightSide = new Rect(
                Mathf.Floor(lineRect.x + lineRect.width),
                Mathf.Floor(lineRect.y + 1),
                1,
                2
            );
            EditorGUI.DrawRect(rightSide, blueLine);

            Rect rightTop = new Rect(rightSide.x, rightSide.y - 1, 1, 1);
            EditorGUI.DrawRect(rightTop, backgroundColor);
        }

        public static void DrawLineRoundDecorator(Rect rect, Color color, Color backgroundColor, bool hardSides = false, bool skipBack = false, bool isLast = false)
        {
            color = CustomColors.DefaultInspectorBright * 1.95f;
            float brightnessMultiplier80 = hardSides ? 0.95f : 0.95f;
            float brightnessMultiplier60 = hardSides ? 0.9f : 0.95f;
            rect.x += 3;
            rect.width -= 6.5f;
            if (isLast)
            {
                // rect.width += 1;
            }
            Rect lineRect = new Rect(rect);
            lineRect.width += 1f;
            lineRect.x -= 0.5f;
            if (hardSides)
            {
                color *= 1.15f;
            }

            Color DarkenColor(Color originalColor, float brightnessFactor)
            {
                return new Color(
                    originalColor.r * brightnessFactor,
                    originalColor.g * brightnessFactor,
                    originalColor.b * brightnessFactor,
                    originalColor.a
                );
            }

            EditorGUI.DrawRect(new Rect(lineRect.x, lineRect.y, lineRect.width, 1),
                hardSides ? color : DarkenColor(color, 1.1f));

            EditorGUI.DrawRect(new Rect(rect.x - 2, rect.y, 2, 1),
                DarkenColor(color, brightnessMultiplier80));

            if (!skipBack)
            {
                EditorGUI.DrawRect(new Rect(rect.x - 3, rect.y, 1, 1), backgroundColor);
            }
            EditorGUI.DrawRect(new Rect(rect.x - 3, rect.y + 1, 1, 1),
                DarkenColor(color, brightnessMultiplier60));

            float rightX = rect.x + rect.width;
            EditorGUI.DrawRect(new Rect(rightX, rect.y, 2, 1),
                DarkenColor(color, brightnessMultiplier80));

            if (!skipBack)
            {
                EditorGUI.DrawRect(new Rect(rightX + 2, rect.y, 1, 1), backgroundColor);
            }

            EditorGUI.DrawRect(new Rect(rightX + 2, rect.y + 1, 1, 1),
                DarkenColor(color, brightnessMultiplier60));
        }
        public static void DrawActiveTabUnder(Rect rect, Color editorColor)
        {
            rect.width += 2;
            rect.x -= 1;
            float width = rect.width;
            rect.height = 2;
            EditorGUI.DrawRect(rect, editorColor * 1.04f);
            rect.width = 1;
            rect.height = 1;
            EditorGUI.DrawRect(rect, CustomColors.SimpleBright);
            rect.x += width - 1;
            EditorGUI.DrawRect(rect, CustomColors.SimpleBright);
        }

        private static Color AdjustOpacity(Color color, float factor)
        {
            return new Color(color.r, color.g, color.b, color.a * factor);
        }

        internal static void DrawTipSection(CoInspectorWindow window)
        {
            GUILayout.Space(15);
            CustomGUIStyles.StartBoxSection();
            GUILayout.Space(5);
            EditorGUILayout.BeginHorizontal();
            GUILayout.Space(5);
            GUILayout.Label("Random Tip: ", CustomGUIStyles.BoldLabel);
            EditorGUILayout.EndHorizontal();
            GUILayout.Space(10);
            EditorGUILayout.BeginVertical();
            EditorGUILayout.BeginHorizontal();
            CustomGUIStyles.HelpBox(window.GetCurrentTip());
            EditorGUILayout.EndHorizontal();
            GUILayout.Space(5);

            //if (window.GetCurrentTip().Contains("Settings Window"))
            {
                Color color = GUI.backgroundColor;
                GUILayout.Space(5);
                EditorGUILayout.BeginHorizontal();
                GUILayout.FlexibleSpace();
                GUI.backgroundColor = CustomColors.NewTabButton;
                if (IsLightSkin())
                {
                    GUI.backgroundColor = Color.blue / 6;
                }

                if (GUILayout.Button(CustomGUIContents.SettingsContent, GUILayout.Width(120), GUILayout.Height(24)))
                {
                    SettingsWindow.ShowWindow();
                }
                CustomGUIContents.DrawCustomButton();
                GUILayout.Space(10);
                EditorGUILayout.EndHorizontal();
                GUI.backgroundColor = color;
            }
            GUILayout.Space(15);
            EditorGUILayout.EndVertical();
            CustomGUIStyles.EndBoxSection();
        }

        public static bool _HasPreviewGUI(this Editor editor)
        {
            if (editor == null || editor.target == null)
            {
                return false;
            }

            if (editor.GetType().Name == "AnimationClipEditor")
            {
                return true;
            }
            return editor.HasPreviewGUI();
        }

        internal static int GetRemovedComponentOriginalPosition(this UnityEditor.SceneManagement.RemovedComponent component, Component[] originalComponents)
        {
            if (originalComponents == null)
            {
                return -1;
            }
            Component actualComponent = component.assetComponent;
            int position = -1;
            foreach (var comp in originalComponents)
            {
                position += 1;
                if (comp == actualComponent)
                {
                    return position;
                }
            }
            return -1;
        }

        internal static VisualElement CreateCursorOverlay(CoInspectorWindow coInspector, MouseCursor cursorType)
        {
            coInspector.ClearCursorOverlay();
            var rootElement = coInspector.rootVisualElement;

            VisualElement overlay = new VisualElement();
            overlay.style.position = Position.Absolute;
            overlay.style.left = 0;
            overlay.style.top = 0;
            overlay.style.width = 0;
            overlay.style.height = 0;
            overlay.pickingMode = PickingMode.Position;


            IMGUIContainer cursorOverlay = new IMGUIContainer(() =>
            {
                Rect rect = new Rect(0, 0, rootElement.resolvedStyle.width, rootElement.resolvedStyle.height);

                coInspector.currentMiddleScrollPosition = Event.current.mousePosition;
                coInspector.UpdateMiddleScroll();


                if (coInspector.cursorOverlay != null && coInspector.middleScrolling && coInspector.cursorOverlay.ClassListContains("middleScroller"))
                {
                    if (coInspector.cursorOverlay.name == "CursorOverlay_ScrollUp")
                    {
                        UnityEngine.Cursor.SetCursor(CustomGUIContents.CursorScrollUp, new Vector2(16, 16), CursorMode.Auto);
                    }
                    else if (coInspector.cursorOverlay.name == "CursorOverlay_ScrollDown")
                    {
                        UnityEngine.Cursor.SetCursor(CustomGUIContents.CursorScrollDown, new Vector2(16, 16), CursorMode.Auto);
                    }
                    else
                    {
                        UnityEngine.Cursor.SetCursor(CustomGUIContents.CursorScroll, new Vector2(16, 16), CursorMode.Auto);
                    }
                }
                EditorGUIUtility.AddCursorRect(rect, cursorType);

                if (Event.current.type == EventType.MouseMove)
                {
                    Event.current.Use();
                }
            });
            overlay.Add(cursorOverlay);
            overlay.name = "CursorOverlay";
            rootElement.panel.visualTree.ReplaceAndInsertFirst(overlay);
            coInspector.cursorOverlay = overlay;
            return overlay;
        }

        internal static ScrollView GetValidScrollView(CoInspectorWindow coInspetor, Vector2 mousePos)
        {
            if (IsValidMiddleClickScrollView(coInspetor.assetScrollView, mousePos))
            {
                return coInspetor.assetScrollView;
            }
            if (IsValidMiddleClickScrollView(coInspetor.componentScrollView, mousePos))
            {
                return coInspetor.componentScrollView;
            }
            return null;
        }

        internal static bool IsValidMiddleClickScrollView(ScrollView scrollView, Vector2 mousePos)
        {
            return scrollView != null && scrollView.contentViewport.worldBound.Contains(mousePos) && scrollView.CanBeScrolled();
        }


        public static VisualElement DebugInspector(Editor editor)
        {
            if (editor == null || editor.serializedObject == null)
            {
                return null;
            }
            VisualElement defaultInspector = new IMGUIContainer(() =>
            {
                EditorGUIUtility.wideMode = true;
                EditorGUIUtility.labelWidth = CoInspectorWindow.MainCoInspector.position.width / 2.7f;
                EditorGUI.indentLevel = 1;
                editor.DrawDefaultInspector();
                EditorGUI.indentLevel = 0;
            });
            return defaultInspector;
        }

        internal static void AutoFocusOnSceneView(GameObject target)
        {
            if (!CoInspectorWindow.autoFocus)
            {
                return;
            }
            FocusOnSceneView(target);
        }

        internal static void AutoFocusOnSceneView(GameObject[] targets)
        {
            if (!CoInspectorWindow.autoFocus)
            {
                return;
            }
            FocusOnSceneView(targets);
        }

        internal static void FocusOnSceneView(GameObject target)
        {
            EditorApplication.delayCall += () =>
            {
                if (target == null)
                {
                    return;
                }
                FrameObject(target);
            };
        }
        internal static void FocusOnSceneView(GameObject[] targets)
        {
            EditorApplication.delayCall += () =>
            {
                if (targets == null || targets.Length == 0)
                {
                    return;
                }
                FrameObjects(targets);
            };
        }
        public static void FrameObject(GameObject target)
        {
            GameObject previousSelection = Selection.activeGameObject;
            Selection.activeGameObject = target;
            SceneView.FrameLastActiveSceneView();
            Selection.activeGameObject = previousSelection;
        }
        public static void FrameObjects(GameObject[] targets)
        {
            if (targets == null || targets.Length == 0)
            {
                return;
            }
            UnityEngine.Object[] previousSelection = Selection.objects;
            Selection.objects = targets;
            SceneView.FrameLastActiveSceneView();
            Selection.objects = previousSelection;
        }
    }

    internal class ComponentDragOperation
    {
        public Component draggedComponent;
        public Editor draggedEditor;
        public int targetIndex;
        public int sourceIndex;
        public int sourceTabIndex = -1;
        public GameObject targetObject;
        public bool isSelf;
        public bool errored;
        public bool prefabError;
        public bool removeAfter;
        public bool isCopy;
        public bool isAsset;
        public bool isMouseBelowRect;
        public bool consumed;
        public bool consuming;
        public bool foldoutOrigin;
        public List<UnityObject> assets;
        public Rect mouseOverRect;
        public ComponentDragOperation()
        {
            CoInspectorWindow.alreadyMovingComponent = false;
        }
        public ComponentDragOperation(Component draggedComponent, int targetIndex, int sourceIndex, GameObject target, bool isSelf, bool removeAfter, bool prefabError)
        {
            this.draggedComponent = draggedComponent;
            this.targetIndex = targetIndex;
            this.sourceIndex = sourceIndex;
            this.targetObject = target;
            this.isSelf = isSelf;
            this.removeAfter = removeAfter;
            this.prefabError = prefabError;
            CoInspectorWindow.alreadyMovingComponent = false;
        }
    }
    public static class ComponentHighlighter
    {
        private static Color currentColor;
        private static Color noColor = new Color(0, 0, 0, 0);
        private static string currentId;
        private static bool isFading;
        private static float startTime;
        private static float maxFadeTime = 0.25f;
        private static int defaultSteps = 70;

        public static void StartHighlight(string id, Color color, int steps = -1)
        {
            if (steps < 0) steps = defaultSteps;
            currentId = id;
            currentColor = color;
            isFading = true;
            startTime = Time.realtimeSinceStartup;
        }

        public static bool IsFading()
        {
            return isFading;
        }

        public static Color GetHighlight(string id, EditorWindow window)
        {
            if (!isFading || currentId != id)
            {
                return Color.black;
            }
            float t = (Time.realtimeSinceStartup - startTime) / maxFadeTime;
            if (t >= 1f)
            {
                isFading = false;
                return noColor;
            }
            return Color.Lerp(currentColor, Color.clear, t);
        }
    }
    internal class DelayedAction
    {
        public Action Action;
        public int Delay;

        public DelayedAction(Action action, int framesRemaining)
        {
            Action = action;
            Delay = framesRemaining;
        }
    }
}
