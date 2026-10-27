// (c) Ahmed Alamrani. All rights reserved.
using System;

namespace Aron.Logic.Access
{
    /// <summary>The colour-blind options. Each one is also a palette for the screen and the hazard markers (see <see cref="AccessPalette"/>).</summary>
    public enum ColourMode { None, Protanopia, Deuteranopia, Tritanopia }

    /// <summary>A colour in sRGB, each channel 0..1.</summary>
    public readonly struct Rgb
    {
        public readonly float R, G, B;
        public Rgb(float r, float g, float b) { R = r; G = g; B = b; }
        public static Rgb Of255(int r, int g, int b) => new Rgb(r / 255f, g / 255f, b / 255f);
    }

    /// <summary>
    /// Colour-vision maths: what a colour looks like with one kind of colour blindness (the Machado, Oliveira and Fernandes 2009 matrices at
    /// full severity, applied in linear light) and how far apart two colours look (CIE76 distance in Lab). Used by the palette tests so a
    /// pair of colours that merge for a player is caught before it ships.
    /// </summary>
    public static class ColourVision
    {
        private static readonly float[][] Protan =
        {
            new[] { 0.152286f, 1.052583f, -0.204868f },
            new[] { 0.114503f, 0.786281f, 0.099216f },
            new[] { -0.003882f, -0.048116f, 1.051998f },
        };

        private static readonly float[][] Deutan =
        {
            new[] { 0.367322f, 0.860646f, -0.227968f },
            new[] { 0.280085f, 0.672501f, 0.047413f },
            new[] { -0.011820f, 0.042940f, 0.968881f },
        };

        private static readonly float[][] Tritan =
        {
            new[] { 1.255528f, -0.076749f, -0.178779f },
            new[] { -0.078411f, 0.930809f, 0.147602f },
            new[] { 0.004733f, 0.691367f, 0.303900f },
        };

        public static ColourMode Parse(string name)
        {
            switch (name)
            {
                case "protanopia": return ColourMode.Protanopia;
                case "deuteranopia": return ColourMode.Deuteranopia;
                case "tritanopia": return ColourMode.Tritanopia;
                default: return ColourMode.None;
            }
        }

        /// <summary>The name stored in the save file (AccessSettings.colourFilter).</summary>
        public static string Name(ColourMode m)
        {
            switch (m)
            {
                case ColourMode.Protanopia: return "protanopia";
                case ColourMode.Deuteranopia: return "deuteranopia";
                case ColourMode.Tritanopia: return "tritanopia";
                default: return "none";
            }
        }

        /// <summary>The next mode in the list, wrapping around.</summary>
        public static ColourMode Next(ColourMode m) => (ColourMode)(((int)m + 1) % 4);

        /// <summary>The settings label key for a mode.</summary>
        public static string LabelKey(ColourMode m) => "cvd_" + Name(m);

        public static float ToLinear(float c) => c <= 0.04045f ? c / 12.92f : (float)Math.Pow((c + 0.055f) / 1.055f, 2.4f);

        public static float ToSrgb(float c)
        {
            c = Clamp01(c);
            return c <= 0.0031308f ? c * 12.92f : 1.055f * (float)Math.Pow(c, 1f / 2.4f) - 0.055f;
        }

        private static float Clamp01(float v) => v < 0f ? 0f : v > 1f ? 1f : v;

        /// <summary>How a colour looks to someone with the given colour blindness. <see cref="ColourMode.None"/> returns it unchanged.</summary>
        public static Rgb Simulate(Rgb c, ColourMode mode)
        {
            if (mode == ColourMode.None) return c;
            var m = mode == ColourMode.Protanopia ? Protan : mode == ColourMode.Deuteranopia ? Deutan : Tritan;
            float r = ToLinear(c.R), g = ToLinear(c.G), b = ToLinear(c.B);
            float nr = m[0][0] * r + m[0][1] * g + m[0][2] * b;
            float ng = m[1][0] * r + m[1][1] * g + m[1][2] * b;
            float nb = m[2][0] * r + m[2][1] * g + m[2][2] * b;
            return new Rgb(ToSrgb(nr), ToSrgb(ng), ToSrgb(nb));
        }

        /// <summary>CIE L*a*b* (D65) of an sRGB colour.</summary>
        public static void ToLab(Rgb c, out float l, out float a, out float b)
        {
            float r = ToLinear(c.R), g = ToLinear(c.G), bl = ToLinear(c.B);
            float x = (0.4124564f * r + 0.3575761f * g + 0.1804375f * bl) / 0.95047f;
            float y = 0.2126729f * r + 0.7151522f * g + 0.0721750f * bl;
            float z = (0.0193339f * r + 0.1191920f * g + 0.9503041f * bl) / 1.08883f;
            float fx = F(x), fy = F(y), fz = F(z);
            l = 116f * fy - 16f; a = 500f * (fx - fy); b = 200f * (fy - fz);
        }

        private static float F(float t) => t > 0.008856f ? (float)Math.Pow(t, 1.0 / 3.0) : 7.787f * t + 16f / 116f;

        /// <summary>Distance between two colours in Lab. About 2.3 is the smallest difference an eye can see; below about 20 two colours are easily confused.</summary>
        public static float DeltaE(Rgb x, Rgb y)
        {
            ToLab(x, out float l1, out float a1, out float b1);
            ToLab(y, out float l2, out float a2, out float b2);
            float dl = l1 - l2, da = a1 - a2, db = b1 - b2;
            return (float)Math.Sqrt(dl * dl + da * da + db * db);
        }

        /// <summary>The distance between two colours as the given player sees them.</summary>
        public static float Distance(Rgb x, Rgb y, ColourMode mode) => DeltaE(Simulate(x, mode), Simulate(y, mode));
    }
}
