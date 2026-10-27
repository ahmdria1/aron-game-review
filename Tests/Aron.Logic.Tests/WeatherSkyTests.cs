// (c) Ahmed Alamrani. All rights reserved.
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using Aron.Logic.Races;
using Aron.Logic.Regions;
using Aron.Logic.Sky;
using NUnit.Framework;

namespace Aron.Logic.Tests
{
    /// <summary>Weather tables, the moon and sun, and who gets to decide the weather in each mode.</summary>
    public class WeatherSkyTests
    {
        private static readonly JsonSerializerOptions Opt = new JsonSerializerOptions { IncludeFields = true };

        private static string Resources()
        {
            for (var d = new DirectoryInfo(TestContext.CurrentContext.TestDirectory); d != null; d = d.Parent)
            {
                var p = Path.Combine(d.FullName, "Assets", "_Aron", "Resources");
                if (Directory.Exists(Path.Combine(p, "Regions"))) return p;
            }
            Assert.Fail("Resources not found");
            return null;
        }

        private static WeatherTable Table() => JsonSerializer.Deserialize<WeatherTable>(File.ReadAllText(Path.Combine(Resources(), "Weather", "weather.json")), Opt);

        private static List<RegionDef> Regions() => Directory.GetFiles(Path.Combine(Resources(), "Regions"), "*.json")
            .Where(f => Path.GetFileName(f) != "RegionCatalog.json")
            .Select(f => JsonSerializer.Deserialize<RegionDef>(File.ReadAllText(f), Opt)).ToList();

        private static Dictionary<string, (string country, bool desert)> RegionMap()
            => Regions().ToDictionary(r => r.id, r => (r.country, r.stages.Any(s => s.terrain == "desert" || s.terrain == "oasis")));

        private static DateTime Utc(int y, int mo, int d, int h, int mi) => new DateTime(y, mo, d, h, mi, 0, DateTimeKind.Utc);

        private static float Share(WeatherWeights w, params WeatherKind[] k) => k.Sum(x => w.Share(x));

        // ---- Tables ----

        [Test]
        public void WeatherTablePassesItsChecksForAllThirtyFiveCountries()
        {
            var t = Table();
            var errors = t.Validate(RegionMap());
            Assert.That(errors, Is.Empty, string.Join("\n", errors));
            Assert.That(t.countries.Length, Is.EqualTo(35));
        }

        [Test]
        public void EveryRegionGetsSensibleWeightsInEveryMonth()
        {
            var t = Table();
            foreach (var kv in RegionMap())
                for (int m = 1; m <= 12; m++)
                {
                    var w = t.Weights(kv.Value.country, kv.Key, m);
                    Assert.That(w.Total, Is.GreaterThan(0f), kv.Key + " month " + m);
                    Assert.That(w.Share(WeatherKind.Clear) + w.Share(WeatherKind.Clouds), Is.GreaterThan(0.2f), kv.Key + " month " + m + ": some calm skies every month");
                }
        }

        [Test]
        public void GulfSummersAreClearAndHazy()
        {
            var t = Table();
            foreach (var (c, r) in new[] { ("saudi", "riyadh"), ("saudi", "eastern"), ("kuwait", "kuwait"), ("uae", "uae"), ("qatar", "qatar"), ("bahrain", "bahrain") })
                for (int m = 6; m <= 8; m++)
                {
                    var w = t.Weights(c, r, m);
                    Assert.That(Share(w, WeatherKind.Clear, WeatherKind.Haze), Is.GreaterThanOrEqualTo(0.75f), r + " month " + m);
                    Assert.That(w.rain + w.snow, Is.EqualTo(0f), r + " has no summer rain in the table");
                }
        }

