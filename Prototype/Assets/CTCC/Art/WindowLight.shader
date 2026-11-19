Shader "CTCC/Window Light"
{
    Properties
    {
        _Tint("Air scatter", Color) = (0.42, 0.58, 0.68, 0.13)
        _MoteStrength("Dust Mote Shimmer", Range(0, 1)) = 0.45
    }
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "RenderType"="Transparent" "Queue"="Transparent+6" }
        Pass
        {
            Cull Off
            ZWrite Off
            Blend SrcAlpha One
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"

            CBUFFER_START(UnityPerMaterial)
                half4 _Tint;
                half _MoteStrength;
            CBUFFER_END

            struct Attributes
            {
                float3 positionOS : POSITION;
                float3 normalOS : NORMAL;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                float eyeDepth : TEXCOORD1;
                float3 posWS : TEXCOORD2;
            };

            Varyings Vert(Attributes input)
            {
                Varyings output;
                VertexPositionInputs position = GetVertexPositionInputs(input.positionOS);
                output.positionCS = position.positionCS;
                output.uv = input.uv;
                output.eyeDepth = -position.positionVS.z;
                output.posWS = position.positionWS;
                return output;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                float2 screenUV = GetNormalizedScreenSpaceUV(input.positionCS);
                float solidDepth = LinearEyeDepth(SampleSceneDepth(screenUV), _ZBufferParams);
                half edges = pow(saturate(sin(input.uv.x * PI)), 1.9);
                half decay = pow(1.0 - smoothstep(0.02, 0.98, input.uv.y), 1.35) * smoothstep(0.01, 0.14, input.uv.y);
                half proximity = smoothstep(0.22, 0.85, input.eyeDepth) * saturate((solidDepth - input.eyeDepth) / 0.48);
                // Subtle animated drifting dust-mist bands inside the volumetric window shaft
                float drift1 = sin(input.posWS.x * 3.8 + input.posWS.y * 2.4 + _Time.y * 0.28) * 0.5 + 0.5;
                float drift2 = sin(input.posWS.z * 4.5 - input.posWS.y * 3.1 - _Time.y * 0.19) * 0.5 + 0.5;
                half mist = lerp(1.0, 0.68 + 0.64 * (drift1 * drift2), _MoteStrength);
                return half4(_Tint.rgb, _Tint.a * edges * decay * proximity * mist);
            }
            ENDHLSL
        }
    }
}
