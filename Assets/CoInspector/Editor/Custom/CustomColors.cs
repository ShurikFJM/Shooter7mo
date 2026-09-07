using UnityEditor;
using UnityEngine;


namespace CoInspector
{
    internal static class CustomColors
    {
        private static Color _defaultInspector = Color.black;
        internal static Color DefaultInspector
        {
            get
            {
                if (_defaultInspector == Color.black)
                {
                    _defaultInspector = EditorGUIUtility.isProSkin ? new Color(0.235f, 0.235f, 0.235f, 1f) : new Color(0.8f, 0.8f, 0.8f, 1);
                }
                return _defaultInspector;
            }
        }
        internal static Color CloseRed = new Color(1, 0.05f, 0.05f);
        private static Color _customLightBlue = Color.black;
        internal static Color CustomLightBlue
        {
            get
            {
                if (_customLightBlue == Color.black)
                {
                    _customLightBlue = new Color(0.7f, 0.7f, 1f, 1f);
                }
                return _customLightBlue;
            }
        }

        private static Color _buttonEnabled = Color.black;
        internal static Color ButtonEnabled
        {
            get
            {
                if (_buttonEnabled == Color.black)
                {
                    _buttonEnabled = EditorGUIUtility.isProSkin ? new Color(0.7f, 0.7f, 0.7f, 0.5f) : new Color(0.6f, 0.6f, 0.8f, 0.5f);
                }
                return _buttonEnabled;
            }
        }

        private static Color _customBlue = Color.black;
        internal static Color CustomBlue
        {
            get
            {
                if (_customBlue == Color.black)
                {
                    _customBlue = new Color(0.6f, 0.6f, 1f, 0.7f);
                }
                return _customBlue;
            }
        }
        private static Color _customFadeBlue = Color.black;
        internal static Color CustomFadeBlue
        {
            get
            {
                if (_customFadeBlue == Color.black)
                {
                    _customFadeBlue = new Color(0.25f, 0.25f, 0.35f, 1f);
                }
                return _customFadeBlue;
            }
        }
        private static Color _fadeBlue = Color.black;
        internal static Color FadeBlue
        {
            get
            {
                if (_fadeBlue == Color.black)
                {
                    if (EditorGUIUtility.isProSkin)
                        _fadeBlue = new Color(0.6f, 0.6f, 1f, 0.15f);
                    else
                        _fadeBlue = new Color(0.6f, 0.6f, 1f, 0.20f);
                }
                return _fadeBlue;
            }
        }

        private static Color _dragTabColor = Color.black;
        internal static Color DragTabColor
        {
            get
            {
                if (_dragTabColor == Color.black)
                {
                    _dragTabColor = new Color(0.4f, 0.4f, 0.7f, 1f);
                }
                return _dragTabColor;
            }
        }

        private static Color _inGameCorrectionColor = Color.black;
        internal static Color InGameCorrectionColor
        {
            get
            {
                if (_inGameCorrectionColor == Color.black)
                {
                    if (EditorGUIUtility.isProSkin)
                    {
                        _inGameCorrectionColor = new Color(0.1f, 0.1f, 0.1f, 0.2f);
                    }
                    else
                    {
                        _inGameCorrectionColor = new Color(0.7f, 0.7f, 0.7f, 0.2f);
                    }
                }


                return _inGameCorrectionColor;
            }
        }
        private static Color _newTabButton = Color.black;
        internal static Color NewTabButton
        {
            get
            {
                if (_newTabButton == Color.black)
                {
                    _newTabButton = new Color(1f, 1f, 1.2f, 0.75f);
                }
                return _newTabButton;
            }
        }

        private static Color _darkHistoryButton = Color.black;
        internal static Color DarkHistoryButton
        {
            get
            {
                if (_darkHistoryButton == Color.black)
                {
                    _darkHistoryButton = new Color(0.25f, 0.25f, 0.65f, 0);
                }
                return _darkHistoryButton;
            }
        }

        private static Color _lightHistoryButton = Color.black;
        internal static Color LightHistoryButton
        {
            get
            {
                if (_lightHistoryButton == Color.black)
                {
                    _lightHistoryButton = new Color(0.3f, 0.3f, 0f, 0);
                }
                return _lightHistoryButton;
            }
        }

        private static Color _floatingTabSelected = Color.black;
        internal static Color FloatingTabSelected
        {
            get
            {
                if (_floatingTabSelected == Color.black)
                {
                    _floatingTabSelected = new Color(0.5f, 0.5f, 0.8f);
                }
                return _floatingTabSelected;
            }
        }

