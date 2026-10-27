// (c) Ahmed Alamrani. All rights reserved.
using System.Collections.Generic;
using Aron.Logic.Sky;
using UnityEngine;

namespace Aron.Stages
{
    /// <summary>
    /// Runtime building blocks for generated stages: coloured primitives, simple lathe meshes, and copies of the
    /// Kenney models the editor leaves in the scene as a kit.
    /// </summary>
    public class StageArt
    {
        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
        private readonly Material baseMaterial;
        private readonly Dictionary<string, GameObject> kit = new Dictionary<string, GameObject>();
        private readonly Dictionary<Color32, Material> plain = new Dictionary<Color32, Material>();
        private readonly Dictionary<(Material, Color32), Material> tinted = new Dictionary<(Material, Color32), Material>();
        private readonly Dictionary<string, Mesh> meshes = new Dictionary<string, Mesh>();
        private readonly Dictionary<PrimitiveType, Mesh> primitives = new Dictionary<PrimitiveType, Mesh>();

        public StageArt(Material baseMaterial, IEnumerable<GameObject> kitPieces)
        {
            this.baseMaterial = baseMaterial;
            if (kitPieces == null) return;
            foreach (var k in kitPieces) if (k != null) kit[k.name] = k;
        }

        public bool Has(string id) => kit.ContainsKey(id);

        public Material Mat(Color c)
        {
            Color32 key = c;
            if (plain.TryGetValue(key, out var m)) return m;
            m = baseMaterial != null ? new Material(baseMaterial) : new Material(Shader.Find("Universal Render Pipeline/Lit"));
            m.name = "Stage_" + ColorUtility.ToHtmlStringRGB(c);
            SetColor(m, c);
            if (m.HasProperty("_Smoothness")) m.SetFloat("_Smoothness", 0.15f);
            plain[key] = m;
            return m;
        }

        private static void SetColor(Material m, Color c)
        {
            if (m.HasProperty(BaseColorId)) m.SetColor(BaseColorId, c);
            m.color = c;
        }

        /// <summary>Recolours materials. With <paramref name="only"/>, just the ones whose name contains it;
        /// <paramref name="replace"/> sets the colour instead of multiplying it.</summary>
        public void Tint(GameObject go, Color c, string only = null, bool replace = false)
        {
            foreach (var r in go.GetComponentsInChildren<Renderer>(true))
            {
                var mats = r.sharedMaterials;
                for (int i = 0; i < mats.Length; i++)
                {
                    var src = mats[i];
                    if (src == null) continue;
                    if (only != null && src.name.IndexOf(only, System.StringComparison.OrdinalIgnoreCase) < 0) continue;
                    Color32 key = replace ? c : c * 0.999f;
                    if (!tinted.TryGetValue((src, key), out var m))
                    {
                        m = new Material(src) { name = src.name };
                        Color old = src.HasProperty(BaseColorId) ? src.GetColor(BaseColorId) : src.color;
                        SetColor(m, replace ? c : old * c);
                        tinted[(src, key)] = m;
                    }
                    mats[i] = m;
                }
                r.sharedMaterials = mats;
            }
        }

        public static Transform Group(string name, Transform parent, Vector3 pos, float yaw = 0f)
        {
            var g = new GameObject(name).transform;
            g.SetParent(parent, false);
            g.localPosition = pos;
            g.localRotation = Quaternion.Euler(0f, yaw, 0f);
            return g;
        }

        private Mesh Primitive(PrimitiveType t)
        {
            if (primitives.TryGetValue(t, out var m)) return m;
            var tmp = PrimitiveFactory.Create(t);
            m = tmp.GetComponent<MeshFilter>().sharedMesh;
            Object.Destroy(tmp);
            primitives[t] = m;
            return m;
        }

        public GameObject Shape(PrimitiveType t, Transform parent, Vector3 centre, Vector3 size, Color c, float yaw = 0f)
            => MeshObject(t.ToString(), Primitive(t), parent, centre, size, c, yaw);

        public GameObject MeshObject(string name, Mesh mesh, Transform parent, Vector3 pos, Vector3 scale, Color c, float yaw = 0f)
        {
            var go = new GameObject(name, typeof(MeshFilter), typeof(MeshRenderer));
            go.transform.SetParent(parent, false);
            go.transform.localPosition = pos;
            go.transform.localRotation = Quaternion.Euler(0f, yaw, 0f);
            go.transform.localScale = scale;
            go.GetComponent<MeshFilter>().sharedMesh = mesh;
            var mr = go.GetComponent<MeshRenderer>();
            mr.sharedMaterial = Mat(c);
            return go;
        }

