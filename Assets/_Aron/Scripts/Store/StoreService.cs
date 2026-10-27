// (c) Ahmed Alamrani. All rights reserved.
using System;
using System.Collections.Generic;
using Aron.Core;
using Aron.Localization;
using Aron.Logic;
using Aron.Save;
using UnityEngine;

namespace Aron.Store
{
    /// <summary>
    /// Owns the catalog, the player's inventory and the connection to the app stores. Lives for the whole
    /// session and starts at launch, so a purchase interrupted last time is delivered straight away.
    /// </summary>
    public class StoreService : MonoBehaviour, IPurchaseListener
    {
        public const string CatalogResource = "Store/StoreCatalog";
        public const string FakeStorePref = "Aron.Store.UseFakeStore";

        /// <summary>Set by the Unity IAP assembly when the package is installed.</summary>
        public static Func<IPurchaseBackend> RealBackend;

        private static StoreService instance;

        public static StoreService Instance
        {
            get
            {
                if (instance == null && Application.isPlaying)
                {
                    var go = new GameObject("StoreService");
                    DontDestroyOnLoad(go);
                    instance = go.AddComponent<StoreService>();
                }
                return instance;
            }
        }

        public StoreCatalog Catalog { get; private set; }
        public StoreInventory Inventory
        {
            get
            {
                // The save can be swapped underneath us (reset from the menu, tests).
                var data = SaveSystem.Data;
                if (inventory == null || inventoryData != data)
                {
                    inventory = new StoreInventory(Catalog, data) { Clock = Aron.Core.GameClock.Now };
                    inventoryData = data;
                }
                return inventory;
            }
        }
        public bool StoreReady => backend != null && backend.IsReady;
        public bool StoreFailed { get; private set; }

        /// <summary>Coins, ownership or equipped items changed.</summary>
        public event Action Changed;
        /// <summary>Short line to show the player (already localized).</summary>
        public event Action<string> Notice;

        private IPurchaseBackend backend;
        private StoreInventory inventory;
        private SaveData inventoryData;
        private bool restoring;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void StartAtLaunch() => _ = Instance;

        private void Awake()
        {
            if (instance != null && instance != this) { Destroy(gameObject); return; }
            instance = this;
            Catalog = LoadCatalog();
            backend = PickBackend();
            try { backend.Initialize(Catalog.MoneyItems(), this); }
            catch (Exception e)
            {
                global::Aron.Logic.Diagnostics.Log.Warn("[Store] Could not start purchasing: " + e.Message);
                StoreFailed = true;
            }
        }

        private void OnDestroy() { if (instance == this) instance = null; }

        public static StoreCatalog LoadCatalog()
        {
            var ta = Resources.Load<TextAsset>(CatalogResource);
            StoreCatalog c = null;
            if (ta != null)
            {
                try { c = JsonUtility.FromJson<StoreCatalog>(ta.text); }
                catch (Exception e) { global::Aron.Logic.Diagnostics.Log.Warn("[Store] Catalog unreadable: " + e.Message); }
            }
            if (c == null) c = new StoreCatalog { items = new StoreItem[0] };
            if (c.developerShop == null) c.developerShop = new WebShop();
            if (c.groceryShop == null) c.groceryShop = new WebShop();
            var problem = c.Validate();
            if (problem != null) global::Aron.Logic.Diagnostics.Log.Warn("[Store] Catalog problem: " + problem);
            return c;
        }

        private static IPurchaseBackend PickBackend()
        {
#if UNITY_EDITOR
            if (UnityEditor.EditorPrefs.GetBool(FakeStorePref, true) || RealBackend == null) return new FakePurchaseBackend();
            return RealBackend();
#else
            return RealBackend != null ? RealBackend() : new NoPurchaseBackend();
#endif
        }

        // ---- Used by the store screen

        public string MoneyPrice(StoreItem item)
        {
            if (item == null || !item.SoldForMoney || backend == null) return null;
            return backend.PriceOf(item.productId);
        }

        public void BuyWithMoney(StoreItem item)
        {
            if (item == null || !item.SoldForMoney) return;
            if (!RemoteConfig.GameSettingsService.IsOn(Logic.Config.Feature.Purchases)) { Say("feature_off"); return; }
            if (!StoreReady) { Say("store_unavailable"); return; }
            if (!item.IsConsumable && Inventory.Owns(item.id)) { Say("store_owned"); return; }
            backend.Purchase(item.productId);
        }

        public void BuyWithCoins(StoreItem item)
        {
            if (item == null) return;
            switch (Inventory.BuyWithCoins(item.id))
            {
                case CoinPurchase.Bought:
                    GameEvents.PlaySfx("coin");
                    Say("store_bought", item.nameAr);
                    Commit();
                    break;
                case CoinPurchase.NotEnoughCoins: Say("store_need_coins", item.coinPrice - Inventory.Coins); break;
                case CoinPurchase.AlreadyOwned: Say("store_owned"); break;
            }
        }

        /// <summary>Rents a look for 48 hours with race points (see StoreInventory.Rent).</summary>
        public void Rent(StoreItem item)
        {
            if (item == null) return;
            switch (Inventory.Rent(item.id))
            {
                case StoreInventory.RentResult.Rented:
                    GameEvents.PlaySfx("coin");
                    Inventory.Equip(item.id);
                    Commit();
                    break;
                case StoreInventory.RentResult.NotEnoughPoints: Say("rent_not_enough"); break;
                case StoreInventory.RentResult.AlreadyOwned: Say("store_owned"); break;
            }
        }

        public void Equip(StoreItem item)
        {
            if (item != null && Inventory.Equip(item.id)) Commit();
        }

        public void Restore()
        {
            if (!StoreReady) { Say("store_unavailable"); return; }
            restoring = true;
            Say("store_restoring");
            backend.Restore();
        }

        private void Commit()
        {
            SaveSystem.Save();
            Changed?.Invoke();
        }

        private void Say(string key, params object[] args)
        {
            string text = args.Length == 0 ? Strings.Get(key) : Strings.Format(key, args);
            Notice?.Invoke(text);
        }

        // ---- IPurchaseListener

        public void OnStoreReady()
        {
            StoreFailed = false;
            Changed?.Invoke();
        }

        public void OnStoreFailed(string reason)
        {
            StoreFailed = true;
            global::Aron.Logic.Diagnostics.Log.Info("[Store] Purchasing unavailable: " + reason);
            Changed?.Invoke();
        }

        public void OnPurchased(string productId, string orderId)
        {
            var item = Catalog.FindByProductId(productId);
            var result = Inventory.GrantPurchase(productId, orderId);
            if (result == Grant.Unknown) global::Aron.Logic.Diagnostics.Log.Warn("[Store] Paid for a product the catalog doesn't know: " + productId);
            if (result == Grant.Granted && item != null && !restoring)
            {
                GameEvents.PlaySfx("life");
                Say("store_bought", item.nameAr);
                Aron.Telemetry.TelemetryService.PurchaseCompleted(productId);
            }
            Commit();
        }

        public void OnPurchaseFailed(string productId, string reason, bool cancelled)
        {
            global::Aron.Logic.Diagnostics.Log.Info("[Store] Purchase of " + productId + " failed: " + reason);
            Say(cancelled ? "store_cancelled" : "store_failed");
        }

        public void OnRestoreFinished(bool ok, string message)
        {
            restoring = false;
            global::Aron.Logic.Diagnostics.Log.Info("[Store] Restore finished: " + ok + " " + message);
            Say(ok ? "store_restored" : "store_restore_failed");
            Commit();
        }
    }
}
