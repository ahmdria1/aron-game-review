// (c) Ahmed Alamrani. All rights reserved.
namespace Aron.Logic
{
    public enum LooseState { Bolting, Waiting, Ready, GoHome }

    /// <summary>
    /// A camel or horse that has just thrown its rider. It skips aside, settles, then waits close by so he can
    /// climb back on. If he runs on and leaves it well behind, it goes back to its post.
    /// </summary>
    public static class LooseMount
    {
        public const float BoltSeconds = 0.6f;
        public const float SettleSeconds = 1.5f;
        public const float LeftBehind = 30f;
        /// <summary>How far it may skip sideways from the middle of the track.</summary>
        public const float MaxSide = 2.4f;

        public static LooseState At(float sinceThrown, float riderAheadBy)
        {
            if (sinceThrown < BoltSeconds) return LooseState.Bolting;
            if (riderAheadBy > LeftBehind) return LooseState.GoHome;
            if (sinceThrown < SettleSeconds) return LooseState.Waiting;
            return LooseState.Ready;
        }

        public static float ClampSide(float x) => x < -MaxSide ? -MaxSide : x > MaxSide ? MaxSide : x;
    }
}
