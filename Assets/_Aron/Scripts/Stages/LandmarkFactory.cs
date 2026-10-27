// (c) Ahmed Alamrani. All rights reserved.
using UnityEngine;

namespace Aron.Stages
{
    /// <summary>
    /// Builds each landmark kind out of simple shapes, at roughly real proportions scaled down to fit beside the
    /// track. Returns the height so the camera knows where to look.
    /// </summary>
    public static class LandmarkFactory
    {
        private static readonly Color Mud = new Color(0.82f, 0.6f, 0.4f), MudDark = new Color(0.68f, 0.47f, 0.32f);
        private static readonly Color Stone = new Color(0.78f, 0.72f, 0.62f), Rock = new Color(0.66f, 0.42f, 0.3f);
        private static readonly Color White = new Color(0.95f, 0.94f, 0.9f), Wood = new Color(0.45f, 0.3f, 0.18f);
        private static readonly Color Palm = new Color(0.25f, 0.5f, 0.22f), Trunk = new Color(0.5f, 0.36f, 0.22f);
        private static readonly Color Glass = new Color(0.45f, 0.6f, 0.72f), Water = new Color(0.2f, 0.5f, 0.68f);
        private static readonly Color Gold = new Color(0.9f, 0.72f, 0.25f), Blue = new Color(0.25f, 0.45f, 0.75f);

        public static float Build(StageArt a, Transform t, string kind, Color accent, Color ground)
        {
            switch (kind)
            {
                case "fort": return Fort(a, t, Mud, false);
                case "round_fort": return Fort(a, t, Stone, true);
                case "masmak": return Masmak(a, t);
                case "mud_town": return Houses(a, t, Mud, 2, false, accent);
                case "tower_houses": return Houses(a, t, Stone, 4, true, accent);
                case "painted_houses": return Houses(a, t, White, 2, false, accent, true);
                case "blue_town": return Houses(a, t, new Color(0.45f, 0.65f, 0.9f), 2, false, White);
                case "rawasheen": return Rawasheen(a, t);
                case "souq": return Souq(a, t, accent);
                case "gate": return Gate(a, t, Mud, accent);
                case "big_arch": return Gate(a, t, Stone, Gold, 2f);
                case "skyscraper": return Tower(a, t, 4, 2.4f, 2f, 22f, Glass);
                case "needle_tower": return Needle(a, t);
                case "arch_tower": return ArchTower(a, t);
                case "twin_spheres": return TwinSpheres(a, t);
                case "wind_towers": return WindTowers(a, t);
                case "dhows": return Dhows(a, t);
                case "fountain": return Fountain(a, t);
                case "causeway": return Causeway(a, t);
                case "columns": return Columns(a, t, 6, 7f);
                case "stone_pillars": return Columns(a, t, 5, 4f, true);
                case "amphitheater": return Amphitheater(a, t);
                case "rock_tomb": return RockTomb(a, t);
                case "rock_arch": return RockArch(a, t);
                case "pyramids": return Pyramids(a, t);
                case "spiral_minaret": return Minaret(a, t, true);
                case "waterwheel": return Waterwheel(a, t);
                case "cedars": return Trees(a, t, new Color(0.18f, 0.38f, 0.2f), true);
                case "palms": return Palms(a, t);
                case "dragon_trees": return Trees(a, t, new Color(0.28f, 0.48f, 0.24f), false);
                case "monument": return Monument(a, t, accent);
                case "volcano": return Volcano(a, t);
                case "railway": return Railway(a, t);
                case "water_tower": return WaterTower(a, t);
                case "lone_tree": return Trees(a, t, new Color(0.32f, 0.5f, 0.24f), false, 1);
                case "salt_lake": return Lake(a, t, new Color(0.95f, 0.95f, 0.92f));
                case "lake": return Lake(a, t, Water);
                case "chimneys": return Chimneys(a, t);
                case "rings": return Rings(a, t);
                case "terraces": return Terraces(a, t);
                case "reeds": return Reeds(a, t);
                case "dunes": return Dunes(a, t, ground);
                case "mountains": return Mountains(a, t);
                case "tents": return Tents(a, t, accent);
                case "acacia": return Acacias(a, t);
                case "savanna": Acacias(a, t); return Dunes(a, t, new Color(0.8f, 0.68f, 0.38f));
                case "bamboo_forest": return Bamboo(a, t);
                case "rainforest": return Trees(a, t, new Color(0.12f, 0.42f, 0.2f), false, 5);
                case "black_forest": return Trees(a, t, new Color(0.1f, 0.26f, 0.16f), true, 5);
                case "cherry_trees": return Trees(a, t, new Color(0.98f, 0.72f, 0.82f), false);
                case "lavender_fields": return Rows(a, t, new Color(0.6f, 0.48f, 0.82f));
                case "canyon": return Canyon(a, t);
                case "drakensberg": return Mountains(a, t);
                case "snow_peaks": return SnowPeaks(a, t);
                case "flat_mountain": return FlatMountain(a, t);
                case "sea_cliffs": return SeaCliffs(a, t);
                case "hill_station": Terraces(a, t); return Houses(a, t, White, 1, false, accent, true);
                case "bazaar_arches": return Arcade(a, t, Stone, accent, 5);
                case "palace_arches": return Arcade(a, t, new Color(0.88f, 0.55f, 0.5f), Gold, 7);
                case "covered_bazaar": Souq(a, t, accent); return Vault(a, t, Stone);
                case "township_market": return Stalls(a, t, accent);
                case "madrasa_portal": return Portal(a, t, new Color(0.2f, 0.55f, 0.75f), Gold);
                case "palace_fort": return Fort(a, t, new Color(0.75f, 0.35f, 0.3f), true);
                case "white_village": return Houses(a, t, White, 1, false, Blue, true);
                case "tile_houses": return Houses(a, t, new Color(0.85f, 0.88f, 0.92f), 3, true, Blue, true);
                case "row_houses": return Houses(a, t, new Color(0.55f, 0.3f, 0.25f), 3, true, White, true);
                case "colour_plaza": return Houses(a, t, accent, 2, false, Gold, true);
                case "stilt_house": return StiltHouse(a, t);
                case "houseboat": return Houseboat(a, t);
                case "windmills": return Windmills(a, t);
                case "fairytale_castle": return Castle(a, t);
                case "clock_tower": return ClockTower(a, t);
                case "red_bus": return Vehicle(a, t, new Color(0.8f, 0.12f, 0.12f), true);
                case "tram": return Vehicle(a, t, new Color(0.95f, 0.78f, 0.2f), false);
                case "eye_rings": return Wheel(a, t);
                case "iron_tower": return Lattice(a, t);
                case "sea_tower": return Lighthouse(a, t);
                case "twin_towers": foreach (float x in new[] { -4f, 4f }) Tower(a, StageArt.Group("Tower", t, new Vector3(x, 0f, 0f)), 8, 2.4f, 1.8f, 26f, Glass); return 29f;
                case "torch_statue": return Statue(a, t);
                case "step_pyramid": return StepPyramid(a, t);
                // Japan and Indonesia: nature and castle shapes, nothing religious.
                case "volcano_rice_terraces": return VolcanoTerraces(a, t);
                case "castle_keep": return CastleKeep(a, t);
                case "snow_mountain": return SnowMountain(a, t);
                case "bamboo_grove": return Bamboo(a, t);
                case "red_wooden_bridge": return RedBridge(a, t);
                // Places of worship: skyline only, simple silhouettes with no symbols or text.
                case "marble_mausoleum": return Mausoleum(a, t);
                case "blue_domes": return Domes(a, t);
                case "tent_mosque": return Domes(a, t);
                // Australia. Stylised shapes only: no copies of real buildings and no names. The red ridges are a generic
                // layered sandstone range, never a single dome and never a copy of a real rock.
                case "shell_hall": return ShellHall(a, t);
                case "steel_arch": return SteelArch(a, t);
                case "gum_forest": return GumForest(a, t);
                case "coral_reef": return CoralReef(a, t);
                case "red_rock_ridges": return RedRockRidges(a, t);
                default: return Fort(a, t, Mud, false);
            }
        }

