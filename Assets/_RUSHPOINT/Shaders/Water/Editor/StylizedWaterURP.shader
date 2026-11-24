
Shader "Stylized/Water URP"
{
    Properties
    {
        // ---- Color y profundidad ----
        _ShallowColor      ("Shallow Color", Color) = (0.33, 0.80, 0.82, 0.60)
        _DeepColor         ("Deep Color", Color)    = (0.07, 0.52, 0.63, 0.95)
        _DepthDistance     ("Depth Distance", Range(0.05, 20)) = 2.0
        _DepthPower        ("Depth Curve", Range(0.2, 4)) = 1.0
        _FlatDepth         ("Flat Blend", Range(0, 1)) = 0.5
        _EdgeSoftness      ("Edge Softness", Range(0, 2)) = 0.15
        [Toggle] _UseMeshUV("Use Mesh UV", Float) = 0

        // ---- Patrón (caustics / celdas) ----
        _PatternColor      ("Pattern Color", Color) = (0.70, 0.95, 0.97, 1)
        _PatternIntensity  ("Pattern Intensity", Range(0, 1)) = 0.55
        _PatternScale      ("Pattern Scale", Range(0, 1)) = 0.6
        _PatternSpeed      ("Pattern Speed", Range(0, 2)) = 0.25
        _PatternLineWidth  ("Line Width", Range(0.01, 0.5)) = 0.12
        _PatternSoftness   ("Line Softness", Range(0.001, 0.5)) = 0.08
        _PatternDeepFade   ("Fade In Deep Water", Range(0, 1)) = 0.5
        [NoScaleOffset] _PatternTex ("Pattern Texture (R)", 2D) = "white" {}

        // ---- Espuma ----
        _FoamColor         ("Foam Color", Color) = (1, 1, 1, 1)
        _FoamWidth         ("Foam Width", Range(0.01, 3)) = 0.5
        _FoamCutoff        ("Foam Cutoff", Range(0, 1)) = 0.3
        _FoamSoftness      ("Foam Softness", Range(0.001, 0.5)) = 0.06
        _FoamNoise         ("Foam Noise", Range(0, 1.5)) = 0.6
        _FoamSpeed         ("Foam Speed", Range(0, 5)) = 1.5

        // ---- Olas (vertex) ----
        _WaveAmplitude     ("Wave Height", Range(0, 0.5)) = 0.04
        _WaveFrequency     ("Wave Frequency", Range(0.05, 6)) = 1.2
        _WaveSpeed         ("Wave Speed", Range(0, 5)) = 0.8
        _WaveDirection     ("Wave Directions", Vector) = (1, 0.3, -0.5, 1)

        // ---- Normal map ----
        [NoScaleOffset][Normal] _NormalMap ("Normal Map", 2D) = "bump" {}
        _NormalStrength    ("Normal Strength", Range(0, 2)) = 0.5
        _NormalTiling      ("Normal Tiling", Range(0.05, 5)) = 0.35
        _NormalSpeed       ("Normal Speed", Vector) = (0.03, 0.02, -0.02, 0.03)

        // ---- Mapas de superficie (ARM o separados) ----
        [NoScaleOffset] _ARMMap       ("ARM Map (R=AO G=Rough B=Metal)", 2D) = "white" {}
        [NoScaleOffset] _OcclusionMap ("Occlusion", 2D) = "white" {}
        [NoScaleOffset] _RoughnessMap ("Roughness", 2D) = "white" {}
        [NoScaleOffset] _MetallicMap  ("Metallic", 2D)  = "white" {}
        _OcclusionStrength ("Occlusion Strength", Range(0, 1)) = 1
        _Roughness         ("Roughness", Range(0, 1)) = 0.25
        _Metallic          ("Metallic", Range(0, 1)) = 0
        _SurfRemap         ("Roughness / Metallic Remap", Vector) = (0, 1, 0, 1)
        _SurfaceTiling     ("Surface Maps Tiling", Range(0.05, 5)) = 0.35

        // ---- Iluminación toon ----
        _ShadowColor       ("Shade Color", Color) = (0.55, 0.78, 0.88, 1)
        _ShadeThreshold    ("Shade Threshold", Range(0, 1)) = 0.45
        _ShadeSoftness     ("Shade Softness", Range(0.001, 1)) = 0.3
        _LightInfluence    ("Light Color Influence", Range(0, 1)) = 0.5
        _FresnelColor      ("Fresnel Color", Color) = (0.80, 0.96, 1, 1)
        _FresnelPower      ("Fresnel Power", Range(0.5, 10)) = 4
        _FresnelStrength   ("Fresnel Strength", Range(0, 1)) = 0.5

        // ---- Especular toon ----
        _SpecularColor     ("Specular Color", Color) = (1, 1, 1, 0.8)
        _SpecThreshold     ("Specular Threshold", Range(0, 1)) = 0.6
        _SpecSoftness      ("Specular Softness", Range(0.001, 0.5)) = 0.05

        // ---- Reflejos / Refracción ----
        _ReflectionStrength("Reflection Strength", Range(0, 1)) = 0.4
        _ReflectionPower   ("Reflection Fresnel", Range(0.5, 8)) = 3
        _RefractionStrength("Refraction Strength", Range(0, 0.2)) = 0.03

        [Enum(UnityEngine.Rendering.CullMode)] _Cull ("Cull", Float) = 2
    }

    SubShader
    {
        Tags
        {
            "RenderType"     = "Transparent"
            "Queue"          = "Transparent"
            "RenderPipeline" = "UniversalPipeline"
            "IgnoreProjector"= "True"
        }

        Pass
        {
            Name "StylizedWater"
            Tags { "LightMode" = "UniversalForward" }

            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            Cull [_Cull]

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma target 3.0
            #pragma multi_compile_fog

            // Keywords "OFF": estado por defecto = función activa (no requiere sincronizar nada al crear el material)
            #pragma shader_feature_local _WAVES_OFF
            #pragma shader_feature_local _NORMALMAP
            #pragma shader_feature_local_fragment _DEPTH_OFF
            #pragma shader_feature_local_fragment _PATTERN_OFF
            #pragma shader_feature_local_fragment _PATTERN_TEXTURE
            #pragma shader_feature_local_fragment _FOAM_OFF
            #pragma shader_feature_local_fragment _SPECULAR_OFF
            #pragma shader_feature_local_fragment _REFLECTIONS
            #pragma shader_feature_local_fragment _REFRACTION
            #pragma shader_feature_local_fragment _ _SURFMAP_ARM _SURFMAP_SEPARATE

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareOpaqueTexture.hlsl"

            TEXTURE2D(_PatternTex);
            TEXTURE2D(_NormalMap);
            TEXTURE2D(_ARMMap);
            TEXTURE2D(_OcclusionMap);
            TEXTURE2D(_RoughnessMap);
            TEXTURE2D(_MetallicMap);

            CBUFFER_START(UnityPerMaterial)
                half4  _ShallowColor;
                half4  _DeepColor;
                float  _DepthDistance;
                float  _DepthPower;
                float  _FlatDepth;
                float  _EdgeSoftness;
                float  _UseMeshUV;

                half4  _PatternColor;
                float  _PatternIntensity;
                float  _PatternScale;
                float  _PatternSpeed;
                float  _PatternLineWidth;
                float  _PatternSoftness;
                float  _PatternDeepFade;

                half4  _FoamColor;
                float  _FoamWidth;
                float  _FoamCutoff;
                float  _FoamSoftness;
                float  _FoamNoise;
                float  _FoamSpeed;

                float  _WaveAmplitude;
                float  _WaveFrequency;
                float  _WaveSpeed;
                float4 _WaveDirection;

                float  _NormalStrength;
                float  _NormalTiling;
                float4 _NormalSpeed;

                float  _OcclusionStrength;
                float  _Roughness;
                float  _Metallic;
                float4 _SurfRemap;
                float  _SurfaceTiling;

                half4  _ShadowColor;
                float  _ShadeThreshold;
                float  _ShadeSoftness;
                float  _LightInfluence;
                half4  _FresnelColor;
                float  _FresnelPower;
                float  _FresnelStrength;

                half4  _SpecularColor;
                float  _SpecThreshold;
                float  _SpecSoftness;

                float  _ReflectionStrength;
                float  _ReflectionPower;
                float  _RefractionStrength;
            CBUFFER_END

            #if !defined(_PATTERN_OFF) || (!defined(_FOAM_OFF) && !defined(_DEPTH_OFF))
                #define WATER_NEEDS_PATTERN 1
            #endif

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS   : NORMAL;
                float2 uv         : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
                half3  normalWS   : TEXCOORD1;
                float4 uv0        : TEXCOORD2;   // xy = patrón, zw = mapas de superficie
            #if defined(_NORMALMAP)
                float4 uvN        : TEXCOORD3;   // dos capas de normal map animadas
            #endif
                half   fogFactor  : TEXCOORD4;
            };

            // ----------------------------------------------------------------- utilidades
            float EyeDepth(float raw)
            {
                #if UNITY_REVERSED_Z
                    float orthoRaw = 1.0 - raw;
                #else
                    float orthoRaw = raw;
                #endif
                float ortho = _ProjectionParams.y + (_ProjectionParams.z - _ProjectionParams.y) * orthoRaw;
                float persp = LinearEyeDepth(raw, _ZBufferParams);
                return lerp(persp, ortho, unity_OrthoParams.w);
            }

            // Escalón suave estilo toon (sin comportamiento indefinido cuando soft -> 0)
            half Toon(float x, float thr, float soft)
            {
                float t = saturate((x - thr) / max(soft, 0.0001) * 0.5 + 0.5);
                return (half)(t * t * (3.0 - 2.0 * t));
            }

            float2 Hash22(float2 p)
            {
                float3 p3 = frac(float3(p.xyx) * float3(0.1031, 0.1030, 0.0973));
                p3 += dot(p3, p3.yzx + 33.33);
                return frac((p3.xx + p3.yz) * p3.zy);
            }

            // Voronoi animado. Sin sin()/cos(): onda triangular suavizada (mucho más barato en móvil).
            // f1 = distancia al punto más cercano, edge = F2 - F1 (bordes de celda)
            void Voronoi(float2 uv, float t, out float f1, out float edge)
            {
                float2 g = floor(uv);
                float2 f = frac(uv);
                float d1 = 8.0, d2 = 8.0;
                [unroll] for (int y = -1; y <= 1; y++)
                {
                    [unroll] for (int x = -1; x <= 1; x++)
                    {
                        float2 o = float2(x, y);
                        float2 h = Hash22(g + o);
                        float2 s = abs(frac(h + t) * 2.0 - 1.0);
                        s = s * s * (3.0 - 2.0 * s);
                        float2 p = o + s - f;
                        float d = dot(p, p);
                        if (d < d1) { d2 = d1; d1 = d; }
                        else if (d < d2) { d2 = d; }
                    }
                }
                f1 = sqrt(d1);
                edge = sqrt(d2) - f1;
            }

            // ----------------------------------------------------------------- vertex
            Varyings Vert(Attributes IN)
            {
                Varyings OUT;

                float3 pws = TransformObjectToWorld(IN.positionOS.xyz);
                half3 nWS  = TransformObjectToWorldNormal(IN.normalOS);

                // UVs en mundo (XZ): la escala del patrón es independiente del tamaño de la malla
                float2 baseUV = lerp(pws.xz, IN.uv, _UseMeshUV);

                #if !defined(_WAVES_OFF)
                    float t  = _Time.y * _WaveSpeed;
                    float p1 = dot(pws.xz, _WaveDirection.xy) * _WaveFrequency + t;
                    float p2 = dot(pws.xz, _WaveDirection.zw) * (_WaveFrequency * 1.7) + t * 1.3;
                    pws.y += (sin(p1) + 0.5 * sin(p2)) * _WaveAmplitude;
                    // Normal analítica (asume plano de agua horizontal)
                    float2 slope = _WaveAmplitude * _WaveFrequency *
                                   (cos(p1) * _WaveDirection.xy + 0.85 * cos(p2) * _WaveDirection.zw);
                    nWS = (half3)normalize(float3(-slope.x, 1.0, -slope.y));
                #endif

                OUT.positionWS = pws;
                OUT.normalWS   = nWS;
                OUT.positionCS = TransformWorldToHClip(pws);
                OUT.uv0        = float4(baseUV * _PatternScale, baseUV * _SurfaceTiling);

                #if defined(_NORMALMAP)
                    float tt = _Time.y;
                    OUT.uvN = float4(baseUV * _NormalTiling + tt * _NormalSpeed.xy,
                                     baseUV * (_NormalTiling * 0.73) + tt * _NormalSpeed.zw + 0.37);
                #endif

                OUT.fogFactor = ComputeFogFactor(OUT.positionCS.z);
                return OUT;
            }

            // ----------------------------------------------------------------- fragment
            half4 Frag(Varyings IN) : SV_Target
            {
                float3 posWS = IN.positionWS;
                half3  V     = (half3)GetWorldSpaceNormalizeViewDir(posWS);
                half3  n     = normalize(IN.normalWS);

                // ---------- Normal map (2 capas, sin tangentes: plano horizontal) ----------
                #if defined(_NORMALMAP)
                    half2 nA   = UnpackNormal(SAMPLE_TEXTURE2D(_NormalMap, sampler_LinearRepeat, IN.uvN.xy)).xy;
                    half2 nB   = UnpackNormal(SAMPLE_TEXTURE2D(_NormalMap, sampler_LinearRepeat, IN.uvN.zw)).xy;
                    half2 nOff = (nA + nB) * (0.5h * (half)_NormalStrength);
                    n = normalize(half3(n.x + nOff.x, n.y, n.z + nOff.y));
                #endif

                // ---------- Mapas de superficie: ARM empaquetado o texturas separadas ----------
                half ao    = 1.0h;
                half rough = (half)_Roughness;
                half metal = (half)_Metallic;
                #if defined(_SURFMAP_ARM)
                    half3 arm = SAMPLE_TEXTURE2D(_ARMMap, sampler_LinearRepeat, IN.uv0.zw).rgb;   // R=AO G=Roughness B=Metallic
                    ao    = lerp(1.0h, arm.r, (half)_OcclusionStrength);
                    rough = lerp((half)_SurfRemap.x, (half)_SurfRemap.y, arm.g);
                    metal = lerp((half)_SurfRemap.z, (half)_SurfRemap.w, arm.b);
                #elif defined(_SURFMAP_SEPARATE)
                    half aoS = SAMPLE_TEXTURE2D(_OcclusionMap, sampler_LinearRepeat, IN.uv0.zw).r;
                    half roS = SAMPLE_TEXTURE2D(_RoughnessMap, sampler_LinearRepeat, IN.uv0.zw).r;
                    half meS = SAMPLE_TEXTURE2D(_MetallicMap,  sampler_LinearRepeat, IN.uv0.zw).r;
                    ao    = lerp(1.0h, aoS, (half)_OcclusionStrength);
                    rough = lerp((half)_SurfRemap.x, (half)_SurfRemap.y, roS);
                    metal = lerp((half)_SurfRemap.z, (half)_SurfRemap.w, meS);
                #endif

                // ---------- Profundidad ----------
                float2 suv     = GetNormalizedScreenSpaceUV(IN.positionCS);
                float  fragEye = EyeDepth(IN.positionCS.z);
                float  d       = 1000.0;
                half   depthT  = (half)_FlatDepth;
                #if !defined(_DEPTH_OFF)
                    float sceneEye = EyeDepth(SampleSceneDepth(suv));
                    d      = max(sceneEye - fragEye, 0.0);
                    depthT = (half)pow(saturate(d / max(_DepthDistance, 0.0001)), _DepthPower);
                #endif

                half3 col   = lerp(_ShallowColor.rgb, _DeepColor.rgb, depthT);
                half  alpha = lerp(_ShallowColor.a,   _DeepColor.a,   depthT);

                // ---------- Patrón (celdas / caustics) ----------
                half patNoise = 0.5h;
                half patLine  = 0.0h;
                #if defined(WATER_NEEDS_PATTERN)
                    float pt = _Time.y * _PatternSpeed;
                    #if defined(_PATTERN_TEXTURE)
                        half pa = SAMPLE_TEXTURE2D(_PatternTex, sampler_LinearRepeat, IN.uv0.xy + float2(pt, pt * 0.7)).r;
                        half pb = SAMPLE_TEXTURE2D(_PatternTex, sampler_LinearRepeat, IN.uv0.xy * 0.83 - float2(pt * 0.8, -pt * 0.5)).r;
                        patNoise = pa;
                        patLine  = Toon(min(pa, pb), 1.0 - _PatternLineWidth * 2.0, _PatternSoftness);
                    #else
                        float vf1, vedge;
                        Voronoi(IN.uv0.xy, pt, vf1, vedge);
                        patNoise = (half)vf1;
                        patLine  = 1.0h - (half)smoothstep(_PatternLineWidth, _PatternLineWidth + max(_PatternSoftness, 0.0001), vedge);
                    #endif
                #endif

                #if !defined(_PATTERN_OFF)
                    half pat = patLine * (half)_PatternIntensity * (1.0h - depthT * (half)_PatternDeepFade);
                    col = lerp(col, _PatternColor.rgb, saturate(pat) * _PatternColor.a);
                #endif

                col *= ao;

                // ---------- Iluminación toon (solo luz principal) ----------
                Light L = GetMainLight();
                half ndl  = dot(n, L.direction) * 0.5h + 0.5h;
                half band = Toon(ndl, _ShadeThreshold, _ShadeSoftness);
                half3 lightTint = lerp(half3(1, 1, 1), L.color, (half)_LightInfluence);
                col *= lerp(_ShadowColor.rgb, half3(1, 1, 1), band) * lightTint;

                // ---------- Refracción (opcional, cuesta 2 samples extra) ----------
                #if defined(_REFRACTION)
                    float2 ruv = suv + n.xz * _RefractionStrength;
                    #if !defined(_DEPTH_OFF)
                        // Evita "arrastrar" objetos que están delante del agua
                        ruv = (EyeDepth(SampleSceneDepth(ruv)) < fragEye) ? suv : ruv;
                    #endif
                    half3 sceneCol = SampleSceneColor(ruv);
                    col   = lerp(sceneCol, col, alpha);
                    alpha = 1.0h;
                #endif

                // ---------- Fresnel ----------
                half nv   = saturate(dot(n, V));
                half rim  = pow(1.0h - nv, (half)_FresnelPower) * (half)_FresnelStrength;
                col = lerp(col, _FresnelColor.rgb, saturate(rim));

                // ---------- Reflejos del entorno (opcional) ----------
                #if defined(_REFLECTIONS)
                    half3 rv   = reflect(-V, n);
                    half4 enc  = SAMPLE_TEXTURECUBE_LOD(unity_SpecCube0, samplerunity_SpecCube0, rv, PerceptualRoughnessToMipmapLevel(rough));
                    half3 env  = DecodeHDREnvironment(enc, unity_SpecCube0_HDR);
                    env *= lerp(half3(1, 1, 1), col, metal);   // metallic tiñe el reflejo con el color del agua
                    half reflAmt = saturate(pow(1.0h - nv, (half)_ReflectionPower)) * (half)_ReflectionStrength;
                    col = lerp(col, env, reflAmt);
                #endif

                // ---------- Especular toon ----------
                #if !defined(_SPECULAR_OFF)
                    half3 H  = normalize(L.direction + V);
                    half  nh = saturate(dot(n, H));
                    float specExp = lerp(2048.0, 16.0, (float)rough);
                    half  spec = Toon(pow((float)nh, specExp), _SpecThreshold, _SpecSoftness);
                    col += _SpecularColor.rgb * (spec * _SpecularColor.a) * L.color;
                #endif

                // ---------- Borde suave contra la orilla ----------
                #if !defined(_DEPTH_OFF)
                    alpha *= (half)saturate(d / max(_EdgeSoftness, 0.0001));
                #endif

                // ---------- Espuma de orilla (usa el ruido del patrón: gratis) ----------
                #if !defined(_FOAM_OFF) && !defined(_DEPTH_OFF)
                    float wob   = 0.75 + 0.25 * sin(_Time.y * _FoamSpeed + patNoise * 6.2831);
                    half  fmask = 1.0h - (half)saturate(d / max(_FoamWidth * wob, 0.0001));
                    half  foam  = Toon(fmask - patNoise * _FoamNoise, _FoamCutoff, _FoamSoftness) * _FoamColor.a;
                    col   = lerp(col, _FoamColor.rgb, foam);
                    alpha = max(alpha, foam);
                #endif

                col = MixFog(col, IN.fogFactor);
                return half4(col, saturate(alpha));
            }
            ENDHLSL
        }
    }

    CustomEditor "StylizedWaterGUI"
    FallBack Off
}
