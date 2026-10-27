// (c) Ahmed Alamrani. All rights reserved.
using System.Collections.Generic;
using Aron.Logic;
using Aron.Logic.Costumes;
using UnityEngine;

namespace Aron.Store
{
    /// <summary>
    /// Dresses the player model in what is equipped in the store: shemagh, thobe, agal and leather shoes for
    /// Aron; abaya trim and handbag for Noura. Colour and material swaps on the generated pieces, plus shoes
    /// built on the foot bones. Re-applies whenever the store changes something.
    /// </summary>
    public class CharacterCosmetics : MonoBehaviour
    {
        public const string Shemagh = "shemagh", Thobe = "thobe", Agal = "agal", Shoes = "shoes";
        public const string AbayaTrim = "abaya_trim", Handbag = "handbag";

        private static readonly int BaseColor = Shader.PropertyToID("_BaseColor");
        private static readonly int BaseMap = Shader.PropertyToID("_BaseMap");
        private static readonly int Smoothness = Shader.PropertyToID("_Smoothness");

        private string characterId;
        private Material template;
        private readonly Dictionary<Renderer, Material[]> originals = new Dictionary<Renderer, Material[]>();
        private readonly List<Object> made = new List<Object>();
        private readonly List<GameObject> extras = new List<GameObject>();
        private TrailRenderer trail;
        private Gradient trailGradient;
        private AnimationCurve trailWidth;
        private float trailTime;
        private AgalGlow glow;

        public static CharacterCosmetics Attach(GameObject model, string characterId)
        {
            var c = model.GetComponent<CharacterCosmetics>();
            if (c == null) c = model.AddComponent<CharacterCosmetics>();
            c.characterId = characterId;
            return c;
        }

