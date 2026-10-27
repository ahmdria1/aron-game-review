// (c) Ahmed Alamrani. All rights reserved.
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Aron.Logic.Names;
using NUnit.Framework;

namespace Aron.Logic.Tests
{
    [TestFixture]
    public class PlayerNameTests
    {
        public static readonly string[] Langs = { "ar", "en", "tr", "de", "es", "pt", "id", "ms", "fr", "ur", "hi", "ja" };

        private static NameChecker checker;
        public static NameChecker Checker()
        {
            if (checker != null) return checker;
            string dir = Path.Combine(TestData.Resources(), "Names");
            var lists = Langs.Select(l => JsonSerializer.Deserialize<BlocklistFile>(File.ReadAllText(Path.Combine(dir, "blocklist_" + l + ".json")), TestData.Opt)).ToList();
            var reserved = JsonSerializer.Deserialize<ReservedFile>(File.ReadAllText(Path.Combine(dir, "reserved.json")), TestData.Opt);
            return checker = new NameChecker(lists, reserved);
        }

        private static string Code(string name) => Checker().Check(name).Code;
        private static void Ok(string name) { var v = Checker().Check(name); Assert.IsTrue(v.Ok, name + " -> " + v.Code + " " + v.Match); }
        private static void Bad(string name, string code = null)
        {
            var v = Checker().Check(name);
            Assert.IsFalse(v.Ok, name + " should be refused");
            if (code != null) Assert.AreEqual(code, v.Code, name);
        }

        // ---- shape --------------------------------------------------------------------------------------------

        [Test]
        public void Length_is_three_to_sixteen()
        {
            Bad("ab", NameCode.TooShort);
            Ok("abc");
            Ok("abcdefghijklmnop");
            Bad("abcdefghijklmnopq", NameCode.TooLong);
            Bad("", NameCode.Empty);
            Bad(null, NameCode.Empty);
        }

        [Test]
        public void Plain_good_names_pass_in_both_scripts()
        {
            foreach (var n in new[] { "Ahmed", "Fahad", "Sultan", "Layla", "Rakan", "Dana", "Hassan", "Mishal", "Lama", "Rayan", "Jood", "Turki", "Saud", "Ali_77", "Omar99",
                "Faisal", "Khalid", "Sara", "Mia Rose", "Essex", "Classic", "Pharaoh", "José", "Zoë", "Nazia", "Colin", "Ababil", "Vergara", "Kurt", "Shine", "Cina", "Abdullah", "Abd Allah",
                "أحمد", "فهد", "سلطان", "ليلى", "ريان", "جود", "تركي", "سعود", "لمى", "عبدالله", "عبد الله", "عبد الرحمن", "زبيدة", "الكلبي", "عبيد", "نيكولا", "محمد", "أسامة", "مها_88", "صقر سريع" })
                Ok(n);
        }

        [Test]
        public void Only_one_space_or_underscore_and_never_at_the_edges()
        {
            Bad(" Ahmed", NameCode.EdgeSpace);
            Bad("Ahmed ", NameCode.EdgeSpace);
            Bad("_Ahmed", NameCode.Separators);
            Bad("Ahmed_", NameCode.Separators);
            Bad("Ah  med", NameCode.Separators);
            Bad("Ah med_x", NameCode.Separators);
            Bad("Ah\tmed", NameCode.Hidden);
            Bad("Ah\u00A0med", NameCode.BadChar);
            Bad("Ah-med", NameCode.BadChar);
            Bad("Ah.med", NameCode.BadChar);
            Bad("a@b.com", NameCode.BadChar);
        }

        [Test]
        public void Hidden_bidi_and_zero_width_characters_are_refused()
        {
            foreach (var c in new[] { '\u200B', '\u200C', '\u200D', '\u200E', '\u200F', '\u202A', '\u202B', '\u202C', '\u202D', '\u202E', '\u2066', '\u2067', '\u2068', '\u2069', '\u00AD', '\uFEFF', '\u061C', '\u2060', '\u3164', '\uFE0F' })
                Bad("Ah" + c + "med", NameCode.Hidden);
            Bad("Ah\u0000med", NameCode.Hidden);
        }

        [Test]
        public void Emoji_and_symbols_are_refused()
        {
            Bad("Ahmed\U0001F600", NameCode.Emoji);
            Bad("\U0001F42A\U0001F42A\U0001F42A", NameCode.Emoji);
            Bad("Ahmed\u2764", NameCode.Emoji);
            Bad("Ahmed\u2605", NameCode.Emoji);
            Bad("Ahmed\u00A9x", NameCode.Emoji);
        }

        [Test]
        public void Other_scripts_and_combining_marks_are_refused()
        {
            Bad("Аhmed");                                                  // Cyrillic A
            Bad("Ahmеd");                                                  // Cyrillic e
            Bad("Αλφα");                                                   // Greek
            Bad("अहमद");                                                   // Devanagari
            Bad("アハメド");                                                // Katakana
            Bad("Ahme\u0301d", NameCode.BadChar);                          // combining accent
            Bad("أَحْمَد", NameCode.BadChar);                                // tashkeel typed in the name
        }

        [Test]
        public void Arabic_and_latin_cannot_be_mixed()
        {
            Bad("Ahmedأحمد", NameCode.MixedScript);
            Bad("Aron_أرون", NameCode.MixedScript);
            Ok("Ali_99");
            Ok("أحمد_99");
            Ok("أحمد٩٩");
        }

