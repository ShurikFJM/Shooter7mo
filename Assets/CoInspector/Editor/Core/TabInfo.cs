using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
#if UNITY_2021_2_OR_NEWER
#else
using UnityEditor.Experimental.SceneManagement;
#endif
using UnityEngine;
using UnityEngine.SceneManagement;
namespace CoInspector
{
    [Serializable]
    internal class TabInfo
    {
        public string path = "";
        public string[] paths;
        public float tabWidth = 0;
#if UNITY_6000_4_OR_NEWER
        [NonSerialized] public EntityId id = EntityId.None;
        [NonSerialized] public EntityId[] ids;
#else
        [NonSerialized] public int id = 0;
        [NonSerialized] public int[] ids;
#endif
        public string name;
        public Texture2D icon;
        public string shortName;
        public string filterString = "";
        public bool newTab = false;
        internal bool markForDeletion = false;
        internal bool willBeDeleted = false;
        public float scrollPosition;
        public bool isSelected = false;
        public bool allCollapsed = false;
        public bool[] runtimeFoldouts;
        public Component[] runtimeMultiComponents;
        public GUIContent tabTextContent;
        public bool multiEditMode = false;
        public bool locked;
        public bool hasPreview = false;
        public float previewHeight = 150;
        public bool debug;
        public bool filtering = false;
        public bool prefab = false;
        public bool multiFoldout = true;
        public GameObject target;
        public GameObject[] targets;
        public GameObject[] linkedPrefabs = null;

        public GameObject trackMultiTarget;
        public SerializableTransform serializedTransform;
        public List<SerializableTransform> serializedTransforms;
        public bool savePlayModeTransform = false;
        public int index;
        public int historyPosition = 0;
        public List<GameObject[]> history = new List<GameObject[]>();
        public List<ComponentMap> componentMaps;
        internal List<RemovedComponent> removedComponents;
        private MaterialMapManager materialMapManager;
        public List<HistoryPaths> historyPaths;
        public CoInspectorWindow owner;
        internal bool zoomFocus = false;

        public TabInfo(GameObject target, int index, CoInspectorWindow owner, string _name = "")
        {
            this.owner = owner;
            this.index = index;
            this.filterString = "";
            this.newTab = true;
            this.name = "New Tab";
            if (_name == "" && target != null)
            {
                name = target.name;
            }
            else
            {
                name = _name;
            }
            this.target = target;
            this.newTab = !target;
            if (owner)
            {
                UpdateTabName();
                UpdatePath();
                UpdateTabWidth(true);
                owner.UpdateTabBar();
            }
        }
        public TabInfo(GameObject[] targets, int index, CoInspectorWindow owner)
        {
            this.owner = owner;
            this.index = index;
            this.newTab = true;
            this.name = "New Tab";
            this.filterString = "";
            if (targets != null && targets.Length > 0)
            {
                if (targets.Length == 1)
                {
                    GameObject gameObject = targets[0];
                    if (gameObject != null)
                    {
                        name = gameObject.name;
                    }
                    this.target = gameObject;
                    this.newTab = !gameObject;
                    return;
                }
                this.name = "(" + targets.Length + ") Objects";
                this.multiEditMode = true;
                this.targets = targets;
                this.newTab = false;
                if (owner)
                {
                    UpdateTabName();
                    UpdatePath();
                    UpdateTabWidth(true);
                    owner.UpdateTabBar();
                }
            }
        }

        public TabInfo(TabInfo reference)
        {
            this.path = reference.path;
            this.paths = reference.paths;
            this.name = reference.name;
            this.filterString = reference.filterString;
            this.icon = reference.icon;
            this.shortName = reference.shortName;
            this.newTab = reference.newTab;
            this.isSelected = reference.isSelected;
            this.runtimeFoldouts = reference.runtimeFoldouts;
            this.multiEditMode = reference.multiEditMode;
            this.locked = reference.locked;
            this.debug = reference.debug;
            this.filtering = reference.filtering;
            this.previewHeight = reference.previewHeight;
            this.hasPreview = reference.hasPreview;
            this.prefab = reference.prefab;
            this.target = reference.target;
            this.targets = reference.targets;
            this.index = reference.index;
            this.historyPosition = reference.historyPosition;
            this.history = reference.history;
            this.owner = reference.owner;
            this.id = reference.id;
            this.ids = reference.ids;
            this.historyPaths = reference.historyPaths;
            this.componentMaps = reference.componentMaps?.Select(cm => new ComponentMap(cm)).ToList();
            this.multiFoldout = reference.multiFoldout;
            this.scrollPosition = reference.scrollPosition;
            this.allCollapsed = reference.allCollapsed;
            this.tabWidth = reference.tabWidth;
            this.runtimeMultiComponents = reference.runtimeMultiComponents;
            this.tabTextContent = reference.tabTextContent;
            this.markForDeletion = false;
            this.willBeDeleted = false;
            this.savePlayModeTransform = reference.savePlayModeTransform;
            if (this.savePlayModeTransform && reference.serializedTransform != null)
            {
                this.serializedTransform = new SerializableTransform(reference.serializedTransform);
            }
        }
        internal void SetNotFiltering()
        {
            if (componentMaps == null)
            {
                foreach (var map in componentMaps)
                {
                    map.isFilteredOut = false;
                }
            }
        }

        internal GameObject MultiTrackingTarget()
        {
            if (trackMultiTarget == null)
            {
                trackMultiTarget = targets[0];//EditorUtils.GetLastCreatedGameObject(targets);
            }
            return trackMultiTarget;
        }


        public GUIContent TabTextContent
        {
            get
            {
                if (tabTextContent == null)
                {
                    tabTextContent = new GUIContent(shortName ?? string.Empty);
                }
                else
                {
                    tabTextContent.text = shortName ?? string.Empty;
                }
                return tabTextContent;
            }
        }

