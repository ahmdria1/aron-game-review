// (c) Ahmed Alamrani. All rights reserved.
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using Aron.Logic.Animals;
using Aron.Logic.Culture;
using Aron.Logic.Races;
using Aron.Logic.Regions;
using NUnit.Framework;

namespace Aron.Logic.Tests
{
    /// <summary>The culture files, local animals and race tracks for all 35 countries.</summary>
    public class CultureRaceFileTests
    {
        private static readonly JsonSerializerOptions Opt = new JsonSerializerOptions { IncludeFields = true };

        private static readonly string[] World =
        {
            "portugal", "spain", "france", "uk", "germany", "turkey", "uzbekistan", "pakistan", "india",
            "malaysia", "indonesia", "japan", "usa", "mexico", "brazil", "south_africa", "australia",
        };

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

        private static List<RegionDef> Regions() => Directory.GetFiles(Path.Combine(Resources(), "Regions"), "*.json")
            .Where(f => Path.GetFileName(f) != "RegionCatalog.json")
            .Select(f => JsonSerializer.Deserialize<RegionDef>(File.ReadAllText(f), Opt)).OrderBy(r => r.order).ToList();

        private static List<string> Countries() => Regions().Select(r => r.country).Distinct().ToList();

        private static Dictionary<string, CultureFile> Cultures() => Directory.GetFiles(Path.Combine(Resources(), "Culture"), "*.json")
            .ToDictionary(Path.GetFileNameWithoutExtension, f => JsonSerializer.Deserialize<CultureFile>(File.ReadAllText(f), Opt));

        [Test]
        public void ThirtyFiveCountriesAndNoOthers()
        {
            var countries = Countries();
            Assert.That(countries.Count, Is.EqualTo(35));
            Assert.That(countries, Does.Contain("saudi").And.Contain("mauritania"));
            foreach (var w in World) Assert.That(countries, Does.Contain(w));
            Assert.That(countries.Any(c => c.Contains("palest")), Is.False);
        }

        /// <summary>Country names that take an article (or none) in Portuguese, German, Spanish and English. Flagged for native-speaker review.</summary>
        [TestCase("usa", "pt", "Nos Estados Unidos")]
        [TestCase("usa", "de", "In den USA")]
        [TestCase("usa", "es", "En Estados Unidos")]
        [TestCase("usa", "en", "In the USA")]
        [TestCase("uae", "pt", "Nos EAU")]
        [TestCase("uae", "de", "In den VAE")]
        [TestCase("uae", "es", "En los EAU")]
        [TestCase("uk", "pt", "No Reino Unido")]
        [TestCase("uk", "es", "En el Reino Unido")]
        [TestCase("uk", "en", "In the United Kingdom")]
        [TestCase("iraq", "pt", "No Iraque")]
        [TestCase("iraq", "de", "Im Irak")]
        [TestCase("turkey", "de", "In der Türkei")]
        [TestCase("turkey", "pt", "Na Turquia")]
        [TestCase("india", "es", "En la India")]
        [TestCase("india", "pt", "Na Índia")]
        [TestCase("lebanon", "es", "En el Líbano")]
        [TestCase("lebanon", "de", "Im Libanon")]
        [TestCase("portugal", "pt", "Em Portugal")]
        [TestCase("morocco", "pt", "Em Marrocos")]
        [TestCase("oman", "pt", "Em Omã")]
        [TestCase("japan", "pt", "No Japão")]
        [TestCase("germany", "pt", "Na Alemanha")]
        [TestCase("germany", "de", "In Deutschland")]
        public void CountryNamesTakeTheirArticles(string country, string lang, string startsWith)
        {
            var c = Cultures()[country];
            // The sentences that begin with the place ("In {c} ...", "Em {c}, ...", "En {c} ...").
            var lead = c.hospitality.Get(lang);
            Assert.That(lead, Does.StartWith(startsWith), country + "/" + lang + ": " + lead);
        }

        [Test]
        public void NoCountryNameIsLeftBareAfterAPreposition()
        {
            // "Em Japão", "In Irak", "In USA", "En EAU" were the old bare forms.
            var bare = new[] { "Em Japão", "Em Brasil", "Em Iraque", "Em EAU", "Em Estados Unidos", "Em Reino Unido", "In Irak", "In USA", "In Türkei", "In VAE", "En EAU", "En Líbano", "In UAE", "In USA,", "In United Kingdom" };
            foreach (var kv in Cultures())
            {
                var all = new List<string>();
                foreach (var l in new[] { "pt", "de", "es", "en" })
                {
                    all.Add(kv.Value.hospitality.Get(l));
                    foreach (var f in kv.Value.foods) all.Add(f.fact.Get(l));
                }
                foreach (var t in all)
                    foreach (var b in bare) Assert.That(t, Does.Not.Contain(b), kv.Key + ": " + t);
            }
        }

