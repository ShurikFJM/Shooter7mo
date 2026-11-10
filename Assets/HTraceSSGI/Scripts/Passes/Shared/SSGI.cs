//pipelinedefine
#define H_URP

using System;
using HTraceSSGI.Scripts.Data.Private;
using HTraceSSGI.Scripts.Data.Public;
using HTraceSSGI.Scripts.Extensions;
using HTraceSSGI.Scripts.Extensions.CameraHistorySystem;
using HTraceSSGI.Scripts.Globals;
using HTraceSSGI.Scripts.Wrappers;
using UnityEngine;
using UnityEngine.Rendering;
using HTraceSSGI.Scripts.Infrastructure.URP;

namespace HTraceSSGI.Scripts.Passes.Shared
{
	internal class SSGI
	{
		// Kernels
		internal enum HTemporalReprojectionKernels
		{
			TemporalReprojection = 0,
			ColorReprojection = 1,
			CopyHistory = 2,
			LuminanceMomentsGeneration = 3,
			LuminanceMomentsClear = 4,
		}

		internal enum HCheckerboardingKernels
		{
			CheckerboardClassification = 0,
			IndirectArguments = 1,
		}

		internal enum HRenderSSGIKernels
		{
			TraceSSGI = 0,
			MaskExclude = 1,
		}

		internal enum HReSTIRKernels
		{
			TemporalResampling = 0,
			FireflySuppression = 1,
			SpatialResampling = 2,
			SpatialValidation = 3,
		}

		internal enum HDenoiserKernels
		{
			TemporalAccumulation = 0,
			TemporalStabilization = 1,
			PointDistributionFill = 2,
			SpatialFilter = 3,
			SpatialFilter1 = 4,
			SpatialFilter2 = 5,
		}

		internal enum HInterpolationKernels
		{
			Interpolation = 0,
		}
		
		// Textures, except History
		public static RTWrapper ColorCopy_URP                 = new RTWrapper(); // URP only
		public static RTWrapper DebugOutput                   = new RTWrapper();
		public static RTWrapper ReservoirLuminance            = new RTWrapper();
		public static RTWrapper Reservoir                     = new RTWrapper();
		public static RTWrapper ReservoirReprojected          = new RTWrapper();
		public static RTWrapper ReservoirSpatial              = new RTWrapper();
		public static RTWrapper SamplecountReprojected        = new RTWrapper();
		public static RTWrapper TemporalInvalidityFilteredA   = new RTWrapper();
		public static RTWrapper TemporalInvalidityFilteredB   = new RTWrapper();
		public static RTWrapper TemporalInvalidityReprojected = new RTWrapper();
		public static RTWrapper SpatialOcclusionReprojected   = new RTWrapper();
		public static RTWrapper AmbientOcclusion              = new RTWrapper();
		public static RTWrapper AmbientOcclusionGuidance      = new RTWrapper();
		public static RTWrapper AmbientOcclusionInvalidity    = new RTWrapper();
		public static RTWrapper AmbientOcclusionReprojected   = new RTWrapper();
		public static RTWrapper RadianceReprojected           = new RTWrapper();
		public static RTWrapper RadianceFiltered              = new RTWrapper();
		public static RTWrapper RadianceInterpolated          = new RTWrapper();
		public static RTWrapper RadianceStabilized            = new RTWrapper();
		public static RTWrapper RadianceStabilizedReprojected = new RTWrapper();
		public static RTWrapper RadianceNormalDepth           = new RTWrapper();
		public static RTWrapper ColorReprojected              = new RTWrapper();
		public static RTWrapper DummyBlackTexture             = new RTWrapper();
		
		// History Textures
		internal struct HistoryCameraDataSSGI : ICameraHistoryData, IDisposable
		{
			private int hash;
			public RTWrapper ColorPreviousFrame;
			public RTWrapper ReservoirTemporal;
			public RTWrapper SampleCount;
			public RTWrapper NormalDepth;
			public RTWrapper NormalDepthFullRes;
			public RTWrapper Radiance;
			public RTWrapper RadianceAccumulated;
			public RTWrapper SpatialOcclusionAccumulated;
			public RTWrapper TemporalInvalidityAccumulated;
			public RTWrapper AmbientOcclusionAccumulated;

			public HistoryCameraDataSSGI(int hash = 0)
			{
				this.hash                     = hash;
				ColorPreviousFrame            = new RTWrapper();
				ReservoirTemporal             = new RTWrapper();
				SampleCount                   = new RTWrapper();
				NormalDepth                   = new RTWrapper();
				NormalDepthFullRes            = new RTWrapper();
				Radiance                      = new RTWrapper();
				RadianceAccumulated           = new RTWrapper();
				SpatialOcclusionAccumulated   = new RTWrapper();
				TemporalInvalidityAccumulated = new RTWrapper();
				AmbientOcclusionAccumulated   = new RTWrapper();
			}

			public int GetHash() => hash;
			public void SetHash(int hashIn) => this.hash = hashIn;

			public void Dispose()
			{
				ColorPreviousFrame?.HRelease();
				ReservoirTemporal?.HRelease();
				SampleCount?.HRelease();
				NormalDepth?.HRelease();
				NormalDepthFullRes?.HRelease();
				Radiance?.HRelease();
				RadianceAccumulated?.HRelease();
				SpatialOcclusionAccumulated?.HRelease();
				TemporalInvalidityAccumulated?.HRelease();
				AmbientOcclusionAccumulated?.HRelease();
			}
		}

		internal static readonly CameraHistorySystem<HistoryCameraDataSSGI> CameraHistorySystem = new CameraHistorySystem<HistoryCameraDataSSGI>();

