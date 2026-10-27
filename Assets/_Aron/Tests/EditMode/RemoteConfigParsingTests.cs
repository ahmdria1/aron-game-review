// (c) Ahmed Alamrani. All rights reserved.
using Aron.RemoteConfig;
using NUnit.Framework;
using UnityEngine;

namespace Aron.Tests.EditMode
{
    public class RemoteConfigParsingTests
    {
        [Test]
        public void ParsesServerJson()
        {
            const string json = "{\"version\":3,\"signs\":[{\"id\":\"shop_1\",\"textAr\":\"قهوة\",\"imageUrl\":\"\",\"colorHex\":\"#7A4B2A\"}]}";
            var cfg = RemoteConfigService.Parse(json);
            Assert.IsNotNull(cfg);
            Assert.AreEqual(3, cfg.version);
            Assert.AreEqual("قهوة", cfg.Find("shop_1").textAr);
        }

        [Test]
        public void RejectsGarbage()
        {
            Assert.IsNull(RemoteConfigService.Parse(""));
            Assert.IsNull(RemoteConfigService.Parse("<html>offline</html>"));
            Assert.IsNull(RemoteConfigService.Parse("{\"version\":1}"));
        }

        [Test]
        public void BundledFallbackIsValid()
        {
            var ta = Resources.Load<TextAsset>(RemoteConfigService.FallbackResource);
            Assert.IsNotNull(ta, "default_signs.json missing from Resources");
            var cfg = RemoteConfigService.Parse(ta.text);
            Assert.IsNotNull(cfg);
            Assert.IsNotNull(cfg.Find("shop_1"));
        }
    }
}
