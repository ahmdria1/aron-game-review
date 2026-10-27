// (c) Ahmed Alamrani. All rights reserved.
using System;
using System.IO;
using System.Linq;
using Aron.Logic.Animals;
using Aron.Logic.Animation;
using Aron.Logic.CameraRig;
using Aron.Logic.Combat;
using Aron.Logic.Heroes;
using Aron.Logic.Localization;
using Aron.Logic.Races;
using NUnit.Framework;

namespace Aron.Logic.Tests
{
    /// <summary>The cartoon camel race, the camel breeds and age classes, the companions' names and the wild oryx.</summary>
    public class CamelRaceTests
    {
        private static readonly DateTime T0 = new DateTime(2026, 10, 27, 12, 0, 0, DateTimeKind.Utc);
        private static CamelRaceFile Camel() => TestData.Read<CamelRaceFile>("Races", "camel_race.json");
        private static StoreCatalog Catalog() => TestData.Read<StoreCatalog>("Store", "StoreCatalog.json");
        private static CompanionNameFile Companions() => TestData.Read<CompanionNameFile>("Companions", "companion_names.json");
        private static LocalAnimalFile Animals() => TestData.Read<LocalAnimalFile>("Companions", "local_animals.json");
        private static string Winner(CamelRaceFile f, float length, int bends)
            => f.breeds.OrderBy(b => CamelSim.Seconds(b.stats, length, bends)).First().id;

        // ---- breeds ------------------------------------------------------------------------------------------

        [Test]
        public void TheFileIsValidWithSixBreedsAndFiveClasses()
        {
            var f = Camel();
            Assert.That(f.Validate(), Is.Empty);
            Assert.That(f.breeds.Select(b => b.id), Is.EqualTo(new[] { "sufr", "shuqah", "shaol", "humr", "maghateer", "majaheem" }));
            Assert.That(f.classes.Select(c => c.id), Is.EqualTo(new[] { "hiqaq", "laqaya", "jidhaa", "thanaya", "hayl" }));
            Assert.That(f.breeds.Select(b => b.nameAr), Is.EqualTo(new[] { "الصفر", "الشقح", "الشعل", "الحمر", "المغاتير", "المجاهيم" }));
            Assert.That(f.Find("maghateer").aliasAr, Is.EqualTo("الوضح"));
            Assert.That(f.classes.Select(c => c.nameAr), Is.EqualTo(new[] { "حقايق", "لقايا", "جذاع", "ثنايا", "حيل" }));
        }

        [Test]
        public void NoBreedIsStrictlyBetterThanAnotherAndAllHaveTheSamePower()
        {
            var f = Camel();
            foreach (var a in f.breeds)
            {
                Assert.That(CamelRaceFile.Power(a.stats), Is.EqualTo(f.powerBudget).Within(f.powerBudget * f.powerTolerance), a.id);
                foreach (var b in f.breeds) if (a != b) Assert.That(CamelRaceFile.Dominates(a.stats, b.stats), Is.False, a.id + " over " + b.id);
            }
            foreach (var b in f.breeds) Assert.That(CamelRaceFile.Power(f.StatsFor(b, RaceMode.Ranked)), Is.EqualTo(f.powerBudget).Within(1e-3f), "ranked is exactly the budget");
            Assert.That(CamelRaceFile.Dominates(new CamelStats { topSpeed = 1.1f }, new CamelStats()), Is.True, "the check itself works");
        }

        [Test]
        public void EachBreedHasItsOwnShape()
        {
            var f = Camel(); var s = f.breeds.ToDictionary(b => b.id, b => b.stats);
            Assert.That(f.breeds.OrderByDescending(b => b.stats.topSpeed).First().id, Is.EqualTo("maghateer"), "the white type is the fastest");
            Assert.That(f.breeds.OrderBy(b => b.stats.stamina).First().id, Is.EqualTo("maghateer"), "and tires first");
            Assert.That(f.breeds.OrderByDescending(b => b.stats.stamina).First().id, Is.EqualTo("majaheem"), "the black type holds its pace");
            Assert.That(f.breeds.OrderBy(b => b.stats.accel).First().id, Is.EqualTo("majaheem"), "and starts slowly");
            Assert.That(f.breeds.OrderByDescending(b => b.stats.handling).First().id, Is.EqualTo("shaol"), "the agile one");
            Assert.That(f.breeds.OrderByDescending(b => b.stats.accel).First().id, Is.EqualTo("humr"), "the strong start");
            Assert.That(s["sufr"].topSpeed + s["sufr"].accel + s["sufr"].stamina + s["sufr"].handling, Is.EqualTo(4f), "the free one is the baseline");
            var q = s["shuqah"]; Assert.That(new[] { q.topSpeed, q.accel, q.stamina, q.handling }.All(x => Math.Abs(x - 1f) <= 0.03f), Is.True, "the all-rounder stays near 1 everywhere");
        }

