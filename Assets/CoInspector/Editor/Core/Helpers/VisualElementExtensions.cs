using UnityEngine;
using UnityEngine.UIElements;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using UnityEditor;
using UnityEditor.UIElements;
using System;

namespace CoInspector
{

    public static class VisualElementExtensions
    {
        internal static Dictionary<VisualElement, VisualElement> activeFloatingTexts = new Dictionary<VisualElement, VisualElement>();


        public static bool CanBeScrolled(this ScrollView scrollView)
        {
            if (scrollView == null)
            {
                return false;
            }
            bool isScrollbarVisible = scrollView.verticalScroller != null &&
                                      scrollView.verticalScroller.style.display == DisplayStyle.Flex;

            if (!isScrollbarVisible)
            {
                return false;
            }
            float scrollRange = scrollView.verticalScroller.highValue - scrollView.verticalScroller.lowValue;
            return scrollRange > 0;
        }

        public static void LimitScrollbarVisibilityTo(this ScrollView scrollView, int threshold = 1)
        {
            if (scrollView == null)
            {
                return;
            }
            scrollView.UnregisterCallback<GeometryChangedEvent>(OnGeometryChanged);
            UpdateScrollbarVisibility(scrollView, threshold);

            void OnGeometryChanged(GeometryChangedEvent evt)
            {
                UpdateScrollbarVisibility(scrollView, threshold);
            }
            scrollView.RegisterCallback<GeometryChangedEvent>(OnGeometryChanged);

            UpdateScrollbarVisibility(scrollView, threshold);
        }

        internal static void UpdateScrollbarVisibility(this ScrollView scrollView, int threshold)
        {
            if (scrollView == null || scrollView.verticalScroller == null)
            {
                return;
            }
            bool canScroll = scrollView.verticalScroller.highValue - scrollView.verticalScroller.lowValue > threshold;

            if (scrollView.verticalScrollerVisibility != (canScroll ? ScrollerVisibility.Auto : ScrollerVisibility.Hidden))
            {
                //Debug.Log("Fixed scrollbar visibility");
                scrollView.verticalScrollerVisibility = canScroll ? ScrollerVisibility.Auto : ScrollerVisibility.Hidden;
            }
        }
        internal static bool IsScrollbarVisible(this ScrollView scrollView)
        {

            VisualElement verticalScroller = scrollView.verticalScroller;
            if (verticalScroller != null)
            {
                return verticalScroller.style.display == DisplayStyle.Flex;
            }

            return true;
        }

        internal static void FixScrollbarVisibility(this ScrollView scrollView, int attempts = 10)
        {
            if (scrollView == null || scrollView.verticalScroller == null)
            {
                return;
            }
            UpdateScrollbarVisibility(scrollView, 1);
            attempts -= 1;
            if (attempts > 0)
            {
                scrollView.schedule.Execute(() =>
                {
                    FixScrollbarVisibility(scrollView, attempts);
                }).StartingIn(0);
            }
        }

        internal static void ScrollToTop(this ScrollView scrollView)
        {
            scrollView.schedule.Execute(() =>
                {
                    scrollView.verticalScroller.value = 0;
                    scrollView.MarkDirtyRepaint();
                });
        }
        internal static void ScrollToName(this ScrollView scrollView, string name, bool alsoFocus = false, bool added = false)
        {
            var element = scrollView.Q<VisualElement>(name);
            if (element != null)
            {
                scrollView.schedule.Execute(() =>
                    {
                        scrollView.ScrollToElement(element, alsoFocus, added);
                        scrollView.MarkDirtyRepaint();
                    });
            }
        }
        internal static void ScrollToElement(this ScrollView scrollView, string elementName, bool alsoFocus = false, bool added = false)
        {
            scrollView.contentContainer.MarkDirtyRepaint();
            scrollView.schedule.Execute(() =>
            {
                scrollView.schedule.Execute(() =>
                {
                    var element = scrollView.GetChild(elementName);
                    if (element != null)
                    {
                        scrollView.CenterElementInView(element);
                        scrollView.MarkDirtyRepaint();
                        if (alsoFocus)
                        {
                            Color color = added ? CustomColors.CustomGreen : CustomColors.CustomBlue;
                            ComponentHighlighter.StartHighlight(element.name, color);
                        }
                    }
                });
            });
        }
        internal static void ScrollToBottom(this ScrollView scrollView)
        {
            scrollView.schedule.Execute(() =>
            {
                bool canScroll = scrollView.verticalScroller.highValue - scrollView.verticalScroller.lowValue > 1;
                if (!canScroll)
                {
                    return;
                }
                scrollView.verticalScroller.value = scrollView.verticalScroller.highValue;
                scrollView.MarkDirtyRepaint();
            });

        }
        internal static void ScrollToElement(this ScrollView scrollView, VisualElement element, bool alsoFocus = false, bool added = false)
        {
            if (element != null)
            {
                scrollView.contentContainer.MarkDirtyRepaint();
                scrollView.schedule.Execute(() =>
                {
                    scrollView.schedule.Execute(() =>
               {
                   scrollView.CenterElementInView(element);
                   scrollView.MarkDirtyRepaint();
                   if (alsoFocus)
                   {
                       Color color = added ? CustomColors.CustomGreen : CustomColors.CustomBlue;
                       ComponentHighlighter.StartHighlight(element.name, color);
                   }
               });
                });
            }
        }

