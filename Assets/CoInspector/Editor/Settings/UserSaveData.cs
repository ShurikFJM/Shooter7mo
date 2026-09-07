using UnityEngine;
using System.Collections.Generic;
using UnityEditor;
using UnityObject = UnityEngine.Object;
using System;
using System.Linq;
using UnityEngine.SceneManagement;

namespace CoInspector
{
    [Serializable]
    internal class UserSaveData : ScriptableObject
    {
        public bool hideUpdateAlert = false;
        public bool recycleUnlockedTabs = false;
        public int generalScrollSensitivity = 1;
        [HideInInspector]
        public string[] userInstalls = new string[0];
        public bool tabPreviewExpanded = true;
        public bool softWheelScrolling = true;
        public int middleScrollMode = 1;
        public bool newTabIfLocked = false;
        internal static SettingsWindow settingsWindow;
        public string rootPath = "Assets/CoInspector";
        public bool showHistory = true;
        public bool showTabName = true;
        public bool showTabTree = true;
        public bool showFilterBar = true;
        public bool hideEmptyComponents = true;
        [HideInInspector] public bool showInstallMessage = true;
        [HideInInspector] public bool showSceneToolsMessage = true;
        public bool softSelection = true;
        public bool showCollapseTool = true;
        public bool showHierarchyButton = true;
        public bool showInspectorButton = true;
        public bool showFocusButton = true;
        public bool showSelectButton = true;
        public bool showMaximizeButton = false;
        public bool assetPreviewExpanded = true;
        public bool showLastClicked = true;
        public bool showMostClicked = true;
        public bool showIcons = true;
        public bool richNames = true;
        public bool showScrollBar = true;
        public bool showAdditionalOptions = true;
        [HideInInspector] public bool rememberSessions = true;
        [HideInInspector] public bool useThumbKeys = true;
        public bool ignoreFolders = true;
        public bool collapsePrefabComponents = true;
        [HideInInspector] public bool openPrefabsInNewTab = true;
        [HideInInspector] public bool showTextAssetPreviews = true;
        public bool showAssetLabels = false;
        public bool autoFocus = false;
        public bool assetInspection = true;
        public bool componentCulling = true;
        public bool reuseTabs = false;
        public bool overrideSceneTools = false;
        public int sessionsMode = 0;
        public int tabCompactMode = 1;
        public int doubleClickMode = 1;
        public int scrollSpeedX = 2;
        public int scrollSpeedY = 2;
        public int scrollDirectionX = 1;
        public int scrollDirectionY = 1;
        public int generalScrollSpeed = 18;
        [HideInInspector]
        [SerializeField] internal List<TabSession> sessions;
        [HideInInspector]
        [SerializeField] public int[] tipsShown;
        [HideInInspector]
        [SerializeField] internal Scene playModeStartScene;
        [HideInInspector][SerializeField] internal bool enteringPlayMode = false;


        public UserSaveData()
        {
            sessions = new List<TabSession>();
        }

        public void SaveData(CoInspectorWindow reference)
        {
            SaveData(true, reference);
        }

        internal static void RefreshSettingsWindow()
        {
            if (settingsWindow != null)
            {
                settingsWindow.Repaint();
            }
        }

        public string GetRootPath()
        {
            rootPath = AssetDatabase.GetAssetPath(this);
            rootPath = rootPath.Substring(0, rootPath.LastIndexOf("CoInspector") + 11);
            return rootPath;
        }