        private static float ShellHall(StageArt a, Transform t)
        {
            a.Box(t, new Vector3(0f, 0.6f, 0f), new Vector3(22f, 1.2f, 12f), White * 0.9f);
            for (int i = 0; i < 4; i++) a.Ball(t, new Vector3(-7f + i * 4.6f, 4f + i * 0.8f, 0f), new Vector3(5.2f, 7f - i * 0.6f, 6f), White);
            return 9f;
        }

        private static float SteelArch(StageArt a, Transform t)
        {
            var steel = new Color(0.42f, 0.46f, 0.5f);
            a.Box(t, new Vector3(0f, 4f, 0f), new Vector3(30f, 0.8f, 4f), steel * 0.8f);
            for (int i = -6; i <= 6; i++)
            {
                float x = i * 2.2f, h = 12f * (1f - (x * x) / (14.5f * 14.5f));
                a.Box(t, new Vector3(x, 4f + h, 0f), new Vector3(2.4f, 0.7f, 1.2f), steel);
                if (i % 2 == 0) a.Box(t, new Vector3(x, 4f + h * 0.5f, 0f), new Vector3(0.3f, h, 0.3f), steel * 0.9f);
            }
            foreach (float x in new[] { -15f, 15f }) a.Box(t, new Vector3(x, 2f, 0f), new Vector3(3f, 4f, 4f), White * 0.7f);
            return 16f;
        }

        private static float GumForest(StageArt a, Transform t)
        {
            Atmosphere.FoliageWind.Register(t, Atmosphere.FoliageKind.Leaves);
            var bark = new Color(0.85f, 0.82f, 0.74f);
            var leaf = new Color(0.34f, 0.5f, 0.38f);
            for (int i = 0; i < 5; i++)
            {
                float x = -8f + i * 4f;
                float trunk = 11f + i % 3, top = 12f + i % 3 + 1.75f;
                a.FoliageCyl(t, new Vector3(x, 0f, (i % 2) * 2f), 0.45f, trunk, bark, 0f, trunk / top);
                a.Foliage(PrimitiveType.Sphere, t, new Vector3(x, 12f + i % 3, (i % 2) * 2f), new Vector3(5f, 3.5f, 5f), leaf, (12f + i % 3 - 1.75f) / top, 1f, 0.35f);
            }
            return 14f;
        }

        private static float CoralReef(StageArt a, Transform t)
        {
            var cols = new[] { new Color(0.95f, 0.45f, 0.4f), new Color(0.98f, 0.7f, 0.3f), new Color(0.6f, 0.4f, 0.8f), new Color(0.3f, 0.75f, 0.7f) };
            for (int i = 0; i < 8; i++)
            {
                a.Ball(t, new Vector3(-7f + i * 2f, 0.7f, (i % 3) - 1f), new Vector3(2.2f, 1.4f, 2.2f), cols[i % cols.Length]);
                if (i % 2 == 0) a.Cyl(t, new Vector3(-7f + i * 2f + 0.8f, 0f, 1.2f), 0.25f, 2.2f, cols[(i + 1) % cols.Length]);
            }
            return 3f;
        }

        private static float RedRockRidges(StageArt a, Transform t)
        {
            // Three staggered ranges of flat-topped layered slabs, each layer a little narrower and a different red.
            var reds = new[] { new Color(0.72f, 0.33f, 0.2f), new Color(0.78f, 0.42f, 0.25f), new Color(0.66f, 0.28f, 0.18f), new Color(0.82f, 0.5f, 0.3f) };
            float[] xs = { -26f, -8f, 12f, 30f };
            float[] zs = { 0f, 6f, -4f, 3f };
            float[] widths = { 16f, 22f, 14f, 18f };
            int[] layers = { 3, 4, 2, 3 };
            for (int r = 0; r < xs.Length; r++)
            {
                for (int l = 0; l < layers[r]; l++)
                {
                    float w = widths[r] - l * 3.2f;
                    a.Box(t, new Vector3(xs[r] + l * 0.8f, 1.1f + l * 2.2f, zs[r]), new Vector3(w, 2.2f, 7f - l), reds[(r + l) % reds.Length]);
                }
            }
            return 9.5f;
        }

        private static float Fort(StageArt a, Transform t, Color c, bool round)
        {
            a.Box(t, new Vector3(0f, 2.5f, 0f), new Vector3(8f, 5f, 8f), c);
            foreach (var p in new[] { new Vector3(-4f, 0f, -4f), new Vector3(4f, 0f, -4f), new Vector3(-4f, 0f, 4f), new Vector3(4f, 0f, 4f) })
            {
                if (round) a.Cyl(t, p, 1.3f, 7f, c * 0.95f);
                else a.Frustum(t, p, 4, 1.6f, 1.2f, 7.5f, c * 0.95f, 45f);
            }
            for (int i = -3; i <= 3; i++) a.Box(t, new Vector3(i * 1.1f, 5.3f, -4f), new Vector3(0.5f, 0.6f, 0.5f), c);
            a.Box(t, new Vector3(0f, 1.3f, -4.05f), new Vector3(1.6f, 2.6f, 0.2f), Wood);
            return 7.5f;
        }

