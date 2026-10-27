// (c) Ahmed Alamrani. All rights reserved.
using System.Collections.Generic;
using UnityEngine;

namespace Aron.Enemies
{
    /// <summary>
    /// Region bosses that aren't people in costume: Dr. Duhaim and his helper robots, the Red Sea shark, rock
    /// golems, wolves, genies and the rest, put together from primitives in the region's boss colours. Feet sit
    /// on the origin and they face +Z, about 3.4 m tall at scale 1 like the costumed fighters. Also adds the eye
    /// patch and beard to the robe fighter for the captains, and the wrappings for the mummy.
    /// </summary>
    public class BossBodies
    {
        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
        private static readonly int EmissionId = Shader.PropertyToID("_EmissionColor");
        private static readonly Dictionary<PrimitiveType, Mesh> primitives = new Dictionary<PrimitiveType, Mesh>();
        private static readonly float[] Sides = { -1f, 1f };
        private static Mesh cone;

        private static readonly Color Skin = new Color(0.86f, 0.64f, 0.48f);
        private static readonly Color Black = new Color(0.06f, 0.06f, 0.07f);
        private static readonly Color White = new Color(0.95f, 0.95f, 0.93f);
        private static readonly Color Bone = new Color(0.93f, 0.89f, 0.8f);
        private static readonly Color Gold = new Color(0.85f, 0.66f, 0.22f);

        private readonly Material template;
        private readonly Dictionary<(Color32, bool), Material> mats = new Dictionary<(Color32, bool), Material>();
        private readonly Color body, trim, glow;
        private readonly string headgear;
        private Transform root;
        private CreatureMotion motion;

        private BossBodies(Material template, Color body, Color trim, Color glow, string headgear)
        {
            this.template = template;
            this.body = body;
            this.trim = trim;
            this.glow = glow;
            this.headgear = headgear ?? "none";
        }

        /// <summary>True when <paramref name="model"/> is one of the creatures built here.</summary>
        public static bool Builds(string model)
        {
            switch (model)
            {
                case "scientist": case "shark": case "golem": case "wolf": case "eagle": case "clam": case "barrel":
                case "marid": case "croc": case "crab": case "ghoul": case "tree":
                    return true;
                default:
                    return false;
            }
        }

        public static GameObject Build(string model, string headgear, Color body, Color trim, Color glow, Material template)
        {
            if (!Builds(model)) return null;
            var b = new BossBodies(template, body, trim, glow, headgear);
            var go = new GameObject("Model");
            b.root = go.transform;
            b.motion = go.AddComponent<CreatureMotion>();
            switch (model)
            {
                case "scientist": b.Scientist(); break;
                case "shark": b.Shark(); break;
                case "golem": b.Golem(); break;
                case "wolf": b.Wolf(); break;
                case "eagle": b.Eagle(); break;
                case "clam": b.Clam(); break;
                case "barrel": b.Barrel(); break;
                case "marid": b.Marid(); break;
                case "croc": b.Croc(); break;
                case "crab": b.Crab(); break;
                case "ghoul": b.Ghoul(); break;
                case "tree": b.Tree(); break;
            }
            return go;
        }

        /// <summary>Extra pieces on a costumed fighter: an eye patch and beard for a captain, wrappings for a mummy.</summary>
        public static void Dress(string model, Transform fighter, Color body, Color trim, Material template)
        {
            if (fighter == null || (model != "captain" && model != "mummy")) return;
            var b = new BossBodies(template, body, trim, new Color(1f, 0.85f, 0.3f), "none") { root = fighter };
            var eyeL = Find(fighter, "EyeL");
            var eyeR = Find(fighter, "EyeR");
            var mouth = Find(fighter, "MouthC");
            var head = Find(fighter, "Head");
            if (head == null || eyeL == null || eyeR == null) return;
            Vector3 fwd = fighter.forward, up = fighter.up, right = fighter.right;
            float gap = Vector3.Distance(eyeL.position, eyeR.position);
            Vector3 mid = (eyeL.position + eyeR.position) * 0.5f;
            var g = new GameObject(model == "captain" ? "CaptainKit" : "MummyKit").transform;
            g.SetParent(head, false);
            g.SetPositionAndRotation(mid, Quaternion.LookRotation(fwd, up));
            Vector3 ls = head.lossyScale;
            g.localScale = new Vector3(gap / ls.x, gap / ls.y, gap / ls.z);
            // From here on one unit is the distance between the eyes, z points out of the face.
            if (model == "captain")
            {
                Vector3 patch = g.InverseTransformPoint(eyeR.position);
                b.S("Patch", g, patch + V(0f, 0f, 0.08f), V(0.62f, 0.52f, 0.2f), Black);
                b.B("Strap", g, V(0f, 0.32f, -0.2f), V(2.1f, 0.07f, 0.08f), Black, V(0f, 0f, 18f));
                float mouthY = mouth != null ? g.InverseTransformPoint(mouth.position).y : -0.8f;
                b.S("Beard", g, V(0f, mouthY - 0.42f, -0.35f), V(1.25f, 0.62f, 0.6f), Black);
                foreach (float s in Sides)
                    b.B("Moustache", g, V(s * 0.28f, mouthY + 0.14f, 0.02f), V(0.55f, 0.13f, 0.14f), Black, V(0f, 0f, s * -14f));
                b.Ring("Earring", g, V(-1.05f, -0.35f, -0.55f), 0.22f, Gold);
            }
            else
            {
                foreach (var e in new[] { eyeL, eyeR })
                {
                    var r = e.GetComponent<Renderer>();
                    if (r != null) r.sharedMaterial = b.Mat(new Color(1f, 0.85f, 0.3f), true);
                }
                var wrap = Color.Lerp(body, Color.white, 0.35f);
                b.B("Wrap", g, V(0f, 0.55f, -0.05f), V(2.2f, 0.18f, 0.25f), wrap, V(0f, 0f, 8f));
                b.B("Wrap", g, V(0f, -0.45f, -0.05f), V(2.1f, 0.2f, 0.25f), wrap, V(0f, 0f, -6f));
                b.B("Wrap", g, V(0f, -0.95f, -0.15f), V(1.7f, 0.2f, 0.25f), wrap, V(0f, 0f, 10f));
                // The nemes: striped cloth over the head and down both sides.
                var blue = Color.Lerp(trim, Color.black, 0.15f);
                b.S("Nemes", g, V(0f, 0.75f, -0.8f), V(2.4f, 1.6f, 2.0f), Gold);
                for (int i = 0; i < 4; i++) b.B("Stripe", g, V(0f, 0.3f + i * 0.28f, -0.25f), V(2.42f, 0.1f, 0.3f), blue);
                foreach (float s in Sides)
                {
                    b.B("Lappet", g, V(s * 1.15f, -0.65f, -0.35f), V(0.5f, 1.7f, 0.25f), Gold);
                    b.B("LappetStripe", g, V(s * 1.15f, -0.45f, -0.2f), V(0.52f, 0.12f, 0.06f), blue);
                    b.B("LappetStripe", g, V(s * 1.15f, -0.95f, -0.2f), V(0.52f, 0.12f, 0.06f), blue);
                }
                b.Cone("Cobra", g, V(0f, 1.4f, 0.1f), V(0.25f, 0.45f, 0.2f), Gold);
            }
        }

        // ------------------------------------------------------------------ Dr. Duhaim

