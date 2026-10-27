// (c) Ahmed Alamrani. All rights reserved.
using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace Aron.EditorTools
{
    /// <summary>
    /// Phone builds, from the Aron menu or headless in CI:
    ///   Unity -batchmode -quit -projectPath . -buildTarget Android -executeMethod Aron.EditorTools.CI.PrepareAndBuild
    /// Android writes Aron.apk (for side-loading) and Aron.aab (for Google Play) next to -customBuildPath.
    /// iOS exports an Xcode project to -customBuildPath; signing and the final build happen on a Mac.
    /// </summary>
    public static class CI
    {
        private const string ApkPath = "Builds/Android/Aron.apk";
        private const string AabPath = "Builds/Android/Aron.aab";
        private const string XcodePath = "Builds/iOS/Aron";

        /// <summary>Set by the optional Addressables assembly: builds the country content for the active build target. Null when the package is absent.</summary>
        public static Action ContentBuilder;
        private static BuildTarget contentBuiltFor = BuildTarget.NoTarget;

        private const AndroidArchitecture AllArm = AndroidArchitecture.ARM64;

        [MenuItem("Aron/4. Build Android APK", priority = 20)]
        public static void BuildApkFromMenu() =>
            FromMenu(ApkPath, () => BuildAndroid(ApkPath, false, AndroidArchitecture.ARM64, BuildOptions.None));

        [MenuItem("Aron/4a. Build Android APK and Run on Connected Phone", priority = 21)]
        public static void BuildAndRunFromMenu() =>
            FromMenu(ApkPath, () => BuildAndroid(ApkPath, false, AndroidArchitecture.ARM64, BuildOptions.AutoRunPlayer));

        [MenuItem("Aron/4b. Build Android App Bundle (Google Play)", priority = 22)]
        public static void BuildAabFromMenu() =>
            FromMenu(AabPath, () => BuildAndroid(AabPath, true, AllArm, BuildOptions.None));

        [MenuItem("Aron/5. Build iOS (Xcode project)", priority = 23)]
        public static void BuildIosFromMenu()
        {
            if (!BuildPipeline.IsBuildTargetSupported(BuildTargetGroup.iOS, BuildTarget.iOS))
            {
                EditorUtility.DisplayDialog("iOS Build Support missing",
                    "Add the \"iOS Build Support\" module to Unity 6000.6.5f1 in Unity Hub, then try again.\n\n" +
                    "The result is an Xcode project. Turning it into an app for an iPhone needs a Mac (or a cloud Mac) " +
                    "and an Apple Developer account. See the iPhone section in README.md.", "OK");
                return;
            }
            FromMenu(XcodePath, () => BuildIos(XcodePath));
        }

        public static void PrepareAndBuild()
        {
            try
            {
                Prepare();
                string output = Argument("-customBuildPath");
                if (RequestedTarget() == BuildTarget.iOS)
                {
                    BuildIos(output ?? XcodePath);
                }
                else
                {
                    string dir = output == null ? Path.GetDirectoryName(ApkPath) : AndroidFolder(output);
                    BuildAndroid(Path.Combine(dir, "Aron.apk"), false, AllArm, BuildOptions.None);
                    BuildAndroid(Path.Combine(dir, "Aron.aab"), true, AllArm, BuildOptions.None);
                }
            }
            catch (Exception e)
            {
                Debug.LogException(e);
                if (Application.isBatchMode) EditorApplication.Exit(1);
                return;
            }
            if (Application.isBatchMode) EditorApplication.Exit(0);
        }

        /// <summary>
        /// Headless release APK for side-loading, 64-bit only (smaller and faster to build than the two-ABI store build):
        ///   Unity -batchmode -quit -projectPath . -buildTarget Android -executeMethod Aron.EditorTools.CI.BuildApkArm64 -customBuildPath out/Aron.apk
        /// </summary>
        public static void BuildApkArm64()
        {
            try
            {
                Prepare();
                string output = Argument("-customBuildPath") ?? ApkPath;
                BuildAndroid(output, false, AndroidArchitecture.ARM64, BuildOptions.None);
            }
            catch (Exception e)
            {
                Debug.LogException(e);
                if (Application.isBatchMode) EditorApplication.Exit(1);
                return;
            }
            if (Application.isBatchMode) EditorApplication.Exit(0);
        }

        public static void Prepare()
        {
            ImportTmpEssentials();
            AronLevelBuilder.BuildAll();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        private static void FromMenu(string output, Action build)
        {
            try
            {
                Prepare();
                build();
            }
            catch (Exception e)
            {
                Debug.LogException(e);
                EditorUtility.DisplayDialog("Build failed", e.Message + "\n\nThe Console has the full error.", "OK");
                return;
            }
            EditorUtility.RevealInFinder(output);
        }

        private static void BuildAndroid(string outputPath, bool bundle, AndroidArchitecture archs, BuildOptions options)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(outputPath)));
            if (EditorUserBuildSettings.activeBuildTarget != BuildTarget.Android)
                EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetGroup.Android, BuildTarget.Android);

            if (ContentBuilder != null && contentBuiltFor != BuildTarget.Android) { ContentBuilder(); contentBuiltFor = BuildTarget.Android; }

            EditorUserBuildSettings.buildAppBundle = bundle;
            EditorUserBuildSettings.androidBuildSubtarget = MobileTextureSubtarget.ASTC;
            PlayerSettings.Android.targetArchitectures = archs;
            ApplyVersion();
            Sign(bundle);

            Build(outputPath, BuildTarget.Android, options);
        }

        private static void BuildIos(string outputPath)
        {
            Directory.CreateDirectory(Path.GetFullPath(outputPath));
            if (EditorUserBuildSettings.activeBuildTarget != BuildTarget.iOS)
                EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetGroup.iOS, BuildTarget.iOS);

            ApplyVersion();
            string team = Argument("-appleTeamId") ?? Environment.GetEnvironmentVariable("APPLE_TEAM_ID");
            PlayerSettings.iOS.appleEnableAutomaticSigning = !string.IsNullOrEmpty(team);
            if (!string.IsNullOrEmpty(team)) PlayerSettings.iOS.appleDeveloperTeamID = team;

            Build(outputPath, BuildTarget.iOS, BuildOptions.None);
        }

        private static void Build(string outputPath, BuildTarget target, BuildOptions options)
        {
            var scenes = EditorBuildSettings.scenes.Where(s => s.enabled).Select(s => s.path).ToArray();
            if (scenes.Length == 0) throw new InvalidOperationException("No scenes in Build Settings; level generation failed.");

            // Shipping builds only: no development flags, no script debugging, no profiler connection.
            BuildHardening.Apply();
            options = BuildHardening.ReleaseOptions(options);
            var problems = BuildHardening.Verify();
            if (problems.Count > 0) throw new InvalidOperationException("Build settings are not release-safe:\n - " + string.Join("\n - ", problems));

            BuildReport report;
            var crashConfig = WriteCrashConfig();
            try
            {
                report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
                {
                    scenes = scenes,
                    locationPathName = outputPath,
                    target = target,
                    targetGroup = BuildPipeline.GetBuildTargetGroup(target),
                    options = options,
                });
            }
            finally { if (crashConfig != null) AssetDatabase.DeleteAsset(crashConfig); }

            var summary = report.summary;
            Debug.Log($"[Aron] {target} build {summary.result}: {outputPath} ({summary.totalSize / (1024 * 1024)} MB, {summary.totalErrors} errors)");
            if (summary.result != BuildResult.Succeeded) throw new Exception($"{target} build failed: {summary.result}");
        }

        // The error-report project address is a CI secret. It is written for the build only and removed afterwards;
        // without it the game still records errors locally and simply never sends them.
        private static string WriteCrashConfig()
        {
            var dsn = Environment.GetEnvironmentVariable("ARON_CRASH_DSN");
            if (string.IsNullOrWhiteSpace(dsn)) return null;
            const string path = "Assets/_Aron/Resources/Config/crash_reporting.json";
            File.WriteAllText(path, "{\"dsn\":\"" + dsn.Trim().Replace("\"", "") + "\"}");
            AssetDatabase.ImportAsset(path);
            return path;
        }

        // A Play upload key comes from CI secrets. Without one the build is signed with Unity's debug key,
        // which installs fine from a file but is refused by Google Play.
        private static void Sign(bool bundle)
        {
            string keystore = Argument("-androidKeystoreName");
            if (!string.IsNullOrEmpty(keystore) && File.Exists(keystore))
            {
                PlayerSettings.Android.useCustomKeystore = true;
                PlayerSettings.Android.keystoreName = Path.GetFullPath(keystore);
                PlayerSettings.Android.keystorePass = Argument("-androidKeystorePass");
                PlayerSettings.Android.keyaliasName = Argument("-androidKeyaliasName");
                PlayerSettings.Android.keyaliasPass = Argument("-androidKeyaliasPass");
                return;
            }

            bool keyReady = PlayerSettings.Android.useCustomKeystore
                && File.Exists(PlayerSettings.Android.keystoreName)
                && !string.IsNullOrEmpty(PlayerSettings.Android.keystorePass)
                && !string.IsNullOrEmpty(PlayerSettings.Android.keyaliasPass);
            if (keyReady) return;

            PlayerSettings.Android.useCustomKeystore = false;
            if (bundle) Debug.LogWarning("[Aron] App bundle signed with the debug key. Set an upload keystore before sending it to Google Play.");
        }

        private static void ApplyVersion()
        {
            string version = Argument("-buildVersion");
            if (!string.IsNullOrEmpty(version) && version != "none") PlayerSettings.bundleVersion = version;
            if (int.TryParse(Argument("-androidVersionCode"), out int code) && code > 0)
            {
                PlayerSettings.Android.bundleVersionCode = code;
                PlayerSettings.iOS.buildNumber = code.ToString();
            }
        }

        private static BuildTarget RequestedTarget()
        {
            string name = Argument("-buildTarget");
            if (name != null && Enum.TryParse(name, true, out BuildTarget target)) return target;
            return EditorUserBuildSettings.activeBuildTarget == BuildTarget.iOS ? BuildTarget.iOS : BuildTarget.Android;
        }

        // GameCI passes a file path such as build/Android/Android.aab; we want its folder.
        private static string AndroidFolder(string path) =>
            Path.HasExtension(path) ? Path.GetDirectoryName(path) : path;

        /// <summary>
        /// Imports TextMeshPro essential resources (shaders, default settings) if missing. Looked up by
        /// reflection because the importer's location differs between TMP 3.x and uGUI 2.x.
        /// </summary>
        private static void ImportTmpEssentials()
        {
            if (AssetDatabase.FindAssets("t:TMP_Settings").Length > 0) return;
            var importer = AppDomain.CurrentDomain.GetAssemblies()
                .Select(a => a.GetType("TMPro.TMP_PackageResourceImporter"))
                .FirstOrDefault(t => t != null);
            var method = importer?.GetMethod("ImportResources", new[] { typeof(bool), typeof(bool), typeof(bool) });
            if (method == null)
            {
                Debug.LogWarning("[Aron] TMP importer not found; import TMP Essentials manually.");
                return;
            }
            method.Invoke(null, new object[] { true, false, false });
            AssetDatabase.Refresh();
        }

        private static string Argument(string name)
        {
            var args = Environment.GetCommandLineArgs();
            for (int i = 0; i < args.Length - 1; i++)
                if (args[i] == name) return args[i + 1];
            return null;
        }
    }
}