        /// <summary>Set by the weather system before the stage streams in: the sea or river, and still ponds.</summary>
        public Material WaterMaterial, PondMaterial;
        /// <summary>High tier: water surfaces get a dense grid so the shader can move the vertices.</summary>
        public bool WaterVertexWaves;
        private readonly Dictionary<(int, int), Mesh> grids = new Dictionary<(int, int), Mesh>();

        /// <summary>Every plain and tinted material made so far (wet-ground darkening walks these).</summary>
        public IEnumerable<Material> AllMaterials()
        {
            foreach (var m in plain.Values) yield return m;
            foreach (var m in tinted.Values) yield return m;
            foreach (var m in foliageMats.Values) yield return m;
        }

        /// <summary>
        /// A body of water: the old coloured box underneath, with a water surface laid on its top face when the weather
        /// system has made a material. <paramref name="pond"/> picks the still-water look (fountains, lakes, moats).
        /// </summary>
        public GameObject Water(Transform p, Vector3 centre, Vector3 size, Color fallback, bool pond = false)
        {
            var box = Box(p, centre, size, fallback);
            box.name = "Water";
            var mat = pond ? PondMaterial : WaterMaterial;
            if (mat == null) return box;
            box.GetComponent<Renderer>().sharedMaterial = Mat(fallback * 0.55f);
            int nx = WaterVertexWaves ? Mathf.Clamp(Mathf.CeilToInt(size.x), 2, 24) : 1;
            int nz = WaterVertexWaves ? Mathf.Clamp(Mathf.CeilToInt(size.z), 2, 64) : 1;
            var top = MeshObject("WaterSurface", Grid(nx, nz), p, centre + new Vector3(0f, size.y * 0.5f + 0.01f, 0f), new Vector3(size.x, 1f, size.z), fallback);
            top.GetComponent<Renderer>().sharedMaterial = mat;
            top.GetComponent<Renderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            return box;
        }

        /// <summary>Round pond or fountain basin: a cylinder whose top takes the still-water material.</summary>
        public GameObject WaterDisc(Transform p, Vector3 basePos, float radius, float height, Color fallback)
        {
            var c = Cyl(p, basePos, radius, height, fallback);
            c.name = "Water";
            if (PondMaterial != null) c.GetComponent<Renderer>().sharedMaterial = PondMaterial;
            return c;
        }

        /// <summary>Flat grid in x/z from -0.5 to 0.5 with 0..1 UVs, facing up.</summary>
        private Mesh Grid(int nx, int nz)
        {
            if (grids.TryGetValue((nx, nz), out var cached)) return cached;
            var v = new Vector3[(nx + 1) * (nz + 1)];
            var uv = new Vector2[v.Length];
            var tri = new int[nx * nz * 6];
            for (int z = 0, i = 0; z <= nz; z++)
                for (int x = 0; x <= nx; x++, i++)
                {
                    v[i] = new Vector3((float)x / nx - 0.5f, 0f, (float)z / nz - 0.5f);
                    uv[i] = new Vector2((float)x / nx, (float)z / nz);
                }
            for (int z = 0, t = 0; z < nz; z++)
                for (int x = 0; x < nx; x++)
                {
                    int a = z * (nx + 1) + x, b = a + nx + 1;
                    tri[t++] = a; tri[t++] = b; tri[t++] = a + 1;
                    tri[t++] = a + 1; tri[t++] = b; tri[t++] = b + 1;
                }
            var m = new Mesh { name = "WaterGrid_" + nx + "x" + nz, vertices = v, uv = uv, triangles = tri };
            m.RecalculateNormals();
            // Waves lift the vertices up to a metre or so; keep the bounds generous so it is not culled early.
            m.bounds = new Bounds(Vector3.zero, new Vector3(1f, 4f, 1f));
            grids[(nx, nz)] = m;
            return m;
        }

        // ---------------------------------------------------------------- foliage that moves in the wind

        private Shader foliageShader;
        private bool foliageShaderLooked;
        private readonly Dictionary<Color32, Material> foliageMats = new Dictionary<Color32, Material>();
        private readonly Dictionary<string, Mesh> foliageMeshes = new Dictionary<string, Mesh>();
        private static FoliageWindFile foliageFile;

        /// <summary>True when the wind shader is in the build; builders fall back to the plain material when it is not.</summary>
        public bool HasFoliageShader
        {
            get
            {
                if (!foliageShaderLooked) { foliageShaderLooked = true; foliageShader = Shader.Find("Aron/FoliageWind"); }
                return foliageShader != null;
            }
        }

