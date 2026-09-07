using UnityEngine;
using UnityEditor;
using System.Linq;
using System;

namespace CoInspector
{
    internal class FirstInstallWindow : EditorWindow
    {
        internal static UserSaveData userSaveData;
        private enum Step
        {
            Welcome,
            OpenWindow,
            SelectGameObject,
            SelectGameObject2,
            SelectAsset,
            NavigateHistory,
            SelectActiveTab,
            Finish
        }

        private Step currentStep = Step.Welcome;
        private bool resize = true;
        private bool alreadySaidBye = false;
        private Vector2 lastSize = new Vector2(480, 320);

        // [MenuItem("CoInspector/First Install Window")]
        internal static void ShowWindow()
        {
            CoInspectorWindow.assetInspection = true;
            FirstInstallWindow window = GetWindow<FirstInstallWindow>(true);
            Vector2 middle = MiddleOfScreen(480, 360);
            window.position = new Rect(middle.x, middle.y, 480, 360);
            window.titleContent = new GUIContent("CoInspector First Steps", CustomGUIContents.MainIconImage);
            window.ShowUtility();
            window.Resize(window.lastSize);
            window.Focus();
        }

        private static Rect GetMainWindowPosition()
        {
#if UNITY_2020_1_OR_NEWER
            return EditorGUIUtility.GetMainWindowPosition();
#else
           return Reflected.GetMainWindowPosition();
#endif
        }
        void TrySave()
        {
            userSaveData = GetUserDataAsset();
            if (userSaveData != null)
            {
                CoInspectorWindow.showInstallMessage = false;
                userSaveData.showInstallMessage = false;
                CoInspectorWindow.showInstallMessage = false;
                EditorUtility.SetDirty(userSaveData);
                //AssetDatabase.SaveAssets();
                AssetDatabase.SaveAssetIfDirty(userSaveData);
            }
        }
        UserSaveData GetUserDataAsset()
        {
            if (userSaveData == null)
            {
                userSaveData = CoInspectorWindow.FindSettingsObject();
                if (userSaveData == null)
                {
                    if (CoInspectorWindow.MainCoInspector)
                    {
                        userSaveData = CoInspectorWindow.MainCoInspector.AutoCreateSettings();
                    }
                    else
                    {
                        userSaveData = CoInspectorWindow._AutoCreateSettings();
                    }
                }
            }
            return userSaveData;
        }

        void BackUpdate()
        {
            if (currentStep is Step.SelectAsset || currentStep is Step.SelectGameObject)
            {
                Repaint();
            }
        }
        static Vector2 MiddleOfScreen(Vector2 size)
        {
            Rect mainWindowRect = GetMainWindowPosition();
            return new Vector2(
                mainWindowRect.x + mainWindowRect.width / 2 - size.x / 2,
                mainWindowRect.y + mainWindowRect.height / 2 - size.y / 2
            );
        }

