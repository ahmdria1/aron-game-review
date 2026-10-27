// Rain and snow drawn as one mesh of quads that all move on the GPU. Each quad stores a random seed (uv2.x) and a
// rank (uv2.y); quads with rank above _Amount collapse to nothing, so the script changes intensity with a single
// float. Drops wrap around a box that follows the camera, so the shower never runs out.
Shader "Aron/Rain"
{
    Properties
    {
        _Color ("Colour", Color) = (0.75, 0.8, 0.9, 0.5)
        _Amount ("Amount (0..1)", Range(0, 1)) = 1
        _Box ("Box size (m)", Vector) = (30, 18, 30, 0)
        _Center ("Box centre (world)", Vector) = (0, 0, 0, 0)
        _Fall ("Fall speed (m/s)", Float) = 11
        _WindVec ("Wind push (xz, m/s)", Vector) = (1.5, 0, 0, 0)
        _Size ("Drop width, length (m)", Vector) = (0.02, 0.55, 0, 0)
        _Light ("Scene light", Range(0, 3)) = 1
    }
    SubShader
    {
        Tags { "Queue" = "Transparent" "RenderType" = "Transparent" "IgnoreProjector" = "True" "RenderPipeline" = "UniversalPipeline" }
        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
        Cull Off

        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile _ _ARON_SNOW
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                half4 _Color;
                float4 _Box, _Center, _WindVec, _Size;
                float _Amount, _Fall, _Light;
            CBUFFER_END

            struct Attributes { float4 positionOS : POSITION; float2 uv : TEXCOORD0; float2 seed : TEXCOORD1; };
            struct Varyings { float4 positionCS : SV_POSITION; float2 uv : TEXCOORD0; half alpha : TEXCOORD1; };

            float Rand(float s, float k) { return frac(sin(s * 12.9898 + k * 78.233) * 43758.5453); }

            Varyings vert(Attributes v)
            {
                Varyings o;
                float s = v.seed.x;
                float t = _Time.y;
                float3 box = _Box.xyz;
            #if defined(_ARON_SNOW)
                float fall = _Fall * (0.6 + 0.4 * Rand(s, 4));
                float3 drift = float3(_WindVec.x, 0, _WindVec.z) * 0.6;
                float wobble = sin(t * (1.0 + Rand(s, 5)) + s * 30.0) * 0.35;
            #else
                float fall = _Fall * (0.85 + 0.3 * Rand(s, 4));
                float3 drift = float3(_WindVec.x, 0, _WindVec.z);
                float wobble = 0;
            #endif
                float3 base = float3(Rand(s, 1), Rand(s, 2), Rand(s, 3)) * box;
                float3 p = base + drift * t + float3(wobble, -fall * t, wobble * 0.7);
                // Wrap into the box around the camera-following centre.
                float3 rel = p - (_Center.xyz - box * 0.5);
                rel = rel - floor(rel / box) * box;
                float3 world = _Center.xyz - box * 0.5 + rel;

                // Stretch along the fall direction, face the camera sideways.
                float3 vel = normalize(float3(drift.x, -fall, drift.z));
                float3 toCam = normalize(_WorldSpaceCameraPos - world);
                float3 side = normalize(cross(vel, toCam) + 1e-5);
                float2 c = v.uv * 2.0 - 1.0;
            #if defined(_ARON_SNOW)
                float3 up = normalize(cross(toCam, side));
                float size = _Size.x * (0.6 + 0.8 * Rand(s, 6));
                world += side * c.x * size + up * c.y * size;
            #else
                world += side * c.x * _Size.x - vel * c.y * _Size.y * 0.5;
            #endif
                bool on = v.seed.y <= _Amount;
                o.positionCS = on ? TransformWorldToHClip(world) : float4(0, 0, -10, 1);
                o.uv = v.uv;
                // Fade near the box walls so wrapping never pops, and right in front of the lens.
                float3 edge = abs(rel / box - 0.5) * 2.0;
                float wall = saturate((1.0 - max(edge.x, edge.z)) * 4.0) * saturate((1.0 - edge.y) * 4.0);
                float near = saturate(distance(world, _WorldSpaceCameraPos) - 0.6);
                o.alpha = wall * near;
                return o;
            }

            half4 frag(Varyings i) : SV_Target
            {
                float2 c = i.uv * 2.0 - 1.0;
            #if defined(_ARON_SNOW)
                half a = saturate(1.0 - dot(c, c));
                a *= a;
            #else
                half a = saturate(1.0 - abs(c.x)) * saturate(1.0 - abs(c.y) * 0.8);
            #endif
                return half4(_Color.rgb * _Light, _Color.a * a * i.alpha);
            }
            ENDHLSL
        }
    }
}