        [Test]
        public void EveryCountryHasAValidCultureFile()
        {
            var cultures = Cultures();
            Assert.That(cultures.Keys, Is.EquivalentTo(Countries()));
            foreach (var kv in cultures) Assert.That(CultureRules.Validate(kv.Value, kv.Key), Is.Empty, kv.Key);
        }

        [Test]
        public void FoodIdsAreUniqueAcrossTheAlbum()
        {
            var ids = Cultures().Values.SelectMany(c => c.foods).Select(f => f.id).ToList();
            Assert.That(ids.Distinct().Count(), Is.EqualTo(ids.Count));
            Assert.That(ids.Count, Is.InRange(35 * CultureRules.MinFoods, 35 * CultureRules.MaxFoods));
        }

        [Test]
        public void CultureAnimalsAreInTheAnimalFile()
        {
            var file = Read<LocalAnimalFile>("Companions", "local_animals.json");
            Assert.That(file.Validate(), Is.Empty);
            foreach (var kv in Cultures())
                foreach (var a in kv.Value.animals)
                {
                    var found = file.Find(a);
                    Assert.That(found, Is.Not.Null, kv.Key + " " + a);
                    Assert.That(found.country, Is.EqualTo(kv.Key), a);
                }
        }

        [Test]
        public void ArabCountriesSpeakTheirDialectAndGetArabic()
        {
            var regions = Regions();
            foreach (var kv in Cultures())
            {
                var group = regions.First(r => r.country == kv.Key).group;
                if (group == "world") continue;
                Assert.That(kv.Value.uiLanguage, Is.EqualTo("ar"), kv.Key);
            }
        }

        [Test]
        public void TracksAreValid()
        {
            var races = Read<RaceFile>("Races", "tracks.json");
            Assert.That(races.Validate(), Is.Empty);
            Assert.That(races.tracks.Length, Is.EqualTo(35));
        }

        [Test]
        public void EachCountryRacesOnceOnItsOwnTrack()
        {
            var races = Read<RaceFile>("Races", "tracks.json");
            var regions = Regions();
            var stages = regions.SelectMany(r => r.stages.Where(s => s.type == "race").Select(s => (r, s))).ToList();
            Assert.That(stages.Count, Is.EqualTo(35));
            Assert.That(stages.Select(x => x.r.country).Distinct().Count(), Is.EqualTo(35));
            foreach (var (r, s) in stages)
            {
                var t = races.Track(s.race);
                Assert.That(t, Is.Not.Null, s.id + " names " + s.race);
                Assert.That(t.region, Is.EqualTo(r.id), s.id);
                Assert.That(r.stages[r.stages.Length - 2], Is.SameAs(s), s.id + " sits right before the boss");
                Assert.That(StageCheck.Problems(StageLayout.Generate(s)), Is.Empty, s.id);
            }
            foreach (var t in races.tracks) Assert.That(stages.Any(x => x.s.race == t.id), t.id + " is never raced");
        }

        [Test]
        public void WorldRegionsPassTheCatalogChecks()
        {
            var c = RegionCatalog.FromRegions(Regions());
            var ids = Regions().Where(r => r.group == "world").Select(r => "region " + r.id).ToList();
            Assert.That(ids.Count, Is.EqualTo(World.Length));
            var errors = c.Validate().Where(e => ids.Any(id => e.StartsWith(id + " ", StringComparison.Ordinal) || e.StartsWith(id + ":", StringComparison.Ordinal)));
            Assert.That(errors, Is.Empty);
            Assert.That(c.regions.Where(r => r.group == "world").Select(r => r.id), Is.EqualTo(World));
        }

        [Test]
        public void WorldCountriesHaveCostumesShopsAndAnIdle()
        {
            foreach (var r in Regions().Where(r => r.group == "world" || r.id == "mauritania"))
            {
                var costumes = Read<Aron.Logic.Costumes.CostumeFile>("Costumes", r.country + ".json");
                Assert.That(Aron.Logic.Costumes.CostumeRules.Validate(costumes), Is.Empty, r.id);
                var aron = costumes.costumes.Single(x => x.hero == "aron" && x.isDefault);
                Assert.That(aron.boredIdle?.id, Is.Not.Null.And.Not.Empty, r.id);
                Assert.That(Read<RegionShopFile>("RegionShops", r.id + ".json").Validate(r), Is.Empty, r.id);
            }
        }

        [Test]
        public void EveryLandmarkKindIsBuiltByTheFactory()
        {
            var src = File.ReadAllText(Path.Combine(Path.GetDirectoryName(Resources()), "Scripts", "Stages", "LandmarkFactory.cs"));
            foreach (var k in LandmarkKinds.All) Assert.That(src, Does.Contain("case \"" + k + "\":"), k);
        }
    }
}
