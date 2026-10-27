// (c) Ahmed Alamrani. All rights reserved.
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using Aron.Logic.Localization;
using Aron.Logic.Races;
using NUnit.Framework;

namespace Aron.Logic.Tests
{
    /// <summary>Race mounts: stats, ranked normalisation, unlocks, purchases, points, rentals, saves and the shemagh styles.</summary>
    public class MountTests
    {
        private static readonly JsonSerializerOptions Opt = new JsonSerializerOptions { IncludeFields = true };
        private static readonly DateTime T0 = new DateTime(2026, 10, 27, 12, 0, 0, DateTimeKind.Utc);

        private static string Resources()
        {
            for (var d = new DirectoryInfo(TestContext.CurrentContext.TestDirectory); d != null; d = d.Parent)
            {
                var p = Path.Combine(d.FullName, "Assets", "_Aron", "Resources");
                if (Directory.Exists(Path.Combine(p, "Mounts"))) return p;
            }
            Assert.Fail("Resources not found");
            return null;
        }

        private static T Read<T>(params string[] parts) => JsonSerializer.Deserialize<T>(File.ReadAllText(Path.Combine(new[] { Resources() }.Concat(parts).ToArray())), Opt);
        private static MountFile Mounts() => Read<MountFile>("Mounts", "race_mounts.json");
        private static PointsTable Table() => Read<PointsTable>("Mounts", "race_points.json");
        private static StoreCatalog Catalog() => Read<StoreCatalog>("Store", "StoreCatalog.json");

        // ---- stats -------------------------------------------------------------------------------------------

        [Test]
        public void MountFileIsValidWithFiveMountsAndHorseIsTheFreeDefault()
        {
            var f = Mounts();
            Assert.That(f.Validate(), Is.Empty);
            Assert.That(f.mounts.Select(m => m.id), Is.EquivalentTo(new[] { "horse", "camel", "ostrich", "giraffe", "falcon_glider" }));
            Assert.That(f.Default.id, Is.EqualTo("horse"));
            Assert.That(f.Default.price.usd, Is.EqualTo(0f));
        }

        [Test]
        public void EachMountHasItsTradeOff()
        {
            var f = Mounts();
            MountTraits S(string id) => f.Find(id).stats;
            foreach (var other in new[] { "horse", "camel", "giraffe", "falcon_glider" })
                Assert.That(S("ostrich").topSpeed, Is.GreaterThan(S(other).topSpeed), "ostrich is fastest on the straights");
            Assert.That(S("ostrich").turning, Is.LessThan(f.mounts.Where(m => m.id != "ostrich").Min(m => m.stats.turning)), "ostrich turns worst");
            Assert.That(S("giraffe").jump, Is.GreaterThan(f.mounts.Where(m => m.id != "giraffe").Max(m => m.stats.jump)), "giraffe clears the most");
            Assert.That(S("giraffe").accel, Is.LessThan(f.mounts.Where(m => m.id != "giraffe").Min(m => m.stats.accel)), "giraffe starts slowest");
            Assert.That(f.Find("giraffe").ability.highShortcuts, Is.True);
            Assert.That(S("camel").sandResist, Is.GreaterThan(f.mounts.Where(m => m.id != "camel").Max(m => m.stats.sandResist)));
            Assert.That(S("camel").heatResist, Is.GreaterThan(f.mounts.Where(m => m.id != "camel").Max(m => m.stats.heatResist)));
            Assert.That(S("camel").topSpeed, Is.InRange(0.88f, 0.97f), "medium speed");
            var h = S("horse");
            Assert.That(new[] { h.topSpeed, h.accel, h.turning, h.jump }, Is.All.EqualTo(1f), "the horse is the even one");
            Assert.That(f.Find("falcon_glider").ability.blocksGroundPickups, Is.True);
            Assert.That(f.Find("falcon_glider").ability.amount, Is.InRange(0.5f, 2f), "short glides");
        }

        [Test]
        public void GlideBlocksGroundPickupsOnlyWhileGliding()
        {
            var f = Mounts(); var falcon = f.Find("falcon_glider");
            var g = new GlideState(falcon.ability, falcon.ability.amount);
            Assert.That(g.CanCollectGround, Is.True);
            Assert.That(g.Start(), Is.True);
            Assert.That(g.CanCollectGround, Is.False);
            g.Tick(0.5f); Assert.That(g.Gliding, Is.True);
            g.Tick(1f); Assert.That(g.Gliding, Is.False);
            Assert.That(g.CanCollectGround, Is.True);
            var horse = new GlideState(f.Find("horse").ability, 0f);
            Assert.That(horse.Start(), Is.False, "only the falcon glides");
        }

        [Test]
        public void TerrainFavoursTheCamelAndHurtsTheHorseOnSand()
        {
            var f = Mounts();
            float Sand(string id, string weather) => TerrainRules.SpeedFactor(f.Find(id).stats, "sand", weather, "day");
            Assert.That(Sand("camel", "haze"), Is.GreaterThan(Sand("horse", "haze")));
            Assert.That(Sand("camel", "clear"), Is.GreaterThan(Sand("horse", "clear")));
            Assert.That(Sand("horse", "haze"), Is.LessThan(Sand("horse", "clear")), "a sandstorm slows more");
            Assert.That(TerrainRules.SpeedFactor(f.Find("horse").stats, "snow", "snow", "day"), Is.EqualTo(1f));
            Assert.That(TerrainRules.SpeedFactor(f.Find("camel").stats, "sand", "clear", "night"), Is.GreaterThan(0.8f));
        }

