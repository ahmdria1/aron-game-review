// (c) Ahmed Alamrani. All rights reserved.
using System;
using System.Collections.Generic;
using Aron.Logic.Culture;
using Aron.Logic.Localization;

namespace Aron.Logic.Audio
{
    /// <summary>
    /// A line Aron or Noura says, in every UI language. The heroes speak the player's language but keep a few
    /// Saudi words as they are (yalla, mashallah, abshir, hala wallah, wallah, ya satir). Arabic stays Saudi dialect.
    /// </summary>
    [Serializable]
    public class HeroLine
    {
        public string id;
        /// <summary>aron, noura, or any (whichever hero is being played says it).</summary>
        public string character;
        public string kind;
        public LocalText text = new LocalText();
    }

    /// <summary>Resources/Dialogue/HeroLines.json.</summary>
    [Serializable]
    public class HeroDialogueFile
    {
        public HeroLine[] lines = Array.Empty<HeroLine>();

        public static readonly string[] Characters = { "aron", "noura", "any" };

        /// <summary>The Saudi words kept in every language, as written in Latin, Urdu, Hindi and Japanese lines.</summary>
        public static readonly string[] SignatureWords =
        {
            "yalla", "mashallah", "abshir", "hala wallah", "wallah", "ya satir",
            "یلا", "ماشاءاللہ", "ابشر", "ہلا واللہ", "واللہ", "یا ساتر",
            "यल्ला", "माशाअल्लाह", "अब्शिर", "हला वल्लाह", "वल्लाह", "या सातिर",
            "ヤッラ", "マーシャッラー", "アブシル", "ハラ・ワッラー", "ワッラー", "ヤー・サーティル",
        };

        public HeroLine Find(string id)
        {
            foreach (var l in lines) if (l.id == id) return l;
            return null;
        }

        /// <summary>The order languages are tried in: the UI language, then English, then Arabic.</summary>
        public static IEnumerable<string> Chain(string language)
        {
            if (UiLanguages.IsKnown(language)) yield return language;
            if (language != "en") yield return "en";
            if (language != "ar") yield return "ar";
        }

        /// <summary>The subtitle for a line in the UI language, falling back to English then Arabic.</summary>
        public string Text(string id, string language)
        {
            var l = Find(id);
            if (l == null) return null;
            foreach (var lang in Chain(language))
            {
                string t = Exact(l.text, lang);
                if (!string.IsNullOrEmpty(t)) return t;
            }
            return null;
        }

        /// <summary>Resources path of the recording: Audio/VO/&lt;lang&gt;/&lt;character&gt;/&lt;lineId&gt;.</summary>
        public static string ClipPath(string language, string character, string id) => "Audio/VO/" + language + "/" + character + "/" + id;

        /// <summary>Every clip path to try, best first. "any" lines are recorded by both heroes; the speaker picks one.</summary>
        public IEnumerable<string> ClipPaths(string id, string language, string speaker)
        {
            var l = Find(id);
            if (l == null) yield break;
            string who = l.character == "any" ? (string.IsNullOrEmpty(speaker) ? "aron" : speaker) : l.character;
            foreach (var lang in Chain(language)) yield return ClipPath(lang, who, id);
        }

        public static string Exact(LocalText t, string lang)
        {
            switch (lang)
            {
                case "ar": return t.ar; case "en": return t.en; case "tr": return t.tr; case "de": return t.de;
                case "ur": return t.ur; case "hi": return t.hi; case "es": return t.es; case "pt": return t.pt;
                case "id": return t.id; case "ms": return t.ms; case "ja": return t.ja; case "fr": return t.fr;
                default: return null;
            }
        }

        public static bool HasSignature(string text)
        {
            if (string.IsNullOrEmpty(text)) return false;
            string t = text.ToLowerInvariant();
            foreach (var w in SignatureWords) if (t.Contains(w)) return true;
            return false;
        }

        public List<string> Validate()
        {
            var e = new List<string>();
            var ids = new HashSet<string>();
            foreach (var l in lines)
            {
                if (string.IsNullOrEmpty(l.id) || !ids.Add(l.id)) e.Add("hero line missing or duplicate id " + l.id);
                if (Array.IndexOf(Characters, l.character) < 0) e.Add(l.id + ": character is aron, noura or any");
                foreach (var lang in l.text.Missing()) e.Add(l.id + ": missing in " + lang);
            }
            // The Saudi flavour has to survive translation: each language keeps the signature words somewhere.
            foreach (var lang in UiLanguages.Codes)
            {
                if (lang == "ar") continue;
                int n = 0;
                foreach (var l in lines) if (HasSignature(Exact(l.text, lang))) n++;
                if (n < lines.Length / 3) e.Add(lang + ": only " + n + " lines keep a Saudi word");
            }
            return e;
        }
    }

    /// <summary>Whether subtitles show. On by default for every player.</summary>
    public static class Subtitles
    {
        public static bool On(SaveData d) => d == null || d.subtitles != 0;
    }
}
