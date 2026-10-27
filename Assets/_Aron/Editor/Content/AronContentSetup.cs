// (c) Ahmed Alamrani. All rights reserved.
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Aron.Content;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Build;
using UnityEditor.AddressableAssets.Settings;
using UnityEditor.AddressableAssets.Settings.GroupSchemas;
using UnityEngine;

namespace Aron.EditorTools.Content
{
    /// <summary>
    /// Builds the Addressables setup from the folders, the same way the scenes are generated, so nothing generated is committed:
    /// every folder <c>Assets/_Aron/Remote/Music/country_&lt;country&gt;</c> becomes one remote group with the same name, its tracks get the
    /// address <c>music/&lt;track&gt;</c> and the group name as their label. Menus, stage-type tracks, stingers, Riyadh/Diriyah and all sound
    /// effects stay in Resources, inside the base APK. Remote bundles are written to <c>ServerData/&lt;BuildTarget&gt;</c> and are loaded
    /// from <c>{ContentHosting.BaseUrl}/&lt;BuildTarget&gt;</c> (docs/content_delivery.md).
    /// </summary>
    public static class AronContentSetup
    {
        public const string MusicRoot = "Assets/_Aron/Remote/Music";
        public const string BuildFolder = "ServerData";

        [InitializeOnLoadMethod]
        private static void Register() => CI.ContentBuilder = BuildContent;

        [MenuItem("Aron/Content/Set up country groups")]
        public static void ConfigureFromMenu() { Configure(); }

        [MenuItem("Aron/Content/Build country content")]
        public static void BuildFromMenu() { BuildContent(); }

        /// <summary>Folder names under <see cref="MusicRoot"/> that are country groups.</summary>
        public static IEnumerable<string> GroupFolders()
        {
            if (!AssetDatabase.IsValidFolder(MusicRoot)) yield break;
            foreach (var f in AssetDatabase.GetSubFolders(MusicRoot))
                if (Path.GetFileName(f).StartsWith("country_", StringComparison.Ordinal)) yield return f;
        }

        public static AddressableAssetSettings Configure()
        {
            var settings = AddressableAssetSettingsDefaultObject.GetSettings(true);
            if (settings == null) throw new InvalidOperationException("Addressables settings could not be created.");

            // Where bundles are built and where players load them from.
            string profile = settings.activeProfileId;
            settings.profileSettings.SetValue(profile, AddressableAssetSettings.kRemoteBuildPath, BuildFolder + "/[BuildTarget]");
            settings.profileSettings.SetValue(profile, AddressableAssetSettings.kRemoteLoadPath, "{Aron.Content.ContentHosting.BaseUrl}/[BuildTarget]");
            settings.BuildRemoteCatalog = true;
            settings.RemoteCatalogBuildPath.SetVariableByName(settings, AddressableAssetSettings.kRemoteBuildPath);
            settings.RemoteCatalogLoadPath.SetVariableByName(settings, AddressableAssetSettings.kRemoteLoadPath);

            var wanted = new HashSet<string>();
            foreach (var folder in GroupFolders())
            {
                string name = Path.GetFileName(folder);
                wanted.Add(name);
                var group = settings.FindGroup(name) ?? settings.CreateGroup(name, false, false, false, null, typeof(BundledAssetGroupSchema), typeof(ContentUpdateGroupSchema));
                var schema = group.GetSchema<BundledAssetGroupSchema>() ?? group.AddSchema<BundledAssetGroupSchema>();
                schema.BuildPath.SetVariableByName(settings, AddressableAssetSettings.kRemoteBuildPath);
                schema.LoadPath.SetVariableByName(settings, AddressableAssetSettings.kRemoteLoadPath);
                schema.BundleMode = BundledAssetGroupSchema.BundlePackingMode.PackTogether;
                schema.Compression = BundledAssetGroupSchema.BundleCompressionMode.LZ4;
                schema.UseAssetBundleCache = true;
                schema.IncludeInBuild = true;
                var update = group.GetSchema<ContentUpdateGroupSchema>();
                if (update != null) update.StaticContent = false;

                foreach (var guid in AssetDatabase.FindAssets("t:AudioClip", new[] { folder }))
                {
                    string path = AssetDatabase.GUIDToAssetPath(guid);
                    var entry = settings.CreateOrMoveEntry(guid, group, false, false);
                    entry.address = ContentAddress(path);
                    entry.SetLabel(name, true, true, false);
                }
            }

            // A folder that no longer exists must not leave a stale group in the build.
            foreach (var g in settings.groups.ToArray())
                if (g != null && g.name.StartsWith("country_", StringComparison.Ordinal) && !wanted.Contains(g.name)) settings.RemoveGroup(g);

            EditorUtility.SetDirty(settings);
            AssetDatabase.SaveAssets();
            return settings;
        }

