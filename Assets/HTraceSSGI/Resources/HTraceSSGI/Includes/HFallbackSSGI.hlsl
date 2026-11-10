//pipelinedefine
#define H_URP


#if UNITY_VERSION >= 600000
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/AmbientProbe.hlsl"
#include "Packages/com.unity.render-pipelines.core/Runtime/Lighting/ProbeVolume/ProbeVolume.hlsl"

float3 EvaluateFallbackSky(float3 Direction)
{
    float3 Sky = SHEvalLinearL0L1(Direction, H_SHAr, H_SHAg, H_SHAb);
    Sky += SHEvalLinearL2(Direction, H_SHBr, H_SHBg, H_SHBb, H_SHC);
    
    return Sky;
}

float3 EvaluateFallbackAPV(float4 APVParams, float3 PositionWS, float3 NormalWS, float3 ViewDirection, float2 pixCoord, uint RenderingLayer)
{
    float3 BakedAPV = 0;
    float3 SamplingOffset = 0;
#ifdef UNITY_SPACE_TRANSFORMS_INCLUDED
    float SamplingNoise = InterleavedGradientNoise(pixCoord, _APVFrameIndex);
    float3 SamplingDirection = ViewDirection;
    SamplingDirection += mul((float3x3)GetViewToWorldMatrix(), float3(0.0, 1.0, 0.0)) * (frac(SamplingNoise * 100.0) - 0.5) + mul((float3x3)GetViewToWorldMatrix(), float3(1.0, 0.0, 0.0)) * (frac(SamplingNoise * 1000.0) - 0.5);
    SamplingOffset = APVParams.z > 0 ? SamplingNoise * APVParams.z * SamplingDirection : 0;
#endif

    PositionWS += SamplingOffset;
    PositionWS = (PositionWS + NormalWS * APVParams.x) + ViewDirection * APVParams.y;
    // Preserve the second noise offset previously applied inside EvaluateAdaptiveProbeVolume.
    PositionWS += SamplingOffset;
    
    APVSample ProbeSample = SampleAPV(PositionWS, NormalWS, RenderingLayer, ViewDirection);
    if (ProbeSample.status != APV_SAMPLE_STATUS_INVALID)
    {
        ProbeSample.Decode();
#if defined(PROBE_VOLUMES_L1)
        EvaluateAPVL1(ProbeSample, NormalWS, BakedAPV);
#elif defined(PROBE_VOLUMES_L2)
        EvaluateAPVL1L2(ProbeSample, NormalWS, BakedAPV);
#endif
        BakedAPV += ProbeSample.L0;

        if (_APVSkyOcclusionWeight > 0)
        {
            float3 SkyDirection = NormalWS;
            if (_APVSkyDirectionWeight > 0)
            {
                SkyDirection = ProbeSample.skyShadingDirection;
                float DirectionLengthSquared = dot(SkyDirection, SkyDirection);
                SkyDirection = DirectionLengthSquared < 0.2f ? NormalWS : SkyDirection * rsqrt(DirectionLengthSquared);
            }

            BakedAPV += EvalSHSkyOcclusion(NormalWS, ProbeSample) * EvaluateFallbackSky(SkyDirection);
        }

        BakedAPV *= _APVWeight;
    }
    else
    {
        BakedAPV = EvaluateFallbackSky(NormalWS);
    }

    #ifdef FALLBACK_STAGE
    BakedAPV *= APVParams.w;
    #endif
    
    return BakedAPV;
}
#else
#include "Packages/com.unity.render-pipelines.core/Runtime/Lighting/ProbeVolume/ProbeVolumeBlendStates.hlsl"
#include "Packages/com.unity.render-pipelines.core/Runtime/Lighting/ProbeVolume/ProbeVolume.hlsl"

uint _EnableProbeVolumes;

float3 EvaluateFallbackAPV(float4 APVParams, float3 PositionWS, float3 NormalWS, float3 ViewDirection, int2 pixCoord, uint RenderingLayer)
{
    return 0;
}

float3 EvaluateFallbackSky(float3 Direction)
{   
    float3 Sky = SHEvalLinearL0L1(Direction, H_SHAr, H_SHAg, H_SHAb);
    Sky += SHEvalLinearL2(Direction, H_SHBr, H_SHBg, H_SHBb, H_SHC);

    return max(Sky, 0);
}
#endif



