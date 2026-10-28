
Shader "Env/Universal environment Material"
{
    Properties
    {
        [Header(Base)]
        _BaseColor("Tint", Color) = (1,1,1,1)
        [MainTexture][NoScaleOffset] _AlbedoTex("Albedo", 2D) = "white" {}

        [Header(UV)]
        [KeywordEnum(Mesh, WorldXZ, WorldXY, WorldZY)] _UVMode("UV Mode", Float) = 0
        _TilingX("Tiling X", Float) = 1
        _TilingY("Tiling Y", Float) = 1
        _Rotation("Rotation (deg)", Range(-180, 180)) = 0
        _OffsetX("Offset X", Float) = 0
        _OffsetY("Offset Y", Float) = 0

        [Header(Voronoi  anti tiling para texturas no seamless)]
        [Toggle(_VORONOI)] _UseVoronoi("Use Voronoi", Float) = 0
        _VoronoiScale("Cells per Tile", Range(0.5, 8)) = 2
        _VoronoiJitter("Cell Irregularity", Range(0, 1)) = 1
        _VoronoiBlend("Edge Blend", Range(0.01, 0.5)) = 0.12
        _RandOffset("Random Offset", Range(0, 1)) = 0.3
        _RandRotation("Random Rotation", Range(0, 1)) = 1
        _RandScale("Random Tiling", Range(0, 0.5)) = 0.15

        [Header(Normal)]
        [Toggle(_NORMALMAP)] _UseNormal("Use Normal Map", Float) = 0
        [NoScaleOffset] _NormalTex("Normal Map", 2D) = "bump" {}
        _NormalStrength("Normal Strength", Range(0, 2)) = 1
        [Toggle(_NORMAL_FLIPY)] _NormalFlipY("Flip Y (DirectX normal)", Float) = 0

        [Header(ARM   R AO   G Roughness   B Metallic)]
        [Toggle(_ARMMAP)] _UseARM("Use ARM Map", Float) = 0
        [NoScaleOffset] _ARMTex("ARM", 2D) = "white" {}
        [Toggle(_ROUGH_INVERT)] _RoughInvert("One Minus Roughness (textura trae Gloss)", Float) = 0
        _AOStrength("AO Strength", Range(0, 1)) = 1
        _RoughMax("Roughness Max", Range(0, 1)) = 1
        _MetalMin("Metallic Max", Range(0, 1)) = 0
        _RoughConst("Roughness (sin ARM)", Range(0, 1)) = 0.6
        _MetalConst("Metallic (sin ARM)", Range(0, 1)) = 0

        [Header(Displacement)]
        [KeywordEnum(Off, Offset, POM, Vertex)] _Disp("Mode", Float) = 0
        [Toggle(_HEIGHT_IN_ARM_A)] _HeightInARM("Height in ARM alpha", Float) = 0
        [NoScaleOffset] _HeightTex("Height", 2D) = "gray" {}
        [Toggle] _HeightInvert("Invert Height", Float) = 0
        _HeightScale("Parallax Scale", Range(0, 0.15)) = 0.04
        _HeightRef("Reference Plane", Range(0, 1)) = 0.5
        _POMMinSteps("POM Min Steps", Range(4, 32)) = 8
        _POMMaxSteps("POM Max Steps", Range(8, 64)) = 24
        _FadeStart("Parallax Fade Start (m)", Float) = 15
        _FadeRange("Parallax Fade Range (m, 0 = off)", Float) = 10
        _VertexDispScale("Vertex Displacement (units)", Float) = 0.1

        [Header(Extras)]
        [Toggle(_EMISSION)] _UseEmission("Emission", Float) = 0
        [HDR] _EmissionColor("Emission Color", Color) = (0,0,0,1)
    }

    SubShader
    {
        Tags
        {
            "RenderType" = "Opaque"
            "RenderPipeline" = "UniversalPipeline"
            "Queue" = "Geometry"
        }
        LOD 300


        HLSLINCLUDE
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

        // ---- Keywords -> features internas
        #if defined(_NORMALMAP)
            #define USE_NORMAL_MAP 1
        #endif
        #if defined(_NORMAL_FLIPY)
            #define NORMAL_MAP_FLIP_Y 1
        #endif
        #if defined(_ARMMAP)
            #define USE_ARM_MAP 1
        #endif
        #if defined(_ROUGH_INVERT)
            #define USE_ROUGH_INVERT 1
        #endif
        #if defined(_EMISSION)
            #define USE_EMISSIVE 1
        #endif
        #if defined(_HEIGHT_IN_ARM_A)
            #define HEIGHT_IN_ARM_A 1
        #endif
        #if defined(_VORONOI)
            #define USE_VORONOI 1
        #endif

        #if defined(_UVMODE_WORLDXZ)
            #define USE_WORLD_UV_XZ 1
            #define ENV_WORLD_UV 1
        #elif defined(_UVMODE_WORLDXY)
            #define USE_WORLD_UV_XY 1
            #define ENV_WORLD_UV 1
        #elif defined(_UVMODE_WORLDZY)
            #define USE_WORLD_UV_ZY 1
            #define ENV_WORLD_UV 1
        #endif

        #if defined(_DISP_OFFSET)
            #define USE_PARALLAX_OFFSET 1
            #define ENV_PARALLAX 1
        #elif defined(_DISP_POM)
            #define USE_POM 1
            #define ENV_PARALLAX 1
        #elif defined(_DISP_VERTEX)
            #define USE_VERTEX_DISPLACEMENT 1
        #endif

        #if defined(ENV_PARALLAX) || defined(USE_VERTEX_DISPLACEMENT)
            #define ENV_USE_HEIGHT 1
        #endif

  
        #if defined(ENV_PARALLAX) || defined(USE_VORONOI)
            #define ENV_NEED_GRAD 1
        #endif


        TEXTURE2D(_AlbedoTex);  SAMPLER(sampler_AlbedoTex);
        TEXTURE2D(_NormalTex);
        TEXTURE2D(_ARMTex);
        TEXTURE2D(_HeightTex);
        #define ENV_SAMPLER sampler_AlbedoTex


        CBUFFER_START(UnityPerMaterial)
            float4 _BaseColor;
            float  _TilingX, _TilingY, _Rotation, _OffsetX, _OffsetY, _UVMode;
            float  _UseVoronoi, _VoronoiScale, _VoronoiJitter, _VoronoiBlend;
            float  _RandOffset, _RandRotation, _RandScale;
            float  _UseNormal, _NormalFlipY, _NormalStrength;
            float  _UseARM, _RoughInvert, _AOStrength;
            float  _RoughMin, _RoughMax, _MetalMin, _MetalMax;
            float  _RoughConst, _MetalConst;
            float  _Disp, _HeightInARM, _HeightInvert;
            float  _HeightScale, _HeightRef;
            float  _POMMinSteps, _POMMaxSteps;
            float  _FadeStart, _FadeRange;
            float  _VertexDispScale;
            float  _UseEmission;
            float4 _EmissionColor;
        CBUFFER_END

        struct EnvSurface
        {
            float3 albedo;
            float3 normalWS;
            float  metallic;
            float  roughness;
            float  ao;
            float3 emissive;
        };


        float2 EnvGetBaseUV(float2 uv, float3 positionWS)
        {
        #if defined(USE_WORLD_UV_XZ)
            return positionWS.xz;
        #elif defined(USE_WORLD_UV_XY)
            return positionWS.xy;
        #elif defined(USE_WORLD_UV_ZY)
            return positionWS.zy;
        #else
            return uv;
        #endif
        }


        float2 EnvSinCos()
        {
            float s, c;
            sincos(radians(_Rotation), s, c);
            return float2(s, c);
        }

        float4 EnvRotMatrix(float2 sc) { return float4(sc.y, -sc.x, sc.x, sc.y); }


        float2 EnvTransformUV(float2 uv, float4 R)
        {
            float4 M = R * float4(_TilingX, _TilingY, _TilingX, _TilingY);
            return float2(dot(uv, M.xy), dot(uv, M.zw)) + float2(_OffsetX, _OffsetY);
        }


        void EnvGetTangentFrame(float3 N, float4 tangentWS, out float3 T, out float3 B)
        {
            float sgn;
        #if defined(USE_WORLD_UV_XZ)
            T = float3(1, 0, 0); sgn = -1.0;
        #elif defined(USE_WORLD_UV_XY)
            T = float3(1, 0, 0); sgn =  1.0;
        #elif defined(USE_WORLD_UV_ZY)
            T = float3(0, 0, 1); sgn = -1.0;
        #else
            T = tangentWS.xyz;   sgn = tangentWS.w;
        #endif

        #if defined(ENV_WORLD_UV)
            T = normalize(T - N * dot(N, T));
        #elif defined(ENV_PARALLAX)
            T = normalize(T);
        #endif
            B = cross(N, T) * sgn;
        }

        // ---- Voronoi ------------------------------------------------------
        // Se buscan las DOS celdas mas cercanas (A = mas cercana, B = segunda).
        // Cada celda remapea la textura con su propio offset / rotacion / escala,
        // alrededor del CENTRO de la textura (asi no se cruzan los bordes de una
        // textura no seamless). Cerca del borde entre celdas se mezclan A y B;
        // en el interior de la celda solo se hace 1 sample (branch).
        struct EnvVoro
        {
            float4 mA, mB;        // rotacion*escala de cada celda (2x2)
            float4 rA, rB;        // rotacion pura (para girar la normal)
            float2 cA, cB;        // centro de la celda (espacio tuv)
            float2 tA, tB;        // punto de la textura donde cae el centro
            float2 gAx, gAy, gBx, gBy;   // gradientes remapeados (mipmaps correctos)
            float  w;             // peso de A (1 = solo A)
        };

        float2 EnvHash22(float2 p)
        {
            float3 p3 = frac(float3(p.xyx) * float3(0.1031, 0.1030, 0.0973));
            p3 += dot(p3, p3.yzx + 33.33);
            return frac((p3.xx + p3.yz) * p3.zy);
        }

        #ifdef USE_VORONOI

        void EnvCellParams(float2 id, float2 ptP, out float4 M, out float4 Rr,
                           out float2 center, out float2 target)
        {
            float2 r1 = EnvHash22(id + 11.7);
            float2 r2 = EnvHash22(id + 37.3);

            float ang = (r1.x * 2.0 - 1.0) * 3.14159265 * _RandRotation;
            float sc  = max(0.2, 1.0 + (r1.y * 2.0 - 1.0) * _RandScale);
            float s, c;
            sincos(ang, s, c);

            Rr     = float4(c, -s, s, c);
            M      = Rr * sc;
            center = ptP / _VoronoiScale;
            target = 0.5 + (r2 * 2.0 - 1.0) * (0.5 * _RandOffset);
        }

        float2 EnvVoroUV(float2 tuv, float4 M, float2 c, float2 t)
        {
            float2 d = tuv - c;
            return float2(dot(M.xy, d), dot(M.zw, d)) + t;
        }

        EnvVoro EnvVoronoiBuild(float2 tuv, float2 gx, float2 gy)
        {
            EnvVoro v = (EnvVoro)0;

            float2 p  = tuv * _VoronoiScale;
            float2 ip = floor(p);

            float  d1 = 16.0, d2 = 16.0;
            float2 id1 = 0, id2 = 0, pt1 = 0, pt2 = 0;

            [unroll] for (int j = -1; j <= 1; j++)
            {
                [unroll] for (int i = -1; i <= 1; i++)
                {
                    float2 id = ip + float2(i, j);
                    float2 pt = id + 0.5 + (EnvHash22(id) - 0.5) * _VoronoiJitter;
                    float2 df = pt - p;
                    float  d  = dot(df, df);
                    if (d < d1)      { d2 = d1; id2 = id1; pt2 = pt1; d1 = d; id1 = id; pt1 = pt; }
                    else if (d < d2) { d2 = d;  id2 = id;  pt2 = pt; }
                }
            }

            // peso: 0.5 justo en el borde entre celdas, 1 dentro de la celda
            float diff = sqrt(d2) - sqrt(d1);
            v.w = 0.5 + 0.5 * smoothstep(0.0, 1.0, saturate(diff / _VoronoiBlend));

            EnvCellParams(id1, pt1, v.mA, v.rA, v.cA, v.tA);
            v.gAx = float2(dot(v.mA.xy, gx), dot(v.mA.zw, gx));
            v.gAy = float2(dot(v.mA.xy, gy), dot(v.mA.zw, gy));

            [branch]
            if (v.w < 0.999)
            {
                EnvCellParams(id2, pt2, v.mB, v.rB, v.cB, v.tB);
                v.gBx = float2(dot(v.mB.xy, gx), dot(v.mB.zw, gx));
                v.gBy = float2(dot(v.mB.xy, gy), dot(v.mB.zw, gy));
            }
            return v;
        }

        float4 EnvSampleVoro(TEXTURE2D_PARAM(tex, samp), float2 tuv, EnvVoro v)
        {
            float2 uvA = EnvVoroUV(tuv, v.mA, v.cA, v.tA);
            float4 a   = SAMPLE_TEXTURE2D_GRAD(tex, samp, uvA, v.gAx, v.gAy);

            [branch]
            if (v.w > 0.999) return a;

            float2 uvB = EnvVoroUV(tuv, v.mB, v.cB, v.tB);
            float4 b   = SAMPLE_TEXTURE2D_GRAD(tex, samp, uvB, v.gBx, v.gBy);
            return lerp(b, a, v.w);
        }

        #endif 

        
        float3 EnvUnpackNormal(float4 s)
        {
            
            float3 n = UnpackNormalScale(s, _NormalStrength);
        #ifdef NORMAL_MAP_FLIP_Y
            n.y = -n.y;
        #endif
            return n;
        }

        #if defined(USE_VORONOI) && defined(USE_NORMAL_MAP)
       
        float3 EnvSampleNormalVoro(float2 tuv, EnvVoro v)
        {
            float2 uvA = EnvVoroUV(tuv, v.mA, v.cA, v.tA);
            float3 nA  = EnvUnpackNormal(SAMPLE_TEXTURE2D_GRAD(_NormalTex, ENV_SAMPLER, uvA, v.gAx, v.gAy));
            nA.xy = mul(nA.xy, float2x2(v.rA.x, v.rA.y, v.rA.z, v.rA.w));

            [branch]
            if (v.w > 0.999) return nA;

            float2 uvB = EnvVoroUV(tuv, v.mB, v.cB, v.tB);
            float3 nB  = EnvUnpackNormal(SAMPLE_TEXTURE2D_GRAD(_NormalTex, ENV_SAMPLER, uvB, v.gBx, v.gBy));
            nB.xy = mul(nB.xy, float2x2(v.rB.x, v.rB.y, v.rB.z, v.rB.w));

            float2 xy = lerp(nB.xy, nA.xy, v.w);
            return float3(xy, sqrt(saturate(1.0 - dot(xy, xy))));
        }
        #endif

        
        #ifdef ENV_USE_HEIGHT
        float EnvFinalHeight(float h)
        {
            return saturate(lerp(h, 1.0 - h, _HeightInvert));
        }

        float EnvSampleHeightGrad(float2 uv, float2 gx, float2 gy)
        {
        #ifdef HEIGHT_IN_ARM_A
            return EnvFinalHeight(SAMPLE_TEXTURE2D_GRAD(_ARMTex, ENV_SAMPLER, uv, gx, gy).a);
        #else
            return EnvFinalHeight(SAMPLE_TEXTURE2D_GRAD(_HeightTex, ENV_SAMPLER, uv, gx, gy).r);
        #endif
        }

        float EnvSampleHeightLOD0(float2 uv)
        {
        #ifdef HEIGHT_IN_ARM_A
            return EnvFinalHeight(SAMPLE_TEXTURE2D_LOD(_ARMTex, ENV_SAMPLER, uv, 0).a);
        #else
            return EnvFinalHeight(SAMPLE_TEXTURE2D_LOD(_HeightTex, ENV_SAMPLER, uv, 0).r);
        #endif
        }

       
        float EnvHeightAt(float2 uv, EnvVoro v, float2 gx, float2 gy)
        {
        #ifdef USE_VORONOI
            float2 uvA = EnvVoroUV(uv, v.mA, v.cA, v.tA);
            float  hA  = EnvSampleHeightGrad(uvA, v.gAx, v.gAy);
            [branch]
            if (v.w > 0.999) return hA;
            float2 uvB = EnvVoroUV(uv, v.mB, v.cB, v.tB);
            float  hB  = EnvSampleHeightGrad(uvB, v.gBx, v.gBy);
            return lerp(hB, hA, v.w);
        #else
            return EnvSampleHeightGrad(uv, gx, gy);
        #endif
        }

        float EnvHeightLOD0At(float2 uv, EnvVoro v)
        {
        #ifdef USE_VORONOI
            float hA = EnvSampleHeightLOD0(EnvVoroUV(uv, v.mA, v.cA, v.tA));
            [branch]
            if (v.w > 0.999) return hA;
            float hB = EnvSampleHeightLOD0(EnvVoroUV(uv, v.mB, v.cB, v.tB));
            return lerp(hB, hA, v.w);
        #else
            return EnvSampleHeightLOD0(uv);
        #endif
        }
        #endif 

        #ifdef ENV_PARALLAX
        float2 EnvParallaxUV(float2 tuv, float3 viewTS, float distFade, float4 R,
                             EnvVoro vor, float2 gx, float2 gy)
        {
            float scale = _HeightScale * distFade;
            float refPl = _HeightRef;

            [branch]
            if (distFade < 0.001) return tuv;   

        #if defined(USE_POM)
            float2 shiftTS = viewTS.xy / max(viewTS.z, 0.1) * scale;
            float2 shiftUV = float2(dot(R.xy, shiftTS), dot(R.zw, shiftTS));

           
            float  layers     = lerp(_POMMaxSteps, _POMMinSteps, saturate(viewTS.z));
            float  layerDepth = rcp(layers);
            float2 deltaUV    = shiftUV * layerDepth;

            float2 uv       = tuv + shiftUV * refPl;
            float  d        = 1.0 - EnvHeightAt(uv, vor, gx, gy);
            float  prevD    = d;
            float  curDepth = 0.0;

            [loop]
            for (int i = 0; i < (int)layers; i++)
            {
                if (curDepth >= d) break;
                prevD     = d;
                uv       -= deltaUV;
                d         = 1.0 - EnvHeightAt(uv, vor, gx, gy);
                curDepth += layerDepth;
            }

            float after  = d - curDepth;
            float before = prevD - (curDepth - layerDepth);
            float w      = (curDepth > 0.0) ? saturate(after / (after - before - 1e-5)) : 0.0;
            return lerp(uv, uv + deltaUV, w);
        #else
            float2 shiftTS = viewTS.xy * scale;
            float2 shiftUV = float2(dot(R.xy, shiftTS), dot(R.zw, shiftTS));
            float  d       = 1.0 - EnvHeightAt(tuv, vor, gx, gy);
            return tuv - shiftUV * (d - refPl);
        #endif
        }
        #endif 

        #ifdef USE_VERTEX_DISPLACEMENT
        float3 EnvDisplaceVertex(float3 positionOS, float3 normalOS, float2 uv, float3 positionWS)
        {
            float2 tuv = EnvTransformUV(EnvGetBaseUV(uv, positionWS), EnvRotMatrix(EnvSinCos()));
        #ifdef USE_VORONOI
            EnvVoro vor = EnvVoronoiBuild(tuv, float2(0, 0), float2(0, 0));
        #else
            EnvVoro vor = (EnvVoro)0;
        #endif
            float h = EnvHeightLOD0At(tuv, vor);
            return positionOS + normalOS * ((h - _HeightRef) * _VertexDispScale);
        }
        #endif

        
        EnvSurface EvaluateEnvMaterial(float2 uv, float3 positionWS, float3 normalWS,
                                       float4 tangentWS, float3 viewVectorWS, float2 sinCos)
        {
            EnvSurface o;

            float4 R   = EnvRotMatrix(sinCos);
            float2 tuv = EnvTransformUV(EnvGetBaseUV(uv, positionWS), R);

        #if defined(USE_NORMAL_MAP) || defined(ENV_PARALLAX)
            float3 N = normalize(normalWS);
            float3 T, B;
            EnvGetTangentFrame(N, tangentWS, T, B);
        #endif

        #ifdef ENV_NEED_GRAD
            
            float2 gradX = ddx(tuv);
            float2 gradY = ddy(tuv);
        #else
            float2 gradX = 0, gradY = 0;
        #endif

            
        #ifdef USE_VORONOI
            EnvVoro vor = EnvVoronoiBuild(tuv, gradX, gradY);
        #else
            EnvVoro vor = (EnvVoro)0;
        #endif

        #ifdef ENV_PARALLAX
            float  dist2   = dot(viewVectorWS, viewVectorWS);
            float  invDist = rsqrt(max(dist2, 1e-8));
            float3 V       = viewVectorWS * invDist;
            float3 viewTS  = float3(dot(V, T), dot(V, B), dot(V, N));

            float distFade = (_FadeRange > 0.0)
                           ? saturate(1.0 - (dist2 * invDist - _FadeStart) / _FadeRange)
                           : 1.0;

            tuv = EnvParallaxUV(tuv, viewTS, distFade, R, vor, gradX, gradY);
        #endif

        #if defined(USE_VORONOI)
            #define ENV_SAMPLE(tex, uvv) EnvSampleVoro(TEXTURE2D_ARGS(tex, ENV_SAMPLER), uvv, vor)
        #elif defined(ENV_PARALLAX)
            #define ENV_SAMPLE(tex, uvv) SAMPLE_TEXTURE2D_GRAD(tex, ENV_SAMPLER, uvv, gradX, gradY)
        #else
            #define ENV_SAMPLE(tex, uvv) SAMPLE_TEXTURE2D(tex, ENV_SAMPLER, uvv)
        #endif

            
            o.albedo = ENV_SAMPLE(_AlbedoTex, tuv).rgb * _BaseColor.rgb;

            
        #ifdef USE_ARM_MAP
            float3 arm = ENV_SAMPLE(_ARMTex, tuv).rgb;
            float  rg  = arm.g;
          #ifdef USE_ROUGH_INVERT
            rg = 1.0 - rg;                      
          #endif
            o.ao        = 1.0 + (arm.r - 1.0) * _AOStrength;
            o.roughness = lerp(_RoughMin, _RoughMax, rg);
            o.metallic  = lerp(_MetalMin, _MetalMax, arm.b);
        #else
            o.ao        = 1.0;
            o.roughness = _RoughConst;
            o.metallic  = _MetalConst;
        #endif

            
        #ifdef USE_NORMAL_MAP
          #ifdef USE_VORONOI
            float3 nTS = EnvSampleNormalVoro(tuv, vor);
          #else
            float3 nTS = EnvUnpackNormal(ENV_SAMPLE(_NormalTex, tuv));
          #endif
            
            nTS.xy = mul(nTS.xy, float2x2(R.x, R.y, R.z, R.w));
            o.normalWS = normalize(T * nTS.x + B * nTS.y + N * nTS.z);
        #else
            o.normalWS = normalize(normalWS);
        #endif

            
        #ifdef USE_EMISSIVE
            o.emissive = _EmissionColor.rgb;
        #else
            o.emissive = 0.0;
        #endif

            #undef ENV_SAMPLE
            return o;
        }
        ENDHLSL

        
        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForward" }

            ZWrite On
            Cull Back

            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex vert
            #pragma fragment frag

            #pragma shader_feature_local _UVMODE_MESH _UVMODE_WORLDXZ _UVMODE_WORLDXY _UVMODE_WORLDZY
            #pragma shader_feature_local _DISP_OFF _DISP_OFFSET _DISP_POM _DISP_VERTEX
            #pragma shader_feature_local _VORONOI
            #pragma shader_feature_local _NORMALMAP
            #pragma shader_feature_local _NORMAL_FLIPY
            #pragma shader_feature_local _ARMMAP
            #pragma shader_feature_local _ROUGH_INVERT
            #pragma shader_feature_local _HEIGHT_IN_ARM_A
            #pragma shader_feature_local _EMISSION

            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile _ _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS
            #pragma multi_compile _ _CLUSTER_LIGHT_LOOP
            #pragma multi_compile_fragment _ _ADDITIONAL_LIGHT_SHADOWS
            #pragma multi_compile_fragment _ _SHADOWS_SOFT
            #pragma multi_compile _ LIGHTMAP_ON
            #pragma multi_compile _ DIRLIGHTMAP_COMBINED
            #pragma multi_compile_fog
            #pragma multi_compile_instancing

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS   : NORMAL;
                float4 tangentOS  : TANGENT;
                float2 uv         : TEXCOORD0;
                float2 lightmapUV : TEXCOORD1;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv         : TEXCOORD0;
                float3 positionWS : TEXCOORD1;
                float3 normalWS   : TEXCOORD2;
                float4 tangentWS  : TEXCOORD3;
                half   fogFactor  : TEXCOORD4;
                half3  vertexLight: TEXCOORD5;
                DECLARE_LIGHTMAP_OR_SH(staticLightmapUV, vertexSH, 6);
                nointerpolation float2 sinCos : TEXCOORD7;
                UNITY_VERTEX_INPUT_INSTANCE_ID
                UNITY_VERTEX_OUTPUT_STEREO
            };

            Varyings vert(Attributes input)
            {
                Varyings output = (Varyings)0;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_TRANSFER_INSTANCE_ID(input, output);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

                float3 posOS = input.positionOS.xyz;
            #ifdef USE_VERTEX_DISPLACEMENT
                posOS = EnvDisplaceVertex(posOS, input.normalOS, input.uv, TransformObjectToWorld(posOS));
            #endif

                VertexPositionInputs vInput = GetVertexPositionInputs(posOS);
                VertexNormalInputs   nInput = GetVertexNormalInputs(input.normalOS, input.tangentOS);

                output.positionCS = vInput.positionCS;
                output.positionWS = vInput.positionWS;
                output.uv         = input.uv;
                output.normalWS   = nInput.normalWS;
                output.tangentWS  = float4(nInput.tangentWS, input.tangentOS.w * GetOddNegativeScale());
                output.fogFactor  = ComputeFogFactor(vInput.positionCS.z);
                output.sinCos     = EnvSinCos();

            #ifdef _ADDITIONAL_LIGHTS_VERTEX
                output.vertexLight = VertexLighting(vInput.positionWS, nInput.normalWS);
            #endif

                OUTPUT_LIGHTMAP_UV(input.lightmapUV, unity_LightmapST, output.staticLightmapUV);
                OUTPUT_SH(output.normalWS.xyz, output.vertexSH);
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

                float3 viewVectorWS = GetWorldSpaceViewDir(input.positionWS);
                EnvSurface s = EvaluateEnvMaterial(input.uv, input.positionWS, input.normalWS,
                                                   input.tangentWS, viewVectorWS, input.sinCos);

                InputData inputData = (InputData)0;
                inputData.positionWS      = input.positionWS;
                inputData.normalWS        = s.normalWS;
                inputData.viewDirectionWS = SafeNormalize(viewVectorWS);
            #if defined(MAIN_LIGHT_CALCULATE_SHADOWS)
                inputData.shadowCoord     = TransformWorldToShadowCoord(inputData.positionWS);
            #else
                inputData.shadowCoord     = float4(0, 0, 0, 0);
            #endif
                inputData.fogCoord        = InitializeInputDataFog(float4(input.positionWS, 1.0), input.fogFactor);
            #ifdef _ADDITIONAL_LIGHTS_VERTEX
                inputData.vertexLighting  = input.vertexLight;
            #else
                inputData.vertexLighting  = half3(0, 0, 0);
            #endif
                inputData.bakedGI         = SAMPLE_GI(input.staticLightmapUV, input.vertexSH, inputData.normalWS);
                inputData.normalizedScreenSpaceUV = GetNormalizedScreenSpaceUV(input.positionCS);
                inputData.shadowMask      = SAMPLE_SHADOWMASK(input.staticLightmapUV);

                SurfaceData surfaceData = (SurfaceData)0;
                surfaceData.albedo     = s.albedo;
                surfaceData.metallic   = s.metallic;
                surfaceData.specular   = half3(0, 0, 0);
                surfaceData.smoothness = 1.0 - s.roughness;
                surfaceData.occlusion  = s.ao;
                surfaceData.emission   = s.emissive;
                surfaceData.alpha      = 1.0;
                surfaceData.normalTS   = half3(0, 0, 1);

                half4 color = UniversalFragmentPBR(inputData, surfaceData);
                color.rgb = MixFog(color.rgb, inputData.fogCoord);
                color.a = 1.0;
                return color;
            }
            ENDHLSL
        }

       
        Pass
        {
            Name "ShadowCaster"
            Tags { "LightMode" = "ShadowCaster" }

            ZWrite On
            ZTest LEqual
            ColorMask 0
            Cull Back

            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex ShadowVert
            #pragma fragment ShadowFrag

            #pragma shader_feature_local _UVMODE_MESH _UVMODE_WORLDXZ _UVMODE_WORLDXY _UVMODE_WORLDZY
            #pragma shader_feature_local _DISP_OFF _DISP_OFFSET _DISP_POM _DISP_VERTEX
            #pragma shader_feature_local _VORONOI
            #pragma shader_feature_local _HEIGHT_IN_ARM_A
            #pragma multi_compile_instancing
            #pragma multi_compile_vertex _ _CASTING_PUNCTUAL_LIGHT_SHADOW

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Shadows.hlsl"

            float3 _LightDirection;
            float3 _LightPosition;

            struct ShadowAttributes
            {
                float4 positionOS : POSITION;
                float3 normalOS   : NORMAL;
                float2 uv         : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct ShadowVaryings
            {
                float4 positionCS : SV_POSITION;
                UNITY_VERTEX_INPUT_INSTANCE_ID
                UNITY_VERTEX_OUTPUT_STEREO
            };

            ShadowVaryings ShadowVert(ShadowAttributes input)
            {
                ShadowVaryings output = (ShadowVaryings)0;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_TRANSFER_INSTANCE_ID(input, output);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

                float3 posOS = input.positionOS.xyz;
            #ifdef USE_VERTEX_DISPLACEMENT
                posOS = EnvDisplaceVertex(posOS, input.normalOS, input.uv, TransformObjectToWorld(posOS));
            #endif

                float3 positionWS = TransformObjectToWorld(posOS);
                float3 normalWS   = TransformObjectToWorldNormal(input.normalOS);

            #if _CASTING_PUNCTUAL_LIGHT_SHADOW
                float3 lightDirectionWS = normalize(_LightPosition - positionWS);
            #else
                float3 lightDirectionWS = _LightDirection;
            #endif

                float4 positionCS = TransformWorldToHClip(ApplyShadowBias(positionWS, normalWS, lightDirectionWS));
            #if UNITY_REVERSED_Z
                positionCS.z = min(positionCS.z, UNITY_NEAR_CLIP_VALUE);
            #else
                positionCS.z = max(positionCS.z, UNITY_NEAR_CLIP_VALUE);
            #endif
                output.positionCS = positionCS;
                return output;
            }

            half4 ShadowFrag(ShadowVaryings input) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(input);
                return 0;
            }
            ENDHLSL
        }

        
        Pass
        {
            Name "DepthOnly"
            Tags { "LightMode" = "DepthOnly" }

            ZWrite On
            ColorMask R
            Cull Back

            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex DepthVert
            #pragma fragment DepthFrag

            #pragma shader_feature_local _UVMODE_MESH _UVMODE_WORLDXZ _UVMODE_WORLDXY _UVMODE_WORLDZY
            #pragma shader_feature_local _DISP_OFF _DISP_OFFSET _DISP_POM _DISP_VERTEX
            #pragma shader_feature_local _VORONOI
            #pragma shader_feature_local _HEIGHT_IN_ARM_A
            #pragma multi_compile_instancing

            struct DepthAttributes
            {
                float4 positionOS : POSITION;
                float3 normalOS   : NORMAL;
                float2 uv         : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct DepthVaryings
            {
                float4 positionCS : SV_POSITION;
                UNITY_VERTEX_INPUT_INSTANCE_ID
                UNITY_VERTEX_OUTPUT_STEREO
            };

            DepthVaryings DepthVert(DepthAttributes input)
            {
                DepthVaryings output = (DepthVaryings)0;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_TRANSFER_INSTANCE_ID(input, output);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

                float3 posOS = input.positionOS.xyz;
            #ifdef USE_VERTEX_DISPLACEMENT
                posOS = EnvDisplaceVertex(posOS, input.normalOS, input.uv, TransformObjectToWorld(posOS));
            #endif
                output.positionCS = TransformObjectToHClip(posOS);
                return output;
            }

            half DepthFrag(DepthVaryings input) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(input);
                return input.positionCS.z;
            }
            ENDHLSL
        }

        
        Pass
        {
            Name "DepthNormals"
            Tags { "LightMode" = "DepthNormals" }

            ZWrite On
            Cull Back

            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex DNVert
            #pragma fragment DNFrag

            #pragma shader_feature_local _UVMODE_MESH _UVMODE_WORLDXZ _UVMODE_WORLDXY _UVMODE_WORLDZY
            #pragma shader_feature_local _DISP_OFF _DISP_OFFSET _DISP_POM _DISP_VERTEX
            #pragma shader_feature_local _VORONOI
            #pragma shader_feature_local _HEIGHT_IN_ARM_A
            #pragma multi_compile_fragment _ _GBUFFER_NORMALS_OCT
            #pragma multi_compile_instancing

            struct DNAttributes
            {
                float4 positionOS : POSITION;
                float3 normalOS   : NORMAL;
                float2 uv         : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct DNVaryings
            {
                float4 positionCS : SV_POSITION;
                float3 normalWS   : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
                UNITY_VERTEX_OUTPUT_STEREO
            };

            DNVaryings DNVert(DNAttributes input)
            {
                DNVaryings output = (DNVaryings)0;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_TRANSFER_INSTANCE_ID(input, output);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

                float3 posOS = input.positionOS.xyz;
            #ifdef USE_VERTEX_DISPLACEMENT
                posOS = EnvDisplaceVertex(posOS, input.normalOS, input.uv, TransformObjectToWorld(posOS));
            #endif
                output.positionCS = TransformObjectToHClip(posOS);
                output.normalWS   = TransformObjectToWorldNormal(input.normalOS);
                return output;
            }

            half4 DNFrag(DNVaryings input) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(input);
                float3 n = normalize(input.normalWS);
            #if defined(_GBUFFER_NORMALS_OCT)
                float2 oct = PackNormalOctQuadEncode(n);
                float2 remapped = saturate(oct * 0.5 + 0.5);
                return half4(PackFloat2To888(remapped), 0.0);
            #else
                return half4(n, 0.0);
            #endif
            }
            ENDHLSL
        }
    }

    Fallback "Hidden/Universal Render Pipeline/FallbackError"
}
