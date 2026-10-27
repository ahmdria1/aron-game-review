// (c) Ahmed Alamrani. All rights reserved.
using Aron.Core;
using UnityEngine;

namespace Aron.Level
{
    /// <summary>
    /// The checkpoint box's mark and what it leaves behind: a traditional lantern (fanous) on each side of the
    /// box, and once it's broken a real lantern standing in its place, lit with a warm flickering glow.
    /// </summary>
    public static class Lantern
    {
        private static readonly Color Brass = new Color(0.78f, 0.55f, 0.22f);
        private static readonly Color Glass = new Color(1f, 0.72f, 0.32f);
        private static readonly Color Flame = new Color(1f, 0.62f, 0.25f);
        private static readonly Color BoxWood = new Color(0.55f, 0.34f, 0.18f);

        /// <summary>A flat lantern shape on all four sides of a box.</summary>
        public static void Mark(Crate crate)
        {
            var t = crate.transform;
            if (t.Find("LanternMark") != null) return;
            var bounds = new Bounds(t.position + Vector3.up * 0.5f, Vector3.one);
            bool first = true;
            foreach (var r in t.GetComponentsInChildren<Renderer>())
            {
                if (first) { bounds = r.bounds; first = false; } else bounds.Encapsulate(r.bounds);
                var block = new MaterialPropertyBlock();
                r.GetPropertyBlock(block);
                block.SetColor("_BaseColor", BoxWood);
                block.SetColor("_Color", BoxWood);
                r.SetPropertyBlock(block);
            }
            var root = new GameObject("LanternMark").transform;
            root.SetParent(t, false);
            Vector3 c = t.InverseTransformPoint(bounds.center);
            Vector3 s = bounds.size;
            s = new Vector3(s.x / Mathf.Max(0.01f, t.lossyScale.x), s.y / Mathf.Max(0.01f, t.lossyScale.y), s.z / Mathf.Max(0.01f, t.lossyScale.z));
            float h = s.y * 0.62f;
            for (int k = 0; k < 4; k++)
            {
                var face = new GameObject("Side").transform;
                face.SetParent(root, false);
                face.localPosition = c;
                face.localRotation = Quaternion.Euler(0f, k * 90f, 0f);
                float d = (k % 2 == 0 ? s.z : s.x) * 0.5f + 0.012f;
                // Ring on top, pointed cap, glowing body with two bars, a base.
                Part(face, new Vector3(0f, h * 0.46f, d), new Vector3(h * 0.14f, h * 0.1f, 0.02f), Brass, 0f);
                var cap = Part(face, new Vector3(0f, h * 0.33f, d), new Vector3(h * 0.26f, h * 0.26f, 0.02f), Brass, 0f);
                cap.localRotation = Quaternion.Euler(0f, 0f, 45f);
                Part(face, new Vector3(0f, h * 0.02f, d), new Vector3(h * 0.4f, h * 0.48f, 0.02f), Glass, 1.4f);
                Part(face, new Vector3(-h * 0.1f, h * 0.02f, d + 0.006f), new Vector3(h * 0.04f, h * 0.48f, 0.02f), Brass, 0f);
                Part(face, new Vector3(h * 0.1f, h * 0.02f, d + 0.006f), new Vector3(h * 0.04f, h * 0.48f, 0.02f), Brass, 0f);
                Part(face, new Vector3(0f, -h * 0.27f, d), new Vector3(h * 0.5f, h * 0.1f, 0.02f), Brass, 0f);
            }
        }

