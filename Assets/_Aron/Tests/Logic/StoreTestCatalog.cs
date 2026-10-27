// (c) Ahmed Alamrani. All rights reserved.
using Aron.Logic;

namespace Aron.Tests.Logic
{
    /// <summary>Small catalog shared by the store tests.</summary>
    internal static class StoreTestCatalog
    {
        public static StoreCatalog Make()
        {
            return new StoreCatalog
            {
                version = 1,
                items = new[]
                {
                    new StoreItem { id = "shemagh_red", tab = StoreTabs.Aron, slot = "shemagh", isDefault = true },
                    new StoreItem { id = "shemagh_white", tab = StoreTabs.Aron, slot = "shemagh", coinPrice = 300 },
                    new StoreItem { id = "shemagh_gold", tab = StoreTabs.Aron, slot = "shemagh", coinPrice = 1500,
                                    productId = "com.ahmdria1.aron.shemagh_gold", productType = ProductKinds.NonConsumable },
                    new StoreItem { id = "agal_black", tab = StoreTabs.Aron, slot = "agal", isDefault = true },
                    new StoreItem { id = "agal_fire", tab = StoreTabs.Aron, slot = "agal",
                                    productId = "com.ahmdria1.aron.agal_fire", productType = ProductKinds.NonConsumable },
                    new StoreItem { id = "coins_small", tab = StoreTabs.Coins, coins = 500,
                                    productId = "com.ahmdria1.aron.coins_small", productType = ProductKinds.Consumable },
                    new StoreItem { id = "remove_ads", tab = StoreTabs.Perks, removesAds = true,
                                    productId = "com.ahmdria1.aron.remove_ads", productType = ProductKinds.NonConsumable },
                    new StoreItem { id = "founder_pack", tab = StoreTabs.Perks, coins = 3000, grants = new[] { "shemagh_gold", "agal_fire" },
                                    productId = "com.ahmdria1.aron.founder_pack", productType = ProductKinds.NonConsumable },
                },
                developerShop = new WebShop { baseUrl = "" },
                groceryShop = new WebShop { baseUrl = "" },
            };
        }
    }
}
