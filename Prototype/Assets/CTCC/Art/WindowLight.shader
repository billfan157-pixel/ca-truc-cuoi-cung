Shader "CTCC/Window Light"
{
    Properties { _Tint("Air scatter", Color) = (0.37,0.52,0.59,0.105) }
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
            CBUFFER_END
            struct Attributes
            {
                float3 positionOS : POSITION;
                float2 uv : TEXCOORD0;
            };
            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                float eyeDepth : TEXCOORD1;
            };
            Varyings Vert(Attributes input)
            {
                Varyings output;
                VertexPositionInputs position = GetVertexPositionInputs(input.positionOS);
                output.positionCS = position.positionCS;
                output.uv = input.uv;
                output.eyeDepth = -position.positionVS.z;
                return output;
            }
            half4 Frag(Varyings input) : SV_Target
            {
                float2 screenUV = GetNormalizedScreenSpaceUV(input.positionCS);
                float solidDepth = LinearEyeDepth(SampleSceneDepth(screenUV), _ZBufferParams);
                half edges = pow(saturate(sin(input.uv.x * PI)), 1.76);
                half decay = (1 - smoothstep(.04,.98,input.uv.y)) * smoothstep(.02,.15,input.uv.y);
                half proximity = smoothstep(.18,.62,input.eyeDepth) * saturate((solidDepth - input.eyeDepth) / .40);
                return half4(_Tint.rgb, _Tint.a * edges * decay * proximity);
            }
            ENDHLSL
        }
    }
}