        [Test]
        public void AbhaIsFoggyAndHasNoSandstorms()
        {
            var t = Table();
            for (int m = 1; m <= 12; m++)
            {
                var asir = t.Weights("saudi", "asir", m);
                Assert.That(asir.Share(WeatherKind.Fog), Is.GreaterThanOrEqualTo(0.15f), "month " + m);
                Assert.That(asir.fog, Is.GreaterThan(t.Weights("saudi", "riyadh", m).fog));
                Assert.That(asir.sandstorm, Is.EqualTo(0f));
                Assert.That(t.Weights("saudi", "bahah", m).sandstorm + t.Weights("saudi", "jazan", m).sandstorm, Is.EqualTo(0f));
            }
        }

        [Test]
        public void JapanJuneIsTheRainySeasonAndIndiaHasItsMonsoon()
        {
            var t = Table();
            Assert.That(t.Weights("japan", "japan", 6).Most(), Is.EqualTo(WeatherKind.Rain));
            Assert.That(t.Weights("japan", "japan", 6).Share(WeatherKind.Rain), Is.GreaterThan(t.Weights("japan", "japan", 5).Share(WeatherKind.Rain)));
            foreach (int m in new[] { 7, 8 }) Assert.That(t.Weights("india", "india", m).Most(), Is.EqualTo(WeatherKind.Rain), "india " + m);
            Assert.That(t.Weights("india", "india", 1).rain, Is.LessThan(t.Weights("india", "india", 7).rain));
        }

        [Test]
        public void SouthernHemisphereSeasonsAreFlipped()
        {
            Assert.That(WeatherTable.SeasonOf(7, "south"), Is.EqualTo(Season.Winter));
            Assert.That(WeatherTable.SeasonOf(1, "south"), Is.EqualTo(Season.Summer));
            Assert.That(WeatherTable.SeasonOf(4, "south"), Is.EqualTo(Season.Autumn));
            Assert.That(WeatherTable.SeasonOf(10, "south"), Is.EqualTo(Season.Spring));
            Assert.That(WeatherTable.SeasonOf(1, "north"), Is.EqualTo(Season.Winter));
            var t = Table();
            foreach (var c in new[] { "brazil", "south_africa", "indonesia" })
            {
                Assert.That(t.Country(c).hemisphere, Is.EqualTo("south"), c);
                Assert.That(t.Weights(c, c, 1).Share(WeatherKind.Rain), Is.GreaterThan(t.Weights(c, c, 7).Share(WeatherKind.Rain)), c + ": wet in the southern summer");
            }
            Assert.That(t.Weights("south_africa", "south_africa", 7).snow, Is.GreaterThan(0f), "Drakensberg snow in July");
            Assert.That(t.Weights("south_africa", "south_africa", 1).snow, Is.EqualTo(0f));
        }

        [Test]
        public void SnowOnlyWhereAndWhenItReallyFalls()
        {
            var t = Table();
            Assert.That(t.Weights("saudi", "tabuk", 1).snow, Is.GreaterThan(0f), "Jabal al-Lawz in winter");
            Assert.That(t.Weights("saudi", "tabuk", 7).snow, Is.EqualTo(0f));
            foreach (var c in new[] { "lebanon", "turkey", "japan", "germany", "uk", "uzbekistan" })
                Assert.That(t.Weights(c, c, 1).snow, Is.GreaterThan(0f), c + " gets winter snow");
            foreach (var kv in RegionMap())
            {
                bool may = Array.IndexOf(WeatherTable.SnowCountries, kv.Value.country) >= 0 || Array.IndexOf(WeatherTable.SnowRegions, kv.Key) >= 0;
                for (int m = 1; m <= 12; m++)
                {
                    float snow = t.Weights(kv.Value.country, kv.Key, m).snow;
                    if (!may) Assert.That(snow, Is.EqualTo(0f), kv.Key + " month " + m);
                    if (snow > 0f) Assert.That(WeatherTable.SeasonOf(m, t.Country(kv.Value.country).hemisphere), Is.EqualTo(Season.Winter), kv.Key + " month " + m);
                }
            }
        }

