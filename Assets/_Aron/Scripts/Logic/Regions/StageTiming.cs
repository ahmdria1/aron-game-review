// (c) Ahmed Alamrani. All rights reserved.
using System;
using System.Collections.Generic;

namespace Aron.Logic.Regions
{
    [Serializable]
    public class TypeTiming
    {
        public string id;
        public float minMinutes, maxMinutes;
    }

    /// <summary>
    /// How long a stage lasts. Every stage plays for 5 to 8 minutes (a firm product requirement); inside that, each stage type has its
    /// own range, and difficulty moves a stage from the short end to the long end. A boss stage counts the walk to the arena AND the
    /// fight. Stars: finish, find the hidden collectible (all the golden boxes), beat par. Read from Resources/Config/stage_timing.json.
    /// </summary>
    [Serializable]
    public class StageTimingFile
    {
        public float hardMinMinutes = 5f, hardMaxMinutes = 8f;
        /// <summary>Spacing of lanterns (checkpoints) in seconds, before the difficulty level's own value.</summary>
        public float checkpointSeconds = 90f;
        /// <summary>Par is this share of the layout's estimated time (see <see cref="StageTiming.ParSeconds"/>).</summary>
        public float parFactor = 0.9f;
        /// <summary>Seconds the boss fight itself lasts at difficulty 1 and 10 (the walk to the arena is the rest).</summary>
        public float bossFightMin = 150f, bossFightMax = 220f;
        /// <summary>The layout stops a section after this much, so the generator aims this many seconds short of the target.</summary>
        public float overshootAllowance = 10f;
        public TypeTiming[] types =
        {
            new TypeTiming { id = "normal", minMinutes = 6f, maxMinutes = 8f },
            new TypeTiming { id = "chase", minMinutes = 5f, maxMinutes = 6f },
            new TypeTiming { id = "action", minMinutes = 5f, maxMinutes = 7f },
            new TypeTiming { id = "race", minMinutes = 5f, maxMinutes = 6f },
            new TypeTiming { id = "boss", minMinutes = 6f, maxMinutes = 8f },
        };

        public TypeTiming For(string type)
        {
            foreach (var t in types) if (t.id == type) return t;
            return types[0];
        }

        public List<string> Validate()
        {
            var e = new List<string>();
            if (hardMinMinutes < 5f || hardMaxMinutes > 8f) e.Add("the hard limits must stay 5..8 minutes");
            foreach (var t in types)
            {
                if (t.minMinutes < hardMinMinutes || t.maxMinutes > hardMaxMinutes) e.Add(t.id + ": range outside the 5..8 minute limits");
                if (t.minMinutes > t.maxMinutes) e.Add(t.id + ": min above max");
            }
            foreach (var need in new[] { "normal", "chase", "action", "race", "boss" }) if (Array.FindIndex(types, t => t.id == need) < 0) e.Add("missing type " + need);
            if (checkpointSeconds < 60f || checkpointSeconds > 120f) e.Add("checkpoints should come about every 90 s (60..120)");
            if (parFactor < 0.7f || parFactor > 1f) e.Add("par factor 0.7..1");
            if (bossFightMin < 90f || bossFightMax < bossFightMin || bossFightMax > 300f) e.Add("boss fight seconds");
            return e;
        }
    }

    public static class StageTiming
    {
        private static StageTimingFile active = new StageTimingFile();

        /// <summary>The loaded table (the runtime sets it from Resources/Config/stage_timing.json; defaults match the file).</summary>
        public static StageTimingFile Active { get => active; set => active = value ?? new StageTimingFile(); }

        /// <summary>Stages aim this far (minutes) inside their range, so a layout that lands a few seconds off stays inside it.</summary>
        public const float Margin = 0.4f;

        private static float D01(int difficulty) => (Math.Max(1, Math.Min(10, difficulty)) - 1f) / 9f;
        private static float Lerp(float a, float b, float t) => a + (b - a) * t;