        internal static Vector2 MiddleOfScreen(float width = 0, float height = 0)
        {
            Rect mainWindowRect = GetMainWindowPosition();
            if (width > 0 && height > 0)
            {
                return new Vector2(
                    mainWindowRect.x + mainWindowRect.width / 2 - width / 2,
                    mainWindowRect.y + mainWindowRect.height / 2 - height / 2
                );
            }
            return new Vector2(
                mainWindowRect.x + mainWindowRect.width / 2,
                mainWindowRect.y + mainWindowRect.height / 2
            );
        }
        internal static Vector2 RightSideOfScreen(float width, float height)
        {
            Rect mainWindowRect = GetMainWindowPosition();
            float rightHalfWidth = mainWindowRect.width / 2;
            float x = mainWindowRect.x + mainWindowRect.width / 2 + (rightHalfWidth - width) / 2;
            float y = mainWindowRect.y + (mainWindowRect.height - height) / 2;
            return new Vector2(x, y);
        }
        private float PlaceOnInspectorSide(float padding = 30f)
        {
            EditorWindow instance = CoInspectorWindow.MainCoInspector;
            if (instance == null)
            {
                return 0;
            }
            Rect mainRect = instance.position;
            Rect thisRect = this.position;
            Rect mainWindowRect = GetMainWindowPosition();
            float leftSpace = mainRect.x - mainWindowRect.x;
            float rightSpace = (mainWindowRect.x + mainWindowRect.width) - (mainRect.x + mainRect.width);

            float newX;
            if (leftSpace >= rightSpace)
            {
                if (thisRect.width <= leftSpace - padding)
                {
                    newX = mainRect.x - thisRect.width - padding;
                }
                else
                {
                    newX = mainWindowRect.x;
                }
            }
            else
            {
                float rightEdge = mainRect.x + mainRect.width;
                if (thisRect.width <= rightSpace - padding)
                {
                    newX = rightEdge + padding;
                }
                else
                {
                    newX = mainWindowRect.x + mainWindowRect.width - thisRect.width;
                }
            }

            return newX;
        }
        void Resize(Vector2 size)
        {
            if (Event.current != null && Event.current.type == EventType.Repaint)
            {
                if (resize || minSize != size)
                {
                    Focus();
                    Vector2 _position = position.position;
                    minSize = size;
                    if (currentStep is Step.OpenWindow)
                    {
                        if (CoInspectorWindow.MainCoInspector)
                        {
                            _position.y = MiddleOfScreen().y - size.y / 2;
                        }
                    }
                    else if (currentStep is Step.SelectGameObject)
                    {
                        if (CoInspectorWindow.MainCoInspector)
                        {
                            _position.y = CoInspectorWindow.MainCoInspector.position.y;
                            _position.x = PlaceOnInspectorSide();
                        }
                    }
                    else if (currentStep is Step.SelectAsset)
                    {
                        if (CoInspectorWindow.MainCoInspector)
                        {
                            _position.y = CoInspectorWindow.MainCoInspector.position.y + CoInspectorWindow.MainCoInspector.position.height - size.y;
                            _position.x = PlaceOnInspectorSide();
                        }
                    }
                    else
                    {
                        _position.y = MiddleOfScreen().y - size.y / 2;
                    }
                    position = new Rect(_position, size);
                    if (minSize != size)
                    {
                        Repaint();
                        return;
                    }
                    resize = false;
                    lastSize = size;

                }
            }
        }

        private void OnEnable()
        {
            this.autoRepaintOnSceneChange = true;
            EditorApplication.update += BackUpdate;
        }

        private void OnDisable()
        {
            EditorApplication.update -= BackUpdate;
            if (currentStep != Step.Finish && !_SkipTutorial() && !EditorWindow.focusedWindow == this)
            {
                FirstInstallWindow window = CreateInstance<FirstInstallWindow>();
                window.minSize = new Vector2(480, 360);
                window.position = position;
                window.titleContent = new GUIContent("CoInspector First Steps", CustomGUIContents.MainIconImage);
                window.currentStep = currentStep;
                window.ShowUtility();
            }
        }

        private void OnGUI()
        {
            GUILayout.BeginVertical(GUILayout.ExpandHeight(true));
            switch (currentStep)
            {
                case Step.Welcome:
                    DrawWelcomeScreen();
                    break;
                case Step.OpenWindow:
                    DrawOpenWindowScreen();
                    break;
                case Step.SelectGameObject:
                    DrawSelectGameObjectScreen();
                    break;
                case Step.SelectAsset:
                    DrawSelectAssetScreen();
                    break;
                case Step.SelectGameObject2:
                    DrawSelectGameObjectScreen2();
                    break;
                case Step.NavigateHistory:
                    DrawNavigateHistoryScreen();
                    break;
                case Step.SelectActiveTab:
                    DrawSelectActiveTabScreen();
                    break;
                case Step.Finish:
                    DrawFinishScreen();
                    break;
            }

        }



