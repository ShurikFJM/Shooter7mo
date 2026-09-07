using System;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using System.Collections.Generic;
using UnityEditor.EditorTools;

namespace CoInspector

{
    internal static class Reflected

    {
#if UNITY_2021_2_OR_NEWER
#else
        private static Type prefabStageUtilityType;
        private static MethodInfo openPrefabMethod;
        private static MethodInfo openPrefabWithInstanceMethod;
#endif
        private static Type addComponentWindowType;
        private static Type inspectorWindowType;
        private static Type avatarPreviewType;
        internal static object timeControlledEditor;
        private static FieldInfo avatarPreviewField;
        private static FieldInfo timeControlField;
        private static PropertyInfo _guiViewCurrentProperty;
        private static PropertyInfo playingProperty;
        private static Dictionary<System.Type, PropertyInfo> _inspectorPropertyCache = new Dictionary<System.Type, PropertyInfo>();
        private static Type propertyWindowType;
        private static Type projectWindowType;
        private static Type prefabImporterType;
        private static Type showLabelType;
        private static Type showAssetBundleNameType;
        private static Type containerWindowType;
        private static Type assetImporterEditorType;
        private static Type timeControlType;
        private static Type componentUtilityType;
        private static Type editorGUIType;
        private static MethodInfo moveComponentMethod;
        private static MethodInfo moveComponentToGameObjectMethod;
        private static MethodInfo setAssetImporterMethod;
        private static MethodInfo openPropertyEditorMethod;
        private static MethodInfo openAddComponentWindowMethod;
        private static MethodInfo getInspectorObjectsMethod;
        private static MethodInfo getPropertyWindowObjectsMethod;
        private static MethodInfo removedTitleBarMethod;
        private static MethodInfo showLabelGUI;
        private static MethodInfo saveChangesMethod;
        private static MethodInfo discardChangesMethod;
        private static MethodInfo showAssetBundleNameMethod;
        private static MethodInfo isMainWindowMethod;
        private static MethodInfo doRectHandlesMethod;
        private static MethodInfo _createSerializedObjectMethod;
        private static MethodInfo _warnMethod;
        private static MethodInfo onHeaderGUIMethod;
        private static MethodInfo getPreViewWindowMethod;
        private static PropertyInfo inspectorModeProperty;
        private static PropertyInfo isInspectorLockedProperty;
        private static PropertyInfo windowsProperty;
        private static PropertyInfo positionProperty;
        private static PropertyInfo canMultiEdit;
        private static PropertyInfo trackerProperty_insp;
        private static PropertyInfo trackerProperty_prop;
        private static PropertyInfo materialForRenderingProperty;
        private static FieldInfo hideInspectorField;
        private static FieldInfo windowsField;
        internal static object labelGUIInstance;
        internal static object assetBundleNameGUIInstance;
        internal static object avatarPreview;
        internal static object timeControl;
        internal static bool timeControlGathered = false;
        //BECAUSE IT TAKES A COUPLE OF FRAMES FOR EDITORS TO ASSIGN THE TIMECONTROL AND AVATARPREVIEW
        private static int gatheringAttempts = 0;
        private const int MAX_GATHERING_ATTEMPTS = 10;


        private static Type ContainerWindowType
        {
            get
            {
                if (containerWindowType == null)
                {
                    containerWindowType = typeof(UnityEditor.EditorWindow).Assembly.GetType("UnityEditor.ContainerWindow");
                    if (containerWindowType == null)
                        throw new Exception("ContainerWindow type not found");
                }
                return containerWindowType;
            }
        }

        private static PropertyInfo WindowsProperty
        {
            get
            {
                if (windowsProperty == null)
                {
                    windowsProperty = ContainerWindowType.GetProperty("windows", BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public);
                }
                return windowsProperty;
            }
        }


        private static FieldInfo WindowsField
        {
            get
            {
                if (windowsField == null)
                {
                    windowsField = ContainerWindowType.GetField("windows", BindingFlags.Static | BindingFlags.NonPublic);
                }
                return windowsField;
            }
        }


        private static MethodInfo IsMainWindowMethod
        {
            get
            {
                if (isMainWindowMethod == null)
                {
                    isMainWindowMethod = ContainerWindowType.GetMethod("IsMainWindow", BindingFlags.Instance | BindingFlags.Public);
                    if (isMainWindowMethod == null)
                        throw new Exception("IsMainWindow method not found");
                }
                return isMainWindowMethod;
            }
        }
        internal static void SetLockState(EditorWindow window, bool locked)
        {
            if (window)
            {
                var isLockedProp = GetIsInspectorLockedPropertyInfo();
                isLockedProp?.GetSetMethod().Invoke(window, new object[] { locked });
            }
        }

        private static PropertyInfo PositionProperty
        {
            get
            {
                if (positionProperty == null)
                {
                    positionProperty = ContainerWindowType.GetProperty("position", BindingFlags.Instance | BindingFlags.Public);
                    if (positionProperty == null)
                        throw new Exception("position property not found");
                }
                return positionProperty;
            }
        }

