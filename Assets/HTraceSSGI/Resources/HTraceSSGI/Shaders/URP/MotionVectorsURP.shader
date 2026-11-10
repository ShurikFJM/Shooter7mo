Shader "Hidden/HTraceSSGI/MotionVectorsURP"
{
    SubShader
    {
        Pass
        {
            Name "Camera Motion Vectors"
            
            Cull Off 
            ZWrite Off 

            HLSLPROGRAM

            #pragma vertex VertexStage
            #pragma fragment FragmentStage

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/UnityInput.hlsl"
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/FoveatedRendering.hlsl"
            #include "../../Headers/HMain.hlsl"

            H_TEXTURE(_ObjectMotionVectors);
            H_TEXTURE(_ObjectMotionVectorsDepth);

            float _BiasOffset;
            
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

            struct FragOutput
            {
                float2 MotionVectors : SV_Target0;
                float Mask : SV_Target1;
            };

            Varyings VertexStage(Attributes Input)
            {
                Varyings Output;
                UNITY_SETUP_INSTANCE_ID(Input);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);
                
                Output.PositionCS = GetFullScreenTriangleVertexPosition(Input.VertexID);
                Output.TexCoord = GetFullScreenTriangleTexCoord(Input.VertexID);

                return Output;
            }

            FragOutput FragmentStage(Varyings Input)
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(Input);
                FragOutput Output = (FragOutput)0;
                
                float2 ObjectMotionVectorsColor = H_LOAD(_ObjectMotionVectors, Input.PositionCS.xy).xy;
                float ObjectMotionVectorsDepth = H_LOAD(_ObjectMotionVectorsDepth, Input.PositionCS.xy).x;
                float CameraDepth = LoadSceneDepth(Input.PositionCS.xy);

                #if !UNITY_REVERSED_Z
                    CameraDepth = lerp(UNITY_NEAR_CLIP_VALUE, 1, LoadSceneDepth(Input.PositionCS.xy).x);
                #endif
                
                if (ObjectMotionVectorsDepth >= CameraDepth + _BiasOffset)
                {
                    Output.MotionVectors = ObjectMotionVectorsColor;
                    Output.Mask = 1;
                    return Output;
                }

                // Reconstruct world position
                float3 PositionWS = ComputeWorldSpacePosition(Input.PositionCS.xy * _ScreenSize.zw, CameraDepth, UNITY_MATRIX_I_VP);

                // Multiply with current and previous non-jittered view projection
                float4 PositionCS = mul(H_MATRIX_VP, float4(PositionWS.xyz, 1.0));
                float4 PreviousPositionCS = mul(H_MATRIX_PREV_VP, float4(PositionWS.xyz, 1.0));

                // Non-uniform raster needs to keep the posNDC values in float to avoid additional conversions
                // since uv remap functions use floats
                float2 PositionNDC = PositionCS.xy * rcp(PositionCS.w);
                float2 PreviousPositionNDC = PreviousPositionCS.xy * rcp(PreviousPositionCS.w);
                
                // Calculate forward velocity
                float2 Velocity = (PositionNDC - PreviousPositionNDC);

                // TODO: test that velocity.y is correct
                #if UNITY_UV_STARTS_AT_TOP
                    Velocity.y = -Velocity.y;
                #endif

                // Convert velocity from NDC space (-1..1) to screen UV 0..1 space
                // Note: It doesn't mean we don't have negative values, we store negative or positive offset in the UV space.
                // Note: ((posNDC * 0.5 + 0.5) - (prevPosNDC * 0.5 + 0.5)) = (velocity * 0.5)
                Velocity.xy *= 0.5;
             
                Output.MotionVectors = Velocity;
                Output.Mask = 0;
                
                return Output;
            }

            ENDHLSL
        }


        Pass
        {
            Name "Object Motion Vectors"
            
            Tags { "LightMode" = "MotionVectors" }

            HLSLPROGRAM

            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing
            
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/UnityInput.hlsl"
            #include "../../Headers/HMain.hlsl"
            
            #ifndef HAVE_VFX_MODIFICATION
            #pragma multi_compile _ DOTS_INSTANCING_ON
            #if UNITY_PLATFORM_ANDROID || UNITY_PLATFORM_WEBGL || UNITY_PLATFORM_UWP
            #pragma target 3.5 DOTS_INSTANCING_ON
            #else
            #pragma target 4.5 DOTS_INSTANCING_ON
            #endif
            #endif 
            
            struct Attributes
            {
                float4 Position : POSITION;
                float3 PositionOld : TEXCOORD4;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 PositionCS : SV_POSITION;
                float4 PositionCSNoJitter : TEXCOORD0;
                float4 PreviousPositionCSNoJitter : TEXCOORD1;
                float  MotionMask : TEXCOORD2;
                UNITY_VERTEX_INPUT_INSTANCE_ID
                UNITY_VERTEX_OUTPUT_STEREO
            };
   
            Varyings vert(Attributes Input)
            {
                UNITY_SETUP_INSTANCE_ID(Input);
                Varyings Output = (Varyings)0;
                UNITY_TRANSFER_INSTANCE_ID(Input, Output);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(Output);

                VertexPositionInputs VertexInput = GetVertexPositionInputs(Input.Position.xyz);

                // Jittered. Match the frame.
                Output.PositionCS = VertexInput.positionCS;

                // This is required to avoid artifacts ("gaps" in the _MotionVectorTexture) on some platforms
                #if defined(UNITY_REVERSED_Z)
                    Output.PositionCS.z -= unity_MotionVectorsParams.z * Output.PositionCS.w;
                #else
                    Output.PositionCS.z += unity_MotionVectorsParams.z * Output.PositionCS.w;
                #endif
                
                const float4 PreviousPosition = (unity_MotionVectorsParams.x == 1) ? float4(Input.PositionOld, 1) : Input.Position;
                const float4 PositionWS = mul(UNITY_MATRIX_M, Input.Position);
                const float4 PreviousPositionWS = mul(UNITY_PREV_MATRIX_M, PreviousPosition);

                Output.PositionCSNoJitter = mul(H_MATRIX_VP, PositionWS);
                Output.PreviousPositionCSNoJitter = mul(H_MATRIX_PREV_VP, PreviousPositionWS);
                Output.MotionMask = length(PositionWS - PreviousPositionWS) > 0.0001 ? 1 : 0;
                    
                return Output;
            }

            struct FragOutput
            {
                float2 MotionVectors : SV_Target0;
                float Mask : SV_Target1;
            };
            
            FragOutput frag(Varyings Input)
            {
                UNITY_SETUP_INSTANCE_ID(Input);
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(Input);
                FragOutput Output = (FragOutput)0;
                
                // Note: unity_MotionVectorsParams.y is 0 is forceNoMotion is enabled
                bool ForceNoMotion = unity_MotionVectorsParams.y == 0.0;
                if (ForceNoMotion)
                {
                    Output.MotionVectors = 0;
                    Output.Mask = 0;
                    return Output;
                }

                // Calculate positions
                float4 PositionCS = Input.PositionCSNoJitter;
                float4 PreviousPositionCS = Input.PreviousPositionCSNoJitter;
                
                float2 PositionNDC = PositionCS.xy * rcp(PositionCS.w);
                float2 PreviousPositionNDC = PreviousPositionCS.xy * rcp(PreviousPositionCS.w);
                
                float2 Velocity = (PositionNDC.xy - PreviousPositionNDC.xy);
                #if UNITY_UV_STARTS_AT_TOP
                    Velocity.y = -Velocity.y;
                #endif

                // Convert velocity from NDC space (-1..1) to UV 0..1 space
                // Note: It doesn't mean we don't have negative values, we store negative or positive offset in UV space.
                // Note: ((posNDC * 0.5 + 0.5) - (prevPosNDC * 0.5 + 0.5)) = (velocity * 0.5)
                Velocity.xy *= 0.5;
                
                Output.MotionVectors = Velocity;
                Output.Mask = Input.MotionMask;
                return Output;
            }
            ENDHLSL
        }
    }
}
