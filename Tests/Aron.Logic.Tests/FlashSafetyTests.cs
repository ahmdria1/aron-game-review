// (c) Ahmed Alamrani. All rights reserved.
using Aron.Logic.Access;
using Aron.Logic.CameraRig;
using Aron.Logic.Safety;
using Aron.Logic.Sky;
using NUnit.Framework;

namespace Aron.Logic.Tests
{
    /// <summary>Flash limits, the reduce-flashing option, the photosensitivity-safe switch and camera shake scaling.</summary>
    public class FlashSafetyTests
    {
        private static readonly AccessSettings Normal = new AccessSettings();
        private static readonly AccessSettings Flash = new AccessSettings { reduceFlash = true };
        private static readonly AccessSettings Motion = new AccessSettings { reduceMotion = true };
        private static readonly AccessSettings Safe = new AccessSettings { photoSafe = true };

        [Test]
        public void EverythingIsOffByDefaultAndForOldSaves()
        {
            Assert.That(Normal.photoSafe, Is.False);
            var old = System.Text.Json.JsonSerializer.Deserialize<AccessSettings>("{\"leftHanded\":true}", TestData.Opt);
            Assert.That(FlashSafety.FlashReduced(old), Is.False);
            Assert.That(FlashSafety.MotionReduced(old), Is.False);
            Assert.That(FlashSafety.FlashReduced(null), Is.False);
            Assert.That(FlashSafety.MotionReduced(null), Is.False);
            Assert.That(Safe.Validate(), Is.Empty);
        }

        [Test]
        public void TheSafeSwitchTurnsOnBothReductions()
        {
            Assert.That(FlashSafety.FlashReduced(Safe), Is.True);
            Assert.That(FlashSafety.MotionReduced(Safe), Is.True);
            Assert.That(FlashSafety.FlashReduced(Flash), Is.True);
            Assert.That(FlashSafety.MotionReduced(Flash), Is.False);
            Assert.That(FlashSafety.FlashReduced(Motion), Is.False);
            Assert.That(FlashSafety.MotionReduced(Motion), Is.True);
        }

        [TestCase(8f)]
        [TestCase(10f)]
        [TestCase(16f)]
        [TestCase(60f)]
        public void NoBlinkRunsFasterThanThreeAndNeverMoreThanThreeFlashesInASecond(float asked)
        {
            foreach (var a in new[] { Normal, Flash, Motion, Safe })
            {
                Assert.That(FlashSafety.Hz(a, asked), Is.LessThanOrEqualTo(FlashSafety.GuidanceHz));
                for (float from = 0f; from < 3f; from += 0.037f)
                    Assert.That(FlashSafety.FlashesIn(a, asked, 0.5f, from, 1f), Is.LessThanOrEqualTo(3), $"{asked} Hz from {from}");
            }
        }

        [Test]
        public void ReducedFlashingBlinksAtMostTwiceASecond()
        {
            foreach (var a in new[] { Flash, Safe })
            {
                Assert.That(FlashSafety.Hz(a, 16f), Is.EqualTo(FlashSafety.ReducedHz));
                for (float from = 0f; from < 2f; from += 0.041f)
                    Assert.That(FlashSafety.FlashesIn(a, 16f, 0.7f, from, 1f), Is.LessThanOrEqualTo(2));
            }
        }

        [Test]
        public void SlowBlinksAreLeftAlone()
        {
            Assert.That(FlashSafety.Hz(Normal, 1.5f), Is.EqualTo(1.5f));
            Assert.That(FlashSafety.Hz(Safe, 1.5f), Is.EqualTo(1.5f));
            Assert.That(FlashSafety.Hz(Safe, -4f), Is.EqualTo(0f));
        }

        [Test]
        public void ABlinkIsOnForItsShareOfTheCycle()
        {
            int on = 0;
            for (int i = 0; i < 1000; i++) if (FlashSafety.BlinkOn(Normal, i / 1000f * 2f, 2f, 0.7f)) on++;
            Assert.That(on / 1000f, Is.EqualTo(0.7f).Within(0.02f));
            Assert.That(FlashSafety.BlinkOn(Normal, 0.3f, 2f, 1f), Is.True);
            Assert.That(FlashSafety.BlinkOn(Normal, 0.3f, 2f, 0f), Is.False);
        }

        [Test]
        public void ColourFlashesAreWeakerWhenReducedAndStayInRange()
        {
            Assert.That(FlashSafety.Strength(Normal, 0.8f), Is.EqualTo(0.8f));
            Assert.That(FlashSafety.Strength(Flash, 0.8f), Is.EqualTo(0.4f).Within(1e-5));
            Assert.That(FlashSafety.Strength(Safe, 2f), Is.EqualTo(1f));
            Assert.That(FlashSafety.Strength(Normal, -1f), Is.EqualTo(0f));
        }

        [Test]
        public void CameraShakeFollowsTheLevelAndDropsToAQuarterWhenMotionIsReduced()
        {
            var full = new CameraSettings { shake = 100 };
            Assert.That(FlashSafety.ShakeScale(Normal, full), Is.EqualTo(1f));
            Assert.That(FlashSafety.ShakeScale(Flash, full), Is.EqualTo(1f));
            Assert.That(FlashSafety.ShakeScale(Motion, full), Is.EqualTo(0.25f));
            Assert.That(FlashSafety.ShakeScale(Safe, full), Is.EqualTo(0.25f));
            Assert.That(FlashSafety.ShakeScale(Safe, new CameraSettings { shake = 10 }), Is.EqualTo(0.1f).Within(1e-5));
            Assert.That(FlashSafety.ShakeScale(Safe, new CameraSettings { shake = 0 }), Is.EqualTo(0f));
            Assert.That(FlashSafety.ShakeScale(Normal, null), Is.EqualTo(1f));
        }

        [Test]
        public void LightningStaysWithinThreeFlashesInAnySecondAndHasNoFlashWhenReduced()
        {
            var s = new Strike { Time = 0f, DistanceKm = 0.5f, Flickers = 2 };
            for (float from = 0f; from < 2f; from += 0.013f)
            {
                int edges = 0; bool last = Lightning.Flash(s, from, false) > 0f;
                for (float t = from; t <= from + 1f; t += 0.002f)
                {
                    bool on = Lightning.Flash(s, t, false) > 0f;
                    if (on && !last) edges++;
                    last = on;
                }
                Assert.That(edges, Is.LessThanOrEqualTo(3));
            }
            for (float t = 0f; t < 3f; t += 0.01f)
                Assert.That(Lightning.Flash(s, t, true), Is.LessThanOrEqualTo(Lightning.ReducedPeak + 1e-5f));
        }
    }
}
