using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace CoInspector
{

    internal static class CustomGUIStyles
    {
        static GUIStyle objectListLabel;

        internal static GUIStyle ObjectListLabel
        {
            get
            {
                if (objectListLabel == null)
                {
                    objectListLabel = new GUIStyle(EditorStyles.label)
                    {
                        alignment = TextAnchor.MiddleCenter,
                        wordWrap = true,
                        fontSize = 9
                    };
                }
                return objectListLabel;
            }
        }

        static GUIStyle richLabel;
        internal static GUIStyle RichLabel
        {
            get
            {
                if (richLabel == null)
                {
                    richLabel = new GUIStyle(EditorStyles.label)
                    {
                        richText = true,
                        wordWrap = true,
                        fontSize = 11
                    };
                }
                return richLabel;
            }
        }



        static GUIStyle _inspectorSectionStyle;
        internal static GUIStyle InspectorSectionStyle
        {
            get
            {
                if (_inspectorSectionStyle == null)
                {
                    _inspectorSectionStyle = new GUIStyle(EditorStyles.toolbar)
                    {
                        fixedHeight = 0,
                        fixedWidth = 0,
                        margin = new RectOffset(0, 0, 0, 0),
                        padding = new RectOffset(0, 0, 0, 0),
                        contentOffset = new Vector2(0, 0),
                    };
                }
                return _inspectorSectionStyle;
            }
        }

        static GUIStyle _inspectorButtonStyle;
        internal static GUIStyle InspectorButtonStyle
        {
            get
            {
                if (_inspectorButtonStyle == null)
                {
                    _inspectorButtonStyle = new GUIStyle(AssetToolbarButton)
                    {
                        fixedHeight = 0,
                        fixedWidth = 0,
                        margin = new RectOffset(0, 0, 0, 0),
                        padding = new RectOffset(0, 0, 0, 0),
                        contentOffset = new Vector2(0, 0),
                    };
                }
                return _inspectorButtonStyle;
            }
        }


        static GUIStyle assetToolbarButton;
        internal static GUIStyle AssetToolbarButton
        {
            get
            {
                if (assetToolbarButton == null)
                {
                    assetToolbarButton = new GUIStyle(EditorToolbarButton)
                    {
                        overflow = new RectOffset(0, 0, 0, 0),
                        fixedHeight = 24,
                        contentOffset = new Vector2(1, 0),

                        padding = new RectOffset(0, 0, 0, 0)
                    };
                }
                return assetToolbarButton;

            }
        }

        static GUIStyle transformHistoryButton;
        internal static GUIStyle TransformHistoryButton
        {
            get
            {
                if (transformHistoryButton == null)
                {
                    transformHistoryButton = new GUIStyle(EditorToolbarButton)
                    {
                        overflow = new RectOffset(0, 0, 0, 0),
                        contentOffset = new Vector2(0, -2),
                        fixedHeight = 22,
                        padding = new RectOffset(0, 0, 0, 0)
                    };
                }
                if (!EditorUtils.EditorScaleIsOne())
                {
                    transformHistoryButton.fixedHeight = 21;
                    transformHistoryButton.contentOffset = new Vector2(0, -1);
                }
                return transformHistoryButton;
            }
        }
        static GUIStyle noMarginButton;
        internal static GUIStyle NoMarginButton
        {
            get
            {
                if (noMarginButton == null)
                {
                    noMarginButton = new GUIStyle(GUI.skin.button)
                    {
                        overflow = new RectOffset(0, 0, 0, 0),
                        padding = new RectOffset(2, 2, 2, 2)
                    };
                }
                return noMarginButton;

            }
        }

        static GUIStyle _miniLabel;
        internal static GUIStyle MiniLabel
        {
            get
            {
                if (_miniLabel == null)
                {
                    _miniLabel = new GUIStyle(EditorStyles.miniLabel)
                    {
                        contentOffset = new Vector2(0, 2)
                    };

                }
                return _miniLabel;
            }
        }
        static GUIStyle _richMiniLabel;
        internal static GUIStyle RichMiniLabel
        {
            get
            {
                if (_richMiniLabel == null)
                {
                    _richMiniLabel = new GUIStyle(EditorStyles.miniLabel)
                    {
                        contentOffset = new Vector2(7, 0),
                        richText = true,
                        wordWrap = false,
                        fontSize = 11
                    };

                }
                return _richMiniLabel;
            }
        }
        static GUIStyle filterComponentLabel;
        internal static GUIStyle FilterComponentLabel
        {
            get
            {
                if (filterComponentLabel == null)
                {
                    filterComponentLabel = new GUIStyle(EditorStyles.miniLabel)
                    {
                        contentOffset = new Vector2(0, 0),
                        richText = true,
                        wordWrap = false,
                        fontSize = 10
                    };
                }
                return filterComponentLabel;
            }
        }

        static GUIStyle settingsLabel;
        internal static GUIStyle SettingsLabel
        {
            get
            {
                if (settingsLabel == null)
                {
                    settingsLabel = new GUIStyle(EditorStyles.label)
                    {
                        richText = true,
                        contentOffset = new Vector2(0, 2)
                    };
                }
                return settingsLabel;
            }
        }
        static GUIStyle settingsSubLabel;
        internal static GUIStyle SettingsSubLabel
        {
            get
            {
                if (settingsSubLabel == null)
                {
                    settingsSubLabel = new GUIStyle(EditorStyles.label)
                    {
                        richText = true,
                        contentOffset = new Vector2(0, -1)
                    };
                }
                return settingsSubLabel;
            }
        }

        static GUIStyle settingsFoldout;
        internal static GUIStyle SettingsFoldout
        {
            get
            {
                if (settingsFoldout == null)
                {
                    settingsFoldout = new GUIStyle(EditorStyles.foldout)
                    {
                        fixedWidth = 10,
                        padding = new RectOffset(0, 0, 0, 0),
                        margin = new RectOffset(0, 0, 0, 0)
                    };
                }
                return settingsFoldout;
            }
        }
        static GUIStyle justFoldoutStyle;
        internal static GUIStyle JustFoldoutStyle
        {
            get
            {
                if (justFoldoutStyle == null)
                {
                    justFoldoutStyle = new GUIStyle(EditorStyles.foldout)
                    {
                        // fixedWidth = 10,
                        padding = new RectOffset(0, 0, 0, 0),
                        margin = new RectOffset(0, 0, 1, 0),
                        contentOffset = new Vector2(0, 0)
                    };
                }
                return justFoldoutStyle;
            }
        }

        private static GUIStyle bigBoldLabel;
        internal static GUIStyle BigBoldLabel
        {
            get
            {
                if (bigBoldLabel == null)
                {
                    bigBoldLabel = new GUIStyle(EditorStyles.boldLabel)
                    {
                        fontSize = 15
                    };
                }
                return bigBoldLabel;
            }
        }
        private static GUIStyle headerLabel;
        internal static GUIStyle HeaderLabel
        {
            get
            {
                if (headerLabel == null)
                {
                    headerLabel = new GUIStyle(EditorStyles.boldLabel)
                    {
                        alignment = TextAnchor.MiddleCenter,
                        richText = true,
                        fontSize = 18
                    };
                }
                return headerLabel;
            }
        }
        private static GUIStyle mediumLabel;
        internal static GUIStyle MediumLabel
        {
            get
            {
                if (mediumLabel == null)
                {
                    mediumLabel = new GUIStyle(EditorStyles.label)
                    {
                        fontSize = 13
                    };
                }
                return mediumLabel;
            }
        }

        internal static GUIStyle editorToolbarButton;
        internal static GUIStyle EditorToolbarButton
        {
            get
            {
                if (editorToolbarButton == null)
                {
                    editorToolbarButton = GUI.skin.FindStyle("toolbarbutton");
                }
                return editorToolbarButton;
            }
        }
        internal static void StartBoxSection()
        {
            EditorGUILayout.BeginVertical(BoxStyle);
        }

        internal static void EndBoxSection()
        {
            EditorGUILayout.EndVertical();
            EditorUtils.DrawRectBorder(GUILayoutUtility.GetLastRect(), CustomColors.MediumShadow, 1);
            EditorUtils.DrawLineUnderRect(CustomColors.SimpleBright, 0);
        }

        private static GUIStyle customHelpBoxStyle;
        internal static GUIStyle CustomHelpBoxStyle
        {
            get
            {
                if (customHelpBoxStyle == null)
                {
                    customHelpBoxStyle = new GUIStyle(GUI.skin.GetStyle("helpbox"))
                    {
                        margin = new RectOffset(15, 8, 0, 0)
                    };
                }
                return customHelpBoxStyle;
            }
        }

        private static GUIStyle customButtonStyle;
        internal static GUIStyle CustomButtonStyle
        {
            get
            {
                if (customButtonStyle == null)
                {
                    customButtonStyle = new GUIStyle(GUI.skin.button)
                    {
                        padding = new RectOffset(0, 0, 4, 4),
                        fixedWidth = 40,
                        fixedHeight = 24
                    };
                }
                return customButtonStyle;
            }
        }

        private static GUIStyle helpBoxTextStyle;
        internal static GUIStyle HelpBoxTextStyle
        {
            get
            {
                if (helpBoxTextStyle == null)
                {
                    helpBoxTextStyle = new GUIStyle(EditorStyles.label)
                    {
                        padding = new RectOffset(12, 10, 10, 10),
                        contentOffset = new Vector2(0, 0),
                        wordWrap = true,
                        richText = true,
                        fontSize = 11
                    };
                }
                return helpBoxTextStyle;
            }
        }

        private static GUIStyle bigHelpBoxTextStyle;
        internal static GUIStyle BigHelpBoxTextStyle
        {
            get
            {
                if (bigHelpBoxTextStyle == null)
                {
                    bigHelpBoxTextStyle = new GUIStyle(HelpBoxTextStyle)
                    {
                        fontSize = 12
                    };
                }
                return bigHelpBoxTextStyle;
            }
        }

        private static GUIStyle helpBoxIconStyle;
        internal static GUIStyle HelpBoxIconStyle
        {
            get
            {
                if (helpBoxIconStyle == null)
                {
                    helpBoxIconStyle = new GUIStyle(EditorStyles.label)
                    {
                        padding = new RectOffset(0, 0, 0, 0),
                        alignment = TextAnchor.MiddleCenter,
                        contentOffset = new Vector2(0, 0),
                        stretchHeight = true,
                        margin = new RectOffset(10, 0, 0, 0),
                    };
                }
                return helpBoxIconStyle;
            }
        }

        private static GUIStyle settingsBoldLabel;
        internal static GUIStyle SettingsBoldLabel
        {
            get
            {
                if (settingsBoldLabel == null)
                {
                    settingsBoldLabel = new GUIStyle(EditorStyles.boldLabel)
                    {
                        fontSize = 13
                    };
                }
                return settingsBoldLabel;
            }
        }

        static GUIStyle assetToolbarButton_Open;
        internal static GUIStyle AssetToolbarButton_Open
        {
            get
            {
                if (assetToolbarButton_Open == null)
                {
                    assetToolbarButton_Open = new GUIStyle(EditorToolbarButton)
                    {
                        overflow = new RectOffset(0, 0, 0, 0),
                        fixedHeight = 23,
#if UNITY_2023_1_OR_NEWER
                        fontSize = 8,
#else
                        fontSize = 9,
#endif
                        fontStyle = FontStyle.Bold,
                        contentOffset = new Vector2(-1, 0),

                        padding = new RectOffset(0, 1, 5, 4)
                    };
                }
                return assetToolbarButton_Open;

            }
        }
        private static GUIStyle _goNameStyle;
        internal static GUIStyle GOnameStyle
        {
            get
            {
                if (_goNameStyle == null)
                {
                    _goNameStyle = new GUIStyle(EditorStyles.textField)
                    {
                        fontStyle = FontStyle.Bold
                    };
                }
                return _goNameStyle;
            }
        }

        private static GUIStyle _boxStyle;
        internal static GUIStyle BoxStyle
        {
            get
            {
                if (_boxStyle == null)
                {
                    _boxStyle = new GUIStyle(GUI.skin.box)
                    {
                        richText = true,
                        alignment = TextAnchor.MiddleLeft,
                        fontSize = 12,
                        padding = new RectOffset(10, 15, 5, 5),
                        border = new RectOffset(2, 2, 2, 2)
                    };
                }
                return _boxStyle;
            }
        }
        private static GUIStyle _componentBoxStyle;
        internal static GUIStyle ComponentBoxStyle
        {
            get
            {
                if (_componentBoxStyle == null)
                {
                    _componentBoxStyle = new GUIStyle(GUI.skin.box)
                    {
                        richText = true,
                        alignment = TextAnchor.MiddleCenter,
                        fontSize = 12,
                        padding = new RectOffset(4, 0, 4, 4),
                        border = new RectOffset(2, 2, 2, 2)
                    };
                }
                return _componentBoxStyle;
            }
        }

        private static GUIStyle _boxStylePadding;
        internal static GUIStyle BoxStylePadding
        {
            get
            {
                if (_boxStylePadding == null)
                {
                    _boxStylePadding = new GUIStyle(GUI.skin.box)
                    {
                        richText = true,
                        alignment = TextAnchor.MiddleLeft,
                        fontSize = 12,
                        padding = new RectOffset(10, 15, 5, 5),
                        margin = new RectOffset(10, 10, 0, 0),
                        border = new RectOffset(2, 2, 2, 2)
                    };
                }
                return _boxStylePadding;
            }
        }
        private static GUIStyle _boldLabel;
        internal static GUIStyle BoldLabel
        {
            get
            {
                if (_boldLabel == null)
                {
                    _boldLabel = new GUIStyle(EditorStyles.label)
                    {
                        fontStyle = FontStyle.Bold,
                        contentOffset = new Vector2(0, 0),

                    };
                }
                return _boldLabel;
            }
        }

        internal static GUIStyle ButtonsUpRight
        {
            get
            {
                if (_buttonsUpRight == null)
                {
                    _buttonsUpRight = new GUIStyle(IconButton)
                    {
                        fontSize = 11,
                        fixedHeight = 20,
                        fixedWidth = 20,
                        stretchWidth = true,
                        fontStyle = FontStyle.Bold,
                        alignment = TextAnchor.MiddleCenter,
                    };
                }
                return _buttonsUpRight;
            }
        }
        static GUIStyle _buttonsUpLeft;
        internal static GUIStyle ButtonsUpLeft
        {
            get
            {
                if (_buttonsUpLeft == null)
                {
                    _buttonsUpLeft = new GUIStyle(IconButton)
                    {
                        fontSize = 11,
                        stretchWidth = true,
                        fontStyle = FontStyle.Bold,
                        alignment = TextAnchor.MiddleCenter,
                    };
                }
                return _buttonsUpLeft;
            }
        }
        private static GUIStyle _filterButtonStyle;
        internal static GUIStyle FilterButtonStyle
        {
            get
            {
                if (_filterButtonStyle == null)
                {
                    _filterButtonStyle = new GUIStyle(IconButton)
                    {
                        fontSize = 11,
                        fixedHeight = 20,
                        fixedWidth = 13,
                        stretchWidth = true,
                        fontStyle = FontStyle.Bold,
                        alignment = TextAnchor.MiddleCenter,
                        padding = new RectOffset(0, 0, 0, 0),
                        border = new RectOffset(0, 0, 0, 0),
                        contentOffset = new Vector2(0, 0),
                        margin = new RectOffset(0, 0, 0, 0),
                    };
                }
                return _filterButtonStyle;
            }
        }

        private static GUIStyle _filterStyle;
        internal static GUIStyle FilterStyle
        {
            get
            {
                if (_filterStyle == null)
                {
                    _filterStyle = new GUIStyle(EditorStyles.miniTextField)
                    {
                        fontSize = 11,
                        fixedHeight = 14,
                        padding = new RectOffset(15, 0, 0, 0),
                        contentOffset = new Vector2(0, 0)
                    };
                }
                return _filterStyle;
            }
        }

        private static GUIStyle _emptyButton;
        internal static GUIStyle EmptyButton
        {
            get
            {
                if (_emptyButton == null)
                {
                    _emptyButton = new GUIStyle(IconButton)
                    {
                        fontSize = 11,
                        stretchWidth = true,
                        fontStyle = FontStyle.Bold,
                        alignment = TextAnchor.MiddleCenter,
                        padding = new RectOffset(0, 0, 0, 0),
                    };
                }
                return _emptyButton;
            }
        }


        private static GUIStyle logoStyle;
        internal static GUIStyle LogoStyle
        {
            get
            {
                if (logoStyle == null)
                {
                    logoStyle = new GUIStyle(EditorStyles.label)
                    {
                        fontSize = 12,
                        richText = true,
                        wordWrap = true,
                        alignment = TextAnchor.MiddleCenter

                    };
                }

                return logoStyle;
            }
        }

        internal static void InfoLabel(string text)
        {
            EditorGUILayout.BeginHorizontal();
            GUIStyle wrapStyle = WrapLabelStyle;
            int indent = EditorGUI.indentLevel;
            EditorGUI.indentLevel = 2;
            EditorGUILayout.LabelField(text, wrapStyle);
            EditorGUI.indentLevel = indent;
            EditorGUILayout.EndHorizontal();
            Rect rect = EditorUtils.GetLastLineRect();
            EditorUtils.DrawLineOverRect(rect, 2);
            EditorUtils.DrawLineOverRect(rect, CustomColors.MediumShadow, 3);
            EditorUtils.DrawLineUnderRect(rect, CustomColors.HardShadow, 2);
            rect.x += 10;
            rect.y -= 1;
            rect.width = 17;
            GUI.Label(rect, CustomGUIContents.InfoButtonImage);
        }

        internal static void InfoLabel(string text, VisualElement root)
        {
            EditorGUILayout.BeginHorizontal();
            GUIStyle wrapStyle = WrapLabelStyle;
            int indent = EditorGUI.indentLevel;
            EditorGUI.indentLevel = 2;
            EditorGUILayout.LabelField(text, wrapStyle);
            EditorGUI.indentLevel = indent;
            EditorGUILayout.EndHorizontal();
            Rect rect = EditorUtils.GetLastLineRect();
            if (root != null)
            {
                rect.width = root.layout.width;
            }
            EditorUtils.DrawLineOverRect(rect, 2);
            EditorUtils.DrawLineOverRect(rect, CustomColors.MediumShadow, 3);
            rect.x += 10;
            rect.y -= 1;
            rect.width = 17;
            GUI.Label(rect, CustomGUIContents.InfoButtonImage);

        }

        internal static void HelpBox(string text, bool centered = false, bool big = false)
        {
            if (centered)
            {

                HelpBoxTextStyle.alignment = TextAnchor.MiddleCenter;
            }
            else
            {

                HelpBoxTextStyle.alignment = TextAnchor.MiddleLeft;
            }
            if (big)
            {
                HelpBoxTextStyle.fontSize = 12;
            }
            else
            {
                HelpBoxTextStyle.fontSize = 11;
            }
            bool isPro = EditorGUIUtility.isProSkin;
            Color color = GUI.backgroundColor;
            if (!isPro)
            {
                GUI.backgroundColor = CustomColors.DarkInspector;
            }
            EditorGUILayout.BeginHorizontal(CustomHelpBoxStyle);
            GUILayout.Label(CustomGUIContents.InfoButtonImage, HelpBoxIconStyle, GUILayout.Width(25));
            Rect lastRect = GUILayoutUtility.GetLastRect();
            lastRect.width = 1;
            lastRect.x += 30;
            lastRect.height -= 10;
            lastRect.y += 5;
            float multiplier = 1.5f;
            if (!isPro)
            {
                multiplier = 1.1f;
            }
            EditorGUI.DrawRect(lastRect, CustomColors.CustomGreen / multiplier);
            GUILayout.Label(text, HelpBoxTextStyle);
            EditorGUILayout.EndHorizontal();
            GUI.backgroundColor = color;
            lastRect = GUILayoutUtility.GetLastRect();
            EditorUtils.DrawLineOverRect(lastRect, CustomColors.SimpleBright, -1);
            EditorUtils.DrawRectBorder(lastRect, CustomColors.MediumShadow, 1);
        }

        internal static void HelpBox(GUIContent text, bool centered = false)
        {
            if (centered)
            {
                HelpBoxTextStyle.fontSize = 12;
                HelpBoxTextStyle.alignment = TextAnchor.MiddleCenter;
            }
            else
            {
                HelpBoxTextStyle.fontSize = 11;
                HelpBoxTextStyle.alignment = TextAnchor.MiddleLeft;
            }
            bool isPro = EditorGUIUtility.isProSkin;
            Color color = GUI.backgroundColor;
            if (!isPro)
            {
                GUI.backgroundColor = CustomColors.DarkInspector;
            }
            EditorGUILayout.BeginHorizontal(CustomHelpBoxStyle);
            GUILayout.Label(CustomGUIContents.InfoButtonImage, HelpBoxIconStyle, GUILayout.Width(25));
            Rect lastRect = GUILayoutUtility.GetLastRect();
            lastRect.width = 1;
            lastRect.x += 30;
            lastRect.height -= 10;
            lastRect.y += 5;
            float multiplier = 1.5f;
            if (!isPro)
            {
                multiplier = 1.1f;
            }
            EditorGUI.DrawRect(lastRect, CustomColors.CustomGreen / multiplier);
            GUILayout.Label(text, HelpBoxTextStyle);
            EditorGUILayout.EndHorizontal();
            GUI.backgroundColor = color;

            lastRect = GUILayoutUtility.GetLastRect();
            EditorUtils.DrawLineOverRect(lastRect, CustomColors.SimpleBright, -1);
            EditorUtils.DrawRectBorder(lastRect, CustomColors.MediumShadow, 1);
        }

        private static GUIStyle _iconButton;
        internal static GUIStyle IconButton
        {
            get
            {
                if (_iconButton == null)
                {
                    _iconButton = new GUIStyle(GUI.skin.FindStyle("IconButton"));
                }
                return _iconButton;
            }
        }

        private static Texture2D CreateBackgroundColorTexture(Color color)
        {
            Texture2D texture = new Texture2D(1, 1);
            texture.SetPixel(0, 0, color);
            texture.Apply();
            return texture;
        }



        private static GUIStyle _buttonsUpRightWide;
        internal static GUIStyle ButtonsUpRight_Wide
        {
            get
            {
                if (_buttonsUpRightWide == null)
                {

                    _buttonsUpRightWide = new GUIStyle(IconButton)
                    {
                        fontSize = 11,
                        fixedHeight = 20,
                        fixedWidth = 30,

                        fontStyle = FontStyle.Bold,
                        alignment = TextAnchor.MiddleCenter,
                        padding = new RectOffset(0, 0, 0, 0),
                        border = new RectOffset(0, 0, 0, 0),
                    };
                }
                return _buttonsUpRightWide;
            }
        }

        private static GUILayoutOption[] _toolBarOptions;
        private static float currentWidth = 0;
        internal static GUILayoutOption[] ToolBarOptions(float width)
        {
            if (_toolBarOptions == null || currentWidth != width)
            {
                currentWidth = width;
                _toolBarOptions = new GUILayoutOption[]
                {
                    GUILayout.Height(25),
                    GUILayout.ExpandWidth(true),
                    GUILayout.MaxWidth(width - 24)
                };
            }
            return _toolBarOptions;
        }

        private static float _lastUserHeight;
        private static GUILayoutOption[] _userHeightOptions;

        internal static GUILayoutOption[] GetUserHeightOptions(float userHeight)
        {
            if (_lastUserHeight != userHeight || _userHeightOptions == null)
            {
                _userHeightOptions = new GUILayoutOption[2]
                {
                    GUILayout.Height(userHeight), GUILayout.ExpandHeight(true)
                };
                _lastUserHeight = userHeight;
            }
            return _userHeightOptions;
        }

        private static GUIStyle _boldFoldoutStyle;
        internal static GUIStyle BoldFoldoutStyle
        {
            get
            {
                if (_boldFoldoutStyle == null)
                {
                    _boldFoldoutStyle = new GUIStyle(EditorStyles.foldout)
                    {
                        fontStyle = FontStyle.Bold
                    };
                }
                return _boldFoldoutStyle;
            }
        }

        private static float _lastMultiWidth;
        private static GUIStyle _buttonsStyle;
        internal static GUIStyle MultiAssetButtonsStyle(float width)
        {
            if (_buttonsStyle == null || currentWidth != _lastMultiWidth)
            {
                _buttonsStyle = new GUIStyle(IconButton)
                {
                    stretchHeight = true,
                    stretchWidth = true,
                    fixedWidth = width - 40
                };
                _lastMultiWidth = width;
            }
            return _buttonsStyle;
        }


        private static GUIStyle _multiFoldoutStyle;
        internal static GUIStyle MultiFoldoutStyle
        {
            get
            {
                if (_multiFoldoutStyle == null)
                {
                    GUIStyle linkStyle = new GUIStyle(GUI.skin.FindStyle("LinkLabel"));
                    _multiFoldoutStyle = new GUIStyle(CustomGUIStyles.RichMiniLabel)
                    {
                        padding = new RectOffset(0, 0, 0, 0)
                    };
                    _multiFoldoutStyle.normal.textColor = linkStyle.normal.textColor;
                    _multiFoldoutStyle.hover.textColor = linkStyle.normal.textColor * 1.15f;
                    _multiFoldoutStyle.richText = true;
                    _multiFoldoutStyle.wordWrap = false;
                }
                return _multiFoldoutStyle;
            }
        }


        private static GUIStyle _plusStyle;
        internal static GUIStyle PlusStyle
        {
            get
            {
                if (_plusStyle == null)
                {
                    _plusStyle = new GUIStyle(CustomGUIStyles.RichMiniLabel)
                    {
                        padding = new RectOffset(0, 0, 0, 0)
                    };
                }
                return _plusStyle;
            }
        }

        private static GUIStyle _italicStyle;
        internal static GUIStyle ItalicStyle
        {
            get
            {
                if (_italicStyle == null)
                {
                    _italicStyle = new GUIStyle(EditorStyles.label)
                    {
                        fontStyle = FontStyle.Italic,
                        fontSize = 10
                    };
                }
                return _italicStyle;
            }
        }

        private static GUIStyle _expandButton;
        internal static GUIStyle ExpandButtonStyle
        {
            get
            {
                if (_expandButton == null)
                {

                    _expandButton = new GUIStyle(GUI.skin.button)
                    {
                        fontSize = 11,
                        fixedHeight = 12,
                        fixedWidth = 35,

                        fontStyle = FontStyle.Bold,
                        alignment = TextAnchor.MiddleCenter,
                        padding = new RectOffset(0, 0, 0, 0),
                        border = new RectOffset(0, 0, 0, 0)



                    };
                }
                return _expandButton;
            }
        }
        private static GUIStyle _buttonsUpRight;
        internal static GUIStyle ButtonsUpSection
        {
            get
            {
                if (_buttonsUpSection == null)
                {
                    _buttonsUpSection = new GUIStyle(EditorStyles.toolbar)
                    {
                        fixedHeight = 10
                    };
                }
                return _buttonsUpSection;
            }
        }
        private static GUIStyle _buttonsUpSection;
        internal static GUIStyle DraggingTabs
        {
            get
            {
                if (_draggingTab == null)
                {
                    _draggingTab = new GUIStyle(EditorToolbarButton)
                    {
                        fontSize = 11,
                        alignment = TextAnchor.MiddleCenter,
                        fixedHeight = 23,
                        padding = new RectOffset(15, 0, 0, 0)

                    };
                }
                return _draggingTab;
            }
        }
        private static GUIStyle _draggingTab;
        private static GUIStyle _addStyle;
        internal static GUIStyle AddStyle
        {
            get
            {
                if (_addStyle == null)
                {
                    _addStyle = new GUIStyle(EditorToolbarButton)
                    {
                        fixedHeight = 23,
                        padding = new RectOffset(0, 0, 0, 0)

                    };

                }
                return _addStyle;
            }
        }
        private static GUIStyle _modifiedToolbarButton;
        internal static GUIStyle ModifiedToolbarButton
        {
            get
            {
                if (_modifiedToolbarButton == null)
                {
                    _modifiedToolbarButton = new GUIStyle(EditorToolbarButton)
                    {
                        padding = new RectOffset(3, 3, 0, 0),
                        fixedHeight = 23
                    };
                }
                return _modifiedToolbarButton;
            }
        }

        private static GUIStyle _componentStyle;
        internal static GUIStyle ComponentStyle
        {
            get
            {
                if (_componentStyle == null)
                {
                    _componentStyle = new GUIStyle();
                    _componentStyle.padding.top = 4;
                    _componentStyle.padding.bottom = 3;
                    _componentStyle.margin.left = 18;
                    _componentStyle.margin.right = 1;
                    _componentStyle.stretchWidth = true;
                    _componentStyle.stretchHeight = true;
                }
                return _componentStyle;
            }
        }

        private static GUIStyle _activeButtonStyle;
        internal static GUIStyle ActiveButtonStyle
        {
            get
            {
                if (_activeButtonStyle == null)
                {
                    _activeButtonStyle = new GUIStyle(GUI.skin.button);


                }
                return _activeButtonStyle;
            }
        }

        private static GUIStyle _textStyle;
        internal static GUIStyle TextStyle()
        {
            if (_textStyle == null || _textStyle.wordWrap != true)
            {
                _textStyle = new GUIStyle(EditorStyles.textArea)
                {
                    wordWrap = true,
                    stretchHeight = false
                };
            }
            return _textStyle;
        }

        private static GUIStyle _wrapLabelStyle;
        internal static GUIStyle WrapLabelStyle
        {
            get
            {
                if (_wrapLabelStyle == null)
                {
                    _wrapLabelStyle = new GUIStyle(EditorStyles.label)
                    {
                        contentOffset = new Vector2(0, 1),
                        richText = true,
                        wordWrap = true
                    };
                }
                return _wrapLabelStyle;
            }
        }

        private static GUIStyle _centeredLabelStyle;
        internal static GUIStyle CenteredLabelStyle
        {
            get
            {
                if (_centeredLabelStyle == null)
                {
                    _centeredLabelStyle = new GUIStyle(EditorStyles.label)
                    {
                        richText = true,
                        alignment = TextAnchor.MiddleCenter
                    };
                }
                return _centeredLabelStyle;
            }
        }
        private static GUIStyle _tipStyle;
        internal static GUIStyle TipStyle
        {
            get
            {
                if (_tipStyle == null)
                {
                    _tipStyle = new GUIStyle()
                    {
                        alignment = TextAnchor.MiddleLeft
                    };
                    _tipStyle.normal.background = EditorGUIUtility.whiteTexture;
                    _tipStyle.fontSize = 11;
                    _tipStyle.richText = true;
                    _tipStyle.wordWrap = false;
                    _tipStyle.normal.textColor = Color.black;
                    _tipStyle.padding = new RectOffset(6, 6, 3, 3);
                }
                return _tipStyle;
            }
        }

        private static GUIStyle _tipMultiStyle;
        internal static GUIStyle TipMultiStyle
        {
            get
            {
                if (_tipMultiStyle == null)
                {
                    _tipMultiStyle = new GUIStyle()
                    {
                        alignment = TextAnchor.MiddleLeft
                    };
                    _tipMultiStyle.normal.background = EditorGUIUtility.whiteTexture;
                    _tipMultiStyle.fontSize = 11;
                    _tipMultiStyle.richText = true;
                    _tipMultiStyle.wordWrap = false;
                    _tipMultiStyle.normal.textColor = Color.black;
                    _tipMultiStyle.border = new RectOffset(2, 2, 2, 2);
                    _tipMultiStyle.padding = new RectOffset(6, 6, 3, 3);


                }
                return _tipMultiStyle;
            }
        }

        private static GUIStyle _scrollTabsStyle;
        internal static GUIStyle ScrollTabsStyle
        {
            get
            {
                if (_scrollTabsStyle == null)
                {
                    _scrollTabsStyle = new GUIStyle(GUIStyle.none)
                    {
                        stretchHeight = false,
                        margin = new RectOffset(0, 23, 0, 0),
                        padding = new RectOffset(0, 0, 0, 0),
                        stretchWidth = false
                    };

                }
                return _scrollTabsStyle;
            }
        }

        private static GUIStyle _scrollTabsHistoryStyle;
        internal static GUIStyle ScrollTabsHistoryStyle
        {
            get
            {
                if (_scrollTabsHistoryStyle == null)
                {
                    _scrollTabsHistoryStyle = new GUIStyle(GUIStyle.none)
                    {
                        stretchHeight = false,

                        margin = new RectOffset(40, 22, 0, 0),
                        padding = new RectOffset(0, 0, 0, 0),
                        stretchWidth = false
                    };
                }
                return _scrollTabsHistoryStyle;
            }
        }

        private static GUIStyle _toolbarButtonTabs_Active;
        internal static GUIStyle ToolbarButtonTabs_Active
        {
            get
            {
                if (_toolbarButtonTabs_Active == null)
                {
                    _toolbarButtonTabs_Active = new GUIStyle(EditorToolbarButton)
                    {
                        fixedHeight = 22
                    };
                    _toolbarButtonTabs_Active.padding.top = 3;
                    _toolbarButtonTabs_Active.padding.right = 8;
                    _toolbarButtonTabs_Active.alignment = TextAnchor.MiddleLeft;
                    _toolbarButtonTabs_Active.contentOffset = new Vector2(0, 0);
                    _toolbarButtonTabs_Active.fontSize = 11;
                    if (EditorGUIUtility.isProSkin)
                    {
                        _toolbarButtonTabs_Active.normal.textColor = new Color(0.95f, 0.95f, 0.95f, 1);
                        _toolbarButtonTabs_Active.active.textColor = new Color(1f, 1f, 1f, 1);
                        _toolbarButtonTabs_Active.hover.textColor = new Color(1f, 1f, 1f, 1);
                    }
                    else
                    {
                        _toolbarButtonTabs_Active.normal.textColor = new Color(0.2f, 0.2f, 0.2f, 1);
                        _toolbarButtonTabs_Active.active.textColor = new Color(0.1f, 0.1f, 0.1f, 1);
                        _toolbarButtonTabs_Active.hover.textColor = new Color(0.1f, 0.1f, 0.1f, 1);
                    }

                }
                return _toolbarButtonTabs_Active;
            }
        }
        private static GUIStyle _toolbarButtonTabsPrefab_active;
        internal static GUIStyle ToolbarButtonTabsPrefab_Active
        {
            get
            {
                if (_toolbarButtonTabsPrefab_active == null)
                {
                    _toolbarButtonTabsPrefab_active = new GUIStyle(EditorToolbarButton);
                    _toolbarButtonTabsPrefab_active.padding.top = 3;
                    _toolbarButtonTabsPrefab_active.alignment = TextAnchor.MiddleLeft;
                    _toolbarButtonTabsPrefab_active.fixedHeight = 22;
                    _toolbarButtonTabsPrefab_active.padding.right = 8;
                    _toolbarButtonTabsPrefab_active.contentOffset = new Vector2(0, 0);
                    _toolbarButtonTabsPrefab_active.fontSize = 11;
                    if (EditorGUIUtility.isProSkin)
                    {
                        _toolbarButtonTabsPrefab_active.normal.textColor = new Color(0.7f, 0.7f, 0.9f, 1);
                        _toolbarButtonTabsPrefab_active.hover.textColor = new Color(0.77f, 0.77f, 1f, 1);
                        _toolbarButtonTabsPrefab_active.active.textColor = new Color(0.8f, 0.8f, 1f, 1);
                    }
                    else
                    {
                        _toolbarButtonTabsPrefab_active.normal.textColor = new Color(0.4f, 0.4f, 0.6f, 1);
                        _toolbarButtonTabsPrefab_active.hover.textColor = new Color(0.3f, 0.3f, 0.5f, 1);
                        _toolbarButtonTabsPrefab_active.active.textColor = new Color(0.3f, 0.3f, 0.5f, 1);
                    }
                }
                return _toolbarButtonTabsPrefab_active;
            }
        }

        private static GUIStyle _toolbarButtonTabsPrefab;
        internal static GUIStyle ToolbarButtonTabsPrefab
        {
            get
            {
                if (_toolbarButtonTabsPrefab == null)
                {
                    _toolbarButtonTabsPrefab = new GUIStyle(EditorToolbarButton);
                    _toolbarButtonTabsPrefab.padding.top = 3;
                    _toolbarButtonTabsPrefab.padding.right = 8;
                    _toolbarButtonTabsPrefab.fixedHeight = 22;
                    _toolbarButtonTabsPrefab.alignment = TextAnchor.MiddleLeft;
                    _toolbarButtonTabsPrefab.contentOffset = new Vector2(0, 0);
                    _toolbarButtonTabsPrefab.fontSize = 11;
                    if (EditorGUIUtility.isProSkin)
                    {
                        _toolbarButtonTabsPrefab.normal.textColor = new Color(0.75f, 0.75f, 1f, 1);
                        _toolbarButtonTabsPrefab.hover.textColor = new Color(0.8f, 0.8f, 1f, 1);
                        _toolbarButtonTabsPrefab.active.textColor = new Color(0.83f, 0.83f, 1f, 1);
                    }
                    else
                    {
                        _toolbarButtonTabsPrefab.normal.textColor = new Color(0.3f, 0.3f, 0.5f, 1);
                        _toolbarButtonTabsPrefab.hover.textColor = new Color(0.3f, 0.3f, 0.5f, 1);
                        _toolbarButtonTabsPrefab.active.textColor = new Color(0.3f, 0.3f, 0.5f, 1);
                    }
                }
                return _toolbarButtonTabsPrefab;
            }
        }
        private static GUIStyle _toolbarButtonTabs;
        internal static GUIStyle ToolbarButtonTabs
        {
            get
            {
                if (_toolbarButtonTabs == null)
                {
                    _toolbarButtonTabs = new GUIStyle(EditorToolbarButton);
                    _toolbarButtonTabs.padding.top = 4;
                    _toolbarButtonTabs.fixedHeight = 22;
                    _toolbarButtonTabs.padding.right = 8;
                    _toolbarButtonTabs.alignment = TextAnchor.MiddleLeft;
                    _toolbarButtonTabs.contentOffset = new Vector2(0, 0);
                    _toolbarButtonTabs.fontSize = 11;
                    if (EditorGUIUtility.isProSkin)
                    {
                        _toolbarButtonTabs.normal.textColor = new Color(0.85f, 0.85f, 0.85f, 1);
                        _toolbarButtonTabs.active.textColor = new Color(0.9f, 0.9f, 0.9f, 1);
                        _toolbarButtonTabs.hover.textColor = new Color(1f, 1f, 1f, 1);
                    }
                    else
                    {
                        _toolbarButtonTabs.normal.textColor = new Color(0.2f, 0.2f, 0.2f, 1);
                        _toolbarButtonTabs.active.textColor = new Color(0.1f, 0.1f, 0.1f, 1);
                        _toolbarButtonTabs.hover.textColor = new Color(0.1f, 0.1f, 0.1f, 1);
                    }
                }
                return _toolbarButtonTabs;
            }
        }

        private static GUIStyle _toolbarButtonTabs_White;
        internal static GUIStyle ToolbarButtonTabs_White
        {
            get
            {
                if (_toolbarButtonTabs_White == null)
                {
                    _toolbarButtonTabs_White = new GUIStyle(EditorToolbarButton);
                    _toolbarButtonTabs_White.padding.top = 3;
                    _toolbarButtonTabs_White.fixedHeight = 22;
                    _toolbarButtonTabs_White.padding.right = 8;
                    _toolbarButtonTabs_White.alignment = TextAnchor.MiddleLeft;
                    _toolbarButtonTabs_White.contentOffset = new Vector2(0, 0);
                    _toolbarButtonTabs_White.fontSize = 11;
                    _toolbarButtonTabs_White.normal.textColor = new Color(0.2f, 0.2f, 0.2f, 1);
                    _toolbarButtonTabs_White.active.textColor = new Color(0.1f, 0.1f, 0.1f, 1);
                    _toolbarButtonTabs_White.hover.textColor = new Color(0.1f, 0.1f, 0.1f, 1);
                }
                return _toolbarButtonTabs_White;
            }
        }
        private static GUIStyle _centerLabel;
        internal static GUIStyle CenterLabel
        {
            get
            {
                if (_centerLabel == null)
                {
                    _centerLabel = new GUIStyle(EditorStyles.label)
                    {
                        richText = true,
                        alignment = TextAnchor.MiddleCenter,
                        wordWrap = true,
                        padding = new RectOffset(10, 10, 10, 10)
                    };
                }
                return _centerLabel;
            }
        }
        private static GUIStyle _bigCenterLabel;
        internal static GUIStyle BigCenterLabel
        {
            get
            {
                if (_bigCenterLabel == null)
                {
                    _bigCenterLabel = new GUIStyle(EditorStyles.largeLabel)
                    {
                        richText = true,
                        alignment = TextAnchor.MiddleCenter,
                        wordWrap = true,
                        fontSize = 14,
                        normal = { textColor = new Color(0.93f, 0.93f, 0.93f, 1) },
                        fontStyle = FontStyle.Bold,
                        padding = new RectOffset(10, 10, 10, 10)
                    };
                }
                return _bigCenterLabel;
            }
        }


        private static GUIStyle _horizontalScrollbar;
        internal static GUIStyle HorizontalScrollbar
        {
            get
            {
                if (_horizontalScrollbar == null)
                {
                    _horizontalScrollbar = new GUIStyle(GUI.skin.horizontalScrollbar)
                    {
                        fixedHeight = 9,
                        contentOffset = new Vector2(2, 2),
                        padding = new RectOffset(0, 0, 1, 0)
                    };
                }
                return _horizontalScrollbar;
            }
        }

        private static GUIStyle _toolbarButtonAsset;
        internal static GUIStyle ToolbarButtonAsset
        {
            get
            {
                if (_toolbarButtonAsset == null)
                {
                    _toolbarButtonAsset = new GUIStyle(EditorToolbarButton)
                    {
                        padding = new RectOffset(4, 4, 3, 3),
                    };
                }
                return _toolbarButtonAsset;
            }
        }

        private static GUIStyle _addComponentButton;
        internal static GUIStyle AddComponentButton
        {
            get
            {
                //  if (_addComponentButton == null)
                {
                    _addComponentButton = new GUIStyle(EditorToolbarButton)
                    {
                        padding = new RectOffset(6, 4, 0, 0),
                        margin = new RectOffset(0, 0, 0, 0),
                        richText = true,
                        fontSize = 11,
                        border = new RectOffset(0, 0, 0, 0),
                    };
                }
                return _addComponentButton;
            }
        }



        private static GUIStyle _headerStyle;
        internal static GUIStyle HeaderStyle
        {
            get
            {
                if (_headerStyle == null)
                {
                    _headerStyle = new GUIStyle(EditorStyles.largeLabel)
                    {
                        fontSize = 13
                    };
                }
                return _headerStyle;
            }
        }

        private static GUIStyle _debugIconStyle;
        internal static GUIStyle DebugIconStyle
        {
            get
            {
                if (_debugIconStyle == null)
                {
                    _debugIconStyle = new GUIStyle(IconButton)
                    {
                        padding = new RectOffset(1, 1, 1, 0),
                        active = { background = null },
                        normal = { background = null },
                    };
                }
                return _debugIconStyle;
            }
        }
        private static GUIStyle _historyIconStyle;
        internal static GUIStyle HistoryIconStyle
        {
            get
            {
                if (_historyIconStyle == null)
                {
                    _historyIconStyle = new GUIStyle(IconButton)
                    {
                        padding = new RectOffset(0, 0, 0, 0),
                        active = { background = null },
                        normal = { background = null },
                    };
                }
                return _historyIconStyle;
            }
        }

        private static GUIStyle _alignedLabelStyle;
        internal static GUIStyle AlignedLabelStyle
        {
            get
            {
                if (_alignedLabelStyle == null)
                {
                    _alignedLabelStyle = new GUIStyle(EditorStyles.label)
                    {
                        alignment = TextAnchor.MiddleLeft,
                        contentOffset = new Vector2(0, -1),
                        padding = new RectOffset(2, 2, 2, 2),
                        margin = new RectOffset(2, 2, 2, 2)
                    };
                    _alignedLabelStyle.normal.background = null;
                    _alignedLabelStyle.fixedHeight = 20;
                    if (EditorUtils.IsLightSkin())
                    {
                        _alignedLabelStyle.fontSize = 12;
                    }
                    else
                    {
                        _alignedLabelStyle.fontSize = 11;
                    }
                    _alignedLabelStyle.richText = true;
                    _alignedLabelStyle.hover.textColor = Color.white;
                }
                return _alignedLabelStyle;
            }
        }
        static GUIStyle _compStyle;
        internal static GUIStyle CompStyle
        {
            get
            {
                if (_compStyle == null)
                {
                    _compStyle = new GUIStyle(EditorStyles.inspectorDefaultMargins);
                }
                EditorGUIUtility.wideMode = true;
                CoInspectorWindow.UpdateLabelSize();
                return _compStyle;
            }
        }
        static GUIStyle _collapsedCompStyle;
        internal static GUIStyle CollapsedCompStyle
        {
            get
            {
                if (_collapsedCompStyle == null)
                {
                    _collapsedCompStyle = new GUIStyle(EditorStyles.inspectorDefaultMargins);
                    _collapsedCompStyle.padding.bottom = 0;
                    _collapsedCompStyle.padding.top = 0;
                    _collapsedCompStyle.margin.bottom = 0;
                    _collapsedCompStyle.padding.top = 0;

                }
                EditorGUIUtility.wideMode = true;
                CoInspectorWindow.UpdateLabelSize();
                return _collapsedCompStyle;
            }
        }
    }
}