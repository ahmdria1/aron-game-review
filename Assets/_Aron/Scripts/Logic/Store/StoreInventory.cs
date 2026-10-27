// (c) Ahmed Alamrani. All rights reserved.
using System;
using System.Collections.Generic;

namespace Aron.Logic
{
    public enum CoinPurchase { Bought, AlreadyOwned, NotEnoughCoins, NotForCoins, Unknown }

    public enum Grant { Granted, AlreadyOwned, Duplicate, Unknown }

    /// <summary>What the player owns and wears, kept in <see cref="SaveData"/>. Saving to disk is the caller's job.</summary>
    public class StoreInventory
    {
        public const int OrderMemory = 64;

        private readonly StoreCatalog catalog;
        private readonly SaveData data;

        public StoreInventory(StoreCatalog catalog, SaveData data)
        {
            this.catalog = catalog ?? new StoreCatalog();
            this.data = data ?? new SaveData();
            Upgrade(this.data);
        }

        /// <summary>The clock used for rentals; tests swap it.</summary>
        public Func<DateTime> Clock = () => DateTime.UtcNow;

        public StoreCatalog Catalog => catalog;
        public int Coins => data.coins;
        public int RacePoints => data.racePoints;
        public bool CanRent(int points) => data.racePoints >= points;
        public bool AdsRemoved => data.adsRemoved;

        /// <summary>Fills in fields an old save doesn't have. Dates collected before the store existed become coins.</summary>
        public static void Upgrade(SaveData d)
        {
            d.sig = Aron.Logic.Signature.StudioMark.Value;   // header field, keeps older saves stamped too
            if (d.clock == null) d.clock = new Aron.Logic.Integrity.ClockMemory();
            if (d.ownedItems == null) d.ownedItems = new List<string>();
            if (d.equippedItems == null) d.equippedItems = new List<string>();
            if (d.handledOrders == null) d.handledOrders = new List<string>();
            if (d.version < 2)
            {
                d.coins += d.totalDates;
                d.version = 2;
            }
            if (d.ticketCountries == null) d.ticketCountries = new List<string>();
            if (d.raceTracks == null) d.raceTracks = new List<string>();
            if (d.raceBest == null) d.raceBest = new List<string>();
            if (d.carriedFoods == null) d.carriedFoods = new List<string>();
            if (d.companionLove == null) d.companionLove = new List<string>();
            if (d.raceMedals == null) d.raceMedals = new List<string>();
            if (d.secretsFound == null) d.secretsFound = new List<string>();
            if (d.helpers == null) d.helpers = new List<string>();
            if (d.costumePieces == null) d.costumePieces = new List<string>();
            if (d.foodsCollected == null) d.foodsCollected = new List<string>();
            if (d.achievements == null) d.achievements = new List<string>();
            if (d.pointLog == null) d.pointLog = new List<string>();
            if (d.rentals == null) d.rentals = new List<string>();
            if (d.version < 3)
            {
                // Everyone who played before countries had a ring started in Saudi Arabia.
                if (string.IsNullOrEmpty(d.homeCountry)) d.homeCountry = Regions.ProgressRing.DefaultHome;
                if (string.IsNullOrEmpty(d.difficulty)) d.difficulty = "normal";
                d.version = 3;
            }
            if (d.version < 4)
            {
                // Race mounts: the Arabian horse is the free default, so nothing needs granting. An old save
                // that rode a mount keeps the same one; points start at zero.
                if (d.racePoints < 0) d.racePoints = 0;
                if (d.racePointsLifetime < d.racePoints) d.racePointsLifetime = d.racePoints;
                if (string.IsNullOrEmpty(d.aiDifficulty)) d.aiDifficulty = "normal";
                d.version = 4;
            }
        }

        public void AddCoins(int amount)
        {
            if (amount > 0) data.coins += amount;
        }

        public bool Owns(string id)
        {
            var item = catalog.Find(id);
            if (item == null) return false;
            if (item.isDefault) return true;
            if (item.removesAds && data.adsRemoved) return true;
            return data.ownedItems.Contains(id) || IsRented(id);
        }

        /// <summary>True only for what was bought or earned (a rental doesn't count).</summary>
        public bool OwnsForGood(string id)
        {
            var item = catalog.Find(id);
            return item != null && (item.isDefault || data.ownedItems.Contains(id));
        }

        private DateTime Now()
        {
            var now = Clock();
            // A clock set back never shortens how far time has gone.
            if (now.Ticks < data.rentLatestSeen) now = new DateTime(data.rentLatestSeen, DateTimeKind.Utc);
            else data.rentLatestSeen = now.Ticks;
            return now;
        }

        public DateTime? RentalEnds(string id)
        {
            string prefix = id + "=";
            foreach (var e in data.rentals)
                if (e.StartsWith(prefix, StringComparison.Ordinal) && long.TryParse(e.Substring(prefix.Length), out long t)) return new DateTime(t, DateTimeKind.Utc);
            return null;
        }

        public bool IsRented(string id)
        {
            var end = RentalEnds(id);
            return end.HasValue && end.Value > Now();
        }

        public enum RentResult { Rented, AlreadyOwned, NotEnoughPoints, NotRentable, Unknown }

