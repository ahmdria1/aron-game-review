// (c) Ahmed Alamrani. All rights reserved.
using System;
using System.Collections.Generic;
using System.Globalization;

namespace Aron.Logic.Regions
{
    /// <summary>Today's short challenge, picked the same way on every phone from the UTC date. Works offline.</summary>
    public class DailyChallenge
    {
        public const string TimeAttack = "time", Boxes = "boxes", NoDamage = "nohit";
        public static readonly string[] Kinds = { TimeAttack, Boxes, NoDamage };
        public const int RewardCoins = 150;

        public string day;
        public string kind;
        public string stageId;
        /// <summary>Seconds to beat for time attack, boxes to break for the box count.</summary>
        public int target;
        public int reward = RewardCoins;

        public static string DayOf(DateTime utc) => utc.ToUniversalTime().ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);

        /// <summary>Same seed for the same UTC day everywhere (no string.GetHashCode, which changes per run).</summary>
        public static int Seed(string day)
        {
            unchecked
            {
                int h = 17;
                foreach (char c in day) h = h * 31 + c;
                return h & 0x7fffffff;
            }
        }

        /// <summary>
        /// Picks from the stages the player has already finished (any stage when none are), never a boss. The time
        /// target is the layout's estimate plus some room, the box target about three quarters of the boxes.
        /// </summary>
        public static DailyChallenge For(DateTime utc, RegionCatalog catalog, SaveData data)
        {
            string day = DayOf(utc);
            var rnd = new Random(Seed(day));
            var pool = new List<StageDef>();
            foreach (var r in catalog.regions)
                foreach (var s in r.stages)
                    if (!s.boss && StageProgress.IsDone(data, s.id)) pool.Add(s);
            if (pool.Count == 0)
                foreach (var r in catalog.regions) foreach (var s in r.stages) if (!s.boss) { pool.Add(s); break; }
            if (pool.Count == 0) return null;
            pool.Sort((a, b) => string.CompareOrdinal(a.id, b.id));
            var st = pool[rnd.Next(pool.Count)];
            var c = new DailyChallenge { day = day, stageId = st.id, kind = Kinds[rnd.Next(Kinds.Length)] };
            var layout = StageLayout.Generate(st);
            if (c.kind == TimeAttack) c.target = (int)Math.Ceiling(layout.EstimatedSeconds * 1.15f / 10f) * 10;
            else if (c.kind == Boxes) c.target = Math.Max(1, layout.BoxCount * 3 / 4);
            return c;
        }

        public bool Met(float seconds, int boxes, int hits)
        {
            if (kind == TimeAttack) return seconds > 0f && seconds <= target;
            if (kind == Boxes) return boxes >= target;
            return hits == 0;
        }

        /// <summary>Pays out once per UTC day. Returns the coins given (0 when already done or not met).</summary>
        public int Complete(SaveData data, float seconds, int boxes, int hits)
        {
            if (data.dailyDone == day || !Met(seconds, boxes, hits)) return 0;
            string yesterday = DateTime.ParseExact(day, "yyyy-MM-dd", CultureInfo.InvariantCulture).AddDays(-1).ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
            data.dailyStreak = data.dailyDone == yesterday ? data.dailyStreak + 1 : 1;
            data.dailyDone = day;
            data.coins += reward;
            return reward;
        }
    }
}
