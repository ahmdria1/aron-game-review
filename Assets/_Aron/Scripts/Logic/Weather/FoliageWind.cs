// (c) Ahmed Alamrani. All rights reserved.
using System;
using System.Collections.Generic;

namespace Aron.Logic.Sky
{
    /// <summary>
    /// How hard trees and palms sway and how loud they rustle, as numbers (Resources/Config/foliage_wind.json).
    /// The renderer (AronFoliageWind.shader) and the audio only read what <see cref="FoliageWindMath"/> returns, so the
    /// whole mapping from weather to motion and sound can be tested without the engine.
    /// </summary>
    [Serializable]
    public class FoliageWindFile
    {
        /// <summary>Sway in metres at the very tip: in a dead calm, at full wind, and the hard ceiling after the weather boosts.</summary>
        public float idleSway = 0.02f, maxSway = 0.3f, maxTotalSway = 0.4f;
        /// <summary>Wind to sway: below 1 the first breaths already move things, above 1 the strong wind catches up late.</summary>
        public float swayCurve = 1.3f;
        /// <summary>Extra sway at full dust (a sandstorm) and full rain, as a multiplier.</summary>
        public float sandstormBoost = 1.3f, rainBoost = 1.12f;
        /// <summary>Sway speed in hertz, calm and windy.</summary>
        public float freqCalm = 0.45f, freqWindy = 1.25f;
        /// <summary>Frond flutter: amplitude in metres (calm, windy) and speed in hertz (calm, windy).</summary>
        public float flutterIdle = 0.008f, flutterMax = 0.07f, flutterFreqCalm = 3.5f, flutterFreqWindy = 8.5f;
        /// <summary>Gusts only start above this wind; at full gust they add this share of the sway.</summary>
        public float gustMinWind = 0.2f, gustShare = 0.6f;
        /// <summary>Trunk to tip: weight = height fraction to this power, so the base barely moves.</summary>
        public float weightCurve = 1.5f;
        /// <summary>Rustle: wind below the first value is silent; loudest volume; how the wind maps to volume.</summary>
        public float rustleMinWind = 0.06f, rustleMaxVolume = 1f, rustleCurve = 0.8f;
        /// <summary>Metres: foliage this close is at full volume, and beyond the second one it is silent.</summary>
        public float fullVolumeDistance = 3f, audibleDistance = 24f;
        public float pitchCalm = 0.88f, pitchWindy = 1.22f;
        /// <summary>Frond creaks: gust level that allows them, chances per second at full gust, seconds between two, how close, how loud.</summary>
        public float creakMinGust = 0.5f, creakRate = 0.35f, creakGapMin = 3.5f, creakGapMax = 9f, creakMaxDistance = 18f, creakVolume = 0.7f;
        /// <summary>
        /// Rustle strength layers (light, moderate or medium, strong): the wind at which the middle and the strong loop take over,
        /// and how wide the crossfade around each of those two points is.
        /// </summary>
        public float layerMidAt = 0.38f, layerStrongAt = 0.72f, layerBlend = 0.2f;
        /// <summary>Gust whooshes: gust level that allows one, chances per second at full gust, seconds between two, how close, how loud.</summary>
        public float gustSoundMinGust = 0.55f, gustSoundRate = 0.25f, gustSoundGapMin = 6f, gustSoundGapMax = 16f, gustSoundMaxDistance = 30f, gustSoundVolume = 0.6f;

