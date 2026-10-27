// (c) Ahmed Alamrani. All rights reserved.
using Aron.Logic;
using NUnit.Framework;

namespace Aron.Tests.Logic
{
    public class MountRideTests
    {
        [Test]
        public void ClimbingOnTakesAMomentThenYouRide()
        {
            var r = new MountRide();
            Assert.IsTrue(r.TryMount(MountKind.Camel, 0f));
            Assert.AreEqual(MountPhase.Mounting, r.Phase);
            Assert.IsFalse(r.CanControl);
            r.Tick(0.5f);
            Assert.AreEqual(MountPhase.Riding, r.Phase);
            Assert.AreEqual(MountKind.Camel, r.Kind);
            Assert.IsTrue(r.CanControl);
        }

        [Test]
        public void AHitThrowsYouOffInsteadOfHurting()
        {
            var r = new MountRide();
            r.TryMount(MountKind.Horse, 0f);
            r.Tick(1f);
            Assert.AreEqual(HitOutcome.ThrownOff, r.TakeHit(2f));
            Assert.AreEqual(MountPhase.Thrown, r.Phase);
            Assert.IsFalse(r.IsMounted);
            Assert.AreEqual(HitOutcome.Ignored, r.TakeHit(2.3f), "a second hit while tumbling is free");
            r.Tick(3.5f);
            Assert.AreEqual(MountPhase.OnFoot, r.Phase);
            Assert.AreEqual(HitOutcome.Hurt, r.TakeHit(4f));
        }

        [Test]
        public void OnFootHitsHurt()
        {
            Assert.AreEqual(HitOutcome.Hurt, new MountRide().TakeHit(0f));
        }

        [Test]
        public void YouCantJumpStraightBackOnAfterBeingThrown()
        {
            var r = new MountRide();
            r.TryMount(MountKind.Camel, 0f);
            r.Tick(1f);
            r.TakeHit(1f);
            Assert.IsFalse(r.TryMount(MountKind.Camel, 1.5f));
            r.Tick(2.3f);
            Assert.IsFalse(r.TryMount(MountKind.Camel, 2.4f));
            Assert.IsTrue(r.TryMount(MountKind.Camel, 2.6f));
        }

        [Test]
        public void SwappingAnimalsWhileRiding()
        {
            var r = new MountRide();
            r.TryMount(MountKind.Camel, 0f);
            r.Tick(1f);
            Assert.IsFalse(r.TryMount(MountKind.Camel, 2f), "same animal again does nothing");
            Assert.IsTrue(r.TryMount(MountKind.Horse, 2f));
            Assert.AreEqual(MountKind.Horse, r.Kind);
        }

        [Test]
        public void GettingOffOnPurposeAndDying()
        {
            var r = new MountRide();
            Assert.IsFalse(r.Dismount(0f));
            r.TryMount(MountKind.Camel, 0f);
            r.Tick(1f);
            Assert.IsTrue(r.Dismount(2f));
            Assert.AreEqual(MountKind.None, r.Kind);
            r.TryMount(MountKind.Camel, 5f);
            r.Reset();
            Assert.AreEqual(MountPhase.OnFoot, r.Phase);
            Assert.IsTrue(r.TryMount(MountKind.Camel, 5.1f), "no remount delay after a reset");
        }

        [Test]
        public void CamelJumpsHigherHorseRunsFaster()
        {
            var camel = MountStats.For(MountKind.Camel);
            var horse = MountStats.For(MountKind.Horse);
            Assert.Greater(camel.jump, horse.jump);
            Assert.Greater(horse.speed, camel.speed);
            Assert.Greater(camel.jump, 1f);
            Assert.AreEqual(1f, MountStats.For(MountKind.None).speed);

            var r = new MountRide();
            Assert.AreEqual(1f, r.Stats.speed);
            r.TryMount(MountKind.Horse, 0f);
            Assert.AreEqual(horse.speed, r.Stats.speed);
        }

        [Test]
        public void NothingToRide()
        {
            Assert.IsFalse(new MountRide().TryMount(MountKind.None, 0f));
        }
    }
}
