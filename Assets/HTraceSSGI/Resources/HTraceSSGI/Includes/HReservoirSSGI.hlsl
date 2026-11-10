#include "HCommonSSGI.hlsl"

H_TEXTURE(_Reservoir);
H_TEXTURE(_ReservoirReprojected);
H_TEXTURE(_ReservoirLuminance);

H_RW_TEXTURE(uint4, _Reservoir_Output);
H_RW_TEXTURE(uint4, _ReservoirSpatial_Output);
H_RW_TEXTURE(uint4, _ReservoirTemporal_Output);
H_RW_TEXTURE(float, _ReservoirLuminance_Output);

float _ReservoirDiffuseWeight;

// ------------------------ RESERVOIR STRUCTS -----------------------
struct OcclusionReservoir
{
    // Main
    float Occlusion;
    float Wsum;
    float M;
    float W;
    
    // Ray
    float3 Direction;
    float Distance;
};

struct TemporalReservoir
{
    // Main
    float3 Color;
    float Wsum;
    float M;
    float W;

    // Origin
    float3 OriginNormal;

    // Ray
    float3 Direction;
    float Distance;
    bool HitFound;
};

struct SpatialReservoir
{
    // Main
    float3 Color;
    float Wsum;
    float M;
    float W;

    // Denoiser
    float3 Normal;
    float Depth;
    float Occlusion;
    
    // Ray
    float3 Direction;
    float Distance;
    bool HitFound;
};


// ------------------------ PACKING FUNCTIONS -----------------------

uint4 PackTemporalReservoir(TemporalReservoir Reservoir)
{
    uint W = f32tof16(Reservoir.W);		
    uint M = f32tof16(Reservoir.M);
    uint PackedMW = (W << 16) | (M & 0xFFFE) | (Reservoir.HitFound & 0x1);
    uint PackedColorHit = PackToR11G11B10f(Reservoir.Color); // PackColorHit(Reservoir.Color, Reservoir.HitFound);
    uint PackedDistance = f32tof16(Reservoir.Distance); // uint(Reservoir.Distance * 65534.0f + 0.5f);
    uint PackedDirection = PackDirection32Bit(Reservoir.Direction);
    uint PackedOriginaNormal = PackDirection16Bit(Reservoir.OriginNormal);
    uint PackedNormalDistance = (PackedDistance << 16) | (PackedOriginaNormal << 0);
        
    return uint4(PackedColorHit, PackedMW, PackedDirection, PackedNormalDistance);
}

void UnpackTemporalReservoir(uint4 ReservoirPacked, float3 Diffuse, inout TemporalReservoir Reservoir)
{
    Reservoir.HitFound = ReservoirPacked.y & 0x1;
    Reservoir.Color = UnpackFromR11G11B10f(ReservoirPacked.x); // UnpackColorHit(ReservoirPacked.x, Reservoir.HitFound);
    Reservoir.W = f16tof32(ReservoirPacked.y >> 16);
    Reservoir.M = f16tof32(ReservoirPacked.y & 0xFFFE);
    Reservoir.Wsum = Reservoir.W * Reservoir.M * Luminance(Reservoir.Color * Diffuse);
    Reservoir.Distance =  f16tof32(ReservoirPacked.w >> 16);
    Reservoir.Direction = UnpackDirection32Bit(ReservoirPacked.z);
    Reservoir.OriginNormal = UnpackDirection16Bit(ReservoirPacked.w);
}

uint4 PackSpatialReservoir(SpatialReservoir Reservoir)
{   
    uint W = f32tof16(Reservoir.W);		
    uint M = f32tof16(Reservoir.M);		
    uint PackedMW = (W << 16) | (M & 0xFFFE) | (Reservoir.HitFound & 0x1);
    uint PackedColor = PackToR11G11B10f(Reservoir.Color);
    uint PackedDistance = (uint(f32tof16(Reservoir.Distance)) >> 6) & 0x3FF;
    uint PackedDirection = PackDirection16Bit(Reservoir.Direction) & 0xFFFF;
    uint PackedOcclusion = uint(Reservoir.Occlusion * 63.0f + 0.5f) & 0x3F;
    uint PackedDirectionDistanceOcclusion = (PackedDirection << 16) | (PackedOcclusion << 10) | (PackedDistance << 0);
    uint DepthPacked = f32tof16(Reservoir.Depth) & 0xFFFF;
    uint NormalPacked = PackDirection16Bit(Reservoir.Normal) & 0xFFFF;
    uint NormalDepthPacked = (NormalPacked << 16) | (DepthPacked << 0);
        
    return uint4(PackedColor, PackedMW, PackedDirectionDistanceOcclusion, NormalDepthPacked);
}

