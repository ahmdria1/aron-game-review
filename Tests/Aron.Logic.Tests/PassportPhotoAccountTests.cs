// (c) Ahmed Alamrani. All rights reserved.
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using Aron.Logic.Account;
using Aron.Logic.Passport;
using Aron.Logic.Photo;
using Aron.Logic.Regions;
using NUnit.Framework;

namespace Aron.Logic.Tests
{
    public class PassportTests
    {
        private static readonly DateTime Now = new DateTime(2026, 10, 27, 9, 0, 0, DateTimeKind.Utc);

        private static StageDef St(string id, bool boss = false) => new StageDef { id = id, boss = boss, type = boss ? "boss" : "normal" };

        private static RegionCatalog Catalog() => RegionCatalog.FromRegions(new[]
        {
            new RegionDef { id = "riyadh", group = "saudi", order = 100, country = "saudi", stages = new[] { St("r1"), St("r2", true) } },
            new RegionDef { id = "makkah", group = "saudi", order = 101, country = "saudi", stages = new[] { St("m1"), St("m2", true) } },
            new RegionDef { id = "japan", group = "world", order = 444, country = "japan", stages = new[] { St("j1"), St("j2", true) } },
        });

        [Test]
        public void StampOnlyWhenTheCountrysLastBossFalls()
        {
            var c = Catalog(); var d = new SaveData();
            StageProgress.Finish(d, "r1", 30f, 1);
            Assert.That(PassportBook.OnStageFinished(c, d, "r1", Now), Is.False, "not a boss");
            StageProgress.Finish(d, "r2", 30f, 1);
            Assert.That(PassportBook.OnStageFinished(c, d, "r2", Now), Is.False, "Saudi still has a boss standing");
            StageProgress.Finish(d, "m2", 30f, 1);
            Assert.That(PassportBook.OnStageFinished(c, d, "m2", Now), Is.True);
            Assert.That(PassportBook.OnStageFinished(c, d, "m2", Now), Is.False, "only once");
            var page = PassportBook.Page(c, d, "saudi", null);
            Assert.That(page.stamped && page.stampDate == "2026-10-27" && page.photo == "auto");
            Assert.That(page.stamp.date, Is.EqualTo("27.10.2026"));
            Assert.That(page.stamp.Original(), Is.True);
            Assert.That(page.localName, Is.EqualTo("السعودية"));
        }

        [Test]
        public void ABossNotYetBeatenGivesNoStamp()
        {
            var c = Catalog(); var d = new SaveData();
            Assert.That(PassportBook.OnStageFinished(c, d, "j2", Now), Is.False);
            Assert.That(PassportBook.Page(c, d, "japan", null).stamped, Is.False);
        }

        [Test]
        public void TicketVisitGivesAVisaStickerOnce()
        {
            var d = new SaveData();
            Assert.That(PassportBook.OnTicketVisit(d, "japan", Now), Is.True);
            Assert.That(PassportBook.OnTicketVisit(d, "japan", Now.AddDays(1)), Is.False);
            var p = PassportBook.Page(Catalog(), d, "japan", null);
            Assert.That(p.visa && !p.stamped && p.visaDate == "2026-10-27");
        }

        [Test]
        public void PhotoSlotOnlyOnVisitedPagesAndSafeNames()
        {
            var d = new SaveData();
            Assert.That(PassportBook.SetPhoto(d, "japan", "a.png"), Is.False);
            PassportBook.OnTicketVisit(d, "japan", Now);
            Assert.That(PassportBook.SetPhoto(d, "japan", "../x.png"), Is.False);
            Assert.That(PassportBook.SetPhoto(d, "japan", "aron_japan_1.png"), Is.True);
            Assert.That(PassportBook.Page(Catalog(), d, "japan", null).photo, Is.EqualTo("photo:aron_japan_1.png"));
        }

        [Test]
        public void EveryCountryHasALocalNameAndAnOriginalStamp()
        {
            Assert.That(PassportBook.LocalNames.Count, Is.EqualTo(35));
            Assert.That(PassportBook.LocalNames.ContainsKey("palestine"), Is.False);
            foreach (var kv in PassportBook.LocalNames) Assert.That(StampArt.For(kv.Key, kv.Value, "01.01.2027").Original(), Is.True, kv.Key);
            Assert.That(new StampArt { shape = "circle", motif = "sun", text = "Kingdom of Nowhere" }.Original(), Is.False);
        }