        [Test]
        public void CornersAndObstaclesFollowTurningAndJump()
        {
            var f = Mounts();
            Assert.That(TerrainRules.CornerFactor(f.Find("ostrich").stats, 1f), Is.LessThan(TerrainRules.CornerFactor(f.Find("horse").stats, 1f)));
            Assert.That(TerrainRules.CornerFactor(f.Find("falcon_glider").stats, 1f), Is.EqualTo(1f).Within(1e-5));
            Assert.That(TerrainRules.CornerFactor(f.Find("ostrich").stats, 0f), Is.EqualTo(1f));
            Assert.That(TerrainRules.ObstacleClearance(f.Find("giraffe").stats, f.Find("giraffe").ability),
                        Is.GreaterThan(TerrainRules.ObstacleClearance(f.Find("horse").stats, f.Find("horse").ability) + 0.5f));
            Assert.That(TerrainRules.StraightFactor(f.Find("ostrich").ability, 10f), Is.GreaterThan(1f));
            Assert.That(TerrainRules.StraightFactor(f.Find("horse").ability, 10f), Is.EqualTo(1f));
        }

        // ---- modes ------------------------------------------------------------------------------------------

        [Test]
        public void StoryAndStageRacesUseTheRealStats()
        {
            var f = Mounts();
            foreach (var mode in new[] { RaceMode.Story, RaceMode.Stage, RaceMode.Ghost })
                foreach (var m in f.mounts)
                {
                    var s = f.StatsFor(m, mode);
                    Assert.That(s.topSpeed, Is.EqualTo(m.stats.topSpeed));
                    Assert.That(s.jump, Is.EqualTo(m.stats.jump));
                    Assert.That(f.AbilityAmount(m, mode), Is.EqualTo(m.ability.amount));
                }
        }

        [Test]
        public void RankedRacesGiveEveryMountTheSameOverallPower()
        {
            var f = Mounts();
            foreach (var m in f.mounts)
                Assert.That(MountFile.Power(f.StatsFor(m, RaceMode.Ranked)), Is.EqualTo(f.powerBudget).Within(1e-4), m.id);
            var powers = f.mounts.Select(m => MountFile.Power(f.StatsFor(m, RaceMode.Ranked))).ToList();
            Assert.That(powers.Max() - powers.Min(), Is.LessThan(1e-4));
        }

        [Test]
        public void RankedKeepsTheStyleAndNeverMovesFarFromTheRealStats()
        {
            var f = Mounts();
            foreach (var m in f.mounts)
            {
                var r = f.StatsFor(m, RaceMode.Ranked);
                Assert.That(r.topSpeed / r.turning, Is.EqualTo(m.stats.topSpeed / m.stats.turning).Within(1e-4), m.id + " keeps its shape");
                Assert.That(r.topSpeed / m.stats.topSpeed, Is.InRange(0.96f, 1.04f), m.id);
            }
            var o = f.StatsFor(f.Find("ostrich"), RaceMode.Ranked); var h = f.StatsFor(f.Find("horse"), RaceMode.Ranked);
            Assert.That(o.topSpeed, Is.GreaterThan(h.topSpeed)); Assert.That(o.turning, Is.LessThan(h.turning));
            Assert.That(f.StatsFor(f.Find("horse"), RaceMode.Ranked).topSpeed, Is.EqualTo(1f).Within(1e-5), "the horse is already at the budget");
        }

        [Test]
        public void RankedGlideScalesWithTheSameFactor()
        {
            var f = Mounts(); var fg = f.Find("falcon_glider");
            float k = f.powerBudget / MountFile.Power(fg.stats);
            Assert.That(f.AbilityAmount(fg, RaceMode.Ranked), Is.EqualTo(fg.ability.amount * k).Within(1e-5));
        }

        [Test]
        public void FixedRaceWeatherStillAppliesInRankedRaces()
        {
            var races = Read<RaceFile>("Races", "tracks.json");
            var f = Mounts();
            var sand = races.tracks.First(t => t.surface == "sand" && t.weather == "clear");
            var horse = f.StatsFor(f.Find("horse"), RaceMode.Ranked); var camel = f.StatsFor(f.Find("camel"), RaceMode.Ranked);
            // The track's own weather decides: a clear day on sand is hot and sandy, the same track at night is only sandy.
            Assert.That(TerrainRules.SpeedFactor(horse, sand.surface, sand.weather, "day"), Is.LessThan(TerrainRules.SpeedFactor(horse, sand.surface, sand.weather, "night")));
            Assert.That(TerrainRules.SpeedFactor(camel, sand.surface, sand.weather, "day"), Is.GreaterThan(TerrainRules.SpeedFactor(horse, sand.surface, sand.weather, "day")));
            // A haze (sandstorm) race would slow more; no shipped track uses it yet, the rule is ready for one.
            Assert.That(TerrainRules.SpeedFactor(horse, "sand", "haze", "day"), Is.LessThan(TerrainRules.SpeedFactor(horse, "sand", "clear", "night")));
            Assert.That(Array.IndexOf(Aron.Logic.Sky.WeatherPolicy.RaceWeathers, "haze"), Is.GreaterThanOrEqualTo(0));
        }

