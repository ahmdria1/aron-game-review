// (c) Ahmed Alamrani. All rights reserved.
using System.Linq;
using NUnit.Framework;

namespace Aron.Logic.Tests
{
    public class CompanionAbilityTests
    {
        private static CompanionDef Def(string id, params int[] costs) => new CompanionDef
        {
            id = id,
            levels = costs.Select((c, i) => new CompanionLevel { cost = c, stamina = 3 + i, shieldHits = 1 + i / 2 }).ToArray(),
        };

        private static CompanionFile File() => new CompanionFile
        {
            companions = new[] { Def("camel", 0, 100, 300), Def("horse", 0, 200), Def("falcon", 0, 150), Def("oryx", 0, 120) },
        };

        [Test]
        public void FileIsValid() => Assert.That(File().Validate(), Is.Empty);

        [Test]
        public void MissingAnimalOrBadCostsAreErrors()
        {
            var f = File();
            f.companions = f.companions.Where(c => c.id != "oryx").ToArray();
            Assert.That(f.Validate(), Has.Some.Contains("missing oryx"));
            f = File();
            f.companions[0].levels[2].cost = 50;
            Assert.That(f.Validate(), Has.Some.Contains("must cost more"));
            f = File();
            f.companions[0].levels[1].cost = 0;
            f.companions[0].levels[0].cost = 10;
            Assert.That(f.Validate(), Has.Some.Contains("level 1 is free"));
        }

        [Test]
        public void WorseUpgradeIsAnError()
        {
            var f = File();
            f.companions[2].levels[1].scoutCooldown = 30f;
            Assert.That(f.Validate(), Has.Some.Contains("longer cooldown"));
            f = File();
            f.companions[1].levels[1].speed = 1f;
            Assert.That(f.Validate(), Has.Some.Contains("worse"));
        }

        [Test]
        public void UpgradeSpendsCoinsAndStopsAtMax()
        {
            var data = new SaveData { coins = 350 };
            var camel = File().Get("camel");
            Assert.That(CompanionUpgrades.Level(data, "camel"), Is.EqualTo(1));
            Assert.That(CompanionUpgrades.NextCost(camel, data), Is.EqualTo(100));
            Assert.That(CompanionUpgrades.TryUpgrade(camel, data), Is.True);
            Assert.That(data.coins, Is.EqualTo(250));
            Assert.That(CompanionUpgrades.Level(data, "camel"), Is.EqualTo(2));
            Assert.That(CompanionUpgrades.TryUpgrade(camel, data), Is.False, "300 needed, 250 left");
            data.coins = 300;
            Assert.That(CompanionUpgrades.TryUpgrade(camel, data), Is.True);
            Assert.That(CompanionUpgrades.NextCost(camel, data), Is.EqualTo(-1));
            Assert.That(CompanionUpgrades.TryUpgrade(camel, data), Is.False);
            Assert.That(data.companionLevels, Is.EqualTo(new[] { "camel=3" }));
            Assert.That(CompanionUpgrades.Current(File(), data, "camel").stamina, Is.EqualTo(5f));
        }

        [Test]
        public void SandSprintDrainsAndRecovers()
        {
            var s = new SandSprint(new CompanionLevel { stamina = 2f, staminaRegen = 1f, sprintBoost = 1.5f });
            Assert.That(s.Tick(1f, true, false), Is.EqualTo(1.5f));
            Assert.That(s.Stamina, Is.EqualTo(1f).Within(1e-4));
            s.Tick(1f, true, false);
            Assert.That(s.Stamina, Is.EqualTo(0f));
            Assert.That(s.Tick(0.1f, true, false), Is.EqualTo(1f), "empty: no sprint");
            s.Tick(0.6f, false, false);
            Assert.That(s.Tick(0.01f, true, false), Is.EqualTo(1.5f), "refilled past 30%");
        }

        [Test]
        public void SandSprintCostsHalfOnSand()
        {
            var s = new SandSprint(new CompanionLevel { stamina = 2f });
            s.Tick(1f, true, true);
            Assert.That(s.Stamina, Is.EqualTo(1.5f).Within(1e-4));
        }

        [Test]
        public void ShieldAbsorbsItsHitsThenStops()
        {
            var sh = new HitShield(1);
            Assert.That(sh.Absorb(), Is.True);
            Assert.That(sh.Absorb(), Is.False);
            sh.Refill(2);
            Assert.That(sh.Absorb() && sh.Absorb(), Is.True);
            Assert.That(sh.Left, Is.EqualTo(0));
        }