        [Test]
        public void DifferentRacesAreWonByDifferentBreeds()
        {
            var f = Camel();
            Assert.That(Winner(f, 500f, 0), Is.EqualTo("maghateer"), "a sprint");
            Assert.That(Winner(f, 1200f, 12), Is.EqualTo("shaol"), "twisty ground");
            Assert.That(Winner(f, 4000f, 2), Is.EqualTo("majaheem"), "a long race");
            Assert.That(new[] { Winner(f, 500f, 0), Winner(f, 1200f, 12), Winner(f, 4000f, 2), Winner(f, 1500f, 4) }.Distinct().Count(), Is.GreaterThanOrEqualTo(3));
            Func<float, int, string> ranked = (l, n) => f.breeds.OrderBy(b => CamelSim.Seconds(f.StatsFor(b, RaceMode.Ranked), l, n)).First().id;
            Assert.That(ranked(500f, 0), Is.EqualTo("maghateer")); Assert.That(ranked(4000f, 2), Is.EqualTo("majaheem"));
            Assert.That(f.breeds.All(b => CamelSim.Seconds(b.stats, 1000f, 2) > 40f && CamelSim.Seconds(b.stats, 1000f, 2) < 90f), Is.True, "sensible times");
        }

        [Test]
        public void ColoursMatchTheRealTypesAndAreDistinct()
        {
            var f = Camel();
            Assert.That(f.breeds.Select(b => b.coat).Distinct().Count(), Is.EqualTo(6));
            Func<string, float> lum = h => 0.299f * Convert.ToInt32(h.Substring(1, 2), 16) + 0.587f * Convert.ToInt32(h.Substring(3, 2), 16) + 0.114f * Convert.ToInt32(h.Substring(5, 2), 16);
            Assert.That(f.breeds.OrderByDescending(b => lum(b.coat)).First().id, Is.EqualTo("maghateer"), "white");
            Assert.That(f.breeds.OrderBy(b => lum(b.coat)).First().id, Is.EqualTo("majaheem"), "black");
        }

        // ---- age classes -------------------------------------------------------------------------------------

        [Test]
        public void AgeClassesGetHarderAndUnlockOneAfterTheOther()
        {
            var f = Camel();
            Assert.That(f.classes.Length, Is.EqualTo(5));
            for (int i = 1; i < 5; i++)
            {
                var a = f.classes[i - 1]; var b = f.classes[i];
                Assert.That(b.rivalTop, Is.GreaterThan(a.rivalTop)); Assert.That(b.length, Is.GreaterThan(a.length)); Assert.That(b.bends, Is.GreaterThanOrEqualTo(a.bends));
                Assert.That(b.unlockedBy, Is.EqualTo(a.id));
            }
            Assert.That(f.classes[0].unlockedBy, Is.Empty, "the first cup is open");
            Assert.That(f.classes[0].rivalTop, Is.LessThan(1f), "the first cup is easier than the player"); Assert.That(f.classes[4].rivalTop, Is.GreaterThan(1f));
            Assert.That(f.classes.All(c => c.rivalBreeds.All(r => f.Find(r) != null)), Is.True);
        }

