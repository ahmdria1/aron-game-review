// (c) Ahmed Alamrani. All rights reserved.
using System;
using System.Collections.Generic;
using Aron.Logic.Passport;
using Aron.Logic.Regions;

namespace Aron.Logic.Culture
{
    /// <summary>
    /// One cultural collectible: a craft, instrument, textile or gem each country is known for (Resources/Museum/artifacts.json).
    /// Always an everyday or craft object, never a flag, a seal, a weapon or anything religious.
    /// </summary>
    [Serializable]
    public class ArtifactDef
    {
        public string id, country;
        /// <summary>textile, craft, instrument, glass or gem.</summary>
        public string kind;
        /// <summary>The silhouette the museum draws: cloth, boat, pearl, hood, burner, window, instrument, bottle, tile, scroll, basket, pouf, vase, pot, plate, panel, paper, gem, beads, figure, cup, clock.</summary>
        public string shape;
        public string color = "#C8A060", color2 = "#7A4A22";
        public LocalText name = new LocalText();
        /// <summary>One short line, Arabic and English (other languages show the English).</summary>
        public LocalText fact = new LocalText();
    }

    [Serializable]
    public class ArtifactFile
    {
        public static readonly string[] Kinds = { "textile", "craft", "instrument", "glass", "gem" };
        public ArtifactDef[] artifacts = Array.Empty<ArtifactDef>();

        public ArtifactDef For(string country)
        {
            foreach (var a in artifacts) if (a.country == country) return a;
            return null;
        }

        public List<string> Validate(IEnumerable<string> countries)
        {
            var e = new List<string>();
            var ids = new HashSet<string>(); var seen = new HashSet<string>();
            foreach (var a in artifacts)
            {
                if (string.IsNullOrEmpty(a.id) || !a.id.StartsWith("artifact_")) e.Add("bad id " + a.id);
                if (!ids.Add(a.id)) e.Add("duplicate id " + a.id);
                if (!seen.Add(a.country)) e.Add("two artifacts for " + a.country);
                if (Array.IndexOf(Kinds, a.kind) < 0) e.Add(a.id + ": unknown kind " + a.kind);
                foreach (var l in a.name.Missing()) e.Add(a.id + ": name missing in " + l);
                if (string.IsNullOrWhiteSpace(a.fact.ar) || string.IsNullOrWhiteSpace(a.fact.en)) e.Add(a.id + ": fact needs Arabic and English");
                string low = (a.name.en + " " + a.fact.en).ToLowerInvariant();
                foreach (var w in Banned) if (low.Contains(w)) e.Add(a.id + ": banned word '" + w + "'");
            }
            foreach (var c in countries) if (!seen.Contains(c)) e.Add("no artifact for " + c);
            return e;
        }

        /// <summary>Words an artifact must never carry: state symbols, weapons, worship.</summary>
        public static readonly string[] Banned = { "flag", "emblem", "coat of arms", "seal", "sword", "dagger", "khanjar", "jambiya", "gun", "rifle", "idol", "relic", "prayer", "altar" };
    }

    public class MuseumEntry
    {
        public ArtifactDef artifact;
        public string country, localCountry;
        public bool found;
        public string date = "";
    }

    /// <summary>Finding artifacts and the museum list.</summary>
    public static class ArtifactBook
    {
        public const string Sfx = PassportBook.StampSfx, Haptic = PassportBook.StampHaptic, Fx = "sparkle";

        public static bool Has(SaveData d, string country) => PassportBook.Get(d.artifactsFound, country).Length > 0;
        public static string Date(SaveData d, string country) => PassportBook.Get(d.artifactsFound, country);

        /// <summary>
        /// A country's artifact is found the first time its hidden star is earned on any of its ordinary stages (every golden box in one
        /// run). Races have no golden boxes. Returns the artifact when it is new, else null.
        /// </summary>
        public static ArtifactDef OnHiddenStar(RegionCatalog catalog, SaveData data, ArtifactFile file, string stageId, bool hiddenStar, DateTime utcNow)
        {
            if (!hiddenStar) return null;
            var s = catalog.Stage(stageId, out int r, out _);
            if (s == null || s.IsRace) return null;
            string country = ProgressRing.CountryOf(catalog.regions[r]);
            var a = file.For(country);
            if (a == null || Has(data, country)) return null;
            string day = PassportBook.Day(utcNow);
            var list = data.artifactsFound;
            PassportBook.Set(ref list, country, day);
            data.artifactsFound = list;
            return a;
        }

        public static int Count(SaveData d, ArtifactFile file, out int total)
        {
            total = file.artifacts.Length; int n = 0;
            foreach (var a in file.artifacts) if (Has(d, a.country)) n++;
            return n;
        }

        /// <summary>The museum shelves: one entry per country in the player's ring order (home first); not-yet-found ones are shown as shadows.</summary>
        public static List<MuseumEntry> Entries(RegionCatalog catalog, SaveData data, ArtifactFile file)
        {
            var list = new List<MuseumEntry>();
            foreach (var c in ProgressRing.CountryOrder(catalog, data.homeCountry))
            {
                var a = file.For(c);
                if (a == null) continue;
                string date = Date(data, c);
                list.Add(new MuseumEntry { artifact = a, country = c, localCountry = PassportBook.LocalNames.TryGetValue(c, out var n) ? n : c, found = date.Length > 0, date = date });
            }
            return list;
        }

        /// <summary>The museum's own badge: every country's artifact found.</summary>
        public static bool Complete(SaveData d, ArtifactFile file) { Count(d, file, out int t); return Count(d, file, out _) == t && t > 0; }
    }
}
