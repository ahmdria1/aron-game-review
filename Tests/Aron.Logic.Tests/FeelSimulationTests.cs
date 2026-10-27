// (c) Ahmed Alamrani. All rights reserved.
using System;
using System.IO;
using System.Linq;
using Aron.Logic.GameFeel;
using Aron.Logic.Animals;
using Aron.Logic.Regions;
using NUnit.Framework;

namespace Aron.Logic.Tests
{
    /// <summary>
    /// The tuning pass, as numbers. A headless copy of the player's frame (<see cref="FeelSim"/>) plays scripted input over the shapes the stage
    /// builder lays out, at 30, 60 and 120 fps, and the results are held against the stage metrics and against the targets in
    /// docs/game_feel.md ("The tuning pass"). Change a value in game_feel.json and these say whether the game still plays.
    /// What this cannot say: how it feels in the thumb. That needs a phone.
    /// </summary>
    public class FeelSimulationTests
    {
        private static readonly int[] Rates = { 30, 60, 120 };
        private static FeelValues V() => TestData.Read<FeelValues>("Config", "game_feel.json");

        // ------------------------------------------------------------ helpers
        /// <summary>Runs at the edge at <c>edge</c> and presses jump on the last frame his feet are still on the floor.</summary>
        private static FeelSim EdgeJump(FeelValues v, int fps, float edge, float gap, float dy, float runUp, float speed = 1f, float jump = 1f, float accel = 1f, bool sprint = false)
        {
            float dt = 1f / fps;
            var t = Terrain.GapRun(edge, gap, dy);
            return FeelSim.Play(v, t, fps, 8f, (s, time) => new SimInput
            {
                Move = 1, Sprint = sprint, JumpHeld = true,
                JumpPressed = s.Jumps == 0 && s.Grounded && s.Z + s.Run.Velocity.Z * dt + 0.05f >= edge
            }, edge - runUp, s => { s.SpeedMult = speed; s.JumpMult = jump; s.AccelMult = accel; });
        }

        private static bool Clears(FeelSim s, float edge, float gap, float dy)
        {
            if (s.JumpTimes.Count == 0) return false;
            foreach (var l in s.Landings)
                if (l.T > s.JumpTimes[0]) return l.Z >= edge + gap && Math.Abs(l.Y - dy) < 0.01f;
            return false;
        }

        /// <summary>The widest gap (steps of 5 cm) that edge jumps clear at every frame rate.</summary>
        private static float MaxGap(FeelValues v, float runUp, float dy = 0f, float speed = 1f, float jump = 1f, float accel = 1f, float limit = 20f)
        {
            float best = 0f;
            for (float g = 1f; g < limit; g += 0.05f)
            {
                bool all = Rates.All(fps => Clears(EdgeJump(v, fps, 60f, g, dy, runUp, speed, jump, accel), 60f, g, dy));
                if (all) best = g; else if (g > best + 0.5f) break;
            }
            return best;
        }

        private static FeelSim StandingJump(FeelValues v, int fps, float holdSeconds, float press = 0.5f)
            => FeelSim.Play(v, new Terrain().Flat(-50f, 200f), fps, 3f, (q, t) => new SimInput
            {
                JumpPressed = t + 1f / fps >= press && q.Jumps == 0,
                JumpHeld = q.Jumps > 0 && t - q.JumpTimes[0] < holdSeconds
            });

        // ------------------------------------------------------------ the jump
        [Test]
        public void AHeldJumpPeaksAtTheJumpHeightOnEveryFrameRate()
        {
            var v = V();
            foreach (int fps in new[] { 20, 30, 45, 60, 90, 120, 144 })
            {
                var s = StandingJump(v, fps, 10f);
                Assert.That(s.PeakY(), Is.EqualTo(v.jumpHeight).Within(v.jumpHeight * 0.015f), fps + " fps");
            }
        }

