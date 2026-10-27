// (c) Ahmed Alamrani. All rights reserved.
using System;

namespace Aron.Logic.GameFeel
{
    /// <summary>
    /// Head-bonk corner correction (Celeste, Mario): when he jumps up into the very edge of a ceiling, slide him sideways around the corner
    /// instead of stopping him dead. The caller measures how far the blocking corner overlaps his head, and in which direction a free
    /// path lies; this decides whether to nudge and by how much.
    /// </summary>
    public static class CornerCorrection
    {
        /// <param name="overlap">How far the ceiling edge reaches into his head width, metres (&gt; 0).</param>
        /// <param name="clearance">Extra space needed beyond the overlap so he does not clip the corner again.</param>
        /// <returns>The sideways shift to apply (always the overlap plus clearance), or 0 if the corner is too deep to slip past.</returns>
        public static float Nudge(FeelValues v, float overlap, float clearance = 0.02f, float verticalSpeed = 1f)
        {
            if (verticalSpeed <= 0f || overlap <= 0f) return 0f;
            float shift = overlap + clearance;
            return shift <= v.headCornerNudge ? shift : 0f;
        }

        /// <summary>The upward speed left after a full head bonk: he is sent down at <c>headBonkDownSpeed</c>, never kept hanging on the ceiling.</summary>
        public static float AfterBonk(FeelValues v, float verticalSpeed) => verticalSpeed > 0f ? -v.headBonkDownSpeed : verticalSpeed;
    }

    /// <summary>
    /// Ledge forgiveness for the feet (not coyote time: that is about leaving a ledge, this is about arriving at one). If a jump falls just short
    /// and his feet strike the side of a platform within <c>ledgeForgiveHeight</c> of its top, he is lifted onto it and keeps his momentum.
    /// </summary>
    public static class LedgeForgiveness
    {
        /// <returns>The lift in metres to put his feet on the ledge top, or 0 when it is too high, he is rising fast or the cooldown is running.</returns>
        public static float Lift(FeelValues v, float feetY, float ledgeTopY, float verticalSpeed, float sinceLastLift, float clearance = 0.02f)
        {
            if (sinceLastLift < v.ledgeForgiveCooldown) return 0f;
            if (verticalSpeed > v.ledgeForgiveMaxRise) return 0f;
            float gap = ledgeTopY - feetY;
            if (gap <= 0f || gap > v.ledgeForgiveHeight) return 0f;
            return gap + clearance;
        }

        /// <summary>The horizontal speed kept through the lift (1 keeps all of it).</summary>
        public static float KeepSpeed(FeelValues v, float speed) => speed * v.ledgeForgiveKeepSpeed;
    }

    /// <summary>Slope handling: speed on a slope, what counts as too steep, and the slide down it.</summary>
    public static class Slopes
    {
        public static float AngleDegrees(float normalY)
        {
            double c = Math.Max(-1.0, Math.Min(1.0, normalY));
            return (float)(Math.Acos(c) * 180.0 / Math.PI);
        }

        public static bool TooSteep(FeelValues v, float normalY) => AngleDegrees(normalY) > v.slopeLimit;

        /// <summary>
        /// Speed multiplier on a slope. <paramref name="downhillDot"/> is the dot product of the move direction with the downhill direction
        /// (1 straight down, -1 straight up, 0 across). Uphill loses up to <c>slopeUphillSlow</c> at the steepest walkable angle, downhill gains up to
        /// <c>slopeDownhillBoost</c>, across the slope is untouched.
        /// </summary>
        public static float SpeedScale(FeelValues v, float normalY, float downhillDot)
        {
            float t = Math.Min(1f, AngleDegrees(normalY) / Math.Max(1f, v.slopeLimit));
            if (downhillDot < 0f) return 1f - v.slopeUphillSlow * t * -downhillDot;
            return 1f + v.slopeDownhillBoost * t * downhillDot;
        }