        private static Color _activeDark = Color.black;
        internal static Color ActiveDark
        {
            get
            {
                if (_activeDark == Color.black)
                {
                    _activeDark = EditorGUIUtility.isProSkin ? new Color(0.25f, 0.25f, 0.25f, 1) : new Color(0.8f, 0.8f, 0.8f, 1);
                }
                return _activeDark;
            }
        }



        private static Color _activeLight = Color.black;
        internal static Color ActiveLight
        {
            get
            {
                if (_activeLight == Color.black)
                {
                    _activeLight = EditorGUIUtility.isProSkin ? new Color(0.25f, 0.25f, 0.25f, 1) : new Color(0.8f, 0.8f, 0.8f, 1);
                }
                return _activeLight;
            }
        }

        internal static Color FloatingTabUnselected = new Color(1f, 1f, 1.2f, 1f);

        private static Color _darkInspector = Color.black;
        internal static Color DarkInspector
        {
            get
            {
                if (_darkInspector == Color.black)
                {
                    if (EditorGUIUtility.isProSkin)
                    {
                        _darkInspector = new Color(0.2f, 0.2f, 0.2f);
                    }
                    else
                    {
                        _darkInspector = new Color(0.55f, 0.55f, 0.6f);
                    }
                }
                return _darkInspector;
            }
        }

        private static Color _darkGray = Color.black;
        internal static Color DarkGray
        {
            get
            {
                if (_darkGray == Color.black)
                {
                    _darkGray = new Color(0.2f, 0.2f, 0.2f);
                }
                return _darkGray;
            }
        }

        private static Color _floatingTabSelected2 = Color.black;
        internal static Color FloatingTabSelected2
        {
            get
            {
                if (_floatingTabSelected2 == Color.black)
                {
                    _floatingTabSelected2 = new Color(0.35f, 0.35f, 0.5f);
                }
                return _floatingTabSelected2;
            }
        }

        private static Color _editorToolInactive = Color.black;
        internal static Color EditorToolInactive
        {
            get
            {
                if (_editorToolInactive == Color.black)
                {
                    _editorToolInactive = new Color(0.4f, 0.4f, 0.4f, 0);
                }
                return _editorToolInactive;
            }
        }

        private static Color _editorToolActive = Color.black;
        internal static Color EditorToolActive
        {
            get
            {
                if (_editorToolActive == Color.black)
                {
                    _editorToolActive = new Color(0.1f, 0.1f, 0.5f, 0);
                }
                return _editorToolActive;
            }
        }

        private static Color _darkHistoryDisabled = Color.black;
        internal static Color DarkHistoryDisabled
        {
            get
            {
                if (_darkHistoryDisabled == Color.black)
                {
                    _darkHistoryDisabled = new Color(0.1f, 0.1f, -0.3f, 0);
                }
                return _darkHistoryDisabled;
            }
        }

        private static Color _lightHistoryDisabled = Color.black;
        internal static Color LightHistoryDisabled
        {
            get
            {
                if (_lightHistoryDisabled == Color.black)
                {
                    _lightHistoryDisabled = new Color(0.6f, 0.6f, 0.5f, 0);
                }
                return _lightHistoryDisabled;
            }
        }

        private static Color _inGameCorrectionColorActive = Color.black;
        internal static Color InGameCorrectionColorActive
        {
            get
            {
                if (_inGameCorrectionColorActive == Color.black)
                {
                    _inGameCorrectionColorActive = new Color(1f, 1f, 1f, 0.02f);
                }
                return _inGameCorrectionColorActive;
            }
        }

        private static Color _lineColor = Color.black;
        internal static Color LineColor
        {
            get
            {
                if (_lineColor == Color.black)
                {
                    _lineColor = new Color(0.4f, 0.4f, 0.8f, 0.5f);
                }
                return _lineColor;
            }
        }

        private static Color _dragColor = Color.black;
        internal static Color DragColor
        {
            get
            {
                if (_dragColor == Color.black)
                {
                    _dragColor = new Color(0.65f, 0.65f, 0.75f, 1f);
                }
                return _dragColor;
            }
        }

        private static Color _textPreviewColor = Color.black;
        internal static Color TextPreviewColor
        {
            get
            {
                if (_textPreviewColor == Color.black)
                {
                    _textPreviewColor = new Color(0.8f, 0.8f, 0.8f, 1);
                }
                return _textPreviewColor;
            }
        }

        private static Color _flexibleColor = Color.black;
        internal static Color FlexibleColor
        {
            get
            {
                if (_flexibleColor == Color.black)
                {
                    _flexibleColor = new Color(0f, 0f, 0f, 0.1f);
                }
                return _flexibleColor;
            }
        }

