// (c) Ahmed Alamrani. All rights reserved.
using System;
using System.Collections.Generic;
using Aron.Logic.Culture;
using Aron.Logic.Localization;
using Aron.Logic.Safety;

namespace Aron.Logic.Access
{
    public enum CaptionKind { Speech, Sound }

    /// <summary>One caption: what is said (or what the sound is), in every language.</summary>
    [Serializable]
    public class CaptionEntry
    {
        /// <summary>The voice line id (<c>vo_aron_help</c>) or sound cue id (<c>enemy_stomp</c>) it belongs to.</summary>
        public string id;
        /// <summary>"speech" or "sound".</summary>
        public string kind = "speech";
        /// <summary>aron, noura, boss, rival or empty for a sound.</summary>
        public string speaker = "";
        /// <summary>Sounds only: true when missing it can cost the player something (a warning, a hit, a timer). Captioned even when sound captions are set to "important only".</summary>
        public bool important;
        public LocalText text = new LocalText();

        public CaptionKind GetKind() => kind == "sound" ? CaptionKind.Sound : CaptionKind.Speech;
    }

    /// <summary>Resources/Config/captions.json.</summary>
    [Serializable]
    public class CaptionFile
    {
        public CaptionEntry[] entries = Array.Empty<CaptionEntry>();

        [NonSerialized] private Dictionary<string, CaptionEntry> byId;

        public CaptionEntry Find(string id)
        {
            if (string.IsNullOrEmpty(id)) return null;
            if (byId == null)
            {
                byId = new Dictionary<string, CaptionEntry>();
                foreach (var e in entries) if (e != null && !string.IsNullOrEmpty(e.id) && !byId.ContainsKey(e.id)) byId[e.id] = e;
            }
            return byId.TryGetValue(id, out var v) ? v : null;
        }

        /// <summary>The caption in the UI language, then English, then Arabic. Null when the id has no caption.</summary>
        public string Text(string id, string language)
        {
            var e = Find(id);
            if (e == null) return null;
            foreach (var lang in Chain(language))
            {
                string t = e.text.Get(lang);
                if (!string.IsNullOrEmpty(t)) return t;
            }
            return null;
        }

        private static IEnumerable<string> Chain(string language)
        {
            if (UiLanguages.IsKnown(language)) yield return language;
            if (language != "en") yield return "en";
            yield return "ar";
        }

        public List<string> Validate()
        {
            var e = new List<string>();
            var seen = new HashSet<string>();
            foreach (var c in entries)
            {
                if (c == null || string.IsNullOrEmpty(c.id)) { e.Add("caption without an id"); continue; }
                if (!seen.Add(c.id)) e.Add("duplicate caption " + c.id);
                if (c.kind != "speech" && c.kind != "sound") e.Add(c.id + ": kind is speech or sound");
                foreach (var lang in c.text.Missing()) e.Add(c.id + ": missing in " + lang);
                foreach (var t in c.text.All()) if (t != null && t.Length > CaptionRules.MaxChars) e.Add(c.id + ": longer than " + CaptionRules.MaxChars + " characters");
            }
            return e;
        }
    }

    /// <summary>How captions are shown: sizes, reading time and what is allowed on screen together.</summary>
    public static class CaptionRules
    {
        /// <summary>A caption is one short line, so it fits two lines at the largest size.</summary>
        public const int MaxChars = 60;
        /// <summary>Text size steps for subtitles, as multipliers of the base size.</summary>
        public static readonly float[] Sizes = { 0.8f, 1f, 1.25f, 1.5f, 2f };
        /// <summary>Base subtitle size on the 1920x1080 reference canvas.</summary>
        public const float BaseFontSize = 40f;
        public const int MaxLines = 2;
        public const float MinSeconds = 1.6f, MaxSeconds = 6f;
        /// <summary>Characters a young reader gets per second.</summary>
        public const float CharsPerSecond = 12f;

        public static float NextSize(float current)
        {
            int best = 0;
            for (int i = 0; i < Sizes.Length; i++) if (Math.Abs(Sizes[i] - current) < Math.Abs(Sizes[best] - current)) best = i;
            return Sizes[(best + 1) % Sizes.Length];
        }

        public static float FontSize(float size) => BaseFontSize * Math.Max(Sizes[0], Math.Min(Sizes[Sizes.Length - 1], size));

        /// <summary>How long a caption stays: reading time at <see cref="CharsPerSecond"/>, never shorter than 1.6 s or longer than 6 s.</summary>
        public static float Duration(string text)
        {
            int n = string.IsNullOrEmpty(text) ? 0 : text.Length;
            return Math.Max(MinSeconds, Math.Min(MaxSeconds, 0.8f + n / CharsPerSecond));
        }

        /// <summary>A sound is written in brackets so it is not mistaken for speech: [ ding ].</summary>
        public static string Format(CaptionKind kind, string text) => kind == CaptionKind.Sound ? "[ " + text + " ]" : text;
    }

    /// <summary>The caption settings that decide whether a cue is shown.</summary>
    public static class CaptionPolicy
    {
        public static bool Show(AccessSettings a, CaptionEntry e)
        {
            if (a == null || e == null) return false;
            if (e.GetKind() == CaptionKind.Speech) return a.captions;
            // sound captions are for the sounds that matter, or for all of them when the player asks
            return a.captions && a.soundCaptions != SoundCaptionMode.Off && (a.soundCaptions == SoundCaptionMode.All || e.important);
        }
    }

    public enum SoundCaptionMode { Off, Important, All }

    public struct CaptionLine
    {
        public string Id;
        public string Text;
        public CaptionKind Kind;
        public string Speaker;
        public double Until;
    }

    /// <summary>
    /// What is on screen right now: at most two lines, speech before sounds, the same cue not repeated while it is still up, a sound never
    /// pushes speech out. Pure: the caller passes the time.
    /// </summary>
    public class CaptionQueue
    {
        /// <summary>The same cue is ignored for this long after it was shown (a footstep every 0.3 s is one caption, not ten).</summary>
        public const float RepeatSeconds = 1.5f;

        private readonly List<CaptionLine> lines = new List<CaptionLine>();
        private readonly Dictionary<string, double> lastShown = new Dictionary<string, double>();

        public IReadOnlyList<CaptionLine> Lines => lines;

        public bool Request(string id, string text, CaptionKind kind, string speaker, double now)
        {
            Tick(now);
            if (string.IsNullOrEmpty(text)) return false;
            if (lastShown.TryGetValue(id ?? "", out var t) && now - t < RepeatSeconds) return false;
            var line = new CaptionLine { Id = id, Text = CaptionRules.Format(kind, text), Kind = kind, Speaker = speaker, Until = now + CaptionRules.Duration(text) };
            if (lines.Count >= CaptionRules.MaxLines)
            {
                // make room: the oldest sound goes first; speech is only replaced by newer speech
                int drop = lines.FindIndex(l => l.Kind == CaptionKind.Sound);
                if (drop < 0) { if (kind == CaptionKind.Sound) return false; drop = 0; }
                lines.RemoveAt(drop);
            }
            lastShown[id ?? ""] = now;
            // speech is listed above sounds; within a kind the order of arrival is kept
            int at = lines.Count;
            if (kind == CaptionKind.Speech)
            {
                at = lines.FindIndex(l => l.Kind == CaptionKind.Sound);
                if (at < 0) at = lines.Count;
            }
            lines.Insert(at, line);
            return true;
        }

        public void Tick(double now) => lines.RemoveAll(l => now >= l.Until);

        public void Clear() { lines.Clear(); lastShown.Clear(); }
    }
}