        [Test]
        public void TheJumpHasTheTargetShape()
        {
            var v = V();
            float v0 = JumpArc.VelocityForHeight(v, v.jumpHeight);
            float rise = JumpArc.TimeToApex(v, v0);
            Assert.That(rise, Is.InRange(0.38f, 0.48f), "time to the top");
            Assert.That(JumpArc.ApexHang(v, v0), Is.InRange(0.08f, 0.2f), "a short hang at the top, not a float");
            var s = StandingJump(v, 120, 10f);
            float air = s.AirTime();
            Assert.That(air, Is.InRange(0.68f, 0.85f), "whole jump");
            float fall = air - rise;
            Assert.That(fall, Is.LessThan(rise * 0.9f), "the way down is quicker than the way up");
            // the speed he hits the ground with: a landing that arrives, not drifts in
            var d = JumpArc.Launch(0f); float y = v.jumpHeight;
            while (y > 0f) { d = JumpArc.Step(v, d, JumpArc.SubStep, true); y += d.VelocityY * JumpArc.SubStep; }
            Assert.That(-d.VelocityY, Is.GreaterThan(12f));
            Assert.That(-d.VelocityY, Is.LessThanOrEqualTo(v.maxFallSpeed));
        }

        [Test]
        public void ATapIsAShortHopAndStillClimbsTheBiggestOrdinaryStep()
        {
            var v = V();
            foreach (int fps in Rates)
            {
                float tap = StandingJump(v, fps, 0f).PeakY(), held = StandingJump(v, fps, 10f).PeakY();
                Assert.That(tap, Is.LessThan(held * 0.7f), fps + " fps: shorter than a held jump");
                Assert.That(tap, Is.GreaterThan(StageLayout.FootStep + 0.08f), fps + " fps: a tap still clears a step of " + StageLayout.FootStep + " m");
            }
        }

        [Test]
        public void TheJumpClearsTheBuildersGapsAndStepsWithRoomToSpare()
        {
            var v = V();
            // standing at the edge (run-up 0) with the stick held, and at a trot
            float fromRest = MaxGap(v, 0f), trot = MaxGap(v, 2f), full = MaxGap(v, 8f);
            Assert.That(fromRest, Is.GreaterThanOrEqualTo(StageLayout.FootGap * 1.12f), "from a standstill at the edge");
            Assert.That(trot, Is.GreaterThanOrEqualTo(StageLayout.FootGap * 1.25f), "after two metres of run-up");
            Assert.That(full, Is.GreaterThan(trot));
            // up and down: the step the builder asks for on a short gap, and the longest drop it uses
            foreach (int fps in Rates)
            {
                Assert.That(Clears(EdgeJump(v, fps, 30f, 2.6f, StageLayout.FootStep, 6f), 30f, 2.6f, StageLayout.FootStep), Is.True, "step up at " + fps);
                Assert.That(Clears(EdgeJump(v, fps, 30f, StageLayout.FootGap, -3f, 6f), 30f, StageLayout.FootGap, -3f), Is.True, "long gap and a 3 m drop at " + fps);
            }
            // the highest platform one jump reaches
            float reach = 0f;
            for (float dy = 1f; dy < 3f; dy += 0.05f)
                if (Rates.All(fps => Clears(EdgeJump(v, fps, 30f, 2.6f, dy, 6f), 30f, 2.6f, dy))) reach = dy;
            Assert.That(reach, Is.GreaterThanOrEqualTo(StageLayout.FootStep * 1.6f));
        }

        [Test]
        public void MountsClearTheirOwnGapsAndSteps()
        {
            var v = V();
            foreach (var (kind, name) in new[] { (MountKind.Camel, "camel"), (MountKind.Horse, "horse") })
            {
                var m = MountStats.For(kind);
                float gap = StageLayout.MaxGap(name), step = StageLayout.MaxStep(name);
                foreach (int fps in Rates)
                {
                    var s = EdgeJump(v, fps, 60f, gap, 0f, 12f, m.speed, m.jump, m.acceleration);
                    Assert.That(Clears(s, 60f, gap, 0f), Is.True, $"{name} gap {gap} at {fps}");
                    var u = EdgeJump(v, fps, 60f, 2.6f, step, 12f, m.speed, m.jump, m.acceleration);
                    Assert.That(Clears(u, 60f, 2.6f, step), Is.True, $"{name} step {step} at {fps}");
                }
                Assert.That(MaxGap(v, 12f, 0f, m.speed, m.jump, m.acceleration, 20f), Is.GreaterThanOrEqualTo(gap * 1.2f), name + " has room");
            }
        }

