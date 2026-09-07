using UnityEngine;
using UnityEditor;
using System;
using System.Linq;
using System.Reflection;
using System.Collections.Generic;
using UnityObject = UnityEngine.Object;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;
using System.Text;
using System.IO;
using UnityEditorInternal;
using UnityEngine.Video;
using UnityEngine.Audio;
using UnityEditor.UIElements;
using static CoInspector.EditorUtils;
#if UNITY_2021_2_OR_NEWER
#else
using UnityEditor.Experimental.SceneManagement;
#endif
namespace CoInspector
{
    public class CoInspectorWindow : EditorWindow, IHasCustomMenu
    {
        //RUNTIME SETTINGS
        internal static bool hideUpdateAlert = false;
        internal static bool recycleUnlockedTabs = false;
        internal static string[] userInstalls = new string[0];
        internal static bool tabPreviewExpanded = true;
        internal static bool softWheelScrolling = true;
        internal static int middleScrollMode = 1;
        internal static bool newTabIfLocked = false;
        internal static bool showHistory = true;
        internal static bool showTabName = true;
        internal static bool showTabTree = true;
        internal static bool showFilterBar = true;
        internal static bool showHierarchyButton = true;
        internal static bool showInspectorButton = true;
        internal static bool showFocusButton = true;
        internal static bool showSelectButton = true;
        internal static bool hideEmptyComponents = false;
        internal static bool showInstallMessage = true;
        internal static bool showSceneToolsMessage = true;
        internal static bool softSelection = true;
        internal static bool showIcons = true;
        internal static bool richNames = true;
        internal static bool autoFocus = false;
        internal static bool showScrollBar = true;
        internal static bool showAdditionalOptions = true;
        internal static bool rememberSessions = true;
        internal static bool useThumbKeys = true;
        internal static bool ignoreFolders = true;
        internal static bool collapsePrefabComponents = true;
        internal static bool openPrefabsInNewTab = true;
        internal static bool showTextAssetPreviews = true;
        internal static bool showAssetLabels = false;
        internal static bool showCollapseTool = true;
        internal static bool showLastClicked = true;
        internal static bool showMostClicked = true;
        internal static bool showMaximizeButton = false;
        internal static bool assetInspection = true;
        internal static bool componentCulling = true;
        internal static bool reuseTabs = false;
        internal static bool overrideSceneTools = false;
        internal static bool assetPreviewExpanded = true;
        internal static int sessionsMode = 0;
        internal static int tabCompactMode = 1;
        internal static int doubleClickMode = 1;
        internal static int scrollSpeedX = 2;
        internal static int scrollSpeedY = 2;
        internal static int scrollDirectionX = 1;
        internal static int scrollDirectionY = 1;
        internal static int mouseWheelSensitivity = 18;
        internal static int mouseWheelSpeed = 1;


        //SETTINGS ASSET
        [SerializeField] internal UserSaveData settingsData;

        //EDITOR LOGIC
        private Editor gameObjectEditor;
        private Editor[] materialEditors;
        private Editor[] prefabMaterialEditors;
        private Editor[] componentEditors;
        private Editor assetEditor;
        private VisualElement headerBox;
        private VisualElement gameObjectBox;
        private VisualElement addComponentBox;
        private VisualElement assetBox;
        private VisualElement assetImportSettings;
        private VisualElement footerBar;
        internal VisualElement cursorOverlay = null;
        internal SmoothScrollView componentScrollView;
        internal ScrollView middleDragScrollView = null;
        internal bool isScheduled = false;
        internal bool differentComponents = false;
        internal bool differentPrefabComponents = false;
        internal bool shouldFocusFilter = false;
        internal bool editingHeader = false;
        internal bool middleScrolling = false;
        internal bool movedMinumim = false;
        internal int editingHeaderID = -1;
        internal SmoothScrollView assetScrollView;
        private Editor[] prefabEditors;
#if UNITY_2020_2_OR_NEWER
        private UnityEditor.AssetImporters.AssetImporterEditor assetImportSettingsEditor;
#else
        private UnityEditor.Experimental.AssetImporters.AssetImporterEditor assetImportSettingsEditor;
#endif
        private AssetImporter assetImporter;
        private AssetImporter[] assetImporters;
        private Dictionary<Editor, MethodInfo> sceneMethods;
        private bool[] componentFoldouts_;
        private bool[] prefabFoldouts_;
        private bool[] prefabFoldoutsChangeTracker_;
        static internal string rootPath;
        private List<DelayedAction> methodsToRun = new List<DelayedAction>();
        private List<Action> pendingActions = new List<Action>();
        internal List<SerializableTransform> playModeTransforms;
        private MaterialMapManager prefabMaterialManager = new MaterialMapManager();
        private ComponentMapManager prefabComponentManager = new ComponentMapManager();
        internal Rect flexibleRect;
        internal Rect headerRect;
        private Vector2 addComponentVector = new Vector2(230, 350);
        private Vector2 originalMiddleScrollPosition = Vector2.zero;
        internal Vector2 currentMiddleScrollPosition = Vector2.zero;
        internal Vector2 middleScrollDelta = Vector2.zero;
        internal GameObjectTracker tracker;
        internal string currentTip = "";

        //DRAG AND RESIZE LOGIC
        internal bool assetsCollapsed = false;
        internal bool hoveringResize = false;
        internal bool canProceedWithDrag = false;
        internal int maximizeMode = 0;
        private RectOffset _paddingIcon;
        private RectOffset _paddingNoIcon;
        internal FloatingTab floatingTab;
        internal float lastValidTabWidth = 0;
        internal float suggestedHeight = 0;
        internal float userHeight = 0;
        internal float rawUserHeight = 0;
        internal float maxAssetViewSize = 0;
        [SerializeField] internal float userAssetViewSize = -1;
        internal bool alreadyCalculatedHeight = false;
        internal float lastKnownHeight = 0;
        private bool pendingComponentDrag = false;
        internal ComponentDragOperation pendingOperation;
        private bool dragging = false;
        private bool triggeringADrag = false;
        private bool enteredSafeZone = false;
        [SerializeField] internal bool GOdragging = false;
        private int dragIndex = -1;
        private int dragTargetIndex = -1;
        private Rect dragRect;
        private Rect assetViewRect;
        internal Rect leftHandleRect;
        internal Rect rightHandleRect;
        private Vector2 mousePositionOnClick;
        private Vector2 mousePositionOnAssetBarClick;
        private bool waitingToDrag = false;
        private float resizeOriginalCursorY;
        private float startHeight = 0;
        private bool resizingAssetView = false;
        private bool ignoreNextDragEvent = false;
        internal static bool alreadyMovingComponent = false;
        private int performPasteComponent = 0;

        //RUNTIME TARGETS AND SELECTION
        internal GameObject targetGameObject;
        internal UnityObject targetObject;
        internal List<List<GameObject>> lastClicked;
        internal List<List<GameObject>> mostClicked;
        [SerializeField] internal UnityObject[] targetObjects;
        private UnityObject[] ignoreSelection;
        private Dictionary<Type, List<UnityObject>> sortedAssetSelection;
        [SerializeField] internal bool forceSelection = false;
        [SerializeField] internal bool ignoreNextSelection = false;

        //TABS
        [SerializeField] internal List<TabInfo> tabs;
        [SerializeField] internal List<TabInfo> closedTabs;
        [SerializeField] internal TabSession lastSessionData;
        [SerializeField] internal List<HistoryAssets> historyAssets;
        [NonSerialized] internal float barSpacing = -1;
        internal int activeIndex = 0;
        internal int lastActiveIndex = -1;
        private Rect scrollRect;
        private Rect activeTabRect;
        private int pendingTabSwitch = -1;
        private Vector2 mousePosition = Vector2.zero;
        private Vector2 clickMousePosition = Vector2.zero;


        //STYLES   
        private GUIStyle lockButtonStyle;

        //RUNTIME VARIABLES
        [SerializeField] internal bool lockedAsset = false;
        [SerializeField] internal bool assetOnlyMode = false;
        [SerializeField] internal bool filteringComponents = false;
        internal bool showImportSettings = false;
        private bool isRepainting = false;
        private float dirtyDuration = 0f;
        private float startTime = 0f;
        internal static List<CoInspectorWindow> instances = new List<CoInspectorWindow>();
        internal static float lastOpen = -1;
        internal static bool textPluginPresent = false;
        internal static bool odinInspectorPresent = false;
        internal static bool ueePresent = false;
        [SerializeField] internal bool pendingRestore = false;
        [SerializeField] internal bool exitingPlayMode = false;
        [SerializeField] internal bool enteringPlayMode = false;
        [SerializeField] internal bool scenesChanged = false;
        [SerializeField] internal bool changingScenes = false;
        private static List<string> namespaces = new List<string> { "ScriptInspector" };
        internal static SceneInfo activeScene;
        internal static string lastValidScenePath = "";
        private bool switchingTabs = false;
        [SerializeField] internal bool debugAsset = false;
        [SerializeField] internal bool globalDebugMode = false;
        private bool awaitingAssetClick = false;
        private bool onPrefabSceneMode = false;
        [SerializeField] internal Vector2 scrollPosition;
        [SerializeField] internal float forceScroll = 0;
        [NonSerialized] internal bool toolScrollBarVisible = false;
        [SerializeField] internal Vector2 toolbarScrollPosition;
        private List<float> totalWidth = new List<float>();
        internal Rect previewRect = new Rect(0, 0, 0, 0);
        internal static bool justOpened = false;
        [SerializeField] internal bool isLocked = true;
        [SerializeField] internal int lastClickedTab = -1;
        [SerializeField] internal TabInfo previousTab;
        private float lastTabClick = -1;
        private float lastChangeOfState = 0;
        [SerializeField] internal bool inActualPlayMode = false;

        [MenuItem("GameObject/★ Open Selection in a New Tab", false, 0)]
        public static void OpenSelectionInNewTab()
        {
            if (Time.realtimeSinceStartup - lastOpen < 0.5f && lastOpen != -1)
            {
                return;
            }
            CoInspectorWindow insp = null;

            if (MainCoInspector)
            {
                insp = MainCoInspector;
            }
            else
            {
                ShowWindow();
                if (MainCoInspector)
                {
                    insp = MainCoInspector;
                }
            }
            if (insp != null)
            {
                lastOpen = Time.realtimeSinceStartup;
                EditorApplication.delayCall += () =>
                {
                    if (Selection.gameObjects.Length == 0)
                    {
                        return;
                    }

                    if (Selection.gameObjects.Length == 1)
                    {
                        insp.AddTabNext(Selection.gameObjects[0], false);
                        return;
                    }

                    if (Selection.gameObjects.Length > 1)
                    {
                        insp.AddMultiTabNext(Selection.gameObjects, false);
                    }
                };
            }
        }

        [MenuItem("GameObject/★ Open Selection in a New Tab", true)]
        internal static bool ValidateOpenSelectionInNewTab()
        {
            return Selection.activeGameObject != null;
        }
        [MenuItem("Window/CoInspector/Open CoInspector")]
        public static void ShowWindow()
        {
            justOpened = true;
            CoInspectorWindow insp = GetWindow<CoInspectorWindow>("CoInspector");
            insp.titleContent = new GUIContent("CoInspector", CustomGUIContents.MainIconImage);
            Vector2 middle = FirstInstallWindow.RightSideOfScreen(450, 700);
            insp.position = new Rect(middle.x, middle.y, 450, 700);
            RegisterWindow(insp);
            insp.Focus();
        }
        [MenuItem("CONTEXT/Component/Collapse All/Collapse All Components", false, 1000)]
        public static void CollapseAllComponents(MenuCommand command)
        {
            Component component = (Component)command.context;
            bool isPrefab = EditorUtils.IsAPrefabAsset(component.gameObject);
            if (component)
            {
                if (MainCoInspector)
                {
                    MainCoInspector.SetAllComponentsTo(false, null, isPrefab);
                }
            }
        }
        [MenuItem("CONTEXT/Component/Expand All/Expand All Components", false, 1000)]
        public static void ExpandAllComponents(MenuCommand command)
        {
            if (Time.realtimeSinceStartup - lastOpen < 0.5f && lastOpen != -1)
            {
                return;
            }
            Component component = (Component)command.context;
            bool isPrefab = EditorUtils.IsAPrefabAsset(component.gameObject);
            if (component)
            {
                if (MainCoInspector)
                {
                    MainCoInspector.SetAllComponentsTo(true, null, isPrefab);
                }
            }
        }
        [MenuItem("CONTEXT/Component/Move to Top", false, 0)]
        public static void MoveComponentToFirst(MenuCommand command)
        {
            Component component = (Component)command.context;


            if (component)
            {
                Reflected.MoveComponentToPosition(component, 1);
                FocusComponentIfNecessary(component);
            }
        }
        static void FocusComponentIfNecessary(Component component)
        {
            if (!component)
            {
                return;
            }
            bool isPrefab = EditorUtils.IsAPrefabAsset(component.gameObject);

            if (MainCoInspector)
            {
                if (!isPrefab)
                {
                    MainCoInspector.rootVisualElement.schedule.Execute((Action)(() =>
                        {
                            MainCoInspector.ReinitializeComponentEditors();
                            MainCoInspector.componentScrollView.ScrollToTop();
                            MainCoInspector.componentScrollView.FocusChild("Component1");
                        }));
                }
                else
                {
                    MainCoInspector.ReinitializePrefabComponentEditors();
                    MainCoInspector.assetScrollView.ScrollToTop();
                    MainCoInspector.assetScrollView.FocusChild("Component1");
                }
            }
        }
        [MenuItem("CONTEXT/Component/Move to Top", true)]
        internal static bool ValidateMoveComponentToFirst(MenuCommand command)
        {
            Component component = (Component)command.context;
            if (component == null)
            {
                return false;
            }
            Component[] components = component.GetComponents<Component>();
            if (components.Length > 1 && components[1] != component && components[0] != component)
            {
                return true;
            }
            return false;
        }
        [MenuItem("CONTEXT/Component/Expand All/Expand All But This", false, 1000)]
        internal static void ExpandAllButThis(MenuCommand command)
        {
            if (Time.realtimeSinceStartup - lastOpen < 0.5f && lastOpen != -1)
            {
                return;
            }
            Component component = (Component)command.context;
            bool isPrefab = EditorUtils.IsAPrefabAsset(component.gameObject);
            if (component)
            {
                if (MainCoInspector)
                {
                    MainCoInspector.SetAllComponentsTo(true, component, isPrefab);
                }
            }
        }
        [MenuItem("CONTEXT/Component/Expand All/Expand All But This", true)]
        internal static bool ValidateExpandAllButThis(MenuCommand command)
        {
            return true;
        }
        [MenuItem("CONTEXT/Component/Collapse All/Collapse All But This", true)]
        internal static bool ValidateCollapseAllButThis(MenuCommand command)
        {
            return true;
        }
        [MenuItem("CONTEXT/Component/Collapse All/Collapse All Components", true)]
        internal static bool ValidateCollapseAllComponents(MenuCommand command)
        {
            if (EditorWindow.focusedWindow.GetType() != typeof(CoInspectorWindow))
            {
                return true;
            }
            Component component = (Component)command.context;
            if (!EditorUtils.IsAPrefabAsset(component.gameObject))
            {
                if (MainCoInspector)
                {
                    return !MainCoInspector.GetActiveTab().AreAllCollapsed();
                }
            }
            else
            {
                if (MainCoInspector)
                {
                    return !MainCoInspector.PrefabComponentMapManager.AreAllCollapsed(MainCoInspector.debugAsset);
                }
            }
            return false;
        }
        [MenuItem("CONTEXT/Component/Expand All/Expand All Components", true)]
        internal static bool ValidateExpandAllComponents(MenuCommand command)
        {
            if (EditorWindow.focusedWindow.GetType() != typeof(CoInspectorWindow))
            {
                return true;
            }
            Component component = (Component)command.context;
            if (!EditorUtils.IsAPrefabAsset(component.gameObject))
            {
                if (MainCoInspector)
                {
                    return !MainCoInspector.GetActiveTab().AreAllExpanded();
                }
            }
            else
            {
                if (MainCoInspector)
                {
                    return !MainCoInspector.PrefabComponentMapManager.AreAllExpanded();
                }
            }
            return false;
        }
        [MenuItem("CONTEXT/Component/Collapse All/Collapse All But This", false, 1000)]
        internal static void CollapseAllButThis(MenuCommand command)
        {
            if (Time.realtimeSinceStartup - lastOpen < 0.5f && lastOpen != -1)
            {
                return;
            }
            Component component = (Component)command.context;
            bool isPrefab = EditorUtils.IsAPrefabAsset(component.gameObject);
            if (component)
            {
                if (MainCoInspector)
                {
                    MainCoInspector.SetAllComponentsTo(false, component, isPrefab);
                }
            }
        }

        void HandleGUIEvents()
        {
            DrawEventsContainer();
            if (tabs != null && tabs.Count > 0)
            {
                UpdateTabBar();
            }
        }
        void DrawEventsContainer()
        {
            RunDelayedMethods();
            FixActiveIndex();
            if (changingScenes || (exitingPlayMode && scenesChanged))
            {
                HandleAssetViewResize();
                return;
            }
        }

        void DrawTabBarContainer()
        {
            /*
            if (EditorGUIUtility.editingTextField && Event.current != null && Event.current.type == EventType.MouseDown)
            {
                GUIUtility.hotControl = 0;
                GUIUtility.keyboardControl = 0;
                rootVisualElement.MarkDirtyRepaint();
            } */
            if (tabs != null && tabs.Count > 0)
            {
                TranslateIMGUIEvents();
                HandleGUIEvents();
                HandleTabDragging();
                DrawTabBar();
                DrawScrollBar();
            }
        }
        void DrawHeaderContainer()
        {
            if (IsActiveTabNew())
            {
                headerRect = new Rect(0, 35, position.width, 20);
                return;
            }
            else if (GetActiveTab().target || IsActiveTabValidMulti())
            {
                DrawHeader();
            }
            if (headerRect.height > 1)
            {
                clickMousePosition.y = headerRect.height;
            }
        }
        void DrawTabBarDecorationsContainer()
        {
            if (!InRecoverScreen())
            {
                DrawActiveTabUnder();
                if (toolbarScrollPosition.x > 0)
                {
                    Rect rect = showHistory ? PoolCache.historyMarginRect : PoolCache.marginRect;
                    EditorGUI.DrawRect(new Rect(rect.x - 1, rect.y, 1, rect.height), CustomColors.MediumShadow);
                    if (EditorUtils.IsLightSkin())
                    {
                        rect.x -= 1;
                        EditorUtils.DrawFadeToRight(rect, CustomColors.SimpleBright);
                    }
                    else
                    {
                        EditorUtils.DrawFadeToRight(rect, CustomColors.GradientShadow);
                    }
                }
                if (toolbarScrollPosition.x < GetMaximumScroll())
                {
                    Rect rect = new Rect(position.width - 24 - 20, 1, 20, 22);
                    if (EditorUtils.IsLightSkin())
                    {
                        rect.x += 1;
                        EditorUtils.DrawFadeToLeft(rect, CustomColors.SimpleBright);
                    }
                    else
                    {
                        EditorUtils.DrawFadeToLeft(rect, CustomColors.GradientShadow);
                    }
                }
                HandleFloatingTabInBar();
            }
        }

        void DrawMultiHeaderUnderContainer()
        {
            Rect multiHeaderRect = Rect.zero;
            if (IsActiveTabValidMulti())
            {
                GUILayout.BeginHorizontal();
                GUILayout.Space(0);
                GUILayout.EndHorizontal();
                Rect rect = GUILayoutUtility.GetLastRect();

                if (GetActiveTab().multiFoldout)
                {
                    EditorUtils.DrawLineUnderRect(rect, CustomColors.SoftShadow, 1, 4);
                    GUILayout.Space(2);
                }
                DrawUnderMultiObjectHeader(rect);
                if (!GetActiveTab().multiFoldout)
                {
                    GUILayout.Space(0);
                    return;
                }
                multiHeaderRect = GUILayoutUtility.GetLastRect();
                multiHeaderRect.y = GetActiveTab().multiFoldout ? 2 : 4;
                GUILayout.Space(2);
            }
        }

        void RebuildGameObjectView()
        {
            gameObjectBox = new VisualElement();
            gameObjectBox.name = "GameObjectBox";
            gameObjectBox.style.flexDirection = FlexDirection.Column;
            gameObjectBox.style.overflow = Overflow.Visible;
            gameObjectBox.pickingMode = PickingMode.Ignore;
            componentScrollView = new SmoothScrollView(ScrollViewMode.Vertical);
            componentScrollView.style.overflow = Overflow.Visible;
            componentScrollView.verticalScrollerVisibility = ScrollerVisibility.Auto;
            componentScrollView.style.flexGrow = 1;
            componentScrollView.style.flexShrink = 1;
            componentScrollView.style.flexBasis = 0;
            componentScrollView.pickingMode = PickingMode.Ignore;
            componentScrollView.elasticity = 0;
            componentScrollView.name = "ComponentScrollView";
            componentScrollView.mouseWheelScrollSize = mouseWheelSensitivity;
            //componentScrollView.ApplySoftScrolling(this);
            VisualElement gameObjectView = new VisualElement();
            gameObjectView.style.flexGrow = 1;
            gameObjectView.style.flexShrink = 1;
            gameObjectView.style.overflow = Overflow.Visible;
            gameObjectView.pickingMode = PickingMode.Ignore;
            IMGUIContainer drawHeaderShadow = new IMGUIContainer(DrawHeaderShadowContainer);
            EditorUtils.SetStaticElement(drawHeaderShadow);
            drawHeaderShadow.style.position = Position.Absolute;
            componentScrollView.Add(gameObjectBox);
            IMGUIContainer drawLastComponent = new IMGUIContainer(() =>
            {
                DrawLastComponentLine();
            });
            drawLastComponent.style.position = Position.Relative;
            EditorUtils.SetStaticElement(drawLastComponent);
            componentScrollView.Add(drawLastComponent);
            gameObjectView.Add(componentScrollView);
            gameObjectView.Add(drawHeaderShadow);
            gameObjectView.name = "GameObjectView";
            if (!rootVisualElement.Contains(gameObjectView))
            {
                rootVisualElement.Add(gameObjectView);
            }
            drawHeaderShadow.PlaceInFront(componentScrollView);
        }

        void SetComponentViewVisibility()
        {
            foreach (VisualElement element in rootVisualElement.Children())
            {
                EditorUtils.SetElementVisible(element, !assetOnlyMode);
                if (element.name == "AddComponentBox")
                {
                    break;
                }
            }
            EditorUtils.SetElementVisible(rootVisualElement.GetChild("Header"), !assetOnlyMode);
            if (assetOnlyMode)
            {
                assetBox.style.flexGrow = 1;
                assetBox.style.flexShrink = 1;
                assetScrollView.style.flexGrow = 1;
                assetScrollView.style.flexShrink = 1;
            }
            else
            {
                rootVisualElement.MarkDirtyRepaint();
            }
        }

        void DrawCurrentComponentsContainer(bool skipHeader = false)
        {
            FixActiveIndex();
            if (gameObjectBox == null || componentScrollView == null)
            {
                return;
            }
            if (componentEditors == null)
            {
                componentEditors = new Editor[0];
            }
            componentScrollView.Reset();
            addComponentBox?.Remove("Preview");
            GetActiveTab().hasPreview = false;
            GetActiveTab().TriggerMaterialRebuild();
            scrollPosition.y = GetActiveTab().scrollPosition;
            componentScrollView.LimitScrollbarVisibilityTo();
            gameObjectBox.Clear();
            componentScrollView.style.flexDirection = FlexDirection.Column;
            if (!skipHeader)
            {
                RebuildHeaderContainer();
            }
            if (IsActiveTabNew())
            {
                rootVisualElement.MarkDirtyRepaint();
                RefreshAllTabNames();
                SetScrollPosition(0);
                return;
            }

            bool oneVisible = false;
            VisualElement multiComponentFooter = null;
            if (!IsActiveTabValidMulti())
            {
                if (GetActiveTab() == null || GetActiveTab().target == null)
                {
                    return;
                }
                Component[] components = GetActiveTab().target.GetComponents<Component>();
                List<RemovedComponent> removedComponents = null;
                if (PrefabUtility.IsPartOfPrefabInstance(GetActiveTab().target))
                {
                    removedComponents = PrefabUtility.GetRemovedComponents(GetActiveTab().target);
                    if (removedComponents != null && removedComponents.Count > 0)
                    {
                        GetActiveTab().removedComponents = new List<RemovedComponent>(removedComponents);
                        removedComponents = removedComponents.Where(c =>
                            c != null &&
                            c.containingInstanceGameObject != null &&
                            GetActiveTab() != null &&
                            GetActiveTab().target != null &&
                            c.assetComponent != null &&
EditorUtils.GetObjectId(c.containingInstanceGameObject) ==
                            EditorUtils.GetObjectId(GetActiveTab().target) &&
                            EditorUtils.CanAddMultipleTimes(c.assetComponent.GetType(), GetActiveTab().target)
                        ).ToList();
                    }
                }
                GetActiveTab().CleanMap(components);
                Editor[] runtimeEditors = componentEditors;
                int componentCount = runtimeEditors.Length;
                int currentIndex = 0;
                while (currentIndex < componentCount)
                {
                    Editor editor = runtimeEditors[currentIndex];
                    if (editor == null && components[currentIndex] == null)
                    {
                        ComponentMap emptyMap = new ComponentMap();
                        emptyMap.index = currentIndex;
                        emptyMap.missingComponentID = EditorUtils.GetMissingComponentID(gameObjectEditor.serializedObject, currentIndex);
                        GetActiveTab().componentMaps.Add(emptyMap);
                        VisualElement missingComponent = new IMGUIContainer(() =>
                        {
                            ShowMissingComponent(false);
                        });
                        EditorUtils.SetStaticElement(missingComponent);
                        missingComponent.name = "ComponentElement" + currentIndex;
                        gameObjectBox.Add(missingComponent);
                        currentIndex++;
                        continue;
                    }
                    if (editor != null)
                    {

                        VisualElement componentBox = DrawSingleComponent(editor, currentIndex, componentCount, components);
                        if (componentBox != null)
                        {
                            EditorUtils.SetStaticElement(componentBox);
                            if (!oneVisible && AreWeFilteringComponents())
                            {
                                oneVisible = IsComponentFilteredInTab(GetActiveTab().GetFoldoutMapForComponent(components[currentIndex], editor));
                            }
                            if (EditorUtils.IsInvisibleComponent(components[currentIndex], ActiveTabInDebugMode()))
                            {
                                componentBox.style.display = DisplayStyle.None;
                            }

                            gameObjectBox.Add(componentBox);
                            componentBox.TrackSerializedObjectValue(editor.serializedObject, KeepTrackOfComponent);
                            AddIfNecessary(runtimeEditors[currentIndex]);
                        }
                    }
                    currentIndex++;
                }
                if (!AreWeFilteringComponents() && removedComponents != null && removedComponents.Count > 0)
                {
                    Component[] prefabComponents = removedComponents[0].assetComponent.gameObject.GetComponents<Component>();
                    int inserted = 0;
                    foreach (var kvp in removedComponents)
                    {
                        if (kvp == null)
                        {
                            continue;
                        }
                        Component removedComponent = kvp.assetComponent;
                        IMGUIContainer removedPrefabComponent = null;
                        removedPrefabComponent = new IMGUIContainer(() =>
                        {
                            Reflected.RemovedComponentTitlebar(GUILayoutUtility.GetRect(position.width, 21), GetActiveTab().target, removedComponent);
                            if (!removedComponent)
                            {
                                removedPrefabComponent.RemoveFromHierarchy();
                            }
                        });
                        int originalPosition = kvp.GetRemovedComponentOriginalPosition(prefabComponents);
                        removedPrefabComponent.name = originalPosition == -1 ? "RemovedComponent" : $"RemovedComponent{originalPosition}";
                        gameObjectBox.Add(removedPrefabComponent);
                        if (originalPosition != -1)
                        {
                            removedPrefabComponent.PlaceBefore(gameObjectBox, $"ComponentElement{originalPosition - inserted}");
                            inserted += 1;
                        }
                    }
                    removedComponents.Clear();
                }
                //  if (EditorUtils.IsAnUIObject(GetActiveTab().target))


                {
                    List<Editor> allPreviewEditors = new List<Editor>();
                    foreach (var editor in runtimeEditors)
                    {
                        if (editor && editor.HasPreviewGUI())
                        {

                            allPreviewEditors.Insert(0, editor);
                        }

                    }
                    if (allPreviewEditors.Count == 0)
                    {
                        if (gameObjectEditor && gameObjectEditor.HasPreviewGUI())
                        {
                            allPreviewEditors.Insert(0, gameObjectEditor);
                        }

                    }

                    if (allPreviewEditors.Count > 0)
                    {
                        allPreviewEditors.Add(runtimeEditors[0]);
                        VisualElement preview = EditorUtils.CreatePreviewToolbar(allPreviewEditors.ToArray(), GetActiveTab());
                        if (preview != null)
                        {
                            GetActiveTab().hasPreview = true;
                            preview.name = "Preview";
                            if (addComponentBox != null)
                            {
                                addComponentBox.ReplaceAndInsertFirst(preview);
                            }
                        }
                    }
                }

                GetActiveTab().OrderMapsByIndex();
            }
            else
            {
                if (componentEditors != null)
                {
                    GameObject lastGameObject = GetActiveTab().MultiTrackingTarget();
                    int indexOfLast = Array.IndexOf(GetActiveTab().targets, lastGameObject);
                    Component[] components = componentEditors.Select(editor => editor.targets[indexOfLast] as Component).ToArray();
                    bool hasNullComponent = lastGameObject.GetComponents<Component>().Any(c => c == null);
                    GetActiveTab().CleanMap(components);
                    int componentCount = componentEditors.Length;
                    GetActiveTab().runtimeMultiComponents = new Component[componentCount];
                    Array.Copy(components, GetActiveTab().runtimeMultiComponents, componentCount);

                    for (int i = 0; i < componentEditors.Length; i++)
                    {
                        Editor editor = componentEditors[i];
                        if (editor == null)
                        {
                            continue;
                        }
                        if (hasNullComponent && i == 0)
                        {
                            VisualElement missingComponent = new IMGUIContainer(() =>
                            {
                                ShowMissingComponent(true);
                            });
                            missingComponent.style.flexGrow = 0;
                            missingComponent.style.flexShrink = 0;
                            missingComponent.name = "ComponentElement" + -1;
                            gameObjectBox.Add(missingComponent);
                        }
                        VisualElement componentBox = DrawMultiComponent(editor, i, componentCount, lastGameObject);
                        if (componentBox != null)
                        {
                            EditorUtils.SetStaticElement(componentBox);
                            if (!oneVisible && AreWeFilteringComponents())
                            {
                                oneVisible = IsComponentFilteredInTab(GetActiveTab().GetFoldoutMapForComponent(editor.target as Component, editor));
                            }
                            if (EditorUtils.IsInvisibleComponent(editor.target as Component, ActiveTabInDebugMode()))
                            {
                                componentBox.style.display = DisplayStyle.None;
                            }
                            gameObjectBox.Add(componentBox);

                            componentBox.TrackSerializedObjectValue(editor.serializedObject, KeepTrackOfComponent);
                            AddIfNecessary(editor);
                        }
                    }

                    multiComponentFooter = new IMGUIContainer(() =>
                    {
                        EditorUtils.MultiEditFooter(differentComponents, rootVisualElement);
                    });
                    multiComponentFooter.name = "MultiComponentFooter";
                    gameObjectBox.Add(multiComponentFooter);
                    List<Editor> allPreviewEditors = new List<Editor>();
                    foreach (var editor in componentEditors)
                    {
                        if (editor && editor.HasPreviewGUI())
                        {
                            allPreviewEditors.Insert(0, editor);
                        }
                    }
                    if (allPreviewEditors.Count == 0)
                    {
                        if (gameObjectEditor && gameObjectEditor.HasPreviewGUI())
                        {
                            allPreviewEditors.Insert(0, gameObjectEditor);
                        }

                    }
                    if (allPreviewEditors.Count > 0)
                    {
                        allPreviewEditors.Add(componentEditors[0]);
                        VisualElement preview = EditorUtils.CreatePreviewToolbar(allPreviewEditors.ToArray(), GetActiveTab());
                        if (preview != null)
                        {
                            GetActiveTab().hasPreview = true;
                            preview.name = "Preview";
                            if (addComponentBox != null)
                            {
                                addComponentBox.ReplaceAndInsertFirst(preview);
                            }
                        }
                    }
                }
            }
            if (AreWeFilteringComponents() && !oneVisible)
            {
                IMGUIContainer noResultsLabel = new IMGUIContainer(() =>
                   {
                       EditorUtils.NoResultsLabel(rootVisualElement);
                   });
                noResultsLabel.name = "NoResultsLabel";
                gameObjectBox.Add(noResultsLabel);
                if (multiComponentFooter != null)
                {
                    noResultsLabel.PlaceBehind(multiComponentFooter);
                }
            }
            IMGUIContainer scrollerController = ControlScrollerBarVisibility(componentScrollView);
            if (scrollerController != null)
            {
                EditorUtils.SetStaticElement(scrollerController);
                gameObjectBox.Add(scrollerController);
            }
            gameObjectBox.style.flexGrow = 0;
            gameObjectBox.style.flexShrink = 0;
            gameObjectBox.style.top = 0;
            gameObjectBox.style.paddingTop = 0;
            rootVisualElement.MarkDirtyRepaint();
            //CullNow(true);
            CullNow(true);
            //GetActiveTab().MarkMaterialsForRebuild();
            RefreshAllTabNames();

            if (!IsActiveTabNew())
            {
                //componentScrollView.scrollOffset = new Vector2(0, GetActiveTab().scrollPosition);
                //    SetScrollPosition(GetActiveTab().scrollPosition);
            }
        }

        void FixComponentScrollAfterDrag(ComponentDragOperation componentDrag)
        {
            if (componentDrag == null)
            {
                return;
            }
            if (componentDrag.draggedComponent == null)
            {
                return;
            }
            int totalComponents = GetActiveTab().target.GetComponents<Component>().Length;
            if (componentDrag.targetIndex < 0 || componentDrag.targetIndex >= totalComponents - 1)
            {
                return;
            }
            if (componentDrag.targetIndex > componentDrag.sourceIndex)
            {
                ComponentMap componentMap = GetActiveTab().GetFoldoutMapForComponent(componentDrag.draggedComponent);
                if (componentMap != null)
                {
                    float height = componentMap.height;
                    float currentScroll = componentScrollView.scrollOffset.y;
                    float newScroll = currentScroll - height;
                    float minScroll = 0f;
                    float maxScroll = componentScrollView.contentContainer.resolvedStyle.height
                                      - componentScrollView.resolvedStyle.height;
                    newScroll = Mathf.Clamp(newScroll, minScroll, maxScroll);
                    SetScrollPosition(newScroll);
                }
            }
        }


        void DrawPrefabEditors()
        {
            bool isModel = IsAModelTarget();
            if (assetScrollView == null || (!IsAPrefabTarget() && !isModel) || assetEditor == null)
            {
                return;
            }
            bool isMulti = AssetTargetMode() == 2;
            if (!isModel)
            {
                assetScrollView.Clear();
            }
            GameObject prefab = null;
            Component[] components = null;
            Editor[] runtimeEditors = prefabEditors;
            if (prefabEditors == null)
            {
                return;
            }
            int componentCount = 0;
            if (!isMulti)
            {
                prefab = (GameObject)targetObject;
                components = prefab.GetComponents<Component>();
                runtimeEditors = prefabEditors;
                componentCount = components.Length;
            }
            else
            {
                prefab = (GameObject)targetObjects[0];
                components = prefab.GetComponents<Component>();
                runtimeEditors = prefabEditors;
                componentCount = prefabEditors.Length;
            }
            IMGUIContainer assetHeader = new IMGUIContainer(() =>
            {
                DrawHeader(assetEditor);
            });
            assetHeader.name = "AssetHeader";
            assetHeader.TrackSerializedObjectValue(assetEditor.serializedObject, KeepTrackOfPrefab);
            assetScrollView.Replace(assetHeader);
            if (components == null || components.Any(c => c == null))
            {
                IMGUIContainer missingComponent = new IMGUIContainer(() =>
                {
                    ShowPrefabMissingComponent(isMulti);
                });
                assetScrollView.Add(missingComponent);
                assetScrollView.style.top = 0;
                assetScrollView.LimitScrollbarVisibilityTo(1);
                return;
            }
            if (runtimeEditors == null || runtimeEditors.Length == 0)
            {
                return;
            }
            PrefabComponentMapManager.CleanMap(components);
            for (int i = 0; i < componentCount; i++)
            {
                Editor editor = runtimeEditors[i];
                if (editor == null || editor.serializedObject == null)
                {
                    continue;
                }
                VisualElement componentBox = DrawPrefabComponent(editor, i, componentCount, components);
                if (componentBox != null)
                {
                    assetScrollView.Add(componentBox);
                    componentBox.style.display = DisplayStyle.Flex;
                    componentBox.style.top = -1;
                    if (isModel)
                    {
                        componentBox.SetEnabled(false);
                    }
                    assetBox.MarkDirtyRepaint();
                }
            }
            if (isMulti)
            {
                IMGUIContainer multiComponentFooter = new IMGUIContainer(() =>
                    {
                        EditorUtils.MultiEditFooter(differentPrefabComponents, rootVisualElement, true);
                    });
                multiComponentFooter.name = "MultiComponentPrefabFooter";
                assetScrollView.Add(multiComponentFooter);
            }
            IMGUIContainer LastComponent = new IMGUIContainer(() =>
            {
                DrawPrefabLastComponentLine();
            });
            EditorUtils.SetStaticElement(LastComponent);
            assetScrollView.Add(LastComponent);
            assetScrollView.style.top = -1;
            assetScrollView.style.flexGrow = 0;
        }
        void ReinitializeInspectors()
        {
            ReinitializeComponentEditors();
            ReinitializePrefabComponentEditors();
        }

        internal void OnProjectChanged()
        {
            rootVisualElement.schedule
        .Execute(() => ReinitializeAssetView())
        .StartingIn(0);
        }

        void ReinitializeAssetView()
        {
            if (!IsValidAssetTarget())
            {
                CloseAssetView();
                return;
            }
            if (AssetTargetMode() == 2 && targetObjects.Length > 1)
            {
                if (assetEditor == null || SortedAssetSelection == null || SortedAssetSelection.Count == 0)
                {
                    if (assetEditor == null)
                    {
                        Debug.LogWarning("Asset editor is null!");
                    }
                    ResetMultiAssetEditors();
                    targetObject = null;
                    targetObjects = null;
                }
                if (!HandleMultiAssetNulls())
                {
                    rootVisualElement.MarkDirtyRepaint();
                    return;
                }
                return;
            }
            if (assetEditor == null || assetEditor.target != targetObject)
            {
                AssetInfo assetInfo = PoolCache.GetAssetInfo(targetObject);
                bool prefabMode = false;
                if (assetInfo != null)
                {
                    prefabMode = assetInfo.isPrefab || assetInfo.isImportedObject;
                }
                ResetAssetInspector(prefabMode);
                return;
            }
            DrawCurrentComponentsContainer();
        }

        void ReinitializeComponentEditors()
        {
            DoReinitializeComponentEditors();
        }

        /*    void CheckBatchCalls()
            {        
                return;    
                if (cullNow)
                {
                    cullNow = false;
                    if (EditorWindow.focusedWindow != this)
                    {
                        return;
                    }                
                    ResetComponentCulling();               
                    rootVisualElement.schedule.Execute(() =>
                    {
                        CullComponents();
                    });
                }
                if (updateLastMostContents)
                {
                    updateLastMostContents = false;
                    DoUpdateLastMostContents();
                }
                if (rebuildComponents)
                {
                    rebuildComponents = false;
                    ReinitializeComponentEditors();
                }      
            }*/
        internal void RequestAction(Action action, int delay = 0)
        {
            if (action == null)
            {
                isScheduled = false;
                return;
            }
            pendingActions.Remove(action);
            pendingActions.Add(action);
            DelayedAction delayedAction = new DelayedAction(action, delay);

            if (!isScheduled)
            {
                isScheduled = true;

                if (rootVisualElement != null)
                {
                    rootVisualElement.schedule.Execute(ExecutePendingActions).StartingIn(0);
                }
                else
                {
                    Debug.LogWarning("ExecutePendingActions scheduled but rootVisualElement is null.");
                }
            }
        }

        private void ExecutePendingActions()
        {
            while (pendingActions.Count > 0)
            {
                List<Action> actionsToExecute = new List<Action>(pendingActions);
                foreach (var action in actionsToExecute)
                {
                    try
                    {
                        string methodName = action.Method.Name;
                        /* if (!methodName.Contains("DoCullComponents"))
                         {
                             //Debug.Log($"ActionScheduler: Executing action {action.Method.Name}");
                         }*/
                        action.Invoke();
                    }
                    catch (Exception ex)
                    {
                        Debug.LogError($"ActionScheduler: Exception during action execution: {ex} ");
                    }
                    pendingActions.Remove(action);
                }
            }
            isScheduled = false;
            Repaint();
        }


        VisualElement DrawSingleComponent(Editor editor, int i, int componentCount, Component[] components)
        {
            bool isLastComponent = i == componentCount - 1;
            VisualElement componentBox = new VisualElement();
            VisualElement editorTool = null;
            Component component = editor.target as Component;
            ComponentMap map = GetActiveTab().GetFoldoutMapForComponent(component, editor);

            VisualElement componentInspector = new InspectorElement(editor);

            if (editor.serializedObject == null || !editor.serializedObject.targetObject)
            {
                Debug.LogError("SerializedObject is null or invalid");
            }
            IMGUIContainer componentBar = new IMGUIContainer(() =>
            {
                if (editor.target == null || editor.serializedObject == null)
                {
                    Repaint();
                    return;
                }
                Color color = ComponentHighlighter.GetHighlight("Component" + i, this);
                if (color != Color.black)
                {
                    GUI.color += color;
                    componentBox.MarkDirtyRepaint();
                }
                DrawComponentBar(editor, i, componentCount, map);
                bool lastVisibility = map.isFilteredOut;
                componentInspector.style.display = (!map.foldout || (map.hidden && !ActiveTabInDebugMode())) ? DisplayStyle.None : DisplayStyle.Flex;
                componentBox.style.display = IsComponentFilteredInTab(map) && !EditorUtils.IsInvisibleComponent(component, ActiveTabInDebugMode()) ? DisplayStyle.Flex : DisplayStyle.None;
                if (editorTool != null)
                {
                    editorTool.style.display = componentInspector.style.display;
                }
                if (i != 0 && EditorUtils.AreWeDraggingThis(component))
                {
                    if (componentBox.enabledInHierarchy)
                    {
                        componentBox.SetEnabled(false);
                    }
                }
                else
                {
                    if (!componentBox.enabledInHierarchy)
                    {
                        componentBox.SetEnabled(true);
                    }

                }
                HandleDragOperations(i, editor.target as Component, components, editor, componentBox.worldBound.height);
            });


            componentBar.name = "Component" + i;
            componentBar.style.flexGrow = 0;
            componentBar.style.flexShrink = 0;
            componentBar.style.top = 0;
            componentBox.Add(componentBar);
            EditorUtils.OverrideComponentName(component, componentBar, componentBox, componentInspector);
            componentInspector.style.top = 0;
            componentInspector.style.flexGrow = 0;
            componentInspector.style.flexShrink = 0;
            componentInspector.style.display = (!map.foldout || (map.hidden && !ActiveTabInDebugMode())) ? DisplayStyle.None : DisplayStyle.Flex;
            if (ActiveTabInDebugMode())
            {
                componentInspector.Clear();
                var _componentInspector = new IMGUIContainer(() =>
                 {
                     if (editor.target == null)
                     {
                         Repaint();
                         return;
                     }
                     EditorGUIUtility.wideMode = true;
                     EditorGUIUtility.labelWidth = MainCoInspector.position.width / 2.4f;
                     EditorGUI.indentLevel = 1;
                     editor.DrawDefaultInspector();
                     EditorGUI.indentLevel = 0;
                 });
                _componentInspector.cullingEnabled = true;
                componentInspector = _componentInspector;
            }
            else if (Reflected.ComponentHasEditorTool(component))
            {
                bool validType = EditorUtils.IsValidEditorToolType(component);
                editorTool = new IMGUIContainer(() =>
                {
                    if (editor.target == null)
                    {
                        Repaint();
                        return;
                    }
                    if (map.foldout)
                    {
                        GUILayout.Space(5);
                        DrawComponentEditorTools(map);
                    }
                });
                editorTool.name = "EditorTool" + i;
                editorTool.style.flexGrow = 0;
                editorTool.style.flexShrink = 0;
                componentBox.Add(editorTool);
            }
            componentBox.Add(componentInspector);
            if (editorTool != null)
            {
                editorTool.PlaceBehind(componentInspector);
            }
            if (EditorUtils.IsMaterialComponent(editor.target as Component))
            {
                bool visible = componentInspector.style.display == DisplayStyle.Flex;
                //componentInspector.ReplaceCallback<GeometryChangedEvent>(CheckMaterialChanges);
                IMGUIContainer materialEditor = new IMGUIContainer(() =>
                {
                    if (editor.target == null)
                    {
                        Repaint();
                        return;
                    }
                    EditorGUIUtility.wideMode = true;
                    EditorGUI.indentLevel = 0;
                    if (map.foldout)
                    {
                        DrawComponentMaterials(editor.target as Component);
                    }
                });
                materialEditor.style.flexGrow = 0;
                materialEditor.style.flexShrink = 0;
                materialEditor.style.paddingTop = -3;
                if (map.foldout)
                {
                    materialEditor.style.paddingBottom = 5;
                }
                else
                {
                    materialEditor.style.paddingBottom = 0;
                }
                materialEditor.ReplaceCallback<GeometryChangedEvent>(evt =>
                {
                    if (map.foldout)
                    {
                        materialEditor.style.paddingBottom = 5;
                    }
                    else
                    {
                        materialEditor.style.paddingBottom = 0;
                    }
                });
                componentBox.Add(materialEditor);
            }
            componentBox.style.flexGrow = 0;
            componentBox.style.flexShrink = 0;
            componentBox.style.top = 0;
            componentBox.style.paddingTop = 0;
            componentBox.style.marginTop = 0;
            componentBox.style.marginBottom = 0;
            componentBox.style.paddingBottom = 0;
            IMGUIContainer dragSpace = new IMGUIContainer(() =>
            {
                if (editor.target == null)
                {
                    Repaint();
                    return;
                }
                if (componentInspector != null && componentInspector.worldBound.height != 4)
                {
                    map.height = componentBox.worldBound.height;
                }

                Color color = ComponentHighlighter.GetHighlight("Component" + i, this);
                if (color != Color.black)
                {
                    Color currentColor = componentBox.style.color.value;
                    Color newColor = currentColor + color / 2;
                    newColor.r = Mathf.Clamp01(newColor.r);
                    newColor.g = Mathf.Clamp01(newColor.g);
                    newColor.b = Mathf.Clamp01(newColor.b);
                    newColor.a = Mathf.Clamp01(newColor.a);
                    componentBox.style.backgroundColor = newColor;
                    componentBox.MarkDirtyRepaint();
                }
                else
                    if (componentBox.style.backgroundColor != componentBox.style.color.value)
                    {
                        componentBox.style.backgroundColor = componentBox.style.color.value;
                    }

                if (pendingOperation != null && pendingOperation.targetIndex == i)
                {
                    componentScrollView.MarkDirtyRepaint();
                }
            });

            dragSpace.style.flexGrow = 0;
            dragSpace.style.flexShrink = 0;
            componentBox.Add(dragSpace);
            componentBox.focusable = pendingOperation == null;
            componentBox.name = "ComponentElement" + i;
            return componentBox;
        }

        VisualElement DrawMultiComponent(Editor editor, int i, int componentCount, GameObject lastGameObject)
        {
            bool isLastComponent = i == componentCount - 1;
            bool noMultiEdit = !Reflected.CanBeMultiEdited(editor);
            bool anyNull = editor.targets.Any(t => t == null);
            VisualElement componentBox = new VisualElement();
            IMGUIContainer componentBar = null;
            int indexOfLast = Array.IndexOf(GetActiveTab().targets, lastGameObject);
            ComponentMap componentMap = GetActiveTab().GetFoldoutMapForComponent(editor.targets[indexOfLast] as Component, editor);
            VisualElement componentInspector = new InspectorElement(editor);
            if (editor.serializedObject == null || !editor.serializedObject.targetObject)
            {
                Debug.LogError("SerializedObject is null or invalid");
            }
            componentBar = new IMGUIContainer(() =>
            {
                anyNull = editor.targets.Any(t => t == null);
                if (anyNull)
                {
                    gameObjectBox.schedule.Execute(() =>
                    Repaint()
                    );
                    Repaint();
                    return;
                }
                if (componentBar != null && componentInspector != null && componentInspector.worldBound.height != 4)
                {
                    componentMap.height = componentBox.worldBound.height;

                }
                DrawMultiComponentBar(editor, i, componentCount, componentMap, indexOfLast);
                bool lastVisibility = componentMap.isFilteredOut;
                componentInspector.style.display = (!componentMap.foldout || (componentMap.hidden && !ActiveTabInDebugMode())) ? DisplayStyle.None : DisplayStyle.Flex;
                componentBox.style.display = IsComponentFilteredInTab(componentMap) && !EditorUtils.IsInvisibleComponent(componentMap.component, ActiveTabInDebugMode()) ? DisplayStyle.Flex : DisplayStyle.None;
            });
            EditorUtils.SetStaticElement(componentBar);
            componentBar.style.top = 0;
            componentBar.style.paddingTop = 0;
            componentBox.Add(componentBar);
            EditorUtils.OverrideComponentName(editor.targets[0] as Component, componentBar, componentBox, componentInspector);
            componentInspector.style.top = 0;
            componentInspector.style.display = (!componentMap.foldout || (componentMap.hidden && !ActiveTabInDebugMode())) ? DisplayStyle.None : DisplayStyle.Flex;
            if (ActiveTabInDebugMode())
            {
                componentInspector.Clear();
                componentInspector = new IMGUIContainer(() =>
                 {
                     anyNull = editor.targets.Any(t => t == null);
                     if (anyNull)
                     {
                         KeepTrackOfComponentStructure();
                         Repaint();
                         return;
                     }
                     EditorGUIUtility.wideMode = true;
                     EditorGUIUtility.labelWidth = MainCoInspector.position.width / 2.4f;
                     EditorGUI.indentLevel = 1;
                     editor.DrawDefaultInspector();
                     EditorGUI.indentLevel = 0;
                 });
            }
            else if (noMultiEdit)
            {
                componentInspector.Clear();
                componentInspector = new IMGUIContainer(() =>
                    {
                        anyNull = editor.targets.Any(t => t == null);
                        if (anyNull)
                        {
                            KeepTrackOfComponentStructure();
                            Repaint();
                            return;
                        }
                        ShowNotMultiComponentGUI();
                    });
                componentInspector.style.marginBottom = 5;
                componentInspector.style.marginTop = 5;
                componentInspector.style.marginLeft = 5;
                componentInspector.style.marginRight = 5;
            }
            EditorUtils.SetStaticElement(componentInspector);
            componentBox.Add(componentInspector);
            if (EditorUtils.IsMaterialComponent(editor.targets[indexOfLast] as Component))
            {
                //componentInspector.TrackSerializedObjectValue(editor.serializedObject, CheckMaterialChanges);
                IMGUIContainer materialEditor = new IMGUIContainer(() =>
                {
                    anyNull = editor.targets.Any(t => t == null);
                    if (anyNull)
                    {
                        KeepTrackOfComponentStructure();
                        Repaint();
                        return;
                    }
                    EditorGUIUtility.wideMode = true;
                    EditorGUI.indentLevel = 0;
                    if (componentMap.foldout)
                    {
                        DrawComponentMaterials(editor.targets[indexOfLast] as Component, editor);
                    }
                });
                materialEditor.style.flexGrow = 0;
                materialEditor.style.flexShrink = 0;
                materialEditor.style.paddingTop = -3;
                if (componentMap.foldout)
                {
                    materialEditor.style.paddingBottom = 5;
                }
                else
                {
                    materialEditor.style.paddingBottom = 0;
                }
                materialEditor.ReplaceCallback<GeometryChangedEvent>(evt =>
                {
                    if (componentMap.foldout)
                    {
                        materialEditor.style.paddingBottom = 5;
                    }
                    else
                    {
                        materialEditor.style.paddingBottom = 0;
                    }
                });
                componentBox.Add(materialEditor);
            }
            componentBox.focusable = pendingOperation == null;
            componentBox.name = "ComponentElement" + i;
            return componentBox;

        }
        VisualElement DrawPrefabComponent(Editor editor, int i, int componentCount, Component[] components)
        {
            bool isLastComponent = i == componentCount - 1;
            bool isMulti = AssetTargetMode() == 2;
            VisualElement componentBox = new VisualElement();
            ComponentMap map = PrefabComponentMapManager.GetFoldoutMapForComponent(editor.target as Component, editor, debugAsset);
            VisualElement componentInspector = new InspectorElement(editor);
            if (editor.serializedObject == null || !editor.serializedObject.targetObject)
            {
                Debug.LogError("SerializedObject is null or invalid");
            }

            IMGUIContainer componentBar = new IMGUIContainer(() =>
            {
                if (editor.target == null || editor.serializedObject == null)
                {
                    Repaint();
                    return;
                }

                DrawComponentBar(editor, i, componentCount, map, true);
                bool lastVisibility = map.isFilteredOut;
                componentInspector.style.display = (!map.foldout || (map.hidden && !debugAsset)) ? DisplayStyle.None : DisplayStyle.Flex;
                componentBox.style.display = DisplayStyle.Flex;
            });
            componentBar.name = "PrefabComponent" + i;
            componentBar.style.flexGrow = 0;
            componentBar.style.flexShrink = 0;
            componentBar.style.top = 0;
            componentBox.Add(componentBar);
            EditorUtils.OverrideComponentName(editor.target as Component, componentBar, componentBox, componentInspector);
            componentInspector.style.top = 0;
            componentInspector.style.flexGrow = 0;
            componentInspector.style.flexShrink = 0;
            componentInspector.style.display = (!map.foldout || (map.hidden && !debugAsset)) ? DisplayStyle.None : DisplayStyle.Flex;
            if (debugAsset)
            {
                componentInspector.Clear();
                componentInspector = new IMGUIContainer(() =>
                 {
                     EditorGUIUtility.wideMode = true;
                     EditorGUIUtility.labelWidth = MainCoInspector.position.width / 2.4f;
                     EditorGUI.indentLevel = 1;
                     editor.DrawDefaultInspector();
                     EditorGUI.indentLevel = 0;
                 });
            }
            componentBox.Add(componentInspector);
            componentInspector.TrackSerializedObjectValue(editor.serializedObject, CheckPrefabMaterialChanges);
            if (EditorUtils.IsMaterialComponent(editor.target as Component))
            {
                bool visible = componentInspector.style.display == DisplayStyle.Flex;
                //componentInspector.ReplaceCallback<GeometryChangedEvent>(CheckPrefabMaterialChanges);
                IMGUIContainer materialEditor = new IMGUIContainer(() =>
                {
                    EditorGUIUtility.wideMode = true;
                    EditorGUI.indentLevel = 0;
                    if (map.foldout)
                    {
                        DrawComponentMaterials(editor.target as Component, null, true);
                    }
                });

                materialEditor.style.flexGrow = 0;
                materialEditor.style.flexShrink = 0;
                materialEditor.style.paddingTop = -3;
                materialEditor.style.paddingBottom = 3;
                if (map.foldout)
                {
                    materialEditor.style.paddingBottom = 5;
                }
                else
                {
                    materialEditor.style.paddingBottom = 0;
                }
                materialEditor.ReplaceCallback<GeometryChangedEvent>(evt =>
                {
                    if (map.foldout)
                    {
                        materialEditor.style.paddingBottom = 5;
                    }
                    else
                    {
                        materialEditor.style.paddingBottom = 0;
                    }
                });
                componentBox.Add(materialEditor);
            }
            componentBox.style.flexGrow = 0;
            componentBox.style.flexShrink = 0;
            componentBox.style.top = 0;
            componentBox.name = "PrefabComponentElement" + i;


            return componentBox;
        }

        void DrawMultiComponentBar(Editor editor, int i, int componentCount, ComponentMap componentMap, int indexOfLast)
        {
            componentMap.index = i;
            bool componentHidden = !EditorUtils.HasVisibleFields(componentEditors[i]) && !ActiveTabInDebugMode();
            /*
            Behaviour behaviour = componentEditors[i].targets[0] as Behaviour;
            bool isBehaviour = behaviour != null;
            bool wasEnabled = isBehaviour && behaviour.enabled;
            if (isBehaviour)
            {
                EditorGUI.BeginChangeCheck();
            }*/
            EditorGUILayout.BeginVertical();
            EditorGUILayout.BeginHorizontal(CustomGUIStyles.InspectorButtonStyle);

            if (i == 0)
            {
                DrawMultiTabTransformLogic(componentMap);
            }
            bool flag = EditorGUILayout.InspectorTitlebar(componentMap.foldout,
            componentEditors[i]);
            /*
            if (isBehaviour)
            {
               if (DrawMultiComponentEnabledToggle(componentEditors[i], wasEnabled))
                {
                    flag = componentMap.foldout;
                }
            }*/

            EditorGUILayout.EndHorizontal();
            if (componentHidden)
            {
                componentMap.hidden = true;
                flag = false;
            }
            else
            {
                componentMap.hidden = false;
            }
            bool changed = flag != componentFoldouts_[i];
            if (changed)
            {
                CullNow(true);
                componentFoldouts_[i] = flag;
                GetActiveTab().SaveFoldoutToMap(componentEditors[i].targets[indexOfLast] as Component, flag, componentEditors[i]);
            }
            EditorGUILayout.EndVertical();
            Rect rect1 = GUILayoutUtility.GetLastRect();
            rect1.x = 0;
            EditorUtils.DrawLineOverRect(rect1, -1);
            if (!flag || componentHidden)
            {
                EditorUtils.DrawLineUnderRect(rect1, CustomColors.SoftShadow, -1, 2);
            }
            else
            {
                EditorUtils.DrawLineUnderRect(rect1, CustomColors.MediumShadow);
                Rect underRect = new Rect(rect1);
                underRect.y += 22;
                underRect.height = 4;
                EditorUtils.DrawFadeToBottom(underRect, CustomColors.SoftShadow);
            }

        }

        void DrawComponentMaterials(Component component, Editor editor = null, bool isPrefab = false)
        {
            MaterialMap materialMap;
            if (isPrefab)
            {
                materialMap = PrefabMaterialMapManager.GetMaterialMapForComponent(component, editor);
            }
            else
            {
                materialMap = GetActiveTab().GetMaterialMapForComponent(component, editor);
            }
            if (materialMap == null)
            {
                return;
            }
            List<Material> materialsList = materialMap.materials;
            if (materialsList.Count > 0)
            {
                EditorUtils.DrawMaterials(materialMap, component, isPrefab);
            }
        }
        void TriggerPrefabMaterialRebuild()
        {
            PrefabMaterialMapManager._DestroyAllMaterialMaps();
        }
        public bool AreAssetsInSelection()
        {
            int mode = AssetTargetMode();
            if (mode == 0)
            {
                return false;
            }
            if (mode == 1)
            {
                return Selection.objects.Contains(targetObject);
            }
            if (mode == 2)
            {
                return EditorUtils.AreObjectsInSelection(targetObjects);
            }
            return false;
        }

        void CheckPrefabMaterialChanges(SerializedObject obj)
        {

            if (obj == null || assetsCollapsed)
            {
                return;
            }
            if (!AreAssetsInSelection())
            {
                SaveCurrentTargetOrTargets();
            }
            if (obj == null || obj.targetObject == null) return;
            Component component = obj.targetObject as Component;
            if (component == null) return;
            if (EditorUtils.IsMaterialComponent(component))
            {
                bool materialsChanged = PrefabMaterialMapManager.HaveMaterialsChanged(obj);
                if (materialsChanged)
                {
                    TriggerPrefabMaterialRebuild();
                }
            }
            rootVisualElement.MarkDirtyRepaint();
        }

        void SetMaterialChanged()
        {
            if (GetActiveTab() != null)
            {
                GetActiveTab().MarkMaterialsForRebuild();
            }
            rootVisualElement.MarkDirtyRepaint();
        }

        internal void ResetGameObjectEditor()
        {

            if (GetActiveTab()?.target)
            {
                DestroyIfNotNull(gameObjectEditor);
                gameObjectEditor = null;
                Editor.CreateCachedEditor(GetActiveTab().target, null, ref gameObjectEditor);
            }
            else if (IsActiveTabValidMulti())
            {
                DestroyIfNotNull(gameObjectEditor);
                gameObjectEditor = null;
                Editor.CreateCachedEditor(GetActiveTab().targets, null, ref gameObjectEditor);


            }
            RebuildHeaderContainer();
        }

        void KeepTrackOfGameObject(SerializedObject obj)
        {
            if (obj == null)
            {
                return;
            }
            if (GetActiveTab().HaveLinkedPrefabsChanged())
            {
                FocusTab();
                return;
            }
            KeepTrackOfComponentStructure();

        }


        void KeepTrackOfComponentStructure()
        {

            if (GetActiveTab() == null || IsActiveTabNew())
            {
                return;
            }

            if (!IsActiveTabValidMulti() && GetActiveTab() != null && GetActiveTab().target)
            {
                Component[] components = GetActiveTab().target.GetComponents<Component>();
                TrackComponentMaps(components, GetActiveTab().componentMaps);
                // ReinitializeComponentEditors();
                DoCullNow();
                Repaint();
            }
            else if (IsActiveTabValidMulti())
            {
                Component[] components = null;
                GameObject[] gameObjects = GetActiveTab().targets;
                if (gameObjects == null || gameObjects.Any(go => go == null))
                {
                    ReinitializeComponentEditors();
                    return;
                }
                GameObject lastGameObject = gameObjects[0];
                List<KeyValuePair<Type, List<List<Component>>>> map = EditorUtils.OrderedComponentMap(GetActiveTab().targets, this);
                List<Component> orderedComponentArrays = new List<Component>();
                foreach (Component comp in lastGameObject.GetComponents<Component>())
                {
                    if (comp == null) continue;

                    Type compType = comp.GetType();
                    var typeEntry = map.FirstOrDefault(e => e.Key == compType);
                    if (typeEntry.Key != null)
                    {
                        int compIndex = typeEntry.Value[0].IndexOf(comp);
                        if (compIndex != -1)
                        {
                            Component targetComponent = typeEntry.Value.Select(list => list[compIndex]).First();
                            orderedComponentArrays.Add(targetComponent);
                        }
                    }
                }
                components = orderedComponentArrays.ToArray();
                TrackComponentMaps(components, GetActiveTab().componentMaps);
                // ReinitializeComponentEditors();
                DoCullNow();
            }
            else
            {
                ReinitializeComponentEditors();
            }
            RefreshAllIcons();
        }
        internal void RefreshAllPrefabModes()
        {
            if (tabs != null)
            {
                foreach (var tab in tabs)
                {
                    tab.SetPrefabMode();
                }
            }
        }
        internal static void RefreshAllCoInspectors()
        {

            if (CoInspectorWindow.instances != null)
            {
                foreach (CoInspectorWindow instance in CoInspectorWindow.instances)
                {
                    instance.rootVisualElement.MarkDirtyRepaint();
                }
            }
        }
        void KeepTrackOfComponent(SerializedObject obj)
        {

            if (obj == null)
            {
                KeepTrackOfComponentStructure();
                return;
            }
            if (obj == null || obj.targetObject == null)
            {
                UpdateSceneToolsIfNecessary(obj);
                rootVisualElement.MarkDirtyRepaint();
            }
            Component component = obj.targetObject as Component;
            if (component != null && EditorUtils.IsMaterialComponent(component))
            {
                bool materialsChanged = GetActiveTab().HaveMaterialsChanged(obj);
                if (materialsChanged)
                {
                    //SetMaterialChanged();
                    GetActiveTab().TriggerMaterialRebuild();
                }
            }
            UpdateSceneToolsIfNecessary(obj);
            rootVisualElement.MarkDirtyRepaint();
        }


        static void UpdateSceneToolsIfNecessary(SerializedObject obj)
        {
            if (obj == null)
            {
                return;
            }
            if (obj.targetObject is Transform || obj.targetObject is GameObject || obj.targetObject is RectTransform)
            {
                EditorToolManager.RefreshHandlePosition();
            }
        }
        void KeepTrackOfPrefab(SerializedObject obj)
        {

            if (obj == null)
            {
                return;
            }
            KeepTrackOfPrefabStructure();
        }
        void KeepTrackOfPrefabStructure()
        {
            if (IsValidAssetTarget())
            {
                AssetInfo assetInfo;
                if (AssetTargetMode() == 1 && !PoolCache.IsAnImportedObject(targetObject))
                {
                    assetInfo = PoolCache.GetAssetInfo(targetObject);
                    if (assetInfo.isPrefab || assetInfo.isImportedObject)
                    {
                        GameObject prefab = (GameObject)targetObject;
                        Component[] components = prefab.GetComponents<Component>();
                        TrackComponentMaps(components, PrefabComponentMapManager.componentMaps, true);
                    }
                }
                else if (AssetTargetMode() == 2)
                {
                    assetInfo = PoolCache.GetAssetInfo(targetObjects[0]);
                    if (assetInfo.isPrefab || assetInfo.isImportedObject)
                    {
                        Component[] components = null;
                        GameObject[] prefabs = new GameObject[targetObjects.Length];
                        for (int i = 0; i < targetObjects.Length; i++)
                        {
                            prefabs[i] = targetObjects[i] as GameObject;
                        }
                        GameObject lastGameObject = prefabs[0];
                        List<KeyValuePair<Type, List<List<Component>>>> map = EditorUtils.OrderedComponentMap(prefabs, this, true);
                        int indexOfLast = Array.IndexOf(prefabs, lastGameObject);
                        List<Component> orderedComponents = new List<Component>();
                        foreach (var entry in map)
                        {
                            orderedComponents.AddRange(entry.Value.ElementAt(indexOfLast));
                        }
                        components = orderedComponents.ToArray();
                        TrackComponentMaps(components, PrefabComponentMapManager.componentMaps, true);
                    }
                }
            }
        }

        void TrackComponentMaps(Component[] components, List<ComponentMap> maps, bool prefabAsset = false)
        {
            if (maps == null || components == null || components.Length == 0)
            {
                return;
            }

            bool rebuild = false;
            bool added = false;
            if (maps.Count != components.Length/* && (prefabAsset || !AreWeFilteringComponents())*/)
            {
                //Debug.Log("Component count mismatch " + maps.Count + " " + components.Length);
                rebuild = true;
                if (maps.Count < components.Length)
                {
                    added = true;
                }
            }
            else if (GetActiveTab().RemovedComponentsChanged())
            {
                rebuild = true;
            }
            else
            {
                for (int i = 0; i < components.Length; i++)
                {
                    var component = components[i];
                    if (component != null)
                    {
                        ComponentMap map = maps[i];
                        if (map == null)
                        {
                            rebuild = true;
                            break;
                        }
                        if (map.component != component)
                        {
                            //Debug.Log(i + "Component mismatch. Map: " + map.component + " Component: " + component);
                            rebuild = true;
                            break;
                        }
                    }
#if UNITY_6000_4_OR_NEWER
                    else if (!maps[i].missingComponentID.IsValid())

#else
                    else if (maps[i].missingComponentID == -1)

#endif

                    {
                        //Debug.Log(i + "Component mismatch. " + maps[i].component.GetType());
                        rebuild = true;
                        break;
                    }
                }
            }
            if (rebuild)
            {

                if (prefabAsset)
                {
                    if (assetOnlyMode)
                    {
                        DrawCurrentAssets();
                    }
                    else
                    {
                        ReinitializePrefabComponentEditors();
                    }
                    if (added)
                    {
                        int addedIndex = EditorUtils.GetAddedComponentIndex(components, maps);

                        if (addedIndex == -1)
                        {
                            assetScrollView.ScrollToBottom();
                        }
                        else
                        {
                            if (!EditorUtils.HasProblematicScrollComponent(components[0].gameObject))
                            {
                                assetScrollView.ScrollToElement("Component" + addedIndex, true, true);
                            }
                        }
                    }
                }
                else
                {
                    ReinitializeComponentEditors();
                    if (added)
                    {
                        int addedIndex = EditorUtils.GetAddedComponentIndex(components, maps);
                        if (addedIndex == -1)
                        {
                            componentScrollView.ScrollToBottom();
                            ComponentHighlighter.StartHighlight("Component" + (components.Length - 1), CustomColors.CustomGreen);
                        }
                        else
                        {
                            rootVisualElement.schedule.Execute(() =>
                            {
                                if (!EditorUtils.HasProblematicScrollComponent(components[0].gameObject))
                                {
                                    componentScrollView.ScrollToElement("Component" + addedIndex, true, true);
                                }
                                ComponentHighlighter.StartHighlight("Component" + addedIndex, CustomColors.CustomGreen);
                            }).StartingIn(100);
                            ;
                        }
                    }
                }

            }

        }

        private void OnResize(GeometryChangedEvent scroll)
        {
            Rect rect = scroll.newRect;
            Rect rect1 = scroll.oldRect;
            //Debug.Log(rect + " " + rect1);
            if (rect.height != rect1.height)
            {
                CullComponents();
            }
        }
        private void RepaintGeometry(GeometryChangedEvent scroll)
        {
            rootVisualElement.MarkDirtyRepaint();
        }
        private void CullNow(GeometryChangedEvent scroll)
        {
            Rect rect = scroll.newRect;
            Rect rect1 = scroll.oldRect;
            if (rect.height == 0)
            {
                return;
            }
            _UpdateTabScroll();
            if (rect.height != rect1.height)
            {
                //GetActiveTab().MarkMaterialsForRebuild();
                CullNow(true);
                UpdateTabScroll();
            }
        }
        internal void _UpdateTabScroll()
        {
            if (componentScrollView != null && GetActiveTab() != null && IsActiveTabValid() && !float.IsNaN(componentScrollView.contentRect.height))
            {
                GetActiveTab().scrollPosition = componentScrollView.scrollOffset.y;
            }
        }
        internal void UpdateTabScroll()
        {
            if (componentScrollView != null)
            {

                //GetActiveTab().scrollPosition = componentScrollView.scrollOffset.y;
            }
        }
        internal void CullNow(bool _override = false)
        {
            if (_override)
            {
                RequestAction(DoCullNow);
            }
            else
            {
                RequestAction(DoCullIfFocused);
            }
        }

        private void DoCullIfFocused()
        {
            if (EditorWindow.focusedWindow != this)
            {
                return;
            }
            ResetComponentCulling();
            rootVisualElement.schedule.Execute(() =>
            {
                CullComponents();
            });
        }
        private void DoCullNow()
        {
            rootVisualElement.schedule.Execute(() =>
             {
                 DoCullComponents();
             });
            /* ResetComponentCulling();
             rootVisualElement.schedule.Execute(() =>
             {
                 DoCullComponents();
             });*/
        }

        private void OnScroll(float scroll)
        {
            CullComponents();
            _UpdateTabScroll();
            // UpdateTabScroll();
        }

        void CullComponents()
        {
            RequestAction(DoCullComponents);
        }
        void DoCullComponents()
        {
            if (componentScrollView == null || gameObjectBox == null || GetActiveTab() == null || IsActiveTabNew())
            {
                return;
            }
            Rect viewportRect = componentScrollView.contentViewport.worldBound;
            float viewportTop = viewportRect.y;
            float viewportBottom = viewportRect.y + viewportRect.height;
            bool oneVisible = false;

            List<VisualElement> elements = gameObjectBox.GetChildrenComponents();
            for (int i = 0; i < elements.Count; i++)
            {
                VisualElement element = elements[i];
                VisualElement componentElement = element;
                ComponentMap map = GetActiveTab().GetMapWithIndex(i);
                if (element != null)
                {
                    Rect elementRect = element.worldBound;
                    bool nullRect = elementRect.x == 0 && elementRect.y == 0;
                    bool isVisible = nullRect ||
                        (elementRect.y + elementRect.height >= viewportTop && elementRect.y <= viewportBottom);
                    if (isVisible)
                    {
                        if (element.style.display == DisplayStyle.Flex)
                        {
                            oneVisible = true;
                        }
                        componentElement.visible = true;
                        element.style.height = StyleKeyword.Initial;

                        if (map != null && !float.IsNaN(elementRect.height))
                        {
                            map.height = elementRect.height;
                        }
                    }
                    else if (map != null && !float.IsNaN(map.height))
                    {
                        element.style.height = map.height;
                        componentElement.visible = false;
                    }
                }
            }
            EditorUtils.UpdateScrollVisibilityOnce(componentScrollView, 1);

            if (!oneVisible && componentScrollView.contentRect.height > 0)
            {
                if (AreWeFilteringComponents())
                {
                    return;
                }
                if (IsActiveTabValidMulti())
                {
                    if (componentEditors != null && componentEditors.Length == 0)
                    {
                        return;
                    }
                }
                DrawCurrentComponentsContainer(true);
                // ResetComponentCulling(); // Commented out but could be re-enabled if needed
            }
        }

        void ResetComponentCulling()
        {
            //DoCullComponents();
            //RequestAction(DoResetComponentCulling);
            //DoResetComponentCulling();
        }

        void DoResetComponentCulling()
        {
            if (GetActiveTab() == null || IsActiveTabNew())
            {
                return;
            }
            EditorUtils.UpdateScrollVisibilityOnce(componentScrollView, 0);
            if (componentScrollView == null || gameObjectBox == null)
            {
                return;
            }
            List<VisualElement> elements = gameObjectBox.GetChildrenComponents();
            for (int i = 0; i < elements.Count; i++)
            {
                VisualElement element = elements[i];
                ComponentMap map = GetActiveTab().GetMapWithIndex(i);
                if (element != null)
                {
                    element.visible = true;
                    if (map != null && element.worldBound.height != 0)
                    {
                        map.height = element.worldBound.height;
                    }
                }
            }
            //   Repaint();
        }
        void DrawComponentBar(Editor editor, int index, int componentCount, ComponentMap componentMap, bool isPrefab = false)
        {
            if (editor.serializedObject == null || !editor.serializedObject.targetObject)
            {
                return;
            }
            Component component = editor.target as Component;
            bool isMulti = editor.targets.Length > 1;
            if (isMulti)
            {
                component = editor.targets[0] as Component;
            }
            EditorGUILayout.BeginVertical();
            EditorGUILayout.BeginHorizontal(CustomGUIStyles.InspectorButtonStyle);
            componentMap.index = index;
            bool debug = isPrefab ? debugAsset : ActiveTabInDebugMode();
            bool componentHidden = !EditorUtils.HasVisibleFields(editor) && !debug;
            bool isDragged = false;
            bool isCopied = false;

            if (!isPrefab)
            {
                DrawComponentIsDragged(componentMap, component, index, out isDragged, out isCopied);
            }
            bool originalFlag = componentMap.foldout;
            bool flag;
            GUI.SetNextControlName("Component" + index);
            if (!isMulti)
            {
                flag = EditorGUILayout.InspectorTitlebar(componentMap.foldout, editor);

            }
            else
            {
                flag = EditorGUILayout.InspectorTitlebar(componentMap.foldout, editor);
            }
            EditorGUILayout.EndHorizontal();
            if (componentHidden)
            {
                flag = false;
                componentMap.hidden = true;
            }
            else
            {
                componentMap.hidden = false;
            }
            Rect compHeaderRect = GUILayoutUtility.GetLastRect();
            DrawAfterComponentBar(isDragged, isCopied, componentMap, compHeaderRect, index, flag);
            EditorGUILayout.EndVertical();
            componentMap.foldout = flag;
            bool changed = flag != originalFlag;
            if (changed)
            {
                bool shouldIgnore = (EditorUtils.AreWeDragging() && Event.current.mousePosition.x > 30) || triggeringADrag;
                if (shouldIgnore)
                {
                    componentMap.foldout = originalFlag;
                }
                else
                {
                    CullNow(true);
                    if (!isPrefab)
                    {
                        ResetComponentCulling();
                    }

                    Repaint();
                }
            }
            Rect rect1 = new Rect(compHeaderRect);
            rect1.x = 0;
            EditorUtils.DrawLineOverRect(rect1, -1);
            if (!flag || componentHidden)
            {
                EditorUtils.DrawLineUnderRect(rect1, CustomColors.SoftShadow, -1, 2);
            }
            else
            {
                EditorUtils.DrawLineUnderRect(rect1, CustomColors.MediumShadow);
                Rect underRect = new Rect(rect1);
                underRect.y += 22;
                underRect.height = 4;
                EditorUtils.DrawFadeToBottom(underRect, CustomColors.SoftShadow);
            }

        }

        void DrawHeaderShadowContainer()
        {
            if (componentScrollView.scrollOffset.y > 0)
            {
                Color shadowColor = CustomColors.GradientShadow;
                int height = 20;
                if (EditorUtils.IsLightSkin())
                {
                    shadowColor = CustomColors.SimpleShadow;
                    height = 12;
                }
                headerRect = new Rect(0, 0, position.width, 10);
                EditorUtils.DrawLineOverRect(headerRect, CustomColors.GradientShadow, 0);
                EditorUtils.DrawLineOverRect(headerRect, CustomColors.DefaultInspector, 1);
                EditorUtils.DrawFadeToBottom(headerRect, shadowColor, height);
            }
        }
        void DrawAssetShadowContainer()
        {
            if (assetScrollView.scrollOffset.y > 1 && assetScrollView.contentViewport.worldBound.height > 5)
            {
                Color shadowColor = CustomColors.SimpleShadow;
                int height = 20;
                if (EditorUtils.IsLightSkin())
                {
                    shadowColor = CustomColors.SimpleShadow;
                    height = 14;
                }
                headerRect = new Rect(0, -1, position.width, 14);
                EditorUtils.DrawLineOverRect(headerRect, CustomColors.GradientShadow, 0);
                EditorUtils.DrawLineOverRect(headerRect, CustomColors.DefaultInspector, 1);
                EditorUtils.DrawFadeToBottom(headerRect, shadowColor, height);
            }
            if (assetScrollView.scrollOffset.y + 1 < assetScrollView.verticalScroller.highValue && assetScrollView.contentViewport.worldBound.height > 10)
            {
                Color shadowColor = CustomColors.SimpleShadow;
                int height = 20;
                if (EditorUtils.IsLightSkin())
                {
                    shadowColor = CustomColors.SimpleShadow;
                    height = 12;
                }
                float y = assetScrollView.contentViewport.worldBound.height;
                headerRect = new Rect(0, y - 13, position.width, 13);
                EditorUtils.DrawFadeToTop(headerRect, shadowColor, height);
            }
        }
        void DrawAddComponentShadowContainer()
        {
            if (componentScrollView.scrollOffset.y < componentScrollView.verticalScroller.highValue)
            {
                Color shadowColor = CustomColors.SimpleShadow;
                int height = 20;
                if (EditorUtils.IsLightSkin())
                {
                    shadowColor = CustomColors.SimpleShadow;
                    height = 12;
                }
                headerRect = new Rect(0, -10, position.width, 10);
                EditorUtils.DrawFadeToTop(headerRect, shadowColor, height);
            }
        }

        public void CreateCursorOverlay(VisualElement targetElement, MouseCursor cursorType = MouseCursor.Arrow, int expand = 0, VisualElement placementElement = null, bool isComponentBar = false)
        {
            float width = targetElement.resolvedStyle.width;
            float height = targetElement.resolvedStyle.height;
            float y = targetElement.layout.y;
            VisualElement overlay = new VisualElement();
            overlay.style.position = Position.Absolute;
            overlay.style.left = 0;
            overlay.style.top = targetElement.layout.y;
            overlay.style.width = targetElement.worldBound.width;
            overlay.style.height = targetElement.worldBound.height;
            overlay.pickingMode = PickingMode.Ignore;

            IMGUIContainer cursorContainer = new IMGUIContainer(() =>
            {
                if (EditorUtils.AreWeDragging() || editingHeader || middleScrolling || EditorGUIUtility.editingTextField)
                {
                    return;
                }
                y = targetElement.layout.y;
                width = targetElement.resolvedStyle.width;
                height = targetElement.resolvedStyle.height;
                if (expand == -1)
                {
                    height += y;
                    y = 0;
                }
                else if (expand == 1)
                {
                    height = position.height - y;
                }
                Rect rect = new Rect(0, y, width, height);

                if (isComponentBar && IsValidAssetTarget())

                {
                    bool newTabNoAssetCase = IsActiveTabNew() && !IsValidAssetTarget();
                    if (!assetsCollapsed && !newTabNoAssetCase)
                    {
                        leftHandleRect.y = y;
                        rightHandleRect.y = y;
                        if (GetActiveTab().hasPreview)
                        {
                            leftHandleRect.y += GetActiveTab().previewHeight;
                            rightHandleRect.y += GetActiveTab().previewHeight;
                        }
                        bool updateCase = !hideUpdateAlert && UpdateChecker.IsUpdateAvailable && position.width > 322;
                        if (updateCase)
                        {
                            leftHandleRect.width -= 55;
                            leftHandleRect.x += 55;
                        }
                        Rect middleRect = new Rect(leftHandleRect.xMax, y, rightHandleRect.x - leftHandleRect.xMax, height);
                        EditorGUIUtility.AddCursorRect(leftHandleRect, MouseCursor.ResizeVertical);
                        EditorGUIUtility.AddCursorRect(middleRect, MouseCursor.Arrow);
                        EditorGUIUtility.AddCursorRect(rightHandleRect, MouseCursor.ResizeVertical);
                        rect.y += 36;
                        rect.height -= 36;
                        EditorGUIUtility.AddCursorRect(rect, cursorType);
                    }


                }
                else
                {
                    if (!resizingAssetView && !middleScrolling)
                    {
                        EditorGUIUtility.AddCursorRect(rect, cursorType);
                    }
                    else
                    {
                        EditorGUIUtility.AddCursorRect(rect, MouseCursor.ResizeVertical);
                    }

                }

            });

            overlay.Add(cursorContainer);
            overlay.name = "CursorOverlay" + targetElement.name;
            targetElement.parent.Replace(overlay);
            if (placementElement != null)
            {
                overlay.PlaceBehind(placementElement);
            }
            else
            {
                overlay.PlaceInFront(targetElement);
            }
        }

        void DrawMaximizeAssetViewContainer()
        {
            if (resizingAssetView && !InRecoverScreen() && addComponentBox != null && !assetsCollapsed)
            {
                Rect windowRect = new Rect(0, 0, position.width, addComponentBox.worldBound.y);

                EditorGUIUtility.AddCursorRect(windowRect, MouseCursor.ResizeVertical);
                if (Event.current.mousePosition.y < 100)
                {
                    EditorGUI.DrawRect(windowRect, CustomColors.FadeBlue);
                    string labelText = "Release to Enter\nAsset-Only Mode";
                    Color shadowColor = Color.black;
                    Vector2 shadowOffset = new Vector2(1, 2);
                    Color originalColor = GUI.contentColor;
                    GUI.contentColor = shadowColor;
                    GUI.Label(new Rect(windowRect.x + shadowOffset.x, windowRect.y + shadowOffset.y, windowRect.width, windowRect.height), labelText, CustomGUIStyles.BigCenterLabel);
                    shadowOffset = new Vector2(0, 2);
                    GUI.Label(new Rect(windowRect.x + shadowOffset.x, windowRect.y + shadowOffset.y, windowRect.width, windowRect.height), labelText, CustomGUIStyles.BigCenterLabel);
                    shadowColor.a = 0.3f;
                    shadowOffset = new Vector2(-1, -1);
                    GUI.contentColor = shadowColor;
                    GUI.Label(new Rect(windowRect.x + shadowOffset.x, windowRect.y + shadowOffset.y, windowRect.width, windowRect.height), labelText, CustomGUIStyles.BigCenterLabel);
                    shadowOffset = new Vector2(0, 3);
                    GUI.Label(new Rect(windowRect.x + shadowOffset.x, windowRect.y + shadowOffset.y, windowRect.width, windowRect.height), labelText, CustomGUIStyles.BigCenterLabel);
                    GUI.contentColor = originalColor;
                    GUI.Label(windowRect, labelText, CustomGUIStyles.BigCenterLabel);
                    GUI.contentColor = originalColor;
                    windowRect.height = 3;
                    EditorGUI.DrawRect(windowRect, CustomColors.CustomBlue);
                    maximizeMode = 2;
                    rootVisualElement.MarkDirtyRepaint();
                }
                else
                {
                    maximizeMode = 0;
                }
            }
        }

        void DrawComponentBarContainer()
        {
            //  if (!IsActiveTabNew() && IsActiveTabValid())
            {
                bool darken = IsValidAssetTarget();
                Color backgroundColor = GUI.backgroundColor;
                if (darken)
                {
                    Rect rect = GUILayoutUtility.GetRect(0, 0);
                    EditorUtils.DrawLineUnderRect(rect, 0);
                    EditorUtils.DrawLineUnderRect(rect, CustomColors.SimpleBright);
                    if (!EditorUtils.IsLightSkin())
                    {
                        GUI.backgroundColor -= CustomColors.BackDarkColor;
                    }
                    else
                    {
                        GUI.backgroundColor = Color.gray / 4.5f;
                    }
                }
                DrawAddComponentBar(backgroundColor);
            }
            HandleAssetViewResize();
        }

        void DrawNewTabContainer()
        {
            if (IsActiveTabNew())
            {
                StartMessageGUI();
            }
        }

        void DrawTabTipController()
        {
            if (!dragging && !assetOnlyMode)
            {
                PopUpTip.ShowGUI();
            }
        }
        void DrawLastComponentLine()
        {
            if (!IsActiveTabNew())
            {
                if (GetActiveTab() != null && GetActiveTab().componentMaps != null)
                {
                    GUILayout.BeginHorizontal();
                    GUILayout.Space(0);
                    GUILayout.EndHorizontal();
                    Rect rect = GUILayoutUtility.GetLastRect();

                    EditorUtils.DrawLineUnderRect(rect, CustomColors.HardShadow, 0);
                    EditorUtils.DrawLineUnderRect(rect, CustomColors.SimpleBright, 1);
                }
            }
        }
        void DrawPrefabLastComponentLine()
        {
            if (IsAPrefabTarget() && assetOnlyMode)
            {
                if (PrefabComponentMapManager.componentMaps != null && PrefabComponentMapManager.componentMaps.Count > 0)
                {
                    ComponentMap map = PrefabComponentMapManager.GetLastComponentMap();
                    if (map == null)
                    {
                        return;
                    }
                    GUILayout.BeginHorizontal();
                    GUILayout.EndHorizontal();
                    Rect rect = GUILayoutUtility.GetLastRect();
                    if (!map.foldout)
                    {
                        EditorUtils.DrawLineUnderRect(rect, CustomColors.HardShadow, -1);
                        EditorUtils.DrawLineUnderRect(rect, CustomColors.SimpleBright, 0);
                    }
                    else
                    {
                        EditorUtils.DrawLineUnderRect(rect, CustomColors.HardShadow, -1);
                        EditorUtils.DrawLineUnderRect(rect, CustomColors.SimpleBright, 0);
                    }
                }
            }
        }

        internal void ClearCursorOverlay()
        {
            if (cursorOverlay != null)
            {
                cursorOverlay.Clear();
                cursorOverlay.RemoveFromHierarchy();
            }
        }

        void RebuildHeaderContainer()
        {
            componentScrollView.RemoveIfPresent("NewTab");
            if (headerBox != null)
            {
                headerBox.Clear();
                headerBox.RemoveFromHierarchy();
            }
            headerBox = new VisualElement
            {
                name = "Header"
            };

            EditorUtils.SetStaticElement(headerBox);
            IMGUIContainer drawHeader = new IMGUIContainer(() =>
            {
                GUI.SetNextControlName("IMGUIContainerInitialization");
                GUI.GetNameOfFocusedControl();
                EditorUtils.SetElementVisible(headerBox, !assetOnlyMode);
                DrawHeaderContainer();
            });
            drawHeader.focusable = false;
            if (gameObjectEditor != null && !IsActiveTabNew())
            {
                if (gameObjectEditor.serializedObject.isEditingMultipleObjects)
                {
                    GameObject lastCreated = GetActiveTab().MultiTrackingTarget();
                    SerializedObject serializedObject = new SerializedObject(lastCreated);
                    drawHeader.TrackSerializedObjectValue(serializedObject, KeepTrackOfGameObject);
                }
                else
                {
                    drawHeader.TrackSerializedObjectValue(gameObjectEditor.serializedObject, KeepTrackOfGameObject);
                }

            }
            EditorUtils.SetStaticElement(drawHeader);
            headerBox.Add(drawHeader);
            if (IsActiveTabValidMulti())
            {
                IMGUIContainer drawMultiHeaderUnder = new IMGUIContainer(DrawMultiHeaderUnderContainer);
                EditorUtils.SetStaticElement(drawMultiHeaderUnder);
                drawMultiHeaderUnder.style.top = 0;
                headerBox.Add(drawMultiHeaderUnder);
            }
            rootVisualElement.Replace(headerBox);
            headerBox.PlaceBefore(rootVisualElement, "HideHeader");
            CreateCursorOverlay(headerBox, MouseCursor.Arrow, -1);
            if (IsActiveTabNew())
            {
                IMGUIContainer newTab = new IMGUIContainer(DrawNewTabContainer);
                newTab.name = "NewTab";
                newTab.style.flexShrink = 1;
                newTab.style.flexGrow = 1;
                newTab.style.position = Position.Relative;
                componentScrollView.Add(newTab);
            }
        }
        void CreateGUI()
        {
            _CreateGUI(0);
        }
        void _CreateGUI(float currentScroll)
        {
            float currentHeight = -1;
            if (assetBox != null && !assetsCollapsed && !assetOnlyMode && !InRecoverScreen())
            {
                currentHeight = assetBox.style.height.value.value;
            }
            DestroyAllIfNotNull(componentEditors);
            if (currentScroll == 0)
            {
                if (componentScrollView != null)
                {
                    currentScroll = componentScrollView.scrollOffset.y;
                }
                else if (GetActiveTab() != null)
                {
                    currentScroll = GetActiveTab().scrollPosition;
                }
            }
            isScheduled = false;
            rootVisualElement.Clear();
            IMGUIContainer drawTabBar = new IMGUIContainer(DrawTabBarContainer);
            drawTabBar.name = "TabBar";
            drawTabBar.style.top = 0;
            EditorUtils.SetStaticElement(drawTabBar);
            drawTabBar.focusable = true;
            drawTabBar.pickingMode = PickingMode.Position;
            rootVisualElement.Add(drawTabBar);

            IMGUIContainer hideHeader = new IMGUIContainer(HideHeaderMargin);
            hideHeader.name = "HideHeader";
            hideHeader.style.position = Position.Absolute;
            hideHeader.style.top = 0;
            EditorUtils.SetStaticElement(hideHeader);
            rootVisualElement.Add(hideHeader);
            IMGUIContainer drawTabBarDecorations = new IMGUIContainer(DrawTabBarDecorationsContainer);
            drawTabBarDecorations.style.position = Position.Absolute;
            drawTabBarDecorations.style.top = 0;
            EditorUtils.SetStaticElement(drawTabBarDecorations);
            rootVisualElement.Add(drawTabBarDecorations);

            //We create this now so Tab Previews don't get lost on first load
            addComponentBox = new VisualElement();

            //GAMEOBJECT VIEW
            ReinitializeComponentEditors();
            RebuildGameObjectView();
            DrawCurrentComponentsContainer();
            IMGUIContainer maximizeAssetView = new IMGUIContainer(DrawMaximizeAssetViewContainer);
            EditorUtils.SetStaticElement(maximizeAssetView);
            maximizeAssetView.style.position = Position.Absolute;
            maximizeAssetView.style.top = 0;
            rootVisualElement.Add(maximizeAssetView);

            //ADD COMPONENT BAR            
            addComponentBox.name = "AddComponentBox";
            EditorUtils.SetStaticElement(addComponentBox);
            IMGUIContainer drawComponentBar = new IMGUIContainer(DrawComponentBarContainer);
            EditorUtils.SetStaticElement(drawComponentBar);
            IMGUIContainer drawComponentShadow = new IMGUIContainer(DrawAddComponentShadowContainer);
            EditorUtils.SetStaticElement(drawComponentShadow);
            drawComponentShadow.style.position = Position.Absolute;
            addComponentBox.Add(drawComponentShadow);
            addComponentBox.Add(drawComponentBar);
            rootVisualElement.Add(addComponentBox);
            CreateCursorOverlay(addComponentBox, MouseCursor.Arrow, 1, drawTabBarDecorations, true);

            //ASSET VIEW            
            assetBox = new VisualElement();
            assetScrollView = new SmoothScrollView();
            assetScrollView.style.overflow = Overflow.Hidden;
            assetScrollView.verticalScrollerVisibility = ScrollerVisibility.Auto;
            assetScrollView.name = "AssetScrollView";
            rootVisualElement.Add(assetBox);

            //POPUP TIP CONTROLLER
            IMGUIContainer PopUpTipController = new IMGUIContainer(DrawTabTipController);
            PopUpTipController.style.position = Position.Absolute;
            rootVisualElement.Add(PopUpTipController);

            //EVENT HANDLERS
            componentScrollView.verticalScroller.valueChanged += OnScroll;
            gameObjectBox.ReplaceCallback<GeometryChangedEvent>(CullNow);
            componentScrollView.ReplaceCallback<GeometryChangedEvent>(OnResize);
            assetScrollView.ReplaceCallback<GeometryChangedEvent>(RepaintGeometry);
            addComponentBox.ReplaceCallback<MouseDownEvent>(ManageAssetResize, TrickleDown.TrickleDown);
            addComponentBox.ReplaceCallback<MouseUpEvent>(ManageAssetResize, TrickleDown.NoTrickleDown);
            rootVisualElement.ReplaceCallback<MouseDownEvent>(OnMouseEvent, TrickleDown.TrickleDown);
            rootVisualElement.panel?.visualTree.ReplaceCallback<MouseUpEvent>(OnMouseEvent, TrickleDown.NoTrickleDown);
            rootVisualElement.panel?.visualTree.ReplaceCallback<MouseMoveEvent>(OnMouseEvent, TrickleDown.TrickleDown);
            rootVisualElement.panel?.visualTree.ReplaceCallback<KeyDownEvent>(OnKeyEvent, TrickleDown.TrickleDown);
            rootVisualElement.panel?.visualTree.ReplaceCallback<WheelEvent>(OnWheelEvent, TrickleDown.TrickleDown);
            rootVisualElement.ReplaceCallback<KeyUpEvent>(OnKeyEvent, TrickleDown.TrickleDown);
            rootVisualElement.ReplaceCallback<DragUpdatedEvent>(OnDragEvent, TrickleDown.TrickleDown);
            rootVisualElement.ReplaceCallback<DragPerformEvent>(OnDragEvent, TrickleDown.TrickleDown);
            rootVisualElement.ReplaceCallback<AttachToPanelEvent>(OnAttachToPanel);
            rootVisualElement.ReplaceCallback<DetachFromPanelEvent>(DettachFromPanel);
            DrawCurrentAssets(true);
            if (currentHeight > 0)
            {
                assetBox.style.height = currentHeight;
            }
            if (currentScroll != 0)
            {
                SetScrollPosition(currentScroll);
            }
        }

        internal void OnAttachToPanel(AttachToPanelEvent evt)
        {
            rootVisualElement.panel?.visualTree.ReplaceCallback<MouseUpEvent>(OnMouseEvent, TrickleDown.NoTrickleDown);
            rootVisualElement.panel?.visualTree.ReplaceCallback<MouseMoveEvent>(OnMouseEvent, TrickleDown.TrickleDown);
            rootVisualElement.panel?.visualTree.ReplaceCallback<KeyDownEvent>(OnKeyEvent, TrickleDown.TrickleDown);
            rootVisualElement.panel?.visualTree.ReplaceCallback<WheelEvent>(OnWheelEvent, TrickleDown.TrickleDown);
            CreateGUI();
        }
        internal void DettachFromPanel(DetachFromPanelEvent evt)
        {

            if (evt.originPanel != null)
            {
                evt.originPanel.visualTree.UnregisterCallback<MouseUpEvent>(OnMouseEvent, TrickleDown.NoTrickleDown);
                evt.originPanel.visualTree.UnregisterCallback<MouseMoveEvent>(OnMouseEvent, TrickleDown.TrickleDown);
                evt.originPanel.visualTree.UnregisterCallback<KeyDownEvent>(OnKeyEvent, TrickleDown.TrickleDown);
                evt.originPanel.visualTree.UnregisterCallback<WheelEvent>(OnWheelEvent, TrickleDown.TrickleDown);

            }
        }

        internal void DrawCurrentAssets(bool rebuilding = false)
        {
            DoDrawCurrentAssets(rebuilding);
        }
        internal void DoDrawCurrentAssets(bool rebuilding = false)
        {
            if (AssetTargetMode() == 0)
            {
                if (assetBox != null)
                {
                    assetBox.Clear();
                    assetBox.style.minHeight = 0;
                    EditorUtils.SetElementVisible(assetBox, false);
                }
                return;
            }
            if (assetEditor == null)
            {
                return;
            }

            if (assetBox != null && assetBox.parent == rootVisualElement)
            {
                assetBox.Clear();
                rootVisualElement.Remove(assetBox);
            }
            assetBox = new VisualElement();
            assetBox.style.flexGrow = assetOnlyMode ? 1 : 0;
            EditorUtils.SetElementVisible(assetBox, true);
            rootVisualElement.Add(assetBox);
            if (assetScrollView != null)
            {
                assetScrollView.Clear();
            }
            assetScrollView = new SmoothScrollView();
            assetScrollView.style.overflow = Overflow.Hidden;
            assetScrollView.verticalScrollerVisibility = ScrollerVisibility.Auto;
            assetScrollView.mouseWheelScrollSize = mouseWheelSensitivity;
            //assetScrollView.ApplySoftScrolling(this);
            assetScrollView.name = "AssetScrollView";
            if (footerBar != null)
            {
                footerBar.Clear();
            }
            VisualElement assetView = new VisualElement();
            assetView.style.flexGrow = 1;
            assetView.style.flexShrink = assetOnlyMode ? 1 : 1;
            VisualElement assetHeader = null;
            VisualElement assetInspector = null;
            VisualElement assetImportHeader = null;
            assetImportSettings = null;
            IMGUIContainer preview = null;
            VisualElement assetLabels = null;
            VisualElement flex = new VisualElement();

            flex.style.flexGrow = 1;
            bool isMulti = AssetTargetMode() == 2;
            AssetInfo assetInfo = PoolCache.GetAssetInfo(targetObject);
            if (isMulti)
            {
                assetInfo = PoolCache.GetAssetInfo(targetObjects[0]);
            }
            //assetInfo.singleHeader = false;
            bool sameType = isMulti && SortedAssetSelection.Count == 1;
            bool isPrefab = assetInfo.isPrefab;
            bool isImportedObject = assetInfo.isImportedObject;
            bool isNested = !assetInfo.isMainAsset;
            bool isMissingScript = IsMissingAssetTarget();
            bool isTransition = assetEditor.GetType().ToString() == "UnityEditor.Graphs.AnimationStateMachine.AnimatorStateTransitionInspector";
            if (!isTransition && !isMulti)
            {
                isTransition = targetObject is UnityEditor.Animations.BlendTree;
            }
            MissingScriptManager missingScript = isMissingScript ? targetObject as MissingScriptManager : null;
            IMGUIContainer assetBar = new IMGUIContainer(() =>
            {
                DrawAssetBar(assetInfo.isFolder, assetInfo.isPrefab, false, isMulti, sameType);
            });
            assetBar.name = "AssetBar";
            EditorUtils.SetStaticElement(assetBar);
            assetBar.style.flexBasis = StyleKeyword.Auto;
            assetBox.Add(assetBar);
            IMGUIContainer shadowContainer = new IMGUIContainer(() =>
           {
               DrawAssetShadowContainer();
           });
            assetBox.name = "AssetView";
            shadowContainer.style.position = Position.Absolute;
            EditorUtils.SetStaticElement(shadowContainer);

            if (sameType || !isMulti)
            {
                bool overrideEditorWithImport = ImportEditorOverridesInspector(assetEditor) && assetImportSettingsEditor != null;

                bool hasImportSettings = assetImportSettingsEditor != null;
                bool hasImportPreview = hasImportSettings && assetImportSettingsEditor._HasPreviewGUI();
                if (isImportedObject && hasImportSettings && !hasImportPreview)
                {
                    Editor modelClipEditor = Reflected.GetModelClipEditor(assetImportSettingsEditor);
                    hasImportPreview = modelClipEditor ? modelClipEditor._HasPreviewGUI() : false;
                }

                bool shouldDrawHeaders = ShouldDrawImportSettings() && !isPrefab;
                bool hasPreview = assetEditor._HasPreviewGUI() && !debugAsset;
                bool isSprite = targetObject is Sprite;
                if (!debugAsset)
                {

                    if (odinInspectorPresent && targetObject is ScriptableObject)
                    {
                        var imgui = new InspectorElement(overrideEditorWithImport ? assetImportSettingsEditor : assetEditor);
                        imgui.style.minHeight = 50;
                        assetInspector = imgui;

                    }
                    else
                    {
                        assetInspector = new InspectorElement(overrideEditorWithImport ? assetImportSettingsEditor : assetEditor);
                    }

                    if (isMissingScript)
                    {
                        EditorUtils.SetElementVisible(assetInspector, false);
                    }

                }
                else
                {
                    assetInspector = EditorUtils.DebugInspector(assetEditor);
                }

                assetInspector.name = "AssetInspector";
                assetInspector.style.paddingBottom = 8;
                if (hasImportSettings && !overrideEditorWithImport)
                {
                    assetImportSettings = new InspectorElement(assetImportSettingsEditor);
                    assetImportSettings.name = "ImportSettingsInspector";
                    assetImportSettings.style.paddingBottom = 8;
                    assetImportSettings.style.flexGrow = 1;
                    assetImportSettings.style.flexShrink = 1;
                    assetImportHeader = new IMGUIContainer(() =>
                    {
                        DrawHeader(assetImportSettingsEditor);
                    });
                    assetImportHeader.name = "ImportSettingsHeader";

                    if (assetImportSettingsEditor.target is TextureImporter)
                    {

                        EditorUtils.SetElementVisible(assetInspector, false);
                    }
                    EditorUtils.SetStaticElement(assetImportHeader);
                }
                assetHeader = new IMGUIContainer(() =>
                {
                    if (!isMissingScript)
                    {
                        DrawHeader(assetEditor);
                    }
                    else
                    {
                        DrawMissingAssetScriptHeader(isMulti);
                    }
                    if (targetObject is Sprite && hasImportSettings)
                    {
                        EditorUtils.SetElementVisible(assetImportSettings, false);
                    }
                });
                assetHeader.name = "AssetHeader";

                bool hasImportHeader = assetImportHeader != null;
                EditorUtils.SetElementVisible(assetImportSettings, ShouldDrawImportSettings());

                assetBox.RegisterOnce<GeometryChangedEvent>((evt) =>
                {
                    if (assetInspector != null)
                    {

                        float inspectorHeight = assetInspector.contentRect.height;
                        bool invisibleInspector = !assetsCollapsed && assetInspector.contentRect.height <= 5;
                        if (invisibleInspector)
                        {
                            EditorUtils.SetElementVisible(assetInspector, false);
                            if (!hasPreview && hasImportSettings && !ShouldDrawImportSettings())
                            {
                                EditorUtils.SetElementVisible(assetImportSettings, true);
                                assetBox.RegisterOnce<GeometryChangedEvent>(LimitAssetViewHeight);
                                return;
                            }
                            if (!hasImportSettings && !hasPreview)
                            {
                                EditorUtils.SetElementVisible(assetHeader, true);
                                assetInfo.singleHeader = true;
                            }
                        }
                    }
                    LimitAssetViewHeight(evt);
                });
                EditorUtils.SetElementVisible(assetHeader, missingScript != null || (!hasImportHeader) && (shouldDrawHeaders || (isMulti && sameType)));
                EditorUtils.SetElementVisible(assetImportHeader, hasImportHeader && (shouldDrawHeaders || (isMulti && sameType)));
                EditorUtils.SetElementVisible(assetView, !assetsCollapsed);
                EditorUtils.SetStaticElement(assetHeader);
                if (debugAsset)
                {
                    EditorUtils.SetElementVisible(assetHeader, true);
                    EditorUtils.SetElementVisible(assetInspector, true);
                    if (hasImportSettings)
                    {
                        EditorUtils.SetElementVisible(assetImportSettings, true);
                    }
                    if (hasImportHeader)
                    {
                        EditorUtils.SetElementVisible(assetImportHeader, true);
                    }
                }
                assetHeader.style.paddingBottom = 1;
                assetHeader.style.marginTop = -1;
                if (hasImportHeader)
                {
                    assetImportHeader.style.paddingBottom = 1;
                    assetImportHeader.style.marginTop = -1;
                    assetScrollView.Add(assetImportHeader);
                }
                assetScrollView.Add(assetImportSettings);
                assetScrollView.Add(assetHeader);
                if (assetInfo.isPrefab || (assetInfo.isImportedObject && (debugAsset || isNested || assetOnlyMode)))
                {
                    ReinitializePrefabComponentEditors();
                }
                assetScrollView.Add(assetInspector);
                assetInspector.style.flexGrow = 1;
                assetInspector.style.flexShrink = 1;
                if (isSprite && !assetOnlyMode)
                {
                    assetInspector.style.height = 80;
                }
                assetHeader.style.flexGrow = 0;
                assetHeader.style.flexShrink = 0;
                assetScrollView.style.flexGrow = 1;
                assetScrollView.style.flexShrink = 1;
                if (isMulti && sameType && hasImportHeader)
                {
                    assetScrollView.style.flexGrow = 0;
                }
                assetBox.style.flexGrow = 0;
                assetBox.style.flexShrink = 1;
                assetBox.style.flexDirection = FlexDirection.Column;

                if (hasPreview || hasImportPreview)
                {
                    preview = new IMGUIContainer();
                    preview.style.backgroundColor = new Color(0.18f, 0.18f, 0.19f);
                    float previewHeight = GetPreviewHeight();
                    preview.onGUIHandler = () =>
                    {
                        previewHeight = preview.contentRect.height;
                        if (previewHeight > 30)
                        {
                            var rect = GUILayoutUtility.GetRect(0, previewHeight);
                            GUI.BeginGroup(rect);
                            DrawPreview(rect);
                            GUI.EndGroup();
                        }
                        else
                        {
                            GUILayout.Space(previewHeight);
                        }
                    };
                    preview.AddPreviewBackground();
                    preview.name = "AssetPreview";
                    preview.style.height = previewHeight;
                    assetView.Add(assetScrollView);

                    if (isTransition || ShouldDrawImportSettings() || debugAsset || isPrefab || isMulti)
                    {
                        preview.style.minHeight = GetPreviewHeight();
                        preview.style.flexShrink = 0;
                    }
                    else
                    {
                        EditorUtils.SetElementVisible(assetScrollView, false);
                        preview.style.flexShrink = 1;

                    }
                    preview.style.flexGrow = 1;
                    if (assetOnlyMode)
                    {
                        preview.style.flexGrow = 0;
                    }
                    if (isTransition || ShouldDrawImportSettings() || debugAsset || isPrefab || isMulti)
                    {
                        var toolbar = CreateAssetPreviewBar(preview);
                        if (toolbar != null)
                        {
                            if (EditorUtils.IsLightSkin())
                            {
                                assetView.Add(EditorUtils.HorizontalLine(Color.gray));
                            }
                            assetView.Add(toolbar);
                            // if (AssetTargetMode() == 2 || !EditorUtils.IsLightSkin())
                            {
                                assetView.Add(EditorUtils.HorizontalLine(CustomColors.SimpleShadow));
                            }
                        }
                    }
                    assetView.Add(preview);
                }
                else
                {
                    assetView.Add(assetScrollView);
                }
            }
            else
            {
                IMGUIContainer summaryGUI = new IMGUIContainer(() =>
                {
                    DrawMultiAssetSummary();
                });
                summaryGUI.name = "MultiAssetInspector";
                summaryGUI.style.flexShrink = 1;
                summaryGUI.style.flexGrow = 1;
                summaryGUI.style.top = -1;
                assetScrollView.Add(summaryGUI);
                assetView.Add(assetScrollView);
                assetView.Add(flex);
                EditorUtils.SetElementVisible(assetView, !assetsCollapsed);
                assetView.style.backgroundColor = CustomColors.SoftShadow;
            }
            if (!isMulti)
            {
                assetLabels = new IMGUIContainer(() =>
                {
                    EditorUtils.DrawAssetLabelGUI(targetObject, showAssetLabels, assetsCollapsed, assetOnlyMode);
                    EditorUtils.SetElementVisible(assetLabels, showAssetLabels);
                });
                assetLabels.name = "AssetLabelFooter";
            }
            else
            {
                assetLabels = new IMGUIContainer(() =>
                {
                    EditorUtils.DrawAssetLabelGUI(targetObjects, showAssetLabels, assetsCollapsed, assetOnlyMode);
                    EditorUtils.SetElementVisible(assetLabels, showAssetLabels);
                });
                assetLabels.name = "MultiAssetLabelFooter";
            }
            if (isPrefab && !isImportedObject)
            {
                EditorUtils.SetElementVisible(assetInspector, false);
                EditorUtils.SetElementVisible(assetImportSettings, false);
                EditorUtils.SetElementVisible(assetHeader, true);
                EditorUtils.SetElementVisible(assetImportHeader, true);
                assetScrollView.style.flexGrow = 0;
            }
            if (isImportedObject)
            {
                assetScrollView.style.flexGrow = 0;
                if (isNested)
                {
                    EditorUtils.SetElementVisible(assetImportHeader, false);
                    EditorUtils.SetElementVisible(assetImportSettings, false);
                }
                EditorUtils.SetElementVisible(assetInspector, false);
            }
            if (assetOnlyMode && !isImportedObject && !isPrefab)
            {
                if (assetHeader != null)
                {
                    assetHeader.style.display = DisplayStyle.Flex;
                }
            }
            EditorUtils.SetStaticElement(assetLabels);
            footerBar = new IMGUIContainer(() =>
            {
                DrawAssetBottomBar(false, isMulti);
            });
            footerBar.name = "AssetFooterBar";
            EditorUtils.SetStaticElement(footerBar);
            assetBox.style.minHeight = 44;
            assetBox.Add(assetView);
            assetView.Add(assetLabels);
            assetView.Add(shadowContainer);
            assetBox.Add(footerBar);
            SetComponentViewVisibility();
            assetScrollView.LimitScrollbarVisibilityTo(1);
            IMGUIContainer scrollerController = ControlScrollerBarVisibility(assetScrollView);
            if (scrollerController != null)
            {
                EditorUtils.SetStaticElement(scrollerController);
                assetBox.Add(scrollerController);
            }
            if (assetHeader != null && assetHeader.style.display == DisplayStyle.Flex)
            {
                assetHeader.style.display = DisplayStyle.Flex;
            }
            if (IsHeaderAndPreview(assetHeader, assetImportHeader, preview))
            {

                assetScrollView.style.flexGrow = 0;
            }
            assetScrollView.style.flexGrow = assetPreviewExpanded && !assetOnlyMode ? 0 : 1;
        }
        internal VisualElement CreateAssetPreviewBar(VisualElement preview)
        {
            var toolbar = new VisualElement();
            toolbar.style.height = 12;
            toolbar.style.minHeight = 12;
            toolbar.style.maxHeight = 12;
            toolbar.style.flexDirection = FlexDirection.Row;
            toolbar.style.paddingLeft = 0;
            toolbar.style.paddingRight = 0;
            toolbar.style.backgroundColor = EditorUtils.IsLightSkin() ? new Color(0.75f, 0.75f, 0.75f) : new Color(0.45f, 0.45f, 0.45f);
            toolbar.style.flexShrink = 0;
            preview.style.display = assetPreviewExpanded ? DisplayStyle.Flex : DisplayStyle.None;
            assetScrollView.style.flexGrow = assetPreviewExpanded && !assetOnlyMode ? 0 : 1;
            UnityEngine.UIElements.Image buttonImage = new UnityEngine.UIElements.Image();
            buttonImage.image = assetPreviewExpanded ? CustomGUIContents.PreviewCollapse : CustomGUIContents.PreviewExpand;
            buttonImage.style.paddingBottom = assetPreviewExpanded ? 2 : 4;
            ToolbarButton expandButton = null;

            expandButton = new ToolbarButton(() =>
            {
                assetPreviewExpanded = !assetPreviewExpanded;
                preview.style.display = assetPreviewExpanded ? DisplayStyle.Flex : DisplayStyle.None;
                assetScrollView.style.flexGrow = assetPreviewExpanded && !assetOnlyMode ? 0 : 1;
                toolbar.tooltip = assetPreviewExpanded ? "Hide Asset Preview" : "Show Asset Preview";
                buttonImage.image = assetPreviewExpanded ? CustomGUIContents.PreviewCollapse : CustomGUIContents.PreviewExpand;
                buttonImage.style.paddingBottom = assetPreviewExpanded ? 2 : 4;
                assetScrollView.MarkDirtyRepaint();
            });

            expandButton.Add(buttonImage);
            expandButton.style.height = 12;
            expandButton.style.justifyContent = Justify.Center;
            expandButton.style.marginTop = 1;
            expandButton.style.marginBottom = 1;
            expandButton.style.marginRight = -2;
            expandButton.focusable = false;
            expandButton.style.flexGrow = 1;
            expandButton.text = "";
            toolbar.tooltip = assetPreviewExpanded ? "Hide Asset Preview" : "Show Asset Preview";
            expandButton.style.backgroundColor = EditorUtils.IsLightSkin() ? new Color(0.6f, 0.6f, 0.6f) : new Color(0.28f, 0.28f, 0.28f);
            toolbar.RegisterCallback<MouseEnterEvent>((evt) =>
            {
                expandButton.style.backgroundColor = EditorUtils.IsLightSkin() ? new Color(0.65f, 0.65f, 0.65f) : new Color(0.32f, 0.32f, 0.32f);
            });

            toolbar.RegisterCallback<MouseLeaveEvent>((evt) =>
                {
                    expandButton.style.backgroundColor = EditorUtils.IsLightSkin() ? new Color(0.6f, 0.6f, 0.6f) : new Color(0.28f, 0.28f, 0.28f);
                });
            toolbar.Add(expandButton);
            return toolbar;
        }

        bool IsHeaderAndPreview(VisualElement assetHeader, VisualElement importHeader, VisualElement preview)
        {
            if (preview == null || assetOnlyMode)
            {
                return false;
            }
            bool oneHeaderVisible = (assetHeader != null && assetHeader.style.display == DisplayStyle.Flex) || (importHeader != null && importHeader.style.display == DisplayStyle.Flex);
            return oneHeaderVisible;
        }

        private IMGUIContainer ControlScrollerBarVisibility(ScrollView scrollView)
        {
            if (scrollView == null)
            {
                return null;
            }
            IMGUIContainer scrollerController = new IMGUIContainer(() =>
            {
                scrollView.UpdateScrollbarVisibility(1);
            });
            return scrollerController;
        }

        void LimitAssetViewHeight(GeometryChangedEvent evt)
        {
            if (assetsCollapsed)
            {
                return;
            }
            maxAssetViewSize = position.height - 300;
            float maxDrag = maxAssetViewSize + 100;
            if (userAssetViewSize != -1 && userAssetViewSize <= maxDrag)
            {
                maxAssetViewSize = userAssetViewSize;
            }
            if (assetBox.contentRect.height > maxAssetViewSize || userAssetViewSize != -1)
            {
                assetBox.style.height = maxAssetViewSize;
            }
            bool allEmpty = true;
            foreach (var element in assetScrollView.Children())
            {
                if (element.contentRect.height > 4)
                {
                    allEmpty = false;
                    break;
                }
            }
            if (allEmpty)
            {
                assetScrollView.style.flexGrow = 0;
            }
        }
        void ManageAssetResize(MouseDownEvent evt)
        {
            if (!IsValidAssetTarget() || assetsCollapsed)
            {
                return;
            }
            leftHandleRect.y = 0;
            rightHandleRect.y = 0;
            if (GetActiveTab().hasPreview)
            {
                leftHandleRect.y += GetActiveTab().previewHeight;
                rightHandleRect.y += GetActiveTab().previewHeight;
            }
            if (!hideUpdateAlert && UpdateChecker.IsUpdateAvailable && position.width > 322)
            {
                leftHandleRect.width -= 55;
                leftHandleRect.x += 55;
            }
            bool handlePressed = leftHandleRect.Contains(evt.localMousePosition) || rightHandleRect.Contains(evt.localMousePosition);

            if (!resizingAssetView && evt.button == 0 && handlePressed)
            {
                resizingAssetView = true;
                EditorUtils.CreateCursorOverlay(this, MouseCursor.ResizeVertical);
                resizeOriginalCursorY = evt.mousePosition.y;
                startHeight = assetBox.contentRect.height;
                evt.StopPropagation();
            }
            else if (resizingAssetView)
            {
                evt.StopPropagation();
            }
        }

        void ManageAssetResize(MouseUpEvent evt)
        {
            ClearCursorOverlay();
            if (resizingAssetView)
            {
                if (!IsAPrefabTarget())
                {
                    userAssetViewSize = assetBox.style.height.value.value;
                }
                resizingAssetView = false;
            }
        }

        internal void SetScrollPosition(float scroll = 0)
        {
            if (IsActiveTabNew())
            {
                scroll = 0;
            }

            if (componentScrollView != null)
            {
                forceScroll = scroll;
                componentScrollView.scrollOffset = new Vector2(0, scroll);
                GetActiveTab().scrollPosition = scroll;
                gameObjectBox.RegisterOnce<GeometryChangedEvent>(ForceScroll);
            }
        }

        void ForceScroll(GeometryChangedEvent evt)
        {
            componentScrollView.scrollOffset = new Vector2(0, forceScroll);
            if (forceScroll > componentScrollView.verticalScroller.highValue || componentScrollView.scrollOffset.y != forceScroll)
            {
                gameObjectBox.ForceUpdate();
                gameObjectBox.RegisterOnce<GeometryChangedEvent>(InsistScroll);
                void InsistScroll(GeometryChangedEvent evt)
                {
                    componentScrollView.scrollOffset = new Vector2(0, forceScroll);
                    if (componentScrollView.scrollOffset.y != forceScroll)
                    {
                        gameObjectBox.RegisterOnce<GeometryChangedEvent>(ReInsistScroll);
                    }
                    void ReInsistScroll(GeometryChangedEvent evt)
                    {
                        if (componentScrollView.scrollOffset.y != forceScroll)
                        {
                            gameObjectBox.schedule.Execute(() => componentScrollView.scrollOffset = new Vector2(0, forceScroll)).ExecuteLater(0);
                        }
                        componentScrollView.scrollOffset = new Vector2(0, forceScroll);
                        componentScrollView.MarkDirtyRepaint();
                    }
                }
            }
            componentScrollView.scrollOffset = new Vector2(0, forceScroll);
            componentScrollView.MarkDirtyRepaint();
        }

        private void OnMouseEvent(EventBase evt)
        {

            HandleEvent(evt);
        }

        private void OnWheelEvent(WheelEvent evt)
        {
            HandleEvent(evt);
        }

        private void OnKeyEvent(EventBase evt)
        {
            HandleEvent(evt);
        }

        private void OnDragEvent(EventBase evt)
        {
            HandleEvent(evt);
        }
        private void HandleEvent(EventBase evt, bool translated = false)
        {
            Vector2 realMousePosition = Vector2.zero;
            int padding = 20;
            if (translated)
            {
                padding = 0;
            }
            if (evt is IMouseEvent mouseEvent)
            {
                clickMousePosition.x = mouseEvent.mousePosition.x;
                realMousePosition = new Vector2(mouseEvent.mousePosition.x, mouseEvent.mousePosition.y - padding);
            }
            switch (evt)
            {
                case MouseDownEvent mouseDownEvent:
                    HandleMouseDownEvent(mouseDownEvent, realMousePosition);
                    break;

                case MouseUpEvent mouseUpEvent:
                    HandleMouseUpEvent(mouseUpEvent, realMousePosition);
                    break;

                case MouseMoveEvent mouseMoveEvent:
                    HandleMouseMoveEvent(mouseMoveEvent, realMousePosition);
                    break;

                case WheelEvent wheelEvent:
                    HandleWheelEvent(wheelEvent);
                    break;

                case KeyDownEvent keyDownEvent:
                    HandleKeyDownEvent(keyDownEvent);
                    break;

                case DragUpdatedEvent dragUpdatedEvent:
                    HandleDragUpdatedEvent(dragUpdatedEvent, realMousePosition);
                    break;

                case DragPerformEvent dragExitedEvent:
                    HandleDragExitedEvent(dragExitedEvent, realMousePosition);
                    break;

                default:
                    break;
            }
        }

        private void HandleMouseDownEvent(MouseDownEvent evt, Vector2 realMousePosition)
        {
            if (middleScrollMode != 0 && evt.button == 2)
            {
                middleDragScrollView = EditorUtils.GetValidScrollView(this, evt.mousePosition);
                if (middleDragScrollView != null)
                {
                    VisualElement acutalTarget = evt.target as VisualElement;
                    acutalTarget.RegisterOnce<MouseUpEvent>(evt =>
                    {
                        if (movedMinumim)
                        {
                            EndMiddleScrolling();
                            GUIUtility.hotControl = 0;
                            evt.StopPropagation();
                            return;
                        }
                        EndMiddleScrolling();
                    });

                    originalMiddleScrollPosition = evt.mousePosition;
                    currentMiddleScrollPosition = originalMiddleScrollPosition;
                    middleScrolling = true;

                    if (middleScrollMode == 1)
                    {
                        UnityEngine.Cursor.SetCursor(CustomGUIContents.CursorScroll, new Vector2(16, 16), CursorMode.Auto);
                        EditorUtils.CreateCursorOverlay(this, MouseCursor.CustomCursor);
                        if (cursorOverlay != null)
                        {
                            cursorOverlay.AddToClassList("middleScroller");
                        }
                    }
                    else
                    {
                        EditorUtils.CreateCursorOverlay(this, MouseCursor.Pan);

                    }
                    //evt.StopPropagation();
                    return;
                }
            }
            else
            {
                EndMiddleScrolling();
            }
            bool isSearchFocused = EditorWindow.focusedWindow == this && GUI.GetNameOfFocusedControl() == "SearchField";
            bool isHeaderRenamed = EditorWindow.focusedWindow == this && editingHeader && GUIUtility.keyboardControl == editingHeaderID;
            if ((isSearchFocused || isHeaderRenamed) && gameObjectBox != null)
            {
                if (evt.mousePosition.y > gameObjectBox.worldBound.y)
                {
                    GUI.FocusControl(null);
                    Repaint();
                }
            }

            FloatingTab.fallingTab = false;

            if (evt.button == 0 && dragRect.Contains(realMousePosition))
            {
                waitingToDrag = true;
                mousePositionOnClick = realMousePosition;
            }
            else
            {
                waitingToDrag = false;
            }
            PopUpTip.Hide();
        }

        void EndMiddleScrolling()
        {
            middleDragScrollView = null;
            originalMiddleScrollPosition = Vector2.zero;
            currentMiddleScrollPosition = Vector2.zero;
            middleScrolling = false;
            movedMinumim = false;
            ClearCursorOverlay();
        }

        void HandlePendingComponentDrag(bool copyMode = false)
        {
            if (!triggerDrag && pendingOperation != null)
            {
                if (ignoreNextDragEvent)
                {
                    ignoreNextDragEvent = false;
                }
                if (!canProceedWithDrag && !pendingOperation.prefabError && !pendingOperation.errored)
                {
                    pendingOperation = null;
                    return;
                }

                bool hoveringComponent = false;
                bool movingSame = false;
                if (!pendingOperation.errored && !pendingOperation.prefabError)
                {
                    movingSame = (pendingOperation.targetIndex == pendingOperation.sourceIndex || pendingOperation.targetIndex + 1 == pendingOperation.sourceIndex) && pendingOperation.sourceTabIndex == activeIndex;
                }
                if ((!pendingOperation.isCopy && movingSame) || Event.current.keyCode == KeyCode.Escape)
                {
                    pendingOperation = null;
                }

                if (componentScrollView != null && pendingOperation != null && !pendingOperation.consumed)
                {
                    var targetComponent = componentScrollView.GetChild("Component" + pendingOperation.targetIndex);
                    if (targetComponent != null)
                    {
                        ComponentMap map = GetActiveTab().GetFoldoutMapForComponent(pendingOperation.targetIndex);
                        bool isCollapsed = false;
                        if (map.foldout == false || (map.hidden && !ActiveTabInDebugMode()))
                        {
                            isCollapsed = true;
                        }
                        targetComponent = targetComponent.parent;
                        int padding = isCollapsed ? 30 : 29;
                        Rect influenceRect = new Rect(targetComponent.worldBound);
                        influenceRect.y += influenceRect.height - padding;
                        influenceRect.height = 17;
                        Vector2 mouse = Event.current.mousePosition;
                        if (copyMode)
                        {
                            mouse.y -= 20;
                        }
                        hoveringComponent = influenceRect.Contains(mouse);

                    }
                }
                if (pendingOperation != null && !pendingOperation.consumed && !IsActiveTabNew() && !IsActiveTabValidMulti() && hoveringComponent /* && pendingOperation.mouseOverRect.Contains(realMousePosition)*/)
                {
                    triggerDrag = true;
                    TriggerComponentDrag();
                    RepaintForAWhile();
                }
                else if (pendingOperation != null)
                {

                    pendingComponentDrag = false;
                    pendingOperation = null;
                }
            }
        }

        private void HandleMouseUpEvent(MouseUpEvent evt, Vector2 realMousePosition)
        {
            if (middleScrolling)
            {
                EndMiddleScrolling();
            }
            if (resizingAssetView)
            {
                EndAssetViewResize();
            }
            if (Application.platform != RuntimePlatform.OSXEditor)
            {
                HandlePendingComponentDrag();
                canProceedWithDrag = false;
            }
            enteredSafeZone = false;
            mousePosition = Vector2.zero;
            bool endingAssetDrag = mousePositionOnAssetBarClick != Vector2.zero;
            if (dragging || endingAssetDrag)
            {
                waitingToDrag = false;
                if (!endingAssetDrag)
                {
                    EndDrag();
                }
                EndAssetViewResize();
                dragging = false;
                GOdragging = false;
                GUIUtility.hotControl = 0;
                DragAndDrop.PrepareStartDrag();
            }
            mousePositionOnAssetBarClick = Vector2.zero;
            waitingToDrag = false;
            DragAndDrop.objectReferences = new UnityEngine.Object[0];
            rootVisualElement.MarkDirtyRepaint();
        }
        private void HandleDragExitedEvent(DragPerformEvent evt, Vector2 realMousePosition)
        {
            if (Application.platform == RuntimePlatform.OSXEditor)
            {
                canProceedWithDrag = true;
                HandlePendingComponentDrag(true);
                canProceedWithDrag = false;
                return;

            }
            enteredSafeZone = false;
            if (pendingOperation != null && !pendingOperation.consumed)
            {
                canProceedWithDrag = true;
                RepaintForAWhile();

                if (pendingOperation.isCopy)
                {
                    RunNextFrame(() =>
                    {
                        HandleEvent(MouseUpEvent.GetPooled(Event.current));
                        RepaintForAWhile();
                    }, 50);
                }
            }
            if (dragging)
            {
                waitingToDrag = false;
                EndDrag();
                dragging = false;
                GOdragging = false;
                EndAssetViewResize();
            }
            waitingToDrag = false;
            DragAndDrop.objectReferences = new UnityEngine.Object[0];
            mousePosition = Event.current.mousePosition;
            Repaint();
        }

        private void HandleMouseMoveEvent(MouseMoveEvent evt, Vector2 realMousePosition)
        {
            if (middleScrolling)
            {
                PopUpTip.Hide();
                evt.StopImmediatePropagation();
                /*
                if (middleScrollMode == 1)
                {
                    currentMiddleScrollPosition = evt.mousePosition;
                }
                */

                return;
            }

            mousePosition = realMousePosition + scrollPosition;
            if (resizingAssetView && !assetsCollapsed && evt.button == 0)
            {
                PopUpTip.Hide();
                float delta = realMousePosition.y + 20 - resizeOriginalCursorY;
                float newHeight = startHeight - delta;
                if (newHeight < 1)
                {
                    newHeight = 1;
                }
                assetBox.style.height = newHeight;
                float maxHeight = position.height - 200;

                if (newHeight > maxHeight)
                {
                    assetBox.style.height = maxHeight;
                }

                assetBox.style.maxHeight = maxHeight;
                evt.StopImmediatePropagation();
                return;
            }
            if (waitingToDrag && dragRect != null && dragRect.Contains(realMousePosition) && dragIndex != -1 && tabs.Count > 1)
            {
                if ((evt.pressedButtons & 1) != 0)
                {
                    PopUpTip.Hide();

                    if (Vector2.Distance(mousePositionOnClick, realMousePosition) > 5 && dragIndex < tabs.Count)
                    {
                        FloatingTab.tabDragPoint = mousePositionOnClick.x - dragRect.position.x;
                        dragging = true;
                        FloatingTab.tabRect = Rect.zero;
                        dragTargetIndex = dragIndex;

                        if (activeIndex != dragIndex)
                        {
                            PopUpTip.Hide();
                        }
                    }
                }
            }

            if (scrollRect != null && scrollRect.width > 1)
            {
                Rect _scrollRect = new Rect(scrollRect) { height = toolScrollBarVisible && showScrollBar ? 42 : 22 };

                if (_scrollRect.Contains(realMousePosition))
                {
                    float mouseY = realMousePosition.y;
                    if (mouseY <= 22 && mouseY >= 0)
                    {
                        PopUpTip.show = true;
                    }
                    else
                    {
                        PopUpTip.Hide();
                    }
                }
                else
                {
                    pendingTabSwitch = -1;
                    PopUpTip.Hide();
                }
            }
        }
        private void HandleWheelEvent(WheelEvent evt)
        {
            if (scrollRect != null && scrollRect.width > 1)
            {
                Rect _scrollRect = new Rect(scrollRect) { height = toolScrollBarVisible && showScrollBar ? 60 : 40 };
                _scrollRect.y = 0;

                if (_scrollRect.Contains(evt.mousePosition))
                {
                    int multiplier = evt.delta.x > 0 ? scrollSpeedX * scrollDirectionX : scrollSpeedY * scrollDirectionY;
                    toolbarScrollPosition.x += (evt.delta.y > 0 || evt.delta.x > 0) ? 10 * multiplier : -10 * multiplier;
                    LimitScrollBar();
                    PopUpTip.Hide();
                    evt.StopPropagation();
                }
            }
        }
        private void HandleKeyDownEvent(KeyDownEvent evt)
        {
            if (evt.keyCode == KeyCode.Escape)
            {
                PopUpTip.Hide();
                dragging = false;
                waitingToDrag = false;
                dragIndex = -1;
                dragTargetIndex = -1;
                pendingOperation = null;
            }
        }
        private void HandleDragUpdatedEvent(DragUpdatedEvent evt, Vector2 realMousePosition)
        {

            if (dragging)
            {
                if (GOdragging)
                {
                    if (realMousePosition.y < 30)
                    {
                        GOdragging = false;
                        DragAndDrop.objectReferences = null;
                        DragAndDrop.AcceptDrag();
                        DragAndDrop.visualMode = DragAndDropVisualMode.Generic;
                        Repaint();
                    }
                }
                else if (!GOdragging && tabs[dragIndex].target != null)
                {
                    if (realMousePosition.y > 30)
                    {
                        GOdragging = true;
                        FloatingTab.Reset(this);
                        DragAndDrop.objectReferences = new UnityEngine.Object[] { tabs[dragIndex].target };
                        evt.StopPropagation();
                    }
                }
            }
            if (pendingOperation != null)
            {
                mousePosition = realMousePosition + new Vector2(0, scrollPosition.y);
            }
            if (realMousePosition.y > 30)
            {
                PopUpTip.Hide();
            }
            ScrollDrag(realMousePosition);
        }

        void ScrollDrag(Vector2 realMousePosition)
        {
            realMousePosition.y += 20;
            Rect viewportRect = componentScrollView.contentViewport.worldBound;
            if (!viewportRect.Contains(realMousePosition))
            {
                return;
            }
            float scrollZoneHeight = 50f;
            float baseScrollSpeed = 40f;
            Vector2 scrollOffset = componentScrollView.scrollOffset;
            float contentHeight = componentScrollView.contentContainer.layout.height;
            float viewportHeight = componentScrollView.contentViewport.layout.height;
            float maxScroll = contentHeight - viewportHeight;
            maxScroll = Mathf.Max(0, maxScroll);
            if (viewportHeight <= scrollZoneHeight * 2 + 20)
            {
                scrollZoneHeight = viewportHeight / 3;
            }
            Rect safeZone = new Rect(
                viewportRect.x,
                viewportRect.y + scrollZoneHeight,
                viewportRect.width,
                viewportRect.height - (scrollZoneHeight * 2)
            );

            if (!enteredSafeZone)
            {
                enteredSafeZone = safeZone.Contains(realMousePosition);
                if (!enteredSafeZone)
                {
                    return;
                }
            }
            if (maxScroll > 0)
            {
                if (realMousePosition.y <= viewportRect.yMin + scrollZoneHeight && scrollOffset.y > 0)
                {
                    float distanceToTop = realMousePosition.y - viewportRect.yMin;
                    float scrollSpeed = baseScrollSpeed * (1.0f - Mathf.Clamp01(distanceToTop / scrollZoneHeight));
                    scrollOffset.y -= scrollSpeed;
                    scrollOffset.y = Mathf.Max(scrollOffset.y, 0);
                }
                else if (realMousePosition.y >= viewportRect.yMax - scrollZoneHeight && scrollOffset.y < maxScroll)
                {
                    float distanceToBottom = viewportRect.yMax - realMousePosition.y;
                    float scrollSpeed = baseScrollSpeed * (1.0f - Mathf.Clamp01(distanceToBottom / scrollZoneHeight));
                    scrollOffset.y += scrollSpeed;
                    scrollOffset.y = Mathf.Min(scrollOffset.y, maxScroll);
                }
                componentScrollView.scrollOffset = scrollOffset;
                rootVisualElement.MarkDirtyRepaint();
            }
        }

        void OnFocus()
        {
            if (Event.current != null)
            {
                if (Event.current.type == EventType.MouseDown && Event.current.mousePosition.y <= 18)
                {
                    var previousScroll = GetActiveTab() != null ? GetActiveTab().scrollPosition : 0;
                    _CreateGUI(previousScroll);
                }
            }
            if (IsActiveTabValid())
            {
                Repaint();
            }
        }
        private void OnLostFocus()
        {
            PopUpTip.Hide();
        }

        internal void SetAllComponentsTo(bool expanded, Component componentToExclude = null, bool prefabMode = false)
        {
            if (EditorWindow.focusedWindow != null && (EditorWindow.focusedWindow.GetType().Name == "InspectorWindow" || EditorWindow.focusedWindow.GetType().Name == "PropertyEditor"))
            {
                ActiveEditorTracker tracker = Reflected.GetInspectorTracker(EditorWindow.focusedWindow);
                if (tracker != null && tracker.activeEditors != null && tracker.activeEditors.Length > 0)
                {
                    foreach (Editor editor in tracker.activeEditors)
                    {
                        if (editor == null)
                        {
                            continue;
                        }
                        if (editor.target == componentToExclude)
                        {
                            UnityEditorInternal.InternalEditorUtility.SetIsInspectorExpanded(editor.target, !expanded);
                            continue;
                        }
                        if (editor.target is Component)
                        {
                            UnityEditorInternal.InternalEditorUtility.SetIsInspectorExpanded(editor.target, expanded);
                        }
                    }
                    tracker.ForceRebuild();
                }
                return;
            }
            lastOpen = Time.realtimeSinceStartup;
            if (!prefabMode && GetActiveTab().componentMaps != null && GetActiveTab().componentMaps.Count > 0)
            {
                foreach (ComponentMap map in GetActiveTab().componentMaps)
                {
                    if (map.component == null)
                    {
                        continue;
                    }
                    if (map.component != componentToExclude)
                    {
                        map.foldout = expanded;
                    }
                    else if (componentToExclude != null)
                    {
                        map.foldout = !expanded;
                        map.awaitingScroll = true;
                        map.focusAfter = 1;
                    }
                }
                ReinitializeComponentEditors();
            }
            else if (prefabMode && PrefabComponentMapManager != null && PrefabComponentMapManager.componentMaps.Count > 0)
            {
                foreach (ComponentMap map in PrefabComponentMapManager.componentMaps)
                {
                    if (map.component == null)
                    {
                        continue;
                    }
                    if (map.component != componentToExclude)
                    {
                        map.foldout = expanded;
                    }
                    else if (componentToExclude != null)
                    {
                        map.foldout = !expanded;
                        map.awaitingScroll = true;
                    }
                }
                ReinitializePrefabComponentEditors();
            }
        }
        private void CloseCoInspector()
        {
            CloseCoInspector(SceneManager.GetActiveScene(), true);
        }
        internal void RepaintForAWhile()
        {
            Repaint();
            RepaintFor(0.1f);
        }
        internal void RepaintFor(float time)
        {
            if (!isRepainting)
            {
                isRepainting = true;
                dirtyDuration = time;
                startTime = Time.realtimeSinceStartup;
            }
        }
        private void KeepRepainting()
        {
            if (isRepainting && Time.realtimeSinceStartup < startTime + dirtyDuration)
            {
                GUI.changed = true;
                Repaint();
                return;
            }
            if (isRepainting)
            {
                startTime = 0;
                dirtyDuration = 0;
            }
            isRepainting = false;
        }
        private void CloseCoInspector(Scene scene)
        {
            if (!EnteringPlaymode)
            {
                scenesChanged = true;
                changingScenes = true;
            }
            CloseCoInspector(scene, true);
        }
        private void CloseCoInspector(Scene scene, bool closing)
        {
            if (closing || !closing && !exitingPlayMode)
            {
                DoClose();
            }
        }

        private void DoClose()
        {
            activeScene = SceneInfo.FromActiveScene(activeScene);
            if (!Application.isPlaying)
            {
                UpdateAllTabPaths();
            }
            CloseScene();
            CleanAllEditors();
            instances.Remove(this);
        }

        private void ReopenCoInspector(Scene scene, OpenSceneMode mode)
        {
            if (instances.Contains(this))
            {
                return;
            }
            if (!instances.Contains(this))
            {
                RegisterWindow(this);
                justOpened = true;
                OnEnable();
            }
        }
        private void ReopenCoInspector(Scene scene, LoadSceneMode mode)
        {
            if (instances.Contains(this))
            {
                return;
            }
            ReopenCoInspector(scene, OpenSceneMode.Single);
        }
        internal RectOffset PaddingIcon
        {
            get
            {
                if (_paddingIcon == null || _paddingIcon.left != 20)
                    _paddingIcon = new RectOffset(20, 0, 0, 0);
                return _paddingIcon;
            }
            set { _paddingIcon = value; }
        }
        internal RectOffset PaddingNoIcon
        {
            get
            {
                if (_paddingNoIcon == null || _paddingNoIcon.left != 8)
                    _paddingNoIcon = new RectOffset(8, 0, 0, 0);
                return _paddingNoIcon;
            }
            set { _paddingNoIcon = value; }
        }

        internal FloatingTab FloatingTab
        {
            get
            {
                if (floatingTab == null)
                {
                    floatingTab = new FloatingTab(this);
                }
                return floatingTab;
            }
        }
        bool OnBlankWorkspace()
        {
            return tabs.Count == 1 && tabs[0].newTab;
        }


        void ManageAssemblyReload()
        {
            if (!exitingPlayMode && !EnteringPlaymode)
            {
                SaveSettings();
                TrySaveSession();
            }
            CleanAllAssetEditors();
            CleanGameObjectEditors();
            if (sessionsMode == 2)
            {
                pendingRestore = true;
            }
            triggerDrag = false;
            triggeringADrag = false;
            isRepainting = false;
        }
        void ManageAfterAssemblyReload()
        {
            triggerDrag = false;
            triggeringADrag = false;
            isRepainting = false;
            if (sessionsMode == 2 && pendingRestore)
            {
                RestoreSession(true);
            }
            pendingRestore = false;
            UpdateAllWidths();
            ScrollToActiveTab();
            return;
        }
        private void OnActiveSceneChanged(Scene previousScene, Scene newScene)
        {
            DoRegisterSceneChange();
        }

        private void DoRegisterSceneChange()
        {
            if (exitingPlayMode)
            {
                return;
            }
            HandleSceneChanged();
        }

        void AddIfNecessary(Editor editor)
        {
            if (editor == null)
            {
                return;
            }
            if (sceneMethods == null)
            {
                sceneMethods = new Dictionary<Editor, MethodInfo>();
            }
            if (sceneMethods.ContainsKey(editor))
            {
                return;
            }
            MethodInfo method = editor.GetType().GetMethod("OnSceneGUI", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
            if (method != null)
            {
                sceneMethods.Add(editor, method);
            }
        }

        void _OnSceneGUI(SceneView sceneView)
        {
            if (IsActiveTabValidMulti())
            {
                EditorToolManager.DrawCustomHandles(GetActiveTab().targets);
            }
            else if (IsActiveTabValid())
            {
                EditorToolManager.DrawCustomHandles(GetActiveTab().target);
            }
            if (sceneMethods != null)
            {
                Dictionary<Editor, MethodInfo> toKeep = new Dictionary<Editor, MethodInfo>();
                foreach (Editor editor in sceneMethods.Keys)
                {
                    if (editor == null || editor.target == null)
                    {
                        continue;
                    }
                    MethodInfo method = sceneMethods[editor];
                    toKeep.Add(editor, method);
                    if (method != null)
                    {
                        method.Invoke(editor, null);
                    }
                }
                sceneMethods = toKeep;
            }
        }
#if UNITY_2021_2_OR_NEWER
        void OpenPrefabStage(UnityEditor.SceneManagement.PrefabStage prefabStage)
        {
            if (IsAPrefabTarget())
            {
                CloseAssetView();
            }
            if (onPrefabSceneMode)
            {
                ClosePrefabStage(null);
            }
            if (!onPrefabSceneMode)
            {
                onPrefabSceneMode = true;
                GameObject relativeTarget = EditorUtils.GetRelativeGameObjectInPrefabMode(prefabStage.openedFromInstanceObject);
                GameObject actualTarget = relativeTarget ? relativeTarget : prefabStage.prefabContentsRoot;

                if (relativeTarget)
                {
                    AddTabNext(actualTarget, true);
                    ignoreNextSelection = true;
                    EditorUtils.TargetGameObject(actualTarget);
                }
            }

        }
        void ClosePrefabStage(UnityEditor.SceneManagement.PrefabStage prefabStage)
        {
            //onPrefabSceneMode = SceneIsInPrefabMode();
            onPrefabSceneMode = false;
            ignoreNextSelection = true;
            FixActiveIndex();
            Repaint();
        }
#else
        void OpenPrefabStage(UnityEditor.Experimental.SceneManagement.PrefabStage prefabStage)
        {
            onPrefabSceneMode = true;
            ignoreNextSelection = true;
            AddTabNext(prefabStage.prefabContentsRoot, true);
        }
        void ClosePrefabStage(UnityEditor.Experimental.SceneManagement.PrefabStage prefabStage)
        {
            //onPrefabSceneMode = SceneIsInPrefabMode();
            onPrefabSceneMode = false;
            FixActiveIndex();
            Repaint();
        }
#endif       
        void HookEvents()
        {
#if UNITY_2021_2_OR_NEWER
            UnityEditor.SceneManagement.PrefabStage.prefabStageOpened += OpenPrefabStage;
            UnityEditor.SceneManagement.PrefabStage.prefabStageClosing += ClosePrefabStage;
#else
            UnityEditor.Experimental.SceneManagement.PrefabStage.prefabStageOpened += OpenPrefabStage;
            UnityEditor.Experimental.SceneManagement.PrefabStage.prefabStageClosing += ClosePrefabStage;
#endif
#if UNITY_6000_4_OR_NEWER
            EditorApplication.projectWindowItemByEntityIdOnGUI += HandleAssetClickEntity;
#elif UNITY_2022_1_OR_NEWER
            EditorApplication.projectWindowItemInstanceOnGUI += HandleAssetClick;
#else
    EditorApplication.projectWindowItemOnGUI += HandleAssetClick;
#endif
            //EditorApplication.projectChanged += OnProjectChanged;
            AssetTracker.OnAssetChangesDetected += OnProjectChanged;
            EditorApplication.hierarchyChanged += KeepTrackOfComponentStructure;
            EditorApplication.hierarchyChanged += RefreshAllTabNames;
            EditorApplication.hierarchyChanged += RefreshTrackerPaths;
            SceneView.duringSceneGui += _OnSceneGUI;
            Selection.selectionChanged += HandleSelectionChange;
            EditorApplication.playModeStateChanged += HandlePlayModeStateChanged;
#if UNITY_6000_4_OR_NEWER
            EditorApplication.hierarchyWindowItemByEntityIdOnGUI += HandleMiddleClickEntity;
#else
            EditorApplication.hierarchyWindowItemOnGUI += HandleMiddleClick;
#endif
            EditorApplication.update += BackUpdate;
            EditorApplication.quitting += TrySaveSession;
            Undo.undoRedoPerformed += UndoRedoPerformed;
            AssemblyReloadEvents.afterAssemblyReload += ManageAfterAssemblyReload;
            AssemblyReloadEvents.beforeAssemblyReload += ManageAssemblyReload;
            EditorSceneManager.sceneSaving += OnSceneSaving;
            // EditorSceneManager.sceneClosing += CloseCoInspector;
            EditorSceneManager.activeSceneChangedInEditMode += OnActiveSceneChanged;
            SceneManager.activeSceneChanged += OnActiveSceneChanged;
            onPrefabSceneMode = SceneIsInPrefabMode();
            // SceneManager.sceneUnloaded += CloseCoInspector;
        }
        void UnhookEvents()
        {
#if UNITY_2021_2_OR_NEWER
            UnityEditor.SceneManagement.PrefabStage.prefabStageOpened -= OpenPrefabStage;
            UnityEditor.SceneManagement.PrefabStage.prefabStageClosing -= ClosePrefabStage;
#else
            UnityEditor.Experimental.SceneManagement.PrefabStage.prefabStageOpened -= OpenPrefabStage;
            UnityEditor.Experimental.SceneManagement.PrefabStage.prefabStageClosing -= ClosePrefabStage;
#endif
#if UNITY_6000_4_OR_NEWER
            EditorApplication.projectWindowItemByEntityIdOnGUI -= HandleAssetClickEntity;
#elif UNITY_2022_1_OR_NEWER
            EditorApplication.projectWindowItemInstanceOnGUI -= HandleAssetClick;
#else
    EditorApplication.projectWindowItemOnGUI -= HandleAssetClick;
#endif
            //EditorApplication.projectChanged -= OnProjectChanged;
            AssetTracker.OnAssetChangesDetected -= OnProjectChanged;
            EditorApplication.hierarchyChanged -= KeepTrackOfComponentStructure;
            EditorApplication.hierarchyChanged -= RefreshAllTabNames;
            EditorApplication.hierarchyChanged -= RefreshTrackerPaths;
            SceneView.duringSceneGui -= _OnSceneGUI;
            Selection.selectionChanged -= HandleSelectionChange;
            EditorApplication.playModeStateChanged -= HandlePlayModeStateChanged;
#if UNITY_6000_4_OR_NEWER
            EditorApplication.hierarchyWindowItemByEntityIdOnGUI -= HandleMiddleClickEntity;
#else
            EditorApplication.hierarchyWindowItemOnGUI -= HandleMiddleClick;
#endif
            EditorApplication.update -= BackUpdate;
            EditorApplication.quitting -= TrySaveSession;
            Undo.undoRedoPerformed -= UndoRedoPerformed;
            AssemblyReloadEvents.afterAssemblyReload -= ManageAfterAssemblyReload;
            AssemblyReloadEvents.beforeAssemblyReload -= ManageAssemblyReload;
            EditorSceneManager.sceneSaving -= OnSceneSaving;
            EditorSceneManager.activeSceneChangedInEditMode -= OnActiveSceneChanged;
            SceneManager.activeSceneChanged -= OnActiveSceneChanged;
        }
        private static CoInspectorWindow mainCoInspector;
        public static CoInspectorWindow MainCoInspector
        {
            get
            {
                if (mainCoInspector == null && instances != null && instances.Count > 0)
                {
                    mainCoInspector = instances[0];
                }
                return mainCoInspector;
            }
        }
        internal static UserSaveData FindSettingsObject()
        {
            if (MainCoInspector)
            {
                if (MainCoInspector.settingsData && AssetDatabase.GetAssetPath(MainCoInspector.settingsData) != "")
                {
                    return MainCoInspector.settingsData;
                }
            }
            string path = _GetRootPath() + "/Settings/UserData.asset";
            UnityObject _settingsData = AssetDatabase.LoadAssetAtPath<UserSaveData>(path);
            if (_settingsData)
            {
                return _settingsData as UserSaveData;
            }
            UserSaveData settingsData = AssetDatabase.LoadAssetAtPath<UserSaveData>(path);
            if (settingsData)
            {
                return settingsData;
            }
            return null;
        }
        void FixReferences()
        {
            if (settingsData == null)
            {
                settingsData = FindSettingsObject();
            }
            if (settingsData == null)
            {
                settingsData = AutoCreateSettings();
            }
        }
        private void OnEnable()
        {
            FixReferences();
            CleanTabs();
            if (instances == null)
            {
                instances = new List<CoInspectorWindow>();
            }
            RegisterWindow(this);

            titleContent = new GUIContent("CoInspector", CustomGUIContents.MainIconImage);
            /*macOS 'UpdateScene every frame' makes it impossible to keep for a good performance.*/
            if (!(Application.platform is RuntimePlatform.OSXEditor))
            {
                // autoRepaintOnSceneChange = true;
            }
            autoRepaintOnSceneChange = false;
            wantsMouseEnterLeaveWindow = true;
            isRepainting = false;
            UnhookEvents();
            HookEvents();
            RestoreLastAssets();
            textPluginPresent = IsAnyNamespacePresent(namespaces);
            odinInspectorPresent = IsOdinInspectorPresent();
            ueePresent = IsNamespacePresent("InfinityCode.UltimateEditorEnhancer");
            lastTabClick = -1;
            lastClickedTab = -1;
            if (settingsData)
            {
                if (TryLoadSession() || ((inActualPlayMode || exitingPlayMode) && lastSessionData != null))
                {
                    if ((!justOpened && !lastSessionData.checkingToLoad) || sessionsMode == 1 || (inActualPlayMode && !lastSessionData.checkingToLoad && sessionsMode == 1) || (pendingRestore && !lastSessionData.checkingToLoad) || exitingPlayMode && !lastSessionData.checkingToLoad)
                    {
                        if (pendingRestore)
                        {
                            pendingRestore = false;
                        }
                        if (!StartedPlayModeInOtherScene() || sessionsMode == 1)
                        {
                            RestoreSession(true);
                        }
                        else
                        {
                            HandleSceneChanged(false);
                        }
                    }
                    else
                    {
                        if (lastSessionData != null && lastSessionData.checkingToLoad)
                        {
                            justOpened = true;
                            lastSessionData.checkingToLoad = false;
                        }
                        CleanTabs();
                    }
                }
                else if (justOpened)
                {
                    UpdateCurrentTip();
                    CleanTabs();
                }
            }
            if (tabs == null)
            {
                tabs = new List<TabInfo>();
            }
            if (tabs.Count == 0)
            {
                tabs.Add(new TabInfo(GetActiveTab().target, 0, this));
                FocusTab(0);
            }
            UpdateAllWidths();
            if (exitingPlayMode)
            {
                exitingPlayMode = false;
                activeScene = SceneInfo.FromActiveScene(activeScene);
            }
            DrawCurrentAssets();
        }

        private bool StartedPlayModeInOtherScene()
        {
            if (inActualPlayMode && !exitingPlayMode && settingsData != null)
            {
                if (settingsData.playModeStartScene != default(Scene) && settingsData.playModeStartScene != SceneManager.GetActiveScene())
                {
                    return true;
                }
            }
            if (settingsData != null)
            {
                settingsData.playModeStartScene = default(Scene);
            }
            return false;
        }

        private void UndoRedoPerformed()
        {
            RefreshAllTabNames();
            RefreshAllIcons();
            CleanAllTabMaps();
            EditorToolManager.RefreshHandlePosition();
            EditorToolManager.RefreshCache();

        }
        void CleanGameObjectEditors()
        {
            CleanAllTabMaps();
            DestroyAllIfNotNull(materialEditors);
            materialEditors = null;
            DestroyAllIfNotNull(componentEditors);
            componentEditors = null;
            DestroyIfNotNull(gameObjectEditor);
            gameObjectEditor = null;
        }
        void CleanAllEditors()
        {
            CleanAllTabMaps();
            DestroyAllIfNotNull(componentEditors);
            componentEditors = null;
            DestroyAllIfNotNull(prefabEditors);
            prefabEditors = null;
            DestroyAllIfNotNull(materialEditors);
            materialEditors = null;
            DestroyAllIfNotNull(prefabMaterialEditors);
            prefabMaterialEditors = null;
            DestroyIfNotNull(assetEditor);
            assetEditor = null;
            DestroyIfNotNull(assetImportSettingsEditor);
            assetImportSettingsEditor = null;
            if (assetImporters != null)
            {
                assetImporters = null;
            }
            if (assetImporter != null)
            {
                assetImporter = null;
            }
            DestroyIfNotNull(gameObjectEditor);
            gameObjectEditor = null;
        }
        void ReadLastClicked()
        {
            if (tracker != null)
            {
                UpdateLastMostContents();
                mostClicked = tracker.GetMostClicked();
                lastClicked = tracker.GetRecentlyClicked();
            }
        }
        void UpdateLastMostContents()
        {
            RequestAction(DoUpdateLastMostContents);
        }
        void DoUpdateLastMostContents()
        {
            if (tracker != null)
            {
                tracker.UpdateContents();
            }
        }
        internal void UpdateClicked(GameObject gameObject)
        {
            if (gameObject == null)
            {
                return;
            }
            if (tracker == null)
            {
                tracker = new GameObjectTracker();
            }
            tracker.UpdateClicked(gameObject);
            UpdateLastMostContents();
        }

        internal void UpdateClicked(GameObject[] gameObjects)
        {
            if (gameObjects == null || gameObjects.Length == 0)
            {
                return;
            }
            if (tracker == null)
            {
                tracker = new GameObjectTracker();
            }
            tracker.UpdateClicked(gameObjects);
            UpdateLastMostContents();
        }

        internal void CleanTabs()
        {
            tabs = new List<TabInfo>();
            closedTabs = new List<TabInfo>();
            activeIndex = -1;
            CleanGameObjectEditors();
            AddTabNext();
        }
        void CloseScene()
        {
            if (EnteringPlaymode)
            {
                return;
            }
            if (settingsData && !InRecoverScreen())
            {
                settingsData.SaveData(false, this, true);
                CleanTabs();
            }
            else if (InRecoverScreen() && lastSessionData != null)
            {
                lastSessionData.checkingToLoad = true;
                lastSessionData.SaveAssets(this);
            }
            CleanAllEditors();
        }

        private void OnDisable()
        {
            if (!settingsData)
            {
                settingsData = AutoCreateSettings();
            }
            UnhookEvents();
            if (settingsData && !InRecoverScreen())
            {
                if (!inActualPlayMode)
                {
                    settingsData.SaveData(this);
                }
                else
                {
                    settingsData.SaveData(false, this);
                }
            }
            else if (InRecoverScreen())
            {
                lastSessionData.checkingToLoad = true;
            }
            EditorApplication.delayCall += CleanAllEditors;
        }

        internal void RunNextFrame(Action action, int framesToWait = 1)
        {
            if (action == null)
            {
                return;
            }
            if (methodsToRun == null)
            {
                methodsToRun = new List<DelayedAction>();
            }
            if (methodsToRun.Exists(a => a.Action == action))
            {
                return;
            }
            methodsToRun.Add(new DelayedAction(action, framesToWait));
            Repaint();
        }



        internal void RunDelayedMethods()
        {
            if (methodsToRun == null || methodsToRun.Count == 0)
            {
                return;
            }
            for (int i = methodsToRun.Count - 1; i >= 0; i--)
            {
                DelayedAction delayedAction = methodsToRun[i];

                if (delayedAction.Delay <= 0)
                {
                    delayedAction.Action?.Invoke();
                    methodsToRun.RemoveAt(i);
                }
                else
                {
                    delayedAction.Delay--;
                }
            }
            if (methodsToRun.Count > 0)
            {
                Repaint();
            }
        }

        internal static void CallVeryLate(Action action, int numNestedDelays = 3)
        {
            DoCallVeryLate(action, numNestedDelays);
        }
        static void DoCallVeryLate(Action action, int numNestedDelays, int currentDelay = 0)
        {
            if (currentDelay < numNestedDelays)
            {
                currentDelay++;
                EditorApplication.delayCall += () =>
                {
                    DoCallVeryLate(action, numNestedDelays, currentDelay);
                };
            }
            else
            {
                action();
            }
        }
        void TriggerDelayedDrag()
        {
            rootVisualElement.schedule.Execute((Action)(() =>
            {
                HandleComponentDrag();
                this.ReinitializeComponentEditors();
                triggerDrag = false;
                triggeringADrag = false;
                Repaint();
            }));
            Repaint();
        }

        void CheckForMacChanges()
        {
            if (IsActiveTabValid())
            {
                var activeTab = GetActiveTab();
                if (!activeTab.newTab)
                {
                    if (EditorUtils.CheckEditorChanged(gameObjectEditor))
                    {
                        return;
                    }
                    if (EditorUtils.CheckEditorsChanged(componentEditors))
                    {
                        return;
                    }
                    if (EditorUtils.CheckEditorsChanged(materialEditors))
                    {
                        return;
                    }
                }
            }
            if (EditorUtils.CheckEditorChanged(assetEditor))
            {
                return;
            }
            if (EditorUtils.CheckEditorChanged(assetImportSettingsEditor))
            {
                return;
            }
            if (EditorUtils.CheckEditorsChanged(prefabEditors))
            {
                return;
            }
            if (EditorUtils.CheckEditorsChanged(prefabMaterialEditors))
            {
                return;
            }
        }

        internal void UpdateMiddleScroll()
        {
            if (middleScrolling)
            {
                PopUpTip.Hide();
                if (middleScrollMode != 0 && middleDragScrollView != null)
                {
                    Vector2 direction = originalMiddleScrollPosition - currentMiddleScrollPosition;
                    float deadZone = 10;
                    float maxSpeed = 80f;
                    float maxDistance = 180;
                    if (middleScrollMode == 1)
                    {
                        float effectiveDistance = Mathf.Max(0, direction.magnitude - deadZone);
                        float t = Mathf.Clamp01(effectiveDistance / maxDistance);
                        float magnitude = maxSpeed * (t * t);

                        if (direction.y > deadZone)
                        {
                            if (!movedMinumim)
                            {
                                GUIUtility.hotControl = 0;
                                movedMinumim = true;
                            }
                            cursorOverlay.name = "CursorOverlay_ScrollUp";
                        }
                        else if (direction.y < -deadZone)
                        {
                            if (!movedMinumim)
                            {
                                GUIUtility.hotControl = 0;
                                movedMinumim = true;
                            }
                            cursorOverlay.name = "CursorOverlay_ScrollDown";
                        }
                        else
                        {
                            cursorOverlay.name = "CursorOverlay_Scroll";
                        }
                        middleDragScrollView.scrollOffset -= direction.normalized * magnitude;
                        middleDragScrollView.MarkDirtyRepaint();
                    }
                    else if (middleScrollMode == 2)
                    {
                        float distance = Vector2.Distance(originalMiddleScrollPosition, currentMiddleScrollPosition);

                        if (!movedMinumim)
                        {
                            if (distance < 5)
                            {
                                return;
                            }
                            GUIUtility.hotControl = 0;
                            movedMinumim = true;
                        }
                        middleScrollDelta = currentMiddleScrollPosition - originalMiddleScrollPosition;
                        if (currentMiddleScrollPosition.y <= 10)
                        {
                            var globalMousePos = new Vector2(
                                this.position.x + currentMiddleScrollPosition.x,
                                this.position.y - 11 + this.position.height
                            );
                            EditorUtils.MoveCursor((int)globalMousePos.x, (int)globalMousePos.y);
                            currentMiddleScrollPosition.y = this.position.height - 11;
                            originalMiddleScrollPosition = currentMiddleScrollPosition;
                        }
                        else if (currentMiddleScrollPosition.y >= this.position.height - 10)
                        {
                            var globalMousePos = new Vector2(
                                this.position.x + currentMiddleScrollPosition.x,
                                this.position.y + 11
                            );
                            EditorUtils.MoveCursor((int)globalMousePos.x, (int)globalMousePos.y);
                            currentMiddleScrollPosition.y = 11;
                            originalMiddleScrollPosition = currentMiddleScrollPosition;
                        }
                        else
                        {
                            originalMiddleScrollPosition = currentMiddleScrollPosition;

                        }
                        if (distance < position.height / 2)
                        {
                            Vector2 newScroll = middleDragScrollView.scrollOffset -= middleScrollDelta;
                            if (newScroll.y < 0 || newScroll.x < 0)
                            {
                                return;
                            }
                            middleDragScrollView.scrollOffset = newScroll;
                            middleDragScrollView.MarkDirtyRepaint();
                        }
                    }
                }
            }
        }

        void BackUpdate()
        {
            if (Reflected.IsTimeControlPlaying() && assetBox != null)
            {
                assetBox.MarkDirtyRepaint();
            }
            //CheckActiveScrolls();
            //CheckBatchCalls();
            AutoCheckScene();
            /* if (Application.platform is RuntimePlatform.OSXEditor)
             {
                 CheckForMacChanges();
             } */
            if (DragAndDrop.objectReferences.Length > 0 && Event.current != null && Event.current.type == EventType.Repaint)
            {
                bool isAnyNull = false;
                foreach (var obj in DragAndDrop.objectReferences)
                {
                    if (obj == null)
                    {
                        isAnyNull = true;
                        break;
                    }
                }
                if (isAnyNull)
                {
                    pendingComponentDrag = false;
                    pendingOperation = null;
                    triggerDrag = false;
                    triggeringADrag = false;
                    DragAndDrop.objectReferences = null;
                    DragAndDrop.PrepareStartDrag();
                    Repaint();
                }
            }
            KeepRepainting();
        }

        void TriggerComponentDrag()
        {

            if (triggerDrag && !triggeringADrag)
            {
                triggeringADrag = true;
                TriggerDelayedDrag();
            }
            rootVisualElement.schedule.Execute(() =>
            {
                DragAndDrop.PrepareStartDrag();
            });
        }

        void RestoreCurrentWorkspace()
        {
            if (TryLoadSession())
            {
                RestoreSession();
            }
        }
        internal bool EmptySavedSession()
        {
            if (lastSessionData != null && lastSessionData.tabs != null && lastSessionData.tabs.Count == 1 && lastSessionData.tabs[0].newTab)
            {
                return true;
            }
            return false;
        }

        internal float GetPreviewHeight()
        {
            return IntGetPreviewHeight();
        }
        internal int IntGetPreviewHeight()
        {
            if (AssetTargetMode() == 1 && !ShouldDrawImportSettings())
            {
                if (targetObject is Texture || targetObject is Sprite || targetObject is Material)
                {
                    return assetOnlyMode ? 200 : 150;
                }
                if (PoolCache.IsAnImportedObject(targetObject) || targetObject is UnityEditor.Animations.BlendTree || targetObject is UnityEditor.Animations.AnimatorStateTransition)
                {
                    return assetOnlyMode ? 200 : 150;
                }
                if (targetObject is AnimationClip)
                {
                    return assetOnlyMode ? 200 : 200;
                }
                if (targetObject is VideoClip)
                {
                    return assetOnlyMode ? 300 : 250;
                }
            }
            else if (AssetTargetMode() == 2)
            {
                if (assetEditor?.GetType().ToString() == "UnityEditor.Graphs.AnimationStateMachine.AnimatorStateTransitionInspector")
                {
                    return ShouldDrawImportSettings() ? 100 : 150;
                }
                int baseValue = 100;
                int increment = (targetObjects.Length - 1) / 5 * 100;
                return Mathf.Clamp(baseValue + increment, 100, 300);
            }
            return assetOnlyMode ? 150 : 100;
        }

        internal bool IsAModelTarget()
        {
            int mode = AssetTargetMode();
            if (mode == 1)
            {
                return PoolCache.IsAnImportedObject(targetObject);
            }
            if (mode == 2)
            {
                foreach (var obj in targetObjects)
                {
                    if (!PoolCache.IsAnImportedObject(obj))
                    {
                        return false;
                    }
                }
                return true;
            }
            return false;
        }

        internal bool IsAPrefabTarget()
        {
            int mode = AssetTargetMode();
            if (mode == 0)
            {
                return false;
            }
            if (mode == 1)
            {
                return PoolCache.IsAPrefabAsset(targetObject);
            }
            if (mode == 2)
            {
                foreach (var obj in targetObjects)
                {
                    if (!PoolCache.IsAPrefabAsset(obj))
                    {
                        return false;
                    }
                }
                return true;
            }
            return false;
        }
        internal bool IsMissingAssetTarget()
        {
            return AssetTargetMode() == 1 && MissingScriptManager.IsActive() && targetObject is MissingScriptManager;
        }
        internal bool IsValidAssetTarget()
        {
            return AssetTargetMode() != 0;
        }
        internal int AssetTargetMode()
        {
            bool nullTargets = targetObjects == null || targetObjects.Length == 0;
            if (targetObject == null && nullTargets)
            {
                return 0;
            }
            if (targetObject == null && targetObjects.Length == 1)
            {
                targetObject = targetObjects[0];
            }
            if (targetObject != null)
            {
                targetObjects = null;
                return 1;
            }
            if (targetObjects != null && !nullTargets)
            {
                return 2;
            }
            return 0;
        }
        internal bool InRecoverScreen()
        {
            if (justOpened && tabs != null && tabs.Count == 1 && IsThereValidSession() && sessionsMode == 0)
            {
                if (EmptySavedSession())
                {
                    return false;
                }
                AdjustTabsToStartMessage();
                return true;
            }
            if (sessionsMode == 0 && lastSessionData != null && lastSessionData.checkingToLoad)
            {
                if (EmptySavedSession())
                {
                    return false;
                }
                return true;
            }
            return false;
        }
        void AdjustTabsToStartMessage()
        {
            if (tabs.Count != 1 || tabs.Count == 1 && !tabs[0].newTab)
            {
                CleanTabs();
            }
        }
        internal static UserSaveData _AutoCreateSettings()
        {
            try
            {
                string rootPath = _GetRootPath();
                if (string.IsNullOrEmpty(rootPath))
                {
                    Debug.LogError("CoInspector: Failed to get root path");
                    rootPath = "Assets/CoInspector";
                }

                string settingsFolder = Path.Combine(rootPath, "Settings");
                string assetPath = Path.Combine(settingsFolder, "UserData.asset");

                settingsFolder = settingsFolder.Replace('\\', '/');
                assetPath = assetPath.Replace('\\', '/');

                try
                {
                    if (!Directory.Exists(settingsFolder))
                    {
                        Directory.CreateDirectory(settingsFolder);
                        AssetDatabase.Refresh();
                        System.Threading.Thread.Sleep(100);
                    }
                }
                catch (Exception dirEx)
                {
                    Debug.LogError($"CoInspector: Failed to create settings directory: {dirEx.Message}");
                    settingsFolder = "Assets";
                    assetPath = "Assets/CoInspectorUserData.asset";
                }

                bool assetExists = File.Exists(assetPath);
                UserSaveData existingData = null;

                if (assetExists)
                {
                    existingData = AssetDatabase.LoadAssetAtPath<UserSaveData>(assetPath);
                    if (existingData != null)
                    {
                        if (MainCoInspector != null)
                        {
                            MainCoInspector.settingsData = existingData;
                        }
                        return existingData;
                    }

                    Debug.LogWarning($"CoInspector: Asset exists at {assetPath} but couldn't be loaded. Recreating.");
                    AssetDatabase.DeleteAsset(assetPath);
                    AssetDatabase.Refresh();
                }

                UserSaveData newSettingsData = ScriptableObject.CreateInstance<UserSaveData>();

                try
                {
                    AssetDatabase.CreateAsset(newSettingsData, assetPath);
                    EditorUtility.SetDirty(newSettingsData);
                    AssetDatabase.SaveAssets();
                    AssetDatabase.Refresh();

                    System.Threading.Thread.Sleep(200);

                    UserSaveData loadedData = AssetDatabase.LoadAssetAtPath<UserSaveData>(assetPath);

                    if (loadedData != null)
                    {
                        if (MainCoInspector != null)
                        {
                            MainCoInspector.settingsData = loadedData;
                        }
                        return loadedData;
                    }
                    else
                    {
                        Debug.LogError($"CoInspector: Failed to load newly created asset at {assetPath}");
                        return newSettingsData;
                    }
                }
                catch (Exception assetEx)
                {
                    Debug.LogError($"CoInspector: Error creating asset: {assetEx.Message}\n{assetEx.StackTrace}");
                    return newSettingsData;
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"CoInspector: Critical error in _AutoCreateSettings: {ex.Message}\n{ex.StackTrace}");
                return ScriptableObject.CreateInstance<UserSaveData>();
            }
        }
        internal UserSaveData AutoCreateSettings()
        {
            if (settingsData == null)
            {
                settingsData = FindSettingsObject();
                if (settingsData == null)
                {
                    rootPath = _GetRootPath();
                    if (rootPath != null)
                    {
                        rootPath += "/Settings";
                    }
                    if (!Directory.Exists(rootPath))
                    {
                        Directory.CreateDirectory(rootPath);
                    }
                    EditorUtility.DisplayDialog("Missing User Save Data", "CoInspector User Save Data file is missing. CoInspector can't work without it.\n\nCreating a new one at " + rootPath, "OK");

                    string assetPath = rootPath + "/UserData.asset";
                    UserSaveData _settingsData = ScriptableObject.CreateInstance<UserSaveData>();
                    if (File.Exists(assetPath))
                    {
                        AssetDatabase.DeleteAsset(assetPath);
                    }
                    AssetDatabase.CreateAsset(_settingsData, assetPath);
                    //AssetDatabase.SaveAssets();
                    AssetDatabase.SaveAssetIfDirty(_settingsData);
                    AssetDatabase.Refresh();
                    settingsData = AssetDatabase.LoadAssetAtPath<UserSaveData>(assetPath);
                }
            }
            return settingsData;
        }
        internal static string _GetRootPath()
        {
            if (Directory.Exists("Assets/CoInspector/Editor"))
            {
                return "Assets/CoInspector/Editor";
            }
            if (Directory.Exists("Assets/Plugins/CoInspector/Editor"))
            {
                return "Assets/Plugins/CoInspector/Editor";
            }
            Assembly assembly = typeof(CoInspectorWindow).Assembly;
            string assemblyPath = assembly.Location;
            assemblyPath = assemblyPath.Replace("\\", "/");
            if (!assemblyPath.Contains("/Assets/"))
            {
                return GetRootScript();
            }
            assemblyPath = assemblyPath.Substring(assemblyPath.LastIndexOf("Assets"));
            assemblyPath = assemblyPath.Substring(0, assemblyPath.LastIndexOf("/"));
            rootPath = assemblyPath;
            return assemblyPath;
        }

        internal static string GetRootScript()
        {
            string _rootPath = "";
            string[] guids = AssetDatabase.FindAssets("CoInspector t:Script");
            if (guids.Length > 0)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[0]);
                _rootPath = path.Substring(0, path.LastIndexOf("/"));
                _rootPath = _rootPath.Substring(0, path.LastIndexOf("/Core/"));
                rootPath = _rootPath;
            }
            return _rootPath;
        }
        internal void SaveSession()
        {
            if (settingsData == null)
            {
                settingsData = AutoCreateSettings();
            }
            if (settingsData && !InRecoverScreen())
            {
                settingsData.SaveData(this);
            }
            else if (settingsData)
            {
                TrySaveLastAssets();
                EditorUtility.SetDirty(settingsData);
                AssetDatabase.SaveAssetIfDirty(settingsData);
            }
        }
        private void OnSceneSaving(Scene scene, string path)
        {
            SaveSession();
        }
        void ManageDebugMode(bool _debug)
        {
            if (IsActiveTabValid())
            {
                {
                    GetActiveTab().debug = _debug;
                    ReinitializeComponentEditors();
                }
            }
        }
        internal bool ActiveTabInDebugMode()
        {
            if (IsActiveTabValid())
            {
                return GetActiveTab().debug || globalDebugMode;
            }
            return globalDebugMode;
        }
        void ManageGlobalDebugMode(bool _debug)
        {
            if (IsActiveTabValid())
            {
                globalDebugMode = _debug;
                ReinitializeComponentEditors();
            }
        }
        static void RegisterWindow(CoInspectorWindow window)
        {
            if (instances == null)
            {
                instances = new List<CoInspectorWindow>();
            }
            if (!instances.Contains(window))
            {
                instances.Add(window);
            }
            mainCoInspector = window;
            for (int i = 0; i < instances.Count; i++)
            {
                if (instances[i] == null)
                {
                    instances.RemoveAt(i);
                    i--;
                }
            }
        }
        bool InEmptyWorkspace()
        {
            if (tabs != null || tabs.Count == 1)
            {
                if (tabs[0].newTab)
                {
                    return true;
                }
            }
            return false;
        }
        bool IsThereAPreviousSession()
        {
            if (settingsData)
            {
                lastSessionData = settingsData.LoadData(this, false);
                if (lastSessionData != null && lastSessionData.tabs != null && lastSessionData.tabs.Count > 0)
                {
                    //   Debug.Log("Loaded a session with " + lastSessionData.tabs.Count + " tabs");
                    return true;
                }
                else
                {
                    //   Debug.Log("No session found");
                    return false;
                }
            }
            return false;
        }
        void SetupTracker()
        {
            if (lastSessionData != null && lastSessionData.tracker != null)
            {
                tracker = new GameObjectTracker(lastSessionData.tracker);
            }
            else
            {
                tracker = new GameObjectTracker();
            }
            ReadLastClicked();
        }

        bool TryLoadSession()
        {
            if (settingsData)
            {
                lastSessionData = settingsData.LoadData(this);
                SetupTracker();
                if (sessionsMode == 2 && InEmptyWorkspace())
                {
                    return false;
                }
                if (lastSessionData != null && lastSessionData.tabs != null && lastSessionData.tabs.Count > 0)
                {
                    // Debug.Log("Loaded a session with " + lastSessionData.tabs.Count + " tabs");
                    return true;
                }
                else
                {
                    // Debug.Log("No session found");
                    return false;
                }
            }
            else
            {
                settingsData = AutoCreateSettings();
            }
            return false;
        }

        internal void CloseTab(TabInfo tab, bool remember = true, bool bulkClosing = false)
        {
            if (tabs.Count > 1 && tab != null)
            {
                int index = tabs.IndexOf(tab);
                if (index < tabs.Count)
                {
                    CloseTab(index, remember, bulkClosing);
                }
            }
        }

        internal void DoCloseTab(TabInfo tab, bool remember = true, bool bulkClosing = false)
        {

            if (tab == null)
            {
                return;
            }
            if (closedTabs == null)
            {
                closedTabs = new List<TabInfo>();
            }
            int index = tabs.IndexOf(tab);
            tab.DestroyAllMaterialMaps();
            if (tab == GetActiveTab())
            {
                EditorToolCache.RestorePreviousPersistentTool();
            }
            tab.index = index;
            if (!tab.newTab && remember)
            {
                closedTabs.Insert(0, tab);
                ManageClosedTabs();
            }
            tabs.Remove(tab);
            bool changedActive = false;
            if (activeIndex == index && tabs.Count > 0)
            {
                changedActive = true;
            }
            if (activeIndex == index && previousTab != null)
            {
                activeIndex = previousTab.index;
            }
            if (activeIndex > 0 && index <= activeIndex)
            {
                activeIndex -= 1;
            }
            if (!bulkClosing && changedActive)
            {
                if (IsActiveTabValid())
                {
                    FocusTab(activeIndex);
                }
                else
                {
                    FocusTab(0);
                }
            }
            Repaint();
        }

        void CloseTab(int index, bool remember = true, bool bulkClosing = false)
        {
            HandlePendingTabDeletion();
            if (tabs.Count > 1 && index < tabs.Count)
            {
                if (closedTabs == null)
                {
                    closedTabs = new List<TabInfo>();
                }
                TabInfo tab = tabs[index];
                if (tab == null)
                {
                    return;
                }
                if (!bulkClosing/* && index < tabs.Count - 1*/)
                {
                    FloatingTab.StartClosingTab(tab);
                }
                else
                {
                    DoCloseTab(tab, remember, bulkClosing);
                }
            }
        }
        bool AreThereClosedTabs()
        {
            if (closedTabs != null && closedTabs.Count > 0)
            {
                return true;
            }
            return false;
        }
        internal static void ResetToDefault()
        {
            hideUpdateAlert = false;
            recycleUnlockedTabs = false;
            mouseWheelSpeed = 1;
            userInstalls = new string[] { };
            tabPreviewExpanded = true;
            softWheelScrolling = true;
            middleScrollMode = 1;
            newTabIfLocked = false;
            showHistory = true;
            showTabName = true;
            showTabTree = true;
            showFilterBar = true;
            hideEmptyComponents = false;
            softSelection = true;
            showIcons = true;
            richNames = true;
            autoFocus = false;
            showScrollBar = true;
            showCollapseTool = true;
            showHierarchyButton = true;
            showInspectorButton = true;
            showFocusButton = true;
            showSelectButton = true;
            showMaximizeButton = false;
            assetPreviewExpanded = true;
            showAdditionalOptions = true;
            showLastClicked = true;
            showMostClicked = true;
            rememberSessions = true;
            useThumbKeys = true;
            ignoreFolders = true;
            collapsePrefabComponents = true;
            openPrefabsInNewTab = true;
            showTextAssetPreviews = true;
            showAssetLabels = false;
            assetInspection = true;
            componentCulling = true;
            reuseTabs = false;
            overrideSceneTools = false;
            sessionsMode = 0;
            tabCompactMode = 1;
            doubleClickMode = 1;
            scrollSpeedX = 2;
            scrollSpeedY = 2;
            scrollDirectionX = 1;
            scrollDirectionY = 1;
            mouseWheelSensitivity = 18;
            if (MainCoInspector)
            {
                MainCoInspector.UpdateAllWidths();
            }
        }
        void ManageClosedTabs()
        {
            if (closedTabs != null && closedTabs.Count > 10)
            {
                closedTabs[closedTabs.Count - 1].DestroyAllMaterialMaps();
                closedTabs.RemoveAt(closedTabs.Count - 1);
            }
        }

        internal void UpdateAllWidths()
        {
            if (tabs != null && tabs.Count > 0)
            {
                for (int i = 0; i < tabs.Count; i++)
                {
                    tabs[i].UpdateTabWidth();
                }
            }
        }
        void RestoreClosedTab()
        {
            if (closedTabs != null && closedTabs.Count > 0)
            {
                TabInfo tab = closedTabs[0];
                if (tab == null || (tab.target == null && !tab.IsValidMultiTarget()))
                {
                    closedTabs.Remove(tab);
                    if (closedTabs.Count > 0)
                    {
                        RestoreClosedTab();
                    }
                    return;
                }
                tab.markForDeletion = false;
                tab.willBeDeleted = false;
                int previousIndex = tab.index;
                UpdatePreviousTab();
                if (previousIndex > tabs.Count)
                {
                    tabs.Add(tab);
                    totalWidth.Add(100);
                    previousIndex = tabs.Count - 1;
                }
                else
                {
                    tabs.Insert(previousIndex, tab);
                    totalWidth.Insert(previousIndex, 100);
                }
                closedTabs.Remove(tab);
                FocusTab(previousIndex);
            }
        }
        internal bool TryReuseTab(GameObject target, bool isMiddleClick = false)
        {
            if (!reuseTabs)
            {
                return false;
            }
            if (target == null || tabs == null)
            {
                return false;
            }
            TabInfo tab = GetTabWithTarget(target);
            if (tab != null)
            {
                if (tab != GetActiveTab())
                {
                    FocusTab(tab.index);
                }
                UpdateClicked(target);
                return true;
            }
            return false;
        }
        internal bool ActiveTabHasDisabledTargets()
        {
            if (GetActiveTab() != null)
            {
                return TabHasDisabledTargets(GetActiveTab());
            }
            return false;
        }
        internal bool TabHasDisabledTargets(TabInfo tab)
        {
            if (tab == null)
            {
                return false;
            }
            if (tab.multiEditMode)
            {
                foreach (var target in tab.targets)
                {
                    if (target is GameObject go && go.activeInHierarchy)
                    {
                        return false;
                    }
                }
                return true;
            }
            else if (tab.target)
            {
                return !tab.target.activeInHierarchy;
            }
            return false;
        }

        internal TabInfo GetClosestUnlockedTab()
        {
            if (tabs == null || tabs.Count < 2)
            {
                return null;
            }

            if (activeIndex < 0 || activeIndex >= tabs.Count)
            {
                return null;
            }

            if (tabs[activeIndex] == null)
            {
                return null;
            }

            int maxOffset = Math.Max(activeIndex, tabs.Count - 1 - activeIndex);

            for (int offset = 1; offset <= maxOffset; offset++)
            {
                int plusIndex = activeIndex + offset;
                if (plusIndex < tabs.Count)
                {
                    var plusTab = tabs[plusIndex];
                    if (plusTab != null && !plusTab.locked)
                    {
                        return plusTab;
                    }
                }
                int minusIndex = activeIndex - offset;
                if (minusIndex >= 0)
                {
                    var minusTab = tabs[minusIndex];
                    if (minusTab != null && !minusTab.locked)
                    {
                        return minusTab;
                    }
                }
            }
            return null;
        }


        internal TabInfo GetTabWithTarget(GameObject target)
        {
            if (target == null || tabs == null)
            {
                return null;
            }
            if (GetActiveTab().target && GetActiveTab().target == target && !IsActiveTabLocked())
            {
                return GetActiveTab();
            }
            if (GetActiveTab().newTab)
            {
                return null;
            }
            foreach (var tab in tabs)
            {
                if (tab == null)
                {
                    continue;
                }
                if (tab.target == target && !(tab == GetActiveTab() && tab.locked && !newTabIfLocked))
                {
                    return tab;
                }
            }
            return null;
        }


        void FocusTab()
        {
            if (tabs.Count > 0 && activeIndex < tabs.Count)
            {
                FocusTab(activeIndex);
            }
        }
        void FocusTab(int index, bool forceAfter = false, bool keepCount = false)
        {
            if (tabs.Count > index)
            {
                GameObject[] gos;
                if (tabs[index].IsValidMultiTarget())
                {
                    gos = tabs[index].targets;
                    targetGameObject = null;
                }
                else
                {
                    targetGameObject = tabs[index].target;
                    gos = new GameObject[] { targetGameObject };
                }
                string[] names = null;
                if (gos != null)
                {
                    gos = gos.Where(t => t != null).ToArray();
                    if (gos.Length > 0)
                    {
                        names = gos.Where(t => t).Select(t => t.name).ToArray();
                    }
                    else
                    {
                        gos = null;
                    }
                }
                if (EditorGUIUtility.editingTextField)
                {
                    EditorGUIUtility.keyboardControl = -1;
                }
                DoFocusTab(index, forceAfter, gos, names, keepCount);
                if (index >= 0 && index < tabs.Count)
                {
                    if (tabs[index].newTab)
                    {
                        ReadLastClicked();
                    }
                }
            }
        }
        internal void UpdateTabBar(bool @override = false)
        {

            if (Event.current == null && !@override)
            {
                return;
            }
            if (tabs == null || tabs.Count == 0)
            {
                return;
            }
            HandlePendingTabDeletion();
            if (totalWidth == null)
            {
                totalWidth = new List<float>();
            }
            totalWidth.Clear();
            for (int i = 0; i < tabs.Count; i++)
            {
                UpdateTab(tabs[i], i);
            }
        }
        void UpdateTab(TabInfo tab, int index)
        {
            if (tab == null)
            {
                return;
            }
            tab.UpdateTabName();
            tab.index = index;
            if (tab.markForDeletion)
            {
                totalWidth.Add(0);
            }
            else if (FloatingTab.isClosing && FloatingTab.linkedTab != null && FloatingTab.linkedTab == tab)
            {
                totalWidth.Add(FloatingTab.GetClosingTabWidth());
            }
            else if (FloatingTab.isOpening && FloatingTab.linkedTab != null && FloatingTab.linkedTab == tab)
            {
                totalWidth.Add(FloatingTab.GetOpeningTabWidth());
            }

            else
            {
                totalWidth.Add(tab.tabWidth);
            }

        }
        void DoFocusTab(int index, bool forceAfter, GameObject[] gos, string[] names, bool keepCount)
        {
            FixActiveIndex();
            GetActiveTab().DestroyAllMaterialMaps();

            if (keepCount)
            {
                bool clickingSame = index == activeIndex;
                if (!clickingSame)
                {
                    UpdateClicked(gos);
                }
            }
            int _previousIndex = activeIndex;
            if (activeIndex != index)
            {
                _UpdateTabScroll();
            }
            activeIndex = index;
            if (GetActiveTab() != null)
            {
                GetActiveTab().zoomFocus = false;
            }
            if (CanAutoFocus(_previousIndex, activeIndex))
            {
                EditorUtils.FocusOnSceneView(gos);
            }
            if (!softSelection && !tabs[index].newTab)
            {
                SelectIfNotAlready(gos, true, forceAfter);
            }
            // ReinitializeComponentEditors(false);
            ReinitializeComponentEditors();
            LoadTabFoldoutsIfPresent();
            RefreshAllTabNames();
            RefreshAllIcons();
            if (gos != null && names != null && gos.Length == names.Length)
            {
                EditorApplication.delayCall += () =>
                {
                    for (int i = 0; i < gos.Length; i++)
                    {
                        if (gos[i] != null && i < names.Length)
                        {
                            if (gos[i].name != names[i])
                            {
                                gos[i].name = names[i];
                            }
                        }
                    }
                };
            }
            ScrollToIndex(index);
            SetScrollPosition(GetActiveTab().scrollPosition);
            EditorToolCache.RestorePreviousPersistentTool();
            enteredSafeZone = false;
        }
        bool IsItAMultiPrefabTarget()
        {
            if (AssetTargetMode() == 0)
            {
                return false;
            }
            if (AssetTargetMode() == 2)
            {
                if (targetObjects.All(t => t is GameObject))
                {
                    return true;
                }
            }
            return false;
        }
        internal void RefreshAllIcons()
        {
            if (tabs == null)
            {
                return;
            }
            for (int i = 0; i < tabs.Count; i++)
            {
                if (tabs[i] == GetActiveTab())
                {
                    bool wasPrefabInstance = tabs[i].IsPrefabInstance();
                    tabs[i].RefreshIcon();
                    if (wasPrefabInstance != GetActiveTab().IsPrefabInstance())
                    {
                        ReinitializeComponentEditors();
                    }
                }
                else
                {
                    tabs[i].RefreshIcon();
                }
            }
        }
        void SaveFoldoutsToTab()
        {
            if (GetActiveTab() != null)
            {
                if (componentFoldouts_ != null)
                {
                    GetActiveTab().runtimeFoldouts = new bool[componentFoldouts_.Length];
                    Array.Copy(componentFoldouts_, GetActiveTab().runtimeFoldouts, componentFoldouts_.Length);
                }
            }
        }
        void LoadTabFoldoutsIfPresent()
        {
            if (GetActiveTab() != null)
            {
                if (GetActiveTab().runtimeFoldouts != null && componentEditors != null && componentEditors.Length == GetActiveTab().runtimeFoldouts.Length)
                {
                    componentFoldouts_ = new bool[GetActiveTab().runtimeFoldouts.Length];
                    Array.Copy(GetActiveTab().runtimeFoldouts, componentFoldouts_, componentFoldouts_.Length);
                }
            }
        }


        public void SetTargetGameObject(GameObject go)
        {
            ReadLastClicked();
            GetActiveTab().runtimeMultiComponents = null;
            bool allCollapsed = GetActiveTab().AreAllCollapsed();
            bool targetChanged = false;
            if (go || IsActiveTabNew())
            {
                if (go != null)
                {
                    GameObject[] gos = new GameObject[] { go };
                    ignoreSelection = gos;
                    if (!softSelection && !IsAlreadySelected(gos))
                    {
                        if (ignoreSelection != null)
                        {
                            Selection.objects = gos;
                        }
                    }
                }
                GetActiveTab().multiEditMode = false;
                GetActiveTab().targets = null;
                GetActiveTab().AddToHistoryIfProceeds(go);
                UpdateClicked(go);
                if (targetGameObject != go)
                {
                    targetChanged = true;
                }
                targetGameObject = go;
                GetActiveTab().UpdateLinkedPrefabs();
                RefreshAllTabNames();
                UpdateAllTabPaths();
                GetActiveTab().RefreshIcon();
                dragIndex = -1;
                //ReinitializeComponentEditors();
                if (targetChanged)
                {
                    if (componentScrollView != null)
                    {
                        componentScrollView.scrollOffset = Vector2.zero;
                    }
                }
                FocusTab();
                if (targetChanged)
                {
                    GetActiveTab().SetAllMapsTo(!allCollapsed);
                }
            }
        }
        public void SetTargetGameObjects(GameObject[] gameObjects)
        {
            ReadLastClicked();
            if (tabs != null && GetActiveTab() != null)
            {
                bool allCollapsed = GetActiveTab().AreAllCollapsed();
                bool targetChanged = false;
                targetGameObject = null;
                gameObjects = gameObjects.Distinct().ToArray();
                GetActiveTab().runtimeMultiComponents = null;
                GetActiveTab().target = null;
                GetActiveTab().newTab = false;
                GetActiveTab().multiEditMode = true;
                ignoreSelection = gameObjects;

                if (!softSelection && !IsAlreadySelected(gameObjects))
                {
                    Selection.objects = gameObjects;
                }
                GetActiveTab().SetPrefabMode();
                if (!EditorUtils.AreArraysIdentical(gameObjects, GetActiveTab().targets))
                {
                    targetChanged = true;
                }
                GetActiveTab().AddToHistoryIfProceeds(gameObjects);
                GetActiveTab().UpdateLinkedPrefabs();
                UpdateAllTabPaths();
                RefreshAllTabNames();
                /*
                if (autoFocus)
                {
                    EditorUtils.FocusOnSceneView(gameObjects);
                }*/
                GetActiveTab().RefreshIcon();
                UpdateClicked(gameObjects);
                dragIndex = -1;
                //ReinitializeComponentEditors();
                if (targetChanged)
                {
                    if (componentScrollView != null)
                    {
                        componentScrollView.scrollOffset = Vector2.zero;
                    }
                }
                FocusTab();
                if (targetChanged)
                {
                    GetActiveTab().SetAllMapsTo(!allCollapsed);
                }

            }
        }
        internal void ShowButton(Rect position)
        {
            if (lockButtonStyle == null)
            {
                lockButtonStyle = "IN LockButton";
            }
            if (assetOnlyMode)
            {
                EditorGUI.BeginChangeCheck();
                bool lockStatus = GUI.Toggle(position, lockedAsset, GUIContent.none, lockButtonStyle);
                if (EditorGUI.EndChangeCheck())
                {
                    lockedAsset = lockStatus;
                    Repaint();
                }
                GUIContent content = CustomGUIContents.DebugIconOFF;

                if (debugAsset)
                {
                    content = CustomGUIContents.DebugIconON;
                }
                EditorGUI.BeginChangeCheck();
                Rect rect = new Rect(position.x - 18, position.y, 20, 20);
                bool debugStatus = GUI.Toggle(rect, debugAsset, content, CustomGUIStyles.DebugIconStyle);
                GUI.enabled = true;
                if (EditorGUI.EndChangeCheck())
                {
                    debugAsset = debugStatus;
                    DrawCurrentAssets();
                }

                return;
            }
            if (tabs == null || tabs.Count == 0)
            {
                return;
            }
            if (tabs.Count > activeIndex && activeIndex >= 0)
            {
                if (GetActiveTab().newTab)
                {
                    GUI.enabled = false;
                }
                EditorGUI.BeginChangeCheck();
                bool lockStatus = GUI.Toggle(position, GetActiveTab().locked, GUIContent.none, lockButtonStyle);
                GUI.enabled = true;
                if (EditorGUI.EndChangeCheck())
                {
                    if (GetActiveTab().target != null || GetActiveTab().IsValidMultiTarget())
                    {
                        GetActiveTab().locked = lockStatus;
                        UpdateAllWidths();
                    }
                }
                if (GetActiveTab().newTab)
                {
                    GUI.enabled = false;
                }
                GUIContent content = CustomGUIContents.DebugIconOFF;
                if (globalDebugMode)
                {
                    content = CustomGUIContents.FullDebugIconON;
                }
                else if (GetActiveTab().debug)
                {
                    content = CustomGUIContents.DebugIconON;
                }
                EditorGUI.BeginChangeCheck();
                Rect rect = new Rect(position.x - 18, position.y, 20, 20);
                bool debugStatus;

                if (!globalDebugMode)
                {
                    debugStatus = GUI.Toggle(rect, GetActiveTab().debug, content, CustomGUIStyles.DebugIconStyle);
                }
                else
                {
                    debugStatus = GUI.Toggle(rect, globalDebugMode, content, CustomGUIStyles.DebugIconStyle);
                }
                GUI.enabled = true;
                if (EditorGUI.EndChangeCheck())
                {
                    int pressedButton = Event.current.button;
                    if (GetActiveTab().target != null || GetActiveTab().IsValidMultiTarget())
                    {
                        if (pressedButton == 2)
                        {
                            ManageGlobalDebugMode(!globalDebugMode);
                        }
                        else if (pressedButton == 1)
                        {
                            GenericMenu menu = new GenericMenu();
                            if (!globalDebugMode && !GetActiveTab().debug)
                            {
                                menu.AddItem(new GUIContent("Enter Tab Debug Mode"), false, () =>
                                {
                                    ManageDebugMode(true);
                                    Repaint();
                                });
                                menu.AddItem(new GUIContent("Enter Full Debug Mode"), false, () =>
                                {
                                    ManageGlobalDebugMode(true);
                                    Repaint();
                                });
                            }
                            else
                            {
                                if (globalDebugMode)
                                {
                                    menu.AddItem(new GUIContent("Exit Full Debug Mode"), false, () =>
                                    {
                                        ManageGlobalDebugMode(false);
                                        Repaint();
                                    });
                                }
                                else if (GetActiveTab().debug)
                                {
                                    menu.AddItem(new GUIContent("Exit Tab Debug Mode"), false, () =>
                                    {
                                        ManageDebugMode(false);
                                        Repaint();
                                    });
                                    menu.AddItem(new GUIContent("Enter Full Debug Mode"), false, () =>
                                    {
                                        ManageGlobalDebugMode(true);
                                        Repaint();
                                    });
                                }
                            }
                            menu.ShowAsContext();

                        }
                        else
                        {
                            if (!globalDebugMode)
                            {
                                ManageDebugMode(debugStatus);
                            }
                            else
                            {
                                ManageGlobalDebugMode(debugStatus);
                            }
                        }
                    }
                }
            }
        }
        public void AddItemsToMenu(GenericMenu menu)
        {
            if (GetActiveTab() != null && !IsActiveTabNew())
            {
                menu.AddItem(new GUIContent("Normal Mode"), !globalDebugMode, () =>
                 {
                     ManageGlobalDebugMode(false);
                     Repaint();
                 });
                menu.AddItem(new GUIContent("Full Debug Mode"), globalDebugMode, () =>
                {
                    ManageGlobalDebugMode(true);
                    Repaint();
                });
                menu.AddSeparator("");
            }
            if (MainCoInspector)
            {
                if (MainCoInspector.IsThereAPreviousSession())
                {
                    menu.AddItem(new GUIContent("Restore Last Saved Session"), false, () =>
                    {
                        ShowRecoverSessionDialogue();
                    });
                }
            }
            menu.AddItem(new GUIContent("Refresh CoInspector UI"), false, () =>
            {
                CreateGUI();
            });
            menu.AddItem(new GUIContent("★ CoInspector Settings"), false, () =>
            {
                SettingsWindow.ShowWindow();
            });
        }
        internal void ShowSettingsMenu(GenericMenu menu, bool skip = false)
        {
            if (GetActiveTab() != null && !IsActiveTabNew() && !skip)
            {
                if (globalDebugMode)
                {
                    menu.AddItem(new GUIContent("Exit Full Debug Mode"), false, () =>
                    {
                        ManageGlobalDebugMode(false);
                        Repaint();
                    });
                }
                else
                {
                    menu.AddItem(new GUIContent("Normal"), !GetActiveTab().debug, () =>
                    {
                        GetActiveTab().debug = false;
                        ManageDebugMode(false);
                        Repaint();
                    });
                    menu.AddItem(new GUIContent("Debug Tab"), GetActiveTab().debug, () =>
                    {
                        GetActiveTab().debug = true;
                        ManageDebugMode(true);
                        Repaint();
                    });
                }
                menu.AddSeparator("");
            }
            menu.AddItem(new GUIContent("Inspector Settings/On Tab Click/Select in Hierarchy"), !softSelection, () =>
            {
                softSelection = !softSelection;
                SaveSettings();
                Repaint();
                if (tabs != null && tabs.Count > 0)
                {
                    if (GetActiveTab() != null)
                    {
                        FocusTab(activeIndex);
                    }
                }
            });
            menu.AddItem(new GUIContent("Inspector Settings/On Tab Click/Frame on Scene View"), autoFocus, () =>
           {
               autoFocus = !autoFocus;
               SaveSettings();
               Repaint();
               if (tabs != null && tabs.Count > 0)
               {
                   if (GetActiveTab() != null)
                   {
                       FocusTab(activeIndex);
                   }
               }
           });
            menu.AddDisabledItem(new GUIContent("Inspector Settings/On Tab Double-click/CHOOSE ONE"));
            menu.AddItem(new GUIContent("Inspector Settings/On Tab Double-click/Lock or Unlock Tab"), doubleClickMode == 0, () =>
            {
                doubleClickMode = 0;
                SaveSettings();
                Repaint();
            });
            menu.AddItem(new GUIContent("Inspector Settings/On Tab Double-click/Select in Hierarchy"), doubleClickMode == 1, () =>
            {
                doubleClickMode = 1;
                SaveSettings();
                Repaint();
            });
            menu.AddItem(new GUIContent("Inspector Settings/On Tab Double-click/Frame on Scene View"), doubleClickMode == 2, () =>
            {
                doubleClickMode = 2;
                SaveSettings();
                Repaint();
            });
            menu.AddItem(new GUIContent("Inspector Settings/On Tab Double-click/Show In Local Hierarchy View"), doubleClickMode == 3, () =>
           {
               doubleClickMode = 3;
               SaveSettings();
               Repaint();
           });
            menu.AddItem(new GUIContent("Inspector Settings/On Tab Hover Info/Tree view"), showTabTree && showTabTree, () =>
       {
           showTabName = true;
           showTabTree = true;
           SaveSettings();
           Repaint();
       });
            menu.AddItem(new GUIContent("Inspector Settings/On Tab Hover Info/Name"), showTabName && !showTabTree, () =>
            {
                showTabName = true;
                showTabTree = false;
                SaveSettings();
                Repaint();
            });
            menu.AddItem(new GUIContent("Inspector Settings/On Tab Hover Info/Nothing"), !showTabName, () =>
           {
               showTabName = false;
               showTabTree = false;
               SaveSettings();
               Repaint();
           });

            menu.AddItem(new GUIContent("Inspector Settings/Tab Bar Design/Tab Size/Compact"), tabCompactMode == 1, () =>
            {
                tabCompactMode = 1;
                UpdateAllWidths();
                ScrollToActiveTab();
                SaveSettings();
                Repaint();
            });
            menu.AddItem(new GUIContent("Inspector Settings/Tab Bar Design/Tab Size/Normal"), tabCompactMode == 2, () =>
            {
                tabCompactMode = 2;
                UpdateAllWidths();
                ScrollToActiveTab();
                SaveSettings();
                Repaint();
            });
            menu.AddItem(new GUIContent("Inspector Settings/Tab Bar Design/Show History Tracking buttons"), showHistory, () =>
        {
            showHistory = !showHistory;
            SaveSettings();
            Repaint();
        });
            menu.AddItem(new GUIContent("Inspector Settings/Tab Bar Design/Show Tab icons"), showIcons, () =>
        {
            showIcons = !showIcons;
            UpdateAllWidths();
            SaveSettings();
            Repaint();
        });
            menu.AddItem(new GUIContent("Inspector Settings/Tab Bar Design/Show Scrollbar"), showScrollBar, () =>
           {
               showScrollBar = !showScrollBar;
               SaveSettings();
               Repaint();
           });
            menu.AddItem(new GUIContent("Inspector Settings/Tab Bar Design/Show Collapse button"), showCollapseTool, () =>
                {
                    showCollapseTool = !showCollapseTool;
                    SaveSettings();
                    Repaint();
                });
            menu.AddItem(new GUIContent("Inspector Settings/Tab Bar Design/Show Component Filter bar"), showFilterBar, () =>
            {
                showFilterBar = !showFilterBar;
                if (!showFilterBar)
                {
                    DisableFilterField();
                }
                else if (IsActiveTabValid())
                {
                    GetActiveTab().filtering = false;
                }
                SaveSettings();
                Repaint();
            });
            menu.AddItem(new GUIContent("Inspector Settings/Tab Bar Scroll Settings/Vertical Mouse Wheel Speed/Normal"), scrollSpeedY == 2, () =>
         {
             scrollSpeedY = 2;
             SaveSettings();
             Repaint();
         });
            menu.AddItem(new GUIContent("Inspector Settings/Tab Bar Scroll Settings/Vertical Mouse Wheel Speed/Fast"), scrollSpeedY == 3, () =>
            {
                scrollSpeedY = 3;
                SaveSettings();
                Repaint();
            });
            menu.AddItem(new GUIContent("Inspector Settings/Tab Bar Scroll Settings/Vertical Mouse Wheel Speed/Fastest"), scrollSpeedY == 4, () =>
            {
                scrollSpeedY = 4;
                SaveSettings();
                Repaint();
            });
            menu.AddItem(new GUIContent("Inspector Settings/Tab Bar Scroll Settings/Horizontal Mouse Wheel Speed/Normal"), scrollSpeedX == 2, () =>
      {
          scrollSpeedX = 2;
          SaveSettings();
          Repaint();
      });
            menu.AddItem(new GUIContent("Inspector Settings/Tab Bar Scroll Settings/Horizontal Mouse Wheel Speed/Fast"), scrollSpeedX == 3, () =>
           {
               scrollSpeedX = 3;
               SaveSettings();
               Repaint();
           });
            menu.AddItem(new GUIContent("Inspector Settings/Tab Bar Scroll Settings/Horizontal Mouse Wheel Speed/Fastest"), scrollSpeedX == 4, () =>
           {
               scrollSpeedX = 4;
               SaveSettings();
               Repaint();
           });
            menu.AddItem(new GUIContent("Inspector Settings/Tab Bar Scroll Settings/Invert Vertical Mouse Wheel"), scrollDirectionY == -1, () =>
             {
                 if (scrollDirectionY == -1)
                 {
                     scrollDirectionY = 1;
                 }
                 else
                 {
                     scrollDirectionY = -1;
                 }
                 SaveSettings();
                 Repaint();
             });
            menu.AddItem(new GUIContent("Inspector Settings/Tab Bar Scroll Settings/Invert Horizontal Scroll Wheel"), scrollDirectionX == -1, () =>
          {
              if (scrollDirectionX == -1)
              {
                  scrollDirectionX = 1;
              }
              else
              {
                  scrollDirectionX = -1;
              }
              SaveSettings();
              Repaint();
          });
            menu.AddItem(new GUIContent("Inspector Settings/Sessions Behavior/Always Ask"), sessionsMode == 0, () =>
           {
               sessionsMode = 0;
               rememberSessions = true;
               SaveSettings();
               Repaint();
           });
            menu.AddItem(new GUIContent("Inspector Settings/Sessions Behavior/Always Restore"), sessionsMode == 1, () =>
            {
                sessionsMode = 1;
                rememberSessions = true;
                SaveSettings();
                Repaint();
            });
            menu.AddItem(new GUIContent("Inspector Settings/Sessions Behavior/Disable Sessions"), sessionsMode == 2, () =>
            {
                if (EditorUtility.DisplayDialog("Are you sure?", "Your workspace and open tabs will be reset when you switch scenes or restart the editor.", "Yes!", "No"))
                {
                    sessionsMode = 2;
                    rememberSessions = false;
                    SaveSettings();
                    Repaint();
                }
            });
            menu.AddItem(new GUIContent("Inspector Settings/Switch to Existing Tabs"), reuseTabs, () =>
            {
                reuseTabs = !reuseTabs;
                SaveSettings();
                Repaint();
            });
            menu.AddItem(new GUIContent("Inspector Settings/Ignore Folder Inspection"), ignoreFolders, () =>
             {
                 ignoreFolders = !ignoreFolders;
                 SaveSettings();
                 Repaint();
             });
            menu.AddItem(new GUIContent("Inspector Settings/Disable Asset Inspection"), !assetInspection, () =>
            {
                assetInspection = !assetInspection;
                if (!assetInspection)
                {
                    CloseAssetView();
                }
                SaveSettings();
                Repaint();
            });
            menu.AddItem(new GUIContent("Inspector Settings/★ Open the Settings Window"), false, () =>
            {
                SettingsWindow.ShowWindow();
            });
        }
        internal void RefreshTrackerPaths()
        {
            if (tracker != null)
            {
                tracker.RefreshAllPaths();
            }
        }

        internal void RefreshAllTabNames()
        {
            if (tabs == null)
            {
                return;
            }
            for (int i = 0; i < tabs.Count; i++)
            {
                if (tabs[i] == null)
                {
                    tabs.RemoveAt(i);
                    i--;
                    continue;
                }
                tabs[i].RefreshName();
                tabs[i].SetPrefabMode();
            }
        }
        internal static bool IsAssetAFolder(string assetPath)
        {
            return AssetDatabase.IsValidFolder(assetPath);
        }
        internal void HideHeaderMargin()
        {/*
            if (EditorGUIUtility.editingTextField && Event.current != null && Event.current.type == EventType.MouseDown)
            {
                GUIUtility.hotControl = 0;
                GUIUtility.keyboardControl = 0;
                rootVisualElement.MarkDirtyRepaint();
            }*/
            AutoScrollOnDrag();
            Rect underRect = new Rect(scrollRect);
            if (!toolScrollBarVisible)
            {
                underRect.y += underRect.height - 2;
                underRect.height = 15;
                underRect.x = 0;
                underRect.width = this.position.width;
                EditorUtils.DrawLineOverRect(underRect, CustomColors.DefaultInspector, -0, 5);
                EditorUtils.DrawLineOverRect(underRect);
                if (!EditorUtils.IsLightSkin())
                {
                    EditorUtils.DrawLineOverRect(underRect, CustomColors.HardShadow, 1);
                }
                return;
            }
            underRect.y += underRect.height + 5;
            underRect.height = 20;
            underRect.x = 0;
            underRect.width = this.position.width;
            EditorUtils.DrawLineOverRect(underRect, CustomColors.DefaultInspector, -10, 3);
            EditorUtils.DrawLineOverRect(underRect, -10);
            if (!EditorUtils.IsLightSkin())
            {
                EditorUtils.DrawLineOverRect(underRect, CustomColors.MediumShadow, -9, 1);
            }
            Rect rect = underRect;
            rect.x = rect.x + rect.width - 13;
            rect.y -= 2;
            rect.width = 13;
            rect.height = 15;
            EditorUtils.DrawLineOverRect(underRect, CustomColors.DefaultInspector, 2);
            EditorUtils.DrawLineOverRect(underRect, CustomColors.MediumShadow, 2);

            if (!EditorUtils.IsLightSkin())
            {
                EditorUtils.DrawLineOverRect(underRect, CustomColors.MediumShadow, 8);
            }
            EditorGUI.DrawRect(rect, CustomColors.DefaultInspector);
            rect.x = 0;
            rect.width = 13;
            EditorGUI.DrawRect(rect, CustomColors.DefaultInspector);
        }
        internal void DrawActiveTabUnder()
        {
            if (GetActiveTab() != null)
            {
                Rect rect = activeTabRect;
                if (FloatingTab.isClosing)
                {
                    if (FloatingTab.linkedTab == GetActiveTab())
                    {
                        rect.width = FloatingTab.GetClosingTabWidth();
                    }
                }

                rect.y = 0;

                rect = new Rect(rect.x, rect.y + 20, rect.width - 1, 3);
                if (rect.xMax > position.width - 24)
                {
                    rect.width = position.width - 24 - rect.x;
                }
                if (rect.xMin > position.width - 24)
                {
                    rect.width = 0;
                }
                if (showHistory)
                {
                    if (rect.x < 40)
                    {
                        rect.width -= 40 - rect.x;
                        rect.x = 40;
                    }
                    if (rect.xMax < 40)
                    {
                        rect.width = 0;
                    }
                }
                else if (dragging && !GOdragging && dragIndex == activeIndex)
                {
                    rect.y -= 1;
                }
                Rect fullLine = new Rect(0, rect.y + 3, this.position.width, 1);
                Color editorColor = CustomColors.DefaultInspector;
                if (showScrollBar)
                {
                    if (toolScrollBarVisible)
                    {
                        Rect hugeLine = new Rect(fullLine)
                        {
                            height = 6
                        };
                        EditorGUI.DrawRect(hugeLine, CustomColors.DefaultInspector);
                    }
                }

                EditorGUI.DrawRect(fullLine, editorColor);
                EditorUtils.DrawLineOverRect(fullLine, CustomColors.SimpleShadow, 1);
                EditorUtils.DrawLineOverRect(fullLine, CustomColors.SoftShadow, 1);
                EditorUtils.DrawLineOverRect(fullLine, CustomColors.SimpleBright);
                rect.y += 2;
                rect.height = 2;
                if (dragging && !GOdragging && dragIndex == activeIndex)
                {

                }
                else
                {
                    EditorUtils.DrawActiveTabUnder(rect, editorColor);
                }
            }
        }

        internal void DrawScrollBar()
        {
            if (Event.current.type == EventType.MouseDown)
            {
                if (Event.current.mousePosition.y <= 29)
                {
                    Event.current.Use();
                }
            }
            bool repaintAfter = false;
            if (!showScrollBar || InRecoverScreen() || tabs == null || tabs.Count == 0)
            {
                if (toolScrollBarVisible)
                {
                    repaintAfter = true;
                }
                LimitScrollBar();
                toolScrollBarVisible = false;
                if (repaintAfter)
                {
                    Repaint();
                }
                return;
            }
            int historyVar = 36;
            int addVar = 24;
            if (!showHistory)
            {
                historyVar = 0;
            }
            float windowWidth = position.width;
            float scrollbarWidth = windowWidth - addVar;
            float contentWidth = GetTotalTabsWidth();
            if (contentWidth <= 0)
            {
                return;
            }
            float viewportWidth = windowWidth - addVar - historyVar;
            if (contentWidth <= viewportWidth)
            {
                LimitScrollBar();
                toolScrollBarVisible = false;
                toolbarScrollPosition.x = 0;
                return;
            }
            if (!toolScrollBarVisible)
            {
                repaintAfter = true;
            }
            toolScrollBarVisible = true;
            GUILayout.Space(1);
            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            float newScrollPosition = GUILayout.HorizontalScrollbar(
                toolbarScrollPosition.x,
                viewportWidth,
                0f,
                contentWidth,
                GUILayout.Width(scrollbarWidth)
            );
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();
            Rect underRect = GUILayoutUtility.GetLastRect();
            if (!EditorUtils.IsLightSkin())
            {
                underRect.y -= 1;
                EditorUtils.DrawLineOverRect(underRect, CustomColors.MediumShadow, -2);
            }
            else
            {
                EditorUtils.DrawLineOverRect(underRect, CustomColors.SoftShadow, -2);
            }
            toolbarScrollPosition.x = newScrollPosition;
            LimitScrollBar();
            if (repaintAfter)
            {
                Repaint();
            }
        }

        internal void DrawComponentFilterField(float xPosition = 0, float yPosition = 0, bool multi = false)
        {
            if (!showFilterBar)
            {
                return;
            }
            if (Event.current != null)
            {
                bool enterPressed = Event.current.type == EventType.KeyDown && Event.current.keyCode == KeyCode.Return;
                bool escapePressed = Event.current.type == EventType.KeyDown && Event.current.keyCode == KeyCode.Escape;
                if ((enterPressed || escapePressed) && GUI.GetNameOfFocusedControl() == "SearchField")
                {
                    GUI.FocusControl(null);
                    Event.current.Use();
                }
            }
            float MiddlePoint = position.width / 2 - 10;
            float searchWidth = 70;
            float diff = 0;
            float widthPadding = 200;
            if (!showFocusButton)
            {
                widthPadding -= 20;
            }
            if (!showInspectorButton)
            {
                widthPadding -= 20;
            }
            if (!showHierarchyButton)
            {
                widthPadding -= 20;
            }
            if (!showSelectButton)
            {
                widthPadding -= 20;
            }
            if (MiddlePoint < widthPadding)
            {
                diff = widthPadding - MiddlePoint;
                searchWidth -= diff;
                xPosition += diff;

            }
            float xPositionSearch = xPosition + 8;
            if (multi)
            {
                xPositionSearch += 15;
            }
            GUIContent filterButton = CustomGUIContents.FilterOFFContent;
            if (GetActiveTab().filtering)
            {
                filterButton = CustomGUIContents.FilterONContent;
            }
            if (GUI.Button(new Rect(xPositionSearch + searchWidth, yPosition + 1, 12, 20), filterButton, CustomGUIStyles.FilterButtonStyle))
            {
                editingHeader = false;
                GetActiveTab().filtering = !GetActiveTab().filtering;
                if (!GetActiveTab().filtering)
                {
                    GUI.FocusControl(null);
                }
                gameObjectBox.MarkDirtyRepaint();
                DrawCurrentComponentsContainer();
                Repaint();
            }
            Rect fieldRect = new Rect(xPositionSearch, yPosition + 4, searchWidth, 15);
            bool clicked = Event.current.type == EventType.MouseDown && Event.current.button == 0;
            int clickedMode = clicked ? 0 : -1;
            if (clicked)
            {
                editingHeader = false;
                if (fieldRect.Contains(Event.current.mousePosition))
                {
                    if (!GetActiveTab().filtering)
                    {
                        clickedMode = 1;
                    }
                    else
                    {
                        clickedMode = -1;
                    }
                }
            }

            bool isSearchFocused = EditorWindow.focusedWindow == this && GUI.GetNameOfFocusedControl() == "SearchField";
            if (diff < 54)
            {
                EditorGUI.BeginChangeCheck();
                GUI.SetNextControlName("SearchField");
                GUI.enabled = GetActiveTab().filtering;
                GetActiveTab().filterString = EditorGUI.TextField(new Rect(xPositionSearch, yPosition + 4, searchWidth, 16), GetActiveTab().filterString, CustomGUIStyles.FilterStyle);
                if (EditorGUI.EndChangeCheck())
                {
                    DrawCurrentComponentsContainer(true);
                    gameObjectBox.MarkDirtyRepaint();
                }
                GUI.Label(new Rect(xPositionSearch + 3, yPosition + 4, 15, 15), CustomGUIContents.SearchButtonImage);
            }
            if (diff < 29 && GetActiveTab().filterString == "" && !isSearchFocused)
            {
                GUI.enabled = GetActiveTab().filtering;
                GUI.Label(new Rect(xPositionSearch + 13, yPosition + 3, searchWidth, 15), "<color=grey>Type</color>", CustomGUIStyles.FilterComponentLabel);
            }
            if (clickedMode == 0)
            {
                if (isSearchFocused)
                {
                    GUI.FocusControl(null);
                    Repaint();
                }
            }
            else if (clickedMode == 1)
            {
                if (!GetActiveTab().filtering)
                {
                    FocusFilterField();
                }
            }

            if (shouldFocusFilter)
            {
                EditorGUI.FocusTextInControl("SearchField");
                shouldFocusFilter = false;

            }

            GUI.enabled = true;
        }
        internal void FocusFilterField()
        {
            if (IsActiveTabValid())
            {
                GUI.FocusControl(null);
                Focus();
                GetActiveTab().filtering = true;
                shouldFocusFilter = true;
                DrawCurrentComponentsContainer(true);
                gameObjectBox.MarkDirtyRepaint();
            }
        }
        internal void ToggleFilterField()
        {
            if (IsActiveTabValid())
            {
                if (GetActiveTab().filtering)
                {
                    GUI.FocusControl(null);
                    DisableFilterField();
                }
                else
                {
                    GetActiveTab().filtering = true;
                    shouldFocusFilter = false;
                    DrawCurrentComponentsContainer(true);
                    gameObjectBox.MarkDirtyRepaint();
                }
                Repaint();
            }
        }
        internal void DisableFilterField()
        {
            if (IsActiveTabValid())
            {
                GetActiveTab().filtering = false;
                shouldFocusFilter = false;
                DrawCurrentComponentsContainer(true);
                gameObjectBox.MarkDirtyRepaint();
            }
        }
        internal void DrawUnderButtons()
        {
            if (!showCollapseTool && !showFilterBar && !showSelectButton && !showFocusButton && !showHierarchyButton && !showInspectorButton)
            {
                if (!IsActiveTabValidMulti())
                {
                    return;
                }
            }
            GUILayout.BeginHorizontal(CustomGUIStyles.ButtonsUpSection, GUILayout.Height(20));
            EditorGUILayout.Space(1);
            GUILayout.EndHorizontal();
            Rect lastRect = GUILayoutUtility.GetLastRect();
            EditorUtils.DrawLineUnderRect(lastRect, CustomColors.DefaultInspector, -1);
            EditorUtils.DrawLineOverRect(lastRect, CustomColors.DefaultInspector, 8, 15);
            GUIStyle toolbarButtonStyle = CustomGUIStyles.ButtonsUpRight;
            Rect inspectRect;
            Rect hierarchyRect;
            Rect selectRect;
            Rect focusRect;
            bool popup = false;
            if (EditorUtils.IsShiftOrAltHeldInWindow() && lastRect.height > 1)
            {
                Vector2 mousePos = Event.current.mousePosition;
                popup = mousePos.y < lastRect.yMax + 5 && mousePos.y > lastRect.yMax - 25 && mousePos.x > position.width / 1.5f;
            }
            float yPosition = lastRect.y - 11;
            if (GetActiveTab().IsAPrefabTab())
            {
                yPosition += 2;
            }

            float buttonWidth = 20;
            float buttonHeight = 20;
            float xPosition = this.position.width - buttonWidth - 5;
            int toAdd = 0;
            int toAddToInspector = 0;
            int invisible = 0;
            if (popup && showInspectorButton)
            {
                toAdd += 12;
                toAddToInspector += 12;
            }
            hierarchyRect = new Rect(xPosition, yPosition, buttonWidth, buttonHeight);
            if (!showHierarchyButton)
            {
                toAdd -= 20;
                invisible++;
            }
            inspectRect = new Rect(xPosition - buttonWidth - toAdd, yPosition, buttonWidth + toAddToInspector, buttonHeight);
            if (!showInspectorButton)
            {
                toAdd -= 20;
                invisible++;
            }
            focusRect = new Rect(xPosition - buttonWidth * 2 - toAdd, yPosition, buttonWidth, buttonHeight);
            if (!showFocusButton)
            {
                toAdd -= 20;
                invisible++;
            }
            selectRect = new Rect(xPosition - buttonWidth * 3 - toAdd, yPosition, buttonWidth, buttonHeight);
            if (!showSelectButton)
            {
                invisible++;
            }
            float totalWidth = hierarchyRect.width + inspectRect.width + focusRect.width + selectRect.width;
            totalWidth -= invisible * 20;

            DrawComponentFilterField(position.width - totalWidth - 100, yPosition - 1);
            GUI.enabled = true;
            GUIContent focusContent = CustomGUIContents.FocusContent;
            if (showFocusButton)
            {
                if (GUI.Button(focusRect, focusContent, toolbarButtonStyle))
                {
                    ScrollToActiveTab();
                    if (Event.current.button == 2)
                    {
                        return;
                    }
                    if (IsActiveTabValidMulti())
                    {
                        if (Event.current.button == 0)
                        {
                            EditorUtils.FocusOnSceneView(GetActiveTab().targets);
                        }
                        else
                        {
                            EditorGUIUtility.PingObject(GetActiveTab().targets[0]);
                        }
                    }
                    else
                    {
                        if (Event.current.button == 0)
                        {
                            EditorUtils.FocusOnSceneView(GetActiveTab().target);
                        }
                        else
                        {
                            EditorGUIUtility.PingObject(GetActiveTab().target);
                        }
                    }
                }
            }
            if (!IsActiveTabValidMulti() && IsAlreadySelected(new GameObject[1] { GetActiveTab().target }))
            {
                GUI.enabled = false;
            }
            else if (IsActiveTabValidMulti() && IsAlreadySelected(GetActiveTab().targets))
            {
                GUI.enabled = false;
            }
            if (showSelectButton)
            {
                if (GUI.Button(selectRect, CustomGUIContents.GetSelectContent(!GUI.enabled), toolbarButtonStyle))
                {
                    ScrollToActiveTab();
                    if (IsActiveTabValidMulti())
                    {
                        ignoreSelection = GetActiveTab().targets;
                        Selection.objects = GetActiveTab().targets;
                    }
                    else
                    {
                        ignoreSelection = new GameObject[] { GetActiveTab().target };
                        Selection.objects = ignoreSelection;
                    }
                    if (Event.current.button != 0)
                    {
                        EditorUtils.FocusOnSceneView(Selection.gameObjects);
                    }
                }
            }
            GUI.enabled = true;
            GUIContent editContent = CustomGUIContents.EditContentDefault;
            if (popup)
            {
                toolbarButtonStyle = CustomGUIStyles.ButtonsUpRight_Wide;
                editContent = CustomGUIContents.EditContentPopup;
            }
            if (showInspectorButton)
            {
                if (GUI.Button(inspectRect, editContent, toolbarButtonStyle))
                {
                    if (IsActiveTabValidMulti())
                    {
                        PopUpInspectorWindow(GetActiveTab().targets, popup);
                    }
                    else
                    {
                        PopUpInspectorWindow(new GameObject[] { GetActiveTab().target }, popup);
                    }
                }
            }
            toolbarButtonStyle = CustomGUIStyles.ButtonsUpRight;
            GUI.enabled = !GetActiveTab().newTab && !GetActiveTab().IsValidMultiTarget();
            if (showHierarchyButton)
            {
                if (GUI.Button(hierarchyRect, CustomGUIContents.HierarchyContent, toolbarButtonStyle))
                {
                    HierarchyPopup.ShowWindow(GetActiveTab().target, this, clickMousePosition);
                }
            }
            GUI.enabled = true;
            if (showCollapseTool)
            {
                float middleX = this.position.width / 2 - 24 / 2;
                Rect middleRect = new Rect(middleX, yPosition + 4, 35, 12);
                Color color = GUI.color;
                bool allCollapsed = GetActiveTab().AreAllCollapsed();
                if (allCollapsed)
                {
                    if (EditorUtils.IsLightSkin())
                    {
                        GUI.color += CustomColors.AllCollapsed * 2;
                    }
                    else
                    {
                        GUI.color += CustomColors.AllCollapsed;
                    }
                }
                else
                {
                    if (!EditorUtils.IsLightSkin())
                    {

                        GUI.color -= CustomColors.NotAllCollapsed;
                    }
                }
                if (GUI.Button(middleRect, CustomGUIContents.GetExpandCollapseContent(allCollapsed), CustomGUIStyles.ExpandButtonStyle))
                {
                    if (Event.current.button == 0 && !allCollapsed)
                    {
                        SetAllComponentsTo(false);
                    }
                    else if (Event.current.button == 1 || allCollapsed)
                    {
                        SetAllComponentsTo(true);
                    }
                }
                GUI.color = color;
                middleRect.x += 1;
                middleRect.width -= 2;
                middleRect.height = 10;
                middleRect.y += 1;
                EditorUtils.DrawLineOverRect(middleRect, CustomColors.HarderBright);
            }
            if (IsActiveTabValidMulti())
            {
                GUIContent foldoutContent = CustomGUIContents.FoldedFoldout;
                if (GetActiveTab().multiFoldout)
                {
                    foldoutContent = CustomGUIContents.UnfoldedFoldout;
                }
                if (GUI.Button(new Rect(4, yPosition + 4, 80, 22), foldoutContent, GUIStyle.none))
                {
                    GetActiveTab().multiFoldout = !GetActiveTab().multiFoldout;
                }
            }
        }

        internal void HandlePendingTabDeletion()
        {

            if (tabs == null || tabs.Count == 0)
            {
                return;
            }
            if ((FloatingTab.isClosing || FloatingTab.isOpening) && (FloatingTab.linkedTab == null))
            {
                FloatingTab.isClosing = false;
                FloatingTab.isOpening = false;
            }
            if (FloatingTab.isClosing && FloatingTab.linkedTab != null && FloatingTab.linkedTab.markForDeletion && Event.current.type == EventType.Layout)
            {
                FloatingTab.isClosing = false;
                TabInfo tabToClose = FloatingTab.linkedTab;
                rootVisualElement.schedule.Execute(() =>
                {
                    DoCloseTab(tabToClose);
                });
            }
        }
        void DoDrawHeader(Editor editor)
        {
            if (EditorUtils.CanDrawHeader())
            {
                //editor.DrawHeader();
                Reflected.OnHeaderGUI(editor);
            }
            else
            {
                Reflected.OnHeaderGUI(editor);
                //EditorUtils.HeaderMessage();
            }
        }

        internal void DrawHeader()
        {
            Rect limit = new Rect(68, 8, position.width - 148, 19);
            if (Event.current.type == EventType.MouseDown)
            {
                if (editingHeader && editingHeaderID == EditorGUIUtility.keyboardControl && !limit.Contains(Event.current.mousePosition))
                {
                    EditorGUIUtility.keyboardControl = 0;
                    rootVisualElement.MarkDirtyRepaint();
                }
            }
            else if (Event.current.type == EventType.MouseUp)
            {
                if (limit.Contains(Event.current.mousePosition))
                {
                    if (EditorGUIUtility.editingTextField)
                    {
                        editingHeader = true;
                        editingHeaderID = EditorGUIUtility.keyboardControl;
                    }
                }
            }
            if (editingHeader && !Reflected.IsValidGUIContext() && Event.current.type == EventType.Layout)
            {
                GUILayout.Space(headerBox.worldBound.height);
                return;
            }
            if (!GetActiveTab().multiEditMode)
            {
                if (gameObjectEditor != null && gameObjectEditor.target != null && gameObjectEditor.serializedObject != null && GetActiveTab().target)
                {
                    DoDrawHeader(gameObjectEditor);
                    DrawUnderButtons();
                }
            }
            else
            {
                if (gameObjectEditor != null && GetActiveTab().targets != null && gameObjectEditor.targets != null && gameObjectEditor.serializedObject != null)
                {
                    DoDrawHeader(gameObjectEditor);
                    DrawUnderButtons();
                }
            }
        }



        void HandleTabDoubleClick(TabInfo tab)
        {
            if (doubleClickMode == 0)
            {
                tab.locked = !tab.locked;
                UpdateAllWidths();
            }
            else if (doubleClickMode == 1)
            {
                if (tab.IsValidMultiTarget())
                {
                    Selection.objects = tab.targets;
                }
                else
                {
                    Selection.activeGameObject = tab.target;
                }
            }
            else if (doubleClickMode == 2)
            {
                if (tab.IsValidMultiTarget())
                {
                    EditorUtils.FocusOnSceneView(tab.targets);
                }
                else
                {
                    EditorUtils.FocusOnSceneView(tab.target);
                }
            }
            else if (doubleClickMode == 3 && tab.target && !tab.newTab && !tab.IsValidMultiTarget())
            {
                HierarchyPopup.ShowWindow(tab.target, this, clickMousePosition);
            }
        }

        void AutoScrollOnDrag()
        {
            int extendedX = 40;
            if (!showHistory)
            {
                extendedX = 0;
            }
            if (!dragging && DragAndDrop.objectReferences.Length > 0)
            {
                Rect leftRect = new Rect(0, 0, 30 + extendedX, 42);
                Rect rightRect = new Rect(position.width - 50, 0, 50, 42);
                if (leftRect.Contains(Event.current.mousePosition))
                {
                    if (toolbarScrollPosition.x > 0)
                    {
                        toolbarScrollPosition.x -= 2;
                        LimitScrollBar();
                    }
                }
                else if (rightRect.Contains(Event.current.mousePosition))
                {
                    toolbarScrollPosition.x += 2;
                    LimitScrollBar();
                }
            }
        }

        private void DrawUnderMultiObjectHeader(Rect headerRect)
        {
            if (EditorUtils.IsLightSkin())
            {
                EditorUtils.DrawLineUnderRect(headerRect);
            }
            else
            {
                EditorUtils.DrawLineUnderRect(headerRect, CustomColors.MediumShadow);
            }
            int selectedObjectCount = GetActiveTab().targets.Length;
            Rect foldoutRect = new Rect(headerRect.x + 6, headerRect.y + headerRect.height - 17, 15, 20);
            GUIContent countContent = CustomGUIContents.SelectionContent(selectedObjectCount);
            GUIStyle italicStyle = CustomGUIStyles.ItalicStyle;
            Rect _labelRect = new Rect(foldoutRect.xMax, foldoutRect.y - 3, italicStyle.CalcSize(countContent).x, foldoutRect.height);
            GUI.Label(_labelRect, countContent, italicStyle);
            foldoutRect.width += _labelRect.width + 30;
            GUIContent foldoutContent = CustomGUIContents.FoldedFoldout;
            if (GetActiveTab().multiFoldout)
            {
                foldoutContent = CustomGUIContents.UnfoldedFoldout;
            }
            if (GUI.Button(foldoutRect, "", GUIStyle.none))
            {
                GetActiveTab().multiFoldout = !GetActiveTab().multiFoldout;
            }
            if (GetActiveTab().multiFoldout)
            {
                EditorGUILayout.BeginVertical();
                EditorGUILayout.BeginHorizontal();
                GUIStyle labelStyle = CustomGUIStyles.MultiFoldoutStyle;
                float width = CustomGUIStyles.RichMiniLabel.CalcSize(CustomGUIContents.MultiSelectingContent).x + 10;
                if (EditorUtils.IsLightSkin())
                {
                    GUILayout.Label("Selecting:", CustomGUIStyles.RichMiniLabel, GUILayout.Width(width));
                }
                else
                {
                    GUILayout.Label("<b>Selecting:</b>", CustomGUIStyles.RichMiniLabel, GUILayout.Width(width));
                }
                float currentWidth = width;
                float maxWidth = position.width - 35;
                for (int i = 0; i < selectedObjectCount; i++)
                {
                    GameObject go = GetActiveTab().targets[i];
                    GUIContent goContent = CustomGUIContents.MultiSelectionContent(GetActiveTab().targets, i);
                    Vector2 size = labelStyle.CalcSize(goContent);
                    if (currentWidth + size.x > maxWidth)
                    {
                        GUILayout.FlexibleSpace();
                        EditorGUILayout.EndHorizontal();
                        EditorGUILayout.BeginHorizontal();
                        currentWidth = 0;
                    }
                    Rect labelRect = GUILayoutUtility.GetRect(goContent, labelStyle, GUILayout.Width(size.x));
                    if (!middleScrolling && !resizingAssetView)
                        EditorGUIUtility.AddCursorRect(labelRect, MouseCursor.Link);
                    if (GUI.Button(labelRect, goContent, labelStyle))
                    {
                        if (Event.current.button == 0)
                        {
                            EditorApplication.delayCall += () =>
                            {
                                SetTargetGameObject(go);
                            };
                        }
                        if (Event.current.button == 2)
                        {
                            EditorApplication.delayCall += () =>
                            {
                                AddTabNext(go);
                            };
                        }
                        else if (Event.current.button == 1)
                        {
                            GenericMenu menu = new GenericMenu();
                            menu.AddItem(new GUIContent("Set as Target"), false, () =>
                            {
                                Selection.activeGameObject = go;
                            });
                            menu.AddItem(new GUIContent("Open in new Tab"), false, () =>
                            {
                                AddTabNext(go);
                                Repaint();
                            });
                            menu.AddSeparator("");
                            menu.AddItem(new GUIContent("Ping"), false, () =>
                            {
                                EditorGUIUtility.PingObject(go);
                            });
                            menu.AddItem(new GUIContent("Frame on Scene View"), false, () =>
                            {
                                EditorUtils.FocusOnSceneView(go);
                            });
                            menu.AddItem(new GUIContent("Show In Local Hierarchy View"), false, () =>
                            {
                                HierarchyPopup.ShowWindow(go, this, clickMousePosition);
                            });
                            menu.AddSeparator("");
                            menu.AddItem(new GUIContent("Remove from selection"), false, () =>
                            {
                                List<GameObject> newTargets = GetActiveTab().targets.ToList();
                                newTargets.Remove(go);
                                FocusTab(activeIndex);
                                bool wasLocked = GetActiveTab().locked;
                                GetActiveTab().locked = false;
                                EditorApplication.delayCall += () =>
                                {
                                    if (newTargets.Count == 1)
                                    {
                                        SetTargetGameObject(newTargets[0]);
                                    }
                                    else
                                    {
                                        SetTargetGameObjects(newTargets.ToArray());
                                    }
                                    GetActiveTab().locked = wasLocked;
                                };
                            });
                            menu.ShowAsContext();
                        }
                    }
                    currentWidth += size.x;
                    if (i < selectedObjectCount - 1)
                    {
                        GUIContent plusContent = CustomGUIContents.PlusSymbolContent;
                        Vector2 plusSize = CustomGUIContents.plusSize;
                        Rect plusRect = GUILayoutUtility.GetRect(plusContent, CustomGUIStyles.PlusStyle, GUILayout.Width(plusSize.x));
                        GUI.Label(plusRect, plusContent, CustomGUIStyles.PlusStyle);
                        currentWidth += plusSize.x;
                    }
                }
                GUILayout.FlexibleSpace();
                EditorGUILayout.EndHorizontal();
                EditorGUILayout.EndVertical();
            }
        }

        void RepositionDraggedTab(Rect buttonRect, int i)
        {
            if (dragging && i != dragIndex && !GOdragging)
            {
                Rect fixedRect = new Rect
                {
                    width = totalWidth[dragIndex],
                    x = Event.current.mousePosition.x - FloatingTab.tabDragPoint
                };
                if (FloatingTab.tabRect != null && !EditorUtils.AreRectsOverlapping(buttonRect, fixedRect))
                {
                    return;
                }
                PopUpTip.Hide();
                float correction = 0;
                float tabLeft = fixedRect.x;
                float tabRight = fixedRect.xMax;
                float tabCenter = fixedRect.center.x;
                float targetLeft = buttonRect.x;
                float targetRight = buttonRect.xMax;
                float targetCenter = buttonRect.center.x;
                if (tabRight > targetCenter + correction && tabRight >= targetLeft && tabRight < targetRight && tabCenter < targetCenter)
                {
                    dragTargetIndex = i + 1;
                }
                else if (tabLeft < targetCenter - correction && tabLeft <= targetRight && tabLeft > targetLeft && tabCenter > targetCenter)
                {
                    dragTargetIndex = i;
                }
            }
        }

        void DragTabLogic(Rect fixedButtonRect, Rect buttonRect, int i, int click, bool ignoreHover)
        {
            if (!dragging && i != dragIndex && tabs.Count > 1)
            {
                dragIndex = i;
                if (PopUpTip.waitingToOpen)
                {
                    PopUpTip.Hide();
                }
                dragRect = fixedButtonRect;
                dragTargetIndex = i;
            }
            if (dragging && i != dragIndex && !GOdragging)
            {

            }
            else if (dragging && i == dragIndex && !GOdragging)
            {
                dragTargetIndex = -1;
            }
            else if (!dragging && !FloatingTab.isClosing)
            {
                Rect _lastRect = buttonRect;
                Rect rect1 = new Rect(_lastRect.x, _lastRect.y + 25, 100, 30);
                if (showHistory)
                {
                    rect1.x += 44;
                }
                string text = tabs[i].name;
                if (click == activeIndex)
                {
                    text = "";
                }
                if (!tabs[click].newTab && DragAndDrop.objectReferences != null && DragAndDrop.objectReferences.Length > 0 && activeIndex != click)
                {
                    if (pendingTabSwitch == -1 || pendingTabSwitch != click)
                    {
                        lastTabClick = Time.realtimeSinceStartup;
                        pendingTabSwitch = click;
                        RepaintFor(0.4f);
                        DragAndDrop.visualMode = DragAndDropVisualMode.Move;
                    }
                }
                else if (pendingTabSwitch != -1)
                {
                    pendingTabSwitch = -1;
                }
                else
                {
                    if (ignoreHover)
                    {
                        PopUpTip.Hide();
                    }
                    else if (showTabTree && !tabs[click].newTab && !tabs[click].multiEditMode)
                    {
                        text = CustomGUIContents.GetSingleHoverName(tabs[click].target);
                    }
                    else if (tabs[click].multiEditMode)
                    {
                        text = CustomGUIContents.GetMultiHoverName(tabs[click].targets);
                    }
                }
                rect1.x -= toolbarScrollPosition.x;
                if (tabs[click].multiEditMode)
                {
                    PopUpTip.ShowMulti(text, rect1, this);
                }
                else
                {
                    PopUpTip.Show(text, rect1, this);
                }
            }
        }


        private void DrawAddComponentBar(Color backgroundColor)
        {
            EditorGUILayout.BeginVertical(CustomGUIStyles.InspectorSectionStyle, GUILayout.Height(36));
            GUILayout.Space(5);
            GUI.backgroundColor = backgroundColor;
            EditorGUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            GUI.enabled = !IsActiveTabNew();
            if (GUILayout.Button("Add Component", GUILayout.Height(25), GUILayout.Width(180)))
            {
                float xPos = position.width / 2 - addComponentVector.x / 2;
                float yPos = 5;
                Rect rect = new Rect(xPos, yPos, addComponentVector.x, 0);
                UnityEngine.GameObject[] objects = GetActiveTab().multiEditMode ? GetActiveTab().targets : new GameObject[] { GetActiveTab().target };
                Reflected.ShowAddComponentWindow(rect, objects);
                Repaint();
                GUIUtility.ExitGUI();
            }
            GUI.enabled = true;
            Rect rect1 = GUILayoutUtility.GetLastRect();

            if (!hideUpdateAlert && UpdateChecker.IsUpdateAvailable && position.width > 322)
            {
                Rect buttonRect = new Rect(5, rect1.y + 2, 54, 21);
                if (GUI.Button(buttonRect, CustomGUIContents.UpdateContent, GUIStyle.none))
                {
                    UnityEditor.PackageManager.UI.Window.Open("278831");
                }
                GUI.Label(new Rect(buttonRect.x + 20, buttonRect.y - 2, 54, 21), UpdateChecker.LatestVersion, CustomGUIStyles.MiniLabel);
                if (!middleScrolling)
                {
                    EditorGUIUtility.AddCursorRect(buttonRect, MouseCursor.Link);
                }
            }
            Rect rect2 = new Rect(rect1.x + 1, rect1.y, rect1.width - 2, rect1.height);
            EditorUtils.DrawLineOverRect(rect2, CustomColors.HarderBright, -1);
            EditorUtils.DrawLineOverRect(rect2, CustomColors.SubtleBright, -1, 4);
            EditorUtils.DrawLineOverRect(rect2, CustomColors.SubtleBright, -1, 8);
            EditorUtils.DrawRectBorder(rect1, CustomColors.SimpleShadow, 1);
            if (assetInspection)
            {
                DrawAssetHistoryButton();
            }
            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.EndVertical();
            Rect addRect = EditorUtils.GetLastLineRect();
            EditorUtils.DrawLineOverRect(addRect, CustomColors.HardShadow, 0);
            EditorUtils.DrawLineOverRect(addRect, CustomColors.HarderBright, -1);
            EditorUtils.DrawLineUnderRect(addRect, CustomColors.SimpleShadow, -1);
        }
        private void DrawRecoverScreen()
        {
            GUILayout.Space(10);
            GUIStyle labelStyle = CustomGUIStyles.CenterLabel;
            Rect rect = GUILayoutUtility.GetLastRect();
            rect.x += 20;
            rect.width = position.width - 40;
            rect.y += 5;
            int numberTabs = lastSessionData.tabs.Count;
            string tabsText = " Tabs";
            if (numberTabs == 1)
            {
                tabsText = " Tab";
            }
            GUIContent welcomeMessage = CustomGUIContents.WelcomeMessage(numberTabs + tabsText);
            rect.height = labelStyle.CalcHeight(welcomeMessage, rect.width) + 15;
            EditorGUILayout.BeginHorizontal();
            GUIContent content = welcomeMessage;
            string tabs = "";
            if (lastSessionData.tabs != null && lastSessionData.tabs.Count > 0)
            {
                if (lastSessionData.GetSaveTime() != default)
                {
                    tabs += "   " + lastSessionData.lastSaveTimePrint + "   \nTabs:\n";
                }
                for (int i = 0; i < lastSessionData.tabs.Count; i++)
                {
                    TabInfo tab = lastSessionData.tabs[i];
                    if (i != 0)
                    {
                        tabs += "\n";
                    }
                    if (tab.newTab)
                    {
                        tabs += "   " + (i + 1) + ".  " + "*New Tab" + "   ";
                    }
                    else
                    {
                        tabs += "   " + (i + 1) + ".  " + tab.name + "   ";
                    }
                }
            }


            content.tooltip = tabs;
            CustomGUIStyles.HelpBox(content, true);
            EditorGUILayout.EndHorizontal();
            Rect rect1 = GUILayoutUtility.GetLastRect();
            if (rect1.Contains(Event.current.mousePosition))
            {
                EditorGUIUtility.AddCursorRect(rect1, MouseCursor.Link);
            }
            GUILayout.Space(15);
            EditorGUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            Color color = GUI.backgroundColor;
            GUI.backgroundColor += CustomColors.WelcomeBackColor;
            if (EditorUtils.IsLightSkin())
            {
                GUI.backgroundColor = Color.blue / 3;
            }
            if (GUILayout.Button("Restore!", GUILayout.Width(90), GUILayout.Height(25)))
            {
                sessionsMode = 0;
                justOpened = false;
                RestoreSession();
                ScrollToIndex(activeIndex);
            }
            CustomGUIContents.DrawCustomButton(true);
            GUI.backgroundColor = color;
            if (GUILayout.Button("Don't restore", GUILayout.Width(100), GUILayout.Height(25)))
            {
                lastSessionData.checkingToLoad = false;
                sessionsMode = 0;
                lastSessionData = null;
            }
            CustomGUIContents.DrawCustomButton(true);
            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();
            GUIStyle foldoutStyle = EditorStyles.foldout;
            GUILayout.Space(35);
            EditorGUILayout.BeginHorizontal();
            GUILayout.Space(25);
            showAdditionalOptions = EditorGUILayout.Foldout(showAdditionalOptions, " Additional Options", true, foldoutStyle);
            EditorGUILayout.EndHorizontal();
            Rect rect2 = EditorUtils.GetLastLineRect();
            if (!showAdditionalOptions)
            {
                EditorUtils.DrawLineUnderRect(rect2, 6);
                EditorUtils.DrawLineUnderRect(rect2, CustomColors.HarderBright, 7);
            }
            else
            {
                EditorUtils.DrawLineUnderRect(rect2, CustomColors.HardShadow, 6);
                EditorUtils.DrawLineUnderRect(rect2, CustomColors.SoftShadow, 7);
                EditorUtils.DrawLineUnderRect(rect2, CustomColors.VerySoftShadow, 8, 3);
                EditorUtils.DrawLineUnderRect(rect2, CustomColors.VerySoftShadow, 8, 89);
                GUILayout.Space(25);
                EditorGUILayout.BeginHorizontal();
                GUILayout.FlexibleSpace();
                if (GUILayout.Button("Always restore", GUILayout.Height(22), GUILayout.Width(100)))
                {
                    if (EditorUtility.DisplayDialog("Are you sure?", "CoInspector will automatically continue your previous Sessions without asking.", "Yes!", "Oops, that sounds annoying"))
                    {
                        sessionsMode = 1;
                        SaveSettings();
                        RestoreSession();
                        ScrollToIndex(activeIndex);
                        justOpened = false;
                    }
                }
                CustomGUIContents.DrawCustomButton(true);
                GUI.backgroundColor += CustomColors.WelcomeSubBackColor;
                if (EditorUtils.IsLightSkin())
                {
                    GUI.backgroundColor -= CustomColors.LightSkinRed;
                }
                if (GUILayout.Button("Don't ask again", GUILayout.Height(22), GUILayout.Width(105)))
                {
                    if (EditorUtility.DisplayDialog("Are you sure?", "This will completely disable the Sessions feature until enabled again.", "Yeah, shut up already", "No"))
                    {
                        sessionsMode = 2;
                        lastSessionData.checkingToLoad = false;
                        rememberSessions = false;
                        lastSessionData = null;
                        SaveSettings();
                    }
                }
                CustomGUIContents.DrawCustomButton(true);
                GUI.backgroundColor = color;
                GUILayout.FlexibleSpace();
                EditorGUILayout.EndHorizontal();
                GUILayout.Space(5);
                EditorGUILayout.LabelField("<b>*</b><i>You can always change it in the Settings!</i>", CustomGUIStyles.CenterLabel);
                rect2 = EditorUtils.GetLastLineRect();
                EditorUtils.DrawLineUnderRect(rect2, 6);
                EditorUtils.DrawLineUnderRect(rect2, CustomColors.HarderBright, 7);
            }
            GUILayout.Space(10);
        }

        float GetCurrentWidth()
        {
            return this.position.width;
        }

        private void DrawLastClickedSection()
        {
            if (lastClicked != null && lastClicked.Count > 0)
            {
                //float width = position.width - 55;
                float width = GetCurrentWidth() - 60;
                if (lastClicked.Count < 5)
                {
                    width = width / lastClicked.Count;
                }
                else
                {
                    width = width / 5;
                }
                GUILayout.BeginHorizontal(GUILayout.Height(10));
                GUILayout.Space(17);

                bool flag = EditorGUILayout.Foldout(showLastClicked, " Last Clicked", true, CustomGUIStyles.BoldFoldoutStyle);
                if (!hoveringResize)
                {
                    showLastClicked = flag;
                }
                GUILayout.EndHorizontal();
                if (!showLastClicked)
                {
                    GUILayout.Space(5);
                    GUILayout.BeginHorizontal();
                    GUILayout.Space(0);
                    GUILayout.EndHorizontal();
                    Rect rect = GUILayoutUtility.GetLastRect();
                    rect.width = GetCurrentWidth() - 10;
                    rect.x = 5;
                    EditorUtils.DrawLineUnderRect(rect, CustomColors.HardShadow);
                    EditorUtils.DrawLineUnderRect(rect, CustomColors.HarderBright, 1);
                    return;
                }
                GUILayout.BeginVertical(CustomGUIStyles.BoxStyle);
                GUILayout.Space(5);
                GUILayout.BeginHorizontal();
                for (int i = 0; i < Mathf.Min(lastClicked.Count, 5); i++)
                {
                    DrawGameObjectEntry(lastClicked[i], width, 1, i);
                    GUILayout.FlexibleSpace();
                }
                GUILayout.EndHorizontal();
                if (lastClicked.Count > 5)
                {
                    GUILayout.Space(5);
                    GUILayout.BeginHorizontal();
                    for (int i = 5; i < Mathf.Min(lastClicked.Count, 10); i++)
                    {
                        DrawGameObjectEntry(lastClicked[i], width, 1, i);
                        GUILayout.FlexibleSpace();
                    }
                    int remaining = 10 - lastClicked.Count;
                    if (remaining > 0)
                    {
                        for (int i = 0; i < remaining; i++)
                        {
                            GUILayout.Space(width);
                            GUILayout.FlexibleSpace();
                        }
                    }
                    GUILayout.EndHorizontal();
                }
                GUILayout.EndVertical();
                EditorUtils.DrawRectBorder(GUILayoutUtility.GetLastRect(), CustomColors.MediumShadow, 1);
                EditorUtils.DrawLineUnderRect(CustomColors.SimpleBright, 0);
            }
        }

        void RemoveFromMostClicked(List<GameObject> gos)
        {
            if (tracker == null)
            {
                return;
            }
            tracker.RemoveFromMost(gos);
        }

        void RemoveFromLastClicked(List<GameObject> gos)
        {
            if (tracker == null)
            {
                return;
            }
            tracker.RemoveFromLast(gos);
        }

        private void DrawMostClickedSection()
        {
            if (mostClicked != null && mostClicked.Count > 0)
            {
                float width = GetCurrentWidth() - 60;
                if (mostClicked.Count < 5)
                {
                    width = width / mostClicked.Count;
                }
                else
                {
                    width = width / 5;
                }
                GUILayout.Space(10);
                GUILayout.BeginHorizontal(GUILayout.Height(20));
                GUILayout.Space(17);

                bool flag = EditorGUILayout.Foldout(showMostClicked, " Most Clicked", true, CustomGUIStyles.BoldFoldoutStyle);
                {
                    showMostClicked = flag;
                }
                GUI.enabled = true;
                GUILayout.EndHorizontal();
                if (!showMostClicked)
                {
                    GUILayout.Space(5);
                    GUILayout.BeginHorizontal();
                    GUILayout.Space(0);
                    GUILayout.EndHorizontal();
                    Rect rect = GUILayoutUtility.GetLastRect();
                    rect.width = GetCurrentWidth() - 10;
                    rect.x = 5;
                    EditorUtils.DrawLineUnderRect(rect, CustomColors.HardShadow);
                    EditorUtils.DrawLineUnderRect(rect, CustomColors.HarderBright, 1);
                    return;
                }
                GUILayout.BeginVertical(CustomGUIStyles.BoxStyle);
                GUILayout.Space(5);
                GUILayout.BeginHorizontal();

                for (int i = 0; i < Mathf.Min(mostClicked.Count, 5); i++)
                {
                    DrawGameObjectEntry(mostClicked[i], width, 2, i);
                    GUILayout.FlexibleSpace();
                }
                GUILayout.EndHorizontal();
                if (mostClicked.Count > 5)
                {
                    GUILayout.Space(5);
                    GUILayout.BeginHorizontal();
                    for (int i = 5; i < Mathf.Min(mostClicked.Count, 10); i++)
                    {
                        DrawGameObjectEntry(mostClicked[i], width, 2, i);
                        GUILayout.FlexibleSpace();
                    }
                    int remaining = 10 - mostClicked.Count;
                    if (remaining > 0)
                    {
                        for (int i = 0; i < remaining; i++)
                        {
                            GUILayout.Space(width);
                            GUILayout.FlexibleSpace();
                        }
                    }
                    GUILayout.EndHorizontal();
                }
                GUILayout.EndVertical();
                EditorUtils.DrawRectBorder(GUILayoutUtility.GetLastRect(), CustomColors.MediumShadow, 1);
                EditorUtils.DrawLineUnderRect(CustomColors.SimpleBright, 0);
            }
        }

        private void DrawGameObjectEntry(List<GameObject> entry, float width, int mode, int index)
        {
            GUILayout.BeginVertical(GUILayout.Width(width));
            GUIContent content;
            if (mode == 1)
            {
                content = tracker.GetContentForLast(index);
            }
            else if (mode == 2)
            {
                content = tracker.GetContentForMost(index);
            }
            else
            {
                content = CustomGUIContents.EmptyContent;
            }
            if (content == null)
            {
                content = CustomGUIContents.EmptyContent;
            }
            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            Color color = GUI.backgroundColor;
            if (!EditorUtils.IsLightSkin())
            {
                GUI.backgroundColor = CustomColors.NewTabButton;
            }
            else
            {
                GUI.backgroundColor -= CustomColors.SoftShadow;
            }
            if (GUILayout.Button(content, CustomGUIStyles.CustomButtonStyle))
            {
                if (Event.current.button == 0)
                {
                    if (entry.Count == 1)
                    {
                        SetTargetGameObject(entry[0]);
                        EditorUtils.AutoFocusOnSceneView(entry[0]);
                    }
                    else
                    {
                        GameObject[] _targets = entry.ToArray();
                        SetTargetGameObjects(_targets);
                        EditorUtils.AutoFocusOnSceneView(_targets);
                    }
                    RepaintForAWhile();
                    FocusTab(activeIndex);
                }
                else if (Event.current.button == 1)
                {
                    GenericMenu menu = new GenericMenu();
                    menu.AddItem(new GUIContent("Open in new Tab"), false, () =>
                    {
                        if (entry.Count == 1)
                        {
                            AddTabNext(entry[0]);
                        }
                        else
                        {
                            GameObject[] _targets = entry.ToArray();
                            AddMultiTabNext(_targets);
                        }
                        Repaint();
                    });
                    menu.AddItem(new GUIContent("Select"), false, () =>
                    {
                        if (entry.Count == 1)
                        {
                            SetTargetGameObject(entry[0]);
                            EditorUtils.AutoFocusOnSceneView(entry[0]);
                        }
                        else
                        {
                            GameObject[] _targets = entry.ToArray();
                            SetTargetGameObjects(_targets);
                            EditorUtils.AutoFocusOnSceneView(_targets);
                        }
                        RepaintForAWhile();
                        FocusTab(activeIndex);
                    });
                    menu.AddSeparator("");
                    menu.AddItem(new GUIContent("Ping"), false, () =>
                    {
                        EditorGUIUtility.PingObject(entry[0]);
                    });
                    menu.AddItem(new GUIContent("Frame on Scene View"), false, () =>
                    {
                        if (entry.Count == 1)
                        {
                            EditorUtils.FocusOnSceneView(entry[0]);
                        }
                        else
                        {
                            EditorUtils.FocusOnSceneView(entry.ToArray());
                        }
                    });
                    if (entry.Count == 1)
                    {
                        menu.AddItem(new GUIContent("Show In Local Hierarchy View"), false, () =>
                        {
                            HierarchyPopup.ShowWindow(entry[0], this, clickMousePosition);

                        });
                    }
                    else
                    {
                        menu.AddDisabledItem(new GUIContent("Show In Local Hierarchy View"));

                    }
                    menu.AddSeparator("");
                    menu.AddItem(new GUIContent("Remove from List"), false, () =>
                    {
                        if (mode == 1)
                        {
                            RemoveFromLastClicked(entry);
                        }
                        else
                        {
                            RemoveFromMostClicked(entry);
                        }
                        ReadLastClicked();
                    });
                    menu.ShowAsContext();
                }
                else if (Event.current.button == 2)
                {
                    EndMiddleScrolling();
                    if (entry.Count == 1)
                    {
                        AddTabNext(entry[0]);

                    }
                    else
                    {
                        GameObject[] _targets = entry.ToArray();
                        AddMultiTabNext(_targets);
                    }
                    Repaint();
                }
            }
            GUI.backgroundColor = color;
            Rect rect = GUILayoutUtility.GetLastRect();
            CustomGUIContents.DrawCustomButton(rect);
            if (!middleScrolling && !resizingAssetView)
                EditorGUIUtility.AddCursorRect(rect, MouseCursor.Link);
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();
            if (entry.Count == 1)
            {
                GUILayout.Label(entry[0].name, CustomGUIStyles.ObjectListLabel);
            }
            else
            {
                GUIContent content2 = CustomGUIContents.EmptyContent;
                content2.text = $"({entry.Count}) Objects";
                content2.tooltip = string.Join("\n", entry.ConvertAll(go => go.name));
                GUILayout.Label(content2, CustomGUIStyles.ObjectListLabel);
            }
            GUILayout.EndVertical();
        }
        internal void UpdateCurrentTip()
        {
            ReadLastClicked();
            if (!justOpened)
            {
                SaveSettings();
            }
            currentTip = TipsManager.GetRandomTip();
        }
        internal string GetCurrentTip()
        {
            if (currentTip != null && currentTip.Length > 0)
            {
                return currentTip;
            }
            else return "Select a GameObject to inspect it!";
        }

        private void StartMessageGUI()
        {
            if (tabs == null || tabs.Count == 0 || Event.current == null)
            {
                return;
            }
            GUILayout.Space(10);
            if (!InRecoverScreen())
            {
                DrawLastClickedSection();
                DrawMostClickedSection();
                EditorUtils.DrawTipSection(this);
                justOpened = false;
            }
            else
            {
                DrawRecoverScreen();
            }

        }

        void ShowRecoverSessionDialogue()
        {
            int tabNumber = lastSessionData.tabs.Count;
            string tabNames = "";
            for (int j = 0; j < tabNumber; j++)
            {
                if (lastSessionData.tabs[j].newTab)
                {
                    tabNames += "\n" + (j + 1) + ".  " + "*New Tab";
                }
                else
                {
                    tabNames += "\n" + (j + 1) + ".  " + lastSessionData.tabs[j].name;
                }
            }
            if (EditorUtility.DisplayDialog("Are you sure?", "This will restore " + tabNumber + " Tabs from your Session:\n\n" + lastSessionData.lastSaveTimePrint + "\n" + tabNames, "Yes!", "No"))
            {
                RestoreSession();
            }
        }
        private void DrawTabBar()
        {
            if (Event.current == null)
            {
                return;
            }
            float padding = 0;
            GUIStyle toolbar = CustomGUIStyles.ScrollTabsStyle;
            if (showHistory)
            {
                toolbar = CustomGUIStyles.ScrollTabsHistoryStyle;
                EditorGUILayout.BeginVertical();
                DrawBackNextButtons();
                EditorGUILayout.EndVertical();
                Rect _lineRect = GUILayoutUtility.GetLastRect();
                _lineRect.width = 1;
                _lineRect.height = 23;
                _lineRect.x += 19;
                EditorGUI.DrawRect(_lineRect, CustomColors.SimpleShadow);
                _lineRect.x += 20;
                EditorGUI.DrawRect(_lineRect, CustomColors.SimpleShadow);
                padding = 40;
            }

            Rect rect1 = new Rect(padding, 0, GetTotalTabsWidth() - 4, 3);
            //   EditorGUI.DrawRect(rect1, CustomColors.DarkInspector);
            if (tabs != null && tabs.Count > 1 && dragging)
            {
                if (!EditorUtils.IsLightSkin())
                {
                    EditorGUI.DrawRect(rect1, CustomColors.DarkInspector);
                    rect1.y += 3;
                    rect1.height = 20;
                }
                else
                {
                    rect1.height = 24;
                    EditorUtils.DrawLineOverRect(rect1, CustomColors.HardShadow);
                }
                EditorGUI.DrawRect(rect1, CustomColors.LineColor * 1.4f);

            }
            GUIStyle toolbarButton;
            toolbar.margin.top = 0;
            bool ignoreHover = showHistory && (Event.current.mousePosition.x <= 40);
            GUILayoutOption[] options = CustomGUIStyles.ToolBarOptions(position.width);
            EditorGUILayout.BeginHorizontal(options);
            if (showHistory)
            {
                GUILayout.Space(40);
            }
            GUILayout.BeginScrollView(toolbarScrollPosition, false, false, GUIStyle.none, GUIStyle.none, toolbar, GUILayout.ExpandWidth(true));
            EditorGUILayout.BeginHorizontal();
            Color color = GUI.color;
            List<TabInfo> temp;
            bool recoverScreen = InRecoverScreen();
            if (recoverScreen)
            {
                temp = new List<TabInfo>();
            }
            else
            {
                temp = new List<TabInfo>(tabs);
            }
            bool alreadyDragging = false;
            for (int i = 0; i < temp.Count; i++)
            {
                GUI.color = color;
                var item = temp[i].target;
                toolbarButton = CustomGUIStyles.ToolbarButtonTabs_Active;
                if (temp[i].prefab)
                {
                    toolbarButton = CustomGUIStyles.ToolbarButtonTabsPrefab_Active;
                }
                if (i != activeIndex)
                {
                    GUI.color *= CustomColors.TextPreviewColor;
                    if (!temp[i].prefab)
                    {
                        toolbarButton = CustomGUIStyles.ToolbarButtonTabs;
                        if (!EditorGUIUtility.isProSkin)
                        {
                            toolbarButton = CustomGUIStyles.ToolbarButtonTabs_White;
                        }
                    }
                    else
                    {
                        toolbarButton = CustomGUIStyles.ToolbarButtonTabsPrefab;
                    }
                }
                if (!(tabs.Count - 1 >= i))
                {
                    continue;
                }
                GUIContent inspectorContent = CustomGUIContents.TabContent;
                inspectorContent.text = tabs[i].shortName;
                GUIContent tabImage = CustomGUIContents.TabIconContent;
                tabImage.image = null;
                float size = 16;
                float pad = 4;
                if (item)
                {
                    if (showIcons)
                    {
                        tabImage.image = tabs[i].icon;
                    }

                    if (tabs[i].locked)
                    {
                        tabImage.image = CustomGUIContents.AssetLocked.image as Texture2D;
                        size = 17;
                        pad = 3;
                    }
                }
                else if (tabs[i].IsValidMultiTarget())
                {
                    if (tabs[i].locked)
                    {
                        tabImage.image = CustomGUIContents.AssetLocked.image as Texture2D;
                        size = 17;
                        pad = 3;
                    }
                    else
                    {
                        tabImage.image = CustomGUIContents.TabMulti.image as Texture2D;
                    }
                }
                if (tabImage.image != null && (showIcons || tabs[i].locked))
                {
                    toolbarButton.padding = PaddingIcon;
                }
                else
                {
                    toolbarButton.padding = PaddingNoIcon;
                    tabImage.image = null;
                }
                toolbarButton.fixedHeight = 23;
                int click = i;
                if (!alreadyDragging && dragging && !GOdragging && dragTargetIndex == i)
                {
                    float _width;
                    if (FloatingTab.fallingTab)
                    {
                        _width = totalWidth[FloatingTab.dragIndex];
                    }
                    else
                    {
                        _width = totalWidth[dragIndex];
                    }
                    GUILayout.Space(_width);
                    Rect lineRect = GUILayoutUtility.GetLastRect();
                    lineRect.y += 1;
                    alreadyDragging = true;
                    if (!FloatingTab.fallingTab && dragIndex == activeIndex)
                    {
                        if (showHistory)
                        {
                            lineRect.y -= 1;
                            lineRect.x += 40;
                        }
                        activeTabRect = lineRect;
                    }
                }
                else
                {
                    GUILayout.Space(0);
                }
                if (dragging && dragIndex == i)
                {
                    GUI.color = color;
                    if (!GOdragging)
                    {
                        EditorGUILayout.BeginVertical();
                        GUILayout.Space(0);
                        EditorGUILayout.EndVertical();
                        continue;
                    }
                }
                float width = totalWidth[i];
                toolbarButton.contentOffset = Vector2.zero;
                EditorGUILayout.BeginVertical(GUILayout.Width(width));
                var button = GUILayout.Button(inspectorContent, toolbarButton, GUILayout.Width(width));
                EditorGUILayout.EndVertical();
                toolbarButton.contentOffset = Vector2.zero;
                Rect buttonRect = GUILayoutUtility.GetLastRect();
                Rect fixedButtonRect = new Rect(buttonRect);
                if (showHistory)
                {
                    fixedButtonRect.x += 40;
                }
                fixedButtonRect.x -= toolbarScrollPosition.x;
                bool drawLine = false;
                if (tabs[i].IsValidMultiTarget())
                {
                    if (IsAlreadySelected(tabs[i].targets))
                    {
                        drawLine = true;
                    }
                }
                else if (Selection.gameObjects.Contains(item))
                {
                    drawLine = true;
                }
                tabs[i].isSelected = drawLine;
                if (tabs[i].prefab)
                {
                    EditorUtils.DrawFadeToBottom(buttonRect, CustomColors.FadeBlue, 23);
                }
                else
                {
                    if (EditorUtils.IsLightSkin())
                    {
                        EditorUtils.DrawFadeToBottom(buttonRect, CustomColors.SimpleBright, 12);
                    }
                    else if (i == activeIndex)
                    {
                        EditorUtils.DrawFadeToBottom(buttonRect, CustomColors.SoftBright, 12);
                    }
                }
                if (activeIndex == i)
                {
                    activeTabRect = fixedButtonRect;
                }
                float _pad = 0;
                if (i == tabs.Count - 1)
                {
                    _pad = 1;
                }
                if (drawLine)
                {
                    bool isLast = i == tabs.Count - 1;
                    Rect lineRect = new Rect(buttonRect.x + 1, buttonRect.y, buttonRect.width - _pad - 3, 2);
                    EditorUtils.DrawSelectedLineDecorator(lineRect, CustomColors.HardShadow, activeIndex != i, isLast);
                }
                else if (!(FloatingTab.isClosing && FloatingTab.linkedTab == tabs[i]))
                {
                    bool isLast = i == tabs.Count - 1;
                    Rect lineRect = new Rect(buttonRect.x, buttonRect.y, buttonRect.width, 1);
                    EditorUtils.DrawLineRoundDecorator(lineRect, CustomColors.HarderBright, CustomColors.HardShadow, activeIndex != i, false, isLast);
                }
                Rect rect = new Rect(buttonRect.x + 1, buttonRect.y + pad, size, size);
                if (tabs[i].prefab)
                {
                    GUI.color += Color.blue * 2;
                }
                GUI.enabled = !TabHasDisabledTargets(tabs[i]);
                GUI.Label(rect, tabImage);
                GUI.enabled = true;
                if (tabs[i].prefab)
                {
                    GUI.color -= Color.blue * 2;
                }
                Rect overlayRect = new Rect(buttonRect);
                if (Application.isPlaying)
                {
                    if (activeIndex != i)
                    {
                        EditorGUI.DrawRect(overlayRect, CustomColors.InGameCorrectionColor);
                    }
                    else
                    {
                        EditorGUI.DrawRect(overlayRect, CustomColors.InGameCorrectionColorActive);
                    }
                }
                else
                {
                    if (activeIndex == i)
                    {
                        EditorGUI.DrawRect(overlayRect, CustomColors.InGameCorrectionColorActive);
                    }
                    else
                    {
                        EditorGUI.DrawRect(overlayRect, CustomColors.InGameCorrectionColor);
                    }
                }
                bool hovered = IsLastRectHovered();
                if (hovered)
                {
                    DragTabLogic(fixedButtonRect, buttonRect, i, click, ignoreHover);
                    if (tabs[click].locked && EditorUtils.NotDragging())
                    {
                        float accumulatedWidth = GetTotalTabsWidth(click - 1);
                        Rect lockRect = new Rect(accumulatedWidth - 1, 4, 15, 15);
                        GUI.Button(lockRect, CustomGUIContents.QuickUnlock, CustomGUIStyles.EmptyButton);
                        if (lockRect.Contains(Event.current.mousePosition))
                        {
                            EditorGUI.DrawRect(lockRect, CustomColors.SimpleBright);
                            GUI.Label(rect, tabImage);
                            PopUpTip.Hide();
                        }
                    }
                    if (!EditorUtils.NotDragging())
                    {
                        DragAndDrop.visualMode = DragAndDropVisualMode.Move;
                    }
                }
                else if (pendingTabSwitch == i && Event.current != null && Event.current.type == EventType.Repaint)
                {
                    pendingTabSwitch = -1;
                }
                RepositionDraggedTab(buttonRect, i);
                if (!hovered && ignoreHover)
                {
                    PopUpTip.Hide();
                }
                if (button || pendingTabSwitch == click)
                {
                    dragging = false;
                    if (Event.current.button == 2 && pendingTabSwitch == -1)
                    {
                        if (tabs.Count > 1)
                        {
                            HandlePendingTabDeletion();
                            if (!FloatingTab.isClosing)
                            {
                                CloseTab(click);
                                targetGameObject = GetActiveTab().target;
                            }
                        }
                    }
                    else if (Event.current.button == 1 && pendingTabSwitch == -1)
                    {
                        PopUpTip.Hide();
                        ShowTabContextMenu(buttonRect, i, click, item);
                    }
                    else
                    {
                        if (tabs[click].locked && EditorUtils.NotDragging())
                        {
                            float accumulatedWidth = GetTotalTabsWidth(click - 1);
                            Rect lockRect = new Rect(accumulatedWidth - 1, 4, 15, 15);
                            if (lockRect.Contains(Event.current.mousePosition))
                            {
                                if (item || tabs[click].IsValidMultiTarget())
                                {
                                    tabs[click].locked = !tabs[click].locked;
                                    GUI.color = color;
                                    GUI.enabled = true;
                                    UpdateAllWidths();
                                    continue;
                                }
                            }
                        }
                        if (pendingTabSwitch == -1)
                        {
                            if (Time.realtimeSinceStartup - lastTabClick < 0.3f && lastTabClick != -1)
                            {
                                if ((item || tabs[click].IsValidMultiTarget()) && click == lastClickedTab)
                                {
                                    HandleTabDoubleClick(tabs[click]);
                                }
                            }
                            lastClickedTab = click;
                            lastTabClick = Time.realtimeSinceStartup;
                        }
                        else
                        {
                            if (Time.realtimeSinceStartup - lastTabClick < 0.5f)
                            {
                                GUI.color = color;
                                GUI.enabled = true;
                                continue;
                            }
                            else
                            {
                                lastTabClick = Time.realtimeSinceStartup;
                            }
                        }
                        PopUpTip.Hide();
                        bool keepCount = false;
                        if (activeIndex != click || !softSelection)
                        {
                            SaveFoldoutsToTab();
                            if (click != activeIndex)
                            {
                                keepCount = true;
                                UpdatePreviousTab();
                            }
                            if (pendingTabSwitch != -1)
                            {
                                click = pendingTabSwitch;
                                pendingTabSwitch = -1;
                                if (!softSelection)
                                {
                                    switchingTabs = true;
                                }
                            }
                        }
                        FocusTab(click, switchingTabs, keepCount);
                    }
                }
                GUI.color = color;
                GUI.enabled = true;
            }
            if (!alreadyDragging && dragging && dragTargetIndex == temp.Count && !GOdragging)
            {
                GUILayout.Space(totalWidth[dragIndex]);
            }
            else
            {
                GUILayout.Space(0);
            }

            GUILayout.FlexibleSpace();
            flexibleRect = GUILayoutUtility.GetLastRect();
            if (flexibleRect.x != 0)
            {
                flexibleRect.height = 23;
                Rect drawFlexibleRect = new Rect(flexibleRect);
                drawFlexibleRect.width += 1;
                drawFlexibleRect.x -= 1;
                if (dragging && !GOdragging)
                {
                    if (flexibleRect.Contains(new Vector2(Event.current.mousePosition.x, dragRect.y)))
                    {
                        dragTargetIndex = temp.Count;
                    }
                }
                else if (Event.current.button == 1 && Event.current.type == EventType.MouseDown)
                {
                    if (flexibleRect.x != 0 && flexibleRect.Contains(Event.current.mousePosition))
                    {
                        ShowNoTabContextMenu(flexibleRect);
                    }
                }
                EditorGUI.DrawRect(new Rect(drawFlexibleRect.x + 1f, 0, 1, 23), CustomColors.DefaultInspector);
                EditorGUI.DrawRect(drawFlexibleRect, CustomColors.FlexibleColor);

                float spacing = flexibleRect.width;
                if (flexibleRect.x != 0)
                {
                    barSpacing = spacing;
                }
            }
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.EndScrollView();
            EditorGUILayout.EndHorizontal();
            Rect _scrollRect = GUILayoutUtility.GetLastRect();

            if (EditorUtils.IsLightSkin() && recoverScreen)
            {
                EditorUtils.DrawLineUnderRect(_scrollRect, -3);
                EditorUtils.DrawLineUnderRect(_scrollRect, CustomColors.SimpleBright, -2);
            }
            _scrollRect.width -= barSpacing;
            if (_scrollRect.width > 1)
            {
                scrollRect = _scrollRect;
            }
            DrawAddButton();
            GUILayout.Space(2);
            if (FloatingTab.isClosing || FloatingTab.isOpening)
            {
                Repaint();
            }

        }

        void ShowNoTabContextMenu(Rect flexibleRect)
        {
            GenericMenu menu = new GenericMenu();
            menu.AddItem(new GUIContent("Add New Tab"), false, () =>
            {
                AddTabNext();
            });
            menu.AddSeparator("");
            if (AreThereClosedTabs())
            {
                menu.AddItem(new GUIContent("Restore Closed Tab"), false, () =>
                {
                    RestoreClosedTab();
                });
            }
            else
            {
                menu.AddDisabledItem(new GUIContent("Restore Closed Tab"));
            }
            if (IsThereAPreviousSession())
            {
                menu.AddItem(new GUIContent("Restore Last Saved Session"), false, () =>
                {
                    ShowRecoverSessionDialogue();
                });
            }
            else
            {
                menu.AddDisabledItem(new GUIContent("Restore Last Saved Session"));
            }
            menu.AddSeparator("");
            ShowSettingsMenu(menu, false);
            Rect _rect = new Rect(Event.current.mousePosition.x, flexibleRect.y + 25, 0, 0);
            menu.DropDown(_rect);
        }

        internal void DuplicateTabNext(int click)
        {
            int previousIndex = activeIndex;
            int index = click + 1;
            tabs.Insert(index, new TabInfo(tabs[click]));
            tabs[index].index = index;
            activeIndex = index;
            totalWidth.Add(100);
            FocusTab();
            UpdatePreviousTab(tabs[previousIndex]);
        }

        void UpdatePreviousTab(TabInfo tab = null)
        {
            if (tab == null)
            {
                tab = GetActiveTab();
            }
            if (tab != null)
            {
                previousTab = tab;
            }
        }

        void ResetMaximizedView()
        {
            if (assetOnlyMode)
            {
                bool areAllExpanded = PrefabComponentMapManager.AreAllExpanded();
                assetOnlyMode = false;
                assetsCollapsed = false;
                ReinitializeComponentEditors();
                if (areAllExpanded)
                {
                    PrefabComponentMapManager.SetAllComponentsTo(false);
                }
                ResetAssetViewSize();
            }
        }

        public void AddTabNext(GameObject target = null, bool focus = true)
        {
            ResetMaximizedView();
            tabs.Insert(activeIndex + 1, new TabInfo(target, tabs.Count, this));
            UpdatePreviousTab();
            int index = activeIndex + 1;
            if (focus)
            {
                activeIndex = index;
            }
            TabInfo tab = tabs[index];
            tab.newTab = true;
            totalWidth.Add(100);
            GetActiveTab().scrollPosition = 0;
            if (target)
            {
                tab.newTab = false;
                if (IsGameObjectInPrefabMode(target))
                {
                    tab.prefab = true;
                    tab.history = null;
                }
                UpdateClicked(target);
                EditorUtils.AutoFocusOnSceneView(target);
            }
            tabs[index].scrollPosition = 0;
            if (focus)
            {
                FocusTab(index);
            }
            else
            {
                RefreshAllIcons();
                ScrollToIndex(index, true);
            }
            DrawCurrentComponentsContainer();
            RepaintForAWhile();
        }
        internal void AddMultiTabNext(GameObject[] targets, bool focus = true)
        {
            ResetMaximizedView();
            tabs.Insert(activeIndex + 1, new TabInfo(targets, tabs.Count, this));
            UpdatePreviousTab();
            int index = activeIndex + 1;
            if (focus)
            {
                activeIndex = index;
            }
            TabInfo tab = tabs[index];
            tab.newTab = true;
            tab.multiEditMode = true;
            totalWidth.Add(100);
            if (targets != null && targets.Length > 0)
            {
                tab.newTab = false;
                if (IsGameObjectInPrefabMode(targets[0]))
                {
                    tab.prefab = true;
                    tab.history = null;
                }
                UpdateClicked(targets);
                EditorUtils.AutoFocusOnSceneView(targets);
            }
            if (focus)
            {
                FocusTab(index);
            }
            else
            {
                RefreshAllIcons();
                ScrollToIndex(index, true);
            }
            DrawCurrentComponentsContainer();
            RepaintForAWhile();
        }

        void DrawAddButton()
        {
            GUIStyle addStyle = CustomGUIStyles.AddStyle;
            GUIContent addContent = CustomGUIContents.AddContent;
            int yPosition = 0;
            Color color = GUI.backgroundColor;
            if (EditorGUIUtility.isProSkin)
            {
                GUI.backgroundColor += CustomColors.AddButtonColorDark;
            }
            else
            {
                GUI.backgroundColor -= CustomColors.AddButtonColorLight;
            }
            Rect buttonRect = new Rect(this.position.width - 23, yPosition, 23, 23);
            EditorGUIUtility.AddCursorRect(buttonRect, MouseCursor.Link);


            if (GUI.Button(buttonRect, addContent, addStyle))
            {
                if (Event.current.button == 2)
                {/*

                    foreach (var tab in tabs)
                    {
                        Debug.Log(tab.markForDeletion);
                    } 

                    return;*/
                }
                UpdateCurrentTip();
                if (InRecoverScreen())
                {
                    tabs.Clear();
                    activeIndex = -1;
                    lastSessionData = null;
                }
                if (activeIndex > tabs.Count - 1)
                {
                    activeIndex = tabs.Count - 1;
                }
                int index = tabs.Count;
                if (Event.current.button == 0)
                {
                    index = activeIndex + 1;
                }
                UpdatePreviousTab();
                tabs.Insert(index, new TabInfo(null, index, this, ""));
                tabs[index].newTab = true;
                activeIndex = index;
                targetGameObject = null;
                totalWidth.Add(100);
                ScrollToIndex(activeIndex);
                DrawCurrentComponentsContainer();
                RepaintForAWhile();
            }
            CustomGUIContents.DrawCustomButton(buttonRect, false, false);
            GUI.backgroundColor = color;
        }
        void DrawBackNextButtons()
        {
            GUIStyle toolbarButton = CustomGUIStyles.ModifiedToolbarButton;
            int yPosition = 0;
            int height = 24;
            GUI.enabled = GetActiveTab().CanMoveBack();
            Color backColor = GUI.backgroundColor;
            Color guiColor = GUI.color;
            if (GUI.enabled)
            {
                if (EditorGUIUtility.isProSkin)
                {
                    GUI.backgroundColor += CustomColors.DarkHistoryButton;
                }
                else
                {
                    GUI.backgroundColor -= CustomColors.LightHistoryButton * 1.75f;
                }
            }
            else
            {
                if (EditorGUIUtility.isProSkin)
                {
                    GUI.backgroundColor -= CustomColors.DarkHistoryDisabled;
                }
                else
                {
                    GUI.backgroundColor -= CustomColors.LightHistoryDisabled * 1.75f;
                }
            }
            GUIContent forwardContent = CustomGUIContents.ForwardContent;
            if (GetActiveTab().CanMoveForward())
            {


                forwardContent.tooltip = "Go to next Selection\nMiddle click -> Open in new tab";
            }
            GUIContent backContent = CustomGUIContents.BackContent;
            if (GetActiveTab().CanMoveBack())
            {


                backContent.tooltip = "Go to previous Selection\n" + "Middle click -> Open in new tab";
            }
            Rect buttonRect = new Rect(0, yPosition, 20, height);
            if (GUI.Button(buttonRect, backContent, toolbarButton))
            {
                if (Event.current.button == 1)
                {
                    GenericMenu menu = new GenericMenu();
                    for (int i = 0; i < GetActiveTab()._BackHistory().Count; i++)
                    {
                        var go = GetActiveTab().BackHistory()[i];
                        menu.AddItem(new GUIContent(GetActiveTab()._BackHistory()[i]), false, () =>
                         {
                             GetActiveTab().MoveBackUntil(go);
                         });
                    }
                    Rect rect = new Rect(0, yPosition + 25, 0, 0);
                    menu.DropDown(rect);
                }
                else if (Event.current.button == 0)
                {
                    GetActiveTab().MoveBack();
                }
                else if (Event.current.button == 2)
                {
                    GameObject[] _step = GetActiveTab().BackHistory()[0];
                    if (_step != null)
                    {
                        if (_step.Length == 1)
                        {
                            AddTabNext(_step[0]);
                        }
                        else
                        {
                            AddMultiTabNext(_step);
                        }
                    }
                    List<GameObject[]> history = new List<GameObject[]>(tabs[activeIndex - 1].history);
                    GetActiveTab().history = history;
                    GetActiveTab().historyPosition = tabs[activeIndex - 1].historyPosition - 1;
                }
            }
            if (GUI.enabled)
            {
                CustomGUIContents.DrawCustomButton(buttonRect, false, false, true);
            }
            else
            {
                EditorUtils.DrawLineOverRect(buttonRect, 0);
            }
            GUI.enabled = true;
            GUI.backgroundColor = backColor;
            bool showPast = GetActiveTab().CanMoveForward();
            GUI.enabled = showPast;
            if (showPast)
            {
                if (EditorGUIUtility.isProSkin)
                {
                    GUI.backgroundColor += CustomColors.DarkHistoryButton;
                }
                else
                {
                    GUI.backgroundColor -= CustomColors.LightHistoryButton * 1.75f;
                }
            }
            else
            {
                if (EditorGUIUtility.isProSkin)
                {
                    GUI.backgroundColor -= CustomColors.DarkHistoryDisabled;
                }
                else
                {
                    GUI.backgroundColor -= CustomColors.LightHistoryDisabled * 1.75f;
                }
            }
            buttonRect = new Rect(20, yPosition, 20, height);
            if (GUI.Button(buttonRect, forwardContent, toolbarButton))
            {
                if (Event.current.button == 1)
                {
                    GenericMenu menu = new GenericMenu();
                    for (int i = 0; i < GetActiveTab()._ForwardHistory().Count; i++)
                    {
                        var go = GetActiveTab().ForwardHistory()[i];
                        menu.AddItem(new GUIContent(GetActiveTab()._ForwardHistory()[i]), false, () =>
                        {
                            GetActiveTab().MoveForwardUntil(go);
                        });
                    }
                    Rect rect = new Rect(20, yPosition + 25, 0, 0);
                    menu.DropDown(rect);
                }
                else if (Event.current.button == 0)
                {
                    GetActiveTab().MoveForward();
                }
                else if (Event.current.button == 2)
                {
                    GameObject[] _step = GetActiveTab().ForwardHistory()[0];
                    if (_step != null)
                    {
                        if (_step.Length == 1)
                        {
                            AddTabNext(_step[0]);
                        }
                        else
                        {
                            AddMultiTabNext(_step);
                        }
                    }
                    List<GameObject[]> history = new List<GameObject[]>(tabs[activeIndex - 1].history);
                    GetActiveTab().history = history;
                    GetActiveTab().historyPosition = tabs[activeIndex - 1].historyPosition + 1;
                }
            }
            if (GUI.enabled)
            {
                CustomGUIContents.DrawCustomButton(buttonRect, false, false, true);
            }
            else
            {
                EditorUtils.DrawLineOverRect(buttonRect, 0);
            }
            GUI.enabled = true;
            GUI.backgroundColor = backColor;
        }

        void LimitScrollBar()
        {
            if (toolbarScrollPosition.x < 0)
            {
                toolbarScrollPosition.x = 0;
                return;
            }
            float maxScroll = GetMaximumScroll();


            if (maxScroll < 0)
            {
                maxScroll = 0;
            }
            if (maxScroll == 0)
            {
                toolbarScrollPosition.x = 0;
                return;
            }
            if (toolbarScrollPosition.x > maxScroll)
            {
                toolbarScrollPosition.x = maxScroll;
            }
        }

        float GetMaximumScroll()
        {
            int historyVar = 36;
            float windowWidth = position.width;
            int addVar = 24;
            if (!showHistory)
            {
                historyVar = 0;
            }
            float viewportWidth = windowWidth - addVar - historyVar;
            float total = GetTotalTabsWidth() - viewportWidth;
            if (total < 0)
            {
                total = 0;
            }
            return total;
        }
        internal float GetTotalTabsWidth(int upToIndex)
        {
            if (upToIndex == -1)
            {
                return 3;
            }

            float total = 0;
            int lastIndex = Mathf.Min(upToIndex, tabs.Count - 1);

            for (int i = 0; i <= lastIndex; i++)
            {
                total += tabs[i].tabWidth;
            }
            total += 3;
            return total;
        }

        internal float GetTotalTabsWidth()
        {
            if (totalWidth == null || totalWidth.Count == 0)
            {
                return lastValidTabWidth;
            }
            float total = 0;
            for (int i = 0; i < tabs.Count; i++)
            {
                total += tabs[i].tabWidth;
            }
            if (total == 0)
            {
                return lastValidTabWidth;
            }
            total += 3;
            lastValidTabWidth = total;
            return total;
        }
        void ScrollToActiveTab()
        {
            if (activeIndex < 0 || tabs == null || activeIndex > tabs.Count - 1)
            {
                return;
            }
            _UpdateTabScroll();
            FocusTab(activeIndex);
        }
        void ScrollToIndex(int index, bool softScroll = false)
        {
            if (tabs != null && tabs.Count > 0)
            {
                UpdateAllWidths();
                UpdateTabBar(true);
                LimitScrollBar();
            }
            if (index > tabs.Count - 1)
            {
                index = tabs.Count - 1;
            }
            if (index < 0)
            {
                index = 0;
            }
            if (totalWidth.Count == 0 || totalWidth == null || totalWidth.Count < index || totalWidth.Count < 1)
            {
                totalWidth.Add(100);
                return;
            }
            if (!softScroll)
            {
                activeIndex = index;
            }
            if (activeIndex < 0 || activeIndex > totalWidth.Count - 1)
            {
                totalWidth.Add(100);
            }
            float buttonWidth = totalWidth[index];
            float total = 0f;
            float totalContentWidth = 0f;
            for (int i = 0; i < index; i++)
            {
                total += totalWidth[i];
            }
            for (int i = 0; i < totalWidth.Count; i++)
            {
                if (!isLocked && i == 0)
                {
                    continue;
                }
                totalContentWidth += totalWidth[i];
            }

            float viewWidth = scrollRect.width + 1;
            if (showHistory)
            {
                viewWidth -= 36;
            }
            if (total - 25 < toolbarScrollPosition.x)
            {
                toolbarScrollPosition.x = total - 25;
            }
            else if (total + buttonWidth + 25 > toolbarScrollPosition.x + viewWidth)
            {
                toolbarScrollPosition.x = total + 25 + buttonWidth - viewWidth;
            }
            toolbarScrollPosition.x = Mathf.Max(0, Mathf.Min(toolbarScrollPosition.x, total));
            LimitScrollBar();
            RepaintForAWhile();
        }

        void DrawComponentEditorTools(ComponentMap map)
        {
            if (map.component == null)
            {
                return;
            }
            if (map.component is ArticulationBody)
            {
                ArticulationBody articulationBody = map.component as ArticulationBody;
                if (articulationBody.isRoot)
                {
                    return;
                }
            }
            Rect compHeaderRect = Rect.zero;
            if (ShouldDrawAlwaysEditorTools())
            {
                if (ShouldDrawSelectButton(map.component))
                {
                    DrawSelectComponentButton(map);
                }
                if (EditorUtils.IsValidEditorToolType(map.component))
                {
                    DoDrawComponentEditorTools(map.component, compHeaderRect);
                }
            }
            else
            {
                if (!IsIncludedInSelection(new GameObject[1] { GetActiveTab().target }))
                {
                    if (EditorUtils.IsValidEditorToolType(map.component))
                    {
                        DoDrawComponentEditorTools(map.component, compHeaderRect);
                    }
                    else
                    {
                        DrawSelectComponentButton(map);
                    }
                }
            }
        }
        static bool ShouldDrawAlwaysEditorTools()
        {
            bool drawEditors = false;
#if UNITY_2020_2_OR_NEWER && !UNITY_2022_2_OR_NEWER
        drawEditors = true;
#endif
            return drawEditors;
        }

        bool ShouldDrawSelectButton(Component component)
        {
            if (component == null)
            {
                return false;
            }
            bool alreadySelected = IsIncludedInSelection(new GameObject[1] { GetActiveTab().target });
            if (alreadySelected)
            {
                return false;
            }

            return !EditorUtils.IsValidEditorToolType(component);
        }

        void DrawSelectComponentButton(ComponentMap map)
        {
            if (Reflected.ComponentHasEditorTool(map.component))
            {
                Color color = GUI.backgroundColor;
                GUI.backgroundColor += CustomColors.AssetBarBackColor * 2;
                GUIContent button = new GUIContent(CustomGUIContents.SelectButtonImage);
                GUILayout.BeginHorizontal();
                GUILayout.Space(18);
                GUILayout.BeginVertical();
                if (GUILayout.Button(button, CustomGUIStyles.NoMarginButton, GUILayout.Width(24), GUILayout.Height(20)))
                {
                    SelectIfNotAlready(map.component.gameObject);
                    ScrollToActiveTab();
                }
                GUILayout.EndVertical();
                Rect rect1 = GUILayoutUtility.GetLastRect();
                rect1.width = 22;
                rect1.x += 1;
                rect1.y += 1;
                EditorUtils.DrawLineOverRect(rect1);
                EditorUtils.DrawFadeToBottom(rect1, CustomColors.SubtleBright, 10);
                EditorUtils.DrawFadeToBottom(rect1, CustomColors.SubtleBright, 5);
                GUILayout.EndHorizontal();
                Rect rect = GUILayoutUtility.GetLastRect();

                rect.width = position.width - rect.x + rect.width;
                rect.x += 48;
                GUI.enabled = false;
                GUI.Label(rect, CustomGUIContents.SelectForTools, CustomGUIStyles.RichLabel);
                GUI.enabled = true;
                EditorGUIUtility.AddCursorRect(rect, MouseCursor.Link);
                GUI.backgroundColor = color;
            }
        }
        void DoDrawComponentEditorTools(Component component, Rect compHeaderRect)
        {
            if (Reflected.ComponentHasEditorTool(component))
            {
                if (!ActiveTabInDebugMode())
                {
                    GUI.enabled = true;
                    var tools = EditorToolCache.GetToolsForComponent(component);
                    EditorGUILayout.BeginHorizontal();
                    float labelWidth = EditorGUIUtility.labelWidth;
                    EditorGUIUtility.labelWidth -= 55;
                    EditorGUIUtility.labelWidth = labelWidth;
                    GUILayout.FlexibleSpace();
                    for (int i = 0; i < tools.Count; i++)
                    {
                        var tool = tools[i];
                        GUIContent button = EditorToolCache.
                        GetToolIconForComponent(component, tool);
                        GUIStyle style = CustomGUIStyles.ActiveButtonStyle;
                        Rect buttonRect = compHeaderRect;
                        buttonRect.y = 0;
                        //  buttonRect.width = 32;
                        buttonRect.height = 22;
                        buttonRect.x = 0;
                        buttonRect.x += buttonRect.width * i;
                        Color color1 = GUI.color;
                        bool active = EditorToolCache.IsToolActiveForComponent(component, tool);
                        if (!active)
                        {
                            GUI.color -= CustomColors.EditorToolInactive;
                        }
                        else
                        {
                            GUI.color += CustomColors.EditorToolActive;
                        }

                        if (GUILayout.Button(button, style, GUILayout.Width(32), GUILayout.Height(22)))
                        {
                            if (!active)
                            {
                                EditorToolCache.ActivateTool(tool, component);
                            }
                            else
                            {
                                EditorToolCache.RestorePreviousPersistentTool();
                            }
                        }
                        buttonRect = GUILayoutUtility.GetLastRect();
                        GUI.color = color1;
                        GUI.Label(buttonRect, button, CustomGUIStyles.CenteredLabelStyle);
                        buttonRect.width -= 2;
                        buttonRect.x += 1;
                        EditorUtils.DrawLineOverRect(buttonRect, -1);
                    }
                    GUILayout.FlexibleSpace();
                    EditorGUILayout.EndHorizontal();
                    GUI.enabled = true;
                }
            }
        }
#if UNITY_6000_4_OR_NEWER
        internal void HandleAssetClickEntity(EntityId entityId, Rect selectionRect)
        {
            HandleAssetClick(entityId, selectionRect);
        }
#endif
#if UNITY_2022_1_OR_NEWER
#if UNITY_6000_4_OR_NEWER
        internal void HandleAssetClick(EntityId entityID, Rect selectionRect)

#else
        internal void HandleAssetClick(int entityID, Rect selectionRect)

#endif
        {
            Event current = Event.current;
            if ((current.type == EventType.MouseDown || current.type == EventType.MouseUp) && current.button == 0 && selectionRect.Contains(current.mousePosition))
            {
                UnityEngine.Object asset = null;
                MissingScriptManager missing = null;
#if UNITY_6000_4_OR_NEWER
                if (entityID.IsValid() && IdToObject(entityID) == null)

#else
                if (entityID != 0 && IdToObject(entityID) == null)

#endif
                {
                    MissingScriptManager.WriteData(entityID);
                    asset = missing;
                }
                else
                {
#if UNITY_6000_4_OR_NEWER
                    asset = EditorUtils.IdToObject(entityID);
#else
#pragma warning disable CS0618
                    asset = EditorUtility.InstanceIDToObject(entityID);
#pragma warning restore CS0618


#endif
                }
                if (asset != null)
                {
                    DoAssetClick(asset);
                    Repaint();
                }
            }
        }
#else
        internal void HandleAssetClick(string guid, Rect selectionRect)
{
    Event current = Event.current;
    if ((current.type == EventType.MouseDown || current.type == EventType.MouseUp) && current.button == 0 && selectionRect.Contains(current.mousePosition))
    {        
        string assetPath = AssetDatabase.GUIDToAssetPath(guid);
        UnityEngine.Object asset = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(assetPath);        
        if (asset != null)
        {
            DoAssetClick(asset);
            Repaint();
        }
    }
}
#endif
        internal void DoAssetClick(UnityEngine.Object asset)
        {
            Event current = Event.current;
            if (EditorUtils.IsCtrlHeld())
            {
                return;
            }
            if (lockedAsset || (ignoreFolders && IsAssetAFolder(AssetDatabase.GetAssetPath(asset))))
            {
                return;
            }
            if (!lockedAsset && asset != targetObject)
            {
                if (current.type == EventType.MouseDown)
                {
                    awaitingAssetClick = true;
                    return;
                }
                else if (current.type == EventType.MouseUp && awaitingAssetClick)
                {
                    awaitingAssetClick = false;
                    ignoreSelection = new UnityEngine.Object[] { asset };
                    if (!CheckForApplyRevertOnClose())
                    {
                        return;
                    }
                    SetTargetAsset(asset);
                }
            }
            if (current.type == EventType.MouseUp)
            {
                awaitingAssetClick = false;
            }
        }
        internal void OpenInNewTab(GameObject[] gameObjects, bool focusAfter = true)
        {
            tabs.Add(new TabInfo(gameObjects, tabs.Count, this));

            if (!focusAfter)
            {
                ignoreNextSelection = true;
                RefreshAllIcons();
                RepaintForAWhile();
                return;
            }
            else
            {
                UpdatePreviousTab();
            }
            activeIndex = tabs.Count - 1;
            tabs[tabs.Count - 1].newTab = false;
            totalWidth.Add(1000);
            if (IsActiveTabValidMulti())
            {
                SetTargetGameObjects(GetActiveTab().targets);
            }
            else
            {
                SetTargetGameObject(GetActiveTab().target);
            }
            FocusTab(activeIndex);
            RepaintForAWhile();
        }
#if UNITY_6000_4_OR_NEWER
        internal void HandleMiddleClickEntity(EntityId entityId, Rect selectionRect)
        {
            HandleMiddleClick(entityId, selectionRect);
        }
#endif

#if UNITY_6000_4_OR_NEWER
        internal void HandleMiddleClick(EntityId entityID, Rect selectionRect)

#else
        internal void HandleMiddleClick(int entityID, Rect selectionRect)

#endif
        {
            Event current = Event.current;
            if (current.type == EventType.MouseUp && current.button == 0 && !current.alt && !current.shift && !EditorUtils.IsCtrlHeld())
            {
                if (GetActiveTab() != null)
                {
                    GetActiveTab().RefreshName();
                }
                if (Time.realtimeSinceStartup - lastTabClick < 0.1f && lastTabClick != -1)
                {
                    return;
                }
                if (!selectionRect.Contains(current.mousePosition))
                {
                    return;
                }
                lastTabClick = Time.realtimeSinceStartup;
                if (!selectionRect.Contains(current.mousePosition))
                {
                    return;
                }
                if (current.button == 0)
                {
                    if (!selectionRect.Contains(current.mousePosition))
                    {
                        return;
                    }

                    GameObject go = IdToObject(entityID) as GameObject;

                    if (go == null)
                    {
                        ignoreNextSelection = true;
                        return;
                    }
                    if (TryReuseTab(go, false))
                    {
                        return;
                    }
                    if (IsActiveTabLocked())
                    {

                        if (newTabIfLocked)
                        {
                            if (recycleUnlockedTabs)
                            {
                                TabInfo unlockedTab = GetClosestUnlockedTab();
                                if (unlockedTab != null)
                                {
                                    FocusTab(unlockedTab.index);
                                    SetTargetGameObject(go);
                                    return;
                                }
                            }
                            AddTabNext(go);
                        }
                        return;
                    }
                    /* NEEDS SOME REWORK. DISABLED FOR NOW

                    if (EditorUtils.IsCtrlHeld() && GetActiveTab() != null && !GetActiveTab().newTab)
                    {
                        if (Selection.gameObjects != null && Selection.gameObjects.Length > 0)
                        {
                            GameObject[] tabObjects;
                            if (IsActiveTabValidMulti())
                            {
                                tabObjects = GetActiveTab().targets;
                            }
                            else
                            {
                                tabObjects = new GameObject[] { GetActiveTab().target };
                            }

                            if (!AreArraysEqual(Selection.gameObjects, tabObjects))
                            {
                                List<GameObject> newObjects = new List<GameObject>(tabObjects);
                               
                                if (!newObjects.Contains(go))
                                {                               
                                    newObjects.Add(go);
                                }
                                 
                                OverrideMulti(newObjects.ToArray());
                                Selection.objects = null;
                               ignoreNextSelection = true;
                               ignoreSelection = newObjects.ToArray();
                                Selection.objects = newObjects.ToArray();
                                return;

                            }
                        }
                    } */

                    if (tabs.Count > 0 && GetActiveTab().target != go)
                    {
                        GetActiveTab().multiEditMode = false;
                        GetActiveTab().scrollPosition = 0;
                        EditorToolCache.RestorePreviousPersistentTool();
                        SetTargetGameObject(go);
                        Repaint();
                    }

                    return;
                }
            }

            else if (current.type == EventType.MouseUp && !EditorUtils.IsCtrlHeld() && current.button == 2 && !current.shift)
            {
                if (GetActiveTab() != null)
                {
                    GetActiveTab().RefreshName();
                }
                if (Time.realtimeSinceStartup - lastTabClick < 0.1f && lastTabClick != -1)
                {
                    return;
                }
                if (!selectionRect.Contains(current.mousePosition))
                {
                    return;
                }
                GameObject go = IdToObject(entityID) as GameObject;

                if (go == null)
                {
                    return;
                }
                lastTabClick = Time.realtimeSinceStartup;
                if (Selection.gameObjects != null && Selection.gameObjects.Length > 1)
                {
                    if (Selection.gameObjects.Contains(go))
                    {
                        if (InRecoverScreen())
                        {
                            tabs.Clear();
                            activeIndex = -1;
                            lastSessionData = null;
                        }
                        AddMultiTabNext(Selection.gameObjects);
                        return;
                    }
                }
                Selection.activeGameObject = go;
                if (InRecoverScreen())
                {
                    tabs.Clear();
                    activeIndex = -1;
                    lastSessionData = null;
                }
                if (TryReuseTab(go, true))
                {
                    return;
                }
                AddTabNext(go);
            }
        }
        void HandleMoveComponent(Component draggedComponent, int index, int targetIndex, Component[] components)
        {
            if (pendingOperation == null || pendingOperation.consumed)
            {
                return;
            }
            pendingOperation.consumed = true;
            Undo.RegisterCompleteObjectUndo(draggedComponent.gameObject, "Move Components");
            int compIndex = index;
            Reflected.MoveComponent(draggedComponent, components[targetIndex], false);
            performPasteComponent = 0;
        }
        /* internal static bool isTargetARemovedScript()
         {
             GenericInspector.ObjectIsMonoBehaviourOrScriptableObjectWithoutScript(target)
         }*/

        internal void ApplyPlayModeTransforms()
        {
            if (playModeTransforms == null)
            {
                return;
            }

            var keptTransforms = new List<SerializableTransform>();

            foreach (var transform in playModeTransforms)
            {
                Transform go = transform.owner;
                if (go == null)
                {
                    continue;
                }

                if (SerializableTransform.SameTransforms(go, transform))
                {
                    continue;
                }

                transform.Apply();
                if (transform.appliedTransform)
                {
                    keptTransforms.Add(transform);
                }
            }
            if (keptTransforms.Count == 0)
            {
                playModeTransforms = null;
            }
            else
            {
                playModeTransforms = keptTransforms;
            }
        }
        internal void ResetPlayModeTransform(bool hardMode = true)
        {
            WipePlayModeSave();
        }

        internal bool EnteringPlaymode
        {
            get
            {
                if (settingsData != null)
                {
                    return settingsData.enteringPlayMode;
                }
                return enteringPlayMode;
            }
        }
        internal void SetEnteringPlayMode()
        {
            if (settingsData != null)
            {
                _UpdateTabScroll();
                settingsData.enteringPlayMode = true;
            }
            enteringPlayMode = true;
        }
        internal void SetNotEnteringPlayMode()
        {
            if (settingsData != null)
            {
                settingsData.enteringPlayMode = false;
            }
            enteringPlayMode = false;
        }

        private void HandlePlayModeStateChanged(PlayModeStateChange state)
        {
            ReloadPreview();
            switch (state)
            {

                case PlayModeStateChange.ExitingEditMode:

                    inActualPlayMode = true;
                    lastTabClick = -1;
                    lastTabClick = -1;
                    lastOpen = -1;
                    SetEnteringPlayMode();
                    pendingRestore = false;
                    ResetPlayModeTransform();
                    rootVisualElement.Clear();
                    //Debug.Log("Exited Edit Mode");
                    break;
                case PlayModeStateChange.ExitingPlayMode:

                    inActualPlayMode = false;
                    SetNotEnteringPlayMode();
                    UpdateAllAutoSaveTransforms();
                    CloseCoInspector();
                    exitingPlayMode = true;
                    if (sessionsMode == 2)
                    {
                        pendingRestore = true;
                    }
                    rootVisualElement.Clear();
                    break;
                case PlayModeStateChange.EnteredEditMode:
                    CreateGUI();
                    inActualPlayMode = false;
                    SetNotEnteringPlayMode();
                    lastTabClick = -1;
                    lastTabClick = -1;
                    lastOpen = -1;
                    pendingRestore = !InRecoverScreen();
                    activeScene = SceneInfo.FromActiveScene();
                    ReopenCoInspector(SceneManager.GetActiveScene(), LoadSceneMode.Single);
                    scenesChanged = false;
                    pendingRestore = false;
                    RunNextFrame(FocusTab);
                    ApplyPlayModeTransforms();

                    break;
                case PlayModeStateChange.EnteredPlayMode:
                    CreateGUI();
                    inActualPlayMode = true;
                    SetNotEnteringPlayMode();
                    ResetPlayModeTransform();
                    lastTabClick = -1;
                    lastTabClick = -1;
                    FocusTab();
                    EditorApplication.delayCall += ReinitializeComponentEditors;
                    EditorApplication.delayCall += Repaint;
                    if (instances == null)
                    {
                        instances = new List<CoInspectorWindow>();
                    }
                    if (!instances.Contains(this))
                    {
                        instances.Add(this);
                    }
                    break;
            }
        }
        void SetAllEditorsDebugTo(bool value)
        {
            if (componentEditors != null)
            {
                InspectorMode inspectorMode = InspectorMode.Normal;
                if (value)
                {
                    inspectorMode = InspectorMode.Debug;
                }
                for (int i = 0; i < componentEditors.Length; i++)
                {
                    if (componentEditors[i] != null)
                    {
                        Reflected.SetInspectorMode(componentEditors[i], inspectorMode);
                    }
                }
            }
        }
        void SetAllAssetEditorsDebugTo(bool value)
        {
            if (prefabEditors != null)
            {
                InspectorMode inspectorMode = InspectorMode.Normal;
                if (value)
                {
                    inspectorMode = InspectorMode.Debug;
                }
                for (int i = 0; i < prefabEditors.Length; i++)
                {
                    if (prefabEditors[i] != null)
                    {
                        Reflected.SetInspectorMode(prefabEditors[i], inspectorMode);
                    }
                }
            }
            if (assetEditor != null)
            {
                InspectorMode inspectorMode = InspectorMode.Normal;
                if (value)
                {
                    inspectorMode = InspectorMode.Debug;
                }
                Reflected.SetInspectorMode(assetEditor, inspectorMode);
            }
            if (assetImportSettingsEditor != null)
            {
                InspectorMode inspectorMode = InspectorMode.Normal;
                if (value)
                {
                    inspectorMode = InspectorMode.Debug;
                }
                Reflected.SetInspectorMode(assetImportSettingsEditor, inspectorMode);
            }
            DrawPrefabEditors();

        }
        internal static string GetPrefabStageRootPath()
        {
#if UNITY_2021_2_OR_NEWER
            UnityEditor.SceneManagement.PrefabStage prefabStage = UnityEditor.SceneManagement.PrefabStageUtility.GetCurrentPrefabStage();
#else
            UnityEditor.Experimental.SceneManagement.PrefabStage prefabStage = UnityEditor.Experimental.SceneManagement.PrefabStageUtility.GetCurrentPrefabStage();
#endif
            if (prefabStage != null && prefabStage.prefabContentsRoot != null)
            {
#if UNITY_2020_1_OR_NEWER
                return prefabStage.assetPath;
#else
                return prefabStage.prefabAssetPath;
#endif
            }
            return "";
        }

        internal static GameObject GetPrefabStageRoot()
        {
#if UNITY_2021_2_OR_NEWER
            UnityEditor.SceneManagement.PrefabStage prefabStage = UnityEditor.SceneManagement.PrefabStageUtility.GetCurrentPrefabStage();
#else
            UnityEditor.Experimental.SceneManagement.PrefabStage prefabStage = UnityEditor.Experimental.SceneManagement.PrefabStageUtility.GetCurrentPrefabStage();
#endif
            if (prefabStage != null && prefabStage.prefabContentsRoot != null)
            {
                return prefabStage.prefabContentsRoot;
            }
            return null;
        }

        internal static bool SceneIsInPrefabMode()
        {
#if UNITY_2021_2_OR_NEWER
            UnityEditor.SceneManagement.PrefabStage prefabStage = UnityEditor.SceneManagement.PrefabStageUtility.GetCurrentPrefabStage();
#else
            UnityEditor.Experimental.SceneManagement.PrefabStage prefabStage = UnityEditor.Experimental.SceneManagement.PrefabStageUtility.GetCurrentPrefabStage();
#endif
            if (prefabStage != null && prefabStage.prefabContentsRoot != null)
            {
                return true;
            }
            return false;
        }
        internal static bool AreAllGameObjectsInPrefabMode(GameObject[] gameObjects)
        {
            if (gameObjects == null || gameObjects.Length == 0)
            {
                return false;
            }
            for (int i = 0; i < gameObjects.Length; i++)
            {
                if (!IsGameObjectInPrefabMode(gameObjects[i]))
                {
                    return false;
                }
            }
            return true;
        }

        internal static bool IsGameObjectInPrefabMode(GameObject gameObject)
        {
            if (gameObject == null)
            {
                return false;
            }
#if UNITY_2021_2_OR_NEWER
            UnityEditor.SceneManagement.PrefabStage prefabStage = UnityEditor.SceneManagement.PrefabStageUtility.GetCurrentPrefabStage();
#else
            UnityEditor.Experimental.SceneManagement.PrefabStage prefabStage = UnityEditor.Experimental.SceneManagement.PrefabStageUtility.GetCurrentPrefabStage();
#endif
            if (prefabStage != null && prefabStage.scene == gameObject.scene)
            {
                return true;
            }
            return false;
        }
        internal static void DestroyAllIfNotNull(Editor[] editors)
        {
            if (editors != null)
            {
                for (int i = 0; i < editors.Length; i++)
                {
                    DestroyIfNotNull(editors[i]);
                }
            }
        }
        internal static void DestroyIfNotNull(Editor editor)
        {
            if (editor != null)
            {
                if (editor is IDisposable disposableEditor)
                {
                    disposableEditor.Dispose();
                }
                DestroyImmediate(editor);
            }
        }

        internal static void DestroyEmptyEditors(Editor[] editors)
        {
            if (editors != null)
            {
                for (int i = 0; i < editors.Length; i++)
                {
                    if (editors[i] != null)
                    {
                        if (editors[i].targets.Length == 0 || editors[i].targets[0] == null || editors[i].serializedObject == null)
                        {
                            DestroyIfNotNull(editors[i]);
                            editors[i] = null;
                        }
                    }
                }
            }
        }

        internal void ReinitializePrefabComponentEditors()
        {
            DoReinitializePrefabComponentEditors();
        }

        internal void DoReinitializePrefabComponentEditors()
        {
            DestroyAllIfNotNull(prefabMaterialEditors);
            prefabMaterialEditors = null;
            DestroyAllIfNotNull(prefabEditors);
            prefabEditors = null;
            if (AssetTargetMode() == 1 && targetObject is GameObject)
            {
                GameObject prefab = targetObject as GameObject;
                Component[] components = prefab.GetComponents<Component>();
                if (prefabEditors == null || prefabEditors.Length != components.Length)
                {
                    prefabEditors = new Editor[components.Length];
                }
                for (int i = 0; i < components.Length; i++)
                {
#if UNITY_6000_4_OR_NEWER
                    if (prefabEditors[i] == null || EditorUtility.EntityIdToObject(prefabEditors[i].GetEntityId()) == null)
#else
#pragma warning disable CS0618
                    if (prefabEditors[i] == null || EditorUtility.InstanceIDToObject(prefabEditors[i].GetInstanceID()) == null)
#pragma warning restore CS0618

#endif

                    {
                        DestroyIfNotNull(prefabEditors[i]);
                        prefabEditors[i] = null;
                        Editor.CreateCachedEditor(components[i], null, ref prefabEditors[i]);
                    }
                }
                prefabFoldouts_ = new bool[prefabEditors.Length];
                prefabFoldoutsChangeTracker_ = new bool[prefabEditors.Length];
                bool defaultCollapse = !collapsePrefabComponents;
                if (assetOnlyMode)
                {
                    defaultCollapse = true;
                }

                for (int i = 0; i < prefabEditors.Length; i++)
                {
                    prefabFoldouts_[i] = defaultCollapse;
                    prefabFoldoutsChangeTracker_[i] = defaultCollapse;
                }
                SetAllAssetEditorsDebugTo(debugAsset);
            }
            else if (AssetTargetMode() == 2 && (EditorUtils.AreAllTargetsPrefabs(targetObjects) || EditorUtils.AreAllTargetsImportedObjects(targetObjects)))
            {
                RebuildPrefabMultiComponentEditors();
            }
            else
            {
                prefabEditors = null;
                prefabFoldouts_ = null;
            }

        }
        private void CleanAllTabMaps()
        {
            if (tabs != null)
            {
                for (int i = 0; i < tabs.Count; i++)
                {
                    tabs[i].DestroyAllMaterialMaps();
                    tabs[i].ResetCulling();
                }
            }
            PrefabMaterialMapManager.DestroyAllMaterialMaps();
        }

        void DoReinitializeComponentEditors()
        {
            ReinitializeComponentEditors(true);
        }
        internal void ReinitializeComponentEditors(bool skipGO = false)
        {
            if (GetActiveTab() == null)
            {
                return;
            }
            //Debug.Log("Reinitializing component editors");
            CleanAllTabMaps();
            ReadLastClicked();
            DestroyIfNotNull(gameObjectEditor);
            gameObjectEditor = null;
            DestroyAllIfNotNull(materialEditors);
            materialEditors = null;
            GetActiveTab().trackMultiTarget = null;
            if (tabs != null && GetActiveTab().multiEditMode)
            {
                Editor.CreateCachedEditor(GetActiveTab().targets, null, ref gameObjectEditor);
                DestroyAllIfNotNull(componentEditors);
                componentEditors = null;
                differentComponents = false;
                GameObject lastObject = GetActiveTab().targets[0];
                if (lastObject == null)
                {
                    return;
                }
                int lastObjectIndex = Array.IndexOf(GetActiveTab().targets, lastObject);
                List<KeyValuePair<Type, List<List<Component>>>> map = EditorUtils.OrderedComponentMap(GetActiveTab().targets, this);
                if (map == null)
                {
                    return;
                }
                List<Component[]> orderedComponentArrays = new List<Component[]>();
                foreach (Component comp in lastObject.GetComponents<Component>())
                {
                    if (comp == null) continue;

                    Type compType = comp.GetType();
                    var typeEntry = map.FirstOrDefault(e => e.Key == compType);
                    if (typeEntry.Key != null)
                    {
                        int compIndex = typeEntry.Value[0].IndexOf(comp);
                        if (compIndex != -1)
                        {
                            Component[] targetComponents = typeEntry.Value.Select(list => list[compIndex]).ToArray();
                            orderedComponentArrays.Add(targetComponents);
                        }
                    }
                }
                List<Editor> editorList = new List<Editor>();
                foreach (Component[] targetComponents in orderedComponentArrays)
                {
                    Editor editor = null;
                    Editor.CreateCachedEditor(targetComponents, null, ref editor);
                    if (editor != null)
                    {
                        editorList.Add(editor);
                    }
                }
                componentEditors = editorList.ToArray();
                componentFoldouts_ = new bool[componentEditors.Length];
                for (int i = 0; i < componentEditors.Length; i++)
                {
                    componentFoldouts_[i] = true;
                }
                SetAllEditorsDebugTo(ActiveTabInDebugMode());
                RefreshAllTabNames();
                RefreshAllIcons();
                UpdateAllTabPaths();
                ResetComponentCulling();
                DrawCurrentComponentsContainer();
                Repaint();
                return;
            }
            DestroyAllIfNotNull(componentEditors);
            componentEditors = null;
            componentFoldouts_ = null;
            if (GetActiveTab().target != null)
            {
                Editor.CreateCachedEditor(GetActiveTab().target, null, ref gameObjectEditor);
                Component[] components = GetActiveTab().target.GetComponents<Component>();
                if (componentEditors == null || componentEditors.Length != components.Length)
                {
                    componentEditors = new Editor[components.Length];
                }
                for (int i = 0; i < components.Length; i++)
                {
                    DestroyIfNotNull(componentEditors[i]);
                    componentEditors[i] = null;
                    Editor.CreateCachedEditor(components[i], null, ref componentEditors[i]);
                }
            }
            else
            {
                componentEditors = null;
                componentFoldouts_ = null;
            }
            ResetComponentCulling();
            DrawCurrentComponentsContainer();
            SetAllEditorsDebugTo(ActiveTabInDebugMode());
            RefreshAllTabNames();
            RefreshAllIcons();
            UpdateAllTabPaths();
            rootVisualElement.MarkDirtyRepaint();
            rootVisualElement.schedule.Execute(() =>
            {
                Repaint();
            });
        }
        bool LastChangeWasNotNow()
        {
            if (Time.realtimeSinceStartup - lastChangeOfState > 0.5f)
            {
                return true;
            }
            return false;
        }
        internal static void FixSaveDataReferences()
        {
            if (MainCoInspector)
            {
                if (MainCoInspector.settingsData && AssetDatabase.GetAssetPath(MainCoInspector.settingsData) != "")
                {
                }
                else
                {
                    MainCoInspector.settingsData = null;
                    MainCoInspector.AutoCreateSettings();
                }
            }
        }

        internal void SaveSettings()
        {
            FixSaveDataReferences();
            if (settingsData == null)
            {
                settingsData = AutoCreateSettings();
            }
            if (settingsData)
            {
                if (!InRecoverScreen())
                {
                    settingsData.SaveData(false, this);
                }
                else
                {
                    settingsData.SaveData(false);
                }
            }
            UpdateTabScroll();
        }
        internal Dictionary<Type, List<UnityObject>> SortedAssetSelection { get => sortedAssetSelection; set => sortedAssetSelection = value; }
        internal static bool IsAlreadySelected(UnityObject[] array2)
        {
            UnityObject[] array1 = Selection.objects;

            if (array1 == null || array2 == null)
            {
                return false;
            }
#if UNITY_6000_4_OR_NEWER
            var set1 = new HashSet<EntityId>(array1.Where(obj => obj != null).Select(obj => GetObjectId(obj)));
            var set2 = new HashSet<EntityId>(array2.Where(obj => obj != null).Select(obj => GetObjectId(obj)));
#else
            var set1 = new HashSet<int>(array1.Where(obj => obj != null).Select(obj => GetObjectId(obj)));
            var set2 = new HashSet<int>(array2.Where(obj => obj != null).Select(obj => GetObjectId(obj)));
#endif


            return set1.SetEquals(set2);
        }
        public static bool IsIncludedInSelection(UnityObject[] array2)
        {
            UnityObject[] array1 = Selection.objects;

            if (array1 == null || array2 == null)
            {
                return false;
            }

#if UNITY_6000_4_OR_NEWER
            var set1 = new HashSet<EntityId>(array1.Where(obj => obj != null).Select(obj => GetObjectId(obj)));
            var set2 = new HashSet<EntityId>(array2.Where(obj => obj != null).Select(obj => GetObjectId(obj)));
#else
            var set1 = new HashSet<int>(array1.Where(obj => obj != null).Select(obj => GetObjectId(obj)));
            var set2 = new HashSet<int>(array2.Where(obj => obj != null).Select(obj => GetObjectId(obj)));
#endif

            return set1.IsSupersetOf(set2);
        }
        void SelectIfNotAlready(GameObject[] array, bool ignoreAfter = false, bool forceAfter = false)
        {
            if (array == null || array.Length == 0)
            {
                return;
            }
            if (!IsAlreadySelected(array))
            {
                EditorToolCache.RestorePreviousPersistentTool();
                if (ignoreAfter)
                {
                    ignoreSelection = array;
                }
                if (forceAfter)
                {
                    forceSelection = true;
                    Selection.objects = array;
                }
                else
                {
                    Selection.objects = array;
                }
                RepaintForAWhile();
            }
        }
        internal void SelectIfNotAlready(GameObject gameObject)
        {
            SelectIfNotAlready(new GameObject[] { gameObject });
        }
        void HandleSelectionChange()
        {
            if (ignoreNextSelection)
            {
                ignoreNextSelection = false;
                return;
            }
            MissingScriptManager missingScript = null;
            int missingScriptCount = MissingScriptManager.CountMissingScripts();
            if (missingScriptCount > 0)
            {
                if (missingScriptCount == 1)
                {
                    missingScript = MissingScriptManager.WriteData();
                }
                else
                {
                    missingScript = MissingScriptManager.WriteMultiData(missingScriptCount);
                }
            }

            if (missingScript == null && (Selection.objects == null || Selection.objects.Length == 0))
            {
                return;
            }
            if ((ignoreSelection != null && IsAlreadySelected(ignoreSelection)) || !LastChangeWasNotNow())
            {
                if (switchingTabs)
                {
                    switchingTabs = false;
                    Undo.PerformRedo();
                }
                ignoreSelection = null;
                Repaint();
                return;
            }
            List<GameObject> realGameObjects = new List<GameObject>();
            List<UnityEngine.Object> realAssets = new List<UnityEngine.Object>();
            foreach (var obj in Selection.objects)
            {
                if (obj is GameObject gameObject && !EditorUtils.IsAPrefabAsset(gameObject) && !EditorUtils.IsAnImportedObject(gameObject))
                {
                    realGameObjects.Add(gameObject);
                }
                else
                {
                    realAssets.Add(obj);
                }
            }
            if (missingScript != null)
            {
                realAssets.Add(missingScript);
            }
            GameObject[] gameArray = realGameObjects.ToArray();
            UnityEngine.Object[] assetArray = realAssets.ToArray();
            if (gameArray.Length > 0)
            {
                if (tabs?.Count == 0)
                {
                    activeIndex = 0;
                    tabs.Insert(0, new TabInfo(null, 0, this, ""));
                    tabs[0].newTab = true;
                    DrawCurrentComponentsContainer();
                }
                bool startingPrefabMode = !onPrefabSceneMode && IsGameObjectInPrefabMode(gameArray[0]);
                if (realGameObjects.Count == 1 && (startingPrefabMode || !IsActiveTabLocked(false)))
                {
                    if (assetArray.Length == 0 && TryReuseTab(gameArray[0], false))
                    {
                        return;
                    }
                    if (IsActiveTabLocked())
                    {
                        AddTabNext(gameArray[0]);
                    }
                    else
                    {
                        SetTargetGameObject(gameArray[0]);
                    }

                }
                else if (!IsActiveTabLocked(false))
                {
                    if (!IsActiveTabLocked())
                    {
                        SetTargetGameObjects(gameArray);
                    }
                    else
                    {
                        AddMultiTabNext(gameArray);
                    }
                }


            }
            if (assetArray.Length > 0 && !lockedAsset && assetInspection && !EditorUtils.AssetsAlreadyTargets(assetArray, this))
            {
                if (AssetTargetMode() == 1)
                {
                    lockedAsset = false;
                }
                if (assetArray.Length == 1)
                {
                    SetTargetAsset(assetArray[0]);
                }
                else
                {
                    SetTargetAssets(assetArray);
                }
            }
            Repaint();
        }

        bool IsAssetImporterEditor(Editor _editor)
        {
            bool isIt = false;
#if UNITY_2020_2_OR_NEWER
            if (_editor is UnityEditor.AssetImporters.AssetImporterEditor)
            {
                isIt = true;
            }
#else
            if (_editor is UnityEditor.Experimental.AssetImporters.AssetImporterEditor)
            {
                isIt = true;
            }
#endif
            return isIt;
        }

        bool IsEditorValid(Editor editor)
        {
            if (editor == null)
            {
                return false;
            }
            if (editor.targets == null)
            {
                return false;
            }
            else
                if (editor.target != null || (editor.targets != null && editor.targets.Length > 0))
                {
                    if (IsAssetImporterEditor(editor))
                    {
                        if (assetImporter == null && assetImporters == null)
                        {
                            return false;
                        }
                        if (editor.targets.Length == 1 && assetImporter != editor.target)
                        {
                            return false;
                        }
                        else if (editor.targets.Length > 1 && assetImporters != editor.targets)
                        {
                            return false;
                        }
                    }
                    return true;
                }
            return false;
        }
        void ReinitializeAssetEditors()
        {
            bool wasLocked = lockedAsset;
            lockedAsset = false;
            if (AssetTargetMode() == 1)
            {
                ResetAssetInspector();
            }
            else if (AssetTargetMode() == 2)
            {
                ResetMultiAssetEditors();
                targetObject = null;
            }

            lockedAsset = wasLocked;
        }

        internal void ReAssignAssetTargets()
        {
            int assetMode = AssetTargetMode();
            if (assetMode == 1)
            {
                SetTargetAsset(targetObject);
            }
            else
            {
                SetTargetAssets(targetObjects);
            }
        }

        public void SetTargetAssets(UnityEngine.Object[] objects, bool overrideLock = false, bool overrideHeight = false)
        {

            if (objects != null && objects.Length == 1)
            {
                bool locked = lockedAsset;
                lockedAsset = false;
                targetObjects = null;
                SetTargetAsset(objects[0]);
                EditorApplication.delayCall += () =>
                {
                    lockedAsset = locked;
                };
                return;
            }
            else if (objects == null || objects.Length == 0)
            {
                CloseAssetView();
                return;
            }
            if (!overrideHeight && AreWeChangingAssetTypes(objects))
            {
                userAssetViewSize = -1;
            }
            bool _override = overrideLock && AssetTargetMode() == 2;
            if (!_override)
            {
                if (lockedAsset && AssetTargetMode() == 2)
                {
                    return;
                }
            }
            Dictionary<Type, List<UnityEngine.Object>> objectsByType = new Dictionary<Type, List<UnityEngine.Object>>();
            List<UnityEngine.Object> folders = new List<UnityEngine.Object>();
            List<UnityEngine.Object> importedObjects = new List<UnityEngine.Object>();
            List<UnityEngine.Object> finalAssets = new List<UnityEngine.Object>();
            alreadyCalculatedHeight = false;
            foreach (UnityEngine.Object obj in objects)
            {
                if (!obj || obj is GameObject && !EditorUtils.IsAPrefabAsset(obj))
                {
                    if (EditorUtils.IsAnImportedObject(obj))
                    {
                        finalAssets.Add(obj);
                        importedObjects.Add(obj);
                    }
                    continue;
                }
                else
                {
                    finalAssets.Add(obj);
                }
                if (PoolCache.IsAssetAFolder(obj))
                {
                    folders.Add(obj);
                    continue;
                }
                Type objType = obj.GetType();
                if (!objectsByType.ContainsKey(objType))
                {
                    objectsByType[objType] = new List<UnityEngine.Object>();
                }
                objectsByType[objType].Add(obj);
            }
            if (folders.Count > 0)
            {
                if (objectsByType.ContainsKey(typeof(UnityObject)))
                {
                    if (!objectsByType.ContainsKey(typeof(DefaultAsset)))
                    {
                        objectsByType.Add(typeof(DefaultAsset), folders);
                    }
                    else
                    {
                        objectsByType[typeof(DefaultAsset)].AddRange(folders);
                    }
                }
                else
                {
                    objectsByType.Add(typeof(UnityObject), folders);
                }
            }
            if (importedObjects.Count > 0)
            {
                if (objectsByType.ContainsKey(typeof(UnityObject)))
                {
                    if (!objectsByType.ContainsKey(typeof(GameObject)))
                    {
                        objectsByType.Add(typeof(GameObject), importedObjects);
                    }
                    else
                    {
                        objectsByType[typeof(GameObject)].AddRange(importedObjects);
                    }
                }
                else
                {
                    objectsByType.Add(typeof(UnityObject), importedObjects);
                }
            }
            var sortedByCount = objectsByType.OrderByDescending(x => x.Value.Count);
            objectsByType = sortedByCount.ToDictionary(x => x.Key, x => x.Value);

            SortedAssetSelection = objectsByType;
            targetObjects = finalAssets.ToArray();

            if (targetObjects != null && targetObjects.Length == 1)
            {
                bool locked = lockedAsset;
                lockedAsset = false;
                targetObjects = null;
                SetTargetAsset(finalAssets[0]);
                EditorApplication.delayCall += () =>
                {
                    lockedAsset = locked;
                };
                return;
            }
            else if (finalAssets == null || finalAssets.Count == 0)
            {
                targetObjects = null;
                ResetMultiAssetEditors();
                CloseAssetView();
                return;
            }

            PrefabMaterialMapManager.DestroyAllMaterialMaps();
            targetObject = null;
            HandleAssetHistory(targetObjects);
            ResetMultiAssetEditors();
            StringBuilder summary = new StringBuilder();
            summary.AppendLine("Found:");
            bool foundMissing = false;
            foreach (var type in objectsByType.Keys)
            {
                if (type == typeof(MissingScriptManager))
                {
                    foundMissing = true;
                }
                int count = objectsByType[type].Count;
                summary.AppendLine($"{count} {type.Name}(s)");
            }
            if (!foundMissing)
            {
                MissingScriptManager.SetInactive();
                MissingScriptManager.ClearData();
            }
            alreadyCalculatedHeight = false;
        }
        void ResetMultiAssetEditors()
        {
            CleanAllAssetEditors();
            if (SortedAssetSelection == null || SortedAssetSelection.Count == 0)
            {
                if (targetObjects == null || targetObjects.Length == 0)
                {
                    return;
                }
                else
                {
                    SetTargetAssets(targetObjects);
                }
            }
            else if (SortedAssetSelection.Count == 1)
            {
                var targets = SortedAssetSelection.Values.ElementAt(0).ToArray();
                Editor.CreateCachedEditor(targets, null, ref assetEditor);
                AssetInfo assetInfo = PoolCache.GetAssetInfo(targets[0]);
                assetImporters = new AssetImporter[targets.Length];
                bool isPrefab = targets[0] is GameObject;
                bool isImportedObject = assetInfo.isImportedObject;
                bool isNested = !assetInfo.isMainAsset;
                HashSet<string> seenPaths = new HashSet<string>();
                assetImporters = targets
                    .Select(t => AssetDatabase.GetAssetPath(t))
                    .Where(path => seenPaths.Add(path))
                    .Select(path => AssetImporter.GetAtPath(path))
                    .ToArray();
                if (assetImporters != null && assetImporters.Length == targets.Length && (!isPrefab || (isImportedObject && !isNested)))
                {
                    DestroyIfNotNull(assetImportSettingsEditor);
                    assetImportSettingsEditor = null;
                    Editor temp = null;
                    Editor.CreateCachedEditor(assetImporters, null, ref temp);
                    if (IsAssetImporterEditor(temp))
                    {
#if UNITY_2020_2_OR_NEWER
                        assetImportSettingsEditor = temp as UnityEditor.AssetImporters.AssetImporterEditor;
#else
                        assetImportSettingsEditor = temp as UnityEditor.Experimental.AssetImporters.AssetImporterEditor;
#endif
                        if (assetImportSettingsEditor == null)
                        {
                            DestroyIfNotNull(temp);
                        }
                        Reflected.UpdateCurrentApplyRevertMethod(assetImportSettingsEditor, assetEditor);
                    }
                    else
                    {
                        DestroyIfNotNull(temp);
                    }
                }
                if (isPrefab || isImportedObject)
                {
                    ReinitializePrefabComponentEditors();
                }
            }
            else
            {
                Editor.CreateCachedEditor(targetObjects[0], null, ref assetEditor);
            }
            TryGatherTimeControls();
            SetAllAssetEditorsDebugTo(debugAsset);
            DrawCurrentAssets();
            Repaint();
        }

        internal void TryGatherTimeControls()
        {
            object avatarOwner = null;
            if (assetEditor.GetType().ToString() == "UnityEditor.Graphs.AnimationStateMachine.AnimatorStateTransitionInspector")
            {
                avatarOwner = Reflected.GetTransitionPreview(assetEditor);
            }
            Reflected.GatherTimeControl(assetEditor, avatarOwner);
            if (Reflected.timeControlledEditor != null)
            {
                return;
            }
            else
            {
                Reflected.GatherTimeControl(assetImportSettingsEditor);
            }
            if (Reflected.timeControlledEditor == null)
            {
                Reflected.GatherTimeControl(Reflected.GetModelClipEditor(assetImportSettingsEditor));
            }

        }
        void DrawMultiAssetSummary()
        {
            if (SortedAssetSelection == null || SortedAssetSelection.Count == 0 || targetObjects == null || targetObjects.Length == 0)
            {
                return;
            }
            EditorGUIUtility.wideMode = true;
            if (!assetsCollapsed)
            {
                int missingCount = 0;
                if (MissingScriptManager.IsMulti)
                {
                    missingCount = MissingScriptManager.Count() - 1;
                }
                if (targetObjects == null || targetObjects.Length == 0)
                {
                    return;
                }
                GUIStyle headerStyle = CustomGUIStyles.HeaderStyle;
                EditorGUILayout.BeginVertical();
                GUILayout.Space(10);
                EditorGUILayout.BeginHorizontal(GUILayout.Height(50));
                GUILayout.Space(8);
                Rect textureRect = GUILayoutUtility.GetRect(40, 40, GUILayout.ExpandWidth(false));
                GUI.DrawTexture(textureRect, CustomGUIContents.MultiAsset.image);
                EditorGUILayout.LabelField(targetObjects.Length + missingCount + " Objects", headerStyle);
                EditorGUILayout.EndHorizontal();
                EditorGUILayout.EndVertical();
                Rect rect1 = GUILayoutUtility.GetLastRect();
                EditorUtils.DrawLineUnderRect(rect1, CustomColors.HardShadow);
                EditorUtils.DrawLineUnderRect(rect1, CustomColors.SoftShadow, 0, 4);
                EditorUtils.DrawLineOverRect(rect1, CustomColors.HardShadow);
                EditorUtils.DrawLineOverRect(rect1, -1);
                EditorGUI.DrawRect(rect1, CustomColors.SubtleBright);
                GUILayout.Space(2);
                EditorGUILayout.BeginHorizontal();
                GUILayout.Space(10);
                EditorGUILayout.LabelField("Narrow the Selection:", EditorStyles.label);
                GUIContent buttonContent = CustomGUIContents.EmptyContent;
                EditorGUILayout.EndHorizontal();
                GUILayout.Space(5);
                for (int i = 0; i < SortedAssetSelection.Keys.Count; i++)
                {
                    var type = SortedAssetSelection.Keys.ElementAt(i);
                    buttonContent = CustomGUIContents.AssetContent(SortedAssetSelection, i);
                    GUIStyle buttonsStyle = CustomGUIStyles.MultiAssetButtonsStyle(position.width);
                    EditorGUILayout.BeginHorizontal();
                    GUILayout.Space(20);
                    if (GUILayout.Button(buttonContent, buttonsStyle, GUILayout.MaxHeight(20)))
                    {
                        bool lockWas = lockedAsset;
                        lockedAsset = false;
                        //Selection.objects = SortedAssetSelection[type].ToArray();
                        SetTargetAssets(SortedAssetSelection[type].ToArray());
                        EditorApplication.delayCall += () =>
                        {
                            lockedAsset = lockWas;
                        };
                    }
                    EditorGUILayout.EndHorizontal();
                    Rect rect = GUILayoutUtility.GetLastRect();
                    if (rect.Contains(Event.current.mousePosition))
                    {
                        if (!middleScrolling && !resizingAssetView)
                            EditorGUIUtility.AddCursorRect(rect, MouseCursor.Link);
                    }
                }
                GUILayout.Space(10);
            }
        }


        void FixNullAssets()
        {
            if (targetObjects == null || targetObjects.Length == 0)
            {
                return;
            }
            bool nulls = false;
            for (int i = 0; i < targetObjects.Length; i++)
            {
                if (targetObjects[i] == null)
                {
                    nulls = true;
                }
            }
            if (nulls)
            {
                SetTargetAssets(targetObjects);
                targetObjects = null;
            }
        }

        public void SetTargetAsset(UnityObject ob, bool overrideLock = false, bool overrideHeight = false)
        {
            bool _override = ob != null && overrideLock && assetInspection;
            if (!_override)
            {
                if (ob == null || lockedAsset || !assetInspection)
                {
                    if (assetEditor == null && lockedAsset)
                    {
                        lockedAsset = false;
                    }
                    else
                    {
                        return;
                    }
                }
            }

            if (!CheckForApplyRevertOnClose())
            {
                return;
            }
            if (ignoreFolders && PoolCache.IsAssetAFolder(ob))
            {
                return;
            }
            if (!overrideHeight && AreWeChangingAssetTypes(ob))
            {

                userAssetViewSize = -1;
            }
            HandleAssetHistory(new UnityObject[] { ob });
            targetObjects = null;
            {
                PrefabMaterialMapManager.DestroyAllMaterialMaps();
                targetObject = ob;
                DoReinitializePrefabComponentEditors();
                ResetAssetInspector(EditorUtils.IsAPrefabAsset(targetObject));
                ResetAssetViewSize();
                EditorApplication.delayCall += Repaint;
            }
            if (targetObject == null || targetObject is not MissingScriptManager)
            {
                MissingScriptManager.SetInactive();
                MissingScriptManager.ClearData();
            }

        }

        bool AreWeChangingAssetTypes(UnityObject newTarget)
        {
            if (newTarget == null)
            {
                return false;
            }
            if (PoolCache.IsAPrefabAsset(newTarget))
            {
                return true;
            }
            int mode = AssetTargetMode();
            if (mode == 0)
            {
                return true;
            }
            bool isMulti = mode == 2;
            bool isSameType;
            if (isMulti)
            {
                return true;
            }
            else
            {
                isSameType = targetObject.GetType() == newTarget.GetType();
            }
            return !isSameType;
        }
        bool AreWeChangingAssetTypes(UnityObject[] newTargets)
        {
            if (newTargets == null || newTargets.Length == 0)
            {
                return false;
            }
            if (newTargets[0] is GameObject)
            {
                return true;
            }
            foreach (UnityObject obj in newTargets)
            {
                if (obj == null)
                {
                    return true;
                }
            }
            Type newTargetType = newTargets[0].GetType();
            if (newTargetType == null)
            {
                return true;
            }

            for (int i = 1; i < newTargets.Length; i++)
            {
                if (newTargets[i].GetType() != newTargetType)
                {
                    return true;
                }
            }
            int mode = AssetTargetMode();

            if (mode == 0 || mode == 1)
            {
                return true;
            }

            bool isMulti = mode == 2;
            bool isSameType;

            if (isMulti)
            {
                isSameType = SortedAssetSelection != null && SortedAssetSelection.Count == 1;
                if (!isSameType)
                {
                    return true;
                }
                Type currentType = SortedAssetSelection.Keys.ElementAt(0);
                isSameType = currentType == newTargetType;
            }
            else
            {
                isSameType = targetObject.GetType() == newTargetType;
            }

            return !isSameType;
        }

        void SaveCurrentTargetOrTargets()
        {

            int mode = AssetTargetMode();
            if (mode == 1)
            {
                EditorUtils.SaveAsset(targetObject, assetEditor, assetImportSettingsEditor);
            }
            else if (mode == 2)
            {
                EditorUtils.SaveAssets(targetObjects, assetEditor, assetImportSettingsEditor);
            }
            Repaint();
        }
        internal void ResetAssetViewSize(bool keepCollapsed = false, bool skipRebuild = false)
        {
            if (!assetsCollapsed)
            {
                alreadyCalculatedHeight = false;
                if (userHeight != suggestedHeight)
                {
                    userHeight = suggestedHeight;
                }
            }

            if (skipRebuild)
            {
                return;
            }
            RepaintForAWhile();
            DrawCurrentAssets();
            ReloadPreview();

        }
        bool ShouldDrawImportSettings()
        {
            if (assetEditor == null)
            {
                return false;
            }
            if (showImportSettings || assetOnlyMode)
            {
                return true;
            }
            return false;
        }
        internal void ForceCollapseOrDefault(int forceSpecific = 0)
        {
            EndAssetViewResize();
            mousePositionOnAssetBarClick = Vector2.zero;
            if (forceSpecific == 0)
            {
                if (assetOnlyMode)
                {
                    bool areAllExpanded = PrefabComponentMapManager.AreAllExpanded();
                    if (areAllExpanded)
                    {
                        PrefabComponentMapManager.SetAllComponentsTo(false);
                    }
                    ReinitializeComponentEditors();
                    assetOnlyMode = false;
                    assetsCollapsed = true;
                    userHeight = 1;

                }
                if (!assetsCollapsed)
                {
                    userHeight = 1;
                    rawUserHeight = userHeight;
                    assetsCollapsed = true;
                    assetOnlyMode = false;
                    bool areAllExpanded = PrefabComponentMapManager.AreAllExpanded();
                    if (areAllExpanded)
                    {
                        PrefabComponentMapManager.SetAllComponentsTo(false);
                    }
                    if (assetEditor._HasPreviewGUI())
                    {
                        rawUserHeight -= IntGetPreviewHeight();
                    }


                }
                else
                {
                    assetsCollapsed = false;
                    alreadyCalculatedHeight = false;

                }
                lastKnownHeight = rawUserHeight;
            }
            else if (forceSpecific == 1)
            {
                userHeight = suggestedHeight;
                rawUserHeight = userHeight;
            }
            else if (forceSpecific == 2)
            {

                userHeight = 1;
                rawUserHeight = userHeight;
                if (assetEditor != null)
                {
                    rawUserHeight -= IntGetPreviewHeight();
                }
            }
            if (!assetsCollapsed)
            {
                TryGatherTimeControls();

            }
            DrawCurrentAssets();
            RepaintForAWhile();
        }

        void ReloadPreview()
        {
            if (assetEditor == null)
            {
                return;
            }
            if (assetEditor._HasPreviewGUI())
            {
                assetEditor.ReloadPreviewInstances();
            }
            else if (assetImportSettingsEditor != null)
            {
                assetImportSettingsEditor.ReloadPreviewInstances();
            }
        }

        void DrawHeader(Editor editor)
        {
            if (editor == null)
            {
                return;
            }
            GUILayout.Space(1);
            DoDrawHeader(editor);
            Rect rect = GUILayoutUtility.GetLastRect();
            rect.y = 1;
            EditorUtils.DrawLineOverRect(rect, CustomColors.SimpleBright);
            rect.width = 20;
            rect.y = rect.height - 15;
            rect.height = 15;
            int mode = AssetTargetMode();
            if (mode == 2)
            {
                bool allMaterials = targetObjects.All(t => t is Material);
                if (allMaterials)
                {
                    EditorGUI.DrawRect(rect, CustomColors.DefaultInspector);
                }
            }
            else if (mode == 1 && targetObject is Material)
            {
                EditorGUI.DrawRect(rect, CustomColors.DefaultInspector);
            }
        }

        void DrawPreview(Rect rect)
        {
            bool videoClip = targetObject is VideoClip;
            bool hasImportPreview = assetImportSettingsEditor != null && assetImportSettingsEditor.HasPreviewGUI();


            if (assetEditor != null && assetEditor.HasPreviewGUI() && assetEditor.target && (!videoClip && !hasImportPreview))
            {
                EditorGUI.BeginChangeCheck();
                assetEditor.DrawPreview(rect);
                if (EditorGUI.EndChangeCheck())
                {

                    TryGatherTimeControls();
                }
            }
            else if (assetImportSettingsEditor != null && assetImportSettingsEditor.target != null)
            {

                EditorGUI.BeginChangeCheck();
                EditorGUI.BeginChangeCheck();
                try
                {
                    assetImportSettingsEditor.DrawPreview(rect);
                }
                catch (MissingReferenceException)
                {

                    //Unity's SpriteAtlasImporterInspector internally accesses a destroyed preview texture.
                    //Nothing we can do about it.
                    ResetAssetInspector();
                }
                if (EditorGUI.EndChangeCheck())
                {
                    TryGatherTimeControls();
                }
            }
            Rect rect1 = GUILayoutUtility.GetLastRect();
            if (IsAPrefabTarget())
            {
                {
                    TryGatherTimeControls();
                }
                EditorUtils.DrawLineOverRect(rect1, CustomColors.GradientShadow, 2);
                EditorUtils.DrawLineOverRect(rect1, CustomColors.GradientShadow, 2);
            }
            else if (ShouldDrawImportSettings())
            {
                EditorUtils.DrawLineOverRect(rect1, CustomColors.GradientShadow, 0);
                EditorUtils.DrawLineOverRect(rect1, CustomColors.GradientShadow, 0);
            }
            rect1.height = 20;
            rect1.width = 30;
            /*if (IsAModelTarget())
            {
                if (Event.current.rawType == EventType.Used && rect1.Contains(Event.current.mousePosition))
                {
                    TryGatherTimeControls();
                }
            }*/
        }
        void SetAllTabsDebugTo(bool debug)
        {
            if (tabs != null)
            {
                for (int i = 0; i < tabs.Count; i++)
                {
                    tabs[i].debug = debug;
                }
            }
        }
        void ResetAssetInspector(bool isPrefab = false)
        {
            if (assetEditor != null)
            {
                ResetAssetViewSize(false, true);
            }
            DestroyIfNotNull(assetEditor);
            assetEditor = null;
            DestroyAllIfNotNull(prefabMaterialEditors);
            prefabMaterialEditors = null;
            DestroyIfNotNull(assetImportSettingsEditor);
            assetImportSettingsEditor = null;
            if (assetImporter != null)
            {
                assetImporter = null;
            }
            if (isPrefab)
            {
                ResetAssetViewSize();
                GameObject prefab = targetObject as GameObject;
                Editor.CreateCachedEditor(targetObject, null, ref assetEditor);
                if (assetImporter == null || assetImporter.assetPath != AssetDatabase.GetAssetPath(targetObject))
                {
                    DestroyIfNotNull(assetImportSettingsEditor);
                    assetImportSettingsEditor = null;
                    assetImporter = AssetImporter.GetAtPath(AssetDatabase.GetAssetPath(targetObject));
                }
                ReinitializePrefabComponentEditors();
                Repaint();
                return;
            }
            Editor.CreateCachedEditor(targetObject, null, ref assetEditor);
            if (assetEditor == null)
            {
                Debug.LogWarning("Asset editor is null!");
            }
            if (assetEditor && EditorUtils.IsMainAsset(targetObject))
            {
                if (assetImporter == null || assetImporter.assetPath != AssetDatabase.GetAssetPath(targetObject))
                {
                    DestroyIfNotNull(assetImportSettingsEditor);
                    assetImportSettingsEditor = null;
                    assetImporter = null;
                    assetImporter = AssetImporter.GetAtPath(AssetDatabase.GetAssetPath(targetObject));
                    if (assetImporter != null)
                    {
                        Editor _editor = null;
                        Editor.CreateCachedEditor(assetImporter, null, ref _editor);

                        if (IsAssetImporterEditor(_editor))
                        {

#if UNITY_2020_2_OR_NEWER
                            assetImportSettingsEditor = _editor as UnityEditor.AssetImporters.AssetImporterEditor;
#else
                            assetImportSettingsEditor = _editor as UnityEditor.Experimental.AssetImporters.AssetImporterEditor;
#endif                          
                            if (assetImportSettingsEditor == null)
                            {
                                DestroyIfNotNull(_editor);
                            }
                            Reflected.UpdateCurrentApplyRevertMethod(assetImportSettingsEditor, assetEditor);
                        }
                        else
                        {
                            DestroyIfNotNull(_editor);
                        }
                    }
                }
                if (targetObject is Material)
                {
                    UnityEditorInternal.InternalEditorUtility.SetIsInspectorExpanded(targetObject, ShouldDrawImportSettings());
                }
            }
            TryGatherTimeControls();
            SetAllAssetEditorsDebugTo(debugAsset);
            DrawCurrentAssets();
            Repaint();
        }
        static bool EditorBefore2021_2()
        {
            bool version = true;
#if UNITY_2021_2_OR_NEWER
            version = false;
#endif
            return version;
        }
        void DoOpenPrefabInScene(string path, GameObject item)
        {
            if (path == "")
            {
                return;
            }
#if UNITY_2021_2_OR_NEWER
            if (item == null)
            {
                return;
            }
            EditorUtils.OpenGameObjectInPrefabContext(item);

#else
           Reflected.OpenPrefab(path, item);
#endif

        }

        internal static void DoOpenPrefabInIsolation(string path, GameObject item)
        {
            if (path == "")
            {
                return;
            }
#if UNITY_2021_2_OR_NEWER
            EditorUtils.OpenGameObjectInPrefabIsolation(item);
#else
            DoOpenPrefab(path);
#endif

        }


        internal static void DoOpenPrefab(string path)
        {

            if (path == "")
            {
                return;
            }
#if UNITY_2021_2_OR_NEWER
            UnityEditor.SceneManagement.PrefabStageUtility.OpenPrefab(path);
#else
            Reflected.OpenPrefab(path);
#endif

        }
        void OpenPrefab()
        {
            onPrefabSceneMode = false;
            FixActiveIndex();
            if (!assetsCollapsed)
            {
                if (assetOnlyMode)
                {
                    ResetMaximizedView();
                }
                ForceCollapseOrDefault(0);
            }
            DoOpenPrefab(AssetDatabase.GetAssetPath(targetObject));
            if (!openPrefabsInNewTab)
            {
                SetTargetGameObject(GetActiveTab().target as GameObject);
            }
            CloseAssetView();
        }
        bool HandleMultiAssetNulls()
        {
            if (AssetTargetMode() == 2)
            {
                List<UnityObject> objects = new List<UnityObject>(targetObjects);
                foreach (var obj in targetObjects)
                {
                    if (obj == null)
                    {
                        objects.Remove(obj);
                    }
                }
                if (objects.Count == targetObjects.Length)
                {
                    return true;
                }
                if (objects.Count == 0)
                {
                    CloseAssetView();
                    return false;
                }
                UnityObject[] _objects = objects.ToArray();
                bool wasLocked = lockedAsset;
                lockedAsset = false;
                if (_objects.Length == 1)
                {
                    SetTargetAsset(_objects[0]);
                    Repaint();
                }
                else
                {
                    SetTargetAssets(_objects);
                    Repaint();
                }
                EditorApplication.delayCall += () =>
                {
                    lockedAsset = wasLocked;
                };
            }
            return false;
        }

        static bool IsAnyAPrefabAsset(GameObject[] gameObjects)
        {
            foreach (var gameObject in gameObjects)
            {
                if (EditorUtils.IsAPrefabAsset(gameObject))
                {
                    return true;
                }
            }
            return false;
        }
        void DrawAssetBar(bool isFolder, bool prefabMode, bool isDirty, bool multiMode = false, bool sameType = false)
        {
            if (!IsValidAssetTarget())
            {
                return;
            }
            multiMode = AssetTargetMode() == 2;
            bool importedObjectMode = false;
            Color color = GUI.backgroundColor;
            if (EditorUtils.IsLightSkin())
            {
                GUI.backgroundColor += Color.white / 8;
            }
            GUIStyle toolbarButton = CustomGUIStyles.AssetToolbarButton;
            GUIContent content = CustomGUIContents.EmptyContent;
            GUIContent emptyButton = CustomGUIContents.NoneContent;
            string extension = "";
            bool isMainAsset;
            bool isMissingScript = !multiMode && MissingScriptManager.IsActive() && targetObject is MissingScriptManager;
            AssetInfo assetInfo = null;
            if (!multiMode)
            {
                assetInfo = PoolCache.GetAssetInfo(targetObject);
                isMainAsset = assetInfo.isMainAsset;
                if (isMainAsset)
                {
                    extension = assetInfo.extension;
                }
            }
            else
            {
                if (sameType)
                {
                    assetInfo = PoolCache.GetAssetInfo(targetObjects[0]);
                    string _extension = assetInfo.extension;
                    if (prefabMode && _extension == ".prefab")
                    {
                        extension = "Prefab";
                    }
                    else if (isFolder)
                    {
                        extension = "Folder";
                    }
                    else
                    {
                        string niceType = assetInfo.niceType;
                        if (assetInfo.isImportedObject)
                        {
                            niceType = "Imported Object";
                            importedObjectMode = true;
                        }
                        extension = niceType;
                    }
                }
                else
                {
                    extension = "Asset";
                }
            }
            string assetName;
            string modified = "";
            if (IsAssetImportSettingsDirty())
            {
                modified = "*";
            }
            if (!multiMode)
            {
                prefabMode = assetInfo.isPrefab || assetInfo.isImportedObject;
                if (prefabMode && extension == ".prefab")
                {
                    assetName = " <b>" + targetObject.name + modified + "</b> (Prefab)";
                }
                else
                {
                    string optExtension = "";
                    if (targetObject is AudioClip || targetObject is TextAsset || targetObject is UnityEngine.Texture || targetObject is GameObject)
                    {
                        optExtension += extension;
                    }
                    string niceType = assetInfo.niceType;
                    if (prefabMode && extension != ".prefab")
                    {
                        niceType = "Imported Object";
                        importedObjectMode = true;
                    }
                    /*
                    if (EditorUtils.IsLightSkin())
                    {
                        assetName = targetObject.name + optExtension +  modified +"  (" + niceType + ")";
                    }
                    else*/
                    {
                        assetName = "<b>" + targetObject.name + optExtension + modified + " </b> (" + niceType + ")";
                    }
                    if (isMissingScript)
                    {
                        var missingScriptName = (targetObject as MissingScriptManager)?.assetName;
                        assetName = "<b>" + missingScriptName + "</b> (Missing Script)";
                    }
                }
            }
            else
            {
                int missingCount = 0;
                if (MissingScriptManager.IsMulti)
                {
                    missingCount = MissingScriptManager.Count() - 1;
                }
                assetName = "<b>Selecting " + (targetObjects.Length + missingCount) + " " + extension;
            }
            if (extension == "")
            {
                assetName = assetName.Replace("Default Asset", "Folder");
            }
            string plural = multiMode ? "s" : "";
            assetName = assetName.Replace("UnityEngine.", "");
            content.text = assetName;
            if (multiMode)
            {
                if (assetName[assetName.Length - 1] != 's')
                {
                    content.text += plural + "</b>" + modified;
                }
            }
            if (EditorUtils.IsLightSkin())
            {
                content.text = content.text.Replace("<b>", "");
                content.text = content.text.Replace("</b>", "");
            }
            emptyButton.text = "";
            if (content.image == null)
            {
                if (!multiMode || sameType)
                {
                    if (prefabMode && !importedObjectMode)
                    {
                        content.image = CustomGUIContents.PrefabIcon.image;
                    }
                    else if (importedObjectMode)
                    {
                        content.image = CustomGUIContents.ImportedIcon.image;
                    }
                    else if (isFolder)
                    {
                        content.image = CustomGUIContents.FolderIcon.image;
                    }
                    else if (!multiMode)
                    {
                        content.image = assetInfo.icon;
                    }
                    else
                    {
                        content.image = AssetPreview.GetMiniTypeThumbnail(targetObjects[0].GetType());
                    }
                }
                else
                {
                    content.image = CustomGUIContents.TabMulti.image;
                }
            }
            if (assetOnlyMode)
            {
                emptyButton.tooltip = "Click to Exit Asset-Only Mode\nDrag to Drag the Asset";
            }
            else
            {
                if (!assetsCollapsed)
                {


                    emptyButton.tooltip = "Click to Collapse\nDrag to Drag the Asset";
                }
                else
                {


                    emptyButton.tooltip = "Click to Expand\nDrag to Drag the Asset";
                }
            }


            emptyButton.tooltip += plural;
            if (EditorGUIUtility.isProSkin)
            {
                if (!assetOnlyMode)
                {
                    GUI.backgroundColor += CustomColors.DarkHistoryButton * 0.4f;
                }
                else
                {
                    GUI.backgroundColor *= 0.75f;
                }
            }
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.BeginHorizontal(CustomGUIStyles.InspectorButtonStyle, GUILayout.ExpandWidth(true));
            if (debugAsset && !assetOnlyMode)
            {
                if (GUILayout.Button(CustomGUIContents.DebugIconON, toolbarButton, GUILayout.Width(25)))
                {
                    debugAsset = false;
                    DrawCurrentAssets();
                    assetBox.MarkDirtyRepaint();
                }
                Rect rect1 = GUILayoutUtility.GetLastRect();
                EditorUtils.DrawFadeToBottom(rect1, CustomColors.SoftBright, 5);
            }
            else if (assetOnlyMode)
            {
                GUI.enabled = historyAssets != null && historyAssets.Count > 0;
                GUI.backgroundColor = Color.white;
                if (EditorUtils.IsLightSkin())
                {
                    GUI.backgroundColor = new Color(0.6f, 0.6f, 0.75f);
                }
                else
                {
                    GUI.backgroundColor += CustomColors.DarkHistoryButton * 0.4f;
                }
                Color color1 = GUI.backgroundColor;
                if (GUI.enabled && EditorGUIUtility.isProSkin)
                {
                    GUI.backgroundColor += CustomColors.FadeBlue * 0.3f;
                }
                if (GUILayout.Button(CustomGUIContents.HistoryButton, toolbarButton, GUILayout.Width(25)))
                {
                    ShowHistoryContextMenu();
                }
                Rect rect1 = GUILayoutUtility.GetLastRect();
                EditorUtils.DrawFadeToBottom(rect1, CustomColors.SoftBright, 5);
                GUI.enabled = true;
                GUI.backgroundColor = color1;
            }
            else
            {
                GUILayout.Space(5);
            }
            GUIStyle labelStyle = CustomGUIStyles.AlignedLabelStyle;
            Texture2D icon = content.image as Texture2D;
            content.image = null;
            if (icon != null)
            {
                GUILayout.Label(icon, GUILayout.Width(20), GUILayout.Height(20));
            }
            if (assetOnlyMode)
            {
                labelStyle.contentOffset = new Vector2(0, 0);
            }
            else
            {
                labelStyle.contentOffset = new Vector2(0, -1);
            }
            GUILayout.Label(content, labelStyle);
            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();
            Rect barRect = GUILayoutUtility.GetLastRect();
            DrawAssetTopBarButtons(toolbarButton, prefabMode, isDirty, multiMode);
            EditorGUILayout.EndHorizontal();
            Rect adjustedRect = GUILayoutUtility.GetLastRect();
            adjustedRect.x -= 5;
            adjustedRect.width += 10;
            if (debugAsset && !assetOnlyMode)
            {
                barRect.x += 25;
                barRect.width -= 25;
            }

            EditorUtils.DrawLineOverRect(adjustedRect, CustomColors.HarderBright, 0);
            if (EditorGUIUtility.isProSkin)
            {
                EditorUtils.DrawLineOverRect(adjustedRect, CustomColors.SubtleBlue, 0);
                if (userHeight > 55)
                {
                    EditorUtils.DrawLineUnderRect(adjustedRect, CustomColors.HardShadow, -1);
                }
                GUI.backgroundColor = color;
            }
            else
            {
                EditorUtils.DrawLineOverRect(adjustedRect, CustomColors.SoftShadow, 1);
            }
            Event _event = Event.current;
            if (_event.type == EventType.Repaint)
            {
                assetViewRect = adjustedRect;
            }
            if (!resizingAssetView && barRect.Contains(_event.mousePosition))
            {
                if (_event.button == 0)
                {
                    if (_event.type == EventType.MouseDown)
                    {
                        mousePositionOnAssetBarClick = _event.mousePosition;
                    }
                    if (_event.type == EventType.MouseDrag)
                    {
                        if (mousePositionOnAssetBarClick != Vector2.zero && Vector2.Distance(mousePositionOnAssetBarClick, _event.mousePosition) > 2)
                        {
                            GUIUtility.hotControl = 0;
                            DragAndDrop.PrepareStartDrag();
                            if (multiMode)
                            {
                                DragAndDrop.objectReferences = targetObjects;
                                DragAndDrop.StartDrag("Dragging " + targetObjects.Length + " " + extension);
                                _event.Use();
                            }
                            else
                            {
                                DragAndDrop.objectReferences = new UnityEngine.Object[] { targetObject };
                                DragAndDrop.StartDrag("Dragging " + targetObject.name);
                                _event.Use();
                            }
                        }
                    }
                }
            }
            GUI.backgroundColor = color;
            if (GUI.Button(barRect, emptyButton, GUIStyle.none))
            {
                if (_event.button == 0)
                {
                    ForceCollapseOrDefault();
                }
                else if (_event.button == 1)
                {
                    AssetTopBarMenus(prefabMode, multiMode);
                }
                else if (_event.button == 2)
                {
                    if (!CheckForApplyRevertOnClose())
                    {
                        return;
                    }
                    CloseAssetView();
                }
            }
            Rect rect = EditorUtils.GetLastLineRect();
            EditorUtils.DrawLineOverRect(rect, CustomColors.SimpleShadow, 2);
            if (EditorGUIUtility.isProSkin)
            {
                EditorUtils.DrawLineUnderRect(rect, CustomColors.HardShadow, -1, 1);
            }
            else
            {
                EditorUtils.DrawLineUnderRect(rect, CustomColors.MediumShadow, -1, 1);
            }
        }

        void OpenMaximizedAssetMode()
        {
            bool wereAllCollapsed = PrefabComponentMapManager.AreAllCollapsed(debugAsset);
            assetOnlyMode = true;
            assetsCollapsed = false;
            maximizeMode = 0;
            ResetAssetViewSize();
            if (wereAllCollapsed)
            {
                PrefabComponentMapManager.SetAllComponentsTo(true);
            }
            RepaintForAWhile();
        }
        void InitiateMovementRecording(Component component, GameObject _targetGameObject)
        {
            bool nullComponent = !component && _targetGameObject;
            bool differentGameObjects = false;
            if (!nullComponent)
            {
                var gameObject = component.gameObject;
                differentGameObjects = gameObject != _targetGameObject;
                Undo.RecordObject(gameObject, "Component Modification");
            }
            if (differentGameObjects || nullComponent)
            {
                Undo.RecordObject(_targetGameObject, "Component Modification");
            }
        }
        void FinalizeMovementRecording(Component component, GameObject _targetGameObject, string operationName = "Component Drag")
        {
            bool nullComponent = !component && _targetGameObject;
            bool differentGameObjects = false;
            if (!nullComponent)
            {
                var gameObject = component.gameObject;
                differentGameObjects = gameObject != _targetGameObject;
                EditorUtility.SetDirty(gameObject);
            }
            if (differentGameObjects || nullComponent)
            {
                EditorUtility.SetDirty(_targetGameObject);
            }
            Undo.SetCurrentGroupName(operationName);
            Undo.CollapseUndoOperations(Undo.GetCurrentGroup());
            pendingOperation.consumed = true;
            pendingOperation = null;
        }
        void DrawAssetTopBarButtons(GUIStyle toolbarButton, bool prefabMode, bool isDirty, bool multiMode = false)
        {
            if (AssetTargetMode() == 0)
            {
                return;
            }
            bool sameType = false;
            AssetInfo assetInfo = null;
            bool importObjectMode = false;
            if (multiMode)
            {
                sameType = SortedAssetSelection.Count == 1;
            }
            else
            {
                assetInfo = PoolCache.GetAssetInfo(targetObject);
                importObjectMode = assetInfo.isImportedObject;
            }
            if (!EditorUtils.IsLightSkin())
            {
                GUI.backgroundColor += CustomColors.SubtleBlue;
            }
            else
            {
                GUI.backgroundColor = Color.white;
            }
            GUIContent lockAssetContent = CustomGUIContents.AssetUnlocked;
            if (lockedAsset)
            {
                lockAssetContent = CustomGUIContents.AssetLocked;
            }
            GUIContent openContent = CustomGUIContents.OpenPrefabContent;
            if (multiMode)
            {
                isDirty = IsAnyAssetDirty(targetObjects) || IsAssetImportSettingsDirty();
                if (prefabMode && sameType)
                {
                    importObjectMode = EditorUtils.IsAnImportedObject(targetObjects[0]);
                }
            }
            else
            {
                isDirty = IsAssetDirty(targetObject);
            }
            float currentX = position.width;
            Rect rect = GUILayoutUtility.GetLastRect();
            float y = rect.y;
            int buttonHeight = 24;
            int buttonWidth = 26;
            if (EditorGUIUtility.isProSkin)
            {
                GUI.backgroundColor -= CustomColors.SubtleBlue;
            }
            Color color = GUI.backgroundColor;
            if (EditorGUIUtility.isProSkin)
            {
                GUI.backgroundColor = Color.white;
                GUI.backgroundColor += CustomColors.CloseRed * 1.5f;
            }
            else
            {
                GUI.backgroundColor -= CustomColors.LightSkinRed * 1.75f;
            }
            if (GUI.Button(new Rect(currentX - buttonWidth, y, buttonWidth, buttonHeight), CustomGUIContents.CloseAsset, toolbarButton))
            {
                if (!CheckForApplyRevertOnClose())
                {
                    return;
                }
                CloseAssetView();
            }
            Rect buttonRect = new Rect(currentX - buttonWidth, y, buttonWidth, buttonHeight);
            EditorUtils.DrawFadeToBottom(buttonRect, CustomColors.VerySoftShadow, 10);
            EditorUtils.DrawFadeToBottom(buttonRect, CustomColors.SoftBright, 4);
            currentX -= buttonWidth;
            GUI.backgroundColor = color;
            currentX -= buttonWidth;
            if (assetOnlyMode)
            {
                if (GUI.Button(new Rect(currentX, y, buttonWidth, buttonHeight), CustomGUIContents.MinimizeContent, toolbarButton))
                {
                    ResetMaximizedView();
                }
            }
            else
            {
                if (showMaximizeButton)
                {
                    if (GUI.Button(new Rect(currentX, y, buttonWidth, buttonHeight), CustomGUIContents.MaximizeContent, toolbarButton))
                    {
                        assetOnlyMode = true;
                        this.ReinitializeComponentEditors();
                        assetsCollapsed = false;
                        ResetAssetViewSize();
                        Repaint();
                    }
                    currentX -= buttonWidth;

                }
                if (GUI.Button(new Rect(currentX, y, buttonWidth, buttonHeight), lockAssetContent, toolbarButton))
                {
                    lockedAsset = !lockedAsset;
                }
                if (lockedAsset)
                {
                    if (!EditorUtils.IsLightSkin())
                    {
                        EditorGUI.DrawRect(new Rect(currentX, y, buttonWidth - 1, buttonHeight), CustomColors.SimpleBright);
                    }
                    else
                    {
                        EditorGUI.DrawRect(new Rect(currentX, y, buttonWidth - 1, buttonHeight), CustomColors.ButtonEnabled);
                    }
                }
            }
            bool popup = false;
            if (EditorUtils.IsShiftOrAltHeldInWindow() && rect.height > 1)
            {
                Vector2 mousePos = Event.current.mousePosition;
                popup = mousePos.y < rect.yMax + 5 && mousePos.y > rect.yMax - 25 && mousePos.x > position.width / 1.5f;
            }
            GUIContent _content = popup ? CustomGUIContents.InspectAssetPopup : CustomGUIContents.InspectAssetNormal;
            int inspectButtonWidth = popup ? 33 : buttonWidth;
            currentX -= inspectButtonWidth;

            if (GUI.Button(new Rect(currentX, y, inspectButtonWidth, buttonHeight), _content, toolbarButton))
            {
                if (multiMode)
                {
                    PopUpInspectorWindow(targetObjects, popup);
                }
                else
                {
                    PopUpInspectorWindow(new UnityObject[] { targetObject }, popup);
                }
            }

            /*
            if (multiMode && sameType && targetObjects != null && targetObjects[0] is Material)
            {
                GUI.enabled = false;
            }*/
            if (!multiMode && AssetTargetMode() == 1 && targetObject is Material)
            {
                UnityEditorInternal.InternalEditorUtility.SetIsInspectorExpanded(targetObject, ShouldDrawImportSettings());
            }
            else if (multiMode && sameType && AssetTargetMode() == 2 && targetObjects[0] is Material)
            {
                UnityEditorInternal.InternalEditorUtility.SetIsInspectorExpanded(targetObjects[0], ShouldDrawImportSettings());
            }
            GUI.enabled = true;
            if ((isDirty && (!multiMode || sameType)))
            {
                currentX -= buttonWidth;
                if (GUI.Button(new Rect(currentX, y, buttonWidth, buttonHeight), CustomGUIContents.SaveAsset, toolbarButton))
                {
                    SaveCurrentTargetOrTargets();
                }
            }
            if (prefabMode && !importObjectMode && (!multiMode || sameType))
            {
                if (multiMode)
                {
                    GUI.enabled = false;
                }
                else if (!(targetObject is GameObject))
                {
                    GUI.enabled = false;
                }
                currentX -= 54;
                openContent.text = "OPEN";
                if (GUI.Button(new Rect(currentX, y, 54, buttonHeight), openContent, CustomGUIStyles.AssetToolbarButton_Open))
                {
                    OpenPrefab();
                }
            }
            GUI.enabled = true;
            bool onlyHeader = assetInfo != null && assetInfo.singleHeader && !IsAPrefabTarget();
            if (IsMissingAssetTarget() || (multiMode && !sameType) || onlyHeader || debugAsset || IsAPrefabTarget()) /* || targetObject is Material*/
            {
                GUI.enabled = false;
            }
            GUIContent importContent = showImportSettings ? CustomGUIContents.HideImport : CustomGUIContents.ShowImport;

            if (!assetOnlyMode && !IsAPrefabTarget() && GUI.enabled)
            {
                currentX -= buttonWidth;
                if (GUI.Button(new Rect(currentX, y, buttonWidth, buttonHeight), importContent, toolbarButton))
                {
                    showImportSettings = !showImportSettings;
                    userAssetViewSize = -1;
                    if (!multiMode && targetObject is Material)
                    {
                        UnityEditorInternal.InternalEditorUtility.SetIsInspectorExpanded(targetObject, showImportSettings);
                    }
                    else if (multiMode && sameType && targetObjects[0] is Material)
                    {
                        UnityEditorInternal.InternalEditorUtility.SetIsInspectorExpanded(targetObjects[0], showImportSettings);
                    }
                    ResetAssetViewSize();
                    Repaint();
                }
                if (GUI.enabled)
                {
                    if (showImportSettings)
                    {
                        if (!EditorUtils.IsLightSkin())
                        {
                            EditorGUI.DrawRect(new Rect(currentX, y, buttonWidth - 1, buttonHeight), CustomColors.SimpleBright);
                        }
                        else
                        {
                            EditorGUI.DrawRect(new Rect(currentX, y, buttonWidth - 1, buttonHeight), CustomColors.ButtonEnabled);
                        }
                    }
                }
                else
                {
                    if (EditorUtils.IsLightSkin())
                    {
                        EditorGUI.DrawRect(new Rect(currentX, y, buttonWidth - 1, buttonHeight), CustomColors.SoftShadow);
                    }
                }
            }
            GUI.enabled = true;
            EditorGUILayout.BeginHorizontal(CustomGUIStyles.InspectorSectionStyle);
            GUILayout.Space(position.width - currentX);
            EditorGUILayout.EndHorizontal();
            Rect allButtonsRect = GUILayoutUtility.GetLastRect();
            allButtonsRect.height = 8;
            EditorUtils.DrawFadeToBottom(allButtonsRect, CustomColors.SubtleBright);
        }
        void DrawAssetBottomBar(bool isFolder, bool multiMode = false)
        {
            if (AssetTargetMode() == 0)
            {
                return;
            }
            if (!assetEditor)
            {
                ReAssignAssetTargets();
                return;
            }
            multiMode = AssetTargetMode() == 2;
            bool sameMultiType = false;
            bool isMissingScript = IsMissingAssetTarget();
            MissingScriptManager missingScript = null;
            if (multiMode)
            {
                sameMultiType = SortedAssetSelection.Count == 1;
            }
            if (assetOnlyMode)
            {
                GUILayout.FlexibleSpace();
            }
            EditorGUILayout.BeginHorizontal();
            GUIContent label = CustomGUIContents.EmptyContent;
            if (!multiMode)
            {
                AssetInfo assetInfo = PoolCache.GetAssetInfo(targetObject);

                if (isMissingScript)
                {
                    missingScript = targetObject as MissingScriptManager;
                    if (missingScript != null)
                    {
                        label.text = missingScript.path;
                    }
                }
                else
                {
                    label = assetEditor.GetPreviewTitle();
                    label.text = assetInfo.path;
                }
            }
            else
            {
                label.text = " ";
            }
            bool missingMulti = false;
            if (isMissingScript)
            {
                if (MissingScriptManager.IsMulti)
                {
                    missingMulti = true;
                }
            }
            bool hideEdit = false;

            if ((multiMode && !sameMultiType) || isFolder || isMissingScript)
            {
                hideEdit = true;
            }
            EditorGUILayout.BeginHorizontal();
            ShowOpenAssetButtons(hideEdit, multiMode);
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();
            Rect labelRect = GUILayoutUtility.GetLastRect();
            labelRect.y -= 3;

            bool drawingSettings = false;
            float minusX = 0;
            int padding = 4;
            if (assetEditor != null)
            {
                Color color = GUI.backgroundColor;
                bool hasPreviewGUI = assetEditor != null && assetEditor.HasPreviewGUI();
                bool hasImportPreview = assetImportSettingsEditor != null && assetImportSettingsEditor.HasPreviewGUI();

                bool isModel = PoolCache.IsAnImportedObject(targetObject);
                if ((!multiMode && hasPreviewGUI) || (multiMode && hasPreviewGUI && sameMultiType) && !IsAPrefabTarget())
                {
                    EditorGUILayout.BeginHorizontal(CustomGUIStyles.InspectorSectionStyle);
                    bool videoClip = targetObject is VideoClip;
                    if (hasPreviewGUI && !hasImportPreview)
                    {
                        GUI.backgroundColor += CustomColors.AddButtonColorDark * (EditorUtils.IsLightSkin() ? 0.25f : 0.55f);
                        assetEditor.OnPreviewSettings();
                        drawingSettings = true;
                    }
                    else if (hasImportPreview)
                    {
                        GUI.backgroundColor += CustomColors.AddButtonColorDark * (EditorUtils.IsLightSkin() ? 0.25f : 0.55f);
                        assetImportSettingsEditor.OnPreviewSettings();
                        drawingSettings = true;
                    }
                    EditorGUILayout.EndHorizontal();
                    minusX = GUILayoutUtility.GetLastRect().width;
                }
                GUI.backgroundColor = color;
                if (IsAPrefabTarget())
                {
                    drawingSettings = true;
                    if (EditorGUIUtility.isProSkin)
                    {
                        GUI.backgroundColor += CustomColors.AddButtonColorDark * 0.55f;
                    }
                    else
                    {
                        GUI.backgroundColor -= CustomColors.AddButtonColorLight;
                    }
                    padding = 3;
                    bool init = false;
                    EditorGUILayout.BeginHorizontal(CustomGUIStyles.ButtonsUpSection);
                    if (GUILayout.Button(CustomGUIContents.AddComponentContent, CustomGUIStyles.AddComponentButton))
                    {
                        init = true;
                    }
                    EditorGUILayout.EndHorizontal();
                    if (init)
                    {
                        float xPos = position.width - addComponentVector.x - 15;
                        float yPos = -addComponentVector.y + 30;
                        Rect rect2 = new Rect(xPos, yPos, addComponentVector.x, 0);
                        UnityEngine.GameObject[] objects = null;
                        if (multiMode)
                        {
                            objects = targetObjects.Cast<UnityEngine.GameObject>().ToArray();
                        }
                        else
                        {
                            objects = new UnityEngine.GameObject[] { targetObject as UnityEngine.GameObject };
                        }
                        Reflected.ShowAddComponentWindow(rect2, objects);
                    }
                }
                GUI.backgroundColor = color;
                Rect rect3 = GUILayoutUtility.GetLastRect();
                labelRect.height = 23;
                labelRect.y += 3;
                string path = "";
                if (!multiMode && !missingMulti)
                {
                    path = AssetDatabase.GetAssetPath(targetObject);
                    if (missingScript != null)
                    {
                        path = missingScript.path;
                    }
                }
                if (GUI.Button(labelRect, CustomGUIContents.PathContent(path), CustomGUIStyles.ToolbarButtonAsset))
                {
                    if (!multiMode && !missingMulti)
                    {
                        if (Event.current.button == 0)
                        {
                            EditorWindow.GetWindow(Reflected.GetProjectWindowType());
                            if (!isMissingScript)
                            {
                                EditorGUIUtility.PingObject(targetObject);
                            }
                            else if (missingScript != null)
                            {
#if UNITY_6000_4_OR_NEWER
                                EditorGUIUtility.PingObject(missingScript.entityID);

#else
                                EditorGUIUtility.PingObject(missingScript.instanceID);

#endif
                            }
                            ActiveEditorTracker.sharedTracker.ForceRebuild();
                        }
                        else if (Event.current.button == 1)
                        {
                            CustomGUIContents.PathContent(null);
                            EditorGUIUtility.systemCopyBuffer = path;
                            Debug.Log("Copied '" + path + "' to clipboard!");
                            rootVisualElement.CreateFloatingText("Copied Asset path!", new Vector2(Event.current.mousePosition.x, this.position.height - 25));
                        }
                    }
                }
                labelRect.y -= 3;
                labelRect.x += 5;
                labelRect.width -= 5;
                if ((multiMode || missingMulti) && labelRect.width > 40)
                {
                    Rect lineRect = new Rect(labelRect);
                    lineRect.width = 35;
                    lineRect.y = 11;
                    lineRect.x += 5;
                    Color lineColor = CustomGUIStyles.MiniLabel.normal.textColor * 0.75f;

                    EditorUtils.DrawLineOverRect(lineRect, lineColor);
                }
                else
                {
                    GUI.Label(labelRect, label, CustomGUIStyles.MiniLabel);
                }
                if (drawingSettings)
                {
                    float textWidth = CustomGUIStyles.MiniLabel.CalcSize(label).x;
                    Rect rect4 = new Rect(rect3);
                    bool hide = textWidth > labelRect.width - 10;
                    rect3.x -= padding;
                    rect3.height = 23;
                    if (hide)
                    {
                        rect3.width = 3;
                    }
                    else
                    {
                        rect3.width = 1;
                        rect3.x += 2;
                    }
                    EditorGUI.DrawRect(rect3, CustomColors.DefaultInspector);
                    if (hide)
                    {
                        rect3.width = 10;
                        rect3.x -= 10;
                        EditorUtils.DrawFadeToLeft(rect3, CustomColors.DefaultInspector);
                    }
                    EditorUtils.DrawFadeToBottom(rect4, CustomColors.SoftBright, 8);
                }
            }
            EditorGUILayout.EndHorizontal();
            EditorUtils.DrawLineOverRect(CustomColors.HarderBright);
            EditorUtils.DrawLineOverRect(CustomColors.SimpleShadow, 1);
        }
        void ShowOpenAssetButtons(bool onlyFolder = false, bool multiMode = false)
        {
            if (IsValidAssetTarget())
            {
                Color color = GUI.backgroundColor;
                if (!EditorUtils.IsLightSkin())
                {
                    GUI.backgroundColor += CustomColors.DarkHistoryButton * 0.9f;
                }
                GUIStyle toolbarButton = CustomGUIStyles.ToolbarButtonAsset;
                MissingScriptManager missingScript = IsMissingAssetTarget() ? targetObject as MissingScriptManager : null;
                GUIContent openFolderContent = CustomGUIContents.FolderIcon;
                if (onlyFolder || (IsAPrefabTarget() && !IsAModelTarget()))
                {
                    if (GUILayout.Button(openFolderContent, toolbarButton, GUILayout.Width(25)))
                    {
                        if (!multiMode)
                        {
                            string path = AssetDatabase.GetAssetPath(targetObject);
                            if (missingScript != null)
                            {
                                path = missingScript.path;
                            }
                            if (path != "")
                            {
                                EditorUtility.RevealInFinder(path);
                            }
                        }
                        else
                        {
                            foreach (var obj in targetObjects)
                            {
                                string path = AssetDatabase.GetAssetPath(obj);
                                if (path != "")
                                {
                                    EditorUtility.RevealInFinder(path);
                                }
                            }
                        }
                    }
                    Rect rect = GUILayoutUtility.GetLastRect();
                    EditorUtils.DrawFadeToBottom(rect, CustomColors.SoftBright, 8);
                    EditorUtils.DrawLineOverRect(rect, CustomColors.SoftBright);
                    EditorUtils.DrawLineOverRect(rect, CustomColors.SimpleShadow, 1);
                    if (!EditorUtils.IsLightSkin())
                    {
                        GUI.backgroundColor = color;
                    }
                    return;
                }
                GUIContent openContent = CustomGUIContents.OpenContent;
                if (GUILayout.Button(openFolderContent, toolbarButton, GUILayout.Width(25)))
                {
                    if (!multiMode)
                    {
                        string path = AssetDatabase.GetAssetPath(targetObject);
                        if (path != "")
                        {
                            EditorUtility.RevealInFinder(path);
                        }
                    }
                    else
                    {
                        foreach (var obj in targetObjects)
                        {
                            string path = AssetDatabase.GetAssetPath(obj);
                            if (path != "")
                            {
                                EditorUtility.RevealInFinder(path);
                            }
                        }
                    }
                }
                Rect _rect = GUILayoutUtility.GetLastRect();
                EditorUtils.DrawFadeToBottom(_rect, CustomColors.SoftBright, 8);
                EditorUtils.DrawLineOverRect(_rect, CustomColors.SoftBright);
                EditorUtils.DrawLineOverRect(_rect, CustomColors.SimpleShadow, 1);
                if (GUILayout.Button(openContent, toolbarButton, GUILayout.Width(25)))
                {
                    if (!multiMode)
                    {
                        string path = AssetDatabase.GetAssetPath(targetObject);
                        if (path != "")
                        {
                            OpenAsset(path);
                        }
                    }
                    else
                    {
                        foreach (var obj in targetObjects)
                        {
                            string path = AssetDatabase.GetAssetPath(obj);
                            if (path != "")
                            {
                                OpenAsset(path);
                            }
                        }
                    }
                }
                _rect = GUILayoutUtility.GetLastRect();
                EditorUtils.DrawFadeToBottom(_rect, CustomColors.SoftBright, 8);
                EditorUtils.DrawLineOverRect(_rect, CustomColors.SoftBright);
                EditorUtils.DrawLineOverRect(_rect, CustomColors.SimpleShadow, 1);
                if (!EditorUtils.IsLightSkin())
                {
                    GUI.backgroundColor = color;
                }
            }
        }
        internal static void OpenAsset(string pathToAsset)
        {
            var asset = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(pathToAsset);
            if (asset != null)
            {
                AssetDatabase.OpenAsset(asset);
            }
            else
            {
                Debug.LogError("Asset not found at: " + pathToAsset);
            }
        }
        internal static void OpenAsset(UnityEngine.Object asset)
        {
            if (asset != null)
            {
                AssetDatabase.OpenAsset(asset);
            }
        }
        private void PopUpInspectorWindow(UnityEngine.Object[] newTargets, bool popUp = false)
        {
            Type inspectorType = Reflected.GetInspectorWindowType();

            if (inspectorType != null && newTargets != null && newTargets.Length > 0)
            {
                MissingScriptManager missingScript = newTargets[0] is MissingScriptManager && IsMissingAssetTarget() ? targetObject as MissingScriptManager : null;
                /*
                   if (newTargets is Component)
                   {
                       Component component = newTargets as Component;              
                       newTargets = component.gameObject;
                   } */
                EditorWindow inspector = null;
                if (popUp)
                {
                    inspector = (EditorWindow)ScriptableObject.CreateInstance(inspectorType);
                }
                else
                {
                    inspector = EditorWindow.GetWindow(inspectorType);
                }
                if (inspector != null)
                {
                    UnityObject[] currentSelection = Selection.objects;
                    if (missingScript == null)
                    {
                        Selection.objects = newTargets;
                    }
                    else
                    {
#if UNITY_6000_4_OR_NEWER
                        Selection.entityIds = new EntityId[] { missingScript.entityID };
#else
#pragma warning disable CS0618
                        Selection.instanceIDs = new int[] { missingScript.instanceID };
#pragma warning restore CS0618

#endif

                    }

                    inspector.Show();
                    inspector.Focus();
                    EditorApplication.delayCall += () =>
                    {
                        if (popUp)
                        {
                            Reflected.SetLockState(inspector, true);
                            ignoreNextSelection = true;
                            Selection.objects = currentSelection;
                        }
                    };
                }
            }
        }

        internal void PopUpPreviewWindow(UnityEngine.Object[] newTargets, bool popUp = false)
        {
            Type inspectorType = Reflected.GetInspectorWindowType();

            if (inspectorType != null && newTargets != null && newTargets.Length > 0)
            {
                MissingScriptManager missingScript = newTargets[0] is MissingScriptManager && IsMissingAssetTarget() ? targetObject as MissingScriptManager : null;
                EditorWindow inspector = null;
                if (popUp)
                {
                    inspector = (EditorWindow)ScriptableObject.CreateInstance(inspectorType);
                }
                else
                {
                    inspector = EditorWindow.GetWindow(inspectorType);
                }
                if (inspector != null)
                {
                    UnityObject[] currentSelection = Selection.objects;
                    if (missingScript == null)
                    {
                        Selection.objects = newTargets;
                    }
                    else
                    {
#if UNITY_6000_4_OR_NEWER
                        Selection.entityIds = new EntityId[] { missingScript.GetEntityId() };
#else
#pragma warning disable CS0618
                        Selection.instanceIDs = new int[] { missingScript.instanceID };
#pragma warning restore CS0618
#endif
                    }
                    inspector.Show();
                    EditorApplication.delayCall += () =>
                    {
                        if (popUp)
                        {
                            ignoreNextSelection = true;
                            Selection.objects = currentSelection;
                            Reflected.GetPreviewWindow(inspector);
                        }
                    };
                }
            }
        }

        void FixActiveIndex(bool exitingPrefab = false)
        {
            if (onPrefabSceneMode)
            {
                if (!SceneIsInPrefabMode())
                {
                    onPrefabSceneMode = false;
                    exitingPrefab = true;
                }
            }
            bool removedGO = false;
            bool removedActiveGO = false;
            if (tabs != null)
            {
                List<TabInfo> toRemove = new List<TabInfo>();
                for (int i = 0; i < tabs.Count; i++)
                {
                    TabInfo tab = tabs[i];
                    if (FloatingTab.isClosing && FloatingTab.linkedTab == tab)
                    {
                        continue;
                    }
                    if (tab.markForDeletion && FloatingTab.isClosing && FloatingTab.linkedTab != tab)
                    {
                        toRemove.Add(tab);
                    }
                    if (tab.newTab && tab.prefab)
                    {
                        tab.prefab = false;
                        toRemove.Add(tab);
                    }
                    if (!tab.IsTabValid() && !tab.willBeDeleted)
                    {
                        EditorUtils.RebuildTab(tab, this);
                    }
                    if (tab.target == null && !tab.newTab && !tab.IsValidMultiTarget())
                    {
                        if (tab.prefab)
                        {
                            if (tab.target != null || tab.IsValidMultiTarget())
                            {
                                tab.prefab = false;
                                continue;
                            }
                            tab.willBeDeleted = true;
                            toRemove.Add(tab);
                        }
                        else
                        {
                            if (tab.target != null || tab.IsValidMultiTarget())
                            {
                                continue;
                            }
                            string name = tab.name;
                            toRemove.Add(tab);
                        }
                    }
                    else if (tab.IsValidMultiTarget())
                    {
                        bool removed = false;
                        foreach (GameObject go in tab.targets)
                        {
                            if (go == null)
                            {
                                tab.targets = tab.targets.Where(g => g != null).ToArray();
                                removed = true;
                                break;
                            }
                        }
                        if (tab.targets.Length == 1)
                        {
                            tab.target = tab.targets[0];
                            tab.targets = new GameObject[0];
                            tab.multiEditMode = false;
                        }
                        if (tab.targets.Length == 0)
                        {
                            tab.TrySetValidHistoryTarget();
                            if (tab.target != null || tab.IsValidMultiTarget())
                            {
                                tab.RefreshIcon();
                                tab.RefreshName();
                                continue;
                            }
                            removed = true;
                        }
                        if (removed)
                        {
                            removedGO = true;
                        }
                    }
                }
                if (toRemove.Count > 0)
                {
                    for (int i = 0; i < toRemove.Count; i++)
                    {
                        TabInfo tab = toRemove[i];
                        if (tab == null)
                        {
                            continue;
                        }
                        int tabIndex = tabs.IndexOf(tab);
                        if (tabIndex == -1)
                        {
                            continue;
                        }
                        if (i == toRemove.Count)
                        {
                            tab.ResetTab();
                            activeIndex = tab.index;
                        }
                        else
                        {
                            if (tabIndex == activeIndex)
                            {
                                DoCloseTab(tab, false, true);
                            }
                            else
                            {
                                CloseTab(tabIndex, false, true);
                            }
                            if (activeIndex > tabs.Count - 1)
                            {
                                activeIndex = tabs.Count - 1;
                            }
                        }
                    }
                    if (tabs == null || tabs.Count == 0)
                    {
                        tabs = new List<TabInfo>
                        {
                            new TabInfo(null, 0, this)
                        };
                        activeIndex = 0;
                    }
                    ReinitializeComponentEditors();
                    rootVisualElement.MarkDirtyRepaint();
                    DoCullNow();
                    //ReinitializeComponentEditors(false);
                    //Repaint();
                }
            }
            if (tabs == null || tabs.Count == 0)
            {
                tabs = new List<TabInfo>
                {
                    new TabInfo(null, 0, this)
                };
                activeIndex = 0;
                Repaint();
                return;
            }
            if (activeIndex > tabs.Count - 1)
            {
                activeIndex = tabs.Count - 1;
            }
            if (activeIndex < 0)
            {
                activeIndex = 0;
            }
            if (removedGO)
            {
                ReinitializeComponentEditors();
                //ReinitializeComponentEditors(false);
                RefreshAllTabNames();
            }
            if (removedActiveGO && GetActiveTab().target)
            {
                SetTargetGameObject(GetActiveTab().target);
            }
            else if (removedActiveGO && GetActiveTab().IsValidMultiTarget())
            {
                SetTargetGameObjects(GetActiveTab().targets);
            }
            /*
            if (exitingPrefab)
            {
                foreach (TabInfo tab in tabs)
                {
                    tab.FixNulls();                 
                    
                    if (tab.prefab && !tab.markForDeletion && !tab.willBeDeleted)
                    {
                        tab.prefab = false;
                    }
                
                }
            } */
        }
        void StartFallingTab()
        {
            if (GOdragging || FloatingTab.tabRect == null || FloatingTab.tabRect == Rect.zero)
            {
                return;
            }

            float padding = 0;
            if (showHistory)
            {
                padding = 40;
            }
            FloatingTab.startX = FloatingTab.tabRect.x;
            FloatingTab.targetTabX = GetTotalTabsWidth(dragTargetIndex - 1) + padding - toolbarScrollPosition.x - 3;
            FloatingTab.fallingTab = true;
            FloatingTab.startTime = Time.realtimeSinceStartup;
            FloatingTab.tabRect.x = FloatingTab.targetTabX;
        }
        float UpdateFallingTab()
        {
            if (!FloatingTab.fallingTab || FloatingTab.startTime < 0f)
            {
                FloatingTab.fallingTab = false;
                FloatingTab.tabRect = Rect.zero;
                return -1f;
            }
            float t = (Time.realtimeSinceStartup - FloatingTab.startTime) / FloatingTab.animationDuration * 4f;
            if (t >= 1f)
            {
                FloatingTab.startTime = -1f;
                FloatingTab.fallingTab = false;
                FloatingTab.tabRect = Rect.zero;
                return FloatingTab.targetTabX;
            }
            Rect rect = new Rect(FloatingTab.tabRect);
            rect.height -= 3;
            rect.y += 3;
            EditorGUI.DrawRect(rect, CustomColors.LineColor * 1f);
            float x = Mathf.Lerp(FloatingTab.startX, FloatingTab.targetTabX, t);
            Repaint();

            return x;
        }
        void EndDrag()
        {
            enteredSafeZone = false;
            GUIUtility.hotControl = 0;
            waitingToDrag = false;
            dragging = false;
            DragAndDrop.objectReferences = null;
            if (GOdragging)
            {
                GOdragging = false;
                dragIndex = -1;
                dragTargetIndex = -1;
            }
            if (dragIndex == -1 || dragTargetIndex == -1)
            {
                StartFallingTab();
                dragIndex = -1;
                dragTargetIndex = -1;
            }
            if (dragIndex == dragTargetIndex)
            {
                StartFallingTab();
                dragIndex = -1;
                dragTargetIndex = -1;
            }
            if (dragIndex <= dragTargetIndex)
            {
                dragTargetIndex -= 1;
            }
            if (dragIndex != -1 && dragTargetIndex != -1)
            {
                if (dragIndex > tabs.Count - 1 || dragTargetIndex > tabs.Count - 1 || dragIndex < 0 || dragTargetIndex < 0)
                {
                    dragIndex = -1;
                    dragTargetIndex = -1;
                    return;
                }
                TabInfo tabToMove = tabs[dragIndex];
                TabInfo activeTab = GetActiveTab();
                tabs.RemoveAt(dragIndex);
                if (tabToMove != null)
                {
                    tabs.Insert(dragTargetIndex, tabToMove);
                }
                activeIndex = tabs.IndexOf(activeTab);
                StartFallingTab();
                dragIndex = -1;
                dragTargetIndex = -1;
                PopUpTip.Hide();
            }
        }
        void HandleTabDragging()
        {

            if (dragging)
            {
                waitingToDrag = false;
                int _control = GUIUtility.GetControlID(FocusType.Passive);
                Event currentEvent = Event.current;
                switch (currentEvent.GetTypeForControl(_control))
                {
                    case EventType.MouseDrag:
                        GUIUtility.hotControl = _control;
                        if (dragging)
                        {
                            if (!GOdragging && tabs[dragIndex].target || tabs[dragIndex].IsValidMultiTarget())
                            {
                                if (currentEvent.mousePosition.y > 30)
                                {
                                    GOdragging = true;
                                    FloatingTab.Reset(this, true);
                                    GUIUtility.hotControl = 0;
                                    DragAndDrop.PrepareStartDrag();
                                    if (tabs[dragIndex].IsValidMultiTarget())
                                    {
                                        DragAndDrop.objectReferences = tabs[dragIndex].targets;
                                    }
                                    else
                                    {
                                        DragAndDrop.objectReferences = new UnityEngine.Object[] { tabs[dragIndex].target };
                                    }
                                    DragAndDrop.StartDrag(tabs[dragIndex].shortName);
                                    currentEvent.Use();
                                }
                            }
                        }
                        currentEvent.Use();
                        break;
                    case EventType.MouseUp:
                        EndAssetViewResize();
                        EndDrag();
                        currentEvent.Use();
                        break;

                    case EventType.MouseEnterWindow:
                        if (currentEvent.mousePosition.y >= 30)
                        {
                            EndAssetViewResize();
                            EndDrag();
                        }
                        break;
                    case EventType.DragUpdated:
                        if (dragging)
                        {
                            if (GOdragging)
                            {
                                if (currentEvent.mousePosition.y < 30)
                                {
                                    GOdragging = false;
                                    DragAndDrop.objectReferences = null;
                                    DragAndDrop.PrepareStartDrag();
                                    DragAndDrop.visualMode = DragAndDropVisualMode.None;
                                    Repaint();
                                }
                            }
                            else if (!GOdragging && tabs[dragIndex].target)
                            {
                                if (currentEvent.mousePosition.y > 30)
                                {
                                    GOdragging = true;
                                    FloatingTab.Reset(this);
                                    DragAndDrop.objectReferences = new UnityEngine.Object[] { tabs[dragIndex].target };
                                    Event.current.Use();
                                }
                            }
                        }
                        break;
                }
            }
        }
        void EndAssetViewResize()
        {
            ClearCursorOverlay();
            GUIUtility.hotControl = 0;
            resizingAssetView = false;
            resizeOriginalCursorY = 0;
            alreadyCalculatedHeight = true;
            if (assetOnlyMode && maximizeMode == 1)
            {
                maximizeMode = 0;
                Repaint();
                RunNextFrame(() =>
                {
                    ResetMaximizedView();
                });
            }
            else if (!assetOnlyMode && maximizeMode == 2)
            {
                maximizeMode = 0;
                OpenMaximizedAssetMode();
            }
            else
            {
                maximizeMode = 0;
                Repaint();
            }
        }
        void HandleAssetViewResize()
        {
            bool newTabNoAssetCase = false;
            if (!IsValidAssetTarget())
            {
                if (!IsActiveTabNew() && !assetOnlyMode)
                {
                    return;
                }
                else
                {
                    EditorGUILayout.BeginHorizontal(CustomGUIStyles.InspectorSectionStyle);
                    GUILayout.Space(35);
                    EditorGUILayout.EndHorizontal();
                    newTabNoAssetCase = true;
                }
            }
            Rect actionRect = new Rect(assetViewRect)
            {
                height = 36
            };
            actionRect.y -= 1;
            if (newTabNoAssetCase)
            {
                actionRect = GUILayoutUtility.GetLastRect();
                actionRect.width = position.width;
                actionRect.height = 36;
                actionRect.y -= 37;
            }
            if (IsActiveTabNew() || assetOnlyMode)
            {
                Rect drawRect = new Rect(actionRect)
                {
                    height = 35
                };
                drawRect.y += 1;
                Color color = CustomColors.DefaultInspector * 0.85f;
                color.a = 1;
                EditorGUI.DrawRect(drawRect, color);
                if (!newTabNoAssetCase)
                {
                    EditorUtils.DrawLineOverRect(actionRect, CustomColors.SoftShadow, -3, 33);
                }
                EditorUtils.DrawLineOverRect(actionRect, CustomColors.HarderBright, -2);
                EditorUtils.DrawLineOverRect(actionRect, CustomColors.HardShadow, -1);
                EditorUtils.DrawLineUnderRect(drawRect, CustomColors.HardShadow, 0);
            }
            leftHandleRect = new Rect(actionRect);
            rightHandleRect = new Rect(actionRect);

            leftHandleRect.width = leftHandleRect.width / 2 - 100;
            rightHandleRect.x += leftHandleRect.width + 200;
            rightHandleRect.width = leftHandleRect.width;
            if (!hideUpdateAlert && UpdateChecker.IsUpdateAvailable && position.width > 322)
            {
                leftHandleRect.width -= 55;
                leftHandleRect.x += 55;
            }
            if (!assetsCollapsed && !newTabNoAssetCase && !middleScrolling)
            {
                EditorGUIUtility.AddCursorRect(leftHandleRect, MouseCursor.ResizeVertical);
                EditorGUIUtility.AddCursorRect(rightHandleRect, MouseCursor.ResizeVertical);
                if (Event.current.type == EventType.Repaint)
                {
                    hoveringResize = leftHandleRect.Contains(Event.current.mousePosition) || rightHandleRect.Contains(Event.current.mousePosition);
                }
            }
            leftHandleRect.y += 14;
            leftHandleRect.width -= 20;
            leftHandleRect.x += 13;
            if (!assetsCollapsed && !newTabNoAssetCase)
            {
                EditorUtils.DrawLineOverRect(leftHandleRect, CustomColors.MediumShadow, -2);
                EditorUtils.DrawLineOverRect(leftHandleRect, CustomColors.MediumShadow, -6);
                EditorUtils.DrawLineOverRect(leftHandleRect, CustomColors.HarderBright, -3);
                EditorUtils.DrawLineOverRect(leftHandleRect, CustomColors.HarderBright, -7);
            }
            if (!hideUpdateAlert && UpdateChecker.IsUpdateAvailable && position.width > 322)
            {
                leftHandleRect.width += 55;
                leftHandleRect.x -= 55;
            }
            rightHandleRect = leftHandleRect;
            rightHandleRect.x += leftHandleRect.width + 13 + 200;
            if (!assetsCollapsed && !newTabNoAssetCase)
            {
                EditorUtils.DrawLineOverRect(rightHandleRect, CustomColors.MediumShadow, -2);
                EditorUtils.DrawLineOverRect(rightHandleRect, CustomColors.MediumShadow, -6);
                EditorUtils.DrawLineOverRect(rightHandleRect, CustomColors.HarderBright, -3);
                EditorUtils.DrawLineOverRect(rightHandleRect, CustomColors.HarderBright, -7);
            }
            if (assetsCollapsed)
            {
                if (Event.current.type == EventType.MouseUp)
                {
                    Vector2 mousePos = Event.current.mousePosition;
                    if (leftHandleRect.Contains(mousePos) || rightHandleRect.Contains(mousePos))
                    {
                        ForceCollapseOrDefault();
                    }
                }

            }
            if (IsActiveTabNew() || assetOnlyMode)
            {
                Vector2 _position = new Vector2(position.width / 2 - 103, actionRect.y + 6);
                if (newTabNoAssetCase)
                {
                    _position = new Vector2(position.width / 2 - 103, actionRect.y + 6);
                    actionRect.y = position.height - 4;
                    actionRect.height = 15;
                }
                DrawAssetHistoryButton(_position);
                if (newTabNoAssetCase)
                {
                    return;
                }
            }
        }

        void DrawAssetHistoryButton(Vector2 _position)
        {
            GUIContent addText = CustomGUIContents.AddComponent;
            GUIStyle addButtonStyle = GUI.skin.button;
            float addButtonWidth = 180f;
            float addButtonHeight = 25f;
            Rect addButtonRect = new Rect(_position.x, _position.y, addButtonWidth, addButtonHeight);
            if (!assetInspection)
            {
                addButtonRect.x += 11;
            }
            GUI.enabled = false;
            GUI.Button(addButtonRect, addText, addButtonStyle);
            EditorUtils.DrawRectBorder(addButtonRect, CustomColors.SimpleShadow);
            EditorUtils.DrawLineOverRect(addButtonRect, CustomColors.HarderBright, -1, 1);
            GUI.enabled = true;
            if (!hideUpdateAlert && UpdateChecker.IsUpdateAvailable && position.width > 322)
            {
                Rect buttonRect = new Rect(5, addButtonRect.y + 2, 54, 21);
                if (GUI.Button(buttonRect, CustomGUIContents.UpdateContent, GUIStyle.none))
                {
                    UnityEditor.PackageManager.UI.Window.Open("");
                }
                GUI.Label(new Rect(buttonRect.x + 20, buttonRect.y - 2, 54, 21), UpdateChecker.LatestVersion, CustomGUIStyles.MiniLabel);
                if (!middleScrolling)
                {
                    EditorGUIUtility.AddCursorRect(buttonRect, MouseCursor.Link);
                }

            }
            if (!assetInspection)
            {
                return;
            }
            GUIContent _historyButton = CustomGUIContents.HistoryButton;
            GUIStyle historyButtonStyle = CustomGUIStyles.NoMarginButton;
            float historyButtonWidth = 20f;
            float historyButtonHeight = 25f;
            Rect historyButtonRect = new Rect(addButtonRect.x + addButtonRect.width + 3, _position.y, historyButtonWidth, historyButtonHeight);
            if (EditorUtils.IsLightSkin())
            {
                GUI.backgroundColor -= CustomColors.LightHistoryButton * 1.75f;
            }
            else
            {
                GUI.backgroundColor += CustomColors.BackHistory;
            }
            GUI.enabled = historyAssets != null && historyAssets.Count > 0;
            if (GUI.Button(historyButtonRect, _historyButton, historyButtonStyle))
            {
                ShowHistoryContextMenu();
                Event.current.Use();
            }
            if (GUI.enabled)
            {
                CustomGUIContents.DrawCustomButton(historyButtonRect, false, true);
            }
            else
            {
                EditorUtils.DrawLineOverRect(historyButtonRect, -1);
                EditorUtils.DrawRectBorder(historyButtonRect, CustomColors.SimpleShadow);
                GUI.enabled = true;
            }
            if (EditorUtils.IsLightSkin())
            {
                GUI.backgroundColor += CustomColors.LightHistoryButton * 1.75f;
            }
            else
            {
                GUI.backgroundColor -= CustomColors.BackHistory;
            }
        }
        void DrawAssetHistoryButton()
        {
            GUIContent _historyButton = CustomGUIContents.HistoryButton;
            if (EditorUtils.IsLightSkin())
            {
                GUI.backgroundColor -= CustomColors.LightHistoryButton * 1.75f;
            }
            else
            {
                GUI.backgroundColor += CustomColors.BackHistory;
            }
            GUI.enabled = historyAssets != null && historyAssets.Count > 0;
            if (GUILayout.Button(_historyButton, CustomGUIStyles.NoMarginButton, GUILayout.Height(25), GUILayout.Width(20)))
            {
                ShowHistoryContextMenu();
                Event.current.Use();
            }
            if (GUI.enabled)
            {
                CustomGUIContents.DrawCustomButton(false, true);
            }
            else
            {
                Rect rect = GUILayoutUtility.GetLastRect();
                EditorUtils.DrawLineOverRect(rect, -1);
                EditorUtils.DrawRectBorder(rect, CustomColors.SimpleShadow);
                GUI.enabled = true;
            }
            if (EditorUtils.IsLightSkin())
            {
                GUI.backgroundColor += CustomColors.LightHistoryButton * 1.75f;
            }
            else
            {
                GUI.backgroundColor -= CustomColors.BackHistory;
            }
        }
        private bool triggerDrag = false;
        private void HandleFloatingTabInBar()
        {
            if ((dragging && !GOdragging) || FloatingTab.fallingTab)
            {
                bool isActiveTab = FloatingTab.linkedTab == GetActiveTab();
                Rect buttonRect = dragRect;
                if (FloatingTab.fallingTab)
                {
                    buttonRect = FloatingTab.tabRect;
                }
                else
                {
                    dragRect.width = totalWidth[dragIndex];
                    FloatingTab.linkedTab = tabs[dragIndex];
                    FloatingTab.dragTargetIndex = dragTargetIndex;
                    FloatingTab.dragIndex = dragIndex;
                }

                Color color = GUI.backgroundColor;
                GUIStyle style = CustomGUIStyles.ToolbarButtonTabs;
                if (FloatingTab.linkedTab.prefab)
                {
                    style = CustomGUIStyles.ToolbarButtonTabsPrefab;
                }
                style.contentOffset = new Vector2(0, -1);
                Texture2D icon = null;
                if (!FloatingTab.linkedTab.newTab && !FloatingTab.linkedTab.multiEditMode)
                {
                    if (showIcons)
                    {
                        icon = FloatingTab.linkedTab.icon;
                        style.padding = PaddingIcon;
                    }
                    else
                    {
                        style.padding = PaddingNoIcon;
                    }

                    if (FloatingTab.linkedTab.locked)
                    {
                        icon = CustomGUIContents.ContentWithLockIcon.image as Texture2D;
                        style.padding = PaddingIcon;
                    }
                }
                else if (FloatingTab.linkedTab.IsValidMultiTarget() && showIcons && !FloatingTab.linkedTab.locked)
                {
                    style.padding = PaddingIcon;
                    icon = CustomGUIContents.TabMulti.image as Texture2D;
                }
                else
                {
                    style.padding = PaddingNoIcon;
                    if (FloatingTab.linkedTab.IsValidMultiTarget() && FloatingTab.linkedTab.locked)
                    {
                        icon = CustomGUIContents.ContentWithLockIcon.image as Texture2D;
                        style.padding = PaddingIcon;
                    }
                }
                buttonRect.y = dragRect.y;
                int xLimit = 0;
                if (FloatingTab.fallingTab)
                {
                    buttonRect.x = UpdateFallingTab();
                    if (buttonRect.x < 0)
                    {
                        GUI.backgroundColor = color;
                        return;
                    }
                }
                else
                {
                    buttonRect.x = Event.current.mousePosition.x - FloatingTab.tabDragPoint;
                    if (showHistory)
                    {
                        xLimit = 40;
                    }
                    if (buttonRect.x < xLimit)
                    {
                        buttonRect.x = xLimit;
                        toolbarScrollPosition.x -= 2;
                    }
                    if (buttonRect.xMax + 30 > position.width)
                    {
                        toolbarScrollPosition.x += 2;
                    }
                    if (buttonRect.xMax > position.width - 23)
                    {
                        buttonRect.x = position.width - buttonRect.width - 23;
                    }
                }
                LimitScrollBar();
                style.fixedHeight = 23;
                if (!FloatingTab.fallingTab)
                {
                    FloatingTab.style = style;
                    FloatingTab.tabRect = buttonRect;
                    FloatingTab.icon = icon;
                    FloatingTab.isSelected = activeIndex == dragIndex;
                }
                else
                {
                    if (FloatingTab.isSelected)
                    {
                        FloatingTab.style = CustomGUIStyles.ToolbarButtonTabs;
                    }
                }
                if (activeIndex != FloatingTab.dragIndex)
                {
                    GUI.backgroundColor += CustomColors.SubtleBlue * 0.5f;
                }
                else
                {
                    GUI.backgroundColor += CustomColors.VerySubtleBlue;
                }
                Rect trimRect = new Rect(buttonRect);
                trimRect.height = 2;
                trimRect.y += 1;
                //FloatingTab.style.fixedHeight = 22;
                GUI.Button(trimRect, FloatingTab.linkedTab.shortName, FloatingTab.style);
                trimRect.y -= 1;
                trimRect.height = 1;
                trimRect.x += 1;
                trimRect.width -= 2;
                EditorGUI.DrawRect(trimRect, CustomColors.DefaultInspector);
                GUI.backgroundColor = color;

                if ((showIcons || FloatingTab.linkedTab.locked) && FloatingTab.icon)
                {
                    float size = 16;
                    float pad = 4;
                    if (FloatingTab.linkedTab.locked)
                    {
                        size = 17;
                        pad = 3;
                    }
                    Rect iconRect = new Rect(buttonRect.x + 1, buttonRect.y + pad, size, size);
                    GUI.Label(iconRect, FloatingTab.icon);
                }
                EditorUtils.DrawFadeToBottom(buttonRect, CustomColors.SoftBright, 8);
                buttonRect.width -= 1;
                if (FloatingTab.linkedTab.isSelected && !FloatingTab.linkedTab.newTab)
                {
                    Rect selectRect = new Rect(buttonRect.x + 1, buttonRect.y, buttonRect.width - 2, 2);
                    EditorUtils.DrawSelectedLineDecorator(selectRect, CustomColors.HardShadow, activeIndex == dragIndex);
                }
                else
                {
                    EditorUtils.DrawLineRoundDecorator(buttonRect, CustomColors.HarderBright, CustomColors.HardShadow, true);
                }


                Rect rect = new Rect(buttonRect.x, buttonRect.y + 22, buttonRect.width, 3);
                if (FloatingTab.isSelected)
                {
                    Color editorColor = EditorGUIUtility.isProSkin ? CustomColors.ActiveDark : CustomColors.ActiveLight;
                    EditorUtils.DrawActiveTabUnder(rect, editorColor);
                }
                if (!isActiveTab)
                {
                    rect.height = 0;
                    EditorUtils.DrawLineUnderRect(rect, CustomColors.DarkInspector);
                }


                if (buttonRect.x - xLimit <= 0)
                {
                    dragTargetIndex = 0;
                }
                Repaint();
            }
        }
        bool MultiEditMode()
        {
            if (tabs == null || tabs.Count == 0)
            {
                return false;
            }
            if (GetActiveTab().multiEditMode)
            {
                return true;
            }
            return false;
        }
        internal void CloseAssetView()
        {
            targetObject = null;
            targetObjects = null;
            lockedAsset = false;
            if (assetOnlyMode)
            {
                ReinitializeComponentEditors();
                assetOnlyMode = false;
                SetComponentViewVisibility();
            }
            ResetAssetViewSize();
            lastKnownHeight = 1;
            alreadyCalculatedHeight = false;
            if (assetEditor || prefabEditors != null)
            {
                EditorApplication.delayCall += () =>
                {
                    CleanAllAssetEditors();
                    Repaint();
                };
            }

            MissingScriptManager.SetInactive();
            MissingScriptManager.ClearData();

        }
        private void CleanAllAssetEditors()
        {
            PrefabMaterialMapManager.DestroyAllMaterialMaps();
            PrefabComponentMapManager.componentMaps = new List<ComponentMap>();
            DestroyIfNotNull(assetEditor);
            assetEditor = null;
            DestroyIfNotNull(assetImportSettingsEditor);
            assetImportSettingsEditor = null;
            if (assetImporter != null)
            {
                assetImporter = null;
            }
            if (assetImporters != null)
            {
                assetImporters = null;
            }
            DestroyAllIfNotNull(prefabEditors);
            prefabEditors = null;
            DestroyAllIfNotNull(prefabMaterialEditors);
            prefabMaterialEditors = null;
        }

        void TrySaveSession()
        {
            if (activeScene == null || !activeScene.IsValid())
            {
                return;
            }
            if (settingsData == null)
            {
                settingsData = AutoCreateSettings();
            }
            if (settingsData && !InRecoverScreen())
            {
                settingsData.SaveSession(this, activeScene);
            }
            else if (settingsData)
            {
                TrySaveLastAssets();
                EditorUtility.SetDirty(settingsData);
                AssetDatabase.SaveAssetIfDirty(settingsData);
            }
        }

        void HandleSceneChanged(bool saveLast = true)
        {
            changingScenes = false;
            ReloadPreview();
            if (enteringPlayMode && !StartedPlayModeInOtherScene())
            {
                return;
            }
            if (saveLast)
            {
                TrySaveSession();
            }
            activeScene = SceneInfo.FromActiveScene(activeScene);
            lastValidScenePath = activeScene.ScenePath;
            if (sessionsMode != 2 || exitingPlayMode)
            {
                if (instances != null && instances.Contains(this))
                {
                    instances.Remove(this);
                }
                ReopenCoInspector(SceneManager.GetActiveScene(), LoadSceneMode.Single);
            }
            else
            {
                CleanTabs();
                RestoreLastAssets();
                TryLoadSession();
                RegisterWindow(this);
            }
            UpdateCurrentTip();
        }


        void AutoCheckScene()
        {
            if (exitingPlayMode)
            {
                return;
            }
            Scene currentScene = SceneManager.GetActiveScene();
            bool isSceneValid = currentScene.IsValid() && !string.IsNullOrEmpty(currentScene.path);

            if (!isSceneValid)
            {
                return;
            }
            if (activeScene == null || activeScene.ScenePath != currentScene.path)
            {
                activeScene = SceneInfo.FromActiveScene();
                lastValidScenePath = activeScene.ScenePath;
            }
        }

        private void TranslateIMGUIEvents()
        {
            if (Event.current.keyCode == KeyCode.Escape)
            {
                pendingOperation = null;
            }
            if (Event.current != null && Event.current.isMouse)
            {
                EventBase eventBase = null;

                if (Event.current.type == EventType.MouseUp || Event.current.type == EventType.MouseEnterWindow)
                {

                    if (Event.current.keyCode != KeyCode.Escape)
                    {
                        eventBase = MouseUpEvent.GetPooled(Event.current);
                    }
                }
                if (Event.current.type == EventType.MouseLeaveWindow)
                {
                    Vector2 mouse = Event.current.mousePosition;
                    if (mouse.x < 0 || mouse.x > position.width || mouse.y < 0 || mouse.y > position.height)
                    {
                        if (Event.current.keyCode != KeyCode.Escape)
                        {
                            eventBase = MouseUpEvent.GetPooled(Event.current);
                        }
                    }
                    PopUpTip.Hide();
                }
                else if (Event.current.type == EventType.MouseDrag)
                {
                    eventBase = MouseMoveEvent.GetPooled(Event.current);
                }
                if (eventBase != null)
                {
                    HandleEvent(eventBase, true);
                }
            }
        }

        void AssetTopBarMenus(bool prefabMode, bool multiMode)
        {
            if (!targetObject && targetObjects == null)
            {
                return;
            }
            GenericMenu menu = new GenericMenu();

            if (!assetOnlyMode && (assetImportSettingsEditor != null || prefabMode))
            {
                if (!showImportSettings)
                {
                    string label = "Show All Import Settings";
                    if (prefabMode)
                    {
                        label = "Show All Import Settings";
                    }
                    menu.AddItem(new GUIContent(label), false, () =>
                    {
                        showImportSettings = true;
                        userAssetViewSize = -1;
                        if (!assetsCollapsed)
                        {
                            ResetAssetViewSize();
                            Repaint();
                        }
                    });
                }
                else
                {
                    string label = "Hide Import Settings";
                    if (prefabMode)
                    {
                        label = "Hide Import Settings";
                    }
                    menu.AddItem(new GUIContent(label), false, () =>
                    {
                        showImportSettings = false;
                        userAssetViewSize = -1;
                        if (userHeight != 1)
                        {
                            ResetAssetViewSize();
                        }
                    });
                }
                menu.AddSeparator("");
            }
            if (!InRecoverScreen())
            {

                string maximized = "Show in Asset-Only Mode";
                if (assetOnlyMode)
                {
                    maximized = "Exit Asset-Only Mode";
                }
                menu.AddItem(new GUIContent(maximized), false, (GenericMenu.MenuFunction)(() =>
                   {
                       assetOnlyMode = !assetOnlyMode;
                       if (assetOnlyMode)
                       {
                           this.ReinitializeComponentEditors();
                           assetsCollapsed = false;
                       }
                       ResetAssetViewSize();
                       Repaint();
                   }));
            }
            if (!assetsCollapsed)
            {
                menu.AddItem(new GUIContent("Collapse the Asset View"), false, () =>
                {
                    bool wasMax = assetOnlyMode;
                    ForceCollapseOrDefault();
                    if (wasMax)
                    {
                        ForceCollapseOrDefault();
                    }
                });
            }
            else
            {
                menu.AddItem(new GUIContent("Expand the Asset View"), false, () =>
                {
                    ForceCollapseOrDefault();
                });
            }
            menu.AddSeparator("");
            menu.AddItem(new GUIContent("Normal"), !debugAsset, () =>
               {
                   debugAsset = false;
                   DrawCurrentAssets();
               });
            menu.AddItem(new GUIContent("Debug"), debugAsset, () =>
            {
                debugAsset = true;
                DrawCurrentAssets();
            });
            menu.AddSeparator("");
            menu.AddItem(new GUIContent("Reveal in Folder"), false, () =>
            {
                if (multiMode)
                {
                    foreach (var obj in targetObjects)
                    {
                        EditorUtility.RevealInFinder(AssetDatabase.GetAssetPath(obj));
                    }
                }
                else
                {
                    EditorUtility.RevealInFinder(AssetDatabase.GetAssetPath(targetObject));
                }
            });
            if (!multiMode)
            {
                menu.AddItem(new GUIContent("Ping Asset in Project"), false, () =>
                {
                    EditorWindow.GetWindow(Reflected.GetProjectWindowType());
                    EditorGUIUtility.PingObject(targetObject);
                });
            }
            menu.AddSeparator("");
            menu.AddItem(new GUIContent("Ignore Folder Inspection"), ignoreFolders, () =>
            {
                ignoreFolders = !ignoreFolders;
                SaveSettings();
            });
            menu.AddItem(new GUIContent("Disable Asset Inspection"), !assetInspection, () =>
                {
                    assetInspection = !assetInspection;
                    if (!assetInspection)
                    {
                        CloseAssetView();
                    }
                    SaveSettings();
                    Repaint();
                });
            menu.AddSeparator("");
            menu.AddItem(new GUIContent("Collapsed Prefab View"), collapsePrefabComponents, () =>
                {
                    collapsePrefabComponents = !collapsePrefabComponents;
                    SaveSettings();
                    ReinitializePrefabComponentEditors();
                });
            menu.AddItem(new GUIContent("Show AssetBundle Footer"), showAssetLabels, () =>
            {
                showAssetLabels = !showAssetLabels;
                SaveSettings();
                ResetAssetViewSize();
                RepaintForAWhile();
            });
            menu.AddSeparator("");
            menu.AddItem(new GUIContent("Close the Asset View"), false, () =>
            {
                if (!CheckForApplyRevertOnClose())
                {
                    return;
                }
                CloseAssetView();
            });
            float height = 15 * (menu.GetItemCount() + 1);
            if (position.height - Event.current.mousePosition.y < height)
            {
                menu.DropDown(new Rect(Event.current.mousePosition.x, Event.current.mousePosition.y - height, 0, 0));
            }
            else
            {
                menu.ShowAsContext();
            }
        }
        public bool NotNullTabs()
        {
            if (tabs != null && tabs.Count > 0)
            {
                return true;
            }
            return false;
        }
        public bool IsThereANullEditor()
        {
            if (componentEditors != null)
            {
                foreach (var editor in componentEditors)
                {
                    if (editor == null)
                    {
                        return true;
                    }
                }
            }
            return false;
        }
        internal bool CanSkipRefresh()
        {
            if (Event.current.type == EventType.Used)
            {
                //       oops not using this now
            }
            return false;
        }
        void HandleComponentDrag()
        {

            DragAndDrop.objectReferences = null;
            DragAndDrop.PrepareStartDrag();
            if (!pendingComponentDrag)
            {
                return;
            }
            pendingComponentDrag = false;

            if (pendingOperation == null || pendingOperation.targetObject == null || pendingOperation.consumed)
            {
                return;
            }
            InitiateMovementRecording(pendingOperation.draggedComponent, pendingOperation.targetObject);
            if (pendingOperation.isAsset)
            {
                if (pendingOperation.assets == null || pendingOperation.assets.Count == 0)
                {
                    if (pendingOperation.errored)
                    {
                        pendingOperation.consumed = true;
                        pendingOperation = null;
                        EditorUtils.ShowPasteFailedMessage();
                        return;
                    }
                    pendingOperation = null;
                    return;
                }
                for (int i = 0; i < pendingOperation.assets.Count; i++)
                {
                    if (pendingOperation.assets[i] == null)
                    {
                        continue;
                    }
                    UnityObject toAdd = pendingOperation.assets[i];
                    Type typeToAdd = toAdd.GetType();
                    bool isUIObject = EditorUtils.IsAnUIObject(pendingOperation.targetObject);
                    if (toAdd is MonoScript)
                    {
                        typeToAdd = (toAdd as MonoScript).GetClass();
                    }
                    if (toAdd is AudioClip)
                    {
                        typeToAdd = typeof(AudioSource);
                    }
                    else if (toAdd is AudioMixerGroup)
                    {
                        typeToAdd = typeof(AudioSource);
                    }
                    else if (toAdd is UnityEditor.Animations.AnimatorController)
                    {
                        typeToAdd = typeof(Animator);
                    }
                    else if (toAdd is AnimationClip)
                    {
                        typeToAdd = typeof(Animation);
                    }
                    else if (toAdd is Sprite)
                    {
                        if (isUIObject)
                        {
                            typeToAdd = typeof(UnityEngine.UI.Image);
                        }
                        else
                        {
                            typeToAdd = typeof(SpriteRenderer);
                        }
                    }
                    else if (toAdd is Texture2D)
                    {
                        if (isUIObject)
                        {
                            typeToAdd = typeof(UnityEngine.UI.RawImage);
                        }
                        else
                        {
                            Texture2D texture = toAdd as Texture2D;
                            Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(AssetDatabase.GetAssetPath(texture));
                            if (sprite)
                            {
                                toAdd = sprite;
                                typeToAdd = typeof(SpriteRenderer);
                            }
                            else
                            {
                                typeToAdd = typeof(MeshRenderer);
                            }
                        }
                    }
                    else if (toAdd is VideoClip)
                    {
                        typeToAdd = typeof(UnityEngine.Video.VideoPlayer);
                    }
                    else if (toAdd is Font)
                    {
                        typeToAdd = typeof(UnityEngine.UI.Text);
                    }
                    else if (toAdd is Material)
                    {
                        typeToAdd = typeof(MeshRenderer);
                    }
                    alreadyMovingComponent = true;
                    if (Undo.AddComponent(pendingOperation.targetObject, typeToAdd))
                    {
                        Component justAdded = pendingOperation.targetObject.GetComponents<Component>()[pendingOperation.targetObject.GetComponents<Component>().Length - 1];
                        if (justAdded is MeshRenderer)
                        {
                            MeshRenderer meshRenderer = justAdded as MeshRenderer;
                            if (toAdd is Material)
                            {
                                meshRenderer.sharedMaterial = toAdd as Material;
                            }
                            else
                            {
                                meshRenderer.sharedMaterial = new Material(Shader.Find("Standard"));
                                meshRenderer.sharedMaterial.mainTexture = toAdd as Texture2D;
                            }
                        }
                        else if (justAdded is AudioSource)
                        {
                            AudioSource audioSource = justAdded as AudioSource;
                            if (toAdd is AudioClip)
                            {
                                audioSource.clip = toAdd as AudioClip;
                            }
                            else if (toAdd is AudioMixerGroup)
                            {
                                audioSource.outputAudioMixerGroup = toAdd as AudioMixerGroup;
                            }
                        }
                        else if (justAdded is Animator)
                        {
                            Animator animator = justAdded as Animator;
                            animator.runtimeAnimatorController = toAdd as UnityEditor.Animations.AnimatorController;
                        }
                        else if (justAdded is SpriteRenderer)
                        {
                            SpriteRenderer spriteRenderer = justAdded as SpriteRenderer;
                            spriteRenderer.sprite = toAdd as Sprite;

                        }
                        else if (justAdded is Animation)
                        {
                            Animation animation = justAdded as Animation;
                            animation.clip = toAdd as AnimationClip;
                        }
                        else if (justAdded is UnityEngine.UI.Image)
                        {
                            UnityEngine.UI.Image image = justAdded as UnityEngine.UI.Image;
                            image.sprite = toAdd as Sprite;
                        }
                        else if (justAdded is UnityEngine.UI.RawImage)
                        {
                            UnityEngine.UI.RawImage _image = justAdded as UnityEngine.UI.RawImage;
                            _image.texture = toAdd as Texture2D;
                        }
                        else if (justAdded is UnityEngine.Video.VideoPlayer)
                        {
                            UnityEngine.Video.VideoPlayer videoPlayer = justAdded as UnityEngine.Video.VideoPlayer;
                            videoPlayer.clip = toAdd as VideoClip;
                        }
                        else if (justAdded is UnityEngine.UI.Text)
                        {
                            UnityEngine.UI.Text text = justAdded as UnityEngine.UI.Text;
                            text.font = toAdd as Font;
                        }
                        Component[] _components = pendingOperation.targetObject.GetComponents<Component>();
                        int curIndex = _components.Length - 1;
                        HandleMoveComponent(justAdded, curIndex, (pendingOperation.targetIndex + i), _components);
                        componentScrollView.ScrollToElement("Component" + (pendingOperation.targetIndex + 1), true, true);

                    }
                }
                FinalizeMovementRecording(null, pendingOperation.targetObject, "Asset Dragged as Component");
                return;
            }
            if (pendingOperation == null || pendingOperation.targetIndex == -1 || pendingOperation.draggedComponent == null)
            {
                return;
            }
            int targetIndex = pendingOperation.targetIndex;
            int index = pendingOperation.sourceIndex;
            alreadyMovingComponent = true;
            Component draggedComponent = pendingOperation.draggedComponent;
            Component[] components = pendingOperation.targetObject.GetComponents<Component>();
            if (pendingOperation.isSelf)
            {
                if (pendingOperation.prefabError)
                {
                    pendingOperation.consumed = true;
                    pendingOperation = null;
                    GameObject prefab = PrefabUtility.GetCorrespondingObjectFromSource(draggedComponent.gameObject);
                    EditorUtils.ShowPrefabFailedMessage(prefab);
                    return;
                }
                if (pendingOperation.errored)
                {
                    pendingOperation.consumed = true;
                    pendingOperation = null;
                    EditorUtils.ShowPasteFailedMessage();
                    return;
                }
                string operation = "Moved Component";
                if (pendingOperation.isCopy)
                {
                    ComponentUtility.CopyComponent(draggedComponent);
                    int numComponents = components.Length;
                    operation = "Cloned Component";
                    if (ComponentUtility.PasteComponentAsNew(pendingOperation.targetObject))
                    {
                        Repaint();
                        components = pendingOperation.targetObject.GetComponents<Component>();
                        if (numComponents == components.Length)
                        {
                            pendingOperation.consumed = true;
                            pendingOperation = null;
                            EditorUtils.ShowPasteFailedMessage();
                            return;
                        }
                        draggedComponent = components[components.Length - 1];
                        index = components.Length - 1;
                        GetActiveTab().SaveFoldoutToMap(draggedComponent, pendingOperation.foldoutOrigin, null, 2);
                        HandleMoveComponent(draggedComponent, index, targetIndex, components);
                        FinalizeMovementRecording(draggedComponent, pendingOperation.targetObject, operation);
                        return;
                    }
                    else
                    {
                        pendingOperation.consumed = true;
                        pendingOperation = null;
                        EditorUtils.ShowPasteFailedMessage();
                        return;
                    }
                }
                else
                {
                    GetActiveTab().SaveFoldoutToMap(draggedComponent, pendingOperation.foldoutOrigin, null, 1);

                    HandleMoveComponent(draggedComponent, index, targetIndex, components);
                    FixComponentScrollAfterDrag(pendingOperation);
                    FinalizeMovementRecording(draggedComponent, pendingOperation.targetObject, operation);

                }
            }
            else
            {
                if (pendingOperation.errored)
                {
                    pendingOperation.consumed = true;
                    pendingOperation = null;
                    EditorUtils.ShowPasteFailedMessage();
                    return;
                }
                bool removeAfter = pendingOperation.removeAfter;
                ComponentUtility.CopyComponent(draggedComponent);
                if (ComponentUtility.PasteComponentAsNew(pendingOperation.targetObject))
                {
                    string operation = "Cloned Component";
                    components = pendingOperation.targetObject.GetComponents<Component>();
                    if (removeAfter)
                    {
                        Undo.DestroyObjectImmediate(draggedComponent);
                        operation = "Dragged Component";
                    }
                    draggedComponent = components[components.Length - 1];
                    index = components.Length - 1;
                    operation += " to " + pendingOperation.targetObject.name;
                    int cloneMode = removeAfter ? 1 : 2;
                    GetActiveTab().SaveFoldoutToMap(draggedComponent, pendingOperation.foldoutOrigin, null, cloneMode);
                    HandleMoveComponent(draggedComponent, index, targetIndex, components);
                    FinalizeMovementRecording(draggedComponent, pendingOperation.targetObject, operation);
                    return;
                }
                else if (pendingOperation.targetObject.GetComponent(draggedComponent.GetType()) != null)
                {
                    pendingOperation.consumed = true;
                    pendingOperation = null;
                    EditorUtils.ShowPasteFailedMessage();
                    return;
                }
                else
                {
                    pendingOperation.consumed = true;
                    pendingOperation = null;
                    EditorUtils.ShowPasteFailedMessageError();
                    return;
                }
            }
            Repaint();
        }
        private MaterialMapManager PrefabMaterialMapManager
        {
            get
            {
                if (prefabMaterialManager == null)
                {
                    prefabMaterialManager = new MaterialMapManager();
                }
                return prefabMaterialManager;
            }
        }
        private ComponentMapManager PrefabComponentMapManager
        {
            get
            {
                if (prefabComponentManager == null)
                {
                    prefabComponentManager = new ComponentMapManager();
                }
                return prefabComponentManager;
            }
        }

        internal bool IsComponentFilteredInTab(ComponentMap componentMap)
        {
            if (componentMap == null)
            {
                return false;
            }
            if (!AreWeFilteringComponents())
            {
                componentMap.isFilteredOut = false;
                return true;
            }
            if (GetActiveTab().filterString == "" || GetActiveTab().filterString == null)
            {
                componentMap.isFilteredOut = false;
                return true;
            }
            if (componentMap.component == null)
            {
                return false;
            }
            componentMap.isFilteredOut = true;
            componentMap.FillInNames();
            string typeName = componentMap.componentName;
            string niceTypeName = componentMap.niceComponentName;
            string filter = GetActiveTab().filterString.ToLower();
            bool match = typeName.Contains(filter) || niceTypeName.Contains(filter);
            if (match)
            {
                componentMap.isFilteredOut = false;
                return true;
            }
            return false;
        }

        internal bool IsComponentFilteredInTab(Component component, Editor editor)
        {
            if (GetActiveTab() == null)
            {
                return false;
            }
            ComponentMap componentMap = GetActiveTab().GetFoldoutMapForComponent(component, editor);
            if (componentMap == null)
            {
                return false;
            }
            return IsComponentFilteredInTab(componentMap);

        }
        internal bool AreWeFilteringComponents()
        {
            if (!showFilterBar || !GetActiveTab().filtering)
            {
                return false;
            }
            if (GetActiveTab() == null || IsActiveTabNew())
            {
                return false;
            }
            if (GetActiveTab().filterString == "" || GetActiveTab().filterString == null)
            {

                return false;
            }
            return true;
        }

        private bool DrawDragSpace(int index)
        {
            int hoverIndex = index;
            if (index > 0)
            {
                hoverIndex--;
                if (pendingOperation != null && pendingOperation.targetIndex == hoverIndex && !pendingOperation.isAsset)
                {
                    GUILayout.Space(5);
                    return true;
                }
                else
                {
                    GUILayout.Space(0);
                    return false;
                }
            }
            return false;
        }
        private void DrawAfterComponentBar(bool isDragged, bool isCopied, ComponentMap componentMap, Rect compHeaderRect, int index, bool flag)
        {
            if (isDragged)
            {
                if (!flag)
                {
                    Rect rect = new Rect(compHeaderRect)
                    {
                        width = 10
                    };
                    EditorGUI.DrawRect(rect, CustomColors.DefaultInspector);
                }
                if (!isCopied)
                {
                    if (EditorUtils.IsLightSkin())
                    {
                        EditorUtils.DrawFadeToLeft(compHeaderRect, CustomColors.CustomBlue * 0.4f);
                    }
                    else
                    {
                        EditorUtils.DrawFadeToLeft(compHeaderRect, CustomColors.CustomSubtleBlue);
                    }
                }
                else
                {
                    if (EditorUtils.IsLightSkin())
                    {
                        EditorUtils.DrawFadeToLeft(compHeaderRect, CustomColors.CustomGreen * 0.6f);
                    }
                    else
                    {
                        EditorUtils.DrawFadeToLeft(compHeaderRect, CustomColors.SubtleGreen);
                    }
                }
            }
            if (componentMap.focusAfter != 0)
            {
                bool isCopy = componentMap.focusAfter == 2;
                componentMap.focusAfter = 0;
                var component = componentScrollView.GetChild("Component" + index);

                if (component != null)
                {
                    if (componentMap.awaitingScroll)
                    {
                        if (!EditorUtils.HasProblematicScrollComponent(componentMap.component.gameObject))
                        {
                            componentScrollView.ScrollToElement(component, true, isCopy);
                        }
                        componentMap.awaitingScroll = false;
                    }
                    else
                    {
                        componentScrollView.FocusChild(component, isCopy);
                    }
                }
            }

        }

        private bool DrawComponentIsDragged(ComponentMap componentMap, Component component, int index, out bool isDragged, out bool isCopied)
        {
            isDragged = false;
            isCopied = false;
            if (index != 0 && EditorUtils.AreWeDraggingThis(component))
            {
                isDragged = true;
                if (!componentMap.foldout)
                {
                    GUILayout.Space(10);
                }
                if (EditorUtils.IsCtrlHeld())
                {
                    isCopied = true;
                }
            }
            else
            {
                GUILayout.Space(0);
            }
            if (index == 0)
            {
                SerializableTransform savedTransform = GetPlayModeSave(component.gameObject);
                if (Application.isPlaying)
                {
                    bool alreadySaved = savedTransform != null;
                    bool hasChanged = false;
                    bool isAutoSave = savedTransform != null && savedTransform.autoSave;
                    if (alreadySaved)
                    {
                        SerializableTransform currentTransform = new SerializableTransform(component.gameObject.transform);
                        if (savedTransform != null && currentTransform != null)
                        {
                            hasChanged = !SerializableTransform.SameTransforms(savedTransform, currentTransform);
                        }
                    }
                    int buttonPressed = Event.current.button;

                    if (GUILayout.Button(CustomGUIContents.SaveTransformContent(alreadySaved, hasChanged, isAutoSave), CustomGUIStyles.ButtonsUpRight, GUILayout.Width(19), GUILayout.Height(22)))
                    {
                        if (buttonPressed == 1)
                        {
                            GenericMenu menu = new GenericMenu();
                            string label = "Save current";
                            if (alreadySaved)
                            {
                                label = "Update Saved";
                            }
                            if (!isAutoSave)
                            {
                                if (!alreadySaved || hasChanged)
                                {
                                    menu.AddItem(new GUIContent(label + " Transform"), false, () =>
                                    {
                                        AddToPlayModeSave(component.gameObject);
                                    });
                                }
                                menu.AddItem(new GUIContent("Auto Save on Exit Play Mode"), false, () =>
                                {
                                    AddToPlayModeSave(component.gameObject, true);
                                });
                            }
                            else
                            {
                                menu.AddItem(new GUIContent("Save current Transform"), false, () =>
                                {
                                    AddToPlayModeSave(component.gameObject, false);
                                });
                            }
                            if (alreadySaved)
                            {
                                menu.AddItem(new GUIContent("Don't Save Transform"), false, () =>
                                    {
                                        RemoveFromPlayModeSave(savedTransform);
                                    });
                            }
                            menu.ShowAsContext();

                        }
                        else if ((!alreadySaved || hasChanged) && buttonPressed != 2)

                        {
                            AddToPlayModeSave(component.gameObject);
                        }
                        else
                        {
                            RemoveFromPlayModeSave(savedTransform);
                        }
                    }
                    Rect rect = GUILayoutUtility.GetLastRect();
                    rect.x = 0;
                    rect.width = 23;
                    EditorUtils.DrawLineOverRect(rect, CustomColors.HardShadow, 0);
                    if (componentMap.foldout)
                    {
                        EditorUtils.DrawLineUnderRect(rect, CustomColors.GradientShadow, -1);
                    }
                }
                else if (savedTransform != null)
                {
                    ShowSerializableTransformButton(savedTransform);
                }
            }
            GUI.enabled = true;
            if (isDragged && !isCopied)
            {
                GUI.enabled = false;
            }
            return isDragged;
        }

        private void ShowSerializableTransformButton(SerializableTransform savedTransform)
        {
            Color color = GUI.backgroundColor;
            Color newColor = color + CustomColors.DarkHistoryButton;
            GUI.backgroundColor = newColor;
            GUILayout.Space(21);
            Rect rect = GUILayoutUtility.GetLastRect();
            rect.x = 0;
            rect.width = 21;
            rect.height = 22;
            rect.y = 1;
            if (savedTransform.appliedTransform)
            {
                if (GUI.Button(rect, CustomGUIContents.UndoSaveContent, CustomGUIStyles.TransformHistoryButton))
                {
                    if (Event.current.button == 0)
                    {
                        savedTransform.Apply();
                    }
                    else if (Event.current.button == 1)
                    {
                        ShowSerializableTransformMenu(savedTransform);
                    }
                }
            }
            else
            {
                if (GUI.Button(rect, CustomGUIContents.RedoSaveContent, CustomGUIStyles.TransformHistoryButton))
                {
                    if (Event.current.button == 0)
                    {
                        savedTransform.Apply();
                    }
                    else if (Event.current.button == 1)
                    {
                        ShowSerializableTransformMenu(savedTransform);
                    }
                }
            }
            EditorUtils.DrawLineOverRect(rect, CustomColors.HardShadow, 1);
            EditorUtils.DrawLineOverRect(rect);
            GUI.backgroundColor = color;
            rect.width = 20;
            EditorUtils.DrawFadeToBottom(rect, CustomColors.SoftBright, 4);
            EditorUtils.DrawLineUnderRect(rect, CustomColors.CustomFadeBlue, -2, 2);
        }

        private void ShowSerializableTransformMenu(SerializableTransform savedTransform)
        {
            GenericMenu menu = new GenericMenu();
            if (savedTransform.appliedTransform)
            {
                menu.AddItem(new GUIContent("Undo Changes"), false, () =>
                {
                    savedTransform.Apply();
                });
                menu.AddDisabledItem(new GUIContent("Redo Changes"));
            }
            else
            {
                menu.AddItem(new GUIContent("Redo Changes"), false, () =>
                {
                    savedTransform.Apply();
                });
                menu.AddDisabledItem(new GUIContent("Undo Changes"));
            }
            menu.AddSeparator("");
            menu.AddItem(new GUIContent("End Changes Edition"), false, () =>
            {
                RemoveFromPlayModeSave(savedTransform);
            });
            menu.ShowAsContext();
        }
        private void ShowSerializableTransformMenu(List<SerializableTransform> savedTransforms, bool undoMode = true)
        {
            GenericMenu menu = new GenericMenu();
            if (undoMode)
            {
                menu.AddItem(new GUIContent("Undo All Changes"), false, () =>
                {
                    foreach (var savedTransform in savedTransforms)
                    {
                        if (savedTransform.appliedTransform)
                        {
                            savedTransform.Apply();
                        }
                    }
                });
                menu.AddDisabledItem(new GUIContent("Redo All Changes"));
            }
            else
            {
                menu.AddItem(new GUIContent("Redo All Changes"), false, () =>
                {
                    foreach (var savedTransform in savedTransforms)
                    {
                        if (!savedTransform.appliedTransform)
                        {
                            savedTransform.Apply();
                        }
                    }
                });
                menu.AddDisabledItem(new GUIContent("Undo All Changes"));
            }
            menu.AddSeparator("");
            menu.AddItem(new GUIContent("End Changes Edition"), false, () =>
            {
                RemoveFromPlayModeSave(savedTransforms);
            });
            menu.ShowAsContext();
        }

        internal static void UpdateLabelSize()
        {
            if (!MainCoInspector)
            {
                return;
            }
            if (MainCoInspector)
            {
                EditorGUIUtility.wideMode = true;
                EditorGUIUtility.labelWidth = MainCoInspector.position.width / 2.4f;

            }
        }
        private void HandleDragOperations(int index, Component component, Component[] components, Editor editor, float importedHeight = 0)
        {
            bool areWeDragging = DragAndDrop.objectReferences != null && DragAndDrop.objectReferences.Length > 0 && !triggeringADrag;
            if (!areWeDragging)
            {
                return;
            }
            bool escape = Event.current.keyCode == KeyCode.Escape;
            if (escape)
            {
                performPasteComponent = -1;
                pendingComponentDrag = false;
                pendingOperation = null;
                ignoreNextDragEvent = true;
                DragAndDrop.objectReferences = null;
                DragAndDrop.PrepareStartDrag();
                return;
            }
            EditorGUILayout.BeginVertical();
            GUILayout.Space(0);
            EditorGUILayout.EndVertical();
            Rect componentRect = GUILayoutUtility.GetLastRect();
            componentRect.height += importedHeight;
            componentRect.y = 0;
            UnityObject[] draggedObjects = DragAndDrop.objectReferences;
            Rect adjustedRect = new Rect(componentRect);

            int threshold;
            {
                ComponentMap map = GetActiveTab().GetFoldoutMapForComponent(index);
                bool isCollapsed = false;
                if (map.foldout == false || (map.hidden && !ActiveTabInDebugMode()))
                {
                    isCollapsed = true;
                }
                if (!isCollapsed)
                {
                    threshold = 14;
                    adjustedRect.height -= 20;
                    adjustedRect.y += 10;
                }
                else
                {
                    threshold = 14;
                    adjustedRect.height = 15;
                    adjustedRect.y -= 2;
                }
            }
            float distanceFromBottom = Event.current.mousePosition.y - (adjustedRect.y + adjustedRect.height);
            bool isNegative = adjustedRect.height < 0;
            Rect realInfluenceRect = new Rect(adjustedRect.x, mousePosition.y - 10, adjustedRect.width, 20);
            bool isMouseBelowRect = !escape && EditorWindow.mouseOverWindow == this && distanceFromBottom >= 0 && distanceFromBottom <= threshold && !isNegative;
            if (draggedObjects.Length == 1 && draggedObjects[0] is Component)
            {
                Component draggedComponent = draggedObjects[0] as Component;
                if (draggedComponent is Transform || draggedComponent is RectTransform)
                {
                    return;
                }

                GameObject owner = draggedComponent.gameObject;
                bool isSelf = owner == component.gameObject;
                if (isMouseBelowRect)
                {
                    StartOrUpdateDragOperation(index, component, draggedComponent, realInfluenceRect, editor);
                    bool copy = EditorUtils.IsCtrlHeld() || DraggingPrefabComponents();
                    bool canCopy = EditorUtils.CanAddMultipleTimes(draggedComponent, component.gameObject);
                    int correctedIndex = index + 1 >= components.Length ? index : index + 1;
                    bool prefabCanMove = EditorUtils.CanMovePrefabComponents(draggedComponent, components[correctedIndex]);
                    bool prefabCantModify = !IsGameObjectInPrefabMode(component.gameObject) && PrefabUtility.IsPartOfPrefabInstance(component.gameObject) && !prefabCanMove && !canCopy;
                    if ((prefabCantModify && isSelf) || !prefabCanMove)
                    {
                        DragAndDrop.visualMode = DragAndDropVisualMode.Rejected;
                        EditorUtils.DrawLineUnderRect(componentRect, CustomColors.CustomRed, -5, 5);
                        performPasteComponent = 4;
                    }
                    else
                    {
                        if (copy)
                        {
                            if (canCopy)
                            {
                                alreadyMovingComponent = false;
                                EditorUtils.DrawLineUnderRect(componentRect, CustomColors.CustomGreen, -5, 5);
                                performPasteComponent = 2;
                                DragAndDrop.visualMode = DragAndDropVisualMode.Copy;
                            }
                            else
                            {
                                alreadyMovingComponent = false;
                                EditorUtils.DrawLineUnderRect(componentRect, CustomColors.CustomRed, -5, 5);
                                performPasteComponent = 1;
                                DragAndDrop.visualMode = DragAndDropVisualMode.Rejected;
                            }
                        }
                        else
                        {
                            if (isSelf || canCopy)
                            {
                                alreadyMovingComponent = false;
                                EditorUtils.DrawLineUnderRect(componentRect, CustomColors.CustomBlue, -5, 5);
                                performPasteComponent = 0;
                                DragAndDrop.visualMode = DragAndDropVisualMode.Move;
                            }
                            else
                            {
                                alreadyMovingComponent = false;
                                EditorUtils.DrawLineUnderRect(componentRect, CustomColors.CustomRed, -5, 5);
                                performPasteComponent = 1;
                                DragAndDrop.visualMode = DragAndDropVisualMode.Rejected;
                            }
                        }
                    }
                }
            }
            else if (draggedObjects.Length > 0)
            {
                List<UnityObject> validAssets = new List<UnityObject>();
                List<UnityObject> invalidAssets = new List<UnityObject>();
                bool repeatingAsset = false;
                foreach (UnityObject draggedObject in draggedObjects)
                {
                    if (EditorUtils.IsValidAssetType(draggedObject, component.gameObject, out repeatingAsset) && !(draggedObject is GameObject))
                    {
                        validAssets.Add(draggedObject);
                    }
                    else if (draggedObject != null && !(draggedObject is GameObject))
                    {
                        invalidAssets.Add(draggedObject);
                    }
                }
                if (isMouseBelowRect)
                {
                    if (validAssets.Count > 0)
                    {
                        alreadyMovingComponent = false;
                        EditorUtils.DrawLineUnderRect(componentRect, CustomColors.CustomGreen, -5, 5);
                        DragAndDrop.visualMode = DragAndDropVisualMode.Copy;
                        pendingComponentDrag = true;
                        pendingOperation = new ComponentDragOperation
                        {
                            isMouseBelowRect = true,
                            isAsset = true,
                            assets = validAssets,
                            targetIndex = index,
                            targetObject = component.gameObject,
                            mouseOverRect = realInfluenceRect
                        };
                    }
                    else if (invalidAssets.Count > 0)
                    {
                        if (repeatingAsset)
                        {
                            pendingComponentDrag = true;
                            pendingOperation = new ComponentDragOperation
                            {
                                isMouseBelowRect = true,
                                errored = true,
                                targetIndex = index,
                                isAsset = true,
                                draggedComponent = component,
                                targetObject = component.gameObject,
                                mouseOverRect = realInfluenceRect
                            };
                        }
                        alreadyMovingComponent = false;
                        EditorUtils.DrawLineUnderRect(componentRect, CustomColors.CustomRed, -5, 5);
                        DragAndDrop.visualMode = DragAndDropVisualMode.Rejected;
                    }
                }
            }
        }
        private void StartOrUpdateDragOperation(int index, Component component, Component draggedComponent, Rect realInfluenceRect, Editor editor)
        {
            pendingComponentDrag = true;
            if (pendingOperation == null)
            {
                pendingOperation = new ComponentDragOperation();
            }
            pendingOperation.targetObject = component.gameObject;
            pendingOperation.isMouseBelowRect = true;
            pendingOperation.draggedEditor = editor;
            pendingOperation.sourceIndex = Array.IndexOf(component.gameObject.GetComponents<Component>(), draggedComponent);
            pendingOperation.targetIndex = index;
            pendingOperation.draggedComponent = draggedComponent;
            if (pendingOperation.sourceTabIndex == -1)
            {
                pendingOperation.sourceTabIndex = activeIndex;
            }
            pendingOperation.foldoutOrigin = tabs[pendingOperation.sourceTabIndex].GetFoldoutForComponent(draggedComponent, editor);
            pendingOperation.isSelf = component.gameObject == draggedComponent.gameObject;
            pendingOperation.errored = performPasteComponent == 1;
            pendingOperation.removeAfter = performPasteComponent == 0;
            pendingOperation.isCopy = performPasteComponent == 2;
            pendingOperation.prefabError = performPasteComponent == 4;
            pendingOperation.mouseOverRect = realInfluenceRect;
            GUI.changed = true;
            DragAndDrop.visualMode = DragAndDropVisualMode.Move;
        }

        void ShowNotMultiComponentGUI()
        {
            GUILayout.BeginVertical(CustomGUIStyles.ComponentBoxStyle);
            GUILayout.Label("Multi-object editing not supported", EditorStyles.miniLabel);
            GUILayout.EndVertical();
            Rect rect = GUILayoutUtility.GetLastRect();
            EditorUtils.DrawRectBorder(rect, CustomColors.MediumShadow);
            EditorUtils.DrawLineUnderRect(rect, CustomColors.SimpleBright);
        }

        void DrawMissingAssetScriptHeader(bool multi = false)
        {
            multi = MissingScriptManager.IsMulti;
            EditorGUI.indentLevel = 0;
            GUIContent _content = new GUIContent(AssetPreview.GetMiniThumbnail(targetObject));
            EditorGUILayout.BeginVertical(CustomGUIStyles.InspectorSectionStyle);
            GUILayout.Space(8);
            GUILayout.Label(_content, GUILayout.Height(35));
            GUILayout.Space(10);
            EditorGUILayout.EndVertical();
            Rect rect = GUILayoutUtility.GetLastRect();
            rect.x += 45;
            rect.y -= 12;
            string headerText = "(Missing Script)";
            MissingScriptManager missingScript = targetObject as MissingScriptManager;
            if (missingScript != null && missingScript.path != "")
            {
                headerText = missingScript.assetName + " (Missing Script)";
                if (multi)
                {
                    headerText = missingScript.assetName + " (Missing Scripts)";
                }
                UpdateLabelSize();
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.BeginVertical(EditorStyles.inspectorDefaultMargins);
                EditorGUIUtility.labelWidth -= 20;
                GUI.enabled = false;
                EditorGUILayout.ObjectField("Script", missingScript.script, typeof(MonoScript), false);
                GUI.enabled = true;
                GUILayout.Space(EditorGUIUtility.standardVerticalSpacing);
#if UNITY_6000_4_OR_NEWER
                string entityID = missingScript.entityID.GetHashCode().ToString();

#else
                string entityID = missingScript.instanceID.ToString();

#endif

                string localIdentifier = missingScript.localIdentifierInFile.ToString();
                if (multi)
                {
                    entityID = "―";
                    localIdentifier = "―";
                }
#if UNITY_6000_4_OR_NEWER
                EditorGUILayout.TextField("Entity ID", entityID);

#else
                EditorGUILayout.TextField("Instance ID", entityID);

#endif
                GUILayout.Space(EditorGUIUtility.standardVerticalSpacing);
                EditorGUILayout.TextField("Local identifier in file", localIdentifier);
                ShowMissingAssetScript(multi);
                EditorGUILayout.EndVertical();
                EditorGUILayout.EndHorizontal();
            }
            GUI.Label(rect, headerText, CustomGUIStyles.MediumLabel);

        }
        void ShowMissingAssetScript(bool multi = false)
        {
            string message = "The associated script can not be loaded.\nPlease fix any compile errors or assign a valid script in the regular Inspector.";
            if (multi)
            {
                message = "One or more Objects have missing scripts.\nPlease fix any compile errors or assign a valid script in the regular Inspector.";
            }
            EditorGUILayout.HelpBox(message, MessageType.Warning);
            EditorGUILayout.Space(2);
            EditorGUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            GUIContent content = CustomGUIContents.MissingComponentContent;
            content.image = CustomGUIContents.EditContentDefault.image;
            if (GUILayout.Button(content, GUILayout.Width(130)))
            {
                {
                    PopUpInspectorWindow(new UnityObject[] { targetObject }, false);
                }
            }
            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.Space(2);
        }

        void ShowMissingComponent(bool multi = false)
        {
            if (AreWeFilteringComponents())
            {
                return;
            }
            EditorGUILayout.BeginHorizontal(CustomGUIStyles.InspectorButtonStyle, GUILayout.Height(18));
            GUILayout.Space(60);
            EditorGUILayout.LabelField("(Missing Script!)", CustomGUIStyles.BoldLabel, GUILayout.Height(17));
            EditorGUILayout.EndHorizontal();
            Rect rect = EditorUtils.GetLastLineRect();
            EditorUtils.DrawLineOverRect(rect, 0);
            EditorUtils.DrawLineOverRect(rect, CustomColors.GradientShadow, 1);
            EditorUtils.DrawLineUnderRect(rect, 0);
            EditorUtils.DrawLineUnderRect(rect, CustomColors.SimpleShadow, 0, 2);
            GUI.enabled = false;
            GUIContent _content = new GUIContent(AssetPreview.GetMiniTypeThumbnail(this.GetType()));
            rect.x = 20;
            rect.y = 3;
            GUI.Label(rect, _content, CustomGUIStyles.IconButton);
            rect.x = 40;
            rect.y = 0;
            GUI.Toggle(rect, true, "");
            GUI.enabled = true;
            EditorGUILayout.Space(5);
            UpdateLabelSize();
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.BeginVertical(EditorStyles.inspectorDefaultMargins);
            EditorGUIUtility.labelWidth -= 20;
            GUI.enabled = false;
            MonoScript script = null;
            EditorGUILayout.ObjectField("Script", script, typeof(MonoScript), false);
            GUI.enabled = true;
            EditorGUILayout.EndVertical();
            EditorGUILayout.EndHorizontal();
            string message = "The associated script cannot be loaded.\nPlease, fix any compile errors or assign a valid script in the regular Inspector.";
            if (multi)
            {
                message = "One or more Components have missing scripts.\nPlease, fix any compile errors or assign a valid script in the regular Inspector.";
            }
            EditorGUILayout.BeginVertical(EditorStyles.inspectorDefaultMargins);
            EditorGUILayout.HelpBox(message, MessageType.Warning);
            EditorGUILayout.EndVertical();
            EditorGUILayout.Space(2);
            EditorGUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            GUIContent content = CustomGUIContents.MissingComponentContent;
            content.image = CustomGUIContents.EditContentDefault.image;
            if (GUILayout.Button(content, GUILayout.Width(130)))
            {
                if (IsActiveTabValidMulti())
                {
                    PopUpInspectorWindow(GetActiveTab().targets, false);
                }
                else
                {
                    PopUpInspectorWindow(new GameObject[] { GetActiveTab().target }, false);
                }
            }
            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.Space(5);
        }
        void ShowPrefabMissingComponent(bool multi = false)
        {
            string message = "This Prefab has missing scripts. Open it in the regular Inspector to fix the issue.";
            if (multi)
            {
                message = "One or more Prefabs have missing scripts.\nOpen them individually in regular Inspector to fix the issue.";
            }
            EditorGUILayout.HelpBox(message, MessageType.Warning);
            if (multi)
            {
                GUI.enabled = false;
            }
            GUILayout.Space(5);
            EditorGUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            GUIContent content = CustomGUIContents.MissingComponentContent;
            if (GUILayout.Button(content, GUILayout.Width(130)))
            {
                ignoreNextSelection = true;
                PopUpInspectorWindow(new GameObject[] { targetObject as GameObject }, false);
                OpenPrefab();
            }
            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();
            GUILayout.Space(10);
            GUI.enabled = true;
        }
        static bool DraggingPrefabComponents()
        {
            UnityObject[] draggedObjects = DragAndDrop.objectReferences;
            if (draggedObjects == null || draggedObjects.Length == 0)
            {
                return false;
            }
            foreach (UnityObject draggedObject in draggedObjects)
            {
                if (draggedObject is Component)
                {
                    Component component = draggedObject as Component;
                    if (component.gameObject != null && EditorUtils.IsAPrefabAsset(component.gameObject))
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        private void RebuildPrefabMultiComponentEditors()
        {
            DestroyAllIfNotNull(prefabEditors);
            prefabEditors = null;
            differentPrefabComponents = false;
            GameObject[] prefabs = new GameObject[targetObjects.Length];
            for (int i = 0; i < targetObjects.Length; i++)
            {
                prefabs[i] = targetObjects[i] as GameObject;
            }
            GameObject lastObject = prefabs[0];
            if (lastObject == null)
            {
                return;
            }
            int lastObjectIndex = 0;
            List<KeyValuePair<Type, List<List<Component>>>> map = EditorUtils.OrderedComponentMap(prefabs, this, true);
            if (map == null)
            {
                return;
            }
            List<Component[]> orderedComponentArrays = new List<Component[]>();
            foreach (Component comp in lastObject.GetComponents<Component>())
            {
                if (comp == null) continue;

                Type compType = comp.GetType();
                var typeEntry = map.FirstOrDefault(e => e.Key == compType);
                if (typeEntry.Key != null)
                {
                    int compIndex = typeEntry.Value[lastObjectIndex].IndexOf(comp);
                    if (compIndex != -1)
                    {
                        Component[] targetComponents = typeEntry.Value.Select(list => list[compIndex]).ToArray();
                        orderedComponentArrays.Add(targetComponents);
                    }
                }
            }
            List<Editor> editorList = new List<Editor>();
            foreach (Component[] targetComponents in orderedComponentArrays)
            {
                Editor editor = null;
                Editor.CreateCachedEditor(targetComponents, null, ref editor);
                if (editor != null)
                {
                    editorList.Add(editor);
                }
            }
            prefabEditors = editorList.ToArray();
            prefabFoldouts_ = new bool[prefabEditors.Length];
            prefabFoldoutsChangeTracker_ = new bool[prefabEditors.Length];
            bool defaultCollapse = !collapsePrefabComponents;
            if (assetOnlyMode)
            {
                defaultCollapse = true;
            }
            for (int i = 0; i < prefabEditors.Length; i++)
            {
                prefabFoldouts_[i] = defaultCollapse;
                prefabFoldoutsChangeTracker_[i] = defaultCollapse;
            }
            SetAllAssetEditorsDebugTo(debugAsset);
        }

        private bool IsAnyAssetDirty(UnityObject[] objects)
        {
            if (objects == null)
            {
                return false;
            }
            foreach (var obj in objects)
            {
                if (EditorUtility.IsDirty(obj))
                {
                    return true;
                }
                if (IsEditorValid(assetImportSettingsEditor))
                {
                    if (assetImportSettingsEditor.HasModified())
                    {
                        return true;
                    }
                }
            }
            return false;
        }
        private bool IsAssetDirty(UnityObject obj)
        {
            if (obj == null)
            {
                return false;
            }
            if (EditorUtility.IsDirty(obj))
            {
                return true;
            }
            if (IsEditorValid(assetImportSettingsEditor))
            {
                if (assetImportSettingsEditor.HasModified())
                {
                    return true;
                }
            }
            return false;
        }

        internal bool IsAssetImportSettingsDirty()
        {
            if (assetImportSettingsEditor == null || assetImportSettingsEditor.targets == null || assetImportSettingsEditor.targets.Length == 0 || assetImportSettingsEditor.target == null)
            {
                return false;
            }

            if (assetImportSettingsEditor.HasModified())
            {
                return true;
            }
            return false;
        }

        bool CheckForApplyRevertOnClose()
        {
            if (IsAssetImportSettingsDirty())
            {
                int result = EditorUtils.ShowUnappliedImportSettings(assetImportSettingsEditor);
                if (result == 1)
                {
                    return false;
                }
                if (result == 0)
                {
                    Reflected.ApplyChanges(assetImportSettingsEditor);
                }
                if (result == 2)
                {
                    Reflected.DiscardChanges(assetImportSettingsEditor);
                }

            }
            return true;
        }
        internal void HandleAssetHistory(UnityEngine.Object[] newTargets)
        {
            newTargets = newTargets.Where(t => t is not MissingScriptManager).ToArray();
            if (newTargets == null || newTargets.Length == 0)
            {

                return;
            }
            UnityObject[] objects = new UnityObject[newTargets.Length];
            Array.Copy(newTargets, objects, newTargets.Length);
            var newHistoryAsset = new HistoryAssets(objects);
            CleanHistoryAssets();
            var existingIndex = historyAssets.FindIndex(entry =>
                entry.assetGUIDs.OrderBy(guid => guid).SequenceEqual(newHistoryAsset.assetGUIDs.OrderBy(guid => guid)));
            if (existingIndex != -1)
            {
                historyAssets.RemoveAt(existingIndex);
            }
            historyAssets.Insert(0, newHistoryAsset);
            if (historyAssets.Count > 11)
            {
                historyAssets.RemoveAt(11);
            }
        }
        private void CleanHistoryAssets()
        {
            if (historyAssets == null)
            {
                historyAssets = new List<HistoryAssets>();
                return;
            }
            foreach (var entry in historyAssets)
            {
                entry.assetGUIDs = entry.assetGUIDs
                    .Where(guid => AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(AssetDatabase.GUIDToAssetPath(guid)) != null)
                    .ToArray();
            }
            historyAssets.RemoveAll(entry => entry.assetGUIDs.Length == 0);
        }
        private UnityEngine.Object FindAssetByGUID(string guid)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            return AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(path);
        }
        internal void BackToPreviousAsset()
        {
            CleanHistoryAssets();
            if (historyAssets.Count == 0)
            {
                return;
            }
            int currentIndex = 0;
            if (EditorUtils.AssetAlreadyTarget(FindAssetByGUID(historyAssets[0].assetGUIDs[0]), this))
            {
                currentIndex = 1;
            }
            if (currentIndex >= historyAssets.Count)
            {
                return;
            }
            var historyEntry = historyAssets[currentIndex];
            if (historyEntry.assetGUIDs.Length == 1)
            {
                var asset = FindAssetByGUID(historyEntry.assetGUIDs[0]);
                if (asset != null)
                {
                    SetTargetAsset(asset, true);
                }
            }
            else if (historyEntry.assetGUIDs.Length > 1)
            {
                var assets = historyEntry.assetGUIDs.Select(FindAssetByGUID).Where(a => a != null).ToArray();
                SetTargetAssets(assets, true);
            }
        }
        internal void ShowHistoryContextMenu()
        {
            if (Event.current.button == 1)
            {
                BackToPreviousAsset();
                return;
            }
            GenericMenu menu = new GenericMenu();
            CleanHistoryAssets();
            bool firstIsCurrent = EditorUtils.AssetsAlreadyTargets(historyAssets[0].assetGUIDs.Select(FindAssetByGUID).ToArray(), this);
            int adjustIndex = 1;
            if (firstIsCurrent)
            {
                adjustIndex = 0;
            }
            if (historyAssets.Count < 1)
            {
                menu.AddDisabledItem(new GUIContent("No previous selections found!"));
            }
            else
            {
                for (int i = 0; i < historyAssets.Count; i++)
                {
                    if (i == 1 && firstIsCurrent)
                    {
                        menu.AddSeparator("");
                    }
                    bool status = false;
                    var historyEntry = historyAssets[i];
                    if (historyEntry.assetGUIDs.Length == 1)
                    {
                        var asset = FindAssetByGUID(historyEntry.assetGUIDs[0]);
                        status = EditorUtils.AssetAlreadyTarget(asset, this);
                        if (asset != null)
                        {
                            string preName = i + adjustIndex + ". ";
                            if (status)
                            {
                                preName = "✓ ";
                            }
                            string assetPath = AssetDatabase.GetAssetPath(asset);
                            bool isFolder = AssetDatabase.IsValidFolder(assetPath);
                            string assetNameWithExtension = !isFolder ?
                                                            CleanSlash(asset.name) + Path.GetExtension(assetPath).ToLower() :
                                                            asset.name;
                            menu.AddItem(new GUIContent(preName + assetNameWithExtension), false, () => SetTargetAsset(asset, true));
                        }
                    }
                    else if (historyEntry.assetGUIDs.Length > 1)
                    {
                        var assets = historyEntry.assetGUIDs.Select(FindAssetByGUID).Where(a => a != null).ToArray();
                        status = EditorUtils.AssetsAlreadyTargets(assets, this);
                        string menuPath = $"({historyEntry.assetGUIDs.Length}) Assets/";
                        string preName = i + adjustIndex + ". ";
                        if (status)
                        {
                            preName = "✓ ";
                        }
                        menuPath = preName + menuPath;
                        menu.AddItem(new GUIContent(menuPath + "- Re-select All -"), false, () =>
                        {
                            SetTargetAssets(assets, true);
                        });
                        for (int j = 0; j < historyEntry.assetGUIDs.Length; j++)
                        {
                            var guid = historyEntry.assetGUIDs[j];
                            var asset = FindAssetByGUID(guid);
                            if (asset != null)
                            {
                                status = EditorUtils.AssetAlreadyTarget(asset, this);
                                string _preName = j + 1 + ". ";
                                string assetPath = AssetDatabase.GetAssetPath(asset);
                                bool isFolder = AssetDatabase.IsValidFolder(assetPath);
                                string assetNameWithExtension = !isFolder ?
                                                                _preName + CleanSlash(asset.name) + Path.GetExtension(assetPath).ToLower() :
                                                                _preName + CleanSlash(asset.name);
                                menu.AddItem(new GUIContent(menuPath + assetNameWithExtension), status, () => SetTargetAsset(asset, true));
                            }
                        }
                    }
                }
            }
            menu.AddSeparator("");
            menu.AddItem(new GUIContent("Wipe Asset History"), false, WipeAssetHistory);
            float height = 20 * (historyAssets.Count + 1);
            if (position.height - Event.current.mousePosition.y < height)
            {
                menu.DropDown(new Rect(Event.current.mousePosition.x, Event.current.mousePosition.y - height, 0, 0));
            }
            else
            {
                menu.ShowAsContext();
            }
        }

        static string CleanSlash(string path)
        {
            return path.Replace("/", " ∕ ");
        }
        private void WipeAssetHistory()
        {

            if (EditorUtility.DisplayDialog("Wipe Asset History",
                "You sure about this?", "Yes", "No"))
            {
                DoWipeAssetHistory();
            }
        }
        internal void DoWipeAssetHistory()
        {
            if (historyAssets == null)
            {
                return;
            }
            historyAssets.Clear();
        }

        static bool isText(UnityObject obj)
        {
            if (obj is TextAsset || obj is Shader)
            {
                return true;
            }
            return false;
        }
        public bool IsLastRectHovered()
        {
            if (Event.current == null)
            {
                return false;
            }
            Rect lastRect = GUILayoutUtility.GetLastRect();
            var position = Event.current.mousePosition;
            if (dragging && !GOdragging)
            {
                position = new Vector2(position.x, lastRect.y);
                int xLimit = 2;
                if (showHistory)
                {
                    xLimit = 45;
                }
                if (position.x < xLimit)
                {
                    dragTargetIndex = 0;
                    position.x = xLimit;
                }
            }
            return lastRect.Contains(position);
        }
        void ShowTabContextMenu(Rect buttonRect, int i, int click, GameObject item)
        {

            PopUpTip.Hide();
            GenericMenu menu = new GenericMenu();
            menu.AddItem(new GUIContent("Add Tab to the Right"), false, () =>
                {
                    int previousIndex = activeIndex;
                    activeIndex = click;
                    AddTabNext(null);

                });
            menu.AddItem(new GUIContent("Duplicate Tab"), false, () =>
            {
                DuplicateTabNext(click);
            });
            menu.AddSeparator("");
            if (item || tabs[i].multiEditMode)
            {
                if (tabs[i].locked)
                {
                    menu.AddItem(new GUIContent("Unlock Tab"), false, () =>
                    {
                        tabs[click].locked = false;
                        UpdateAllWidths();
                    });
                }
                else
                {
                    menu.AddItem(new GUIContent("Lock Tab"), false, () =>
                    {
                        tabs[click].locked = true;
                        UpdateAllWidths();
                    });
                }
                if (!tabs[click].IsValidMultiTarget() && PrefabUtility.IsPartOfPrefabInstance(item))
                {
                    menu.AddSeparator("");
                    string typeGO = "Prefab Asset";
                    var (prefabInstance, path, prefabAsset) = EditorUtils.GetPrefabReferences(item);

                    if (PoolCache.IsAnImportedObject(prefabAsset))
                    {
                        typeGO = "Imported Object";
                        menu.AddItem(new GUIContent("Edit Imported Object"), false, () =>
                    {

                        if (prefabInstance && !string.IsNullOrEmpty(path))
                        {
                            OpenAsset(path);
                        }
                    });
                    }
                    else
                    {
                        menu.AddItem(new GUIContent("Open Prefab in Scene Context"), false, () =>
                        {
                            if (prefabInstance && !string.IsNullOrEmpty(path))
                            {
                                DoOpenPrefabInScene(path, item);
                            }
                            else
                            {
                                Debug.LogWarning("[CoInspector] No valid prefab asset found to open.");
                            }
                        });

                        menu.AddItem(new GUIContent("Open Prefab in Isolation"), false, () =>
                        {

                            if (prefabInstance && !string.IsNullOrEmpty(path))
                            {
                                DoOpenPrefabInIsolation(path, item);
                            }
                        });
                    }
                    menu.AddItem(new GUIContent("Ping " + typeGO), false, () =>
                    {

                        if (prefabInstance && !string.IsNullOrEmpty(path))
                        {
                            EditorWindow.GetWindow(Reflected.GetProjectWindowType());
                            EditorGUIUtility.PingObject(prefabAsset);
                        }
                    });
                }
                else if (IsGameObjectInPrefabMode(item))
                {

                    GameObject prefab = GetPrefabStageRoot();
                    if (prefab)
                    {
                        menu.AddSeparator("");
                        menu.AddItem(new GUIContent("Ping Prefab Asset"), false, () =>
                        {
                            prefab = AssetDatabase.LoadAssetAtPath<GameObject>(GetPrefabStageRootPath());
                            if (prefab)
                            {
                                EditorGUIUtility.PingObject(prefab);
                            }
                        });
                    }
                }
                menu.AddSeparator("");
                if (tabs[click].IsValidMultiTarget())
                {
                    if (!IsAlreadySelected(tabs[click].targets))
                    {
                        menu.AddItem(new GUIContent("Select in Hierarchy"), false, () =>
                        {
                            ignoreSelection = tabs[click].targets;
                            Selection.objects = tabs[click].targets;
                        });
                    }
                    else
                    {
                        menu.AddDisabledItem(new GUIContent("Select in Hierarchy"));
                    }
                }
                else if (item != Selection.activeGameObject)
                {
                    menu.AddItem(new GUIContent("Select in Hierarchy"), false, () =>
                    {
                        ignoreSelection = new GameObject[] { item };
                        Selection.objects = ignoreSelection;
                        Selection.activeGameObject = item;
                    });
                }
                else
                {
                    menu.AddDisabledItem(new GUIContent("Select in Hierarchy"));
                }
                menu.AddItem(new GUIContent("Frame on Scene View"), false, () =>
               {
                   if (tabs[click].IsValidMultiTarget())
                   {
                       EditorUtils.FocusOnSceneView(tabs[click].targets);
                   }
                   else
                   {
                       EditorUtils.FocusOnSceneView(item);
                   }
               });
                menu.AddItem(new GUIContent("Ping in Hierarchy"), false, () =>
            {
                if (tabs[click].IsValidMultiTarget())
                {
                    EditorGUIUtility.PingObject(tabs[click].targets[0]);
                }
                else
                {
                    EditorGUIUtility.PingObject(item);
                }
            });
                if (!tabs[click].IsValidMultiTarget() && item != null)
                {
                    menu.AddItem(new GUIContent("Show In Local Hierarchy View"), false, () =>
                {
                    HierarchyPopup.ShowWindow(tabs[click].target, this, clickMousePosition);
                });
                }
                else
                {
                    menu.AddDisabledItem(new GUIContent("Show In Local Hierarchy View"));
                }
            }
            else
            {
                menu.AddDisabledItem(new GUIContent("Lock Tab"));
                menu.AddSeparator("");
                menu.AddDisabledItem(new GUIContent("Select in Hierarchy"));
                menu.AddDisabledItem(new GUIContent("Ping in Hierarchy"));
            }
            menu.AddSeparator("");
            if (tabs.Count > 1)
            {
                menu.AddItem(new GUIContent("Close Tab"), false, () =>
            {
                CloseTab(click);
                targetGameObject = GetActiveTab().target;
            });

                if (click != 0)
                {
                    menu.AddItem(new GUIContent("Close Tabs to the Left"), false, () =>
                    {
                        for (int j = 0; j < click; j++)
                        {
                            FixActiveIndex();
                            CloseTab(0, true, true);
                        }
                        FixActiveIndex();
                        FocusTab(activeIndex);
                    });
                }
                if (click != tabs.Count - 1)
                {
                    menu.AddItem(new GUIContent("Close Tabs to the Right"), false, () =>
                    {
                        for (int j = tabs.Count - 1; j > click; j--)
                        {
                            FixActiveIndex();
                            CloseTab(tabs.Count - 1, true, true);
                        }
                        FixActiveIndex();
                        FocusTab(activeIndex);
                    });
                }

                menu.AddItem(new GUIContent("Close all other Tabs"), false, () =>
                {
                    SaveSettings();
                    List<TabInfo> _tabs = new List<TabInfo>(tabs);
                    TabInfo tab = _tabs[click];
                    foreach (var _tab in _tabs)
                    {
                        if (_tab != tab)
                        {
                            FixActiveIndex();
                            CloseTab(tabs.IndexOf(_tab), true, true);
                        }
                    }
                    activeIndex = 0;
                    FocusTab(activeIndex);
                });
            }
            else
            {
                menu.AddDisabledItem(new GUIContent("Close Tab"));
                menu.AddDisabledItem(new GUIContent("Close all Other Tabs"));
            }
            menu.AddSeparator("");
            if (AreThereClosedTabs())
            {
                menu.AddItem(new GUIContent("Restore Closed Tab"), false, () =>
                {
                    RestoreClosedTab();
                });
            }
            else
            {
                menu.AddDisabledItem(new GUIContent("Restore Closed Tab"));
            }
            if (IsThereAPreviousSession())
            {
                menu.AddItem(new GUIContent("Restore Last Saved Session"), false, () =>
                {
                    ShowRecoverSessionDialogue();
                });
            }
            else
            {
                menu.AddDisabledItem(new GUIContent("Restore Last Saved Session"));
            }
            menu.AddSeparator("");
            if (!tabs[click].newTab && tabs[click] != null)
            {
                if (globalDebugMode)
                {
                    menu.AddItem(new GUIContent("Exit Full Debug Mode"), false, () =>
                    {
                        ManageGlobalDebugMode(false);
                        Repaint();
                    });
                }

                else
                {
                    menu.AddItem(new GUIContent("Normal"), !tabs[click].debug, () =>
                    {
                        tabs[click].debug = false;
                        if (click == activeIndex)
                        {
                            ManageDebugMode(false);
                            Repaint();
                        }
                    });
                    menu.AddItem(new GUIContent("Debug Tab"), tabs[click].debug, () =>
                    {
                        tabs[click].debug = true;
                        if (click == activeIndex)
                        {
                            ManageDebugMode(true);
                            Repaint();
                        }
                    });
                }
                menu.AddSeparator("");
            }
            ShowSettingsMenu(menu, true);
            float accumulatedWidth = 0;
            for (int j = 0; j < click; j++)
            {
                accumulatedWidth += totalWidth[j];
            }
            Rect _rect = new Rect(accumulatedWidth, buttonRect.y + 25, 0, 0);
            menu.DropDown(_rect);
            PopUpTip.Hide();
        }
        void DoPrefixLabel(GUIContent label, GUIStyle style)
        {
            var rect = GUILayoutUtility.GetRect(label, style, GUILayout.ExpandWidth(false));
            rect.height = Math.Max(20, rect.height);
            GUI.Label(rect, label, style);
        }
        internal void UpdateAllTabPaths()
        {
            if (tabs == null || tabs.Count == 0)
            {
                return;
            }
            foreach (var tab in tabs)
            {
                tab.UpdatePath();
            }
            if (closedTabs == null || closedTabs.Count == 0)
            {
                return;
            }
            foreach (var tab in closedTabs)
            {
                tab.UpdatePath();
            }
        }
        bool IsThereValidSession()
        {
            if (lastSessionData != null)
            {
                if (lastSessionData.tabs == null || lastSessionData.tabs.Count == 0)
                {
                    lastSessionData = null;
                    return false;
                }
                return true;
            }
            return false;
        }

        void RepopulateAllTabHistories()
        {
            if (tabs != null)
            {
                foreach (var tab in tabs)
                {
                    tab.TryRepopulateHistory();
                }
            }
            if (closedTabs != null)
            {
                foreach (var tab in closedTabs)
                {
                    tab.TryRepopulateHistory();
                }
            }
        }

        void TrySaveLastAssets()
        {
            if (settingsData != null && settingsData.sessions != null && settingsData.sessions.Any())
            {
                var mostRecentSession = settingsData.sessions
                    .OrderByDescending(session => session.GetSaveTime())
                    .FirstOrDefault();
                if (mostRecentSession != null)
                {
                    mostRecentSession.SaveAssets(this);
                }
            }
        }
        bool CanAutoFocus(int originTab, int newTab)
        {
            if (!autoFocus || IsActiveTabNew())
            {
                return false;
            }
            if (originTab == newTab)
            {
                return false;
            }
            if (tabs == null || tabs.Count == 0)
            {
                return false;
            }
            TabInfo orTab = tabs[originTab];
            TabInfo nTab = tabs[newTab];
            bool orMulti = orTab.IsValidMultiTarget();
            bool nMulti = nTab.IsValidMultiTarget();
            bool allSingle = !orMulti && !nMulti;
            if (orMulti != nMulti)
            {
                return true;
            }
            if (allSingle)
            {
                if (orTab.target == nTab.target)
                {
                    return false;
                }
            }
            else if (EditorUtils.DoArraysShareContent(orTab.targets, nTab.targets))
            {
                return false;
            }
            return true;
        }
        void RestoreLastAssets()
        {
            if (settingsData != null && settingsData.sessions != null && settingsData.sessions.Any())
            {

                var mostRecentSession = settingsData.sessions
                                       .OrderByDescending(session => session.GetSaveTime())
                                       .FirstOrDefault();
                if (mostRecentSession != null)
                {
                    targetObject = mostRecentSession.targetObject;
                    targetObjects = mostRecentSession.targetObjects;
                    if (IsValidAssetTarget())
                    {
                        assetsCollapsed = mostRecentSession.assetsCollapsed;
                        userAssetViewSize = mostRecentSession.userAssetViewSize;
                        alreadyCalculatedHeight = false;
                    }
                    if (AssetTargetMode() == 1)
                    {
                        SetTargetAsset(targetObject, true, true);
                    }
                    else if (AssetTargetMode() == 2)
                    {
                        SetTargetAssets(targetObjects, true, true);
                    }
                    lockedAsset = mostRecentSession.lockedAsset;
                    assetOnlyMode = mostRecentSession.maximizedAssetView;
                    showImportSettings = mostRecentSession.showImportSettings;
                    if (mostRecentSession.historyAssets != null)
                    {
                        historyAssets = mostRecentSession.historyAssets
                                      .Where(item => item != null)
                                      .Select(item => item.Clone())
                                      .ToList();
                    }
                }
            }
        }
        void RestoreSession(bool light = false)
        {
            if (light)
            {
                RepopulateAllTabHistories();
            }
            if (lastSessionData != null && lastSessionData.tabs != null)
            {
                tabs = EditorUtils.CloneTabList(lastSessionData.tabs);
                if (tabs.Count == 0)
                {
                    tabs.Add(new TabInfo(null, 0, this));
                    DrawCurrentComponentsContainer();
                }
                scrollPosition = lastSessionData.scrollPosition;
                activeIndex = lastSessionData.activeIndex;
                if (activeIndex > tabs.Count - 1)
                {
                    activeIndex = tabs.Count - 1;
                }
                if (activeIndex < 0)
                {
                    activeIndex = 0;
                }
                if (GetActiveTab() != null)
                {
                    GetActiveTab().scrollPosition = scrollPosition.y;
                }
                /*if (componentScrollView != null)
                {
                    SetScrollPosition(scrollPosition.y);
                }*/
                toolbarScrollPosition = lastSessionData.toolbarScrollPosition;
                lastValidTabWidth = lastSessionData.lastValidTabWidth;
                lastClickedTab = lastSessionData.lastClickedTab;
                closedTabs = EditorUtils.CloneTabList(lastSessionData.closedTabs);
                playModeTransforms = EditorUtils.CloneTransformList(lastSessionData.playModeTransforms);
                lastSessionData.playModeTransforms = null;
                tracker = new GameObjectTracker(lastSessionData.tracker);
                alreadyCalculatedHeight = false;
                if (GetActiveTab() != null && GetActiveTab().IsValidMultiTarget())
                {
                    SetTargetGameObjects(GetActiveTab().targets);
                }
                else if (GetActiveTab() != null && GetActiveTab().target != null)
                {
                    SetTargetGameObject(GetActiveTab().target);
                }
                userHeight = 0;
                lastKnownHeight = 0;
                rawUserHeight = 0;
                previewRect = new Rect(0, 0, 0, 0);
                lastSessionData = null;
            }
            UpdateAllWidths();
            activeScene = SceneInfo.FromActiveScene();
            //ReinitializeComponentEditors();
            UpdateTabBar();
            DoCullNow();
            FocusTab();
        }
        internal TabInfo GetActiveTab()
        {
            if (activeIndex != lastActiveIndex)
            {
                if (tabs != null && tabs.Count > 0 && activeIndex < tabs.Count && activeIndex >= 0)
                {
                    lastActiveIndex = activeIndex;
                    return tabs[activeIndex];
                }
                else
                {
                    lastActiveIndex = -1;
                    return null;
                }
            }
            return lastActiveIndex >= 0 ? tabs[lastActiveIndex] : null;
        }

        internal TabInfo GetLastTab()
        {
            if (tabs == null || tabs.Count == 0 || previousTab == null)
            {
                return null;
            }
            return previousTab;
        }

        internal bool WasLastTabNew()
        {
            TabInfo lastTab = GetLastTab();
            if (lastTab != null)
            {
                return lastTab.newTab;
            }
            return false;
        }
        bool IsActiveTabNew()
        {
            if (tabs != null && tabs.Count > 0 && activeIndex < tabs.Count)
            {
                return GetActiveTab().newTab;
            }
            return false;
        }
        internal bool IsActiveTabValidMulti()
        {
            if (tabs != null && tabs.Count > 0 && activeIndex < tabs.Count)
            {
                if (GetActiveTab().multiEditMode && GetActiveTab().targets != null && GetActiveTab().targets.Length > 0)
                {
                    return true;
                }
            }
            return false;
        }

        void AddToPlayModeSave(GameObject go, bool isAutoSave = false)
        {
            if (go == null)
            {
                return;
            }
            playModeTransforms ??= new List<SerializableTransform>();
            playModeTransforms.RemoveAll(item => item.owner == go.transform);
            SerializableTransform targetTransform = new SerializableTransform(go.transform, isAutoSave);
            playModeTransforms.Add(targetTransform);
            if (IsActiveTabValidMulti())
            {
                GetActiveTab().serializedTransforms.Add(targetTransform);
            }
        }
        void AddToPlayModeSave(GameObject[] gameObjects, bool isAutoSave = false)
        {
            if (gameObjects == null || gameObjects.Length == 0)
            {
                return;
            }
            GetActiveTab().serializedTransforms = new List<SerializableTransform>();
            foreach (GameObject go in gameObjects)
            {
                AddToPlayModeSave(go, isAutoSave);
            }
        }
        internal void DrawMultiTabTransformLogic(ComponentMap componentMap)
        {
            List<SerializableTransform> transforms = GetAllPlayModeSaves(GetActiveTab().targets);
            if (Application.isPlaying)
            {
                int saves = 0;
                if (transforms != null)
                {
                    saves = transforms.Count;
                }
                bool showSaved = transforms != null && saves > 0;
                bool hasChanged = false;
                if (showSaved)
                {
                    if (saves != GetActiveTab().targets.Length)
                    {
                        hasChanged = true;
                    }
                    else
                    {
                        if (!SameTransforms(transforms))
                        {

                            hasChanged = true;
                        }
                    }
                }
                int cursorButton = Event.current.button;
                bool middleClick = cursorButton == 2;
                bool isAutoSave = transforms != null && transforms.All(t => t.autoSave);
                if (GUILayout.Button(CustomGUIContents.SaveTransformContent(showSaved, hasChanged, isAutoSave), CustomGUIStyles.ButtonsUpRight, GUILayout.Width(19), GUILayout.Height(22)))
                {
                    if (cursorButton == 1)
                    {
                        DrawMultiTabTransformContextMenus(cursorButton, showSaved, isAutoSave);
                    }
                    else
                    {
                        if ((!showSaved || hasChanged) && !middleClick)

                        {
                            AddToPlayModeSave(GetActiveTab().targets);
                        }
                        else
                        {
                            RemoveFromPlayModeSave(GetActiveTab().targets);
                        }
                    }
                }
                Rect rect = GUILayoutUtility.GetLastRect();
                rect.x = 0;
                rect.width = 23;
                EditorUtils.DrawLineOverRect(rect, CustomColors.HardShadow, 0);
                if (componentMap.foldout)
                {
                    EditorUtils.DrawLineUnderRect(rect, CustomColors.GradientShadow, -1);
                }
            }
            else if (transforms != null && SerializableTransform.DoTheyMatch(transforms, GetActiveTab().targets))
            {
                Color color = GUI.backgroundColor;
                GUI.backgroundColor += CustomColors.DarkHistoryButton;
                GUILayout.Space(21);
                Rect rect = GUILayoutUtility.GetLastRect();
                rect.x = 0;
                rect.width = 21;
                rect.height = 22;
                rect.y = 1;
                if (UndoPending(transforms))
                {
                    if (GUI.Button(rect, CustomGUIContents.UndoSaveContent, CustomGUIStyles.TransformHistoryButton))
                    {
                        if (Event.current.button == 0)
                        {
                            List<SerializableTransform> newList = new List<SerializableTransform>(transforms);
                            foreach (var savedTransform in newList)
                            {
                                if (savedTransform.appliedTransform)
                                {
                                    savedTransform.Apply();
                                }
                            }
                        }
                        else if (Event.current.button == 1)
                        {
                            ShowSerializableTransformMenu(transforms);
                        }
                    }
                }
                else if (GUI.Button(rect, CustomGUIContents.RedoSaveContent, CustomGUIStyles.TransformHistoryButton))
                {
                    if (Event.current.button == 0)
                    {
                        foreach (var savedTransform in transforms)
                        {
                            if (!savedTransform.appliedTransform)
                            {
                                savedTransform.Apply();
                            }
                        }
                    }
                    else if (Event.current.button == 1)
                    {
                        ShowSerializableTransformMenu(transforms, false);
                    }
                }
                EditorUtils.DrawLineOverRect(rect, CustomColors.HardShadow, 1);
                EditorUtils.DrawLineOverRect(rect);
                rect.width = 20;
                GUI.backgroundColor = color;

                EditorUtils.DrawFadeToBottom(rect, CustomColors.SoftBright, 4);
                EditorUtils.DrawLineUnderRect(rect, CustomColors.CustomFadeBlue, -2, 2);
            }
        }

        void DrawMultiTabTransformContextMenus(int cursorButton, bool showSaved, bool isAutoSave)
        {
            if (cursorButton == 1)
            {
                GenericMenu menu = new GenericMenu();
                string label = "Save current";
                if (showSaved)
                {
                    label = "Update Saved";
                }
                if (!isAutoSave)
                {
                    menu.AddItem(new GUIContent(label + "  Transforms"), false, () =>
                    {
                        AddToPlayModeSave(GetActiveTab().targets, false);
                    });
                    menu.AddItem(new GUIContent("Auto Save All on Exit Play Mode"), false, () =>
                    {
                        AddToPlayModeSave(GetActiveTab().targets, true);
                    });
                    if (showSaved)
                    {
                        menu.AddItem(new GUIContent("Don't Save Transforms"), false, () =>
                        {
                            RemoveFromPlayModeSave(GetActiveTab().targets);
                        });
                    }
                }
                else
                {
                    menu.AddItem(new GUIContent("Save Current Transforms"), false, () =>
                    {
                        AddToPlayModeSave(GetActiveTab().targets, false);
                    });
                    menu.AddItem(new GUIContent("Don't Save Transforms"), false, () =>
                    {
                        RemoveFromPlayModeSave(GetActiveTab().targets);
                    });
                }
                menu.ShowAsContext();
            }
        }


        void UpdateAllAutoSaveTransforms()
        {
            if (playModeTransforms == null || playModeTransforms.Count == 0)
            {
                return;
            }
            foreach (var transform in playModeTransforms)
            {
                if (transform.autoSave)
                {
                    transform.UpdateValues();
                    transform.autoSave = false;
                }
            }
        }

        bool UndoPending(List<SerializableTransform> serializableTransforms)
        {
            if (serializableTransforms == null || serializableTransforms.Count == 0)
            {
                return false;
            }
            foreach (var transform in serializableTransforms)
            {
                if (transform.appliedTransform)
                {
                    return true;
                }
            }
            return false;
        }
        void WipePlayModeSave()
        {
            if (playModeTransforms != null)
            {
                playModeTransforms.Clear();
            }
            if (lastSessionData != null && lastSessionData.playModeTransforms != null)
            {
                lastSessionData.playModeTransforms.Clear();
            }
        }
        bool IsGameObjectInPlayModeSave(GameObject go)
        {
            if (go == null || playModeTransforms == null)
            {
                return false;
            }
            return playModeTransforms.Any(item => item.owner == go.transform);
        }
        bool AreAllGameObjectsInPlayModeSave(GameObject[] gameObjects)
        {
            if (gameObjects == null || gameObjects.Length == 0 || playModeTransforms == null)
            {
                return false;
            }
            return gameObjects.All(go => playModeTransforms.Any(item => item.owner == go.transform));
        }

        SerializableTransform GetPlayModeSave(GameObject go)
        {
            if (go == null || playModeTransforms == null || playModeTransforms.Count == 0)
            {
                return null;
            }

            SerializableTransform targetTransform = playModeTransforms.FirstOrDefault(item => item.owner == go.transform);
            if (targetTransform != null)
            {
                return targetTransform;
            }
            return null;
        }

        bool SameTransforms(List<SerializableTransform> serializableTransforms)
        {
            if (serializableTransforms == null || serializableTransforms.Count == 0)
            {
                return false;
            }
            foreach (var transform in serializableTransforms)
            {
                SerializableTransform currentTransform = new SerializableTransform(transform.owner);
                if (!SerializableTransform.SameTransforms(currentTransform, transform))
                {
                    return false;
                }
            }
            return true;

        }

        List<SerializableTransform> GetAllPlayModeSaves(GameObject[] gameObjects)
        {
            if (gameObjects == null || gameObjects.Length == 0 || playModeTransforms == null || playModeTransforms.Count == 0)
            {
                return null;
            }
            if (GetActiveTab().serializedTransforms != null && GetActiveTab().serializedTransforms.Count == gameObjects.Length)
            {
                return GetActiveTab().serializedTransforms;
            }
            List<SerializableTransform> targetTransforms = new List<SerializableTransform>();
            foreach (var go in gameObjects)
            {
                SerializableTransform targetTransform = playModeTransforms.FirstOrDefault(item => item.owner == go.transform);
                if (targetTransform != null)
                {
                    targetTransforms.Add(targetTransform);
                }
            }
            GetActiveTab().serializedTransforms = targetTransforms;
            return targetTransforms;
        }
        void RemoveFromPlayModeSave(GameObject go)
        {
            if (go == null || playModeTransforms == null)
            {
                return;
            }
            playModeTransforms.RemoveAll(item => item.owner == go.transform);
        }
        void RemoveFromPlayModeSave(SerializableTransform transform)
        {
            if (transform == null || playModeTransforms == null)
            {
                return;
            }
            playModeTransforms.Remove(transform);
        }
        void RemoveFromPlayModeSave(GameObject[] gameObjects)
        {
            if (gameObjects == null || gameObjects.Length == 0 || playModeTransforms == null)
            {
                return;
            }

            foreach (GameObject go in gameObjects)
            {
                if (go != null && playModeTransforms != null)
                {
                    playModeTransforms.RemoveAll(item => item.owner == go.transform);
                }
            }
        }
        void RemoveFromPlayModeSave(List<SerializableTransform> transforms)
        {
            if (transforms == null || transforms.Count == 0 || playModeTransforms == null)
            {
                return;
            }
            foreach (var transform in transforms)
            {
                if (transform != null && playModeTransforms != null)
                {
                    playModeTransforms.Remove(transform);
                }
            }
        }

        internal bool IsActiveTabValid()
        {
            if (tabs != null && tabs.Count > 0 && activeIndex < tabs.Count)
            {
                TabInfo tab = GetActiveTab();
                if (tab.newTab)
                {
                    return true;
                }
                if (tab != null && ((!tab.multiEditMode && tab.target != null) || (tab.multiEditMode && tab.targets != null && tab.targets.Length > 0)))
                {
                    return true;
                }
            }
            return false;
        }
        public bool IsActiveTabLocked(bool ignoreNewOnLock = true)
        {
            if (tabs != null && tabs.Count > 0 && activeIndex < tabs.Count)
            {
                if (ignoreNewOnLock)
                {
                    return GetActiveTab().locked;
                }
                return GetActiveTab().locked && !newTabIfLocked;
            }
            return false;
        }

        private void OnDestroy()
        {
            if (!settingsData)
            {
                settingsData = AutoCreateSettings();
            }
            UpdateAllTabPaths();
            CleanAllEditors();
            justOpened = false;
            instances.Count();
            instances.Remove(this);
        }
        internal static bool IsNamespacePresent(string namespaceName)
        {
            var assemblies = AppDomain.CurrentDomain.GetAssemblies();
            foreach (var assembly in assemblies)
            {
                var types = assembly.GetTypes();
                if (types.Any(type => type.Namespace != null && type.Namespace.Contains(namespaceName)))
                {
                    return true;
                }
            }
            return false;
        }

        internal static bool IsOdinInspectorPresent()
        {
            return IsNamespacePresent("Sirenix.OdinInspector");
        }
        internal static bool IsTutorialFrameworkPresent()
        {
            return IsNamespacePresent("Unity.Tutorials.Core");
        }
        internal static bool IsAnyNamespacePresent(List<string> namespaceNames)
        {
            foreach (var name in namespaceNames)
            {
                if (IsNamespacePresent(name))
                {
                    return true;
                }
            }
            return false;
        }
    }

}