// (c) Ahmed Alamrani. All rights reserved.
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using Aron.Logic.Animals;
using Aron.Logic.Costumes;
using Aron.Logic.Culture;
using Aron.Logic.Localization;
using Aron.Logic.Passport;
using Aron.Logic.Races;
using Aron.Logic.Regions;
using Aron.Logic.Sky;
using NUnit.Framework;

namespace Aron.Logic.Tests
{
    /// <summary>Country number 35 and the wildlife of all countries.</summary>
    public class AustraliaWildlifeTests
    {
        private static readonly JsonSerializerOptions Opt = new JsonSerializerOptions { IncludeFields = true };

        private static string Resources()
        {
            for (var d = new DirectoryInfo(TestContext.CurrentContext.TestDirectory); d != null; d = d.Parent)
            {
                var p = Path.Combine(d.FullName, "Assets", "_Aron", "Resources");
                if (Directory.Exists(Path.Combine(p, "Regions"))) return p;
            }
            Assert.Fail("Resources not found");
            return null;
        }

        private static T Read<T>(params string[] parts) => JsonSerializer.Deserialize<T>(File.ReadAllText(Path.Combine(new[] { Resources() }.Concat(parts).ToArray())), Opt);

        private static Dictionary<string, RegionDef> Regions() => Directory.GetFiles(Path.Combine(Resources(), "Regions"), "*.json")
            .Where(f => Path.GetFileName(f) != "RegionCatalog.json")
            .Select(f => JsonSerializer.Deserialize<RegionDef>(File.ReadAllText(f), Opt)).ToDictionary(r => r.id);

        private static LocalAnimalFile Animals() => Read<LocalAnimalFile>("Companions", "local_animals.json");

        private static RegionDef Au() => Regions()["australia"];

        // ---- Australia ----

        [Test]
        public void AustraliaIsOneRegionWithEightStagesOneRaceAndTheBoss()
        {
            var r = Au();
            Assert.That(r.country, Is.EqualTo("australia")); Assert.That(r.group, Is.EqualTo("world"));
            Assert.That(r.stages.Length, Is.InRange(7, 9));
            Assert.That(r.stages.Count(s => s.type == "race"), Is.EqualTo(1));
            Assert.That(r.stages.Count(s => s.boss), Is.EqualTo(1));
            Assert.That(r.stages.Last().boss, Is.True);
            Assert.That(Regions().Values.Count(x => x.country == "australia"), Is.EqualTo(1), "one region");
            Assert.That(r.boss.nameEn, Is.EqualTo("Dr. Duhaim")); Assert.That(r.boss.nameAr, Is.EqualTo("د. دحيم"));
            var names = string.Join(" ", r.stages.Select(s => s.nameEn));
            foreach (var want in new[] { "Outback", "Eucalyptus", "Coral Reef", "Harbour", "Rainforest" }) Assert.That(names, Does.Contain(want));
        }

        [Test]
        public void TheRaceIsASurfRaceOnTheWater()
        {
            var t = Read<RaceFile>("Races", "tracks.json").tracks.Single(x => x.region == "australia");
            Assert.That(t.kind, Is.EqualTo("surf")); Assert.That(t.surface, Is.EqualTo("water"));
            Assert.That(t.id, Is.EqualTo("australia_surf"));
            Assert.That(Au().stages.Single(s => s.type == "race").race, Is.EqualTo(t.id));
            Assert.That(t.landmarks, Does.Not.Contain("red_rock_ridges"));
        }

