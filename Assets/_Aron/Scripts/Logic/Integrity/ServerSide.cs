// (c) Ahmed Alamrani. All rights reserved.
using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;
using Aron.Logic.Races;

namespace Aron.Logic.Integrity
{
    // ---------------------------------------------------------------- race results

    public class RaceReport
    {
        public string trackId, vehicleId;
        public float totalSeconds;
        /// <summary>One entry per lap (empty for point-to-point tracks).</summary>
        public float[] lapSeconds = new float[0];
    }

    /// <summary>Per-vehicle speed ceiling used to judge a result. The Cloud Code module runs this same class.</summary>
    public static class SpeedCaps
    {
        /// <summary>Highest believable speed for a vehicle: base speed x its top-speed trait x its ability allowance x the class tolerance.</summary>
        public static float MaxSpeed(VehicleDef v, VehicleClassDef c, float baseMetresPerSecond)
        {
            float top = v != null && v.stats != null ? v.stats.topSpeed : 1f;
            float tol = c != null ? 1f + c.rawTolerance : 1.04f;
            float ability = v != null && v.ability != null && v.ability.kind != "none" && v.ability.kind != "steady" ? 1.25f : 1f;
            return baseMetresPerSecond * top * ability * tol;
        }
    }

    public static class RaceResultValidator
    {
        /// <summary>Null when the result could really have been driven, otherwise the reason code.</summary>
        public static string Check(RaceReport r, RaceFile races, float maxSpeed)
        {
            if (r == null) return "missing_report";
            var t = races?.Track(r.trackId);
            if (t == null) return "unknown_track";
            if (maxSpeed <= 0f || float.IsNaN(maxSpeed)) return "bad_speed_cap";
            if (float.IsNaN(r.totalSeconds) || float.IsInfinity(r.totalSeconds) || r.totalSeconds <= 0f || r.totalSeconds >= 3600f) return "bad_total_time";
            if (r.totalSeconds < Safety.ScoreCheck.MinimumSeconds(t, races, maxSpeed) * 0.98f) return "implausible_time";
            if (t.laps > 0)
            {
                if (r.lapSeconds == null || r.lapSeconds.Length != t.laps) return "wrong_lap_count";
                float minLap = Safety.ScoreCheck.MinimumSeconds(t, races, maxSpeed) / t.laps * 0.98f, sum = 0f;
                foreach (var l in r.lapSeconds)
                {
                    if (float.IsNaN(l) || l < minLap) return "impossible_lap";
                    sum += l;
                }
                if (Math.Abs(sum - r.totalSeconds) > 0.5f) return "laps_do_not_add_up";
            }
            return null;
        }
    }

    // ---------------------------------------------------------------- authoritative economy

    public class GrantOutcome
    {
        public bool Accepted;
        public string Reason = "";
        public PointsAward Award;
        public int ServerPoints;
    }

    /// <summary>
    /// The rules the Cloud Code module applies. The server keeps one <see cref="SaveData"/>-shaped record per player and a clock of
    /// its own; the phone only asks. Points, daily caps and rentals are decided here, using the same classes the game uses offline.
    /// </summary>
    public class ServerEconomy
    {
        private readonly PointsTable table;
        private readonly RaceFile races;
        private readonly StoreCatalog catalog;
        private readonly ClaimValidator claims;

        public ServerEconomy(PointsTable table, RaceFile races, StoreCatalog catalog, string salt)
        {
            this.table = table; this.races = races; this.catalog = catalog; claims = new ClaimValidator(races, salt);
        }

        public GrantOutcome GrantOnlineRace(SaveData player, RaceClaim claim, RaceReport report, float maxSpeed, DateTime serverUtc)
        {
            var o = new GrantOutcome { ServerPoints = player.racePoints };
            string why = RaceResultValidator.Check(report, races, maxSpeed);
            if (why == null && claim != null && (report.trackId != claim.track || Math.Abs(report.totalSeconds - claim.seconds) > 0.05f)) why = "report_does_not_match_claim";
            if (why == null) why = claims.Check(claim, maxSpeed);
            if (why != null) { o.Reason = why; return o; }
            var a = RacePoints.Grant(player, table, PointsSource.Online, claim.difficulty, claim.track, claim.place, claim.realRacers, serverUtc, true);
            o.Award = a; o.Accepted = a.Total > 0; o.Reason = a.reason ?? ""; o.ServerPoints = player.racePoints;
            return o;
        }