        // ---- unlocks, purchases, shop ------------------------------------------------------------------------

        [Test]
        public void ShopMountsMatchTheMountFileAndTheirPrices()
        {
            var cat = Catalog(); var f = Mounts();
            Assert.That(cat.Validate(), Is.Null);
            foreach (var m in f.mounts)
            {
                var item = cat.Find("mount_" + m.id);
                Assert.IsNotNull(item, m.id);
                Assert.That(item.tab, Is.EqualTo(StoreTabs.Mounts)); Assert.That(item.slot, Is.EqualTo("mount"));
                Assert.That(item.isDefault, Is.EqualTo(m.isDefault));
                if (m.isDefault) continue;
                Assert.That(item.productId, Is.EqualTo(m.price.productId));
                Assert.That(item.productType, Is.EqualTo(ProductKinds.NonConsumable));
                Assert.That(item.usd, Is.EqualTo(m.price.usd));
                Assert.That(item.coinPrice, Is.EqualTo(0), "no random or coin-gambling route");
            }
            var prices = f.mounts.Where(m => !m.isDefault).OrderBy(m => m.price.usd).Select(m => m.price.usd).ToArray();
            Assert.That(prices, Is.EqualTo(new[] { 1.99f, 2.99f, 3.99f, 4.99f }));
        }

        [Test]
        public void NoRandomItemsAndNoAdsInTheNewCatalogEntries()
        {
            var cat = Catalog();
            foreach (var i in cat.items.Where(i => i.tab == StoreTabs.Mounts || i.slot == ShemaghStyles.ThobeSlot))
            {
                Assert.That(i.IsConsumable, Is.False, i.id + " is bought once, not drawn");
                Assert.That(i.removesAds, Is.False);
                Assert.That(i.helper, Is.Empty.Or.Null);
                Assert.That(i.id.ToLowerInvariant(), Does.Not.Contain("box").And.Not.Contain("gacha").And.Not.Contain("loot").And.Not.Contain("random"));
            }
        }

        [Test]
        public void DefaultMountIsTheHorseAndOldSavesKeepIt()
        {
            var old = new SaveData { version = 3, ownedItems = null };
            var inv = new StoreInventory(Catalog(), old);
            Assert.That(old.version, Is.EqualTo(4));
            Assert.That(inv.Equipped("mount").id, Is.EqualTo("mount_horse"));
            Assert.That(inv.Owns("mount_horse"), Is.True);
            Assert.That(inv.Owns("mount_camel"), Is.False);
            Assert.That(old.racePoints, Is.EqualTo(0)); Assert.That(old.pointLog, Is.Not.Null); Assert.That(old.rentals, Is.Not.Null);
            Assert.That(old.rivalChatter, Is.EqualTo(1)); Assert.That(old.aiDifficulty, Is.EqualTo("normal"));
                        Assert.That(inv.Equipped(ShemaghStyles.ThobeSlot).id, Is.EqualTo("thobe_c_white"));
        }

        [Test]
        public void SaveFromBeforeMountsRoundTripsThroughJson()
        {
            string json = "{\"version\":3,\"coins\":120,\"ownedItems\":[\"shemagh_gold\"],\"highestLevelCompleted\":4}";
            var d = JsonSerializer.Deserialize<SaveData>(json, Opt);
            var inv = new StoreInventory(Catalog(), d);
            Assert.That(d.version, Is.EqualTo(4)); Assert.That(d.coins, Is.EqualTo(120));
            Assert.That(inv.Owns("shemagh_gold"), Is.True);
            Assert.That(inv.Equipped("mount").id, Is.EqualTo("mount_horse"));
            var back = JsonSerializer.Deserialize<SaveData>(JsonSerializer.Serialize(d, Opt), Opt);
            Assert.That(back.version, Is.EqualTo(4)); Assert.That(back.racePointsLifetime, Is.EqualTo(0));
        }

        [Test]
        public void PurchaseGivesTheMountForGoodAndIsSafeToRepeat()
        {
            var d = new SaveData(); var inv = new StoreInventory(Catalog(), d);
            Assert.That(inv.GrantPurchase("com.ahmdria1.aron.mount_ostrich", "order-1"), Is.EqualTo(Grant.Granted));
            Assert.That(inv.Owns("mount_ostrich"), Is.True); Assert.That(inv.OwnsForGood("mount_ostrich"), Is.True);
            Assert.That(inv.Equipped("mount").id, Is.EqualTo("mount_ostrich"));
            Assert.That(inv.GrantPurchase("com.ahmdria1.aron.mount_ostrich", "order-1"), Is.EqualTo(Grant.Duplicate));
            Assert.That(inv.GrantPurchase("com.ahmdria1.aron.mount_ostrich", "order-2"), Is.EqualTo(Grant.AlreadyOwned), "a restore never charges twice");
            Assert.That(d.coins, Is.EqualTo(0), "mounts give no coin bonus");
            Assert.That(inv.Owns("mount_giraffe"), Is.False);
            Assert.That(inv.BuyWithCoins("mount_giraffe"), Is.EqualTo(CoinPurchase.NotForCoins), "mounts are never coin gambles");
        }

