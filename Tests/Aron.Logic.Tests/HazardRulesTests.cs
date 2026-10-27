// (c) Ahmed Alamrani. All rights reserved.
using System.Linq;
using Aron.Logic.Regions;
using NUnit.Framework;

namespace Aron.Logic.Tests
{
    /// <summary>Touching enemies, dates into lives, the box count, the golden bisht and the mercy rule.</summary>
    public class HazardRulesTests
    {
        [Test]
        public void LandingOnTheHeadFinishesTheEnemy()
        {
            Assert.That(HeroContact.Resolve(true, 0.9f, false, false, false), Is.EqualTo(ContactResult.EnemyDefeated));
            Assert.That(HeroContact.Resolve(true, 0.9f, false, false, true), Is.EqualTo(ContactResult.EnemyDefeated));
        }

        [Test]
        public void AttackingFinishesTheEnemyFromAnySide()
        {
            Assert.That(HeroContact.Resolve(false, 0f, true, false, false), Is.EqualTo(ContactResult.EnemyDefeated));
            Assert.That(HeroContact.Resolve(true, 0.1f, true, false, false), Is.EqualTo(ContactResult.EnemyDefeated));
        }

        [Test]
        public void WalkingIntoItFromTheSideOrFrontHurts()
        {
            Assert.That(HeroContact.Resolve(false, 0f, false, false, false), Is.EqualTo(ContactResult.HeroHurt));
            // Falling past its side, not onto its head.
            Assert.That(HeroContact.Resolve(true, 0.3f, false, false, false), Is.EqualTo(ContactResult.HeroHurt));
            // Rising up through it isn't a stomp either.
            Assert.That(HeroContact.Resolve(false, 1f, false, false, false), Is.EqualTo(ContactResult.HeroHurt));
        }

        [Test]
        public void BlinkingAfterAHitNothingHappens()
        {
            Assert.That(HeroContact.Resolve(false, 0f, false, false, true), Is.EqualTo(ContactResult.Nothing));
        }

        [Test]
        public void InvincibleHeroSmashesWhateverHeTouches()
        {
            Assert.That(HeroContact.Resolve(false, 0f, false, true, false), Is.EqualTo(ContactResult.EnemyDefeated));
        }

        [Test]
        public void EveryHundredDatesIsALifeAndTheCounterStartsAgain()
        {
            var p = new PlayerProgress(3, 100);
            Assert.That(p.AddDates(99), Is.EqualTo(0));
            Assert.That(p.ToNextLife, Is.EqualTo(1));
            Assert.That(p.AddDates(1), Is.EqualTo(1));
            Assert.That(p.Lives, Is.EqualTo(4));
            Assert.That(p.Dates, Is.EqualTo(0));
            Assert.That(p.AddDates(250), Is.EqualTo(2));
            Assert.That(p.Lives, Is.EqualTo(6));
            Assert.That(p.Dates, Is.EqualTo(50));
            Assert.That(p.ToNextLife, Is.EqualTo(50));
        }

        [Test]
        public void BoxTallyCountsBrokenAndLeft()
        {
            var t = new BoxTally();
            t.SetTotal(42);
            for (int i = 0; i < 37; i++) t.Break();
            Assert.That(t.Broken, Is.EqualTo(37));
            Assert.That(t.Left, Is.EqualTo(5));
            Assert.That(t.AllBroken, Is.False);
            Assert.That(t.Line("{0} / {1} - {2}"), Is.EqualTo("37 / 42 - 5"));
            for (int i = 0; i < 5; i++) t.Break();
            Assert.That(t.AllBroken, Is.True);
            Assert.That(t.Left, Is.Zero);
        }

        [Test]
        public void ABoxNobodyCountedStillCounts()
        {
            var t = new BoxTally();
            t.SetTotal(1);
            t.Break(); t.Break();
            Assert.That(t.Total, Is.EqualTo(2));
            Assert.That(t.Left, Is.Zero);
        }

        [Test]
        public void ArabicBoxLineKeepsItsNumbersInOrder()
        {
            var t = new BoxTally();
            t.SetTotal(42);
            for (int i = 0; i < 37; i++) t.Break();
            string line = t.Line("الصناديق: {0} / {1} — باقي {2}");
            Assert.That(line, Is.EqualTo("الصناديق: 37 / 42 — باقي 5"));
            // TMP draws right to left, so what reads on screen is the shaped string backwards.
            string onScreen = new string(ArabicText.Fix(line).Reverse().ToArray());
            Assert.That(onScreen, Does.Contain("37 / 42"));
            Assert.That(onScreen.IndexOf("37"), Is.LessThan(onScreen.IndexOf("42")));
        }

        [Test]
        public void EachBishtTakesOneHit()
        {
            var b = new BishtPower();
            Assert.That(b.Absorb(), Is.False);
            Assert.That(b.Collect(), Is.EqualTo(BishtPickup.Layer));
            Assert.That(b.Collect(), Is.EqualTo(BishtPickup.Layer));
            Assert.That(b.Layers, Is.EqualTo(2));
            Assert.That(b.Absorb(), Is.True);
            Assert.That(b.Absorb(), Is.True);
            Assert.That(b.Absorb(), Is.False);
        }