        internal static Rect GetMainWindowPosition()
        {
            try
            {
                IEnumerable<object> windows;
                if (WindowsProperty != null)
                {
                    windows = WindowsProperty.GetValue(null) as IEnumerable<object>;
                }
                else if (WindowsField != null)
                {
                    windows = WindowsField.GetValue(null) as IEnumerable<object>;
                }
                else
                {
                    throw new Exception("Cannot access windows collection");
                }

                if (windows == null)
                    throw new Exception("Failed to get ContainerWindow instances");

                foreach (var window in windows)
                {
                    bool isMainWindow = (bool)IsMainWindowMethod.Invoke(window, null);
                    if (isMainWindow)
                    {
                        return (Rect)PositionProperty.GetValue(window);
                    }
                }

                throw new Exception("Main window not found");
            }
            catch (Exception e)
            {
                Debug.LogError($"Error in GetMainWindowPosition: {e.Message}");
                return new Rect(0f, 0f, 1000f, 600f);
            }
        }

        internal static bool CanBeMultiEdited(Editor editor)
        {
            if (editor == null)
            {
                return false;
            }
            var result_ = editor.GetType().GetCustomAttributes(typeof(CanEditMultipleObjects), inherit: false);
            if (result_ != null && result_.Length > 0)
            {
                return true;
            }
            Editor testEditor = null;
            bool result = false;
            Editor.CreateCachedEditor(editor.target, null, ref testEditor);
            if (testEditor.GetType().FullName == "UnityEditor.GenericInspector")
            {
                //Debug.Log("Can Multi Edit: " + testEditor.GetType().FullName);
                result = true;
            }
            if (testEditor != null)
            {
                UnityEngine.Object.DestroyImmediate(testEditor);
            }
            return result;

        }


#if UNITY_2021_2_OR_NEWER
#else
        internal static Type GetPrefabStageUtilityType()
        {
            if (prefabStageUtilityType != null)
            {
                return prefabStageUtilityType;
            }
            prefabStageUtilityType = typeof(EditorWindow).Assembly.GetType("UnityEditor.Experimental.SceneManagement.PrefabStageUtility");
            return prefabStageUtilityType;
        }
        internal static MethodInfo GetOpenPrefabMethod()
        {
            if (openPrefabMethod != null)
            {
                return openPrefabMethod;
            }
            Type prefabStageUtilityType = GetPrefabStageUtilityType();
            if (prefabStageUtilityType != null)
            {
                openPrefabMethod = prefabStageUtilityType.GetMethod("OpenPrefab", BindingFlags.NonPublic | BindingFlags.Static, null, new[] { typeof(string) }, null);
                if (openPrefabMethod == null)
                {
                    Debug.LogError("OpenPrefab method not found.");
                }
                return openPrefabMethod;
            }
            else
            {
                return null;
            }
        }

        internal static MethodInfo GetOpenPrefabWithInstanceMethod()
        {
            if (openPrefabWithInstanceMethod != null)
            {
                return openPrefabWithInstanceMethod;
            }
            Type prefabStageUtilityType = GetPrefabStageUtilityType();
            if (prefabStageUtilityType != null)
            {
                openPrefabWithInstanceMethod = prefabStageUtilityType.GetMethod("OpenPrefab", BindingFlags.NonPublic | BindingFlags.Static, null, new[] { typeof(string), typeof(GameObject) }, null);
                if (openPrefabWithInstanceMethod == null)
                {
                    Debug.LogError("OpenPrefab with instance method not found.");
                }
                return openPrefabWithInstanceMethod;
            }
            else
            {
                Debug.LogError("PrefabStageUtility type not found.");
                return null;
            }
        }
        public static object OpenPrefab(string prefabAssetPath)
        {
            MethodInfo method = GetOpenPrefabMethod();
            if (method != null)
            {
                return method.Invoke(null, new object[] { prefabAssetPath });
            }
            else
            {
                Debug.LogError("Failed to invoke OpenPrefab.");
                return null;
            }
        }

        public static object OpenPrefab(string prefabAssetPath, GameObject openedFromInstance)
        {
            MethodInfo method = GetOpenPrefabWithInstanceMethod();
            if (method != null)
            {
                return method.Invoke(null, new object[] { prefabAssetPath, openedFromInstance });
            }
            else
            {
                Debug.LogError("Failed to invoke OpenPrefab with instance.");
                return null;
            }
        }

#endif

        public static bool IsComponentHidden(Component component, bool debug = false)
        {
            if (component == null) return false;

            if ((component.hideFlags & HideFlags.HideInInspector) == HideFlags.HideInInspector || (component.hideFlags & HideFlags.HideAndDontSave) == HideFlags.HideAndDontSave)
            {
                return !debug;
            }
            return false;
        }

        private static Type ComponentUtilityType
        {
            get
            {
                if (componentUtilityType == null)
                {
                    componentUtilityType = typeof(UnityEditor.Editor).Assembly.GetType("UnityEditorInternal.ComponentUtility");
                    if (componentUtilityType == null)
                        throw new Exception("ComponentUtility type not found");
                }
                return componentUtilityType;
            }
        }