        private void Start()
        {
            Remember();
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

        private void Remember()
        {
            foreach (var r in GetComponentsInChildren<Renderer>(true))
            {
                originals[r] = r.sharedMaterials;
                if (template == null && r is MeshRenderer && r.sharedMaterial != null && r.sharedMaterial.HasProperty(BaseColor))
                    template = r.sharedMaterial;
            }
            var tip = Find(transform, "Tip");
            trail = tip != null ? tip.GetComponent<TrailRenderer>() : null;
            if (trail != null)
            {
                trailGradient = trail.colorGradient;
                trailWidth = trail.widthCurve;
                trailTime = trail.time;
            }
        }

        public void Apply()
        {
            // The costume kit goes on last, over the store's colours, and is taken off first so both restore cleanly.
            var dresser = Characters.CostumeDresser.Attach(gameObject);
            dresser.Undress();
            Restore();
            var costume = Data.CostumeChoice.For(characterId == "noura" ? "noura" : "aron");
            var svc = StoreService.Instance;
            if (svc != null)
            {
                var inv = svc.Inventory;
                if (characterId == "noura")
                {
                    DressAbaya(inv.Equipped(AbayaTrim));
                    DressHandbag(inv.Equipped(Handbag));
                }
                else
                {
                    DressThobe(inv.Equipped(Thobe));
                    // The kit brings its own shoes; building the store pair too would put two on each foot.
                    if (!KitHasShoes(costume)) BuildShoes(inv.Equipped(Shoes));
                }
            }
            dresser.Dress(costume);
            // The kit builds the headdress he actually wears, so the store colours go on after it.
            if (svc != null && characterId != "noura")
            {
                DressShemagh(svc.Inventory.Equipped(Shemagh));
                DressAgal(svc.Inventory.Equipped(Agal));
            }
            lastSwing = true;
        }

        private static bool KitHasShoes(CostumeDefinition c)
        {
            if (c == null) return false;
            foreach (var m in c.meshes) if (m.slot == "feet") return true;
            return false;
        }

        private Player.MeleeAttack swing;
        private bool lastSwing = true;

        // The agal is either on his head or in his hand, never both.
        private void LateUpdate()
        {
            if (characterId == "noura") return;
            if (swing == null) { swing = GetComponentInParent<Player.MeleeAttack>(); if (swing == null) return; }
            bool now = swing.IsSwinging;
            if (now == lastSwing) return;
            lastSwing = now;
            var d = GetComponent<Characters.CostumeDresser>();
            if (d != null) d.SetAgal(true, now);
        }

        /// <summary>Re-dresses everyone on screen, after a wardrobe pick.</summary>
        public static void RefreshAll()
        {
            foreach (var c in FindObjectsByType<CharacterCosmetics>(FindObjectsSortMode.None)) c.Apply();
        }

        private void Restore()
        {
            foreach (var kv in originals) if (kv.Key != null) kv.Key.sharedMaterials = kv.Value;
            foreach (var o in made) if (o != null) Destroy(o);
            made.Clear();
            foreach (var g in extras) if (g != null) Destroy(g);
            extras.Clear();
            if (glow != null) Destroy(glow);
            if (trail != null)
            {
                trail.colorGradient = trailGradient;
                trail.widthCurve = trailWidth;
                trail.time = trailTime;
            }
        }

        // ---- Aron

        private void DressShemagh(StoreItem item)
        {
            if (item == null || item.isDefault) return;
            var cloth = Tinted(Hex(item.color, UnityEngine.Color.red));
            if (item.effect == "check") Pattern(cloth, Checks(Hex(item.color, UnityEngine.Color.red), Hex(item.accent, UnityEngine.Color.white)));
            PaintAll(transform, "Cap", cloth);
            PaintAll(transform, "Drape", cloth);
            if (item.effect == "gold_trim")
            {
                var gold = Tinted(Hex(item.accent, new Color(0.95f, 0.75f, 0.25f)));
                gold.SetFloat(Smoothness, 0.8f);
                var drape = Find(transform, "Drape");
                if (drape != null) Extra(PrimitiveType.Cube, "GoldTrim", drape, new Vector3(0f, -0.48f, 0f), new Vector3(1.04f, 0.06f, 1.08f), gold);
                var cap = Find(transform, "Cap");
                if (cap != null) Extra(PrimitiveType.Cylinder, "GoldBand", cap, new Vector3(0f, -0.18f, 0f), new Vector3(1.02f, 0.03f, 1.02f), gold);
            }
        }

        private void DressThobe(StoreItem item)
        {
            if (item == null || item.isDefault) return;
            var main = Hex(item.color, UnityEngine.Color.white);
            var thobe = Tinted(main);
            Paint(Find(transform, "Thobe"), thobe);
            ReplaceNamed("Clothes", thobe);
            var accent = Hex(item.accent, main);
            ReplaceNamed("Band", Tinted(accent));
        }

        private void DressAgal(StoreItem item)
        {
            if (item == null || item.isDefault) return;
            var cord = Tinted(Hex(item.color, UnityEngine.Color.black));
            cord.SetFloat(Smoothness, item.effect == "gold" ? 0.85f : 0.5f);
            PaintAll(transform, "AgalRing", cord);
            PaintAll(transform, "Cord", cord);
            PaintAll(transform, "Loop", cord);

            if (trail == null) return;
            switch (item.effect)
            {
                case "gold":
                    trail.colorGradient = Fade(new Color(1f, 0.92f, 0.55f), new Color(0.9f, 0.65f, 0.15f));
                    break;
                case "fire":
                    trail.colorGradient = Fade(new Color(1f, 0.9f, 0.3f), new Color(0.9f, 0.15f, 0.02f));
                    trail.widthCurve = new AnimationCurve(new Keyframe(0f, 0.3f), new Keyframe(1f, 0.02f));
                    trail.time = trailTime * 1.6f;
                    break;
                case "lightning":
                    trail.colorGradient = Fade(UnityEngine.Color.white, new Color(0.35f, 0.7f, 1f));
                    trail.widthCurve = new AnimationCurve(new Keyframe(0f, 0.12f), new Keyframe(0.3f, 0.26f), new Keyframe(0.55f, 0.06f),
                                                          new Keyframe(0.75f, 0.2f), new Keyframe(1f, 0f));
                    trail.time = trailTime * 1.3f;
                    break;
            }
            if (item.effect == "fire" || item.effect == "lightning")
            {
                glow = gameObject.AddComponent<AgalGlow>();
                glow.Setup(cord, item.effect == "fire" ? new Color(1f, 0.35f, 0.05f) : new Color(0.4f, 0.75f, 1f), item.effect == "lightning");
            }
        }

        /// <summary>
        /// Classic closed leather shoes on both feet: sole, heel, a rounded toe box and the upper.
        /// Built in the model's rest pose and then carried by the foot bones.
        /// </summary>
        private void BuildShoes(StoreItem item)
        {
            if (item == null) return;
            var leather = Tinted(Hex(item.color, new Color(0.08f, 0.06f, 0.05f)));
            leather.SetFloat(Smoothness, 0.75f);
            var sole = Tinted(Hex(item.accent, new Color(0.05f, 0.04f, 0.03f)));
            sole.SetFloat(Smoothness, 0.3f);
            float s = HeightScale();
            Vector3 fwd = transform.forward, up = transform.up;
            foreach (var side in new[] { "Foot.L", "Foot.R" })
            {
                var foot = Find(transform, side);
                if (foot == null) continue;
                Vector3 ankle = foot.position;
                // Laid out at world scale first, then handed to the foot bone so it walks with him.
                var shoe = new GameObject("Shoe_" + side).transform;
                shoe.position = ankle - up * Vector3.Dot(ankle - transform.position, up);
                shoe.rotation = Quaternion.LookRotation(fwd, up);
                Part(PrimitiveType.Cube, "Sole", shoe, new Vector3(0f, 0.012f, 0.045f), new Vector3(0.1f, 0.024f, 0.27f), s, sole);
                Part(PrimitiveType.Cube, "Heel", shoe, new Vector3(0f, 0.022f, -0.065f), new Vector3(0.088f, 0.044f, 0.07f), s, sole);
                Part(PrimitiveType.Sphere, "Upper", shoe, new Vector3(0f, 0.06f, 0.02f), new Vector3(0.105f, 0.085f, 0.21f), s, leather);
                Part(PrimitiveType.Sphere, "ToeBox", shoe, new Vector3(0f, 0.045f, 0.12f), new Vector3(0.098f, 0.06f, 0.11f), s, leather);
                Part(PrimitiveType.Cylinder, "Collar", shoe, new Vector3(0f, 0.09f, -0.02f), new Vector3(0.095f, 0.012f, 0.11f), s, leather);
                if (item.effect == "oxford")
                {
                    var lace = Tinted(Hex(item.accent, UnityEngine.Color.black));
                    for (int i = 0; i < 3; i++)
                        Part(PrimitiveType.Cube, "Lace", shoe, new Vector3(0f, 0.1f - i * 0.008f, 0.03f + i * 0.022f), new Vector3(0.05f, 0.006f, 0.008f), s, lace);
                }
                else if (item.effect == "loafer")
                {
                    var strap = Tinted(Hex(item.accent, new Color(0.6f, 0.45f, 0.2f)));
                    Part(PrimitiveType.Cube, "Strap", shoe, new Vector3(0f, 0.095f, 0.06f), new Vector3(0.08f, 0.01f, 0.025f), s, strap);
                }
                shoe.SetParent(foot, true);
                extras.Add(shoe.gameObject);
            }
        }

        private float HeightScale()
        {
            // Sizes above are for a 1.8 m man, whose head bone sits about 1.55 m up.
            var head = Find(transform, "Head");
            if (head == null) return 1f;
            float h = Vector3.Dot(head.position - transform.position, transform.up);
            return h > 0.5f ? h / 1.55f : 1f;
        }

        // ---- Noura

        private void DressAbaya(StoreItem item)
        {
            if (item == null || item.isDefault) return;
            var trim = Tinted(Hex(item.color, UnityEngine.Color.black));
            if (item.effect == "sadu") Pattern(trim, Sadu(Hex(item.color, UnityEngine.Color.red), Hex(item.accent, UnityEngine.Color.black)));
            if (item.effect == "pearl") trim.SetFloat(Smoothness, 0.9f);
            ReplaceNamed("Band", trim);
        }

        private void DressHandbag(StoreItem item)
        {
            if (item == null || item.isDefault) return;
            var leather = Tinted(Hex(item.color, UnityEngine.Color.black));
            leather.SetFloat(Smoothness, 0.65f);
            if (item.effect == "sadu") Pattern(leather, Sadu(Hex(item.color, UnityEngine.Color.red), Hex(item.accent, UnityEngine.Color.black)));
            Paint(Find(transform, "Bag"), leather);
            Paint(Find(transform, "Strap"), leather);
            var bag = Find(transform, "Bag");
            if (bag != null && (item.effect == "clasp" || item.effect == "sadu"))
            {
                var gold = Tinted(Hex(item.accent, new Color(0.95f, 0.75f, 0.25f)));
                gold.SetFloat(Smoothness, 0.9f);
                Extra(PrimitiveType.Cube, "Clasp", bag, new Vector3(0f, 0.3f, 0.52f), new Vector3(0.3f, 0.18f, 0.06f), gold);
            }
        }

        // ---- Helpers

        private Material Tinted(Color c)
        {
            var m = template != null ? new Material(template) : new Material(Shader.Find("Universal Render Pipeline/Lit"));
            m.name = "Cosmetic";
            if (m.HasProperty(BaseColor)) m.SetColor(BaseColor, c);
            m.color = c;
            if (m.HasProperty(BaseMap)) m.SetTexture(BaseMap, null);
            made.Add(m);
            return m;
        }

        private static void Pattern(Material m, Texture2D tex)
        {
            m.SetTexture(BaseMap, tex);
            m.mainTexture = tex;
            if (m.HasProperty(BaseColor)) m.SetColor(BaseColor, UnityEngine.Color.white);
            m.color = UnityEngine.Color.white;
        }

        private static void PaintAll(Transform root, string name, Material m)
        {
            if (root.name == name) Paint(root, m);
            foreach (Transform c in root) PaintAll(c, name, m);
        }

        private static void Paint(Transform t, Material m)
        {
            if (t == null) return;
            var r = t.GetComponent<Renderer>();
            if (r == null) return;
            var mats = r.sharedMaterials;
            for (int i = 0; i < mats.Length; i++) mats[i] = m;
            r.sharedMaterials = mats;
        }

        /// <summary>Swaps every model material whose name contains <paramref name="part"/> (e.g. "Clothes", "Band").</summary>
        private void ReplaceNamed(string part, Material m)
        {
            foreach (var kv in originals)
            {
                var r = kv.Key;
                if (r == null || !(r is SkinnedMeshRenderer)) continue;
                var mats = r.sharedMaterials;
                bool changed = false;
                for (int i = 0; i < mats.Length; i++)
                {
                    var src = kv.Value[i];
                    if (src == null || src.name.IndexOf(part, System.StringComparison.OrdinalIgnoreCase) < 0) continue;
                    var copy = new Material(m);
                    var tex = src.HasProperty(BaseMap) ? src.GetTexture(BaseMap) : null;
                    if (tex != null && m.GetTexture(BaseMap) == null) copy.SetTexture(BaseMap, tex);
                    made.Add(copy);
                    mats[i] = copy;
                    changed = true;
                }
                if (changed) r.sharedMaterials = mats;
            }
        }

        private void Extra(PrimitiveType type, string name, Transform parent, Vector3 localPos, Vector3 localScale, Material m)
        {
            var go = PrimitiveFactory.Create(type);
            go.name = name;
            Destroy(go.GetComponent<Collider>());
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPos;
            go.transform.localScale = localScale;
            go.GetComponent<Renderer>().sharedMaterial = m;
            extras.Add(go);
        }

        private static void Part(PrimitiveType type, string name, Transform shoe, Vector3 pos, Vector3 size, float s, Material m)
        {
            var go = PrimitiveFactory.Create(type);
            go.name = name;
            Destroy(go.GetComponent<Collider>());
            go.transform.SetParent(shoe, false);
            go.transform.localPosition = pos * s;
            go.transform.localScale = size * s;
            go.GetComponent<Renderer>().sharedMaterial = m;
        }

        private Texture2D Checks(Color main, Color line)
        {
            const int n = 32;
            var tex = new Texture2D(n, n, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Repeat, filterMode = FilterMode.Bilinear };
            for (int y = 0; y < n; y++)
                for (int x = 0; x < n; x++)
                {
                    // Small checks with a darker crossing, like a real shemagh.
                    bool cx = (x / 4) % 2 == 0, cy = (y / 4) % 2 == 0;
                    tex.SetPixel(x, y, cx && cy ? main : cx || cy ? UnityEngine.Color.Lerp(main, line, 0.6f) : line);
                }
            tex.Apply();
            made.Add(tex);
            return tex;
        }

        private Texture2D Sadu(Color a, Color b)
        {
            const int w = 32, h = 16;
            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Repeat, filterMode = FilterMode.Point };
            for (int y = 0; y < h; y++)
                for (int x = 0; x < w; x++)
                {
                    // Bands of triangles, the usual Sadu weave look.
                    int k = Mathf.Abs((x % 8) - 4);
                    bool tri = (y % 8) >= k;
                    bool band = (y / 8) % 2 == 0;
                    tex.SetPixel(x, y, band == tri ? a : b);
                }
            tex.Apply();
            made.Add(tex);
            return tex;
        }

