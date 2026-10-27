// (c) Ahmed Alamrani. All rights reserved.
using System;
using System.Collections.Generic;

namespace Aron.Logic.Costumes
{
    /// <summary>
    /// One generated piece of a costume. Head pieces are measured in head lengths from the head bone, body and foot
    /// pieces in metres for a 1.8 m man standing at the origin, arm pieces in metres along the forearm.
    /// </summary>
    public class KitPart
    {
        public string name;
        /// <summary>head, body, foot or arm.</summary>
        public string anchor;
        /// <summary>main, pattern, trim, metal, leather or sole.</summary>
        public string tint = "main";
        public bool doubleSided;
        /// <summary>Part of the agal, hidden while the agal is in his hand.</summary>
        public bool agal;
        public Func<int, ShapeData> shape;

        public ShapeData Build(int segments)
        {
            var d = shape(segments);
            if (doubleSided) d.DoubleSide();
            return d;
        }
    }

    /// <summary>
    /// Every costume piece the game can build, keyed by the mesh names and accessory kinds used in
    /// Resources/Costumes. Pieces are low-poly and get fewer sides on weaker phones.
    /// </summary>
    public static class CostumeKit
    {
        public static readonly string[] NewAccessoryKinds =
        {
            "kumma", "massar", "khanjar", "futa", "imma", "chechia", "jard", "yashmagh", "tarboosha", "agal_cords",
            // Around the world. Simple stand-ins until the modelled versions arrive.
            "pagri", "jinnah_cap", "cowboy_hat", "top_hat", "bavarian_hat", "sombrero", "songkok", "fisherman_cap", "straw_hat",
            "kurta", "shirt", "shalwar", "jeans", "lederhosen", "long_coat", "charro_jacket", "dupatta", "sari_drape",
            "rebozo", "apron", "samping", "batik", "kebaya", "bush_hat", "doek", "shweshwe", "safari_vest", "outback_hat",
        };

        /// <summary>Accessories that are trousers and so take the place of the thobe skirt.</summary>
        public static readonly string[] Trousers = { "futa", "shalwar", "jeans", "lederhosen" };

        public static int Segments(QualityTier tier) => tier == QualityTier.Low ? 8 : tier == QualityTier.Medium ? 12 : 16;

        public static int TriangleBudget(QualityTier tier) => tier == QualityTier.Low ? 3000 : tier == QualityTier.Medium ? 6000 : 10000;

        private static readonly Dictionary<string, KitPart[]> meshes = new Dictionary<string, KitPart[]>
        {
            // The models already wear these two, they only get recoloured.
            ["thobe_long"] = new KitPart[0],
            ["abaya_long"] = new KitPart[0],
            ["shoes_closed"] = Shoes(false),
            ["babouche_closed"] = Shoes(true),
            ["kandura"] = new[] { RoundCollar(), Placket(1.1f) },
            ["galabeya_wide"] = new[] { Sleeve("galabeya_sleeve", 0.18f), Placket(1.2f) },
            ["jalabiya"] = new[] { Sleeve("jalabiya_sleeve", 0.13f), RoundCollar() },
            ["djellaba_hooded"] = new[] { Hood("main"), Sleeve("djellaba_sleeve", 0.12f), Placket(0.95f), Buttons() },
            ["burnous_cape"] = new[] { Cape(), Hood("main"), HoodTassel() },
            ["jebba"] = new[] { Jebba(), NeckBand("trim"), FrontBand("trim", 1.42f, 0.5f) },
            ["sherwal"] = new[] { Sherwal() },
            ["abaya_embroidered"] = new[]
            {
                OverCoat(), FrontBand("trim", 1.44f, 0.1f), HemBand(), Sleeve("abaya_sleeve", 0.15f), Cuff(),
            },
        };

