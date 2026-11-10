//pipelinedefine
#define H_URP

#if UNITY_EDITOR

using HTraceSSGI.Scripts.Data.Private;
using HTraceSSGI.Scripts.Editor;
using HTraceSSGI.Scripts.Globals;
using UnityEngine;
using UnityEngine.Rendering;
using HTraceSSGI.Scripts.Infrastructure.URP;

namespace HTraceSSGI.Scripts.Data.Public
{
	internal static class HTraceSSGIPresetData
	{
		public static void ApplyPresetVolume(HTraceSSGIVolumeEditorURP hTraceSSGIVolumeEditorUrp, HTraceSSGIPreset preset)
		{
			var              stack  = VolumeManager.instance.stack;
			HTraceSSGIVolume volume = stack?.GetComponent<HTraceSSGIVolume>();
			if (volume == null) return;
			
			HTraceSSGIProfile profile = CreatePresetProfile(HTraceSSGISettings.ActiveProfile, preset);
			if (profile == null) return;
			
			// General Settings
			hTraceSSGIVolumeEditorUrp.p_DebugMode.value.enumValueIndex = (int)profile.GeneralSettings.DebugMode;
			hTraceSSGIVolumeEditorUrp.p_DebugMode.overrideState.boolValue = true;
			
			hTraceSSGIVolumeEditorUrp.p_FallbackType.value.enumValueIndex = (int)profile.GeneralSettings.FallbackType;
			hTraceSSGIVolumeEditorUrp.p_FallbackType.overrideState.boolValue = true;
			
			hTraceSSGIVolumeEditorUrp.p_SkyIntensity.value.floatValue = profile.GeneralSettings.SkyIntensity;
			hTraceSSGIVolumeEditorUrp.p_SkyIntensity.overrideState.boolValue = true;
			
			hTraceSSGIVolumeEditorUrp.p_DenoiseFallback.value.boolValue = profile.GeneralSettings.DenoiseFallback;
			hTraceSSGIVolumeEditorUrp.p_DenoiseFallback.overrideState.boolValue = true;
			
			// Visuals
			hTraceSSGIVolumeEditorUrp.p_BackfaceLighting.value.floatValue = profile.SSGISettings.BackfaceLighting;
			hTraceSSGIVolumeEditorUrp.p_BackfaceLighting.overrideState.boolValue = true;
			
			hTraceSSGIVolumeEditorUrp.p_MaxRayLength.value.floatValue = profile.SSGISettings.MaxRayLength;
			hTraceSSGIVolumeEditorUrp.p_MaxRayLength.overrideState.boolValue = true;
			
			hTraceSSGIVolumeEditorUrp.p_ThicknessMode.value.enumValueIndex = (int)profile.SSGISettings.ThicknessMode;
			hTraceSSGIVolumeEditorUrp.p_ThicknessMode.overrideState.boolValue = true;
			
			hTraceSSGIVolumeEditorUrp.p_Thickness.value.floatValue = profile.SSGISettings.Thickness;
			hTraceSSGIVolumeEditorUrp.p_Thickness.overrideState.boolValue = true;
			
			hTraceSSGIVolumeEditorUrp.p_Falloff.value.floatValue = profile.SSGISettings.Falloff;
			hTraceSSGIVolumeEditorUrp.p_Falloff.overrideState.boolValue = true;
			
			// Quality - Tracing
			hTraceSSGIVolumeEditorUrp.p_RayCount.value.intValue = profile.SSGISettings.RayCount;
			hTraceSSGIVolumeEditorUrp.p_RayCount.overrideState.boolValue = true;
			
			hTraceSSGIVolumeEditorUrp.p_StepCount.value.intValue = profile.SSGISettings.StepCount;
			hTraceSSGIVolumeEditorUrp.p_StepCount.overrideState.boolValue = true;
			
			hTraceSSGIVolumeEditorUrp.p_RefineIntersection.value.boolValue = profile.SSGISettings.RefineIntersection;
			hTraceSSGIVolumeEditorUrp.p_RefineIntersection.overrideState.boolValue = true;
			
			hTraceSSGIVolumeEditorUrp.p_FullResolutionDepth.value.boolValue = profile.SSGISettings.FullResolutionDepth;
			hTraceSSGIVolumeEditorUrp.p_FullResolutionDepth.overrideState.boolValue = true;
			
			// Quality - Rendering
			hTraceSSGIVolumeEditorUrp.p_Checkerboard.value.boolValue = profile.SSGISettings.Checkerboard;
			hTraceSSGIVolumeEditorUrp.p_Checkerboard.overrideState.boolValue = true;
			
			hTraceSSGIVolumeEditorUrp.p_RenderScale.value.floatValue = profile.SSGISettings.RenderScale;
			hTraceSSGIVolumeEditorUrp.p_RenderScale.overrideState.boolValue = true;
			
			// Denoising
			hTraceSSGIVolumeEditorUrp.p_BrightnessClamp.value.enumValueIndex = (int)profile.DenoisingSettings.BrightnessClamp;
			hTraceSSGIVolumeEditorUrp.p_BrightnessClamp.overrideState.boolValue = true;
			
			hTraceSSGIVolumeEditorUrp.p_MaxValueBrightnessClamp.value.floatValue = profile.DenoisingSettings.MaxValueBrightnessClamp;
			hTraceSSGIVolumeEditorUrp.p_MaxValueBrightnessClamp.overrideState.boolValue = true;
			
			hTraceSSGIVolumeEditorUrp.p_MaxDeviationBrightnessClamp.value.floatValue = profile.DenoisingSettings.MaxDeviationBrightnessClamp;
			hTraceSSGIVolumeEditorUrp.p_MaxDeviationBrightnessClamp.overrideState.boolValue = true;
			
			// Denoising - Temporal Validation
			hTraceSSGIVolumeEditorUrp.p_HalfStepValidation.value.boolValue = profile.DenoisingSettings.HalfStepValidation;
			hTraceSSGIVolumeEditorUrp.p_HalfStepValidation.overrideState.boolValue = true;
			
			hTraceSSGIVolumeEditorUrp.p_SpatialOcclusionValidation.value.boolValue = profile.DenoisingSettings.SpatialOcclusionValidation;
			hTraceSSGIVolumeEditorUrp.p_SpatialOcclusionValidation.overrideState.boolValue = true;
			
			hTraceSSGIVolumeEditorUrp.p_TemporalLightingValidation.value.boolValue = profile.DenoisingSettings.TemporalLightingValidation;
			hTraceSSGIVolumeEditorUrp.p_TemporalLightingValidation.overrideState.boolValue = true;
			
			hTraceSSGIVolumeEditorUrp.p_TemporalOcclusionValidation.value.boolValue = profile.DenoisingSettings.TemporalOcclusionValidation;
			hTraceSSGIVolumeEditorUrp.p_TemporalOcclusionValidation.overrideState.boolValue = true;
			
			// Denoising - Spatial Filter
			hTraceSSGIVolumeEditorUrp.p_SpatialRadius.value.floatValue = profile.DenoisingSettings.SpatialRadius;
			hTraceSSGIVolumeEditorUrp.p_SpatialRadius.overrideState.boolValue = true;
			
			hTraceSSGIVolumeEditorUrp.p_Adaptivity.value.floatValue = profile.DenoisingSettings.Adaptivity;
			hTraceSSGIVolumeEditorUrp.p_Adaptivity.overrideState.boolValue = true;
			
			hTraceSSGIVolumeEditorUrp.p_RecurrentBlur.value.boolValue = profile.DenoisingSettings.RecurrentBlur;
			hTraceSSGIVolumeEditorUrp.p_RecurrentBlur.overrideState.boolValue = true;
			
			hTraceSSGIVolumeEditorUrp.p_FireflySuppression.value.boolValue = profile.DenoisingSettings.FireflySuppression;
			hTraceSSGIVolumeEditorUrp.p_FireflySuppression.overrideState.boolValue = true;
		}
		
