// (c) Ahmed Alamrani. All rights reserved.
using System.Linq;
using Aron.Logic.Regions;
using NUnit.Framework;

namespace Aron.Logic.Tests
{
    public class StageTimingTests
    {
        private static StageDef Stage(string type, int d, bool boss = false) => new StageDef { id = "t_" + type + d, type = type, difficulty = d, boss = boss, terrain = "street" };

        [Test]
        public void TheShippedTableIsValidAndMatchesTheBuiltInDefaults()
        {
            var f = TestData.Read<StageTimingFile>("Config", "stage_timing.json");
            Assert.That(f.Validate(), Is.Empty);
            var d = new StageTimingFile();
            Assert.That(f.checkpointSeconds, Is.EqualTo(d.checkpointSeconds)); Assert.That(f.parFactor, Is.EqualTo(d.parFactor));
            Assert.That(f.bossFightMin, Is.EqualTo(d.bossFightMin)); Assert.That(f.overshootAllowance, Is.EqualTo(d.overshootAllowance));
            foreach (var t in d.types) { var s = f.For(t.id); Assert.That(s.minMinutes, Is.EqualTo(t.minMinutes), t.id); Assert.That(s.maxMinutes, Is.EqualTo(t.maxMinutes), t.id); }
        }

        [Test]
        public void TheTypeRangesAreAsAgreedAndNeverLeaveFiveToEight()
        {
            var f = new StageTimingFile();
            Assert.That((f.For("normal").minMinutes, f.For("normal").maxMinutes), Is.EqualTo((6f, 8f)));
            Assert.That((f.For("chase").minMinutes, f.For("chase").maxMinutes), Is.EqualTo((5f, 6f)));
            Assert.That((f.For("action").minMinutes, f.For("action").maxMinutes), Is.EqualTo((5f, 7f)));
            Assert.That((f.For("race").minMinutes, f.For("race").maxMinutes), Is.EqualTo((5f, 6f)));
            Assert.That((f.For("boss").minMinutes, f.For("boss").maxMinutes), Is.EqualTo((6f, 8f)));
            foreach (var t in f.types) Assert.That(t.minMinutes >= 5f && t.maxMinutes <= 8f, Is.True, t.id);
        }

        [Test]
        public void TheValidatorRejectsAnythingOutsideFiveToEight()
        {
            var f = new StageTimingFile(); f.types[0].maxMinutes = 9f;
            Assert.That(f.Validate(), Has.Some.Contain("normal"));
            f = new StageTimingFile(); f.types[1].minMinutes = 4f;
            Assert.That(f.Validate(), Has.Some.Contain("chase"));
            f = new StageTimingFile { hardMaxMinutes = 12f };
            Assert.That(f.Validate(), Has.Some.Contain("5..8"));
            f = new StageTimingFile { checkpointSeconds = 200f };
            Assert.That(f.Validate(), Is.Not.Empty);
        }

        [Test]
        public void MinutesGrowWithDifficultyInsideTheTypeRange()
        {
            foreach (var type in new[] { "normal", "chase", "action", "race" })
            {
                var tt = StageTiming.Active.For(type);
                float a = StageTiming.Minutes(Stage(type, 1)), b = StageTiming.Minutes(Stage(type, 10));
                Assert.That(a, Is.LessThan(b), type);
                Assert.That(a, Is.GreaterThanOrEqualTo(tt.minMinutes)); Assert.That(b, Is.LessThanOrEqualTo(tt.maxMinutes));
            }
        }

        [Test]
        public void AStageCanSetItsOwnLengthButOnlyInsideItsTypeRange()
        {
            var s = Stage("chase", 3); s.targetMinutes = 8f;
            Assert.That(StageTiming.Minutes(s), Is.LessThanOrEqualTo(6f));
            s.targetMinutes = 5.5f; Assert.That(StageTiming.Minutes(s), Is.EqualTo(5.5f));
            s.targetMinutes = 3f; Assert.That(StageTiming.Minutes(s), Is.GreaterThanOrEqualTo(5f));
        }

