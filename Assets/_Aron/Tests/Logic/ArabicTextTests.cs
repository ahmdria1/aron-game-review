// (c) Ahmed Alamrani. All rights reserved.
using Aron.Logic;
using NUnit.Framework;

namespace Aron.Tests.Logic
{
    public class ArabicTextTests
    {
        [Test]
        public void NonArabicTextIsUnchanged()
        {
            Assert.AreEqual("Hello 123", ArabicText.Fix("Hello 123"));
        }

        [Test]
        public void SingleLetterUsesIsolatedForm()
        {
            Assert.AreEqual("\uFE8F", ArabicText.Shape("\u0628")); // ب
        }

        [Test]
        public void DualJoiningLettersGetInitialMedialFinal()
        {
            // بيت -> initial beh, medial yeh, final teh
            Assert.AreEqual("\uFE91\uFEF4\uFE96", ArabicText.Shape("بيت"));
        }

        [Test]
        public void RightJoiningLetterBreaksTheChain()
        {
            // دار: dal isolated, alef isolated (dal doesn't join forward), reh isolated
            Assert.AreEqual("\uFEA9\uFE8D\uFEAD", ArabicText.Shape("دار"));
        }

        [Test]
        public void LamAlefBecomesLigature()
        {
            Assert.AreEqual("\uFEFB", ArabicText.Shape("لا"));
            // سلام: seen initial, lam-alef final ligature, meem isolated
            Assert.AreEqual("\uFEB3\uFEFC\uFEE1", ArabicText.Shape("سلام"));
        }

        [Test]
        public void DiacriticsDoNotBreakJoining()
        {
            string shaped = ArabicText.Shape("بَت");
            Assert.AreEqual('\uFE91', shaped[0]);
            Assert.AreEqual('\u064E', shaped[1]);
            Assert.AreEqual('\uFE96', shaped[2]);
        }

        [Test]
        public void HamzaDoesNotJoin()
        {
            // ماء: meem initial, alef final, hamza isolated
            Assert.AreEqual("\uFEE3\uFE8E\uFE80", ArabicText.Shape("ماء"));
        }

        [Test]
        public void DigitRunsArePreReversedForRtlRendering()
        {
            string fixedText = ArabicText.Fix("التمر: 12");
            StringAssert.EndsWith("21", fixedText);
        }

        [Test]
        public void BracketsAreMirrored()
        {
            StringAssert.Contains(")", ArabicText.Fix("(ب").Substring(0, 1));
        }

        [Test]
        public void DetectsArabic()
        {
            Assert.IsTrue(ArabicText.ContainsArabic("Aron أرون"));
            Assert.IsFalse(ArabicText.ContainsArabic("Aron"));
            Assert.IsFalse(ArabicText.ContainsArabic(null));
        }
    }
}
