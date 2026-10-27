// (c) Ahmed Alamrani. All rights reserved.
using System;
using System.Collections;
using System.Collections.Generic;
using Aron.Audio;
using Aron.Core;
using Aron.Input;
using Aron.Localization;
using Aron.Logic;
using Aron.UI;
using TMPro;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;

namespace Aron.Store
{
    /// <summary>
    /// The "go in?" prompt and the two store screens (Ahmed's electronics store and his grocery), built in
    /// code like the rest of the UI. Right-to-left, inside the safe area, and the card grid re-flows from
    /// 4:3 tablets to 21:9 phones. The game is stopped while any of it is up.
    /// </summary>
    public class StoreScreen : MonoBehaviour
    {
        private static readonly Color Back = new Color(0.07f, 0.06f, 0.05f, 0.94f);
        private static readonly Color Card = new Color(1f, 1f, 1f, 0.07f);
        private static readonly Color TabIdle = new Color(1f, 1f, 1f, 0.1f);
        private static readonly Color Gold = new Color(0.95f, 0.75f, 0.25f);
        private static readonly Color Muted = new Color(1f, 1f, 1f, 0.15f);
        private static readonly Color Grocery = new Color(0.0f, 0.5f, 0.3f);

        private static StoreScreen instance;
        private static readonly GroceryCart cart = new GroceryCart();
        private static readonly Dictionary<string, Texture2D> images = new Dictionary<string, Texture2D>();

        private RectTransform promptRoot, storeRoot;
        private TextMeshProUGUI promptTitle;
        private TextMeshProUGUI title, wallet, notice, cartLabel;
        private RectTransform tabBar, grid, soon;
        private ScrollRect scroll;
        private Button restoreButton, clearButton, checkoutButton;
        private ShopKind kind;
        private string tab, musicBefore;
        private float noticeUntil;

        public static bool IsShowing => instance != null && (instance.promptRoot.gameObject.activeSelf || instance.storeRoot.gameObject.activeSelf);

        public static void Ask(ShopKind kind)
        {
            if (!StoreAllowed()) return;
            Ensure().ShowPrompt(kind);
        }

        public static void Open(ShopKind kind)
        {
            if (!StoreAllowed()) return;
            Ensure().ShowStore(kind);
        }

        // The server settings can pause the shops; what players own stays theirs.
        private static bool StoreAllowed()
        {
            if (RemoteConfig.GameSettingsService.IsOn(Logic.Config.Feature.Store)) return true;
            FeatureOffNotice.Show();
            return false;
        }

        private static StoreScreen Ensure()
        {
            if (instance == null) instance = new GameObject("StoreScreen").AddComponent<StoreScreen>();
            return instance;
        }

        private void Awake()
        {
            instance = this;
            Build();
        }

        private void OnEnable()
        {
            var s = StoreService.Instance;
            if (s == null) return;
            s.Changed += Refresh;
            s.Notice += Say;
        }

        private void OnDisable()
        {
            var s = StoreService.Instance;
            if (s == null) return;
            s.Changed -= Refresh;
            s.Notice -= Say;
        }

        private void OnDestroy()
        {
            if (instance == this) instance = null;
            if (GameManager.Instance != null && GameManager.Instance.InStore) GameManager.Instance.SetInStore(false);
        }

        private void Update()
        {
            if (!IsShowing) return;
            var input = InputRouter.Instance;
            if (input != null && input.Current.pausePressed) Close();
            if (notice != null && noticeUntil > 0f && Time.unscaledTime > noticeUntil)
            {
                noticeUntil = 0f;
                ArabicTMP.SetText(notice, DefaultNotice());
            }
        }

        // ---- Flow

        private void ShowPrompt(ShopKind k)
        {
            if (IsShowing) return;
            kind = k;
            ArabicTMP.SetText(promptTitle, Strings.Get(k == ShopKind.Developer ? "shop_dev" : "shop_grocery"));
            promptRoot.gameObject.SetActive(true);
            Hold(true);
        }

