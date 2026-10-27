// (c) Ahmed Alamrani. All rights reserved.
using System;
using System.Collections.Generic;
using System.IO;

namespace Aron.Logic.Online
{
    /// <summary>One sample of a racer: where it is, which way it faces and what it is doing.</summary>
    public struct GhostFrame
    {
        public float x, y, z;
        /// <summary>Heading in degrees, 0..360.</summary>
        public float yaw;
        /// <summary>Animation state id (run, jump, drift, boost...).</summary>
        public byte anim;
    }

    /// <summary>
    /// A recorded run at 10 Hz, stored small: the first frame in full, then deltas in centimetres and
    /// tenth-degrees as variable-length integers. A 3-minute race is a few kilobytes, small enough for
    /// Cloud Save or leaderboard metadata, and the same bytes work offline as the local ghost.
    /// </summary>
    public class Ghost
    {
        public const int Hz = 10;
        public const byte Version = 1;
        public const int MaxFrames = Hz * 60 * 8;

        public string track = "";
        public string hero = "aron";
        public string costume = "";
        public float time;
        public readonly List<GhostFrame> frames = new List<GhostFrame>();

        public void Add(GhostFrame f) { if (frames.Count < MaxFrames) frames.Add(f); }

        /// <summary>The ghost at a moment of its run, blended between the two nearest samples.</summary>
        public GhostFrame At(float seconds)
        {
            if (frames.Count == 0) return default;
            float f = Math.Max(0f, seconds * Hz);
            int i = Math.Min(frames.Count - 1, (int)f);
            int j = Math.Min(frames.Count - 1, i + 1);
            float k = Math.Min(1f, f - i);
            var a = frames[i]; var b = frames[j];
            float dy = ((b.yaw - a.yaw + 540f) % 360f) - 180f;
            return new GhostFrame
            {
                x = a.x + (b.x - a.x) * k, y = a.y + (b.y - a.y) * k, z = a.z + (b.z - a.z) * k,
                yaw = (a.yaw + dy * k + 360f) % 360f, anim = k < 0.5f ? a.anim : b.anim,
            };
        }

        public byte[] Encode()
        {
            using (var ms = new MemoryStream())
            using (var w = new BinaryWriter(ms))
            {
                w.Write(Version);
                w.Write(track ?? ""); w.Write(hero ?? ""); w.Write(costume ?? "");
                w.Write(time);
                VarUInt(w, (uint)frames.Count);
                int px = 0, py = 0, pz = 0, pyaw = 0; byte panim = 0;
                foreach (var f in frames)
                {
                    int x = Cm(f.x), y = Cm(f.y), z = Cm(f.z), yaw = (int)Math.Round(((f.yaw % 360f) + 360f) % 360f * 10f) % 3600;
                    VarInt(w, x - px); VarInt(w, y - py); VarInt(w, z - pz);
                    int dyaw = ((yaw - pyaw) % 3600 + 5400) % 3600 - 1800;
                    VarInt(w, dyaw);
                    if (f.anim == panim) w.Write((byte)0); else { w.Write((byte)1); w.Write(f.anim); }
                    px = x; py = y; pz = z; pyaw = yaw; panim = f.anim;
                }
                w.Flush();
                return ms.ToArray();
            }
        }

        public static Ghost Decode(byte[] data)
        {
            if (data == null || data.Length < 2) return null;
            try
            {
                using (var r = new BinaryReader(new MemoryStream(data)))
                {
                    if (r.ReadByte() != Version) return null;
                    var g = new Ghost { track = r.ReadString(), hero = r.ReadString(), costume = r.ReadString(), time = r.ReadSingle() };
                    uint n = ReadVarUInt(r);
                    if (n > MaxFrames) return null;
                    int x = 0, y = 0, z = 0, yaw = 0; byte anim = 0;
                    for (uint i = 0; i < n; i++)
                    {
                        x += ReadVarInt(r); y += ReadVarInt(r); z += ReadVarInt(r);
                        yaw = ((yaw + ReadVarInt(r)) % 3600 + 3600) % 3600;
                        if (r.ReadByte() == 1) anim = r.ReadByte();
                        g.frames.Add(new GhostFrame { x = x / 100f, y = y / 100f, z = z / 100f, yaw = yaw / 10f, anim = anim });
                    }
                    return g;
                }
            }
            catch (EndOfStreamException) { return null; }
            catch (IOException) { return null; }
        }