        private static MethodInfo MoveComponentMethod
        {
            get
            {
                if (moveComponentMethod == null)
                {
                    moveComponentMethod = ComponentUtilityType.GetMethod(
                        "MoveComponentRelativeToComponent",
                        BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public,
                        null,
                        new Type[] { typeof(Component), typeof(Component), typeof(bool) },
                        null
                    );
                    if (moveComponentMethod == null)
                        throw new Exception("MoveComponentRelativeToComponent method not found.");
                }
                return moveComponentMethod;
            }
        }

        private static MethodInfo MoveComponentToGameObjectMethod
        {
            get
            {
                if (moveComponentToGameObjectMethod == null)
                {
                    moveComponentToGameObjectMethod = ComponentUtilityType.GetMethod(
                        "MoveComponentToGameObject",
                        BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public,
                        null,
                        new Type[] { typeof(Component), typeof(GameObject) },
                        null
                    );
                    if (moveComponentToGameObjectMethod == null)
                        throw new Exception("MoveComponentToGameObject method not found.");
                }
                return moveComponentToGameObjectMethod;
            }
        }

        public static bool MoveComponent(Component target, Component relative, bool moveAbove)
        {
            if (MoveComponentMethod == null)
            {
                throw new Exception("MoveComponentRelativeToComponent method not found.");
            }

            return (bool)MoveComponentMethod.Invoke(null, new object[] { target, relative, moveAbove });
        }

        public static void MoveComponentToPosition(Component component, int targetIndex)
        {
            if (component == null)
            {
                return;
            }

            GameObject gameObject = component.gameObject;
            Component[] components = gameObject.GetComponents<Component>();
            int currentIndex = Array.IndexOf(components, component);
            if (currentIndex == -1)
            {
                Debug.LogError("Component not found on the GameObject.");
                return;
            }

            int relativeOffset = targetIndex - currentIndex;
            if (relativeOffset == 0)
            {
                return;
            }
            MoveComponent(component, components[targetIndex], relativeOffset < 0);
        }

        public static bool MoveComponentToGameObject(Component component, GameObject targetGameObject)
        {
            if (component == null || targetGameObject == null)
            {
                return false;
            }

            if (MoveComponentToGameObjectMethod == null)
            {
                throw new Exception("MoveComponentToGameObject method not found.");
            }

            return (bool)MoveComponentToGameObjectMethod.Invoke(null, new object[] { component, targetGameObject });
        }
        internal static Type GetAssetBundleNameType()
        {
            if (showAssetBundleNameType != null)
            {
                return showAssetBundleNameType;
            }
            else
            {
                showAssetBundleNameType = typeof(EditorWindow).Assembly.GetType("UnityEditor.AssetBundleNameGUI");
                return showAssetBundleNameType;
            }
        }

        internal static Type GetProjectWindowType()
        {
            if (projectWindowType != null)
            {
                return projectWindowType;
            }
            else
            {
                projectWindowType = typeof(EditorWindow).Assembly.GetType("UnityEditor.ProjectBrowser");
                return projectWindowType;
            }
        }


        internal static Type GetAddComponentWindowType()
        {
            if (addComponentWindowType != null)
            {
                return addComponentWindowType;
            }
            else
            {
                addComponentWindowType = typeof(EditorWindow).Assembly.GetType("UnityEditor.AddComponent.AddComponentWindow");
                return addComponentWindowType;
            }
        }

        internal static PropertyInfo GetIsInspectorLockedPropertyInfo()
        {
            if (isInspectorLockedProperty != null)
            {
                return isInspectorLockedProperty;
            }
            if (GetInspectorWindowType() != null)
            {

                isInspectorLockedProperty = GetInspectorWindowType().GetProperty("isLocked", BindingFlags.Instance | BindingFlags.Public);
                if (isInspectorLockedProperty != null)
                {
                    return isInspectorLockedProperty;
                }
                else
                {
                    return null;
                }
            }
            else
            {
                return null;
            }
        }
        internal static EditorWindow GetPreviewWindow(EditorWindow inspector)
        {
            if (GetInspectorWindowType() != null)
            {
                var method = GetInspectorWindowType().GetMethod("GetInspectorPreviewWindow",
                    System.Reflection.BindingFlags.Static |
                    System.Reflection.BindingFlags.Public |
                    System.Reflection.BindingFlags.NonPublic);

                if (method != null)
                {
                    return method.Invoke(null, new object[] { inspector }) as EditorWindow;
                }
            }
            return null;
        }
        internal static MethodInfo GetPreviewWindowMethod()
        {
            if (getPreViewWindowMethod == null)
            {
                getPreViewWindowMethod = GetInspectorWindowType().GetMethod("GetInspectorPreviewWindow",
                System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic);
            }
            return getPreViewWindowMethod;
        }


        internal static MethodInfo GetInspectedObjectsMethod(bool propertyWindow = false)
        {
            if (propertyWindow)
            {
                if (GetPropertyEditorWindowType() == null)
                {
                    getPropertyWindowObjectsMethod = GetPropertyEditorWindowType().GetMethod("GetInspectedObject",
                   BindingFlags.NonPublic | BindingFlags.Instance);
                }
                return getPropertyWindowObjectsMethod;
            }
            if (GetInspectorWindowType() == null)
            {
                getInspectorObjectsMethod = GetInspectorWindowType().GetMethod("GetInspectedObjects",
                    BindingFlags.NonPublic | BindingFlags.Instance);
            }
            return getInspectorObjectsMethod;
        }