        private void ShowStore(ShopKind k)
        {
            kind = k;
            promptRoot.gameObject.SetActive(false);
            storeRoot.gameObject.SetActive(true);
            Hold(true);
            if (AudioManager.CurrentTrack != MusicCues.Store) { musicBefore = AudioManager.CurrentTrack; AudioManager.PlayMusic(MusicCues.Store); }
            tab = null;
            BuildTabs();
            Refresh();
        }

        private void Close()
        {
            promptRoot.gameObject.SetActive(false);
            storeRoot.gameObject.SetActive(false);
            Hold(false);
            if (!string.IsNullOrEmpty(musicBefore) && AudioManager.CurrentTrack == MusicCues.Store) AudioManager.PlayMusic(musicBefore);
            musicBefore = null;
        }

        private static void Hold(bool on)
        {
            if (GameManager.Instance != null) GameManager.Instance.SetInStore(on);
            else Time.timeScale = on ? 0f : 1f;
        }

        // ---- Layout

        private void Build()
        {
            var canvas = UIFactory.CreateCanvas("StoreCanvas", 80);
            canvas.transform.SetParent(transform, false);

            // Prompt
            promptRoot = UIFactory.Panel(canvas.transform, "Prompt", UIFactory.Dark);
            var safeP = SafeChild(promptRoot);
            var box = UIFactory.Panel(safeP, "Box", Back, false);
            box.anchorMin = new Vector2(0.18f, 0.25f); box.anchorMax = new Vector2(0.82f, 0.75f);
            box.offsetMin = box.offsetMax = Vector2.zero;
            promptTitle = Text(box, "Title", "", 64f, UIFactory.Sand, TextAlignmentOptions.Center);
            Place(promptTitle.rectTransform, 0.05f, 0.95f, 0.62f, 0.92f);
            var q = Text(box, "Question", Strings.Get("shop_enter_q"), 38f, Color.white, TextAlignmentOptions.Center);
            Place(q.rectTransform, 0.05f, 0.95f, 0.38f, 0.62f);
            var enter = MakeButton(box, Strings.Get("shop_enter"), UIFactory.Green, () => ShowStore(kind));
            Place((RectTransform)enter.transform, 0.52f, 0.92f, 0.08f, 0.32f);
            var later = MakeButton(box, Strings.Get("shop_later"), Muted, Close);
            Place((RectTransform)later.transform, 0.08f, 0.48f, 0.08f, 0.32f);
            promptRoot.gameObject.SetActive(false);

            // Store
            storeRoot = UIFactory.Panel(canvas.transform, "Store", Back);
            var safe = SafeChild(storeRoot);

            var header = Strip(safe, "Header", 1f, 130f, 0f);
            title = Text(header, "Title", "", 58f, UIFactory.Sand, TextAlignmentOptions.Right);
            Place(title.rectTransform, 0.42f, 0.99f, 0f, 1f);
            wallet = Text(header, "Wallet", "", 40f, Gold, TextAlignmentOptions.Center);
            Place(wallet.rectTransform, 0.2f, 0.42f, 0f, 1f);
            var close = MakeButton(header, Strings.Get("store_close"), Muted, Close);
            Place((RectTransform)close.transform, 0.01f, 0.19f, 0.15f, 0.85f);

            tabBar = Strip(safe, "Tabs", 1f, 96f, 136f);
            var h = tabBar.gameObject.AddComponent<HorizontalLayoutGroup>();
            h.spacing = 10f; h.padding = new RectOffset(10, 10, 0, 0);
            h.reverseArrangement = true;
            h.childControlWidth = h.childControlHeight = true;
            h.childForceExpandWidth = h.childForceExpandHeight = true;

            var footer = Strip(safe, "Footer", 0f, 110f, 0f);
            notice = Text(footer, "Notice", "", 34f, Color.white, TextAlignmentOptions.Center);
            Place(notice.rectTransform, 0.25f, 0.75f, 0f, 1f);
            restoreButton = MakeButton(footer, Strings.Get("store_restore"), Muted, () => StoreService.Instance.Restore());
            Place((RectTransform)restoreButton.transform, 0.01f, 0.24f, 0.12f, 0.88f);
            cartLabel = Text(footer, "Cart", "", 38f, UIFactory.Sand, TextAlignmentOptions.Right);
            Place(cartLabel.rectTransform, 0.76f, 0.99f, 0f, 1f);
            checkoutButton = MakeButton(footer, Strings.Get("grocery_checkout"), Grocery, Checkout);
            Place((RectTransform)checkoutButton.transform, 0.01f, 0.24f, 0.12f, 0.88f);
            clearButton = MakeButton(footer, Strings.Get("grocery_clear"), Muted, () => { cart.Clear(); Refresh(); });
            Place((RectTransform)clearButton.transform, 0.25f, 0.42f, 0.12f, 0.88f);

            // Scrolling card grid between the tabs and the footer.
            var view = new GameObject("Scroll", typeof(RectTransform), typeof(RectMask2D), typeof(Image)).GetComponent<RectTransform>();
            view.SetParent(safe, false);
            view.anchorMin = Vector2.zero; view.anchorMax = Vector2.one;
            view.offsetMin = new Vector2(10f, 116f); view.offsetMax = new Vector2(-10f, -240f);
            view.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.01f);
            scroll = view.gameObject.AddComponent<ScrollRect>();
            scroll.horizontal = false;
            scroll.movementType = ScrollRect.MovementType.Clamped;
            scroll.scrollSensitivity = 40f;
            grid = new GameObject("Grid", typeof(RectTransform)).GetComponent<RectTransform>();
            grid.SetParent(view, false);
            grid.anchorMin = new Vector2(0f, 1f); grid.anchorMax = new Vector2(1f, 1f); grid.pivot = new Vector2(0.5f, 1f);
            grid.offsetMin = grid.offsetMax = Vector2.zero;
            var g = grid.gameObject.AddComponent<GridLayoutGroup>();
            g.startCorner = GridLayoutGroup.Corner.UpperRight;
            g.childAlignment = TextAnchor.UpperRight;
            g.spacing = new Vector2(18f, 18f);
            g.padding = new RectOffset(6, 6, 6, 6);
            g.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            grid.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            grid.gameObject.AddComponent<ResponsiveGrid>();
            scroll.viewport = view;
            scroll.content = grid;

