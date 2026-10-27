// (c) Ahmed Alamrani. All rights reserved.
using System.IO;
using System.Text.Json;
using Aron.Logic;
using Aron.Logic.Heroes;
using NUnit.Framework;

namespace Aron.Logic.Tests
{
    public class SleeveTests
    {
        private static SleeveDirector Aron() => new SleeveDirector("aron");
        private static void Run(SleeveDirector d, float seconds) { for (float t = 0; t < seconds; t += 0.05f) d.Tick(0.05f); }

        [Test]
        public void AnEnemyInRangeRollsTheSleevesUpInAboutAThirdOfASecondAndAHalf()
        {
            var d = Aron();
            d.EnemyNear(SleeveRules.EngageRange + 3f);
            Assert.That(d.Tick(0.1f), Is.EqualTo(SleeveCue.None)); Assert.That(d.State, Is.EqualTo(SleeveState.Down));
            d.EnemyNear(SleeveRules.EngageRange - 1f);
            Assert.That(d.Tick(0.05f), Is.EqualTo(SleeveCue.RollUp)); Assert.That(d.State, Is.EqualTo(SleeveState.RollingUp));
            Run(d, 0.3f); Assert.That(d.Progress, Is.InRange(0.4f, 0.7f));
            Run(d, 0.4f); Assert.That(d.State, Is.EqualTo(SleeveState.Up)); Assert.That(d.Progress, Is.EqualTo(1f));
            Assert.That(SleeveRules.RollUpSeconds, Is.EqualTo(0.6f));
        }

        [Test]
        public void TheFirstAttackRollsThemUpAndAnAttackCutsTheRollShort()
        {
            var d = Aron();
            d.Attacked();
            Assert.That(d.Tick(0.016f), Is.EqualTo(SleeveCue.RollUp));
            Assert.That(d.State, Is.EqualTo(SleeveState.Up), "an attack never waits for the roll");
            var e = Aron(); e.EnemyNear(2f); e.Tick(0.1f);
            Assert.That(e.State, Is.EqualTo(SleeveState.RollingUp));
            e.Attacked();
            Assert.That(e.State, Is.EqualTo(SleeveState.Up)); Assert.That(e.Progress, Is.EqualTo(1f));
        }

        [Test]
        public void SixQuietSecondsLowerThemCalmly()
        {
            var d = Aron(); d.Attacked(); d.Tick(0.016f);
            Run(d, SleeveRules.QuietSeconds - 0.5f); Assert.That(d.State, Is.EqualTo(SleeveState.Up));
            Run(d, 0.7f); Assert.That(d.State, Is.EqualTo(SleeveState.RollingDown));
            Run(d, SleeveRules.RollDownSeconds + 0.1f); Assert.That(d.State, Is.EqualTo(SleeveState.Down)); Assert.That(d.Progress, Is.EqualTo(0f));
        }

        [Test]
        public void FightingKeepsThemUpAndTheTimerRestarts()
        {
            var d = Aron(); d.Attacked(); d.Tick(0.016f);
            for (int i = 0; i < 5; i++) { Run(d, 4f); d.Attacked(); }
            Assert.That(d.State, Is.EqualTo(SleeveState.Up));
            Run(d, 3f); d.EnemyNear(3f); Run(d, 3f);
            Assert.That(d.State, Is.EqualTo(SleeveState.Up), "an enemy still nearby restarts the quiet timer");
        }

        [Test]
        public void ANewFightDuringTheRollDownBringsThemBackUp()
        {
            var d = Aron(); d.Attacked(); Run(d, 6.4f);
            Assert.That(d.State, Is.EqualTo(SleeveState.RollingDown));
            d.EnemyNear(1f); d.Tick(0.016f);
            Assert.That(d.State, Is.EqualTo(SleeveState.RollingUp));
        }

