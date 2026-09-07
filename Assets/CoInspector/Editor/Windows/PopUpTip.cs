using UnityEngine;

namespace CoInspector
{
    internal static class PopUpTip
    {
        internal static Rect rect;
        internal static CoInspectorWindow inspector;
        internal static string text;
        internal static bool show = false;
        internal static bool followMouse = false;
        internal static float enterTime = -1;
        //  internal static float exitTime = -1;
        internal static bool lastStatus = false;
        internal static bool waitingToOpen = false;
        internal static bool opened = false;
        internal static bool isMulti = false;

        internal static void Show(string _text, Rect _rect, CoInspectorWindow owner, bool _followMouse = false)
        {
            if (!CoInspectorWindow.showTabName)
            {
                show = false;
                return;
            }
            if (_text == "")
            {
                PopUpTip.text = _text;
                PopUpTip.show = false;
                return;
            }
            PopUpTip.text = _text;
            PopUpTip.rect = _rect;
            PopUpTip.show = true;
            PopUpTip.followMouse = _followMouse;
            isMulti = false;
            inspector = owner;

        }
        internal static void ShowMulti(string _text, Rect _rect, CoInspectorWindow owner, bool _followMouse = false)
        {
            if (!CoInspectorWindow.showTabName)
            {
                show = false;
                return;
            }

            if (_text == "")
            {
                PopUpTip.text = _text;
                PopUpTip.show = false;
                return;
            }
            PopUpTip.text = _text;
            PopUpTip.rect = _rect;
            PopUpTip.show = true;
            PopUpTip.followMouse = _followMouse;
            isMulti = true;
            inspector = owner;
        }

        internal static void Hide()
        {
            enterTime = -1;
            PopUpTip.show = false;
            waitingToOpen = false;
            isMulti = false;
            inspector = null;
            opened = false;
        }
        internal static bool ShowGUI()
        {
            if (inspector == null)
            {
                show = false;
                return false;
            }

            if (!CoInspectorWindow.showTabName)
            {
                show = false;
                return false;
            }

            if (show && text != "")
            {
                if (!opened && !waitingToOpen)
                {
                    waitingToOpen = true;
                    enterTime = Time.realtimeSinceStartup;

                }
                if (!opened && waitingToOpen && Time.realtimeSinceStartup - enterTime < 0.5f)
                {
                    return false;
                }
                waitingToOpen = false;
                opened = true;
                GUIStyle style = CustomGUIStyles.TipStyle;
                if (isMulti)
                {
                    style = CustomGUIStyles.TipMultiStyle;
                }
                if (followMouse)
                {
                    rect.x = Event.current.mousePosition.x;
                    rect.y = Event.current.mousePosition.y;
                }
                rect.width = style.CalcSize(new GUIContent(text)).x;
                rect.height = style.CalcSize(new GUIContent(text)).y;
                if (rect.xMax > inspector.position.width)
                {
                    rect.x = inspector.position.width - rect.width - 10;
                }
                Color color = GUI.color;
                GUI.color = new Color(1, 1, 1, 0.9f);
                GUI.Box(rect, text, style);
                GUI.color = color;
                return true;
            }
            else if (show && text == "")
            {
                show = false;
            }
            if (!show)
            {
                opened = false;
                waitingToOpen = false;
            }

            return false;
        }
    }
}