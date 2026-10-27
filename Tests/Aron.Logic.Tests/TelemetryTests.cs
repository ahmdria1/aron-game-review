// (c) Ahmed Alamrani. All rights reserved.
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using Aron.Logic;
using Aron.Logic.Config;
using Aron.Logic.Regions;
using Aron.Logic.Telemetry;
using NUnit.Framework;

namespace Aron.Logic.Tests
{
    /// <summary>Usage statistics: off until a grown-up says yes, only the listed events, batched, and erasable.</summary>
    public class TelemetryTests
    {
        private const long T0 = 1_800_000_000;
        private static PrivacyChoices On() { var c = new PrivacyChoices(); ConsentGate.Set(c, Channel.Analytics, true, true, T0); return c; }
        private static string Doc(string name) => File.ReadAllText(Path.Combine(TestPaths.Root(), "docs", name));

        // ------------------------------------------------------------------ consent gate

        [Test]
        public void EverythingIsOffUntilChosen()
        {
            var c = new PrivacyChoices();
            Assert.IsFalse(ConsentGate.Allowed(c, Channel.Analytics));
            Assert.IsFalse(ConsentGate.Allowed(c, Channel.Diagnostics));
            Assert.IsFalse(ConsentGate.Allowed(null, Channel.Analytics));
            Assert.IsTrue(ConsentGate.NeedsAsking(c, Channel.Analytics));
        }

        [Test]
        public void ANewSaveStartsWithNothingAllowed()
        {
            var d = new SaveData();
            Assert.IsNotNull(d.privacy);
            Assert.IsFalse(ConsentGate.Allowed(d.privacy, Channel.Analytics));
            Assert.IsFalse(ConsentGate.Allowed(d.privacy, Channel.Diagnostics));
        }

        [Test]
        public void TurningOnNeedsTheGrownUpCheck()
        {
            var c = new PrivacyChoices();
            Assert.IsFalse(ConsentGate.Set(c, Channel.Analytics, true, false, T0));
            Assert.IsFalse(ConsentGate.Allowed(c, Channel.Analytics));
            Assert.IsTrue(ConsentGate.Set(c, Channel.Analytics, true, true, T0));
            Assert.IsTrue(ConsentGate.Allowed(c, Channel.Analytics));
        }

        [Test]
        public void TurningOffNeverNeedsAnyCheck()
        {
            var c = On();
            Assert.IsTrue(ConsentGate.Set(c, Channel.Analytics, false, false, T0 + 5));
            Assert.IsFalse(ConsentGate.Allowed(c, Channel.Analytics));
            Assert.IsFalse(ConsentGate.NeedsAsking(c, Channel.Analytics), "a 'no' is an answer; do not nag");
        }

        [Test]
        public void ChannelsAreIndependent()
        {
            var c = On();
            Assert.IsFalse(ConsentGate.Allowed(c, Channel.Diagnostics));
            ConsentGate.Set(c, Channel.Diagnostics, true, true, T0);
            ConsentGate.Set(c, Channel.Analytics, false, false, T0);
            Assert.IsTrue(ConsentGate.Allowed(c, Channel.Diagnostics));
            Assert.IsFalse(ConsentGate.Allowed(c, Channel.Analytics));
        }

        [Test]
        public void ANewerNoticeVersionStopsCollectionUntilAskedAgain()
        {
            var c = On();
            Assert.IsTrue(ConsentGate.Allowed(c, Channel.Analytics, ConsentGate.NoticeVersion));
            Assert.IsFalse(ConsentGate.Allowed(c, Channel.Analytics, "2099-01"));
            Assert.IsTrue(ConsentGate.NeedsAsking(c, Channel.Analytics, "2099-01"));
        }

        [Test]
        public void WithdrawingEverythingTurnsBothOffAndResetsFirstOpen()
        {
            var c = On();
            ConsentGate.Set(c, Channel.Diagnostics, true, true, T0);
            c.firstOpenSent = true;
            ConsentGate.WithdrawAll(c, T0 + 1);
            Assert.IsFalse(ConsentGate.Allowed(c, Channel.Analytics));
            Assert.IsFalse(ConsentGate.Allowed(c, Channel.Diagnostics));
            Assert.IsFalse(c.firstOpenSent);
        }

