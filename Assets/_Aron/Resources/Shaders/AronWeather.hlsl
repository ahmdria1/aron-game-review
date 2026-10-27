#ifndef ARON_WEATHER_INCLUDED
#define ARON_WEATHER_INCLUDED

// Small hash and value-noise helpers shared by the sky, cloud, water and puddle shaders. No textures needed.

float AronHash21(float2 p)
{
    p = frac(p * float2(123.34, 456.21));
    p += dot(p, p + 45.32);
    return frac(p.x * p.y);
}

float AronHash31(float3 p)
{
    p = frac(p * 0.1031);
    p += dot(p, p.zyx + 31.32);
    return frac((p.x + p.y) * p.z);
}

float AronNoise2(float2 p)
{
    float2 i = floor(p);
    float2 f = frac(p);
    float2 u = f * f * (3.0 - 2.0 * f);
    float a = AronHash21(i);
    float b = AronHash21(i + float2(1, 0));
    float c = AronHash21(i + float2(0, 1));
    float d = AronHash21(i + float2(1, 1));
    return lerp(lerp(a, b, u.x), lerp(c, d, u.x), u.y);
}

float AronFbm2(float2 p, int octaves)
{
    float sum = 0.0;
    float amp = 0.5;
    for (int o = 0; o < 4; o++)
    {
        if (o >= octaves) break;
        sum += amp * AronNoise2(p);
        p = p * 2.03 + float2(17.1, 9.7);
        amp *= 0.5;
    }
    return sum;
}

#endif
