#ifdef FULL_RESOLUTION_DEPTH
#define MAX_DEPTH_MIP_LEVEL 0
#else
#define MAX_DEPTH_MIP_LEVEL 1
#endif

#include "HReservoirSSGI.hlsl"

uint _StepCount;
float _RayLength;
float _BackfaceLighting;
float4 _ThicknessParams;

H_TEXTURE(_Color);


float GetRayOriginAndDirectionNDC(float MaxLength, float Depth, float2 pixCoordNDC, float3 PositionWS, float3 RayDirectionWS, float3 NormalWS, inout float3 RayOriginBiasedWS, inout float3 RayStartPositionNDC, inout float3 RayEndPositionNDC)
{
    // Bias ray origin in world space
    {
        RayOriginBiasedWS = PositionWS;
        float3 NormalForBias = dot(NormalWS, RayDirectionWS) < 0 ? -NormalWS : NormalWS;

        // Calculate normal bias
        float2 CornerCoordNDC = pixCoordNDC + 0.5f * _ScreenSize.zw;
        float3 CornerPositionWS = H_COMPUTE_POSITION_WS(CornerCoordNDC.xy, Depth, H_MATRIX_I_VP);
        float NormalBias = abs(dot(CornerPositionWS - PositionWS, NormalForBias)) * 2.0f;

        // This can push the ray origin off-screen causing black pixels on the border
        RayOriginBiasedWS += NormalForBias * max(NormalBias, 0.01f) + RayDirectionWS * 0.01f;
    }

    // Calculate ray start position in screen space
    RayStartPositionNDC = H_COMPUTE_NDC_Z(RayOriginBiasedWS, H_MATRIX_VP).xyz;

    // Calculate ray end clipped position in screen space
    {
        // Calculate clipped ray distance in world space
        float MaxRayDistanceWS = MaxLength;
        
        float3 RayDirectionVS = H_TRANSFORM_WORLD_TO_VIEW_DIR(-RayDirectionWS, true);
        float SceneDepth = H_LINEAR_EYE_DEPTH(RayOriginBiasedWS, H_MATRIX_V);
        float RayClippedDistanceWS = RayDirectionVS.z < 0.0 ? min(-0.99f * SceneDepth / RayDirectionVS.z, MaxRayDistanceWS) : MaxRayDistanceWS;
        
        // Calculate ray end position in screen space
        RayEndPositionNDC.xyz = H_COMPUTE_NDC_Z(RayOriginBiasedWS + RayDirectionWS * RayClippedDistanceWS, H_MATRIX_VP).xyz;

        // Recalculate ray end position where it leaves the screen
        float2 ScreenEdgeIntersections = HLineBoxIntersect(RayStartPositionNDC, RayEndPositionNDC, 0, 1);
        RayEndPositionNDC = RayStartPositionNDC + (RayEndPositionNDC - RayStartPositionNDC) * ScreenEdgeIntersections.y;

        return ScreenEdgeIntersections.y;
    }
}

float2 GetWorkingDepth(float SurfaceDepth, float RayDepth)
{
    #ifdef LINEAR_THICKNESS
    return float2(SurfaceDepth, RayDepth);
    #else
    return float2(H_LINEAR_EYE_DEPTH(SurfaceDepth), H_LINEAR_EYE_DEPTH(RayDepth));
    #endif
}

bool WorkingDepthCompare(float SurfaceDepth, float RayDepth, float2 ThicknessParams)
{
    #ifdef LINEAR_THICKNESS
    return RayDepth > SurfaceDepth * ThicknessParams.x + ThicknessParams.y; 
    #else
    return RayDepth < SurfaceDepth * ThicknessParams.x + ThicknessParams.y;
    #endif
}


