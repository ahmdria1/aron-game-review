// (c) Ahmed Alamrani. All rights reserved.
using System.Collections.Generic;
using System.Linq;
using Aron.Logic.Combat;
using NUnit.Framework;

namespace Aron.Logic.Tests
{
    public class CombatFeelTests
    {
        private static CombatFeelFile File() => TestData.Read<CombatFeelFile>("Config", "combat_feel.json");
        private static AimCandidate C(string id, TargetKind k, float x, float y, float dist = 5f, float angle = 0f) => new AimCandidate { id = id, kind = k, screenX = x, screenY = y, distance = dist, angle = angle };

        [Test] public void TheShippedFileIsValid() => Assert.That(File().Validate(), Is.Empty);

        [Test]
        public void ASlingTapNearAnEnemyLandsOnIt()
        {
            var f = File().aim; var list = new List<AimCandidate> { C("e", TargetKind.Enemy, 500, 500) };
            Assert.That(AimAssist.PickSling(list, 520, 510, 160f, f, 1f), Is.EqualTo(0));
            Assert.That(AimAssist.PickSling(list, 900, 900, 160f, f, 1f), Is.EqualTo(-1), "a tap far away is a plain tap");
        }

        [Test]
        public void AnimalsBirdsCompanionsAndHeroesAreNeverPicked()
        {
            var f = File().aim;
            foreach (var k in new[] { TargetKind.Animal, TargetKind.Bird, TargetKind.Companion, TargetKind.Hero, TargetKind.Civilian })
            {
                var list = new List<AimCandidate> { C("x", k, 500, 500, 1f, 0f) };
                Assert.That(AimAssist.PickSling(list, 500, 500, 160f, f, 2f), Is.EqualTo(-1), k + " sling");
                Assert.That(AimAssist.PickMelee(list, f, 2f), Is.EqualTo(-1), k + " melee");
                Assert.That(AimAssist.Targetable(k), Is.False, k.ToString());
            }
        }

        [Test]
        public void AnAnimalBesideAnEnemyDoesNotStealOrBlockTheAim()
        {
            var f = File().aim;
            var list = new List<AimCandidate> { C("bird", TargetKind.Bird, 500, 500), C("e", TargetKind.Enemy, 530, 500) };
            Assert.That(AimAssist.PickSling(list, 505, 500, 160f, f, 1f), Is.EqualTo(1), "the enemy is still the target and the bird is left alone");
        }

        [Test]
        public void TheCloserOfTwoEnemiesWinsAndABossBeatsAnEqualEnemy()
        {
            var f = File().aim;
            var two = new List<AimCandidate> { C("far", TargetKind.Enemy, 540, 500), C("near", TargetKind.Enemy, 510, 500) };
            Assert.That(AimAssist.PickSling(two, 505, 500, 160f, f, 1f), Is.EqualTo(1));
            var tie = new List<AimCandidate> { C("e", TargetKind.Enemy, 520, 500), C("b", TargetKind.Boss, 520, 500) };
            Assert.That(AimAssist.PickSling(tie, 505, 500, 160f, f, 1f), Is.EqualTo(1));
        }

        [Test]
        public void TheAssistIsLightAndScalesWithDifficultyAndOneHandMode()
        {
            var f = File().aim;
            Assert.That(f.Scale("easy"), Is.GreaterThan(f.Scale("normal"))); Assert.That(f.Scale("hard"), Is.LessThan(f.Scale("normal"))); Assert.That(f.Scale("hard"), Is.GreaterThan(0f));
            var list = new List<AimCandidate> { C("e", TargetKind.Enemy, 500, 500) };
            // a tap 60 px off at 160 dpi: only the stronger settings reach it
            Assert.That(AimAssist.PickSling(list, 560, 500, 160f, f, f.Scale("hard")), Is.EqualTo(-1));
            Assert.That(AimAssist.PickSling(list, 560, 500, 160f, f, f.Scale("easy") * 1.5f), Is.EqualTo(0), "easy with one-hand mode reaches further");
            Assert.That(AimAssist.PickSling(list, 505, 500, 160f, f, 0f), Is.EqualTo(-1), "off means off");
            Assert.That(AimAssist.PickSling(list, 505, 500, 160f, f, f.Scale("normal")), Is.EqualTo(0));
        }

