Shader "CTCC/DigitalHumanEye"
{
    Properties
    {
        _BaseMap("Eye Iris & Sclera Atlas (sRGB)", 2D) = "white" {}
        _BaseColor("Tint", Color) = (1, 1, 1, 1)
        _BumpMap("Iris Fiber Normal", 2D) = "bump" {}
        _BumpScale("Iris Normal Scale", Range(0.0, 2.0)) = 0.75
        _IrisRadius("Iris UV Radius", Range(0.05, 0.25)) = 0.122
        _RefractionDepth("Cornea IOR 1.376 Parallax Depth", Range(0.0, 0.04)) = 0.015
        _CausticTint("Anterior Chamber Caustic Tint", Color) = (0.62, 0.34, 0.16, 1.0)
        _CausticStrength("Iris Caustic Strength", Range(0.0, 2.5)) = 1.15
        _CorneaRoughness("Cornea Wet Roughness", Range(0.01, 0.25)) = 0.028
        _CorneaSpecIntensity("Cornea Catchlight Intensity", Range(0.2, 3.0)) = 1.45
        _LimbalDarkening("Limbal Ring Depth", Range(0.0, 1.0)) = 0.38
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
            half _IrisRadius;
            half _RefractionDepth;
            half4 _CausticTint;
            half _CausticStrength;
            half _CorneaRoughness;
            half _CorneaSpecIntensity;
            half _LimbalDarkening;
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
            #pragma multi_compile _ _CLUSTER_LIGHT_LOOP
            #pragma multi_compile_fog

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            TEXTURE2D(_BaseMap); SAMPLER(sampler_BaseMap);
            TEXTURE2D(_BumpMap); SAMPLER(sampler_BumpMap);

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

            half GGXEye(half NdotH, half roughness)
            {
                half a = max(roughness * roughness, 0.0006h);
                half a2 = a * a;
                half d = NdotH * NdotH * (a2 - 1.0h) + 1.0h;
                return a2 / (3.14159265h * d * d + 1e-6h);
            }

            half3 EvaluateEyeLight(Light light, half3 albedo, half3 N_cornea, half3 N_iris, half3 V,
                                   half irisMask, float2 irisRadialDir, float3 T_ws, float3 B_ws)
            {
                half atten = light.distanceAttenuation * light.shadowAttenuation;
                if (atten <= 0.0001h)
                    return 0.0h;

                half3 L = light.direction;
                half3 radiance = light.color * atten;

                // Soft wrapped scleral diffuse (moist white tissue scatters subtly)
                half scleraDiff = saturate((dot(N_cornea, L) + 0.18h) / 1.18h);
                // Concave iris stroma lighting + anterior-chamber refracted caustic opposite the light!
                half irisDiff = saturate(dot(N_iris, L) * 0.78h + 0.22h);
                float2 lightTangent2D = float2(dot(L, T_ws), dot(L, B_ws));
                half causticAlign = saturate(dot(-normalize(lightTangent2D + float2(1e-4, 0.0)), irisRadialDir));
                half caustic = pow(causticAlign, 1.8h) * irisMask * _CausticStrength * saturate(dot(N_cornea, L) + 0.35h);

                half3 diffColor = albedo * lerp(scleraDiff, irisDiff, irisMask) + _CausticTint.rgb * albedo * caustic;

                // Razor-crisp wet cornea catchlight (IOR 1.376 -> F0 = 0.025)
                half3 H = normalize(L + V);
                half NdotH = saturate(dot(N_cornea, H));
                half VdotH = saturate(dot(V, H));
                half fresnel = 0.025h + 0.975h * pow(1.0h - VdotH, 5.0h);
                half corneaR = lerp(_CorneaRoughness * 2.2h, _CorneaRoughness, irisMask);
                half spec = GGXEye(NdotH, corneaR) * fresnel * 0.25h * _CorneaSpecIntensity * saturate(dot(N_cornea, L) * 5.0h);

                return (diffColor + spec.xxx) * radiance;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                float3 N_cornea = normalize(input.normalWS);
                float3 T_ws = normalize(input.tangentWS.xyz);
                float3 B_ws = normalize(cross(N_cornea, T_ws) * input.tangentWS.w);
                float3x3 TBN = float3x3(T_ws, B_ws, N_cornea);
                float3 V = normalize(_WorldSpaceCameraPos - input.positionWS);

                // Dual-eye MakeHuman high-poly UV layout: Left iris (0.704, 0.704), Right iris (0.290, 0.292)
                float2 eyeCenter = (input.uv.x + input.uv.y > 1.0) ? float2(0.704, 0.704) : float2(0.290, 0.292);
                float2 rel = input.uv - eyeCenter;
                float distUV = length(rel);
                half irisMask = 1.0h - smoothstep(_IrisRadius * 0.86h, _IrisRadius * 1.06h, (half)distUV);
                half limbalRing = smoothstep(_IrisRadius * 0.76h, _IrisRadius * 0.96h, (half)distUV) * irisMask;

                // Cornea IOR 1.376 parallax refraction offset inside the iris disc!
                float3 V_ts = normalize(float3(dot(V, T_ws), dot(V, B_ws), max(dot(V, N_cornea), 0.15)));
                float domeHeight = saturate(1.0 - (distUV / max(0.02, (float)_IrisRadius)));
                float2 refractUV = input.uv - (V_ts.xy / (V_ts.z + 0.45)) * (_RefractionDepth * domeHeight * irisMask);

                half3 albedo = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, refractUV).rgb * _BaseColor.rgb;
                albedo *= (1.0h - limbalRing * _LimbalDarkening);

                // Subtle upper-eyelid ambient shadow grounding the top of the eyeball in the socket
                half upperLidShadow = lerp(1.0h, 0.74h, smoothstep(0.02h, 0.15h, (half)rel.y));
                albedo *= upperLidShadow;

                half3 nIrisTS = UnpackNormalScale(SAMPLE_TEXTURE2D(_BumpMap, sampler_BumpMap, refractUV), _BumpScale * irisMask);
                half3 N_iris = normalize(mul(nIrisTS, TBN));
                float2 irisRadialDir = distUV > 1e-4 ? (rel / distUV) : float2(0.0, 1.0);

                InputData inputData = (InputData)0;
                inputData.positionWS = input.positionWS;
                inputData.normalWS = N_cornea;
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
                half3 color = EvaluateEyeLight(mainLight, albedo, N_cornea, N_iris, V, irisMask, irisRadialDir, T_ws, B_ws);

                #if defined(_ADDITIONAL_LIGHTS)
                    uint pixelLightCount = GetAdditionalLightsCount();
                    #if USE_CLUSTER_LIGHT_LOOP
                    [loop] for (uint lightIndex = 0; lightIndex < min(URP_FP_DIRECTIONAL_LIGHTS_COUNT, MAX_VISIBLE_LIGHTS); lightIndex++)
                    {
                        CLUSTER_LIGHT_LOOP_SUBTRACTIVE_LIGHT_CHECK
                        color += EvaluateEyeLight(GetAdditionalLight(lightIndex, input.positionWS, shadowMask), albedo, N_cornea, N_iris, V, irisMask, irisRadialDir, T_ws, B_ws);
                    }
                    #endif
                    LIGHT_LOOP_BEGIN(pixelLightCount)
                        color += EvaluateEyeLight(GetAdditionalLight(lightIndex, input.positionWS, shadowMask), albedo, N_cornea, N_iris, V, irisMask, irisRadialDir, T_ws, B_ws);
                    LIGHT_LOOP_END
                #endif

                // Indirect GI + crisp wet corneal environment reflection catchlight!
                half3 gi = SampleSHPixel(input.vertexSH, N_cornea);
                #if USE_CLUSTER_LIGHT_LOOP
                    half3 envRefl = GlossyEnvironmentReflection(reflect(-V, N_cornea), input.positionWS, _CorneaRoughness * 1.4h, 1.0h, inputData.normalizedScreenSpaceUV);
                #else
                    half3 envRefl = GlossyEnvironmentReflection(reflect(-V, N_cornea), _CorneaRoughness * 1.4h, 1.0h);
                #endif
                half envF = 0.025h + 0.55h * pow(1.0h - saturate(dot(N_cornea, V)), 4.0h);
                color += albedo * gi * 0.92h + envRefl * envF * 0.85h;

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
            half4 FragDN(Varyings input) : SV_Target { return half4(NormalizeNormalPerPixel(input.normalWS), 0.0h); }
            ENDHLSL
        }
    }
}