        public void SaveData(bool saveToDisk = false, CoInspectorWindow reference = null, bool skipCheck = false)
        {
            hideUpdateAlert = CoInspectorWindow.hideUpdateAlert;
            recycleUnlockedTabs = CoInspectorWindow.recycleUnlockedTabs;
            generalScrollSensitivity = CoInspectorWindow.mouseWheelSpeed;
            userInstalls = CoInspectorWindow.userInstalls;
            tabPreviewExpanded = CoInspectorWindow.tabPreviewExpanded;
            softWheelScrolling = CoInspectorWindow.softWheelScrolling;
            middleScrollMode = CoInspectorWindow.middleScrollMode;
            newTabIfLocked = CoInspectorWindow.newTabIfLocked;
            showHistory = CoInspectorWindow.showHistory;
            showTabName = CoInspectorWindow.showTabName;
            showTabTree = CoInspectorWindow.showTabTree;
            showFilterBar = CoInspectorWindow.showFilterBar;
            hideEmptyComponents = CoInspectorWindow.hideEmptyComponents;
            showInstallMessage = CoInspectorWindow.showInstallMessage;
            showSceneToolsMessage = CoInspectorWindow.showSceneToolsMessage;
            softSelection = CoInspectorWindow.softSelection;
            showIcons = CoInspectorWindow.showIcons;
            assetInspection = CoInspectorWindow.assetInspection;
            componentCulling = CoInspectorWindow.componentCulling;
            reuseTabs = CoInspectorWindow.reuseTabs;
            showMaximizeButton = CoInspectorWindow.showMaximizeButton;
            assetPreviewExpanded = CoInspectorWindow.assetPreviewExpanded;
            overrideSceneTools = CoInspectorWindow.overrideSceneTools;
            richNames = CoInspectorWindow.richNames;
            showScrollBar = CoInspectorWindow.showScrollBar;
            rememberSessions = CoInspectorWindow.rememberSessions;
            showCollapseTool = CoInspectorWindow.showCollapseTool;
            showHierarchyButton = CoInspectorWindow.showHierarchyButton;
            showInspectorButton = CoInspectorWindow.showInspectorButton;
            showFocusButton = CoInspectorWindow.showFocusButton;
            showSelectButton = CoInspectorWindow.showSelectButton;
            showMostClicked = CoInspectorWindow.showMostClicked;
            showLastClicked = CoInspectorWindow.showLastClicked;
            autoFocus = CoInspectorWindow.autoFocus;
            useThumbKeys = CoInspectorWindow.useThumbKeys;
            ignoreFolders = CoInspectorWindow.ignoreFolders;
            collapsePrefabComponents = CoInspectorWindow.collapsePrefabComponents;
            openPrefabsInNewTab = CoInspectorWindow.openPrefabsInNewTab;
            showAdditionalOptions = CoInspectorWindow.showAdditionalOptions;
            showTextAssetPreviews = CoInspectorWindow.showTextAssetPreviews;
            showAssetLabels = CoInspectorWindow.showAssetLabels;
            sessionsMode = CoInspectorWindow.sessionsMode;
            tabCompactMode = CoInspectorWindow.tabCompactMode;
            doubleClickMode = CoInspectorWindow.doubleClickMode;
            scrollSpeedX = CoInspectorWindow.scrollSpeedX;
            scrollSpeedY = CoInspectorWindow.scrollSpeedY;
            scrollDirectionX = CoInspectorWindow.scrollDirectionX;
            scrollDirectionY = CoInspectorWindow.scrollDirectionY;
            generalScrollSpeed = CoInspectorWindow.mouseWheelSensitivity;
            if (reference != null)
            {
                if (!skipCheck && reference.exitingPlayMode)
                {
                    return;
                }
                if (reference.inActualPlayMode)
                {
                    playModeStartScene = SceneManager.GetActiveScene();
                    reference.enteringPlayMode = false;
                }
                else
                {
                    playModeStartScene = default(Scene);
                }

                if (!reference.InRecoverScreen())
                {
                    SaveSession(reference);
                }
            }
            EditorUtility.SetDirty(this);
            if (saveToDisk)
            {
                AssetDatabase.SaveAssetIfDirty(this);
            }
            RefreshSettingsWindow();
        }