        /// <summary>The address a track is loaded by: <c>music/&lt;file name without extension&gt;</c>.</summary>
        public static string ContentAddress(string assetPath) => AddressablesBackend.AddressPrefix + Path.GetFileNameWithoutExtension(assetPath);

        /// <summary>Everything wrong with the setup, empty when sound. Used by the tests and before every content build.</summary>
        public static List<string> Verify(AddressableAssetSettings settings)
        {
            var problems = new List<string>();
            var groups = settings.groups.Where(g => g != null && g.name.StartsWith("country_", StringComparison.Ordinal)).ToList();
            if (groups.Count == 0) problems.Add("no country groups");
            foreach (var g in groups)
            {
                var s = g.GetSchema<BundledAssetGroupSchema>();
                if (s == null) { problems.Add(g.name + ": no bundle schema"); continue; }
                if (!s.IncludeInBuild) problems.Add(g.name + ": not included in the build");
                if (s.BuildPath.GetName(settings) != AddressableAssetSettings.kRemoteBuildPath) problems.Add(g.name + ": build path is not remote");
                if (s.LoadPath.GetName(settings) != AddressableAssetSettings.kRemoteLoadPath) problems.Add(g.name + ": load path is not remote");
                if (g.entries.Count == 0) problems.Add(g.name + ": empty");
                foreach (var e in g.entries)
                {
                    if (e.AssetPath.Contains("/Resources/")) problems.Add(e.AssetPath + ": in Resources (belongs to the base install)");
                    if (!e.labels.Contains(g.name)) problems.Add(e.AssetPath + ": missing the label " + g.name);
                    if (!e.address.StartsWith(AddressablesBackend.AddressPrefix, StringComparison.Ordinal)) problems.Add(e.AssetPath + ": address " + e.address);
                }
            }
            foreach (var g in settings.groups)
            {
                if (g == null || g.name.StartsWith("country_", StringComparison.Ordinal)) continue;
                var s = g.GetSchema<BundledAssetGroupSchema>();
                if (s != null && g.entries.Count > 0 && s.IncludeInBuild && s.BuildPath.GetName(settings) != AddressableAssetSettings.kRemoteBuildPath)
                    problems.Add(g.name + ": base group with entries (would grow the base APK)");
            }
            return problems;
        }

        /// <summary>Sets the groups up and builds the bundles for the active build target into ServerData/&lt;target&gt;.</summary>
        public static void BuildContent()
        {
            var settings = Configure();
            var problems = Verify(settings);
            if (problems.Count > 0) throw new InvalidOperationException("Content setup is not sound:\n - " + string.Join("\n - ", problems));

            AddressableAssetSettings.CleanPlayerContent();
            AddressableAssetSettings.BuildPlayerContent(out AddressablesPlayerBuildResult result);
            if (!string.IsNullOrEmpty(result.Error)) throw new InvalidOperationException("Content build failed: " + result.Error);

            string dir = Path.Combine(BuildFolder, EditorUserBuildSettings.activeBuildTarget.ToString());
            long bytes = Directory.Exists(dir) ? Directory.GetFiles(dir, "*", SearchOption.AllDirectories).Sum(f => new FileInfo(f).Length) : 0;
            Debug.Log($"[Aron] Country content built: {dir} ({bytes / 1024} KB in {Directory.GetFiles(dir).Length} files)");
        }

        /// <summary>Batch entry point: <c>-executeMethod Aron.EditorTools.Content.AronContentSetup.BuildContentBatch</c>.</summary>
        public static void BuildContentBatch()
        {
            try { BuildContent(); }
            catch (Exception e) { Debug.LogException(e); EditorApplication.Exit(1); return; }
            EditorApplication.Exit(0);
        }
    }
}
