// (c) Ahmed Alamrani. All rights reserved.
using System;
using System.Collections.Generic;
using Aron.Logic.GameFeel;

namespace Aron.Logic.Tests
{
    /// <summary>
    /// A flat stretch or a ramp of walkable ground, in the side view the stage builder lays out: z is forward, y is up.
    /// The top height runs linearly from <c>Y0</c> at <c>Z0</c> to <c>Y1</c> at <c>Z1</c>.
    /// </summary>
    public sealed class Surface
    {
        public readonly float Z0, Z1, Y0, Y1;
        public Surface(float z0, float z1, float y0, float y1) { Z0 = z0; Z1 = z1; Y0 = y0; Y1 = y1; }
        public bool Covers(float z) => z >= Z0 - 1e-6f && z <= Z1 + 1e-6f;
        public float TopAt(float z) => Z1 - Z0 < 1e-6f ? Y0 : Y0 + (Y1 - Y0) * ((z - Z0) / (Z1 - Z0));
        public float NormalY { get { float dy = Y1 - Y0, dz = Z1 - Z0; return (float)(dz / Math.Sqrt(dy * dy + dz * dz)); } }
        /// <summary>Slope direction: positive when the ground rises going forward.</summary>
        public float Rise => Z1 - Z0 < 1e-6f ? 0f : (Y1 - Y0) / (Z1 - Z0);
    }

    /// <summary>The ground a simulated run happens on.</summary>
    public sealed class Terrain
    {
        public readonly List<Surface> Surfaces = new List<Surface>();
        public Terrain Flat(float z0, float z1, float y = 0f) { Surfaces.Add(new Surface(z0, z1, y, y)); return this; }
        public Terrain Ramp(float z0, float z1, float y0, float y1) { Surfaces.Add(new Surface(z0, z1, y0, y1)); return this; }

        /// <summary>The highest ground under this z, or null over a gap.</summary>
        public Surface At(float z)
        {
            Surface best = null; float top = float.NegativeInfinity;
            foreach (var s in Surfaces) if (s.Covers(z)) { float t = s.TopAt(z); if (t > top) { top = t; best = s; } }
            return best;
        }

        /// <summary>The nearest start of ground inside (from, to], used as the wall a platform presents from the side.</summary>
        public float? WallBetween(float from, float to)
        {
            float? w = null;
            foreach (var s in Surfaces) if (s.Z0 > from + 1e-6f && s.Z0 <= to + 1e-6f && (!w.HasValue || s.Z0 < w.Value)) w = s.Z0;
            return w;
        }

        /// <summary>A start line, <paramref name="runUp"/> metres of floor, a gap, then a landing platform at a height difference.</summary>
        public static Terrain GapRun(float runUp, float gap, float dy = 0f, float landing = 30f)
        {
            var t = new Terrain();
            t.Flat(-50f, runUp, 0f);
            t.Flat(runUp + gap, runUp + gap + landing, dy);
            return t;
        }
    }

    public struct SimInput
    {
        public bool JumpPressed, JumpHeld, AttackPressed, Sprint, SlidePressed;
        /// <summary>Stick forward, 0..1.</summary>
        public float Move;
    }

    public struct SimSample { public float T, Z, Y, Vy, Speed; public bool Grounded; }

    public struct SimLanding { public float T, Z, Y, Fall; public bool Lag; }

    /// <summary>
    /// A headless copy of the player's frame in <c>PlayerController.Update</c>, played over a side-view terrain and driven by scripted input.
    /// It uses the same pure models in the same order: the 60 Hz stepper, jump buffer and coyote clock, slope speed scale, ground and air
    /// locomotion, the shaped jump arc sub-stepped at 120 Hz, spin, land lag and ledge forgiveness. The only things standing in for Unity are
    /// the ground contact (the character is a single point, so a ledge gives him no extra footing a real capsule would, which keeps every clearance
    /// result on the safe side) and the wall a platform presents to a jump that falls short.
    /// </summary>
    public sealed class FeelSim
    {
        public readonly FeelValues V;
        public readonly Terrain Ground;
        public float Z, Y, Vy, T;
        public bool Grounded;
        public readonly RunState Run = new RunState();
        public readonly List<SimSample> Trace = new List<SimSample>();
        public readonly List<SimLanding> Landings = new List<SimLanding>();
        public readonly List<float> JumpTimes = new List<float>();
        public readonly List<float> JumpZ = new List<float>();
        public int Jumps;
        public bool OneHand, DoubleJumpAllowed;
        /// <summary>A mount's speed, jump and acceleration multipliers (1 on foot).</summary>
        public float SpeedMult = 1f, JumpMult = 1f, AccelMult = 1f;

        private readonly InputBuffers buffers = new InputBuffers();
        private readonly FixedStepper stepper = new FixedStepper();
        private readonly AttackTimeline spin = new AttackTimeline();
        private LandLag landLag;
        private JumpState jump;
        private int spinCooldown;
        private bool usedDouble, wasGrounded, noCut;
        private float airPeakY, lastLiftTime = -100f, flatOutTime, jumpSpeedCache, jumpSpeedFor = -1f;

