// (c) Ahmed Alamrani. All rights reserved.
using System;
using System.Collections.Generic;
using Aron.Logic.Costumes;
using NUnit.Framework;

namespace Aron.Logic.Tests
{
    public class CostumeKitTests
    {
        private static IEnumerable<KitPart> AllParts()
        {
            foreach (var id in CostumeKit.MeshIds) foreach (var p in CostumeKit.MeshParts(id)) yield return p;
            foreach (var k in CostumeKit.AccessoryIds) foreach (var p in CostumeKit.AccessoryParts(k)) yield return p;
        }

        [Test]
        public void EveryAccessoryKindHasAPiece()
        {
            foreach (var kind in CostumeRules.AccessoryKinds) Assert.That(CostumeKit.HasAccessory(kind), kind);
            foreach (var kind in CostumeKit.AccessoryIds) Assert.That(CostumeRules.AccessoryKinds, Does.Contain(kind));
            foreach (var kind in CostumeKit.NewAccessoryKinds) Assert.That(CostumeKit.AccessoryParts(kind), Is.Not.Empty, kind);
        }

        [Test]
        public void ShapesAreWellFormed()
        {
            foreach (var tier in new[] { QualityTier.Low, QualityTier.Medium, QualityTier.High })
                foreach (var p in AllParts())
                {
                    var d = p.Build(CostumeKit.Segments(tier));
                    Assert.That(d.TriangleCount, Is.GreaterThan(0), p.name);
                    Assert.That(d.tris.Count % 3, Is.Zero, p.name);
                    Assert.That(d.uvs.Count, Is.EqualTo(d.VertexCount * 2), p.name);
                    foreach (var i in d.tris) Assert.That(i, Is.InRange(0, d.VertexCount - 1), p.name);
                    foreach (var v in d.verts) Assert.That(float.IsNaN(v) || float.IsInfinity(v), Is.False, p.name);
                    Assert.That(d.VertexCount, Is.LessThan(65535), p.name);
                }
        }

        [Test]
        public void LowTierUsesFewerTriangles()
        {
            foreach (var p in AllParts())
                Assert.That(p.Build(CostumeKit.Segments(QualityTier.Low)).TriangleCount,
                            Is.LessThanOrEqualTo(p.Build(CostumeKit.Segments(QualityTier.High)).TriangleCount), p.name);
        }

        [Test]
        public void PiecesFitOnAPerson()
        {
            foreach (var p in AllParts())
            {
                var d = p.Build(16);
                float lo = float.MaxValue, hi = float.MinValue, wide = 0f;
                for (int i = 0; i < d.verts.Count; i += 3)
                {
                    lo = Math.Min(lo, d.verts[i + 1]);
                    hi = Math.Max(hi, d.verts[i + 1]);
                    wide = Math.Max(wide, Math.Max(Math.Abs(d.verts[i]), Math.Abs(d.verts[i + 2])));
                }
                if (p.anchor == "body") Assert.That(lo >= 0f && hi <= 1.6f && wide <= 0.6f, p.name);
                if (p.anchor == "foot") Assert.That(lo >= 0f && hi <= 0.15f && wide <= 0.25f, p.name);
                if (p.anchor == "head") Assert.That(lo >= -1.2f && hi <= 1.5f && wide <= 1.3f, p.name);
                if (p.anchor == "arm") Assert.That(lo >= -0.1f && hi <= 0.35f && wide <= 0.25f, p.name);
            }
        }

        [Test]
        public void ClosedShapesFaceOutwards()
        {
            var ball = CostumeShapes.Ball(0f, 0f, 0f, 1f, 1f, 1f, 12);
            var ring = CostumeShapes.Torus(0f, 1f, 0.2f, 12, 6);
            var cap = CostumeShapes.Lathe(new[] { new Ring(0f, 1f), new Ring(1f, 1f) }, 12, closeTop: true);
            Assert.That(Outward(ball, (x, y, z) => new[] { x, y, z }), Is.True);
            Assert.That(Outward(ring, (x, y, z) =>
            {
                float l = (float)Math.Sqrt(x * x + z * z);
                return new[] { x - x / l, y, z - z / l };
            }), Is.True);
            Assert.That(Outward(cap, (x, y, z) => new[] { x, y - 0.5f, z }), Is.True);
        }

