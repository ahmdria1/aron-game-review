// (c) Ahmed Alamrani. All rights reserved.
using System;
using System.Collections.Generic;

namespace Aron.Logic.Names
{
    public enum NameStatus { Ok, Invalid, Blocked, Reserved, Taken, Cooldown, RateLimited, NotFound, Offline, Error }

    public class NameResult
    {
        public NameStatus Status;
        public string Code;        // a NameCode when not Ok
        public string Name = "";   // the kept name, without the number
        public int Tag;            // 0 when the name is unique on its own, otherwise 1000-9999
        public bool Available;     // for a check: nobody else holds it
        public bool MustRename;    // after enough reports: the player has to pick a new name
        public string Display => NameRegistry.Display(Name, Tag);
        public bool Ok => Status == NameStatus.Ok;
    }

    /// <summary>What the server keeps for one player. Never contains anything but the chosen name and the times.</summary>
    [Serializable]
    public class NameRecord
    {
        public string playerId = "";
        public string name = "";
        public int tag;
        public string key = "";       // PlayerNameRules.UniqueKey(name)
        public long changedTicks;     // when the name was last set (UTC ticks)
        public bool mustRename;
    }

    /// <summary>
    /// The storage behind the registry. The registry needs these to be atomic: TryClaim must never give one slot to two players.
    /// In Unity Cloud Code this is a thin adapter over Cloud Save with write locks (cloud/AronNames/README.md); tests use InMemoryNameStore.
    /// </summary>
    public interface INameStore
    {
        /// <summary>Gives the slot to this player when nobody else holds it. True when the player holds it afterwards.</summary>
        bool TryClaim(string slot, string playerId);
        /// <summary>Who holds the slot, or null.</summary>
        string Owner(string slot);
        void Release(string slot, string playerId);
        NameRecord GetRecord(string playerId);
        void SetRecord(NameRecord record);
        /// <summary>Adds one to a counter that is reset every <paramref name="window"/> and returns the new value.</summary>
        int Bump(string counter, long windowId);
        /// <summary>Records a report. Returns the number of different reporters for this target, or -1 when this reporter already reported it.</summary>
        int AddReport(string targetId, string reporterId);
    }

    public class InMemoryNameStore : INameStore
    {
        private readonly Dictionary<string, string> slots = new Dictionary<string, string>();
        private readonly Dictionary<string, NameRecord> records = new Dictionary<string, NameRecord>();
        private readonly Dictionary<string, int> counters = new Dictionary<string, int>();
        private readonly Dictionary<string, HashSet<string>> reports = new Dictionary<string, HashSet<string>>();

        public virtual bool TryClaim(string slot, string playerId)
        {
            if (slots.TryGetValue(slot, out var owner)) return owner == playerId;
            slots[slot] = playerId;
            return true;
        }
        public void Release(string slot, string playerId) { if (slots.TryGetValue(slot, out var o) && o == playerId) slots.Remove(slot); }
        public NameRecord GetRecord(string playerId) => records.TryGetValue(playerId, out var r) ? Copy(r) : null;
        public void SetRecord(NameRecord r) => records[r.playerId] = Copy(r);
        public int Bump(string counter, long windowId)
        {
            string k = counter + "@" + windowId;
            counters.TryGetValue(k, out int n);
            counters[k] = ++n;
            return n;
        }
        public int AddReport(string targetId, string reporterId)
        {
            if (!reports.TryGetValue(targetId, out var set)) reports[targetId] = set = new HashSet<string>();
            return set.Add(reporterId) ? set.Count : -1;
        }
        public string Owner(string slot) => slots.TryGetValue(slot, out var o) ? o : null;
        public int ReportCount(string targetId) => reports.TryGetValue(targetId, out var s) ? s.Count : 0;
        private static NameRecord Copy(NameRecord r) => new NameRecord { playerId = r.playerId, name = r.name, tag = r.tag, key = r.key, changedTicks = r.changedTicks, mustRename = r.mustRename };
    }

