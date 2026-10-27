#ifndef ARON_FOLIAGE_WIND_INCLUDED
#define ARON_FOLIAGE_WIND_INCLUDED

// Vertex sway for trees and palms. The weather system (FoliageWind.cs) sets these globals every frame:
//   _AronFoliageWind  : xy = downwind direction on the ground (unit), z = sway at the tip in metres, w = sway speed in hertz
//   _AronFoliageFlutter: x = frond flutter in metres, y = flutter speed in hertz, z = gust 0..1, w = share of the sway a full gust adds
// The sway weight (0 at the root, 1 at the tip) comes from, in this order of preference, the vertex colour red channel,
// the second UV set x, or the height of the vertex in the mesh. The flutter mask (fronds, leaf edges) is vertex colour
// green or UV2 y; in height mode it is a fraction of the sway weight.
// The low tier (ARON_FOLIAGE_LOW) has a cheaper path: one sine, no flutter, no gust.
float4 _AronFoliageWind;
float4 _AronFoliageFlutter;

float AronFoliageHash(float2 p)
{
    p = frac(p * float2(123.34, 456.21));
    p += dot(p, p + 45.32);
    return frac(p.x * p.y);
}

// positionOS and the object's own origin go in; the world position of the swayed vertex comes out.
float3 AronFoliageSway(float3 positionOS, float4 color, float2 uv2, float heightRange, float swayScale, float flutterScale)
{
    float3 positionWS = TransformObjectToWorld(positionOS);

    float weight;
    float flutterMask;
#if defined(_MASK_VERTEXCOLOR)
    weight = color.r;
    flutterMask = color.g;
#elif defined(_MASK_UV2)
    weight = uv2.x;
    flutterMask = uv2.y;
#else
    weight = pow(saturate(positionOS.y / max(heightRange, 0.01)), 1.5);
    flutterMask = weight * 0.4;
#endif

    float4x4 o2w = GetObjectToWorldMatrix();
    float3 originWS = float3(o2w._m03, o2w._m13, o2w._m23);
    // Per-instance phase: every tree (every object) starts its sway at a different moment, so a grove is never in step.
    float phase = AronFoliageHash(originWS.xz) * 6.2831853;
    float2 dir = _AronFoliageWind.xy;
    float amp = _AronFoliageWind.z * swayScale * weight;
    float hz = _AronFoliageWind.w;
    float t = _Time.y;
    // The wind is a wave that travels downwind across the stage, so neighbouring trees lean one after another.
    float travel = dot(originWS.xz, dir) * 0.35;
    float s1 = sin(t * hz * 6.2831853 + phase + travel);

#if defined(ARON_FOLIAGE_LOW)
    // Cheap path: lean downwind and swing on one sine.
    float2 offset = dir * (amp * (0.55 + 0.45 * s1));
    positionWS.xz += offset;
#else
    float s2 = sin(t * hz * 14.4513 + phase * 1.7 + travel * 1.4) * 0.35;
    float gust = _AronFoliageFlutter.z * (0.6 + 0.4 * sin(travel * 2.0 - t * 1.5));
    float2 perp = float2(-dir.y, dir.x);
    float lean = 0.5 + 0.35 * (s1 + s2) + _AronFoliageFlutter.w * gust;
    positionWS.xz += dir * (amp * lean) + perp * (amp * 0.25 * s2);
    positionWS.y -= amp * amp * 0.5 * lean;                       // a bending tip dips a little

    // Frond flutter: quick, small, with its own phase per object and per vertex.
    float fAmp = _AronFoliageFlutter.x * flutterScale * flutterMask * (1.0 + _AronFoliageFlutter.z * 1.5);
    if (fAmp > 0.0001)
    {
        float vp = phase + dot(positionOS, float3(12.9898, 78.233, 37.719));
        float f = t * _AronFoliageFlutter.y * 6.2831853 + vp;
        positionWS.y += sin(f) * fAmp;
        positionWS.xz += perp * (cos(f * 0.83 + phase) * fAmp * 0.6);
    }
#endif
    return positionWS;
}

#endif