        private static Color _customSubtleBlue = Color.black;
        internal static Color CustomSubtleBlue
        {
            get
            {
                if (_customSubtleBlue == Color.black)
                {
                    _customSubtleBlue = new Color(0.6f, 0.6f, 1f, 0.1f);
                }
                return _customSubtleBlue;
            }
        }

        private static Color _customGreen = Color.black;
        internal static Color CustomGreen
        {
            get
            {
                if (_customGreen == Color.black)
                {
                    _customGreen = new Color(0.6f, 1f, 0.6f, 0.7f);
                }
                return _customGreen;
            }
        }

        private static Color _subtleGreen = Color.black;
        internal static Color SubtleGreen
        {
            get
            {
                if (_subtleGreen == Color.black)
                {
                    _subtleGreen = new Color(0.6f, 1f, 0.6f, 0.1f);
                }
                return _subtleGreen;
            }
        }

        private static Color _customRed = Color.black;
        internal static Color CustomRed
        {
            get
            {
                if (_customRed == Color.black)
                {
                    _customRed = new Color(1f, 0.6f, 0.6f, 0.7f);
                }
                return _customRed;
            }
        }

        private static Color _resetToDefault = Color.black;
        internal static Color ResetToDefault
        {
            get
            {
                if (_resetToDefault == Color.black)
                {
                    _resetToDefault = new Color(1.5f, 0.8f, 0.8f);
                }
                return _resetToDefault;
            }
        }

        private static Color _defaultInspectorBright = Color.black;
        internal static Color DefaultInspectorBright
        {
            get
            {
                if (_defaultInspectorBright == Color.black)
                {
                    _defaultInspectorBright = EditorGUIUtility.isProSkin ? new Color(0.25f, 0.25f, 0.25f, 1) : new Color(0.8f, 0.8f, 0.8f, 1);
                }
                return _defaultInspectorBright;
            }
        }

        private static Color _subtleBlue = Color.black;
        internal static Color SubtleBlue
        {
            get
            {
                if (_subtleBlue == Color.black)
                {
                    _subtleBlue = new Color(0.2f, 0.2f, 0.5f, 0.025f);
                }
                return _subtleBlue;
            }
        }
        private static Color _verySubtleBlue = Color.black;
        internal static Color VerySubtleBlue
        {
            get
            {
                if (_verySubtleBlue == Color.black)
                {
                    _verySubtleBlue = new Color(0.4f, 0.4f, 0.75f, 0.06f);
                }
                return _verySubtleBlue;
            }
        }



        private static Color _subtleBright = Color.black;
        internal static Color SubtleBright
        {
            get
            {
                if (_subtleBright == Color.black)
                {
                    _subtleBright = new Color(1, 1, 1, 0.03f);
                }
                return _subtleBright;
            }
        }
        private static Color _softBright = Color.black;
        internal static Color SoftBright
        {
            get
            {
                //   if (_softBright == Color.black)
                {
                    _softBright = new Color(1, 1, 1, 0.08f);
                }
                return _softBright;
            }
        }

        private static Color simpleBright = Color.black;
        internal static Color SimpleBright
        {
            get
            {
                if (simpleBright == Color.black)
                {
                    simpleBright = new Color(1f, 1f, 1f, 0.15f);
                }
                return simpleBright;
            }
        }

        private static Color _addButtonColorDark = Color.black;
        internal static Color AddButtonColorDark
        {
            get
            {
                if (_addButtonColorDark == Color.black)
                {
                    _addButtonColorDark = new Color(0.1f, 0.45f, 0.1f, 0);
                }
                return _addButtonColorDark;
            }
        }

        private static Color _addButtonColorLight = Color.black;
        internal static Color AddButtonColorLight
        {
            get
            {
                if (_addButtonColorLight == Color.black)
                {
                    _addButtonColorLight = new Color(0.6f, 0.4f, 0.6f, 0);
                }
                return _addButtonColorLight;
            }
        }

        private static Color _floatingBarBase = Color.black;
        internal static Color FloatingBarBase
        {
            get
            {
                if (_floatingBarBase == Color.black)
                {
                    _floatingBarBase = new Color(0.2f, 0.2f, 0.4f, 1f);
                }
                return _floatingBarBase;
            }
        }

        private static Color _backHistory = Color.black;
        internal static Color BackHistory
        {
            get
            {
                if (_backHistory == Color.black)
                {
                    _backHistory = new Color(-0.1f, -0.1f, 0.1f);
                }
                return _backHistory;
            }
        }

        private static Color _lightSkinRed = Color.black;
        internal static Color LightSkinRed
        {
            get
            {
                if (_lightSkinRed == Color.black)
                {
                    _lightSkinRed = new Color(-0.4f, 0.42f, 0.42f, 0);
                }
                return _lightSkinRed;
            }
        }

