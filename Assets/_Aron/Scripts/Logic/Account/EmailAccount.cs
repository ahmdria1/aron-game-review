// (c) Ahmed Alamrani. All rights reserved.
using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;

namespace Aron.Logic.Account
{
    /// <summary>
    /// Passwordless email sign-in. The client only ever sends an address and a typed code; codes are made, hashed,
    /// checked and mailed on the server (Cloud Code module AronAccount), which shares this class so the rules
    /// are the same on both sides. The mail provider and its API key live in server-side secrets only.
    /// </summary>
    public static class EmailAddress
    {
        private static readonly Regex Shape = new Regex(@"^[A-Za-z0-9._%+\-]{1,64}@[A-Za-z0-9\-]+(\.[A-Za-z0-9\-]+)*\.[A-Za-z]{2,24}$", RegexOptions.CultureInvariant);

        public static string Normalize(string email) => (email ?? "").Trim().ToLowerInvariant();

        public static bool Valid(string email)
        {
            var e = Normalize(email);
            return e.Length <= 254 && Shape.IsMatch(e) && !e.Contains("..");
        }

        /// <summary>What the profile and every log line show: a***@g***.com.</summary>
        public static string Mask(string email)
        {
            var e = Normalize(email);
            int at = e.IndexOf('@');
            if (at <= 0) return "***";
            int dot = e.LastIndexOf('.');
            string domain = dot > at ? e.Substring(at + 1, dot - at - 1) : e.Substring(at + 1);
            string tld = dot > at ? e.Substring(dot) : "";
            return e[0] + "***@" + (domain.Length > 0 ? domain[0].ToString() : "") + "***" + tld;
        }

        /// <summary>Salted SHA-256, hex. The server's pepper comes from its secrets; the address itself is never stored in consent records.</summary>
        public static string Hash(string email, string pepper)
        {
            using (var sha = SHA256.Create())
            {
                var bytes = sha.ComputeHash(Encoding.UTF8.GetBytes((pepper ?? "") + "|" + Normalize(email)));
                var sb = new StringBuilder(64);
                foreach (var b in bytes) sb.Append(b.ToString("x2"));
                return sb.ToString();
            }
        }

        private static readonly Regex Any = new Regex(@"[A-Za-z0-9._%+\-]+@[A-Za-z0-9.\-]+\.[A-Za-z]{2,}", RegexOptions.CultureInvariant);

        /// <summary>Every address in a log line, analytics event or error report is replaced by its mask.</summary>
        public static string ScrubLog(string text) => string.IsNullOrEmpty(text) ? text : Any.Replace(text, m => Mask(m.Value));
    }

    public enum OtpIssue { Sent, Cooldown, RateLimited, InvalidEmail, ConsentRequired }
    public enum OtpCheck { Ok, Wrong, Expired, TooManyAttempts, NoCode }

    /// <summary>A sliding one-hour window per key (email, IP, device).</summary>
    public class RateLimiter
    {
        private readonly Dictionary<string, List<DateTime>> hits = new Dictionary<string, List<DateTime>>();
        public static readonly TimeSpan Window = TimeSpan.FromHours(1);

        public bool Allow(string key, int max, DateTime now)
        {
            if (string.IsNullOrEmpty(key)) return true;
            if (!hits.TryGetValue(key, out var list)) hits[key] = list = new List<DateTime>();
            list.RemoveAll(t => now - t >= Window);
            if (list.Count >= max) return false;
            list.Add(now);
            return true;
        }
    }

    /// <summary>
    /// Six-digit codes: valid 10 minutes, 5 tries, a new code at most every 60 s; at most 5 codes per address,
    /// 20 per IP and 10 per device an hour. Only a hash of the code is kept.
    /// </summary>
    public class OtpStore
    {
        public const int Digits = 6, MaxAttempts = 5;
        public static readonly TimeSpan Life = TimeSpan.FromMinutes(10), ResendAfter = TimeSpan.FromSeconds(60);
        public const int PerEmailHour = 5, PerIpHour = 20, PerDeviceHour = 10;

        private class Pending { public string hash; public DateTime issued; public int attempts; }

        private readonly Dictionary<string, Pending> pending = new Dictionary<string, Pending>();
        private readonly RateLimiter limits = new RateLimiter();
        private readonly string pepper;
        /// <summary>Makes the code; tests swap it for a fixed one.</summary>
        public Func<string> NewCode = RandomCode;

        public OtpStore(string pepper) { this.pepper = pepper ?? ""; }

        public static string RandomCode()
        {
            int n = RandomNumberGenerator.GetInt32(0, 1000000);
            return n.ToString("D6");
        }

