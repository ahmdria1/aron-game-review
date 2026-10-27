// (c) Ahmed Alamrani. All rights reserved.
using System;
using System.IO;
using System.Linq;
using System.Text.Json;
using Aron.Logic;
using Aron.Logic.Config;
using Aron.Logic.Integrity;
using NUnit.Framework;

namespace Aron.Logic.Tests
{
    /// <summary>Server-driven settings: safe defaults, limits, layering, kill switches, timed events and pin mode.</summary>
    public class SettingsTests
    {
        private static readonly JsonSerializerOptions Opt = new JsonSerializerOptions { IncludeFields = true, PropertyNameCaseInsensitive = true };
        private static GameSettings Parse(string json) => JsonSerializer.Deserialize<GameSettings>(json, Opt);
        private static string Bundled() => File.ReadAllText(Path.Combine(TestPaths.Root(), "Assets", "_Aron", "Resources", "Config", "remote_defaults.json"));
        private static string Doc(string body) => "{" + body + "}";

        // ------------------------------------------------------------------ defaults

        [Test]
        public void ShippedFileMatchesTheBuiltInDefaults()
        {
            var shipped = SettingsPolicy.Sanitize(Parse(Bundled()));
            var def = GameSettings.Defaults();
            Assert.AreEqual(JsonSerializer.Serialize(def, Opt), JsonSerializer.Serialize(shipped, Opt).Replace("\"version\":1", "\"version\":0"));
            foreach (Feature f in Enum.GetValues(typeof(Feature))) Assert.IsTrue(shipped.IsOn(f), f.ToString());
            Assert.AreEqual(0, shipped.events.Length);
        }

        [Test]
        public void EverySwitchIsOnByDefault()
        {
            var d = GameSettings.Defaults();
            foreach (Feature f in Enum.GetValues(typeof(Feature))) Assert.IsTrue(d.IsOn(f), f.ToString());
            foreach (var t in StoreTabs.Order) Assert.IsTrue(d.features.TabIsOn(t), t);
        }

        [Test]
        public void EverySwitchHasAnEntryInTheShippedFile()
        {
            using (var doc = JsonDocument.Parse(Bundled()))
            {
                var names = doc.RootElement.GetProperty("features").EnumerateObject().Select(p => p.Name).ToList();
                var fields = typeof(FeatureFlags).GetFields().Select(f => f.Name).ToList();
                CollectionAssert.AreEquivalent(fields, names);
            }
        }

        [Test]
        public void ADocumentWithOnlyOneKeyKeepsEveryOtherDefault()
        {
            var s = SettingsPolicy.Sanitize(Parse(Doc("\"version\": 3, \"features\": { \"vehicleCars\": false }")));
            Assert.IsFalse(s.features.vehicleCars);
            Assert.IsTrue(s.features.store && s.features.purchases && s.features.onlineRaces && s.features.vehicleSea);
            Assert.AreEqual(1f, s.difficulty.enemySpeedScale);
            Assert.AreEqual(PinNames.ReportOnly, s.pinMode);
        }

        // ------------------------------------------------------------------ kill switches

        [Test]
        public void VehicleSwitchesMapToTheirShopTabsOnly()
        {
            var f = new FeatureFlags { vehicleBicycles = false };
            foreach (var t in StoreTabs.Order) Assert.AreEqual(t != StoreTabs.Bicycles, f.TabIsOn(t), t);
        }

        [Test]
        public void PurchasesOffDoesNotTouchTheStoreFlag()
        {
            var f = new FeatureFlags { purchases = false };
            Assert.IsFalse(f.IsOn(Feature.Purchases));
            Assert.IsTrue(f.IsOn(Feature.Store));
        }

        // ------------------------------------------------------------------ limits

        [Test]
        public void DifficultyTuningIsClampedToItsBand()
        {
            var s = SettingsPolicy.Sanitize(Parse(Doc("\"difficulty\": { \"enemySpeedScale\": 9, \"bossHealthScale\": 0.01, \"extraLivesBonus\": 50 }")));
            Assert.AreEqual(DifficultyTuning.MaxScale, s.difficulty.enemySpeedScale);
            Assert.AreEqual(DifficultyTuning.MinScale, s.difficulty.bossHealthScale);
            Assert.AreEqual(DifficultyTuning.MaxLives, s.difficulty.extraLivesBonus);
        }

