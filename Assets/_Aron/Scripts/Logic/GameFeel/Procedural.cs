// (c) Ahmed Alamrani. All rights reserved.
using System;

namespace Aron.Logic.GameFeel
{
    /// <summary>How much of the procedural layer runs. Low tier keeps only the cheap parts; reduce-motion turns amplitudes down everywhere.</summary>
    public struct ProcBudget
    {
        public bool Lean, Breath, SquashStretch, Flinch, LookAt, FootIk, Cloth;
        /// <summary>1 normally, <c>reduceMotionScale</c> under reduce-motion.</summary>
        public float Amplitude;

        public static ProcBudget For(FeelValues v, QualityTier tier, bool reduceMotion)
        {
            bool low = tier == QualityTier.Low;
            bool high = tier == QualityTier.High;
            return new ProcBudget
            {
                Lean = true, Breath = true, SquashStretch = true, Flinch = true,
                LookAt = !low,
                FootIk = high,
                Cloth = !low,
                Amplitude = reduceMotion ? v.reduceMotionScale : 1f
            };
        }
    }

    /// <summary>Body lean: into a turn (more at speed), forward with speed, back while skidding. Degrees; a smoothed critically-damped follow.</summary>
    public class LeanModel
    {
        public float Roll, Pitch;
        private float rollVel, pitchVel;

        public void Reset() { Roll = Pitch = rollVel = pitchVel = 0f; }

        /// <param name="turnRate">Signed yaw rate, deg/s, positive turning right.</param>
        public void Step(FeelValues v, float turnRate, float speed01, bool skidding, bool grounded, float dt, float amplitude = 1f)
        {
            float turn01 = Math.Max(-1f, Math.Min(1f, turnRate / Math.Max(1f, v.turnSpeed * 0.5f)));
            float targetRoll = grounded ? -turn01 * v.leanTurnDegrees * (0.4f + 0.6f * speed01) : 0f;
            float targetPitch = (skidding ? -v.leanSkidBackDegrees : v.leanForwardDegrees * speed01) * (grounded ? 1f : 0.5f);
            targetRoll *= amplitude; targetPitch *= amplitude;
            Roll = SmoothDamp(Roll, targetRoll, ref rollVel, v.leanSmooth, dt);
            Pitch = SmoothDamp(Pitch, targetPitch, ref pitchVel, v.leanSmooth, dt);
        }

        public static float SmoothDamp(float current, float target, ref float vel, float smoothTime, float dt)
        {
            float omega = 2f / Math.Max(0.0001f, smoothTime);
            float x = omega * dt;
            float exp = 1f / (1f + x + 0.48f * x * x + 0.235f * x * x * x);
            float change = current - target;
            float temp = (vel + omega * change) * dt;
            vel = (vel - omega * temp) * exp;
            return target + (change + temp) * exp;
        }
    }

    /// <summary>
    /// Head and eye look-at. Picks the nearest of the given points of interest (Noura first when she is within range), limits the turn to
    /// <c>lookMaxYaw</c>/<c>lookMaxPitch</c> from the body's forward, and eases toward it. Beyond the limit it lets go (looks ahead), so he never
    /// wrings his neck, and a target behind him is ignored.
    /// </summary>
    public class LookAtModel
    {
        public float Yaw, Pitch;      // degrees relative to the body, the head's current look
        public float Weight;          // 0..1 how much look-at is applied right now
        private float yawVel, pitchVel, weightVel;

        public void Reset() { Yaw = Pitch = Weight = yawVel = pitchVel = weightVel = 0f; }

        /// <summary>Angles to a point, in degrees relative to the body's forward. dx is to his right, dz forward, dy up, all in the body's frame.</summary>
        public static void AnglesTo(float dx, float dy, float dz, out float yaw, out float pitch)
        {
            yaw = (float)(Math.Atan2(dx, dz) * 180.0 / Math.PI);
            pitch = (float)(Math.Atan2(dy, Math.Sqrt(dx * dx + dz * dz)) * 180.0 / Math.PI);
        }

        /// <summary>Is a point inside the look range and in front of him (within the yaw limit plus a margin)?</summary>
        public static bool CanSee(FeelValues v, float dx, float dy, float dz)
        {
            float dist = (float)Math.Sqrt(dx * dx + dy * dy + dz * dz);
            if (dist > v.lookRange || dist < 0.2f) return false;
            AnglesTo(dx, dy, dz, out float yaw, out _);
            return Math.Abs(yaw) <= v.lookMaxYaw + 25f;
        }

