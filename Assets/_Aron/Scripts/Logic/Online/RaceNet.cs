// (c) Ahmed Alamrani. All rights reserved.
using System;

namespace Aron.Logic.Online
{
    /// <summary>One racer at one moment, in the small whole-number units that go on the wire.</summary>
    public struct RaceState
    {
        /// <summary>Distance along the track in 5 cm steps (keeps counting across laps).</summary>
        public int dist;
        /// <summary>Sideways offset from the racing line in 10 cm steps.</summary>
        public int lat;
        /// <summary>Speed in 0.25 m/s steps.</summary>
        public int speed;
        /// <summary>Bit 0 airborne, 1 boosting, 2 crashed, 3 finished, 4 shielded, 5 using an ability.</summary>
        public int flags;

        public static RaceState From(float distM, float latM, float speedMps, int flags) => new RaceState
        {
            dist = Clamp((int)Math.Round(distM / RaceQuant.DistUnit), 0, 1 << 24),
            lat = Clamp((int)Math.Round(latM / RaceQuant.LatUnit), -2000, 2000),
            speed = Clamp((int)Math.Round(speedMps / RaceQuant.SpeedUnit), 0, 1023),
            flags = flags & 0xFF,
        };

        private static int Clamp(int v, int lo, int hi) => v < lo ? lo : v > hi ? hi : v;
        public bool Same(in RaceState o) => dist == o.dist && lat == o.lat && speed == o.speed && flags == o.flags;
    }

    public static class RaceQuant
    {
        public const float DistUnit = 0.05f, LatUnit = 0.1f, SpeedUnit = 0.25f;
        public static float Dist(int v) => v * DistUnit;
        public static float Lat(int v) => v * LatUnit;
        public static float Speed(int v) => v * SpeedUnit;
    }

    /// <summary>
    /// Compact racer record. A one-byte header carries the racer number (3 bits) and which fields follow (5 bits);
    /// unchanged fields are left out, changed ones are sent as small differences. A key record (every few seconds)
    /// carries full values so a lost packet heals itself.
    /// </summary>
    public static class RaceStateCodec
    {
        private const int Dist = 1, Lat = 2, Speed = 4, Flags = 8, Key = 16;
        public const int MaxPlayers = 8;
        public const int MaxRecordBytes = 1 + 4 + 3 + 2 + 1;

        public static int Write(byte[] buf, int pos, int player, in RaceState prev, in RaceState cur, bool key)
        {
            int mask = key ? (Dist | Lat | Speed | Flags | Key) : 0;
            if (!key)
            {
                if (cur.dist != prev.dist) mask |= Dist;
                if (cur.lat != prev.lat) mask |= Lat;
                if (cur.speed != prev.speed) mask |= Speed;
                if (cur.flags != prev.flags) mask |= Flags;
            }
            buf[pos++] = (byte)((player & 7) << 5 | mask);
            if ((mask & Dist) != 0) pos = VarInt(key ? (uint)cur.dist : Zig(cur.dist - prev.dist), buf, pos);
            if ((mask & Lat) != 0) pos = VarInt(Zig(key ? cur.lat : cur.lat - prev.lat), buf, pos);
            if ((mask & Speed) != 0) pos = VarInt(key ? (uint)cur.speed : Zig(cur.speed - prev.speed), buf, pos);
            if ((mask & Flags) != 0) buf[pos++] = (byte)cur.flags;
            return pos;
        }

        /// <summary>Reads one record and applies it to <paramref name="states"/>[player]. Returns false on a malformed buffer.</summary>
        public static bool Read(byte[] buf, int end, ref int pos, RaceState[] states, out int player)
        {
            player = 0;
            if (pos >= end) return false;
            int h = buf[pos++];
            player = h >> 5;
            int mask = h & 31;
            if (player >= states.Length) return false;
            bool key = (mask & Key) != 0;
            var s = states[player];
            uint v;
            if ((mask & Dist) != 0) { if (!ReadVar(buf, end, ref pos, out v)) return false; s.dist = key ? (int)v : s.dist + Unzig(v); }
            if ((mask & Lat) != 0) { if (!ReadVar(buf, end, ref pos, out v)) return false; s.lat = key ? Unzig(v) : s.lat + Unzig(v); }
            if ((mask & Speed) != 0) { if (!ReadVar(buf, end, ref pos, out v)) return false; s.speed = key ? (int)v : s.speed + Unzig(v); }
            if ((mask & Flags) != 0) { if (pos >= end) return false; s.flags = buf[pos++]; }
            states[player] = s;
            return true;
        }

        private static uint Zig(int v) => (uint)((v << 1) ^ (v >> 31));
        private static int Unzig(uint v) => (int)(v >> 1) ^ -(int)(v & 1);

        private static int VarInt(uint v, byte[] buf, int pos)
        {
            while (v >= 0x80) { buf[pos++] = (byte)(v | 0x80); v >>= 7; }
            buf[pos++] = (byte)v;
            return pos;
        }

