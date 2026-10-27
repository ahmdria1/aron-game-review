// (c) Ahmed Alamrani. All rights reserved.
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using Aron.Logic.Localization;
using Aron.Logic.Online;
using Aron.Logic.Races;
using NUnit.Framework;

namespace Aron.Logic.Tests
{
    /// <summary>Computer racers (offline and online fill) and the 35 recurring rivals with their barks.</summary>
    public class AiRivalTests
    {
        private static readonly JsonSerializerOptions Opt = new JsonSerializerOptions { IncludeFields = true };

        private static string Resources()
        {
            for (var d = new DirectoryInfo(TestContext.CurrentContext.TestDirectory); d != null; d = d.Parent)
            {
                var p = Path.Combine(d.FullName, "Assets", "_Aron", "Resources");
                if (Directory.Exists(Path.Combine(p, "Rivals"))) return p;
            }
            Assert.Fail("Resources not found");
            return null;
        }

        private static T Read<T>(params string[] parts) => JsonSerializer.Deserialize<T>(File.ReadAllText(Path.Combine(new[] { Resources() }.Concat(parts).ToArray())), Opt);
        private static RivalFile Rivals() => Read<RivalFile>("Rivals", "rivals.json");
        private static AiRacerFile Cfg() => Read<AiRacerFile>("Races", "ai_racers.json");
        private static MountFile Mounts() => Read<MountFile>("Mounts", "race_mounts.json");
        private static PointsTable Table() => Read<PointsTable>("Mounts", "race_points.json");
        private static string[] Countries() => Directory.GetFiles(Path.Combine(Resources(), "Culture"), "*.json").Select(f => Path.GetFileNameWithoutExtension(f)).OrderBy(x => x).ToArray();

        // ---------------------------------------------------------------- rivals

        [Test]
        public void EveryCountryHasOneRivalAndTheFileIsValid()
        {
            var f = Rivals();
            Assert.That(Countries().Length, Is.EqualTo(35));
            Assert.That(f.rivals.Length, Is.EqualTo(35));
            Assert.That(f.Validate(Countries()), Is.Empty);
            foreach (var c in Countries()) Assert.That(f.ForCountry(c), Is.Not.Null, c);
        }

        [Test]
        public void EachRivalHasSixToTenLinesAndNoNameIsUsedTwice()
        {
            var f = Rivals();
            foreach (var r in f.rivals) Assert.That(r.lines.Length, Is.InRange(6, 10), r.id);
            Assert.That(f.rivals.Select(r => r.nameEn.ToLowerInvariant()).Distinct().Count(), Is.EqualTo(35));
            Assert.That(f.rivals.Select(r => r.nameAr).Distinct().Count(), Is.EqualTo(35));
            Assert.That(f.rivals.SelectMany(r => r.lines).Select(l => l.id).Distinct().Count(), Is.EqualTo(f.rivals.Sum(r => r.lines.Length)));
        }

        [Test]
        public void ArabCountriesSpeakTheirOwnDialectAndTheirTagSaysSo()
        {
            var f = Rivals();
            var dialects = new Dictionary<string, string>
            {
                ["egypt"] = "egyptian", ["morocco"] = "darija", ["iraq"] = "iraqi", ["syria"] = "levantine", ["lebanon"] = "levantine",
                ["kuwait"] = "kuwaiti", ["saudi"] = "najdi", ["sudan"] = "sudanese", ["tunisia"] = "tunisian", ["algeria"] = "algerian",
            };
            foreach (var kv in dialects)
                Assert.That(f.ForCountry(kv.Key).lines.All(l => l.lang == "ar" && l.dialect == kv.Value), Is.True, kv.Key);
            // The dialect lines really differ from each other (not one text copied everywhere).
            Assert.That(f.ForCountry("egypt").Line("overtaken").text, Is.Not.EqualTo(f.ForCountry("iraq").Line("overtaken").text));
            Assert.That(f.ForCountry("kuwait").Line("overtaken").text, Is.Not.EqualTo(f.ForCountry("morocco").Line("overtaken").text));
        }

