// (c) Ahmed Alamrani. All rights reserved.
using System;
using System.Linq;
using Aron.Logic;
using Aron.Logic.GameFeel;
using NUnit.Framework;

namespace Aron.Logic.Tests
{
    public class GameFeelTests
    {
        private static FeelValues V() => new FeelValues();

        // ------------------------------------------------------------ the values file
        [Test]
        public void TheShippedJsonMatchesTheCodeDefaultsAndIsValid()
        {
            var shipped = TestData.Read<FeelValues>("Config", "game_feel.json");
            var code = V();
            foreach (var f in FeelValues.Fields) Assert.That(shipped.Get(f), Is.EqualTo(code.Get(f)).Within(1e-6f), f.Name);
            Assert.That(code.Validate(), Is.Empty);
            Assert.That(FeelValues.Fields.Length, Is.GreaterThan(100));
        }

        [Test]
        public void EveryValueIsOnTheTuningPanelWithASaneRangeAndItsDefaultInsideIt()
        {
            var v = V();
            foreach (var f in FeelValues.Fields)
            {
                var a = FeelValues.Info(f);
                Assert.That(a.Max, Is.GreaterThan(a.Min), f.Name);
                Assert.That(v.Get(f), Is.InRange(a.Min, a.Max), f.Name);
                Assert.That(string.IsNullOrEmpty(a.Group), Is.False, f.Name);
            }
            // a public field with no [Feel] would be a value nobody can tune on the phone
            var all = typeof(FeelValues).GetFields().Where(x => !x.IsStatic).ToArray();
            Assert.That(all.Length, Is.EqualTo(FeelValues.Fields.Length));
            Assert.That(FeelValues.Groups(), Does.Contain("Jump").And.Contain("Camera").And.Contain("Input").And.Contain("Anim cloth"));
        }

        [Test]
        public void SettingAValueClampsItAndIntegersRound()
        {
            var v = V();
            var jump = FeelValues.Fields.First(f => f.Name == "jumpHeight");
            v.Set(jump, 99f); Assert.That(v.jumpHeight, Is.EqualTo(FeelValues.Info(jump).Max));
            var spin = FeelValues.Fields.First(f => f.Name == "spinActiveFrames");
            v.Set(spin, 11.6f); Assert.That(v.spinActiveFrames, Is.EqualTo(12));
            v.runSpeed = 400f; v.Clamp(); Assert.That(v.runSpeed, Is.LessThanOrEqualTo(14f));
            var c = v.Clone(); c.runSpeed = 3f; Assert.That(v.runSpeed, Is.Not.EqualTo(3f));
        }

        [Test]
        public void ContradictorySettingsAreCaught()
        {
            var v = V(); v.crateBounceHeldHeight = 2f; Assert.That(v.Validate(), Is.Not.Empty);
            v = V(); v.spinJumpCancelFrame = 50; Assert.That(v.Validate(), Is.Not.Empty);
            v = V(); v.stickDeadZone = 0.4f; v.stickOuterZone = 0.6f; Assert.That(v.Validate(), Is.Not.Empty);
        }

        // ------------------------------------------------------------ jump arc
        [Test]
        public void ASolvedTakeOffSpeedPeaksAtExactlyTheRequestedHeight()
        {
            var v = V();
            foreach (float h in new[] { 1.0f, 2.2f, 3.5f })
            {
                float v0 = JumpArc.VelocityForHeight(v, h);
                Assert.That(JumpArc.PeakHeight(v, v0), Is.EqualTo(h).Within(0.01f), "height " + h);
            }
            // with plain gravity the answer is the textbook sqrt(2 g h)
            var plain = V(); plain.riseGravityMult = 1f; plain.apexGravityMult = 1f; plain.fallGravityMult = 1f;
            Assert.That(JumpArc.VelocityForHeight(plain, 2.2f), Is.EqualTo((float)Math.Sqrt(2 * plain.gravity * 2.2f)).Within(0.2f));
        }

        [Test]
        public void ReleasingTheButtonCutsTheJumpShortButNeverBelowASmallHop()
        {
            var v = V();
            float v0 = JumpArc.VelocityForHeight(v, v.jumpHeight);
            float PeakWithRelease(float releaseAt)
            {
                var s = JumpArc.Launch(v0); float y = 0, peak = 0; float t = 0;
                for (int i = 0; i < 4000 && (s.VelocityY > 0f || i == 0); i++)
                {
                    s = JumpArc.Step(v, s, JumpArc.SubStep, t < releaseAt); t += JumpArc.SubStep; y += s.VelocityY * JumpArc.SubStep; peak = Math.Max(peak, y);
                }
                return peak;
            }
            float tap = PeakWithRelease(0f), held = PeakWithRelease(10f);
            Assert.That(held, Is.EqualTo(v.jumpHeight).Within(0.02f));
            Assert.That(tap, Is.LessThan(held * 0.6f), "a tap is a short hop");
            Assert.That(tap, Is.GreaterThan(held * 0.15f), "and still a hop, not a twitch");
            Assert.That(PeakWithRelease(0.15f), Is.InRange(tap, held));
        }

        [Test]
        public void TheApexHangsAndTheFallIsFasterThanTheRise()
        {
            var v = V();
            float v0 = JumpArc.VelocityForHeight(v, v.jumpHeight);
            var noHang = V(); noHang.apexGravityMult = 1f;
            Assert.That(JumpArc.ApexHang(v, v0), Is.GreaterThan(JumpArc.ApexHang(noHang, JumpArc.VelocityForHeight(noHang, v.jumpHeight))), "the apex gravity patch adds hang time");
            // rise time vs fall time over the same height
            float rise = JumpArc.TimeToApex(v, v0);
            var s = JumpArc.Launch(0f); float y = v.jumpHeight, fall = 0;
            while (y > 0f && fall < 5f) { s = JumpArc.Step(v, s, JumpArc.SubStep, true); y += s.VelocityY * JumpArc.SubStep; fall += JumpArc.SubStep; }
            Assert.That(fall, Is.LessThan(rise * 1.05f + 0.15f));
            Assert.That(JumpArc.GravityFor(v, new JumpState { VelocityY = -9f }, true), Is.GreaterThan(JumpArc.GravityFor(v, new JumpState { VelocityY = 9f }, true)));
        }

        [Test]
        public void FastFallIsHeavierAndTheFallSpeedIsCapped()
        {
            var v = V();
            var s = JumpArc.Launch(-5f); var f = JumpArc.Launch(-5f);
            for (int i = 0; i < 20; i++) { s = JumpArc.Step(v, s, 1f / 60f, false); f = JumpArc.Step(v, f, 1f / 60f, false, true); }
            Assert.That(f.VelocityY, Is.LessThan(s.VelocityY));
            Assert.That(f.FastFall, Is.True); Assert.That(s.FastFall, Is.False);
            for (int i = 0; i < 600; i++) { s = JumpArc.Step(v, s, 1f / 60f, false); f = JumpArc.Step(v, f, 1f / 60f, false, true); }
            Assert.That(s.VelocityY, Is.EqualTo(-v.maxFallSpeed).Within(1e-4f));
            Assert.That(f.VelocityY, Is.EqualTo(-v.fastFallMaxSpeed).Within(1e-4f));
            // a rising hero is never fast-fall latched
            Assert.That(JumpArc.Step(v, JumpArc.Launch(8f), 1f / 60f, true, true).FastFall, Is.False);
        }

