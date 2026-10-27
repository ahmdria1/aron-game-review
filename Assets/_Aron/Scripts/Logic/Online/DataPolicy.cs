// (c) Ahmed Alamrani. All rights reserved.
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Text;
using Aron.Logic.Performance;

namespace Aron.Logic.Online
{
    public enum NetworkKind { None, Wifi, Mobile }

    public enum Traffic { RaceState, Download, Sync, Telemetry, CrashReport }

    /// <summary>
    /// Decides what may use the network right now. Downloads wait for Wi-Fi (mobile data only when the player
    /// allows it) and never run during play; telemetry and crash reports go out on Wi-Fi, or at most once per
    /// session on mobile data; Data saver pushes everything optional to Wi-Fi; nothing runs in the background.
    /// </summary>
    public class DataGate
    {
        private int mobileUploads;

        public bool Allow(Traffic kind, NetworkKind net, PowerSettings s, ScreenMode mode)
        {
            if (mode == ScreenMode.Background || net == NetworkKind.None) return false;
            switch (kind)
            {
                case Traffic.RaceState: return true;
                case Traffic.Download:
                    if (mode == ScreenMode.Gameplay) return false;
                    return net == NetworkKind.Wifi || s.mobileData;
                case Traffic.Sync: return net == NetworkKind.Wifi || !s.dataSaver;
                default:
                    return net == NetworkKind.Wifi || (!s.dataSaver && mobileUploads == 0);
            }
        }

        /// <summary>Call after a telemetry or crash upload so mobile data is used only once per session.</summary>
        public void NoteSent(Traffic kind, NetworkKind net)
        {
            if (net == NetworkKind.Mobile && (kind == Traffic.Telemetry || kind == Traffic.CrashReport)) mobileUploads++;
        }

        public void NewSession() { mobileUploads = 0; }
    }

    /// <summary>Small helper for gzip so payloads stay small.</summary>
    public static class Compact
    {
        public static byte[] Gzip(byte[] data)
        {
            using (var ms = new MemoryStream())
            {
                using (var gz = new GZipStream(ms, CompressionMode.Compress, true)) gz.Write(data, 0, data.Length);
                return ms.ToArray();
            }
        }

        public static byte[] Gzip(string text) => Gzip(Encoding.UTF8.GetBytes(text));

        public static string GunzipText(byte[] data)
        {
            using (var gz = new GZipStream(new MemoryStream(data), CompressionMode.Decompress))
            using (var sr = new StreamReader(gz, Encoding.UTF8)) return sr.ReadToEnd();
        }
    }

    /// <summary>Collects small JSON items and sends them as one gzipped array. The oldest are dropped when full.</summary>
    public class UploadBatcher
    {
        public int MaxItems = 60;
        public int MaxBytes = 24 * 1024;
        private readonly List<string> items = new List<string>();
        private int bytes;

        public int Count => items.Count;
        public int Bytes => bytes;
        public int Dropped { get; private set; }

        public void Add(string json)
        {
            if (string.IsNullOrEmpty(json)) return;
            items.Add(json); bytes += json.Length + 1;
            while ((items.Count > MaxItems || bytes > MaxBytes) && items.Count > 1) { bytes -= items[0].Length + 1; items.RemoveAt(0); Dropped++; }
        }

        public string BuildJsonArray() => "[" + string.Join(",", items) + "]";
        public byte[] BuildGzip() => Compact.Gzip(BuildJsonArray());
        public void Clear() { items.Clear(); bytes = 0; }
    }

    [Serializable]
    public class EtagEntry
    {
        public string key = "", etag = "", body = "";
        public long fetchedAtTicks;
    }

    [Serializable]
    public class EtagStore
    {
        public List<EtagEntry> entries = new List<EtagEntry>();
    }

    /// <summary>
    /// Leaderboard and profile reads send If-None-Match; a "304 Not Modified" costs a few hundred bytes
    /// and the cached body is used.
    /// </summary>
    public class EtagCache
    {
        public readonly EtagStore store;
        public int NotModified { get; private set; }
        public int MaxEntries = 20;

        public EtagCache(EtagStore store = null) { this.store = store ?? new EtagStore(); }

        public EtagEntry Find(string key) { foreach (var e in store.entries) if (e.key == key) return e; return null; }

        /// <summary>Value for the If-None-Match header, or null when there is nothing cached.</summary>
        public string IfNoneMatch(string key) { var e = Find(key); return e != null && e.etag.Length > 0 ? e.etag : null; }

        /// <summary>Returns the body to use: the new one (200), or the cached one (304). Null when there is nothing usable.</summary>
        public string OnResponse(string key, int status, string etag, string body, long nowTicks)
        {
            var e = Find(key);
            if (status == 304) { if (e == null) return null; NotModified++; return e.body; }
            if (status < 200 || status >= 300) return null;
            if (e == null)
            {
                e = new EtagEntry { key = key };
                store.entries.Add(e);
                if (store.entries.Count > MaxEntries) store.entries.RemoveAt(0);
            }
            e.etag = etag ?? ""; e.body = body ?? ""; e.fetchedAtTicks = nowTicks;
            return e.body;
        }
    }

    /// <summary>Per-session byte counter by kind, for the debug overlay and the budget doc.</summary>
    public class DataMeter
    {
        private readonly long[] sent = new long[5], received = new long[5];
        public void Sent(Traffic t, int bytes) { sent[(int)t] += bytes; }
        public void Received(Traffic t, int bytes) { received[(int)t] += bytes; }
        public long SentBytes(Traffic t) => sent[(int)t];
        public long ReceivedBytes(Traffic t) => received[(int)t];
        public long Total { get { long n = 0; for (int i = 0; i < 5; i++) n += sent[i] + received[i]; return n; } }
    }
}