        internal void UpdateTabName()
        {
            if (owner == null)
            {
                return;
            }
            string _name = name;
            if (newTab)
            {
                _name = "New Tab";
            }
            _name = _name.TrimStart();
            _name = _name.TrimEnd();

            int charLimit = 15;
            if (CoInspectorWindow.tabCompactMode == 2)
            {
                charLimit = 18;
            }
            int tabCount = owner.tabs.Count;
            int tabLimit = 2;
            if (CoInspectorWindow.tabCompactMode == 2)
            {
                tabLimit = 3;
            }
            for (int j = 0; j < tabCount; j++)
            {
                if (j >= tabLimit)
                {
                    charLimit -= 2;
                }
                else
                {
                    charLimit -= 1;
                }
            }

            if (charLimit < 4)
            {
                charLimit = 4;
            }
            if (!newTab && _name.Length > charLimit && tabCount > 1 && _name.Length - charLimit > 1)
            {
                _name = _name.Substring(0, charLimit).TrimEnd() + "…";
            }

            if (shortName != _name || tabWidth == 0)
            {
                shortName = _name;
                UpdateTabWidth();
            }
        }

        public void UpdateTabWidth(bool force = false)
        {
            if (CustomGUIStyles.editorToolbarButton == null)
            {
                return;
            }
            if (TabTextContent != null)
            {
                if ((CoInspectorWindow.showIcons || locked) && !newTab && owner)
                {
                    CustomGUIStyles.ToolbarButtonTabs.padding = owner.PaddingIcon;
                }
                else if (owner)
                {
                    CustomGUIStyles.ToolbarButtonTabs.padding = owner.PaddingNoIcon;
                }
                int padding = 7;
                tabWidth = Mathf.RoundToInt(CustomGUIStyles.ToolbarButtonTabs.CalcSize(TabTextContent).x) + padding;
            }
        }

        public void AddToHistoryIfProceeds(GameObject[] newTargets)
        {
            zoomFocus = false;
            if (newTargets != null && newTargets.Length > 0)
            {

                if (newTargets != null && !EditorUtils.CompareArrays(newTargets, targets))
                {
                    AddToHistory(newTargets);
                }
                else if (newTargets != null && history == null || history.Count == 0)
                {
                    AddToHistory(newTargets);
                }
            }

        }

        public void AddToHistoryIfProceeds(GameObject newTarget)
        {
            zoomFocus = false;
            multiFoldout = true;
            if (newTarget != null)
            {

                if (newTarget != null && newTarget != target)
                {
                    AddToHistory(new GameObject[] { newTarget });

                }
                else if (newTarget != null && history == null || history.Count == 0)
                {
                    AddToHistory(new GameObject[] { newTarget });
                }
            }

        }


        public void ResetTab()
        {
            target = null;
            targets = null;
            locked = false;
            debug = false;
            multiEditMode = false;
            multiFoldout = true;
            newTab = true;
            prefab = false;
            filterString = "";
            filtering = false;
            previewHeight = 150;
            hasPreview = false;
            name = "New Tab";
            path = "";
            paths = null;
#if UNITY_6000_4_OR_NEWER
            id = EntityId.None;

#else
            id = 0;

#endif
            ids = null;
            history = new List<GameObject[]>();
            historyPosition = 0;
            componentMaps = new List<ComponentMap>();
            historyPaths = new List<HistoryPaths>();
            zoomFocus = false;
            runtimeMultiComponents = null;
            runtimeFoldouts = null;
            scrollPosition = 0;
            allCollapsed = false;
            DestroyAllMaterialMaps();
            if (CoInspectorWindow.MainCoInspector)
            {
                if (CoInspectorWindow.MainCoInspector.GetActiveTab() == this)
                {
                    CoInspectorWindow.MainCoInspector.targetGameObject = null;
                }
            }
        }
        internal bool IsPrefabInstance()
        {
            if (!prefab && icon)
            {
                return icon == CustomGUIContents.PrefabIcon.image;
            }
            return false;
        }

        internal void SetPrefabMode()
        {
            if (newTab)
            {
                prefab = false;
                return;
            }
            if (IsValidMultiTarget())
            {
                prefab = CoInspectorWindow.AreAllGameObjectsInPrefabMode(targets);
            }
            else if (target != null)
            {
                prefab = CoInspectorWindow.IsGameObjectInPrefabMode(target);
            }

        }


        public void LoadTargetFromPath()
        {
            if (path != "" && !multiEditMode)
            {
                target = GameObject.Find(path);
                if (target != null)
                {
                    newTab = false;
                }
            }
            else if (IsValidMultiTarget() && paths != null && paths.Length > 0)
            {
                List<GameObject> _targets = new List<GameObject>();
                foreach (var _path in paths)
                {
                    GameObject _target = GameObject.Find(_path);
                    if (_target != null)
                    {
                        newTab = false;
                        _targets.Add(_target);
                    }
                }
                targets = _targets.ToArray();
            }
        }
        internal bool IsTabValid(bool excludeNew = false)
        {
            if (excludeNew && newTab)
            {
                return false;
            }
            if (newTab)
            {
                return true;
            }
            if (IsValidMultiTarget())
            {
                return true;
            }
            if (target != null)
            {
                return true;
            }
            return false;
        }
        internal void AutoSortTargets()
        {
            if (target == null && (targets == null || targets.Length == 0))
            {
                multiEditMode = false;
                target = null;
                targets = null;
                return;
            }
            if (target != null && (targets == null || targets.Length == 0))
            {
                multiEditMode = false;
                targets = null;
                return;
            }
            if (targets != null && targets.Length > 0)
            {
                if (targets.Length > 1)
                {
                    multiEditMode = true;
                    target = null;
                    return;
                }
                if (targets.Length == 1)
                {
                    multiEditMode = false;
                    target = targets[0];
                    targets = null;
                    return;
                }
            }
            multiEditMode = false;
            targets = null;
        }

