Shader "CTCC/RainWindowGlass"
{
    Properties
    {
        _BaseColor("Glass Tint", Color) = (0.16, 0.24, 0.25, 0.14)
        _RainIntensity("Rain Intensity", Range(0, 1)) = 0.85
        _GrimeAmount("Edge Condensation / Grime", Range(0, 1)) = 0.42
        _StreakSpeed("Rain Speed", Float) = 0.24
        _SpecularColor("Moonlight Specular", Color) = (0.58, 0.76, 0.88, 1.0)
    }
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "RenderType"="Transparent" "Queue"="Transparent+2" }
        Pass
        {
            Name "ForwardLit"
            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            Cull Off

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                half4 _BaseColor;
                half4 _SpecularColor;
                half _RainIntensity;
                half _GrimeAmount;
                half _StreakSpeed;
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
                float3 normalWS : TEXCOORD1;
                float2 uv : TEXCOORD2;
            };

            float Hash21(float2 p)
            {
                p = frac(p * float2(123.34, 456.21));
                p += dot(p, p + 45.32);
                return frac(p.x * p.y);
            }

            float RainStreakLayer(float2 uv, float t, float2 gridScale)
            {
                float2 st = uv * gridScale;
                float col = floor(st.x);
                float colShift = Hash21(float2(col, 3.7));
                // Only ~42% of columns carry an active rain rivulet at any moment
                float active = step(0.58, Hash21(float2(col, 11.3)));
                st.y += t * (0.65 + 0.75 * colShift) + colShift * 12.0;
                float2 id = floor(st);
                float2 gv = frac(st) - 0.5;
                float n = Hash21(id);
                float x = (n - 0.5) * 0.58;
                x += sin(uv.y * 28.0 + n * 24.0) * 0.055;
                float y = -0.34 + frac(t * (0.25 + 0.35 * n) + n) * 0.68;
                // Elongated micro-droplet head (~3-4 mm real scale) + thin specular water rivulet trail
                float2 dHead = (gv - float2(x, -y)) * float2(1.0, 2.8);
                float head = smoothstep(0.11, 0.02, length(dHead));
                float trail = smoothstep(0.042, 0.006, abs(gv.x - x)) *
                              smoothstep(-y, 0.48, gv.y) * smoothstep(0.50, -y + 0.04, gv.y);
                return active * saturate(head * 0.90 + trail * 0.52);
            }

            Varyings Vert(Attributes input)
            {
                Varyings output;
                VertexPositionInputs pos = GetVertexPositionInputs(input.positionOS);
                output.positionCS = pos.positionCS;
                output.positionWS = pos.positionWS;
                output.normalWS = TransformObjectToWorldNormal(input.normalOS);
                output.uv = input.uv;
                return output;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                float3 viewDir = normalize(_WorldSpaceCameraPos - input.positionWS);
                float3 normalWS = normalize(input.normalWS);
                float fresnel = pow(1.0 - saturate(abs(dot(normalWS, viewDir))), 3.6);

                // Fine physically scaled monsoon rain rivulets (grid cells ~3.5 cm wide x 18 cm tall!)
                float2 wUV = float2(input.positionWS.x, input.positionWS.y);
                float t = _Time.y * _StreakSpeed;

                float r1 = RainStreakLayer(wUV, t, float2(26.0, 5.2));
                float r2 = RainStreakLayer(wUV + float2(0.17, 0.09), t * 0.78, float2(44.0, 9.5));
                float rain = saturate(r1 * 0.78 + r2 * 0.48) * _RainIntensity;

                // Subtle grime & moisture condensation around sash corners
                float edgeX = abs(input.uv.x - 0.5) * 2.0;
                float edgeY = abs(input.uv.y - 0.5) * 2.0;
                float edgeMask = saturate(pow(max(edgeX, edgeY), 2.8)) * _GrimeAmount;

                half3 glassCol = _BaseColor.rgb * (0.75 + edgeMask * 0.45);
                half3 glint = _SpecularColor.rgb * (rain * 0.52 + fresnel * 0.30);
                half alpha = saturate(_BaseColor.a + fresnel * 0.18 + edgeMask * 0.22 + rain * 0.15);
                return half4(glassCol + glint, alpha);
            }
            ENDHLSL
        }
    }
}