        [Test]
        public void BossStartIsSleevesFirstThenTheWrap()
        {
            var d = Aron();
            Assert.That(d.WrapMayStart, Is.False);
            d.BossStart(); d.Tick(0.016f);
            Assert.That(d.State, Is.EqualTo(SleeveState.RollingUp)); Assert.That(d.WrapMayStart, Is.False, "the wrap waits for the sleeves");
            Run(d, 0.7f);
            Assert.That(d.WrapMayStart, Is.True);
            Run(d, 30f); Assert.That(d.State, Is.EqualTo(SleeveState.Up), "a boss fight never lowers them");
            // and the real shemagh director follows
            var shemagh = new ShemaghDirector();
            if (d.WrapMayStart) shemagh.BeginBoss();
            Assert.That(shemagh.Current, Is.EqualTo(ShemaghStyles.FaceWrap));
        }

        [Test]
        public void BossWinLowersTheWrapWithASmileThenTheSleeves()
        {
            var d = Aron(); d.BossStart(); Run(d, 1f);
            var shemagh = new ShemaghDirector(); shemagh.BeginBoss();
            shemagh.EndBoss(true); d.BossEnd(true);
            Assert.That(shemagh.SmileAfterWin, Is.True); Assert.That(shemagh.Current, Is.EqualTo(ShemaghStyles.Royal));
            Run(d, SleeveRules.WrapOffSeconds - 0.2f); Assert.That(d.State, Is.EqualTo(SleeveState.Up), "sleeves wait for the wrap");
            var cue = SleeveCue.None;
            for (float t = 0; t < 0.5f; t += 0.05f) { var c = d.Tick(0.05f); if (c != SleeveCue.None) cue = c; }
            Assert.That(cue, Is.EqualTo(SleeveCue.RollDown)); Assert.That(d.State, Is.Not.EqualTo(SleeveState.Up));
            Run(d, 1.2f); Assert.That(d.State, Is.EqualTo(SleeveState.Down));
        }

        [Test]
        public void StageClearLowersThemAtOnceCalmly()
        {
            var d = Aron(); d.Attacked(); d.Tick(0.016f);
            d.StageClear(); d.Tick(0.05f);
            Assert.That(d.State, Is.EqualTo(SleeveState.RollingDown));
        }

        [Test]
        public void ALostBossFightLowersThemAfterAShortWhile()
        {
            var d = Aron(); d.BossStart(); Run(d, 1f); d.BossEnd(false);
            Run(d, 2f); Assert.That(d.State, Is.EqualTo(SleeveState.Up));
            Run(d, 2f); Assert.That(d.State, Is.Not.EqualTo(SleeveState.Up));
        }

        [Test]
        public void ResetPutsThemDownWithoutAnimation()
        {
            var d = Aron(); d.Attacked(); d.Tick(0.016f); d.Reset();
            Assert.That(d.State, Is.EqualTo(SleeveState.Down)); Assert.That(d.Progress, Is.EqualTo(0f)); Assert.That(d.Tick(0.1f), Is.EqualTo(SleeveCue.None));
        }

        [Test]
        public void NouraNeverRollsUpHerSleeves()
        {
            var n = new SleeveDirector("noura");
            n.EnemyNear(1f); n.Attacked(); n.BossStart(); n.StageClear(); n.BossEnd(true);
            for (int i = 0; i < 200; i++) Assert.That(n.Tick(0.05f), Is.EqualTo(SleeveCue.None));
            Assert.That(n.State, Is.EqualTo(SleeveState.Down)); Assert.That(n.Progress, Is.EqualTo(0f));
            Assert.That(SleeveRules.Allowed("noura"), Is.False); Assert.That(SleeveRules.Allowed("aron"), Is.True);
            Assert.That(n.WrapMayStart, Is.True, "nothing to wait for");
        }

        [Test]
        public void HeroDataAgreesAndTheHookNamesAreStable()
        {
            var dir = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
            while (!Directory.Exists(Path.Combine(dir.FullName, "Assets"))) dir = dir.Parent;
            var f = JsonSerializer.Deserialize<Aron.Logic.Heroes.HeroFile>(File.ReadAllText(Path.Combine(dir.FullName, "Assets/_Aron/Resources/Heroes/heroes.json")), new JsonSerializerOptions { IncludeFields = true });
            Assert.That(f.Get("aron").rollsSleeves, Is.True); Assert.That(f.Get("noura").rollsSleeves, Is.False);
            Assert.That(f.Validate(), Is.Empty);
            Assert.That(SleeveRules.Hook, Is.EqualTo("sleeve_roll"));
        }
    }
}
