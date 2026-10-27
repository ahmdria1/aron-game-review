// (c) Ahmed Alamrani. All rights reserved.
using Aron.Logic.Diagnostics;
using NUnit.Framework;

namespace Aron.Logic.Tests
{
    public class PerfStatsTests
    {
        [Test]
        public void SteadySixtyFpsReadsSixty()
        {
            var s = new FrameStats(100);
            for (int i = 0; i < 100; i++) s.Add(1f / 60f);
            Assert.That(s.AverageFps, Is.EqualTo(60f).Within(0.01f));
            Assert.That(s.CurrentFps, Is.EqualTo(60f).Within(0.01f));
            Assert.That(s.OnePercentLowFps, Is.EqualTo(60f).Within(0.01f));
            Assert.That(s.AverageMs, Is.EqualTo(16.667f).Within(0.01f));
        }

        [Test]
        public void AverageIsFramesOverTimeNotMeanOfFps()
        {
            var s = new FrameStats(10);
            for (int i = 0; i < 9; i++) s.Add(0.010f);   // 100 fps
            s.Add(0.100f);                                // one 10 fps frame
            // 10 frames in 0.19 s = 52.6 fps; the mean of per-frame fps would say 91.
            Assert.That(s.AverageFps, Is.EqualTo(10f / 0.19f).Within(0.05f));
        }

        [Test]
        public void OnePercentLowUsesTheSlowestHundredthOfFrames()
        {
            var s = new FrameStats(600);
            for (int i = 0; i < 594; i++) s.Add(0.016f);
            for (int i = 0; i < 6; i++) s.Add(0.050f);   // exactly 1% of 600
            Assert.That(s.OnePercentLowFps, Is.EqualTo(20f).Within(0.01f));
            Assert.That(s.WorstMs, Is.EqualTo(50f).Within(0.01f));
        }

        [Test]
        public void OnePercentLowOfAFewFramesIsTheWorstFrame()
        {
            var s = new FrameStats(20);
            s.Add(0.01f); s.Add(0.02f); s.Add(0.04f);
            Assert.That(s.OnePercentLowFps, Is.EqualTo(25f).Within(0.01f));
        }

        [Test]
        public void RingBufferForgetsOldFramesAndNeverGrows()
        {
            var s = new FrameStats(10);
            for (int i = 0; i < 10; i++) s.Add(0.1f);
            for (int i = 0; i < 10; i++) s.Add(0.01f);
            Assert.That(s.Count, Is.EqualTo(10));
            Assert.That(s.AverageFps, Is.EqualTo(100f).Within(0.01f));
        }

        [Test]
        public void BadFramesAreIgnoredAndEmptyIsZero()
        {
            var s = new FrameStats(10);
            Assert.That(s.AverageFps, Is.EqualTo(0f));
            Assert.That(s.OnePercentLowFps, Is.EqualTo(0f));
            s.Add(0f); s.Add(-1f); s.Add(float.NaN); s.Add(float.PositiveInfinity);
            Assert.That(s.Count, Is.EqualTo(0));
        }

        [Test]
        public void FiveQuickTapsOpenItAndSlowOnesDoNot()
        {
            var t = new TapCounter();
            Assert.That(t.Tap(0f), Is.False); Assert.That(t.Tap(0.3f), Is.False); Assert.That(t.Tap(0.6f), Is.False); Assert.That(t.Tap(0.9f), Is.False);
            Assert.That(t.Tap(1.2f), Is.True);
            Assert.That(t.Tap(1.3f), Is.False, "the count starts over after it opens");
            var slow = new TapCounter();
            for (int i = 0; i < 10; i++) Assert.That(slow.Tap(i * 3f), Is.False);
        }

        [Test]
        public void BenchmarkRunsSixtySecondsThenSummarises()
        {
            var b = new Benchmark(60);
            Assert.That(b.Frame(0.016f), Is.False, "not started yet");
            b.Start();
            bool done = false; int frames = 0;
            while (!done && frames < 10000) { done = b.Frame(1f / 60f); frames++; }
            Assert.That(done, Is.True);
            Assert.That(frames, Is.InRange(3599, 3601));
            Assert.That(b.Done && !b.Running, Is.True);
            var s = b.Summary("Pixel", "High", "saudi_1", 120, 900, true);
            Assert.That(s.averageFps, Is.EqualTo(60f).Within(0.5f));
            Assert.That(s.Verdict, Is.EqualTo("smooth"));
            Assert.That(s.rain, Is.True);
            Assert.That(PerfFormat.Summary(s), Does.Contain("Pixel").And.Contain("60.0"));
        }

        [Test]
        public void SlowRunIsMarkedSlowAndCountsSlowFrames()
        {
            var b = new Benchmark(60);
            b.Start();
            while (!b.Frame(1f / 30f)) { }
            var s = b.Summary("Old phone", "Low", "x", 1, 2, true);
            Assert.That(s.Verdict, Is.EqualTo("slow"));
            Assert.That(s.slowFrameShare, Is.EqualTo(1f).Within(0.001f));
        }

        [Test]
        public void FlyThroughIsTheSamePathEveryRunAndEndsAtTheStageEnd()
        {
            FlyThrough.At(0f, 500f, out var x0, out _, out _);
            FlyThrough.At(30f, 500f, out var xm, out var h1, out var y1);
            FlyThrough.At(30f, 500f, out var xm2, out var h2, out var y2);
            FlyThrough.At(500f, 500f, out var xe, out _, out _);
            Assert.That(x0, Is.EqualTo(0f)); Assert.That(xm, Is.EqualTo(250f).Within(0.01f)); Assert.That(xe, Is.EqualTo(500f));
            Assert.That(h1, Is.EqualTo(h2)); Assert.That(y1, Is.EqualTo(y2)); Assert.That(xm, Is.EqualTo(xm2));
        }

        [Test]
        public void DiagnosticsEventsAreNeverSentToAnalytics()
        {
            foreach (var e in DiagnosticsPolicy.NeverSent) Assert.That(DiagnosticsPolicy.ShouldSend(e), Is.False, e);
            Assert.That(DiagnosticsPolicy.ShouldSend("stage_complete"), Is.True);
        }
    }
}