        private static readonly Dictionary<string, KitPart[]> accessories = new Dictionary<string, KitPart[]>
        {
            ["shemagh"] = new[] { HeadCap("main"), Drape(70f, 290f, -0.6f) },
            ["ghutra"] = new[] { HeadCap("main"), Drape(70f, 290f, -0.6f) },
            ["keffiyeh"] = new[] { HeadCap("main"), Drape(40f, 320f, -0.8f) },
            ["yashmagh"] = new[] { HeadCap("main"), Drape(95f, 265f, -0.35f), BrowRoll(), YashmaghTail() },
            ["agal"] = new[] { AgalRing(1.04f, 0.59f) },
            ["agal_cords"] = new[] { AgalCord(-1f), AgalCord(1f) },
            ["kufi"] = new[] { Cap("kufi", 1.02f) },
            ["kumma"] = new[] { Cap("kumma", 1.1f), Band("kumma_band", 0.76f, 0.67f, "pattern"), Band("kumma_band_top", 0.98f, 0.665f, "pattern") },
            ["turban"] = Wraps("turban", 3, 0.64f, 0.11f, "main"),
            ["massar"] = Concat(Wraps("massar", 3, 0.64f, 0.11f, "pattern"), new[] { WrapTail("massar_tail", -0.5f) }),
            ["imma"] = Wraps("imma", 4, 0.68f, 0.14f, "main"),
            ["fez"] = new[] { Fez(), FezTassel() },
            ["chechia"] = new[] { Chechia() },
            ["hijab"] = new[] { Hijab(), HijabFront() },
            ["headband"] = new[] { Band("headband", 0.8f, 0.65f, "main") },
            ["flower_crown"] = new[] { Band("crown_ring", 0.85f, 0.64f, "main"), Flowers() },
            ["bisht"] = new[] { Bisht(), BishtCollar(), FrontBand("trim", 1.45f, 0.14f) },
            ["abaya"] = new[] { FrontBand("pattern", 1.42f, 0.1f), HemBand() },
            ["vest"] = new[] { Vest(), VestEdge() },
            ["sash"] = new[] { Sash() },
            ["belt"] = new[] { Belt("leather") },
            ["jambiya_belt"] = new[] { Belt("pattern"), JambiyaSheath(), Hilt(false) },
            ["khanjar"] = new[] { Belt("metal"), KhanjarSheath(), Hilt(true), KhanjarRings() },
            ["futa"] = new[] { Futa(), FutaStripe(), FutaFlap() },
            ["jard"] = new[] { Jard(), JardRoll() },
            ["tarboosha"] = new[] { Tarboosha(), TarbooshaTassel() },
            ["embroidery"] = new[] { NeckBand("main"), FrontBand("main", 1.42f, 1.1f) },
            ["shawl"] = new[] { Shawl() },
            // Hats.
            ["pagri"] = Concat(Wraps("pagri", 4, 0.7f, 0.12f, "main"), new[] { WrapTail("pagri_tail", -0.4f) }),
            ["jinnah_cap"] = new[] { Karakul() },
            ["cowboy_hat"] = new[] { Brim("cowboy_brim", 0.95f, 1.15f, 0.08f), Crown("cowboy_crown", 0.95f, 1.42f, 0.6f, 0.5f), Band("cowboy_band", 1.02f, 0.62f, "pattern") },
            ["top_hat"] = new[] { Brim("top_brim", 0.98f, 0.95f, 0.03f), Crown("top_crown", 0.98f, 1.48f, 0.6f, 0.62f), Band("top_band", 1.04f, 0.61f, "pattern") },
            ["bavarian_hat"] = new[] { Brim("alpine_brim", 0.95f, 0.88f, 0.05f), Crown("alpine_crown", 0.95f, 1.3f, 0.62f, 0.4f), Band("alpine_band", 1.0f, 0.62f, "pattern"), Feather() },
            ["sombrero"] = new[] { Brim("sombrero_brim", 0.95f, 1.28f, 0.16f), Crown("sombrero_crown", 0.95f, 1.45f, 0.58f, 0.3f), Band("sombrero_band", 1.05f, 0.58f, "pattern") },
            ["songkok"] = new[] { Cap("songkok", 1.12f) },
            ["fisherman_cap"] = new[] { Barrete(), BarreteTip() },
            ["straw_hat"] = new[] { Brim("straw_brim", 0.95f, 1.0f, 0.04f), Crown("straw_crown", 0.95f, 1.25f, 0.6f, 0.5f), Band("straw_band", 1.0f, 0.61f, "pattern") },
            // Clothes.
            ["kurta"] = new[] { Tunic("kurta", 0.5f, 0.36f), Placket(1.2f), Sleeve("kurta_sleeve", 0.09f) },
            ["shirt"] = new[] { Tunic("shirt", 0.82f, 0.31f), RoundCollar(), Sleeve("shirt_sleeve", 0.09f) },
            ["shalwar"] = new[] { Legs("shalwar", 0.31f, 0.12f, 0.06f) },
            ["jeans"] = new[] { Legs("jeans", 0.27f, 0.13f, 0.04f), Belt("leather") },
            ["lederhosen"] = new[] { Legs("lederhosen", 0.28f, 0.17f, 0.5f), Suspenders(), Socks() },
            ["long_coat"] = new[] { LongCoat(), FrontBand("trim", 1.42f, 0.2f), Sleeve("coat_sleeve", 0.1f) },
            ["charro_jacket"] = new[] { Bolero(), Buttons(), Sleeve("charro_sleeve", 0.085f) },
            ["dupatta"] = new[] { Dupatta() },
            ["sari_drape"] = new[] { SariDrape(), Sash() },
            ["rebozo"] = new[] { Shawl(), RebozoEnds() },
            ["apron"] = new[] { Apron(), Belt("pattern") },
            ["samping"] = new[] { Samping() },
            ["batik"] = new[] { BatikBands() },
            ["outback_hat"] = new[] { Brim("outback_brim", 0.98f, 1.1f, 0.06f), Crown("outback_crown", 0.98f, 1.2f, 0.55f, 0.5f), Band("outback_band", 1.0f, 0.62f, "pattern") },
            ["bush_hat"] = new[] { Brim("bush_brim", 0.95f, 1.08f, 0.06f), Crown("bush_crown", 0.95f, 1.3f, 0.6f, 0.45f), Band("bush_band", 1.0f, 0.61f, "pattern") },
            // A doek is tied over the hijab, never instead of it (the validator still wants the hijab).
            ["doek"] = Concat(Wraps("doek", 3, 0.7f, 0.13f, "main"), new[] { WrapTail("doek_knot", 0.6f) }),
            ["shweshwe"] = new[] { BatikBands(), HemBand() },
            ["safari_vest"] = new[] { Vest(), VestEdge(), Belt("leather") },
            ["kebaya"] = new[] { Tunic("kebaya", 0.62f, 0.35f), FrontBand("pattern", 1.42f, 0.62f), Sleeve("kebaya_sleeve", 0.1f) },
        };

        public static bool HasMesh(string id) => id != null && meshes.ContainsKey(id);

        public static bool HasAccessory(string kind) => kind != null && accessories.ContainsKey(kind);

        public static IEnumerable<string> MeshIds => meshes.Keys;

        public static IEnumerable<string> AccessoryIds => accessories.Keys;

        public static KitPart[] MeshParts(string id) => HasMesh(id) ? meshes[id] : new KitPart[0];

        public static KitPart[] AccessoryParts(string kind) => HasAccessory(kind) ? accessories[kind] : new KitPart[0];

