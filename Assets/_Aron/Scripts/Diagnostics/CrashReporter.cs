// (c) Ahmed Alamrani. All rights reserved.
using System;
using System.Collections;
using System.Diagnostics;
using System.IO;
using System.Threading;
using Aron.Core;
using Aron.Logic;
using Aron.Logic.Diagnostics;
using Aron.Logic.Online;
using Aron.Logic.Performance;
using Aron.Save;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.SceneManagement;

namespace Aron.Diagnostics
{
    /// <summary>
    /// Error reports and freeze detection with no SDK and no background trackers.
    /// - Errors and exceptions are scrubbed (names, emails) and kept in a small file, at most 20 new ones per session.
    /// - A once-a-second timer notices when the main thread has not drawn a frame for 2 seconds (the freeze behind an ANR)
    ///   and records it with the last few things the player did. The timer stops while the app is in the background.
    /// - Reports are sent as one gzipped Sentry envelope, only with the diagnostics permission (see <see cref="Allowed"/>),
    ///   a project address in Resources/Config/crash_reporting.json (written by the build from a secret, never committed),
    ///   and a connection that <see cref="DataGate"/> allows (Wi-Fi, or once per session on mobile data).
    /// Hard native crashes cannot be caught from C#; Google Play's Android vitals and Xcode's Organizer cover those at no cost.
    /// </summary>
    public class CrashReporter : MonoBehaviour
    {
        public const string ConfigResource = "Config/crash_reporting";
        private static CrashReporter instance;
        private static readonly Breadcrumbs Crumbs = new Breadcrumbs(30);
        private static readonly object Gate = new object();

        /// <summary>Whether reports may leave the phone. Set by the privacy consent screen; off until then.</summary>
        public static Func<bool> Allowed = () => false;

        /// <summary>Errors recorded since install; the analytics "crash count" reads this.</summary>
        public static int TotalRecorded { get { lock (Gate) return instance != null ? instance.log.TotalRecorded : 0; } }

        private CrashLog log;
        private SentryDsn dsn;
        private FreezeDetector freeze;
        private Timer timer;
        private readonly Stopwatch clock = Stopwatch.StartNew();
        private bool sentThisSession;
        private string tier = "", os = "", release = "";

        // Unity's paths and save data may only be read on the main thread, so they are cached here.
        private static string queuePath = "";
        private string[] secrets = new string[0];
        private volatile bool dirty;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Boot()
        {
            if (Application.isEditor) return;
            var go = new GameObject("CrashReporter");
            DontDestroyOnLoad(go);
            go.AddComponent<CrashReporter>();
        }

        /// <summary>One line about what just happened. Call on events (scene, stage, purchase), never per frame.</summary>
        public static void Crumb(string what)
        {
            lock (Gate) Crumbs.Add(what, Now());
        }

        public static void SuspendWatchdog() { if (instance != null) lock (Gate) instance.freeze.Suspend(); }
        public static void ResumeWatchdog() { if (instance != null) lock (Gate) instance.freeze.Resume(Now()); }

        private static long Now() => instance != null ? instance.clock.ElapsedMilliseconds : 0L;

        private void Awake()
        {
            instance = this;
            queuePath = Path.Combine(Application.persistentDataPath, "crash_queue.json");
            RefreshSecrets();
            release = "aron@" + Application.version;
            os = SystemInfo.operatingSystem;
            tier = DeviceSettings.Tier.ToString();
            log = new CrashLog(LoadStore());
            log.Secrets = () => secrets;
            freeze = new FreezeDetector(0);
            var cfg = Resources.Load<TextAsset>(ConfigResource);
            if (cfg != null) SentryDsn.TryParse(JsonUtility.FromJson<Config>(cfg.text)?.dsn, out dsn);
            Application.logMessageReceivedThreaded += OnLog;
            SceneManager.activeSceneChanged += OnScene;
            StartTimer();
        }

        [Serializable] private class Config { public string dsn = ""; }

        private void Start() => StartCoroutine(SendWhenAllowed());

        private void OnDestroy()
        {
            Application.logMessageReceivedThreaded -= OnLog;
            SceneManager.activeSceneChanged -= OnScene;
            StopTimer();
            if (instance == this) instance = null;
        }

