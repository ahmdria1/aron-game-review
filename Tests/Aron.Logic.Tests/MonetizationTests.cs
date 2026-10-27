// (c) Ahmed Alamrani. All rights reserved.
using System.IO;
using System.Linq;
using System.Text.Json;
using Aron.Logic;
using NUnit.Framework;

namespace Aron.Logic.Tests
{
    /// <summary>
    /// No paid randomness (Belgium / Netherlands loot-box rules, store odds rules): the gift box is free and on a timer,
    /// nothing bought with money or coins gives a random result, and nothing sold depends on ads (the game has none).
    /// </summary>
    public class MonetizationTests
    {
        private static readonly JsonSerializerOptions Opt = new JsonSerializerOptions { IncludeFields = true };

        private static string Res(params string[] p)
        {
            for (var d = new DirectoryInfo(TestContext.CurrentContext.TestDirectory); d != null; d = d.Parent)
            {
                var r = Path.Combine(d.FullName, "Assets", "_Aron", "Resources");
                if (Directory.Exists(r)) return Path.Combine(new[] { r }.Concat(p).ToArray());
            }
            Assert.Fail("Resources not found");
            return null;
        }

        private static StoreCatalog Catalog() => JsonSerializer.Deserialize<StoreCatalog>(File.ReadAllText(Res("Store", "StoreCatalog.json")), Opt);

        [Test]
        public void NothingForSaleIsRandom()
        {
            string[] words = { "random", "mystery", "loot", "chance", "gacha", "عشوائ", "مفاجأة", "صندوق", "حظ" };
            foreach (var i in Catalog().items)
            {
                string t = (i.id + " " + i.nameAr + " " + i.descAr).ToLowerInvariant();
                foreach (var w in words) Assert.That(t, Does.Not.Contain(w), i.id);
                // A paid item's contents are fixed and listed: coins and named grants only.
                foreach (var g in i.grants) Assert.That(Catalog().Find(g), Is.Not.Null, i.id + " grants unknown " + g);
            }
        }

        [Test]
        public void TheGiftBoxIsFreeAndCannotBeBoughtOrSkipped()
        {
            var t = JsonSerializer.Deserialize<GiftTable>(File.ReadAllText(Res("Config", "gifts.json")), Opt);
            Assert.That(t.hoursBetween, Is.GreaterThan(0f));
            foreach (var i in Catalog().items)
                Assert.That(i.id.Contains("gift") || i.slot == "gift", Is.False, "gift box or gift skip must never be sold: " + i.id);
        }

        [Test]
        public void NothingSoldAboutAdsBecauseThereAreNone()
        {
            foreach (var i in Catalog().items)
            {
                Assert.That(i.removesAds, Is.False, i.id);
                Assert.That(i.grants, Does.Not.Contain("remove_ads"), i.id);
            }
        }

        [Test]
        public void EveryStoreProductHasAFixedDescription()
        {
            foreach (var i in Catalog().items.Where(x => !string.IsNullOrEmpty(x.productId)))
            {
                Assert.That(i.descAr, Is.Not.Empty, i.id);
                Assert.That(i.productId, Does.StartWith("com.ahmdria1.aron."), i.id);
            }
        }
    }
}

namespace Aron.Logic.Tests
{
    public class AccountDeletionTests
    {
        [Test]
        public void DeletingTheAccountRemovesEmailConsentAndPhotosBeforeThePlayerId()
        {
            var s = Aron.Logic.Safety.AccountData.DeleteSteps;
            foreach (var step in new[] { "local_save", "cloud_save", "ghosts", "leaderboard_entries", "email_link", "consent_record", "otp_state", "passport_photos" })
                Assert.That(System.Array.IndexOf(s, step), Is.GreaterThanOrEqualTo(0).And.LessThan(System.Array.IndexOf(s, "player_id")), step);
            Assert.That(s[s.Length - 1], Is.EqualTo("player_id"));
        }
    }
}
