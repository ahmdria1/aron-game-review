// (c) Ahmed Alamrani. All rights reserved.
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Aron.Content;
using Aron.Logic;
using Aron.Logic.Content;
using Aron.Logic.Regions;
using Aron.Stages;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace Aron.Tests.PlayMode
{
    /// <summary>
    /// The download rules end to end with a fake host: Wi-Fi only unless allowed, never in a stage, cancel when a stage starts,
    /// the progress screen asks before using mobile data, and a missing country group falls back to the base music.
    /// </summary>
    public class ContentDownloadFlowTests
    {
        private sealed class FakeBackend : IContentBackend
        {
            public bool Available { get; set; } = true;
            public readonly HashSet<string> OnDevice = new HashSet<string>();
            public readonly List<string> Fetched = new List<string>();
            public int FramesPerFetch = 3;
            public bool Cancelled;
            public long Bytes = 3 * 1024 * 1024;

            public IEnumerator SizeOf(string group, Action<long> result) { yield return null; result(OnDevice.Contains(group) ? 0 : Bytes); }

            public IEnumerator Fetch(string group, Action<float> progress, Action<string> done)
            {
                Cancelled = false; Fetched.Add(group);
                for (int i = 0; i < FramesPerFetch; i++)
                {
                    if (Cancelled) { done("cancelled"); yield break; }
                    progress?.Invoke((float)i / FramesPerFetch);
                    yield return null;
                }
                OnDevice.Add(group); progress?.Invoke(1f); done(null);
            }

            public IEnumerator LoadMusic(string group, IReadOnlyList<string> tracks, Action<string, AudioClip> loaded, Action<string> done)
            {
                var any = Resources.Load<AudioClip>("Audio/Music/music_menu");   // a real shipped clip stands in for the downloaded one
                foreach (var t in tracks) loaded(t, any);
                yield return null;
                done(null);
            }

            public void Cancel() { Cancelled = true; }
        }

        private FakeBackend fake;
        private string group, track;
        private StageDef stage;
        private RegionDef region;
        private NetworkKind network;
        private bool cellularOptIn, inGameplay;

        [SetUp]
        public void SetUp()
        {
            ContentService.ResetForTests();
            fake = new FakeBackend();
            ContentService.Backend = fake;
            network = NetworkKind.Wifi; cellularOptIn = false; inGameplay = false;
            ContentService.Network = () => network;
            ContentService.CellularOptIn = () => cellularOptIn;
            ContentService.InGameplay = () => inGameplay;

            // A real stage whose music is not in the base install.
            var cat = StageSession.Catalog;
            foreach (var r in cat.regions)
                foreach (var s in r.stages)
                {
                    var groups = ContentService.Map.GroupsForStage(s, r);
                    if (groups.Length > 0 && stage == null) { stage = s; region = r; group = groups[0]; track = ContentService.Map.TracksOf(group)[0]; }
                }
            Assert.IsNotNull(stage, "no stage with country music found");
        }

        [TearDown]
        public void TearDown()
        {
            if (ContentProgressScreen.Current != null) UnityEngine.Object.Destroy(ContentProgressScreen.Current.gameObject);
            ContentService.ResetForTests();
            StageSession.Clear();
        }

        private IEnumerator Prefetch(params string[] groups)
        {
            yield return ContentService.Prefetch(groups);
        }

        [UnityTest]
        public IEnumerator WifiPrefetchDownloadsAndLoadsTheGroup()
        {
            yield return Prefetch(group);
            Assert.AreEqual(1, fake.Fetched.Count);
            Assert.IsTrue(ContentService.IsLoaded(group));
            Assert.IsNotNull(ContentService.TryGetMusic(track));
            Assert.AreEqual(track, ContentService.PlayableTrack(track));
        }

        [UnityTest]
        public IEnumerator MobileDataWithoutPermissionDownloadsNothing()
        {
            network = NetworkKind.Cellular;
            yield return Prefetch(group);
            Assert.AreEqual(0, fake.Fetched.Count);
            Assert.IsFalse(ContentService.IsLoaded(group));
        }

        [UnityTest]
        public IEnumerator MobileDataWithSavedPermissionDownloads()
        {
            network = NetworkKind.Cellular; cellularOptIn = true;
            yield return Prefetch(group);
            Assert.AreEqual(1, fake.Fetched.Count);
            Assert.IsTrue(ContentService.IsLoaded(group));
        }

        [UnityTest]
        public IEnumerator NothingDownloadsWhileAStageIsRunning()
        {
            inGameplay = true;
            yield return Prefetch(group);
            Assert.AreEqual(0, fake.Fetched.Count, "a download started during gameplay");
            Assert.IsFalse(ContentService.IsLoaded(group));
        }

        [UnityTest]
        public IEnumerator ADownloadInProgressStopsTheMomentAStageStarts()
        {
            fake.FramesPerFetch = 400;
            var run = ContentService.Prefetch(new[] { group });
            var co = ContentServiceTestHost.Run(run);
            yield return null; yield return null;
            Assert.AreEqual(1, fake.Fetched.Count, "the download should have begun on Wi-Fi");
            inGameplay = true;
            ContentService.StageStarting();
            float until = Time.realtimeSinceStartup + 3f;
            while (!fake.Cancelled && Time.realtimeSinceStartup < until) yield return null;
            Assert.IsTrue(fake.Cancelled, "the download was not cancelled when the stage started");
            Assert.IsFalse(ContentService.IsLoaded(group));
            ContentServiceTestHost.Stop(co);
        }

        [Test]
        public void AMissingGroupPlaysTheBaseTrackInstead()
        {
            string played = ContentService.PlayableTrack(track);
            Assert.AreNotEqual(track, played);
            Assert.IsFalse(ContentService.IsRemoteTrack(played), "the fallback must be a base-install track");
        }

        [Test]
        public void BaseTracksAreNeverRemote()
        {
            Assert.IsFalse(ContentService.IsRemoteTrack(MusicCues.Menu));
            Assert.IsFalse(ContentService.IsRemoteTrack(MusicCues.SaudiNajd));
            Assert.AreEqual(MusicCues.Menu, ContentService.PlayableTrack(MusicCues.Menu));
        }

        private static Button FindButton(string name)
        {
            var screen = ContentProgressScreen.Current;
            if (screen == null) return null;
            foreach (var b in screen.GetComponentsInChildren<Button>(true)) if (b.name == name) return b;
            return null;
        }

        [UnityTest]
        public IEnumerator PlayingAStageWithMissingMusicOnMobileDataAsksBeforeDownloading()
        {
            network = NetworkKind.Cellular;
            StageSession.Play(stage.id);
            for (int i = 0; i < 20 && (ContentProgressScreen.Current == null || ContentProgressScreen.Current.Phase != DownloadPhase.AskingCellular); i++) yield return null;
            Assert.IsNotNull(ContentProgressScreen.Current, "the progress screen did not open");
            Assert.AreEqual(DownloadPhase.AskingCellular, ContentProgressScreen.Current.Phase);
            Assert.AreEqual(0, fake.Fetched.Count, "downloaded before the player agreed to mobile data");
            Assert.IsTrue(FindButton("UseCellular").gameObject.activeSelf);
            Assert.IsTrue(FindButton("Wait").gameObject.activeSelf);
            Assert.IsTrue(FindButton("Skip").gameObject.activeSelf);

            FindButton("Wait").onClick.Invoke();   // back to the map without starting the stage
            yield return null;
            Assert.IsNull(ContentProgressScreen.Current);
            Assert.IsFalse(StageSession.Active, "waiting for Wi-Fi must not leave a stage selected");
            Assert.AreEqual(0, fake.Fetched.Count);
        }

        [UnityTest]
        public IEnumerator TheProgressScreenDownloadsOnceMobileDataIsAllowedAndThenProceeds()
        {
            network = NetworkKind.Cellular;
            bool proceeded = false;
            ContentProgressScreen.Open(null, new[] { group }, () => proceeded = true, () => Assert.Fail("cancelled"));
            for (int i = 0; i < 20 && ContentProgressScreen.Current.Phase != DownloadPhase.AskingCellular; i++) yield return null;
            Assert.AreEqual(DownloadPhase.AskingCellular, ContentProgressScreen.Current.Phase);
            FindButton("UseCellular").onClick.Invoke();
            float until = Time.realtimeSinceStartup + 5f;
            while (!proceeded && Time.realtimeSinceStartup < until) yield return null;
            Assert.IsTrue(proceeded, "the stage did not start after the download");
            Assert.AreEqual(1, fake.Fetched.Count);
            Assert.IsTrue(ContentService.IsLoaded(group));
        }

        [UnityTest]
        public IEnumerator SkippingStartsTheStageWithoutDownloadingAnything()
        {
            network = NetworkKind.Cellular;
            bool proceeded = false;
            ContentProgressScreen.Open(null, new[] { group }, () => proceeded = true, () => { });
            for (int i = 0; i < 20 && ContentProgressScreen.Current.Phase != DownloadPhase.AskingCellular; i++) yield return null;
            FindButton("Skip").onClick.Invoke();
            for (int i = 0; i < 10 && !proceeded; i++) yield return null;
            Assert.IsTrue(proceeded);
            Assert.AreEqual(0, fake.Fetched.Count);
            Assert.AreNotEqual(track, ContentService.PlayableTrack(track), "without the download the base track plays");
        }

        [UnityTest]
        public IEnumerator OfflineShowsRetryAndSkipAndNeverStartsADownload()
        {
            network = NetworkKind.None;
            bool proceeded = false;
            ContentProgressScreen.Open(null, new[] { group }, () => proceeded = true, () => { });
            for (int i = 0; i < 20 && ContentProgressScreen.Current.Phase != DownloadPhase.Failed; i++) yield return null;
            Assert.AreEqual(DownloadPhase.Failed, ContentProgressScreen.Current.Phase);
            Assert.IsTrue(FindButton("Retry").gameObject.activeSelf);
            Assert.AreEqual(0, fake.Fetched.Count);
            FindButton("Skip").onClick.Invoke();
            for (int i = 0; i < 10 && !proceeded; i++) yield return null;
            Assert.IsTrue(proceeded);
        }

        [UnityTest]
        public IEnumerator AlreadyDownloadedGroupsOnlyLoadAndShowNoScreen()
        {
            fake.OnDevice.Add(group);
            bool proceeded = false;
            ContentProgressScreen.Open(null, new[] { group }, () => proceeded = true, () => { });
            for (int i = 0; i < 20 && !proceeded; i++) yield return null;
            Assert.IsTrue(proceeded);
            Assert.AreEqual(0, fake.Fetched.Count);
            Assert.IsTrue(ContentService.IsLoaded(group));
        }
    }

    /// <summary>Runs a coroutine from a test without a MonoBehaviour of its own.</summary>
    internal static class ContentServiceTestHost
    {
        private sealed class H : MonoBehaviour { }
        private static H host;
        public static Coroutine Run(IEnumerator e)
        {
            if (host == null) host = new GameObject("ContentTestHost").AddComponent<H>();
            return host.StartCoroutine(e);
        }
        public static void Stop(Coroutine c) { if (host != null) { host.StopCoroutine(c); UnityEngine.Object.Destroy(host.gameObject); host = null; } }
    }
}
