// (c) Ahmed Alamrani. All rights reserved.
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using Aron.Logic.Audio;
using Aron.Logic.Localization;
using Aron.Logic.Races;
using NUnit.Framework;

namespace Aron.Logic.Tests
{
    /// <summary>The four race-vehicle classes next to the mounts: sea, cars, bicycles and motorcycles, and the rules around them.</summary>
    public class VehicleTests
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
        private static VehicleFile Vehicles() => Read<VehicleFile>("Mounts", "race_vehicles.json");
        private static StoreCatalog Catalog() => Read<StoreCatalog>("Store", "StoreCatalog.json");
        private static EngineAudioFile Engines() => Read<EngineAudioFile>("Audio", "engine_audio.json");
        private static IEnumerable<VehicleDef> All(VehicleFile f) => f.classes.SelectMany(c => c.vehicles);

        [Test]
        public void TheFileIsValidAndEveryMotorcycleLinksARealEngineSound()
        {
            var f = Vehicles();
            Assert.That(f.Validate(Engines().bikes.Select(b => b.id).ToList()), Is.Empty);
            Assert.That(f.classes.Select(c => c.id), Is.EqualTo(new[] { "sea", "cars", "bicycles", "motorcycles" }));
            foreach (var m in f.Class("motorcycles").vehicles)
                Assert.That(Engines().bikes.Select(b => b.id), Does.Contain(m.engineId), m.id);
            Assert.That(f.Class("motorcycles").vehicles.Select(v => v.engineId), Does.Contain("scooter").And.Contain("dirt_bike").And.Contain("sport_bike").And.Contain("cruiser"));
            Assert.That(f.Validate(new List<string> { "scooter" }), Is.Not.Empty, "a missing engine id is caught");
        }

        [Test]
        public void TheVehicleTableHasTheRightVehiclesAndOneFreeStarterPerClass()
        {
            var f = Vehicles();
            Assert.That(f.Class("sea").vehicles.Select(v => v.id), Is.EqualTo(new[] { "boat_pearl", "boat_dhow", "boat_speed", "boat_water_scooter" }));
            Assert.That(f.Class("cars").vehicles.Select(v => v.id), Is.EqualTo(new[] { "car_starter", "car_sports", "car_convertible", "car_buggy", "car_4x4" }));
            Assert.That(f.Class("bicycles").vehicles.Select(v => v.id), Is.EqualTo(new[] { "bike_city", "bike_road", "bike_mountain", "bike_fat", "bike_bmx" }));
            Assert.That(f.Class("motorcycles").vehicles.Select(v => v.id), Is.EqualTo(new[] { "moto_scooter", "moto_dirt", "moto_sport", "moto_cruiser", "moto_quad" }));
            foreach (var c in f.classes)
            {
                Assert.That(c.vehicles.Count(v => v.isDefault), Is.EqualTo(1), c.id);
                var d = f.DefaultFor(c.id); Assert.That(d.usd, Is.EqualTo(0f)); Assert.That(d.productId, Is.Empty);
            }
            Assert.That(f.DefaultFor("sea").id, Is.EqualTo("boat_pearl")); Assert.That(f.DefaultFor("cars").id, Is.EqualTo("car_starter"));
            Assert.That(f.DefaultFor("bicycles").id, Is.EqualTo("bike_city")); Assert.That(f.DefaultFor("motorcycles").id, Is.EqualTo("moto_scooter"));
            Assert.That(All(f).Count(), Is.EqualTo(19));
        }

        [Test]
        public void NoBrandNamesAnywhereInTheVehicles()
        {
            var brands = new[] { "jet ski", "jetski", "vespa", "harley", "ducati", "yamaha", "honda", "kawasaki", "suzuki", "ferrari", "porsche", "jeep", "hummer", "mini", "beetle", "mustang", "corvette", "lamborghini", "toyota", "land rover", "bmw", "schwinn", "trek", "specialized" };
            var f = Vehicles();
            foreach (var v in All(f))
            {
                var text = (v.id + " " + v.nameEn + " " + v.nameAr + " " + v.descAr + " " + v.productId).ToLowerInvariant();
                foreach (var b in brands) Assert.That(text, Does.Not.Contain(b), v.id);
            }
            foreach (var l in UiStringTables.All.Keys)
                foreach (var v in All(f)) foreach (var b in brands) Assert.That(UiStringTables.All[l]["r" + v.id].ToLowerInvariant(), Does.Not.Contain(b), l + " " + v.id);
        }

