Shader "CTCC/ApparitionHuman"
{
    Properties
    {
        _BaseMap("Human Surface Albedo RGBA (sRGB)", 2D) = "white" {}
        _BaseColor("Tint", Color) = (1, 1, 1, 1)
        _BumpMap("Surface Normal", 2D) = "bump" {}
        _BumpScale("Normal Scale", Range(0.0, 2.0)) = 0.75
        _MaskMap("Mask (G=AO, A=Smoothness)", 2D) = "white" {}
        _Cutoff("Texture Alpha Cutoff (Hair/Lashes)", Range(0.0, 0.85)) = 0.0
        _RimColor("Memory Halo Rim Color", Color) = (0.78, 0.90, 0.88, 1.0)
        _RimPower("Halo Rim Power", Range(0.8, 5.0)) = 2.6
        _RimStrength("Halo Rim Strength", Range(0.0, 1.2)) = 0.34
        _Desaturate("Memory Desaturation", Range(0.0, 0.6)) = 0.14
        _Alpha("Visibility Alpha", Range(0.0, 1.0)) = 0.82
    }

    SubShader
    {
        Tags
        {
            "RenderPipeline" = "UniversalPipeline"
            "RenderType" = "Transparent"
            "Queue" = "Transparent+10"
        }

        HLSLINCLUDE
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

        CBUFFER_START(UnityPerMaterial)
            float4 _BaseMap_ST;
            half4 _BaseColor;
            half _BumpScale;
            half _Cutoff;
            half4 _RimColor;
            half _RimPower;
            half _RimStrength;
            half _Desaturate;
            half _Alpha;
        CBUFFER_END
        ENDHLSL

        // Depth prepass so Bé An's human body/clothes never show overlapping double-transparent limbs!
        Pass
        {
            Name "ApparitionDepthPrepass"
            Tags { "LightMode" = "SRPDefaultUnlit" }
            Cull Back
            ZWrite On
            ColorMask 0

            HLSLPROGRAM
            #pragma target 2.0
            #pragma vertex VertPre
            #pragma fragment FragPre

            TEXTURE2D(_BaseMap); SAMPLER(sampler_BaseMap);

            struct Attributes { float4 positionOS : POSITION; float2 uv : TEXCOORD0; };
            struct Varyings { float4 positionCS : SV_POSITION; float2 uv : TEXCOORD0; };

            Varyings VertPre(Attributes input)
            {
                Varyings o;
                o.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                o.uv = TRANSFORM_TEX(input.uv, _BaseMap);
                return o;
            }
            half4 FragPre(Varyings input) : SV_Target
            {
                clip(_Alpha - 0.02h);
                if (_Cutoff > 0.01h)
                    clip(SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, input.uv).a - _Cutoff);
                return 0;
            }
            ENDHLSL
        }

        Pass
        {
            Name "ApparitionHumanForward"
            Tags { "LightMode" = "UniversalForward" }
            Cull Back
            ZWrite Off
            ZTest LEqual
            Blend SrcAlpha OneMinusSrcAlpha

            HLSLPROGRAM
            #pragma target 3.0
            #pragma vertex Vert
            #pragma fragment Frag

            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile _ _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS
            #pragma multi_compile _ _CLUSTER_LIGHT_LOOP
            #pragma multi_compile_fog

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            TEXTURE2D(_BaseMap); SAMPLER(sampler_BaseMap);
            TEXTURE2D(_BumpMap); SAMPLER(sampler_BumpMap);
            TEXTURE2D(_MaskMap); SAMPLER(sampler_MaskMap);

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                float4 tangentOS : TANGENT;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                float3 positionWS : TEXCOORD1;
                float3 normalWS : TEXCOORD2;
                float4 tangentWS : TEXCOORD3;
                half3 vertexSH : TEXCOORD4;
                float fogFactor : TEXCOORD5;
            };

            Varyings Vert(Attributes input)
            {
                Varyings o;
                VertexPositionInputs posInputs = GetVertexPositionInputs(input.positionOS.xyz);
                VertexNormalInputs normInputs = GetVertexNormalInputs(input.normalOS, input.tangentOS);
                o.positionCS = posInputs.positionCS;
                o.positionWS = posInputs.positionWS;
                o.normalWS = normInputs.normalWS;
                o.tangentWS = float4(normInputs.tangentWS, input.tangentOS.w * GetOddNegativeScale());
                o.uv = TRANSFORM_TEX(input.uv, _BaseMap);
                o.vertexSH = SampleSHVertex(normInputs.normalWS);
                o.fogFactor = ComputeFogFactor(posInputs.positionCS.z);
                return o;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                if (_Alpha <= 0.005h)
                    discard;

                half4 baseSample = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, input.uv);
                if (_Cutoff > 0.01h)
                    clip(baseSample.a - _Cutoff);

                half3 albedo = baseSample.rgb * _BaseColor.rgb;
                half lum = dot(albedo, half3(0.299h, 0.587h, 0.114h));
                albedo = lerp(albedo, lum.xxx, _Desaturate);

                half4 mask = SAMPLE_TEXTURE2D(_MaskMap, sampler_MaskMap, input.uv);
                half ao = lerp(1.0h, mask.g, 0.65h);

                float3 N_geom = normalize(input.normalWS);
                float3 T_ws = normalize(input.tangentWS.xyz);
                float3 B_ws = normalize(cross(N_geom, T_ws) * input.tangentWS.w);
                half3 nTS = UnpackNormalScale(SAMPLE_TEXTURE2D(_BumpMap, sampler_BumpMap, input.uv), _BumpScale);
                half3 N = normalize(mul(nTS, float3x3(T_ws, B_ws, N_geom)));
                half3 V = normalize(_WorldSpaceCameraPos - input.positionWS);

                // Warm human lighting preserved underneath soft memory translucency
                Light mainLight = GetMainLight();
                half mainDiff = saturate((dot(N, mainLight.direction) + 0.25h) / 1.25h);
                half3 lit = albedo * (SampleSHPixel(input.vertexSH, N) * 1.05h + mainLight.color * mainDiff * 0.90h) * ao;

                #if defined(_ADDITIONAL_LIGHTS)
                    InputData inputData = (InputData)0;
                    inputData.positionWS = input.positionWS;
                    inputData.normalizedScreenSpaceUV = GetNormalizedScreenSpaceUV(input.positionCS);
                    uint pixelLightCount = GetAdditionalLightsCount();
                    #if USE_CLUSTER_LIGHT_LOOP
                    [loop] for (uint lightIndex = 0; lightIndex < min(URP_FP_DIRECTIONAL_LIGHTS_COUNT, MAX_VISIBLE_LIGHTS); lightIndex++)
                    {
                        CLUSTER_LIGHT_LOOP_SUBTRACTIVE_LIGHT_CHECK
                        Light addLight = GetAdditionalLight(lightIndex, input.positionWS);
                        lit += albedo * addLight.color * addLight.distanceAttenuation * saturate((dot(N, addLight.direction) + 0.25h) / 1.25h) * ao;
                    }
                    #endif
                    LIGHT_LOOP_BEGIN(pixelLightCount)
                        Light addLight = GetAdditionalLight(lightIndex, input.positionWS);
                        lit += albedo * addLight.color * addLight.distanceAttenuation * saturate((dot(N, addLight.direction) + 0.25h) / 1.25h) * ao;
                    LIGHT_LOOP_END
                #endif

                // Subtle gentle memory Fresnel halo (preserves full human child materials beneath!)
                half rim = pow(1.0h - saturate(dot(N, V)), _RimPower) * _RimStrength;
                half3 finalCol = lit + _RimColor.rgb * rim;
                finalCol = MixFog(finalCol, input.fogFactor);

                half texAlpha = (_Cutoff > 0.01h) ? 1.0h : baseSample.a;
                half outAlpha = saturate(_Alpha * texAlpha * (0.76h + 0.24h * rim));
                return half4(finalCol, outAlpha);
            }
            ENDHLSL
        }
    }
}
