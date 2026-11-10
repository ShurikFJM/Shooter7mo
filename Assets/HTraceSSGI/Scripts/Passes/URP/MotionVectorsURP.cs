//pipelinedefine
#define H_URP

using System;
using HTraceSSGI.Scripts.Data.Private;
using HTraceSSGI.Scripts.Extensions;
using HTraceSSGI.Scripts.Globals;
using HTraceSSGI.Scripts.Wrappers;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
#if UNITY_2023_3_OR_NEWER
using UnityEngine.Rendering.RenderGraphModule;
#else
using UnityEngine.Experimental.Rendering.RenderGraphModule;
#endif


namespace HTraceSSGI.Scripts.Passes.URP
{
	internal class MotionVectorsURP : ScriptableRenderPass
	{
		// Texture Names
		const string _ObjectMotionVectorsColorURP    = "_ObjectMotionVectorsColorURP";
		const string _ObjectMotionVectorsDepthURP    = "_ObjectMotionVectorsDepthURP";
		const string _CustomCameraMotionVectorsURP_0 = "_CustomCameraMotionVectorsURP_0";
		const string _CustomCameraMotionVectorsURP_1 = "_CustomCameraMotionVectorsURP_1";
		const string _MotionVectorsTagID             = "MotionVectors";
		
		// Shader Properties
		private static readonly int _ObjectMotionVectorsColor = Shader.PropertyToID("_ObjectMotionVectors");
		private static readonly int _ObjectMotionVectorsDepth = Shader.PropertyToID("_ObjectMotionVectorsDepth");
		private static readonly int _BiasOffset               = Shader.PropertyToID("_BiasOffset");

		// Textures
		internal static RTWrapper[] CustomCameraMotionVectorsURP = new RTWrapper[2] {new RTWrapper(), new RTWrapper()};
		internal static RTWrapper   ObjectMotionVectorsColorURP = new RTWrapper();
		internal static RTWrapper   ObjectMotionVectorsDepthURP = new RTWrapper();
		
		ShaderTagId[] tags
#if UNITY_2023_1_OR_NEWER
			= {new ShaderTagId("MotionVectors")};
#else
			= {new ShaderTagId("UniversalForward"), new ShaderTagId("UniversalForwardOnly"), new ShaderTagId("LightweightForward"), new ShaderTagId("SRPDefaultUnlit"), new ShaderTagId("Meta")};
#endif
		
		// Materials
		private static Material MotionVectorsMaterial_URP;
		
		private static ProfilingSampler ObjectMVProfilingSampler = new ProfilingSampler(HNames.HTRACE_OBJECTS_MV_PASS_NAME);
		private static ProfilingSampler MVProfilingSampler = new ProfilingSampler(HNames.HTRACE_CAMERA_MV_PASS_NAME);

		
		#region --------------------------- Non Render Graph ---------------------------
		
#if !UNITY_6000_4_OR_NEWER
		private        ScriptableRenderer _renderer;
		private static int _historyCameraIndex;

		protected internal void Initialize(ScriptableRenderer renderer)
		{
			_renderer    = renderer;
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
			if (MotionVectorsMaterial_URP == null) MotionVectorsMaterial_URP = new Material(Shader.Find($"Hidden/{HNames.ASSET_NAME}/MotionVectorsURP"));

			int width  = (int)(camera.scaledPixelWidth * renderScale);
			int height = (int)(camera.scaledPixelHeight * renderScale);

			if (desc.width != width || desc.height != height)
				desc = new RenderTextureDescriptor(width, height);

			desc.depthBufferBits    = 0; // Color and depth cannot be combined in RTHandles
			desc.stencilFormat      = GraphicsFormat.None;
			desc.depthStencilFormat = GraphicsFormat.None;
			desc.msaaSamples        = 1;
			desc.bindMS             = false;
			desc.enableRandomWrite  = true;

			RenderTextureDescriptor depthDesc = desc;
			depthDesc.depthBufferBits   = 32;
			depthDesc.enableRandomWrite = false;
			depthDesc.colorFormat       = RenderTextureFormat.Depth;

			CustomCameraMotionVectorsURP[0].ReAllocateIfNeeded(_CustomCameraMotionVectorsURP_0, ref desc, graphicsFormat: GraphicsFormat.R16G16_SFloat);
			CustomCameraMotionVectorsURP[1].ReAllocateIfNeeded(_CustomCameraMotionVectorsURP_1, ref desc, graphicsFormat: GraphicsFormat.R8_SNorm);
			ObjectMotionVectorsColorURP.ReAllocateIfNeeded(_ObjectMotionVectorsColorURP, ref desc, graphicsFormat: GraphicsFormat.R16G16_SFloat);
			ObjectMotionVectorsDepthURP.ReAllocateIfNeeded(_ObjectMotionVectorsDepthURP, ref depthDesc, enableRandomWrite: false);
		}
		
#if UNITY_2023_3_OR_NEWER
		[System.Obsolete]
#endif
		public override void Configure(CommandBuffer cmd, RenderTextureDescriptor cameraTextureDescriptor)
		{
			ConfigureInput(ScriptableRenderPassInput.Motion | ScriptableRenderPassInput.Depth);
		}

#if UNITY_2023_3_OR_NEWER
		[System.Obsolete]
#endif
		public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
		{
			var cmd = CommandBufferPool.Get(HNames.HTRACE_MV_PASS_NAME);
			
			Camera camera = renderingData.cameraData.camera;
			

			RenderMotionVectorsNonRenderGraph(cmd, camera, ref renderingData, ref context);
			
			context.ExecuteCommandBuffer(cmd);
			cmd.Clear();
			CommandBufferPool.Release(cmd);
		}