        [Test]
        public void NonArabicLinesAreInTheCountrysLanguageAndFlaggedForNativeReview()
        {
            var f = Rivals();
            var langs = new Dictionary<string, string> { ["japan"] = "ja", ["turkey"] = "tr", ["germany"] = "de", ["india"] = "hi", ["pakistan"] = "ur", ["brazil"] = "pt", ["mexico"] = "es", ["indonesia"] = "id", ["malaysia"] = "ms", ["france"] = "fr", ["uzbekistan"] = "uz" };
            foreach (var kv in langs)
            {
                var r = f.ForCountry(kv.Key);
                Assert.That(r.lines.All(l => l.lang == kv.Value && l.review == "native"), Is.True, kv.Key);
            }
            foreach (var r in f.rivals)
                foreach (var l in r.lines)
                {
                    if (l.lang == "ar") Assert.That(l.review, Is.EqualTo("dialect"), l.id);
                    else if (l.lang == "en") Assert.That(l.review, Is.EqualTo("none"), l.id);
                    else Assert.That(l.review, Is.EqualTo("native"), l.id);
                }
        }

        [Test]
        public void EverySubtitleExistsInAll12UiLanguagesAndArabicIsAlwaysFirst()
        {
            var f = Rivals();
            var keys = f.rivals.SelectMany(r => r.lines).Select(l => l.subtitleKey).Distinct().ToList();
            Assert.That(keys.Count, Is.EqualTo(8));
            foreach (var lang in UiLanguages.Codes)
                foreach (var k in keys) Assert.That(UiLanguages.Get(lang, k), Is.Not.Null.And.Not.Empty.And.Not.EqualTo(k), lang + "/" + k);
            var line = f.ForCountry("japan").Line("win");
            var de = RivalSubtitles.For(line, "de", true, k => UiLanguages.Get("de", k));
            Assert.That(de.Length, Is.EqualTo(2));
            Assert.That(de[0], Is.EqualTo(line.subtitleAr));
            Assert.That(de[1], Is.EqualTo(UiLanguages.Get("de", "rival_bark_win")));
            Assert.That(RivalSubtitles.For(line, "ar", true, k => UiLanguages.Get("ar", k)).Length, Is.EqualTo(1));
            Assert.That(RivalSubtitles.For(line, "de", false, k => UiLanguages.Get("de", k)), Is.Empty, "subtitles off");
        }

        [Test]
        public void RivalsAreModestFamilyFriendlyAndWomenAreFlaggedForOutfitReview()
        {
            var f = Rivals();
            var banned = new[] { "cross", "crescent", "flag", "swastika", "uniform", "military", "weapon", "gun", "bikini", "shorts", "skirt", "cleavage", "sleeveless" };
            foreach (var r in f.rivals)
            {
                Assert.That(r.modest, Is.True, r.id);
                foreach (var b in banned) Assert.That(r.outfit.ToLowerInvariant(), Does.Not.Contain(b), r.id);
                if (r.female) Assert.That(r.outfitReview && (r.outfit.Contains("scarf") || r.outfit.Contains("helmet") || r.outfit.Contains("headscarf") || r.outfit.Contains("hat")), Is.True, r.id);
            }
            Assert.That(f.rivals.Count(r => r.female), Is.GreaterThan(5));
        }

        [Test]
        public void EveryRivalMountIsARealMount()
        {
            var mf = Mounts();
            foreach (var r in Rivals().rivals) Assert.That(mf.Find(r.mount), Is.Not.Null, r.id + " " + r.mount);
            foreach (var m in RivalFile.Mounts) Assert.That(mf.Find(m), Is.Not.Null, m);
        }