        /// <summary>Makes a code for the address and hands it to <paramref name="mail"/> (the provider abstraction).</summary>
        public OtpIssue Issue(string email, string ip, string device, DateTime now, Action<string, string> mail)
        {
            if (!EmailAddress.Valid(email)) return OtpIssue.InvalidEmail;
            string key = EmailAddress.Hash(email, pepper);
            if (pending.TryGetValue(key, out var p) && now - p.issued < ResendAfter) return OtpIssue.Cooldown;
            if (!limits.Allow("e:" + key, PerEmailHour, now) || !limits.Allow("i:" + ip, PerIpHour, now) || !limits.Allow("d:" + device, PerDeviceHour, now))
                return OtpIssue.RateLimited;
            string code = NewCode();
            pending[key] = new Pending { hash = EmailAddress.Hash(code, pepper + key), issued = now };
            mail?.Invoke(EmailAddress.Normalize(email), code);
            return OtpIssue.Sent;
        }

        public OtpCheck Verify(string email, string code, DateTime now)
        {
            string key = EmailAddress.Hash(email, pepper);
            if (!pending.TryGetValue(key, out var p)) return OtpCheck.NoCode;
            if (now - p.issued >= Life) { pending.Remove(key); return OtpCheck.Expired; }
            if (p.attempts >= MaxAttempts) { pending.Remove(key); return OtpCheck.TooManyAttempts; }
            p.attempts++;
            if (!FixedTimeEquals(p.hash, EmailAddress.Hash((code ?? "").Trim(), pepper + key)))
            {
                if (p.attempts >= MaxAttempts) { pending.Remove(key); return OtpCheck.TooManyAttempts; }
                return OtpCheck.Wrong;
            }
            pending.Remove(key);
            return OtpCheck.Ok;
        }

        private static bool FixedTimeEquals(string a, string b)
        {
            if (a.Length != b.Length) return false;
            int diff = 0;
            for (int i = 0; i < a.Length; i++) diff |= a[i] ^ b[i];
            return diff == 0;
        }
    }

    /// <summary>
    /// The age of digital consent by country (ISO 3166 alpha-2), loaded from Resources/Config/consent_ages.json.
    /// 13 by default (COPPA); GDPR Art. 8 lets EU states pick 13 to 16; India (DPDP Act 2023) and a few others say 18.
    /// </summary>
    [Serializable]
    public class ConsentAges
    {
        public int defaultAge = 13;
        public ConsentAge[] countries = Array.Empty<ConsentAge>();

        public int For(string iso)
        {
            string c = (iso ?? "").Trim().ToUpperInvariant();
            foreach (var e in countries ?? Array.Empty<ConsentAge>()) if (e.iso == c) return e.age;
            return defaultAge;
        }

        public bool NeedsParent(int age, string iso) => age < For(iso);

        public List<string> Validate()
        {
            var errors = new List<string>();
            if (defaultAge < 13 || defaultAge > 18) errors.Add("default age is 13..18");
            var seen = new HashSet<string>();
            foreach (var e in countries ?? Array.Empty<ConsentAge>())
            {
                if (e.iso == null || e.iso.Length != 2 || !seen.Add(e.iso)) errors.Add("bad or duplicate country " + e.iso);
                if (e.age < 13 || e.age > 18) errors.Add(e.iso + ": consent age 13..18");
            }
            return errors;
        }
    }

    [Serializable]
    public class ConsentAge { public string iso; public int age; }

    /// <summary>
    /// The grown-up check before a parent's email is asked for: the parent's birth year (must make them 18+) and a
    /// times-table question with two numbers from 6 to 12, typed as a number. Three misses lock it for 10 minutes.
    /// </summary>
    public class ParentalGate
    {
        public const int MaxMisses = 3;
        public static readonly TimeSpan Lockout = TimeSpan.FromMinutes(10);
        public int a, b;
        private int misses;
        private DateTime lockedUntil = DateTime.MinValue;

        public ParentalGate(Random rnd) { New(rnd); }

        public void New(Random rnd) { a = rnd.Next(6, 13); b = rnd.Next(6, 13); }

        public bool Locked(DateTime now) => now < lockedUntil;

        public bool Answer(int birthYear, int product, int thisYear, DateTime now, Random rnd)
        {
            if (Locked(now)) return false;
            int age = thisYear - birthYear;
            bool ok = age >= 18 && age <= 110 && product == a * b;
            if (ok) { misses = 0; return true; }
            if (++misses >= MaxMisses) { lockedUntil = now + Lockout; misses = 0; }
            New(rnd);
            return false;
        }
    }

    public enum ConsentState { None, GatePassed, Requested, Approved, Declined, Revoked }

    /// <summary>
    /// What the server keeps for a child's email link: when, which version of the notice, and a salted hash of the
    /// parent's address (never the address). Revocable from the email and from Settings > Family.
    /// </summary>
    [Serializable]
    public class ConsentRecord
    {
        public string playerId = "";
        public string parentEmailHash = "";
        public string language = "en";
        public string noticeVersion = ConsentFlow.NoticeVersion;
        public long requestedAt, decidedAt;
        public ConsentState state = ConsentState.None;
        /// <summary>Random token in the Approve / Decline / Revoke links; only its hash is stored server-side.</summary>
        public string tokenHash = "";
    }

