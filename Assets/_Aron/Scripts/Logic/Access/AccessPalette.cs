// (c) Ahmed Alamrani. All rights reserved.
using System;

namespace Aron.Logic.Access
{
    /// <summary>What a colour on screen means. A role always has the same shape too, so colour is never the only way to tell them apart.</summary>
    public enum ColourRole { Danger, Warning, Safe, Reward, Info }

    public enum MarkShape { Triangle, Diamond, Circle, Star, Square }

    /// <summary>
    /// The colours for HUD elements and hazard markers. With no colour-blind mode they are the game's own colours; with one, each mode has a
    /// palette picked from the Okabe-Ito set (Okabe and Ito 2008) so that no two roles look alike to that player (tests hold every pair to
    /// a Lab distance of at least 24 as that player sees them). Each role also has its own shape.
    /// </summary>
    public static class AccessPalette
    {
        public static readonly ColourRole[] Roles = { ColourRole.Danger, ColourRole.Warning, ColourRole.Safe, ColourRole.Reward, ColourRole.Info };

        // The Okabe-Ito colours
        private static readonly Rgb Vermillion = Rgb.Of255(213, 94, 0), Orange = Rgb.Of255(230, 159, 0), Yellow = Rgb.Of255(240, 228, 66),
            Green = Rgb.Of255(0, 158, 115), Sky = Rgb.Of255(86, 180, 233), Blue = Rgb.Of255(0, 114, 178);

        /// <summary>The game's usual HUD colours (the boss bar red, the timer orange, the bisht gold).</summary>
        private static Rgb Standard(ColourRole r)
        {
            switch (r)
            {
                case ColourRole.Danger: return new Rgb(0.8f, 0.1f, 0.1f);
                case ColourRole.Warning: return new Rgb(1f, 0.55f, 0.35f);
                case ColourRole.Safe: return new Rgb(0f, 0.6f, 0.3f);
                case ColourRole.Reward: return new Rgb(1f, 0.8f, 0.3f);
                default: return new Rgb(0.3f, 0.6f, 1f);
            }
        }

        public static Rgb Colour(ColourMode mode, ColourRole role)
        {
            switch (mode)
            {
                case ColourMode.Protanopia: return Pick(role, Vermillion, Orange, Green, Sky, Blue);
                case ColourMode.Deuteranopia: return Pick(role, Vermillion, Yellow, Green, Sky, Blue);
                case ColourMode.Tritanopia: return Pick(role, Vermillion, Orange, Sky, Yellow, Blue);
                default: return Standard(role);
            }
        }

        // order: danger, warning, safe, reward, info
        private static Rgb Pick(ColourRole role, Rgb danger, Rgb warning, Rgb safe, Rgb reward, Rgb info)
        {
            switch (role)
            {
                case ColourRole.Danger: return danger;
                case ColourRole.Warning: return warning;
                case ColourRole.Safe: return safe;
                case ColourRole.Reward: return reward;
                default: return info;
            }
        }

        public static MarkShape Shape(ColourRole role)
        {
            switch (role)
            {
                case ColourRole.Danger: return MarkShape.Triangle;
                case ColourRole.Warning: return MarkShape.Diamond;
                case ColourRole.Safe: return MarkShape.Circle;
                case ColourRole.Reward: return MarkShape.Star;
                default: return MarkShape.Square;
            }
        }

        /// <summary>The smallest distance between any two role colours as a player with <paramref name="seenAs"/> sees them.</summary>
        public static float MinDistance(ColourMode palette, ColourMode seenAs)
        {
            float min = float.MaxValue;
            for (int i = 0; i < Roles.Length; i++)
                for (int j = i + 1; j < Roles.Length; j++)
                    min = Math.Min(min, ColourVision.Distance(Colour(palette, Roles[i]), Colour(palette, Roles[j]), seenAs));
            return min;
        }
    }

    /// <summary>The outline of each marker shape, as a mask the Unity side turns into a texture.</summary>
    public static class ShapeMask
    {
        /// <summary>Is the point (x, y), both -1..1 with y up, inside the shape?</summary>
        public static bool Inside(MarkShape s, float x, float y)
        {
            float ax = Math.Abs(x), ay = Math.Abs(y);
            switch (s)
            {
                case MarkShape.Triangle: return y >= -0.8f && y <= 0.8f && ax <= (0.8f - y) * 0.5f;
                case MarkShape.Diamond: return ax + ay <= 0.9f;
                case MarkShape.Circle: return x * x + y * y <= 0.64f;
                case MarkShape.Star: return Math.Sqrt(ax) + Math.Sqrt(ay) <= 1f;
                default: return Math.Max(ax, ay) <= 0.7f;
            }
        }

        /// <summary>Row-major pixels, row 0 at the bottom, true where the shape is.</summary>
        public static bool[] Pixels(MarkShape s, int size)
        {
            var px = new bool[size * size];
            for (int row = 0; row < size; row++)
                for (int col = 0; col < size; col++)
                    px[row * size + col] = Inside(s, (col + 0.5f) / size * 2f - 1f, (row + 0.5f) / size * 2f - 1f);
            return px;
        }

        /// <summary>True when a pixel is inside the shape but touches the outside: the rim that gets a dark outline.</summary>
        public static bool IsRim(bool[] px, int size, int col, int row)
        {
            if (!px[row * size + col]) return false;
            for (int dy = -1; dy <= 1; dy++)
                for (int dx = -1; dx <= 1; dx++)
                {
                    int c = col + dx, r = row + dy;
                    if (c < 0 || r < 0 || c >= size || r >= size || !px[r * size + c]) return true;
                }
            return false;
        }
    }
}
