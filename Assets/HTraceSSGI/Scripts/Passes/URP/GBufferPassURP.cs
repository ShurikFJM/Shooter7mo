//pipelinedefine
#define H_URP

using HTraceSSGI.Scripts.Data.Private;
using HTraceSSGI.Scripts.Data.Public;
using HTraceSSGI.Scripts.Extensions;
using HTraceSSGI.Scripts.Globals;
using HTraceSSGI.Scripts.Infrastructure.URP;
using HTraceSSGI.Scripts.Wrappers;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RendererUtils;
using UnityEngine.Rendering.Universal;

#if UNITY_2023_3_OR_NEWER
using UnityEngine.Rendering.RenderGraphModule;
#endif

namespace HTraceSSGI.Scripts.Passes.URP
{
	internal class GBufferPassURP : ScriptableRenderPass
	{	
		// Texture Names
		private const string _Dummy                  = "_Dummy";
		private const string _DepthPyramid           = "_DepthPyramid";
		private const string _ForwardGBuffer0        = "_ForwardGBuffer0";
		private const string _ForwardGBuffer1        = "_ForwardGBuffer1";
		private const string _ForwardGBuffer2        = "_ForwardGBuffer2";
		private const string _ForwardGBuffer3        = "_ForwardGBuffer3";
		private const string _ForwardRenderLayerMask = "_ForwardRenderLayerMask";
		private const string _ForwardGBufferDepth    = "_ForwardGBufferDepth";

		// Materials & Computes
		internal static ComputeShader HDepthPyramid = null;

		// Samplers
		internal static readonly ProfilingSampler GBufferProfilingSampler = new("GBuffer");
		private static readonly  ProfilingSampler DepthPyramidGenerationProfilingSampler = new ProfilingSampler("Depth Pyramid Generation");

		// Textures
		internal static RTWrapper Dummy = new RTWrapper();
		internal static RTWrapper ForwardGBuffer0 = new RTWrapper();
		internal static RTWrapper ForwardGBuffer1 = new RTWrapper();
		internal static RTWrapper ForwardGBuffer2 = new RTWrapper();
		internal static RTWrapper ForwardGBuffer3 = new RTWrapper();
		internal static RTWrapper ForwardRenderLayerMask = new RTWrapper();
		internal static RTWrapper ForwardGBufferDepth = new RTWrapper();
		internal static RTWrapper DepthPyramidRT = new RTWrapper();

		// MRT Arrays
		internal static RenderTargetIdentifier[] GBufferMRT = null;

		// Misc
		internal static RenderStateBlock ForwardGBufferRenderStateBlock = new RenderStateBlock(RenderStateMask.Nothing);

		#region --------------------------- Non Render Graph ---------------------------

#if !UNITY_6000_4_OR_NEWER
		private ScriptableRenderer _renderer;

		protected internal void Initialize(ScriptableRenderer renderer)
		{
			_renderer       = renderer;
		}
		
#if UNITY_2023_3_OR_NEWER
		[System.Obsolete]
#endif
		public override void OnCameraSetup(CommandBuffer cmd, ref RenderingData renderingData)
		{
			Setup(renderingData.cameraData.camera, renderingData.cameraData.renderScale, renderingData.cameraData.cameraTargetDescriptor);
		}
		
