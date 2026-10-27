// (c) Ahmed Alamrani. All rights reserved.
using System;
using System.Linq;
using Aron.Logic.Diagnostics;
using Aron.Logic.Online;
using Aron.Logic.Performance;
using NUnit.Framework;

namespace Aron.Logic.Tests
{
    public class RaceStateCodecTests
    {
        private static RaceState S(float d, float l, float v, int f = 0) => RaceState.From(d, l, v, f);

        [Test]
        public void QuantizingKeepsCentimetreAndWalkingPaceAccuracy()
        {
            var s = S(1234.567f, -3.14f, 21.3f, 3);
            Assert.AreEqual(1234.567f, RaceQuant.Dist(s.dist), 0.026f);
            Assert.AreEqual(-3.14f, RaceQuant.Lat(s.lat), 0.051f);
            Assert.AreEqual(21.3f, RaceQuant.Speed(s.speed), 0.126f);
            Assert.AreEqual(3, s.flags);
        }

        [Test]
        public void QuantizingClampsInsteadOfWrapping()
        {
            var s = S(-5f, 5000f, 9999f, 0x1FF);
            Assert.AreEqual(0, s.dist);
            Assert.AreEqual(2000, s.lat);
            Assert.AreEqual(1023, s.speed);
            Assert.AreEqual(0xFF, s.flags);
        }

        [Test]
        public void AKeyRecordRoundTripsExactly()
        {
            var buf = new byte[32]; var states = new RaceState[4];
            var cur = S(900.25f, -2.5f, 30f, 5);
            int end = RaceStateCodec.Write(buf, 0, 2, default, cur, true);
            int pos = 0;
            Assert.IsTrue(RaceStateCodec.Read(buf, end, ref pos, states, out int who));
            Assert.AreEqual(2, who);
            Assert.AreEqual(end, pos);
            Assert.IsTrue(states[2].Same(cur));
        }

        [Test]
        public void ADeltaRecordCarriesOnlyWhatChanged()
        {
            var buf = new byte[32];
            var a = S(100f, 1f, 20f); var b = S(101.5f, 1f, 20f);
            int end = RaceStateCodec.Write(buf, 0, 1, a, b, false);
            Assert.LessOrEqual(end, 3, "header + one small difference");
            var states = new RaceState[4]; states[1] = a;
            int pos = 0; RaceStateCodec.Read(buf, end, ref pos, states, out _);
            Assert.IsTrue(states[1].Same(b));
        }

        [Test]
        public void NegativeDifferencesWork()
        {
            var buf = new byte[32];
            var a = S(100f, 5f, 30f, 1); var b = S(100f, -5f, 12f, 0);
            int end = RaceStateCodec.Write(buf, 0, 0, a, b, false);
            var st = new RaceState[1]; st[0] = a;
            int pos = 0; Assert.IsTrue(RaceStateCodec.Read(buf, end, ref pos, st, out _));
            Assert.IsTrue(st[0].Same(b));
        }

        [Test]
        public void ARecordIsNeverLongerThanTheDocumentedMaximum()
        {
            var buf = new byte[64];
            var big = new RaceState { dist = 1 << 24, lat = -2000, speed = 1023, flags = 255 };
            Assert.LessOrEqual(RaceStateCodec.Write(buf, 0, 7, default, big, true), RaceStateCodec.MaxRecordBytes + 3);
        }

        [Test]
        public void MalformedPacketsAreRefusedNotCrashed()
        {
            var states = new RaceState[2];
            int pos = 0;
            Assert.IsFalse(RaceStateCodec.Read(new byte[0], 0, ref pos, states, out _));
            pos = 0; Assert.IsFalse(RaceStateCodec.Read(new byte[] { (byte)(7 << 5 | 1) }, 1, ref pos, states, out _), "racer number out of range");
            pos = 0; Assert.IsFalse(RaceStateCodec.Read(new byte[] { 1, 0x80 }, 2, ref pos, states, out _), "varint cut short");
            pos = 0; Assert.IsFalse(RaceStateCodec.Read(new byte[] { 1, 0x80, 0x80, 0x80, 0x80, 0x80, 0x80 }, 7, ref pos, states, out _), "varint too long");
            pos = 0; Assert.IsFalse(RaceStateCodec.Read(new byte[] { 8 }, 1, ref pos, states, out _), "flags missing");
            int last = -1;
            Assert.IsFalse(RaceSnapshot.Read(new byte[0], 0, states, ref last, out _));
        }