        internal static Type GetEditorGUIType()
        {
            if (editorGUIType != null)
            {
                return editorGUIType;
            }
            else
            {
                editorGUIType = typeof(UnityEditor.EditorGUI);
            }
            return editorGUIType;
        }
        private static MethodInfo GetWarnMethod()
        {
            if (_warnMethod == null)
            {
                Type componentUtilityType = Type.GetType("UnityEditor.ComponentUtility, UnityEditor");
                if (componentUtilityType == null)
                {
                    Debug.LogError("ComponentUtility type not found.");
                    return null;
                }

                _warnMethod = componentUtilityType.GetMethod(
                    "WarnCanAddScriptComponent",
                    BindingFlags.NonPublic | BindingFlags.Static
                );

                if (_warnMethod == null)
                {
                    Debug.LogError("WarnCanAddScriptComponent method not found in ComponentUtility.");
                }
            }
            return _warnMethod;
        }

        public static bool CallWarnCanAddScriptComponent(GameObject gameObject, MonoScript script)
        {
            MethodInfo method = GetWarnMethod();
            if (method != null)
            {
                try
                {
                    object result = method.Invoke(null, new object[] { gameObject, script });
                    return result is bool && (bool)result;
                }
                catch (Exception ex)
                {
                    Debug.LogError($"Error invoking WarnCanAddScriptComponent: {ex.Message}");
                }
            }
            return false;
        }
        internal static MethodInfo GetRemovedTitlebarMethod()
        {
            if (removedTitleBarMethod != null)
            {
                return removedTitleBarMethod;
            }
            if (GetEditorGUIType() == null)
            {
                return null;
            }
            removedTitleBarMethod = GetEditorGUIType().GetMethod(
                "RemovedComponentTitlebar",
                BindingFlags.Static | BindingFlags.NonPublic,
                null,
                new System.Type[] { typeof(Rect), typeof(GameObject), typeof(Component) },
                null
            );
            return removedTitleBarMethod;
        }

        public static void RemovedComponentTitlebar(Rect rect, GameObject go, Component comp)
        {
            if (GetRemovedTitlebarMethod() == null)
            {
                return;
            }
            GetRemovedTitlebarMethod().Invoke(null, new object[] { rect, go, comp });
        }
        public static bool ToggleTitlebar(bool foldout, GUIContent label, SerializedProperty property)
        {
            MethodInfo toggleTitlebarMethod = typeof(EditorGUILayout).GetMethod("ToggleTitlebar",
                BindingFlags.NonPublic | BindingFlags.Static,
                null,
                new System.Type[] { typeof(bool), typeof(GUIContent), typeof(SerializedProperty) },
                null);

            if (toggleTitlebarMethod == null)
            {
                Debug.LogError("ToggleTitlebar method not found in EditorGUILayout");
                return foldout;
            }

            return (bool)toggleTitlebarMethod.Invoke(null, new object[] { foldout, label, property });
        }
        public static bool ToggleTitlebar(bool foldout, GUIContent label, ref bool toggleValue)
        {
            var method = typeof(EditorGUILayout).GetMethod("ToggleTitlebar",
                BindingFlags.NonPublic | BindingFlags.Static,
                null,
                new Type[] { typeof(bool), typeof(GUIContent), typeof(bool).MakeByRefType() },
                null);

            object[] parameters = new object[] { foldout, label, toggleValue };
            bool result = (bool)method.Invoke(null, parameters);
            toggleValue = (bool)parameters[2];
            return result;
        }
        public static bool FoldoutTitlebar(bool foldout, GUIContent label, bool skipIconSpacing)
        {
            var method = typeof(EditorGUILayout).GetMethod("FoldoutTitlebar",
                BindingFlags.NonPublic | BindingFlags.Static,
                null,
                new Type[] { typeof(bool), typeof(GUIContent), typeof(bool) },
                null);

            return (bool)method.Invoke(null, new object[] { foldout, label, skipIconSpacing });
        }
        private static PropertyInfo GUIViewCurrentProperty
        {
            get
            {
                if (_guiViewCurrentProperty == null)
                {
                    var guiViewType = typeof(EditorWindow).Assembly.GetType("UnityEditor.GUIView");
                    _guiViewCurrentProperty = guiViewType?.GetProperty("current",
                        BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);
                }
                return _guiViewCurrentProperty;
            }
        }

        internal static bool IsValidGUIContext()
        {
            return GUIViewCurrentProperty?.GetValue(null) != null;
        }

