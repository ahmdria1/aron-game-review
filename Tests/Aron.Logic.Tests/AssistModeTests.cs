// (c) Ahmed Alamrani. All rights reserved.
using System.Linq;
using Aron.Logic.Access;
using Aron.Logic.GameFeel;
using Aron.Logic.Localization;
using Aron.Logic.Presentation;
using Aron.Logic.Regions;
using Aron.Logic.Safety;
using NUnit.Framework;

namespace Aron.Logic.Tests
{
    /// <summary>Assist mode helps, and never blocks progress or rewards.</summary>
    public class AssistModeTests
    {
        private static readonly AccessSettings On = new AccessSettings { assistMode = true };
        private static readonly AccessSettings Off = new AccessSettings();

        [Test]
        public void ItIsOffByDefaultAndForOldSaves()
        {
            Assert.That(Off.assistMode, Is.False);
            var old = System.Text.Json.JsonSerializer.Deserialize<AccessSettings>("{\"leftHanded\":true}", TestData.Opt);
            Assert.That(AssistRules.On(old), Is.False);
            Assert.That(AssistRules.On(null), Is.False);
            Assert.That(Off.Validate(), Is.Empty);
            Assert.That(On.Validate(), Is.Empty);
        }

        [Test]
        public void OffChangesNothing()
        {
            Assert.That(AssistRules.CheckpointSeconds(Off, 90f), Is.EqualTo(90f));
            Assert.That(AssistRules.ChaseSpeed(Off), Is.EqualTo(1f));
            Assert.That(AssistRules.ShadowBoost(Off), Is.False);
            Assert.That(AssistRules.CheckpointSeconds(null, 90f), Is.EqualTo(90f));
        }

        [Test]
        public void OnBringsCheckpointsSoonerAndSlowsTheChase()
        {
            Assert.That(AssistRules.CheckpointSeconds(On, 90f), Is.EqualTo(54f).Within(0.001f));
            Assert.That(AssistRules.CheckpointSeconds(On, 20f), Is.EqualTo(AssistRules.MinCheckpointSeconds), "never closer than the layout's floor");
            Assert.That(AssistRules.ChaseSpeed(On), Is.EqualTo(0.8f).Within(0.001f));
            Assert.That(AssistRules.ShadowBoost(On), Is.True);
        }

        [Test]
        public void TheChaseStaysRunnableAtTheSlowestSetting()
        {
            // the boulder runs at 0.72..0.88 of the hero's speed; assist only makes that slower, so it can never catch up faster
            Assert.That(AssistRules.ChaseSpeed(On) * 0.88f, Is.LessThan(0.88f));
            Assert.That(AssistRules.ChaseSpeed(On), Is.GreaterThan(0.5f), "slower, but still a chase");
        }

        [Test]
        public void TheShadowBoostMakesTheShadowBiggerAndDarkerAndNeverPastItsCap()
        {
            var v = TestData.Read<FeelValues>("Config", "game_feel.json");
            foreach (var h in new[] { 0f, 1f, 3f })
            {
                var plain = DropShadowMath.Evaluate(v, true, h, false, AssistRules.ShadowBoost(Off));
                var boosted = DropShadowMath.Evaluate(v, true, h, false, AssistRules.ShadowBoost(On));
                Assert.That(boosted.Scale, Is.GreaterThan(plain.Scale), "height " + h);
                Assert.That(boosted.Alpha, Is.GreaterThanOrEqualTo(plain.Alpha), "height " + h);
                Assert.That(boosted.Alpha, Is.LessThanOrEqualTo(0.9f));
            }
        }

        private static StageLayout WithAssist(StageDef s)
        {
            var l = StageLayout.Generate(s);
            AssistRules.AddCheckpoints(l, AssistRules.CheckpointSeconds(On, StageLayout.CheckpointSeconds));
            return l;
        }

        private static float LongestStretch(StageLayout l)
        {
            float last = 0f, worst = 0f;
            foreach (var p in l.Pieces)
            {
                if (p.kind != PieceKind.Checkpoint) continue;
                worst = System.Math.Max(worst, p.time - last); last = p.time;
            }
            return System.Math.Max(worst, l.EstimatedSeconds - last);
        }