        [Test]
        public void Digits_are_limited_and_names_need_letters()
        {
            Ok("Ahmed1234");
            Bad("Ahmed12345", NameCode.TooManyDigits);
            Bad("1234567", NameCode.NotEnoughLetters);
            Bad("12345", NameCode.NotEnoughLetters);
            Bad("A1234", NameCode.NotEnoughLetters);
            Bad("٣٣٣٣٣أب", NameCode.TooManyDigits);
        }

        [Test]
        public void Tatweel_is_removed_and_counts_for_nothing()
        {
            var v = Checker().Check("أحـــــمد");
            Assert.IsTrue(v.Ok);
            Assert.AreEqual("أحمد", v.Clean);
            Bad("اـــــب", NameCode.TooShort);
        }

        // ---- comparison form ----------------------------------------------------------------------------------

        [Test]
        public void Arabic_forms_fold_to_one_key()
        {
            Assert.AreEqual(PlayerNameRules.Key("أحمد"), PlayerNameRules.Key("احمد"));
            Assert.AreEqual(PlayerNameRules.Key("إيمان"), PlayerNameRules.Key("ايمان"));
            Assert.AreEqual(PlayerNameRules.Key("آمنة"), PlayerNameRules.Key("امنه"));
            Assert.AreEqual(PlayerNameRules.Key("نورة"), PlayerNameRules.Key("نوره"));
            Assert.AreEqual(PlayerNameRules.Key("مصطفى"), PlayerNameRules.Key("مصطفي"));
            Assert.AreEqual(PlayerNameRules.Key("أحـــمد"), PlayerNameRules.Key("احمد"));
            Assert.AreEqual(PlayerNameRules.Key("أحمد"), PlayerNameRules.Key("أَحْمَد"));
            Assert.AreEqual(PlayerNameRules.Key("کریم"), PlayerNameRules.Key("كريم"));      // Urdu keheh and yeh
            Assert.AreEqual(PlayerNameRules.Key("مؤمن"), PlayerNameRules.Key("مومن"));
            Assert.AreNotEqual(PlayerNameRules.Key("أحمد"), PlayerNameRules.Key("احمر"));
        }

        [Test]
        public void Latin_look_alikes_fold_to_one_key()
        {
            Assert.AreEqual(PlayerNameRules.UniqueKey("Ahmed"), PlayerNameRules.UniqueKey("AHMED"));
            Assert.AreEqual(PlayerNameRules.UniqueKey("Ahmed"), PlayerNameRules.UniqueKey("Ahm3d"));
            Assert.AreEqual(PlayerNameRules.UniqueKey("Ahmed"), PlayerNameRules.UniqueKey("A h m e d"));
            Assert.AreEqual(PlayerNameRules.UniqueKey("Ahmed"), PlayerNameRules.UniqueKey("Ahmed"));
            Assert.AreEqual(PlayerNameRules.UniqueKey("Ilya"), PlayerNameRules.UniqueKey("llya"));  // I and l
            Assert.AreEqual(PlayerNameRules.UniqueKey("Rami"), PlayerNameRules.UniqueKey("Rarni")); // m and rn
            Assert.AreEqual(PlayerNameRules.UniqueKey("José"), PlayerNameRules.UniqueKey("Jose"));
            Assert.AreEqual(PlayerNameRules.UniqueKey("Ali_77"), PlayerNameRules.UniqueKey("Ali77"));
            Assert.AreNotEqual(PlayerNameRules.UniqueKey("Ali77"), PlayerNameRules.UniqueKey("Ali78"));
        }

        // ---- blocklists ---------------------------------------------------------------------------------------

        [Test]
        public void Every_language_has_a_list_with_every_category()
        {
            string dir = Path.Combine(TestData.Resources(), "Names");
            foreach (var l in Langs)
            {
                var f = JsonSerializer.Deserialize<BlocklistFile>(File.ReadAllText(Path.Combine(dir, "blocklist_" + l + ".json")), TestData.Opt);
                Assert.AreEqual(l, f.lang);
                foreach (var cat in f.Categories())
                    Assert.Greater(cat.Value.terms.Length + cat.Value.words.Length + cat.Value.exact.Length, 2, l + " " + cat.Key);
            }
        }

        [Test]
        public void Every_blocked_term_blocks_a_name_made_of_it()
        {
            string dir = Path.Combine(TestData.Resources(), "Names");
            int tested = 0;
            foreach (var l in Langs)
            {
                var f = JsonSerializer.Deserialize<BlocklistFile>(File.ReadAllText(Path.Combine(dir, "blocklist_" + l + ".json")), TestData.Opt);
                foreach (var cat in f.Categories())
                    foreach (var t in cat.Value.terms.Concat(cat.Value.words).Concat(cat.Value.exact))
                    {
                        string key = PlayerNameRules.Key(t);
                        if (key.Length < 2) continue;
                        Assert.IsTrue(Checker().Blocked(t, out _, out _), l + " " + cat.Key + " " + t);
                        tested++;
                    }
            }
            Assert.Greater(tested, 600);
        }