        private static bool ReadVar(byte[] buf, int end, ref int pos, out uint v)
        {
            v = 0;
            for (int shift = 0; shift < 35; shift += 7)
            {
                if (pos >= end) return false;
                byte b = buf[pos++];
                v |= (uint)(b & 0x7F) << shift;
                if ((b & 0x80) == 0) return true;
            }
            return false;
        }
    }

    /// <summary>One network packet: a sequence byte, then racer records until the end.</summary>
    public static class RaceSnapshot
    {
        public static int Write(byte[] buf, byte seq, RaceState[] prev, RaceState[] cur, bool[] include, bool key)
        {
            buf[0] = seq;
            int pos = 1;
            for (int i = 0; i < cur.Length; i++)
            {
                if (include != null && !include[i]) continue;
                int before = pos;
                pos = RaceStateCodec.Write(buf, pos, i, prev[i], cur[i], key);
                if (!key && pos == before + 1) pos = before; // nothing changed: leave the racer out
            }
            return pos;
        }

        /// <summary>Applies a packet. <paramref name="gap"/> is true when packets were missed (a key record fixes it).</summary>
        public static bool Read(byte[] buf, int len, RaceState[] states, ref int lastSeq, out bool gap)
        {
            gap = false;
            if (len < 1) return false;
            int seq = buf[0];
            if (lastSeq >= 0) gap = ((seq - lastSeq) & 0xFF) != 1;
            lastSeq = seq;
            int pos = 1;
            while (pos < len) if (!RaceStateCodec.Read(buf, len, ref pos, states, out _)) return false;
            return true;
        }
    }

    /// <summary>
    /// How often race state goes out: 10 times a second, 8 with Data saver on (the game fills the gaps by interpolating).
    /// Every packet carries about 57 bytes of network headers, so a higher rate costs more than it seems: at 12 Hz a
    /// four-racer race is over 5 MB an hour.
    /// </summary>
    public static class RaceNetRate
    {
        public const int KeySeconds = 3;
        /// <summary>Bigger races stay on Wi-Fi: six racers pass the 5 MB an hour budget.</summary>
        public const int MaxPlayersOnMobile = 4;
        public static int Hz(NetworkKind net, bool dataSaver) => dataSaver ? 8 : 10;
    }

    /// <summary>
    /// Counts what a race really costs on the wire by running the real codec on a synthetic race.
    /// The synthetic racers are an estimate; the real figure needs a device on a real network.
    /// </summary>
    public static class BandwidthEstimator
    {
        /// <summary>IPv4 20 + UDP 8 + DTLS record header 13 + authentication tag 16.</summary>
        public const int PacketOverhead = 57;

        public struct Result
        {
            public long upBytes, downBytes, upPackets, downPackets;
            public int seconds;
            public double UpMBPerHour => upBytes * (3600.0 / seconds) / 1e6;
            public double DownMBPerHour => downBytes * (3600.0 / seconds) / 1e6;
            public double TotalMBPerHour => UpMBPerHour + DownMBPerHour;
        }

        public static Result Simulate(int players, int hz, int seconds, int seed = 7)
        {
            var rnd = new Random(seed);
            var prev = new RaceState[players];
            var cur = new RaceState[players];
            var dist = new double[players];
            var speed = new double[players];
            var lat = new double[players];
            for (int i = 0; i < players; i++) { speed[i] = 18 + rnd.NextDouble() * 6; lat[i] = (i - players / 2.0) * 1.5; }

            var buf = new byte[RaceStateCodec.MaxRecordBytes * RaceStateCodec.MaxPlayers + 1];
            var res = new Result { seconds = seconds };
            int ticks = seconds * hz, keyEvery = hz * RaceNetRate.KeySeconds;
            double dt = 1.0 / hz;
            var others = new bool[players];
            for (int i = 1; i < players; i++) others[i] = true;
            var mine = new bool[players]; mine[0] = true;

            for (int t = 0; t < ticks; t++)
            {
                for (int i = 0; i < players; i++)
                {
                    speed[i] = Math.Max(6, Math.Min(36, speed[i] + (rnd.NextDouble() - 0.5) * 2.0));
                    lat[i] = Math.Max(-8, Math.Min(8, lat[i] + (rnd.NextDouble() - 0.5) * 0.6));
                    dist[i] += speed[i] * dt;
                    int flags = rnd.Next(100) < 3 ? 1 : 0;
                    cur[i] = RaceState.From((float)dist[i], (float)lat[i], (float)speed[i], flags | (((t / (hz * 10)) % 3 == 0) ? 2 : 0));
                }
                bool key = t % keyEvery == 0;
                int n = RaceSnapshot.Write(buf, (byte)t, prev, cur, mine, key);
                res.upBytes += n + PacketOverhead; res.upPackets++;
                if (players > 1)
                {
                    n = RaceSnapshot.Write(buf, (byte)t, prev, cur, others, key);
                    res.downBytes += n + PacketOverhead; res.downPackets++;
                }
                Array.Copy(cur, prev, players);
            }
            return res;
        }
    }
}
