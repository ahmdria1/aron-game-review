// (c) Ahmed Alamrani. All rights reserved.
using System;
using System.Collections.Generic;

namespace Aron.Logic
{
    /// <summary>Upgrade data for the camel, the horse and the falcon. Lives in Resources/Companions/companions.json.</summary>
    [Serializable]
    public class CompanionFile
    {
        public CompanionDef[] companions = Array.Empty<CompanionDef>();

        public CompanionDef Get(string id)
        {
            foreach (var c in companions) if (c.id == id) return c;
            return null;
        }

        public List<string> Validate()
        {
            var errors = new List<string>();
            foreach (var id in CompanionIds.All)
                if (Get(id) == null) errors.Add("missing " + id);
            foreach (var c in companions)
            {
                if (c.levels == null || c.levels.Length < 2) { errors.Add(c.id + ": needs at least two levels"); continue; }
                if (c.levels[0].cost != 0) errors.Add(c.id + ": level 1 is free");
                for (int i = 1; i < c.levels.Length; i++)
                {
                    var a = c.levels[i - 1]; var b = c.levels[i];
                    if (b.cost <= a.cost) errors.Add(c.id + ": level " + (i + 1) + " must cost more than the one before");
                    if (b.stamina < a.stamina || b.shieldHits < a.shieldHits || b.dateBonus < a.dateBonus || b.speed < a.speed
                        || b.gallopJump < a.gallopJump || b.chargeTime < a.chargeTime || b.driftDistance < a.driftDistance
                        || b.scoutRange < a.scoutRange || b.fetchRange < a.fetchRange || b.warnDistance < a.warnDistance
                        || b.leadRange < a.leadRange || b.enemySense < a.enemySense)
                        errors.Add(c.id + ": level " + (i + 1) + " is worse than the one before");
                    if (b.scoutCooldown > a.scoutCooldown || b.swoopCooldown > a.swoopCooldown || b.chargeCooldown > a.chargeCooldown || b.driftCooldown > a.driftCooldown)
                        errors.Add(c.id + ": level " + (i + 1) + " has a longer cooldown");
                }
            }
            return errors;
        }
    }

    public static class CompanionIds
    {
        public const string Camel = "camel", Horse = "horse", Falcon = "falcon", Oryx = "oryx";
        public static readonly string[] All = { Camel, Horse, Falcon, Oryx };
    }

    [Serializable]
    public class CompanionDef
    {
        public string id;
        public string nameAr;
        public CompanionLevel[] levels = Array.Empty<CompanionLevel>();

        public CompanionLevel At(int level) => levels[Math.Max(0, Math.Min(levels.Length - 1, level - 1))];
    }

    /// <summary>One upgrade level. Each animal only reads its own fields.</summary>
    [Serializable]
    public class CompanionLevel
    {
        /// <summary>Coins to buy this level. Level 1 is free.</summary>
        public int cost;
        public string descAr = "";

        // Camel
        public float stamina = 3f, staminaRegen = 0.6f, sprintBoost = 1.35f;
        public int shieldHits = 1;
        /// <summary>Extra dates per pickup while riding: 0.5 means one more date every second pickup.</summary>
        public float dateBonus = 0.5f;

        // Horse
        public float speed = 1.7f, gallopJump = 1.4f, chargeTime = 1.2f, chargeCooldown = 6f, driftDistance = 2.2f, driftCooldown = 2.5f;

        // Falcon
        public float scoutRange = 25f, scoutCooldown = 12f, swoopCooldown = 7f, fetchRange = 6f, warnDistance = 10f;

        // Oryx (Noura's). Its horn charge uses chargeTime and chargeCooldown.
        /// <summary>How far ahead it finds oases, water and hidden desert paths.</summary>
        public float leadRange = 20f;
        /// <summary>Enemies inside this distance make it stamp and snort.</summary>
        public float enemySense = 9f;
    }

