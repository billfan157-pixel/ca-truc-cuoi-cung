Shader "CTCC/TropicalFoliage"
{
    Properties
    {
        _BaseMap("Leaf Atlas RGBA", 2D) = "white" {}
        _BaseColor("Tint", Color) = (0.76, 0.86, 0.72, 1.0)
        _Cutoff("Alpha Cutoff", Range(0.1, 0.9)) = 0.42
        _WindSpeed("Monsoon Wind Speed", Float) = 1.65
        _WindAmp("Wind Sway Amplitude", Float) = 0.065
        _Wetness("Rain Sheen", Range(0, 1)) = 0.55
    }
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "RenderType"="TransparentCutout" "Queue"="AlphaTest" }
        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode"="UniversalForward" }
            Cull Off

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma multi_compile_fog
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            TEXTURE2D(_BaseMap);
            SAMPLER(sampler_BaseMap);

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseMap_ST;
                half4 _BaseColor;
                half _Cutoff;
                half _WindSpeed;
                half _WindAmp;
                half _Wetness;
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
                float3 positionWS : TEXCOORD1;
                float3 normalWS : TEXCOORD2;
                float fogFactor : TEXCOORD3;
            };

            Varyings Vert(Attributes input)
            {
                Varyings output;
                float3 posWS = TransformObjectToWorld(input.positionOS);
                // Organic monsoon wind flutter stronger toward leaf tips (uv.y) and higher canopy
                float swayPhase = _Time.y * _WindSpeed + posWS.x * 1.15 + posWS.z * 0.92;
                float tipMask = saturate(input.uv.y * 1.15) * saturate((posWS.y + 0.5) * 0.25);
                posWS.x += sin(swayPhase) * _WindAmp * tipMask;
                posWS.z += cos(swayPhase * 1.37) * (_WindAmp * 0.65) * tipMask;
                posWS.y += sin(swayPhase * 2.1) * (_WindAmp * 0.22) * tipMask;

                output.positionWS = posWS;
                output.positionCS = TransformWorldToHClip(posWS);
                output.normalWS = TransformObjectToWorldNormal(input.normalOS);
                output.uv = TRANSFORM_TEX(input.uv, _BaseMap);
                output.fogFactor = ComputeFogFactor(output.positionCS.z);
                return output;
            }

            half4 Frag(Varyings input, bool isFrontFace : SV_IsFrontFace) : SV_Target
            {
                half4 tex = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, input.uv);
                clip(tex.a - _Cutoff);

                float3 N = normalize(isFrontFace ? input.normalWS : -input.normalWS);
                float3 V = normalize(_WorldSpaceCameraPos - input.positionWS);
                Light mainLight = GetMainLight();
                float3 L = normalize(mainLight.direction);

                float ndl = saturate(dot(N, L) * 0.65 + 0.35);
                // Subtle translucent backlighting through wet tropical leaves
                float backLight = pow(saturate(dot(V, -L)), 2.5) * 0.38;
                // Wet monsoon specular rim
                float3 H = normalize(L + V);
                float spec = pow(saturate(dot(N, H)), 24.0) * _Wetness * 0.45;

                half3 ambient = SampleSH(N) * 0.85;
                half3 lit = tex.rgb * _BaseColor.rgb * (ambient + mainLight.color * (ndl + backLight)) + mainLight.color * spec;
                lit = MixFog(lit, input.fogFactor);
                return half4(lit, 1.0);
            }
            ENDHLSL
        }

        Pass
        {
            Name "ShadowCaster"
            Tags { "LightMode"="ShadowCaster" }
            Cull Off
            ZWrite On
            ColorMask 0

            HLSLPROGRAM
            #pragma vertex VertShadow
            #pragma fragment FragShadow
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Shadows.hlsl"

            TEXTURE2D(_BaseMap);
            SAMPLER(sampler_BaseMap);

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseMap_ST;
                half4 _BaseColor;
                half _Cutoff;
                half _WindSpeed;
                half _WindAmp;
                half _Wetness;
            CBUFFER_END

            struct Attributes { float3 positionOS : POSITION; float3 normalOS : NORMAL; float2 uv : TEXCOORD0; };
            struct Varyings { float4 positionCS : SV_POSITION; float2 uv : TEXCOORD0; };

            Varyings VertShadow(Attributes input)
            {
                Varyings o;
                float3 posWS = TransformObjectToWorld(input.positionOS);
                float3 normWS = TransformObjectToWorldNormal(input.normalOS);
                o.positionCS = TransformWorldToHClip(ApplyShadowBias(posWS, normWS, _MainLightPosition.xyz));
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
    }
}
