// (c) Ahmed Alamrani. All rights reserved.
namespace Aron.Logic
{
    public enum HemPhase { Down, Reach, Lift, Bite, Hold, Release, Drop }

    /// <summary>
    /// Running in a thobe: he bends for the front hem with his right hand, lifts it to his face, takes the edge
    /// in his teeth and then runs with both arms free. When he slows down he lets go and the hem falls back.
    /// Raise is how far the front hem has come up (0 hanging, 1 at the mouth), Hand how much the right hand is
    /// on the hem, Pump how much both arms swing with the run.
    /// </summary>
    public class HemGrab
    {
        public const float ReachTime = 0.14f, LiftTime = 0.16f, BiteTime = 0.08f, ReleaseTime = 0.08f, DropTime = 0.18f;

        public HemPhase Phase { get; private set; } = HemPhase.Down;
        public float Raise { get; private set; }
        public float Hand { get; private set; }
        public float Pump { get; private set; }
        /// <summary>The hem edge is between his teeth.</summary>
        public bool Bitten => Phase == HemPhase.Hold || Phase == HemPhase.Bite && progress >= 1f;

        private float progress;

        public void Tick(bool sprinting, float dt)
        {
            if (sprinting && (Phase == HemPhase.Down || Phase == HemPhase.Drop || Phase == HemPhase.Release)) Enter(HemPhase.Reach);
            else if (!sprinting && (Phase == HemPhase.Reach || Phase == HemPhase.Lift || Phase == HemPhase.Bite || Phase == HemPhase.Hold))
                Enter(Phase == HemPhase.Hold || Phase == HemPhase.Bite ? HemPhase.Release : HemPhase.Drop);

            float span = Span(Phase);
            if (span > 0f) progress += dt / span;
            if (progress >= 1f)
            {
                progress = 1f;
                switch (Phase)
                {
                    case HemPhase.Reach: Enter(HemPhase.Lift); break;
                    case HemPhase.Lift: Enter(HemPhase.Bite); break;
                    case HemPhase.Bite: Enter(HemPhase.Hold); break;
                    case HemPhase.Release: Enter(HemPhase.Drop); break;
                    case HemPhase.Drop: Enter(HemPhase.Down); break;
                }
            }
            Pose();
        }

        private void Enter(HemPhase p)
        {
            // Coming back in the middle of a drop picks up from where the hem is, not from the floor.
            startRaise = Raise; startHand = Hand; startPump = Pump;
            Phase = p;
            progress = 0f;
        }

        private float startRaise, startHand, startPump;

        private static float Span(HemPhase p)
        {
            switch (p)
            {
                case HemPhase.Reach: return ReachTime;
                case HemPhase.Lift: return LiftTime;
                case HemPhase.Bite: return BiteTime;
                case HemPhase.Release: return ReleaseTime;
                case HemPhase.Drop: return DropTime;
                default: return 0f;
            }
        }

        private static float Smooth(float t) => t * t * (3f - 2f * t);
        private static float Lerp(float a, float b, float t) => a + (b - a) * t;

        private void Pose()
        {
            float k = Smooth(progress);
            switch (Phase)
            {
                case HemPhase.Down: Raise = 0f; Hand = 0f; Pump = 0f; break;
                // Hand goes down to the hem; the hem doesn't move yet.
                case HemPhase.Reach: Raise = Lerp(startRaise, 0f, k); Hand = Lerp(startHand, 1f, k); Pump = Lerp(startPump, 0f, k); break;
                // Hand carries the hem up to just under the chin.
                case HemPhase.Lift: Raise = Lerp(startRaise, 0.88f, k); Hand = 1f; Pump = 0f; break;
                // Teeth take it; the hand lets go.
                case HemPhase.Bite: Raise = Lerp(startRaise, 1f, k); Hand = Lerp(startHand, 0f, k); Pump = Lerp(startPump, 1f, k); break;
                case HemPhase.Hold: Raise = 1f; Hand = 0f; Pump = 1f; break;
                // Mouth opens, the hem slips a little.
                case HemPhase.Release: Raise = Lerp(startRaise, 0.85f, k); Hand = 0f; Pump = Lerp(startPump, 0f, k); break;
                // And falls back down to the ankles.
                case HemPhase.Drop: Raise = Lerp(startRaise, 0f, k * k); Hand = Lerp(startHand, 0f, k); Pump = Lerp(startPump, 0f, k); break;
            }
        }
    }
}
