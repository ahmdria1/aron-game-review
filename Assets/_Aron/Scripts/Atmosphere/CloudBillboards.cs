// (c) Ahmed Alamrani. All rights reserved.
using System.Collections.Generic;
using UnityEngine;

namespace Aron.Atmosphere
{
    /// <summary>
    /// High tier: lit cloud puffs (Aron/Cloud) in two or three rings around the player, drifting with the wind and
    /// fading in and out with cloud cover. Lower tiers rely on the painted layer in the sky dome.
    /// </summary>
    public class CloudBillboards : MonoBehaviour
    {
        private static readonly int SunId = Shader.PropertyToID("_SunDir"), OpacityId = Shader.PropertyToID("_Opacity"),
            LitId = Shader.PropertyToID("_LitColor"), ShadeId = Shader.PropertyToID("_ShadeColor"), SeedId = Shader.PropertyToID("_Seed"),
            HazeId = Shader.PropertyToID("_Haze"), HazeColorId = Shader.PropertyToID("_HazeColor");

        private struct Puff { public Transform T; public Vector3 Offset; public float Threshold; public MeshRenderer R; }

        private readonly List<Puff> puffs = new List<Puff>();
        private Material mat;
        private Mesh quad;
        private Vector3 drift;

        public static CloudBillboards Create(Transform parent, int layers)
        {
            var shader = Shader.Find("Aron/Cloud");
            if (shader == null || layers <= 0) return null;
            var go = new GameObject("Clouds");
            go.transform.SetParent(parent, false);
            var c = go.AddComponent<CloudBillboards>();
            c.mat = new Material(shader) { name = "Clouds" };
            c.quad = BuildQuad();
            var rnd = new System.Random(99);
            var block = new MaterialPropertyBlock();
            int perLayer = 7;
            for (int l = 0; l < layers; l++)
                for (int i = 0; i < perLayer; i++)
                {
                    float ang = (i + (float)rnd.NextDouble() * 0.6f) / perLayer * Mathf.PI * 2f;
                    float dist = 95f + l * 30f + (float)rnd.NextDouble() * 25f;
                    float h = 38f + l * 9f + (float)rnd.NextDouble() * 8f;
                    var p = new GameObject("Puff", typeof(MeshFilter), typeof(MeshRenderer));
                    p.transform.SetParent(go.transform, false);
                    float w = 45f + (float)rnd.NextDouble() * 40f;
                    p.transform.localScale = new Vector3(w, w * 0.45f, 1f);
                    p.GetComponent<MeshFilter>().sharedMesh = c.quad;
                    var r = p.GetComponent<MeshRenderer>();
                    r.sharedMaterial = c.mat;
                    r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                    r.receiveShadows = false;
                    block.SetFloat(SeedId, (float)rnd.NextDouble() * 50f);
                    r.SetPropertyBlock(block);
                    // Each puff shows above its own share of cover, so light cloud is a few puffs and overcast is all.
                    c.puffs.Add(new Puff { T = p.transform, Offset = new Vector3(Mathf.Cos(ang) * dist, h, Mathf.Sin(ang) * dist), Threshold = (float)rnd.NextDouble() * 0.85f, R = r });
                }
            return c;
        }

        private static Mesh BuildQuad()
        {
            var m = new Mesh { name = "CloudQuad" };
            m.vertices = new[] { new Vector3(-0.5f, -0.5f, 0), new Vector3(-0.5f, 0.5f, 0), new Vector3(0.5f, 0.5f, 0), new Vector3(0.5f, -0.5f, 0) };
            m.uv = new[] { new Vector2(0, 0), new Vector2(0, 1), new Vector2(1, 1), new Vector2(1, 0) };
            m.triangles = new[] { 0, 1, 2, 0, 2, 3 };
            m.bounds = new Bounds(Vector3.zero, Vector3.one * 2f);
            return m;
        }

        /// <summary>Called by the director at its update rate.</summary>
        public void Apply(float cover, Vector3 sunDir, Color lit, Color shade, float haze, Color hazeColor, Vector3 wind, float dt)
        {
            if (mat == null) return;
            mat.SetVector(SunId, sunDir);
            mat.SetColor(LitId, lit);
            mat.SetColor(ShadeId, shade);
            mat.SetFloat(HazeId, haze);
            mat.SetColor(HazeColorId, hazeColor);
            mat.SetFloat(OpacityId, Mathf.Lerp(0.7f, 0.95f, cover));
            drift += wind * dt * 0.6f;
            var cam = Camera.main;
            var c = cam != null ? cam.transform.position : Vector3.zero;
            foreach (var p in puffs)
            {
                bool on = cover > p.Threshold + 0.08f;
                if (p.R.enabled != on) p.R.enabled = on;
                if (!on) continue;
                // Wrap the drift so puffs stay on their ring around the player.
                var o = p.Offset + drift;
                o.x = Mathf.Repeat(o.x + 160f, 320f) - 160f;
                o.z = Mathf.Repeat(o.z + 160f, 320f) - 160f;
                p.T.position = new Vector3(c.x + o.x, o.y, c.z + o.z);
            }
        }

        private void OnDestroy()
        {
            if (mat != null) Destroy(mat);
            if (quad != null) Destroy(quad);
        }
    }
}
