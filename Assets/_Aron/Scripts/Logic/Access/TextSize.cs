// (c) Ahmed Alamrani. All rights reserved.
using System;

namespace Aron.Logic.Access
{
    /// <summary>The text size option: every UI font is multiplied by one number, and it never grows past what a button can hold.</summary>
    public static class TextSizeRules
    {
        public static readonly float[] Steps = { 0.9f, 1f, 1.15f, 1.3f, 1.6f };
        public const float Min = 0.8f, Max = 1.6f;

        /// <summary>The next step, wrapping back to the first after the largest.</summary>
        public static float Next(float current)
        {
            int best = 0;
            for (int i = 0; i < Steps.Length; i++) if (Math.Abs(Steps[i] - current) < Math.Abs(Steps[best] - current)) best = i;
            return Steps[(best + 1) % Steps.Length];
        }

        public static float Clamp(float size) => size < Min ? Min : size > Max ? Max : size;

        /// <summary>The font size to use for a base size, rounded to a whole point so the text stays sharp.</summary>
        public static int Font(int baseSize, float size) => Math.Max(1, (int)Math.Round(baseSize * Clamp(size), MidpointRounding.AwayFromZero));

        /// <summary>Whole percent for the settings label (1.15 becomes 115).</summary>
        public static int Percent(float size) => (int)Math.Round(size * 100f, MidpointRounding.AwayFromZero);
    }
}