        [Test]
        public void TuningNeverBreaksTheDifficultyFileRanges()
        {
            var t = new DifficultyTuning { enemySpeedScale = 99f, bossHealthScale = -4f, extraLivesBonus = -9 };
            foreach (var id in new[] { "easy", "normal", "hard" })
            {
                var lv = new DifficultyLevel { id = id, extraLives = 0, enemySpeed = 1.2f, bossHealth = 0.8f };
                var tuned = t.Apply(lv);
                Assert.AreEqual(id, tuned.id);
                Assert.GreaterOrEqual(tuned.extraLives, 0);
                Assert.LessOrEqual(tuned.enemySpeed, 1.2f * DifficultyTuning.MaxScale + 1e-4f);
                Assert.GreaterOrEqual(tuned.bossHealth, 0.8f * DifficultyTuning.MinScale - 1e-4f);
            }
            Assert.AreEqual(lvCopy().checkpointSeconds, t.Apply(lvCopy()).checkpointSeconds);
            Assert.IsNull(t.Apply(null));
        }

        private static DifficultyLevel lvCopy() => new DifficultyLevel { id = "normal", checkpointSeconds = 90f };

        [Test]
        public void TuningDoesNotChangeTheOriginalLevel()
        {
            var lv = new DifficultyLevel { enemySpeed = 1f };
            new DifficultyTuning { enemySpeedScale = 1.2f }.Apply(lv);
            Assert.AreEqual(1f, lv.enemySpeed);
        }

        [Test]
        public void BadEventsAreDroppedAndGoodOnesKept()
        {
            const long day = 86400;
            long t0 = 1_800_000_000;
            var s = new GameSettings
            {
                events = new[]
                {
                    new TimedEvent { id = "ok", startUnix = t0, endUnix = t0 + 2 * day, datesMultiplier = 2f },
                    new TimedEvent { id = "ok", startUnix = t0, endUnix = t0 + day },                       // duplicate id
                    new TimedEvent { id = "", startUnix = t0, endUnix = t0 + day },                         // no id
                    new TimedEvent { id = "backwards", startUnix = t0 + day, endUnix = t0 },
                    new TimedEvent { id = "forever", startUnix = t0, endUnix = t0 + 400 * day },
                    new TimedEvent { id = "nostart", startUnix = 0, endUnix = t0 },
                    new TimedEvent { id = "big", startUnix = t0, endUnix = t0 + day, datesMultiplier = 50f },
                    new TimedEvent { id = "nan", startUnix = t0, endUnix = t0 + day, datesMultiplier = float.NaN },
                    null,
                }
            };
            var c = SettingsPolicy.Sanitize(s);
            CollectionAssert.AreEqual(new[] { "ok", "big", "nan" }, c.events.Select(e => e.id).ToArray());
            Assert.AreEqual(TimedEvent.MaxMultiplier, c.events.Single(e => e.id == "big").datesMultiplier);
            Assert.AreEqual(1f, c.events.Single(e => e.id == "nan").datesMultiplier);
        }

        [Test]
        public void NoMoreThanEightEventsSurvive()
        {
            long t0 = 1_800_000_000;
            var s = new GameSettings { events = Enumerable.Range(0, 30).Select(i => new TimedEvent { id = "e" + i, startUnix = t0, endUnix = t0 + 1000 }).ToArray() };
            Assert.AreEqual(GameSettings.MaxEvents, SettingsPolicy.Sanitize(s).events.Length);
        }

        [Test]
        public void SanitizingNullOrHalfEmptyDocumentsGivesDefaults()
        {
            Assert.AreEqual(0, SettingsPolicy.Sanitize(null).version);
            var half = SettingsPolicy.Sanitize(new GameSettings { features = null, difficulty = null, events = null, version = -5 });
            Assert.IsNotNull(half.features);
            Assert.IsNotNull(half.events);
            Assert.AreEqual(0, half.version);
        }