        bool DrawButton(string text, GUIContent content = null)
        {
            return DrawButton(text, Color.blue / 2, content);
        }
        private GUIContent continueButton;
        private GUIContent ContinueButton
        {
            get
            {
                if (continueButton == null)
                {
                    Texture2D texture = CustomGUIContents.ForwardContent.image as Texture2D;
                    continueButton = new GUIContent("Continue", texture);
                }
                return continueButton;
            }
        }

        private GUIContent selectAsset;
        private GUIContent SelectAsset
        {
            get
            {
                if (selectAsset == null)

                {
                    Texture2D texture = EditorGUIUtility.IconContent("AudioClip Icon").image as Texture2D;
                    selectAsset = new GUIContent("Select an Asset", texture);
                }
                return selectAsset;
            }
        }

        private GUIContent documentation;
        private GUIContent DocumentationContent
        {
            get
            {
                if (documentation == null)
                {
                    Texture2D texture = EditorGUIUtility.IconContent("UnityEditor.ConsoleWindow").image as Texture2D;
                    documentation = new GUIContent("Documentation", texture);
                }
                return documentation;
            }
        }

        private GUIContent openCoInspector;
        private GUIContent OpenCoInspector
        {
            get
            {
                if (openCoInspector == null)
                {
                    Texture2D texture = CustomGUIContents.MainIconImage;
                    openCoInspector = new GUIContent("Open CoInspector", texture);
                }
                return openCoInspector;
            }
        }

        private GUIContent clicktoContinue;
        private GUIContent ClickToContinue
        {
            get
            {
                if (clicktoContinue == null)
                {
                    Texture2D texture = CustomGUIContents.HierarchyContent.image as Texture2D;
                    clicktoContinue = new GUIContent("Click a GameObject", texture);
                }
                return clicktoContinue;
            }
        }

        void DrawExtraButton(Rect rect, GUIContent content, string text, int offset = 0, bool import = false, bool tight = false)
        {
            rect.y += rect.height - 35;
            rect.x = 80 + offset;
            rect.height = 22;
            rect.width = 23;
            if (tight)
            {
                rect.width = 20;
            }
            GUIContent _content = new GUIContent(content.image);
            Color color = GUI.backgroundColor;
            GUI.backgroundColor += Color.blue / 4;
            if (!EditorGUIUtility.isProSkin)
            {
                GUI.backgroundColor = Color.blue / 2;
            }
            if (GUI.Button(rect, _content, CustomGUIStyles.NoMarginButton))
            {
                if (import && CoInspectorWindow.MainCoInspector)
                {
                    CoInspectorWindow.MainCoInspector.showImportSettings = !CoInspectorWindow.MainCoInspector.showImportSettings;
                    CoInspectorWindow.MainCoInspector.DrawCurrentAssets();
                }
            }
            GUI.backgroundColor = color;
            CustomGUIContents.DrawCustomButton(rect);
            rect.x += 25;
            if (tight)
            {
                rect.x -= 2;
            }
            GUI.Label(rect, text);
        }

        void DrawAddButton(Rect rect, GUIContent content, string text, int offset = 0, bool import = false, bool tight = false)
        {
            rect.y += rect.height - 35;
            rect.x = 80 + offset;
            rect.height = 22;
            rect.width = 24;
            if (tight)
            {
                rect.width = 20;
            }
            GUIContent _content = new GUIContent(content.image);
            Color color = GUI.backgroundColor;
            GUI.backgroundColor += Color.green / 6;
            if (!EditorGUIUtility.isProSkin)
            {
                GUI.backgroundColor = Color.green / 2;
            }
            if (GUI.Button(rect, _content, CustomGUIStyles.NoMarginButton))
            {
                if (CoInspectorWindow.MainCoInspector)
                {
                    CoInspectorWindow.MainCoInspector.AddTabNext();
                    CoInspectorWindow.MainCoInspector.Repaint();
                }
            }
            GUI.backgroundColor = color;
            CustomGUIContents.DrawCustomButton(rect);
            rect.x += 25;
            if (tight)
            {
                rect.x -= 5;
            }
            GUI.Label(rect, text);
        }


