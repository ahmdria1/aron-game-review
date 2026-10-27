// (c) Ahmed Alamrani. All rights reserved.
using Aron.Logic;
using NUnit.Framework;

namespace Aron.Tests.Logic
{
    public class ShopSignsConfigTests
    {
        private static ShopSignsConfig Config(int version, params string[] ids)
        {
            var signs = new ShopSignEntry[ids.Length];
            for (int i = 0; i < ids.Length; i++) signs[i] = new ShopSignEntry { id = ids[i], textAr = "قهوة" };
            return new ShopSignsConfig { version = version, signs = signs };
        }

        [Test]
        public void FindsSignById()
        {
            var c = Config(1, "shop_1", "shop_2");
            Assert.AreEqual("shop_2", c.Find("shop_2").id);
            Assert.IsNull(c.Find("missing"));
            Assert.IsNull(c.Find(null));
        }

        [Test]
        public void ConfigWithoutSignsIsInvalid()
        {
            Assert.IsFalse(new ShopSignsConfig { version = 1 }.IsValid());
            Assert.IsFalse(Config(1, "shop_1", "").IsValid());
            Assert.IsTrue(Config(1).IsValid(), "an empty list is a valid way to clear signs");
        }

        [Test]
        public void NewerOrEqualVersionReplacesCurrent()
        {
            Assert.IsTrue(ShopSignsConfig.ShouldReplace(Config(2, "a"), Config(3, "a")));
            Assert.IsTrue(ShopSignsConfig.ShouldReplace(Config(2, "a"), Config(2, "a")));
            Assert.IsTrue(ShopSignsConfig.ShouldReplace(null, Config(1, "a")));
        }

        [Test]
        public void OlderOrInvalidConfigIsRejected()
        {
            Assert.IsFalse(ShopSignsConfig.ShouldReplace(Config(5, "a"), Config(4, "a")));
            Assert.IsFalse(ShopSignsConfig.ShouldReplace(Config(1, "a"), null));
            Assert.IsFalse(ShopSignsConfig.ShouldReplace(Config(1, "a"), new ShopSignsConfig { version = 9 }));
        }
    }
}