        /// <summary>Trousers and the futa replace the thobe skirt rather than sit over it.</summary>
        public static bool HidesSkirt(CostumeDefinition c)
        {
            foreach (var m in c.meshes) if (m.mesh == "sherwal") return true;
            foreach (var t in Trousers) if (c.Has(t)) return true;
            return false;
        }

        public static int Triangles(CostumeDefinition c, QualityTier tier)
        {
            int n = Segments(tier), total = 0;
            foreach (var m in c.meshes) foreach (var p in MeshParts(m.mesh)) total += p.Build(n).TriangleCount * (p.anchor == "foot" || p.anchor == "arm" ? 2 : 1);
            foreach (var a in c.accessories) foreach (var p in AccessoryParts(a.kind)) total += p.Build(n).TriangleCount;
            return total;
        }

        /// <summary>What stops the kit from dressing this costume.</summary>
        public static List<string> Problems(CostumeDefinition c)
        {
            var errors = new List<string>();
            string w = "costume " + c.id;
            foreach (var m in c.meshes) if (!HasMesh(m.mesh)) errors.Add(w + ": no kit piece for mesh " + m.mesh);
            foreach (var a in c.accessories) if (!HasAccessory(a.kind)) errors.Add(w + ": no kit piece for " + a.kind);
            if (c.Has("agal_cords") && !c.Has("agal")) errors.Add(w + ": agal cords hang from an agal");
            if (c.Has("tarboosha") && c.hero != "aron") errors.Add(w + ": the tarboosha is on the kandura");
            bool feet = false;
            foreach (var m in c.meshes) if (m.slot == "feet") feet = true;
            if (c.hero == "aron" && !feet) errors.Add(w + ": needs closed shoes on his feet");
            return errors;
        }

        // ---- Head

        private static KitPart HeadCap(string tint) => new KitPart
        {
            name = "Cap", anchor = "head", tint = tint,
            shape = n => CostumeShapes.Lathe(new[] { new Ring(0.6f, 0.67f), new Ring(0.92f, 0.62f), new Ring(1.12f, 0.42f), new Ring(1.24f, 0f) }, n),
        };

        private static KitPart Drape(float from, float to, float bottom) => new KitPart
        {
            name = "Drape", anchor = "head", doubleSided = true,
            shape = n => CostumeShapes.Lathe(new[] { new Ring(0.9f, 0.66f), new Ring(0.25f, 0.72f), new Ring(bottom, 0.86f, -0.06f) }, n, from, to),
        };

        private static KitPart BrowRoll() => new KitPart
        {
            name = "BrowRoll", anchor = "head",
            shape = n => CostumeShapes.Torus(0.74f, 0.66f, 0.07f, n, Sides(n)),
        };

        private static KitPart YashmaghTail() => new KitPart
        {
            name = "Tail", anchor = "head", doubleSided = false,
            shape = n => CostumeShapes.Tube(new[] { P(-0.6f, 0.6f, -0.1f), P(-0.75f, 0.1f, 0.1f), P(-0.7f, -0.5f, 0.35f) }, new[] { 0.09f, 0.11f, 0.13f }, Sides(n)),
        };

        // One thick black ring, the way it is worn in Najd.
        private static KitPart AgalRing(float y, float r) => new KitPart
        {
            name = "AgalRing", anchor = "head", agal = true,
            shape = n => CostumeShapes.Torus(y, r, 0.07f, n, Sides(n), 1.05f),
        };

        private static KitPart AgalCord(float side) => new KitPart
        {
            name = "Cord", anchor = "head", agal = true,
            shape = n =>
            {
                var d = CostumeShapes.Tube(new[] { P(side * 0.12f, 1.02f, -0.6f), P(side * 0.16f, 0.4f, -0.72f), P(side * 0.2f, -0.6f, -0.78f) },
                                           new[] { 0.03f, 0.03f, 0.03f }, Math.Max(4, n / 3));
                d.Append(CostumeShapes.Ball(side * 0.2f, -0.72f, -0.78f, 0.06f, 0.12f, 0.06f, Math.Max(6, n / 2)));
                return d;
            },
        };

        private static KitPart Cap(string name, float top) => new KitPart
        {
            name = name, anchor = "head",
            shape = n => CostumeShapes.Lathe(new[] { new Ring(0.62f, 0.67f), new Ring(top - 0.04f, 0.66f), new Ring(top, 0.6f) }, n, closeTop: true),
        };

        private static KitPart Band(string name, float y, float r, string tint) => new KitPart
        {
            name = name, anchor = "head", tint = tint,
            shape = n => CostumeShapes.Torus(y, r, 0.03f, n, Sides(n)),
        };

        private static KitPart[] Wraps(string name, int layers, float radius, float thick, string tint)
        {
            var parts = new List<KitPart>();
            for (int i = 0; i < layers; i++)
            {
                float y = 0.68f + i * thick * 1.15f;
                float r = radius - i * 0.025f + (i % 2) * 0.02f;
                parts.Add(new KitPart
                {
                    name = name + "_wrap", anchor = "head", tint = i % 2 == 0 ? tint : "main",
                    shape = n => CostumeShapes.Torus(y, r, thick, n, Sides(n), 1.08f),
                });
            }
            float top = 0.68f + layers * thick * 1.15f;
            parts.Add(new KitPart
            {
                name = name + "_top", anchor = "head",
                shape = n => CostumeShapes.Lathe(new[] { new Ring(top - thick, radius - 0.06f), new Ring(top + 0.08f, radius * 0.6f), new Ring(top + 0.14f, 0f) }, n),
            });
            return parts.ToArray();
        }

