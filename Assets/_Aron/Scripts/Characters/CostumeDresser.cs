// (c) Ahmed Alamrani. All rights reserved.
using System.Collections.Generic;
using Aron.Core;
using Aron.Logic;
using Aron.Logic.Costumes;
using UnityEngine;

namespace Aron.Characters
{
    /// <summary>
    /// Puts a costume from Resources/Costumes on a character: recolours the model's own cloth, then builds the kit
    /// pieces (caps, turbans, cloaks, belts, daggers, shoes) on the head, hip, arm and foot bones. Meshes are shared
    /// between characters wearing the same piece at the same detail.
    /// </summary>
    public class CostumeDresser : MonoBehaviour
    {
        private static readonly int BaseColor = Shader.PropertyToID("_BaseColor");
        private static readonly int BaseMap = Shader.PropertyToID("_BaseMap");
        private static readonly int Smoothness = Shader.PropertyToID("_Smoothness");
        private static readonly Dictionary<KitPart, Mesh>[] meshCache =
        {
            new Dictionary<KitPart, Mesh>(), new Dictionary<KitPart, Mesh>(), new Dictionary<KitPart, Mesh>(),
        };

        private readonly List<GameObject> pieces = new List<GameObject>();
        private readonly List<Renderer> agalPieces = new List<Renderer>();
        private readonly List<Renderer> wristPieces = new List<Renderer>();
        private Transform wristCoil;
        private float snapUntil;
        private readonly List<Renderer> hidden = new List<Renderer>();
        private readonly List<Object> made = new List<Object>();
        private readonly Dictionary<Renderer, Material[]> originals = new Dictionary<Renderer, Material[]>();
        private Material template;
        private Renderer skirt;

        public CostumeDefinition Current { get; private set; }

        public static CostumeDresser Attach(GameObject model)
        {
            var d = model.GetComponent<CostumeDresser>();
            return d != null ? d : model.AddComponent<CostumeDresser>();
        }

        public void Dress(CostumeDefinition costume) => Dress(costume, DeviceSettings.Tier);

        public void Dress(CostumeDefinition costume, QualityTier tier)
        {
            Undress();
            if (costume == null) return;
            Current = costume;
            Remember();
            foreach (var swap in costume.materials) Recolour(swap.material, Hex(swap.color, Color.white));

            var thobe = Find(transform, "Thobe");
            skirt = thobe != null ? thobe.GetComponent<Renderer>() : null;
            if (skirt != null && CostumeKit.HidesSkirt(costume)) skirt.enabled = false;

            int n = CostumeKit.Segments(tier);
            var clothes = Hex(ClothesColour(costume), Color.white);
            var trim = Hex(TrimColour(costume), new Color(0.79f, 0.64f, 0.27f));
            foreach (var m in costume.meshes)
                foreach (var part in CostumeKit.MeshParts(m.mesh))
                    Place(part, n, Tints(costume, clothes, trim));
            foreach (var a in costume.accessories)
            {
                var main = Hex(a.color, Color.white);
                var tints = Tints(costume, main, string.IsNullOrEmpty(a.pattern) ? trim : Hex(a.pattern, trim));
                foreach (var part in CostumeKit.AccessoryParts(a.kind))
                {
                    // Checked cloth on the headdress itself, plain on its trims.
                    var mat = tints(part.tint);
                    if (!string.IsNullOrEmpty(a.pattern) && part.tint == "main" && IsCheckedCloth(a.kind)) Checks(mat, main, Hex(a.pattern, main));
                    Place(part, n, _ => mat);
                }
            }
            HideBuiltInHeadwear(costume);
            if (costume.hero != "noura" && costume.AgalSocket == "AgalWrist") BuildWristCoil();
            SetAgal(false, false);
        }

        // The prefab carries a plain shemagh and ring for when no costume is loaded. Under a costume that dresses
        // the head it would show through as a second agal.
        private void HideBuiltInHeadwear(CostumeDefinition costume)
        {
            var own = Find(transform, "Shemagh");
            if (own == null) return;
            bool covered = false;
            foreach (var a in costume.accessories)
                foreach (var part in CostumeKit.AccessoryParts(a.kind))
                    if (part.anchor == "head") covered = true;
            if (!covered) return;
            foreach (var r in own.GetComponentsInChildren<Renderer>())
                if (r.enabled) { r.enabled = false; hidden.Add(r); }
        }