		private void RenderMotionVectorsNonRenderGraph(CommandBuffer cmd, Camera camera, ref RenderingData renderingData, ref ScriptableRenderContext context)
		{
			void RenderObjectsMotionVectors(ref RenderingData renderingData, ref ScriptableRenderContext context)
			{
#if UNITY_2023_3_OR_NEWER
				if (camera.cameraType == CameraType.SceneView)
					return;
#endif

				CoreUtils.SetRenderTarget(cmd, ObjectMotionVectorsColorURP.rt, ClearFlag.All, Color.clear);
				CoreUtils.SetRenderTarget(cmd, ObjectMotionVectorsDepthURP.rt, ClearFlag.All, Color.clear);
#if UNITY_2023_1_OR_NEWER
				// We'll write not only to our own Color, but also to our own Depth target to use it later (in Camera MV) to compose per-object mv
				CoreUtils.SetRenderTarget(cmd, ObjectMotionVectorsColorURP.rt, ObjectMotionVectorsDepthURP.rt);
#else
			// Prior to 2023 camera motion vectors are rendered directly on objects, so we write to both motion mask and motion vectors via MRT
			RenderTargetIdentifier[] motionVectorsMRT = { CustomCameraMotionVectorsURP[0].rt, CustomCameraMotionVectorsURP[1].rt,};
			CoreUtils.SetRenderTarget(cmd, motionVectorsMRT, ObjectMotionVectorsDepthURP.rt);

#endif // UNITY_2023_1_OR_NEWER

				CullingResults cullingResults = renderingData.cullResults;
				
				var renderList = new UnityEngine.Rendering.RendererUtils.RendererListDesc(tags, cullingResults, camera)
				{
					rendererConfiguration = PerObjectData.MotionVectors,
					renderQueueRange      = RenderQueueRange.opaque,
					sortingCriteria       = SortingCriteria.CommonOpaque,
					layerMask             = camera.cullingMask,
					overrideMaterial
#if UNITY_2023_1_OR_NEWER
						= null,
#else
					= MotionVectorsMaterial_URP,
					overrideMaterialPassIndex = 1,
					// If somethingis wrong with our custom shader we can always use the standard one (and ShaderPass = 0) instead
					// Material ObjectMotionVectorsMaterial = new Material(Shader.Find("Hidden/Universal Render Pipeline/ObjectMotionVectors"));
					// overrideMaterialPassIndex = 0,
#endif //UNITY_2023_1_OR_NEWER
					
				};

				CoreUtils.DrawRendererList(context, cmd, context.CreateRendererList(renderList));

#if !UNITY_2023_1_OR_NEWER
			// Prior to 2023 camera motion vectors are rendered directly on objects, so we will finish mv calculation here and won't execute camera mv
			cmd.SetGlobalTexture(HShaderParams.g_HTraceMotionVectors, CustomCameraMotionVectorsURP[0].rt);
			cmd.SetGlobalTexture(HShaderParams.g_HTraceMotionMask, CustomCameraMotionVectorsURP[1].rt);
#endif // UNITY_2023_1_OR_NEWER
			}

			
			void RenderCameraMotionVectors()
			{
#if UNITY_2023_1_OR_NEWER

				float DepthBiasOffset = 0;
#if UNITY_2023_1_OR_NEWER
				DepthBiasOffset = 0.00099f;
#endif // UNITY_2023_1_OR_NEWER
#if UNITY_6000_0_OR_NEWER
				DepthBiasOffset = 0;
#endif // UNITY_6000_0_OR_NEWER

				// Target target[0] is set as a Depth Buffer, just because this method requires Depth, but we don't care for it in the fullscreen pass
				RenderTargetIdentifier[] motionVectorsMRT = { CustomCameraMotionVectorsURP[0].rt, CustomCameraMotionVectorsURP[1].rt};
				CoreUtils.SetRenderTarget(cmd, motionVectorsMRT, motionVectorsMRT[0]);

				MotionVectorsMaterial_URP.SetTexture(_ObjectMotionVectorsColor, ObjectMotionVectorsColorURP.rt);
				MotionVectorsMaterial_URP.SetTexture(_ObjectMotionVectorsDepth, ObjectMotionVectorsDepthURP.rt);
				MotionVectorsMaterial_URP.SetFloat(_BiasOffset, DepthBiasOffset);

				cmd.DrawProcedural(Matrix4x4.identity, MotionVectorsMaterial_URP, 0, MeshTopology.Triangles, 3, 1);

				// This restores color camera color target (.SetRenderTarget can be used for Forward + any Depth Priming, but doesn't work in Deferred)
#pragma warning disable CS0618
				ConfigureTarget(_renderer.cameraColorTargetHandle);
#pragma warning restore CS0618

				cmd.SetGlobalTexture(HShaderParams.g_HTraceMotionVectors, CustomCameraMotionVectorsURP[0].rt);
				cmd.SetGlobalTexture(HShaderParams.g_HTraceMotionMask, CustomCameraMotionVectorsURP[1].rt);
#endif // UNITY_2023_1_OR_NEWER
			}

			RenderObjectsMotionVectors(ref renderingData, ref context);
			RenderCameraMotionVectors();
		}
#endif // !UNITY_6000_4_OR_NEWER

