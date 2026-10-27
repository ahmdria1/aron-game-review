// Lit cloud puffs for high-end phones. Each billboard is shaded like a soft sphere so the side facing the sun
// is bright and the far side takes the shade colour, which reads as volume without ray marching.
Shader "Aron/Cloud"
{
    Properties
    {
        _LitColor ("Lit", Color) = (1, 1, 1, 1)
        _ShadeColor ("Shade", Color) = (0.55, 0.58, 0.66, 1)
        _SunDir ("Sun direction (world)", Vector) = (0, 0.6, 0.8, 0)
        _Opacity ("Opacity", Range(0, 1)) = 0.85
        _Seed ("Seed", Float) = 0
        _HazeColor ("Haze colour", Color) = (0.8, 0.8, 0.8, 1)
        _Haze ("Haze", Range(0, 1)) = 0
    }
    SubShader
    {
        Tags { "Queue" = "Transparent-50" "RenderType" = "Transparent" "IgnoreProjector" = "True" "RenderPipeline" = "UniversalPipeline" }
        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
        Cull Off

        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "AronWeather.hlsl"

            CBUFFER_START(UnityPerMaterial)
                half4 _LitColor, _ShadeColor, _HazeColor;
                float4 _SunDir;
                float _Opacity, _Seed, _Haze;
            CBUFFER_END

            struct Attributes { float4 positionOS : POSITION; float2 uv : TEXCOORD0; };
            struct Varyings { float4 positionCS : SV_POSITION; float2 uv : TEXCOORD0; float3 right : TEXCOORD1; float3 up : TEXCOORD2; float3 fwd : TEXCOORD3; };

            Varyings vert(Attributes v)
            {
                Varyings o;
                // Billboard: the quad's object origin stays put, the corners turn to face the camera.
                float3 centre = TransformObjectToWorld(float3(0, 0, 0));
                float3 scale = float3(length(UNITY_MATRIX_M._m00_m10_m20), length(UNITY_MATRIX_M._m01_m11_m21), 1);
                float3 fwd = normalize(_WorldSpaceCameraPos - centre);
                float3 right = normalize(cross(float3(0, 1, 0), fwd));
                float3 up = cross(fwd, right);
                float3 world = centre + right * v.positionOS.x * scale.x + up * v.positionOS.y * scale.y;
                o.positionCS = TransformWorldToHClip(world);
                o.uv = v.uv;
                o.right = right; o.up = up; o.fwd = fwd;
                return o;
            }

            half4 frag(Varyings i) : SV_Target
            {
                float2 c = i.uv * 2.0 - 1.0;
                // Lumpy outline: three overlapping blobs broken up with noise.
                float n = AronFbm2(i.uv * 3.0 + _Seed * 7.3 + _Time.y * 0.01, 4);
                float d = min(length(c - float2(-0.35, -0.1)) * 1.25, min(length(c - float2(0.3, -0.05)) * 1.35, length(c - float2(0.0, 0.2)) * 1.1));
                float shape = saturate((1.0 - d) * 2.2 + (n - 0.5) * 1.2);
                if (shape <= 0.001) discard;
                float3 nrm = normalize(i.right * c.x + i.up * c.y + i.fwd * sqrt(saturate(1.0 - dot(c, c) * 0.6)));
                float lit = saturate(dot(nrm, normalize(_SunDir.xyz)) * 0.6 + 0.5);
                half3 col = lerp(_ShadeColor.rgb, _LitColor.rgb, lit * (0.8 + 0.2 * n));
                // Far clouds sit beyond the fog end; haze them by hand instead of with scene fog.
                col = lerp(col, _HazeColor.rgb, _Haze);
                return half4(col, shape * _Opacity);
            }
            ENDHLSL
        }
    }
}