        [Test]
        public void EveryGeneratedStageStaysInsideWhatTheJumpCanDo()
        {
            var v = V();
            float footReach = MaxGap(v, 0f), worstFoot = 0f, worstStep = 0f, worstDrop = 0f;
            foreach (var stage in TestData.Catalog().regions.SelectMany(r => r.stages))
            {
                var layout = StageLayout.Generate(stage);
                var ground = layout.Pieces.Where(p => p.kind == PieceKind.Ground).OrderBy(p => p.Start).ToList();
                bool mounted = layout.Pieces.Any(p => p.kind == PieceKind.MountPost);
                for (int i = 1; i < ground.Count; i++)
                {
                    float gap = ground[i].Start - ground[i - 1].End, dy = ground[i].y - ground[i - 1].y;
                    if (gap < 0.05f) { Assert.That(Math.Abs(dy), Is.LessThanOrEqualTo(3.05f), stage.id); continue; }
                    // a moving platform carries him over the wide ones
                    float lo = ground[i - 1].End - 0.5f, hi = ground[i].Start + 0.5f;
                    if (layout.Pieces.Any(p => p.kind == PieceKind.Mover && p.z >= lo && p.z <= hi)) continue;
                    float maxGap = mounted ? StageLayout.GiraffeGap : StageLayout.FootGap, maxStep = mounted ? StageLayout.GiraffeStep : StageLayout.FootStep;
                    Assert.That(gap, Is.LessThanOrEqualTo(maxGap + 0.01f), stage.id + " gap");
                    Assert.That(dy, Is.LessThanOrEqualTo(maxStep + 0.01f), stage.id + " step");
                    Assert.That(dy, Is.GreaterThanOrEqualTo(-3.01f), stage.id + " drop");
                    if (!mounted) { worstFoot = Math.Max(worstFoot, gap); worstStep = Math.Max(worstStep, dy); worstDrop = Math.Min(worstDrop, dy); }
                }
            }
            Assert.That(worstFoot, Is.GreaterThan(3f), "the scan found the gaps");
            Assert.That(worstFoot * 1.1f, Is.LessThanOrEqualTo(footReach), "the widest gap on foot is within what a standing jump clears");
        }

        // ------------------------------------------------------------ frame-rate independence
        [Test]
        public void RunningAndJumpingCoverTheSameGroundAtAnyFrameRate()
        {
            var v = V();
            var run = Rates.Select(fps => FeelSim.Play(v, new Terrain().Flat(-5f, 500f), fps, 1.5f, (q, t) => new SimInput { Move = 1 })).ToList();
            foreach (var s in run) Assert.That(s.Z, Is.EqualTo(run[2].Z).Within(0.15f), "1.5 s of running (under 1.5%)");
            var jumps = Rates.Select(fps => FeelSim.Play(v, new Terrain().Flat(-5f, 500f), fps, 3f, (q, t) => new SimInput { Move = 1, JumpHeld = true, JumpPressed = t > 1f && q.Jumps == 0 })).ToList();
            for (int i = 0; i < Rates.Length; i++)
            {
                float dist = jumps[i].Landings[0].Z - jumps[i].JumpZ[0], reference = jumps[2].Landings[0].Z - jumps[2].JumpZ[0];
                // a landing is found on a whole frame, so the distance can differ by the ground covered in one of them
                Assert.That(dist, Is.EqualTo(reference).Within(v.runSpeed * v.sprintMultiplier / Rates[i] + 0.1f), Rates[i] + " fps");
                Assert.That(jumps[i].AirTime(), Is.EqualTo(jumps[2].AirTime()).Within(1.5f / Rates[i] + 0.01f), Rates[i] + " fps air time");
            }
        }

        [Test]
        public void TheRunUpIsShort()
        {
            var v = V();
            foreach (int fps in Rates)
            {
                var s = FeelSim.Play(v, new Terrain().Flat(-5f, 500f), fps, 2f, (q, t) => new SimInput { Move = 1 });
                var at = s.Trace.First(x => x.Speed >= v.runSpeed * 0.9f);
                Assert.That(at.T, Is.LessThan(0.3f), fps + " fps: seconds to 90% of run speed");
                Assert.That(at.Z, Is.LessThan(1.5f), fps + " fps: metres to 90% of run speed");
                // and he stops where the thumb stops
                var stop = FeelSim.Play(v, new Terrain().Flat(-5f, 500f), fps, 4f, (q, t) => new SimInput { Move = t < 1.2f ? 1 : 0 });
                float z = stop.Trace.First(x => x.T >= 1.2f).Z;
                Assert.That(stop.Z - z, Is.LessThan(0.7f), fps + " fps: braking distance");
            }
        }

