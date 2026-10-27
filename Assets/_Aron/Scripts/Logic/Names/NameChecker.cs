// (c) Ahmed Alamrani. All rights reserved.
using System;
using System.Collections.Generic;

namespace Aron.Logic.Names
{
    /// <summary>One category of blocked words in one language. terms: found anywhere inside the name. words: a whole part of the name (or the whole name). exact: the whole name only.</summary>
    [Serializable]
    public class BlockCategory
    {
        public string[] terms = new string[0];
        public string[] words = new string[0];
        public string[] exact = new string[0];
    }

    /// <summary>The file Resources/Names/blocklist_xx.json.</summary>
    [Serializable]
    public class BlocklistFile
    {
        public string lang = "";
        public BlockCategory profanity = new BlockCategory();
        public BlockCategory insult = new BlockCategory();
        public BlockCategory hate = new BlockCategory();
        public BlockCategory sexual = new BlockCategory();
        public BlockCategory figures = new BlockCategory();

        public IEnumerable<KeyValuePair<string, BlockCategory>> Categories()
        {
            yield return new KeyValuePair<string, BlockCategory>("profanity", profanity);
            yield return new KeyValuePair<string, BlockCategory>("insult", insult);
            yield return new KeyValuePair<string, BlockCategory>("hate", hate);
            yield return new KeyValuePair<string, BlockCategory>("sexual", sexual);
            yield return new KeyValuePair<string, BlockCategory>("figures", figures);
        }
    }

    /// <summary>The file Resources/Names/reserved.json. exact: the name (after look-alike folding and with trailing digits dropped) may not be this. contains: may not contain this.</summary>
    [Serializable]
    public class ReservedFile
    {
        public string[] exact = new string[0];
        public string[] contains = new string[0];
    }

    public class NameVerdict
    {
        public bool Ok;
        public string Code;      // a NameCode, null when Ok
        public string Clean;     // the name that will be kept
        public string Category;  // which blocklist category matched
        public string Match;     // the matched term (for logs and tests, never shown to a child)
    }

    /// <summary>The full name check: shape, then the blocklists of every language, then the reserved names. Used by the client and, with the same files, by the server.</summary>
    public class NameChecker
    {
        private readonly List<string[]> terms = new List<string[]>();   // [category, key, collapsedKey]
        private readonly List<string[]> words = new List<string[]>();
        private readonly List<string[]> exact = new List<string[]>();
        private readonly HashSet<string> reservedExact = new HashSet<string>();
        private readonly List<string> reservedContains = new List<string>();

        public int TermCount => terms.Count + words.Count + exact.Count;

        public NameChecker(IEnumerable<BlocklistFile> lists, ReservedFile reserved)
        {
            if (lists != null) foreach (var f in lists) Add(f);
            if (reserved != null)
            {
                foreach (var r in reserved.exact) { var k = PlayerNameRules.Key(r); if (k.Length > 0) reservedExact.Add(k); }
                foreach (var r in reserved.contains) { var k = PlayerNameRules.Key(r); if (k.Length > 0) reservedContains.Add(k); }
            }
        }

        private void Add(BlocklistFile f)
        {
            foreach (var cat in f.Categories())
            {
                Put(terms, cat.Key, cat.Value.terms);
                Put(words, cat.Key, cat.Value.words);
                Put(exact, cat.Key, cat.Value.exact);
            }
        }

        private static void Put(List<string[]> into, string cat, string[] src)
        {
            if (src == null) return;
            foreach (var t in src)
            {
                var k = PlayerNameRules.Key(t);
                if (k.Length < 2) continue;
                into.Add(new[] { cat, k, PlayerNameRules.Collapse(k), t });
            }
        }

        /// <summary>The word lists only (no shape rules): what blocked word, if any, is in this text.</summary>
        public bool Blocked(string name, out string category, out string match)
        {
            // Twice: once with trailing digits dropped ("shit2"), once with them read as look-alike letters ("m3rd4").
            return BlockedBy(name, true, out category, out match) || BlockedBy(name, false, out category, out match);
        }

        private bool BlockedBy(string name, bool stripTrailing, out string category, out string match)
        {
            category = null; match = null;
            string key = PlayerNameRules.Key(name, stripTrailing);
            string col = PlayerNameRules.Collapse(key);
            foreach (var t in terms)
            {
                if (key.Contains(t[1]) || (t[2].Length >= 4 && col.Contains(t[2]))) { category = t[0]; match = t[3]; return true; }
            }
            var tokens = PlayerNameRules.Tokens(name, stripTrailing);
            foreach (var w in words)
            {
                if (key == w[1] || col == w[2]) { category = w[0]; match = w[3]; return true; }
                foreach (var tk in tokens) if (tk == w[1] || PlayerNameRules.Collapse(tk) == w[2]) { category = w[0]; match = w[3]; return true; }
            }
            foreach (var e in exact)
            {
                if (key == e[1] || col == e[2]) { category = e[0]; match = e[3]; return true; }
            }
            return false;
        }

        public bool IsReserved(string name)
        {
            string key = PlayerNameRules.Key(name);
            if (reservedExact.Contains(key)) return true;
            foreach (var c in reservedContains) if (key.Contains(c)) return true;
            return false;
        }

        public NameVerdict Check(string name)
        {
            var v = new NameVerdict();
            string code = PlayerNameRules.CheckShape(name, out string clean);
            v.Clean = clean;
            if (code != null) { v.Code = code; return v; }
            if (Blocked(clean, out v.Category, out v.Match)) { v.Code = NameCode.Blocked; return v; }
            if (IsReserved(clean)) { v.Code = NameCode.Reserved; return v; }
            v.Ok = true;
            return v;
        }
    }
}
