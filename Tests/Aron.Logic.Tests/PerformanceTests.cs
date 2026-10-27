// (c) Ahmed Alamrani. All rights reserved.
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Aron.Logic.Diagnostics;
using Aron.Logic.Online;
using Aron.Logic.Performance;
using NUnit.Framework;

namespace Aron.Logic.Tests
{
    public class FrameCapPolicyTests
    {
        private static PowerInputs Play(QualityTier tier, bool prefer60 = false) => new PowerInputs { tier = tier, mode = ScreenMode.Gameplay, prefer60 = prefer60, batteryPercent = 80 };

        [Test]
        public void EveryTierPlaysAt30ByDefault()
        {
            foreach (QualityTier t in Enum.GetValues(typeof(QualityTier)))
                Assert.AreEqual(30, FrameCapPolicy.Decide(Play(t)).target, t.ToString());
        }

        [Test]
        public void SixtyIsOnlyForHighPhonesThatAskedForIt()
        {
            Assert.AreEqual(60, FrameCapPolicy.Decide(Play(QualityTier.High, true)).target);
            Assert.AreEqual(30, FrameCapPolicy.Decide(Play(QualityTier.Medium, true)).target);
            Assert.AreEqual(30, FrameCapPolicy.Decide(Play(QualityTier.Low, true)).target);
        }

        [Test]
        public void SixtyIsWithdrawnWhenPowerIsShortOrThePhoneIsWarm()
        {
            var p = Play(QualityTier.High, true);
            var saver = p; saver.batterySaver = true;
            var os = p; os.osPowerSave = true;
            var low = p; low.batteryPercent = 15;
            var lowCharging = low; lowCharging.charging = true;
            var warm = p; warm.thermal = ThermalLevel.Fair;
            var slow = p; slow.heldTo30 = true;
            foreach (var x in new[] { saver, os, low, warm, slow }) Assert.AreEqual(30, FrameCapPolicy.Decide(x).target);
            Assert.AreEqual(60, FrameCapPolicy.Decide(lowCharging).target, "charging phones may use 60");
            var unknown = p; unknown.batteryPercent = -1;
            Assert.AreEqual(60, FrameCapPolicy.Decide(unknown).target, "an unknown battery level is not treated as low");
        }

        [Test]
        public void MenusPauseAndLoadingDrawFifteenFramesASecond()
        {
            foreach (var m in new[] { ScreenMode.Menu, ScreenMode.Paused, ScreenMode.Loading })
            {
                var c = FrameCapPolicy.Decide(new PowerInputs { tier = QualityTier.High, mode = m, prefer60 = true });
                Assert.AreEqual(15, c.RenderedFps, m.ToString());
                Assert.AreEqual(30, c.target, "the loop stays at 30 so taps feel instant");
            }
        }

        [Test]
        public void BackgroundDropsToAlmostNothingAndCriticalHeatDropsFurther()
        {
            Assert.AreEqual(1, FrameCapPolicy.Decide(new PowerInputs { mode = ScreenMode.Background }).target);
            var hot = Play(QualityTier.High, true); hot.thermal = ThermalLevel.Critical;
            Assert.AreEqual(20, FrameCapPolicy.Decide(hot).target);
        }

        [Test]
        public void DecidingAllocatesNothing()
        {
            var p = Play(QualityTier.High, true);
            int sink = 0;
            AllocationCheck.AssertNoAllocation(() => sink += FrameCapPolicy.Decide(p).target, "FrameCapPolicy.Decide");
            Assert.Greater(sink, 0);
        }
    }

    public class ThermalTests
    {
        [Test]
        public void PhoneStatusesMapOntoFourLevels()
        {
            Assert.AreEqual(ThermalLevel.Nominal, ThermalMap.FromAndroid(0));
            Assert.AreEqual(ThermalLevel.Nominal, ThermalMap.FromAndroid(1));
            Assert.AreEqual(ThermalLevel.Fair, ThermalMap.FromAndroid(2));
            Assert.AreEqual(ThermalLevel.Serious, ThermalMap.FromAndroid(3));
            Assert.AreEqual(ThermalLevel.Critical, ThermalMap.FromAndroid(4));
            Assert.AreEqual(ThermalLevel.Critical, ThermalMap.FromAndroid(6));
            Assert.AreEqual(ThermalLevel.Nominal, ThermalMap.FromIos(0));
            Assert.AreEqual(ThermalLevel.Fair, ThermalMap.FromIos(1));
            Assert.AreEqual(ThermalLevel.Serious, ThermalMap.FromIos(2));
            Assert.AreEqual(ThermalLevel.Critical, ThermalMap.FromIos(3));
        }

