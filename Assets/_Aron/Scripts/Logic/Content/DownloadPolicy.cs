// (c) Ahmed Alamrani. All rights reserved.
using System;

namespace Aron.Logic.Content
{
    public enum NetworkKind { None, Cellular, Wifi }

    public enum DownloadVerdict
    {
        /// <summary>The content is already on the device.</summary>
        NotNeeded,
        /// <summary>Start downloading.</summary>
        Go,
        /// <summary>On mobile data without the player's say-so: ask first (progress screen only).</summary>
        AskCellular,
        /// <summary>A background fetch on mobile data: do nothing until Wi-Fi comes.</summary>
        WaitForWifi,
        /// <summary>No connection.</summary>
        Offline,
        /// <summary>A stage is being played: never download now.</summary>
        BlockedDuringGameplay,
    }

    /// <summary>Wi-Fi only by default, mobile data only after the player agrees, nothing at all while a stage is running.</summary>
    public static class DownloadPolicy
    {
        /// <param name="needed">The group is not on the device yet.</param>
        /// <param name="cellularOptIn">The saved setting "download over mobile data".</param>
        /// <param name="confirmedNow">The player just tapped "use mobile data" on the progress screen.</param>
        /// <param name="interactive">True on the progress screen (may ask), false for a quiet prefetch from the map.</param>
        public static DownloadVerdict Decide(bool needed, NetworkKind network, bool cellularOptIn, bool confirmedNow, bool inGameplay, bool interactive)
        {
            if (!needed) return DownloadVerdict.NotNeeded;
            if (inGameplay) return DownloadVerdict.BlockedDuringGameplay;
            switch (network)
            {
                case NetworkKind.None: return DownloadVerdict.Offline;
                case NetworkKind.Wifi: return DownloadVerdict.Go;
                default:
                    if (cellularOptIn || confirmedNow) return DownloadVerdict.Go;
                    return interactive ? DownloadVerdict.AskCellular : DownloadVerdict.WaitForWifi;
            }
        }

        /// <summary>Size to show the player, e.g. "3.2 MB".</summary>
        public static string FormatBytes(long bytes)
        {
            if (bytes < 1024) return bytes + " B";
            double mb = bytes / 1048576.0;
            if (mb < 1.0) return (bytes / 1024.0).ToString("0.#", System.Globalization.CultureInfo.InvariantCulture) + " KB";
            return mb.ToString("0.#", System.Globalization.CultureInfo.InvariantCulture) + " MB";
        }
    }

    public enum DownloadPhase { Idle, AskingCellular, Downloading, Done, Failed, Skipped }

    /// <summary>What the progress screen shows, one transition at a time (so it can be tested without a screen or a network).</summary>
    public sealed class DownloadSession
    {
        public DownloadPhase Phase { get; private set; } = DownloadPhase.Idle;
        public long TotalBytes { get; private set; }
        public long DoneBytes { get; private set; }
        public string Error { get; private set; } = "";
        public bool Finished => Phase == DownloadPhase.Done || Phase == DownloadPhase.Failed || Phase == DownloadPhase.Skipped;

        public float Progress => Phase == DownloadPhase.Done ? 1f : TotalBytes <= 0 ? 0f : Math.Min(1f, (float)DoneBytes / TotalBytes);

        public void Start(DownloadVerdict verdict, long totalBytes)
        {
            TotalBytes = Math.Max(0, totalBytes); DoneBytes = 0; Error = "";
            switch (verdict)
            {
                case DownloadVerdict.NotNeeded: Phase = DownloadPhase.Done; break;
                case DownloadVerdict.Go: Phase = DownloadPhase.Downloading; break;
                case DownloadVerdict.AskCellular: Phase = DownloadPhase.AskingCellular; break;
                case DownloadVerdict.Offline: Fail("offline"); break;
                case DownloadVerdict.WaitForWifi: Phase = DownloadPhase.AskingCellular; break;
                default: Fail("not now"); break;
            }
        }

        /// <summary>The player chose "use mobile data".</summary>
        public bool ConfirmCellular() { if (Phase != DownloadPhase.AskingCellular) return false; Phase = DownloadPhase.Downloading; return true; }

        public void Report(long done) { if (Phase == DownloadPhase.Downloading) DoneBytes = Math.Max(DoneBytes, Math.Min(done, TotalBytes > 0 ? TotalBytes : done)); }
        public void Complete() { if (Phase == DownloadPhase.Downloading) { DoneBytes = TotalBytes; Phase = DownloadPhase.Done; } }
        public void Fail(string why) { Error = why ?? ""; Phase = DownloadPhase.Failed; }

        /// <summary>The player chose to play without the country music, or to wait for Wi-Fi.</summary>
        public void Skip() { if (Phase != DownloadPhase.Done && Phase != DownloadPhase.Skipped) Phase = DownloadPhase.Skipped; }   // also after a failure: "play without it"
    }
}