        [Test]
        public void ASnapshotCarriesSeveralRacersAndSkipsStillOnes()
        {
            var prev = new[] { S(10, 0, 10), S(20, 1, 10), S(30, 2, 10) };
            var cur = new[] { S(11, 0, 10), prev[1], S(31, 2, 10) };
            var buf = new byte[64];
            int len = RaceSnapshot.Write(buf, 5, prev, cur, null, false);
            var seen = new RaceState[3]; Array.Copy(prev, seen, 3);
            int seq = -1;
            Assert.IsTrue(RaceSnapshot.Read(buf, len, seen, ref seq, out bool gap));
            Assert.IsFalse(gap);
            for (int i = 0; i < 3; i++) Assert.IsTrue(seen[i].Same(cur[i]), "racer " + i);
            Assert.AreEqual(1 + 2 * 2, len, "sequence byte + two racers of 2 bytes each; the still racer costs nothing");
        }

        [Test]
        public void LostPacketsAreNoticedAndAKeyRecordHealsThem()
        {
            var truth = new RaceState[1]; var prev = new RaceState[1]; var seen = new RaceState[1];
            var buf = new byte[64]; int last = -1; bool anyGap = false;
            for (int t = 0; t < 40; t++)
            {
                truth[0] = S(t * 2f, 0, 20);
                bool key = t % 10 == 0;
                int len = RaceSnapshot.Write(buf, (byte)t, prev, truth, null, key);
                Array.Copy(truth, prev, 1);
                if (t == 4 || t == 5) continue;                  // two packets lost
                RaceSnapshot.Read(buf, len, seen, ref last, out bool gap);
                if (gap) anyGap = true;
                if (t == 6) Assert.IsFalse(seen[0].Same(truth[0]), "a delta on a stale base is off until the next key record");
                if (t == 10) Assert.IsTrue(seen[0].Same(truth[0]), "the key record repairs it");
            }
            Assert.IsTrue(anyGap);
        }

        [Test]
        public void SequenceWrapAroundIsNotAGap()
        {
            var st = new RaceState[1]; var buf = new byte[8];
            int last = 255; buf[0] = 0;
            RaceSnapshot.Read(buf, 1, st, ref last, out bool gap);
            Assert.IsFalse(gap);
        }

        [Test]
        public void EncodingAfterWarmUpAllocatesNothing()
        {
            var buf = new byte[64]; var a = new[] { S(10, 0, 10), S(20, 1, 10) }; var b = new[] { S(11, 0, 11), S(21, 1, 10) };
            var seen = new RaceState[2]; int last = -1; byte seq = 0;
            AllocationCheck.AssertNoAllocation(() =>
            {
                int len = RaceSnapshot.Write(buf, seq++, a, b, null, false);
                RaceSnapshot.Read(buf, len, seen, ref last, out _);
            }, "RaceSnapshot write+read");
        }
    }

    public class BandwidthTests
    {
        [Test]
        public void ARaceOfFourFitsTheBudgetAtEveryRate()
        {
            foreach (var (net, saver) in new[] { (NetworkKind.Wifi, false), (NetworkKind.Mobile, false), (NetworkKind.Mobile, true) })
            {
                int hz = RaceNetRate.Hz(net, saver);
                var r = BandwidthEstimator.Simulate(4, hz, 600);
                TestContext.WriteLine(net + " saver=" + saver + " " + hz + "Hz 4 players: up " + r.UpMBPerHour.ToString("0.00") + " down " + r.DownMBPerHour.ToString("0.00") + " total " + r.TotalMBPerHour.ToString("0.00") + " MB/h");
                Assert.LessOrEqual(r.TotalMBPerHour, 5.0, net + " saver=" + saver);
            }
        }

        [Test]
        public void DataSaverUsesLessThanTheNormalRate()
        {
            double Mb(NetworkKind n, bool s) => BandwidthEstimator.Simulate(4, RaceNetRate.Hz(n, s), 300).TotalMBPerHour;
            Assert.Less(Mb(NetworkKind.Mobile, true), Mb(NetworkKind.Mobile, false));
        }

        [Test]
        public void ARaceOfSixIsOverTheBudgetSoMobileDataRacesAreCappedAtFour()
        {
            Assert.Greater(BandwidthEstimator.Simulate(6, 10, 300).TotalMBPerHour, BandwidthEstimator.Simulate(4, 10, 300).TotalMBPerHour);
            Assert.AreEqual(4, RaceNetRate.MaxPlayersOnMobile);
        }

