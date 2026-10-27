// (c) Ahmed Alamrani. All rights reserved.
using System;
using System.Collections.Generic;
using System.Globalization;

namespace Aron.Logic.Seasons
{
    /// <summary>Server-side say over the seasons, carried in the remote config file.</summary>
    [Serializable]
    public class SeasonOverride
    {
        /// <summary>Days to move the Hijri calendar by when the moon sighting differs from Umm al-Qura (-2..2).</summary>
        public int hijriOffsetDays;
        /// <summary>Season ids to switch on whatever the date ("ramadan", "eid_fitr", "eid_adha", "national").</summary>
        public string[] force = Array.Empty<string>();
        /// <summary>Season ids to switch off.</summary>
        public string[] disable = Array.Empty<string>();
    }

    /// <summary>A national day: month and day in the Gregorian calendar.</summary>
    public struct NationalDay
    {
        public int Month, Day;
        public NationalDay(int month, int day) { Month = month; Day = day; }
    }

    /// <summary>
    /// Which festive dressing is up on a given day. Ramadan and the two Eids follow the Umm al-Qura calendar used in
    /// Saudi Arabia, so other countries may be a day apart; the server offset can correct that. National days are
    /// fixed Gregorian dates, sources in docs/culture_sources.md.
    /// </summary>
    public static class SeasonCalendar
    {
        public const string Ramadan = "ramadan", EidFitr = "eid_fitr", EidAdha = "eid_adha", National = "national";

        private static readonly UmAlQuraCalendar Hijri = new UmAlQuraCalendar();

        /// <summary>
        /// The main national day each country celebrates. The UK has no single official national day, so it uses
        /// none; see the sources file.
        /// </summary>
        public static readonly Dictionary<string, NationalDay> NationalDays = new Dictionary<string, NationalDay>
        {
            {"saudi", new NationalDay(9, 23)}, {"kuwait", new NationalDay(2, 25)}, {"uae", new NationalDay(12, 2)},
            {"bahrain", new NationalDay(12, 16)}, {"qatar", new NationalDay(12, 18)}, {"oman", new NationalDay(11, 20)},
            {"yemen", new NationalDay(5, 22)}, {"iraq", new NationalDay(10, 3)}, {"syria", new NationalDay(4, 17)},
            {"lebanon", new NationalDay(11, 22)}, {"jordan", new NationalDay(5, 25)},
            {"egypt", new NationalDay(7, 23)}, {"sudan", new NationalDay(1, 1)}, {"libya", new NationalDay(12, 24)},
            {"tunisia", new NationalDay(3, 20)}, {"algeria", new NationalDay(7, 5)}, {"morocco", new NationalDay(7, 30)},
            {"india", new NationalDay(8, 15)}, {"pakistan", new NationalDay(8, 14)}, {"turkey", new NationalDay(10, 29)},
            {"usa", new NationalDay(7, 4)}, {"germany", new NationalDay(10, 3)}, {"mexico", new NationalDay(9, 16)},
            {"brazil", new NationalDay(9, 7)}, {"portugal", new NationalDay(6, 10)}, {"indonesia", new NationalDay(8, 17)},
            {"malaysia", new NationalDay(8, 31)}, {"south_africa", new NationalDay(4, 27)},
        };

        /// <summary>Hijri month and day for a Gregorian date, after the server offset.</summary>
        public static void HijriDate(DateTime date, int offsetDays, out int month, out int day)
        {
            var d = date.Date.AddDays(offsetDays);
            month = Hijri.GetMonth(d);
            day = Hijri.GetDayOfMonth(d);
        }

        /// <summary>First Gregorian day of a Hijri month in the given Hijri year.</summary>
        public static DateTime HijriToGregorian(int hijriYear, int month, int day) => Hijri.ToDateTime(hijriYear, month, day, 0, 0, 0, 0);

        public static bool IsNationalDay(DateTime date, string country)
            => country != null && NationalDays.TryGetValue(country, out var n) && date.Month == n.Month && date.Day == n.Day;

        /// <summary>Season ids active on <paramref name="date"/> (local date) for a stage in <paramref name="country"/>.</summary>
        public static List<string> Active(DateTime date, string country, SeasonOverride o = null)
        {
            var list = new List<string>();
            int offset = o == null ? 0 : Math.Max(-2, Math.Min(2, o.hijriOffsetDays));
            HijriDate(date, offset, out int m, out int d);
            if (m == 9) list.Add(Ramadan);
            if (m == 10 && d <= 3) list.Add(EidFitr);
            if (m == 12 && d >= 10 && d <= 13) list.Add(EidAdha);
            if (IsNationalDay(date, country)) list.Add(National);
            if (o != null)
            {
                if (o.force != null) foreach (var f in o.force) if (!string.IsNullOrEmpty(f) && !list.Contains(f)) list.Add(f);
                if (o.disable != null) foreach (var x in o.disable) list.Remove(x);
            }
            return list;
        }
    }
}
