// (c) Ahmed Alamrani. All rights reserved.
using System.IO;
using System.Linq;
using Aron.Logic;
using Aron.Logic.Content;
using Aron.Logic.Localization;
using Aron.Logic.Regions;
using NUnit.Framework;

namespace Aron.Logic.Tests
{
    /// <summary>On-demand country music: what is in the base install, which group a stage needs, and when a download may start.</summary>
    public class ContentDownloadTests
    {
        private static RegionCatalog Catalog() => TestData.Catalog();
        private static string Remote() => Path.GetFullPath(Path.Combine(TestData.Resources(), "..", "Remote", "Music"));

        [Test]
        public void TheMapIsSoundAndEveryCountryTrackHasOneGroup()
        {
            var map = ContentMap.Build(Catalog());
            Assert.That(map.Problems, Is.Empty);
            Assert.That(map.Groups.Count, Is.GreaterThanOrEqualTo(30));
            foreach (var g in map.Groups) { Assert.That(map.TracksOf(g), Is.Not.Empty, g); StringAssert.StartsWith("country_", g); }
            foreach (var t in MusicCues.All(Catalog()))
                Assert.That(map.GroupOfTrack(t) == null, Is.EqualTo(ContentMap.IsBaseTrack(t)), t);
        }

        [Test]
        public void TheBaseHoldsMenusStagesStingersAndRiyadhOnly()
        {
            foreach (var t in MusicCues.Generic) Assert.That(ContentMap.IsBaseTrack(t), Is.True, t);
            Assert.That(ContentMap.IsBaseTrack(MusicCues.SaudiNajd), Is.True);
            foreach (var t in new[] { MusicCues.SaudiHejaz, MusicCues.SaudiSouth, MusicCues.SaudiEastern, MusicCues.SaudiNorth, MusicCues.TabukBoss, MusicCues.KuwaitSea, MusicCues.Country("egypt") })
                Assert.That(ContentMap.IsBaseTrack(t), Is.False, t);
            var map = ContentMap.Build(Catalog());
            Assert.That(map.GroupOfTrack(MusicCues.SaudiHejaz), Is.EqualTo("country_saudi"));
            Assert.That(map.GroupOfTrack(MusicCues.KuwaitSea), Is.EqualTo("country_kuwait"));
            Assert.That(map.GroupOfTrack(MusicCues.Country("egypt")), Is.EqualTo("country_egypt"));
        }

        [Test]
        public void RiyadhStagesNeedNothingDownloaded()
        {
            var cat = Catalog(); var map = ContentMap.Build(cat);
            var riyadh = cat.regions.First(r => r.id == "riyadh");
            foreach (var s in riyadh.stages) Assert.That(map.GroupsForStage(s, riyadh), Is.Empty, s.id);
        }

        [Test]
        public void ABossStageNeedsItsOwnBossTrackGroup()
        {
            var cat = Catalog(); var map = ContentMap.Build(cat);
            var tabuk = cat.regions.First(r => r.id == "tabuk");
            var boss = tabuk.stages.First(s => s.id == "tabuk_7");
            Assert.That(map.GroupsForStage(boss, tabuk), Is.EqualTo(new[] { "country_saudi" }));
            var egypt = cat.regions.First(r => r.id == "egypt");
            Assert.That(map.GroupsForStage(egypt.stages[0], egypt).Length, Is.LessThanOrEqualTo(1));
        }

        [Test]
        public void EveryGroupHasItsFilesOnDiskAndNothingElseIsThere()
        {
            var map = ContentMap.Build(Catalog());
            var dirs = Directory.GetDirectories(Remote()).Select(Path.GetFileName).OrderBy(x => x).ToArray();
            Assert.That(dirs, Is.EqualTo(map.Groups.OrderBy(x => x).ToArray()));
            foreach (var g in map.Groups)
            {
                var files = Directory.GetFiles(Path.Combine(Remote(), g), "*.ogg").Select(Path.GetFileNameWithoutExtension).OrderBy(x => x).ToArray();
                Assert.That(files, Is.EqualTo(map.TracksOf(g).OrderBy(x => x).ToArray()), g);
            }
        }

        [Test]
        public void PrefetchLooksAtTheFrontierAndTheNextFewStagesOnly()
        {
            var cat = Catalog(); var map = ContentMap.Build(cat);
            var first = cat.regions[0].stages[0];
            var none = map.GroupsToPrefetch(cat, first, 0);
            var some = map.GroupsToPrefetch(cat, first, 40);
            Assert.That(some.Length, Is.GreaterThanOrEqualTo(none.Length));
            var egypt = cat.regions.First(r => r.id == "egypt");
            Assert.That(map.GroupsToPrefetch(cat, egypt.stages[0], 0), Is.SubsetOf(map.GroupsForStage(egypt.stages[0], egypt)));
        }

        [Test]
        public void FallbackIsAlwaysATrackInTheBase()
        {
            var cat = Catalog(); var map = ContentMap.Build(cat);
            foreach (var t in MusicCues.All(cat)) Assert.That(ContentMap.IsBaseTrack(ContentMap.Fallback(t)), Is.True, t);
        }

        // ------------------------------------------------------------------ policy

