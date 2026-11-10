//pipelinedefine
#define H_URP

using System;
using HTraceSSGI.Scripts.Extensions;
using UnityEngine;
using HTraceSSGI.Scripts.Globals;

namespace HTraceSSGI.Scripts.Data.Public
{
	[Serializable]
	public class GeneralSettings
	{
		public DebugMode DebugMode = DebugMode.None;
		public HBuffer   HBuffer   = HBuffer.Multi;
		
		[SerializeField]
		public bool MetallicIndirectFallback = false;
		[SerializeField]
		public bool AmbientOverride = true;
		
		public bool Multibounce = true;
	
#if UNITY_2023_3_OR_NEWER
		public RenderingLayerMask ExcludeCastingMask = 0;
		public RenderingLayerMask ExcludeReceivingMask = 0;
#endif
		
		[SerializeField]
		public FallbackType FallbackType = FallbackType.None;
		
		[SerializeField]
		private float _skyIntensity = 0.5f;
		/// <summary>
		/// 
		/// </summary>
		/// <value>[0.0;1.0]</value>
		[HExtensions.HRangeAttribute(0.0f,1.0f)]
		public float SkyIntensity
		{
			get => _skyIntensity;    
			set
			{
				if (Mathf.Abs(value - _skyIntensity) < Mathf.Epsilon)
					return;

				_skyIntensity = HExtensions.Clamp(value, typeof(GeneralSettings), nameof(GeneralSettings.SkyIntensity));
			}
		}
		
		[SerializeField]
		private float _viewBias = 0.0f;
		/// <summary>
		/// 
		/// </summary>
		/// <value>[0.0;2.0]</value>
		[HExtensions.HRangeAttribute(0.0f,2.0f)]
		public float ViewBias
		{
			get => _viewBias;    
			set
			{
				if (Mathf.Abs(value - _viewBias) < Mathf.Epsilon)
					return;

				_viewBias = HExtensions.Clamp(value, typeof(GeneralSettings), nameof(GeneralSettings.ViewBias));
			}
		}
		
		[SerializeField]
		private float _normalBias = 0.33f;
		/// <summary>
		/// 
		/// </summary>
		/// <value>[0.0;4.0]</value>
		[HExtensions.HRangeAttribute(0.0f,2.0f)]
		public float NormalBias
		{
			get => _normalBias;    
			set
			{
				if (Mathf.Abs(value - _normalBias) < Mathf.Epsilon)
					return;

				_normalBias = HExtensions.Clamp(value, typeof(GeneralSettings), nameof(GeneralSettings.NormalBias));
			}
		}
		
		[SerializeField]
		private float _samplingNoise = 0.1f;
		/// <summary>
		/// 
		/// </summary>
		/// <value>[0.0;1.0]</value>
		[HExtensions.HRangeAttribute(0.0f,1.0f)]
		public float SamplingNoise
		{
			get => _samplingNoise;    
			set
			{
				if (Mathf.Abs(value - _samplingNoise) < Mathf.Epsilon)
					return;

				_samplingNoise = HExtensions.Clamp(value, typeof(GeneralSettings), nameof(GeneralSettings.SamplingNoise));
			}
		}

		[SerializeField]
		private float _intensityMultiplier = 1f;
		/// <summary>
		///
		/// </summary>
		/// <value>[0.0;1.0]</value>
		[HExtensions.HRangeAttribute(0.0f,1.0f)]
		public float IntensityMultiplier
		{
			get => _intensityMultiplier;
			set
			{
				if (Mathf.Abs(value - _intensityMultiplier) < Mathf.Epsilon)
					return;

				_intensityMultiplier = HExtensions.Clamp(value, typeof(GeneralSettings), nameof(GeneralSettings.IntensityMultiplier));
			}
		}
		
		public bool DenoiseFallback = true;
	}
}