float4 RayMarch(float3 RayStartPositionNDC, float3 RayEndPositionNDC, float StepJitter)
{
    int StepCount = _StepCount;
    
    bool HitFound = false;
    bool SkyPassed = false;
    
    float3 HitCoordNDC = 0;
    float3 PreviousPositionNDC = RayStartPositionNDC;
    
    for (int i = 0; i < StepCount; i++)
    {
        float3 SamplePositionNDC = lerp(RayStartPositionNDC, RayEndPositionNDC, pow(float(min(float(StepCount), i + StepJitter * 1)) / float(StepCount), 2));
        
        uint DepthLOD = lerp(MAX_DEPTH_MIP_LEVEL, 4, (float(i)) / StepCount); 
        float SurfaceDepth = H_SAMPLE_LOD(g_HTraceDepthPyramidSSGI, H_SAMPLER_POINT_CLAMP, SamplePositionNDC.xy * HRenderScale.xy, DepthLOD).x;
    
        if (SurfaceDepth <= 0)
        {
            SkyPassed = true;
        }
        
        if (SamplePositionNDC.z < SurfaceDepth)
        {
            float2 WorkingDepth = GetWorkingDepth(SurfaceDepth, SamplePositionNDC.z);
            
            if (WorkingDepthCompare(WorkingDepth.x, WorkingDepth.y, _ThicknessParams.xy))
            {
                HitCoordNDC = SamplePositionNDC.xyz;
                HitFound = true;
                break;  
            }
            if (REFINE_INTERSECTION && WorkingDepthCompare(WorkingDepth.x, WorkingDepth.y, _ThicknessParams.zw))
            {
                float3 MiddlePositionNDC = lerp(SamplePositionNDC, PreviousPositionNDC, 0.25f);
                float MiddleSurfaceDepth = H_SAMPLE_LOD(g_HTraceDepthPyramidSSGI, H_SAMPLER_POINT_CLAMP, MiddlePositionNDC.xy * HRenderScale.xy, DepthLOD).x;
                float2 MiddleWorkingDepth = GetWorkingDepth(MiddleSurfaceDepth, MiddlePositionNDC.z);
            
                if (WorkingDepthCompare(MiddleWorkingDepth.x, MiddleWorkingDepth.y, _ThicknessParams.xy)) // && SurfaceDepth > 0)
                {
                    HitCoordNDC = MiddlePositionNDC.xyz;
                    HitFound = true;
                    break;  
                }
            }
        }
        
        PreviousPositionNDC = SamplePositionNDC;
    }
    
    HitCoordNDC =  SkyPassed ? PreviousPositionNDC : HitCoordNDC; 
    
    return float4(HitCoordNDC.xyz, HitFound);
}


float4 RayMarchHalf(float3 RayStartPositionNDC, float3 RayEndPositionNDC, float StepJitter)
{
    int StepCount = _StepCount / 2;
    uint FirstStep = uint(_FrameCount) % 2;
    
    bool HitFound = false;
    bool SkyPassed = false;
    
    float3 HitCoordNDC = 0;
    float3 PreviousPositionNDC = RayStartPositionNDC;
    
    for (int i = FirstStep; i < StepCount; i = i + 2)
    {
        float3 SamplePositionNDC = lerp(RayStartPositionNDC, RayEndPositionNDC, pow(float(min(StepCount, i + StepJitter * 1)) / StepCount, 2));
    
        uint DepthLOD = lerp(MAX_DEPTH_MIP_LEVEL, 4, (float(i)) / StepCount); 
        float SurfaceDepth = H_SAMPLE_LOD(g_HTraceDepthPyramidSSGI, H_SAMPLER_POINT_CLAMP, SamplePositionNDC.xy * HRenderScale.xy, DepthLOD).x;

        if (SurfaceDepth <= 0)
        {
            SkyPassed = true;
        }
        
        if (SamplePositionNDC.z < SurfaceDepth)
        {
            float2 WorkingDepth = GetWorkingDepth(SurfaceDepth, SamplePositionNDC.z);
            
            if (WorkingDepthCompare(WorkingDepth.x, WorkingDepth.y, _ThicknessParams.xy))
            {   
                HitCoordNDC = SamplePositionNDC.xyz;
                HitFound = true;
                break;  
            }
            else if (REFINE_INTERSECTION && WorkingDepthCompare(WorkingDepth.x, WorkingDepth.y, _ThicknessParams.zw))
            {
                float3 MiddlePositionNDC = lerp(SamplePositionNDC, PreviousPositionNDC, 0.25f);
                float MiddleSurfaceDepth = H_SAMPLE_LOD(g_HTraceDepthPyramidSSGI, H_SAMPLER_POINT_CLAMP, MiddlePositionNDC.xy * HRenderScale.xy, DepthLOD).x;
                float2 MiddleWorkingDepth = GetWorkingDepth(MiddleSurfaceDepth, MiddlePositionNDC.z);
            
                if (WorkingDepthCompare(MiddleWorkingDepth.x, MiddleWorkingDepth.y, _ThicknessParams.xy))
                {
                    HitCoordNDC = MiddlePositionNDC.xyz;
                    HitFound = true;
                    break;  
                }
            }
        }
        
        PreviousPositionNDC = SamplePositionNDC;
    }

    HitCoordNDC =  SkyPassed ? PreviousPositionNDC : HitCoordNDC; 

    return float4(HitCoordNDC.xyz, HitFound);
}


float4 RayMarchValidation(float3 RayStartPositionNDC, float3 RayEndPositionNDC, float StepJitter)
{
    #ifdef HALF_STEP_VALIDATION
    return RayMarchHalf(RayStartPositionNDC, RayEndPositionNDC, StepJitter);
    #else
    return RayMarch(RayStartPositionNDC, RayEndPositionNDC, StepJitter);
    #endif
}