        public List<string> Validate()
        {
            var e = new List<string>();
            if (idleSway < 0f || idleSway > maxSway) e.Add("idleSway 0..maxSway");
            if (maxSway <= 0f || maxSway > maxTotalSway) e.Add("maxSway 0..maxTotalSway");
            if (maxTotalSway > 0.6f) e.Add("maxTotalSway at most 0.6 m (trees must not wobble like jelly)");
            if (swayCurve < 0.5f || swayCurve > 3f) e.Add("swayCurve 0.5..3");
            if (sandstormBoost < 1f || sandstormBoost > 2f || rainBoost < 1f || rainBoost > 2f) e.Add("boosts 1..2");
            if (freqCalm <= 0f || freqWindy < freqCalm || freqWindy > 3f) e.Add("sway frequency calm <= windy <= 3 Hz");
            if (flutterIdle < 0f || flutterMax < flutterIdle || flutterMax > 0.15f) e.Add("flutter 0 <= idle <= max <= 0.15 m");
            if (flutterFreqCalm <= 0f || flutterFreqWindy < flutterFreqCalm || flutterFreqWindy > 14f) e.Add("flutter frequency calm <= windy <= 14 Hz");
            if (gustMinWind < 0f || gustMinWind >= 1f || gustShare < 0f || gustShare > 1f) e.Add("gust settings");
            if (weightCurve < 1f || weightCurve > 3f) e.Add("weightCurve 1..3");
            if (rustleMinWind < 0f || rustleMinWind >= 1f || rustleMaxVolume <= 0f || rustleMaxVolume > 1f || rustleCurve <= 0f) e.Add("rustle settings");
            if (fullVolumeDistance < 0f || audibleDistance <= fullVolumeDistance) e.Add("rustle distances: full < audible");
            if (pitchCalm <= 0f || pitchWindy < pitchCalm || pitchWindy > 2f) e.Add("pitch calm <= windy <= 2");
            if (creakMinGust < 0f || creakMinGust >= 1f || creakRate <= 0f || creakGapMin < 0.5f || creakGapMax < creakGapMin || creakMaxDistance <= 0f || creakVolume <= 0f || creakVolume > 1f) e.Add("creak settings");
            if (layerBlend <= 0f || layerBlend > 0.5f || layerMidAt - layerBlend * 0.5f <= rustleMinWind || layerStrongAt <= layerMidAt + layerBlend * 0.5f || layerStrongAt + layerBlend * 0.5f > 1f) e.Add("rustle layers: min wind < mid < strong < 1, blends must not overlap");
            if (gustSoundMinGust < 0f || gustSoundMinGust >= 1f || gustSoundRate <= 0f || gustSoundGapMin < 1f || gustSoundGapMax < gustSoundGapMin || gustSoundMaxDistance <= 0f || gustSoundVolume <= 0f || gustSoundVolume > 1f) e.Add("gust sound settings");
            return e;
        }
    }

    /// <summary>Everything the shader needs for one frame, already scaled for the weather, the phone and reduce motion.</summary>
    public struct FoliageWindParams
    {
        /// <summary>Downwind direction on the ground (unit vector, x and z).</summary>
        public float DirX, DirZ;
        /// <summary>Sway at the tip in metres, and its speed in hertz.</summary>
        public float Sway, SwayHz;
        /// <summary>Frond flutter in metres and hertz (zero on the Low tier, which does not flutter).</summary>
        public float Flutter, FlutterHz;
        /// <summary>Gust 0..1 (a pulse that comes and goes).</summary>
        public float Gust;
        /// <summary>0 = Low (one sine), 1 = Medium, 2 = High.</summary>
        public int Tier;
    }

    public static class FoliageWindMath
    {
        /// <summary>Trunk-to-tip weight for a point at this share of the tree's height (0 at the root, 1 at the top). Rises monotonically.</summary>
        public static float Weight(FoliageWindFile f, float heightFraction)
        {
            float h = Clamp01(heightFraction);
            return (float)Math.Pow(h, f.weightCurve);
        }

        private static float Boost(FoliageWindFile f, float dust, float rain)
            => 1f + (f.sandstormBoost - 1f) * Clamp01(dust) + (f.rainBoost - 1f) * Clamp01(rain);

        /// <summary>Tip sway in metres. Reduce motion scales it by the same share as the wind push on the hero (<c>reduceMotionWindShare</c>).</summary>
        public static float SwayAmplitude(FoliageWindFile f, float wind, float dust, float rain, bool reduceMotion, float reduceShare)
        {
            float w = (float)Math.Pow(Clamp01(wind), f.swayCurve);
            float a = (f.idleSway + (f.maxSway - f.idleSway) * w) * Boost(f, dust, rain);
            a = Math.Min(a, f.maxTotalSway);
            return reduceMotion ? a * Clamp01(reduceShare) : a;
        }