        public bool SpinRunning => spin.Running;
        public bool LandLagActive => landLag.Active;

        public FeelSim(FeelValues v, Terrain ground, float startZ = 0f)
        {
            V = v; Ground = ground; Z = startZ;
            var s = ground.At(startZ);
            Y = s != null ? s.TopAt(startZ) : 0f; Grounded = s != null; wasGrounded = Grounded;
            landLag = LandLag.For(v, 0f);
            jump = new JumpState();
        }

        public float TakeOffSpeed(float mult) => JumpSpeed(V.jumpHeight * mult);

        private float JumpSpeed(float height)
        {
            if (Math.Abs(jumpSpeedFor - height) > 1e-6f) { jumpSpeedCache = JumpArc.VelocityForHeight(V, height); jumpSpeedFor = height; }
            return jumpSpeedCache;
        }

        /// <summary>A crate or stomp bounce to this height, as the controller does it (it also refreshes the double jump).</summary>
        public void Bounce(float height)
        {
            Vy = JumpSpeed(height); jump = JumpArc.Launch(Vy); noCut = true; usedDouble = false; Grounded = false;
        }

        /// <summary>One game frame of <paramref name="dt"/> seconds.</summary>
        public void Frame(float dt, SimInput input)
        {
            T += dt;
            double now = T;
            bool grounded = Grounded;
            if (grounded) { buffers.Grounded(now); usedDouble = false; }

            int steps = stepper.Advance(dt);
            for (int i = 0; i < steps; i++)
            {
                if (spin.Running && spin.Tick()) spinCooldown = V.spinCooldownFrames;
                else if (!spin.Running && spinCooldown > 0) spinCooldown--;
                landLag.Tick();
            }

            if (input.JumpPressed) buffers.PressJump(now);

            bool flatOut = input.Move * input.Move > 0.8f;
            flatOutTime = flatOut ? flatOutTime + dt : 0f;
            bool sprinting = flatOut && (input.Sprint || flatOutTime >= V.autoSprintDelay);
            float mult = (sprinting ? V.sprintMultiplier : 1f) * SpeedMult;
            float jumpWindow = InputBuffers.JumpWindow(V, OneHand), coyoteWindow = InputBuffers.CoyoteWindow(V, OneHand);
            if (landLag.Active)
            {
                if (landLag.CanCancel(V, input.JumpPressed || buffers.JumpBuffered(now, jumpWindow), input.Move > 0.2f)) landLag.Frame = landLag.Total;
                else mult *= landLag.SpeedMult(V);
            }
            var under = Ground.At(Z);
            if (grounded && under != null && input.Move > 0.01f && Math.Abs(under.Rise) > 1e-4f)
                mult *= Slopes.SpeedScale(V, under.NormalY, under.Rise < 0f ? 1f : -1f);
            float top = Math.Max(0.1f, V.runSpeed * mult);
            var desired = new V2(0f, input.Move * top);

            if (grounded) Locomotion.StepGround(V, Run, desired, dt * AccelMult, top);
            else Locomotion.StepAir(V, Run, desired, dt, JumpArc.AtApex(V, Vy));

            bool attackHoldsJump = spin.Running && !spin.CanJumpCancel;
            if (buffers.JumpBuffered(now, jumpWindow) && !attackHoldsJump)
            {
                if (buffers.CoyoteOpen(now, coyoteWindow))
                {
                    Launch(JumpMult); buffers.Left(); buffers.ConsumeJump();
                }
                else if (DoubleJumpAllowed && !usedDouble && !grounded)
                {
                    usedDouble = true; Launch(V.doubleJumpMultiplier); buffers.ConsumeJump();
                }
            }

            if (input.AttackPressed && !grounded && !spin.Running && spinCooldown <= 0)
            {
                spin.Start(AttackFrames.Spin(V));
                if (Vy < 0f) Vy *= V.spinFallDamping;
                jump.VelocityY = Vy;
            }

            bool held = input.JumpHeld || noCut;
            float dy;
            if (grounded && Vy <= 0f)
            {
                float stick = under != null && Run.Velocity.Z > 0.1f ? Slopes.StickSpeed(V, Run.Velocity.Z, under.NormalY, under.Rise < 0f ? 1f : 0f) : V.groundStickSpeed;
                Vy = -stick; jump = new JumpState { VelocityY = Vy }; noCut = false;
                dy = Vy * dt;
            }
            else
            {
                jump.VelocityY = Vy;
                bool fast = input.SlidePressed && Vy <= 0.5f;
                jump = JumpArc.Advance(V, jump, dt, held, fast, out dy);
                Vy = jump.VelocityY;
                if (Vy <= 0f) noCut = false;
            }

            Move(dt, dy);
            Track();
            Trace.Add(new SimSample { T = T, Z = Z, Y = Y, Vy = Vy, Speed = Run.Velocity.Z, Grounded = Grounded });
        }