        internal static void Remove(this VisualElement visualElement, string name)
        {
            var existingElement = visualElement.GetChild(name);
            if (existingElement != null)
            {
                visualElement.Remove(existingElement);
            }
        }

        internal static void Replace(this VisualElement visualElement, VisualElement newElement)
        {
            if (visualElement == null || newElement == null)
            {
                return;
            }
            visualElement.Remove(newElement.name);
            visualElement.Add(newElement);
        }
        internal static void ReplaceAndInsertFirst(this VisualElement visualElement, VisualElement newElement)
        {
            if (visualElement == null || newElement == null)
            {
                return;
            }
            visualElement.Remove(newElement.name);
            visualElement.Insert(0, newElement);
        }
        internal static void RemoveIfPresent(this VisualElement visualElement, string name)
        {
            if (visualElement == null)
            {
                return;
            }
            var existingElement = visualElement.GetChild(name);
            if (existingElement != null)
            {
                visualElement.Remove(existingElement);
            }
        }

        public static void RegisterOnce<TEventType>(this VisualElement element, EventCallback<TEventType> callback) where TEventType : EventBase<TEventType>, new()
        {
            EventCallback<TEventType> wrappedCallback = null;
            wrappedCallback = evt =>
            {
                element.UnregisterCallback(wrappedCallback);
                callback(evt);
            };
            element.RegisterCallback(wrappedCallback);
        }

        internal static void AddPreviewBackground(this VisualElement visualElement)
        {
#if UNITY_2022_3_OR_NEWER

            if (visualElement == null)
            {
                return;
            }
            visualElement.style.backgroundImage = CustomGUIContents.PreviewBackground;
            visualElement.style.backgroundRepeat = new BackgroundRepeat(Repeat.Repeat, Repeat.Repeat);
            visualElement.style.backgroundSize = new BackgroundSize(72, 72);
#endif
        }

        public static int GetChildrenComponentCount(this VisualElement element)
        {
            int count = 0;
            var children = element.Children();
            foreach (var child in children)
            {
                if (child.name == "ComponentElement" + count)
                {
                    count++;
                }
                else
                {
                    break;
                }
            }
            return count;
        }
        public static void SetPickingModeRecursively(this VisualElement element, bool mode)
        {
            if (element == null) return;

            element.pickingMode = mode ? PickingMode.Position : PickingMode.Ignore;

            if (element.childCount > 0)
            {
                foreach (var child in element.Children())
                {
                    child.SetPickingModeRecursively(mode);
                }
            }
        }
        public static List<VisualElement> GetChildrenComponents(this VisualElement element)
        {
            List<VisualElement> components = new List<VisualElement>();
            var children = element.Children();
            int count = 0;
            foreach (var child in children)
            {
                if (child.name.Contains("ComponentElement" + count))
                {
                    count++;
                    components.Add(child);
                }
            }
            return components;
        }
        public static void ReplaceCallback<TEventType>(this VisualElement element, EventCallback<TEventType> callback, TrickleDown trickleDown = TrickleDown.NoTrickleDown) where TEventType : EventBase<TEventType>, new()
        {
            element.UnregisterCallback(callback, trickleDown);
            element.RegisterCallback(callback, trickleDown);
        }
        public static void ForceUpdate(this VisualElement element)
        {
            float width = element.style.marginRight.value.value;
            element.schedule.Execute(() =>
                {
                    var placeholder = Rect.zero;
                    var actualRect = element.layout;

                    using var evt = GeometryChangedEvent.GetPooled(placeholder, actualRect);
                    evt.target = element.contentContainer;
                    element.contentContainer.SendEvent(evt);
                    element.MarkDirtyRepaint();
                });
        }


        internal static void PlaceBefore(this VisualElement element, VisualElement root, string target)
        {
            if (element == null || root == null)
            {
                return;
            }
            VisualElement targetElement = root.GetChild(target);

            if (targetElement == null)
            {
                root.Add(element);
            }
            else
            {
                root.Insert(root.IndexOf(targetElement), element);
            }

        }
        internal static void AddFirst(this VisualElement visualElement, VisualElement newElement)
        {
            if (newElement == null)
            {
                return;
            }
            visualElement.Insert(0, newElement);
        }

        internal static void MoveLast(this VisualElement visualElement)
        {
            if (visualElement == null || visualElement.parent == null)
            {
                return;
            }

            VisualElement parent = visualElement.parent;
            parent.Remove(visualElement);
            parent.Add(visualElement);
        }

