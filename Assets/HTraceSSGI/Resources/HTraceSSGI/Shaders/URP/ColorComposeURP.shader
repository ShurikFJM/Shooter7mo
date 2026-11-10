Shader "Hidden/HTraceSSGI/ColorComposeURP"
{
    SubShader
    {
        HLSLINCLUDE
        
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
        #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/FoveatedRendering.hlsl"
        
        struct Attributes
        {
            uint VertexID : SV_VertexID;
            UNITY_VERTEX_INPUT_INSTANCE_ID
        };

        struct Varyings
        {
            float4 PositionCS : SV_POSITION;
            float2 TexCoord   : TEXCOORD0;
            UNITY_VERTEX_OUTPUT_STEREO
        };
        
        Varyings SharedVertexStage(Attributes input)
        {
            Varyings output;
            UNITY_SETUP_INSTANCE_ID(input);
            UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);
            
            output.PositionCS = GetFullScreenTriangleVertexPosition(input.VertexID);
            output.TexCoord = GetFullScreenTriangleTexCoord(input.VertexID);

            return output;
        }
        
        ENDHLSL
        
        Pass
        {
            Name "Copy Color Buffer"
                
            Cull Off 
            ZWrite Off
            ColorMask RGB

            HLSLPROGRAM

            #pragma vertex SharedVertexStage
            #pragma fragment FragmentStage

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/UnityInput.hlsl"
            #include "../../Headers/HMain.hlsl"

            float4 FragmentStage(Varyings input) : SV_Target
            {
                return float4(HBUFFER_COLOR(input.TexCoord * _ScreenSize.xy).xyz, 1);
            }

            ENDHLSL
        }

        Pass
        {
            Name "Override Indirect Lighting"
                
            Cull Off 
            ZWrite Off
            ColorMask RGB
            
            // Doesn't seem to work for the R16G16A16B16 Color Buffer
            // Blend One One
            // BlendOp RevSub
            
            HLSLPROGRAM

            #pragma vertex SharedVertexStage
            #pragma fragment FragmentStage

            #pragma multi_compile _ PROBE_VOLUMES_L1 PROBE_VOLUMES_L2
            #pragma multi_compile _ _GBUFFER_NORMALS_OCT

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            #include "../../Headers/HMain.hlsl"
            #include "../../Headers/HSpaceTransforms.hlsl"
            #include "../../Includes/HFallbackSSGI.hlsl"
            
            H_TEXTURE(_ColorCopy);
            
            #define kMaterialFlagSpecularSetup 8
            uint UnpackMaterialFlags(float packedMaterialFlags)
            { return uint((packedMaterialFlags * 255.0h) + 0.5h); }
            
            float4 FragmentStage(Varyings input) : SV_Target
            {
                uint2 pixCoord = input.TexCoord * _ScreenSize.xy;
                float3 ColorCopy = H_LOAD(_ColorCopy, pixCoord).xyz;
                
                float DepthCenter = HBUFFER_DEPTH(pixCoord);
                float3 NormalCenterWS = HBUFFER_NORMAL_WS(pixCoord);
                float3 PositionCenterWS = H_COMPUTE_POSITION_WS(input.TexCoord, DepthCenter, H_MATRIX_I_VP);
                
                float4 GBuffer0 = H_LOAD(g_HTraceGBuffer0, pixCoord);
                float4 Gbuffer1 = H_LOAD(g_HTraceGBuffer1, pixCoord);
                
                float3 IndirectLighting = 0;
                float Metallic = Gbuffer1.r;
                
                // This seems to make everything worse.
                // if ((UnpackMaterialFlags(GBuffer0.a) & kMaterialFlagSpecularSetup) != 0)
                //   Metallic = (ReflectivitySpecular(Gbuffer1.rgb));

#if UNITY_VERSION >= 600000
                if (_EnableProbeVolumes)
                {
                    if (PROBE_VOLUMES_L1 || PROBE_VOLUMES_L2)
                    { IndirectLighting = EvaluateFallbackAPV(float4(0,0,0,0), PositionCenterWS, NormalCenterWS, H_GET_VIEW_DIRECTION_WS(PositionCenterWS), pixCoord, HBUFFER_RENDER_LAYER_MASK(pixCoord)); }
                }
                else
#endif
                { IndirectLighting = EvaluateFallbackSky(NormalCenterWS); }

                // This works reliably only with Render Graph, so we'll use Unity's _ScreenSpaceOcclusionTexture texture directly here
                // float SSAO = H_SAMPLE(g_HTraceSSAO, H_SAMPLER_POINT_CLAMP, input.TexCoord);
                float SSAO = _AmbientOcclusionParam.x == 0 ? 1 : H_SAMPLE(_ScreenSpaceOcclusionTexture, H_SAMPLER_POINT_CLAMP, input.TexCoord).x;
                float AmbientOcclusion = min(SSAO, Gbuffer1.a);
                
                IndirectLighting = IndirectLighting * GBuffer0.rgb * (1.0 - Metallic) * AmbientOcclusion;
                
                return float4(max(ColorCopy - IndirectLighting, 0), 1);  
            }

            ENDHLSL
        }

        Pass
        {
            Name "Final Output"
                
            Cull Off 
            ZWrite Off
            ColorMask RGB
            Blend One One

            HLSLPROGRAM

            #pragma vertex SharedVertexStage
            #pragma fragment FragmentStage

            #pragma multi_compile _ PROBE_VOLUMES_L1 PROBE_VOLUMES_L2
            #pragma multi_compile _ _GBUFFER_NORMALS_OCT
            #pragma multi_compile _ USE_RECEIVE_LAYER_MASK

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            
            #include "../../Headers/HMain.hlsl"
            #include "../../Headers/HSpaceTransforms.hlsl"
            #include "../../Includes/HFallbackSSGI.hlsl"

            H_TEXTURE(_HTraceBufferGI);
            
            uint _MetallicIndirectFallback;
            uint _ExcludeReceivingLayerMaskSSGI;
            float _IndirectLightingIntensity;
            float4 _APVParams;
            
            float4 FragmentStage(Varyings input) : SV_Target
            {
                uint2 pixCoord = input.TexCoord * _ScreenSize.xy;
                
                if (HBUFFER_DEPTH(pixCoord) <= 1e-7) return 0;
                
                float3 IndirectLighting = H_LOAD(_HTraceBufferGI, pixCoord).xyz;

#if UNITY_VERSION >= 600000
                // Restore indirect lighting on masked out objects
                if (USE_RECEIVE_LAYER_MASK)
                {
                    uint RenderingLayer = HBUFFER_RENDER_LAYER_MASK(pixCoord);
                    if (RenderingLayer & _ExcludeReceivingLayerMaskSSGI)
                    {
                        float3 NormalCenterWS = HBUFFER_NORMAL_WS(pixCoord);
                        if (_EnableProbeVolumes)
                        {
                            float DepthCenter = HBUFFER_DEPTH(pixCoord);
                            float3 PositionCenterWS = H_COMPUTE_POSITION_WS(input.TexCoord, DepthCenter, H_MATRIX_I_VP);
                            
                            if (PROBE_VOLUMES_L1 || PROBE_VOLUMES_L2)
                            { IndirectLighting = EvaluateFallbackAPV(_APVParams, PositionCenterWS, NormalCenterWS, H_GET_VIEW_DIRECTION_WS(PositionCenterWS), pixCoord, RenderingLayer); }
                        }
                        else
                        IndirectLighting = EvaluateFallbackSky(NormalCenterWS);
                    }
                }
#endif
                float4 GBuffer0 =  H_LOAD(g_HTraceGBuffer0, pixCoord);
                float4 Gbuffer1 = H_LOAD(g_HTraceGBuffer1, pixCoord);

                // This works reliably only with Render Graph, so we'll use Unity's _ScreenSpaceOcclusionTexture texture directly here
                // float SSAO = H_SAMPLE(g_HTraceSSAO, H_SAMPLER_POINT_CLAMP, input.TexCoord);
                float SSAO = _AmbientOcclusionParam.x == 0 ? 1 : H_SAMPLE(_ScreenSpaceOcclusionTexture, H_SAMPLER_POINT_CLAMP, input.TexCoord).x;
                float AmbientOcclusion = min(SSAO, Gbuffer1.a);
                
                float Metallic = _MetallicIndirectFallback ? 0 : MetallicFromReflectivity(ReflectivitySpecular(Gbuffer1.rgb));
                
                float3 FinalIndirectLighting = IndirectLighting * _IndirectLightingIntensity * GBuffer0.rgb * (1 - Metallic) * AmbientOcclusion;
                
                return float4(FinalIndirectLighting, 1);
            }

            ENDHLSL
        }
    }
}
