

using HTraceSSGI.Scripts.Data.Public;
using HTraceSSGI.Scripts.Globals;
using UnityEngine;

namespace HTraceSSGI.Scripts.Data.Private
{
	[CreateAssetMenu(fileName = "HTraceSSGI Profile", menuName = "HTrace/SSGI Profile", order = 251)]
	[HelpURL(HNames.HTRACE_SSGI_DOCUMENTATION_LINK)]
	public class HTraceSSGIProfile : ScriptableObject
	{
		[SerializeField]
		public GeneralSettings GeneralSettings = new GeneralSettings();
		[SerializeField]
		public SSGISettings SSGISettings = new SSGISettings();
		[SerializeField]
		public DenoisingSettings DenoisingSettings = new DenoisingSettings();
		[SerializeField]
		public DebugSettings DebugSettings = new DebugSettings();
		[SerializeField, HideInInspector]
		private HTraceSSGIPreset _preset = HTraceSSGIPreset.Balanced;

#if  UNITY_EDITOR
		/// <summary>
		/// Applies a preset configuration to this profile.
		/// This will overwrite all current settings with the preset values.
		/// </summary>
		/// <param name="preset">The preset type to apply</param>
		public void ApplyPreset(HTraceSSGIPreset preset)
		{
			var presetProfile = HTraceSSGIPresetData.CreatePresetProfile(this, preset);
			if (presetProfile == null)
				return;
			
			// Copy all settings from preset profile
			CopySettingsFrom(presetProfile);
			_preset = preset;
		}
#endif
		
		/// <summary>
		/// Copies all settings from another profile to this profile.
		/// </summary>
		/// <param name="sourceProfile">The profile to copy settings from</param>
		public void CopySettingsFrom(HTraceSSGIProfile sourceProfile)
		{
			if (sourceProfile == null)
				return;
			
			// Copy General Settings
			GeneralSettings.DebugMode = sourceProfile.GeneralSettings.DebugMode;
			GeneralSettings.HBuffer = sourceProfile.GeneralSettings.HBuffer;
			GeneralSettings.MetallicIndirectFallback = sourceProfile.GeneralSettings.MetallicIndirectFallback;
			GeneralSettings.AmbientOverride = sourceProfile.GeneralSettings.AmbientOverride;
			GeneralSettings.Multibounce = sourceProfile.GeneralSettings.Multibounce;
#if UNITY_2023_3_OR_NEWER
			GeneralSettings.ExcludeCastingMask = sourceProfile.GeneralSettings.ExcludeCastingMask;
			GeneralSettings.ExcludeReceivingMask = sourceProfile.GeneralSettings.ExcludeReceivingMask;
#endif
			GeneralSettings.FallbackType = sourceProfile.GeneralSettings.FallbackType;
			GeneralSettings.SkyIntensity = sourceProfile.GeneralSettings.SkyIntensity;
			GeneralSettings.ViewBias = sourceProfile.GeneralSettings.ViewBias;
			GeneralSettings.NormalBias = sourceProfile.GeneralSettings.NormalBias;
			GeneralSettings.SamplingNoise = sourceProfile.GeneralSettings.SamplingNoise;
			GeneralSettings.IntensityMultiplier = sourceProfile.GeneralSettings.IntensityMultiplier;
			GeneralSettings.DenoiseFallback = sourceProfile.GeneralSettings.DenoiseFallback;
			
			// Copy SSGI Settings
			SSGISettings.BackfaceLighting = sourceProfile.SSGISettings.BackfaceLighting;
			SSGISettings.MaxRayLength = sourceProfile.SSGISettings.MaxRayLength;
			SSGISettings.ThicknessMode = sourceProfile.SSGISettings.ThicknessMode;
			SSGISettings.Thickness = sourceProfile.SSGISettings.Thickness;
			SSGISettings.Intensity = sourceProfile.SSGISettings.Intensity;
			SSGISettings.Falloff = sourceProfile.SSGISettings.Falloff;
			SSGISettings.RayCount = sourceProfile.SSGISettings.RayCount;
			SSGISettings.StepCount = sourceProfile.SSGISettings.StepCount;
			SSGISettings.RefineIntersection = sourceProfile.SSGISettings.RefineIntersection;
			SSGISettings.FullResolutionDepth = sourceProfile.SSGISettings.FullResolutionDepth;
			SSGISettings.Checkerboard = sourceProfile.SSGISettings.Checkerboard;
			SSGISettings.RenderScale = sourceProfile.SSGISettings.RenderScale;
			
			// Copy Denoising Settings
			DenoisingSettings.BrightnessClamp = sourceProfile.DenoisingSettings.BrightnessClamp;
			DenoisingSettings.MaxValueBrightnessClamp = sourceProfile.DenoisingSettings.MaxValueBrightnessClamp;
			DenoisingSettings.MaxDeviationBrightnessClamp = sourceProfile.DenoisingSettings.MaxDeviationBrightnessClamp;
			DenoisingSettings.HalfStepValidation = sourceProfile.DenoisingSettings.HalfStepValidation;
			DenoisingSettings.SpatialOcclusionValidation = sourceProfile.DenoisingSettings.SpatialOcclusionValidation;
			DenoisingSettings.TemporalLightingValidation = sourceProfile.DenoisingSettings.TemporalLightingValidation;
			DenoisingSettings.TemporalOcclusionValidation = sourceProfile.DenoisingSettings.TemporalOcclusionValidation;
			DenoisingSettings.SpatialRadius = sourceProfile.DenoisingSettings.SpatialRadius;
			DenoisingSettings.Adaptivity = sourceProfile.DenoisingSettings.Adaptivity;
			DenoisingSettings.RecurrentBlur = sourceProfile.DenoisingSettings.RecurrentBlur;
			DenoisingSettings.FireflySuppression = sourceProfile.DenoisingSettings.FireflySuppression;
		}
	}
}
