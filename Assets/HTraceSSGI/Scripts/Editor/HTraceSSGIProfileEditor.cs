//pipelinedefine
#define H_URP

#if UNITY_EDITOR
using HTraceSSGI.Scripts.Data.Private;
using HTraceSSGI.Scripts.Editor.WindowsAndMenu;
using HTraceSSGI.Scripts.Globals;
using UnityEditor;
using UnityEditor.AnimatedValues;
using UnityEditor.Rendering;
using UnityEngine;

using HTraceSSGI.Scripts.Infrastructure.URP;

namespace HTraceSSGI.Scripts.Editor
{
	[CustomEditor(typeof(HTraceSSGIProfile))]
	internal class HTraceSSGIProfileEditor : UnityEditor.Editor
	{
		SerializedProperty Preset;
		
		bool _globalSettingsTab = true;
		bool _qualityTab = true;
		bool _denoisingTab = true;
		bool _debugTab = true;

		private AnimBool AnimBoolGeneralTab;
		private AnimBool AnimBoolQualityTab;
		private AnimBool AnimBoolDenoisingTab;
		private AnimBool AnimBoolDebugTab;
		private AnimBool AnimBoolEMPTY;
		
		bool _showPipelineIntegration = true;
		bool _showVisualsArea = true;
		bool _showTracingArea = true;
		bool _showRenderingArea = true;
		bool _showRestirValidationArea = true;
		bool _showSpatialArea = true;

		SerializedProperty GeneralSettings;
		SerializedProperty SSGISettings;
		SerializedProperty DenoisingSettings;
		SerializedProperty DebugSettings;
		
		// Debug Data
		SerializedProperty EnableDebug;
		SerializedProperty HTraceLayer;

		// General Tab
		SerializedProperty DebugMode;
		SerializedProperty HBuffer;
		SerializedProperty MainCamera;
		
		SerializedProperty MetallicIndirectFallback;
		SerializedProperty AmbientOverride;
		SerializedProperty Multibounce;

		SerializedProperty ExcludeReceivingMask;
		SerializedProperty ExcludeCastingMask;
		SerializedProperty FallbackType;
		SerializedProperty SkyIntensity;
		//Apv
		SerializedProperty ViewBias;
		SerializedProperty NormalBias;
		SerializedProperty SamplingNoise;
		SerializedProperty DenoiseFallback;

		// Visuals
		SerializedProperty BackfaceLighting;
		SerializedProperty MaxRayLength;
		SerializedProperty ThicknessMode;
		SerializedProperty Thickness;
		SerializedProperty Intensity;
		SerializedProperty Falloff;
		
		// Quality tab
		// Tracing
		SerializedProperty RayCount;
		SerializedProperty StepCount;
		SerializedProperty RefineIntersection;
		SerializedProperty FullResolutionDepth;

		// Rendering
		SerializedProperty Checkerboard;
		SerializedProperty RenderScale;
		
		// Denoising tab
		SerializedProperty BrightnessClamp;
		SerializedProperty MaxValueBrightnessClamp;
		SerializedProperty MaxDeviationBrightnessClamp;
		
		// Temporal
		SerializedProperty HalfStepValidation;
		SerializedProperty SpatialOcclusionValidation;
		SerializedProperty TemporalLightingValidation;
		SerializedProperty TemporalOcclusionValidation;
		
		// Spatial Filter
		SerializedProperty SpatialRadius;
		SerializedProperty Adaptivity;
		// SerializedProperty SpatialPassCount;
		SerializedProperty RecurrentBlur;
		SerializedProperty FireflySuppression;
		
		// Debug DEVS
		SerializedProperty ShowBowels;
		SerializedProperty ShowFullDebugLog;
		SerializedProperty TestCheckBox1;
		SerializedProperty TestCheckBox2;
		SerializedProperty TestCheckBox3;
		
