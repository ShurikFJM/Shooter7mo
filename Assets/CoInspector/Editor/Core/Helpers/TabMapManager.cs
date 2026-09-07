using System;
using System.Collections.Generic;
using UnityObject = UnityEngine.Object;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.UIElements;

namespace CoInspector
{
    [Serializable]
    internal class MaterialMap
    {
        [SerializeField] internal Component component;
        [SerializeField] internal List<Material> materials;
        [SerializeField] internal List<Editor> editors;
    }
    [Serializable]
    internal class ComponentMap
    {
        [SerializeField] internal int index;
        [SerializeField] internal Component component;
        internal VisualElement visualElement;
#if UNITY_6000_3_OR_NEWER
        [SerializeField] internal EntityId missingComponentID = EntityId.None;
#else
        [SerializeField] internal int missingComponentID = -1;

#endif
        [SerializeField] internal bool foldout;
        [SerializeField] internal int focusAfter;
        [SerializeField] internal bool awaitingScroll;
        [SerializeField] internal bool hidden;
        [SerializeField] internal string componentName = "";
        [SerializeField] internal string niceComponentName = "";
        [NonSerialized] internal float height = -1;
        [NonSerialized] internal bool isCulled = false;
        [NonSerialized] internal bool isFilteredOut = false;
        [NonSerialized] internal int needsVisualElementInspector = -1;

        /*
         [SerializeField] internal SerializedProperty[] infos;
         [SerializeField] internal SerializedObject serializedObject;
         */
        public ComponentMap(ComponentMap other)
        {
            this.component = other.component;
            this.componentName = other.componentName;
            this.niceComponentName = other.niceComponentName;
            this.foldout = other.foldout;
            this.focusAfter = other.focusAfter;
            this.awaitingScroll = other.awaitingScroll;
            this.hidden = other.hidden;
            this.height = -1;
            this.visualElement = other.visualElement;
            this.needsVisualElementInspector = other.needsVisualElementInspector;
        }
        public ComponentMap() { }

        public void FillInNames()
        {
            if (component == null)
            {
                return;
            }
            string _componentName = component.GetType().Name.ToLower();
            bool rebuild = false;
            if (componentName != _componentName)
            {
                rebuild = true;
            }

            if (string.IsNullOrEmpty(componentName) || rebuild)
            {
                componentName = component.GetType().Name.ToLower();
            }
            if (string.IsNullOrEmpty(niceComponentName) || rebuild)
            {
                niceComponentName = ObjectNames.NicifyVariableName(component.GetType().Name).ToLower();
            }
        }
    }
    [Serializable]
    internal class MaterialMapManager
    {
        [SerializeField] internal List<MaterialMap> materialMaps;
        [SerializeField] internal bool timeToRebuild;

        private List<Material> FetchMultiMaterials(UnityObject[] targets)
        {
            HashSet<Material> allMaterials = new HashSet<Material>();
            foreach (var target in targets)
            {
                Component component = target as Component;
                List<Material> materials = FetchValidMaterials(component);
                if (materials != null && materials.Count > 0)
                {
                    foreach (Material mat in materials)
                    {
                        allMaterials.Add(mat);
                    }
                }
            }
            HashSet<Material> commonMaterials = new HashSet<Material>(allMaterials);
            foreach (var target in targets)
            {
                List<Material> materials = FetchValidMaterials(target as Component);
                if (materials != null && materials.Count > 0)
                {
                    HashSet<Material> targetMaterials = new HashSet<Material>(materials);
                    commonMaterials.IntersectWith(targetMaterials);
                }
            }
            return commonMaterials.ToList();
        }
        internal void MarkMaterialsForRebuild()
        {
            if (materialMaps != null && materialMaps.Count > 0)
            {
                timeToRebuild = true;
            }
        }

        private bool HandleTMP(Component component, List<Material> materials)
        {
            if (component.GetType().Name == "TextMeshProUGUI")
            {
                Material materialToRender = Reflected.GetTMPMaterialForRendering(component);
                if (materialToRender)
                {
                    materials.Add(materialToRender);
                    return true;
                }
            }
            return false;
        }



        private List<Material> FetchValidMaterials(Component component)
        {
            List<Material> materialsList = new List<Material>();
            if (!EditorUtils.IsAcceptedMaterialComponent(component))
            {
                return materialsList;
            }
            bool handledSpecial = false;
            bool handledSpecialMask = false;
            if (HandleTMP(component, materialsList))
            {
                handledSpecial = true;
            }
            if (!handledSpecial)
            {
                if (component is Renderer && !EditorUtils.ContainsMask(component) && component.gameObject.GetComponents<Renderer>().Length == 1)
                {
                    materialsList.AddRange((component as Renderer).sharedMaterials);
                }
                else if (component is Graphic)
                {
                    bool isMasked = component is MaskableGraphic && component.gameObject.GetComponent<Mask>();
                    if (!isMasked)
                    {
                        materialsList.Add((component as Graphic).material);
                    }
                }
            }
            if (component is Mask && !handledSpecialMask)
            {
                Mask mask = component as Mask;
                if (mask.graphic != null)
                {
                    materialsList.Add(mask.GetModifiedMaterial(mask.graphic.material));
                }
            }
            materialsList.RemoveAll(x => x == null);
            materialsList = materialsList.Distinct().ToList();
            return materialsList;
        }

