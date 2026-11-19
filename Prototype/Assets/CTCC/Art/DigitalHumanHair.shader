Shader "CTCC/DigitalHumanHair"
{
    Properties
    {
        _BaseMap("Hair Strands RGBA (sRGB)", 2D) = "white" {}
        _BaseColor("Hair Tint", Color) = (1, 1, 1, 1)
        _BumpMap("Hair Strand Normal", 2D) = "bump" {}
        _BumpScale("Normal Scale", Range(0.0, 2.0)) = 0.72
        _FlowMap("Hair Flow & ID (Linear)", 2D) = "gray" {}
        _Cutoff("Core Alpha Cutoff", Range(0.15, 0.85)) = 0.48
        _SpecColorPrimary("Primary Specular Tint", Color) = (0.85, 0.80, 0.76, 1.0)
        _SpecColorSecondary("Secondary Warm Specular Tint", Color) = (0.58, 0.36, 0.24, 1.0)
        _ShiftPrimary("Primary Tangent Shift", Range(-0.5, 0.5)) = -0.10
        _ShiftSecondary("Secondary Tangent Shift", Range(-0.5, 0.5)) = 0.14
        _SpecPowerPrimary("Primary Spec Power", Range(8.0, 180.0)) = 72.0
        _SpecPowerSecondary("Secondary Spec Power", Range(4.0, 96.0)) = 26.0
        _SpecIntensity("Anisotropic Spec Intensity", Range(0.0, 1.5)) = 0.42
        _BackScatter("Back-Lit Strand Rim", Range(0.0, 1.0)) = 0.25
    }

    SubShader
    {
        Tags
        {
            "RenderPipeline" = "UniversalPipeline"
            "RenderType" = "TransparentCutout"
            "Queue" = "AlphaTest+5"
        }

        HLSLINCLUDE
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

        CBUFFER_START(UnityPerMaterial)
            float4 _BaseMap_ST;
            half4 _BaseColor;
            half _BumpScale;
            half _Cutoff;
            half4 _SpecColorPrimary;
            half4 _SpecColorSecondary;
            half _ShiftPrimary;
            half _ShiftSecondary;
            half _SpecPowerPrimary;
            half _SpecPowerSecondary;
            half _SpecIntensity;
            half _BackScatter;
        CBUFFER_END
        ENDHLSL

        // PASS 1: Solid opaque hair card core (ZWrite On, crisp depth & zero sorting artifacts!)
        Pass
        {
            Name "HairCoreOpaque"
            Tags { "LightMode" = "UniversalForward" }
            Cull Off
            ZWrite On

            HLSLPROGRAM
            #pragma target 3.0
            #pragma vertex Vert
            #pragma fragment FragCore

            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile _ _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS
            #pragma multi_compile_fragment _ _ADDITIONAL_LIGHT_SHADOWS
            #pragma multi_compile_fragment _ _SHADOWS_SOFT _SHADOWS_SOFT_LOW _SHADOWS_SOFT_MEDIUM _SHADOWS_SOFT_HIGH
            #pragma multi_compile _ _CLUSTER_LIGHT_LOOP
            #pragma multi_compile_fog

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            TEXTURE2D(_BaseMap); SAMPLER(sampler_BaseMap);
            TEXTURE2D(_BumpMap); SAMPLER(sampler_BumpMap);
            TEXTURE2D(_FlowMap); SAMPLER(sampler_FlowMap);

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
                float4 shadowCoord : TEXCOORD6;
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
                o.shadowCoord = GetShadowCoord(posInputs);
                return o;
            }

            // Kajiya-Kay / Marschner dual anisotropic hair highlight
            half StrandSpecular(half3 T_shifted, half3 V, half3 L, half exponent)
            {
                half3 H = normalize(L + V);
                half dotTH = dot(T_shifted, H);
                half sinTH = sqrt(max(0.0h, 1.0h - dotTH * dotTH));
                half dirAtten = smoothstep(-1.0h, 0.0h, dotTH);
                return dirAtten * pow(sinTH, exponent);
            }

            half3 EvaluateHairLight(Light light, half3 albedo, half3 N, half3 T_strand, half3 V, half strandVar)
            {
                half atten = light.distanceAttenuation * light.shadowAttenuation;
                if (atten <= 0.0001h)
                    return 0.0h;

                half3 L = light.direction;
                half3 radiance = light.color * atten;
                half wrapDiff = saturate((dot(N, L) + 0.35h) / 1.35h);

                half shiftJitter = (strandVar - 0.5h) * 0.22h;
                half3 T1 = normalize(T_strand + N * (_ShiftPrimary + shiftJitter));
                half3 T2 = normalize(T_strand + N * (_ShiftSecondary + shiftJitter * 1.4h));

                half spec1 = StrandSpecular(T1, V, L, _SpecPowerPrimary);
                half spec2 = StrandSpecular(T2, V, L, _SpecPowerSecondary);
                half3 anisoSpec = (_SpecColorPrimary.rgb * spec1 * 0.55h + _SpecColorSecondary.rgb * spec2 * 0.45h) * _SpecIntensity * saturate(dot(N, L) + 0.40h);

                // Subtle rim scattering along backlit strand edges
                half rim = pow(saturate(dot(V, -L)), 3.0h) * _BackScatter * 0.35h;

                return (albedo * (wrapDiff + rim) + anisoSpec) * radiance;
            }

            half3 ShadeHair(Varyings input, bool isFrontFace, out half alphaOut)
            {
                half4 baseSample = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, input.uv);
                alphaOut = baseSample.a;
                half3 albedo = baseSample.rgb * _BaseColor.rgb;

                float3 N_geom = normalize(isFrontFace ? input.normalWS : -input.normalWS);
                float3 T_ws = normalize(input.tangentWS.xyz);
                float3 B_ws = normalize(cross(N_geom, T_ws) * input.tangentWS.w);
                float3x3 TBN = float3x3(T_ws, B_ws, N_geom);

                half3 nTS = UnpackNormalScale(SAMPLE_TEXTURE2D(_BumpMap, sampler_BumpMap, input.uv), _BumpScale);
                half3 N = normalize(mul(nTS, TBN));

                half3 flowSample = SAMPLE_TEXTURE2D(_FlowMap, sampler_FlowMap, input.uv).rgb;
                float2 flowDir2D = flowSample.rg * 2.0 - 1.0;
                float flen = length(flowDir2D);
                float2 fd = flen > 0.1 ? (flowDir2D / flen) : float2(0.0, 1.0);
                half3 T_strand = normalize(T_ws * fd.x + B_ws * fd.y);
                half strandVar = flowSample.b;

                half3 V = normalize(_WorldSpaceCameraPos - input.positionWS);

                InputData inputData = (InputData)0;
                inputData.positionWS = input.positionWS;
                inputData.normalWS = N;
                inputData.viewDirectionWS = V;
                inputData.normalizedScreenSpaceUV = GetNormalizedScreenSpaceUV(input.positionCS);
                #if defined(REQUIRES_VERTEX_SHADOW_COORD_INTERPOLATOR)
                    inputData.shadowCoord = input.shadowCoord;
                #elif defined(MAIN_LIGHT_CALCULATE_SHADOWS)
                    inputData.shadowCoord = TransformWorldToShadowCoord(input.positionWS);
                #else
                    inputData.shadowCoord = float4(0, 0, 0, 0);
                #endif

                half4 shadowMask = half4(1, 1, 1, 1);
                Light mainLight = GetMainLight(inputData.shadowCoord, input.positionWS, shadowMask);
                half3 color = EvaluateHairLight(mainLight, albedo, N, T_strand, V, strandVar);

                #if defined(_ADDITIONAL_LIGHTS)
                    uint pixelLightCount = GetAdditionalLightsCount();
                    #if USE_CLUSTER_LIGHT_LOOP
                    [loop] for (uint lightIndex = 0; lightIndex < min(URP_FP_DIRECTIONAL_LIGHTS_COUNT, MAX_VISIBLE_LIGHTS); lightIndex++)
                    {
                        CLUSTER_LIGHT_LOOP_SUBTRACTIVE_LIGHT_CHECK
                        color += EvaluateHairLight(GetAdditionalLight(lightIndex, input.positionWS, shadowMask), albedo, N, T_strand, V, strandVar);
                    }
                    #endif
                    LIGHT_LOOP_BEGIN(pixelLightCount)
                        color += EvaluateHairLight(GetAdditionalLight(lightIndex, input.positionWS, shadowMask), albedo, N, T_strand, V, strandVar);
                    LIGHT_LOOP_END
                #endif

                half3 gi = SampleSHPixel(input.vertexSH, N);
                color += albedo * gi * 0.85h;
                return MixFog(color, input.fogFactor);
            }

            half4 FragCore(Varyings input, bool isFrontFace : SV_IsFrontFace) : SV_Target
            {
                half alpha;
                half3 col = ShadeHair(input, isFrontFace, alpha);
                clip(alpha - _Cutoff);
                return half4(col, 1.0h);
            }
            ENDHLSL
        }

        // PASS 2: Feathered alpha-blended hairline & wispy strand fringes (ZWrite Off, ZTest LEqual)
        Pass
        {
            Name "HairWispsBlend"
            Tags { "LightMode" = "SRPDefaultUnlit" }
            Cull Off
            ZWrite Off
            ZTest LEqual
            Blend SrcAlpha OneMinusSrcAlpha

            HLSLPROGRAM
            #pragma target 3.0
            #pragma vertex Vert
            #pragma fragment FragWisp

            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile _ _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS
            #pragma multi_compile _ _CLUSTER_LIGHT_LOOP
            #pragma multi_compile_fog

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            TEXTURE2D(_BaseMap); SAMPLER(sampler_BaseMap);
            TEXTURE2D(_FlowMap); SAMPLER(sampler_FlowMap);

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
                half3 vertexSH : TEXCOORD3;
                float fogFactor : TEXCOORD4;
            };

            Varyings Vert(Attributes input)
            {
                Varyings o;
                VertexPositionInputs posInputs = GetVertexPositionInputs(input.positionOS.xyz);
                o.positionCS = posInputs.positionCS;
                o.positionWS = posInputs.positionWS;
                o.normalWS = TransformObjectToWorldNormal(input.normalOS);
                o.uv = TRANSFORM_TEX(input.uv, _BaseMap);
                o.vertexSH = SampleSHVertex(o.normalWS);
                o.fogFactor = ComputeFogFactor(posInputs.positionCS.z);
                return o;
            }

            half4 FragWisp(Varyings input, bool isFrontFace : SV_IsFrontFace) : SV_Target
            {
                half4 tex = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, input.uv);
                // Render only soft semi-transparent edges below _Cutoff so hairline & ends feather organically!
                clip(tex.a - 0.03h);
                if (tex.a >= _Cutoff)
                    discard;

                half3 albedo = tex.rgb * _BaseColor.rgb;
                half3 N = normalize(isFrontFace ? input.normalWS : -input.normalWS);
                Light mainLight = GetMainLight();
                half diff = saturate((dot(N, mainLight.direction) + 0.35h) / 1.35h);
                half3 col = albedo * (SampleSHPixel(input.vertexSH, N) * 0.85h + mainLight.color * diff);

                #if defined(_ADDITIONAL_LIGHTS)
                    InputData inputData = (InputData)0;
                    inputData.positionWS = input.positionWS;
                    inputData.normalizedScreenSpaceUV = GetNormalizedScreenSpaceUV(input.positionCS);
                    uint pixelLightCount = GetAdditionalLightsCount();
                    LIGHT_LOOP_BEGIN(pixelLightCount)
                        Light l = GetAdditionalLight(lightIndex, input.positionWS);
                        col += albedo * l.color * l.distanceAttenuation * saturate((dot(N, l.direction) + 0.35h) / 1.35h);
                    LIGHT_LOOP_END
                #endif

                col = MixFog(col, input.fogFactor);
                half softAlpha = saturate(tex.a / max(_Cutoff, 0.05h));
                return half4(col, softAlpha * 0.92h);
            }
            ENDHLSL
        }

        Pass
        {
            Name "ShadowCaster"
            Tags { "LightMode" = "ShadowCaster" }
            ZWrite On
            ZTest LEqual
            ColorMask 0
            Cull Off

            HLSLPROGRAM
            #pragma target 2.0
            #pragma vertex VertShadow
            #pragma fragment FragShadow
            #pragma multi_compile_vertex _ _CASTING_PUNCTUAL_LIGHT_SHADOW
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Shadows.hlsl"

            TEXTURE2D(_BaseMap); SAMPLER(sampler_BaseMap);
            float3 _LightDirection;
            float3 _LightPosition;

            struct Attributes { float4 positionOS : POSITION; float3 normalOS : NORMAL; float2 uv : TEXCOORD0; };
            struct Varyings { float4 positionCS : SV_POSITION; float2 uv : TEXCOORD0; };

            Varyings VertShadow(Attributes input)
            {
                Varyings o;
                float3 posWS = TransformObjectToWorld(input.positionOS.xyz);
                float3 normWS = TransformObjectToWorldNormal(input.normalOS);
                #if _CASTING_PUNCTUAL_LIGHT_SHADOW
                    float3 lightDir = normalize(_LightPosition - posWS);
                #else
                    float3 lightDir = _LightDirection;
                #endif
                float4 posCS = TransformWorldToHClip(ApplyShadowBias(posWS, normWS, lightDir));
                #if UNITY_REVERSED_Z
                    posCS.z = min(posCS.z, UNITY_NEAR_CLIP_VALUE);
                #else
                    posCS.z = max(posCS.z, UNITY_NEAR_CLIP_VALUE);
                #endif
                o.positionCS = posCS;
                o.uv = TRANSFORM_TEX(input.uv, _BaseMap);
                return o;
            }
            half4 FragShadow(Varyings input) : SV_Target
            {
                clip(SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, input.uv).a - _Cutoff);
                return 0;
            }
            ENDHLSL
        }

        Pass
        {
            Name "DepthOnly"
            Tags { "LightMode" = "DepthOnly" }
            ZWrite On
            ColorMask R
            Cull Off

            HLSLPROGRAM
            #pragma target 2.0
            #pragma vertex VertDepth
            #pragma fragment FragDepth

            TEXTURE2D(_BaseMap); SAMPLER(sampler_BaseMap);

            struct Attributes { float4 positionOS : POSITION; float2 uv : TEXCOORD0; };
            struct Varyings { float4 positionCS : SV_POSITION; float2 uv : TEXCOORD0; };

            Varyings VertDepth(Attributes input)
            {
                Varyings o;
                o.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                o.uv = TRANSFORM_TEX(input.uv, _BaseMap);
                return o;
            }
            half4 FragDepth(Varyings input) : SV_Target
            {
                clip(SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, input.uv).a - _Cutoff);
                return 0;
            }
            ENDHLSL
        }

        Pass
        {
            Name "DepthNormals"
            Tags { "LightMode" = "DepthNormals" }
            ZWrite On
            Cull Off

            HLSLPROGRAM
            #pragma target 2.0
            #pragma vertex VertDN
            #pragma fragment FragDN

            TEXTURE2D(_BaseMap); SAMPLER(sampler_BaseMap);

            struct Attributes { float4 positionOS : POSITION; float3 normalOS : NORMAL; float2 uv : TEXCOORD0; };
            struct Varyings { float4 positionCS : SV_POSITION; float3 normalWS : TEXCOORD0; float2 uv : TEXCOORD1; };

            Varyings VertDN(Attributes input)
            {
                Varyings o;
                o.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                o.normalWS = TransformObjectToWorldNormal(input.normalOS);
                o.uv = TRANSFORM_TEX(input.uv, _BaseMap);
                return o;
            }
            half4 FragDN(Varyings input, bool isFrontFace : SV_IsFrontFace) : SV_Target
            {
                clip(SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, input.uv).a - _Cutoff);
                return half4(NormalizeNormalPerPixel(isFrontFace ? input.normalWS : -input.normalWS), 0.0h);
            }
            ENDHLSL
        }
    }
}
