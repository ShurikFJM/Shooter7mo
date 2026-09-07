using System;
using System.Linq;
using System.Collections.Generic;
using UnityEditor;

namespace CoInspector
{
    internal static class TipsManager
    {
        static List<string> tips = new List<string>
    {
        "Select a <b>GameObject</b> to inspect it!",
        "You can hold <b>Middle-click to scroll</b> faster!",
        "You can <b>Pan</b> your Components with <b>Middle-click</b> if you set it as your <b>Middle-click scroll Mode</b> in the <b>Settings Window</b>",
        "<b>Middle click</b> any Tab's <b>Debug button</b> to enter <b>Full Debug Mode</b>",
        "Enable <b>Switch to existing Tabs</b> to reuse your existing Tabs!",
        "You can customize your <b>Header Buttons</b> in the <b>Settings Window</b>",
        "You can customize you <b>mouse wheel speed and amount</b> in the <b>Settings Window</b>",
        "Enable <b>Override Scene Tools</b> in the <b>Settings Window</b> to use <b>Move, Rotate and Scale</b> with your active Tab, even if it's not selected",
        "You can collapse your <b>Asset previews</b>",
        "You can use you <b>Mouse side buttons</b> to navigate your Tab's History",
"Hover the new <b>Inspector Buttons</b> at the <b>GameObject Header</b> to learn what they can do for you!",
"Middle-click the <b>Focus Button</b> to bring the active Tab into view!",
"Right-click the <b>Add Tab Button</b> to create a Tab at the end of the Bar.",
"Use the <b>Local Hierarchy Button</b> to check your target's position in the Scene Hierarchy!",
"You can middle-click to scroll faster!",
"Right-click the <b>History Buttons</b> to see all History steps.",
"Middle-click the <b>History Buttons</b> to open them in a new Tab.",
"You can <b>drag and drop your Tabs</b> and use them <b>as GameObjects</b> on your Inspector fields!",
"Hold <b>Ctrl/Cmd</b> when reordering Components to <b>Clone</b> them!",
"You can <b>Move Components</b> between <b>Tabbed GameObjects</b>!",
"You can <b>Clone Components</b> between <b>Tabbed GameObjects</b>!\n\nJust hold <b>Ctrl/Cmd</b> while dragging.",
"<b>Hover a Tab</b> to see information about its target(s).\n\n<i>(Change how much to show in the <b>Settings Window</b>.)</i>",
"You can <b>auto-select in the Hierarchy</b> your targets when switching Tabs.\n\n<i>(Enable or disable this in the <b>Settings Window</b>.)</i>",
"You can auto-focus on Scene View your targets when switching Tabs.\n\n<i>(Enable or disable this in the <b>Settings Window</b>.)</i>",
"You can set <b>automated Behaviors</b> when double-clicking a Tab <i>(as Locking it, selecting it or Pinging it)</i>.\n\n<i>(Change this in the <b>Settings Window</b>.)</i>",
"You can automatically <b>create Components</b> by dragging Assets onto GameObjects!\n\n<i>(Like AudioClips, Animators, or Sprites.)</i>",
"You can hide <b>Tab Icons</b>, the <b>scrollbar</b> or the <b>History Buttons</b> in the <b>Settings Window</b>.",
"The new <b>multi-GameObject Header</b> allows you to check and remove elements of your multiple selections.",
"Recover your last active workspace by choosing <b>Recover Last Saved Session</b>!",
"Use the <b>Inspector Button</b> to send any Tab or Asset to the regular Inspector.\n\n<i>(Hold Alt to pop up a new exclusive window.)</i>",
"CoInspector saves your workspace and Tabs in each Scene.\n\n<i>(Choose how to manage them in the <b>Sessions</b> section of the <b>Settings Window</b>.)</i>",
"You can check and manage all your <b>saved Sessions</b> in the <b>Settings Window</b>.",
"Instantly expand or collapse the <b>Asset View</b> by clicking the blue <b>Asset Bar</b>!",
"You can drag and drop your <b>active Assets</b> directly from the <b>Asset Bar</b>!",
"Right-click the <b>Asset Bar</b> to access the <b>Asset Context Menu</b>!",
"Right-click any Tab to access the <b>Tab Context Menu</b>!",
"You can temporarily disable <b>Asset Inspection</b>!\n\n<i>(Check the <b>Asset Context Menu</b> or the <b>Settings Window</b>.)</i>",
"Drag the <b>Add Component Bar</b> to resize your Asset View!",
"Close the <b>Asset View</b> by middle-clicking the <b>Asset Bar</b>!",
"Instantly Ping your <b>active Asset</b> by clicking the <b>Path Bar</b>!",
"Instantly <b>copy to the clipboard</b> the path of your <b>active Asset</b> by right-clicking its path!",
"Show or hide the <b>AssetBundle footer</b> in the <b>Settings Window</b>!",
"Show or hide the <b>Import Settings</b> of your Assets by using the <b>Import Settings Button</b> in the <b>Asset Bar</b>!",
"Check your <b>previously inspected Assets</b> by using the <b>Asset History Button</b> in the <b>Add Component Bar</b>!",
"Switch between your <b>last 2 inspected Assets</b> by right-clicking the <b>Asset History Button</b> in the <b>Add Component Bar</b>!",
"You can restore your <b>last saved Session</b> at any time by using the <b>Recover Last Saved Session</b> option in the <b>Tabs Context Menu</b>!"
    };