        /// <summary>Rents with the server's time, so changing the phone clock changes nothing.</summary>
        public StoreInventory.RentResult Rent(SaveData player, string itemId, DateTime serverUtc)
        {
            var inv = new StoreInventory(catalog, player) { Clock = () => serverUtc };
            return inv.Rent(itemId);
        }

        public bool RentalActive(SaveData player, string itemId, DateTime serverUtc)
        {
            var inv = new StoreInventory(catalog, player) { Clock = () => serverUtc };
            return inv.IsRented(itemId);
        }

        /// <summary>When the phone and the server disagree about a balance, the server's number wins.</summary>
        public static int Reconcile(int clientClaim, int serverValue) => serverValue;
    }

    // ---------------------------------------------------------------- purchases

    public enum ReceiptVerdict { Valid, Invalid, Duplicate, WrongProduct, WrongApp, Unreachable }

    public class PurchaseReceipt
    {
        public string store, productId, transactionId, packageName, payload, signature;
        /// <summary>Google Play: 0 = purchased. Anything else is pending or cancelled.</summary>
        public int purchaseState;
    }

    /// <summary>Asked before a paid item is granted. The game gets its answer from the Cloud Code module (see docs/security.md).</summary>
    public interface IPurchaseVerifier
    {
        void Verify(PurchaseReceipt receipt, Action<ReceiptVerdict> done);
    }

    public static class PurchaseGate
    {
        /// <summary>Null until the backend exists: purchases then fall back to the store's own signed result only.</summary>
        public static IPurchaseVerifier Verifier;
    }

    /// <summary>The checks a server does on a receipt once it has the store's answer.</summary>
    public class ReceiptPolicy
    {
        private readonly StoreCatalog catalog;
        private readonly string expectedPackage;
        private readonly HashSet<string> seen = new HashSet<string>();

        public ReceiptPolicy(StoreCatalog catalog, string expectedPackage) { this.catalog = catalog; this.expectedPackage = expectedPackage; }

        public ReceiptVerdict Check(PurchaseReceipt r)
        {
            if (r == null || string.IsNullOrEmpty(r.transactionId)) return ReceiptVerdict.Invalid;
            if (catalog.FindByProductId(r.productId) == null) return ReceiptVerdict.WrongProduct;
            if (!string.IsNullOrEmpty(r.packageName) && r.packageName != expectedPackage) return ReceiptVerdict.WrongApp;
            if (r.store == "GooglePlay" && r.purchaseState != 0) return ReceiptVerdict.Invalid;
            return seen.Add(r.store + "|" + r.transactionId) ? ReceiptVerdict.Valid : ReceiptVerdict.Duplicate;
        }
    }

#if NET5_0_OR_GREATER
    /// <summary>Google Play receipts carry a signed JSON string: RSA-SHA1 (PKCS#1) with the public key from Play Console.</summary>
    public static class GooglePlaySignature
    {
        public static bool Verify(string signedJson, string signatureBase64, byte[] publicKeySpki)
        {
            try
            {
                using (var rsa = RSA.Create())
                {
                    rsa.ImportSubjectPublicKeyInfo(publicKeySpki, out _);
                    return rsa.VerifyData(Encoding.UTF8.GetBytes(signedJson), Convert.FromBase64String(signatureBase64), HashAlgorithmName.SHA1, RSASignaturePadding.Pkcs1);
                }
            }
            catch (Exception e) when (e is CryptographicException || e is FormatException) { return false; }
        }
    }
#endif

    // ---------------------------------------------------------------- device attestation

    public enum DeviceTrust { Unknown, Verified, Failed }

    public class IntegrityToken
    {
        /// <summary>"play_integrity", "app_attest" or "none".</summary>
        public string provider = "none";
        public string token = "";
        public string error = "";
    }