        [Test]
        public void TheArcDoesNotChangeWithTheFrameRate()
        {
            var v = V(); float v0 = JumpArc.VelocityForHeight(v, v.jumpHeight);
            // whole frames, the way the controller plays them: the distance is summed over the sub-steps, so the peak is the same on every phone
            float Peak(int fps)
            {
                float dt = 1f / fps, y = 0f, peak = 0f; var s = JumpArc.Launch(v0);
                for (int i = 0; i < fps * 4 && (s.VelocityY > 0f || i == 0); i++)
                {
                    s = JumpArc.Advance(v, s, dt, true, false, out float dy);
                    y += dy; peak = Math.Max(peak, y);
                }
                return peak;
            }
            foreach (int fps in new[] { 20, 30, 45, 60, 90, 120, 144 })
                Assert.That(Peak(fps), Is.EqualTo(v.jumpHeight).Within(0.03f), fps + " fps");
            Assert.That(Peak(240), Is.EqualTo(v.jumpHeight).Within(0.06f), "a 240 Hz display takes smaller steps than the arc was solved with");
        }

        // ------------------------------------------------------------ buffers
        [Test]
        public void JumpBufferHoldsAPressForItsWindowAndIsConsumedOnce()
        {
            var v = V(); var b = new InputBuffers();
            b.PressJump(10.0);
            Assert.That(b.JumpBuffered(10.0 + v.jumpBuffer - 0.001, v.jumpBuffer), Is.True);
            Assert.That(b.JumpBuffered(10.0 + v.jumpBuffer + 0.02, v.jumpBuffer), Is.False);
            b.ConsumeJump();
            Assert.That(b.JumpBuffered(10.01, v.jumpBuffer), Is.False, "consumed");
        }

        [Test]
        public void CoyoteTimeAllowsAGroundJumpJustAfterWalkingOffAndNotLater()
        {
            var v = V(); var b = new InputBuffers();
            b.Grounded(5.0);
            Assert.That(b.CoyoteOpen(5.0 + v.coyoteTime - 0.001, v.coyoteTime), Is.True);
            Assert.That(b.CoyoteOpen(5.0 + v.coyoteTime + 0.03, v.coyoteTime), Is.False);
            b.Grounded(7.0); b.Left();
            Assert.That(b.CoyoteOpen(7.01, v.coyoteTime), Is.False, "a jump uses the coyote window up");
        }

        [Test]
        public void OneHandModeWidensBothWindows()
        {
            var v = V();
            Assert.That(InputBuffers.JumpWindow(v, true), Is.GreaterThan(InputBuffers.JumpWindow(v, false)));
            Assert.That(InputBuffers.CoyoteWindow(v, true), Is.GreaterThan(InputBuffers.CoyoteWindow(v, false)));
        }

        [Test]
        public void TheFixedStepperGivesTheSameStepCountAtAnyFrameRate()
        {
            foreach (float fps in new[] { 30f, 60f, 90f, 120f })
            {
                var st = new FixedStepper(); int steps = 0;
                for (int i = 0; i < (int)fps * 2; i++) steps += st.Advance(1f / fps);
                Assert.That(steps, Is.InRange(118, 120), fps + " fps for 2 s");
            }
            var hitch = new FixedStepper();
            Assert.That(hitch.Advance(0.5f), Is.EqualTo(FixedStepper.MaxStepsPerFrame), "a hitch cannot fast-forward a swing");
            Assert.That(hitch.Advance(0f), Is.EqualTo(0));
        }

        // ------------------------------------------------------------ corner correction, ledge forgiveness
        [Test]
        public void HeadCornerCorrectionSlidesAroundShallowCornersOnly()
        {
            var v = V();
            Assert.That(CornerCorrection.Nudge(v, 0.1f), Is.EqualTo(0.12f).Within(1e-5f));
            Assert.That(CornerCorrection.Nudge(v, v.headCornerNudge), Is.EqualTo(0f), "any deeper than the limit is a real ceiling");
            Assert.That(CornerCorrection.Nudge(v, v.headCornerNudge - 0.05f), Is.GreaterThan(0f));
            Assert.That(CornerCorrection.Nudge(v, 0.1f, 0.02f, -2f), Is.EqualTo(0f), "only while rising");
            Assert.That(CornerCorrection.Nudge(v, -0.1f), Is.EqualTo(0f));
            Assert.That(CornerCorrection.AfterBonk(v, 6f), Is.EqualTo(-v.headBonkDownSpeed));
            Assert.That(CornerCorrection.AfterBonk(v, -3f), Is.EqualTo(-3f));
        }

        [Test]
        public void LedgeForgivenessLiftsFeetThatCatchTheEdgeAndHasItsOwnCooldownApartFromCoyote()
        {
            var v = V();
            Assert.That(LedgeForgiveness.Lift(v, 1.0f, 1.2f, -2f, 1f), Is.EqualTo(0.22f).Within(1e-5f));
            Assert.That(LedgeForgiveness.Lift(v, 1.0f, 1.0f + v.ledgeForgiveHeight + 0.05f, -2f, 1f), Is.EqualTo(0f), "too high");
            Assert.That(LedgeForgiveness.Lift(v, 1.0f, 1.2f, v.ledgeForgiveMaxRise + 1f, 1f), Is.EqualTo(0f), "still shooting upward");
            Assert.That(LedgeForgiveness.Lift(v, 1.0f, 1.2f, -2f, v.ledgeForgiveCooldown - 0.01f), Is.EqualTo(0f), "cooldown");
            Assert.That(LedgeForgiveness.Lift(v, 1.3f, 1.2f, -2f, 1f), Is.EqualTo(0f), "feet already above");
            Assert.That(LedgeForgiveness.KeepSpeed(v, 6f), Is.EqualTo(6f * v.ledgeForgiveKeepSpeed));
        }

        // ------------------------------------------------------------ frame windows
        [Test]
        public void MeleeFramesHaveTheStartupActiveRecoverySplit()
        {
            var v = V(); var f = AttackFrames.Melee(v);
            Assert.That(f.Total, Is.EqualTo(24));
            Assert.That(f.PhaseAt(0), Is.EqualTo(AttackPhase.Startup));
            Assert.That(f.PhaseAt(v.meleeStartupFrames - 1), Is.EqualTo(AttackPhase.Startup));
            Assert.That(f.PhaseAt(v.meleeStartupFrames), Is.EqualTo(AttackPhase.Active));
            Assert.That(f.PhaseAt(v.meleeStartupFrames + v.meleeActiveFrames - 1), Is.EqualTo(AttackPhase.Active));
            Assert.That(f.PhaseAt(v.meleeStartupFrames + v.meleeActiveFrames), Is.EqualTo(AttackPhase.Recovery));
            Assert.That(f.PhaseAt(f.Total - 1), Is.EqualTo(AttackPhase.Recovery));
            Assert.That(f.PhaseAt(f.Total), Is.EqualTo(AttackPhase.None)); Assert.That(f.PhaseAt(-1), Is.EqualTo(AttackPhase.None));
        }