        public static void CreateFloatingText(
            this VisualElement visualElement,
            string message,
            Vector2? position = null,
            Color? color = null,
            float duration = 1.4f
        )
        {
            VisualElement root = visualElement;
            VisualElement overlay = root.Q<VisualElement>("FloatingTextOverlay");
            if (overlay == null)
            {
                overlay = new VisualElement();
                overlay.name = "FloatingTextOverlay";
                overlay.style.position = Position.Absolute;
                overlay.pickingMode = PickingMode.Ignore;
                overlay.style.top = 0;
                overlay.style.left = 0;
                overlay.style.width = Length.Percent(100);
                overlay.style.height = Length.Percent(100);
                root.Add(overlay);
            }
            else
            {
                overlay.MoveLast();
            }

            var container = new VisualElement();
            container.name = "FloatingTextContainer";
            container.style.position = Position.Absolute;
            container.pickingMode = PickingMode.Ignore;
            overlay.Add(container);

            var shadowLabel = new Label(message)
            {
                name = "FloatingTextShadow",
                pickingMode = PickingMode.Ignore
            };
            shadowLabel.style.position = Position.Absolute;
            shadowLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
            shadowLabel.style.color = Color.black;
            shadowLabel.style.opacity = 1f;
            shadowLabel.style.fontSize = 14;
            shadowLabel.style.unityTextAlign = TextAnchor.MiddleCenter;
            shadowLabel.style.width = StyleKeyword.Auto;
            shadowLabel.style.height = StyleKeyword.Auto;

            var mainLabel = new Label(message)
            {
                name = "FloatingText",
                pickingMode = PickingMode.Ignore
            };
            mainLabel.style.position = Position.Absolute;
            mainLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
            mainLabel.style.color = color ?? new Color(1f, 1, 0.75f);
            mainLabel.style.opacity = 1f;
            mainLabel.style.fontSize = 14;
            mainLabel.style.unityTextAlign = TextAnchor.MiddleCenter;
            mainLabel.style.width = StyleKeyword.Auto;
            mainLabel.style.height = StyleKeyword.Auto;
            mainLabel.style.unityTextOutlineColor = Color.black;
            mainLabel.style.unityTextOutlineWidth = 0.18f;

            container.Add(shadowLabel);
            container.Add(mainLabel);

            Vector2 pos = position ?? Vector2.zero;

            mainLabel.RegisterCallback<GeometryChangedEvent>(evt =>
            {
                Vector2 size = evt.newRect.size;
                container.style.width = size.x;
                container.style.height = size.y;

                float leftPos = pos.x - size.x / 2f;
                float topPos = pos.y - size.y;

                container.style.left = leftPos;
                container.style.top = topPos;

                mainLabel.style.left = 0;
                mainLabel.style.top = 0;
                shadowLabel.style.left = 1;
                shadowLabel.style.top = 2;
                shadowLabel.style.scale = new Scale(new Vector3(1.015f, 1.015f, 1f));

            });

            float startTime = Time.realtimeSinceStartup;
            container.schedule.Execute(() =>
            {
                float elapsed = Time.realtimeSinceStartup - startTime;
                if (elapsed >= duration)
                {
                    activeFloatingTexts.Remove(visualElement);

                    container.RemoveFromHierarchy();
                    return;
                }

                float progress = elapsed / duration;
                float alpha = 1f - progress;
                float yOffset = -progress * 30f;

                mainLabel.style.opacity = alpha;
                shadowLabel.style.opacity = alpha * 1f;
                container.style.top = pos.y - container.layout.height + yOffset;

            }).Every(0);

            activeFloatingTexts[visualElement] = container;
        }

        internal static VisualElement FocusChild(this VisualElement parent, string childName, bool isCopy = false)
        {
            var childElement = parent.Q<VisualElement>(childName);
            if (childElement != null)
            {
                Color color = isCopy ? CustomColors.CustomGreen : CustomColors.CustomBlue;
                ComponentHighlighter.StartHighlight(childElement.name, color);
            }
            return childElement;
        }
        internal static void FocusChild(this VisualElement parent, VisualElement childElement, bool isCopy = false)
        {
            if (childElement != null)
            {
                Color color = isCopy ? CustomColors.CustomGreen : CustomColors.CustomBlue;
                ComponentHighlighter.StartHighlight(childElement.name, color);
            }
        }
        internal static IVisualElementScheduledItem TryUntilDone(this VisualElement element, Func<bool> tryAction, Action onSuccess = null, long intervalMs = 0)
        {
            if (tryAction())
            {
                onSuccess?.Invoke();
                return null;
            }
            IVisualElementScheduledItem scheduleItem = null;
            scheduleItem = element.schedule.Execute(() =>
            {
                if (tryAction())
                {
                    onSuccess?.Invoke();
                    scheduleItem?.Pause();
                }
            });
            if (intervalMs > 0)
            {
                scheduleItem.Every(intervalMs);
            }
            else
            {
                scheduleItem.Every(0);
            }

            return scheduleItem;
        }



        internal static VisualElement GetChild(this VisualElement parent, string childName)
        {
            return parent.Q<VisualElement>(childName);
        }
        internal static VisualElement HideFirstChildOfClass(this VisualElement element, string typeName)
        {
            var child = element.GetFirstChildOfClass(typeName);
            if (child != null)
            {
                EditorUtils.SetElementVisible(child, false);
            }
            return child;
        }