		private const string NO_RENDERER_FEATURE_MESSAGE  = "HTrace Screen Space Global Illumination feature is missing in the active URP renderer.";
		private const string RENDERER_FEATURE_OFF_MESSAGE = "HTrace Screen Space Global Illumination is disabled in the active URP renderer.";
		
		private void OnEnable()
		{
			if (target == null)
				return;

			PropertiesRelative();

			AnimBoolGeneralTab = new AnimBool(_globalSettingsTab);
			AnimBoolGeneralTab.valueChanged.RemoveAllListeners();
			AnimBoolGeneralTab.valueChanged.AddListener(Repaint);
			
			AnimBoolQualityTab = new AnimBool(_qualityTab);
			AnimBoolQualityTab.valueChanged.RemoveAllListeners();
			AnimBoolQualityTab.valueChanged.AddListener(Repaint);
			
			AnimBoolDenoisingTab = new AnimBool(_denoisingTab);
			AnimBoolDenoisingTab.valueChanged.RemoveAllListeners();
			AnimBoolDenoisingTab.valueChanged.AddListener(Repaint);

			AnimBoolDebugTab = new AnimBool(_debugTab);
			AnimBoolDebugTab.valueChanged.RemoveAllListeners();
			AnimBoolDebugTab.valueChanged.AddListener(Repaint);

			AnimBoolEMPTY = new AnimBool(false);
		}

