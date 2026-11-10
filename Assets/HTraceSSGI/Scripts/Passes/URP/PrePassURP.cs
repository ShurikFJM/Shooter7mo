//pipelinedefine
#define H_URP

using HTraceSSGI.Scripts.Data.Private;
using HTraceSSGI.Scripts.Extensions.CameraHistorySystem;
using HTraceSSGI.Scripts.Globals;
using HTraceSSGI.Scripts.Passes.Shared;
using HTraceSSGI.Scripts.Wrappers;
using Unity.Collections;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RendererUtils;
using UnityEngine.Rendering.Universal;

#if UNITY_2023_3_OR_NEWER
using UnityEngine.Rendering.RenderGraphModule;
#else
using UnityEngine.Experimental.Rendering.RenderGraphModule;
#endif

namespace HTraceSSGI.Scripts.Passes.URP
{
	internal class PrePassURP : ScriptableRenderPass
	{
		private static Vector4 s_HRenderScalePrevious = Vector4.one;

		private struct HistoryCameraData : ICameraHistoryData
		{
			private int hash;
			public Matrix4x4 previousViewProjMatrix;
			public Matrix4x4 previousInvViewProjMatrix;

			public int GetHash() => hash;
			public void SetHash(int hashIn) => this.hash = hashIn;
		}

		private static readonly CameraHistorySystem<HistoryCameraData> CameraHistorySystem = new CameraHistorySystem<HistoryCameraData>();
		private static int s_FrameCount = 0;

		#region --------------------------- Non Render Graph ---------------------------
		
#if !UNITY_6000_4_OR_NEWER
		private ScriptableRenderer _renderer;

		protected internal void Initialize(ScriptableRenderer renderer)
		{
			_renderer    = renderer;
		}

#if UNITY_2023_3_OR_NEWER
		[System.Obsolete]
#endif
		public override void OnCameraSetup(CommandBuffer cmd, ref RenderingData renderingData)
		{
			Camera camera = renderingData.cameraData.camera;

			CameraHistorySystem.UpdateCameraHistoryIndex(camera.GetHashCode());
			CameraHistorySystem.UpdateCameraHistoryData();
			CameraHistorySystem.GetCameraData().SetHash(camera.GetHashCode());
		}
		
#if UNITY_2023_3_OR_NEWER
		[System.Obsolete]
#endif
		public override void Configure(CommandBuffer cmd, RenderTextureDescriptor cameraTextureDescriptor)
		{
			ConfigureInput(ScriptableRenderPassInput.Depth); // | ScriptableRenderPassInput.Normal);
		}

#if UNITY_2023_3_OR_NEWER
		[System.Obsolete]
#endif
		public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
		{
			var cmd = CommandBufferPool.Get(HNames.HTRACE_PRE_PASS_NAME);

			Camera camera = renderingData.cameraData.camera;

			// -------------- HRenderScale -----------------
			// Unity's _RTHandleScale in URP always (1,1,1,1)? We need to overwrite it anyway...
			cmd.SetGlobalVector(HShaderParams.HRenderScalePrevious, s_HRenderScalePrevious);
			//s_HRenderScalePrevious = new Vector4(RTHandles.rtHandleProperties.rtHandleScale.x, RTHandles.rtHandleProperties.rtHandleScale.y, 1 / RTHandles.rtHandleProperties.rtHandleScale.x, 1 / RTHandles.rtHandleProperties.rtHandleScale.y); //we don't needed it more
			cmd.SetGlobalVector(HShaderParams.HRenderScale, s_HRenderScalePrevious);

			// -------------- Matrix -----------------
			Matrix4x4 projMatrix        = GL.GetGPUProjectionMatrix(camera.projectionMatrix, true); //renderingData.cameraData.GetGPUProjectionMatrix();
			Matrix4x4 viewMatrix        = renderingData.cameraData.GetViewMatrix();
			Matrix4x4 viewProjMatrix    = projMatrix * viewMatrix;
			Matrix4x4 invViewProjMatrix = Matrix4x4.Inverse(viewProjMatrix);
			{
				cmd.SetGlobalMatrix(HShaderParams.H_MATRIX_VP, viewProjMatrix);
				cmd.SetGlobalMatrix(HShaderParams.H_MATRIX_I_VP, invViewProjMatrix);
				ref var previousViewProjMatrix = ref CameraHistorySystem.GetCameraData().previousViewProjMatrix;
				cmd.SetGlobalMatrix(HShaderParams.H_MATRIX_PREV_VP, previousViewProjMatrix);
				ref var previousInvViewProjMatrix = ref CameraHistorySystem.GetCameraData().previousInvViewProjMatrix;
				cmd.SetGlobalMatrix(HShaderParams.H_MATRIX_PREV_I_VP, previousInvViewProjMatrix);

				previousViewProjMatrix = viewProjMatrix;
				previousInvViewProjMatrix = invViewProjMatrix;
				CameraHistorySystem.GetCameraData().SetHash(camera.GetHashCode());

				// HistoryCameraData currentData = CameraHistorySystem.GetCameraData();
				// currentData.previousViewProjMatrix = viewProjMatrix;
				// currentData.previousInvViewProjMatrix = invViewProjMatrix;
				// CameraHistorySystem.SetCameraData(currentData);
			}


			// -------------- Other -----------------
			cmd.SetGlobalInt(HShaderParams.FrameCount, s_FrameCount);
			s_FrameCount++;
			//	Unity's blue noise is unreliable, so we'll use ours in all pipelines
			HBlueNoise.SetTextures(cmd);

			context.ExecuteCommandBuffer(cmd);
			cmd.Clear();
			CommandBufferPool.Release(cmd);
		}
#endif //# !UNITY_6000_4_OR_NEWER
		
