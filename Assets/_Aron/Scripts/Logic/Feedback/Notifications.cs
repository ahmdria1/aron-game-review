// (c) Ahmed Alamrani. All rights reserved.
using System;
using System.Collections.Generic;

namespace Aron.Logic.Feedback
{
    /// <summary>Kinds of local notification, each with its own toggle in settings.</summary>
    public static class NotifyKind
    {
        public const string Gift = "gift", Streak = "streak", Daily = "daily", TicketSoon = "ticket_soon", TicketOver = "ticket_over",
            Season = "season", Ghost = "ghost";
        public static readonly string[] All = { Gift, Streak, Daily, TicketSoon, TicketOver, Season, Ghost };

        /// <summary>The screen a notification opens.</summary>
        public static string DeepLink(string kind)
        {
            switch (kind)
            {
                case Gift: return "aron://gift";
                case Streak: case Daily: return "aron://daily";
                case TicketSoon: case TicketOver: return "aron://map";
                case Season: return "aron://menu";
                case Ghost: return "aron://races";
                default: return "aron://menu";
            }
        }
    }

    public class PlannedNotice
    {
        public string kind;
        /// <summary>Local time it fires.</summary>
        public DateTime at;
        public string titleKey, bodyKey;
        /// <summary>Country or track the text names, filled into {0}.</summary>
        public string arg = "";
        public string Link => NotifyKind.DeepLink(kind);
    }

    /// <summary>The player's notification choices, saved with the game.</summary>
    [Serializable]
    public class NotifySettings
    {
        /// <summary>0 not asked yet, 1 allowed, 2 refused. Asked once, after the first gift box, never at launch.</summary>
        public int asked;
        public int quietStartHour = 22, quietEndHour = 8;
        public List<string> off = new List<string>();
        /// <summary>Under the age gate notifications stay off unless a parent turns them on.</summary>
        public bool child;
        public bool parentAllowed;

        public bool KindOn(string kind) => off == null || !off.Contains(kind);
        public bool Allowed => asked == 1 && (!child || parentAllowed);
    }

    /// <summary>
    /// Plans the local notifications for the next days from the save: never more than two a day, nothing in
    /// quiet hours (moved to the morning), only kinds the player kept on, nothing at all without permission.
    /// No marketing: every notice is about the player's own gift, streak, challenge, pass, a festival or a ghost.
    /// </summary>
    public static class NotificationPlanner
    {
        public const int MaxPerDay = 2;
        public const int StreakHour = 19;
        public static readonly TimeSpan TicketWarning = TimeSpan.FromHours(1);

        public static bool Quiet(NotifySettings s, DateTime local)
        {
            int h = local.Hour;
            return s.quietStartHour > s.quietEndHour ? (h >= s.quietStartHour || h < s.quietEndHour) : (h >= s.quietStartHour && h < s.quietEndHour);
        }

        /// <summary>Moves a time out of quiet hours to when they end.</summary>
        public static DateTime OutOfQuiet(NotifySettings s, DateTime local)
        {
            if (!Quiet(s, local)) return local;
            var end = local.Date.AddHours(s.quietEndHour);
            if (end <= local) end = end.AddDays(1);
            return end;
        }

        /// <param name="nowLocal">Local clock now.</param>
        /// <param name="toLocal">Turns a UTC time into local time (the device zone).</param>
        public static List<PlannedNotice> Plan(SaveData d, NotifySettings s, DateTime nowLocal, Func<DateTime, DateTime> toLocal,
            DateTime? giftReadyUtc, bool playedToday, IEnumerable<KeyValuePair<DateTime, string>> seasonStartsLocal)
        {
            var list = new List<PlannedNotice>();
            if (!s.Allowed) return list;
            void Add(string kind, DateTime at, string title, string body, string arg = "")
            {
                if (!s.KindOn(kind) || at <= nowLocal) return;
                list.Add(new PlannedNotice { kind = kind, at = OutOfQuiet(s, at), titleKey = title, bodyKey = body, arg = arg });
            }

            if (giftReadyUtc.HasValue) Add(NotifyKind.Gift, toLocal(giftReadyUtc.Value), "notify_gift_title", "notify_gift_body");
            if (!playedToday && d.dailyStreak > 0) Add(NotifyKind.Streak, nowLocal.Date.AddHours(StreakHour), "notify_gift_title", "notify_streak_body");
            Add(NotifyKind.Daily, nowLocal.Date.AddDays(1).AddHours(s.quietEndHour + 1), "notify_gift_title", "notify_daily_body");

            foreach (var e in d.ticketCountries ?? new List<string>())
            {
                int eq = e.IndexOf('=');
                if (eq <= 0 || !long.TryParse(e.Substring(eq + 1), out long ticks)) continue;
                var endLocal = toLocal(new DateTime(ticks, DateTimeKind.Utc));
                string country = e.Substring(0, eq);
                Add(NotifyKind.TicketSoon, endLocal - TicketWarning, "notify_gift_title", "notify_ticket_soon", country);
                Add(NotifyKind.TicketOver, endLocal, "notify_gift_title", "notify_ticket_over", country);
            }

            if (seasonStartsLocal != null)
                foreach (var kv in seasonStartsLocal) Add(NotifyKind.Season, kv.Key.Date.AddHours(10), "notify_gift_title", "notify_season_body", kv.Value);

            return Cap(list);
        }

        /// <summary>Keeps at most two a day, the earliest (ticket warnings and gifts first on ties).</summary>
        public static List<PlannedNotice> Cap(List<PlannedNotice> list)
        {
            list.Sort((a, b) => a.at != b.at ? a.at.CompareTo(b.at) : Rank(a.kind).CompareTo(Rank(b.kind)));
            var perDay = new Dictionary<DateTime, int>();
            var kept = new List<PlannedNotice>();
            foreach (var n in list)
            {
                perDay.TryGetValue(n.at.Date, out int c);
                if (c >= MaxPerDay) continue;
                perDay[n.at.Date] = c + 1;
                kept.Add(n);
            }
            return kept;
        }

        private static int Rank(string kind) => kind == NotifyKind.TicketSoon || kind == NotifyKind.TicketOver ? 0 : kind == NotifyKind.Gift ? 1 : 2;

        /// <summary>Ticket notices for a country are dropped when the pass is renewed or the ring reaches the country for good.</summary>
        public static List<PlannedNotice> DropTicket(List<PlannedNotice> plan, string country)
        {
            plan.RemoveAll(n => (n.kind == NotifyKind.TicketSoon || n.kind == NotifyKind.TicketOver) && n.arg == country);
            return plan;
        }
    }

    /// <summary>The platform side (Unity Mobile Notifications on Android and iOS).</summary>
    public interface INotificationService
    {
        void RequestPermission(Action<bool> done);
        void CancelAll();
        void Schedule(PlannedNotice n, string title, string body);
    }
}
