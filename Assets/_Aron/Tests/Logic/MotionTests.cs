// (c) Ahmed Alamrani. All rights reserved.
using System.Collections.Generic;
using System.Linq;
using Aron.Logic;
using Aron.Logic.Regions;
using NUnit.Framework;

namespace Aron.Tests.Logic
{
    public class CheckpointTrailTests
    {
        private static CheckpointTrail.Spot At(float z) => new CheckpointTrail.Spot(0f, 0f, z);

        [Test]
        public void ComesBackAtTheLastBrokenBox()
        {
            var t = new CheckpointTrail(At(1f));
            Assert.That(t.Save(At(60f)), Is.True);
            Assert.That(t.Save(At(130f)), Is.True);
            Assert.That(t.Died(livesLeft: true), Is.EqualTo(DeathOutcome.Respawn));
            Assert.That(t.Current.z, Is.EqualTo(130f));
            Assert.That(t.Saves, Is.EqualTo(2));
        }

        [Test]
        public void BreakingTheSameBoxTwiceSavesOnce()
        {
            var t = new CheckpointTrail(At(1f));
            t.Save(At(60f));
            Assert.That(t.Save(At(60f)), Is.False);
            Assert.That(t.Saves, Is.EqualTo(1));
        }

        [Test]
        public void GameOverSendsTheStageBackToTheStart()
        {
            var t = new CheckpointTrail(At(1f));
            t.Save(At(60f));
            Assert.That(t.Died(livesLeft: false), Is.EqualTo(DeathOutcome.RestartStage));
            Assert.That(t.Current.z, Is.EqualTo(1f));
            Assert.That(t.Saves, Is.Zero);
        }
    }

    public class DeathActsTests
    {
        [Test]
        public void AronEnemyHitSpinsThenLiesWithStarsAndTheAgalRollsOff()
            => Assert.That(DeathActs.Beats("aron", DeathCause.Enemy),
                Is.EqualTo(new[] { DeathActs.Spin, DeathActs.Topple, DeathActs.Stars, DeathActs.AgalRolls }));

        [Test]
        public void AronPitThrowsTheShemaghUpAndWaves()
        {
            var b = DeathActs.Beats("aron", DeathCause.Pit);
            Assert.That(b, Does.Contain(DeathActs.ShemaghUp).And.Contain(DeathActs.Wave).And.Contain(DeathActs.Drop));
            Assert.That(System.Array.IndexOf(b, DeathActs.ShemaghUp), Is.LessThan(System.Array.IndexOf(b, DeathActs.Drop)));
        }

        [Test]
        public void AronWaterLeavesOnlyTheShemaghFloating()
        {
            var b = DeathActs.Beats("aron", DeathCause.Water);
            Assert.That(b.First(), Is.EqualTo(DeathActs.Bubbles));
            Assert.That(b.Last(), Is.EqualTo(DeathActs.ShemaghFloats));
        }

        [Test]
        public void NouraSitsDazedWithHerBagAndTheOryx()
        {
            var b = DeathActs.Beats("noura", DeathCause.Enemy);
            Assert.That(b, Is.EqualTo(new[] { DeathActs.AbayaSwirl, DeathActs.SitDown, DeathActs.Stars, DeathActs.BagBeside, DeathActs.Uff, DeathActs.OryxNudge }));
            Assert.That(DeathActs.UffAr, Is.EqualTo("أفّ"));
        }

        [Test]
        public void EveryCauseLooksDifferent()
        {
            foreach (var hero in new[] { "aron", "noura" })
            {
                var acts = new[] { DeathCause.Enemy, DeathCause.Pit, DeathCause.Water }.Select(c => string.Join(",", DeathActs.Beats(hero, c))).ToList();
                Assert.That(acts.Distinct().Count(), Is.EqualTo(3), hero);
            }
        }

        [Test]
        public void NothingIsGoryOrLong()
        {
            foreach (var hero in new[] { "aron", "noura" })
                foreach (DeathCause c in System.Enum.GetValues(typeof(DeathCause)))
                {
                    Assert.That(DeathActs.Seconds(hero, c), Is.InRange(1f, 3f));
                    Assert.That(DeathActs.Beats(hero, c), Is.Not.Empty);
                }
        }

        [Test]
        public void FallingIntoTheSeaOrRiverIsWater()
        {
            Assert.That(DeathActs.ForFall("coast"), Is.EqualTo(DeathCause.Water));
            Assert.That(DeathActs.ForFall("river"), Is.EqualTo(DeathCause.Water));
            Assert.That(DeathActs.ForFall("desert"), Is.EqualTo(DeathCause.Pit));
            Assert.That(DeathActs.ForFall(null), Is.EqualTo(DeathCause.Pit));
        }