        private static KitPart WrapTail(string name, float side) => new KitPart
        {
            name = name, anchor = "head", tint = "pattern",
            shape = n => CostumeShapes.Tube(new[] { P(side * 0.5f, 0.9f, -0.4f), P(side * 0.62f, 0.5f, -0.5f), P(side * 0.6f, 0.05f, -0.45f) },
                                            new[] { 0.08f, 0.09f, 0.05f }, Sides(n)),
        };

        private static KitPart Fez() => new KitPart
        {
            name = "Fez", anchor = "head",
            shape = n => CostumeShapes.Lathe(new[] { new Ring(0.66f, 0.6f), new Ring(1.3f, 0.48f) }, n, closeTop: true),
        };

        private static KitPart FezTassel() => new KitPart
        {
            name = "Tassel", anchor = "head", tint = "pattern",
            shape = n => CostumeShapes.Tube(new[] { P(0f, 1.31f, 0f), P(-0.2f, 1.3f, -0.3f), P(-0.3f, 1.0f, -0.42f) }, new[] { 0.02f, 0.025f, 0.06f }, Math.Max(4, n / 3)),
        };

        /// <summary>The soft red chechia: lower and rounder than a fez, no tassel.</summary>
        private static KitPart Chechia() => new KitPart
        {
            name = "Chechia", anchor = "head",
            shape = n => CostumeShapes.Lathe(new[] { new Ring(0.64f, 0.65f), new Ring(1.0f, 0.65f), new Ring(1.12f, 0.56f), new Ring(1.17f, 0.3f), new Ring(1.18f, 0f) }, n),
        };

        private static KitPart Hijab() => new KitPart
        {
            name = "Hijab", anchor = "head", doubleSided = true,
            shape = n => CostumeShapes.Lathe(new[]
            {
                new Ring(1.26f, 0.04f), new Ring(1.15f, 0.5f), new Ring(0.8f, 0.72f), new Ring(0.3f, 0.75f),
                new Ring(-0.2f, 0.82f), new Ring(-0.65f, 1.2f),
            }, n, 35f, 325f),
        };

        private static KitPart HijabFront() => new KitPart
        {
            name = "HijabFront", anchor = "head", doubleSided = true,
            shape = n => CostumeShapes.Lathe(new[] { new Ring(-0.05f, 0.8f), new Ring(-0.65f, 1.2f) }, Math.Max(3, n / 3), 145f, 215f),
        };

        private static KitPart Flowers() => new KitPart
        {
            name = "Flowers", anchor = "head", tint = "pattern",
            shape = n =>
            {
                var d = new ShapeData();
                for (int i = 0; i < 6; i++)
                {
                    double a = i * Math.PI / 3.0;
                    d.Append(CostumeShapes.Ball((float)Math.Sin(a) * 0.64f, 0.88f, -(float)Math.Cos(a) * 0.64f, 0.08f, 0.06f, 0.08f, 6));
                }
                return d;
            },
        };

        // ---- Body

        private static KitPart Hood(string tint) => new KitPart
        {
            name = "Hood", anchor = "body", tint = tint, doubleSided = true,
            shape = n => CostumeShapes.Lathe(new[]
            {
                new Ring(1.47f, 0.17f, -0.2f), new Ring(1.36f, 0.17f, -0.27f), new Ring(1.18f, 0.1f, -0.31f), new Ring(1.0f, 0f, -0.32f),
            }, n, 0f, 360f, 0.55f),
        };

        private static KitPart HoodTassel() => new KitPart
        {
            name = "HoodTassel", anchor = "body", tint = "trim",
            shape = n => CostumeShapes.Tube(new[] { P(0f, 1.0f, -0.32f), P(0f, 0.86f, -0.33f) }, new[] { 0.012f, 0.03f }, Math.Max(4, n / 3)),
        };

        private static KitPart Cape() => new KitPart
        {
            name = "Cape", anchor = "body", doubleSided = true,
            shape = n => CostumeShapes.Lathe(new[]
            {
                new Ring(1.48f, 0.2f), new Ring(1.37f, 0.34f), new Ring(0.8f, 0.42f), new Ring(0.16f, 0.48f),
            }, n, 25f, 335f, 0.85f),
        };

        private static KitPart Bisht() => new KitPart
        {
            name = "Bisht", anchor = "body", doubleSided = true,
            shape = n => CostumeShapes.Lathe(new[]
            {
                new Ring(1.46f, 0.2f), new Ring(1.38f, 0.32f), new Ring(1.0f, 0.34f), new Ring(0.5f, 0.38f), new Ring(0.14f, 0.42f),
            }, n, 35f, 325f, 0.8f),
        };

        private static KitPart BishtCollar() => new KitPart
        {
            name = "BishtCollar", anchor = "body", tint = "trim", doubleSided = true,
            shape = n => CostumeShapes.Lathe(new[] { new Ring(1.475f, 0.205f), new Ring(1.4f, 0.3f) }, n, 34f, 326f, 0.8f),
        };

        private static KitPart OverCoat() => new KitPart
        {
            name = "OverCoat", anchor = "body", doubleSided = true,
            shape = n => CostumeShapes.Lathe(new[]
            {
                new Ring(1.46f, 0.2f), new Ring(1.36f, 0.3f), new Ring(0.9f, 0.32f), new Ring(0.08f, 0.4f),
            }, n, 30f, 330f, 0.8f),
        };

        /// <summary>Two strips down the front opening.</summary>
        private static KitPart FrontBand(string tint, float top, float bottom) => new KitPart
        {
            name = "FrontBand", anchor = "body", tint = tint,
            shape = n =>
            {
                var rings = new[] { new Ring(top, 0.215f), new Ring((top + bottom) * 0.5f, 0.345f), new Ring(bottom, 0.425f) };
                var d = CostumeShapes.Lathe(rings, 1, 154f, 166f, 0.8f);
                d.Append(CostumeShapes.Lathe(rings, 1, 194f, 206f, 0.8f));
                d.DoubleSide();
                return d;
            },
        };

