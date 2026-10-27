// (c) Ahmed Alamrani. All rights reserved.
using System;
using System.Collections.Generic;

namespace Aron.Logic.Sky
{
    /// <summary>What a weather looks like, as numbers the renderer blends between. All 0..1.</summary>
    public struct WeatherLook
    {
        public float CloudCover, Rain, Snow, Fog, Haze, Dust, Wind, Lightning, Wetness;

        public static WeatherLook Of(WeatherKind k)
        {
            switch (k)
            {
                case WeatherKind.Clouds: return new WeatherLook { CloudCover = 0.65f, Wind = 0.35f };
                case WeatherKind.Haze: return new WeatherLook { CloudCover = 0.1f, Haze = 0.7f, Wind = 0.2f };
                case WeatherKind.Rain: return new WeatherLook { CloudCover = 0.9f, Rain = 0.6f, Fog = 0.25f, Wind = 0.4f, Wetness = 1f };
                case WeatherKind.Thunderstorm: return new WeatherLook { CloudCover = 1f, Rain = 1f, Fog = 0.35f, Wind = 0.8f, Lightning = 1f, Wetness = 1f };
                case WeatherKind.Fog: return new WeatherLook { CloudCover = 0.5f, Fog = 0.9f, Wind = 0.05f, Wetness = 0.25f };
                case WeatherKind.Sandstorm: return new WeatherLook { CloudCover = 0.2f, Haze = 0.6f, Dust = 1f, Fog = 0.7f, Wind = 1f };
                case WeatherKind.Snow: return new WeatherLook { CloudCover = 0.85f, Snow = 0.8f, Fog = 0.3f, Wind = 0.3f };
                default: return new WeatherLook { CloudCover = 0.05f, Wind = 0.2f };
            }
        }

        public static WeatherLook Lerp(WeatherLook a, WeatherLook b, float t)
        {
            t = t < 0f ? 0f : t > 1f ? 1f : t;
            // Smoothstep so the change starts and ends gently.
            t = t * t * (3f - 2f * t);
            return new WeatherLook
            {
                CloudCover = a.CloudCover + (b.CloudCover - a.CloudCover) * t, Rain = a.Rain + (b.Rain - a.Rain) * t,
                Snow = a.Snow + (b.Snow - a.Snow) * t, Fog = a.Fog + (b.Fog - a.Fog) * t, Haze = a.Haze + (b.Haze - a.Haze) * t,
                Dust = a.Dust + (b.Dust - a.Dust) * t, Wind = a.Wind + (b.Wind - a.Wind) * t,
                Lightning = a.Lightning + (b.Lightning - a.Lightning) * t, Wetness = a.Wetness + (b.Wetness - a.Wetness) * t,
            };
        }
    }

    /// <summary>The weather right now: what it was, what it is turning into, and how far along.</summary>
    public struct WeatherState
    {
        public WeatherKind From, To;
        public float Blend;
        public bool Dynamic;
        public WeatherLook Look => WeatherLook.Lerp(WeatherLook.Of(From), WeatherLook.Of(To), Blend);
        public WeatherKind Main => Blend < 0.5f ? From : To;
        public static WeatherState Fixed(WeatherKind k) => new WeatherState { From = k, To = k, Blend = 1f };
    }

    /// <summary>
    /// Picks the weather from the tables. Time is cut into slots of 8 to 20 minutes; each slot's weather comes from a
    /// hash of the region and the slot number, so every player in a region sees the same sky at the same time and a
    /// restart does not reroll it. The last part of each slot blends into the next one.
    /// </summary>
    public static class WeatherSchedule
    {
        public static WeatherKind Pick(WeatherWeights w, double roll)
        {
            float total = w.Total;
            if (total <= 0f) return WeatherKind.Clear;
            double x = Math.Max(0.0, Math.Min(0.999999, roll)) * total;
            foreach (WeatherKind k in Enum.GetValues(typeof(WeatherKind)))
            {
                x -= w.Of(k);
                if (x < 0) return k;
            }
            return w.Most();
        }

