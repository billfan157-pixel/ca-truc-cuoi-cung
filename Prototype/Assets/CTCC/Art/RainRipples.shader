Shader "CTCC/RainRipples"
{
    Properties
    {
        _Glint("Water reflection tint", Color) = (0.53, 0.63, 0.68, 0.20)
        _GlancingLight("Direction of reflected practical", Vector) = (0.26, 0.91, 0.31, 0)
        _PatchSeed("Patch variation", Float) = 0
    }
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "RenderType"="Transparent" "Queue"="Transparent+1" }
        Pass
        {
            Name "Shallow Pool"
            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            Cull Back

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                half4 _Glint;
                float4 _GlancingLight;
                float _PatchSeed;
            CBUFFER_END
            float _CTCCReduceWeatherMotion;

            struct Attributes
            {
                float3 positionOS : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
                float2 uv : TEXCOORD1;
            };

            Varyings Vert(Attributes input)
            {
                Varyings output;
                output.positionWS = TransformObjectToWorld(input.positionOS);
                output.positionCS = TransformWorldToHClip(output.positionWS);
                output.uv = input.uv;
                return output;
            }

            float Hash(float2 v)
            {
                v = frac(v * float2(0.1031, 0.11369));
                v += dot(v, v.yx + 19.19);
                return frac((v.x + v.y) * v.x);
            }

            half4 Frag(Varyings input) : SV_Target
            {
                float2 local = input.uv * 2 - 1;
                float2 world = input.positionWS.xz;
                // Leave transparent gaps and vary shore shape along a single depth depression.
                float shore = dot(local, local) + 0.16 * sin(local.x * 7 - local.y * 4)
                    + 0.085 * cos(local.y * 11 + _PatchSeed);
                float puddle = 1 - smoothstep(0.48, 1, shore);
                clip(puddle - 0.008);

                float moving = 1 - saturate(_CTCCReduceWeatherMotion);
                float2 grid = world * 6.5;
                float2 cell = floor(grid);
                float seed = Hash(cell + _PatchSeed);
                float2 origin = 0.28 + 0.44 * float2(seed, Hash(cell + 19.3));
                float age = frac(_Time.y * lerp(0.62, 1.33, seed) * moving + seed);
                float radius = length(frac(grid) - origin);
                float wave = 1 - smoothstep(0.008, max(0.026, fwidth(radius) * 1.2),
                    abs(radius - age * 0.26));
                float rings = wave * (1 - age) * moving;
                float3 view = normalize(_WorldSpaceCameraPos - input.positionWS);
                float grazing = pow(1 - saturate(view.y), 3);
                float facing = pow(saturate(dot(reflect(-view, float3(0, 1, 0)), normalize(_GlancingLight.xyz))), 6);
                float sheen = 0.12 + 0.31 * grazing + 0.16 * facing;
                return half4(_Glint.rgb, _Glint.a * puddle * (sheen + 0.78 * rings));
            }
            ENDHLSL
        }
    }
}
