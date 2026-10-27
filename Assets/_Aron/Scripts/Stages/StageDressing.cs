// (c) Ahmed Alamrani. All rights reserved.
using Aron.Logic.Regions;
using UnityEngine;

namespace Aron.Stages
{
    /// <summary>
    /// Scenery along the shoulders that fits the terrain, plus the things you can break: clay jars by the market
    /// stalls, mud walls in old towns, and metal barriers across the track while you're on the horse.
    /// </summary>
    public static class StageDressing
    {
        public static void Dress(StageArt art, Transform world, StageLayout layout, string terrain, Color ground, Color accent)
        {
            var rnd = new System.Random(layout.Stage.Seed ^ 0x5A17);
            float edge = layout.HalfWidth + 1.6f;
            int n = 0;
            foreach (var g in layout.Of(PieceKind.Ground))
            {
                for (float z = g.Start + 1.5f; z < g.End - 1f; z += 5f + (float)rnd.NextDouble() * 4f, n++)
                {
                    float side = n % 2 == 0 ? 1f : -1f;
                    var pos = new Vector3(side * (edge + (float)rnd.NextDouble() * 5f), g.y - 0.3f, z);
                    Prop(art, world, terrain, pos, rnd, ground, accent);
                }
                if (terrain == "souq" || terrain == "street" || terrain == "ruins")
                    for (float z = g.Start + 4f; z < g.End - 3f; z += 11f + (float)rnd.NextDouble() * 6f)
                    {
                        float side = rnd.Next(2) == 0 ? 1f : -1f;
                        Jar(art, world, new Vector3(side * (layout.HalfWidth - 0.6f), g.y, z), rnd.Next(3) == 0 ? 2 : 0);
                    }
            }

            // Mud walls block the side path in old towns; a charge (mount or oryx) knocks them down for the coins behind.
            if (terrain == "street" || terrain == "ruins" || terrain == "mountain")
            {
                foreach (var cp in layout.Of(PieceKind.Checkpoint))
                {
                    float z = StageConditions.SafeZ(layout, cp.z + 8f);
                    float gy = layout.GroundAt(z);
                    if (float.IsNaN(gy)) continue;
                    Wall(art, world, new Vector3(layout.HalfWidth - 1.1f, gy, z), ground);
                    break;
                }
            }

            if (layout.Stage.mount == "horse") Barriers(art, world, layout);
            Nooks(art, world, layout, terrain, ground, rnd);
        }

        // Side nooks with dates for the two heroes: one that needs Noura (lock, narrow slot or hidden planks)
        // and one high ledge that needs both of them.
        private static void Nooks(StageArt art, Transform world, StageLayout layout, string terrain, Color ground, System.Random rnd)
        {
            var cps = new System.Collections.Generic.List<float>();
            foreach (var cp in layout.Of(PieceKind.Checkpoint)) cps.Add(cp.z);
            if (cps.Count == 0) return;
            float side = rnd.Next(2) == 0 ? 1f : -1f;
            float x = side * (layout.HalfWidth - 1.2f);

            float z = StageConditions.SafeZ(layout, cps[0] + 14f);
            float gy = layout.GroundAt(z);
            if (!float.IsNaN(gy))
            {
                var at = new Vector3(x, gy, z);
                if (terrain == "street" || terrain == "souq") Gate(art, world, at, side, ground);
                else if (terrain == "desert" || terrain == "oasis" || terrain == "coast") Secret(art, world, at, side, ground);
                else Slot(art, world, at, side, ground);
            }

            z = StageConditions.SafeZ(layout, cps[cps.Count - 1] + 10f);
            gy = layout.GroundAt(z);
            if (!float.IsNaN(gy)) Ledge(art, world, new Vector3(-x, gy, z), -side, ground);
        }