        private void OnScene(Scene a, Scene b) { Crumb("scene:" + b.name); RefreshSecrets(); }

        private void RefreshSecrets()
        {
            var name = SaveSystem.Data.displayName;
            secrets = string.IsNullOrEmpty(name) ? new string[0] : new[] { name };
        }

        // ------------------------------------------------------------------ freeze watchdog

        private void Update()
        {
            lock (Gate) freeze.Beat(clock.ElapsedMilliseconds);
            if (dirty) { dirty = false; Persist(); }
        }

        private void OnApplicationPause(bool paused)
        {
            if (paused) { if (dirty) { dirty = false; Persist(); } StopTimer(); lock (Gate) freeze.Suspend(); Crumb("background"); }
            else { lock (Gate) freeze.Resume(clock.ElapsedMilliseconds); StartTimer(); Crumb("foreground"); }
        }

        private void StartTimer()
        {
            if (timer == null) timer = new Timer(CheckFreeze, null, 1000, 1000);
        }

        private void StopTimer()
        {
            timer?.Dispose();
            timer = null;
        }

        private void CheckFreeze(object _)
        {
            long ms;
            bool frozen;
            lock (Gate) frozen = freeze.Check(clock.ElapsedMilliseconds, out ms);
            if (frozen) Record("warning", "Main thread frozen for " + ms + " ms", "");
        }

        // ------------------------------------------------------------------ errors

        private void OnLog(string message, string stack, LogType type)
        {
            if (type == LogType.Error || type == LogType.Exception || type == LogType.Assert)
                Record(type == LogType.Exception ? "fatal" : "error", message, stack);
        }

        private void Record(string level, string message, string stack)
        {
            string crumbs;
            bool added;
            lock (Gate)
            {
                crumbs = Crumbs.Dump(clock.ElapsedMilliseconds);
                added = log.Record(level, message, stack, crumbs, tier, os, release, DateTimeOffset.UtcNow.ToUnixTimeSeconds());
            }
            if (added) dirty = true;
        }

        private void Persist()
        {
            try { string json; lock (Gate) json = JsonUtility.ToJson(log.store); AtomicFile.WriteAllText(queuePath, json); }
            catch (Exception) { /* a full disk must not cause more errors */ }
        }

        private static CrashStore LoadStore()
        {
            try { if (File.Exists(queuePath)) return JsonUtility.FromJson<CrashStore>(File.ReadAllText(queuePath)) ?? new CrashStore(); }
            catch (Exception) { /* start fresh */ }
            return new CrashStore();
        }

        // ------------------------------------------------------------------ sending

        private IEnumerator SendWhenAllowed()
        {
            // Once per session, a little after start so loading is never slowed.
            yield return new WaitForSecondsRealtime(20f);
            while (!sentThisSession)
            {
                bool nothing; lock (Gate) nothing = log.Pending == 0;
                if (nothing || dsn == null) yield break;
                if (Allowed() && NetworkState.Allow(Traffic.CrashReport)) yield return Send();
                else yield return new WaitForSecondsRealtime(120f);
            }
        }

        private IEnumerator Send()
        {
            CrashRecord[] batch;
            lock (Gate) batch = log.store.records.ToArray();
            byte[] body = Compact.Gzip(SentryEnvelope.Build(dsn, batch, DateTimeOffset.UtcNow.ToUnixTimeSeconds()));
            using (var req = new UnityWebRequest(dsn.EnvelopeUrl, "POST"))
            {
                req.uploadHandler = new UploadHandlerRaw(body);
                req.downloadHandler = new DownloadHandlerBuffer();
                req.SetRequestHeader("Content-Type", "application/x-sentry-envelope");
                req.SetRequestHeader("Content-Encoding", "gzip");
                req.SetRequestHeader("X-Sentry-Auth", dsn.AuthHeader);
                req.timeout = 15;
                yield return req.SendWebRequest();
                sentThisSession = true;                       // one try per session, whatever the outcome
                NetworkState.NoteSent(Traffic.CrashReport);
                NetworkState.Meter.Sent(Traffic.CrashReport, body.Length);
                if (req.result != UnityWebRequest.Result.Success) yield break;
            }
            lock (Gate) log.Clear();
            Persist();
        }
    }
}
