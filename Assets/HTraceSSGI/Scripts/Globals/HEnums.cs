//pipelinedefine
#define H_URP

using UnityEngine.Rendering;

namespace HTraceSSGI.Scripts.Globals
{
	public enum HTraceSSGIPreset
	{
		Performance = 1,
		Optimized = 2,
		Balanced = 3,
		Quality = 4,
	}

	public enum ThicknessMode
	{
		Relative = 0,
		Uniform,
	}
	
	public enum FallbackType
	{
		None = 0,
		Sky = 1,
#if NONE
		APV = 2,
#endif

#if UNITY_6000_0_OR_NEWER
		APV = 2,
#endif
	}

	public enum BrightnessClamp
	{
		Manual = 0,
		Automatic = 1,
	}
	
	public enum ReprojectionFilter
	{
		Linear4Taps = 0,
		Lanczos12Taps  = 1,
	}
	
	public enum AlphaCutout
	{
		Evaluate = 0,
		DepthTest = 1,
	}

	public enum DebugMode
	{
		None                 = 0,
		MainBuffers          = 1,
		DirectLighting       = 2,
		GlobalIllumination   = 3,
		TemporalDisocclusion = 4,
	}

	public enum HBuffer
	{
		Multi,
		Depth,
		Diffuse,
		Normal,
		MotionMask,
		MotionVectors,
	}

	public enum HInjectionPoint
	{
	}

	public enum DebugType
	{
		Log,
		Warning,
		Error,
	}
}