        [Test]
        public void NoPathBuysAMountOrVehicleForGoodWithPointsOrGold()
        {
            // The old earn-by-play columns are gone from the code and the data.
            Assert.That(typeof(StoreItem).GetField("unlockPoints"), Is.Null); Assert.That(typeof(StoreItem).GetField("unlockGold"), Is.Null);
            Assert.That(typeof(MountPrice).GetField("points"), Is.Null); Assert.That(typeof(MountPrice).GetField("goldMedals"), Is.Null);
            Assert.That(typeof(StoreInventory).GetMethod("GrantEarned"), Is.Null);
            Assert.That(typeof(StoreItem).GetProperty("SoldForPoints"), Is.Null);
            foreach (var file in new[] { Path.Combine("Store", "StoreCatalog.json"), Path.Combine("Mounts", "race_mounts.json"), Path.Combine("Mounts", "race_vehicles.json"), Path.Combine("Mounts", "race_points.json") })
            {
                string text = File.ReadAllText(Path.Combine(Resources(), file));
                foreach (var gone in new[] { "unlockPoints", "unlockGold", "goldMedals", "\"bonus\"", "ghostBonus" }) Assert.That(text, Does.Not.Contain(gone), file + " " + gone);
            }
            // Whatever the player has earned, every mount and vehicle that is not the free default stays unowned.
            var d = new SaveData { racePoints = 99999, racePointsLifetime = 999999 };
            for (int i = 0; i < 40; i++) d.raceMedals.Add("t" + i + "=1");
            var inv = new StoreInventory(Catalog(), d) { Clock = () => T0 };
            foreach (var i in Catalog().items.Where(i => i.IsVehicle && !i.isDefault))
            {
                Assert.That(inv.OwnsForGood(i.id), Is.False, i.id);
                Assert.That(inv.BuyWithCoins(i.id), Is.Not.EqualTo(CoinPurchase.Bought), i.id);
            }
        }

        [Test]
        public void SpendingPointsOnRentalsNeverLowersTheUnlockTotal()
        {
            var d = new SaveData(); var inv = new StoreInventory(Catalog(), d) { Clock = () => T0 };
            d.racePoints = 250; d.racePointsLifetime = 250;
            Assert.That(inv.Rent("thobe_c_navy"), Is.EqualTo(StoreInventory.RentResult.Rented));
            Assert.That(d.racePoints, Is.EqualTo(150)); Assert.That(d.racePointsLifetime, Is.EqualTo(250));
        }

        [Test]
        public void SaddlesAreCosmeticAndCoinPricedLooksNeverChangeStats()
        {
            var cat = Catalog(); var f = Mounts();
            foreach (var m in f.mounts)
            {
                var royal = cat.Find("saddle_royal_" + m.id); var sadu = cat.Find("saddle_sadu_" + m.id);
                var tassels = cat.Find("deco_tassels_" + m.id);
                Assert.That(royal.SoldForMoney && royal.usd > 0f, Is.True, m.id);
                Assert.That(sadu.SoldForCoins && !sadu.SoldForMoney && sadu.coinPrice == 300, Is.True, "a look you can earn the coins for");
                Assert.That(tassels.SoldForCoins && !tassels.SoldForMoney && tassels.coinPrice == 500, Is.True);
                Assert.That(cat.DefaultFor("saddle_" + m.id), Is.Not.Null); Assert.That(cat.DefaultFor("deco_" + m.id), Is.Not.Null);
            }
            Assert.That(typeof(StoreItem).GetFields().Select(x => x.Name), Does.Not.Contain("speedBonus"), "cosmetics have no stat fields");
        }

        // ---- points -----------------------------------------------------------------------------------------

        [Test]
        public void PointsTableIsValidAndPaysExactlyFiveThreeOne()
        {
            var t = Table();
            Assert.That(t.Validate(), Is.Empty);
            Assert.That(t.finish, Is.EqualTo(new[] { 5, 3, 1 }));
            Assert.That(typeof(PointsTable).GetField("bonus"), Is.Null, "no winner bonus");
            Assert.That(typeof(PointsTable).GetField("ghostBonus"), Is.Null, "no ghost bonus");
            Assert.That(t.hardMultiplier, Is.EqualTo(1.5f)); Assert.That(t.onlineMultiplier, Is.EqualTo(2f)); Assert.That(t.minRealRacersOnline, Is.EqualTo(2));
            Assert.That(t.rentPoints, Is.EqualTo(100)); Assert.That(t.rentHours, Is.EqualTo(48));
            Assert.That(Catalog().rentPoints, Is.EqualTo(t.rentPoints)); Assert.That(Catalog().rentHours, Is.EqualTo(t.rentHours));
        }

