// (c) Ahmed Alamrani. All rights reserved.
using System;
using System.IO;
using System.Linq;
using Aron.Logic.Diagnostics;
using Aron.Logic.Integrity;
using NUnit.Framework;

namespace Aron.Logic.Tests
{
    public class HardeningTests
    {
        private static string Root()
        {
            for (var d = new DirectoryInfo(TestContext.CurrentContext.TestDirectory); d != null; d = d.Parent)
                if (Directory.Exists(Path.Combine(d.FullName, "Assets", "_Aron")) && Directory.Exists(Path.Combine(d.FullName, "ProjectSettings"))) return d.FullName;
            Assert.Fail("project root not found");
            return null;
        }

        private static string Read(params string[] parts) => File.ReadAllText(Path.Combine(new[] { Root() }.Concat(parts).ToArray()));

        // ---------------------------------------------------------------- log wrapper

        [Test]
        public void InfoAndWarnAreCompiledOutOfAReleaseStyleBuildButErrorsStay()
        {
            var got = new System.Collections.Generic.List<string>();
            int evaluated = 0;
            Log.Sink = (lvl, msg) => got.Add(lvl + ":" + msg);
            try
            {
                string Expensive() { evaluated++; return "x"; }
                Log.Info(Expensive());
                Log.Warn(Expensive());
                Log.Error("boom");
            }
            finally { Log.Sink = null; }
#if ARON_LOGS || DEVELOPMENT_BUILD || UNITY_EDITOR
            Assert.AreEqual(3, got.Count);
#else
            CollectionAssert.AreEqual(new[] { "error:boom" }, got);
            Assert.AreEqual(0, evaluated, "the arguments of a removed call are never evaluated");
#endif
        }

        [Test]
        public void ErrorWithNoSinkDoesNotThrow()
        {
            Log.Sink = null;
            Assert.DoesNotThrow(() => Log.Error("x"));
        }

        [Test]
        public void NoRawConsoleLoggingInRuntimeCode()
        {
            var bad = Directory.GetFiles(Path.Combine(Root(), "Assets", "_Aron", "Scripts"), "*.cs", SearchOption.AllDirectories)
                .Where(f => !f.EndsWith("LogBridge.cs"))
                .SelectMany(f => File.ReadAllLines(f).Select((l, i) => (f, i: i + 1, l)))
                .Where(x => System.Text.RegularExpressions.Regex.IsMatch(x.l, @"\bDebug\.Log(Warning)?\(") && !x.l.TrimStart().StartsWith("//"))
                .Select(x => Path.GetFileName(x.f) + ":" + x.i).ToList();
            Assert.IsEmpty(bad, "use Aron.Logic.Diagnostics.Log (compiled out of release builds): " + string.Join(", ", bad));
        }

        // ---------------------------------------------------------------- build settings in source

        [Test]
        public void HardeningSettingsAreInTheBuildScripts()
        {
            var h = Read("Assets", "_Aron", "Editor", "BuildHardening.cs");
            foreach (var needle in new[] { "ScriptingImplementation.IL2CPP", "ManagedStrippingLevel.High", "EditorUserBuildSettings.development = false",
                "EditorUserBuildSettings.allowDebugging = false", "minifyRelease = true", "AndroidCreateSymbols.Disabled", "Il2CppCompilerConfiguration.Release" })
                StringAssert.Contains(needle, h);
            var m = Read("Assets", "_Aron", "Editor", "MobileSetup.cs");
            StringAssert.Contains("AndroidArchitecture.ARM64;", m);
            StringAssert.DoesNotContain("ARMv7", m);
            StringAssert.DoesNotContain("ManagedStrippingLevel.Low", m);
            var ci = Read("Assets", "_Aron", "Editor", "CI.cs");
            StringAssert.Contains("BuildHardening.Verify()", ci);
            StringAssert.DoesNotContain("BuildOptions.Development", ci);
            StringAssert.DoesNotContain("ARMv7", ci);
        }

        [Test]
        public void LinkXmlKeepsWhatIsOnlyReachedByReflection()
        {
            var x = Read("Assets", "_Aron", "link.xml");
            StringAssert.Contains("Aron.Logic", x);
            StringAssert.Contains("System.Security.Cryptography", x);
            StringAssert.Contains("Aron.Store.Iap", x);
            var p = Read("Assets", "Plugins", "Android", "proguard-user.txt");
            StringAssert.Contains("com.ahmedalamrani.aron", p);
            StringAssert.Contains("renamesourcefileattribute", p);
        }