        private static KitPart HemBand() => new KitPart
        {
            name = "HemBand", anchor = "body", tint = "trim", doubleSided = true,
            shape = n => CostumeShapes.Lathe(new[] { new Ring(0.2f, 0.405f), new Ring(0.1f, 0.415f) }, n, 0f, 360f, 0.8f),
        };

        private static KitPart Sleeve(string name, float flare) => new KitPart
        {
            name = name, anchor = "arm", doubleSided = true,
            shape = n => CostumeShapes.Lathe(new[] { new Ring(-0.04f, 0.07f), new Ring(0.12f, 0.09f), new Ring(0.27f, flare) }, Math.Max(6, n * 3 / 4)),
        };

        private static KitPart Cuff() => new KitPart
        {
            name = "Cuff", anchor = "arm", tint = "trim",
            shape = n => CostumeShapes.Torus(0.27f, 0.15f, 0.015f, Math.Max(6, n * 3 / 4), 4),
        };

        private static KitPart RoundCollar() => new KitPart
        {
            name = "Collar", anchor = "body",
            shape = n => CostumeShapes.Torus(1.44f, 0.16f, 0.018f, n, 4, 0.85f),
        };

        private static KitPart NeckBand(string tint) => new KitPart
        {
            name = "NeckBand", anchor = "body", tint = tint,
            shape = n => CostumeShapes.Torus(1.44f, 0.17f, 0.022f, n, 4, 0.85f),
        };

        private static KitPart Placket(float bottom) => new KitPart
        {
            name = "Placket", anchor = "body", tint = "trim",
            shape = n => CostumeShapes.Lathe(new[] { new Ring(1.43f, 0.215f), new Ring(bottom, 0.29f) }, 1, 176f, 184f, 0.8f),
        };

        private static KitPart Buttons() => new KitPart
        {
            name = "Buttons", anchor = "body", tint = "trim",
            shape = n =>
            {
                var d = new ShapeData();
                for (int i = 0; i < 6; i++) d.Append(CostumeShapes.Ball(0f, 1.38f - i * 0.075f, 0.18f + i * 0.012f, 0.012f, 0.012f, 0.01f, 6));
                return d;
            },
        };

        private static KitPart Jebba() => new KitPart
        {
            name = "Jebba", anchor = "body", doubleSided = true,
            shape = n => CostumeShapes.Lathe(new[]
            {
                new Ring(1.45f, 0.2f), new Ring(1.37f, 0.3f), new Ring(1.0f, 0.3f), new Ring(0.45f, 0.35f),
            }, n, 0f, 360f, 0.8f),
        };

        private static KitPart Sherwal() => new KitPart
        {
            name = "Sherwal", anchor = "body", doubleSided = true,
            shape = n => CostumeShapes.Lathe(new[]
            {
                new Ring(1.0f, 0.27f), new Ring(0.7f, 0.31f), new Ring(0.4f, 0.27f), new Ring(0.18f, 0.16f), new Ring(0.08f, 0.13f),
            }, n, 0f, 360f, 0.75f),
        };

        private static KitPart Vest() => new KitPart
        {
            name = "Vest", anchor = "body", doubleSided = true,
            shape = n => CostumeShapes.Lathe(new[] { new Ring(1.45f, 0.21f), new Ring(1.36f, 0.3f), new Ring(0.98f, 0.29f) }, n, 30f, 330f, 0.8f),
        };

        private static KitPart VestEdge() => new KitPart
        {
            name = "VestEdge", anchor = "body", tint = "pattern", doubleSided = true,
            shape = n => CostumeShapes.Lathe(new[] { new Ring(1.0f, 0.295f), new Ring(0.97f, 0.295f) }, n, 30f, 330f, 0.8f),
        };

        private static KitPart Sash() => new KitPart
        {
            name = "Sash", anchor = "body", tint = "pattern",
            shape = n =>
            {
                var d = CostumeShapes.Lathe(new[] { new Ring(1.02f, 0.285f), new Ring(0.9f, 0.29f) }, n, 0f, 360f, 0.8f);
                d.Append(CostumeShapes.Tube(new[] { P(-0.27f, 0.95f, 0.05f), P(-0.3f, 0.75f, 0.06f), P(-0.31f, 0.6f, 0.04f) }, new[] { 0.035f, 0.04f, 0.045f }, Sides(n)));
                return d;
            },
        };

        private static KitPart Belt(string tint) => new KitPart
        {
            name = "Belt", anchor = "body", tint = tint,
            shape = n => CostumeShapes.Lathe(new[] { new Ring(1.0f, 0.285f), new Ring(0.93f, 0.29f) }, n, 0f, 360f, 0.8f),
        };

        /// <summary>Yemeni jambiya: the sheath runs down the middle and hooks sharply up to the right.</summary>
        private static KitPart JambiyaSheath() => new KitPart
        {
            name = "Jambiya", anchor = "body", tint = "pattern",
            shape = n => CostumeShapes.Tube(new[]
            {
                P(0f, 0.99f, 0.245f), P(0f, 0.86f, 0.255f), P(0.015f, 0.78f, 0.26f), P(0.06f, 0.76f, 0.26f), P(0.09f, 0.83f, 0.255f), P(0.1f, 0.9f, 0.25f),
            }, new[] { 0.035f, 0.033f, 0.03f, 0.026f, 0.02f, 0.012f }, Sides(n)),
        };