        [Test]
        public void Profanity_in_each_language_is_refused()
        {
            var samples = new Dictionary<string, string[]>
            {
                ["en"] = new[] { "fuckyou", "Big_Shit", "bitch99", "asshole", "Cunt" },
                ["ar"] = new[] { "شرموط", "ابن الكلب", "ولد الزنا", "كسمك", "ديوث" },
                ["tr"] = new[] { "orospu", "siktir", "yarrak", "pezevenk", "gotveren" },
                ["de"] = new[] { "Arschloch", "Hurensohn", "wichser", "Scheisse", "Fotze" },
                ["es"] = new[] { "hijodeputa", "pendejo", "cabron", "gilipollas", "mierda" },
                ["pt"] = new[] { "caralho", "filhodaputa", "buceta", "arrombado", "merda" },
                ["id"] = new[] { "anjing", "bajingan", "kontol", "ngentot", "bangsat" },
                ["ms"] = new[] { "pukimak", "cibai", "sundal", "keparat", "bangsat" },
                ["fr"] = new[] { "putain", "connard", "salope", "enculer", "merde" },
                ["ur"] = new[] { "حرامزادہ", "مادرچود", "kutta", "chutiya", "haramzada" },
                ["hi"] = new[] { "madarchod", "behenchod", "bhosdike", "gandu", "chutiya" },
                ["ja"] = new[] { "chikusho", "bakayaro", "kisama", "chinko", "manko" },
            };
            foreach (var kv in samples) foreach (var n in kv.Value)
            {
                var v = Checker().Check(n);
                Assert.IsFalse(v.Ok, kv.Key + " " + n);
            }
        }

        [Test]
        public void Insults_hate_sexual_and_figures_are_refused()
        {
            foreach (var n in new[] { "idiot_boy", "dumbass", "غبي_جدا", "احمق", "aptal", "vollidiot", "imbecil", "bodoh", "bewakoof", "bakayarou" }) Bad(n, NameCode.Blocked);                 // insults
            foreach (var n in new[] { "nigger", "faggot", "رافضي", "روافض", "صفوي", "kanake", "bougnoule", "sudaca", "gavur", "zigeuner", "keling", "chamar" }) Bad(n, NameCode.Blocked);   // hate
            foreach (var n in new[] { "pornhub", "سكس", "blowjob", "sexo", "porno", "bokep", "chudai", "oppai", "pedophile", "vergewaltiger" }) Bad(n, NameCode.Blocked);               // sexual
            foreach (var n in new[] { "hitler", "Adolf Hitler", "هتلر", "داعش", "ترامب", "putin", "Jesus", "المسيح", "الله", "Allah", "satan", "الشيطان", "binladen", "erdogan", "اردوغان", "macron", "bolsonaro" }) Bad(n, NameCode.Blocked); // figures
        }

        [Test]
        public void Leetspeak_and_repeats_do_not_get_round_the_lists()
        {
            foreach (var n in new[] { "FuCk", "fuuuuck", "fuuck_u", "sh1t", "b1tch", "a55hole", "p0rn", "s3x", "sex_69", "sex1234",
                "n1gger", "f4gg0t", "h1tl3r", "h_itler", "k0ntol", "4njing", "p3ndejo", "m3rd4", "m3rde", "put4in",
                "ش_رموط", "شـــرموط", "ه تلر", "هـ تلر" })
                Bad(n, NameCode.Blocked);
        }

        [Test]
        public void Symbol_look_alikes_are_caught_by_the_lists_too()
        {
            // $ ! @ are not allowed in a name (bad_char), but the lists still see through them.
            foreach (var n in new[] { "$h!t", "b!tch", "@sshole", "n!gga", "p0rn", "h!tler", "sh i t", "f u c k" }) Assert.IsTrue(Checker().Blocked(n, out _, out _), n);
            Bad("$h!t", NameCode.BadChar);
        }

        [Test]
        public void Spacing_tricks_are_caught_by_the_lists_even_when_the_shape_rules_would_allow_them()
        {
            // The shape rules allow one separator in total, so these are refused for their shape first.
            foreach (var n in new[] { "f u c k", "f_u_c_k", "h i t l e r", "c u n t", "ش ر م و ط" }) Bad(n, NameCode.Separators);
            // The word lists catch the same tricks on their own (used by the server for reports and by future free-text places).
            foreach (var n in new[] { "f u c k", "f_u_c_k", "h i t l e r", "c u n t", "ش ر م و ط", "ه ت ل ر", "s  e  x" })
                Assert.IsTrue(Checker().Blocked(n, out _, out _), n);
        }

        [Test]
        public void Short_blocked_words_do_not_catch_innocent_names()
        {
            foreach (var n in new[] { "Essex", "Sussex", "Assam", "Cassandra", "Dickens", "Cockburn", "Classic", "Passion", "Bassem", "Assad", "Massimo", "Pussycat".Replace("Pussycat", "Pusan"), "Analisa", "Cumin", "Cumberland", "Trumpet", "Hamasa" })
                Ok(n);
        }

        [Test]
        public void Whole_word_terms_match_a_part_but_not_inside_a_word()
        {
            Bad("sex_king", NameCode.Blocked);
            Bad("king_sex", NameCode.Blocked);
            Bad("Sex", NameCode.Blocked);
            Ok("Sexton");
            Ok("Essex");
            Bad("عبد", NameCode.Blocked);          // alone
            Ok("عبد الله");                         // as a first part of a normal name
            Ok("عبد العزيز".Replace("العزيز", "عزيز"));
        }

        // ---- reserved -----------------------------------------------------------------------------------------

