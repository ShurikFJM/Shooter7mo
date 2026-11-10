#include "../Headers/HMain.hlsl"
#include "../Headers/HMath.hlsl"
#include "../Headers/HSpaceTransforms.hlsl"

#define ENABLE_RCRS_FILTER 1
#define ENABLE_EXPOSURE_CONTROL 1

#define ENABLE_SPATIAL_RESTIR 1
#define ENABLE_TEMPORAL_RESTIR 1

#define ENABLE_SPATIAL_DENOISING 1
#define ENABLE_TEMPORAL_DENOISING 1
#define ENABLE_TEMPORAL_STABILIZATION 1


// ------------------------ COMMON PROPERTIES -------------------------
float _HScaleFactorSSGI;
float _HPreviousScaleFactorSSGI;

float _Falloff;
float4 _APVParams;
float4 _DepthToViewParams;

uint _ExcludeCastingLayerMaskSSGI;
uint _ExcludeReceivingLayerMaskSSGI;


// ------------------------ SCALING FUNCTIONS -------------------------
uint2 GetUnscaledCoords(uint2 pixCoord)
{
   return round(pixCoord.xy * _HScaleFactorSSGI.xx);
}


// ------------------------ SAMPLING FUNCTIONS -------------------------
float ProbePlaneWeighting(float4 Plane, float3 WorldPosSample, float DepthLinearCenter, float Multiplier)
{
    float PlaneDistance = abs(dot(float4(WorldPosSample, -1), Plane));
    float DepthDifference = PlaneDistance / DepthLinearCenter;
    float PlaneWeight = exp2(-100.0f * Multiplier * (DepthDifference * DepthDifference));
    return PlaneWeight;
}

float3 ComputeFastViewSpacePosition(float2 pixCoordNDC, float Depth, float DepthLinear)
{
    #ifdef VR_COMPATIBILITY
    return ComputeViewSpacePosition(pixCoordNDC, Depth, UNITY_MATRIX_I_P) * float3(1, -1, 1);
    #endif 
    
    float3 PositionVS = float3((pixCoordNDC * _DepthToViewParams.xy + _DepthToViewParams.zw) * DepthLinear.xx, DepthLinear);
    return float3(PositionVS.x, PositionVS.y, -PositionVS.z);
}

float ExponentialFalloff(float HitDistance, float MaxDistance)
{
    float Falloff = 0;
    
    float Threshold = 0.35 * MaxDistance;
    if (HitDistance <= Threshold)
        Falloff = 1.0;
    
    float NormalizedDistance = saturate((HitDistance - Threshold) / (MaxDistance - Threshold));
    Falloff = exp2(-(_Falloff * 3) * NormalizedDistance);
    
    return Falloff;
}

float RadicalInverseVdC(uint bits)
{
    bits = (bits << 16) | (bits >> 16);
    bits = ((bits & 0x55555555) << 1) | ((bits & 0xAAAAAAAA) >> 1);
    bits = ((bits & 0x33333333) << 2) | ((bits & 0xCCCCCCCC) >> 2);
    bits = ((bits & 0x0F0F0F0F) << 4) | ((bits & 0xF0F0F0F0) >> 4);
    bits = ((bits & 0x00FF00FF) << 8) | ((bits & 0xFF00FF00) >> 8);
    return float(bits) * 2.3283064365386963e-10;
}

float2 SampleUnitDisk(uint Index)
{
    float Angle = RadicalInverseVdC(Index) * 2.0 * H_PI;
    float Radius = sqrt(frac(float(Index) * 0.61803398875)); // Golden ratio for decorrelation
    return float2(cos(Angle), sin(Angle)) * Radius;
}

inline float Jitter(float2 Coord)
{
    float a = 12.9898;
    float b = 78.233;
    float c = 43758.5453;
    float dt = dot(Coord.xy, float2(a, b));
    float sn = fmod(dt, 3.14);
    return frac(sin(sn) * c);
}

float sqr(float value)
{
    return value * value;
}

float GaussianWeighting(float Radius, float Sigma)
{
    return exp2(-sqr(Radius / Sigma));
}

float3 SpatialDenoisingTonemap(float3 Color)
{
    return Color * rcp(max(max(Color.r, Color.g), Color.b) + 1.0);
}

float3 SpatialDenoisingTonemapInverse(float3 Color)
{
    return Color * rcp(1.0 - max(max(Color.r, Color.g), Color.b));
}

// ------------------------ Color.hlsl in HDRP -------------------------

float HInterleavedGradientNoise(float2 pixCoord, int frameCount)
{
    const float3 magic = float3(0.06711056f, 0.00583715f, 52.9829189f);
    float2 frameMagicScale = float2(2.083f, 4.867f);
    pixCoord += frameCount * frameMagicScale;
    return frac(magic.z * frac(dot(pixCoord, magic.xy)));
}

// ------------------------ PACKING FUNCTIONS -------------------------

