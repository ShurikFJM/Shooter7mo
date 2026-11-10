//pipelinedefine
#define H_URP

using HTraceSSGI.Scripts.Data.Private;
using HTraceSSGI.Scripts.Data.Public;
using HTraceSSGI.Scripts.Extensions;
using HTraceSSGI.Scripts.Globals;
using HTraceSSGI.Scripts.Passes.Shared;
using HTraceSSGI.Scripts.Wrappers;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

#if UNITY_2023_3_OR_NEWER
using HTraceSSGI.Scripts.Infrastructure.URP;
using UnityEngine.Rendering.RenderGraphModule;
#endif

namespace HTraceSSGI.Scripts.Passes.URP
{
	internal class SSGIPassURP : ScriptableRenderPass 
	{
		private static readonly int CameraNormalsTexture = Shader.PropertyToID("_CameraNormalsTexture");
	    
		// Texture Names
		internal const string _ColorPreviousFrame            = "_ColorPreviousFrame";
		internal const string _DebugOutput                   = "_DebugOutput";
		internal const string _ColorCopy                     = "_ColorCopy";
		internal const string _ReservoirLuminance            = "_ReservoirLuminance";
		internal const string _Reservoir                     = "_Reservoir";
		internal const string _ReservoirReprojected          = "_ReservoirReprojected";
		internal const string _ReservoirSpatial              = "_ReservoirSpatial";
		internal const string _ReservoirTemporal             = "_ReservoirTemporal";
		internal const string _SampleCount                   = "_Samplecount";
		internal const string _SamplecountReprojected        = "_SamplecountReprojected";
		internal const string _TemporalInvalidityFilteredA   = "_TemporalInvalidityFilteredA";
		internal const string _TemporalInvalidityFilteredB   = "_TemporalInvalidityFilteredB";
		internal const string _TemporalInvalidityAccumulated = "_TemporalInvalidityAccumulated";
		internal const string _TemporalInvalidityReprojected = "_TemporalInvalidityReprojected";
		internal const string _SpatialOcclusionAccumulated   = "_SpatialOcclusionAccumulated";
		internal const string _SpatialOcclusionReprojected   = "_SpatialOcclusionReprojected";
		internal const string _AmbientOcclusion              = "_AmbientOcclusion";
		internal const string _AmbientOcclusionGuidance      = "_AmbientOcclusionGuidance";
		internal const string _AmbientOcclusionInvalidity    = "_AmbientOcclusionInvalidity";
		internal const string _AmbientOcclusionAccumulated   = "_AmbientOcclusionAccumulated";
		internal const string _AmbientOcclusionReprojected   = "_AmbientOcclusionReprojected";
		internal const string _Radiance                      = "_Radiance";
		internal const string _RadianceReprojected           = "_RadianceReprojected";
		internal const string _RadianceAccumulated           = "_RadianceAccumulated";
		internal const string _RadianceFiltered              = "_RadianceFiltered";
		internal const string _RadianceInterpolated          = "_RadianceInterpolated";
		internal const string _RadianceStabilized            = "_RadianceStabilized";
		internal const string _RadianceStabilizedReprojected = "_RadianceStabilizedReprojected";
		internal const string _RadianceNormalDepth           = "_RadianceNormalDepth";
		internal const string _ColorReprojected              = "_ColorReprojected";
		internal const string _NormalDepthHistory            = "_NormalDepthHistory";
		internal const string _NormalDepthHistoryFullRes     = "_NormalDepthHistoryFullRes";
		internal const string _DummyBlackTexture             = "_DummyBlackTexture";

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
			SSGI.CameraHistorySystem.UpdateCameraHistoryIndex(renderingData.cameraData.camera.GetHashCode());
			SSGI.CameraHistorySystem.UpdateCameraHistoryData();
			SSGI.CameraHistorySystem.GetCameraData().SetHash(renderingData.cameraData.camera.GetHashCode());

			SetupShared(renderingData.cameraData.camera, renderingData.cameraData.renderScale, renderingData.cameraData.cameraTargetDescriptor);
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
			HTraceSSGIProfile profile = HTraceSSGISettings.ActiveProfile;
			var cmd = CommandBufferPool.Get(HNames.HTRACE_SSGI_PASS_NAME);

