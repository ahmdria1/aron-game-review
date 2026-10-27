// (c) Ahmed Alamrani. All rights reserved.
using System.IO;
using System.Linq;
using System.Text.Json;
using Aron.Logic.Audio;
using Aron.Logic.Costumes;
using Aron.Logic.Heroes;
using Aron.Logic.Regions;
using NUnit.Framework;

namespace Aron.Logic.Tests
{
    /// <summary>The companion, hero and costume files that ship in Resources.</summary>
    public class ShippedDataTests
    {
        private static readonly JsonSerializerOptions Opts = new JsonSerializerOptions { IncludeFields = true };

        private static string Resources()
        {
            for (var d = new DirectoryInfo(TestContext.CurrentContext.TestDirectory); d != null; d = d.Parent)
            {
                var p = Path.Combine(d.FullName, "Assets", "_Aron", "Resources");
                if (Directory.Exists(p)) return p;
            }
            Assert.Fail("Resources not found");
            return null;
        }

        private static T Read<T>(params string[] path) => JsonSerializer.Deserialize<T>(File.ReadAllText(Path.Combine(new[] { Resources() }.Concat(path).ToArray())), Opts);

        [Test]
        public void CompanionsAreValid()
        {
            var f = Read<CompanionFile>("Companions", "companions.json");
            Assert.That(f.Validate(), Is.Empty);
            Assert.That(f.companions.Select(c => c.id), Is.EquivalentTo(CompanionIds.All));
            Assert.That(f.companions.All(c => c.levels.Length >= 3 && !string.IsNullOrEmpty(c.nameAr)));
        }

        [Test]
        public void HeroesAreValid() => Assert.That(Read<HeroFile>("Heroes", "heroes.json").Validate(), Is.Empty);

        [Test]
        public void EveryCostumeFileIsValid()
        {
            var files = Directory.GetFiles(Path.Combine(Resources(), "Costumes"), "*.json");
            Assert.That(files.Select(Path.GetFileNameWithoutExtension), Does.Contain("saudi"));
            foreach (var path in files)
            {
                var f = JsonSerializer.Deserialize<CostumeFile>(File.ReadAllText(path), Opts);
                Assert.That(Path.GetFileNameWithoutExtension(path), Is.EqualTo(f.country), path);
                Assert.That(CostumeRules.Validate(f), Is.Empty, path);
            }
        }

        [Test]
        public void RegionCostumesExistForBothHeroes()
        {
            var costumes = Directory.GetFiles(Path.Combine(Resources(), "Costumes"), "*.json")
                .SelectMany(p => JsonSerializer.Deserialize<CostumeFile>(File.ReadAllText(p), Opts).costumes).ToList();
            foreach (var path in Directory.GetFiles(Path.Combine(Resources(), "Regions"), "*.json"))
            {
                var r = JsonSerializer.Deserialize<RegionDef>(File.ReadAllText(path), Opts);
                foreach (var id in new[] { r.costume }.Concat(r.stages.Select(s => s.costume)).Where(i => !string.IsNullOrEmpty(i)))
                    Assert.That(costumes.Any(c => c.id == id && c.hero == "aron"), id + " in " + r.id);
                if (string.IsNullOrEmpty(r.costume)) continue;
                var noura = CostumeRules.Pick(costumes, "noura", r.country, r.costume, "", "", null);
                Assert.That(noura, Is.Not.Null, r.id);
            }
        }
    
        [Test]
        public void VoiceLinesCoverEveryBark()
        {
            var f = Read<VoiceLineFile>("Audio", "VoiceLines.json");
            Assert.That(f.Validate(), Is.Empty);
            foreach (var id in AudioCues.Voice) Assert.That(f.Find(id), Is.Not.Null, id);
            foreach (var model in new[] { "scientist", "shark", "golem", "wolf", "captain" })
                foreach (var kind in new[] { AudioCues.BossTaunt, AudioCues.BossHurt, AudioCues.BossDefeat })
                    Assert.That(f.Find(AudioCues.Boss(model, kind)), Is.Not.Null, model + " " + kind);
            Assert.That(f.lines.Where(l => l.character == "aron").All(l => !l.textAr.Contains("ولد")));
        }

        [Test]
        public void AMissingClipIsJustSilence()
        {
            var f = Read<VoiceLineFile>("Audio", "VoiceLines.json");
            Assert.That(f.ClipFor(AudioCues.VoAronStomp), Is.Null);
            Assert.That(f.ClipFor("no_such_line"), Is.Null);
            Assert.That(new VoiceLineFile().ClipFor(AudioCues.VoNouraUff), Is.Null);
        }

        [Test]
        public void EveryBossInTheRegionsHasLines()
        {
            var f = Read<VoiceLineFile>("Audio", "VoiceLines.json");
            foreach (var path in Directory.GetFiles(Path.Combine(Resources(), "Regions"), "*.json"))
            {
                var r = JsonSerializer.Deserialize<RegionDef>(File.ReadAllText(path), Opts);
                if (r.boss == null || string.IsNullOrEmpty(r.boss.model)) continue;
                Assert.That(f.Find(AudioCues.Boss(r.boss.model, AudioCues.BossTaunt)), Is.Not.Null, r.id + " " + r.boss.model);
            }
        }
}
}