		// Shader Properties
		public static readonly  int _RayCounter                    = Shader.PropertyToID("_RayCounter");
		public static readonly  int _RayCounter_Output             = Shader.PropertyToID("_RayCounter_Output");
		public static readonly  int _IndirectCoords_Output         = Shader.PropertyToID("_IndirectCoords_Output");
		public static readonly  int _IndirectArguments_Output      = Shader.PropertyToID("_IndirectArguments_Output");
		public static readonly  int _TracingCoords                 = Shader.PropertyToID("_TracingCoords");
		public static readonly  int _RayTracedCounter              = Shader.PropertyToID("_RayTracedCounter");
		public static readonly  int _DepthToViewParams             = Shader.PropertyToID("_DepthToViewParams");
		public static readonly  int _HScaleFactorSSGI              = Shader.PropertyToID("_HScaleFactorSSGI");
		public static readonly  int _HPreviousScaleFactorSSGI      = Shader.PropertyToID("_HPreviousScaleFactorSSGI");
		public static readonly  int _ReservoirDiffuseWeight        = Shader.PropertyToID("_ReservoirDiffuseWeight");
		public static readonly  int _ExcludeCastingLayerMaskSSGI   = Shader.PropertyToID("_ExcludeCastingLayerMaskSSGI");
		public static readonly  int _ExcludeReceivingLayerMaskSSGI = Shader.PropertyToID("_ExcludeReceivingLayerMaskSSGI");
		public static readonly  int _APVParams                     = Shader.PropertyToID("_APVParams");
		public static readonly  int _BrightnessClamp               = Shader.PropertyToID("_BrightnessClamp");
		public static readonly  int _MaxDeviation                  = Shader.PropertyToID("_MaxDeviation");
		public static readonly  int _RayCount                      = Shader.PropertyToID("_RayCount");
		public static readonly  int _StepCount                     = Shader.PropertyToID("_StepCount");
		public static readonly  int _RayLength                     = Shader.PropertyToID("_RayLength");
		public static readonly  int _SkyFallbackIntensity          = Shader.PropertyToID("_SkyFallbackIntensity");
		public static readonly  int _BackfaceLighting              = Shader.PropertyToID("_BackfaceLighting");
		public static readonly  int _Falloff                       = Shader.PropertyToID("_Falloff");
		public static readonly  int _ThicknessParams               = Shader.PropertyToID("_ThicknessParams");
		public static readonly  int _DenoiseFallback               = Shader.PropertyToID("_DenoiseFallback");
		public static readonly  int _FilterAdaptivity              = Shader.PropertyToID("_FilterAdaptivity");
		public static readonly  int _FilterRadius                  = Shader.PropertyToID("_FilterRadius");
		public static readonly  int _ColorCopy                     = Shader.PropertyToID("_ColorCopy");
		public static readonly  int _Color_History                 = Shader.PropertyToID("_Color_History");
		public static readonly  int _LuminanceMoments              = Shader.PropertyToID("_LuminanceMoments");
		public static readonly  int _Radiance_History              = Shader.PropertyToID("_Radiance_History");
		public static readonly  int _RadianceNormalDepth           = Shader.PropertyToID("_RadianceNormalDepth");
		public static readonly  int _Radiance_Output               = Shader.PropertyToID("_Radiance_Output");
		public static readonly  int _NormalDepth_History           = Shader.PropertyToID("_NormalDepth_History");
		public static readonly  int _ReprojectedColor_Output       = Shader.PropertyToID("_ReprojectedColor_Output");
		public static readonly  int _Samplecount_History           = Shader.PropertyToID("_Samplecount_History");
		public static readonly  int _Samplecount_Output            = Shader.PropertyToID("_Samplecount_Output");
		public static readonly  int _SpatialOcclusion_History      = Shader.PropertyToID("_SpatialOcclusion_History");
		public static readonly  int _SpatialOcclusion_Output       = Shader.PropertyToID("_SpatialOcclusion_Output");
		public static readonly  int _PointDistribution_Output      = Shader.PropertyToID("_PointDistribution_Output");
		public static readonly  int _Reservoir                     = Shader.PropertyToID("_Reservoir");
		public static readonly  int _Reservoir_Output              = Shader.PropertyToID("_Reservoir_Output");
		public static readonly  int _TemporalInvalidity_History    = Shader.PropertyToID("_TemporalInvalidity_History");
		public static readonly  int _TemporalInvalidity_Output     = Shader.PropertyToID("_TemporalInvalidity_Output");
		public static readonly  int _AmbientOcclusion_History      = Shader.PropertyToID("_AmbientOcclusion_History");
		public static readonly  int _AmbientOcclusion_Output       = Shader.PropertyToID("_AmbientOcclusion_Output");
		public static readonly  int _SampleCount                   = Shader.PropertyToID("_SampleCount");
		public static readonly  int _Color                         = Shader.PropertyToID("_Color");
		public static readonly  int _AmbientOcclusion              = Shader.PropertyToID("_AmbientOcclusion");
		public static readonly  int _AmbientOcclusionInvalidity    = Shader.PropertyToID("_AmbientOcclusionInvalidity");
		public static readonly  int _AmbientOcclusionReprojected   = Shader.PropertyToID("_AmbientOcclusionReprojected");
		public static readonly  int _SpatialOcclusion              = Shader.PropertyToID("_SpatialOcclusion");
		public static readonly  int _ReservoirReprojected          = Shader.PropertyToID("_ReservoirReprojected");
		public static readonly  int _ReservoirSpatial_Output       = Shader.PropertyToID("_ReservoirSpatial_Output");
		public static readonly  int _ReservoirLuminance            = Shader.PropertyToID("_ReservoirLuminance");
		public static readonly  int _ReservoirTemporal_Output      = Shader.PropertyToID("_ReservoirTemporal_Output");
		public static readonly  int _TemporalInvalidity            = Shader.PropertyToID("_TemporalInvalidity");
		public static readonly  int _NormalDepth_HistoryOutput     = Shader.PropertyToID("_NormalDepth_HistoryOutput");
		public static readonly  int _ReservoirLuminance_Output     = Shader.PropertyToID("_ReservoirLuminance_Output");
		public static readonly  int _SpatialGuidance_Output        = Shader.PropertyToID("_SpatialGuidance_Output");
		public static readonly  int _PointDistribution             = Shader.PropertyToID("_PointDistribution");
		public static readonly  int _NormalDepth                   = Shader.PropertyToID("_NormalDepth");
		public static readonly  int _RadianceNormalDepth_Output    = Shader.PropertyToID("_RadianceNormalDepth_Output");
		public static readonly  int _SpatialGuidance               = Shader.PropertyToID("_SpatialGuidance");
		public static readonly  int _SamplecountReprojected        = Shader.PropertyToID("_SamplecountReprojected");
		public static readonly  int _Radiance                      = Shader.PropertyToID("_Radiance");
		public static readonly  int _RadianceReprojected           = Shader.PropertyToID("_RadianceReprojected");
		public static readonly  int _Radiance_TemporalOutput       = Shader.PropertyToID("_Radiance_TemporalOutput");
		public static readonly  int _Radiance_SpatialOutput        = Shader.PropertyToID("_Radiance_SpatialOutput");
		public static readonly  int _SampleCountSSGI               = Shader.PropertyToID("_SampleCountSSGI");
		public static readonly  int _HTraceBufferGI                = Shader.PropertyToID("_HTraceBufferGI");
		public static readonly  int _IndirectLightingIntensity     = Shader.PropertyToID("_IndirectLightingIntensity");
		public static readonly  int _MetallicIndirectFallback      = Shader.PropertyToID("_MetallicIndirectFallback");
		private static readonly int _DebugSwitch                   = Shader.PropertyToID("_DebugSwitch");
		private static readonly int _BuffersSwitch                 = Shader.PropertyToID("_BuffersSwitch");
		private static readonly int _Debug_Output                  = Shader.PropertyToID("_Debug_Output");

		// Keywords
		public static readonly string VR_COMPATIBILITY              = "VR_COMPATIBILITY";
		public static readonly string USE_TEMPORAL_INVALIDITY       = "USE_TEMPORAL_INVALIDITY";
		public static readonly string USE_RECEIVE_LAYER_MASK        = "USE_RECEIVE_LAYER_MASK";
		public static readonly string USE_SPATIAL_OCCLUSION         = "USE_SPATIAL_OCCLUSION";
		public static readonly string INTERPOLATION_OUTPUT          = "INTERPOLATION_OUTPUT";
		public static readonly string VALIDATE_SPATIAL_OCCLUSION    = "VALIDATE_SPATIAL_OCCLUSION";
		public static readonly string AUTOMATIC_THICKNESS           = "AUTOMATIC_THICKNESS";
		public static readonly string LINEAR_THICKNESS              = "LINEAR_THICKNESS";
		public static readonly string UNIFORM_THICKNESS             = "UNIFORM_THICKNESS";
		public static readonly string CHECKERBOARDING               = "CHECKERBOARDING";
		public static readonly string FALLBACK_APV                  = "FALLBACK_APV";
		public static readonly string FALLBACK_SKY                  = "FALLBACK_SKY";
		public static readonly string FULL_RESOLUTION_DEPTH         = "FULL_RESOLUTION_DEPTH";
		public static readonly string REFINE_INTERSECTION           = "REFINE_INTERSECTION";
		public static readonly string VALIDATE_TEMPORAL_OCCLUSION   = "VALIDATE_TEMPORAL_OCCLUSION";
		public static readonly string VALIDATE_TEMPORAL_LIGHTING    = "VALIDATE_TEMPORAL_LIGHTING";
		public static readonly string HALF_STEP_VALIDATION          = "HALF_STEP_VALIDATION";
		public static readonly string REPROJECT_TEMPORAL_INVALIDITY = "REPROJECT_TEMPORAL_INVALIDITY";
		public static readonly string AUTOMATIC_BRIGHTNESS_CLAMP    = "AUTOMATIC_BRIGHTNESS_CLAMP";
		public static readonly string REPROJECT_SPATIAL_OCCLUSION   = "REPROJECT_SPATIAL_OCCLUSION";
		public static readonly string FULL_RESOLUTION_REPROJECTION  = "FULL_RESOLUTION_REPROJECTION";
		public static readonly string REPROJECT_COLOR               = "REPROJECT_COLOR";
		
