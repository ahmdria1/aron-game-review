// (c) Ahmed Alamrani. All rights reserved.
using Aron.Level;
using TMPro;
using UnityEngine;

namespace Aron.EditorTools
{
    /// <summary>Street dressing for Riyadh: shop fronts with their goods, street name signs and souq gates.</summary>
    public static partial class AronLevelBuilder
    {
        private enum ShopKind { Shawarma, Grocery, Oud, Qahwa, Dates, Spices }

        private static readonly Color StreetSignBlue = new Color(0.07f, 0.27f, 0.55f);

        private static void BuildStreetLife(Transform world, Material signMat)
        {
            var street = Group("Street", world).transform;

            // Real places along the old centre; the shops themselves are made up.
            StreetSign(street, new Vector3(-5.3f, 0f, 5f), "الدرعية - حي الطريف", "Diriyah - At-Turaif");
            StreetSign(street, new Vector3(5.3f, 0f, 38f), "حي الديرة", "Al Dirah");
            StreetSign(street, new Vector3(-5.3f, 0f, 57f), "شارع الثميري", "Al Thumairi St.");
            StreetSign(street, new Vector3(5.3f, 0f, 112f), "طريق الملك فهد", "King Fahd Rd.");
            SouqGate(street, 32f, "سوق طيبة", "Souq Taibah");
            SouqGate(street, 88f, "سوق الزل", "Souq Al Zal");

            DressShop(MakeShopSign(world, "shop_6", new Vector3(-6.5f, 0f, 40f), signMat), ShopKind.Spices);
            DressShop(MakeShopSign(world, "shop_5", new Vector3(6.5f, 0f, 92f), signMat), ShopKind.Dates);
            BuildRealShops(world);
        }

        private static TextMeshPro WorldText(Transform parent, string name, Vector3 localPos, float size, Vector2 box, Color color)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPos;
            var tmp = go.AddComponent<TextMeshPro>();
            tmp.fontSize = size;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = color;
            tmp.rectTransform.sizeDelta = box;
            return tmp;
        }

        private static void ArabicSign(Transform parent, Vector3 localPos, string ar, float size, Vector2 box, Color color)
        {
            var tmp = WorldText(parent, "TextAr", localPos, size, box, color);
            tmp.text = ar;
            Set(tmp.gameObject.AddComponent<ArabicLabel>(), "text", ar);
        }

        private static void StreetSign(Transform parent, Vector3 pos, string ar, string en)
        {
            var sign = Group("StreetSign", parent, pos).transform;
            sign.localRotation = Quaternion.Euler(0f, pos.x < 0f ? -20f : 20f, 0f);
            var pole = Mat("SignPole", new Color(0.55f, 0.57f, 0.6f));
            Block("Pole", sign, new Vector3(0f, 1.5f, 0f), new Vector3(0.1f, 1.5f, 0.1f), pole, PrimitiveType.Cylinder, false);
            Block("Frame", sign, new Vector3(0f, 3.2f, 0f), new Vector3(2.5f, 0.95f, 0.06f), Mat("SignWhite", new Color(0.95f, 0.95f, 0.95f)), PrimitiveType.Cube, false);
            Block("Board", sign, new Vector3(0f, 3.2f, -0.01f), new Vector3(2.38f, 0.83f, 0.06f), Mat("StreetSign", StreetSignBlue), PrimitiveType.Cube, false);
            ArabicSign(sign, new Vector3(0f, 3.36f, -0.06f), ar, 3.2f, new Vector2(2.3f, 0.5f), Color.white);
            WorldText(sign, "TextEn", new Vector3(0f, 2.97f, -0.06f), 1.8f, new Vector2(2.3f, 0.3f), Color.white).text = en;
        }

        private static void SouqGate(Transform parent, float z, string ar, string en)
        {
            var gate = Group("SouqGate", parent, new Vector3(0f, 0f, z)).transform;
            var mud = Mat("GateMud", MudColor);
            var trim = Mat("GateTrim", MudDark);
            foreach (float x in new[] { -5.3f, 5.3f })
            {
                Block("Pillar", gate, new Vector3(x, 3f, 0f), new Vector3(0.9f, 6f, 0.9f), mud, PrimitiveType.Cube, false);
                for (int i = 0; i < 2; i++)
                    Block("Merlon", gate, new Vector3(x + (i == 0 ? -0.25f : 0.25f), 6.2f, 0f), new Vector3(0.3f, 0.4f, 0.9f), trim, PrimitiveType.Cube, false);
            }
            Block("Beam", gate, new Vector3(0f, 5.4f, 0f), new Vector3(11.5f, 1.2f, 0.7f), mud, PrimitiveType.Cube, false);
            for (float x = -5f; x <= 5.01f; x += 1f)
                Block("Notch", gate, new Vector3(x, 6.2f, 0f), new Vector3(0.45f, 0.4f, 0.7f), trim, PrimitiveType.Cube, false);
            Block("Panel", gate, new Vector3(0f, 5.4f, -0.37f), new Vector3(4.4f, 1f, 0.05f), Mat("SouqPanel", new Color(0.18f, 0.32f, 0.2f)), PrimitiveType.Cube, false);
            ArabicSign(gate, new Vector3(0f, 5.52f, -0.42f), ar, 5f, new Vector2(4.2f, 0.7f), new Color(1f, 0.9f, 0.6f));
            WorldText(gate, "TextEn", new Vector3(0f, 5.06f, -0.42f), 2f, new Vector2(4.2f, 0.3f), new Color(1f, 0.9f, 0.6f)).text = en;
        }