        internal static VisualElement GetFirstChildOfClass(this VisualElement element, string className)
        {
            if (element == null || string.IsNullOrEmpty(className))
            {
                return null;
            }
            if (!className.StartsWith("."))
            {
                className = "." + className;
            }
            string classNameWithoutDot = className.Substring(1);
            return element.Q(className: classNameWithoutDot);
        }
        internal static void CenterElementInView(this ScrollView scrollView, VisualElement childElement)
        {
            scrollView.contentContainer.MarkDirtyRepaint();

            var scrollViewHeight = scrollView.contentViewport.resolvedStyle.height;
            var childElementHeight = childElement.resolvedStyle.height;
            if (scrollViewHeight == 0 || childElementHeight == 0)
            {
                return;
            }

            var elementOffset = childElement.worldBound.y - scrollView.contentContainer.worldBound.y;
            var elementEndOffset = elementOffset + childElementHeight;

            var scrollViewStart = scrollView.scrollOffset.y;
            var scrollViewEnd = scrollViewStart + scrollViewHeight;

            if (elementOffset >= scrollViewStart && elementEndOffset <= scrollViewEnd)
            {
                return;
            }

            float targetScrollPosition = Mathf.Max(0, elementOffset - (scrollViewHeight / 2) + (childElementHeight / 2));

            float clampedScrollPosition = Mathf.Clamp(
                targetScrollPosition,
                scrollView.verticalScroller.lowValue,
                scrollView.verticalScroller.highValue
            );

            scrollView.verticalScroller.value = clampedScrollPosition;

            if (scrollView.verticalScroller.value < 0)
            {
                scrollView.verticalScroller.value = 0;
            }
        }
        internal static bool HasAnyOdinIMGUIInspector(this VisualElement element)
        {
            if (!CoInspectorWindow.IsOdinInspectorPresent() || element == null)
            {
                return false;
            }
            var inspectors = element.GetChildrenComponents();
            if (inspectors == null)
            {
                return false;
            }
            foreach (var inspector in inspectors)
            {
                if (IsOdinIMGUIInspector(inspector))
                {
                    return true;
                }
            }
            return false;
        }

        internal static bool IsOdinIMGUIInspector(this VisualElement element)
        {
            if (!CoInspectorWindow.IsOdinInspectorPresent() || element == null)
            {
                return false;
            }
            return element.Query().Where(e =>
        e.GetType().FullName.Contains("OdinImGuiElement")).First() != null;
        }

        public static void BlockMouseEvents(this VisualElement element)
        {
            element.pickingMode = PickingMode.Ignore;
            element.RegisterCallback<MouseDownEvent>(evt => evt.StopPropagation());
            element.RegisterCallback<MouseUpEvent>(evt => evt.StopPropagation());
            element.RegisterCallback<MouseMoveEvent>(evt => evt.StopPropagation());
            element.RegisterCallback<MouseEnterEvent>(evt => evt.StopPropagation());
            element.RegisterCallback<MouseLeaveEvent>(evt => evt.StopPropagation());
            element.RegisterCallback<WheelEvent>(evt => evt.StopPropagation());
            element.RegisterCallback<MouseOverEvent>(evt => evt.StopPropagation());
            element.RegisterCallback<MouseOutEvent>(evt => evt.StopPropagation());
        }