        [Test]
        public void ProjectFilesShipNoDebugSymbolsOrSourceMaps()
        {
            var bad = Directory.GetFiles(Root(), "*", SearchOption.AllDirectories)
                .Where(f => !f.Contains(Path.DirectorySeparatorChar + ".git" + Path.DirectorySeparatorChar) && !f.Contains(Path.DirectorySeparatorChar + "bin" + Path.DirectorySeparatorChar) && !f.Contains(Path.DirectorySeparatorChar + "obj" + Path.DirectorySeparatorChar)
                    && !new[] { "Library", "Temp", "Logs", ".utmp" }.Any(skip => f.Contains(Path.DirectorySeparatorChar + skip + Path.DirectorySeparatorChar)))   // Unity's local caches are never committed
                .Where(f => new[] { ".pdb", ".mdb", ".map" }.Contains(Path.GetExtension(f).ToLowerInvariant())).ToList();
            Assert.IsEmpty(bad);
        }

        // ---------------------------------------------------------------- soft device checks

        [Test]
        public void OneWeakHintIsNotRootButTwoStrongOnesAre()
        {
            Assert.AreEqual(SoftFlag.None, DeviceChecks.Evaluate(new DeviceSignals { testKeysBuild = true }));
            Assert.AreEqual(SoftFlag.None, DeviceChecks.Evaluate(new DeviceSignals { systemWritable = true }));
            Assert.AreEqual(SoftFlag.RootedDevice, DeviceChecks.Evaluate(new DeviceSignals { testKeysBuild = true, systemWritable = true }));
            Assert.AreEqual(SoftFlag.RootedDevice, DeviceChecks.Evaluate(new DeviceSignals { suBinaryFound = true }));
            Assert.AreEqual(SoftFlag.RootedDevice, DeviceChecks.Evaluate(new DeviceSignals { jailbreakFilesFound = true }));
            Assert.AreEqual(SoftFlag.None, DeviceChecks.Evaluate(new DeviceSignals { unknownInstaller = true, emulatorHints = true }), "side-loading and emulators are not flags by themselves");
        }

        [Test]
        public void DebuggerAndRepackagedBuildsAreFlaggedButADevBuildMayDebug()
        {
            Assert.IsTrue(DeviceChecks.Evaluate(new DeviceSignals { debuggerAttached = true }).HasFlag(SoftFlag.DebuggerAttached));
            Assert.AreEqual(SoftFlag.None, DeviceChecks.Evaluate(new DeviceSignals { debuggerAttached = true }, developmentBuild: true));
            Assert.IsTrue(DeviceChecks.Evaluate(new DeviceSignals { signatureMismatch = true }).HasFlag(SoftFlag.ModifiedBuild));
            Assert.IsTrue(DeviceChecks.Evaluate(new DeviceSignals { injectionLibraryLoaded = true }).HasFlag(SoftFlag.MemoryEdit));
            Assert.AreEqual(SoftFlag.None, DeviceChecks.Evaluate(null));
        }

        [Test]
        public void FlagsNeverTurnOfflinePlayOff()
        {
            SoftFlags.Clear();
            SoftFlags.Raise(DeviceChecks.Evaluate(new DeviceSignals { suBinaryFound = true, debuggerAttached = true, signatureMismatch = true }));
            Assert.IsTrue(SoftFlags.OnlinePlayFlagged);
            Assert.IsTrue(IntegrityPolicy.OfflinePlayAllowed);
            SoftFlags.Clear();
            Assert.IsFalse(SoftFlags.Any);
        }

        // ---------------------------------------------------------------- certificate pins

        private static byte[] Key(byte b) => Enumerable.Repeat(b, 32).ToArray();

        [Test]
        public void PinsAcceptTheirKeyAndRefuseOthersWhenEnforced()
        {
            var pins = new CertPins(new[] { CertPins.PinOf(Key(1)), CertPins.PinOf(Key(2)) }, PinMode.Enforce);
            Assert.IsTrue(pins.Accept(Key(1)));
            Assert.IsTrue(pins.Accept(Key(2)), "the backup pin works too");
            Assert.IsFalse(pins.Accept(Key(3)));
            Assert.IsFalse(pins.Accept(null));
            Assert.AreEqual(2, pins.Mismatches);
        }

        [Test]
        public void ReportOnlyAndOffNeverBreakAConnection()
        {
            var pins = new CertPins(new[] { CertPins.PinOf(Key(1)) }, PinMode.ReportOnly);
            Assert.IsTrue(pins.Accept(Key(9))); Assert.AreEqual(1, pins.Mismatches);
            pins.Mode = PinMode.Off;
            Assert.IsTrue(pins.Accept(Key(9)));
            Assert.IsTrue(new CertPins(new string[0], PinMode.Enforce).Accept(Key(9)), "no pins configured: nothing to enforce");
        }

        [Test]
        public void EnforcingWithOnePinIsNotSafe()
        {
            Assert.IsFalse(CertPins.SafeToEnforce(1));
            Assert.IsTrue(CertPins.SafeToEnforce(2));
        }
    }
}