		private static void Setup(Camera camera, float renderScale, RenderTextureDescriptor desc)
		{
			if (HDepthPyramid == null) HDepthPyramid = HExtensions.LoadComputeShader("HDepthPyramid");

			int width  = (int)(camera.scaledPixelWidth * renderScale);
			int height = (int)(camera.scaledPixelHeight * renderScale);

			if (desc.width != width || desc.height != height)
				desc = new RenderTextureDescriptor(width, height);
			desc.depthBufferBits    = 0; // Color and depth cannot be combined in RTHandles
			desc.stencilFormat      = GraphicsFormat.None;
			desc.depthStencilFormat = GraphicsFormat.None;
			desc.msaaSamples        = 1;
			desc.bindMS             = false;

			var graphicsFormatRenderingLayerMask = GraphicsFormat.R8G8B8A8_UNorm;

			#if UNITY_6000_2_OR_NEWER
			graphicsFormatRenderingLayerMask = GraphicsFormat.R8G8_UInt;
			#endif

			ForwardGBuffer0.ReAllocateIfNeeded(_ForwardGBuffer0, ref desc, graphicsFormat: GraphicsFormat.R8G8B8A8_SRGB);
			ForwardGBuffer1.ReAllocateIfNeeded(_ForwardGBuffer1,  ref desc, graphicsFormat: GraphicsFormat.R8G8B8A8_UNorm);
			ForwardGBuffer2.ReAllocateIfNeeded(_ForwardGBuffer2,  ref desc, graphicsFormat: GraphicsFormat.R8G8B8A8_SNorm);
			ForwardGBuffer3.ReAllocateIfNeeded(_ForwardGBuffer3,  ref desc, graphicsFormat: GraphicsFormat.B10G11R11_UFloatPack32);
			ForwardRenderLayerMask.ReAllocateIfNeeded(_ForwardRenderLayerMask,  ref desc, graphicsFormat: graphicsFormatRenderingLayerMask);
			DepthPyramidRT.ReAllocateIfNeeded(_DepthPyramid,  ref desc, graphicsFormat: GraphicsFormat.R16_SFloat, useMipMap: true);

			RenderTextureDescriptor depthDesc = desc;
			depthDesc.depthBufferBits = 32;
			depthDesc.depthStencilFormat = GraphicsFormat.None;
			ForwardGBufferDepth.ReAllocateIfNeeded(_ForwardGBufferDepth,  ref depthDesc, graphicsFormat: GraphicsFormat.R16_SFloat, useMipMap: false, enableRandomWrite: false);
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
			Camera camera = renderingData.cameraData.camera;

			var cmd = CommandBufferPool.Get(HNames.HTRACE_GBUFFER_PASS_NAME);

			int width  = (int)(camera.scaledPixelWidth * renderingData.cameraData.renderScale);
			int height = (int)(camera.scaledPixelHeight * renderingData.cameraData.renderScale);
			
			var nativeGBuffer0 = Shader.GetGlobalTexture(HShaderParams._GBuffer0);
			var nativeGBuffer1 = Shader.GetGlobalTexture(HShaderParams._GBuffer1);
			var nativeGBuffer2 = Shader.GetGlobalTexture(HShaderParams._GBuffer2);
			var renderLayerMaskTexture = Shader.GetGlobalTexture(HShaderParams._CameraRenderingLayersTexture);
			var screenSpaceOcclusionTexture = Shader.GetGlobalTexture(HShaderParams.g_ScreenSpaceOcclusionTexture);
			
			// Set Depth, Color and SSAO to HTrace passes
			cmd.SetGlobalTexture(HShaderParams.g_HTraceColor, _renderer.cameraColorTargetHandle);
			cmd.SetGlobalTexture(HShaderParams.g_HTraceDepth, _renderer.cameraDepthTargetHandle);
			cmd.SetGlobalTexture(HShaderParams.g_HTraceSSAO, screenSpaceOcclusionTexture == null ? Texture2D.whiteTexture : screenSpaceOcclusionTexture);
			
			GBufferGenerationNonRenderGraph(cmd, width, height, nativeGBuffer0, nativeGBuffer1, nativeGBuffer2, renderLayerMaskTexture, _renderer.cameraColorTargetHandle, _renderer.cameraDepthTargetHandle, ref context, ref renderingData);

			GenerateDepthPyramidShared(cmd, width, height, DepthPyramidRT.rt);

			context.ExecuteCommandBuffer(cmd);
			cmd.Clear();
			CommandBufferPool.Release(cmd);
		}