			Camera camera = renderingData.cameraData.camera;
			float renderScale = renderingData.cameraData.renderScale;
			int width  = (int)(camera.scaledPixelWidth * renderScale);
			int height = (int)(camera.scaledPixelHeight * renderScale);

			if (Shader.GetGlobalTexture(CameraNormalsTexture) == null)
			    return;

			// ---------------------------------------- AMBIENT LIGHTING OVERRIDE ---------------------------------------- //
			using (new ProfilingScope(cmd, SSGI.AmbientLightingOverrideSampler))
			{	
			   cmd.SetGlobalFloat(SSGI._IndirectLightingIntensity, profile.SSGISettings.Intensity);
			    
			   if (profile.GeneralSettings.AmbientOverride)
			   {
					// Copy Color buffer
				   CoreUtils.SetRenderTarget(cmd, SSGI.ColorCopy_URP.rt);
				   cmd.DrawProcedural(Matrix4x4.identity, SSGI.ColorCompose_URP, 0, MeshTopology.Triangles, 3, 1);

				   // Subtract indirect lighting from Color buffer
				   CoreUtils.SetRenderTarget(cmd, _renderer.cameraColorTargetHandle);
				   SSGI.ColorCompose_URP.SetTexture(SSGI._ColorCopy, SSGI.ColorCopy_URP.rt);
				   cmd.DrawProcedural(Matrix4x4.identity, SSGI.ColorCompose_URP, 1, MeshTopology.Triangles, 3, 1);
			   }

			   // Early out if we want to prview direct lighting only
			   if (profile.GeneralSettings.DebugMode == DebugMode.DirectLighting)
			   {
				   ConfigureTarget(_renderer.cameraColorTargetHandle);

				   context.ExecuteCommandBuffer(cmd);
				   cmd.Clear();
				   CommandBufferPool.Release(cmd);

				   return;
			   }
			}

			SSGI.Execute(cmd, camera, width, height, _renderer.cameraColorTargetHandle);

			// ---------------------------------------- INDIRECT LIGHTING INJECTION ---------------------------------------- //
			using (new ProfilingScope(cmd, SSGI.IndirectLightingInjectionSampler))
			{
				var finalOutput = Mathf.Approximately(profile.SSGISettings.RenderScale, 1.0f) ? SSGI.RadianceFiltered.rt : SSGI.RadianceInterpolated.rt;
				cmd.SetGlobalTexture(SSGI._SampleCountSSGI, SSGI.SamplecountReprojected.rt);
				cmd.SetGlobalTexture(SSGI._HTraceBufferGI, finalOutput);

				// Copy color buffer + indirect lighting (without intensity multiplication) for multibounce
				if (profile.GeneralSettings.Multibounce == true)
				{
					cmd.SetComputeTextureParam(SSGI.HTemporalReprojection, (int)SSGI.HTemporalReprojectionKernels.CopyHistory, SSGI._Radiance_History, _renderer.cameraColorTargetHandle);
					cmd.SetComputeTextureParam(SSGI.HTemporalReprojection, (int)SSGI.HTemporalReprojectionKernels.CopyHistory, SSGI._Radiance_Output, SSGI.CameraHistorySystem.GetCameraData().ColorPreviousFrame.rt);
					cmd.DispatchCompute(SSGI.HTemporalReprojection, (int)SSGI.HTemporalReprojectionKernels.CopyHistory, Mathf.CeilToInt(width / 8.0f), Mathf.CeilToInt(height / 8.0f), HRenderer.TextureXrSlices);
				}

#if UNITY_2023_3_OR_NEWER
				if (profile.GeneralSettings.ExcludeReceivingMask != 0)
					SSGI.ColorCompose_URP.EnableKeyword(SSGI.USE_RECEIVE_LAYER_MASK);
#endif

				// Inject final indirect lighting (with intensity multiplication) into color buffer via additive blending
				CoreUtils.SetRenderTarget(cmd, _renderer.cameraColorTargetHandle);
				SSGI.ColorCompose_URP.SetInt(SSGI._MetallicIndirectFallback, profile.GeneralSettings.MetallicIndirectFallback ? 1 : 0);
				cmd.DrawProcedural(Matrix4x4.identity, SSGI.ColorCompose_URP, 2, MeshTopology.Triangles, 3, 1);

				ConfigureTarget(_renderer.cameraColorTargetHandle);
			}

			SSGI.History.Update();

