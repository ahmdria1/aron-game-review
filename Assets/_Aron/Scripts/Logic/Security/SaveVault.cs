// (c) Ahmed Alamrani. All rights reserved.
using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace Aron.Logic.Security
{
    public enum OpenResult { Ok, BadFormat, Tampered, WrongKey, UnsupportedVersion }

    /// <summary>Authenticated encryption. Two implementations: AES-256-GCM, and AES-256-CBC with an HMAC-SHA256 tag (encrypt-then-MAC).</summary>
    public interface IAead
    {
        byte Id { get; }
        int NonceSize { get; }
        int TagSize { get; }
        /// <summary>Returns ciphertext and the tag. <paramref name="associated"/> is authenticated but not encrypted.</summary>
        void Seal(byte[] key, byte[] nonce, byte[] plain, byte[] associated, out byte[] cipher, out byte[] tag);
        /// <summary>False when the tag does not match (wrong key, or any byte changed).</summary>
        bool Open(byte[] key, byte[] nonce, byte[] cipher, byte[] tag, byte[] associated, out byte[] plain);
    }

#if NET5_0_OR_GREATER
    /// <summary>AES-256-GCM. Used where the runtime has it (.NET 5+: tests, Cloud Code). Unity's player may not; see <see cref="Aeads.Default"/>.</summary>
    public sealed class AesGcmAead : IAead
    {
        public byte Id => 1;
        public int NonceSize => 12;
        public int TagSize => 16;

        public void Seal(byte[] key, byte[] nonce, byte[] plain, byte[] associated, out byte[] cipher, out byte[] tag)
        {
            cipher = new byte[plain.Length]; tag = new byte[TagSize];
            using (var g = new AesGcm(key, TagSize)) g.Encrypt(nonce, plain, cipher, tag, associated);
        }

        public bool Open(byte[] key, byte[] nonce, byte[] cipher, byte[] tag, byte[] associated, out byte[] plain)
        {
            plain = new byte[cipher.Length];
            try { using (var g = new AesGcm(key, TagSize)) g.Decrypt(nonce, cipher, tag, plain, associated); return true; }
            catch (CryptographicException) { plain = null; return false; }
        }
    }
#endif

    /// <summary>AES-256-CBC + HMAC-SHA256 over (associated data, iv, ciphertext). Works on every Unity platform.</summary>
    public sealed class AesCbcHmacAead : IAead
    {
        public byte Id => 2;
        public int NonceSize => 16;
        public int TagSize => 32;

        private static void Split(byte[] key, out byte[] enc, out byte[] mac)
        {
            using (var h = new HMACSHA256(key))
            {
                enc = h.ComputeHash(Encoding.ASCII.GetBytes("enc"));
                mac = h.ComputeHash(Encoding.ASCII.GetBytes("mac"));
            }
        }

        private static byte[] Tag(byte[] mac, byte[] associated, byte[] nonce, byte[] cipher)
        {
            using (var h = new HMACSHA256(mac))
            {
                h.TransformBlock(associated, 0, associated.Length, null, 0);
                h.TransformBlock(nonce, 0, nonce.Length, null, 0);
                h.TransformFinalBlock(cipher, 0, cipher.Length);
                return h.Hash;
            }
        }

        public void Seal(byte[] key, byte[] nonce, byte[] plain, byte[] associated, out byte[] cipher, out byte[] tag)
        {
            Split(key, out var enc, out var mac);
            using (var aes = Aes.Create())
            {
                aes.Key = enc; aes.IV = nonce; aes.Mode = CipherMode.CBC; aes.Padding = PaddingMode.PKCS7;
                using (var t = aes.CreateEncryptor()) cipher = t.TransformFinalBlock(plain, 0, plain.Length);
            }
            tag = Tag(mac, associated, nonce, cipher);
        }

        public bool Open(byte[] key, byte[] nonce, byte[] cipher, byte[] tag, byte[] associated, out byte[] plain)
        {
            plain = null;
            Split(key, out var enc, out var mac);
            if (!FixedEquals(Tag(mac, associated, nonce, cipher), tag)) return false;
            try
            {
                using (var aes = Aes.Create())
                {
                    aes.Key = enc; aes.IV = nonce; aes.Mode = CipherMode.CBC; aes.Padding = PaddingMode.PKCS7;
                    using (var t = aes.CreateDecryptor()) plain = t.TransformFinalBlock(cipher, 0, cipher.Length);
                }
                return true;
            }
            catch (CryptographicException) { return false; }
        }

        internal static bool FixedEquals(byte[] a, byte[] b)
        {
            if (a == null || b == null || a.Length != b.Length) return false;
            int d = 0;
            for (int i = 0; i < a.Length; i++) d |= a[i] ^ b[i];
            return d == 0;
        }
    }

    public static class Aeads
    {
        /// <summary>The cipher new saves are written with. GCM when the runtime offers it, otherwise the CBC+HMAC construction.</summary>
        public static IAead Default
        {
            get
            {
#if NET5_0_OR_GREATER
                return new AesGcmAead();
#else
                return new AesCbcHmacAead();
#endif
            }
        }

        public static IAead ById(byte id)
        {
            if (id == 2) return new AesCbcHmacAead();
#if NET5_0_OR_GREATER
            if (id == 1) return new AesGcmAead();
#endif
            return null;
        }
    }

    /// <summary>
    /// The file envelope: "ARN" 0x02 | format | algorithm | nonce | tag | ciphertext.
    /// The first 6 bytes (magic, format, algorithm) are authenticated as associated data, so the header can't be swapped.
    /// </summary>
    public static class Envelope
    {
        public const byte Format = 2;
        private static readonly byte[] Magic = { (byte)'A', (byte)'R', (byte)'N' };

        public static byte[] Seal(byte[] plain, byte[] key, IAead aead = null, Func<int, byte[]> random = null)
        {
            aead = aead ?? Aeads.Default;
            var nonce = random != null ? random(aead.NonceSize) : RandomBytes(aead.NonceSize);
            var head = new[] { Magic[0], Magic[1], Magic[2], Format, aead.Id };
            aead.Seal(key, nonce, plain, head, out var cipher, out var tag);
            using (var ms = new MemoryStream())
            {
                ms.Write(head, 0, head.Length);
                ms.Write(nonce, 0, nonce.Length);
                ms.Write(tag, 0, tag.Length);
                ms.Write(cipher, 0, cipher.Length);
                return ms.ToArray();
            }
        }

        public static bool LooksSealed(byte[] blob) => blob != null && blob.Length > 5 && blob[0] == Magic[0] && blob[1] == Magic[1] && blob[2] == Magic[2];

        public static OpenResult Open(byte[] blob, byte[] key, out byte[] plain)
        {
            plain = null;
            if (!LooksSealed(blob)) return OpenResult.BadFormat;
            if (blob[3] > Format) return OpenResult.UnsupportedVersion;
            var aead = Aeads.ById(blob[4]);
            if (aead == null) return OpenResult.UnsupportedVersion;
            int head = 5, n = aead.NonceSize, t = aead.TagSize;
            if (blob.Length < head + n + t) return OpenResult.BadFormat;
            var nonce = new byte[n]; var tag = new byte[t]; var cipher = new byte[blob.Length - head - n - t];
            Buffer.BlockCopy(blob, head, nonce, 0, n);
            Buffer.BlockCopy(blob, head + n, tag, 0, t);
            Buffer.BlockCopy(blob, head + n + t, cipher, 0, cipher.Length);
            var assoc = new byte[head]; Buffer.BlockCopy(blob, 0, assoc, 0, head);
            return aead.Open(key, nonce, cipher, tag, assoc, out plain) ? OpenResult.Ok : OpenResult.Tampered;
        }

        public static byte[] RandomBytes(int n)
        {
            var b = new byte[n];
            using (var r = RandomNumberGenerator.Create()) r.GetBytes(b);
            return b;
        }
    }

    /// <summary>
    /// Builds the per-install save key. <c>installSecret</c> is random bytes made on first run and kept by the platform wrapper
    /// (Android Keystore / iOS Keychain) so it never sits in the save folder. Without a platform store, the fallback mixes
    /// the device id with an obfuscated salt compiled into the game (see <see cref="IKeyStore"/>).
    /// </summary>
    public static class SaveKey
    {
        private static readonly byte[] SaltBlob = { 0x3B, 0xA1, 0x7C, 0x52, 0xE9, 0x14, 0x88, 0xD3, 0x6F, 0x20, 0xB5, 0x4A, 0x9E, 0x01, 0xC7, 0x73 };
        private static readonly byte[] SaltMask = { 0x91, 0x2E, 0xF4, 0x0B, 0x66, 0xD8, 0x35, 0xAC };

        /// <summary>The compiled-in salt, un-masked at run time so it isn't a plain literal in the binary.</summary>
        public static byte[] Salt()
        {
            var s = new byte[SaltBlob.Length];
            for (int i = 0; i < s.Length; i++) s[i] = (byte)(SaltBlob[i] ^ SaltMask[i % SaltMask.Length]);
            return s;
        }

        public static byte[] Derive(byte[] installSecret, string deviceId, int iterations = 20000)
        {
            var salt = Salt();
            var pwd = new byte[(installSecret?.Length ?? 0) + 1 + Encoding.UTF8.GetByteCount(deviceId ?? "")];
            int o = 0;
            if (installSecret != null) { Buffer.BlockCopy(installSecret, 0, pwd, 0, installSecret.Length); o = installSecret.Length; }
            pwd[o++] = 0x7C;
            var id = Encoding.UTF8.GetBytes(deviceId ?? ""); Buffer.BlockCopy(id, 0, pwd, o, id.Length);
            using (var kdf = new Rfc2898DeriveBytes(pwd, salt, iterations, HashAlgorithmName.SHA256)) return kdf.GetBytes(32);
        }
    }

    /// <summary>The platform wrapper: Android Keystore or iOS Keychain hold the per-install secret. A native plugin implements this.</summary>
    public interface IKeyStore
    {
        /// <summary>The stored install secret, or null when none yet.</summary>
        byte[] Load();
        void Save(byte[] secret);
        /// <summary>A stable id for this device/install used by the fallback.</summary>
        string DeviceId { get; }
    }

    /// <summary>In-memory key store (tests, and the fallback when no native wrapper is installed).</summary>
    public sealed class MemoryKeyStore : IKeyStore
    {
        private byte[] secret;
        public MemoryKeyStore(string deviceId = "device") { DeviceId = deviceId; }
        public string DeviceId { get; }
        public byte[] Load() => secret;
        public void Save(byte[] s) { secret = s; }
    }

    public enum LoadSource { None, Main, Backup, Legacy }

    public class LoadOutcome
    {
        public string Json;
        public LoadSource Source;
        /// <summary>True when the main file was damaged or altered and the backup (or nothing) was used.</summary>
        public bool Recovered;
        /// <summary>True when the data came from an old plaintext save: the caller should save once to re-write it sealed.</summary>
        public bool NeedsRewrite;
        public string Note = "";
    }

    /// <summary>
    /// Sealed, atomically written save with a backup slot. Write: seal to a temp file, move the current file to the backup
    /// (only if it still opens), then swap the temp in. Read: main, else backup, else an old plaintext save.
    /// </summary>
    public class SecureSaveStore
    {
        private readonly string path;
        private readonly IKeyStore keys;
        private readonly IAead aead;
        private byte[] key;

        public SecureSaveStore(string path, IKeyStore keys, IAead aead = null) { this.path = path; this.keys = keys; this.aead = aead; }

        public string BackupPath => path + ".bak";
        public string LegacyCopyPath => path + ".plain.old";
        public string SealedMarkerPath => path + ".sealed";

        private byte[] Key()
        {
            if (key != null) return key;
            var secret = keys.Load();
            if (secret == null) { secret = Envelope.RandomBytes(32); keys.Save(secret); }
            return key = SaveKey.Derive(secret, keys.DeviceId);
        }

        public void Write(string json)
        {
            var blob = Envelope.Seal(Encoding.UTF8.GetBytes(json), Key(), aead);
            Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(path)));
            string tmp = path + ".tmp";
            File.WriteAllBytes(tmp, blob);
            if (File.Exists(path) && TryOpenFile(path, out _) == OpenResult.Ok) File.Copy(path, BackupPath, true);
            if (File.Exists(path)) File.Replace(tmp, path, null); else File.Move(tmp, path);
            // From now on a plaintext file at this path is refused (it would be a downgrade), see TryLegacy.
            try { File.WriteAllBytes(SealedMarkerPath, new byte[0]); } catch (IOException) { }
        }

        private OpenResult TryOpenFile(string p, out string json)
        {
            json = null;
            byte[] blob;
            try { blob = File.ReadAllBytes(p); } catch (IOException) { return OpenResult.BadFormat; }
            var r = Envelope.Open(blob, Key(), out var plain);
            if (r == OpenResult.Ok) json = Encoding.UTF8.GetString(plain);
            return r;
        }

        public LoadOutcome Read()
        {
            var o = new LoadOutcome { Source = LoadSource.None };
            if (File.Exists(path))
            {
                var r = TryOpenFile(path, out var json);
                if (r == OpenResult.Ok) { o.Json = json; o.Source = LoadSource.Main; return o; }
                if (r == OpenResult.BadFormat && !File.Exists(SealedMarkerPath) && TryLegacy(path, o)) return o;
                o.Recovered = true; o.Note = "main save rejected: " + r;
            }
            if (File.Exists(BackupPath) && TryOpenFile(BackupPath, out var bj) == OpenResult.Ok)
            {
                o.Json = bj; o.Source = LoadSource.Backup; o.Recovered = true; o.NeedsRewrite = true; o.Note += " restored from backup";
                return o;
            }
            return o;
        }

        /// <summary>An old plaintext save is JSON text. It is kept as .plain.old once, and a sealed copy replaces it on the next save.</summary>
        private bool TryLegacy(string p, LoadOutcome o)
        {
            string text;
            try { text = File.ReadAllText(p); } catch (IOException) { return false; }
            var t = text.TrimStart('\uFEFF', ' ', '\r', '\n', '\t');
            if (t.Length == 0 || t[0] != '{') return false;
            try { if (!File.Exists(LegacyCopyPath)) File.Copy(p, LegacyCopyPath); } catch (IOException) { }
            o.Json = text; o.Source = LoadSource.Legacy; o.NeedsRewrite = true; o.Note = "migrated from plaintext save";
            return true;
        }
    }
}
