Shader "CTCC/Window Light"
{
    Properties
    {
        _Tint("Air scatter", Color) = (0.42, 0.58, 0.68, 0.13)
        _MoteStrength("Dust Mote Shimmer", Range(0, 1)) = 0.45
        [Toggle(_CTCC_ROUND_BEAM)] _RoundBeam("Rounded Cone", Float) = 0
        [HideInInspector] _ConeTop("Cone Top (World)", Vector) = (0,0,0,0)
        [HideInInspector] _ConeBottom("Cone Bottom (World)", Vector) = (0,0,0,0)
        [HideInInspector] _Cull("Cull Mode", Float) = 0
    }
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "RenderType"="Transparent" "Queue"="Transparent+6" }
        Pass
        {
            Cull [_Cull]
            ZWrite Off
            Blend SrcAlpha One
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma shader_feature_local_fragment _CTCC_ROUND_BEAM
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"

            CBUFFER_START(UnityPerMaterial)
                half4 _Tint;
                half _MoteStrength;
                float4 _ConeTop;
                float4 _ConeBottom;
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
                float3 normalWS : TEXCOORD3;
            };

            Varyings Vert(Attributes input)
            {
                Varyings output;
                VertexPositionInputs position = GetVertexPositionInputs(input.positionOS);
                output.positionCS = position.positionCS;
                output.uv = input.uv;
                output.eyeDepth = -position.positionVS.z;
                output.posWS = position.positionWS;
                output.normalWS = TransformObjectToWorldNormal(input.normalOS);
                return output;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                float2 screenUV = GetNormalizedScreenSpaceUV(input.positionCS);
                float solidDepth = LinearEyeDepth(SampleSceneDepth(screenUV), _ZBufferParams);
                float3 towardEye = GetWorldSpaceNormalizeViewDir(input.posWS);
                half edges = pow(saturate(sin(input.uv.x * PI)), 2.2);
                #if defined(_CTCC_ROUND_BEAM)
                float3 axis = lerp(_ConeTop.xyz, _ConeBottom.xyz, input.uv.y);
                float3 radial = SafeNormalize(float3(input.posWS.x - axis.x, 0, input.posWS.z - axis.z));
                edges = pow(smoothstep(0.14, 0.88, abs(dot(radial, towardEye))), 1.75);
                #else
                // Grazing-angle fade prevents hard glowing slivers on crossed window shaft planes
                float3 nWS = SafeNormalize(input.normalWS);
                edges *= smoothstep(0.10, 0.58, abs(dot(nWS, towardEye)));
                #endif
                half decay = pow(1.0 - smoothstep(0.02, 0.96, input.uv.y), 1.55) * smoothstep(0.01, 0.18, input.uv.y);
                half proximity = smoothstep(0.28, 0.95, input.eyeDepth) * saturate((solidDepth - input.eyeDepth) / 0.55);
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
