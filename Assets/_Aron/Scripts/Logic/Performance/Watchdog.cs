// (c) Ahmed Alamrani. All rights reserved.
using System;
using System.Text;

namespace Aron.Logic.Performance
{
    /// <summary>
    /// Tells a separate checker when the main thread stopped answering. The game thread calls
    /// <see cref="Beat"/> once per frame; a checker that wakes about once a second calls
    /// <see cref="Check"/>. A stall is reported once, not every second, and time spent in the background
    /// or in a load never counts.
    /// </summary>
    public class FreezeDetector
    {
        public long ThresholdMs = 2000;
        private long lastBeat;
        private bool reported;
        private int suspended;

        public int Freezes { get; private set; }
        public bool Suspended => suspended > 0;

        public FreezeDetector(long startMs = 0) { lastBeat = startMs; }

        public void Beat(long nowMs) { lastBeat = nowMs; reported = false; }

        /// <summary>Loads and the background legitimately stop the heartbeat.</summary>
        public void Suspend() { suspended++; }

        public void Resume(long nowMs)
        {
            if (suspended > 0) suspended--;
            if (suspended == 0) { lastBeat = nowMs; reported = false; }
        }

        /// <summary>True exactly once per stall that lasted longer than the threshold.</summary>
        public bool Check(long nowMs, out long stalledMs)
        {
            stalledMs = nowMs - lastBeat;
            if (suspended > 0 || reported || stalledMs < ThresholdMs) return false;
            reported = true;
            Freezes++;
            return true;
        }
    }

    /// <summary>
    /// The last few things the player did (scene, stage start, purchase), kept in a fixed ring so crash
    /// reports show what led up to the problem. Add one on events, never per frame.
    /// </summary>
    public class Breadcrumbs
    {
        private readonly string[] text;
        private readonly long[] at;
        private int next, count;

        public Breadcrumbs(int capacity = 30) { text = new string[capacity < 1 ? 1 : capacity]; at = new long[text.Length]; }
        public int Count => count;

        public void Add(string message, long nowMs)
        {
            text[next] = message;
            at[next] = nowMs;
            next = (next + 1) % text.Length;
            if (count < text.Length) count++;
        }

        /// <summary>Oldest first, as "+seconds message" lines relative to <paramref name="nowMs"/>.</summary>
        public string Dump(long nowMs, int maxChars = 2000)
        {
            var sb = new StringBuilder();
            int start = (next - count + text.Length) % text.Length;
            for (int i = 0; i < count; i++)
            {
                int k = (start + i) % text.Length;
                sb.Append('-').Append(((nowMs - at[k]) / 1000.0).ToString("0.0", System.Globalization.CultureInfo.InvariantCulture)).Append("s ").Append(text[k]).Append('\n');
            }
            return sb.Length > maxChars ? sb.ToString(sb.Length - maxChars, maxChars) : sb.ToString();
        }

        public void Clear() { Array.Clear(text, 0, text.Length); next = count = 0; }
    }
}