		public override void OnInspectorGUI()
		{
			if (target == null)
				return;

			serializedObject.Update();
			UpdateStandartStyles();
			// base.OnInspectorGUI();
			//return;

			AnimBoolEMPTY = new AnimBool(false);
			
			Color standartBackgroundColor = GUI.backgroundColor;
			Color standartColor           = GUI.color;
			
			WarningsHandle();

// ------------------------------------- Global settings ----------------------------------------------------------
			
			using (new HEditorUtils.FoldoutScope(AnimBoolGeneralTab, out var shouldDraw, "Global Settings"))
			{
				_globalSettingsTab = shouldDraw;
				if (shouldDraw)
				{
					EditorGUILayout.Space(3f);
            
					EditorGUILayout.BeginHorizontal();
					HTraceSSGIPreset selectedPreset = Preset != null ? (HTraceSSGIPreset)Preset.intValue : HTraceSSGIPreset.Balanced;
					EditorGUI.BeginChangeCheck();
					selectedPreset = (HTraceSSGIPreset)EditorGUILayout.EnumPopup(new GUIContent("Preset"), selectedPreset);
					if (EditorGUI.EndChangeCheck() && Preset != null)
						Preset.intValue = (int)selectedPreset;
					if (GUILayout.Button("Apply", GUILayout.Width(60)))
					{
						HTraceSSGIProfile profileLocal = (HTraceSSGIProfile)target;
						if (profileLocal != null)
						{
							if (Preset != null)
								Preset.intValue = (int)selectedPreset;
							serializedObject.ApplyModifiedProperties();
							profileLocal.ApplyPreset(selectedPreset);
							EditorUtility.SetDirty(profileLocal);
						}
					}
					EditorGUILayout.EndHorizontal();
					EditorGUILayout.Space(3f);

					serializedObject.Update();
					
					EditorGUILayout.PropertyField(DebugMode, HEditorStyles.DebugModeContent);
					if (DebugMode.enumValueIndex == 1)
						EditorGUILayout.PropertyField(HBuffer, HEditorStyles.HBuffer);
					EditorGUILayout.Space(5f);
					

#if UNITY_2023_3_OR_NEWER				
					EditorGUILayout.PropertyField(ExcludeCastingMask, HEditorStyles.ExcludeCastingMask);
					EditorGUILayout.PropertyField(ExcludeReceivingMask, HEditorStyles.ExcludeReceivingMask);
#endif
					EditorGUILayout.Space(3f);

					EditorGUILayout.PropertyField(FallbackType, HEditorStyles.FallbackType);
					if ((Globals.FallbackType)FallbackType.enumValueIndex == Globals.FallbackType.Sky)
						EditorGUILayout.Slider(SkyIntensity, 0.0f, 1.0f, HEditorStyles.SkyIntensity);

					_showPipelineIntegration = EditorGUILayout.BeginFoldoutHeaderGroup(_showPipelineIntegration, "Pipeline Integration");
					if (_showPipelineIntegration)
					{
						EditorGUI.indentLevel++;
						EditorGUILayout.PropertyField(MetallicIndirectFallback, new GUIContent("Metallic Indirect Fallback"));
						EditorGUILayout.PropertyField(AmbientOverride, new GUIContent("Ambient Override"));
						if (RenderSettings.ambientIntensity > 1.0f && AmbientOverride.boolValue == true)
							EditorGUILayout.HelpBox("Ambient Override may not work correctly when the Environment Lighting Multiplier is set above 1 !", MessageType.Warning);
						EditorGUILayout.PropertyField(Multibounce, new GUIContent("Multibounce"));
						EditorGUI.indentLevel--;
						EditorGUILayout.Space(3f);
					}
					EditorGUILayout.EndFoldoutHeaderGroup();
					
					EditorGUILayout.Space(3f);
					
					{
						_showVisualsArea = EditorGUILayout.BeginFoldoutHeaderGroup(_showVisualsArea, "Visuals");
						if (_showVisualsArea)
						{
							EditorGUI.indentLevel++;

							EditorGUILayout.Slider(BackfaceLighting, 0.0f, 1.0f, HEditorStyles.BackfaceLighting);
							EditorGUILayout.PropertyField(MaxRayLength, HEditorStyles.MaxRayLength);
							if (MaxRayLength.floatValue < 0)
								MaxRayLength.floatValue = 0f;
							EditorGUILayout.PropertyField(ThicknessMode, HEditorStyles.ThicknessMode);
							EditorGUILayout.Slider(Thickness, 0.0f, 1.0f, HEditorStyles.Thickness);
							EditorGUILayout.Slider(Intensity, 0.1f, 5.0f, HEditorStyles.Intensity);
							EditorGUILayout.Slider(Falloff,   0.0f, 1.0f, HEditorStyles.Falloff);
							EditorGUI.indentLevel--;
						}
						
						EditorGUILayout.EndFoldoutHeaderGroup();
						EditorGUILayout.Space(3f);
					}
					
					
				}
			}
			
			// -------------------------------------   Quality  ----------------------------------------------------------

			using (new HEditorUtils.FoldoutScope(AnimBoolQualityTab, out var shouldDraw, "Quality"))
			{
				_qualityTab = shouldDraw;
				if (shouldDraw)
				{
					EditorGUILayout.Space(3f);
					_showTracingArea = EditorGUILayout.BeginFoldoutHeaderGroup(_showTracingArea, "Tracing");
					if (_showTracingArea)
					{
						EditorGUI.indentLevel++;

						RayCount.intValue  = EditorGUILayout.IntSlider(HEditorStyles.RayCount,  RayCount.intValue,  2, 16);
						StepCount.intValue = EditorGUILayout.IntSlider(HEditorStyles.StepCount, StepCount.intValue, 8, 64);
						EditorGUILayout.PropertyField(RefineIntersection, HEditorStyles.RefineIntersection);
						EditorGUILayout.PropertyField(FullResolutionDepth, HEditorStyles.FullResolutionDepth);
							
						EditorGUI.indentLevel--;
					}

					EditorGUILayout.EndFoldoutHeaderGroup();
					EditorGUILayout.Space(5f);
						
					_showRenderingArea = EditorGUILayout.BeginFoldoutHeaderGroup(_showRenderingArea, "Rendering");
					if (_showRenderingArea)
					{
						EditorGUI.indentLevel++;

						EditorGUILayout.PropertyField(Checkerboard,   HEditorStyles.Checkerboard);
						EditorGUILayout.Slider(RenderScale,   0.5f, 1.0f, HEditorStyles.RenderScale);
						RenderScale.floatValue = RenderScale.floatValue.RoundToCeilTail(2);
						if (Mathf.Approximately(RenderScale.floatValue, 1.0f) == false)
						{
							EditorGUI.indentLevel++;

							EditorGUI.indentLevel--;
						}
							
						EditorGUI.indentLevel--;
					}
						
					EditorGUILayout.EndFoldoutHeaderGroup();
					EditorGUILayout.Space(3f);
				}
			}
			
			// -------------------------------------   Denoising  ----------------------------------------------------------

			using (new HEditorUtils.FoldoutScope(AnimBoolDenoisingTab, out var shouldDraw, "Denoising"))
			{
				_denoisingTab = shouldDraw;
				if (shouldDraw)
				{
					EditorGUILayout.Space(3f);
					
					EditorGUILayout.PropertyField(BrightnessClamp, HEditorStyles.BrightnessClamp);
					if ((BrightnessClamp)BrightnessClamp.enumValueIndex == Globals.BrightnessClamp.Manual)
						EditorGUILayout.Slider(MaxValueBrightnessClamp, 1.0f, 30.0f, HEditorStyles.MaxValueBrightnessClamp);
					if ((BrightnessClamp)BrightnessClamp.enumValueIndex == Globals.BrightnessClamp.Automatic)
						EditorGUILayout.Slider(MaxDeviationBrightnessClamp, 1.0f, 5.0f, HEditorStyles.MaxDeviationBrightnessClamp);
					EditorGUILayout.Space(5f);
					
					_showRestirValidationArea = EditorGUILayout.BeginFoldoutHeaderGroup(_showRestirValidationArea, "ReSTIR Validation");
					if (_showRestirValidationArea)
					{
						EditorGUI.indentLevel++;

						EditorGUILayout.PropertyField(HalfStepValidation,          HEditorStyles.HalfStepValidation);
						EditorGUILayout.Space(3f);

						EditorGUILayout.LabelField(new GUIContent("Validation Types:"), HEditorStyles.bold);
						EditorGUILayout.PropertyField(SpatialOcclusionValidation,  HEditorStyles.SpatialOcclusionValidation);
						EditorGUILayout.PropertyField(TemporalLightingValidation,  HEditorStyles.TemporalLightingValidation);
						EditorGUILayout.PropertyField(TemporalOcclusionValidation, HEditorStyles.TemporalOcclusionValidation);
							
						EditorGUI.indentLevel--;
					}

					EditorGUILayout.EndFoldoutHeaderGroup();
					EditorGUILayout.Space(5f);
						
					_showSpatialArea = EditorGUILayout.BeginFoldoutHeaderGroup(_showSpatialArea, "Spatial Filter");
					if (_showSpatialArea)
					{
						EditorGUI.indentLevel++;
						EditorGUILayout.Slider(SpatialRadius,    0.0f, 1.0f, HEditorStyles.SpatialRadius);
						EditorGUILayout.Slider(Adaptivity,      0.0f, 1.0f, HEditorStyles.Adaptivity);
						// SpatialPassCount.intValue  = EditorGUILayout.IntSlider(HEditorStyles.SpatialPassCount,  SpatialPassCount.intValue,  0, 4);
						EditorGUILayout.PropertyField(RecurrentBlur, HEditorStyles.RecurrentBlur);
						EditorGUILayout.PropertyField(FireflySuppression, HEditorStyles.FireflySuppression);
							
						EditorGUI.indentLevel--;
					}
						
					EditorGUILayout.EndFoldoutHeaderGroup();
					EditorGUILayout.Space(3f);
				}
			}
			
			// ------------------------------------- Debug settings ----------------------------------------------------------
			
			
			HEditorUtils.HorizontalLine(1f);
			EditorGUILayout.Space(3f);
			
			//HEditorUtils.DrawClickableLink($"HTrace AO Version: {HNames.HTRACE_AO_VERSION}", HNames.HTRACE_AO_DOCUMENTATION_LINK, true);
			HEditorUtils.DrawLinkRow(
				($"Documentation (v." + HNames.HTRACE_SSGI_VERSION + ")", () => Application.OpenURL(HNames.HTRACE_SSGI_DOCUMENTATION_LINK)),
				("Discord", () => Application.OpenURL(HNames.HTRACE_DISCORD_LINK)),
				("Bug report", () => HBugReporterWindow.ShowWindow())
			);
			
			GUI.backgroundColor = standartBackgroundColor;
			GUI.color           = standartColor;
			
			serializedObject.ApplyModifiedProperties();
		}

