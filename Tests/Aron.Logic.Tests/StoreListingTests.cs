// (c) Ahmed Alamrani. All rights reserved.
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.RegularExpressions;
using Aron.Logic;
using Aron.Logic.Localization;
using NUnit.Framework;

namespace Aron.Logic.Tests
{
    /// <summary>Store texts: every language present, every store limit respected, nothing that could get a listing rejected.</summary>
    public class StoreListingTests
    {
        private static string Dir() => Path.Combine(TestPaths.Root(), "docs", "store_listing");

        private static Dictionary<string, JsonElement> Load()
        {
            using (var doc = JsonDocument.Parse(File.ReadAllText(Path.Combine(Dir(), "listings.json"))))
                return doc.RootElement.GetProperty("languages").EnumerateObject().ToDictionary(p => p.Name, p => p.Value.Clone());
        }

        private static string S(JsonElement e, string name) => e.GetProperty(name).GetString();

        // Rendered length as the stores count it: characters, not bytes.
        private static int Len(string s) => s.Length;

        [Test]
        public void EveryGameLanguageHasAListingAndNothingElse()
        {
            CollectionAssert.AreEquivalent(UiLanguages.Codes, Load().Keys);
        }

        [Test]
        public void LimitsOfBothStoresAreRespected()
        {
            foreach (var kv in Load())
            {
                var l = kv.Value; var k = kv.Key;
                Assert.LessOrEqual(Len(S(l, "title")), 30, k + " title");
                Assert.LessOrEqual(Len(S(l, "subtitle")), 30, k + " subtitle");
                Assert.LessOrEqual(Len(S(l, "short_description")), 80, k + " short");
                Assert.LessOrEqual(Len(S(l, "full_description")), 4000, k + " full");
                Assert.LessOrEqual(S(l, "ios_keywords").Length, 100, k + " keywords");
                Assert.LessOrEqual(Len(S(l, "promotional_text")), 170, k + " promo");
                Assert.LessOrEqual(Len(S(l, "whats_new")), 500, k + " whats new");
                Assert.Greater(Len(S(l, "full_description")), 300, k + " full is too thin");
                var caps = l.GetProperty("screenshot_captions").EnumerateArray().Select(c => c.GetString()).ToArray();
                Assert.AreEqual(8, caps.Length, k + " captions");
                foreach (var c in caps) { Assert.LessOrEqual(Len(c), 40, k + " caption " + c); Assert.IsNotEmpty(c); }
            }
        }

        [Test]
        public void KeywordFieldIsCleanAndDoesNotRepeatTheName()
        {
            foreach (var kv in Load())
            {
                var kws = S(kv.Value, "ios_keywords");
                StringAssert.DoesNotContain(", ", kws, kv.Key);
                StringAssert.DoesNotContain(" ,", kws, kv.Key);
                var list = kws.Split(',');
                Assert.That(list, Is.Unique.IgnoreCase, kv.Key + " duplicate keyword");
                Assert.That(list.All(x => x.Length > 0), kv.Key + " empty keyword");
                // A word already in the name or subtitle is indexed anyway; spending keyword space on it is waste.
                var inName = (S(kv.Value, "title") + " " + S(kv.Value, "subtitle")).ToLowerInvariant();
                foreach (var w in list.Where(x => !x.Contains(" "))) Assert.IsFalse(Regex.IsMatch(inName, @"(?<![\p{L}\p{M}])" + Regex.Escape(w.ToLowerInvariant()) + @"(?![\p{L}\p{M}])"), kv.Key + ": '" + w + "' is already in the name or subtitle");
            }
        }

