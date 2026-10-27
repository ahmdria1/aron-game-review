// (c) Ahmed Alamrani. All rights reserved.
namespace Aron.Logic
{
    public enum FrameRateChange { None, DropTo30, LowerTier }

    /// <summary>
    /// Watches frame times during play. A phone that can't hold its target for several seconds (often
    /// because it is heating up and throttling) is moved to 30 fps, and if even that slips, the caller is
    /// asked to lower the graphics tier. It never climbs back up mid-session: bouncing between rates
    /// feels worse than a steady 30 and keeps the phone warm.
    /// </summary>
    public class FrameRateGovernor
    {
        public float WarmupSeconds = 3f;
        public float SlowSeconds = 6f;
        public float Smoothing = 1f;
        public float MaxCountedFrame = 0.25f;

        private float average, warmup, slowFor;

        public int Target { get; private set; }

        public FrameRateGovernor(int target)
        {
            Target = target;
            Reset();
        }

        /// <summary>Call after loading a scene: the first seconds are full of one-off hitches.</summary>
        public void Reset()
        {
            warmup = WarmupSeconds;
            slowFor = 0f;
            average = 1f / Target;
        }

        public FrameRateChange Update(float dt)
        {
            if (dt <= 0f || dt > MaxCountedFrame) return FrameRateChange.None;
            if (warmup > 0f) { warmup -= dt; return FrameRateChange.None; }

            float k = dt / (Smoothing + dt);
            average += (dt - average) * k;

            // Allow a little slack: 60 fps counts as missed under ~50, 30 fps under ~27.
            float limit = Target >= 60 ? 1f / 50f : 1f / 27f;
            slowFor = average > limit ? slowFor + dt : 0f;
            if (slowFor < SlowSeconds) return FrameRateChange.None;

            slowFor = 0f;
            warmup = WarmupSeconds;
            if (Target > 30)
            {
                Target = 30;
                average = 1f / 30f;
                return FrameRateChange.DropTo30;
            }
            average = 1f / 30f;
            return FrameRateChange.LowerTier;
        }
    }
}