        [Test]
        public void BossStagesCountTheWalkInAndTheFight()
        {
            var s = Stage("boss", 5, true);
            var l = StageLayout.Generate(s);
            float total = StageTiming.TotalSeconds(s, l);
            Assert.That(total, Is.GreaterThan(l.EstimatedSeconds + 100f));
            Assert.That(total / 60f, Is.InRange(6f, 8f));
            Assert.That(StageTiming.BossFightSeconds(10), Is.GreaterThan(StageTiming.BossFightSeconds(1)));
        }

        [Test]
        public void ParIsWithinReachButAboveTheTimeMedalTarget()
        {
            foreach (var s in TestData.Catalog().regions.SelectMany(r => r.stages).Take(60))
            {
                var l = StageLayout.Generate(s);
                float par = StageTiming.ParSeconds(s, l), total = StageTiming.TotalSeconds(s, l);
                Assert.That(par, Is.LessThan(total), s.id); Assert.That(par, Is.GreaterThan(total * 0.75f), s.id);
                Assert.That(par % 5f, Is.EqualTo(0f), "par is in 5 s steps");
                if (!s.boss) Assert.That(par, Is.GreaterThan(Secrets.MedalTarget(s)), s.id + ": the par star comes before any time medal");
            }
            var o = Stage("normal", 3); o.parSeconds = 200f;
            Assert.That(StageTiming.ParSeconds(o, StageLayout.Generate(o)), Is.EqualTo(200f), "a stage's own par wins");
        }

        [Test]
        public void LanternsComeAboutEveryNinetySeconds()
        {
            Assert.That(StageLayout.CheckpointSeconds, Is.EqualTo(90f));
            Assert.That(new StageTimingFile().checkpointSeconds, Is.EqualTo(90f));
            var normal = TestData.Read<Aron.Logic.DifficultyFile>("Config", "difficulty.json").levels.First(l => l.id == "normal");
            Assert.That(normal.checkpointSeconds, Is.EqualTo(90f));
        }

        [Test]
        public void ThreeStarsAreFinishHiddenAndPar()
        {
            var d = new SaveData();
            StageProgress.Finish(d, "x", 400f, 10, 20, 3);
            Assert.That(StageStars.Count(StageProgress.Get(d, "x")), Is.EqualTo(1), "finishing is one star");
            Assert.That(StageStars.Record(d, "x", 400f, 380f, 3, 5), Is.EqualTo(0), "3 of 5 golden boxes and over par");
            Assert.That(StageStars.Record(d, "x", 370f, 380f, 5, 5), Is.EqualTo(2));
            Assert.That(StageStars.Count(StageProgress.Get(d, "x")), Is.EqualTo(3));
            Assert.That(StageStars.Total(d), Is.EqualTo(3));
        }

        [Test]
        public void StarsAreNeverTakenAwayAndUnfinishedStagesHaveNone()
        {
            var d = new SaveData();
            StageProgress.Finish(d, "y", 300f, 0, 0, 5);
            StageStars.Record(d, "y", 300f, 320f, 5, 5);
            StageProgress.Finish(d, "y", 500f, 0, 0, 0);
            StageStars.Record(d, "y", 500f, 320f, 0, 5);
            Assert.That(StageStars.Count(StageProgress.Get(d, "y")), Is.EqualTo(3), "a worse run keeps the stars");
            Assert.That(StageStars.Count(null), Is.EqualTo(0));
            Assert.That(StageStars.Count(new StageRecord { id = "z", done = false, starPar = true }), Is.EqualTo(0));
            Assert.That(StageStars.FoundHidden(0, 0), Is.False, "a stage without golden boxes has no hidden star to find");
            Assert.That(StageStars.BeatPar(0f, 300f), Is.False);
        }
    }
}
