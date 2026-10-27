// (c) Ahmed Alamrani. All rights reserved.
using System;
using System.Collections.Generic;
using Aron.Logic.Account;
using Aron.Logic.Regions;

namespace Aron.Logic.Telemetry
{
    public enum Channel { Analytics, Diagnostics }

    /// <summary>Stored answers. Zero means "never asked", and never asked means off.</summary>
    public static class Choice { public const int Unset = 0, On = 1, Off = 2; }

    /// <summary>What a grown-up chose for usage statistics and crash reports, kept in the save.</summary>
    [Serializable]
    public class PrivacyChoices
    {
        public int analytics, diagnostics;
        /// <summary>Privacy notice version each answer was given under. A newer notice asks again; until then the answer does not count.</summary>
        public string analyticsNotice = "", diagnosticsNotice = "";
        public long analyticsAt, diagnosticsAt;
        public bool firstOpenSent;
        /// <summary>Crash records already counted in a crash_count event.</summary>
        public int crashesReported;
    }

    /// <summary>
    /// The only door to collecting anything. Off until a grown-up turns it on (the app puts its grown-up check in front of the switch),
    /// off again the moment it is withdrawn, and off after a privacy notice change until it is answered again.
    /// </summary>
    public static class ConsentGate
    {
        public const string NoticeVersion = ConsentFlow.NoticeVersion;

        public static int ChoiceOf(PrivacyChoices c, Channel ch) => c == null ? Choice.Unset : ch == Channel.Analytics ? c.analytics : c.diagnostics;
        private static string NoticeOf(PrivacyChoices c, Channel ch) => ch == Channel.Analytics ? c.analyticsNotice : c.diagnosticsNotice;

        public static bool Allowed(PrivacyChoices c, Channel ch, string notice = NoticeVersion)
            => c != null && ChoiceOf(c, ch) == Choice.On && NoticeOf(c, ch) == notice;

        /// <summary>True when the question should be shown: never answered, or answered under an older notice.</summary>
        public static bool NeedsAsking(PrivacyChoices c, Channel ch, string notice = NoticeVersion)
            => c == null || ChoiceOf(c, ch) == Choice.Unset || NoticeOf(c, ch) != notice;

        /// <summary>Records an answer. Turning something ON needs the grown-up check to have been passed; turning OFF never does. Returns true when the answer was stored.</summary>
        public static bool Set(PrivacyChoices c, Channel ch, bool on, bool grownUpChecked, long nowUnix, string notice = NoticeVersion)
        {
            if (c == null || (on && !grownUpChecked)) return false;
            int v = on ? Choice.On : Choice.Off;
            if (ch == Channel.Analytics) { c.analytics = v; c.analyticsNotice = notice; c.analyticsAt = nowUnix; }
            else { c.diagnostics = v; c.diagnosticsNotice = notice; c.diagnosticsAt = nowUnix; }
            return true;
        }

        public static void WithdrawAll(PrivacyChoices c, long nowUnix)
        {
            if (c == null) return;
            Set(c, Channel.Analytics, false, false, nowUnix);
            Set(c, Channel.Diagnostics, false, false, nowUnix);
            c.firstOpenSent = false;
        }
    }

    [Serializable]
    public class EventParam
    {
        public string k = "";
        public string s = "";
        public long i;
        public bool isNumber;
    }

    [Serializable]
    public class AnalyticsEvent
    {
        public string name = "";
        public List<EventParam> p = new List<EventParam>();

        public AnalyticsEvent() { }
        public AnalyticsEvent(string name) { this.name = name; }
        public AnalyticsEvent Id(string key, string value) { p.Add(new EventParam { k = key, s = value ?? "" }); return this; }
        public AnalyticsEvent Num(string key, long value) { p.Add(new EventParam { k = key, i = value, isNumber = true }); return this; }
    }

    /// <summary>
    /// The complete list of what may be sent. An event whose name or keys are not listed here, whose ids hold anything but
    /// lowercase letters, digits and _ . -, or whose numbers are out of range, is refused and never leaves the phone.
    /// There is no field for names, emails, text, places, photos or device ids.
    /// </summary>
    public static class EventSchema
    {
        public const string FirstOpen = "first_open", RegionUnlocked = "region_unlocked", StageStart = "stage_start", StageComplete = "stage_complete",
            StageFail = "stage_fail", BossDefeated = "boss_defeated", PurchaseCompleted = "purchase_completed", CrashCount = "crash_count";