        [Test]
        public void PointsAreTheSameInEveryClassAndNothingIsAnExtraReward()
        {
            var t = TestData.Read<PointsTable>("Mounts", "race_points.json");
            Assert.That(t.finish, Is.EqualTo(new[] { 5, 3, 1 }));
            foreach (var c in Camel().classes)
                Assert.That(new[] { 1, 2, 3 }.Select(p => RacePoints.Award(t, PointsSource.AiRace, "normal", p, 1).Total), Is.EqualTo(new[] { 5, 3, 1 }), c.id);
            string text = File.ReadAllText(Path.Combine(TestData.Resources(), "Races", "camel_race.json")).ToLowerInvariant();
            foreach (var bad in new[] { "lootbox", "loot_box", "gacha", "mystery", "crate", "rewardedad", "coinprice" }) Assert.That(text, Does.Not.Contain(bad), bad);
        }

        // ---- the store ---------------------------------------------------------------------------------------

        [Test]
        public void PricesAreTheThreeTiersAndTheFreeOneIsTheDefault()
        {
            var f = Camel(); var cat = Catalog();
            Assert.That(cat.Validate(), Is.Null.Or.Empty);
            Assert.That(f.breeds.Where(b => !b.isDefault).Select(b => b.usd).Distinct().OrderBy(x => x), Is.EqualTo(new[] { 1.99f, 2.99f, 3.99f }));
            Assert.That(f.Find("sufr").isDefault && f.Find("sufr").usd == 0f, Is.True);
            Assert.That(f.Find("shuqah").usd, Is.EqualTo(1.99f)); Assert.That(f.Find("shaol").usd, Is.EqualTo(2.99f)); Assert.That(f.Find("humr").usd, Is.EqualTo(2.99f));
            Assert.That(f.Find("maghateer").usd, Is.EqualTo(3.99f)); Assert.That(f.Find("majaheem").usd, Is.EqualTo(3.99f));
            foreach (var b in f.breeds)
            {
                var i = cat.Find(b.ItemId);
                Assert.That(i, Is.Not.Null, b.id);
                Assert.That(i.slot, Is.EqualTo("camel_breed")); Assert.That(i.isDefault, Is.EqualTo(b.isDefault));
                Assert.That(i.IsVehicle, Is.False, "a breed is not one of the race vehicles, the camel mount stays as it was");
                if (b.isDefault) continue;
                Assert.That(i.usd, Is.EqualTo(b.usd)); Assert.That(i.productId, Is.EqualTo("com.ahmdria1.aron." + b.ItemId));
                Assert.That(i.productType, Is.EqualTo("non_consumable")); Assert.That(i.SoldForMoney, Is.True); Assert.That(i.SoldForCoins, Is.False, "no coin price, no loot");
                Assert.That(i.coins, Is.EqualTo(0)); Assert.That(i.grants, Is.Empty);
            }
            Assert.That(cat.DefaultFor("camel_breed").id, Is.EqualTo("breed_sufr"));
            var camel = cat.Find("mount_camel"); Assert.That(camel.usd, Is.EqualTo(1.99f), "the camel mount is unchanged"); Assert.That(camel.productId, Is.EqualTo("com.ahmdria1.aron.mount_camel"));
        }

        [Test]
        public void BreedsAreOwnedOnlyByRealPurchaseButCanBeRentedForOneHundredPointsAndTwoDays()
        {
            var cat = Catalog();
            var d = new SaveData { racePoints = 99999, racePointsLifetime = 999999 };
            for (int i = 0; i < 40; i++) d.raceMedals.Add("t" + i + "=1");
            var inv = new StoreInventory(cat, d) { Clock = () => T0 };
            foreach (var b in Camel().breeds.Where(b => !b.isDefault))
            {
                Assert.That(inv.OwnsForGood(b.ItemId), Is.False, b.id);
                Assert.That(inv.BuyWithCoins(b.ItemId), Is.Not.EqualTo(CoinPurchase.Bought), b.id);
            }
            var d2 = new SaveData { racePoints = 250, racePointsLifetime = 250 };
            var inv2 = new StoreInventory(cat, d2) { Clock = () => T0 };
            Assert.That(inv2.Rent("breed_maghateer"), Is.EqualTo(StoreInventory.RentResult.Rented));
            Assert.That(d2.racePoints, Is.EqualTo(150)); Assert.That(inv2.IsRented("breed_maghateer"), Is.True); Assert.That(inv2.OwnsForGood("breed_maghateer"), Is.False);
            Assert.That(inv2.Equip("breed_maghateer"), Is.True); Assert.That(inv2.Equipped("camel_breed").id, Is.EqualTo("breed_maghateer"));
            var later = new StoreInventory(cat, d2) { Clock = () => T0.AddHours(47) }; Assert.That(later.IsRented("breed_maghateer"), Is.True);
            var over = new StoreInventory(cat, d2) { Clock = () => T0.AddHours(49) }; Assert.That(over.IsRented("breed_maghateer"), Is.False, "48 hours");
            Assert.That(over.Equipped("camel_breed").id, Is.EqualTo("breed_sufr"), "back to the free breed");
        }

