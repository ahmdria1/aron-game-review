// (c) Ahmed Alamrani. All rights reserved.
using System;
using System.Collections.Generic;
using System.Reflection;

namespace Aron.Logic.GameFeel
{
    /// <summary>Marks a tunable: its panel group, slider range and a one-line note. Every public field of <see cref="FeelValues"/> carries one.</summary>
    [AttributeUsage(AttributeTargets.Field)]
    public sealed class FeelAttribute : Attribute
    {
        public readonly string Group;
        public readonly float Min, Max;
        public readonly string Unit;
        public FeelAttribute(string group, float min, float max, string unit = "") { Group = group; Min = min; Max = max; Unit = unit; }
    }

    /// <summary>
    /// Every number that decides how the hero feels to control, in one plain object (see docs/game_feel.md for why each default is what it is).
    /// It lives in the <c>GameFeelConfig</c> ScriptableObject at run time, ships as Resources/Config/game_feel.json, and the hidden tuning panel
    /// builds one slider per field from the <see cref="FeelAttribute"/> ranges. Public fields only, so JsonUtility and the tests can read it.
    /// Times are seconds, distances metres, speeds m/s, accelerations m/s², attack timings are frames at <see cref="FixedStepper.Rate"/> (60 per second).
    /// </summary>
    [Serializable]
    public class FeelValues
    {
        // ---------------------------------------------------------------- Run
        [Feel("Run", 3f, 14f, "m/s")] public float runSpeed = 7f;
        [Feel("Run", 1f, 2f, "x")] public float sprintMultiplier = 1.3f;
        [Feel("Run", 0f, 6f, "s")] public float autoSprintDelay = 1.6f;
        [Feel("Run", 10f, 140f, "m/s2")] public float accelGround = 46f;
        [Feel("Run", 0.1f, 1f, "x")] public float accelTopShare = 0.55f;
        [Feel("Run", 0.5f, 3f)] public float accelCurve = 1.5f;
        [Feel("Run", 10f, 160f, "m/s2")] public float decelGround = 60f;
        [Feel("Run", 180f, 1600f, "deg/s")] public float turnSpeed = 760f;
        [Feel("Run", 0f, 5f, "m/s")] public float turnInPlaceSpeed = 1.6f;
        [Feel("Run", 40f, 160f, "m/s2")] public float turnInPlaceAccel = 90f;
        [Feel("Run", 1f, 12f, "m/s")] public float skidMinSpeed = 4.5f;
        [Feel("Run", 60f, 179f, "deg")] public float skidAngle = 120f;
        [Feel("Run", 5f, 120f, "m/s2")] public float skidDecel = 36f;
        [Feel("Run", 0f, 4f, "m/s")] public float skidExitSpeed = 1.2f;
        [Feel("Run", 0.05f, 1f, "s")] public float skidMaxTime = 0.4f;
        [Feel("Run", 0f, 1f, "s")] public float skidRecoverSeconds = 0.08f;

        // ---------------------------------------------------------------- Air
        [Feel("Air", 0f, 100f, "m/s2")] public float airAccel = 30f;
        [Feel("Air", 0f, 60f, "m/s2")] public float airDecel = 9f;
        [Feel("Air", 1f, 3f, "x")] public float airReverseMult = 1.4f;
        [Feel("Air", 0.5f, 1.3f, "x")] public float airSpeedMult = 1f;
        [Feel("Air", 180f, 1600f, "deg/s")] public float airTurnSpeed = 560f;
        [Feel("Air", 1f, 3f, "x")] public float apexAccelMult = 1.35f;