        public const int MaxSeconds = 24 * 3600, MaxCount = 100000, MaxIdLength = 64;

        private enum Kind { Id, Number }
        private struct Field { public string key; public Kind kind; public Field(string k, Kind kd) { key = k; kind = kd; } }

        private static readonly Dictionary<string, Field[]> Specs = new Dictionary<string, Field[]>
        {
            { FirstOpen, new Field[0] },
            { RegionUnlocked, new[] { new Field("region", Kind.Id) } },
            { StageStart, new[] { new Field("stage", Kind.Id) } },
            { StageComplete, new[] { new Field("stage", Kind.Id), new Field("seconds", Kind.Number) } },
            { StageFail, new[] { new Field("stage", Kind.Id), new Field("seconds", Kind.Number) } },
            { BossDefeated, new[] { new Field("stage", Kind.Id) } },
            { PurchaseCompleted, new[] { new Field("product", Kind.Id) } },
            { CrashCount, new[] { new Field("count", Kind.Number) } },
        };

        public static IEnumerable<string> Names => Specs.Keys;

        public static bool IsId(string s)
        {
            if (string.IsNullOrEmpty(s) || s.Length > MaxIdLength) return false;
            foreach (char c in s)
            {
                bool ok = (c >= 'a' && c <= 'z') || (c >= '0' && c <= '9') || c == '_' || c == '.' || c == '-';
                if (!ok) return false;
            }
            return true;
        }

        /// <summary>Null when the event is allowed, otherwise the reason it is not.</summary>
        public static string Check(AnalyticsEvent e)
        {
            if (e == null || string.IsNullOrEmpty(e.name)) return "no name";
            if (!Specs.TryGetValue(e.name, out var spec)) return "unknown event";
            var ps = e.p ?? new List<EventParam>();
            if (ps.Count != spec.Length) return "wrong number of parameters";
            foreach (var f in spec)
            {
                EventParam found = null;
                foreach (var x in ps) if (x != null && x.k == f.key) { found = x; break; }
                if (found == null) return "missing " + f.key;
                if (f.kind == Kind.Id)
                {
                    if (found.isNumber || !IsId(found.s)) return "bad id for " + f.key;
                }
                else
                {
                    if (!found.isNumber) return "not a number: " + f.key;
                    long max = f.key == "seconds" ? MaxSeconds : MaxCount;
                    if (found.i < 0 || found.i > max) return "out of range: " + f.key;
                }
            }
            return null;
        }
    }

    /// <summary>Builders for each allowed event. Ids are lowercased; numbers are clamped into range (never negative).</summary>
    public static class Events
    {
        private static string Clean(string id) => (id ?? "").Trim().ToLowerInvariant();
        private static long Secs(float seconds) => seconds < 0f || float.IsNaN(seconds) ? 0 : seconds > EventSchema.MaxSeconds ? EventSchema.MaxSeconds : (long)seconds;

        public static AnalyticsEvent FirstOpen() => new AnalyticsEvent(EventSchema.FirstOpen);
        public static AnalyticsEvent RegionUnlocked(string region) => new AnalyticsEvent(EventSchema.RegionUnlocked).Id("region", Clean(region));
        public static AnalyticsEvent StageStart(string stage) => new AnalyticsEvent(EventSchema.StageStart).Id("stage", Clean(stage));
        public static AnalyticsEvent StageComplete(string stage, float seconds) => new AnalyticsEvent(EventSchema.StageComplete).Id("stage", Clean(stage)).Num("seconds", Secs(seconds));
        public static AnalyticsEvent StageFail(string stage, float seconds) => new AnalyticsEvent(EventSchema.StageFail).Id("stage", Clean(stage)).Num("seconds", Secs(seconds));
        public static AnalyticsEvent BossDefeated(string stage) => new AnalyticsEvent(EventSchema.BossDefeated).Id("stage", Clean(stage));
        /// <summary>The store product id only: no price, no receipt, no order number.</summary>
        public static AnalyticsEvent PurchaseCompleted(string productId) => new AnalyticsEvent(EventSchema.PurchaseCompleted).Id("product", Clean(productId));
        public static AnalyticsEvent CrashCount(int count) => new AnalyticsEvent(EventSchema.CrashCount).Num("count", Math.Max(0, Math.Min(count, EventSchema.MaxCount)));
    }