        private static Color _assetBarBackColor = Color.black;
        internal static Color AssetBarBackColor
        {
            get
            {
                if (_assetBarBackColor == Color.black)
                {
                    _assetBarBackColor = new Color(0.05f, 0.05f, 0.2f, 1f);
                }
                return _assetBarBackColor;
            }
        }
        private static Color harderBright = Color.black;
        internal static Color HarderBright
        {
            get
            {
                if (harderBright == Color.black)
                {
                    if (EditorGUIUtility.isProSkin)
                        harderBright = new Color(1f, 1f, 1f, 0.2f);
                    else
                        harderBright = new Color(1f, 1f, 1f, 0.4f);
                }
                return harderBright;
            }
        }
        private static Color simpleShadow = Color.black;
        internal static Color SimpleShadow
        {
            get
            {
                if (simpleShadow == Color.black)
                {
                    simpleShadow = new Color(0f, 0f, 0f, 0.2f);
                }
                return simpleShadow;
            }
        }
        private static Color softShadow = Color.black;
        internal static Color SoftShadow
        {
            get
            {
                if (softShadow == Color.black)
                {
                    softShadow = new Color(0f, 0f, 0f, 0.1f);
                }
                return softShadow;
            }
        }
        private static Color backDarkColor = Color.black;
        internal static Color BackDarkColor
        {
            get
            {
                if (backDarkColor == Color.black)
                {
                    backDarkColor = new Color(0.3f, 0.3f, 0.3f, 0.25f);
                }
                return backDarkColor;
            }
        }
        private static Color verySoftShadow = Color.black;
        internal static Color VerySoftShadow
        {
            get
            {
                if (verySoftShadow == Color.black)
                {
                    verySoftShadow = new Color(0f, 0f, 0f, 0.05f);
                }
                return verySoftShadow;
            }
        }
        private static Color hardShadow = Color.black;
        internal static Color HardShadow
        {
            get
            {
                if (hardShadow == Color.black)
                {
                    hardShadow = new Color(0f, 0f, 0f, 0.6f);
                }
                return hardShadow;
            }
        }
        private static Color gradientShadow = Color.black;
        internal static Color GradientShadow
        {
            get
            {
                if (gradientShadow == Color.black)
                {
                    gradientShadow = new Color(0.12f, 0.12f, 0.12f, 0.6f);
                }
                return gradientShadow;
            }
        }

        private static Color welcomeColor = Color.black;
        internal static Color WelcomeColor
        {
            get
            {
                if (welcomeColor == Color.black)
                {
                    welcomeColor = new Color(0f, 0f, 0f, 0.5f);
                }
                return welcomeBackColor;
            }
        }
        private static Color welcomeBackColor = Color.black;
        internal static Color WelcomeBackColor
        {
            get
            {
                if (welcomeBackColor == Color.black)
                {
                    welcomeBackColor = new Color(0f, 0f, 0.15f);
                }
                return welcomeBackColor;
            }
        }

        private static Color welcomeSubBackColor = Color.black;
        internal static Color WelcomeSubBackColor
        {
            get
            {
                if (welcomeSubBackColor == Color.black)
                {
                    welcomeSubBackColor = new Color(0.8f, -0.1f, -0.1f);
                }
                return welcomeSubBackColor;
            }
        }

        private static Color mediumShadow = Color.black;
        internal static Color MediumShadow
        {
            get
            {
                if (mediumShadow == Color.black)
                {
                    mediumShadow = new Color(0f, 0f, 0f, 0.35f);
                }
                return mediumShadow;
            }
        }
        private static Color _allCollapsed = Color.black;
        internal static Color AllCollapsed
        {
            get
            {
                if (_allCollapsed == Color.black)
                {
                    _allCollapsed = new Color(-0.2f, -0.2f, 0.1f, 0);
                }
                return _allCollapsed;
            }
        }

        private static Color _notAllCollapsed = Color.black;
        internal static Color NotAllCollapsed
        {
            get
            {
                if (_notAllCollapsed == Color.black)
                {
                    _notAllCollapsed = new Color(0.45f, 0.45f, 0.45f, 0);
                }
                return _notAllCollapsed;
            }
        }

        private static Color _multiFooter = new Color(0.5f, 0.5f, 0.5f, 0.5f);
        internal static Color MultiFooter
        {
            get
            {
                if (_multiFooter == Color.black)
                {
                    _multiFooter = new Color(0.5f, 0.5f, 0.5f, 0.5f);
                }
                return _multiFooter;
            }
        }
    }
}