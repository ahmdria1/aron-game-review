// (c) Ahmed Alamrani. All rights reserved.
using System.Collections.Generic;
using Aron.Logic.Costumes;
using NUnit.Framework;

namespace Aron.Logic.Tests
{
    public class WardrobeTests
    {
        private static CostumeDefinition C(string id, string hero, int price) =>
            new CostumeDefinition { id = id, hero = hero, price = price, country = "saudi" };

        [Test]
        public void FreeCostumeIsWornWithoutPaying()
        {
            var d = new SaveData { coins = 10 };
            Assert.IsTrue(Wardrobe.TryWear(d, C("saudi_hijazi", "aron", 0)));
            Assert.AreEqual("saudi_hijazi", d.costumeId);
            Assert.AreEqual(10, d.coins);
        }

        [Test]
        public void PaidCostumeCostsCoinsOnce()
        {
            var d = new SaveData { coins = 500 };
            var bisht = C("saudi_bisht", "aron", 300);
            Assert.IsTrue(Wardrobe.TryWear(d, bisht));
            Assert.AreEqual(200, d.coins);
            Wardrobe.WearRegion(d, "aron");
            Assert.AreEqual("", d.costumeId);
            Assert.IsTrue(Wardrobe.TryWear(d, bisht));
            Assert.AreEqual(200, d.coins);
        }

        [Test]
        public void NotEnoughCoinsChangesNothing()
        {
            var d = new SaveData { coins = 50 };
            Assert.IsFalse(Wardrobe.TryWear(d, C("saudi_bisht", "aron", 300)));
            Assert.AreEqual("", d.costumeId);
            Assert.AreEqual(50, d.coins);
            Assert.IsEmpty(d.ownedCostumes);
        }

        [Test]
        public void NouraKeepsHerOwnPick()
        {
            var d = new SaveData { coins = 0 };
            Wardrobe.TryWear(d, C("saudi_hijazi", "aron", 0));
            Wardrobe.TryWear(d, C("saudi_noura_south", "noura", 0));
            Assert.AreEqual("saudi_hijazi", Wardrobe.PickOf(d, "aron"));
            Assert.AreEqual("saudi_noura_south", Wardrobe.PickOf(d, "noura"));
        }

        [Test]
        public void ListIsPerHeroCheapestFirst()
        {
            var all = new List<CostumeDefinition> { C("b", "aron", 300), C("n", "noura", 0), C("a", "aron", 0) };
            var list = Wardrobe.For(all, "aron");
            Assert.AreEqual(2, list.Count);
            Assert.AreEqual("a", list[0].id);
        }

        [Test]
        public void PickedCostumeWinsOverRegionOnceOwned()
        {
            var all = new List<CostumeDefinition>
            {
                new CostumeDefinition { id = "saudi_najdi", hero = "aron", country = "saudi", isDefault = true },
                C("saudi_bisht", "aron", 300),
                C("saudi_hijazi", "aron", 0),
            };
            var d = new SaveData { costumeId = "saudi_bisht" };
            var c = CostumeRules.Pick(all, "aron", "saudi", "saudi_hijazi", "", Wardrobe.PickOf(d, "aron"), d.ownedCostumes);
            Assert.AreEqual("saudi_hijazi", c.id);
            d.ownedCostumes.Add("saudi_bisht");
            c = CostumeRules.Pick(all, "aron", "saudi", "saudi_hijazi", "", Wardrobe.PickOf(d, "aron"), d.ownedCostumes);
            Assert.AreEqual("saudi_bisht", c.id);
        }
    }
}