        [Test]
        public void ChoicesSurviveTheSaveFile()
        {
            var o = new JsonSerializerOptions { IncludeFields = true };
            var c = On();
            var back = JsonSerializer.Deserialize<PrivacyChoices>(JsonSerializer.Serialize(c, o), o);
            Assert.IsTrue(ConsentGate.Allowed(back, Channel.Analytics));
            var old = JsonSerializer.Deserialize<SaveData>("{}", o);
            Assert.IsFalse(ConsentGate.Allowed(old.privacy, Channel.Analytics), "saves from before this feature stay off");
        }

        [Test]
        public void TheServerSwitchIsOnByDefaultAndIsAFeature()
        {
            Assert.IsTrue(GameSettings.Defaults().IsOn(Feature.Analytics));
            Assert.IsFalse(new FeatureFlags { analytics = false }.IsOn(Feature.Analytics));
        }

        // ------------------------------------------------------------------ schema

        private static IEnumerable<AnalyticsEvent> Samples()
        {
            yield return Events.FirstOpen();
            yield return Events.RegionUnlocked("japan");
            yield return Events.StageStart("r1");
            yield return Events.StageComplete("r1", 83.4f);
            yield return Events.StageFail("r1", 12f);
            yield return Events.BossDefeated("r2");
            yield return Events.PurchaseCompleted("com.example.pack_small");
            yield return Events.CrashCount(3);
        }

        [Test]
        public void EveryAllowedEventHasABuilderThatPassesTheSchema()
        {
            var names = Samples().Select(e => e.name).ToList();
            CollectionAssert.AreEquivalent(EventSchema.Names, names);
            foreach (var e in Samples()) Assert.IsNull(EventSchema.Check(e), e.name);
        }

        [Test]
        public void OnlyTheAgreedKeysEverAppear()
        {
            var keys = Samples().SelectMany(e => e.p).Select(p => p.k).Distinct().OrderBy(x => x).ToArray();
            CollectionAssert.AreEqual(new[] { "count", "product", "region", "seconds", "stage" }, keys);
        }

        [Test]
        public void AnythingNotListedIsRefused()
        {
            Assert.IsNotNull(EventSchema.Check(null));
            Assert.IsNotNull(EventSchema.Check(new AnalyticsEvent("")));
            Assert.IsNotNull(EventSchema.Check(new AnalyticsEvent("player_name")));
            Assert.IsNotNull(EventSchema.Check(Events.StageStart("r1").Id("email", "a@b.c")), "extra parameter");
            Assert.IsNotNull(EventSchema.Check(new AnalyticsEvent(EventSchema.StageStart)), "missing parameter");
            Assert.IsNotNull(EventSchema.Check(new AnalyticsEvent(EventSchema.StageStart).Num("stage", 5)), "number where an id belongs");
            Assert.IsNotNull(EventSchema.Check(new AnalyticsEvent(EventSchema.StageComplete).Id("stage", "r1").Id("seconds", "12")), "text where a number belongs");
        }

        [TestCase("a@b.com")]
        [TestCase("Ahmed")]
        [TestCase("has space")]
        [TestCase("نص")]
        [TestCase("")]
        [TestCase("0123456789012345678901234567890123456789012345678901234567890123456789")]
        public void IdsCannotCarryPersonalText(string bad)
        {
            Assert.IsFalse(EventSchema.IsId(bad));
            Assert.IsNotNull(EventSchema.Check(new AnalyticsEvent(EventSchema.StageStart).Id("stage", bad)));
        }

        [Test]
        public void NumbersAreClampedByTheBuildersAndCheckedByTheSchema()
        {
            Assert.AreEqual(0, Events.StageComplete("r1", -5f).p.Single(p => p.k == "seconds").i);
            Assert.AreEqual(0, Events.StageComplete("r1", float.NaN).p.Single(p => p.k == "seconds").i);
            Assert.AreEqual(EventSchema.MaxSeconds, Events.StageFail("r1", 1e9f).p.Single(p => p.k == "seconds").i);
            Assert.AreEqual(0, Events.CrashCount(-3).p.Single().i);
            Assert.AreEqual(EventSchema.MaxCount, Events.CrashCount(int.MaxValue).p.Single().i);
            Assert.IsNotNull(EventSchema.Check(new AnalyticsEvent(EventSchema.CrashCount).Num("count", -1)));
            Assert.IsNotNull(EventSchema.Check(new AnalyticsEvent(EventSchema.StageFail).Id("stage", "r1").Num("seconds", EventSchema.MaxSeconds + 1)));
        }

        [Test]
        public void BuildersLowercaseIdsSoNothingMixedCaseSlipsThroughByAccident()
        {
            Assert.AreEqual("japan", Events.RegionUnlocked(" Japan ").p.Single().s);
            Assert.IsNull(EventSchema.Check(Events.RegionUnlocked(" Japan ")));
        }