        /// <summary>
        /// Picks which target to look at: the preferred one (Noura) when visible, otherwise the closest visible. Returns -1 for none.
        /// Each point is (dx, dy, dz) relative to the head in the body's frame.
        /// </summary>
        public static int Pick(FeelValues v, float[] dx, float[] dy, float[] dz, int count, int preferred = -1)
        {
            if (preferred >= 0 && preferred < count && CanSee(v, dx[preferred], dy[preferred], dz[preferred])) return preferred;
            int best = -1; float bestD = float.MaxValue;
            for (int i = 0; i < count; i++)
            {
                if (!CanSee(v, dx[i], dy[i], dz[i])) continue;
                float d = dx[i] * dx[i] + dy[i] * dy[i] + dz[i] * dz[i];
                if (d < bestD) { bestD = d; best = i; }
            }
            return best;
        }

        public void Step(FeelValues v, bool hasTarget, float targetYaw, float targetPitch, float dt, float amplitude = 1f)
        {
            float ty = 0f, tp = 0f, tw = 0f;
            if (hasTarget && Math.Abs(targetYaw) <= v.lookMaxYaw + 25f)
            {
                ty = Math.Max(-v.lookMaxYaw, Math.Min(v.lookMaxYaw, targetYaw));
                tp = Math.Max(-v.lookMaxPitch, Math.Min(v.lookMaxPitch, targetPitch));
                tw = 1f;
            }
            Yaw = LeanModel.SmoothDamp(Yaw, ty * amplitude, ref yawVel, v.lookSmooth, dt);
            Pitch = LeanModel.SmoothDamp(Pitch, tp * amplitude, ref pitchVel, v.lookSmooth, dt);
            Weight = LeanModel.SmoothDamp(Weight, tw, ref weightVel, v.lookSmooth, dt);
        }

        /// <summary>The share of the look the head does (the rest is the eyes), so the head leads and the eyes finish the glance.</summary>
        public static float HeadShare(FeelValues v) => v.lookHeadWeight;
    }

    /// <summary>
    /// Breathing: a slow sine on the chest and shoulders. It speeds up and deepens after exertion (a sprint, a hard landing) and settles over
    /// <c>breathRecoverSeconds</c>. The value is the scale to add to the torso (1 + Value).
    /// </summary>
    public class BreathModel
    {
        public float Phase, Exertion;
        public float Value { get; private set; }

        public void Reset() { Phase = Exertion = Value = 0f; }

        public void Step(FeelValues v, float dt, bool exerting, float amplitude = 1f)
        {
            if (exerting) Exertion = Math.Min(1f, Exertion + dt / 1.2f);
            else Exertion = Math.Max(0f, Exertion - dt / Math.Max(0.1f, v.breathRecoverSeconds));
            float rate = v.breathRate * (1f + (v.breathExertionMult - 1f) * Exertion);
            Phase += dt * rate * (float)(2.0 * Math.PI);
            if (Phase > 1000f) Phase -= 1000f;
            float amp = v.breathAmount * (1f + Exertion * 1.5f) * amplitude;
            Value = (float)Math.Sin(Phase) * amp;
        }
    }

    /// <summary>
    /// Squash and stretch as a spring (volume-preserving: when the height changes by s, the width changes by the inverse square root).
    /// Take-off stretches him tall, a fall stretches a little as it speeds up, a landing squashes him flat and the spring rings back.
    /// </summary>
    public class SquashStretchModel
    {
        public float Offset;   // height scale minus 1
        private float vel;

        public void Reset() { Offset = vel = 0f; }

        public void Impulse(float amount) { Offset = amount; vel = 0f; }
        /// <summary>Kicks the spring: a negative height change squashes (landing), a positive one stretches (take-off).</summary>
        public void Kick(float velocity) { vel += velocity; }

        public void Step(FeelValues v, float dt, float airborneVy, bool grounded, float amplitude = 1f)
        {
            float target = 0f;
            if (!grounded && airborneVy < -4f) target = v.stretchFall * Math.Min(1f, (-airborneVy - 4f) / 14f);
            // sub-step the spring so a long frame does not blow it up
            int n = Math.Max(1, (int)Math.Ceiling(dt / 0.008f)); float h = dt / n;
            for (int i = 0; i < n; i++)
            {
                float acc = -v.squashStiffness * (Offset - target * amplitude) - v.squashDamping * vel;
                vel += acc * h; Offset += vel * h;
            }
            Offset = Math.Max(-0.45f, Math.Min(0.45f, Offset));
        }

        public float Height => 1f + Offset;
        /// <summary>Width and depth scale that keeps the volume constant.</summary>
        public float Width => (float)(1.0 / Math.Sqrt(Math.Max(0.4, 1f + Offset)));
    }

