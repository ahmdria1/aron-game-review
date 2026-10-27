// (c) Ahmed Alamrani. All rights reserved.
using System;
using System.Collections.Generic;

namespace Aron.Logic.Costumes
{
    /// <summary>
    /// Low-poly cloth and trim shapes for the costume kit, as plain vertex and index lists so they can be built and
    /// checked without the engine. Y is up, Z is forward. The runtime turns them into meshes and lets Unity work out
    /// the normals.
    /// </summary>
    public class ShapeData
    {
        public readonly List<float> verts = new List<float>();
        public readonly List<float> uvs = new List<float>();
        public readonly List<int> tris = new List<int>();

        public int VertexCount => verts.Count / 3;
        public int TriangleCount => tris.Count / 3;

        public int Add(float x, float y, float z, float u, float v)
        {
            verts.Add(x); verts.Add(y); verts.Add(z);
            uvs.Add(u); uvs.Add(v);
            return VertexCount - 1;
        }

        public void Tri(int a, int b, int c) { tris.Add(a); tris.Add(b); tris.Add(c); }

        public void Append(ShapeData other)
        {
            int start = VertexCount;
            verts.AddRange(other.verts);
            uvs.AddRange(other.uvs);
            foreach (var i in other.tris) tris.Add(start + i);
        }

        /// <summary>Copies every face with the winding flipped, so open cloth shows from inside too.</summary>
        public void DoubleSide()
        {
            int start = VertexCount, count = tris.Count;
            verts.AddRange(verts.ToArray());
            uvs.AddRange(uvs.ToArray());
            for (int i = 0; i < count; i += 3) Tri(start + tris[i], start + tris[i + 2], start + tris[i + 1]);
        }
    }

    /// <summary>One ring of a lathe: height, radius, and how far the ring is pushed forward (+) or back (-).</summary>
    public struct Ring
    {
        public float y, r, z;
        public Ring(float y, float r, float z = 0f) { this.y = y; this.r = r; this.z = z; }
    }

    public static class CostumeShapes
    {
        /// <summary>
        /// Spins a profile around Y. <paramref name="from"/>/<paramref name="to"/> are degrees measured from the
        /// back (0) through the left side, so 40..320 is a cloak open at the front.
        /// Depth squashes front-to-back, cloth is rarely round.
        /// </summary>
        public static ShapeData Lathe(Ring[] rings, int segments, float from = 0f, float to = 360f, float depth = 1f, bool closeTop = false)
        {
            var d = new ShapeData();
            bool full = to - from >= 359.9f;
            int cols = segments + 1;
            for (int r = 0; r < rings.Length; r++)
                for (int s = 0; s <= segments; s++)
                {
                    float a = (from + (to - from) * s / segments) * (float)Math.PI / 180f;
                    float x = (float)Math.Sin(a) * rings[r].r;
                    float z = -(float)Math.Cos(a) * rings[r].r * depth + rings[r].z;
                    d.Add(x, rings[r].y, z, s / (float)segments, r / (float)Math.Max(1, rings.Length - 1));
                }
            for (int r = 0; r < rings.Length - 1; r++)
                for (int s = 0; s < segments; s++)
                {
                    int a = r * cols + s, b = a + 1, c = a + cols, e = c + 1;
                    d.Tri(a, c, b);
                    d.Tri(b, c, e);
                }
            if (closeTop && full)
            {
                var top = rings[rings.Length - 1];
                int centre = d.Add(0f, top.y, top.z, 0.5f, 1f);
                int last = (rings.Length - 1) * cols;
                for (int s = 0; s < segments; s++) d.Tri(last + s, centre, last + s + 1);
            }
            return d;
        }

        /// <summary>A ring of rope or wrapped cloth lying flat around Y.</summary>
        public static ShapeData Torus(float y, float radius, float thick, int segments, int sides, float depth = 1f)
        {
            var d = new ShapeData();
            for (int i = 0; i <= segments; i++)
            {
                float a = i * 2f * (float)Math.PI / segments;
                float ca = (float)Math.Cos(a), sa = (float)Math.Sin(a);
                for (int j = 0; j <= sides; j++)
                {
                    float b = j * 2f * (float)Math.PI / sides;
                    float rr = radius + thick * (float)Math.Cos(b);
                    d.Add(sa * rr, y + thick * (float)Math.Sin(b), -ca * rr * depth, i / (float)segments, j / (float)sides);
                }
            }
            int cols = sides + 1;
            for (int i = 0; i < segments; i++)
                for (int j = 0; j < sides; j++)
                {
                    int a = i * cols + j, b = a + 1, c = a + cols, e = c + 1;
                    d.Tri(a, b, c);
                    d.Tri(b, e, c);
                }
            return d;
        }