        [Test]
        public void GettingHotterIsObeyedAtOnce()
        {
            var g = new ThermalGovernor();
            Assert.IsTrue(g.Report(ThermalLevel.Serious, 1f));
            Assert.AreEqual(ThermalLevel.Serious, g.Applied);
            Assert.IsFalse(g.Report(ThermalLevel.Serious, 2f));
        }

        [Test]
        public void CoolingDownIsBelievedSlowlyAndOneLevelAtATime()
        {
            var g = new ThermalGovernor { CooldownSeconds = 60f };
            g.Report(ThermalLevel.Critical, 0f);
            Assert.IsFalse(g.Report(ThermalLevel.Nominal, 10f));
            Assert.IsFalse(g.Report(ThermalLevel.Nominal, 69f));
            Assert.IsTrue(g.Report(ThermalLevel.Nominal, 71f));
            Assert.AreEqual(ThermalLevel.Serious, g.Applied);
            Assert.IsFalse(g.Report(ThermalLevel.Nominal, 100f));
            Assert.IsTrue(g.Report(ThermalLevel.Nominal, 131f));
            Assert.AreEqual(ThermalLevel.Fair, g.Applied);
            Assert.IsTrue(g.Report(ThermalLevel.Nominal, 192f));
            Assert.AreEqual(ThermalLevel.Nominal, g.Applied);
        }

        [Test]
        public void ABlipOfHeatDuringCooldownRestartsTheClock()
        {
            var g = new ThermalGovernor { CooldownSeconds = 60f };
            g.Report(ThermalLevel.Serious, 0f);
            g.Report(ThermalLevel.Fair, 10f);
            g.Report(ThermalLevel.Serious, 50f);
            Assert.IsFalse(g.Report(ThermalLevel.Fair, 80f), "the 60 seconds start again from the last hot report");
            Assert.IsTrue(g.Report(ThermalLevel.Fair, 141f));
        }

        [Test]
        public void TheGovernorAllocatesNothing()
        {
            var g = new ThermalGovernor();
            float t = 0; int i = 0;
            AllocationCheck.AssertNoAllocation(() => { t += 1f; g.Report((ThermalLevel)(i++ & 3), t); }, "ThermalGovernor.Report");
        }
    }

    public class QualityLadderTests
    {
        [Test]
        public void EachStepIsCheaperThanTheLast()
        {
            for (int s = 1; s <= QualityLadder.MaxStep; s++)
            {
                var a = QualityLadder.Plan(s - 1); var b = QualityLadder.Plan(s);
                Assert.LessOrEqual(b.resolution, a.resolution);
                Assert.LessOrEqual(b.foliage, a.foliage);
                Assert.LessOrEqual(b.particles, a.particles);
                Assert.GreaterOrEqual((int)b.shadows, (int)a.shadows);
                Assert.Less(b.resolution + b.foliage + b.particles + (int)b.shadows * -0.1f, a.resolution + a.foliage + a.particles + (int)a.shadows * -0.1f);
            }
            Assert.AreEqual(1f, QualityLadder.Plan(0).resolution);
        }

        [Test]
        public void ResolutionComesFirstAndParticlesStayAliveLongest()
        {
            var one = QualityLadder.Plan(1);
            Assert.Less(one.resolution, 1f);
            Assert.AreEqual(ShadowMode.Hard, one.shadows);
            var three = QualityLadder.Plan(3);
            Assert.AreEqual(ShadowMode.Off, three.shadows);
            Assert.Greater(three.particles, 0f, "effects are thinned, never switched off");
        }

        [Test]
        public void StepsOutOfRangeAreClamped()
        {
            Assert.AreEqual(0, QualityLadder.Plan(-4).step);
            Assert.AreEqual(QualityLadder.MaxStep, QualityLadder.Plan(99).step);
        }