    [Serializable]
    public class EventStore
    {
        public List<AnalyticsEvent> events = new List<AnalyticsEvent>();
        public List<long> at = new List<long>();
    }

    /// <summary>
    /// Events wait here until a batch is worth sending. Nothing is kept unless the consent check says yes at the moment of recording
    /// (the default check says no). A batch goes out when 20 are waiting or the oldest is 15 minutes old, and only when the data rules
    /// allow it (see DataGate: Wi-Fi, or one mobile upload per session when Data saver is off).
    /// </summary>
    public class EventBuffer
    {
        public const int MaxEvents = 200, BatchSize = 20;
        public const long MaxAgeSeconds = 15 * 60;

        private readonly List<AnalyticsEvent> items = new List<AnalyticsEvent>();
        private readonly List<long> times = new List<long>();

        /// <summary>Asked before every event is kept. Set by the game to the consent gate; defaults to "no".</summary>
        public Func<bool> MayRecord = () => false;

        public int Count => items.Count;
        public int Dropped { get; private set; }
        public int RefusedNoConsent { get; private set; }
        public int RefusedInvalid { get; private set; }

        public bool Record(AnalyticsEvent e, long nowUnix)
        {
            if (!MayRecord()) { RefusedNoConsent++; return false; }
            if (EventSchema.Check(e) != null) { RefusedInvalid++; return false; }
            items.Add(e); times.Add(nowUnix);
            while (items.Count > MaxEvents) { items.RemoveAt(0); times.RemoveAt(0); Dropped++; }
            return true;
        }

        public bool Due(long nowUnix, bool networkAllowed)
            => networkAllowed && items.Count > 0 && (items.Count >= BatchSize || nowUnix - times[0] >= MaxAgeSeconds);

        /// <summary>The next events to send, oldest first. They stay in the buffer until <see cref="Remove"/>.</summary>
        public List<AnalyticsEvent> Peek(int max = BatchSize * 3) => items.GetRange(0, Math.Min(max, items.Count));

        public void Remove(int count)
        {
            count = Math.Min(count, items.Count);
            if (count <= 0) return;
            items.RemoveRange(0, count); times.RemoveRange(0, count);
        }

        public void Clear() { items.Clear(); times.Clear(); }

        public EventStore Snapshot() => new EventStore { events = new List<AnalyticsEvent>(items), at = new List<long>(times) };

        /// <summary>Restores a saved queue. Without consent the saved queue is thrown away; invalid entries are skipped.</summary>
        public void Load(EventStore s)
        {
            Clear();
            if (s == null || s.events == null || !MayRecord()) return;
            for (int i = 0; i < s.events.Count; i++)
            {
                if (EventSchema.Check(s.events[i]) != null) continue;
                items.Add(s.events[i]);
                times.Add(s.at != null && i < s.at.Count ? s.at[i] : 0);
            }
            while (items.Count > MaxEvents) { items.RemoveAt(0); times.RemoveAt(0); Dropped++; }
        }
    }

    public static class CrashCountPolicy
    {
        /// <summary>Crash records not yet counted. Never negative, even if the log was cleared.</summary>
        public static int Pending(int totalRecorded, int alreadyReported) => Math.Max(0, totalRecorded - Math.Max(0, alreadyReported));
    }

    /// <summary>Spots regions that opened for good (the ring, not a temporary travel pass) during a stage result.</summary>
    public static class RegionWatch
    {
        public static HashSet<string> Reached(RegionCatalog catalog, SaveData data)
        {
            var set = new HashSet<string>();
            if (catalog == null || data == null || catalog.regions == null) return set;
            for (int i = 0; i < catalog.regions.Length; i++)
                if (StageProgress.IsRegionReached(catalog, data, i)) set.Add(ProgressRing.CountryOf(catalog.regions[i]));
            return set;
        }

        public static List<string> Opened(HashSet<string> before, HashSet<string> after)
        {
            var list = new List<string>();
            if (after == null) return list;
            foreach (var r in after) if (before == null || !before.Contains(r)) list.Add(r);
            list.Sort(StringComparer.Ordinal);
            return list;
        }
    }
}