        [TestCase(1, 5)] [TestCase(2, 3)] [TestCase(3, 1)] [TestCase(4, 0)] [TestCase(8, 0)]
        public void AiRaceOnNormalPaysFiveThreeOne(int place, int total)
            => Assert.That(RacePoints.Award(Table(), PointsSource.AiRace, "normal", place, 1).Total, Is.EqualTo(total));

        [Test]
        public void HardMultipliesByOnePointFiveRoundedDownAndEasyPaysNothing()
        {
            var t = Table();
            Assert.That(new[] { 1, 2, 3 }.Select(p => RacePoints.Award(t, PointsSource.AiRace, "hard", p, 1).Total), Is.EqualTo(new[] { 7, 4, 1 }), "7.5, 4.5 and 1.5 round down");
            for (int p = 1; p <= 3; p++) Assert.That(RacePoints.Award(t, PointsSource.AiRace, "easy", p, 1).Total, Is.EqualTo(0));
            Assert.That(RacePoints.Award(t, PointsSource.AiRace, "easy", 1, 1).reason, Is.EqualTo("easy"));
        }

        [Test]
        public void OnlineDoublesOnlyWithTwoRealRacersAndComputerRacersDoNotCount()
        {
            var t = Table();
            Assert.That(RacePoints.Award(t, PointsSource.Online, "normal", 1, 1).Total, Is.EqualTo(0), "alone with computer racers");
            Assert.That(RacePoints.Award(t, PointsSource.Online, "normal", 1, 0).Total, Is.EqualTo(0));
            Assert.That(new[] { 1, 2, 3 }.Select(p => RacePoints.Award(t, PointsSource.Online, "normal", p, 2).Total), Is.EqualTo(new[] { 10, 6, 2 }));
            Assert.That(RacePoints.Award(t, PointsSource.Online, "normal", 3, 8).Total, Is.EqualTo(2));
            Assert.That(RacePoints.Award(t, PointsSource.Online, "normal", 4, 8).Total, Is.EqualTo(0));
            Assert.That(new[] { 1, 2, 3 }.Select(p => RacePoints.Award(t, PointsSource.Online, "hard", p, 2).Total), Is.EqualTo(new[] { 15, 9, 3 }), "hard x1.5 and online x2");
            Assert.That(RacePoints.Award(t, PointsSource.Online, "easy", 1, 4).Total, Is.EqualTo(0));
        }

        [Test]
        public void GhostRacesPayTheNormalFiveThreeOneOnNormalAndHardOnly()
        {
            var t = Table();
            Assert.That(new[] { 1, 2, 3 }.Select(p => RacePoints.Award(t, PointsSource.Ghost, "normal", p, 1).Total), Is.EqualTo(new[] { 5, 3, 1 }));
            Assert.That(new[] { 1, 2, 3 }.Select(p => RacePoints.Award(t, PointsSource.Ghost, "hard", p, 1).Total), Is.EqualTo(new[] { 7, 4, 1 }));
            Assert.That(RacePoints.Award(t, PointsSource.Ghost, "easy", 1, 1).Total, Is.EqualTo(0));
            Assert.That(RacePoints.Award(t, PointsSource.Ghost, "normal", 4, 1).Total, Is.EqualTo(0));
            Assert.That(RacePoints.Award(t, PointsSource.Ghost, "normal", 1, 1, finished: false).Total, Is.EqualTo(0));
        }

        [Test]
        public void GrantBanksPointsAndStopsAtTheDailyLimit()
        {
            var t = Table(); var d = new SaveData();
            for (int i = 0; i < t.dailyPerTrack; i++)
                Assert.That(RacePoints.Grant(d, t, PointsSource.AiRace, "normal", "riyadh", 1, 1, T0).Total, Is.EqualTo(5));
            Assert.That(d.racePoints, Is.EqualTo(5 * t.dailyPerTrack)); Assert.That(d.racePointsLifetime, Is.EqualTo(5 * t.dailyPerTrack));
            var over = RacePoints.Grant(d, t, PointsSource.AiRace, "normal", "riyadh", 1, 1, T0);
            Assert.That(over.Total, Is.EqualTo(0)); Assert.That(over.reason, Is.EqualTo("daily_limit"));
            Assert.That(RacePoints.Grant(d, t, PointsSource.AiRace, "normal", "other_track", 1, 1, T0).Total, Is.EqualTo(5), "another track has its own limit");
            Assert.That(RacePoints.Grant(d, t, PointsSource.AiRace, "normal", "riyadh", 1, 1, T0.AddDays(1)).Total, Is.EqualTo(5), "the next day starts fresh");
            Assert.That(d.pointLog.All(e => e.StartsWith("2026-10-28|")), Is.True, "yesterday's log is dropped");
        }

        [Test]
        public void OnlinePointsHaveAnOverallDailyLimitToo()
        {
            var t = Table(); var d = new SaveData(); int granted = 0;
            for (int i = 0; i < 40; i++)
                if (RacePoints.Grant(d, t, PointsSource.Online, "normal", "track" + i, 1, 4, T0).Total > 0) granted++;
            Assert.That(granted, Is.EqualTo(t.dailyOnline));
        }

