using HTraceSSGI.Scripts.Globals;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace HTraceSSGI.Scripts.Editor.WindowsAndMenu
{
#if UNITY_EDITOR
    public class HBugReporterWindow : EditorWindow
    {
        private string _reportData = "";

        private GUIStyle _styleLabel;
        private Vector2 _scrollPosition = Vector2.zero;

        [MenuItem("Window/HTrace/Report a Bug HTrace SSGI", false, priority: 32)]
        public static void ShowWindow()
        {
            var window = GetWindow<HBugReporterWindow>(false, "Report Bug", true);
            window.minSize = new Vector2(400, 330);
        }

        void OnEnable()
        {
            _reportData = "";

            var pipeline = HRenderer.CurrentHRenderPipeline.ToString();

            _reportData += $"{HNames.ASSET_NAME_FULL} Version: {HNames.HTRACE_SSGI_VERSION}" + "\n";

            _reportData += "\n";

            _reportData += "Unity Version: " + Application.unityVersion + "\n";
            _reportData += "Pipeline: " + pipeline + "\n";
            _reportData += "Platform: " + Application.platform + "\n";
            _reportData += "Graphics API: " + SystemInfo.graphicsDeviceType + "\n";

            _reportData += "\n";

            _reportData += "OS: " + SystemInfo.operatingSystem + "\n";
            _reportData += "Graphics: " + SystemInfo.graphicsDeviceName + "\n";

            _reportData += "\n";
            _reportData += "Additional details:\n";
        }

        void OnGUI()
        {
            SetGUIStyles();

            GUILayout.Space(-2);

            GUILayout.BeginHorizontal();
            GUILayout.Space(15);

            GUILayout.BeginVertical();

            _scrollPosition = GUILayout.BeginScrollView(_scrollPosition, false, false, GUILayout.Width(this.position.width - 28), GUILayout.Height(this.position.height - 80));

            GUILayout.Label(_reportData, _styleLabel);

            GUILayout.Space(15);

            if (GUILayout.Button("Copy Details To Clipboard", GUILayout.Height(24)))
            {
                var copyData = _reportData;

                GUIUtility.systemCopyBuffer = copyData;
            }
            if (GUILayout.Button("Report Bug on Discord", GUILayout.Height(24)))
            {
                Application.OpenURL(HNames.HTRACE_DISCORD_BUGS_SSGI_LINK);
            }

            GUILayout.FlexibleSpace();

            GUILayout.Space(20);

            GUILayout.EndScrollView();

            GUILayout.EndVertical();

            GUILayout.Space(13);
            GUILayout.EndHorizontal();
        }

        void SetGUIStyles()
        {
            _styleLabel = new GUIStyle(EditorStyles.label)
            {
                richText = true,
            };
        }
    }

#endif
}
