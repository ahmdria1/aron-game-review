// (c) Ahmed Alamrani. All rights reserved.
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using Aron.Logic.Regions;
using NUnit.Framework;

namespace Aron.Logic.Tests
{
    /// <summary>How long stages play, their puzzles, checkpoints and golden boxes, for every stage that ships.</summary>
    public class StagePlayTests
    {
        private static List<StageDef> stages;

        private static IEnumerable<StageDef> All()
        {
            if (stages != null) return stages;
            string dir = null;
            for (var d = new DirectoryInfo(TestContext.CurrentContext.TestDirectory); d != null; d = d.Parent)
            {
                var p = Path.Combine(d.FullName, "Assets", "_Aron", "Resources", "Regions");
                if (Directory.Exists(p)) { dir = p; break; }
            }
            Assert.That(dir, Is.Not.Null);
            var opts = new JsonSerializerOptions { IncludeFields = true };
            var cat = RegionCatalog.FromRegions(Directory.GetFiles(dir, "*.json").Select(f => JsonSerializer.Deserialize<RegionDef>(File.ReadAllText(f), opts)));
            stages = cat.regions.SelectMany(r => r.stages).ToList();
            Assert.That(stages, Is.Not.Empty);
            return stages;
        }

        private static IEnumerable<StageDef> Normal() => All().Where(s => !s.boss);

        /// <summary>The same stage at every difficulty, to cover the whole range and not just what ships.</summary>
        private static IEnumerable<StageDef> EveryDifficulty()
        {
            foreach (var s in All().Take(40))
                for (int d = 1; d <= 10; d++)
                {
                    var c = (StageDef)s.GetType().GetMethod("MemberwiseClone", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic).Invoke(s, null);
                    c.difficulty = d;
                    yield return c;
                }
        }

        [Test]
        public void EveryStagePlaysFiveToEightMinutes()
        {
            foreach (var s in All().Concat(EveryDifficulty()))
            {
                var l = StageLayout.Generate(s);
                float total = StageTiming.TotalSeconds(s, l);
                Assert.That(total, Is.InRange(StageLayout.MinStageSeconds, StageLayout.MaxStageSeconds), $"{s.id} {s.type} d{s.difficulty}");
                Assert.That(StageTiming.Problems(s, l), Is.Empty, s.id);
            }
        }

        [Test]
        public void EachTypeStaysInItsOwnRange()
        {
            var f = StageTiming.Active;
            foreach (var s in All().Concat(EveryDifficulty()))
            {
                var tt = f.For(s.boss ? "boss" : s.type);
                float min = StageTiming.TotalSeconds(s, StageLayout.Generate(s)) / 60f;
                Assert.That(min, Is.InRange(tt.minMinutes - 0.05f, tt.maxMinutes + 0.05f), $"{s.id} {s.type} d{s.difficulty}");
            }
        }

        [Test]
        public void HarderStagesRunLonger()
        {
            foreach (var type in new[] { "normal", "action", "boss" })
            {
                var s = All().First(c => (c.boss ? "boss" : c.type) == type);
                var easy = EveryDifficulty().FirstOrDefault(c => c.id == s.id && c.difficulty == 1) ?? Clone(s, 1);
                var hard = Clone(s, 10);
                Assert.That(StageTiming.TotalSeconds(hard, StageLayout.Generate(hard)), Is.GreaterThan(StageTiming.TotalSeconds(easy, StageLayout.Generate(easy)) + 40f), type);
            }
        }

        private static StageDef Clone(StageDef s, int d)
        {
            var c = (StageDef)s.GetType().GetMethod("MemberwiseClone", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic).Invoke(s, null);
            c.difficulty = d;
            return c;
        }

        [Test]
        public void BossStagesWalkInThenFight()
        {
            foreach (var s in All().Where(s => s.boss))
            {
                var l = StageLayout.Generate(s);
                Assert.That(l.EstimatedSeconds, Is.InRange(120f, 340f), s.id + " the walk in");
                Assert.That(l.Count(PieceKind.BossArena), Is.EqualTo(1), s.id);
            }
        }

        [Test]
        public void EveryStageCanBeFinished()
        {
            foreach (var s in All().Concat(EveryDifficulty()))
                Assert.That(StageCheck.Problems(StageLayout.Generate(s)), Is.Empty, $"{s.id} d{s.difficulty}");
        }