			context.ExecuteCommandBuffer(cmd);
			cmd.Clear();
			CommandBufferPool.Release(cmd);
			return;
		}
	    
#endif // !UNITY_6000_4_OR_NEWER

		#endregion --------------------------- Non Render Graph ---------------------------

      #region --------------------------- Render Graph ---------------------------

#if UNITY_2023_3_OR_NEWER
		private class PassData
		{
		    public UniversalCameraData UniversalCameraData;
		    public TextureHandle ColorTexture;
		    public TextureHandle DepthTexture;
		}

		public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
		{
			using (var builder = renderGraph.AddUnsafePass<PassData>(HNames.HTRACE_SSGI_PASS_NAME, out var passData, new ProfilingSampler(HNames.HTRACE_SSGI_PASS_NAME)))
			{
				UniversalResourceData  resourceData           = frameData.Get<UniversalResourceData>();
				UniversalCameraData    universalCameraData    = frameData.Get<UniversalCameraData>();
				UniversalRenderingData universalRenderingData = frameData.Get<UniversalRenderingData>();
				UniversalLightData     lightData              = frameData.Get<UniversalLightData>();

				ConfigureInput(ScriptableRenderPassInput.Normal);

				builder.AllowGlobalStateModification(true);
				builder.AllowPassCulling(false);

				RenderTextureDescriptor targetDesc = universalCameraData.cameraTargetDescriptor;
				TextureHandle colorTexture = universalRenderingData.renderingMode == RenderingMode.Deferred
#if UNITY_6000_1_OR_NEWER
				                             || universalRenderingData.renderingMode == RenderingMode.DeferredPlus
#endif
					? resourceData.activeColorTexture : resourceData.cameraColor;
				TextureHandle depthTexture = universalRenderingData.renderingMode == RenderingMode.Deferred
#if UNITY_6000_1_OR_NEWER
				                             || universalRenderingData.renderingMode == RenderingMode.DeferredPlus
#endif
					? resourceData.activeDepthTexture : resourceData.cameraDepth;
				builder.UseTexture(colorTexture, AccessFlags.Write);
				builder.UseTexture(resourceData.cameraNormalsTexture);
				builder.UseTexture(resourceData.motionVectorColor);

				passData.UniversalCameraData = universalCameraData;
				passData.ColorTexture        = colorTexture;
				passData.DepthTexture        = depthTexture;

				Camera camera = universalCameraData.camera;

				SSGI.CameraHistorySystem.UpdateCameraHistoryIndex(camera.GetHashCode());
				SSGI.CameraHistorySystem.UpdateCameraHistoryData();
				SSGI.CameraHistorySystem.GetCameraData().SetHash(camera.GetHashCode());

				SetupShared(camera, universalCameraData.renderScale, universalCameraData.cameraTargetDescriptor);

				builder.SetRenderFunc((PassData data, UnsafeGraphContext context) => ExecutePass(data, context));
			}

		}

