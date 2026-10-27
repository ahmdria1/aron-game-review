// (c) Ahmed Alamrani. All rights reserved.
using System;
using System.Collections.Generic;
using System.Globalization;

namespace Aron.Logic.Races
{
    /// <summary>Which kind of race finished, for points. Online counts only with real racers.</summary>
    public enum PointsSource { AiRace, Ghost, Online }

    public class PointsAward
    {
        /// <summary>What the finish pays: 5, 3 or 1 for 1st, 2nd and 3rd, times the Hard and online multipliers (rounded down).</summary>
        public int finish;
        public int Total => finish;
        public string reason = "";
    }

    /// <summary>
    /// Race points: what a finish pays, the daily limits that stop farming, and the spendable balance used for 48-hour
    /// rentals. Points never buy anything for good: a mount or vehicle is owned only by a real purchase.
    /// All values are in Resources/Mounts/race_points.json (defaults here match it) and documented in docs/mounts.md.
    /// </summary>
    [Serializable]
    public class PointsTable
    {
        public int version = 2;
        /// <summary>Points for 1st, 2nd and 3rd. Computer-racer races and ghost races pay the same, on Normal and Hard only.</summary>
        public int[] finish = { 5, 3, 1 };
        /// <summary>Hard multiplies the points; online races too (ranked, at least two real racers). Results are rounded down.</summary>
        public float hardMultiplier = 1.5f, onlineMultiplier = 2f;
        public int minRealRacersOnline = 2;
        /// <summary>Most awarded finishes per UTC day for one track and source, and for online races overall.</summary>
        public int dailyPerTrack = 3, dailyOnline = 10;
        /// <summary>Most points per UTC day from every race together: one rental's worth.</summary>
        public int dailyPoints = 100;
        /// <summary>Race points a 48-hour rental of any premium item costs, and its length.</summary>
        public int rentPoints = 100, rentHours = 48;

        public static PointsTable Default() => new PointsTable();

        public List<string> Validate()
        {
            var e = new List<string>();
            if (finish == null || finish.Length != 3) { e.Add("three podium places"); return e; }
            for (int i = 0; i < 3; i++)
            {
                if (finish[i] < 0) e.Add("negative points");
                if (i > 0 && finish[i] > finish[i - 1]) e.Add("1st must pay the most");
            }
            if (hardMultiplier < 1f || onlineMultiplier < 1f) e.Add("multipliers at least 1");
            if (minRealRacersOnline < 2) e.Add("online points need at least two real racers");
            if (rentPoints <= 0 || rentHours <= 0) e.Add("rental");
            if (dailyPerTrack < 1 || dailyOnline < 1 || dailyPoints < 1) e.Add("daily limits");
            return e;
        }
    }

    public static class RacePoints
    {
        public const int MaxPlace = 3;

        /// <summary>
        /// Points for one finished race: 5, 3, 1 for the podium. Easy pays nothing (ghost races too). Hard multiplies by 1.5,
        /// online by 2 (only with at least two real racers; computer racers never count), always rounded down.
        /// </summary>
        public static PointsAward Award(PointsTable t, PointsSource source, string difficulty, int place, int realRacers, bool finished = true)
        {
            var a = new PointsAward();
            if (!finished) { a.reason = "not_finished"; return a; }
            if (place < 1 || place > MaxPlace) { a.reason = "no_podium"; return a; }
            if (difficulty == "easy") { a.reason = "easy"; return a; }
            if (source == PointsSource.Online && realRacers < t.minRealRacersOnline) { a.reason = "not_enough_real_racers"; return a; }
            float mult = 1f;
            if (difficulty == "hard") mult *= t.hardMultiplier;
            if (source == PointsSource.Online) mult *= t.onlineMultiplier;
            a.finish = (int)Math.Floor(t.finish[place - 1] * mult + 1e-4f);
            a.reason = "ok";
            return a;
        }

        public static string Day(DateTime utc) => utc.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);

        private static string Key(string day, PointsSource s, string track) => day + "|" + s.ToString().ToLowerInvariant() + "|" + (track ?? "");

        private static int Count(SaveData d, string key)
        {
            foreach (var e in d.pointLog)
            {
                int eq = e.LastIndexOf('=');
                if (eq > 0 && e.Substring(0, eq) == key && int.TryParse(e.Substring(eq + 1), out int n)) return n;
            }
            return 0;
        }

        private static void Bump(SaveData d, string key)
        {
            int n = Count(d, key) + 1;
            d.pointLog.RemoveAll(e => e.StartsWith(key + "=", StringComparison.Ordinal));
            d.pointLog.Add(key + "=" + n);
        }