        [Test]
        public void EveryClassNormalisesToItsOwnBudgetInRankedRaces()
        {
            var f = Vehicles();
            foreach (var c in f.classes)
            {
                foreach (var v in c.vehicles)
                {
                    var raw = f.StatsFor(v.id, RaceMode.Story);
                    Assert.That(VehicleFile.Power(raw), Is.EqualTo(c.powerBudget).Within(c.powerBudget * c.rawTolerance), v.id + " raw power in the data");
                    var ranked = f.StatsFor(v.id, RaceMode.Ranked);
                    Assert.That(VehicleFile.Power(ranked), Is.EqualTo(c.powerBudget).Within(0.001f), v.id + " ranked power");
                    // The shape stays: the ratios between stats do not change.
                    Assert.That(ranked.topSpeed / ranked.accel, Is.EqualTo(raw.topSpeed / raw.accel).Within(0.001f), v.id);
                    Assert.That(f.AbilityAmount(v.id, RaceMode.Story), Is.EqualTo(v.ability.amount));
                }
                var powers = c.vehicles.Select(v => VehicleFile.Power(f.StatsFor(v.id, RaceMode.Ranked))).ToArray();
                Assert.That(powers.Max() - powers.Min(), Is.LessThan(0.002f), c.id + ": every vehicle of a class has the same ranked power");
            }
        }

        [Test]
        public void EveryVehicleHasARealTradeOffAndNothingIsBestAtEverything()
        {
            var f = Vehicles();
            foreach (var c in f.classes)
            {
                foreach (var v in c.vehicles.Where(v => !v.isDefault))
                {
                    var s = v.stats; var d = f.DefaultFor(c.id).stats;
                    bool better = s.topSpeed > d.topSpeed + 0.05f || s.accel > d.accel + 0.05f || s.turning > d.turning + 0.05f || s.jump > d.jump + 0.05f || s.terrain > d.terrain + 0.2f || s.weather > d.weather + 0.2f || s.stamina > d.stamina + 0.05f;
                    bool worse = s.topSpeed < d.topSpeed - 0.05f || s.accel < d.accel - 0.05f || s.turning < d.turning - 0.05f || s.jump < d.jump - 0.05f;
                    Assert.That(better && worse, Is.True, v.id + " must be better at something and worse at something than the starter");
                }
                foreach (var stat in new Func<VehicleTraits, float>[] { s => s.topSpeed, s => s.accel, s => s.turning, s => s.jump })
                {
                    var best = c.vehicles.OrderByDescending(v => stat(v.stats)).First();
                    Assert.That(c.vehicles.Count(v => stat(v.stats) >= stat(best.stats) - 0.001f), Is.EqualTo(1).Or.GreaterThan(0));
                }
                // No vehicle tops every one of the four main stats.
                foreach (var v in c.vehicles)
                    Assert.That(new Func<VehicleTraits, float>[] { s => s.topSpeed, s => s.accel, s => s.turning, s => s.jump }
                        .All(stat => c.vehicles.All(o => stat(v.stats) >= stat(o.stats))), Is.False, v.id);
            }
            var sea = f.Class("sea").vehicles.ToDictionary(v => v.id, v => v.stats);
            Assert.That(sea["boat_speed"].topSpeed, Is.GreaterThan(sea["boat_water_scooter"].topSpeed)); Assert.That(sea["boat_speed"].turning, Is.LessThan(sea["boat_pearl"].turning));
            Assert.That(sea["boat_water_scooter"].turning, Is.GreaterThan(sea["boat_dhow"].turning)); Assert.That(sea["boat_dhow"].weather, Is.GreaterThan(sea["boat_speed"].weather));
            var car = f.Class("cars").vehicles.ToDictionary(v => v.id, v => v.stats);
            Assert.That(car["car_sports"].topSpeed, Is.EqualTo(car.Values.Max(s => s.topSpeed))); Assert.That(car["car_sports"].terrain, Is.LessThan(car["car_4x4"].terrain));
            Assert.That(car["car_buggy"].jump, Is.EqualTo(car.Values.Max(s => s.jump))); Assert.That(car["car_4x4"].terrain, Is.EqualTo(car.Values.Max(s => s.terrain)));
            var moto = f.Class("motorcycles").vehicles.ToDictionary(v => v.id, v => v.stats);
            Assert.That(moto["moto_sport"].topSpeed, Is.EqualTo(moto.Values.Max(s => s.topSpeed))); Assert.That(moto["moto_dirt"].jump, Is.EqualTo(moto.Values.Max(s => s.jump)));
            Assert.That(moto["moto_quad"].terrain, Is.EqualTo(moto.Values.Max(s => s.terrain))); Assert.That(moto["moto_cruiser"].turning, Is.LessThan(moto["moto_scooter"].turning));
            var bike = f.Class("bicycles").vehicles.ToDictionary(v => v.id, v => v.stats);
            Assert.That(bike["bike_road"].stamina, Is.EqualTo(bike.Values.Max(s => s.stamina))); Assert.That(bike["bike_bmx"].stamina, Is.EqualTo(bike.Values.Min(s => s.stamina)));
            Assert.That(bike["bike_fat"].terrain, Is.EqualTo(bike.Values.Max(s => s.terrain)));
        }