		// Profiler Samplers
		public static ProfilingSampler AmbientLightingOverrideSampler   = new ProfilingSampler("Ambient Lighting Override");
		public static ProfilingSampler TemporalReprojectionSampler      = new ProfilingSampler("Temporal Reprojection");
		public static ProfilingSampler CheckerboardingSampler           = new ProfilingSampler("Checkerboarding");
		public static ProfilingSampler RadianceTracingSampler           = new ProfilingSampler("Trace Radiance");
		public static ProfilingSampler RestirTemporalSampler            = new ProfilingSampler("ReSTIR Temporal");
		public static ProfilingSampler RestirFireflySampler             = new ProfilingSampler("ReSTIR Firefly");
		public static ProfilingSampler RestirSpatialSampler             = new ProfilingSampler("ReSTIR Spatial");
		public static ProfilingSampler TemporalAccumulationSampler      = new ProfilingSampler("Temporal Accumulation");
		public static ProfilingSampler SpatialFilterSampler             = new ProfilingSampler("Spatial Filter");
		public static ProfilingSampler InterpolationSampler             = new ProfilingSampler("Interpolation");
		public static ProfilingSampler DebugSampler                     = new ProfilingSampler("Debug");
		public static ProfilingSampler IndirectLightingInjectionSampler = new ProfilingSampler("Indirect Lighting Injection");
		
		// Computes
		public static ComputeShader HDebug                = null;
		public static ComputeShader HRenderSSGI           = null;
		public static ComputeShader HReSTIR               = null;
		public static ComputeShader HDenoiser             = null;
		public static ComputeShader HInterpolation        = null;
		public static ComputeShader HCheckerboarding      = null;
		public static ComputeShader PyramidGeneration     = null;
		public static ComputeShader HTemporalReprojection = null;
		
		// Materials
		public static Material ColorCompose_BIRP;
		public static Material ColorCompose_URP;
		
		// Buffers
		public static ComputeBuffer PointDistributionBuffer;
		public static ComputeBuffer LuminanceMoments;
		public static ComputeBuffer IndirectArguments;
		public static ComputeBuffer RayCounter;
		public static HDynamicBuffer IndirectCoords;
		
		// Variables
		public static RenderTexture finalOutput;
		
		internal struct HistoryData : IHistoryData
		{
			public bool RecurrentBlur;
			public float ScaleFactor;
			public void Update()
			{
				HTraceSSGIProfile profile = HTraceSSGISettings.ActiveProfile;
				RecurrentBlur = profile.DenoisingSettings.RecurrentBlur;
				ScaleFactor = Mathf.Round(1.0f / profile.SSGISettings.RenderScale * 100f) / 100f;
			}
		}

		internal static HistoryData History = new HistoryData();
		