        [Test]
        public void Game_and_staff_names_are_reserved()
        {
            foreach (var n in new[] { "Aron", "ARON", "aron99", "4ron", "Ar0n", "A r o n", "أرون", "ارون", "أرون٢٠", "Noura", "N0ura", "نورة", "نوره", "Duhaim", "دهيم", "Dr Duhaim", "دكتور دهيم", "د. دهيم".Replace("د. دهيم", "دكتوردهيم"),
                "البرق", "غزيّل".Replace("ّ", ""), "سهم", "أصيل", "بيضا", "Bayda", "Admin", "Adm1n", "4dmin", "moderator", "Moderator_1", "official", "SupportTeam", "Support_01", "الدعم", "المشرف", "الادارة", "الإدارة", "رسمي", "مشرف", "مدير", "نظام", "مطور", "رحلة أرون", "AronOfficial" })
            {
                var v = Checker().Check(n);
                Assert.IsFalse(v.Ok, n);
                Assert.IsTrue(v.Code == NameCode.Reserved || v.Code == NameCode.Blocked || v.Code == NameCode.Separators || v.Code == NameCode.TooShort, n + " " + v.Code);
            }
            foreach (var n in new[] { "Aron", "أرون", "Noura", "نورة", "دهيم", "Duhaim", "Admin", "Moderator", "الدعم", "المشرف", "بيضا", "البرق", "سهم", "أصيل" })
                Bad(n, NameCode.Reserved);
        }

        [Test]
        public void Companion_names_are_reserved_in_the_json_and_in_the_companion_file()
        {
            var comp = File.ReadAllText(Path.Combine(TestData.Resources(), "Companions", "companion_names.json"));
            Assert.IsTrue(comp.Contains("noura_oryx"));
            Bad("بيضا", NameCode.Reserved);
        }

        [Test]
        public void Names_close_to_reserved_ones_are_fine()
        {
            Ok("Aaron");
            Ok("Aronia");
            Ok("Noura_Ali");
            Ok("نورة_٩٩".Replace("_٩٩", "ه_٩٩").Replace("نورةه", "نوريه"));
        }

        // ---- discriminator ------------------------------------------------------------------------------------

        [Test]
        public void Discriminators_are_four_digits_and_do_not_repeat()
        {
            for (int seed = 0; seed < 50; seed++)
            {
                var seen = new HashSet<int>();
                for (int i = 0; i < NameDiscriminator.Attempts; i++)
                {
                    int t = NameDiscriminator.Candidate(seed * 7919, i);
                    Assert.IsTrue(NameDiscriminator.IsAllowed(t), t.ToString());
                    Assert.IsTrue(seen.Add(t), "repeat " + t);
                }
            }
            Assert.AreEqual("Ahmed#1000", NameRegistry.Display("Ahmed", 1000));
            Assert.AreEqual("Ahmed#1234", NameRegistry.Display("Ahmed", 1234));
            Assert.AreEqual("Ahmed", NameRegistry.Display("Ahmed", 0));
        }

        // ---- registry -----------------------------------------------------------------------------------------

        private class Clock { public long Now = new System.DateTime(2026, 10, 27, 12, 0, 0, System.DateTimeKind.Utc).Ticks; }

        private static NameRegistry Registry(out InMemoryNameStore store, out Clock clock, int seed = 1234)
        {
            store = new InMemoryNameStore();
            var c = clock = new Clock();
            return new NameRegistry(Checker(), store, () => c.Now, () => seed);
        }

        [Test]
        public void Reserve_gives_a_unique_name_to_one_player()
        {
            var reg = Registry(out var store, out _);
            var r = reg.Reserve("p1", "Ahmed", false);
            Assert.AreEqual(NameStatus.Ok, r.Status);
            Assert.AreEqual("Ahmed", r.Display);
            var r2 = reg.Reserve("p2", "AHMED", false);
            Assert.AreEqual(NameStatus.Taken, r2.Status);
            Assert.AreEqual(NameStatus.Taken, reg.Reserve("p2", "Ahm3d", false).Status);
            Assert.AreEqual(NameStatus.Taken, reg.Reserve("p2", "A_hmed".Replace("_", ""), false).Status);
            Assert.IsFalse(reg.Check("p2", "ahmed").Available);
            Assert.IsTrue(reg.Check("p2", "Ahmed2").Available);
        }

        [Test]
        public void Arabic_spelling_variants_are_the_same_name()
        {
            var reg = Registry(out _, out _);
            Assert.AreEqual(NameStatus.Ok, reg.Reserve("p1", "أحمد", false).Status);
            Assert.AreEqual(NameStatus.Taken, reg.Reserve("p2", "احمد", false).Status);
            Assert.AreEqual(NameStatus.Taken, reg.Reserve("p3", "أحـمد", false).Status);
        }

        [Test]
        public void A_taken_name_gets_a_number_when_allowed()
        {
            var reg = Registry(out var store, out _);
            reg.Reserve("p1", "أحمد", true);
            var r = reg.Reserve("p2", "أحمد", true);
            Assert.AreEqual(NameStatus.Ok, r.Status);
            Assert.GreaterOrEqual(r.Tag, 1000);
            Assert.LessOrEqual(r.Tag, 9999);
            Assert.AreEqual("أحمد#" + r.Tag, r.Display);
            var r3 = reg.Reserve("p3", "أحمد", true);
            Assert.AreEqual(NameStatus.Ok, r3.Status);
            Assert.AreNotEqual(r.Tag, r3.Tag);
            Assert.AreEqual(r.Display, reg.DisplayName("p2"));
        }

