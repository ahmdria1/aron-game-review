// (c) Ahmed Alamrani. All rights reserved.
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using Aron.Core;
using Aron.Diagnostics;
using Aron.Logic;
using Aron.Logic.Config;
using Aron.Logic.Online;
using Aron.Logic.Performance;
using Aron.Logic.Telemetry;
using Aron.RemoteConfig;
using Aron.Save;
using UnityEngine;

namespace Aron.Telemetry
{
    /// <summary>Delivers a batch of already-checked events somewhere (Unity Analytics in the shipped game).</summary>
    public interface IEventSink
    {
        /// <summary>Sends the batch. Calls <c>done(true)</c> only when it was handed over.</summary>
        IEnumerator Send(List<AnalyticsEvent> batch, Action<bool> done);
        /// <summary>Asks the service to forget what it holds about this player (the consent was withdrawn first).</summary>
        void RequestDeletion();
    }

    /// <summary>
    /// Usage statistics, with the grown-up's choice in front of everything (see <see cref="ConsentGate"/>).
    /// No consent: nothing is recorded, queued or sent, and the saved queue is thrown away at start.
    /// Consent: events are checked against <see cref="EventSchema"/>, queued on the phone, and sent as one batch when 20 are waiting or the
    /// oldest is 15 minutes old, on Wi-Fi or (Data saver off) once per session on mobile data, never during play or in the background.
    /// </summary>
    public class TelemetryService : MonoBehaviour
    {
        public const string QueueFile = "analytics_queue.json";
        public static IEventSink Sink;

        private static TelemetryService instance;
        private static readonly EventBuffer Buffer = new EventBuffer();
        private bool dirty;

        private static PrivacyChoices Choices
        {
            get
            {
                var d = SaveSystem.Data;
                if (d.privacy == null) d.privacy = new PrivacyChoices();
                return d.privacy;
            }
        }

        private static string QueuePath => Path.Combine(Application.persistentDataPath, QueueFile);

        /// <summary>The single yes/no used everywhere: the grown-up said yes for this notice version, and the server switch is on.</summary>
        public static bool AnalyticsAllowed => ConsentGate.Allowed(Choices, Channel.Analytics) && GameSettingsService.IsOn(Feature.Analytics);
        public static bool DiagnosticsAllowed => ConsentGate.Allowed(Choices, Channel.Diagnostics);

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Boot()
        {
            if (instance != null) return;
            var go = new GameObject("Telemetry");
            DontDestroyOnLoad(go);
            go.AddComponent<TelemetryService>();
        }

        private void Awake()
        {
            instance = this;
            Buffer.MayRecord = () => AnalyticsAllowed;
            CrashReporter.Allowed = () => DiagnosticsAllowed;
            LoadQueue();
            GameEvents.BossDefeated += OnBoss;
        }

        private void OnDestroy()
        {
            GameEvents.BossDefeated -= OnBoss;
            if (instance == this) instance = null;
        }

        private static void OnBoss()
        {
            if (Stages.StageSession.Active) BossDefeated(Stages.StageSession.Stage.id);
        }

        private void Start()
        {
            StartCoroutine(Run());
        }

        // ------------------------------------------------------------------ recording

        /// <summary>Records one event if (and only if) the grown-up allowed it.</summary>
        public static void Record(AnalyticsEvent e)
        {
            if (instance == null || e == null) return;
            if (Buffer.Record(e, DateTimeOffset.UtcNow.ToUnixTimeSeconds())) instance.dirty = true;
        }

        public static void StageStart(string stageId) => Record(Events.StageStart(stageId));
        public static void StageComplete(string stageId, float seconds) => Record(Events.StageComplete(stageId, seconds));
        public static void StageFail(string stageId, float seconds) => Record(Events.StageFail(stageId, seconds));
        public static void BossDefeated(string stageId) => Record(Events.BossDefeated(stageId));
        public static void PurchaseCompleted(string productId) => Record(Events.PurchaseCompleted(productId));
        public static void RegionsOpened(IEnumerable<string> regions) { foreach (var r in regions) Record(Events.RegionUnlocked(r)); }