        [Test]
        public void AllRacesTogetherPayAtMostOneRentalAWithTheLastRacePartial()
        {
            var t = Table(); var d = new SaveData(); int total = 0;
            for (int i = 0; i < 60; i++) total += RacePoints.Grant(d, t, PointsSource.Online, "hard", "track" + (i % 30), 1, 4, T0.AddMinutes(i)).Total;
            Assert.That(total, Is.EqualTo(t.dailyPoints)); Assert.That(d.racePoints, Is.EqualTo(100));
            Assert.That(RacePoints.Grant(d, t, PointsSource.AiRace, "normal", "fresh_track", 1, 1, T0).reason, Is.EqualTo("daily_points_limit"));
            d.racePoints = 0; d.pointLog.Clear();
            for (int i = 0; i < 7; i++) RacePoints.Grant(d, t, PointsSource.Online, "hard", "x" + i, 1, 4, T0);
            Assert.That(d.racePoints, Is.EqualTo(100), "six 15-point finishes make 90; the seventh pays only the 10 left");
            Assert.That(RacePoints.Grant(d, t, PointsSource.AiRace, "normal", "next_day", 1, 1, T0.AddDays(1)).Total, Is.EqualTo(5));
        }

        [Test]
        public void ServerCheckAcceptsAGoodClaimAndRefusesBadOnes()
        {
            var races = Read<RaceFile>("Races", "tracks.json"); var tr = races.tracks[0];
            var v = new ClaimValidator(races, "salt");
            float minTime = Aron.Logic.Safety.ScoreCheck.MinimumSeconds(tr, races, 1.2f);
            RaceClaim Make(string match, int place, int real, int ai, float seconds)
            {
                var c = new RaceClaim { playerId = "p1", matchId = match, track = tr.id, place = place, realRacers = real, aiRacers = ai, seconds = seconds };
                c.signature = ClaimValidator.SignClaim(c, "salt"); return c;
            }
            Assert.That(v.Check(Make("m1", 1, 3, 5, minTime * 1.3f), 1.2f), Is.Null);
            Assert.That(v.Check(Make("m1", 1, 3, 5, minTime * 1.3f), 1.2f), Is.EqualTo("duplicate_match"));
            Assert.That(v.Check(Make("m2", 1, 1, 7, minTime * 1.3f), 1.2f), Is.EqualTo("not_enough_real_racers"));
            Assert.That(v.Check(Make("m3", 9, 3, 5, minTime * 1.3f), 1.2f), Is.EqualTo("bad_place"));
            Assert.That(v.Check(Make("m4", 1, 5, 5, minTime * 1.3f), 1.2f), Is.EqualTo("bad_field_size"));
            Assert.That(v.Check(Make("m5", 1, 3, 5, 3f), 1.2f), Is.EqualTo("implausible_time"));
            var forged = Make("m6", 1, 3, 5, minTime * 1.3f); forged.place = 1; forged.realRacers = 8; forged.aiRacers = 0;
            Assert.That(v.Check(forged, 1.2f), Is.EqualTo("bad_signature"));
        }

        // ---- rentals ----------------------------------------------------------------------------------------

        [Test]
        public void RentalCosts100PointsLasts48HoursAndThenReturnsTheDefault()
        {
            var d = new SaveData { racePoints = 100, racePointsLifetime = 100 };
            var now = T0; var inv = new StoreInventory(Catalog(), d) { Clock = () => now };
            Assert.That(inv.Rent("thobe_c_olive"), Is.EqualTo(StoreInventory.RentResult.Rented));
            Assert.That(d.racePoints, Is.EqualTo(0));
            Assert.That(inv.Owns("thobe_c_olive"), Is.True); Assert.That(inv.OwnsForGood("thobe_c_olive"), Is.False);
            Assert.That(inv.Equip("thobe_c_olive"), Is.True);
            Assert.That(inv.Equipped(ShemaghStyles.ThobeSlot).id, Is.EqualTo("thobe_c_olive"));
            now = T0.AddHours(47.9); Assert.That(inv.IsRented("thobe_c_olive"), Is.True);
            now = T0.AddHours(48.1);
            Assert.That(inv.IsRented("thobe_c_olive"), Is.False);
                        inv.ClearExpiredRentals(); Assert.That(d.rentals, Is.Empty);
        }

        [Test]
        public void RentalRulesRefuseWhatIsOwnedTooPoorOrNotALook()
        {
            var d = new SaveData { racePoints = 99 }; var inv = new StoreInventory(Catalog(), d) { Clock = () => T0 };
            Assert.That(inv.Rent("thobe_c_olive"), Is.EqualTo(StoreInventory.RentResult.NotEnoughPoints));
            d.racePoints = 500;
            Assert.That(inv.Rent("thobe_c_white"), Is.EqualTo(StoreInventory.RentResult.NotRentable), "defaults are free already");
            Assert.That(inv.Rent("coins_small"), Is.EqualTo(StoreInventory.RentResult.NotRentable));
            Assert.That(inv.Rent("nope"), Is.EqualTo(StoreInventory.RentResult.Unknown));
            inv.GrantPurchase("com.ahmdria1.aron.thobe_c_brown", "o1");
            Assert.That(inv.Rent("thobe_c_brown"), Is.EqualTo(StoreInventory.RentResult.AlreadyOwned));
            Assert.That(d.racePoints, Is.EqualTo(500), "nothing was charged");
            Assert.That(inv.Rent("mount_ostrich"), Is.EqualTo(StoreInventory.RentResult.Rented), "mounts rent too");
            Assert.That(inv.Rent("shemagh_gold"), Is.EqualTo(StoreInventory.RentResult.Rented), "the older looks rent too");
        }

