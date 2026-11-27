Shader "CTCC/ReceptionGlazing"
{
    Properties
    {
        _Tint("Transmission tint", Color) = (0.28, 0.39, 0.35, 0.05)
        _EdgeSoil("Local cleaning residue", Range(0,1)) = 0.15
        _BroadGlint("Soft reflected light", Range(0,1)) = 0.24
        _ReflectionStrength("Probe reflection strength", Range(0,2)) = 0.78
        _DustRoughness("Dust roughness", Range(0,1)) = 0.35
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
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            CBUFFER_START(UnityPerMaterial)
                half4 _Tint;
                half _EdgeSoil;
                half _BroadGlint;
                half _ReflectionStrength;
                half _DustRoughness;
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

            float Hash21(float2 p)
            {
                p = frac(p * float2(123.34, 456.21));
                p += dot(p, p + 45.32);
                return frac(p.x * p.y);
            }

            float Noise2(float2 p)
            {
                float2 i = floor(p);
                float2 f = frac(p);
                f = f * f * (3.0 - 2.0 * f);
                float a = Hash21(i);
                float b = Hash21(i + float2(1, 0));
                float c = Hash21(i + float2(0, 1));
                float d = Hash21(i + float2(1, 1));
                return lerp(lerp(a, b, f.x), lerp(c, d, f.x), f.y);
            }

            float Fbm(float2 p)
            {
                float v = 0.0;
                float a = 0.5;
                for (int k = 0; k < 3; k++)
                {
                    v += a * Noise2(p);
                    p = p * 2.07 + float2(13.1, 7.7);
                    a *= 0.5;
                }
                return v;
            }

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
                half3 N = normalize(input.normalWS);
                // Ensure normal faces viewer so both sides of thin pane reflect physically
                if (dot(N, V) < 0.0) N = -N;

                float2 pane = input.positionWS.zy; // pane.x = world Z (-4.5..-2.0), pane.y = world Y (1.18..2.22)
                // Micro-wavy vintage 1990s Vietnamese float glass normal ripple
                float wave1 = sin(pane.x * 11.0 + pane.y * 4.5) * 0.008;
                float wave2 = (Fbm(pane * 4.5) - 0.5) * 0.022;
                half3 Nw = normalize(N + half3(0.0, wave1 + wave2, wave2 * 0.8));

                half ndotv = saturate(dot(Nw, V));
                half fresnel = pow(1.0 - ndotv, 3.6);
                half schlick = 0.04 + 0.96 * fresnel;

                // Janitorial circular wipe arcs near the service opening + organic monsoon dust/condensation film
                float2 wipeCenter = (pane - float2(-3.24, 1.48)) * float2(0.75, 1.45);
                float wipe = 0.5 + 0.5 * sin(length(wipeCenter) * 72.0 + 0.45 * sin(pane.x * 24.0));
                float fineDust = Fbm(pane * 6.5);
                float dripNoise = Fbm(float2(pane.x * 26.0, pane.y * 2.4));

                // Edge grime hugging mullions (z = -4.48, -3.66, -2.82, -2.00) and sill/header (y = 1.18, 1.40, 2.22)
                float sillSoil = exp(-(pane.y - 1.38) * (pane.y - 1.38) * 18.0);
                float topSoil = exp(-(pane.y - 2.20) * (pane.y - 2.20) * 32.0);
                float mullionDist = min(min(abs(pane.x + 3.66), abs(pane.x + 2.82)), min(abs(pane.x + 4.46), abs(pane.x + 2.02)));
                float mullionSoil = exp(-mullionDist * 14.0);
                float streaks = smoothstep(0.58, 0.86, dripNoise) * 0.42;

                half deposits = saturate(0.48 * sillSoil + 0.34 * topSoil + 0.44 * mullionSoil +
                                         0.26 * fineDust + 0.16 * wipe * sillSoil + streaks * _EdgeSoil);

                // Sample local LOC02 Reflection Probe along reflected view vector!
                half3 R = reflect(-V, Nw);
                half mip = saturate(0.06 + deposits * _DustRoughness * 0.72) * 5.0;
                half4 encodedIrradiance = SAMPLE_TEXTURECUBE_LOD(unity_SpecCube0, samplerunity_SpecCube0, R, mip);
                half3 envRefl = DecodeHDREnvironment(encodedIrradiance, unity_SpecCube0_HDR);

                // Soft overhead fluorescent / desk lamp specular highlight catch across the glass plane
                half3 L_overhead = normalize(half3(0.45, 0.82, -0.35));
                half3 H = normalize(L_overhead + V);
                half specSharp = pow(saturate(dot(Nw, H)), lerp(96.0, 20.0, deposits * _EdgeSoil));
                half3 glintCol = half3(0.84, 0.90, 0.85) * (_BroadGlint * (0.14 + specSharp * 0.65));

                // Dusty backscatter film in R vs clean soda-lime glass tint in B
                half3 dustColor = half3(0.24, 0.28, 0.26) * (0.65 + 0.55 * fineDust);
                half3 glassColor = _Tint.rgb + envRefl * (_ReflectionStrength * (0.16 + schlick * 0.95)) + glintCol;
                half3 finalRgb = lerp(glassColor, dustColor + envRefl * 0.18, saturate(_EdgeSoil * deposits * 0.55));

                half opacity = saturate(_Tint.a + schlick * 0.26 + _EdgeSoil * deposits * 0.44 + specSharp * 0.10);
                return half4(finalRgb, opacity);
            }
            ENDHLSL
        }
    }
}
