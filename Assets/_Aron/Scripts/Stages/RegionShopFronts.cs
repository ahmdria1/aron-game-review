// (c) Ahmed Alamrani. All rights reserved.
using Aron.Core;
using Aron.Localization;
using Aron.Logic.Regions;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Aron.Stages
{
    /// <summary>Puts the region's local shops along the street when a stage loads.</summary>
    public static class RegionShopFronts
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Hook()
        {
            SceneManager.sceneLoaded -= OnLoaded;
            SceneManager.sceneLoaded += OnLoaded;
            OnLoaded(SceneManager.GetActiveScene(), LoadSceneMode.Single);
        }

        private static void OnLoaded(Scene scene, LoadSceneMode mode)
        {
            if (scene.name != SceneNames.Stage || !StageSession.Active || StageSession.Stage.boss) return;
            var ta = Resources.Load<TextAsset>("RegionShops/" + StageSession.Region.id);
            if (ta == null) return;
            var file = JsonUtility.FromJson<RegionShopFile>(ta.text);
            var here = file?.On(StageSession.Stage.id);
            if (here == null || here.Count == 0) return;

            var layout = StageLayout.Generate(StageSession.Stage);
            var root = new GameObject("RegionShops").transform;
            for (int i = 0; i < here.Count; i++)
            {
                float z = Mathf.Lerp(18f, layout.Length - 20f, (i + 1f) / (here.Count + 1f));
                float y = layout.GroundAt(z);
                if (float.IsNaN(y)) y = layout.GroundAt(z + 3f);
                if (float.IsNaN(y)) continue;
                float side = i % 2 == 0 ? -1f : 1f;
                Build(root, here[i], new Vector3(side * (layout.HalfWidth + 2.2f), y, z), side);
            }
        }

        private static void Build(Transform parent, RegionShop shop, Vector3 pos, float side)
        {
            var go = new GameObject("Shop_" + shop.kind);
            go.transform.SetParent(parent, false);
            go.transform.position = pos;
            go.transform.rotation = Quaternion.Euler(0f, side < 0f ? 90f : -90f, 0f);

            Color wall = new Color(0.93f, 0.86f, 0.72f);
            Color awning = Awning(shop.kind);
            Box(go.transform, "Walls", new Vector3(0f, 1.6f, 0.8f), new Vector3(4f, 3.2f, 2f), wall);
            Box(go.transform, "Door", new Vector3(0f, 1.1f, -0.22f), new Vector3(1.2f, 2.2f, 0.06f), new Color(0.35f, 0.22f, 0.12f));
            var aw = Box(go.transform, "Awning", new Vector3(0f, 2.55f, -0.6f), new Vector3(4.2f, 0.12f, 1.3f), awning);
            aw.transform.localRotation = Quaternion.Euler(-12f, 0f, 0f);
            Box(go.transform, "SignBoard", new Vector3(0f, 3.05f, -0.25f), new Vector3(3.6f, 0.75f, 0.1f), new Color(0.12f, 0.2f, 0.16f));
            Box(go.transform, "Goods", new Vector3(1.3f, 0.35f, -0.7f), new Vector3(0.9f, 0.7f, 0.7f), awning * 0.8f);

            var t = new GameObject("SignText");
            t.transform.SetParent(go.transform, false);
            t.transform.localPosition = new Vector3(0f, 3.05f, -0.32f);
            var tmp = t.AddComponent<TextMeshPro>();
            tmp.fontSize = 4.5f;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = new Color(0.98f, 0.92f, 0.75f);
            tmp.rectTransform.sizeDelta = new Vector2(3.4f, 0.7f);
            tmp.enableAutoSizing = true;
            tmp.fontSizeMin = 1.5f;
            tmp.fontSizeMax = 4.5f;
            ArabicTMP.SetText(tmp, shop.nameAr);
        }

        private static GameObject Box(Transform parent, string name, Vector3 local, Vector3 size, Color c)
        {
            var b = PrimitiveFactory.Create(PrimitiveType.Cube);
            b.name = name;
            Object.Destroy(b.GetComponent<Collider>());
            b.transform.SetParent(parent, false);
            b.transform.localPosition = local;
            b.transform.localScale = size;
            var r = b.GetComponent<Renderer>();
            r.material.color = c;
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            return b;
        }

        private static Color Awning(string kind)
        {
            switch (kind)
            {
                case "grocery": return new Color(0.15f, 0.55f, 0.3f);
                case "fish": return new Color(0.2f, 0.45f, 0.75f);
                case "gold": case "silver": return new Color(0.85f, 0.68f, 0.2f);
                case "sweets": case "bakery": return new Color(0.85f, 0.45f, 0.3f);
                case "spices": case "coffee": return new Color(0.6f, 0.3f, 0.12f);
                case "perfume": case "frankincense": return new Color(0.5f, 0.3f, 0.55f);
                default: return new Color(0.7f, 0.15f, 0.18f);
            }
        }
    }
}