    public static class ReportReasons
    {
        public const string Offensive = "offensive", Impersonation = "impersonation", RealName = "real_name", Other = "other";
        public static readonly string[] All = { Offensive, Impersonation, RealName, Other };
        public static bool IsValid(string r) { foreach (var x in All) if (x == r) return true; return false; }
    }

    /// <summary>The numbers after a name when two players want the same one: Ahmed#1234. Tries a fixed, spread-out order so a retry does not repeat.</summary>
    public static class NameDiscriminator
    {
        public const int Min = 1000, Max = 9999, Attempts = 24;
        private static readonly HashSet<int> Avoid = new HashSet<int> { 1488, 1919, 6666, 6969, 9111, 1312, 1818, 8888, 4200 };

        public static bool IsAllowed(int tag) => tag >= Min && tag <= Max && !Avoid.Contains(tag);

        /// <summary>The nth candidate for this seed. Different attempts give different numbers.</summary>
        public static int Candidate(int seed, int attempt)
        {
            long span = Max - Min + 1;
            long v = ((long)(seed & 0x7FFFFFFF) + (long)attempt * 3797L) % span; // 3797 is prime and does not divide 9000
            int tag = Min + (int)v;
            int guard = 0;
            while (!IsAllowed(tag) && guard++ < 20) tag = Min + (tag - Min + 1) % (int)span;
            return tag;
        }
    }

    /// <summary>
    /// Server-authoritative name registration. The client only asks; this checks everything again (shape, blocklists, reserved names),
    /// holds the unique slot, applies the change cooldown and rate limits, and counts reports. Runs inside the Cloud Code module.
    /// </summary>
    public class NameRegistry
    {
        public const int ChangeCooldownHours = 24;
        public const int ChecksPerMinute = 30, ReservesPerMinute = 6, ReportsPerDay = 10, ReportsToForceRename = 3;

        private readonly NameChecker checker;
        private readonly INameStore store;
        private readonly Func<long> nowTicks;
        private readonly Func<int> seed;

        public NameRegistry(NameChecker checker, INameStore store, Func<long> nowUtcTicks, Func<int> randomSeed)
        {
            this.checker = checker; this.store = store; nowTicks = nowUtcTicks; seed = randomSeed;
        }

        public static string Display(string name, int tag) => tag > 0 ? name + "#" + tag.ToString("0000") : name;
        private static string Slot(string key, int tag) => tag > 0 ? "n:" + key + "#" + tag : "n:" + key;
        private long Minute => nowTicks() / TimeSpan.TicksPerMinute;
        private long Day => nowTicks() / TimeSpan.TicksPerDay;

        private static NameResult Fail(NameStatus s, string code, string name = "") => new NameResult { Status = s, Code = code, Name = name };

        private NameResult Validate(string name, out NameVerdict v)
        {
            v = checker.Check(name);
            if (v.Ok) return null;
            var st = v.Code == NameCode.Blocked ? NameStatus.Blocked : v.Code == NameCode.Reserved ? NameStatus.Reserved : NameStatus.Invalid;
            return Fail(st, v.Code, v.Clean);
        }

        /// <summary>Is the name valid, and is it free? Claims nothing.</summary>
        public NameResult Check(string playerId, string name)
        {
            if (string.IsNullOrEmpty(playerId)) return Fail(NameStatus.Error, NameCode.Error);
            if (store.Bump("check:" + playerId, Minute) > ChecksPerMinute) return Fail(NameStatus.RateLimited, NameCode.RateLimited);
            var bad = Validate(name, out var v);
            if (bad != null) return bad;
            string key = PlayerNameRules.UniqueKey(v.Clean);
            var res = new NameResult { Status = NameStatus.Ok, Name = v.Clean };
            string owner = store.Owner(Slot(key, 0));
            res.Available = owner == null || owner == playerId;
            return res;
        }

