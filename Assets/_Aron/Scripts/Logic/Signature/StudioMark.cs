// (c) Ahmed Alamrani. All rights reserved.
using System;
using System.Text;

namespace Aron.Logic.Signature
{
    /// <summary>
    /// The studio mark. The text is not stored as a plain string: it is kept XOR-encoded and only decoded on demand,
    /// so it does not turn up in a plain string search of the binary. It travels in every save header (<c>sig</c>)
    /// and in the build info of the hidden performance overlay. It is never shown in normal UI.
    /// </summary>
    public static class StudioMark
    {
        private static readonly byte[] Blob = { 0x1C, 0x8D, 0x3F, 0x9F, 0xCC, 0xF6, 0x51, 0xD6, 0x5D, 0x64, 0xD1, 0x16, 0x61, 0x22 };
        private static readonly byte[] Pad = { 0x5A, 0xC3, 0x17, 0x9E, 0x2B, 0x74, 0xD1 };
        private static string cached;

        /// <summary>The decoded mark.</summary>
        public static string Value => cached ?? (cached = Decode(Blob, Pad));

        /// <summary>Decodes a blob made by <see cref="Encode"/>.</summary>
        public static string Decode(byte[] blob, byte[] pad)
        {
            if (blob == null || pad == null || pad.Length == 0) return "";
            var raw = new byte[blob.Length];
            for (int i = 0; i < blob.Length; i++) raw[i] = (byte)(blob[i] ^ pad[i % pad.Length] ^ ((i * 31 + 7) & 0xFF));
            return Encoding.UTF8.GetString(raw);
        }

        /// <summary>The inverse of <see cref="Decode"/> (used by tests and the tooling that produced the blob).</summary>
        public static byte[] Encode(string text, byte[] pad)
        {
            var raw = Encoding.UTF8.GetBytes(text ?? "");
            var blob = new byte[raw.Length];
            for (int i = 0; i < raw.Length; i++) blob[i] = (byte)(raw[i] ^ pad[i % pad.Length] ^ ((i * 31 + 7) & 0xFF));
            return blob;
        }

        /// <summary>True when <paramref name="s"/> is exactly the mark.</summary>
        public static bool Matches(string s) => string.Equals(s, Value, StringComparison.Ordinal);

        /// <summary>The line for the hidden credit easter egg.</summary>
        public static string Credit => "Built by " + Value;

        /// <summary>Short fingerprint of the mark (FNV-1a, 8 hex digits) for places that must not carry the text itself.</summary>
        public static string Fingerprint
        {
            get
            {
                uint h = 2166136261;
                foreach (char c in Value) { h ^= c; h *= 16777619; }
                return h.ToString("x8");
            }
        }
    }

    /// <summary>
    /// Long-press timer for the hidden credit: call <see cref="Begin"/> on press, <see cref="Cancel"/> on release,
    /// and <see cref="Tick"/> while held. <see cref="Tick"/> returns true once, when the hold reaches 5 seconds.
    /// </summary>
    public class HoldTimer
    {
        public const float Seconds = 5f;
        private float start = -1f;
        private bool fired;

        public bool Holding => start >= 0f;

        public void Begin(float now) { start = now; fired = false; }

        public void Cancel() { start = -1f; fired = false; }

        public bool Tick(float now)
        {
            if (start < 0f || fired) return false;
            if (now - start < Seconds) return false;
            fired = true;
            return true;
        }
    }
}