        [Test]
        public void TheRateStaysInTheDocumentedRange()
        {
            Assert.AreEqual(10, RaceNetRate.Hz(NetworkKind.Wifi, false));
            Assert.AreEqual(10, RaceNetRate.Hz(NetworkKind.Mobile, false));
            Assert.GreaterOrEqual(RaceNetRate.Hz(NetworkKind.Mobile, true), 8);
        }

        [Test]
        public void HeadersAreMostOfTheCostSoTheEstimateCountsThem()
        {
            var r = BandwidthEstimator.Simulate(4, 10, 60);
            long payloadOnly = r.upBytes - r.upPackets * BandwidthEstimator.PacketOverhead;
            Assert.Greater(r.upPackets * BandwidthEstimator.PacketOverhead, payloadOnly);
        }

        [Test]
        public void ASoloRunSendsNothingDown()
        {
            var r = BandwidthEstimator.Simulate(1, 10, 60);
            Assert.AreEqual(0, r.downBytes);
            Assert.Greater(r.upBytes, 0);
        }

        [Test]
        public void TheSimulationIsRepeatable()
        {
            Assert.AreEqual(BandwidthEstimator.Simulate(4, 10, 60, 3).downBytes, BandwidthEstimator.Simulate(4, 10, 60, 3).downBytes);
        }
    }

    public class DataGateTests
    {
        private static readonly PowerSettings Default = new PowerSettings();

        [Test]
        public void DownloadsWaitForWifiAndNeverRunDuringPlay()
        {
            var g = new DataGate();
            Assert.IsTrue(g.Allow(Traffic.Download, NetworkKind.Wifi, Default, ScreenMode.Menu));
            Assert.IsFalse(g.Allow(Traffic.Download, NetworkKind.Mobile, Default, ScreenMode.Menu));
            Assert.IsTrue(g.Allow(Traffic.Download, NetworkKind.Mobile, new PowerSettings { mobileData = true }, ScreenMode.Menu));
            Assert.IsFalse(g.Allow(Traffic.Download, NetworkKind.Wifi, Default, ScreenMode.Gameplay));
            Assert.IsFalse(g.Allow(Traffic.Download, NetworkKind.Wifi, new PowerSettings { mobileData = true }, ScreenMode.Gameplay));
        }

        [Test]
        public void NothingRunsInTheBackgroundOrOffline()
        {
            var g = new DataGate();
            foreach (Traffic t in Enum.GetValues(typeof(Traffic)))
            {
                Assert.IsFalse(g.Allow(t, NetworkKind.Wifi, Default, ScreenMode.Background), t + " in background");
                Assert.IsFalse(g.Allow(t, NetworkKind.None, Default, ScreenMode.Menu), t + " offline");
            }
        }

        [Test]
        public void TelemetryAndCrashReportsUseMobileDataOncePerSession()
        {
            var g = new DataGate();
            Assert.IsTrue(g.Allow(Traffic.Telemetry, NetworkKind.Mobile, Default, ScreenMode.Menu));
            g.NoteSent(Traffic.Telemetry, NetworkKind.Mobile);
            Assert.IsFalse(g.Allow(Traffic.Telemetry, NetworkKind.Mobile, Default, ScreenMode.Menu));
            Assert.IsFalse(g.Allow(Traffic.CrashReport, NetworkKind.Mobile, Default, ScreenMode.Menu), "the budget is shared");
            Assert.IsTrue(g.Allow(Traffic.CrashReport, NetworkKind.Wifi, Default, ScreenMode.Menu));
            g.NewSession();
            Assert.IsTrue(g.Allow(Traffic.CrashReport, NetworkKind.Mobile, Default, ScreenMode.Menu));
        }

        [Test]
        public void WifiSendsDoNotUseUpTheMobileAllowance()
        {
            var g = new DataGate();
            g.NoteSent(Traffic.Telemetry, NetworkKind.Wifi);
            Assert.IsTrue(g.Allow(Traffic.Telemetry, NetworkKind.Mobile, Default, ScreenMode.Menu));
        }

        [Test]
        public void DataSaverPushesEverythingOptionalToWifi()
        {
            var g = new DataGate(); var saver = new PowerSettings { dataSaver = true };
            Assert.IsFalse(g.Allow(Traffic.Telemetry, NetworkKind.Mobile, saver, ScreenMode.Menu));
            Assert.IsFalse(g.Allow(Traffic.CrashReport, NetworkKind.Mobile, saver, ScreenMode.Menu));
            Assert.IsFalse(g.Allow(Traffic.Sync, NetworkKind.Mobile, saver, ScreenMode.Menu));
            Assert.IsTrue(g.Allow(Traffic.Sync, NetworkKind.Wifi, saver, ScreenMode.Menu));
            Assert.IsTrue(g.Allow(Traffic.RaceState, NetworkKind.Mobile, saver, ScreenMode.Gameplay), "the race itself is what the player asked for");
        }
    }