        private void Scientist()
        {
            var thobe = Black;
            var coat = new Color(0.46f, 0.2f, 0.62f);
            var coatDark = Color.Lerp(coat, Color.black, 0.3f);
            C("Thobe", root, V(0f, 0.85f, 0f), V(1.0f, 0.85f, 0.9f), thobe);
            C("Hem", root, V(0f, 0.1f, 0f), V(1.03f, 0.04f, 0.93f), Gold);
            C("HemInner", root, V(0f, 0.3f, 0f), V(1.02f, 0.02f, 0.92f), Gold);
            B("Placket", root, V(0f, 1.15f, 0.44f), V(0.1f, 1.5f, 0.04f), Gold);
            for (int i = 0; i < 4; i++)
                foreach (float s in Sides)
                    B("Diamond", root, V(s * 0.16f, 0.55f + i * 0.3f, 0.43f), V(0.09f, 0.09f, 0.03f), Gold, V(0f, 0f, 45f));
            S("Body", root, V(0f, 1.85f, 0f), V(1.15f, 1.0f, 0.95f), thobe);

            B("CoatBack", root, V(0f, 1.3f, -0.38f), V(1.12f, 1.7f, 0.12f), coat);
            foreach (float s in Sides)
            {
                B("CoatSide", root, V(s * 0.55f, 1.3f, 0f), V(0.1f, 1.7f, 0.85f), coat);
                B("CoatFront", root, V(s * 0.38f, 1.3f, 0.44f), V(0.3f, 1.7f, 0.08f), coat, V(0f, s * -8f, 0f));
                B("Lapel", root, V(s * 0.2f, 2.02f, 0.5f), V(0.17f, 0.45f, 0.06f), coatDark, V(0f, 0f, s * 22f));
            }
            S("Shoulders", root, V(0f, 2.05f, 0f), V(1.3f, 0.6f, 1.0f), coat);
            B("Pocket", root, V(0.33f, 1.72f, 0.49f), V(0.2f, 0.16f, 0.03f), coatDark);
            C("Pen", root, V(0.3f, 1.84f, 0.5f), V(0.03f, 0.08f, 0.03f), Gold);
            C("Pen", root, V(0.37f, 1.84f, 0.5f), V(0.03f, 0.08f, 0.03f), new Color(0.2f, 0.6f, 0.9f));

            foreach (float s in Sides)
            {
                var arm = G(s < 0f ? "ArmL" : "ArmR", root, V(s * 0.7f, 2.1f, 0f), V(0f, 0f, s * 8f));
                K("Sleeve", arm, V(0f, -0.45f, 0f), V(0.34f, 0.5f, 0.34f), coat);
                C("Cuff", arm, V(0f, -0.9f, 0f), V(0.36f, 0.05f, 0.36f), White);
                var hand = S("Hand", arm, V(0f, -1.05f, 0f), V(0.25f, 0.25f, 0.25f), Skin);
                if (s > 0f)
                {
                    // A flask of something green and bubbling.
                    C("FlaskNeck", hand, V(0f, -0.2f, 1.2f), V(0.5f, 0.6f, 0.5f), White);
                    S("Flask", hand, V(0f, -1.3f, 1.2f), V(1.5f, 1.5f, 1.5f), new Color(0.45f, 1f, 0.35f), Vector3.zero, true);
                }
                motion.Add(arm, CreatureMotion.Kind.Stride, Vector3.right, 25f, 1f, s > 0f ? 0f : Mathf.PI);
            }

            var head = G("Skull", root, V(0f, 2.35f, 0f));
            S("Head", head, V(0f, 0.55f, 0.05f), V(1.25f, 1.15f, 1.15f), Skin);
            var hair = new Color(0.88f, 0.88f, 0.86f);
            foreach (float s in Sides)
            {
                S("Hair", head, V(s * 0.57f, 0.55f, -0.15f), V(0.35f, 0.45f, 0.5f), hair);
                S("Ear", head, V(s * 0.63f, 0.5f, 0.02f), V(0.15f, 0.25f, 0.12f), Skin);
                C("Rim", head, V(s * 0.25f, 0.6f, 0.6f), V(0.38f, 0.03f, 0.38f), Black, V(90f, 0f, 0f));
                C("Lens", head, V(s * 0.25f, 0.6f, 0.625f), V(0.31f, 0.02f, 0.31f), new Color(0.88f, 0.96f, 1f), V(90f, 0f, 0f));
                S("Eye", head, V(s * 0.23f, 0.6f, 0.645f), V(0.11f, 0.11f, 0.04f), Black);
                B("Temple", head, V(s * 0.5f, 0.62f, 0.33f), V(0.03f, 0.03f, 0.55f), Black);
                B("Brow", head, V(s * 0.25f, 0.84f, 0.6f), V(0.3f, 0.06f, 0.05f), hair, V(0f, 0f, s * 16f));
            }
            S("HairBack", head, V(0f, 0.45f, -0.5f), V(0.85f, 0.42f, 0.3f), hair);
            B("Bridge", head, V(0f, 0.62f, 0.64f), V(0.14f, 0.04f, 0.04f), Black);
            S("Nose", head, V(0f, 0.42f, 0.64f), V(0.17f, 0.19f, 0.17f), Color.Lerp(Skin, Color.red, 0.12f));
            B("Moustache", head, V(0f, 0.29f, 0.6f), V(0.42f, 0.08f, 0.09f), hair);
            B("Grin", head, V(0f, 0.18f, 0.58f), V(0.32f, 0.05f, 0.04f), new Color(0.3f, 0.05f, 0.05f), V(0f, 0f, -4f));
            motion.Add(head, CreatureMotion.Kind.Sway, Vector3.forward, 4f, 1.6f);

            // Two little helper robots circling him.
            var ring = G("Helpers", root, Vector3.zero);
            motion.Add(ring, CreatureMotion.Kind.Spin, Vector3.up, 0f, 45f);
            foreach (float s in Sides)
            {
                var bot = G("Helper", ring, V(s * 1.8f, 1.9f, 0f));
                var metal = new Color(0.74f, 0.77f, 0.82f);
                S("Shell", bot, Vector3.zero, V(0.45f, 0.42f, 0.45f), metal);
                B("Visor", bot, V(0f, 0.04f, 0.19f), V(0.3f, 0.1f, 0.06f), new Color(0.3f, 0.95f, 1f), Vector3.zero, true);
                C("Antenna", bot, V(0f, 0.3f, 0f), V(0.03f, 0.1f, 0.03f), Black);
                S("Tip", bot, V(0f, 0.42f, 0f), V(0.08f, 0.08f, 0.08f), new Color(1f, 0.25f, 0.2f), Vector3.zero, true);
                var rotor = C("Rotor", bot, V(0f, 0.26f, 0f), V(0.5f, 0.01f, 0.08f), Black);
                motion.Add(rotor, CreatureMotion.Kind.Spin, Vector3.up, 0f, 900f);
                foreach (float a in Sides) B("Claw", bot, V(a * 0.26f, -0.12f, 0.05f), V(0.06f, 0.2f, 0.06f), Black, V(0f, 0f, a * 25f));
                motion.Add(bot, CreatureMotion.Kind.Hover, Vector3.up, 0.18f, 3f, s);
            }
        }

        // ------------------------------------------------------------------ Red Sea shark