        [Test]
        public void PagesTurnInReadingDirection()
        {
            Assert.That(PassportBook.Turn(3, 35, 1f, "ar"), Is.EqualTo(4));
            Assert.That(PassportBook.Turn(3, 35, -1f, "en"), Is.EqualTo(4));
            Assert.That(PassportBook.Turn(0, 35, 1f, "en"), Is.EqualTo(0));
            Assert.That(PassportBook.Turn(34, 35, 1f, "ar"), Is.EqualTo(34));
            Assert.That(PassportBook.ShareFileName("japan", Now), Is.EqualTo("aron_passport_japan_20261027.png"));
        }
    }

    public class PhotoModeTests
    {
        [Test]
        public void DisabledInOnlineRacesAndDuringATimedRun()
        {
            Assert.That(PhotoMode.Allowed(new PhotoContext { onlineRace = true, inHub = true }), Is.False);
            Assert.That(PhotoMode.Allowed(new PhotoContext { timeTrial = true, raceStarted = true }), Is.False);
            Assert.That(PhotoMode.Allowed(new PhotoContext { timeTrial = true }), Is.True);
            Assert.That(PhotoMode.Allowed(new PhotoContext { timeTrial = true, raceStarted = true, raceFinished = true }), Is.True);
            Assert.That(PhotoMode.Allowed(new PhotoContext { inStage = true }), Is.False, "only from pause");
            Assert.That(PhotoMode.Allowed(new PhotoContext { inStage = true, paused = true }), Is.True);
            Assert.That(PhotoMode.Allowed(new PhotoContext { inHub = true }), Is.True);
            Assert.That(PhotoMode.Allowed(new PhotoContext { inHub = true, cutscene = true }), Is.False);
        }

        [Test]
        public void CameraStaysInsideItsLimits()
        {
            var b = new PhotoBounds { minX = -10, maxX = 10, minY = 0, maxY = 8, minZ = -10, maxZ = 10 };
            var cam = PhotoMode.Clamp(new PhotoCamera { distance = 50f, pitch = -80f, fov = 150f, tilt = 90f, yaw = -30f, panX = 30f }, b);
            Assert.That(cam.distance, Is.EqualTo(b.maxDistance));
            Assert.That(cam.pitch, Is.EqualTo(b.minPitch));
            Assert.That(cam.fov, Is.EqualTo(b.maxFov));
            Assert.That(cam.tilt, Is.EqualTo(b.maxTilt));
            Assert.That(cam.yaw, Is.EqualTo(330f).Within(1e-3));
            Assert.That(cam.panX, Is.EqualTo(b.maxPan).Within(1e-4));
            PhotoMode.Position(new PhotoCamera { distance = 9f, pitch = 70f }, 9f, 7f, -9f, b, out float x, out float y, out float z);
            Assert.That(x, Is.InRange(-10f, 10f)); Assert.That(y, Is.InRange(0f, 8f)); Assert.That(z, Is.InRange(-10f, 10f));
            Assert.That(PhotoMode.PullIn(6f, 2f, b), Is.EqualTo(1.75f).Within(1e-4));
        }

        [Test]
        public void HighTierOnlyExtras()
        {
            Assert.That(PhotoMode.DepthOfField(QualityTierName.High), Is.True);
            Assert.That(PhotoMode.DepthOfField(QualityTierName.Medium), Is.False);
            Assert.That(PhotoMode.CaptureScale(QualityTierName.High, true), Is.EqualTo(2));
            Assert.That(PhotoMode.CaptureScale(QualityTierName.Low, true), Is.EqualTo(1));
            Assert.That(PhotoMode.Filters, Is.SupersetOf(new[] { "warm_desert", "cinematic", "mono", "vivid" }));
            Assert.That(PhotoMode.Poses, Is.SupersetOf(new[] { "joy", "scream", "victory_dance", "eat_local" }));
        }

        [Test]
        public void ExportCarriesTheWatermark()
        {
            Assert.That(Watermark.AlwaysOn, Is.True);
            int w = 200, h = 100;
            var photo = new byte[w * h * 4];
            var logo = Enumerable.Repeat((byte)255, 10 * 5 * 4).ToArray();
            Watermark.Apply(photo, w, h, logo, 10, 5, false);
            Watermark.Rect(w, h, 10, 5, false, out int x, out int y, out int rw, out int rh);
            Assert.That(x + rw, Is.LessThanOrEqualTo(w)); Assert.That(x, Is.GreaterThan(w / 2));
            int i = ((y + 1) * w + x + 1) * 4;
            Assert.That(photo[i], Is.GreaterThan(200), "logo pixel blended in");
            Assert.That(photo[0], Is.EqualTo(0), "the rest untouched");
            Watermark.Rect(w, h, 10, 5, true, out int rx, out _, out _, out _);
            Assert.That(rx, Is.LessThan(w / 2), "bottom-left in RTL");
        }