        [Test]
        public void JumpCancelOpensAtTheConfiguredFrameAndNeverDuringStartup()
        {
            var v = V(); var f = AttackFrames.Spin(v);
            Assert.That(f.CanJumpCancel(v.spinJumpCancelFrame - 1), Is.False);
            Assert.That(f.CanJumpCancel(v.spinJumpCancelFrame), Is.True);
            Assert.That(f.CanJumpCancel(f.Total - 1), Is.True);
            Assert.That(f.CanJumpCancel(f.Total), Is.False);
            var early = f; early.JumpCancelFrame = 0;
            Assert.That(early.CanJumpCancel(0), Is.False, "the swing has to commit first");
            Assert.That(early.CanJumpCancel(early.Startup), Is.True);
        }

        [Test]
        public void AFrameTimelineRunsForExactlyTheTotalFramesAndThenEnds()
        {
            var v = V(); var t = new AttackTimeline(); t.Start(AttackFrames.Melee(v));
            int ticks = 0; bool ended = false;
            while (t.Running && ticks < 100) { ended = t.Tick(); ticks++; }
            Assert.That(ticks, Is.EqualTo(24)); Assert.That(ended, Is.True); Assert.That(t.Phase, Is.EqualTo(AttackPhase.None));
        }

        [Test]
        public void HitStopFreezesOnlyTheAttackerForTheConfiguredFramesAndOnlyOnce()
        {
            var v = V(); var t = new AttackTimeline(); t.Start(AttackFrames.Melee(v));
            for (int i = 0; i < 6; i++) t.Tick();
            int frame = t.Frame;
            t.Hit(v.hitStopFrames); t.Hit(1);
            for (int i = 0; i < v.hitStopFrames; i++) { t.Tick(); Assert.That(t.Frame, Is.EqualTo(frame), "frozen"); }
            t.Tick(); Assert.That(t.Frame, Is.EqualTo(frame + 1));
            // a longer freeze wins, a shorter one does not shorten it
            t.Hit(5); t.Hit(2); Assert.That(t.FreezeLeft, Is.EqualTo(5));
        }

        [Test]
        public void AnAttackPressedNearTheEndIsBufferedIntoTheNextSwing()
        {
            var v = V(); var t = new AttackTimeline(); t.Start(AttackFrames.Melee(v));
            t.PressAttack(v.meleeBufferFrames); Assert.That(t.BufferedNext, Is.False, "too early to count");
            for (int i = 0; i < 24 - v.meleeBufferFrames; i++) t.Tick();
            t.PressAttack(v.meleeBufferFrames); Assert.That(t.BufferedNext, Is.True);
            t.Cancel(); Assert.That(t.BufferedNext, Is.False); Assert.That(t.Running, Is.False);
        }

        [Test]
        public void AJumpCancelsTheTimelineFromItsCancelFrame()
        {
            var v = V(); var t = new AttackTimeline(); t.Start(AttackFrames.Melee(v));
            for (int i = 0; i < v.meleeJumpCancelFrame; i++) { Assert.That(t.CanJumpCancel, Is.False, "frame " + i); t.Tick(); }
            Assert.That(t.CanJumpCancel, Is.True);
        }

        [Test]
        public void WeaponTimesInSecondsConvertToFrames()
        {
            var f = AttackFrames.FromSeconds(0.08f, 0.25f, 0.4f);
            Assert.That(f.Startup, Is.EqualTo(5)); Assert.That(f.Active, Is.EqualTo(10)); Assert.That(f.Total, Is.EqualTo(24));
        }

        // ------------------------------------------------------------ landing
        [Test]
        public void LandingFeelGrowsWithTheFallAndSoftStepsAreSilent()
        {
            var v = V();
            Assert.That(LandingFeel.From(v, 0.3f).DustBursts, Is.EqualTo(0));
            Assert.That(LandingFeel.From(v, 0.3f).Squash, Is.EqualTo(0f));
            var small = LandingFeel.From(v, 1f); var big = LandingFeel.From(v, 7f);
            Assert.That(big.Squash, Is.GreaterThan(small.Squash)); Assert.That(big.Squash, Is.LessThanOrEqualTo(v.landSquashMax + 1e-5f));
            Assert.That(small.Lag, Is.False); Assert.That(big.Lag, Is.True); Assert.That(big.Shake, Is.True); Assert.That(small.Shake, Is.False);
        }

        [Test]
        public void LandLagIsShortSlowsMovementAndIsCancelledByJumpAtOnceAndByMovementLater()
        {
            var v = V();
            Assert.That(LandLag.For(v, 1f).Active, Is.False, "a small drop has no lag");
            var lag = LandLag.For(v, 5f);
            Assert.That(lag.Active, Is.True); Assert.That(lag.SpeedMult(v), Is.EqualTo(v.landLagSpeedMult));
            Assert.That(lag.CanCancel(v, true, false), Is.True, "a buffered jump cancels from frame 0");
            Assert.That(lag.CanCancel(v, false, true), Is.False, "moving cannot cancel at once");
            for (int i = 0; i < v.landLagMoveCancelFrame; i++) lag = lag.Tick();
            Assert.That(lag.CanCancel(v, false, true), Is.True);
            for (int i = 0; i < 30; i++) lag = lag.Tick();
            Assert.That(lag.Active, Is.False); Assert.That(lag.SpeedMult(v), Is.EqualTo(1f));
            Assert.That(v.landLagFrames / 60f, Is.LessThan(0.25f), "short enough not to feel sticky");
        }

        // ------------------------------------------------------------ bounce
        [Test]
        public void CrateBounceHeightIsIndependentOfFallSpeedAndHoldingJumpGivesTheHighOne()
        {
            var v = V();
            float plain = BounceMath.Height(v, v.crateBounceHeight, false, 5f);
            Assert.That(plain, Is.EqualTo(v.crateBounceHeight));
            Assert.That(BounceMath.Height(v, v.crateBounceHeight, true, 5f), Is.EqualTo(v.crateBounceHeldHeight).Within(1e-5f));
            Assert.That(BounceMath.Height(v, v.crateBounceHeight, false, v.bounceBufferBefore - 0.01f), Is.EqualTo(v.crateBounceHeldHeight).Within(1e-5f), "pressed just before impact: buffered");
            Assert.That(BounceMath.Height(v, v.crateBounceHeight, false, v.bounceBufferBefore + 0.05f), Is.EqualTo(plain));
            Assert.That(BounceMath.Height(v, v.stompBounceHeight, true, 5f), Is.GreaterThan(v.stompBounceHeight));
            Assert.That(BounceMath.Velocity(v, v.crateBounceHeight), Is.EqualTo((float)Math.Sqrt(2 * v.gravity * v.crateBounceHeight)).Within(1e-4f));
            Assert.That(BounceMath.LateBoostAllowed(v, v.bounceBufferAfter - 0.01f, false), Is.True);
            Assert.That(BounceMath.LateBoostAllowed(v, v.bounceBufferAfter + 0.01f, false), Is.False);
            Assert.That(BounceMath.LateBoostAllowed(v, 0.01f, true), Is.False);
        }