        private void Shark()
        {
            var sea = new Color(0.12f, 0.45f, 0.72f);
            C("Sea", root, V(0f, 0.03f, 0f), V(5f, 0.03f, 5f), sea);
            C("Shallows", root, V(0f, 0.05f, 0f), V(3.4f, 0.02f, 3.4f), new Color(0.3f, 0.66f, 0.86f));
            for (int i = 0; i < 10; i++)
            {
                float a = i * Mathf.PI * 2f / 10f;
                var foam = S("Foam", root, V(Mathf.Cos(a) * 1.35f, 0.12f, Mathf.Sin(a) * 1.25f), V(0.75f, 0.22f, 0.5f), White, V(0f, -a * Mathf.Rad2Deg, 0f));
                motion.Add(foam, CreatureMotion.Kind.Hover, Vector3.up, 0.05f, 4f, i);
            }

            var skin = Color.Lerp(body, new Color(0.45f, 0.52f, 0.6f), 0.45f);
            var belly = new Color(0.92f, 0.92f, 0.9f);
            var shark = G("Shark", root, Vector3.zero, V(12f, 0f, 0f));
            motion.Add(shark, CreatureMotion.Kind.Sway, Vector3.up, 6f, 1.3f);
            S("Torso", shark, V(0f, 1.5f, 0f), V(1.9f, 3.6f, 1.7f), skin);
            S("Belly", shark, V(0f, 1.45f, 0.24f), V(1.55f, 3.2f, 1.35f), belly);
            S("Snout", shark, V(0f, 3.0f, 0.2f), V(1.35f, 1.1f, 1.45f), skin);
            S("Mouth", shark, V(0f, 2.45f, 0.7f), V(1.1f, 0.7f, 0.5f), new Color(0.45f, 0.08f, 0.1f));
            for (int i = 0; i < 7; i++)
            {
                float x = -0.42f + i * 0.14f;
                Cone("Tooth", shark, V(x, 2.74f, 0.86f - Mathf.Abs(x) * 0.25f), V(0.12f, 0.2f, 0.1f), White, V(180f, 0f, 0f));
            }
            var jaw = G("Jaw", shark, V(0f, 2.3f, 0.45f));
            S("Chin", jaw, V(0f, -0.08f, 0.2f), V(1.15f, 0.36f, 0.7f), belly);
            for (int i = 0; i < 6; i++)
            {
                float x = -0.36f + i * 0.144f;
                Cone("Tooth", jaw, V(x, 0.06f, 0.48f - Mathf.Abs(x) * 0.25f), V(0.11f, 0.18f, 0.1f), White);
            }
            motion.Add(jaw, CreatureMotion.Kind.Chomp, Vector3.right, 14f, 2.4f);
            foreach (float s in Sides)
            {
                S("Eye", shark, V(s * 0.56f, 3.0f, 0.6f), V(0.2f, 0.25f, 0.17f), Black);
                S("Glint", shark, V(s * 0.6f, 3.06f, 0.66f), V(0.06f, 0.06f, 0.04f), White);
                B("Brow", shark, V(s * 0.5f, 3.2f, 0.66f), V(0.35f, 0.07f, 0.1f), Color.Lerp(skin, Color.black, 0.5f), V(0f, s * 20f, s * 22f));
                for (int i = 0; i < 3; i++)
                    B("Gill", shark, V(s * 0.86f, 2.15f - i * 0.17f, 0.22f), V(0.04f, 0.3f, 0.06f), Color.Lerp(skin, Color.black, 0.45f));
                var fin = G("Fin", shark, V(s * 0.88f, 1.45f, 0.2f));
                Cone("Pectoral", fin, Vector3.zero, V(0.14f, 1.1f, 0.6f), skin, V(-20f, 0f, s * -72f));
                motion.Add(fin, CreatureMotion.Kind.Sway, Vector3.forward, 10f, 3f, s);
            }
            Cone("Dorsal", shark, V(0f, 2.5f, -0.72f), V(0.16f, 1.2f, 0.95f), skin, V(-38f, 0f, 0f));

            var tail = G("Tail", root, V(0f, 0.1f, -2.2f));
            S("Back", tail, V(0f, -0.02f, 0.85f), V(0.9f, 0.32f, 1.4f), skin);
            Cone("Lobe", tail, Vector3.zero, V(0.12f, 1.35f, 0.65f), skin, V(-28f, 0f, 0f));
            Cone("Lobe", tail, Vector3.zero, V(0.1f, 0.7f, 0.45f), skin, V(-150f, 0f, 0f));
            motion.Add(tail, CreatureMotion.Kind.Sway, Vector3.up, 22f, 2.6f);
        }

        // ------------------------------------------------------------------ rock golem

        private void Golem()
        {
            var stone = body;
            var light = Color.Lerp(body, Color.white, 0.18f);
            var dark = Color.Lerp(body, Color.black, 0.3f);
            var eyes = new Color(1f, 0.78f, 0.22f);
            B("Hips", root, V(0f, 1.15f, 0f), V(1.3f, 0.5f, 0.8f), dark);
            foreach (float s in Sides)
            {
                var leg = G("Leg", root, V(s * 0.42f, 1.05f, 0f));
                B("Thigh", leg, V(0f, -0.35f, 0f), V(0.56f, 0.72f, 0.6f), stone, V(0f, s * 4f, 0f));
                B("Foot", leg, V(0f, -0.85f, 0.08f), V(0.66f, 0.4f, 0.82f), dark);
                motion.Add(leg, CreatureMotion.Kind.Stride, Vector3.right, 22f, 1f, s > 0f ? 0f : Mathf.PI);
            }
            var torso = G("Torso", root, V(0f, 1.35f, 0f));
            motion.Add(torso, CreatureMotion.Kind.Sway, Vector3.forward, 2.5f, 1.2f);
            B("Chest", torso, V(0f, 0.9f, 0f), V(1.9f, 1.4f, 1.1f), stone, V(0f, 0f, 1.5f));
            B("Belly", torso, V(0f, 0.2f, 0.05f), V(1.4f, 0.5f, 0.9f), dark);
            // A Hegra tomb front carved in his chest: the stepped crown over a doorway.
            for (int i = 0; i < 3; i++)
                B("Step", torso, V(0f, 1.12f + i * 0.12f, 0.56f), V(0.9f - i * 0.3f, 0.12f, 0.05f), trim);
            B("Doorway", torso, V(0f, 0.72f, 0.56f), V(0.46f, 0.6f, 0.05f), Color.Lerp(trim, Color.black, 0.5f));
            B("Crack", torso, V(-0.55f, 0.6f, 0.56f), V(0.05f, 0.6f, 0.03f), eyes, V(0f, 0f, 20f), true);
            B("Crack", torso, V(0.6f, 1.3f, 0.56f), V(0.04f, 0.4f, 0.03f), eyes, V(0f, 0f, -30f), true);
            foreach (float s in Sides)
            {
                B("Shoulder", torso, V(s * 1.05f, 1.45f, 0f), V(0.72f, 0.66f, 0.82f), light, V(0f, 0f, s * -12f));
                var arm = G("Arm", torso, V(s * 1.12f, 1.3f, 0f));
                B("Upper", arm, V(0f, -0.45f, 0f), V(0.5f, 0.8f, 0.55f), stone);
                B("Fore", arm, V(0f, -1.05f, 0.05f), V(0.66f, 0.7f, 0.7f), light, V(0f, 0f, s * 5f));
                B("Fist", arm, V(0f, -1.52f, 0.08f), V(0.62f, 0.46f, 0.62f), dark);
                motion.Add(arm, CreatureMotion.Kind.Stride, Vector3.right, 20f, 1f, s > 0f ? Mathf.PI : 0f);
            }
            var head = G("Skull", torso, V(0f, 1.6f, 0.1f));
            B("Head", head, V(0f, 0.25f, 0f), V(0.86f, 0.7f, 0.76f), stone);
            B("BrowRidge", head, V(0f, 0.4f, 0.37f), V(0.92f, 0.15f, 0.16f), dark);
            foreach (float s in Sides)
                B("Eye", head, V(s * 0.2f, 0.28f, 0.38f), V(0.2f, 0.11f, 0.06f), eyes, Vector3.zero, true);
            B("Jaw", head, V(0f, 0.0f, 0.33f), V(0.6f, 0.15f, 0.14f), dark);
            if (headgear == "crown")
                for (int i = 0; i < 5; i++)
                {
                    float a = i * Mathf.PI * 2f / 5f;
                    B("CrownStone", head, V(Mathf.Sin(a) * 0.32f, 0.72f, Mathf.Cos(a) * 0.28f), V(0.22f, 0.3f, 0.22f), trim, V(0f, a * Mathf.Rad2Deg, 0f));
                }
            else if (headgear == "turban")
                C("Turban", head, V(0f, 0.68f, 0f), V(0.95f, 0.15f, 0.85f), Color.Lerp(trim, Color.white, 0.4f));
        }

        // ------------------------------------------------------------------ wolf

