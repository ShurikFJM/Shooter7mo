//pipelinedefine
#define H_URP

using UnityEngine;
#if UNITY_EDITOR
using HTraceSSGI.Scripts.PipelelinesConfigurator;
using UnityEditor;
#endif

public static class HTraceSetupBuildBatchmode
{
	public static void ApplyRuntimeResources()
	{
#if UNITY_EDITOR
		if (!Application.isBatchMode)
			return;
		
		Debug.Log("[HTraceSetup] Starting ApplyRuntimeResources in batch mode...");
        
		try
		{
			HPipelinesConfigurator.AlwaysIncludedShaders();
            
			// Save changes
			AssetDatabase.SaveAssets();
			AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            
			Debug.Log("[HTraceSetup] Runtime resources patched successfully");
			EditorApplication.Exit(0);
		}
		catch (System.Exception e)
		{
			Debug.LogError($"[HTraceSetup] Failed to apply runtime resources: {e.Message}\n{e.StackTrace}");
			EditorApplication.Exit(1);
		}
#endif
	}
}
