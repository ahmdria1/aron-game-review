// (c) Ahmed Alamrani. All rights reserved.
using System.Collections.Generic;
using System.Globalization;

namespace Aron.Logic.Performance
{
    /// <summary>
    /// Design targets for one tier at one frame rate. These are what the game is built to, not measurements:
    /// real numbers need a device (the debug benchmark reports them; see docs/perf_checklist.md).
    /// </summary>
    public readonly struct TierBudget
    {
        public readonly QualityTier tier;
        public readonly int fps;
        /// <summary>Whole-frame time at the target rate.</summary>
        public readonly float frameMs;
        /// <summary>Main-thread work per frame. Kept well below the frame so the CPU can idle and stay cool.</summary>
        public readonly float cpuMs;
        public readonly float gpuMs;
        public readonly int drawCalls;
        public readonly int trianglesK;
        public readonly int memoryMB;

        public TierBudget(QualityTier tier, int fps, float cpuMs, float gpuMs, int drawCalls, int trianglesK, int memoryMB)
        {
            this.tier = tier; this.fps = fps; frameMs = 1000f / fps; this.cpuMs = cpuMs; this.gpuMs = gpuMs;
            this.drawCalls = drawCalls; this.trianglesK = trianglesK; this.memoryMB = memoryMB;
        }

        public string FrameText => frameMs.ToString("0.0", CultureInfo.InvariantCulture) + " ms";
    }

    public struct PerfMeasured
    {
        public float averageFrameMs, onePercentLowFps;
        public int drawCalls, trianglesK, memoryMB;
    }

    public static class PerfBudgets
    {
        public static readonly TierBudget[] All =
        {
            new TierBudget(QualityTier.Low,    30, 14f, 20f, 150, 200, 700),
            new TierBudget(QualityTier.Medium, 30, 14f, 22f, 250, 400, 1100),
            new TierBudget(QualityTier.High,   30, 12f, 20f, 400, 800, 1600),
            new TierBudget(QualityTier.High,   60,  8f, 12f, 400, 800, 1600),
        };

        public static TierBudget For(QualityTier tier, int fps)
        {
            TierBudget best = All[0];
            foreach (var b in All) if (b.tier == tier && b.fps == (fps >= 60 ? 60 : 30)) return b;
            foreach (var b in All) if (b.tier == tier) best = b;
            return best;
        }

        /// <summary>Lists what is over budget (empty when everything fits). A little slack is allowed on frame time.</summary>
        public static List<string> Check(in TierBudget b, in PerfMeasured m)
        {
            var bad = new List<string>();
            if (m.averageFrameMs > b.frameMs * 1.05f) bad.Add("average frame " + m.averageFrameMs.ToString("0.0", CultureInfo.InvariantCulture) + " ms > " + b.FrameText);
            if (m.onePercentLowFps > 0 && m.onePercentLowFps < b.fps * 0.8f) bad.Add("1% low " + m.onePercentLowFps.ToString("0", CultureInfo.InvariantCulture) + " fps < " + (b.fps * 0.8f).ToString("0", CultureInfo.InvariantCulture));
            if (m.drawCalls > b.drawCalls) bad.Add("draw calls " + m.drawCalls + " > " + b.drawCalls);
            if (m.trianglesK > b.trianglesK) bad.Add("triangles " + m.trianglesK + "k > " + b.trianglesK + "k");
            if (m.memoryMB > b.memoryMB) bad.Add("memory " + m.memoryMB + " MB > " + b.memoryMB + " MB");
            return bad;
        }
    }
}
