// (c) Ahmed Alamrani. All rights reserved.
using Aron.Logic;
using NUnit.Framework;

namespace Aron.Tests.Logic
{
    public class ScreenFitTests
    {
        [Test]
        public void NotchedPhoneInsetsTheSides()
        {
            // 2400x1080 landscape with a 100 px cutout on the left and a 60 px gesture bar at the bottom.
            var a = ScreenFit.SafeAnchors(100, 60, 2300, 1020, 2400, 1080);
            Assert.AreEqual(100f / 2400f, a.minX, 1e-5f);
            Assert.AreEqual(60f / 1080f, a.minY, 1e-5f);
            Assert.AreEqual(1f, a.maxX, 1e-5f);
            Assert.AreEqual(1f, a.maxY, 1e-5f);
        }

        [Test]
        public void FullScreenWhenNoSafeAreaIsReported()
        {
            var a = ScreenFit.SafeAnchors(0, 0, 0, 0, 2400, 1080);
            Assert.AreEqual(0f, a.minX); Assert.AreEqual(0f, a.minY);
            Assert.AreEqual(1f, a.maxX); Assert.AreEqual(1f, a.maxY);
        }

        [Test]
        public void AnchorsStayInsideTheScreen()
        {
            var a = ScreenFit.SafeAnchors(-10, -10, 3000, 2000, 2400, 1080);
            Assert.AreEqual(0f, a.minX); Assert.AreEqual(0f, a.minY);
            Assert.AreEqual(1f, a.maxX); Assert.AreEqual(1f, a.maxY);
        }

        [TestCase(1920, 1080, 1f)]
        [TestCase(2400, 1080, 1f)]
        [TestCase(3440, 1440, 1f)]
        [TestCase(2048, 1536, 0f)]
        [TestCase(2388, 1668, 0f)]
        [TestCase(2208, 1768, 0f)]
        public void WideScreensMatchHeightSquareOnesMatchWidth(int w, int h, float expected)
        {
            Assert.AreEqual(expected, ScreenFit.CanvasMatch(w, h));
        }

        [Test]
        public void ReferenceLayoutFitsEveryAspect()
        {
            // Whatever the screen, the scaled 1920x1080 reference must fit inside it.
            foreach (var (w, h) in new[] { (1920, 1080), (2400, 1080), (2560, 1080), (2048, 1536), (2208, 1768), (1280, 800) })
            {
                float match = ScreenFit.CanvasMatch(w, h);
                float scale = match >= 0.5f ? h / 1080f : w / 1920f;
                Assert.LessOrEqual(1920f * scale, w + 0.5f, $"{w}x{h} width");
                Assert.LessOrEqual(1080f * scale, h + 0.5f, $"{w}x{h} height");
            }
        }
    }
}
