// (c) Ahmed Alamrani. All rights reserved.
using System.IO;
using System.Linq;
using System.Text.Json;
using Aron.Logic.Regions;
using NUnit.Framework;

namespace Aron.Logic.Tests
{
    /// <summary>Checks the region files that ship in Resources/Regions and lays out every stage in it.</summary>
    public class RegionFileTests
    {
        private static RegionCatalog Load()
        {
            string dir = null;
            for (var d = new DirectoryInfo(TestContext.CurrentContext.TestDirectory); d != null; d = d.Parent)
            {
                var p = Path.Combine(d.FullName, "Assets", "_Aron", "Resources", "Regions");
                if (Directory.Exists(p)) { dir = p; break; }
            }
            Assert.That(dir, Is.Not.Null, "Resources/Regions not found");
            var opts = new JsonSerializerOptions { IncludeFields = true };
            var files = Directory.GetFiles(dir, "*.json");
            Assert.That(files, Is.Not.Empty);
            foreach (var f in files)
            {
                var r = JsonSerializer.Deserialize<RegionDef>(File.ReadAllText(f), opts);
                Assert.That(Path.GetFileNameWithoutExtension(f), Is.EqualTo(r.id), "file name must match the region id");
            }
            return RegionCatalog.FromRegions(files.Select(f => JsonSerializer.Deserialize<RegionDef>(File.ReadAllText(f), opts)));
        }

        [Test]
        public void ShippedCatalogIsValid()
        {
            Assert.That(Load().Validate(), Is.Empty);
        }

        [Test]
        public void RiyadhComesFirstWithItsSevenStagesAndDrDuhaim()
        {
            var riyadh = Load().regions[0];
            Assert.That(riyadh.id, Is.EqualTo("riyadh"));
            Assert.That(riyadh.stages.Select(s => s.nameAr), Is.EqualTo(new[]
            {
                "الديرة والمصمك", "سوق الزل", "سوق طيبة", "الدرعية - حي الطريف", "شارع الثميري", "نفود الثمامة", "ساحة د. دحيم",
            }));
            Assert.That(riyadh.stages[5].mount, Is.EqualTo("camel"));
            Assert.That(riyadh.stages[6].boss, Is.True);
            Assert.That(riyadh.boss.nameAr, Is.EqualTo("د. دحيم"));
        }

        [Test]
        public void SaudiThenGulfThenArab()
        {
            var c = Load();
            var gulf = c.regions.Where(r => r.group == "gulf").Select(r => r.id).ToArray();
            var saudi = c.regions.TakeWhile(r => r.group == "saudi").Count();
            Assert.That(saudi, Is.GreaterThanOrEqualTo(1));
            Assert.That(c.regions.Skip(saudi).Any(r => r.group == "saudi"), Is.False);
            if (gulf.Length > 0)
                Assert.That(gulf, Is.EqualTo(new[] { "kuwait", "uae", "bahrain", "qatar", "oman" }));
        }

        [Test]
        public void EveryStageLaysOutCleanly()
        {
            foreach (var r in Load().regions)
                foreach (var s in r.stages)
                {
                    var problems = StageCheck.Problems(StageLayout.Generate(s));
                    Assert.That(problems, Is.Empty, s.id);
                }
        }

        [Test]
        public void NoTwoStagesShareALayout()
        {
            var seen = Load().regions.SelectMany(r => r.stages)
                .Select(s => string.Join(";", StageLayout.Generate(s).Pieces.Where(p => p.kind != PieceKind.Landmark).Select(p => p.ToString())))
                .ToList();
            Assert.That(seen.Distinct().Count(), Is.EqualTo(seen.Count));
        }

        [Test]
        public void DesertRegionsHaveACamelStage()
        {
            foreach (var r in Load().regions)
                foreach (var s in r.stages.Where(s => s.terrain == "desert" && !s.boss && s.type != "race"))
                    Assert.That(s.mount, Is.Not.Empty, s.id + " is desert but has nothing to ride");
        }
    }
}
