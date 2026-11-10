//pipelinedefine
#define H_URP

using System;
using HTraceSSGI.Scripts.Data.Private;
using HTraceSSGI.Scripts.Data.Public;
using HTraceSSGI.Scripts.Globals;
using UnityEngine;
using UnityEngine.Rendering;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace HTraceSSGI.Scripts.Infrastructure.URP
{
	[ExecuteAlways]
	public class HAmbientOverrideVolume: MonoBehaviour
	{
		private Volume _volumeComponent;
#if UNITY_6000_0_OR_NEWER
		private ProbeVolumesOptions _probeVolumesOptionsOverrideComponent;
#endif

		private static HAmbientOverrideVolume s_instance;

		public static HAmbientOverrideVolume Instance
		{
			get
			{
				if (s_instance == null)
				{
					s_instance = FindObjectOfType<HAmbientOverrideVolume>();

					if (s_instance == null)
					{
						GameObject singletonObject = new GameObject($"HTrace AmbientOverride");
						s_instance = singletonObject.AddComponent<HAmbientOverrideVolume>();

						if (Application.isPlaying)
							DontDestroyOnLoad(singletonObject);
					}
				}

				return s_instance;
			}
		}

		public void SetActiveVolume(bool isActive)
		{
			_volumeComponent.enabled = isActive;
#if UNITY_6000_0_OR_NEWER
			_probeVolumesOptionsOverrideComponent.active = isActive;
#endif
		}

		private void Awake()
		{
			InitializeSingleton();
			SetupVolumeURP();
		}

		private void InitializeSingleton()
		{
			if (s_instance == null)
			{
				s_instance = this;
				if (Application.isPlaying)
					DontDestroyOnLoad(gameObject);
			}
			else if (s_instance != this)
			{
				if (Application.isPlaying)
					Destroy(gameObject);
				else
					DestroyImmediate(gameObject);
			}
		}

#if UNITY_EDITOR
		private void OnValidate()
		{
			HTraceSSGIProfile profile = HTraceSSGISettings.ActiveProfile;
			if (!EditorApplication.isPlayingOrWillChangePlaymode)
				SetupVolumeURP();

			gameObject.hideFlags = HideFlags.HideAndDontSave;
			if (s_instance != null && profile != null && profile.DebugSettings != null)
				s_instance.gameObject.hideFlags = profile.DebugSettings.ShowBowels ? HideFlags.None : HideFlags.HideAndDontSave;
		}
#endif

		private void OnDestroy()
		{
			if (s_instance == this)
			{
				s_instance = null;
			}
		}

		private void SetupVolumeURP()
		{
#if UNITY_6000_0_OR_NEWER
			CreateSSGIOverrideComponent();
			ApplySSGIOverrideComponentSettings();
			ChangeObjectWithSerialization_ONLYEDITOR();
#endif
		}

#if UNITY_6000_0_OR_NEWER
		private void CreateSSGIOverrideComponent()
		{
			HTraceSSGIProfile profile = HTraceSSGISettings.ActiveProfile;
			if (profile != null && profile != null && profile.DebugSettings != null)
				gameObject.hideFlags = profile.DebugSettings.ShowBowels ? HideFlags.None : HideFlags.HideAndDontSave;

			_volumeComponent = gameObject.GetComponent<Volume>();
			if (_volumeComponent == null)
			{
				_volumeComponent = gameObject.AddComponent<Volume>();
				_volumeComponent.enabled = false;
			}

			if (_volumeComponent.sharedProfile == null)
			{
				//We can't crate it in runtime, because after build it will break.
				//it will call only in editor, but if someone changes it in runtime, we will override.
				_volumeComponent.sharedProfile = Resources.Load<VolumeProfile>($"{HNames.ASSET_NAME}/HTraceSSGI Volume Profile URP");
			}

			if (_probeVolumesOptionsOverrideComponent == null) 
				_volumeComponent.sharedProfile.TryGet(out _probeVolumesOptionsOverrideComponent);
		}

		private void ApplySSGIOverrideComponentSettings()
		{
			_volumeComponent.weight   = 1;
			_volumeComponent.priority = 100;
#if UNITY_EDITOR
			_volumeComponent.runInEditMode = true;
#endif
			if (_probeVolumesOptionsOverrideComponent != null)
			{
				_probeVolumesOptionsOverrideComponent.normalBias.overrideState    = true;
				_probeVolumesOptionsOverrideComponent.viewBias.overrideState      = true;
				_probeVolumesOptionsOverrideComponent.samplingNoise.overrideState = true;
			}
		}

		private void ChangeObjectWithSerialization_ONLYEDITOR()
		{
#if UNITY_EDITOR
			if (_probeVolumesOptionsOverrideComponent == null)
				return;

			SerializedObject probeVolumesOptionsObject = new SerializedObject(_probeVolumesOptionsOverrideComponent);

			var normalBias = probeVolumesOptionsObject.FindProperty("normalBias");
			var m_OverrideState_normalBias = normalBias.FindPropertyRelative("m_OverrideState");
			var m_Value_normalBias = normalBias.FindPropertyRelative("m_Value");
			m_OverrideState_normalBias.boolValue = true;
			m_Value_normalBias.floatValue = 0.0f;

			var viewBias = probeVolumesOptionsObject.FindProperty("viewBias");
			var m_OverrideState_viewBias = viewBias.FindPropertyRelative("m_OverrideState");
			var m_Value_viewBias = viewBias.FindPropertyRelative("m_Value");
			m_OverrideState_viewBias.boolValue = true;
			m_Value_viewBias.floatValue = 0.0f;

			var samplingNoise = probeVolumesOptionsObject.FindProperty("samplingNoise");
			var m_OverrideState_samplingNoise = samplingNoise.FindPropertyRelative("m_OverrideState");
			var m_Value_samplingNoise = samplingNoise.FindPropertyRelative("m_Value");
			m_OverrideState_samplingNoise.boolValue = true;
			m_Value_samplingNoise.floatValue = 0.0f;

			probeVolumesOptionsObject.ApplyModifiedProperties();
#endif
		}
#endif
	}
}