        private GUIStyle messageLabelStyle;

        private GUIStyle MessageLabelStyle
        {
            get
            {
                if (messageLabelStyle == null)
                {
                    messageLabelStyle = new GUIStyle(EditorStyles.label)
                    {
                        fontSize = 12,
                        richText = true,
                        wordWrap = true,
                        alignment = TextAnchor.MiddleLeft
                    };
                }

                return messageLabelStyle;
            }
        }
        private GUIStyle messageLabelCentered;
        private GUIStyle MessageLabelCentered
        {
            get
            {
                if (messageLabelCentered == null)
                {
                    messageLabelCentered = new GUIStyle(EditorStyles.label)
                    {
                        fontSize = 12,
                        richText = true,
                        wordWrap = true,
                        alignment = TextAnchor.MiddleCenter
                    };
                }

                return messageLabelCentered;
            }
        }

        bool DrawButton(string text, Color color, GUIContent content = null)
        {
            var _text = content;
            bool doContent = content != null;
            bool doColor = color != Color.black;
            if (!doColor)
            {
                bool res = GUILayout.Button(text, GUILayout.Height(24), GUILayout.ExpandWidth(true), GUILayout.MinWidth(70));
                CustomGUIContents.DrawCustomButton();
                return res;
            }
            Color oldColor = GUI.backgroundColor;
            GUI.backgroundColor += color / 2;
            if (!EditorGUIUtility.isProSkin)
            {
                GUI.backgroundColor = color / 2.5f;
            }
            bool result;
            if (doContent)
            {
                if (_text.text == "Select an Asset")
                {
                    result = GUILayout.Button(_text, GUILayout.Height(24), GUILayout.ExpandWidth(true), GUILayout.Width(130));
                }
                else
                {
                    result = GUILayout.Button(_text, GUILayout.Height(24), GUILayout.ExpandWidth(true), GUILayout.MinWidth(70));
                }
            }
            else
            {
                result = GUILayout.Button(text, GUILayout.Height(24), GUILayout.ExpandWidth(true), GUILayout.MinWidth(70));
            }
            CustomGUIContents.DrawCustomButton();
            GUI.backgroundColor = oldColor;
            return result;
        }

        void DrawLogo(Rect rect)
        {
            GUI.Label(rect, CustomGUIContents.MainLogoImage, CustomGUIStyles.LogoStyle);
        }

        void StartMainSection()
        {
            EditorGUILayout.Space(5);
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.Space(10);
            EditorGUILayout.BeginVertical();
            EditorGUILayout.Space();
            Rect rect = EditorGUILayout.GetControlRect(GUILayout.Height(80), GUILayout.ExpandWidth(true));
            if (EditorGUIUtility.isProSkin)
            {
                EditorGUI.DrawRect(rect, new Color(0.8f, 0.8f, 0.8f, 1f));
            }
            else
            {
                EditorGUI.DrawRect(rect, new Color(0.9f, 0.9f, 0.9f, 1f));
                EditorUtils.DrawRectBorder(rect, Color.gray);
            }

            DrawLogo(rect);
            EditorGUILayout.Space(10);
        }
        void EndMainSection()
        {
            EditorGUILayout.Space();
            EditorGUILayout.EndVertical();
            EditorGUILayout.Space(10);
            EditorGUILayout.EndHorizontal();

        }
        void StartButtonSection()
        {
            EditorGUILayout.Space(10);
            EditorGUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
        }
        void EndButtonSection()
        {
            EditorGUILayout.Space();
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.Space(15);
            EditorGUILayout.EndVertical();
            if (Event.current.type == EventType.Repaint)
            {

                Rect rect = GUILayoutUtility.GetLastRect();
                Vector2 size = new Vector2(rect.width, rect.height);
                Resize(size);
            }
            GUILayout.FlexibleSpace();
        }

