// (c) Ahmed Alamrani. All rights reserved.
using Aron.Logic;
using NUnit.Framework;

namespace Aron.Tests.Logic
{
    public class StoreInventoryTests
    {
        private SaveData data;
        private StoreInventory inv;

        [SetUp]
        public void SetUp()
        {
            data = new SaveData();
            inv = new StoreInventory(StoreTestCatalog.Make(), data);
        }

        [Test]
        public void DefaultsAreOwnedAndWorn()
        {
            Assert.IsTrue(inv.Owns("shemagh_red"));
            Assert.AreEqual("shemagh_red", inv.Equipped("shemagh").id);
            Assert.IsFalse(inv.Owns("shemagh_white"));
        }

        [Test]
        public void OldSaveTurnsDatesIntoCoins()
        {
            var old = new SaveData { version = 1, totalDates = 240, ownedItems = null, equippedItems = null, handledOrders = null };
            var i = new StoreInventory(StoreTestCatalog.Make(), old);
            Assert.AreEqual(240, i.Coins);
            Assert.AreEqual(4, old.version);
            Assert.AreEqual("saudi", old.homeCountry, "old saves keep starting in Saudi Arabia");
            Assert.IsNotNull(old.ownedItems);
            new StoreInventory(StoreTestCatalog.Make(), old);
            Assert.AreEqual(240, old.coins, "only once");
        }

        [Test]
        public void BuyingWithCoinsSpendsAndEquips()
        {
            data.coins = 1000;
            Assert.AreEqual(CoinPurchase.Bought, inv.BuyWithCoins("shemagh_white"));
            Assert.AreEqual(700, inv.Coins);
            Assert.IsTrue(inv.Owns("shemagh_white"));
            Assert.IsTrue(inv.IsEquipped("shemagh_white"));
            Assert.IsFalse(inv.IsEquipped("shemagh_red"));
        }

        [Test]
        public void CannotBuyWithoutEnoughCoins()
        {
            data.coins = 100;
            Assert.AreEqual(CoinPurchase.NotEnoughCoins, inv.BuyWithCoins("shemagh_white"));
            Assert.AreEqual(100, inv.Coins);
            Assert.IsFalse(inv.Owns("shemagh_white"));
        }

        [Test]
        public void CannotBuyTwice()
        {
            data.coins = 1000;
            inv.BuyWithCoins("shemagh_white");
            Assert.AreEqual(CoinPurchase.AlreadyOwned, inv.BuyWithCoins("shemagh_white"));
            Assert.AreEqual(700, inv.Coins);
        }

        [Test]
        public void MoneyOnlyItemsAreNotForCoins()
        {
            data.coins = 99999;
            Assert.AreEqual(CoinPurchase.NotForCoins, inv.BuyWithCoins("agal_fire"));
            Assert.AreEqual(CoinPurchase.NotForCoins, inv.BuyWithCoins("coins_small"));
            Assert.AreEqual(CoinPurchase.Unknown, inv.BuyWithCoins("ghost"));
        }

        [Test]
        public void CannotEquipWhatYouDoNotOwn()
        {
            Assert.IsFalse(inv.Equip("shemagh_white"));
            Assert.AreEqual("shemagh_red", inv.Equipped("shemagh").id);
        }

        [Test]
        public void EquippingTheDefaultGoesBack()
        {
            data.coins = 300;
            inv.BuyWithCoins("shemagh_white");
            Assert.IsTrue(inv.Equip("shemagh_red"));
            Assert.AreEqual("shemagh_red", inv.Equipped("shemagh").id);
            Assert.IsEmpty(data.equippedItems);
        }

        [Test]
        public void SlotsAreIndependent()
        {
            data.coins = 300;
            inv.BuyWithCoins("shemagh_white");
            inv.GrantPurchase("com.ahmdria1.aron.agal_fire", "o1");
            Assert.AreEqual("shemagh_white", inv.Equipped("shemagh").id);
            Assert.AreEqual("agal_fire", inv.Equipped("agal").id);
        }

        [Test]
        public void EquippedItemThatIsNoLongerOwnedFallsBack()
        {
            data.equippedItems.Add("shemagh=shemagh_gold");
            Assert.AreEqual("shemagh_red", inv.Equipped("shemagh").id);
        }

        [Test]
        public void CoinPackAddsCoinsEveryTime()
        {
            Assert.AreEqual(Grant.Granted, inv.GrantPurchase("com.ahmdria1.aron.coins_small", "a"));
            Assert.AreEqual(Grant.Granted, inv.GrantPurchase("com.ahmdria1.aron.coins_small", "b"));
            Assert.AreEqual(1000, inv.Coins);
        }

        [Test]
        public void SameOrderIsOnlyGrantedOnce()
        {
            inv.GrantPurchase("com.ahmdria1.aron.coins_small", "a");
            Assert.AreEqual(Grant.Duplicate, inv.GrantPurchase("com.ahmdria1.aron.coins_small", "a"));
            Assert.AreEqual(500, inv.Coins);
        }

        [Test]
        public void OrderMemoryStaysSmall()
        {
            for (int i = 0; i < StoreInventory.OrderMemory + 20; i++) inv.GrantPurchase("com.ahmdria1.aron.coins_small", "o" + i);
            Assert.AreEqual(StoreInventory.OrderMemory, data.handledOrders.Count);
        }

        [Test]
        public void UnknownProductIsIgnored()
        {
            Assert.AreEqual(Grant.Unknown, inv.GrantPurchase("com.someone.else", "x"));
            Assert.AreEqual(0, inv.Coins);
        }

        [Test]
        public void RemoveAdsSticks()
        {
            inv.GrantPurchase("com.ahmdria1.aron.remove_ads", "x");
            Assert.IsTrue(inv.AdsRemoved);
            Assert.IsTrue(inv.Owns("remove_ads"));
        }

        [Test]
        public void FounderPackUnlocksEverythingAndCoinsOnce()
        {
            Assert.AreEqual(Grant.Granted, inv.GrantPurchase("com.ahmdria1.aron.founder_pack", "x"));
            Assert.IsTrue(inv.Owns("shemagh_gold"));
            Assert.IsTrue(inv.Owns("agal_fire"));
            Assert.AreEqual(3000, inv.Coins);
            // A restore after reinstalling hands it over again without a second pile of coins.
            Assert.AreEqual(Grant.AlreadyOwned, inv.GrantPurchase("com.ahmdria1.aron.founder_pack", "y"));
            Assert.AreEqual(3000, inv.Coins);
        }

        [Test]
        public void BoughtLookIsWornStraightAway()
        {
            inv.GrantPurchase("com.ahmdria1.aron.shemagh_gold", "x");
            Assert.AreEqual("shemagh_gold", inv.Equipped("shemagh").id);
        }

        [Test]
        public void RestoredLookDoesNotOverrideWhatYouWear()
        {
            data.coins = 300;
            inv.GrantPurchase("com.ahmdria1.aron.shemagh_gold", "x");
            inv.BuyWithCoins("shemagh_white");
            inv.GrantPurchase("com.ahmdria1.aron.shemagh_gold", "restore");
            Assert.AreEqual("shemagh_white", inv.Equipped("shemagh").id);
        }
    }
}
