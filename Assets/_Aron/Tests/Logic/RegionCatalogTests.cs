// (c) Ahmed Alamrani. All rights reserved.
using System.Linq;
using Aron.Logic.Regions;
using NUnit.Framework;

namespace Aron.Logic.Tests
{
    public class RegionCatalogTests
    {
        public static RegionCatalog Small()
        {
            RegionDef Region(string id, int order, string group, string bossName, string head, string attack)
            {
                var stages = Enumerable.Range(1, 5).Select(i => new StageDef
                {
                    id = id + "_" + i, nameAr = "مرحلة " + i, nameEn = "Stage " + i, terrain = "street",
                    landmarks = new[] { "fort" }, difficulty = i, boss = i == 5,
                    type = i == 5 ? "boss" : i == 3 ? "action" : "normal",
                    time = i == 2 ? "night" : "day", weather = i == 4 ? "rain" : "clear",
                }).ToArray();
                return new RegionDef
                {
                    id = id, order = order, nameAr = id, nameEn = id, group = group, country = id, stages = stages,
                    boss = new BossDef
                    {
                        nameAr = bossName, nameEn = bossName, headgear = head,
                        phases = new[]
                        {
                            new BossPhaseDef { threshold = 1f, attacks = new[] { "charge" }, tauntAr = "يلا" },
                            new BossPhaseDef { threshold = 0.5f, attacks = new[] { "charge", attack }, tauntAr = "يلا" },
                        },
                    },
                };
            }
            return new RegionCatalog
            {
                regions = new[]
                {
                    Region("a", 100, "saudi", "أ", "helmet", "wave"),
                    Region("b", 200, "gulf", "ب", "crown", "volley"),
                    Region("c", 300, "arab", "ج", "fez", "leap"),
                },
            };
        }

        [Test]
        public void SmallCatalogIsValid() => Assert.That(Small().Validate(), Is.Empty);

        [Test]
        public void FromRegionsSortsByOrder()
        {
            var s = Small();
            var c = RegionCatalog.FromRegions(new[] { s.regions[2], s.regions[0], s.regions[1] });
            Assert.That(c.regions.Select(r => r.id), Is.EqualTo(new[] { "a", "b", "c" }));
            Assert.That(c.Validate(), Is.Empty);
        }

        [Test]
        public void OrderOutsideItsGroupIsAnError()
        {
            var c = Small();
            c.regions[1].order = 150;
            Assert.That(c.Validate(), Has.Some.Contains("order for gulf"));
        }

        [Test]
        public void DuplicateOrderIsAnError()
        {
            var c = Small();
            c.regions[1].group = "saudi";
            c.regions[1].order = 100;
            Assert.That(c.Validate(), Has.Some.Contains("unique and rising"));
        }

        [Test]
        public void EveryRegionNeedsANightStage()
        {
            var c = Small();
            c.regions[0].stages[1].time = "sunset";
            Assert.That(c.Validate(), Has.Some.Contains("night stage"));
        }

        [Test]
        public void EveryRegionNeedsActionOrChase()
        {
            var c = Small();
            c.regions[1].stages[2].type = "normal";
            Assert.That(c.Validate(), Has.Some.Contains("action or chase"));
            c.regions[1].stages[2].type = "chase";
            Assert.That(c.Validate(), Is.Empty);
        }

        [Test]
        public void EveryRegionNeedsRainOrSandstorm()
        {
            var c = Small();
            c.regions[2].stages[3].weather = "clear";
            Assert.That(c.Validate(), Has.Some.Contains("rain or sandstorm"));
        }

        [Test]
        public void SandstormNeedsSand()
        {
            var c = Small();
            c.regions[0].stages[3].weather = "sandstorm";
            c.regions[0].stages[3].terrain = "forest";
            Assert.That(c.Validate(), Has.Some.Contains("sandstorm needs sand"));
        }

        [Test]
        public void BossTypeOnlyOnTheBossStage()
        {
            var c = Small();
            c.regions[0].stages[4].type = "action";
            Assert.That(c.Validate(), Has.Some.Contains("type boss"));
            c = Small();
            c.regions[0].stages[0].type = "boss";
            Assert.That(c.Validate(), Has.Some.Contains("type boss"));
        }

        [Test]
        public void UnknownConditionsAreErrors()
        {
            var c = Small();
            c.regions[0].stages[0].time = "noon";
            c.regions[0].stages[1].weather = "snow";
            c.regions[0].stages[2].type = "puzzle";
            var e = c.Validate();
            Assert.That(e, Has.Some.Contains("day, sunset or night"));
            Assert.That(e, Has.Some.Contains("clear, rain or sandstorm"));
            Assert.That(e, Has.Some.Contains("unknown stage type"));
        }

