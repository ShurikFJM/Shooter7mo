//pipelinedefine
#define H_URP

using HTraceSSGI.Scripts.Data.Private;
using HTraceSSGI.Scripts.Data.Public;
using HTraceSSGI.Scripts.Passes.Shared;
using HTraceSSGI.Scripts.Extensions;
using HTraceSSGI.Scripts.Globals;
using HTraceSSGI.Scripts.Infrastructure.URP;
using HTraceSSGI.Scripts.Wrappers;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

#if UNITY_2023_3_OR_NEWER
using UnityEngine.Rendering.RenderGraphModule;
#endif

namespace HTraceSSGI.Scripts.Passes.URP
{
	internal class FinalPassURP : ScriptableRenderPass
	{
		#region --------------------------- Non Render Graph ---------------------------

#if !UNITY_6000_4_OR_NEWER
		private ScriptableRenderer _renderer;

		protected internal void Initialize(ScriptableRenderer renderer)
		{
			_renderer = renderer;
		}
		
#if UNITY_2023_3_OR_NEWER
		[System.Obsolete]
#endif
		public override void OnCameraSetup(CommandBuffer cmd, ref RenderingData renderingData)
		{
		}
		
#if UNITY_2023_3_OR_NEWER
		[System.Obsolete]
#endif
		public override void Configure(CommandBuffer cmd, RenderTextureDescriptor cameraTextureDescriptor)
		{
		}
		
#if UNITY_2023_3_OR_NEWER
		[System.Obsolete]
#endif
		public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
		{
			var cmd = CommandBufferPool.Get(HNames.HTRACE_FINAL_PASS_NAME);
	
			HTraceSSGIProfile profile = HTraceSSGISettings.ActiveProfile;
			if (profile.GeneralSettings.DebugMode != DebugMode.None && profile.GeneralSettings.DebugMode != DebugMode.DirectLighting)
			{
				Blitter.BlitCameraTexture(cmd, SSGI.DebugOutput.rt, _renderer.cameraColorTargetHandle);
			}

			context.ExecuteCommandBuffer(cmd);
			cmd.Clear();
			CommandBufferPool.Release(cmd);
		}
#endif
		
		#endregion --------------------------- Non Render Graph ---------------------------

		#region --------------------------- Render Graph ---------------------------
		
#if UNITY_2023_3_OR_NEWER
		private class PassData
		{
			public TextureHandle ColorTexture;
		}

	    public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
	    {
		    using (var builder = renderGraph.AddUnsafePass<PassData>(HNames.HTRACE_FINAL_PASS_NAME, out var passData, new ProfilingSampler(HNames.HTRACE_FINAL_PASS_NAME)))
		    {
			    UniversalResourceData  resourceData           = frameData.Get<UniversalResourceData>();
			    UniversalRenderingData universalRenderingData = frameData.Get<UniversalRenderingData>();

			    builder.AllowGlobalStateModification(true);
			    builder.AllowPassCulling(false);
			    
			    passData.ColorTexture = universalRenderingData.renderingMode == RenderingMode.Deferred ? resourceData.activeColorTexture : resourceData.cameraColor;
			    
			    builder.SetRenderFunc((PassData data, UnsafeGraphContext context) => ExecutePass(data, context));
		    }
	    }

	    private static void ExecutePass(PassData data, UnsafeGraphContext rgContext)
	    {
		    var cmd = CommandBufferHelpers.GetNativeCommandBuffer(rgContext.cmd);
		    
		    HTraceSSGIProfile profile = HTraceSSGISettings.ActiveProfile;
		    if (profile.GeneralSettings.DebugMode != DebugMode.None && profile.GeneralSettings.DebugMode != DebugMode.DirectLighting)
		    {
			    Blitter.BlitCameraTexture(cmd, SSGI.DebugOutput.rt, data.ColorTexture);
		    }
	    }
#endif
		#endregion --------------------------- Render Graph ---------------------------
		
		#region --------------------------- Shared ---------------------------
		
		protected internal void Dispose()
		{
		}

		#endregion --------------------------- Shared ---------------------------
	}
}