        [Test]
        public void ChatterOffIsSilentAndBarksAreRateLimited()
        {
            var f = Rivals(); var egypt = f.ForCountry("egypt"); var iraq = f.ForCountry("iraq");
            var rnd = new Random(1);
            var off = new BarkLimiter(false);
            Assert.That(off.Try(egypt, "start", 0f, rnd), Is.Null);
            Assert.That(off.Try(egypt, "win", 100f, rnd), Is.Null);

            var on = new BarkLimiter(true);
            Assert.That(on.Try(egypt, "start", 0f, rnd), Is.Not.Null);
            Assert.That(on.Try(iraq, "boost", 1f, rnd), Is.Null, "one voice at a time, 4 s apart");
            Assert.That(on.Try(iraq, "boost", 5f, rnd), Is.Not.Null);
            Assert.That(on.Try(egypt, "boost", 9f, rnd), Is.Null, "the same rival waits 14 s");
            Assert.That(on.Try(egypt, "boost", 20f, rnd), Is.Not.Null);
            Assert.That(on.Try(egypt, "win", 21.5f, rnd), Is.Not.Null, "the win line may follow quickly");
        }

        [Test]
        public void BarkLimiterCapsPerRaceAndNeverRepeatsALine()
        {
            var f = Rivals(); var rnd = new Random(7);
            var lim = new BarkLimiter(true);
            var said = new List<string>(); float t = 0f;
            for (int i = 0; i < 200; i++)
            {
                foreach (var r in f.rivals)
                {
                    t += 20f;
                    var l = lim.Try(r, new[] { "overtake", "overtaken", "boost", "crash" }[i % 4], t, rnd);
                    if (l != null) said.Add(l.id);
                }
            }
            Assert.That(said.Count, Is.EqualTo(BarkLimiter.MaxPerRace));
            Assert.That(said.Distinct().Count(), Is.EqualTo(said.Count));
        }

        // ---------------------------------------------------------------- Computer-racer config and driving

        [Test]
        public void AiFileIsValidAndHarderMeansBetterNotFaster()
        {
            var cfg = Cfg();
            Assert.That(cfg.Validate(), Is.Empty);
            var e = cfg.Profile("easy"); var n = cfg.Profile("normal"); var h = cfg.Profile("hard");
            Assert.That(e.mistakesPerMinute, Is.GreaterThan(n.mistakesPerMinute));
            Assert.That(n.mistakesPerMinute, Is.GreaterThan(h.mistakesPerMinute));
            Assert.That(h.skill + h.catchUp, Is.LessThanOrEqualTo(1.0001f), "hard never exceeds the mount's top speed");
        }

        private static (int slips, float maxSpeed, float cap) Drive(string difficulty, string mountId, int seed, float seconds, Func<float, AiContext> ctx = null, MountFile mounts = null)
        {
            var mf = mounts ?? Mounts(); var cfg = Cfg();
            var m = mf.Find(mountId);
            var d = new AiDriver(cfg, difficulty, mf.StatsFor(m, RaceMode.Story), m.ability, 20f, seed);
            float dt = 1f / 15f, max = 0f;
            for (float t = 0f; t < seconds; t += dt)
            {
                var c = ctx != null ? ctx(t) : new AiContext();
                c.dt = dt;
                var cmd = d.Step(c);
                if (cmd.speed > max) max = cmd.speed;
            }
            return (d.Slips, max, d.Cap);
        }

        [Test]
        public void MistakeRatesFollowTheDifficulty()
        {
            // Ten simulated minutes each, three seeds, so the average is stable.
            double Avg(string d) => new[] { 1, 2, 3 }.Average(s => Drive(d, "horse", s, 600f).slips) / 10.0;
            double easy = Avg("easy"), normal = Avg("normal"), hard = Avg("hard");
            Assert.That(easy, Is.InRange(4.5, 7.5), "easy ~6 slips a minute");
            Assert.That(normal, Is.InRange(2.0, 4.0), "normal ~3");
            Assert.That(hard, Is.InRange(0.4, 1.8), "hard ~1");
            Assert.That(easy, Is.GreaterThan(normal)); Assert.That(normal, Is.GreaterThan(hard));
        }

