//pipelinedefine
#define H_URP

using System;
using HTraceSSGI.Scripts.Extensions;
using HTraceSSGI.Scripts.Globals;
using UnityEngine;

namespace HTraceSSGI.Scripts.Data.Public
{
	[Serializable]
	public class SSGISettings
	{
		// ----------------------------------------------- Visuals -----------------------------------------------

		[SerializeField]
		private float _backfaceLighting = 0f;
		/// <summary>
		///
		/// </summary>
		/// <value>[0.0;1.0]</value>
		[HExtensions.HRangeAttribute(0.0f,1.0f)]
		public float BackfaceLighting
		{
			get => _backfaceLighting;
			set
			{
				if (Mathf.Abs(value - _backfaceLighting) < Mathf.Epsilon)
					return;

				_backfaceLighting = HExtensions.Clamp(value, typeof(SSGISettings), nameof(SSGISettings.BackfaceLighting));
			}
		}

		[SerializeField]
		private float _maxRayLength = 100f;
		/// <summary>
		/// 
		/// </summary>
		/// <value>[0.0;infinity]</value>
		[HExtensions.HRangeAttribute(0.0f,float.MaxValue)]
		public float MaxRayLength
		{
			get => _maxRayLength;    
			set
			{
				if (Mathf.Abs(value - _maxRayLength) < Mathf.Epsilon)
					return;

				_maxRayLength = HExtensions.Clamp(value, typeof(SSGISettings), nameof(SSGISettings.MaxRayLength));
			}
		}
		
		public ThicknessMode ThicknessMode = ThicknessMode.Relative;
		
		[SerializeField]
		private float _thickness = 0.35f;
		/// <summary>
		/// 
		/// </summary>
		/// <value>[0.0;1.0]</value>
		[HExtensions.HRangeAttribute(0.0f,1.0f)]
		public float Thickness
		{
			get => _thickness;    
			set
			{
				if (Mathf.Abs(value - _thickness) < Mathf.Epsilon)
					return;

				_thickness = HExtensions.Clamp(value, typeof(SSGISettings), nameof(SSGISettings.Thickness));
			}
		}
		
		[SerializeField]
		private float _intensity = 1f;
		/// <summary>
		/// 
		/// </summary>
		/// <value>[0.1;5.0]</value>
		[HExtensions.HRangeAttribute(0.1f,5.0f)]
		public float Intensity
		{
			get => _intensity;    
			set
			{
				if (Mathf.Abs(value - _intensity) < Mathf.Epsilon)
					return;

				_intensity = HExtensions.Clamp(value, typeof(SSGISettings), nameof(SSGISettings.Intensity));
			}
		}
		
		[SerializeField]
		private float _falloff = 0.0f;
		/// <summary>
		/// 
		/// </summary>
		/// <value>[0.0;1.0]</value>
		[HExtensions.HRangeAttribute(0f,1.0f)]
		public float Falloff
		{
			get => _falloff;    
			set
			{
				if (Mathf.Abs(value - _falloff) < Mathf.Epsilon)
					return;

				_falloff = HExtensions.Clamp(value, typeof(SSGISettings), nameof(SSGISettings.Falloff));
			}
		}
		
		// ----------------------------------------------- Quality -----------------------------------------------
		// ----------------------------------------------- Tracing -----------------------------------------------
		
		[SerializeField]
		private int _rayCount = 4;
		/// <summary>
		/// 
		/// </summary>
		/// <value>[2;16]</value>
		[HExtensions.HRangeAttribute(2,16)]
		public int RayCount
		{
			get
			{
				return _rayCount;
			}
			set
			{
				if (value == _rayCount)
					return;

				_rayCount = HExtensions.Clamp(value, typeof(SSGISettings), nameof(SSGISettings.RayCount));
			}
		}
		
		[SerializeField]
		private int _stepCount = 32;
		/// <summary>
		/// 
		/// </summary>
		/// <value>[8;64]</value>
		[HExtensions.HRangeAttribute(8,128)]
		public int StepCount
		{
			get => _stepCount;    
			set
			{
				if (value == _stepCount)
					return;

				_stepCount = HExtensions.Clamp(value, typeof(SSGISettings), nameof(SSGISettings.StepCount));
			}
		}

		[SerializeField]
		public bool RefineIntersection = true;
		
		[SerializeField]
		public bool FullResolutionDepth = true;

		// ----------------------------------------------- Rendering -----------------------------------------------

		[SerializeField]
		public bool Checkerboard = false;
		
		[SerializeField]
		private float _renderScale = 1f;
		/// <summary>
		/// 
		/// </summary>
		/// <value>[0.5;1.0]</value>
		[HExtensions.HRangeAttribute(0.5f,1.0f)]
		public float RenderScale
		{
			get => _renderScale;    
			set
			{
				if (Mathf.Abs(value - _renderScale) < Mathf.Epsilon)
					return;

				_renderScale = HExtensions.Clamp(value, typeof(SSGISettings), nameof(SSGISettings.RenderScale));
			}
		}
	}
}
