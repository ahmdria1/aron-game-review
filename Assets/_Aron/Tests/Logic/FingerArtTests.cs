// (c) Ahmed Alamrani. All rights reserved.
using Aron.Logic;
using NUnit.Framework;

namespace Aron.Tests.Logic
{
    public class FingerArtTests
    {
        private const int Size = 128;

        private static float Alpha(float[] px, float u, float v)
        {
            int x = (int)(u * Size), y = (int)(v * Size);
            return px[(y * Size + x) * 4 + 3];
        }

        [Test]
        public void FingertipSitsAtTheTipPivot()
        {
            var px = FingerArt.Render(FingerShape.Hand, Size);
            Assert.Greater(Alpha(px, 0.51f, FingerArt.TipV - 0.03f), 0.9f);
            Assert.Less(Alpha(px, 0.51f, FingerArt.TipV + 0.04f), 0.1f);
        }

        [Test]
        public void HandHasOutlineAndSkin()
        {
            var px = FingerArt.Render(FingerShape.Hand, Size);
            int mid = ((int)(0.6f * Size) * Size + Size / 2) * 4;
            Assert.Greater(px[mid], 0.8f, "skin inside the finger");
            Assert.AreEqual(1f, px[mid + 3], 1e-3f);
        }

        [Test]
        public void CornersAreTransparent()
        {
            foreach (var shape in new[] { FingerShape.Hand, FingerShape.Ring })
            {
                var px = FingerArt.Render(shape, Size);
                Assert.AreEqual(0f, Alpha(px, 0.01f, 0.01f), 1e-3f);
                Assert.AreEqual(0f, Alpha(px, 0.99f, 0.99f), 1e-3f);
                Assert.AreEqual(0f, Alpha(px, 0.01f, 0.99f), 1e-3f);
            }
        }

        [Test]
        public void RingIsHollow()
        {
            var px = FingerArt.Render(FingerShape.Ring, Size);
            Assert.AreEqual(0f, Alpha(px, 0.5f, 0.5f), 1e-3f);
            Assert.Greater(Alpha(px, 0.5f + 0.39f, 0.5f), 0.9f);
        }
    }
}
