// (c) Ahmed Alamrani. All rights reserved.
using System.Collections.Generic;
using Aron.Animals;
using Aron.Config;
using Aron.Logic.GameFeel;
using Aron.Logic.Presentation;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace Aron.Fx
{
    /// <summary>
    /// The soft round shadow under a character: Aron, Noura, companions, mounts, enemies, bosses and crates. One raycast straight down per character per frame
    /// finds the ground (platforms, slopes, moving platforms), the disc lies on that surface, shrinks and fades with height, and is hidden over a pit.
    /// It is a cheap quad with a soft texture, so it works the same on every quality tier. It does not follow the reduce-motion option (it is a landing aid, not motion).
    /// The numbers are in game_feel.json (group Shadow), the maths is <see cref="DropShadowMath"/> and is unit-tested.
    /// </summary>
    public class DropShadow : MonoBehaviour
    {
        private static readonly List<DropShadow> All = new List<DropShadow>(32);

        /// <summary>The assist mode switch: a bigger, stronger landing shadow.</summary>
        public static bool AssistBoost;

        private float radius, footOffset;
        private bool autoSize;
        private bool priority;
        private Rider rider;
        private Transform quad;
        private MeshRenderer quadRenderer;

        /// <summary>
        /// Gives <paramref name="go"/> a shadow. <paramref name="radius"/> is the disc radius in metres, or 0 to size it from the model's mesh bounds on the first frame.
        /// <paramref name="footOffset"/> is the height of the pivot above the feet (0 when the pivot is at the feet). Safe to call twice.
        /// </summary>
        public static DropShadow Ensure(GameObject go, float radius = 0f, float footOffset = 0f, bool priority = false)
        {
            if (go == null) return null;
            var s = go.GetComponent<DropShadow>();
            if (s == null) s = go.AddComponent<DropShadow>();
            s.radius = radius; s.footOffset = footOffset; s.autoSize = radius <= 0f; s.priority = priority;
            return s;
        }

        private void OnEnable()
        {
            rider = GetComponent<Rider>();
            if (priority) All.Insert(0, this); else All.Add(this);
            Director.Touch();
        }

        private void OnDisable()
        {
            All.Remove(this);
            if (quad != null) quad.gameObject.SetActive(false);
        }

        private void OnDestroy()
        {
            if (quad != null) Destroy(quad.gameObject);
        }

        private void AutoSize()
        {
            autoSize = false;
            bool any = false; Bounds b = default;
            foreach (var r in GetComponentsInChildren<Renderer>())
            {
                if (!(r is MeshRenderer) && !(r is SkinnedMeshRenderer)) continue;
                if (r.GetComponentInParent<DropShadow>() != this) continue;
                if (!any) { b = r.bounds; any = true; } else b.Encapsulate(r.bounds);
            }
            if (!any) { radius = 0.5f; return; }
            radius = Mathf.Clamp(Mathf.Max(b.extents.x, b.extents.z) * 0.85f, 0.35f, 6f);
            if (footOffset == 0f) footOffset = Mathf.Clamp(b.min.y - transform.position.y, -0.2f, 1.5f);
        }

        private void Tick(bool realShadows, FeelValues v, ref RaycastHit[] hits)
        {
            if (autoSize) AutoSize();
            bool mounted = rider != null && rider.IsMounted;   // the mount carries the shadow
            Vector3 feet = transform.position + Vector3.up * footOffset;
            Vector3 origin = feet + Vector3.up * DropShadowMath.RayLift;
            float reach = DropShadowMath.RayLift + v.shadowMaxDistance;
            bool found = false; float best = float.MaxValue; Vector3 point = default, normal = Vector3.up;
            if (!mounted)
            {
                int n = Physics.RaycastNonAlloc(origin, Vector3.down, hits, reach, ~0, QueryTriggerInteraction.Ignore);
                for (int i = 0; i < n; i++)
                {
                    var h = hits[i];
                    if (h.distance >= best || h.collider.transform.IsChildOf(transform)) continue;
                    if (h.collider.GetComponentInParent<DropShadow>() != null) continue;   // never "land" on another character
                    best = h.distance; point = h.point; normal = h.normal; found = true;
                }
            }
            var look = DropShadowMath.Evaluate(v, found, found ? DropShadowMath.HeightFromHit(best) : 0f, realShadows, AssistBoost);
            if (!look.Visible) { if (quad != null && quad.gameObject.activeSelf) quad.gameObject.SetActive(false); return; }
            if (quad == null) Director.Build(this, out quad, out quadRenderer);
            if (!quad.gameObject.activeSelf) quad.gameObject.SetActive(true);
            quad.SetPositionAndRotation(point + normal * 0.03f, Quaternion.FromToRotation(Vector3.up, normal));
            float d = radius * look.Scale * 2f;
            quad.localScale = new Vector3(d, 1f, d);
            Director.SetAlpha(quadRenderer, look.Alpha);
        }

        // ---------------------------------------------------------------------------------------------------------

        /// <summary>One hidden object that updates every shadow after everything has moved, shares the mesh, texture and material.</summary>
        private sealed class Director : MonoBehaviour
        {
            private static Director instance;
            private static Mesh mesh;
            private static Material material;
            private static MaterialPropertyBlock block;
            private static readonly int ColorId = Shader.PropertyToID("_Color");
            private RaycastHit[] hits = new RaycastHit[8];
            private bool realShadows;
            private int nextCheck;

            [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
            private static void ResetStatics() { instance = null; mesh = null; material = null; block = null; All.Clear(); AssistBoost = false; }

            public static void Touch()
            {
                if (instance != null) return;
                var go = new GameObject("DropShadows");
                DontDestroyOnLoad(go);
                instance = go.AddComponent<Director>();
            }

            public static void Build(DropShadow owner, out Transform quad, out MeshRenderer mr)
            {
                if (mesh == null) mesh = MakeMesh();
                if (material == null) material = MakeMaterial();
                if (block == null) block = new MaterialPropertyBlock();
                var go = new GameObject("Shadow_" + owner.name);
                go.transform.SetParent(instance.transform, false);
                go.AddComponent<MeshFilter>().sharedMesh = mesh;
                mr = go.AddComponent<MeshRenderer>();
                mr.sharedMaterial = material;
                mr.shadowCastingMode = ShadowCastingMode.Off;
                mr.receiveShadows = false;
                mr.lightProbeUsage = LightProbeUsage.Off;
                mr.reflectionProbeUsage = ReflectionProbeUsage.Off;
                quad = go.transform;
            }

            public static void SetAlpha(MeshRenderer mr, float a)
            {
                block.SetColor(ColorId, new Color(0f, 0f, 0f, a));
                mr.SetPropertyBlock(block);
            }

            private static Mesh MakeMesh()
            {
                var m = new Mesh { name = "ContactShadowQuad" };
                m.vertices = new[] { new Vector3(-0.5f, 0f, -0.5f), new Vector3(0.5f, 0f, -0.5f), new Vector3(0.5f, 0f, 0.5f), new Vector3(-0.5f, 0f, 0.5f) };
                m.uv = new[] { new Vector2(0, 0), new Vector2(1, 0), new Vector2(1, 1), new Vector2(0, 1) };
                m.triangles = new[] { 0, 2, 1, 0, 3, 2 };
                m.normals = new[] { Vector3.up, Vector3.up, Vector3.up, Vector3.up };
                m.bounds = new Bounds(Vector3.zero, new Vector3(1f, 0.1f, 1f));
                return m;
            }

            private static Material MakeMaterial()
            {
                const int N = 64;
                var tex = new Texture2D(N, N, TextureFormat.RGBA32, false) { name = "ContactShadowDisc", wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Bilinear };
                var px = new Color32[N * N];
                for (int y = 0; y < N; y++)
                    for (int x = 0; x < N; x++)
                    {
                        float dx = (x + 0.5f) / N * 2f - 1f, dy = (y + 0.5f) / N * 2f - 1f;
                        float r = Mathf.Sqrt(dx * dx + dy * dy);
                        float a = Mathf.Clamp01((1f - r) / 0.55f);   // solid centre, soft edge
                        a = a * a * (3f - 2f * a);
                        px[y * N + x] = new Color32(0, 0, 0, (byte)Mathf.RoundToInt(a * 255f));
                    }
                tex.SetPixels32(px); tex.Apply(false, true);
                var shader = Shader.Find("Sprites/Default");
                var m = new Material(shader) { name = "ContactShadow", mainTexture = tex };
                return m;
            }

            private void LateUpdate()
            {
                if (All.Count == 0) return;
                if (nextCheck-- <= 0) { nextCheck = 45; realShadows = RealShadowsOn(); }
                var v = FeelSettings.V;
                int n = Mathf.Min(All.Count, DropShadowMath.MaxCasters);
                for (int i = 0; i < n; i++) All[i].Tick(realShadows, v, ref hits);
                // the ones past the limit stay hidden
                for (int i = n; i < All.Count; i++) All[i].HideNow();
            }

            private static bool RealShadowsOn()
            {
                var urp = GraphicsSettings.currentRenderPipeline as UniversalRenderPipelineAsset;
                return urp != null && urp.shadowDistance > 1f;   // the Low tier sets the distance to 0
            }
        }

        private void HideNow() { if (quad != null && quad.gameObject.activeSelf) quad.gameObject.SetActive(false); }
    }
}
