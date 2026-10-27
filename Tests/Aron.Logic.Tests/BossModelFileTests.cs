// (c) Ahmed Alamrani. All rights reserved.
using System.IO;
using System.Linq;
using System.Text.Json;
using Aron.Logic.Regions;
using NUnit.Framework;

namespace Aron.Logic.Tests
{
    /// <summary>Each region's boss is built as what its name says it is.</summary>
    public class BossModelFileTests
    {
        private static readonly JsonSerializerOptions Opt = new JsonSerializerOptions { IncludeFields = true };

        private static BossDef Boss(string region)
        {
            for (var d = new DirectoryInfo(TestContext.CurrentContext.TestDirectory); d != null; d = d.Parent)
            {
                var p = Path.Combine(d.FullName, "Assets", "_Aron", "Resources", "Regions", region + ".json");
                if (File.Exists(p)) return JsonSerializer.Deserialize<RegionDef>(File.ReadAllText(p), Opt).boss;
            }
            Assert.Fail(region + ".json not found");
            return null;
        }

        [TestCase("riyadh", "scientist")]
        [TestCase("makkah", "shark")]
        [TestCase("madinah", "golem")]
        [TestCase("tabuk", "wolf")]
        [TestCase("northern", "wolf")]
        [TestCase("kuwait", "captain")]
        [TestCase("tunisia", "captain")]
        [TestCase("egypt", "mummy")]
        [TestCase("bahah", "eagle")]
        [TestCase("bahrain", "clam")]
        [TestCase("eastern", "barrel")]
        [TestCase("iraq", "croc")]
        [TestCase("jazan", "crab")]
        [TestCase("lebanon", "tree")]
        [TestCase("yemen", "marid")]
        public void BossIsBuiltAsItsName(string region, string model)
        {
            Assert.AreEqual(model, Boss(region).model);
        }

        [Test]
        public void TheSharkIsNoCaptain()
        {
            var shark = Boss("makkah");
            Assert.That(shark.headgear, Is.EqualTo("none"));
            Assert.That(RegionCatalog.FighterBody(shark.model), Is.EqualTo(-1));
            Assert.That(shark.nameAr, Does.Not.Contain("النوخذة"));
            Assert.That(Boss("kuwait").nameAr, Does.Contain("النوخذة"));
        }

        [Test]
        public void FightersMapToTheThreeCostumedBodies()
        {
            Assert.AreEqual(0, RegionCatalog.FighterBody("soldier"));
            Assert.AreEqual(1, RegionCatalog.FighterBody("ninja"));
            Assert.AreEqual(2, RegionCatalog.FighterBody("robe"));
            Assert.AreEqual(2, RegionCatalog.FighterBody("captain"));
            Assert.AreEqual(2, RegionCatalog.FighterBody("mummy"));
            foreach (var m in RegionCatalog.BossModels.Skip(5)) Assert.AreEqual(-1, RegionCatalog.FighterBody(m), m);
        }
    }
}