    /// <summary>
    /// Under the consent age the email link waits for a parent: gate, parent's email, consent mail with
    /// Approve/Decline, and only then a code to the child's address. The child plays as a guest the whole time
    /// with full progress.
    /// </summary>
    public class ConsentFlow
    {
        public const string NoticeVersion = "2026-10";
        public ConsentRecord Record { get; } = new ConsentRecord();

        public bool GatePassed => Record.state != ConsentState.None;
        public bool Approved => Record.state == ConsentState.Approved;

        public void PassGate() { if (Record.state == ConsentState.None) Record.state = ConsentState.GatePassed; }

        /// <summary>Parent's address entered: the consent mail goes out in <paramref name="language"/>.</summary>
        public bool Request(string playerId, string parentEmail, string language, string pepper, DateTime now, Action<string, string> sendConsentMail)
        {
            if (Record.state == ConsentState.None || !EmailAddress.Valid(parentEmail)) return false;
            Record.playerId = playerId;
            Record.parentEmailHash = EmailAddress.Hash(parentEmail, pepper);
            Record.language = string.IsNullOrEmpty(language) ? "en" : language;
            Record.requestedAt = now.Ticks;
            Record.state = ConsentState.Requested;
            string token = Guid.NewGuid().ToString("N");
            Record.tokenHash = EmailAddress.Hash(token, pepper);
            sendConsentMail?.Invoke(EmailAddress.Normalize(parentEmail), token);
            return true;
        }

        /// <summary>The parent's click on Approve or Decline in the mail.</summary>
        public bool Decide(string token, bool approve, string pepper, DateTime now)
        {
            if (Record.state != ConsentState.Requested || EmailAddress.Hash(token, pepper) != Record.tokenHash) return false;
            Record.state = approve ? ConsentState.Approved : ConsentState.Declined;
            Record.decidedAt = now.Ticks;
            return true;
        }

        /// <summary>Revoke from the mail link (with its token) or from Settings > Family (behind the gate): unlinks the child's email.</summary>
        public bool Revoke(SaveData data, DateTime now)
        {
            if (Record.state != ConsentState.Approved) return false;
            Record.state = ConsentState.Revoked;
            Record.decidedAt = now.Ticks;
            EmailLink.Unlink(data);
            return true;
        }
    }

    public enum LinkResult { Linked, Conflict, Failed, Blocked }

    /// <summary>Unity Authentication behind an interface: the email is a custom OpenID Connect provider.</summary>
    public interface IAuthBackend
    {
        string PlayerId { get; }
        /// <summary>Links the id token the Cloud Code module minted after a good code. Conflict when that email already belongs to another player.</summary>
        LinkResult LinkOpenId(string idProvider, string idToken);
        void UnlinkOpenId(string idProvider);
    }

    public static class EmailLink
    {
        /// <summary>The Unity Authentication OpenID Connect provider name (set up in the dashboard).</summary>
        public const string Provider = "oidc-aron-email";

        /// <summary>
        /// Links a verified email to the signed-in (often guest) player. Progress is untouched: only the masked
        /// address and the provider list change. A conflict goes to the choose-a-save screen (SaveMerge).
        /// Children under the consent age need an approved <see cref="ConsentFlow"/>.
        /// </summary>
        public static LinkResult Link(IAuthBackend auth, SaveData data, string email, string idToken, bool needsParent, ConsentFlow consent)
        {
            if (needsParent && (consent == null || !consent.Approved)) return LinkResult.Blocked;
            var r = auth.LinkOpenId(Provider, idToken);
            if (r != LinkResult.Linked) return r;
            if (data.linkedProviders == null) data.linkedProviders = new List<string>();
            if (!data.linkedProviders.Contains("email")) data.linkedProviders.Add("email");
            data.emailMasked = EmailAddress.Mask(email);
            return r;
        }

        /// <summary>Changing the address: the new one is verified first; then the link moves over.</summary>
        public static LinkResult Change(IAuthBackend auth, SaveData data, string newEmail, string newToken, bool needsParent, ConsentFlow consent)
        {
            if (needsParent && (consent == null || !consent.Approved)) return LinkResult.Blocked;
            auth.UnlinkOpenId(Provider);
            return Link(auth, data, newEmail, newToken, needsParent, consent);
        }

        public static void Unlink(SaveData data)
        {
            data.linkedProviders?.Remove("email");
            data.emailMasked = "";
        }

        public static void Unlink(IAuthBackend auth, SaveData data)
        {
            auth.UnlinkOpenId(Provider);
            Unlink(data);
        }
    }

    /// <summary>
    /// The sign-in screen's checks before any request leaves the phone. A child under the consent age gets no code
    /// until the parent has approved.
    /// </summary>
    public static class EmailSignIn
    {
        public static OtpIssue Request(OtpStore server, string email, string ip, string device, DateTime now, bool needsParent, ConsentFlow consent, Action<string, string> mail)
        {
            if (needsParent && (consent == null || !consent.Approved)) return OtpIssue.ConsentRequired;
            return server.Issue(email, ip, device, now, mail);
        }
    }
}
