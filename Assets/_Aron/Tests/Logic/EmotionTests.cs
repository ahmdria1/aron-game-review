// (c) Ahmed Alamrani. All rights reserved.
using System;
using Aron.Logic;
using NUnit.Framework;

namespace Aron.Tests.Logic
{
    public class EmotionTests
    {
        [Test]
        public void StreakNeedsEnoughHitsInsideTheWindow()
        {
            var s = new StreakCounter(2f, 3);
            Assert.IsFalse(s.Hit(0f));
            Assert.IsFalse(s.Hit(1f));
            Assert.IsTrue(s.Hit(1.5f));
            Assert.AreEqual(0, s.Count);
        }

        [Test]
        public void StreakForgetsOldHits()
        {
            var s = new StreakCounter(1f, 3);
            s.Hit(0f);
            s.Hit(0.5f);
            Assert.IsFalse(s.Hit(2f));
            Assert.AreEqual(1, s.Count);
        }

        [Test]
        public void SadnessIsNotCutShortByJoy()
        {
            Assert.IsFalse(EmotionRules.CanInterrupt(Emotion.Sadness, Emotion.Joy));
            Assert.IsTrue(EmotionRules.CanInterrupt(Emotion.Worry, Emotion.Fear));
            Assert.IsTrue(EmotionRules.CanInterrupt(Emotion.Neutral, Emotion.Worry));
        }

        [Test]
        public void EveryEmotionHasItsOwnFace()
        {
            var joy = FacePose.For(Emotion.Joy);
            var sad = FacePose.For(Emotion.Sadness);
            Assert.Greater(joy.smile, 0f);
            Assert.Less(sad.smile, 0f);
            Assert.Greater(sad.tears, 0f);
            Assert.Greater(FacePose.For(Emotion.Fear).eyeOpen, FacePose.For(Emotion.Neutral).eyeOpen);
        }

        [Test]
        public void PoseBlendHitsBothEnds()
        {
            var a = FacePose.For(Emotion.Neutral);
            var b = FacePose.For(Emotion.Anger);
            Assert.AreEqual(a.browTilt, FacePose.Lerp(a, b, 0f).browTilt, 1e-5f);
            Assert.AreEqual(b.browTilt, FacePose.Lerp(a, b, 1f).browTilt, 1e-5f);
        }

        [Test]
        public void EmoteIconsHaveShapeAndTransparentCorners()
        {
            foreach (Emotion e in Enum.GetValues(typeof(Emotion)))
            {
                if (e == Emotion.Neutral) continue;
                var px = EmoteArt.Render(e, 32);
                Assert.AreEqual(32 * 32 * 4, px.Length);
                Assert.AreEqual(0f, px[3], 1e-3f, e + " corner should be clear");
                float covered = 0f;
                for (int i = 3; i < px.Length; i += 4) covered += px[i];
                Assert.Greater(covered, 32 * 32 * 0.08f, e + " icon is empty");
            }
        }
    }
}