        [Test]
        public void The_number_runs_out_politely()
        {
            // a store where every numbered slot is already held
            var store = new FullStore();
            var reg = new NameRegistry(Checker(), store, () => 0, () => 5);
            var r = reg.Reserve("p9", "Ahmed", true);
            Assert.AreEqual(NameStatus.Taken, r.Status);
        }

        private class FullStore : InMemoryNameStore
        {
            public override bool TryClaim(string slot, string playerId) => false;
        }

        [Test]
        public void The_server_checks_again_and_refuses_what_the_client_should_have_caught()
        {
            var reg = Registry(out _, out _);
            Assert.AreEqual(NameStatus.Blocked, reg.Reserve("p1", "hitler", true).Status);
            Assert.AreEqual(NameStatus.Reserved, reg.Reserve("p1", "Aron", true).Status);
            Assert.AreEqual(NameStatus.Invalid, reg.Reserve("p1", "Ah\u200Bmed", true).Status);
            Assert.AreEqual(NameStatus.Invalid, reg.Reserve("p1", "ab", true).Status);
            Assert.AreEqual(NameStatus.Error, reg.Reserve("", "Ahmed", true).Status);
            Assert.IsNull(reg.DisplayName("p1"));
        }

        [Test]
        public void Changing_a_name_frees_the_old_one_after_the_cooldown()
        {
            var reg = Registry(out var store, out var clock);
            Assert.AreEqual(NameStatus.Ok, reg.Reserve("p1", "Ahmed", false).Status);
            Assert.AreEqual(NameStatus.Cooldown, reg.Change("p1", "Fahad", false).Status);
            clock.Now += System.TimeSpan.TicksPerHour * 23;
            Assert.AreEqual(NameStatus.Cooldown, reg.Change("p1", "Fahad", false).Status);
            clock.Now += System.TimeSpan.TicksPerHour * 2;
            Assert.AreEqual(NameStatus.Ok, reg.Change("p1", "Fahad", false).Status);
            Assert.AreEqual("Fahad", reg.DisplayName("p1"));
            Assert.AreEqual(NameStatus.Ok, reg.Reserve("p2", "Ahmed", false).Status); // freed
            Assert.AreEqual(NameStatus.Taken, reg.Reserve("p3", "Fahad", false).Status);
        }

        [Test]
        public void Asking_for_the_same_name_again_is_fine()
        {
            var reg = Registry(out _, out _);
            reg.Reserve("p1", "Ahmed", false);
            var again = reg.Reserve("p1", "Ahmed", false);
            Assert.AreEqual(NameStatus.Ok, again.Status);
            Assert.AreEqual("Ahmed", again.Display);
        }

        [Test]
        public void A_failed_change_keeps_the_old_name()
        {
            var reg = Registry(out _, out var clock);
            reg.Reserve("p1", "Ahmed", false);
            reg.Reserve("p2", "Fahad", false);
            clock.Now += System.TimeSpan.TicksPerDay * 2;
            Assert.AreEqual(NameStatus.Taken, reg.Change("p1", "Fahad", false).Status);
            Assert.AreEqual("Ahmed", reg.DisplayName("p1"));
        }

        [Test]
        public void Reserving_is_rate_limited()
        {
            var reg = Registry(out _, out var clock);
            NameStatus last = NameStatus.Ok;
            for (int i = 0; i < NameRegistry.ReservesPerMinute + 3; i++) last = reg.Reserve("p1", "Zz" + (char)('a' + i) + "x", true).Status;
            Assert.AreEqual(NameStatus.RateLimited, last);
            clock.Now += System.TimeSpan.TicksPerMinute * 2;
            Assert.AreNotEqual(NameStatus.RateLimited, reg.Check("p1", "Zzzz").Status);
        }

        [Test]
        public void Checking_is_rate_limited()
        {
            var reg = Registry(out _, out _);
            NameStatus last = NameStatus.Ok;
            for (int i = 0; i < NameRegistry.ChecksPerMinute + 2; i++) last = reg.Check("p1", "Ahmed").Status;
            Assert.AreEqual(NameStatus.RateLimited, last);
        }

        [Test]
        public void Three_different_reporters_force_a_rename()
        {
            var reg = Registry(out var store, out var clock);
            reg.Reserve("t", "Fahad", false);
            Assert.AreEqual(NameStatus.Ok, reg.Report("a", "t", ReportReasons.Offensive).Status);
            Assert.AreEqual(NameStatus.Ok, reg.Report("a", "t", ReportReasons.Offensive).Status); // same reporter again does not count twice
            Assert.AreEqual("Fahad", reg.DisplayName("t"));
            Assert.IsFalse(reg.Report("b", "t", ReportReasons.RealName).MustRename);
            var third = reg.Report("c", "t", ReportReasons.Impersonation);
            Assert.IsTrue(third.MustRename);
            Assert.IsTrue(reg.DisplayName("t").StartsWith("Racer"));
            Assert.AreEqual(NameStatus.Ok, reg.Reserve("x", "Fahad", false).Status); // the name is free again
            // the reported player can pick again straight away (no cooldown)
            Assert.AreEqual(NameStatus.Ok, reg.Reserve("t", "Faisal", true).Status);
            Assert.AreEqual("Faisal", reg.DisplayName("t"));
        }

