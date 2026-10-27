// (c) Ahmed Alamrani. All rights reserved.
using Aron.Logic;
using NUnit.Framework;

namespace Aron.Tests.Logic
{
    public class StoreLinksTests
    {
        private static WebShop Shop(string url, string template = "") => new WebShop { baseUrl = url, checkoutTemplate = template };

        [Test]
        public void OnlyWebLinksCount()
        {
            Assert.IsTrue(StoreLinks.IsWebUrl("https://ahmed.example/shop"));
            Assert.IsTrue(StoreLinks.IsWebUrl("http://ahmed.example"));
            Assert.IsFalse(StoreLinks.IsWebUrl(""));
            Assert.IsFalse(StoreLinks.IsWebUrl(null));
            Assert.IsFalse(StoreLinks.IsWebUrl("products/1"));
            Assert.IsFalse(StoreLinks.IsWebUrl("file:///etc/passwd"));
            Assert.IsFalse(StoreLinks.IsWebUrl("intent://scan"));
        }

        [Test]
        public void RelativeProductPathJoinsTheBase()
        {
            var shop = Shop("https://ahmed.example/");
            Assert.AreEqual("https://ahmed.example/products/watch",
                StoreLinks.ProductUrl(shop, new WebProduct { url = "/products/watch" }));
            Assert.AreEqual("https://ahmed.example/p/1",
                StoreLinks.ProductUrl(Shop("https://ahmed.example"), new WebProduct { url = "p/1" }));
        }

        [Test]
        public void FullProductLinkIsKept()
        {
            Assert.AreEqual("https://other.example/x",
                StoreLinks.ProductUrl(Shop(""), new WebProduct { url = "https://other.example/x" }));
        }

        [Test]
        public void NoBaseMeansNoLink()
        {
            Assert.IsNull(StoreLinks.ProductUrl(Shop(""), new WebProduct { url = "products/watch" }));
            Assert.IsNull(StoreLinks.ProductUrl(Shop("https://a.example"), null));
        }

        [Test]
        public void ProductWithoutPathOpensTheHomePage()
        {
            Assert.AreEqual("https://a.example", StoreLinks.ProductUrl(Shop("https://a.example"), new WebProduct()));
        }

        [Test]
        public void CheckoutWithoutTemplateOpensHome()
        {
            var cart = new GroceryCart();
            cart.Add("rice");
            Assert.AreEqual("https://grocery.example", StoreLinks.CheckoutUrl(Shop("https://grocery.example"), cart));
        }

        [Test]
        public void CheckoutClosedShopHasNoLink()
        {
            var cart = new GroceryCart();
            cart.Add("rice");
            Assert.IsNull(StoreLinks.CheckoutUrl(Shop("", "{base}/cart?items={items}"), cart));
        }

        [Test]
        public void CheckoutPutsTheCartInTheLink()
        {
            var cart = new GroceryCart();
            cart.Add("rice", 2);
            cart.Add("dates");
            var url = StoreLinks.CheckoutUrl(Shop("https://grocery.example/", "{base}/cart?items={items}&n={count}"), cart);
            Assert.AreEqual("https://grocery.example/cart?items=rice%3A2%2Cdates%3A1&n=3", url);
        }

        [Test]
        public void CheckoutCanSplitIdsAndQuantities()
        {
            var cart = new GroceryCart();
            cart.Add("a", 3);
            cart.Add("b", 1);
            var url = StoreLinks.CheckoutUrl(Shop("https://g.example", "https://g.example/add?ids={ids}&qty={qtys}"), cart);
            Assert.AreEqual("https://g.example/add?ids=a%2Cb&qty=3%2C1", url);
        }

        [Test]
        public void RelativeTemplateHangsOffTheBase()
        {
            var cart = new GroceryCart();
            cart.Add("a");
            Assert.AreEqual("https://g.example/cart?i=a%3A1", StoreLinks.CheckoutUrl(Shop("https://g.example", "cart?i={items}"), cart));
        }

        [Test]
        public void ProductIdsAreEscaped()
        {
            var cart = new GroceryCart();
            cart.Add("أرز&بسمتي");
            var url = StoreLinks.CheckoutUrl(Shop("https://g.example", "{base}/c?i={items}"), cart);
            StringAssert.DoesNotContain("&بسمتي", url);
            StringAssert.StartsWith("https://g.example/c?i=", url);
        }

        [Test]
        public void EmptyCartOpensHome()
        {
            Assert.AreEqual("https://g.example", StoreLinks.CheckoutUrl(Shop("https://g.example", "{base}/c?i={items}"), new GroceryCart()));
        }
    }
}