        void LabelField(string text)
        {
            EditorGUILayout.LabelField(text, MessageLabelStyle);
        }
        GUIStyle bigLabelStyle;
        GUIStyle BigLabelStyle
        {
            get
            {
                if (bigLabelStyle == null)
                {
                    bigLabelStyle = new GUIStyle(MessageLabelStyle)
                    {
                        fontSize = 14
                    };
                }
                return bigLabelStyle;
            }
        }

        GUIStyle bigLabelCenteredStyle;
        GUIStyle BigLabelCenteredStyle
        {
            get
            {
                if (bigLabelCenteredStyle == null)
                {
                    bigLabelCenteredStyle = new GUIStyle(MessageLabelCentered)
                    {
                        fontSize = 14
                    };
                }
                return bigLabelCenteredStyle;
            }
        }

        void BigLabelField(string text)
        {
            EditorGUILayout.LabelField(text, BigLabelStyle);
        }

        void BigLabelFieldCentered(string text)
        {
            EditorGUILayout.LabelField(text, BigLabelCenteredStyle);
        }

        void LabelFieldCentered(string text)
        {
            EditorGUILayout.LabelField(text, MessageLabelCentered);
        }

        void LineSpace()
        {
            EditorGUILayout.Space(10);
        }

        void OnDestroy()
        {
            CoInspectorWindow.showInstallMessage = false;
            if (CoInspectorWindow.MainCoInspector)
            {
                CoInspectorWindow.MainCoInspector.SaveSession();
            }
            else
            {
                TrySave();
            }
        }

        bool _SkipTutorial()
        {
            return SkipTutorial(true);
        }

        private void DrawWelcomeScreen()
        {
            StartMainSection();
            BigLabelField("<b>Thanks for installing CoInspector!</b>");
            EditorGUILayout.Space();
            CustomGUIStyles.HelpBox("CoInspector allows you to work on Assets and multiple Tabbed GameObjects at the same time.\n\n<b>Let's get started by opening a CoInspector Window.</b>\n\n<i>(We promise this won't take too long)</i>", false, true);
            StartButtonSection();
            if (DrawButton("Skip this thing", Color.red))
            {
                SkipTutorial();
            }
            GUILayout.Space(10);

            if (DrawButton("Open CoInspector", OpenCoInspector))
            {
                {
                    CoInspectorWindow.ShowWindow();
                    CoInspectorWindow.tabPreviewExpanded = false;
                }
                TipsManager.ResetTips();
                CoInspectorWindow.MainCoInspector?.CleanTabs();
                if (CoInspectorWindow.MainCoInspector)
                {
                    CoInspectorWindow.MainCoInspector.tracker = new GameObjectTracker();
                    CoInspectorWindow.MainCoInspector.CloseAssetView();
                    CoInspectorWindow.MainCoInspector.targetObject = null;
                    CoInspectorWindow.MainCoInspector.targetObjects = null;
                    CoInspectorWindow.MainCoInspector.targetGameObject = null;
                    if (CoInspectorWindow.MainCoInspector.historyAssets != null)
                    {
                        CoInspectorWindow.MainCoInspector.historyAssets.Clear();
                    }
                    CoInspectorWindow.MainCoInspector.showImportSettings = false;
                    CoInspectorWindow.MainCoInspector.UpdateCurrentTip();
                }                
                currentStep = Step.OpenWindow;
                resize = true;
            }
            EndMainSection();
            EndButtonSection();

        }

        private void OnSelectionChange()
        {
            if (currentStep == Step.OpenWindow && CoInspectorWindow.MainCoInspector?.targetGameObject != null)
            {               
                    CoInspectorWindow.MainCoInspector?.Focus();
                    Focus();
                    currentStep = Step.SelectGameObject;
                    resize = true;
                    Repaint();                
            }
             if (currentStep == Step.SelectGameObject2 && CoInspectorWindow.MainCoInspector?.targetObject != null)
            {
                CoInspectorWindow.MainCoInspector?.Focus();
                Focus();
                currentStep = Step.SelectAsset;
                resize = true;
            }
        }