        // ---------------------------------------------------------------- Jump
        [Feel("Jump", 0.5f, 5f, "m")] public float jumpHeight = 2.2f;
        [Feel("Jump", 0.5f, 1.2f, "x")] public float doubleJumpMultiplier = 0.85f;
        [Feel("Jump", 10f, 90f, "m/s2")] public float gravity = 30f;
        [Feel("Jump", 0.4f, 1.5f, "x")] public float riseGravityMult = 1f;
        [Feel("Jump", 1f, 6f, "x")] public float jumpCutGravityMult = 2.8f;
        [Feel("Jump", 0f, 0.2f, "s")] public float jumpCutDelay = 0.08f;
        [Feel("Jump", 0f, 8f, "m/s")] public float apexThreshold = 1.6f;
        [Feel("Jump", 0.15f, 1f, "x")] public float apexGravityMult = 0.6f;
        [Feel("Jump", 1f, 3f, "x")] public float fallGravityMult = 1.55f;
        [Feel("Jump", 1f, 5f, "x")] public float fastFallGravityMult = 2.2f;
        [Feel("Jump", 8f, 60f, "m/s")] public float maxFallSpeed = 28f;
        [Feel("Jump", 8f, 80f, "m/s")] public float fastFallMaxSpeed = 38f;
        [Feel("Jump", 0f, 0.3f, "s")] public float coyoteTime = 0.12f;
        [Feel("Jump", 0f, 0.35f, "s")] public float jumpBuffer = 0.15f;

        // ---------------------------------------------------------------- Corners and ledges
        [Feel("Corners", 0f, 0.7f, "m")] public float headCornerNudge = 0.3f;
        [Feel("Corners", 0f, 8f, "m/s")] public float headBonkDownSpeed = 2f;
        [Feel("Corners", 0f, 0.9f, "m")] public float ledgeForgiveHeight = 0.35f;
        [Feel("Corners", 0f, 10f, "m/s")] public float ledgeForgiveMaxRise = 3f;
        [Feel("Corners", 0f, 0.5f, "s")] public float ledgeForgiveCooldown = 0.12f;
        [Feel("Corners", 0f, 1f, "x")] public float ledgeForgiveKeepSpeed = 1f;

        // ---------------------------------------------------------------- Slopes
        [Feel("Slopes", 0f, 0.9f, "m")] public float stepUp = 0.45f;
        [Feel("Slopes", 25f, 75f, "deg")] public float slopeLimit = 50f;
        [Feel("Slopes", 0f, 0.7f, "x")] public float slopeUphillSlow = 0.25f;
        [Feel("Slopes", 0f, 0.5f, "x")] public float slopeDownhillBoost = 0.1f;
        [Feel("Slopes", 0f, 20f, "m/s")] public float slopeSlideSpeed = 8f;
        [Feel("Slopes", 0f, 15f, "m/s")] public float groundStickSpeed = 6f;

        // ---------------------------------------------------------------- Landing
        [Feel("Landing", 0f, 3f, "m")] public float landFxMinFall = 0.6f;
        [Feel("Landing", 0f, 0.5f, "x")] public float landSquashMax = 0.26f;
        [Feel("Landing", 1f, 12f, "m")] public float landSquashFullFall = 6f;
        [Feel("Landing", 0.05f, 0.6f, "s")] public float landSquashTime = 0.2f;
        [Feel("Landing", 1f, 4f, "n")] public int landDustBursts = 1;
        [Feel("Landing", 0.5f, 12f, "m")] public float landLagMinFall = 3.2f;
        [Feel("Landing", 0f, 24f, "frames")] public int landLagFrames = 6;
        [Feel("Landing", 0f, 1f, "x")] public float landLagSpeedMult = 0.45f;
        [Feel("Landing", 0f, 24f, "frames")] public int landLagMoveCancelFrame = 3;

        // ---------------------------------------------------------------- Spin (air attack), in 60 Hz frames
        [Feel("Spin", 0f, 12f, "frames")] public int spinStartupFrames = 2;
        [Feel("Spin", 4f, 40f, "frames")] public int spinActiveFrames = 16;
        [Feel("Spin", 0f, 24f, "frames")] public int spinRecoveryFrames = 6;
        [Feel("Spin", 0f, 60f, "frames")] public int spinCooldownFrames = 18;
        [Feel("Spin", 0f, 60f, "frames")] public int spinJumpCancelFrame = 10;
        [Feel("Spin", 0.5f, 3f, "m")] public float spinRadius = 1.4f;
        [Feel("Spin", 0f, 1f, "x")] public float spinFallDamping = 0.3f;
        [Feel("Spin", 360f, 2880f, "deg/s")] public float spinTurnSpeed = 1440f;

