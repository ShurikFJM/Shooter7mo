#pragma once




// --------------------------------- GET CAMERA POSITION
float3 H_GET_CAMERA_POSITION_WS()
{


    return _WorldSpaceCameraPos;
}


// --------------------------------- GET ABSOLUTE WS POSITION
float3 H_GET_ABSOLUTE_POSITION_WS(float3 PositionWS)
{


    return PositionWS;
}


// --------------------------------- GET RELATIVE WS POSITION
float3 H_GET_RELATIVE_POSITION_WS(float3 PositionWS)
{


    return GetCameraRelativePositionWS(PositionWS);
}


// --------------------------------- GET NORMALIZED WS VIEW DIRECTION
float3 H_GET_VIEW_DIRECTION_WS(float3 PositionWS)
{
    float3 ViewDirectionWS = H_GET_CAMERA_POSITION_WS() - H_GET_ABSOLUTE_POSITION_WS(PositionWS);
    return normalize(ViewDirectionWS);
}


// --------------------------------- GET NORMALIZED WS VIEW VECTOR
float3 H_GET_VIEW_VECTOR_WS(float2 pixCoordNDC)
{
    float4 PositionVS = mul(H_MATRIX_I_P, float4(pixCoordNDC * 2.0 - 1.0, 1.0, 1.0));
    float3 DirectionVS = normalize(float3(PositionVS.x, -PositionVS.y, PositionVS.z));
    return normalize(mul((float3x3)H_MATRIX_I_V, DirectionVS));
}


// --------------------------------- COMPUTE CLIP SPACE POSITION
float4 H_COMPUTE_POSITION_CS(float2 pixCoordNDC, float Depth)
{
    float4 PositionCS = float4(pixCoordNDC * 2.0 - 1.0, Depth, 1.0);

    #if UNITY_UV_STARTS_AT_TOP
    PositionCS.y = -PositionCS.y;
    #endif

    return PositionCS;
}


// --------------------------------- COMPUTE CLIP SPACE POSITION
float3 H_COMPUTE_NDC_Z(float3 Position, float4x4 InvViewProjMatrix)
{

    return ComputeNormalizedDeviceCoordinatesWithZ(Position, InvViewProjMatrix);

}


// --------------------------------- COMPUTE WORLD SPACE POSITION
float3 H_COMPUTE_POSITION_WS(float2 pixCoordNDC, float Depth, float4x4 InvViewProjMatrix)
{

    return ComputeWorldSpacePosition(pixCoordNDC, Depth, InvViewProjMatrix);

}


// --------------------------------- WORLD TO VIEW DIRECTION
float3 H_TRANSFORM_WORLD_TO_VIEW_DIR(float3 DirectionWS, bool Normalize = false)
{
    float3 DirectionVS = mul((float3x3)H_MATRIX_V, DirectionWS).xyz;
    if (Normalize) return normalize(DirectionVS);

    return DirectionVS;
}


// --------------------------------- VIEW TO WORLD DIRECTION
float3 H_TRANSFORM_VIEW_TO_WORLD_DIR(float3 DirectionVS, bool Normalize = false)
{
    float3 DirectionWS = mul((float3x3)H_MATRIX_I_V, DirectionVS).xyz;
    if (Normalize) return normalize(Normalize);

    return DirectionWS;
}


// --------------------------------- WORLD TO VIEW NOWMAL
float3 H_TRANSFORM_WORLD_TO_VIEW_NORMAL(float3 NormalWS, bool Normalize = false)
{

    return TransformWorldToViewNormal(NormalWS);
    
}



// --------------------------------- VIEW TO WORLD NOWMAL
float3 H_TRANSFORM_VIEW_TO_WORLD_NORMAL(float3 NormalVS, bool Normalize = false)
{

    return TransformViewToWorldNormal(NormalVS);
    
}



// --------------------------------- RAW TO 01 LINEAR DEPTH
float H_LINEAR_01_DEPTH(float Depth)
{
    
    
    return Linear01Depth(Depth, _ZBufferParams);
}


// --------------------------------- RAW TO EYE LINEAR DEPTH
float H_LINEAR_EYE_DEPTH(float3 PositionWS, float4x4 ViewMatrix)
{
    
    
    return LinearEyeDepth(PositionWS, ViewMatrix);
}


float H_LINEAR_EYE_DEPTH(float Depth)
{
    
    
    return LinearEyeDepth(Depth, _ZBufferParams);
}
