// (c) Ahmed Alamrani. All rights reserved.
using System.IO;
using Aron.Logic;
using Aron.Save;
using NUnit.Framework;
using UnityEngine;

namespace Aron.Tests.EditMode
{
    /// <summary>Old save files on disk, read through SaveSystem.Load the way a player who updates the app would.</summary>
    public class SaveMigrationTests
    {
        private string path;

        [SetUp]
        public void SetUp()
        {
            path = Path.Combine(Application.temporaryCachePath, "aron_migrate_test.json");
            Wipe();
            SaveSystem.UsePath(path);
        }

        [TearDown]
        public void TearDown()
        {
            SaveSystem.UsePath(null);
            Wipe();
        }

        // The sealed store keeps a backup next to the save file; a stale one would be restored by the next test.
        private void Wipe()
        {
            foreach (var f in Directory.GetFiles(Path.GetDirectoryName(path), Path.GetFileName(path) + "*")) File.Delete(f);
            foreach (var f in Directory.GetFiles(Path.GetDirectoryName(path), "aron_migrate_test*")) File.Delete(f);
        }

        private static SaveData Current() => new SaveData();

        [Test]
        public void V1FileOnDiskUpgradesToTheCurrentVersion()
        {
            File.WriteAllText(path, "{\"version\":1,\"totalDates\":240,\"language\":\"en\"}");
            var d = SaveSystem.Data;
            Assert.AreEqual(Current().version, d.version);
            Assert.AreEqual(240, d.coins, "v1 dates become coins once");
            Assert.AreEqual("en", d.language);
            Assert.AreEqual("saudi", d.homeCountry);
            Assert.AreEqual("normal", d.difficulty);
            Assert.IsNotNull(d.ownedItems); Assert.IsNotNull(d.rentals); Assert.IsNotNull(d.pointLog);
            SaveSystem.Save();
            SaveSystem.UsePath(path);
            Assert.AreEqual(240, SaveSystem.Data.coins, "second load must not add the dates again");
        }

        [Test]
        public void V2FileKeepsItsCoinsAndGetsTheCountryRing()
        {
            File.WriteAllText(path, "{\"version\":2,\"coins\":90,\"totalDates\":500}");
            var d = SaveSystem.Data;
            Assert.AreEqual(90, d.coins, "v2 already converted dates");
            Assert.AreEqual(Current().version, d.version);
            Assert.AreEqual("saudi", d.homeCountry);
        }

        [Test]
        public void V3FileGetsRaceDefaults()
        {
            File.WriteAllText(path, "{\"version\":3,\"coins\":10,\"homeCountry\":\"oman\",\"racePoints\":-5}");
            var d = SaveSystem.Data;
            Assert.AreEqual(Current().version, d.version);
            Assert.AreEqual("oman", d.homeCountry, "an existing home country is kept");
            Assert.AreEqual(0, d.racePoints);
            Assert.AreEqual("normal", d.aiDifficulty);
        }

        [Test]
        public void CorruptFileStartsFreshAndKeepsRunning()
        {
            File.WriteAllText(path, "{ not json");
            // The Unity console bridge is only installed when play mode starts, so capture the log sink directly.
            var previous = Aron.Logic.Diagnostics.Log.Sink;
            var warned = false;
            Aron.Logic.Diagnostics.Log.Sink = (level, message) => { if (level == "warn") warned = true; };
            try
            {
                var d = SaveSystem.Data;
                Assert.AreEqual(Current().version, d.version);
                Assert.AreEqual(0, d.coins);
                Assert.IsTrue(warned, "a damaged save is reported as a warning");
            }
            finally { Aron.Logic.Diagnostics.Log.Sink = previous; }
        }
    }
}
