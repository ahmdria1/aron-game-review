// (c) Ahmed Alamrani. All rights reserved.
using System;

namespace Aron.Logic.GameFeel
{
    /// <summary>
    /// Camera look-ahead: the camera looks a little way in the direction he runs (more the faster he goes), eased so a turn does not whip it
    /// around, and kept for <c>camLookAheadHold</c> after he stops so a short stop does not snap the view back (Crash and Mario both lead the run).
    /// </summary>
    public class LookAheadModel
    {
        public V2 Offset;
        private V2 lastDir;
        private float sinceMoving = 100f;

        public void Reset() { Offset = new V2(0f, 0f); lastDir = new V2(0f, 0f); sinceMoving = 100f; }

        /// <param name="velocity">His ground velocity, m/s.</param>
        /// <param name="topSpeed">The speed that counts as full (for the 0..1 share).</param>
        /// <param name="distanceMult">1 for the free camera, <c>camChaseLookAheadMult</c> for the chase camera.</param>
        public V2 Step(FeelValues v, V2 velocity, float topSpeed, float dt, float distanceMult = 1f)
        {
            float speed = velocity.Length;
            float share = topSpeed > 0f ? Math.Min(1f, speed / topSpeed) : 0f;
            V2 target;
            if (share > 0.15f) { lastDir = velocity.Normalized(); sinceMoving = 0f; target = lastDir * (v.camLookAheadRun * distanceMult * share); }
            else
            {
                sinceMoving += dt;
                target = sinceMoving < v.camLookAheadHold ? Offset : new V2(0f, 0f);
            }
            float k = 1f - (float)Math.Exp(-dt / Math.Max(0.02f, v.camLookAheadSmooth) * 3f);
            Offset = Offset + (target - Offset) * k;
            return Offset;
        }
    }

    /// <summary>
    /// Vertical camera follow that ignores small hops. The camera height stays put while he moves inside a band of <c>camVertWindow</c> metres
    /// above and below it (so ordinary jumps and crate bounces do not bob the screen); once he leaves the window the camera follows just enough to keep
    /// him inside it, smoothly. When he lands somewhere new, it settles onto that height slowly. A long drop catches up quickly.
    /// </summary>
    public class VerticalFollowModel
    {
        public float Y { get; private set; }
        private float vel;
        private bool started;

        public void Reset(float y) { Y = y; vel = 0f; started = true; }

        /// <param name="windowMult">&lt; 1 tightens the window (the chase camera).</param>
        public float Step(FeelValues v, float targetY, bool grounded, float dt, float windowMult = 1f)
        {
            if (!started) Reset(targetY);
            float half = v.camVertWindow * windowMult;   // the dead zone reaches this far above and below the camera height
            float desired;
            if (grounded) desired = targetY;            // settled: aim at the floor he stands on
            else if (targetY > Y + half) desired = targetY - half;
            else if (targetY < Y - half) desired = targetY + half;
            else desired = Y;
            float smooth = grounded ? v.camVertSettleSmooth : v.camVertSmooth;
            float drop = Y - targetY;
            if (drop > v.camFallSnapDistance) { float step = v.camFallSnapSpeed * dt; Y = Math.Max(desired, Y - step); vel = 0f; return Y; }
            // critically damped smoothing toward the desired height
            float omega = 2f / Math.Max(0.02f, smooth);
            float x = omega * dt;
            float exp = 1f / (1f + x + 0.48f * x * x + 0.235f * x * x * x);
            float change = Y - desired;
            float temp = (vel + omega * change) * dt;
            vel = (vel - omega * temp) * exp;
            Y = desired + (change + temp) * exp;
            return Y;
        }
    }

    /// <summary>Boss framing: how the camera weights the hero and the boss.</summary>
    public static class BossFraming
    {
        public static void Focus(FeelValues v, float heroX, float heroZ, float bossX, float bossZ, out float x, out float z)
        {
            x = heroX * v.camBossHeroWeight + bossX * (1f - v.camBossHeroWeight);
            z = heroZ * v.camBossHeroWeight + bossZ * (1f - v.camBossHeroWeight);
        }

        /// <summary>A big boss rises above the hero in frame; this lifts the look point a little the further away it is, up to <c>camBossLift</c>.</summary>
        public static float Lift(FeelValues v, float gap) => v.camBossLift * Math.Min(1f, gap / 12f);
    }

    /// <summary>Stick and touch shaping: dead zone, outer zone, response curve, and the one-hand variants.</summary>
    public static class InputShaping
    {
        /// <summary>
        /// The shaped move vector. Below the dead zone it is zero; the range from the dead zone to the outer zone is rescaled to 0..1 (so there is no
        /// jump at the edge of the dead zone), then raised to <c>stickCurve</c> (above 1 gives fine control near the middle). One-hand mode widens the
        /// dead zone a little, flattens the curve and gains the stick, so one thumb reaches full speed with less travel.
        /// </summary>
        public static void Apply(FeelValues v, float x, float y, bool oneHand, out float ox, out float oy)
        {
            float mag = (float)Math.Sqrt(x * x + y * y);
            float dead = v.stickDeadZone + (oneHand ? v.oneHandDeadZoneAdd : 0f);
            float outer = v.stickOuterZone;
            float curve = oneHand ? v.oneHandCurve : v.stickCurve;
            if (mag <= dead || mag < 1e-6f) { ox = 0f; oy = 0f; return; }
            float m = mag * (oneHand ? v.oneHandGain : 1f);
            float t = (m - dead) / Math.Max(0.01f, outer - dead);
            t = t < 0f ? 0f : t > 1f ? 1f : t;
            t = (float)Math.Pow(t, curve);
            ox = x / mag * t; oy = y / mag * t;
        }
    }
}