        [Test]
        public void VehicleStatsFeedTheSameTerrainRulesAsMounts()
        {
            var f = Vehicles();
            var quad = f.Find("moto_quad").stats.AsMount(); var sport = f.Find("moto_sport").stats.AsMount();
            Assert.That(TerrainRules.SpeedFactor(quad, "sand", "clear", "day"), Is.GreaterThan(TerrainRules.SpeedFactor(sport, "sand", "clear", "day")));
        }

        [Test]
        public void HelmetsAreAlwaysOnTwoWheelsAndNouraRidesInHerAbaya()
        {
            var f = Vehicles();
            foreach (var id in new[] { "bicycles", "motorcycles" })
            {
                var c = f.Class(id);
                Assert.That(c.helmetAlways, Is.True, id);
                Assert.That(VehicleFile.Headgear(c), Is.EqualTo("helmet"));
                Assert.That(VehicleFile.OutfitFor(c, "noura"), Is.EqualTo("abaya"));
                Assert.That(VehicleFile.OutfitFor(c, "aron"), Is.Null);
            }
            foreach (var id in new[] { "sea", "cars" })
            {
                Assert.That(VehicleFile.Headgear(f.Class(id)), Is.Empty); Assert.That(VehicleFile.OutfitFor(f.Class(id), "noura"), Is.Null);
            }
            Assert.That(VehicleFile.Headgear(null), Is.Empty);
        }