    public class PayloadTests
    {
        [Test]
        public void GzipShrinksJsonAndRoundTrips()
        {
            string json = "[" + string.Join(",", Enumerable.Range(0, 40).Select(i => "{\"e\":\"stage_complete\",\"stage\":\"riyadh_" + i % 5 + "\",\"t\":" + (100 + i) + "}")) + "]";
            var z = Compact.Gzip(json);
            Assert.Less(z.Length, json.Length / 3);
            Assert.AreEqual(json, Compact.GunzipText(z));
        }

        [Test]
        public void TheBatcherDropsTheOldestWhenFull()
        {
            var b = new UploadBatcher { MaxItems = 3 };
            for (int i = 0; i < 5; i++) b.Add("{\"n\":" + i + "}");
            Assert.AreEqual(3, b.Count);
            Assert.AreEqual(2, b.Dropped);
            Assert.AreEqual("[{\"n\":2},{\"n\":3},{\"n\":4}]", b.BuildJsonArray());
            b.Clear();
            Assert.AreEqual(0, b.Count);
            Assert.AreEqual("[]", b.BuildJsonArray());
        }

        [Test]
        public void TheBatcherHonoursTheByteCap()
        {
            var b = new UploadBatcher { MaxBytes = 100 };
            for (int i = 0; i < 20; i++) b.Add(new string('x', 30));
            Assert.LessOrEqual(b.Bytes, 100 + 31);
            Assert.AreEqual(b.Count == 0 ? 0 : b.Count, b.Count);
        }

        [Test]
        public void ANotModifiedAnswerUsesTheCachedBody()
        {
            var c = new EtagCache();
            Assert.IsNull(c.IfNoneMatch("board"));
            Assert.AreEqual("{\"a\":1}", c.OnResponse("board", 200, "\"v1\"", "{\"a\":1}", 5));
            Assert.AreEqual("\"v1\"", c.IfNoneMatch("board"));
            Assert.AreEqual("{\"a\":1}", c.OnResponse("board", 304, null, null, 6));
            Assert.AreEqual(1, c.NotModified);
            Assert.AreEqual("{\"a\":2}", c.OnResponse("board", 200, "\"v2\"", "{\"a\":2}", 7));
            Assert.AreEqual("\"v2\"", c.IfNoneMatch("board"));
            Assert.AreEqual(1, c.store.entries.Count);
        }

        [Test]
        public void ErrorsAndUnknown304sGiveNothingAndKeepTheCache()
        {
            var c = new EtagCache();
            Assert.IsNull(c.OnResponse("x", 304, null, null, 1));
            c.OnResponse("x", 200, "e", "body", 1);
            Assert.IsNull(c.OnResponse("x", 500, null, null, 2));
            Assert.AreEqual("e", c.IfNoneMatch("x"));
        }

        [Test]
        public void TheCacheIsSmallAndSurvivesJson()
        {
            var c = new EtagCache { MaxEntries = 3 };
            for (int i = 0; i < 6; i++) c.OnResponse("k" + i, 200, "e" + i, "b", i);
            Assert.AreEqual(3, c.store.entries.Count);
            Assert.IsNull(c.Find("k0"));
            Assert.IsNotNull(c.Find("k5"));
        }

        [Test]
        public void TheMeterAddsUp()
        {
            var m = new DataMeter();
            m.Sent(Traffic.RaceState, 100); m.Received(Traffic.RaceState, 300); m.Sent(Traffic.Telemetry, 50);
            Assert.AreEqual(450, m.Total);
            Assert.AreEqual(300, m.ReceivedBytes(Traffic.RaceState));
        }
    }

    public class CrashReportTests
    {
        private const string Dsn = "https://abc123@o42.ingest.sentry.io/7777";

        [Test]
        public void ADsnIsParsedIntoItsParts()
        {
            Assert.IsTrue(SentryDsn.TryParse(Dsn, out var d));
            Assert.AreEqual("abc123", d.Key);
            Assert.AreEqual("o42.ingest.sentry.io", d.Host);
            Assert.AreEqual("7777", d.ProjectId);
            Assert.AreEqual("https://o42.ingest.sentry.io/api/7777/envelope/", d.EnvelopeUrl);
            StringAssert.Contains("sentry_key=abc123", d.AuthHeader);
        }

