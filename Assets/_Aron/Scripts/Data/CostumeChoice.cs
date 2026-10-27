// (c) Ahmed Alamrani. All rights reserved.
using Aron.Logic.Costumes;
using Aron.Save;
using Aron.Stages;

namespace Aron.Data
{
    /// <summary>Which costume a hero wears right now: wardrobe pick, else the stage's, the region's, the country's.</summary>
    public static class CostumeChoice
    {
        public static CostumeDefinition For(string hero)
        {
            string country = "saudi", region = "", stage = "";
            var b = StageBuilder.Current;
            if (b != null)
            {
                if (b.Region != null)
                {
                    if (!string.IsNullOrEmpty(b.Region.country)) country = b.Region.country;
                    region = b.Region.costume;
                }
                if (b.Stage != null) stage = b.Stage.costume;
            }
            var data = SaveSystem.Data;
            return CostumeRules.Pick(GameData.Costumes, hero, country, region, stage, Wardrobe.PickOf(data, hero), data.ownedCostumes);
        }
    }
}