		private void WarningsHandle()
		{
			var htraceAoRendererFeature = HRendererURP.GetRendererFeatureByTypeName(nameof(HTraceSSGIRendererFeature)) as HTraceSSGIRendererFeature;
			if (htraceAoRendererFeature == null)
			{
				EditorGUILayout.Space();
				CoreEditorUtils.DrawFixMeBox(NO_RENDERER_FEATURE_MESSAGE, MessageType.Error, HEditorStyles.FixButtonName, () =>
				{
					HRendererURP.AddHTraceRendererFeatureToUniversalRendererData();
					GUIUtility.ExitGUI();
				});
				//EditorGUILayout.HelpBox(NO_RENDERER_FEATURE_MESSAGE, MessageType.Error, wide: true);
				return;
			}
			else if (!htraceAoRendererFeature.isActive)
			{
				EditorGUILayout.Space();
				CoreEditorUtils.DrawFixMeBox(RENDERER_FEATURE_OFF_MESSAGE, MessageType.Warning, HEditorStyles.FixButtonName, () =>
				{
					htraceAoRendererFeature.SetActive(true);
					GUIUtility.ExitGUI();
				});
				EditorGUILayout.Space();
			}
		}

		private void DebugPart()
		{
			using (new HEditorUtils.FoldoutScope(AnimBoolDebugTab, out var shouldDraw, HEditorStyles.DebugContent.text, toggle: EnableDebug))
			{
				_debugTab = shouldDraw;
				if (shouldDraw)
				{
					//EditorGUILayout.PropertyField(EnableDebug, HEditorStyles.OcclusionEnable);

					//if (EnableDebug.boolValue == true)
					{
						EditorGUILayout.PropertyField(HTraceLayer,          HEditorStyles.hTraceLayerContent);
					}
					
					EditorGUILayout.PropertyField(ShowBowels, new GUIContent("Show Bowels"));
					ShowFullDebugLog.boolValue = EditorGUILayout.Toggle(new GUIContent("Show Full Debug Log"), ShowFullDebugLog.boolValue);
					
					TestCheckBox1.boolValue = EditorGUILayout.Toggle(new GUIContent("TestCheckBox1"), TestCheckBox1.boolValue);
					TestCheckBox2.boolValue = EditorGUILayout.Toggle(new GUIContent("TestCheckBox2"), TestCheckBox2.boolValue);
					TestCheckBox3.boolValue = EditorGUILayout.Toggle(new GUIContent("TestCheckBox3"), TestCheckBox3.boolValue);
					
					EditorGUILayout.Space(3);
				}
			}
		}