        [Test]
        public void Reports_are_checked_and_limited()
        {
            var reg = Registry(out _, out _);
            reg.Reserve("t", "Fahad", false);
            Assert.AreEqual(NameStatus.Error, reg.Report("a", "a", ReportReasons.Offensive).Status);
            Assert.AreEqual(NameStatus.Error, reg.Report("a", "t", "because").Status);
            Assert.AreEqual(NameStatus.NotFound, reg.Report("a", "nobody", ReportReasons.Other).Status);
            NameStatus last = NameStatus.Ok;
            for (int i = 0; i < NameRegistry.ReportsPerDay + 2; i++) last = reg.Report("spam", "t", ReportReasons.Other).Status;
            Assert.AreEqual(NameStatus.RateLimited, last);
        }

        // ---- the client service -------------------------------------------------------------------------------

        [Test]
        public void Client_refuses_bad_names_without_asking_the_server()
        {
            var svc = new PlayerNameService(Checker(), new Boom());
            Assert.AreEqual(NameStatus.Blocked, svc.CheckAsync("hitler").Result.Status);
            Assert.AreEqual(NameStatus.Invalid, svc.ReserveAsync("a\u200Bb", true).Result.Status);
        }

        private class Boom : IPlayerNameService
        {
            public Task<NameResult> CheckAsync(string n) => throw new System.Exception("offline");
            public Task<NameResult> ReserveAsync(string n, bool t) => throw new System.Exception("offline");
            public Task<NameResult> ChangeAsync(string n, bool t) => throw new System.Exception("offline");
            public Task<NameResult> ReportAsync(string t, string r) => throw new System.Exception("offline");
        }

        [Test]
        public void With_no_server_the_name_is_kept_on_the_device()
        {
            var svc = new PlayerNameService(Checker(), null);
            var r = svc.ReserveAsync("Ahmed", true).Result;
            Assert.AreEqual(NameStatus.Offline, r.Status);
            Assert.AreEqual("Ahmed", r.Name);
            Assert.AreEqual(NameStatus.Offline, new PlayerNameService(Checker(), new Boom()).ChangeAsync("Ahmed", true).Result.Status);
            Assert.AreEqual(NameStatus.Offline, svc.ReportAsync("x", ReportReasons.Other).Result.Status);
        }

        [Test]
        public void Client_talks_to_the_registry_through_the_interface()
        {
            var reg = Registry(out _, out _);
            var a = new PlayerNameService(Checker(), new RegistryNameService(reg, "p1"));
            var b = new PlayerNameService(Checker(), new RegistryNameService(reg, "p2"));
            Assert.AreEqual(NameStatus.Ok, a.ReserveAsync("Ahmed", false).Result.Status);
            Assert.AreEqual(NameStatus.Taken, b.ReserveAsync("Ahmed", false).Result.Status);
            var tagged = b.ReserveAsync("Ahmed", true).Result;
            Assert.AreEqual(NameStatus.Ok, tagged.Status);
            Assert.IsTrue(tagged.Tag >= 1000);
            Assert.AreEqual(NameStatus.Ok, a.ReportAsync("p2", ReportReasons.Offensive).Result.Status);
        }

        // ---- children, safe names -----------------------------------------------------------------------------

        [Test]
        public void Children_and_unknown_ages_pick_a_name_teens_and_adults_type()
        {
            Assert.AreEqual(NameFlow.Mode.Pick, NameFlow.ModeFor("child"));
            Assert.AreEqual(NameFlow.Mode.Pick, NameFlow.ModeFor(""));
            Assert.AreEqual(NameFlow.Mode.Pick, NameFlow.ModeFor(null));
            Assert.AreEqual(NameFlow.Mode.Type, NameFlow.ModeFor("teen"));
            Assert.AreEqual(NameFlow.Mode.Type, NameFlow.ModeFor("adult"));
            Assert.IsFalse(NameFlow.CanRaceOnline(false, false));
            Assert.IsFalse(NameFlow.CanRaceOnline(true, true));
            Assert.IsTrue(NameFlow.CanRaceOnline(true, false));
            Assert.IsTrue(NameFlow.CooldownPassed(0, 5));
            Assert.IsFalse(NameFlow.CooldownPassed(100, 100 + System.TimeSpan.TicksPerHour));
            Assert.IsTrue(NameFlow.CooldownPassed(100, 100 + System.TimeSpan.TicksPerHour * 25));
        }

        [Test]
        public void Every_safe_name_passes_the_checker_and_fits()
        {
            foreach (var lang in Langs)
            {
                var adj = SafeNames.UsesArabic(lang) ? SafeNames.AdjectivesAr : SafeNames.AdjectivesEn;
                var ani = SafeNames.UsesArabic(lang) ? SafeNames.AnimalsAr : SafeNames.AnimalsEn;
                for (int i = 0; i < adj.Length * ani.Length; i++)
                {
                    string n = SafeNames.Make(0, i, lang);
                    var v = Checker().Check(n);
                    Assert.IsTrue(v.Ok, lang + " " + n + " " + v.Code + " " + v.Match);
                    Assert.LessOrEqual(n.Length, PlayerNameRules.Max, n);
                }
            }
        }

        [Test]
        public void Safe_name_options_are_different_from_each_other()
        {
            foreach (var lang in new[] { "ar", "en", "ur", "fr" })
                for (int seed = 0; seed < 40; seed++)
                    Assert.AreEqual(5, SafeNames.Options(seed * 31, lang, 5).Distinct().Count(), lang + seed);
        }

