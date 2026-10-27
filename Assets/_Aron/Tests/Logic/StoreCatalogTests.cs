// (c) Ahmed Alamrani. All rights reserved.
using Aron.Logic;
using NUnit.Framework;

namespace Aron.Tests.Logic
{
    public class StoreCatalogTests
    {
        [Test]
        public void TestCatalogIsValid() => Assert.IsNull(StoreTestCatalog.Make().Validate());

        [Test]
        public void FindsItemsByIdAndProductId()
        {
            var c = StoreTestCatalog.Make();
            Assert.AreEqual("shemagh_white", c.Find("shemagh_white").id);
            Assert.AreEqual("coins_small", c.FindByProductId("com.ahmdria1.aron.coins_small").id);
            Assert.IsNull(c.Find("nope"));
            Assert.IsNull(c.FindByProductId(""));
        }

        [Test]
        public void ListsTabsAndDefaults()
        {
            var c = StoreTestCatalog.Make();
            Assert.AreEqual(5, c.InTab(StoreTabs.Aron).Count);
            Assert.AreEqual(2, c.InTab(StoreTabs.Perks).Count);
            Assert.AreEqual("agal_black", c.DefaultFor("agal").id);
            Assert.AreEqual(5, c.MoneyItems().Count);
        }

        [Test]
        public void RejectsDuplicateIds()
        {
            var c = StoreTestCatalog.Make();
            c.items[1].id = "shemagh_red";
            StringAssert.Contains("duplicate id", c.Validate());
        }

        [Test]
        public void RejectsDuplicateProductIds()
        {
            var c = StoreTestCatalog.Make();
            c.items[4].productId = c.items[2].productId;
            StringAssert.Contains("duplicate product id", c.Validate());
        }

        [Test]
        public void RejectsConsumableLooks()
        {
            var c = StoreTestCatalog.Make();
            c.items[4].productType = ProductKinds.Consumable;
            Assert.IsNotNull(c.Validate());
        }

        [Test]
        public void RejectsBundlesPointingNowhere()
        {
            var c = StoreTestCatalog.Make();
            c.items[7].grants = new[] { "ghost" };
            Assert.IsNotNull(c.Validate());
        }

        [Test]
        public void RejectsItemsYouCannotGet()
        {
            var c = StoreTestCatalog.Make();
            c.items[1].coinPrice = 0;
            Assert.IsNotNull(c.Validate());
        }

        [Test]
        public void RejectsTwoDefaultsInOneSlot()
        {
            var c = StoreTestCatalog.Make();
            c.items[1].isDefault = true;
            Assert.IsNotNull(c.Validate());
        }

        [Test]
        public void ShopWithoutUrlIsClosed()
        {
            Assert.IsFalse(new WebShop { baseUrl = "" }.IsOpen);
            Assert.IsFalse(new WebShop { baseUrl = "   " }.IsOpen);
            Assert.IsFalse(new WebShop { baseUrl = "javascript:alert(1)" }.IsOpen);
            Assert.IsTrue(new WebShop { baseUrl = "https://shop.example.sa" }.IsOpen);
        }

        [Test]
        public void ShopListsProductsByCategory()
        {
            var shop = new WebShop
            {
                products = new[]
                {
                    new WebProduct { id = "rice", category = "grains" },
                    new WebProduct { id = "dates", category = "dates" },
                    new WebProduct { id = "bulgur", category = "grains" },
                }
            };
            Assert.AreEqual(2, shop.InCategory("grains").Count);
            Assert.AreEqual(3, shop.InCategory(null).Count);
            Assert.AreEqual("dates", shop.Find("dates").id);
        }
    }
}