        internal static ActiveEditorTracker GetInspectorTracker(EditorWindow inspectorWindow)
        {
            bool isPropertyWindow = inspectorWindow.GetType() == GetPropertyEditorWindowType();
            if (GetInspectorWindowType() == null || GetTrackerProperty(isPropertyWindow) == null || GetPropertyEditorWindowType() == null)
            {
                return null;
            }

            try
            {
                return GetTrackerProperty(isPropertyWindow).GetValue(inspectorWindow) as ActiveEditorTracker;

            }
            catch
            {
                return null;
            }
        }
        internal static PropertyInfo GetTrackerProperty(bool propertyWindow = false)
        {
            if (propertyWindow)
            {
                Type propertyEditorWindowType = GetPropertyEditorWindowType();
                if (propertyEditorWindowType != null)
                {
                    trackerProperty_prop = propertyEditorWindowType.GetProperty(
                        "tracker",
                        BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static
                    );
                }
                return trackerProperty_prop;
            }

            Type inspectorWindowType = GetInspectorWindowType();
            if (inspectorWindowType != null)
            {
                trackerProperty_insp = inspectorWindowType.GetProperty(
                    "tracker",
                    BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static
                );
            }

            return trackerProperty_insp;
        }
        private static bool GetRectMethod()
        {
            if (doRectHandlesMethod == null)
            {
                Type handlesType = typeof(UnityEditor.Handles);
                doRectHandlesMethod = handlesType.GetMethod("DoRectHandles", BindingFlags.Static | BindingFlags.NonPublic);
            }
            return doRectHandlesMethod != null;
        }

        public static Vector2 DoRectHandles(Quaternion rotation, Vector3 position, Vector2 size, bool handlesOnly)
        {
            if (GetRectMethod())
            {
                try
                {
                    object result = doRectHandlesMethod.Invoke(null, new object[] { rotation, position, size, handlesOnly });
                    return (Vector2)result;
                }
                catch (Exception ex)
                {
                    Debug.LogError("Failed to invoke 'DoRectHandles' method: " + ex.Message);
                    doRectHandlesMethod = null;
                }
            }
            else
            {
                Debug.LogError("Could not find the method 'DoRectHandles' in UnityEditor.Handles");
            }
            return size;
        }

        private static MethodInfo GetCreateSerializedObjectMethod()
        {
            if (_createSerializedObjectMethod == null)
            {
                var inspectorElementType = typeof(UnityEditor.UIElements.InspectorElement);
                _createSerializedObjectMethod = inspectorElementType.GetMethod(
                    "CreateSerializedObjectForTarget",
                    BindingFlags.NonPublic | BindingFlags.Static
                );
            }
            return _createSerializedObjectMethod;
        }



        public static SerializedObject CreateSerializedObjectForComponent(UnityEngine.Object component)
        {
            var method = GetCreateSerializedObjectMethod();
            return method.Invoke(null, new object[] { component }) as SerializedObject;
        }

        internal static UnityEngine.Object[] GetInspectedObjects(EditorWindow inspectorWindow)
        {
            if (GetInspectorWindowType() == null || GetInspectedObjectsMethod() == null || GetPropertyEditorWindowType() == null)
            {
                return null;
            }
            try
            {
                bool isPropertyWindow = inspectorWindow.GetType() == GetPropertyEditorWindowType();
                if (isPropertyWindow)
                {
                    UnityEngine.Object obj = GetInspectedObjectsMethod(isPropertyWindow).Invoke(inspectorWindow, null) as UnityEngine.Object;
                    if (obj != null)
                    {
                        return new UnityEngine.Object[] { obj };
                    }
                }
                return GetInspectedObjectsMethod().Invoke(inspectorWindow, null) as UnityEngine.Object[];
            }
            catch
            {
                return null;
            }
        }

        internal static MethodInfo GetAddComponentWindowShowMethod()
        {
            if (openAddComponentWindowMethod != null)
            {
                return openAddComponentWindowMethod;
            }
            Type windowType = GetAddComponentWindowType();
            if (windowType != null)
            {

                openAddComponentWindowMethod = windowType.GetMethod("Show", BindingFlags.NonPublic | BindingFlags.Static, null, new[] { typeof(Rect), typeof(GameObject[]) }, null);
                return openAddComponentWindowMethod;
            }
            else
            {
                //    Debug.LogError("AddComponentWindow type not found.");
                return null;
            }
        }

        internal static Type GetInspectorWindowType()
        {
            if (inspectorWindowType == null)
            {
                inspectorWindowType = Assembly.Load("UnityEditor").GetType("UnityEditor.InspectorWindow");
            }
            return inspectorWindowType;
        }

#if UNITY_2020_2_OR_NEWER
        internal static void UpdateCurrentApplyRevertMethod(UnityEditor.AssetImporters.AssetImporterEditor importEditor, Editor editor)
#else
        internal static void UpdateCurrentApplyRevertMethod(UnityEditor.Experimental.AssetImporters.AssetImporterEditor importEditor, Editor editor)
#endif
        {
            if (editor == null || importEditor == null)
            {
                return;
            }
            if (assetImporterEditorType == null)
            {
                CacheMethods();
            }
            if (assetImporterEditorType == null)
            {
                assetImporterEditorType = importEditor.GetType();
            }
            if (setAssetImporterMethod == null)
            {
                setAssetImporterMethod = assetImporterEditorType.GetMethod("InternalSetAssetImporterTargetEditor", BindingFlags.NonPublic | BindingFlags.Instance);
            }

            if (setAssetImporterMethod != null)
            {
                setAssetImporterMethod.Invoke(importEditor, new object[] { editor });
            }

        }

