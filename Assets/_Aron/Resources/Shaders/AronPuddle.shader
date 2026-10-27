// Puddles that appear as the ground gets wet: a dark, glossy disc that takes the sky reflection, with rain
// rings while it is raining. _Wet scales the puddle in and out; nothing is drawn when the ground is dry.
Shader "Aron/Puddle"
{
    Properties
    {
        _Tint ("Tint", Color) = (0.12, 0.13, 0.15, 0.85)
        _Wet ("Wetness", Range(0, 1)) = 0
        _Rain ("Rain", Range(0, 1)) = 0
        _Seed ("Seed", Float) = 0
    }
    SubShader
    {
        Tags { "Queue" = "Transparent-100" "RenderType" = "Transparent" "RenderPipeline" = "UniversalPipeline" }
        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
        Offset -1, -1

        Pass
        {
            Tags { "LightMode" = "UniversalForward" }
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_fog
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            #include "AronWeather.hlsl"

            CBUFFER_START(UnityPerMaterial)
                half4 _Tint;
                float _Wet, _Rain, _Seed;
            CBUFFER_END

            struct Attributes { float4 positionOS : POSITION; float2 uv : TEXCOORD0; };
            struct Varyings { float4 positionCS : SV_POSITION; float2 uv : TEXCOORD0; float3 positionWS : TEXCOORD1; half fog : TEXCOORD2; };

            Varyings vert(Attributes v)
            {
                Varyings o;
                o.positionWS = TransformObjectToWorld(v.positionOS.xyz);
                o.positionCS = TransformWorldToHClip(o.positionWS);
                o.uv = v.uv;
                o.fog = ComputeFogFactor(o.positionCS.z);
                return o;
            }

            half4 frag(Varyings i) : SV_Target
            {
                float2 c = i.uv * 2.0 - 1.0;
                float edge = length(c) + (AronFbm2(i.uv * 4.0 + _Seed, 3) - 0.5) * 0.5;
                float grow = saturate(_Wet * 1.3 - 0.2);
                float mask = smoothstep(grow, grow - 0.15, edge);
                if (mask <= 0.001) discard;

                float2 slope = 0;
                if (_Rain > 0.01)
                {
                    float t = _Time.y;
                    float2 g = i.positionWS.xz * 2.0;
                    float2 cell = floor(g);
                    float2 f = frac(g) - 0.5;
                    float ph = frac(t * 1.1 + AronHash21(cell + _Seed));
                    float r = length(f);
                    float ring = sin((r - ph * 0.5) * 50.0) * saturate(1.0 - abs(r - ph * 0.5) * 10.0) * (1.0 - ph);
                    slope = normalize(f + 1e-4) * ring * 0.3 * _Rain;
                }
                float3 n = normalize(float3(-slope.x, 1.0, -slope.y));
                float3 v = normalize(GetWorldSpaceViewDir(i.positionWS));
                float fres = 0.04 + 0.96 * pow(1.0 - saturate(dot(n, v)), 5.0);
                half3 refl = GlossyEnvironmentReflection(reflect(-v, n), i.positionWS, 0.05, 1.0);
                Light sun = GetMainLight();
                float spec = pow(saturate(dot(n, normalize(sun.direction + v))), 400.0) * 3.0;
                half3 col = lerp(_Tint.rgb, refl, saturate(fres + 0.25)) + sun.color * spec;
                col = MixFog(col, i.fog);
                return half4(col, _Tint.a * mask);
            }
            ENDHLSL
        }
    }
}
