// (c) Ahmed Alamrani. All rights reserved.
using UnityEditor;
using UnityEditor.Build;
using UnityEngine;
using UnityEngine.iOS;
using UnityEngine.Rendering;

namespace Aron.EditorTools
{
    /// <summary>
    /// Aron is a phone and tablet game. This puts every Player Setting that matters for Android and iOS
    /// in one place so a fresh clone builds the same way on any machine, and keeps the editor on Android.
    /// </summary>
    [InitializeOnLoad]
    public static class MobileSetup
    {
        public const string AppName = "رحلة أرون";
        public const string BundleId = "com.ahmedalamrani.aron";
        public const string MinIos = "15.0";
        private const string Company = "Ahmed Alamrani";
        private const string SwitchedKey = "Aron.SwitchedToAndroid";

        static MobileSetup()
        {
            EditorApplication.delayCall += DefaultToAndroid;
        }

        private static void DefaultToAndroid()
        {
            if (Application.isBatchMode || SessionState.GetBool(SwitchedKey, false)) return;
            SessionState.SetBool(SwitchedKey, true);

            var target = EditorUserBuildSettings.activeBuildTarget;
            if (target == BuildTarget.Android || target == BuildTarget.iOS) return;
            if (!BuildPipeline.IsBuildTargetSupported(BuildTargetGroup.Android, BuildTarget.Android))
            {
                Debug.LogWarning("[Aron] Aron only ships on phones. Install Android Build Support from Unity Hub, then reopen the project.");
                return;
            }
            Debug.Log("[Aron] Phone-only project: switching the editor to Android.");
            EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetGroup.Android, BuildTarget.Android);
        }

        [MenuItem("Aron/Apply Phone Settings (Android + iOS)", priority = 40)]
        public static void ApplyFromMenu()
        {
            Apply();
            Debug.Log("[Aron] Android and iOS player settings applied.");
        }

        public static void Apply()
        {
            Common();
            Android();
            Ios();
            Audio();
            QualityTiers.Setup();
            AssetDatabase.SaveAssets();
        }

        private static void Common()
        {
            PlayerSettings.companyName = Company;
            PlayerSettings.productName = AppName;
            if (string.IsNullOrEmpty(PlayerSettings.bundleVersion)) PlayerSettings.bundleVersion = "1.0.0";

            // Side-scroller built for a 16:9 landscape canvas: either landscape way up, never portrait.
            PlayerSettings.defaultInterfaceOrientation = UIOrientation.AutoRotation;
            PlayerSettings.allowedAutorotateToLandscapeLeft = true;
            PlayerSettings.allowedAutorotateToLandscapeRight = true;
            PlayerSettings.allowedAutorotateToPortrait = false;
            PlayerSettings.allowedAutorotateToPortraitUpsideDown = false;

            PlayerSettings.statusBarHidden = true;
            PlayerSettings.runInBackground = false;
            PlayerSettings.muteOtherAudioSources = false;
            BuildHardening.Apply();
        }

        private static void Android()
        {
            PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.Android, BundleId);
            PlayerSettings.SetScriptingBackend(NamedBuildTarget.Android, ScriptingImplementation.IL2CPP);
            // 64-bit only: Google Play requires ARM64, and every phone that runs Unity 6.6 has it.
            PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;

            // 26 is the lowest Unity 6.6 accepts (Android 8.0); 36 is what Google Play requires for new uploads.
            PlayerSettings.Android.minSdkVersion = AndroidSdkVersions.AndroidApiLevel26;
            PlayerSettings.Android.targetSdkVersion = AndroidSdkVersions.AndroidApiLevel36;

            // Every phone that can run Unity 6.6 (GLES 3.1 or Vulkan) decodes ASTC in hardware.
            PlayerSettings.Android.textureCompressionFormats = new[] { TextureCompressionFormat.ASTC };
            EditorUserBuildSettings.androidBuildSubtarget = MobileTextureSubtarget.ASTC;
            PlayerSettings.SetUseDefaultGraphicsAPIs(BuildTarget.Android, true);

            // Marked as a game so Android 16 tablets keep the landscape lock instead of letterboxing it away.
            PlayerSettings.Android.appCategory = "game";
            PlayerSettings.Android.renderOutsideSafeArea = true;
            PlayerSettings.Android.optimizedFramePacing = true;
            PlayerSettings.Android.androidTVCompatibility = false;
            // Back must keep arriving as Escape, which opens the pause menu.
            PlayerSettings.Android.predictiveBackSupport = false;
        }

        private static void Ios()
        {
            PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.iOS, BundleId);
            PlayerSettings.SetScriptingBackend(NamedBuildTarget.iOS, ScriptingImplementation.IL2CPP);
            PlayerSettings.SetArchitecture(NamedBuildTarget.iOS, 1);
            PlayerSettings.SetUseDefaultGraphicsAPIs(BuildTarget.iOS, false);
            PlayerSettings.SetGraphicsAPIs(BuildTarget.iOS, new[] { GraphicsDeviceType.Metal });

            PlayerSettings.iOS.targetOSVersionString = MinIos;
            PlayerSettings.iOS.targetDevice = iOSTargetDevice.iPhoneAndiPad;
            // Landscape-only on iPad means opting out of Split View.
            PlayerSettings.iOS.requiresFullScreen = true;
            // A swipe down near the bottom edge slows Aron; make iOS ask for a second swipe before going home.
            PlayerSettings.iOS.deferSystemGesturesMode = SystemGestureDeferMode.BottomEdge;
            PlayerSettings.iOS.microphoneUsageDescription = "يستخدم الميكروفون للأوامر الصوتية أثناء اللعب فقط.";
        }

        private static void Audio()
        {
            var assets = AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/AudioManager.asset");
            if (assets.Length == 0) return;
            var so = new SerializedObject(assets[0]);
            // "Good latency": low enough for jump sounds to feel instant, large enough to avoid crackle on cheap phones.
            var buffer = so.FindProperty("m_RequestedDSPBufferSize") ?? so.FindProperty("m_DSPBufferSize");
            if (buffer != null) buffer.intValue = 512;
            so.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
