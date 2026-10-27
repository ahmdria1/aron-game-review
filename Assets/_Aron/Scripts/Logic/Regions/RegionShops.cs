// (c) Ahmed Alamrani. All rights reserved.
using System;
using System.Collections.Generic;

namespace Aron.Logic.Regions
{
    /// <summary>Local shops along a region's streets: Resources/RegionShops/&lt;regionId&gt;.json.</summary>
    [Serializable]
    public class RegionShopFile
    {
        public string region;
        public RegionShop[] shops = Array.Empty<RegionShop>();

        public List<RegionShop> On(string stageId)
        {
            var list = new List<RegionShop>();
            foreach (var s in shops ?? Array.Empty<RegionShop>()) if (s.stage == stageId) list.Add(s);
            return list;
        }

        public List<string> Validate(RegionDef def)
        {
            var errors = new List<string>();
            if (def == null || def.id != region) { errors.Add("shops for " + region + " don't match a region"); return errors; }
            if (shops == null || shops.Length < 2) errors.Add(region + ": needs at least two shops");
            var names = new HashSet<string>();
            foreach (var s in shops ?? Array.Empty<RegionShop>())
            {
                bool found = false;
                foreach (var st in def.stages) if (st.id == s.stage) { found = !st.boss; break; }
                if (!found) errors.Add(region + ": shop " + s.nameAr + " is on a stage that isn't a street of this region");
                if (string.IsNullOrEmpty(s.nameAr) || !names.Add(s.nameAr)) errors.Add(region + ": shop name missing or used twice");
                if (string.IsNullOrEmpty(s.kind)) errors.Add(region + ": shop " + s.nameAr + " needs a kind");
            }
            return errors;
        }
    }

    [Serializable]
    public class RegionShop
    {
        public string stage;
        public string nameAr;
        /// <summary>What it sells; picks the awning colour and the goods out front.</summary>
        public string kind;
    }
}
