// (c) Ahmed Alamrani. All rights reserved.
using Aron.Logic.Audio;
using NUnit.Framework;

namespace Aron.Tests.Logic
{
    public class SfxLogicTests
    {
        [Test]
        public void LimiterUsesFreeVoicesFirst()
        {
            var l = new SfxVoiceLimiter(3);
            Assert.AreEqual(0, l.Acquire("a", 0f, 1f, 3));
            Assert.AreEqual(1, l.Acquire("b", 0f, 1f, 3));
            Assert.AreEqual(2, l.Acquire("c", 0f, 1f, 3));
            Assert.AreEqual(3, l.Active(0.5f));
        }

        [Test]
        public void LimiterStealsOldestOfSameIdAtCap()
        {
            var l = new SfxVoiceLimiter(8);
            int first = l.Acquire("crate", 0f, 2f, 2);
            l.Acquire("crate", 0.1f, 2f, 2);
            l.Acquire("glass", 0.2f, 2f, 2);
            Assert.AreEqual(first, l.Acquire("crate", 0.3f, 2f, 2));
            Assert.AreEqual(2, l.Playing("crate", 0.3f));
        }

        [Test]
        public void LimiterStealsOldestOverallWhenFull()
        {
            var l = new SfxVoiceLimiter(2);
            int a = l.Acquire("a", 0f, 5f, 4);
            l.Acquire("b", 1f, 5f, 4);
            Assert.AreEqual(a, l.Acquire("c", 2f, 5f, 4));
        }

        [Test]
        public void FinishedVoicesAreReused()
        {
            var l = new SfxVoiceLimiter(1);
            l.Acquire("a", 0f, 0.5f, 1);
            Assert.AreEqual(0, l.Active(1f));
            Assert.AreEqual(0, l.Acquire("b", 1f, 0.5f, 1));
        }

        [Test]
        public void PickerNeverRepeatsBackToBack()
        {
            var p = new SfxVariantPicker(42);
            int prev = p.Next("x", 3);
            for (int i = 0; i < 200; i++)
            {
                int n = p.Next("x", 3);
                Assert.AreNotEqual(prev, n);
                Assert.That(n, Is.InRange(0, 2));
                prev = n;
            }
        }

        [Test]
        public void PickerSingleTakeIsZero()
        {
            var p = new SfxVariantPicker(1);
            Assert.AreEqual(0, p.Next("x", 1));
            Assert.AreEqual(0, p.Next("x", 1));
        }

        [Test]
        public void WeatherCrossfadesOverFadeTime()
        {
            var m = new WeatherMix(2f);
            m.Set(Weather.Rain, false, 2f);
            m.Step(1f);
            Assert.AreEqual(0.5f, m.Gain(WeatherLayer.RainLight), 1e-4f);
            m.Step(1.5f);
            Assert.AreEqual(1f, m.Gain(WeatherLayer.RainLight), 1e-4f);

            m.Set(Weather.Sandstorm, false, 2f);
            m.Step(1f);
            Assert.AreEqual(0.5f, m.Gain(WeatherLayer.RainLight), 1e-4f);
            Assert.AreEqual(0.5f, m.Gain(WeatherLayer.Sandstorm), 1e-4f);
        }

        [Test]
        public void ClearFadesEverythingOut()
        {
            var m = new WeatherMix(1f);
            m.Set(Weather.HeavyRain, true, 1f);
            m.Snap();
            Assert.Greater(m.Gain(WeatherLayer.RainRoof), 0f);
            m.Set(Weather.Clear, false, 1f);
            m.Step(1f);
            Assert.IsTrue(m.Silent);
        }
    }
}