        private void Wolf()
        {
            var fur = body;
            var pale = trim;
            var dark = Color.Lerp(body, Color.black, 0.35f);
            var eyes = new Color(0.55f, 0.88f, 1f);
            S("Torso", root, V(0f, 1.45f, -0.1f), V(1.3f, 1.25f, 2.6f), fur);
            S("Ruff", root, V(0f, 1.55f, 0.85f), V(1.3f, 1.4f, 1.05f), pale);
            for (int i = 0; i < 5; i++)
                Cone("Hackle", root, V(0f, 2.02f - i * 0.04f, 0.6f - i * 0.35f), V(0.3f, 0.45f, 0.3f), dark, V(-35f, 0f, 0f));

            var head = G("Skull", root, V(0f, 1.95f, 1.15f));
            motion.Add(head, CreatureMotion.Kind.Sway, Vector3.right, 4f, 2f);
            S("Head", head, V(0f, 0.35f, 0.35f), V(0.86f, 0.8f, 0.95f), fur);
            B("Snout", head, V(0f, 0.2f, 0.95f), V(0.46f, 0.38f, 0.72f), fur);
            B("LowerJaw", head, V(0f, 0.0f, 0.9f), V(0.4f, 0.15f, 0.6f), pale);
            S("Nose", head, V(0f, 0.34f, 1.31f), V(0.2f, 0.15f, 0.14f), Black);
            foreach (float s in Sides)
            {
                Cone("Fang", head, V(s * 0.15f, 0.08f, 1.18f), V(0.07f, 0.16f, 0.07f), White, V(180f, 0f, 0f));
                S("Eye", head, V(s * 0.22f, 0.5f, 0.76f), V(0.15f, 0.1f, 0.08f), eyes, V(0f, 0f, s * -15f), true);
                B("Brow", head, V(s * 0.22f, 0.6f, 0.76f), V(0.24f, 0.06f, 0.08f), dark, V(0f, 0f, s * 20f));
                Cone("Ear", head, V(s * 0.3f, 0.72f, 0.2f), V(0.26f, 0.48f, 0.16f), fur, V(-10f, 0f, s * -16f));
                Cone("EarInside", head, V(s * 0.3f, 0.74f, 0.26f), V(0.14f, 0.32f, 0.06f), new Color(0.75f, 0.55f, 0.55f), V(-10f, 0f, s * -16f));
            }
            var legs = new[] { V(-0.45f, 1.2f, 0.75f), V(0.45f, 1.2f, 0.75f), V(-0.45f, 1.25f, -0.95f), V(0.45f, 1.25f, -0.95f) };
            for (int i = 0; i < 4; i++)
            {
                var leg = G("Leg", root, legs[i]);
                K("Upper", leg, V(0f, -0.35f, 0f), V(0.34f, 0.4f, 0.38f), fur);
                K("Lower", leg, V(0f, -0.82f, 0.05f), V(0.25f, 0.34f, 0.27f), fur);
                S("Paw", leg, V(0f, -1.13f, 0.12f), V(0.33f, 0.15f, 0.44f), dark);
                bool forward = i == 0 || i == 3;
                motion.Add(leg, CreatureMotion.Kind.Stride, Vector3.right, 30f, 1.2f, forward ? 0f : Mathf.PI);
            }
            var tail = G("Tail", root, V(0f, 1.75f, -1.35f), V(-35f, 0f, 0f));
            S("Brush", tail, V(0f, 0f, -0.45f), V(0.38f, 0.38f, 0.95f), fur);
            S("Tip", tail, V(0f, 0f, -0.98f), V(0.3f, 0.3f, 0.55f), pale);
            motion.Add(tail, CreatureMotion.Kind.Sway, Vector3.up, 18f, 4f);
        }

        // ------------------------------------------------------------------ Eagle of Sarat

        private void Eagle()
        {
            var feathers = body;
            var dark = Color.Lerp(body, Color.black, 0.3f);
            var beak = new Color(0.95f, 0.72f, 0.15f);
            S("Body", root, V(0f, 1.6f, 0f), V(1.3f, 1.75f, 1.2f), feathers, V(15f, 0f, 0f));
            S("Chest", root, V(0f, 1.62f, 0.26f), V(1.02f, 1.3f, 0.85f), Color.Lerp(feathers, Color.white, 0.15f));
            var head = G("Skull", root, V(0f, 2.42f, 0.25f));
            motion.Add(head, CreatureMotion.Kind.Sway, Vector3.up, 12f, 1.1f);
            S("Head", head, V(0f, 0.25f, 0.1f), V(0.76f, 0.72f, 0.76f), trim);
            S("Beak", head, V(0f, 0.2f, 0.5f), V(0.3f, 0.26f, 0.48f), beak);
            Cone("Hook", head, V(0f, 0.16f, 0.72f), V(0.13f, 0.26f, 0.13f), Color.Lerp(beak, Color.black, 0.25f), V(150f, 0f, 0f));
            foreach (float s in Sides)
            {
                S("Eye", head, V(s * 0.22f, 0.33f, 0.37f), V(0.14f, 0.14f, 0.08f), new Color(1f, 0.82f, 0.2f), Vector3.zero, true);
                S("Pupil", head, V(s * 0.22f, 0.33f, 0.41f), V(0.07f, 0.07f, 0.03f), Black);
                B("Brow", head, V(s * 0.22f, 0.44f, 0.38f), V(0.24f, 0.06f, 0.08f), dark, V(0f, 0f, s * 22f));
                var wing = G("Wing", root, V(s * 0.62f, 2.15f, -0.05f));
                B("Feathers", wing, V(s * 0.12f, -0.55f, -0.1f), V(0.18f, 1.4f, 0.95f), dark, V(0f, 0f, s * 10f));
                for (int i = 0; i < 3; i++)
                    B("Tip", wing, V(s * 0.24f, -1.25f - i * 0.08f, -0.1f - i * 0.28f), V(0.14f, 0.55f, 0.24f), Color.Lerp(dark, Color.black, 0.2f), V(0f, 0f, s * 14f));
                motion.Add(wing, CreatureMotion.Kind.Sway, Vector3.forward, s * 16f, 5f);
                var leg = G("Leg", root, V(s * 0.3f, 0.8f, 0.1f));
                C("Shank", leg, V(0f, -0.4f, 0f), V(0.13f, 0.4f, 0.13f), beak);
                for (int t = -1; t <= 1; t++)
                    Cone("Talon", leg, V(t * 0.1f, -0.75f, 0.1f), V(0.08f, 0.28f, 0.08f), Black, V(90f, t * 20f, 0f));
                motion.Add(leg, CreatureMotion.Kind.Stride, Vector3.right, 18f, 1.4f, s > 0f ? 0f : Mathf.PI);
            }
            B("Tail", root, V(0f, 0.9f, -0.62f), V(0.82f, 0.1f, 0.95f), dark, V(-30f, 0f, 0f));
            B("TailTip", root, V(0f, 0.62f, -1.05f), V(0.84f, 0.1f, 0.25f), trim, V(-30f, 0f, 0f));
        }

        // ------------------------------------------------------------------ Abu Mahar, the oyster

        private void Clam()
        {
            var shell = body;
            var ridge = Color.Lerp(body, Color.black, 0.2f);
            foreach (float s in Sides)
            {
                var leg = G("Leg", root, V(s * 0.65f, 0.55f, 0f));
                C("Shin", leg, V(0f, -0.25f, 0f), V(0.16f, 0.25f, 0.16f), new Color(0.9f, 0.55f, 0.35f));
                S("Foot", leg, V(0f, -0.5f, 0.12f), V(0.32f, 0.12f, 0.44f), new Color(0.8f, 0.45f, 0.3f));
                motion.Add(leg, CreatureMotion.Kind.Stride, Vector3.right, 28f, 1.6f, s > 0f ? 0f : Mathf.PI);
            }
            S("Bottom", root, V(0f, 0.95f, 0f), V(2.6f, 1.0f, 2.2f), shell);
            for (int i = -2; i <= 2; i++)
                S("Ridge", root, V(i * 0.42f, 0.92f, 1.0f - Mathf.Abs(i) * 0.12f), V(0.24f, 0.72f, 0.2f), ridge, V(0f, i * 15f, 0f));
            S("Flesh", root, V(0f, 1.3f, 0.15f), V(2.2f, 0.5f, 1.8f), new Color(0.92f, 0.72f, 0.7f));
            S("Pearl", root, V(0f, 1.55f, 0.35f), V(0.62f, 0.62f, 0.62f), trim);

            var lid = G("Lid", root, V(0f, 1.3f, -0.9f));
            motion.Add(lid, CreatureMotion.Kind.Chomp, Vector3.right, -22f, 1.8f);
            S("Top", lid, V(0f, 0.25f, 0.9f), V(2.6f, 0.9f, 2.2f), shell);
            for (int i = -2; i <= 2; i++)
                S("Ridge", lid, V(i * 0.42f, 0.5f, 1.4f - Mathf.Abs(i) * 0.15f), V(0.22f, 0.5f, 0.9f), ridge, V(0f, i * 15f, 0f));
            foreach (float s in Sides)
            {
                C("Stalk", lid, V(s * 0.5f, 0.85f, 0.7f), V(0.09f, 0.26f, 0.09f), ridge);
                S("Eyeball", lid, V(s * 0.5f, 1.15f, 0.7f), V(0.38f, 0.38f, 0.38f), White);
                S("Pupil", lid, V(s * 0.47f, 1.15f, 0.87f), V(0.16f, 0.16f, 0.08f), Black);
                B("Brow", lid, V(s * 0.48f, 1.38f, 0.82f), V(0.34f, 0.07f, 0.08f), Black, V(0f, 0f, s * 20f));
            }
        }