        /// <summary>Minutes this stage is meant to last: its own targetMinutes if set, else by type and difficulty.</summary>
        public static float Minutes(StageDef s)
        {
            float own = s.targetMinutes;
            var tt = active.For(s.boss ? "boss" : s.type);
            float lo = tt.minMinutes + Margin, hi = tt.maxMinutes - Margin;
            if (own > 0f) return Math.Max(lo, Math.Min(hi, own));
            return Lerp(lo, hi, D01(s.difficulty));
        }

        public static float BossFightSeconds(int difficulty) => Lerp(active.bossFightMin, active.bossFightMax, D01(difficulty));

        /// <summary>What the layout generator should aim for: the whole target on normal stages, the walk-in on boss stages.</summary>
        public static float LayoutTargetSeconds(StageDef s)
        {
            float total = Minutes(s) * 60f;
            float aim = total - active.overshootAllowance;
            if (s.boss) aim -= BossFightSeconds(s.difficulty);
            return Math.Max(60f, aim);
        }

        /// <summary>Whole play time of a built stage, including a boss fight.</summary>
        public static float TotalSeconds(StageDef s, StageLayout layout) => layout.EstimatedSeconds + (s.boss ? BossFightSeconds(s.difficulty) : 0f);

        /// <summary>Beat this time for the third star: the stage's parSeconds, or the table's share of the estimate, in 5 s steps.</summary>
        public static float ParSeconds(StageDef s, StageLayout layout)
        {
            if (s.parSeconds > 0f) return s.parSeconds;
            return (float)Math.Round(TotalSeconds(s, layout) * active.parFactor / 5f) * 5f;
        }

        /// <summary>Checks one stage: 5..8 minutes in total and inside its type's range (a small slack for the section that ends the stage).</summary>
        public static List<string> Problems(StageDef s, StageLayout layout)
        {
            var e = new List<string>();
            float min = TotalSeconds(s, layout) / 60f;
            if (min < active.hardMinMinutes || min > active.hardMaxMinutes) e.Add($"{s.id}: {min:0.0} min is outside {active.hardMinMinutes}..{active.hardMaxMinutes}");
            if (s.targetMinutes > 0f && (s.targetMinutes < active.hardMinMinutes || s.targetMinutes > active.hardMaxMinutes)) e.Add(s.id + ": targetMinutes outside 5..8");
            float par = ParSeconds(s, layout);
            if (par <= 0f || par >= TotalSeconds(s, layout)) e.Add(s.id + ": par must be under the stage time");
            return e;
        }
    }

    /// <summary>One to three stars per stage: finish, find the hidden collectible, beat par.</summary>
    public static class StageStars
    {
        public const int Max = 3;

        public static int Count(StageRecord r) => r == null || !r.done ? 0 : 1 + (r.starHidden ? 1 : 0) + (r.starPar ? 1 : 0);

        /// <summary>The hidden collectible of a stage is its set of golden boxes: all of them found in one run.</summary>
        public static bool FoundHidden(int bisht, int bishtTotal) => bishtTotal > 0 && bisht >= bishtTotal;

        public static bool BeatPar(float seconds, float par) => seconds > 0f && par > 0f && seconds <= par;

        /// <summary>Records a finished run's stars (never taken away). Returns the stars newly earned this run.</summary>
        public static int Record(SaveData data, string stageId, float seconds, float par, int bisht, int bishtTotal)
        {
            var r = StageProgress.Get(data, stageId);
            if (r == null) return 0;
            int before = Count(r);
            if (FoundHidden(bisht, bishtTotal)) r.starHidden = true;
            if (BeatPar(seconds, par)) r.starPar = true;
            return Count(r) - before;
        }

        public static int Total(SaveData data)
        {
            int n = 0;
            if (data.stages != null) foreach (var r in data.stages) n += Count(r);
            return n;
        }
    }
}
