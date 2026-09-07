
using UnityEditor.ShortcutManagement;
using UnityEngine;

namespace CoInspector
{
    public class ShortcutManager : MonoBehaviour
    {
        [Shortcut("CoInspector/Tabs/Toggle Target GameObject(s)", KeyCode.D, ShortcutModifiers.Alt)]
        public static void ToggleGameObject(ShortcutArguments args)
        {

            if (CoInspectorWindow.MainCoInspector)
            {
                if (CoInspectorWindow.MainCoInspector.IsActiveTabValid())
                {
                    bool setTargetsTo = CoInspectorWindow.MainCoInspector.ActiveTabHasDisabledTargets();

                    if (CoInspectorWindow.MainCoInspector.IsActiveTabValidMulti())
                    {
                        foreach (var target in CoInspectorWindow.MainCoInspector.GetActiveTab().targets)
                        {
                            target.SetActive(setTargetsTo);
                        }
                    }
                    else
                    {
                        CoInspectorWindow.MainCoInspector.GetActiveTab().target.SetActive(setTargetsTo);
                    }
                }

                (args.context as Event)?.Use();
            }

        }
        [Shortcut("CoInspector/Asset View/Expand or Collapse Asset View", KeyCode.V, ShortcutModifiers.Alt)]
        public static void ToggleAssetView(ShortcutArguments args)
        {

            if (CoInspectorWindow.MainCoInspector)
            {
                if (CoInspectorWindow.MainCoInspector.IsValidAssetTarget())
                {
                    CoInspectorWindow.MainCoInspector.ForceCollapseOrDefault();
                }
                else
                {
                    CoInspectorWindow.MainCoInspector.BackToPreviousAsset();
                }
            }
        }
        [Shortcut("CoInspector/Tabs/Expand All Components", KeyCode.E, ShortcutModifiers.Alt)]
        public static void ExpandComponents(ShortcutArguments args)
        {
            if (CoInspectorWindow.MainCoInspector)
            {
                if (CoInspectorWindow.MainCoInspector.IsActiveTabValid())
                {
                    CoInspectorWindow.MainCoInspector.SetAllComponentsTo(true);
                }

                (args.context as Event)?.Use();
            }

        }
        [Shortcut("CoInspector/Tabs/Collapse All Components", KeyCode.W, ShortcutModifiers.Alt)]
        public static void CollapseComponents(ShortcutArguments args)
        {
            if (CoInspectorWindow.MainCoInspector)
            {
                if (CoInspectorWindow.MainCoInspector.IsActiveTabValid())
                {
                    CoInspectorWindow.MainCoInspector.SetAllComponentsTo(false);
                }

                (args.context as Event)?.Use();
            }
        }
        [Shortcut("CoInspector/Tabs/Toggle Expand-Collpase Components", KeyCode.R, ShortcutModifiers.Alt)]
        public static void ToggleCollapseComponents(ShortcutArguments args)
        {
            if (CoInspectorWindow.MainCoInspector)
            {
                if (CoInspectorWindow.MainCoInspector.IsActiveTabValid())
                {
                    bool allCollapsed = CoInspectorWindow.MainCoInspector.GetActiveTab().AreAllCollapsed();
                    CoInspectorWindow.MainCoInspector.SetAllComponentsTo(allCollapsed);
                }

                (args.context as Event)?.Use();
            }
        }
        [Shortcut("CoInspector/Tabs/Go Back Tab History", KeyCode.LeftArrow, ShortcutModifiers.Alt)]
        public static void GoBack(ShortcutArguments args)
        {
            if (CoInspectorWindow.MainCoInspector)
            {
                if (CoInspectorWindow.MainCoInspector.IsActiveTabValid())
                {
                    if (CoInspectorWindow.MainCoInspector.GetActiveTab().CanMoveBack())
                    {
                        CoInspectorWindow.MainCoInspector.GetActiveTab().MoveBack();
                    }
                }
            }
        }
#if UNITY_2022_1_OR_NEWER
        [Shortcut("CoInspector/Tabs/Go Back Tab History (Mouse)", KeyCode.Mouse3)]
        public static void GoBackMouse(ShortcutArguments args)
        {
            GoBack(args);
        }
        [Shortcut("CoInspector/Tabs/Go Forward Tab History (Mouse)", KeyCode.Mouse4)]
        public static void GoForwardMouse(ShortcutArguments args)
        {
            GoForward(args);
        }
#endif
        [Shortcut("CoInspector/Tabs/Go Forward Tab History", KeyCode.RightArrow, ShortcutModifiers.Alt)]
        public static void GoForward(ShortcutArguments args)
        {
            if (CoInspectorWindow.MainCoInspector)
            {
                if (CoInspectorWindow.MainCoInspector.IsActiveTabValid())
                {
                    if (CoInspectorWindow.MainCoInspector.GetActiveTab().CanMoveForward())
                    {
                        CoInspectorWindow.MainCoInspector.GetActiveTab().MoveForward();
                    }
                }
            }
        }
        [Shortcut("CoInspector/Tabs/Edit Component Filter", KeyCode.F, ShortcutModifiers.Alt)]
        public static void FocusFilter(ShortcutArguments args)
        {
            if (CoInspectorWindow.MainCoInspector)
            {
                CoInspectorWindow.MainCoInspector.FocusFilterField();
            }
        }
        [Shortcut("CoInspector/Tabs/Toggle Component Filter", KeyCode.G, ShortcutModifiers.Alt)]
        public static void DisableFilter(ShortcutArguments args)
        {
            if (CoInspectorWindow.MainCoInspector)
            {
                CoInspectorWindow.MainCoInspector.ToggleFilterField();
            }
        }
    }
}