        // ------------------------------------------------------------ slopes
        [Test]
        public void SlopesSlowTheClimbSpeedTheDescentAndSlideOnlyWhenTooSteep()
        {
            var v = V();
            float flatNormal = 1f, steepWalkable = (float)Math.Cos(45 * Math.PI / 180), tooSteep = (float)Math.Cos(65 * Math.PI / 180);
            Assert.That(Slopes.SpeedScale(v, flatNormal, 1f), Is.EqualTo(1f));
            Assert.That(Slopes.SpeedScale(v, steepWalkable, -1f), Is.LessThan(1f));
            Assert.That(Slopes.SpeedScale(v, steepWalkable, 1f), Is.GreaterThan(1f));
            Assert.That(Slopes.SpeedScale(v, steepWalkable, 0f), Is.EqualTo(1f), "across the slope is untouched");
            Assert.That(Slopes.SpeedScale(v, tooSteep, -1f), Is.GreaterThanOrEqualTo(1f - v.slopeUphillSlow - 1e-4f));
            Assert.That(Slopes.TooSteep(v, steepWalkable), Is.False); Assert.That(Slopes.TooSteep(v, tooSteep), Is.True);
            Assert.That(Slopes.SlideSpeed(v, steepWalkable), Is.EqualTo(0f)); Assert.That(Slopes.SlideSpeed(v, tooSteep), Is.GreaterThan(0f));
            Assert.That(Slopes.SlideSpeed(v, 0.1f), Is.GreaterThan(Slopes.SlideSpeed(v, tooSteep)));
            Assert.That(Slopes.CanStepUp(v, v.stepUp), Is.True); Assert.That(Slopes.CanStepUp(v, v.stepUp + 0.1f), Is.False);
            Assert.That(Slopes.AngleDegrees(1f), Is.EqualTo(0f).Within(1e-4f));
        }

        // ------------------------------------------------------------ locomotion
        private static RunState Run(FeelValues v, V2 desired, float seconds, RunState s = null, float traction = 1f)
        {
            s = s ?? new RunState();
            for (float t = 0; t < seconds; t += 1f / 60f) Locomotion.StepGround(v, s, desired, 1f / 60f, v.runSpeed, traction);
            return s;
        }

        [Test]
        public void TheRunReachesTopSpeedQuicklyAndStopsFasterThanItStarted()
        {
            var v = V(); var top = new V2(0f, v.runSpeed);
            var s = new RunState(); float tFull = -1;
            for (int i = 0; i < 120; i++) { Locomotion.StepGround(v, s, top, 1f / 60f, v.runSpeed); if (tFull < 0 && s.Speed >= v.runSpeed - 0.01f) tFull = i / 60f; }
            Assert.That(tFull, Is.InRange(0.12f, 0.45f), "full speed in a quarter of a second or so");
            Assert.That(s.Phase, Is.EqualTo(RunPhase.Cruising));
            float tStop = -1;
            for (int i = 0; i < 120; i++) { Locomotion.StepGround(v, s, new V2(0, 0), 1f / 60f, v.runSpeed); if (s.Speed < 0.01f) { tStop = (i + 1) / 60f; break; } }
            Assert.That(tStop, Is.GreaterThan(0f).And.LessThan(tFull), "braking is quicker than accelerating");
            Assert.That(s.Phase, Is.EqualTo(RunPhase.Idle));
        }

        [Test]
        public void TheAccelerationCurveStartsStrongAndEasesTowardTopSpeed()
        {
            var v = V();
            Assert.That(Locomotion.AccelAt(v, 0f), Is.EqualTo(v.accelGround).Within(1e-4f));
            Assert.That(Locomotion.AccelAt(v, 1f), Is.EqualTo(v.accelGround * v.accelTopShare).Within(1e-4f));
            float prev = float.MaxValue;
            for (int i = 0; i <= 10; i++) { float a = Locomotion.AccelAt(v, i / 10f); Assert.That(a, Is.LessThanOrEqualTo(prev + 1e-5f)); prev = a; }
            Assert.That(Locomotion.AccelAt(v, 0.5f, 0.5f), Is.LessThan(Locomotion.AccelAt(v, 0.5f)), "wet ground has less grip");
        }

        [Test]
        public void AReversalAtSpeedSkidsAndAReversalAtAWalkTurnsInPlace()
        {
            var v = V();
            var fast = Run(v, new V2(0f, v.runSpeed), 1f);
            Locomotion.StepGround(v, fast, new V2(0f, -v.runSpeed), 1f / 60f, v.runSpeed);
            Assert.That(fast.Phase, Is.EqualTo(RunPhase.Skidding));
            // he keeps sliding forward while skidding, then ends up facing the new way
            Assert.That(fast.Velocity.Z, Is.GreaterThan(0f));
            int n = 0; while (fast.Phase == RunPhase.Skidding && n++ < 200) Locomotion.StepGround(v, fast, new V2(0f, -v.runSpeed), 1f / 60f, v.runSpeed);
            Assert.That(n / 60f, Is.LessThanOrEqualTo(v.skidMaxTime + 0.05f));
            Assert.That(Locomotion.TurnRate(v, RunPhase.Skidding, true), Is.EqualTo(0f), "no turning mid-skid");
            Run(v, new V2(0f, -v.runSpeed), 1f, fast);
            Assert.That(fast.Velocity.Z, Is.LessThan(-v.runSpeed * 0.95f), "and then runs the other way");

            var slow = new RunState { Velocity = new V2(0f, 1.0f), Phase = RunPhase.Accelerating };
            Locomotion.StepGround(v, slow, new V2(0f, -v.runSpeed), 1f / 60f, v.runSpeed);
            Assert.That(slow.Phase, Is.EqualTo(RunPhase.TurningInPlace));
            Run(v, new V2(0f, -v.runSpeed), 0.1f, slow);
            Assert.That(slow.Velocity.Z, Is.LessThan(0f), "turned within a tenth of a second");
        }

        [Test]
        public void AGentleDirectionChangeIsNeitherASkidNorAStall()
        {
            var v = V(); var s = Run(v, new V2(0f, v.runSpeed), 1f);
            Assert.That(Locomotion.IsSharpReversal(v, s.Velocity, new V2(v.runSpeed, 0f)), Is.False, "90 degrees");
            Assert.That(Locomotion.IsSharpReversal(v, s.Velocity, new V2(0.2f, -v.runSpeed)), Is.True);
            Assert.That(Locomotion.IsSharpReversal(v, new V2(0f, 2f), new V2(0f, -5f)), Is.False, "too slow to skid");
        }