        [Test]
        public void The_lobby_filter_uses_the_same_rules()
        {
            Aron.Logic.Online.NameFilter.Checker = Checker();
            try
            {
                Assert.IsTrue(Aron.Logic.Online.NameFilter.Allowed("Ahmed"));
                Assert.IsFalse(Aron.Logic.Online.NameFilter.Allowed("h1tler"));
                Assert.IsFalse(Aron.Logic.Online.NameFilter.Allowed("Aron"));
                Assert.IsTrue(Checker().Check(Aron.Logic.Online.NameFilter.Default("player-1")).Ok);
            }
            finally { Aron.Logic.Online.NameFilter.Checker = null; }
        }

        [Test]
        public void Reserved_file_lists_the_companions_and_staff_words()
        {
            var r = JsonSerializer.Deserialize<ReservedFile>(File.ReadAllText(Path.Combine(TestData.Resources(), "Names", "reserved.json")), TestData.Opt);
            foreach (var w in new[] { "Aron", "أرون", "Noura", "نورة", "دهيم", "البرق", "سهم", "أصيل", "بيضا", "Admin", "Moderator", "Official", "Support", "الدعم", "المشرف" })
                Assert.IsTrue(r.exact.Contains(w) || r.contains.Contains(w.ToLowerInvariant()) , w);
        }

        // ---- text, save, docs ---------------------------------------------------------------------------------

        private static readonly string[] UiKeys =
        {
            "name_title", "name_pick_title", "name_hint", "name_placeholder", "name_confirm", "name_reroll", "name_row", "name_must_change", "name_report",
            "name_report_title", "name_report_sent", "name_report_offline", "report_offensive", "report_impersonation", "report_real_name", "report_other",
            "age_title", "age_note", "name_err_offline_saved",
        };

        [Test]
        public void Every_name_text_exists_in_all_twelve_languages()
        {
            var keys = UiKeys.Concat(NameCode.All.Select(c => "name_err_" + c)).ToList();
            Assert.AreEqual(12, Aron.Logic.Localization.UiLanguages.Codes.Count());
            foreach (var l in Aron.Logic.Localization.UiLanguages.Codes)
                foreach (var k in keys)
                {
                    Assert.IsTrue(Aron.Logic.Localization.UiStringTables.All[l].TryGetValue(k, out var v), l + " " + k);
                    Assert.IsNotEmpty(v, l + " " + k);
                }
            foreach (var l in Aron.Logic.Localization.UiLanguages.Codes.Where(l => l != "ar"))
                Assert.AreNotEqual(Aron.Logic.Localization.UiStringTables.All["ar"]["name_hint"], Aron.Logic.Localization.UiStringTables.All[l]["name_hint"], l);
            foreach (var l in Aron.Logic.Localization.UiLanguages.Codes)
            {
                Assert.IsTrue(Aron.Logic.Localization.UiStringTables.All[l]["name_row"].Contains("{0}"), l);
                Assert.IsTrue(Aron.Logic.Localization.UiStringTables.All[l]["name_report_title"].Contains("{0}"), l);
            }
            Assert.AreEqual("لا تكتب اسمك الحقيقي الكامل", Aron.Logic.Localization.UiStringTables.All["ar"]["name_hint"]);
        }

        [Test]
        public void Report_reasons_have_a_text_each()
        {
            foreach (var l in Aron.Logic.Localization.UiLanguages.Codes)
                foreach (var r in ReportReasons.All)
                    Assert.IsNotEmpty(Aron.Logic.Localization.UiStringTables.All[l]["report_" + r], l + r);
        }


        // ---- Cloud Save storage logic (write locks) ----------------------------------------------------------

        private static NameRegistry CloudRegistry(InMemoryVersionedKv kv, int seed = 7)
        {
            CloudNameStore.InitShards(kv);
            return new NameRegistry(Checker(), new CloudNameStore(kv), () => 0, () => seed);
        }

        [Test]
        public void Cloud_store_needs_its_shards_before_the_first_claim()
        {
            var store = new CloudNameStore(new InMemoryVersionedKv());
            Assert.Throws<System.InvalidOperationException>(() => store.TryClaim("ahmed", "p1"));
            var kv = new InMemoryVersionedKv();
            Assert.AreEqual(CloudNameStore.Shards, CloudNameStore.InitShards(kv));
            Assert.AreEqual(0, CloudNameStore.InitShards(kv));
        }

        [Test]
        public void Cloud_store_gives_a_name_to_one_player_only()
        {
            var kv = new InMemoryVersionedKv();
            var reg = CloudRegistry(kv);
            var a = reg.Reserve("p1", "Ahmed", true);
            var b = reg.Reserve("p2", "ahmed", true);
            Assert.IsTrue(a.Ok); Assert.AreEqual(0, a.Tag);
            Assert.IsTrue(b.Ok); Assert.Greater(b.Tag, 0);
            Assert.AreEqual(NameStatus.Taken, reg.Reserve("p3", "Ahmed", false).Status);
            Assert.AreEqual("Ahmed", reg.DisplayName("p1"));
            Assert.AreEqual(b.Display, reg.DisplayName("p2"));
        }