		private void UpdateStandartStyles()
		{
			HEditorStyles.foldout.fontStyle = FontStyle.Bold;
		}
		
		private void PropertiesRelative()
		{
			GeneralSettings   = serializedObject.FindProperty("GeneralSettings");
			SSGISettings      = serializedObject.FindProperty("SSGISettings");
			DenoisingSettings = serializedObject.FindProperty("DenoisingSettings");
			DebugSettings     = serializedObject.FindProperty("DebugSettings");
			Preset            = serializedObject.FindProperty("_preset");

			// Debug Data
			HTraceLayer      = DebugSettings.FindPropertyRelative("HTraceLayer");
			ShowBowels       = DebugSettings.FindPropertyRelative("ShowBowels");
			ShowFullDebugLog = DebugSettings.FindPropertyRelative("ShowFullDebugLog");
			TestCheckBox1    = DebugSettings.FindPropertyRelative("TestCheckBox1");
			TestCheckBox2    = DebugSettings.FindPropertyRelative("TestCheckBox2");
			TestCheckBox3    = DebugSettings.FindPropertyRelative("TestCheckBox3");
			
			// Global Tab
			DebugMode = GeneralSettings.FindPropertyRelative("DebugMode");
			HBuffer = GeneralSettings.FindPropertyRelative("HBuffer");
			ExcludeReceivingMask = GeneralSettings.FindPropertyRelative("ExcludeReceivingMask");
			ExcludeCastingMask   = GeneralSettings.FindPropertyRelative("ExcludeCastingMask");
			MetallicIndirectFallback = GeneralSettings.FindPropertyRelative("MetallicIndirectFallback");
			AmbientOverride = GeneralSettings.FindPropertyRelative("AmbientOverride");
			Multibounce = GeneralSettings.FindPropertyRelative("Multibounce");
			FallbackType   = GeneralSettings.FindPropertyRelative("FallbackType");
			SkyIntensity   = GeneralSettings.FindPropertyRelative("_skyIntensity");
			ViewBias       = GeneralSettings.FindPropertyRelative("_viewBias");
			NormalBias     = GeneralSettings.FindPropertyRelative("_normalBias");
			SamplingNoise  = GeneralSettings.FindPropertyRelative("_samplingNoise");
			DenoiseFallback = GeneralSettings.FindPropertyRelative("DenoiseFallback");
			
			// Visuals
			BackfaceLighting  = SSGISettings.FindPropertyRelative("_backfaceLighting");
			MaxRayLength  = SSGISettings.FindPropertyRelative("_maxRayLength");
			ThicknessMode = SSGISettings.FindPropertyRelative("ThicknessMode");
			Thickness     = SSGISettings.FindPropertyRelative("_thickness");
			Intensity = SSGISettings.FindPropertyRelative("_intensity");
			Falloff   = SSGISettings.FindPropertyRelative("_falloff");
		
			// Quality tab
			// Tracing
			RayCount            = SSGISettings.FindPropertyRelative("_rayCount");
			StepCount           = SSGISettings.FindPropertyRelative("_stepCount");
			RefineIntersection  = SSGISettings.FindPropertyRelative("RefineIntersection");
			FullResolutionDepth = SSGISettings.FindPropertyRelative("FullResolutionDepth");

			// Rendering
			Checkerboard = SSGISettings.FindPropertyRelative("Checkerboard");
			RenderScale = SSGISettings.FindPropertyRelative("_renderScale");
		
			// Denoising tab
			BrightnessClamp             = DenoisingSettings.FindPropertyRelative("BrightnessClamp");
			MaxValueBrightnessClamp     = DenoisingSettings.FindPropertyRelative("_maxValueBrightnessClamp");
			MaxDeviationBrightnessClamp = DenoisingSettings.FindPropertyRelative("_maxDeviationBrightnessClamp");
		
			// ReSTIR Validation
			HalfStepValidation          = DenoisingSettings.FindPropertyRelative("HalfStepValidation");
			SpatialOcclusionValidation  = DenoisingSettings.FindPropertyRelative("SpatialOcclusionValidation");
			TemporalLightingValidation  = DenoisingSettings.FindPropertyRelative("TemporalLightingValidation");
			TemporalOcclusionValidation = DenoisingSettings.FindPropertyRelative("TemporalOcclusionValidation");
		
			// Spatial
			SpatialRadius      = DenoisingSettings.FindPropertyRelative("_spatialRadius");
			Adaptivity         = DenoisingSettings.FindPropertyRelative("_adaptivity");
			// SpatialPassCount   = DenoisingData.FindPropertyRelative("_spatialPassCount");
			RecurrentBlur      = DenoisingSettings.FindPropertyRelative("RecurrentBlur");
			FireflySuppression = DenoisingSettings.FindPropertyRelative("FireflySuppression");
		}
	}
}
#endif