        public static double Roll(string region, long slot)
        {
            unchecked
            {
                ulong h = 1469598103934665603UL;
                foreach (char c in region ?? "") { h ^= c; h *= 1099511628211UL; }
                h ^= (ulong)slot; h *= 1099511628211UL;
                h ^= h >> 33; h *= 0xff51afd7ed558ccdUL; h ^= h >> 33;
                return (h >> 11) / (double)(1UL << 53);
            }
        }

        private static double SlotMinutes(WeatherTable t, string region, long slot)
            => t.changeMinMinutes + (t.changeMaxMinutes - t.changeMinMinutes) * Roll(region + "#len", slot);

        /// <summary>The weather at an instant, using the local month of the region for the season.</summary>
        public static WeatherState At(WeatherTable t, string country, string region, DateTime utc)
        {
            var c = t.Country(country);
            double offset = c?.utcOffset ?? 3.0;
            var local = utc.AddHours(offset);
            // Slots are counted from the start of the local day so each day starts fresh.
            double minutes = local.TimeOfDay.TotalMinutes;
            long day = (long)(local.Date - new DateTime(2000, 1, 1)).TotalDays;
            long slot = 0;
            double start = 0, len = SlotMinutes(t, region, day * 1000);
            while (start + len <= minutes && slot < 999) { start += len; slot++; len = SlotMinutes(t, region, day * 1000 + slot); }
            var weights = t.Weights(country, region, local.Month);
            var now = Pick(weights, Roll(region, day * 1000 + slot));
            var next = Pick(weights, Roll(region, day * 1000 + slot + 1));
            double blendMinutes = t.blendSeconds / 60.0;
            double into = minutes - (start + len - blendMinutes);
            float blend = into <= 0 ? 0f : (float)Math.Min(1.0, into / blendMinutes);
            return new WeatherState { From = now, To = next, Blend = blend, Dynamic = true };
        }

        public static WeatherKind FromStage(string stageWeather)
        {
            switch (stageWeather)
            {
                case "rain": return WeatherKind.Rain;
                case "sandstorm": return WeatherKind.Sandstorm;
                default: return WeatherKind.Clear;
            }
        }

        public static bool TryParse(string id, out WeatherKind k)
        {
            switch (id)
            {
                case "clear": k = WeatherKind.Clear; return true;
                case "clouds": k = WeatherKind.Clouds; return true;
                case "haze": k = WeatherKind.Haze; return true;
                case "rain": k = WeatherKind.Rain; return true;
                case "thunderstorm": k = WeatherKind.Thunderstorm; return true;
                case "fog": k = WeatherKind.Fog; return true;
                case "sandstorm": k = WeatherKind.Sandstorm; return true;
                case "snow": k = WeatherKind.Snow; return true;
                default: k = WeatherKind.Clear; return false;
            }
        }
    }

    public enum PlayMode { Story, FreePlay, Race, TimeTrial, Boss }

    /// <summary>Saved with the game.</summary>
    [Serializable]
    public class WeatherSettings
    {
        /// <summary>Weather that changes as you play. Off: each stage keeps its authored sky.</summary>
        public bool dynamicWeather = true;
        /// <summary>Free play only: wet ground grips a little less, snow slows a little. Off by default.</summary>
        public bool freePlayEffects;
        /// <summary>Mirror-like reflections on water; high tier only, costs battery.</summary>
        public bool planarReflections;
    }

    /// <summary>Who decides the weather in each mode.</summary>
    public static class WeatherPolicy
    {
        /// <summary>Weathers a race or time trial may be set to: nothing that hides the track or flashes.</summary>
        public static readonly string[] RaceWeathers = { "clear", "clouds", "haze", "snow" };

        /// <summary>Races, time trials and anything on a leaderboard use one fixed weather so times are fair.</summary>
        public static bool Fixed(PlayMode m) => m == PlayMode.Race || m == PlayMode.TimeTrial || m == PlayMode.Boss;