        [Test]
        public void NoRacerEverGoesFasterThanItsMountEvenWhenFarBehind()
        {
            var mf = Mounts();
            foreach (var m in mf.mounts)
                foreach (var d in new[] { "easy", "normal", "hard" })
                {
                    var (_, max, cap) = Drive(d, m.id, 5, 120f, t => new AiContext { gapToPlayer = 500f }, mf);
                    Assert.That(max, Is.LessThanOrEqualTo(cap + 1e-4f), m.id + "/" + d);
                    Assert.That(max, Is.LessThanOrEqualTo(20f * m.stats.topSpeed + 1e-4f));
                }
        }

        [Test]
        public void CatchUpIsLightAndAheadRacersEaseOffWithoutStopping()
        {
            var mf = Mounts(); var cfg = Cfg(); var m = mf.Find("horse");
            float Speed(float gap) => new AiDriver(Cfg(), "normal", mf.StatsFor(m, RaceMode.Story), m.ability, 20f, 4).Step(new AiContext { dt = 0.0001f, gapToPlayer = gap }).speed;
            float behind = Speed(1000f), level = Speed(0f), ahead = Speed(-1000f);
            Assert.That(behind, Is.GreaterThan(level)); Assert.That(level, Is.GreaterThan(ahead));
            Assert.That(behind / level, Is.LessThan(1.06f), "catch-up is under 6%");
            Assert.That(ahead / level, Is.GreaterThan(0.94f));
        }

        [Test]
        public void CamelsIgnoreSandOthersSlowDownAndFalconsGlidePitsAndGiraffesTakeHighPaths()
        {
            var mf = Mounts();
            float Speed(string mount, bool sand)
            {
                var m = mf.Find(mount);
                return new AiDriver(Cfg(), "normal", mf.StatsFor(m, RaceMode.Story), m.ability, 20f, 4).Step(new AiContext { dt = 0.0001f, onSand = sand }).speed;
            }
            Assert.That(Speed("camel", true), Is.EqualTo(Speed("camel", false)).Within(1e-3f));
            Assert.That(Speed("ostrich", true), Is.LessThan(Speed("ostrich", false) * 0.8f));

            int glides = 0, highs = 0, noGlide = 0;
            for (int s = 0; s < 200; s++)
            {
                var fm = mf.Find("falcon_glider"); var gm = mf.Find("giraffe"); var hm = mf.Find("horse");
                if (new AiDriver(Cfg(), "hard", mf.StatsFor(fm, RaceMode.Story), fm.ability, 20f, s).Step(new AiContext { dt = 0.05f, pitAhead = true }).glide) glides++;
                if (new AiDriver(Cfg(), "hard", mf.StatsFor(gm, RaceMode.Story), gm.ability, 20f, s).Step(new AiContext { dt = 0.05f, highPathOpen = true }).useHighPath) highs++;
                if (new AiDriver(Cfg(), "hard", mf.StatsFor(hm, RaceMode.Story), hm.ability, 20f, s).Step(new AiContext { dt = 0.05f, pitAhead = true, highPathOpen = true }).glide) noGlide++;
            }
            Assert.That(glides, Is.GreaterThan(190)); Assert.That(highs, Is.GreaterThan(170)); Assert.That(noGlide, Is.EqualTo(0));
        }