        [Test]
        public void EachCauseHasItsOwnSound()
        {
            Assert.That(AudioCues.ForDeath(DeathCause.Pit, "aron"), Is.EqualTo(AudioCues.DeathFall));
            Assert.That(AudioCues.ForDeath(DeathCause.Water, "aron"), Is.EqualTo(AudioCues.DeathSplash));
            Assert.That(AudioCues.ForDeath(DeathCause.Enemy, "aron"), Is.EqualTo(AudioCues.DeathSpin));
            Assert.That(AudioCues.ForDeath(DeathCause.Enemy, "noura"), Is.EqualTo(AudioCues.DeathSit));
            foreach (var id in new[] { AudioCues.DeathFall, AudioCues.DeathSplash, AudioCues.DeathSpin, AudioCues.DeathSit })
                Assert.That(AudioCues.Sfx, Does.Contain(id), id);
        }
    }

    public class LooseMountTests
    {
        [Test]
        public void BoltsThenWaitsThenCanBeRiddenAgain()
        {
            Assert.That(LooseMount.At(0.1f, 0f), Is.EqualTo(LooseState.Bolting));
            Assert.That(LooseMount.At(LooseMount.BoltSeconds + 0.1f, 0f), Is.EqualTo(LooseState.Waiting));
            Assert.That(LooseMount.At(LooseMount.BoltSeconds + LooseMount.SettleSeconds + 0.1f, 0f), Is.EqualTo(LooseState.Ready));
        }

        [Test]
        public void GoesHomeWhenLeftFarBehind()
            => Assert.That(LooseMount.At(5f, LooseMount.LeftBehind + 1f), Is.EqualTo(LooseState.GoHome));

        [Test]
        public void StaysOnTheTrack()
        {
            Assert.That(LooseMount.ClampSide(9f), Is.EqualTo(LooseMount.MaxSide));
            Assert.That(LooseMount.ClampSide(-9f), Is.EqualTo(-LooseMount.MaxSide));
            Assert.That(LooseMount.ClampSide(1f), Is.EqualTo(1f));
        }
    }

    public class HemGrabTests
    {
        private static void Run(HemGrab g, bool sprint, float seconds)
        {
            for (float t = 0f; t < seconds; t += 0.01f) g.Tick(sprint, 0.01f);
        }

        [Test]
        public void ReachLiftBiteThenRunWithBothArms()
        {
            var g = new HemGrab();
            var seen = new List<HemPhase>();
            for (int i = 0; i < 100; i++)
            {
                g.Tick(true, 0.01f);
                if (seen.Count == 0 || seen.Last() != g.Phase) seen.Add(g.Phase);
            }
            Assert.That(seen, Is.EqualTo(new[] { HemPhase.Reach, HemPhase.Lift, HemPhase.Bite, HemPhase.Hold }));
            Assert.That(g.Bitten, Is.True);
            Assert.That(g.Raise, Is.EqualTo(1f));
            Assert.That(g.Hand, Is.Zero);
            Assert.That(g.Pump, Is.EqualTo(1f));
        }

        [Test]
        public void TheHandIsOnTheHemWhileLifting()
        {
            var g = new HemGrab();
            Run(g, true, HemGrab.ReachTime + HemGrab.LiftTime * 0.5f);
            Assert.That(g.Phase, Is.EqualTo(HemPhase.Lift));
            Assert.That(g.Hand, Is.EqualTo(1f));
            Assert.That(g.Raise, Is.GreaterThan(0f).And.LessThan(1f));
            Assert.That(g.Bitten, Is.False);
        }

        [Test]
        public void LetsGoAndTheHemDropsWhenTheSprintEnds()
        {
            var g = new HemGrab();
            Run(g, true, 1f);
            g.Tick(false, 0.01f);
            Assert.That(g.Phase, Is.EqualTo(HemPhase.Release));
            Assert.That(g.Bitten, Is.False);
            Run(g, false, 1f);
            Assert.That(g.Phase, Is.EqualTo(HemPhase.Down));
            Assert.That(g.Raise, Is.Zero);
            Assert.That(g.Pump, Is.Zero);
        }

        [Test]
        public void SprintingAgainMidDropPicksUpWhereTheHemIs()
        {
            var g = new HemGrab();
            Run(g, true, 1f);
            Run(g, false, HemGrab.ReleaseTime + HemGrab.DropTime * 0.3f);
            float before = g.Raise;
            g.Tick(true, 0.001f);
            Assert.That(g.Phase, Is.EqualTo(HemPhase.Reach));
            Assert.That(g.Raise, Is.EqualTo(before).Within(0.05f));
        }
    }

    public class HemShapeTests
    {
        private static readonly V3 Up = new V3(0f, 1f, 0f), Fwd = new V3(0f, 0f, 1f);
        private static readonly V3 Waist = new V3(0f, 1f, 0.1f), Hem = new V3(0f, 0.1f, 0.15f), Mouth = new V3(0f, 1.6f, 0.12f);