        private static bool Outward(ShapeData d, Func<float, float, float, float[]> away)
        {
            for (int t = 0; t < d.tris.Count; t += 3)
            {
                float[] a = V(d, d.tris[t]), b = V(d, d.tris[t + 1]), c = V(d, d.tris[t + 2]);
                float ux = b[0] - a[0], uy = b[1] - a[1], uz = b[2] - a[2];
                float vx = c[0] - a[0], vy = c[1] - a[1], vz = c[2] - a[2];
                float nx = uy * vz - uz * vy, ny = uz * vx - ux * vz, nz = ux * vy - uy * vx;
                if (nx * nx + ny * ny + nz * nz < 1e-10f) continue;
                var o = away((a[0] + b[0] + c[0]) / 3f, (a[1] + b[1] + c[1]) / 3f, (a[2] + b[2] + c[2]) / 3f);
                if (nx * o[0] + ny * o[1] + nz * o[2] < 0f) return false;
            }
            return true;
        }

        private static float[] V(ShapeData d, int i) => new[] { d.verts[i * 3], d.verts[i * 3 + 1], d.verts[i * 3 + 2] };

        [Test]
        public void ShoesStayClosed()
        {
            foreach (var id in new[] { "shoes_closed", "babouche_closed" })
            {
                var names = new List<string>();
                foreach (var p in CostumeKit.MeshParts(id)) names.Add(p.name);
                Assert.That(names, Does.Contain("Sole").And.Contain("Upper").And.Contain("Collar"), id);
                Assert.That(names.Contains("Heel") || names.Contains("HeelCounter"), id);
            }
        }

        [Test]
        public void OnlyTheAgalHidesWhenItIsInHand()
        {
            foreach (var k in CostumeKit.AccessoryIds)
                foreach (var p in CostumeKit.AccessoryParts(k))
                    Assert.That(p.agal, Is.EqualTo(k == "agal" || k == "agal_cords"), k + "/" + p.name);
            Assert.That(CostumeRules.Agal(true, true, true), Is.EqualTo(AgalSpot.Hand));
            Assert.That(CostumeRules.Agal(true, true, false), Is.EqualTo(AgalSpot.Head));
        }

        [Test]
        public void AgalCordsNeedAnAgal()
        {
            var c = new CostumeDefinition
            {
                id = "qatar_x", country = "qatar", hero = "aron",
                meshes = new[] { new MeshSwap { slot = "feet", mesh = "shoes_closed" } },
                accessories = new[] { new Accessory { kind = "ghutra" }, new Accessory { kind = "agal_cords" } },
            };
            Assert.That(CostumeKit.Problems(c), Has.Some.Contains("agal cords"));
            c.accessories = new[] { new Accessory { kind = "ghutra" }, new Accessory { kind = "agal" }, new Accessory { kind = "agal_cords" } };
            Assert.That(CostumeKit.Problems(c), Is.Empty);
        }

        [Test]
        public void UnknownPiecesAreReported()
        {
            var c = new CostumeDefinition
            {
                id = "x_y", country = "x", hero = "aron",
                meshes = new[] { new MeshSwap { slot = "body", mesh = "space_suit" } },
                accessories = new[] { new Accessory { kind = "cape_of_wonder" } },
            };
            var problems = CostumeKit.Problems(c);
            Assert.That(problems, Has.Some.Contains("space_suit"));
            Assert.That(problems, Has.Some.Contains("cape_of_wonder"));
            Assert.That(problems, Has.Some.Contains("closed shoes"));
        }

        [Test]
        public void TrousersAndFutaReplaceTheSkirt()
        {
            var c = new CostumeDefinition { meshes = new[] { new MeshSwap { slot = "body", mesh = "sherwal" } } };
            Assert.That(CostumeKit.HidesSkirt(c), Is.True);
            c = new CostumeDefinition { accessories = new[] { new Accessory { kind = "futa" } } };
            Assert.That(CostumeKit.HidesSkirt(c), Is.True);
            c = new CostumeDefinition { meshes = new[] { new MeshSwap { slot = "body", mesh = "galabeya_wide" } } };
            Assert.That(CostumeKit.HidesSkirt(c), Is.False);
        }
    }
}
