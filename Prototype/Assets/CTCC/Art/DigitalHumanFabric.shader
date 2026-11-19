Shader "CTCC/DigitalHumanFabric"
{
    Properties
    {
        _BaseMap("Fabric Albedo (sRGB)", 2D) = "white" {}
        _BaseColor("Tint", Color) = (1, 1, 1, 1)
        _BumpMap("Cloth Fold & Weave Normal", 2D) = "bump" {}
        _BumpScale("Normal Scale", Range(0.0, 2.0)) = 0.88
        _MaskMap("Mask (G=AO, A=Smoothness)", 2D) = "white" {}
        _SmoothnessScale("Smoothness Scale", Range(0.05, 1.0)) = 0.65
        _OcclusionStrength("AO Strength", Range(0.0, 1.0)) = 0.80
        _SheenColor("Microfiber Grazing Sheen Color", Color) = (0.92, 0.93, 0.90, 1.0)
        _SheenIntensity("Sheen Intensity", Range(0.0, 1.0)) = 0.26
        _SheenRoughness("Sheen Roughness", Range(0.15, 0.95)) = 0.58
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
            half _SmoothnessScale;
            half _OcclusionStrength;
            half4 _SheenColor;
            half _SheenIntensity;
            half _SheenRoughness;
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

            half CharlieSheen(half NdotH, half roughness)
            {
                half invR = 1.0h / max(roughness, 0.08h);
                half sin2h = max(1.0h - NdotH * NdotH, 0.0078125h);
                return (2.0h + invR) * pow(sin2h, invR * 0.5h) / 6.2831853h;
            }

            half3 EvaluateClothLight(Light light, half3 albedo, half3 N, half3 V, half roughness, half ao)
            {
                half atten = light.distanceAttenuation * light.shadowAttenuation;
                if (atten <= 0.0001h)
                    return 0.0h;

                half3 L = light.direction;
                half3 radiance = light.color * atten;
                half NdotL = saturate(dot(N, L));
                half NdotV = saturate(dot(N, V));

                half wrapDiff = saturate((dot(N, L) + 0.14h) / 1.14h);
                half3 H = normalize(L + V);
                half NdotH = saturate(dot(N, H));

                half sheen = CharlieSheen(NdotH, _SheenRoughness) * (0.18h + 0.82h * pow(1.0h - NdotV, 2.2h)) * _SheenIntensity * NdotL;
                half a2 = max(roughness * roughness, 0.25h);
                half d = NdotH * NdotH * (a2 - 1.0h) + 1.0h;
                half spec = (a2 / (3.14159h * d * d + 1e-4h)) * 0.032h * NdotL;

                return (albedo * wrapDiff * ao + _SheenColor.rgb * sheen * ao + spec.xxx) * radiance;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                half4 baseSample = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, input.uv);
                half3 albedo = baseSample.rgb * _BaseColor.rgb;
                half4 mask = SAMPLE_TEXTURE2D(_MaskMap, sampler_MaskMap, input.uv);
                half ao = lerp(1.0h, mask.g, _OcclusionStrength);
                half smoothness = saturate(mask.a * _SmoothnessScale);
                half roughness = max(0.45h, 1.0h - smoothness);

                float3 N_geom = normalize(input.normalWS);
                float3 T_ws = normalize(input.tangentWS.xyz);
                float3 B_ws = normalize(cross(N_geom, T_ws) * input.tangentWS.w);
                half3 nTS = UnpackNormalScale(SAMPLE_TEXTURE2D(_BumpMap, sampler_BumpMap, input.uv), _BumpScale);
                half3 N = normalize(mul(nTS, float3x3(T_ws, B_ws, N_geom)));
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
                    ssao = GetScreenSpaceAmbientOcclusion(inputData.normalizedScreenSpaceUV).indirectAmbientOcclusion;
                #endif
                half finalAO = ao * ssao;

                half4 shadowMask = half4(1, 1, 1, 1);
                Light mainLight = GetMainLight(inputData.shadowCoord, input.positionWS, shadowMask);
                half3 color = EvaluateClothLight(mainLight, albedo, N, V, roughness, ao);

                #if defined(_ADDITIONAL_LIGHTS)
                    uint pixelLightCount = GetAdditionalLightsCount();
                    #if USE_CLUSTER_LIGHT_LOOP
                    [loop] for (uint lightIndex = 0; lightIndex < min(URP_FP_DIRECTIONAL_LIGHTS_COUNT, MAX_VISIBLE_LIGHTS); lightIndex++)
                    {
                        CLUSTER_LIGHT_LOOP_SUBTRACTIVE_LIGHT_CHECK
                        color += EvaluateClothLight(GetAdditionalLight(lightIndex, input.positionWS, shadowMask), albedo, N, V, roughness, ao);
                    }
                    #endif
                    LIGHT_LOOP_BEGIN(pixelLightCount)
                        color += EvaluateClothLight(GetAdditionalLight(lightIndex, input.positionWS, shadowMask), albedo, N, V, roughness, ao);
                    LIGHT_LOOP_END
                #endif

                half3 gi = SampleSHPixel(input.vertexSH, N);
                half rimSheen = pow(1.0h - saturate(dot(N, V)), 3.2h) * _SheenIntensity * 0.35h;
                color += (albedo + _SheenColor.rgb * rimSheen) * gi * finalAO;
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
