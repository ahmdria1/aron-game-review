// (c) Ahmed Alamrani. All rights reserved.
using Aron.Animals;
using Aron.Level;
using UnityEngine;

namespace Aron.EditorTools
{
    /// <summary>
    /// The end of the Riyadh run: the Thumamah dunes on a camel, a wadi only the camel can clear in one jump, then
    /// the horse track with its hurdles, and a hitching post before the boss bridge.
    /// </summary>
    public static partial class AronLevelBuilder
    {
        private const float DesertHalfWidth = 5f;

        // (startZ, endZ, y) like the street segments.
        private static readonly float[,] DesertSegs =
        {
            { 140, 196, 0 }, { 201, 230, 1.6f }, { 234, 290, 0 },
        };

        public const float DesertEnd = 290f;

        private static void BuildDesert(Transform world, Configs cfg, GameObject dateTpl)
        {
            var desert = Group("Desert_Thumamah", world).transform;
            var sand = Mat("DuneSand", new Color(0.9f, 0.73f, 0.48f));
            var sandDark = Mat("DuneSandDark", new Color(0.8f, 0.62f, 0.4f));

            for (int i = 0; i < DesertSegs.GetLength(0); i++)
            {
                float z0 = DesertSegs[i, 0], z1 = DesertSegs[i, 1], y = DesertSegs[i, 2], len = z1 - z0;
                var seg = Group("DuneTrack_" + i, desert, new Vector3(0f, y, (z0 + z1) * 0.5f));
                var col = seg.AddComponent<BoxCollider>();
                col.center = new Vector3(0f, -0.5f, 0f);
                col.size = new Vector3(DesertHalfWidth * 2f, 1f, len);
                Block("Sand", seg.transform, new Vector3(0f, -3f, 0f), new Vector3(DesertHalfWidth * 2f, 6f, len), sand, PrimitiveType.Cube, false);
                // Soft ripples on top so it doesn't read as a box.
                for (float z = -len * 0.5f + 3f; z < len * 0.5f - 2f; z += 5f)
                    Block("Ripple", seg.transform, new Vector3(Mathf.Sin(z) * 1.5f, -0.05f, z), new Vector3(6f, 0.25f, 2.2f), sandDark, PrimitiveType.Sphere, false);
                foreach (float side in new[] { -1f, 1f })
                {
                    var wall = Group("DuneWall", seg.transform, new Vector3(side * (DesertHalfWidth + 0.8f), 1.2f, 0f));
                    wall.AddComponent<BoxCollider>().size = new Vector3(1.6f, 3.4f, len);
                    for (float z = -len * 0.5f; z < len * 0.5f; z += 6f)
                        Block("Dune", seg.transform, new Vector3(side * (DesertHalfWidth + 2.2f), 0.2f, z + 3f), new Vector3(6f, 3.2f, 8f), sand, PrimitiveType.Sphere, false);
                }
            }

            var rnd = new System.Random(23);
            float R(float a, float b) => a + (float)rnd.NextDouble() * (b - a);
            for (int i = 0; i < 40; i++)
            {
                float side = rnd.NextDouble() < 0.5 ? -1f : 1f;
                Block("BigDune", desert, new Vector3(side * R(14f, 60f), R(-4f, -1f), R(140f, 300f)), new Vector3(R(14f, 30f), R(6f, 12f), R(14f, 30f)),
                      rnd.NextDouble() < 0.5 ? sand : sandDark, PrimitiveType.Sphere, false);
            }
            string[] props = { "cactus_tall", "cactus_short", "rock_largeA", "rock_smallC", "plant_bushSmall" };
            for (int i = 0; i < 30; i++)
            {
                float side = rnd.NextDouble() < 0.5 ? -1f : 1f;
                var p = props[rnd.Next(props.Length)];
                AronArt.Place(AronArt.Nature + p + ".fbx", desert, new Vector3(side * R(9f, 30f), 0f, R(142f, 295f)), R(0f, 360f), R(2f, 4f));
            }
            BuildTent(desert, new Vector3(-13f, 0f, 160f), 25f);
            BuildTent(desert, new Vector3(14f, 0f, 252f), -30f);

            SouqGate(desert, 143f, "روضة نفود الثمامة", "Al Thumamah Dunes");
            StreetSign(desert, new Vector3(5.6f, 0f, 236f), "ميدان الفروسية", "Equestrian Track");

            // The wadi: a wide gap up to a high bank. Easy on the camel, a sprint and a double jump on foot.
            var wadi = Group("Wadi", desert, new Vector3(0f, -3.5f, 198.5f));
            Block("WadiBed", wadi.transform, Vector3.zero, new Vector3(DesertHalfWidth * 2f, 1f, 5f), Mat("Earth", MudDark), PrimitiveType.Cube, false);
            MakeCrate(desert, new Vector3(3.5f, 0f, 193.5f), Crate.CrateType.Bounce);

            // Mount posts.
            MountPost(desert, cfg.camelModel, new Vector3(-2f, 0f, 148f), "CamelPost");
            MountPost(desert, cfg.horseModel, new Vector3(2f, 0f, 238f), "HorsePost");

            // Hurdles on the horse track.
            var fence = Mat("HurdleWood", new Color(0.92f, 0.9f, 0.85f));
            foreach (float z in new[] { 248f, 258f, 268f, 278f })
            {
                var h = Group("Hurdle", desert, new Vector3(0f, 0f, z));
                var bc = h.AddComponent<BoxCollider>();
                bc.center = new Vector3(0f, 0.4f, 0f);
                bc.size = new Vector3(DesertHalfWidth * 2f, 0.8f, 0.3f);
                Block("Rail", h.transform, new Vector3(0f, 0.7f, 0f), new Vector3(DesertHalfWidth * 2f, 0.12f, 0.12f), fence, PrimitiveType.Cube, false);
                Block("Rail", h.transform, new Vector3(0f, 0.35f, 0f), new Vector3(DesertHalfWidth * 2f, 0.1f, 0.1f), Mat("HurdleRed", new Color(0.75f, 0.12f, 0.12f)), PrimitiveType.Cube, false);
            }

            // Hitching post: riders get off here, the animal stays tied.
            var hitch = Group("HitchingPost", desert, new Vector3(0f, 0f, 286f));
            Trigger(hitch, new Vector3(DesertHalfWidth * 2f, 4f, 1.5f), new Vector3(0f, 2f, 0f));
            hitch.AddComponent<RideZoneEnd>();
            var wood = Mat("PostWood", new Color(0.42f, 0.27f, 0.15f));
            foreach (float x in new[] { -4f, 4f })
                Block("Post", hitch.transform, new Vector3(x, 0.9f, 0f), new Vector3(0.25f, 0.9f, 0.25f), wood, PrimitiveType.Cylinder, false);
            Block("Bar", hitch.transform, new Vector3(0f, 1.6f, 0f), new Vector3(8.2f, 0.12f, 0.12f), wood, PrimitiveType.Cube, false);

            // Coins: over the dunes, a high arc across the wadi, and on the far side of each hurdle.
            var coins = Group("DesertCoins", desert).transform;
            for (float z = 150f; z < 288f; z += 3f)
            {
                if (InGap(DesertSegs, z)) continue;
                MakeCoin(coins, new Vector3(Mathf.Sin(z * 0.25f) * 3f, GroundY(DesertSegs, z) + 0.8f, z));
            }
            for (int i = 0; i < 4; i++) MakeCoin(coins, new Vector3(0f, 3.4f + Mathf.Sin(i / 3f * Mathf.PI), 196.5f + i * 1.5f));

            var enemies = Group("DesertEnemies", desert).transform;
            MakeEnemy(enemies, "Patrol_Dunes", new Vector3(-3f, 0f, 170f), cfg.patrol, cfg.guardModel, new Vector3(6f, 0f, 0f), dateTpl);
            MakeEnemy(enemies, "Chaser_Dunes", new Vector3(2f, 0f, 186f), cfg.chaser, cfg.chaserModel, new Vector3(0f, 0f, -5f), dateTpl);
            MakeEnemy(enemies, "Patrol_Ridge", new Vector3(-3f, 1.6f, 216f), cfg.patrol, cfg.guardModel, new Vector3(6f, 0f, 0f), dateTpl);
            MakeEnemy(enemies, "Chaser_Track", new Vector3(0f, 0f, 263f), cfg.chaser, cfg.chaserModel, new Vector3(3f, 0f, 0f), dateTpl);
            MakeEnemy(enemies, "Patrol_Track", new Vector3(-3f, 0f, 274f), cfg.patrol, cfg.guardModel, new Vector3(6f, 0f, 0f), dateTpl);

            MakeCrate(desert, new Vector3(-3.5f, 0f, 176f), Crate.CrateType.Life);
            MakeCrate(desert, new Vector3(3.5f, 1.6f, 224f), Crate.CrateType.Basic);
            MakeCheckpoint(desert, new Vector3(4.2f, 0f, 146f));
            MakeCheckpoint(desert, new Vector3(4.2f, 0f, 240f));
            MakeDialogueTrigger(desert, "Talk_Desert", new Vector3(0f, 1f, 145f), cfg.desert);
            MakeDialogueTrigger(desert, "Talk_Horse", new Vector3(0f, 1f, 236.5f), cfg.horseTrack);
        }