        private void Launch(float mult)
        {
            Vy = JumpSpeed(V.jumpHeight * mult);
            jump = JumpArc.Launch(Vy); noCut = false; Grounded = false;
            Jumps++; JumpTimes.Add(T); JumpZ.Add(Z);
        }

        /// <summary>Horizontal move first, then vertical, like the character controller.</summary>
        private void Move(float dt, float dy)
        {
            float newZ = Z + Run.Velocity.Z * dt;
            var at = Ground.At(newZ);
            bool wasOn = Grounded;
            if (at != null && at.TopAt(newZ) > Y + 1e-4f)
            {
                float rise = at.TopAt(newZ) - Y;
                if (wasOn && (rise <= V.stepUp || Slopes.AngleDegrees(at.NormalY) <= V.slopeLimit && Math.Abs(at.Rise) > 1e-4f)) { /* stepped or climbed */ }
                else
                {
                    float lift = LedgeForgiveness.Lift(V, Y, at.TopAt(newZ), Vy, T - lastLiftTime);
                    if (lift > 0f) { Y += lift; lastLiftTime = T; Run.Velocity = new V2(0f, LedgeForgiveness.KeepSpeed(V, Run.Velocity.Z)); }
                    else
                    {
                        float? wall = Ground.WallBetween(Z, newZ);
                        newZ = wall.HasValue ? wall.Value - 1e-3f : Z;
                        Run.Velocity = new V2(0f, 0f);
                        at = Ground.At(newZ);
                    }
                }
            }
            Z = newZ;
            float ny = Y + dy;
            var below = Ground.At(Z);
            float topY = below != null ? below.TopAt(Z) : float.NegativeInfinity;
            if (below != null && Vy <= 0.001f && ny <= topY + 1e-4f && Y >= topY - 0.5f)
            {
                Y = topY; Grounded = true;
            }
            else if (below != null && wasOn && topY > Y)
            {
                Y = topY; Grounded = true;   // climbing a ramp or stepping up
            }
            else { Y = ny; Grounded = false; }
            if (Grounded && Vy < 0f) { /* the stick speed is re-applied next frame */ }
        }

        private void Track()
        {
            if (!Grounded) airPeakY = wasGrounded ? Y : Math.Max(airPeakY, Y);
            else if (!wasGrounded)
            {
                float fall = airPeakY - Y;
                landLag = LandLag.For(V, fall, true);
                Landings.Add(new SimLanding { T = T, Z = Z, Y = Y, Fall = fall, Lag = landLag.Total > 0 });
                noCut = false;
            }
            wasGrounded = Grounded;
        }

        // ------------------------------------------------------------ playing a script

        public delegate SimInput Script(FeelSim sim, float t);

        /// <summary>Plays <paramref name="script"/> at a fixed frame rate for <paramref name="seconds"/> and returns the sim.</summary>
        public static FeelSim Play(FeelValues v, Terrain ground, int fps, float seconds, Script script, float startZ = 0f, Action<FeelSim> setup = null)
        {
            var s = new FeelSim(v, ground, startZ);
            setup?.Invoke(s);
            float dt = 1f / fps;
            int frames = (int)Math.Round(seconds * fps);
            for (int i = 0; i < frames; i++) s.Frame(dt, script(s, s.T));
            return s;
        }

        public float PeakY() { float p = float.NegativeInfinity; foreach (var s in Trace) if (s.Y > p) p = s.Y; return p; }

        /// <summary>Seconds from the first take-off to the first landing afterwards.</summary>
        public float AirTime()
        {
            if (JumpTimes.Count == 0) return 0f;
            foreach (var l in Landings) if (l.T > JumpTimes[0]) return l.T - JumpTimes[0];
            return float.NaN;
        }
    }

    /// <summary>The camera's two follow models run on a recorded hero path, for the lag checks.</summary>
    public static class CameraSim
    {
        public struct Sample { public float T, HeroY, CamY, LookAhead; }

        public static List<Sample> Follow(FeelValues v, IList<SimSample> trace, int fps, float topSpeed, float vertWindowMult = 1f, float lookMult = 1f)
        {
            var look = new LookAheadModel(); var vert = new VerticalFollowModel();
            float dt = 1f / fps;
            var list = new List<Sample>();
            bool first = true;
            foreach (var s in trace)
            {
                if (first) { vert.Reset(s.Y); first = false; }
                var off = look.Step(v, new V2(0f, s.Speed), topSpeed, dt, lookMult);
                float cy = vert.Step(v, s.Y, s.Grounded, dt, vertWindowMult);
                list.Add(new Sample { T = s.T, HeroY = s.Y, CamY = cy, LookAhead = off.Z });
            }
            return list;
        }
    }
}