		#endregion --------------------------- Non Render Graph ---------------------------

		#region --------------------------- Render Graph ---------------------------
		
#if UNITY_2023_3_OR_NEWER
		private class ObjectMVPassData
		{
			public RendererListHandle RendererListHandle;
			public TextureHandle MotionVectorsTexture;
		}

		private class CameraMVPassData
		{
			public TextureHandle ObjectMotionVectorsColor;
			public TextureHandle ObjectMotionVectorsDepth;
		}
		
		public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
		{
			UniversalCameraData universalCameraData = frameData.Get<UniversalCameraData>();
			
			ConfigureInput(ScriptableRenderPassInput.Motion | ScriptableRenderPassInput.Depth);
			
			using (IRasterRenderGraphBuilder builder = renderGraph.AddRasterRenderPass<ObjectMVPassData>(HNames.HTRACE_OBJECTS_MV_PASS_NAME, out var passData, ObjectMVProfilingSampler))
			{
				UniversalResourceData  resourceData           = frameData.Get<UniversalResourceData>();
				UniversalRenderingData universalRenderingData = frameData.Get<UniversalRenderingData>();
			
				builder.AllowGlobalStateModification(true);
				builder.AllowPassCulling(false);
				
				TextureHandle depthTexture = resourceData.activeDepthTexture;
				TextureHandle motionVectorsTexture = resourceData.motionVectorColor;
				passData.MotionVectorsTexture = motionVectorsTexture;
				if (motionVectorsTexture.IsValid())
					builder.UseTexture(motionVectorsTexture, AccessFlags.Read);

				AddRendererList(renderGraph, universalCameraData, universalRenderingData, passData, builder);

				// This was previously colorTexture.GetDescriptor(renderGraph);
				TextureDesc descDepth = depthTexture.GetDescriptor(renderGraph);
				descDepth.colorFormat = GraphicsFormat.R16G16_SFloat;
				descDepth.name  = _ObjectMotionVectorsColorURP;
				TextureHandle objectMotionVectorsColorTexHandle = renderGraph.CreateTexture(descDepth);

				builder.SetRenderAttachment(objectMotionVectorsColorTexHandle, 0);
				builder.SetRenderAttachmentDepth(depthTexture, AccessFlags.ReadWrite);

				builder.SetGlobalTextureAfterPass(objectMotionVectorsColorTexHandle, HShaderParams.g_HTraceMotionMask);

				builder.SetRenderFunc((ObjectMVPassData data, RasterGraphContext context) =>
				{
					RasterCommandBuffer cmd = context.cmd;

					if (data.MotionVectorsTexture.IsValid())
						cmd.SetGlobalTexture(HShaderParams.g_HTraceMotionVectors, data.MotionVectorsTexture);

					cmd.ClearRenderTarget(false, true, Color.black);
					cmd.DrawRendererList(data.RendererListHandle);
				});
			}
			
			// Render Graph + Game View - no need to render camera mv, as they are already available to us in this combination
			if (universalCameraData.cameraType == CameraType.Game)
				return;
			
			using (IRasterRenderGraphBuilder builder = renderGraph.AddRasterRenderPass<CameraMVPassData>(HNames.HTRACE_CAMERA_MV_PASS_NAME, out var passData, MVProfilingSampler))
			{
				UniversalResourceData  resourceData  = frameData.Get<UniversalResourceData>();

				builder.AllowGlobalStateModification(true);
				builder.AllowPassCulling(false);

				TextureHandle colorTexture = resourceData.activeColorTexture;
				
				if (MotionVectorsMaterial_URP == null) MotionVectorsMaterial_URP = new Material(Shader.Find($"Hidden/{HNames.ASSET_NAME}/MotionVectorsURP"));
				
				TextureDesc desc = colorTexture.GetDescriptor(renderGraph);
				desc.colorFormat = GraphicsFormat.R16G16_SFloat;
				desc.name  = _CustomCameraMotionVectorsURP_0;
				TextureHandle cameraMotionVectorsColorTexHandle = renderGraph.CreateTexture(desc);
			
				builder.SetRenderAttachment(cameraMotionVectorsColorTexHandle, 0);
				builder.SetGlobalTextureAfterPass(cameraMotionVectorsColorTexHandle, HShaderParams.g_HTraceMotionVectors);
				
				builder.SetRenderFunc((CameraMVPassData data, RasterGraphContext context) =>
				{
					RasterCommandBuffer cmd = context.cmd;
					
					MotionVectorsMaterial_URP.SetTexture(_ObjectMotionVectorsColor, context.defaultResources.blackTexture);
					MotionVectorsMaterial_URP.SetTexture(_ObjectMotionVectorsDepth, context.defaultResources.whiteTexture);
					MotionVectorsMaterial_URP.SetFloat(_BiasOffset, 0);

					cmd.DrawProcedural(Matrix4x4.identity, MotionVectorsMaterial_URP, 0, MeshTopology.Triangles, 3, 1);
				});
			}
		}
		