        // ------------------------------------------------------------------ timed events

        [Test]
        public void EventMultipliesOnlyInsideItsWindowAndOnlyOnATrustedClock()
        {
            long t0 = 1_800_000_000;
            var s = SettingsPolicy.Sanitize(new GameSettings { events = new[] { new TimedEvent { id = "weekend", startUnix = t0, endUnix = t0 + 1000, datesMultiplier = 2f } } });
            Assert.AreEqual(1f, s.DatesMultiplier(t0 - 1, true));
            Assert.AreEqual(2f, s.DatesMultiplier(t0, true));
            Assert.AreEqual(2f, s.DatesMultiplier(t0 + 999, true));
            Assert.AreEqual(1f, s.DatesMultiplier(t0 + 1000, true));
            Assert.AreEqual(1f, s.DatesMultiplier(t0 + 10, false), "a clock that cannot be trusted never starts an event");
            Assert.AreEqual(0, s.ActiveEvents(t0 + 10, false).Count);
            Assert.AreEqual(1, s.ActiveEvents(t0 + 10, true).Count);
        }

        [Test]
        public void EventsSwitchTurnsEveryEventOff()
        {
            long t0 = 1_800_000_000;
            var s = SettingsPolicy.Sanitize(new GameSettings
            {
                features = new FeatureFlags { events = false },
                events = new[] { new TimedEvent { id = "x", startUnix = t0, endUnix = t0 + 1000, datesMultiplier = 2f } },
            });
            Assert.AreEqual(1f, s.DatesMultiplier(t0 + 1, true));
            Assert.AreEqual(0, s.ActiveEvents(t0 + 1, true).Count);
        }

        [Test]
        public void OverlappingEventsTakeTheBestNotTheProduct()
        {
            long t0 = 1_800_000_000;
            var s = SettingsPolicy.Sanitize(new GameSettings
            {
                events = new[]
                {
                    new TimedEvent { id = "a", startUnix = t0, endUnix = t0 + 1000, datesMultiplier = 1.5f },
                    new TimedEvent { id = "b", startUnix = t0, endUnix = t0 + 1000, datesMultiplier = 2f },
                },
            });
            Assert.AreEqual(2f, s.DatesMultiplier(t0 + 1, true));
        }

        // ------------------------------------------------------------------ layering

        private static string V(int version, string extra = "") => Doc("\"version\": " + version + extra);

        [Test]
        public void NothingAtAllGivesTheBuiltInDefaults()
        {
            var r = SettingsResolver.Resolve(null, null, null, Parse);
            Assert.AreEqual(SettingsSource.Defaults, r.source);
            Assert.IsTrue(r.settings.IsOn(Feature.Store));
        }

        [Test]
        public void BundledFileIsUsedWhenThereIsNothingElse()
        {
            var r = SettingsResolver.Resolve(Bundled(), "", "", Parse);
            Assert.AreEqual(SettingsSource.Bundled, r.source);
        }

        [Test]
        public void NewerServerAnswerWins()
        {
            var r = SettingsResolver.Resolve(V(1), V(2), V(3, ", \"features\": { \"store\": false }"), Parse);
            Assert.AreEqual(SettingsSource.Remote, r.source);
            Assert.IsFalse(r.settings.IsOn(Feature.Store));
        }

        [Test]
        public void OlderServerAnswerIsIgnored()
        {
            var r = SettingsResolver.Resolve(V(1), V(5, ", \"features\": { \"store\": false }"), V(4), Parse);
            Assert.AreEqual(SettingsSource.Cached, r.source);
            Assert.IsFalse(r.settings.IsOn(Feature.Store));
        }

        [Test]
        public void SameVersionFromTheServerIsAccepted()
        {
            var r = SettingsResolver.Resolve(V(1), V(2), V(2, ", \"features\": { \"events\": false }"), Parse);
            Assert.AreEqual(SettingsSource.Remote, r.source);
            Assert.IsFalse(r.settings.IsOn(Feature.Events));
        }

