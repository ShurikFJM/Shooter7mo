using UnityEngine;
using UnityEditor;
using System;
using System.Collections.Generic;
using UnityEngine.UIElements;

namespace CoInspector
{
    public class ChangelogData
    {
        [Serializable]
        public class ChangelogEntry
        {
            public string section;
            public string title;
            public string description;
            public bool important;
        }

        [Serializable]
        public class Changelog
        {
            public string version;
            public string date;
            public List<ChangelogEntry> entries;
        }
    }

    public class ChangelogPopupWindow : EditorWindow
    {
        private ChangelogData.Changelog changelog;
        private bool showFullChangelog = false;
        private SmoothScrollView scrollView;
        private VisualElement sectionContainer;

        public static void ShowAsPopup()
        {
            if (CustomGUIContents.ValidChangelog == null)
            {
                return;
            }
            ChangelogPopupWindow window = GetWindow<ChangelogPopupWindow>(true);
            window.titleContent = new GUIContent("CoInspector Changelog");

            Vector2 size = new Vector2(550, 420);
           /* window.position = new Rect(
                (Screen.currentResolution.width - size.x) / 2,
                (Screen.currentResolution.height - size.y) / 2,
                size.x, size.y);*/

            window.minSize = new Vector2(400, 400);
            window.ShowUtility();
        }

  
        public void CreateGUI()
        {
            changelog = CustomGUIContents.ValidChangelog;
            BuildUI();
            //scrollView.ApplySoftScrolling(this);
            rootVisualElement.RegisterCallback<WheelEvent>(evt =>
            {
                if (scrollView.IsScrollbarVisible())
                {
                    scrollView.scrollOffset = new Vector2(
                    scrollView.scrollOffset.x,
                    scrollView.scrollOffset.y + evt.delta.y * 20);
                    scrollView.MarkDirtyRepaint();
                    evt.StopPropagation();
                }
            });
        }


        private void BuildUI()
        {
            var root = rootVisualElement;
            root.AddPreviewBackground();

            var mainContainer = new VisualElement();
            mainContainer.style.flexGrow = 1;
            root.Add(mainContainer);

            var headerContainer = new VisualElement();
            headerContainer.style.backgroundColor = EditorUtils.IsLightSkin()?new Color(0.45f, 0.45f, 0.45f):new Color(0.27f, 0.27f, 0.27f);
            headerContainer.style.height = 70;
            headerContainer.style.flexDirection = FlexDirection.Row;
            headerContainer.style.alignItems = Align.Center;
            headerContainer.style.justifyContent = Justify.Center;
            EditorUtils.SetStaticElement(headerContainer);

            var iconLogo = new Image();
            iconLogo.image = CustomGUIContents.SmallLogoImage;
            iconLogo.style.width = 50;
            iconLogo.style.height = 50;
            iconLogo.style.marginRight = 10;

            headerContainer.Add(iconLogo);

            var titleLabel = new Label($"What's new in CoInspector v{UpdateChecker.currentVersion}");
            titleLabel.style.fontSize = 18;
            titleLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
            titleLabel.style.color = Color.white;
            headerContainer.Add(titleLabel);

            mainContainer.Add(headerContainer);
            mainContainer.Add(EditorUtils.HorizontalLine());

            scrollView = new SmoothScrollView(ScrollViewMode.Vertical);
            scrollView.style.flexGrow = 1;
            mainContainer.Add(scrollView);

            sectionContainer = new VisualElement();
            sectionContainer.style.flexGrow = 1;
            sectionContainer.style.paddingRight = 15;
            scrollView.Add(sectionContainer);

            RefreshContent();

            var separatorContainer = new VisualElement();
            separatorContainer.style.height = 2;
            separatorContainer.style.flexDirection = FlexDirection.Row;
            if (!EditorUtils.IsLightSkin())
            {
                mainContainer.Add(EditorUtils.HorizontalLine());
            }            
            mainContainer.Add(separatorContainer);

            var line = new VisualElement();
            line.style.flexGrow = 1;
            line.style.height = 1;
            line.style.backgroundColor = EditorUtils.IsLightSkin()?new Color(0.1f, 0.1f, 0.1f):new Color(0.35f, 0.35f, 0.35f);
            separatorContainer.Add(line);

            var footerContainer = new VisualElement();
            footerContainer.style.height = 50;
            footerContainer.style.backgroundColor = EditorUtils.IsLightSkin()?new Color(0.48f, 0.48f, 0.48f):new Color(0.28f, 0.28f, 0.28f);

            footerContainer.style.justifyContent = Justify.Center;
            footerContainer.style.alignItems = Align.Center;
            mainContainer.Add(footerContainer);
            EditorUtils.SetStaticElement(footerContainer);


            var buttonContainer = new VisualElement();
            buttonContainer.style.flexDirection = FlexDirection.Row;
            buttonContainer.style.justifyContent = Justify.Center;
            buttonContainer.style.width = new Length(100, LengthUnit.Percent);
            footerContainer.Add(buttonContainer);

            Button toggleButton = null;
            toggleButton = new Button(() =>
            {
                showFullChangelog = !showFullChangelog;
                toggleButton.text = showFullChangelog ? "Show Important Only" : "Full Changelog";
                RefreshContent();
            });
            toggleButton.text = "Full Changelog";
            toggleButton.style.marginLeft = 10;
            toggleButton.style.marginRight = 10;
            toggleButton.style.height = 30;
            toggleButton.style.width = 150;
            toggleButton.style.fontSize = 12;
            buttonContainer.Add(toggleButton);

            var closeButton = new Button(() =>
            {
                Close();
            });
            closeButton.text = "Ok, nice";
            closeButton.style.marginLeft = 10;
            closeButton.style.marginRight = 10;
            closeButton.style.height = 30;
            closeButton.style.width = 80;
            closeButton.style.fontSize = 12;
            closeButton.style.color = Color.white;

            closeButton.style.backgroundColor = new Color(0.40f, 0.40f, 0.55f);
            closeButton.RegisterCallback<MouseEnterEvent>((evt) =>
            {
                closeButton.style.backgroundColor = new Color(0.45f, 0.45f, 0.60f);
            });
            closeButton.RegisterCallback<MouseLeaveEvent>((evt) =>
            {
                closeButton.style.backgroundColor = new Color(0.40f, 0.40f, 0.55f);
            });
            buttonContainer.Add(closeButton);
        }

