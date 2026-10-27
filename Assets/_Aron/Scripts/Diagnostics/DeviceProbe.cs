// (c) Ahmed Alamrani. All rights reserved.
using System;
using System.IO;
using Aron.Logic.Integrity;
using UnityEngine;

namespace Aron.Diagnostics
{
    /// <summary>
    /// Reads the signals once per launch, turns them into soft flags and stops. Nothing here blocks offline play or touches the save:
    /// the flags only ride along on online requests so the server can keep a flagged phone out of ranked results.
    /// </summary>
    public static class DeviceProbe
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterAssembliesLoaded)]
        private static void Run()
        {
            if (Application.isEditor) return;
            try { SoftFlags.Raise(DeviceChecks.Evaluate(Read(), Debug.isDebugBuild)); }
            catch (Exception) { /* a failing probe must never hurt the game */ }
        }

        private static DeviceSignals Read()
        {
            var s = new DeviceSignals { debuggerAttached = System.Diagnostics.Debugger.IsAttached, debuggableBuild = Debug.isDebugBuild };
#if UNITY_ANDROID
            foreach (var p in DeviceChecks.AndroidRootPaths) if (File.Exists(p) || Directory.Exists(p)) { s.suBinaryFound = true; break; }
            try
            {
                using (var build = new AndroidJavaClass("android.os.Build"))
                {
                    var tags = build.GetStatic<string>("TAGS");
                    s.testKeysBuild = tags != null && tags.Contains("test-keys");
                    var fp = build.GetStatic<string>("FINGERPRINT") ?? "";
                    s.emulatorHints = fp.StartsWith("generic") || fp.Contains("emulator");
                }
                using (var unity = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
                using (var activity = unity.GetStatic<AndroidJavaObject>("currentActivity"))
                using (var pm = activity.Call<AndroidJavaObject>("getPackageManager"))
                {
                    foreach (var pkg in DeviceChecks.AndroidRootPackages)
                    {
                        try { using (pm.Call<AndroidJavaObject>("getPackageInfo", pkg, 0)) { s.rootManagerAppFound = true; } }
                        catch (AndroidJavaException) { }
                    }
                    var installer = pm.Call<string>("getInstallerPackageName", activity.Call<string>("getPackageName"));
                    s.unknownInstaller = installer != "com.android.vending";
                }
            }
            catch (Exception) { }
#elif UNITY_IOS
            foreach (var p in DeviceChecks.IosJailbreakPaths) if (File.Exists(p) || Directory.Exists(p)) { s.jailbreakFilesFound = true; break; }
            try
            {
                string probe = "/private/aron_probe.txt";
                File.WriteAllText(probe, "x");
                File.Delete(probe);
                s.sandboxEscaped = true;   // a normal app cannot write outside its sandbox
            }
            catch (Exception) { }
#endif
            return s;
        }
    }
}