        /// <summary>
        /// Rents a look for <see cref="StoreCatalog.rentHours"/> hours for <see cref="StoreCatalog.rentPoints"/> race points.
        /// Renting again while it runs adds the time on. Anything already bought or earned can't be rented.
        /// </summary>
        public RentResult Rent(string id)
        {
            var item = catalog.Find(id);
            if (item == null) return RentResult.Unknown;
            if (!item.CanRent) return RentResult.NotRentable;
            if (OwnsForGood(id)) return RentResult.AlreadyOwned;
            if (!Races.RacePoints.Spend(data, catalog.rentPoints)) return RentResult.NotEnoughPoints;
            var now = Now();
            var end = RentalEnds(id);
            var from = end.HasValue && end.Value > now ? end.Value : now;
            data.rentals.RemoveAll(e => e.StartsWith(id + "=", StringComparison.Ordinal));
            data.rentals.Add(id + "=" + from.AddHours(catalog.rentHours).Ticks);
            return RentResult.Rented;
        }

        /// <summary>Drops rentals that ran out. Whatever was equipped from them falls back to the default by itself.</summary>
        public void ClearExpiredRentals()
        {
            var now = Now();
            data.rentals.RemoveAll(e =>
            {
                int eq = e.IndexOf('=');
                return eq < 0 || !long.TryParse(e.Substring(eq + 1), out long t) || t <= now.Ticks;
            });
        }

        /// <summary>Equipped item for a slot, falling back to the slot's default.</summary>
        public StoreItem Equipped(string slot)
        {
            string prefix = slot + "=";
            foreach (var e in data.equippedItems)
            {
                if (!e.StartsWith(prefix)) continue;
                var item = catalog.Find(e.Substring(prefix.Length));
                if (item != null && item.slot == slot && Owns(item.id)) return item;
            }
            return catalog.DefaultFor(slot);
        }

        public bool IsEquipped(string id)
        {
            var item = catalog.Find(id);
            return item != null && item.HasSlot && Equipped(item.slot) == item;
        }

        public bool Equip(string id)
        {
            var item = catalog.Find(id);
            if (item == null || !item.HasSlot || !Owns(id)) return false;
            string prefix = item.slot + "=";
            data.equippedItems.RemoveAll(e => e.StartsWith(prefix));
            if (!item.isDefault) data.equippedItems.Add(prefix + id);
            return true;
        }

        public bool CanAfford(StoreItem item) => item != null && item.SoldForCoins && data.coins >= item.coinPrice;

        public CoinPurchase BuyWithCoins(string id)
        {
            var item = catalog.Find(id);
            if (item == null) return CoinPurchase.Unknown;
            if ((item.IsHelper || item.tickets > 0) && item.SoldForCoins)
            {
                if (data.coins < item.coinPrice) return CoinPurchase.NotEnoughCoins;
                data.coins -= item.coinPrice;
                if (item.IsHelper) Helpers.Add(data, item.helper, Math.Max(1, item.helperCount));
                data.tickets += item.tickets;
                return CoinPurchase.Bought;
            }
            if (!item.SoldForCoins || item.IsConsumable) return CoinPurchase.NotForCoins;
            if (OwnsForGood(id)) return CoinPurchase.AlreadyOwned; // a running rental doesn't block buying it for good
            if (data.coins < item.coinPrice) return CoinPurchase.NotEnoughCoins;
            data.coins -= item.coinPrice;
            Unlock(item);
            if (item.HasSlot) Equip(id);
            return CoinPurchase.Bought;
        }

        /// <summary>
        /// Hands over what a store purchase paid for. <paramref name="orderId"/> guards against the same order
        /// arriving twice (the app closed before it was confirmed, a restore). Non-consumables are safe to grant again.
        /// </summary>
        public Grant GrantPurchase(string productId, string orderId)
        {
            var item = catalog.FindByProductId(productId);
            if (item == null) return Grant.Unknown;
            if (!string.IsNullOrEmpty(orderId))
            {
                if (data.handledOrders.Contains(orderId)) return Grant.Duplicate;
                data.handledOrders.Add(orderId);
                while (data.handledOrders.Count > OrderMemory) data.handledOrders.RemoveAt(0);
            }
            if (item.IsConsumable)
            {
                data.coins += item.coins;
                if (item.IsHelper) Helpers.Add(data, item.helper, Math.Max(1, item.helperCount));
                data.tickets += item.tickets;
                return Grant.Granted;
            }
            if (OwnsForGood(item.id) && AllGrantsOwned(item)) return Grant.AlreadyOwned; // paying while renting still buys it
            bool firstTime = !OwnsForGood(item.id);
            Unlock(item);
            if (firstTime) data.coins += item.coins;
            if (firstTime && item.HasSlot) Equip(item.id);
            return Grant.Granted;
        }

        private void Unlock(StoreItem item)
        {
            if (!data.ownedItems.Contains(item.id)) data.ownedItems.Add(item.id);
            data.rentals.RemoveAll(r => r.StartsWith(item.id + "=", StringComparison.Ordinal)); // bought for good: the rental no longer matters
            if (item.removesAds) data.adsRemoved = true;
            if (item.grants == null) return;
            foreach (var g in item.grants)
            {
                var granted = catalog.Find(g);
                if (granted == null || granted.IsConsumable) continue;
                if (!data.ownedItems.Contains(g)) data.ownedItems.Add(g);
                if (granted.removesAds) data.adsRemoved = true;
            }
        }

        private bool AllGrantsOwned(StoreItem item)
        {
            if (item.grants == null) return true;
            foreach (var g in item.grants) if (!OwnsForGood(g)) return false;
            return true;
        }
    }
}
