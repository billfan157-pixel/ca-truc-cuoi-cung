Shader "CTCC/PerceptualEcho"
{
    Properties
    {
        _BaseMap("Imprint Map", 2D) = "white" {}
        _BaseColor("Tint Color", Color) = (0.86, 0.92, 0.90, 1)
        _RimColor("Rim Color", Color) = (0.76, 0.93, 0.91, 1)
        _Alpha("Visibility Alpha", Range(0.0, 1.0)) = 0.0
        _RimPower("Rim Power", Range(0.6, 5.0)) = 2.1
        _PulseSpeed("Breath Speed", Range(0.0, 4.0)) = 0.90
        _DriftAmp("Subtle Drift", Range(0.0, 0.03)) = 0.006
        _EdgeFeather("Edge Feather", Range(0.02, 0.45)) = 0.16
        _ReduceMotion("Reduce Motion", Range(0.0, 1.0)) = 0.0
    }
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "RenderType"="Transparent" "Queue"="Transparent+15" }
        Pass
        {
            Name "PerceptualEchoForward"
            Cull Off
            ZWrite Off
            Blend SrcAlpha OneMinusSrcAlpha

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            TEXTURE2D(_BaseMap);
            SAMPLER(sampler_BaseMap);

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseMap_ST;
                half4 _BaseColor;
                half4 _RimColor;
                half _Alpha;
                half _RimPower;
                half _PulseSpeed;
                half _DriftAmp;
                half _EdgeFeather;
                half _ReduceMotion;
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
                float3 normalWS : TEXCOORD1;
                float3 viewDirWS : TEXCOORD2;
                float3 positionWS : TEXCOORD3;
            };

            Varyings Vert(Attributes input)
            {
                Varyings output;
                float motion = saturate(1.0 - _ReduceMotion);
                float3 posOS = input.positionOS;
                float wave = sin(_Time.y * _PulseSpeed + posOS.x * 6.5 + posOS.y * 4.2 + posOS.z * 5.0);
                posOS += input.normalOS * (wave * _DriftAmp * motion);

                VertexPositionInputs pos = GetVertexPositionInputs(posOS);
                output.positionCS = pos.positionCS;
                output.uv = TRANSFORM_TEX(input.uv, _BaseMap);
                output.normalWS = TransformObjectToWorldNormal(input.normalOS);
                output.viewDirWS = GetWorldSpaceNormalizeViewDir(pos.positionWS);
                output.positionWS = pos.positionWS;
                return output;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                half4 tex = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, input.uv);
                half3 n = normalize(input.normalWS);
                half3 v = normalize(input.viewDirWS);
                half ndotv = abs(dot(n, v));
                half rim = pow(saturate(1.0h - ndotv), _RimPower);

                // Soft elliptical border feathering eliminates any hard rectangular polygon edge.
                half2 edge = min(input.uv, 1.0h - input.uv);
                half edgeMask = smoothstep(0.0h, max(0.02h, _EdgeFeather), min(edge.x, edge.y));
                half2 radial = (input.uv - 0.5h) * 2.0h;
                half radialMask = 1.0h - smoothstep(0.76h, 1.06h, length(radial));
                half softMask = saturate(edgeMask * (0.45h + 0.55h * radialMask));

                half motion = saturate(1.0h - _ReduceMotion);
                half breath = lerp(1.0h, 0.90h + 0.10h * sin(_Time.y * _PulseSpeed * 1.15h + input.positionWS.y * 3.0h), motion);

                half3 col = tex.rgb * _BaseColor.rgb;
                col = lerp(col, _RimColor.rgb * 1.12h, rim * 0.42h);
                half alpha = saturate(tex.a * _BaseColor.a * _Alpha * softMask * breath * (0.78h + 0.22h * rim));
                clip(alpha - 0.004h);
                return half4(col, alpha);
            }
            ENDHLSL
        }
    }
}
