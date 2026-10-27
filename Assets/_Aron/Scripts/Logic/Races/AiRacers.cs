// (c) Ahmed Alamrani. All rights reserved.
using System;
using System.Collections.Generic;

namespace Aron.Logic.Races
{
    /// <summary>
    /// How one difficulty drives the computer racers (Resources/Races/ai_racers.json). Nothing here can push a racer past its mount's
    /// top speed: <see cref="AiDriver"/> clamps to the mount, so "hard" is better driving, never cheating.
    /// </summary>
    [Serializable]
    public class AiProfile
    {
        public string difficulty;
        /// <summary>Share of the mount's top speed it drives at on a clean run.</summary>
        public float skill = 0.95f;
        /// <summary>Slips per minute of driving (a clipped obstacle, a wide corner), the speed share kept while slipping, and for how long.</summary>
        public float mistakesPerMinute = 3f, mistakeSpeed = 0.7f, mistakeSeconds = 1.2f;
        /// <summary>Chance it dodges an obstacle in time (a failed dodge is a slip).</summary>
        public float avoidChance = 0.9f;
        /// <summary>How far it wanders from the best racing line, in metres.</summary>
        public float lineWander = 0.8f;
        /// <summary>Chance to take a shortcut when one is open, and to use the mount's ability (glide, high path).</summary>
        public float shortcutChance = 0.5f, abilityUse = 0.7f;
        /// <summary>Light catch-up and ease-off, as a share of top speed, full at <see cref="fullGap"/> metres. Capped at the mount's top speed.</summary>
        public float catchUp = 0.04f, holdBack = 0.03f, fullGap = 60f;
        /// <summary>Seconds before it reacts to something new ahead.</summary>
        public float reaction = 0.4f;
    }

    [Serializable]
    public class AiRacerFile
    {
        public int version = 1;
        public int maxOffline = 7;
        public AiProfile[] profiles = Array.Empty<AiProfile>();
        /// <summary>Share of speed lost on sand by a mount with no sand resistance (the camel loses none).</summary>
        public float sandPenalty = 0.3f;
        /// <summary>Chance a rival uses another mount than its own, so fields aren't predictable.</summary>
        public float mountVariety = 0.25f;

        public AiProfile Profile(string d)
        {
            foreach (var p in profiles) if (p.difficulty == d) return p;
            foreach (var p in profiles) if (p.difficulty == "normal") return p;
            return profiles.Length > 0 ? profiles[0] : new AiProfile();
        }

        public List<string> Validate()
        {
            var e = new List<string>();
            if (maxOffline < 1 || maxOffline > 7) e.Add("up to 7 computer racers offline (8 seats with the player)");
            if (sandPenalty < 0f || sandPenalty > 0.6f) e.Add("sand penalty 0..0.6");
            foreach (var d in new[] { "easy", "normal", "hard" })
            {
                AiProfile p = null; foreach (var x in profiles) if (x.difficulty == d) p = x;
                if (p == null) { e.Add("profile missing: " + d); continue; }
                if (p.skill < 0.6f || p.skill > 1f) e.Add(d + ": skill 0.6..1 (never above the mount's top speed)");
                if (p.skill + p.catchUp > 1.0001f) e.Add(d + ": skill + catch-up must stay within the mount's top speed");
                if (p.mistakesPerMinute < 0f || p.mistakesPerMinute > 12f) e.Add(d + ": mistakes per minute 0..12");
                if (p.mistakeSpeed < 0.3f || p.mistakeSpeed > 0.95f || p.mistakeSeconds <= 0f) e.Add(d + ": mistake size");
                if (p.avoidChance < 0.5f || p.avoidChance > 1f) e.Add(d + ": avoid chance 0.5..1");
                if (p.catchUp < 0f || p.catchUp > 0.08f || p.holdBack < 0f || p.holdBack > 0.08f) e.Add(d + ": catch-up is light (0..0.08)");
                if (p.fullGap < 20f) e.Add(d + ": full gap at least 20 m");
                if (p.shortcutChance < 0f || p.shortcutChance > 1f || p.abilityUse < 0f || p.abilityUse > 1f) e.Add(d + ": chances 0..1");
            }
            AiProfile easy = Profile("easy"), normal = Profile("normal"), hard = Profile("hard");
            if (!(easy.mistakesPerMinute > normal.mistakesPerMinute && normal.mistakesPerMinute > hard.mistakesPerMinute)) e.Add("harder means fewer mistakes");
            if (!(easy.skill < normal.skill && normal.skill < hard.skill)) e.Add("harder means a higher skill");
            return e;
        }
    }

