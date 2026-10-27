// (c) Ahmed Alamrani. All rights reserved.
using System.Collections.Generic;
using Aron.Logic;
using NUnit.Framework;

namespace Aron.Tests.Logic
{
    public class FalconBrainTests
    {
        private static readonly Spot Me = new Spot(0f, 0f, 0f);

        [Test]
        public void PicksTheNearestEnemyInRange()
        {
            var enemies = new List<Spot> { new Spot(0f, 0f, 6f), new Spot(3f, 0f, 0f), new Spot(1f, 0f, 20f) };
            Assert.AreEqual(1, FalconBrain.Nearest(Me, enemies, 8f));
            Assert.AreEqual(-1, FalconBrain.Nearest(Me, enemies, 2f));
            Assert.AreEqual(-1, FalconBrain.Nearest(Me, new List<Spot>(), 8f));
        }

        [Test]
        public void SwoopHasACooldown()
        {
            var f = new FalconBrain { SwoopCooldown = 7f };
            var enemies = new List<Spot> { new Spot(2f, 0f, 2f) };
            Assert.AreEqual(0, f.TrySwoop(0f, Me, enemies));
            Assert.AreEqual(FalconState.Swooping, f.State);
            f.Arrived(0.6f);
            f.BackHome();
            Assert.AreEqual(-1, f.TrySwoop(3f, Me, enemies));
            Assert.AreEqual(4f, f.SwoopCooldownLeft(3f), 1e-4);
            Assert.AreEqual(0, f.TrySwoop(7f, Me, enemies));
        }

        [Test]
        public void NoTargetMeansNoCooldownSpent()
        {
            var f = new FalconBrain();
            Assert.AreEqual(-1, f.TrySwoop(0f, Me, new List<Spot> { new Spot(30f, 0f, 0f) }));
            Assert.IsTrue(f.CanSwoop(0.1f));
            Assert.AreEqual(FalconState.Hover, f.State);
        }

        [Test]
        public void CantSwoopWhileFetching()
        {
            var f = new FalconBrain();
            Assert.AreEqual(0, f.TryFetch(0f, Me, new List<Spot> { new Spot(1f, 1f, 1f) }));
            Assert.AreEqual(-1, f.TrySwoop(0.2f, Me, new List<Spot> { new Spot(1f, 0f, 0f) }));
        }

        [Test]
        public void FetchesOnlyEverySoOften()
        {
            var f = new FalconBrain { FetchInterval = 4f };
            var coins = new List<Spot> { new Spot(2f, 0f, 0f) };
            Assert.AreEqual(0, f.TryFetch(0f, Me, coins));
            f.Arrived(0.5f); f.BackHome();
            Assert.AreEqual(-1, f.TryFetch(2f, Me, coins));
            Assert.AreEqual(0, f.TryFetch(4f, Me, coins));
        }

        [Test]
        public void ATripThatTakesTooLongGivesUp()
        {
            var f = new FalconBrain { MaxTripTime = 2f };
            f.TrySwoop(0f, Me, new List<Spot> { new Spot(1f, 0f, 0f) });
            f.Tick(2.5f, false);
            Assert.AreEqual(FalconState.Returning, f.State);
            f.Tick(7f, false);
            Assert.AreEqual(FalconState.Hover, f.State);
        }

        [Test]
        public void LandsOnTheArmWhenAronStandsStill()
        {
            var f = new FalconBrain { PerchDelay = 2f };
            f.Tick(0f, true);
            f.Tick(1f, true);
            Assert.AreEqual(FalconState.Hover, f.State);
            f.Tick(2.1f, true);
            Assert.AreEqual(FalconState.Perched, f.State);
            f.Tick(2.2f, false);
            Assert.AreEqual(FalconState.Hover, f.State);
            f.Tick(3f, true);
            f.Tick(4f, true);
            Assert.AreEqual(FalconState.Hover, f.State, "the wait starts again after moving");
        }
    
        [Test]
        public void TheFalconFetchesAKeyWithinTwiceTheCoinRange()
        {
            var f = new FalconBrain { FetchRange = 6f };
            Assert.AreEqual(12f, f.KeyRange, 1e-4);
            Assert.IsFalse(f.TryFetchKey(0f, Me, new Spot(0f, 4f, 20f)), "too far");
            Assert.AreEqual(FalconState.Hover, f.State);
            Assert.IsTrue(f.TryFetchKey(0f, Me, new Spot(0f, 3.6f, 9f)));
            Assert.AreEqual(FalconState.Fetching, f.State);
            Assert.IsFalse(f.TryFetchKey(0.5f, Me, new Spot(0f, 3.6f, 9f)), "already busy");
            f.Arrived(1f); f.BackHome();
            Assert.IsFalse(f.TryFetchKey(2f, Me, new Spot(0f, 3.6f, 9f)), "waits out the fetch interval");
            Assert.IsTrue(f.TryFetchKey(6f, Me, new Spot(0f, 3.6f, 9f)));
        }
    }
}