    /// <summary>Owned upgrade levels, stored as "id=level" in the save.</summary>
    public static class CompanionUpgrades
    {
        public static int Level(SaveData data, string id)
        {
            string key = id + "=";
            foreach (var s in data.companionLevels)
                if (s.StartsWith(key, StringComparison.Ordinal) && int.TryParse(s.Substring(key.Length), out int l)) return Math.Max(1, l);
            return 1;
        }

        /// <summary>Price of the next level, or -1 when it's maxed out.</summary>
        public static int NextCost(CompanionDef def, SaveData data)
        {
            int l = Level(data, def.id);
            return l < def.levels.Length ? def.levels[l].cost : -1;
        }

        public static bool TryUpgrade(CompanionDef def, SaveData data)
        {
            int cost = NextCost(def, data);
            if (cost < 0 || data.coins < cost) return false;
            data.coins -= cost;
            int next = Level(data, def.id) + 1;
            data.companionLevels.RemoveAll(s => s.StartsWith(def.id + "=", StringComparison.Ordinal));
            data.companionLevels.Add(def.id + "=" + next);
            return true;
        }

        public static CompanionLevel Current(CompanionFile file, SaveData data, string id)
        {
            var def = file?.Get(id);
            return def == null || def.levels.Length == 0 ? new CompanionLevel() : def.At(Level(data, id));
        }
    }

    /// <summary>The camel's sand sprint. Stamina drains while sprinting and refills while walking.</summary>
    public class SandSprint
    {
        private readonly CompanionLevel stats;
        private bool exhausted;
        public float Stamina { get; private set; }
        public float Fraction => stats.stamina > 0f ? Stamina / stats.stamina : 0f;
        public bool Sprinting { get; private set; }

        public SandSprint(CompanionLevel stats) { this.stats = stats; Stamina = stats.stamina; }

        /// <summary>Returns the speed multiplier for this frame.</summary>
        public float Tick(float dt, bool wantsSprint, bool onSand)
        {
            if (exhausted && Fraction >= 0.3f) exhausted = false;
            Sprinting = wantsSprint && !exhausted && Stamina > 0f;
            if (Sprinting)
            {
                // Sand is the camel's home ground: sprinting there costs half.
                Stamina = Math.Max(0f, Stamina - dt * (onSand ? 0.5f : 1f));
                if (Stamina <= 0f) exhausted = true;
                return stats.sprintBoost;
            }
            Stamina = Math.Min(stats.stamina, Stamina + dt * stats.staminaRegen);
            return 1f;
        }
    }

    /// <summary>Hits the mount soaks up before the rider is thrown. Refilled each time Aron climbs on.</summary>
    public class HitShield
    {
        public int Left { get; private set; }
        public HitShield(int hits) { Left = Math.Max(0, hits); }
        public void Refill(int hits) => Left = Math.Max(0, hits);
        public bool Absorb()
        {
            if (Left <= 0) return false;
            Left--;
            return true;
        }
    }

    /// <summary>Extra dates while riding the camel. Keeps the fraction so 0.5 gives one extra every two pickups.</summary>
    public class DateCarry
    {
        private float carry;
        public int Bonus(int picked, float bonus)
        {
            carry += picked * Math.Max(0f, bonus);
            int whole = (int)Math.Floor(carry + 1e-4f);
            carry -= whole;
            return whole;
        }
    }

    /// <summary>A timed ability with a cooldown: horse charge, drift, falcon scout. Fraction feeds the HUD.</summary>
    public class Cooldown
    {
        public float Duration { get; set; }
        public float Length { get; set; }
        private float startedAt = -1e6f;

        public Cooldown(float duration, float cooldown) { Duration = duration; Length = cooldown; }

        public bool Ready(float now) => now - startedAt >= Duration + Length;
        public bool Active(float now) => now - startedAt < Duration;

        public bool TryStart(float now)
        {
            if (!Ready(now)) return false;
            startedAt = now;
            return true;
        }

        /// <summary>0 right after use, 1 when ready again.</summary>
        public float Fraction(float now)
        {
            float t = now - startedAt - Duration;
            return Length <= 0f ? 1f : Math.Max(0f, Math.Min(1f, t / Length));
        }

