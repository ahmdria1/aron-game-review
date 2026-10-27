// (c) Ahmed Alamrani. All rights reserved.
using System.IO;
using System.Linq;
using System.Text.Json;
using Aron.Logic.Regions;
using NUnit.Framework;

namespace Aron.Logic.Tests
{
    /// <summary>Religious and national respect: no stage at a mosque or holy site, sacred text never on props, no flags.</summary>
    public class RespectTests
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

        [Test]
        public void NoStageAtAHolyPlaceAndWorshipIsSkylineOnly()
        {
            foreach (var f in Directory.GetFiles(Path.Combine(Resources(), "Regions"), "*.json"))
            {
                var r = JsonSerializer.Deserialize<RegionDef>(File.ReadAllText(f), Opt);
                foreach (var s in r.stages)
                {
                    Assert.That(RespectRules.NamesAHolyPlace(s.nameEn) || RespectRules.NamesAHolyPlace(s.nameAr), Is.False, s.id);
                    Assert.That(s.landmarks.Intersect(LandmarkKinds.Sacred), Is.Empty, s.id);
                }
            }
        }

        [Test]
        public void MakkahAndMadinahStagesStayAwayFromTheHaram()
        {
            foreach (var id in new[] { "makkah", "madinah" })
            {
                var r = JsonSerializer.Deserialize<RegionDef>(File.ReadAllText(Path.Combine(Resources(), "Regions", id + ".json")), Opt);
                foreach (var s in r.stages)
                {
                    string n = (s.nameEn + " " + s.nameAr + " " + s.lineAr).ToLowerInvariant();
                    foreach (var w in new[] { "haram", "kaaba", "prophet", "uhud", "الحرم", "الكعبة", "النبوي", "جبل أحد" }) Assert.That(n, Does.Not.Contain(w), s.id);
                }
            }
        }

        [Test]
        public void NoForbiddenLandmarkInAnyRegionStageOrSkyline()
        {
            foreach (var f in Directory.GetFiles(Path.Combine(Resources(), "Regions"), "*.json"))
            {
                var r = JsonSerializer.Deserialize<RegionDef>(File.ReadAllText(f), Opt);
                foreach (var l in r.stages.SelectMany(x => x.landmarks).Concat(r.skyline ?? new string[0]))
                    foreach (var bad in LandmarkKinds.Forbidden)
                        Assert.That(l, Does.Not.Contain(bad), r.id + ": landmark " + l);
            }
            foreach (var l in LandmarkKinds.All)
                foreach (var bad in LandmarkKinds.Forbidden)
                    Assert.That(l, Does.Not.Contain(bad), "landmark kind " + l);
            foreach (var bad in new[] { "stupa", "pagoda", "torii", "church", "cathedral", "cross", "redeemer", "temple_active", "shrine", "arms_statue", "domed_basilica", "uluru", "red_monolith" })
                Assert.That(LandmarkKinds.Forbidden, Does.Contain(bad));
        }

        [Test]
        public void JapanAndIndonesiaUseNatureAndCastleLandmarksOnly()
        {
            RegionDef Load(string id) => JsonSerializer.Deserialize<RegionDef>(File.ReadAllText(Path.Combine(Resources(), "Regions", id + ".json")), Opt);
            Assert.That(Load("japan").skyline, Is.EqualTo(new[] { "castle_keep", "snow_mountain", "bamboo_grove", "red_wooden_bridge" }));
            Assert.That(Load("indonesia").skyline, Is.EqualTo(new[] { "volcano_rice_terraces", "volcano" }));
            foreach (var id in new[] { "japan", "indonesia" })
                Assert.That(Load(id).skyline.Intersect(LandmarkKinds.Sacred), Is.Empty, id);
        }