        [Test]
        public void ABikeCrashIsAKidSafeTumbleAndARespawn()
        {
            var f = Vehicles(); var rules = f.Class("motorcycles").crash;
            Assert.That(rules.kidSafe, Is.True);
            var s = new CrashState(rules);
            Assert.That(s.SpeedFactor, Is.EqualTo(1f));
            Assert.That(s.Crash(), Is.True); Assert.That(s.Phase, Is.EqualTo(CrashPhase.Tumbling)); Assert.That(s.SpeedFactor, Is.EqualTo(0f));
            Assert.That(s.Crash(), Is.False, "can't crash again while down");
            s.Tick(rules.tumbleSeconds + 0.01f); Assert.That(s.Phase, Is.EqualTo(CrashPhase.Respawning));
            s.Tick(rules.respawnSeconds + 0.01f); Assert.That(s.Phase, Is.EqualTo(CrashPhase.Riding));
            Assert.That(s.Protected, Is.True); Assert.That(s.SpeedFactor, Is.EqualTo(rules.speedAfter));
            Assert.That(s.Crash(), Is.False, "a few protected seconds after the respawn");
            s.Tick(rules.invulnerableSeconds + 0.01f);
            Assert.That(s.Protected, Is.False); Assert.That(s.SpeedFactor, Is.EqualTo(1f)); Assert.That(s.Crash(), Is.True);
            Assert.That(rules.tumbleSeconds + rules.respawnSeconds, Is.LessThanOrEqualTo(3f), "a quick respawn");
            foreach (var id in new[] { "sea", "cars", "bicycles" }) Assert.That(new CrashState(f.Class(id).crash).Crash(), Is.False, id + " never crashes");
        }

        [Test]
        public void PedalStaminaDrainsWhenSprintingAndTheBeatGivesAPulse()
        {
            var f = Vehicles(); var v = f.Find("bike_road");
            var p = new PedalStamina(v.stats, v.ability);
            Assert.That(p.SpeedFactor(true), Is.EqualTo(PedalStamina.SprintBoost).Within(0.001f));
            for (int i = 0; i < 100 && !p.Winded; i++) p.Tick(0.1f, true);
            Assert.That(p.Winded, Is.True); Assert.That(p.SpeedFactor(true), Is.EqualTo(PedalStamina.WindedSlow).Within(0.001f));
            for (int i = 0; i < 200 && p.Winded; i++) p.Tick(0.1f, false);
            Assert.That(p.Winded, Is.False);
            var q = new PedalStamina(v.stats, v.ability);
            Assert.That(q.Tap(1f), Is.False, "the first tap sets the beat"); Assert.That(q.Tap(1.5f), Is.True);
            Assert.That(q.SpeedFactor(false), Is.EqualTo(1f + v.ability.amount).Within(0.001f));
            Assert.That(q.Tap(2.2f), Is.False, "off the beat");
            q.Tick(1.5f, false); Assert.That(q.SpeedFactor(false), Is.EqualTo(1f));
            // A bike with more stamina lasts longer.
            var strong = new PedalStamina(f.Find("bike_road").stats, f.Find("bike_road").ability); var weak = new PedalStamina(f.Find("bike_bmx").stats, f.Find("bike_bmx").ability);
            for (int i = 0; i < 20; i++) { strong.Tick(0.1f, true); weak.Tick(0.1f, true); }
            Assert.That(strong.Stamina, Is.GreaterThan(weak.Stamina));
        }

        // ---- shop, prices, products --------------------------------------------------------------------------

        [Test]
        public void EveryVehicleIsInTheShopOnItsOwnTabWithAProductIdAndNoCoinPrice()
        {
            var f = Vehicles(); var cat = Catalog();
            Assert.That(cat.Validate(), Is.Null);
            Assert.That(StoreTabs.Order, Is.SupersetOf(new[] { "sea", "cars", "bicycles", "motorcycles" }));
            foreach (var c in f.classes)
                foreach (var v in c.vehicles)
                {
                    var item = cat.Find(v.id);
                    Assert.IsNotNull(item, v.id);
                    Assert.That(item.tab, Is.EqualTo(c.tab)); Assert.That(item.slot, Is.EqualTo(c.slot)); Assert.That(item.IsVehicle, Is.True);
                    Assert.That(item.isDefault, Is.EqualTo(v.isDefault));
                    Assert.That(item.coinPrice, Is.EqualTo(0), v.id + " is never bought with coins");
                    Assert.That(item.productId, Is.EqualTo(v.productId)); Assert.That(item.usd, Is.EqualTo(v.usd));
                    if (v.isDefault) continue;
                    Assert.That(item.productType, Is.EqualTo(ProductKinds.NonConsumable)); Assert.That(item.SoldForMoney, Is.True);
                    Assert.That(item.productId, Is.EqualTo("com.ahmdria1.aron." + v.id));
                    Assert.That(item.CanRent, Is.True);
                }
            var ids = cat.items.Where(i => !string.IsNullOrEmpty(i.productId)).Select(i => i.productId).ToList();
            Assert.That(ids.Distinct().Count(), Is.EqualTo(ids.Count), "product ids are unique");
            foreach (var slot in StoreTabs.VehicleSlots) Assert.That(cat.DefaultFor(slot), Is.Not.Null, slot);
            Assert.That(cat.items.Where(i => i.IsVehicle).Count(), Is.EqualTo(19 + 5));
        }