		private static void ExecutePass(PassData data, UnsafeGraphContext rgContext) 
		{
		   HTraceSSGIProfile profile = HTraceSSGISettings.ActiveProfile;
			var cmd = CommandBufferHelpers.GetNativeCommandBuffer(rgContext.cmd);

			Camera camera = data.UniversalCameraData.camera;
			float renderScale = data.UniversalCameraData.renderScale;
			int width  = (int)(camera.scaledPixelWidth * renderScale);
			int height = (int)(camera.scaledPixelHeight * renderScale);

			if (Shader.GetGlobalTexture(CameraNormalsTexture) == null)
			    return;

			// ---------------------------------------- AMBIENT LIGHTING OVERRIDE ---------------------------------------- //
			using (new ProfilingScope(cmd, SSGI.AmbientLightingOverrideSampler))
			{
				cmd.SetGlobalFloat(SSGI._IndirectLightingIntensity, profile.SSGISettings.Intensity);
			    
				if (profile.GeneralSettings.AmbientOverride)
				{
					// Copy Color buffer
					CoreUtils.SetRenderTarget(cmd, SSGI.ColorCopy_URP.rt);
					cmd.DrawProcedural(Matrix4x4.identity, SSGI.ColorCompose_URP, 0, MeshTopology.Triangles, 3, 1);

					// Subtract indirect lighting from Color buffer
					CoreUtils.SetRenderTarget(cmd, data.ColorTexture);
					SSGI.ColorCompose_URP.SetTexture(SSGI._ColorCopy, SSGI.ColorCopy_URP.rt);
					cmd.DrawProcedural(Matrix4x4.identity, SSGI.ColorCompose_URP, 1, MeshTopology.Triangles, 3, 1);
				}

				// Early out if we want to prview direct lighting only
				if (profile.GeneralSettings.DebugMode == DebugMode.DirectLighting)
				{
					return;
				}
			}

			SSGI.Execute(cmd, camera, width, height, data.ColorTexture);

			// ---------------------------------------- INDIRECT LIGHTING INJECTION ---------------------------------------- //
			using (new ProfilingScope(cmd, SSGI.IndirectLightingInjectionSampler))
			{
				cmd.SetGlobalTexture(SSGI._HTraceBufferGI, SSGI.finalOutput);

				// Copy color buffer + indirect lighting (without intensity multiplication) for multibounce
				if (profile.GeneralSettings.Multibounce == true)
				{
					cmd.SetComputeTextureParam(SSGI.HTemporalReprojection, (int)SSGI.HTemporalReprojectionKernels.CopyHistory, SSGI._Radiance_History, data.ColorTexture);
					cmd.SetComputeTextureParam(SSGI.HTemporalReprojection, (int)SSGI.HTemporalReprojectionKernels.CopyHistory, SSGI._Radiance_Output, SSGI.CameraHistorySystem.GetCameraData().ColorPreviousFrame.rt);
					cmd.DispatchCompute(SSGI.HTemporalReprojection, (int)SSGI.HTemporalReprojectionKernels.CopyHistory, Mathf.CeilToInt(width / 8.0f), Mathf.CeilToInt(height / 8.0f), HRenderer.TextureXrSlices);
				}

#if UNITY_2023_3_OR_NEWER
				if (profile.GeneralSettings.ExcludeReceivingMask != 0)
					SSGI.ColorCompose_URP.EnableKeyword(SSGI.USE_RECEIVE_LAYER_MASK);
#endif

				// Inject final indirect lighting (with intensity multiplication) into color buffer via additive blending
				CoreUtils.SetRenderTarget(cmd, data.ColorTexture);
				SSGI.ColorCompose_URP.SetInt(SSGI._MetallicIndirectFallback, profile.GeneralSettings.MetallicIndirectFallback ? 1 : 0);
				cmd.DrawProcedural(Matrix4x4.identity, SSGI.ColorCompose_URP, 2, MeshTopology.Triangles, 3, 1);
			}

			SSGI.History.Update();
	    }
#endif
		#endregion --------------------------- Render Graph ---------------------------