        internal TabSession LoadData(CoInspectorWindow reference = null, bool rebuild = true)
        {
            CoInspectorWindow.hideUpdateAlert = hideUpdateAlert;
            CoInspectorWindow.recycleUnlockedTabs = recycleUnlockedTabs;
            CoInspectorWindow.mouseWheelSpeed = generalScrollSensitivity;
            CoInspectorWindow.userInstalls = userInstalls;
            CoInspectorWindow.tabPreviewExpanded = tabPreviewExpanded;
            CoInspectorWindow.softWheelScrolling = softWheelScrolling;
            CoInspectorWindow.middleScrollMode = middleScrollMode;
            CoInspectorWindow.newTabIfLocked = newTabIfLocked;
            TabSession loadedSession = null;
            CoInspectorWindow.showHistory = showHistory;
            CoInspectorWindow.showTabName = showTabName;
            CoInspectorWindow.showTabTree = showTabTree;
            CoInspectorWindow.showFilterBar = showFilterBar;
            CoInspectorWindow.hideEmptyComponents = hideEmptyComponents;
            CoInspectorWindow.showInstallMessage = showInstallMessage;
            CoInspectorWindow.showSceneToolsMessage = showSceneToolsMessage;
            CoInspectorWindow.softSelection = softSelection;
            CoInspectorWindow.showIcons = showIcons;
            CoInspectorWindow.autoFocus = autoFocus;
            CoInspectorWindow.assetInspection = assetInspection;
            CoInspectorWindow.componentCulling = componentCulling;
            CoInspectorWindow.reuseTabs = reuseTabs;
            CoInspectorWindow.showMaximizeButton = showMaximizeButton;
            CoInspectorWindow.assetPreviewExpanded = assetPreviewExpanded;
            CoInspectorWindow.overrideSceneTools = overrideSceneTools;
            CoInspectorWindow.richNames = richNames;
            CoInspectorWindow.showScrollBar = showScrollBar;
            CoInspectorWindow.showCollapseTool = showCollapseTool;
            CoInspectorWindow.showHierarchyButton = showHierarchyButton;
            CoInspectorWindow.showInspectorButton = showInspectorButton;
            CoInspectorWindow.showFocusButton = showFocusButton;
            CoInspectorWindow.showSelectButton = showSelectButton;
            CoInspectorWindow.showMostClicked = showMostClicked;
            CoInspectorWindow.showLastClicked = showLastClicked;
            CoInspectorWindow.rememberSessions = rememberSessions;
            CoInspectorWindow.showAdditionalOptions = showAdditionalOptions;
            CoInspectorWindow.useThumbKeys = useThumbKeys;
            CoInspectorWindow.ignoreFolders = ignoreFolders;
            CoInspectorWindow.collapsePrefabComponents = collapsePrefabComponents;
            CoInspectorWindow.openPrefabsInNewTab = openPrefabsInNewTab;
            CoInspectorWindow.showTextAssetPreviews = showTextAssetPreviews;
            CoInspectorWindow.showAssetLabels = showAssetLabels;
            CoInspectorWindow.sessionsMode = sessionsMode;
            CoInspectorWindow.tabCompactMode = tabCompactMode;
            CoInspectorWindow.doubleClickMode = doubleClickMode;
            CoInspectorWindow.scrollSpeedX = scrollSpeedX;
            CoInspectorWindow.scrollSpeedY = scrollSpeedY;
            CoInspectorWindow.scrollDirectionX = scrollDirectionX;
            CoInspectorWindow.scrollDirectionY = scrollDirectionY;
            CoInspectorWindow.mouseWheelSensitivity = generalScrollSpeed;
            if (reference != null)
            {
                loadedSession = LoadValidSession(reference, rebuild);
            }
            /*if (loadedSession != null)
            {
                Debug.Log("Session data Loaded");                
            }*/
            return loadedSession;
        }

