// =============================================================================
//  Instrucciones de uso
//
//  UN shader, UN selector "Projection":
//     Mesh      -> UV0 de la malla (props, paredes modulares, todo lo que ya tiene UVs)
//     WorldXZ   -> planar en mundo, mirando a +Y (suelos, terreno plano)
//     WorldXY   -> planar en mundo, mirando a +Z (paredes)
//     WorldZY   -> planar en mundo, mirando a +X (paredes)
//     Triplanar -> proyeccion triplanar (rocas, terreno, mallas sin UVs o con UVs malas)
//
//  Caracteristicas (todas disponibles en TODOS los modos):
//   Albedo, Normal, ARM (One Minus para gloss, sliders Min/Max), Height con
//   Offset / POM / Vertex displacement, Voronoi anti-tiling, Emision con fuente
//   Texture / alpha del Albedo / alpha de la ARM, tiling, rotacion y offset.
//
//  Costo: cada modo compila SOLO su codigo (shader_feature_local). Un material en
//  Mesh o World* hace 1 muestra por textura, igual que un shader UV normal; el
//  triplanar solo entra cuando eliges Triplanar.
//
//  Triplanar correcto (para que no se vea mal):
//   1. La U de cada eje se voltea segun el signo de la normal: sin esto las caras
//      opuestas (-X, -Y, -Z) salen espejadas.
//   2. Las normales de cada proyeccion se corrigen con el mismo signo y se
//      combinan en espacio de proyeccion (Whiteout / RNM / UDN).
//   3. Los gradientes (mipmaps) salen de la derivada de la posicion con el signo
//      ya aplicado: no hay mips corruptos donde la normal cambia de signo.
//   4. Cada eje usa un offset distinto para que la mezcla no repita el patron.
//   5. Pesos con potencia + cutoff relativo al eje dominante (sin zonas negras).
//   6. Los ejes con peso bajo NO se muestrean (branch dinamico con SampleGrad).
//
//  Notas:
//   - En Triplanar y World* la proyeccion usa la malla SIN desplazar: la textura
//     se queda pegada a la geometria aunque uses Vertex displacement.
//   - Tiling en Triplanar y World* = repeticiones por unidad (1 = 1 por metro en
//     World). En Mesh depende de las UVs del modelo.
//   - Para props que se mueven o rotan usa Triplanar con Projection Space =
//     Object u ObjectScaled (la textura se queda pegada al objeto).
//   - Deja los ajustes de Triplanar en su valor por defecto si no lo usas: asi no
//     se generan variantes de shader innecesarias.
// =============================================================================
Shader "Env/Universal environment Material (UV + Triplanar)"
{
    Properties
    {
        [Header(Base)]
        _BaseColor("Tint", Color) = (1,1,1,1)
        [MainTexture][NoScaleOffset] _AlbedoTex("Albedo", 2D) = "white" {}

        [Header(Projection)]
        [KeywordEnum(Mesh, WorldXZ, WorldXY, WorldZY, Triplanar)] _Proj("Projection", Float) = 0

        [Header(Triplanar  solo con Projection Triplanar)]
        [KeywordEnum(World, Object, ObjectScaled)] _TriSpace("Projection Space", Float) = 0
        _TriSharpness("Blend Sharpness", Range(1, 32)) = 4
        _TriBlendOffset("Blend Cutoff", Range(0, 0.9)) = 0
        _TriAxisWeights("Axis Weights X Y Z", Vector) = (1,1,1,0)
        _TriCull("Skip Axis Below", Range(0, 0.5)) = 0.02
        [Toggle(_TRI_TWO_AXES)] _TriTwoAxes("Max 2 Axes (faster)", Float) = 0
        _TriAxisOffset("Axis Decorrelation Offset", Range(0, 1)) = 1
        [KeywordEnum(Whiteout, RNM, UDN)] _TriNormalBlend("Normal Blend Method", Float) = 0
        [Toggle(_TRI_HEIGHTBLEND)] _TriHeightBlend("Height Based Blend", Float) = 0
        _TriHeightStrength("Height Influence", Range(0, 4)) = 1
        _TriHeightDepth("Height Blend Softness", Range(0.01, 1)) = 0.2

        [Header(UV  se aplica a la proyeccion elegida)]
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
        _RoughMin("Roughness Min", Range(0, 1)) = 0
        _RoughMax("Roughness Max", Range(0, 1)) = 1
        _MetalMin("Metallic Min", Range(0, 1)) = 0
        _MetalMax("Metallic Max", Range(0, 1)) = 1
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

        [Header(Emission)]
        [Toggle(_EMISSION)] _UseEmission("Emission", Float) = 0
        [KeywordEnum(Texture, AlbedoAlpha, ARMAlpha)] _EmissionSrc("Emission Source (mask)", Float) = 0
        [NoScaleOffset] _EmissionTex("Emission Map", 2D) = "black" {}
        [HDR] _EmissionColor("Emission Color", Color) = (1,1,1,1)
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

        // =====================================================================
        //  CODIGO COMPARTIDO por todos los pases
        // =====================================================================
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
            #if defined(_EMISSIONSRC_ALBEDOALPHA)
                #define EMISSION_ALBEDO_A 1
            #elif defined(_EMISSIONSRC_ARMALPHA)
                #define EMISSION_ARM_A 1
            #else
                #define EMISSION_TEX 1
            #endif
        #endif
        #if defined(_HEIGHT_IN_ARM_A)
            #define HEIGHT_IN_ARM_A 1
        #endif
        #if defined(_VORONOI)
            #define USE_VORONOI 1
        #endif

        // ---- Proyeccion
        //  ENV_TRIPLANAR : triplanar
        //  ENV_UV_WORLD  : planar en mundo (UV derivada de la posicion)
        //  ENV_UV_MESH   : UV0 de la malla
        #if defined(_PROJ_TRIPLANAR)
            #define ENV_TRIPLANAR 1
            #if defined(_TRISPACE_OBJECTSCALED)
                #define ENV_PSPACE_OBJSCALED 1
            #elif defined(_TRISPACE_OBJECT)
                #define ENV_PSPACE_OBJECT 1
            #endif
            #if defined(_TRI_TWO_AXES)
                #define USE_TRI_TWO_AXES 1
            #endif
            #if defined(_TRI_HEIGHTBLEND)
                #define USE_TRI_HEIGHTBLEND 1
            #endif
        #elif defined(_PROJ_WORLDXZ)
            #define USE_WORLD_UV_XZ 1
            #define ENV_UV_WORLD 1
        #elif defined(_PROJ_WORLDXY)
            #define USE_WORLD_UV_XY 1
            #define ENV_UV_WORLD 1
        #elif defined(_PROJ_WORLDZY)
            #define USE_WORLD_UV_ZY 1
            #define ENV_UV_WORLD 1
        #else
            #define ENV_UV_MESH 1
        #endif

        #if !defined(ENV_PSPACE_OBJSCALED) && !defined(ENV_PSPACE_OBJECT)
            #define ENV_PSPACE_WORLD 1
        #endif

        // ---- Displacement
        #if defined(_DISP_OFFSET)
            #define USE_PARALLAX_OFFSET 1
            #define ENV_PARALLAX 1
        #elif defined(_DISP_POM)
            #define USE_POM 1
            #define ENV_PARALLAX 1
        #elif defined(_DISP_VERTEX)
            #define USE_VERTEX_DISPLACEMENT 1
        #endif

        #if defined(ENV_PARALLAX) || defined(USE_VERTEX_DISPLACEMENT) || defined(USE_TRI_HEIGHTBLEND)
            #define ENV_USE_HEIGHT 1
        #endif

        // ---- Recursos (un solo sampler: el del Albedo)
        TEXTURE2D(_AlbedoTex);  SAMPLER(sampler_AlbedoTex);
        TEXTURE2D(_NormalTex);
        TEXTURE2D(_ARMTex);
        TEXTURE2D(_HeightTex);
        TEXTURE2D(_EmissionTex);
        #define ENV_SAMPLER sampler_AlbedoTex

        // ---- Propiedades (SRP Batcher)
        CBUFFER_START(UnityPerMaterial)
            float4 _BaseColor;
            float4 _TriAxisWeights;
            float  _Proj;
            float  _TriSpace, _TriSharpness, _TriBlendOffset, _TriCull, _TriTwoAxes, _TriAxisOffset;
            float  _TriNormalBlend, _TriHeightBlend, _TriHeightStrength, _TriHeightDepth;
            float  _TilingX, _TilingY, _Rotation, _OffsetX, _OffsetY;
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
            float  _UseEmission, _EmissionSrc;
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

        // ---- Espacio de proyeccion ------------------------------------------
        float3 EnvObjectScale()
        {
            float4x4 m = GetObjectToWorldMatrix();
            return max(float3(length(float3(m[0][0], m[1][0], m[2][0])),
                              length(float3(m[0][1], m[1][1], m[2][1])),
                              length(float3(m[0][2], m[1][2], m[2][2]))), 1e-5);
        }

        // Posicion y normal en el espacio donde se proyecta la textura.
        // Siempre con la malla SIN desplazar: la textura se queda pegada a la malla.
        // (En Mesh UV estos valores no se usan y el compilador los elimina.)
        void EnvProjInputs(float3 posOS, float3 normalOS, out float3 posP, out float3 nP)
        {
        #if defined(ENV_PSPACE_OBJSCALED)
            float3 sc = EnvObjectScale();
            posP = posOS * sc;
            nP   = normalize(normalOS / sc);      // normal bajo transformacion de escala
        #elif defined(ENV_PSPACE_OBJECT)
            posP = posOS;
            nP   = normalOS;
        #else
            posP = TransformObjectToWorld(posOS);
            nP   = TransformObjectToWorldNormal(normalOS);
        #endif
        }

        // ---- UV -----------------------------------------------------------
        // UV base del modo UV (Mesh o planar en mundo)
        float2 EnvGetBaseUV(float2 uv, float3 posP)
        {
        #if defined(USE_WORLD_UV_XZ)
            return posP.xz;
        #elif defined(USE_WORLD_UV_XY)
            return posP.xy;
        #elif defined(USE_WORLD_UV_ZY)
            return posP.zy;
        #else
            return uv;
        #endif
        }

        // (sin, cos) de la rotacion global: se calcula por VERTICE y llega sin interpolar
        float2 EnvSinCos()
        {
            float s, c;
            sincos(radians(_Rotation), s, c);
            return float2(s, c);
        }

        float4 EnvRotMatrix(float2 sc) { return float4(sc.y, -sc.x, sc.x, sc.y); }

        // uv' = (Rot * Tiling) * uv + offset    (2 dot + 1 add)
        float2 EnvTransformUV(float2 uv, float4 R)
        {
            float4 M = R * float4(_TilingX, _TilingY, _TilingX, _TilingY);
            return float2(dot(uv, M.xy), dot(uv, M.zw)) + float2(_OffsetX, _OffsetY);
        }

        // Misma transformacion para vectores (derivadas): sin offset
        float2 EnvTransformDir(float2 d, float4 R)
        {
            float4 M = R * float4(_TilingX, _TilingY, _TilingX, _TilingY);
            return float2(dot(d, M.xy), dot(d, M.zw));
        }

        // Base tangente del modo UV. En planar de mundo se deduce del eje de
        // proyeccion (pensado para caras hacia +Y en XZ, +Z en XY y +X en ZY).
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

        #if defined(ENV_UV_WORLD)
            T = normalize(T - N * dot(N, T));
        #elif defined(ENV_PARALLAX)
            T = normalize(T);
        #endif
            B = cross(N, T) * sgn;
        }

        // ---- Voronoi ------------------------------------------------------
        // Se buscan las DOS celdas mas cercanas. Cada celda remapea la textura con
        // su propio offset / rotacion / escala alrededor del CENTRO de la textura.
        // Cerca del borde entre celdas se mezclan A y B (solo ahi hay 2 samples).
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

        #endif // USE_VORONOI

        // ---- Normal -------------------------------------------------------
        float3 EnvUnpackNormal(float4 s)
        {
            // UnpackNormalScale maneja BC5 / DXT5nm / ASTC segun plataforma
            float3 n = UnpackNormalScale(s, _NormalStrength);
        #ifdef NORMAL_MAP_FLIP_Y
            n.y = -n.y;
        #endif
            return n;
        }

        #if defined(USE_VORONOI) && defined(USE_NORMAL_MAP)
        // Cada muestra gira su normal con la rotacion de SU celda antes de mezclar
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

        // ---- Height -------------------------------------------------------
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

        // Height en una UV arbitraria usando las celdas voronoi YA elegidas para el
        // pixel (A y B se mezclan con el mismo peso -> el parallax es continuo).
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
        #endif // ENV_USE_HEIGHT

        #ifdef ENV_PARALLAX
        float2 EnvParallaxUV(float2 tuv, float3 viewTS, float distFade, float4 R,
                             EnvVoro vor, float2 gx, float2 gy)
        {
            float scale = _HeightScale * distFade;
            float refPl = _HeightRef;

            [branch]
            if (distFade < 0.001) return tuv;   // lejos: costo cero

        #if defined(USE_POM)
            float2 shiftTS = viewTS.xy / max(viewTS.z, 0.1) * scale;
            float2 shiftUV = float2(dot(R.xy, shiftTS), dot(R.zw, shiftTS));

            // Pasos adaptativos: de frente = pocos, rasante = muchos
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
        #endif // ENV_PARALLAX

        // =====================================================================
        //  PROYECCION: contexto de UNA proyeccion (UV, o eje X / Y / Z del triplanar)
        // =====================================================================
        struct EnvAxis
        {
            float2 uv;      // UV transformada (tiling / rotacion / offset)
            float2 gx, gy;  // gradientes de esa UV
            float  flipU;   // signo aplicado a la U base (para corregir normales)
            float3 vTS;     // vector de vista en el espacio tangente de esta proyeccion
        };

        // Construye la proyeccion del eje 'a' (0 = X, 1 = Y, 2 = Z) del triplanar.
        //  Convencion de Unity (mano izquierda): para que la textura no salga
        //  espejada en caras opuestas la U se multiplica por:
        //    eje X (uv = z, y): +signo(nx)     eje Y (uv = x, z): +signo(ny)
        //    eje Z (uv = x, y): -signo(nz)
        //  vTS = (dot(V,T), dot(V,B), dot(V,N)) con T, B las direcciones en el
        //  espacio de proyeccion de +u y +v, y N la normal del eje hacia el lado visible.
        EnvAxis EnvMakeAxis(int a, float3 p, float3 dpx, float3 dpy, float3 sgn, float3 V, float4 R)
        {
            EnvAxis o;
            float2 b, bx, by, off;

            if (a == 0)
            {
                b  = float2(p.z   * sgn.x, p.y);
                bx = float2(dpx.z * sgn.x, dpx.y);
                by = float2(dpy.z * sgn.x, dpy.y);
                o.flipU = sgn.x;
                o.vTS   = float3(sgn.x * V.z, V.y, sgn.x * V.x);
                off     = float2(0.0, 0.0);
            }
            else if (a == 1)
            {
                b  = float2(p.x   * sgn.y, p.z);
                bx = float2(dpx.x * sgn.y, dpx.z);
                by = float2(dpy.x * sgn.y, dpy.z);
                o.flipU = sgn.y;
                o.vTS   = float3(sgn.y * V.x, V.z, sgn.y * V.y);
                off     = float2(0.33, 0.33);
            }
            else
            {
                float s = -sgn.z;
                b  = float2(p.x   * s, p.y);
                bx = float2(dpx.x * s, dpx.y);
                by = float2(dpy.x * s, dpy.y);
                o.flipU = s;
                o.vTS   = float3(s * V.x, V.y, sgn.z * V.z);
                off     = float2(0.67, 0.67);
            }

            o.uv = EnvTransformUV(b, R) + off * _TriAxisOffset;
            o.gx = EnvTransformDir(bx, R);
            o.gy = EnvTransformDir(by, R);
            return o;
        }

        // Normaliza y descarta ejes con peso menor al umbral (el dominante siempre se conserva)
        float3 EnvNormalizeCull(float3 w)
        {
            w /= max(w.x + w.y + w.z, 1e-5);
            float mx = max(max(w.x, w.y), w.z);
            w *= step(min(_TriCull, mx), w);
            return w / max(w.x + w.y + w.z, 1e-5);
        }

        // Pesos de mezcla por eje (solo dependen de la normal)
        float3 EnvTriWeights(float3 nP)
        {
            float3 a  = abs(nP);
            float  mx = max(max(a.x, a.y), a.z);
            float3 r  = a / max(mx, 1e-5);                               // el eje dominante vale 1
            r = saturate((r - _TriBlendOffset) / max(1.0 - _TriBlendOffset, 1e-3));
            float3 w  = pow(r, _TriSharpness) * _TriAxisWeights.xyz;

        #ifdef USE_TRI_TWO_AXES
            // quitar solo el eje mas debil (maximo 2 proyecciones por pixel)
            float mn = min(min(w.x, w.y), w.z);
            if (w.z <= mn)      w.z = 0.0;
            else if (w.y <= mn) w.y = 0.0;
            else                w.x = 0.0;
        #endif
            return EnvNormalizeCull(w);
        }

        // Mezcla de normales de una proyeccion. 't' = normal tangente ya corregida
        // (rotacion y flip de U aplicados). Devuelve la normal en espacio de proyeccion.
        float3 EnvBlendAxisNormal(int a, float3 t, float3 nP)
        {
            // normal de la malla "swizzleada" al espacio tangente de este eje
            float3 nT = (a == 0) ? nP.zyx : ((a == 1) ? nP.xzy : nP.xyz);
            float3 t2;

        #if defined(_TRINORMALBLEND_RNM)
            // Reoriented Normal Mapping
            float3 n1 = float3(nT.xy, abs(nT.z));
            n1.z += 1.0;
            float3 n2 = float3(-t.xy, t.z);
            t2 = n1 * dot(n1, n2) / n1.z - n2;
            t2.z *= (nT.z < 0.0) ? -1.0 : 1.0;
        #elif defined(_TRINORMALBLEND_UDN)
            t2 = float3(t.xy + nT.xy, nT.z);
        #else
            // Whiteout (por defecto)
            t2 = float3(t.xy + nT.xy, abs(t.z) * nT.z);
        #endif

            // de espacio tangente del eje de vuelta al espacio de proyeccion
            return (a == 0) ? t2.zyx : ((a == 1) ? t2.xzy : t2.xyz);
        }

        // Resultado de muestrear todas las texturas en UNA proyeccion
        struct EnvAxisOut
        {
            float4 albedo;
            float4 arm;
            float3 nTS;
            float3 emis;
        };

        EnvAxisOut EnvEvalAxis(EnvAxis ax, float4 R, float distFade)
        {
            EnvAxisOut o = (EnvAxisOut)0;
            float2 tuv = ax.uv;

        #ifdef USE_VORONOI
            EnvVoro vor = EnvVoronoiBuild(tuv, ax.gx, ax.gy);
        #else
            EnvVoro vor = (EnvVoro)0;
        #endif

        #ifdef ENV_PARALLAX
            tuv = EnvParallaxUV(tuv, ax.vTS, distFade, R, vor, ax.gx, ax.gy);
        #endif

        #if defined(USE_VORONOI)
            #define ENV_SAMPLE(tex, uvv) EnvSampleVoro(TEXTURE2D_ARGS(tex, ENV_SAMPLER), uvv, vor)
        #elif defined(ENV_TRIPLANAR) || defined(ENV_PARALLAX)
            #define ENV_SAMPLE(tex, uvv) SAMPLE_TEXTURE2D_GRAD(tex, ENV_SAMPLER, uvv, ax.gx, ax.gy)
        #else
            // Modo UV simple: muestreo normal (hardware calcula los gradientes)
            #define ENV_SAMPLE(tex, uvv) SAMPLE_TEXTURE2D(tex, ENV_SAMPLER, uvv)
        #endif

            o.albedo = ENV_SAMPLE(_AlbedoTex, tuv);

        #if defined(USE_ARM_MAP) || defined(EMISSION_ARM_A)
            o.arm = ENV_SAMPLE(_ARMTex, tuv);
        #endif

        #ifdef USE_NORMAL_MAP
          #ifdef USE_VORONOI
            float3 nTS = EnvSampleNormalVoro(tuv, vor);
          #else
            float3 nTS = EnvUnpackNormal(ENV_SAMPLE(_NormalTex, tuv));
          #endif
            // 1) girar la normal junto con la rotacion global (inversa = transpuesta)
            nTS.xy = mul(nTS.xy, float2x2(R.x, R.y, R.z, R.w));
            // 2) corregir el flip de U de esta proyeccion (1 en modo UV)
            nTS.x *= ax.flipU;
            o.nTS = nTS;
        #endif

        #if defined(EMISSION_TEX)
            o.emis = ENV_SAMPLE(_EmissionTex, tuv).rgb;
        #endif

            #undef ENV_SAMPLE
            return o;
        }

        // Acumulador ponderado de las proyecciones
        struct EnvAcc
        {
            float4 alb;
            float4 arm;
            float3 emis;
            float3 n;
        };

        void EnvAccumAxis(inout EnvAcc acc, int idx, EnvAxis ax, float wgt,
                          float4 R, float distFade, float3 nP)
        {
            EnvAxisOut r = EnvEvalAxis(ax, R, distFade);
            acc.alb  += wgt * r.albedo;
            acc.arm  += wgt * r.arm;
            acc.emis += wgt * r.emis;
        #ifdef USE_NORMAL_MAP
            acc.n    += wgt * EnvBlendAxisNormal(idx, r.nTS, nP);
        #endif
        }

        // ---- Vertex displacement --------------------------------------------
        #ifdef USE_VERTEX_DISPLACEMENT
        float EnvAxisHeightLOD0(EnvAxis ax)
        {
        #ifdef USE_VORONOI
            EnvVoro vor = EnvVoronoiBuild(ax.uv, float2(0, 0), float2(0, 0));
        #else
            EnvVoro vor = (EnvVoro)0;
        #endif
            return EnvHeightLOD0At(ax.uv, vor);
        }

        // posP / nPIn salen de EnvProjInputs (malla sin desplazar)
        float3 EnvDisplaceVertex(float3 posOS, float3 normalOS, float2 uv, float3 posP, float3 nPIn)
        {
            float4 R = EnvRotMatrix(EnvSinCos());
            float  h = 0.0;

        #if defined(ENV_TRIPLANAR)
            float3 nP  = normalize(nPIn);
            float3 sgn = step(0.0, nP) * 2.0 - 1.0;
            float3 w   = EnvTriWeights(nP);
            float3 z   = float3(0, 0, 0);
            float3 V   = float3(0, 0, 1);

            [branch] if (w.x > 0.0) { EnvAxis a = EnvMakeAxis(0, posP, z, z, sgn, V, R); h += w.x * EnvAxisHeightLOD0(a); }
            [branch] if (w.y > 0.0) { EnvAxis a = EnvMakeAxis(1, posP, z, z, sgn, V, R); h += w.y * EnvAxisHeightLOD0(a); }
            [branch] if (w.z > 0.0) { EnvAxis a = EnvMakeAxis(2, posP, z, z, sgn, V, R); h += w.z * EnvAxisHeightLOD0(a); }
        #else
            EnvAxis a;
            a.uv    = EnvTransformUV(EnvGetBaseUV(uv, posP), R);
            a.gx    = float2(0, 0);
            a.gy    = float2(0, 0);
            a.flipU = 1.0;
            a.vTS   = float3(0, 0, 1);
            h = EnvAxisHeightLOD0(a);
        #endif

            return posOS + normalOS * ((h - _HeightRef) * _VertexDispScale);
        }
        #endif

        // ---- Evaluacion del material ----------------------------------------
        //   uv           : UV0 (solo Mesh)
        //   posP / nPIn  : posicion y normal en el espacio de proyeccion
        //   normalWS     : normal de la malla en mundo
        //   tangentWS    : xyz tangente, w signo (solo Mesh con normal map / parallax)
        //   viewVectorWS : camaraPos - positionWS (sin normalizar)
        EnvSurface EvaluateEnvMaterial(float2 uv, float3 posP, float3 nPIn, float3 normalWS,
                                       float4 tangentWS, float3 viewVectorWS, float2 sinCos)
        {
            EnvSurface o;

            float4 R = EnvRotMatrix(sinCos);

            // Vista en mundo + fade por distancia (solo parallax)
            float  distFade = 1.0;
            float3 vWS      = float3(0, 0, 1);
        #ifdef ENV_PARALLAX
            float  dist2   = dot(viewVectorWS, viewVectorWS);
            float  invDist = rsqrt(max(dist2, 1e-8));
            vWS      = viewVectorWS * invDist;
            distFade = (_FadeRange > 0.0)
                     ? saturate(1.0 - (dist2 * invDist - _FadeStart) / _FadeRange)
                     : 1.0;
        #endif

            EnvAcc acc = (EnvAcc)0;
            float3 nW  = normalize(normalWS);

        #if defined(ENV_TRIPLANAR)
            // =================================================================
            //  TRIPLANAR
            // =================================================================
            float3 nP  = normalize(nPIn);
            float3 sgn = step(0.0, nP) * 2.0 - 1.0;          // +1 / -1 (nunca 0)

            // Derivadas de la posicion: fuera de cualquier branch
            float3 dpx = ddx(posP);
            float3 dpy = ddy(posP);

            // Vista en el espacio de proyeccion
            float3 vP = float3(0, 0, 1);
          #ifdef ENV_PARALLAX
            #if defined(ENV_PSPACE_OBJSCALED)
            vP = normalize(TransformWorldToObjectDir(vWS, false) * EnvObjectScale());
            #elif defined(ENV_PSPACE_OBJECT)
            vP = normalize(TransformWorldToObjectDir(vWS, false));
            #else
            vP = vWS;
            #endif
          #endif

            float3 w = EnvTriWeights(nP);

            EnvAxis axX = EnvMakeAxis(0, posP, dpx, dpy, sgn, vP, R);
            EnvAxis axY = EnvMakeAxis(1, posP, dpx, dpy, sgn, vP, R);
            EnvAxis axZ = EnvMakeAxis(2, posP, dpx, dpy, sgn, vP, R);

          #ifdef USE_TRI_HEIGHTBLEND
            // Mezcla guiada por height: el relieve "gana" en las transiciones.
            float3 hgt = float3(0, 0, 0);
            [branch] if (w.x > 0.0) hgt.x = EnvSampleHeightGrad(axX.uv, axX.gx, axX.gy);
            [branch] if (w.y > 0.0) hgt.y = EnvSampleHeightGrad(axY.uv, axY.gx, axY.gy);
            [branch] if (w.z > 0.0) hgt.z = EnvSampleHeightGrad(axZ.uv, axZ.gx, axZ.gy);

            float3 t  = w + hgt * _TriHeightStrength;
            float  m  = max(max(t.x, t.y), t.z);
            float3 hb = max(t - (m - _TriHeightDepth), 0.0) * step(1e-5, w);
            w = EnvNormalizeCull(hb);
          #endif

            // Acumulacion: solo se evaluan los ejes con peso
            [branch] if (w.x > 0.0) EnvAccumAxis(acc, 0, axX, w.x, R, distFade, nP);
            [branch] if (w.y > 0.0) EnvAccumAxis(acc, 1, axY, w.y, R, distFade, nP);
            [branch] if (w.z > 0.0) EnvAccumAxis(acc, 2, axZ, w.z, R, distFade, nP);

          #ifdef USE_NORMAL_MAP
            // Normal: de espacio de proyeccion a mundo
            float3 nBlend = normalize(acc.n);
            #if defined(ENV_PSPACE_OBJSCALED)
            nW = normalize(TransformObjectToWorldNormal(nBlend * EnvObjectScale()));
            #elif defined(ENV_PSPACE_OBJECT)
            nW = normalize(TransformObjectToWorldNormal(nBlend));
            #else
            nW = nBlend;
            #endif
          #endif

        #else
            // =================================================================
            //  UV (Mesh o planar en mundo): UNA sola proyeccion, 1 muestra por textura
            // =================================================================
          #if defined(USE_NORMAL_MAP) || defined(ENV_PARALLAX)
            float3 N = nW;
            float3 T, B;
            EnvGetTangentFrame(N, tangentWS, T, B);
          #endif

            EnvAxis ax;
            ax.uv    = EnvTransformUV(EnvGetBaseUV(uv, posP), R);
            ax.gx    = ddx(ax.uv);       // gradientes de la UV original (fuera de branches)
            ax.gy    = ddy(ax.uv);
            ax.flipU = 1.0;
          #ifdef ENV_PARALLAX
            ax.vTS   = float3(dot(vWS, T), dot(vWS, B), dot(vWS, N));
          #else
            ax.vTS   = float3(0, 0, 1);
          #endif

            EnvAxisOut r = EnvEvalAxis(ax, R, distFade);
            acc.alb  = r.albedo;
            acc.arm  = r.arm;
            acc.emis = r.emis;

          #ifdef USE_NORMAL_MAP
            nW = normalize(T * r.nTS.x + B * r.nTS.y + N * r.nTS.z);
          #endif
        #endif

            // ---- Albedo
            o.albedo = acc.alb.rgb * _BaseColor.rgb;

            // ---- ARM
        #ifdef USE_ARM_MAP
            float rg = acc.arm.g;
          #ifdef USE_ROUGH_INVERT
            rg = 1.0 - rg;                      // solo si la textura trae GLOSS
          #endif
            o.ao        = 1.0 + (acc.arm.r - 1.0) * _AOStrength;
            o.roughness = lerp(_RoughMin, _RoughMax, rg);
            o.metallic  = lerp(_MetalMin, _MetalMax, acc.arm.b);
        #else
            o.ao        = 1.0;
            o.roughness = _RoughConst;
            o.metallic  = _MetalConst;
        #endif

            // ---- Normal
            o.normalWS = nW;

            // ---- Emisivo
        #ifdef USE_EMISSIVE
          #if defined(EMISSION_ALBEDO_A)
            o.emissive = _EmissionColor.rgb * acc.alb.a;       // mascara en alpha del Albedo
          #elif defined(EMISSION_ARM_A)
            o.emissive = _EmissionColor.rgb * acc.arm.a;       // mascara en alpha de la ARM
          #else
            o.emissive = acc.emis * _EmissionColor.rgb;
          #endif
        #else
            o.emissive = 0.0;
        #endif

            return o;
        }
        ENDHLSL

        // ---------------------------------------------------------------- Forward
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

            #pragma shader_feature_local _PROJ_MESH _PROJ_WORLDXZ _PROJ_WORLDXY _PROJ_WORLDZY _PROJ_TRIPLANAR
            #pragma shader_feature_local _TRISPACE_WORLD _TRISPACE_OBJECT _TRISPACE_OBJECTSCALED
            #pragma shader_feature_local _TRINORMALBLEND_WHITEOUT _TRINORMALBLEND_RNM _TRINORMALBLEND_UDN
            #pragma shader_feature_local _TRI_TWO_AXES
            #pragma shader_feature_local _TRI_HEIGHTBLEND
            #pragma shader_feature_local _DISP_OFF _DISP_OFFSET _DISP_POM _DISP_VERTEX
            #pragma shader_feature_local _VORONOI
            #pragma shader_feature_local _NORMALMAP
            #pragma shader_feature_local _NORMAL_FLIPY
            #pragma shader_feature_local _ARMMAP
            #pragma shader_feature_local _ROUGH_INVERT
            #pragma shader_feature_local _HEIGHT_IN_ARM_A
            #pragma shader_feature_local _EMISSION
            #pragma shader_feature_local _EMISSIONSRC_TEXTURE _EMISSIONSRC_ALBEDOALPHA _EMISSIONSRC_ARMALPHA

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

            // La tangente solo hace falta en modo Mesh con normal map o parallax
            #if defined(ENV_UV_MESH) && (defined(USE_NORMAL_MAP) || defined(ENV_PARALLAX))
                #define ENV_NEED_TANGENT 1
            #endif

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
                float3 positionWS : TEXCOORD0;
                float3 normalWS   : TEXCOORD1;
            #if defined(ENV_TRIPLANAR) || defined(ENV_UV_WORLD)
                float3 positionP  : TEXCOORD2;       // posicion en espacio de proyeccion
            #endif
            #if defined(ENV_TRIPLANAR) && !defined(ENV_PSPACE_WORLD)
                float3 normalP    : TEXCOORD3;       // normal en espacio de proyeccion
            #endif
                half   fogFactor  : TEXCOORD4;
                half3  vertexLight: TEXCOORD5;
                DECLARE_LIGHTMAP_OR_SH(staticLightmapUV, vertexSH, 6);
                nointerpolation float2 sinCos : TEXCOORD7;
            #if defined(ENV_UV_MESH)
                float2 uv         : TEXCOORD8;
            #endif
            #if defined(ENV_NEED_TANGENT)
                float4 tangentWS  : TEXCOORD9;       // w = signo de bitangente
            #endif
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

                // Proyeccion con la malla ORIGINAL (la textura no "nada" al desplazar)
                float3 posP, nP;
                EnvProjInputs(posOS, input.normalOS, posP, nP);

            #ifdef USE_VERTEX_DISPLACEMENT
                posOS = EnvDisplaceVertex(posOS, input.normalOS, input.uv, posP, nP);
            #endif

                VertexPositionInputs vInput = GetVertexPositionInputs(posOS);
                VertexNormalInputs   nInput = GetVertexNormalInputs(input.normalOS, input.tangentOS);

                output.positionCS = vInput.positionCS;
                output.positionWS = vInput.positionWS;
                output.normalWS   = nInput.normalWS;
            #if defined(ENV_TRIPLANAR) || defined(ENV_UV_WORLD)
                output.positionP  = posP;
            #endif
            #if defined(ENV_TRIPLANAR) && !defined(ENV_PSPACE_WORLD)
                output.normalP    = nP;
            #endif
            #if defined(ENV_UV_MESH)
                output.uv         = input.uv;
            #endif
            #if defined(ENV_NEED_TANGENT)
                output.tangentWS  = float4(nInput.tangentWS, input.tangentOS.w * GetOddNegativeScale());
            #endif
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

                // Entradas de la proyeccion segun el modo
                float2 uv        = float2(0, 0);
                float4 tangentWS = float4(1, 0, 0, 1);
                float3 posP      = input.positionWS;
                float3 nPIn      = input.normalWS;
            #if defined(ENV_UV_MESH)
                uv = input.uv;
            #endif
            #if defined(ENV_NEED_TANGENT)
                tangentWS = input.tangentWS;
            #endif
            #if defined(ENV_TRIPLANAR) || defined(ENV_UV_WORLD)
                posP = input.positionP;
            #endif
            #if defined(ENV_TRIPLANAR) && !defined(ENV_PSPACE_WORLD)
                nPIn = input.normalP;
            #endif

                EnvSurface s = EvaluateEnvMaterial(uv, posP, nPIn, input.normalWS,
                                                   tangentWS, viewVectorWS, input.sinCos);

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

        // ---------------------------------------------------------------- ShadowCaster
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

            #pragma shader_feature_local _PROJ_MESH _PROJ_WORLDXZ _PROJ_WORLDXY _PROJ_WORLDZY _PROJ_TRIPLANAR
            #pragma shader_feature_local _TRISPACE_WORLD _TRISPACE_OBJECT _TRISPACE_OBJECTSCALED
            #pragma shader_feature_local _TRI_TWO_AXES
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
                float3 posP, nP;
                EnvProjInputs(posOS, input.normalOS, posP, nP);
                posOS = EnvDisplaceVertex(posOS, input.normalOS, input.uv, posP, nP);
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

        // ---------------------------------------------------------------- DepthOnly
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

            #pragma shader_feature_local _PROJ_MESH _PROJ_WORLDXZ _PROJ_WORLDXY _PROJ_WORLDZY _PROJ_TRIPLANAR
            #pragma shader_feature_local _TRISPACE_WORLD _TRISPACE_OBJECT _TRISPACE_OBJECTSCALED
            #pragma shader_feature_local _TRI_TWO_AXES
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
                float3 posP, nP;
                EnvProjInputs(posOS, input.normalOS, posP, nP);
                posOS = EnvDisplaceVertex(posOS, input.normalOS, input.uv, posP, nP);
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

        // ---------------------------------------------------------------- DepthNormals (SSAO, etc.)
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

            #pragma shader_feature_local _PROJ_MESH _PROJ_WORLDXZ _PROJ_WORLDXY _PROJ_WORLDZY _PROJ_TRIPLANAR
            #pragma shader_feature_local _TRISPACE_WORLD _TRISPACE_OBJECT _TRISPACE_OBJECTSCALED
            #pragma shader_feature_local _TRI_TWO_AXES
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
                float3 posP, nP;
                EnvProjInputs(posOS, input.normalOS, posP, nP);
                posOS = EnvDisplaceVertex(posOS, input.normalOS, input.uv, posP, nP);
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