		internal static void Execute(CommandBuffer cmd, Camera camera, int cameraWidth, int cameraHeight, RTHandle cameraColorBuffer = null, RTHandle previousColorBuffer = null)
		{
			HTraceSSGIProfile profile = HTraceSSGISettings.ActiveProfile;
			
			bool UseAPV = false;
			bool UseInterpolation = Mathf.Approximately(profile.SSGISettings.RenderScale, 1.0f) ? false : true;
			float RenderScaleFactor = (Mathf.Round(1.0f / profile.SSGISettings.RenderScale * 100f) / 100f);

			// Depth to PositionVS parameters
			float FovRadians = camera.fieldOfView * Mathf.Deg2Rad;
			float TanHalfFOVY = Mathf.Tan(FovRadians * 0.5f);
			float invHalfTanFov = 1 / TanHalfFOVY;
			Vector2 focalLen = new Vector2(invHalfTanFov * ((float)cameraWidth / (float)cameraWidth), invHalfTanFov);
			Vector2 invFocalLen = new Vector2(1 / focalLen.x, 1 / focalLen.y);
			Vector4 DepthToViewParams = new Vector4(2 * invFocalLen.x, 2 * invFocalLen.y, -1 * invFocalLen.x, -1 * invFocalLen.y);

			// Tracing thickness management
			float Thickness = Mathf.Clamp(profile.SSGISettings.Thickness, 0.05f, 1.0f);
			float n = camera.nearClipPlane;
			float f = camera.farClipPlane;
			Vector4 ThicknessScaleBias = new Vector4();
			ThicknessScaleBias.x = 1.0f / (1.0f + Thickness / 5.0f);
			ThicknessScaleBias.y = -n / (f - n) * (Thickness / 5.0f * ThicknessScaleBias.x);
			ThicknessScaleBias.z = 1.0f / (1.0f + Thickness / 5.0f * 2.0f);
			ThicknessScaleBias.w = -n / (f - n) * (Thickness / 5.0f * 2.0f * ThicknessScaleBias.z);

			if ((int)profile.SSGISettings.ThicknessMode == 1)
			{
				ThicknessScaleBias.x = 1.0f;
				ThicknessScaleBias.y = Thickness;
				ThicknessScaleBias.z = 1.0f;
				ThicknessScaleBias.w = Thickness * 2.0f;
			}

			// Spatial radius management
			float FilterRadiusReSTIR = profile.DenoisingSettings.SpatialRadius - 0.25f; // HData.DenoisingData.SpatialRadius / 2.0f;
			float FilterRadiusDenoiser = FilterRadiusReSTIR * 0.7f;
			FilterRadiusReSTIR = FilterRadiusReSTIR.RoundToCeilTail(1);
			FilterRadiusDenoiser = FilterRadiusDenoiser.RoundToCeilTail(1);



			// ---------------------------------------- PARAMETERS SET ---------------------------------------- //
			cmd.SetGlobalVector(_DepthToViewParams, DepthToViewParams);
			cmd.SetGlobalFloat(_HScaleFactorSSGI, RenderScaleFactor);
			cmd.SetGlobalFloat(_HPreviousScaleFactorSSGI, History.ScaleFactor);
			cmd.SetGlobalFloat(_ReservoirDiffuseWeight, profile.GeneralSettings.DebugMode == DebugMode.GlobalIllumination ? 0.0f : 1.0f);

#if UNITY_2023_3_OR_NEWER
			cmd.SetGlobalInt(_ExcludeCastingLayerMaskSSGI, profile.GeneralSettings.ExcludeCastingMask);
			cmd.SetGlobalInt(_ExcludeReceivingLayerMaskSSGI, profile.GeneralSettings.ExcludeReceivingMask);
#endif

			cmd.SetGlobalVector(_APVParams, new Vector4(profile.GeneralSettings.NormalBias, profile.GeneralSettings.ViewBias, profile.GeneralSettings.SamplingNoise, profile.GeneralSettings.IntensityMultiplier));

			cmd.SetComputeFloatParam(HTemporalReprojection, _BrightnessClamp, profile.DenoisingSettings.MaxValueBrightnessClamp);
			cmd.SetComputeFloatParam(HTemporalReprojection, _MaxDeviation, profile.DenoisingSettings.MaxDeviationBrightnessClamp);

			cmd.SetComputeIntParam(HRenderSSGI, _RayCount, profile.SSGISettings.RayCount);
			cmd.SetComputeIntParam(HRenderSSGI, _StepCount, profile.SSGISettings.StepCount);
			cmd.SetComputeFloatParam(HRenderSSGI, _RayLength, Mathf.Max(profile.SSGISettings.MaxRayLength, 0.1f));
			cmd.SetComputeFloatParam(HRenderSSGI, _BackfaceLighting, profile.SSGISettings.BackfaceLighting);
			cmd.SetComputeFloatParam(HRenderSSGI, _SkyFallbackIntensity, profile.GeneralSettings.SkyIntensity);
			cmd.SetComputeFloatParam(HRenderSSGI, _Falloff, profile.SSGISettings.Falloff);
			cmd.SetComputeVectorParam(HRenderSSGI, _ThicknessParams, ThicknessScaleBias);

			cmd.SetComputeIntParam(HReSTIR, _DenoiseFallback, (profile.GeneralSettings.DenoiseFallback || profile.GeneralSettings.FallbackType == FallbackType.None) ? 1 : 0);
			cmd.SetComputeIntParam(HReSTIR, _RayCount, profile.SSGISettings.RayCount);
			cmd.SetComputeIntParam(HReSTIR, _StepCount, profile.SSGISettings.StepCount);
			cmd.SetComputeFloatParam(HReSTIR, _RayLength, Mathf.Max(profile.SSGISettings.MaxRayLength, 0.1f));
			cmd.SetComputeFloatParam(HReSTIR, _BackfaceLighting, profile.SSGISettings.BackfaceLighting);
			cmd.SetComputeFloatParam(HReSTIR, _FilterAdaptivity, profile.DenoisingSettings.Adaptivity);
			cmd.SetComputeFloatParam(HReSTIR, _Falloff, profile.SSGISettings.Falloff);
			cmd.SetComputeFloatParam(HReSTIR, _FilterRadius, FilterRadiusReSTIR);
			cmd.SetComputeVectorParam(HReSTIR, _ThicknessParams, ThicknessScaleBias);

			cmd.SetComputeFloatParam(HDenoiser, _FilterAdaptivity, profile.DenoisingSettings.Adaptivity);
			cmd.SetComputeFloatParam(HDenoiser, _FilterRadius, FilterRadiusDenoiser);
			cmd.SetComputeFloatParam(HDenoiser, _StepCount, profile.SSGISettings.StepCount);
			
			RTWrapper ColorPreviousFrame = CameraHistorySystem.GetCameraData().ColorPreviousFrame;
			RTWrapper ReservoirTemporal = CameraHistorySystem.GetCameraData().ReservoirTemporal;
			RTWrapper SampleCount = CameraHistorySystem.GetCameraData().SampleCount;
			RTWrapper NormalDepth = CameraHistorySystem.GetCameraData().NormalDepth;
			RTWrapper NormalDepthFullRes = CameraHistorySystem.GetCameraData().NormalDepthFullRes;
			RTWrapper Radiance = CameraHistorySystem.GetCameraData().Radiance;
			RTWrapper RadianceAccumulated = CameraHistorySystem.GetCameraData().RadianceAccumulated;
			RTWrapper SpatialOcclusionAccumulated = CameraHistorySystem.GetCameraData().SpatialOcclusionAccumulated;
			RTWrapper TemporalInvalidityAccumulated = CameraHistorySystem.GetCameraData().TemporalInvalidityAccumulated;
			RTWrapper AmbientOcclusionAccumulated = CameraHistorySystem.GetCameraData().AmbientOcclusionAccumulated;
			
			 
			// ---------------------------------------- TEMPORAL REPROJECTION ---------------------------------------- //
			using (new ProfilingScope(cmd, TemporalReprojectionSampler))
			{	
				var CurrentColorBuffer = ColorPreviousFrame.rt;
				var PreviousColorBuffer = ColorPreviousFrame.rt;
				
				CurrentColorBuffer = cameraColorBuffer;
				
				cmd.SetComputeTextureParam(HTemporalReprojection, (int)HTemporalReprojectionKernels.LuminanceMomentsGeneration, _Color_History, profile.GeneralSettings.Multibounce ?  PreviousColorBuffer : CurrentColorBuffer);
				cmd.SetComputeBufferParam(HTemporalReprojection, (int)HTemporalReprojectionKernels.LuminanceMomentsGeneration, _LuminanceMoments, LuminanceMoments);
				cmd.DispatchCompute(HTemporalReprojection, (int)HTemporalReprojectionKernels.LuminanceMomentsGeneration, Mathf.CeilToInt(cameraWidth / 8.0f), Mathf.CeilToInt(cameraHeight / 8.0f), 1);

				KeywordSwitch(HTemporalReprojection, profile.DenoisingSettings.TemporalLightingValidation | profile.DenoisingSettings.TemporalOcclusionValidation, REPROJECT_TEMPORAL_INVALIDITY);
				KeywordSwitch(HTemporalReprojection, profile.DenoisingSettings.BrightnessClamp == BrightnessClamp.Automatic, AUTOMATIC_BRIGHTNESS_CLAMP);
				KeywordSwitch(HTemporalReprojection, profile.DenoisingSettings.SpatialOcclusionValidation, REPROJECT_SPATIAL_OCCLUSION);
				KeywordSwitch(HTemporalReprojection, UseInterpolation == false, FULL_RESOLUTION_REPROJECTION);
				KeywordSwitch(HTemporalReprojection, profile.GeneralSettings.Multibounce, REPROJECT_COLOR);
				
				if (UseInterpolation)
				{
					cmd.SetComputeTextureParam(HTemporalReprojection, (int)HTemporalReprojectionKernels.ColorReprojection, _Color_History, PreviousColorBuffer);
					cmd.SetComputeTextureParam(HTemporalReprojection, (int)HTemporalReprojectionKernels.ColorReprojection, _Radiance_History, RadianceStabilized.rt);
					cmd.SetComputeTextureParam(HTemporalReprojection, (int)HTemporalReprojectionKernels.ColorReprojection, _Radiance_Output, RadianceStabilizedReprojected.rt);
					cmd.SetComputeTextureParam(HTemporalReprojection, (int)HTemporalReprojectionKernels.ColorReprojection, _NormalDepth_History, NormalDepthFullRes.rt);
					cmd.SetComputeTextureParam(HTemporalReprojection, (int)HTemporalReprojectionKernels.ColorReprojection, _ReprojectedColor_Output, ColorReprojected.rt);
					cmd.SetComputeBufferParam(HTemporalReprojection, (int)HTemporalReprojectionKernels.ColorReprojection, _LuminanceMoments, LuminanceMoments);
					cmd.DispatchCompute(HTemporalReprojection, (int)HTemporalReprojectionKernels.ColorReprojection, Mathf.CeilToInt(cameraWidth / 8.0f), Mathf.CeilToInt(cameraHeight / 8.0f), HRenderer.TextureXrSlices);
				}
				
				cmd.SetComputeTextureParam(HTemporalReprojection, (int)HTemporalReprojectionKernels.TemporalReprojection, _Color_History, PreviousColorBuffer);
				cmd.SetComputeTextureParam(HTemporalReprojection, (int)HTemporalReprojectionKernels.TemporalReprojection, _Samplecount_History, SampleCount.rt);
				cmd.SetComputeTextureParam(HTemporalReprojection, (int)HTemporalReprojectionKernels.TemporalReprojection, _Samplecount_Output, SamplecountReprojected.rt);
				cmd.SetComputeTextureParam(HTemporalReprojection, (int)HTemporalReprojectionKernels.TemporalReprojection, _NormalDepth_History, NormalDepth.rt);
				cmd.SetComputeTextureParam(HTemporalReprojection, (int)HTemporalReprojectionKernels.TemporalReprojection, _ReprojectedColor_Output, ColorReprojected.rt); 
				cmd.SetComputeTextureParam(HTemporalReprojection, (int)HTemporalReprojectionKernels.TemporalReprojection, _Radiance_Output, RadianceReprojected.rt);
				cmd.SetComputeTextureParam(HTemporalReprojection, (int)HTemporalReprojectionKernels.TemporalReprojection, _Radiance_History, profile.DenoisingSettings.RecurrentBlur ? Radiance.rt : RadianceAccumulated.rt);
				cmd.SetComputeTextureParam(HTemporalReprojection, (int)HTemporalReprojectionKernels.TemporalReprojection, _SpatialOcclusion_History, SpatialOcclusionAccumulated.rt);
				cmd.SetComputeTextureParam(HTemporalReprojection, (int)HTemporalReprojectionKernels.TemporalReprojection, _SpatialOcclusion_Output, SpatialOcclusionReprojected.rt);
				cmd.SetComputeTextureParam(HTemporalReprojection, (int)HTemporalReprojectionKernels.TemporalReprojection, _Reservoir, ReservoirTemporal.rt);
				cmd.SetComputeTextureParam(HTemporalReprojection, (int)HTemporalReprojectionKernels.TemporalReprojection, _Reservoir_Output, ReservoirReprojected.rt);
				cmd.SetComputeTextureParam(HTemporalReprojection, (int)HTemporalReprojectionKernels.TemporalReprojection, _TemporalInvalidity_History, TemporalInvalidityAccumulated.rt);
				cmd.SetComputeTextureParam(HTemporalReprojection, (int)HTemporalReprojectionKernels.TemporalReprojection, _TemporalInvalidity_Output, TemporalInvalidityReprojected.rt);
				cmd.SetComputeTextureParam(HTemporalReprojection, (int)HTemporalReprojectionKernels.TemporalReprojection, _AmbientOcclusion_History, AmbientOcclusionAccumulated.rt);
				cmd.SetComputeTextureParam(HTemporalReprojection, (int)HTemporalReprojectionKernels.TemporalReprojection, _AmbientOcclusion_Output, AmbientOcclusionReprojected.rt);
				cmd.SetComputeBufferParam(HTemporalReprojection, (int)HTemporalReprojectionKernels.TemporalReprojection, _LuminanceMoments, LuminanceMoments);
				cmd.DispatchCompute(HTemporalReprojection, (int)HTemporalReprojectionKernels.TemporalReprojection, Mathf.CeilToInt(cameraWidth / RenderScaleFactor / 8.0f), Mathf.CeilToInt(cameraHeight / RenderScaleFactor / 8.0f), HRenderer.TextureXrSlices);
				
				cmd.SetComputeBufferParam(HTemporalReprojection, (int)HTemporalReprojectionKernels.LuminanceMomentsClear, _LuminanceMoments, LuminanceMoments);
				cmd.DispatchCompute(HTemporalReprojection, (int)HTemporalReprojectionKernels.LuminanceMomentsClear, 1, 1, 1);
			}
			
		
			// ---------------------------------------- CHECKERBOARDING ---------------------------------------- //
			if (profile.SSGISettings.Checkerboard)
			{
				using (new ProfilingScope(cmd, CheckerboardingSampler))
				{
					cmd.SetComputeTextureParam(HCheckerboarding, (int)HCheckerboardingKernels.CheckerboardClassification, _SampleCount, SamplecountReprojected.rt);
					cmd.SetComputeBufferParam(HCheckerboarding, (int)HCheckerboardingKernels.CheckerboardClassification, _RayCounter_Output, RayCounter);
					cmd.SetComputeBufferParam(HCheckerboarding, (int)HCheckerboardingKernels.CheckerboardClassification, _IndirectCoords_Output, IndirectCoords.ComputeBuffer);
					cmd.DispatchCompute(HCheckerboarding, (int)HCheckerboardingKernels.CheckerboardClassification, Mathf.CeilToInt(cameraWidth / RenderScaleFactor / 8.0f), Mathf.CeilToInt(cameraHeight / RenderScaleFactor / 8.0f), HRenderer.TextureXrSlices);
				
					cmd.SetComputeIntParam(HCheckerboarding, _RayTracedCounter, 0);
					cmd.SetComputeBufferParam(HCheckerboarding, (int)HCheckerboardingKernels.IndirectArguments, _RayCounter, RayCounter);
					cmd.SetComputeBufferParam(HCheckerboarding, (int)HCheckerboardingKernels.IndirectArguments, _IndirectArguments_Output, IndirectArguments);
					cmd.DispatchCompute(HCheckerboarding, (int)HCheckerboardingKernels.IndirectArguments, 1, 1, HRenderer.TextureXrSlices);
				}
			}

			
			// ---------------------------------------- GI TRACING ---------------------------------------- //
			using (new ProfilingScope(cmd, RadianceTracingSampler))
			{
				CoreUtils.SetRenderTarget(cmd, RadianceFiltered.rt, ClearFlag.Color, Color.clear, 0, CubemapFace.Unknown, -1);
			
				KeywordSwitch(HRenderSSGI, profile.GeneralSettings.FallbackType == FallbackType.Sky, FALLBACK_SKY);
				KeywordSwitch(HRenderSSGI, (int)profile.GeneralSettings.FallbackType == 2 /* Only in HDRP & URP 6000+ */, FALLBACK_APV);
				KeywordSwitch(HRenderSSGI, profile.SSGISettings.Checkerboard, CHECKERBOARDING);
				KeywordSwitch(HRenderSSGI, profile.SSGISettings.RefineIntersection, REFINE_INTERSECTION);
				KeywordSwitch(HRenderSSGI, profile.SSGISettings.FullResolutionDepth, FULL_RESOLUTION_DEPTH);
				
				if ((int)profile.SSGISettings.ThicknessMode == 0) {HRenderSSGI.EnableKeyword(LINEAR_THICKNESS); HRenderSSGI.DisableKeyword(UNIFORM_THICKNESS); HRenderSSGI.DisableKeyword(AUTOMATIC_THICKNESS);}
				if ((int)profile.SSGISettings.ThicknessMode == 1) {HRenderSSGI.EnableKeyword(UNIFORM_THICKNESS); HRenderSSGI.DisableKeyword(LINEAR_THICKNESS); HRenderSSGI.DisableKeyword(AUTOMATIC_THICKNESS);}
				
				cmd.SetComputeTextureParam(HRenderSSGI, (int)HRenderSSGIKernels.TraceSSGI, _Color, ColorReprojected.rt);
				cmd.SetComputeTextureParam(HRenderSSGI, (int)HRenderSSGIKernels.TraceSSGI, _Radiance_Output, RadianceFiltered.rt);
				cmd.SetComputeTextureParam(HRenderSSGI, (int)HRenderSSGIKernels.TraceSSGI, _Reservoir_Output, Reservoir.rt);
				cmd.SetComputeTextureParam(HRenderSSGI, (int)HRenderSSGIKernels.TraceSSGI, _AmbientOcclusion_Output, AmbientOcclusion.rt);
				cmd.SetComputeTextureParam(HRenderSSGI, (int)HRenderSSGIKernels.TraceSSGI, _SampleCount, SamplecountReprojected.rt);
				cmd.SetComputeBufferParam(HRenderSSGI, (int)HRenderSSGIKernels.TraceSSGI, _RayCounter, RayCounter);
				cmd.SetComputeBufferParam(HRenderSSGI, (int)HRenderSSGIKernels.TraceSSGI, _TracingCoords, IndirectCoords.ComputeBuffer);
				
				if (profile.SSGISettings.Checkerboard)
				{	
					cmd.SetComputeIntParam(HRenderSSGI, HShaderParams.IndexXR, 0);
					cmd.DispatchCompute(HRenderSSGI, (int)HRenderSSGIKernels.TraceSSGI, IndirectArguments, 0);

					if (HRenderer.TextureXrSlices > 1)
					{	
						cmd.SetComputeIntParam(HRenderSSGI, HShaderParams.IndexXR, 1);
						cmd.DispatchCompute(HRenderSSGI, (int)HRenderSSGIKernels.TraceSSGI, IndirectArguments, sizeof(uint) * 3);
					}
				}
				else cmd.DispatchCompute(HRenderSSGI, (int)HRenderSSGIKernels.TraceSSGI, Mathf.CeilToInt(cameraWidth / RenderScaleFactor / 8.0f), Mathf.CeilToInt(cameraHeight / RenderScaleFactor / 8.0f), HRenderer.TextureXrSlices);
			}

			
			// ---------------------------------------- RESTIR TEMPORAL ---------------------------------------- //
			using (new ProfilingScope(cmd, RestirTemporalSampler))
			{	
				KeywordSwitch(HReSTIR, profile.DenoisingSettings.TemporalOcclusionValidation, VALIDATE_TEMPORAL_OCCLUSION);
				KeywordSwitch(HReSTIR, profile.DenoisingSettings.TemporalLightingValidation, VALIDATE_TEMPORAL_LIGHTING);
				KeywordSwitch(HReSTIR, profile.DenoisingSettings.HalfStepValidation, HALF_STEP_VALIDATION);
				KeywordSwitch(HReSTIR, profile.SSGISettings.FullResolutionDepth, FULL_RESOLUTION_DEPTH);
				KeywordSwitch(HReSTIR, profile.SSGISettings.Checkerboard, CHECKERBOARDING);
				
				if ((int)profile.SSGISettings.ThicknessMode == 0) {HReSTIR.EnableKeyword(LINEAR_THICKNESS); HReSTIR.DisableKeyword(UNIFORM_THICKNESS); HReSTIR.DisableKeyword(AUTOMATIC_THICKNESS);}
				if ((int)profile.SSGISettings.ThicknessMode == 1) {HReSTIR.EnableKeyword(UNIFORM_THICKNESS); HReSTIR.DisableKeyword(LINEAR_THICKNESS); HReSTIR.DisableKeyword(AUTOMATIC_THICKNESS);}
				
				cmd.SetComputeTextureParam(HReSTIR, (int)HReSTIRKernels.TemporalResampling, _Radiance_Output, RadianceFiltered.rt);
				cmd.SetComputeTextureParam(HReSTIR, (int)HReSTIRKernels.TemporalResampling, _SampleCount, SamplecountReprojected.rt);
				cmd.SetComputeTextureParam(HReSTIR, (int)HReSTIRKernels.TemporalResampling, _Color, ColorReprojected.rt);
				cmd.SetComputeTextureParam(HReSTIR, (int)HReSTIRKernels.TemporalResampling, _AmbientOcclusion, AmbientOcclusion.rt);
				cmd.SetComputeTextureParam(HReSTIR, (int)HReSTIRKernels.TemporalResampling, _AmbientOcclusionInvalidity, AmbientOcclusionInvalidity.rt);
				cmd.SetComputeTextureParam(HReSTIR, (int)HReSTIRKernels.TemporalResampling, _AmbientOcclusionReprojected, AmbientOcclusionReprojected.rt);
				cmd.SetComputeTextureParam(HReSTIR, (int)HReSTIRKernels.TemporalResampling, _AmbientOcclusion_Output, AmbientOcclusionAccumulated.rt);
				cmd.SetComputeTextureParam(HReSTIR, (int)HReSTIRKernels.TemporalResampling, _SpatialOcclusion, SpatialOcclusionReprojected.rt);
				cmd.SetComputeTextureParam(HReSTIR, (int)HReSTIRKernels.TemporalResampling, _Reservoir, Reservoir.rt);
				cmd.SetComputeTextureParam(HReSTIR, (int)HReSTIRKernels.TemporalResampling, _ReservoirReprojected, ReservoirReprojected.rt);
				cmd.SetComputeTextureParam(HReSTIR, (int)HReSTIRKernels.TemporalResampling, _ReservoirSpatial_Output, ReservoirSpatial.rt);
				cmd.SetComputeTextureParam(HReSTIR, (int)HReSTIRKernels.TemporalResampling, _ReservoirTemporal_Output, ReservoirTemporal.rt);
				cmd.SetComputeTextureParam(HReSTIR, (int)HReSTIRKernels.TemporalResampling, _TemporalInvalidity, TemporalInvalidityReprojected.rt);
				cmd.SetComputeTextureParam(HReSTIR, (int)HReSTIRKernels.TemporalResampling, _TemporalInvalidity_Output, TemporalInvalidityAccumulated.rt);
				cmd.SetComputeTextureParam(HReSTIR, (int)HReSTIRKernels.TemporalResampling, _NormalDepth_HistoryOutput, NormalDepth.rt);
				cmd.SetComputeTextureParam(HReSTIR, (int)HReSTIRKernels.TemporalResampling, _ReservoirLuminance_Output, ReservoirLuminance.rt); 
				cmd.SetComputeBufferParam(HReSTIR, (int)HReSTIRKernels.TemporalResampling, _RayCounter, RayCounter);
				cmd.SetComputeBufferParam(HReSTIR, (int)HReSTIRKernels.TemporalResampling, _TracingCoords, IndirectCoords.ComputeBuffer);
				cmd.DispatchCompute(HReSTIR, (int)HReSTIRKernels.TemporalResampling, Mathf.CeilToInt(cameraWidth / RenderScaleFactor / 8.0f), Mathf.CeilToInt(cameraHeight / RenderScaleFactor / 8.0f), HRenderer.TextureXrSlices);
			}

			
			// ---------------------------------------- RESTIR FIREFLY ---------------------------------------- //
			using (new ProfilingScope(cmd, RestirFireflySampler))
			{
				if (profile.DenoisingSettings.FireflySuppression)
				{
					cmd.SetComputeTextureParam(HReSTIR, (int)HReSTIRKernels.FireflySuppression, _SampleCount, SamplecountReprojected.rt);
					cmd.SetComputeTextureParam(HReSTIR, (int)HReSTIRKernels.FireflySuppression, _ReservoirLuminance, ReservoirLuminance.rt);
					cmd.SetComputeTextureParam(HReSTIR, (int)HReSTIRKernels.FireflySuppression, _ReservoirSpatial_Output, ReservoirSpatial.rt);
					cmd.DispatchCompute(HReSTIR, (int)HReSTIRKernels.FireflySuppression, Mathf.CeilToInt(cameraWidth / RenderScaleFactor / 8.0f), Mathf.CeilToInt(cameraHeight / RenderScaleFactor / 8.0f), HRenderer.TextureXrSlices);
				}
			}
			
			
			// ---------------------------------------- RESTIR SPATIAL ---------------------------------------- //
			using (new ProfilingScope(cmd, RestirSpatialSampler))
			{				
				KeywordSwitch(HReSTIR, profile.DenoisingSettings.SpatialOcclusionValidation, VALIDATE_SPATIAL_OCCLUSION); 
				KeywordSwitch(HReSTIR, HRenderer.TextureXrSlices > 1, VR_COMPATIBILITY);
				
				cmd.SetComputeBufferParam(HDenoiser, (int)HDenoiserKernels.PointDistributionFill, _PointDistribution_Output, PointDistributionBuffer);
				cmd.DispatchCompute(HDenoiser, (int)HDenoiserKernels.PointDistributionFill, 1, 1, 1);

				cmd.SetComputeTextureParam(HReSTIR, (int)HReSTIRKernels.SpatialResampling, _Reservoir, ReservoirSpatial.rt);
				cmd.SetComputeTextureParam(HReSTIR, (int)HReSTIRKernels.SpatialResampling, _Reservoir_Output, Reservoir.rt);
				cmd.SetComputeTextureParam(HReSTIR, (int)HReSTIRKernels.SpatialResampling, _SampleCount, SamplecountReprojected.rt);
				cmd.SetComputeTextureParam(HReSTIR, (int)HReSTIRKernels.SpatialResampling, _TemporalInvalidity, TemporalInvalidityAccumulated.rt);
				cmd.SetComputeTextureParam(HReSTIR, (int)HReSTIRKernels.SpatialResampling, _TemporalInvalidity_Output, TemporalInvalidityFilteredA.rt);
				cmd.SetComputeTextureParam(HReSTIR, (int)HReSTIRKernels.SpatialResampling, _AmbientOcclusion, AmbientOcclusionAccumulated.rt);
				cmd.SetComputeTextureParam(HReSTIR, (int)HReSTIRKernels.SpatialResampling, _SpatialGuidance_Output, AmbientOcclusionGuidance.rt);
				cmd.SetComputeBufferParam(HReSTIR, (int)HReSTIRKernels.SpatialResampling, _PointDistribution, PointDistributionBuffer);
				cmd.DispatchCompute(HReSTIR, (int)HReSTIRKernels.SpatialResampling, Mathf.CeilToInt(cameraWidth / RenderScaleFactor / 8.0f), Mathf.CeilToInt(cameraHeight / RenderScaleFactor / 8.0f), HRenderer.TextureXrSlices);
				
				cmd.SetComputeTextureParam(HReSTIR, (int)HReSTIRKernels.SpatialValidation, _Color, ColorReprojected.rt);
				cmd.SetComputeTextureParam(HReSTIR, (int)HReSTIRKernels.SpatialValidation, _Radiance_Output, Radiance.rt);
				cmd.SetComputeTextureParam(HReSTIR, (int)HReSTIRKernels.SpatialValidation, _SampleCount, SamplecountReprojected.rt);
				cmd.SetComputeTextureParam(HReSTIR, (int)HReSTIRKernels.SpatialValidation, _AmbientOcclusion, AmbientOcclusionAccumulated.rt);
				cmd.SetComputeTextureParam(HReSTIR, (int)HReSTIRKernels.SpatialValidation, _Reservoir, Reservoir.rt);
				cmd.SetComputeTextureParam(HReSTIR, (int)HReSTIRKernels.SpatialValidation, _Reservoir_Output, ReservoirSpatial.rt);
				cmd.SetComputeTextureParam(HReSTIR, (int)HReSTIRKernels.SpatialValidation, _SpatialOcclusion, SpatialOcclusionReprojected.rt);
				cmd.SetComputeTextureParam(HReSTIR, (int)HReSTIRKernels.SpatialValidation, _SpatialOcclusion_Output, SpatialOcclusionAccumulated.rt);
				cmd.SetComputeTextureParam(HReSTIR, (int)HReSTIRKernels.SpatialValidation, _TemporalInvalidity, TemporalInvalidityFilteredA.rt);
				cmd.SetComputeTextureParam(HReSTIR, (int)HReSTIRKernels.SpatialValidation, _TemporalInvalidity_Output, TemporalInvalidityFilteredB.rt);
				cmd.SetComputeTextureParam(HReSTIR, (int)HReSTIRKernels.SpatialValidation, _SpatialGuidance, AmbientOcclusionGuidance.rt);
				cmd.SetComputeBufferParam(HReSTIR, (int)HReSTIRKernels.SpatialValidation, _PointDistribution, PointDistributionBuffer);
				cmd.DispatchCompute(HReSTIR, (int)HReSTIRKernels.SpatialValidation, Mathf.CeilToInt(cameraWidth / RenderScaleFactor / 8.0f), Mathf.CeilToInt(cameraHeight / RenderScaleFactor / 8.0f), HRenderer.TextureXrSlices);
			}
			
			
			// ---------------------------------------- DENOISER TEMPORAL ---------------------------------------- //
			using (new ProfilingScope(cmd, TemporalAccumulationSampler))
			{		
				KeywordSwitch(HDenoiser, profile.DenoisingSettings.TemporalLightingValidation | profile.DenoisingSettings.TemporalOcclusionValidation, USE_TEMPORAL_INVALIDITY);
				KeywordSwitch(HDenoiser, HRenderer.TextureXrSlices > 1, VR_COMPATIBILITY);
				
				cmd.SetComputeTextureParam(HDenoiser, (int)HDenoiserKernels.TemporalAccumulation, _TemporalInvalidity, TemporalInvalidityFilteredB.rt);
				cmd.SetComputeTextureParam(HDenoiser, (int)HDenoiserKernels.TemporalAccumulation, _SamplecountReprojected, SamplecountReprojected.rt);
				cmd.SetComputeTextureParam(HDenoiser, (int)HDenoiserKernels.TemporalAccumulation, _Samplecount_Output, SampleCount.rt);
				cmd.SetComputeTextureParam(HDenoiser, (int)HDenoiserKernels.TemporalAccumulation, _Radiance, Radiance.rt);
				cmd.SetComputeTextureParam(HDenoiser, (int)HDenoiserKernels.TemporalAccumulation, _RadianceReprojected, RadianceReprojected.rt);
				cmd.SetComputeTextureParam(HDenoiser, (int)HDenoiserKernels.TemporalAccumulation, _Radiance_TemporalOutput, RadianceAccumulated.rt);
				cmd.SetComputeTextureParam(HDenoiser, (int)HDenoiserKernels.TemporalAccumulation, _Radiance_SpatialOutput, RadianceFiltered.rt);
				cmd.DispatchCompute(HDenoiser, (int)HDenoiserKernels.TemporalAccumulation, Mathf.CeilToInt(cameraWidth / RenderScaleFactor / 8.0f), Mathf.CeilToInt(cameraHeight / RenderScaleFactor / 8.0f), HRenderer.TextureXrSlices);
			}
			
		
			// ---------------------------------------- DENOISER SPATIAL ---------------------------------------- //
			using (new ProfilingScope(cmd, SpatialFilterSampler))
			{	
				KeywordSwitch(HDenoiser, profile.DenoisingSettings.SpatialOcclusionValidation, USE_SPATIAL_OCCLUSION);
				KeywordSwitch(HDenoiser, UseInterpolation, INTERPOLATION_OUTPUT);

				cmd.SetComputeTextureParam(HDenoiser, (int)HDenoiserKernels.SpatialFilter1, _NormalDepth, NormalDepth.rt);
				cmd.SetComputeTextureParam(HDenoiser, (int)HDenoiserKernels.SpatialFilter1, _SpatialGuidance, AmbientOcclusionGuidance.rt);
				cmd.SetComputeTextureParam(HDenoiser, (int)HDenoiserKernels.SpatialFilter1, _AmbientOcclusion, AmbientOcclusionAccumulated.rt);
				cmd.SetComputeTextureParam(HDenoiser, (int)HDenoiserKernels.SpatialFilter1, _Radiance, RadianceFiltered.rt);
				cmd.SetComputeTextureParam(HDenoiser, (int)HDenoiserKernels.SpatialFilter1, _Radiance_Output, Radiance.rt);
				cmd.SetComputeTextureParam(HDenoiser, (int)HDenoiserKernels.SpatialFilter1, _SpatialOcclusion, SpatialOcclusionAccumulated.rt);
				cmd.SetComputeBufferParam(HDenoiser, (int)HDenoiserKernels.SpatialFilter1, _PointDistribution, PointDistributionBuffer);
				cmd.DispatchCompute(HDenoiser, (int)HDenoiserKernels.SpatialFilter1, Mathf.CeilToInt(cameraWidth / RenderScaleFactor / 8.0f), Mathf.CeilToInt(cameraHeight / RenderScaleFactor / 8.0f), HRenderer.TextureXrSlices);
				
				cmd.SetComputeTextureParam(HDenoiser, (int)HDenoiserKernels.SpatialFilter2, _NormalDepth, NormalDepth.rt);
				cmd.SetComputeTextureParam(HDenoiser, (int)HDenoiserKernels.SpatialFilter2, _SpatialGuidance, AmbientOcclusionGuidance.rt);
				cmd.SetComputeTextureParam(HDenoiser, (int)HDenoiserKernels.SpatialFilter2, _AmbientOcclusion, AmbientOcclusionAccumulated.rt);
				cmd.SetComputeTextureParam(HDenoiser, (int)HDenoiserKernels.SpatialFilter2, _Radiance, Radiance.rt);
				cmd.SetComputeTextureParam(HDenoiser, (int)HDenoiserKernels.SpatialFilter2, _Radiance_Output, RadianceFiltered.rt);
				cmd.SetComputeTextureParam(HDenoiser, (int)HDenoiserKernels.SpatialFilter2, _RadianceNormalDepth_Output, RadianceNormalDepth.rt);
				cmd.SetComputeTextureParam(HDenoiser, (int)HDenoiserKernels.SpatialFilter2, _SpatialOcclusion, SpatialOcclusionAccumulated.rt);
				cmd.SetComputeBufferParam(HDenoiser, (int)HDenoiserKernels.SpatialFilter2, _PointDistribution, PointDistributionBuffer);
				cmd.DispatchCompute(HDenoiser, (int)HDenoiserKernels.SpatialFilter2, Mathf.CeilToInt(cameraWidth / RenderScaleFactor / 8.0f), Mathf.CeilToInt(cameraHeight / RenderScaleFactor / 8.0f), HRenderer.TextureXrSlices);
			}

			
			// ---------------------------------------- INTERPOLATION SPATIAL ---------------------------------------- //
			using (new ProfilingScope(cmd, InterpolationSampler))
			{
				if (UseInterpolation)
				{	
					KeywordSwitch(HInterpolation, HRenderer.TextureXrSlices > 1, VR_COMPATIBILITY); 
					
					// Interpolation
					cmd.SetComputeTextureParam(HInterpolation, (int)HInterpolationKernels.Interpolation, _RadianceNormalDepth, RadianceNormalDepth.rt);
					cmd.SetComputeTextureParam(HInterpolation, (int)HInterpolationKernels.Interpolation, _Radiance_Output,  RadianceInterpolated.rt);
					cmd.SetComputeTextureParam(HInterpolation, (int)HInterpolationKernels.Interpolation, _NormalDepth_HistoryOutput, NormalDepthFullRes.rt);
					cmd.DispatchCompute(HInterpolation, (int)HInterpolationKernels.Interpolation, Mathf.CeilToInt(cameraWidth / 8.0f), Mathf.CeilToInt(cameraHeight / 8.0f), HRenderer.TextureXrSlices);
					
					// Stabilization
					cmd.SetComputeTextureParam(HDenoiser, (int)HDenoiserKernels.TemporalStabilization, _TemporalInvalidity, TemporalInvalidityFilteredB.rt);
					cmd.SetComputeTextureParam(HDenoiser, (int)HDenoiserKernels.TemporalStabilization, _SamplecountReprojected, SamplecountReprojected.rt);
					cmd.SetComputeTextureParam(HDenoiser, (int)HDenoiserKernels.TemporalStabilization, _Radiance, RadianceInterpolated.rt);
					cmd.SetComputeTextureParam(HDenoiser, (int)HDenoiserKernels.TemporalStabilization, _RadianceReprojected, RadianceStabilizedReprojected.rt);
					cmd.SetComputeTextureParam(HDenoiser, (int)HDenoiserKernels.TemporalStabilization, _Radiance_TemporalOutput, RadianceStabilized.rt);
					cmd.DispatchCompute(HDenoiser, (int)HDenoiserKernels.TemporalStabilization, Mathf.CeilToInt(cameraWidth / 8.0f), Mathf.CeilToInt(cameraHeight / 8.0f), HRenderer.TextureXrSlices);
				}
			}
			
			finalOutput = Mathf.Approximately(profile.SSGISettings.RenderScale, 1.0f) ? SSGI.RadianceFiltered.rt : SSGI.RadianceInterpolated.rt;
				
			// --------------------------------------------- DEBUG ----------------------------------------------- //
			using (new ProfilingScope(cmd, DebugSampler))
			{
				if (profile.GeneralSettings.DebugMode != DebugMode.None && profile.GeneralSettings.DebugMode != DebugMode.DirectLighting)
				{
					cmd.SetComputeIntParams(HDebug, _DebugSwitch, (int)profile.GeneralSettings.DebugMode);
					cmd.SetComputeIntParams(HDebug, _BuffersSwitch, (int)profile.GeneralSettings.HBuffer);
					cmd.SetComputeTextureParam(HDebug, 0, _Debug_Output, DebugOutput.rt);
					cmd.SetComputeTextureParam(HDebug, 0, _SampleCountSSGI, SamplecountReprojected.rt);
					cmd.SetComputeTextureParam(HDebug, 0, _HTraceBufferGI, finalOutput);
					cmd.DispatchCompute(HDebug, 0, Mathf.CeilToInt(cameraWidth / 8.0f), Mathf.CeilToInt(cameraHeight / 8.0f), HRenderer.TextureXrSlices);
					
					cmd.SetGlobalTexture(_Debug_Output, DebugOutput.rt);
				}
			}
		}
		
		private static void KeywordSwitch(ComputeShader compute, bool state, string keyword)
		{
			if (state)
				compute.EnableKeyword(keyword);
			else
				compute.DisableKeyword(keyword);
		}
	}
	
}
