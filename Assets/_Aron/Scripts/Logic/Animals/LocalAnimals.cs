// (c) Ahmed Alamrani. All rights reserved.
using System;
using System.Collections.Generic;

namespace Aron.Logic.Animals
{
    /// <summary>A native animal seen in a country's stages. Resources/Companions/local_animals.json.</summary>
    [Serializable]
    public class LocalAnimal
    {
        public string id;
        public string country;
        public string nameAr, nameEn;
        /// <summary>mount, flier, ground, pointer, hazard or figure (a statue or carving, not a live animal).</summary>
        public string role = "ground";
        /// <summary>The existing rig it borrows until it has its own model: camel, horse, falcon, oryx or none.</summary>
        public string plays = "none";
        public string body = "#8A6A4A", accent = "#3A2A1A";
        public float size = 1f;
        /// <summary>How it acts in a stage, see <see cref="Wildlife.Behaviors"/>. Empty means ambient scenery.</summary>
        public string behavior = "";
        /// <summary>For hazards only: what touching one does to the hero. knockback or life. The animal is never hurt.</summary>
        public string effect = "";
        /// <summary>Region ids where it appears (empty: wherever the stage ids say, or the whole country).</summary>
        public string[] regions = Array.Empty<string>();
        /// <summary>Stage ids where it appears (empty: any stage of its country or regions).</summary>
        public string[] stages = Array.Empty<string>();

        /// <summary>The species without the country prefix, e.g. "indian_cobra". String key: "animal_" + Species.</summary>
        public string Species => !string.IsNullOrEmpty(id) && !string.IsNullOrEmpty(country) && id.Length > country.Length + 1 ? id.Substring(country.Length + 1) : id;
        public bool IsHazard => role == "hazard";
    }

    [Serializable]
    public class LocalAnimalFile
    {
        public LocalAnimal[] animals = Array.Empty<LocalAnimal>();

        public static readonly string[] Roles = { "mount", "flier", "ground", "pointer", "hazard", "figure" };
        public static readonly string[] Rigs = { "camel", "horse", "falcon", "oryx", "none" };

        public List<string> Validate()
        {
            var e = new List<string>();
            var ids = new HashSet<string>();
            foreach (var a in animals)
            {
                string w = "animal " + a.id;
                if (string.IsNullOrEmpty(a.id) || !ids.Add(a.id)) e.Add(w + ": missing or duplicate id");
                if (string.IsNullOrEmpty(a.country) || a.id == null || !a.id.StartsWith(a.country + "_", StringComparison.Ordinal)) e.Add(w + ": id starts with the country");
                if (string.IsNullOrEmpty(a.nameAr) || string.IsNullOrEmpty(a.nameEn)) e.Add(w + ": names");
                if (Array.IndexOf(Roles, a.role) < 0) e.Add(w + ": unknown role " + a.role);
                if (Array.IndexOf(Rigs, a.plays) < 0) e.Add(w + ": unknown rig " + a.plays);
                if (a.role == "mount" && a.plays != "camel" && a.plays != "horse") e.Add(w + ": a mount rides on the camel or horse rig");
                if (a.role == "flier" && a.plays != "falcon") e.Add(w + ": fliers use the falcon rig");
                if (!Culture.CultureRules.Hex(a.body) || !Culture.CultureRules.Hex(a.accent)) e.Add(w + ": colours");
                if (a.size <= 0.1f || a.size > 3f) e.Add(w + ": size 0.1..3");
                e.AddRange(Wildlife.Check(a));
            }
            return e;
        }

        public List<LocalAnimal> Of(string country)
        {
            var list = new List<LocalAnimal>();
            foreach (var a in animals) if (a.country == country) list.Add(a);
            return list;
        }

        public LocalAnimal Find(string id)
        {
            foreach (var a in animals) if (a.id == id) return a;
            return null;
        }
    }
}