        private void RefreshContent()
        {
            sectionContainer.Clear();

            if (changelog == null || changelog.entries == null)
            {
                ShowErrorMessage();
                return;
            }

            bool hasContent = false;
            
            hasContent |= DisplaySectionEntries("new", "Added", !showFullChangelog);
            hasContent |= DisplaySectionEntries("changed", "Changed", !showFullChangelog);
            hasContent |= DisplaySectionEntries("fixed", "Fixed", !showFullChangelog);
            
            if (!hasContent)
            {
                ShowErrorMessage();
            }
        }

        private void ShowErrorMessage()
        {
            var messageContainer = new VisualElement();
            messageContainer.style.flexGrow = 1;
            messageContainer.style.justifyContent = Justify.Center;
            messageContainer.style.alignItems = Align.Center;
            messageContainer.style.paddingLeft = 20;
            messageContainer.style.paddingRight = 20;
            messageContainer.style.paddingTop = 50;
            
            var messageLabel = new Label("Okay, so there was supposed to be a changelog here, but something went wrong.\n\n\nPlease, close this window so we can pretend this never happened.");
            messageLabel.style.whiteSpace = WhiteSpace.Normal;
            messageLabel.style.unityTextAlign = TextAnchor.MiddleCenter;
            messageLabel.style.fontSize = 14;
            messageLabel.style.color = new Color(0.9f, 0.9f, 0.9f);
            
            messageContainer.Add(messageLabel);
            sectionContainer.Add(messageContainer);
        }

        private bool DisplaySectionEntries(string sectionType, string sectionTitle, bool importantOnly)
        {
            var sectionEntries = changelog.entries.FindAll(e =>
                e.section.Equals(sectionType, StringComparison.OrdinalIgnoreCase) &&
                (!importantOnly || e.important));

            if (sectionEntries.Count == 0)
                return false;

            var sectionHeader = new VisualElement();
            sectionHeader.style.height = 35;

            var sectionLabel = new Label(sectionTitle);
            sectionLabel.style.fontSize = 15;
            sectionLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
            sectionLabel.style.color = EditorUtils.IsLightSkin()?Color.black:Color.white;
            sectionLabel.style.paddingLeft = 15;
            sectionLabel.style.paddingTop = 8;
            sectionLabel.style.paddingBottom = 8;

            sectionHeader.Add(sectionLabel);
            sectionContainer.Add(sectionHeader);

            var entriesContainer = new VisualElement();
            entriesContainer.style.paddingRight = 5;
            sectionContainer.Add(entriesContainer);

            foreach (var entry in sectionEntries)
            {
                var entryContainer = new VisualElement();

                var titleContainer = new VisualElement();
                titleContainer.style.flexDirection = FlexDirection.Row;
                titleContainer.style.minHeight = 20;

                var bulletPoint = new Label(entry.important?"•":"<i>*</i>");
                bulletPoint.style.fontSize = 11;
                bulletPoint.style.color = EditorUtils.IsLightSkin()?Color.black:Color.white;
                bulletPoint.style.width = 10;
                bulletPoint.style.marginLeft = 20;
                bulletPoint.style.paddingTop = 0;
                titleContainer.style.paddingBottom = 3;
                titleContainer.Add(bulletPoint);

                var titleLabel = new Label(entry.title);
                if (!entry.important)
                {
                    string colorString = EditorUtils.IsLightSkin()?"434360":"B7D0EB";
                    titleLabel.text = $"<i><color=#{colorString}>{titleLabel.text}</color></i>";
                }
                if (EditorUtils.IsLightSkin())
                {
                    titleLabel.text = "<b>" + titleLabel.text + "</b>";
                }
                titleLabel.style.fontSize = 12;               
                titleLabel.style.flexGrow = 1;
                titleLabel.style.paddingTop = 0;
                titleLabel.style.paddingRight = 10;
                titleLabel.style.marginRight = 10;
                titleLabel.style.whiteSpace = WhiteSpace.Normal;
                titleLabel.style.flexWrap = Wrap.Wrap;
                titleContainer.Add(titleLabel);
                entryContainer.Add(titleContainer);

                if (!string.IsNullOrEmpty(entry.description))
                {
                    var descriptionLabel = new Label(entry.description);
                    descriptionLabel.style.fontSize = 11;
                    descriptionLabel.style.color = new Color(0.9f, 0.9f, 0.9f);
                    descriptionLabel.style.whiteSpace = WhiteSpace.Normal;
                    descriptionLabel.style.paddingRight = 15;
                    entryContainer.Add(descriptionLabel);
                }

                entriesContainer.Add(entryContainer);
            }
            
            return true;
        }
    }
}