		/// <summary>
		/// Creates a profile from the specified preset type.
		/// </summary>
		public static HTraceSSGIProfile CreatePresetProfile(HTraceSSGIProfile activeProfile, HTraceSSGIPreset preset)
		{
			switch (preset)
			{
				case HTraceSSGIPreset.Performance:
					return CreatePerformancePreset(activeProfile);
				case HTraceSSGIPreset.Optimized:
					return CreateOptimizedPreset(activeProfile);
				case HTraceSSGIPreset.Balanced:
					return CreateBalancedPreset(activeProfile);
				case HTraceSSGIPreset.Quality:
					return CreateQualityPreset(activeProfile);
				default:
					return null;
			}
		}
		
		public static HTraceSSGIProfile CreatePerformancePreset(HTraceSSGIProfile activeProfile)
		{
			var profile = Object.Instantiate(activeProfile);
			
			// Tracing
			profile.SSGISettings.RayCount = 2;
			profile.SSGISettings.StepCount = 28;
			profile.SSGISettings.RefineIntersection = false;
			profile.SSGISettings.FullResolutionDepth = false;
			
			// Rendering
			profile.SSGISettings.Checkerboard = true;
			profile.SSGISettings.RenderScale = 0.5f;
			
			// ReSTIR Validation
			profile.DenoisingSettings.HalfStepValidation = true;
			profile.DenoisingSettings.SpatialOcclusionValidation = true;
			profile.DenoisingSettings.TemporalLightingValidation = false;
			profile.DenoisingSettings.TemporalOcclusionValidation = false;
			
			// Spatial Filter
			profile.DenoisingSettings.SpatialRadius = 0.65f;
			profile.DenoisingSettings.Adaptivity = 0.7f;
			profile.DenoisingSettings.RecurrentBlur = true;
			profile.DenoisingSettings.FireflySuppression = false;
			
			return profile;
		}
		