        // ---------------------------------------------------------------- Melee, in 60 Hz frames
        [Feel("Melee", 0f, 20f, "frames")] public int meleeStartupFrames = 5;
        [Feel("Melee", 2f, 30f, "frames")] public int meleeActiveFrames = 10;
        [Feel("Melee", 0f, 30f, "frames")] public int meleeRecoveryFrames = 9;
        [Feel("Melee", 0f, 60f, "frames")] public int meleeJumpCancelFrame = 15;
        [Feel("Melee", 0f, 30f, "frames")] public int meleeBufferFrames = 9;
        [Feel("Melee", 0f, 1f, "x")] public float meleeMoveMult = 0.55f;
        [Feel("Melee", 0f, 8f, "frames")] public int hitStopFrames = 3;

        // ---------------------------------------------------------------- Crates and stomps
        [Feel("Bounce", 0.5f, 5f, "m")] public float stompBounceHeight = 1.6f;
        [Feel("Bounce", 1f, 8f, "m")] public float crateBounceHeight = 3.2f;
        [Feel("Bounce", 1f, 10f, "m")] public float crateBounceHeldHeight = 4.4f;
        [Feel("Bounce", 0f, 0.35f, "s")] public float bounceBufferBefore = 0.14f;
        [Feel("Bounce", 0f, 0.3f, "s")] public float bounceBufferAfter = 0.1f;

        // ---------------------------------------------------------------- Camera
        [Feel("Camera", 0f, 8f, "m")] public float camLookAheadRun = 2.5f;
        [Feel("Camera", 0.05f, 1.5f, "s")] public float camLookAheadSmooth = 0.4f;
        [Feel("Camera", 0f, 1.5f, "s")] public float camLookAheadHold = 0.35f;
        [Feel("Camera", 0f, 4f, "m")] public float camVertWindow = 1.1f;
        [Feel("Camera", 0.05f, 1.2f, "s")] public float camVertSmooth = 0.25f;
        [Feel("Camera", 0.05f, 1.5f, "s")] public float camVertSettleSmooth = 0.4f;
        [Feel("Camera", 1f, 12f, "m")] public float camFallSnapDistance = 2.6f;
        [Feel("Camera", 2f, 40f, "m/s")] public float camFallSnapSpeed = 16f;
        [Feel("Camera", 0.5f, 3f, "x")] public float camChaseLookAheadMult = 1.4f;
        [Feel("Camera", 0f, 2f, "x")] public float camChaseVertWindowMult = 0.7f;
        [Feel("Camera", 0.3f, 0.9f, "x")] public float camBossHeroWeight = 0.6f;
        [Feel("Camera", 0f, 3f, "m")] public float camBossLift = 0.8f;

        // ---------------------------------------------------------------- Input
        [Feel("Input", 0f, 0.4f)] public float stickDeadZone = 0.12f;
        [Feel("Input", 0.6f, 1f)] public float stickOuterZone = 0.95f;
        [Feel("Input", 0.5f, 3f)] public float stickCurve = 1.4f;
        [Feel("Input", 0f, 0.3f)] public float oneHandDeadZoneAdd = 0.04f;
        [Feel("Input", 0.5f, 3f)] public float oneHandCurve = 1.15f;
        [Feel("Input", 0.6f, 2f, "x")] public float oneHandGain = 1.15f;
        [Feel("Input", 0f, 0.2f, "s")] public float oneHandJumpBufferAdd = 0.05f;
        [Feel("Input", 0f, 0.15f, "s")] public float oneHandCoyoteAdd = 0.04f;
        [Feel("Input", 2f, 40f)] public float touchSteerResponse = 14f;
        [Feel("Input", 0f, 0.3f, "in")] public float touchFollowDeadzoneInches = 0.06f;
        [Feel("Input", 0.03f, 0.5f, "in")] public float touchRunDragInches = 0.15f;

