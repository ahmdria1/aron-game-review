// (c) Ahmed Alamrani. All rights reserved.
using System;

namespace Aron.Logic
{
    /// <summary>
    /// Draws the little emote icons (star, rain cloud, anger mark, "!!", heart, sweat drop) with signed distance
    /// shapes, so no image files are needed. Output is RGBA in 0..1, rows bottom to top like Texture2D.SetPixels.
    /// </summary>
    public static class EmoteArt
    {
        private const float Outline = 0.07f;

        public static float[] Render(Emotion e, int size)
        {
            var px = new float[size * size * 4];
            if (e == Emotion.Neutral) return px;
            Fill(e, out float r, out float g, out float b);
            float aa = 2.2f / size;
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float u = (x + 0.5f) / size * 2f - 1f;
                    float v = (y + 0.5f) / size * 2f - 1f;
                    float d = Distance(e, u, v);
                    float inner = Smooth(d, aa);
                    float outer = Smooth(d - Outline, aa);
                    int i = (y * size + x) * 4;
                    px[i] = 1f + (r - 1f) * inner;
                    px[i + 1] = 1f + (g - 1f) * inner;
                    px[i + 2] = 1f + (b - 1f) * inner;
                    px[i + 3] = outer;
                }
            }
            return px;
        }

        public static void Fill(Emotion e, out float r, out float g, out float b)
        {
            switch (e)
            {
                case Emotion.Joy: r = 1f; g = 0.82f; b = 0.12f; break;
                case Emotion.Sadness: r = 0.32f; g = 0.45f; b = 0.75f; break;
                case Emotion.Anger: r = 0.92f; g = 0.12f; b = 0.1f; break;
                case Emotion.Fear: r = 0.55f; g = 0.3f; b = 0.9f; break;
                case Emotion.Admiration: r = 1f; g = 0.35f; b = 0.55f; break;
                case Emotion.Worry: r = 0.45f; g = 0.78f; b = 1f; break;
                case Emotion.Surprise: r = 1f; g = 0.6f; b = 0.1f; break;
                case Emotion.Embarrassment: r = 1f; g = 0.5f; b = 0.6f; break;
                case Emotion.Relief: r = 0.5f; g = 0.85f; b = 0.55f; break;
                case Emotion.Affection: r = 0.95f; g = 0.25f; b = 0.45f; break;
                case Emotion.Hunger: r = 0.95f; g = 0.7f; b = 0.3f; break;
                default: r = g = b = 1f; break;
            }
        }

        /// <summary>Signed distance to the icon shape in a -1..1 box (negative inside).</summary>
        public static float Distance(Emotion e, float x, float y)
        {
            switch (e)
            {
                case Emotion.Joy: return Star(x, y + 0.05f, 0.78f, 0.42f);
                case Emotion.Sadness: return Cloud(x, y);
                case Emotion.Anger: return AngerMark(x, y);
                case Emotion.Fear: return Math.Min(Bang(x + 0.3f, y), Bang(x - 0.3f, y));
                case Emotion.Admiration: return Heart(x, y);
                case Emotion.Worry: return Drop(x, y);
                case Emotion.Surprise: return Bang(x, y);
                case Emotion.Embarrassment: return Cloud(x, y * 1.1f);
                case Emotion.Relief: return Drop(x, y * 0.9f);
                case Emotion.Affection: return Heart(x * 0.9f, y * 0.9f);
                case Emotion.Hunger: return Star(x, y, 0.7f, 0.5f);
                default: return 1f;
            }
        }

        internal static float Smooth(float d, float aa)
        {
            float t = 0.5f - d / aa;
            return t < 0f ? 0f : t > 1f ? 1f : t;
        }

        internal static float Circle(float x, float y, float cx, float cy, float r) => Len(x - cx, y - cy) - r;

        private static float Len(float x, float y) => (float)Math.Sqrt(x * x + y * y);

        internal static float Segment(float x, float y, float ax, float ay, float bx, float by, float r)
        {
            float px = x - ax, py = y - ay, dx = bx - ax, dy = by - ay;
            float h = (px * dx + py * dy) / (dx * dx + dy * dy);
            h = h < 0f ? 0f : h > 1f ? 1f : h;
            return Len(px - dx * h, py - dy * h) - r;
        }

        private static float Star(float x, float y, float outer, float inner)
        {
            // Five-pointed star as the union of the triangles fanned between its outer and inner vertices.
            double a = Math.Atan2(x, y);
            double seg = Math.PI * 2.0 / 5.0;
            double local = ((a % seg) + seg) % seg - seg * 0.5;
            float rad = Len(x, y);
            float lx = (float)(Math.Sin(local) * rad), ly = (float)(Math.Cos(local) * rad);
            float ix = (float)(Math.Sin(seg * 0.5) * inner), iy = (float)(Math.Cos(seg * 0.5) * inner);
            // Edge from the outer tip (0, outer) to the inner corner (ix, iy); mirror makes it symmetric.
            lx = Math.Abs(lx);
            float ex = ix, ey = iy - outer;
            float side = (lx * ey - (ly - outer) * ex) >= 0f ? -1f : 1f;
            float dist = Segment(lx, ly, 0f, outer, ix, iy, 0f);
            return dist * side - 0.04f;
        }

        private static float Cloud(float x, float y)
        {
            float c = Math.Min(Circle(x, y, -0.3f, 0.25f, 0.3f), Circle(x, y, 0.05f, 0.4f, 0.38f));
            c = Math.Min(c, Circle(x, y, 0.4f, 0.22f, 0.28f));
            c = Math.Min(c, Segment(x, y, -0.35f, 0.08f, 0.4f, 0.08f, 0.16f));
            float drops = Math.Min(Drop((x + 0.35f) * 3.2f, (y + 0.45f) * 3.2f) / 3.2f, Drop((x - 0.3f) * 3.2f, (y + 0.6f) * 3.2f) / 3.2f);
            return Math.Min(c, drops);
        }

        private static float AngerMark(float x, float y)
        {
            float d = 10f;
            for (int sx = -1; sx <= 1; sx += 2)
                for (int sy = -1; sy <= 1; sy += 2)
                {
                    float corner = Math.Min(Segment(x, y, sx * 0.18f, sy * 0.7f, sx * 0.18f, sy * 0.18f, 0.11f),
                                            Segment(x, y, sx * 0.18f, sy * 0.18f, sx * 0.7f, sy * 0.18f, 0.11f));
                    d = Math.Min(d, corner);
                }
            return d;
        }

        private static float Bang(float x, float y)
        {
            float bar = Segment(x, y, 0f, 0.72f, 0f, -0.15f, 0.13f);
            float dot = Circle(x, y, 0f, -0.6f, 0.15f);
            return Math.Min(bar, dot);
        }

        private static float Heart(float x, float y)
        {
            // Two lobes plus the lower point, softened into one outline.
            float lobes = Math.Min(Circle(x, y, -0.33f, 0.28f, 0.42f), Circle(x, y, 0.33f, 0.28f, 0.42f));
            float ax = Math.Abs(x);
            float tip = Segment(ax, y, 0.62f, 0.14f, 0f, -0.75f, 0f);
            bool inside = y > -0.75f && y < 0.3f && ax < (y + 0.75f) / 0.89f * 0.62f;
            float wedge = inside ? -Math.Min(tip, Math.Abs(y - 0.3f)) : tip;
            return Math.Min(lobes, wedge);
        }

        private static float Drop(float x, float y)
        {
            float body = Circle(x, y, 0f, -0.25f, 0.48f);
            float ax = Math.Abs(x);
            float edge = Segment(ax, y, 0.36f, -0.06f, 0f, 0.82f, 0f);
            bool inside = y > -0.25f && y < 0.82f && ax < (0.82f - y) / 0.88f * 0.36f;
            float cone = inside ? -edge : edge;
            return Math.Min(body, cone);
        }
    }
}