        [Test]
        public void RentingAgainAddsTimeAndWindingTheClockBackDoesNotExtendIt()
        {
            var d = new SaveData { racePoints = 500 }; var now = T0; var inv = new StoreInventory(Catalog(), d) { Clock = () => now };
            inv.Rent("thobe_c_navy"); now = T0.AddHours(24); inv.Rent("thobe_c_navy");
            Assert.That(inv.RentalEnds("thobe_c_navy").Value, Is.EqualTo(T0.AddHours(96)));
            now = T0.AddHours(-500);
            Assert.That(inv.IsRented("thobe_c_navy"), Is.True);
            now = T0.AddHours(100); Assert.That(inv.IsRented("thobe_c_navy"), Is.False);
            now = T0.AddHours(10); Assert.That(inv.IsRented("thobe_c_navy"), Is.False, "clock set back after expiry stays expired");
        }

        // ---- merge ------------------------------------------------------------------------------------------

        [Test]
        public void MergingTwoDevicesKeepsEverythingBoughtOrEarned()
        {
            var a = new SaveData { ownedItems = new List<string> { "mount_camel" }, racePointsLifetime = 100, stages = new List<Regions.StageRecord>() };
            var b = new SaveData { ownedItems = new List<string> { "mount_ostrich" }, racePointsLifetime = 900, stages = new List<Regions.StageRecord> { new Regions.StageRecord() } };
            var m = Aron.Logic.Safety.SaveMerge.Merge(a, 1, b, 2);
            Assert.That(m.ownedItems, Is.EquivalentTo(new[] { "mount_camel", "mount_ostrich" }));
            Assert.That(m.racePointsLifetime, Is.EqualTo(900));
        }

        // ---- strings and names ------------------------------------------------------------------------------

        [Test]
        public void EveryStringKeyExistsInAllTwelveLanguages()
        {
            var all = UiStringTables.All;
            Assert.That(all.Keys, Is.EquivalentTo(UiLanguages.Codes));
            var ar = all["ar"].Keys.ToList();
            foreach (var l in UiLanguages.Codes)
            {
                Assert.That(all[l].Keys.Except(ar), Is.Empty, l + " has keys ar lacks");
                Assert.That(ar.Except(all[l].Keys), Is.Empty, l + " is missing keys");
                foreach (var kv in all[l]) Assert.That(string.IsNullOrWhiteSpace(kv.Value), Is.False, l + ":" + kv.Key);
            }
        }

        [Test]
        public void PlaceholdersMatchAcrossLanguages()
        {
            var all = UiStringTables.All;
            var rx = new System.Text.RegularExpressions.Regex(@"\{\d\}");
            foreach (var key in all["en"].Keys)
            {
                var expect = rx.Matches(all["en"][key]).Select(m => m.Value).Distinct().OrderBy(x => x).ToArray();
                foreach (var l in UiLanguages.Codes)
                    Assert.That(rx.Matches(all[l][key]).Select(m => m.Value).Distinct().OrderBy(x => x).ToArray(), Is.EqualTo(expect), l + ":" + key);
            }
        }

        [Test]
        public void ItemNamesAreComposedInEveryLanguage()
        {
            var cat = Catalog();
            foreach (var l in UiLanguages.Codes)
            {
                var t = UiStringTables.All[l];
                Func<string, string> get = k => t[k];
                Func<string, object[], string> fmt = (k, a) => string.Format(t[k], a);
                Func<string, bool> has = k => t.ContainsKey(k);
                foreach (var i in cat.items.Where(i => i.tab == StoreTabs.Mounts || i.slot == ShemaghStyles.ThobeSlot))
                {
                    string n = StoreItemNames.Name(i, get, fmt, has);
                    Assert.That(n, Is.Not.Empty, i.id);
                    if (i.isDefault && i.tab == StoreTabs.Mounts && i.slot != "mount") continue;
                    if (l != "ar") Assert.That(n, Is.Not.EqualTo(i.nameAr), l + " " + i.id + " should not fall back to the Arabic name");
                }
            }
            var ar = UiStringTables.All["ar"];
            Assert.That(StoreItemNames.Name(cat.Find("thobe_c_navy"), k => ar[k], (k, a) => string.Format(ar[k], a), k => ar.ContainsKey(k)), Does.Contain("ثوب كحلي"));
        }

        // ---- shemagh styles ---------------------------------------------------------------------------------