        internal static int[] consumedTips;
        static UserSaveData userSaveData;

        static bool CheckSaveData()
        {
            if (userSaveData == null)
            {
                string path = CoInspectorWindow._GetRootPath() + "/Settings/UserData.asset";
                if (System.IO.File.Exists(path))
                {
                    userSaveData = AssetDatabase.LoadAssetAtPath<UserSaveData>(path);
                }
            }
            return userSaveData != null;
        }

        public static string GetTipsProgress()
        {
            LoadTips();
            List<int> consumedTipsList = new List<int>(consumedTips);
            int seenCount = consumedTipsList.Count(x => x > 0);
            int totalCount = tips.Count;

            return $"({seenCount}/{totalCount})";
        }

        static void LoadTips()
        {
            if (!CheckSaveData())
            {
                return;
            }

            int[] savedTips = userSaveData.tipsShown;
            if (consumedTips == null || savedTips == null || savedTips.Length != tips.Count)
            {
                consumedTips = new int[tips.Count];
                if (savedTips != null && savedTips.Length == tips.Count)
                {
                    Array.Copy(savedTips, consumedTips, savedTips.Length);
                }
            }
            else
            {
                consumedTips = savedTips;
            }
        }
        public static void ResetTips()
        {
            if (!CheckSaveData())
            {
                return;
            }
            consumedTips = new int[tips.Count];
            userSaveData.tipsShown = consumedTips;
            EditorUtility.SetDirty(userSaveData);
        }

        public static void SaveTips()
        {
            if (!CheckSaveData())
            {
                return;
            }
            userSaveData.tipsShown = consumedTips;
            EditorUtility.SetDirty(userSaveData);
        }

        public static string GetRandomTip()
        {
            LoadTips();
            if (consumedTips == null)
            {
                consumedTips = new int[tips.Count];
            }
            if (consumedTips.Length == tips.Count && Array.TrueForAll(consumedTips, x => x > 0))
            {
                consumedTips = new int[tips.Count];
            }

            if (Array.TrueForAll(consumedTips, x => x == 0))
            {
                consumedTips[0]++;
                SaveTips();
                return tips[0];
            }

            int index = UnityEngine.Random.Range(0, tips.Count);
            while (consumedTips[index] > 0)
            {
                index = UnityEngine.Random.Range(0, tips.Count);
            }

            consumedTips[index]++;
            SaveTips();
            return tips[index];
        }


    }
}