        [Test]
        public void SandstormsOnlyBlowWhereThereIsSand()
        {
            var t = Table();
            foreach (var kv in RegionMap().Where(kv => !kv.Value.desert))
                for (int m = 1; m <= 12; m++)
                    Assert.That(t.Weights(kv.Value.country, kv.Key, m).sandstorm, Is.EqualTo(0f), kv.Key);
            Assert.That(t.Weights("kuwait", "kuwait", 6).sandstorm, Is.GreaterThan(0f), "summer shamal");
            Assert.That(t.Weights("sudan", "sudan", 7).sandstorm, Is.GreaterThan(0f), "haboob season");
        }

        [Test]
        public void RiversAndSeasGetTheRightWater()
        {
            var t = Table();
            Assert.That(t.WaterFor("egypt", "egypt", "river"), Is.EqualTo("nile"));
            Assert.That(t.WaterFor("sudan", "sudan", "river"), Is.EqualTo("nile"));
            Assert.That(t.WaterFor("brazil", "brazil", "river"), Is.EqualTo("amazon"));
            Assert.That(t.WaterFor("iraq", "iraq", "river"), Is.EqualTo("iraq_marshes"));
            Assert.That(t.WaterFor("saudi", "eastern", "coast"), Is.EqualTo("gulf"));
            Assert.That(t.WaterFor("saudi", "makkah", "coast"), Is.EqualTo("red_sea"));
            foreach (var r in Regions())
                foreach (var s in r.stages.Where(s => s.terrain == "river" || s.terrain == "coast"))
                {
                    var w = t.Water(t.WaterFor(r.country, r.id, s.terrain));
                    Assert.That(w, Is.Not.Null, r.id + " " + s.id);
                    if (s.terrain == "river") Assert.That(w.kind, Is.Not.EqualTo("sea"), r.id + " " + s.id);
                }
        }

        // ---- Schedule ----

        [Test]
        public void ScheduleIsTheSameForEveryoneAndBlendsSmoothly()
        {
            var t = Table();
            var at = Utc(2026, 1, 10, 9, 0);
            var a = WeatherSchedule.At(t, "uk", "uk", at);
            var b = WeatherSchedule.At(t, "uk", "uk", at);
            Assert.That(a.From, Is.EqualTo(b.From)); Assert.That(a.To, Is.EqualTo(b.To)); Assert.That(a.Blend, Is.EqualTo(b.Blend));
            // Sampled every 2 seconds the look never jumps.
            var prev = WeatherSchedule.At(t, "uk", "uk", at).Look;
            for (int s = 2; s < 6 * 3600; s += 2)
            {
                var now = WeatherSchedule.At(t, "uk", "uk", at.AddSeconds(s)).Look;
                Assert.That(Math.Abs(now.CloudCover - prev.CloudCover) + Math.Abs(now.Rain - prev.Rain) + Math.Abs(now.Fog - prev.Fog), Is.LessThan(0.25f), "second " + s);
                prev = now;
            }
        }

        [Test]
        public void SchedulePicksFollowTheTable()
        {
            var t = Table();
            int calm = 0, total = 0, rain = 0, jTotal = 0;
            for (int d = 0; d < 120; d++)
                for (int h = 0; h < 24; h += 2)
                {
                    var july = WeatherSchedule.At(t, "saudi", "riyadh", Utc(2026, 7, 1, h, 7).AddDays(d % 31));
                    total++; if (july.Main == WeatherKind.Clear || july.Main == WeatherKind.Haze) calm++;
                    var june = WeatherSchedule.At(t, "japan", "japan", Utc(2026, 6, 1, h, 7).AddDays(d % 29));
                    jTotal++; if (june.Main == WeatherKind.Rain) rain++;
                }
            Assert.That(calm / (double)total, Is.GreaterThan(0.75));
            Assert.That(rain / (double)jTotal, Is.GreaterThan(0.3));
            var w = new WeatherWeights { clear = 1f, rain = 1f };
            Assert.That(WeatherSchedule.Pick(w, 0.2), Is.EqualTo(WeatherKind.Clear));
            Assert.That(WeatherSchedule.Pick(w, 0.8), Is.EqualTo(WeatherKind.Rain));
        }