    /// <summary>
    /// Foot IK on slopes and steps. Given the ground height under each foot (from a ray) and the foot heights the animation wants, it
    /// returns how far to move each foot and how far to drop the pelvis so the lower foot reaches the ground. The caller blends by
    /// <c>footIkWeight</c> and by how planted each foot is in the clip.
    /// </summary>
    public static class FootIk
    {
        public struct Result { public float LeftLift, RightLift, PelvisDrop; }

        /// <param name="groundL">Ground height under the left foot, world Y. NaN when the ray hit nothing.</param>
        /// <param name="rootY">The height of the character's root (what the animation thinks is the floor).</param>
        public static Result Solve(FeelValues v, float groundL, float groundR, float rootY)
        {
            float l = Offset(groundL, rootY), r = Offset(groundR, rootY);
            l = Math.Max(-v.footIkMaxDrop, Math.Min(v.footIkMaxDrop, l));
            r = Math.Max(-v.footIkMaxDrop, Math.Min(v.footIkMaxDrop, r));
            // The pelvis follows the lower foot so that foot can reach; the higher foot is lifted the rest of the way.
            float drop = Math.Min(0f, Math.Min(l, r));
            return new Result { LeftLift = (l - drop) * v.footIkWeight, RightLift = (r - drop) * v.footIkWeight, PelvisDrop = drop * v.footIkWeight };
        }

        private static float Offset(float ground, float rootY) => float.IsNaN(ground) ? 0f : ground - rootY;

        /// <summary>Foot weight from the clip: 1 while planted, 0 while swinging (the foot's height above its planted height over a small threshold).</summary>
        public static float PlantedWeight(float footHeightAboveRoot, float threshold = 0.12f)
            => 1f - Math.Max(0f, Math.Min(1f, footHeightAboveRoot / Math.Max(0.01f, threshold)));
    }

    /// <summary>A hit reaction: an additive flinch whose direction follows the hit and whose size follows its strength, decaying over <c>flinchDuration</c>.</summary>
    public class FlinchModel
    {
        public float Pitch, Roll, Shove;   // degrees back/forward, degrees sideways, metres pushed away
        private float age = 99f, dirX, dirZ, strength;

        public bool Active => age < 1f;

        /// <summary>The hit came from (fromX, fromZ) relative to him in his own frame (x right, z forward). Strength 0..1.</summary>
        public void Hit(float fromX, float fromZ, float power)
        {
            float l = (float)Math.Sqrt(fromX * fromX + fromZ * fromZ);
            if (l < 1e-4f) { dirX = 0f; dirZ = 1f; } else { dirX = fromX / l; dirZ = fromZ / l; }
            strength = Math.Max(0f, Math.Min(1f, power));
            age = 0f;
        }

        public void Step(FeelValues v, float dt, float amplitude = 1f)
        {
            if (age >= 1f) { Pitch = Roll = Shove = 0f; return; }
            age += dt / Math.Max(0.05f, v.flinchDuration);
            float a = Math.Min(1f, age);
            // fast in, slow out: peaks at 15% of the way, then eases away
            float env = a < 0.15f ? a / 0.15f : (float)Math.Pow(1f - (a - 0.15f) / 0.85f, 2.0);
            if (a >= 1f) env = 0f;
            float k = env * strength * amplitude;
            // a hit from the front rocks him back (negative pitch), from behind pitches him forward, from the side rolls him away
            Pitch = -dirZ * v.flinchDegrees * k;
            Roll = -dirX * v.flinchDegrees * k * 0.8f;
            Shove = -v.flinchShove * k;
        }
    }

    /// <summary>
    /// A cheap verlet chain for secondary motion (shemagh tails, a thobe hem). Joint 0 is pinned to the bone; the others follow with inertia,
    /// gravity and wind, spring back toward the rest pose, keep their segment lengths and stay within <c>clothMaxAngle</c> of their parent.
    /// Positions are plain floats (x, y, z), no engine types, so it is tested like the rest.
    /// </summary>
    public class VerletChain
    {
        public readonly int Count;
        public readonly float[] X, Y, Z, PX, PY, PZ, RestX, RestY, RestZ, Length;

        public VerletChain(int count, float segmentLength)
        {
            Count = Math.Max(2, count);
            X = new float[Count]; Y = new float[Count]; Z = new float[Count];
            PX = new float[Count]; PY = new float[Count]; PZ = new float[Count];
            RestX = new float[Count]; RestY = new float[Count]; RestZ = new float[Count];
            Length = new float[Count];
            for (int i = 0; i < Count; i++) { Length[i] = segmentLength; Y[i] = PY[i] = RestY[i] = -segmentLength * i; }
        }

        /// <summary>Sets the rest pose (where each joint wants to be) in world space and moves the pinned root.</summary>
        public void SetRest(int i, float x, float y, float z) { RestX[i] = x; RestY[i] = y; RestZ[i] = z; }

