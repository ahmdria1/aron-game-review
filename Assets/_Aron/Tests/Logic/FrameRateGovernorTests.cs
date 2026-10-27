// (c) Ahmed Alamrani. All rights reserved.
using Aron.Logic;
using NUnit.Framework;

namespace Aron.Tests.Logic
{
    public class FrameRateGovernorTests
    {
        private static FrameRateChange Run(FrameRateGovernor g, float fps, float seconds)
        {
            float dt = 1f / fps;
            for (float t = 0f; t < seconds; t += dt)
            {
                var change = g.Update(dt);
                if (change != FrameRateChange.None) return change;
            }
            return FrameRateChange.None;
        }

        [Test]
        public void SteadySixtyStaysAtSixty()
        {
            var g = new FrameRateGovernor(60);
            Assert.AreEqual(FrameRateChange.None, Run(g, 59f, 30f));
            Assert.AreEqual(60, g.Target);
        }

        [Test]
        public void SustainedSlowdownDropsTo30()
        {
            var g = new FrameRateGovernor(60);
            Assert.AreEqual(FrameRateChange.DropTo30, Run(g, 40f, 15f));
            Assert.AreEqual(30, g.Target);
        }

        [Test]
        public void ShortHitchesAreForgiven()
        {
            var g = new FrameRateGovernor(60);
            Run(g, 60f, 4f);
            for (int i = 0; i < 10; i++)
            {
                Assert.AreEqual(FrameRateChange.None, Run(g, 35f, 1f));
                Assert.AreEqual(FrameRateChange.None, Run(g, 60f, 3f));
            }
            Assert.AreEqual(60, g.Target);
        }

        [Test]
        public void LoadingSpikesAndWarmupAreIgnored()
        {
            var g = new FrameRateGovernor(60);
            Assert.AreEqual(FrameRateChange.None, g.Update(2f));
            Assert.AreEqual(FrameRateChange.None, Run(g, 20f, 2.5f), "still warming up");
        }

        [Test]
        public void StrugglingAt30AsksForALowerTier()
        {
            var g = new FrameRateGovernor(30);
            Assert.AreEqual(FrameRateChange.LowerTier, Run(g, 20f, 15f));
            Assert.AreEqual(30, g.Target);
        }

        [Test]
        public void ResetRestartsWarmup()
        {
            var g = new FrameRateGovernor(60);
            Run(g, 60f, 5f);
            g.Reset();
            Assert.AreEqual(FrameRateChange.None, Run(g, 20f, 2.5f));
        }
    }
}
