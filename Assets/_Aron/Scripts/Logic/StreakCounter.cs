// (c) Ahmed Alamrani. All rights reserved.
using System.Collections.Generic;

namespace Aron.Logic
{
    /// <summary>Counts events that land within a sliding time window, e.g. coins picked up in quick succession.</summary>
    public class StreakCounter
    {
        private readonly Queue<float> hits = new Queue<float>();
        public float Window { get; }
        public int Threshold { get; }
        public int Count => hits.Count;

        public StreakCounter(float window, int threshold)
        {
            Window = window;
            Threshold = threshold;
        }

        /// <returns>True when this hit completes a streak. The counter resets so the next streak starts fresh.</returns>
        public bool Hit(float time)
        {
            while (hits.Count > 0 && time - hits.Peek() > Window) hits.Dequeue();
            hits.Enqueue(time);
            if (hits.Count < Threshold) return false;
            hits.Clear();
            return true;
        }

        public void Reset() => hits.Clear();
    }
}