        /// <summary>Speed of the slide down a slope too steep to stand on (zero on walkable ground), growing with the steepness.</summary>
        public static float SlideSpeed(FeelValues v, float normalY)
        {
            float a = AngleDegrees(normalY);
            if (a <= v.slopeLimit) return 0f;
            float t = Math.Min(1f, (a - v.slopeLimit) / Math.Max(1f, 90f - v.slopeLimit));
            return v.slopeSlideSpeed * (0.4f + 0.6f * t);
        }

        /// <summary>
        /// The downward speed that keeps his feet on the ground while he runs. On the flat it is <c>groundStickSpeed</c>; running down a slope the ground
        /// falls away at <c>speed x tan(angle)</c>, so the stick has to be faster than that or he leaves the ground every frame and skips down the hill
        /// (the animator flickers, the landing dust fires). <paramref name="downhillDot"/> is 1 straight down the slope, -1 straight up, 0 across.
        /// Only used while he is standing on something: walking off a ledge still starts the fall gently.
        /// </summary>
        public static float StickSpeed(FeelValues v, float horizontalSpeed, float normalY, float downhillDot)
        {
            if (downhillDot <= 0f) return v.groundStickSpeed;
            double a = AngleDegrees(normalY) * Math.PI / 180.0;
            float drop = horizontalSpeed * downhillDot * (float)Math.Tan(Math.Min(a, 1.4)) * 1.25f;
            return Math.Min(v.maxFallSpeed, Math.Max(v.groundStickSpeed, drop));
        }

        /// <summary>Can he step onto something this high without jumping? (The controller's step offset, kept separate from the ledge lift.)</summary>
        public static bool CanStepUp(FeelValues v, float stepHeight) => stepHeight <= v.stepUp + 1e-6f;
    }

    /// <summary>What a landing looks like, from how far he fell.</summary>
    public struct LandingFeel
    {
        public float Squash;       // 0..landSquashMax, the fraction of height lost at the bottom of the squash
        public int DustBursts;     // 0 for a soft step
        public bool Lag;           // a stumble follows (see LandLag)
        public bool Shake;         // a small camera thump

        public static LandingFeel From(FeelValues v, float fallDistance)
        {
            var l = new LandingFeel();
            if (fallDistance < v.landFxMinFall) return l;
            float t = Math.Min(1f, (fallDistance - v.landFxMinFall) / Math.Max(0.01f, v.landSquashFullFall - v.landFxMinFall));
            l.Squash = v.landSquashMax * (0.25f + 0.75f * t);
            l.DustBursts = t > 0.5f ? v.landDustBursts : 1;
            l.Lag = fallDistance >= v.landLagMinFall;
            l.Shake = fallDistance > 3f;
            return l;
        }
    }

    /// <summary>Crate and stomp bounces. The height never depends on how fast he fell, so every crate feels the same; holding jump gives the high bounce.</summary>
    public static class BounceMath
    {
        public static float Velocity(FeelValues v, float height) => (float)Math.Sqrt(2f * v.gravity * Math.Max(0f, height));

        /// <summary>
        /// The bounce height: the high one when the jump button is down at the moment of impact, or was pressed up to <c>bounceBufferBefore</c> before it
        /// (a buffered bounce), or is pressed up to <c>bounceBufferAfter</c> after it.
        /// </summary>
        public static float Height(FeelValues v, float baseHeight, bool jumpHeld, float secondsSinceJumpPress)
        {
            bool boosted = jumpHeld || secondsSinceJumpPress <= v.bounceBufferBefore;
            return boosted ? Math.Max(baseHeight, v.crateBounceHeldHeight * (baseHeight / v.crateBounceHeight)) : baseHeight;
        }

        /// <summary>A press right after the bounce still upgrades it (while he is still rising and inside the window).</summary>
        public static bool LateBoostAllowed(FeelValues v, float secondsSinceBounce, bool alreadyBoosted) => !alreadyBoosted && secondsSinceBounce <= v.bounceBufferAfter;
    }
}