		private static void GBufferGenerationNonRenderGraph(CommandBuffer cmd, int width, int height, Texture nativeGBuffer0, Texture nativeGBuffer1,
			Texture nativeGBuffer2, Texture renderLayerMask, RTHandle cameraColorBuffer, RTHandle cameraDepthBuffer,
			ref ScriptableRenderContext context, ref RenderingData renderingData)
		{
			HTraceSSGIProfile profile = HTraceSSGISettings.ActiveProfile;
			var camera = renderingData.cameraData.camera;
			using (new ProfilingScope(cmd, GBufferProfilingSampler))
			{
				// Set sky probe management
				SphericalHarmonicsL2 ambientProbe = RenderSettings.ambientProbe;
				cmd.SetGlobalVector(HShaderParams.H_SHAr, new Vector4(ambientProbe[0, 3], ambientProbe[0, 1], ambientProbe[0, 2], ambientProbe[0, 0] - ambientProbe[0, 6]));
				cmd.SetGlobalVector(HShaderParams.H_SHAg, new Vector4(ambientProbe[1, 3], ambientProbe[1, 1], ambientProbe[1, 2], ambientProbe[1, 0] - ambientProbe[1, 6]));
				cmd.SetGlobalVector(HShaderParams.H_SHAb, new Vector4(ambientProbe[2, 3], ambientProbe[2, 1], ambientProbe[2, 2], ambientProbe[2, 0] - ambientProbe[2, 6]));
				cmd.SetGlobalVector(HShaderParams.H_SHBr, new Vector4(ambientProbe[0, 4], ambientProbe[0, 5], ambientProbe[0, 6] * 3, ambientProbe[0, 7]));
				cmd.SetGlobalVector(HShaderParams.H_SHBg, new Vector4(ambientProbe[1, 4], ambientProbe[1, 5], ambientProbe[1, 6] * 3, ambientProbe[1, 7]));
				cmd.SetGlobalVector(HShaderParams.H_SHBb, new Vector4(ambientProbe[2, 4], ambientProbe[2, 5], ambientProbe[2, 6] * 3, ambientProbe[2, 7]));
				cmd.SetGlobalVector(HShaderParams.H_SHC, new Vector4(ambientProbe[0, 8], ambientProbe[1, 8], ambientProbe[2, 8], 1));

				// Check if GBuffer is valid (e.g. Forward / wrong scale / is not set, etc.)
				bool RequestForwardGBufferRender = false;
				if (nativeGBuffer0 == null || nativeGBuffer1 == null || nativeGBuffer2 == null)
				{ RequestForwardGBufferRender = true; }
				else if ( nativeGBuffer0.width != width || nativeGBuffer0.height != height) // CameraDepthBuffer.rtHandleProperties.currentViewportSize.x
				{ RequestForwardGBufferRender = true; }

				// Set Render Layer Mask to black dummy in case it's disabled as a feature and we can't render it
				if (renderLayerMask == null)
					renderLayerMask = Texture2D.blackTexture;// Dummy.rt;

				// RequestForwardGBufferRender = true;

				// GBuffer can't be rendered because its resolution doesn't match the resolution of Unity's depth buffer. Happens when switching between Scene and Game windows
				if (ForwardGBuffer0.rt.rt.width != cameraDepthBuffer.rt.width || ForwardGBuffer0.rt.rt.height != cameraDepthBuffer.rt.height)
				{
					cmd.SetGlobalTexture(HShaderParams.g_HTraceGBuffer0, ForwardGBuffer0.rt);
					cmd.SetGlobalTexture(HShaderParams.g_HTraceGBuffer1, ForwardGBuffer1.rt);
					cmd.SetGlobalTexture(HShaderParams.g_HTraceGBuffer2, ForwardGBuffer2.rt);
					cmd.SetGlobalTexture(HShaderParams.g_HTraceRenderLayerMask, ForwardRenderLayerMask.rt);
					return;
				}

				// Set GBuffer to HTrace passes if valid or render it otherwise
				if (RequestForwardGBufferRender == false)
				{
					cmd.SetGlobalTexture(HShaderParams.g_HTraceGBuffer0, nativeGBuffer0);
					cmd.SetGlobalTexture(HShaderParams.g_HTraceGBuffer1, nativeGBuffer1);
					cmd.SetGlobalTexture(HShaderParams.g_HTraceGBuffer2, nativeGBuffer2);
					cmd.SetGlobalTexture(HShaderParams.g_HTraceRenderLayerMask, renderLayerMask);
				}
				else
				{

					if (HRendererURP.RenderGraphEnabled)
						GBufferMRT = new RenderTargetIdentifier[] { ForwardGBuffer0.rt, ForwardGBuffer1.rt, ForwardGBuffer2.rt, ForwardGBuffer3.rt, ForwardGBufferDepth.rt, ForwardRenderLayerMask.rt };
					else
						GBufferMRT = new RenderTargetIdentifier[] { ForwardGBuffer0.rt, ForwardGBuffer1.rt, ForwardGBuffer2.rt, ForwardGBuffer3.rt, ForwardRenderLayerMask.rt };

					// If CameraDepthBuffer.rt doesn't work for any reason - we can replace it with our ForwardGBufferDepth.rt, but GBuffer rendering performance will suffer.
					CoreUtils.SetRenderTarget(cmd, GBufferMRT, cameraDepthBuffer.rt);
					
					CullingResults cullingResults = renderingData.cullResults;
					int layerMask = camera.cullingMask;

					ForwardGBufferRenderStateBlock.depthState = new DepthState(false, CompareFunction.LessEqual);
					ForwardGBufferRenderStateBlock.mask |= RenderStateMask.Depth;

					var renderList = new UnityEngine.Rendering.RendererUtils.RendererListDesc(HShaderParams.UniversalGBufferTag, cullingResults, camera)
					{
						rendererConfiguration = PerObjectData.None,
						renderQueueRange = RenderQueueRange.opaque,
						sortingCriteria = SortingCriteria.OptimizeStateChanges,
						layerMask = layerMask,
						stateBlock = ForwardGBufferRenderStateBlock,
					};
					
					// Cache the current keyword state set by Unity
					bool RenderLayerKeywordState = Shader.IsKeywordEnabled(HShaderParams._WRITE_RENDERING_LAYERS);
					
					// If we don't need render layers we do not touch the keyword at all
#if UNITY_2023_3_OR_NEWER
					if (profile.GeneralSettings.ExcludeReceivingMask != 0 || profile.GeneralSettings.ExcludeCastingMask != 0) 
						CoreUtils.SetKeyword(cmd, HShaderParams._WRITE_RENDERING_LAYERS, true);
#endif
					
					CoreUtils.DrawRendererList(context, cmd, context.CreateRendererList(renderList));

					// If we altered the keyword, we restore it to the original state
#if UNITY_2023_3_OR_NEWER
					if (profile.GeneralSettings.ExcludeReceivingMask != 0 || profile.GeneralSettings.ExcludeCastingMask != 0) 
						CoreUtils.SetKeyword(cmd, HShaderParams._WRITE_RENDERING_LAYERS, RenderLayerKeywordState);
#endif
					
					cmd.SetGlobalTexture(HShaderParams.g_HTraceGBuffer0, ForwardGBuffer0.rt);
					cmd.SetGlobalTexture(HShaderParams.g_HTraceGBuffer1, ForwardGBuffer1.rt);
					cmd.SetGlobalTexture(HShaderParams.g_HTraceGBuffer2, ForwardGBuffer2.rt);
					cmd.SetGlobalTexture(HShaderParams.g_HTraceRenderLayerMask, ForwardRenderLayerMask.rt);
				}
			}
		}
#endif
		
