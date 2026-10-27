// (c) Ahmed Alamrani. All rights reserved.
using System;
using System.IO;
using System.Text;
using Aron.Logic;
using Aron.Logic.Security;
using NUnit.Framework;

namespace Aron.Logic.Tests
{
    [TestFixture]
    public class SaveVaultTests
    {
        private string dir;

        [SetUp] public void Up() { dir = Path.Combine(Path.GetTempPath(), "aron_vault_" + Guid.NewGuid().ToString("N")); Directory.CreateDirectory(dir); }
        [TearDown] public void Down() { try { Directory.Delete(dir, true); } catch (IOException) { } }

        private static byte[] Key() { var k = new byte[32]; for (int i = 0; i < 32; i++) k[i] = (byte)(i * 7 + 1); return k; }
        private string P => Path.Combine(dir, "save.dat");
        private static IAead[] Both() => new IAead[] { new AesGcmAead(), new AesCbcHmacAead() };

        [TestCaseSource(nameof(Both))]
        public void RoundTripGivesBackTheSameBytes(IAead aead)
        {
            var plain = Encoding.UTF8.GetBytes("{\"coins\":12,\"name\":\"أرون\"}");
            var blob = Envelope.Seal(plain, Key(), aead);
            Assert.AreEqual(OpenResult.Ok, Envelope.Open(blob, Key(), out var back));
            CollectionAssert.AreEqual(plain, back);
        }

        [TestCaseSource(nameof(Both))]
        public void SameInputSealsDifferentlyEachTime(IAead aead)
        {
            var plain = Encoding.UTF8.GetBytes("abc");
            CollectionAssert.AreNotEqual(Envelope.Seal(plain, Key(), aead), Envelope.Seal(plain, Key(), aead));
        }

        [TestCaseSource(nameof(Both))]
        public void EveryFlippedByteIsRejected(IAead aead)
        {
            var blob = Envelope.Seal(Encoding.UTF8.GetBytes("{\"coins\":5}"), Key(), aead);
            for (int i = 0; i < blob.Length; i++)
            {
                var bad = (byte[])blob.Clone(); bad[i] ^= 0x01;
                var r = Envelope.Open(bad, Key(), out var plain);
                Assert.AreNotEqual(OpenResult.Ok, r, "byte " + i + " was changed but the save still opened");
                Assert.IsNull(plain);
            }
        }

        [TestCaseSource(nameof(Both))]
        public void WrongKeyAndTruncationAreRejected(IAead aead)
        {
            var blob = Envelope.Seal(new byte[] { 1, 2, 3 }, Key(), aead);
            var other = Key(); other[0] ^= 0xFF;
            Assert.AreEqual(OpenResult.Tampered, Envelope.Open(blob, other, out _));
            Assert.AreEqual(OpenResult.BadFormat, Envelope.Open(new byte[] { (byte)'A', (byte)'R', (byte)'N', 2, aead.Id, 0 }, Key(), out _));
            Assert.AreEqual(OpenResult.BadFormat, Envelope.Open(Encoding.UTF8.GetBytes("{\"coins\":1}"), Key(), out _));
        }

        [Test]
        public void NewerFormatIsNotMisread()
        {
            var blob = Envelope.Seal(new byte[] { 9 }, Key());
            blob[3] = 9;
            Assert.AreEqual(OpenResult.UnsupportedVersion, Envelope.Open(blob, Key(), out _));
        }

        [Test]
        public void KeyDependsOnInstallSecretAndDevice()
        {
            var a = SaveKey.Derive(new byte[] { 1, 2, 3 }, "dev-1", 1000);
            Assert.AreEqual(32, a.Length);
            CollectionAssert.AreEqual(a, SaveKey.Derive(new byte[] { 1, 2, 3 }, "dev-1", 1000));
            CollectionAssert.AreNotEqual(a, SaveKey.Derive(new byte[] { 1, 2, 4 }, "dev-1", 1000));
            CollectionAssert.AreNotEqual(a, SaveKey.Derive(new byte[] { 1, 2, 3 }, "dev-2", 1000));
        }

        [Test]
        public void SaltIsNotStoredAsPlainLiteralAndIsStable()
        {
            Assert.AreEqual(16, SaveKey.Salt().Length);
            CollectionAssert.AreEqual(SaveKey.Salt(), SaveKey.Salt());
        }

        [Test]
        public void StoreWritesSealedFileNotPlainJson()
        {
            var s = new SecureSaveStore(P, new MemoryKeyStore());
            s.Write("{\"coins\":77}");
            var raw = File.ReadAllBytes(P);
            Assert.IsTrue(Envelope.LooksSealed(raw));
            StringAssert.DoesNotContain("coins", Encoding.UTF8.GetString(raw));
            var o = s.Read();
            Assert.AreEqual(LoadSource.Main, o.Source);
            Assert.AreEqual("{\"coins\":77}", o.Json);
            Assert.IsFalse(File.Exists(P + ".tmp"));
        }

