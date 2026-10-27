// (c) Ahmed Alamrani. All rights reserved.
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using Aron.Logic.Safety;

namespace Aron.Logic.Diagnostics
{
    /// <summary>
    /// A Sentry project address (https://KEY@HOST/PROJECT). It is read from a config file that is written by the
    /// build from a secret and never committed.
    /// </summary>
    public sealed class SentryDsn
    {
        public string Key { get; private set; }
        public string Host { get; private set; }
        public string ProjectId { get; private set; }
        public string Raw { get; private set; }
        public string EnvelopeUrl => "https://" + Host + "/api/" + ProjectId + "/envelope/";
        public string AuthHeader => "Sentry sentry_version=7, sentry_client=aron/1, sentry_key=" + Key;

        public static bool TryParse(string dsn, out SentryDsn result)
        {
            result = null;
            if (string.IsNullOrWhiteSpace(dsn)) return false;
            if (!Uri.TryCreate(dsn.Trim(), UriKind.Absolute, out var u) || u.Scheme != "https") return false;
            if (string.IsNullOrEmpty(u.UserInfo)) return false;
            string key = u.UserInfo.Split(':')[0];
            string project = u.AbsolutePath.Trim('/');
            if (key.Length == 0 || project.Length == 0 || project.IndexOf('/') >= 0) return false;
            result = new SentryDsn { Key = key, Host = u.Host, ProjectId = project, Raw = dsn.Trim() };
            return true;
        }
    }

    [Serializable]
    public class CrashRecord
    {
        public string fingerprint = "";
        public string level = "error";
        public string message = "";
        public string stack = "";
        public string crumbs = "";
        public string tier = "";
        public string os = "";
        public string release = "";
        public long unixSeconds;
        public int count = 1;
    }

    [Serializable]
    public class CrashStore
    {
        public List<CrashRecord> records = new List<CrashRecord>();
        public int totalRecorded;
    }

    /// <summary>
    /// Keeps a small, scrubbed list of errors on the phone. The same error twice becomes one record with a count,
    /// a session records at most <see cref="MaxPerSession"/> new ones, and the file never holds more than
    /// <see cref="MaxStored"/>. Nothing here touches the network.
    /// </summary>
    public class CrashLog
    {
        public const int MaxPerSession = 20;
        public const int MaxStored = 30;
        public const int MaxMessage = 600, MaxStack = 3000;

        public readonly CrashStore store;
        public Func<IEnumerable<string>> Secrets = () => null;
        private int newThisSession;

        public CrashLog(CrashStore store = null) { this.store = store ?? new CrashStore(); }

        public int Pending => store.records.Count;
        public int TotalRecorded => store.totalRecorded;

        public static string Fingerprint(string message, string stack)
        {
            string firstMessage = (message ?? "").Split('\n')[0];
            var frames = new StringBuilder();
            int n = 0;
            foreach (var line in (stack ?? "").Split('\n'))
            {
                if (line.Trim().Length == 0) continue;
                frames.Append(line.Trim()).Append('|');
                if (++n == 3) break;
            }
            uint h = 2166136261;
            foreach (char c in firstMessage + "#" + frames) { h ^= c; h *= 16777619; }
            return h.ToString("x8");
        }

        public bool Record(string level, string message, string stack, string crumbs, string tier, string os, string release, long unixSeconds)
        {
            string fp = Fingerprint(message, stack);
            foreach (var r in store.records) if (r.fingerprint == fp) { r.count++; return false; }
            if (newThisSession >= MaxPerSession) return false;
            newThisSession++;
            store.totalRecorded++;
            var secrets = Secrets();
            store.records.Add(new CrashRecord
            {
                fingerprint = fp, level = level,
                message = Cut(ErrorReportScrub.Scrub(message, secrets), MaxMessage),
                stack = Cut(ErrorReportScrub.Scrub(stack, secrets), MaxStack),
                crumbs = Cut(ErrorReportScrub.Scrub(crumbs, secrets), 2000),
                tier = tier ?? "", os = os ?? "", release = release ?? "", unixSeconds = unixSeconds,
            });
            while (store.records.Count > MaxStored) store.records.RemoveAt(0);
            return true;
        }

        public void Clear() => store.records.Clear();

        private static string Cut(string s, int max) => s == null ? "" : s.Length <= max ? s : s.Substring(0, max);
    }

    /// <summary>Writes the Sentry envelope format by hand, so no SDK (and no background threads) is needed.</summary>
    public static class SentryEnvelope
    {
        public static string Escape(string s)
        {
            if (string.IsNullOrEmpty(s)) return "";
            var sb = new StringBuilder(s.Length + 8);
            foreach (char c in s)
            {
                switch (c)
                {
                    case '"': sb.Append("\\\""); break;
                    case '\\': sb.Append("\\\\"); break;
                    case '\n': sb.Append("\\n"); break;
                    case '\r': sb.Append("\\r"); break;
                    case '\t': sb.Append("\\t"); break;
                    default: if (c < 0x20) sb.Append("\\u").Append(((int)c).ToString("x4")); else sb.Append(c); break;
                }
            }
            return sb.ToString();
        }

        public static string EventJson(CrashRecord r, string eventId)
        {
            var sb = new StringBuilder(512);
            sb.Append("{\"event_id\":\"").Append(eventId).Append("\",\"timestamp\":").Append(r.unixSeconds.ToString(CultureInfo.InvariantCulture))
              .Append(",\"platform\":\"other\",\"level\":\"").Append(r.level == "fatal" || r.level == "warning" ? r.level : "error")
              .Append("\",\"message\":{\"formatted\":\"").Append(Escape(r.message)).Append("\"}")
              .Append(",\"release\":\"").Append(Escape(r.release)).Append("\",\"environment\":\"release\"")
              .Append(",\"fingerprint\":[\"").Append(r.fingerprint).Append("\"]")
              .Append(",\"tags\":{\"tier\":\"").Append(Escape(r.tier)).Append("\",\"os\":\"").Append(Escape(r.os)).Append("\"}")
              .Append(",\"extra\":{\"count\":").Append(r.count.ToString(CultureInfo.InvariantCulture))
              .Append(",\"stack\":\"").Append(Escape(r.stack)).Append("\",\"recent\":\"").Append(Escape(r.crumbs)).Append("\"}")
              .Append(",\"sdk\":{\"name\":\"aron.mini\",\"version\":\"1\"}}");
            return sb.ToString();
        }

        /// <summary>One envelope holding several events. No user, device name or address is included.</summary>
        public static string Build(SentryDsn dsn, IList<CrashRecord> records, long sentAtUnix)
        {
            var sb = new StringBuilder();
            sb.Append("{\"event_id\":\"").Append(Guid.NewGuid().ToString("N")).Append("\",\"sent_at\":").Append(sentAtUnix.ToString(CultureInfo.InvariantCulture))
              .Append(",\"dsn\":\"").Append(Escape(dsn.Raw)).Append("\"}\n");
            foreach (var r in records)
            {
                string ev = EventJson(r, Guid.NewGuid().ToString("N"));
                sb.Append("{\"type\":\"event\",\"length\":").Append(Encoding.UTF8.GetByteCount(ev).ToString(CultureInfo.InvariantCulture)).Append("}\n").Append(ev).Append('\n');
            }
            return sb.ToString();
        }
    }
}
