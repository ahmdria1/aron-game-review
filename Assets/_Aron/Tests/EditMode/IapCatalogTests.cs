// (c) Ahmed Alamrani. All rights reserved.
using System.Collections.Generic;
using System.Linq;
using Aron.Logic;
using Aron.Store;
using NUnit.Framework;
using UnityEngine;

namespace Aron.Tests.EditMode
{
    /// <summary>The shipped store catalog (Resources/Store/StoreCatalog.json) run through the pretend store, the way the editor does it.</summary>
    public class IapCatalogTests
    {
        private sealed class Recorder : IPurchaseListener
        {
            public bool Ready; public string Failed;
            public readonly List<string> Purchased = new List<string>();
            public readonly List<string> Orders = new List<string>();
            public readonly List<string> PurchaseFailures = new List<string>();
            public int RestoreOk = -1;
            public void OnStoreReady() => Ready = true;
            public void OnStoreFailed(string reason) => Failed = reason;
            public void OnPurchased(string productId, string orderId) { Purchased.Add(productId); Orders.Add(orderId); }
            public void OnPurchaseFailed(string productId, string reason, bool cancelled) => PurchaseFailures.Add(productId + ":" + reason);
            public void OnRestoreFinished(bool ok, string message) => RestoreOk = ok ? 1 : 0;
        }

        private StoreCatalog catalog;
        private string savedOwned;

        [SetUp]
        public void SetUp()
        {
            savedOwned = PlayerPrefs.GetString(FakePurchaseBackend.OwnedPref, "");
            PlayerPrefs.DeleteKey(FakePurchaseBackend.OwnedPref);
            catalog = StoreService.LoadCatalog();
        }

        [TearDown]
        public void TearDown()
        {
            if (savedOwned.Length == 0) PlayerPrefs.DeleteKey(FakePurchaseBackend.OwnedPref);
            else PlayerPrefs.SetString(FakePurchaseBackend.OwnedPref, savedOwned);
        }

        private List<StoreItem> Paid() => catalog.items.Where(i => !string.IsNullOrEmpty(i.productId)).ToList();

        [Test]
        public void ShippedCatalogIsValidAndHasPaidProducts()
        {
            Assert.IsNull(catalog.Validate(), "catalog fails its own validation");
            Assert.Greater(Paid().Count, 10, "expected the real-money products to be in the shipped catalog");
        }

        [Test]
        public void ProductIdsAreUniqueAndStoreSafe()
        {
            var ids = Paid().Select(i => i.productId).ToList();
            CollectionAssert.AllItemsAreUnique(ids);
            foreach (var id in ids)
                Assert.IsTrue(System.Text.RegularExpressions.Regex.IsMatch(id, "^[a-z0-9._]+$"), "Play Console ids are lower case letters, digits, dot, underscore: " + id);
        }

        [Test]
        public void EveryProductIsKnownToTheFakeStoreWithAPrice()
        {
            var store = new FakePurchaseBackend(); var rec = new Recorder();
            store.Initialize(Paid(), rec);
            Assert.IsTrue(rec.Ready); Assert.IsTrue(store.IsReady);
            foreach (var p in Paid()) Assert.IsFalse(string.IsNullOrEmpty(store.PriceOf(p.productId)), p.productId + " has no price");
        }

        [Test]
        public void BuyingEveryProductReportsTheMatchingOrder()
        {
            var store = new FakePurchaseBackend(); var rec = new Recorder();
            store.Initialize(Paid(), rec);
            foreach (var p in Paid()) store.Purchase(p.productId);
            CollectionAssert.AreEqual(Paid().Select(p => p.productId).ToList(), rec.Purchased);
            CollectionAssert.AllItemsAreUnique(rec.Orders, "each order needs its own id so one purchase is never granted twice");
            Assert.IsEmpty(rec.PurchaseFailures);
        }

        [Test]
        public void UnknownProductFailsWithoutGrantingAnything()
        {
            var store = new FakePurchaseBackend(); var rec = new Recorder();
            store.Initialize(Paid(), rec);
            store.Purchase("com.example.not_a_product");
            Assert.IsEmpty(rec.Purchased);
            Assert.AreEqual(1, rec.PurchaseFailures.Count);
        }

        [Test]
        public void RestoreGivesBackOnlyNonConsumables()
        {
            var store = new FakePurchaseBackend(); var first = new Recorder();
            store.Initialize(Paid(), first);
            foreach (var p in Paid()) store.Purchase(p.productId);

            var again = new FakePurchaseBackend(); var rec = new Recorder();
            again.Initialize(Paid(), rec);
            again.Restore();
            var expected = Paid().Where(p => !p.IsConsumable).Select(p => p.productId).OrderBy(x => x).ToList();
            CollectionAssert.AreEqual(expected, rec.Purchased.OrderBy(x => x).ToList());
            Assert.AreEqual(1, rec.RestoreOk);
        }

        [Test]
        public void NoPurchaseBackendFailsCleanlyAndCoinsStillWork()
        {
            var store = new NoPurchaseBackend(); var rec = new Recorder();
            store.Initialize(Paid(), rec);
            Assert.IsFalse(store.IsReady);
            Assert.IsNotNull(rec.Failed);
            store.Purchase(Paid()[0].productId);
            Assert.IsEmpty(rec.Purchased);
        }
    }
}
