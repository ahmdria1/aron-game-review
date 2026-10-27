// (c) Ahmed Alamrani. All rights reserved.
using System;
using System.Collections.Generic;
using Aron.Logic.Integrity;

namespace Aron.Logic.Config
{
    /// <summary>Things the server can switch off without a new version.</summary>
    public enum Feature { OnlineRaces, Store, Purchases, Events, ShopSigns, VehicleSea, VehicleCars, VehicleBicycles, VehicleMotorcycles, Analytics }

    /// <summary>
    /// Kill switches. Every switch is ON unless the server says otherwise, so a missing or broken answer never
    /// takes a feature away. Switching something off hides or blocks new use; it never removes what a player already owns.
    /// </summary>
    [Serializable]
    public class FeatureFlags
    {
        public bool onlineRaces = true;
        public bool store = true;
        /// <summary>New real-money purchases. Restoring earlier purchases always works.</summary>
        public bool purchases = true;
        public bool events = true;
        public bool shopSigns = true;
        /// <summary>Usage statistics. Even when true, nothing is collected without a grown-up's choice.</summary>
        public bool analytics = true;
        public bool vehicleSea = true;
        public bool vehicleCars = true;
        public bool vehicleBicycles = true;
        public bool vehicleMotorcycles = true;

        public bool IsOn(Feature f)
        {
            switch (f)
            {
                case Feature.OnlineRaces: return onlineRaces;
                case Feature.Store: return store;
                case Feature.Purchases: return purchases;
                case Feature.Events: return events;
                case Feature.ShopSigns: return shopSigns;
                case Feature.Analytics: return analytics;
                case Feature.VehicleSea: return vehicleSea;
                case Feature.VehicleCars: return vehicleCars;
                case Feature.VehicleBicycles: return vehicleBicycles;
                case Feature.VehicleMotorcycles: return vehicleMotorcycles;
                default: return true;
            }
        }

        /// <summary>The switch behind a shop tab id (sea, cars, bicycles, motorcycles). Every other tab is always on.</summary>
        public bool TabIsOn(string tabId)
        {
            switch (tabId)
            {
                case "sea": return vehicleSea;
                case "cars": return vehicleCars;
                case "bicycles": return vehicleBicycles;
                case "motorcycles": return vehicleMotorcycles;
                default: return true;
            }
        }
    }

    /// <summary>Small adjustments on top of difficulty.json. Limited to a band so a typo cannot make the game impossible.</summary>
    [Serializable]
    public class DifficultyTuning
    {
        public const float MinScale = 0.8f, MaxScale = 1.2f;
        public const int MinLives = -1, MaxLives = 2;
        public float enemySpeedScale = 1f;
        public float bossHealthScale = 1f;
        public int extraLivesBonus;

        /// <summary>A copy of the level with the tuning applied (limits re-checked here too).</summary>
        public DifficultyLevel Apply(DifficultyLevel level)
        {
            if (level == null) return null;
            return new DifficultyLevel
            {
                id = level.id,
                extraLives = Math.Max(0, level.extraLives + Clamp(extraLivesBonus, MinLives, MaxLives)),
                checkpointSeconds = level.checkpointSeconds,
                enemySpeed = level.enemySpeed * Clamp(enemySpeedScale, MinScale, MaxScale),
                bossHealth = level.bossHealth * Clamp(bossHealthScale, MinScale, MaxScale),
                mercyDeaths = level.mercyDeaths,
                bossExtraAttack = level.bossExtraAttack,
                assist = level.assist,
            };
        }

        internal static float Clamp(float v, float lo, float hi) => float.IsNaN(v) ? 1f : v < lo ? lo : v > hi ? hi : v;
        internal static int Clamp(int v, int lo, int hi) => v < lo ? lo : v > hi ? hi : v;
    }

    /// <summary>A timed bonus, for example a double-dates weekend. Times are Unix seconds (UTC).</summary>
    [Serializable]
    public class TimedEvent
    {
        public const float MaxMultiplier = 2f;
        public const long MaxDurationSeconds = 60L * 24 * 3600;
        public string id = "";
        public long startUnix;
        public long endUnix;
        /// <summary>Multiplier on dates picked up (1 = none, at most 2).</summary>
        public float datesMultiplier = 1f;
        /// <summary>Key of the short name shown in the menu; optional.</summary>
        public string nameKey = "";

        public bool IsActive(long nowUnix) => nowUnix >= startUnix && nowUnix < endUnix;
    }

    public static class PinNames
    {
        public const string Off = "off", ReportOnly = "reportOnly", Enforce = "enforce";
        public static bool IsKnown(string s) => s == Off || s == ReportOnly || s == Enforce;
    }

    /// <summary>The whole server-driven file. Every field has a safe default, so a partial file is fine.</summary>
    [Serializable]
    public class GameSettings
    {
        public const int MaxEvents = 8;
        /// <summary>Only a higher or equal number replaces what the phone already has.</summary>
        public int version;
        public FeatureFlags features = new FeatureFlags();
        public DifficultyTuning difficulty = new DifficultyTuning();
        public TimedEvent[] events = new TimedEvent[0];
        /// <summary>"reportOnly" or "enforce" (see <see cref="PinPolicy"/>). "off" is not accepted from the server.</summary>
        public string pinMode = PinNames.ReportOnly;

        public static GameSettings Defaults() => new GameSettings();

        public bool IsOn(Feature f) => features != null ? features.IsOn(f) : true;