        private void DrawOpenWindowScreen()
        {
            StartMainSection();
            BigLabelField("<b>Getting started with CoInspector</b>  <i>(0/4)</i>");
            EditorGUILayout.Space(10);
            CustomGUIStyles.HelpBox("<b>Nice!</b>\n\nNow, let's <b>select a GameObject in the Hierarchy</b>.\n\n<i>(It may also be a good idea to dock CoInspector somewhere handy.)</i>", false, true);
            StartButtonSection();
            GUI.enabled = false;
            DrawButton("Click a GameObject to continue", ClickToContinue);
            GUI.enabled = true;
            EndMainSection();
            EndButtonSection();

        }

        private void DrawSelectGameObjectScreen()
        {
            StartMainSection();
            BigLabelField("<b>Getting started with CoInspector</b>  <i>(1/4)</i>");
            EditorGUILayout.Space(10);
            CustomGUIStyles.HelpBox("<b>Good!</b> So, <b>GameObjects</b> will show up there.\n\n<b>Create Tabs</b> with the <b>Add Tab Button</b>* or by <b>middle-clicking</b> GameObjects in the Hierarchy.\n\nFeel free to <b>reorder Tabs</b>, <b>close them</b>, <b>duplicate them</b> and so on.\n\nTo see more Tab-related options, right-click any of them!\n\n", false, true);
            DrawAddButton(GUILayoutUtility.GetLastRect(), CustomGUIContents.AddContent, "*", 0, false);
            StartButtonSection();
            if (DrawButton("Continue", ContinueButton))
            {
                Focus();
                currentStep = Step.SelectGameObject2;
                resize = true;
            }
            EndMainSection();
            EndButtonSection();
        }

        private void DrawSelectGameObjectScreen2()
        {
            StartMainSection();
            BigLabelField("<b>Getting started with CoInspector</b>  <i>(2/4)</i>");
            EditorGUILayout.Space(10);
            CustomGUIStyles.HelpBox("<b>Now, press the button bellow and we'll select an Asset for you.</b>\n\n<i>(You can also just click any Asset yourself.)</i>", false, true);
            StartButtonSection();
            if (DrawButton("Select an Asset", SelectAsset))
            {
                AudioClip clip = CustomGUIContents.LoadCustomAsset("/Custom/Example Assets/FX_door.wav") as AudioClip;
                if (clip != null)
                {
                    CoInspectorWindow.MainCoInspector?.Focus();
                    CoInspectorWindow.MainCoInspector?.SetTargetAsset(clip);
                }
                clip = CustomGUIContents.LoadCustomAsset("/Custom/Example Assets/FX_tada.wav") as AudioClip;
                if (clip != null)
                {
                    CoInspectorWindow.MainCoInspector?.Focus();
                    CoInspectorWindow.MainCoInspector?.SetTargetAsset(clip);
                    currentStep = Step.SelectAsset;
                }
            }
            EndMainSection();
            EndButtonSection();          

        }