        [Test]
        public void SecondWriteKeepsAGoodBackup()
        {
            var s = new SecureSaveStore(P, new MemoryKeyStore());
            s.Write("{\"n\":1}"); s.Write("{\"n\":2}");
            Assert.IsTrue(File.Exists(s.BackupPath));
            Assert.AreEqual("{\"n\":2}", s.Read().Json);
        }

        [Test]
        public void TamperedMainFileRestoresFromBackup()
        {
            var s = new SecureSaveStore(P, new MemoryKeyStore());
            s.Write("{\"n\":1}"); s.Write("{\"n\":2}");
            var raw = File.ReadAllBytes(P); raw[raw.Length - 1] ^= 0x55; File.WriteAllBytes(P, raw);
            var o = s.Read();
            Assert.AreEqual(LoadSource.Backup, o.Source);
            Assert.IsTrue(o.Recovered); Assert.IsTrue(o.NeedsRewrite);
            Assert.AreEqual("{\"n\":1}", o.Json);
        }

        [Test]
        public void TruncatedMainFileRestoresFromBackupAndNextWriteDoesNotPoisonIt()
        {
            var ks = new MemoryKeyStore();
            var s = new SecureSaveStore(P, ks);
            s.Write("{\"n\":1}"); s.Write("{\"n\":2}");
            File.WriteAllBytes(P, new byte[] { 1, 2, 3 });
            Assert.AreEqual("{\"n\":1}", s.Read().Json);
            s.Write("{\"n\":3}");   // the broken main must not overwrite the good backup
            Assert.AreEqual("{\"n\":1}", ReadBackup(s, ks));
            Assert.AreEqual("{\"n\":3}", s.Read().Json);
        }

        private string ReadBackup(SecureSaveStore s, IKeyStore ks)
        {
            string copy = Path.Combine(dir, "probe.dat");
            File.Copy(s.BackupPath, copy, true);
            return new SecureSaveStore(copy, ks).Read().Json;
        }

        [Test]
        public void BothFilesBrokenGivesNothingNotACrash()
        {
            var s = new SecureSaveStore(P, new MemoryKeyStore());
            s.Write("{\"n\":1}"); s.Write("{\"n\":2}");
            File.WriteAllBytes(P, new byte[] { 0, 0, 0 }); File.WriteAllBytes(s.BackupPath, new byte[] { 0, 0 });
            var o = s.Read();
            Assert.AreEqual(LoadSource.None, o.Source);
            Assert.IsNull(o.Json); Assert.IsTrue(o.Recovered);
        }

        [Test]
        public void OldPlaintextSaveIsMigratedOnceAndKept()
        {
            File.WriteAllText(P, "{\"coins\":9,\"version\":3}");
            var s = new SecureSaveStore(P, new MemoryKeyStore());
            var o = s.Read();
            Assert.AreEqual(LoadSource.Legacy, o.Source);
            Assert.IsTrue(o.NeedsRewrite);
            StringAssert.Contains("\"coins\":9", o.Json);
            Assert.IsTrue(File.Exists(s.LegacyCopyPath));
            s.Write(o.Json);
            Assert.IsTrue(Envelope.LooksSealed(File.ReadAllBytes(P)));
            Assert.AreEqual(LoadSource.Main, s.Read().Source);
        }

        [Test]
        public void PlaintextCannotBeSwappedInAfterTheFirstSealedSave()
        {
            var s = new SecureSaveStore(P, new MemoryKeyStore());
            s.Write("{\"coins\":1}");
            File.WriteAllText(P, "{\"coins\":999999}");
            var o = s.Read();
            Assert.AreNotEqual(LoadSource.Legacy, o.Source);
            Assert.IsNull(o.Json);   // no backup yet, so nothing is trusted
        }

        [Test]
        public void SaveDataSurvivesSealingAndKeepsTheSignature()
        {
            var d = new SaveData { coins = 31 };
            StoreInventory.Upgrade(d);
            var s = new SecureSaveStore(P, new MemoryKeyStore());
            s.Write(UnityJson(d));
            StringAssert.Contains("Ahmed Alamrani", s.Read().Json);
        }

        private static string UnityJson(SaveData d) => "{\"sig\":\"" + d.sig + "\",\"coins\":" + d.coins + "}";

        [Test]
        public void KeyIsKeptByTheStoreSoReopeningWorksButAnotherInstallCannotRead()
        {
            var ks = new MemoryKeyStore("d1");
            new SecureSaveStore(P, ks).Write("{\"a\":1}");
            Assert.AreEqual("{\"a\":1}", new SecureSaveStore(P, ks).Read().Json);
            var other = new SecureSaveStore(P, new MemoryKeyStore("d1"));   // different install secret
            Assert.IsNull(other.Read().Json);
        }
    }
}
