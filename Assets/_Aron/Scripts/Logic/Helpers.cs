// (c) Ahmed Alamrani. All rights reserved.
using System;
using System.Collections.Generic;

namespace Aron.Logic
{
    /// <summary>
    /// Small one-stage helpers: a golden bisht to start with, a coin magnet, and an extra lantern early on. They can
    /// be bought, but boxes and the free gift hand them out too. Never on hard, never in a boss fight, and at most one
    /// of each kind per stage. Nothing here gives lives or opens stages.
    /// </summary>
    public static class Helpers
    {
        public const string BishtStart = "bisht_start", CoinMagnet = "coin_magnet", EarlyLantern = "early_lantern";
        public static readonly string[] Kinds = { BishtStart, CoinMagnet, EarlyLantern };

        public static bool IsKind(string k) => Array.IndexOf(Kinds, k) >= 0;

        public static int Count(SaveData d, string kind)
        {
            if (d.helpers == null) return 0;
            foreach (var e in d.helpers)
                if (e.StartsWith(kind + "=", StringComparison.Ordinal) && int.TryParse(e.Substring(kind.Length + 1), out int n)) return n;
            return 0;
        }

        public static void Add(SaveData d, string kind, int n)
        {
            if (!IsKind(kind) || n <= 0) return;
            Set(d, kind, Count(d, kind) + n);
        }

        private static void Set(SaveData d, string kind, int n)
        {
            if (d.helpers == null) d.helpers = new List<string>();
            d.helpers.RemoveAll(e => e.StartsWith(kind + "=", StringComparison.Ordinal));
            if (n > 0) d.helpers.Add(kind + "=" + n);
        }

        public enum Use { Ok, NoneLeft, HardMode, BossFight, AlreadyUsed, Unknown }

        /// <summary>Whether a helper may go into this stage.</summary>
        public static Use Check(SaveData d, string kind, bool bossStage, ICollection<string> usedThisStage)
        {
            if (!IsKind(kind)) return Use.Unknown;
            if (d.difficulty == "hard") return Use.HardMode;
            if (bossStage) return Use.BossFight;
            if (usedThisStage != null && usedThisStage.Contains(kind)) return Use.AlreadyUsed;
            if (Count(d, kind) <= 0) return Use.NoneLeft;
            return Use.Ok;
        }

        /// <summary>Takes one from the save and marks it used for this stage when allowed.</summary>
        public static Use Spend(SaveData d, string kind, bool bossStage, ICollection<string> usedThisStage)
        {
            var r = Check(d, kind, bossStage, usedThisStage);
            if (r != Use.Ok) return r;
            Set(d, kind, Count(d, kind) - 1);
            usedThisStage?.Add(kind);
            return r;
        }
    }
}