        /// <summary>One wind material per colour, shared by every tree of that colour (so the SRP Batcher and instancing can group them).</summary>
        public Material FoliageMat(Color c)
        {
            if (!HasFoliageShader) return Mat(c);
            Color32 key = c;
            if (foliageMats.TryGetValue(key, out var m)) return m;
            m = new Material(foliageShader) { name = "Foliage_" + ColorUtility.ToHtmlStringRGB(c), enableInstancing = true };
            m.SetColor("_BaseColor", c);
            m.EnableKeyword("_MASK_VERTEXCOLOR");
            m.SetFloat("_Mask", 1f);
            foliageMats[key] = m;
            return m;
        }

        /// <summary>
        /// A box, ball or cylinder that sways. <paramref name="from"/> and <paramref name="to"/> are the share of the whole tree's
        /// height at the bottom and top of this part (0 root, 1 tip): the sway weight rises trunk to tip across them.
        /// <paramref name="flutter"/> is how much the part flutters (fronds 1, leaf balls 0.3, trunks 0); with
        /// <paramref name="alongZ"/> the flutter grows towards the +z end (a frond's tip), otherwise towards the rim.
        /// </summary>
        public GameObject Foliage(PrimitiveType type, Transform p, Vector3 centre, Vector3 size, Color c, float from, float to, float flutter = 0f, bool alongZ = false, float yaw = 0f)
        {
            if (!HasFoliageShader) return Shape(type, p, centre, size, c, yaw);
            var go = Shape(type, p, centre, size, c, yaw);
            go.GetComponent<MeshFilter>().sharedMesh = FoliageMesh("p" + (int)type, Primitive(type), from, to, flutter, alongZ);
            go.GetComponent<MeshRenderer>().sharedMaterial = FoliageMat(c);
            return go;
        }

        /// <summary>A cylinder standing on <paramref name="basePos"/> that sways (trunks, bamboo); see <see cref="Foliage"/>.</summary>
        public GameObject FoliageCyl(Transform p, Vector3 basePos, float radius, float height, Color c, float from, float to, float flutter = 0f)
            => Foliage(PrimitiveType.Cylinder, p, basePos + Vector3.up * height * 0.5f, new Vector3(radius * 2f, height * 0.5f, radius * 2f), c, from, to, flutter);

        /// <summary>A tapered column or cone that sways (trunks and fir trees); see <see cref="Foliage"/>.</summary>
        public GameObject FoliageFrustum(Transform p, Vector3 basePos, int sides, float r0, float r1, float height, Color c, float from, float to, float flutter = 0f)
        {
            if (!HasFoliageShader) return Frustum(p, basePos, sides, r0, r1, height, c);
            var go = Frustum(p, basePos, sides, r0, r1, height, c);
            string key = sides + "_" + (r1 / Mathf.Max(0.001f, r0)).ToString("0.00");
            go.GetComponent<MeshFilter>().sharedMesh = FoliageMesh("f" + key, meshes[key], from, to, flutter, false);
            go.GetComponent<MeshRenderer>().sharedMaterial = FoliageMat(c);
            return go;
        }

        private Mesh FoliageMesh(string baseKey, Mesh src, float from, float to, float flutter, bool alongZ)
        {
            string key = baseKey + "|" + from.ToString("0.00") + "|" + to.ToString("0.00") + "|" + flutter.ToString("0.00") + "|" + alongZ;
            if (foliageMeshes.TryGetValue(key, out var cached)) return cached;
            if (foliageFile == null) foliageFile = WindFile();
            var m = Object.Instantiate(src);
            m.name = src.name + "_wind";
            var v = m.vertices;
            var colors = new Color[v.Length];
            var b = src.bounds;
            float h = Mathf.Max(0.0001f, b.size.y), rz = Mathf.Max(0.0001f, b.size.z), rr = Mathf.Max(0.0001f, Mathf.Max(b.extents.x, b.extents.z));
            for (int i = 0; i < v.Length; i++)
            {
                float ty = Mathf.Clamp01((v[i].y - b.min.y) / h);
                float weight = FoliageWindMath.Weight(foliageFile, Mathf.Lerp(from, to, ty));
                float edge = alongZ ? Mathf.Clamp01((v[i].z - b.min.z) / rz) : Mathf.Clamp01(new Vector2(v[i].x, v[i].z).magnitude / rr);
                colors[i] = new Color(weight, flutter * edge, 0f, 1f);
            }
            m.colors = colors;
            foliageMeshes[key] = m;
            return m;
        }