        /// <summary>Omani khanjar: silver sheath with the deep right-angle curve and rings for the belt.</summary>
        private static KitPart KhanjarSheath() => new KitPart
        {
            name = "Khanjar", anchor = "body", tint = "metal",
            shape = n => CostumeShapes.Tube(new[]
            {
                P(0f, 0.98f, 0.25f), P(0f, 0.86f, 0.26f), P(0.02f, 0.77f, 0.265f), P(0.08f, 0.73f, 0.265f), P(0.14f, 0.745f, 0.26f), P(0.17f, 0.77f, 0.255f),
            }, new[] { 0.04f, 0.038f, 0.034f, 0.028f, 0.02f, 0.01f }, Sides(n)),
        };

        private static KitPart KhanjarRings() => new KitPart
        {
            name = "KhanjarRings", anchor = "body", tint = "metal",
            shape = n =>
            {
                var d = CostumeShapes.Move(CostumeShapes.Torus(0f, 0.045f, 0.008f, Math.Max(6, n / 2), 4), 0f, 0.95f, 0.25f);
                d.Append(CostumeShapes.Move(CostumeShapes.Torus(0f, 0.042f, 0.008f, Math.Max(6, n / 2), 4), 0f, 0.88f, 0.255f));
                return d;
            },
        };

        private static KitPart Hilt(bool omani) => new KitPart
        {
            name = "Hilt", anchor = "body", tint = omani ? "metal" : "trim",
            shape = n =>
            {
                var d = CostumeShapes.Tube(new[] { P(0f, 0.99f, 0.245f), P(0f, 1.08f, 0.24f) }, new[] { 0.018f, 0.022f }, Math.Max(4, n / 2));
                if (omani) d.Append(CostumeShapes.Tube(new[] { P(-0.055f, 1.09f, 0.24f), P(0.055f, 1.09f, 0.24f) }, new[] { 0.022f, 0.022f }, Math.Max(4, n / 2)));
                else d.Append(CostumeShapes.Ball(0f, 1.1f, 0.24f, 0.04f, 0.03f, 0.03f, Math.Max(6, n / 2)));
                return d;
            },
        };

        private static KitPart Futa() => new KitPart
        {
            name = "Futa", anchor = "body", doubleSided = true,
            shape = n => CostumeShapes.Lathe(new[] { new Ring(1.0f, 0.28f), new Ring(0.6f, 0.3f), new Ring(0.22f, 0.32f) }, n, 0f, 360f, 0.8f),
        };

        private static KitPart FutaStripe() => new KitPart
        {
            name = "FutaStripe", anchor = "body", tint = "pattern", doubleSided = true,
            shape = n => CostumeShapes.Lathe(new[] { new Ring(0.32f, 0.317f), new Ring(0.27f, 0.321f) }, n, 0f, 360f, 0.8f),
        };

        private static KitPart FutaFlap() => new KitPart
        {
            name = "FutaFlap", anchor = "body", tint = "pattern", doubleSided = true,
            shape = n => CostumeShapes.Lathe(new[] { new Ring(1.0f, 0.29f), new Ring(0.22f, 0.33f) }, 2, 196f, 214f, 0.8f),
        };

        /// <summary>Libyan jard: a long wool wrap over the left shoulder and round the back, rolled across the chest.</summary>
        private static KitPart Jard() => new KitPart
        {
            name = "Jard", anchor = "body", doubleSided = true,
            shape = n => CostumeShapes.Lathe(new[]
            {
                new Ring(1.47f, 0.21f), new Ring(1.37f, 0.33f, -0.01f), new Ring(0.95f, 0.35f, -0.02f), new Ring(0.42f, 0.37f, -0.03f),
            }, n, 10f, 300f, 0.82f),
        };

        private static KitPart JardRoll() => new KitPart
        {
            name = "JardRoll", anchor = "body",
            shape = n => CostumeShapes.Tube(new[] { P(-0.22f, 1.42f, 0.1f), P(-0.05f, 1.2f, 0.27f), P(0.16f, 0.98f, 0.27f), P(0.3f, 0.86f, 0.1f) },
                                            new[] { 0.05f, 0.06f, 0.06f, 0.05f }, Sides(n)),
        };

        private static KitPart Tarboosha() => new KitPart
        {
            name = "Tarboosha", anchor = "body", tint = "pattern",
            shape = n => CostumeShapes.Tube(new[] { P(0.02f, 1.43f, 0.15f), P(0.025f, 1.3f, 0.215f), P(0.03f, 1.1f, 0.235f) },
                                            new[] { 0.007f, 0.007f, 0.007f }, 4, false),
        };

        private static KitPart TarbooshaTassel() => new KitPart
        {
            name = "TarbooshaTassel", anchor = "body", tint = "pattern",
            shape = n => CostumeShapes.Tube(new[] { P(0.03f, 1.1f, 0.235f), P(0.03f, 1.02f, 0.24f) }, new[] { 0.008f, 0.024f }, Math.Max(4, n / 3)),
        };

        private static KitPart Shawl() => new KitPart
        {
            name = "Shawl", anchor = "body", doubleSided = true,
            shape = n => CostumeShapes.Lathe(new[] { new Ring(1.48f, 0.19f), new Ring(1.38f, 0.32f), new Ring(1.12f, 0.34f, 0.02f) }, n, 0f, 360f, 0.85f),
        };

        // ---- Feet. Same shape on both feet, carried by the foot bones.