        // ------------------------------------------------------------ forgiveness
        [Test]
        public void ThePressStillCountsJustAfterWalkingOffAnEdgeAtEveryFrameRate()
        {
            var v = V();
            Assert.That(v.coyoteTime, Is.GreaterThanOrEqualTo(0.1f), "never stricter than a tenth of a second");
            foreach (int fps in Rates)
                foreach (var (late, expect) in new[] { (0.03f, 1), (0.09f, 1), (0.2f, 0) })
                {
                    float dt = 1f / fps, leaveT = -1f;
                    var s = FeelSim.Play(v, new Terrain().Flat(-30f, 5f).Flat(5f, 80f, -10f), fps, 3f, (q, t) =>
                    {
                        if (leaveT < 0f && q.Trace.Count > 0 && !q.Trace[q.Trace.Count - 1].Grounded) leaveT = q.Trace[q.Trace.Count - 1].T;
                        return new SimInput { Move = 1, JumpHeld = true, JumpPressed = leaveT >= 0f && q.Jumps == 0 && q.T + dt >= leaveT + late - 1e-4f && q.T + dt < leaveT + late + 0.9f * dt };
                    });
                    Assert.That(s.Jumps, Is.EqualTo(expect), $"{fps} fps, press {late}s after leaving the edge");
                }
        }

        [Test]
        public void ThePressStillCountsJustBeforeLandingAtEveryFrameRate()
        {
            var v = V();
            Assert.That(v.jumpBuffer, Is.GreaterThanOrEqualTo(0.12f));
            foreach (int fps in Rates)
            {
                var floor = new Terrain().Flat(-30f, 100f);
                float land = FeelSim.Play(v, floor, fps, 1f, (q, t) => new SimInput(), 0f, s => { s.Y = 1.5f; s.Grounded = false; }).Landings[0].T;
                foreach (var (early, expect) in new[] { (0.05f, 1), (0.12f, 1), (0.3f, 0) })
                {
                    var s = FeelSim.Play(v, floor, fps, 1.2f, (q, t) => new SimInput { JumpHeld = true, JumpPressed = q.Jumps == 0 && t + 1f / fps >= land - early && t + 0.001f < land - early + 1f / fps },
                        0f, q => { q.Y = 1.5f; q.Grounded = false; });
                    Assert.That(s.Jumps, Is.EqualTo(expect), $"{fps} fps, press {early}s before landing");
                }
            }
        }

        [Test]
        public void OneHandModeWidensBothWindows()
        {
            var v = V();
            Assert.That(InputBuffers.CoyoteWindow(v, true), Is.GreaterThan(InputBuffers.CoyoteWindow(v, false)));
            Assert.That(InputBuffers.JumpWindow(v, true), Is.GreaterThan(InputBuffers.JumpWindow(v, false)));
            Assert.That(InputBuffers.CoyoteWindow(v, true), Is.LessThanOrEqualTo(0.2f), "wide, but not a free hover");
            Assert.That(InputBuffers.JumpWindow(v, true), Is.LessThanOrEqualTo(0.25f));
        }

        // ------------------------------------------------------------ slopes
        [Test]
        public void HeStaysOnTheGroundRunningDownAnySlopeHeCanStandOn()
        {
            var v = V();
            float run = v.runSpeed * v.sprintMultiplier * (1f + v.slopeDownhillBoost);
            foreach (int fps in Rates)
                foreach (float deg in new[] { 15f, 30f, 40f, v.slopeLimit })
                {
                    float dz = 60f, dy = -(float)Math.Tan(deg * Math.PI / 180.0) * dz;
                    var t = new Terrain().Flat(-30f, 0f).Ramp(0f, dz, 0f, dy).Flat(dz, dz + 30f, dy);
                    var s = FeelSim.Play(v, t, fps, 6f, (q, tt) => new SimInput { Move = 1, Sprint = true }, -8f);
                    var onSlope = s.Trace.Where(x => x.Z > 2f && x.Z < dz - 2f).ToList();
                    Assert.That(onSlope.Count, Is.GreaterThan(20));
                    // at the very limit and 30 fps one frame at the top of the slope can still lift off (a step of 0.33 s at 50 degrees); it lands the next frame
                    int allowed = deg >= v.slopeLimit && fps <= 30 ? 1 : 0;
                    Assert.That(onSlope.Count(x => !x.Grounded), Is.LessThanOrEqualTo(allowed), $"{deg}° at {fps} fps (sprint {run:0.0} m/s)");
                }
        }

