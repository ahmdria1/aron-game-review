// (c) Ahmed Alamrani. All rights reserved.
using System;
using System.Collections.Generic;

namespace Aron.Logic.Races
{
    /// <summary>
    /// The other four shop tabs of race vehicles next to the animal mounts: sea, cars, bicycles and motorcycles. Everything
    /// here is original (no brands, no copies of real models). Each class has its own power budget, so in ranked online races
    /// a vehicle only chooses the SHAPE of its strength, never how strong it is. A vehicle is owned only by a real purchase;
    /// race points rent it for 48 hours (see <see cref="RacePoints"/>).
    /// Data: Resources/Mounts/race_vehicles.json. Names in 12 languages: UI keys "r" + id (rboat_dhow); tab names tab_sea ...
    /// </summary>
    [Serializable]
    public class VehicleTraits
    {
        /// <summary>Top speed on the straights, acceleration, turning grip and how tall an obstacle or wave it clears. A rough average is 1.</summary>
        public float topSpeed = 1f, accel = 1f, turning = 1f, jump = 1f;
        /// <summary>How much of the rough-ground (sand, mud, chop) and the weather (heat, rain, wind) slowdown it shrugs off.</summary>
        public float terrain, weather;
        /// <summary>Bicycles only: how long a rider can sprint before running out of breath. 0 for engines and sails.</summary>
        public float stamina;
        /// <summary>What the special ability is worth in the power budget.</summary>
        public float ability;

        public VehicleTraits Copy() => (VehicleTraits)MemberwiseClone();

        public VehicleTraits Scaled(float k) => new VehicleTraits
        {
            topSpeed = topSpeed * k, accel = accel * k, turning = turning * k, jump = jump * k,
            terrain = terrain * k, weather = weather * k, stamina = stamina * k, ability = ability * k,
        };

        /// <summary>The same numbers in the shape <see cref="TerrainRules"/> understands (terrain = sand resist, weather = heat resist).</summary>
        public MountTraits AsMount() => new MountTraits
        {
            topSpeed = topSpeed, accel = accel, turning = turning, jump = jump, sandResist = terrain, heatResist = weather, ability = ability,
        };
    }

    [Serializable]
    public class VehicleAbility
    {
        /// <summary>See <see cref="VehicleFile.AbilityKinds"/>. The amount scales with the stats in ranked races; the kind never changes.</summary>
        public string kind = "none";
        public float amount;
    }

    /// <summary>What happens after a crash. 7+ friendly: a dizzy spin with stars, a quick respawn on the track, no injury, no blood.</summary>
    [Serializable]
    public class CrashRules
    {
        /// <summary>0 means this class never crashes.</summary>
        public float tumbleSeconds, respawnSeconds, invulnerableSeconds;
        /// <summary>Share of the speed the rider comes back with.</summary>
        public float speedAfter = 0.5f;
        /// <summary>Must stay true: the tumble is a cartoon spin, nothing more.</summary>
        public bool kidSafe = true;
    }

    [Serializable]
    public class VehicleDef
    {
        public string id, nameAr, nameEn, descAr;
        public VehicleTraits stats = new VehicleTraits();
        public VehicleAbility ability = new VehicleAbility();
        /// <summary>Direct price tier in US dollars and the store product id. 0 and empty for the free starter.</summary>
        public float usd;
        public string productId = "";
        public bool isDefault;
        /// <summary>Motorcycles: the id of the engine sound in Resources/Audio/engine_audio.json.</summary>
        public string engineId = "";
        public string color = "#888888", accent = "#FFFFFF";
    }

    [Serializable]
    public class VehicleClassDef
    {
        public string id, tab, slot;
        /// <summary>Ranked races give every vehicle of this class the same overall power.</summary>
        public float powerBudget = 8f;
        public float rawTolerance = 0.04f;
        /// <summary>Bicycles and motorcycles: the rider always wears a helmet, whoever it is and whatever else is equipped.</summary>
        public bool helmetAlways;
        /// <summary>Noura's riding outfit ("abaya" on two wheels), or empty for her usual modest outfit.</summary>
        public string nouraOutfit = "";
        /// <summary>Bicycles: pedal-rhythm stamina instead of an engine.</summary>
        public bool pedalStamina;
        public CrashRules crash = new CrashRules();
        public VehicleDef[] vehicles = Array.Empty<VehicleDef>();
    }

    [Serializable]
    public class VehicleFile
    {
        public int version = 1;
        public VehicleClassDef[] classes = Array.Empty<VehicleClassDef>();