        /// <summary>
        /// Awards and banks the points (balance and lifetime), unless today's limit for this track and source is used up.
        /// Old days are dropped from the log. Returns what was really paid.
        /// </summary>
        public static PointsAward Grant(SaveData d, PointsTable t, PointsSource source, string difficulty, string track, int place, int realRacers, DateTime utcNow, bool finished = true)
        {
            SaveUpgrade.Fill(d);
            var a = Award(t, source, difficulty, place, realRacers, finished);
            if (a.Total <= 0) return a;
            string day = Day(utcNow);
            d.pointLog.RemoveAll(e => !e.StartsWith(day + "|", StringComparison.Ordinal));
            string key = Key(day, source, track);
            if (Count(d, key) >= t.dailyPerTrack) { a.finish = 0; a.reason = "daily_limit"; return a; }
            if (source == PointsSource.Online)
            {
                int online = 0;
                foreach (var e in d.pointLog) if (e.Contains("|online|")) { int eq = e.LastIndexOf('='); if (eq > 0 && int.TryParse(e.Substring(eq + 1), out int n)) online += n; }
                if (online >= t.dailyOnline) { a.finish = 0; a.reason = "daily_limit"; return a; }
            }
            // One rental's worth a day from every race together; the last race pays only what is left.
            string totalKey = day + "|points|";
            int earned = Count(d, totalKey);
            int left = t.dailyPoints - earned;
            if (left <= 0) { a.finish = 0; a.reason = "daily_points_limit"; return a; }
            if (a.finish > left) { a.finish = left; a.reason = "daily_points_limit_partial"; }
            Bump(d, key);
            d.pointLog.RemoveAll(e => e.StartsWith(totalKey + "=", StringComparison.Ordinal));
            d.pointLog.Add(totalKey + "=" + (earned + a.finish));
            d.racePoints += a.Total;
            d.racePointsLifetime += a.Total; // only a statistic now: it opens nothing
            return a;
        }

        public static bool Spend(SaveData d, int points)
        {
            if (points <= 0 || d.racePoints < points) return false;
            d.racePoints -= points;
            return true;
        }

        /// <summary>Gold medals earned in all races (see ProfileStats medals "track=1").</summary>
        public static int GoldMedals(SaveData d)
        {
            int n = 0;
            foreach (var e in d.raceMedals ?? new List<string>()) if (e.EndsWith("=1", StringComparison.Ordinal)) n++;
            return n;
        }
    }

    /// <summary>Fills fields an older save lacks (kept apart from StoreInventory.Upgrade so the race code can call it alone).</summary>
    public static class SaveUpgrade
    {
        public static void Fill(SaveData d)
        {
            if (d.pointLog == null) d.pointLog = new List<string>();
            if (d.rentals == null) d.rentals = new List<string>();
        }
    }

    /// <summary>One online race result as the client sends it to the Cloud Code module that grants points.</summary>
    [Serializable]
    public class RaceClaim
    {
        public string playerId, matchId, track, difficulty = "normal";
        public int place, realRacers, aiRacers;
        public float seconds;
        public string signature;
    }

    /// <summary>
    /// The check the server runs before it grants online race points. This class is shared with the Cloud Code module
    /// (Ahmed creates it in UGS, see docs/mounts.md): the client's own check is only for showing the result early.
    /// </summary>
    public class ClaimValidator
    {
        private readonly HashSet<string> seen = new HashSet<string>();
        private readonly RaceFile races;
        private readonly string salt;

        public ClaimValidator(RaceFile races, string salt) { this.races = races; this.salt = salt; }

        public static string SignClaim(RaceClaim c, string salt) =>
            Safety.ScoreCheck.Sign(c.playerId + "#" + c.matchId + "#" + c.place + "#" + c.realRacers + "#" + c.aiRacers, c.track, c.seconds, salt);

        /// <summary>Null when the claim is fine, otherwise the reason it is refused.</summary>
        public string Check(RaceClaim c, float topSpeed)
        {
            if (c == null || string.IsNullOrEmpty(c.playerId) || string.IsNullOrEmpty(c.matchId)) return "missing_ids";
            var t = races.Track(c.track);
            if (t == null) return "unknown_track";
            if (c.realRacers < 2) return "not_enough_real_racers";
            if (c.aiRacers < 0 || c.realRacers + c.aiRacers > Online.LobbyFlow.MaxPlayers) return "bad_field_size";
            if (c.place < 1 || c.place > c.realRacers + c.aiRacers) return "bad_place";
            if (!Safety.ScoreCheck.Plausible(c.seconds, t, races, topSpeed)) return "implausible_time";
            if (SignClaim(c, salt) != c.signature) return "bad_signature";
            if (!seen.Add(c.playerId + "|" + c.matchId)) return "duplicate_match";
            return null;
        }
    }
}
