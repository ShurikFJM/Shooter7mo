//pipelinedefine
#define H_URP

using System;
using HTraceSSGI.Scripts.Data.Private;
using HTraceSSGI.Scripts.Data.Public;
using HTraceSSGI.Scripts.Globals;
using HTraceSSGI.Scripts.Passes.URP;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
#if UNITY_EDITOR
using HTraceSSGI.Scripts.PipelelinesConfigurator;
using UnityEditor;
#endif

namespace HTraceSSGI.Scripts.Infrastructure.URP
{
	[DisallowMultipleRendererFeature]
	[ExecuteAlways, ExecuteInEditMode]
	[HelpURL(HNames.HTRACE_SSGI_DOCUMENTATION_LINK)]
	public class HTraceSSGIRendererFeature : ScriptableRendererFeature
	{
		public bool UseVolumes = false;
		internal static bool IsUseVolumes { get; private set; }

		private PrePassURP   _prePassURP;
		private MotionVectorsURP _motionVectors;
		private GBufferPassURP _gBufferPass;
		private SSGIPassURP  _ssgiPassUrp;
		private FinalPassURP _finalPassURP;
		
		private bool _initialized = false;

		/// <summary>
		/// Initializes this feature's resources. This is called every time serialization happens.
		/// </summary>
		public override void Create()
		{
			name = HNames.ASSET_NAME_FULL;
			// ActiveProfile = Profile;
			IsUseVolumes = UseVolumes;
			Dispose();

			if (UseVolumes)
			{
				var stack = VolumeManager.instance.stack;
				HTraceSSGIVolume hTraceVolume = stack?.GetComponent<HTraceSSGIVolume>();
				SettingsBuild(hTraceVolume);
			}

			_prePassURP   = new PrePassURP();
			_prePassURP.renderPassEvent = RenderPassEvent.AfterRenderingOpaques;
			_motionVectors = new MotionVectorsURP();
			_motionVectors.renderPassEvent = RenderPassEvent.AfterRenderingOpaques;
			_gBufferPass = new GBufferPassURP();
			_gBufferPass.renderPassEvent = RenderPassEvent.AfterRenderingOpaques;
			_ssgiPassUrp  = new SSGIPassURP();
			_ssgiPassUrp.renderPassEvent = RenderPassEvent.AfterRenderingOpaques;
			_finalPassURP = new FinalPassURP();
			_finalPassURP.renderPassEvent = RenderPassEvent.AfterRenderingPostProcessing;

#if UNITY_EDITOR
			HPipelinesConfigurator.AlwaysIncludedShaders();
#endif

			_initialized = true;
		}

#if !UNITY_6000_4_OR_NEWER
		/// <summary>
		/// Called when render targets are allocated and ready to be used.
		/// </summary>
		/// <param name="renderer"></param>
		/// <param name="renderingData"></param>
		/// <!--https://docs.unity3d.com/Packages/com.unity.render-pipelines.universal@13.1/manual/upgrade-guide-2022-1.html-->
		public override void SetupRenderPasses(ScriptableRenderer renderer, in RenderingData renderingData)
		{
			if (_initialized == false)
				return;

			if (HRendererURP.RenderGraphEnabled == false)
			{
				_prePassURP.Initialize(renderer);
				_motionVectors.Initialize(renderer);
				_gBufferPass.Initialize(renderer);
				_ssgiPassUrp.Initialize(renderer);
				_finalPassURP.Initialize(renderer);
			}
		}
#endif

		/// <summary>
		/// Injects one or multiple ScriptableRenderPass in the renderer.
		/// </summary>
		/// <param name="renderer"></param>
		/// <param name="renderingData"></param>
		public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
		{
			if (renderingData.cameraData.cameraType == CameraType.Reflection || renderingData.cameraData.cameraType == CameraType.Preview)
				return;

			var isActive = HTraceSSGISettings.ActiveProfile != null;
			
			IsUseVolumes = UseVolumes;
			if (UseVolumes)
			{
				var stack = VolumeManager.instance.stack;
				var hTraceVolume = stack.GetComponent<HTraceSSGIVolume>();
				isActive = hTraceVolume != null && hTraceVolume.IsActive();
				
				SettingsBuild(hTraceVolume);
			}

#if UNITY_6000_0_OR_NEWER
			HAmbientOverrideVolume.Instance.SetActiveVolume(isActive && HTraceSSGISettings.ActiveProfile != null && HTraceSSGISettings.ActiveProfile.GeneralSettings != null && HTraceSSGISettings.ActiveProfile.GeneralSettings.AmbientOverride);
#endif

			if (!isActive)
				return;
			
#if !UNITY_2022_1_OR_NEWER
			_prePassURP.Initialize(renderer);
			_motionVectors.Initialize(renderer);
			_gBufferPass.Initialize(renderer);
			_ssgiPassUrp.Initialize(renderer);
			_finalPassURP.Initialize(renderer);
#endif	
			renderer.EnqueuePass(_prePassURP);
			renderer.EnqueuePass(_motionVectors);
			renderer.EnqueuePass(_gBufferPass);
			renderer.EnqueuePass(_ssgiPassUrp);
			renderer.EnqueuePass(_finalPassURP);
		}