        public const float WTop = 3f, WAccel = 1.5f, WTurn = 1.5f, WJump = 1.5f, WResist = 1f, WStamina = 1f, WAbility = 1f;
        /// <summary>Kinds of special ability, shared across classes: none, steady (free starter), tailwind and planing and wave_hop (sea),
        /// slipstream, cruise, bounce, low_gear (cars and motorcycles), pedal_rhythm (bicycles), bump_absorb, all_terrain (motorcycles).</summary>
        public static readonly string[] AbilityKinds =
            { "none", "steady", "tailwind", "planing", "wave_hop", "slipstream", "cruise", "bounce", "low_gear", "pedal_rhythm", "bump_absorb", "all_terrain" };

        /// <summary>The shop and track classes, in tab order. "mounts" lives in race_mounts.json; "gear" is what a track gives you (skis, a surfboard).</summary>
        public static readonly string[] ClassIds = { "mounts", "sea", "cars", "bicycles", "motorcycles", "gear" };

        /// <summary>The vehicle class a kind of race is run with.</summary>
        public static string ClassForKind(string kind)
        {
            switch (kind)
            {
                case "boat": case "sail": return "sea";
                case "car": return "cars";
                case "bicycle": return "bicycles";
                case "motorcycle": return "motorcycles";
                case "ski": case "surf": return "gear";
                default: return "mounts"; // camel, horse, ostrich, bull, sky
            }
        }

        public VehicleClassDef Class(string id)
        {
            foreach (var c in classes) if (c.id == id) return c;
            return null;
        }

        public VehicleDef Find(string vehicleId)
        {
            foreach (var c in classes) foreach (var v in c.vehicles) if (v.id == vehicleId) return v;
            return null;
        }

        public VehicleClassDef ClassOf(string vehicleId)
        {
            foreach (var c in classes) foreach (var v in c.vehicles) if (v.id == vehicleId) return c;
            return null;
        }

        public VehicleDef DefaultFor(string classId)
        {
            var c = Class(classId);
            if (c == null) return null;
            foreach (var v in c.vehicles) if (v.isDefault) return v;
            return c.vehicles.Length > 0 ? c.vehicles[0] : null;
        }

        public static float Power(VehicleTraits s) =>
            WTop * s.topSpeed + WAccel * s.accel + WTurn * s.turning + WJump * s.jump + WResist * (s.terrain + s.weather) + WStamina * s.stamina + WAbility * s.ability;

        /// <summary>The stats a racer really gets. Ranked: every stat is scaled by one factor so the power equals the CLASS budget; the shape stays.</summary>
        public VehicleTraits StatsFor(string vehicleId, RaceMode mode)
        {
            var v = Find(vehicleId); var c = ClassOf(vehicleId);
            if (v == null || c == null) return new VehicleTraits();
            if (mode != RaceMode.Ranked) return v.stats.Copy();
            float p = Power(v.stats);
            return p <= 0f ? v.stats.Copy() : v.stats.Scaled(c.powerBudget / p);
        }

        public float AbilityAmount(string vehicleId, RaceMode mode)
        {
            var v = Find(vehicleId); var c = ClassOf(vehicleId);
            if (v == null || c == null) return 0f;
            if (mode != RaceMode.Ranked) return v.ability.amount;
            float p = Power(v.stats);
            return p <= 0f ? v.ability.amount : v.ability.amount * c.powerBudget / p;
        }

        /// <summary>"helmet" whenever the class demands it, whoever rides and whatever else is equipped. Otherwise empty (the hero's usual headgear).</summary>
        public static string Headgear(VehicleClassDef c) => c != null && c.helmetAlways ? "helmet" : "";

        /// <summary>The outfit Noura wears on this class ("abaya" on bicycles and motorcycles), or null for her usual one. Aron is unaffected.</summary>
        public static string OutfitFor(VehicleClassDef c, string hero) =>
            c != null && hero == "noura" && !string.IsNullOrEmpty(c.nouraOutfit) ? c.nouraOutfit : null;

