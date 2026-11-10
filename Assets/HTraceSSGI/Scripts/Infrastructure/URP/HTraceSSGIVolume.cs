//pipelinedefine
#define H_URP


using System;
using HTraceSSGI.Scripts.Globals;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace HTraceSSGI.Scripts.Infrastructure.URP
{
#if UNITY_2023_1_OR_NEWER
	[VolumeComponentMenu("Lighting/HTrace: Screen Space Global Illumination"), SupportedOnRenderPipeline(typeof(UniversalRenderPipelineAsset))]
#else
[VolumeComponentMenuForRenderPipeline("Lighting/HTrace: Screen Space Global Illumination", typeof(UniversalRenderPipeline))]
#endif
#if UNITY_2023_3_OR_NEWER
	[VolumeRequiresRendererFeatures(typeof(HTraceSSGIRendererFeature))]
#endif
	[HelpURL(HNames.HTRACE_SSGI_DOCUMENTATION_LINK)]
	public sealed class HTraceSSGIVolume : VolumeComponent, IPostProcessComponent
	{
		public HTraceSSGIVolume()
		{
			displayName = HNames.ASSET_NAME_FULL;
		}

		/// <summary>
		/// Enable Screen Space Global Illumination.
		/// </summary>
		[Tooltip("Enable HTrace Screen Space Global Illumination")]
		public BoolParameter Enable = new BoolParameter(false, BoolParameter.DisplayType.EnumPopup);
		[SerializeField, HideInInspector]
		public HTraceSSGIPreset Preset = HTraceSSGIPreset.Balanced;
		
		// --------------------------------------- GLOBAL SETTINGS ---------------------------------------------

		[InspectorName("Debug Mode"), Tooltip("")]
		public DebugModeParameter DebugMode = new(Globals.DebugMode.None, false);

		[InspectorName("Buffer"), Tooltip("Visualizes data from different buffers for debugging")]
		public HBufferParameter HBuffer = new(Globals.HBuffer.Multi, false);

#if UNITY_2023_3_OR_NEWER
		[InspectorName("Exclude Casting Mask"), Tooltip("Prevents objects on the selected layers from contributing to GI")]
		public RenderingLayerMaskEnumParameter ExcludeCastingMask = new(0, false);

		[InspectorName("Exclude Receiving Mask"), Tooltip("Prevents objects on the selected layers from receiving GI")]
		public RenderingLayerMaskEnumParameter ExcludeReceivingMask = new(0, false);
#endif

		[InspectorName("Fallback Type"), Tooltip("Fallback data for rays that failed to find a hit in screen space")]
		public FallbackTypeParameter FallbackType = new FallbackTypeParameter(Globals.FallbackType.None, false);
		
		[InspectorName("Sky Intensity"), Tooltip("Brightness of the sky used for fallback")]
		public ClampedFloatParameter SkyIntensity = new ClampedFloatParameter(0.5f, 0.0f, 1.0f, false);

		[InspectorName("View Bias"), Tooltip("Offsets the APV sample position towards the camera")]
		public ClampedFloatParameter ViewBias = new ClampedFloatParameter(0.1f, 0.0f, 2.0f, false);

		[InspectorName("Normal Bias"), Tooltip("Offsets the APV sample position along the pixel’s surface normal")]
		public ClampedFloatParameter NormalBias = new ClampedFloatParameter(0.25f, 0.0f, 2.0f, false);

		[InspectorName("Sampling Noise"), Tooltip("Amount of jitter noise added to the APV sample position")]
		public ClampedFloatParameter SamplingNoise = new ClampedFloatParameter(0.1f, 0.0f, 1.0f, false);

		[InspectorName("Intensity Multiplier"), Tooltip("Strength of the light contribution from APV")]
		public ClampedFloatParameter IntensityMultiplier = new ClampedFloatParameter(1.0f, 0.0f, 1.0f, false);

		[InspectorName("Denoise Fallback"), Tooltip("Includes fallback lighting in denoising")]
		public BoolParameter DenoiseFallback = new BoolParameter(true, false);

		// ------------------------------------- Pipeline Integration -----------------------------------------
		
		[InspectorName("Metallic Indirect Fallback"), Tooltip("Renders metals as diffuse when no reflections exist, preventing them from going black")]
		public BoolParameter MetallicIndirectFallback = new BoolParameter(false, false);

		[InspectorName("Ambient Override"), Tooltip("Removes Unity’s ambient lighting before HTrace SSGI runs to avoid double GI")]
		public BoolParameter AmbientOverride = new BoolParameter(true, false);

		[InspectorName("Multibounce"), Tooltip("Uses previous frame GI to approximate additional light bounces")]
		public BoolParameter Multibounce = new BoolParameter(true, false);
		
		// -------------------------------------------- Visuals -----------------------------------------------
		
		[InspectorName("Backface Lighting"), Tooltip("Include lighting from back-facing surfaces")]
		public ClampedFloatParameter BackfaceLighting = new ClampedFloatParameter(0.25f, 0.0f, 1.0f, false);

		[InspectorName("Max Ray Length"), Tooltip("Maximum ray distance in meters")]
		public ClampedFloatParameter MaxRayLength = new ClampedFloatParameter(100.0f, 0.0f, float.MaxValue, false);

		[InspectorName("Thickness Mode"), Tooltip("Method used for thickness estimation")]
		public ThicknessModeParameter ThicknessMode = new(Globals.ThicknessMode.Relative, false);

		[InspectorName("Thickness"), Tooltip("Virtual object thickness for ray intersections")]
		public ClampedFloatParameter Thickness = new ClampedFloatParameter(0.35f, 0.0f, 1.0f, false);

		[InspectorName("Intensity"), Tooltip("Indirect lighting intensity multiplier")]
		public ClampedFloatParameter Intensity = new ClampedFloatParameter(1.0f, 0.1f, 5.0f, false);

		[InspectorName("Falloff"), Tooltip("Softens indirect lighting over distance")]
		public ClampedFloatParameter Falloff = new ClampedFloatParameter(0.0f, 0.0f, 1.0f, false);

		// -------------------------------------------- QUALITY -----------------------------------------------
		
		// -------------------------------------------- Tracing -----------------------------------------------

		[InspectorName("Ray Count"), Tooltip("Number of rays per pixel")]
		public ClampedIntParameter RayCount = new ClampedIntParameter(3, 2, 16, false);

		[InspectorName("Step Count"), Tooltip("Number of steps per ray")]
		public ClampedIntParameter StepCount = new ClampedIntParameter(24, 8, 64, false);

		[InspectorName("Refine Intersection"), Tooltip("Extra check to confirm intersection")]
		public BoolParameter RefineIntersection = new BoolParameter(true, false);

		[InspectorName("Full Resolution Depth"), Tooltip("Uses full-res depth buffer for tracing")]
		public BoolParameter FullResolutionDepth = new BoolParameter(true, false);

		// ------------------------------------------- Rendering -----------------------------------------------

		[InspectorName("Checkerboard"), Tooltip("Enables checkerboard rendering, processing only half of the pixels per frame")]
		public BoolParameter Checkerboard = new BoolParameter(false, false);

		[InspectorName("Render Scale"), Tooltip("Local render scale of SSGI pipeline")]
		public ClampedFloatParameter RenderScale = new ClampedFloatParameter(1.0f, 0.5f, 1.0f, false);

		// ------------------------------------------- DENOISING -----------------------------------------------

		[InspectorName("Brightness Clamp"), Tooltip("Defines the maximum brightness at the hit point")]
		public BrightnessClampParameter BrightnessClamp = new(Globals.BrightnessClamp.Manual, false);

		[InspectorName("Max Value"), Tooltip("Maximum brightness allowed at hit points.")]
		public ClampedFloatParameter MaxValueBrightnessClamp = new ClampedFloatParameter(7.0f, 1.0f, 30.0f, false);

		[InspectorName("Max Deviation"), Tooltip("Maximum standard deviation for brightness allowed at hit points")]
		public ClampedFloatParameter MaxDeviationBrightnessClamp = new ClampedFloatParameter(3.0f, 1.0f, 5.0f, false);

		// ---------------------------------------- ReSTIR Validation ------------------------------------------

		[InspectorName("Half Step"), Tooltip("Halves validation ray steps")]
		public BoolParameter HalfStepValidation = new BoolParameter(false, false);

		[InspectorName("Spatial Occlusion"), Tooltip("Protects indirect shadows and details from overblurring")]
		public BoolParameter SpatialOcclusionValidation = new BoolParameter(true, false);

		[InspectorName("Temporal Lighting"), Tooltip("Faster reaction to changing lighting conditions")]
		public BoolParameter TemporalLightingValidation = new BoolParameter(true, false);

		[InspectorName("Temporal Occlusion"), Tooltip("Faster reaction to moving indirect shadows")]
		public BoolParameter TemporalOcclusionValidation = new BoolParameter(true, false);


		// ----------------------------------------- Spatial Filter ---------------------------------------------------

		[InspectorName("Spatial"), Tooltip("Width of spatial filter")]
		public ClampedFloatParameter SpatialRadius = new ClampedFloatParameter(0.6f, 0.0f, 1.0f, false);

		[InspectorName("Adaptivity"), Tooltip("Shrinks the filter radius in geometry corners to preserve details")]
		public ClampedFloatParameter Adaptivity = new ClampedFloatParameter(0.9f, 0.0f, 1.0f, false);

		[InspectorName("Recurrent Blur"), Tooltip("Makes blur stronger by using the spatial output as temporal history")]
		public BoolParameter RecurrentBlur = new BoolParameter(false, false);

		[InspectorName("Firefly Suppression"), Tooltip("Removes bright outliers before denoising")]
		public BoolParameter FireflySuppression = new BoolParameter(false, false);

		// ----------------------------------------------- Debug -----------------------------------------------------

		public BoolParameter ShowBowels = new BoolParameter(false, true);

		public bool IsActive()
		{
			return Enable.value;
		}

#if !UNITY_2023_2_OR_NEWER
		public bool IsTileCompatible() => false;
#endif

		[Serializable]
		public sealed class HBufferParameter : VolumeParameter<HBuffer>
		{
			/// <param name="value">The initial value to store in the parameter.</param>
			/// <param name="overrideState">The initial override state for the parameter.</param>
			public HBufferParameter(HBuffer value, bool overrideState = false) : base(value, overrideState) { }
		}

		[Serializable]
		public sealed class DebugModeParameter : VolumeParameter<DebugMode>
		{
			/// <param name="value">The initial value to store in the parameter.</param>
			/// <param name="overrideState">The initial override state for the parameter.</param>
			public DebugModeParameter(DebugMode value, bool overrideState = false) : base(value, overrideState) { }
		}

#if UNITY_2023_3_OR_NEWER
		[Serializable]
		public sealed class RenderingLayerMaskEnumParameter : VolumeParameter<RenderingLayerMask>
		{
			/// <param name="value">The initial value to store in the parameter.</param>
			/// <param name="overrideState">The initial override state for the parameter.</param>
			public RenderingLayerMaskEnumParameter(RenderingLayerMask value, bool overrideState = false) : base(value, overrideState) { }
		}
#endif

		[Serializable]
		public sealed class FallbackTypeParameter : VolumeParameter<FallbackType>
		{
			/// <param name="value">The initial value to store in the parameter.</param>
			/// <param name="overrideState">The initial override state for the parameter.</param>
			public FallbackTypeParameter(FallbackType value, bool overrideState = false) : base(value, overrideState) { }
		}

		[Serializable]
		public sealed class ThicknessModeParameter : VolumeParameter<ThicknessMode>
		{
			/// <param name="value">The initial value to store in the parameter.</param>
			/// <param name="overrideState">The initial override state for the parameter.</param>
			public ThicknessModeParameter(ThicknessMode value, bool overrideState = false) : base(value, overrideState) { }
		}

		[Serializable]
		public sealed class BrightnessClampParameter : VolumeParameter<BrightnessClamp>
		{
			/// <param name="value">The initial value to store in the parameter.</param>
			/// <param name="overrideState">The initial override state for the parameter.</param>
			public BrightnessClampParameter(BrightnessClamp value, bool overrideState = false) : base(value, overrideState) { }
		}
	}
}