    /// <summary>What the driver is told about the road ahead each decision. Everything comes from track data, so no physics raycasts are needed.</summary>
    public struct AiContext
    {
        public float dt;
        /// <summary>Metres the player is ahead of this racer (negative: this racer leads). Only used for catch-up.</summary>
        public float gapToPlayer;
        public bool obstacleAhead, onSand, highPathOpen, shortcutOpen, pitAhead;
    }

    public struct AiCommand
    {
        public float speed;
        /// <summary>Sideways offset from the racing line, metres.</summary>
        public float lateral;
        public bool takeShortcut, useHighPath, glide, slipping;
    }

    /// <summary>
    /// One computer racer's driving brain. A decision every few ticks: pick a speed from its mount and skill, dodge or clip obstacles,
    /// slip now and then, take shortcuts and use its mount's trick like a person would. Deterministic for a given seed.
    /// </summary>
    public class AiDriver
    {
        private readonly AiProfile p;
        private readonly AiRacerFile cfg;
        private readonly MountTraits stats;
        private readonly MountAbility ability;
        private readonly float baseSpeed;
        private readonly Random rnd;
        private float slipLeft, reactLeft, wanderPhase;
        private bool decidedShortcut, wantsShortcut, obstacleSeen, obstacleHandled;
        public int Slips { get; private set; }
        public float Cap => baseSpeed * stats.topSpeed;

        /// <param name="baseSpeed">Metres per second of a top-speed 1.0 mount (the same for the player and every racer).</param>
        public AiDriver(AiRacerFile cfg, string difficulty, MountTraits mountStats, MountAbility mountAbility, float baseSpeed, int seed)
        {
            this.cfg = cfg; p = cfg.Profile(difficulty); stats = mountStats; ability = mountAbility ?? new MountAbility();
            this.baseSpeed = baseSpeed; rnd = new Random(seed); wanderPhase = (float)rnd.NextDouble() * 6.28f;
        }

        public AiCommand Step(AiContext c)
        {
            var cmd = new AiCommand();
            // Slips happen at a steady random rate, not on a timer.
            if (slipLeft > 0f) slipLeft -= c.dt;
            else if (rnd.NextDouble() < p.mistakesPerMinute / 60f * c.dt) { slipLeft = p.mistakeSeconds; Slips++; }

            // Obstacle: it reacts after its reaction time, then dodges or clips.
            if (c.obstacleAhead)
            {
                if (!obstacleSeen && !obstacleHandled) { obstacleSeen = true; reactLeft = p.reaction; }
                reactLeft -= c.dt;
                if (reactLeft <= 0f && obstacleSeen)
                {
                    obstacleSeen = false; obstacleHandled = true; reactLeft = 0f;
                    if (rnd.NextDouble() > p.avoidChance) { slipLeft = Math.Max(slipLeft, p.mistakeSeconds); Slips++; }
                    else cmd.lateral += (rnd.Next(2) == 0 ? -1f : 1f) * 1.5f;
                }
            }
            else { obstacleSeen = false; obstacleHandled = false; }

            // Shortcuts are decided once each time one opens.
            if (c.shortcutOpen)
            {
                if (!decidedShortcut) { decidedShortcut = true; wantsShortcut = rnd.NextDouble() < p.shortcutChance; }
                cmd.takeShortcut = wantsShortcut;
            }
            else decidedShortcut = false;

            // The mount's trick.
            cmd.useHighPath = c.highPathOpen && ability.kind == "high_path" && rnd.NextDouble() < Math.Min(1f, p.abilityUse * 0.9f + 0.05f);
            cmd.glide = c.pitAhead && ability.kind == "glide" && rnd.NextDouble() < p.abilityUse;

            // Speed: skill x mount, light catch-up or ease-off, sand slows unless the mount shrugs it off, slips cost speed. Never over the mount's top.
            float t = Math.Max(-1f, Math.Min(1f, c.gapToPlayer / Math.Max(1f, p.fullGap)));
            float band = t > 0f ? t * p.catchUp : t * p.holdBack;
            float k = Math.Min(1f, p.skill + band);
            float sandResist = ability.kind == "sand_walker" ? 1f : Math.Min(1f, stats.sandResist);
            if (c.onSand && !cmd.glide) k *= 1f - cfg.sandPenalty * (1f - sandResist);
            if (slipLeft > 0f) { k *= p.mistakeSpeed; cmd.slipping = true; }
            wanderPhase += c.dt * 0.7f;
            cmd.lateral += (float)Math.Sin(wanderPhase) * p.lineWander;
            cmd.speed = Math.Min(Cap, baseSpeed * stats.topSpeed * k);
            return cmd;
        }
    }