        // ---- the arcade layer, the venue, the camera and the music ---------------------------------------------

        [Test]
        public void RivalsAreRobotsTheVenueIsADesertTrackAndTheCameraAndMusicAreNamed()
        {
            var f = Camel();
            Assert.That(f.rivalJockey, Is.EqualTo("robot"));
            string text = File.ReadAllText(Path.Combine(TestData.Resources(), "Races", "camel_race.json")).ToLowerInvariant();
            foreach (var bad in new[] { "child", "kid", "boy", "طفل" }) Assert.That(text, Does.Not.Contain(bad), bad);
            Assert.That(f.venue.id, Is.EqualTo("hijn_maydan")); Assert.That(f.venue.nameAr, Is.EqualTo("ميدان الهجن")); Assert.That(f.venue.surface, Is.EqualTo("sand"));
            Assert.That(f.musicId, Is.EqualTo("music_race_camel"));
            var cam = TestData.Read<CameraFile>("Config", "camera.json");
            Assert.That(cam.Validate(), Is.Empty);
            Assert.That(cam.Mode(f.cameraMode), Is.Not.Null); Assert.That(CameraFile.IsRaceLike(f.cameraMode), Is.True);
            Assert.That(CameraMath.RaceFov(cam.Mode("camel_race"), 1f), Is.GreaterThan(CameraMath.RaceFov(cam.Mode("camel_race"), 0f)));
            Assert.That(CameraFile.IsRaceLike("chase"), Is.False);
            Assert.That(f.Style(f.activeStyle).id, Is.EqualTo("cartoon"));
        }

        [Test]
        public void TheArcadeLayerNeverHurtsAnyone()
        {
            var a = Camel().arcade;
            Assert.That(CamelArcadeRules.BoostedTop(a, 20f), Is.GreaterThan(20f));
            Assert.That(CamelArcadeRules.AfterStumble(a, 20f), Is.InRange(1f, 19.9f), "a stumble slows the camel, it never stops it");
            Assert.That(CamelArcadeRules.SpeedLines(a, 0.3f), Is.EqualTo(0f)); Assert.That(CamelArcadeRules.SpeedLines(a, 1f), Is.EqualTo(1f));
            Assert.That(CamelArcadeRules.BoostPads(a, 1000f), Is.EqualTo(8)); Assert.That(CamelArcadeRules.DuneJumps(a, 1000f), Is.EqualTo(4));
            Assert.That(typeof(CamelArcade).GetFields().Select(x => x.Name), Does.Not.Contain("damage"));
        }

        // ---- the companions' names ----------------------------------------------------------------------------

