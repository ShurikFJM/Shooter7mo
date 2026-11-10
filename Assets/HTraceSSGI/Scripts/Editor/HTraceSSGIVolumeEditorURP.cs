//pipelinedefine
#define H_URP

using System;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

#if UNITY_EDITOR
using HTraceSSGI.Scripts.Data.Private;
using HTraceSSGI.Scripts.Data.Public;
using HTraceSSGI.Scripts.Editor.WindowsAndMenu;
using HTraceSSGI.Scripts.Globals;
using HTraceSSGI.Scripts.Infrastructure.URP;
using UnityEditor;
using UnityEditor.Rendering;
using UnityEditor.AnimatedValues;

namespace HTraceSSGI.Scripts.Editor
{
	[CanEditMultipleObjects]
#if UNITY_2022_2_OR_NEWER
	[CustomEditor(typeof(HTraceSSGIVolume))]
#else
[VolumeComponentEditor(typeof(HTraceSSGIVolume))]
#endif
	internal class HTraceSSGIVolumeEditorURP : VolumeComponentEditor
	{
		private const string NO_RENDERER_FEATURE_MESSAGE  = "HTrace Screen Space Global Illumination feature is missing in the active URP renderer.";
		private const string RENDERER_FEATURE_OFF_MESSAGE = "HTrace Screen Space Global Illumination is disabled in the active URP renderer.";
		private const string LIGHTING_MULTIPLIER_ABOVE_1_MESSAGE = "Ambient Override may not work correctly when the Environment Lighting Multiplier is set above 1 !";
		private const string RENDETIN_LIGHTING_SETTINGS_WINDOW_PATH = "Window/Rendering/Lighting";

		private Texture2D m_Icon;

		SerializedDataParameter p_Enable;

		// General
		internal SerializedDataParameter p_DebugMode;
		internal SerializedDataParameter p_HBuffer;
		internal SerializedDataParameter p_ExcludeCastingMask;
		internal SerializedDataParameter p_ExcludeReceivingMask;
		internal SerializedDataParameter p_FallbackType;
		internal SerializedDataParameter p_SkyIntensity;
		internal SerializedDataParameter p_MetallicIndirectFallback;
		internal SerializedDataParameter p_AmbientOverride;
		internal SerializedDataParameter  p_Multibounce;

		//Apv
		internal SerializedDataParameter          p_ViewBias;
		internal SerializedDataParameter          p_NormalBias;
		internal SerializedDataParameter          p_SamplingNoise;
		internal SerializedDataParameter          p_IntensityMultiplier;
		internal SerializedDataParameter p_DenoiseFallback;

		// Visuals
		internal SerializedDataParameter p_BackfaceLighting;
		internal SerializedDataParameter          p_MaxRayLength;
		internal SerializedDataParameter          p_ThicknessMode;
		internal SerializedDataParameter          p_Thickness;
		internal SerializedDataParameter          p_Intensity;
		internal SerializedDataParameter          p_Falloff;
		
		// Quality tab
		// Tracing
		internal SerializedDataParameter p_RayCount;
		internal SerializedDataParameter p_StepCount;
		internal SerializedDataParameter p_RefineIntersection;
		internal SerializedDataParameter p_FullResolutionDepth;

		// Rendering
		internal SerializedDataParameter p_Checkerboard;
		internal SerializedDataParameter p_RenderScale;
		
		// Denoising tab
		internal SerializedDataParameter p_BrightnessClamp;
		internal SerializedDataParameter p_MaxValueBrightnessClamp;
		internal SerializedDataParameter p_MaxDeviationBrightnessClamp;
		
		// Temporal
		internal SerializedDataParameter p_HalfStepValidation;
		internal SerializedDataParameter p_SpatialOcclusionValidation;
		internal SerializedDataParameter p_TemporalLightingValidation;
		internal SerializedDataParameter p_TemporalOcclusionValidation;
		
		// Spatial Filter
		internal SerializedDataParameter p_SpatialRadius;
		internal SerializedDataParameter p_Adaptivity;
		internal SerializedDataParameter p_RecurrentBlur;
		internal SerializedDataParameter p_FireflySuppression;

