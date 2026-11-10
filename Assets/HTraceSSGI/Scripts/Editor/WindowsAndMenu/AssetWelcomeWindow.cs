#if UNITY_EDITOR
using System;
using HTraceSSGI.Scripts.Extensions;
using HTraceSSGI.Scripts.Globals;
using UnityEditor;
using UnityEngine;

namespace HTraceSSGI.Scripts.Editor.WindowsAndMenu
{
	public class AssetWelcomeWindow : EditorWindow
	{
		private static Texture2D _icon;
		
		public static void ShowWindow()
		{
			_icon = Resources.Load<Texture2D>("SSGI UI Card");
			
			var window = GetWindow<AssetWelcomeWindow>("Welcome");
			
			Vector2 minSize = new Vector2(600, 240);
			
			if (HExtensions.IsUnityLTS(Application.unityVersion))
				minSize.y -= 45;
			
			if (_icon != null)
				minSize.y += 100;
			
			Vector2 maxSize = minSize - new Vector2(1, 1);
			window.minSize = minSize;
			window.maxSize = maxSize;
			
			Rect main = EditorGUIUtility.GetMainWindowPosition();
			window.position = new Rect(
				main.x + (main.width - minSize.x) / 2,
				main.y + (main.height - minSize.y) / 2,
				minSize.x,
				minSize.y
			);
		}

		private void OnGUI()
		{
			if (_icon != null)
			{
				GUILayout.Space(5);
				Rect rect = GUILayoutUtility.GetAspectRect((float)_icon.width / _icon.height);
				rect.xMin += 4;
				rect.xMax -= 4;
				GUI.DrawTexture(rect, _icon, ScaleMode.ScaleToFit);
				EditorGUILayout.Space(5f);
			}
			
			GUILayout.Space(5);

			GUILayout.Label($"Thank you for purchasing {HNames.ASSET_NAME_FULL_WITH_DOTS}!", EditorStyles.boldLabel);
			GUILayout.Space(5);

			DrawUnityVersionWarning();

			GUILayout.Space(10);
			
			var richLabel = new GUIStyle(EditorStyles.wordWrappedLabel)
			{
				richText = true
			};
			GUILayout.Label(
				"Please make sure to read the <b>Documentation</b> before using the asset.\n" +
				"If you run into any issues, check the <b>Known Issues</b> and <b>FAQ</b> sections before reporting a bug.",
				richLabel
			);
			GUILayout.Space(5);
			
			DrawLinksLine();

			GUILayout.Space(10);
			GUILayout.Label(
				"Shortcuts to the Documentation, Discord support channel, and Bug Report form " +
				"can be found at the bottom of the HTrace UI.",
				EditorStyles.wordWrappedLabel
			);

			GUILayout.Space(15);

			bool dontShow = GUILayout.Toggle(
				EditorPrefs.GetBool(HNames.HTRACE_SHOW_KEY, false),
				"Don't show next time"
			);

			EditorPrefs.SetBool(HNames.HTRACE_SHOW_KEY, dontShow);

			GUILayout.Space(10);

			if (GUILayout.Button("I understand, close window"))
			{
				Close();
			}
		}
		private static void DrawUnityVersionWarning()
        {
            string unityVersion = Application.unityVersion;

            if (!HExtensions.IsUnityLTS(unityVersion))
            {
                EditorGUILayout.HelpBox(
                    $"The current Unity version ({unityVersion}) is not an LTS release.\n" +
                    "Bug fixes for non-LTS releases are not guaranteed.",
                    MessageType.Warning
                );
            }
        }

        private static void DrawLinksLine()
        {
            EditorGUILayout.BeginHorizontal();

            DrawLinkButton("Documentation", HNames.HTRACE_SSGI_DOCUMENTATION_LINK);
            GUILayout.Label("|", GUILayout.Width(10));
            DrawLinkButton("Known Issues", HNames.HTRACE_SSGI_DOCUMENTATION_LINK_KNOWN_ISSUES);
            GUILayout.Label("|", GUILayout.Width(10));
            DrawLinkButton("FAQ", HNames.HTRACE_SSGI_DOCUMENTATION_LINK_FAQ);

            EditorGUILayout.EndHorizontal();
        }

        private static void DrawLinkButton(string label, string url)
        {
            var style = new GUIStyle(EditorStyles.linkLabel)
            {
                wordWrap = false
            };

            if (GUILayout.Button(label, style, GUILayout.Width(EditorStyles.linkLabel.CalcSize(new GUIContent(label)).x)))
            {
                Application.OpenURL(url);
            }
        }
	}
}
#endif