        // ------------------------------------------------------------------ Abu Barmeel

        private void Barrel()
        {
            var steel = new Color(0.62f, 0.64f, 0.68f);
            var glowing = new Color(1f, 0.6f, 0.15f);
            foreach (float s in Sides)
            {
                var leg = G("Leg", root, V(s * 0.4f, 0.95f, 0f));
                C("Pipe", leg, V(0f, -0.45f, 0f), V(0.3f, 0.45f, 0.3f), steel);
                B("Boot", leg, V(0f, -0.85f, 0.1f), V(0.46f, 0.22f, 0.62f), Black);
                motion.Add(leg, CreatureMotion.Kind.Stride, Vector3.right, 24f, 1f, s > 0f ? 0f : Mathf.PI);
            }
            var drum = G("Drum", root, Vector3.zero);
            motion.Add(drum, CreatureMotion.Kind.Sway, Vector3.forward, 3f, 2f);
            C("Barrel", drum, V(0f, 1.78f, 0f), V(1.5f, 0.86f, 1.5f), body);
            foreach (float y in new[] { 1.12f, 1.78f, 2.44f })
                C("Hoop", drum, V(0f, y, 0f), V(1.56f, 0.05f, 1.56f), trim);
            B("Label", drum, V(0f, 1.55f, 0.76f), V(0.6f, 0.42f, 0.02f), trim);
            S("Drop", drum, V(0f, 1.55f, 0.775f), V(0.2f, 0.28f, 0.02f), Black);
            C("Lid", drum, V(0f, 2.65f, 0f), V(1.45f, 0.03f, 1.45f), Color.Lerp(body, Color.black, 0.3f));
            C("Bung", drum, V(0.4f, 2.7f, 0.2f), V(0.18f, 0.04f, 0.18f), trim);
            foreach (float s in Sides)
            {
                B("Eye", drum, V(s * 0.25f, 2.22f, 0.75f), V(0.22f, 0.12f, 0.04f), glowing, V(0f, 0f, s * -10f), true);
                B("Brow", drum, V(s * 0.25f, 2.36f, 0.76f), V(0.28f, 0.06f, 0.04f), Black, V(0f, 0f, s * 18f));
                var arm = G("Arm", drum, V(s * 0.82f, 2.2f, 0f), V(0f, 0f, s * 10f));
                S("Joint", arm, Vector3.zero, V(0.32f, 0.32f, 0.32f), steel);
                C("Upper", arm, V(0f, -0.4f, 0f), V(0.18f, 0.4f, 0.18f), steel);
                S("Elbow", arm, V(0f, -0.8f, 0f), V(0.24f, 0.24f, 0.24f), steel);
                C("Fore", arm, V(0f, -1.1f, 0.05f), V(0.18f, 0.3f, 0.18f), steel);
                C("Valve", arm, V(0f, -1.42f, 0.05f), V(0.42f, 0.04f, 0.42f), trim);
                motion.Add(arm, CreatureMotion.Kind.Stride, Vector3.right, 22f, 1f, s > 0f ? Mathf.PI : 0f);
            }
            B("Grille", drum, V(0f, 1.98f, 0.76f), V(0.46f, 0.1f, 0.03f), Black);
            C("Chimney", drum, V(-0.35f, 2.95f, -0.2f), V(0.17f, 0.3f, 0.17f), Black);
            for (int i = 0; i < 3; i++)
            {
                var puff = S("Smoke", drum, V(-0.35f - i * 0.12f, 3.45f + i * 0.35f, -0.2f), Vector3.one * (0.3f + i * 0.12f), new Color(0.35f, 0.35f, 0.37f));
                motion.Add(puff, CreatureMotion.Kind.Hover, Vector3.up, 0.12f, 2.5f, i);
            }
        }

        // ------------------------------------------------------------------ marid / ifrit

        private void Marid()
        {
            var skin = body;
            var smoke = Color.Lerp(body, Color.white, 0.25f);
            var tail = G("Smoke", root, Vector3.zero);
            motion.Add(tail, CreatureMotion.Kind.Sway, Vector3.forward, 7f, 2f);
            S("Puff", tail, V(0f, 1.3f, -0.05f), V(1.15f, 0.9f, 1.05f), smoke);
            S("Puff", tail, V(0.1f, 0.78f, -0.1f), V(0.82f, 0.7f, 0.8f), smoke);
            S("Puff", tail, V(0.24f, 0.38f, -0.14f), V(0.56f, 0.5f, 0.56f), smoke);
            S("Puff", tail, V(0.34f, 0.12f, -0.1f), V(0.36f, 0.25f, 0.36f), smoke);

            var top = G("Upper", root, Vector3.zero);
            motion.Add(top, CreatureMotion.Kind.Hover, Vector3.up, 0.14f, 2f);
            C("Sash", top, V(0f, 1.75f, 0f), V(1.28f, 0.13f, 1.12f), trim);
            S("Torso", top, V(0f, 2.35f, 0f), V(1.6f, 1.3f, 1.05f), skin);
            var light = Color.Lerp(skin, Color.white, 0.12f);
            foreach (float s in Sides)
            {
                S("Pec", top, V(s * 0.32f, 2.55f, 0.37f), V(0.62f, 0.45f, 0.35f), light);
                var arm = G("Arm", top, V(s * 0.9f, 2.75f, 0f), V(0f, 0f, s * 12f));
                S("Shoulder", arm, Vector3.zero, V(0.62f, 0.62f, 0.62f), skin);
                K("Upper", arm, V(0f, -0.45f, 0f), V(0.42f, 0.45f, 0.42f), skin);
                K("Fore", arm, V(0f, -1.0f, 0.1f), V(0.38f, 0.38f, 0.38f), skin);
                C("Bracelet", arm, V(0f, -1.2f, 0.1f), V(0.45f, 0.07f, 0.45f), Gold);
                S("Fist", arm, V(0f, -1.38f, 0.12f), V(0.4f, 0.4f, 0.4f), skin);
                motion.Add(arm, CreatureMotion.Kind.Sway, Vector3.right, 12f, 2f, s);
            }
            var head = G("Skull", top, V(0f, 3.0f, 0.05f));
            S("Head", head, V(0f, 0.38f, 0.05f), V(0.78f, 0.86f, 0.8f), skin);
            foreach (float s in Sides)
            {
                Cone("Ear", head, V(s * 0.36f, 0.42f, 0f), V(0.13f, 0.36f, 0.12f), skin, V(0f, 0f, s * -70f));
                B("Eye", head, V(s * 0.17f, 0.45f, 0.42f), V(0.17f, 0.09f, 0.06f), glow, V(0f, 0f, s * -12f), true);
                B("Brow", head, V(s * 0.17f, 0.56f, 0.42f), V(0.24f, 0.06f, 0.06f), Black, V(0f, 0f, s * 22f));
                Cone("Fang", head, V(s * 0.1f, 0.18f, 0.43f), V(0.06f, 0.12f, 0.06f), White, V(180f, 0f, 0f));
                if (headgear == "horns")
                    Cone("Horn", head, V(s * 0.24f, 0.72f, 0f), V(0.15f, 0.55f, 0.15f), Color.Lerp(Bone, Color.black, 0.5f), V(-10f, 0f, s * -25f));
            }
            S("Earring", head, V(-0.4f, 0.2f, 0f), V(0.12f, 0.12f, 0.12f), Gold);
            B("Mouth", head, V(0f, 0.22f, 0.41f), V(0.32f, 0.06f, 0.04f), new Color(0.25f, 0.04f, 0.04f));
            Cone("Beard", head, V(0f, 0.12f, 0.33f), V(0.3f, 0.5f, 0.24f), Black, V(168f, 0f, 0f));
            if (headgear == "fez")
            {
                C("Fez", head, V(0f, 0.86f, 0f), V(0.48f, 0.14f, 0.48f), new Color(0.72f, 0.1f, 0.1f));
                Cone("Tassel", head, V(0.12f, 0.98f, -0.1f), V(0.06f, 0.2f, 0.06f), Black, V(0f, 0f, 160f));
            }
            else if (headgear == "hood")
                S("Hood", head, V(0f, 0.45f, -0.1f), V(0.95f, 1.0f, 0.95f), Color.Lerp(trim, Color.black, 0.3f));
            else if (headgear != "horns")
            {
                S("Topknot", head, V(0f, 0.86f, -0.05f), V(0.3f, 0.3f, 0.3f), Black);
                Cone("Plume", head, V(0f, 0.95f, -0.08f), V(0.18f, 0.5f, 0.18f), Black, V(-30f, 0f, 0f));
            }
        }

