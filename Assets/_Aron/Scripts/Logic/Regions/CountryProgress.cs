// (c) Ahmed Alamrani. All rights reserved.
using System.Collections.Generic;
using Aron.Logic.Culture;

namespace Aron.Logic.Regions
{
    /// <summary>What the world map shows under a country: boxes, foods and bisht boxes found out of the total.</summary>
    public class CountryProgress
    {
        public string country;
        public int stagesDone, stagesTotal;
        public int boxes, boxesTotal;
        public int bisht, bishtTotal;
        public int foods, foodsTotal;

        public static CountryProgress For(RegionCatalog catalog, SaveData data, string country, CultureFile culture)
        {
            var p = new CountryProgress { country = country };
            foreach (var r in catalog.regions)
            {
                if (ProgressRing.CountryOf(r) != country) continue;
                foreach (var s in r.stages)
                {
                    var layout = StageLayout.Generate(s);
                    p.stagesTotal++;
                    p.boxesTotal += layout.BoxCount;
                    p.bishtTotal += layout.Count(PieceKind.CrateBisht);
                    var rec = StageProgress.Get(data, s.id);
                    if (rec == null) continue;
                    if (rec.done) p.stagesDone++;
                    p.boxes += System.Math.Min(rec.bestBoxes, layout.BoxCount);
                    p.bisht += System.Math.Min(rec.bestBisht, layout.Count(PieceKind.CrateBisht));
                }
            }
            if (culture != null && culture.foods != null)
            {
                p.foodsTotal = culture.foods.Length;
                foreach (var f in culture.foods) if (FoodAlbum.Has(data, f.id)) p.foods++;
            }
            return p;
        }
    }
}