        private static EditorWindow GetEditorWindow(this VisualElement element)
        {
            if (element == null)
            {
                return null;
            }
            return element.GetFirstAncestorOfType<EditorWindow>();
        }
        public static void ApplySoftScrolling(this ScrollView scrollView, EditorWindow editorOwner)
        {
            const float STOP_THRESHOLD = 2f;

            Vector2 targetOffset = Vector2.zero;
            IVisualElementScheduledItem currentAnimation = null;

            float GetSmoothFactor(float scrollSpeed)
            {
                if (Mathf.Approximately(scrollSpeed, 18f))
                {
                    return 0.2f;
                }
                return 0.15f + Mathf.Clamp01(scrollSpeed / 120f) * 0.15f;
            }

            if (!CoInspectorWindow.softWheelScrolling)
            {
                scrollView.mouseWheelScrollSize = CoInspectorWindow.mouseWheelSensitivity;
                scrollView.UnregisterCallback<WheelEvent>(OnSoftScroll);
                return;
            }

            scrollView.mouseWheelScrollSize = 0;
            scrollView.ReplaceCallback<WheelEvent>(OnSoftScroll);
            targetOffset = scrollView.scrollOffset;

            void OnSoftScroll(WheelEvent evt)
            {
                if (!CoInspectorWindow.softWheelScrolling || !scrollView.CanBeScrolled())
                {
                    return;
                }

                float scrollAmount = CoInspectorWindow.mouseWheelSensitivity;

                evt.StopImmediatePropagation();
                evt.StopPropagation();

                float maxScrollX = Mathf.Max(0, scrollView.contentContainer.resolvedStyle.width - scrollView.resolvedStyle.width);
                float maxScrollY = Mathf.Max(0, scrollView.contentContainer.resolvedStyle.height - scrollView.resolvedStyle.height);

                if (currentAnimation?.isActive == true)
                {
                    targetOffset.y = Mathf.Clamp(targetOffset.y + evt.delta.y * scrollAmount, 0, maxScrollY);
                    if (maxScrollX > 0)
                        targetOffset.x = Mathf.Clamp(targetOffset.x + evt.delta.x * scrollAmount, 0, maxScrollX);
                }
                else
                {
                    targetOffset.y = Mathf.Clamp(scrollView.scrollOffset.y + evt.delta.y * scrollAmount, 0, maxScrollY);
                    if (maxScrollX > 0)
                        targetOffset.x = Mathf.Clamp(scrollView.scrollOffset.x + evt.delta.x * scrollAmount, 0, maxScrollX);

                    AnimateScroll();
                }
            }

            void AnimateScroll()
            {
                if (!CoInspectorWindow.softWheelScrolling)
                {
                    return;
                }
                float scrollAmount = CoInspectorWindow.mouseWheelSensitivity;
                float smoothFactor = GetSmoothFactor(scrollAmount);

                Vector2 startPosition = scrollView.scrollOffset;
                Vector2 totalDistance = targetOffset - startPosition;
                float startTime = Time.realtimeSinceStartup;

                currentAnimation = scrollView.schedule.Execute(() =>
                {
                    float elapsedTime = Time.realtimeSinceStartup - startTime;

                    float dynamicSmoothFactor = smoothFactor * (1 + elapsedTime * 2f);
                    dynamicSmoothFactor = Mathf.Clamp(dynamicSmoothFactor, smoothFactor, 0.5f);
                    Vector2 newPosition = Vector2.Lerp(scrollView.scrollOffset, targetOffset, dynamicSmoothFactor);
                    scrollView.scrollOffset = newPosition;

                }).Until(() =>
                {
                    float distanceX = Mathf.Abs(scrollView.scrollOffset.x - targetOffset.x);
                    float distanceY = Mathf.Abs(scrollView.scrollOffset.y - targetOffset.y);

                    if (distanceX < STOP_THRESHOLD && distanceY < STOP_THRESHOLD)
                    {
                        scrollView.scrollOffset = targetOffset;
                        scrollView.MarkDirtyRepaint();
                        SmoothScrollManager.activeScrolls.RemoveIfPresent(currentAnimation);
                        return true;
                    }
                    return false;
                });
                SmoothScrollManager.activeScrolls.AddIfNotPresent(currentAnimation);
            }
        }
        internal static VisualElement CreatePreviewToolbar(Editor[] previewEditors, TabInfo tab)
        {

            float previewHeight = 130;
            float barHeight = 13;
            if (previewEditors.Length == 0)
            {
                return null;
            }
            var previewContainer = new IMGUIContainer();
            var totalHeight = CoInspectorWindow.tabPreviewExpanded ? previewHeight + barHeight : barHeight;
            var root = new VisualElement();
            root.Add(HorizontalLine(EditorUtils.IsLightSkin() ? Color.gray : Color.black));
            root.style.height = totalHeight;
            root.style.minHeight = totalHeight;
            root.style.overflow = Overflow.Hidden;
            root.style.flexShrink = 0;
            root.style.flexGrow = 0;

            // Toolbar
            var toolbar = new VisualElement();
            toolbar.style.height = barHeight;
            toolbar.style.minHeight = barHeight;
            toolbar.style.maxHeight = barHeight;
            toolbar.style.flexDirection = FlexDirection.Row;
            toolbar.style.paddingLeft = 0;
            toolbar.style.paddingRight = 0;
            toolbar.style.backgroundColor = EditorUtils.IsLightSkin() ? new Color(0.75f, 0.75f, 0.75f) : new Color(0.42f, 0.42f, 0.42f);
            toolbar.style.flexShrink = 0;
            toolbar.style.marginRight = -2;
            root.Add(toolbar);
            root.Add(HorizontalLine());
            root.Add(HorizontalLine(CustomColors.SimpleShadow));
            var dropdown = new ToolbarMenu();
            dropdown.style.height = barHeight;
            dropdown.style.marginTop = 1;
            dropdown.style.marginBottom = 1;
            dropdown.style.fontSize = 10;
            dropdown.style.unityFontStyleAndWeight = FontStyle.Bold;
            Editor currentEditor = previewEditors[0];
            string currentName = GetEditorDisplayName(currentEditor);
            dropdown.text = currentName.Length > 20 ? currentName.Substring(0, 20) + "…" : currentName;
            toolbar.Add(dropdown);
            previewContainer.style.display = CoInspectorWindow.tabPreviewExpanded ? DisplayStyle.Flex : DisplayStyle.None;
            tab.previewHeight = CoInspectorWindow.tabPreviewExpanded ? previewHeight + barHeight : barHeight;
            UnityEngine.UIElements.Image buttonImage = new UnityEngine.UIElements.Image();
            buttonImage.image = CoInspectorWindow.tabPreviewExpanded ? CustomGUIContents.PreviewCollapse : CustomGUIContents.PreviewExpand;
            buttonImage.style.top = CoInspectorWindow.tabPreviewExpanded ? 4 : 3;
            ToolbarButton expandButton = null;
            expandButton = new ToolbarButton(() =>
            {

                CoInspectorWindow.tabPreviewExpanded = !CoInspectorWindow.tabPreviewExpanded;
                previewContainer.style.display = CoInspectorWindow.tabPreviewExpanded ? DisplayStyle.Flex : DisplayStyle.None;
                tab.previewHeight = CoInspectorWindow.tabPreviewExpanded ? previewHeight + barHeight : barHeight;
                root.style.height = tab.previewHeight;
                root.style.minHeight = tab.previewHeight;
                expandButton.tooltip = CoInspectorWindow.tabPreviewExpanded ? "Hide Component Preview" : "Show Component Preview";
                buttonImage.style.top = CoInspectorWindow.tabPreviewExpanded ? 4 : 3;
                buttonImage.image = CoInspectorWindow.tabPreviewExpanded ? CustomGUIContents.PreviewCollapse : CustomGUIContents.PreviewExpand;
                root.MarkDirtyRepaint();
            });
            VisualElement imageContainer = new VisualElement();
            imageContainer.style.position = Position.Absolute;
            imageContainer.style.left = 0;
            imageContainer.style.right = 0;
            imageContainer.style.alignItems = Align.Center;
            imageContainer.style.justifyContent = Justify.Center;
            imageContainer.pickingMode = PickingMode.Ignore;
            buttonImage.pickingMode = PickingMode.Ignore;
            imageContainer.Add(buttonImage);
            imageContainer.name = "Expand Button";
            root.Add(imageContainer);
            expandButton.style.backgroundColor = EditorUtils.IsLightSkin() ? new Color(0.6f, 0.6f, 0.6f) : new Color(0.29f, 0.29f, 0.29f);
            expandButton.RegisterCallback<MouseEnterEvent>((evt) =>
            {
                expandButton.style.backgroundColor = EditorUtils.IsLightSkin() ? new Color(0.65f, 0.65f, 0.65f) : new Color(0.32f, 0.32f, 0.32f);
            });

            expandButton.RegisterCallback<MouseLeaveEvent>((evt) =>
                {
                    expandButton.style.backgroundColor = EditorUtils.IsLightSkin() ? new Color(0.6f, 0.6f, 0.6f) : new Color(0.29f, 0.29f, 0.29f);
                });
            expandButton.tooltip = CoInspectorWindow.tabPreviewExpanded ? "Hide Component Preview" : "Show Component Preview";
            expandButton.style.height = barHeight;
            expandButton.style.marginTop = 1;
            expandButton.style.marginBottom = 1;
            expandButton.focusable = false;
            expandButton.style.flexGrow = 1;
            expandButton.text = "";
            toolbar.Add(expandButton);

            foreach (var editor in previewEditors)
            {
                string editorName = GetEditorDisplayName(editor);
                string shortName = editorName.Length > 20 ? editorName.Substring(0, 20) + "…" : editorName;
                dropdown.menu.AppendAction(editorName, (DropdownMenuAction action) =>
                {
                    currentEditor = editor;
                    dropdown.text = shortName;
                    dropdown.style.unityFontStyleAndWeight = FontStyle.Bold;
                });
            }
            previewContainer.style.height = previewHeight;
            previewContainer.style.minHeight = previewHeight;
            previewContainer.style.maxHeight = previewHeight;
            previewContainer.style.flexShrink = 0;
            previewContainer.style.backgroundColor = new Color(0.15f, 0.15f, 0.17f);
            previewContainer.style.flexGrow = 0;
            root.Add(previewContainer);
            previewContainer.AddPreviewBackground();
            previewContainer.onGUIHandler = () =>
            {
                if (currentEditor != null)
                {
                    var rect = new Rect(0, 0, previewContainer.layout.width, previewHeight);
                    GUI.BeginGroup(rect);
                    {
                        if (currentEditor.target is RectTransform rectTransform)
                        {
                            DrawLayoutProperties(rectTransform);
                        }
                        else
                        {
                            currentEditor.DrawPreview(new Rect(0, 0, rect.width, rect.height));
                        }

                    }
                    GUI.EndGroup();
                }
            };
            return root;
        }
        private static string GetEditorDisplayName(Editor editor)
        {
            if (editor == null || editor.target == null)
            {
                return "Preview";
            }
            if (editor.target is RectTransform)
            {
                return $"Layout Properties";
            }
            string label = editor.GetPreviewTitle().text;
            return label;
        }

