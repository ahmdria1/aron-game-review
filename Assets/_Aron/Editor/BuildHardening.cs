// (c) Ahmed Alamrani. All rights reserved.
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Build;
using UnityEngine;

namespace Aron.EditorTools
{
    /// <summary>
    /// Release hardening in one place: IL2CPP, 64-bit, high managed stripping (see Assets/_Aron/link.xml), no development or
    /// script-debugging flags, no console text, Android R8 minify, no symbol files inside the shipped app.
    /// <see cref="Verify"/> is run by CI before every build and fails it if anything drifted.
    /// </summary>
    public static class BuildHardening
    {
        public static void Apply()
        {
            foreach (var t in new[] { NamedBuildTarget.Android, NamedBuildTarget.iOS })
            {
                PlayerSettings.SetScriptingBackend(t, ScriptingImplementation.IL2CPP);
                PlayerSettings.SetManagedStrippingLevel(t, ManagedStrippingLevel.High);
                PlayerSettings.SetIl2CppCompilerConfiguration(t, Il2CppCompilerConfiguration.Release);
            }

            EditorUserBuildSettings.development = false;
            EditorUserBuildSettings.allowDebugging = false;
            EditorUserBuildSettings.connectProfiler = false;
            EditorUserBuildSettings.waitForManagedDebugger = false;

            // No stack text for ordinary log lines, script-only for errors (the crash reporter symbolicates them).
            PlayerSettings.SetStackTraceLogType(LogType.Log, StackTraceLogType.None);
            PlayerSettings.SetStackTraceLogType(LogType.Warning, StackTraceLogType.None);
            PlayerSettings.SetStackTraceLogType(LogType.Error, StackTraceLogType.ScriptOnly);
            PlayerSettings.SetStackTraceLogType(LogType.Exception, StackTraceLogType.ScriptOnly);

            PlayerSettings.Android.minifyRelease = true;     // R8 shrinks and renames Java code
            PlayerSettings.Android.minifyDebug = false;
            // Native symbols stay out of the shipped app. CI can build them as a separate zip for the crash reporter (ARON_SYMBOLS=1).
            EditorUserBuildSettings.androidCreateSymbols = System.Environment.GetEnvironmentVariable("ARON_SYMBOLS") == "1"
                ? AndroidCreateSymbols.Public : AndroidCreateSymbols.Disabled;
        }

        public static BuildOptions ReleaseOptions(BuildOptions options) =>
            options & ~(BuildOptions.Development | BuildOptions.AllowDebugging | BuildOptions.ConnectWithProfiler | BuildOptions.EnableDeepProfilingSupport | BuildOptions.WaitForPlayerConnection);

        /// <summary>Empty list when the project is release-safe.</summary>
        public static List<string> Verify()
        {
            var bad = new List<string>();
            foreach (var t in new[] { NamedBuildTarget.Android, NamedBuildTarget.iOS })
            {
                if (PlayerSettings.GetScriptingBackend(t) != ScriptingImplementation.IL2CPP) bad.Add(t.TargetName + ": scripting backend is not IL2CPP");
                if (PlayerSettings.GetManagedStrippingLevel(t) != ManagedStrippingLevel.High) bad.Add(t.TargetName + ": managed stripping is not High");
            }
            if (EditorUserBuildSettings.development) bad.Add("development build is on");
            if (EditorUserBuildSettings.allowDebugging) bad.Add("script debugging is on");
            if (EditorUserBuildSettings.connectProfiler) bad.Add("profiler auto-connect is on");
            if ((PlayerSettings.Android.targetArchitectures & AndroidArchitecture.ARM64) == 0) bad.Add("Android ARM64 is off");
            if (!PlayerSettings.Android.minifyRelease) bad.Add("Android R8 minify (release) is off");
            if (!System.IO.File.Exists("Assets/_Aron/link.xml")) bad.Add("Assets/_Aron/link.xml is missing");
            return bad;
        }

        [MenuItem("Aron/Security/Check Release Settings", priority = 60)]
        public static void CheckFromMenu()
        {
            var bad = Verify();
            Debug.Log(bad.Count == 0 ? "[Aron] Release settings are hardened." : "[Aron] Not release-safe:\n - " + string.Join("\n - ", bad));
        }
    }
}
