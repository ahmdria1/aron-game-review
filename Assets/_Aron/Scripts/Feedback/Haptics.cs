// (c) Ahmed Alamrani. All rights reserved.
using System.Collections.Generic;
using Aron.Core;
using Aron.Logic.Feedback;
using Aron.Save;
using UnityEngine;

namespace Aron.Feedback
{
    /// <summary>
    /// Game-facing haptics: <c>Haptics.Play("coin")</c>. The table is Resources/Config/haptics.json (every event in HapticFile.Events),
    /// the on/off, Low/Medium/High and battery-saver choices come from the save, and the gate (Aron.Logic.Feedback.HapticGate) applies
    /// them along with a per-event rate limit. Loop patterns (race_boost, drift, gallop) repeat while <see cref="Hold"/> is called.
    /// Event hooks (box breaks, coins, boss hits...) are wired at the bottom, so gameplay code stays untouched.
    /// </summary>
    public static class Haptics
    {
        public const string ConfigPath = "Config/haptics";

        private static HapticFile file;
        private static HapticGate gate;
        private static HapticService service;
        private static float lastBatteryCheck = -100f;
        private static readonly Dictionary<string, float> holdUntil = new Dictionary<string, float>();
        private static readonly Dictionary<string, float> holdNext = new Dictionary<string, float>();

        public static HapticFile File
        {
            get
            {
                if (file == null)
                {
                    var t = Resources.Load<TextAsset>(ConfigPath);
                    file = t != null ? JsonUtility.FromJson<HapticFile>(t.text) : new HapticFile();
                    if (file.Validate().Count > 0) global::Aron.Logic.Diagnostics.Log.Warn("haptics.json: " + string.Join("; ", file.Validate()));
                }
                return file;
            }
        }

        private static HapticGate Gate
        {
            get
            {
                if (gate == null) { gate = new HapticGate(File); service = new HapticService(); }
                var s = SaveSystem.Data.haptics ?? (SaveSystem.Data.haptics = new HapticSettings());
                s.Apply(gate);
                gate.AppInForeground = Application.isFocused;
                if (Time.unscaledTime - lastBatteryCheck > 10f) { lastBatteryCheck = Time.unscaledTime; gate.BatterySaver = service.BatterySaver; }
                return gate;
            }
        }

        /// <summary>One vibration for a game event, if the settings, the gate and the rate limit allow it.</summary>
        public static void Play(string eventId)
        {
            if (string.IsNullOrEmpty(eventId)) return;
            var g = Gate;
            var r = g.Fire(eventId, Time.unscaledTimeAsDouble);
            if (r.HasValue) service.Play(r.Value);
        }

        /// <summary>Call every frame while a continuous event lasts (boost, drift, gallop); the pattern repeats while the calls keep coming.</summary>
        public static void Hold(string eventId)
        {
            var p = File.For(eventId);
            if (p == null || !p.loop) return;
            holdUntil[eventId] = Time.unscaledTime + 0.25f;
            if (!holdNext.TryGetValue(eventId, out var next) || Time.unscaledTime >= next)
            {
                Play(eventId);
                float len = 0f; foreach (var t in p.timings) len += t;
                holdNext[eventId] = Time.unscaledTime + Mathf.Max(len / 1000f, 1f / p.maxPerSecond);
            }
        }

        public static void Stop() { if (service != null) service.Stop(); holdUntil.Clear(); holdNext.Clear(); }

        /// <summary>For emotion cues (EmotionCue.haptic) and passport moments (Passport.StampHaptic).</summary>
        public static void OnCue(string hapticId) => Play(hapticId);

        // ---- hooks ----

        private static float lastBossHealth = 1f;
        private static int lastBisht;

        private static readonly Dictionary<string, string> SfxToHaptic = new Dictionary<string, string>
        {
            { "coin", "coin" }, { "hit", "hit" }, { "enemy", "stomp" }, { "bounce", "land" },
            { "checkpoint", "checkpoint" }, { "explosion", "explosion" }, { "life", "collect_item" },
        };

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Hook()
        {
            GameEvents.SfxRequested -= OnSfx; GameEvents.SfxRequested += OnSfx;
            GameEvents.CrateBroken -= OnCrate; GameEvents.CrateBroken += OnCrate;
            GameEvents.PlayerDied -= OnDied; GameEvents.PlayerDied += OnDied;
            GameEvents.BossEncounterStarted -= OnBossStart; GameEvents.BossEncounterStarted += OnBossStart;
            GameEvents.BossHealthChanged -= OnBossHealth; GameEvents.BossHealthChanged += OnBossHealth;
            GameEvents.BossDefeated -= OnBossDefeated; GameEvents.BossDefeated += OnBossDefeated;
            GameEvents.BishtChanged -= OnBisht; GameEvents.BishtChanged += OnBisht;
            GameEvents.PuzzleSolved -= OnPuzzle; GameEvents.PuzzleSolved += OnPuzzle;
            GameEvents.LevelCompleted -= OnLevelDone; GameEvents.LevelCompleted += OnLevelDone;
        }

        private static void OnSfx(string id) { if (SfxToHaptic.TryGetValue(id, out var h)) Play(h); }
        private static void OnCrate(Vector3 at) => Play("box_break");
        private static void OnDied() => Play("death");
        private static void OnBossStart() => lastBossHealth = 1f;
        private static void OnBossHealth(float h) { if (h < lastBossHealth) Play("boss_hit"); lastBossHealth = h; }
        private static void OnBossDefeated() => Play("boss_defeat");
        private static void OnBisht(int layers, bool invincible) { if (layers < lastBisht) Play("bisht_lost"); lastBisht = layers; }
        private static void OnPuzzle(string kind) => Play("collect_item");
        private static void OnLevelDone() => Play("star");
    }
}