		#region ------------------------------------ Shared ------------------------------------
	   private static void SetupShared(Camera camera, float renderScale, RenderTextureDescriptor desc)
		{
			HTraceSSGIProfile profile = HTraceSSGISettings.ActiveProfile;

			if (SSGI.HDebug == null) SSGI.HDebug                               = HExtensions.LoadComputeShader("HDebugSSGI");
			if (SSGI.HReSTIR == null) SSGI.HReSTIR                             = HExtensions.LoadComputeShader("HRestirSSGI");
			if (SSGI.HRenderSSGI == null) SSGI.HRenderSSGI                     = HExtensions.LoadComputeShader("HRenderSSGI");
			if (SSGI.HDenoiser == null) SSGI.HDenoiser                         = HExtensions.LoadComputeShader("HDenoiserSSGI");
			if (SSGI.HInterpolation == null) SSGI.HInterpolation               = HExtensions.LoadComputeShader("HInterpolationSSGI");
			if (SSGI.HCheckerboarding == null) SSGI.HCheckerboarding           = HExtensions.LoadComputeShader("HCheckerboardingSSGI");
			if (SSGI.PyramidGeneration == null) SSGI.PyramidGeneration         = HExtensions.LoadComputeShader("HDepthPyramid");
			if (SSGI.HTemporalReprojection == null) SSGI.HTemporalReprojection = HExtensions.LoadComputeShader("HTemporalReprojectionSSGI");

			if (SSGI.ColorCompose_URP == null) SSGI.ColorCompose_URP = new Material(Shader.Find($"Hidden/{HNames.ASSET_NAME}/ColorComposeURP"));

			int width  = (int)(camera.scaledPixelWidth * renderScale);
			int height = (int)(camera.scaledPixelHeight * renderScale);
			if (desc.width != width || desc.height != height)
				desc = new RenderTextureDescriptor(width, height);

			// Debug.Log($"All params in cameraTargetDescriptor:   width: {desc.width},  height:{desc.height},  volumeDepth: {desc.volumeDepth},  depthBufferBits: {desc.depthBufferBits},  \n" +
			//           $"graphicsFormat: {desc.graphicsFormat},  colorFormat: {desc.colorFormat},  stencilFormat: {desc.stencilFormat},  msaaSamples: {desc.msaaSamples},  \n" +
			//           $"useMipMap: {desc.useMipMap},  autoGenerateMips: {desc.autoGenerateMips},  mipCount: {desc.mipCount},  \n" +
			//           $"enableRandomWrite: {desc.enableRandomWrite},  useDynamicScale: {desc.useDynamicScale}, ");

			desc.depthBufferBits    = 0; // Color and depth cannot be combined in RTHandles
			desc.stencilFormat      = GraphicsFormat.None;
			desc.depthStencilFormat = GraphicsFormat.None;
			desc.msaaSamples        = 1;
			desc.bindMS             = false;
			desc.enableRandomWrite  = true;

			ref var cameraData = ref SSGI.CameraHistorySystem.GetCameraData();
			if (cameraData.ColorPreviousFrame == null) cameraData.ColorPreviousFrame = new RTWrapper();
			if (cameraData.ReservoirTemporal == null) cameraData.ReservoirTemporal = new RTWrapper();
			if (cameraData.SampleCount == null) cameraData.SampleCount = new RTWrapper();
			if (cameraData.NormalDepth == null) cameraData.NormalDepth = new RTWrapper();
			if (cameraData.NormalDepthFullRes == null) cameraData.NormalDepthFullRes = new RTWrapper();
			if (cameraData.Radiance == null) cameraData.Radiance = new RTWrapper();
			if (cameraData.RadianceAccumulated == null) cameraData.RadianceAccumulated = new RTWrapper();
			if (cameraData.SpatialOcclusionAccumulated == null) cameraData.SpatialOcclusionAccumulated = new RTWrapper();
			if (cameraData.TemporalInvalidityAccumulated == null) cameraData.TemporalInvalidityAccumulated = new RTWrapper();
			if (cameraData.AmbientOcclusionAccumulated == null) cameraData.AmbientOcclusionAccumulated = new RTWrapper();

			cameraData.ColorPreviousFrame.ReAllocateIfNeeded(_ColorPreviousFrame, ref desc, graphicsFormat: GraphicsFormat.B10G11R11_UFloatPack32);
			cameraData.ReservoirTemporal.ReAllocateIfNeeded(_ReservoirTemporal, ref desc, graphicsFormat: GraphicsFormat.R32G32B32A32_UInt);
			cameraData.TemporalInvalidityAccumulated.ReAllocateIfNeeded(_TemporalInvalidityAccumulated, ref desc, graphicsFormat: GraphicsFormat.R8G8_UNorm);
			cameraData.SpatialOcclusionAccumulated.ReAllocateIfNeeded(_SpatialOcclusionAccumulated, ref desc, graphicsFormat: GraphicsFormat.R8_UNorm);
			cameraData.AmbientOcclusionAccumulated.ReAllocateIfNeeded(_AmbientOcclusionAccumulated, ref desc, graphicsFormat: GraphicsFormat.R8_UNorm);
			cameraData.Radiance.ReAllocateIfNeeded(_Radiance, ref desc, graphicsFormat: profile.DenoisingSettings.RecurrentBlur ? GraphicsFormat.R16G16B16A16_SFloat : GraphicsFormat.B10G11R11_UFloatPack32);
			cameraData.RadianceAccumulated.ReAllocateIfNeeded(_RadianceAccumulated, ref desc, graphicsFormat: GraphicsFormat.R16G16B16A16_SFloat);
			cameraData.SampleCount.ReAllocateIfNeeded(_SampleCount, ref desc, graphicsFormat: GraphicsFormat.R16_SFloat);
			cameraData.NormalDepth.ReAllocateIfNeeded(_NormalDepthHistory, ref desc, graphicsFormat: GraphicsFormat.R32_UInt);
			cameraData.NormalDepthFullRes.ReAllocateIfNeeded(_NormalDepthHistoryFullRes, ref desc, graphicsFormat: GraphicsFormat.R32_UInt);

			SSGI.ColorCopy_URP.ReAllocateIfNeeded(_ColorCopy, ref desc, graphicsFormat: GraphicsFormat.B10G11R11_UFloatPack32);
			SSGI.DebugOutput.ReAllocateIfNeeded(_ColorCopy, ref desc, graphicsFormat: GraphicsFormat.B10G11R11_UFloatPack32);
			SSGI.ColorReprojected.ReAllocateIfNeeded(_ColorReprojected, ref desc, graphicsFormat: GraphicsFormat.R32_UInt);
			SSGI.Reservoir.ReAllocateIfNeeded(_Reservoir, ref desc, graphicsFormat: GraphicsFormat.R32G32B32A32_UInt);
			SSGI.ReservoirReprojected.ReAllocateIfNeeded(_ReservoirReprojected, ref desc, graphicsFormat: GraphicsFormat.R32G32B32A32_UInt);
			SSGI.ReservoirSpatial.ReAllocateIfNeeded(_ReservoirSpatial, ref desc, graphicsFormat: GraphicsFormat.R32G32B32A32_UInt);
			SSGI.ReservoirLuminance.ReAllocateIfNeeded(_ReservoirLuminance, ref desc, graphicsFormat: GraphicsFormat.R16_SFloat);
			SSGI.TemporalInvalidityFilteredA.ReAllocateIfNeeded(_TemporalInvalidityFilteredA, ref desc, graphicsFormat: GraphicsFormat.R8G8_UNorm);
			SSGI.TemporalInvalidityFilteredB.ReAllocateIfNeeded(_TemporalInvalidityFilteredB, ref desc, graphicsFormat: GraphicsFormat.R8G8_UNorm);
			SSGI.TemporalInvalidityReprojected.ReAllocateIfNeeded(_TemporalInvalidityReprojected, ref desc, graphicsFormat: GraphicsFormat.R8G8_UNorm);
			SSGI.SpatialOcclusionReprojected.ReAllocateIfNeeded(_SpatialOcclusionReprojected, ref desc, graphicsFormat: GraphicsFormat.R8_UNorm);
			SSGI.AmbientOcclusion.ReAllocateIfNeeded(_AmbientOcclusion, ref desc, graphicsFormat: GraphicsFormat.R8_SNorm);
			SSGI.AmbientOcclusionGuidance.ReAllocateIfNeeded(_AmbientOcclusionGuidance, ref desc, graphicsFormat: GraphicsFormat.R8G8_UInt);
			SSGI.AmbientOcclusionInvalidity.ReAllocateIfNeeded(_AmbientOcclusionInvalidity, ref desc, graphicsFormat: GraphicsFormat.R8_UNorm);
			SSGI.AmbientOcclusionReprojected.ReAllocateIfNeeded(_AmbientOcclusionReprojected, ref desc, graphicsFormat: GraphicsFormat.R8_UNorm);
			SSGI.RadianceFiltered.ReAllocateIfNeeded(_RadianceFiltered, ref desc, graphicsFormat: GraphicsFormat.B10G11R11_UFloatPack32);
			SSGI.RadianceReprojected.ReAllocateIfNeeded(_RadianceReprojected, ref desc, graphicsFormat: GraphicsFormat.R16G16B16A16_SFloat);
			SSGI.RadianceNormalDepth.ReAllocateIfNeeded(_RadianceNormalDepth, ref desc, graphicsFormat: GraphicsFormat.R32G32_UInt);
			SSGI.RadianceInterpolated.ReAllocateIfNeeded(_RadianceInterpolated, ref desc, graphicsFormat: GraphicsFormat.B10G11R11_UFloatPack32);
			SSGI.RadianceStabilizedReprojected.ReAllocateIfNeeded(_RadianceStabilizedReprojected, ref desc, graphicsFormat: GraphicsFormat.R16G16B16A16_SFloat);
			SSGI.RadianceStabilized.ReAllocateIfNeeded(_RadianceStabilized, ref desc, graphicsFormat: GraphicsFormat.R16G16B16A16_SFloat);
			SSGI.SamplecountReprojected.ReAllocateIfNeeded(_SamplecountReprojected, ref desc, graphicsFormat: GraphicsFormat.R16_SFloat);
			SSGI.DummyBlackTexture.ReAllocateIfNeeded(_DummyBlackTexture, ref desc, graphicsFormat: GraphicsFormat.R8_UNorm);

			if (SSGI.PointDistributionBuffer == null) SSGI.PointDistributionBuffer = new ComputeBuffer(32 * 4 * HRenderer.TextureXrSlices, 3 * sizeof(int));
			if (SSGI.LuminanceMoments == null) SSGI.LuminanceMoments = new ComputeBuffer(2 * HRenderer.TextureXrSlices, 2 * sizeof(int));
			if (SSGI.IndirectArguments == null) SSGI.IndirectArguments = new ComputeBuffer(3 * HRenderer.TextureXrSlices, sizeof(int), ComputeBufferType.IndirectArguments);
			if (SSGI.IndirectCoords == null) SSGI.IndirectCoords = new HDynamicBuffer(BufferType.ComputeBuffer, 2 * sizeof(uint), HRenderer.TextureXrSlices, avoidDownscale: false);
			SSGI.IndirectCoords.ReAllocIfNeeded(new Vector2Int(width, height));
			
			if (SSGI.RayCounter == null)
			{
				SSGI.RayCounter = new ComputeBuffer(2 * 1, sizeof(uint));
				uint[] zeroArray = new uint[2 * 1];
				SSGI.RayCounter.SetData(zeroArray);
			}
		}