        public static void DrawLayoutProperties(RectTransform rect)
        {
            GUIStyle labelStyle = new GUIStyle(EditorStyles.label);
            GUIStyle headerStyle = new GUIStyle(EditorStyles.boldLabel);
            labelStyle.padding.right += 4;
            labelStyle.padding.left += 4;
            headerStyle.padding.right += 4;
            headerStyle.padding.left += 4;
            headerStyle.padding.top += 5;
            headerStyle.padding.bottom += 3;

            EditorGUILayout.BeginHorizontal();
            GUILayout.Label("Property", headerStyle, GUILayout.Width(110));
            GUILayout.Label("Value", headerStyle, GUILayout.Width(100));
            GUILayout.Label("Source", headerStyle);
            EditorGUILayout.EndHorizontal();
            ILayoutElement source = null;

            void ShowProperty(string label, float value, ILayoutElement propertySource)
            {
                EditorGUILayout.BeginHorizontal();
                GUILayout.Label(label, labelStyle, GUILayout.Width(110));
                GUILayout.Label(value.ToString(System.Globalization.CultureInfo.InvariantCulture), labelStyle, GUILayout.Width(100));
                GUILayout.Label(propertySource == null ? "none" : propertySource.GetType().Name, labelStyle);
                EditorGUILayout.EndHorizontal();
            }

            void ShowFlexibleProperty(string label, float value, ILayoutElement propertySource)
            {
                EditorGUILayout.BeginHorizontal();
                GUILayout.Label(label, labelStyle, GUILayout.Width(110));
                GUILayout.Label(value > 0 ? $"enabled ({value.ToString(System.Globalization.CultureInfo.InvariantCulture)})" : "disabled", labelStyle, GUILayout.Width(100));
                GUILayout.Label(propertySource == null ? "none" : propertySource.GetType().Name, labelStyle);
                EditorGUILayout.EndHorizontal();
            }

            ShowProperty("Min Width", LayoutUtility.GetLayoutProperty(rect, e => e.minWidth, 0, out source), source);
            ShowProperty("Min Height", LayoutUtility.GetLayoutProperty(rect, e => e.minHeight, 0, out source), source);
            ShowProperty("Preferred Width", LayoutUtility.GetLayoutProperty(rect, e => e.preferredWidth, 0, out source), source);
            ShowProperty("Preferred Height", LayoutUtility.GetLayoutProperty(rect, e => e.preferredHeight, 0, out source), source);

            float flexibleWidth = LayoutUtility.GetLayoutProperty(rect, e => e.flexibleWidth, 0, out source);
            ShowFlexibleProperty("Flexible Width", flexibleWidth, source);

            float flexibleHeight = LayoutUtility.GetLayoutProperty(rect, e => e.flexibleHeight, 0, out source);
            ShowFlexibleProperty("Flexible Height", flexibleHeight, source);

            if (!rect.GetComponent<LayoutElement>())
            {
                EditorGUILayout.Space(1);
                EditorGUILayout.LabelField("Add a LayoutElement to override values.", labelStyle);
            }
        }
        internal static VisualElement HorizontalLine(Color color = default, float thickness = 1)
        {
            if (color == default)
                color = new Color(0.1f, 0.1f, 0.1f, 1f);

            var line = new VisualElement();
            line.style.height = thickness;
            line.style.backgroundColor = color;
            line.style.marginTop = 0;
            line.style.marginBottom = 0;
            line.style.flexGrow = 0;
            line.style.flexShrink = 0;
            line.style.alignSelf = Align.Stretch;
            line.pickingMode = PickingMode.Ignore;
            return line;
        }
    }
    internal class SmoothScrollView : ScrollView
    {
        private const float STOP_THRESHOLD = 2f;

