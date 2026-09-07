using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace CoInspector
{
    internal static class CustomGUIContents
    {

        static Texture2D mainIcon;
        static Texture2D mainLogo;
        static Texture2D smallLogo;
        static Texture2D historyButton;
        static Texture2D expandButton;
        static Texture2D debugONButton;
        static Texture2D fullDebugONButton;
        static Texture2D debugOFFButton;
        static Texture2D foldoutONButton;
        static Texture2D foldoutOFFButton;
        static Texture2D lockONButton;
        static Texture2D saveButton;
        static Texture2D saveAsset;
        static Texture2D saveButtonActive;
        static Texture2D saveButtonAuto;
        static Texture2D saveButtonActiveUpdated;
        static Texture2D undoSaveButton;
        static Texture2D redoSaveButton;
        static Texture2D installed_1;
        static Texture2D installed_2;
        static Texture2D installed_3;
        static Texture2D installed_4;
        static Texture2D updateButton;
        static Texture2D searchButton;
        static Texture2D filterButtonON;
        static Texture2D filterButtonOFF;
        static Texture2D settingsButton;
        static Texture2D lockOFFButton;
        static Texture2D selectButton;
        static Texture2D selectButtonSelected;
        static Texture2D sceneToolsButton;
        static Texture2D sceneToolsButtonSelected;
        static Texture2D multiSelectButton;
        static Texture2D closeButton;
        static Texture2D minimizeButton;
        static Texture2D maximizeButton;
        static Texture2D historyNextButton;
        static Texture2D historyBackButton;
        static Texture2D newTabButton;
        static Texture2D multiTabButton;
        static Texture2D infoButton;
        static Texture2D previewExpand;
        static Texture2D previewCollapse;
        static Texture2D previewBackground;
        static Texture2D scrollCursor;
        static Texture2D scrollCursorUp;
        static Texture2D scrollCursorDown;
        static string rootPath;
        static ChangelogData.Changelog changelog;
        static ChangelogData.Changelog validChangelog;

        static string GetRootPath()
        {
            if (string.IsNullOrEmpty(rootPath))
            {
                rootPath = CoInspectorWindow._GetRootPath();
            }
            return rootPath;
        }

        static Texture2D LoadTexture(string textureName)
        {
            bool use2x = !EditorUtils.EditorScaleIsOne();
            string path = System.IO.Path.Combine(CoInspectorWindow._GetRootPath() + "/Custom/Textures/", textureName);
            if (use2x)
            {
                string path2x = path.Replace(".png", "2x.png");
                if (System.IO.File.Exists(path2x))
                {
                    path = path2x;
                }
            }
            if (System.IO.File.Exists(path))
            {
                return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
            }
            return null;
        }
        internal static UnityEngine.Object LoadCustomAsset(string _path, bool notInEditor = false)
        {
            string path = GetRootPath();
            if (notInEditor)
            {
                path = path.Substring(0, path.LastIndexOf("/Editor", StringComparison.Ordinal));
            }
            path += _path;
            return AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(path);
        }
        internal static Texture2D MainIconImage
        {
            get
            {
                if (mainIcon == null)
                {
                    if (EditorGUIUtility.isProSkin)
                    {
                        mainIcon = LoadTexture("icon.png");
                    }
                    else
                    {
                        mainIcon = LoadTexture("icon_light.png");
                    }
                }
                return mainIcon;
            }
        }

        internal static ChangelogData.Changelog ValidChangelog
        {
            get
            {
                if (changelog == null)
                {
                    UnityEngine.Object jsonFile = LoadCustomAsset("/Settings/changelog.json");
                    if (!jsonFile)
                    {
                        return null;
                    }
                    string json = File.ReadAllText(AssetDatabase.GetAssetPath(jsonFile));
                    changelog = JsonUtility.FromJson<ChangelogData.Changelog>(json);
                    if (changelog != null)
                    {
                        validChangelog = changelog.version == UpdateChecker.currentVersion ? changelog : null;
                    }
                    else
                    {
                        validChangelog = null;
                    }
                }
                return validChangelog;
            }
        }
        internal static Texture2D SmallLogoImage
        {
            get
            {
                if (smallLogo == null)
                {
                    {
                        smallLogo = LoadTexture("icon_logoSmall.png");
                    }
                }
                return smallLogo;
            }
        }
        internal static Texture2D SceneToolsButtonImage
        {
            get
            {
                if (sceneToolsButton == null)
                {
                    sceneToolsButton = LoadTexture("icon_sceneTools.png");
                }
                return sceneToolsButton;
            }
        }
        internal static Texture2D SceneToolsButtonImageSelected
        {
            get
            {
                if (sceneToolsButtonSelected == null)
                {
                    sceneToolsButtonSelected = LoadTexture("icon_sceneTools_selected.png");
                }
                return sceneToolsButtonSelected;
            }
        }

        internal static Texture2D SettingsButtonImage
        {
            get
            {
                if (settingsButton == null)
                {
                    settingsButton = LoadTexture("icon_settings.png");
                }
                return settingsButton;
            }
        }
        internal static Texture2D SaveButtonImage
        {
            get
            {
                if (saveButton == null)
                {
                    saveButton = LoadTexture("icon_save.png");
                }
                return saveButton;
            }
        }
        internal static Texture2D SaveButtonActiveImage
        {
            get
            {
                if (saveButtonActive == null)
                {
                    saveButtonActive = LoadTexture("icon_save_active.png");
                }
                return saveButtonActive;
            }
        }
        internal static Texture2D SaveAssetImage
        {
            get
            {
                if (saveAsset == null)
                {
                    saveAsset = LoadTexture("icon_save_asset.png");
                }
                return saveAsset;
            }
        }
        internal static Texture2D SaveButtonActiveUpdated
        {
            get
            {
                if (saveButtonActiveUpdated == null)
                {
                    saveButtonActiveUpdated = LoadTexture("icon_save_updated.png");
                }
                return saveButtonActiveUpdated;
            }
        }
        internal static Texture2D SaveButtonAuto
        {
            get
            {
                if (saveButtonAuto == null)
                {
                    saveButtonAuto = LoadTexture("icon_save_auto.png");
                }
                return saveButtonAuto;
            }
        }
        internal static Texture2D UndoSaveButtonImage
        {
            get
            {
                if (undoSaveButton == null)
                {
                    undoSaveButton = LoadTexture("icon_undo.png");
                }
                return undoSaveButton;
            }
        }
        internal static Texture2D RedoSaveButtonImage
        {
            get
            {
                if (redoSaveButton == null)
                {
                    redoSaveButton = LoadTexture("icon_redo.png");
                }
                return redoSaveButton;
            }
        }

        internal static Texture2D MainLogoImage
        {
            get
            {
                if (mainLogo == null)
                {
                    mainLogo = LoadTexture("icon_logo.png");
                }
                return mainLogo;
            }
        }
        internal static Texture2D Installed_1
        {
            get
            {
                if (installed_1 == null)
                {
                    installed_1 = LoadTexture("icon_installed_1.png");
                }
                return installed_1;
            }
        }
        internal static Texture2D Installed_2
        {
            get
            {
                if (installed_2 == null)
                {
                    installed_2 = LoadTexture("icon_installed_2.png");
                }
                return installed_2;
            }
        }
        internal static Texture2D Installed_3
        {
            get
            {
                if (installed_3 == null)
                {
                    installed_3 = LoadTexture("icon_installed_3.png");
                }
                return installed_3;
            }
        }
        internal static Texture2D Installed_4
        {
            get
            {
                if (installed_4 == null)
                {
                    installed_4 = LoadTexture("icon_installed_4.png");
                }
                return installed_4;
            }
        }
        internal static Texture2D UpdateLogo
        {
            get
            {
                if (updateButton == null)
                {
                    updateButton = LoadTexture("frame_update.png");
                }
                return updateButton;
            }
        }
        internal static Texture2D SearchButtonImage
        {
            get
            {
                if (searchButton == null)
                {
                    searchButton = LoadTexture("icon_search.png");
                }
                return searchButton;
            }
        }
        internal static Texture2D FilterButtonONImage
        {
            get
            {
                if (filterButtonON == null)
                {
                    filterButtonON = LoadTexture("icon_filter_on.png");
                }
                return filterButtonON;
            }
        }
        internal static Texture2D FilterButtonOFFImage
        {
            get
            {
                if (filterButtonOFF == null)
                {
                    filterButtonOFF = LoadTexture("icon_filter_off.png");
                }
                return filterButtonOFF;
            }
        }
        internal static Texture2D CursorScroll
        {
            get
            {
                if (scrollCursor == null)
                {
                    scrollCursor = LoadTexture("cursor_scroll.png");
                }
                return scrollCursor;
            }
        }
        internal static Texture2D CursorScrollUp
        {
            get
            {
                if (scrollCursorUp == null)
                {
                    scrollCursorUp = LoadTexture("cursor_scrollUp.png");
                }
                return scrollCursorUp;
            }
        }

        internal static Texture2D CursorScrollDown
        {
            get
            {
                if (scrollCursorDown == null)
                {
                    scrollCursorDown = LoadTexture("cursor_scrollDown.png");
                }
                return scrollCursorDown;
            }
        }

        internal static Texture2D MinimizeButtonImage
        {
            get
            {
                if (minimizeButton == null)
                {
                    minimizeButton = LoadTexture(EditorUtils.IsLightSkin()?"icon_minimize_light.png":"icon_minimize.png");
                }
                return minimizeButton;
            }
        }
        internal static Texture2D MaximizeButtonImage
        {
            get
            {
                if (maximizeButton == null)
                {
                    maximizeButton = LoadTexture(EditorUtils.IsLightSkin()?"icon_maximize_light.png":"icon_maximize.png");
                }
                return maximizeButton;
            }
        }
        static GUIContent _minimizeContent;
        internal static GUIContent MinimizeContent
        {
            get
            {
                if (_minimizeContent == null)
                {
                    _minimizeContent = new GUIContent(MinimizeButtonImage)
                    {
                        tooltip = "Exit Asset-Only Mode"
                    };
                }
                return _minimizeContent;
            }
        }
        static GUIContent _maximizeContenr;
        internal static GUIContent MaximizeContent
        {
            get
            {
                if (_maximizeContenr == null)
                {
                    _maximizeContenr = new GUIContent(MaximizeButtonImage)
                    {
                        tooltip = "Enter Asset-Only Mode"
                    };
                }
                return _maximizeContenr;
            }
        }

        static GUIContent undoSaveContent;
        internal static GUIContent UndoSaveContent
        {
            get
            {
                if (undoSaveContent == null)
                {
                    undoSaveContent = new GUIContent(UndoSaveButtonImage)
                    {
                        tooltip = "Undo Play Mode changes"
                    };
                }
                return undoSaveContent;
            }
        }
        static GUIContent redoSaveContent;
        internal static GUIContent RedoSaveContent
        {
            get
            {
                if (redoSaveContent == null)
                {
                    redoSaveContent = new GUIContent(RedoSaveButtonImage)
                    {
                        tooltip = "Redo Play Mode changes"
                    };
                }
                return redoSaveContent;
            }
        }

        internal static Texture2D NewTabButtonImage
        {
            get
            {
                if (newTabButton == null)
                {
                    if (EditorGUIUtility.isProSkin)
                    {
                        newTabButton = LoadTexture("icon_newTab.png");
                    }
                    else
                    {
                        newTabButton = LoadTexture("icon_newTab_light.png");
                    }
                }
                return newTabButton;
            }
        }
        internal static Texture2D InfoButtonImage
        {
            get
            {
                if (infoButton == null)
                {
                    infoButton = LoadTexture("icon_info.png");
                }
                return infoButton;
            }
        }
        internal static Texture2D PreviewExpand
        {
            get
            {
                if (previewExpand == null)
                {
                    previewExpand = LoadTexture("icon_previewExpand.png");
                }
                return previewExpand;
            }
        }
        internal static Texture2D PreviewCollapse
        {
            get
            {
                if (previewCollapse == null)
                {
                    previewCollapse = LoadTexture("icon_previewCollapse.png");
                }
                return previewCollapse;
            }
        }
        internal static Texture2D PreviewBackground
        {
            get
            {
                if (previewBackground == null)
                {
                    previewBackground = LoadTexture(EditorUtils.IsLightSkin()?"tex_previewBack_light.png":"tex_previewBack.png");
                }
                return previewBackground;
            }
        }

        internal static Texture2D MultiTabButtonImage
        {
            get
            {
                if (multiTabButton == null)
                {
                    if (EditorGUIUtility.isProSkin)
                    {
                        multiTabButton = LoadTexture("icon_multiTarget.png");
                    }
                    else
                    {
                        multiTabButton = LoadTexture("icon_multiTarget_light.png");
                    }
                }
                return multiTabButton;
            }
        }
        internal static Texture2D LockONButtonImage
        {
            get
            {
                if (lockONButton == null)
                {
                    lockONButton = LoadTexture("icon_lockON_light.png");
                }
                return lockONButton;
            }
        }
        internal static Texture2D LockOFFButtonImage
        {
            get
            {
                if (lockOFFButton == null)
                {
                    lockOFFButton = LoadTexture("icon_lockOFF_light.png");
                }
                return lockOFFButton;
            }
        }
        internal static Texture2D HistoryNextButtonImage
        {
            get
            {
                if (historyNextButton == null)
                {
                    if (EditorGUIUtility.isProSkin)
                    {
                        historyNextButton = LoadTexture("icon_nextHistory.png");
                    }
                    else
                    {
                        historyNextButton = LoadTexture("icon_nextHistory_light.png");
                    }
                }
                return historyNextButton;
            }
        }
        internal static Texture2D HistoryBackButtonImage
        {
            get
            {
                if (historyBackButton == null)
                {
                    if (EditorGUIUtility.isProSkin)
                    {
                        historyBackButton = LoadTexture("icon_backHistory.png");
                    }
                    else
                    {
                        historyBackButton = LoadTexture("icon_backHistory_light.png");
                    }
                }
                return historyBackButton;
            }
        }

        internal static Texture2D FoldoutONButtonImage
        {
            get
            {
                if (foldoutONButton == null)
                {
                    if (EditorGUIUtility.isProSkin)
                    {
                        foldoutONButton = LoadTexture("icon_foldoutON.png");
                    }
                    else
                    {
                        foldoutONButton = LoadTexture("icon_foldoutON_light.png");
                    }

                }
                return foldoutONButton;
            }
        }


        internal static Texture2D FoldoutOFFButtonImage
        {
            get
            {
                if (foldoutOFFButton == null)
                {
                    if (EditorGUIUtility.isProSkin)
                    {
                        foldoutOFFButton = LoadTexture("icon_foldoutOFF.png");
                    }
                    else
                    {
                        foldoutOFFButton = LoadTexture("icon_foldoutOFF_light.png");
                    }
                }
                return foldoutOFFButton;
            }
        }

        internal static Texture2D HistoryButtonImage
        {
            get
            {
                if (historyButton == null)
                {
                    if (EditorGUIUtility.isProSkin)
                    {
                        historyButton = LoadTexture("icon_history.png");
                    }
                    else
                    {
                        historyButton = LoadTexture("icon_history_light.png");
                    }
                }
                return historyButton;
            }
        }
        internal static Texture2D CloseButtonImage
        {
            get
            {
                if (closeButton == null)
                {
                    if (EditorGUIUtility.isProSkin)
                    {
                        closeButton = LoadTexture("icon_close.png");
                    }
                    else
                    {
                        closeButton = LoadTexture("icon_close_light.png");
                    }
                }
                return closeButton;
            }
        }

        internal static Texture2D ExpandButtonImage
        {
            get
            {
                if (expandButton == null)
                {
                    expandButton = LoadTexture("icon_collapseTool.png");
                }
                return expandButton;
            }
        }

        internal static Texture2D DebugONButtonImage
        {
            get
            {
                if (debugONButton == null)
                {
                    if (EditorGUIUtility.isProSkin)
                    {
                        debugONButton = LoadTexture("icon_debugON.png");
                    }
                    else
                    {
                        debugONButton = LoadTexture("icon_debugON_light.png");
                    }
                }
                return debugONButton;
            }
        }
        internal static Texture2D FullDebugONButtonImage
        {
            get
            {
                if (fullDebugONButton == null)
                {

                    fullDebugONButton = LoadTexture("icon_fullDebugON.png");
                }
                return fullDebugONButton;
            }
        }

        internal static Texture2D DebugOFFButtonImage
        {
            get
            {
                if (debugOFFButton == null)
                {
                    if (EditorGUIUtility.isProSkin)
                    {
                        debugOFFButton = LoadTexture("icon_debugOFF.png");
                    }
                    else
                    {
                        debugOFFButton = LoadTexture("icon_debugOFF_light.png");
                    }
                }
                return debugOFFButton;
            }
        }

        internal static Texture2D SelectButtonImage
        {
            get
            {
                if (selectButton == null)
                {
                    if (EditorGUIUtility.isProSkin)
                    {
                        selectButton = LoadTexture("icon_selectTool.png");
                    }
                    else
                    {
                        selectButton = LoadTexture("icon_selectTool_light.png");
                    }
                }
                return selectButton;
            }
        }

        internal static Texture2D SelectButtonSelectedImage
        {
            get
            {
                if (selectButtonSelected == null)
                {
                    if (EditorGUIUtility.isProSkin)
                    {
                        selectButtonSelected = LoadTexture("icon_selectTool_selected.png");
                    }
                    else
                    {
                        selectButtonSelected = LoadTexture("icon_selectTool_light_selected.png");
                    }
                }
                return selectButtonSelected;
            }
        }

        internal static Texture2D MultiSelectButtonImage
        {
            get
            {
                if (multiSelectButton == null)
                {
                    if (EditorGUIUtility.isProSkin)
                    {
                        multiSelectButton = LoadTexture("icon_multiSelection.png");
                    }
                    else
                    {
                        multiSelectButton = LoadTexture("icon_multiSelection_light.png");
                    }
                }
                return multiSelectButton;
            }
        }

        internal static GUIContent SelectContent
        {
            get
            {
                if (_selectContent == null)
                {
                    _selectContent = new GUIContent(SelectButtonImage)
                    {
                        tooltip = "Click: Select in Hierarchy\nRight-click: Select & Frame"
                    };
                }
                return _selectContent;
            }
        }

        internal static GUIContent GetSelectContent(bool selected)
        {
            GUIContent content = SelectContent;
            content.image = SelectButtonImage;
            string tooltip = "Click: Select in Hierarchy\nRight-click: Select & Frame";
            string tooltipSelected = "Already selected";
            if (selected && content.tooltip != tooltipSelected)
            {
                content.tooltip = tooltipSelected;
            }
            else if (!selected && content.tooltip != tooltip)
            {
                content.tooltip = tooltip;
            }
            return content;
        }

        private static GUIContent _selectContent;

        private static GUIContent _focusContent;
        internal static GUIContent FocusContent
        {
            get
            {
                if (_focusContent == null)
                {
                    GUIContent focusIcon;
                    if (EditorGUIUtility.isProSkin)
                    {
                        focusIcon = EditorGUIUtility.IconContent("d_Animation.FilterBySelection");
                    }
                    else

                    {
                        focusIcon = EditorGUIUtility.IconContent("Animation.FilterBySelection");
                    }

                    _focusContent = new GUIContent(focusIcon)
                    {
                        tooltip = "Click: Frame on Scene View\nRight-click: Ping in Hierarchy\nMiddle-click: Scroll to Tab"
                    };
                }
                return _focusContent;
            }
        }

        internal static GUIContent MultiSelectingContent
        {
            get
            {
                if (_multiSelectingContent == null)
                {
                    _multiSelectingContent = new GUIContent("Selecting:");
                }
                return _multiSelectingContent;
            }
        }

        private static GUIContent _multiSelectingContent;

        internal static GUIContent MissingComponentContent
        {
            get
            {
                if (_missingComponentContent == null)
                {
                    _missingComponentContent = new GUIContent(" Fix in Inspector")
                    {
                        image = CustomGUIContents.EditContentDefault.image
                    };
                }
                return _missingComponentContent;
            }
        }

        private static GUIContent _missingComponentContent;

        private static GUIContent unfoldedFoldout;
        internal static GUIContent UnfoldedFoldout
        {
            get
            {
                if (unfoldedFoldout == null)
                {
                    unfoldedFoldout = new GUIContent(FoldoutONButtonImage)
                    {
                        tooltip = "Click to fold selection",

                    };
                }
                return unfoldedFoldout;
            }
        }

        private static GUIContent saveTransformContent;
        internal static GUIContent SaveTransformContent(bool alreadySaved, bool sameTransform = false, bool isAutoSave = false)
        {

            if (saveTransformContent == null)
            {
                saveTransformContent = new GUIContent(SaveButtonImage);
            }
            if (alreadySaved && isAutoSave)
            {
                saveTransformContent.image = SaveButtonAuto;
                return saveTransformContent;
            }
            else
            {
                saveTransformContent.image = alreadySaved ? SaveButtonActiveImage : SaveButtonImage;
            }
            saveTransformContent.tooltip = alreadySaved ? "Don't save changes to this Transform" : "Save Play Mode changes to this Transform";

            if (sameTransform)
            {
                saveTransformContent.image = SaveButtonActiveUpdated;
                saveTransformContent.tooltip = "Update changes to this Transform";
            }
            return saveTransformContent;

        }
        private static GUIContent undoSaveTransformContent;
        internal static GUIContent UndoSaveTransformContent()
        {

            if (undoSaveTransformContent == null)
            {
                undoSaveTransformContent = new GUIContent(HistoryButton);
                undoSaveTransformContent.tooltip = "Undo Play Mode changes";
            }

            return undoSaveTransformContent;

        }

        private static GUIContent foldedFoldout;
        internal static GUIContent FoldedFoldout
        {
            get
            {
                if (foldedFoldout == null)
                {
                    foldedFoldout = new GUIContent(FoldoutOFFButtonImage)
                    {
                        tooltip = "Click to expand selection"
                    };
                }
                return foldedFoldout;
            }
        }

        internal static GUIContent updateContent;
        internal static GUIContent UpdateContent
        {
            get
            {
                if (updateContent == null)
                {
                    updateContent = new GUIContent(UpdateLogo)
                    {
                        tooltip = "Click to Update CoInspector"
                    };
                }
                return updateContent;
            }
        }

        internal static GUIContent EditContentDefault
        {
            get
            {
                if (_editContentDefault == null)
                {
                    _editContentDefault = new GUIContent(EditorGUIUtility.IconContent("UnityEditor.InspectorWindow"))
                    {
                        tooltip = "Show in Inspector Window\n(Hold Alt or Shift to pop-up!)",
                        text = ""
                    };
                }
                return _editContentDefault;
            }
        }

        private static GUIContent _emptyGameObjectContent;
        internal static GUIContent EmptyGameObjectContent
        {
            get
            {
                if (_emptyGameObjectContent == null)
                {
                    _emptyGameObjectContent = new GUIContent(EditorGUIUtility.IconContent("Transform Icon"))
                    {
                        tooltip = "Create Empty GameObject"
                    };
                }
                return _emptyGameObjectContent;
            }
        }

        private static GUIContent _deleteContent;
        internal static GUIContent DeleteContent
        {
            get
            {
                if (_deleteContent == null)
                {
                    _deleteContent = new GUIContent(CloseButtonImage)
                    {
                        tooltip = "Delete Session"
                    };
                }
                return _deleteContent;
            }
        }
        private static GUIContent _emptyRectTransformContent;
        internal static GUIContent EmptyRectTransformContent
        {
            get
            {
                if (_emptyRectTransformContent == null)
                {
                    _emptyRectTransformContent = new GUIContent(EditorGUIUtility.IconContent("RectTransform Icon"))
                    {
                        tooltip = "Create Empty RectTransform"
                    };
                }
                return _emptyRectTransformContent;
            }
        }

        private static GUIContent _multiAssetContent;
        internal static GUIContent MultiAsset
        {
            get
            {
                if (_multiAssetContent == null)
                {

                    _multiAssetContent = new GUIContent(MultiSelectButtonImage);
                }


                return _multiAssetContent;
            }
        }

        internal static GUIContent EditContentPopup
        {
            get
            {
                if (_editContentPopup == null)
                {
                    _editContentPopup = new GUIContent(EditorGUIUtility.IconContent("UnityEditor.InspectorWindow"))
                    {
                        tooltip = "Pop-up in new Inspector Window",
                        text = "+"
                    };
                }
                return _editContentPopup;
            }
        }

        private static GUIContent _hierarchyContent;
        internal static GUIContent HierarchyContent
        {
            get
            {
                if (_hierarchyContent == null)
                {
                    _hierarchyContent = new GUIContent(EditorGUIUtility.IconContent("UnityEditor.SceneHierarchyWindow"))
                    {
                        tooltip = "Pop up target in local Hierarchy view"
                    };
                }

                return _hierarchyContent;
            }
        }


        private static GUIContent openContent;
        internal static GUIContent OpenContent
        {
            get
            {
                if (openContent == null)
                {
                    openContent = new GUIContent(EditorGUIUtility.IconContent("CollabEdit Icon"))
                    {
                        tooltip = "Open this asset in the default app",
                    };
                }
                return openContent;
            }
        }

        private static Dictionary<Type, List<UnityEngine.Object>> lastAssetSelection;
        private static GUIContent[] _assetContents;
        internal static GUIContent AssetContent(Dictionary<Type, List<UnityEngine.Object>> assetSelection, int index)
        {
            if (lastAssetSelection == null || lastAssetSelection != assetSelection || _assetContents == null || index >= _assetContents.Length)
            {
                //  Debug.Log("Creating new asset contents");
                lastAssetSelection = assetSelection;
                _assetContents = new GUIContent[assetSelection.Count];
                int i = 0;
                foreach (var type in assetSelection.Keys)
                {
                    GUIContent buttonContent = new GUIContent();
                    int missingCount = 0;
                    bool isMissingScript = type == typeof(MissingScriptManager);
                    if (isMissingScript && MissingScriptManager.IsMulti)
                    {
                        missingCount = MissingScriptManager.Count() - 1;
                    }
                    string plural = assetSelection[type].Count + missingCount > 1 ? "s" : "";
                    bool isPrefab = type == typeof(GameObject);
                    bool isImportedObject = EditorUtils.AreAllTargetsImportedObjects(assetSelection[type].ToArray());

                    bool isFolder = CoInspectorWindow.IsAssetAFolder(AssetDatabase.GetAssetPath(assetSelection[type][0]));
                    if (isImportedObject)
                    {
                        buttonContent.image = CustomGUIContents.ImportedIcon.image;
                        buttonContent.text = assetSelection[type].Count.ToString() + " Imported Object";
                    }
                    else if (!isPrefab && !isFolder)
                    {
                        buttonContent.image = AssetPreview.GetMiniTypeThumbnail(type);
                        buttonContent.text = (assetSelection[type].Count + missingCount).ToString() + " " + ObjectNames.NicifyVariableName(type.Name);
                        if (isMissingScript)
                        {
                            buttonContent.text = buttonContent.text.Replace("Manager", "");
                        }
                    }
                    else if (isPrefab)
                    {
                        buttonContent.image = CustomGUIContents.PrefabIcon.image;
                        buttonContent.text = assetSelection[type].Count.ToString() + " Prefab";
                    }

                    else if (isFolder)
                    {
                        buttonContent.image = CustomGUIContents.FolderIcon.image;
                        buttonContent.text = assetSelection[type].Count.ToString() + " Folder";
                    }
                    if (buttonContent.text[buttonContent.text.Length - 1] != 's')
                    {
                        buttonContent.text += plural;
                    }
                    _assetContents[i] = buttonContent;
                    i++;
                }
            }
            return _assetContents[index];
        }

        private static GUIContent filterONContent;
        internal static GUIContent FilterONContent
        {
            get
            {
                if (filterONContent == null)
                {
                    filterONContent = new GUIContent(FilterButtonONImage)
                    {
                        tooltip = "Toggle Component Filter\n(Alt + G)"
                    };
                }
                return filterONContent;
            }
        }
        private static GUIContent filterOFFContent;
        internal static GUIContent FilterOFFContent
        {
            get
            {
                if (filterOFFContent == null)
                {
                    filterOFFContent = new GUIContent(FilterButtonOFFImage)
                    {
                        tooltip = "Filter Components\n(Alt + F)"
                    };
                }
                return filterOFFContent;
            }
        }

        private static GUIContent _noneContent;
        internal static GUIContent NoneContent
        {
            get
            {
                if (_noneContent == null)
                {
                    _noneContent = new GUIContent(GUIContent.none);
                }
                return _noneContent;
            }
        }

        private static GUIContent _emptyButton;
        internal static GUIContent EmptyButton
        {
            get
            {
                if (_emptyButton == null)
                {
                    _emptyButton = new GUIContent(GUIContent.none)
                    {
                        tooltip = "Click to ping\nRight click to copy path"
                    };
                }
                return _emptyButton;
            }
        }
        private static GUIContent _quickUnlock;
        internal static GUIContent QuickUnlock
        {
            get
            {
                if (_quickUnlock == null)
                {
                    _quickUnlock = new GUIContent(GUIContent.none)
                    {
                        tooltip = "Unlock Tab"
                    };
                }
                return _quickUnlock;
            }
        }

        private static GUIContent _emptyContent;
        internal static GUIContent EmptyContent
        {
            get
            {
                if (_emptyContent == null)
                {
                    _emptyContent = new GUIContent(GUIContent.none);
                }
                _emptyContent.tooltip = null;
                return _emptyContent;
            }
        }

        private static GUIContent _openPrefabContent;
        internal static GUIContent OpenPrefabContent
        {
            get
            {
                if (_openPrefabContent == null)
                {
                    _openPrefabContent = new GUIContent(EditorGUIUtility.IconContent("SceneAsset Icon"))
                    {
                        tooltip = "Open Prefab"
                    };
                }
                return _openPrefabContent;
            }
        }

        private static GUIContent folderIcon;
        internal static GUIContent FolderIcon
        {
            get
            {
                if (folderIcon == null)
                {
                    folderIcon = new GUIContent(EditorGUIUtility.IconContent("Folder Icon"))
                    {
                        tooltip = "Reveal the asset in its folder"
                    };
                }
                return folderIcon;
            }
        }
        private static GUIContent _debugIconON;
        internal static GUIContent DebugIconON
        {
            get
            {
                if (_debugIconON == null)
                {
                    _debugIconON = new GUIContent(DebugONButtonImage)
                    {
                        tooltip = "Exit Debug Mode"
                    };
                }
                return _debugIconON;
            }
        }
        private static GUIContent _debugIconOFF;

        internal static GUIContent DebugIconOFF
        {
            get
            {
                if (_debugIconOFF == null)
                {
                    _debugIconOFF = new GUIContent(DebugOFFButtonImage)
                    {
                        tooltip = "Click: Debug Tab\nMiddle-click: Full Debug Mode"
                    };
                }
                return _debugIconOFF;
            }
        }
        private static GUIContent _fullDebugIconON;
        internal static GUIContent FullDebugIconON
        {
            get
            {
                if (_fullDebugIconON == null)
                {
                    _fullDebugIconON = new GUIContent(FullDebugONButtonImage)
                    {
                        tooltip = "Exit Full Debug Mode"
                    };
                }
                return _fullDebugIconON;
            }
        }
        private static GUIContent _inspectAssetNormal;
        internal static GUIContent InspectAssetNormal
        {
            get
            {
                if (_inspectAssetNormal == null)
                {
                    _inspectAssetNormal = new GUIContent(EditorGUIUtility.IconContent("UnityEditor.InspectorWindow"))
                    {
                        tooltip = "Open this asset in Inspector\n(Hold Alt/Shift to pop-up a new window!)"
                    };
                }
                return _inspectAssetNormal;
            }
        }
        private static GUIContent _inspectAssetPopup;
        internal static GUIContent InspectAssetPopup
        {
            get
            {
                if (_inspectAssetPopup == null)
                {
                    _inspectAssetPopup = new GUIContent(EditorGUIUtility.IconContent("UnityEditor.InspectorWindow"))
                    {
                        tooltip = "Pop-up this asset in a new Inspector window",
                        text = "+"
                    };
                }
                return _inspectAssetPopup;
            }
        }

        private static GUIContent _saveAsset;
        internal static GUIContent SaveAsset
        {
            get
            {
                if (_saveAsset == null)
                {
                    _saveAsset = new GUIContent(SaveAssetImage)
                    {
                        tooltip = "Save changes to this Asset"
                    };
                }
                return _saveAsset;
            }
        }

        private static int countGOs = 2;
        private static GUIContent _selectionContent;
        internal static GUIContent SelectionContent(int count)
        {
            if (_selectionContent == null || count != countGOs)
            {
                countGOs = count;
                _selectionContent = new GUIContent("(" + count.ToString() + " Objects)");
            }
            return _selectionContent;
        }


        private static GUIContent _closeAsset;
        internal static GUIContent CloseAsset
        {
            get
            {
                if (_closeAsset == null)
                {
                    _closeAsset = new GUIContent(CloseButtonImage)
                    {
                        tooltip = "Close the Asset View"
                    };
                }
                return _closeAsset;
            }
        }

        private static GUIContent _addComponent;
        internal static GUIContent AddComponent
        {
            get
            {
                if (_addComponent == null)
                {
                    _addComponent = new GUIContent("Add Component");
                }
                return _addComponent;
            }
        }

        private static GUIContent _settingsContent;
        internal static GUIContent SettingsContent
        {
            get
            {
                if (_settingsContent == null)
                {
                    _settingsContent = new GUIContent(SettingsButtonImage)
                    {
                        text = "Open Settings"
                    };
                }
                return _settingsContent;
            }
        }

        private static GameObject[] currentMultiSelection;
        private static GUIContent[] _multiSelectionContent;
        internal static GUIContent MultiSelectionContent(GameObject[] selection, int index)
        {

            if (currentMultiSelection == null || _multiSelectionContent == null || selection != currentMultiSelection || index >= _multiSelectionContent.Length)
            {

                currentMultiSelection = selection;
                _multiSelectionContent = new GUIContent[selection.Length];
                for (int i = 0; i < selection.Length; i++)
                {
                    _multiSelectionContent[i] = new GUIContent(selection[i].name)
                    {
                        tooltip = "Click: Select\nMiddle-click: Open in new Tab\nRight-click: More options"
                    };
                }
            }
            if (_multiSelectionContent[index] == null || _multiSelectionContent[index].text != selection[index].name)
            {

                _multiSelectionContent[index] = new GUIContent(selection[index].name)
                {
                    tooltip = "Click: Select\nMiddle-click: Open in new Tab\nRight-click: More options"
                };
            }

            return _multiSelectionContent[index];
        }

        private static GUIContent _tabContent;
        internal static GUIContent TabContent
        {
            get
            {
                if (_tabContent == null)
                {
                    _tabContent = new GUIContent();
                }
                return _tabContent;
            }
        }

        private static GUIContent _tabIconContent;
        internal static GUIContent TabIconContent
        {
            get
            {
                if (_tabIconContent == null)
                {
                    _tabIconContent = new GUIContent();
                }
                return _tabIconContent;
            }
        }

        private static GUIContent _expandCollapseContent2;
        internal static GUIContent ExpandCollapseContent2
        {
            get
            {
                if (_expandCollapseContent2 == null || _expandCollapseContent2.image == null)
                {
                    _expandCollapseContent2 = new GUIContent
                    {
                        image = ExpandButtonImage,
                        tooltip = "Click to expand All Components"
                    };
                }
                return _expandCollapseContent2;
            }
        }

        internal static GUIContent GetExpandCollapseContent(bool allCollapsed)
        {
            return !allCollapsed ? ExpandCollapseContent : ExpandCollapseContent2;
        }
        private static GUIContent _expandCollapseContent;
        internal static GUIContent ExpandCollapseContent
        {
            get
            {
                if (_expandCollapseContent == null || _expandCollapseContent.image == null)
                {
                    _expandCollapseContent = new GUIContent
                    {
                        image = ExpandButtonImage,
                        tooltip = "Click: Collapse All Components\nRight-click: Expand All Components"
                    };
                }
                return _expandCollapseContent;
            }
        }

        private static GUIContent _historyButton;
        internal static GUIContent HistoryButton
        {
            get
            {
                if (_historyButton == null || _historyButton.image == null)
                {
                    _historyButton = new GUIContent
                    {
                        image = HistoryButtonImage,
                        tooltip = "Click: Show Asset History\nRight-click: Go back to previous Asset"
                    };
                }
                return _historyButton;
            }
        }
        private static GUIContent _pathContent;
        internal static GUIContent PathContent(string tooltip)
        {
            if (_pathContent == null)
            {
                _pathContent = new GUIContent();
            }
            if (tooltip != "")
            {
                tooltip = "Click to Ping the Asset\nRight-click to copy path";

            }
            if (VisualElementExtensions.activeFloatingTexts != null && VisualElementExtensions.activeFloatingTexts.Count > 0)
            {
                tooltip = "";
            }
            _pathContent.tooltip = tooltip;
            return _pathContent;
        }
        private static GUIContent _selectForTools;
        internal static GUIContent SelectForTools
        {
            get
            {
                if (_selectForTools == null)
                {
                    _selectForTools = new GUIContent("<i>Select to use all Scene Tools</i>  <b>[?]</b>")
                    {
                        tooltip = "Some tools require the target to be selected in the Hierarchy"
                    };
                }
                return _selectForTools;
            }
        }

        private static GUIContent _tabMulti;
        internal static GUIContent TabMulti
        {
            get
            {
                if (_tabMulti == null)
                {
                    _tabMulti = new GUIContent(MultiTabButtonImage)
                    {
                        tooltip = "Open in new tab"
                    };
                }
                return _tabMulti;
            }
        }
        private static GUIContent _assetLocked;
        internal static GUIContent AssetLocked
        {
            get
            {
                if (_assetLocked == null)
                {
                    if (EditorGUIUtility.isProSkin)
                    {
                        _assetLocked = new GUIContent(EditorGUIUtility.IconContent("IN LockButton on"));
                    }
                    else
                    {
                        _assetLocked = new GUIContent(LockONButtonImage);
                    }

                    _assetLocked.tooltip = "Unlock Asset Inspection";
                }
                return _assetLocked;
            }
        }
        private static GUIContent _assetUnlocked;
        internal static GUIContent AssetUnlocked
        {
            get
            {
                if (_assetUnlocked == null)
                {
                    if (EditorGUIUtility.isProSkin)
                    {
                        _assetUnlocked = new GUIContent(EditorGUIUtility.IconContent("IN LockButton"))
                        {
                            tooltip = "Lock Asset Inspection"
                        };
                    }
                    else
                    {
                        _assetUnlocked = new GUIContent(LockOFFButtonImage)
                        {
                            tooltip = "Lock Asset Inspection"
                        };
                    }
                }
                return _assetUnlocked;
            }
        }

        internal static void DrawCustomButton(bool subtle = false, bool border = true, bool square = false)
        {
            Rect rect = GUILayoutUtility.GetLastRect();
            DrawCustomButton(rect, subtle, border, square);
        }
        internal static void DrawCustomButton(Rect rect, bool subtle = false, bool border = true, bool square = false)
        {
            int padding = 1;
            if (square)
            {
                padding = 0;
            }
            Rect lineRect = new Rect(rect.x + 1, rect.y, rect.width - 2, rect.height);
            EditorUtils.DrawLineOverRect(lineRect, CustomColors.HarderBright, -padding);
            if (!subtle)
            {
                EditorUtils.DrawLineOverRect(lineRect, CustomColors.SimpleBright, -padding, 1);
            }
            else
            {
                EditorUtils.DrawLineOverRect(rect, CustomColors.SubtleBright, -padding, 1);
            }

            EditorUtils.DrawLineOverRect(lineRect, CustomColors.SubtleBright, -padding, 3);
            EditorUtils.DrawLineOverRect(lineRect, CustomColors.SubtleBright, -padding, 6);
            EditorUtils.DrawLineOverRect(lineRect, CustomColors.SubtleBright, -padding, 13);
            if (border)
            {
                if (square)
                {
                    EditorUtils.DrawOutsideRectBorder(rect, CustomColors.MediumShadow, 1);
                }
                else
                {
                    EditorUtils.DrawRectBorder(rect, CustomColors.MediumShadow, 1);
                }

            }
        }
        private static GUIContent _editContentDefault;
        private static GUIContent _editContentPopup;
        private static GUIContent _contentWithIcon;
        private static GUIContent _selectNowIcon;
        internal static GUIContent SelectNowIcon
        {
            get
            {
                if (_selectNowIcon == null)
                {
                    _selectNowIcon = new GUIContent(EditorGUIUtility.IconContent("Grid.BoxTool"))
                    {
                        tooltip = "Select now in Hierarchy"
                    };
                }
                return _selectNowIcon;
            }
        }
        private static GUIContent _contentWithLockIcon;
        internal static GUIContent ContentWithIcon
        {
            get
            {
                if (_contentWithIcon == null)
                {
                    _contentWithIcon = new GUIContent();
                }
                CustomGUIStyles.DraggingTabs.padding = new RectOffset(15, 0, 0, 0);
                return _contentWithIcon;
            }
        }


        private static GameObject _lastHover;
        private static GameObject[] _lastMultiHover;
        private static string _lastHoverName;
        private static string _lastMultiHoverName;

        internal static string GetSingleHoverName(GameObject go)
        {
            if (_lastHover != go || _lastHoverName == null)
            {
                _lastHover = go;
                string path = "";
                string _spacing = "           ";
                Transform parent = go.transform;
                for (int j = 4; j >= 0; j--)
                {
                    if (parent != null)
                    {
                        if (j == 4)
                        {
                            path = parent.name;
                            _spacing = _spacing.Remove(_spacing.Length - 2);
                        }
                        else
                        {
                            _spacing = _spacing.Remove(_spacing.Length - 2);
                            path = "<color=#455050>" + parent.name + "</color>" + "\n" + _spacing + "<b>∟</b>" + path;
                        }
                        parent = parent.parent;
                        if (parent != null && j == 4)
                        {
                            path = "<b>" + path + "</b>";
                        }
                    }
                }
                _lastHoverName = path;
            }
            return _lastHoverName;
        }

        internal static string GetMultiHoverName(GameObject[] go)
        {
            if (_lastMultiHover != go || _lastMultiHoverName == null)
            {
                string text = "";
                _lastMultiHover = go;
                if (go == null || go.Length == 0)
                {
                    //      Debug.Log("Null objects");                   
                }

                for (int u = 0; u < go.Length; u++)
                {
                    var GO = go[u];
                    if (GO == null)
                    {
                        continue;
                    }
                    if (u == go.Length - 1)
                    {
                        text += u + 1 + ".  " + GO.name;
                    }
                    else
                    {
                        text += u + 1 + ".  " + GO.name + "\n";
                    }
                }
                _lastMultiHoverName = text;
            }
            return _lastMultiHoverName;
        }

        private static GUIContent _plusSymbolContent;
        internal static Vector2 plusSize;
        internal static GUIContent PlusSymbolContent
        {
            get
            {
                if (_plusSymbolContent == null)
                {
                    GUIContent plusContent = new GUIContent("+");
                    plusSize = CustomGUIStyles.PlusStyle.CalcSize(plusContent);
                    _plusSymbolContent = new GUIContent("+");
                }
                return _plusSymbolContent;
            }
        }

        private static GUIContent _showSavedSessionsButton;
        private static GUIContent _hideSavedSessionsButton;

        internal static GUIContent GetSavedSessionsButton(bool showSessions)
        {
            if (showSessions)
            {
                if (_hideSavedSessionsButton == null)
                {
                    _hideSavedSessionsButton = new GUIContent("Hide Saved Sessions", SaveAsset.image);
                }
                return _hideSavedSessionsButton;
            }
            else
            {
                if (_showSavedSessionsButton == null)
                {
                    _showSavedSessionsButton = new GUIContent("Show Saved Sessions", SaveAsset.image);
                }
                return _showSavedSessionsButton;
            }
        }

        private static GUIContent _resetToDefaultContent;
        internal static GUIContent ResetToDefaultContent
        {
            get
            {
                if (_resetToDefaultContent == null)
                {
                    _resetToDefaultContent = new GUIContent
                    {
                        image = HistoryButtonImage,
                        text = " Reset to Default "
                    };
                }
                return _resetToDefaultContent;
            }
        }

        internal static GUIContent ContentWithLockIcon
        {
            get
            {
                if (_contentWithLockIcon == null)
                {
                    if (EditorGUIUtility.isProSkin)
                    {
                        _contentWithLockIcon = new GUIContent
                        {
                            image = EditorGUIUtility.IconContent("IN LockButton On").image
                        };
                    }
                    else
                    {
                        _contentWithLockIcon = new GUIContent
                        {
                            image = LockONButtonImage
                        };
                    }

                }
                //   CustomGUIStyles.DraggingTabs.padding = new RectOffset(0, 0, 0, 0);
                return _contentWithLockIcon;
            }
        }

        private static GUIContent _showImport;
        internal static GUIContent ShowImport
        {
            get
            {
                if (_showImport == null)
                {
                    _showImport = new GUIContent(EditorGUIUtility.IconContent("Preset.Context"))
                    {
                        tooltip = "Show All Import Settings"
                    };
                }
                return _showImport;
            }
        }
        private static GUIContent _showHeader;
        internal static GUIContent ShowHeader
        {
            get
            {
                if (_showHeader == null)
                {
                    _showHeader = new GUIContent(EditorGUIUtility.IconContent("Preset.Context"))
                    {
                        tooltip = "Show Prefab Header"
                    };
                }
                return _showHeader;
            }
        }
        private static GUIContent _hideHeader;
        internal static GUIContent HideHeader
        {
            get
            {
                if (_hideHeader == null)
                {
                    _hideHeader = new GUIContent(EditorGUIUtility.IconContent("Preset.Context"))
                    {
                        tooltip = "Hide Prefab Header"
                    };
                }
                return _hideHeader;
            }
        }
        private static GUIContent _prefabIcon;
        internal static GUIContent PrefabIcon
        {
            get
            {
                if (_prefabIcon == null)
                {
                    _prefabIcon = new GUIContent(EditorGUIUtility.IconContent("Prefab Icon"))
                    {
                        tooltip = "Prefab"
                    };
                }
                return _prefabIcon;
            }
        }

        private static GUIContent _importedObjectIcon;
        internal static GUIContent ImportedIcon
        {
            get
            {
                if (_importedObjectIcon == null)
                {
                    _importedObjectIcon = new GUIContent(EditorGUIUtility.IconContent("PrefabModel Icon"))
                    {
                        tooltip = "Imported Object"
                    };
                }
                return _importedObjectIcon;
            }
        }

        private static GUIContent _hideImport;
        internal static GUIContent HideImport
        {
            get
            {
                if (_hideImport == null)
                {
                    _hideImport = new GUIContent(EditorGUIUtility.IconContent("Preset.Context"))
                    {
                        tooltip = "Hide Import Settings"
                    };
                }
                return _hideImport;
            }
        }

        private static GUIContent _defaultAsset;
        internal static GUIContent DefaultAsset
        {
            get
            {
                if (_defaultAsset == null)
                {
                    _defaultAsset = new GUIContent(EditorGUIUtility.IconContent("DefaultAsset Icon"));
                }
                return _defaultAsset;
            }
        }
        private static string _comparisonString;
        private static string _fullString;
        private static GUIContent _welcomeMessage;
        internal static GUIContent WelcomeMessage(string comparisonString)
        {
            if (_welcomeMessage == null || _comparisonString != comparisonString)
            {
                string colorCode = "33AEDB";
                if (!EditorGUIUtility.isProSkin)
                {
                    colorCode = "007ACC";
                }
                _comparisonString = comparisonString;
                _welcomeMessage = new GUIContent("<b>Hey!</b>\n\nWe found <b><color=#" + colorCode + ">" + comparisonString + " </color></b>from your last time in this scene.\n\n<b>Continue where you left off?</b>");
            }
            return _welcomeMessage;
        }

        internal static string FullString
        {
            get { return _fullString; }
            set { _fullString = value; }
        }

        private static GUIContent _newTab;
        internal static GUIContent NewTab
        {
            get
            {
                if (_newTab == null)
                {
                    _newTab = new GUIContent(EditorGUIUtility.IconContent("winbtn_mac_max_a"));
                }
                return _newTab;
            }
        }

        internal static GUIContent AddContent
        {
            get
            {
                if (_addContent == null)
                {
                    _addContent = new GUIContent(NewTabButtonImage)
                    {
                        tooltip = "Click: Add new Tab\nRight/Middle click: Add new Tab at the end"
                    };
                }
                return _addContent;
            }
        }


        private static GUIContent _addContent;

        internal static GUIContent AddComponentContent
        {
            get
            {
                if (_addComponentContent == null)
                {
                    _addComponentContent = new GUIContent(NewTabButtonImage)
                    {
                        text = " <b> Component </b>  ",
                        tooltip = "Add Component"
                    };
                }
                return _addComponentContent;
            }
        }

        private static GUIContent _addComponentContent;
        private static GUIContent _forwardContent;
        private static GUIContent _backContent;
        internal static GUIContent ForwardContent
        {
            get
            {
                if (_forwardContent == null)
                {

                    _forwardContent = new GUIContent(HistoryNextButtonImage)
                    {
                        tooltip = "Go forward"
                    };

                }
                return _forwardContent;
            }
        }

        internal static GUIContent BackContent
        {
            get
            {
                if (_backContent == null)
                {
                    _backContent = new GUIContent(HistoryBackButtonImage)
                    {
                        tooltip = "Go back"
                    };
                }
                return _backContent;
            }
        }
    }
}