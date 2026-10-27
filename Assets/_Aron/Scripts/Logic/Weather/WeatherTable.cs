// (c) Ahmed Alamrani. All rights reserved.
using System;
using System.Collections.Generic;
using System.Globalization;

namespace Aron.Logic.Sky
{
    /// <summary>Every weather the sky system can show. Snow and sandstorms only appear where the tables allow them.</summary>
    public enum WeatherKind { Clear, Clouds, Haze, Rain, Thunderstorm, Fog, Sandstorm, Snow }

    public enum Season { Winter, Spring, Summer, Autumn }

    /// <summary>Relative chances of each weather. Any scale; only the ratios matter.</summary>
    [Serializable]
    public class WeatherWeights
    {
        public float clear, clouds, haze, rain, thunder, fog, sandstorm, snow;

        public float Of(WeatherKind k)
        {
            switch (k)
            {
                case WeatherKind.Clear: return clear;
                case WeatherKind.Clouds: return clouds;
                case WeatherKind.Haze: return haze;
                case WeatherKind.Rain: return rain;
                case WeatherKind.Thunderstorm: return thunder;
                case WeatherKind.Fog: return fog;
                case WeatherKind.Sandstorm: return sandstorm;
                default: return snow;
            }
        }

        public float Total => clear + clouds + haze + rain + thunder + fog + sandstorm + snow;

        /// <summary>The weather with the biggest share.</summary>
        public WeatherKind Most()
        {
            var best = WeatherKind.Clear;
            foreach (WeatherKind k in Enum.GetValues(typeof(WeatherKind))) if (Of(k) > Of(best)) best = k;
            return best;
        }

        public float Share(WeatherKind k) => Total <= 0f ? 0f : Of(k) / Total;
    }

    [Serializable]
    public class SeasonWeights
    {
        /// <summary>winter, spring, summer or autumn, as felt in that hemisphere.</summary>
        public string season;
        public WeatherWeights w = new WeatherWeights();
    }

    /// <summary>A month that does not follow its season, such as Japan's June rains or the Indian monsoon.</summary>
    [Serializable]
    public class MonthWeights
    {
        public int month;
        public WeatherWeights w = new WeatherWeights();
    }

    /// <summary>A region inside a country with its own climate (Abha's fog, snow on Jabal al-Lawz).</summary>
    [Serializable]
    public class RegionClimate
    {
        public string region;
        public float latitude, longitude;
        public SeasonWeights[] seasons = Array.Empty<SeasonWeights>();
        public MonthWeights[] months = Array.Empty<MonthWeights>();
        public string water = "", riverWater = "";
        /// <summary>0..1, how windy it usually is.</summary>
        public float wind = -1f;
    }

    [Serializable]
    public class CountryClimate
    {
        public string country;
        /// <summary>north or south. In the south July is winter.</summary>
        public string hemisphere = "north";
        /// <summary>desert, semi_arid, mediterranean, oceanic, temperate, continental, subtropical, tropical, monsoon or highland.</summary>
        public string climate;
        public float latitude, longitude;
        /// <summary>Hours ahead of UTC for the country's main city, used to place authored stage times.</summary>
        public float utcOffset;
        public SeasonWeights[] seasons = Array.Empty<SeasonWeights>();
        public MonthWeights[] months = Array.Empty<MonthWeights>();
        public RegionClimate[] regions = Array.Empty<RegionClimate>();
        /// <summary>Water profile ids: <see cref="water"/> for coasts, <see cref="riverWater"/> for rivers and marshes.</summary>
        public string water = "", riverWater = "";
        public float wind = 0.3f;
        /// <summary>Where the data comes from, in a few words.</summary>
        public string source = "";
    }

    /// <summary>How a sea, river or marsh looks.</summary>
    [Serializable]
    public class WaterProfile
    {
        public string id;
        /// <summary>sea, river or marsh.</summary>
        public string kind = "sea";
        public string shallow = "#3FA7B5", deep = "#0B3D5C", foam = "#F2F5F2";
        /// <summary>Metres, peak to trough, on the high tier.</summary>
        public float waveHeight = 0.3f;
        public float waveLength = 8f;
        /// <summary>Metres per second downstream; 0 for seas.</summary>
        public float flow;
        /// <summary>0 = muddy, 1 = glass clear.</summary>
        public float clarity = 0.5f;
    }