        public bool IsValidMultiTarget(bool _debug = false)
        {
            if (targets != null && targets.Length > 0 && multiEditMode)
            {
                foreach (var _target in targets)
                {
                    if (_target == null)
                    {
                        return false;
                    }
                }
                return true;
            }
            /*
            if (debug)
            {
                if (targets == null)
                {
                    Debug.Log("Targets null");
                }
                else if (targets.Length == 0)
                {
                    Debug.Log("Targets empty");
                }
                if (!multiEditMode)
                {
                    Debug.Log("Not multi edit mode");
                }
            }*/

            return false;
        }

        public bool HasNullMultiTargets()
        {
            return targets?.Any(t => t == null) ?? false;
        }

        internal void UpdateLinkedPrefabs()
        {
            linkedPrefabs = null;

            if (IsValidMultiTarget())
            {
                var newPrefabs = targets.Select(go =>
                {
                    if (go != null && PrefabUtility.IsOutermostPrefabInstanceRoot(go))
                    {
                        var prefabAsset = PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(go);
                        if (!string.IsNullOrEmpty(prefabAsset))
                        {
                            return AssetDatabase.LoadAssetAtPath<GameObject>(prefabAsset);
                        }
                    }
                    return null;
                }).ToArray();

                if (newPrefabs.Any(p => p != null))
                {
                    linkedPrefabs = newPrefabs;
                }
            }
            else if (target != null)
            {
                GameObject go = target as GameObject;
                if (go != null && PrefabUtility.IsOutermostPrefabInstanceRoot(go))
                {
                    var prefabAsset = PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(go);
                    if (!string.IsNullOrEmpty(prefabAsset))
                    {
                        linkedPrefabs = new GameObject[1] { AssetDatabase.LoadAssetAtPath<GameObject>(prefabAsset) };
                    }
                }
            }
            //DebugLinkedPrefabs();
        }

        void DebugLinkedPrefabs()
        {
            if (linkedPrefabs == null)
            {
                Debug.Log($"LinkedPrefabs: null");
                return;
            }

            Debug.Log($"LinkedPrefabs [{linkedPrefabs.Length}]:");
            for (int i = 0; i < linkedPrefabs.Length; i++)
            {
                Debug.Log($"  [{i}] {(linkedPrefabs[i] != null ? linkedPrefabs[i].name : "null")}");
            }
        }

        internal bool HaveLinkedPrefabsChanged()
        {
            //DebugLinkedPrefabs();
            GameObject[] currentPrefabs = linkedPrefabs;
            UpdateLinkedPrefabs();

            if (currentPrefabs == null && linkedPrefabs == null)
            {
                return false;
            }

            if (currentPrefabs == null || linkedPrefabs == null)
            {
                return true;
            }

            if (currentPrefabs.Length != linkedPrefabs.Length)
            {
                return true;
            }

            for (int i = 0; i < currentPrefabs.Length; i++)
            {
                if (currentPrefabs[i] != linkedPrefabs[i])
                {
                    return true;
                }
            }
            return false;
        }

        public void UpdatePath()
        {
            if (newTab)
            {
                path = "";
                paths = null;
#if UNITY_6000_4_OR_NEWER
                id = EntityId.None;

#else
                id = 0;

#endif
                ids = null;
                historyPaths = null;
                history = null;
                return;
            }
            if (target != null)
            {
                path = EditorUtils.GatherGameObjectPath(target);
                id = EditorUtils.GetObjectId(target);
                paths = null;
                ids = null;
            }
            else if (IsValidMultiTarget())
            {
                paths = targets.Select(EditorUtils.GatherGameObjectPath).ToArray();
                ids = targets.Select(t => EditorUtils.GetObjectId(t)).ToArray();
                path = "";
#if UNITY_6000_4_OR_NEWER
                id = EntityId.None;

#else
                id = 0;

#endif

            }
            if (IsTabValid() && history != null && history.Count > 0)
            {
                historyPaths = history.Select(_history =>
                {
                    var _paths = _history.Select(EditorUtils.GatherGameObjectPath).ToArray();
                    var _ids = _history.Select(h => EditorUtils.GetObjectId(h)).ToArray();
                    return new HistoryPaths(_paths, _ids, prefab);
                }).ToList();

            }
        }

        internal bool RemovedComponentsChanged()
        {
            if (target != null && removedComponents != null && PrefabUtility.IsPartOfPrefabInstance(target))
            {
                var currentRemoved = PrefabUtility.GetRemovedComponents(target);
                if (!EditorUtils.SameRemovedComponents(removedComponents, currentRemoved))
                {
                    removedComponents = currentRemoved;
                    return true;
                }
            }
            return false;
        }

        public bool HasValidTargets()
        {
            if (targets != null && targets.Length > 0)
            {
                return RemoveNulls();
            }
            return false;
        }

        public bool RemoveNulls()
        {
            List<GameObject> _targets = new List<GameObject>();
            foreach (var _target in targets)
            {
                if (_target != null)
                {
                    _targets.Add(_target);
                }
            }
            targets = _targets.ToArray();
            return targets.Length > 0;
        }
        bool IsMulti(int _index)
        {
            if (_index < 0 || _index >= history.Count)
            {
                return false;
            }
            if (history[_index] != null && history[_index].Length > 1)
            {
                return true;
            }
            return false;
        }

        public List<string> NamesBack()
        {
            List<string> names = new List<string>();
            for (int i = historyPosition; i < history.Count; i++)
            {
                if (IsMulti(i))
                {
                    names.Add("(" + history[i].Length + ") Objects");
                }
                else
                {
                    names.Add(history[i][0].name);
                }
            }
            return names;
        }
        public List<string> NamesForward()
        {
            List<string> names = new List<string>();
            for (int i = historyPosition; i > 0; i--)
            {
                if (IsMulti(i))
                {
                    names.Add("(" + history[i].Length + ") Objects");
                }
                else
                {
                    names.Add(history[i][0].name);
                }
            }
            return names;
        }

