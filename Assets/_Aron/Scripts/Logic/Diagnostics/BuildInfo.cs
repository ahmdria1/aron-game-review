// (c) Ahmed Alamrani. All rights reserved.
using Aron.Logic.Signature;

namespace Aron.Logic.Diagnostics
{
    /// <summary>Build identification for the hidden performance overlay and for bug reports.</summary>
    public static class BuildInfo
    {
        /// <summary>One line for the overlay: version and the studio fingerprint.</summary>
        public static string Line(string version) => "build v" + (version ?? "?") + " #" + StudioMark.Fingerprint;

        /// <summary>The full record (version plus the signature field), used when a report is exported.</summary>
        public static string Export(string version) => "version=" + (version ?? "?") + ";sig=" + StudioMark.Value;
    }
}
