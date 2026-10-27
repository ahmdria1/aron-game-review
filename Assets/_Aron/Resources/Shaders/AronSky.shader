// Sky dome for the weather system: Fajr-to-Maghrib gradient, sun disc, the moon in its real phase with the lit edge
// turned the right way, stars and the Milky Way on clear nights, and a painted cloud layer on low and medium phones.
// Every value is set by WeatherDirector each update; nothing here reads the clock except the cloud drift.
Shader "Aron/Sky"
{
    Properties
    {
        _ZenithColor ("Zenith", Color) = (0.22, 0.45, 0.85, 1)
        _HorizonColor ("Horizon", Color) = (0.7, 0.82, 0.95, 1)
        _GroundColor ("Below horizon", Color) = (0.35, 0.32, 0.3, 1)
        _SunDir ("Sun direction (to the sun)", Vector) = (0, 0.5, 0.8, 0)
        _SunColor ("Sun colour", Color) = (1, 0.95, 0.85, 1)
        _SunSize ("Sun size", Range(0.005, 0.1)) = 0.03
        _MoonDir ("Moon direction", Vector) = (0, 0.3, -0.9, 0)
        _MoonPhase ("x lit fraction, y bright limb (radians clockwise from up), z size, w visible", Vector) = (0.5, 0, 0.035, 1)
        _Stars ("Stars", Range(0, 1)) = 0
        _MilkyWay ("Milky Way", Range(0, 1)) = 0
        _StarSpin ("Star rotation (radians)", Float) = 0
        _CloudCover ("Cloud cover", Range(0, 1)) = 0.1
        _CloudColor ("Cloud lit colour", Color) = (1, 1, 1, 1)
        _CloudShade ("Cloud shade colour", Color) = (0.55, 0.58, 0.64, 1)
        _CloudSpeed ("Cloud drift (xy)", Vector) = (0.004, 0.001, 0, 0)
        _CloudLayer ("Draw clouds in the dome", Range(0, 1)) = 1
        _Haze ("Haze", Range(0, 1)) = 0
        _HazeColor ("Haze colour", Color) = (0.85, 0.75, 0.6, 1)
        _Flash ("Lightning (extra light)", Range(0, 3)) = 0
    }
    SubShader
    {
        Tags { "Queue" = "Background" "RenderType" = "Background" "PreviewType" = "Skybox" "RenderPipeline" = "UniversalPipeline" }
        Cull Off
        ZWrite Off

        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "AronWeather.hlsl"

            CBUFFER_START(UnityPerMaterial)
                half4 _ZenithColor, _HorizonColor, _GroundColor, _SunColor, _CloudColor, _CloudShade, _HazeColor;
                float4 _SunDir, _MoonDir, _MoonPhase, _CloudSpeed;
                float _SunSize, _Stars, _MilkyWay, _StarSpin, _CloudCover, _CloudLayer, _Haze, _Flash;
            CBUFFER_END

            struct Attributes { float4 positionOS : POSITION; };
            struct Varyings { float4 positionCS : SV_POSITION; float3 dir : TEXCOORD0; };

            Varyings vert(Attributes v)
            {
                Varyings o;
                o.positionCS = TransformObjectToHClip(v.positionOS.xyz);
                o.dir = v.positionOS.xyz;
                return o;
            }

            float3 Spin(float3 d, float a)
            {
                // Turn the star field about a tilted pole so it wheels slowly through the night.
                float s = sin(a), c = cos(a);
                float3 r = float3(d.x * c - d.z * s, d.y, d.x * s + d.z * c);
                float t = 0.42; // about 24 degrees of tilt
                return float3(r.x, r.y * cos(t) - r.z * sin(t), r.y * sin(t) + r.z * cos(t));
            }

            half StarField(float3 d)
            {
                float3 p = d * 220.0;
                float3 cell = floor(p);
                float h = AronHash31(cell);
                if (h < 0.965) return 0;
                float3 centre = cell + 0.5 + (float3(AronHash31(cell + 1.3), AronHash31(cell + 7.1), AronHash31(cell + 3.7)) - 0.5) * 0.6;
                float dist = length(p - centre);
                float twinkle = 0.75 + 0.25 * sin(_Time.y * (2.0 + h * 5.0) + h * 40.0);
                return saturate(1.0 - dist * 2.2) * (h - 0.965) / 0.035 * twinkle;
            }

            half MilkyWayBand(float3 d)
            {
                // A soft band along a great circle, broken up by dust lanes.
                float3 pole = normalize(float3(0.3, 0.45, 0.84));
                float b = abs(dot(d, pole));
                float band = exp(-b * b * 40.0);
                float n = AronFbm2(float2(atan2(d.z, d.x) * 3.0, d.y * 6.0), 4);
                float lanes = smoothstep(0.35, 0.65, AronFbm2(float2(atan2(d.z, d.x) * 7.0, d.y * 12.0) + 3.1, 3));
                return band * (0.35 + 0.65 * n) * (0.55 + 0.45 * lanes);
            }

            half3 Moon(float3 d, float3 m, out half mask)
            {
                mask = 0;
                float size = _MoonPhase.z;
                float cosAng = dot(d, m);
                if (cosAng < cos(size * 1.6)) return 0;
                // Disc frame as the viewer sees it: up is the world up projected onto the sky, right is clockwise.
                float3 up = normalize(float3(0, 1, 0) - m * m.y + float3(0, 0, 1e-4));
                float3 right = normalize(cross(up, m));
                float2 p = float2(dot(d - m, right), dot(d - m, up)) / size;
                float r2 = dot(p, p);
                half glow = saturate(1.0 - sqrt(r2) / 1.6) * 0.15 * _MoonPhase.x;
                if (r2 > 1.0) return glow * _MoonPhase.w;
                float3 n = float3(p, sqrt(1.0 - r2));
                // Direction of the sun seen from the moon, in the disc frame: towards the bright limb, tipped back by the phase.
                float k = saturate(_MoonPhase.x);
                float cosI = 2.0 * k - 1.0;
                float sinI = sqrt(saturate(1.0 - cosI * cosI));
                float2 limb = float2(sin(_MoonPhase.y), cos(_MoonPhase.y));
                float3 s = float3(limb * sinI, cosI);
                float lit = smoothstep(-0.03, 0.05, dot(n, s));
                float maria = 0.82 + 0.18 * AronNoise2(p * 3.0 + 5.0);
                mask = 1;
                // The dark side keeps a faint earthshine so a thin crescent still reads as a whole moon.
                half3 col = lerp(half3(0.05, 0.06, 0.09), half3(1.0, 0.98, 0.9) * maria, lit);
                return col * _MoonPhase.w;
            }

            half4 frag(Varyings i) : SV_Target
            {
                float3 d = normalize(i.dir);
                float up = d.y;
                float h = saturate(up);
                // Gradient: horizon colour hugging the horizon, zenith above, a darker ground below.
                half3 sky = lerp(_HorizonColor.rgb, _ZenithColor.rgb, pow(h, 0.45));
                sky = lerp(sky, _GroundColor.rgb, saturate(-up * 6.0));

                // Warm glow around the sun near the horizon (Fajr and Maghrib), and the disc itself.
                float3 sd = normalize(_SunDir.xyz);
                float sunDot = dot(d, sd);
                float glowAmt = pow(saturate(sunDot), 8.0) * saturate(1.0 - abs(sd.y) * 3.0);
                sky += _SunColor.rgb * glowAmt * 0.35;
                float disc = smoothstep(cos(_SunSize), cos(_SunSize * 0.8), sunDot) * step(-0.02, sd.y);
                sky += _SunColor.rgb * pow(saturate(sunDot), 600.0) * 0.6;

                // Night sky: stars and the Milky Way, only above the horizon.
                float above = smoothstep(0.0, 0.12, up);
                if (_Stars > 0.001)
                {
                    float3 sdir = Spin(d, _StarSpin);
                    sky += StarField(sdir) * _Stars * above;
                    sky += MilkyWayBand(sdir) * _MilkyWay * above * half3(0.55, 0.58, 0.7) * 0.5;
                }

                half moonMask;
                half3 moon = Moon(d, normalize(_MoonDir.xyz), moonMask);

                // Painted clouds on a flat layer overhead (low and medium tiers; high draws lit billboards instead).
                half cloud = 0;
                if (_CloudLayer > 0.5 && up > 0.0)
                {
                    float2 uv = d.xz / (up + 0.12) * 0.6 + _CloudSpeed.xy * _Time.y;
                    float n = AronFbm2(uv * 1.7, 4);
                    float cover = lerp(0.72, 0.18, _CloudCover);
                    cloud = smoothstep(cover, cover + 0.25, n) * smoothstep(0.0, 0.2, up);
                    cloud = saturate(cloud + _CloudCover * _CloudCover * 0.6 * smoothstep(0.0, 0.3, up));
                }
                half3 cloudCol = lerp(_CloudShade.rgb, _CloudColor.rgb, saturate(0.4 + 0.6 * saturate(dot(sd, float3(0, 1, 0)) + glowAmt)));

                half3 col = sky;
                col = lerp(col, col * 0.4 + moon, moonMask);
                col += moon * (1 - moonMask); // halo
                col += _SunColor.rgb * disc * (1.0 - cloud) * 2.0;
                col = lerp(col, cloudCol, cloud);
                // Haze and dust wash out the lower sky.
                col = lerp(col, _HazeColor.rgb, _Haze * (1.0 - h * 0.6));
                col += _Flash * half3(0.7, 0.75, 0.9) * (0.4 + 0.6 * cloud);
                return half4(col, 1);
            }
            ENDHLSL
        }
    }
    Fallback Off
}