        private static void DressShop(GameObject shop, ShopKind kind)
        {
            var t = shop.transform;
            var goods = Group("Goods", t).transform;
            var wood = Mat("ShopWood", new Color(0.45f, 0.3f, 0.18f));
            Awning(t, kind);
            switch (kind)
            {
                case ShopKind.Shawarma:
                {
                    var steel = Mat("ShopSteel", new Color(0.7f, 0.72f, 0.75f));
                    Block("Counter", goods, new Vector3(0f, 0.45f, -1.5f), new Vector3(1.8f, 0.9f, 0.55f), steel, PrimitiveType.Cube, false);
                    Block("Spit", goods, new Vector3(-0.45f, 1.45f, -1.5f), new Vector3(0.05f, 0.55f, 0.05f), steel, PrimitiveType.Cylinder, false);
                    var meat = Mat("ShawarmaMeat", new Color(0.6f, 0.33f, 0.16f));
                    Block("Meat", goods, new Vector3(-0.45f, 1.4f, -1.5f), new Vector3(0.36f, 0.38f, 0.36f), meat, PrimitiveType.Cylinder, false);
                    Block("MeatTop", goods, new Vector3(-0.45f, 1.8f, -1.5f), new Vector3(0.3f, 0.12f, 0.3f), meat, PrimitiveType.Sphere, false);
                    Block("Grill", goods, new Vector3(-0.45f, 1.4f, -1.25f), new Vector3(0.5f, 0.75f, 0.04f), Mat("GrillGlow", new Color(1f, 0.45f, 0.15f)), PrimitiveType.Cube, false);
                    var bread = Mat("Bread", new Color(0.93f, 0.82f, 0.6f));
                    for (int i = 0; i < 5; i++)
                        Block("Bread", goods, new Vector3(0.45f, 0.92f + i * 0.025f, -1.5f), new Vector3(0.38f, 0.012f, 0.38f), bread, PrimitiveType.Cylinder, false);
                    break;
                }
                case ShopKind.Grocery:
                {
                    Block("Stand", goods, new Vector3(0f, 0.3f, -1.5f), new Vector3(2f, 0.6f, 0.6f), wood, PrimitiveType.Cube, false);
                    Block("Stand2", goods, new Vector3(0f, 0.75f, -1.3f), new Vector3(2f, 0.3f, 0.3f), wood, PrimitiveType.Cube, false);
                    Color[] packs = { new Color(0.9f, 0.2f, 0.2f), new Color(0.95f, 0.8f, 0.2f), new Color(0.2f, 0.5f, 0.9f), new Color(0.3f, 0.75f, 0.35f), new Color(0.95f, 0.55f, 0.2f) };
                    for (int i = 0; i < 9; i++)
                    {
                        float x = -0.85f + i * 0.21f;
                        Block("Box", goods, new Vector3(x, 0.72f, -1.55f), new Vector3(0.17f, 0.24f, 0.14f), Mat("Pack" + i % packs.Length, packs[i % packs.Length]), PrimitiveType.Cube, false);
                    }
                    var water = Mat("WaterPack", new Color(0.65f, 0.85f, 1f));
                    for (int i = 0; i < 6; i++)
                        Block("Bottle", goods, new Vector3(-0.75f + (i % 3) * 0.14f, 1.03f, -1.3f + (i / 3) * 0.1f), new Vector3(0.1f, 0.13f, 0.1f), water, PrimitiveType.Cylinder, false);
                    var fruit = Mat("Oranges", new Color(1f, 0.55f, 0.1f));
                    for (int i = 0; i < 7; i++)
                        Block("Orange", goods, new Vector3(0.35f + (i % 4) * 0.13f, 1.0f + (i / 4) * 0.08f, -1.3f), Vector3.one * 0.12f, fruit, PrimitiveType.Sphere, false);
                    break;
                }
                case ShopKind.Oud:
                {
                    Block("Cabinet", goods, new Vector3(0f, 0.45f, -1.5f), new Vector3(1.6f, 0.9f, 0.5f), Mat("OudWood", new Color(0.25f, 0.13f, 0.08f)), PrimitiveType.Cube, false);
                    Color[] glass = { new Color(0.85f, 0.65f, 0.2f), new Color(0.55f, 0.15f, 0.45f), new Color(0.7f, 0.1f, 0.15f), new Color(0.2f, 0.45f, 0.4f) };
                    for (int i = 0; i < 8; i++)
                    {
                        var m = Mat("Bottle" + i % glass.Length, glass[i % glass.Length]);
                        Vector3 p = new Vector3(-0.6f + i * 0.17f, 0.98f, -1.5f);
                        Block("Bottle", goods, p, new Vector3(0.09f, 0.08f, 0.09f), m, PrimitiveType.Cylinder, false);
                        Block("Cap", goods, p + Vector3.up * 0.11f, Vector3.one * 0.06f, Mat("Gold", new Color(0.95f, 0.78f, 0.3f)), PrimitiveType.Sphere, false);
                    }
                    var burner = Mat("Mabkhara", new Color(0.35f, 0.2f, 0.12f));
                    Block("Mabkhara", goods, new Vector3(0.9f, 0.18f, -1.7f), new Vector3(0.22f, 0.36f, 0.22f), burner, PrimitiveType.Cube, false);
                    Block("Bowl", goods, new Vector3(0.9f, 0.4f, -1.7f), new Vector3(0.3f, 0.08f, 0.3f), Mat("Gold", new Color(0.95f, 0.78f, 0.3f)), PrimitiveType.Cylinder, false);
                    Smoke(goods, new Vector3(0.9f, 0.5f, -1.7f));
                    break;
                }
                case ShopKind.Qahwa:
                {
                    Block("Carpet", goods, new Vector3(0f, 0.02f, -1.6f), new Vector3(2.2f, 0.03f, 1.1f), Mat("Carpet", new Color(0.62f, 0.1f, 0.12f)), PrimitiveType.Cube, false);
                    Block("Cushion", goods, new Vector3(-0.7f, 0.15f, -1.2f), new Vector3(0.7f, 0.25f, 0.35f), Mat("Cushion", new Color(0.12f, 0.25f, 0.4f)), PrimitiveType.Cube, false);
                    Block("Tray", goods, new Vector3(0.2f, 0.06f, -1.7f), new Vector3(0.6f, 0.02f, 0.6f), Mat("Brass", new Color(0.8f, 0.62f, 0.25f)), PrimitiveType.Cylinder, false);
                    var brass = Mat("Brass", new Color(0.8f, 0.62f, 0.25f));
                    Block("DallahBody", goods, new Vector3(0.1f, 0.22f, -1.7f), new Vector3(0.2f, 0.14f, 0.2f), brass, PrimitiveType.Cylinder, false);
                    Block("DallahNeck", goods, new Vector3(0.1f, 0.42f, -1.7f), new Vector3(0.12f, 0.08f, 0.12f), brass, PrimitiveType.Cylinder, false);
                    Block("DallahLid", goods, new Vector3(0.1f, 0.52f, -1.7f), new Vector3(0.14f, 0.1f, 0.14f), brass, PrimitiveType.Sphere, false);
                    var spout = Block("DallahSpout", goods, new Vector3(0.25f, 0.36f, -1.7f), new Vector3(0.035f, 0.13f, 0.035f), brass, PrimitiveType.Cylinder, false);
                    spout.transform.localRotation = Quaternion.Euler(0f, 0f, -50f);
                    var cup = Mat("Finjan", new Color(0.96f, 0.95f, 0.9f));
                    for (int i = 0; i < 3; i++)
                        Block("Finjan", goods, new Vector3(0.3f + i * 0.09f, 0.1f, -1.85f), new Vector3(0.06f, 0.03f, 0.06f), cup, PrimitiveType.Cylinder, false);
                    Smoke(goods, new Vector3(0.1f, 0.6f, -1.7f));
                    break;
                }
                case ShopKind.Dates:
                {
                    Block("Table", goods, new Vector3(0f, 0.4f, -1.5f), new Vector3(2f, 0.8f, 0.7f), wood, PrimitiveType.Cube, false);
                    Color[] kinds = { new Color(0.35f, 0.17f, 0.08f), new Color(0.55f, 0.3f, 0.1f), new Color(0.25f, 0.1f, 0.06f) };
                    var tray = Mat("Tray", new Color(0.78f, 0.78f, 0.8f));
                    for (int k = 0; k < 3; k++)
                    {
                        float x = -0.62f + k * 0.62f;
                        Block("Tray", goods, new Vector3(x, 0.82f, -1.5f), new Vector3(0.55f, 0.02f, 0.55f), tray, PrimitiveType.Cylinder, false);
                        var date = Mat("Dates" + k, kinds[k]);
                        for (int i = 0; i < 9; i++)
                        {
                            float a = i * 2.4f, r = 0.06f + (i % 3) * 0.06f;
                            Block("Date", goods, new Vector3(x + Mathf.Cos(a) * r, 0.88f + (i < 3 ? 0.06f : 0f), -1.5f + Mathf.Sin(a) * r),
                                  new Vector3(0.07f, 0.06f, 0.11f), date, PrimitiveType.Sphere, false);
                        }
                    }
                    var basket = Mat("PalmBasket", new Color(0.8f, 0.65f, 0.38f));
                    Block("Basket", goods, new Vector3(1.25f, 0.2f, -1.6f), new Vector3(0.45f, 0.2f, 0.45f), basket, PrimitiveType.Cylinder, false);
                    break;
                }
                case ShopKind.Spices:
                {
                    var sack = Mat("Sack", new Color(0.75f, 0.62f, 0.42f));
                    Color[] spice = { new Color(0.85f, 0.2f, 0.1f), new Color(0.95f, 0.75f, 0.1f), new Color(0.4f, 0.55f, 0.2f), new Color(0.6f, 0.3f, 0.15f), new Color(0.95f, 0.5f, 0.1f) };
                    for (int i = 0; i < 5; i++)
                    {
                        float x = -0.9f + i * 0.45f;
                        float z = -1.5f - (i % 2) * 0.25f;
                        Block("Sack", goods, new Vector3(x, 0.28f, z), new Vector3(0.42f, 0.56f, 0.42f), sack, PrimitiveType.Cylinder, false);
                        Block("Spice", goods, new Vector3(x, 0.57f, z), new Vector3(0.36f, 0.1f, 0.36f), Mat("Spice" + i, spice[i]), PrimitiveType.Sphere, false);
                    }
                    break;
                }
            }
        }