        /// <summary>
        /// Dates multiplier right now. Events count only while the game's own clock is trusted, so changing the phone's
        /// date cannot start or stretch an event.
        /// </summary>
        public float DatesMultiplier(long nowUnix, bool clockTrusted)
        {
            if (!clockTrusted || !IsOn(Feature.Events) || events == null) return 1f;
            float best = 1f;
            foreach (var e in events) if (e != null && e.IsActive(nowUnix) && e.datesMultiplier > best) best = e.datesMultiplier;
            return Math.Min(best, TimedEvent.MaxMultiplier);
        }

        public List<TimedEvent> ActiveEvents(long nowUnix, bool clockTrusted)
        {
            var list = new List<TimedEvent>();
            if (!clockTrusted || !IsOn(Feature.Events) || events == null) return list;
            foreach (var e in events) if (e != null && e.IsActive(nowUnix)) list.Add(e);
            return list;
        }
    }

    public static class SettingsPolicy
    {
        /// <summary>A clean copy: limits applied, bad events dropped, unknown values replaced by defaults. Null gives the defaults.</summary>
        public static GameSettings Sanitize(GameSettings s)
        {
            if (s == null) return GameSettings.Defaults();
            var f = s.features ?? new FeatureFlags();
            var d = s.difficulty ?? new DifficultyTuning();
            var clean = new GameSettings
            {
                version = Math.Max(0, s.version),
                features = new FeatureFlags
                {
                    onlineRaces = f.onlineRaces, store = f.store, purchases = f.purchases, events = f.events, shopSigns = f.shopSigns, analytics = f.analytics,
                    vehicleSea = f.vehicleSea, vehicleCars = f.vehicleCars, vehicleBicycles = f.vehicleBicycles, vehicleMotorcycles = f.vehicleMotorcycles,
                },
                difficulty = new DifficultyTuning
                {
                    enemySpeedScale = DifficultyTuning.Clamp(d.enemySpeedScale, DifficultyTuning.MinScale, DifficultyTuning.MaxScale),
                    bossHealthScale = DifficultyTuning.Clamp(d.bossHealthScale, DifficultyTuning.MinScale, DifficultyTuning.MaxScale),
                    extraLivesBonus = DifficultyTuning.Clamp(d.extraLivesBonus, DifficultyTuning.MinLives, DifficultyTuning.MaxLives),
                },
                pinMode = s.pinMode == PinNames.Enforce ? PinNames.Enforce : PinNames.ReportOnly,
            };
            var seen = new HashSet<string>();
            var events = new List<TimedEvent>();
            if (s.events != null)
                foreach (var e in s.events)
                {
                    if (events.Count >= GameSettings.MaxEvents) break;
                    if (e == null || string.IsNullOrEmpty(e.id) || e.id.Length > 40 || !seen.Add(e.id)) continue;
                    if (e.startUnix <= 0 || e.endUnix <= e.startUnix || e.endUnix - e.startUnix > TimedEvent.MaxDurationSeconds) continue;
                    float m = float.IsNaN(e.datesMultiplier) ? 1f : e.datesMultiplier;
                    events.Add(new TimedEvent
                    {
                        id = e.id, startUnix = e.startUnix, endUnix = e.endUnix,
                        datesMultiplier = m < 1f ? 1f : m > TimedEvent.MaxMultiplier ? TimedEvent.MaxMultiplier : m,
                        nameKey = e.nameKey ?? "",
                    });
                }
            clean.events = events.ToArray();
            return clean;
        }
    }

    public enum SettingsSource { Defaults, Bundled, Cached, Remote }

    public readonly struct ResolvedSettings
    {
        public readonly GameSettings settings;
        public readonly SettingsSource source;
        public ResolvedSettings(GameSettings s, SettingsSource src) { settings = s; source = src; }
    }

    /// <summary>
    /// Picks the settings in force. Order: the server answer, then the last good answer kept on the phone, then the file shipped in the game,
    /// then the built-in defaults. An answer that does not parse, or is older than what the phone already has, is ignored.
    /// </summary>
    public static class SettingsResolver
    {
        public static ResolvedSettings Resolve(string bundledJson, string cachedJson, string remoteJson, Func<string, GameSettings> parse)
        {
            var best = Defaults();
            var source = SettingsSource.Defaults;
            var b = TryParse(bundledJson, parse);
            if (b != null) { best = b; source = SettingsSource.Bundled; }
            var c = TryParse(cachedJson, parse);
            if (c != null && c.version >= best.version) { best = c; source = SettingsSource.Cached; }
            var r = TryParse(remoteJson, parse);
            if (r != null && r.version >= best.version) { best = r; source = SettingsSource.Remote; }
            return new ResolvedSettings(best, source);
        }

        private static GameSettings Defaults() => GameSettings.Defaults();

        private static GameSettings TryParse(string json, Func<string, GameSettings> parse)
        {
            if (string.IsNullOrWhiteSpace(json) || parse == null) return null;
            try { var s = parse(json); return s == null ? null : SettingsPolicy.Sanitize(s); }
            catch (Exception) { return null; }
        }
    }

    /// <summary>
    /// Certificate pinning mode. The server may move between report-only and enforce (the escape hatch for a certificate that was
    /// rotated by mistake), but enforcing needs at least two pins, and "off" cannot be asked for from the server.
    /// </summary>
    public static class PinPolicy
    {
        public static PinMode Effective(PinMode shipped, string fromServer, CertPins pins)
        {
            if (fromServer == PinNames.Enforce) return pins != null && CertPins.SafeToEnforce(pins.Count) ? PinMode.Enforce : PinMode.ReportOnly;
            if (fromServer == PinNames.ReportOnly) return shipped == PinMode.Off ? PinMode.Off : PinMode.ReportOnly;
            return shipped;
        }
    }
}
