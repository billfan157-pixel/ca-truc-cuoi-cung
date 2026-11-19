Shader "CTCC/ApparitionSoft"
{
    Properties
    {
        _BaseColor("Base Color", Color) = (0.76, 0.86, 0.84, 1)
        _RimColor("Rim Color", Color) = (0.72, 0.92, 0.90, 1)
        _RimPower("Rim Power", Range(0.5, 6.0)) = 2.4
        _Alpha("Visibility Alpha", Range(0.0, 1.0)) = 0.0
    }
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "RenderType"="Transparent" "Queue"="Transparent+10" }
        Pass
        {
            Name "ApparitionForward"
            Cull Back
            ZWrite Off
            Blend SrcAlpha OneMinusSrcAlpha

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                half4 _BaseColor;
                half4 _RimColor;
                half _RimPower;
                half _Alpha;
            CBUFFER_END

            struct Attributes
            {
                float3 positionOS : POSITION;
                float3 normalOS : NORMAL;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 normalWS : TEXCOORD0;
                float3 viewDirWS : TEXCOORD1;
            };

            Varyings Vert(Attributes input)
            {
                Varyings output;
                VertexPositionInputs pos = GetVertexPositionInputs(input.positionOS);
                output.positionCS = pos.positionCS;
                output.normalWS = TransformObjectToWorldNormal(input.normalOS);
                output.viewDirWS = GetWorldSpaceNormalizeViewDir(pos.positionWS);
                return output;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                half3 n = normalize(input.normalWS);
                half3 v = normalize(input.viewDirWS);
                half ndotv = saturate(dot(n, v));
                half rim = pow(1.0h - ndotv, _RimPower);
                half3 col = lerp(_BaseColor.rgb * 0.72h, _RimColor.rgb * 1.15h, rim);
                half alpha = saturate(_Alpha * (0.28h + 0.72h * rim));
                return half4(col, alpha);
            }
            ENDHLSL
        }
    }
}