        private static void Awning(Transform shop, ShopKind kind)
        {
            Color a = kind == ShopKind.Grocery ? new Color(0.2f, 0.55f, 0.25f)
                    : kind == ShopKind.Shawarma ? new Color(0.75f, 0.12f, 0.1f)
                    : kind == ShopKind.Oud ? new Color(0.35f, 0.12f, 0.4f)
                    : kind == ShopKind.Spices ? new Color(0.9f, 0.45f, 0.1f)
                    : new Color(0.55f, 0.35f, 0.2f);
            var ma = Mat("Awning_" + kind, a);
            var mb = Mat("AwningWhite", new Color(0.96f, 0.94f, 0.88f));
            var awning = Group("Awning", shop, new Vector3(0f, 2.55f, -1.55f)).transform;
            awning.localRotation = Quaternion.Euler(-22f, 0f, 0f);
            for (int i = 0; i < 6; i++)
                Block("Stripe", awning, new Vector3(-1.25f + i * 0.5f, 0f, 0f), new Vector3(0.5f, 0.04f, 1.1f), i % 2 == 0 ? ma : mb, PrimitiveType.Cube, false);
        }

        private static void Smoke(Transform parent, Vector3 localPos)
        {
            var go = Group("Bakhoor", parent, localPos);
            var ps = go.AddComponent<ParticleSystem>();
            var main = ps.main;
            main.startLifetime = 2.6f;
            main.startSpeed = 0.25f;
            main.startSize = new ParticleSystem.MinMaxCurve(0.12f, 0.25f);
            main.startColor = new Color(0.9f, 0.9f, 0.9f, 0.35f);
            main.maxParticles = 40;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            var em = ps.emission;
            em.rateOverTime = 6f;
            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Cone;
            shape.angle = 6f;
            shape.radius = 0.03f;
            var size = ps.sizeOverLifetime;
            size.enabled = true;
            size.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0f, 0.6f, 1f, 1.8f));
            var col = ps.colorOverLifetime;
            col.enabled = true;
            var g = new Gradient();
            g.SetKeys(new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                      new[] { new GradientAlphaKey(0.4f, 0f), new GradientAlphaKey(0f, 1f) });
            col.color = g;
            var r = go.GetComponent<ParticleSystemRenderer>();
            r.sharedMaterial = AronArt.ParticleMaterial();
            go.transform.localRotation = Quaternion.Euler(-90f, 0f, 0f);
        }
    }
}
