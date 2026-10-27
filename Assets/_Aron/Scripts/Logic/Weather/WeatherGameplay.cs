// (c) Ahmed Alamrani. All rights reserved.
using System;
using System.Collections.Generic;

namespace Aron.Logic.Sky
{
    /// <summary>
    /// What weather does to play, as numbers (Resources/Config/weather_gameplay.json). Sandstorm: lower visibility (never so low that a hazard
    /// hides) and a gusting wind that leans on the hero. Rain: slippery ground and puddles that slow. Heat: in the sun on a hot day he slows
    /// down a little, and shade brings it back. Nothing hurts; every effect is mild and has a way out.
    /// </summary>
    [Serializable]
    public class WeatherGameplayFile
    {
        /// <summary>Metres you can see at full dust or fog, and the floor: hazards and the path ahead stay readable.</summary>
        public float fullDustVisibility = 24f, minVisibility = 14f, clearVisibility = 120f;
        /// <summary>Strongest wind push in m/s (sideways, gusting), and how fast gusts come and go.</summary>
        public float maxWindPush = 1.8f, gustPeriodSeconds = 6f, ridingWindShare = 0.5f, reduceMotionWindShare = 0.4f;
        /// <summary>Wind below this does nothing at all.</summary>
        public float windThreshold = 0.55f;
        /// <summary>Ground grip (1 is dry): at full wetness, and on snow.</summary>
        public float wetTraction = 0.55f, snowTraction = 0.7f, puddleTraction = 0.4f;
        /// <summary>Speed share while standing in a puddle.</summary>
        public float puddleSpeed = 0.7f;
        /// <summary>Heat: seconds in full sun to reach full stress, the slowest speed share, seconds in shade to recover, and the stress that shows the hint.</summary>
        public float heatBuildSeconds = 30f, heatMinSpeed = 0.85f, heatRecoverSeconds = 6f, heatHintAt = 0.6f;
        public ClimateHeat[] climates =
        {
            new ClimateHeat { climate = "desert", heat = 1f }, new ClimateHeat { climate = "semi_arid", heat = 0.8f },
            new ClimateHeat { climate = "tropical", heat = 0.6f }, new ClimateHeat { climate = "subtropical", heat = 0.5f },
            new ClimateHeat { climate = "monsoon", heat = 0.5f },
        };

        public float HeatOf(string climate)
        {
            foreach (var c in climates) if (c.climate == climate) return c.heat;
            return 0f;
        }

        public List<string> Validate()
        {
            var e = new List<string>();
            if (minVisibility < 10f) e.Add("visibility must stay at least 10 m so hazards never hide");
            if (fullDustVisibility < minVisibility || fullDustVisibility > clearVisibility) e.Add("visibility order");
            if (maxWindPush < 0f || maxWindPush > 2.5f) e.Add("wind push 0..2.5 m/s");
            if (wetTraction < 0.3f || wetTraction > 1f || puddleTraction < 0.25f) e.Add("traction floors (slippery, never ice)");
            if (puddleSpeed < 0.5f || puddleSpeed > 1f) e.Add("puddle speed 0.5..1");
            if (heatMinSpeed < 0.75f || heatMinSpeed > 1f) e.Add("heat slows to 75..100 %");
            if (heatBuildSeconds < 15f || heatRecoverSeconds <= 0f || heatRecoverSeconds > heatBuildSeconds) e.Add("heat builds slowly and recovers faster");
            foreach (var c in climates) if (Array.IndexOf(WeatherTable.Climates, c.climate) < 0) e.Add("unknown climate " + c.climate);
            return e;
        }
    }

    [Serializable]
    public class ClimateHeat { public string climate; public float heat; }

    public static class WeatherGameplay
    {
        /// <summary>Wind and heat only act on story and free-play stages. Races, time trials and bosses stay fair: the same for everyone.</summary>
        public static bool Applies(PlayMode mode) => mode == PlayMode.Story || mode == PlayMode.FreePlay;

        public static bool Daylight(DayPhase p) => p == DayPhase.Morning || p == DayPhase.Noon || p == DayPhase.Asr;

        /// <summary>How far the hero can see, in metres.</summary>
        public static float Visibility(WeatherGameplayFile f, WeatherLook look)
        {
            float murk = Math.Max(look.Dust, look.Fog * 0.8f);
            murk = murk < 0f ? 0f : murk > 1f ? 1f : murk;
            return f.clearVisibility + (f.fullDustVisibility - f.clearVisibility) * murk;
        }

        /// <summary>Sideways push in m/s (negative left, positive right): a slow gust wave on top of a steady lean, nothing in light wind.</summary>
        public static float WindPush(WeatherGameplayFile f, WeatherLook look, double time, bool riding, bool reduceMotion)
        {
            if (look.Wind <= f.windThreshold) return 0f;
            float strength = (look.Wind - f.windThreshold) / (1f - f.windThreshold);
            double wave = Math.Sin(time * 2.0 * Math.PI / f.gustPeriodSeconds);
            float gust = 0.6f + 0.4f * (float)wave;
            float push = f.maxWindPush * strength * gust;
            if (riding) push *= f.ridingWindShare;
            if (reduceMotion) push *= f.reduceMotionWindShare;
            return push;
        }

        /// <summary>Grip on the ground: dry 1, wet down to wetTraction, snow, and a puddle's own low value.</summary>
        public static float Traction(WeatherGameplayFile f, WeatherLook look, bool inPuddle)
        {
            float t = 1f - (1f - f.wetTraction) * Clamp01(look.Wetness);
            if (look.Snow > 0.3f) t = Math.Min(t, f.snowTraction);
            if (inPuddle) t = Math.Min(t, f.puddleTraction);
            return t;
        }

        public static float SpeedInPuddle(WeatherGameplayFile f, bool inPuddle) => inPuddle ? f.puddleSpeed : 1f;

        /// <summary>Hot, sunny and dry: where the heat rule applies at all (0 when it does not, else the climate's strength).</summary>
        public static float HeatStrength(WeatherGameplayFile f, string climate, WeatherLook look, bool daylight)
        {
            if (!daylight || look.Rain > 0.05f || look.Snow > 0.05f || look.CloudCover > 0.6f) return 0f;
            return f.HeatOf(climate) * (1f - look.CloudCover * 0.5f);
        }

        private static float Clamp01(float v) => v < 0f ? 0f : v > 1f ? 1f : v;
    }

    /// <summary>How hot he has got (0..1): builds in the sun on a hot day, drains in shade. Slows him a little, never more.</summary>
    public class HeatStress
    {
        private readonly WeatherGameplayFile f;
        public float Level { get; private set; }
        public HeatStress(WeatherGameplayFile file) { f = file ?? new WeatherGameplayFile(); }

        public void Tick(float dt, float heatStrength, bool inShade, bool drinking)
        {
            if (heatStrength <= 0f || inShade || drinking) Level = Math.Max(0f, Level - dt / f.heatRecoverSeconds);
            else Level = Math.Min(heatStrength, Level + dt * heatStrength / f.heatBuildSeconds);
        }

        /// <summary>Speed share, from 1 down to heatMinSpeed.</summary>
        public float SpeedScale => 1f - (1f - f.heatMinSpeed) * Level;

        /// <summary>Show the "find some shade" hint.</summary>
        public bool ShowHint => Level >= f.heatHintAt;

        public void Reset() => Level = 0f;
    }
}