        [Test]
        public void TheRedRockRidgesAreAGenericFarSkylineAndTheSacredRockIsGone()
        {
            var r = Au();
            Assert.That(r.skyline, Is.EqualTo(new[] { "red_rock_ridges" }));
            Assert.That(LandmarkKinds.All, Does.Contain("red_rock_ridges"));
            Assert.That(LandmarkKinds.All, Does.Not.Contain("red_monolith"));
            Assert.That(LandmarkKinds.Forbidden, Does.Contain("uluru")); Assert.That(LandmarkKinds.Forbidden, Does.Contain("red_monolith"));
            foreach (var s in r.stages)
            {
                var text = (s.nameAr + " " + s.nameEn + " " + s.lineAr).ToLowerInvariant();
                foreach (var w in new[] { "uluru", "ayers", "monolith", "أولورو", "أيرز", "أنانغو", "anangu", "climb", "تسلق" }) Assert.That(text, Does.Not.Contain(w), s.id);
            }
            Assert.That(RegionCatalog.FromRegions(Regions().Values).Validate().Where(e => e.StartsWith("region australia")), Is.Empty);
            // The ridges are a layered range, not one dome: the factory builds several slabs in several ranges.
            var src = File.ReadAllText(Path.Combine(Resources(), "..", "Scripts", "Stages", "LandmarkFactory.cs"));
            Assert.That(src, Does.Contain("RedRockRidges")); Assert.That(src, Does.Not.Contain("RedMonolith"));
            // Nowhere in any culture or animal text either.
            var all = File.ReadAllText(Path.Combine(Resources(), "Culture", "australia.json")).ToLowerInvariant();
            Assert.That(all, Does.Not.Contain("uluru"));
        }

        [Test]
        public void LandmarksAreUnnamedShapesNotCopiesOfBuildings()
        {
            var r = Au();
            var kinds = r.stages.SelectMany(s => s.landmarks).Concat(r.skyline).Distinct().ToList();
            Assert.That(kinds, Does.Contain("shell_hall")); Assert.That(kinds, Does.Contain("steel_arch"));
            foreach (var k in kinds) Assert.That(LandmarkKinds.All, Does.Contain(k), k);
            foreach (var s in r.stages) foreach (var n in new[] { s.nameEn, s.nameAr }) Assert.That(RespectRules.NamesABrand(n), Is.False, n);
            Assert.That(string.Join(" ", r.stages.Select(s => s.nameEn)).ToLowerInvariant(), Does.Not.Contain("opera").And.Not.Contain("sydney"));
        }

        [Test]
        public void AronWearsAnOutbackHatOverHisShemaghAndNouraIsFullyModest()
        {
            var f = Read<CostumeFile>("Costumes", "australia.json");
            Assert.That(CostumeFileCheck(f), Is.Empty);
            var aron = f.costumes.Single(c => c.hero == "aron");
            var kinds = aron.accessories.Select(a => a.kind).ToList();
            Assert.That(kinds, Does.Contain("shemagh")); Assert.That(kinds, Does.Contain("outback_hat"));
            Assert.That(kinds.IndexOf("outback_hat"), Is.GreaterThan(kinds.IndexOf("shemagh")), "hat is over the shemagh");
            var noura = f.costumes.Single(c => c.hero == "noura");
            var nk = noura.accessories.Select(a => a.kind).ToList();
            Assert.That(nk, Does.Contain("abaya")); Assert.That(nk, Does.Contain("hijab"));
            Assert.That(noura.meshes.Any(m => m.mesh == "abaya_long"), Is.True);
            Assert.That(nk.Intersect(CostumeKit.Trousers), Is.Empty);
        }

        private static List<string> CostumeFileCheck(CostumeFile f) => CostumeRules.Validate(f);

        [Test]
        public void FiveHalalFoodsWithNoPorkOrAlcohol()
        {
            var c = Read<CultureFile>("Culture", "australia.json");
            var ids = c.foods.Select(x => x.id).ToList();
            Assert.That(ids, Is.EquivalentTo(new[] { "australia_lamington", "australia_pavlova", "australia_anzac_biscuit", "australia_halal_meat_pie", "australia_grilled_barramundi" }));
            var bad = new[] { "pork", "bacon", "ham ", "lard", "beer", "wine", "alcohol", "liquor", "rum", "whisky", "gelatin", "خنزير", "لحم الخنزير", "خمر", "كحول", "نبيذ" };
            var text = (File.ReadAllText(Path.Combine(Resources(), "Culture", "australia.json"))).ToLowerInvariant();
            foreach (var w in bad) Assert.That(System.Text.RegularExpressions.Regex.IsMatch(text, @"(?<![\p{L}])" + System.Text.RegularExpressions.Regex.Escape(w.Trim()) + @"(?![\p{L}])"), Is.False, w);
            Assert.That(c.foods.Single(x => x.id == "australia_halal_meat_pie").local, Does.Contain("Halal"));
        }

