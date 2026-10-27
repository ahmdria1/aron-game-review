// (c) Ahmed Alamrani. All rights reserved.
using System;
using System.Collections.Generic;

namespace Aron.Logic.Feedback
{
    /// <summary>One vibration: pulses of (milliseconds on, amplitude 0..1) with gaps, or an iOS feedback style.</summary>
    [Serializable]
    public class HapticPattern
    {
        public string id;
        /// <summary>Android waveform: on/off milliseconds alternating, starting with a pause.</summary>
        public int[] timings = Array.Empty<int>();
        /// <summary>Amplitude 0..255 for each timing (0 for the pauses).</summary>
        public int[] amplitudes = Array.Empty<int>();
        /// <summary>iOS: light, medium, heavy (impact) or success, warning, error (notification).</summary>
        public string ios = "light";
        /// <summary>Most times per second this pattern may fire (coins are capped at about 15).</summary>
        public float maxPerSecond = 8f;
        /// <summary>Continuous patterns (boost, drift, gallop) repeat while their event lasts.</summary>
        public bool loop;
    }

    /// <summary>Resources/Config/haptics.json: game event to pattern.</summary>
    [Serializable]
    public class HapticFile
    {
        public static readonly string[] Ios = { "light", "medium", "heavy", "success", "warning", "error" };
        public static readonly string[] Events =
        {
            "box_break", "coin", "land", "stomp", "hit", "bisht_lost", "death", "explosion", "boss_hit", "boss_defeat",
            "race_boost", "drift", "collision", "gift_break", "gallop",
            "sleeve_tick", "passport_stamp", "passport_visa", "checkpoint", "collect_item", "companion_dash", "hit_stop", "race_win", "star", "ui_confirm",
        };

        public HapticPattern[] patterns = Array.Empty<HapticPattern>();

        public HapticPattern For(string id)
        {
            foreach (var p in patterns) if (p.id == id) return p;
            return null;
        }

        public List<string> Validate()
        {
            var e = new List<string>();
            foreach (var ev in Events) if (For(ev) == null) e.Add("no haptic for " + ev);
            foreach (var p in patterns)
            {
                if (p.timings.Length == 0 || p.timings.Length != p.amplitudes.Length) e.Add(p.id + ": timings and amplitudes must pair up");
                int total = 0;
                foreach (var t in p.timings) { if (t < 0) e.Add(p.id + ": negative timing"); total += t; }
                if (total > 1500) e.Add(p.id + ": longer than 1.5 s");
                foreach (var a in p.amplitudes) if (a < 0 || a > 255) e.Add(p.id + ": amplitude 0..255");
                if (Array.IndexOf(Ios, p.ios) < 0) e.Add(p.id + ": unknown iOS style " + p.ios);
                if (p.maxPerSecond <= 0f || p.maxPerSecond > 20f) e.Add(p.id + ": rate 0..20 per second");
            }
            var coin = For("coin");
            if (coin != null && coin.maxPerSecond > 15f) e.Add("coin ticks are capped at 15 a second");
            return e;
        }
    }

    public enum HapticIntensity { Low, Medium, High }

    /// <summary>The player's haptic choices, saved with the game: on or off, Low / Medium / High, and off while the phone is in battery saver.</summary>
    [Serializable]
    public class HapticSettings
    {
        public bool enabled = true;
        /// <summary>0 Low, 1 Medium (default), 2 High.</summary>
        public int level = 1;
        public bool offInBatterySaver = true;

        public HapticIntensity Intensity => level <= 0 ? HapticIntensity.Low : level >= 2 ? HapticIntensity.High : HapticIntensity.Medium;

        public void CycleLevel() { level = (Math.Max(0, Math.Min(2, level)) + 1) % 3; }

        /// <summary>Copies the choices onto a gate.</summary>
        public void Apply(HapticGate gate)
        {
            gate.Enabled = enabled; gate.Intensity = Intensity; gate.OffInBatterySaver = offInBatterySaver;
        }
    }

    /// <summary>What the platform layer (Android VibrationEffect / iOS feedback generators) is asked to play.</summary>
    public struct HapticRequest
    {
        public HapticPattern pattern;
        /// <summary>Amplitudes already scaled for the intensity setting.</summary>
        public int[] amplitudes;
    }

    /// <summary>
    /// Decides whether an event vibrates: off in settings, app in the background, OS haptics off or battery saver
    /// (when the player chose that) all stop it; each pattern is rate-limited; amplitudes scale with intensity.
    /// </summary>
    public class HapticGate
    {
        private readonly HapticFile file;
        private readonly Dictionary<string, double> last = new Dictionary<string, double>();

        public bool Enabled = true;
        public HapticIntensity Intensity = HapticIntensity.Medium;
        public bool OffInBatterySaver = true;
        public bool AppInForeground = true;
        public bool OsHapticsOn = true;
        public bool BatterySaver;

        public HapticGate(HapticFile file) { this.file = file; }

        public static float Scale(HapticIntensity i) => i == HapticIntensity.Low ? 0.45f : i == HapticIntensity.High ? 1f : 0.75f;

        public bool Allowed => Enabled && AppInForeground && OsHapticsOn && !(OffInBatterySaver && BatterySaver);

        /// <summary>Returns the request to play, or null. <paramref name="now"/> is in seconds.</summary>
        public HapticRequest? Fire(string eventId, double now)
        {
            if (!Allowed || file == null) return null;
            var p = file.For(eventId);
            if (p == null) return null;
            double gap = 1.0 / p.maxPerSecond;
            if (last.TryGetValue(eventId, out var t) && now - t < gap - 1e-6) return null;
            last[eventId] = now;
            float s = Scale(Intensity);
            var amps = new int[p.amplitudes.Length];
            for (int i = 0; i < amps.Length; i++) amps[i] = p.amplitudes[i] == 0 ? 0 : Math.Max(1, Math.Min(255, (int)Math.Round(p.amplitudes[i] * s)));
            return new HapticRequest { pattern = p, amplitudes = amps };
        }
    }

    /// <summary>The platform side. Android uses VibrationEffect waveforms (Handheld.Vibrate when that is missing), iOS a tiny native plugin.</summary>
    public interface IHapticService
    {
        void Play(HapticRequest request);
        void Stop();
    }
}