        [Test]
        public void FileNamesCarryNoPersonalData()
        {
            var n = PhotoMode.FileName("japan", new DateTime(2026, 10, 27, 1, 2, 3, DateTimeKind.Utc));
            Assert.That(n, Is.EqualTo("aron_japan_20261027_010203.png"));
        }

        private class FakeGallery : IGallery
        {
            public GalleryPermission Status { get; set; }
            public GalleryPermission Answer;
            public int Asked, Saved;
            public void Request(Action<GalleryPermission> done) { Asked++; Status = Answer; done(Answer); }
            public bool Save(byte[] png, string fileName) { Saved++; return true; }
        }

        [Test]
        public void GalleryPermissionAskedAtFirstSaveOnly()
        {
            var d = new SaveData();
            var g = new FakeGallery { Answer = GalleryPermission.Granted };
            SaveResult r = SaveResult.Failed;
            PhotoSaver.Save(g, d, new byte[1], "a.png", x => r = x);
            Assert.That(r == SaveResult.Saved && g.Asked == 1 && d.galleryPermission == 1);
            PhotoSaver.Save(g, d, new byte[1], "b.png", x => r = x);
            Assert.That(g.Asked, Is.EqualTo(1));
            Assert.That(g.Saved, Is.EqualTo(2));

            var no = new FakeGallery { Answer = GalleryPermission.Denied };
            PhotoSaver.Save(no, d, new byte[1], "c.png", x => r = x);
            Assert.That(r == SaveResult.Denied && no.Saved == 0 && d.galleryPermission == 2);
            PhotoSaver.Save(no, d, new byte[1], "c.png", x => r = x);
            Assert.That(no.Asked, Is.EqualTo(1), "no nagging after a no");
        }
    }

    public class EmailAccountTests
    {
        private static readonly DateTime T0 = new DateTime(2026, 10, 27, 9, 0, 0, DateTimeKind.Utc);

        private static OtpStore Store(string code = "123456") => new OtpStore("pepper") { NewCode = () => code };

        [Test]
        public void CodeExpiresAfterTenMinutes()
        {
            var s = Store();
            Assert.That(s.Issue("a@b.com", "ip", "dev", T0, null), Is.EqualTo(OtpIssue.Sent));
            Assert.That(s.Verify("a@b.com", "123456", T0.AddMinutes(10)), Is.EqualTo(OtpCheck.Expired));
            s.Issue("a@b.com", "ip", "dev", T0.AddMinutes(11), null);
            Assert.That(s.Verify("A@B.com ", "123456", T0.AddMinutes(20)), Is.EqualTo(OtpCheck.Ok));
            Assert.That(s.Verify("a@b.com", "123456", T0.AddMinutes(20)), Is.EqualTo(OtpCheck.NoCode), "single use");
        }

        [Test]
        public void FiveTriesThenTheCodeIsGone()
        {
            var s = Store();
            s.Issue("a@b.com", "ip", "dev", T0, null);
            for (int i = 0; i < 4; i++) Assert.That(s.Verify("a@b.com", "000000", T0), Is.EqualTo(OtpCheck.Wrong));
            Assert.That(s.Verify("a@b.com", "000000", T0), Is.EqualTo(OtpCheck.TooManyAttempts));
            Assert.That(s.Verify("a@b.com", "123456", T0), Is.EqualTo(OtpCheck.NoCode));
        }

        [Test]
        public void ResendWaitsSixtySeconds()
        {
            var s = Store();
            s.Issue("a@b.com", "ip", "dev", T0, null);
            Assert.That(s.Issue("a@b.com", "ip", "dev", T0.AddSeconds(59), null), Is.EqualTo(OtpIssue.Cooldown));
            Assert.That(s.Issue("a@b.com", "ip", "dev", T0.AddSeconds(60), null), Is.EqualTo(OtpIssue.Sent));
        }