        public void OnDestroy()
        {
            DestroyAllMaterialMaps();
        }
        public void RefreshIcon()
        {
            if (target != null)
            {
                icon = EditorUtils.GetBestFittingIconForGameObject(target);
            }
        }

        public bool IsAPrefabTab()
        {
            if (IsValidMultiTarget())
            {
                return EditorUtils._AreAllPrefabs(targets);
            }
            else if (target != null)
            {
                return PrefabUtility.IsAnyPrefabInstanceRoot(target);
            }
            return false;
        }
        public void AddToHistory(GameObject[] newTargets)
        {
            if (newTargets == null)
            {
                return;
            }
            if (newTargets.Length == 1)
            {
                _AddToHistory(newTargets[0]);
                FixNulls();
                return;
            }
            if (locked)
            {
                return;
            }
            FixNulls();
            CoInspectorWindow.justOpened = false;
            RemoveIfAlreadyInHistory(newTargets);
            if (historyPosition != 0 && !EditorUtils.CompareArrays(newTargets, targets))
            {
                history.RemoveRange(0, -historyPosition);
                historyPosition = 0;
            }
            if (history.Count > 10)
            {
                history.RemoveAt(history.Count - 1);
            }
            if (history.Count == 0 && targets != null && targets != newTargets)
            {
                history.Insert(0, targets);
            }
            history.Insert(0, newTargets);
            targets = newTargets;
            target = null;
            name = "(" + targets.Length + ") Objects";
            FixNulls();
        }
        bool AlreadyInHistory(GameObject newTarget)
        {
            for (int i = 0; i < history.Count; i++)
            {
                if (IsMulti(i))
                {
                    continue;
                }
                if (history[i][0] == newTarget)
                {
                    return true;
                }
            }
            return false;
        }

        void RemoveIfAlreadyInHistory(GameObject[] newTargets)
        {
            for (int i = 0; i < history.Count; i++)
            {
                if (!IsMulti(i) && EditorUtils.CompareArrays(history[i], newTargets))
                {
                    history.RemoveAt(i);
                    return;
                }
            }
        }

        void RemoveIfAlreadyInHistory(GameObject newTarget)
        {
            for (int i = 0; i < history.Count; i++)
            {
                if (!IsMulti(i) && history[i][0] == newTarget)
                {
                    history.RemoveAt(i);
                    return;
                }
            }
        }
        public void TrySetValidHistoryTarget()
        {
            FixNulls();
            if (history == null)
            {
                return;
            }
            if (history.Count > 0)
            {
                if (history[0] != null && history[0].Length > 0)
                {
                    if (history[0].Length > 1)
                    {
                        targets = history[0];
                        target = null;
                        name = "(" + targets.Length + ") Objects";
                        if (owner.GetActiveTab() != null && owner.GetActiveTab() == this)
                        {
                            owner.SetTargetGameObjects(targets);
                        }
                        else
                        {
                            RefreshIcon();
                        }
                        multiEditMode = true;
                        return;
                    }
                    else
                    {
                        target = history[0][0];
                        targets = null;
                        name = target.name;
                        if (owner.GetActiveTab() != null && owner.GetActiveTab() == this)
                        {
                            owner.SetTargetGameObject(target);
                        }
                        else
                        {
                            RefreshIcon();
                        }
                        multiEditMode = false;
                        return;
                    }
                }

                if (CanMoveBack())
                {
                    MoveBack();
                }
                else if (CanMoveForward())
                {
                    MoveForward();
                }
            }
        }

        public void _AddToHistory(GameObject newTarget)
        {
            newTab = false;
            if (newTarget == null)
            {
                return;
            }
            if (locked && !multiEditMode)
            {
                return;
            }
            else if (multiEditMode)
            {
                return;
            }
            FixNulls();
            CoInspectorWindow.justOpened = false;
            if (historyPosition != 0 && newTarget != target)

            {
                history.RemoveRange(0, -historyPosition);
                historyPosition = 0;
            }
            RemoveIfAlreadyInHistory(newTarget);
            if (history.Count > 10)
            {
                history.RemoveAt(history.Count - 1);
            }
            if (history.Count == 0 && target && target != newTarget)
            {
                history.Insert(0, new GameObject[] { target });
            }

            target = newTarget;
            targets = null;
            icon = EditorUtils.GetBestFittingIconForGameObject(newTarget);
            history.Insert(0, new GameObject[] { newTarget });
            name = target.name;
        }

        public void MoveBack(int movements = 1, bool soft = false)
        {
            FixNulls();
            scrollPosition = 0;
            if (history.Count > -historyPosition + 1)
            {
                bool wereAllCollapsed = AreAllCollapsed();
                historyPosition -= movements;
                if (IsMulti(-historyPosition))
                {
                    targets = history[-historyPosition];
                    target = null;
                    name = "(" + targets.Length + ") Objects";
                    if (owner.GetActiveTab() == this && !soft)
                    {
                        owner.SetTargetGameObjects(targets);
                    }
                    else
                    {
                        RefreshIcon();
                    }
                }
                else
                {
                    target = history[-historyPosition][0];
                    targets = null;
                    name = target.name;
                    if (owner.GetActiveTab() == this && !soft)
                    {
                        owner.SetTargetGameObject(target);
                    }
                    else
                    {
                        RefreshIcon();
                    }
                }
                if (wereAllCollapsed)
                {
                    SetAllMapsTo(false);
                }

            }
        }
        public void MoveForward(int movements = 1, bool soft = false)
        {
            FixNulls();
            scrollPosition = 0;
            if (historyPosition < 0)
            {
                bool wereAllCollapsed = AreAllCollapsed();
                historyPosition += movements;
                if (IsMulti(-historyPosition))
                {
                    targets = history[-historyPosition];
                    target = null;

                    name = "(" + targets.Length + ") Objects";
                    if (owner.GetActiveTab() == this && !soft)
                    {
                        owner.SetTargetGameObjects(targets);
                    }
                    else
                    {
                        RefreshIcon();
                    }
                }
                else
                {
                    target = history[-historyPosition][0];
                    targets = null;

                    name = target.name;
                    if (owner.GetActiveTab() == this && !soft)
                    {
                        owner.SetTargetGameObject(target);
                    }
                    else
                    {
                        RefreshIcon();
                    }
                }
                if (wereAllCollapsed)
                {
                    SetAllMapsTo(false);
                }
            }
        }
        public void RefreshName()
        {
            if (IsValidMultiTarget())
            {
                name = "(" + targets.Length + ") Objects";
                target = null;
            }
            else if (target != null)
            {
                name = target.name;
                targets = null;
            }
        }
        public void TryRepopulateHistory()
        {
            if (newTab)
            {
                return;
            }
            history = EditorUtils.GetHistory(this);
        }

