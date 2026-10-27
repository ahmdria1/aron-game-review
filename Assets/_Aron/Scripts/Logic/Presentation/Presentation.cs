// (c) Ahmed Alamrani. All rights reserved.
using System;
using System.Collections.Generic;

namespace Aron.Logic.Presentation
{
    /// <summary>Camera for a stretch of a stage. Stage JSON: "cameras": [{"from":0,"mode":"chase"}, ...] (metres).</summary>
    [Serializable]
    public class CameraSegment
    {
        public float from;
        /// <summary>chase (behind), reverse (running toward the camera), side (2.5D side scroll) or arena (fixed boss camera).</summary>
        public string mode = "chase";
        public float blend = 0.6f;

        public static readonly string[] Modes = { "chase", "reverse", "side", "arena" };
    }

    public static class CameraPlan
    {
        /// <summary>The segment in force at a distance; segments are sorted by from. Chase when there are none.</summary>
        public static CameraSegment At(CameraSegment[] plan, float z)
        {
            CameraSegment cur = null;
            if (plan != null) foreach (var s in plan) if (s.from <= z) cur = s;
            return cur ?? new CameraSegment();
        }

        public static List<string> Validate(CameraSegment[] plan, bool boss)
        {
            var e = new List<string>();
            float last = -1f;
            foreach (var s in plan ?? Array.Empty<CameraSegment>())
            {
                if (Array.IndexOf(CameraSegment.Modes, s.mode) < 0) e.Add("unknown camera mode " + s.mode);
                if (s.from <= last) e.Add("camera segments must go forward");
                if (s.blend < 0f || s.blend > 2f) e.Add("camera blend 0..2 s");
                if (s.mode == "arena" && !boss) e.Add("the arena camera is for boss fights");
                last = s.from;
            }
            return e;
        }

        /// <summary>Blend weight toward a new segment <paramref name="t"/> seconds after entering it (smoothstep).</summary>
        public static float Blend(CameraSegment s, float t)
        {
            if (s.blend <= 0f) return 1f;
            float x = Math.Max(0f, Math.Min(1f, t / s.blend));
            return x * x * (3f - 2f * x);
        }
    }

    /// <summary>Breakable crates. Our own designs: the Eid firecracker crate (صندوق الطراطيع) and the red-hot chili crate (صندوق الشطة) are the two hazards.</summary>
    public static class BoxKinds
    {
        public const string Normal = "normal", Bounce = "bounce", Arrow = "arrow", Lantern = "lantern", Firecracker = "firecracker", Chili = "chili",
            Iron = "iron", Mystery = "mystery", Life = "life", Bonus = "bonus";
        public static readonly string[] All = { Normal, Bounce, Arrow, Lantern, Firecracker, Chili, Iron, Mystery, Life, Bonus };
        /// <summary>Seconds the firecracker crate fuse burns before the confetti pops.</summary>
        public const float FuseSeconds = 3f;

        /// <summary>Boxes that count for the all-boxes gem (iron can't break, so it doesn't count).</summary>
        public static bool Counts(string kind) => kind != Iron;
        /// <summary>The firecracker pops in sparks and confetti after its fuse; the chili crate puffs at once on touch.</summary>
        public static bool Pops(string kind) => kind == Firecracker || kind == Chili;
        public static bool PopsOnTouch(string kind) => kind == Chili;
        public static bool BreaksOnSpin(string kind) => kind != Iron && kind != Bounce;

        /// <summary>A mystery box's prize from a roll in 0..1: coins, a life box's life or a bisht.</summary>
        public static string MysteryPrize(double roll) => roll < 0.7 ? "coins" : roll < 0.9 ? "food_card" : "bisht";

        /// <summary>How much fuse is left (1 just lit, 0 when it pops). Shown as a shrinking fuse with sparkles, no digits.</summary>
        public static float FuseLeft(float sinceLit) => Math.Max(0f, 1f - sinceLit / FuseSeconds);
    }

    /// <summary>The counters slide in when something is picked up and slide away after a moment.</summary>
    public class ContextualHud
    {
        public const float Show = 2.2f;
        private readonly Dictionary<string, float> left = new Dictionary<string, float>();
        public bool Paused;

        public void Pickup(string counter) => left[counter] = Show;
        public bool Visible(string counter) => Paused || (left.TryGetValue(counter, out var t) && t > 0f);

        public void Tick(float dt)
        {
            var keys = new List<string>(left.Keys);
            foreach (var k in keys) left[k] = left[k] - dt;
        }
    }

    /// <summary>The punch of a hit: a short freeze, a push back, a shake and a white flash (all smaller with reduce motion).</summary>
    public struct HitFeel
    {
        public float freezeSeconds, knockback, shake, flash;

        public static HitFeel For(string kind, bool reduceMotion)
        {
            HitFeel h;
            switch (kind)
            {
                case "boss_hit": h = new HitFeel { freezeSeconds = 0.08f, knockback = 4f, shake = 0.5f, flash = 0.6f }; break;
                case "explosion": h = new HitFeel { freezeSeconds = 0.07f, knockback = 6f, shake = 0.8f, flash = 0.8f }; break;
                case "stomp": h = new HitFeel { freezeSeconds = 0.05f, knockback = 0f, shake = 0.15f, flash = 0f }; break;
                default: h = new HitFeel { freezeSeconds = 0.06f, knockback = 3f, shake = 0.3f, flash = 0.4f }; break;
            }
            if (reduceMotion) { h.shake = 0f; h.flash = 0f; }
            return h;
        }
    }

    /// <summary>Squash on landing and stretch on take-off, as a scale for the body.</summary>
    public static class SquashStretch
    {
        /// <summary>Vertical scale: above 1 when moving up fast, below 1 just after landing.</summary>
        public static float Vertical(float verticalSpeed, float sinceLanded)
        {
            float stretch = 1f + Math.Max(-0.12f, Math.Min(0.18f, verticalSpeed * 0.012f));
            if (sinceLanded >= 0f && sinceLanded < 0.15f) stretch -= 0.22f * (1f - sinceLanded / 0.15f);
            return stretch;
        }

        /// <summary>Width follows so the volume stays about the same.</summary>
        public static float Horizontal(float vertical) => (float)(1.0 / Math.Sqrt(Math.Max(0.5f, vertical)));
    }

    /// <summary>The bonus majlis (مجلس المكافأة): reached from a bonus crate; falling there just sends you back to the stage, no life lost.</summary>
    public static class BonusRound
    {
        public static bool FallCostsLife(bool inBonus) => !inBonus;
    }
}
