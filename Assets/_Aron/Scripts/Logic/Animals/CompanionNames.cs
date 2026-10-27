// (c) Ahmed Alamrani. All rights reserved.
using System;
using System.Collections.Generic;

namespace Aron.Logic.Animals
{
    /// <summary>
    /// The animals that go with Aron and Noura, and the key their name is looked up under in every language
    /// (Resources/Companions/companion_names.json). The name itself is never in code or in this file: it is the UI string
    /// <see cref="NameKey"/>. Until a name is chosen the entry is <see cref="namePending"/> and the species name shows instead.
    /// </summary>
    [Serializable]
    public class CompanionDef
    {
        public string id, species, owner;
        /// <summary>True until the name is in the string tables.</summary>
        public bool namePending = true;

        public string NameKey => "companion_name_" + owner + "_" + species;
        public string SpeciesKey => "companion_species_" + species;
    }

    [Serializable]
    public class CompanionNameFile
    {
        public CompanionDef[] companions = Array.Empty<CompanionDef>();
        public static readonly string[] Owners = { "aron", "noura" };
        public static readonly string[] Species = { "falcon", "oryx", "camel", "horse" };

        public CompanionDef Find(string id) { foreach (var c in companions) if (c != null && c.id == id) return c; return null; }

        /// <summary>The string key to show for the companion: its own name when it has one in the tables, else the species name.</summary>
        public static string DisplayKey(CompanionDef c, Func<string, bool> has)
        {
            if (c == null) return "";
            return !c.namePending && has(c.NameKey) ? c.NameKey : c.SpeciesKey;
        }

        public List<string> Validate()
        {
            var e = new List<string>();
            var ids = new HashSet<string>(); var keys = new HashSet<string>();
            foreach (var c in companions)
            {
                string w = "companion " + c.id;
                if (string.IsNullOrEmpty(c.id) || !ids.Add(c.id)) e.Add(w + ": missing or duplicate id");
                if (Array.IndexOf(Owners, c.owner) < 0) e.Add(w + ": owner is aron or noura");
                if (Array.IndexOf(Species, c.species) < 0) e.Add(w + ": species");
                if (c.id != c.owner + "_" + c.species) e.Add(w + ": id is owner_species");
                if (!keys.Add(c.NameKey)) e.Add(w + ": duplicate name key");
            }
            return e;
        }
    }
}