        public void MoveForwardUntil(GameObject[] _target, int maxMovements = 10)
        {
            if (maxMovements == 0)
            {
                return;
            }
            if (EditorUtils.ContainsArray(ForwardHistory(), _target))

            {
                bool wereAllCollapsed = AreAllCollapsed();
                MoveForward(1, true);
                if (_target.Length == 1)
                {
                    if (this.target != _target[0])
                    {
                        MoveForwardUntil(_target, maxMovements - 1);
                    }
                    else if (owner.GetActiveTab() == this)
                    {
                        owner.SetTargetGameObject(target);
                    }
                }
                else
                {
                    if (!EditorUtils.CompareArrays(this.targets, _target))
                    {
                        MoveForwardUntil(_target, maxMovements - 1);
                    }
                    else if (owner.GetActiveTab() == this)
                    {
                        owner.SetTargetGameObjects(targets);
                    }
                }
                if (wereAllCollapsed)
                {
                    SetAllMapsTo(false);
                }
            }
        }

        public void MoveBackUntil(GameObject[] _target, int maxMovements = 10)
        {
            if (EditorUtils.ContainsArray(BackHistory(), _target))
            {
                if (maxMovements == 0)
                {
                    return;
                }
                bool wereAllCollapsed = AreAllCollapsed();
                MoveBack(1, true);
                if (_target.Length == 1)
                {
                    if (this.target != _target[0])
                    {
                        MoveBackUntil(_target, maxMovements - 1);
                    }
                    else if (owner.GetActiveTab() == this)
                    {
                        owner.SetTargetGameObject(target);
                    }
                }
                else
                {
                    if (!EditorUtils.CompareArrays(this.targets, _target))
                    {
                        MoveBackUntil(_target, maxMovements - 1);
                    }
                    else if (owner.GetActiveTab() == this)
                    {
                        owner.SetTargetGameObjects(targets);
                    }
                }
                if (wereAllCollapsed)
                {
                    SetAllMapsTo(false);
                }
            }
        }
        public List<string> _ForwardHistory()
        {
            FixNulls();
            List<string> historyStrings = new List<string>();
            Dictionary<string, int> nameCounts = new Dictionary<string, int>();

            for (int i = 0; i < -historyPosition; i++)
            {
                string itemName;
                if (this.history[i].Length == 1 && this.history[i][0] != null)
                {
                    itemName = this.history[i][0].name;
                }
                else if (this.history[i].Length > 1)
                {
                    itemName = $"({this.history[i].Length}) Objects";
                }
                else
                {
                    continue;
                }

                if (nameCounts.ContainsKey(itemName))
                {
                    nameCounts[itemName]++;
                    itemName += new string(' ', nameCounts[itemName]);
                }
                else
                {
                    nameCounts[itemName] = 0;
                }

                historyStrings.Add(itemName);
            }

            historyStrings.Reverse();
            return historyStrings;
        }

        public List<string> _BackHistory()
        {
            FixNulls();
            List<string> historyStrings = new List<string>();
            Dictionary<string, int> nameCounts = new Dictionary<string, int>();

            for (int i = -historyPosition + 1; i < this.history.Count; i++)
            {
                string itemName;
                if (this.history[i].Length == 1 && this.history[i][0] != null)
                {
                    itemName = this.history[i][0].name;
                }
                else if (this.history[i].Length > 1)
                {
                    itemName = $"({this.history[i].Length}) Objects";
                }
                else
                {
                    continue;
                }

                if (nameCounts.ContainsKey(itemName))
                {
                    nameCounts[itemName]++;
                    itemName += new string(' ', nameCounts[itemName]);
                }
                else
                {
                    nameCounts[itemName] = 0;
                }

                historyStrings.Add(itemName);
            }

            return historyStrings;
        }

        public List<GameObject[]> ForwardHistory()
        {
            FixNulls();
            List<GameObject[]> forwardHistory = new List<GameObject[]>();
            for (int i = 0; i < -historyPosition; i++)
            {
                forwardHistory.Add(this.history[i]);
            }
            forwardHistory.Reverse();
            return forwardHistory;
        }
        public List<GameObject[]> BackHistory()
        {
            FixNulls();
            List<GameObject[]> backHistory = new List<GameObject[]>();
            for (int i = -historyPosition + 1; i < this.history.Count; i++)
            {
                backHistory.Add(this.history[i]);
            }
            return backHistory;
        }

        public bool CanMoveBack()
        {
            if (history == null)
            {
                return false;
            }
            int relativePosition = -historyPosition + 1;
            return history.Count > relativePosition && history.Count > 1;
        }

        public bool CanMoveForward()
        {
            if (history == null)
            {
                return false;
            }
            return historyPosition < 0;
        }

