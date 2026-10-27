// (c) Ahmed Alamrani. All rights reserved.
namespace Aron.Logic
{
    public enum MountKind { None, Camel, Horse }

    public enum MountPhase { OnFoot, Mounting, Riding, Thrown }

    /// <summary>What a hit did to the rider.</summary>
    public enum HitOutcome
    {
        /// <summary>Not riding, so the hit counts as normal damage.</summary>
        Hurt,
        /// <summary>The mount took it and threw the rider off. No life lost.</summary>
        ThrownOff,
        /// <summary>Already thrown and still in the grace window; ignore it.</summary>
        Ignored,
    }

    public readonly struct MountStats
    {
        public readonly float speed, jump, acceleration, seatHeight;

        public MountStats(float speed, float jump, float acceleration, float seatHeight)
        {
            this.speed = speed; this.jump = jump; this.acceleration = acceleration; this.seatHeight = seatHeight;
        }

        public static readonly MountStats OnFoot = new MountStats(1f, 1f, 1f, 0f);

        /// <summary>The camel jumps higher; the horse runs faster.</summary>
        public static MountStats For(MountKind kind)
        {
            switch (kind)
            {
                case MountKind.Camel: return new MountStats(1.3f, 1.55f, 0.8f, 1.75f);
                case MountKind.Horse: return new MountStats(1.7f, 1.2f, 1.1f, 1.35f);
                default: return OnFoot;
            }
        }
    }

    /// <summary>
    /// Riding rules: walk into a mount to climb on, a hit throws you off instead of costing a life,
    /// and you can't climb straight back on while you're still tumbling.
    /// </summary>
    public class MountRide
    {
        public float MountTime { get; set; } = 0.35f;
        public float ThrownTime { get; set; } = 1.2f;
        public float RemountDelay { get; set; } = 1.5f;

        public MountPhase Phase { get; private set; } = MountPhase.OnFoot;
        public MountKind Kind { get; private set; } = MountKind.None;

        private float phaseStart, lastOff = float.NegativeInfinity;

        public bool IsMounted => Phase == MountPhase.Mounting || Phase == MountPhase.Riding;
        public bool CanControl => Phase != MountPhase.Mounting;
        public MountStats Stats => IsMounted ? MountStats.For(Kind) : MountStats.OnFoot;

        public bool CanMount(MountKind kind, float now)
        {
            if (kind == MountKind.None) return false;
            if (Phase == MountPhase.Riding) return kind != Kind;
            if (Phase != MountPhase.OnFoot) return false;
            return now - lastOff >= RemountDelay;
        }

        /// <summary>Climb on, or swap to a different animal while riding.</summary>
        public bool TryMount(MountKind kind, float now)
        {
            if (!CanMount(kind, now)) return false;
            Kind = kind;
            Enter(MountPhase.Mounting, now);
            return true;
        }

        public HitOutcome TakeHit(float now)
        {
            if (Phase == MountPhase.Thrown) return HitOutcome.Ignored;
            if (!IsMounted) return HitOutcome.Hurt;
            Enter(MountPhase.Thrown, now);
            lastOff = now;
            return HitOutcome.ThrownOff;
        }

        /// <summary>Getting off on purpose, e.g. at the end of the desert. The animal stays where it is.</summary>
        public bool Dismount(float now)
        {
            if (!IsMounted) return false;
            Kind = MountKind.None;
            Enter(MountPhase.OnFoot, now);
            lastOff = now;
            return true;
        }

        public void Tick(float now)
        {
            float t = now - phaseStart;
            if (Phase == MountPhase.Mounting && t >= MountTime) Enter(MountPhase.Riding, now);
            else if (Phase == MountPhase.Thrown && t >= ThrownTime)
            {
                Kind = MountKind.None;
                Enter(MountPhase.OnFoot, now);
            }
        }

        /// <summary>Death or a restart: straight back on foot, no remount delay.</summary>
        public void Reset()
        {
            Phase = MountPhase.OnFoot;
            Kind = MountKind.None;
            lastOff = float.NegativeInfinity;
        }

        private void Enter(MountPhase p, float now)
        {
            Phase = p;
            phaseStart = now;
        }
    }
}