        // ---- Modes ----

        [Test]
        public void RacesAndTimeTrialsAlwaysUseTheTracksFixedWeather()
        {
            var storm = new WeatherState { From = WeatherKind.Thunderstorm, To = WeatherKind.Sandstorm, Blend = 0.5f, Dynamic = true };
            var on = new WeatherSettings { dynamicWeather = true, freePlayEffects = true };
            var file = JsonSerializer.Deserialize<RaceFile>(File.ReadAllText(Path.Combine(Resources(), "Races", "tracks.json")), Opt);
            Assert.That(file.tracks.Length, Is.EqualTo(35));
            foreach (var track in file.tracks)
            {
                Assert.That(WeatherPolicy.RaceWeathers, Does.Contain(track.weather), track.id);
                foreach (var mode in new[] { PlayMode.Race, PlayMode.TimeTrial })
                {
                    var w = WeatherPolicy.Resolve(mode, on, "rain", track.weather, storm);
                    Assert.That(w.Dynamic, Is.False, track.id);
                    Assert.That(w.From, Is.EqualTo(w.To));
                    Assert.That(WeatherSchedule.TryParse(track.weather, out var k), Is.True);
                    Assert.That(w.Main, Is.EqualTo(k), track.id);
                    Assert.That(w.Look.Lightning, Is.EqualTo(0f));
                    Assert.That(WeatherPolicy.Traction(mode, on, w), Is.EqualTo(1f));
                }
            }
            Assert.That(file.tracks.Where(x => x.kind == "ski").All(x => x.weather == "snow"));
            Assert.That(WeatherPolicy.Fixed(PlayMode.Race) && WeatherPolicy.Fixed(PlayMode.TimeTrial) && WeatherPolicy.Fixed(PlayMode.Boss));
            Assert.That(file.Validate(), Is.Empty);
        }

        [Test]
        public void RaceValidationRejectsStormyTracks()
        {
            var file = JsonSerializer.Deserialize<RaceFile>(File.ReadAllText(Path.Combine(Resources(), "Races", "tracks.json")), Opt);
            file.tracks[0].weather = "thunderstorm";
            Assert.That(file.Validate().Any(e => e.Contains("fixed weather")));
            file.tracks[0].weather = "snow";
            Assert.That(file.Validate().Any(e => e.Contains("only ski")), Is.EqualTo(file.tracks[0].kind != "ski"));
        }

        [Test]
        public void BossesKeepTheirAuthoredWeather()
        {
            var storm = new WeatherState { From = WeatherKind.Thunderstorm, To = WeatherKind.Thunderstorm, Blend = 1f, Dynamic = true };
            var on = new WeatherSettings();
            Assert.That(WeatherPolicy.Resolve(PlayMode.Boss, on, "clear", "", storm).Main, Is.EqualTo(WeatherKind.Clear));
            Assert.That(WeatherPolicy.Resolve(PlayMode.Boss, on, "sandstorm", "", storm).Main, Is.EqualTo(WeatherKind.Sandstorm));
            Assert.That(WeatherPolicy.Resolve(PlayMode.Boss, on, "rain", "", storm).Dynamic, Is.False);
        }

        [Test]
        public void StoryWeatherIsCosmeticAndGentleAndCanBeTurnedOff()
        {
            var storm = new WeatherState { From = WeatherKind.Thunderstorm, To = WeatherKind.Sandstorm, Blend = 0.2f, Dynamic = true };
            var on = new WeatherSettings();
            var story = WeatherPolicy.Resolve(PlayMode.Story, on, "clear", "", storm);
            Assert.That(story.From, Is.EqualTo(WeatherKind.Rain));
            Assert.That(story.To, Is.EqualTo(WeatherKind.Haze));
            Assert.That(WeatherPolicy.Traction(PlayMode.Story, new WeatherSettings { freePlayEffects = true }, story), Is.EqualTo(1f));
            Assert.That(WeatherPolicy.Resolve(PlayMode.Story, on, "rain", "", storm).Main, Is.EqualTo(WeatherKind.Rain), "authored rain stays");
            var off = new WeatherSettings { dynamicWeather = false };
            Assert.That(WeatherPolicy.Resolve(PlayMode.Story, off, "clear", "", storm).Main, Is.EqualTo(WeatherKind.Clear));
            Assert.That(WeatherPolicy.Resolve(PlayMode.FreePlay, off, "clear", "", storm).Dynamic, Is.False);
        }

