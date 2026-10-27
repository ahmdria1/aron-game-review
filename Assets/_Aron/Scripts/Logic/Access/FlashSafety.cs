// (c) Ahmed Alamrani. All rights reserved.
using System;
using Aron.Logic.Safety;

namespace Aron.Logic.Access
{
    /// <summary>
    /// Flashing and photosensitivity. The guidance is no more than three flashes in any one second, so no blink in the game runs
    /// faster than <see cref="GuidanceHz"/> whatever the settings. With "reduce flashing" or the
    /// photosensitivity-safe switch on, every blink and flash in the game is held to <see cref="ReducedHz"/> or fewer a second and made
    /// less bright; the safe switch also turns on reduce motion, so camera shake drops to a quarter and the hit freeze goes away.
    /// </summary>
    public static class FlashSafety
    {
        /// <summary>The most flashes in a second the guidance allows.</summary>
        public const float GuidanceHz = 3f;
        /// <summary>The most any blink runs at when flashing is reduced; under the guidance with room to spare.</summary>
        public const float ReducedHz = 2f;
        /// <summary>How much of a colour flash is kept when flashing is reduced.</summary>
        public const float ReducedStrength = 0.5f;

        public static bool FlashReduced(AccessSettings a) => a != null && (a.reduceFlash || a.photoSafe);
        public static bool MotionReduced(AccessSettings a) => a != null && (a.reduceMotion || a.photoSafe);

        /// <summary>
        /// The rate a blink is allowed to run at, given the rate the effect asks for: never above <see cref="GuidanceHz"/>, and no
        /// more than <see cref="ReducedHz"/> when flashing is reduced.
        /// </summary>
        public static float Hz(AccessSettings a, float requested)
            => Math.Min(Math.Max(0f, requested), FlashReduced(a) ? ReducedHz : GuidanceHz);

        /// <summary>How strong a colour flash is allowed to be (0..1 of what the effect asks for).</summary>
        public static float Strength(AccessSettings a, float requested)
            => Math.Max(0f, Math.Min(1f, FlashReduced(a) ? requested * ReducedStrength : requested));

        /// <summary>The camera-shake multiplier (0..1): the player's shake level, held to the reduced cap when motion is reduced.</summary>
        public static float ShakeScale(AccessSettings a, Aron.Logic.CameraRig.CameraSettings camera)
            => (camera ?? new Aron.Logic.CameraRig.CameraSettings()).ShakeScale(MotionReduced(a));

        /// <summary>
        /// A square-wave blink: on for <paramref name="onFraction"/> of each cycle. <paramref name="phase"/> is a time or a counting-down
        /// timer in seconds; only its fractional position in the cycle matters.
        /// </summary>
        public static bool BlinkOn(AccessSettings a, float phase, float requestedHz, float onFraction = 0.5f)
        {
            float hz = Hz(a, requestedHz);
            float cycle = phase * hz;
            float frac = cycle - (float)Math.Floor(cycle);
            return frac >= 1f - Math.Max(0f, Math.Min(1f, onFraction));
        }

        /// <summary>How many times a square wave of this rate turns on during one second (for tests and tuning).</summary>
        public static int FlashesIn(AccessSettings a, float requestedHz, float onFraction, float from, float seconds, int samplesPerSecond = 2000)
        {
            int flashes = 0;
            bool last = BlinkOn(a, from, requestedHz, onFraction);
            int n = (int)(seconds * samplesPerSecond);
            for (int i = 1; i <= n; i++)
            {
                bool on = BlinkOn(a, from + i / (float)samplesPerSecond, requestedHz, onFraction);
                if (on && !last) flashes++;
                last = on;
            }
            return flashes;
        }
    }
}
