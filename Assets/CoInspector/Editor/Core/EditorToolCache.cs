using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.EditorTools;
using UnityEngine;
using UnityObject = UnityEngine.Object;

namespace CoInspector
{
    [InitializeOnLoad]
    internal static class EditorToolCache
    {
        static EditorToolCache()
        {
#if UNITY_2020_2_OR_NEWER && !UNITY_2022_2_OR_NEWER
            AssemblyReloadEvents.beforeAssemblyReload += ClearCache;
            PopulateCache();
#else
            AssemblyReloadEvents.beforeAssemblyReload += ClearCache;
            PopulateCache();
#endif
        }

        private static Dictionary<string, List<Type>> toolTypesCache = new Dictionary<string, List<Type>>();
        private static Dictionary<string, List<GUIContent>> toolContentsCache = new Dictionary<string, List<GUIContent>>();
        private static Type editorToolManagerType;
        private static MethodInfo getComponentToolMethod;
        private static List<EditorTool> activeTools = new List<EditorTool>();

        private static Type GetEditorToolManagerType()
        {
            if (editorToolManagerType != null)
            {
                return editorToolManagerType;
            }
            else
            {
                editorToolManagerType = typeof(EditorUtility).Assembly.GetType("UnityEditor.EditorToolManager");
                return editorToolManagerType;
            }
        }

        private static MethodInfo GetGetComponentToolMethod()
        {
            if (getComponentToolMethod != null)
            {
                return getComponentToolMethod;
            }
            else
            {
                if (GetEditorToolManagerType() != null)
                {
                    getComponentToolMethod = GetEditorToolManagerType().GetMethod("GetComponentTool", BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public);
                    return getComponentToolMethod;
                }
                else
                {
                    //   Debug.LogError("EditorToolManager type not found.");
                    return null;
                }
            }
        }

        private static void PopulateCache()
        {
            toolTypesCache.Clear();
            toolContentsCache.Clear();

            var assemblies = AppDomain.CurrentDomain.GetAssemblies();
            foreach (var assembly in assemblies)
            {
                if (assembly == null || assembly.GetName().ToString().Contains("Sirenix"))
                {
                    continue;
                }
                var editorToolTypes = assembly.GetTypes()
                                              .Where(t => t.IsSubclassOf(typeof(EditorTool)) && !t.IsAbstract);

                foreach (var type in editorToolTypes)
                {
                    var customToolAttributes = type.GetCustomAttributes(typeof(EditorToolAttribute), false);
                    foreach (EditorToolAttribute attr in customToolAttributes)
                    {
                        int i = 0;
                        if (attr != null && attr.targetType != null)
                        {
                            i += 1;

                            string typeName = attr.targetType.FullName;
                            if (!toolTypesCache.ContainsKey(typeName))
                            {
                                toolTypesCache[typeName] = new List<Type>();
                            }
                            toolTypesCache[typeName].Add(type);

                            if (type.FullName.Contains("LightProbeGroupTool"))
                            {
                                continue;
                            }
                            if (typeof(ScriptableObject).IsAssignableFrom(type))
                            {
                                try
                                {
                                    ScriptableObject editorToolInstance = ScriptableObject.CreateInstance(type);
                                    EditorTool editorTool = editorToolInstance as EditorTool;

                                    if (editorTool == null || editorToolInstance == null)
                                    {
                                        continue;
                                    }

                                    GUIContent content = new GUIContent();
                                    if (editorTool.toolbarIcon != null)
                                    {
                                        if (editorTool.toolbarIcon.image != null || !string.IsNullOrEmpty(editorTool.toolbarIcon.tooltip))
                                        {
                                            content = new GUIContent(editorTool.toolbarIcon.image, editorTool.toolbarIcon.tooltip);
                                            content.text = editorTool.toolbarIcon.text;
                                        }
                                    }

                                    if (!toolContentsCache.ContainsKey(typeName))
                                    {
                                        toolContentsCache[typeName] = new List<GUIContent>();
                                    }
                                    toolContentsCache[typeName].Add(content);

                                    UnityEngine.Object.DestroyImmediate(editorToolInstance);
                                }
                                catch
                                {
                                    continue;
                                }
                            }
                        }
                    }
                }
            }
        }

        public static EditorTool GetComponentTool(Type componentType)
        {
            if (GetEditorToolManagerType() != null)
            {
                if (GetGetComponentToolMethod() != null)
                {
                    object result = GetGetComponentToolMethod().Invoke(null, new object[] { componentType });

                    return result as EditorTool;
                }
                else
                {
                    //     Debug.LogError("GetComponentTool method not found.");
                }
            }
            else
            {
                //     Debug.LogError("EditorToolManager type not found.");
            }

            return null;
        }

        public static GUIContent GetToolIconForComponent(Component component, Type toolType)
        {
            string typeName = component.GetType().FullName;
            if (toolTypesCache.TryGetValue(typeName, out List<Type> toolTypes) &&
                toolContentsCache.TryGetValue(typeName, out List<GUIContent> toolIcons))
            {
                int index = toolTypes.IndexOf(toolType);
                if (index != -1 && index < toolIcons.Count)
                {
                    return toolIcons[index];
                }
            }
            return null;
        }

        public static GUIContent GetToolIconForType(Type componentType, Type toolType)
        {
            string typeName = componentType.FullName;
            if (toolTypesCache.TryGetValue(typeName, out List<Type> toolTypes) &&
                toolContentsCache.TryGetValue(typeName, out List<GUIContent> toolIcons))
            {
                int index = toolTypes.IndexOf(toolType);
                if (index != -1 && index < toolIcons.Count)
                {
                    return toolIcons[index];
                }
            }
            return null;
        }