		#endregion --------------------------- Non Render Graph ---------------------------

		#region --------------------------- Render Graph ---------------------------
		
#if UNITY_2023_3_OR_NEWER
		private class PassData
		{
			public TextureHandle[]     GBufferTextures;
			public TextureHandle       SSAOTexture;
			public TextureHandle       ColorTexture;
			public TextureHandle       DepthTexture;
			public TextureHandle       NormalsTexture;
			public RendererListHandle  ForwardGBufferRendererListHandle;
			public UniversalCameraData UniversalCameraData;
			
			public TextureHandle       ForwardGBuffer0Texture;
			public TextureHandle       ForwardGBuffer1Texture;
			public TextureHandle       ForwardGBuffer2Texture;
			public TextureHandle       ForwardGBuffer3Texture;
			public TextureHandle       ForwardRenderLayerMaskTexture;
			public TextureHandle       DepthPyramidTexture;
			public TextureHandle       DepthPyramidIntermediateTexture;
			public TextureHandle       ForwardGBufferDepthTexture;
		}

	    public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
	    {
		    using (var builder = renderGraph.AddUnsafePass<PassData>(HNames.HTRACE_GBUFFER_PASS_NAME, out var passData, new ProfilingSampler(HNames.HTRACE_GBUFFER_PASS_NAME)))
		    {
			    UniversalResourceData  resourceData           = frameData.Get<UniversalResourceData>();
			    UniversalCameraData    universalCameraData    = frameData.Get<UniversalCameraData>();
			    UniversalRenderingData universalRenderingData = frameData.Get<UniversalRenderingData>();
			    UniversalLightData     lightData              = frameData.Get<UniversalLightData>();
			    
			    builder.AllowGlobalStateModification(true);
			    builder.AllowPassCulling(false);

			    TextureHandle colorTexture = universalRenderingData.renderingMode == RenderingMode.Deferred ? resourceData.activeColorTexture : resourceData.cameraColor;
			    TextureHandle depthTexture = universalRenderingData.renderingMode == RenderingMode.Deferred ? resourceData.activeDepthTexture : resourceData.cameraDepth;
			    TextureHandle normalsTexture = resourceData.cameraNormalsTexture;
			    builder.UseTexture(depthTexture, AccessFlags.Read);
			    builder.UseTexture(normalsTexture, AccessFlags.Read);
			    
			    passData.NormalsTexture      = resourceData.cameraNormalsTexture;
			    passData.ColorTexture        = colorTexture;
			    passData.DepthTexture        = depthTexture;
			    passData.UniversalCameraData = universalCameraData;
			    passData.GBufferTextures     = resourceData.gBuffer;
			    passData.SSAOTexture         = resourceData.ssaoTexture;
			    
			    if (HDepthPyramid == null) HDepthPyramid = HExtensions.LoadComputeShader("HDepthPyramid");

			    TextureHandle colorTextureHandle = resourceData.cameraColor;
			    TextureDesc desc = colorTextureHandle.GetDescriptor(renderGraph);
			    desc.clearBuffer = false;
			    TextureHandle depthTextureHandle = resourceData.cameraDepthTexture;
			    TextureDesc descDepth = depthTextureHandle.GetDescriptor(renderGraph);
			    descDepth.clearBuffer = false;
			    var graphicsFormatRenderingLayerMask = GraphicsFormat.R8G8B8A8_UNorm;

#if UNITY_6000_2_OR_NEWER
				graphicsFormatRenderingLayerMask = GraphicsFormat.R8G8_UInt;
#endif
			    passData.ForwardGBuffer0Texture = ExtensionsURP.CreateTexture(_ForwardGBuffer0, renderGraph, ref desc, format: GraphicsFormat.R8G8B8A8_SRGB);
			    builder.UseTexture(passData.ForwardGBuffer0Texture, AccessFlags.Write);
			    passData.ForwardGBuffer1Texture = ExtensionsURP.CreateTexture(_ForwardGBuffer1, renderGraph, ref desc, format: GraphicsFormat.R8G8B8A8_UNorm);
			    builder.UseTexture(passData.ForwardGBuffer1Texture, AccessFlags.Write);
			    passData.ForwardGBuffer2Texture = ExtensionsURP.CreateTexture(_ForwardGBuffer2, renderGraph, ref desc, format: GraphicsFormat.R8G8B8A8_SNorm);
			    builder.UseTexture(passData.ForwardGBuffer2Texture, AccessFlags.Write);
			    passData.ForwardGBuffer3Texture = ExtensionsURP.CreateTexture(_ForwardGBuffer3, renderGraph, ref desc, format: GraphicsFormat.B10G11R11_UFloatPack32);
			    builder.UseTexture(passData.ForwardGBuffer3Texture, AccessFlags.Write);
			    passData.ForwardRenderLayerMaskTexture = ExtensionsURP.CreateTexture(_ForwardRenderLayerMask, renderGraph, ref desc, format: graphicsFormatRenderingLayerMask);
			    builder.UseTexture(passData.ForwardRenderLayerMaskTexture, AccessFlags.Write);
			    passData.DepthPyramidTexture = ExtensionsURP.CreateTexture(_DepthPyramid, renderGraph, ref desc, format: GraphicsFormat.R16_SFloat, useMipMap: true);
			    builder.UseTexture(passData.DepthPyramidTexture, AccessFlags.Write);
			    
			    desc.width /= 16;
			    desc.height /= 16;
				
			    passData.ForwardGBufferDepthTexture = ExtensionsURP.CreateTexture(_ForwardGBufferDepth, renderGraph, ref descDepth, format: GraphicsFormat.R16_SFloat, useMipMap: false, enableRandomWrite: false);
			    builder.UseTexture(passData.ForwardGBufferDepthTexture, AccessFlags.Write);

			    if (universalRenderingData.renderingMode == RenderingMode.Deferred
#if UNITY_6000_1_OR_NEWER
						|| universalRenderingData.renderingMode == RenderingMode.DeferredPlus
#endif
			       )
			    {
				    if (resourceData.ssaoTexture.IsValid())
						builder.UseTexture(resourceData.ssaoTexture, AccessFlags.Read);
				    builder.UseTexture(resourceData.gBuffer[0], AccessFlags.Read);
				    builder.UseTexture(resourceData.gBuffer[1], AccessFlags.Read);
				    builder.UseTexture(resourceData.gBuffer[2], AccessFlags.Read);
				    builder.UseTexture(resourceData.gBuffer[3], AccessFlags.Read);
				    if (resourceData.gBuffer.Length >= 6 && resourceData.gBuffer[5].IsValid())
					    builder.UseTexture(resourceData.gBuffer[5], AccessFlags.Read);
			    }


			    CullingResults cullingResults = universalRenderingData.cullResults;
			    ShaderTagId    tags           = new ShaderTagId("UniversalGBuffer");
			    int            layerMask      = universalCameraData.camera.cullingMask;

			    ForwardGBufferRenderStateBlock.depthState =  new DepthState(false, CompareFunction.LessEqual);
			    ForwardGBufferRenderStateBlock.mask       |= RenderStateMask.Depth;

			    RendererListDesc rendererListDesc = new RendererListDesc(tags, cullingResults, universalCameraData.camera)
			    {
				    rendererConfiguration = PerObjectData.None,
				    renderQueueRange      = RenderQueueRange.opaque,
				    sortingCriteria       = SortingCriteria.OptimizeStateChanges,
				    layerMask             = layerMask,
				    stateBlock = ForwardGBufferRenderStateBlock,
			    };

			    passData.ForwardGBufferRendererListHandle = renderGraph.CreateRendererList(rendererListDesc);
			    builder.UseRendererList(passData.ForwardGBufferRendererListHandle);

			    builder.SetRenderFunc((PassData data, UnsafeGraphContext context) => ExecutePass(data, context));
		    }
	    }

