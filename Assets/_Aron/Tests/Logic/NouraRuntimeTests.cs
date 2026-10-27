// (c) Ahmed Alamrani. All rights reserved.
using Aron.Logic;
using Aron.Logic.Heroes;
using NUnit.Framework;

namespace Aron.Tests.Logic
{
    /// <summary>The rules the stage nooks and Noura's glide lean on.</summary>
    public class NouraRuntimeTests
    {
        private static HeroDef Aron() => new HeroDef { id = "aron", radius = 0.4f, jumps = 2 };
        private static HeroDef Noura() => new HeroDef { id = "noura", radius = 0.3f, jumps = 2, glideTime = 0.9f, glideFall = 1.6f };

        [Test]
        public void NookSlotLetsNouraInButNotAron()
        {
            Assert.IsTrue(Coop.Fits(Noura(), 0.8f));
            Assert.IsFalse(Coop.Fits(Aron(), 0.8f));
        }

        [Test]
        public void GlideAfterWalkingOffALedgeAndOneAirJump()
        {
            // The controller counts the air jump twice when there was no ground jump.
            var g = new JumpGlide(Noura());
            g.Tick(0.016f, true, false, 0f);
            g.TryJump(false); g.TryJump(false);
            Assert.AreEqual(-1.6f, g.Tick(0.1f, false, true, -5f));
            Assert.IsTrue(g.Gliding);
        }

        [Test]
        public void NoGlideWithoutAFingerDown()
        {
            var g = new JumpGlide(Noura());
            g.TryJump(true); g.TryJump(false);
            Assert.IsNull(g.Tick(0.1f, false, false, -5f));
        }

        [Test]
        public void GlideRunsOut()
        {
            var g = new JumpGlide(Noura());
            g.TryJump(true); g.TryJump(false);
            for (int i = 0; i < 10; i++) g.Tick(0.1f, false, true, -5f);
            Assert.IsNull(g.Tick(0.1f, false, true, -5f));
        }

        [Test]
        public void AronNeverGlides()
        {
            var g = new JumpGlide(Aron());
            g.TryJump(true); g.TryJump(false);
            Assert.IsNull(g.Tick(0.1f, false, true, -5f));
        }

        [Test]
        public void HoldingIsReportedWhileAFingerIsDown()
        {
            var r = new GestureRecognizer();
            r.BeginFrame();
            r.Feed(1, TouchState.Began, 10f, 10f, 0f, 100f);
            Assert.IsTrue(r.EndFrame(0f).holding);
            r.BeginFrame();
            r.Feed(1, TouchState.Ended, 10f, 10f, 0.5f, 100f);
            Assert.IsFalse(r.EndFrame(0.5f).holding);
        }

        [Test]
        public void SwappingNeedsThePartnerCloseAndAHeroNotBusy()
        {
            var s = new HeroSwitch("aron", "noura");
            Assert.IsFalse(s.TrySwitch(1f, 2f, true));
            Assert.IsFalse(s.TrySwitch(1f, 30f, false));
            Assert.IsTrue(s.TrySwitch(1f, 2f, false));
            Assert.AreEqual("noura", s.Active);
            Assert.IsFalse(s.TrySwitch(1.2f, 2f, false));
        }
    }
}