		public static HTraceSSGIProfile CreateOptimizedPreset(HTraceSSGIProfile activeProfile)
		{
			var profile = Object.Instantiate(activeProfile);
			
			// Tracing
			profile.SSGISettings.RayCount = 3;
			profile.SSGISettings.StepCount = 30;
			profile.SSGISettings.RefineIntersection = true;
			profile.SSGISettings.FullResolutionDepth = false;
			
			// Rendering
			profile.SSGISettings.Checkerboard = true;
			profile.SSGISettings.RenderScale = 0.75f;
			
			// ReSTIR Validation
			profile.DenoisingSettings.HalfStepValidation = true;
			profile.DenoisingSettings.SpatialOcclusionValidation = true;
			profile.DenoisingSettings.TemporalLightingValidation = true;
			profile.DenoisingSettings.TemporalOcclusionValidation = true;
			
			// Spatial Filter
			profile.DenoisingSettings.SpatialRadius = 0.6f;
			profile.DenoisingSettings.Adaptivity = 0.8f;
			profile.DenoisingSettings.RecurrentBlur = true;
			profile.DenoisingSettings.FireflySuppression = true;
			
			return profile;
		}
		
		public static HTraceSSGIProfile CreateBalancedPreset(HTraceSSGIProfile activeProfile)
		{
			var profile = Object.Instantiate(activeProfile);
			
			// Tracing
			profile.SSGISettings.RayCount = 3;
			profile.SSGISettings.StepCount = 32;
			profile.SSGISettings.RefineIntersection = true;
			profile.SSGISettings.FullResolutionDepth = true;
			
			// Rendering
			profile.SSGISettings.Checkerboard = true;
			profile.SSGISettings.RenderScale = 1.0f;
			
			// ReSTIR Validation
			profile.DenoisingSettings.HalfStepValidation = true;
			profile.DenoisingSettings.SpatialOcclusionValidation = true;
			profile.DenoisingSettings.TemporalLightingValidation = true;
			profile.DenoisingSettings.TemporalOcclusionValidation = true;
			
			// Spatial Filter
			profile.DenoisingSettings.SpatialRadius = 0.6f;
			profile.DenoisingSettings.Adaptivity = 0.9f;
			profile.DenoisingSettings.RecurrentBlur = false;
			profile.DenoisingSettings.FireflySuppression = true;
			
			return profile;
		}

		public static HTraceSSGIProfile CreateQualityPreset(HTraceSSGIProfile activeProfile)
		{
			var profile = Object.Instantiate(activeProfile);
			
			// Tracing
			profile.SSGISettings.RayCount = 4;
			profile.SSGISettings.StepCount = 36;
			profile.SSGISettings.RefineIntersection = true;
			profile.SSGISettings.FullResolutionDepth = true;
			
			// Rendering
			profile.SSGISettings.Checkerboard = false;
			profile.SSGISettings.RenderScale = 1.0f;
			
			// ReSTIR Validation
			profile.DenoisingSettings.HalfStepValidation = false;
			profile.DenoisingSettings.SpatialOcclusionValidation = true;
			profile.DenoisingSettings.TemporalLightingValidation = true;
			profile.DenoisingSettings.TemporalOcclusionValidation = true;
			
			// Spatial Filter
			profile.DenoisingSettings.SpatialRadius = 0.6f;
			profile.DenoisingSettings.Adaptivity = 0.9f;
			profile.DenoisingSettings.RecurrentBlur = false;
			profile.DenoisingSettings.FireflySuppression = true;
			
			return profile;
		}
	}
}

#endif