        [Test]
        public void DateCarryGivesWholeDatesOverTime()
        {
            var c = new DateCarry();
            int total = 0;
            for (int i = 0; i < 4; i++) total += c.Bonus(1, 0.5f);
            Assert.That(total, Is.EqualTo(2));
            Assert.That(c.Bonus(3, 1f), Is.EqualTo(3));
        }

        [Test]
        public void CooldownRunsThenRecharges()
        {
            var cd = new Cooldown(1f, 4f);
            Assert.That(cd.TryStart(0f), Is.True);
            Assert.That(cd.Active(0.5f), Is.True);
            Assert.That(cd.Active(1.2f), Is.False);
            Assert.That(cd.TryStart(2f), Is.False);
            Assert.That(cd.Fraction(3f), Is.EqualTo(0.5f).Within(1e-4));
            Assert.That(cd.Ready(5f), Is.True);
            Assert.That(cd.Fraction(6f), Is.EqualTo(1f));
        }

        [Test]
        public void HorseChargeBreaksBarriersAndDriftDodges()
        {
            var h = new HorseMoves(new CompanionLevel { chargeTime = 1f, chargeCooldown = 5f, driftDistance = 2f, driftCooldown = 2f, gallopJump = 1.5f });
            Assert.That(h.BreaksBarriers(0f), Is.False);
            Assert.That(h.Charge.TryStart(0f), Is.True);
            Assert.That(h.BreaksBarriers(0.5f), Is.True);
            Assert.That(h.SpeedMultiplier(0.5f), Is.GreaterThan(1f));
            Assert.That(h.BreaksBarriers(1.5f), Is.False);
            Assert.That(h.TryDrift(2f, -0.3f), Is.EqualTo(-2f));
            Assert.That(h.Dodging(2.1f), Is.True);
            Assert.That(h.TryDrift(2.5f, 1f), Is.EqualTo(0f), "still cooling down");
            Assert.That(h.TryDrift(5f, 1f), Is.EqualTo(2f));
            Assert.That(h.JumpMultiplier(true), Is.EqualTo(1.5f));
            Assert.That(h.JumpMultiplier(false), Is.EqualTo(1f));
        }

        [Test]
        public void FalconScoutRevealsAheadOnCooldown()
        {
            var f = new FalconScout(new CompanionLevel { scoutRange = 20f, scoutCooldown = 10f, warnDistance = 8f });
            var secrets = new[] { 5f, 18f, 40f, -10f };
            Assert.That(f.Reveal(0f, 0f, secrets), Is.EqualTo(new[] { 0, 1 }));
            Assert.That(f.Reveal(3f, 0f, secrets), Is.Empty, "cooling down");
            Assert.That(f.Scout.Fraction(7f), Is.EqualTo(0.5f).Within(1e-4));
            Assert.That(f.Reveal(13f, 25f, secrets), Is.EqualTo(new[] { 2 }));
        }

        [Test]
        public void FalconWarnsOncePerTrap()
        {
            var f = new FalconScout(new CompanionLevel { warnDistance = 8f });
            Assert.That(f.Warn(1, 12f), Is.False);
            Assert.That(f.Warn(1, 7f), Is.True);
            Assert.That(f.Warn(1, 3f), Is.False);
            Assert.That(f.Warn(2, 6f), Is.True);
            Assert.That(f.Warn(3, -1f), Is.False, "behind the player");
        }

        [Test]
        public void OryxLeadsToTheNearestWaterAheadAndSensesEnemies()
        {
            var o = new OryxSense(new CompanionLevel { leadRange = 20f, enemySense = 9f, chargeTime = 1f, chargeCooldown = 5f });
            Assert.That(o.Lead(10f, new[] { 5f, 40f, 25f, 22f }), Is.EqualTo(3));
            Assert.That(o.Lead(10f, new[] { 5f, 40f }), Is.EqualTo(-1));
            Assert.That(o.SensesEnemy(8f), Is.True);
            Assert.That(o.SensesEnemy(10f), Is.False);
            Assert.That(o.Horn.TryStart(0f), Is.True);
            Assert.That(o.Horn.TryStart(3f), Is.False);
            Assert.That(OryxSense.SideOffset(2f, 4f), Is.LessThan(0f));
        }
    }
}
