// (c) Ahmed Alamrani. All rights reserved.
using System;

namespace Aron.Logic.GameFeel
{
    /// <summary>The vertical state of a jump, advanced in fixed steps.</summary>
    public struct JumpState
    {
        public float VelocityY;
        public float TimeSinceJump;
        /// <summary>The button was let go while he was still rising: the rest of the rise is cut short.</summary>
        public bool Cut;
        public bool FastFall;
    }

    /// <summary>
    /// The jump arc, from the gravity shaping people call "game feel": a gravity of 30 with the rise a touch lighter than the fall
    /// (<c>fallGravityMult</c>) so he leaves the ground fast and does not float down, a lighter patch at the apex (<c>apexGravityMult</c>, a
    /// short hang that makes platform landings easier to aim, Celeste and Super Mario Odyssey both do it), a much heavier gravity once the button is
    /// released while rising (<c>jumpCutGravityMult</c>, variable height without the velocity snap), an optional fast fall, and a terminal speed.
    /// <see cref="VelocityForHeight"/> solves the take-off speed numerically, so the peak is exactly <c>jumpHeight</c> whatever the shaping is.
    /// </summary>
    public static class JumpArc
    {
        /// <summary>The longest step the arc is integrated with, in seconds. The controller sub-steps every frame to at most this, and the take-off speed is solved with it, so the peak is the same at any frame rate.</summary>
        public const float SubStep = 1f / 120f;

        public static bool AtApex(FeelValues v, float vy) => Math.Abs(vy) < v.apexThreshold;

        /// <summary>The gravity (a positive magnitude, pulling down) for this state.</summary>
        public static float GravityFor(FeelValues v, in JumpState s, bool jumpHeld)
        {
            float g = v.gravity;
            if (s.VelocityY > 0f)
            {
                bool cutting = s.Cut || (!jumpHeld && s.TimeSinceJump >= v.jumpCutDelay);
                if (cutting) return g * v.jumpCutGravityMult;
                if (AtApex(v, s.VelocityY)) return g * v.apexGravityMult;
                return g * v.riseGravityMult;
            }
            if (s.FastFall) return g * v.fastFallGravityMult;
            if (AtApex(v, s.VelocityY)) return g * v.apexGravityMult;
            return g * v.fallGravityMult;
        }

        public static float MaxFall(FeelValues v, in JumpState s) => s.FastFall ? v.fastFallMaxSpeed : v.maxFallSpeed;

        /// <summary>Advances one step with semi-implicit Euler, the same scheme the controller uses, and returns the new state.</summary>
        public static JumpState Step(FeelValues v, JumpState s, float dt, bool jumpHeld, bool fastFallInput = false)
        {
            if (fastFallInput && s.VelocityY <= 0f) s.FastFall = true;
            if (s.VelocityY > 0f && !jumpHeld && s.TimeSinceJump >= v.jumpCutDelay) s.Cut = true;
            float g = GravityFor(v, s, jumpHeld);
            s.VelocityY -= g * dt;
            float cap = MaxFall(v, s);
            if (s.VelocityY < -cap) s.VelocityY = -cap;
            s.TimeSinceJump += dt;
            return s;
        }

        /// <summary>
        /// Advances one frame in sub-steps of at most 1/120 s and returns how far he moved up (negative: down) over the frame. The distance is summed
        /// from the sub-steps, not taken as end-of-frame speed times the frame time: that shortcut loses half a gravity step per frame, which is
        /// about 3 % of the jump height at 60 fps and 15 % at 30 fps, so every phone would have jumped a different height.
        /// </summary>
        public static JumpState Advance(FeelValues v, JumpState s, float dt, bool jumpHeld, bool fastFallInput, out float displacement)
        {
            int sub = Math.Max(1, Math.Min(8, (int)Math.Ceiling(dt / SubStep - 1e-4f)));
            float h = dt / sub, dy = 0f;
            for (int i = 0; i < sub; i++)
            {
                s = Step(v, s, h, jumpHeld, fastFallInput && i == 0);
                dy += s.VelocityY * h;
            }
            displacement = dy;
            return s;
        }

        /// <summary>Resets the state for a fresh take-off.</summary>
        public static JumpState Launch(float velocityY) => new JumpState { VelocityY = velocityY };

        /// <summary>The height reached if the button is held all the way up, simulated at the given step.</summary>
        public static float PeakHeight(FeelValues v, float launchSpeed, float dt = SubStep)
        {
            var s = Launch(launchSpeed); float y = 0f, peak = 0f;
            for (int i = 0; i < 20000 && (s.VelocityY > 0f || i == 0); i++)
            {
                s = Step(v, s, dt, true);
                y += s.VelocityY * dt;
                if (y > peak) peak = y;
            }
            return peak;
        }

        /// <summary>The take-off speed that makes a held jump peak at exactly <paramref name="height"/> (bisection over the simulated arc).</summary>
        public static float VelocityForHeight(FeelValues v, float height, float dt = SubStep)
        {
            float lo = 0.1f, hi = (float)Math.Sqrt(2f * v.gravity * Math.Max(0.1f, height)) * 3f + 5f;
            for (int i = 0; i < 40; i++)
            {
                float mid = 0.5f * (lo + hi);
                if (PeakHeight(v, mid, dt) < height) lo = mid; else hi = mid;
            }
            return 0.5f * (lo + hi);
        }

        /// <summary>Seconds from take-off to the top of a held jump.</summary>
        public static float TimeToApex(FeelValues v, float launchSpeed, float dt = SubStep)
        {
            var s = Launch(launchSpeed); float t = 0f;
            for (int i = 0; i < 20000 && s.VelocityY > 0f; i++) { s = Step(v, s, dt, true); t += dt; }
            return t;
        }

        /// <summary>Seconds the hero spends within the apex band at the top (the "hang").</summary>
        public static float ApexHang(FeelValues v, float launchSpeed, float dt = SubStep)
        {
            var s = Launch(launchSpeed); float t = 0f;
            for (int i = 0; i < 40000; i++)
            {
                s = Step(v, s, dt, true);
                if (AtApex(v, s.VelocityY)) t += dt;
                if (s.VelocityY < -v.apexThreshold * 3f) break;
            }
            return t;
        }
    }
}