        private static float Masmak(StageArt a, Transform t)
        {
            Fort(a, t, Mud, false);
            a.Frustum(t, new Vector3(0f, 0f, 2f), 4, 1.8f, 1.3f, 10f, MudDark, 45f);
            for (int i = 0; i < 6; i++) a.Box(t, new Vector3(-3f + i * 1.2f, 3.6f, -4.05f), new Vector3(0.18f, 0.5f, 0.1f), MudDark * 0.6f);
            return 10f;
        }

        private static float Houses(StageArt a, Transform t, Color c, int floors, bool tall, Color trim, bool painted = false)
        {
            float h = 0f;
            for (int i = 0; i < 4; i++)
            {
                float x = -4.5f + i * 3f, f = floors + (i % 2);
                float hh = f * (tall ? 2.6f : 2.2f);
                a.Box(t, new Vector3(x, hh * 0.5f, (i % 2) * 1.5f), new Vector3(2.8f, hh, 3f), c * (1f - i * 0.03f));
                for (int k = 0; k < f; k++) a.Box(t, new Vector3(x, 1.3f + k * 2.2f, -1.52f + (i % 2) * 1.5f), new Vector3(0.7f, 0.8f, 0.05f), painted ? trim : MudDark * 0.5f);
                a.Box(t, new Vector3(x, hh + 0.15f, (i % 2) * 1.5f), new Vector3(3f, 0.3f, 3.2f), painted ? trim : c * 0.9f);
                if (painted) a.Box(t, new Vector3(x, hh * 0.5f, -1.52f + (i % 2) * 1.5f), new Vector3(2.8f, 0.3f, 0.04f), trim);
                h = Mathf.Max(h, hh);
            }
            return h;
        }

        private static float Rawasheen(StageArt a, Transform t)
        {
            for (int i = 0; i < 3; i++)
            {
                float x = -4f + i * 4f, hh = 8f + i;
                a.Box(t, new Vector3(x, hh * 0.5f, 0f), new Vector3(3.8f, hh, 4f), new Color(0.9f, 0.86f, 0.78f));
                for (int k = 0; k < 3; k++)
                    a.Box(t, new Vector3(x, 2.5f + k * 2.4f, -2.3f), new Vector3(1.8f, 1.8f, 0.6f), i == 1 ? new Color(0.2f, 0.45f, 0.4f) : Wood);
            }
            return 10f;
        }

        private static float Souq(StageArt a, Transform t, Color accent)
        {
            for (int i = 0; i < 4; i++)
            {
                float x = -4.5f + i * 3f;
                a.Box(t, new Vector3(x, 1.6f, 0f), new Vector3(2.7f, 3.2f, 3f), Mud);
                a.Box(t, new Vector3(x, 2.6f, -2f), new Vector3(2.8f, 0.1f, 1.6f), i % 2 == 0 ? accent : White, 0f).transform.localRotation = Quaternion.Euler(-15f, 0f, 0f);
                a.Box(t, new Vector3(x, 0.5f, -2.3f), new Vector3(2.2f, 1f, 0.8f), Wood);
                a.Ball(t, new Vector3(x - 0.5f, 1.15f, -2.3f), new Vector3(0.5f, 0.3f, 0.5f), Gold);
                a.Ball(t, new Vector3(x + 0.5f, 1.15f, -2.3f), new Vector3(0.5f, 0.3f, 0.5f), accent * 0.8f);
            }
            return 3.2f;
        }

        private static float Gate(StageArt a, Transform t, Color c, Color trim, float size = 1f)
        {
            float w = 3f * size, h = 6f * size;
            a.Box(t, new Vector3(-w, h * 0.5f, 0f), new Vector3(2f * size, h, 2f), c);
            a.Box(t, new Vector3(w, h * 0.5f, 0f), new Vector3(2f * size, h, 2f), c);
            a.Box(t, new Vector3(0f, h - size, 0f), new Vector3(w * 2f + 2f * size, 2f * size, 2f), c);
            a.Box(t, new Vector3(0f, h - 1.6f * size, -1.02f), new Vector3(w * 2f, 0.3f, 0.05f), trim);
            return h;
        }

        private static float Tower(StageArt a, Transform t, int sides, float r0, float r1, float h, Color c)
        {
            a.Frustum(t, Vector3.zero, sides, r0, r1, h, c, 45f);
            a.Frustum(t, new Vector3(0f, h, 0f), sides, r1, 0.2f, 3f, c * 0.8f, 45f);
            return h + 3f;
        }

        private static float Needle(StageArt a, Transform t)
        {
            a.Frustum(t, Vector3.zero, 6, 3f, 0.9f, 24f, Glass);
            a.Frustum(t, new Vector3(0f, 24f, 0f), 6, 0.9f, 0.05f, 8f, new Color(0.75f, 0.78f, 0.82f));
            return 32f;
        }

        private static float ArchTower(StageArt a, Transform t)
        {
            // Two tapering legs joined by the sky bridge, with the opening at the top.
            foreach (float x in new[] { -2.2f, 2.2f })
            {
                var leg = a.Box(t, new Vector3(x, 10f, 0f), new Vector3(1.6f, 20f, 3f), Glass);
                leg.transform.localRotation = Quaternion.Euler(0f, 0f, x < 0 ? -6f : 6f);
            }
            a.Box(t, new Vector3(0f, 6f, 0f), new Vector3(4f, 12f, 3f), Glass * 0.9f);
            a.Box(t, new Vector3(0f, 17f, 0f), new Vector3(3.2f, 0.4f, 1.2f), new Color(0.7f, 0.72f, 0.76f));
            return 21f;
        }

        private static float TwinSpheres(StageArt a, Transform t)
        {
            a.Frustum(t, Vector3.zero, 12, 0.8f, 0.3f, 18f, White);
            a.Ball(t, new Vector3(0f, 10f, 0f), Vector3.one * 4f, Blue);
            a.Ball(t, new Vector3(0f, 14.5f, 0f), Vector3.one * 2.4f, Blue);
            a.Frustum(t, new Vector3(4f, 0f, 1f), 12, 0.6f, 0.25f, 11f, White);
            a.Ball(t, new Vector3(4f, 7f, 1f), Vector3.one * 2.6f, Blue);
            return 18f;
        }

        private static float WindTowers(StageArt a, Transform t)
        {
            for (int i = 0; i < 3; i++)
            {
                float x = -4f + i * 4f;
                a.Box(t, new Vector3(x, 2f, 0f), new Vector3(3.6f, 4f, 3.6f), Stone);
                a.Box(t, new Vector3(x, 5.5f, 0f), new Vector3(1.6f, 3f, 1.6f), Stone * 0.95f);
                for (int k = 0; k < 3; k++) a.Box(t, new Vector3(x - 0.5f + k * 0.5f, 5.8f, -0.82f), new Vector3(0.15f, 2f, 0.05f), MudDark * 0.6f);
            }
            return 7f;
        }

