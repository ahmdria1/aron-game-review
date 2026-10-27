// (c) Ahmed Alamrani. All rights reserved.
using System.Collections.Generic;
using Aron.Logic.Regions;
using Aron.Stages;
using UnityEngine;

namespace Aron.Atmosphere
{
    /// <summary>
    /// Wet ground: every stage material darkens and (medium tier and up) turns glossy as the ground wets, then
    /// dries back over a few minutes. Puddles (Aron/Puddle) grow in on the track when the weather system brings rain.
    /// </summary>
    public class WetSurfaces : MonoBehaviour
    {
        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor"), SmoothId = Shader.PropertyToID("_Smoothness"),
            WetId = Shader.PropertyToID("_Wet"), RainId = Shader.PropertyToID("_Rain"), SeedId = Shader.PropertyToID("_Seed");

        private struct Dry { public Color Color; public float Smooth; public bool HasSmooth; }

        private readonly Dictionary<Material, Dry> dry = new Dictionary<Material, Dry>();
        private readonly List<Renderer> puddles = new List<Renderer>();
        private StageArt art;
        private StageBuilder builder;
        private bool gloss;
        private int puddleCount;
        private Material puddleMat;
        private float applied = -1f;

        public void Init(StageBuilder b, StageArt a, bool wetSpecular, int maxPuddles)
        {
            builder = b; art = a; gloss = wetSpecular; puddleCount = maxPuddles;
        }

        /// <summary>Called at the director's rate. <paramref name="puddlesWanted"/> is false when the stage already
        /// has its own authored puddles.</summary>
        public void Apply(float wet, float rain, bool puddlesWanted)
        {
            if (art == null) return;
            if (Mathf.Abs(wet - applied) > 0.01f || (wet > 0f && applied < 0f))
            {
                applied = wet;
                foreach (var m in art.AllMaterials())
                {
                    if (m == null) continue;
                    if (!dry.TryGetValue(m, out var d))
                    {
                        bool hasColor = m.HasProperty(BaseColorId);
                        d = new Dry
                        {
                            Color = hasColor ? m.GetColor(BaseColorId) : m.color,
                            HasSmooth = m.HasProperty(SmoothId),
                        };
                        d.Smooth = d.HasSmooth ? m.GetFloat(SmoothId) : 0f;
                        dry[m] = d;
                    }
                    // Wet stone and sand go about a third darker; the shine comes from the raised smoothness.
                    var c = d.Color * (1f - 0.32f * wet);
                    c.a = d.Color.a;
                    if (m.HasProperty(BaseColorId)) m.SetColor(BaseColorId, c);
                    if (gloss && d.HasSmooth) m.SetFloat(SmoothId, Mathf.Lerp(d.Smooth, 0.75f, wet));
                }
            }
            if (puddlesWanted && puddleCount > 0 && wet > 0.05f && puddles.Count == 0) MakePuddles();
            if (puddleMat != null)
            {
                puddleMat.SetFloat(WetId, wet);
                puddleMat.SetFloat(RainId, rain);
                bool show = wet > 0.02f;
                foreach (var r in puddles) if (r != null && r.enabled != show) r.enabled = show;
            }
        }

        private void MakePuddles()
        {
            var shader = Shader.Find("Aron/Puddle");
            if (shader == null || builder == null || builder.Layout == null) { puddleCount = 0; return; }
            puddleMat = new Material(shader) { name = "Puddles" };
            var layout = builder.Layout;
            var rnd = new System.Random(builder.Stage != null ? builder.Stage.Seed + 7 : 7);
            var block = new MaterialPropertyBlock();
            var quad = Quad();
            foreach (var g in layout.Of(PieceKind.Ground))
            {
                for (float z = g.Start + 4f; z < g.End - 3f && puddles.Count < puddleCount; z += 9f + (float)rnd.NextDouble() * 9f)
                {
                    float x = ((float)rnd.NextDouble() * 2f - 1f) * (layout.HalfWidth - 1f);
                    float r = 0.8f + (float)rnd.NextDouble() * 1.1f;
                    var go = new GameObject("Puddle", typeof(MeshFilter), typeof(MeshRenderer));
                    go.transform.SetParent(builder.World, false);
                    go.transform.localPosition = new Vector3(x, g.y + 0.015f, z);
                    go.transform.localScale = new Vector3(r * 2f, 1f, r * 2.6f);
                    go.GetComponent<MeshFilter>().sharedMesh = quad;
                    var mr = go.GetComponent<MeshRenderer>();
                    mr.sharedMaterial = puddleMat;
                    mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                    block.SetFloat(SeedId, (float)rnd.NextDouble() * 30f);
                    mr.SetPropertyBlock(block);
                    puddles.Add(mr);
                }
                if (puddles.Count >= puddleCount) break;
            }
        }

        private static Mesh Quad()
        {
            var m = new Mesh { name = "PuddleQuad" };
            m.vertices = new[] { new Vector3(-0.5f, 0, -0.5f), new Vector3(-0.5f, 0, 0.5f), new Vector3(0.5f, 0, 0.5f), new Vector3(0.5f, 0, -0.5f) };
            m.uv = new[] { new Vector2(0, 0), new Vector2(0, 1), new Vector2(1, 1), new Vector2(1, 0) };
            m.normals = new[] { Vector3.up, Vector3.up, Vector3.up, Vector3.up };
            m.triangles = new[] { 0, 1, 2, 0, 2, 3 };
            return m;
        }

        private void OnDestroy()
        {
            // Materials are shared with the stage art and outlive nothing, but put them back so the editor's
            // play-mode copies do not stay dark.
            foreach (var kv in dry)
            {
                if (kv.Key == null) continue;
                if (kv.Key.HasProperty(BaseColorId)) kv.Key.SetColor(BaseColorId, kv.Value.Color);
                if (kv.Value.HasSmooth) kv.Key.SetFloat(SmoothId, kv.Value.Smooth);
            }
            if (puddleMat != null) Destroy(puddleMat);
        }
    }
}