            soon = UIFactory.Panel(view, "Soon", new Color(0f, 0f, 0f, 0f));
            var s1 = Text(soon, "Soon", Strings.Get("store_soon"), 110f, Gold, TextAlignmentOptions.Center);
            Place(s1.rectTransform, 0f, 1f, 0.45f, 0.8f);
            var s2 = Text(soon, "Sub", Strings.Get("store_soon_sub"), 42f, Color.white, TextAlignmentOptions.Center);
            Place(s2.rectTransform, 0f, 1f, 0.25f, 0.45f);
            soon.gameObject.SetActive(false);

            storeRoot.gameObject.SetActive(false);
        }

        private void BuildTabs()
        {
            Clear(tabBar);
            var cat = StoreService.Instance.Catalog;
            if (kind == ShopKind.Developer)
            {
                var on = RemoteConfig.GameSettingsService.Current.features;
                foreach (var t in StoreTabs.Order) if (on.TabIsOn(t)) AddTab(t, Strings.Get("tab_" + t));
                if (tab == null || !on.TabIsOn(tab)) tab = StoreTabs.Aron;
            }
            else
            {
                AddTab("", Strings.Get("tab_all"));
                if (cat.groceryShop.categories != null)
                    foreach (var c in cat.groceryShop.categories)
                        if (c != null && !string.IsNullOrEmpty(c.id)) AddTab(c.id, c.nameAr);
                if (tab == null) tab = "";
            }
        }

        private void AddTab(string id, string label)
        {
            var b = MakeButton(tabBar, label, TabIdle, () => { tab = id; Refresh(); scroll.verticalNormalizedPosition = 1f; });
            b.name = "Tab_" + id;
        }