        public void Undress()
        {
            foreach (var g in pieces) if (g != null) Destroy(g);
            pieces.Clear();
            agalPieces.Clear();
            wristPieces.Clear();
            wristCoil = null;
            foreach (var kv in originals) if (kv.Key != null) kv.Key.sharedMaterials = kv.Value;
            originals.Clear();
            foreach (var o in made) if (o != null) Destroy(o);
            made.Clear();
            if (skirt != null) skirt.enabled = true;
            foreach (var r in hidden) if (r != null) r.enabled = true;
            hidden.Clear();
            Current = null;
        }

        /// <summary>
        /// Shows the agal in one place only: on the head, coiled on the right wrist, or (while swinging) in his hand.
        /// The wrist coil pops back with a little squash when a swing ends, as if it snapped home.
        /// </summary>
        public void SetAgal(bool weaponIsAgal, bool attacking)
        {
            bool hasAgal = Current != null && Current.Has("agal");
            bool wristOutfit = Current != null && Current.hero != "noura" && Current.AgalSocket == "AgalWrist";
            var spot = CostumeRules.Agal(hasAgal, weaponIsAgal || wristOutfit, attacking);
            foreach (var r in agalPieces) if (r != null) r.enabled = spot == AgalSpot.Head;
            bool onWrist = spot == AgalSpot.Wrist;
            bool wasOnWrist = wristPieces.Count > 0 && wristPieces[0] != null && wristPieces[0].enabled;
            foreach (var r in wristPieces) if (r != null) r.enabled = onWrist;
            if (onWrist && !wasOnWrist) snapUntil = Time.time + 0.18f;
        }

        private void LateUpdate()
        {
            if (wristCoil == null) return;
            float left = snapUntil - Time.time;
            float k = left > 0f ? 1f + 0.35f * Mathf.Sin((1f - left / 0.18f) * Mathf.PI) : 1f;
            wristCoil.localScale = new Vector3(k, 1f / k, k);
        }

        // A coiled agal worn like a bracelet: three thin black rings stacked on the right wrist. A rig that brings
        // its own AgalWrist socket gets it there; otherwise it sits just above the right hand.
        private void BuildWristCoil()
        {
            var socket = Find(transform, "AgalWrist");
            var hand = Find(transform, "Hand.R") ?? Find(transform, "Fist.R") ?? Find(transform, "Wrist.R");
            var elbow = Find(transform, "LowerArm.R");
            Transform parent = socket != null ? socket : hand != null ? hand : elbow;
            if (parent == null) return;
            var root = new GameObject("AgalWristCoil").transform;
            root.SetParent(parent, false);
            if (socket == null && hand != null && elbow != null)
            {
                root.position = Vector3.Lerp(hand.position, elbow.position, 0.18f);
                root.up = (hand.position - elbow.position).normalized;
            }
            float s = HeightScale();
            var colour = Hex(AgalColour(Current), new Color(0.07f, 0.07f, 0.07f));
            var mat = Tinted(colour, 0.25f);
            for (int i = 0; i < 3; i++)
            {
                var ring = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                ring.name = "AgalCoil" + i;
                var col = ring.GetComponent<Collider>();
                if (col != null) Destroy(col);
                ring.transform.SetParent(root, false);
                ring.transform.localPosition = new Vector3(0f, (i - 1) * 0.022f * s, 0f);
                ring.transform.localScale = new Vector3(0.075f * s, 0.008f * s, 0.075f * s);
                var r = ring.GetComponent<Renderer>();
                r.sharedMaterial = mat;
                r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                r.receiveShadows = false;
                wristPieces.Add(r);
            }
            pieces.Add(root.gameObject);
            wristCoil = root;
        }

        private static string AgalColour(CostumeDefinition c)
        {
            foreach (var a in c.accessories) if (a.kind == "agal") return a.color;
            return "#111111";
        }

        private void OnDestroy() => Undress();