        private static Transform Basket(StageArt art, Transform parent, Vector3 local, int dates)
        {
            var g = StageArt.Group("DateBasket", parent, local);
            art.Cyl(g, Vector3.zero, 0.32f, 0.3f, new Color(0.6f, 0.45f, 0.25f));
            art.Ball(g, new Vector3(0f, 0.32f, 0f), new Vector3(0.5f, 0.2f, 0.5f), new Color(0.45f, 0.2f, 0.1f));
            var t = g.gameObject.AddComponent<SphereCollider>();
            t.isTrigger = true; t.radius = 0.7f; t.center = new Vector3(0f, 0.4f, 0f);
            g.gameObject.AddComponent<Heroes.DateBasket>().Setup(dates);
            return g;
        }

        private static GameObject Solid(GameObject box)
        {
            box.AddComponent<BoxCollider>();
            return box;
        }

        // Local -z faces the track.
        private static float Facing(float side) => side > 0f ? 90f : -90f;

        // Three walls round a basket; the side facing the track is a door with bars.
        private static void Gate(StageArt art, Transform world, Vector3 p, float side, Color ground)
        {
            var g = Alcove(art, world, "LockedNook", p, side, ground, 2.2f);
            var bars = StageArt.Group("Bars", g, new Vector3(0f, 0f, -1.1f));
            var iron = new Color(0.25f, 0.25f, 0.27f);
            for (int i = 0; i < 6; i++) art.Box(bars, new Vector3(-0.9f + i * 0.36f, 1.1f, 0f), new Vector3(0.08f, 2.2f, 0.08f), iron);
            art.Box(bars, new Vector3(0f, 1.1f, -0.05f), new Vector3(0.3f, 0.35f, 0.12f), new Color(0.8f, 0.62f, 0.2f));
            var col = bars.gameObject.AddComponent<BoxCollider>();
            col.center = new Vector3(0f, 1.1f, 0f); col.size = new Vector3(2.2f, 2.2f, 0.2f);
            g.gameObject.AddComponent<Heroes.LockedGate>().Setup(bars);
            Basket(art, g, Vector3.zero, 6);
        }

        // Same nook, but the way in is a slot only Noura is slim enough for.
        private static void Slot(StageArt art, Transform world, Vector3 p, float side, Color ground)
        {
            const float gap = 0.8f;
            var g = Alcove(art, world, "NarrowNook", p, side, ground, 2.2f);
            float post = (2.2f - gap) * 0.5f;
            Solid(art.Box(g, new Vector3(-(gap + post) * 0.5f, 1.2f, -1.1f), new Vector3(post, 2.4f, 0.4f), ground * 0.8f));
            Solid(art.Box(g, new Vector3((gap + post) * 0.5f, 1.2f, -1.1f), new Vector3(post, 2.4f, 0.4f), ground * 0.8f));
            g.gameObject.AddComponent<Heroes.NarrowGap>().Setup(gap);
            Basket(art, g, Vector3.zero, 6);
        }

        // A rock too tall to jump, and planks up to it that only show up for Noura.
        private static void Secret(StageArt art, Transform world, Vector3 p, float side, Color ground)
        {
            var rock = StageArt.Group("SecretRock", world, p + new Vector3(side * 0.6f, 0f, 4.2f), Facing(side));
            Solid(art.Box(rock, new Vector3(0f, 2.3f, 0f), new Vector3(2.2f, 4.6f, 2.2f), ground * 0.75f));
            Basket(art, rock, new Vector3(0f, 4.6f, 0f), 8);
            var route = StageArt.Group("SecretRoute", world, p);
            var wood = new Color(0.6f, 0.45f, 0.28f);
            for (int i = 0; i < 5; i++)
                Solid(art.Box(route, new Vector3(0f, 0.75f + i * 0.75f, i * 0.6f - 1.2f), new Vector3(1.2f, 0.15f, 0.6f), wood));
            route.gameObject.AddComponent<Heroes.SecretRoute>();
        }

        private static void Ledge(StageArt art, Transform world, Vector3 p, float side, Color ground)
        {
            var g = StageArt.Group("CoopLedge", world, p, Facing(side));
            Solid(art.Box(g, new Vector3(0f, 2.3f, 1.2f), new Vector3(2.4f, 4.6f, 2f), ground * 0.78f));
            Basket(art, g, new Vector3(0f, 4.6f, 1.4f), 8);
            g.gameObject.AddComponent<Heroes.CoopLedge>().Setup(new Vector3(0f, 4.7f, 0.8f));
        }