        [Test]
        public void ObstacleDodgingFollowsTheDifficultyAndShortcutsAreChosenOncePerOpening()
        {
            var mf = Mounts(); var m = mf.Find("horse");
            int Slips(string d)
            {
                int total = 0;
                for (int s = 0; s < 300; s++)
                {
                    var drv = new AiDriver(Cfg(), d, mf.StatsFor(m, RaceMode.Story), m.ability, 20f, s);
                    // 1.2 s of obstacle ahead is longer than any reaction time, so it is either dodged or clipped, once.
                    for (int i = 0; i < 24; i++) drv.Step(new AiContext { dt = 0.05f, obstacleAhead = true }); // one obstacle, rolled once
                    total += drv.Slips;
                }
                return total;
            }
            int e = Slips("easy"), n = Slips("normal"), h = Slips("hard");
            Assert.That(e, Is.GreaterThan(n)); Assert.That(n, Is.GreaterThan(h));
            Assert.That(h / 300.0, Is.LessThan(0.2), $"e={e} n={n} h={h}");

            var d2 = new AiDriver(Cfg(), "normal", mf.StatsFor(m, RaceMode.Story), m.ability, 20f, 3);
            bool first = d2.Step(new AiContext { dt = 0.05f, shortcutOpen = true }).takeShortcut;
            for (int i = 0; i < 20; i++) Assert.That(d2.Step(new AiContext { dt = 0.05f, shortcutOpen = true }).takeShortcut, Is.EqualTo(first), "same answer while it stays open");
        }

        [Test]
        public void SameSeedDrivesTheSameWay()
        {
            Assert.That(Drive("normal", "camel", 9, 60f), Is.EqualTo(Drive("normal", "camel", 9, 60f)));
        }

        [Test]
        public void LowEndPhonesThinkSlowlyAndNeverUsePhysicsRaycasts()
        {
            Assert.That(AiTick.Hz(QualityTier.Low), Is.LessThan(AiTick.Hz(QualityTier.Medium)));
            Assert.That(AiTick.Hz(QualityTier.Medium), Is.LessThan(AiTick.Hz(QualityTier.High)));
            Assert.That(AiTick.MayRaycast(QualityTier.Low), Is.False);
            Assert.That(AiTick.MayRaycast(QualityTier.Medium), Is.False);
            Assert.That(AiTick.FullRate(QualityTier.Low), Is.LessThanOrEqualTo(3));
            Assert.That(AiTick.Interval(QualityTier.Low, 5), Is.GreaterThan(AiTick.Interval(QualityTier.Low, 0)), "far racers think at half rate");
            Assert.That(AiTick.Hz(QualityTier.High), Is.LessThanOrEqualTo(30), "decisions are never per frame");
        }

        // ---------------------------------------------------------------- fields and lobby

        [Test]
        public void OfflineFieldIsUpToSevenFromDifferentCountriesAndAlwaysHasTheHostRival()
        {
            var f = Rivals(); var cfg = Cfg();
            foreach (var host in new[] { "saudi", "japan", "australia", "brazil" })
                for (int seed = 0; seed < 30; seed++)
                {
                    var field = AiRoster.Build(f, cfg, 7, "normal", host, new Random(seed));
                    Assert.That(field.Count, Is.EqualTo(7));
                    Assert.That(field.Select(x => x.country).Distinct().Count(), Is.EqualTo(7));
                    Assert.That(field[0].country, Is.EqualTo(host));
                    Assert.That(field.Select(x => x.name).Distinct().Count(), Is.EqualTo(7));
                    Assert.That(field.All(x => RivalFile.Mounts.Contains(x.mount)), Is.True);
                }
            Assert.That(AiRoster.Build(f, cfg, 20, "easy", "saudi", new Random(1)).Count, Is.EqualTo(7), "never more than 7 computer racers");
            Assert.That(AiRoster.Build(f, cfg, 0, "easy", "saudi", new Random(1)), Is.Empty);
        }

        [Test]
        public void FieldsOnlyUseMountsThePlayerCouldMeetAndMixThem()
        {
            var f = Rivals(); var cfg = Cfg(); var seen = new HashSet<string>();
            for (int s = 0; s < 60; s++) foreach (var e in AiRoster.Build(f, cfg, 7, "normal", "uae", new Random(s))) seen.Add(e.mount);
            Assert.That(seen.Count, Is.GreaterThanOrEqualTo(4));
            var only = AiRoster.Build(f, cfg, 7, "normal", "uae", new Random(2), new[] { "horse" });
            Assert.That(only.All(e => e.mount == "horse"), Is.True);
        }