        public static bool IsToolAvailableForType(Type _type)
        {
            string typeName = _type.FullName;
            return toolTypesCache.ContainsKey(typeName) && toolTypesCache[typeName].Count > 0;
        }

        public static List<Type> GetToolsForType(Type _type)
        {
            string typeName = _type.FullName;
            if (toolTypesCache.TryGetValue(typeName, out List<Type> toolTypes))
            {
                return toolTypes;
            }
            return null;
        }

        public static bool IsToolAvailableForComponent(Component component)
        {
            string typeName = component.GetType().FullName;

            return toolTypesCache.ContainsKey(typeName) && toolTypesCache[typeName].Count > 0;
        }

        public static EditorTool GetActiveTool()
        {
            return Reflected.CallGetActiveTool();
        }

        public static bool IsToolActiveForComponent(Component component, Type type)
        {
            var activeTool = GetActiveTool();
            if (activeTool != null)
            {
                var activeToolType = activeTool.GetType();
                if (activeToolType == type)
                {
                    EditorTool _activeTool = activeTool;
                    if (_activeTool != null)
                    {
                        var target = _activeTool.target;
                        if (target != null)
                        {
                            if (target is Component)
                            {
                                Component _target = (Component)target;
                                if (_target == component)
                                {
                                    return true;
                                }
                            }
                        }
                    }
                    return false;
                }
            }
            return false;
        }

        public static List<Type> GetToolsForComponent(Component component)
        {
            string typeName = component.GetType().FullName;
            if (toolTypesCache.TryGetValue(typeName, out List<Type> toolTypes))
            {
                return toolTypes;
            }
            return null;
        }

        public static Type GetActiveToolType()
        {
#if UNITY_2020_2_OR_NEWER
            return ToolManager.activeToolType;
#else
        return EditorTools.activeToolType;
#endif
        }

        public static void ResetActiveTool(UnityObject _object)
        {
            Type toolType;
#if UNITY_2020_2_OR_NEWER
            toolType = ToolManager.activeToolType;
#else
        toolType = EditorTools.activeToolType;
#endif

            if (toolType != null && toolType.IsSubclassOf(typeof(EditorTool)))
            {
                EditorTool toolInstance = (EditorTool)ScriptableObject.CreateInstance(toolType);
                //     Debug.Log("Resetting tool " + toolType);
                SetTarget(toolInstance, _object);
                SetActiveTool(toolInstance);
                //SetTarget(toolInstance, _object);
            }
        }

        public static void RestorePreviousPersistentTool()
        {
            ManageActiveTools();
            if (activeTools == null || activeTools.Count == 0)
            {
                return;
            }
            string activeTool = ToolManager.activeToolType.ToString();
            if (activeTool == "UnityEditor.MoveTool" || activeTool == "UnityEditor.RotateTool" || activeTool == "UnityEditor.ScaleTool" || activeTool == "UnityEditor.RectTool" || activeTool == "UnityEditor.TransformTool" || activeTool == "UnityEditor.ViewModeTool")
            {
                return;
            }

#if UNITY_2020_2_OR_NEWER
            ToolManager.RestorePreviousPersistentTool();
#else
#if UNITY_2019_2_OR_NEWER

            EditorTools.RestorePreviousPersistentTool();
#endif
#endif
        }

        public static void SetActiveTool(EditorTool toolInstance)
        {
            if (toolInstance != null)
            {

#if UNITY_2020_2_OR_NEWER
                ToolManager.SetActiveTool(toolInstance);
#else
                        EditorTools.SetActiveTool(toolInstance);
#endif
            }
        }
        public static void ManageActiveTools()
        {
            if (activeTools == null)
            {
                activeTools = new List<EditorTool>();
            }
            for (int i = 0; i < activeTools.Count; i++)
            {
                EditorTool tool = activeTools[i];
                if (tool == null || tool.target == null)
                {
                    activeTools.RemoveAt(i);
                }
            }
        }


        public static void AddToActiveTools(EditorTool tool)
        {
            ManageActiveTools();
            if (tool != null && tool.target != null)
            {
                if (!activeTools.Contains(tool))
                {
                    activeTools.Add(tool);
                }
            }
            else if (tool != null)
            {
                UnityObject.DestroyImmediate(tool);
            }
        }

        public static void ActivateTool(Type toolType, Component component)
        {
            if (toolType != null && toolType.IsSubclassOf(typeof(EditorTool)))
            {

                EditorTool toolInstance = (EditorTool)ScriptableObject.CreateInstance(toolType);
                SetTarget(toolInstance, component);
                SetActiveTool(toolInstance);
                AddToActiveTools(toolInstance);

            }
        }
        public static void ActivateTool(EditorTool tool, Component component)
        {
            if (tool)
            {
                SetTarget(tool, component);
                SetActiveTool(tool);
                AddToActiveTools(tool);
            }
        }

        public static void SetTarget(EditorTool editor, UnityObject target)
        {
            Type editorType = editor.GetType();
            UnityObject[] targets = new UnityObject[] { target };
            UnityObject _target = target;
            FieldInfo targetField = editorType.GetField("m_Target", BindingFlags.NonPublic | BindingFlags.Instance);
            FieldInfo targetsField = editorType.GetField("m_Targets", BindingFlags.NonPublic | BindingFlags.Instance);

            if (targetField != null)
            {
                targetField.SetValue(editor, _target);
            }
            else
            {
                //    Debug.LogError("The field 'm_Target' was not found.");
            }
            if (targetsField != null)
            {
                targetsField.SetValue(editor, targets);
            }

            else
            {
                //     Debug.LogError("The field 'm_Targets' was not found.");
            }
        }

        public static void ClearCache()
        {
            toolTypesCache.Clear();
        }
    }
}