        [Test]
        public void PurchaseEventCarriesTheProductIdAndNothingElse()
        {
            var e = Events.PurchaseCompleted("com.example.pack_small");
            Assert.AreEqual(1, e.p.Count);
            Assert.AreEqual("product", e.p[0].k);
        }

        // ------------------------------------------------------------------ buffer

        private static EventBuffer Buf(Func<bool> may = null) => new EventBuffer { MayRecord = may ?? (() => true) };

        [Test]
        public void ABufferWithoutConsentKeepsNothing()
        {
            var b = new EventBuffer();               // default check says no
            Assert.IsFalse(b.Record(Events.StageStart("r1"), T0));
            Assert.AreEqual(0, b.Count);
            Assert.AreEqual(1, b.RefusedNoConsent);
        }

        [Test]
        public void ConsentIsCheckedAtEveryEventNotJustOnce()
        {
            bool yes = true;
            var b = Buf(() => yes);
            Assert.IsTrue(b.Record(Events.StageStart("r1"), T0));
            yes = false;
            Assert.IsFalse(b.Record(Events.StageStart("r2"), T0));
            Assert.AreEqual(1, b.Count);
        }

        [Test]
        public void InvalidEventsAreRefusedEvenWithConsent()
        {
            var b = Buf();
            Assert.IsFalse(b.Record(new AnalyticsEvent("who_is_this"), T0));
            Assert.AreEqual(1, b.RefusedInvalid);
            Assert.AreEqual(0, b.Count);
        }

        [Test]
        public void ABatchIsDueAtTwentyEventsOrFifteenMinutes()
        {
            var b = Buf();
            for (int i = 0; i < EventBuffer.BatchSize - 1; i++) b.Record(Events.StageStart("r" + i), T0);
            Assert.IsFalse(b.Due(T0 + 60, true), "19 events, one minute old");
            Assert.IsTrue(b.Due(T0 + EventBuffer.MaxAgeSeconds, true), "oldest is 15 minutes old");
            b.Record(Events.StageStart("last"), T0 + 60);
            Assert.IsTrue(b.Due(T0 + 61, true), "20 events");
        }

        [Test]
        public void NothingIsDueWhenTheNetworkIsNotAllowedOrTheBufferIsEmpty()
        {
            var b = Buf();
            Assert.IsFalse(b.Due(T0, true));
            for (int i = 0; i < 30; i++) b.Record(Events.StageStart("r" + i), T0);
            Assert.IsFalse(b.Due(T0 + 99999, false));
        }

        [Test]
        public void SentEventsAreRemovedOnlyAfterTheyWereHandedOver()
        {
            var b = Buf();
            for (int i = 0; i < 5; i++) b.Record(Events.StageStart("r" + i), T0);
            var batch = b.Peek();
            Assert.AreEqual(5, batch.Count);
            Assert.AreEqual(5, b.Count, "still queued until the send worked");
            b.Remove(batch.Count);
            Assert.AreEqual(0, b.Count);
        }

        [Test]
        public void TheQueueIsCapped()
        {
            var b = Buf();
            for (int i = 0; i < EventBuffer.MaxEvents + 25; i++) b.Record(Events.StageStart("r" + i), T0);
            Assert.AreEqual(EventBuffer.MaxEvents, b.Count);
            Assert.AreEqual(25, b.Dropped);
            Assert.AreEqual("r25", b.Peek(1)[0].p[0].s, "oldest are dropped first");
        }

        [Test]
        public void ASavedQueueIsThrownAwayWithoutConsentAndFilteredWithIt()
        {
            var good = Buf();
            good.Record(Events.StageStart("r1"), T0);
            var snap = good.Snapshot();
            snap.events.Add(new AnalyticsEvent("tampered"));
            snap.at.Add(T0);

            var denied = new EventBuffer();
            denied.Load(snap);
            Assert.AreEqual(0, denied.Count);

            var allowed = Buf();
            allowed.Load(snap);
            Assert.AreEqual(1, allowed.Count, "the tampered entry is skipped");
        }

        [Test]
        public void ClearEmptiesTheQueue()
        {
            var b = Buf();
            b.Record(Events.StageStart("r1"), T0);
            b.Clear();
            Assert.AreEqual(0, b.Count);
            Assert.AreEqual(0, b.Snapshot().events.Count);
        }

