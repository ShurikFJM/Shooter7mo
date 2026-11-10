//pipelinedefine
#define H_URP

using System;
using HTraceSSGI.Scripts.Extensions;
using HTraceSSGI.Scripts.Globals;
using UnityEngine;

namespace HTraceSSGI.Scripts.Data.Public
{
	[Serializable]
	public class DenoisingSettings
	{
		// ----------------------------------------------- DENOISING -----------------------------------------------

		[SerializeField]
		public BrightnessClamp BrightnessClamp = BrightnessClamp.Automatic;
		
		[SerializeField]
		private float _maxValueBrightnessClamp 
			= 1.0f;
		/// <summary>
		/// 
		/// </summary>
		/// <value>[1.0;30.0]</value>
		[HExtensions.HRangeAttribute(1.0f,30.0f)]
		public float MaxValueBrightnessClamp
		{
			get => _maxValueBrightnessClamp;
			set
			{
				if (Mathf.Abs(value - _maxValueBrightnessClamp) < Mathf.Epsilon)
					return;

				_maxValueBrightnessClamp = HExtensions.Clamp(value, typeof(DenoisingSettings), nameof(DenoisingSettings.MaxValueBrightnessClamp));
			}
		}

		[SerializeField]
		private float _maxDeviationBrightnessClamp
			= 3.0f;
		
		
		/// <summary>
		/// 
		/// </summary>
		/// <value>[1.0;5.0]</value>
		[HExtensions.HRangeAttribute(1.0f,5.0f)]
		public float MaxDeviationBrightnessClamp
		{
			get => _maxDeviationBrightnessClamp;
			set
			{
				if (Mathf.Abs(value - _maxDeviationBrightnessClamp) < Mathf.Epsilon)
					return;

				_maxDeviationBrightnessClamp = HExtensions.Clamp(value, typeof(DenoisingSettings), nameof(DenoisingSettings.MaxDeviationBrightnessClamp));
			}
		}
		
		// ----------------------------------------------- ReSTIR Validation -----------------------------------------------
		
		[SerializeField]
		public bool HalfStepValidation = false;
		
		[SerializeField]
		public bool SpatialOcclusionValidation = true;
		
		[SerializeField]
		public bool TemporalLightingValidation = true;
		
		[SerializeField]
		public bool TemporalOcclusionValidation = true;
		
		
		// ----------------------------------------------- Spatial -----------------------------------------------
		
		[SerializeField]
		private float _spatialRadius = 0.6f;
		/// <summary>
		/// 
		/// </summary>
		/// <value>[0.0;1.0]</value>
		[HExtensions.HRangeAttribute(0.0f,1.0f)]
		public float SpatialRadius
		{
			get => _spatialRadius;    
			set
			{
				if (Mathf.Abs(value - _spatialRadius) < Mathf.Epsilon)
					return;

				_spatialRadius = HExtensions.Clamp(value, typeof(DenoisingSettings), nameof(DenoisingSettings.SpatialRadius));
			}
		}
		
		[SerializeField]
		private float _adaptivity = 0.9f;
		/// <summary>
		/// 
		/// </summary>
		/// <value>[0.0;1.0]</value>
		[HExtensions.HRangeAttribute(0.0f,1.0f)]
		public float Adaptivity
		{
			get => _adaptivity;    
			set
			{
				if (Mathf.Abs(value - _adaptivity) < Mathf.Epsilon)
					return;

				_adaptivity = HExtensions.Clamp(value, typeof(DenoisingSettings), nameof(DenoisingSettings.Adaptivity));
			}
		}
		
		[SerializeField]
		public bool RecurrentBlur = false;
		
		[SerializeField]
		public bool FireflySuppression = true;
	}
}