        [Test]
        public void ThePressOntoTheGroundGrowsOnlyWhenRunningDownhill()
        {
            var v = V();
            float flat = Slopes.StickSpeed(v, 9f, 1f, 1f);
            Assert.That(flat, Is.EqualTo(v.groundStickSpeed), "flat ground");
            Assert.That(Slopes.StickSpeed(v, 9f, (float)Math.Cos(40 * Math.PI / 180), -1f), Is.EqualTo(v.groundStickSpeed), "uphill");
            Assert.That(Slopes.StickSpeed(v, 9f, (float)Math.Cos(40 * Math.PI / 180), 0f), Is.EqualTo(v.groundStickSpeed), "across");
            float steep = Slopes.StickSpeed(v, 9f, (float)Math.Cos(v.slopeLimit * Math.PI / 180), 1f);
            Assert.That(steep, Is.GreaterThan(9f * Math.Tan(v.slopeLimit * Math.PI / 180)), "faster than the ground falls away");
            Assert.That(steep, Is.LessThanOrEqualTo(v.maxFallSpeed));
        }

        [Test]
        public void UphillSlowsHimALittleAndNeverStopsHim()
        {
            var v = V();
            foreach (int fps in Rates)
            {
                float dz = 40f, dy = (float)Math.Tan(v.slopeLimit * Math.PI / 180.0) * dz;
                var t = new Terrain().Flat(-30f, 0f).Ramp(0f, dz, 0f, dy).Flat(dz, dz + 30f, dy);
                var s = FeelSim.Play(v, t, fps, 8f, (q, tt) => new SimInput { Move = 1 }, -3f);
                Assert.That(s.Z, Is.GreaterThan(dz + 1f), fps + " fps: he gets to the top of the steepest walkable slope");
                var mid = s.Trace.First(x => x.Z > dz * 0.5f);
                Assert.That(mid.Speed, Is.GreaterThanOrEqualTo(v.runSpeed * (1f - v.slopeUphillSlow) - 0.1f));
                Assert.That(mid.Grounded, Is.True);
            }
            Assert.That(Slopes.SlideSpeed(v, (float)Math.Cos((v.slopeLimit + 10) * Math.PI / 180)), Is.GreaterThan(0f), "steeper than the limit he slides");
        }

        // ------------------------------------------------------------ landing
        [Test]
        public void LandingFromAJumpCostsNothingAndTheBiggestDropCostsAFewCentimetres()
        {
            var v = V();
            foreach (int fps in Rates)
            {
                var flat = FeelSim.Play(v, new Terrain().Flat(-5f, 500f), fps, 4f, (q, t) => new SimInput { Move = 1, JumpHeld = true, JumpPressed = q.Jumps == 0 && t > 1f });
                Assert.That(flat.Landings.Any(l => l.Lag), Is.False, fps + " fps: an ordinary jump never staggers him");
                // the longest fall the builder makes: a full jump off a ledge down a 3 m drop
                var held = EdgeJump(v, fps, 30f, 4.2f, -3f, 8f);
                var l3 = held.Landings.First(l => l.T > held.JumpTimes[0]);
                Assert.That(l3.Fall, Is.GreaterThan(v.landLagMinFall), "this drop does stagger him");
                Assert.That(l3.Lag, Is.True);
                var control = FeelSim.Play(v, Terrain.GapRun(30f, 4.2f, -3f), fps, 8f, (q, t) => new SimInput { Move = 1, Sprint = false }, 22f);
                // distance covered 0.6 s after landing, with and without the stumble: the stumble is under half a metre
                float after(FeelSim s, float landT) => s.Trace.First(x => x.T >= landT + 0.6f).Z - s.Trace.First(x => x.T >= landT).Z;
                float withLag = after(held, l3.T);
                float noLag = held.Trace.First(x => x.T >= l3.T).Speed * 0.6f;
                Assert.That(noLag - withLag, Is.LessThan(0.5f), fps + " fps: what the stumble costs in metres");
            }
            Assert.That(v.landLagFrames / FixedStepper.Rate, Is.LessThanOrEqualTo(0.12f), "a stumble is over in 0.12 s at most");
        }