        // ------------------------------------------------------------------ the choice

        /// <summary>Called by the privacy screen after the grown-up check. Switching off clears everything that was waiting.</summary>
        public static bool Choose(Channel channel, bool on, bool grownUpChecked)
        {
            var c = Choices;
            if (!ConsentGate.Set(c, channel, on, grownUpChecked, DateTimeOffset.UtcNow.ToUnixTimeSeconds())) return false;
            if (channel == Channel.Analytics && !on) ClearQueue();
            SaveSystem.Save();
            return true;
        }

        /// <summary>"Delete my statistics data": consent off, local queue deleted, and the Unity Analytics deletion request sent (it retries on its own).</summary>
        public static void EraseAll()
        {
            ConsentGate.WithdrawAll(Choices, DateTimeOffset.UtcNow.ToUnixTimeSeconds());
            ClearQueue();
            SaveSystem.Save();
            Sink?.RequestDeletion();
        }

        private static void ClearQueue()
        {
            Buffer.Clear();
            try { if (File.Exists(QueuePath)) File.Delete(QueuePath); } catch (Exception) { /* nothing sensitive is left in it anyway: it was just emptied */ }
            if (instance != null) instance.dirty = false;
        }

        // ------------------------------------------------------------------ loop

        private IEnumerator Run()
        {
            // Let the first scene load, then note the first open and any crashes since last time.
            yield return new WaitForSecondsRealtime(10f);
            Opening();
            while (true)
            {
                yield return new WaitForSecondsRealtime(30f);
                if (dirty) PersistQueue();
                if (!AnalyticsAllowed || Sink == null) continue;
                var mode = DeviceSettings.Mode;
                if (mode == ScreenMode.Gameplay || mode == ScreenMode.Loading || mode == ScreenMode.Background) continue;
                long now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
                if (!Buffer.Due(now, NetworkState.Allow(Traffic.Telemetry))) continue;
                var batch = Buffer.Peek();
                bool ok = false;
                yield return Sink.Send(batch, r => ok = r);
                NetworkState.NoteSent(Traffic.Telemetry);
                if (ok)
                {
                    Buffer.Remove(batch.Count);
                    NetworkState.Meter.Sent(Traffic.Telemetry, 60 * batch.Count + 400);   // estimate: about 60 bytes an event plus headers
                    PersistQueue();
                }
            }
        }

        private void Opening()
        {
            if (!AnalyticsAllowed) return;
            var c = Choices;
            if (!c.firstOpenSent) { Record(Events.FirstOpen()); c.firstOpenSent = true; SaveSystem.Save(); }
            int pending = CrashCountPolicy.Pending(CrashReporter.TotalRecorded, c.crashesReported);
            if (pending > 0) { Record(Events.CrashCount(pending)); c.crashesReported += pending; SaveSystem.Save(); }
        }

        private void OnApplicationPause(bool paused) { if (paused && dirty) PersistQueue(); }
        private void OnApplicationQuit() { if (dirty) PersistQueue(); }

        private void PersistQueue()
        {
            dirty = false;
            try
            {
                if (Buffer.Count == 0) { if (File.Exists(QueuePath)) File.Delete(QueuePath); return; }
                AtomicFile.WriteAllText(QueuePath, JsonUtility.ToJson(Buffer.Snapshot()));
            }
            catch (Exception) { /* the queue is only a convenience */ }
        }

        private void LoadQueue()
        {
            try
            {
                if (!File.Exists(QueuePath)) return;
                var store = JsonUtility.FromJson<EventStore>(File.ReadAllText(QueuePath));
                Buffer.Load(store);                    // thrown away when there is no consent
                if (!AnalyticsAllowed) File.Delete(QueuePath);
            }
            catch (Exception) { try { File.Delete(QueuePath); } catch (Exception) { } }
        }
    }
}