        [Test]
        public void WetGroundLengthensTheSkidAndTheStop()
        {
            var v = V();
            float Stop(float traction)
            {
                var s = Run(v, new V2(0f, v.runSpeed), 1f, null, traction); float t = 0;
                while (s.Speed > 0.01f && t < 3f) { Locomotion.StepGround(v, s, new V2(0, 0), 1f / 60f, v.runSpeed, traction); t += 1f / 60f; }
                return t;
            }
            Assert.That(Stop(0.5f), Is.GreaterThan(Stop(1f)));
        }

        [Test]
        public void AirControlIsSeparateWeakerThanTheGroundAndBitesHarderOnAReversal()
        {
            var v = V();
            var air = new RunState(); var gnd = new RunState();
            for (int i = 0; i < 6; i++) { Locomotion.StepAir(v, air, new V2(0f, v.runSpeed), 1f / 60f, false); Locomotion.StepGround(v, gnd, new V2(0f, v.runSpeed), 1f / 60f, v.runSpeed); }
            Assert.That(air.Speed, Is.LessThan(gnd.Speed), "less control in the air");
            Assert.That(air.Speed, Is.GreaterThan(0f), "but real control");
            var a = new RunState { Velocity = new V2(0f, 5f) }; var b = new RunState { Velocity = new V2(0f, 5f) };
            Locomotion.StepAir(v, a, new V2(0f, 0f), 0.1f, false);                    // let go
            Locomotion.StepAir(v, b, new V2(0f, -v.runSpeed), 0.1f, false);           // push the other way
            Assert.That(b.Velocity.Z, Is.LessThan(a.Velocity.Z));
            var apex = new RunState(); var low = new RunState();
            Locomotion.StepAir(v, apex, new V2(0f, v.runSpeed), 0.1f, true); Locomotion.StepAir(v, low, new V2(0f, v.runSpeed), 0.1f, false);
            Assert.That(apex.Speed, Is.GreaterThan(low.Speed), "extra steering at the apex");
            Assert.That(v.airAccel, Is.LessThan(v.accelGround));
        }

        // ------------------------------------------------------------ camera
        [Test]
        public void LookAheadLeadsTheRunEasesInAndHoldsThroughAShortStop()
        {
            var v = V(); var m = new LookAheadModel();
            for (int i = 0; i < 180; i++) m.Step(v, new V2(0f, v.runSpeed), v.runSpeed, 1f / 60f);
            Assert.That(m.Offset.Z, Is.EqualTo(v.camLookAheadRun).Within(0.1f)); Assert.That(Math.Abs(m.Offset.X), Is.LessThan(0.01f));
            var before = m.Offset.Z;
            for (int i = 0; i < 10; i++) m.Step(v, new V2(0f, 0f), v.runSpeed, 1f / 60f);
            Assert.That(m.Offset.Z, Is.GreaterThan(before * 0.8f), "held through a short stop");
            for (int i = 0; i < 300; i++) m.Step(v, new V2(0f, 0f), v.runSpeed, 1f / 60f);
            Assert.That(m.Offset.Length, Is.LessThan(0.05f), "back to centre once he has really stopped");
            // run left: the offset swings left
            var l = new LookAheadModel();
            for (int i = 0; i < 180; i++) l.Step(v, new V2(-v.runSpeed, 0f), v.runSpeed, 1f / 60f);
            Assert.That(l.Offset.X, Is.LessThan(-1f));
            // chase view leads further
            var c = new LookAheadModel();
            for (int i = 0; i < 180; i++) c.Step(v, new V2(0f, v.runSpeed), v.runSpeed, 1f / 60f, v.camChaseLookAheadMult);
            Assert.That(c.Offset.Z, Is.GreaterThan(m.Offset.Z * 0.01f + 3f));
        }

        [Test]
        public void TheCameraIgnoresSmallHopsFollowsBigClimbsAndCatchesBigDrops()
        {
            var v = V(); var cam = new VerticalFollowModel(); cam.Reset(0f);
            float minY = 0, maxY = 0;
            for (int i = 0; i < 120; i++) // hop 1.0 m up and down repeatedly
            {
                float hero = (float)Math.Abs(Math.Sin(i * 0.15)) * 1.0f;
                float y = cam.Step(v, hero, hero < 0.01f, 1f / 60f);
                minY = Math.Min(minY, y); maxY = Math.Max(maxY, y);
            }
            Assert.That(maxY - minY, Is.LessThan(0.35f), "a 1 m hop barely moves the screen");
            var climb = new VerticalFollowModel(); climb.Reset(0f);
            float last = 0; for (int i = 0; i < 300; i++) last = climb.Step(v, 6f, false, 1f / 60f);
            Assert.That(last, Is.GreaterThan(6f - v.camVertWindow - 0.15f), "he is never left outside the window");
            for (int i = 0; i < 400; i++) last = climb.Step(v, 6f, true, 1f / 60f);
            Assert.That(last, Is.EqualTo(6f).Within(0.05f), "settles on the new floor");
            var drop = new VerticalFollowModel(); drop.Reset(10f);
            float y2 = 10f; int frames = 0;
            while (y2 > v.camVertWindow + 0.3f && frames++ < 600) y2 = drop.Step(v, 0f, false, 1f / 60f);
            Assert.That(frames / 60f, Is.LessThan(2.5f), "falls into a pit are followed");
            var tight = new VerticalFollowModel(); tight.Reset(0f); var wide = new VerticalFollowModel(); wide.Reset(0f);
            float ty = 0, wy = 0; for (int i = 0; i < 60; i++) { ty = tight.Step(v, 1.2f, false, 1f / 60f, v.camChaseVertWindowMult); wy = wide.Step(v, 1.2f, false, 1f / 60f); }
            Assert.That(ty, Is.GreaterThan(wy), "the chase camera follows a smaller window");
        }

        [Test]
        public void BossFramingWeightsTheHeroAndLiftsForADistantBoss()
        {
            var v = V();
            BossFraming.Focus(v, 0f, 0f, 10f, 20f, out float x, out float z);
            Assert.That(x, Is.EqualTo(10f * (1f - v.camBossHeroWeight)).Within(1e-4f)); Assert.That(z, Is.EqualTo(20f * (1f - v.camBossHeroWeight)).Within(1e-4f));
            Assert.That(BossFraming.Lift(v, 0f), Is.EqualTo(0f)); Assert.That(BossFraming.Lift(v, 40f), Is.EqualTo(v.camBossLift));
        }

        // ------------------------------------------------------------ input
        [Test]
        public void TheDeadZoneRemovesJitterWithoutAJumpAtItsEdgeAndFullTiltIsReachable()
        {
            var v = V();
            InputShaping.Apply(v, 0.05f, 0.05f, false, out float x, out float y); Assert.That(x, Is.EqualTo(0f)); Assert.That(y, Is.EqualTo(0f));
            InputShaping.Apply(v, v.stickDeadZone + 0.01f, 0f, false, out x, out _); Assert.That(x, Is.LessThan(0.03f), "starts from zero at the edge of the dead zone");
            InputShaping.Apply(v, 1f, 0f, false, out x, out _); Assert.That(x, Is.EqualTo(1f).Within(1e-5f));
            InputShaping.Apply(v, v.stickOuterZone, 0f, false, out x, out _); Assert.That(x, Is.EqualTo(1f).Within(1e-5f), "full tilt before the physical rim");
            InputShaping.Apply(v, 0.3f, 0.4f, false, out x, out y); Assert.That(x / y, Is.EqualTo(0.75f).Within(1e-4f), "direction is preserved");
        }