        // ------------------------------------------------------------ spin and bounce
        [Test]
        public void TheSpinLastsTheSameNumberOfSixtyHertzFramesAtAnyFrameRate()
        {
            var v = V();
            int frames = v.spinStartupFrames + v.spinActiveFrames + v.spinRecoveryFrames;
            foreach (int fps in Rates)
            {
                bool spun = false; float startT = -1f, endT = -1f;
                var s = FeelSim.Play(v, new Terrain().Flat(-50f, 200f), fps, 2f, (q, t) =>
                {
                    var i = new SimInput { JumpHeld = true, JumpPressed = t > 0.4f && q.Jumps == 0 };
                    if (q.Jumps > 0 && !spun && q.Vy < 0f) { i.AttackPressed = true; spun = true; startT = t; }
                    if (spun && endT < 0f && startT >= 0f && t > startT + 0.05f && !q.SpinRunning) endT = t;
                    return i;
                });
                Assert.That(spun, Is.True);
                Assert.That(endT - startT, Is.EqualTo(frames / FixedStepper.Rate).Within(2f / fps + 0.03f), $"{fps} fps spin length");
                // the hang the spin gives is brief: it is a move, not a glide
                var plain = StandingJump(v, fps, 10f, 0.4f).AirTime();
                Assert.That(s.AirTime() - plain, Is.LessThanOrEqualTo(0.12f), fps + " fps extra air time from a spin");
            }
        }

        [Test]
        public void CratesAlwaysThrowHimTheSameHeightAtAnyFrameRate()
        {
            var v = V();
            foreach (int fps in Rates)
                foreach (var (h, name) in new[] { (v.crateBounceHeight, "plain"), (v.crateBounceHeldHeight, "held"), (v.stompBounceHeight, "stomp") })
                {
                    // the bounce replaces his vertical speed, so what he fell from does not matter; only the arc does
                    var b = FeelSim.Play(v, new Terrain().Flat(-50f, 200f), fps, 2f, (q, t) => new SimInput { JumpHeld = true }, 0f, q => q.Bounce(h));
                    Assert.That(b.PeakY(), Is.EqualTo(h).Within(h * 0.03f), $"{name} bounce at {fps} fps");
                }
            Assert.That(v.crateBounceHeight, Is.GreaterThanOrEqualTo(StageLayout.FootStep * 2f), "the crate lifts him over the step it stands beside");
            Assert.That(v.crateBounceHeldHeight, Is.GreaterThan(v.crateBounceHeight));
        }

        // ------------------------------------------------------------ camera
        [Test]
        public void TheCameraLooksAheadQuicklyAndNeverFarther()
        {
            var v = V();
            foreach (int fps in Rates)
            {
                var s = FeelSim.Play(v, new Terrain().Flat(-5f, 500f), fps, 3f, (q, t) => new SimInput { Move = 1 });
                var cam = CameraSim.Follow(v, s.Trace, fps, v.runSpeed);
                Assert.That(cam.First(c => c.LookAhead >= 0.9f * v.camLookAheadRun).T, Is.LessThan(0.55f), fps + " fps: time to 90% of the look-ahead");
                Assert.That(cam.Max(c => c.LookAhead), Is.LessThanOrEqualTo(v.camLookAheadRun + 0.01f));
            }
        }

