// (c) Ahmed Alamrani. All rights reserved.
using System.Collections.Generic;

namespace Aron.Logic.Regions
{
    /// <summary>One door in a country's hub square.</summary>
    public class HubDoor
    {
        public StageDef stage;
        public bool open, done, boss;
        /// <summary>The boss gate stays sealed until every other stage of the hub is done.</summary>
        public bool sealedGate;
        public float bestTime;
        public int boxesBest, boxesTotal, bishtBest, bishtTotal;
    }

    /// <summary>
    /// The hub square of a region (a majlis, souq or safari-lodge courtyard): a door per stage that lights up when the stage is open, and the boss gate
    /// in the middle. Follows the same unlock rules as the map.
    /// </summary>
    public static class HubDoors
    {
        public static List<HubDoor> For(RegionCatalog catalog, SaveData data, int regionIndex)
        {
            var list = new List<HubDoor>();
            if (regionIndex < 0 || regionIndex >= catalog.regions.Length) return list;
            var stages = catalog.regions[regionIndex].stages;
            bool allOthersDone = true;
            foreach (var s in stages) if (!s.boss && !StageProgress.IsDone(data, s.id)) allOthersDone = false;
            foreach (var s in stages)
            {
                var rec = StageProgress.Get(data, s.id);
                var layout = StageLayout.Generate(s);
                var d = new HubDoor
                {
                    stage = s,
                    boss = s.boss,
                    done = rec != null && rec.done,
                    open = StageProgress.IsUnlocked(catalog, data, s.id) && (!s.boss || allOthersDone),
                    bestTime = rec?.bestTime ?? 0f,
                    boxesBest = rec?.bestBoxes ?? 0,
                    bishtBest = rec?.bestBisht ?? 0,
                    boxesTotal = layout.BoxCount,
                    bishtTotal = layout.Count(PieceKind.CrateBisht),
                };
                d.sealedGate = s.boss && !d.open;
                list.Add(d);
            }
            return list;
        }
    }
}