        public void Reset() => startedAt = -1e6f;
    }

    /// <summary>Horse moves: a long gallop jump, a barrier-breaking charge and a sideways drift.</summary>
    public class HorseMoves
    {
        public readonly Cooldown Charge;
        public readonly Cooldown Drift;
        private readonly CompanionLevel stats;
        public float DriftDirection { get; private set; }

        public HorseMoves(CompanionLevel stats)
        {
            this.stats = stats;
            Charge = new Cooldown(stats.chargeTime, stats.chargeCooldown);
            Drift = new Cooldown(0.3f, stats.driftCooldown);
        }

        public float SpeedMultiplier(float now) => Charge.Active(now) ? 1.35f : 1f;
        public float JumpMultiplier(bool galloping) => galloping ? stats.gallopJump : 1f;
        public bool BreaksBarriers(float now) => Charge.Active(now);

        /// <summary>Starts a drift to the left (-1) or right (+1). Returns the sideways distance, or 0.</summary>
        public float TryDrift(float now, float direction)
        {
            if (direction == 0f || !Drift.TryStart(now)) return 0f;
            DriftDirection = Math.Sign(direction);
            return DriftDirection * stats.driftDistance;
        }

        /// <summary>Hits slide off during a drift or a charge.</summary>
        public bool Dodging(float now) => Drift.Active(now) || Charge.Active(now);
    }

    /// <summary>The falcon's scouting run and trap warnings.</summary>
    public class FalconScout
    {
        public readonly Cooldown Scout;
        private readonly CompanionLevel stats;
        private readonly HashSet<int> warned = new HashSet<int>();

        public FalconScout(CompanionLevel stats)
        {
            this.stats = stats;
            Scout = new Cooldown(2f, stats.scoutCooldown);
        }

        public float Range => stats.scoutRange;

        /// <summary>Indices of secrets within scouting range ahead of the player (z only grows forward).</summary>
        public List<int> Reveal(float now, float playerZ, IReadOnlyList<float> secretZ)
        {
            var found = new List<int>();
            if (!Scout.TryStart(now)) return found;
            for (int i = 0; i < secretZ.Count; i++)
                if (secretZ[i] >= playerZ - 2f && secretZ[i] <= playerZ + stats.scoutRange) found.Add(i);
            return found;
        }

        /// <summary>True once per trap, the first time the player gets within warning distance of it.</summary>
        public bool Warn(int trapId, float distanceAhead)
        {
            if (distanceAhead < 0f || distanceAhead > stats.warnDistance) return false;
            return warned.Add(trapId);
        }
    }
}

namespace Aron.Logic
{
    /// <summary>Noura's oryx: runs beside her, horn-charges obstacles, leads to water and warns of enemies.</summary>
    public class OryxSense
    {
        public readonly Cooldown Horn;
        private readonly CompanionLevel stats;

        public OryxSense(CompanionLevel stats)
        {
            this.stats = stats;
            Horn = new Cooldown(stats.chargeTime, stats.chargeCooldown);
        }

        /// <summary>Spot beside Noura: half a metre behind and off to the side away from the nearest wall.</summary>
        public static float SideOffset(float nouraX, float halfWidth) => nouraX > 0f ? -1.4f : 1.4f;

        /// <summary>The nearest guide target (oasis, water, hidden path) ahead within range, or -1.</summary>
        public int Lead(float nouraZ, System.Collections.Generic.IReadOnlyList<float> targetZ)
        {
            int best = -1; float bestD = float.MaxValue;
            for (int i = 0; i < targetZ.Count; i++)
            {
                float d = targetZ[i] - nouraZ;
                if (d < 0f || d > stats.leadRange || d >= bestD) continue;
                best = i; bestD = d;
            }
            return best;
        }

        public bool SensesEnemy(float nearestEnemyDistance) => nearestEnemyDistance >= 0f && nearestEnemyDistance <= stats.enemySense;
    }
}
