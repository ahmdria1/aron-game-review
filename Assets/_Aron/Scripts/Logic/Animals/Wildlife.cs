// (c) Ahmed Alamrani. All rights reserved.
using System;
using System.Collections.Generic;

namespace Aron.Logic.Animals
{
    public enum HazardEffect { None, Knockback, Life }

    /// <summary>What happened when the hero met an animal.</summary>
    public struct Encounter
    {
        public HazardEffect Effect;
        /// <summary>Always false. Nothing in the game can hurt an animal.</summary>
        public bool AnimalHurt;
        /// <summary>Seconds of warning before a hazard moves, so a player can always see it coming.</summary>
        public float Telegraph;
    }

    /// <summary>
    /// The rules for wild animals. They are never attacked or hurt: not by weapons, not by the handbag, not by bosses.
    /// Dangerous ones (snakes, bears, big cats, crocodiles, rhinos, the komodo dragon) are hazards to avoid: they block a
    /// path, wander a trail, chase or charge in a way you can see coming, and touching one knocks the hero back or
    /// costs a life. No blood and nothing scary-graphic. Peaceful ones are scenery or something to play with.
    /// </summary>
    public static class Wildlife
    {
        public static readonly string[] HazardBehaviors = { "block_path", "wander_trail", "chase_scene", "water_edge", "charge_lane" };
        public static readonly string[] FriendlyBehaviors = { "ambient", "hop_along", "bounce_pad", "wave_back", "graze", "distant", "flee_gently" };
        public static readonly string[] Effects = { "knockback", "life" };

        /// <summary>Species that are dangerous and must be hazards.</summary>
        public static readonly string[] Dangerous =
        {
            "indian_cobra", "egyptian_cobra", "green_anaconda", "sand_viper", "brown_bear", "black_bear", "lion", "bengal_tiger", "jaguar",
            "crocodile", "saltwater_crocodile", "rhino", "komodo",
        };

        /// <summary>Species that are peaceful and never hazards.</summary>
        public static readonly string[] Peaceful = { "sloth", "zebra", "langur", "kangaroo", "koala", "emu", "dhub", "arabian_wolf", "oryx" };

        /// <summary>Where each hazard species really lives: the only countries it may appear in.</summary>
        public static readonly Dictionary<string, string[]> HazardHomes = new Dictionary<string, string[]>
        {
            ["indian_cobra"] = new[] { "india" }, ["egyptian_cobra"] = new[] { "egypt" }, ["green_anaconda"] = new[] { "brazil" },
            ["sand_viper"] = new[] { "saudi", "uae" }, ["brown_bear"] = new[] { "usa", "turkey" }, ["black_bear"] = new[] { "japan" },
            ["lion"] = new[] { "south_africa" }, ["rhino"] = new[] { "south_africa" }, ["bengal_tiger"] = new[] { "india" },
            ["jaguar"] = new[] { "brazil" }, ["crocodile"] = new[] { "egypt", "sudan" }, ["saltwater_crocodile"] = new[] { "australia" },
            ["komodo"] = new[] { "indonesia" },
        };

        /// <summary>The one place a hero's attack meets an animal: nothing happens to the animal.</summary>
        public static Encounter HeroHits(LocalAnimal animal) => new Encounter { Effect = HazardEffect.None, AnimalHurt = false };

        /// <summary>The hero touches an animal. Only hazards do anything, and only to the hero.</summary>
        public static Encounter HeroTouches(LocalAnimal animal)
        {
            if (animal == null || !animal.IsHazard) return new Encounter();
            return new Encounter
            {
                Effect = animal.effect == "life" ? HazardEffect.Life : HazardEffect.Knockback,
                AnimalHurt = false,
                Telegraph = animal.behavior == "charge_lane" ? 1.2f : animal.behavior == "chase_scene" ? 0.8f : 0.6f,
            };
        }

        /// <summary>Wild animals are never a target: the sling and the melee aim assist only pick enemies, bosses and breakables (see AimAssist.Targetable).</summary>
        public static bool IsTargetable(LocalAnimal animal) => false;

        /// <summary>Only a mount can be ridden. A kangaroo, a zebra or a rhino never can.</summary>
        public static bool CanRide(LocalAnimal animal) => animal != null && animal.role == "mount";

        public static bool IsHazardBehavior(string b) => Array.IndexOf(HazardBehaviors, b ?? "") >= 0;

        /// <summary>Checks one entry against the rules above. Empty list means fine.</summary>
        public static List<string> Check(LocalAnimal a)
        {
            var e = new List<string>();
            string w = "animal " + a.id;
            string b = a.behavior ?? "";
            if (a.role == "hazard")
            {
                if (!IsHazardBehavior(b)) e.Add(w + ": a hazard needs a hazard behaviour");
                if (Array.IndexOf(Effects, a.effect ?? "") < 0) e.Add(w + ": a hazard knocks the hero back or costs a life");
            }
            else
            {
                if (b != "" && Array.IndexOf(FriendlyBehaviors, b) < 0) e.Add(w + ": unknown behaviour " + b);
                if (!string.IsNullOrEmpty(a.effect)) e.Add(w + ": only hazards have an effect");
            }
            string sp = a.Species;
            if (Array.IndexOf(Dangerous, sp) >= 0 && a.role != "hazard") e.Add(w + ": dangerous animals are hazards to avoid");
            if (Array.IndexOf(Peaceful, sp) >= 0 && a.role == "hazard") e.Add(w + ": peaceful animals are never hazards");
            if (a.role == "hazard" && HazardHomes.TryGetValue(sp, out var homes) && Array.IndexOf(homes, a.country) < 0)
                e.Add(w + ": " + sp + " does not live in " + a.country);
            return e;
        }
    }
}