		//Debug
		internal SerializedDataParameter p_ShowBowels;

		// Main foldout groups
		private AnimBool AnimBoolGeneralTab;
		private AnimBool AnimBoolQualityTab;
		private AnimBool AnimBoolDenoisingTab;
		private AnimBool AnimBoolDebugTab;
		private AnimBool AnimBoolEMPTY;

		// Menu state
		private bool _showPipelineIntegration = true;
		private bool _showVisualsArea = true;
		private bool _showTracingArea = true;
		private bool _showRenderingArea = true;
		private bool _showRestirValidationArea = true;
		private bool _showSpatialArea = true;
		
		private SerializedProperty _preset;
		
		public override void OnEnable()
		{
			var o = new PropertyFetcher<HTraceSSGIVolume>(serializedObject);

			m_Icon = Resources.Load<Texture2D>("SSGI UI Card");
			_preset = serializedObject.FindProperty("Preset");

			p_Enable = Unpack(o.Find(x => x.Enable));
			// General Settings
			p_DebugMode = Unpack(o.Find(x => x.DebugMode));
			p_HBuffer = Unpack(o.Find(x => x.HBuffer));
#if UNITY_2023_3_OR_NEWER
			p_ExcludeReceivingMask = Unpack(o.Find(x => x.ExcludeReceivingMask));
			p_ExcludeCastingMask = Unpack(o.Find(x => x.ExcludeCastingMask));
#endif
			p_FallbackType = Unpack(o.Find(x => x.FallbackType));
			p_SkyIntensity = Unpack(o.Find(x => x.SkyIntensity));
			//Pipeline integration
			p_MetallicIndirectFallback = Unpack(o.Find(x => x.MetallicIndirectFallback));
			p_AmbientOverride = Unpack(o.Find(x => x.AmbientOverride));
			p_Multibounce = Unpack(o.Find(x => x.Multibounce));
			//Apv
			p_ViewBias = Unpack(o.Find(x => x.ViewBias));
			p_NormalBias = Unpack(o.Find(x => x.NormalBias));
			p_SamplingNoise = Unpack(o.Find(x => x.SamplingNoise));
			p_IntensityMultiplier = Unpack(o.Find(x => x.IntensityMultiplier));
			p_DenoiseFallback = Unpack(o.Find(x => x.DenoiseFallback));

			// Visuals
			p_BackfaceLighting = Unpack(o.Find(x => x.BackfaceLighting));
			p_MaxRayLength = Unpack(o.Find(x => x.MaxRayLength));
			p_ThicknessMode = Unpack(o.Find(x => x.ThicknessMode));
			p_Thickness = Unpack(o.Find(x => x.Thickness));
			p_Intensity = Unpack(o.Find(x => x.Intensity));
			p_Falloff = Unpack(o.Find(x => x.Falloff));

			// Quality tab
			// Tracing
			p_RayCount = Unpack(o.Find(x => x.RayCount));
			p_StepCount = Unpack(o.Find(x => x.StepCount));
			p_RefineIntersection = Unpack(o.Find(x => x.RefineIntersection));
			p_FullResolutionDepth = Unpack(o.Find(x => x.FullResolutionDepth));

			// Rendering
			p_Checkerboard = Unpack(o.Find(x => x.Checkerboard));
			p_RenderScale = Unpack(o.Find(x => x.RenderScale));

			// Denoising tab
			p_BrightnessClamp = Unpack(o.Find(x => x.BrightnessClamp));
			p_MaxValueBrightnessClamp = Unpack(o.Find(x => x.MaxValueBrightnessClamp));
			p_MaxDeviationBrightnessClamp = Unpack(o.Find(x => x.MaxDeviationBrightnessClamp));

			// Temporal
			p_HalfStepValidation = Unpack(o.Find(x => x.HalfStepValidation));
			p_SpatialOcclusionValidation = Unpack(o.Find(x => x.SpatialOcclusionValidation));
			p_TemporalLightingValidation = Unpack(o.Find(x => x.TemporalLightingValidation));
			p_TemporalOcclusionValidation = Unpack(o.Find(x => x.TemporalOcclusionValidation));

			// Spatial Filter
			p_SpatialRadius = Unpack(o.Find(x => x.SpatialRadius));
			p_Adaptivity = Unpack(o.Find(x => x.Adaptivity));
			p_RecurrentBlur = Unpack(o.Find(x => x.RecurrentBlur));
			p_FireflySuppression = Unpack(o.Find(x => x.FireflySuppression));

			// Debug
			p_ShowBowels = Unpack(o.Find(x => x.ShowBowels));

			AnimBoolGeneralTab = new AnimBool(true);
			AnimBoolGeneralTab.valueChanged.RemoveAllListeners();
			AnimBoolGeneralTab.valueChanged.AddListener(Repaint);
			
			AnimBoolQualityTab = new AnimBool(true);
			AnimBoolQualityTab.valueChanged.RemoveAllListeners();
			AnimBoolQualityTab.valueChanged.AddListener(Repaint);
			
			AnimBoolDenoisingTab = new AnimBool(true);
			AnimBoolDenoisingTab.valueChanged.RemoveAllListeners();
			AnimBoolDenoisingTab.valueChanged.AddListener(Repaint);

			AnimBoolDebugTab = new AnimBool(true); //_debugTab.boolValue
			AnimBoolDebugTab.valueChanged.RemoveAllListeners();
			AnimBoolDebugTab.valueChanged.AddListener(Repaint);

			AnimBoolEMPTY = new AnimBool(false);
		}

