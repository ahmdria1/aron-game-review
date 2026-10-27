// (c) Ahmed Alamrani. All rights reserved.
using Aron.Logic.Heroes;
using NUnit.Framework;

namespace Aron.Logic.Tests
{
    public class HeroTests
    {
        private static HeroDef Aron() => new HeroDef { id = "aron", speed = 1f, radius = 0.4f, jumps = 2, weapon = "agal", companion = "falcon", rollsSleeves = true };

        private static HeroDef Noura() => new HeroDef
        {
            id = "noura", speed = 1.12f, radius = 0.3f, jumps = 2, glideTime = 0.9f, glideFall = 1.6f,
            opensLocks = true, seesSecrets = true, weapon = "handbag", throwRange = 7f, throwTime = 1f, companion = "oryx",
        };

        private static HeroFile File() => new HeroFile { heroes = new[] { Aron(), Noura() } };

        [Test]
        public void HeroFileIsValid() => Assert.That(File().Validate(), Is.Empty);

        [Test]
        public void NouraMustBeLighterQuickerAndSmart()
        {
            var f = File();
            f.heroes[1].radius = 0.45f;
            f.heroes[1].speed = 0.9f;
            f.heroes[1].opensLocks = false;
            f.heroes[1].glideTime = 0f;
            var e = f.Validate();
            Assert.That(e, Has.Some.Contains("slimmer"));
            Assert.That(e, Has.Some.Contains("quicker"));
            Assert.That(e, Has.Some.Contains("locked"));
            Assert.That(e, Has.Some.Contains("glides"));
        }

        [Test]
        public void SwitchNeedsThePartnerCloseAndHasACooldown()
        {
            var s = new HeroSwitch();
            Assert.That(s.TrySwitch(0f, 20f, false), Is.False, "too far");
            Assert.That(s.TrySwitch(0f, 5f, true), Is.False, "mid-jump or riding");
            Assert.That(s.TrySwitch(0f, 5f, false), Is.True);
            Assert.That(s.Active, Is.EqualTo("noura"));
            Assert.That(s.Partner, Is.EqualTo("aron"));
            Assert.That(s.TrySwitch(0.3f, 5f, false), Is.False, "cooling down");
            Assert.That(s.TrySwitch(1f, 5f, false), Is.True);
            Assert.That(s.Active, Is.EqualTo("aron"));
        }

        [Test]
        public void DoubleJumpThenGlideWhileHolding()
        {
            var j = new JumpGlide(Noura());
            Assert.That(j.TryJump(true), Is.True);
            Assert.That(j.Tick(0.1f, false, true, -2f), Is.Null, "one jump used, no glide yet");
            Assert.That(j.TryJump(false), Is.True);
            Assert.That(j.TryJump(false), Is.False, "only two");
            Assert.That(j.Tick(0.1f, false, true, 3f), Is.Null, "still rising");
            Assert.That(j.Tick(0.1f, false, true, -4f), Is.EqualTo(-1.6f));
            Assert.That(j.Gliding, Is.True);
            Assert.That(j.Tick(0.1f, false, false, -4f), Is.Null, "finger up");
            for (int i = 0; i < 20; i++) j.Tick(0.1f, false, true, -4f);
            Assert.That(j.Gliding, Is.False, "glide ran out");
            j.Tick(0.1f, true, false, 0f);
            Assert.That(j.JumpsUsed, Is.EqualTo(0));
            Assert.That(j.GlideLeft, Is.EqualTo(0.9f));
        }

        [Test]
        public void AronDoesNotGlide()
        {
            var j = new JumpGlide(Aron());
            j.TryJump(true); j.TryJump(false);
            Assert.That(j.Tick(0.1f, false, true, -4f), Is.Null);
        }

        [Test]
        public void HandbagGoesOutAndComesBack()
        {
            var b = new Boomerang(7f, 1f);
            Assert.That(b.TryThrow(0f), Is.True);
            Assert.That(b.TryThrow(0.2f), Is.False, "already out");
            Assert.That(b.Distance(0.5f), Is.EqualTo(7f).Within(1e-3));
            Assert.That(b.Returning(0.4f), Is.False);
            Assert.That(b.Returning(0.7f), Is.True);
            Assert.That(b.Distance(0.75f), Is.LessThan(7f).And.GreaterThan(0f));
            Assert.That(b.Distance(1.05f), Is.EqualTo(0f));
            Assert.That(b.Out, Is.False);
            Assert.That(b.TryThrow(1.1f), Is.True);
        }

        [Test]
        public void CoopBeatsNeedBothHeroes()
        {
            Assert.That(Coop.Ready(CoopKind.BoostUp, 1f, 1.5f, "aron"), Is.True);
            Assert.That(Coop.Ready(CoopKind.BoostUp, 1f, 6f, "aron"), Is.False);
            Assert.That(Coop.Ready(CoopKind.PullUp, 1f, 3f, "aron"), Is.True, "she reaches down from the ledge");
            Assert.That(Coop.Ready(CoopKind.LockedGate, 1f, 1f, "aron"), Is.False, "only Noura opens locks");
            Assert.That(Coop.Ready(CoopKind.LockedGate, 9f, 1f, "noura"), Is.True);
            Assert.That(Coop.Lifted(CoopKind.BoostUp), Is.EqualTo("noura"));
            Assert.That(Coop.Lifted(CoopKind.PullUp), Is.EqualTo("aron"));
            Assert.That(Coop.Lifted(CoopKind.LockedGate), Is.Null);
        }

        [Test]
        public void OnlyNouraFitsTheNarrowGap()
        {
            Assert.That(Coop.Fits(Noura(), 0.8f), Is.True);
            Assert.That(Coop.Fits(Aron(), 0.8f), Is.False);
            Assert.That(Coop.Fits(Aron(), 1f), Is.True);
        }
    }
}
