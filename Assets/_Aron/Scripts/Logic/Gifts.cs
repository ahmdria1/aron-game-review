// (c) Ahmed Alamrani. All rights reserved.
using System;
using System.Collections.Generic;
using System.Globalization;

namespace Aron.Logic
{
    /// <summary>One line of the gift table in Resources/Config/gifts.json.</summary>
    [Serializable]
    public class GiftEntry
    {
        /// <summary>coins, costume_piece, food_card, helper or ticket.</summary>
        public string kind = "coins";
        public int weight = 1;
        public int min = 10, max = 30;
    }

    [Serializable]
    public class GiftTable
    {
        public float hoursBetween = 6f;
        public GiftEntry[] entries = Array.Empty<GiftEntry>();
        /// <summary>Costumes that can be put together from gift pieces, in the order they're worked on.</summary>
        public string[] pieceCostumes = Array.Empty<string>();
        public int piecesPerCostume = 5;
        /// <summary>Extra coins per day of streak, as a fraction, and the most days that count.</summary>
        public float streakBonusPerDay = 0.25f;
        public int streakCap = 7;

        public static readonly string[] Kinds = { "coins", "costume_piece", "food_card", "helper", "ticket" };

        public List<string> Validate()
        {
            var e = new List<string>();
            if (hoursBetween < 1f || hoursBetween > 48f) e.Add("hours between gifts 1..48");
            if (entries.Length == 0) e.Add("no gift entries");
            int total = 0;
            foreach (var g in entries)
            {
                if (Array.IndexOf(Kinds, g.kind) < 0) e.Add("unknown gift kind " + g.kind);
                if (g.weight <= 0) e.Add(g.kind + ": weight must be positive");
                if (g.kind == "coins" && (g.min <= 0 || g.max < g.min)) e.Add("coins: min..max");
                total += Math.Max(0, g.weight);
            }
            if (total <= 0) e.Add("weights add up to nothing");
            if (piecesPerCostume < 2) e.Add("at least two pieces per costume");
            if (streakCap < 1 || streakBonusPerDay < 0f || streakBonusPerDay > 1f) e.Add("streak bonus out of range");
            return e;
        }

        /// <summary>Weighted pick. <paramref name="roll"/> is in [0, 1).</summary>
        public GiftEntry Pick(double roll)
        {
            int total = 0;
            foreach (var g in entries) total += Math.Max(0, g.weight);
            if (total <= 0) return null;
            double at = Math.Max(0.0, Math.Min(0.999999, roll)) * total;
            foreach (var g in entries)
            {
                at -= Math.Max(0, g.weight);
                if (at < 0) return g;
            }
            return entries[entries.Length - 1];
        }

        public float StreakMultiplier(int streak) => 1f + Math.Max(0, Math.Min(streak, streakCap) - 1) * streakBonusPerDay;
    }

    /// <summary>What a broken gift box gave.</summary>
    public class GiftResult
    {
        public string kind;
        public int coins;
        /// <summary>Costume the piece belongs to, or the costume that just got finished.</summary>
        public string costumeId = "";
        public int pieces, piecesNeeded;
        public bool costumeUnlocked;
        public string foodId = "";
        public string helper = "";
        public int streak;
    }

    /// <summary>
    /// The free gift box. One every few hours, judged by the UTC clock with two guards: the latest time ever seen is
    /// kept, so winding the clock back holds the gift until real time catches up again, and within one session the
    /// clock may not run ahead of the game's own steady timer by more than a couple of minutes.
    /// </summary>
    public class GiftTimer
    {
        public const double SlackSeconds = 120.0;
        private readonly SaveData data;
        private readonly GiftTable table;
        private DateTime sessionStartUtc;
        private double sessionStartSteady = -1;

        public GiftTimer(SaveData data, GiftTable table) { this.data = data; this.table = table; }

        /// <summary>
        /// The time to trust. <paramref name="steadySeconds"/> is a clock that only ever goes forward while the game
        /// runs (Unity's realtimeSinceStartup).
        /// </summary>
        public DateTime Trusted(DateTime utcNow, double steadySeconds)
        {
            if (sessionStartSteady < 0 || steadySeconds < sessionStartSteady) { sessionStartSteady = steadySeconds; sessionStartUtc = utcNow; }
            var steadyNow = sessionStartUtc.AddSeconds(steadySeconds - sessionStartSteady);
            if (utcNow > steadyNow.AddSeconds(SlackSeconds)) return steadyNow; // clock pushed ahead mid-session
            return utcNow;
        }

        /// <summary>Remembers the latest time seen. Call whenever the menu or hub checks the gift.</summary>
        public void Observe(DateTime trustedUtc)
        {
            if (trustedUtc.Ticks > data.giftLatestSeen) data.giftLatestSeen = trustedUtc.Ticks;
        }

