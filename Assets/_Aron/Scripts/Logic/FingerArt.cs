// (c) Ahmed Alamrani. All rights reserved.
using System;

namespace Aron.Logic
{
    public enum FingerShape { Hand, Ring }

    /// <summary>
    /// The pointing hand and touch ripple used by the gesture tutorial, drawn from distance fields like
    /// <see cref="EmoteArt"/>. The hand points straight up and its fingertip sits at <see cref="TipV"/>.
    /// </summary>
    public static class FingerArt
    {
        /// <summary>Height of the fingertip in the 0..1 texture space, used as the sprite pivot.</summary>
        public const float TipV = 0.94f;

        private const float Outline = 0.06f;

        public static float[] Render(FingerShape shape, int size)
        {
            var px = new float[size * size * 4];
            float aa = 2.2f / size;
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float u = (x + 0.5f) / size * 2f - 1f;
                    float v = (y + 0.5f) / size * 2f - 1f;
                    int i = (y * size + x) * 4;
                    if (shape == FingerShape.Ring)
                    {
                        float ring = Math.Abs(EmoteArt.Circle(u, v, 0f, 0f, 0.78f)) - 0.1f;
                        px[i] = px[i + 1] = px[i + 2] = 1f;
                        px[i + 3] = EmoteArt.Smooth(ring, aa);
                        continue;
                    }

                    float d = Hand(u, v);
                    float inner = EmoteArt.Smooth(d, aa);
                    float outer = EmoteArt.Smooth(d - Outline, aa);
                    float nail = EmoteArt.Smooth(EmoteArt.Segment(u, v, 0.02f, 0.62f, 0.02f, 0.76f, 0.07f), aa) * inner;
                    // Dark outline so it reads on sand and sky alike, warm skin inside, pale nail.
                    float r = 0.18f + (0.95f - 0.18f) * inner, g = 0.12f + (0.78f - 0.12f) * inner, b = 0.09f + (0.62f - 0.09f) * inner;
                    px[i] = r + (1f - r) * nail * 0.7f;
                    px[i + 1] = g + (0.92f - g) * nail * 0.7f;
                    px[i + 2] = b + (0.88f - b) * nail * 0.7f;
                    px[i + 3] = outer;
                }
            }
            return px;
        }

        public static float Hand(float x, float y)
        {
            float index = EmoteArt.Segment(x, y, 0.02f, -0.05f, 0.02f, 0.72f, 0.16f);
            float palm = Box(x - 0.08f, y + 0.5f, 0.32f, 0.3f) - 0.16f;
            float knuckles = Math.Min(EmoteArt.Circle(x, y, 0.26f, -0.12f, 0.15f), EmoteArt.Circle(x, y, 0.44f, -0.22f, 0.13f));
            float thumb = EmoteArt.Segment(x, y, -0.3f, -0.45f, -0.52f, -0.08f, 0.13f);
            return Math.Min(Math.Min(index, palm), Math.Min(knuckles, thumb));
        }

        private static float Box(float x, float y, float hx, float hy)
        {
            float dx = Math.Abs(x) - hx, dy = Math.Abs(y) - hy;
            float ox = Math.Max(dx, 0f), oy = Math.Max(dy, 0f);
            return (float)Math.Sqrt(ox * ox + oy * oy) + Math.Min(Math.Max(dx, dy), 0f);
        }
    }
}
