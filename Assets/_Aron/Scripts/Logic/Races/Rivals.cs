// (c) Ahmed Alamrani. All rights reserved.
using System;
using System.Collections.Generic;

namespace Aron.Logic.Races
{
    /// <summary>One spoken line of a rival. The text is in the rival's own language or dialect; the Arabic subtitle and the UI-language subtitle say the same thing.</summary>
    [Serializable]
    public class RivalLine
    {
        public string id, kind, lang, dialect, text, subtitleAr, subtitleKey;
        /// <summary>none (English), native (a native speaker should check it) or dialect (an Arabic dialect speaker should).</summary>
        public string review = "native";
    }

    /// <summary>One recurring rival: a fictional, family-friendly racer in a respectful national outfit. Resources/Rivals/rivals.json, written by tools/gen_rivals.py.</summary>
    [Serializable]
    public class Rival
    {
        public string id, country, nameEn, nameAr, mount, style, model, outfit;
        public bool female, modest = true;
        /// <summary>Outfit of a woman rival (or any unsure one) that a person from that culture should look over.</summary>
        public bool outfitReview;
        public RivalLine[] lines = Array.Empty<RivalLine>();

        public RivalLine Line(string kind, int variant = 0)
        {
            RivalLine first = null; int n = 0;
            foreach (var l in lines) if (l.kind == kind) { if (first == null) first = l; if (n++ == variant) return l; }
            return first;
        }
    }

    [Serializable]
    public class RivalFile
    {
        public int version = 1;
        public string[] kinds = Array.Empty<string>();
        public Rival[] rivals = Array.Empty<Rival>();

        public static readonly string[] Kinds = { "start", "start2", "overtake", "overtaken", "boost", "crash", "win", "lose" };
        public static readonly string[] Styles = { "clean", "steady", "aggressive" };
        public static readonly string[] Mounts = { "horse", "camel", "ostrich", "giraffe", "falcon_glider" };
        public static readonly string[] Reviews = { "none", "native", "dialect" };

        public Rival ForCountry(string country)
        {
            foreach (var r in rivals) if (r.country == country) return r;
            return null;
        }

        public Rival Find(string id)
        {
            foreach (var r in rivals) if (r.id == id) return r;
            return null;
        }

        /// <summary>Checks the file. <paramref name="countries"/> is every country id that must have a rival.</summary>
        public List<string> Validate(IEnumerable<string> countries)
        {
            var e = new List<string>();
            var ids = new HashSet<string>(); var namesEn = new HashSet<string>(); var namesAr = new HashSet<string>(); var lineIds = new HashSet<string>();
            var seen = new HashSet<string>();
            foreach (var r in rivals)
            {
                string w = "rival " + r.id;
                if (string.IsNullOrEmpty(r.id) || !ids.Add(r.id)) e.Add(w + ": missing or duplicate id");
                if (string.IsNullOrEmpty(r.country) || !seen.Add(r.country)) e.Add(w + ": one rival per country");
                if (string.IsNullOrEmpty(r.nameEn) || !namesEn.Add(r.nameEn.ToLowerInvariant())) e.Add(w + ": name missing or used twice");
                if (string.IsNullOrEmpty(r.nameAr) || !namesAr.Add(r.nameAr)) e.Add(w + ": Arabic name missing or used twice");
                if (Array.IndexOf(Mounts, r.mount) < 0) e.Add(w + ": unknown mount " + r.mount);
                if (Array.IndexOf(Styles, r.style) < 0) e.Add(w + ": unknown style " + r.style);
                if (string.IsNullOrEmpty(r.outfit) || string.IsNullOrEmpty(r.model)) e.Add(w + ": outfit and model");
                if (!r.modest) e.Add(w + ": every rival is dressed modestly");
                if (r.female && !r.outfitReview) e.Add(w + ": a woman rival's outfit is flagged for review");
                if (r.lines == null || r.lines.Length < 6 || r.lines.Length > 10) e.Add(w + ": 6..10 lines");
                var kinds = new HashSet<string>();
                foreach (var l in r.lines ?? Array.Empty<RivalLine>())
                {
                    string lw = w + "/" + l.id;
                    if (string.IsNullOrEmpty(l.id) || !lineIds.Add(l.id)) e.Add(lw + ": missing or duplicate line id");
                    if (Array.IndexOf(Kinds, l.kind) < 0) e.Add(lw + ": unknown kind");
                    kinds.Add(l.kind);
                    if (string.IsNullOrWhiteSpace(l.text) || l.text.Length > 60) e.Add(lw + ": text 1..60 characters");
                    if (string.IsNullOrWhiteSpace(l.lang) || string.IsNullOrWhiteSpace(l.dialect)) e.Add(lw + ": language and dialect tag");
                    if (string.IsNullOrWhiteSpace(l.subtitleAr)) e.Add(lw + ": Arabic subtitle");
                    if (string.IsNullOrEmpty(l.subtitleKey) || !l.subtitleKey.StartsWith("rival_bark_")) e.Add(lw + ": subtitle key");
                    if (Array.IndexOf(Reviews, l.review) < 0) e.Add(lw + ": review flag");
                    if (l.lang != "en" && l.review == "none") e.Add(lw + ": only English lines can skip native review");
                }
                foreach (var k in new[] { "start", "overtake", "overtaken", "boost", "crash", "win", "lose" })
                    if (!kinds.Contains(k)) e.Add(w + ": missing a " + k + " line");
            }
            if (countries != null)
                foreach (var c in countries) if (!seen.Contains(c)) e.Add("no rival for " + c);
            return e;
        }
    }