        [Test]
        public void NouraOryxIsCalledBaydaInEveryLanguageAndTheOthersAreStillPending()
        {
            var f = Companions();
            Assert.That(f.Validate(), Is.Empty);
            var oryx = f.Find("noura_oryx");
            Assert.That(oryx.species, Is.EqualTo("oryx")); Assert.That(oryx.owner, Is.EqualTo("noura")); Assert.That(oryx.namePending, Is.False);
            Assert.That(oryx.NameKey, Is.EqualTo("companion_name_noura_oryx"));
            Assert.That(UiStringTables.All["ar"][oryx.NameKey], Is.EqualTo("بيضا"));
            var expected = new System.Collections.Generic.Dictionary<string, string> { ["en"] = "Bayda", ["tr"] = "Bayda", ["de"] = "Bayda", ["ur"] = "بیضا", ["hi"] = "बैदा", ["es"] = "Bayda", ["pt"] = "Bayda", ["id"] = "Bayda", ["ms"] = "Bayda", ["ja"] = "バイダ", ["fr"] = "Bayda" };
            foreach (var kv in expected) Assert.That(UiStringTables.All[kv.Key][oryx.NameKey], Is.EqualTo(kv.Value), kv.Key);
            foreach (var l in UiLanguages.Codes) Assert.That(UiStringTables.All[l][oryx.NameKey], Is.Not.Empty, l);
            var heroes = TestData.Read<HeroFile>("Heroes", "heroes.json");
            Assert.That(heroes.Get("noura").companion, Is.EqualTo(oryx.species)); Assert.That(heroes.Get("aron").companion, Is.EqualTo(f.Find("aron_falcon").species));
            Func<string, bool> has = k => UiStringTables.All["ar"].ContainsKey(k);
            Assert.That(CompanionNameFile.DisplayKey(oryx, has), Is.EqualTo("companion_name_noura_oryx"));
            foreach (var c in f.companions.Where(c => c.namePending))
            {
                Assert.That(CompanionNameFile.DisplayKey(c, has), Is.EqualTo(c.SpeciesKey), c.id);
                Assert.That(UiStringTables.All["ar"].ContainsKey(c.NameKey), Is.False, "no invented name for " + c.id);
            }
            foreach (var c in f.companions) foreach (var l in UiLanguages.Codes) Assert.That(UiStringTables.All[l][c.SpeciesKey], Is.Not.Empty, l + " " + c.SpeciesKey);
        }

        // ---- the wild oryx ------------------------------------------------------------------------------------

        [Test]
        public void TheOryxIsAPeacefulHerdAnimalOfTheAlUlaAndNorthernStages()
        {
            var file = Animals(); Assert.That(file.Validate(), Is.Empty);
            var o = file.Find("saudi_oryx");
            Assert.That(o.role, Is.EqualTo("ground")); Assert.That(o.IsHazard, Is.False); Assert.That(o.behavior, Is.EqualTo("flee_gently"));
            Assert.That(o.Species, Is.EqualTo("oryx")); Assert.That(Wildlife.Check(o), Is.Empty);
            Assert.That(o.regions, Is.EquivalentTo(new[] { "madinah", "tabuk", "northern" }));
            var cat = TestData.Catalog();
            foreach (var r in o.regions) Assert.That(cat.Region(r), Is.Not.Null, r);
            foreach (var s in o.stages)
            {
                var st = cat.Stage(s, out int ri, out _);
                Assert.That(st, Is.Not.Null, s); Assert.That(o.regions, Does.Contain(cat.regions[ri].id), s + " is in one of its regions");
            }
            Assert.That(o.stages, Does.Contain("madinah_5")); Assert.That(o.stages, Does.Contain("tabuk_3"));
            Assert.That(Array.IndexOf(Wildlife.Peaceful, "oryx") >= 0 && Array.IndexOf(Wildlife.Dangerous, "oryx") < 0, Is.True);
            Assert.That(UiStringTables.All["ar"]["animal_oryx"], Is.EqualTo("مها"));
        }

        [Test]
        public void TheOryxCanNeverBeHurtTargetedRiddenOrUsedAsAHazard()
        {
            var o = Animals().Find("saudi_oryx");
            var hit = Wildlife.HeroHits(o); Assert.That(hit.AnimalHurt, Is.False); Assert.That(hit.Effect, Is.EqualTo(HazardEffect.None));
            var touch = Wildlife.HeroTouches(o); Assert.That(touch.AnimalHurt, Is.False); Assert.That(touch.Effect, Is.EqualTo(HazardEffect.None), "touching it costs the hero nothing");
            Assert.That(Wildlife.IsTargetable(o), Is.False); Assert.That(Wildlife.CanRide(o), Is.False);
            Assert.That(Wildlife.IsHazardBehavior(o.behavior), Is.False);
            Assert.That(AimAssist.Targetable(TargetKind.Animal), Is.False, "the aim assist never picks an animal");
            foreach (var k in Enum.GetValues(typeof(TargetKind)).Cast<TargetKind>())
                if (k != TargetKind.Enemy && k != TargetKind.Boss && k != TargetKind.Breakable) Assert.That(AimAssist.Targetable(k), Is.False, k.ToString());
            var bad = new LocalAnimal { id = "saudi_oryx", role = "hazard", behavior = "charge_lane", effect = "knockback", country = "saudi" };
            Assert.That(Wildlife.Check(bad), Is.Not.Empty, "an oryx entry can never be made a hazard");
        }