        // ---- Content

        private void Refresh()
        {
            if (!storeRoot.gameObject.activeSelf) return;
            var svc = StoreService.Instance;
            var cat = svc.Catalog;
            bool dev = kind == ShopKind.Developer;

            ArabicTMP.SetText(title, Strings.Get(dev ? "shop_dev" : "shop_grocery"));
            wallet.gameObject.SetActive(dev);
            if (dev) ArabicTMP.SetText(wallet, Strings.Format("store_wallet", svc.Inventory.Coins));
            restoreButton.gameObject.SetActive(dev);
            cartLabel.gameObject.SetActive(!dev);
            checkoutButton.gameObject.SetActive(!dev);
            clearButton.gameObject.SetActive(!dev && !cart.IsEmpty);
            if (!dev)
            {
                ArabicTMP.SetText(cartLabel, cart.IsEmpty ? Strings.Get("grocery_cart_empty") : Strings.Format("grocery_cart", cart.TotalQuantity));
                SetLabel(checkoutButton, Strings.Get(cat.groceryShop.IsOpen ? "grocery_checkout" : "grocery_checkout_soon"));
                checkoutButton.GetComponent<Image>().color = cat.groceryShop.IsOpen && !cart.IsEmpty ? Grocery : Muted;
            }

            foreach (Transform t in tabBar)
                t.GetComponent<Image>().color = t.name == "Tab_" + tab ? (dev ? UIFactory.Green : Grocery) : TabIdle;

            Clear(grid);
            bool closedShop = dev ? tab == StoreTabs.Developer && !cat.developerShop.IsOpen : false;
            soon.gameObject.SetActive(closedShop);
            if (noticeUntil <= 0f) ArabicTMP.SetText(notice, DefaultNotice());
            if (closedShop) return;

            if (!dev) { foreach (var p in cat.groceryShop.InCategory(tab)) GroceryCard(cat.groceryShop, p); return; }
            if (tab == StoreTabs.Developer) { foreach (var p in cat.developerShop.InCategory(null)) WebCard(cat.developerShop, p); return; }
            if (tab == StoreTabs.Companions)
                foreach (var def in Data.GameData.Companions.companions) UpgradeCard(svc, def);
            if (tab == StoreTabs.Aron || tab == StoreTabs.Noura) Wardrobe(tab);
            foreach (var item in cat.InTab(tab)) ItemCard(svc, item);
        }

        // Country costumes: the region's own by default, or one picked here (bought with coins if it has a price).
        private void Wardrobe(string hero)
        {
            var data = Save.SaveSystem.Data;
            bool regional = string.IsNullOrEmpty(Logic.Costumes.Wardrobe.PickOf(data, hero));
            var reg = NewCard(Strings.Get("wardrobe_region"), Strings.Get("wardrobe_region_desc"));
            CardButton(reg, Strings.Get(regional ? "store_equipped" : "store_equip"), regional ? Muted : UIFactory.Green, 0f, 1f, () =>
            {
                Logic.Costumes.Wardrobe.WearRegion(Save.SaveSystem.Data, hero);
                Worn();
            }).interactable = !regional;

            foreach (var c in Logic.Costumes.Wardrobe.For(Data.GameData.Costumes, hero))
            {
                bool owned = Logic.Costumes.Wardrobe.Owns(data, c), on = Logic.Costumes.Wardrobe.Wearing(data, c);
                var card = NewCard(c.nameAr, Strings.Get("country_" + c.country));
                string label = on ? Strings.Get("store_equipped") : owned ? Strings.Get("store_equip") : Strings.Format("wardrobe_buy", c.price);
                var costume = c;
                var b = CardButton(card, label, on ? Muted : owned ? UIFactory.Green : Gold, 0f, 1f, () =>
                {
                    if (!Logic.Costumes.Wardrobe.TryWear(Save.SaveSystem.Data, costume)) { Say(Strings.Get("upgrade_poor")); return; }
                    Worn();
                });
                b.interactable = !on;
                if (!owned) b.GetComponentInChildren<TMP_Text>().color = Color.black;
            }
        }