        private Vector2 targetOffset = Vector2.zero;
        private IVisualElementScheduledItem currentAnimation = null;

        internal SmoothScrollView() : base()
        {
            Initialize();
        }

        internal SmoothScrollView(ScrollViewMode mode = ScrollViewMode.Vertical) : base(mode)
        {
            Initialize();
        }

        private void Initialize()
        {
            StopAnimation();
            targetOffset = scrollOffset;

            if (CoInspectorWindow.softWheelScrolling)
            {
                this.mouseWheelScrollSize = 0;
            }
            else
            {
                this.mouseWheelScrollSize = CoInspectorWindow.mouseWheelSensitivity;
            }
            this.ReplaceCallback<WheelEvent>(OnSoftScroll);
            this.ReplaceCallback<AttachToPanelEvent>(OnAttachToPanel);
            this.ReplaceCallback<DetachFromPanelEvent>(OnDetachFromPanel);
        }

        internal void Reset()
        {
            StopAnimation();
            targetOffset = scrollOffset;
        }
        private void StopAnimation()
        {
            if (currentAnimation?.isActive == true)
            {
                SmoothScrollManager.activeScrolls.RemoveIfPresent(currentAnimation);
                currentAnimation.Pause();
                currentAnimation = null;
            }
        }

        private void OnAttachToPanel(AttachToPanelEvent evt)
        {
            if (CoInspectorWindow.softWheelScrolling)
            {
                this.mouseWheelScrollSize = 0;
            }
            else
            {
                this.mouseWheelScrollSize = CoInspectorWindow.mouseWheelSensitivity;
            }
        }

