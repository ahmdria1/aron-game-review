// (c) Ahmed Alamrani. All rights reserved.
using System.Collections.Generic;
using Aron.Logic.Costumes;
using NUnit.Framework;

namespace Aron.Logic.Tests
{
    public class CostumeTests
    {
        private static CostumeDefinition Najdi() => new CostumeDefinition
        {
            id = "saudi_najdi", country = "saudi", hero = "aron", nameAr = "ثوب", isDefault = true,
            materials = new[] { new MaterialSwap { material = "Clothes", color = "#F7F7F2" } },
            accessories = new[] { new Accessory { kind = "shemagh", color = "#C8141E" }, new Accessory { kind = "agal", color = "#111111" } },
        };

        private static CostumeDefinition Hijazi() => new CostumeDefinition
        {
            id = "saudi_hijazi", country = "saudi", hero = "aron", nameAr = "حجازي",
            accessories = new[] { new Accessory { kind = "turban", color = "#EDE4CC" } },
        };

        private static CostumeDefinition Noura(string id = "saudi_noura", bool def = true) => new CostumeDefinition
        {
            id = id, country = "saudi", hero = "noura", nameAr = "عباية", isDefault = def, footwear = "closed_flats",
            accessories = new[] { new Accessory { kind = "abaya", color = "#141418" }, new Accessory { kind = "hijab", color = "#141418" } },
        };

        private static CostumeFile File() => new CostumeFile
        {
            country = "saudi", costumes = new[] { Najdi(), Hijazi(), Noura(), Noura("saudi_noura_hijazi", false) },
        };

        [Test]
        public void SaudiFileIsValid() => Assert.That(CostumeRules.Validate(File()), Is.Empty);

        [Test]
        public void NoSneakersOrBareFeet()
        {
            foreach (var shoe in new[] { "sneakers", "barefoot", "sandals" })
            {
                var f = File();
                f.costumes[2].footwear = shoe;
                Assert.That(CostumeRules.Validate(f), Has.Some.Contains("closed shoes only"), shoe);
            }
        }

        [Test]
        public void SaudiAronWearsLeatherShoes()
        {
            var f = File();
            f.costumes[1].footwear = "leather_boots";
            Assert.That(CostumeRules.Validate(f), Has.Some.Contains("leather shoes"));
        }

        [Test]
        public void DefaultSaudiLookIsWhiteThobeRedShemaghAndAgal()
        {
            var f = File();
            f.costumes[0].materials[0].color = "#202020";
            Assert.That(CostumeRules.Validate(f), Has.Some.Contains("white thobe"));
            f = File();
            f.costumes[0].accessories[0].color = "#FFFFFF";
            Assert.That(CostumeRules.Validate(f), Has.Some.Contains("red shemagh"));
            f = File();
            f.costumes[0].accessories = new[] { new Accessory { kind = "shemagh", color = "#C8141E" } };
            Assert.That(CostumeRules.Validate(f), Has.Some.Contains("shemagh and agal"));
        }

        [Test]
        public void AgalNeedsSomethingToSitOn()
        {
            var f = File();
            f.costumes[1].accessories = new[] { new Accessory { kind = "agal", color = "#111111" } };
            Assert.That(CostumeRules.Validate(f), Has.Some.Contains("agal sits on"));
        }

        [Test]
        public void NouraAlwaysHasAbayaAndHijab()
        {
            var f = File();
            f.costumes[2].accessories = new[] { new Accessory { kind = "abaya", color = "#141418" } };
            Assert.That(CostumeRules.Validate(f), Has.Some.Contains("abaya and a hijab"));
        }

        [Test]
        public void OneDefaultPerHeroAndIdsCarryTheCountry()
        {
            var f = File();
            f.costumes[1].isDefault = true;
            Assert.That(CostumeRules.Validate(f), Has.Some.Contains("exactly one default"));
            f = File();
            f.costumes[1].id = "hijazi";
            Assert.That(CostumeRules.Validate(f), Has.Some.Contains("starts with the country"));
            f = File();
            f.costumes[1].accessories[0].kind = "cap";
            Assert.That(CostumeRules.Validate(f), Has.Some.Contains("unknown accessory"));
        }