        public bool RolledBack(DateTime trustedUtc) => trustedUtc.Ticks + TimeSpan.FromSeconds(SlackSeconds).Ticks < data.giftLatestSeen;

        public DateTime NextGift => data.giftLastClaim <= 0 ? DateTime.MinValue
            : new DateTime(data.giftLastClaim, DateTimeKind.Utc).AddHours(table.hoursBetween);

        public bool Ready(DateTime trustedUtc) => !RolledBack(trustedUtc) && trustedUtc >= NextGift;

        /// <summary>How long until the next gift; zero when it's ready. While the clock is wound back it counts from the latest time seen.</summary>
        public TimeSpan Remaining(DateTime trustedUtc)
        {
            var from = trustedUtc;
            var wait = TimeSpan.Zero;
            if (RolledBack(trustedUtc))
            {
                from = new DateTime(data.giftLatestSeen, DateTimeKind.Utc);
                wait = from - trustedUtc;
            }
            var left = wait + (NextGift > from ? NextGift - from : TimeSpan.Zero);
            return left < TimeSpan.Zero ? TimeSpan.Zero : left;
        }

        /// <summary>Opens the box: rolls the table, pays out, starts the next wait. Null when it isn't ready.</summary>
        public GiftResult Claim(DateTime trustedUtc, Random rnd, IList<string> allFoodIds)
        {
            Observe(trustedUtc);
            if (!Ready(trustedUtc)) return null;
            string today = trustedUtc.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
            string yesterday = trustedUtc.AddDays(-1).ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
            if (data.giftLastDay == today) { }
            else if (data.giftLastDay == yesterday) data.giftStreak++;
            else data.giftStreak = 1;
            if (data.giftStreak < 1) data.giftStreak = 1;
            data.giftLastDay = today;
            data.giftLastClaim = trustedUtc.Ticks;

            var entry = table.Pick(rnd.NextDouble()) ?? new GiftEntry();
            var r = new GiftResult { kind = entry.kind, streak = data.giftStreak };
            if (entry.kind == "food_card" && !GiveFood(r, rnd, allFoodIds)) entry = null;
            else if (entry.kind == "costume_piece" && !GivePiece(r)) entry = null;
            else if (entry.kind == "helper")
            {
                r.helper = Helpers.Kinds[rnd.Next(Helpers.Kinds.Length)];
                Helpers.Add(data, r.helper, 1);
            }
            else if (entry.kind == "ticket") data.tickets++;
            if (entry == null || r.kind == "coins")
            {
                var coins = entry != null && entry.kind == "coins" ? entry : FirstCoins();
                r.kind = "coins";
                int baseCoins = coins.min + rnd.Next(Math.Max(1, coins.max - coins.min + 1));
                r.coins = (int)Math.Round(baseCoins * table.StreakMultiplier(data.giftStreak));
                data.coins += r.coins;
            }
            return r;
        }

        private GiftEntry FirstCoins()
        {
            foreach (var g in table.entries) if (g.kind == "coins") return g;
            return new GiftEntry();
        }

        private bool GiveFood(GiftResult r, Random rnd, IList<string> all)
        {
            if (all == null) return false;
            var missing = new List<string>();
            foreach (var f in all) if (!data.foodsCollected.Contains(f)) missing.Add(f);
            if (missing.Count == 0) return false;
            r.foodId = missing[rnd.Next(missing.Count)];
            data.foodsCollected.Add(r.foodId);
            return true;
        }

        private bool GivePiece(GiftResult r)
        {
            foreach (var id in table.pieceCostumes)
            {
                if (data.ownedCostumes.Contains(id)) continue;
                int have = Pieces(data, id) + 1;
                r.costumeId = id; r.pieces = have; r.piecesNeeded = table.piecesPerCostume;
                SetPieces(data, id, have);
                if (have >= table.piecesPerCostume)
                {
                    data.ownedCostumes.Add(id);
                    r.costumeUnlocked = true;
                }
                return true;
            }
            return false;
        }

        public static int Pieces(SaveData d, string costumeId)
        {
            if (d.costumePieces == null) return 0;
            foreach (var e in d.costumePieces)
                if (e.StartsWith(costumeId + "=", StringComparison.Ordinal) && int.TryParse(e.Substring(costumeId.Length + 1), out int n)) return n;
            return 0;
        }

        private static void SetPieces(SaveData d, string costumeId, int n)
        {
            if (d.costumePieces == null) d.costumePieces = new List<string>();
            d.costumePieces.RemoveAll(e => e.StartsWith(costumeId + "=", StringComparison.Ordinal));
            d.costumePieces.Add(costumeId + "=" + n);
        }
    }
}
