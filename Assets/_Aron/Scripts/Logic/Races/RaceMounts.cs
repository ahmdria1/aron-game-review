// (c) Ahmed Alamrani. All rights reserved.
using System;
using System.Collections.Generic;

namespace Aron.Logic.Races
{
    /// <summary>Where a race is being run. Story, stage and ghost races use the mount's real stats; ranked online races normalise them.</summary>
    public enum RaceMode { Story, Stage, Ghost, Ranked }

    /// <summary>The numbers a mount is made of. Horse = 1 on the first four; resistances are 0 or more (see <see cref="TerrainRules"/>).</summary>
    [Serializable]
    public class MountTraits
    {
        /// <summary>Top speed on the straights, accelerations, turning grip, how tall an obstacle it clears.</summary>
        public float topSpeed = 1f, accel = 1f, turning = 1f, jump = 1f;
        /// <summary>How much of the sand/sandstorm and heat slowdown it shrugs off (0 = none).</summary>
        public float sandResist, heatResist;
        /// <summary>What the special ability is worth in the power budget (a glide, a high path, a straight-line sprint).</summary>
        public float ability;

        public MountTraits Copy() => (MountTraits)MemberwiseClone();

        public MountTraits Scaled(float k) => new MountTraits
        {
            topSpeed = topSpeed * k, accel = accel * k, turning = turning * k, jump = jump * k,
            sandResist = sandResist * k, heatResist = heatResist * k, ability = ability * k,
        };
    }

    /// <summary>Special ability of a mount. Strength scales with the stats in ranked races, the kind never changes.</summary>
    [Serializable]
    public class MountAbility
    {
        /// <summary>none, steady (horse: forgiving recovery), sprint (ostrich), high_path (giraffe), sand_walker (camel), glide (falcon).</summary>
        public string kind = "none";
        /// <summary>Glide length in seconds (glide) or extra clearance in metres (high_path).</summary>
        public float amount;
        /// <summary>Gliding mounts can't pick up ground pickups (coins, food) until they land.</summary>
        public bool blocksGroundPickups;
        /// <summary>The mount can take shortcuts that sit above the normal path.</summary>
        public bool highShortcuts;
    }

    [Serializable]
    public class MountPrice
    {
        /// <summary>Direct price tier in US dollars (what Ahmed picks in Play Console and App Store Connect). 0 for the free horse.</summary>
        public float usd;
        public string productId = "";
        // A mount is owned only by a real purchase. Race points only rent it for 48 hours (see RacePoints).
    }

    [Serializable]
    public class MountDef
    {
        public string id, kind, nameAr, nameEn;
        /// <summary>UI string key of the one-line style ("mount_style_ostrich").</summary>
        public string styleKey;
        public MountTraits stats = new MountTraits();
        public MountAbility ability = new MountAbility();
        public MountPrice price = new MountPrice();
        public bool isDefault;
    }

    [Serializable]
    public class MountFile
    {
        public int version = 1;
        /// <summary>Ranked races give every mount this much overall power.</summary>
        public float powerBudget = 8.1f;
        /// <summary>How far a mount's real power may sit from the budget in the data (keeps story balance honest).</summary>
        public float rawTolerance = 0.04f;
        public MountDef[] mounts = Array.Empty<MountDef>();

        public const float WTop = 3f, WAccel = 1.5f, WTurn = 1.5f, WJump = 1.5f, WResist = 1f, WAbility = 1f;
        public static readonly string[] Kinds = { "none", "steady", "sprint", "high_path", "sand_walker", "glide" };

        public MountDef Find(string id)
        {
            foreach (var m in mounts) if (m != null && m.id == id) return m;
            return null;
        }

        public MountDef Default
        {
            get { foreach (var m in mounts) if (m != null && m.isDefault) return m; return mounts.Length > 0 ? mounts[0] : null; }
        }

        /// <summary>One number for how strong a mount is overall: the weighted sum of its stats.</summary>
        public static float Power(MountTraits s) =>
            WTop * s.topSpeed + WAccel * s.accel + WTurn * s.turning + WJump * s.jump + WResist * (s.sandResist + s.heatResist) + WAbility * s.ability;

        /// <summary>The stats a racer really gets. Ranked: every stat is scaled by the same factor so the power equals the budget; the shape (the style) stays.</summary>
        public MountTraits StatsFor(MountDef m, RaceMode mode)
        {
            if (m == null) return new MountTraits();
            if (mode != RaceMode.Ranked) return m.stats.Copy();
            float p = Power(m.stats);
            return p <= 0f ? m.stats.Copy() : m.stats.Scaled(powerBudget / p);
        }

        /// <summary>Glide seconds and other ability amounts scale with the same ranked factor as the stats.</summary>
        public float AbilityAmount(MountDef m, RaceMode mode)
        {
            if (m == null) return 0f;
            if (mode != RaceMode.Ranked) return m.ability.amount;
            float p = Power(m.stats);
            return p <= 0f ? m.ability.amount : m.ability.amount * powerBudget / p;
        }