        internal void SaveSession(CoInspectorWindow reference, SceneInfo overrideSceneInfo = null)
        {
            if (reference.tabs == null || reference.tabs.Count == 0)
            {
                return;
            }

            bool validTab = reference.tabs.Any(tab => tab != null);

            if (sessions == null)
            {
                sessions = new List<TabSession>();
            }
            bool overrideScene = overrideSceneInfo != null;

            SceneInfo sceneInfo = overrideSceneInfo ?? SceneInfo.FromActiveScene();

            if (string.IsNullOrEmpty(sceneInfo.SceneGUID))
            {
                //Debug.LogWarning("Invalid scene GUID. Cannot save session.");
                return;
            }

            int sessionIndex = sessions.FindIndex(session => session.sceneGUID == sceneInfo.SceneGUID);
            if (sessionIndex != -1)
            {
                if (sessions[sessionIndex].GetSaveTime() != default(DateTime) && DateTime.Now - sessions[sessionIndex].GetSaveTime() < TimeSpan.FromSeconds(0.5))
                {
                    //Debug.Log($"Session data is too recent. Not saving to avoid loops. Scene: {sceneInfo.SceneName} ({sceneInfo.SceneGUID})");
                    return;
                }
                if (!validTab)
                {
                    //Debug.Log($"No valid tabs found. Just saving assets and settings. Scene: {sceneInfo.SceneName} ({sceneInfo.SceneGUID})");
                    sessions[sessionIndex].SaveAssets(reference);
                }
                else
                {
                    sessions[sessionIndex] = new TabSession(reference, sceneInfo, overrideScene)
                    {
                        sceneGUID = sceneInfo.SceneGUID
                    };
                    // Debug.Log($"Session Saved. Saved {sessions[sessionIndex].tabs.Count} tabs. Scene: {sceneInfo.SceneName} ({sceneInfo.SceneGUID})");
                }
            }
            else
            {
                var newSession = new TabSession(reference, sceneInfo, overrideScene)
                {
                    sceneGUID = sceneInfo.SceneGUID
                };
                sessions.Add(newSession);
                // Debug.Log($"Session Saved. Saved {newSession.tabs.Count} tabs. Scene: {sceneInfo.SceneName} ({sceneInfo.SceneGUID})");
            }
            HandleDeletedScenes();
        }

        TabSession LoadValidSession(CoInspectorWindow reference, bool rebuild = true)
        {
            if (sessions == null || sessions.Count == 0)
            {
                return null;
            }
            var activeScene = SceneManager.GetActiveScene();
            if (string.IsNullOrEmpty(activeScene.path))
            {
                return null;
            }
            string sceneGUID = AssetDatabase.AssetPathToGUID(activeScene.path);
            var session = sessions.Find(s => s.sceneGUID == sceneGUID);
            if (session == null)
            {
                return null;
            }
            if (session.tabs != null && session.tabs.Count == 0)
            {
                session.tabs = null;
            }
            /*
            if (session.tabs != null)
            {
                Debug.Log("Tabs found before rebuild: " + session.tabs.Count);
            }*/

            if (session.closedTabs != null && session.closedTabs.Count == 0)
            {
                session.closedTabs = null;
            }
            if (session.targetObjects != null && session.targetObjects.Length == 0)
            {
                session.targetObjects = null;
            }
            if (session.tabs != null)
            {
                session.tabs = EditorUtils.RebuildTabs(session.tabs, reference, rebuild);
                //Debug.Log("Tabs after rebuild: " + session.tabs.Count);
            }
            else
            {
                session.tabs = new List<TabInfo>();
            }
            if (session.closedTabs != null)
            {
                session.closedTabs = EditorUtils.RebuildTabs(session.closedTabs, reference, rebuild);
            }
            if (session.activeIndex > session.tabs.Count - 1)
            {
                session.activeIndex = session.tabs.Count - 1;
            }
            if (session.activeIndex < 0)
            {
                session.activeIndex = 0;
            }
            //   Debug.Log("Session data loaded. Found " + session.tabs.Count + " tabs. Active index: " + session.activeIndex + ". Scene: " + activeScene.name + " (" + sceneGUID + ")");
            return session;
        }

        void HandleDeletedScenes()
        {
            if (sessions == null) return;

            foreach (var session in sessions.ToList())
            {
                if (!EditorUtils.SceneExists(session.sceneGUID))
                {
                    session.gracePeriod -= 1;
                }
                if (session.gracePeriod <= 0)
                {
                    sessions.Remove(session);
                }
            }
        }
    }

