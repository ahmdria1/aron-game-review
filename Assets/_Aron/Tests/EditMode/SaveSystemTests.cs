// (c) Ahmed Alamrani. All rights reserved.
using System.IO;
using Aron.Save;
using NUnit.Framework;
using UnityEngine;

namespace Aron.Tests.EditMode
{
    public class SaveSystemTests
    {
        private string path;

        [SetUp]
        public void SetUp()
        {
            path = Path.Combine(Application.temporaryCachePath, "aron_save_test.json");
            if (File.Exists(path)) File.Delete(path);
            SaveSystem.UsePath(path);
        }

        [TearDown]
        public void TearDown()
        {
            SaveSystem.UsePath(null);
            if (File.Exists(path)) File.Delete(path);
        }

        [Test]
        public void MissingFileGivesDefaults()
        {
            Assert.AreEqual("aron", SaveSystem.Data.selectedCharacterId);
        }

        [Test]
        public void RoundTripsThroughDisk()
        {
            SaveSystem.Data.selectedCharacterId = "noura";
            SaveSystem.Data.totalDates = 42;
            SaveSystem.Save();

            SaveSystem.UsePath(path); // drop the in-memory copy
            Assert.AreEqual("noura", SaveSystem.Data.selectedCharacterId);
            Assert.AreEqual(42, SaveSystem.Data.totalDates);
        }

        [Test]
        public void CorruptFileFallsBackToDefaults()
        {
            File.WriteAllText(path, "{ not json");
            SaveSystem.Load();
            Assert.IsNotNull(SaveSystem.Data);
            Assert.AreEqual("", SaveSystem.Data.language, "default language follows the phone");
        }
    }
}