        [Test]
        public void FreePlayEffectsAreLightAndOptIn()
        {
            var rain = new WeatherState { From = WeatherKind.Rain, To = WeatherKind.Rain, Blend = 1f, Dynamic = true };
            var snow = new WeatherState { From = WeatherKind.Snow, To = WeatherKind.Snow, Blend = 1f, Dynamic = true };
            var defaults = new WeatherSettings();
            Assert.That(defaults.dynamicWeather, Is.True);
            Assert.That(defaults.freePlayEffects, Is.False);
            Assert.That(WeatherPolicy.Traction(PlayMode.FreePlay, defaults, WeatherPolicy.Resolve(PlayMode.FreePlay, defaults, "clear", "", rain)), Is.EqualTo(1f));
            var fx = new WeatherSettings { freePlayEffects = true };
            float r = WeatherPolicy.Traction(PlayMode.FreePlay, fx, WeatherPolicy.Resolve(PlayMode.FreePlay, fx, "clear", "", rain));
            float s = WeatherPolicy.Traction(PlayMode.FreePlay, fx, WeatherPolicy.Resolve(PlayMode.FreePlay, fx, "clear", "", snow));
            Assert.That(r, Is.InRange(0.8f, 0.95f));
            Assert.That(s, Is.InRange(0.75f, 0.95f));
        }

        // ---- Tiers ----

        [Test]
        public void HigherTiersNeverDoLess()
        {
            var lo = WeatherQuality.For(QualityTier.Low);
            var mid = WeatherQuality.For(QualityTier.Medium);
            var hi = WeatherQuality.For(QualityTier.High);
            Assert.That(lo.RainDrops, Is.LessThan(mid.RainDrops)); Assert.That(mid.RainDrops, Is.LessThan(hi.RainDrops));
            Assert.That(lo.SnowFlakes, Is.LessThan(mid.SnowFlakes)); Assert.That(mid.SnowFlakes, Is.LessThan(hi.SnowFlakes));
            Assert.That(lo.Splashes, Is.EqualTo(0)); Assert.That(mid.Splashes, Is.LessThan(hi.Splashes));
            Assert.That(lo.VertexWaves || lo.VolumetricClouds || lo.PuddleRipples || lo.ReflectionProbe, Is.False);
            Assert.That(hi.VertexWaves && hi.VolumetricClouds && hi.PuddleRipples && hi.ReflectionProbe);
            Assert.That(hi.RainDrops, Is.LessThanOrEqualTo(4096), "one draw call of quads");
            Assert.That(WeatherQuality.PlanarReflections(QualityTier.Medium, new WeatherSettings { planarReflections = true }), Is.False);
            Assert.That(WeatherQuality.PlanarReflections(QualityTier.High, new WeatherSettings { planarReflections = true }), Is.True);
            Assert.That(WeatherQuality.PlanarReflections(QualityTier.High, new WeatherSettings()), Is.False);
        }

        // ---- Lightning ----