    /// <summary>Resources/Weather/weather.json.</summary>
    [Serializable]
    public class WeatherTable
    {
        /// <summary>Minutes between weather changes, shortest and longest.</summary>
        public float changeMinMinutes = 8f, changeMaxMinutes = 20f;
        /// <summary>Seconds one weather takes to blend into the next.</summary>
        public float blendSeconds = 30f;
        public CountryClimate[] countries = Array.Empty<CountryClimate>();
        public WaterProfile[] water = Array.Empty<WaterProfile>();

        public static readonly string[] Seasons = { "winter", "spring", "summer", "autumn" };
        public static readonly string[] Climates = { "desert", "semi_arid", "mediterranean", "oceanic", "temperate", "continental", "subtropical", "tropical", "monsoon", "highland" };
        public static readonly string[] WaterKinds = { "sea", "river", "marsh" };

        /// <summary>Countries where snow really falls in winter, at least in the hills the game shows.</summary>
        public static readonly string[] SnowCountries =
        {
            "lebanon", "turkey", "japan", "germany", "uk", "uzbekistan", "france", "spain", "portugal", "usa", "pakistan",
            "india", "syria", "jordan", "iraq", "morocco", "algeria", "south_africa",
        };

        /// <summary>Single regions with snow in a country that otherwise has none: Tabuk (Jabal al-Lawz).</summary>
        public static readonly string[] SnowRegions = { "tabuk" };

        public CountryClimate Country(string id)
        {
            foreach (var c in countries) if (c.country == id) return c;
            return null;
        }

        public WaterProfile Water(string id)
        {
            foreach (var w in water) if (w.id == id) return w;
            return null;
        }

        public static Season SeasonOf(int month, string hemisphere)
        {
            int m = hemisphere == "south" ? (month + 5) % 12 + 1 : month;
            if (m == 12 || m <= 2) return Season.Winter;
            if (m <= 5) return Season.Spring;
            if (m <= 8) return Season.Summer;
            return Season.Autumn;
        }

        /// <summary>
        /// The chances for a region in a month. The most specific entry wins: the region's month, the region's season,
        /// the country's month, then the country's season.
        /// </summary>
        public WeatherWeights Weights(string country, string region, int month)
        {
            var c = Country(country);
            if (c == null) return new WeatherWeights { clear = 1f };
            string season = Seasons[(int)SeasonOf(month, c.hemisphere)];
            RegionClimate r = null;
            foreach (var x in c.regions ?? Array.Empty<RegionClimate>()) if (x.region == region) r = x;
            return (r == null ? null : FindMonth(r.months, month) ?? FindSeason(r.seasons, season))
                ?? FindMonth(c.months, month) ?? FindSeason(c.seasons, season) ?? new WeatherWeights { clear = 1f };
        }

        /// <summary>The water profile for a stage: river terrain gets the river, everything else the sea.</summary>
        public string WaterFor(string country, string region, string terrain = "coast")
        {
            var c = Country(country);
            if (c == null) return "";
            bool river = terrain == "river";
            foreach (var x in c.regions ?? Array.Empty<RegionClimate>())
                if (x.region == region)
                {
                    string own = river ? x.riverWater : x.water;
                    if (!string.IsNullOrEmpty(own)) return own;
                }
            return (river ? c.riverWater : c.water) ?? "";
        }

        public float WindFor(string country, string region)
        {
            var c = Country(country);
            if (c == null) return 0.3f;
            foreach (var x in c.regions ?? Array.Empty<RegionClimate>()) if (x.region == region && x.wind >= 0f) return x.wind;
            return c.wind;
        }

        public void Place(string country, string region, out float latitude, out float longitude)
        {
            var c = Country(country);
            latitude = c?.latitude ?? 24.7f; longitude = c?.longitude ?? 46.7f;
            if (c == null) return;
            foreach (var x in c.regions ?? Array.Empty<RegionClimate>())
                if (x.region == region && (x.latitude != 0f || x.longitude != 0f)) { latitude = x.latitude; longitude = x.longitude; }
        }