        private static void MountPost(Transform parent, GameObject prefab, Vector3 pos, string name)
        {
            var post = Group(name, parent, pos).transform;
            var wood = Mat("PostWood", new Color(0.42f, 0.27f, 0.15f));
            Block("Post", post, new Vector3(-1.6f, 0.9f, 0f), new Vector3(0.22f, 0.9f, 0.22f), wood, PrimitiveType.Cylinder, false);
            if (prefab != null) PlaceCharacter(prefab, post, Vector3.zero, 0f, prefab.name);
        }

        private static void BuildTent(Transform parent, Vector3 pos, float yaw)
        {
            // Bait al-sha'ar: black goat-hair tent with a Sadu divider and a coffee pot by the fire.
            var tent = Group("BedouinTent", parent, pos).transform;
            tent.localRotation = Quaternion.Euler(0f, yaw, 0f);
            var hair = Mat("TentHair", new Color(0.12f, 0.1f, 0.09f));
            var red = Mat("SaduRed", new Color(0.63f, 0.15f, 0.17f));
            Block("Roof", tent, new Vector3(0f, 2.2f, 0f), new Vector3(7f, 0.15f, 4f), hair, PrimitiveType.Cube, false);
            Block("Back", tent, new Vector3(0f, 1.1f, 2f), new Vector3(7f, 2.2f, 0.1f), hair, PrimitiveType.Cube, false);
            foreach (float x in new[] { -3.4f, 3.4f })
                Block("Side", tent, new Vector3(x, 1.1f, 1f), new Vector3(0.1f, 2.2f, 2f), hair, PrimitiveType.Cube, false);
            Block("Sadu", tent, new Vector3(0f, 1.6f, 1.95f), new Vector3(6.6f, 0.4f, 0.05f), red, PrimitiveType.Cube, false);
            Block("Rug", tent, new Vector3(0f, 0.02f, 0.4f), new Vector3(5f, 0.04f, 3f), red, PrimitiveType.Cube, false);
            Block("Fire", tent, new Vector3(0f, 0.1f, -2.6f), new Vector3(0.8f, 0.2f, 0.8f), Mat("Embers", new Color(0.95f, 0.4f, 0.1f)), PrimitiveType.Cylinder, false);
            Block("Dallah", tent, new Vector3(0.7f, 0.25f, -2.4f), new Vector3(0.25f, 0.25f, 0.25f), Mat("DallahBrass", new Color(0.85f, 0.65f, 0.25f)), PrimitiveType.Sphere, false);
        }
    }
}
