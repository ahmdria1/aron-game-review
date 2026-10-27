// (c) Ahmed Alamrani. All rights reserved.
using System.Linq;
using Aron.Logic.Access;
using Aron.Logic.Localization;
using Aron.Logic.Safety;
using NUnit.Framework;

namespace Aron.Logic.Tests
{
    /// <summary>The colour-blind modes: the simulation maths, the palettes, and the shapes that back colour up.</summary>
    public class ColourVisionTests
    {
        private static readonly ColourMode[] Modes = { ColourMode.None, ColourMode.Protanopia, ColourMode.Deuteranopia, ColourMode.Tritanopia };
        private static readonly ColourMode[] Cvd = { ColourMode.Protanopia, ColourMode.Deuteranopia, ColourMode.Tritanopia };

        [Test]
        public void NoModeLeavesAColourAlone()
        {
            var c = new Rgb(0.8f, 0.1f, 0.3f);
            var s = ColourVision.Simulate(c, ColourMode.None);
            Assert.That(s.R, Is.EqualTo(c.R)); Assert.That(s.G, Is.EqualTo(c.G)); Assert.That(s.B, Is.EqualTo(c.B));
        }

        [Test]
        public void WhiteAndBlackAndGreyStayNeutralInEveryMode()
        {
            foreach (var m in Cvd)
            {
                foreach (var v in new[] { 0f, 0.2f, 0.5f, 1f })
                {
                    var s = ColourVision.Simulate(new Rgb(v, v, v), m);
                    Assert.That(s.R, Is.EqualTo(v).Within(0.01f), m + " " + v);
                    Assert.That(s.G, Is.EqualTo(v).Within(0.01f), m + " " + v);
                    Assert.That(s.B, Is.EqualTo(v).Within(0.01f), m + " " + v);
                }
            }
        }

        [Test]
        public void ResultsStayInRange()
        {
            foreach (var m in Cvd)
                foreach (var r in new[] { 0f, 0.5f, 1f }) foreach (var g in new[] { 0f, 0.5f, 1f }) foreach (var b in new[] { 0f, 0.5f, 1f })
                {
                    var s = ColourVision.Simulate(new Rgb(r, g, b), m);
                    Assert.That(new[] { s.R, s.G, s.B }.All(x => x >= 0f && x <= 1f), Is.True, m + " " + r + g + b);
                }
        }

        [Test]
        public void RedAndGreenMergeForRedGreenColourBlindness()
        {
            var red = new Rgb(0.8f, 0.1f, 0.1f);
            var green = new Rgb(0.1f, 0.6f, 0.1f);
            float normal = ColourVision.Distance(red, green, ColourMode.None);
            Assert.That(normal, Is.GreaterThan(50f));
            Assert.That(ColourVision.Distance(red, green, ColourMode.Protanopia), Is.LessThan(normal * 0.6f));
            Assert.That(ColourVision.Distance(red, green, ColourMode.Deuteranopia), Is.LessThan(normal * 0.6f));
        }

        [Test]
        public void BlueAndYellowMergeForBlueYellowColourBlindness()
        {
            var blue = new Rgb(0.1f, 0.3f, 0.9f);
            var teal = new Rgb(0.1f, 0.6f, 0.6f);
            Assert.That(ColourVision.Distance(blue, teal, ColourMode.Tritanopia), Is.LessThan(ColourVision.Distance(blue, teal, ColourMode.None)));
        }

        [Test]
        public void DistanceIsZeroForTheSameColourAndSymmetric()
        {
            var a = new Rgb(0.2f, 0.4f, 0.6f); var b = new Rgb(0.9f, 0.1f, 0.2f);
            Assert.That(ColourVision.DeltaE(a, a), Is.EqualTo(0f).Within(0.001f));
            Assert.That(ColourVision.DeltaE(a, b), Is.EqualTo(ColourVision.DeltaE(b, a)).Within(0.001f));
            Assert.That(ColourVision.DeltaE(new Rgb(0, 0, 0), new Rgb(1, 1, 1)), Is.EqualTo(100f).Within(0.5f), "black to white is 100 L units");
        }

        [Test]
        public void ModeNamesRoundTripAndTheSaveFilterListMatches()
        {
            foreach (var m in Modes) Assert.That(ColourVision.Parse(ColourVision.Name(m)), Is.EqualTo(m));
            Assert.That(ColourVision.Parse("nonsense"), Is.EqualTo(ColourMode.None));
            Assert.That(ColourVision.Parse(null), Is.EqualTo(ColourMode.None));
            CollectionAssert.AreEqual(AccessSettings.Filters, Modes.Select(ColourVision.Name).ToArray());
            var m2 = ColourMode.None;
            for (int i = 0; i < 4; i++) m2 = ColourVision.Next(m2);
            Assert.That(m2, Is.EqualTo(ColourMode.None));
            foreach (var m in Modes) Assert.That(new AccessSettings { colourFilter = ColourVision.Name(m) }.Validate(), Is.Empty);
        }

        // ---- palettes

        [Test]
        public void EveryPaletteKeepsAllFiveRolesApartForItsOwnPlayer()
        {
            foreach (var m in Cvd) Assert.That(AccessPalette.MinDistance(m, m), Is.GreaterThanOrEqualTo(24f), m.ToString());
            Assert.That(AccessPalette.MinDistance(ColourMode.None, ColourMode.None), Is.GreaterThanOrEqualTo(30f));
        }

