Shader "Hidden/Accessibility/ColorBlind_URP"
{
    Properties
    {
        _Mode     ("Mode (0=Normal 1=Prot 2=Deut 3=Trit 4=Acro)", Float) = 0
        _Strength ("Strength", Range(0,1)) = 1
        _Correct  ("Correct (0=Simular, 1=Corregir)", Float) = 0
    }
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" }
        Pass
        {
            Name "ColorBlind"
            ZTest Always ZWrite Off Cull Off

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"

            CBUFFER_START(UnityPerMaterial)
                float _Mode;
                float _Strength;
                float _Correct;
            CBUFFER_END

            
            static const float3x3 PROTAN = float3x3(
                 0.152286,  1.052583, -0.204868,
                 0.114503,  0.786281,  0.099216,
                -0.003882, -0.048116,  1.051998);

            static const float3x3 DEUTAN = float3x3(
                 0.367322,  0.860646, -0.227968,
                 0.280085,  0.672501,  0.047413,
                -0.011820,  0.042940,  0.968881);

            static const float3x3 TRITAN = float3x3(
                 1.255528, -0.076749, -0.178779,
                -0.078411,  0.930809,  0.147602,
                 0.004733,  0.691367,  0.303900);

            float3 ApplyFilter(float3 c)
            {
                int m = (int)round(_Mode);
                if (m <= 0) return c;

                float3 sim = c;
                if      (m == 1) sim = mul(PROTAN, c);
                else if (m == 2) sim = mul(DEUTAN, c);
                else if (m == 3) sim = mul(TRITAN, c);
                else if (m == 4) { float l = dot(c, float3(0.2126, 0.7152, 0.0722)); sim = l.xxx; }

                float3 result = sim;

                
                if (_Correct > 0.5 && m >= 1 && m <= 3)
                {
                    float3 err = c - sim;
                    result = c + float3(0.0, 0.7 * err.r + err.g, 0.7 * err.r + err.b);
                }

                return lerp(c, saturate(result), _Strength);
            }

            half4 Frag(Varyings i) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);
                float3 c = SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, i.texcoord).rgb;
                return half4(ApplyFilter(c), 1);
            }
            ENDHLSL
        }
    }
}