        [Test]
        public void TheRadiusFollowsTheScreenDensityNotThePixels()
        {
            var f = File().aim; var list = new List<AimCandidate> { C("e", TargetKind.Enemy, 500, 500) };
            Assert.That(AimAssist.PickSling(list, 580, 500, 160f, f, 1f), Is.EqualTo(-1));
            Assert.That(AimAssist.PickSling(list, 580, 500, 480f, f, 1f), Is.EqualTo(0), "a dense screen has more pixels per finger");
        }

        [Test]
        public void SlingTargetsBeyondRangeAreIgnored()
        {
            var f = File().aim; var list = new List<AimCandidate> { C("e", TargetKind.Enemy, 500, 500, f.slingMaxRange + 1f) };
            Assert.That(AimAssist.PickSling(list, 500, 500, 160f, f, 1f), Is.EqualTo(-1));
        }

        [Test]
        public void MeleeTurnsTowardTheNearestEnemyInFrontButNotBehind()
        {
            var f = File().aim;
            var list = new List<AimCandidate> { C("behind", TargetKind.Enemy, 0, 0, 1.0f, 150f), C("front", TargetKind.Enemy, 0, 0, 2.0f, 10f) };
            Assert.That(AimAssist.PickMelee(list, f, 1f), Is.EqualTo(1));
            Assert.That(AimAssist.PickMelee(new List<AimCandidate> { C("far", TargetKind.Enemy, 0, 0, 6f, 0f) }, f, 1f), Is.EqualTo(-1), "out of reach");
        }

        [Test]
        public void TheSnapIsASmallNudgeAndWrapsAroundNorth()
        {
            var f = File().aim;
            Assert.That(AimAssist.SnapYaw(10f, 30f, f, 1f), Is.EqualTo(30f));
            Assert.That(AimAssist.SnapYaw(10f, 120f, f, 1f), Is.EqualTo(10f), "too far off: the player meant something else");
            Assert.That(AimAssist.SnapYaw(350f, 10f, f, 1f), Is.EqualTo(370f).Within(0.01f), "20 degrees across north");
        }

        [Test]
        public void HitStopIsSixtyToEightyMsOnTheFinalHitAndPhaseBreaks()
        {
            var f = File().hitStop; Assert.That(f.finalHitMs, Is.InRange(60f, 80f)); Assert.That(f.phaseBreakMs, Is.InRange(60f, 80f));
            var h = new HitStop(f);
            var r = h.Trigger(HitStop.FinalHit, 10.0, false, false);
            Assert.That(r.seconds, Is.InRange(0.06f, 0.08f)); Assert.That(r.haptic, Is.True);
            Assert.That(h.Trigger(HitStop.PhaseBreak, 11.0, false, false).seconds, Is.InRange(0.06f, 0.08f));
            Assert.That(h.Trigger("ordinary_hit", 12.0, false, false).seconds, Is.EqualTo(0f), "ordinary hits do not freeze");
        }

        [Test]
        public void NoSecondFreezeInsideTheCooldownNoneWhilePausedAndReduceMotionKeepsOnlyTheHaptic()
        {
            var h = new HitStop(File().hitStop);
            Assert.That(h.Trigger(HitStop.FinalHit, 5.0, false, false).seconds, Is.GreaterThan(0f));
            Assert.That(h.Trigger(HitStop.PhaseBreak, 5.1, false, false).seconds, Is.EqualTo(0f));
            Assert.That(h.Trigger(HitStop.PhaseBreak, 6.0, true, false).seconds, Is.EqualTo(0f), "paused");
            var rm = h.Trigger(HitStop.PhaseBreak, 7.0, false, true);
            Assert.That(rm.seconds, Is.EqualTo(0f)); Assert.That(rm.haptic, Is.True);
        }

        [Test]
        public void TheHitStopHapticExistsAndTheValidatorCatchesBadValues()
        {
            Assert.That(TestData.Read<Aron.Logic.Feedback.HapticFile>("Config", "haptics.json").For(HitStop.Haptic), Is.Not.Null);
            Assert.That(new HitStopFile { finalHitMs = 200f }.Validate(), Is.Not.Empty);
            Assert.That(new AimAssistFile { easyScale = 5f }.Validate(), Is.Not.Empty);
            Assert.That(new Aron.Logic.Safety.AccessSettings().aimAssist, Is.True);
        }
    }
}
