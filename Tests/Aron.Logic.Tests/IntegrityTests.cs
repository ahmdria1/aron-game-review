// (c) Ahmed Alamrani. All rights reserved.
using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Aron.Logic.Integrity;
using Aron.Logic.Races;
using NUnit.Framework;

namespace Aron.Logic.Tests
{
    public class IntegrityTests
    {
        private static readonly JsonSerializerOptions Opt = new JsonSerializerOptions { IncludeFields = true };
        private static readonly DateTime T0 = new DateTime(2026, 10, 27, 12, 0, 0, DateTimeKind.Utc);
        private const string Salt = "test-salt";

        private static string Resources()
        {
            for (var d = new DirectoryInfo(TestContext.CurrentContext.TestDirectory); d != null; d = d.Parent)
            {
                var p = Path.Combine(d.FullName, "Assets", "_Aron", "Resources");
                if (Directory.Exists(Path.Combine(p, "Mounts"))) return p;
            }
            Assert.Fail("Resources not found");
            return null;
        }
        private static T Read<T>(params string[] parts) => JsonSerializer.Deserialize<T>(File.ReadAllText(Path.Combine(new[] { Resources() }.Concat(parts).ToArray())), Opt);
        private static RaceFile Races() => Read<RaceFile>("Races", "tracks.json");
        private static VehicleFile Vehicles() => Read<VehicleFile>("Mounts", "race_vehicles.json");
        private static StoreCatalog Catalog() => Read<StoreCatalog>("Store", "StoreCatalog.json");
        private static PointsTable Table() => Read<PointsTable>("Mounts", "race_points.json");

        // ------------------------------------------------------------ guarded numbers

        [Test]
        public void GuardedIntKeepsValueAndDoesNotStoreItPlain()
        {
            var g = new GuardedInt(1234);
            Assert.AreEqual(1234, g.Value);
            g.Add(66); Assert.AreEqual(1300, g.Value);
            Assert.IsTrue(g.TrySpend(300)); Assert.AreEqual(1000, g.Value);
            Assert.IsFalse(g.TrySpend(5000)); Assert.AreEqual(1000, g.Value);
            Assert.IsFalse(g.TrySpend(-1));
            Assert.IsFalse(g.Tampered);
        }