        public bool HaveMaterialsChanged(SerializedObject obj)
        {
            if (obj == null || materialMaps == null || materialMaps.Count == 0)
            {
                return true;
            }
            Component component = obj.targetObject as Component;
            if (component == null)
            {
                return true;
            }
            bool isMulti = obj.isEditingMultipleObjects;
            MaterialMap currentMap = null;
            foreach (var map in materialMaps)
            {
                if (map.component == component)
                {
                    currentMap = map;
                    break;
                }
            }
            if (currentMap == null)
            {
                return true;
            }
            MaterialMap newMap = new MaterialMap
            {
                component = component
            };
            if (isMulti)
            {
                newMap.materials = FetchMultiMaterials(obj.targetObjects);
            }
            else
            {
                newMap.materials = FetchValidMaterials(component);
            }
            if (newMap.materials.Count != currentMap.materials.Count)
            {
                return true;
            }
            for (int i = 0; i < newMap.materials.Count; i++)
            {
                if (newMap.materials[i] != currentMap.materials[i])
                {
                    return true;
                }
            }
            return false;
        }
        public bool HaveMaterialsChanged(Component component)
        {
            if (component == null || materialMaps == null || materialMaps.Count == 0)
            {
                return true;
            }
            MaterialMap currentMap = null;
            foreach (var map in materialMaps)
            {
                if (map.component == component)
                {
                    currentMap = map;
                    break;
                }
            }
            if (currentMap == null)
            {
                return true;
            }
            MaterialMap newMap = new MaterialMap
            {
                component = component
            };
            {
                newMap.materials = FetchValidMaterials(component);
            }
            if (newMap.materials.Count != currentMap.materials.Count)
            {
                return true;
            }
            for (int i = 0; i < newMap.materials.Count; i++)
            {
                if (newMap.materials[i] != currentMap.materials[i])
                {
                    return true;
                }
            }
            return false;
        }
        public bool HaveMaterialsChanged(Component[] components)
        {
            if (components == null || materialMaps == null || materialMaps.Count == 0)
            {
                return true;
            }
            Component component = components[0];
            MaterialMap currentMap = null;
            foreach (var map in materialMaps)
            {
                if (map.component == component)
                {
                    currentMap = map;
                    break;
                }
            }
            if (currentMap == null)
            {
                return true;
            }
            MaterialMap newMap = new MaterialMap
            {
                component = component
            };

            {
                newMap.materials = FetchMultiMaterials(components);
            }
            if (newMap.materials.Count != currentMap.materials.Count)
            {
                return true;
            }
            for (int i = 0; i < newMap.materials.Count; i++)
            {
                if (newMap.materials[i] != currentMap.materials[i])
                {
                    return true;
                }
            }
            return false;
        }


        public MaterialMap GetMaterialMapForComponent(Component component, Editor editor = null)
        {
            if (materialMaps == null)
            {
                materialMaps = new List<MaterialMap>();
            }
            CleanNullEditors();
            CleanEmptyMaps();
            foreach (var map in materialMaps)
            {
                if (map.component == component)
                {
                    return map;
                }
            }
            /*
            if (materials == null || materials.Count == 0)
            {
                return null;
            } */
            MaterialMap newMap = new MaterialMap
            {
                component = component
            };
            if (editor != null)
            {
                newMap.materials = FetchMultiMaterials(editor.targets);
            }
            else
            {

                newMap.materials = FetchValidMaterials(component);
                //Debug.Log("Fetched " + newMap.materials.Count + " materials for " + component.GetType());
            }
            newMap.editors = new List<Editor>(newMap.materials.Count);
            foreach (var material in newMap.materials)
            {
                if (material == null)
                {
                    continue;
                }
                Editor newEditor = null;
                Editor.CreateCachedEditor(material, null, ref newEditor);
                if (newEditor != null)
                {
                    newMap.editors.Add(newEditor);
                }
            }


            materialMaps.Add(newMap);
            return newMap;
        }
        public MaterialMap _GetMaterialMapForComponent(Component component)
        {
            if (materialMaps == null || materialMaps.Count == 0)
            {
                return null;
            }
            foreach (var map in materialMaps)
            {
                if (map.component == component)
                {
                    return map;
                }
            }
            return null;
        }