        [Test]
        public void TheHerdFleesGentlyAndCalmsDown()
        {
            Assert.That(OryxHerd.Next(OryxState.Graze, 60f), Is.EqualTo(OryxState.Graze));
            Assert.That(OryxHerd.Next(OryxState.Graze, 20f), Is.EqualTo(OryxState.Alert));
            Assert.That(OryxHerd.Next(OryxState.Alert, 10f), Is.EqualTo(OryxState.Flee));
            Assert.That(OryxHerd.Next(OryxState.Flee, 18f), Is.EqualTo(OryxState.Flee), "it keeps going until it feels safe");
            Assert.That(OryxHerd.Next(OryxState.Flee, 30f), Is.EqualTo(OryxState.Alert));
            Assert.That(OryxHerd.Speed(OryxState.Graze), Is.EqualTo(0f)); Assert.That(OryxHerd.Speed(OryxState.Flee), Is.LessThan(5f), "a trot, not a panic");
            Assert.That(OryxHerd.FleeMetres(5f), Is.LessThanOrEqualTo(OryxHerd.MaxFleeMetres)); Assert.That(OryxHerd.FleeMetres(100f), Is.EqualTo(0f));
        }

        // ---- animations and words -----------------------------------------------------------------------------

        [Test]
        public void TheAnimationListHasTheCamelRaceAndOryxClips()
        {
            var file = TestData.Read<AnimClipFile>("Config", "animation_clips.json");
            Assert.That(file.Validate(), Is.Empty);
            string repo = Path.GetFullPath(Path.Combine(TestData.Resources(), "..", "..", ".."));
            string md = File.ReadAllText(Path.Combine(repo, "docs", "animation_list.md"));
            foreach (var id in new[] { "camel_racer_kneel", "camel_racer_rise", "camel_racer_gallop", "camel_racer_gallop_full", "camel_racer_stumble", "camel_racer_boost", "camel_racer_finish_celebrate",
                "robot_jockey_ride_gallop", "rider_camel_race_crouch", "noura_camel_race_sideline_cheer", "oryx_wild_idle_graze", "oryx_wild_walk", "oryx_wild_trot", "oryx_wild_flee", "oryx_wild_alert" })
            {
                Assert.That(file.clips.Any(c => c.id == id), Is.True, id);
                Assert.That(md, Does.Contain(id), id);
            }
            Assert.That(file.clips.Where(c => c.actor == "camel_racer" || c.actor == "robot_jockey" || c.actor == "oryx_wild").All(c => c.priority != "P0"), Is.True, "the new clips are P1 and P2");
            Assert.That(file.clips.Single(c => c.id == "oryx_wild_flee").param, Is.EqualTo("Fleeing"));
        }

        [Test]
        public void EveryNewWordExistsInAllTwelveLanguages()
        {
            var f = Camel();
            var keys = f.breeds.SelectMany(b => new[] { b.NameKey, b.StyleKey }).Concat(f.classes.Select(c => c.NameKey)).Concat(new[] { "camel_race_title", "animal_oryx" }).ToList();
            foreach (var l in UiLanguages.Codes) foreach (var k in keys) { Assert.That(UiStringTables.All[l].ContainsKey(k), Is.True, l + " " + k); Assert.That(UiStringTables.All[l][k], Is.Not.Empty, l + " " + k); }
            Assert.That(UiStringTables.All["ar"]["rbreed_maghateer"], Is.EqualTo("المغاتير"));
            Assert.That(UiStringTables.All["ar"]["camel_class_hiqaq"], Is.EqualTo("حقايق"));
            var cat = Catalog();
            foreach (var l in UiLanguages.Codes.Where(l => l != "ar"))
            {
                var t = UiStringTables.All[l];
                Assert.That(StoreItemNames.Name(cat.Find("breed_humr"), k => t[k], (k, a) => string.Format(t[k], a), k => t.ContainsKey(k)), Is.EqualTo(t["rbreed_humr"]), l);
            }
        }
    }
}
