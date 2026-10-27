// (c) Ahmed Alamrani. All rights reserved.
namespace Aron.Logic.Audio
{
    /// <summary>
    /// Decides which pooled voice a new sound gets. Each id has its own cap so ten crates breaking
    /// at once don't drown out everything else; when the pool or the cap is full the oldest voice is reused.
    /// </summary>
    public class SfxVoiceLimiter
    {
        private readonly string[] ids;
        private readonly float[] started;
        private readonly float[] ends;

        public int Capacity => ids.Length;

        public SfxVoiceLimiter(int voices)
        {
            ids = new string[voices];
            started = new float[voices];
            ends = new float[voices];
        }

        public int Playing(string id, float now)
        {
            int n = 0;
            for (int i = 0; i < ids.Length; i++) if (ids[i] == id && ends[i] > now) n++;
            return n;
        }

        public int Active(float now)
        {
            int n = 0;
            for (int i = 0; i < ids.Length; i++) if (ids[i] != null && ends[i] > now) n++;
            return n;
        }

        /// <returns>The voice to play on.</returns>
        public int Acquire(string id, float now, float duration, int perIdCap)
        {
            int slot = Playing(id, now) >= perIdCap ? Oldest(id, now) : Free(now);
            if (slot < 0) slot = Oldest(null, now);
            ids[slot] = id;
            started[slot] = now;
            ends[slot] = now + duration;
            return slot;
        }

        public void Release(int slot)
        {
            if (slot < 0 || slot >= ids.Length) return;
            ids[slot] = null;
            ends[slot] = 0f;
        }

        private int Free(float now)
        {
            for (int i = 0; i < ids.Length; i++) if (ids[i] == null || ends[i] <= now) return i;
            return -1;
        }

        private int Oldest(string id, float now)
        {
            int best = -1;
            for (int i = 0; i < ids.Length; i++)
            {
                if (id != null && (ids[i] != id || ends[i] <= now)) continue;
                if (best < 0 || started[i] < started[best]) best = i;
            }
            return best;
        }
    }
}