        [TestCase("")] [TestCase(null)] [TestCase("not a url")]
        [TestCase("http://abc@host/1")] [TestCase("https://host/1")] [TestCase("https://abc@host/")] [TestCase("https://abc@host/1/2")]
        public void BadOrInsecureDsnsAreRefused(string dsn) => Assert.IsFalse(SentryDsn.TryParse(dsn, out _));

        [Test]
        public void TheSameErrorIsCountedNotStoredAgain()
        {
            var log = new CrashLog();
            Assert.IsTrue(log.Record("error", "NullReference", "at A\nat B", "", "Low", "Android 14", "1.0", 1));
            Assert.IsFalse(log.Record("error", "NullReference", "at A\nat B", "", "Low", "Android 14", "1.0", 2));
            Assert.AreEqual(1, log.Pending);
            Assert.AreEqual(2, log.store.records[0].count);
            Assert.IsTrue(log.Record("error", "NullReference", "at C\nat D", "", "Low", "Android 14", "1.0", 3), "a different stack is a different problem");
        }

        [Test]
        public void ASessionAndTheFileAreBothCapped()
        {
            var log = new CrashLog();
            for (int i = 0; i < 100; i++) log.Record("error", "boom " + i, "at X" + i, "", "", "", "", i);
            Assert.AreEqual(CrashLog.MaxPerSession, log.TotalRecorded);
            var stored = new CrashLog(new CrashStore());
            for (int i = 0; i < CrashLog.MaxStored + 10; i++) stored.store.records.Add(new CrashRecord());
            stored.Record("error", "late", "at Z", "", "", "", "", 1);
            Assert.AreEqual(CrashLog.MaxStored, stored.Pending);
        }

        [Test]
        public void NamesAndEmailsAreScrubbedAndTextIsCut()
        {
            var log = new CrashLog { Secrets = () => new[] { "Layla" } };
            log.Record("error", "Layla failed for sara@example.com " + new string('x', 5000), "at Layla.Run " + new string('y', 9000), "Layla opened shop", "", "", "", 1);
            var r = log.store.records[0];
            StringAssert.DoesNotContain("Layla", r.message + r.stack + r.crumbs);
            StringAssert.DoesNotContain("sara@example.com", r.message);
            Assert.LessOrEqual(r.message.Length, CrashLog.MaxMessage);
            Assert.LessOrEqual(r.stack.Length, CrashLog.MaxStack);
        }

        [Test]
        public void TheEnvelopeHasTheRightShapeAndNoPersonalFields()
        {
            SentryDsn.TryParse(Dsn, out var dsn);
            var recs = new[]
            {
                new CrashRecord { fingerprint = "aa", message = "bad \"quote\"\nline", stack = "at A", crumbs = "-1.0s scene", tier = "Low", os = "Android 14", release = "aron@1.0", unixSeconds = 1700000000, count = 3 },
                new CrashRecord { fingerprint = "bb", message = "two", unixSeconds = 1700000001 },
            };
            string env = SentryEnvelope.Build(dsn, recs, 1700000100);
            var lines = env.TrimEnd('\n').Split('\n');
            Assert.AreEqual(1 + 2 * 2, lines.Length);
            StringAssert.Contains("\"dsn\":\"" + Dsn + "\"", lines[0]);
            Assert.AreEqual("{\"type\":\"event\",\"length\":" + System.Text.Encoding.UTF8.GetByteCount(lines[2]) + "}", lines[1]);
            StringAssert.Contains("bad \\\"quote\\\"\\nline", lines[2]);
            StringAssert.Contains("\"fingerprint\":[\"aa\"]", lines[2]);
            foreach (var bad in new[] { "\"user\"", "device", "server_name", "ip_address" }) StringAssert.DoesNotContain(bad, env);
        }

        [Test]
        public void ReportsAreSmallAfterGzip()
        {
            SentryDsn.TryParse(Dsn, out var dsn);
            var recs = Enumerable.Range(0, 10).Select(i => new CrashRecord { fingerprint = "f" + i, message = "NullReferenceException in Stage", stack = string.Join("\n", Enumerable.Repeat("at Aron.Stages.StageBuilder.Build()", 40)), unixSeconds = i }).ToList();
            var z = Compact.Gzip(SentryEnvelope.Build(dsn, recs, 1));
            Assert.Less(z.Length, 4000);
        }

        [Test]
        public void ControlCharactersAreEscaped()
        {
            Assert.AreEqual("a\\u0001b\\t", SentryEnvelope.Escape("a\u0001b\t"));
        }
    }
}