        // Back and two side walls with a roof, open toward the track.
        private static Transform Alcove(StageArt art, Transform world, string name, Vector3 p, float side, Color ground, float width)
        {
            var g = StageArt.Group(name, world, p, Facing(side));
            var wall = ground * 0.85f;
            Solid(art.Box(g, new Vector3(0f, 1.2f, 1.1f), new Vector3(width + 0.4f, 2.4f, 0.4f), wall));
            Solid(art.Box(g, new Vector3(-width * 0.5f - 0.1f, 1.2f, 0f), new Vector3(0.4f, 2.4f, 2.6f), wall));
            Solid(art.Box(g, new Vector3(width * 0.5f + 0.1f, 1.2f, 0f), new Vector3(0.4f, 2.4f, 2.6f), wall));
            Solid(art.Box(g, new Vector3(0f, 2.5f, 0f), new Vector3(width + 0.6f, 0.2f, 2.8f), wall * 0.9f));
            return g;
        }

        private static void Prop(StageArt art, Transform world, string terrain, Vector3 p, System.Random rnd, Color ground, Color accent)
        {
            var g = StageArt.Group("Prop", world, p, (float)rnd.NextDouble() * 360f);
            float s = 0.8f + (float)rnd.NextDouble() * 0.5f;
            switch (terrain)
            {
                case "desert":
                    if (rnd.Next(3) == 0) art.Ball(g, Vector3.zero, new Vector3(1.6f, 0.9f, 1.3f) * s, new Color(0.62f, 0.45f, 0.32f));
                    else art.Ball(g, new Vector3(0f, 0.3f, 0f), new Vector3(1.1f, 0.8f, 1.1f) * s, new Color(0.5f, 0.55f, 0.3f));
                    break;
                case "oasis":
                case "coast":
                    art.Frustum(g, Vector3.zero, 6, 0.22f, 0.16f, 4.5f * s, new Color(0.5f, 0.36f, 0.22f));
                    art.Ball(g, new Vector3(0f, 4.6f * s, 0f), new Vector3(2.6f, 0.6f, 2.6f) * s, new Color(0.25f, 0.5f, 0.22f));
                    break;
                case "mountain":
                    art.Frustum(g, Vector3.zero, 5, 1.4f * s, 0.3f, 2.4f * s, new Color(0.5f, 0.42f, 0.36f));
                    break;
                case "forest":
                    art.Cyl(g, Vector3.zero, 0.2f, 1.6f * s, new Color(0.4f, 0.28f, 0.18f));
                    art.Frustum(g, new Vector3(0f, 1f, 0f), 7, 1.4f * s, 0.1f, 3.6f * s, new Color(0.2f, 0.4f, 0.22f));
                    break;
                case "river":
                    for (int i = 0; i < 4; i++) art.Cyl(g, new Vector3(i * 0.3f - 0.45f, 0f, (i % 2) * 0.3f), 0.05f, 1.6f + i * 0.2f, new Color(0.55f, 0.6f, 0.3f));
                    break;
                case "ruins":
                    art.Box(g, new Vector3(0f, 0.6f * s, 0f), new Vector3(1f, 1.2f * s, 1f), new Color(0.74f, 0.66f, 0.54f));
                    break;
                case "souq":
                    art.Box(g, new Vector3(0f, 0.45f, 0f), new Vector3(1.6f, 0.9f, 0.9f), new Color(0.45f, 0.3f, 0.18f));
                    art.Box(g, new Vector3(0f, 2.2f, 0f), new Vector3(1.9f, 0.06f, 1.3f), rnd.Next(2) == 0 ? accent : new Color(0.94f, 0.9f, 0.8f));
                    for (int i = 0; i < 2; i++) art.Cyl(g, new Vector3(i * 1.6f - 0.8f, 0f, 0.5f), 0.05f, 2.2f, new Color(0.35f, 0.25f, 0.16f));
                    break;
                default: // street
                    if (rnd.Next(2) == 0) art.Box(g, new Vector3(0f, 1.4f, 0f), new Vector3(2.6f, 2.8f, 2.6f), ground * 0.95f);
                    else { art.Cyl(g, Vector3.zero, 0.12f, 2.4f, new Color(0.45f, 0.32f, 0.2f)); art.Ball(g, new Vector3(0f, 2.6f, 0f), new Vector3(1.8f, 1f, 1.8f), new Color(0.3f, 0.5f, 0.25f)); }
                    break;
            }
            foreach (var c in g.GetComponentsInChildren<Collider>()) Object.Destroy(c);
        }

