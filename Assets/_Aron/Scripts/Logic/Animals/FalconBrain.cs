// (c) Ahmed Alamrani. All rights reserved.
using System.Collections.Generic;

namespace Aron.Logic
{
    public enum FalconState { Hover, Perched, Swooping, Fetching, Returning }

    /// <summary>A point in the world, kept engine free so the falcon rules can be tested.</summary>
    public readonly struct Spot
    {
        public readonly float x, y, z;
        public Spot(float x, float y, float z) { this.x = x; this.y = y; this.z = z; }

        public float SqrDistance(Spot o)
        {
            float dx = x - o.x, dy = y - o.y, dz = z - o.z;
            return dx * dx + dy * dy + dz * dz;
        }
    }

    /// <summary>
    /// The falcon's decisions: when it may swoop at an enemy, when it fetches a coin, and when it lands on
    /// Aron's arm. Swoops share one cooldown so the falcon helps without playing the level for you.
    /// </summary>
    public class FalconBrain
    {
        public float SwoopCooldown { get; set; } = 7f;
        public float SwoopRange { get; set; } = 8f;
        public float FetchInterval { get; set; } = 4.5f;
        public float FetchRange { get; set; } = 6f;
        public float PerchDelay { get; set; } = 2.5f;
        public float MaxTripTime { get; set; } = 2.5f;

        public FalconState State { get; private set; } = FalconState.Hover;

        private float lastSwoop = float.NegativeInfinity, lastFetch = float.NegativeInfinity;
        private float stillSince = float.NegativeInfinity, tripStart;
        private bool still;

        public bool Busy => State == FalconState.Swooping || State == FalconState.Fetching || State == FalconState.Returning;

        public float SwoopCooldownLeft(float now)
        {
            float left = SwoopCooldown - (now - lastSwoop);
            return left > 0f ? left : 0f;
        }

        public bool CanSwoop(float now) => !Busy && SwoopCooldownLeft(now) <= 0f;
        public bool CanFetch(float now) => !Busy && now - lastFetch >= FetchInterval;

        /// <summary>Index of the nearest target within range, or -1.</summary>
        public static int Nearest(Spot from, IReadOnlyList<Spot> targets, float range)
        {
            int best = -1;
            float bestD = range * range;
            for (int i = 0; i < targets.Count; i++)
            {
                float d = from.SqrDistance(targets[i]);
                if (d <= bestD) { best = i; bestD = d; }
            }
            return best;
        }

        /// <summary>On an attack: picks the enemy to swoop at and starts the cooldown, or returns -1.</summary>
        public int TrySwoop(float now, Spot from, IReadOnlyList<Spot> enemies)
        {
            if (!CanSwoop(now)) return -1;
            int i = Nearest(from, enemies, SwoopRange);
            if (i < 0) return -1;
            lastSwoop = now;
            Begin(FalconState.Swooping, now);
            return i;
        }

        public int TryFetch(float now, Spot from, IReadOnlyList<Spot> coins)
        {
            if (!CanFetch(now)) return -1;
            lastFetch = now;
            int i = Nearest(from, coins, FetchRange);
            if (i >= 0) Begin(FalconState.Fetching, now);
            return i;
        }

        /// <summary>How far away a key can be and still be fetched: twice the coin range.</summary>
        public float KeyRange => FetchRange * 2f;

        /// <summary>A locked gate's key hangs out of reach: the falcon goes for it if it is free and the key is within KeyRange.</summary>
        public bool TryFetchKey(float now, Spot from, Spot key)
        {
            if (!CanFetch(now) || from.SqrDistance(key) > KeyRange * KeyRange) return false;
            lastFetch = now;
            Begin(FalconState.Fetching, now);
            return true;
        }

        /// <summary>Reached the target (or it vanished); head back to Aron.</summary>
        public void Arrived(float now)
        {
            if (State == FalconState.Swooping || State == FalconState.Fetching) Begin(FalconState.Returning, now);
        }

        public void BackHome() { if (State == FalconState.Returning) State = FalconState.Hover; }

        /// <summary>Call every frame with whether Aron is standing still on the ground.</summary>
        public void Tick(float now, bool playerStill)
        {
            if (playerStill && !still) stillSince = now;
            still = playerStill;

            if ((State == FalconState.Swooping || State == FalconState.Fetching) && now - tripStart > MaxTripTime) Begin(FalconState.Returning, now);
            else if (State == FalconState.Returning && now - tripStart > MaxTripTime * 2f) State = FalconState.Hover;

            if (State == FalconState.Hover && still && now - stillSince >= PerchDelay) State = FalconState.Perched;
            else if (State == FalconState.Perched && !still) State = FalconState.Hover;
        }

        private void Begin(FalconState s, float now)
        {
            State = s;
            tripStart = now;
        }
    }
}
