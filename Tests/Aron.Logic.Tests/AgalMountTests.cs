// (c) Ahmed Alamrani. All rights reserved.
using System.IO;
using System.Linq;
using System.Text.Json;
using Aron.Logic.Costumes;
using NUnit.Framework;

namespace Aron.Logic.Tests
{
    /// <summary>Aron's agal is his weapon in every outfit: on the headdress where the outfit has one, on his right wrist otherwise.</summary>
    public class AgalMountTests
    {
        private static readonly JsonSerializerOptions Opt = new JsonSerializerOptions { IncludeFields = true };

        private static string Dir()
        {
            for (var d = new DirectoryInfo(TestContext.CurrentContext.TestDirectory); d != null; d = d.Parent)
            {
                var p = Path.Combine(d.FullName, "Assets", "_Aron", "Resources", "Costumes");
                if (Directory.Exists(p)) return p;
            }
            Assert.Fail("Resources/Costumes not found");
            return null;
        }

        [Test]
        public void EveryShippedCostumeSaysWhereTheAgalGoes()
        {
            foreach (var f in Directory.GetFiles(Dir(), "*.json"))
                foreach (var c in JsonSerializer.Deserialize<CostumeFile>(File.ReadAllText(f), Opt).costumes)
                {
                    Assert.That(new[] { "head", "wrist", "none" }, Does.Contain(c.agalMount), c.id);
                    Assert.That(c.agalMount, Is.EqualTo(c.ExpectedAgalMount), c.id);
                }
        }

        [Test]
        public void HeaddressOutfitsWearItOnTheHeadOthersOnTheWrist()
        {
            var najdi = new CostumeDefinition { id = "saudi_najdi", hero = "aron", accessories = new[] { new Accessory { kind = "shemagh" }, new Accessory { kind = "agal" } } };
            var turban = new CostumeDefinition { id = "saudi_hijazi", hero = "aron", accessories = new[] { new Accessory { kind = "turban" } } };
            var noura = new CostumeDefinition { id = "saudi_noura", hero = "noura" };
            Assert.That(najdi.ExpectedAgalMount, Is.EqualTo("head"));
            Assert.That(najdi.AgalSocket, Is.EqualTo("AgalHead"));
            Assert.That(turban.ExpectedAgalMount, Is.EqualTo("wrist"));
            Assert.That(turban.AgalSocket, Is.EqualTo("AgalWrist"));
            Assert.That(noura.ExpectedAgalMount, Is.EqualTo("none"));
        }

        [Test]
        public void AWrongMountIsCaught()
        {
            var file = new CostumeFile
            {
                country = "saudi",
                costumes = new[]
                {
                    new CostumeDefinition { id = "saudi_najdi", country = "saudi", hero = "aron", nameAr = "ثوب", isDefault = true, agalMount = "wrist",
                        accessories = new[] { new Accessory { kind = "shemagh", color = "#C8141E" }, new Accessory { kind = "agal", color = "#111111" } } },
                },
            };
            Assert.That(CostumeRules.Validate(file).Any(e => e.Contains("agalMount")), Is.True);
        }
    }
}