        [Test]
        public void LightningRespectsReduceFlashing()
        {
            var strikes = Lightning.Plan(7, 600f, 1f);
            Assert.That(strikes.Count, Is.GreaterThan(5));
            for (int i = 1; i < strikes.Count; i++) Assert.That(strikes[i].Time - strikes[i - 1].Time, Is.GreaterThanOrEqualTo(Lightning.MinGapSeconds));
            foreach (var s in strikes)
            {
                Assert.That(s.ThunderAt - s.Time, Is.EqualTo(s.DistanceKm * 1000f / 343f).Within(0.001f), "thunder after the flash, at the speed of sound");
                // Normal: count separate flashes in any one-second window, sampled at 200 Hz.
                int flashes = 0; bool lit = false;
                for (float t = 0f; t < 1f; t += 0.005f)
                {
                    bool now = Lightning.Flash(s, t, false) > 0f;
                    if (now && !lit) flashes++;
                    lit = now;
                }
                Assert.That(flashes, Is.InRange(1, 3));
                // Reduced: small, slow, no sudden jump.
                float peak = 0f, prev = 0f;
                for (float t = 0f; t < 3f; t += 0.01f)
                {
                    float v = Lightning.Flash(s, t, true);
                    Assert.That(Math.Abs(v - prev), Is.LessThan(0.01f), "no sudden change");
                    peak = Math.Max(peak, v); prev = v;
                }
                Assert.That(peak, Is.LessThanOrEqualTo(0.15f));
                Assert.That(Lightning.Flash(s, 0.1f, true), Is.LessThan(peak * 0.5f + 1e-6f), "ramps up over at least 0.3 s");
            }
            Assert.That(Lightning.Plan(1, 600f, 0f), Is.Empty);
        }

        [Test]
        public void GroundWetsInAMinuteAndDriesSlowly()
        {
            float wet = 0f;
            for (int i = 0; i < 60; i++) wet = Wetness.Step(wet, 1f, 0f, 1f);
            Assert.That(wet, Is.GreaterThanOrEqualTo(0.95f));
            for (int i = 0; i < 60; i++) wet = Wetness.Step(wet, 0f, 0f, 1f);
            Assert.That(wet, Is.InRange(0.6f, 0.9f), "still damp a minute later");
            for (int i = 0; i < 300; i++) wet = Wetness.Step(wet, 0f, 0f, 1f);
            Assert.That(wet, Is.EqualTo(0f));
        }

        [Test]
        public void AmbienceFollowsTheWeather()
        {
            var storm = AmbienceBeds.For(WeatherLook.Of(WeatherKind.Thunderstorm), true, false);
            var calm = AmbienceBeds.For(WeatherLook.Of(WeatherKind.Clear), false, false);
            Assert.That(storm.Wind, Is.GreaterThan(calm.Wind));
            Assert.That(storm.HeavyRain, Is.GreaterThan(0.9f));
            Assert.That(storm.Waves, Is.GreaterThan(0f)); Assert.That(calm.Waves, Is.EqualTo(0f));
            Assert.That(AmbienceBeds.For(WeatherLook.Of(WeatherKind.Rain), false, true).Rain, Is.LessThan(AmbienceBeds.For(WeatherLook.Of(WeatherKind.Rain), false, false).Rain));
            Assert.That(AmbienceBeds.MixWeather(WeatherKind.Thunderstorm), Is.EqualTo(Audio.Weather.HeavyRain));
            Assert.That(AmbienceBeds.MixWeather(WeatherKind.Sandstorm), Is.EqualTo(Audio.Weather.Sandstorm));
            Assert.That(AmbienceBeds.MixWeather(WeatherKind.Fog), Is.EqualTo(Audio.Weather.Clear));
        }

        // ---- Moon and sun ----

        [TestCase(2024, 4, 8, 18, 21)]
        [TestCase(2026, 2, 17, 12, 1)]
        public void NewMoonsAreDark(int y, int mo, int d, int h, int mi)
        {
            var m = Astronomy.Moon(Utc(y, mo, d, h, mi));
            Assert.That(m.Illumination, Is.LessThan(0.01));
            Assert.That(m.Name, Is.EqualTo("new"));
        }

        [TestCase(2024, 9, 18, 2, 34)]
        [TestCase(2026, 3, 3, 11, 38)]
        public void FullMoonsAreFull(int y, int mo, int d, int h, int mi)
        {
            var m = Astronomy.Moon(Utc(y, mo, d, h, mi));
            Assert.That(m.Illumination, Is.GreaterThan(0.99));
            Assert.That(m.Name, Is.EqualTo("full"));
        }