        private static WeatherWeights FindMonth(MonthWeights[] list, int month)
        {
            if (list != null) foreach (var m in list) if (m.month == month) return m.w;
            return null;
        }

        private static WeatherWeights FindSeason(SeasonWeights[] list, string season)
        {
            if (list != null) foreach (var s in list) if (s.season == season) return s.w;
            return null;
        }

        /// <summary>
        /// Checks the file. <paramref name="desertRegions"/> maps every region id to its country and whether it has
        /// sand to blow (a desert or oasis stage); pass null to skip the cross-checks.
        /// </summary>
        public List<string> Validate(IDictionary<string, (string country, bool desert)> regions = null)
        {
            var e = new List<string>();
            if (changeMinMinutes < 2f || changeMaxMinutes < changeMinMinutes || changeMaxMinutes > 120f) e.Add("change minutes");
            if (blendSeconds < 5f || blendSeconds > 180f) e.Add("blend seconds 5..180");
            var waterIds = new HashSet<string>();
            foreach (var w in water)
            {
                string where = "water " + w.id;
                if (string.IsNullOrEmpty(w.id) || !waterIds.Add(w.id)) e.Add(where + ": missing or duplicate id");
                if (Array.IndexOf(WaterKinds, w.kind) < 0) e.Add(where + ": kind");
                if (!Hex(w.shallow) || !Hex(w.deep) || !Hex(w.foam)) e.Add(where + ": colours");
                if (w.waveHeight < 0f || w.waveHeight > 2f || w.waveLength < 1f || w.waveLength > 60f) e.Add(where + ": waves");
                if (w.kind == "sea" && w.flow != 0f) e.Add(where + ": the sea does not flow");
                if (w.kind == "river" && (w.flow <= 0f || w.flow > 3f)) e.Add(where + ": a river flows 0..3 m/s");
                if (w.kind != "sea" && w.waveHeight > 0.3f) e.Add(where + ": rivers and marshes have small waves");
                if (w.clarity < 0f || w.clarity > 1f) e.Add(where + ": clarity 0..1");
            }
            var ids = new HashSet<string>();
            foreach (var c in countries)
            {
                string where = "weather " + c.country;
                if (string.IsNullOrEmpty(c.country) || !ids.Add(c.country)) e.Add(where + ": missing or duplicate country");
                if (c.hemisphere != "north" && c.hemisphere != "south") e.Add(where + ": hemisphere north or south");
                if ((c.hemisphere == "south") != (c.latitude < 0f)) e.Add(where + ": hemisphere does not match the latitude");
                if (Math.Abs(c.latitude) > 70f || Math.Abs(c.longitude) > 180f) e.Add(where + ": latitude or longitude");
                if (c.utcOffset < -12f || c.utcOffset > 14f) e.Add(where + ": utc offset");
                if (Array.IndexOf(Climates, c.climate) < 0) e.Add(where + ": unknown climate " + c.climate);
                if (string.IsNullOrEmpty(c.source)) e.Add(where + ": say where the climate comes from");
                if (c.wind < 0f || c.wind > 1f) e.Add(where + ": wind 0..1");
                if (string.IsNullOrEmpty(c.water) || !waterIds.Contains(c.water)) e.Add(where + ": unknown sea " + c.water);
                if (string.IsNullOrEmpty(c.riverWater) || !waterIds.Contains(c.riverWater)) e.Add(where + ": unknown river " + c.riverWater);
                else if (Water(c.riverWater).kind == "sea") e.Add(where + ": the river water is a sea");
                Seasons4(e, where, c.seasons, false);
                Months(e, where, c.months);
                bool snowOk = Array.IndexOf(SnowCountries, c.country) >= 0;
                Allowed(e, where, c.seasons, c.months, c.hemisphere, snowOk);
                foreach (var r in c.regions ?? Array.Empty<RegionClimate>())
                {
                    string rw = where + " region " + r.region;
                    if (regions != null && (!regions.TryGetValue(r.region ?? "", out var info) || info.country != c.country)) e.Add(rw + ": not a region of " + c.country);
                    if (r.seasons.Length > 0) Seasons4(e, rw, r.seasons, true);
                    Months(e, rw, r.months);
                    Allowed(e, rw, r.seasons, r.months, c.hemisphere, snowOk || Array.IndexOf(SnowRegions, r.region) >= 0);
                    if (!string.IsNullOrEmpty(r.water) && !waterIds.Contains(r.water)) e.Add(rw + ": unknown water " + r.water);
                    if (!string.IsNullOrEmpty(r.riverWater) && !waterIds.Contains(r.riverWater)) e.Add(rw + ": unknown river " + r.riverWater);
                    if (r.wind > 1f) e.Add(rw + ": wind 0..1");
                    if ((r.latitude != 0f || r.longitude != 0f) && (r.latitude < 0f) != (c.hemisphere == "south")) e.Add(rw + ": latitude");
                }
                if (regions != null)
                    foreach (var kv in regions)
                    {
                        if (kv.Value.country != c.country || kv.Value.desert) continue;
                        for (int m = 1; m <= 12; m++)
                            if (Weights(c.country, kv.Key, m).sandstorm > 0f) { e.Add(where + " region " + kv.Key + ": no sand there, so no sandstorm"); break; }
                    }
            }
            if (regions != null)
            {
                var countriesNeeded = new HashSet<string>();
                foreach (var kv in regions) countriesNeeded.Add(kv.Value.country);
                foreach (var n in countriesNeeded) if (!ids.Contains(n)) e.Add("weather: no climate for " + n);
                foreach (var n in ids) if (!countriesNeeded.Contains(n)) e.Add("weather: " + n + " is not in the game");
            }
            return e;
        }

