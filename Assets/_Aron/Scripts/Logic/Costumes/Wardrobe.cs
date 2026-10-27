// (c) Ahmed Alamrani. All rights reserved.
using System.Collections.Generic;

namespace Aron.Logic.Costumes
{
    /// <summary>What each hero wears: the wardrobe pick saved per hero, bought with coins.</summary>
    public static class Wardrobe
    {
        public static string PickOf(SaveData data, string hero) => hero == "noura" ? data.nouraCostumeId : data.costumeId;

        public static bool Owns(SaveData data, CostumeDefinition c)
            => c != null && ((c.price <= 0 && !c.IsReward) || data.ownedCostumes.Contains(c.id));

        /// <summary>Hands over the hard-mode reward costumes once every country is beaten on hard.</summary>
        public static bool GrantHardReward(SaveData data, Regions.RegionCatalog catalog, DifficultyFile rules)
        {
            if (rules == null || !DifficultyFile.HardRewardEarned(catalog, data)) return false;
            bool any = false;
            foreach (var id in new[] { rules.hardRewardAron, rules.hardRewardNoura })
                if (!string.IsNullOrEmpty(id) && !data.ownedCostumes.Contains(id)) { data.ownedCostumes.Add(id); any = true; }
            return any;
        }

        public static bool Wearing(SaveData data, CostumeDefinition c) => c != null && PickOf(data, c.hero) == c.id;

        /// <summary>Buys if needed, then wears it. False when there aren't enough coins.</summary>
        public static bool TryWear(SaveData data, CostumeDefinition c)
        {
            if (c == null) return false;
            if (!Owns(data, c))
            {
                if (c.IsReward || data.coins < c.price) return false;
                data.coins -= c.price;
                data.ownedCostumes.Add(c.id);
            }
            Set(data, c.hero, c.id);
            return true;
        }

        /// <summary>Back to whatever the region dresses him (or her) in.</summary>
        public static void WearRegion(SaveData data, string hero) => Set(data, hero, "");

        public static List<CostumeDefinition> For(IReadOnlyList<CostumeDefinition> all, string hero)
        {
            var list = new List<CostumeDefinition>();
            foreach (var c in all) if (c.hero == hero) list.Add(c);
            list.Sort((a, b) => a.price != b.price ? a.price.CompareTo(b.price) : string.CompareOrdinal(a.id, b.id));
            return list;
        }

        private static void Set(SaveData data, string hero, string id)
        {
            if (hero == "noura") data.nouraCostumeId = id; else data.costumeId = id;
        }
    }
}
