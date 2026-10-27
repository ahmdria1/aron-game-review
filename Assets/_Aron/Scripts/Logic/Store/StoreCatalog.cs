// (c) Ahmed Alamrani. All rights reserved.
using System;
using System.Collections.Generic;

namespace Aron.Logic
{
    public static class StoreTabs
    {
        public const string Aron = "aron";
        public const string Noura = "noura";
        public const string Companions = "companions";
        public const string Mounts = "mounts";
        public const string Sea = "sea";
        public const string Cars = "cars";
        public const string Bicycles = "bicycles";
        public const string Motorcycles = "motorcycles";
        public const string Coins = "coins";
        public const string Perks = "perks";
        public const string Developer = "developer";

        public static readonly string[] Order = { Aron, Noura, Companions, Mounts, Sea, Cars, Bicycles, Motorcycles, Coins, Perks, Developer };

        /// <summary>The five race-vehicle shop tabs (the animal tab is Mounts).</summary>
        public static readonly string[] VehicleTabs = { Mounts, Sea, Cars, Bicycles, Motorcycles };

        /// <summary>Slots of the race vehicles; one is equipped per slot (the free default is the fallback).</summary>
        public static readonly string[] VehicleSlots = { "mount", "boat", "car", "bicycle", "motorcycle" };
    }

    public static class ProductKinds
    {
        public const string Consumable = "consumable";
        public const string NonConsumable = "non_consumable";
    }

    /// <summary>Something sold inside the game: a look, a coin pack or a perk.</summary>
    [Serializable]
    public class StoreItem
    {
        public string id;
        public string tab;
        /// <summary>What it replaces when equipped (shemagh, thobe, agal, shoes, abaya_trim, handbag, camel). Empty for packs and perks.</summary>
        public string slot;
        public string nameAr;
        public string descAr;
        /// <summary>Price in game coins. 0 means it can't be bought with coins.</summary>
        public int coinPrice;
        /// <summary>Store product id on Google Play and the App Store. Empty means coins only.</summary>
        public string productId;
        public string productType;
        /// <summary>Coins given when bought (coin packs, bundles).</summary>
        public int coins;
        /// <summary>Other item ids unlocked by this one (bundles).</summary>
        public string[] grants;
        public bool removesAds;
        public bool isDefault;
        public string color;
        public string accent;
        public string effect;
        /// <summary>One-stage helper this item hands out (see Helpers), and how many.</summary>
        public string helper = "";
        public int helperCount;
        /// <summary>Travel tickets this item hands out.</summary>
        public int tickets;
        /// <summary>Informational direct price tier in US dollars for the store consoles (0 = not for money).</summary>
        public float usd;

        public bool HasSlot => !string.IsNullOrEmpty(slot);
        public bool SoldForMoney => !string.IsNullOrEmpty(productId);
        public bool SoldForCoins => coinPrice > 0;
        public bool IsConsumable => productType == ProductKinds.Consumable;
        /// <summary>A race vehicle (an animal mount, boat, car, bicycle or motorcycle). Owned only by a real purchase; points only rent it.</summary>
        public bool IsVehicle => Array.IndexOf(StoreTabs.VehicleSlots, slot ?? "") >= 0;
        /// <summary>Every premium item that isn't a default can be rented for 48 hours with race points (100 points each time).</summary>
        public bool CanRent => HasSlot && !isDefault && !IsConsumable && !IsHelper && tickets == 0;
        public bool IsHelper => !string.IsNullOrEmpty(helper);
    }

    /// <summary>A product on one of Ahmed's websites. Opened in the browser, never sold in the game.</summary>
    [Serializable]
    public class WebProduct
    {
        public string id;
        public string category;
        public string nameAr;
        public string descAr;
        public string priceText;
        public string imageUrl;
        /// <summary>Full link, or a path relative to the shop's base URL.</summary>
        public string url;
    }

    [Serializable]
    public class WebCategory
    {
        public string id;
        public string nameAr;
    }

    [Serializable]
    public class WebShop
    {
        public string nameAr;
        /// <summary>Home page of the shop. Empty until Ahmed puts his link in, and then the shop shows «قريباً».</summary>
        public string baseUrl;
        /// <summary>
        /// Optional cart link. {base} is the base URL, {items} is "id:qty,id:qty", {ids} and {qtys} are the
        /// two halves of that, {count} is the number of pieces. Empty means checkout just opens the home page.
        /// </summary>
        public string checkoutTemplate;
        public WebCategory[] categories;
        public WebProduct[] products;

        public bool IsOpen => StoreLinks.IsWebUrl(baseUrl);

        public WebProduct Find(string id)
        {
            if (products == null || id == null) return null;
            foreach (var p in products) if (p != null && p.id == id) return p;
            return null;
        }