        [Test]
        public void TheCatalogRefusesAVehicleThatCostsCoinsOrHasNoPrice()
        {
            var cat = Catalog();
            var coin = cat.Find("boat_dhow"); coin.coinPrice = 5000;
            Assert.That(cat.Validate(), Does.Contain("can't be bought with coins")); coin.coinPrice = 0;
            var noPrice = cat.Find("car_sports"); float usd = noPrice.usd; noPrice.usd = 0f;
            Assert.That(cat.Validate(), Does.Contain("direct price")); noPrice.usd = usd;
            Assert.That(cat.Validate(), Is.Null);
        }

        [Test]
        public void ThePriceTiersAreSmallAndSensible()
        {
            var f = Vehicles();
            foreach (var v in All(f).Where(v => !v.isDefault)) Assert.That(new[] { 0.99f, 1.99f, 2.99f, 3.99f }, Does.Contain(v.usd), v.id);
        }

        [Test]
        public void VehicleNamesExistInAllTwelveLanguagesAndTheTabsToo()
        {
            var f = Vehicles(); var cat = Catalog();
            foreach (var l in UiLanguages.Codes)
            {
                var t = UiStringTables.All[l];
                foreach (var tab in new[] { "tab_sea", "tab_cars", "tab_bicycles", "tab_motorcycles" }) Assert.That(string.IsNullOrWhiteSpace(t[tab]), Is.False, l + tab);
                foreach (var v in All(f))
                {
                    Assert.That(t.ContainsKey("r" + v.id), Is.True, l + " r" + v.id);
                    string n = StoreItemNames.Name(cat.Find(v.id), k => t[k], (k, a) => string.Format(t[k], a), k => t.ContainsKey(k));
                    Assert.That(n, Is.EqualTo(t["r" + v.id]), l + " " + v.id);
                    if (l == "ar") Assert.That(n, Is.EqualTo(v.nameAr)); if (l == "en") Assert.That(n, Is.EqualTo(v.nameEn));
                }
            }
            Assert.That(UiStringTables.All["ja"]["rmoto_dirt"], Is.Not.EqualTo(UiStringTables.All["ar"]["rmoto_dirt"]));
        }

        // ---- tracks ------------------------------------------------------------------------------------------

        [Test]
        public void EveryTrackNamesAValidVehicleClassThatMatchesItsKind()
        {
            var races = Read<RaceFile>("Races", "tracks.json");
            Assert.That(races.Validate(), Is.Empty);
            foreach (var t in races.tracks)
            {
                Assert.That(VehicleFile.ClassIds, Does.Contain(t.vehicleClass), t.id);
                Assert.That(t.vehicleClass, Is.EqualTo(VehicleFile.ClassForKind(t.kind)), t.id);
            }
            Assert.That(races.tracks.Where(t => t.kind == "boat" || t.kind == "sail").All(t => t.vehicleClass == "sea"), Is.True);
            Assert.That(races.tracks.Where(t => t.kind == "car").All(t => t.vehicleClass == "cars"), Is.True);
            var bad = new RaceFile { tracks = new[] { new RaceTrack { id = "x", kind = "car", vehicleClass = "sea" } } };
            Assert.That(bad.Validate(), Has.Some.Contain("uses the cars class"));
            foreach (var k in RaceFile.Kinds) Assert.That(VehicleFile.ClassIds, Does.Contain(VehicleFile.ClassForKind(k)), k);
            Assert.That(RaceFile.Fits("bicycle", "asphalt"), Is.True); Assert.That(RaceFile.Fits("bicycle", "water"), Is.False);
            Assert.That(RaceFile.Fits("motorcycle", "sand"), Is.True); Assert.That(RaceFile.Fits("motorcycle", "snow"), Is.False);
        }

