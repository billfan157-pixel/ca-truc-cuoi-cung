Shader "CTCC/ReceptionGlazing"
{
    Properties
    {
        _Tint("Transmission tint", Color) = (0.30, 0.40, 0.36, 0.06)
        _EdgeSoil("Local cleaning residue", Range(0,1)) = 0.15
        _BroadGlint("Soft reflected light", Range(0,1)) = 0.22
    }
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "RenderType"="Transparent" "Queue"="Transparent+2" }
        Pass
        {
            Name "Reception Glass"
            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            Cull Back

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                half4 _Tint;
                half _EdgeSoil;
                half _BroadGlint;
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
                float3 positionWS : TEXCOORD0;
                half3 normalWS : TEXCOORD1;
                float2 uv : TEXCOORD2;
            };

            Varyings Vert(Attributes input)
            {
                Varyings output;
                output.positionWS = TransformObjectToWorld(input.positionOS);
                output.positionCS = TransformWorldToHClip(output.positionWS);
                output.normalWS = TransformObjectToWorldNormal(input.normalOS);
                output.uv = input.uv;
                return output;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                float3 V = normalize(_WorldSpaceCameraPos - input.positionWS);
                half fresnel = pow(1 - saturate(abs(dot(normalize(input.normalWS), V))), 3);
                float2 pane = input.positionWS.zy;
                // Janitorial wipe arcs near the service ledge, with a different density at each side.
                float2 wipeCenter = (pane - float2(-3.13, 1.39)) * float2(0.72, 1.65);
                float wipe = sin(length(wipeCenter) * 95 + 0.3 * sin(pane.x * 29));
                float ledgeSoil = exp(-(pane.y - 1.40) * (pane.y - 1.40) * 26);
                float sideSoil = smoothstep(0.27, 0.9, abs(pane.x + 3.30));
                half deposits = saturate(0.59 * ledgeSoil + 0.28 * sideSoil + 0.11 * wipe * ledgeSoil);
                half softGlow = 0.12 + 0.5 * fresnel + 0.34 * deposits;
                half3 tint = _Tint.rgb + _BroadGlint * softGlow * half3(0.74, 0.78, 0.74);
                half opacity = saturate(_Tint.a + fresnel * 0.18 + _EdgeSoil * deposits * 0.40);
                return half4(tint, opacity);
            }
            ENDHLSL
        }
    }
}