        /// <summary>A share code a friend can paste: URL-safe base64 of the encoded ghost.</summary>
        public string ShareCode() => Convert.ToBase64String(Encode()).Replace('+', '-').Replace('/', '_').TrimEnd('=');

        public static Ghost FromShareCode(string code)
        {
            if (string.IsNullOrEmpty(code)) return null;
            string s = code.Trim().Replace('-', '+').Replace('_', '/');
            while (s.Length % 4 != 0) s += "=";
            try { return Decode(Convert.FromBase64String(s)); }
            catch (FormatException) { return null; }
        }

        private static int Cm(float m) => (int)Math.Round(m * 100f);

        private static void VarUInt(BinaryWriter w, uint v)
        {
            while (v >= 0x80) { w.Write((byte)(v | 0x80)); v >>= 7; }
            w.Write((byte)v);
        }

        private static void VarInt(BinaryWriter w, int v) => VarUInt(w, (uint)((v << 1) ^ (v >> 31)));

        private static uint ReadVarUInt(BinaryReader r)
        {
            uint v = 0; int shift = 0;
            while (true)
            {
                byte b = r.ReadByte();
                v |= (uint)(b & 0x7f) << shift;
                if ((b & 0x80) == 0) return v;
                shift += 7;
                if (shift > 28) throw new IOException("varint too long");
            }
        }

        private static int ReadVarInt(BinaryReader r)
        {
            uint u = ReadVarUInt(r);
            return (int)(u >> 1) ^ -(int)(u & 1);
        }
    }

    /// <summary>Which ghost to race: the player's best, a friend's share code, or the top of the leaderboard.</summary>
    public enum GhostSource { OwnBest, Friend, LeaderboardTop }

    /// <summary>Where ghosts are kept: the cloud when signed in, the phone otherwise. The game only sees this.</summary>
    public interface IGhostStore
    {
        bool Online { get; }
        void Save(string key, byte[] data);
        byte[] Load(string key);
    }

    /// <summary>Uses the cloud store when it is online and has the ghost, else the local copy. Saves go to both.</summary>
    public class GhostLibrary
    {
        private readonly IGhostStore cloud, local;
        public GhostLibrary(IGhostStore cloud, IGhostStore local) { this.cloud = cloud; this.local = local; }

        public static string Key(string track) => "ghost_" + track;

        public void SaveBest(Ghost g)
        {
            var bytes = g.Encode();
            local?.Save(Key(g.track), bytes);
            if (cloud != null && cloud.Online) cloud.Save(Key(g.track), bytes);
        }

        public Ghost Best(string track)
        {
            byte[] b = null;
            if (cloud != null && cloud.Online) b = cloud.Load(Key(track));
            if (b == null && local != null) b = local.Load(Key(track));
            return Ghost.Decode(b);
        }

        /// <summary>Keeps the faster of the stored ghost and a new run. True when the new run replaced it.</summary>
        public bool Offer(Ghost run)
        {
            var best = Best(run.track);
            if (best != null && best.time > 0f && best.time <= run.time) return false;
            SaveBest(run);
            return true;
        }
    }

    /// <summary>A store kept in memory: the offline fallback in tests, and the base of the file store at run time.</summary>
    public class MemoryGhostStore : IGhostStore
    {
        private readonly Dictionary<string, byte[]> data = new Dictionary<string, byte[]>();
        public bool Online { get; set; } = true;
        public void Save(string key, byte[] bytes) => data[key] = bytes;
        public byte[] Load(string key) => data.TryGetValue(key, out var b) ? b : null;
    }
}