        private static LobbyInfo Room(int humans)
        {
            var l = new LobbyInfo();
            for (int i = 0; i < humans; i++) l.players.Add(new LobbyPlayer { id = "p" + i, name = "Human" + i, host = i == 0 });
            return l;
        }

        [Test]
        public void OnlineFillUsesTheSameRivalsLabelsThemAndNeverCountsThemAsReal()
        {
            var f = Rivals(); var cfg = Cfg();
            var room = Room(3);
            int added = AiFill.Fill(room, LobbyFlow.MaxPlayers, f, cfg, "normal", "egypt", new Random(5));
            Assert.That(added, Is.EqualTo(5));
            Assert.That(room.players.Count, Is.EqualTo(8));
            Assert.That(AiFill.RealRacers(room), Is.EqualTo(3));
            var bots = room.players.Where(p => p.ai).ToList();
            Assert.That(bots.All(b => b.rivalId.StartsWith("rival_") && f.Find(b.rivalId) != null), Is.True);
            Assert.That(bots.Any(b => b.rivalId == "rival_egypt"), Is.True, "host-country rival is in");
            Assert.That(bots.Select(b => b.name).Distinct().Count(), Is.EqualTo(5));
            foreach (var lang in UiLanguages.Codes) Assert.That(UiLanguages.Get(lang, "ai_named"), Does.Contain("{0}"), lang);
        }

        [Test]
        public void OnlinePointsIgnoreComputerRacersAndEasyPaysNothing()
        {
            var t = Table();
            var room = Room(1);
            AiFill.Fill(room, 8, Rivals(), Cfg(), "normal", "saudi", new Random(1));
            Assert.That(AiFill.RealRacers(room), Is.EqualTo(1));
            Assert.That(RacePoints.Award(t, PointsSource.Online, "normal", 1, AiFill.RealRacers(room)).Total, Is.EqualTo(0), "alone with computer racers earns nothing online");
            var two = Room(2);
            AiFill.Fill(two, 8, Rivals(), Cfg(), "normal", "saudi", new Random(1));
            Assert.That(RacePoints.Award(t, PointsSource.Online, "normal", 1, AiFill.RealRacers(two)).Total, Is.GreaterThan(0));
        }

        [Test]
        public void AiRacePointsAreFiveThreeOneOnNormalAndHardGetsAnExtraHalfAndNoneOnEasy()
        {
            var t = Table();
            Assert.That(new[] { 1, 2, 3 }.Select(p => RacePoints.Award(t, PointsSource.AiRace, "normal", p, 1).finish), Is.EqualTo(new[] { 5, 3, 1 }));
            Assert.That(new[] { 1, 2, 3 }.Select(p => RacePoints.Award(t, PointsSource.AiRace, "hard", p, 1).finish), Is.EqualTo(new[] { 7, 4, 1 }));
            foreach (var p in new[] { 1, 2, 3 }) Assert.That(RacePoints.Award(t, PointsSource.AiRace, "easy", p, 1).Total, Is.EqualTo(0));
            Assert.That(RacePoints.Award(t, PointsSource.AiRace, "hard", 1, 1).Total, Is.GreaterThan(RacePoints.Award(t, PointsSource.AiRace, "normal", 1, 1).Total));
        }

        [Test]
        public void ComputerRacerStringsExistIn12Languages()
        {
            foreach (var lang in UiLanguages.Codes)
                foreach (var k in new[] { "ai_difficulty", "ai_easy", "ai_normal", "ai_hard", "ai_computer", "ai_named", "ai_easy_note", "ai_online_note", "ai_fill_notice", "rival_chatter" })
                    Assert.That(UiLanguages.Get(lang, k), Is.Not.Null.And.Not.EqualTo(k), lang + "/" + k);
        }
    }
}