        private static float Dhows(StageArt a, Transform t)
        {
            a.Water(t, new Vector3(0f, -0.2f, 0f), new Vector3(12f, 0.2f, 10f), Water);
            for (int i = 0; i < 2; i++)
            {
                var hull = StageArt.Group("Dhow", t, new Vector3(-2.5f + i * 5f, 0.5f, i * 2f), 10f + i * 15f);
                a.Box(hull, new Vector3(0f, 0f, 0f), new Vector3(1.8f, 1f, 6f), Wood);
                a.Frustum(hull, new Vector3(0f, -0.5f, 3f), 4, 0.9f, 0.1f, 1.5f, Wood * 0.9f).transform.localRotation = Quaternion.Euler(70f, 45f, 0f);
                a.Cyl(hull, new Vector3(0f, 0.5f, 0.5f), 0.1f, 6f, Wood * 0.7f);
                var sail = a.Box(hull, new Vector3(0f, 4f, 0.8f), new Vector3(0.05f, 3.5f, 3f), White);
                sail.name = "Sail";
                Atmosphere.WindSway.Add(sail.transform, Atmosphere.SwayKind.Sail);
            }
            return 6.5f;
        }

        private static float Fountain(StageArt a, Transform t)
        {
            a.Cyl(t, Vector3.zero, 4f, 0.6f, Stone);
            a.WaterDisc(t, new Vector3(0f, 0.1f, 0f), 3.6f, 0.55f, Water);
            a.Frustum(t, new Vector3(0f, 0.6f, 0f), 10, 0.35f, 0.05f, 16f, new Color(0.85f, 0.93f, 1f));
            return 16f;
        }

        private static float Causeway(StageArt a, Transform t)
        {
            a.Water(t, new Vector3(0f, -0.2f, 0f), new Vector3(12f, 0.2f, 12f), Water);
            a.Box(t, new Vector3(0f, 3f, 0f), new Vector3(2.5f, 0.5f, 12f), Stone);
            for (int i = -2; i <= 2; i++) a.Box(t, new Vector3(0f, 1.4f, i * 2.6f), new Vector3(1.2f, 2.8f, 0.8f), Stone * 0.9f);
            a.Box(t, new Vector3(0f, 6f, 3f), new Vector3(2.2f, 6f, 2.2f), White);
            return 9f;
        }

        private static float Columns(StageArt a, Transform t, int n, float h, bool rough = false)
        {
            for (int i = 0; i < n; i++)
            {
                float x = -5f + i * 10f / Mathf.Max(1, n - 1);
                if (rough) a.Box(t, new Vector3(x, h * 0.5f, (i % 2) * 0.8f), new Vector3(0.9f, h * (0.8f + 0.1f * (i % 3)), 0.8f), Rock);
                else
                {
                    a.Cyl(t, new Vector3(x, 0f, 0f), 0.45f, h, Stone);
                    a.Box(t, new Vector3(x, h + 0.2f, 0f), new Vector3(1.2f, 0.4f, 1.2f), Stone * 0.95f);
                }
            }
            if (!rough) a.Box(t, new Vector3(0f, h + 0.6f, 0f), new Vector3(11f, 0.6f, 1.3f), Stone);
            return h + 1f;
        }

        private static float Amphitheater(StageArt a, Transform t)
        {
            for (int i = 0; i < 5; i++) a.Cyl(t, new Vector3(0f, 0f, 2f), 6f - i * 0.9f, 0.6f * (i + 1), Stone * (1f - i * 0.03f));
            a.Box(t, new Vector3(0f, 0.6f, -3f), new Vector3(8f, 1.2f, 2f), Stone * 0.85f);
            return 3f;
        }

        private static float RockTomb(StageArt a, Transform t)
        {
            a.Ball(t, new Vector3(0f, 4f, 1f), new Vector3(12f, 10f, 9f), Rock);
            a.Box(t, new Vector3(0f, 3.5f, -3.3f), new Vector3(4f, 6f, 0.4f), Rock * 1.1f);
            a.Box(t, new Vector3(0f, 1.5f, -3.55f), new Vector3(1.4f, 2.4f, 0.1f), Rock * 0.4f);
            a.Box(t, new Vector3(0f, 6.4f, -3.55f), new Vector3(4.4f, 0.4f, 0.2f), Rock * 1.15f);
            return 9f;
        }

        private static float RockArch(StageArt a, Transform t)
        {
            a.Ball(t, new Vector3(-3.5f, 3f, 0f), new Vector3(4f, 7f, 4f), Rock);
            a.Ball(t, new Vector3(3.5f, 3f, 0f), new Vector3(4f, 7f, 4f), Rock);
            a.Ball(t, new Vector3(0f, 7f, 0f), new Vector3(10f, 3f, 4f), Rock * 0.95f);
            return 8.5f;
        }

        private static float Pyramids(StageArt a, Transform t)
        {
            a.Frustum(t, Vector3.zero, 4, 6f, 0.05f, 9f, new Color(0.88f, 0.76f, 0.52f), 45f);
            a.Frustum(t, new Vector3(5f, 0f, 4f), 4, 3.5f, 0.05f, 5f, new Color(0.84f, 0.72f, 0.5f), 45f);
            return 9f;
        }

        private static float Minaret(StageArt a, Transform t, bool spiral)
        {
            int rings = spiral ? 6 : 3;
            for (int i = 0; i < rings; i++) a.Frustum(t, new Vector3(0f, i * 3f, 0f), 12, 3f - i * 0.4f, 2.7f - i * 0.4f, 3f, Mud * (1f - i * 0.02f));
            return rings * 3f;
        }

        private static float Waterwheel(StageArt a, Transform t)
        {
            a.Water(t, new Vector3(0f, -0.2f, 0f), new Vector3(12f, 0.2f, 6f), Water);
            var wheel = a.Cyl(t, new Vector3(0f, 0f, 0f), 4.5f, 0.6f, Wood);
            wheel.transform.localRotation = Quaternion.Euler(0f, 0f, 90f);
            wheel.transform.localPosition = new Vector3(0f, 4.5f, 0f);
            wheel.AddComponent<Spin>();
            a.Box(t, new Vector3(-1f, 2.5f, 0f), new Vector3(0.6f, 5f, 0.6f), Stone);
            return 9f;
        }

