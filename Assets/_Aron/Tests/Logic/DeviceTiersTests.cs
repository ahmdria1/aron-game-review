// (c) Ahmed Alamrani. All rights reserved.
using Aron.Logic;
using NUnit.Framework;

namespace Aron.Tests.Logic
{
    public class DeviceTiersTests
    {
        private static DeviceSpecs Android(int mb, string gpu, int cores = 8) =>
            new DeviceSpecs { memoryMB = mb, cpuCores = cores, gpuName = gpu, screenWidth = 2400, screenHeight = 1080 };

        private static DeviceSpecs Iphone(int mb) =>
            new DeviceSpecs { memoryMB = mb, cpuCores = 6, gpuName = "Apple A15 GPU", isIOS = true };

        [Test]
        public void FlagshipAndroidGetsHigh()
        {
            Assert.AreEqual(QualityTier.High, DeviceTiers.Pick(Android(11500, "Adreno (TM) 750")));
            Assert.AreEqual(QualityTier.High, DeviceTiers.Pick(Android(7600, "Mali-G715-Immortalis MC11")));
        }

        [Test]
        public void BudgetAndroidGetsLow()
        {
            Assert.AreEqual(QualityTier.Low, DeviceTiers.Pick(Android(2800, "Adreno (TM) 610")));
            Assert.AreEqual(QualityTier.Low, DeviceTiers.Pick(Android(7600, "Mali-G52 MC2")), "slow GPU wins over memory");
            Assert.AreEqual(QualityTier.Low, DeviceTiers.Pick(Android(3700, "Mali-T830")));
        }

        [Test]
        public void MidRangeAndroidGetsMedium()
        {
            Assert.AreEqual(QualityTier.Medium, DeviceTiers.Pick(Android(3700, "Adreno (TM) 642L")));
            Assert.AreEqual(QualityTier.Medium, DeviceTiers.Pick(Android(7600, "Mali-G57 MC2")));
            Assert.AreEqual(QualityTier.Medium, DeviceTiers.Pick(Android(7600, "Adreno (TM) 619")));
        }

        [Test]
        public void ExactModelsDoNotCatchNewerGpusWithLongerNames()
        {
            Assert.AreEqual(QualityTier.High, DeviceTiers.Pick(Android(7600, "Mali-G710 MC10")));
            Assert.AreEqual(QualityTier.High, DeviceTiers.Pick(Android(7600, "Mali-G720-Immortalis MC12")));
            Assert.AreEqual(QualityTier.High, DeviceTiers.Pick(Android(7600, "PowerVR D-Series DXT-48-1536")));
        }

        [Test]
        public void FewCoresDropOneTierButNotBelowLow()
        {
            Assert.AreEqual(QualityTier.Medium, DeviceTiers.Pick(Android(7600, "Adreno (TM) 740", cores: 4)));
            Assert.AreEqual(QualityTier.Low, DeviceTiers.Pick(Android(2000, "Adreno (TM) 740", cores: 4)));
        }

        [Test]
        public void UnknownMemoryFallsBackToMedium()
        {
            Assert.AreEqual(QualityTier.Medium, DeviceTiers.Pick(Android(0, null)));
            Assert.AreEqual(QualityTier.Medium, DeviceTiers.Pick(Iphone(0)));
        }

        [Test]
        public void IphonesAreJudgedByMemory()
        {
            Assert.AreEqual(QualityTier.Low, DeviceTiers.Pick(Iphone(1990)), "iPhone 6s / SE 1");
            Assert.AreEqual(QualityTier.Medium, DeviceTiers.Pick(Iphone(2990)), "iPhone XR / 11");
            Assert.AreEqual(QualityTier.High, DeviceTiers.Pick(Iphone(3700)), "iPhone 12 and later");
        }

        [Test]
        public void RenderScaleOnlyShrinksSharpScreens()
        {
            Assert.AreEqual(1f, DeviceTiers.RenderScale(QualityTier.High, 2400, 1080), 1e-4f);
            Assert.AreEqual(1f, DeviceTiers.RenderScale(QualityTier.Low, 1280, 720), 1e-4f);
            Assert.AreEqual(1080f / 1440f, DeviceTiers.RenderScale(QualityTier.High, 3200, 1440), 1e-4f);
            Assert.AreEqual(720f / 1440f, DeviceTiers.RenderScale(QualityTier.Low, 1440, 3200), 1e-4f, "portrait-reported size still uses the short side");
        }

        [Test]
        public void RenderScaleNeverGoesBelowHalfOrBreaksOnZero()
        {
            Assert.AreEqual(0.5f, DeviceTiers.RenderScale(QualityTier.Low, 4000, 2000), 1e-4f);
            Assert.AreEqual(1f, DeviceTiers.RenderScale(QualityTier.Low, 0, 0), 1e-4f);
        }

        [Test]
        public void OnlyLowTierStartsAt30()
        {
            Assert.AreEqual(30, DeviceTiers.FrameRate(QualityTier.Low));
            Assert.AreEqual(60, DeviceTiers.FrameRate(QualityTier.Medium));
            Assert.AreEqual(60, DeviceTiers.FrameRate(QualityTier.High));
        }
    }
}