        internal static MethodInfo GetShowAssetBundleNameMethod()
        {
            Type labelType = GetAssetBundleNameType();
            if (labelType != null)
            {
                if (showAssetBundleNameMethod != null)
                {
                    return showAssetBundleNameMethod;
                }
                showAssetBundleNameMethod = labelType.GetMethod("OnAssetBundleNameGUI", BindingFlags.Public | BindingFlags.Instance);
                return showAssetBundleNameMethod;
            }
            else
            {
                //     Debug.LogError("AssetBundleNameGUI type not found.");
                return null;
            }
        }
        private static MethodInfo GetOnHeaderGUIMethod()
        {
            if (onHeaderGUIMethod == null)
            {
                onHeaderGUIMethod = typeof(Editor).GetMethod("OnHeaderGUI", BindingFlags.Instance | BindingFlags.NonPublic);
                if (onHeaderGUIMethod == null)
                    Debug.LogError("OnHeaderGUI method not found.");
            }
            return onHeaderGUIMethod;
        }

        public static void OnHeaderGUI(Editor editor)
        {
            if (editor == null) return;

            var method = GetOnHeaderGUIMethod();
            if (method != null)
            {
                try
                {
                    method.Invoke(editor, null);
                }
                catch (System.Reflection.TargetInvocationException tie)
                {
                    if (tie.InnerException is UnityEngine.ExitGUIException)
                        return;

                    Debug.LogError($"Error invoking OnHeaderGUI: {tie.InnerException?.Message}\nStack trace: {tie.InnerException?.StackTrace}");
                }
                catch (UnityEngine.ExitGUIException)
                {
                    return;
                }
                catch (System.Exception ex)
                {
                    Debug.LogError($"Error invoking OnHeaderGUI: {ex.Message}\nStack trace: {ex.StackTrace}");
                }
            }
        }

        public static object GetTransitionPreview(Editor assetEditor)
        {
            if (assetEditor == null) return null;
            var transitionPreviewField = assetEditor.GetType().GetField("m_TransitionPreview", BindingFlags.NonPublic | BindingFlags.Instance);
            if (transitionPreviewField == null) return null;
            return transitionPreviewField.GetValue(assetEditor);
        }

        public static Editor GetModelClipEditor(Editor assetImporterEditor)
        {
            if (assetImporterEditor == null)
            {
                return null;
            }
            var baseType = assetImporterEditor.GetType();
            while (baseType != null && baseType.Name != "AssetImporterTabbedEditor")
            {
                baseType = baseType.BaseType;
            }

            if (baseType == null)
            {
                return null;
            }

            var tabsField = baseType.GetField("m_Tabs",
                BindingFlags.NonPublic | BindingFlags.Instance);

            if (tabsField == null)
            {
                return null;
            }

            var tabsValue = tabsField.GetValue(assetImporterEditor) as System.Array;

            if (tabsValue == null || tabsValue.Length == 0)
            {
                return null;
            }

            foreach (var tab in tabsValue)
            {
                if (tab == null) continue;

                var tabType = tab.GetType();

                if (tabType.Name == "ModelImporterClipEditor")
                {
                    var animationClipEditorField = tabType.GetField("m_AnimationClipEditor",
                        BindingFlags.NonPublic | BindingFlags.Instance);

                    if (animationClipEditorField == null)
                    {
                        continue;
                    }

                    var animationClipEditor = animationClipEditorField.GetValue(tab) as Editor;

                    if (animationClipEditor == null)
                    {
                        continue;
                    }
                    return animationClipEditor;
                }
            }
            return null;
        }
        private static FieldInfo innerEditorField;

        public static void GatherTimeControl(Editor editor, object avatarOwner = null)
        {
            timeControl = null;
            avatarPreview = null;
            avatarPreviewField = null;
            innerEditorField = null;
            timeControlGathered = false;
            ResetTimeControlGathering();

            if (editor == null)
            {
                return;
            }
            if (avatarOwner == null)
            {

                avatarOwner = editor;
            }

            Type editorType = avatarOwner.GetType();

            if (avatarPreviewType == null)
            {

                avatarPreviewType = typeof(Editor).Assembly.GetType("UnityEditor.AvatarPreview");
                if (avatarPreviewType == null)
                {
                    return;
                }
            }

            avatarPreviewField = FindAvatarPreviewField(editorType);

            if (avatarPreviewField == null)
            {

                foreach (var f in editorType.GetFields(BindingFlags.NonPublic | BindingFlags.Instance))
                {
                    if (!typeof(Editor).IsAssignableFrom(f.FieldType))
                    {
                        continue;
                    }

                    var inner = f.GetValue(avatarOwner) as Editor;
                    if (inner == null)
                    {

                        continue;
                    }

                    var found = FindAvatarPreviewField(inner.GetType());
                    if (found != null)
                    {

                        innerEditorField = f;
                        avatarPreviewField = found;
                        break;
                    }
                }
            }

            if (avatarPreviewField == null)
            {
                return;
            }

            timeControlledEditor = avatarOwner;
        }

        private static FieldInfo FindAvatarPreviewField(Type t)
        {
            while (t != null && t != typeof(object))
            {
                foreach (var f in t.GetFields(BindingFlags.NonPublic | BindingFlags.Instance))
                {
                    if (avatarPreviewType.IsAssignableFrom(f.FieldType))
                    {
                        return f;
                    }
                }
                t = t.BaseType;
            }
            return null;
        }