        // Trees and palms use the wind shader: the weight rises from the trunk (0) to the tip (1) as a share of the tree's
        // height, and the palm fronds and leaf rims flutter. The grove root is registered so the rustle knows where foliage is.
        private static float Trees(StageArt a, Transform t, Color leaves, bool cone, int n = 4)
        {
            Atmosphere.FoliageWind.Register(t, Atmosphere.FoliageKind.Leaves);
            float top = cone ? 6.5f : 4f;
            for (int i = 0; i < n; i++)
            {
                var p = new Vector3(n == 1 ? 0f : -4f + i * 2.7f, 0f, (i % 2) * 2f);
                a.FoliageCyl(t, p, 0.25f, 2.5f, Trunk, 0f, 2.5f / top);
                if (cone) a.FoliageFrustum(t, p + Vector3.up * 1.5f, 8, 2f, 0.1f, 5f, leaves, 1.5f / top, 1f, 0.25f);
                else a.Foliage(PrimitiveType.Sphere, t, p + Vector3.up * 3.2f, new Vector3(3.4f, 1.6f, 3.4f), leaves, 2.4f / top, 1f, 0.35f);
            }
            return top;
        }

        private static float Palms(StageArt a, Transform t)
        {
            Atmosphere.FoliageWind.Register(t, Atmosphere.FoliageKind.Palm);
            for (int i = 0; i < 4; i++)
            {
                var p = new Vector3(-4.5f + i * 3f, 0f, (i % 2) * 1.8f);
                float h = 5f + (i % 3);
                a.FoliageFrustum(t, p, 6, 0.3f, 0.22f, h, Trunk, 0f, 0.95f);
                for (int k = 0; k < 6; k++)
                {
                    // Each frond flutters towards its tip, with its own phase (the shader takes it from the object's position).
                    var leaf = a.Foliage(PrimitiveType.Cube, t, p + new Vector3(0f, h, 0f), new Vector3(0.5f, 0.08f, 2.6f), Palm, 1f, 1f, 1f, true, k * 60f);
                    leaf.transform.localRotation = Quaternion.Euler(25f, k * 60f, 0f);
                    leaf.transform.localPosition += leaf.transform.forward * 1.1f;
                    leaf.name = "PalmLeaf";
                }
            }
            return 7.5f;
        }

        private static float Monument(StageArt a, Transform t, Color accent)
        {
            a.Box(t, new Vector3(0f, 1f, 0f), new Vector3(5f, 2f, 5f), Stone);
            a.Frustum(t, new Vector3(0f, 2f, 0f), 4, 1.2f, 0.4f, 12f, White, 45f);
            a.Box(t, new Vector3(0f, 1f, -2.52f), new Vector3(4f, 0.5f, 0.05f), accent);
            return 14f;
        }

        private static float Volcano(StageArt a, Transform t)
        {
            a.Frustum(t, Vector3.zero, 14, 7f, 2f, 9f, new Color(0.32f, 0.27f, 0.25f));
            a.Cyl(t, new Vector3(0f, 8.9f, 0f), 1.8f, 0.2f, new Color(1f, 0.4f, 0.1f));
            return 9f;
        }

        private static float Railway(StageArt a, Transform t)
        {
            for (int i = -5; i <= 5; i++) a.Box(t, new Vector3(i, 0.05f, 0f), new Vector3(0.3f, 0.1f, 2.4f), Wood);
            foreach (float z in new[] { -0.7f, 0.7f }) a.Box(t, new Vector3(0f, 0.15f, z), new Vector3(11f, 0.1f, 0.1f), new Color(0.35f, 0.35f, 0.38f));
            var loco = StageArt.Group("Locomotive", t, new Vector3(1f, 0.2f, 0f));
            a.Box(loco, new Vector3(0f, 1.2f, 0f), new Vector3(5f, 1.6f, 1.8f), new Color(0.15f, 0.15f, 0.17f)).transform.localRotation = Quaternion.identity;
            a.Box(loco, new Vector3(-1.8f, 2.4f, 0f), new Vector3(1.6f, 1.4f, 1.9f), new Color(0.15f, 0.15f, 0.17f));
            a.Cyl(loco, new Vector3(1.8f, 2f, 0f), 0.25f, 1f, new Color(0.15f, 0.15f, 0.17f));
            a.Box(t, new Vector3(-3f, 2f, 3f), new Vector3(5f, 4f, 3f), Stone);
            return 4f;
        }

        private static float WaterTower(StageArt a, Transform t)
        {
            a.Frustum(t, Vector3.zero, 10, 0.8f, 0.6f, 12f, White);
            a.Frustum(t, new Vector3(0f, 10f, 0f), 16, 1.5f, 3.4f, 2.5f, Blue);
            a.Frustum(t, new Vector3(0f, 12.5f, 0f), 16, 3.4f, 0.5f, 1.8f, Blue * 0.9f);
            return 14.5f;
        }

        private static float Lake(StageArt a, Transform t, Color c)
        {
            a.WaterDisc(t, new Vector3(0f, -0.15f, 0f), 6f, 0.2f, c);
            a.Ball(t, new Vector3(-4f, 0.3f, 4f), new Vector3(2f, 1f, 2f), Palm);
            return 2f;
        }

        private static float Chimneys(StageArt a, Transform t)
        {
            for (int i = 0; i < 4; i++) a.Frustum(t, new Vector3(-4.5f + i * 3f, 0f, (i % 2) * 2f), 8, 1.2f, 0.4f, 4f + i * 1.2f, new Color(0.72f, 0.68f, 0.6f));
            return 7.6f;
        }

        private static float Rings(StageArt a, Transform t)
        {
            for (int i = 0; i < 3; i++) a.Cyl(t, new Vector3(0f, 0f, 0f), 6f - i * 1.5f, 0.6f + i * 0.4f, Rock * (1f + i * 0.08f));
            return 1.8f;
        }

        private static float Terraces(StageArt a, Transform t)
        {
            for (int i = 0; i < 5; i++)
            {
                a.Box(t, new Vector3(0f, i * 1.2f + 0.6f, i * 1.6f), new Vector3(12f, 1.2f, 1.8f), Stone * 0.9f);
                a.Box(t, new Vector3(0f, i * 1.2f + 1.25f, i * 1.6f), new Vector3(11.6f, 0.1f, 1.6f), new Color(0.36f, 0.58f, 0.3f));
            }
            return 6f;
        }

        private static float Reeds(StageArt a, Transform t)
        {
            a.Water(t, new Vector3(0f, -0.15f, 0f), new Vector3(12f, 0.2f, 10f), Water);
            for (int i = 0; i < 18; i++) a.Cyl(t, new Vector3(-5f + (i * 7) % 11, 0f, -4f + (i * 5) % 9), 0.06f, 2.2f + (i % 3) * 0.5f, new Color(0.55f, 0.6f, 0.3f));
            var hut = StageArt.Group("Mudhif", t, new Vector3(0f, 0f, 2f));
            a.Ball(hut, new Vector3(0f, 1.2f, 0f), new Vector3(4f, 4.5f, 6f), new Color(0.78f, 0.7f, 0.45f));
            return 3.5f;
        }