        /// <summary>
        /// The weather for a stage. <paramref name="authored"/> is the stage's own weather (clear, rain, sandstorm),
        /// <paramref name="trackWeather"/> the race track's fixed one, <paramref name="dynamicNow"/> what the
        /// schedule says right now.
        /// </summary>
        public static WeatherState Resolve(PlayMode mode, WeatherSettings settings, string authored, string trackWeather, WeatherState dynamicNow)
        {
            switch (mode)
            {
                case PlayMode.Race:
                case PlayMode.TimeTrial:
                    WeatherSchedule.TryParse(trackWeather, out var k);
                    return WeatherState.Fixed(k);
                case PlayMode.Boss:
                    return WeatherState.Fixed(WeatherSchedule.FromStage(authored));
            }
            var own = WeatherSchedule.FromStage(authored);
            // A stage written with rain or a sandstorm keeps it; that weather is part of how the stage plays.
            if (own != WeatherKind.Clear || settings == null || !settings.dynamicWeather) return WeatherState.Fixed(own);
            // Story stages written as clear only take the gentler skies, so a story stage is never surprised by a storm.
            if (mode == PlayMode.Story)
            {
                var gentle = new WeatherState { From = Gentle(dynamicNow.From), To = Gentle(dynamicNow.To), Blend = dynamicNow.Blend, Dynamic = true };
                return gentle;
            }
            var d = dynamicNow; d.Dynamic = true;
            return d;
        }

        private static WeatherKind Gentle(WeatherKind k)
            => k == WeatherKind.Thunderstorm ? WeatherKind.Rain : k == WeatherKind.Sandstorm ? WeatherKind.Haze : k;

        /// <summary>Grip for the hero. Dynamic weather only ever changes it in free play with the option on.</summary>
        public static float Traction(PlayMode mode, WeatherSettings settings, WeatherState w)
        {
            if (mode != PlayMode.FreePlay || settings == null || !settings.freePlayEffects || !w.Dynamic) return 1f;
            var look = w.Look;
            return 1f - 0.15f * look.Wetness - 0.2f * look.Snow;
        }
    }

    /// <summary>What each graphics tier can afford.</summary>
    public struct WeatherQuality
    {
        public int RainDrops, SnowFlakes, Splashes, CloudLayers;
        /// <summary>Clouds as lit billboards in the world (high) or painted into the sky dome (low, medium).</summary>
        public bool VolumetricClouds;
        public bool PuddleRipples, WetSpecular, VertexWaves, ReflectionProbe, LightningLight, ShorelineFoam;
        /// <summary>Times per second the sky and wetness update.</summary>
        public float UpdateHz;

        public static WeatherQuality For(QualityTier tier)
        {
            switch (tier)
            {
                case QualityTier.Low:
                    return new WeatherQuality { RainDrops = 600, SnowFlakes = 400, Splashes = 0, CloudLayers = 1, UpdateHz = 5f, ShorelineFoam = true };
                case QualityTier.Medium:
                    return new WeatherQuality { RainDrops = 1600, SnowFlakes = 900, Splashes = 16, CloudLayers = 2, PuddleRipples = true, WetSpecular = true, ReflectionProbe = true, LightningLight = true, ShorelineFoam = true, UpdateHz = 10f };
                default:
                    return new WeatherQuality { RainDrops = 3600, SnowFlakes = 2000, Splashes = 40, CloudLayers = 3, VolumetricClouds = true, PuddleRipples = true, WetSpecular = true, VertexWaves = true, ReflectionProbe = true, LightningLight = true, ShorelineFoam = true, UpdateHz = 20f };
            }
        }

        /// <summary>Planar reflections render the scene twice; only on high, and only when the player asks.</summary>
        public static bool PlanarReflections(QualityTier tier, WeatherSettings s) => tier == QualityTier.High && s != null && s.planarReflections;
    }

    /// <summary>One lightning strike: when it flashes, how far away, and when its thunder arrives.</summary>
    public struct Strike
    {
        public float Time, DistanceKm;
        public int Flickers;
        public float ThunderAt => Time + DistanceKm * 1000f / Lightning.SpeedOfSound;
        public bool Near => DistanceKm < 1.5f;
    }

