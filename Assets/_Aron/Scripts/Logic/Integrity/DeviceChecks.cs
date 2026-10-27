// (c) Ahmed Alamrani. All rights reserved.
using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;

namespace Aron.Logic.Integrity
{
    /// <summary>What the phone-side probe saw. Every value is a hint; none of them is proof.</summary>
    public class DeviceSignals
    {
        public bool suBinaryFound, testKeysBuild, rootManagerAppFound, systemWritable;
        public bool jailbreakFilesFound, sandboxEscaped, injectionLibraryLoaded;
        public bool debuggerAttached, debuggableBuild, emulatorHints;
        /// <summary>Installed from somewhere other than the official store (side-loaded).</summary>
        public bool unknownInstaller;
        /// <summary>The app signing certificate hash differs from the release one (repackaged build).</summary>
        public bool signatureMismatch;
    }

    public static class DeviceChecks
    {
        /// <summary>Turns signals into soft flags. Two or more weak hints are needed for "rooted"; a debugger or a signature mismatch is enough alone.</summary>
        public static SoftFlag Evaluate(DeviceSignals s, bool developmentBuild = false)
        {
            var f = SoftFlag.None;
            if (s == null) return f;
            int root = (s.suBinaryFound ? 2 : 0) + (s.rootManagerAppFound ? 2 : 0) + (s.testKeysBuild ? 1 : 0) + (s.systemWritable ? 1 : 0)
                     + (s.jailbreakFilesFound ? 2 : 0) + (s.sandboxEscaped ? 2 : 0);
            if (root >= 2) f |= SoftFlag.RootedDevice;
            if (!developmentBuild && (s.debuggerAttached || s.debuggableBuild)) f |= SoftFlag.DebuggerAttached;
            if (s.injectionLibraryLoaded) f |= SoftFlag.ModifiedBuild | SoftFlag.MemoryEdit;
            if (s.signatureMismatch) f |= SoftFlag.ModifiedBuild;
            return f;
        }

        /// <summary>Android paths whose presence suggests root.</summary>
        public static readonly string[] AndroidRootPaths =
        {
            "/system/bin/su", "/system/xbin/su", "/sbin/su", "/system/sd/xbin/su", "/system/bin/failsafe/su", "/data/local/su", "/su/bin/su",
            "/data/adb/magisk", "/sbin/.magisk",
        };

        public static readonly string[] AndroidRootPackages = { "com.topjohnwu.magisk", "eu.chainfire.supersu", "com.noshufou.android.su", "me.weishu.kernelsu" };

        /// <summary>iOS paths whose presence suggests a jailbreak.</summary>
        public static readonly string[] IosJailbreakPaths =
        {
            "/Applications/Cydia.app", "/Library/MobileSubstrate/MobileSubstrate.dylib", "/bin/bash", "/usr/sbin/sshd", "/etc/apt",
            "/private/var/lib/apt/", "/var/jb", "/Applications/Sileo.app",
        };
    }

    // ------------------------------------------------------------------ certificate pinning

    public enum PinMode { Off, ReportOnly, Enforce }

    /// <summary>
    /// Pins for our own backend host. A pin is the base64 SHA-256 of the server certificate's public key bytes (see tools/pin_hash.py).
    /// Always ship at least two (current + next key) so a certificate renewal can't lock players out; <see cref="PinMode.ReportOnly"/> logs
    /// a mismatch without failing. The mode can be moved between report-only and enforce from the server config (see <c>PinPolicy</c>), so a mistaken
    /// rotation can be relaxed without an app update; "off" needs a new version.
    /// </summary>
    public class CertPins
    {
        private readonly HashSet<string> pins = new HashSet<string>(StringComparer.Ordinal);
        public PinMode Mode { get; set; } = PinMode.ReportOnly;
        public int Mismatches { get; private set; }

        public CertPins(IEnumerable<string> base64Sha256Pins, PinMode mode = PinMode.ReportOnly)
        {
            Mode = mode;
            if (base64Sha256Pins != null) foreach (var p in base64Sha256Pins) if (!string.IsNullOrWhiteSpace(p)) pins.Add(p.Trim());
        }

        public int Count => pins.Count;

        public static string PinOf(byte[] publicKeyBytes)
        {
            using (var h = SHA256.Create()) return Convert.ToBase64String(h.ComputeHash(publicKeyBytes));
        }

        /// <summary>True when the connection may go ahead.</summary>
        public bool Accept(byte[] serverPublicKeyBytes)
        {
            if (Mode == PinMode.Off || pins.Count == 0) return true;
            bool match = serverPublicKeyBytes != null && pins.Contains(PinOf(serverPublicKeyBytes));
            if (match) return true;
            Mismatches++;
            return Mode == PinMode.ReportOnly;
        }

        /// <summary>A config needs two pins before enforcing; one pin and Enforce is refused (it would lock everyone out at the next renewal).</summary>
        public static bool SafeToEnforce(int pinCount) => pinCount >= 2;
    }
}