        internal static bool FinishGatheringTimeControl(object editor)
        {
            if (editor == null || avatarPreviewField == null)
            {
                return false;
            }

            // Resolve the actual host of m_AvatarPreview (outer for legacy, inner Editor for 6.x+).
            object host = editor;
            if (innerEditorField != null)
            {
                host = innerEditorField.GetValue(editor);
                if (host == null)
                {
                    return false;
                }
            }

            avatarPreview = avatarPreviewField.GetValue(host);
            if (avatarPreview == null)
            {
                return false;
            }

            if (timeControlField == null)
            {
                timeControlField = avatarPreviewType.GetField("timeControl", BindingFlags.Public | BindingFlags.Instance);
                if (timeControlField == null)
                {
                    return false;
                }
            }
            timeControl = timeControlField.GetValue(avatarPreview);
            if (timeControl == null)
            {
                return false;
            }

            if (playingProperty == null)
            {
                if (timeControlType == null)
                {
                    timeControlType = timeControl.GetType();
                    if (timeControlType == null)
                    {
                        return false;
                    }
                }
                playingProperty = timeControlType.GetProperty("playing", BindingFlags.Public | BindingFlags.Instance);
                if (playingProperty == null)
                {
                    return false;
                }
            }

            timeControlGathered = true;
            return true;
        }

        public static bool IsTimeControlPlaying()
        {
            if (timeControlledEditor == null || avatarPreviewField == null)
            {

                return false;
            }
            if (!timeControlGathered && avatarPreview == null && gatheringAttempts < MAX_GATHERING_ATTEMPTS)
            {

                gatheringAttempts++;
                if (!FinishGatheringTimeControl(timeControlledEditor))
                {
                    return false;
                }
                else
                {
                    gatheringAttempts = 0;
                }
            }
            if (!timeControlGathered || timeControlledEditor == null || timeControl == null || playingProperty == null)
            {
                return false;
            }
            return (bool)playingProperty.GetValue(timeControl, null);
        }
        public static void ResetTimeControlGathering()
        {
            timeControlGathered = false;
            timeControlledEditor = null;
            avatarPreview = null;
            timeControl = null;
            gatheringAttempts = 0;
        }

        private static void CacheMethods()
        {
#if UNITY_2020_2_OR_NEWER
            assetImporterEditorType = typeof(UnityEditor.AssetImporters.AssetImporterEditor);
#else
            assetImporterEditorType = typeof(UnityEditor.Experimental.AssetImporters.AssetImporterEditor);
#endif

#if UNITY_2022_3_OR_NEWER
            saveChangesMethod = assetImporterEditorType.GetMethod("SaveChanges", BindingFlags.Public | BindingFlags.Instance);
            discardChangesMethod = assetImporterEditorType.GetMethod("DiscardChanges", BindingFlags.Public | BindingFlags.Instance);
#else
            saveChangesMethod = assetImporterEditorType.GetMethod("ApplyAndImport", BindingFlags.NonPublic | BindingFlags.Instance);
            discardChangesMethod = assetImporterEditorType.GetMethod("ResetValues", BindingFlags.NonPublic | BindingFlags.Instance);
#endif
        }

        internal static void ApplyChanges(Editor editor)
        {
            if (saveChangesMethod == null || assetImporterEditorType == null)
            {
                CacheMethods();
            }

            if (editor == null || !assetImporterEditorType.IsInstanceOfType(editor))
            {
                return;
            }

            saveChangesMethod?.Invoke(editor, null);
        }

        internal static void DiscardChanges(Editor editor)
        {
            if (discardChangesMethod == null || assetImporterEditorType == null)
            {
                CacheMethods();
            }

            if (editor == null || !assetImporterEditorType.IsInstanceOfType(editor))
            {
                return;
            }

            discardChangesMethod?.Invoke(editor, null);
        }

        internal static void ShowAddComponentWindow(Rect rect, GameObject[] gameObjects)
        {
            MethodInfo _showMethod = GetAddComponentWindowShowMethod();
            if (_showMethod != null)
            {
                _showMethod.Invoke(null, new object[] { rect, gameObjects });
            }
            else
            {
                //    Debug.LogError("AddComponentWindow.Show method not found.");
            }
        }

        internal static bool ComponentHasEditorTool(Component component)
        {
            return EditorToolCache.IsToolAvailableForComponent(component);
        }


        internal static Type GetPrefabImporterType()
        {
            if (prefabImporterType != null)
            {
                return prefabImporterType;
            }
            else
            {
                prefabImporterType = typeof(EditorWindow).Assembly.GetType("UnityEditor.PrefabImporterEditor");
                return prefabImporterType;
            }
        }

        private static Type editorToolManagerType = null;
        private static MethodInfo getActiveToolMethod = null;

        internal static Type GetEditorToolManagerType()
        {
            if (editorToolManagerType != null)
            {
                return editorToolManagerType;
            }
            else
            {
                editorToolManagerType = typeof(EditorWindow).Assembly.GetType("UnityEditor.EditorTools.EditorToolManager");
                return editorToolManagerType;
            }
        }