        [Test]
        public void GuardedIntMaskChangesOnEveryWrite()
        {
            var g = new GuardedInt(5);
            var f = typeof(GuardedInt).GetField("maskA", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var seen = new System.Collections.Generic.HashSet<int>();
            for (int i = 0; i < 20; i++) { g.Value = 5; seen.Add((int)f.GetValue(g)); }
            Assert.Greater(seen.Count, 15, "the same value should not look the same in memory");
            Assert.IsFalse(seen.Contains(5));
        }

        [Test]
        public void GuardedIntNoticesAnOutsideEditAndKeepsTheLastGoodValue()
        {
            var g = new GuardedInt(250);
            int fired = 0; Action<GuardedInt> h = _ => fired++;
            GuardedInt.TamperDetected += h;
            try
            {
                g.CorruptForTest(999999);
                Assert.AreEqual(250, g.Value);
                Assert.AreEqual(250, g.Value);
                Assert.IsTrue(g.Tampered);
                Assert.AreEqual(1, fired, "reported once");
            }
            finally { GuardedInt.TamperDetected -= h; }
        }

        // ------------------------------------------------------------ clock

        private class Fake { public DateTime Dev = T0; public long Mono = 1_000_000; }

        private static TrustedClock Clock(Fake f, ClockMemory m = null) => new TrustedClock(() => f.Dev, () => f.Mono, m);

        private static void Pass(Fake f, TimeSpan d) { f.Dev += d; f.Mono += (long)d.TotalMilliseconds; }

        [Test]
        public void OfflineClockInStepWithMonotonicIsVerified()
        {
            var f = new Fake(); var c = Clock(f);
            c.Now();                           // first run: nothing to compare
            Assert.AreEqual(ClockTrust.Unverified, c.Trust);
            Pass(f, TimeSpan.FromHours(3));
            Assert.AreEqual(f.Dev, c.Now());
            Assert.AreEqual(ClockTrust.Verified, c.Trust);
            Assert.IsTrue(c.CanEarn);
        }

        [Test]
        public void SettingTheClockForwardIsCaughtAndIgnored()
        {
            var f = new Fake(); var c = Clock(f); c.Now();
            Pass(f, TimeSpan.FromHours(1));
            var honest = c.Now();
            f.Dev += TimeSpan.FromDays(3);     // player moves the clock, monotonic does not move
            var t = c.Now();
            Assert.AreEqual(ClockTrust.Tampered, c.Trust);
            Assert.IsFalse(c.CanEarn);
            Assert.Less((t - honest).TotalMinutes, 1);
            Assert.AreEqual(1, c.Memory.tamperCount);
        }

        [Test]
        public void SettingTheClockBackNeverShortensTime()
        {
            var f = new Fake(); var c = Clock(f); c.Now();
            Pass(f, TimeSpan.FromHours(5));
            var before = c.Now();
            f.Dev -= TimeSpan.FromDays(2);
            Assert.GreaterOrEqual(c.Now(), before);
            Assert.AreEqual(ClockTrust.Tampered, c.Trust);
        }

        [Test]
        public void RebootIsUnverifiedButStillNeverGoesBack()
        {
            var f = new Fake(); var m = new ClockMemory();
            var c = Clock(f, m); c.Now(); Pass(f, TimeSpan.FromHours(2)); var last = c.Now();
            f.Mono = 500;                      // the phone restarted: monotonic clock starts again
            f.Dev = last - TimeSpan.FromHours(10);
            var c2 = Clock(f, m);
            Assert.GreaterOrEqual(c2.Now(), last);
            Assert.AreEqual(ClockTrust.Unverified, c2.Trust);
            Assert.IsFalse(c2.CanEarn);
        }

        [Test]
        public void ServerTimeWinsAndTheDeviceClockIsIgnored()
        {
            var f = new Fake(); var c = Clock(f);
            c.SyncServer(T0.AddHours(10));
            f.Dev = T0.AddYears(5); f.Mono += 60_000;
            Assert.AreEqual(T0.AddHours(10).AddMinutes(1), c.Now());
            Assert.AreEqual(ClockTrust.Server, c.Trust);
        }

        [Test]
        public void RentalsFollowTheTrustedClockNotThePhoneClock()
        {
            var cat = Catalog(); var item = cat.items.First(i => i.CanRent);
            var f = new Fake(); var c = Clock(f); c.Now();
            var data = new SaveData { racePoints = 500 };
            var inv = new StoreInventory(cat, data) { Clock = c.Now };
            Assert.AreEqual(StoreInventory.RentResult.Rented, inv.Rent(item.id));
            f.Dev += TimeSpan.FromDays(10);    // player skips ahead on the phone clock only
            Assert.IsTrue(inv.IsRented(item.id), "still rented: only a few seconds really passed");
            Pass(f, TimeSpan.FromHours(49));   // real time passes
            Assert.IsFalse(inv.IsRented(item.id));
        }

        // ------------------------------------------------------------ race results

        private static (RaceFile races, RaceTrack t, float top) Setup()
        {
            var races = Races(); var t = races.tracks.First(x => x.laps >= 2);
            return (races, t, 20f);
        }

        private static RaceReport Honest(RaceFile races, RaceTrack t, float top, float slack = 1.3f)
        {
            float min = Safety.ScoreCheck.MinimumSeconds(t, races, top);
            float lap = min / t.laps * slack;
            return new RaceReport { trackId = t.id, totalSeconds = lap * t.laps, lapSeconds = Enumerable.Repeat(lap, t.laps).ToArray() };
        }

        [Test]
        public void HonestResultPassesAndImpossibleOnesFail()
        {
            var (races, t, top) = Setup();
            Assert.IsNull(RaceResultValidator.Check(Honest(races, t, top), races, top));
            var fast = Honest(races, t, top, 0.5f);
            Assert.AreEqual("implausible_time", RaceResultValidator.Check(fast, races, top));
            var oneBadLap = Honest(races, t, top);
            oneBadLap.lapSeconds[0] = 3f; oneBadLap.lapSeconds[1] += oneBadLap.lapSeconds[0] - 3f;
            Assert.AreEqual("impossible_lap", RaceResultValidator.Check(oneBadLap, races, top));
            var mismatch = Honest(races, t, top); mismatch.totalSeconds += 30;
            Assert.AreEqual("laps_do_not_add_up", RaceResultValidator.Check(mismatch, races, top));
            var wrongLaps = Honest(races, t, top); wrongLaps.lapSeconds = new[] { wrongLaps.totalSeconds };
            Assert.AreEqual("wrong_lap_count", RaceResultValidator.Check(wrongLaps, races, top));
            var nan = Honest(races, t, top); nan.totalSeconds = float.NaN;
            Assert.AreEqual("bad_total_time", RaceResultValidator.Check(nan, races, top));
            Assert.AreEqual("unknown_track", RaceResultValidator.Check(new RaceReport { trackId = "nope", totalSeconds = 100 }, races, top));
            Assert.AreEqual("missing_report", RaceResultValidator.Check(null, races, top));
        }

        [Test]
        public void ASlowVehicleCannotPostAFastVehiclesTime()
        {
            var (races, t, _) = Setup(); var vf = Vehicles();
            var slow = vf.classes.SelectMany(c => c.vehicles.Select(v => (c, v))).OrderBy(x => x.v.stats.topSpeed).First();
            var quick = vf.classes.SelectMany(c => c.vehicles.Select(v => (c, v))).OrderByDescending(x => x.v.stats.topSpeed).First();
            float slowCap = SpeedCaps.MaxSpeed(slow.v, slow.c, 15f), quickCap = SpeedCaps.MaxSpeed(quick.v, quick.c, 15f);
            Assert.Less(slowCap, quickCap);
            var rep = Honest(races, t, quickCap, 1.0f);
            Assert.IsNull(RaceResultValidator.Check(rep, races, quickCap));
            Assert.AreEqual("implausible_time", RaceResultValidator.Check(rep, races, slowCap));
        }

        // ------------------------------------------------------------ server economy

        private static (ServerEconomy eco, RaceFile races, RaceTrack t, float top) Server()
        {
            var (races, t, top) = Setup();
            return (new ServerEconomy(Table(), races, Catalog(), Salt), races, t, top);
        }

        private static RaceClaim Claim(RaceTrack t, RaceReport rep, string match, int place = 1)
        {
            var c = new RaceClaim { playerId = "p1", matchId = match, track = t.id, difficulty = "normal", place = place, realRacers = 2, aiRacers = 2, seconds = rep.totalSeconds };
            c.signature = ClaimValidator.SignClaim(c, Salt);
            return c;
        }

        [Test]
        public void ServerGrantsOnlyOnceAndStopsAtTheDailyCaps()
        {
            var (eco, races, t, top) = Server();
            var player = new SaveData();
            int paid = 0;
            for (int i = 0; i < 20; i++)
            {
                var rep = Honest(races, t, top);
                var o = eco.GrantOnlineRace(player, Claim(t, rep, "m" + i), rep, top, T0);
                if (o.Accepted) paid++;
            }
            Assert.Greater(paid, 0);
            Assert.LessOrEqual(paid, Table().dailyPerTrack);
            Assert.LessOrEqual(player.racePoints, Table().dailyPoints);
            // next UTC day resets
            var rep2 = Honest(races, t, top);
            Assert.IsTrue(eco.GrantOnlineRace(player, Claim(t, rep2, "next-day"), rep2, top, T0.AddDays(1)).Accepted);
        }

        [Test]
        public void ReplayedMatchForgedSignatureAndEditedTimeAreRefused()
        {
            var (eco, races, t, top) = Server();
            var player = new SaveData();
            var rep = Honest(races, t, top);
            var claim = Claim(t, rep, "m1");
            Assert.IsTrue(eco.GrantOnlineRace(player, claim, rep, top, T0).Accepted);
            Assert.AreEqual("duplicate_match", eco.GrantOnlineRace(player, claim, rep, top, T0).Reason);
            var forged = Claim(t, rep, "m2"); forged.signature = "00000000";
            Assert.AreEqual("bad_signature", eco.GrantOnlineRace(player, forged, rep, top, T0).Reason);
            var edited = Claim(t, rep, "m3"); edited.seconds -= 5;
            Assert.AreEqual("report_does_not_match_claim", eco.GrantOnlineRace(player, edited, rep, top, T0).Reason);
            Assert.AreEqual(player.racePoints, eco.GrantOnlineRace(player, forged, rep, top, T0).ServerPoints);
        }

        [Test]
        public void ServerRentUsesServerTimeAndTheClientBalanceClaimIsIgnored()
        {
            var (eco, _, _, _) = Server();
            var cat = Catalog(); var item = cat.items.First(i => i.CanRent);
            var player = new SaveData { racePoints = 100 };
            Assert.AreEqual(StoreInventory.RentResult.Rented, eco.Rent(player, item.id, T0));
            Assert.IsTrue(eco.RentalActive(player, item.id, T0.AddHours(47)));
            Assert.IsFalse(eco.RentalActive(player, item.id, T0.AddHours(49)));
            Assert.AreEqual(StoreInventory.RentResult.NotEnoughPoints, eco.Rent(new SaveData { racePoints = 5 }, item.id, T0));
            Assert.AreEqual(0, ServerEconomy.Reconcile(999999, 0));
        }

        // ------------------------------------------------------------ purchases

        [Test]
        public void ReceiptPolicyChecksProductAppStateAndRepeats()
        {
            var cat = Catalog(); var prod = cat.items.First(i => !string.IsNullOrEmpty(i.productId)).productId;
            var pol = new ReceiptPolicy(cat, "com.ahmedalamrani.aron");
            PurchaseReceipt R(string tx, string pid = null, string pkg = "com.ahmedalamrani.aron", int state = 0) =>
                new PurchaseReceipt { store = "GooglePlay", productId = pid ?? prod, transactionId = tx, packageName = pkg, purchaseState = state };
            Assert.AreEqual(ReceiptVerdict.Valid, pol.Check(R("t1")));
            Assert.AreEqual(ReceiptVerdict.Duplicate, pol.Check(R("t1")));
            Assert.AreEqual(ReceiptVerdict.WrongProduct, pol.Check(R("t2", "free.gems.9999")));
            Assert.AreEqual(ReceiptVerdict.WrongApp, pol.Check(R("t3", null, "com.other.app")));
            Assert.AreEqual(ReceiptVerdict.Invalid, pol.Check(R("t4", null, "com.ahmedalamrani.aron", 1)));
            Assert.AreEqual(ReceiptVerdict.Invalid, pol.Check(new PurchaseReceipt { productId = prod }));
        }

        [Test]
        public void GooglePlaySignatureVerifiesAndRejectsAnyChange()
        {
            using (var rsa = RSA.Create(2048))
            {
                string json = "{\"orderId\":\"GPA.1\",\"productId\":\"x\",\"purchaseState\":0}";
                string sig = Convert.ToBase64String(rsa.SignData(Encoding.UTF8.GetBytes(json), HashAlgorithmName.SHA1, RSASignaturePadding.Pkcs1));
                var pub = rsa.ExportSubjectPublicKeyInfo();
                Assert.IsTrue(GooglePlaySignature.Verify(json, sig, pub));
                Assert.IsFalse(GooglePlaySignature.Verify(json.Replace("\"x\"", "\"y\""), sig, pub));
                Assert.IsFalse(GooglePlaySignature.Verify(json, "!!not base64!!", pub));
                Assert.IsFalse(GooglePlaySignature.Verify(json, sig, new byte[] { 1, 2, 3 }));
                using (var other = RSA.Create(2048)) Assert.IsFalse(GooglePlaySignature.Verify(json, sig, other.ExportSubjectPublicKeyInfo()));
            }
        }

        // ------------------------------------------------------------ attestation

        [Test]
        public void NonceIsStableUrlSafeAndBoundToTheMatch()
        {
            string a = IntegrityPolicy.Nonce("p1", "m1", "c1");
            Assert.AreEqual(a, IntegrityPolicy.Nonce("p1", "m1", "c1"));
            Assert.AreNotEqual(a, IntegrityPolicy.Nonce("p1", "m2", "c1"));
            Assert.AreNotEqual(a, IntegrityPolicy.Nonce("p1", "m1", "c2"));
            StringAssert.DoesNotContain("+", a); StringAssert.DoesNotContain("/", a); StringAssert.DoesNotContain("=", a);
        }

        [Test]
        public void RankedPlayNeedsAVerifiedDeviceButOfflinePlayNeverDoes()
        {
            Assert.IsTrue(IntegrityPolicy.RankedAllowed(DeviceTrust.Verified, true));
            Assert.IsFalse(IntegrityPolicy.RankedAllowed(DeviceTrust.Failed, false));
            Assert.IsTrue(IntegrityPolicy.RankedAllowed(DeviceTrust.Unknown, false), "no account set up yet: don't lock honest players out");
            Assert.IsFalse(IntegrityPolicy.RankedAllowed(DeviceTrust.Unknown, true));
            Assert.IsTrue(IntegrityPolicy.OfflinePlayAllowed);
            IntegrityToken got = null;
            var none = new NoIntegrity(); none.Request("n", t => got = t);
            Assert.AreEqual("not_configured", got.error); Assert.IsFalse(none.Available);
        }

        // ------------------------------------------------------------ rate limits

        [Test]
        public void RateLimiterAllowsABurstThenRefillsAndLocksOutAbuse()
        {
            var r = new RateLimiter(); r.Set("report", 3, 6);
            var t = T0;
            Assert.IsTrue(r.Allow("report", "u1", t)); Assert.IsTrue(r.Allow("report", "u1", t)); Assert.IsTrue(r.Allow("report", "u1", t));
            Assert.IsFalse(r.Allow("report", "u1", t));
            Assert.IsTrue(r.Allow("report", "u2", t), "keys are separate");
            Assert.IsFalse(r.Allow("report", "u1", t.AddMinutes(6)), "still locked out after the refusal");
            Assert.IsTrue(r.Allow("report", "u1", t.AddMinutes(30)));
            Assert.IsTrue(r.Allow("other", "u1", t), "unknown endpoint is not limited here");
        }

        [Test]
        public void RepeatedAbuseLocksLongerAndOldKeysArePruned()
        {
            var r = new RateLimiter(); r.Set("name_change", 1, 1);
            var t = T0;
            Assert.IsTrue(r.Allow("name_change", "u", t));
            Assert.IsFalse(r.Allow("name_change", "u", t));                   // lock 5 min
            Assert.IsFalse(r.Allow("name_change", "u", t.AddMinutes(4)));
            Assert.IsTrue(r.Allow("name_change", "u", t.AddHours(2)));
            Assert.AreEqual(1, r.Prune(t.AddDays(3)));
            Assert.AreEqual(0, r.Prune(t.AddDays(3)));
            var d = RateLimiter.Default();
            for (int i = 0; i < 3; i++) Assert.IsTrue(d.Allow("name_change", "x", t));
            Assert.IsFalse(d.Allow("name_change", "x", t));
        }
    }
}

namespace Aron.Logic.Tests
{
    public class WalletWatchTests
    {
        [SetUp] public void Reset() { Aron.Logic.Integrity.SoftFlags.Clear(); }