        [Test]
        public void HeatAndPowerSavingPickTheBiggestStep()
        {
            Assert.AreEqual(0, QualityLadder.StepFor(ThermalLevel.Nominal, false, false, false));
            Assert.AreEqual(1, QualityLadder.StepFor(ThermalLevel.Nominal, true, false, false));
            Assert.AreEqual(1, QualityLadder.StepFor(ThermalLevel.Nominal, false, true, false));
            Assert.AreEqual(1, QualityLadder.StepFor(ThermalLevel.Nominal, false, false, true));
            Assert.AreEqual(2, QualityLadder.StepFor(ThermalLevel.Serious, true, true, true));
            Assert.AreEqual(3, QualityLadder.StepFor(ThermalLevel.Critical, false, false, false));
        }
    }

    public class BatterySaverTests
    {
        [Test]
        public void TheHintComesUnder20PercentOrWhenTheOsSaves()
        {
            var s = new PowerSettings();
            Assert.IsTrue(BatterySaverPolicy.ShouldSuggest(new PowerInputs { batteryPercent = 19 }, s, 100));
            Assert.IsFalse(BatterySaverPolicy.ShouldSuggest(new PowerInputs { batteryPercent = 20 }, s, 100));
            Assert.IsFalse(BatterySaverPolicy.ShouldSuggest(new PowerInputs { batteryPercent = 5, charging = true }, s, 100));
            Assert.IsFalse(BatterySaverPolicy.ShouldSuggest(new PowerInputs { batteryPercent = -1 }, s, 100));
            Assert.IsTrue(BatterySaverPolicy.ShouldSuggest(new PowerInputs { batteryPercent = 90, osPowerSave = true }, s, 100));
        }

        [Test]
        public void ItIsNotAskedTwiceInADayOrWhenAlreadyOn()
        {
            var s = new PowerSettings { saverHintDeclinedDay = 100 };
            var low = new PowerInputs { batteryPercent = 10 };
            Assert.IsFalse(BatterySaverPolicy.ShouldSuggest(low, s, 100));
            Assert.IsTrue(BatterySaverPolicy.ShouldSuggest(low, s, 101));
            Assert.IsFalse(BatterySaverPolicy.ShouldSuggest(low, new PowerSettings { batterySaver = true }, 101));
        }

        [Test]
        public void TheDefaultsSaveTheMost()
        {
            var s = new PowerSettings();
            Assert.IsFalse(s.prefer60);
            Assert.IsFalse(s.mobileData);
            Assert.IsFalse(s.batterySaver);
            Assert.IsFalse(new SaveData().power.prefer60);
        }
    }

    public class PoolTests
    {
        private class Thing { public bool active; }

        [Test]
        public void ThingsComeBackInsteadOfBeingMadeAgain()
        {
            var pool = new Pool<Thing>(() => new Thing(), 4, t => t.active = true, t => t.active = false);
            var a = pool.Get();
            Assert.IsTrue(a.active);
            pool.Release(a);
            Assert.IsFalse(a.active);
            Assert.AreSame(a, pool.Get());
            Assert.AreEqual(1, pool.Created);
            Assert.AreEqual(1, pool.Reused);
        }

        [Test]
        public void OnlyMaxSizeAreKeptAndTheRestAreDropped()
        {
            var dropped = new List<Thing>();
            var pool = new Pool<Thing>(() => new Thing(), 2, null, null, dropped.Add);
            var items = Enumerable.Range(0, 5).Select(_ => pool.Get()).ToList();
            foreach (var i in items) pool.Release(i);
            Assert.AreEqual(2, pool.Free);
            Assert.AreEqual(3, pool.Dropped);
            Assert.AreEqual(3, dropped.Count);
            Assert.AreEqual(0, pool.InUse);
        }

        [Test]
        public void PrewarmFillsUpToTheLimit()
        {
            var pool = new Pool<Thing>(() => new Thing(), 3);
            pool.Prewarm(10);
            Assert.AreEqual(3, pool.Free);
            Assert.AreEqual(3, pool.Created);
            pool.Prewarm(2);
            Assert.AreEqual(3, pool.Created, "a second prewarm does not make more");
        }

        [Test]
        public void TrimGivesMemoryBack()
        {
            int dropped = 0;
            var pool = new Pool<Thing>(() => new Thing(), 8, null, null, _ => dropped++);
            pool.Prewarm(8);
            Assert.AreEqual(6, pool.Trim(2));
            Assert.AreEqual(2, pool.Free);
            Assert.AreEqual(6, dropped);
        }