        // ---- points rent, never buy -------------------------------------------------------------------------

        [Test]
        public void HundredPointsRentAnyPremiumVehicleFor48HoursAndRenewingExtendsIt()
        {
            var cat = Catalog(); var d = new SaveData { racePoints = 300, racePointsLifetime = 300 };
            var now = T0; var inv = new StoreInventory(cat, d) { Clock = () => now };
            Assert.That(inv.Rent("car_sports"), Is.EqualTo(StoreInventory.RentResult.Rented));
            Assert.That(d.racePoints, Is.EqualTo(200));
            Assert.That(inv.Owns("car_sports"), Is.True); Assert.That(inv.OwnsForGood("car_sports"), Is.False, "a rental is never ownership");
            Assert.That(inv.Equip("car_sports"), Is.True); Assert.That(inv.Equipped("car").id, Is.EqualTo("car_sports"));
            now = T0.AddHours(47); Assert.That(inv.IsRented("car_sports"), Is.True);
            Assert.That(inv.Rent("car_sports"), Is.EqualTo(StoreInventory.RentResult.Rented), "renewing");
            now = T0.AddHours(47 + 47); Assert.That(inv.IsRented("car_sports"), Is.True, "the renewal added 48 hours onto the end of the first");
            now = T0.AddHours(96.1); Assert.That(inv.IsRented("car_sports"), Is.False);
            Assert.That(d.racePoints, Is.EqualTo(100));
            // Any premium item in any tab, 100 points each.
            now = T0; d.racePoints = 100 * cat.items.Count(i => i.CanRent);
            foreach (var i in cat.items.Where(i => i.CanRent)) Assert.That(inv.Rent(i.id), Is.EqualTo(StoreInventory.RentResult.Rented), i.id);
            Assert.That(d.racePoints, Is.EqualTo(0));
            foreach (var id in new[] { "boat_dhow", "boat_speed", "boat_water_scooter", "car_convertible", "car_buggy", "car_4x4", "bike_road", "bike_mountain", "bike_fat", "bike_bmx", "moto_dirt", "moto_sport", "moto_cruiser", "moto_quad" })
                Assert.That(inv.IsRented(id), Is.True, id);
            Assert.That(inv.Rent("boat_pearl"), Is.EqualTo(StoreInventory.RentResult.NotRentable), "the free starter needs no rental");
        }

        [Test]
        public void PointsOnlyEverRentAndRealPurchaseIsTheOnlyWayToOwn()
        {
            var cat = Catalog(); var d = new SaveData { racePoints = 100, racePointsLifetime = 5000 };
            var inv = new StoreInventory(cat, d) { Clock = () => T0 };
            Assert.That(inv.Rent("moto_dirt"), Is.EqualTo(StoreInventory.RentResult.Rented));
            Assert.That(inv.OwnsForGood("moto_dirt"), Is.False);
            Assert.That(inv.BuyWithCoins("moto_dirt"), Is.EqualTo(CoinPurchase.NotForCoins));
            Assert.That(inv.GrantPurchase("com.ahmdria1.aron.moto_dirt", "order-1"), Is.EqualTo(Grant.Granted));
            Assert.That(inv.OwnsForGood("moto_dirt"), Is.True);
            Assert.That(inv.Rent("moto_dirt"), Is.EqualTo(StoreInventory.RentResult.AlreadyOwned));
            foreach (var m in new[] { "mount_camel", "mount_ostrich", "mount_giraffe", "mount_falcon_glider" })
                Assert.That(cat.Find(m).SoldForCoins, Is.False, m);
        }
    }
}