        [Test]
        public void TheThirdBishtMakesHimUntouchableForTenSeconds()
        {
            var b = new BishtPower();
            b.Collect(); b.Collect();
            Assert.That(b.Collect(), Is.EqualTo(BishtPickup.Invincible));
            Assert.That(b.Invincible, Is.True);
            for (int i = 0; i < 20; i++) Assert.That(b.Absorb(), Is.True);
            Assert.That(b.Layers, Is.EqualTo(BishtPower.MaxLayers));
            Assert.That(b.Tick(9.9f), Is.False);
            Assert.That(b.Invincible, Is.True);
            Assert.That(b.Tick(0.2f), Is.True);
            Assert.That(b.Invincible, Is.False);
            Assert.That(b.Layers, Is.EqualTo(2));
        }

        [Test]
        public void ABishtWhileUntouchableRestartsTheClock()
        {
            var b = new BishtPower();
            b.Collect(); b.Collect(); b.Collect();
            b.Tick(8f);
            Assert.That(b.Collect(), Is.EqualTo(BishtPickup.Topped));
            Assert.That(b.InvincibleLeft, Is.EqualTo(BishtPower.InvincibleSeconds));
        }

        [Test]
        public void LosingALifeTakesTheBisht()
        {
            var b = new BishtPower();
            b.Collect(); b.Collect(); b.Collect();
            b.Clear();
            Assert.That(b.Layers, Is.Zero);
            Assert.That(b.Invincible, Is.False);
        }

        [Test]
        public void ThirdDeathAtTheSameCheckpointComesBackWithABisht()
        {
            var m = new MercyRule();
            m.Reached("cp1");
            Assert.That(m.Died("cp1"), Is.False);
            Assert.That(m.Died("cp1"), Is.False);
            Assert.That(m.Died("cp1"), Is.True);
            Assert.That(m.Died("cp1"), Is.True, "keeps helping until he gets past");
        }

        [Test]
        public void ANewCheckpointStartsTheMercyCountAgain()
        {
            var m = new MercyRule();
            m.Died("cp1"); m.Died("cp1");
            m.Reached("cp2");
            Assert.That(m.Died("cp2"), Is.False);
            Assert.That(m.Died("cp2"), Is.False);
            Assert.That(m.Died("cp1"), Is.False, "back at an old checkpoint counts from zero");
        }

        [Test]
        public void LeversAllHaveToBePulled()
        {
            var p = new LeverPuzzle(3);
            Assert.That(p.Pull(0), Is.EqualTo(PuzzleStep.Progress));
            Assert.That(p.Pull(0), Is.EqualTo(PuzzleStep.Nothing));
            Assert.That(p.Pull(2), Is.EqualTo(PuzzleStep.Progress));
            Assert.That(p.Solved, Is.False);
            Assert.That(p.Pull(1), Is.EqualTo(PuzzleStep.Solved));
        }

        [Test]
        public void RunesGoInOrderAndAMistakeStartsOver()
        {
            var r = new RunePuzzle(3);
            Assert.That(r.Step(0), Is.EqualTo(PuzzleStep.Progress));
            Assert.That(r.Step(0), Is.EqualTo(PuzzleStep.Nothing));
            Assert.That(r.Step(2), Is.EqualTo(PuzzleStep.Wrong));
            Assert.That(r.Next, Is.Zero);
            r.Step(0);
            Assert.That(r.Step(1), Is.EqualTo(PuzzleStep.Progress));
            Assert.That(r.Step(2), Is.EqualTo(PuzzleStep.Solved));
        }

        [Test]
        public void TimedGateClosesAgain()
        {
            var d = new TimedDoor(5f);
            Assert.That(d.IsOpen(0f), Is.False);
            d.Trigger(10f);
            Assert.That(d.IsOpen(14.9f), Is.True);
            Assert.That(d.IsOpen(15.1f), Is.False);
            float open = StageLayout.TimedOpenSeconds(24f, StageLayout.FootPace);
            Assert.That(open, Is.GreaterThan(24f / StageLayout.FootPace + 1f));
        }

        [Test]
        public void CrateStopsOnThePlate()
        {
            var c = new CratePush(4f, 10f);
            Assert.That(c.Push(3f), Is.EqualTo(PuzzleStep.Progress));
            Assert.That(c.Push(10f), Is.EqualTo(PuzzleStep.Solved));
            Assert.That(c.Position, Is.EqualTo(10f));
            Assert.That(c.Push(1f), Is.EqualTo(PuzzleStep.Nothing));
        }

        [Test]
        public void SoundNamesAreUniqueAndFileFriendly()
        {
            var all = AudioCues.Sfx.Concat(AudioCues.Music).Concat(AudioCues.Voice).ToList();
            Assert.That(all.Distinct().Count(), Is.EqualTo(all.Count));
            Assert.That(all.All(id => System.Text.RegularExpressions.Regex.IsMatch(id, "^[a-z0-9_]+$")), Is.True);
            Assert.That(AudioCues.Voice.All(id => id.StartsWith("vo_")), Is.True);
        }
    }
}