        [TestCase(NetworkKind.Wifi, false, false, false, true, DownloadVerdict.Go)]
        [TestCase(NetworkKind.Wifi, false, false, false, false, DownloadVerdict.Go)]
        [TestCase(NetworkKind.Cellular, false, false, false, true, DownloadVerdict.AskCellular)]
        [TestCase(NetworkKind.Cellular, false, false, false, false, DownloadVerdict.WaitForWifi)]
        [TestCase(NetworkKind.Cellular, true, false, false, false, DownloadVerdict.Go)]
        [TestCase(NetworkKind.Cellular, false, true, false, true, DownloadVerdict.Go)]
        [TestCase(NetworkKind.None, true, true, false, true, DownloadVerdict.Offline)]
        [TestCase(NetworkKind.Wifi, true, true, true, true, DownloadVerdict.BlockedDuringGameplay)]
        [TestCase(NetworkKind.Wifi, true, true, true, false, DownloadVerdict.BlockedDuringGameplay)]
        public void PolicyDecides(NetworkKind net, bool optIn, bool confirmed, bool gameplay, bool interactive, DownloadVerdict expected)
            => Assert.That(DownloadPolicy.Decide(true, net, optIn, confirmed, gameplay, interactive), Is.EqualTo(expected));

        [Test]
        public void NothingToDownloadIsNotNeededEvenInGameplay()
            => Assert.That(DownloadPolicy.Decide(false, NetworkKind.Cellular, false, false, true, true), Is.EqualTo(DownloadVerdict.NotNeeded));

        [Test]
        public void TheDefaultSaveIsWifiOnly()
        {
            Assert.That(new SaveData().cellularContent, Is.False);
            Assert.That(DownloadPolicy.Decide(true, NetworkKind.Cellular, new SaveData().cellularContent, false, false, true), Is.EqualTo(DownloadVerdict.AskCellular));
        }

        [Test]
        public void SizesAreShownInPlainUnits()
        {
            Assert.That(DownloadPolicy.FormatBytes(512), Is.EqualTo("512 B"));
            Assert.That(DownloadPolicy.FormatBytes(2048), Is.EqualTo("2 KB"));
            Assert.That(DownloadPolicy.FormatBytes(3355443), Is.EqualTo("3.2 MB"));
        }

        // ------------------------------------------------------------------ progress screen model

        [Test]
        public void AWifiDownloadRunsToDone()
        {
            var s = new DownloadSession();
            s.Start(DownloadVerdict.Go, 1000);
            Assert.That(s.Phase, Is.EqualTo(DownloadPhase.Downloading));
            s.Report(250); Assert.That(s.Progress, Is.EqualTo(0.25f).Within(1e-4));
            s.Report(100); Assert.That(s.DoneBytes, Is.EqualTo(250), "progress never goes back");
            s.Complete();
            Assert.That(s.Phase, Is.EqualTo(DownloadPhase.Done)); Assert.That(s.Progress, Is.EqualTo(1f)); Assert.That(s.Finished, Is.True);
        }

        [Test]
        public void MobileDataWaitsForTheAnswer()
        {
            var s = new DownloadSession();
            s.Start(DownloadVerdict.AskCellular, 5000);
            Assert.That(s.Phase, Is.EqualTo(DownloadPhase.AskingCellular));
            s.Report(100); Assert.That(s.DoneBytes, Is.EqualTo(0), "nothing moves before the player agrees");
            Assert.That(s.ConfirmCellular(), Is.True);
            Assert.That(s.Phase, Is.EqualTo(DownloadPhase.Downloading));
            Assert.That(s.ConfirmCellular(), Is.False);
        }

        [Test]
        public void SkipWorksAfterAFailureButNotAfterTheContentArrived()
        {
            var a = new DownloadSession(); a.Start(DownloadVerdict.Offline, 0); a.Skip();
            Assert.That(a.Phase, Is.EqualTo(DownloadPhase.Skipped));
            var b = new DownloadSession(); b.Start(DownloadVerdict.Go, 10); b.Complete(); b.Skip();
            Assert.That(b.Phase, Is.EqualTo(DownloadPhase.Done));
        }

        [Test]
        public void SkipAndFailureEndTheScreen()
        {
            var a = new DownloadSession(); a.Start(DownloadVerdict.AskCellular, 10); a.Skip();
            Assert.That(a.Phase, Is.EqualTo(DownloadPhase.Skipped));
            var b = new DownloadSession(); b.Start(DownloadVerdict.Offline, 0);
            Assert.That(b.Phase, Is.EqualTo(DownloadPhase.Failed));
            var c = new DownloadSession(); c.Start(DownloadVerdict.BlockedDuringGameplay, 10);
            Assert.That(c.Phase, Is.EqualTo(DownloadPhase.Failed));
            var d = new DownloadSession(); d.Start(DownloadVerdict.Go, 10); d.Fail("timeout"); Assert.That(d.Error, Is.EqualTo("timeout"));
            var e = new DownloadSession(); e.Start(DownloadVerdict.NotNeeded, 0); Assert.That(e.Phase, Is.EqualTo(DownloadPhase.Done));
        }

        [Test]
        public void EveryLanguageHasTheDownloadScreenText()
        {
            foreach (var l in UiLanguages.Codes)
                foreach (var k in new[] { "content_title", "content_progress", "content_size", "content_ask_cellular", "content_use_cellular", "content_wait_wifi", "content_offline", "content_retry", "content_skip", "content_cellular_setting" })
                    Assert.That(UiStringTables.All[l].TryGetValue(k, out var v) && !string.IsNullOrWhiteSpace(v), Is.True, l + " " + k);
            foreach (var l in UiLanguages.Codes)
            {
                StringAssert.Contains("{0}", UiStringTables.All[l]["content_progress"], l);
                StringAssert.Contains("{0}", UiStringTables.All[l]["content_size"], l);
            }
        }
    }
}