        private void Worn()
        {
            Save.SaveSystem.Save();
            CharacterCosmetics.RefreshAll();
            Refresh();
        }

        // Companion upgrades are paid in coins and saved with the rest of the progress.
        private void UpgradeCard(StoreService svc, Logic.CompanionDef def)
        {
            var data = Save.SaveSystem.Data;
            int level = Logic.CompanionUpgrades.Level(data, def.id);
            int cost = Logic.CompanionUpgrades.NextCost(def, data);
            var now = def.At(level);
            string next = cost >= 0 ? def.At(level + 1).descAr : now.descAr;
            var card = NewCard(def.nameAr, JoinLines(Strings.Format("upgrade_level", level, def.levels.Length), next));
            if (cost < 0) { CardButton(card, Strings.Get("upgrade_max"), Muted, 0f, 1f, null).interactable = false; return; }
            bool afford = data.coins >= cost;
            var b = CardButton(card, Strings.Format("upgrade_buy", cost), afford ? Gold : Muted, 0f, 1f, () =>
            {
                if (!Logic.CompanionUpgrades.TryUpgrade(def, Save.SaveSystem.Data)) { Say(Strings.Get("upgrade_poor")); return; }
                Save.SaveSystem.Save();
                var runner = FindFirstObjectByType<Animals.CompanionAbilityRunner>();
                if (runner != null) runner.Load();
                var oryx = FindFirstObjectByType<Animals.OryxCompanion>();
                if (oryx != null) oryx.Load();
                Say(Strings.Get("upgrade_done"));
                Refresh();
            });
            b.GetComponentInChildren<TMP_Text>().color = afford ? Color.black : Color.white;
        }

        private string DefaultNotice()
        {
            if (kind == ShopKind.Grocery)
                return StoreService.Instance.Catalog.groceryShop.IsOpen ? "" : Strings.Get("store_soon_sub");
            if (tab == StoreTabs.Companions) return Strings.Get("store_companions_note");
            return "";
        }

        private void ItemCard(StoreService svc, StoreItem item)
        {
            var inv = svc.Inventory;
            var card = NewCard(ItemName(item), Describe(item));
            Swatch(card, Parse(item.color, Color.gray), Parse(item.accent, Color.clear), item.effect);

            if (inv.Owns(item.id) && !item.IsConsumable)
            {
                if (item.HasSlot)
                {
                    bool on = inv.IsEquipped(item.id);
                    var b = CardButton(card, Strings.Get(on ? "store_equipped" : "store_equip"), on ? Muted : UIFactory.Green, 0f, 1f,
                        () => svc.Equip(item));
                    b.interactable = !on;
                }
                else CardButton(card, Strings.Get(item.removesAds ? "store_ads_gone" : "store_owned_short"), Muted, 0f, 1f, null).interactable = false;
                return;
            }

            if (item.CanRent)
            {
                // A second, smaller row above the buy buttons.
                var rent = CardButton(card, Strings.Format("mount_rent", svc.Catalog.rentPoints, svc.Catalog.rentHours),
                    inv.CanRent(svc.Catalog.rentPoints) ? UIFactory.Green : Muted, 0f, 1f, () => svc.Rent(item));
                var rt = (RectTransform)rent.transform;
                rt.anchorMin = new Vector2(rt.anchorMin.x, 0.21f);
                rt.anchorMax = new Vector2(rt.anchorMax.x, 0.35f);
            }
            bool both = item.SoldForCoins && item.SoldForMoney && !item.IsConsumable;
            if (item.SoldForCoins && !item.IsConsumable)
            {
                var b = CardButton(card, Strings.Format("store_coins_price", item.coinPrice), inv.CanAfford(item) ? Gold : Muted,
                    both ? 0.51f : 0f, 1f, () => svc.BuyWithCoins(item));
                b.GetComponentInChildren<TMP_Text>().color = inv.CanAfford(item) ? Color.black : Color.white;
            }
            if (item.SoldForMoney)
            {
                string price = svc.MoneyPrice(item);
                string label = price != null ? Strings.Format("store_pay", price)
                             : Strings.Get(svc.StoreFailed ? "store_unavailable" : "store_loading_price");
                var b = CardButton(card, label, price != null ? UIFactory.Green : Muted, 0f, both ? 0.49f : 1f, () => svc.BuyWithMoney(item));
                b.interactable = price != null;
            }
        }

