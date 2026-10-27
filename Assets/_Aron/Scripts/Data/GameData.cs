// (c) Ahmed Alamrani. All rights reserved.
using System.Collections.Generic;
using Aron.Logic;
using Aron.Logic.Costumes;
using Aron.Logic.Heroes;
using UnityEngine;

namespace Aron.Data
{
    /// <summary>Companions, heroes and costumes read once from Resources and kept for the session.</summary>
    public static class GameData
    {
        private static CompanionFile companions;
        private static HeroFile heroes;
        private static List<CostumeDefinition> costumes;

        public static CompanionFile Companions => companions ??= Load<CompanionFile>("Companions/companions") ?? new CompanionFile();
        public static HeroFile Heroes => heroes ??= Load<HeroFile>("Heroes/heroes") ?? new HeroFile();

        /// <summary>Every costume from every country file.</summary>
        public static IReadOnlyList<CostumeDefinition> Costumes
        {
            get
            {
                if (costumes != null) return costumes;
                costumes = new List<CostumeDefinition>();
                foreach (var t in Resources.LoadAll<TextAsset>("Costumes"))
                {
                    var f = JsonUtility.FromJson<CostumeFile>(t.text);
                    if (f?.costumes == null) continue;
                    foreach (var c in f.costumes)
                    {
                        if (string.IsNullOrEmpty(c.country)) c.country = f.country;
                        costumes.Add(c);
                    }
                }
                return costumes;
            }
        }

        public static CostumeDefinition Costume(string id)
        {
            foreach (var c in Costumes) if (c.id == id) return c;
            return null;
        }

        private static T Load<T>(string path) where T : class
        {
            var t = Resources.Load<TextAsset>(path);
            if (t == null) { global::Aron.Logic.Diagnostics.Log.Warn("[Aron] missing Resources/" + path + ".json"); return null; }
            return JsonUtility.FromJson<T>(t.text);
        }
    }
}