        private void DrawSelectAssetScreen()
        {
            StartMainSection();
            BigLabelField("<b>Getting started with CoInspector</b>  <i>(2/4)</i>");
            EditorGUILayout.Space(10);
            CustomGUIStyles.HelpBox("Look at that! <b>Assets</b> will show up down there.\n\nResize the <b>Asset View</b> by dragging the <b>Add Component Bar</b> or minimize it by clicking the blue <b>Asset Bar</b>. Give it a try!\n\nTo show all Import Settings, use the <b>Import Settings Button*</b>.\n\n", false, true);
            DrawExtraButton(GUILayoutUtility.GetLastRect(), CustomGUIContents.ShowImport, "*", 0, true);
            StartButtonSection();
            if (DrawButton("Continue", ContinueButton))
            {
                Focus();
                currentStep = Step.NavigateHistory;
                resize = true;
            }
            EndMainSection();
            EndButtonSection();


        }
        private void DrawNavigateHistoryScreen()
        {
            StartMainSection();
            BigLabelField("<b>Getting started with CoInspector</b>  <i>(3/4)</i>");
            EditorGUILayout.Space(10);
            CustomGUIStyles.HelpBox("Almost there! Let's talk <b>History Tracking</b>!\n\nYou can navigate a Tab's History with the <b>Back/Forward Buttons</b>*.\n\nFor <b>Assets</b>, use the <b>History Button</b> next to <b>Add Component</b>**.\n\n", false, true);
            DrawExtraButton(GUILayoutUtility.GetLastRect(), CustomGUIContents.BackContent, "", 0);
            DrawExtraButton(GUILayoutUtility.GetLastRect(), CustomGUIContents.ForwardContent, "*", 22, false);
            DrawExtraButton(GUILayoutUtility.GetLastRect(), CustomGUIContents.HistoryButton, "**", 70, false, true);
            StartButtonSection();
            if (DrawButton("Continue", ContinueButton))
            {
                Focus();
                currentStep = Step.SelectActiveTab;
                resize = true;
            }
            EndMainSection();
            EndButtonSection();
        }

        private void DrawSelectActiveTabScreen()
        {
            StartMainSection();
            BigLabelField("<b>Getting started with CoInspector</b>  <i>(4/4)</i>");
            EditorGUILayout.Space(10);
            CustomGUIStyles.HelpBox("<b>And, finally:</b>\n\nCoInspector <b>does not need to select GameObjects in the Hierarchy</b> when switching Tabs.\n\n<b>Would you prefer it to?</b>\n\n<i>(You can change this later in the Settings Window.)</i>", false, true);
            StartButtonSection();
            if (DrawButton("Select Tabs in Hierarchy"))
            {
                Focus();
                CoInspectorWindow.softSelection = false;
                CoInspectorWindow.MainCoInspector.SaveSettings();
                currentStep = Step.Finish;
                resize = true;
            }
            EditorGUILayout.Space(10);
            if (DrawButton("Don't select in Hierarchy", Color.gray))
            {
                Focus();
                CoInspectorWindow.softSelection = true;
                CoInspectorWindow.MainCoInspector.SaveSettings();
                currentStep = Step.Finish;
                resize = true;
            }
            EndMainSection();
            EndButtonSection();
        }

        void DoSkip(bool force = false)
        {
            if (force)
            {
                return;
            }
            CoInspectorWindow.tabPreviewExpanded = true;
            Close();
        }

        bool SkipTutorial(bool force = false)
        {
            if (alreadySaidBye)
            {
                return true;
            }
            alreadySaidBye = true;
            if (EditorUtility.DisplayDialog("Skip Tutorial", "Are you sure want to skip the tutorial?\n\nIt's not exactly long, and it may help you get started", "YES", "Don't skip it"))
            {
                if (!CoInspectorWindow.MainCoInspector)
                {
                    if (EditorUtility.DisplayDialog("Skip Tutorial", "Open CoInspector before leaving?\n\nYou can always open it in the 'Window/CoInspector' menu section", "Open CoInspector", "No"))
                    {
                        CoInspectorWindow.ShowWindow();
                        DoSkip(force);
                        return true;
                    }
                    else
                    {
                        DoSkip(force);
                        return true;
                    }
                }
                else
                {
                    DoSkip(force);
                    return true;
                }
            }
            else
            {
                alreadySaidBye = false;
                return false;
            }
        }

        private void DrawInstalledTextures()
        {
            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            GUILayout.Label(CustomGUIContents.Installed_1, GUILayout.Width(60), GUILayout.Height(60));
            GUILayout.Label(CustomGUIContents.Installed_2, GUILayout.Width(60), GUILayout.Height(60));
            GUILayout.Label(CustomGUIContents.Installed_3, GUILayout.Width(60), GUILayout.Height(60));
            GUILayout.Label(CustomGUIContents.Installed_4, GUILayout.Width(60), GUILayout.Height(60));
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();
        }

