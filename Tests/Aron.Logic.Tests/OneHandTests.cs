// (c) Ahmed Alamrani. All rights reserved.
using Aron.Logic.Safety;
using NUnit.Framework;

namespace Aron.Logic.Tests
{
    public class OneHandTests
    {
        [Test]
        public void OffByDefaultAndEverythingStaysAsItWas()
        {
            var a = new AccessSettings();
            Assert.That(a.oneHand, Is.False);
            Assert.That(OneHandMode.OrbitAllowed(a), Is.True); Assert.That(OneHandMode.PinchAllowed(a), Is.True);
            Assert.That(OneHandMode.PauseButtonShown(a), Is.False); Assert.That(OneHandMode.AimScale(a), Is.EqualTo(1f));
            Assert.That(OneHandMode.ButtonX(a, 0.9f), Is.EqualTo(0.9f));
        }

        [Test]
        public void OneHandModeNeedsNoSecondFinger()
        {
            var a = new AccessSettings { oneHand = true };
            Assert.That(OneHandMode.OrbitAllowed(a), Is.False); Assert.That(OneHandMode.PinchAllowed(a), Is.False);
            Assert.That(OneHandMode.RecenterButtonShown(a), Is.False, "the camera centres itself");
            Assert.That(OneHandMode.PauseButtonShown(a), Is.True, "no two-finger tap, so pause is on screen");
            Assert.That(OneHandMode.AimScale(a), Is.GreaterThan(1f));
        }

        [Test]
        public void ButtonsSitOnTheThumbsSide()
        {
            var right = new AccessSettings { oneHand = true };
            var left = new AccessSettings { oneHand = true, leftHanded = true };
            Assert.That(OneHandMode.ButtonX(right, 0.3f), Is.GreaterThanOrEqualTo(0.8f));
            Assert.That(OneHandMode.ButtonX(left, 0.7f), Is.LessThanOrEqualTo(0.2f));
            Assert.That(OneHandMode.ButtonX(new AccessSettings { leftHanded = true }, 0.9f), Is.EqualTo(0.1f).Within(1e-5f));
        }

        [Test]
        public void AllowedOnEveryDifficultyUnlikeOneThumb()
        {
            foreach (var d in new[] { "easy", "normal", "hard" }) Assert.That(new AccessSettings { oneHand = true }.Validate(), Is.Empty, d);
            Assert.That(AccessSettings.OneThumbAllowed("hard"), Is.False);
        }
    }
}