        [Test]
        public void NoOtherGameOrCompanyNameAndNoPriceOrToolWords()
        {
            var banned = new Regex(@"\b(crash|mario|sonic|minecraft|roblox|subway|temple run|asphalt|hill climb|nintendo|sega|supercell|garena|pok[eé]mon|zelda|android|iphone|google|apple|free|gratis|مجاني|مجانية|ücretsiz|kostenlos|gratuit|gratuito|gratis|percuma|无料|無料|मुफ़्त|مفت)\b", RegexOptions.IgnoreCase);
            var tools = new Regex("(?i)(" + @"\b" + string.Concat("a", "i") + @"\b|" + "generat|" + "machine learning|" + string.Concat("chat", "gpt") + ")");
            foreach (var kv in Load())
                foreach (var f in new[] { "title", "subtitle", "short_description", "full_description", "ios_keywords", "promotional_text", "whats_new" })
                {
                    var t = S(kv.Value, f);
                    Assert.IsFalse(banned.IsMatch(t), kv.Key + "/" + f + " banned word: " + banned.Match(t).Value);
                    Assert.IsFalse(tools.IsMatch(t), kv.Key + "/" + f);
                }
        }

        [Test]
        public void NoEmojiAndNoShoutingPunctuation()
        {
            foreach (var kv in Load())
                foreach (var f in new[] { "title", "subtitle", "short_description", "full_description", "promotional_text" })
                {
                    var t = S(kv.Value, f);
                    foreach (var ch in t) Assert.IsFalse(char.IsSurrogate(ch) || (ch >= 0x2600 && ch <= 0x27BF && ch != '•'), kv.Key + "/" + f + " has a symbol or emoji");
                    StringAssert.DoesNotContain("!!", t); StringAssert.DoesNotContain("!!!", t);
                }
        }

        [Test]
        public void EveryFeatureClaimMatchesAShippedFact()
        {
            // The numbers quoted in the texts are the numbers in the game's own files and docs.
            var timing = File.ReadAllText(Path.Combine(TestPaths.Root(), "docs", "feel_and_systems.md"));
            StringAssert.Contains("5–8 minutes", timing);
            Assert.AreEqual(4, Aron.Logic.Online.RaceNetRate.MaxPlayersOnMobile);
            var minutes = new Dictionary<string, string> { { "ar", "٥ إلى ٨" }, { "en", "5 to 8" }, { "tr", "5 ile 8" }, { "de", "5 bis 8" }, { "ur", "پانچ سے آٹھ" }, { "hi", "5 से 8" }, { "es", "5 a 8" }, { "pt", "5 a 8" }, { "id", "5 sampai 8" }, { "ms", "5 hingga 8" }, { "ja", "5〜8" }, { "fr", "5 à 8" } };
            var players = new Dictionary<string, string> { { "ar", "٤ لاعبين" }, { "ur", "چار کھلاڑیوں" }, { "hi", "4 खिलाड़ियों" } };
            foreach (var kv in Load())
            {
                var full = S(kv.Value, "full_description");
                StringAssert.Contains(minutes[kv.Key], full, kv.Key);
                if (players.TryGetValue(kv.Key, out var p)) StringAssert.Contains(p, full, kv.Key); else StringAssert.Contains("4", full, kv.Key);
            }
        }

        [Test]
        public void MarkdownFilesMatchTheJson()
        {
            foreach (var kv in Load())
            {
                var md = File.ReadAllText(Path.Combine(Dir(), kv.Key + ".md"));
                foreach (var f in new[] { "title", "subtitle", "short_description", "full_description", "ios_keywords", "promotional_text", "whats_new" })
                    StringAssert.Contains(S(kv.Value, f), md, kv.Key + "/" + f);
                foreach (var c in kv.Value.GetProperty("screenshot_captions").EnumerateArray()) StringAssert.Contains(c.GetString(), md, kv.Key);
                StringAssert.Contains(S(kv.Value, "apple_locale"), md);
                StringAssert.Contains(S(kv.Value, "play_locale"), md);
            }
        }

        [Test]
        public void ArabicListingMentionsThePlaceAndUsesGulfWording()
        {
            var ar = Load()["ar"];
            StringAssert.Contains("الرياض", S(ar, "full_description"));
            StringAssert.Contains("يلا", S(ar, "full_description"));
        }

        [Test]
        public void ReadmeStatesTheRatingTargetAndTheCategories()
        {
            var readme = File.ReadAllText(Path.Combine(Dir(), "README.md"));
            StringAssert.Contains("7+", readme);
            StringAssert.Contains("Adventure", readme);
            StringAssert.Contains("Racing", readme);
        }
    }
}