        [Test]
        public void GettingAndReleasingAfterPrewarmAllocatesNothing()
        {
            var pool = new Pool<Thing>(() => new Thing(), 8, t => t.active = true, t => t.active = false);
            pool.Prewarm(8);
            AllocationCheck.AssertNoAllocation(() => { var t = pool.Get(); pool.Release(t); }, "Pool.Get/Release");
            Assert.AreEqual(8, pool.Created);
        }

        [Test]
        public void NullAndExtraReleasesAreHarmless()
        {
            var pool = new Pool<Thing>(() => new Thing(), 2);
            Assert.DoesNotThrow(() => pool.Release(null));
            Assert.DoesNotThrow(() => pool.Release(new Thing()));
            Assert.AreEqual(0, pool.InUse);
        }

        [Test]
        public void TheSchedulerReleasesOnlyWhatIsDue()
        {
            var s = new ReleaseScheduler<Thing>(4);
            var a = new Thing(); var b = new Thing(); var c = new Thing();
            s.Schedule(a, 1f); s.Schedule(b, 3f); s.Schedule(c, 2f);
            var got = new List<Thing>();
            Assert.AreEqual(0, s.Collect(0.5f, got.Add));
            Assert.AreEqual(2, s.Collect(2.5f, got.Add));
            CollectionAssert.AreEquivalent(new[] { a, c }, got);
            Assert.AreEqual(1, s.Count);
            Assert.AreEqual(1, s.Collect(9f, got.Add));
            Assert.AreEqual(0, s.Count, "the owner can switch its update off when this is zero");
        }

        [Test]
        public void TheSchedulerGrowsAndCollectingAllocatesNothing()
        {
            var s = new ReleaseScheduler<Thing>(4);
            for (int i = 0; i < 50; i++) s.Schedule(new Thing(), i);
            Assert.AreEqual(50, s.Count);
            var t = new Thing();
            Action<Thing> noop = _ => { };
            AllocationCheck.AssertNoAllocation(() => { s.Schedule(t, 0f); s.Collect(1f, noop); }, "ReleaseScheduler");
        }
    }

    public class CacheRegistryTests
    {
        [Test]
        public void CheapCachesGoFirstAndCriticalOnlyOnesWaitForCritical()
        {
            var order = new List<string>();
            var r = new CacheRegistry();
            r.Register("textures", 5, () => { order.Add("textures"); return 3; }, true);
            r.Register("pools", 1, () => { order.Add("pools"); return 10; });
            r.Register("signs", 2, () => { order.Add("signs"); return 1; });
            Assert.AreEqual(11, r.TrimAll(TrimLevel.Moderate));
            CollectionAssert.AreEqual(new[] { "pools", "signs" }, order);
            order.Clear();
            Assert.AreEqual(14, r.TrimAll(TrimLevel.Critical));
            CollectionAssert.AreEqual(new[] { "pools", "signs", "textures" }, order);
        }

        [Test]
        public void ABrokenCacheDoesNotStopTheOthers()
        {
            var r = new CacheRegistry();
            r.Register("bad", 1, () => throw new InvalidOperationException());
            r.Register("ok", 2, () => 4);
            Assert.AreEqual(4, r.TrimAll(TrimLevel.Critical));
        }

        [Test]
        public void RegisteringTheSameNameReplacesIt()
        {
            var r = new CacheRegistry();
            r.Register("a", 1, () => 1); r.Register("a", 1, () => 2);
            Assert.AreEqual(1, r.Count);
            Assert.AreEqual(2, r.TrimAll(TrimLevel.Moderate));
        }
    }

    public class WatchdogTests
    {
        [Test]
        public void AFreezeOverTwoSecondsIsReportedOnce()
        {
            var d = new FreezeDetector();
            d.Beat(1000);
            Assert.IsFalse(d.Check(2900, out _));
            Assert.IsTrue(d.Check(3100, out long ms));
            Assert.AreEqual(2100, ms);
            Assert.IsFalse(d.Check(4100, out _), "the same stall is not reported twice");
            d.Beat(5000);
            Assert.IsTrue(d.Check(7200, out _), "a new stall after a heartbeat is a new report");
            Assert.AreEqual(2, d.Freezes);
        }

        [Test]
        public void LoadsAndBackgroundNeverCount()
        {
            var d = new FreezeDetector();
            d.Beat(0);
            d.Suspend();
            Assert.IsFalse(d.Check(60000, out _));
            d.Resume(60000);
            Assert.IsFalse(d.Check(61500, out _), "the clock restarts when the pause ends");
            Assert.IsTrue(d.Check(62100, out _));
        }