        [TestCase("")]
        [TestCase("   ")]
        [TestCase("not json at all")]
        [TestCase("{ \"version\": ")]
        [TestCase("[1,2,3]")]
        [TestCase("null")]
        public void BrokenServerAnswerFallsBackToWhatThePhoneHad(string junk)
        {
            var r = SettingsResolver.Resolve(Bundled(), V(2, ", \"features\": { \"vehicleSea\": false }"), junk, Parse);
            Assert.AreEqual(SettingsSource.Cached, r.source);
            Assert.IsFalse(r.settings.IsOn(Feature.VehicleSea));
        }

        [Test]
        public void BrokenCacheFallsBackToTheShippedFile()
        {
            var r = SettingsResolver.Resolve(Bundled(), "{{{{", null, Parse);
            Assert.AreEqual(SettingsSource.Bundled, r.source);
        }

        [Test]
        public void AParserThatThrowsNeverBreaksTheGame()
        {
            var r = SettingsResolver.Resolve(Bundled(), Bundled(), Bundled(), s => throw new InvalidOperationException("boom"));
            Assert.AreEqual(SettingsSource.Defaults, r.source);
            Assert.IsTrue(r.settings.IsOn(Feature.Purchases));
        }

        [Test]
        public void ServerValuesAreSanitizedBeforeUse()
        {
            var r = SettingsResolver.Resolve(Bundled(), null, V(2, ", \"difficulty\": { \"enemySpeedScale\": 100 }, \"pinMode\": \"banana\""), Parse);
            Assert.AreEqual(DifficultyTuning.MaxScale, r.settings.difficulty.enemySpeedScale);
            Assert.AreEqual(PinNames.ReportOnly, r.settings.pinMode);
        }

        // ------------------------------------------------------------------ pin mode

        private static CertPins Pins(int n) => new CertPins(Enumerable.Range(0, n).Select(i => "pin" + i));

        [Test]
        public void EnforceNeedsTwoPins()
        {
            Assert.AreEqual(PinMode.ReportOnly, PinPolicy.Effective(PinMode.ReportOnly, PinNames.Enforce, Pins(1)));
            Assert.AreEqual(PinMode.ReportOnly, PinPolicy.Effective(PinMode.ReportOnly, PinNames.Enforce, Pins(0)));
            Assert.AreEqual(PinMode.ReportOnly, PinPolicy.Effective(PinMode.ReportOnly, PinNames.Enforce, null));
            Assert.AreEqual(PinMode.Enforce, PinPolicy.Effective(PinMode.ReportOnly, PinNames.Enforce, Pins(2)));
        }

        [Test]
        public void ServerCanRelaxEnforceButCannotTurnPinningOff()
        {
            Assert.AreEqual(PinMode.ReportOnly, PinPolicy.Effective(PinMode.Enforce, PinNames.ReportOnly, Pins(2)));
            Assert.AreEqual(PinMode.Enforce, PinPolicy.Effective(PinMode.Enforce, "off", Pins(2)), "'off' from the server is not a mode");
            Assert.AreEqual(PinMode.Enforce, PinPolicy.Effective(PinMode.Enforce, null, Pins(2)));
            Assert.AreEqual(PinMode.Off, PinPolicy.Effective(PinMode.Off, PinNames.ReportOnly, Pins(2)));
        }

        [Test]
        public void ServerCannotSendOffThroughTheSanitizer()
        {
            Assert.AreEqual(PinNames.ReportOnly, SettingsPolicy.Sanitize(new GameSettings { pinMode = PinNames.Off }).pinMode);
        }

        // ------------------------------------------------------------------ nothing sensitive in the file

        [Test]
        public void ShippedFileHoldsNoAddressesOrKeys()
        {
            var text = Bundled();
            StringAssert.DoesNotContain("http", text);
            StringAssert.DoesNotContain("key", text.ToLowerInvariant().Replace("\"", "").Replace("pinmode", ""));
            StringAssert.DoesNotContain("secret", text.ToLowerInvariant());
        }
    }
}