    [Serializable]
    internal class TabSession
    {
        [SerializeField] internal GameObjectTracker tracker;
        [SerializeField] internal string sceneGUID;
        [SerializeField] internal string sceneName;
        [SerializeField] internal string lastSaveTimePrint = " - ";
        [SerializeField] internal int gracePeriod = 3;
        [SerializeField] internal int activeIndex;
        [SerializeField] internal bool checkingToLoad;
        [SerializeField] internal bool assetsCollapsed;
        [SerializeField] internal bool lockedAsset;
        [SerializeField] internal bool maximizedAssetView;
        [SerializeField] internal bool showImportSettings;
        [SerializeField] internal float suggestedHeight = 0;
        [SerializeField] internal float userAssetViewSize = 0;
        [SerializeField] internal float previewHeight = 0;
        [SerializeField] internal Vector2 scrollPosition;
        [SerializeField] internal Vector2 toolbarScrollPosition;
        [SerializeField] internal float rawUserHeight = 0;
        [SerializeField] internal float lastValidTabWidth = 0;
        [SerializeField] internal bool alreadyCalculatedHeight = false;
        [SerializeField] internal float lastKnownHeight = 0;
        [SerializeField] private DateTime lastSaveTime;
        [SerializeField] internal List<TabInfo> tabs;
        [SerializeField] internal List<TabInfo> closedTabs;
        [SerializeField] internal List<SerializableTransform> playModeTransforms;
        [SerializeField] internal List<HistoryAssets> historyAssets;
        [SerializeField] internal UnityObject targetObject;
        [SerializeField] internal UnityObject[] targetObjects;
        [SerializeField] internal string lastSelectedAsset;
        [SerializeField] internal int lastClickedTab = -1;
        [SerializeField] internal int previousTab = 0;


        void UpdateAllTabPaths(List<TabInfo> _tabs)
        {
            if (_tabs == null)
            {
                return;
            }
            foreach (var tab in _tabs)
            {
                if (tab == null)
                {
                    continue;
                }
                tab.UpdatePath();
            }
        }
        public DateTime GetSaveTime()
        {
            if (lastSaveTimePrint == " - ")
            {
                return default(DateTime);
            }
            if (lastSaveTime != default(DateTime))
            {
                return lastSaveTime;
            }
            try
            {
                lastSaveTime = DateTime.ParseExact(
                    lastSaveTimePrint,
                    "hh:mm tt (MMMM d, yyyy)",
                    new System.Globalization.CultureInfo("en-US"));
            }
            catch (FormatException)
            {
                lastSaveTime = DateTime.Now;
                Debug.LogWarning($"Incompatible Date Format found in Session: '{lastSaveTimePrint}'. Replacing with current date and time.");
            }
            return lastSaveTime;
        }

        internal void SaveAssets(CoInspectorWindow reference)
        {
            if (reference == null)
            {
                return;
            }
            targetObject = reference.targetObject;
            lastSelectedAsset = "";
            if (targetObject != null)
            {
                lastSelectedAsset = AssetDatabase.GetAssetPath(targetObject);
            }
            targetObjects = reference.targetObjects;
            if (reference.historyAssets != null)
            {
                historyAssets = reference.historyAssets
                              .Where(item => item != null)
                              .Select(item => item.Clone())
                              .ToList();

            }
            if (historyAssets == null)
            {
                historyAssets = new List<HistoryAssets>();
            }
            assetsCollapsed = reference.assetsCollapsed;
            lockedAsset = reference.lockedAsset;
            maximizedAssetView = reference.assetOnlyMode;
            showImportSettings = reference.showImportSettings;
            checkingToLoad = false;
            suggestedHeight = reference.suggestedHeight;
            userAssetViewSize = reference.userAssetViewSize;
            previewHeight = reference.previewRect.height;
            rawUserHeight = reference.rawUserHeight;
            alreadyCalculatedHeight = reference.alreadyCalculatedHeight;
            lastKnownHeight = reference.lastKnownHeight;
            if (DateTime.Now != default)
            {
                lastSaveTime = DateTime.Now;
                lastSaveTimePrint = lastSaveTime.ToString("hh:mm tt (MMMM d, yyyy)", new System.Globalization.CultureInfo("en-US"));
            }
        }