        private static KitPart[] Shoes(bool babouche)
        {
            var sole = new KitPart
            {
                name = "Sole", anchor = "foot", tint = "sole",
                shape = n => CostumeShapes.Lathe(new[] { new Ring(0f, 0.052f, 0.045f), new Ring(0.024f, 0.052f, 0.045f) }, n, 0f, 360f, 2.6f, true),
            };
            var heel = new KitPart
            {
                name = "Heel", anchor = "foot", tint = "sole",
                shape = n => CostumeShapes.Ball(0f, 0.03f, -0.065f, 0.045f, 0.03f, 0.04f, Math.Max(6, n / 2)),
            };
            var upper = new KitPart
            {
                name = "Upper", anchor = "foot", tint = "leather",
                shape = n => CostumeShapes.Ball(0f, 0.06f, 0.02f, 0.053f, 0.043f, 0.105f, n),
            };
            var collar = new KitPart
            {
                name = "Collar", anchor = "foot", tint = "leather",
                shape = n => CostumeShapes.Torus(0.095f, 0.045f, 0.008f, n, 4, 1.2f),
            };
            if (!babouche)
            {
                var toe = new KitPart
                {
                    name = "ToeBox", anchor = "foot", tint = "leather",
                    shape = n => CostumeShapes.Ball(0f, 0.045f, 0.12f, 0.049f, 0.03f, 0.055f, n),
                };
                return new[] { sole, heel, upper, toe, collar };
            }
            // Closed babouche: pointed toe and a full heel counter, never worn backless here.
            var point = new KitPart
            {
                name = "PointedToe", anchor = "foot", tint = "leather",
                shape = n => CostumeShapes.Tube(new[] { P(0f, 0.045f, 0.1f), P(0f, 0.04f, 0.16f), P(0f, 0.045f, 0.21f) }, new[] { 0.045f, 0.028f, 0.004f }, Math.Max(6, n * 3 / 4)),
            };
            var counter = new KitPart
            {
                name = "HeelCounter", anchor = "foot", tint = "leather", doubleSided = true,
                shape = n => CostumeShapes.Lathe(new[] { new Ring(0.02f, 0.05f, -0.02f), new Ring(0.09f, 0.047f, -0.02f) }, n, 270f, 450f, 1.4f),
            };
            return new[] { sole, upper, point, counter, collar };
        }

        // ---- Around the world

        private static KitPart Brim(string name, float y, float r, float droop) => new KitPart
        {
            name = name, anchor = "head", doubleSided = true,
            shape = n => CostumeShapes.Lathe(new[] { new Ring(y + 0.02f, 0.6f), new Ring(y, r * 0.75f), new Ring(y - droop, r) }, n),
        };

        private static KitPart Crown(string name, float y, float top, float r, float topR) => new KitPart
        {
            name = name, anchor = "head",
            shape = n => CostumeShapes.Lathe(new[] { new Ring(y, r), new Ring(top - 0.04f, topR), new Ring(top, topR * 0.8f) }, n, closeTop: true),
        };

        private static KitPart Karakul() => new KitPart
        {
            name = "Karakul", anchor = "head",
            shape = n => CostumeShapes.Lathe(new[] { new Ring(0.66f, 0.66f), new Ring(1.12f, 0.64f), new Ring(1.16f, 0.5f), new Ring(1.1f, 0.2f), new Ring(1.16f, 0f) }, n),
        };

        private static KitPart Feather() => new KitPart
        {
            name = "Feather", anchor = "head", tint = "trim",
            shape = n => CostumeShapes.Tube(new[] { P(0.55f, 1.05f, 0.1f), P(0.62f, 1.3f, 0.0f), P(0.6f, 1.45f, -0.15f) }, new[] { 0.02f, 0.05f, 0.01f }, Math.Max(4, n / 3)),
        };

        /// <summary>Portuguese fisherman's barrete: a long soft wool stocking cap folded over to one side.</summary>
        private static KitPart Barrete() => new KitPart
        {
            name = "Barrete", anchor = "head",
            shape = n => CostumeShapes.Lathe(new[] { new Ring(0.64f, 0.67f), new Ring(1.0f, 0.64f), new Ring(1.16f, 0.4f), new Ring(1.2f, 0f) }, n),
        };

        private static KitPart BarreteTip() => new KitPart
        {
            name = "BarreteTip", anchor = "head", tint = "pattern",
            shape = n => CostumeShapes.Tube(new[] { P(0f, 1.15f, 0f), P(0.45f, 1.1f, -0.1f), P(0.75f, 0.75f, -0.15f) }, new[] { 0.3f, 0.2f, 0.1f }, Sides(n)),
        };

        private static KitPart Tunic(string name, float hem, float hemR) => new KitPart
        {
            name = name, anchor = "body", doubleSided = true,
            shape = n => CostumeShapes.Lathe(new[] { new Ring(1.46f, 0.2f), new Ring(1.36f, 0.3f), new Ring(1.0f, 0.3f), new Ring(hem, hemR) }, n, 0f, 360f, 0.8f),
        };

        /// <summary>Two legs from the waist down to <paramref name="bottom"/>, bunched at the ankle for shalwar.</summary>
        private static KitPart Legs(string name, float hip, float ankle, float bottom) => new KitPart
        {
            name = name, anchor = "body",
            shape = n =>
            {
                int k = Math.Max(6, n * 3 / 4);
                var left = CostumeShapes.Tube(new[] { P(-0.1f, 1.0f, 0f), P(-0.11f, 0.55f, 0.01f), P(-0.11f, bottom, 0f) }, new[] { hip * 0.5f, hip * 0.42f, ankle * 0.6f }, k);
                var right = CostumeShapes.MirrorX(left);
                left.Append(right);
                left.Append(CostumeShapes.Lathe(new[] { new Ring(1.02f, 0.28f), new Ring(0.86f, 0.27f) }, n, 0f, 360f, 0.8f));
                return left;
            },
        };

