Shader "CTCC/InspectPaperRaking"
{
    Properties
    {
        _BaseMap("Paper Albedo", 2D) = "white" {}
        _BaseColor("Base Tint", Color) = (0.84, 0.83, 0.80, 1)
        _IndentNormalMap("Indentation Normal", 2D) = "bump" {}
        _IndentMaskMap("Indentation Groove Mask", 2D) = "black" {}
        _IndentScale("Indentation Relief Scale", Range(0, 3)) = 1.65
        _RakingReveal("Raking Grazing Reveal", Range(0, 1)) = 0
        _LightDirWS("Key Light Dir WS", Vector) = (0.45, 0.50, -0.74, 0)
        _LightColor("Key Light Color", Color) = (1.0, 0.95, 0.87, 1)
        _RimDirWS("Rim Light Dir WS", Vector) = (-0.52, -0.20, 0.83, 0)
        _RimColor("Rim Light Color", Color) = (0.72, 0.84, 0.90, 1)
        _Cull("Cull Mode", Float) = 2
    }
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "RenderType"="Opaque" "Queue"="Geometry" }
        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode"="UniversalForward" }
            Cull [_Cull]
            ZWrite On

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            TEXTURE2D(_BaseMap);
            SAMPLER(sampler_BaseMap);
            TEXTURE2D(_IndentNormalMap);
            SAMPLER(sampler_IndentNormalMap);
            TEXTURE2D(_IndentMaskMap);
            SAMPLER(sampler_IndentMaskMap);

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseMap_ST;
                half4 _BaseColor;
                half _IndentScale;
                half _RakingReveal;
                float4 _LightDirWS;
                half4 _LightColor;
                float4 _RimDirWS;
                half4 _RimColor;
            CBUFFER_END

            struct Attributes
            {
                float3 positionOS : POSITION;
                float3 normalOS : NORMAL;
                float4 tangentOS : TANGENT;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                float3 posWS : TEXCOORD1;
                float3 normalWS : TEXCOORD2;
                float3 tangentWS : TEXCOORD3;
                float3 bitangentWS : TEXCOORD4;
            };

            Varyings Vert(Attributes input)
            {
                Varyings output;
                VertexPositionInputs pos = GetVertexPositionInputs(input.positionOS);
                VertexNormalInputs nrm = GetVertexNormalInputs(input.normalOS, input.tangentOS);
                output.positionCS = pos.positionCS;
                output.uv = TRANSFORM_TEX(input.uv, _BaseMap);
                output.posWS = pos.positionWS;
                output.normalWS = nrm.normalWS;
                output.tangentWS = nrm.tangentWS;
                output.bitangentWS = nrm.bitangentWS;
                return output;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                half3 albedo = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, input.uv).rgb * _BaseColor.rgb;
                half indentMask = SAMPLE_TEXTURE2D(_IndentMaskMap, sampler_IndentMaskMap, input.uv).r;

                // Blend indentation normal strength from subtle paper tooth when flat to crisp ballpoint relief at raking angle.
                half effectiveScale = lerp(0.18h, _IndentScale, saturate(_RakingReveal));
                half3 normalTS = UnpackNormalScale(SAMPLE_TEXTURE2D(_IndentNormalMap, sampler_IndentNormalMap, input.uv), effectiveScale);
                float3x3 tbn = float3x3(normalize(input.tangentWS), normalize(input.bitangentWS), normalize(input.normalWS));
                float3 N = normalize(mul(normalTS, tbn));
                float3 flatN = normalize(input.normalWS);

                float3 V = normalize(GetWorldSpaceViewDir(input.posWS));
                float3 Lkey = normalize(_LightDirWS.xyz);
                float3 Lrim = normalize(_RimDirWS.xyz);

                // Calibrated key + soft rim + gentle ambient keeping paper typography crisp with zero white washout
                half ndlKey = saturate(dot(N, Lkey));
                half flatNdl = saturate(dot(flatN, Lkey));
                half ndlRim = saturate(dot(N, Lrim));

                // Grazing raking specular catch + directional groove shadow along depressed pen strokes
                float3 H = normalize(Lkey + V);
                half ndh = saturate(dot(N, H));
                half flatNdh = saturate(dot(flatN, H));
                half grazingDelta = saturate((ndh - flatNdh) * 6.5h + abs(ndlKey - flatNdl) * 3.8h);
                half grooveShadow = saturate((flatNdl - ndlKey) * 4.5h) * indentMask;
                half grooveRimSpec = pow(ndh, 24.0h) * indentMask * (0.45h + 1.35h * _RakingReveal);

                half3 ambient = half3(0.46h, 0.45h, 0.43h);
                half3 diffuse = ambient + _LightColor.rgb * (ndlKey * 0.56h) + _RimColor.rgb * (ndlRim * 0.18h);
                half3 lit = albedo * diffuse;

                // Under grazing light, ballpoint indentation troughs darken slightly while pressed ridge edges catch warm light!
                half reveal = saturate(_RakingReveal);
                half troughDarken = indentMask * reveal * (0.34h + 0.24h * grooveShadow);
                lit *= (1.0h - troughDarken);
                lit += _LightColor.rgb * (grooveRimSpec * 0.32h + grazingDelta * indentMask * reveal * 0.28h);

                return half4(lit, 1.0h);
            }
            ENDHLSL
        }
    }
    FallBack "Universal Render Pipeline/Lit"
}