        private static void Seasons4(List<string> e, string where, SeasonWeights[] list, bool partial)
        {
            var seen = new HashSet<string>();
            foreach (var s in list ?? Array.Empty<SeasonWeights>())
            {
                if (Array.IndexOf(Seasons, s.season) < 0 || !seen.Add(s.season)) e.Add(where + ": season " + s.season);
                Weights(e, where + " " + s.season, s.w);
            }
            if (!partial && seen.Count != 4) e.Add(where + ": needs all four seasons");
        }

        private static void Months(List<string> e, string where, MonthWeights[] list)
        {
            var seen = new HashSet<int>();
            foreach (var m in list ?? Array.Empty<MonthWeights>())
            {
                if (m.month < 1 || m.month > 12 || !seen.Add(m.month)) e.Add(where + ": month " + m.month);
                Weights(e, where + " month " + m.month, m.w);
            }
        }

        private static void Weights(List<string> e, string where, WeatherWeights w)
        {
            if (w == null) { e.Add(where + ": no weights"); return; }
            foreach (WeatherKind k in Enum.GetValues(typeof(WeatherKind)))
            {
                float v = w.Of(k);
                if (float.IsNaN(v) || v < 0f || v > 100f) e.Add(where + ": " + k + " 0..100");
            }
            if (w.Total <= 0f) e.Add(where + ": weights add up to nothing");
            if (w.thunder > 0f && w.rain <= 0f) e.Add(where + ": thunder without rain");
        }

        // Snow only where it really falls, and only in winter (or a winter month).
        private static void Allowed(List<string> e, string where, SeasonWeights[] seasons, MonthWeights[] months, string hemisphere, bool snowOk)
        {
            foreach (var s in seasons ?? Array.Empty<SeasonWeights>())
                if (s.w != null && s.w.snow > 0f && (!snowOk || s.season != "winter")) e.Add(where + " " + s.season + ": no snow here then");
            foreach (var m in months ?? Array.Empty<MonthWeights>())
                if (m.w != null && m.w.snow > 0f && (!snowOk || SeasonOf(m.month, hemisphere) != Season.Winter)) e.Add(where + " month " + m.month + ": no snow here then");
        }

        private static bool Hex(string s)
            => s != null && s.Length == 7 && s[0] == '#' && int.TryParse(s.Substring(1), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out _);
    }
}
