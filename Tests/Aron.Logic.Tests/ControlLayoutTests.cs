// (c) Ahmed Alamrani. All rights reserved.
using System.Linq;
using Aron.Logic.Access;
using Aron.Logic.Localization;
using Aron.Logic.Safety;
using NUnit.Framework;

namespace Aron.Logic.Tests
{
    /// <summary>Left-handed layout, button size and opacity, and buttons the player moved by hand.</summary>
    public class ControlLayoutTests
    {
        private static void Near(float a, float b, string why = "") => Assert.That(a, Is.EqualTo(b).Within(0.0001f), why);

        [Test]
        public void TheDefaultsAreTheRightHandSideAsBefore()
        {
            var r = ControlLayout.Resolve(new AccessSettings(), ControlLayout.Recenter);
            Near(r.X, 0.9f); Near(r.Y, 0.3f);
            var p = ControlLayout.Resolve(new AccessSettings(), ControlLayout.Pause);
            Near(p.X, 0.9f); Near(p.Y, 0.5f);
            Assert.That(r.Width, Is.EqualTo(320f).Within(0.01f), "keeps the 10:3 shape at the 96 px floor");
            Assert.That(r.Height, Is.EqualTo(ControlLayout.MinHeight), "the 90 px button is raised to the 96 px floor");
            Near(r.Alpha, 0.8f);
            Assert.That(ControlLayout.Resolve(null, ControlLayout.Pause).X, Is.EqualTo(0.9f).Within(0.0001f));
        }

        [Test]
        public void LeftHandedMirrorsEveryButton()
        {
            var a = new AccessSettings { leftHanded = true };
            foreach (var id in ControlLayout.Ids)
            {
                var normal = ControlLayout.Resolve(new AccessSettings(), id);
                var mirrored = ControlLayout.Resolve(a, id);
                Near(mirrored.X, 1f - normal.X, id);
                Near(mirrored.Y, normal.Y, id);
            }
        }

        [Test]
        public void OneHandModePullsTheButtonsTowardTheThumbForTheRightHand()
        {
            Near(ControlLayout.Resolve(new AccessSettings { oneHand = true }, ControlLayout.Pause).X, 0.9f);
            Near(ControlLayout.Resolve(new AccessSettings { oneHand = true, leftHanded = true }, ControlLayout.Pause).X, 0.1f);
        }

        [Test]
        public void SizeScalesTheButtonAndNeverBelowTheTouchFloor()
        {
            foreach (var s in ControlLayout.Sizes)
            {
                var r = ControlLayout.Resolve(new AccessSettings { buttonSize = s }, ControlLayout.Recenter);
                Assert.That(r.Height, Is.GreaterThanOrEqualTo(ControlLayout.MinHeight), s.ToString());
                Assert.That(r.Width, Is.GreaterThanOrEqualTo(ControlLayout.BaseWidth * s - 0.01f), s.ToString());
            }
            var big = ControlLayout.Resolve(new AccessSettings { buttonSize = 1.6f }, ControlLayout.Recenter);
            Assert.That(big.Height, Is.EqualTo(144f).Within(0.01f));
            Assert.That(big.Width, Is.EqualTo(480f).Within(0.01f));
            var small = ControlLayout.Resolve(new AccessSettings { buttonSize = 0.8f }, ControlLayout.Recenter);
            Assert.That(small.Height, Is.EqualTo(ControlLayout.MinHeight));
            Assert.That(small.Width / small.Height, Is.EqualTo(300f / 90f).Within(0.01f), "keeps its shape");
            var wild = ControlLayout.Resolve(new AccessSettings { buttonSize = 9f }, ControlLayout.Recenter);
            Assert.That(wild.Height, Is.EqualTo(144f).Within(0.01f), "a corrupt save is held to the largest size");
        }

        [Test]
        public void OpacityIsHeldInRange()
        {
            Near(ControlLayout.Resolve(new AccessSettings { buttonOpacity = 0f }, ControlLayout.Pause).Alpha, 0.3f);
            Near(ControlLayout.Resolve(new AccessSettings { buttonOpacity = 5f }, ControlLayout.Pause).Alpha, 1f);
            Near(ControlLayout.Resolve(new AccessSettings { buttonOpacity = 0.6f }, ControlLayout.Pause).Alpha, 0.6f);
        }

        [Test]
        public void SizeAndOpacityStepsCycleThroughEveryValue()
        {
            float s = 1f; var seen = new System.Collections.Generic.HashSet<float>();
            for (int i = 0; i < ControlLayout.Sizes.Length; i++) { s = ControlLayout.NextSize(s); seen.Add(s); }
            Assert.That(seen.Count, Is.EqualTo(ControlLayout.Sizes.Length)); Near(s, 1f);
            float o = 0.8f; seen.Clear();
            for (int i = 0; i < ControlLayout.Opacities.Length; i++) { o = ControlLayout.NextOpacity(o); seen.Add(o); }
            Assert.That(seen.Count, Is.EqualTo(ControlLayout.Opacities.Length)); Near(o, 0.8f);
            foreach (var v in ControlLayout.Sizes) Assert.That(new AccessSettings { buttonSize = v }.Validate(), Is.Empty);
            foreach (var v in ControlLayout.Opacities) Assert.That(new AccessSettings { buttonOpacity = v }.Validate(), Is.Empty);
        }