        private static float Dist(V3 a, V3 b) => (a - b).Length;

        [Test]
        public void TheMiddleOfTheFrontHemIsInHisTeeth()
        {
            var p = HemShape.Point(1f, 1f, Hem, Waist, Hem, Mouth, Up, Fwd, 1f, 1f);
            Assert.That(Dist(p, Mouth), Is.LessThan(0.001f));
        }

        [Test]
        public void HangsStraightWhenNotLifted()
        {
            var hanging = new V3(0.1f, 0.5f, 0.15f);
            Assert.That(Dist(HemShape.Point(0.5f, 1f, hanging, Waist, Hem, Mouth, Up, Fwd, 0f, 1f), hanging), Is.LessThan(0.0001f));
        }

        [Test]
        public void TheSidesComeUpButStayBelowTheFront()
        {
            var side = HemShape.Point(1f, 0f, Hem, Waist, Hem, Mouth, Up, Fwd, 1f, 1f);
            var front = HemShape.Point(1f, 1f, Hem, Waist, Hem, Mouth, Up, Fwd, 1f, 1f);
            Assert.That(side.y, Is.GreaterThan(Hem.y + 0.5f));
            Assert.That(side.y, Is.LessThan(front.y));
        }

        [Test]
        public void TheClothFoldsBelowTheStraightLine()
        {
            // Halfway between the waist and the mouth, the slack cloth hangs lower than the straight line would.
            var mid = HemShape.Point(0.5f, 1f, Hem, Waist, Hem, Mouth, Up, Fwd, 1f, 1f);
            float straightY = (Waist.y + Mouth.y) * 0.5f;
            Assert.That(mid.y, Is.LessThan(straightY));
            Assert.That(mid.z, Is.GreaterThan(Waist.z));
        }
    }

    public class StageChunksTests
    {
        private static StageLayout AnyLong()
        {
            var s = new StageDef { id = "t", nameAr = "t", terrain = "desert", difficulty = 3 };
            return StageLayout.Generate(s);
        }

        [Test]
        public void EveryPieceIsInExactlyOneStretch()
        {
            var l = AnyLong();
            var c = new StageChunks(l.Pieces, l.Length);
            var all = Enumerable.Range(0, c.Count).SelectMany(i => c.PiecesIn(i)).ToList();
            Assert.That(all.Count, Is.EqualTo(l.Pieces.Count));
            Assert.That(all.Distinct().Count(), Is.EqualTo(l.Pieces.Count));
        }

        [Test]
        public void PuzzlePartsGoWithTheirGateAndAfterIt()
        {
            var l = AnyLong();
            var c = new StageChunks(l.Pieces, l.Length);
            for (int i = 0; i < c.Count; i++)
            {
                var list = c.PiecesIn(i).Select(k => l.Pieces[k]).ToList();
                foreach (var part in list.Where(p => p.kind == PieceKind.PuzzlePart))
                {
                    int gate = list.FindIndex(p => p.kind == PieceKind.PuzzleGate && (int)p.sizeB == (int)part.sizeB);
                    Assert.That(gate, Is.GreaterThanOrEqualTo(0), part.ToString());
                    Assert.That(gate, Is.LessThan(list.IndexOf(part)));
                }
            }
        }

        [Test]
        public void OnlyTheStretchAroundThePlayerStands()
        {
            Assert.That(StageChunks.Keep(0, 1f, 1f), Is.True);
            Assert.That(StageChunks.Keep(10, 1f, 1f), Is.False, "400 m ahead");
            Assert.That(StageChunks.Keep(0, 500f, 500f), Is.False, "far behind");
            Assert.That(StageChunks.Keep(0, 500f, 30f), Is.True, "the checkpoint is still back there");
        }

        [Test]
        public void ALongStageNeverHasMuchStanding()
        {
            var l = AnyLong();
            var c = new StageChunks(l.Pieces, l.Length);
            int most = 0;
            for (float z = 0f; z < l.Length; z += 10f)
                most = System.Math.Max(most, Enumerable.Range(0, c.Count).Count(i => StageChunks.Keep(i, z, z)));
            Assert.That(c.Count, Is.GreaterThan(most * 2), "streaming should matter on a long stage");
            Assert.That(most, Is.LessThanOrEqualTo(8));
        }

        [Test]
        public void WalkingBackDoesNotRefillPickups()
        {
            Assert.That(StageChunks.Rebuilt(PieceKind.Ground), Is.True);
            foreach (var k in new[] { PieceKind.Coin, PieceKind.CrateBasic, PieceKind.CrateBisht, PieceKind.Checkpoint, PieceKind.Patrol, PieceKind.PuzzleGate })
                Assert.That(StageChunks.Rebuilt(k), Is.False, k.ToString());
        }
    }
}