    /// <summary>The subtitles for one line: Arabic first (the primary language), then the player's UI language when it differs.</summary>
    public static class RivalSubtitles
    {
        /// <param name="uiText">Resolves a UI string key in the player's language.</param>
        public static string[] For(RivalLine line, string uiLang, bool subtitlesOn, Func<string, string> uiText)
        {
            if (line == null || !subtitlesOn) return Array.Empty<string>();
            if (uiLang == "ar" || uiText == null) return new[] { line.subtitleAr };
            string ui = uiText(line.subtitleKey);
            if (string.IsNullOrEmpty(ui) || ui == line.subtitleAr) return new[] { line.subtitleAr };
            return new[] { line.subtitleAr, ui };
        }
    }

    /// <summary>
    /// Keeps rival chatter occasional: one voice at a time, a pause between lines, a longer pause for the same rival, a cap per race,
    /// and no line twice. 'Rival chatter' off silences everything (subtitles are a separate switch).
    /// </summary>
    public class BarkLimiter
    {
        public const float GlobalGap = 4f, RivalGap = 14f;
        public const int MaxPerRace = 10;
        private readonly bool chatter;
        private readonly Dictionary<string, float> lastByRival = new Dictionary<string, float>();
        private readonly HashSet<string> spoken = new HashSet<string>();
        private float lastAny = float.NegativeInfinity;
        public int Count { get; private set; }

        public BarkLimiter(bool chatterOn) { chatter = chatterOn; }

        /// <summary>Returns the line to speak, or null when this one would be too much.</summary>
        public RivalLine Try(Rival rival, string kind, float now, Random rnd)
        {
            if (!chatter || rival == null) return null;
            if (Count >= MaxPerRace) return null;
            bool important = kind == "win" || kind == "lose";
            if (!important && now - lastAny < GlobalGap) return null;
            if (important && now - lastAny < 1f) return null;
            if (!important && lastByRival.TryGetValue(rival.id, out var t) && now - t < RivalGap) return null;
            var options = new List<RivalLine>();
            foreach (var l in rival.lines) if (l.kind == kind || (kind == "start" && l.kind == "start2")) if (!spoken.Contains(l.id)) options.Add(l);
            if (options.Count == 0) return null;
            var pick = options[rnd.Next(options.Count)];
            spoken.Add(pick.id); lastByRival[rival.id] = now; lastAny = now; Count++;
            return pick;
        }
    }
}
