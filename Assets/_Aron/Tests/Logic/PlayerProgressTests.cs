// (c) Ahmed Alamrani. All rights reserved.
using System;
using Aron.Logic;
using NUnit.Framework;

namespace Aron.Tests.Logic
{
    public class PlayerProgressTests
    {
        [Test]
        public void StartsWithConfiguredLives()
        {
            var p = new PlayerProgress(3, 100);
            Assert.AreEqual(3, p.Lives);
            Assert.AreEqual(0, p.Dates);
            Assert.IsFalse(p.IsGameOver);
        }

        [Test]
        public void HundredDatesGiveAnExtraLife()
        {
            var p = new PlayerProgress(3, 100);
            Assert.AreEqual(0, p.AddDates(99));
            Assert.AreEqual(1, p.AddDates(1));
            Assert.AreEqual(4, p.Lives);
            Assert.AreEqual(0, p.Dates);
        }

        [Test]
        public void LargeDateBatchCanAwardSeveralLives()
        {
            var p = new PlayerProgress(1, 10);
            Assert.AreEqual(2, p.AddDates(25));
            Assert.AreEqual(3, p.Lives);
            Assert.AreEqual(5, p.Dates);
        }

        [Test]
        public void NonPositiveDatesAreIgnored()
        {
            var p = new PlayerProgress(3, 100);
            p.AddDates(-5);
            p.AddDates(0);
            Assert.AreEqual(0, p.Dates);
        }

        [Test]
        public void LosingTheLastLifeEndsTheGame()
        {
            var p = new PlayerProgress(2, 100);
            Assert.IsFalse(p.LoseLife());
            Assert.IsTrue(p.LoseLife());
            Assert.IsTrue(p.IsGameOver);
            Assert.IsTrue(p.LoseLife());
            Assert.AreEqual(0, p.Lives, "lives never go negative");
        }

        [Test]
        public void RejectsInvalidSetup()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new PlayerProgress(0, 100));
            Assert.Throws<ArgumentOutOfRangeException>(() => new PlayerProgress(3, 0));
        }
    }
}
