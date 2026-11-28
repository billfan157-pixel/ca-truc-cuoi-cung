Shader "CTCC/RainRipples"
{
    Properties
    {
        _Glint("Water reflection tint", Color) = (0.58, 0.68, 0.74, 0.62)
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
            #pragma multi_compile_fog
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

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
                float fogFactor : TEXCOORD2;
            };

            Varyings Vert(Attributes input)
            {
                Varyings output;
                output.positionWS = TransformObjectToWorld(input.positionOS);
                output.positionCS = TransformWorldToHClip(output.positionWS);
                output.uv = input.uv;
                output.fogFactor = ComputeFogFactor(output.positionCS.z);
                return output;
            }

            float Hash21(float2 v)
            {
                v = frac(v * float2(0.1031, 0.11369));
                v += dot(v, v.yx + 19.19);
                return frac((v.x + v.y) * v.x);
            }

            float ValueNoise(float2 p)
            {
                float2 i = floor(p);
                float2 f = frac(p);
                f = f * f * (3.0 - 2.0 * f);
                float a = Hash21(i);
                float b = Hash21(i + float2(1.0, 0.0));
                float c = Hash21(i + float2(0.0, 1.0));
                float d = Hash21(i + float2(1.0, 1.0));
                return lerp(lerp(a, b, f.x), lerp(c, d, f.x), f.y);
            }

            float Fbm(float2 p)
            {
                return ValueNoise(p) * 0.58 + ValueNoise(p * 2.13 + 11.7) * 0.28 + ValueNoise(p * 4.37 - 5.2) * 0.14;
            }

            // Physically scaled raindrop ring ripple normal perturbation (2 offset layers so zero visible grid)
            float2 RippleLayer(float2 worldXZ, float t, float scale, float seedOff, out float ringHighlight)
            {
                float2 g = worldXZ * scale;
                float2 cell = floor(g);
                float2 f = frac(g);
                float s = Hash21(cell + seedOff);
                float2 origin = 0.22 + 0.56 * float2(s, Hash21(cell + seedOff + 27.4));
                float age = frac(t * lerp(0.58, 1.18, s) + s);
                float2 delta = f - origin;
                float dist = length(delta) + 1e-4;
                float waveRadius = age * 0.34;
                float band = abs(dist - waveRadius);
                float fade = (1.0 - age) * (1.0 - age) * smoothstep(0.0, 0.06, age);
                // Crisp narrow ripple crest + secondary trailing harmonic
                float crest = exp(-band * band * 2600.0) * fade;
                float trail = exp(-pow(abs(dist - max(0.0, waveRadius - 0.055)), 2.0) * 1800.0) * fade * 0.45;
                float slope = (crest - trail) * sin((dist - waveRadius) * 95.0);
                ringHighlight = (crest + trail * 0.6) * smoothstep(0.38, 0.08, dist);
                return (delta / dist) * slope;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                float2 local = input.uv * 2.0 - 1.0;
                float2 world = input.positionWS.xz;

                // Organic multi-octave FBM puddle basin mask (feathered damp shore -> deep mirror center)
                float nShore = Fbm(world * 1.45 + _PatchSeed * 3.1) - 0.5;
                float dist2 = dot(local, local) + nShore * 0.52;
                float dampRim = 1.0 - smoothstep(0.45, 0.98, dist2);
                float waterBody = 1.0 - smoothstep(0.28, 0.78, dist2);
                clip(dampRim - 0.01);

                float moving = 1.0 - saturate(_CTCCReduceWeatherMotion);
                float t = _Time.y * moving;

                float hi1, hi2;
                float2 n1 = RippleLayer(world, t, 4.8, _PatchSeed, hi1);
                float2 n2 = RippleLayer(world + float2(0.37, 0.61), t * 0.87, 7.2, _PatchSeed + 41.7, hi2);
                float2 rippleGrad = (n1 + n2 * 0.72) * 0.24 * waterBody * moving;
                float rings = saturate(hi1 * 0.75 + hi2 * 0.55) * waterBody * moving;

                float3 N = normalize(float3(rippleGrad.x, 1.0, rippleGrad.y));
                float3 V = normalize(_WorldSpaceCameraPos - input.positionWS);
                float3 R = reflect(-V, N);

                // Real URP ReflectionProbe mirror reflection (reflects gate lanterns, canopy glow & K-A facade!)
                half3 envRefl = GlossyEnvironmentReflection(R, input.positionWS, 0.06h, 1.0h);

                // Grazing Fresnel (Schlick water F0 = 0.02 -> dark deep water steeply, crisp mirror at walking distance!)
                float ndv = saturate(dot(N, V));
                float fresnel = 0.02 + 0.98 * pow(1.0 - ndv, 4.6);

                // Directional monsoon moonlight + warm glancing practical catch on thin ripple crests
                Light mainLight = GetMainLight();
                float3 Hmoon = normalize(mainLight.direction + V);
                float specMoon = pow(saturate(dot(N, Hmoon)), 160.0) * 0.28;

                float3 Lprac = normalize(_GlancingLight.xyz);
                float3 Hprac = normalize(Lprac + V);
                float specPrac = pow(saturate(dot(N, Hprac)), 96.0) * 0.16;

                // Deep dark mirror monsoon puddle (zero chalky/milky oval tint!) + crisp reflection + delicate rain rings
                half3 waterCol = half3(0.010, 0.015, 0.020)
                    + envRefl * lerp(0.14h, 1.05h, fresnel) * lerp(half3(0.74, 0.82, 0.88), _Glint.rgb, 0.30h)
                    + mainLight.color * (specMoon + rings * 0.12h)
                    + half3(0.92, 0.72, 0.44) * (specPrac + rings * 0.08h);

                waterCol = MixFog(waterCol, input.fogFactor);
                // Smooth feathered blend: subtle dark damp edge -> glassy reflective interior
                half alpha = saturate(dampRim * 0.18h + waterBody * lerp(0.16h, 0.72h, fresnel) + rings * 0.14h);
                return half4(waterCol, alpha * saturate(_Glint.a));
            }
            ENDHLSL
        }
    }
}