        [Test]
        public void ABatchOfTwentyIsSmall()
        {
            var b = Buf();
            for (int i = 0; i < 20; i++) b.Record(Events.StageComplete("saudi_riyadh_" + i, 95.5f), T0);
            int bytes = System.Text.Encoding.UTF8.GetByteCount(JsonSerializer.Serialize(b.Peek(), new JsonSerializerOptions { IncludeFields = true }));
            Assert.Less(bytes, 6000, "a batch before compression");
        }

        // ------------------------------------------------------------------ crash count and regions

        [TestCase(0, 0, 0)]
        [TestCase(5, 2, 3)]
        [TestCase(2, 5, 0)]
        [TestCase(4, -1, 4)]
        public void CrashCountIsOnlyWhatIsNew(int total, int reported, int expected)
        {
            Assert.AreEqual(expected, CrashCountPolicy.Pending(total, reported));
        }

        private static StageDef St(string id, bool boss = false) => new StageDef { id = id, boss = boss, type = boss ? "boss" : "normal" };

        [Test]
        public void ARegionOpensWhenThePreviousOnesBossFalls()
        {
            var c = RegionCatalog.FromRegions(new[]
            {
                new RegionDef { id = "riyadh", group = "saudi", order = 100, country = "saudi", stages = new[] { St("r1"), St("r2", true) } },
                new RegionDef { id = "japan", group = "world", order = 444, country = "japan", stages = new[] { St("j1"), St("j2", true) } },
            });
            var d = new SaveData();
            var before = RegionWatch.Reached(c, d);
            CollectionAssert.AreEquivalent(new[] { "saudi" }, before);
            StageProgress.Finish(d, "r1", 30f, 1);
            Assert.IsEmpty(RegionWatch.Opened(before, RegionWatch.Reached(c, d)));
            StageProgress.Finish(d, "r2", 30f, 1);
            CollectionAssert.AreEqual(new[] { "japan" }, RegionWatch.Opened(before, RegionWatch.Reached(c, d)));
            Assert.IsEmpty(RegionWatch.Opened(RegionWatch.Reached(c, d), RegionWatch.Reached(c, d)), "only once");
        }

        // ------------------------------------------------------------------ account deletion and docs

        [Test]
        public void DeletingTheAccountAlsoRemovesStatisticsData()
        {
            var s = Aron.Logic.Safety.AccountData.DeleteSteps;
            Assert.That(Array.IndexOf(s, "analytics_data"), Is.GreaterThanOrEqualTo(0).And.LessThan(Array.IndexOf(s, "player_id")));
        }

        [Test]
        public void TheAnalyticsDocListsEveryEventAndEveryKey()
        {
            var text = Doc("analytics.md");
            foreach (var n in EventSchema.Names) StringAssert.Contains("`" + n + "`", text);
            foreach (var k in new[] { "region", "stage", "seconds", "product", "count" }) StringAssert.Contains("`" + k + "`", text);
        }

        [Test]
        public void ThePrivacyPolicyDescribesStatisticsAndWhatUnityAdds()
        {
            var text = Doc("privacy_policy.md");
            StringAssert.Contains("Usage statistics", text);
            StringAssert.Contains("Unity Analytics", text);
            StringAssert.Contains("IP address", text);
            StringAssert.Contains("grown-up", text);
            StringAssert.Contains("Sentry", text);
        }

        [Test]
        public void NoAdvertisingIdIsReadOrSetAnywhereInTheProject()
        {
            var root = TestPaths.Root();
            var bad = new[] { "advertisingIdentifier", "advertisingTrackingEnabled", "SetAnalyticsUserId", "SetUserId", "GoogleAdvertisingId", "permission.AD_ID", "NSUserTrackingUsageDescription", "RequestTrackingAuthorization" };
            var hits = new List<string>();
            foreach (var dir in new[] { "Assets", "ProjectSettings", "Packages" })
            {
                var d = Path.Combine(root, dir);
                if (!Directory.Exists(d)) continue;
                foreach (var f in Directory.EnumerateFiles(d, "*", SearchOption.AllDirectories))
                {
                    var ext = Path.GetExtension(f).ToLowerInvariant();
                    if (ext != ".cs" && ext != ".xml" && ext != ".json" && ext != ".plist" && ext != ".asset" && ext != ".mm" && ext != ".gradle" && ext != ".txt") continue;
                    var text = File.ReadAllText(f);
                    foreach (var w in bad) if (text.Contains(w)) hits.Add(Path.GetRelativePath(root, f) + ": " + w);
                }
            }
            CollectionAssert.IsEmpty(hits);
        }
    }
}