        public List<WebProduct> InCategory(string category)
        {
            var list = new List<WebProduct>();
            if (products == null) return list;
            foreach (var p in products)
                if (p != null && (string.IsNullOrEmpty(category) || p.category == category)) list.Add(p);
            return list;
        }
    }

    /// <summary>Everything the store sells. Lives in Resources/Store/StoreCatalog.json so it can be edited without code.</summary>
    [Serializable]
    public class StoreCatalog
    {
        public int version;
        /// <summary>Race points and hours of a rental (see StoreInventory.Rent).</summary>
        public int rentPoints = 100, rentHours = 48;
        public StoreItem[] items;
        public WebShop developerShop;
        public WebShop groceryShop;

        public StoreItem Find(string id)
        {
            if (items == null || string.IsNullOrEmpty(id)) return null;
            foreach (var i in items) if (i != null && i.id == id) return i;
            return null;
        }

        public StoreItem FindByProductId(string productId)
        {
            if (items == null || string.IsNullOrEmpty(productId)) return null;
            foreach (var i in items) if (i != null && i.productId == productId) return i;
            return null;
        }

        public List<StoreItem> InTab(string tab)
        {
            var list = new List<StoreItem>();
            if (items == null) return list;
            foreach (var i in items) if (i != null && i.tab == tab) list.Add(i);
            return list;
        }

        public StoreItem DefaultFor(string slot)
        {
            if (items == null || string.IsNullOrEmpty(slot)) return null;
            foreach (var i in items) if (i != null && i.slot == slot && i.isDefault) return i;
            return null;
        }

        /// <summary>Items sold for real money, for the purchase backend.</summary>
        public List<StoreItem> MoneyItems()
        {
            var list = new List<StoreItem>();
            if (items == null) return list;
            foreach (var i in items) if (i != null && i.SoldForMoney) list.Add(i);
            return list;
        }

        /// <summary>Checks ids, product ids, prices and bundle links. Returns null when it's fine, otherwise what's wrong.</summary>
        public string Validate()
        {
            if (items == null || items.Length == 0) return "no items";
            var ids = new HashSet<string>();
            var products = new HashSet<string>();
            var defaults = new HashSet<string>();
            foreach (var i in items)
            {
                if (i == null || string.IsNullOrEmpty(i.id)) return "item without id";
                if (!ids.Add(i.id)) return "duplicate id " + i.id;
                if (Array.IndexOf(StoreTabs.Order, i.tab) < 0 || i.tab == StoreTabs.Developer) return i.id + ": bad tab";
                if (i.SoldForMoney)
                {
                    if (!products.Add(i.productId)) return "duplicate product id " + i.productId;
                    if (i.productType != ProductKinds.Consumable && i.productType != ProductKinds.NonConsumable) return i.id + ": bad product type";
                    if (i.IsConsumable && i.HasSlot) return i.id + ": a look can't be consumable";
                    if (i.IsHelper && !i.IsConsumable) return i.id + ": helpers are used up, so consumable";
                }
                if (i.isDefault)
                {
                    if (!i.HasSlot) return i.id + ": default without slot";
                    if (!defaults.Add(i.slot)) return "two defaults for " + i.slot;
                }
                else if (!i.SoldForMoney && !i.SoldForCoins) return i.id + ": no way to get it";
                if (i.coinPrice < 0 || i.coins < 0 || i.usd < 0f) return i.id + ": negative amount";
                if (i.IsVehicle)
                {
                    // Performance items: bought once for real money, never with coins or points. Points only rent them.
                    if (Array.IndexOf(StoreTabs.VehicleTabs, i.tab) < 0) return i.id + ": a vehicle belongs in a vehicle tab";
                    if (i.coinPrice > 0) return i.id + ": a vehicle can't be bought with coins";
                    if (i.isDefault) { if (i.SoldForMoney) return i.id + ": the default vehicle is free"; }
                    else
                    {
                        if (!i.SoldForMoney || i.usd <= 0f) return i.id + ": a vehicle needs a direct price tier";
                        if (i.productType != ProductKinds.NonConsumable) return i.id + ": a vehicle is bought once";
                    }
                }
                if (i.tickets < 0 || (i.tickets > 0 && (!i.IsConsumable || i.HasSlot))) return i.id + ": tickets are consumable";
                if (i.IsHelper && (!Aron.Logic.Helpers.IsKind(i.helper) || i.helperCount < 1 || i.HasSlot)) return i.id + ": bad helper";
            }
            foreach (var i in items)
                if (i.grants != null)
                    foreach (var g in i.grants)
                        if (!ids.Contains(g) || g == i.id) return i.id + ": grants unknown item " + g;
            return null;
        }
    }
}