        // ------------------------------------------------------------------ Tigris beast

        private void Croc()
        {
            var hide = body;
            var belly = Color.Lerp(new Color(0.78f, 0.76f, 0.5f), body, 0.25f);
            var dark = Color.Lerp(body, Color.black, 0.35f);
            S("Body", root, V(0f, 0.9f, 0f), V(1.5f, 0.9f, 3.0f), hide);
            S("Belly", root, V(0f, 0.76f, 0.1f), V(1.32f, 0.7f, 2.7f), belly);
            for (float z = 1.0f; z > -1.3f; z -= 0.45f)
                foreach (float s in Sides)
                    Cone("Scute", root, V(s * 0.25f, 1.3f, z), V(0.18f, 0.3f, 0.26f), dark);

            var head = G("Skull", root, V(0f, 1.05f, 1.4f));
            motion.Add(head, CreatureMotion.Kind.Sway, Vector3.up, 6f, 1.2f);
            B("Skull", head, V(0f, 0.05f, 0.5f), V(0.95f, 0.5f, 1.0f), hide);
            B("Snout", head, V(0f, -0.05f, 1.35f), V(0.7f, 0.3f, 1.25f), hide);
            for (int i = 0; i < 5; i++)
                foreach (float s in Sides)
                    Cone("Tooth", head, V(s * 0.32f, -0.22f, 0.85f + i * 0.22f), V(0.07f, 0.14f, 0.07f), White, V(180f, 0f, 0f));
            var jaw = G("Jaw", head, V(0f, -0.22f, 0.2f));
            B("Lower", jaw, V(0f, -0.06f, 1.05f), V(0.68f, 0.16f, 1.65f), belly);
            for (int i = 0; i < 4; i++)
                foreach (float s in Sides)
                    Cone("Tooth", jaw, V(s * 0.3f, 0.02f, 0.75f + i * 0.25f), V(0.07f, 0.13f, 0.07f), White);
            motion.Add(jaw, CreatureMotion.Kind.Chomp, Vector3.right, 16f, 2.2f);
            foreach (float s in Sides)
            {
                S("Bump", head, V(s * 0.3f, 0.35f, 0.35f), V(0.32f, 0.26f, 0.32f), hide);
                S("Eye", head, V(s * 0.3f, 0.4f, 0.49f), V(0.17f, 0.12f, 0.06f), new Color(1f, 0.85f, 0.2f), Vector3.zero, true);
                B("Slit", head, V(s * 0.3f, 0.4f, 0.525f), V(0.03f, 0.1f, 0.02f), Black);
                S("Nostril", head, V(s * 0.1f, 0.12f, 1.95f), V(0.08f, 0.06f, 0.08f), Black);
            }
            if (headgear == "crown")
            {
                C("Crown", head, V(0f, 0.38f, 0.42f), V(0.6f, 0.08f, 0.5f), trim);
                for (int i = 0; i < 5; i++)
                {
                    float a = i * Mathf.PI * 2f / 5f;
                    Cone("Point", head, V(Mathf.Sin(a) * 0.28f, 0.44f, 0.42f + Mathf.Cos(a) * 0.22f), V(0.1f, 0.2f, 0.1f), trim);
                }
            }
            var legs = new[] { V(-0.75f, 0.75f, 1.0f), V(0.75f, 0.75f, 1.0f), V(-0.75f, 0.75f, -0.9f), V(0.75f, 0.75f, -0.9f) };
            for (int i = 0; i < 4; i++)
            {
                float s = legs[i].x < 0f ? -1f : 1f;
                var leg = G("Leg", root, legs[i]);
                K("Thigh", leg, V(s * 0.25f, -0.35f, 0f), V(0.32f, 0.38f, 0.32f), hide, V(0f, 0f, s * 30f));
                S("Foot", leg, V(s * 0.45f, -0.68f, 0.1f), V(0.42f, 0.15f, 0.52f), dark);
                motion.Add(leg, CreatureMotion.Kind.Stride, Vector3.right, 26f, 1.3f, i == 0 || i == 3 ? 0f : Mathf.PI);
            }
            var tail = G("Tail", root, V(0f, 0.95f, -1.4f));
            motion.Add(tail, CreatureMotion.Kind.Sway, Vector3.up, 18f, 2.8f);
            S("Seg", tail, V(0f, -0.05f, -0.6f), V(0.9f, 0.6f, 1.45f), hide);
            S("Seg", tail, V(0f, -0.15f, -1.5f), V(0.6f, 0.42f, 1.3f), hide);
            S("Seg", tail, V(0f, -0.24f, -2.3f), V(0.36f, 0.26f, 1.05f), hide);
            for (int i = 0; i < 4; i++) Cone("Scute", tail, V(0f, 0.2f - i * 0.09f, -0.5f - i * 0.5f), V(0.14f, 0.26f, 0.22f), dark);
        }

        // ------------------------------------------------------------------ King of Coral

        private void Crab()
        {
            var shell = body;
            var dark = Color.Lerp(body, Color.black, 0.3f);
            var coral = new Color(1f, 0.5f, 0.45f);
            S("Shell", root, V(0f, 1.4f, 0f), V(2.6f, 1.2f, 2.0f), shell);
            S("Rim", root, V(0f, 1.25f, 0f), V(2.7f, 0.5f, 2.1f), dark);
            for (int i = 0; i < 6; i++)
            {
                float a = i * 1.05f;
                S("Spot", root, V(Mathf.Cos(a) * 0.7f, 1.88f, Mathf.Sin(a) * 0.5f), V(0.25f, 0.1f, 0.25f), Color.Lerp(shell, Color.white, 0.3f));
            }
            // A crown of coral on his shell.
            C("CrownBand", root, V(0f, 1.98f, 0f), V(0.9f, 0.08f, 0.72f), trim);
            for (int i = 0; i < 6; i++)
            {
                float a = i * Mathf.PI * 2f / 6f;
                Vector3 at = V(Mathf.Sin(a) * 0.36f, 2.2f, Mathf.Cos(a) * 0.28f);
                C("Branch", root, at, V(0.08f, 0.24f, 0.08f), coral, V(Mathf.Cos(a) * 15f, 0f, -Mathf.Sin(a) * 15f));
                S("Bud", root, at + V(Mathf.Sin(a) * 0.07f, 0.26f, Mathf.Cos(a) * 0.07f), V(0.16f, 0.16f, 0.16f), coral);
            }
            foreach (float s in Sides)
            {
                C("Stalk", root, V(s * 0.36f, 2.05f, 0.72f), V(0.09f, 0.28f, 0.09f), dark);
                S("Eyeball", root, V(s * 0.36f, 2.42f, 0.72f), V(0.34f, 0.34f, 0.34f), White);
                S("Pupil", root, V(s * 0.34f, 2.44f, 0.87f), V(0.15f, 0.15f, 0.06f), Black);
                B("Brow", root, V(s * 0.34f, 2.64f, 0.82f), V(0.3f, 0.07f, 0.06f), Black, V(0f, 0f, s * 20f));

                var claw = G("Claw", root, V(s * 1.15f, 1.45f, 0.6f));
                K("Arm", claw, V(s * 0.25f, 0.3f, 0.25f), V(0.3f, 0.5f, 0.3f), shell, V(40f, 0f, s * -30f));
                S("Pincer", claw, V(s * 0.45f, 0.85f, 0.65f), V(0.72f, 0.58f, 0.88f), shell);
                Cone("Finger", claw, V(s * 0.45f, 0.98f, 1.0f), V(0.26f, 0.55f, 0.24f), dark, V(80f, 0f, 0f));
                var thumb = G("Thumb", claw, V(s * 0.45f, 0.75f, 1.0f));
                Cone("Finger", thumb, Vector3.zero, V(0.22f, 0.45f, 0.2f), dark, V(105f, 0f, 0f));
                motion.Add(thumb, CreatureMotion.Kind.Chomp, Vector3.right, 25f, 3f, s);
                motion.Add(claw, CreatureMotion.Kind.Sway, Vector3.right, 10f, 1.5f, s);

                for (int i = 0; i < 3; i++)
                {
                    var leg = G("Leg", root, V(s * 1.1f, 1.1f, 0.3f - i * 0.5f));
                    K("Upper", leg, V(s * 0.4f, 0.05f, 0f), V(0.18f, 0.45f, 0.18f), shell, V(0f, 0f, s * -65f));
                    K("Lower", leg, V(s * 0.8f, -0.55f, 0f), V(0.15f, 0.42f, 0.15f), dark, V(0f, 0f, s * -12f));
                    motion.Add(leg, CreatureMotion.Kind.Stride, Vector3.forward, 14f, 1.6f, (i + (s > 0f ? 1 : 0)) % 2 == 0 ? 0f : Mathf.PI);
                }
            }
            B("Mouth", root, V(0f, 1.25f, 1.02f), V(0.5f, 0.1f, 0.05f), Black);
        }

