// (c) Ahmed Alamrani. All rights reserved.
using System;
using System.Collections.Generic;

namespace Aron.Logic.Audio
{
    /// <summary>Picks a random take for a sound without playing the same one twice in a row.</summary>
    public class SfxVariantPicker
    {
        private readonly Random random;
        private readonly Dictionary<string, int> last = new Dictionary<string, int>();

        public SfxVariantPicker(int seed) { random = new Random(seed); }
        public SfxVariantPicker() { random = new Random(); }

        public int Next(string id, int count)
        {
            if (count <= 1) return 0;
            last.TryGetValue(id, out int previous);
            int pick = random.Next(count - 1);
            if (last.ContainsKey(id) && pick >= previous) pick++;
            last[id] = pick;
            return pick;
        }

        /// <summary>Uniform value in [min, max], used for pitch and volume jitter.</summary>
        public float Range(float min, float max) => min + (float)random.NextDouble() * (max - min);
    }
}