        private static FoliageWindFile WindFile()
        {
            var t = Resources.Load<TextAsset>("Config/foliage_wind");
            FoliageWindFile f = null;
            try { f = t != null ? JsonUtility.FromJson<FoliageWindFile>(t.text) : null; } catch (System.Exception) { f = null; }
            return f == null || f.Validate().Count > 0 ? new FoliageWindFile() : f;
        }

        /// <summary>Box with its centre at <paramref name="centre"/>.</summary>
        public GameObject Box(Transform p, Vector3 centre, Vector3 size, Color c, float yaw = 0f) => Shape(PrimitiveType.Cube, p, centre, size, c, yaw);

        /// <summary>Cylinder standing on <paramref name="basePos"/>.</summary>
        public GameObject Cyl(Transform p, Vector3 basePos, float radius, float height, Color c)
            => Shape(PrimitiveType.Cylinder, p, basePos + Vector3.up * height * 0.5f, new Vector3(radius * 2f, height * 0.5f, radius * 2f), c);

        public GameObject Ball(Transform p, Vector3 centre, Vector3 size, Color c) => Shape(PrimitiveType.Sphere, p, centre, size, c);

        /// <summary>Tapered n-sided column standing on <paramref name="basePos"/>: cones, pyramids, minarets, needles.</summary>
        public GameObject Frustum(Transform p, Vector3 basePos, int sides, float r0, float r1, float height, Color c, float yaw = 0f)
        {
            string key = sides + "_" + (r1 / Mathf.Max(0.001f, r0)).ToString("0.00");
            if (!meshes.TryGetValue(key, out var mesh)) meshes[key] = mesh = BuildFrustum(sides, 1f, r1 / Mathf.Max(0.001f, r0), 1f);
            return MeshObject("Frustum", mesh, p, basePos, new Vector3(r0, height, r0), c, yaw);
        }

        private static Mesh BuildFrustum(int sides, float r0, float r1, float h)
        {
            var v = new List<Vector3>();
            var t = new List<int>();
            for (int i = 0; i < sides; i++)
            {
                float a0 = i * Mathf.PI * 2f / sides, a1 = (i + 1) * Mathf.PI * 2f / sides;
                Vector3 b0 = new Vector3(Mathf.Cos(a0) * r0, 0f, Mathf.Sin(a0) * r0), b1 = new Vector3(Mathf.Cos(a1) * r0, 0f, Mathf.Sin(a1) * r0);
                Vector3 u0 = new Vector3(Mathf.Cos(a0) * r1, h, Mathf.Sin(a0) * r1), u1 = new Vector3(Mathf.Cos(a1) * r1, h, Mathf.Sin(a1) * r1);
                int s = v.Count;
                v.Add(b0); v.Add(b1); v.Add(u1); v.Add(u0);
                t.Add(s); t.Add(s + 2); t.Add(s + 1); t.Add(s); t.Add(s + 3); t.Add(s + 2);
                if (r1 > 0.001f) { s = v.Count; v.Add(new Vector3(0f, h, 0f)); v.Add(u0); v.Add(u1); t.Add(s); t.Add(s + 1); t.Add(s + 2); }
                s = v.Count; v.Add(Vector3.zero); v.Add(b1); v.Add(b0); t.Add(s); t.Add(s + 1); t.Add(s + 2);
            }
            var m = new Mesh { name = "Frustum" + sides };
            m.SetVertices(v);
            m.SetTriangles(t, 0);
            m.RecalculateNormals();
            m.RecalculateBounds();
            return m;
        }

        /// <summary>Copy of a kit model. Scale multiplies the kit piece's own scale, which is one metre per native unit.</summary>
        public GameObject Kit(string id, Transform parent, Vector3 pos, float yaw = 0f, float scale = 1f) => Kit(id, parent, pos, yaw, Vector3.one * scale);

        public GameObject Kit(string id, Transform parent, Vector3 pos, float yaw, Vector3 scale)
        {
            if (!kit.TryGetValue(id, out var tpl)) return null;
            var go = Object.Instantiate(tpl, parent, false);
            go.name = id;
            go.transform.localPosition = pos;
            go.transform.localRotation = Quaternion.Euler(0f, yaw, 0f);
            go.transform.localScale = Vector3.Scale(tpl.transform.localScale, scale);
            go.SetActive(true);
            return go;
        }

        public static Bounds BoundsOf(GameObject go)
        {
            var rs = go.GetComponentsInChildren<Renderer>();
            if (rs.Length == 0) return new Bounds(go.transform.position, Vector3.one);
            var b = rs[0].bounds;
            foreach (var r in rs) b.Encapsulate(r.bounds);
            return b;
        }
    }
}