        public bool IsObjectInSamePrefabState(GameObject gameObject)
        {
            if (prefab)
            {
                return CoInspectorWindow.IsGameObjectInPrefabMode(gameObject);
            }
            return !CoInspectorWindow.IsGameObjectInPrefabMode(gameObject);
        }

        public void FixNulls()
        {
            if (history == null)
            {
                history = new List<GameObject[]>();
                historyPosition = 0;
                return;
            }
            /*
            foreach (var _history in history)
            {
                if (_history == null)
                {
                    Debug.Log(_history[0].name);
                }
            }*/

            Scene activeScene = SceneManager.GetActiveScene();
#if UNITY_2021_2_OR_NEWER
            UnityEditor.SceneManagement.PrefabStage prefabStage = PrefabStageUtility.GetCurrentPrefabStage();
#else
    UnityEditor.Experimental.SceneManagement.PrefabStage prefabStage = UnityEditor.Experimental.SceneManagement.PrefabStageUtility.GetCurrentPrefabStage();
#endif
            bool isPrefabStageActive = prefabStage != null;

            int currentIndex = history.Count + historyPosition;
            int removedBeforeCurrent = 0;
            int removedAfterCurrent = 0;

            List<GameObject[]> newHistory = new List<GameObject[]>();
            for (int i = 0; i < history.Count; i++)
            {
                GameObject[] entry = history[i];
                if (entry == null)
                {
                    if (i < currentIndex)
                        removedBeforeCurrent++;
                    else if (i > currentIndex)
                        removedAfterCurrent++;
                    continue;
                }

                GameObject[] validObjects = entry
                    .Where(go => IsValidGameObject(go, activeScene, isPrefabStageActive))
                    .ToArray();

                if (validObjects.Length > 0)
                    newHistory.Add(validObjects);
                else
                {
                    if (i < currentIndex)
                        removedBeforeCurrent++;
                    else if (i > currentIndex)
                        removedAfterCurrent++;
                }
            }
            history = newHistory.Distinct(new GameObjectArrayComparer()).ToList();
            int removedAtCurrent = (currentIndex >= 0 && currentIndex < newHistory.Count && !history.Contains(newHistory[currentIndex])) ? 1 : 0;
            historyPosition += removedBeforeCurrent;
            historyPosition -= removedAfterCurrent;
            historyPosition += removedAtCurrent;
            historyPosition = Mathf.Clamp(historyPosition, -history.Count, 0);
        }
        private class GameObjectArrayComparer : IEqualityComparer<GameObject[]>
        {
            public bool Equals(GameObject[] x, GameObject[] y)
            {
                if (x == null && y == null)
                    return true;
                if (x == null || y == null || x.Length != y.Length)
                    return false;

                for (int i = 0; i < x.Length; i++)
                {
                    if (x[i] != y[i])
                        return false;
                }
                return true;
            }

            public int GetHashCode(GameObject[] obj)
            {
                unchecked
                {
                    int hash = 17;
                    foreach (var gameObject in obj)
                    {
                        hash = hash * 23 + (gameObject != null ? gameObject.GetHashCode() : 0);
                    }
                    return hash;
                }
            }
        }

        private bool IsValidGameObject(GameObject go, Scene activeScene, bool isPrefabStageActive)
        {

            if (go != null && (go.scene == activeScene || go.scene.isLoaded) && go.scene.IsValid())
            {
                return true;
            }
            if (isPrefabStageActive)
            {
#if UNITY_2021_2_OR_NEWER
                UnityEditor.SceneManagement.PrefabStage prefabStage = PrefabStageUtility.GetCurrentPrefabStage();
#else
            UnityEditor.Experimental.SceneManagement.PrefabStage prefabStage = UnityEditor.Experimental.SceneManagement.PrefabStageUtility.GetCurrentPrefabStage();
#endif

                if (prefabStage != null && go != null)
                {
                    return prefabStage.scene == go.scene;
                }
            }
            return false;
        }

        private SerializedProperty[] GetSerializedProperties(SerializedObject serializedObject)
        {
            List<SerializedProperty> propertyList = new List<SerializedProperty>();
            SerializedProperty property = serializedObject.GetIterator();

            while (property.NextVisible(true))
            {
                propertyList.Add(property.Copy());
            }

            return propertyList.ToArray();
        }

        public ComponentMap SaveFoldoutToMap(Component component, bool foldout, Editor editor, int focusAfter = 0)
        {
            if (componentMaps == null)
            {
                componentMaps = new List<ComponentMap>();
            }
            else
            {
                foreach (var map in componentMaps)
                {
                    if (map.component == component)
                    {
                        map.foldout = foldout;
                        map.focusAfter = focusAfter;
                        return map;
                    }
                }
            }

            ComponentMap newMap = new ComponentMap
            {
                component = component,
                foldout = foldout,
                componentName = component.GetType().Name.ToLower(),
                niceComponentName = ObjectNames.NicifyVariableName(component.GetType().Name).ToLower(),
                focusAfter = focusAfter

            };
            /*
             if (editor != null)
                {
                    newMap.serializedObject = editor.serializedObject;
                    newMap.infos = GetSerializedProperties(editor.serializedObject);
                } */

            componentMaps.Add(newMap);
            return newMap;
        }

        public bool GetFoldoutForComponent(Component component, Editor editor)
        {
            if (componentMaps == null || componentMaps.Count == 0)
            {

                SaveFoldoutToMap(component, !AreAllCollapsed(), editor);
                return true;
            }
            foreach (var map in componentMaps)
            {
                if (map.component == component)
                {
                    return map.foldout;
                }
            }
            return true;
        }
        public ComponentMap GetMapWithIndex(int _index)
        {
            if (componentMaps == null)
            {
                componentMaps = new List<ComponentMap>();
                return null;
            }
            foreach (var map in componentMaps)
            {
                if (map.index == _index)
                {
                    return map;
                }
            }
            return null;
        }
        public ComponentMap GetLastComponentMap()
        {
            if (componentMaps == null)
            {
                componentMaps = new List<ComponentMap>();
                return null;
            }
            if (componentMaps.Count == 0)
            {
                return null;
            }
            return GetMapWithIndex(componentMaps.Count - 1);
        }