        [Test]
        public void ADraggedPositionWinsOverLeftHandedAndOneHand()
        {
            var a = new AccessSettings { leftHanded = true, oneHand = true };
            Assert.That(ControlLayout.Place(a, ControlLayout.Recenter, 0.5f, 0.8f), Is.True);
            var r = ControlLayout.Resolve(a, ControlLayout.Recenter);
            Near(r.X, 0.5f); Near(r.Y, 0.8f);
            Near(ControlLayout.Resolve(a, ControlLayout.Pause).X, 0.1f, "the other button is still on its default");
            Assert.That(ControlLayout.HasCustom(a), Is.True);
        }

        [Test]
        public void ADropIsHeldInsideTheScreen()
        {
            var a = new AccessSettings();
            ControlLayout.Place(a, ControlLayout.Pause, 1.5f, -0.5f);
            var r = ControlLayout.Resolve(a, ControlLayout.Pause);
            Near(r.X, 1f - ControlLayout.EdgeMargin); Near(r.Y, ControlLayout.EdgeMargin);
            Assert.That(a.Validate(), Is.Empty);
        }

        [Test]
        public void ADropOnTopOfTheOtherButtonSlidesClear()
        {
            var a = new AccessSettings();
            var pause = ControlLayout.Resolve(a, ControlLayout.Pause);
            Assert.That(ControlLayout.Place(a, ControlLayout.Recenter, pause.X, pause.Y), Is.True);
            Assert.That(ControlLayout.AnyOverlap(a), Is.False);
            var r = ControlLayout.Resolve(a, ControlLayout.Recenter);
            Near(r.X, pause.X, "it keeps the column the player chose");
        }

        [Test]
        public void TheLargestButtonsStillFitAndDefaultsNeverOverlap()
        {
            foreach (var s in ControlLayout.Sizes)
                foreach (var left in new[] { false, true })
                    foreach (var one in new[] { false, true })
                        Assert.That(ControlLayout.AnyOverlap(new AccessSettings { buttonSize = s, leftHanded = left, oneHand = one }), Is.False, s + " " + left + " " + one);
        }

        [Test]
        public void MovingTheSameButtonTwiceKeepsOneEntryAndResetClearsThem()
        {
            var a = new AccessSettings();
            ControlLayout.Place(a, ControlLayout.Pause, 0.3f, 0.6f);
            ControlLayout.Place(a, ControlLayout.Pause, 0.4f, 0.7f);
            Assert.That(a.placements.Count, Is.EqualTo(1));
            Near(ControlLayout.Resolve(a, ControlLayout.Pause).X, 0.4f);
            ControlLayout.Reset(a);
            Assert.That(ControlLayout.HasCustom(a), Is.False);
            Near(ControlLayout.Resolve(a, ControlLayout.Pause).X, 0.9f);
        }

        [Test]
        public void UnknownButtonsAreRefusedAndBadSavesAreReported()
        {
            var a = new AccessSettings();
            Assert.That(ControlLayout.Place(a, "attack", 0.5f, 0.5f), Is.False);
            Assert.That(ControlLayout.Place(null, ControlLayout.Pause, 0.5f, 0.5f), Is.False);
            Assert.That(a.placements, Is.Empty);
            a.placements.Add(new ButtonPlacement { id = "attack", x = 0.5f, y = 0.5f });
            a.placements.Add(new ButtonPlacement { id = "pause", x = 2f, y = 0.5f });
            a.placements.Add(new ButtonPlacement { id = "pause", x = 0.5f, y = 0.5f });
            Assert.That(a.Validate().Count, Is.EqualTo(3));
            // a damaged save still draws on the screen
            Assert.That(ControlLayout.Resolve(a, "pause").X, Is.InRange(0f, 1f));
        }

        [Test]
        public void PlacementsSurviveASaveAndOldSavesHaveNone()
        {
            var a = new AccessSettings();
            ControlLayout.Place(a, ControlLayout.Pause, 0.25f, 0.65f);
            var json = System.Text.Json.JsonSerializer.Serialize(a, TestData.Opt);
            var back = System.Text.Json.JsonSerializer.Deserialize<AccessSettings>(json, TestData.Opt);
            Near(ControlLayout.Resolve(back, ControlLayout.Pause).X, 0.25f);
            var old = System.Text.Json.JsonSerializer.Deserialize<AccessSettings>("{\"leftHanded\":true}", TestData.Opt);
            Assert.That(old.placements, Is.Not.Null);
            Assert.That(ControlLayout.HasCustom(old), Is.False);
        }

        [Test]
        public void EveryLayoutStringExistsInTwelveLanguages()
        {
            foreach (var key in new[] { "left_handed", "button_size", "button_opacity", "move_buttons", "move_buttons_hint", "reset_buttons" })
                foreach (var l in UiLanguages.Codes)
                    Assert.That(UiStringTables.All[l].TryGetValue(key, out var v) && !string.IsNullOrWhiteSpace(v), Is.True, l + " " + key);
            foreach (var key in new[] { "left_handed", "button_size", "button_opacity" })
                foreach (var l in UiLanguages.Codes) Assert.That(UiStringTables.All[l][key], Does.Contain("{0}"), l + " " + key);
        }
    }
}