		protected internal void Dispose()
      {
	      var historyCameraDataSSGI = SSGI.CameraHistorySystem.GetCameraData();
			historyCameraDataSSGI.ColorPreviousFrame?.HRelease();
			historyCameraDataSSGI.ReservoirTemporal?.HRelease();
			historyCameraDataSSGI.SampleCount?.HRelease();
			historyCameraDataSSGI.NormalDepth?.HRelease();
			historyCameraDataSSGI.NormalDepthFullRes?.HRelease();
			historyCameraDataSSGI.Radiance?.HRelease();
			historyCameraDataSSGI.RadianceAccumulated?.HRelease();
			historyCameraDataSSGI.SpatialOcclusionAccumulated?.HRelease();
			historyCameraDataSSGI.TemporalInvalidityAccumulated?.HRelease();
			historyCameraDataSSGI.AmbientOcclusionAccumulated?.HRelease();

			SSGI.ColorCopy_URP?.HRelease();
			SSGI.DebugOutput?.HRelease();
			SSGI.ColorReprojected?.HRelease();
			SSGI.Reservoir?.HRelease();
			SSGI.ReservoirReprojected?.HRelease();
			SSGI.ReservoirSpatial?.HRelease();
			SSGI.ReservoirLuminance?.HRelease();
			SSGI.TemporalInvalidityFilteredA?.HRelease();
			SSGI.TemporalInvalidityFilteredB?.HRelease();
			SSGI.TemporalInvalidityReprojected?.HRelease();
			SSGI.SpatialOcclusionReprojected?.HRelease();
			SSGI.AmbientOcclusion?.HRelease();
			SSGI.AmbientOcclusionGuidance?.HRelease();
			SSGI.AmbientOcclusionInvalidity?.HRelease();
			SSGI.AmbientOcclusionReprojected?.HRelease();
			SSGI.RadianceFiltered?.HRelease();
			SSGI.RadianceReprojected?.HRelease();
			SSGI.RadianceNormalDepth?.HRelease();
			SSGI.RadianceInterpolated?.HRelease();
			SSGI.RadianceStabilizedReprojected?.HRelease();
			SSGI.RadianceStabilized?.HRelease();
			SSGI.SamplecountReprojected?.HRelease();
			SSGI.DummyBlackTexture?.HRelease();

			SSGI.PointDistributionBuffer.HRelease();
			SSGI.LuminanceMoments.HRelease();
			SSGI.RayCounter.HRelease();
			SSGI.IndirectCoords.HRelease();
			SSGI.IndirectArguments.HRelease();

			SSGI.PointDistributionBuffer = null;
			SSGI.LuminanceMoments = null;
			SSGI.RayCounter = null;
			SSGI.IndirectCoords = null;
			SSGI.IndirectArguments = null;
		}
		#endregion ------------------------------------ Shared ------------------------------------
	}
}