        private void Place(KitPart part, int segments, System.Func<string, Material> tint)
        {
            var mesh = SharedMesh(part, segments);
            var mat = tint(part.tint);
            Vector3 up = transform.up, fwd = transform.forward;
            var frame = Quaternion.LookRotation(fwd, up);
            float s = HeightScale();
            switch (part.anchor)
            {
                case "head":
                {
                    var head = Find(transform, "Head");
                    if (head == null) return;
                    var top = Find(transform, "Head_end");
                    float len = top != null ? Mathf.Max(0.12f, Vector3.Distance(head.position, top.position)) : 0.25f * s;
                    Spawn(part, mesh, mat, head, head.position, frame, len);
                    break;
                }
                case "body":
                {
                    var hips = Find(transform, "Hips");
                    Spawn(part, mesh, mat, hips != null ? hips : transform, transform.position, frame, s);
                    break;
                }
                case "foot":
                    foreach (var side in new[] { "Foot.L", "Foot.R" })
                    {
                        var foot = Find(transform, side);
                        if (foot == null) continue;
                        Vector3 ankle = foot.position;
                        Spawn(part, mesh, mat, foot, ankle - up * Vector3.Dot(ankle - transform.position, up), frame, s);
                    }
                    break;
                case "arm":
                    foreach (var side in new[] { ".L", ".R" })
                    {
                        var elbow = Find(transform, "LowerArm" + side);
                        if (elbow == null) continue;
                        var hand = Find(transform, "Fist" + side) ?? Find(transform, "Hand" + side) ?? Find(transform, "Wrist" + side);
                        Vector3 dir = hand != null ? hand.position - elbow.position : -up;
                        if (dir.sqrMagnitude < 1e-6f) dir = -up;
                        Spawn(part, mesh, mat, elbow, elbow.position, Quaternion.FromToRotation(up, dir.normalized) * frame, s);
                    }
                    break;
            }
        }

        private void Spawn(KitPart part, Mesh mesh, Material mat, Transform bone, Vector3 at, Quaternion rot, float scale)
        {
            var go = new GameObject(part.name, typeof(MeshFilter), typeof(MeshRenderer));
            go.transform.SetPositionAndRotation(at, rot);
            go.transform.localScale = Vector3.one * scale;
            go.GetComponent<MeshFilter>().sharedMesh = mesh;
            var r = go.GetComponent<MeshRenderer>();
            r.sharedMaterial = mat;
            r.shadowCastingMode = part.anchor == "body" ? UnityEngine.Rendering.ShadowCastingMode.On : UnityEngine.Rendering.ShadowCastingMode.Off;
            r.receiveShadows = false;
            go.transform.SetParent(bone, true);
            pieces.Add(go);
            if (part.agal) agalPieces.Add(r);
        }

        private static Mesh SharedMesh(KitPart part, int segments)
        {
            var cache = meshCache[segments <= 8 ? 0 : segments <= 12 ? 1 : 2];
            if (cache.TryGetValue(part, out var mesh) && mesh != null) return mesh;
            var d = part.Build(segments);
            var verts = new Vector3[d.VertexCount];
            var uv = new Vector2[d.VertexCount];
            for (int i = 0; i < verts.Length; i++)
            {
                verts[i] = new Vector3(d.verts[i * 3], d.verts[i * 3 + 1], d.verts[i * 3 + 2]);
                uv[i] = new Vector2(d.uvs[i * 2], d.uvs[i * 2 + 1]);
            }
            mesh = new Mesh { name = "Costume_" + part.name };
            mesh.vertices = verts;
            mesh.uv = uv;
            mesh.triangles = d.tris.ToArray();
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            mesh.UploadMeshData(true);
            cache[part] = mesh;
            return mesh;
        }

        private System.Func<string, Material> Tints(CostumeDefinition c, Color main, Color pattern)
        {
            var mats = new Dictionary<string, Material>();
            return tint =>
            {
                if (mats.TryGetValue(tint, out var m)) return m;
                switch (tint)
                {
                    case "pattern": m = Tinted(pattern, 0.35f); break;
                    case "trim": m = Tinted(pattern, 0.6f); break;
                    case "metal": m = Tinted(new Color(0.8f, 0.81f, 0.84f), 0.85f); break;
                    case "leather": m = Tinted(Hex(c.footwearColor, new Color(0.35f, 0.23f, 0.13f)), 0.7f); break;
                    case "sole": m = Tinted(Color.Lerp(Hex(c.footwearColor, Color.black), Color.black, 0.65f), 0.3f); break;
                    default: m = Tinted(main, 0.2f); break;
                }
                mats[tint] = m;
                return m;
            };
        }

