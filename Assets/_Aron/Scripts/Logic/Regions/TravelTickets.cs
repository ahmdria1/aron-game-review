// (c) Ahmed Alamrani. All rights reserved.
using System;
using System.Collections.Generic;

namespace Aron.Logic.Regions
{
    /// <summary>
    /// A travel ticket gives a 24-hour pass to one locked country, starting at its first stage. Its stages still
    /// open one after the other and its boss is still last. When the pass runs out the country locks again until
    /// another ticket or until the ring reaches it the normal way (then it stays open for good). Everything done
    /// during a pass is kept. Tickets come from the gift box, from gems, for coins or as a small purchase, and an
    /// unused ticket never runs out. They can be used once the first two stages of the home country are done.
    /// </summary>
    public static class TravelTickets
    {
        public const int GemsPerTicket = 5;
        public const int CoinPrice = 1200;
        public const int HomeStagesFirst = 2;
        public static readonly TimeSpan PassLength = TimeSpan.FromHours(24);

        /// <summary>The time passes are judged by. The game sets this to its guarded clock; tests set a fixed time.</summary>
        public static Func<DateTime> Clock = () => DateTime.UtcNow;

        public enum Result { Ok, NotYet, NoTicket, AlreadyOpen, Unknown }

        /// <summary>
        /// The clock, never earlier than the latest time the game has seen, so winding the phone's clock back can't
        /// stretch a pass (the same guard as the gift box).
        /// </summary>
        public static DateTime Now(SaveData data)
        {
            var now = Clock();
            if (data != null && data.giftLatestSeen > now.Ticks) return new DateTime(data.giftLatestSeen, DateTimeKind.Utc);
            return now;
        }

        /// <summary>True when the first two stages of the home country are done.</summary>
        public static bool Eligible(RegionCatalog catalog, SaveData data)
        {
            var order = ProgressRing.RegionOrder(catalog, data.homeCountry);
            if (order.Count == 0) return false;
            var home = catalog.regions[order[0]].stages;
            int need = Math.Min(HomeStagesFirst, home.Length);
            for (int i = 0; i < need; i++) if (!StageProgress.IsDone(data, home[i].id)) return false;
            return true;
        }

        /// <summary>First region of a country in the catalog, or -1.</summary>
        public static int FirstRegionOf(RegionCatalog catalog, string country)
        {
            for (int i = 0; i < catalog.regions.Length; i++)
                if (ProgressRing.CountryOf(catalog.regions[i]) == country) return i;
            return -1;
        }

        /// <summary>When the pass for a country ends, or MinValue when there is none.</summary>
        public static DateTime PassEnd(SaveData data, string country)
        {
            if (data.ticketCountries == null) return DateTime.MinValue;
            string key = country + "=";
            foreach (var e in data.ticketCountries)
                if (e.StartsWith(key, StringComparison.Ordinal) && long.TryParse(e.Substring(key.Length), out long t))
                    return new DateTime(t, DateTimeKind.Utc);
            return DateTime.MinValue;
        }

        public static bool PassActive(SaveData data, string country) => Now(data) < PassEnd(data, country);

        /// <summary>Time left on a country's pass, zero when there is none.</summary>
        public static TimeSpan PassLeft(SaveData data, string country)
        {
            var left = PassEnd(data, country) - Now(data);
            return left > TimeSpan.Zero ? left : TimeSpan.Zero;
        }

        /// <summary>
        /// Whether a running pass opens region <paramref name="r"/>: the first region of the country, or a later
        /// region of the same country whose region before it has had its boss beaten.
        /// </summary>
        public static bool OpensRegion(RegionCatalog catalog, SaveData data, int r)
        {
            if (data.ticketCountries == null || data.ticketCountries.Count == 0) return false;
            string c = ProgressRing.CountryOf(catalog.regions[r]);
            if (!PassActive(data, c)) return false;
            int first = FirstRegionOf(catalog, c);
            if (first == r) return true;
            if (r > 0 && ProgressRing.CountryOf(catalog.regions[r - 1]) == c)
            {
                var prev = catalog.regions[r - 1].stages;
                return StageProgress.IsDone(data, prev[prev.Length - 1].id);
            }
            return false;
        }

        public static Result Check(RegionCatalog catalog, SaveData data, string country)
        {
            int r = FirstRegionOf(catalog, country);
            if (r < 0) return Result.Unknown;
            if (StageProgress.IsRegionUnlocked(catalog, data, r)) return Result.AlreadyOpen;
            if (!Eligible(catalog, data)) return Result.NotYet;
            if (data.tickets <= 0) return Result.NoTicket;
            return Result.Ok;
        }

        /// <summary>Spends a ticket on a 24-hour pass for the country.</summary>
        public static Result Use(RegionCatalog catalog, SaveData data, string country)
        {
            var r = Check(catalog, data, country);
            if (r != Result.Ok) return r;
            data.tickets--;
            if (data.ticketCountries == null) data.ticketCountries = new List<string>();
            data.ticketCountries.RemoveAll(e => e.StartsWith(country + "=", StringComparison.Ordinal));
            data.ticketCountries.Add(country + "=" + (Now(data) + PassLength).Ticks);
            return r;
        }

        /// <summary>
        /// After a run in a country whose pass ran out while playing: the run still counts, and the game goes back
        /// to the world map instead of on to the next stage.
        /// </summary>
        public static bool BackToMapAfter(RegionCatalog catalog, SaveData data, string stageId)
        {
            if (catalog.Stage(stageId, out int r, out _) == null) return false;
            return !StageProgress.IsRegionUnlocked(catalog, data, r);
        }

        /// <summary>Adds gems; every <see cref="GemsPerTicket"/> turn into a ticket. Returns tickets made.</summary>
        public static int AddGems(SaveData data, int gems)
        {
            if (gems <= 0) return 0;
            data.gems += gems;
            int made = data.gems / GemsPerTicket;
            data.gems %= GemsPerTicket;
            data.tickets += made;
            return made;
        }

        public static bool BuyWithCoins(SaveData data)
        {
            if (data.coins < CoinPrice) return false;
            data.coins -= CoinPrice;
            data.tickets++;
            return true;
        }
    }
}