        public void Snap()
        {
            for (int i = 0; i < Count; i++) { X[i] = PX[i] = RestX[i]; Y[i] = PY[i] = RestY[i]; Z[i] = PZ[i] = RestZ[i]; }
        }

        public void Step(FeelValues v, float dt, float windX, float windZ, float amplitude = 1f, float maxAngleDegrees = -1f)
        {
            if (dt <= 0f) return;
            X[0] = RestX[0]; Y[0] = RestY[0]; Z[0] = RestZ[0];
            float damp = (float)Math.Pow(v.clothDamping, dt * 60f);
            float stiff = Math.Min(1f, v.clothStiffness * dt * 60f * 0.25f);
            for (int i = 1; i < Count; i++)
            {
                float vx = (X[i] - PX[i]) * damp * v.clothInertia, vy = (Y[i] - PY[i]) * damp * v.clothInertia, vz = (Z[i] - PZ[i]) * damp * v.clothInertia;
                PX[i] = X[i]; PY[i] = Y[i]; PZ[i] = Z[i];
                X[i] += vx + windX * v.clothWind * dt * dt * amplitude;
                Y[i] += vy - v.clothGravity * dt * dt * amplitude;
                Z[i] += vz + windZ * v.clothWind * dt * dt * amplitude;
                // spring home
                X[i] += (RestX[i] - X[i]) * stiff; Y[i] += (RestY[i] - Y[i]) * stiff; Z[i] += (RestZ[i] - Z[i]) * stiff;
            }
            double maxCos = Math.Cos((maxAngleDegrees > 0f ? maxAngleDegrees : v.clothMaxAngle) * Math.PI / 180.0);
            for (int pass = 0; pass < 2; pass++)
            {
                for (int i = 1; i < Count; i++)
                {
                    float dx = X[i] - X[i - 1], dy = Y[i] - Y[i - 1], dz = Z[i] - Z[i - 1];
                    float l = (float)Math.Sqrt(dx * dx + dy * dy + dz * dz);
                    if (l < 1e-6f) continue;
                    float f = Length[i] / l;
                    X[i] = X[i - 1] + dx * f; Y[i] = Y[i - 1] + dy * f; Z[i] = Z[i - 1] + dz * f;
                    // keep within the cone around the rest direction of this segment
                    float rx = RestX[i] - RestX[i - 1], ry = RestY[i] - RestY[i - 1], rz = RestZ[i] - RestZ[i - 1];
                    float rl = (float)Math.Sqrt(rx * rx + ry * ry + rz * rz);
                    if (rl < 1e-6f) continue;
                    float ux = (X[i] - X[i - 1]) / Length[i], uy = (Y[i] - Y[i - 1]) / Length[i], uz = (Z[i] - Z[i - 1]) / Length[i];
                    float c = (ux * rx + uy * ry + uz * rz) / rl;
                    if (c < maxCos)
                    {
                        // pull the segment back onto the cone edge: blend the unit vector toward rest until the angle fits
                        float t = 0.5f;
                        for (int k = 0; k < 8 && c < maxCos; k++)
                        {
                            ux += (rx / rl - ux) * t; uy += (ry / rl - uy) * t; uz += (rz / rl - uz) * t;
                            float ul = (float)Math.Sqrt(ux * ux + uy * uy + uz * uz); ux /= ul; uy /= ul; uz /= ul;
                            c = (ux * rx + uy * ry + uz * rz) / rl;
                        }
                        X[i] = X[i - 1] + ux * Length[i]; Y[i] = Y[i - 1] + uy * Length[i]; Z[i] = Z[i - 1] + uz * Length[i];
                    }
                }
            }
        }
    }

    /// <summary>
    /// Which bones the cloth spring may move. Only the shemagh tails and the hems: a bone with "sleeve" in its name is never touched (the sleeves stay rolled),
    /// and Noura's abaya hem is held to a smaller swing so wind or a jump never lifts it past modesty.
    /// </summary>
    public static class SecondaryMotionRules
    {
        public static readonly string[] Prefixes = { "ShemaghTail", "ThobeHem", "AbayaHem" };
        public const float AbayaMaxAngle = 25f;

        public static bool Matches(string boneName)
        {
            if (string.IsNullOrEmpty(boneName)) return false;
            if (boneName.IndexOf("sleeve", StringComparison.OrdinalIgnoreCase) >= 0) return false;
            foreach (var p in Prefixes) if (boneName.StartsWith(p, StringComparison.Ordinal)) return true;
            return false;
        }

        public static float MaxAngle(FeelValues v, string boneName)
            => boneName != null && boneName.StartsWith("AbayaHem", StringComparison.Ordinal) ? Math.Min(v.clothMaxAngle, AbayaMaxAngle) : v.clothMaxAngle;
    }
}