        [Test]
        public void SmallHonestStepsPass()
        {
            var w = new Aron.Logic.Integrity.WalletWatch(); var d = new SaveData();
            Assert.IsTrue(w.Audit(d));
            d.coins += 120; d.racePoints += 5; d.racePointsLifetime += 5;
            Assert.IsTrue(w.Audit(d));
            d.coins -= 60;
            Assert.IsTrue(w.Audit(d));
            Assert.IsFalse(Aron.Logic.Integrity.SoftFlags.Any);
        }

        [Test]
        public void HugeJumpsAreFlaggedAndNeverBlockAnything()
        {
            var w = new Aron.Logic.Integrity.WalletWatch(); var d = new SaveData();
            w.Audit(d);
            d.coins = 9_999_999;
            Assert.IsFalse(w.Audit(d));
            Assert.AreEqual("coins", w.LastAlert);
            Assert.IsTrue(Aron.Logic.Integrity.SoftFlags.Current.HasFlag(Aron.Logic.Integrity.SoftFlag.WalletJump));
            Assert.IsTrue(w.Audit(d), "the baseline moves on, one alert per jump");
            d.racePoints = 5000;
            Assert.IsFalse(w.Audit(d));
            d.racePointsLifetime = -1;
            Assert.IsFalse(w.Audit(d));
            Assert.AreEqual(3, w.Alerts);
            Assert.IsTrue(Aron.Logic.Integrity.IntegrityPolicy.OfflinePlayAllowed);
        }
    }
}