	    private static void ExecutePass(PassData data, UnsafeGraphContext rgContext)
	    {
		    var cmd = CommandBufferHelpers.GetNativeCommandBuffer(rgContext.cmd);

		    int width  = (int)(data.UniversalCameraData.camera.scaledPixelWidth * data.UniversalCameraData.renderScale);
		    int height = (int)(data.UniversalCameraData.camera.scaledPixelHeight * data.UniversalCameraData.renderScale);

		    // Set Depth, Color and SSAO to HTrace passes
		    cmd.SetGlobalTexture(HShaderParams.g_HTraceDepth, data.DepthTexture);
		    cmd.SetGlobalTexture(HShaderParams.g_HTraceColor, data.ColorTexture);
		    cmd.SetGlobalTexture(HShaderParams.g_HTraceGBuffer2, data.NormalsTexture);
		    cmd.SetGlobalTexture(HShaderParams.g_HTraceSSAO, data.SSAOTexture.IsValid() ? data.SSAOTexture : Texture2D.whiteTexture);
		    
		    var nativeGBuffer0 = data.GBufferTextures[0];
		    var nativeGBuffer1 = data.GBufferTextures[1];
		    var nativeGBuffer2 = data.GBufferTextures[2];
		    Texture renderLayerMaskTexture = data.GBufferTextures.Length >= 6 ? data.GBufferTextures[5] : null;
		    GBufferGenerationRenderGraph(cmd, data, width, height, nativeGBuffer0, nativeGBuffer1, nativeGBuffer2, renderLayerMaskTexture);

		    GenerateDepthPyramidShared(cmd, width, height, data.DepthPyramidTexture);
	    }