        // ------------------------------------------------------------------ ghoul

        private void Ghoul()
        {
            var hide = body;
            var dark = Color.Lerp(body, Color.black, 0.35f);
            var eyes = new Color(1f, 0.85f, 0.2f);
            foreach (float s in Sides)
            {
                var leg = G("Leg", root, V(s * 0.45f, 1.05f, -0.2f));
                K("Thigh", leg, V(0f, -0.28f, 0.15f), V(0.42f, 0.4f, 0.42f), hide, V(-30f, 0f, 0f));
                K("Shin", leg, V(0f, -0.72f, 0.02f), V(0.32f, 0.34f, 0.32f), hide, V(18f, 0f, 0f));
                B("Foot", leg, V(0f, -0.98f, 0.18f), V(0.42f, 0.13f, 0.62f), dark);
                for (int t = -1; t <= 1; t++) Cone("Claw", leg, V(t * 0.13f, -1.0f, 0.5f), V(0.07f, 0.2f, 0.07f), Bone, V(90f, 0f, 0f));
                motion.Add(leg, CreatureMotion.Kind.Stride, Vector3.right, 26f, 1.2f, s > 0f ? 0f : Mathf.PI);
            }
            var torso = G("Torso", root, V(0f, 1.1f, 0f), V(25f, 0f, 0f));
            motion.Add(torso, CreatureMotion.Kind.Sway, Vector3.right, 3f, 1.6f);
            S("Belly", torso, V(0f, 0.4f, 0f), V(1.3f, 1.0f, 1.0f), hide);
            S("Chest", torso, V(0f, 1.0f, 0.05f), V(1.75f, 1.2f, 1.2f), hide);
            S("Hump", torso, V(0f, 1.25f, -0.35f), V(1.3f, 0.9f, 0.9f), dark);
            for (int i = 0; i < 4; i++) Cone("Spine", torso, V(0f, 1.6f - i * 0.25f, -0.62f - i * 0.04f), V(0.18f, 0.35f, 0.18f), trim, V(-60f, 0f, 0f));
            B("Rag", torso, V(0f, -0.05f, 0.42f), V(0.75f, 0.65f, 0.08f), Color.Lerp(trim, Color.black, 0.35f));
            var head = G("Skull", torso, V(0f, 1.45f, 0.55f), V(-25f, 0f, 0f));
            S("Head", head, V(0f, 0.12f, 0.25f), V(0.86f, 0.76f, 0.8f), hide);
            foreach (float s in Sides)
            {
                Cone("Horn", head, V(s * 0.28f, 0.45f, 0.15f), V(0.15f, 0.6f, 0.15f), Bone, V(-20f, 0f, s * -30f));
                Cone("Ear", head, V(s * 0.42f, 0.15f, 0.2f), V(0.12f, 0.35f, 0.1f), hide, V(0f, 0f, s * -75f));
                B("Eye", head, V(s * 0.18f, 0.17f, 0.6f), V(0.17f, 0.11f, 0.06f), eyes, V(0f, 0f, s * -14f), true);
                Cone("Fang", head, V(s * 0.15f, -0.1f, 0.62f), V(0.08f, 0.18f, 0.08f), Bone, V(180f, 0f, 0f));

                var arm = G("Arm", torso, V(s * 0.95f, 1.15f, 0.1f), V(-25f, 0f, s * 8f));
                S("Shoulder", arm, Vector3.zero, V(0.6f, 0.6f, 0.6f), dark);
                K("Upper", arm, V(0f, -0.5f, 0f), V(0.38f, 0.55f, 0.38f), hide);
                K("Fore", arm, V(0f, -1.25f, 0.1f), V(0.34f, 0.5f, 0.34f), hide);
                S("Hand", arm, V(0f, -1.8f, 0.15f), V(0.46f, 0.36f, 0.46f), dark);
                for (int t = -1; t <= 1; t++) Cone("Claw", arm, V(t * 0.13f, -1.95f, 0.25f), V(0.07f, 0.25f, 0.07f), Bone, V(160f, 0f, 0f));
                motion.Add(arm, CreatureMotion.Kind.Stride, Vector3.right, 22f, 1.2f, s > 0f ? Mathf.PI : 0f);
            }
            B("Mouth", head, V(0f, -0.08f, 0.6f), V(0.45f, 0.12f, 0.05f), new Color(0.2f, 0.03f, 0.03f));
        }

        // ------------------------------------------------------------------ Cedar sorcerer