        private void DrawFinishScreen()
        {
            StartMainSection();
            BigLabelField("<b>And that was it! Thank you for your time.</b>");
            DrawInstalledTextures();
            CustomGUIStyles.HelpBox("CoInspector still has more features, like <b>Scene Sessions</b>, <b>Middle-click scrolling</b>, <b>Component Cloning</b>, <b>Play Mode Save for Transforms</b>, the <b>Local Hierarchy View</b>… but we're sure you'll learn about them eventually.\n\nJust check the <b>Documentation</b> and the <b>Settings Window</b> to learn all CoInspector can do for you.", false, true);
            StartButtonSection();

            if (DrawButton("Check Documentation", DocumentationContent))
            {
                UnityEngine.Object obj = CustomGUIContents.LoadCustomAsset("/Documentation/Manual.pdf", true);
                if (obj != null)
                {
                    CoInspectorWindow.OpenAsset(obj);
                }
            }
            EditorGUILayout.Space(5);
            if (DrawButton("Open Settings", CustomGUIContents.SettingsContent))
            {
                SettingsWindow.ShowWindow();
            }
            EditorGUILayout.Space(5);
            if (DrawButton("Just let me work at once", Color.red))
            {
                CoInspectorWindow.tabPreviewExpanded = true;
                Close();
            }
            EndMainSection();
            EndButtonSection();
        }
        
    }
    [InitializeOnLoad]
    internal static class CoInspectorInitializer
    {
        static CoInspectorInitializer()
        {
            if (!SessionState.GetBool("FirstInitDone", false))
            {
                CoInspectorWindow.justOpened = true;
                SessionState.SetBool("FirstInitDone", true);
            }

            EditorApplication.delayCall += () =>
            {
                try
                {
                    InitializeUserData();
                }
                catch (Exception e)
                {
                    Debug.LogError($"CoInspector: Error during delayed initialization: {e.Message}\n{e.StackTrace}");
                    EditorApplication.delayCall += InitializeUserData;
                }
            };
        }

        private static void InitializeUserData()
        {
            UserSaveData userData = CoInspectorWindow.FindSettingsObject();

            if (userData != null)
            {
                userData.LoadData();
            }

            EditorApplication.delayCall += OpenFirstInstallWindow;
        }

        internal static void OpenFirstInstallWindow()
        {
            try
            {
                CoInspectorWindow[] coInspectors = Resources.FindObjectsOfTypeAll<CoInspectorWindow>();
                if (coInspectors.Length > 0)
                {
                    foreach (CoInspectorWindow coInspector in coInspectors)
                    {
                        coInspector.ignoreNextSelection = true;
                        coInspector.UpdateAllWidths(); 
                    }
                }
               
                var userData = CoInspectorWindow.FindSettingsObject();
                if (userData == null)
                {
                    FirstInstallWindow.userSaveData = CoInspectorWindow._AutoCreateSettings();
                    if (FirstInstallWindow.userSaveData)
                    {
                        if (UpdateChecker.HasValidChangelog())
                       {
                            CoInspectorWindow.userInstalls = CoInspectorWindow.userInstalls.Concat(UpdateChecker.currentVersion);
                        }
                        FirstInstallWindow.userSaveData.SaveData(true);
                        FirstInstallWindow.ShowWindow();
                    }
                }
                else 
                {
                   bool newVersion = !CoInspectorWindow.userInstalls.Contains(UpdateChecker.currentVersion);             
                    if (userData && newVersion && UpdateChecker.HasValidChangelog())
                    {                           
                        CoInspectorWindow.userInstalls = CoInspectorWindow.userInstalls.Concat(UpdateChecker.currentVersion);
                        userData.SaveData(true);
                        if (UpdateChecker.autoOpenChangeLog)                        
                        {
                            ChangelogPopupWindow.ShowAsPopup();
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"CoInspector: Error opening first install window: {ex.Message}\n{ex.StackTrace}");
            }
        }
    }
}