        [Test]
        public void QuartersAreHalfLitOnTheRightSide()
        {
            var first = Astronomy.Moon(Utc(2025, 1, 6, 23, 56));
            Assert.That(first.Illumination, Is.InRange(0.45, 0.55));
            Assert.That(first.Waxing, Is.True);
            Assert.That(first.Name, Is.EqualTo("first_quarter"));
            var last = Astronomy.Moon(Utc(2024, 1, 4, 3, 30));
            Assert.That(last.Illumination, Is.InRange(0.45, 0.55));
            Assert.That(last.Waxing, Is.False);
            Assert.That(last.Name, Is.EqualTo("last_quarter"));
            Assert.That(first.AgeDays, Is.InRange(6.5, 8.5));
        }

        [Test]
        public void RamadanCrescentOverRiyadhIsAThinWaxingBowl()
        {
            // Evening of 1 March 2025 (Ramadan 1446 began 1 March in Saudi Arabia), just after sunset in Riyadh.
            var t = Utc(2025, 3, 1, 15, 15);
            var m = Astronomy.Moon(t);
            Assert.That(m.Waxing, Is.True);
            Assert.That(m.Illumination, Is.InRange(0.002, 0.05));
            Assert.That(m.Name, Is.EqualTo("new").Or.EqualTo("waxing_crescent"));
            var moon = Astronomy.MoonPosition(t, 24.71, 46.68);
            var sun = Astronomy.Sun(t, 24.71, 46.68);
            Assert.That(sun.Altitude, Is.InRange(-6.0, 1.0), "around sunset");
            Assert.That(moon.Altitude, Is.GreaterThan(sun.Altitude), "the crescent sits above the setting sun");
            Assert.That(moon.Azimuth, Is.InRange(230.0, 300.0), "low in the west");
            double limb = Astronomy.BrightLimbFromUp(t, 24.71, 46.68);
            Assert.That(Math.Abs(limb - 180.0), Is.LessThan(45.0), "lit edge points down at the sun: a bowl, not a C");
        }

        [Test]
        public void TheLitEdgeFacesTheSun()
        {
            // An evening crescent in the west is lit on its lower right side; a morning one in the east on its lower left.
            var londonEvening = Astronomy.BrightLimbFromUp(Utc(2025, 3, 2, 18, 0), 51.51, -0.13);
            Assert.That(londonEvening, Is.InRange(90.0, 270.0), "lit side points down and to the right towards the set sun");
            double morning = Astronomy.BrightLimbFromUp(Utc(2025, 2, 26, 3, 0), 24.71, 46.68);
            Assert.That(morning, Is.InRange(90.0, 270.0), "points down towards the rising sun");
        }

        [Test]
        public void SunFollowsTheClock()
        {
            // Midsummer midday in Riyadh: nearly overhead (Riyadh is just south of the tropic).
            var noon = Astronomy.Sun(Utc(2025, 6, 21, 9, 0), 24.71, 46.68);
            Assert.That(noon.Altitude, Is.GreaterThan(85.0));
            // Midnight: well below.
            Assert.That(Astronomy.Sun(Utc(2025, 6, 21, 21, 0), 24.71, 46.68).Altitude, Is.LessThan(-30.0));
            // Morning sun in the east, evening in the west.
            Assert.That(Astronomy.Sun(Utc(2025, 3, 20, 4, 0), 24.71, 46.68).Azimuth, Is.InRange(60.0, 120.0));
            Assert.That(Astronomy.Sun(Utc(2025, 3, 20, 14, 0), 24.71, 46.68).Azimuth, Is.InRange(240.0, 300.0));
            // Equinox sunrise in Riyadh is about 06:00 local (03:00 UTC).
            Assert.That(Astronomy.Sun(Utc(2025, 3, 20, 3, 0), 24.71, 46.68).Altitude, Is.InRange(-4.0, 4.0));
        }

