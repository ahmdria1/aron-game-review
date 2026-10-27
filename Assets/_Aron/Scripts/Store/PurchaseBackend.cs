// (c) Ahmed Alamrani. All rights reserved.
using System.Collections.Generic;
using Aron.Logic;
using UnityEngine;

namespace Aron.Store
{
    /// <summary>What the store needs from Google Play / the App Store. Swapped for a fake one in the editor.</summary>
    public interface IPurchaseBackend
    {
        bool IsReady { get; }
        void Initialize(IReadOnlyList<StoreItem> products, IPurchaseListener listener);
        /// <summary>Price as the store shows it to this player ("3.99 ر.س"), or null when it isn't known yet.</summary>
        string PriceOf(string productId);
        void Purchase(string productId);
        void Restore();
    }

    public interface IPurchaseListener
    {
        void OnStoreReady();
        void OnStoreFailed(string reason);
        /// <summary>Grant the goods. The backend confirms the order with the store once this returns.</summary>
        void OnPurchased(string productId, string orderId);
        void OnPurchaseFailed(string productId, string reason, bool cancelled);
        void OnRestoreFinished(bool ok, string message);
    }

    /// <summary>
    /// Pretend store for the editor and for builds without Unity IAP: every purchase goes through at once.
    /// Non-consumables are remembered in PlayerPrefs so Restore has something to give back.
    /// </summary>
    public class FakePurchaseBackend : IPurchaseBackend
    {
        public const string OwnedPref = "Aron.FakeStore.Owned";
        private readonly Dictionary<string, StoreItem> items = new Dictionary<string, StoreItem>();
        private IPurchaseListener listener;
        private int orderCounter;

        public bool IsReady { get; private set; }

        public void Initialize(IReadOnlyList<StoreItem> products, IPurchaseListener l)
        {
            listener = l;
            foreach (var p in products) items[p.productId] = p;
            IsReady = true;
            global::Aron.Logic.Diagnostics.Log.Info("[Store] Fake store: " + items.Count + " products, nothing is charged.");
            listener.OnStoreReady();
        }

        public string PriceOf(string productId)
        {
            if (!items.TryGetValue(productId, out var item)) return null;
            float price = item.IsConsumable ? 3.99f + item.coins / 1000f * 4f : 9.99f;
            return price.ToString("0.00") + " ر.س (تجربة)";
        }

        public void Purchase(string productId)
        {
            if (!items.TryGetValue(productId, out var item))
            {
                listener.OnPurchaseFailed(productId, "unknown product", false);
                return;
            }
            if (!item.IsConsumable) Remember(productId);
            listener.OnPurchased(productId, "fake-" + System.DateTime.UtcNow.Ticks + "-" + (++orderCounter));
        }

        public void Restore()
        {
            var owned = PlayerPrefs.GetString(OwnedPref, "");
            int n = 0;
            foreach (var id in owned.Split(','))
            {
                if (id.Length == 0 || !items.ContainsKey(id)) continue;
                listener.OnPurchased(id, null);
                n++;
            }
            listener.OnRestoreFinished(true, n + " restored");
        }

        private static void Remember(string productId)
        {
            var owned = PlayerPrefs.GetString(OwnedPref, "");
            if (("," + owned + ",").Contains("," + productId + ",")) return;
            PlayerPrefs.SetString(OwnedPref, owned.Length == 0 ? productId : owned + "," + productId);
            PlayerPrefs.Save();
        }
    }

    /// <summary>Used when there is no way to pay on this build. Coins still work.</summary>
    public class NoPurchaseBackend : IPurchaseBackend
    {
        private IPurchaseListener listener;
        public bool IsReady => false;

        public void Initialize(IReadOnlyList<StoreItem> products, IPurchaseListener l)
        {
            listener = l;
            listener.OnStoreFailed("purchasing is not available on this build");
        }

        public string PriceOf(string productId) => null;
        public void Purchase(string productId) => listener?.OnPurchaseFailed(productId, "unavailable", false);
        public void Restore() => listener?.OnRestoreFinished(false, "unavailable");
    }
}