        public static float SwayFrequency(FoliageWindFile f, float wind) => Lerp(f.freqCalm, f.freqWindy, Clamp01(wind));

        public static float FlutterAmplitude(FoliageWindFile f, float wind, float dust, float rain, bool reduceMotion, float reduceShare)
        {
            float a = Lerp(f.flutterIdle, f.flutterMax, Clamp01(wind)) * Boost(f, dust, rain);
            a = Math.Min(a, f.flutterMax * 1.4f);
            return reduceMotion ? a * Clamp01(reduceShare) : a;
        }

        public static float FlutterFrequency(FoliageWindFile f, float wind) => Lerp(f.flutterFreqCalm, f.flutterFreqWindy, Clamp01(wind));

        /// <summary>
        /// A gust 0..1: sharp pulses every <paramref name="periodSeconds"/> with a second, quicker wave woven in so they are not regular.
        /// Silent in calm weather, full strength only in strong wind.
        /// </summary>
        public static float GustPulse(FoliageWindFile f, double time, float periodSeconds, float wind)
        {
            float s = Clamp01((wind - f.gustMinWind) / Math.Max(0.0001f, 1f - f.gustMinWind));
            if (s <= 0f) return 0f;
            double p = Math.Max(0.5f, periodSeconds);
            double a = 0.5 + 0.5 * Math.Sin(2.0 * Math.PI * time / p);
            double b = 0.5 + 0.5 * Math.Sin(2.0 * Math.PI * time / (p * 0.37) + 1.3);
            double raw = 0.7 * a + 0.3 * b;
            return (float)(raw * raw * raw) * s;
        }

        /// <summary>The Low tier uses one sine and no flutter; Medium and High use the full path.</summary>
        public static bool UsesCheapPath(QualityTier tier) => tier == QualityTier.Low;
        public static bool FlutterEnabled(QualityTier tier) => tier != QualityTier.Low;

        /// <summary>All shader inputs for this frame.</summary>
        public static FoliageWindParams Compute(FoliageWindFile f, QualityTier tier, float wind, float dust, float rain, float dirX, float dirZ,
            bool reduceMotion, float reduceShare, double time, float gustPeriodSeconds)
        {
            float len = (float)Math.Sqrt(dirX * dirX + dirZ * dirZ);
            if (len < 1e-4f) { dirX = 1f; dirZ = 0f; } else { dirX /= len; dirZ /= len; }
            bool cheap = UsesCheapPath(tier);
            float gust = cheap ? 0f : GustPulse(f, time, gustPeriodSeconds, wind);
            if (reduceMotion) gust *= Clamp01(reduceShare);
            return new FoliageWindParams
            {
                DirX = dirX, DirZ = dirZ,
                Sway = SwayAmplitude(f, wind, dust, rain, reduceMotion, reduceShare),
                SwayHz = SwayFrequency(f, wind),
                Flutter = FlutterEnabled(tier) ? FlutterAmplitude(f, wind, dust, rain, reduceMotion, reduceShare) : 0f,
                FlutterHz = FlutterFrequency(f, wind),
                Gust = gust,
                Tier = (int)tier,
            };
        }

        // ------------------------------------------------------------------ audio

        /// <summary>
        /// Rustle volume 0..1 for the nearest foliage of one kind: wind sets the level, distance fades it from full (close) to silent (far).
        /// </summary>
        public static float RustleVolume(FoliageWindFile f, float wind, float distance)
        {
            if (wind <= f.rustleMinWind) return 0f;
            if (distance >= f.audibleDistance) return 0f;
            float level = (float)Math.Pow(Clamp01((wind - f.rustleMinWind) / (1f - f.rustleMinWind)), f.rustleCurve);
            float near = distance <= f.fullVolumeDistance ? 1f : 1f - (distance - f.fullVolumeDistance) / (f.audibleDistance - f.fullVolumeDistance);
            return f.rustleMaxVolume * level * near * near;
        }