        private static float Dunes(StageArt a, Transform t, Color ground)
        {
            for (int i = 0; i < 4; i++) a.Ball(t, new Vector3(-5f + i * 3.5f, 0f, (i % 2) * 3f), new Vector3(8f, 4f + i, 7f), ground * (0.95f + 0.03f * (i % 2)));
            return 3.5f;
        }

        private static float Mountains(StageArt a, Transform t)
        {
            a.Frustum(t, new Vector3(-2f, 0f, 3f), 7, 7f, 0.6f, 12f, Rock * 0.8f);
            a.Frustum(t, new Vector3(4f, 0f, 1f), 6, 5f, 0.4f, 8f, Rock * 0.9f);
            return 12f;
        }

        private static float Tents(StageArt a, Transform t, Color accent)
        {
            for (int i = 0; i < 2; i++)
            {
                var tent = StageArt.Group("BaitShaar", t, new Vector3(-3f + i * 6f, 0f, i * 2f), i * 20f);
                var hair = new Color(0.12f, 0.1f, 0.09f);
                a.Box(tent, new Vector3(0f, 2.2f, 0f), new Vector3(5f, 0.15f, 3.5f), hair);
                a.Box(tent, new Vector3(0f, 1.1f, 1.75f), new Vector3(5f, 2.2f, 0.1f), hair);
                a.Box(tent, new Vector3(0f, 1.6f, 1.7f), new Vector3(4.6f, 0.35f, 0.05f), accent);
                a.Box(tent, new Vector3(0f, 0.02f, 0.3f), new Vector3(4f, 0.04f, 2.6f), accent * 0.8f);
            }
            return 2.4f;
        }

        private static float Acacias(StageArt a, Transform t)
        {
            Atmosphere.FoliageWind.Register(t, Atmosphere.FoliageKind.Leaves);
            const float top = 4.25f;
            for (int i = 0; i < 3; i++)
            {
                var p = new Vector3(-5f + i * 5f, 0f, (i % 2) * 2.5f);
                a.FoliageCyl(t, p, 0.25f, 3.5f, Trunk, 0f, 3.5f / top);
                a.Foliage(PrimitiveType.Sphere, t, p + Vector3.up * 3.8f, new Vector3(5f, 0.9f, 5f), new Color(0.35f, 0.5f, 0.22f), 3.35f / top, 1f, 0.35f);
            }
            return 4.3f;
        }

        private static float Bamboo(StageArt a, Transform t)
        {
            Atmosphere.FoliageWind.Register(t, Atmosphere.FoliageKind.Leaves);
            for (int i = 0; i < 16; i++)
                a.FoliageCyl(t, new Vector3(-6f + (i * 7) % 12, 0f, -3f + (i * 5) % 6), 0.18f, 7f + (i % 4), new Color(0.45f, 0.65f, 0.3f), 0f, 1f);
            return 10f;
        }

        private static float Rows(StageArt a, Transform t, Color c)
        {
            for (int i = 0; i < 6; i++) a.Box(t, new Vector3(0f, 0.3f, -5f + i * 2f), new Vector3(14f, 0.6f, 0.9f), c);
            return 0.6f;
        }

        private static float Canyon(StageArt a, Transform t)
        {
            for (int i = 0; i < 4; i++) a.Box(t, new Vector3(-6f + i * 4f, 3f + i % 2, i * 1.5f), new Vector3(4f, 6f + 2f * (i % 2), 4f), Rock * (0.85f + 0.05f * i));
            return 8f;
        }

        private static float SnowPeaks(StageArt a, Transform t)
        {
            float h = Mountains(a, t);
            a.Frustum(t, new Vector3(-2f, 8f, 3f), 7, 2.6f, 0.6f, 4f, White);
            a.Frustum(t, new Vector3(4f, 5.5f, 1f), 6, 1.9f, 0.4f, 2.5f, White);
            return h;
        }

        private static float FlatMountain(StageArt a, Transform t)
        {
            a.Frustum(t, Vector3.zero, 10, 12f, 9f, 9f, Rock * 0.8f);
            a.Box(t, new Vector3(0f, 9.3f, 0f), new Vector3(16f, 0.6f, 6f), White * 0.95f);
            return 9.6f;
        }

        private static float SeaCliffs(StageArt a, Transform t)
        {
            a.Water(t, new Vector3(0f, -0.2f, -4f), new Vector3(16f, 0.2f, 6f), Water);
            a.Box(t, new Vector3(0f, 5f, 2f), new Vector3(16f, 10f, 6f), Stone * 0.9f);
            a.Box(t, new Vector3(0f, 10.1f, 2f), new Vector3(16f, 0.3f, 6f), new Color(0.36f, 0.58f, 0.3f));
            return 10.3f;
        }

        private static float Arcade(StageArt a, Transform t, Color c, Color trim, int n)
        {
            for (int i = 0; i <= n; i++) a.Box(t, new Vector3(-n + i * 2f, 2f, 0f), new Vector3(0.6f, 4f, 1.2f), c);
            a.Box(t, new Vector3(0f, 4.5f, 0f), new Vector3(n * 2f + 0.6f, 1f, 1.2f), c);
            a.Box(t, new Vector3(0f, 4.1f, -0.62f), new Vector3(n * 2f, 0.2f, 0.05f), trim);
            return 5f;
        }

        private static float Vault(StageArt a, Transform t, Color c)
        {
            var roof = a.Cyl(t, Vector3.zero, 1.8f, 13f, c * 0.9f);
            roof.transform.localRotation = Quaternion.Euler(0f, 0f, 90f);
            roof.transform.localPosition = new Vector3(6.5f, 3.4f, 0f);
            return 5.2f;
        }

        private static float Stalls(StageArt a, Transform t, Color accent)
        {
            var colours = new[] { accent, Gold, Blue, new Color(0.85f, 0.3f, 0.25f) };
            for (int i = 0; i < 4; i++)
            {
                float x = -4.5f + i * 3f;
                a.Box(t, new Vector3(x, 1.2f, 0f), new Vector3(2.6f, 2.4f, 2.4f), colours[i]);
                a.Box(t, new Vector3(x, 2.5f, -0.4f), new Vector3(2.9f, 0.12f, 3f), Stone * 0.7f);
            }
            return 2.6f;
        }