        [Test]
        public void RateLimitedPerEmailIpAndDevice()
        {
            var s = Store();
            for (int i = 0; i < OtpStore.PerEmailHour; i++) Assert.That(s.Issue("a@b.com", "ip" + i, "d" + i, T0.AddMinutes(i * 2), null), Is.EqualTo(OtpIssue.Sent));
            Assert.That(s.Issue("a@b.com", "ipx", "dx", T0.AddMinutes(12), null), Is.EqualTo(OtpIssue.RateLimited));
            Assert.That(s.Issue("a@b.com", "ipx", "dx", T0.AddMinutes(61), null), Is.EqualTo(OtpIssue.Sent), "window slides");

            var dev = Store();
            for (int i = 0; i < OtpStore.PerDeviceHour; i++) dev.Issue("u" + i + "@b.com", "ip" + i, "same", T0, null);
            Assert.That(dev.Issue("z@b.com", "ipz", "same", T0, null), Is.EqualTo(OtpIssue.RateLimited));

            var ip = Store();
            for (int i = 0; i < OtpStore.PerIpHour; i++) ip.Issue("u" + i + "@b.com", "shared", "d" + i, T0, null);
            Assert.That(ip.Issue("z@b.com", "shared", "dz", T0, null), Is.EqualTo(OtpIssue.RateLimited));
            Assert.That(ip.Issue("not an email", "x", "y", T0, null), Is.EqualTo(OtpIssue.InvalidEmail));
        }

        private class FakeAuth : IAuthBackend
        {
            public string PlayerId => "guest-1";
            public LinkResult Next = LinkResult.Linked;
            public int Unlinks;
            public LinkResult LinkOpenId(string idProvider, string idToken) => Next;
            public void UnlinkOpenId(string idProvider) => Unlinks++;
        }

        [Test]
        public void LinkingKeepsTheGuestsProgress()
        {
            var d = new SaveData { coins = 777, homeCountry = "saudi" };
            d.foodsCollected.Add("kabsa");
            StageProgress.Finish(d, "riyadh_1", 30f, 3);
            var before = JsonSerializer.Serialize(d.stages, new JsonSerializerOptions { IncludeFields = true });
            Assert.That(EmailLink.Link(new FakeAuth(), d, "ahmed@example.com", "tok", false, null), Is.EqualTo(LinkResult.Linked));
            Assert.That(d.coins, Is.EqualTo(777));
            Assert.That(d.foodsCollected, Does.Contain("kabsa"));
            Assert.That(JsonSerializer.Serialize(d.stages, new JsonSerializerOptions { IncludeFields = true }), Is.EqualTo(before));
            Assert.That(d.linkedProviders, Does.Contain("email"));
            Assert.That(d.emailMasked, Is.EqualTo("a***@e***.com"));
            Assert.That(EmailLink.Link(new FakeAuth { Next = LinkResult.Conflict }, new SaveData(), "x@y.com", "t", false, null), Is.EqualTo(LinkResult.Conflict));
        }

        [Test]
        public void ChangingEmailMovesTheLink()
        {
            var d = new SaveData(); var a = new FakeAuth();
            EmailLink.Link(a, d, "old@example.com", "t1", false, null);
            Assert.That(EmailLink.Change(a, d, "new@mail.org", "t2", false, null), Is.EqualTo(LinkResult.Linked));
            Assert.That(a.Unlinks, Is.EqualTo(1));
            Assert.That(d.emailMasked, Is.EqualTo("n***@m***.org"));
        }

        [Test]
        public void EmailsNeverReachLogsInPlaintext()
        {
            string line = EmailAddress.ScrubLog("link failed for kid.name+x@school.edu.sa at 10:00");
            Assert.That(line, Does.Not.Contain("kid.name"));
            Assert.That(line, Does.Contain("k***@s***.sa"));
            Assert.That(Safety.ErrorReportScrub.Scrub("x ahmed@example.com", null), Does.Not.Contain("ahmed@"));
            Assert.That(EmailAddress.Hash("A@b.com", "p"), Is.EqualTo(EmailAddress.Hash("a@b.com ", "p")));
            Assert.That(EmailAddress.Hash("a@b.com", "p"), Does.Not.Contain("a@b"));
        }