		private static void AddRendererList(RenderGraph renderGraph, UniversalCameraData universalCameraData, UniversalRenderingData universalRenderingData, ObjectMVPassData objectMvPassData, IRasterRenderGraphBuilder builder)
		{
			RenderStateBlock ForwardGBufferRenderStateBlock = new RenderStateBlock(RenderStateMask.Nothing);
			ForwardGBufferRenderStateBlock.depthState = new DepthState(false, CompareFunction.LessEqual); // Probably CompareFunction.Less ?
			ForwardGBufferRenderStateBlock.mask |= RenderStateMask.Depth;
			
			ShaderTagId motionVectorsShaderTag = new ShaderTagId(_MotionVectorsTagID);

			var renderList = new UnityEngine.Rendering.RendererUtils.RendererListDesc(motionVectorsShaderTag, universalRenderingData.cullResults, universalCameraData.camera)
			{
				rendererConfiguration = PerObjectData.MotionVectors,
				renderQueueRange      = RenderQueueRange.opaque,
				sortingCriteria       = SortingCriteria.CommonOpaque,
				stateBlock            = ForwardGBufferRenderStateBlock,
			};
			
			objectMvPassData.RendererListHandle = renderGraph.CreateRendererList(renderList);
			
			builder.UseRendererList(objectMvPassData.RendererListHandle);
		}
#endif
		
		#endregion ---------------------------  Render Graph ---------------------------

		#region --------------------------- Shared ---------------------------
		
		protected internal void Dispose()
		{
			CustomCameraMotionVectorsURP[0]?.HRelease();
			CustomCameraMotionVectorsURP[1]?.HRelease();
			ObjectMotionVectorsColorURP?.HRelease();
			ObjectMotionVectorsDepthURP?.HRelease();
		}

		#endregion --------------------------- Share ---------------------------
	}
}