        [Test]
        public void AgalIsNeverOnTheHeadAndInTheHandAtOnce()
        {
            foreach (bool has in new[] { true, false })
            foreach (bool weapon in new[] { true, false })
            foreach (bool attacking in new[] { true, false })
            {
                var spot = CostumeRules.Agal(has, weapon, attacking);
                Assert.That(spot == AgalSpot.Head || spot == AgalSpot.Hand || spot == AgalSpot.Wrist || spot == AgalSpot.None);
                if (spot == AgalSpot.Head) Assert.That(has, Is.True);
                if (spot == AgalSpot.Hand) Assert.That(weapon && attacking, Is.True);
                if (spot == AgalSpot.Wrist) Assert.That(weapon && !has && !attacking, Is.True);
            }
            Assert.That(CostumeRules.Agal(true, true, false), Is.EqualTo(AgalSpot.Head));
            Assert.That(CostumeRules.Agal(true, true, true), Is.EqualTo(AgalSpot.Hand));
            Assert.That(CostumeRules.Agal(false, true, false), Is.EqualTo(AgalSpot.Wrist));
            Assert.That(CostumeRules.Agal(false, true, true), Is.EqualTo(AgalSpot.Hand));
            Assert.That(CostumeRules.Agal(false, false, false), Is.EqualTo(AgalSpot.None));
            Assert.That(CostumeRules.Agal(true, false, true), Is.EqualTo(AgalSpot.Head));
        }

        [Test]
        public void PickFollowsWardrobeThenStageThenRegionThenDefault()
        {
            var all = File().costumes;
            var bisht = new CostumeDefinition { id = "saudi_bisht", country = "saudi", hero = "aron", price = 600 };
            var list = new List<CostumeDefinition>(all) { bisht };
            var owned = new List<string>();
            Assert.That(CostumeRules.Pick(list, "aron", "saudi", "", "", "", owned).id, Is.EqualTo("saudi_najdi"));
            Assert.That(CostumeRules.Pick(list, "aron", "saudi", "saudi_hijazi", "", "", owned).id, Is.EqualTo("saudi_hijazi"));
            Assert.That(CostumeRules.Pick(list, "aron", "saudi", "saudi_hijazi", "saudi_bisht", "", owned).id, Is.EqualTo("saudi_bisht"));
            Assert.That(CostumeRules.Pick(list, "aron", "saudi", "saudi_hijazi", "", "saudi_bisht", owned).id, Is.EqualTo("saudi_hijazi"), "not bought yet");
            owned.Add("saudi_bisht");
            Assert.That(CostumeRules.Pick(list, "aron", "saudi", "saudi_hijazi", "", "saudi_bisht", owned).id, Is.EqualTo("saudi_bisht"));
            Assert.That(CostumeRules.Pick(list, "aron", "saudi", "saudi_gone", "", "", owned).id, Is.EqualTo("saudi_najdi"));
            Assert.That(CostumeRules.Pick(list, "aron", "kuwait", "", "", "", owned).id, Is.EqualTo("saudi_najdi"), "falls back to Saudi");
        }

        [Test]
        public void NouraGetsHerMatchingVariant()
        {
            var all = File().costumes;
            Assert.That(CostumeRules.Pick(all, "noura", "saudi", "saudi_hijazi", "", "", null).id, Is.EqualTo("saudi_noura_hijazi"));
            Assert.That(CostumeRules.Pick(all, "noura", "saudi", "saudi_hijazi", "saudi_bisht", "", null).id, Is.EqualTo("saudi_noura_hijazi"));
            Assert.That(CostumeRules.Pick(all, "noura", "saudi", "", "", "", null).id, Is.EqualTo("saudi_noura"));
            Assert.That(CostumeRules.ForHero("saudi_south", "noura"), Is.EqualTo("saudi_noura_south"));
            Assert.That(CostumeRules.ForHero("saudi_noura", "noura"), Is.EqualTo("saudi_noura"));
            Assert.That(CostumeRules.ForHero("saudi_south", "aron"), Is.EqualTo("saudi_south"));
        }
    }
}
