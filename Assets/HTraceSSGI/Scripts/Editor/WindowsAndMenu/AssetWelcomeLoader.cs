//pipelinedefine
#define H_URP

#if UNITY_EDITOR
using HTraceSSGI.Scripts.Extensions;
using HTraceSSGI.Scripts.Globals;
using UnityEditor;
using UnityEngine;

namespace HTraceSSGI.Scripts.Editor.WindowsAndMenu
{
	[InitializeOnLoad]
	public static class AssetWelcomeLoader
	{
		static AssetWelcomeLoader()
		{
			EditorApplication.delayCall += TryShowWelcome;
		}

		private static void TryShowWelcome()
		{
			if (Application.isBatchMode)
				return;
			
			if (SessionState.GetBool(HNames.HTRACE_WELCOME_SHOW_SESSION, false))
				return;
			SessionState.SetBool(HNames.HTRACE_WELCOME_SHOW_SESSION, true);
			
			bool   dontShowAgain       = EditorPrefs.GetBool(HNames.HTRACE_SHOW_KEY, false);
			string currentUnityVersion = Application.unityVersion;
			string savedUnityVersion   = EditorPrefs.GetString(HNames.HTRACE_UNITY_VERSION_KEY, string.Empty);

			bool unityVersionChanged = savedUnityVersion != currentUnityVersion;
			bool isLts               = HExtensions.IsUnityLTS(currentUnityVersion);

			bool shouldShowWelcome = !dontShowAgain || (unityVersionChanged && !isLts);

			if (shouldShowWelcome)
			{
				AssetWelcomeWindow.ShowWindow();
			}

			EditorPrefs.SetString(HNames.HTRACE_UNITY_VERSION_KEY, currentUnityVersion);
		}
	}
}
#endif