        [Test]
        public void EveryNormalStageHasPuzzlesAndTheyAllWork()
        {
            foreach (var s in Normal())
            {
                var l = StageLayout.Generate(s);
                var gates = l.Of(PieceKind.PuzzleGate).ToList();
                int need = s.type == "normal" ? 4 : 3;   // shorter stage types hold fewer
                Assert.That(gates.Count, Is.GreaterThanOrEqualTo(need), s.id);
                Assert.That(gates.Select(g => g.tag).Distinct().Count(), Is.GreaterThanOrEqualTo(3), s.id + " needs a mix of puzzles");
                Assert.That(PuzzleCheck.Problems(l), Is.Empty, s.id);
                if (string.IsNullOrEmpty(s.mount)) Assert.That(gates.Any(g => g.tag == PuzzleKinds.Lock), s.id + " gives Noura a lock");
            }
        }

        [Test]
        public void StagesMixRunningRidingChasesAndFights()
        {
            foreach (var s in Normal())
            {
                var l = StageLayout.Generate(s);
                Assert.That(l.Count(PieceKind.MountPost), Is.GreaterThanOrEqualTo(1), s.id + " rides");
                Assert.That(l.Count(PieceKind.ChaseRun), Is.GreaterThanOrEqualTo(1), s.id + " chases");
                Assert.That(l.Count(PieceKind.Ambush), Is.GreaterThanOrEqualTo(1), s.id + " fights");
                if (string.IsNullOrEmpty(s.mount)) Assert.That(l.Count(PieceKind.Mover), Is.GreaterThanOrEqualTo(1), s.id + " platforms");
            }
        }

        [Test]
        public void CheckpointsComeEveryNinetySecondsOrSo()
        {
            foreach (var s in Normal())
            {
                var l = StageLayout.Generate(s);
                var times = l.Of(PieceKind.Checkpoint).Select(p => p.time).OrderBy(t => t).ToList();
                Assert.That(times.Count, Is.InRange(2, 7), s.id);
                float last = 0f;
                foreach (var t in times)
                {
                    Assert.That(t - last, Is.InRange(40f, StageCheck.MaxCheckpointSeconds), s.id);
                    last = t;
                }
                Assert.That(l.EstimatedSeconds - last, Is.LessThanOrEqualTo(StageCheck.MaxCheckpointSeconds), s.id);
            }
        }

        [Test]
        public void FourToSixGoldenBoxesInEveryNormalStage()
        {
            foreach (var s in Normal().Concat(EveryDifficulty().Where(s => !s.boss)))
            {
                var boxes = StageLayout.Generate(s).Of(PieceKind.CrateBisht).ToList();
                Assert.That(boxes.Count, Is.InRange(StageLayout.MinBishtBoxes, StageLayout.MaxBishtBoxes), $"{s.id} d{s.difficulty}");
                Assert.That(boxes.Any(b => b.tag == "path"), s.id + " has one on the main path");
                Assert.That(boxes.Any(b => b.tag == "puzzle"), s.id + " hides one behind a puzzle");
                Assert.That(boxes.Any(b => b.tag == "side"), s.id + " tucks one off to the side");
            }
        }

        [Test]
        public void GoldenBoxesBehindPuzzlesComeAfterTheGate()
        {
            foreach (var s in Normal())
            {
                var l = StageLayout.Generate(s);
                var gates = l.Of(PieceKind.PuzzleGate).Select(g => g.z).ToList();
                foreach (var b in l.Of(PieceKind.CrateBisht).Where(b => b.tag == "puzzle"))
                    Assert.That(gates.Any(g => g < b.z && b.z - g < 15f), s.id);
            }
        }

        [Test]
        public void NoGoldenBoxesOnBossStages()
        {
            foreach (var s in All().Where(s => s.boss))
                Assert.That(StageLayout.Generate(s).Count(PieceKind.CrateBisht), Is.Zero, s.id);
        }

        [Test]
        public void BoxCountIncludesEveryKindOfBox()
        {
            var l = StageLayout.Generate(Normal().First());
            int n = l.Count(PieceKind.CrateBasic) + l.Count(PieceKind.CrateBounce) + l.Count(PieceKind.CrateLife) + l.Count(PieceKind.CrateBisht)
                + l.Count(PieceKind.Checkpoint);
            Assert.That(l.BoxCount, Is.EqualTo(n));
            Assert.That(l.BoxCount, Is.GreaterThan(20));
        }

        [Test]
        public void LayoutsStayTheSameEveryTime()
        {
            var s = Normal().First();
            Assert.That(string.Join(";", StageLayout.Generate(s).Pieces), Is.EqualTo(string.Join(";", StageLayout.Generate(s).Pieces)));
        }
    }
}
