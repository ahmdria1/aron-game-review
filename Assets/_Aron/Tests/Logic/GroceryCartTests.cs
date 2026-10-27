// (c) Ahmed Alamrani. All rights reserved.
using Aron.Logic;
using NUnit.Framework;

namespace Aron.Tests.Logic
{
    public class GroceryCartTests
    {
        [Test]
        public void AddsUpQuantities()
        {
            var c = new GroceryCart();
            c.Add("rice");
            c.Add("rice", 2);
            c.Add("milk");
            Assert.AreEqual(3, c.QuantityOf("rice"));
            Assert.AreEqual(4, c.TotalQuantity);
            Assert.AreEqual(2, c.Lines.Count);
            Assert.AreEqual("rice", c.Lines[0].id, "keeps the order things were added");
        }

        [Test]
        public void RemovingTheLastOneDropsTheLine()
        {
            var c = new GroceryCart();
            c.Add("rice");
            c.Remove("rice");
            Assert.IsTrue(c.IsEmpty);
            c.Remove("rice");
            Assert.AreEqual(0, c.QuantityOf("rice"));
        }

        [Test]
        public void QuantityIsCapped()
        {
            var c = new GroceryCart();
            c.Add("rice", 500);
            Assert.AreEqual(GroceryCart.MaxPerItem, c.QuantityOf("rice"));
        }

        [Test]
        public void IgnoresNonsense()
        {
            var c = new GroceryCart();
            c.Add(null);
            c.Add("", 3);
            c.Add("rice", -2);
            c.Remove("rice", -5);
            Assert.IsTrue(c.IsEmpty);
        }

        [Test]
        public void ClearEmptiesIt()
        {
            var c = new GroceryCart();
            c.Add("a"); c.Add("b");
            c.Clear();
            Assert.IsTrue(c.IsEmpty);
            Assert.AreEqual(0, c.TotalQuantity);
        }
    }
}
