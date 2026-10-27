// (c) Ahmed Alamrani. All rights reserved.
using System;
using Aron.Localization;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem.UI;
#endif

namespace Aron.UI
{
    /// <summary>Builds uGUI + TMP UI from code so scenes stay generator-friendly (no prefabs needed).</summary>
    public static class UIFactory
    {
        public static readonly Color Sand = new Color(0.93f, 0.82f, 0.62f);
        public static readonly Color Green = new Color(0.0f, 0.42f, 0.24f); // Saudi green
        public static readonly Color Dark = new Color(0f, 0f, 0f, 0.65f);

        /// <summary>The player's text size (1 = normal), kept within what the screens can hold.</summary>
        public static float TextScale
        {
            get
            {
                var a = Aron.Save.SaveSystem.Data != null ? Aron.Save.SaveSystem.Data.access : null;
                return a == null ? 1f : Aron.Logic.Access.TextSizeRules.Clamp(a.textSize);
            }
        }

        public static Canvas CreateCanvas(string name, int order)
        {
            EnsureEventSystem();
            var go = new GameObject(name, typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            var c = go.GetComponent<Canvas>();
            c.renderMode = RenderMode.ScreenSpaceOverlay;
            c.sortingOrder = order;
            var s = go.GetComponent<CanvasScaler>();
            s.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            s.referenceResolution = new Vector2(1920f, 1080f);
            s.matchWidthOrHeight = 0.5f;
            go.AddComponent<AdaptiveCanvas>();
            return c;
        }

        /// <summary>
        /// Full-canvas child that stays clear of notches and rounded corners. HUD and buttons go in here;
        /// backgrounds and dimmers stay on the canvas itself so they still cover the whole screen.
        /// </summary>
        public static RectTransform SafeRoot(Canvas canvas)
        {
            var existing = canvas.transform.Find("SafeArea");
            if (existing != null) return (RectTransform)existing;
            var go = new GameObject("SafeArea", typeof(RectTransform), typeof(SafeArea));
            go.transform.SetParent(canvas.transform, false);
            var rt = (RectTransform)go.transform;
            rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one; rt.offsetMin = rt.offsetMax = Vector2.zero;
            return rt;
        }

        public static void EnsureEventSystem()
        {
            if (UnityEngine.Object.FindFirstObjectByType<EventSystem>() != null) return;
            var es = new GameObject("EventSystem", typeof(EventSystem));
#if ENABLE_INPUT_SYSTEM
            es.AddComponent<InputSystemUIInputModule>();
#else
            es.AddComponent<StandaloneInputModule>();
#endif
        }

        public static RectTransform Panel(Transform parent, string name, Color color, bool stretch = true)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            var rt = (RectTransform)go.transform;
            if (stretch) { rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one; rt.offsetMin = rt.offsetMax = Vector2.zero; }
            go.GetComponent<Image>().color = color;
            return rt;
        }

        /// <summary>Panel that fades in from transparent on its left edge to <paramref name="color"/> on its right.</summary>
        public static RectTransform FadePanel(Transform parent, string name, Color color)
        {
            var rt = Panel(parent, name, Color.white, false);
            var tex = new Texture2D(64, 1, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
            for (int x = 0; x < 64; x++)
            {
                float t = Mathf.SmoothStep(0f, 1f, x / 63f);
                tex.SetPixel(x, 0, new Color(color.r, color.g, color.b, color.a * t));
            }
            tex.Apply();
            rt.GetComponent<Image>().sprite = Sprite.Create(tex, new Rect(0, 0, 64, 1), new Vector2(0.5f, 0.5f));
            return rt;
        }

        public static RectTransform VerticalGroup(Transform parent, string name, float spacing)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(VerticalLayoutGroup));
            go.transform.SetParent(parent, false);
            var rt = (RectTransform)go.transform;
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(700f, 700f);
            var v = go.GetComponent<VerticalLayoutGroup>();
            v.spacing = spacing; v.childAlignment = TextAnchor.MiddleCenter;
            v.childControlHeight = false; v.childControlWidth = false; v.childForceExpandHeight = false; v.childForceExpandWidth = false;
            return rt;
        }

        public static TextMeshProUGUI Label(Transform parent, string name, string text, float size, Color color,
                                            TextAlignmentOptions align = TextAlignmentOptions.Center)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var t = go.AddComponent<TextMeshProUGUI>();
            // The text size option (Accessibility) scales every label made after it is changed. A larger label may shrink down to fit its box
            // rather than spill out of a button.
            float scale = TextScale;
            float scaled = Aron.Logic.Access.TextSizeRules.Font(Mathf.RoundToInt(size), scale);
            t.fontSize = scale == 1f ? size : scaled; t.color = color; t.alignment = align; t.raycastTarget = false;
            if (scale > 1f) { t.enableAutoSizing = true; t.fontSizeMax = scaled; t.fontSizeMin = Mathf.Max(8f, size * 0.7f); }
            ((RectTransform)go.transform).sizeDelta = new Vector2(900f, (scale == 1f ? size : scaled) * 1.6f);
            ArabicTMP.SetText(t, text);
            return t;
        }

        public static UnityEngine.UI.Button CreateButton(Transform parent, string name, string text, Action onClick)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(UnityEngine.UI.Button));
            go.transform.SetParent(parent, false);
            ((RectTransform)go.transform).sizeDelta = new Vector2(560f, 110f);
            go.GetComponent<Image>().color = Green;
            var b = go.GetComponent<UnityEngine.UI.Button>();
            b.onClick.AddListener(() => { Aron.Core.GameEvents.PlaySfx("click"); onClick?.Invoke(); });
            var lbl = Label(go.transform, "Label", text, 46f, Color.white);
            var lrt = (RectTransform)lbl.transform;
            lrt.anchorMin = Vector2.zero; lrt.anchorMax = Vector2.one; lrt.offsetMin = lrt.offsetMax = Vector2.zero;
            return b;
        }

        public static void SetButtonText(UnityEngine.UI.Button b, string text)
        {
            var t = b.GetComponentInChildren<TMP_Text>();
            ArabicTMP.SetText(t, text);
        }

        public static void Anchor(RectTransform rt, Vector2 anchor, Vector2 pos, Vector2 size)
        {
            rt.anchorMin = rt.anchorMax = anchor; rt.pivot = anchor; rt.anchoredPosition = pos; rt.sizeDelta = size;
        }
    }
}
