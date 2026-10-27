// (c) Ahmed Alamrani. All rights reserved.
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using Aron.Logic.Costumes;
using Aron.Logic.Regions;
using NUnit.Framework;

namespace Aron.Logic.Tests
{
    /// <summary>The Gulf and Arab regions: their files, stages, bosses, costumes and shops.</summary>
    public class GulfArabRegionTests
    {
        private static readonly string[] Gulf = { "kuwait", "uae", "bahrain", "qatar", "oman" };
        private static readonly string[] Arab =
        {
            "yemen", "iraq", "syria", "lebanon", "jordan", "egypt", "sudan", "libya", "tunisia", "algeria", "morocco", "mauritania",
        };
        private static readonly JsonSerializerOptions Opt = new JsonSerializerOptions { IncludeFields = true };

        private static string Resources()
        {
            for (var d = new DirectoryInfo(TestContext.CurrentContext.TestDirectory); d != null; d = d.Parent)
            {
                var p = Path.Combine(d.FullName, "Assets", "_Aron", "Resources");
                if (Directory.Exists(Path.Combine(p, "Regions"))) return p;
            }
            Assert.Fail("Resources/Regions not found");
            return null;
        }

        private static T Read<T>(params string[] parts) => JsonSerializer.Deserialize<T>(File.ReadAllText(Path.Combine(parts)), Opt);

        private static RegionCatalog Catalog()
        {
            var files = Directory.GetFiles(Path.Combine(Resources(), "Regions"), "*.json")
                .Where(f => Path.GetFileName(f) != "RegionCatalog.json");
            return RegionCatalog.FromRegions(files.Select(f => JsonSerializer.Deserialize<RegionDef>(File.ReadAllText(f), Opt)));
        }

        private static IEnumerable<RegionDef> Ours(RegionCatalog c) => c.regions.Where(r => Gulf.Contains(r.id) || Arab.Contains(r.id));

        [Test]
        public void AllGulfAndArabRegionsShipInOrder()
        {
            var c = Catalog();
            Assert.That(c.regions.Where(r => r.group == "gulf").Select(r => r.id), Is.EqualTo(Gulf));
            Assert.That(c.regions.Where(r => r.group == "arab").Select(r => r.id), Is.EqualTo(Arab));
            int lastSaudi = Array.FindLastIndex(c.regions, r => r.group == "saudi");
            Assert.That(Array.FindIndex(c.regions, r => r.id == "kuwait"), Is.EqualTo(lastSaudi + 1));
        }

        [Test]
        public void OurRegionsPassTheCatalogChecks()
        {
            var c = Catalog();
            var ids = Ours(c).Select(r => "region " + r.id).ToList();
            var errors = c.Validate().Where(e => ids.Any(id => e.StartsWith(id + " ", StringComparison.Ordinal) || e.StartsWith(id + ":", StringComparison.Ordinal)));
            Assert.That(errors, Is.Empty);
        }

        [Test]
        public void FiveToEightStagesWithTheBossLast()
        {
            foreach (var r in Ours(Catalog()))
            {
                Assert.That(r.stages.Count(s => s.type != "race"), Is.InRange(5, 8), r.id);
                for (int i = 0; i < r.stages.Length; i++)
                {
                    bool last = i == r.stages.Length - 1;
                    Assert.That(r.stages[i].boss, Is.EqualTo(last), r.stages[i].id);
                    Assert.That(r.stages[i].type == "boss", Is.EqualTo(last), r.stages[i].id);
                }
                Assert.That(r.boss, Is.Not.Null, r.id);
            }
        }

        [Test]
        public void EveryBossIsItsOwn()
        {
            var bosses = Ours(Catalog()).Select(r => r.boss).ToList();
            Assert.That(bosses.Select(b => b.nameAr).Distinct().Count(), Is.EqualTo(bosses.Count));
            Assert.That(bosses.Select(b => b.model + "/" + b.headgear).Distinct().Count(), Is.EqualTo(bosses.Count));
            Assert.That(bosses.Select(b => b.Pattern).Distinct().Count(), Is.EqualTo(bosses.Count));
        }

        [Test]
        public void NightActionAndWeatherInEveryRegion()
        {
            foreach (var r in Ours(Catalog()))
            {
                Assert.That(r.stages.Any(s => s.time == "night"), r.id + " has no night stage");
                Assert.That(r.stages.Any(s => s.type == "action" || s.type == "chase"), r.id + " has no action or chase stage");
                Assert.That(r.stages.Any(s => s.weather == "rain" || s.weather == "sandstorm"), r.id + " has no rain or sandstorm");
            }
        }

        [Test]
        public void DesertsAreRiddenAndEveryRegionHasAMount()
        {
            foreach (var r in Ours(Catalog()))
            {
                foreach (var s in r.stages.Where(s => s.terrain == "desert" && !s.boss && s.type != "race"))
                    Assert.That(s.mount, Is.EqualTo("camel"), s.id);
                Assert.That(r.stages.Any(s => !string.IsNullOrEmpty(s.mount)), r.id);
            }
        }

        [Test]
        public void EveryStageLaysOut()
        {
            foreach (var r in Ours(Catalog()))
                foreach (var s in r.stages)
                    Assert.That(StageCheck.Problems(StageLayout.Generate(s)), Is.Empty, s.id);
        }

        [Test]
        public void EachCountryHasItsCostumeAndClosedBrownShoes()
        {
            foreach (var r in Ours(Catalog()))
            {
                Assert.That(r.country, Is.EqualTo(r.id));
                var file = Read<CostumeFile>(Resources(), "Costumes", r.country + ".json");
                Assert.That(CostumeRules.Validate(file), Is.Empty, r.country);
                foreach (var c in file.costumes.Where(c => c.hero == "aron"))
                {
                    Assert.That(c.footwear, Is.EqualTo("leather_shoes"), c.id);
                    int red = Convert.ToInt32(c.footwearColor.Substring(1, 2), 16);
                    int green = Convert.ToInt32(c.footwearColor.Substring(3, 2), 16);
                    int blue = Convert.ToInt32(c.footwearColor.Substring(5, 2), 16);
                    Assert.That(red > green && green > blue && red < 0x90, c.id + " shoes aren't brown");
                }
                var ids = file.costumes.Select(c => c.id).ToList();
                foreach (var s in r.stages.Where(s => !string.IsNullOrEmpty(s.costume)))
                    Assert.That(ids, Does.Contain(s.costume), s.id);
            }
        }

        [Test]
        public void LocalShopsSitOnTheirStreets()
        {
            foreach (var r in Ours(Catalog()))
            {
                var shops = Read<RegionShopFile>(Resources(), "RegionShops", r.id + ".json");
                Assert.That(shops.Validate(r), Is.Empty, r.id);
            }
        }
    }
}