        private static string ItemName(StoreItem item) => StoreItemNames.Name(item, Strings.Get, Strings.Format,
            k => Logic.Localization.UiStringTables.All[Strings.Language].ContainsKey(k) || Logic.Localization.UiStringTables.All["en"].ContainsKey(k));

        private static string Describe(StoreItem item)
        {
            if (item.tab == StoreTabs.Mounts && item.slot == "mount") return Strings.Get("mount_style_" + item.id.Substring("mount_".Length));
            string d = item.descAr ?? "";
            if (item.coins > 0) d = (d.Length > 0 ? d + " " : "") + "+" + item.coins;
            return d;
        }

        private void WebCard(WebShop shop, WebProduct p)
        {
            var card = NewCard(p.nameAr, JoinLines(p.priceText, p.descAr));
            Picture(card, p);
            string url = StoreLinks.ProductUrl(shop, p);
            var b = CardButton(card, Strings.Get(url != null ? "store_open_product" : "store_soon"), url != null ? UIFactory.Green : Muted, 0f, 1f,
                () => OpenWeb(url));
            b.interactable = url != null;
        }

        private void GroceryCard(WebShop shop, WebProduct p)
        {
            int qty = cart.QuantityOf(p.id);
            var card = NewCard(p.nameAr, JoinLines(p.priceText, qty > 0 ? Strings.Format("grocery_in_cart", qty) : p.descAr));
            Picture(card, p);
            if (qty > 0)
            {
                CardButton(card, "-", Muted, 0f, 0.28f, () => { cart.Remove(p.id); Refresh(); });
                CardButton(card, "+ " + Strings.Get("grocery_add"), Grocery, 0.3f, 1f, () => { cart.Add(p.id); Refresh(); });
            }
            else CardButton(card, Strings.Get("grocery_add"), Grocery, 0f, 1f, () => { cart.Add(p.id); GameEvents.PlaySfx("coin"); Refresh(); });
        }

        private void Checkout()
        {
            var shop = StoreService.Instance.Catalog.groceryShop;
            string url = StoreLinks.CheckoutUrl(shop, cart);
            if (url == null) { Say(Strings.Get("store_soon_sub")); return; }
            OpenWeb(url);
        }

        private void OpenWeb(string url)
        {
            if (!StoreLinks.IsWebUrl(url)) return;
            // Leaving the game for a web shop is behind the grown-up check (store families policies).
            ParentalGateDialog.Ask(() =>
            {
                Say(Strings.Get("store_opening"));
                Application.OpenURL(url);
            }, Strings.Get("link_leaving"));
        }

        private void Say(string text)
        {
            if (notice == null) return;
            ArabicTMP.SetText(notice, text);
            noticeUntil = Time.unscaledTime + 3f;
        }

        // ---- Cards

        private RectTransform NewCard(string name, string desc)
        {
            var card = UIFactory.Panel(grid, "Card", Card, false);
            var n = Text(card, "Name", name ?? "", 38f, Color.white, TextAlignmentOptions.Right);
            Place(n.rectTransform, 0.05f, 0.95f, 0.36f, 0.5f);
            var d = Text(card, "Desc", desc ?? "", 28f, new Color(1f, 1f, 1f, 0.75f), TextAlignmentOptions.TopRight);
            d.textWrappingMode = TextWrappingModes.Normal;
            Place(d.rectTransform, 0.05f, 0.95f, 0.21f, 0.36f);
            return card;
        }

