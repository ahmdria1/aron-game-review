// (c) Ahmed Alamrani. All rights reserved.
using System;

namespace Aron.Logic.GameFeel
{
    /// <summary>A flat (x, z) vector; the logic assembly has no engine types.</summary>
    public struct V2
    {
        public float X, Z;
        public V2(float x, float z) { X = x; Z = z; }
        public float Length => (float)Math.Sqrt(X * X + Z * Z);
        public float SqrLength => X * X + Z * Z;
        public static V2 operator +(V2 a, V2 b) => new V2(a.X + b.X, a.Z + b.Z);
        public static V2 operator -(V2 a, V2 b) => new V2(a.X - b.X, a.Z - b.Z);
        public static V2 operator *(V2 a, float s) => new V2(a.X * s, a.Z * s);
        public static float Dot(V2 a, V2 b) => a.X * b.X + a.Z * b.Z;
        public V2 Normalized() { float l = Length; return l > 1e-6f ? new V2(X / l, Z / l) : new V2(0f, 0f); }
        /// <summary>Moves toward <paramref name="target"/> by at most <paramref name="maxDelta"/> (never overshoots).</summary>
        public static V2 MoveTowards(V2 from, V2 target, float maxDelta)
        {
            var d = target - from; float l = d.Length;
            if (l <= maxDelta || l < 1e-6f) return target;
            return from + d * (maxDelta / l);
        }
        /// <summary>The angle between two directions in degrees, 0..180.</summary>
        public static float Angle(V2 a, V2 b)
        {
            a = a.Normalized(); b = b.Normalized();
            if (a.SqrLength < 1e-8f || b.SqrLength < 1e-8f) return 0f;
            double c = Math.Max(-1.0, Math.Min(1.0, Dot(a, b)));
            return (float)(Math.Acos(c) * 180.0 / Math.PI);
        }
    }

    public enum RunPhase { Idle, Accelerating, Cruising, Braking, TurningInPlace, Skidding, SkidRecover }

    /// <summary>What the run model carries from one step to the next.</summary>
    public class RunState
    {
        public V2 Velocity;
        public RunPhase Phase = RunPhase.Idle;
        public float SkidTime, RecoverTime;
        /// <summary>The direction he was heading when a skid began; he is held facing it until it ends.</summary>
        public V2 SkidFrom;
        public bool Skidding => Phase == RunPhase.Skidding;
        public float Speed => Velocity.Length;
    }

    /// <summary>
    /// Ground and air horizontal movement. The ground curve is "snappy start, soft top end" (Crash, Mario): the acceleration starts at
    /// <c>accelGround</c> and falls to <c>accelTopShare</c> of that as he nears top speed, so the first step reacts at once and the last
    /// few km/h arrive smoothly. Releasing the stick brakes harder than it accelerated (<c>decelGround</c>), so he stops where the thumb stops.
    /// A sharp reversal at speed is a skid (he slides, dust, and cannot turn until he has slowed or <c>skidMaxTime</c> ran out); a reversal
    /// at a walk is a turn in place (the velocity is redirected at <c>turnInPlaceAccel</c>, no slide). The air has its own, lower numbers.
    /// </summary>
    public static class Locomotion
    {
        /// <summary>Acceleration at a given share (0..1) of top speed.</summary>
        public static float AccelAt(FeelValues v, float speed01, float traction = 1f)
        {
            float k = (float)Math.Pow(Clamp01(speed01), v.accelCurve);
            return v.accelGround * (1f + (v.accelTopShare - 1f) * k) * Math.Max(0.05f, traction);
        }

        public static bool IsSharpReversal(FeelValues v, V2 velocity, V2 desired)
        {
            if (desired.SqrLength < 1e-4f || velocity.Length < v.skidMinSpeed) return false;
            return V2.Angle(velocity, desired) >= v.skidAngle;
        }