        [Test]
        public void AustralianAnimalsAreFriendlyExceptTheCrocodile()
        {
            var a = Animals();
            var k = a.Find("australia_kangaroo");
            Assert.That(k.role, Is.EqualTo("ground")); Assert.That(k.behavior, Is.EqualTo("bounce_pad"));
            Assert.That(Wildlife.CanRide(k), Is.False, "a kangaroo is never ridden");
            Assert.That(Wildlife.HeroHits(k).AnimalHurt, Is.False);
            Assert.That(Wildlife.HeroTouches(k).Effect, Is.EqualTo(HazardEffect.None));
            foreach (var id in new[] { "australia_koala", "australia_emu" }) Assert.That(a.Find(id).role, Is.EqualTo("ground"), id);
            var cr = a.Find("australia_saltwater_crocodile");
            Assert.That(cr.role, Is.EqualTo("hazard")); Assert.That(cr.behavior, Is.EqualTo("water_edge"));
            Assert.That(a.Of("australia").Select(x => x.Species), Is.EquivalentTo(new[] { "kangaroo", "koala", "emu", "saltwater_crocodile" }));
        }

        [Test]
        public void AustralianWeatherFollowsTheSouthernSeasons()
        {
            var t = Read<WeatherTable>("Weather", "weather.json");
            var c = t.Country("australia");
            Assert.That(c.hemisphere, Is.EqualTo("south")); Assert.That(c.water, Is.EqualTo("coral_sea"));
            Assert.That(t.Water("coral_sea").kind, Is.EqualTo("sea"));
            Assert.That(WeatherTable.SeasonOf(7, c.hemisphere), Is.EqualTo(Season.Winter), "July is winter");
            Assert.That(WeatherTable.SeasonOf(1, c.hemisphere), Is.EqualTo(Season.Summer), "January is summer");
            // Wet season of the tropical north, November to April: more rain and thunder than the dry months.
            float Wet(int m) { var w = t.Weights("australia", "australia", m); return w.rain + w.thunder; }
            foreach (var m in new[] { 11, 12, 1, 2, 3, 4 }) Assert.That(Wet(m), Is.GreaterThan(Wet(7) + 10f), "month " + m);
            // The dry interior: dust in the southern spring.
            Assert.That(t.Weights("australia", "australia", 9).sandstorm, Is.GreaterThan(0f));
            // Snow only in the Alps and no stage is alpine, so it never snows in the game.
            for (int m = 1; m <= 12; m++) Assert.That(t.Weights("australia", "australia", m).snow, Is.EqualTo(0f), "month " + m);
            Assert.That(WeatherTable.SnowCountries, Does.Not.Contain("australia"));
            Assert.That(t.Validate(Regions().ToDictionary(r => r.Key, r => (r.Value.country, r.Value.stages.Any(s => s.terrain == "desert" || s.terrain == "oasis")))), Is.Empty);
        }

        [Test]
        public void AustraliaIsInThePassportTheMapAndTheStringTables()
        {
            Assert.That(PassportBook.LocalNames["australia"], Is.EqualTo("Australia"));
            Assert.That(HomeCountry.ByIso["AU"], Is.EqualTo("australia"));
            foreach (var l in UiLanguages.Codes)
            {
                Assert.That(UiStringTables.All[l].ContainsKey("country_australia"), Is.True, l);
                Assert.That(UiStringTables.All[l]["country_australia"], Is.Not.Empty, l);
            }
            Assert.That(UiStringTables.All["ar"]["country_australia"], Is.EqualTo("أستراليا"));
            var w = Read<WeatherTable>("Weather", "weather.json");
            w.Place("australia", "australia", out float lat, out float lon);
            Assert.That(lat, Is.LessThan(0f)); Assert.That(lon, Is.GreaterThan(110f), "an island continent in the south east, the map pin");
        }

        // ---- Wildlife ----