        private void OnDetachFromPanel(DetachFromPanelEvent evt)
        {
            StopAnimation();
        }

        private float GetSmoothFactor(float scrollSpeed)
        {
            if (scrollSpeed == 18)
            {
                return 0.17f;
            }
            if (scrollSpeed > 40)
            {
                return 0.075f;
            }
            return 0.13f;
        }


        private void OnSoftScroll(WheelEvent evt)
        {
            bool softScrollingEnabled = CoInspectorWindow.softWheelScrolling;
            float maxScrollX = Mathf.Max(0, contentContainer.resolvedStyle.width - resolvedStyle.width);
            float maxScrollY = Mathf.Max(0, contentContainer.resolvedStyle.height - resolvedStyle.height);
            Vector2 originalScrollOffset = scrollOffset;

            if (softScrollingEnabled)
            {
                if (this.mouseWheelScrollSize != 0)
                {
                    this.mouseWheelScrollSize = 0;

                }
            }
            else
            {
                float speed = CoInspectorWindow.mouseWheelSensitivity;
                if (this.mouseWheelScrollSize != speed)
                {
                    this.mouseWheelScrollSize = speed;
                    targetOffset = scrollOffset;
                    scrollOffset = new Vector2(scrollOffset.x, Mathf.Clamp(targetOffset.y + evt.delta.y * CoInspectorWindow.mouseWheelSensitivity, 0, maxScrollY));
                    if (currentAnimation != null)
                    {
                        SmoothScrollManager.activeScrolls.RemoveIfPresent(currentAnimation);

                    }
                }
                return;

            }
            evt.StopImmediatePropagation();
            evt.StopPropagation();
            if (!this.CanBeScrolled())
            {
                return;
            }
            if (currentAnimation?.isActive == true)
            {
                targetOffset.y = Mathf.Clamp(targetOffset.y + evt.delta.y * CoInspectorWindow.mouseWheelSensitivity, 0, maxScrollY);
                if (maxScrollX > 0)
                {
                    targetOffset.x = Mathf.Clamp(targetOffset.x + evt.delta.x * CoInspectorWindow.mouseWheelSensitivity, 0, maxScrollX);
                }
            }
            else
            {
                targetOffset.y = Mathf.Clamp(scrollOffset.y + evt.delta.y * CoInspectorWindow.mouseWheelSensitivity, 0, maxScrollY);
                if (maxScrollX > 0)
                {
                    targetOffset.x = Mathf.Clamp(scrollOffset.x + evt.delta.x * CoInspectorWindow.mouseWheelSensitivity, 0, maxScrollX);
                }

                AnimateScroll();
            }
        }

        private void AnimateScroll()
        {
            if (!CoInspectorWindow.softWheelScrolling || !this.CanBeScrolled())
            {
                return;
            }

            float smoothFactor = GetSmoothFactor(CoInspectorWindow.mouseWheelSensitivity);
            if (CoInspectorWindow.mouseWheelSpeed > 1)
            {
                smoothFactor *= CoInspectorWindow.mouseWheelSpeed * 0.6f;
            }
            float startTime = Time.realtimeSinceStartup;

            currentAnimation = schedule.Execute(() =>
            {
                float elapsedTime = Time.realtimeSinceStartup - startTime;
                float dynamicSmoothFactor = smoothFactor * (1 + elapsedTime * (2 + CoInspectorWindow.mouseWheelSpeed));
                dynamicSmoothFactor = Mathf.Clamp(dynamicSmoothFactor, smoothFactor, 0.5f);
                Vector2 newPosition = Vector2.Lerp(scrollOffset, targetOffset, dynamicSmoothFactor);
                scrollOffset = newPosition;

            }).Until(() =>
            {
                float distanceX = Mathf.Abs(scrollOffset.x - targetOffset.x);
                float distanceY = Mathf.Abs(scrollOffset.y - targetOffset.y);

                if (distanceX < STOP_THRESHOLD && distanceY < STOP_THRESHOLD)
                {
                    scrollOffset = targetOffset;
                    this.MarkDirtyRepaint();
                    SmoothScrollManager.activeScrolls.RemoveIfPresent(currentAnimation);
                    return true;
                }
                return false;
            });

            SmoothScrollManager.activeScrolls.AddIfNotPresent(currentAnimation);
        }
    }
    [InitializeOnLoad]
    internal static class SmoothScrollManager
    {
        internal static List<IVisualElementScheduledItem> activeScrolls = new List<IVisualElementScheduledItem>();
        static SmoothScrollManager()
        {
            EditorApplication.update += CheckActiveScrolls;
        }

        static void CheckActiveScrolls()
        {
            if (activeScrolls == null)
            {
                activeScrolls = new List<IVisualElementScheduledItem>();
                return;
            }

            if (activeScrolls.Count == 0)
            {
                return;
            }

            foreach (var scroll in activeScrolls)
            {
                if (scroll != null && scroll.isActive && scroll.element != null)
                {
                    scroll.element.MarkDirtyRepaint();
                    return;
                }
            }
            activeScrolls.Clear();
        }
    }
}