		#endregion --------------------------- Non Render Graph ---------------------------

		#region --------------------------- Render Graph ---------------------------

#if UNITY_2023_3_OR_NEWER
		RTHandle OwenScrambledRTHandle;
		RTHandle ScramblingTileXSPPRTHandle;
		RTHandle RankingTileXSPPRTHandle;
		RTHandle ScramblingTextureRTHandle;

		private class PassData
		{
			public RendererListHandle  RendererListHandle;
			public UniversalCameraData UniversalCameraData;
		}

		public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
		{
			using (var builder = renderGraph.AddRasterRenderPass<PassData>(HNames.HTRACE_PRE_PASS_NAME, out var passData, new ProfilingSampler(HNames.HTRACE_PRE_PASS_NAME)))
			{
				UniversalResourceData  resourceData           = frameData.Get<UniversalResourceData>();
				UniversalCameraData    universalCameraData    = frameData.Get<UniversalCameraData>();
				UniversalRenderingData universalRenderingData = frameData.Get<UniversalRenderingData>();
				UniversalLightData     lightData              = frameData.Get<UniversalLightData>();

				ConfigureInput(ScriptableRenderPassInput.Depth | ScriptableRenderPassInput.Normal);

				builder.AllowGlobalStateModification(true);
				builder.AllowPassCulling(false);

				passData.UniversalCameraData  = universalCameraData;

				Camera camera = universalCameraData.camera;

				CameraHistorySystem.UpdateCameraHistoryIndex(camera.GetHashCode());
				CameraHistorySystem.UpdateCameraHistoryData();
				CameraHistorySystem.GetCameraData().SetHash(camera.GetHashCode());
				
				//Blue noise
				if (OwenScrambledRTHandle == null) OwenScrambledRTHandle = RTHandles.Alloc(HBlueNoise.OwenScrambledTexture);
				TextureHandle owenScrambledTextureHandle = renderGraph.ImportTexture(OwenScrambledRTHandle);
				builder.SetGlobalTextureAfterPass(owenScrambledTextureHandle, HBlueNoise.g_OwenScrambledTexture);

				if (ScramblingTileXSPPRTHandle == null) ScramblingTileXSPPRTHandle = RTHandles.Alloc(HBlueNoise.ScramblingTileXSPP);
				TextureHandle scramblingTileXSPPTextureHandle = renderGraph.ImportTexture(ScramblingTileXSPPRTHandle);
				builder.SetGlobalTextureAfterPass(scramblingTileXSPPTextureHandle, HBlueNoise.g_ScramblingTileXSPP);

				if (RankingTileXSPPRTHandle == null) RankingTileXSPPRTHandle = RTHandles.Alloc(HBlueNoise.RankingTileXSPP);
				TextureHandle rankingTileXSPPTextureHandle = renderGraph.ImportTexture(RankingTileXSPPRTHandle);
				builder.SetGlobalTextureAfterPass(rankingTileXSPPTextureHandle, HBlueNoise.g_RankingTileXSPP);

				if (ScramblingTextureRTHandle == null) ScramblingTextureRTHandle = RTHandles.Alloc(HBlueNoise.ScramblingTexture);
				TextureHandle scramblingTextureHandle = renderGraph.ImportTexture(ScramblingTextureRTHandle);
				builder.SetGlobalTextureAfterPass(scramblingTextureHandle, HBlueNoise.g_ScramblingTexture);

				builder.SetRenderFunc((PassData data, RasterGraphContext context) => ExecutePass(data, context));
			}
		}