        private static KitPart Suspenders() => new KitPart
        {
            name = "Suspenders", anchor = "body", tint = "trim",
            shape = n =>
            {
                var d = CostumeShapes.Tube(new[] { P(-0.1f, 1.0f, 0.24f), P(-0.12f, 1.25f, 0.26f), P(-0.14f, 1.44f, 0.12f), P(-0.1f, 1.3f, -0.24f), P(-0.08f, 1.0f, -0.24f) },
                                           new[] { 0.02f, 0.02f, 0.02f, 0.02f, 0.02f }, 4);
                d.Append(CostumeShapes.MirrorX(d));
                d.Append(CostumeShapes.Tube(new[] { P(-0.12f, 1.2f, 0.27f), P(0.12f, 1.2f, 0.27f) }, new[] { 0.025f, 0.025f }, 4));
                return d;
            },
        };

        private static KitPart Socks() => new KitPart
        {
            name = "Socks", anchor = "body", tint = "pattern",
            shape = n =>
            {
                var d = CostumeShapes.Tube(new[] { P(-0.11f, 0.5f, 0f), P(-0.11f, 0.12f, 0f) }, new[] { 0.06f, 0.05f }, Math.Max(6, n / 2));
                d.Append(CostumeShapes.MirrorX(d));
                return d;
            },
        };

        private static KitPart LongCoat() => new KitPart
        {
            name = "LongCoat", anchor = "body", doubleSided = true,
            shape = n => CostumeShapes.Lathe(new[] { new Ring(1.47f, 0.21f), new Ring(1.37f, 0.32f), new Ring(1.0f, 0.31f), new Ring(0.5f, 0.38f), new Ring(0.25f, 0.4f) }, n, 15f, 345f, 0.82f),
        };

        /// <summary>The short charro jacket that stops at the waist.</summary>
        private static KitPart Bolero() => new KitPart
        {
            name = "Bolero", anchor = "body", doubleSided = true,
            shape = n => CostumeShapes.Lathe(new[] { new Ring(1.46f, 0.21f), new Ring(1.37f, 0.31f), new Ring(1.05f, 0.3f) }, n, 25f, 335f, 0.82f),
        };

        private static KitPart Dupatta() => new KitPart
        {
            name = "Dupatta", anchor = "body", tint = "pattern", doubleSided = true,
            shape = n => CostumeShapes.Tube(new[] { P(-0.28f, 1.0f, 0.12f), P(-0.2f, 1.44f, 0.12f), P(0.2f, 1.44f, -0.1f), P(0.3f, 1.0f, -0.18f), P(0.32f, 0.6f, -0.2f) },
                                            new[] { 0.06f, 0.06f, 0.06f, 0.07f, 0.08f }, Sides(n)),
        };

        /// <summary>The pallu of a sari, over the left shoulder and across the front of the long dress.</summary>
        private static KitPart SariDrape() => new KitPart
        {
            name = "SariDrape", anchor = "body", tint = "pattern", doubleSided = true,
            shape = n => CostumeShapes.Tube(new[] { P(0.3f, 0.95f, 0.18f), P(0.05f, 1.2f, 0.3f), P(-0.2f, 1.45f, 0.1f), P(-0.25f, 1.3f, -0.25f), P(-0.25f, 0.6f, -0.3f) },
                                            new[] { 0.07f, 0.08f, 0.07f, 0.09f, 0.12f }, Sides(n)),
        };

        private static KitPart RebozoEnds() => new KitPart
        {
            name = "RebozoEnds", anchor = "body", tint = "pattern",
            shape = n =>
            {
                var d = CostumeShapes.Tube(new[] { P(-0.18f, 1.3f, 0.26f), P(-0.2f, 0.95f, 0.3f), P(-0.2f, 0.7f, 0.3f) }, new[] { 0.06f, 0.07f, 0.08f }, Sides(n));
                d.Append(CostumeShapes.MirrorX(d));
                return d;
            },
        };

        private static KitPart Apron() => new KitPart
        {
            name = "Apron", anchor = "body", doubleSided = true,
            shape = n => CostumeShapes.Lathe(new[] { new Ring(1.0f, 0.3f), new Ring(0.55f, 0.36f), new Ring(0.3f, 0.38f) }, Math.Max(4, n / 2), 130f, 230f, 0.85f),
        };

        /// <summary>Kain samping: the short sarong wrapped over the trousers of a baju Melayu.</summary>
        private static KitPart Samping() => new KitPart
        {
            name = "Samping", anchor = "body", tint = "pattern", doubleSided = true,
            shape = n => CostumeShapes.Lathe(new[] { new Ring(1.02f, 0.3f), new Ring(0.7f, 0.33f), new Ring(0.5f, 0.34f) }, n, 0f, 360f, 0.82f),
        };

        private static KitPart BatikBands() => new KitPart
        {
            name = "Batik", anchor = "body", tint = "pattern",
            shape = n =>
            {
                var d = CostumeShapes.Torus(1.25f, 0.305f, 0.02f, n, 4, 0.8f);
                d.Append(CostumeShapes.Torus(1.12f, 0.305f, 0.02f, n, 4, 0.8f));
                d.Append(CostumeShapes.Torus(0.98f, 0.31f, 0.02f, n, 4, 0.8f));
                return d;
            },
        };

        private static KitPart[] Concat(KitPart[] a, KitPart[] b)
        {
            var all = new KitPart[a.Length + b.Length];
            a.CopyTo(all, 0);
            b.CopyTo(all, a.Length);
            return all;
        }

        private static int Sides(int segments) => Math.Max(4, segments / 2);

        private static float[] P(float x, float y, float z) => new[] { x, y, z };
    }
}
