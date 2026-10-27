// (c) Ahmed Alamrani. All rights reserved.
using System;
using System.Collections;
using System.Collections.Generic;
using Aron.Core;
using Aron.Logic.Content;
using Aron.Logic.Regions;
using Aron.Save;
using Aron.Stages;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Aron.Content
{
    /// <summary>
    /// On-demand country music. The base install has the menus, the stage-type tracks and Riyadh/Diriyah's theme; every other
    /// country's tracks sit in one downloadable group each. The rules (see <see cref="DownloadPolicy"/>):
    /// Wi-Fi only unless the player allows mobile data, never while a stage is running, and a missing group only costs the
    /// country theme (the base track plays instead), never the game.
    /// </summary>
    public static class ContentService
    {
        /// <summary>Set by the Addressables assembly at start-up; null in builds without it.</summary>
        public static Func<IContentBackend> BackendFactory;

        private static IContentBackend backend;
        private static bool backendCreated;
        private static ContentMap map;
        private static readonly Dictionary<string, AudioClip> clips = new Dictionary<string, AudioClip>(StringComparer.Ordinal);
        private static readonly HashSet<string> loaded = new HashSet<string>(StringComparer.Ordinal);
        private static Runner runner;
        private static bool prefetching;

        /// <summary>Where the network state comes from; tests replace it.</summary>
        public static Func<NetworkKind> Network = DefaultNetwork;
        /// <summary>True while a stage is being played; tests replace it.</summary>
        public static Func<bool> InGameplay = DefaultInGameplay;
        /// <summary>The saved "download over mobile data" setting; tests replace it.</summary>
        public static Func<bool> CellularOptIn = () => SaveSystem.Data != null && SaveSystem.Data.cellularContent;

        public static IContentBackend Backend
        {
            get
            {
                if (!backendCreated) { backend = BackendFactory != null ? BackendFactory() : null; backendCreated = true; }
                return backend;
            }
            set { backend = value; backendCreated = true; }
        }

        public static bool CanDownload => Backend != null && Backend.Available;

        public static ContentMap Map
        {
            get => map ?? (map = ContentMap.Build(StageSession.Catalog));
            set => map = value;
        }

        public static NetworkKind DefaultNetwork()
        {
            switch (Application.internetReachability)
            {
                case NetworkReachability.ReachableViaLocalAreaNetwork: return NetworkKind.Wifi;
                case NetworkReachability.ReachableViaCarrierDataNetwork: return NetworkKind.Cellular;
                default: return NetworkKind.None;
            }
        }

        private static bool DefaultInGameplay()
        {
            string scene = SceneManager.GetActiveScene().name;
            if (scene != SceneNames.Stage && scene != SceneNames.Riyadh) return false;
            var gm = GameManager.Instance;
            return gm != null && !gm.IsOver;
        }

        // ---------------------------------------------------------------- what is on the device

        public static bool IsLoaded(string group) => loaded.Contains(group);

        /// <summary>The clip of a downloaded track, or null (not downloaded yet, or a base track).</summary>
        public static AudioClip TryGetMusic(string track) => clips.TryGetValue(track, out var c) ? c : null;

        /// <summary>True when the track is part of a country group rather than the base install.</summary>
        public static bool IsRemoteTrack(string track) => Map.GroupOfTrack(track) != null;

        /// <summary>Groups this stage needs that are not in memory yet.</summary>
        public static string[] MissingFor(StageDef stage, RegionDef region)
        {
            var need = Map.GroupsForStage(stage, region);
            var list = new List<string>();
            foreach (var g in need) if (!loaded.Contains(g)) list.Add(g);
            return list.ToArray();
        }

        /// <summary>The track to play: the real one when it is on the device, else the base track for that kind of stage.</summary>
        public static string PlayableTrack(string track)
        {
            if (string.IsNullOrEmpty(track) || !IsRemoteTrack(track)) return track;
            return clips.ContainsKey(track) ? track : ContentMap.Fallback(track);
        }

        public static void ResetForTests()
        {
            backend = null; backendCreated = false; map = null; clips.Clear(); loaded.Clear(); prefetching = false;
            Network = DefaultNetwork; InGameplay = DefaultInGameplay;
            CellularOptIn = () => SaveSystem.Data != null && SaveSystem.Data.cellularContent;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics() { runner = null; ResetForTests(); }

        // ---------------------------------------------------------------- downloading

        /// <summary>Handed to <see cref="Download"/> so the progress screen can say "the player just agreed to mobile data".</summary>
        public sealed class Consent { public bool Cellular; }

        /// <summary>
        /// Downloads one group and loads its tracks. The rules are checked again every half second: if the network turns to mobile data
        /// without consent, or a stage starts, the download stops at once. <paramref name="done"/> gets null on success or a reason.
        /// </summary>
        public static IEnumerator Download(string group, bool interactive, Consent consent, Action<float> progress, Action<string> done)
        {
            var b = Backend;
            if (b == null || !b.Available) { done?.Invoke("no content host"); yield break; }
            if (IsStopped(true, interactive, consent)) { done?.Invoke(WhyStopped(interactive, consent)); yield break; }

            string result = null; bool finished = false;
            var fetch = b.Fetch(group, p => progress?.Invoke(p), err => { result = err; finished = true; });
            var host = Host();
            var co = host.StartCoroutine(fetch);
            float nextCheck = Time.realtimeSinceStartup + 0.5f;
            while (!finished)
            {
                if (Time.realtimeSinceStartup >= nextCheck)
                {
                    nextCheck = Time.realtimeSinceStartup + 0.5f;
                    if (IsStopped(true, interactive, consent))
                    {
                        b.Cancel();
                        if (co != null) host.StopCoroutine(co);
                        done?.Invoke(WhyStopped(interactive, consent));
                        yield break;
                    }
                }
                yield return null;
            }
            if (result != null) { done?.Invoke(result); yield break; }
            yield return LoadGroup(group, done);
        }

        /// <summary>Loads a group that is already downloaded (a restart, or a finished prefetch).</summary>
        public static IEnumerator LoadGroup(string group, Action<string> done)
        {
            var b = Backend;
            if (b == null) { done?.Invoke("no content host"); yield break; }
            string result = null; bool finished = false;
            var tracks = Map.TracksOf(group);
            yield return Host().StartCoroutine(b.LoadMusic(group, tracks, (track, clip) => { if (clip != null) clips[track] = clip; }, err => { result = err; finished = true; }));
            while (!finished) yield return null;
            if (result == null) loaded.Add(group);
            done?.Invoke(result);
        }

        private static bool IsStopped(bool needed, bool interactive, Consent consent)
        {
            var verdict = DownloadPolicy.Decide(needed, Network(), CellularOptIn(), consent != null && consent.Cellular, InGameplay(), interactive);
            return verdict != DownloadVerdict.Go;
        }

        private static string WhyStopped(bool interactive, Consent consent)
        {
            var verdict = DownloadPolicy.Decide(true, Network(), CellularOptIn(), consent != null && consent.Cellular, InGameplay(), interactive);
            switch (verdict)
            {
                case DownloadVerdict.BlockedDuringGameplay: return "gameplay";
                case DownloadVerdict.Offline: return "offline";
                default: return "wifi";
            }
        }

        // ---------------------------------------------------------------- quiet prefetch from the map

        /// <summary>
        /// Fetches, in the background and only on Wi-Fi (or with the saved mobile-data permission), the groups of the next few stages.
        /// Called when the world map opens, never from a stage.
        /// </summary>
        public static void PrefetchFrontier(int lookahead = 3)
        {
            if (!CanDownload || prefetching || InGameplay()) return;
            var frontier = StageProgress.Frontier(StageSession.Catalog, SaveSystem.Data);
            if (frontier == null) return;
            var groups = Map.GroupsToPrefetch(StageSession.Catalog, frontier, lookahead);
            if (groups.Length == 0) return;
            Host().StartCoroutine(Prefetch(groups));
        }

        public static IEnumerator Prefetch(string[] groups)
        {
            prefetching = true;
            try
            {
                foreach (var g in groups)
                {
                    if (loaded.Contains(g)) continue;
                    long size = -1;
                    yield return Host().StartCoroutine(Backend.SizeOf(g, s => size = s));
                    if (size < 0) continue;
                    if (size == 0) { yield return LoadGroup(g, null); continue; }
                    var verdict = DownloadPolicy.Decide(true, Network(), CellularOptIn(), false, InGameplay(), false);
                    if (verdict != DownloadVerdict.Go) { if (verdict == DownloadVerdict.BlockedDuringGameplay) break; continue; }
                    string err = null; bool fin = false;
                    yield return Host().StartCoroutine(Download(g, false, null, null, e => { err = e; fin = true; }));
                    while (!fin) yield return null;
                    if (err == "gameplay") break;
                }
            }
            finally { prefetching = false; }
        }

        /// <summary>A stage is about to start: any background download stops now.</summary>
        public static void StageStarting()
        {
            if (Backend != null && prefetching) Backend.Cancel();
        }

        // ---------------------------------------------------------------- the coroutine host

        private sealed class Runner : MonoBehaviour { }

        private static MonoBehaviour Host()
        {
            if (runner == null)
            {
                var go = new GameObject("ContentService") { hideFlags = HideFlags.HideAndDontSave };
                UnityEngine.Object.DontDestroyOnLoad(go);
                runner = go.AddComponent<Runner>();
            }
            return runner;
        }
    }
}