        public TabSession(CoInspectorWindow reference, SceneInfo sceneInfo, bool overrideScene = false)
        {
            if (reference == null || string.IsNullOrEmpty(sceneInfo.SceneGUID))
            {
                return;
            }
            if (!overrideScene)
            {
                reference.UpdateAllTabPaths();
            }
            sceneName = sceneInfo.SceneName;
            SaveAssets(reference);
            activeIndex = reference.activeIndex;
            lastValidTabWidth = reference.lastValidTabWidth;
            tracker = new GameObjectTracker(reference.tracker);
            scrollPosition = reference.scrollPosition;
            if (reference.componentScrollView != null)
            {
                scrollPosition = reference.componentScrollView.scrollOffset;
            }
            lastClickedTab = reference.lastClickedTab;
            if (reference.previousTab != null)
            {
                previousTab = reference.previousTab.index;
            }
            toolbarScrollPosition = reference.toolbarScrollPosition;
            tabs = EditorUtils.CloneTabList(reference.tabs);
            closedTabs = EditorUtils.CloneTabList(reference.closedTabs);
            playModeTransforms = EditorUtils.CloneTransformList(reference.playModeTransforms);
        }
    }
    [Serializable]
    internal class HistoryAssets
    {
        [SerializeField] internal string[] assetGUIDs;

        public HistoryAssets(string[] newGUIDs)
        {
            if (newGUIDs == null)
            {
                return;
            }
            assetGUIDs = new string[newGUIDs.Length];
            Array.Copy(newGUIDs, assetGUIDs, newGUIDs.Length);
        }

        public HistoryAssets(UnityEngine.Object[] assets)
        {
            if (assets == null)
            {
                return;
            }
            assetGUIDs = assets.Where(asset => asset != null)
                               .Select(asset => AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(asset)))
                               .Where(guid => !string.IsNullOrEmpty(guid))
                               .ToArray();
        }
        public HistoryAssets Clone()
        {
            return new HistoryAssets(this.assetGUIDs);
        }
    }
    [Serializable]
    internal class SceneInfo
    {
        [SerializeField] private string sceneName;
        [SerializeField] private string scenePath;
        [SerializeField] private string sceneGUID;

        internal string SceneName => sceneName;
        internal string ScenePath => scenePath;
        internal string SceneGUID => sceneGUID;

        internal SceneInfo(string sceneName, string sceneGUID, string scenePath)
        {
            this.sceneName = sceneName;
            this.scenePath = scenePath;
            this.sceneGUID = sceneGUID;
        }

        internal bool IsValid()
        {
            return !string.IsNullOrEmpty(sceneName) && !string.IsNullOrEmpty(sceneGUID) && !string.IsNullOrEmpty(scenePath);
        }

        internal static SceneInfo FromActiveScene(SceneInfo previousValidScene = null)
        {
            Scene activeScene = SceneManager.GetActiveScene();

            if (!IsSceneValid(activeScene))
            {
                return previousValidScene ?? CreateInvalidSceneInfo();
            }

            string sceneGUID = AssetDatabase.AssetPathToGUID(activeScene.path);
            SceneInfo newSceneInfo = new SceneInfo(activeScene.name, sceneGUID, activeScene.path);

            return newSceneInfo.IsValid() ? newSceneInfo : (previousValidScene ?? CreateInvalidSceneInfo());
        }

        private static bool IsSceneValid(Scene scene)
        {
            return scene.IsValid() && !string.IsNullOrEmpty(scene.path);
        }

        private static SceneInfo CreateInvalidSceneInfo()
        {
            return new SceneInfo("Invalid Scene", string.Empty, string.Empty);
        }
    }
}
