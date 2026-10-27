// (c) Ahmed Alamrani. All rights reserved.
using System;
using System.Collections.Generic;

namespace Aron.Logic.Regions
{
    [Serializable]
    public class StageRecord
    {
        public string id;
        public bool done;
        /// <summary>Seconds; 0 means no finished run yet.</summary>
        public float bestTime;
        /// <summary>Most dates collected in one run.</summary>
        public int bestDates;
        /// <summary>Most boxes broken in one run.</summary>
        public int bestBoxes;
        /// <summary>Most bisht boxes found in one run.</summary>
        public int bestBisht;
        /// <summary>Finished at least once on hard.</summary>
        public bool doneHard;
        /// <summary>Star 2: every golden box found in one run. Star 3: finished under par. Star 1 is finishing (done).</summary>
        public bool starHidden, starPar;
    }

    /// <summary>What's unlocked and the best runs, kept in the save file.</summary>
    public static class StageProgress
    {
        public static StageRecord Get(SaveData data, string stageId)
        {
            if (data.stages == null) data.stages = new List<StageRecord>();
            foreach (var r in data.stages) if (r.id == stageId) return r;
            return null;
        }

        public static bool IsDone(SaveData data, string stageId) => Get(data, stageId)?.done == true;

        /// <summary>
        /// The first stage of the player's home country is always open (see <see cref="ProgressRing"/>). After that a
        /// stage opens when the one before it is done, and a region opens when the boss of the region before it in
        /// the player's ring is beaten.
        /// </summary>
        public static bool IsUnlocked(RegionCatalog catalog, SaveData data, string stageId)
        {
            if (catalog.Stage(stageId, out int r, out int s) == null) return false;
            if (!IsRegionUnlocked(catalog, data, r)) return false;
            return s == 0 || IsDone(data, catalog.regions[r].stages[s - 1].id);
        }

        /// <summary>
        /// A region is open when it starts the player's ring, when the region before it in the ring has had its boss
        /// beaten (open for good), or while a travel ticket pass covers it.
        /// </summary>
        public static bool IsRegionUnlocked(RegionCatalog catalog, SaveData data, int regionIndex)
        {
            if (regionIndex < 0 || regionIndex >= catalog.regions.Length) return false;
            return IsRegionReached(catalog, data, regionIndex) || TravelTickets.OpensRegion(catalog, data, regionIndex);
        }

        /// <summary>Open through the normal ring, which never closes again.</summary>
        public static bool IsRegionReached(RegionCatalog catalog, SaveData data, int regionIndex)
        {
            var order = ProgressRing.RegionOrder(catalog, data.homeCountry);
            int pos = order.IndexOf(regionIndex);
            if (pos < 0) return false;
            if (pos == 0) return true;
            var prev = catalog.regions[order[pos - 1]].stages;
            return IsDone(data, prev[prev.Length - 1].id);
        }

        public static bool IsRegionDone(RegionCatalog catalog, SaveData data, int regionIndex)
        {
            var st = catalog.regions[regionIndex].stages;
            return IsDone(data, st[st.Length - 1].id);
        }

        /// <summary>Records a finished run. Returns true if it beat the best time.</summary>
        public static bool Finish(SaveData data, string stageId, float seconds, int dates) => Finish(data, stageId, seconds, dates, -1, -1);

        /// <summary>Records a finished run with how many boxes and bisht boxes were broken (-1 when unknown).</summary>
        public static bool Finish(SaveData data, string stageId, float seconds, int dates, int boxes, int bisht)
        {
            var r = Get(data, stageId);
            if (r == null) { r = new StageRecord { id = stageId }; data.stages.Add(r); }
            r.done = true;
            if (dates > r.bestDates) r.bestDates = dates;
            if (boxes > r.bestBoxes) r.bestBoxes = boxes;
            if (bisht > r.bestBisht) r.bestBisht = bisht;
            if (data.difficulty == "hard") r.doneHard = true;
            bool best = seconds > 0f && (r.bestTime <= 0f || seconds < r.bestTime);
            if (best) r.bestTime = seconds;
            return best;
        }

        /// <summary>The furthest open stage: where "continue" should take the player.</summary>
        public static StageDef Frontier(RegionCatalog catalog, SaveData data)
        {
            StageDef last = null;
            foreach (int ri in ProgressRing.RegionOrder(catalog, data.homeCountry))
                foreach (var st in catalog.regions[ri].stages)
                {
                    if (!IsUnlocked(catalog, data, st.id)) return last;
                    last = st;
                    if (!IsDone(data, st.id)) return st;
                }
            return last;
        }

        public static string FormatTime(float seconds)
        {
            if (seconds <= 0f) return "--:--";
            int s = (int)Math.Round(seconds);
            return $"{s / 60}:{s % 60:00}";
        }
    }
}