		public override void OnInspectorGUI()
		{
			var hTraceRendererFeature = HRendererURP.GetRendererFeatureByTypeName(nameof(HTraceSSGIRendererFeature)) as HTraceSSGIRendererFeature;
			if (hTraceRendererFeature == null)
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
			else if (!hTraceRendererFeature.isActive)
			{
				EditorGUILayout.Space();
				CoreEditorUtils.DrawFixMeBox(RENDERER_FEATURE_OFF_MESSAGE, MessageType.Warning, HEditorStyles.FixButtonName, () =>
				{
					hTraceRendererFeature.SetActive(true);
					GUIUtility.ExitGUI();
				});
				EditorGUILayout.Space();
			}

			if (m_Icon != null)
			{
				//GUILayout.Label(m_Icon, HEditorStyles.icon, GUILayout.ExpandWidth(false));
				Rect rect = GUILayoutUtility.GetAspectRect((float)m_Icon.width / m_Icon.height);
				rect.xMin += 4;
				rect.xMax -= 4;
				GUI.DrawTexture(rect, m_Icon, ScaleMode.ScaleToFit);
				EditorGUILayout.Space(5f);
			}

			if (HTraceSSGIRendererFeature.IsUseVolumes == false)
			{
				CoreEditorUtils.DrawFixMeBox("\"Use Volumes\" checkbox in the HTrace SSGI Renderer feature is disabled, use the HTraceSSGI component in your scenes.", MessageType.Warning, HEditorStyles.ChangeButtonName, () =>
				{
					hTraceRendererFeature.UseVolumes = true;
					GUIUtility.ExitGUI();
				});
				return;
			}
			
			PropertyField(p_Enable);
			EditorGUILayout.Space(5f);
			
			// ------------------------------------- Global settings ----------------------------------------------------------
			
			using (new HEditorUtils.FoldoutScope(AnimBoolGeneralTab, out var shouldDraw, HEditorStyles.GlobalSettings.text))
			{
				if (shouldDraw)
				{

					using (new IndentLevelScope(10))
					{
						EditorGUILayout.BeginHorizontal();
						EditorGUILayout.PrefixLabel("Preset");

						HTraceSSGIPreset selectedPreset = _preset != null ? (HTraceSSGIPreset)_preset.intValue : HTraceSSGIPreset.Balanced;
						EditorGUI.BeginChangeCheck();
						selectedPreset = (HTraceSSGIPreset)EditorGUILayout.EnumPopup(selectedPreset);
						if (EditorGUI.EndChangeCheck() && _preset != null)
						{
							_preset.intValue = (int)selectedPreset;
							serializedObject.ApplyModifiedProperties();
						}

						if (GUILayout.Button("Apply", GUILayout.Width(60)))
						{
							if (_preset != null)
								_preset.intValue = (int)selectedPreset;
							HTraceSSGIPresetData.ApplyPresetVolume(this, selectedPreset);
							serializedObject.ApplyModifiedProperties();
							EditorUtility.SetDirty(target);
						}
						EditorGUILayout.EndHorizontal();
					}
					
					EditorGUILayout.Space(5f);
					
					PropertyField(p_DebugMode);
					if((DebugMode)p_DebugMode.value.enumValueIndex == DebugMode.MainBuffers)
					{
						PropertyField(p_HBuffer);
					}
					EditorGUILayout.Space(5f);

#if UNITY_2023_3_OR_NEWER
					PropertyField(p_ExcludeCastingMask);
					PropertyField(p_ExcludeReceivingMask);
#endif
					EditorGUILayout.Space(3f);

					PropertyField(p_FallbackType);
					if ((Globals.FallbackType)p_FallbackType.value.enumValueIndex == Globals.FallbackType.Sky)
						PropertyField(p_SkyIntensity);

#if UNITY_6000_0_OR_NEWER
					if ((FallbackType)p_FallbackType.value.enumValueIndex == Globals.FallbackType.APV)
					{
						using (new IndentLevelScope())
						{
							PropertyField(p_ViewBias);
							PropertyField(p_NormalBias);
							PropertyField(p_SamplingNoise);
							PropertyField(p_IntensityMultiplier);
							PropertyField(p_DenoiseFallback);
						}
					}
					if ((FallbackType)p_FallbackType.value.enumValueIndex == Globals.FallbackType.Sky)
					{
						using (new IndentLevelScope())
							PropertyField(p_DenoiseFallback);
					}
#endif
					EditorGUILayout.Space(5f);
					{
						_showPipelineIntegration = EditorGUILayout.BeginFoldoutHeaderGroup(_showPipelineIntegration, HEditorStyles.PipelineIntegration.text);
						if (_showPipelineIntegration)
						{
							using (new IndentLevelScope())
							{
								PropertyField(p_MetallicIndirectFallback);
								PropertyField(p_AmbientOverride);
								if (RenderSettings.ambientIntensity > 1.0f && p_AmbientOverride.value.boolValue == true)
								{
									CoreEditorUtils.DrawFixMeBox(LIGHTING_MULTIPLIER_ABOVE_1_MESSAGE, MessageType.Warning, HEditorStyles.OpenButtonName, () =>
									{
										EditorApplication.ExecuteMenuItem(RENDETIN_LIGHTING_SETTINGS_WINDOW_PATH);
										GUIUtility.ExitGUI();
									});
								}
								PropertyField(p_Multibounce);
							}
							EditorGUILayout.Space(3f);
						}
						EditorGUILayout.EndFoldoutHeaderGroup();
					}
					EditorGUILayout.Space(3f);
					
					{
						_showVisualsArea = EditorGUILayout.BeginFoldoutHeaderGroup(_showVisualsArea, HEditorStyles.Visuals.text);
						if (_showVisualsArea)
						{
							using (new IndentLevelScope())
							{
								PropertyField(p_BackfaceLighting);
								PropertyField(p_MaxRayLength);
								if (p_MaxRayLength.value.floatValue < 0)
									p_MaxRayLength.value.floatValue = 0f;
								PropertyField(p_ThicknessMode);
								PropertyField(p_Thickness);
								PropertyField(p_Intensity);
								PropertyField(p_Falloff);
							}
						}
						
						EditorGUILayout.EndFoldoutHeaderGroup();
						EditorGUILayout.Space(3f);
					}
				}
			}
			
			// -------------------------------------   Quality  ----------------------------------------------------------

			using (new HEditorUtils.FoldoutScope(AnimBoolQualityTab, out var shouldDraw, HEditorStyles.Quality.text))
			{
				if (shouldDraw)
				{
					EditorGUILayout.Space(3f);
					_showTracingArea = EditorGUILayout.BeginFoldoutHeaderGroup(_showTracingArea, HEditorStyles.Tracing.text);
					if (_showTracingArea)
					{
						using (new IndentLevelScope())
						{
							PropertyField(p_RayCount);
							PropertyField(p_StepCount);
							PropertyField(p_RefineIntersection);
							PropertyField(p_FullResolutionDepth);
						}
					}

					EditorGUILayout.EndFoldoutHeaderGroup();
					EditorGUILayout.Space(5f);
						
					_showRenderingArea = EditorGUILayout.BeginFoldoutHeaderGroup(_showRenderingArea, HEditorStyles.Rendering.text);
					if (_showRenderingArea)
					{
						using (new IndentLevelScope())
						{
							PropertyField(p_Checkerboard);
							PropertyField(p_RenderScale);
							p_RenderScale.value.floatValue = p_RenderScale.value.floatValue.RoundToCeilTail(2);
						}
					}
						
					EditorGUILayout.EndFoldoutHeaderGroup();
					EditorGUILayout.Space(3f);
				}
			}
			
			// -------------------------------------   Denoising  ----------------------------------------------------------

			using (new HEditorUtils.FoldoutScope(AnimBoolDenoisingTab, out var shouldDraw, HEditorStyles.Denoising.text))
			{
				if (shouldDraw)
				{
					EditorGUILayout.Space(3f);

					PropertyField(p_BrightnessClamp);
					if ((BrightnessClamp)p_BrightnessClamp.value.enumValueIndex == Globals.BrightnessClamp.Manual)
						PropertyField(p_MaxValueBrightnessClamp);
					if ((BrightnessClamp)p_BrightnessClamp.value.enumValueIndex == Globals.BrightnessClamp.Automatic)
						PropertyField(p_MaxDeviationBrightnessClamp);
					EditorGUILayout.Space(5f);
					
					_showRestirValidationArea = EditorGUILayout.BeginFoldoutHeaderGroup(_showRestirValidationArea, HEditorStyles.ReSTIRValidation.text);
					if (_showRestirValidationArea)
					{
						using (new IndentLevelScope())
						{
							PropertyField(p_HalfStepValidation);
							EditorGUILayout.Space(3f);

							EditorGUILayout.LabelField(HEditorStyles.ValidationTypes, HEditorStyles.bold);
							PropertyField(p_SpatialOcclusionValidation);
							PropertyField(p_TemporalLightingValidation);
							PropertyField(p_TemporalOcclusionValidation);
						}
					}

					EditorGUILayout.EndFoldoutHeaderGroup();
					EditorGUILayout.Space(5f);
						
					_showSpatialArea = EditorGUILayout.BeginFoldoutHeaderGroup(_showSpatialArea, HEditorStyles.SpatialFilter.text);
					if (_showSpatialArea)
					{
						using (new IndentLevelScope())
						{
							PropertyField(p_SpatialRadius);
							PropertyField(p_Adaptivity);
							PropertyField(p_RecurrentBlur);
							PropertyField(p_FireflySuppression);
						}
					}
						
					EditorGUILayout.EndFoldoutHeaderGroup();
					EditorGUILayout.Space(3f);
				}
			}


			HEditorUtils.HorizontalLine(1f);
			EditorGUILayout.Space(3f);

			//HEditorUtils.DrawClickableLink($"HTrace AO Version: {HNames.HTRACE_AO_VERSION}", HNames.HTRACE_AO_DOCUMENTATION_LINK, true);
			HEditorUtils.DrawLinkRow(
				($"Documentation (v." + HNames.HTRACE_SSGI_VERSION + ")", () => Application.OpenURL(HNames.HTRACE_SSGI_DOCUMENTATION_LINK)),
				("Discord", () => Application.OpenURL(HNames.HTRACE_DISCORD_LINK)),
				("Bug report", () => HBugReporterWindow.ShowWindow())
			);
		}

		private void DebugPart()
		{
			using (new HEditorUtils.FoldoutScope(AnimBoolDebugTab, out var shouldDraw, HEditorStyles.Debug.text))
			{
				if (shouldDraw)
				{
					EditorGUILayout.Space(3f);

					PropertyField(p_ShowBowels);

					EditorGUILayout.Space(3f);
				}
			}
		}
	}
}
#endif //UNITY_EDITOR