        [Test]
        public void RegionNeedsACountry()
        {
            var c = Small();
            c.regions[0].country = "";
            Assert.That(c.Validate(), Has.Some.Contains("country"));
        }

        [Test]
        public void TooFewStagesIsAnError()
        {
            var c = Small();
            c.regions[0].stages = c.regions[0].stages.Take(4).ToArray();
            c.regions[0].stages[3].boss = true;
            Assert.That(c.Validate(), Has.Some.Contains("5 to 8"));
        }

        [Test]
        public void BossMustBeLast()
        {
            var c = Small();
            c.regions[1].stages[2].boss = true;
            Assert.That(c.Validate(), Has.Some.Contains("only the last stage"));
        }

        [Test]
        public void GroupsMustBeInOrder()
        {
            var c = Small();
            c.regions = new[] { c.regions[1], c.regions[0], c.regions[2] };
            Assert.That(c.Validate(), Has.Some.Contains("Saudi regions first"));
        }

        [Test]
        public void DifficultyCannotDrop()
        {
            var c = Small();
            c.regions[0].stages[2].difficulty = 1;
            Assert.That(c.Validate(), Has.Some.Contains("difficulty goes down"));
        }

        [Test]
        public void TwoBossesCannotFightTheSameWay()
        {
            var c = Small();
            c.regions[2].boss.phases[1].attacks = new[] { "charge", "wave" };
            Assert.That(c.Validate(), Has.Some.Contains("same way"));
        }

        [Test]
        public void TwoBossesCannotLookTheSame()
        {
            var c = Small();
            c.regions[1].boss.headgear = "helmet";
            Assert.That(c.Validate(), Has.Some.Contains("looks like"));
        }

        [Test]
        public void UnknownLandmarkAndMountAreCaught()
        {
            var c = Small();
            c.regions[0].stages[0].landmarks = new[] { "eiffel" };
            c.regions[0].stages[1].mount = "donkey";
            var e = c.Validate();
            Assert.That(e, Has.Some.Contains("unknown landmark"));
            Assert.That(e, Has.Some.Contains("unknown mount"));
        }

        [Test]
        public void HolyPlacesBrandsAndTheNightTowerAreCaught()
        {
            var c = Small();
            c.regions[0].stages[0].landmarks = new[] { "iron_tower" };
            c.regions[0].stages[0].time = "night";
            c.regions[0].stages[1].landmarks = new[] { "tent_mosque" };
            c.regions[0].stages[2].nameEn = "Burj Khalifa Climb";
            c.regions[1].stages[0].nameEn = "Grand Mosque Plaza";
            var e = c.Validate();
            Assert.That(e, Has.Some.Contains("never shown at night"));
            Assert.That(e, Has.Some.Contains("skyline only"));
            Assert.That(e, Has.Some.Contains("no real brands"));
            Assert.That(e, Has.Some.Contains("mosque or holy site"));
        }

        [Test]
        public void NextRunsThroughRegions()
        {
            var c = Small();
            Assert.That(c.Next("a_2").id, Is.EqualTo("a_3"));
            Assert.That(c.Next("a_5").id, Is.EqualTo("b_1"));
            Assert.That(c.Next("c_5"), Is.Null);
        }
    }

    public class StageLayoutTests
    {
        private static StageDef Stage(string id, int difficulty, string mount = "", bool boss = false, string terrain = "street")
            => new StageDef { id = id, nameAr = id, nameEn = id, difficulty = difficulty, mount = mount, boss = boss, terrain = terrain, landmarks = new[] { "souq", "gate" } };

        [Test]
        public void SameStageSameLayout()
        {
            var a = StageLayout.Generate(Stage("souq_1", 4));
            var b = StageLayout.Generate(Stage("souq_1", 4));
            Assert.That(a.Pieces.Select(p => p.ToString()), Is.EqualTo(b.Pieces.Select(p => p.ToString())));
        }

        [Test]
        public void DifferentStagesDiffer()
        {
            var a = StageLayout.Generate(Stage("souq_1", 4));
            var b = StageLayout.Generate(Stage("souq_2", 4));
            Assert.That(a.Pieces.Select(p => p.ToString()), Is.Not.EqualTo(b.Pieces.Select(p => p.ToString())));
        }

