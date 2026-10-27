// (c) Ahmed Alamrani. All rights reserved.
using System;
using System.Collections.Generic;
using Aron.Logic;
using UnityEngine;
using UnityEngine.Purchasing;

namespace Aron.Store
{
    /// <summary>
    /// Google Play and App Store purchases through Unity IAP 5. Only compiled when com.unity.purchasing is
    /// installed, so the game still builds without it.
    /// </summary>
    public class UnityIapBackend : IPurchaseBackend
    {
        private StoreController store;
        private IPurchaseListener listener;
        private readonly List<ProductDefinition> definitions = new List<ProductDefinition>();
        private bool connected;
        private bool productsFetched;

        public bool IsReady => connected && productsFetched;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Register() => StoreService.RealBackend = () => new UnityIapBackend();

        public async void Initialize(IReadOnlyList<StoreItem> products, IPurchaseListener l)
        {
            listener = l;
            foreach (var p in products)
                definitions.Add(new ProductDefinition(p.productId, p.IsConsumable ? ProductType.Consumable : ProductType.NonConsumable));
            if (definitions.Count == 0) { listener.OnStoreFailed("no products in the catalog"); return; }

            try
            {
                store = UnityIAPServices.StoreController();
                store.OnStoreConnected += OnConnected;
                store.OnStoreDisconnected += OnDisconnected;
                store.OnProductsFetched += OnProductsFetched;
                store.OnProductsFetchFailed += OnProductsFetchFailed;
                store.OnPurchasesFetched += OnPurchasesFetched;
                store.OnPurchasesFetchFailed += f => global::Aron.Logic.Diagnostics.Log.Warn("[Store] Could not read past purchases: " + f.Message);
                store.OnPurchasePending += OnPurchasePending;
                store.OnPurchaseConfirmed += OnPurchaseConfirmed;
                store.OnPurchaseFailed += OnPurchaseFailed;
                store.OnPurchaseDeferred += d => global::Aron.Logic.Diagnostics.Log.Info("[Store] Purchase waiting for approval");
                await store.Connect();
            }
            catch (Exception e)
            {
                connected = false;
                listener.OnStoreFailed(e.Message);
            }
        }

        private void OnConnected()
        {
            connected = true;
            store.FetchProducts(definitions);
        }

        private void OnDisconnected(StoreConnectionFailureDescription failure)
        {
            connected = false;
            listener.OnStoreFailed(failure.Message);
        }

        private void OnProductsFetched(List<Product> products)
        {
            productsFetched = true;
            listener.OnStoreReady();
            // Picks up anything bought on another install, and pending orders left when the app was closed.
            store.FetchPurchases();
        }

        private void OnProductsFetchFailed(ProductFetchFailed failed)
        {
            // Products that did load still work; only give up when none did.
            productsFetched = store.GetProducts().Count > 0;
            global::Aron.Logic.Diagnostics.Log.Warn("[Store] Some products failed to load: " + failed.FailureReason);
            if (productsFetched) listener.OnStoreReady();
            else listener.OnStoreFailed(failed.FailureReason);
        }

        private void OnPurchasesFetched(Orders orders)
        {
            foreach (var confirmed in orders.ConfirmedOrders)
                foreach (var id in ProductIds(confirmed))
                {
                    var p = store.GetProductById(id);
                    if (p != null && p.definition.type != ProductType.Consumable) listener.OnPurchased(id, null);
                }
        }

        private void OnPurchasePending(PendingOrder order)
        {
            string orderId = order.Info != null ? order.Info.TransactionID : null;
            var verifier = Aron.Logic.Integrity.PurchaseGate.Verifier;
            if (verifier == null)
            {
                // No backend yet: rely on the store's own signed result.
                foreach (var id in ProductIds(order)) listener.OnPurchased(id, orderId);
                store.ConfirmPurchase(order);
                return;
            }
            // With a backend: grant only after the server has checked the receipt with Google or Apple.
            var ids = ProductIds(order);
            var receipt = new Aron.Logic.Integrity.PurchaseReceipt
            {
                store = UnityEngine.Application.platform == UnityEngine.RuntimePlatform.IPhonePlayer ? "AppleAppStore" : "GooglePlay",
                productId = ids.Count > 0 ? ids[0] : "",
                transactionId = orderId,
                payload = order.Info != null ? order.Info.Receipt : null,
            };
            verifier.Verify(receipt, verdict =>
            {
                switch (verdict)
                {
                    case Aron.Logic.Integrity.ReceiptVerdict.Valid:
                        foreach (var id in ids) listener.OnPurchased(id, orderId);
                        store.ConfirmPurchase(order);
                        break;
                    case Aron.Logic.Integrity.ReceiptVerdict.Unreachable:
                        break;   // leave it pending; the store hands it back next launch, so nothing paid for is lost
                    case Aron.Logic.Integrity.ReceiptVerdict.Duplicate:
                        store.ConfirmPurchase(order);   // already granted once
                        break;
                    default:
                        global::Aron.Logic.Diagnostics.Log.Warn("[Store] Receipt refused: " + verdict);
                        store.ConfirmPurchase(order);   // clear it without granting
                        break;
                }
            });
        }

        private void OnPurchaseConfirmed(Order order)
        {
            if (order is FailedOrder failed)
                global::Aron.Logic.Diagnostics.Log.Warn("[Store] Confirming an order failed: " + failed.FailureReason + " " + failed.Details);
        }

        private void OnPurchaseFailed(FailedOrder failed)
        {
            var ids = ProductIds(failed);
            string id = ids.Count > 0 ? ids[0] : "";
            listener.OnPurchaseFailed(id, failed.FailureReason + " " + failed.Details,
                failed.FailureReason == PurchaseFailureReason.UserCancelled);
        }

        public string PriceOf(string productId)
        {
            if (!IsReady) return null;
            var p = store.GetProductById(productId);
            if (p == null || p.metadata == null || !p.availableToPurchase) return null;
            return p.metadata.localizedPriceString;
        }

        public void Purchase(string productId)
        {
            var p = IsReady ? store.GetProductById(productId) : null;
            if (p == null || !p.availableToPurchase)
            {
                listener.OnPurchaseFailed(productId, "product not available", false);
                return;
            }
            store.PurchaseProduct(p);
        }

        public void Restore()
        {
            if (store == null) { listener.OnRestoreFinished(false, "not connected"); return; }
            store.RestoreTransactions((ok, message) =>
            {
                if (ok) store.FetchPurchases();
                listener.OnRestoreFinished(ok, message ?? "");
            });
        }

        private static List<string> ProductIds(Order order)
        {
            var ids = new List<string>();
            if (order?.CartOrdered == null) return ids;
            foreach (var item in order.CartOrdered.Items())
                if (item?.Product?.definition != null) ids.Add(item.Product.definition.id);
            return ids;
        }
    }
}