		private static void ExecutePass(PassData data, RasterGraphContext rgContext)
		{
			var cmd = rgContext.cmd;

			Camera camera = data.UniversalCameraData.camera;

			// -------------- HRenderScale -----------------
			// Unity's _RTHandleScale in URP always (1,1,1,1)? We need to overwrite it anyway...
			cmd.SetGlobalVector(HShaderParams.HRenderScalePrevious, s_HRenderScalePrevious);
			//s_HRenderScalePrevious = new Vector4(RTHandles.rtHandleProperties.rtHandleScale.x, RTHandles.rtHandleProperties.rtHandleScale.y, 1 / RTHandles.rtHandleProperties.rtHandleScale.x, 1 / RTHandles.rtHandleProperties.rtHandleScale.y); //we don't needed it more
			cmd.SetGlobalVector(HShaderParams.HRenderScale, s_HRenderScalePrevious);

			// -------------- Matrix -----------------

			Matrix4x4 projMatrix        = GL.GetGPUProjectionMatrix(camera.projectionMatrix, true); //renderingData.cameraData.GetGPUProjectionMatrix();
			Matrix4x4 viewMatrix        = data.UniversalCameraData.GetViewMatrix();
			Matrix4x4 viewProjMatrix    = projMatrix * viewMatrix;
			Matrix4x4 invViewProjMatrix = Matrix4x4.Inverse(viewProjMatrix);
			{
				cmd.SetGlobalMatrix(HShaderParams.H_MATRIX_VP, viewProjMatrix);
				cmd.SetGlobalMatrix(HShaderParams.H_MATRIX_I_VP, invViewProjMatrix);
				ref var previousViewProjMatrix = ref CameraHistorySystem.GetCameraData().previousViewProjMatrix;
				cmd.SetGlobalMatrix(HShaderParams.H_MATRIX_PREV_VP, previousViewProjMatrix);
				ref var previousInvViewProjMatrix = ref CameraHistorySystem.GetCameraData().previousInvViewProjMatrix;
				cmd.SetGlobalMatrix(HShaderParams.H_MATRIX_PREV_I_VP, previousInvViewProjMatrix);

				previousViewProjMatrix = viewProjMatrix;
				previousInvViewProjMatrix = invViewProjMatrix;
				CameraHistorySystem.GetCameraData().SetHash(camera.GetHashCode());

				// HistoryCameraData currentData = CameraHistorySystem.GetCameraData();
				// currentData.previousViewProjMatrix = viewProjMatrix;
				// currentData.previousInvViewProjMatrix = invViewProjMatrix;
				// CameraHistorySystem.SetCameraData(currentData);
			}

			// -------------- Other -----------------
			cmd.SetGlobalInt(HShaderParams.FrameCount, s_FrameCount);
			s_FrameCount++;
		}
#endif

		#endregion --------------------------- Render Graph ---------------------------
		
		protected internal void Dispose()
		{
			s_FrameCount = 0;
		}
	}
}