        private static void Jar(StageArt art, Transform world, Vector3 p, int dates)
        {
            var g = StageArt.Group("Jar", world, p);
            var clay = new Color(0.72f, 0.42f, 0.26f);
            art.Ball(g, new Vector3(0f, 0.4f, 0f), new Vector3(0.7f, 0.8f, 0.7f), clay);
            art.Cyl(g, new Vector3(0f, 0.75f, 0f), 0.18f, 0.2f, clay * 0.9f);
            foreach (var c in g.GetComponentsInChildren<Collider>()) Object.Destroy(c);
            var col = g.gameObject.AddComponent<CapsuleCollider>();
            col.center = new Vector3(0f, 0.45f, 0f); col.radius = 0.35f; col.height = 0.95f;
            g.gameObject.AddComponent<Breakable>().Setup(BreakableKind.Jar, dates);
        }

        private static void Wall(StageArt art, Transform world, Vector3 p, Color ground)
        {
            var g = StageArt.Group("MudWall", world, p);
            art.Box(g, new Vector3(0f, 1f, 0f), new Vector3(2f, 2f, 0.6f), ground * 0.85f);
            for (int i = 0; i < 3; i++) art.Box(g, new Vector3(-0.6f + i * 0.6f, 2.1f, 0f), new Vector3(0.35f, 0.25f, 0.6f), ground * 0.8f);
            foreach (var c in g.GetComponentsInChildren<Collider>()) Object.Destroy(c);
            var col = g.gameObject.AddComponent<BoxCollider>();
            col.center = new Vector3(0f, 1f, 0f); col.size = new Vector3(2f, 2f, 0.6f);
            g.gameObject.AddComponent<Breakable>().Setup(BreakableKind.Wall, 5);
        }

        private static void Barriers(StageArt art, Transform world, StageLayout layout)
        {
            float from = float.NaN, to = float.NaN;
            foreach (var p in layout.Of(PieceKind.MountPost)) { from = p.z; break; }
            foreach (var p in layout.Of(PieceKind.RideEnd)) { to = p.z; break; }
            if (float.IsNaN(from) || float.IsNaN(to) || to - from < 30f) return;
            var steel = new Color(0.55f, 0.57f, 0.6f);
            foreach (float f in new[] { 0.4f, 0.75f })
            {
                float z = StageConditions.SafeZ(layout, Mathf.Lerp(from, to, f));
                float gy = layout.GroundAt(z);
                if (float.IsNaN(gy) || z >= to) continue;
                var g = StageArt.Group("MetalBarrier", world, new Vector3(0f, gy, z));
                float w = layout.HalfWidth * 2f;
                for (int i = 0; i < 3; i++) art.Box(g, new Vector3(0f, 0.35f + i * 0.4f, 0f), new Vector3(w, 0.12f, 0.12f), i % 2 == 0 ? steel : new Color(0.85f, 0.15f, 0.1f));
                art.Box(g, new Vector3(-w * 0.5f, 0.6f, 0f), new Vector3(0.15f, 1.2f, 0.15f), steel * 0.8f);
                art.Box(g, new Vector3(w * 0.5f, 0.6f, 0f), new Vector3(0.15f, 1.2f, 0.15f), steel * 0.8f);
                foreach (var c in g.GetComponentsInChildren<Collider>()) Object.Destroy(c);
                var col = g.gameObject.AddComponent<BoxCollider>();
                col.center = new Vector3(0f, 0.65f, 0f); col.size = new Vector3(w, 1.3f, 0.3f);
                g.gameObject.AddComponent<Breakable>().Setup(BreakableKind.Metal, 3);
            }
        }
    }
}
