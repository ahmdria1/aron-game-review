// (c) Ahmed Alamrani. All rights reserved.
using System.Collections.Generic;
using Aron.Store;
using UnityEngine;

namespace Aron.Animals
{
    /// <summary>
    /// Store looks for the camel, horse and falcon. The item colour is the coat (or the falcon's back), the accent
    /// is the saddle blanket (or the falcon's face), and the effect switches on a matching "Deco_" group the
    /// prefab builder made: tassels, the royal breastplate, the falcon hood.
    /// </summary>
    public class AnimalCosmetics : MonoBehaviour
    {
        [SerializeField] private string slot = "camel";

        private static readonly int BaseColor = Shader.PropertyToID("_BaseColor");
        private static readonly int BaseMap = Shader.PropertyToID("_BaseMap");
        private static readonly int Smoothness = Shader.PropertyToID("_Smoothness");

        private readonly Dictionary<Renderer, Material[]> originals = new Dictionary<Renderer, Material[]>();
        private readonly List<Object> made = new List<Object>();
        private readonly List<GameObject> decos = new List<GameObject>();

        private void Start()
        {
            foreach (var r in GetComponentsInChildren<Renderer>(true)) originals[r] = r.sharedMaterials;
            foreach (var t in GetComponentsInChildren<Transform>(true)) if (t.name.StartsWith("Deco_")) decos.Add(t.gameObject);
            Apply();
            var s = StoreService.Instance;
            if (s != null) s.Changed += Apply;
        }

        private void OnDestroy()
        {
            var s = StoreService.Instance;
            if (s != null) s.Changed -= Apply;
            foreach (var o in made) if (o != null) Destroy(o);
        }

        public void Apply()
        {
            var svc = StoreService.Instance;
            var item = svc != null ? svc.Inventory.Equipped(slot) : null;
            foreach (var kv in originals) if (kv.Key != null) kv.Key.sharedMaterials = kv.Value;
            foreach (var o in made) if (o != null) Destroy(o);
            made.Clear();
            string effect = item != null ? item.effect ?? "" : "";
            foreach (var d in decos) d.SetActive(effect.Length > 0 && d.name == "Deco_" + effect);
            if (item == null) return;

            Color coat = Hex(item.color, Color.gray), accent = Hex(item.accent, coat);
            if (slot == "falcon")
            {
                Swap("_Wings", Tinted(coat));
                Swap("_Head", Tinted(accent));
                return;
            }
            Swap("_Main_Light", Tinted(Color.Lerp(coat, Color.white, 0.35f)));
            Swap("_Main_Dark", Tinted(coat * 0.7f));
            Swap("_Main", Tinted(coat));
            Swap("_Hair", Tinted(Color.Lerp(coat, Color.black, 0.5f)));
            var blanket = Tinted(accent);
            if (slot == "camel" || effect == "royal") Sadu(blanket, accent, Color.Lerp(accent, Color.white, 0.75f));
            SwapNamed("Saddle_Blanket", blanket);
            if (effect == "gold_saddle" || effect == "gold")
            {
                var gold = Tinted(new Color(0.95f, 0.75f, 0.25f));
                gold.SetFloat(Smoothness, 0.85f);
                SwapNamed("Saddle_Leather", gold);
                SwapNamed("Saddle_Wood", gold);
            }
        }

        /// <summary>Recolours materials whose name ends with <paramref name="suffix"/> (the tinted model materials).</summary>
        private void Swap(string suffix, Material m)
        {
            foreach (var kv in originals)
            {
                var r = kv.Key;
                if (r == null) continue;
                var mats = r.sharedMaterials;
                bool changed = false;
                for (int i = 0; i < mats.Length && i < kv.Value.Length; i++)
                {
                    var src = kv.Value[i];
                    if (src == null || mats[i] != src || !src.name.EndsWith(suffix)) continue;
                    mats[i] = m;
                    changed = true;
                }
                if (changed) r.sharedMaterials = mats;
            }
        }

        private void SwapNamed(string objectName, Material m)
        {
            foreach (var kv in originals)
            {
                if (kv.Key == null || !kv.Key.name.StartsWith(objectName)) continue;
                var mats = kv.Key.sharedMaterials;
                for (int i = 0; i < mats.Length; i++) mats[i] = m;
                kv.Key.sharedMaterials = mats;
            }
        }

        private Material Tinted(Color c)
        {
            Material template = null;
            foreach (var kv in originals) if (kv.Value.Length > 0 && kv.Value[0] != null) { template = kv.Value[0]; break; }
            var m = template != null ? new Material(template) : new Material(Shader.Find("Universal Render Pipeline/Lit"));
            m.name = "AnimalLook";
            if (m.HasProperty(BaseColor)) m.SetColor(BaseColor, c);
            m.color = c;
            if (m.HasProperty(BaseMap)) m.SetTexture(BaseMap, null);
            made.Add(m);
            return m;
        }

        private void Sadu(Material m, Color a, Color b)
        {
            const int w = 32, h = 16;
            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Repeat, filterMode = FilterMode.Point };
            for (int y = 0; y < h; y++)
                for (int x = 0; x < w; x++)
                {
                    int k = Mathf.Abs((x % 8) - 4);
                    tex.SetPixel(x, y, ((y % 8) >= k) == ((y / 8) % 2 == 0) ? a : b);
                }
            tex.Apply();
            made.Add(tex);
            if (m.HasProperty(BaseMap)) m.SetTexture(BaseMap, tex);
            m.mainTexture = tex;
            if (m.HasProperty(BaseColor)) m.SetColor(BaseColor, Color.white);
            m.color = Color.white;
        }

        private static Color Hex(string hex, Color fallback)
        {
            if (string.IsNullOrEmpty(hex)) return fallback;
            return ColorUtility.TryParseHtmlString(hex, out var c) ? c : fallback;
        }
    }
}