        /// <summary>One grounded step. <paramref name="desired"/> is the target velocity (direction x input x top speed). Traction below 1 (wet stone) softens everything.</summary>
        public static V2 StepGround(FeelValues v, RunState s, V2 desired, float dt, float topSpeed, float traction = 1f)
        {
            float tr = Math.Max(0.05f, traction);
            float speed = s.Velocity.Length;
            bool wants = desired.SqrLength > 1e-4f;

            if (s.Phase == RunPhase.SkidRecover)
            {
                s.RecoverTime -= dt;
                if (s.RecoverTime <= 0f) s.Phase = RunPhase.Idle;
                s.Velocity = V2.MoveTowards(s.Velocity, new V2(0f, 0f), v.skidDecel * tr * dt);
                if (s.RecoverTime > 0f) return s.Velocity;
            }

            if (s.Phase == RunPhase.Skidding)
            {
                s.SkidTime += dt;
                s.Velocity = V2.MoveTowards(s.Velocity, new V2(0f, 0f), v.skidDecel * tr * dt);
                if (s.Velocity.Length <= v.skidExitSpeed || s.SkidTime >= v.skidMaxTime)
                {
                    s.Phase = RunPhase.SkidRecover; s.RecoverTime = v.skidRecoverSeconds;
                }
                return s.Velocity;
            }

            if (wants && IsSharpReversal(v, s.Velocity, desired))
            {
                s.Phase = RunPhase.Skidding; s.SkidTime = 0f; s.SkidFrom = s.Velocity.Normalized();
                s.Velocity = V2.MoveTowards(s.Velocity, new V2(0f, 0f), v.skidDecel * tr * dt);
                return s.Velocity;
            }

            if (!wants)
            {
                s.Velocity = V2.MoveTowards(s.Velocity, new V2(0f, 0f), v.decelGround * tr * dt);
                s.Phase = s.Velocity.SqrLength < 1e-4f ? RunPhase.Idle : RunPhase.Braking;
                return s.Velocity;
            }

            bool reversing = speed > 1e-3f && V2.Angle(s.Velocity, desired) > 100f;
            if (reversing && speed < v.turnInPlaceSpeed)
            {
                s.Phase = RunPhase.TurningInPlace;
                s.Velocity = V2.MoveTowards(s.Velocity, desired, v.turnInPlaceAccel * tr * dt);
                return s.Velocity;
            }

            float a = AccelAt(v, topSpeed > 0f ? speed / topSpeed : 0f, tr);
            // Slowing down to a lower target (half stick) uses the braking rate, not the soft top-end one.
            if (desired.Length < speed - 0.05f && V2.Angle(s.Velocity, desired) < 45f) a = v.decelGround * tr;
            s.Velocity = V2.MoveTowards(s.Velocity, desired, a * dt);
            s.Phase = desired.Length - s.Velocity.Length < 0.05f ? RunPhase.Cruising : RunPhase.Accelerating;
            return s.Velocity;
        }

        /// <summary>One airborne step: separate, lower acceleration, extra bite when reversing, more at the apex, gentle drag with no input.</summary>
        public static V2 StepAir(FeelValues v, RunState s, V2 desired, float dt, bool atApex)
        {
            desired = desired * v.airSpeedMult;
            bool wants = desired.SqrLength > 1e-4f;
            float rate;
            if (!wants) rate = v.airDecel;
            else
            {
                rate = v.airAccel;
                if (s.Velocity.SqrLength > 0.25f && V2.Dot(s.Velocity, desired) < 0f) rate *= v.airReverseMult;
                if (atApex) rate *= v.apexAccelMult;
            }
            s.Velocity = V2.MoveTowards(s.Velocity, wants ? desired : new V2(0f, 0f), rate * dt);
            s.Phase = RunPhase.Cruising;
            return s.Velocity;
        }

        /// <summary>Turn rate in degrees per second for the current phase: fast on the ground, a little lazier in the air, frozen while skidding.</summary>
        public static float TurnRate(FeelValues v, RunPhase phase, bool grounded)
        {
            if (phase == RunPhase.Skidding) return 0f;
            return grounded ? v.turnSpeed : v.airTurnSpeed;
        }

        private static float Clamp01(float x) => x < 0f ? 0f : x > 1f ? 1f : x;
    }
}