        [Test]
        public void EightThobeColoursAreInTheCatalogAndNoShemaghStylesAreSold()
        {
            var cat = Catalog();
            Assert.That(ShemaghStyles.ThobeColours.Length, Is.EqualTo(8));
            foreach (var c in ShemaghStyles.ThobeColours) Assert.That(cat.Find(ShemaghStyles.ThobeItemId(c)), Is.Not.Null, c);
            Assert.That(cat.DefaultFor(ShemaghStyles.ThobeSlot).id, Is.EqualTo("thobe_c_white"));
            Assert.That(cat.items.Count(i => i.slot == ShemaghStyles.ThobeSlot), Is.EqualTo(8));
            foreach (var i in cat.items.Where(i => i.slot == ShemaghStyles.ThobeSlot && !i.isDefault))
            {
                Assert.That(i.SoldForMoney, Is.True, i.id + " has a direct price"); Assert.That(i.usd, Is.GreaterThan(0f));
                Assert.That(i.productType, Is.EqualTo(ProductKinds.NonConsumable)); Assert.That(i.CanRent, Is.True);
            }
            // The scope is two looks (royal and talathum) and neither is for sale.
            foreach (var i in cat.items)
            {
                Assert.That(i.slot, Is.Not.EqualTo("shemagh_style"));
                Assert.That(i.id, Does.Not.StartWith("style_"));
            }
            foreach (var gone in new[] { "shiyookh", "saqr", "amaliya", "shababiya", "cobra", "saifain", "fanniya", "faris", "bint_albakkar", "malakiya" })
            {
                Assert.That(cat.items.Any(i => i.id.Contains(gone)), Is.False, gone);
                foreach (var l in UiStringTables.All.Values) Assert.That(l.Keys.Any(k => k.StartsWith("style_") || k.Contains("bint_albakkar") || k.Contains("shiyookh")), Is.False, gone);
            }
        }

        [Test]
        public void NoRemovedStyleNameAppearsInTheVoiceLines()
        {
            string csv = File.ReadAllText(Path.Combine(Resources(), "..", "..", "..", "docs", "vo_lines.csv"));
            foreach (var gone in new[] { "shiyookh", "bint_albakkar", "الشيوخ", "بنت البكار", "السيفين", "العملية" })
                Assert.That(csv, Does.Not.Contain(gone), gone);
        }

        [Test]
        public void OnlyTwoLooksExistRoyalAndTalathum()
        {
            Assert.That(ShemaghStyles.Royal, Is.EqualTo("royal")); Assert.That(ShemaghStyles.FaceWrap, Is.EqualTo("talathum"));
            var d = new ShemaghDirector();
            Assert.That(d.Current, Is.EqualTo("royal"), "royal at all times");
            Assert.That(ShemaghStyles.CoversFace(d.Current), Is.False);
        }

        [Test]
        public void BossFightWearsTheFaceWrapAndAfterTheWinItComesOffWithASmile()
        {
            var d = new ShemaghDirector();
            d.BeginBoss(); Assert.That(d.Current, Is.EqualTo("talathum")); Assert.That(ShemaghStyles.CoversFace(d.Current), Is.True);
            d.EndBoss(won: true);
            Assert.That(d.Current, Is.EqualTo("royal"));
            Assert.That(d.SmileAfterWin, Is.True); Assert.That(d.SmileAfterWin, Is.False, "the smile is one moment");
        }

        [Test]
        public void ALossOrAQuitRemovesTheWrapWithoutASmile()
        {
            var d = new ShemaghDirector();
            d.BeginBoss(); d.EndBoss(won: false);
            Assert.That(d.Current, Is.EqualTo("royal")); Assert.That(d.SmileAfterWin, Is.False);
            d.BeginBoss(); d.ClearOverrides();
            Assert.That(d.Current, Is.EqualTo("royal")); Assert.That(d.SmileAfterWin, Is.False);
            d.EndBoss(won: true);
            Assert.That(d.SmileAfterWin, Is.False, "ending a fight that never began gives no smile");
        }

        private static float TimeUntilAdjust(ShemaghDirector d, float max = 60f)
        {
            float t = 0f;
            while (t < max) { t += 0.1f; if (d.Tick(0.1f, false) == IdleAction.Adjust) return t; }
            return -1f;
        }

        [Test]
        public void IdleOnlyAdjustsTheNormalShemaghAndAgalAndNeverChangesTheLook()
        {
            var d = new ShemaghDirector { IdleSeconds = 5f, RepeatEvery = 8f };
            Assert.That(TimeUntilAdjust(d), Is.InRange(4.9f, 5.3f), "after a few seconds");
            Assert.That(d.Current, Is.EqualTo("royal"));
            Assert.That(TimeUntilAdjust(d), Is.InRange(7.9f, 8.3f), "and now and then again");
            Assert.That(d.Current, Is.EqualTo("royal"), "no cycling through looks");
            d.Tick(0.1f, true);
            Assert.That(TimeUntilAdjust(d), Is.InRange(4.9f, 5.3f), "moving starts the wait again");
            Assert.That(Enum.GetNames(typeof(IdleAction)), Is.EquivalentTo(new[] { "None", "Adjust" }));
        }

        [Test]
        public void IdleDoesNothingDuringABossFight()
        {
            var d = new ShemaghDirector(); d.BeginBoss();
            Assert.That(TimeUntilAdjust(d, 120f), Is.EqualTo(-1f));
            Assert.That(d.Current, Is.EqualTo("talathum"));
        }
    }
}
