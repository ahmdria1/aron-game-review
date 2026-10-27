// (c) Ahmed Alamrani. All rights reserved.
using System;
using System.Linq;
using Aron.Logic.Culture;
using Aron.Logic.Passport;
using Aron.Logic.Regions;
using NUnit.Framework;

namespace Aron.Logic.Tests
{
    public class MuseumTests
    {
        private static readonly DateTime Now = new DateTime(2026, 10, 27, 9, 0, 0, DateTimeKind.Utc);
        private static ArtifactFile File() => TestData.Read<ArtifactFile>("Museum", "artifacts.json");
        private static string[] Countries() => TestData.Catalog().regions.Select(ProgressRing.CountryOf).Distinct().ToArray();

        [Test] public void TheShippedFileIsValidAndHasOnePerCountry() => Assert.That(File().Validate(Countries()), Is.Empty);

        [Test]
        public void ThirtyFiveCountriesThirtyFiveArtifactsEachNamedInTwelveLanguages()
        {
            var f = File();
            Assert.That(Countries().Length, Is.EqualTo(35));
            Assert.That(f.artifacts.Length, Is.EqualTo(35));
            foreach (var a in f.artifacts) Assert.That(a.name.Missing(), Is.Empty, a.id);
        }

        [Test]
        public void NamesDifferBetweenLanguagesSoNoneIsLeftAsEnglishByMistake()
        {
            // A name may legitimately match English in some languages (Mandole, Berimbau), but never in most of them.
            foreach (var a in File().artifacts)
            {
                int same = a.name.All().Count(n => n == a.name.en);
                Assert.That(same, Is.LessThanOrEqualTo(9), a.id + " looks untranslated");
                Assert.That(a.name.ar, Is.Not.EqualTo(a.name.en), a.id);
            }
        }

        [Test]
        public void ArtifactsAreCraftsAndNotSymbolsOrWeapons()
        {
            foreach (var a in File().artifacts)
                foreach (var w in ArtifactFile.Banned)
                    Assert.That((a.name.en + " " + a.fact.en).ToLowerInvariant().Contains(w), Is.False, a.id + " " + w);
        }

        [Test]
        public void TheHiddenStarOnAnOrdinaryStageFindsTheCountrysArtifactOnce()
        {
            var cat = TestData.Catalog(); var file = File(); var d = new SaveData();
            var ksa = cat.regions.First(r => ProgressRing.CountryOf(r) == "saudi");
            var stage = ksa.stages.First(s => !s.boss && !s.IsRace);
            Assert.That(ArtifactBook.OnHiddenStar(cat, d, file, stage.id, false, Now), Is.Null, "no hidden star, no artifact");
            var a = ArtifactBook.OnHiddenStar(cat, d, file, stage.id, true, Now);
            Assert.That(a, Is.Not.Null);
            Assert.That(a.country, Is.EqualTo("saudi"));
            Assert.That(ArtifactBook.Has(d, "saudi"), Is.True);
            Assert.That(ArtifactBook.Date(d, "saudi"), Is.EqualTo("2026-10-27"));
            Assert.That(ArtifactBook.OnHiddenStar(cat, d, file, stage.id, true, Now), Is.Null, "only the first time");
            Assert.That(ArtifactBook.Count(d, file, out int total), Is.EqualTo(1));
            Assert.That(total, Is.EqualTo(35));
        }

        [Test]
        public void ARaceStageNeverGivesAnArtifact()
        {
            var cat = TestData.Catalog(); var file = File(); var d = new SaveData();
            var race = cat.regions.SelectMany(r => r.stages).First(s => s.IsRace);
            Assert.That(ArtifactBook.OnHiddenStar(cat, d, file, race.id, true, Now), Is.Null);
            Assert.That(d.artifactsFound, Is.Empty);
        }

        [Test]
        public void TheMuseumListsEveryCountryInRingOrderWithShadowsForTheMissingOnes()
        {
            var cat = TestData.Catalog(); var file = File(); var d = new SaveData { homeCountry = "saudi" };
            var list = ArtifactBook.Entries(cat, d, file);
            Assert.That(list.Count, Is.EqualTo(35));
            Assert.That(list[0].country, Is.EqualTo("saudi"));
            Assert.That(list.All(e => !e.found), Is.True);
            var ksa = cat.regions.First(r => ProgressRing.CountryOf(r) == "saudi").stages.First(s => !s.boss && !s.IsRace);
            ArtifactBook.OnHiddenStar(cat, d, file, ksa.id, true, Now);
            list = ArtifactBook.Entries(cat, d, file);
            Assert.That(list.Count(e => e.found), Is.EqualTo(1));
            Assert.That(list[0].found && list[0].date == "2026-10-27" && list[0].localCountry == "السعودية");
            Assert.That(ArtifactBook.Complete(d, file), Is.False);
        }

        [Test]
        public void TheMuseumIsCompleteWhenEveryCountryIsFound()
        {
            var file = File(); var d = new SaveData();
            foreach (var a in file.artifacts) { var l = d.artifactsFound; PassportBook.Set(ref l, a.country, "2026-10-27"); d.artifactsFound = l; }
            Assert.That(ArtifactBook.Complete(d, file), Is.True);
        }

        [Test]
        public void ThePassportPageShowsTheMuseumStickerAndTheStampMomentReusesPassportFeedback()
        {
            var cat = TestData.Catalog(); var file = File(); var d = new SaveData();
            var stage = cat.regions.First(r => ProgressRing.CountryOf(r) == "japan").stages.First(s => !s.boss && !s.IsRace);
            Assert.That(PassportBook.Page(cat, d, "japan", null).artifact, Is.False);
            ArtifactBook.OnHiddenStar(cat, d, file, stage.id, true, Now);
            var page = PassportBook.Page(cat, d, "japan", null);
            Assert.That(page.artifact, Is.True);
            Assert.That(page.artifactDate, Is.EqualTo("2026-10-27"));
            Assert.That(ArtifactBook.Sfx, Is.EqualTo(PassportBook.StampSfx));
            Assert.That(ArtifactBook.Haptic, Is.EqualTo(PassportBook.StampHaptic));
        }
    }
}