        /// <summary>A round tube along a path, for cords, tassels and dagger sheaths. One radius per point.</summary>
        public static ShapeData Tube(float[][] path, float[] radii, int sides, bool capEnd = true)
        {
            var d = new ShapeData();
            int n = path.Length;
            for (int p = 0; p < n; p++)
            {
                var prev = path[Math.Max(0, p - 1)];
                var next = path[Math.Min(n - 1, p + 1)];
                float tx = next[0] - prev[0], ty = next[1] - prev[1], tz = next[2] - prev[2];
                Normalise(ref tx, ref ty, ref tz);
                float ux = 0f, uy = 0f, uz = 1f;
                if (Math.Abs(tz) > 0.9f) { ux = 1f; uz = 0f; }
                float sx = ty * uz - tz * uy, sy = tz * ux - tx * uz, sz = tx * uy - ty * ux;
                Normalise(ref sx, ref sy, ref sz);
                float bx = sy * tz - sz * ty, by = sz * tx - sx * tz, bz = sx * ty - sy * tx;
                for (int s = 0; s <= sides; s++)
                {
                    float a = s * 2f * (float)Math.PI / sides;
                    float c = (float)Math.Cos(a) * radii[p], si = (float)Math.Sin(a) * radii[p];
                    d.Add(path[p][0] + sx * c + bx * si, path[p][1] + sy * c + by * si, path[p][2] + sz * c + bz * si,
                          s / (float)sides, p / (float)Math.Max(1, n - 1));
                }
            }
            int cols = sides + 1;
            for (int p = 0; p < n - 1; p++)
                for (int s = 0; s < sides; s++)
                {
                    int a = p * cols + s, b = a + 1, c = a + cols, e = c + 1;
                    d.Tri(a, c, b);
                    d.Tri(b, c, e);
                }
            if (capEnd)
            {
                var end = path[n - 1];
                int centre = d.Add(end[0], end[1], end[2], 0.5f, 1f);
                int last = (n - 1) * cols;
                for (int s = 0; s < sides; s++) d.Tri(last + s, centre, last + s + 1);
            }
            return d;
        }

        /// <summary>A squashed ball, for tassels, hilts and toe caps.</summary>
        public static ShapeData Ball(float x, float y, float z, float rx, float ry, float rz, int segments)
        {
            int rings = Math.Max(3, segments / 2);
            var profile = new Ring[rings + 1];
            for (int i = 0; i <= rings; i++)
            {
                float a = (float)Math.PI * i / rings;
                profile[i] = new Ring(-(float)Math.Cos(a), (float)Math.Sin(a));
            }
            var d = Lathe(profile, segments);
            for (int i = 0; i < d.verts.Count; i += 3)
            {
                d.verts[i] = x + d.verts[i] * rx;
                d.verts[i + 1] = y + d.verts[i + 1] * ry;
                d.verts[i + 2] = z + d.verts[i + 2] * rz;
            }
            return d;
        }

        public static ShapeData Move(ShapeData d, float x, float y, float z)
        {
            for (int i = 0; i < d.verts.Count; i += 3) { d.verts[i] += x; d.verts[i + 1] += y; d.verts[i + 2] += z; }
            return d;
        }

        /// <summary>Mirrors left-right, for the other foot or sleeve. Winding is flipped back so it still faces out.</summary>
        public static ShapeData MirrorX(ShapeData src)
        {
            var d = new ShapeData();
            for (int i = 0; i < src.verts.Count; i += 3) d.Add(-src.verts[i], src.verts[i + 1], src.verts[i + 2], src.uvs[i / 3 * 2], src.uvs[i / 3 * 2 + 1]);
            for (int i = 0; i < src.tris.Count; i += 3) d.Tri(src.tris[i], src.tris[i + 2], src.tris[i + 1]);
            return d;
        }

        private static void Normalise(ref float x, ref float y, ref float z)
        {
            float l = (float)Math.Sqrt(x * x + y * y + z * z);
            if (l < 1e-6f) { x = 0f; y = 1f; z = 0f; return; }
            x /= l; y /= l; z /= l;
        }
    }
}
