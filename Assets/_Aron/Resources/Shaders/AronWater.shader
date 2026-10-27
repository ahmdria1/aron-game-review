// Mobile water for seas, the Nile, the Amazon and the Iraqi marshes. High phones move the vertices with a few
// sine waves; everyone else scrolls the same waves through the normal only. Colour goes from shallow to deep
// with the view angle, foam gathers at the edges and on crests, and the sun leaves a specular path.
// Reflections come from the scene's reflection probe, or from a planar camera on high phones with the toggle on.
Shader "Aron/Water"
{
    Properties
    {
        _ShallowColor ("Shallow", Color) = (0.2, 0.65, 0.7, 1)
        _DeepColor ("Deep", Color) = (0.03, 0.2, 0.35, 1)
        _FoamColor ("Foam", Color) = (0.95, 0.97, 1, 1)
        _WaveHeight ("Wave height (m)", Range(0, 1.5)) = 0.25
        _WaveLength ("Wave length (m)", Range(1, 40)) = 9
        _WaveSpeed ("Wave speed", Range(0, 3)) = 1
        _Flow ("River flow (xz, m/s)", Vector) = (0, 0, 0, 0)
        _Choppy ("Choppiness", Range(0, 1)) = 0.4
        _Foam ("Foam amount", Range(0, 1)) = 0.4
        _EdgeFoam ("Edge foam width (0..0.5 of the surface)", Range(0, 0.5)) = 0.08
        _Murk ("Murk (rivers, marshes)", Range(0, 1)) = 0
        _Smoothness ("Smoothness", Range(0, 1)) = 0.92
        _Wind ("Wind (set by weather)", Range(0, 1)) = 0.3
        _RainRipples ("Rain ripples", Range(0, 1)) = 0
        _PlanarTex ("Planar reflection", 2D) = "black" {}
        _Fade ("Reflection strength", Range(0, 1)) = 0.6
    }
    SubShader
    {
        Tags { "RenderType" = "Opaque" "Queue" = "Geometry+10" "RenderPipeline" = "UniversalPipeline" }

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForward" }
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_fog
            #pragma multi_compile _ _ARON_WAVES
            #pragma multi_compile _ _ARON_PLANAR
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            #include "AronWeather.hlsl"

            CBUFFER_START(UnityPerMaterial)
                half4 _ShallowColor, _DeepColor, _FoamColor;
                float4 _Flow, _PlanarTex_ST;
                float _WaveHeight, _WaveLength, _WaveSpeed, _Choppy, _Foam, _EdgeFoam, _Murk, _Smoothness, _Wind, _RainRipples, _Fade;
            CBUFFER_END
            TEXTURE2D(_PlanarTex); SAMPLER(sampler_PlanarTex);
            float _AronWind; // global, set by WeatherDirector

            struct Attributes { float4 positionOS : POSITION; float2 uv : TEXCOORD0; };
            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
                float2 uv : TEXCOORD1;
                float crest : TEXCOORD2;
                float4 screen : TEXCOORD3;
                half fog : TEXCOORD4;
            };

            // Four directional sine waves. Returns height; dh is the slope (dh/dx, dh/dz).
            float Waves(float2 p, float t, out float2 dh)
            {
                float wind = saturate(max(_Wind, _AronWind));
                float amp = _WaveHeight * (0.6 + 0.8 * wind);
                float k0 = 6.2831853 / _WaveLength;
                float2 dirs[4] = { float2(1, 0.2), float2(0.7, 0.7), float2(-0.3, 1), float2(0.9, -0.45) };
                float scales[4] = { 1.0, 0.62, 0.41, 0.27 };
                float h = 0;
                dh = 0;
                p -= _Flow.xz * t;
                for (int i = 0; i < 4; i++)
                {
                    float2 d = normalize(dirs[i]);
                    float k = k0 / scales[i];
                    float a = amp * scales[i] * 0.5;
                    float w = sqrt(9.81 * k) * _WaveSpeed;
                    float ph = dot(d, p) * k - w * t;
                    float s = sin(ph);
                    // Sharpen the crests a little for choppy water.
                    float shaped = s + _Choppy * 0.25 * (s * s - 0.5);
                    h += a * shaped;
                    dh += d * (a * k * cos(ph) * (1.0 + _Choppy * 0.5 * s));
                }
                return h;
            }

            Varyings vert(Attributes v)
            {
                Varyings o;
                float3 ws = TransformObjectToWorld(v.positionOS.xyz);
                float2 dh;
                float h = Waves(ws.xz, _Time.y, dh);
                o.crest = saturate(h / max(_WaveHeight, 0.01) * 0.8 + 0.2);
            #if defined(_ARON_WAVES)
                ws.y += h;
            #endif
                o.positionWS = ws;
                o.positionCS = TransformWorldToHClip(ws);
                o.uv = v.uv;
                o.screen = ComputeScreenPos(o.positionCS);
                o.fog = ComputeFogFactor(o.positionCS.z);
                return o;
            }

            half4 frag(Varyings i) : SV_Target
            {
                float t = _Time.y;
                float2 dh;
                float h = Waves(i.positionWS.xz, t, dh);
                // Fine detail ripples on top of the big waves, plus rain rings when it rains.
                float2 fine = float2(AronNoise2(i.positionWS.xz * 1.3 + t * 0.6) - 0.5, AronNoise2(i.positionWS.zx * 1.3 - t * 0.5) - 0.5);
                dh += fine * (0.15 + 0.25 * _AronWind);
                if (_RainRipples > 0.01)
                {
                    float2 cell = floor(i.positionWS.xz * 1.5);
                    float2 f = frac(i.positionWS.xz * 1.5) - 0.5;
                    float phase = frac(t * 0.9 + AronHash21(cell));
                    float r = length(f - (float2(AronHash21(cell + 3), AronHash21(cell + 7)) - 0.5) * 0.4);
                    float ring = sin((r - phase * 0.5) * 60.0) * saturate(1 - abs(r - phase * 0.5) * 12.0) * (1 - phase);
                    dh += normalize(f + 1e-4) * ring * 0.25 * _RainRipples;
                }
                float3 n = normalize(float3(-dh.x, 1.0, -dh.y));
                float3 v = normalize(GetWorldSpaceViewDir(i.positionWS));
                float ndv = saturate(dot(n, v));
                float fresnel = 0.02 + 0.98 * pow(1.0 - ndv, 5.0);

                half3 water = lerp(_DeepColor.rgb, _ShallowColor.rgb, ndv * ndv * (1 - _Murk * 0.5));
                // Murky rivers and marsh water: brown-green, less clear, softer reflections.
                water = lerp(water, water * half3(0.85, 0.8, 0.6), _Murk * 0.5);

                // Reflection: planar texture when available, else the reflection probe / sky.
                float3 r = reflect(-v, n);
                half3 refl;
            #if defined(_ARON_PLANAR)
                // The reflection camera's image is the mirror view flipped left to right.
                float2 suv = i.screen.xy / i.screen.w;
                suv.x = 1.0 - suv.x;
                suv += n.xz * 0.03;
                refl = SAMPLE_TEXTURE2D(_PlanarTex, sampler_PlanarTex, suv).rgb;
            #else
                refl = GlossyEnvironmentReflection(r, i.positionWS, 1.0 - _Smoothness, 1.0);
            #endif
                half3 col = lerp(water, refl, fresnel * _Fade * (1 - _Murk * 0.6));

                // Sun on the water.
                Light sun = GetMainLight();
                float3 hv = normalize(sun.direction + v);
                float spec = pow(saturate(dot(n, hv)), lerp(64.0, 900.0, _Smoothness)) * 4.0;
                col += sun.color * spec * (1 - _Murk * 0.5);
                col *= lerp(0.35, 1.0, saturate(sun.direction.y * 2.0 + 0.4)) * (0.6 + 0.4 * sun.color.r);

                // Foam on crests (more with wind) and at the shore edges of the surface.
                float crestFoam = smoothstep(0.75, 1.0, saturate(h / max(_WaveHeight, 0.01) * 0.6 + 0.5)) * (0.3 + 0.7 * _AronWind);
                float2 e = min(i.uv, 1.0 - i.uv);
                float edge = 1.0 - smoothstep(0.0, max(_EdgeFoam, 1e-3), min(e.x, e.y));
                float breakup = AronNoise2(i.positionWS.xz * 2.5 + t * 0.3);
                float foam = saturate((crestFoam + edge) * _Foam * smoothstep(0.35, 0.65, breakup + edge * 0.3));
                col = lerp(col, _FoamColor.rgb * (0.6 + 0.4 * sun.color.r), foam);

                col = MixFog(col, i.fog);
                return half4(col, 1);
            }
            ENDHLSL
        }
    }
    Fallback "Universal Render Pipeline/Unlit"
}