        /// <param name="engineIds">The bike ids in engine_audio.json; every motorcycle must point at one.</param>
        public List<string> Validate(ICollection<string> engineIds = null)
        {
            var e = new List<string>();
            if (classes == null || classes.Length != 4) { e.Add("four classes: sea, cars, bicycles, motorcycles"); return e; }
            var expect = new[] { "sea", "cars", "bicycles", "motorcycles" };
            var ids = new HashSet<string>(); var products = new HashSet<string>();
            for (int ci = 0; ci < classes.Length; ci++)
            {
                var c = classes[ci];
                string cw = "class " + c.id;
                if (c.id != expect[ci]) e.Add(cw + ": expected " + expect[ci]);
                if (c.tab != c.id) e.Add(cw + ": tab must be the class id");
                if (Array.IndexOf(StoreTabs.VehicleSlots, c.slot ?? "") < 0 || c.slot == "mount") e.Add(cw + ": slot");
                if (c.powerBudget < 6f || c.powerBudget > 10f) e.Add(cw + ": budget 6..10");
                if (c.rawTolerance <= 0f || c.rawTolerance > 0.05f) e.Add(cw + ": tolerance up to 5%");
                bool twoWheels = c.id == "bicycles" || c.id == "motorcycles";
                if (c.helmetAlways != twoWheels) e.Add(cw + ": helmets are always on, on two wheels only");
                if (twoWheels && c.nouraOutfit != "abaya") e.Add(cw + ": Noura rides in her abaya");
                if (c.pedalStamina != (c.id == "bicycles")) e.Add(cw + ": pedal stamina is for bicycles");
                bool crashes = c.id == "motorcycles";
                if (crashes != (c.crash.tumbleSeconds > 0f)) e.Add(cw + ": only motorcycles tumble");
                if (c.crash.tumbleSeconds > 0f)
                {
                    if (!c.crash.kidSafe) e.Add(cw + ": the tumble must be kid-safe");
                    if (c.crash.tumbleSeconds > 2f || c.crash.respawnSeconds <= 0f || c.crash.respawnSeconds > 2f || c.crash.invulnerableSeconds < 1f) e.Add(cw + ": tumble 0..2 s, respawn 0..2 s, at least 1 s of protection");
                    if (c.crash.speedAfter < 0.3f || c.crash.speedAfter > 0.8f) e.Add(cw + ": speed after a crash 0.3..0.8");
                }
                if (c.vehicles.Length < 4 || c.vehicles.Length > 5) e.Add(cw + ": four or five vehicles");
                int defaults = 0;
                foreach (var v in c.vehicles)
                {
                    string w = c.id + "/" + v.id;
                    if (string.IsNullOrEmpty(v.id) || !ids.Add(v.id)) e.Add(w + ": missing or duplicate id");
                    else if (v.id.Split('_')[0] != SlotPrefix(c.slot)) e.Add(w + ": id starts with " + SlotPrefix(c.slot) + "_");
                    if (string.IsNullOrEmpty(v.nameAr) || string.IsNullOrEmpty(v.nameEn) || string.IsNullOrEmpty(v.descAr)) e.Add(w + ": names and trade-off text");
                    if (Array.IndexOf(AbilityKinds, v.ability.kind) < 0) e.Add(w + ": unknown ability " + v.ability.kind);
                    var s = v.stats;
                    if (s.topSpeed < 0.8f || s.topSpeed > 1.3f) e.Add(w + ": top speed 0.8..1.3");
                    if (s.accel < 0.6f || s.accel > 1.3f) e.Add(w + ": accel 0.6..1.3");
                    if (s.turning < 0.5f || s.turning > 1.3f) e.Add(w + ": turning 0.5..1.3");
                    if (s.jump < 0.6f || s.jump > 1.6f) e.Add(w + ": jump 0.6..1.6");
                    if (s.terrain < 0f || s.weather < 0f || s.terrain > 1.5f || s.weather > 1.5f) e.Add(w + ": resistances 0..1.5");
                    if (s.stamina < 0f || s.stamina > 1.5f || (c.pedalStamina != (s.stamina > 0f))) e.Add(w + ": stamina is for bicycles, 0..1.5");
                    if (Math.Abs(Power(s) - c.powerBudget) > c.powerBudget * c.rawTolerance) e.Add(w + ": raw power " + Power(s).ToString("0.00") + " is too far from the class budget");
                    if (v.isDefault)
                    {
                        defaults++;
                        if (v.usd != 0f || !string.IsNullOrEmpty(v.productId)) e.Add(w + ": the starter is free");
                    }
                    else
                    {
                        if (v.usd <= 0f || v.usd > 9.99f) e.Add(w + ": needs a direct price");
                        if (string.IsNullOrEmpty(v.productId) || !products.Add(v.productId)) e.Add(w + ": product id");
                    }
                    if (c.id == "motorcycles")
                    {
                        if (string.IsNullOrEmpty(v.engineId)) e.Add(w + ": needs an engine sound id");
                        else if (engineIds != null && !engineIds.Contains(v.engineId)) e.Add(w + ": engine sound " + v.engineId + " is not in engine_audio.json");
                    }
                    else if (!string.IsNullOrEmpty(v.engineId)) e.Add(w + ": only motorcycles link an engine sound here");
                    if (c.id == "bicycles" && v.ability.kind != "pedal_rhythm") e.Add(w + ": bicycles use the pedal rhythm");
                    if (v.ability.kind != "none" && v.ability.kind != "steady" && v.ability.amount <= 0f) e.Add(w + ": ability needs an amount");
                }
                if (defaults != 1) e.Add(cw + ": exactly one free default");
            }
            return e;
        }

