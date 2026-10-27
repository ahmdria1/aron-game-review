// (c) Ahmed Alamrani. All rights reserved.
using UnityEngine;

namespace Aron.Level
{
    /// <summary>
    /// Dresses a wooden box as the golden bisht box: gold bands round the edges, a gold star on each side and a
    /// soft gold light, so it can't be taken for an ordinary crate.
    /// </summary>
    public static class GoldTrim
    {
        private static readonly Color Gold = new Color(0.95f, 0.72f, 0.2f);
        private static readonly Color Wood = new Color(0.42f, 0.26f, 0.14f);

        public static void Apply(Crate crate)
        {
            var t = crate.transform;
            if (t.Find("GoldTrim") != null) return;
            var bounds = new Bounds(t.position + Vector3.up * 0.5f, Vector3.one);
            bool first = true;
            foreach (var r in t.GetComponentsInChildren<Renderer>())
            {
                if (first) { bounds = r.bounds; first = false; } else bounds.Encapsulate(r.bounds);
                // Darker wood than the date boxes.
                var block = new MaterialPropertyBlock();
                r.GetPropertyBlock(block);
                block.SetColor("_BaseColor", Wood);
                block.SetColor("_Color", Wood);
                r.SetPropertyBlock(block);
            }
            var root = new GameObject("GoldTrim").transform;
            root.SetParent(t, false);
            Vector3 c = t.InverseTransformPoint(bounds.center);
            Vector3 s = bounds.size;
            s = new Vector3(s.x / Mathf.Max(0.01f, t.lossyScale.x), s.y / Mathf.Max(0.01f, t.lossyScale.y), s.z / Mathf.Max(0.01f, t.lossyScale.z));
            float b = Mathf.Min(s.x, s.y, s.z) * 0.09f;
            // Twelve edge bands.
            for (int i = -1; i <= 1; i += 2)
                for (int j = -1; j <= 1; j += 2)
                {
                    Bar(root, c + new Vector3(0f, i * s.y * 0.5f, j * s.z * 0.5f), new Vector3(s.x + b, b, b));
                    Bar(root, c + new Vector3(i * s.x * 0.5f, 0f, j * s.z * 0.5f), new Vector3(b, s.y + b, b));
                    Bar(root, c + new Vector3(i * s.x * 0.5f, j * s.y * 0.5f, 0f), new Vector3(b, b, s.z + b));
                }
            // An eight-point star on the four sides: two squares, one turned.
            for (int k = 0; k < 4; k++)
            {
                var face = new GameObject("Star").transform;
                face.SetParent(root, false);
                face.localPosition = c;
                face.localRotation = Quaternion.Euler(0f, k * 90f, 0f);
                float d = (k % 2 == 0 ? s.z : s.x) * 0.5f + 0.01f;
                float w = Mathf.Min(s.x, s.y) * 0.34f;
                Bar(face, new Vector3(0f, 0f, d), new Vector3(w, w, 0.02f));
                var turned = Bar(face, new Vector3(0f, 0f, d + 0.005f), new Vector3(w, w, 0.02f));
                turned.localRotation = Quaternion.Euler(0f, 0f, 45f);
            }
            var light = new GameObject("Shine").AddComponent<Light>();
            light.transform.SetParent(root, false);
            light.transform.localPosition = c + Vector3.up * s.y;
            light.type = LightType.Point;
            light.color = Gold;
            light.range = 3f;
            light.intensity = 1.2f;
            light.shadows = LightShadows.None;
        }

        private static Transform Bar(Transform parent, Vector3 pos, Vector3 size)
        {
            var go = PrimitiveFactory.Create(PrimitiveType.Cube);
            go.name = "Gold";
            Object.Destroy(go.GetComponent<Collider>());
            go.transform.SetParent(parent, false);
            go.transform.localPosition = pos;
            go.transform.localScale = size;
            var r = go.GetComponent<Renderer>();
            var m = r.material;
            m.color = Gold;
            m.EnableKeyword("_EMISSION");
            m.SetColor("_EmissionColor", Gold * 0.6f);
            m.SetFloat("_Metallic", 0.8f);
            m.SetFloat("_Smoothness", 0.7f);
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            return go.transform;
        }
    }
}