        private Material Tinted(Color c, float smooth)
        {
            var m = template != null ? new Material(template) : new Material(Shader.Find("Universal Render Pipeline/Lit"));
            m.name = "Costume";
            if (m.HasProperty(BaseColor)) m.SetColor(BaseColor, c);
            if (m.HasProperty(BaseMap)) m.SetTexture(BaseMap, null);
            if (m.HasProperty(Smoothness)) m.SetFloat(Smoothness, smooth);
            m.color = c;
            made.Add(m);
            return m;
        }

        private void Checks(Material m, Color main, Color line)
        {
            const int n = 32;
            var tex = new Texture2D(n, n, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Repeat, filterMode = FilterMode.Bilinear };
            for (int y = 0; y < n; y++)
                for (int x = 0; x < n; x++)
                {
                    bool cx = (x / 4) % 2 == 0, cy = (y / 4) % 2 == 0;
                    tex.SetPixel(x, y, cx && cy ? line : cx || cy ? Color.Lerp(main, line, 0.4f) : main);
                }
            tex.Apply(false, true);
            made.Add(tex);
            if (m.HasProperty(BaseMap)) m.SetTexture(BaseMap, tex);
            m.mainTexture = tex;
            m.mainTextureScale = new Vector2(6f, 4f);
            if (m.HasProperty(BaseColor)) m.SetColor(BaseColor, Color.white);
            m.color = Color.white;
        }

        private static bool IsCheckedCloth(string kind) =>
            kind == "shemagh" || kind == "keffiyeh" || kind == "yashmagh" || kind == "massar" || kind == "futa";

        private void Remember()
        {
            foreach (var r in GetComponentsInChildren<Renderer>(true))
            {
                originals[r] = r.sharedMaterials;
                if (template == null && r is MeshRenderer && r.sharedMaterial != null && r.sharedMaterial.HasProperty(BaseColor))
                    template = r.sharedMaterial;
            }
        }

        private void Recolour(string part, Color c)
        {
            if (string.IsNullOrEmpty(part)) return;
            foreach (var kv in originals)
            {
                var r = kv.Key;
                if (r == null) continue;
                var mats = r.sharedMaterials;
                bool changed = false;
                for (int i = 0; i < mats.Length; i++)
                {
                    var src = kv.Value[i];
                    if (src == null || src.name.IndexOf(part, System.StringComparison.OrdinalIgnoreCase) < 0) continue;
                    var copy = new Material(src);
                    if (copy.HasProperty(BaseColor)) copy.SetColor(BaseColor, c);
                    copy.color = c;
                    made.Add(copy);
                    mats[i] = copy;
                    changed = true;
                }
                if (changed) r.sharedMaterials = mats;
            }
            if (part == "Clothes")
            {
                var thobe = Find(transform, "Thobe");
                var r = thobe != null ? thobe.GetComponent<Renderer>() : null;
                if (r != null && originals.ContainsKey(r))
                {
                    var m = Tinted(c, 0.2f);
                    r.sharedMaterial = m;
                }
            }
        }

        private static string ClothesColour(CostumeDefinition c)
        {
            foreach (var m in c.materials) if (m.material == "Clothes" || m.material == "Abaya") return m.color;
            return c.materials.Length > 0 ? c.materials[0].color : "#F7F7F2";
        }

        private static string TrimColour(CostumeDefinition c)
        {
            foreach (var m in c.materials) if (m.material == "Trim" || m.material == "Band") return m.color;
            foreach (var a in c.accessories) if (a.kind == "embroidery") return a.color;
            return "";
        }

        private float HeightScale()
        {
            var head = Find(transform, "Head");
            if (head == null) return 1f;
            float h = Vector3.Dot(head.position - transform.position, transform.up);
            return h > 0.5f ? h / 1.55f : 1f;
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
}