        [Test]
        public void DroppedLandmarksAreGoneFromFranceSpainJapanAndMauritania()
        {
            RegionDef Load(string id) => JsonSerializer.Deserialize<RegionDef>(File.ReadAllText(Path.Combine(Resources(), "Regions", id + ".json")), Opt);
            var dropped = new[] { "island_abbey", "manuscript_town", "stone_lantern", "lantern_stone", "mosque_cathedral" };
            foreach (var l in dropped) Assert.That(LandmarkKinds.All, Does.Not.Contain(l), l);
            foreach (var id in new[] { "france", "spain", "japan", "mauritania" })
            {
                var r = Load(id);
                var text = string.Join(" ", r.stages.SelectMany(x => new[] { x.nameAr, x.nameEn, x.lineAr }));
                foreach (var l in r.stages.SelectMany(x => x.landmarks).Concat(r.skyline)) Assert.That(dropped, Does.Not.Contain(l), id);
                foreach (var w in new[] { "دير", "المخطوطات", "قرطبة", "Abbey", "Monastery", "Manuscript", "Cordoba", "Córdoba", "Mezquita", "Lantern" })
                    Assert.That(text, Does.Not.Contain(w), id + " " + w);
            }
            Assert.That(Load("france").skyline, Is.Empty);
            // Kept on purpose: the Alhambra-style palace courtyard, the cherry trees, and the Fuji-style snow mountain.
            Assert.That(Load("spain").stages.Any(x => x.landmarks.Contains("palace_arches")), Is.True);
            Assert.That(Load("japan").stages.Any(x => x.landmarks.Contains("cherry_trees")), Is.True);
            Assert.That(Load("japan").skyline, Does.Contain("snow_mountain"));
            // Mosques and minarets stay allowed in skylines, so the forbidden list must not name them.
            foreach (var ok in new[] { "mosque", "minaret", "madrasa" })
                foreach (var f in LandmarkKinds.Forbidden) Assert.That(f, Does.Not.Contain(ok));
            Assert.That(LandmarkKinds.All, Does.Contain("spiral_minaret"));
            Assert.That(LandmarkKinds.All, Does.Contain("madrasa_portal"));
        }

        [Test]
        public void AncientRuinStagesSayRuinsAndNotWorship()
        {
            RegionDef Load(string id) => JsonSerializer.Deserialize<RegionDef>(File.ReadAllText(Path.Combine(Resources(), "Regions", id + ".json")), Opt);
            var karnak = Load("egypt").stages.Single(x => x.id == "egypt_6");
            var baalbek = Load("lebanon").stages.Single(x => x.id == "lebanon_5");
            foreach (var st in new[] { karnak, baalbek })
            {
                Assert.That(st.nameEn.ToLowerInvariant(), Does.Contain("ruins").And.Not.Contain("temple"), st.id);
                Assert.That(st.nameAr, Does.Contain("آثار").And.Not.Contain("معبد").And.Not.Contain("هياكل"), st.id);
                Assert.That(st.lineAr, Does.Not.Contain("معبد"), st.id);
                Assert.That(RespectRules.NamesAHolyPlace(st.nameEn) || RespectRules.NamesAHolyPlace(st.nameAr), Is.False, st.id);
                Assert.That(st.landmarks.Intersect(LandmarkKinds.Sacred), Is.Empty, st.id);
            }
        }

        [Test]
        public void HolyWordsAreCaughtButTheSquareIsNot()
        {
            Assert.That(RespectRules.NamesAHolyPlace("Sheikh Zayed Mosque Area"), Is.True);
            Assert.That(RespectRules.NamesAHolyPlace("جامع الشيخ زايد"), Is.True);
            Assert.That(RespectRules.NamesAHolyPlace("Mount Uhud"), Is.True);
            Assert.That(RespectRules.NamesAHolyPlace("ساحة جامع الفنا"), Is.False);
            Assert.That(RespectRules.NamesAHolyPlace("Luxor - Karnak Ancient Ruins"), Is.False, "ancient ruins are archaeology");
        }

        [Test]
        public void SacredTextNeverOnProps()
        {
            Assert.That(RespectRules.PropTextAllowed("لا إله إلا الله"), Is.False);
            Assert.That(RespectRules.PropTextAllowed("Allah"), Is.True, "only the Arabic is checked here; English props carry no religious text either");
            Assert.That(RespectRules.PropTextAllowed("سوق الزل"), Is.True);
            foreach (var f in Directory.GetFiles(Resources(), "*.json", SearchOption.AllDirectories))
            {
                if (f.Contains("VoiceLines") || f.Contains("HeroLines") || f.Contains("Barks")) continue;
                var t = File.ReadAllText(f);
                foreach (var w in new[] { "لا إله إلا الله", "محمد رسول الله" }) Assert.That(t, Does.Not.Contain(w), f);
            }
        }

        [Test]
        public void NoFlagArtAnywhere()
        {
            foreach (var f in Directory.GetFiles(Resources(), "*.json", SearchOption.AllDirectories))
            {
                var t = File.ReadAllText(f).ToLowerInvariant();
                Assert.That(t, Does.Not.Contain("\"flag_"), f);
                Assert.That(t, Does.Not.Contain("national_flag"), f);
            }
        }
    }
}

namespace Aron.Logic.Tests
{
    public class IpNameTests
    {
        [Test]
        public void BrandsAndNightTowerAreCaught()
        {
            Assert.That(Aron.Logic.Regions.RespectRules.NamesABrand("Burj Khalifa Run"), Is.True);
            Assert.That(Aron.Logic.Regions.RespectRules.NamesABrand("Downtown Dubai"), Is.False);
            var c = new Aron.Logic.Regions.RegionCatalog();
            Assert.That(c, Is.Not.Null);
        }
    }
}