		private static void GBufferGenerationRenderGraph(CommandBuffer cmd, PassData data, int width, int height, Texture nativeGBuffer0, Texture nativeGBuffer1,
			Texture nativeGBuffer2, Texture renderLayerMask)
		{
			HTraceSSGIProfile profile = HTraceSSGISettings.ActiveProfile;
			using (new ProfilingScope(cmd, GBufferProfilingSampler))
			{
				// Sky probe management
				SphericalHarmonicsL2 ambientProbe = RenderSettings.ambientProbe;
				cmd.SetGlobalVector(HShaderParams.H_SHAr, new Vector4(ambientProbe[0, 3], ambientProbe[0, 1], ambientProbe[0, 2], ambientProbe[0, 0] - ambientProbe[0, 6]));
				cmd.SetGlobalVector(HShaderParams.H_SHAg, new Vector4(ambientProbe[1, 3], ambientProbe[1, 1], ambientProbe[1, 2], ambientProbe[1, 0] - ambientProbe[1, 6]));
				cmd.SetGlobalVector(HShaderParams.H_SHAb, new Vector4(ambientProbe[2, 3], ambientProbe[2, 1], ambientProbe[2, 2], ambientProbe[2, 0] - ambientProbe[2, 6]));
				cmd.SetGlobalVector(HShaderParams.H_SHBr, new Vector4(ambientProbe[0, 4], ambientProbe[0, 5], ambientProbe[0, 6] * 3, ambientProbe[0, 7]));
				cmd.SetGlobalVector(HShaderParams.H_SHBg, new Vector4(ambientProbe[1, 4], ambientProbe[1, 5], ambientProbe[1, 6] * 3, ambientProbe[1, 7]));
				cmd.SetGlobalVector(HShaderParams.H_SHBb, new Vector4(ambientProbe[2, 4], ambientProbe[2, 5], ambientProbe[2, 6] * 3, ambientProbe[2, 7]));
				cmd.SetGlobalVector(HShaderParams.H_SHC, new Vector4(ambientProbe[0, 8], ambientProbe[1, 8], ambientProbe[2, 8], 1));

				// Check if GBuffer is valid (e.g. Forward / wrong scale / is not set, etc.)
				bool requestForwardGBufferRender = false;
				if (nativeGBuffer0 == null || nativeGBuffer1 == null || nativeGBuffer2 == null)
				{ requestForwardGBufferRender = true; }
				else if ( nativeGBuffer0.width != width || nativeGBuffer0.height != height) // CameraDepthBuffer.rtHandleProperties.currentViewportSize.x
				{ requestForwardGBufferRender = true; }

				// Set Render Layer Mask to black dummy in case it's disabled as a feature and we can't render it
				if (renderLayerMask == null)
					renderLayerMask = Texture2D.blackTexture; // HRenderer.EmptyTexture;

				//requestForwardGBufferRender = false;

				// Set GBuffer to HTrace passes if valid or render it otherwise
				if (requestForwardGBufferRender == false)
				{
					cmd.SetGlobalTexture(HShaderParams.g_HTraceGBuffer0, nativeGBuffer0);
					cmd.SetGlobalTexture(HShaderParams.g_HTraceGBuffer1, nativeGBuffer1);
					// cmd.SetGlobalTexture(HShaderParams.g_HTraceGBuffer2, nativeGBuffer2);
					cmd.SetGlobalTexture(HShaderParams.g_HTraceRenderLayerMask, renderLayerMask);
				}
				else
				{

					if (HRendererURP.RenderGraphEnabled)
						GBufferMRT = new RenderTargetIdentifier[] { data.ForwardGBuffer0Texture, data.ForwardGBuffer1Texture, data.ForwardGBuffer2Texture, data.ForwardGBuffer3Texture, data.ForwardGBufferDepthTexture, data.ForwardRenderLayerMaskTexture };
					else
						GBufferMRT = new RenderTargetIdentifier[] { data.ForwardGBuffer0Texture, data.ForwardGBuffer1Texture, data.ForwardGBuffer2Texture, data.ForwardGBuffer3Texture, data.ForwardRenderLayerMaskTexture };

					// If CameraDepthBuffer.rt doesn't work for any reason - we can replace it with our ForwardGBufferDepth.rt, but GBuffer rendering performance will suffer.
					CoreUtils.SetRenderTarget(cmd, GBufferMRT, data.DepthTexture, ClearFlag.Color);

					// Cache the current keyword state set by Unity
					bool RenderLayerKeywordState = Shader.IsKeywordEnabled(HShaderParams._WRITE_RENDERING_LAYERS);
					
					// If we don't need render layers we do not touch the keyword at all
#if UNITY_2023_3_OR_NEWER
					if (profile.GeneralSettings.ExcludeReceivingMask != 0 || profile.GeneralSettings.ExcludeCastingMask != 0) 
						CoreUtils.SetKeyword(cmd, HShaderParams._WRITE_RENDERING_LAYERS, true);
#endif
					
					cmd.DrawRendererList(data.ForwardGBufferRendererListHandle);

					// If we altered the keyword, we restore it to the original state
#if UNITY_2023_3_OR_NEWER
					if (profile.GeneralSettings.ExcludeReceivingMask != 0 || profile.GeneralSettings.ExcludeCastingMask != 0) 
						CoreUtils.SetKeyword(cmd, HShaderParams._WRITE_RENDERING_LAYERS, RenderLayerKeywordState);
#endif
					cmd.SetGlobalTexture(HShaderParams.g_HTraceGBuffer0, data.ForwardGBuffer0Texture);
					cmd.SetGlobalTexture(HShaderParams.g_HTraceGBuffer1, data.ForwardGBuffer1Texture);
					// cmd.SetGlobalTexture(HShaderParams.g_HTraceGBuffer2, data.ForwardGBuffer2Texture);
					cmd.SetGlobalTexture(HShaderParams.g_HTraceRenderLayerMask, data.ForwardRenderLayerMaskTexture);
				}
			}
		}
#endif
		#endregion --------------------------- Render Graph ---------------------------
		
