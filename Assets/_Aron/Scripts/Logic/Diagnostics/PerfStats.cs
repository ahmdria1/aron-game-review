// (c) Ahmed Alamrani. All rights reserved.
using System;
using System.Collections.Generic;

namespace Aron.Logic.Diagnostics
{
    /// <summary>
    /// Frame-time maths for the hidden performance overlay. Frames go into a fixed ring buffer, so a long session never allocates.
    /// "Average FPS" is frames divided by time (not the mean of per-frame FPS, which overstates smooth runs);
    /// "1% low" is the frame rate of the slowest 1% of frames (the mean of their frame times, inverted).
    /// </summary>
    public class FrameStats
    {
        private readonly float[] ms;
        private int next, count;
        public int Capacity => ms.Length;
        public int Count => count;
        public float LastMs { get; private set; }

        public FrameStats(int capacity = 600)
        {
            if (capacity < 2) throw new ArgumentException("capacity");
            ms = new float[capacity];
        }

        public void Add(float frameSeconds)
        {
            if (!(frameSeconds > 0f) || float.IsInfinity(frameSeconds)) return;
            float m = frameSeconds * 1000f;
            LastMs = m;
            ms[next] = m;
            next = (next + 1) % ms.Length;
            if (count < ms.Length) count++;
        }

        public void Clear() { next = 0; count = 0; LastMs = 0f; }

        public float CurrentFps => LastMs > 0f ? 1000f / LastMs : 0f;

        public float AverageMs
        {
            get
            {
                if (count == 0) return 0f;
                double sum = 0;
                for (int i = 0; i < count; i++) sum += ms[i];
                return (float)(sum / count);
            }
        }

        public float AverageFps { get { float a = AverageMs; return a > 0f ? 1000f / a : 0f; } }

        /// <summary>Frame rate of the slowest 1% of frames (at least one frame).</summary>
        public float OnePercentLowFps
        {
            get
            {
                if (count == 0) return 0f;
                var copy = new float[count];
                Array.Copy(ms, copy, count);
                Array.Sort(copy);
                int n = Math.Max(1, (int)Math.Ceiling(count * 0.01));
                double sum = 0;
                for (int i = count - n; i < count; i++) sum += copy[i];
                float worst = (float)(sum / n);
                return worst > 0f ? 1000f / worst : 0f;
            }
        }

        public float WorstMs
        {
            get { float w = 0f; for (int i = 0; i < count; i++) if (ms[i] > w) w = ms[i]; return w; }
        }
    }

    /// <summary>Opens the overlay after five quick taps on the menu's version label. A pause longer than the window starts over.</summary>
    public class TapCounter
    {
        public const int Needed = 5;
        public const float WindowSeconds = 2f;
        private int taps;
        private float last = float.NegativeInfinity;

        /// <returns>True on the tap that completes the sequence.</returns>
        public bool Tap(float now)
        {
            if (now - last > WindowSeconds) taps = 0;
            last = now;
            taps++;
            if (taps < Needed) return false;
            taps = 0;
            return true;
        }
    }

    /// <summary>What the 60 second benchmark leaves behind: numbers for a screenshot, never uploaded.</summary>
    public class BenchmarkSummary
    {
        public string device = "", tier = "", stage = "";
        public int targetFps;
        public float seconds, averageFps, onePercentLowFps, worstMs, averageMs;
        public int frames;
        public int managedMB, totalMB;
        public bool rain;
        /// <summary>Share of frames that took longer than the target frame time by 20% or more.</summary>
        public float slowFrameShare;

        public string Verdict
        {
            get
            {
                if (targetFps <= 0 || frames == 0) return "none";
                if (averageFps >= targetFps * 0.95f && onePercentLowFps >= targetFps * 0.75f) return "smooth";
                if (averageFps >= targetFps * 0.8f) return "ok";
                return "slow";
            }
        }
    }

    /// <summary>Runs a fixed-length benchmark off the frame times it is fed. No Unity types, so it is testable.</summary>
    public class Benchmark
    {
        public const float Duration = 60f;
        private readonly FrameStats stats = new FrameStats(Benchmarks.Frames);
        private int slow;
        private readonly float slowLimitMs;
        public float Elapsed { get; private set; }
        public bool Running { get; private set; }
        public bool Done { get; private set; }
        public int TargetFps { get; }
        public float Progress => Math.Min(1f, Elapsed / Duration);

        public Benchmark(int targetFps)
        {
            TargetFps = Math.Max(1, targetFps);
            slowLimitMs = 1000f / TargetFps * 1.2f;
        }

        public void Start() { stats.Clear(); Elapsed = 0f; slow = 0; Running = true; Done = false; }

        /// <summary>Feed one frame. Returns true when the run has just finished.</summary>
        public bool Frame(float dt)
        {
            if (!Running) return false;
            stats.Add(dt);
            Elapsed += dt;
            if (dt * 1000f > slowLimitMs) slow++;
            if (Elapsed < Duration) return false;
            Running = false; Done = true;
            return true;
        }

        public BenchmarkSummary Summary(string device, string tier, string stage, int managedMB, int totalMB, bool rain)
        {
            return new BenchmarkSummary
            {
                device = device, tier = tier, stage = stage, targetFps = TargetFps, seconds = Elapsed,
                averageFps = stats.AverageFps, onePercentLowFps = stats.OnePercentLowFps, worstMs = stats.WorstMs,
                averageMs = stats.AverageMs, frames = stats.Count, managedMB = managedMB, totalMB = totalMB, rain = rain,
                slowFrameShare = stats.Count > 0 ? (float)slow / Math.Max(1, stats.Count) : 0f,
            };
        }
    }

    internal static class Benchmarks { internal const int Frames = 60 * 75; }

    /// <summary>The camera path for the benchmark: a slow fly-by along the stage, always the same so runs compare.</summary>
    public static class FlyThrough
    {
        /// <summary>Position along the stage (0..1 of its length) and a gentle sway, from the seconds into the run.</summary>
        public static void At(float t, float stageLength, out float x, out float height, out float yawDegrees)
        {
            float u = Math.Max(0f, Math.Min(1f, t / Benchmark.Duration));
            x = u * stageLength;
            height = 4f + 1.5f * (float)Math.Sin(u * Math.PI * 4);
            yawDegrees = 12f * (float)Math.Sin(u * Math.PI * 6);
        }
    }

    /// <summary>
    /// Which events the diagnostics never send. The overlay and benchmark are developer tools: off by default,
    /// invisible to players, and nothing from them goes to analytics in a release build.
    /// </summary>
    public static class DiagnosticsPolicy
    {
        public static readonly string[] NeverSent = { "perf_overlay_open", "perf_benchmark_start", "perf_benchmark_done", "perf_sample" };

        /// <summary>False for every diagnostics event, in every build.</summary>
        public static bool ShouldSend(string eventName)
        {
            foreach (var n in NeverSent) if (n == eventName) return false;
            return true;
        }
    }

    public static class PerfFormat
    {
        /// <summary>"58.3" style, one decimal, invariant.</summary>
        public static string F1(float v) => v.ToString("0.0", System.Globalization.CultureInfo.InvariantCulture);
        public static string Summary(BenchmarkSummary s) =>
            $"{s.device} | {s.tier} | {s.stage}\n" +
            $"avg {F1(s.averageFps)} fps | 1% low {F1(s.onePercentLowFps)} fps | worst {F1(s.worstMs)} ms\n" +
            $"target {s.targetFps} | frames {s.frames} | {F1(s.seconds)} s | mem {s.managedMB}/{s.totalMB} MB";
    }
}
