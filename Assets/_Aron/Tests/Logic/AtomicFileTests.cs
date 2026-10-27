// (c) Ahmed Alamrani. All rights reserved.
using System.IO;
using Aron.Logic;
using NUnit.Framework;

namespace Aron.Tests.Logic
{
    public class AtomicFileTests
    {
        private string dir;

        [SetUp]
        public void SetUp()
        {
            dir = Path.Combine(Path.GetTempPath(), "aron-tests-" + Path.GetRandomFileName());
            Directory.CreateDirectory(dir);
        }

        [TearDown]
        public void TearDown() => Directory.Delete(dir, true);

        [Test]
        public void WritesAndOverwritesWithoutLeavingTempFiles()
        {
            string path = Path.Combine(dir, "save.json");
            AtomicFile.WriteAllText(path, "{\"a\":1}");
            AtomicFile.WriteAllText(path, "{\"a\":2}");
            Assert.IsTrue(AtomicFile.TryReadAllText(path, out var text));
            Assert.AreEqual("{\"a\":2}", text);
            Assert.IsFalse(File.Exists(path + ".tmp"));
        }

        [Test]
        public void MissingFileReportsFalse()
        {
            Assert.IsFalse(AtomicFile.TryReadAllText(Path.Combine(dir, "nope.json"), out var text));
            Assert.IsNull(text);
        }

        [Test]
        public void SaveDataDefaultsAreSensible()
        {
            var d = new SaveData();
            Assert.AreEqual("aron", d.selectedCharacterId);
            Assert.AreEqual("", d.language, "empty: follow the phone language");
            Assert.AreEqual("normal", d.difficulty);
            Assert.AreEqual("", d.homeCountry, "picked from the phone on first run");
            Assert.IsTrue(d.musicEnabled);
        }
    }
}