        private static float Portal(StageArt a, Transform t, Color tile, Color trim)
        {
            a.Box(t, new Vector3(0f, 6f, 0f), new Vector3(10f, 12f, 2f), Stone);
            a.Box(t, new Vector3(0f, 4.5f, -1.02f), new Vector3(4.5f, 9f, 0.05f), tile);
            a.Box(t, new Vector3(0f, 9.4f, -1.04f), new Vector3(5.2f, 0.4f, 0.05f), trim);
            return 12f;
        }

        private static float StiltHouse(StageArt a, Transform t)
        {
            foreach (var p in new[] { new Vector3(-2.5f, 0f, -2f), new Vector3(2.5f, 0f, -2f), new Vector3(-2.5f, 0f, 2f), new Vector3(2.5f, 0f, 2f) })
                a.Cyl(t, p, 0.2f, 2.5f, Wood);
            a.Box(t, new Vector3(0f, 3.8f, 0f), new Vector3(6f, 2.6f, 5f), Wood * 1.2f);
            a.Frustum(t, new Vector3(0f, 5.1f, 0f), 4, 4.6f, 0.3f, 3f, new Color(0.55f, 0.3f, 0.2f), 45f);
            return 8.1f;
        }

        private static float Houseboat(StageArt a, Transform t)
        {
            a.Water(t, new Vector3(0f, -0.2f, 0f), new Vector3(16f, 0.2f, 8f), Water);
            a.Box(t, new Vector3(0f, 0.5f, 0f), new Vector3(10f, 1f, 3f), Wood);
            var roof = a.Cyl(t, Vector3.zero, 1.5f, 7f, new Color(0.75f, 0.62f, 0.4f));
            roof.transform.localRotation = Quaternion.Euler(0f, 0f, 90f);
            roof.transform.localPosition = new Vector3(3.5f, 1.2f, 0f);
            return 2.7f;
        }

        private static float Windmills(StageArt a, Transform t)
        {
            for (int i = 0; i < 2; i++)
            {
                var m = StageArt.Group("Windmill", t, new Vector3(-4f + i * 8f, 0f, i * 2f));
                a.Frustum(m, Vector3.zero, 12, 2f, 1.5f, 7f, White);
                a.Frustum(m, new Vector3(0f, 7f, 0f), 12, 1.6f, 0.2f, 2f, Wood);
                var sails = StageArt.Group("Sails", m, new Vector3(0f, 7f, -1.7f));
                sails.localRotation = Quaternion.Euler(-90f, 0f, 0f);
                for (int k = 0; k < 4; k++) a.Box(sails, Vector3.zero, new Vector3(0.6f, 0.1f, 8f), Wood * 1.3f, k * 45f);
                sails.gameObject.AddComponent<Spin>();
            }
            return 11f;
        }

        private static float Castle(StageArt a, Transform t)
        {
            a.Box(t, new Vector3(0f, 4f, 0f), new Vector3(8f, 8f, 5f), White);
            foreach (float x in new[] { -4f, 4f, 0f })
            {
                float h = x == 0f ? 13f : 10f;
                a.Cyl(t, new Vector3(x, 0f, x == 0f ? 1.5f : -2.5f), 1.2f, h, White * 0.97f);
                a.Frustum(t, new Vector3(x, h, x == 0f ? 1.5f : -2.5f), 10, 1.5f, 0.05f, 4f, Blue);
            }
            return 17f;
        }

        private static float ClockTower(StageArt a, Transform t)
        {
            a.Box(t, new Vector3(0f, 9f, 0f), new Vector3(3.6f, 18f, 3.6f), Stone);
            a.Cyl(t, new Vector3(0f, 15f, -1.85f), 1.3f, 0.1f, White).transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            a.Frustum(t, new Vector3(0f, 18f, 0f), 4, 2.4f, 0.1f, 5f, Stone * 0.7f, 45f);
            return 23f;
        }

        private static float Vehicle(StageArt a, Transform t, Color c, bool doubleDeck)
        {
            float h = doubleDeck ? 4.2f : 2.6f;
            a.Box(t, new Vector3(0f, 0.6f + h * 0.5f, 0f), new Vector3(9f, h, 2.5f), c);
            for (int k = 0; k < (doubleDeck ? 2 : 1); k++) a.Box(t, new Vector3(0f, 2f + k * 2f, -1.27f), new Vector3(8f, 0.9f, 0.05f), Glass);
            foreach (float x in new[] { -3f, 3f }) a.Ball(t, new Vector3(x, 0.5f, -1.2f), new Vector3(1f, 1f, 0.4f), new Color(0.1f, 0.1f, 0.1f));
            if (!doubleDeck) a.Box(t, new Vector3(0f, 4.6f, 0f), new Vector3(0.1f, 1.6f, 0.1f), Stone * 0.5f);
            return h + 1f;
        }

        private static float Wheel(StageArt a, Transform t)
        {
            var wheel = StageArt.Group("Wheel", t, new Vector3(0f, 11f, 0f));
            wheel.localRotation = Quaternion.Euler(-90f, 0f, 0f);
            for (int k = 0; k < 12; k++) a.Box(wheel, Vector3.zero, new Vector3(0.2f, 0.2f, 20f), White, k * 15f);
            for (int k = 0; k < 12; k++)
            {
                float ang = k * 30f * Mathf.Deg2Rad;
                a.Ball(wheel, new Vector3(Mathf.Cos(ang) * 10f, 0f, Mathf.Sin(ang) * 10f), new Vector3(1.2f, 1.2f, 1.2f), Glass);
            }
            wheel.gameObject.AddComponent<Spin>().degreesPerSecond = 6f;
            a.Frustum(t, Vector3.zero, 4, 3f, 0.3f, 11f, White * 0.9f, 45f);
            return 22f;
        }

        private static float Lattice(StageArt a, Transform t)
        {
            var iron = new Color(0.45f, 0.38f, 0.3f);
            a.Frustum(t, Vector3.zero, 4, 6f, 2.4f, 10f, iron, 45f);
            a.Box(t, new Vector3(0f, 10f, 0f), new Vector3(4.6f, 0.6f, 4.6f), iron * 0.9f);
            a.Frustum(t, new Vector3(0f, 10f, 0f), 4, 2.2f, 0.8f, 12f, iron, 45f);
            a.Frustum(t, new Vector3(0f, 22f, 0f), 4, 0.8f, 0.1f, 8f, iron, 45f);
            return 30f;
        }