        public static MethodInfo onSceneGUIMethod = null;

        public static void _OnSceneGUI(Editor editor)
        {
            onSceneGUIMethod = editor.GetType().GetMethod("OnSceneGUI", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
            if (onSceneGUIMethod != null)
            {
                //   Debug.Log("OnSceneGUI method was found.");
                onSceneGUIMethod.Invoke(editor, null);
            }

        }

        internal static MethodInfo GetActiveToolMethod()
        {
            if (getActiveToolMethod != null)
            {
                return getActiveToolMethod;
            }
            else
            {
                Type toolManagerType = GetEditorToolManagerType();
                if (toolManagerType != null)
                {
                    getActiveToolMethod = toolManagerType.GetMethod("GetActiveTool", BindingFlags.Public | BindingFlags.Static);
                    if (getActiveToolMethod == null)
                    {
                        //    Debug.LogError("GetActiveTool method not found.");
                    }
                    return getActiveToolMethod;
                }
                else
                {
                    //    Debug.LogError("EditorToolManager type not found.");
                    return null;
                }
            }
        }

        public static EditorTool CallGetActiveTool()
        {
            MethodInfo method = GetActiveToolMethod();
            if (method != null)
            {
                return (EditorTool)method.Invoke(null, null);
            }
            else
            {
                //    Debug.LogError("Failed to invoke GetActiveTool.");
                return null;
            }
        }

        internal static Type GetPropertyEditorWindowType()
        {
            if (propertyWindowType != null)
            {
                return propertyWindowType;
            }
            else
            {
                propertyWindowType = typeof(EditorWindow).Assembly.GetType("UnityEditor.PropertyEditor");
                return propertyWindowType;
            }
        }

        internal static Type GetAssetLabelsType()
        {
            if (showLabelType != null)
            {
                return showLabelType;
            }
            else
            {
                showLabelType = typeof(EditorWindow).Assembly.GetType("UnityEditor.LabelGUI");
                return showLabelType;
            }
        }

        internal static MethodInfo GetShowAssetLabelMethod()
        {
            Type labelType = GetAssetLabelsType();
            if (labelType != null)
            {
                if (showLabelGUI != null)
                {
                    return showLabelGUI;
                }
                showLabelGUI = GetAssetLabelsType().GetMethod("OnLabelGUI", BindingFlags.Public | BindingFlags.Instance);
                return showLabelGUI;
            }
            else
            {
                //     Debug.LogError("LabelGUI type not found.");
                return null;
            }
        }

        internal static MethodInfo GetPropertyEditorWindowShowMethod()
        {
            Type windowType = GetPropertyEditorWindowType();
            if (windowType != null)
            {
                if (openPropertyEditorMethod != null)
                {
                    return openPropertyEditorMethod;
                }
                openPropertyEditorMethod = windowType.GetMethod("Show", BindingFlags.NonPublic | BindingFlags.Static, null, new[] { typeof(UnityEngine.Object) }, null);
                return openPropertyEditorMethod;

            }
            else
            {
                //    Debug.LogError("PropertyEditorWindow type not found.");
                return null;
            }
        }
        internal static void ShowPropertyEditorWindow(UnityEngine.Object obj, bool showWindow = true)
        {

            MethodInfo showMethod = GetPropertyEditorWindowShowMethod();

            if (showMethod != null)
            {
                showMethod.Invoke(null, new object[] { obj, showWindow });
            }
            else
            {
                //     Debug.LogError("PropertyEditorWindow.Show method not found.");
            }
        }
        internal static void SetHideInspector(Editor editor, bool hideInspector)
        {
            if (GetHideInspectorFieldInfo() != null)
            {
                GetHideInspectorFieldInfo().SetValue(editor, hideInspector);
            }
            else
            {
                //     Debug.LogError("Could not find the 'hideInspector' field.");
            }
        }

        internal static FieldInfo GetHideInspectorFieldInfo()
        {
            if (hideInspectorField != null)
            {
                return hideInspectorField;
            }
            hideInspectorField = typeof(Editor).GetField("hideInspector", BindingFlags.NonPublic | BindingFlags.Instance);
            return hideInspectorField;
        }

        internal static PropertyInfo GetInspectorModePropInfo()
        {
            if (inspectorModeProperty != null)
            {
                return inspectorModeProperty;
            }
            inspectorModeProperty = typeof(Editor).GetProperty("inspectorMode", BindingFlags.NonPublic | BindingFlags.Instance);
            return inspectorModeProperty;
        }

        internal static void SetInspectorMode(Editor editor, InspectorMode mode)
        {

            if (GetInspectorModePropInfo() != null)
            {
                GetInspectorModePropInfo().SetValue(editor, mode, null);
            }
            else
            {
                //    Debug.LogError("Could not find the 'inspectorMode' property.");
            }
        }
        public static Material GetTMPMaterialForRendering(Component component)
        {
            if (component == null) return null;

            if (materialForRenderingProperty == null)
            {
                materialForRenderingProperty = component.GetType().GetProperty("materialForRendering");
            }

            try
            {
                return materialForRenderingProperty?.GetValue(component) as Material;
            }
            catch
            {
                return null;
            }
        }

    }
}