        [Test]
        public void Cloud_store_survives_two_players_claiming_at_the_same_moment()
        {
            var kv = new InMemoryVersionedKv();
            var store = new CloudNameStore(kv);
            CloudNameStore.InitShards(kv);
            bool once = false;
            // while player one is between its read and its write, player two claims the same slot
            kv.BeforeSet = key =>
            {
                if (once || !key.StartsWith("s")) return;
                once = true;
                Assert.IsTrue(store.TryClaim("ahmed", "p2"));
            };
            bool first = store.TryClaim("ahmed", "p1");
            kv.BeforeSet = null;
            Assert.IsFalse(first, "the later write must lose, not overwrite");
            Assert.AreEqual("p2", store.Owner("ahmed"));
            Assert.GreaterOrEqual(kv.Conflicts, 1);
        }

        [Test]
        public void Cloud_store_retries_a_conflict_on_a_different_slot_in_the_same_shard()
        {
            var kv = new InMemoryVersionedKv();
            var store = new CloudNameStore(kv);
            CloudNameStore.InitShards(kv);
            // find two slots that share a shard
            string a = "slot0", b = null;
            int shard = (int)(SafeNames.StableHash(a) % CloudNameStore.Shards);
            for (int i = 1; i < 5000 && b == null; i++) if ((int)(SafeNames.StableHash("slot" + i) % CloudNameStore.Shards) == shard) b = "slot" + i;
            Assert.IsNotNull(b);
            bool once = false;
            kv.BeforeSet = key => { if (!once && key.StartsWith("s")) { once = true; Assert.IsTrue(store.TryClaim(b, "p2")); } };
            Assert.IsTrue(store.TryClaim(a, "p1"));
            kv.BeforeSet = null;
            Assert.AreEqual("p1", store.Owner(a));
            Assert.AreEqual("p2", store.Owner(b));
        }

        [Test]
        public void Cloud_store_counts_in_windows_and_remembers_reports()
        {
            var store = new CloudNameStore(new InMemoryVersionedKv());
            Assert.AreEqual(1, store.Bump("check:p1", 5));
            Assert.AreEqual(2, store.Bump("check:p1", 5));
            Assert.AreEqual(1, store.Bump("check:p1", 6));
            Assert.AreEqual(1, store.AddReport("t1", "r1"));
            Assert.AreEqual(-1, store.AddReport("t1", "r1"));
            Assert.AreEqual(2, store.AddReport("t1", "r2"));
        }

        [Test]
        public void Cloud_store_keeps_the_player_record_and_releases_a_name_on_change()
        {
            var kv = new InMemoryVersionedKv();
            var reg = CloudRegistry(kv);
            Assert.IsTrue(reg.Reserve("p1", "Ahmed", true).Ok);
            var rec = new CloudNameStore(kv).GetRecord("p1");
            Assert.AreEqual("Ahmed", rec.name);
            Assert.IsFalse(rec.mustRename);
            long day = System.TimeSpan.TicksPerDay + 1;
            var reg2 = new NameRegistry(Checker(), new CloudNameStore(kv), () => day * 2, () => 7);
            Assert.IsTrue(reg2.Change("p1", "Saad", true).Ok);
            Assert.IsTrue(reg2.Reserve("p2", "Ahmed", false).Ok, "the old name is free again");
        }

        [Test]
        public void Cloud_keys_are_safe_for_cloud_save()
        {
            StringAssert.DoesNotContain(":", CloudNameStore.SafeKey("check:p1"));
            Assert.AreEqual("check_p1", CloudNameStore.SafeKey("check:p1"));
        }

        [Test]
        public void The_save_keeps_only_a_name_and_an_age_band()
        {
            var d = new SaveData();
            Assert.AreEqual("", d.playerName);
            Assert.AreEqual("", d.ageBand);
            Assert.AreEqual(0, d.playerNameTag);
            Assert.IsFalse(d.nameReserved);
            Assert.IsFalse(d.nameMustChange);
            var json = JsonSerializer.Serialize(d, TestData.Opt);
            Assert.IsFalse(json.Contains("birth", System.StringComparison.OrdinalIgnoreCase));
            Assert.IsFalse(Aron.Logic.Safety.AgeGate.CanTypeName(Aron.Logic.Safety.AgeGate.Band(2020, 2026)));
            Assert.IsTrue(Aron.Logic.Safety.AgeGate.CanTypeName(Aron.Logic.Safety.AgeGate.Band(2010, 2026)));
            Assert.IsTrue(Aron.Logic.Safety.AgeGate.CanTypeName(Aron.Logic.Safety.AgeGate.Band(1990, 2026)));
        }

        [Test]
        public void The_privacy_policy_and_rating_notes_describe_typed_names()
        {
            string repo = Path.GetFullPath(Path.Combine(TestData.Resources(), "..", "..", ".."));
            string privacy = File.ReadAllText(Path.Combine(repo, "docs", "privacy_policy.md"));
            string iarc = File.ReadAllText(Path.Combine(repo, "docs", "iarc_draft.md"));
            Assert.IsFalse(privacy.Contains("children cannot type names"));
            Assert.IsFalse(iarc.Contains("children cannot type names"));
            StringAssert.Contains("player name", privacy.ToLowerInvariant());
            StringAssert.Contains("player name", iarc.ToLowerInvariant());
            Assert.IsTrue(File.Exists(Path.Combine(repo, "docs", "player_names.md")));
            Assert.IsTrue(File.Exists(Path.Combine(repo, "cloud", "AronNames", "README.md")));
        }
    }
}