        public List<string> Validate()
        {
            var e = new List<string>();
            if (mounts == null || mounts.Length == 0) { e.Add("no mounts"); return e; }
            var ids = new HashSet<string>(); var products = new HashSet<string>();
            int defaults = 0;
            foreach (var m in mounts)
            {
                string w = "mount " + m.id;
                if (string.IsNullOrEmpty(m.id) || !ids.Add(m.id)) e.Add(w + ": missing or duplicate id");
                if (string.IsNullOrEmpty(m.nameAr) || string.IsNullOrEmpty(m.nameEn)) e.Add(w + ": names");
                if (string.IsNullOrEmpty(m.styleKey)) e.Add(w + ": style key");
                if (Array.IndexOf(Kinds, m.ability.kind) < 0) e.Add(w + ": unknown ability " + m.ability.kind);
                var s = m.stats;
                if (s.topSpeed < 0.8f || s.topSpeed > 1.3f) e.Add(w + ": top speed 0.8..1.3");
                if (s.accel < 0.6f || s.accel > 1.3f) e.Add(w + ": accel 0.6..1.3");
                if (s.turning < 0.5f || s.turning > 1.3f) e.Add(w + ": turning 0.5..1.3");
                if (s.jump < 0.6f || s.jump > 1.6f) e.Add(w + ": jump 0.6..1.6");
                if (s.sandResist < 0f || s.heatResist < 0f || s.sandResist > 1.5f || s.heatResist > 1.5f) e.Add(w + ": resistances 0..1.5");
                if (Math.Abs(Power(s) - powerBudget) > powerBudget * rawTolerance) e.Add(w + ": raw power " + Power(s).ToString("0.00") + " is too far from the budget");
                if (m.isDefault)
                {
                    defaults++;
                    if (m.price.usd != 0f || !string.IsNullOrEmpty(m.price.productId)) e.Add(w + ": the default mount is free");
                }
                else
                {
                    if (m.price.usd <= 0f) e.Add(w + ": needs a direct price");
                    if (string.IsNullOrEmpty(m.price.productId) || !products.Add(m.price.productId)) e.Add(w + ": product id");
                }
                if (m.ability.kind == "glide" && !m.ability.blocksGroundPickups) e.Add(w + ": a glide can't collect ground pickups");
                if (m.ability.kind == "glide" && m.ability.amount <= 0f) e.Add(w + ": glide needs seconds");
            }
            if (defaults != 1) e.Add("exactly one default mount");
            return e;
        }
    }

    /// <summary>How the ground and the weather slow a mount down.</summary>
    public static class TerrainRules
    {
        public const float SandSlow = 0.25f, SandstormSlow = 0.20f, HeatSlow = 0.10f;

        /// <summary>
        /// Speed multiplier from the track: sand slows, a haze on sand is a sandstorm (slower still), a clear day on
        /// sand or asphalt is hot. Resistance divides the slowdown: slow / (1 + resist).
        /// </summary>
        public static float SpeedFactor(MountTraits s, string surface, string weather, string time)
        {
            float sand = 0f, heat = 0f;
            bool sandy = surface == "sand" || surface == "dirt";
            if (sandy) sand += SandSlow;
            if (sandy && weather == "haze") sand += SandstormSlow;
            if ((sandy || surface == "asphalt" || surface == "savanna") && weather == "clear" && time != "night") heat += HeatSlow;
            return 1f - sand / (1f + s.sandResist) - heat / (1f + s.heatResist);
        }

        /// <summary>Speed multiplier through a bend of the given sharpness (0 straight .. 1 hairpin). Weak turners lose speed.</summary>
        public static float CornerFactor(MountTraits s, float sharpness)
        {
            sharpness = Math.Max(0f, Math.Min(1f, sharpness));
            float grip = Math.Min(1.2f, s.turning);
            return 1f - sharpness * Math.Max(0f, 1f - grip) * 0.9f;
        }

        /// <summary>Straight-line speed multiplier: the ostrich's sprint adds a little once it's been running straight.</summary>
        public static float StraightFactor(MountAbility a, float straightSeconds) =>
            a != null && a.kind == "sprint" ? 1f + Math.Min(0.06f, Math.Max(0f, straightSeconds - 1f) * 0.02f) : 1f;

        /// <summary>Tallest obstacle (in metres) a mount can clear, from its jump stat, plus the giraffe's extra reach.</summary>
        public static float ObstacleClearance(MountTraits s, MountAbility a, float baseHeight = 1.0f) =>
            baseHeight * s.jump + (a != null && a.kind == "high_path" ? a.amount : 0f);

        public static bool CanTakeHighShortcut(MountAbility a) => a != null && a.highShortcuts;
    }

    /// <summary>A glide over a gap. The falcon can't pick up anything from the ground until it lands.</summary>
    public class GlideState
    {
        public float Remaining { get; private set; }
        public bool Gliding => Remaining > 0f;
        private readonly bool blocksPickups;
        public GlideState(MountAbility a, float seconds) { blocksPickups = a != null && a.blocksGroundPickups; Remaining = 0f; maxSeconds = seconds; }
        private readonly float maxSeconds;
        public bool Start() { if (Gliding || maxSeconds <= 0f) return false; Remaining = maxSeconds; return true; }
        public void Tick(float dt) { Remaining = Math.Max(0f, Remaining - dt); }
        public void Land() { Remaining = 0f; }
        public bool CanCollectGround => !(Gliding && blocksPickups);
    }
}