        [Test]
        public void TheResponseCurveGivesFineControlNearTheMiddleAndIsMonotonic()
        {
            var v = V(); float prev = -1;
            for (int i = 0; i <= 20; i++) { InputShaping.Apply(v, i / 20f, 0f, false, out float x, out _); Assert.That(x, Is.GreaterThanOrEqualTo(prev)); prev = x; }
            InputShaping.Apply(v, 0.5f, 0f, false, out float half, out _);
            var linear = V(); linear.stickCurve = 1f; InputShaping.Apply(linear, 0.5f, 0f, false, out float lin, out _);
            Assert.That(half, Is.LessThan(lin), "an exponent above 1 flattens the middle");
        }

        [Test]
        public void OneHandModeReachesFullSpeedWithLessTravelAndSoftensTheCurve()
        {
            var v = V();
            InputShaping.Apply(v, 0.7f, 0f, false, out float normal, out _); InputShaping.Apply(v, 0.7f, 0f, true, out float one, out _);
            Assert.That(one, Is.GreaterThan(normal));
            InputShaping.Apply(v, 0.9f, 0f, true, out float full, out _); Assert.That(full, Is.EqualTo(1f).Within(1e-5f));
            InputShaping.Apply(v, v.stickDeadZone + 0.02f, 0f, true, out float nudge, out _); Assert.That(nudge, Is.EqualTo(0f), "a slightly wider dead zone for one thumb");
        }

        // ------------------------------------------------------------ procedural layer
        [Test]
        public void TheBudgetFollowsTheTierAndReduceMotionScalesAmplitudes()
        {
            var v = V();
            var low = ProcBudget.For(v, QualityTier.Low, false); var mid = ProcBudget.For(v, QualityTier.Medium, false); var high = ProcBudget.For(v, QualityTier.High, false);
            Assert.That(low.FootIk || low.LookAt || low.Cloth, Is.False, "Low keeps only the cheap parts");
            Assert.That(low.Lean && low.Breath && low.SquashStretch && low.Flinch, Is.True);
            Assert.That(mid.LookAt && mid.Cloth, Is.True); Assert.That(mid.FootIk, Is.False);
            Assert.That(high.FootIk && high.LookAt && high.Cloth, Is.True);
            Assert.That(ProcBudget.For(v, QualityTier.High, true).Amplitude, Is.EqualTo(v.reduceMotionScale)); Assert.That(high.Amplitude, Is.EqualTo(1f));
        }

        [Test]
        public void LeanGoesIntoTurnsForwardWithSpeedAndBackWhenSkidding()
        {
            var v = V(); var m = new LeanModel();
            for (int i = 0; i < 90; i++) m.Step(v, 300f, 1f, false, true, 1f / 60f);
            Assert.That(m.Roll, Is.LessThan(-3f), "turning right leans right (negative roll)"); Assert.That(m.Pitch, Is.EqualTo(v.leanForwardDegrees).Within(0.5f));
            for (int i = 0; i < 90; i++) m.Step(v, -300f, 1f, false, true, 1f / 60f);
            Assert.That(m.Roll, Is.GreaterThan(3f));
            for (int i = 0; i < 90; i++) m.Step(v, 0f, 1f, true, true, 1f / 60f);
            Assert.That(m.Pitch, Is.EqualTo(-v.leanSkidBackDegrees).Within(0.5f));
            for (int i = 0; i < 120; i++) m.Step(v, 0f, 0f, false, true, 1f / 60f);
            Assert.That(Math.Abs(m.Roll) + Math.Abs(m.Pitch), Is.LessThan(0.1f), "upright again");
            var red = new LeanModel(); for (int i = 0; i < 90; i++) red.Step(v, 300f, 1f, false, true, 1f / 60f, v.reduceMotionScale);
            Assert.That(Math.Abs(red.Pitch), Is.LessThan(v.leanForwardDegrees * 0.5f));
        }

        [Test]
        public void LookAtPicksNouraWhenSheIsInFrontNearOtherwiseTheNearestAndNeverTwistsPastTheLimit()
        {
            var v = V();
            var dx = new[] { 2f, 0f, -1f }; var dy = new[] { 0f, 0f, 0f }; var dz = new[] { 3f, 6f, -2f };   // 2 is behind him
            Assert.That(LookAtModel.Pick(v, dx, dy, dz, 3, 1), Is.EqualTo(1), "Noura first");
            Assert.That(LookAtModel.Pick(v, dx, dy, dz, 3, -1), Is.EqualTo(0), "else the nearest he can see");
            Assert.That(LookAtModel.Pick(v, new[] { 0f }, new[] { 0f }, new[] { -3f }, 1, 0), Is.EqualTo(-1), "behind him is ignored");
            Assert.That(LookAtModel.Pick(v, new[] { 0f }, new[] { 0f }, new[] { 30f }, 1, 0), Is.EqualTo(-1), "out of range");
            LookAtModel.AnglesTo(1f, 0f, 1f, out float yaw, out float pitch); Assert.That(yaw, Is.EqualTo(45f).Within(0.01f)); Assert.That(pitch, Is.EqualTo(0f).Within(0.01f));
            var m = new LookAtModel();
            for (int i = 0; i < 120; i++) m.Step(v, true, 100f, 80f, 1f / 60f);
            Assert.That(m.Yaw, Is.EqualTo(0f).Within(0.5f), "100 degrees to the side is beyond the neck, so he lets go");
            for (int i = 0; i < 120; i++) m.Step(v, true, 50f, 80f, 1f / 60f);
            Assert.That(m.Yaw, Is.EqualTo(50f).Within(0.5f)); Assert.That(m.Pitch, Is.EqualTo(v.lookMaxPitch).Within(0.5f)); Assert.That(m.Weight, Is.EqualTo(1f).Within(0.02f));
            for (int i = 0; i < 180; i++) m.Step(v, false, 0f, 0f, 1f / 60f);
            Assert.That(m.Weight, Is.LessThan(0.02f));
        }