        private static Button CardButton(RectTransform card, string label, Color color, float from, float to, Action onClick)
        {
            var b = MakeButton(card, label, color, onClick);
            var rt = (RectTransform)b.transform;
            rt.anchorMin = new Vector2(Mathf.Lerp(0.04f, 0.96f, from), 0.03f);
            rt.anchorMax = new Vector2(Mathf.Lerp(0.04f, 0.96f, to), 0.19f);
            rt.offsetMin = rt.offsetMax = Vector2.zero;
            return b;
        }

        private static void Swatch(RectTransform card, Color main, Color accent, string effect)
        {
            var top = UIFactory.Panel(card, "Swatch", main, false);
            Place(top, 0.06f, 0.94f, 0.52f, 0.95f);
            if (accent.a > 0f)
            {
                var stripe = UIFactory.Panel(top, "Accent", accent, false);
                Place(stripe, 0f, 1f, 0f, 0.22f);
            }
            if (!string.IsNullOrEmpty(effect))
            {
                var fx = UIFactory.Panel(top, "Effect", EffectTint(effect), false);
                Place(fx, 0.75f, 0.97f, 0.62f, 0.95f);
            }
        }

        private static Color EffectTint(string effect)
        {
            switch (effect)
            {
                case "fire": return new Color(1f, 0.45f, 0.05f);
                case "lightning": return new Color(0.55f, 0.85f, 1f);
                case "gold": case "gold_trim": case "clasp": case "gold_saddle": case "hood": return Gold;
                case "royal": return new Color(0.05f, 0.48f, 0.24f);
                default: return new Color(1f, 1f, 1f, 0.35f);
            }
        }

        private void Picture(RectTransform card, WebProduct p)
        {
            var frame = UIFactory.Panel(card, "Picture", new Color(1f, 1f, 1f, 0.12f), false);
            Place(frame, 0.06f, 0.94f, 0.52f, 0.95f);
            string initial = string.IsNullOrEmpty(p.nameAr) ? "?" : p.nameAr.Substring(0, 1);
            var letter = Text(frame, "Initial", initial, 90f, new Color(1f, 1f, 1f, 0.5f), TextAlignmentOptions.Center);
            Place(letter.rectTransform, 0f, 1f, 0f, 1f);
            if (!StoreLinks.IsWebUrl(p.imageUrl)) return;

            var raw = new GameObject("Image", typeof(RectTransform), typeof(RawImage)).GetComponent<RawImage>();
            raw.transform.SetParent(frame, false);
            Place(raw.rectTransform, 0f, 1f, 0f, 1f);
            raw.raycastTarget = false;
            raw.enabled = false;
            var fit = raw.gameObject.AddComponent<AspectRatioFitter>();
            fit.aspectMode = AspectRatioFitter.AspectMode.FitInParent;
            StartCoroutine(LoadImage(p.imageUrl, tex =>
            {
                if (raw == null || tex == null) return;
                raw.texture = tex;
                fit.aspectRatio = tex.height > 0 ? (float)tex.width / tex.height : 1f;
                raw.enabled = true;
                letter.gameObject.SetActive(false);
            }));
        }

        private static IEnumerator LoadImage(string url, Action<Texture2D> done)
        {
            if (images.TryGetValue(url, out var cached)) { done(cached); yield break; }
            using (var req = UnityWebRequestTexture.GetTexture(url))
            {
                req.timeout = 10;
                yield return req.SendWebRequest();
                if (req.result != UnityWebRequest.Result.Success) yield break;
                var tex = DownloadHandlerTexture.GetContent(req);
                if (tex != null) images[url] = tex;
                done(tex);
            }
        }

        // ---- Small builders

        private static RectTransform SafeChild(RectTransform parent)
        {
            var go = new GameObject("SafeArea", typeof(RectTransform), typeof(Aron.UI.SafeArea));
            go.transform.SetParent(parent, false);
            var rt = (RectTransform)go.transform;
            rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one; rt.offsetMin = rt.offsetMax = Vector2.zero;
            return rt;
        }