        [Test]
        public void HarderStagesAreLongerAndBusier()
        {
            int easyEnemies = 0, hardEnemies = 0;
            float easyLen = 0, hardLen = 0;
            for (int i = 0; i < 20; i++)
            {
                var e = StageLayout.Generate(Stage("e" + i, 1));
                var h = StageLayout.Generate(Stage("h" + i, 10));
                easyEnemies += e.Count(PieceKind.Patrol) + e.Count(PieceKind.Chaser);
                hardEnemies += h.Count(PieceKind.Patrol) + h.Count(PieceKind.Chaser);
                easyLen += e.EstimatedSeconds; hardLen += h.EstimatedSeconds;
            }
            Assert.That(hardEnemies, Is.GreaterThan(easyEnemies * 2));
            Assert.That(hardLen, Is.GreaterThan(easyLen + 20 * 40), "a harder stage lasts longer (seconds)");
        }

        [Test]
        public void ManyRandomStagesAreAllPassable()
        {
            string[] mounts = { "", "camel", "horse" };
            for (int i = 0; i < 300; i++)
            {
                var s = Stage("rand" + i, 1 + i % 10, mounts[i % 3], i % 7 == 0 && i % 3 == 0, i % 3 == 0 ? "street" : "desert");
                var l = StageLayout.Generate(s);
                Assert.That(StageCheck.Problems(l), Is.Empty, s.id);
            }
        }

        [Test]
        public void BossStageEndsInTheArena()
        {
            var l = StageLayout.Generate(Stage("boss", 6, boss: true));
            var arena = l.Of(PieceKind.BossArena).Single();
            float lastGround = l.Of(PieceKind.Ground).Max(p => p.End);
            Assert.That(arena.z - lastGround, Is.EqualTo(StageLayout.ArenaOffset).Within(0.01f));
        }

        [Test]
        public void HorseStagesHaveHurdlesAndNoMovers()
        {
            int hurdles = 0;
            for (int i = 0; i < 10; i++)
            {
                var l = StageLayout.Generate(Stage("horse" + i, 5, "horse", terrain: "desert"));
                hurdles += l.Count(PieceKind.Hurdle);
                Assert.That(l.Count(PieceKind.Mover), Is.Zero);
            }
            Assert.That(hurdles, Is.GreaterThan(0));
        }
    }

    public class StageProgressTests
    {
        [Test]
        public void OnlyTheFirstStageIsOpenAtTheStart()
        {
            var c = RegionCatalogTests.Small();
            var d = new SaveData();
            Assert.That(StageProgress.IsUnlocked(c, d, "a_1"), Is.True);
            Assert.That(StageProgress.IsUnlocked(c, d, "a_2"), Is.False);
            Assert.That(StageProgress.IsRegionUnlocked(c, d, 1), Is.False);
        }

        [Test]
        public void FinishingOpensTheNextStage()
        {
            var c = RegionCatalogTests.Small();
            var d = new SaveData();
            StageProgress.Finish(d, "a_1", 80f, 12);
            Assert.That(StageProgress.IsUnlocked(c, d, "a_2"), Is.True);
            Assert.That(StageProgress.Frontier(c, d).id, Is.EqualTo("a_2"));
        }

        [Test]
        public void BeatingTheBossOpensTheNextRegion()
        {
            var c = RegionCatalogTests.Small();
            var d = new SaveData();
            foreach (var s in c.regions[0].stages) StageProgress.Finish(d, s.id, 60f, 5);
            Assert.That(StageProgress.IsRegionDone(c, d, 0), Is.True);
            Assert.That(StageProgress.IsRegionUnlocked(c, d, 1), Is.True);
            Assert.That(StageProgress.IsUnlocked(c, d, "b_2"), Is.False);
        }

        [Test]
        public void KeepsTheBestTimeAndMostDates()
        {
            var d = new SaveData();
            Assert.That(StageProgress.Finish(d, "a_1", 90f, 10), Is.True);
            Assert.That(StageProgress.Finish(d, "a_1", 70f, 4), Is.True);
            Assert.That(StageProgress.Finish(d, "a_1", 75f, 14), Is.False);
            var r = StageProgress.Get(d, "a_1");
            Assert.That(r.bestTime, Is.EqualTo(70f));
            Assert.That(r.bestDates, Is.EqualTo(14));
        }

        [Test]
        public void OldSavesWithoutStagesStillWork()
        {
            var d = new SaveData { stages = null };
            Assert.That(StageProgress.IsDone(d, "a_1"), Is.False);
            StageProgress.Finish(d, "a_1", 50f, 1);
            Assert.That(StageProgress.IsDone(d, "a_1"), Is.True);
        }

        [Test]
        public void TimeFormat()
        {
            Assert.That(StageProgress.FormatTime(0f), Is.EqualTo("--:--"));
            Assert.That(StageProgress.FormatTime(125.4f), Is.EqualTo("2:05"));
        }
    }
}