        [Test]
        public void BreathingIsSlowAtRestAndFasterDeeperAfterASprintThenRecovers()
        {
            var v = V();
            float Amplitude(BreathModel b, int frames) { float mx = 0; for (int i = 0; i < frames; i++) { b.Step(v, 1f / 60f, false); mx = Math.Max(mx, Math.Abs(b.Value)); } return mx; }
            var rest = new BreathModel(); float restAmp = Amplitude(rest, 600);
            Assert.That(restAmp, Is.InRange(v.breathAmount * 0.8f, v.breathAmount * 1.01f));
            var tired = new BreathModel(); for (int i = 0; i < 120; i++) tired.Step(v, 1f / 60f, true);
            Assert.That(tired.Exertion, Is.GreaterThan(0.9f));
            float p0 = tired.Phase; tired.Step(v, 1f, false); float dPhase = tired.Phase - p0;
            var calm = new BreathModel(); calm.Step(v, 1f, false);
            Assert.That(dPhase, Is.GreaterThan(calm.Phase * 1.5f), "quicker breaths");
            Amplitude(tired, 60 * (int)(v.breathRecoverSeconds + 2f));
            Assert.That(tired.Exertion, Is.EqualTo(0f).Within(1e-3f));
        }

        [Test]
        public void SquashAndStretchKeepsVolumeSquashesOnLandingAndRingsBackToRest()
        {
            var v = V(); var m = new SquashStretchModel();
            m.Impulse(-0.25f); Assert.That(m.Height, Is.EqualTo(0.75f).Within(1e-4f)); Assert.That(m.Width, Is.GreaterThan(1f));
            Assert.That(m.Width * m.Width * m.Height, Is.EqualTo(1f).Within(1e-3f), "volume is preserved");
            bool overshoot = false;
            for (int i = 0; i < 180; i++) { m.Step(v, 1f / 60f, 0f, true); if (m.Offset > 0.001f) overshoot = true; }
            Assert.That(Math.Abs(m.Offset), Is.LessThan(0.005f), "settled"); Assert.That(overshoot, Is.True, "a springy bounce back past rest");
            var f = new SquashStretchModel(); for (int i = 0; i < 90; i++) f.Step(v, 1f / 60f, -18f, false);
            Assert.That(f.Offset, Is.GreaterThan(0.03f), "a fast fall stretches him");
            var j = new SquashStretchModel(); j.Kick(v.stretchJump * 30f); for (int i = 0; i < 4; i++) j.Step(v, 1f / 60f, 8f, false);
            Assert.That(j.Offset, Is.GreaterThan(0.02f), "take-off stretches");
            var wild = new SquashStretchModel(); wild.Step(v, 5f, 0f, true); Assert.That(float.IsNaN(wild.Offset), Is.False, "a huge frame cannot blow the spring up");
        }

        [Test]
        public void FootIkLowersThePelvisToTheLowerFootAndLiftsTheOtherTheRestOfTheWay()
        {
            var v = V();
            var flat = FootIk.Solve(v, 0f, 0f, 0f); Assert.That(flat.PelvisDrop, Is.EqualTo(0f)); Assert.That(flat.LeftLift + flat.RightLift, Is.EqualTo(0f));
            var stairs = FootIk.Solve(v, 0.2f, -0.2f, 0f);
            Assert.That(stairs.PelvisDrop, Is.EqualTo(-0.2f).Within(1e-5f)); Assert.That(stairs.RightLift, Is.EqualTo(0f).Within(1e-5f)); Assert.That(stairs.LeftLift, Is.EqualTo(0.4f).Within(1e-5f));
            var cliff = FootIk.Solve(v, 3f, -3f, 0f);
            Assert.That(stairs.LeftLift, Is.LessThanOrEqualTo(2 * v.footIkMaxDrop)); Assert.That(cliff.PelvisDrop, Is.GreaterThanOrEqualTo(-v.footIkMaxDrop - 1e-5f), "never reaches more than the limit");
            Assert.That(FootIk.Solve(v, float.NaN, float.NaN, 0f).PelvisDrop, Is.EqualTo(0f), "no ground hit, no change");
            v.footIkWeight = 0f; Assert.That(FootIk.Solve(v, 0.2f, -0.2f, 0f).LeftLift, Is.EqualTo(0f));
            Assert.That(FootIk.PlantedWeight(0f), Is.EqualTo(1f)); Assert.That(FootIk.PlantedWeight(0.3f), Is.EqualTo(0f));
        }

        [Test]
        public void AFlinchPushesAwayFromTheHitScalesWithStrengthAndDecays()
        {
            var v = V(); var f = new FlinchModel();
            f.Hit(0f, 1f, 1f);                       // from the front
            float peak = 0; for (int i = 0; i < 6; i++) { f.Step(v, 1f / 60f); peak = Math.Min(peak, f.Pitch); }
            Assert.That(peak, Is.LessThan(-v.flinchDegrees * 0.5f), "rocks back"); Assert.That(f.Roll, Is.EqualTo(0f).Within(1e-4f)); Assert.That(f.Shove, Is.LessThan(0f));
            for (int i = 0; i < 40; i++) f.Step(v, 1f / 60f);
            Assert.That(f.Active, Is.False); Assert.That(f.Pitch, Is.EqualTo(0f));
            var side = new FlinchModel(); side.Hit(1f, 0f, 1f); for (int i = 0; i < 5; i++) side.Step(v, 1f / 60f);
            Assert.That(side.Roll, Is.LessThan(0f), "a hit from the right rolls him left"); Assert.That(Math.Abs(side.Pitch), Is.LessThan(1e-3f));
            var behind = new FlinchModel(); behind.Hit(0f, -1f, 1f); for (int i = 0; i < 5; i++) behind.Step(v, 1f / 60f);
            Assert.That(behind.Pitch, Is.GreaterThan(0f), "pitched forward");
            var soft = new FlinchModel(); soft.Hit(0f, 1f, 0.3f); var hard = new FlinchModel(); hard.Hit(0f, 1f, 1f);
            for (int i = 0; i < 5; i++) { soft.Step(v, 1f / 60f); hard.Step(v, 1f / 60f); }
            Assert.That(Math.Abs(soft.Pitch), Is.LessThan(Math.Abs(hard.Pitch)));
            var calm = new FlinchModel(); calm.Hit(0f, 1f, 1f); for (int i = 0; i < 5; i++) calm.Step(v, 1f / 60f, v.reduceMotionScale);
            Assert.That(Math.Abs(calm.Pitch), Is.LessThan(Math.Abs(hard.Pitch)));
        }