    /// <summary>How often the computer racers think and how they sense the track, by graphics tier. Cheap on weak phones.</summary>
    public static class AiTick
    {
        public static int Hz(QualityTier t) => t == QualityTier.Low ? 8 : t == QualityTier.Medium ? 15 : 30;
        /// <summary>Racers (nearest the player first) that think at full rate; the rest think at half rate.</summary>
        public static int FullRate(QualityTier t) => t == QualityTier.Low ? 2 : t == QualityTier.Medium ? 4 : 7;
        /// <summary>Only high-end phones may use physics raycasts, and then only at the decision rate, never every frame.</summary>
        public static bool MayRaycast(QualityTier t) => t == QualityTier.High;
        public static float Interval(QualityTier t, int rankByDistance) => 1f / (rankByDistance < FullRate(t) ? Hz(t) : Math.Max(1, Hz(t) / 2));
    }

    /// <summary>One computer racer in a race.</summary>
    public class AiEntrant
    {
        public string rivalId, name, nameAr, country, mount, style, model;
        public string difficulty;
    }

    public static class AiRoster
    {
        /// <summary>
        /// A field of computer racers from different countries. The host country's rival is always in; the rest are drawn without
        /// repeats. Each rides its own mount most of the time, sometimes another one.
        /// </summary>
        public static List<AiEntrant> Build(RivalFile rivals, AiRacerFile cfg, int count, string difficulty, string hostCountry, Random rnd, IList<string> mountIds = null)
        {
            var list = new List<AiEntrant>();
            count = Math.Max(0, Math.Min(count, cfg.maxOffline));
            var pool = new List<Rival>(rivals.rivals);
            Rival host = rivals.ForCountry(hostCountry);
            if (host != null && count > 0) { list.Add(Make(host, difficulty, cfg, rnd, mountIds)); pool.Remove(host); }
            while (list.Count < count && pool.Count > 0)
            {
                int i = rnd.Next(pool.Count);
                list.Add(Make(pool[i], difficulty, cfg, rnd, mountIds));
                pool.RemoveAt(i);
            }
            return list;
        }

        private static AiEntrant Make(Rival r, string difficulty, AiRacerFile cfg, Random rnd, IList<string> mountIds)
        {
            string mount = r.mount;
            var all = mountIds != null && mountIds.Count > 0 ? mountIds : RivalFile.Mounts;
            if (rnd.NextDouble() < cfg.mountVariety) mount = all[rnd.Next(all.Count)];
            if (mountIds != null && mountIds.Count > 0 && !mountIds.Contains(mount)) mount = mountIds[0];
            return new AiEntrant { rivalId = r.id, name = r.nameEn, nameAr = r.nameAr, country = r.country, mount = mount, style = r.style, model = r.model, difficulty = difficulty };
        }
    }
}