        [Test]
        public void TheCameraNeverLosesHimAndSettlesOnANewFloorInAboutASecond()
        {
            var v = V();
            // roughly the part of the picture he stays in: the free camera is 8.2 m from its focus and looks through a 60 degree lens
            const float visibleHalfHeight = 4.0f;
            var settleAt = new float[3]; int n = 0;
            foreach (int fps in Rates)
            {
                float worstOffset = 0f, worstSettle = 0f, flatJump = 0f; float dt = 1f / fps;
                foreach (var (gap, dy) in new[] { (4.2f, 0f), (2.6f, 1.2f), (4.2f, -3f), (2f, -1.5f), (2.5f, -2.5f) })
                {
                    var s = EdgeJump(v, fps, 40f, gap, dy, 8f);
                    float landT = s.Landings.First(l => l.T > s.JumpTimes[0]).T;
                    // only the jump and the two seconds after it: he keeps running to the end of the floor and falls off it
                    var cam = CameraSim.Follow(v, s.Trace.Where(x => x.T <= landT + 2f).ToList(), fps, v.runSpeed * v.sprintMultiplier);
                    float off = cam.Max(c => Math.Abs(c.HeroY - c.CamY));
                    float settle = cam.First(c => c.T > landT && Math.Abs(c.HeroY - c.CamY) < 0.1f).T - landT;
                    worstOffset = Math.Max(worstOffset, off); worstSettle = Math.Max(worstSettle, settle);
                    if (dy == 0f) flatJump = off;
                }
                Assert.That(worstOffset, Is.LessThanOrEqualTo(visibleHalfHeight * 0.7f), fps + " fps: farthest he gets from the middle of the picture");
                Assert.That(worstSettle, Is.LessThanOrEqualTo(1.1f), fps + " fps: seconds to settle within 10 cm of a new floor");
                Assert.That(flatJump, Is.LessThanOrEqualTo(v.camVertWindow + 0.9f), fps + " fps: an ordinary jump barely moves the picture");
                settleAt[n++] = worstSettle;
            }
            Assert.That(settleAt.Max() - settleAt.Min(), Is.LessThan(0.2f), "settling takes the same time at every frame rate");
        }

        // ------------------------------------------------------------ the lock
        /// <summary>
        /// The values this pass settled on. Changing one of them on purpose is fine; do it here, in game_feel.json, in FeelValues and in
        /// docs/game_feel.md ("The tuning pass") together, and re-read the targets above.
        /// </summary>
        [Test]
        public void TheTunedValuesAreLocked()
        {
            var v = V();
            var locked = new (string name, float value)[]
            {
                ("runSpeed", 7f), ("jumpHeight", 2.2f), ("gravity", 30f), ("fallGravityMult", 1.55f), ("jumpCutGravityMult", 2.8f), ("jumpCutDelay", 0.08f),
                ("apexThreshold", 1.6f), ("apexGravityMult", 0.6f), ("coyoteTime", 0.12f), ("jumpBuffer", 0.15f),
                ("groundStickSpeed", 6f), ("camVertWindow", 1.1f), ("camVertSmooth", 0.25f), ("camVertSettleSmooth", 0.4f),
                ("camFallSnapDistance", 2.6f), ("camLookAheadRun", 2.5f),
            };
            foreach (var (name, value) in locked)
            {
                var f = FeelValues.Fields.First(x => x.Name == name);
                Assert.That(v.Get(f), Is.EqualTo(value).Within(1e-6f), name);
            }
        }

        [Test]
        public void TheDocWritesDownTheTuningPassAndItsTargets()
        {
            string dir = TestContext.CurrentContext.TestDirectory;
            string doc = null;
            for (var d = new DirectoryInfo(dir); d != null; d = d.Parent)
            {
                var p = Path.Combine(d.FullName, "docs", "game_feel.md");
                if (File.Exists(p)) { doc = File.ReadAllText(p); break; }
            }
            Assert.That(doc, Is.Not.Null);
            int at = doc.IndexOf("## The tuning pass", StringComparison.Ordinal);
            Assert.That(at, Is.GreaterThan(0));
            string section = doc.Substring(at);
            foreach (var name in new[] { "apexThreshold", "apexGravityMult", "coyoteTime", "jumpBuffer", "camVertSmooth", "camVertSettleSmooth", "camFallSnapDistance", "groundStickSpeed" })
                Assert.That(section, Does.Contain("`" + name + "`"), name);
            Assert.That(section, Does.Contain("FeelSimulationTests"));
            Assert.That(section, Does.Contain("not verified").IgnoreCase.Or.Contain("unverified").IgnoreCase.Or.Contain("on a phone"));
        }
    }
}