    /// <summary>
    /// Lightning that respects "reduce flashing". Normal flashes stay within the common photosensitivity guidance
    /// (no more than three flashes in any second). With reduce flashing on there is no flash at all: the sky brightens
    /// a little over half a second and fades, and the thunder still plays.
    /// </summary>
    public static class Lightning
    {
        public const float SpeedOfSound = 343f;
        public const float MinGapSeconds = 4f;
        public const float ReducedPeak = 0.12f, NormalPeak = 2.5f;

        public static List<Strike> Plan(int seed, float seconds, float intensity)
        {
            var list = new List<Strike>();
            if (intensity <= 0f) return list;
            var rnd = new Random(seed);
            float t = 2f + (float)rnd.NextDouble() * 4f;
            while (t < seconds)
            {
                float d = 0.4f + (float)rnd.NextDouble() * 7f;
                list.Add(new Strike { Time = t, DistanceKm = d, Flickers = 1 + rnd.Next(2) });
                t += MinGapSeconds + (float)rnd.NextDouble() * 14f / Math.Max(0.2f, intensity);
            }
            return list;
        }

        /// <summary>Extra light (0 = none) at <paramref name="t"/> seconds after a strike.</summary>
        public static float Flash(Strike s, float t, bool reduceFlash)
        {
            if (t < 0f) return 0f;
            float near = s.Near ? 1f : 0.5f;
            if (reduceFlash)
            {
                // A slow swell: 0.5 s up, 1 s down, small.
                if (t < 0.5f) return ReducedPeak * near * (t / 0.5f);
                if (t < 1.5f) return ReducedPeak * near * (1f - (t - 0.5f));
                return 0f;
            }
            // Each flicker lasts 0.08 s, and flickers start 0.35 s apart: at most 3 in any second.
            for (int i = 0; i < s.Flickers; i++)
            {
                float start = i * 0.35f;
                if (t >= start && t < start + 0.08f) return NormalPeak * near * (i == 0 ? 1f : 0.6f);
            }
            return 0f;
        }
    }

    /// <summary>Ground gets wet within a minute of rain and dries over a few minutes after.</summary>
    public static class Wetness
    {
        public static float Step(float wet, float rain, float snow, float dt)
        {
            float target = Math.Max(rain > 0.05f ? 1f : 0f, snow > 0.05f ? 0.4f : 0f);
            float rate = target > wet ? (0.3f + rain) / 60f : 1f / 240f;
            float next = wet + Math.Sign(target - wet) * rate * dt;
            return target > wet ? Math.Min(next, target) : Math.Max(next, target);
        }
    }

    /// <summary>Gains for the ambience beds that sit under the rain and sand loops.</summary>
    public struct AmbienceBeds
    {
        public float Wind, Waves, Rain, HeavyRain, Sand;

        public static AmbienceBeds For(WeatherLook look, bool nearWater, bool underCover)
        {
            float cover = underCover ? 0.45f : 1f;
            return new AmbienceBeds
            {
                Wind = Math.Min(1f, 0.15f + 0.85f * look.Wind) * cover,
                Waves = nearWater ? 0.5f + 0.5f * look.Wind : 0f,
                Rain = Math.Min(1f, look.Rain * 1.6f) * cover,
                HeavyRain = Math.Max(0f, look.Rain - 0.6f) / 0.4f * cover,
                Sand = look.Dust * cover,
            };
        }

        /// <summary>The older rain and sand mixer still drives the rain and sand loops.</summary>
        public static Audio.Weather MixWeather(WeatherKind k)
        {
            switch (k)
            {
                case WeatherKind.Rain: return Audio.Weather.Rain;
                case WeatherKind.Thunderstorm: return Audio.Weather.HeavyRain;
                case WeatherKind.Sandstorm: return Audio.Weather.Sandstorm;
                default: return Audio.Weather.Clear;
            }
        }
    }
}