        private static float Lighthouse(StageArt a, Transform t)
        {
            a.Frustum(t, Vector3.zero, 12, 2.2f, 1.6f, 12f, White);
            for (int k = 0; k < 3; k++) a.Frustum(t, new Vector3(0f, 2f + k * 4f, 0f), 12, 2.15f - k * 0.2f, 2.05f - k * 0.2f, 1.2f, new Color(0.8f, 0.2f, 0.2f));
            a.Cyl(t, new Vector3(0f, 12f, 0f), 1.4f, 1.6f, Glass);
            return 14f;
        }

        private static float Statue(StageArt a, Transform t)
        {
            var green = new Color(0.45f, 0.7f, 0.62f);
            a.Box(t, new Vector3(0f, 3f, 0f), new Vector3(5f, 6f, 5f), Stone);
            a.Frustum(t, new Vector3(0f, 6f, 0f), 8, 1.6f, 0.9f, 9f, green);
            a.Ball(t, new Vector3(0f, 15.6f, 0f), new Vector3(1.4f, 1.6f, 1.4f), green);
            a.Box(t, new Vector3(1.1f, 17f, 0f), new Vector3(0.4f, 3.4f, 0.4f), green);
            a.Ball(t, new Vector3(1.1f, 19f, 0f), new Vector3(0.7f, 0.9f, 0.7f), Gold);
            return 19.5f;
        }

        private static float StepPyramid(StageArt a, Transform t)
        {
            for (int i = 0; i < 5; i++) a.Box(t, new Vector3(0f, i * 2f + 1f, 0f), new Vector3(14f - i * 2.6f, 2f, 14f - i * 2.6f), Stone * (0.95f - i * 0.03f));
            a.Box(t, new Vector3(0f, 11f, 0f), new Vector3(2.4f, 2f, 2.4f), Stone * 0.8f);
            return 12f;
        }

        private static float Mausoleum(StageArt a, Transform t)
        {
            a.Box(t, new Vector3(0f, 0.5f, 0f), new Vector3(20f, 1f, 20f), White * 0.97f);
            a.Box(t, new Vector3(0f, 5f, 0f), new Vector3(10f, 8f, 10f), White);
            a.Ball(t, new Vector3(0f, 11f, 0f), new Vector3(6f, 7f, 6f), White);
            foreach (var p in new[] { new Vector3(-9f, 1f, -9f), new Vector3(9f, 1f, -9f), new Vector3(-9f, 1f, 9f), new Vector3(9f, 1f, 9f) })
                a.Cyl(t, p, 0.6f, 12f, White * 0.95f);
            return 14.5f;
        }

        private static float Domes(StageArt a, Transform t)
        {
            var grey = new Color(0.55f, 0.6f, 0.7f);
            a.Box(t, new Vector3(0f, 3f, 0f), new Vector3(14f, 6f, 10f), Stone);
            a.Ball(t, new Vector3(0f, 7f, 0f), new Vector3(8f, 6f, 8f), grey);
            foreach (float x in new[] { -5f, 5f }) a.Ball(t, new Vector3(x, 6f, 0f), new Vector3(3.6f, 2.6f, 3.6f), grey);
            foreach (float x in new[] { -8f, 8f }) a.Cyl(t, new Vector3(x, 0f, -4f), 0.5f, 16f, Stone);
            return 16f;
        }

        private static float VolcanoTerraces(StageArt a, Transform t)
        {
            a.Frustum(t, new Vector3(0f, 0f, 4f), 14, 8f, 2f, 10f, new Color(0.32f, 0.27f, 0.25f));
            a.Cyl(t, new Vector3(0f, 9.9f, 4f), 1.8f, 0.2f, new Color(1f, 0.4f, 0.1f));
            var green = new Color(0.36f, 0.58f, 0.3f);
            for (int i = 0; i < 5; i++)
            {
                a.Box(t, new Vector3(0f, i * 0.9f + 0.45f, -2f - i * 1.4f), new Vector3(14f - i * 1.2f, 0.9f, 1.5f), Stone * 0.9f);
                a.Box(t, new Vector3(0f, i * 0.9f + 0.95f, -2f - i * 1.4f), new Vector3(13.6f - i * 1.2f, 0.1f, 1.3f), green);
            }
            return 10.5f;
        }

        private static float CastleKeep(StageArt a, Transform t)
        {
            // A stone-based keep of stacked tiers with swept roofs. A military building of the past: no symbols, no text.
            a.Frustum(t, Vector3.zero, 4, 11f, 8f, 4f, Stone, 45f);
            var roof = new Color(0.22f, 0.24f, 0.28f);
            for (int i = 0; i < 4; i++)
            {
                float w = 7.5f - i * 1.5f, y = 4f + i * 3.2f;
                a.Box(t, new Vector3(0f, y + 1f, 0f), new Vector3(w, 2f, w), White * 0.97f);
                a.Frustum(t, new Vector3(0f, y + 2f, 0f), 4, w * 0.82f, w * 0.5f, 1.2f, roof, 45f);
            }
            return 18f;
        }

        private static float SnowMountain(StageArt a, Transform t)
        {
            a.Frustum(t, Vector3.zero, 12, 12f, 0.5f, 15f, Rock * 0.9f);
            a.Frustum(t, new Vector3(0f, 9f, 0f), 12, 4.6f, 0.4f, 6f, White);
            return 15f;
        }

        private static float RedBridge(StageArt a, Transform t)
        {
            var red = new Color(0.78f, 0.2f, 0.16f);
            for (int i = 0; i < 9; i++)
            {
                float x = -8f + i * 2f, y = 1.2f + 1.6f * Mathf.Sin(i / 8f * Mathf.PI);
                a.Box(t, new Vector3(x, y, 0f), new Vector3(2.1f, 0.35f, 2.2f), Wood);
                a.Box(t, new Vector3(x, y + 0.8f, -1f), new Vector3(2.1f, 0.12f, 0.12f), red);
                a.Box(t, new Vector3(x, y + 0.8f, 1f), new Vector3(2.1f, 0.12f, 0.12f), red);
                if (i % 2 == 0) foreach (float z in new[] { -1f, 1f }) a.Cyl(t, new Vector3(x, y, z), 0.08f, 0.9f, red);
            }
            foreach (float x in new[] { -8f, 8f }) a.Cyl(t, new Vector3(x, 0f, 0f), 0.35f, 1.2f, Wood);
            a.Water(t, new Vector3(0f, -0.15f, 0f), new Vector3(20f, 0.2f, 7f), Water);
            return 4.5f;
        }
    }

    /// <summary>Turns slowly around its local up axis (water wheels).</summary>
    public class Spin : MonoBehaviour
    {
        public float degreesPerSecond = 25f;
        private void Start() => Aron.Core.VisibilityGroup.Attach(this);
        private void Update() => transform.Rotate(0f, degreesPerSecond * Time.deltaTime, 0f, Space.Self);
    }
}