        // ---------------------------------------------------------------- Procedural animation: lean, look, breath, squash
        [Feel("Anim lean", 0f, 30f, "deg")] public float leanTurnDegrees = 14f;
        [Feel("Anim lean", 0f, 25f, "deg")] public float leanForwardDegrees = 8f;
        [Feel("Anim lean", 0f, 25f, "deg")] public float leanSkidBackDegrees = 10f;
        [Feel("Anim lean", 0.02f, 0.5f, "s")] public float leanSmooth = 0.12f;
        [Feel("Anim look", 0f, 120f, "deg")] public float lookMaxYaw = 70f;
        [Feel("Anim look", 0f, 60f, "deg")] public float lookMaxPitch = 28f;
        [Feel("Anim look", 0f, 1f, "x")] public float lookHeadWeight = 0.7f;
        [Feel("Anim look", 0f, 1f, "x")] public float lookEyeWeight = 0.9f;
        [Feel("Anim look", 1f, 20f, "m")] public float lookRange = 8f;
        [Feel("Anim look", 0.02f, 0.6f, "s")] public float lookSmooth = 0.18f;
        [Feel("Anim breath", 0.1f, 0.8f, "Hz")] public float breathRate = 0.27f;
        [Feel("Anim breath", 0f, 0.05f)] public float breathAmount = 0.012f;
        [Feel("Anim breath", 1f, 4f, "x")] public float breathExertionMult = 2.3f;
        [Feel("Anim breath", 0.5f, 8f, "s")] public float breathRecoverSeconds = 3f;
        [Feel("Anim squash", 0f, 0.4f, "x")] public float stretchJump = 0.16f;
        [Feel("Anim squash", 0f, 0.4f, "x")] public float stretchFall = 0.08f;
        [Feel("Anim squash", 40f, 600f)] public float squashStiffness = 260f;
        [Feel("Anim squash", 4f, 60f)] public float squashDamping = 15f;

        // ---------------------------------------------------------------- Procedural animation: feet, cloth, flinch, budget
        [Feel("Anim feet", 0f, 1f, "x")] public float footIkWeight = 1f;
        [Feel("Anim feet", 0f, 0.8f, "m")] public float footIkMaxDrop = 0.45f;
        [Feel("Anim feet", 0.1f, 1.2f, "m")] public float footIkRayLength = 0.9f;
        [Feel("Anim feet", 2f, 40f)] public float footIkSmooth = 14f;
        [Feel("Anim feet", 2f, 40f)] public float pelvisSmooth = 9f;
        [Feel("Anim cloth", 0f, 1f)] public float clothStiffness = 0.3f;
        [Feel("Anim cloth", 0.5f, 0.99f)] public float clothDamping = 0.9f;
        [Feel("Anim cloth", 0f, 30f, "m/s2")] public float clothGravity = 7f;
        [Feel("Anim cloth", 0f, 2f, "x")] public float clothInertia = 0.9f;
        [Feel("Anim cloth", 0f, 2f, "x")] public float clothWind = 0.6f;
        [Feel("Anim cloth", 5f, 90f, "deg")] public float clothMaxAngle = 55f;
        [Feel("Anim flinch", 0f, 35f, "deg")] public float flinchDegrees = 14f;
        [Feel("Anim flinch", 0.05f, 0.8f, "s")] public float flinchDuration = 0.28f;
        [Feel("Anim flinch", 0f, 1f, "m")] public float flinchShove = 0.18f;
        [Feel("Anim budget", 0f, 1f, "x")] public float reduceMotionScale = 0.35f;