        /// <summary>Full-width band stuck to the top (y = 1) or bottom (y = 0), <paramref name="gap"/> away from that edge.</summary>
        private static RectTransform Strip(Transform parent, string name, float y, float height, float gap)
        {
            var rt = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
            rt.SetParent(parent, false);
            rt.anchorMin = new Vector2(0f, y); rt.anchorMax = new Vector2(1f, y); rt.pivot = new Vector2(0.5f, y);
            rt.sizeDelta = new Vector2(-20f, height);
            rt.anchoredPosition = new Vector2(0f, y > 0.5f ? -gap - 8f : gap + 4f);
            return rt;
        }

        private static void Place(RectTransform rt, float xMin, float xMax, float yMin, float yMax)
        {
            rt.anchorMin = new Vector2(xMin, yMin); rt.anchorMax = new Vector2(xMax, yMax);
            rt.offsetMin = rt.offsetMax = Vector2.zero;
        }

        private static TextMeshProUGUI Text(Transform parent, string name, string text, float size, Color color, TextAlignmentOptions align)
        {
            var t = UIFactory.Label(parent, name, text, size, color, align);
            t.enableAutoSizing = true;
            t.fontSizeMax = size;
            t.fontSizeMin = size * 0.45f;
            t.textWrappingMode = TextWrappingModes.NoWrap;
            t.overflowMode = TextOverflowModes.Ellipsis;
            return t;
        }

        private static Button MakeButton(Transform parent, string label, Color color, Action onClick)
        {
            var go = new GameObject("Button", typeof(RectTransform), typeof(Image), typeof(Button));
            go.transform.SetParent(parent, false);
            go.GetComponent<Image>().color = color;
            var b = go.GetComponent<Button>();
            if (onClick != null) b.onClick.AddListener(() => { GameEvents.PlaySfx("click"); onClick(); });
            var t = Text(go.transform, "Label", label, 36f, Color.white, TextAlignmentOptions.Center);
            Place(t.rectTransform, 0.04f, 0.96f, 0.05f, 0.95f);
            return b;
        }

        private static void SetLabel(Button b, string text) => ArabicTMP.SetText(b.GetComponentInChildren<TMP_Text>(), text);

        private static void Clear(Transform t)
        {
            for (int i = t.childCount - 1; i >= 0; i--)
            {
                var c = t.GetChild(i).gameObject;
                c.SetActive(false);
                Destroy(c);
            }
        }

        private static string JoinLines(string a, string b)
        {
            if (string.IsNullOrEmpty(a)) return b ?? "";
            if (string.IsNullOrEmpty(b)) return a;
            return a + " • " + b;
        }

        private static Color Parse(string hex, Color fallback)
        {
            if (string.IsNullOrEmpty(hex)) return fallback;
            return ColorUtility.TryParseHtmlString(hex, out var c) ? c : fallback;
        }
    }

    /// <summary>Picks how many cards fit across, so the grid works on any screen width.</summary>
    [RequireComponent(typeof(GridLayoutGroup))]
    public class ResponsiveGrid : MonoBehaviour
    {
        public float minCardWidth = 340f;
        public float cardHeight = 400f;
        private GridLayoutGroup grid;
        private float lastWidth = -1f;

        private void Awake() => grid = GetComponent<GridLayoutGroup>();

        private void LateUpdate()
        {
            float w = ((RectTransform)transform).rect.width;
            if (Mathf.Abs(w - lastWidth) < 0.5f || w <= 0f) return;
            lastWidth = w;
            float inner = w - grid.padding.left - grid.padding.right;
            int cols = Mathf.Max(2, Mathf.FloorToInt((inner + grid.spacing.x) / (minCardWidth + grid.spacing.x)));
            grid.constraintCount = cols;
            grid.cellSize = new Vector2((inner - grid.spacing.x * (cols - 1)) / cols, cardHeight);
        }
    }
}