        private void Tree()
        {
            var bark = new Color(0.4f, 0.27f, 0.16f);
            var barkDark = Color.Lerp(bark, Color.black, 0.35f);
            var needles = body;
            var magic = glow;
            foreach (float s in Sides)
            {
                var leg = G("Root", root, V(s * 0.36f, 0.95f, 0f));
                K("Stem", leg, V(0f, -0.45f, 0f), V(0.38f, 0.5f, 0.38f), bark);
                for (int t = -1; t <= 1; t++) Cone("Toe", leg, V(t * 0.15f, -0.9f, 0.15f), V(0.13f, 0.4f, 0.13f), barkDark, V(80f, t * 30f, 0f));
                motion.Add(leg, CreatureMotion.Kind.Stride, Vector3.right, 20f, 1f, s > 0f ? 0f : Mathf.PI);
            }
            C("Trunk", root, V(0f, 1.85f, 0f), V(1.15f, 0.95f, 1.05f), bark);
            for (int i = 0; i < 6; i++)
            {
                float a = i * Mathf.PI * 2f / 6f + 0.3f;
                B("Ridge", root, V(Mathf.Sin(a) * 0.56f, 1.8f, Mathf.Cos(a) * 0.51f), V(0.07f, 1.7f, 0.05f), barkDark, V(0f, a * Mathf.Rad2Deg, 0f));
            }
            foreach (float s in Sides)
            {
                S("Socket", root, V(s * 0.22f, 2.3f, 0.47f), V(0.26f, 0.19f, 0.12f), Black);
                S("Eye", root, V(s * 0.22f, 2.3f, 0.52f), V(0.13f, 0.09f, 0.05f), magic, Vector3.zero, true);
                B("Brow", root, V(s * 0.22f, 2.45f, 0.53f), V(0.3f, 0.08f, 0.08f), barkDark, V(0f, 0f, s * 20f));
                var arm = G("Branch", root, V(s * 0.55f, 2.45f, 0f));
                K("Bough", arm, V(s * 0.45f, -0.25f, 0f), V(0.22f, 0.55f, 0.22f), bark, V(0f, 0f, s * 60f));
                C("Twig", arm, V(s * 0.95f, -0.25f, 0.1f), V(0.08f, 0.2f, 0.08f), bark, V(30f, 0f, 0f));
                C("Leaves", arm, V(s * 1.0f, -0.5f, 0.05f), V(0.75f, 0.08f, 0.6f), needles);
                C("Leaves", arm, V(s * 1.05f, -0.38f, 0.05f), V(0.5f, 0.07f, 0.42f), needles);
                motion.Add(arm, CreatureMotion.Kind.Stride, Vector3.right, 18f, 1f, s > 0f ? Mathf.PI : 0f);
            }
            B("Mouth", root, V(0f, 2.05f, 0.52f), V(0.36f, 0.08f, 0.05f), Black);
            Cone("Moss", root, V(0f, 1.95f, 0.5f), V(0.36f, 0.5f, 0.14f), Color.Lerp(needles, Color.black, 0.3f), V(180f, 0f, 0f));
            S("Amulet", root, V(0f, 1.5f, 0.55f), V(0.2f, 0.2f, 0.1f), trim);

            // The cedar's flat layered boughs make his hair.
            var crown = G("Canopy", root, V(0f, 2.75f, 0f));
            motion.Add(crown, CreatureMotion.Kind.Sway, Vector3.forward, 3f, 1.2f);
            float[] radii = { 1.7f, 1.4f, 1.05f, 0.65f };
            for (int i = 0; i < radii.Length; i++)
                C("Tier", crown, V(i % 2 == 0 ? 0.05f : -0.05f, 0.12f + i * 0.36f, 0f), V(radii[i] * 2f * 0.9f, 0.09f, radii[i] * 1.6f * 0.9f), Color.Lerp(needles, Color.black, i * 0.06f));
            Cone("Top", crown, V(0f, 1.4f, 0f), V(0.45f, 0.5f, 0.45f), needles);

            var orbs = G("Orbs", root, Vector3.zero);
            motion.Add(orbs, CreatureMotion.Kind.Spin, Vector3.up, 0f, 70f);
            for (int i = 0; i < 3; i++)
            {
                float a = i * Mathf.PI * 2f / 3f;
                var orb = S("Orb", orbs, V(Mathf.Cos(a) * 1.7f, 2.2f, Mathf.Sin(a) * 1.7f), V(0.3f, 0.3f, 0.3f), magic, Vector3.zero, true);
                motion.Add(orb, CreatureMotion.Kind.Hover, Vector3.up, 0.2f, 2.5f, a);
            }
        }

        // ------------------------------------------------------------------ helpers

        private static Vector3 V(float x, float y, float z) => new Vector3(x, y, z);

        private Transform G(string name, Transform parent, Vector3 pos, Vector3 euler = default)
        {
            var t = new GameObject(name).transform;
            t.SetParent(parent, false);
            t.localPosition = pos;
            t.localRotation = Quaternion.Euler(euler);
            return t;
        }

        private Transform S(string name, Transform p, Vector3 pos, Vector3 size, Color c, Vector3 euler = default, bool lit = false)
            => Part(Primitive(PrimitiveType.Sphere), name, p, pos, size, c, euler, lit);

        private Transform B(string name, Transform p, Vector3 pos, Vector3 size, Color c, Vector3 euler = default, bool lit = false)
            => Part(Primitive(PrimitiveType.Cube), name, p, pos, size, c, euler, lit);

        /// <summary>Cylinder: x and z are the diameter, y is half the height, like Unity's own.</summary>
        private Transform C(string name, Transform p, Vector3 pos, Vector3 size, Color c, Vector3 euler = default, bool lit = false)
            => Part(Primitive(PrimitiveType.Cylinder), name, p, pos, size, c, euler, lit);

        private Transform K(string name, Transform p, Vector3 pos, Vector3 size, Color c, Vector3 euler = default, bool lit = false)
            => Part(Primitive(PrimitiveType.Capsule), name, p, pos, size, c, euler, lit);

        /// <summary>Cone standing on <paramref name="pos"/>; size is base width, height, base depth.</summary>
        private Transform Cone(string name, Transform p, Vector3 pos, Vector3 size, Color c, Vector3 euler = default, bool lit = false)
            => Part(ConeMesh(), name, p, pos, size, c, euler, lit);

        private void Ring(string name, Transform p, Vector3 pos, float size, Color c)
            => C(name, p, pos, V(size, 0.03f, size), c, V(0f, 0f, 90f));

        private Transform Part(Mesh mesh, string name, Transform p, Vector3 pos, Vector3 size, Color c, Vector3 euler, bool lit)
        {
            var go = new GameObject(name, typeof(MeshFilter), typeof(MeshRenderer));
            var t = go.transform;
            t.SetParent(p, false);
            t.localPosition = pos;
            t.localRotation = Quaternion.Euler(euler);
            t.localScale = size;
            go.GetComponent<MeshFilter>().sharedMesh = mesh;
            go.GetComponent<MeshRenderer>().sharedMaterial = Mat(c, lit);
            return t;
        }

        private Material Mat(Color c, bool lit)
        {
            var key = ((Color32)c, lit);
            if (mats.TryGetValue(key, out var m)) return m;
            m = template != null ? new Material(template) : new Material(Shader.Find("Universal Render Pipeline/Lit"));
            m.name = "Boss_" + ColorUtility.ToHtmlStringRGB(c) + (lit ? "_Glow" : "");
            if (m.HasProperty(BaseColorId)) m.SetColor(BaseColorId, c);
            m.color = c;
            if (m.HasProperty("_BaseMap")) m.SetTexture("_BaseMap", null);
            if (m.HasProperty("_Smoothness")) m.SetFloat("_Smoothness", lit ? 0.6f : 0.2f);
            if (lit && m.HasProperty(EmissionId))
            {
                m.EnableKeyword("_EMISSION");
                m.SetColor(EmissionId, c * 1.6f);
                m.globalIlluminationFlags = MaterialGlobalIlluminationFlags.None;
            }
            mats[key] = m;
            return m;
        }

        private static Mesh Primitive(PrimitiveType type)
        {
            if (primitives.TryGetValue(type, out var mesh) && mesh != null) return mesh;
            var go = PrimitiveFactory.Create(type);
            mesh = go.GetComponent<MeshFilter>().sharedMesh;
            if (Application.isPlaying) Object.Destroy(go); else Object.DestroyImmediate(go);
            primitives[type] = mesh;
            return mesh;
        }

        private static Mesh ConeMesh()
        {
            if (cone != null) return cone;
            const int n = 10;
            var v = new List<Vector3>();
            var t = new List<int>();
            for (int i = 0; i < n; i++)
            {
                float a0 = i * Mathf.PI * 2f / n, a1 = (i + 1) * Mathf.PI * 2f / n;
                var b0 = new Vector3(Mathf.Cos(a0) * 0.5f, 0f, Mathf.Sin(a0) * 0.5f);
                var b1 = new Vector3(Mathf.Cos(a1) * 0.5f, 0f, Mathf.Sin(a1) * 0.5f);
                // Both windings, each with its own vertices, so it reads from any side whatever the culling.
                Tri(v, t, b0, Vector3.up, b1);
                Tri(v, t, b1, Vector3.up, b0);
                Tri(v, t, Vector3.zero, b0, b1);
                Tri(v, t, Vector3.zero, b1, b0);
            }
            cone = new Mesh { name = "BossCone" };
            cone.SetVertices(v);
            cone.SetTriangles(t, 0);
            cone.RecalculateNormals();
            cone.RecalculateBounds();
            return cone;
        }

        private static void Tri(List<Vector3> v, List<int> t, Vector3 a, Vector3 b, Vector3 c)
        {
            int s = v.Count;
            v.Add(a); v.Add(b); v.Add(c);
            t.Add(s); t.Add(s + 1); t.Add(s + 2);
        }

        private static Transform Find(Transform root, string name)
        {
            if (root.name == name) return root;
            foreach (Transform c in root) { var f = Find(c, name); if (f != null) return f; }
            return null;
        }
    }
}