        [Test]
        public void ThePaletteBeatsTheUsualColoursForRedGreenColourBlindness()
        {
            // the game's usual colours: warning orange and safe green are only 6.5 apart for a protanope
            Assert.That(AccessPalette.MinDistance(ColourMode.None, ColourMode.Protanopia), Is.LessThan(10f));
            Assert.That(AccessPalette.MinDistance(ColourMode.Protanopia, ColourMode.Protanopia), Is.GreaterThan(2f * AccessPalette.MinDistance(ColourMode.None, ColourMode.Protanopia)));
            Assert.That(AccessPalette.MinDistance(ColourMode.Deuteranopia, ColourMode.Deuteranopia), Is.GreaterThan(AccessPalette.MinDistance(ColourMode.None, ColourMode.Deuteranopia)));
            Assert.That(AccessPalette.MinDistance(ColourMode.Tritanopia, ColourMode.Tritanopia), Is.GreaterThan(AccessPalette.MinDistance(ColourMode.None, ColourMode.Tritanopia)));
        }

        [Test]
        public void TheRedGreenPalettesAlsoWorkForTheOtherRedGreenType()
        {
            Assert.That(AccessPalette.MinDistance(ColourMode.Protanopia, ColourMode.Deuteranopia), Is.GreaterThan(15f));
            Assert.That(AccessPalette.MinDistance(ColourMode.Deuteranopia, ColourMode.Protanopia), Is.GreaterThan(15f));
        }

        [Test]
        public void NoModeKeepsTheHudColoursTheGameAlwaysHad()
        {
            var danger = AccessPalette.Colour(ColourMode.None, ColourRole.Danger);
            Assert.That((danger.R, danger.G, danger.B), Is.EqualTo((0.8f, 0.1f, 0.1f)));
            var warn = AccessPalette.Colour(ColourMode.None, ColourRole.Warning);
            Assert.That((warn.R, warn.G, warn.B), Is.EqualTo((1f, 0.55f, 0.35f)));
            var reward = AccessPalette.Colour(ColourMode.None, ColourRole.Reward);
            Assert.That((reward.R, reward.G, reward.B), Is.EqualTo((1f, 0.8f, 0.3f)));
        }

        [Test]
        public void EveryRoleHasItsOwnShapeAndItsOwnColourInEveryMode()
        {
            Assert.That(AccessPalette.Roles.Select(AccessPalette.Shape).Distinct().Count(), Is.EqualTo(AccessPalette.Roles.Length));
            foreach (var m in Modes)
                Assert.That(AccessPalette.Roles.Select(r => AccessPalette.Colour(m, r)).Select(c => (c.R, c.G, c.B)).Distinct().Count(), Is.EqualTo(AccessPalette.Roles.Length), m.ToString());
            Assert.That(AccessPalette.Shape(ColourRole.Danger), Is.EqualTo(MarkShape.Triangle));
        }

        // ---- shapes

        [Test]
        public void EachShapeHasAMaskThatIsNotEmptyAndNotTheWholeSquare()
        {
            var masks = System.Enum.GetValues(typeof(MarkShape)).Cast<MarkShape>().ToDictionary(s => s, s => ShapeMask.Pixels(s, 32));
            foreach (var kv in masks)
            {
                int n = kv.Value.Count(b => b);
                Assert.That(n, Is.InRange(100, 32 * 32 - 100), kv.Key.ToString());
                Assert.That(kv.Value.Length, Is.EqualTo(32 * 32));
            }
            var keys = masks.Keys.ToArray();
            for (int i = 0; i < keys.Length; i++)
                for (int j = i + 1; j < keys.Length; j++)
                {
                    int diff = 0;
                    for (int p = 0; p < 1024; p++) if (masks[keys[i]][p] != masks[keys[j]][p]) diff++;
                    Assert.That(diff, Is.GreaterThan(80), keys[i] + " vs " + keys[j]);
                }
        }

        [Test]
        public void TheTriangleIsWideAtTheBottomAndPointedAtTheTop()
        {
            Assert.That(ShapeMask.Inside(MarkShape.Triangle, 0f, 0.7f), Is.True);
            Assert.That(ShapeMask.Inside(MarkShape.Triangle, 0.5f, 0.7f), Is.False);
            Assert.That(ShapeMask.Inside(MarkShape.Triangle, 0.6f, -0.7f), Is.True);
            Assert.That(ShapeMask.Inside(MarkShape.Triangle, 0f, -0.95f), Is.False);
        }

        [Test]
        public void ShapesAreSymmetricLeftToRightAndOnlyTheEdgeIsTheRim()
        {
            foreach (var s in System.Enum.GetValues(typeof(MarkShape)).Cast<MarkShape>())
            {
                var px = ShapeMask.Pixels(s, 32);
                for (int row = 0; row < 32; row++) for (int col = 0; col < 16; col++)
                    Assert.That(px[row * 32 + col], Is.EqualTo(px[row * 32 + 31 - col]), s + " " + row + "," + col);
                int rim = 0, inside = 0;
                for (int row = 0; row < 32; row++) for (int col = 0; col < 32; col++)
                {
                    if (px[row * 32 + col]) inside++;
                    if (ShapeMask.IsRim(px, 32, col, row)) rim++;
                    if (!px[row * 32 + col]) Assert.That(ShapeMask.IsRim(px, 32, col, row), Is.False);
                }
                Assert.That(rim, Is.GreaterThan(0)); Assert.That(rim, Is.LessThan(inside));
            }
        }

        // ---- text

        [Test]
        public void EveryColourVisionStringExistsInTwelveLanguages()
        {
            foreach (var key in new[] { "cvd_row" }.Concat(Modes.Select(ColourVision.LabelKey)))
                foreach (var l in UiLanguages.Codes)
                    Assert.That(UiStringTables.All[l].TryGetValue(key, out var v) && !string.IsNullOrWhiteSpace(v), Is.True, l + " " + key);
            foreach (var l in UiLanguages.Codes) Assert.That(UiStringTables.All[l]["cvd_row"], Does.Contain("{0}"), l);
        }
    }
}
