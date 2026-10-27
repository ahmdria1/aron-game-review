// (c) Ahmed Alamrani. All rights reserved.
using System;

namespace Aron.Logic
{
    public readonly struct V3
    {
        public readonly float x, y, z;
        public V3(float x, float y, float z) { this.x = x; this.y = y; this.z = z; }
        public static V3 operator +(V3 a, V3 b) => new V3(a.x + b.x, a.y + b.y, a.z + b.z);
        public static V3 operator -(V3 a, V3 b) => new V3(a.x - b.x, a.y - b.y, a.z - b.z);
        public static V3 operator *(V3 a, float k) => new V3(a.x * k, a.y * k, a.z * k);
        public float Length => (float)Math.Sqrt(x * x + y * y + z * z);
        public static V3 Lerp(V3 a, V3 b, float t) => a + (b - a) * t;
    }

    /// <summary>
    /// Where a point of the thobe's skirt sits while the front hem is held in the teeth. The skirt is a set of
    /// columns from the waist down to the hem. The front column's hem edge goes to the mouth, the ones beside it
    /// get pulled up and inward toward it in a diagonal, and the cloth between waist and hem folds over in front
    /// of the thighs. The back and sides ride up to about the top of the thigh, so the legs show from every side.
    /// </summary>
    public static class HemShape
    {
        /// <summary>How far the back and sides ride up, as a share of the waist to hem drop.</summary>
        public const float SideLift = 0.85f;
        /// <summary>How far the lifted cloth stands off the body, in metres before scaling.</summary>
        public const float StandOff = 0.14f;

        /// <param name="t">0 at the waist, 1 at the hem edge.</param>
        /// <param name="front">1 for the middle of the front panel, falling to 0 at the sides and back.</param>
        /// <param name="hanging">This point with the thobe hanging normally.</param>
        /// <param name="waist">The top of this column, on the waistband.</param>
        /// <param name="hemHanging">The bottom of this column with the thobe hanging normally.</param>
        /// <param name="mouth">Where the teeth hold the edge.</param>
        /// <param name="raise">0 hanging, 1 the edge at the mouth.</param>
        public static V3 Point(float t, float front, V3 hanging, V3 waist, V3 hemHanging, V3 mouth, V3 up, V3 fwd, float raise, float scale)
        {
            if (raise <= 0f) return hanging;
            float drop = waist.y - hemHanging.y;
            V3 edge = Edge(front, hemHanging, mouth, up, fwd, raise, drop, scale);

            // Cloth from the waist to the edge is longer than the straight line once the edge is above the waist,
            // so it sags into a fold in front of the thighs.
            float straight = (edge - waist).Length;
            float slack = Math.Max(0f, drop - straight);
            V3 control = waist - up * (slack * 0.5f * front) + fwd * (StandOff * scale * (0.5f + front));
            float u = 1f - t;
            V3 lifted = waist * (u * u) + control * (2f * u * t) + edge * (t * t);
            return V3.Lerp(hanging, lifted, Clamp01(raise / 0.35f));
        }

        /// <summary>The hem edge of a column. The middle of the front reaches the mouth exactly.</summary>
        public static V3 Edge(float front, V3 hemHanging, V3 mouth, V3 up, V3 fwd, float raise, float drop, float scale)
        {
            float arc = (float)Math.Sin(Math.PI * raise);
            V3 side = hemHanging + up * (drop * SideLift * raise);
            V3 centre = V3.Lerp(hemHanging, mouth, raise) + fwd * (StandOff * scale * arc);
            return V3.Lerp(side, centre, front);
        }

        private static float Clamp01(float v) => v < 0f ? 0f : v > 1f ? 1f : v;
    }
}
