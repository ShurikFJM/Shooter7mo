using UnityEngine;
using UnityEngine.Rendering;

namespace HTraceSSGI.Scripts.Globals
{
	public static class HShaderParams
	{
		//  ---------------------------------------------- Globals, "g_" prefix ----------------------------------------------
		public static readonly int g_HTraceGBuffer0              = Shader.PropertyToID("g_HTraceGBuffer0");
		public static readonly int g_HTraceGBuffer1              = Shader.PropertyToID("g_HTraceGBuffer1");
		public static readonly int g_HTraceGBuffer2              = Shader.PropertyToID("g_HTraceGBuffer2");
		public static readonly int g_HTraceGBuffer3              = Shader.PropertyToID("g_HTraceGBuffer3");
		public static readonly int g_HTraceRenderLayerMask       = Shader.PropertyToID("g_HTraceRenderLayerMask");
		public static readonly int g_HTraceColor                 = Shader.PropertyToID("g_HTraceColor");
		public static readonly int g_HTraceDepth                 = Shader.PropertyToID("g_HTraceDepth");
		public static readonly int g_HTraceNormals               = Shader.PropertyToID("g_HTraceNormals");
		public static readonly int g_HTraceSSAO                  = Shader.PropertyToID("g_HTraceSSAO");
		public static readonly int g_HTraceDepthPyramidSSGI      = Shader.PropertyToID("g_HTraceDepthPyramidSSGI");
		public static readonly int g_HTraceStencilBuffer         = Shader.PropertyToID("g_HTraceStencilBuffer");
		public static readonly int g_HTraceMotionVectors         = Shader.PropertyToID("g_HTraceMotionVectors");
		public static readonly int g_HTraceMotionMask            = Shader.PropertyToID("g_HTraceMotionMask");
		public static readonly int g_HTraceBufferAO              = Shader.PropertyToID("_HTraceBufferAO");
		public static readonly int g_ScreenSpaceOcclusionTexture = Shader.PropertyToID("_ScreenSpaceOcclusionTexture");
		public static readonly int g_CameraDepthTexture          = Shader.PropertyToID("_CameraDepthTexture");
		public static readonly int g_CameraNormalsTexture        = Shader.PropertyToID("_CameraNormalsTexture");


		// ---------------------------------------------- GBuffer ----------------------------------------------
		public static readonly int _GBuffer0                     = Shader.PropertyToID("_GBuffer0");
		public static readonly int _GBuffer1                     = Shader.PropertyToID("_GBuffer1");
		public static readonly int _GBuffer2                     = Shader.PropertyToID("_GBuffer2");
		public static readonly int _GBuffer3                     = Shader.PropertyToID("_GBuffer3");
		public static readonly int _CameraRenderingLayersTexture = Shader.PropertyToID("_CameraRenderingLayersTexture");
		public static readonly int _GBufferTexture0              = Shader.PropertyToID("_GBufferTexture0");
		public static readonly int _CameraGBufferTexture0        = Shader.PropertyToID("_CameraGBufferTexture0");

		public static readonly int _DepthPyramid_OutputMIP0 = Shader.PropertyToID("_DepthPyramid_OutputMIP0");
		public static readonly int _DepthPyramid_OutputMIP1 = Shader.PropertyToID("_DepthPyramid_OutputMIP1");
		public static readonly int _DepthPyramid_OutputMIP2 = Shader.PropertyToID("_DepthPyramid_OutputMIP2");
		public static readonly int _DepthPyramid_OutputMIP3 = Shader.PropertyToID("_DepthPyramid_OutputMIP3");
		public static readonly int _DepthPyramid_OutputMIP4 = Shader.PropertyToID("_DepthPyramid_OutputMIP4");

		public static readonly int H_SHAr = Shader.PropertyToID("H_SHAr");
		public static readonly int H_SHAg = Shader.PropertyToID("H_SHAg");
		public static readonly int H_SHAb = Shader.PropertyToID("H_SHAb");
		public static readonly int H_SHBr = Shader.PropertyToID("H_SHBr");
		public static readonly int H_SHBg = Shader.PropertyToID("H_SHBg");
		public static readonly int H_SHBb = Shader.PropertyToID("H_SHBb");
		public static readonly int H_SHC  = Shader.PropertyToID("H_SHC");

		public static readonly string _GBUFFER_NORMALS_OCT    = "_GBUFFER_NORMALS_OCT";
		public static readonly string _WRITE_RENDERING_LAYERS = "_WRITE_RENDERING_LAYERS";

		public static readonly ShaderTagId UniversalGBufferTag = new ShaderTagId("UniversalGBuffer");
		
		// ---------------------------------------------- Matrix ----------------------------------------------
		public static readonly int H_MATRIX_VP        = Shader.PropertyToID("_H_MATRIX_VP");
		public static readonly int H_MATRIX_I_VP      = Shader.PropertyToID("_H_MATRIX_I_VP");
		public static readonly int H_MATRIX_PREV_VP   = Shader.PropertyToID("_H_MATRIX_PREV_VP");
		public static readonly int H_MATRIX_PREV_I_VP = Shader.PropertyToID("_H_MATRIX_PREV_I_VP");

		// ---------------------------------------------- Additional ----------------------------------------------
		public static int HRenderScale         = Shader.PropertyToID("_HRenderScale");
		public static int HRenderScalePrevious = Shader.PropertyToID("_HRenderScalePrevious");
		public static int FrameCount           = Shader.PropertyToID("_FrameCount");
		public static int ScreenSize           = Shader.PropertyToID("_ScreenSize");

		// ---------------------------------------------- Shared Params Other ----------------------------------------------
		public static readonly int SliceXR             = Shader.PropertyToID("_SliceXR");
		public static readonly int IndexXR             = Shader.PropertyToID("_IndexXR");

		public static readonly int RTAS = Shader.PropertyToID("_RTAS");
	}
}
