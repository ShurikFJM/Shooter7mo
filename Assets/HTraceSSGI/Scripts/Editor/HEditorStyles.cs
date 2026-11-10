#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace HTraceSSGI.Scripts.Editor
{
	internal static class HEditorStyles
	{
		public static float defaultLineSpace = EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
		public static float additionalLineSpace = 10f;
		public static float helpBoxHeight = EditorGUIUtility.singleLineHeight * 2;
		public static float checkBoxOffsetWidth = 15f;
		public static float checkBoxWidth = 15f;
		public static float tabOffset = 8f;

		// Buttons name
		public const string FixButtonName = "Fix";
		public const string ChangeButtonName = "Change";
		public const string OpenButtonName = "Open";

		// Debug Tab
		public static GUIContent DebugContent           = new GUIContent("Debug");
		public static GUIContent hTraceLayerContent    = new GUIContent("HTrace Layer", "Excludes objects from HTrace rendering on a per-layer basis");

		// Foldout names
		public static GUIContent GlobalSettings = new GUIContent("Global Settings");
		public static GUIContent PipelineIntegration = new GUIContent("Pipeline Integration");
		public static GUIContent Visuals = new GUIContent("Visuals");
		public static GUIContent Quality = new GUIContent("Quality");
		public static GUIContent Tracing = new GUIContent("Tracing");
		public static GUIContent Rendering = new GUIContent("Rendering");
		public static GUIContent Denoising = new GUIContent("Denoising");
		public static GUIContent ReSTIRValidation = new GUIContent("ReSTIR Validation");
		public static GUIContent ValidationTypes = new GUIContent("Validation Types:");
		public static GUIContent SpatialFilter = new GUIContent("Spatial Filter");
		public static GUIContent Debug = new GUIContent("Debug");

		// General Tab
		public static GUIContent DebugModeContent = new GUIContent("Debug Mode",   "Visualizes the debug mode for different rendering components of H-Trace.");
		public static GUIContent HBuffer          = new GUIContent("Buffer",   "Visualizes the debug mode for different buffers");
		
		// Visuals
		public static GUIContent ThicknessMode        = new GUIContent("Thickness Mode", "Method for thickness estimation.");
		public static GUIContent Thickness            = new GUIContent("Thickness", "Virtual object thickness for ray intersections.");
		public static GUIContent BackfaceLighting         = new GUIContent("Backface Lighting", "");
		public static GUIContent MaxRayLength         = new GUIContent("Max Ray Length", "Maximum ray distance in meters.");
		public static GUIContent FallbackType         = new("Fallback", "Method used when a ray misses.");
		public static GUIContent SkyIntensity         = new("Sky Intensity", "Brightness of Sky used for ray misses.");
		public static GUIContent ExcludeCastingMask   = new("Exclude Casting", "Prevents objects from casting GI.");
		public static GUIContent ExcludeReceivingMask = new("Exclude Receiving", "Prevents objects from receiving screen-space GI.");
		public static GUIContent NormalBias           = new("APV Normal Bias");
		public static GUIContent ViewBias             = new("APV View Bias");
		public static GUIContent SamplingNoise        = new("APV Sampling Noise");
		public static GUIContent DenoiseFallback       = new("Denoise Fallback", "Includes fallback lighting in denoising.");
		public static GUIContent Intensity            = new GUIContent("Intensity");
		public static GUIContent Falloff              = new GUIContent("Falloff", "Softens indirect lighting over distance.");

		// Quality tab
		// Tracing
		public static GUIContent RayCount              = new GUIContent("Ray Count", "Number of rays per pixel.");
		public static GUIContent StepCount             = new GUIContent("Step Count", "Number of steps per ray.");
		public static GUIContent RefineIntersection    = new GUIContent("Refine Intersection", "Extra check to confirm hits.");
		public static GUIContent FullResolutionDepth = new GUIContent("Full Resolution Depth", "Uses full-res depth buffer for tracing.");
		
		//Rendering
		public static GUIContent Checkerboard   = new GUIContent("Checkerboard");
		public static GUIContent RenderScale = new GUIContent("Render Scale", "Local render scale of SSGI.");
		
		// Denoising tab
		public static GUIContent BrightnessClamp             = new("Brightness Clamp", "Method for clamping brightness at hit point.");
		public static GUIContent MaxValueBrightnessClamp     = new("            Max Value", "Maximum brightness allowed at hit points.");
		public static GUIContent MaxDeviationBrightnessClamp = new("      Max Deviation", "Maximum standard deviation for brightness allowed at hit points.");
		
		// ReSTIR Validation
		public static GUIContent HalfStepValidation          = new("Half-Step Tracing", "Halves validation ray steps.");
		public static GUIContent SpatialOcclusionValidation  = new("Spatial Occlusion", "Preserves detail, reduces leaks during blur.");
		public static GUIContent TemporalLightingValidation  = new("Temporal Lighting", "Reacts faster to changing lights.");
		public static GUIContent TemporalOcclusionValidation = new("Temporal Occlusion", "Reacts faster to moving shadows.");

		// Spatial Fliter
		public static GUIContent SpatialRadius      = new GUIContent("Radius", "Width of spatial filter.");
		public static GUIContent Adaptivity         = new GUIContent("Adaptivity", "Shrinks filter radius in geometry corners to preserve detail.");
		public static GUIContent RecurrentBlur      = new GUIContent("Recurrent Blur", "Stronger blur with low cost, less temporal reactivity.");
		public static GUIContent FireflySuppression = new GUIContent("Firefly Suppression", "Removes bright outliers before denoising.");
		
		public static GUIStyle bold = new GUIStyle()
		{
			alignment = TextAnchor.MiddleLeft,
			margin = new RectOffset(),
			padding = new RectOffset(2, 0, 0, 0),
			fontSize = 12,
			normal = new GUIStyleState()
			{
				textColor = new Color(0.903f, 0.903f, 0.903f, 1f),
			},
			fontStyle = FontStyle.Bold,
		};
		
		public static GUIStyle hiddenFoldout = new GUIStyle()
		{
			alignment = TextAnchor.MiddleLeft,
			margin = new RectOffset(),
			padding = new RectOffset(),
			fontSize = 12,
			normal = new GUIStyleState()
			{
				//textColor = new Color(0.703f, 0.703f, 0.703f, 1f), //default color
				textColor = new Color(0.500f, 0.500f, 0.500f, 1f),
			},
			fontStyle = FontStyle.Bold,
		};

		public static GUIStyle headerFoldout = new GUIStyle()
		{
			alignment = TextAnchor.MiddleLeft,
			margin = new RectOffset(),
			padding = new RectOffset(),
			fontSize = 12,
			normal = new GUIStyleState()
			{
				textColor = new Color(0.903f, 0.903f, 0.903f, 1f),
			},
			fontStyle = FontStyle.Bold,
		};
		
		//buttons gui styles
		public static Color warningBackgroundColor = new Color(1,1, 0);
		public static Color warningColor           = new Color(1, 1, 1);

		public static GUIStyle foldout = EditorStyles.foldout;
	}
}
#endif