        [Test]
        public void AssistAddsCheckpointsAndChangesNothingElseInTheStage()
        {
            var stages = TestData.Catalog().regions.SelectMany(r => r.stages).ToList();
            int shorter = 0, total = 0;
            foreach (var s in stages)
            {
                var normal = StageLayout.Generate(s);
                var assist = WithAssist(s);
                // every other piece is the same, in the same order: coins, crates, extra-life crates, bisht boxes, enemies, the goal
                var a = normal.Pieces.Where(p => p.kind != PieceKind.Checkpoint).Select(p => p.ToString() + p.time).ToList();
                var b = assist.Pieces.Where(p => p.kind != PieceKind.Checkpoint).Select(p => p.ToString() + p.time).ToList();
                CollectionAssert.AreEqual(a, b, s.id);
                // the original lanterns are all still there
                foreach (var cp in normal.Of(PieceKind.Checkpoint)) Assert.That(assist.Of(PieceKind.Checkpoint).Any(c => c.z == cp.z), Is.True, s.id);
                Assert.That(assist.Count(PieceKind.Checkpoint), Is.GreaterThanOrEqualTo(normal.Count(PieceKind.Checkpoint)), s.id);
                Assert.That(LongestStretch(assist), Is.LessThanOrEqualTo(LongestStretch(normal) + 0.01f), s.id);
                Assert.That(assist.EstimatedSeconds, Is.EqualTo(normal.EstimatedSeconds), s.id + ": same length");
                total++;
                if (LongestStretch(assist) < LongestStretch(normal) - 5f) shorter++;
            }
            Assert.That(total, Is.GreaterThan(50));
            Assert.That(shorter, Is.GreaterThan(total / 2), "most stages get a shorter longest stretch without a checkpoint");
        }

        [Test]
        public void ExtraCheckpointsSitOnOpenGroundAwayFromDanger()
        {
            var stages = TestData.Catalog().regions.SelectMany(r => r.stages).ToList();
            int extra = 0;
            foreach (var s in stages)
            {
                var normal = StageLayout.Generate(s);
                var assist = WithAssist(s);
                var original = normal.Of(PieceKind.Checkpoint).Select(c => c.z).ToList();
                foreach (var cp in assist.Of(PieceKind.Checkpoint).Where(c => !original.Contains(c.z)))
                {
                    extra++;
                    var ground = assist.Pieces.First(p => p.kind == PieceKind.Ground && cp.z >= p.Start && cp.z <= p.End);
                    Assert.That(ground.sizeB, Is.GreaterThanOrEqualTo(AssistRules.MinGround), s.id);
                    Assert.That(cp.y, Is.EqualTo(ground.y), s.id);
                    foreach (var o in assist.Pieces.Where(o => o.kind == PieceKind.Patrol || o.kind == PieceKind.Chaser || o.kind == PieceKind.Mover || o.kind == PieceKind.PuzzlePart || o.kind == PieceKind.ChaseRun && cp.z >= o.Start && cp.z <= o.End))
                        Assert.That(o.kind == PieceKind.ChaseRun ? 0f : System.Math.Abs(o.z - cp.z), Is.GreaterThanOrEqualTo(o.kind == PieceKind.ChaseRun ? 0f : 3.5f), s.id + " " + o);
                    Assert.That(assist.Of(PieceKind.Checkpoint).Count(c => System.Math.Abs(c.z - cp.z) < 12f), Is.EqualTo(1), s.id + ": lanterns are not stacked");
                }
            }
            Assert.That(extra, Is.GreaterThan(100), "the catalog gets plenty of extra lanterns");
        }

        [Test]
        public void ItIsSafeOnEmptyInput()
        {
            Assert.That(AssistRules.AddCheckpoints(null, 54f), Is.EqualTo(0));
            var s = TestData.Catalog().regions[0].stages[0];
            var l = StageLayout.Generate(s);
            // asking for lanterns every second is held to the 20 s floor
            AssistRules.AddCheckpoints(l, 1f);
            var z = l.Of(PieceKind.Checkpoint).Select(c => c.time).OrderBy(t => t).ToList();
            for (int i = 1; i < z.Count; i++) Assert.That(z[i] - z[i - 1], Is.GreaterThan(1f));
        }

        [Test]
        public void RewardsAndProgressRulesDoNotTakeTheAssistSetting()
        {
            // assist reaches the game only through AssistRules, and that class offers no way to change a reward or an unlock
            var names = typeof(AssistRules).GetMethods(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.DeclaredOnly)
                .Select(m => m.Name).ToList();
            CollectionAssert.AreEquivalent(new[] { "On", "AddCheckpoints", "CheckpointSeconds", "ChaseSpeed", "ShadowBoost" }, names);
            foreach (var t in new[] { typeof(StageProgress), typeof(PlayerProgress), typeof(CountryProgress), typeof(Secrets), typeof(Aron.Logic.Costumes.Wardrobe), typeof(BishtPower), typeof(StoreInventory) })
                foreach (var m in t.GetMethods())
                    foreach (var p in m.GetParameters())
                        Assert.That(p.ParameterType, Is.Not.EqualTo(typeof(AccessSettings)), t.Name + "." + m.Name);
            Assert.That(AssistRules.Untouched, Does.Contain("progress").And.Contain("coins").And.Contain("medals"));
        }

        [Test]
        public void EveryAssistStringExistsInTwelveLanguages()
        {
            foreach (var key in new[] { "assist_mode", "assist_hint" })
                foreach (var l in UiLanguages.Codes)
                    Assert.That(UiStringTables.All[l].TryGetValue(key, out var v) && !string.IsNullOrWhiteSpace(v), Is.True, l + " " + key);
            foreach (var l in UiLanguages.Codes) Assert.That(UiStringTables.All[l]["assist_mode"], Does.Contain("{0}"), l);
        }
    }
}