        private static readonly (string id, string role)[] Required =
        {
            ("india_indian_cobra", "hazard"), ("egypt_egyptian_cobra", "hazard"), ("brazil_green_anaconda", "hazard"), ("saudi_sand_viper", "hazard"), ("uae_sand_viper", "hazard"),
            ("usa_brown_bear", "hazard"), ("turkey_brown_bear", "hazard"), ("japan_black_bear", "hazard"),
            ("south_africa_lion", "hazard"), ("south_africa_zebra", "ground"), ("south_africa_rhino", "hazard"),
            ("brazil_sloth", "ground"), ("india_bengal_tiger", "hazard"), ("india_langur", "ground"),
            ("saudi_dhub", "ground"), ("saudi_arabian_wolf", "ground"),
        };

        [Test]
        public void TheRequestedAnimalsExistInTheRightCountriesWithTheRightRoles()
        {
            var a = Animals();
            Assert.That(a.Validate(), Is.Empty);
            foreach (var (id, role) in Required) { Assert.That(a.Find(id), Is.Not.Null, id); Assert.That(a.Find(id).role, Is.EqualTo(role), id); }
            Assert.That(a.animals.Length, Is.EqualTo(115));
        }

        [Test]
        public void DangerousAnimalsAreHazardsAndPeacefulOnesAreNot()
        {
            foreach (var x in Animals().animals)
            {
                if (Wildlife.Dangerous.Contains(x.Species)) Assert.That(x.role, Is.EqualTo("hazard"), x.id);
                if (Wildlife.Peaceful.Contains(x.Species)) { Assert.That(x.role, Is.Not.EqualTo("hazard"), x.id); Assert.That(x.effect, Is.Empty, x.id); }
                if (x.role == "hazard") { Assert.That(Wildlife.HazardBehaviors, Does.Contain(x.behavior), x.id); Assert.That(Wildlife.Effects, Does.Contain(x.effect), x.id); }
            }
            foreach (var id in new[] { "brazil_sloth", "south_africa_zebra", "india_langur" }) Assert.That(Animals().Find(id).behavior, Is.Not.Empty.And.Not.AnyOf(Wildlife.HazardBehaviors), id);
        }

        [Test]
        public void HazardsOnlyLiveInTheirRealCountries()
        {
            var homes = new Dictionary<string, string[]>(Wildlife.HazardHomes);
            Assert.That(homes["sand_viper"], Is.EquivalentTo(new[] { "saudi", "uae" }));
            Assert.That(homes["brown_bear"], Is.EquivalentTo(new[] { "usa", "turkey" }));
            Assert.That(homes["black_bear"], Is.EqualTo(new[] { "japan" }));
            foreach (var x in Animals().animals.Where(x => x.IsHazard))
            {
                Assert.That(homes.ContainsKey(x.Species), Is.True, x.id + " is a known hazard species");
                Assert.That(homes[x.Species], Does.Contain(x.country), x.id);
            }
            // And the other way: every country that lists a hazard species in its culture file really has it.
            foreach (var kv in Wildlife.HazardHomes)
                foreach (var country in Regions().Values.Select(r => r.country).Distinct().Except(kv.Value))
                    Assert.That(Animals().Find(country + "_" + kv.Key), Is.Null, country + " " + kv.Key);
        }

        [Test]
        public void NothingCanAttackOrHurtAnAnimal()
        {
            foreach (var x in Animals().animals)
            {
                var hit = Wildlife.HeroHits(x);
                Assert.That(hit.AnimalHurt, Is.False, x.id); Assert.That(hit.Effect, Is.EqualTo(HazardEffect.None), x.id);
                var touch = Wildlife.HeroTouches(x);
                Assert.That(touch.AnimalHurt, Is.False, x.id);
                Assert.That(touch.Effect != HazardEffect.None, Is.EqualTo(x.IsHazard), x.id + " only hazards act, and only on the hero");
                if (x.IsHazard) Assert.That(touch.Telegraph, Is.GreaterThan(0.3f), x.id + " warns before it moves");
                if (x.role != "mount") Assert.That(Wildlife.CanRide(x), Is.False, x.id);
            }
            var words = new[] { "attack", "hurt", "kill", "shoot", "hunt", "bleed", "gore", "damage", "harm" };
            foreach (var b in Wildlife.HazardBehaviors.Concat(Wildlife.FriendlyBehaviors).Concat(Wildlife.Effects))
                foreach (var w in words) Assert.That(b, Does.Not.Contain(w), b);
            foreach (var f in typeof(LocalAnimal).GetFields().Select(f => f.Name.ToLowerInvariant()))
                foreach (var w in words) Assert.That(f, Does.Not.Contain(w), "LocalAnimal field " + f);
            foreach (var x in Animals().animals) foreach (var w in words) Assert.That((x.behavior + x.effect).ToLowerInvariant(), Does.Not.Contain(w), x.id);
        }