void UnpackSpatialReservoir(uint4 ReservoirPacked, float3 Diffuse, inout SpatialReservoir Reservoir)
{
    Reservoir.HitFound = ReservoirPacked.y & 0x1;
    Reservoir.Color = UnpackFromR11G11B10f(ReservoirPacked.x);
    Reservoir.W = f16tof32(ReservoirPacked.y >> 16);
    Reservoir.M = f16tof32(ReservoirPacked.y >> 0);
    Reservoir.Wsum = Reservoir.W * Reservoir.M * Luminance(Reservoir.Color * Diffuse);
    Reservoir.Distance = f16tof32((ReservoirPacked.z & 0x3FF) << 6);
    Reservoir.Occlusion = float((ReservoirPacked.z >> 10) & 0x3F) / 63;
    Reservoir.Direction = UnpackDirection16Bit((ReservoirPacked.z >> 16) & 0xFFFF);
    Reservoir.Normal = UnpackDirection16Bit((ReservoirPacked.w >> 16) & 0xFFFF);
    Reservoir.Depth = f16tof32(ReservoirPacked.w & 0xFFFF); 
}


// ------------------------ RESERVOIR FUNCTIONS -----------------------
float3 GetReservoirDiffuse(uint2 pixCoord)
{
    float3 DiffuseBuffer = HBUFFER_DIFFUSE(pixCoord).xyz * _ReservoirDiffuseWeight;

    if (DiffuseBuffer.x + DiffuseBuffer.y + DiffuseBuffer.z <= 0.05f)
        DiffuseBuffer = float3(0.05f, 0.05f, 0.05f);

    return DiffuseBuffer;
}

// Spatial reservoir exchange
bool ReservoirUpdate(SpatialReservoir SampleReservoir, inout SpatialReservoir MainReservoir, inout uint Random)
{
    float RandomValue = HUintToFloat01(Hash1Mutate(Random));
    
    MainReservoir.Wsum += SampleReservoir.Wsum; 
    MainReservoir.M += SampleReservoir.M;
    
    if (RandomValue < SampleReservoir.Wsum / MainReservoir.Wsum)
    {   
        MainReservoir.Color = SampleReservoir.Color;
        MainReservoir.HitFound = SampleReservoir.HitFound;
        MainReservoir.Distance = SampleReservoir.Distance;
        MainReservoir.Direction = SampleReservoir.Direction;
 
        return true;
    }
    
    return false;
}

// Temporal reservoir exchange
bool ReservoirUpdate(TemporalReservoir SampleReservoir, inout TemporalReservoir MainReservoir, inout uint Random)
{
    float RandomValue = HUintToFloat01(Hash1Mutate(Random));
    
    MainReservoir.Wsum += SampleReservoir.Wsum; 
    MainReservoir.M += SampleReservoir.M;

    if (RandomValue < SampleReservoir.Wsum / MainReservoir.Wsum)
    {
        MainReservoir.Color = SampleReservoir.Color;
        MainReservoir.HitFound = SampleReservoir.HitFound;
        MainReservoir.Distance = SampleReservoir.Distance;
        MainReservoir.Direction = SampleReservoir.Direction;
        MainReservoir.OriginNormal = SampleReservoir.OriginNormal;
        
        return true;
    }
    
    return false;
}


// Radiance reservoir fill
bool ReservoirUpdate(float3 SampleColor, float3 SampleNormal, bool HitFound, float SampleW, float SampleM, inout TemporalReservoir Reservoir, inout uint Random)
{
    float RandomValue = HUintToFloat01(Hash1Mutate(Random));
    
    Reservoir.Wsum += SampleW;
    Reservoir.M += SampleM;
    
    if (RandomValue < SampleW / Reservoir.Wsum)
    {
        Reservoir.Color = SampleColor;
        Reservoir.HitFound = HitFound;
        Reservoir.OriginNormal = SampleNormal;
        
        return true;
    }
    
    return false;
}


// Radiance reservoir fill
bool ReservoirUpdate(float3 SampleColor, float3 SampleDirection, float3 SampleNormal, float SampleDistance, bool HitFound, float SampleW, float SampleM, inout TemporalReservoir Reservoir, inout uint Random)
{
    float RandomValue = HUintToFloat01(Hash1Mutate(Random));
    
    Reservoir.Wsum += SampleW;
    Reservoir.M += SampleM;
    
    if (RandomValue < SampleW / Reservoir.Wsum)
    {
        Reservoir.Color = SampleColor;
        Reservoir.HitFound = HitFound;
        Reservoir.Distance = SampleDistance;
        Reservoir.Direction = SampleDirection;
        Reservoir.OriginNormal = SampleNormal;
        
        return true;
    }
    
    return false;
}


// Occlusion reservoir fill
bool ReservoirUpdate(float SampleOcclusion, float3 SampleDirection, float SampleDistance, float SampleW, float SampleM, inout OcclusionReservoir Reservoir, inout uint Random)
{
    float RandomValue = HUintToFloat01(Hash1Mutate(Random));
    
    Reservoir.Wsum += SampleW;
    Reservoir.M += SampleM;
    
    if (RandomValue < SampleW / Reservoir.Wsum)
    {
        Reservoir.Distance = SampleDistance;
        Reservoir.Direction = SampleDirection;
        Reservoir.Occlusion = SampleOcclusion;
        
        return true;
    }
    
    return false;
}