		#region --------------------------- Shared ---------------------------

		private static void GenerateDepthPyramidShared(CommandBuffer cmd, int width, int height, RTHandle depthPyramidTexture)
		{
			using (new ProfilingScope(cmd, DepthPyramidGenerationProfilingSampler))
			{
				int generate_depth_pyramid_kernel = HDepthPyramid.FindKernel("GenerateDepthPyramid");
				cmd.SetComputeTextureParam(HDepthPyramid, generate_depth_pyramid_kernel, HShaderParams._DepthPyramid_OutputMIP0, depthPyramidTexture, 0);
				cmd.SetComputeTextureParam(HDepthPyramid, generate_depth_pyramid_kernel, HShaderParams._DepthPyramid_OutputMIP1, depthPyramidTexture, 1);
				cmd.SetComputeTextureParam(HDepthPyramid, generate_depth_pyramid_kernel, HShaderParams._DepthPyramid_OutputMIP2, depthPyramidTexture, 2);
				cmd.SetComputeTextureParam(HDepthPyramid, generate_depth_pyramid_kernel, HShaderParams._DepthPyramid_OutputMIP3, depthPyramidTexture, 3);
				cmd.SetComputeTextureParam(HDepthPyramid, generate_depth_pyramid_kernel, HShaderParams._DepthPyramid_OutputMIP4, depthPyramidTexture, 4);
				cmd.DispatchCompute(HDepthPyramid, generate_depth_pyramid_kernel, Mathf.CeilToInt(width / 16.0f), Mathf.CeilToInt(height / 16.0f), HRenderer.TextureXrSlices);

				cmd.SetGlobalTexture(HShaderParams.g_HTraceDepthPyramidSSGI, depthPyramidTexture);
			}
		}


		protected internal void Dispose()
		{
			Dummy?.HRelease();
			ForwardGBuffer0?.HRelease();
			ForwardGBuffer1?.HRelease();
			ForwardGBuffer2?.HRelease();
			ForwardGBuffer3?.HRelease();
			ForwardRenderLayerMask?.HRelease();
			ForwardGBufferDepth?.HRelease();
			DepthPyramidRT?.HRelease();
		}

		#endregion --------------------------- Shared ---------------------------
	}
}