        internal ComponentMap GetFoldoutMapForComponent(Component component)
        {
            if (componentMaps == null || componentMaps.Count == 0)
            {
                return null;
            }
            foreach (var map in componentMaps)
            {
                if (map.component == component)
                {
                    return map;
                }
            }
            return null;
        }

        internal bool GetFoldoutForComponent(Component component)
        {
            if (componentMaps == null || componentMaps.Count == 0)
            {
                return true;
            }
            foreach (var map in componentMaps)
            {
                if (map.component == component)
                {
                    return map.foldout;
                }
            }
            return true;
        }
        internal bool GetFoldoutForComponent(int index)
        {
            if (componentMaps == null || componentMaps.Count == 0)
            {
                return true;
            }
            foreach (var map in componentMaps)
            {
                if (map.index == index)
                {
                    return map.foldout;
                }

            }
            return true;
        }
        internal ComponentMap GetFoldoutMapForComponent(int index)
        {
            if (componentMaps == null || componentMaps.Count == 0)
            {
                return null;
            }
            foreach (var map in componentMaps)
            {
                if (map.index == index)
                {
                    return map;
                }

            }
            return null;
        }

        public ComponentMap GetFoldoutMapForComponent(Component component, Editor editor)
        {
            if (componentMaps == null || componentMaps.Count == 0)
            {

                return SaveFoldoutToMap(component, !AreAllCollapsed(), editor);
            }
            foreach (var map in componentMaps)
            {
                if (map.component == component)
                {
                    if (map.component == null)
                    {
                        map.component = component;
                        map.componentName = component.GetType().Name.ToLower();
                        map.niceComponentName = ObjectNames.NicifyVariableName(component.GetType().Name).ToLower();
                    }
                    /*
                    if (editor)
                    {
                        if (map.serializedObject == null)
                        {
                            map.serializedObject = editor.serializedObject;
                            map.infos = GetSerializedProperties(editor.serializedObject);
                        }
                       
                    }*/
                    return map;
                }
            }
            return SaveFoldoutToMap(component, !AreAllCollapsed(), editor);
        }

        public bool AreAllCollapsed()
        {
            bool _debug = debug;
            if (CoInspectorWindow.MainCoInspector)
            {
                _debug = debug || CoInspectorWindow.MainCoInspector.globalDebugMode;
            }
            if (componentMaps == null)
            {
                componentMaps = new List<ComponentMap>();
            }
            if (componentMaps.Count == 0)
            {
                return false;
            }
            foreach (var map in componentMaps)
            {
                if (map.foldout)
                {
                    if (!_debug && map.hidden)
                    {
                        continue;
                    }
                    return false;
                }
            }
            return true;
        }

        internal void SetAllMapsTo(bool _foldout)
        {
            if (componentMaps == null)
            {
                componentMaps = new List<ComponentMap>();
            }
            foreach (var map in componentMaps)
            {
                map.foldout = _foldout;
            }
        }

        internal int AllMapsStatus()
        {
            int result = -1;
            if (AreAllCollapsed())
            {
                result = 0;
            }
            else if (AreAllExpanded())
            {
                result = 1;
            }
            return result;
        }

        public bool AreAllExpanded()
        {
            bool _debug = debug;
            if (CoInspectorWindow.MainCoInspector)
            {
                _debug = debug || CoInspectorWindow.MainCoInspector.globalDebugMode;
            }
            if (componentMaps == null)
            {
                componentMaps = new List<ComponentMap>();
            }
            if (componentMaps.Count == 0)
            {
                return false;
            }
            foreach (var map in componentMaps)
            {
                if (!_debug && map.hidden)
                {
                    continue;
                }
                if (!map.foldout)
                {
                    return false;
                }
            }
            return true;
        }
        internal void ResetCulling()
        {
            if (componentMaps == null)
            {
                componentMaps = new List<ComponentMap>();
            }
            foreach (var map in componentMaps)
            {
                map.isCulled = false;
                map.height = -1;
            }
        }

        public void OrderMapsByIndex()
        {
            if (componentMaps == null)
            {
                componentMaps = new List<ComponentMap>();
            }
            componentMaps = componentMaps.OrderBy(map => map.index).ToList();
        }
        public void CleanMap(Component[] components)
        {
            if (componentMaps == null)
            {
                componentMaps = new List<ComponentMap>();
                return;
            }

            var componentMapDict = new Dictionary<Component, ComponentMap>();

            foreach (var map in componentMaps)
            {
                if (map.component != null)
                {
                    componentMapDict[map.component] = map;
                }
            }

            componentMaps = components
                .Where(comp => comp != null && componentMapDict.ContainsKey(comp))
                .Select(comp => componentMapDict[comp])
                .ToList();
        }

        private MaterialMapManager _MaterialMapManager
        {
            get
            {
                if (materialMapManager == null)
                {
                    materialMapManager = new MaterialMapManager();
                }
                return materialMapManager;
            }
        }

        public List<Editor> GetAllMaterialEditors()
        {
            List<Editor> allEditors = new List<Editor>();

            if (_MaterialMapManager?.materialMaps == null)
            {
                return allEditors;
            }
            foreach (var map in _MaterialMapManager.materialMaps)
            {
                if (map != null && map.editors != null)
                {
                    allEditors.AddRange(map.editors);
                }
            }
            return allEditors;
        }

        public bool HaveMaterialsChanged(SerializedObject obj)
        {
            return _MaterialMapManager.HaveMaterialsChanged(obj);
        }

        public MaterialMap GetMaterialMapForComponent(Component component, Editor editor = null)
        {
            return _MaterialMapManager.GetMaterialMapForComponent(component, editor);
        }