        [Test]
        public void NestedSuspendsNeedMatchingResumes()
        {
            var d = new FreezeDetector();
            d.Suspend(); d.Suspend();
            d.Resume(10);
            Assert.IsTrue(d.Suspended);
            d.Resume(20);
            Assert.IsFalse(d.Suspended);
        }

        [Test]
        public void HeartbeatsAndChecksAllocateNothing()
        {
            var d = new FreezeDetector();
            long t = 0;
            AllocationCheck.AssertNoAllocation(() => { t += 33; d.Beat(t); d.Check(t, out _); }, "FreezeDetector");
        }

        [Test]
        public void BreadcrumbsKeepTheLastOnesInOrder()
        {
            var b = new Breadcrumbs(3);
            for (int i = 1; i <= 5; i++) b.Add("step" + i, i * 1000);
            string dump = b.Dump(6000);
            Assert.AreEqual(3, b.Count);
            StringAssert.DoesNotContain("step2", dump);
            Assert.Less(dump.IndexOf("step3"), dump.IndexOf("step5"));
            StringAssert.Contains("-3.0s step3", dump);
        }

        [Test]
        public void BreadcrumbDumpIsCapped()
        {
            var b = new Breadcrumbs(30);
            for (int i = 0; i < 30; i++) b.Add(new string('x', 200), i);
            Assert.LessOrEqual(b.Dump(100, 500).Length, 500);
        }
    }

    public class PerfBudgetTests
    {
        [Test]
        public void EveryTierHasBudgetsThatFitItsFrame()
        {
            foreach (var b in PerfBudgets.All)
            {
                Assert.Less(b.cpuMs, b.frameMs * 0.6f, b.tier + "@" + b.fps + ": CPU leaves room to idle");
                Assert.Less(b.gpuMs, b.frameMs, b.tier + "@" + b.fps + ": GPU fits the frame");
            }
            Assert.AreEqual(33.3f, PerfBudgets.For(QualityTier.Low, 30).frameMs, 0.05f);
            Assert.AreEqual(16.7f, PerfBudgets.For(QualityTier.High, 60).frameMs, 0.05f);
            Assert.AreEqual(30, PerfBudgets.For(QualityTier.Medium, 60).fps, "only High has a 60 budget");
        }

        [Test]
        public void BudgetsGrowWithTheTier()
        {
            var l = PerfBudgets.For(QualityTier.Low, 30); var m = PerfBudgets.For(QualityTier.Medium, 30); var h = PerfBudgets.For(QualityTier.High, 30);
            Assert.Less(l.drawCalls, m.drawCalls); Assert.Less(m.drawCalls, h.drawCalls);
            Assert.Less(l.memoryMB, m.memoryMB); Assert.Less(m.memoryMB, h.memoryMB);
        }

        [Test]
        public void CheckListsOnlyWhatIsOver()
        {
            var b = PerfBudgets.For(QualityTier.Low, 30);
            Assert.IsEmpty(PerfBudgets.Check(b, new PerfMeasured { averageFrameMs = 33f, onePercentLowFps = 26, drawCalls = 100, trianglesK = 100, memoryMB = 500 }));
            var bad = PerfBudgets.Check(b, new PerfMeasured { averageFrameMs = 45f, onePercentLowFps = 10, drawCalls = 400, trianglesK = 900, memoryMB = 2000 });
            Assert.AreEqual(5, bad.Count);
        }

        [Test]
        public void TheDocumentListsEveryBudgetNumber()
        {
            var doc = File.ReadAllText(Path.Combine(TestPaths.Root(), "docs", "power_and_data.md"));
            foreach (var b in PerfBudgets.All)
            {
                StringAssert.Contains(b.FrameText, doc);
                StringAssert.Contains(b.drawCalls.ToString(), doc);
                StringAssert.Contains(b.memoryMB.ToString(), doc);
            }
        }
    }

    internal static class TestPaths
    {
        public static string Root()
        {
            for (var d = new DirectoryInfo(TestContext.CurrentContext.TestDirectory); d != null; d = d.Parent)
                if (Directory.Exists(Path.Combine(d.FullName, "Assets", "_Aron")) && Directory.Exists(Path.Combine(d.FullName, "ProjectSettings"))) return d.FullName;
            Assert.Fail("project root not found");
            return null;
        }
    }
}