        /// <summary>First registration, or a change. allowTag: when the name is taken, give this player a number instead of refusing.</summary>
        public NameResult Reserve(string playerId, string name, bool allowTag)
        {
            if (string.IsNullOrEmpty(playerId)) return Fail(NameStatus.Error, NameCode.Error);
            if (store.Bump("reserve:" + playerId, Minute) > ReservesPerMinute) return Fail(NameStatus.RateLimited, NameCode.RateLimited);
            var bad = Validate(name, out var v);
            if (bad != null) return bad;
            string key = PlayerNameRules.UniqueKey(v.Clean);
            var old = store.GetRecord(playerId);

            // the same name again: nothing to do
            if (old != null && old.key == key && old.name == v.Clean && !old.mustRename)
                return new NameResult { Status = NameStatus.Ok, Name = old.name, Tag = old.tag, Available = true };

            if (old != null && !old.mustRename && old.changedTicks > 0 && nowTicks() - old.changedTicks < TimeSpan.TicksPerHour * ChangeCooldownHours)
                return Fail(NameStatus.Cooldown, NameCode.Cooldown, v.Clean);

            int tag = 0;
            bool got = store.TryClaim(Slot(key, 0), playerId);
            if (!got)
            {
                if (!allowTag) return Fail(NameStatus.Taken, NameCode.Taken, v.Clean);
                int s = seed();
                for (int i = 0; i < NameDiscriminator.Attempts && !got; i++)
                {
                    int cand = NameDiscriminator.Candidate(s, i);
                    if (store.TryClaim(Slot(key, cand), playerId)) { got = true; tag = cand; }
                }
                if (!got) return Fail(NameStatus.Taken, NameCode.Taken, v.Clean);
            }

            if (old != null && (old.key != key || old.tag != tag)) store.Release(Slot(old.key, old.tag), playerId);
            store.SetRecord(new NameRecord { playerId = playerId, name = v.Clean, tag = tag, key = key, changedTicks = nowTicks(), mustRename = false });
            return new NameResult { Status = NameStatus.Ok, Name = v.Clean, Tag = tag, Available = true };
        }

        public NameResult Change(string playerId, string name, bool allowTag) => Reserve(playerId, name, allowTag);

        /// <summary>The name to show for a player in a race.</summary>
        public string DisplayName(string playerId)
        {
            var r = store.GetRecord(playerId);
            return r == null ? null : Display(r.name, r.tag);
        }

        /// <summary>A player reports another player's name. Three different players asking for the same name forces a rename.</summary>
        public NameResult Report(string reporterId, string targetId, string reason)
        {
            if (string.IsNullOrEmpty(reporterId) || string.IsNullOrEmpty(targetId) || reporterId == targetId || !ReportReasons.IsValid(reason))
                return Fail(NameStatus.Error, NameCode.Error);
            if (store.Bump("report:" + reporterId, Day) > ReportsPerDay) return Fail(NameStatus.RateLimited, NameCode.RateLimited);
            var rec = store.GetRecord(targetId);
            if (rec == null) return Fail(NameStatus.NotFound, NameCode.Error);
            int n = store.AddReport(targetId, reporterId);
            var res = new NameResult { Status = NameStatus.Ok, Name = rec.name, Tag = rec.tag };
            if (n >= ReportsToForceRename && !rec.mustRename)
            {
                // The name is taken away and the player must pick another one; until then they race as Racer#NNNN.
                store.Release(Slot(rec.key, rec.tag), targetId);
                string safe = SafeNames.RacerName(targetId);
                string sk = PlayerNameRules.UniqueKey(safe);
                int tag = 0;
                for (int i = 0; i < NameDiscriminator.Attempts; i++)
                {
                    tag = NameDiscriminator.Candidate(SafeNames.StableHash(targetId), i);
                    if (store.TryClaim(Slot(sk, tag), targetId)) break;
                    tag = 0;
                }
                store.SetRecord(new NameRecord { playerId = targetId, name = safe, tag = tag, key = sk, changedTicks = nowTicks(), mustRename = true });
                res.MustRename = true;
            }
            return res;
        }
    }
}
