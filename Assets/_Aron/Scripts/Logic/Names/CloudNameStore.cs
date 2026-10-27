// (c) Ahmed Alamrani. All rights reserved.
using System;
using System.Collections.Generic;
using System.Text;

namespace Aron.Logic.Names
{
    /// <summary>
    /// A key-value store with optimistic locking, which is what Unity Cloud Save custom data gives a Cloud Code module:
    /// every read returns a lock token, and a write with an old token is refused (HTTP 409). <see cref="Set"/> returns false on that conflict.
    /// An item that does not exist yet has a null token.
    /// </summary>
    public interface IVersionedKv
    {
        bool Get(string key, out string value, out string lockToken);
        bool Set(string key, string value, string lockToken);
    }

    /// <summary>
    /// <see cref="INameStore"/> on top of <see cref="IVersionedKv"/>. Claims are made atomic with read, change, write with the token, and retry on conflict.
    /// Slots live in 256 shard items so one hot item is not shared by every player. The format is plain text (no JSON library needed in Logic).
    /// </summary>
    public class CloudNameStore : INameStore
    {
        public const int Shards = 256, Tries = 6;
        private readonly IVersionedKv kv;
        public CloudNameStore(IVersionedKv kv) { this.kv = kv; }

        // Cloud Save keys allow letters, digits, underscore and hyphen only.
        public static string SafeKey(string s)
        {
            var sb = new StringBuilder();
            foreach (char c in s) sb.Append((c >= 'a' && c <= 'z') || (c >= 'A' && c <= 'Z') || (c >= '0' && c <= '9') || c == '-' || c == '_' ? c : '_');
            return sb.ToString();
        }

        private static string ShardKey(string slot) => "s" + (SafeNames.StableHash(slot) % Shards);

        private static Dictionary<string, string> ParseMap(string v)
        {
            var m = new Dictionary<string, string>();
            if (string.IsNullOrEmpty(v)) return m;
            foreach (var line in v.Split('\n'))
            {
                int t = line.IndexOf('\t');
                if (t > 0) m[line.Substring(0, t)] = line.Substring(t + 1);
            }
            return m;
        }

        private static string WriteMap(Dictionary<string, string> m)
        {
            var sb = new StringBuilder();
            foreach (var kv in m) sb.Append(kv.Key).Append('\t').Append(kv.Value).Append('\n');
            return sb.ToString();
        }

        /// <summary>
        /// Creates the 256 shard items once. A write with no token overwrites whatever is there, so a shard must exist before the first claim
        /// (otherwise two first claims could both "create" it). Safe to run again.
        /// </summary>
        public static int InitShards(IVersionedKv kv)
        {
            int created = 0;
            for (int i = 0; i < Shards; i++)
                if (!kv.Get("s" + i, out _, out _) && kv.Set("s" + i, "#\n", null)) created++;
            return created;
        }

        private string ReadShard(string key, out string token)
        {
            if (!kv.Get(key, out string v, out token))
                throw new InvalidOperationException("Name shards are not set up: run InitNameShards once before the first claim.");
            return v;
        }

        public bool TryClaim(string slot, string playerId)
        {
            string key = ShardKey(slot);
            for (int i = 0; i < Tries; i++)
            {
                string v = ReadShard(key, out string token);
                var map = ParseMap(v);
                if (map.TryGetValue(slot, out var owner)) return owner == playerId;
                map[slot] = playerId;
                if (kv.Set(key, WriteMap(map), token)) return true;
            }
            return false;
        }

        public string Owner(string slot)
        {
            kv.Get(ShardKey(slot), out string v, out _);
            return ParseMap(v).TryGetValue(slot, out var o) ? o : null;
        }

        public void Release(string slot, string playerId)
        {
            string key = ShardKey(slot);
            for (int i = 0; i < Tries; i++)
            {
                string v = ReadShard(key, out string token);
                var map = ParseMap(v);
                if (!map.TryGetValue(slot, out var owner) || owner != playerId) return;
                map.Remove(slot);
                if (kv.Set(key, WriteMap(map), token)) return;
            }
        }

        public NameRecord GetRecord(string playerId)
        {
            if (!kv.Get("p_" + SafeKey(playerId), out string v, out _) || string.IsNullOrEmpty(v)) return null;
            var f = v.Split('\t');
            if (f.Length < 6) return null;
            long.TryParse(f[4], out long ticks); int.TryParse(f[2], out int tag);
            return new NameRecord { playerId = playerId, name = f[1], tag = tag, key = f[3], changedTicks = ticks, mustRename = f[5] == "1" };
        }

        public void SetRecord(NameRecord r)
        {
            string key = "p_" + SafeKey(r.playerId);
            string v = "r\t" + r.name + "\t" + r.tag + "\t" + r.key + "\t" + r.changedTicks + "\t" + (r.mustRename ? "1" : "0");
            for (int i = 0; i < Tries; i++)
            {
                kv.Get(key, out _, out string token);
                if (kv.Set(key, v, token)) return;
            }
            throw new InvalidOperationException("name record write kept conflicting");
        }

        public int Bump(string counter, long windowId)
        {
            string key = "c_" + SafeKey(counter);
            for (int i = 0; i < Tries; i++)
            {
                kv.Get(key, out string v, out string token);
                long w = -1; int n = 0;
                if (!string.IsNullOrEmpty(v)) { var f = v.Split('\t'); if (f.Length == 2) { long.TryParse(f[0], out w); int.TryParse(f[1], out n); } }
                n = w == windowId ? n + 1 : 1;
                if (kv.Set(key, windowId + "\t" + n, token)) return n;
            }
            return int.MaxValue; // fail closed: too much contention counts as "too many"
        }

        public int AddReport(string targetId, string reporterId)
        {
            string key = "r_" + SafeKey(targetId);
            for (int i = 0; i < Tries; i++)
            {
                kv.Get(key, out string v, out string token);
                var set = new List<string>(string.IsNullOrEmpty(v) ? new string[0] : v.Split('\n'));
                set.RemoveAll(string.IsNullOrEmpty);
                if (set.Contains(reporterId)) return -1;
                set.Add(reporterId);
                if (kv.Set(key, string.Join("\n", set), token)) return set.Count;
            }
            return -1;
        }
    }

    /// <summary>A versioned store in memory that behaves like Cloud Save's write locks, for tests.</summary>
    public class InMemoryVersionedKv : IVersionedKv
    {
        private readonly Dictionary<string, string> values = new Dictionary<string, string>();
        private readonly Dictionary<string, int> versions = new Dictionary<string, int>();
        /// <summary>Called between a read and the next write, to simulate another request getting in.</summary>
        public Action<string> BeforeSet;
        public int Conflicts;

        public bool Get(string key, out string value, out string lockToken)
        {
            if (values.TryGetValue(key, out value)) { lockToken = versions[key].ToString(); return true; }
            value = null; lockToken = null; return false;
        }

        public bool Set(string key, string value, string lockToken)
        {
            BeforeSet?.Invoke(key);
            bool exists = values.ContainsKey(key);
            if (lockToken == null ? exists : (!exists || versions[key].ToString() != lockToken)) { Conflicts++; return false; }
            values[key] = value;
            versions[key] = (exists ? versions[key] : 0) + 1;
            return true;
        }
    }
}
