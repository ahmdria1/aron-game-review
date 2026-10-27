// (c) Ahmed Alamrani. All rights reserved.
using System.Linq;
using Aron.Logic.Feedback;
using Aron.Logic.Heroes;
using Aron.Logic.Passport;
using NUnit.Framework;

namespace Aron.Logic.Tests
{
    public class HapticTests
    {
        private static HapticFile File() => TestData.Read<HapticFile>("Config", "haptics.json");

        [Test] public void TableIsValid() => Assert.That(File().Validate(), Is.Empty);

        [Test]
        public void EveryEventHasAPatternAndNoPatternIsUnused()
        {
            var f = File();
            foreach (var ev in HapticFile.Events) Assert.That(f.For(ev), Is.Not.Null, ev);
            Assert.That(f.patterns.Select(p => p.id).Except(HapticFile.Events), Is.Empty, "no stray patterns");
            Assert.That(f.patterns.Select(p => p.id).Distinct().Count(), Is.EqualTo(f.patterns.Length));
        }

        [Test]
        public void TheKeysTheGameUsesAreInTheTable()
        {
            var f = File();
            foreach (var k in new[] { PassportBook.StampHaptic, PassportBook.VisaHaptic, SleeveRules.Haptic }) Assert.That(f.For(k), Is.Not.Null, k);
        }

        [Test]
        public void ContinuousEventsLoopAndShortOnesDoNot()
        {
            var f = File();
            foreach (var id in new[] { "race_boost", "drift", "gallop" }) Assert.That(f.For(id).loop, Is.True, id);
            foreach (var id in new[] { "coin", "box_break", "hit" }) Assert.That(f.For(id).loop, Is.False, id);
            Assert.That(f.For("coin").maxPerSecond, Is.LessThanOrEqualTo(15f));
        }

        [Test]
        public void PatternsAreShortAndAmplitudesAreInRange()
        {
            foreach (var p in File().patterns)
            {
                Assert.That(p.timings.Sum(), Is.LessThanOrEqualTo(1500), p.id);
                Assert.That(p.timings.Length, Is.EqualTo(p.amplitudes.Length), p.id);
                Assert.That(p.timings[0], Is.EqualTo(0), p.id + " starts without a pause");
            }
        }

        private static HapticGate Gate() => new HapticGate(File());

        [Test]
        public void OffInSettingsMeansNoVibration()
        {
            var g = Gate(); new HapticSettings { enabled = false }.Apply(g);
            Assert.That(g.Fire("hit", 1.0), Is.Null);
            new HapticSettings { enabled = true }.Apply(g);
            Assert.That(g.Fire("hit", 2.0), Is.Not.Null);
        }

        [Test]
        public void LowMediumHighScaleTheStrength()
        {
            int Peak(int level) { var g = Gate(); new HapticSettings { level = level }.Apply(g); return g.Fire("boss_hit", 1.0).Value.amplitudes.Max(); }
            Assert.That(Peak(0), Is.LessThan(Peak(1))); Assert.That(Peak(1), Is.LessThan(Peak(2)));
            Assert.That(Peak(2), Is.LessThanOrEqualTo(255));
            var g2 = Gate(); new HapticSettings { level = 0 }.Apply(g2);
            var r = g2.Fire("coin", 5.0).Value;
            for (int i = 0; i < r.amplitudes.Length; i++) Assert.That(r.amplitudes[i] == 0, Is.EqualTo(r.pattern.amplitudes[i] == 0), "pauses stay pauses, pulses never vanish");
        }

        [Test]
        public void BatterySaverBackgroundAndOsSwitchStopIt()
        {
            var g = Gate(); var s = new HapticSettings(); s.Apply(g);
            g.BatterySaver = true; Assert.That(g.Fire("hit", 1.0), Is.Null, "battery saver");
            s.offInBatterySaver = false; s.Apply(g); Assert.That(g.Fire("hit", 2.0), Is.Not.Null, "unless the player chose otherwise");
            g.AppInForeground = false; Assert.That(g.Fire("hit", 3.0), Is.Null, "in the background");
            g.AppInForeground = true; g.OsHapticsOn = false; Assert.That(g.Fire("hit", 4.0), Is.Null, "OS haptics off");
        }

        [Test]
        public void EachEventIsRateLimited()
        {
            var g = Gate(); new HapticSettings().Apply(g);
            int fired = 0; for (double t = 0; t < 1.0; t += 0.01) if (g.Fire("coin", t) != null) fired++;
            Assert.That(fired, Is.InRange(14, 16), "coins tick about 15 a second at most");
            int boss = 0; for (double t = 0; t < 1.0; t += 0.01) if (g.Fire("boss_defeat", t) != null) boss++;
            Assert.That(boss, Is.LessThanOrEqualTo(1));
            Assert.That(g.Fire("nonsense", 9.0), Is.Null);
        }

        [Test]
        public void LevelCycleWrapsAndDefaultsAreMediumOn()
        {
            var s = new HapticSettings(); Assert.That(s.enabled, Is.True); Assert.That(s.Intensity, Is.EqualTo(HapticIntensity.Medium));
            s.CycleLevel(); Assert.That(s.Intensity, Is.EqualTo(HapticIntensity.High)); s.CycleLevel(); Assert.That(s.Intensity, Is.EqualTo(HapticIntensity.Low)); s.CycleLevel(); Assert.That(s.Intensity, Is.EqualTo(HapticIntensity.Medium));
            Assert.That(new SaveData().haptics.enabled, Is.True);
        }

        [Test]
        public void SettingsSurviveASaveRoundTrip()
        {
            var d = new SaveData(); d.haptics.enabled = false; d.haptics.level = 2; d.camera.shake = 40; d.camera.invertY = true; d.access.oneHand = true;
            var back = System.Text.Json.JsonSerializer.Deserialize<SaveData>(System.Text.Json.JsonSerializer.Serialize(d, TestData.Opt), TestData.Opt);
            Assert.That(back.haptics.enabled, Is.False); Assert.That(back.haptics.level, Is.EqualTo(2));
            Assert.That(back.camera.shake, Is.EqualTo(40)); Assert.That(back.camera.invertY, Is.True); Assert.That(back.access.oneHand, Is.True);
        }
    }
}