        [Test]
        public void ChildNeedsTheGateAndAParentBeforeAnyCode()
        {
            var s = Store(); var d = new SaveData(); var flow = new ConsentFlow();
            int mails = 0;
            Assert.That(EmailSignIn.Request(s, "kid@b.com", "ip", "dev", T0, true, flow, (_, __) => mails++), Is.EqualTo(OtpIssue.ConsentRequired));
            Assert.That(flow.Request("p1", "mum@b.com", "ar", "pep", T0, null), Is.False, "the gate comes first");

            var gate = new ParentalGate(new Random(4));
            Assert.That(gate.Answer(2020, gate.a * gate.b, 2026, T0, new Random(5)), Is.False, "a child's birth year fails");
            Assert.That(gate.Answer(1985, gate.a * gate.b, 2026, T0, new Random(6)), Is.True);
            flow.PassGate();

            string token = null;
            Assert.That(flow.Request("p1", "mum@b.com", "ar", "pep", T0, (to, t) => token = t), Is.True);
            Assert.That(flow.Record.parentEmailHash, Is.Not.Empty.And.Not.Contains("mum"));
            Assert.That(EmailSignIn.Request(s, "kid@b.com", "ip", "dev", T0, true, flow, (_, __) => mails++), Is.EqualTo(OtpIssue.ConsentRequired), "no code while waiting");
            Assert.That(EmailLink.Link(new FakeAuth(), d, "kid@b.com", "t", true, flow), Is.EqualTo(LinkResult.Blocked));
            Assert.That(flow.Decide("wrong", true, "pep", T0), Is.False);
            Assert.That(flow.Decide(token, true, "pep", T0.AddHours(1)), Is.True);
            Assert.That(EmailSignIn.Request(s, "kid@b.com", "ip", "dev", T0.AddHours(1), true, flow, (_, __) => mails++), Is.EqualTo(OtpIssue.Sent));
            Assert.That(mails, Is.EqualTo(1));
            Assert.That(EmailLink.Link(new FakeAuth(), d, "kid@b.com", "t", true, flow), Is.EqualTo(LinkResult.Linked));

            Assert.That(flow.Revoke(d, T0.AddDays(2)), Is.True);
            Assert.That(d.linkedProviders, Does.Not.Contain("email"));
            Assert.That(d.emailMasked, Is.Empty);
            Assert.That(flow.Record.state, Is.EqualTo(ConsentState.Revoked));
        }

        [Test]
        public void DeclineKeepsTheChildAGuest()
        {
            var flow = new ConsentFlow(); flow.PassGate();
            string token = null;
            flow.Request("p1", "dad@b.com", "en", "pep", T0, (_, t) => token = t);
            flow.Decide(token, false, "pep", T0);
            Assert.That(EmailSignIn.Request(Store(), "kid@b.com", "i", "d", T0, true, flow, null), Is.EqualTo(OtpIssue.ConsentRequired));
        }

        [Test]
        public void GateLocksAfterThreeMisses()
        {
            var gate = new ParentalGate(new Random(1));
            for (int i = 0; i < 3; i++) gate.Answer(1980, -1, 2026, T0, new Random(i));
            Assert.That(gate.Locked(T0), Is.True);
            Assert.That(gate.Answer(1980, gate.a * gate.b, 2026, T0.AddMinutes(5), new Random(9)), Is.False);
            Assert.That(gate.Locked(T0.AddMinutes(10)), Is.False);
            Assert.That(gate.a, Is.InRange(6, 12));
        }

        [Test]
        public void ConsentAgeComesFromTheRegionTable()
        {
            string dir = TestContext.CurrentContext.TestDirectory;
            string path = null;
            for (var d = new DirectoryInfo(dir); d != null && path == null; d = d.Parent)
            {
                var p = Path.Combine(d.FullName, "Assets", "_Aron", "Resources", "Config", "consent_ages.json");
                if (File.Exists(p)) path = p;
            }
            Assert.That(path, Is.Not.Null);
            var ages = JsonSerializer.Deserialize<ConsentAges>(File.ReadAllText(path), new JsonSerializerOptions { IncludeFields = true });
            Assert.That(ages.Validate(), Is.Empty);
            Assert.That(ages.For("US"), Is.EqualTo(13));
            Assert.That(ages.For("de"), Is.EqualTo(16));
            Assert.That(ages.For("FR"), Is.EqualTo(15));
            Assert.That(ages.For("IN"), Is.EqualTo(18));
            Assert.That(ages.For("ZZ"), Is.EqualTo(13));
            Assert.That(ages.NeedsParent(14, "DE"), Is.True);
            Assert.That(ages.NeedsParent(14, "US"), Is.False);
            Assert.That(ages.NeedsParent(17, "IN"), Is.True);
        }
    }
}