    /// <summary>
    /// Google Play Integrity (Android) and Apple App Attest (iOS) sit behind this. Both need accounts that only the owner has:
    /// a Google Cloud project linked in Play Console, and the App Attest capability on the App ID. Until then <see cref="NoIntegrity"/> is used.
    /// </summary>
    public interface IIntegrityProvider
    {
        bool Available { get; }
        /// <summary>Asks the OS for a token bound to <paramref name="nonce"/>. The server verifies it with Google or Apple.</summary>
        void Request(string nonce, Action<IntegrityToken> done);
    }

    public sealed class NoIntegrity : IIntegrityProvider
    {
        public bool Available => false;
        public void Request(string nonce, Action<IntegrityToken> done) { done(new IntegrityToken { error = "not_configured" }); }
    }

    public static class IntegrityPolicy
    {
        /// <summary>Base64url nonce tying a token to one player, match and server challenge, so it can't be replayed.</summary>
        public static string Nonce(string playerId, string matchId, string serverChallenge)
        {
            using (var h = SHA256.Create())
            {
                var d = h.ComputeHash(Encoding.UTF8.GetBytes(playerId + "|" + matchId + "|" + serverChallenge));
                return Convert.ToBase64String(d).TrimEnd('=').Replace('+', '-').Replace('/', '_');
            }
        }

        /// <summary>Ranked points need a verified device; casual play never does. An unconfigured provider is "unknown", not "failed".</summary>
        public static bool RankedAllowed(DeviceTrust t, bool softFlags) => t == DeviceTrust.Verified || (t == DeviceTrust.Unknown && !softFlags);

        public static bool OfflinePlayAllowed => true;
    }

    // ---------------------------------------------------------------- rate limiting

    /// <summary>Token bucket per (endpoint, key). The names and reports endpoints use it; the server clock is passed in.</summary>
    public class RateLimiter
    {
        public class Rule { public int burst; public double perHour; }

        private class Bucket { public double tokens; public DateTime at; public int strikes; public DateTime blockedUntil; }

        private readonly Dictionary<string, Rule> rules = new Dictionary<string, Rule>();
        private readonly Dictionary<string, Bucket> buckets = new Dictionary<string, Bucket>();

        public static RateLimiter Default()
        {
            var r = new RateLimiter();
            r.Set("name_check", 10, 30);    // typing a name tries several spellings
            r.Set("name_change", 3, 3);
            r.Set("report", 5, 10);
            r.Set("race_claim", 12, 30);
            r.Set("purchase_verify", 10, 20);
            return r;
        }

        public void Set(string endpoint, int burst, double perHour) { rules[endpoint] = new Rule { burst = burst, perHour = perHour }; }

        /// <summary>True when the call may go ahead. Repeated refusals lock the key out for longer each time (up to 24 hours).</summary>
        public bool Allow(string endpoint, string key, DateTime now)
        {
            if (!rules.TryGetValue(endpoint, out var rule)) return true;
            string id = endpoint + "|" + key;
            if (!buckets.TryGetValue(id, out var b)) buckets[id] = b = new Bucket { tokens = rule.burst, at = now };
            if (now < b.blockedUntil) return false;
            double add = (now - b.at).TotalHours * rule.perHour;
            if (add > 0) { b.tokens = Math.Min(rule.burst, b.tokens + add); b.at = now; }
            if (b.tokens >= 1) { b.tokens -= 1; if (b.strikes > 0 && b.tokens >= rule.burst - 1) b.strikes = 0; return true; }
            b.strikes++;
            b.blockedUntil = now.AddMinutes(Math.Min(24 * 60, 5 * Math.Pow(2, b.strikes - 1)));
            return false;
        }

        /// <summary>Drops idle keys so the table doesn't grow forever.</summary>
        public int Prune(DateTime now)
        {
            var dead = new List<string>();
            foreach (var kv in buckets) if ((now - kv.Value.at).TotalHours > 24 && now > kv.Value.blockedUntil) dead.Add(kv.Key);
            foreach (var k in dead) buckets.Remove(k);
            return dead.Count;
        }
    }
}