		private void SettingsBuild(HTraceSSGIVolume hTraceVolume)
		{
			if (hTraceVolume == null || UseVolumes == false)
				return;

			if (HTraceSSGISettings.ActiveProfile == null)
			{
				HTraceSSGISettings.SetProfile(ScriptableObject.CreateInstance<HTraceSSGIProfile>());
#if UNITY_EDITOR
				HTraceSSGISettings.ActiveProfile.ApplyPreset(HTraceSSGIPreset.Balanced);	
#endif
				HTraceSSGISettings.ActiveProfile.name = "New HTrace SSGI Profile";
			}

			var activeProfile = HTraceSSGISettings.ActiveProfile;
			
			// Global Settings
			activeProfile.GeneralSettings.DebugMode = hTraceVolume.DebugMode.value;
			activeProfile.GeneralSettings.HBuffer = hTraceVolume.HBuffer.value;
			
			// Global - Pipeline Integration
			activeProfile.GeneralSettings.MetallicIndirectFallback = hTraceVolume.MetallicIndirectFallback.value;
			activeProfile.GeneralSettings.AmbientOverride = hTraceVolume.AmbientOverride.value;
			activeProfile.GeneralSettings.Multibounce = hTraceVolume.Multibounce.value;
#if UNITY_2023_3_OR_NEWER
			activeProfile.GeneralSettings.ExcludeReceivingMask = hTraceVolume.ExcludeReceivingMask.value;
			activeProfile.GeneralSettings.ExcludeCastingMask = hTraceVolume.ExcludeCastingMask.value;
#endif
			activeProfile.GeneralSettings.FallbackType = hTraceVolume.FallbackType.value;
			activeProfile.GeneralSettings.SkyIntensity = hTraceVolume.SkyIntensity.value;
			
			// APV parameters
			activeProfile.GeneralSettings.ViewBias = hTraceVolume.ViewBias.value;
			activeProfile.GeneralSettings.NormalBias = hTraceVolume.NormalBias.value;
			activeProfile.GeneralSettings.SamplingNoise = hTraceVolume.SamplingNoise.value;
			activeProfile.GeneralSettings.IntensityMultiplier = hTraceVolume.IntensityMultiplier.value;
			activeProfile.GeneralSettings.DenoiseFallback = hTraceVolume.DenoiseFallback.value;
			
			// Global - Visuals
			activeProfile.SSGISettings.BackfaceLighting = hTraceVolume.BackfaceLighting.value;
			activeProfile.SSGISettings.MaxRayLength = hTraceVolume.MaxRayLength.value;
			activeProfile.SSGISettings.ThicknessMode = hTraceVolume.ThicknessMode.value;
			activeProfile.SSGISettings.Thickness = hTraceVolume.Thickness.value;
			activeProfile.SSGISettings.Intensity = hTraceVolume.Intensity.value;
			activeProfile.SSGISettings.Falloff = hTraceVolume.Falloff.value;
			
			// Quality - Tracing
			activeProfile.SSGISettings.RayCount = hTraceVolume.RayCount.value;
			activeProfile.SSGISettings.StepCount = hTraceVolume.StepCount.value;
			activeProfile.SSGISettings.RefineIntersection = hTraceVolume.RefineIntersection.value;
			activeProfile.SSGISettings.FullResolutionDepth = hTraceVolume.FullResolutionDepth.value;

			// Quality - Rendering
			activeProfile.SSGISettings.Checkerboard = hTraceVolume.Checkerboard.value;
			activeProfile.SSGISettings.RenderScale = hTraceVolume.RenderScale.value;

			// Denoising
			activeProfile.DenoisingSettings.BrightnessClamp = hTraceVolume.BrightnessClamp.value;
			activeProfile.DenoisingSettings.MaxValueBrightnessClamp = hTraceVolume.MaxValueBrightnessClamp.value;
			activeProfile.DenoisingSettings.MaxDeviationBrightnessClamp = hTraceVolume.MaxDeviationBrightnessClamp.value;

			// Denoising - ReSTIR Validation
			activeProfile.DenoisingSettings.HalfStepValidation = hTraceVolume.HalfStepValidation.value;
			activeProfile.DenoisingSettings.SpatialOcclusionValidation = hTraceVolume.SpatialOcclusionValidation.value;
			activeProfile.DenoisingSettings.TemporalLightingValidation = hTraceVolume.TemporalLightingValidation.value;
			activeProfile.DenoisingSettings.TemporalOcclusionValidation = hTraceVolume.TemporalOcclusionValidation.value;

			// Denoising - Spatial Filter
			activeProfile.DenoisingSettings.SpatialRadius = hTraceVolume.SpatialRadius.value;
			activeProfile.DenoisingSettings.Adaptivity = hTraceVolume.Adaptivity.value;
			activeProfile.DenoisingSettings.RecurrentBlur = hTraceVolume.RecurrentBlur.value;
			activeProfile.DenoisingSettings.FireflySuppression = hTraceVolume.FireflySuppression.value;

			// Debug
			activeProfile.DebugSettings.ShowBowels = hTraceVolume.ShowBowels.value;
		}

		protected override void Dispose(bool disposing)
		{
#if UNITY_6000_0_OR_NEWER
			HAmbientOverrideVolume.Instance.SetActiveVolume(isActive && HTraceSSGISettings.ActiveProfile != null && HTraceSSGISettings.ActiveProfile.GeneralSettings != null && HTraceSSGISettings.ActiveProfile.GeneralSettings.AmbientOverride);
#endif
			
			_prePassURP?.Dispose();
			_motionVectors?.Dispose();
			_gBufferPass?.Dispose();
			_ssgiPassUrp?.Dispose();
			_finalPassURP?.Dispose();

			_prePassURP = null;
			_motionVectors = null;
			_gBufferPass = null;
			_ssgiPassUrp = null;
			_finalPassURP = null;

			_initialized = false;
		}
	}
}