        private static string SlotPrefix(string slot) => slot == "motorcycle" ? "moto" : slot == "bicycle" ? "bike" : slot == "boat" ? "boat" : slot;
    }

    /// <summary>
    /// Bicycle stamina. Sprinting is faster but drains the rider; tapping the pedals on the beat (every half second, a little
    /// give either side) gives a speed pulse and a breather. Out of breath means slower until the rider has recovered.
    /// </summary>
    public class PedalStamina
    {
        public const float SprintBoost = 1.10f, WindedSlow = 0.80f, SprintDrain = 0.20f, Recover = 0.15f, RecoverTo = 0.30f;
        public const float Beat = 0.5f, BeatWindow = 0.12f, PulseSeconds = 1f, TapRecovery = 0.05f;

        public float Stamina { get; private set; } = 1f;
        public bool Winded { get; private set; }
        private readonly float endurance, pulse;
        private float pulseLeft, lastTap = -10f;

        /// <param name="s">The bike's stats (stamina is its endurance). <paramref name="a"/>'s amount is the pulse speed share.</param>
        public PedalStamina(VehicleTraits s, VehicleAbility a)
        {
            endurance = Math.Max(0.3f, s == null ? 1f : s.stamina);
            pulse = a == null ? 0f : a.amount;
        }

        public float SpeedFactor(bool sprinting) =>
            (Winded ? WindedSlow : sprinting && Stamina > 0f ? SprintBoost : 1f) * (pulseLeft > 0f ? 1f + pulse : 1f);

        public void Tick(float dt, bool sprinting)
        {
            pulseLeft = Math.Max(0f, pulseLeft - dt);
            if (sprinting && !Winded)
            {
                Stamina = Math.Max(0f, Stamina - SprintDrain / endurance * dt);
                if (Stamina <= 0f) Winded = true;
            }
            else
            {
                Stamina = Math.Min(1f, Stamina + Recover * dt);
                if (Winded && Stamina >= RecoverTo) Winded = false;
            }
        }

        /// <summary>A pedal tap at time <paramref name="now"/> (seconds). True when it landed on the beat.</summary>
        public bool Tap(float now)
        {
            bool good = Math.Abs(now - lastTap - Beat) <= BeatWindow;
            lastTap = now;
            if (!good) return false;
            pulseLeft = PulseSeconds;
            Stamina = Math.Min(1f, Stamina + TapRecovery);
            return true;
        }
    }

    public enum CrashPhase { Riding, Tumbling, Respawning }

    /// <summary>
    /// The 7+ friendly motorcycle crash: a cartoon spin, a short respawn back on the track, then a few protected seconds.
    /// No injury, no blood, nothing is lost but time.
    /// </summary>
    public class CrashState
    {
        public CrashPhase Phase { get; private set; } = CrashPhase.Riding;
        public bool Protected => Phase == CrashPhase.Riding && protectedLeft > 0f;
        private readonly CrashRules rules;
        private float left, protectedLeft;

        public CrashState(CrashRules rules) { this.rules = rules ?? new CrashRules(); }

        /// <summary>A crash. False when this class never crashes, or the rider is already down or protected.</summary>
        public bool Crash()
        {
            if (rules.tumbleSeconds <= 0f || Phase != CrashPhase.Riding || protectedLeft > 0f) return false;
            Phase = CrashPhase.Tumbling; left = rules.tumbleSeconds;
            return true;
        }

        public void Tick(float dt)
        {
            if (Phase == CrashPhase.Riding) { protectedLeft = Math.Max(0f, protectedLeft - dt); return; }
            left -= dt;
            if (left > 0f) return;
            if (Phase == CrashPhase.Tumbling) { Phase = CrashPhase.Respawning; left = rules.respawnSeconds; }
            else { Phase = CrashPhase.Riding; protectedLeft = rules.invulnerableSeconds; }
        }

        /// <summary>Speed multiplier: nothing while down, then the share kept after the crash during the protected seconds.</summary>
        public float SpeedFactor => Phase != CrashPhase.Riding ? 0f : protectedLeft > 0f ? rules.speedAfter : 1f;
    }
}