        /// <summary>A lantern standing on the ground at <paramref name="pos"/>, lit.</summary>
        public static Transform Light(Transform parent, Vector3 pos)
        {
            var root = new GameObject("CheckpointLantern").transform;
            root.SetParent(parent, true);
            root.position = pos;
            Part(root, new Vector3(0f, 0.06f, 0f), new Vector3(0.5f, 0.12f, 0.5f), Brass, 0f);
            Part(root, new Vector3(0f, 0.45f, 0f), new Vector3(0.36f, 0.62f, 0.36f), Glass, 2.2f);
            for (int i = 0; i < 4; i++)
            {
                float a = i * 90f * Mathf.Deg2Rad + Mathf.PI * 0.25f;
                Part(root, new Vector3(Mathf.Cos(a) * 0.19f, 0.45f, Mathf.Sin(a) * 0.19f), new Vector3(0.04f, 0.66f, 0.04f), Brass, 0f);
            }
            var cap = Part(root, new Vector3(0f, 0.86f, 0f), new Vector3(0.3f, 0.3f, 0.3f), Brass, 0f);
            cap.localRotation = Quaternion.Euler(45f, 0f, 45f);
            var ring = PrimitiveFactory.Create(PrimitiveType.Cylinder);
            ring.name = "Ring";
            Object.Destroy(ring.GetComponent<Collider>());
            ring.transform.SetParent(root, false);
            ring.transform.localPosition = new Vector3(0f, 1.08f, 0f);
            ring.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            ring.transform.localScale = new Vector3(0.16f, 0.015f, 0.16f);
            Paint(ring.GetComponent<Renderer>(), Brass, 0f);

            var glow = new GameObject("Glow").AddComponent<UnityEngine.Light>();
            glow.transform.SetParent(root, false);
            glow.transform.localPosition = new Vector3(0f, 0.5f, 0f);
            glow.type = LightType.Point;
            glow.color = Flame;
            glow.range = 6f;
            glow.intensity = 0f;
            glow.shadows = LightShadows.None;
            root.gameObject.AddComponent<LanternFlicker>().Setup(glow);
            return root;
        }

        private static Transform Part(Transform parent, Vector3 pos, Vector3 size, Color c, float glow)
        {
            var go = PrimitiveFactory.Create(PrimitiveType.Cube);
            go.name = glow > 0f ? "Glass" : "Brass";
            Object.Destroy(go.GetComponent<Collider>());
            go.transform.SetParent(parent, false);
            go.transform.localPosition = pos;
            go.transform.localScale = size;
            Paint(go.GetComponent<Renderer>(), c, glow);
            return go.transform;
        }

        private static void Paint(Renderer r, Color c, float glow)
        {
            var m = r.material;
            m.color = c;
            if (glow > 0f) { m.EnableKeyword("_EMISSION"); m.SetColor("_EmissionColor", c * glow); }
            else { m.SetFloat("_Metallic", 0.7f); m.SetFloat("_Smoothness", 0.55f); }
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        }
    }

    /// <summary>Lantern light coming up when it's lit, then flickering like a flame.</summary>
    public class LanternFlicker : MonoBehaviour
    {
        private UnityEngine.Light glow;
        private float born, seed;
        public void Setup(UnityEngine.Light l) { glow = l; born = Time.time; seed = Random.value * 10f; Aron.Core.VisibilityGroup.Attach(this); }

        private void Update()
        {
            if (glow == null) return;
            float rise = Mathf.Clamp01((Time.time - born) / 0.6f);
            float flick = 0.85f + Mathf.PerlinNoise(seed, Time.time * 6f) * 0.3f;
            glow.intensity = 2.4f * rise * flick;
        }
    }

    /// <summary>A checkpoint box breaks just by walking into it.</summary>
    public class TouchBreak : MonoBehaviour
    {
        private Crate crate;
        private void Awake() => crate = GetComponent<Crate>();

        private void Update()
        {
            var p = Player.PlayerController.Active;
            if (p == null || crate == null || !crate.CanBeDamaged) return;
            Vector3 d = p.transform.position - transform.position;
            if (d.y < -0.8f || d.y > 1.6f) return;
            d.y = 0f;
            if (d.sqrMagnitude < 1.1f * 1.1f)
                crate.TakeDamage(new DamageInfo(1, DamageKind.Spin, p.transform.position, p.gameObject));
        }
    }
}