        [Test]
        public void TheClothChainTrailsABodyInMotionKeepsItsLengthsAndSettlesBack()
        {
            var v = V(); var c = new VerletChain(4, 0.1f);
            void Rest(float rootX) { for (int i = 0; i < 4; i++) c.SetRest(i, rootX, -0.1f * i, 0f); }
            Rest(0f); c.Snap();
            for (int i = 0; i < 60; i++) { Rest(i * 0.12f); c.Step(v, 1f / 60f, 0f, 0f); }
            Assert.That(c.X[3], Is.LessThan(c.X[0] - 0.01f), "the tail lags behind the moving root");
            for (int i = 1; i < 4; i++)
            {
                float dx = c.X[i] - c.X[i - 1], dy = c.Y[i] - c.Y[i - 1], dz = c.Z[i] - c.Z[i - 1];
                Assert.That(Math.Sqrt(dx * dx + dy * dy + dz * dz), Is.EqualTo(0.1f).Within(0.002f), "segment " + i);
            }
            Rest(7.2f); for (int i = 0; i < 400; i++) c.Step(v, 1f / 60f, 0f, 0f);
            for (int i = 0; i < 4; i++) { Assert.That(c.X[i], Is.EqualTo(7.2f).Within(0.03f)); Assert.That(c.Y[i], Is.EqualTo(-0.1f * i).Within(0.03f)); }
            var w = new VerletChain(4, 0.1f); for (int i = 0; i < 4; i++) w.SetRest(i, 0f, -0.1f * i, 0f); w.Snap();
            for (int i = 0; i < 200; i++) w.Step(v, 1f / 60f, 40f, 0f);
            Assert.That(w.X[3], Is.GreaterThan(0.01f), "wind blows it sideways");
            float angle = (float)(Math.Atan2(w.X[1] - w.X[0], -(w.Y[1] - w.Y[0])) * 180 / Math.PI);
            Assert.That(Math.Abs(angle), Is.LessThanOrEqualTo(v.clothMaxAngle + 3f), "bounded by the cone");
            for (int i = 0; i < 4; i++) Assert.That(float.IsNaN(w.X[i]) || float.IsNaN(w.Y[i]), Is.False);
        }

        // ------------------------------------------------------------ animation list and hooks
        [Test]
        public void TheClipListIsConsistentAndEveryClipHasAHookThatExists()
        {
            var file = TestData.Read<Animation.AnimClipFile>("Config", "animation_clips.json");
            Assert.That(file.Validate(), Is.Empty);
            Assert.That(file.clips.Length, Is.InRange(200, 340));
            var p0 = file.P0().ToList();
            Assert.That(p0.Count, Is.InRange(30, 100), "a vertical slice, not the whole game");
            foreach (var actor in new[] { "aron", "noura", "duhaim", "falcon", "oryx", "camel" }) Assert.That(p0.Any(c => c.actor == actor), Is.True, actor + " has P0 clips");
            foreach (var actor in Animation.AnimClipFile.Actors) Assert.That(file.For(actor).Any(), Is.True, actor);
            Assert.That(file.clips.Any(c => c.loop) && file.clips.Any(c => !c.loop), Is.True);
        }

        [Test]
        public void EveryP0ParameterIsInTheHooksTableAndTheMarkdownMatchesTheJson()
        {
            var file = TestData.Read<Animation.AnimClipFile>("Config", "animation_clips.json");
            var names = Animation.AnimParams.All.Select(p => p.Name).ToList();
            Assert.That(names.Distinct().Count(), Is.EqualTo(names.Count));
            foreach (var p in file.P0Parameters()) Assert.That(names, Does.Contain(p.Name));
            Assert.That(file.P0Parameters().Count, Is.GreaterThan(15));
            string repo = System.IO.Path.GetFullPath(System.IO.Path.Combine(TestData.Resources(), "..", "..", ".."));
            string md = System.IO.File.ReadAllText(System.IO.Path.Combine(repo, "docs", "animation_list.md"));
            Assert.That(md, Does.Contain("**" + file.clips.Length + " clips**"));
            Assert.That(md, Does.Contain("**" + file.P0().Count() + " of them P0**"));
            foreach (var c in file.clips) Assert.That(md, Does.Contain("`" + c.id + "`"), c.id);
        }

        [Test]
        public void ThePlayerAnimationHooksExposeEveryParameterTheListUsesAndNouraIsAlwaysModest()
        {
            string root = System.IO.Path.GetFullPath(System.IO.Path.Combine(TestData.Resources(), ".."));
            string hooks = System.IO.File.ReadAllText(System.IO.Path.Combine(root, "Scripts", "Player", "PlayerAnimationHooks.cs"));
            Assert.That(hooks, Does.Contain("AnimParams.All"));
            foreach (var m in new[] { "SetFloat", "SetBool", "SetInt", "Trigger", "MissingP0Parameters", "OnLanded", "OnSkid", "OnHit", "OnKneel", "OnRise", "OnStumble", "OnBoost", "OnFinishCelebrate" }) Assert.That(hooks, Does.Contain(m), m);
            var file = TestData.Read<Animation.AnimClipFile>("Config", "animation_clips.json");
            // her clips never take the sleeve roll that is Aron's
            Assert.That(file.For("noura").Any(c => c.id.Contains("sleeve")), Is.False);
            Assert.That(file.For("aron").Any(c => c.id == "aron_sleeve_roll"), Is.True);
        }

        [Test]
        public void SecondaryMotionOnlyTouchesTheShemaghTailsAndTheHemsNeverASleeve()
        {
            foreach (var ok in new[] { "ShemaghTail_01", "ShemaghTail", "ThobeHem_L", "AbayaHem_Back_02" }) Assert.That(SecondaryMotionRules.Matches(ok), Is.True, ok);
            foreach (var no in new[] { "Sleeve_L", "ThobeHem_Sleeve", "ShemaghTail_sleeve", "UpperArm_L", "Hips", "", null }) Assert.That(SecondaryMotionRules.Matches(no), Is.False, no ?? "null");
            var v = new FeelValues { clothMaxAngle = 80f };
            Assert.That(SecondaryMotionRules.MaxAngle(v, "AbayaHem_L"), Is.LessThanOrEqualTo(SecondaryMotionRules.AbayaMaxAngle), "her abaya hem never swings wide enough to show anything");
            Assert.That(SecondaryMotionRules.MaxAngle(v, "ThobeHem_L"), Is.EqualTo(80f));
        }

        [Test]
        public void ThePlayerControllerTakesEveryFeelNumberFromTheSharedValuesNotFromAScene()
        {
            string root = System.IO.Path.GetFullPath(System.IO.Path.Combine(TestData.Resources(), ".."));
            string pc = System.IO.File.ReadAllText(System.IO.Path.Combine(root, "Scripts", "Player", "PlayerController.cs"));
            Assert.That(pc, Does.Contain("FeelSettings.V"));
            foreach (var old in new[] { "config.runSpeed", "config.acceleration", "config.gravity", "config.jumpHeight", "config.coyoteTime", "config.jumpBuffer", "config.spinDuration" })
                Assert.That(pc, Does.Not.Contain(old), old);
            string cfg = System.IO.File.ReadAllText(System.IO.Path.Combine(root, "Scripts", "Config", "PlayerConfig.cs"));
            foreach (var gone in new[] { "runSpeed", "gravity", "jumpHeight", "coyoteTime", "jumpBuffer" }) Assert.That(cfg, Does.Not.Contain(gone), gone);
        }

        [Test]
        public void TheGameFeelDocListsEveryTunableAndEveryShippedValueIsInRange()
        {
            string repo = System.IO.Path.GetFullPath(System.IO.Path.Combine(TestData.Resources(), "..", "..", ".."));
            string md = System.IO.File.ReadAllText(System.IO.Path.Combine(repo, "docs", "game_feel.md"));
            foreach (var f in FeelValues.Fields) Assert.That(md, Does.Contain("`" + f.Name + "`"), f.Name);
            Assert.That(V().Validate(), Is.Empty);
        }
    }
}