uint PackDirection32Bit(float3 Direction)
{
    float2 DirectionOctahedral = PackNormalOctQuadEncode(Direction);
    DirectionOctahedral = DirectionOctahedral * 0.5f + 0.5f;
    
    uint DirectionX = uint(DirectionOctahedral.x * 65534.0f + 0.5f);
    uint DirectionY = uint(DirectionOctahedral.y * 65534.0f + 0.5f);
    return (DirectionX << 16) | (DirectionY << 0);
}

float3 UnpackDirection32Bit(uint PackedDirection)
{
    float3 Direction;
    Direction.x = float((PackedDirection >> 16) & 0xFFFF) / 65534.0f;
    Direction.y = float((PackedDirection >>  0) & 0xFFFF) / 65534.0f;
    Direction = UnpackNormalOctQuadEncode(Direction.xy * 2.0f - 1.0f);
    return Direction;
}

uint PackDirection24Bit(float3 Direction)
{
    float2 DirectionOctahedral = PackNormalOctQuadEncode(Direction);
    DirectionOctahedral = DirectionOctahedral * 0.5f + 0.5f;
    
    uint DirectionX = uint(DirectionOctahedral.x * 4095.0f + 0.5f);
    uint DirectionY = uint(DirectionOctahedral.y * 4095.0f + 0.5f);
    return (DirectionX << 12) | (DirectionY << 0);
}

float3 UnpackDirection24Bit(uint PackedDirection)
{
    float3 Direction;
    Direction.x = float((PackedDirection >> 12) & 0xFFF) / 4095.0f;
    Direction.y = float((PackedDirection >> 0) & 0xFFF) / 4095.0f;
    Direction = UnpackNormalOctQuadEncode(Direction.xy * 2.0f - 1.0f);
    return Direction;
}

uint PackDirection16Bit(float3 Direction)
{
    float2 DirectionOctahedral = PackNormalOctQuadEncode(Direction);
    DirectionOctahedral = DirectionOctahedral * 0.5f + 0.5f;
    
    uint DirectionX = uint(DirectionOctahedral.x * 255.0f + 0.5f);
    uint DirectionY = uint(DirectionOctahedral.y * 255.0f + 0.5f);
    return (DirectionX << 8) | (DirectionY << 0);
}

float3 UnpackDirection16Bit(uint PackedDirection)
{
    float3 Direction;
    Direction.x = float((PackedDirection >> 8) & 0xFF) / 255.0f;
    Direction.y = float((PackedDirection >> 0) & 0xFF) / 255.0f;
    Direction = UnpackNormalOctQuadEncode(Direction.xy * 2.0f - 1.0f);
    return Direction;
}

uint PackAmbientOcclusion(float AmbientOcclusion)
{
    uint AmbientOcclusionPacked = AmbientOcclusion * 255.0f + 0.5f;
    return AmbientOcclusionPacked;
}

float UnpackAmbientOcclusion(uint AmbientOcclusionPacked)
{
    float AmbientOcclusion = float(AmbientOcclusionPacked) / 255.0f;
    return AmbientOcclusion;
}

uint PackColorHit(float3 Color, bool Hit)
{
    uint R = (f32tof16(Color.r) << 17) & 0xFFE00000; 
    uint G = (f32tof16(Color.g) << 6) & 0x001FF800;  
    uint B = (f32tof16(Color.b) >> 4) & 0x000007FE;  
    uint A = Hit ? 0x00000001 : 0x00000000;
    return R | G | B | A;
}

float3 UnpackColorHit(uint ColorHitPacked, inout bool Hit)
{
    float3 Color;
    Color.r = f16tof32((ColorHitPacked >> 17) & 0x7FF0);
    Color.g = f16tof32((ColorHitPacked >> 6) & 0x7FF0);
    Color.b = f16tof32((ColorHitPacked << 4) & 0x7FE0);
    Hit = (ColorHitPacked & 0x00000001) == 0x00000001 ? true : false;
    return Color;
}

uint PackNormalDepth(float3 Normal, float Depth)
{
    float2 NormalOctahedral = PackNormalOctQuadEncode(Normal);
    NormalOctahedral = NormalOctahedral * 0.5f + 0.5f;
    uint NormalX = uint(NormalOctahedral.x * 255.0f + 0.5f);
    uint NormalY = uint(NormalOctahedral.y * 255.0f + 0.5f);
    
    uint DepthPacked = f32tof16(Depth);
    return (DepthPacked << 16) | (NormalX << 8) | (NormalY << 0);
}

float4 UnpackNormalDepth(uint NormalDepthPacked)
{
    float2 Normal;
    float4 NormalDepth;
    Normal.x = float((NormalDepthPacked >> 8) & 0xFF) / 255.0f;
    Normal.y = float((NormalDepthPacked >> 0) & 0xFF) / 255.0f;
    NormalDepth.xyz = UnpackNormalOctQuadEncode(Normal * 2.0f - 1.0f);
    
    NormalDepth.w = f16tof32(NormalDepthPacked >> 16);
    
    return NormalDepth.xyzw;
}