        public void DestroyIfPresent(Component component)
        {

            _MaterialMapManager.DestroyIfPresent(component);
        }

        public void DestroyAllMaterialMaps()
        {
            _MaterialMapManager.DestroyAllMaterialMaps();
        }
        public static void DebugMaterialEditors()
        {
            return;
            /*
           MaterialEditor[] materialEditors = Resources.FindObjectsOfTypeAll<MaterialEditor>();
               
                int numEditors = materialEditors.Length;
                Debug.Log("Material editors " + numEditors);
                for (int i = 0; i < numEditors; i++)
                {
                    Debug.Log(materialEditors[i].target);
                }
               // Debug.Log(CoInspector.instances[^1].asset)
             //   CoInspector.DestroyAllIfNotNull(materialEditors);*/
        }

        public void MarkMaterialsForRebuild()
        {
            if (_MaterialMapManager != null)
            {
                TriggerMaterialRebuild();
            }
        }
        internal void TriggerMaterialRebuild()
        {
            if (_MaterialMapManager == null)
            {
                return;
            }
            _MaterialMapManager._DestroyAllMaterialMaps();
            owner.rootVisualElement.MarkDirtyRepaint();
            owner.CullNow(true);
        }

        public bool RebuildMaterialsIfNecessary()
        {
            return _MaterialMapManager.RebuildIfNecessary();
        }
    }
    internal class FloatingTab
    {
        internal TabInfo linkedTab;
        internal CoInspectorWindow owner;
        internal Rect tabRect;
        internal bool showIcon;
        internal bool isSelected;
        internal bool isClosing;
        internal bool isOpening;
        internal Texture2D icon;
        internal float startX;
        internal float tabWidth;
        internal float targetTabX;
        internal float tabDragPoint = 0;
        internal int dragTargetIndex = -1;
        internal int dragIndex = -1;
        internal GUIStyle style;
        internal bool fallingTab = false;
        internal float startTime = -1f;
        internal float animationDuration = 0.12f;
        private float initialTabWidth;
        private float reductionPerCall;

        internal FloatingTab(CoInspectorWindow _owner)
        {
            this.owner = _owner;
        }
        internal void Reset(CoInspectorWindow _owner, bool soft = false)
        {
            linkedTab = null;
            tabRect = Rect.zero;
            showIcon = false;
            isSelected = false;
            isClosing = false;
            isOpening = false;
            icon = null;
            startX = 0;
            targetTabX = 0;
            if (!soft)
            {
                tabDragPoint = 0;
            }
            dragTargetIndex = -1;
            dragIndex = -1;
            fallingTab = false;
            startTime = -1f;
            owner = _owner;
        }

        internal void StartOpeningTab(TabInfo tab)
        {
            if (!isOpening)
            {
                linkedTab = tab;
                isClosing = false;
                isOpening = true;
                startTime = Time.realtimeSinceStartup;
            }
        }

        internal float GetOpeningTabWidth()
        {
            tabWidth = linkedTab.tabWidth;
            if (Event.current == null)
            {
                return tabWidth;
            }
            if (!isOpening || startTime < 0f)
            {
                isOpening = false;
                return tabWidth;
            }
            float t = (Time.realtimeSinceStartup - startTime) / 0.1f;
            if (t >= 1f)
            {
                isOpening = false;
                return tabWidth;
            }
            float value = Mathf.Lerp(0f, tabWidth, t);
            owner.Repaint();
            owner.rootVisualElement.MarkDirtyRepaint();
            return value;
        }

        internal void StartClosingTab(TabInfo tab)
        {
            owner?.HandlePendingTabDeletion();
            if (!isClosing)
            {
                linkedTab = tab;
                isClosing = true;
                isOpening = false;
                startTime = Time.realtimeSinceStartup;
                tabWidth = tab.tabWidth;
                initialTabWidth = tab.tabWidth;
                reductionPerCall = initialTabWidth * 0.1f;
            }
        }

        private const float ClosingAnimationDuration = 0.12f; // Half a second

        internal float GetClosingTabWidth()
        {
            if (Event.current == null || !isClosing)
            {
                FinishClosing();
                return 0f;
            }

            if (startTime < 0f)
            {
                startTime = Time.realtimeSinceStartup;
                initialTabWidth = tabWidth; // Store initial width when starting
            }

            float t = (Time.realtimeSinceStartup - startTime) / ClosingAnimationDuration;
            if (t >= 1f)
            {
                FinishClosing();
                return 0f;
            }

            tabWidth = Mathf.Lerp(initialTabWidth, 0f, t);
            owner.rootVisualElement.MarkDirtyRepaint();
            return tabWidth;
        }

        internal void FinishClosing()
        {
            if (linkedTab != null && !linkedTab.markForDeletion)
            {
                linkedTab.markForDeletion = true;
            }
            if (owner)
            {
                owner.Repaint();
            }

        }
    }
    [Serializable]
    internal class HistoryPaths
    {
        [SerializeField] internal string[] paths;
#if UNITY_6000_4_OR_NEWER
        internal EntityId[] instances;

#else
        internal int[] instances;

#endif
        [SerializeField] internal bool prefab;

#if UNITY_6000_4_OR_NEWER
        public HistoryPaths(string[] history, EntityId[] instanceIDs, bool _prefab = false)
        {
            paths = new string[history.Length];
            Array.Copy(history, paths, history.Length);
            instances = new EntityId[instanceIDs.Length];
            Array.Copy(instanceIDs, instances, instanceIDs.Length);
            prefab = _prefab;
        }
#else
        public HistoryPaths(string[] history, int[] instanceIDs, bool _prefab = false)
        {
            paths = new string[history.Length];
            Array.Copy(history, paths, history.Length);
            instances = new int[instanceIDs.Length];
            Array.Copy(instanceIDs, instances, instanceIDs.Length);
            prefab = _prefab;
        }
#endif


    }
}