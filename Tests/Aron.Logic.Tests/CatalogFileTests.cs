// (c) Ahmed Alamrani. All rights reserved.
using System.IO;
using System.Linq;
using System.Text.Json;
using Aron.Logic;
using NUnit.Framework;

namespace Aron.Logic.Tests
{
    /// <summary>Checks the real StoreCatalog.json that ships in Resources, so a bad edit fails here and not on a phone.</summary>
    public class CatalogFileTests
    {
        private static StoreCatalog Load()
        {
            string dir = TestContext.CurrentContext.TestDirectory;
            string path = null;
            for (var d = new DirectoryInfo(dir); d != null; d = d.Parent)
            {
                var p = Path.Combine(d.FullName, "Assets", "_Aron", "Resources", "Store", "StoreCatalog.json");
                if (File.Exists(p)) { path = p; break; }
            }
            Assert.That(path, Is.Not.Null, "StoreCatalog.json not found");
            return JsonSerializer.Deserialize<StoreCatalog>(File.ReadAllText(path), new JsonSerializerOptions { IncludeFields = true });
        }

        [Test]
        public void ShippedCatalogIsValid()
        {
            Assert.That(Load().Validate(), Is.Null);
        }

        [Test]
        public void EveryLookSlotHasADefault()
        {
            var c = Load();
            foreach (var slot in c.items.Where(i => i.HasSlot).Select(i => i.slot).Distinct())
                Assert.That(c.DefaultFor(slot), Is.Not.Null, slot);
        }

        [Test]
        public void ProductIdsUseTheGamePrefix()
        {
            foreach (var i in Load().MoneyItems())
                Assert.That(i.productId, Does.StartWith("com.ahmdria1.aron."), i.id);
        }

        [Test]
        public void ShoesAreAlwaysClosedLeather()
        {
            foreach (var i in Load().items.Where(i => i.slot == "shoes"))
            {
                Assert.That(i.nameAr, Does.Contain("جلد"), i.id);
                Assert.That(i.effect ?? "", Is.AnyOf("", "oxford", "loafer"), i.id);
            }
        }

        [Test]
        public void BothWebShopsStartClosedWithExamples()
        {
            var c = Load();
            foreach (var shop in new[] { c.developerShop, c.groceryShop })
            {
                Assert.That(shop.IsOpen, Is.False);
                Assert.That(shop.products, Is.Not.Empty);
                foreach (var p in shop.products)
                    Assert.That(shop.categories.Any(k => k.id == p.category), p.id);
            }
        }
    }
}