        public void DestroyIfPresent(Component component)
        {
            if (materialMaps == null)
            {
                return;
            }
            List<MaterialMap> _materialMaps = new List<MaterialMap>(materialMaps);
            foreach (var map in _materialMaps)
            {
                if (map.component == component)
                {
                    CoInspectorWindow.DestroyAllIfNotNull(map.editors.ToArray());
                    materialMaps.Remove(map);
                    return;
                }
            }
        }
        void CleanNullEditors()
        {
            if (materialMaps == null)
            {
                return;
            }
            foreach (var map in materialMaps)
            {
                if (map.editors == null)
                {
                    map.editors = new List<Editor>();
                }
                List<Editor> _editors = new List<Editor>();
                foreach (var editor in map.editors)
                {
                    if (editor != null && editor.target != null)
                    {
                        _editors.Add(editor);
                    }
                    else
                    {
                        CoInspectorWindow.DestroyIfNotNull(editor);
                    }
                }
                map.editors = _editors;
            }
        }
        public bool RebuildIfNecessary()
        {
            bool rebuild = timeToRebuild;
            if (timeToRebuild)
            {
                _DestroyAllMaterialMaps();
            }

            return rebuild;
        }
        public void _DestroyAllMaterialMaps()
        {
            DestroyAllMaterialMaps();
            timeToRebuild = false;

        }

        void CleanEmptyMaps()
        {
            if (materialMaps == null)
            {
                materialMaps = new List<MaterialMap>();
                return;
            }
            List<MaterialMap> _materialMaps = new List<MaterialMap>();

            foreach (var map in materialMaps)
            {
                if ((map.editors == null && map.materials != null) ||
                    (map.editors != null && map.materials != null && map.editors.Count != map.materials.Count))
                {
                    CoInspectorWindow.DestroyAllIfNotNull(map.editors?.ToArray());
                }
                else
                {
                    _materialMaps.Add(map);
                }
            }

            materialMaps = _materialMaps;

        }

        public void DestroyAllMaterialMaps()
        {
            if (materialMaps == null || materialMaps.Count == 0)
            {
                return;
            }
            foreach (var map in materialMaps)
            {
                CoInspectorWindow.DestroyAllIfNotNull(map.editors.ToArray());
            }
            // TabInfo.DebugMaterialEditors();
            materialMaps = new List<MaterialMap>();
        }

        public bool IsMaterialMapValid(MaterialMap map, Component component, List<Material> materials)
        {
            if (map.component == component && map.materials != null && materials != null && map.materials.Count == materials.Count)
            {
                for (int i = 0; i < materials.Count; i++)
                {
                    if (map.materials[i] != materials[i])
                    {
                        return false;
                    }
                    if (map.editors[i] == null || map.editors[i].target != materials[i])
                    {
                        return false;
                    }
                }
                return true;
            }
            return false;
        }


        public void CleanMaterialMaps(Component[] components)
        {
            if (materialMaps == null)
            {
                materialMaps = new List<MaterialMap>();
                return;
            }
            CleanNullEditors();

            List<MaterialMap> _materialMaps = new List<MaterialMap>();
            foreach (var map in materialMaps)
            {

                if (components.Contains(map.component) && map.materials != null && map.materials.Count > 0)
                {
                    _materialMaps.Add(map);
                }
                else
                {
                    CoInspectorWindow.DestroyAllIfNotNull(map.editors.ToArray());
                }
            }
            materialMaps = _materialMaps;
        }
    }
    [Serializable]
    internal class ComponentMapManager
    {
        [SerializeField] internal List<ComponentMap> componentMaps;
        [SerializeField] internal bool allCollapsed;

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
            bool collapsedByDefault = CoInspectorWindow.collapsePrefabComponents;
            if (CoInspectorWindow.MainCoInspector.assetOnlyMode)
            {
                collapsedByDefault = false;
            }
            ComponentMap newMap = new ComponentMap
            {
                component = component,
                foldout = !collapsedByDefault,
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

        public void SetAllComponentsTo(bool foldout)
        {
            if (componentMaps == null)
            {
                componentMaps = new List<ComponentMap>();
            }
            foreach (var map in componentMaps)
            {
                map.foldout = foldout;
            }
        }

        public bool GetFoldoutForComponent(Component component, Editor editor, bool debug)
        {
            if (componentMaps == null || componentMaps.Count == 0)
            {

                SaveFoldoutToMap(component, !AreAllCollapsed(debug), editor);
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

        public ComponentMap GetFoldoutMapForComponent(Component component, Editor editor, bool debug)
        {
            if (componentMaps == null || componentMaps.Count == 0)
            {

                return SaveFoldoutToMap(component, !AreAllCollapsed(debug), editor);
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
            return SaveFoldoutToMap(component, !AreAllCollapsed(debug), editor);
        }

        public bool AreAllCollapsed(bool debug)
        {
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
                    if (!debug && map.hidden)
                    {
                        continue;
                    }
                    return false;
                }
            }
            return true;
        }

        public bool AreAllExpanded()
        {
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

        public void CleanAll()
        {
            componentMaps = new List<ComponentMap>();
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
    }
}
