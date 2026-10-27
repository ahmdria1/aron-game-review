// (c) Ahmed Alamrani. All rights reserved.
using System;
using System.Collections.Generic;

namespace Aron.Logic.Audio
{
    [Serializable]
    public class VoiceLine
    {
        public string id;
        /// <summary>aron, noura, any, or boss_&lt;model&gt;.</summary>
        public string character;
        public string textAr;
        /// <summary>File name under Resources/Audio/Voice, without extension. Empty until it's recorded.</summary>
        public string clip;
        public string kind;
    }

    /// <summary>Resources/Audio/VoiceLines.json: every line a character can say during play, and its recording.</summary>
    [Serializable]
    public class VoiceLineFile
    {
        public VoiceLine[] lines = Array.Empty<VoiceLine>();

        [NonSerialized] private Dictionary<string, VoiceLine> byId;

        public VoiceLine Find(string id)
        {
            if (string.IsNullOrEmpty(id) || lines == null) return null;
            if (byId == null)
            {
                byId = new Dictionary<string, VoiceLine>();
                foreach (var l in lines) if (l != null && !string.IsNullOrEmpty(l.id) && !byId.ContainsKey(l.id)) byId[l.id] = l;
            }
            return byId.TryGetValue(id, out var v) ? v : null;
        }

        /// <summary>The clip to load for a line, or null when nothing has been recorded yet.</summary>
        public string ClipFor(string id)
        {
            var l = Find(id);
            return l == null || string.IsNullOrEmpty(l.clip) ? null : l.clip;
        }

        public List<string> Validate()
        {
            var problems = new List<string>();
            var seen = new HashSet<string>();
            if (lines == null) { problems.Add("no lines"); return problems; }
            foreach (var l in lines)
            {
                if (l == null || string.IsNullOrEmpty(l.id)) { problems.Add("line without an id"); continue; }
                if (!seen.Add(l.id)) problems.Add("duplicate " + l.id);
                if (string.IsNullOrEmpty(l.character)) problems.Add(l.id + " has no character");
                if (string.IsNullOrEmpty(l.textAr)) problems.Add(l.id + " has no text");
                if (!string.IsNullOrEmpty(l.clip) && (l.clip.Contains("/") || l.clip.Contains("."))) problems.Add(l.id + " clip should be a bare name");
            }
            return problems;
        }
    }
}