        private static Gradient Fade(Color from, Color to)
        {
            var g = new Gradient();
            g.SetKeys(new[] { new GradientColorKey(from, 0f), new GradientColorKey(to, 1f) },
                      new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(0f, 1f) });
            return g;
        }

        private static Color Hex(string hex, Color fallback)
        {
            if (string.IsNullOrEmpty(hex)) return fallback;
            return ColorUtility.TryParseHtmlString(hex, out var c) ? c : fallback;
        }

        private static Transform Find(Transform root, string name)
        {
            if (root.name == name) return root;
            foreach (Transform c in root) { var f = Find(c, name); if (f != null) return f; }
            return null;
        }
    }

    /// <summary>Makes the fire and lightning agal glow; lightning flickers.</summary>
    public class AgalGlow : MonoBehaviour
    {
        private static readonly int Emission = Shader.PropertyToID("_EmissionColor");
        private Material mat;
        private Color color;
        private bool flicker;

        public void Setup(Material m, Color c, bool lightning)
        {
            mat = m; color = c; flicker = lightning;
            mat.EnableKeyword("_EMISSION");
            mat.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
        }

        private void Update()
        {
            if (mat == null) return;
            float t = Time.unscaledTime;
            float k = flicker ? (Random.value > 0.85f ? 2.5f : 0.6f) : 1.1f + 0.5f * Mathf.Sin(t * 7f) + 0.2f * Mathf.Sin(t * 17f);
            mat.SetColor(Emission, color * k);
        }
    }
}