        /// <summary>How much of each strength loop to play: light, moderate (medium for leaves), strong. Equal power, so the crossfade never dips.</summary>
        public struct RustleLayers
        {
            public float Light, Mid, Strong;
            public RustleLayers(float light, float mid, float strong) { Light = light; Mid = mid; Strong = strong; }
        }

        /// <summary>
        /// The mix of the three strength loops for a wind: the light loop in a breeze, the middle one in a steady wind, the strong one in a gale,
        /// crossfaded around <see cref="FoliageWindFile.layerMidAt"/> and <see cref="FoliageWindFile.layerStrongAt"/>.
        /// A low-end phone plays only the nearest single loop (no crossfade, so one voice per kind instead of two).
        /// </summary>
        public static RustleLayers LayerMix(FoliageWindFile f, float wind, bool lowTier)
        {
            float w = Clamp01(wind);
            if (lowTier)
            {
                if (w < f.layerMidAt) return new RustleLayers(1f, 0f, 0f);
                return w < f.layerStrongAt ? new RustleLayers(0f, 1f, 0f) : new RustleLayers(0f, 0f, 1f);
            }
            float x1 = Clamp01((w - (f.layerMidAt - f.layerBlend * 0.5f)) / f.layerBlend);
            float x2 = Clamp01((w - (f.layerStrongAt - f.layerBlend * 0.5f)) / f.layerBlend);
            const float HalfPi = 1.5707964f;
            float light = (float)Math.Cos(x1 * HalfPi), rest = (float)Math.Sin(x1 * HalfPi);
            float mid = rest * (float)Math.Cos(x2 * HalfPi), strong = rest * (float)Math.Sin(x2 * HalfPi);
            return new RustleLayers(light, mid, strong);
        }

        /// <summary>
        /// Whether a gust whoosh should play now: in a real gust, with some foliage in earshot, more likely the harder the gust.
        /// <paramref name="random01"/> is a random number 0..1; <paramref name="dt"/> the seconds since the last check.
        /// </summary>
        public static bool GustSoundDue(FoliageWindFile f, float gust, float distance, float random01, float dt)
        {
            if (gust < f.gustSoundMinGust || distance > f.gustSoundMaxDistance) return false;
            float strength = Clamp01((gust - f.gustSoundMinGust) / Math.Max(0.0001f, 1f - f.gustSoundMinGust));
            float chance = 1f - (float)Math.Exp(-f.gustSoundRate * (0.4f + 0.6f * strength) * Math.Max(0f, dt));
            return random01 < chance;
        }

        /// <summary>Seconds to wait after a gust whoosh before the next may play.</summary>
        public static float NextGustSoundGap(FoliageWindFile f, float random01) => Lerp(f.gustSoundGapMin, f.gustSoundGapMax, Clamp01(random01));

        /// <summary>Stronger wind makes the rustle a little higher and faster.</summary>
        public static float RustlePitch(FoliageWindFile f, float wind) => Lerp(f.pitchCalm, f.pitchWindy, Clamp01(wind));

        /// <summary>
        /// Whether a frond creak should play now: only in a real gust, close to some foliage, and more often the harder the gust.
        /// <paramref name="random01"/> is a random number 0..1; <paramref name="dt"/> the seconds since the last check.
        /// </summary>
        public static bool CreakDue(FoliageWindFile f, float gust, float distance, float random01, float dt)
        {
            if (gust < f.creakMinGust || distance > f.creakMaxDistance) return false;
            float strength = Clamp01((gust - f.creakMinGust) / Math.Max(0.0001f, 1f - f.creakMinGust));
            float chance = 1f - (float)Math.Exp(-f.creakRate * (0.4f + 0.6f * strength) * Math.Max(0f, dt));
            return random01 < chance;
        }

        /// <summary>Seconds to wait after a creak before the next may play.</summary>
        public static float NextCreakGap(FoliageWindFile f, float random01) => Lerp(f.creakGapMin, f.creakGapMax, Clamp01(random01));

        private static float Clamp01(float v) => v < 0f ? 0f : v > 1f ? 1f : v;
        private static float Lerp(float a, float b, float t) => a + (b - a) * t;
    }
}