        // ---------------------------------------------------------------- Contact shadow (the soft disc under every character)
        [Feel("Shadow", 0f, 1f, "x")] public float shadowOpacity = 0.55f;
        [Feel("Shadow", 0.5f, 2f, "x")] public float shadowSize = 1f;
        [Feel("Shadow", 2f, 40f, "m")] public float shadowMaxDistance = 14f;
        [Feel("Shadow", 0.3f, 4f)] public float shadowFadeCurve = 1.6f;
        [Feel("Shadow", 0.1f, 1f, "x")] public float shadowMinScale = 0.4f;
        [Feel("Shadow", 0f, 1f, "x")] public float shadowRealBlend = 0.6f;
        [Feel("Shadow", 0.2f, 6f, "m")] public float shadowRealBlendHeight = 1.5f;
        [Feel("Shadow", 1f, 2.5f, "x")] public float shadowAssistOpacityMult = 1.4f;
        [Feel("Shadow", 1f, 2f, "x")] public float shadowAssistSizeMult = 1.3f;

        // ---------------------------------------------------------------- Helpers
        private static FieldInfo[] fields;
        /// <summary>Every tunable, in declaration order.</summary>
        public static FieldInfo[] Fields
        {
            get
            {
                if (fields != null) return fields;
                var list = new List<FieldInfo>();
                foreach (var f in typeof(FeelValues).GetFields(BindingFlags.Public | BindingFlags.Instance))
                    if (f.GetCustomAttribute<FeelAttribute>() != null) list.Add(f);
                return fields = list.ToArray();
            }
        }

        public static FeelAttribute Info(FieldInfo f) => f.GetCustomAttribute<FeelAttribute>();

        public static List<string> Groups()
        {
            var g = new List<string>();
            foreach (var f in Fields) { var a = Info(f).Group; if (!g.Contains(a)) g.Add(a); }
            return g;
        }

        public float Get(FieldInfo f) => f.FieldType == typeof(int) ? (int)f.GetValue(this) : (float)f.GetValue(this);
        public void Set(FieldInfo f, float v)
        {
            var a = Info(f);
            v = Math.Max(a.Min, Math.Min(a.Max, v));
            if (f.FieldType == typeof(int)) f.SetValue(this, (int)Math.Round(v)); else f.SetValue(this, v);
        }

        public void CopyFrom(FeelValues o) { foreach (var f in Fields) f.SetValue(this, f.GetValue(o)); }
        public FeelValues Clone() { var c = new FeelValues(); c.CopyFrom(this); return c; }

        /// <summary>Pulls every value back inside its slider range (a hand-edited JSON can not break the controller).</summary>
        public void Clamp() { foreach (var f in Fields) Set(f, Get(f)); }

        /// <summary>The problems that would make the controller misbehave (out of range, or settings that contradict each other).</summary>
        public List<string> Validate()
        {
            var p = new List<string>();
            foreach (var f in Fields)
            {
                var a = Info(f); float v = Get(f);
                if (float.IsNaN(v) || v < a.Min - 1e-4f || v > a.Max + 1e-4f) p.Add(f.Name + " " + v + " is outside " + a.Min + ".." + a.Max);
            }
            if (stickOuterZone <= stickDeadZone + 0.2f) p.Add("stickOuterZone must leave room above the dead zone");
            if (fastFallMaxSpeed < maxFallSpeed) p.Add("fastFallMaxSpeed below maxFallSpeed");
            if (crateBounceHeldHeight < crateBounceHeight) p.Add("a held bounce must not be lower than a plain one");
            if (apexGravityMult > fallGravityMult) p.Add("apex gravity above fall gravity");
            if (spinJumpCancelFrame > spinStartupFrames + spinActiveFrames + spinRecoveryFrames) p.Add("spin jump cancel is after the spin ends");
            if (meleeJumpCancelFrame > meleeStartupFrames + meleeActiveFrames + meleeRecoveryFrames) p.Add("melee jump cancel is after the swing ends");
            if (landLagMoveCancelFrame > landLagFrames && landLagFrames > 0) p.Add("land lag cancel frame is after the lag ends");
            return p;
        }
    }
}
