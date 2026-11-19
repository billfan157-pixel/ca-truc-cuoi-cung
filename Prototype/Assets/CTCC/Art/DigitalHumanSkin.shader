Shader "CTCC/DigitalHumanSkin"
{
    Properties
    {
        _BaseMap("Skin Albedo (sRGB)", 2D) = "white" {}
        _BaseColor("Tint", Color) = (1, 1, 1, 1)
        _BumpMap("Anatomical Normal", 2D) = "bump" {}
        _BumpScale("Normal Scale", Range(0.0, 2.0)) = 0.85
        _DetailNormalMap("Micro-Pore Normal", 2D) = "bump" {}
        _DetailTiling("Micro-Pore Tiling", Float) = 14.0
        _DetailScale("Micro-Pore Strength", Range(0.0, 1.5)) = 0.34
        _DetailFadeDist("Micro-Pore Fade Distance", Float) = 2.8
        _MaskMap("Mask (G=AO, B=SSS Thickness, A=Smoothness)", 2D) = "white" {}
        _SmoothnessScale("Smoothness Scale", Range(0.1, 1.5)) = 0.92
        _OcclusionStrength("AO Strength", Range(0.0, 1.0)) = 0.72
        _SSSColor("Subsurface Scatter Tint", Color) = (0.86, 0.24, 0.14, 1.0)
        _SSSStrength("Subsurface Scatter Strength", Range(0.0, 1.5)) = 0.58
        _BackScatter("Back-Scatter Translucency", Range(0.0, 1.0)) = 0.24
        _SpecIntensity("Skin Specular Intensity", Range(0.0, 1.5)) = 0.72
    }

    SubShader
    {
        Tags
        {
            "RenderPipeline" = "UniversalPipeline"
            "RenderType" = "Opaque"
            "Queue" = "Geometry"
        }

        HLSLINCLUDE
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

        CBUFFER_START(UnityPerMaterial)
            float4 _BaseMap_ST;
            half4 _BaseColor;
            half _BumpScale;
            float _DetailTiling;
            half _DetailScale;
            float _DetailFadeDist;
            half _SmoothnessScale;
            half _OcclusionStrength;
            half4 _SSSColor;
            half _SSSStrength;
            half _BackScatter;
            half _SpecIntensity;
        CBUFFER_END
        ENDHLSL

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForward" }
            Cull Back
            ZWrite On

            HLSLPROGRAM
            #pragma target 3.0
            #pragma vertex Vert
            #pragma fragment Frag

            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile _ _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS
            #pragma multi_compile_fragment _ _ADDITIONAL_LIGHT_SHADOWS
            #pragma multi_compile_fragment _ _SHADOWS_SOFT _SHADOWS_SOFT_LOW _SHADOWS_SOFT_MEDIUM _SHADOWS_SOFT_HIGH
            #pragma multi_compile_fragment _ _SCREEN_SPACE_OCCLUSION
            #pragma multi_compile _ _CLUSTER_LIGHT_LOOP
            #pragma multi_compile_fog

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            TEXTURE2D(_BaseMap);         SAMPLER(sampler_BaseMap);
            TEXTURE2D(_BumpMap);         SAMPLER(sampler_BumpMap);
            TEXTURE2D(_DetailNormalMap); SAMPLER(sampler_DetailNormalMap);
            TEXTURE2D(_MaskMap);         SAMPLER(sampler_MaskMap);

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

            half GGXDistribution(half NdotH, half roughness)
            {
                half a = max(roughness * roughness, 0.002h);
                half a2 = a * a;
                half d = (NdotH * NdotH) * (a2 - 1.0h) + 1.0h;
                return a2 / (3.14159265h * d * d + 1e-5h);
            }

            half3 EvaluateSkinLight(Light light, half3 albedo, half3 N, half3 N_macro, half3 V, half roughness, half thickness, half ao)
            {
                half atten = light.distanceAttenuation * light.shadowAttenuation;
                if (atten <= 0.0001h)
                    return 0.0h;

                half3 L = light.direction;
                half3 radiance = light.color * atten;
                half NdotL = dot(N, L);
                half NdotL_macro = dot(N_macro, L);

                // Pre-integrated RGB skin wrap scatter (hemoglobin warmth across shadow terminator)
                half sss = saturate(thickness * _SSSStrength);
                half3 wrap = half3(0.46h, 0.19h, 0.09h) * sss;
                half3 wrapDiff = saturate((NdotL_macro.xxx + wrap) / (1.0h + wrap));
                half terminator = exp(-pow((NdotL_macro - 0.05h) / max(0.24h, 0.36h * sss), 2.0h));
                half3 scatterTerm = _SSSColor.rgb * (terminator * 0.22h * sss);

                half3 diffuse = lerp(saturate(NdotL).xxx, wrapDiff, saturate(0.45h + 0.50h * sss)) + scatterTerm;

                // Soft back-scattering through thin skin (ears, nostrils, finger edges)
                half3 backVec = -(L + N_macro * 0.45h);
                half backAmt = pow(saturate(dot(V, normalize(backVec))), 3.0h) * thickness * _BackScatter;
                half3 backLit = _SSSColor.rgb * backAmt * light.color * light.distanceAttenuation;

                // Dual-lobe human sebum specular (F0 = 0.028 dielectric skin oil)
                half3 H = normalize(L + V);
                half NdotH = saturate(dot(N, H));
                half NdotV = max(dot(N, V), 0.001h);
                half VdotH = saturate(dot(V, H));
                half fresnel = 0.028h + (1.0h - 0.028h) * pow(1.0h - VdotH, 5.0h);

                half r1 = clamp(roughness * 0.64h, 0.16h, 0.88h);
                half r2 = clamp(lerp(roughness, 0.88h, 0.52h), 0.38h, 0.96h);
                half D = 0.72h * GGXDistribution(NdotH, r1) + 0.28h * GGXDistribution(NdotH, r2);
                half vis = 0.25h / max(NdotV * max(NdotL, 0.08h), 0.15h);
                half spec = D * vis * fresnel * saturate(NdotL * 4.0h) * _SpecIntensity;

                return (albedo * diffuse * ao + spec.xxx) * radiance + backLit * albedo;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                half4 baseSample = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, input.uv);
                half3 albedo = baseSample.rgb * _BaseColor.rgb;
                half4 mask = SAMPLE_TEXTURE2D(_MaskMap, sampler_MaskMap, input.uv);

                half ao = lerp(1.0h, mask.g, _OcclusionStrength);
                half thickness = mask.b;
                half smoothness = saturate(mask.a * _SmoothnessScale);
                half roughness = max(0.18h, 1.0h - smoothness);

                float3 N_geom = normalize(input.normalWS);
                float3 T_ws = normalize(input.tangentWS.xyz);
                float3 B_ws = normalize(cross(N_geom, T_ws) * input.tangentWS.w);
                float3x3 TBN = float3x3(T_ws, B_ws, N_geom);

                half3 nMacroTS = UnpackNormalScale(SAMPLE_TEXTURE2D(_BumpMap, sampler_BumpMap, input.uv), _BumpScale);
                float camDist = length(_WorldSpaceCameraPos - input.positionWS);
                half poreFade = saturate(1.0 - (camDist - 0.45) / max(0.25, _DetailFadeDist - 0.45));
                half3 nCombinedTS = nMacroTS;
                float2 poreUv = input.uv * _DetailTiling;
                float2 poreDx = ddx(poreUv);
                float2 poreDy = ddy(poreUv);
                UNITY_BRANCH if (poreFade > 0.0h)
                {
                    half3 detail = UnpackNormalScale(
                        SAMPLE_TEXTURE2D_GRAD(_DetailNormalMap, sampler_DetailNormalMap, poreUv, poreDx, poreDy),
                        _DetailScale * poreFade);
                    half3 t = nMacroTS + half3(0.0h, 0.0h, 1.0h);
                    half3 u = detail * half3(-1.0h, -1.0h, 1.0h);
                    nCombinedTS = normalize(t * dot(t, u) - u * t.z);
                }

                half3 N_macro = normalize(mul(nMacroTS, TBN));
                half3 N = normalize(mul(nCombinedTS, TBN));
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

                half ssao = 1.0h;
                #if defined(_SCREEN_SPACE_OCCLUSION)
                    AmbientOcclusionFactor aoFactor = GetScreenSpaceAmbientOcclusion(inputData.normalizedScreenSpaceUV);
                    ssao = aoFactor.indirectAmbientOcclusion;
                #endif
                half finalAO = ao * ssao;

                half4 shadowMask = half4(1, 1, 1, 1);
                Light mainLight = GetMainLight(inputData.shadowCoord, input.positionWS, shadowMask);

                half3 color = EvaluateSkinLight(mainLight, albedo, N, N_macro, V, roughness, thickness, ao);

                #if defined(_ADDITIONAL_LIGHTS)
                    uint pixelLightCount = GetAdditionalLightsCount();
                    #if USE_CLUSTER_LIGHT_LOOP
                    [loop] for (uint lightIndex = 0; lightIndex < min(URP_FP_DIRECTIONAL_LIGHTS_COUNT, MAX_VISIBLE_LIGHTS); lightIndex++)
                    {
                        CLUSTER_LIGHT_LOOP_SUBTRACTIVE_LIGHT_CHECK
                        Light addLight = GetAdditionalLight(lightIndex, input.positionWS, shadowMask);
                        color += EvaluateSkinLight(addLight, albedo, N, N_macro, V, roughness, thickness, ao);
                    }
                    #endif
                    LIGHT_LOOP_BEGIN(pixelLightCount)
                        Light addLight = GetAdditionalLight(lightIndex, input.positionWS, shadowMask);
                        color += EvaluateSkinLight(addLight, albedo, N, N_macro, V, roughness, thickness, ao);
                    LIGHT_LOOP_END
                #endif

                half3 bakedGI = SampleSHPixel(input.vertexSH, N_macro);
                #if USE_CLUSTER_LIGHT_LOOP
                    half3 envSpec = GlossyEnvironmentReflection(reflect(-V, N), input.positionWS, roughness, finalAO, inputData.normalizedScreenSpaceUV);
                #else
                    half3 envSpec = GlossyEnvironmentReflection(reflect(-V, N), roughness, finalAO);
                #endif
                half NdotV = saturate(dot(N, V));
                half envFresnel = 0.028h + (0.28h * smoothness) * pow(1.0h - NdotV, 4.0h);

                color += albedo * bakedGI * finalAO + envSpec * envFresnel * _SpecIntensity * 0.65h;
                color = MixFog(color, input.fogFactor);
                return half4(color, 1.0h);
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
            Cull Back

            HLSLPROGRAM
            #pragma target 2.0
            #pragma vertex VertShadow
            #pragma fragment FragShadow
            #pragma multi_compile_vertex _ _CASTING_PUNCTUAL_LIGHT_SHADOW
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Shadows.hlsl"

            float3 _LightDirection;
            float3 _LightPosition;

            struct Attributes { float4 positionOS : POSITION; float3 normalOS : NORMAL; };
            struct Varyings { float4 positionCS : SV_POSITION; };

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
                return o;
            }
            half4 FragShadow(Varyings input) : SV_Target { return 0; }
            ENDHLSL
        }

        Pass
        {
            Name "DepthOnly"
            Tags { "LightMode" = "DepthOnly" }
            ZWrite On
            ColorMask R
            Cull Back

            HLSLPROGRAM
            #pragma target 2.0
            #pragma vertex VertDepth
            #pragma fragment FragDepth

            struct Attributes { float4 positionOS : POSITION; };
            struct Varyings { float4 positionCS : SV_POSITION; };

            Varyings VertDepth(Attributes input)
            {
                Varyings o;
                o.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                return o;
            }
            half4 FragDepth(Varyings input) : SV_Target { return 0; }
            ENDHLSL
        }

        Pass
        {
            Name "DepthNormals"
            Tags { "LightMode" = "DepthNormals" }
            ZWrite On
            Cull Back

            HLSLPROGRAM
            #pragma target 2.0
            #pragma vertex VertDN
            #pragma fragment FragDN

            struct Attributes { float4 positionOS : POSITION; float3 normalOS : NORMAL; };
            struct Varyings { float4 positionCS : SV_POSITION; float3 normalWS : TEXCOORD0; };

            Varyings VertDN(Attributes input)
            {
                Varyings o;
                o.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                o.normalWS = TransformObjectToWorldNormal(input.normalOS);
                return o;
            }
            half4 FragDN(Varyings input) : SV_Target
            {
                return half4(NormalizeNormalPerPixel(input.normalWS), 0.0h);
            }
            ENDHLSL
        }
    }
}