        [Test]
        public void SixTimesOfDayComeInOrder()
        {
            var seen = new List<DayPhase>();
            var day = Utc(2025, 3, 20, 0, 0).AddHours(-3);
            for (int min = 0; min < 24 * 60; min += 5)
            {
                var p = Astronomy.PhaseAt(day.AddMinutes(min), 24.71, 46.68);
                if (seen.Count == 0 || seen[seen.Count - 1] != p) seen.Add(p);
            }
            Assert.That(seen, Is.EqualTo(new[] { DayPhase.Night, DayPhase.Fajr, DayPhase.Morning, DayPhase.Noon, DayPhase.Asr, DayPhase.Maghrib, DayPhase.Night }));
        }

        [Test]
        public void StageTimesLandWhereTheyShould()
        {
            var date = new DateTime(2025, 3, 20);
            var sunset = Astronomy.StageInstant("sunset", date, 3, 24.71, 46.68);
            Assert.That(Astronomy.Sun(sunset, 24.71, 46.68).Altitude, Is.InRange(2.0, 10.0));
            Assert.That(Astronomy.Sun(Astronomy.StageInstant("day", date, 3, 24.71, 46.68), 24.71, 46.68).Altitude, Is.GreaterThan(40.0));
            Assert.That(Astronomy.Sun(Astronomy.StageInstant("night", date, 3, 24.71, 46.68), 24.71, 46.68).Altitude, Is.LessThan(-30.0));
        }

        [Test]
        public void StarsAndMilkyWayFadeWithCloudsAndDaylight()
        {
            var clear = SkyLook.For(-30, 0f, 0f, 0f, false);
            Assert.That(clear.Stars, Is.EqualTo(1f).Within(1e-4));
            Assert.That(clear.MilkyWay, Is.EqualTo(1f).Within(1e-4));
            Assert.That(SkyLook.For(-30, 0.5f, 0f, 0f, false).Stars, Is.LessThan(0.3f));
            Assert.That(SkyLook.For(-30, 1f, 0f, 0f, false).Stars, Is.EqualTo(0f));
            Assert.That(SkyLook.For(-30, 0f, 0f, 1f, true).MilkyWay, Is.LessThan(0.3f), "a full moon washes out the Milky Way");
            Assert.That(SkyLook.For(20, 0f, 0f, 0f, false).Stars, Is.EqualTo(0f));
            Assert.That(SkyLook.For(-4, 0f, 0f, 0f, false).Stars, Is.EqualTo(0f), "not yet at Fajr twilight");
            var maghrib = SkyLook.For(-1, 0f, 0f, 0f, false);
            Assert.That(maghrib.HorizonR, Is.GreaterThan(maghrib.HorizonB), "warm horizon at Maghrib");
            var noon = SkyLook.For(60, 0f, 0f, 0f, false);
            Assert.That(noon.ZenithB, Is.GreaterThan(noon.ZenithR), "blue sky by day");
            Assert.That(SkyLook.For(60, 1f, 0f, 0f, false).SunIntensity, Is.LessThan(noon.SunIntensity));
        }

        // ---- Save ----

        [Test]
        public void OldSavesGetDefaultWeatherAndAccessSettings()
        {
            var d = JsonSerializer.Deserialize<SaveData>("{\"version\":3,\"coins\":5}", Opt);
            Assert.That(d.weather, Is.Not.Null);
            Assert.That(d.weather.dynamicWeather, Is.True);
            Assert.That(d.weather.freePlayEffects, Is.False);
            Assert.That(d.access, Is.Not.Null);
            Assert.That(d.access.reduceFlash, Is.False);
            var back = JsonSerializer.Deserialize<SaveData>(JsonSerializer.Serialize(new SaveData { access = new Safety.AccessSettings { reduceFlash = true } }, Opt), Opt);
            Assert.That(back.access.reduceFlash, Is.True);
        }
    }
}
