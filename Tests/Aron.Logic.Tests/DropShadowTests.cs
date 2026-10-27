// (c) Ahmed Alamrani. All rights reserved.
using Aron.Logic.GameFeel;
using Aron.Logic.Presentation;
using NUnit.Framework;

namespace Aron.Logic.Tests
{
    [TestFixture]
    public class DropShadowTests
    {
        private static FeelValues V() => TestData.Read<FeelValues>("Config", "game_feel.json");

        [Test]
        public void Standing_on_the_ground_gives_a_full_size_disc_at_full_opacity_without_real_shadows()
        {
            var v = V();
            var s = DropShadowMath.Evaluate(v, true, 0f, false, false);
            Assert.IsTrue(s.Visible);
            Assert.That(s.Scale, Is.EqualTo(v.shadowSize).Within(1e-5f));
            Assert.That(s.Alpha, Is.EqualTo(v.shadowOpacity).Within(1e-5f));
        }

        [Test]
        public void The_disc_shrinks_and_fades_as_the_character_rises()
        {
            var v = V();
            float lastScale = float.MaxValue, lastAlpha = float.MaxValue;
            for (float h = 0f; h < v.shadowMaxDistance * 0.99f; h += 0.25f)
            {
                var s = DropShadowMath.Evaluate(v, true, h, false, false);
                if (!s.Visible) break;
                Assert.LessOrEqual(s.Scale, lastScale + 1e-6f, "scale at " + h);
                Assert.LessOrEqual(s.Alpha, lastAlpha + 1e-6f, "alpha at " + h);
                lastScale = s.Scale; lastAlpha = s.Alpha;
            }
            var top = DropShadowMath.Evaluate(v, true, v.shadowMaxDistance * 0.95f, false, false);
            Assert.Less(top.Scale, v.shadowSize * 0.5f);
            Assert.Less(top.Alpha, v.shadowOpacity * 0.2f);
        }

        [Test]
        public void A_jump_peak_still_shows_a_shadow_on_every_tier()
        {
            var v = V();
            foreach (bool real in new[] { false, true })
            {
                var s = DropShadowMath.Evaluate(v, true, v.jumpHeight, real, false);
                Assert.IsTrue(s.Visible, "real shadows " + real);
                Assert.Greater(s.Alpha, 0.2f);
            }
            // and a glide or a long fall of several metres
            Assert.IsTrue(DropShadowMath.Evaluate(v, true, 6f, true, false).Visible);
        }

        [Test]
        public void No_ground_within_reach_hides_the_shadow()
        {
            var v = V();
            Assert.IsFalse(DropShadowMath.Evaluate(v, false, 0f, false, false).Visible);
            Assert.IsFalse(DropShadowMath.Evaluate(v, false, 3f, true, true).Visible);
            Assert.IsFalse(DropShadowMath.Evaluate(v, true, v.shadowMaxDistance + 0.01f, false, false).Visible);
            Assert.IsFalse(DropShadowMath.Evaluate(v, true, float.NaN, false, false).Visible);
        }

        [Test]
        public void With_real_shadows_the_disc_is_weaker_on_the_ground_and_never_stronger_than_without()
        {
            var v = V();
            var on = DropShadowMath.Evaluate(v, true, 0f, true, false);
            var off = DropShadowMath.Evaluate(v, true, 0f, false, false);
            Assert.That(on.Alpha, Is.EqualTo(off.Alpha * v.shadowRealBlend).Within(1e-5f));
            for (float h = 0f; h < v.shadowMaxDistance; h += 0.2f)
                Assert.LessOrEqual(DropShadowMath.Evaluate(v, true, h, true, false).Alpha, DropShadowMath.Evaluate(v, true, h, false, false).Alpha + 1e-6f, "h " + h);
            // the combined darkness of a real shadow plus the disc stays below what the disc alone gives on the Low tier
            Assert.Less(on.Alpha, off.Alpha);
        }

        [Test]
        public void The_disc_returns_to_full_strength_above_the_blend_height()
        {
            var v = V();
            float h = v.shadowRealBlendHeight;
            var on = DropShadowMath.Evaluate(v, true, h, true, false);
            var off = DropShadowMath.Evaluate(v, true, h, false, false);
            Assert.That(on.Alpha, Is.EqualTo(off.Alpha).Within(1e-5f));
        }

        [Test]
        public void Assist_mode_makes_the_landing_shadow_bigger_and_stronger_but_never_opaque()
        {
            var v = V();
            for (float h = 0f; h < 6f; h += 0.5f)
            {
                var n = DropShadowMath.Evaluate(v, true, h, false, false);
                var a = DropShadowMath.Evaluate(v, true, h, false, true);
                Assert.Greater(a.Scale, n.Scale);
                Assert.GreaterOrEqual(a.Alpha, n.Alpha);
                Assert.LessOrEqual(a.Alpha, 0.9f);
            }
        }

        [Test]
        public void Height_comes_from_the_ray_distance_minus_the_lift_and_is_never_negative()
        {
            Assert.That(DropShadowMath.HeightFromHit(DropShadowMath.RayLift + 2f), Is.EqualTo(2f).Within(1e-5f));
            Assert.AreEqual(0f, DropShadowMath.HeightFromHit(0.05f));
        }

        [Test]
        public void Zero_opacity_turns_the_shadow_off_and_the_values_stay_in_range()
        {
            var v = V();
            v.shadowOpacity = 0f;
            Assert.IsFalse(DropShadowMath.Evaluate(v, true, 0f, false, false).Visible);
            Assert.That(V().Validate(), Is.Empty);
            foreach (var f in FeelValues.Fields) if (FeelValues.Info(f).Group == "Shadow") Assert.That(f.Name, Does.StartWith("shadow"));
            Assert.AreEqual(9, System.Linq.Enumerable.Count(FeelValues.Fields, f => FeelValues.Info(f).Group == "Shadow"));
        }

        [Test]
        public void At_most_two_dozen_shadows_are_drawn_per_frame()
        {
            Assert.AreEqual(24, DropShadowMath.MaxCasters);
        }
    }
}
