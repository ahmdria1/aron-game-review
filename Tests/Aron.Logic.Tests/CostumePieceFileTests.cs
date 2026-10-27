// (c) Ahmed Alamrani. All rights reserved.
using System.IO;
using System.Linq;
using System.Text.Json;
using Aron.Logic.Costumes;
using Aron.Logic.Regions;
using NUnit.Framework;

namespace Aron.Logic.Tests
{
    public class CostumePieceFileTests
    {
        private static readonly JsonSerializerOptions Opt = new JsonSerializerOptions { IncludeFields = true };

        private static string Resources()
        {
            for (var d = new DirectoryInfo(TestContext.CurrentContext.TestDirectory); d != null; d = d.Parent)
            {
                var p = Path.Combine(d.FullName, "Assets", "_Aron", "Resources");
                if (Directory.Exists(Path.Combine(p, "Costumes"))) return p;
            }
            Assert.Fail("Resources/Costumes not found");
            return null;
        }

        private static CostumeFile[] Files() => Directory.GetFiles(Path.Combine(Resources(), "Costumes"), "*.json")
            .Select(f => JsonSerializer.Deserialize<CostumeFile>(File.ReadAllText(f), Opt)).ToArray();

        [Test]
        public void EveryCostumeUsesPiecesTheKitCanBuild()
        {
            foreach (var f in Files())
            {
                Assert.That(CostumeRules.Validate(f), Is.Empty, f.country);
                foreach (var c in f.costumes) Assert.That(CostumeKit.Problems(c), Is.Empty, c.id);
            }
        }

        [Test]
        public void CostumesStayInTheTriangleBudget()
        {
            foreach (var f in Files())
                foreach (var c in f.costumes)
                    foreach (var tier in new[] { QualityTier.Low, QualityTier.Medium, QualityTier.High })
                        Assert.That(CostumeKit.Triangles(c, tier), Is.LessThanOrEqualTo(CostumeKit.TriangleBudget(tier)), c.id + " " + tier);
        }

        [Test]
        public void AronAlwaysHasBrownClosedLeatherShoes()
        {
            foreach (var f in Files())
                foreach (var c in f.costumes.Where(c => c.hero == "aron"))
                {
                    Assert.That(c.footwear, Is.EqualTo("leather_shoes"), c.id);
                    var feet = c.meshes.Single(m => m.slot == "feet").mesh;
                    Assert.That(feet, Is.EqualTo("shoes_closed").Or.EqualTo("babouche_closed"), c.id);
                    if (feet == "babouche_closed") Assert.That(c.isDefault, Is.False, c.id);
                }
        }

        [Test]
        public void CountriesWearTheirOwnPieces()
        {
            var all = Files().SelectMany(f => f.costumes).ToDictionary(c => c.id);
            void Wears(string id, string piece) =>
                Assert.That(all[id].meshes.Any(m => m.mesh == piece) || all[id].Has(piece), Is.True, id + " wears " + piece);
            Wears("morocco_djellaba", "djellaba_hooded");
            Wears("morocco_babouche", "babouche_closed");
            Wears("algeria_burnous", "burnous_cape");
            Wears("oman_kumma", "kumma");
            Wears("oman_kumma", "khanjar");
            Wears("oman_massar", "massar");
            Wears("yemen_futa", "futa");
            Wears("yemen_futa", "jambiya_belt");
            Wears("sudan_jalabiya", "imma");
            Wears("sudan_jalabiya", "jalabiya");
            Wears("tunisia_jebba", "jebba");
            Wears("tunisia_jebba", "chechia");
            Wears("libya_jard", "chechia");
            Wears("libya_jard", "jard");
            Wears("egypt_galabeya", "galabeya_wide");
            Wears("iraq_dishdasha", "yashmagh");
            Wears("uae_kandura", "kandura");
            Wears("uae_kandura", "tarboosha");
            Wears("qatar_thobe", "agal_cords");
            Wears("syria_sherwal", "sherwal");
            Wears("lebanon_sherwal", "sherwal");
        }

        [Test]
        public void NouraHasAMatchingAbayaForEveryBossStageCostume()
        {
            var all = Files().SelectMany(f => f.costumes).ToList();
            foreach (var file in Directory.GetFiles(Path.Combine(Resources(), "Regions"), "*.json"))
            {
                var r = JsonSerializer.Deserialize<RegionDef>(File.ReadAllText(file), Opt);
                if (r?.stages == null) continue;
                foreach (var s in r.stages.Where(s => !string.IsNullOrEmpty(s.costume)))
                {
                    var id = CostumeRules.ForHero(s.costume, "noura");
                    var noura = all.FirstOrDefault(c => c.id == id);
                    if (!s.costume.EndsWith("_bisht")) continue;
                    Assert.That(noura, Is.Not.Null, s.id + " needs " + id);
                    Assert.That(noura.meshes.Any(m => m.mesh == "abaya_embroidered"), s.id);
                    Assert.That(noura.Has("hijab") && noura.footwear == "closed_flats", s.id);
                }
            }
        }
    }
}
