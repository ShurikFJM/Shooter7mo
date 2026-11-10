//pipelinedefine
#define H_URP

#if UNITY_EDITOR
using HTraceSSGI.Scripts.Data.Private;
using HTraceSSGI.Scripts.Extensions;
using HTraceSSGI.Scripts.Globals;
using UnityEditor;
using UnityEditor.ShortcutManagement;
using UnityEngine;
using HTraceSSGI.Scripts.Infrastructure.URP;

namespace HTraceSSGI.Scripts.Editor.WindowsAndMenu
{

#if UNITY_EDITOR
	
	public class HMenuAndFilesManager : EditorWindow
	{
		[MenuItem("GameObject/Rendering/HTrace Screen Space Global Illumination", false, priority: 30)]
		static void CreateHTraceGameObject(MenuCommand menuCommand)
		{
			HTraceSSGI[] hTraces = FindObjectsOfType(typeof(HTraceSSGI)) as HTraceSSGI[];
			if (hTraces != null && hTraces.Length > 0)
			{
				return;
			}

			GameObject go = new GameObject(HNames.ASSET_NAME);
			GameObjectUtility.SetParentAndAlign(go, menuCommand.context as GameObject);
			go.AddComponent<HTraceSSGI>();

			Undo.RegisterCreatedObjectUndo(go, "Create " + go.name);
			Selection.activeObject = go;
		}

		[MenuItem("Window/HTrace/Add HTrace SSGI Render Feature to active RendererData", false, priority: 32)]
		private static void AddRenderFeature()
		{
			HRendererURP.AddHTraceRendererFeatureToUniversalRendererData();
		}

		[MenuItem("Window/HTrace/Open HTrace SSGI documentation", false, priority: 32)]
		private static void OpenDocumentation()
		{
			Application.OpenURL(HNames.HTRACE_SSGI_DOCUMENTATION_LINK);
		}
	}
	
#endif
}
#endif