        [Test]
        public void StageHooksPointAtRealStagesAndRegionsOfTheSameCountry()
        {
            var regs = Regions();
            var stages = regs.Values.SelectMany(r => r.stages.Select(s => (r, s))).ToDictionary(x => x.s.id, x => x);
            foreach (var x in Animals().animals)
            {
                foreach (var sid in x.stages) { Assert.That(stages.ContainsKey(sid), Is.True, x.id + " " + sid); Assert.That(stages[sid].r.country, Is.EqualTo(x.country), x.id + " " + sid); }
                foreach (var rid in x.regions) { Assert.That(regs.ContainsKey(rid), Is.True, x.id + " " + rid); Assert.That(regs[rid].country, Is.EqualTo(x.country), x.id + " " + rid); }
            }
            var a = Animals();
            // Saudi desert regions, the UAE desert, the Black Sea mountains of north-eastern Turkey.
            foreach (var rid in a.Find("saudi_sand_viper").regions) Assert.That(regs[rid].stages.Any(s => s.terrain == "desert"), Is.True, rid);
            Assert.That(stages[a.Find("uae_sand_viper").stages.Single()].s.terrain, Is.EqualTo("desert"));
            Assert.That(stages[a.Find("turkey_brown_bear").stages.Single()].s.terrain, Is.EqualTo("forest"));
            Assert.That(stages[a.Find("turkey_brown_bear").stages.Single()].s.nameEn, Does.Contain("Black Sea"));
            Assert.That(a.Find("south_africa_lion").stages, Is.EqualTo(new[] { "south_africa_3" }));
            foreach (var x in a.animals.Where(x => x.IsHazard && x.stages.Length + x.regions.Length > 0 && x.country == "australia")) Assert.That(x.stages, Is.Not.Empty);
        }

        [Test]
        public void CultureFilesListTheNewAnimalsAndEveryEntryExists()
        {
            var a = Animals();
            foreach (var f in Directory.GetFiles(Path.Combine(Resources(), "Culture"), "*.json"))
            {
                var c = JsonSerializer.Deserialize<CultureFile>(File.ReadAllText(f), Opt);
                foreach (var id in c.animals) Assert.That(a.Find(id), Is.Not.Null, c.country + " " + id);
                foreach (var x in a.Of(c.country)) Assert.That(c.animals, Does.Contain(x.id), c.country + " lists " + x.id);
            }
        }

        [Test]
        public void EveryNewAnimalHasANameInTwelveLanguagesAndTheWarningHasItsPlaceholder()
        {
            var species = Animals().animals.Select(x => x.Species).Distinct().Where(s => UiStringTables.All["en"].ContainsKey("animal_" + s) || Required.Any(r => r.id.EndsWith("_" + s)) || s.StartsWith("kangaroo") || s == "koala" || s == "emu" || s == "saltwater_crocodile").ToList();
            Assert.That(species.Count, Is.GreaterThanOrEqualTo(17));
            foreach (var s in species)
                foreach (var l in UiLanguages.Codes)
                {
                    Assert.That(UiStringTables.All[l].ContainsKey("animal_" + s), Is.True, l + " animal_" + s);
                    Assert.That(UiStringTables.All[l]["animal_" + s], Is.Not.Empty, l + " animal_" + s);
                }
            foreach (var l in UiLanguages.Codes) Assert.That(UiStringTables.All[l]["animal_hazard_warn"], Does.Contain("{0}"), l);
            Assert.That(UiStringTables.All["ar"]["animal_dhub"], Is.EqualTo("ضب"));
        }
    }
}
