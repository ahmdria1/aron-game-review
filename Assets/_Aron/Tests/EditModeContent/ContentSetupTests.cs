// (c) Ahmed Alamrani. All rights reserved.
using System.IO;
using System.Linq;
using Aron.Content;
using Aron.EditorTools.Content;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.AddressableAssets.Settings;
using UnityEditor.AddressableAssets.Settings.GroupSchemas;

namespace Aron.Tests.EditMode.Content
{
    /// <summary>The generated Addressables setup: one remote group per country folder, base content untouched.</summary>
    public class ContentSetupTests
    {
        private static AddressableAssetSettings settings;

        [OneTimeSetUp]
        public void Configure() { settings = AronContentSetup.Configure(); }

        [Test]
        public void TheSetupIsSound()
        {
            Assert.IsEmpty(AronContentSetup.Verify(settings), string.Join("; ", AronContentSetup.Verify(settings)));
        }

        [Test]
        public void EveryCountryFolderIsOneRemoteGroupWithAllItsTracks()
        {
            var folders = AronContentSetup.GroupFolders().ToList();
            Assert.Greater(folders.Count, 20, "the per-country folders are missing");
            foreach (var folder in folders)
            {
                string name = Path.GetFileName(folder);
                var group = settings.FindGroup(name);
                Assert.IsNotNull(group, name + " has no group");
                int files = AssetDatabase.FindAssets("t:AudioClip", new[] { folder }).Length;
                Assert.AreEqual(files, group.entries.Count, name + ": every track must be in the group");
                var schema = group.GetSchema<BundledAssetGroupSchema>();
                Assert.AreEqual(AddressableAssetSettings.kRemoteBuildPath, schema.BuildPath.GetName(settings), name);
                Assert.AreEqual(AddressableAssetSettings.kRemoteLoadPath, schema.LoadPath.GetName(settings), name);
                foreach (var e in group.entries)
                {
                    Assert.IsTrue(e.labels.Contains(name), e.address + " needs the label " + name);
                    StringAssert.StartsWith(AddressablesBackend.AddressPrefix, e.address);
                }
            }
        }

        [Test]
        public void NothingFromResourcesIsAddressableSoTheBaseInstallKeepsMenusAndCoreSound()
        {
            foreach (var g in settings.groups.Where(g => g != null))
                foreach (var e in g.entries)
                    StringAssert.DoesNotContain("/Resources/", e.AssetPath);
            Assert.IsTrue(File.Exists("Assets/_Aron/Resources/Audio/Music/music_menu.ogg"), "the menu track must stay in the base install");
        }

        [Test]
        public void TheRemoteLoadPathUsesTheConfigurableHost()
        {
            string value = settings.profileSettings.GetValueByName(settings.activeProfileId, AddressableAssetSettings.kRemoteLoadPath);
            StringAssert.Contains("Aron.Content.ContentHosting.BaseUrl", value);
        }

        [Test]
        public void ARemoteCatalogIsBuilt()
        {
            Assert.IsTrue(settings.BuildRemoteCatalog);
        }
    }
}
