// (c) Ahmed Alamrani. All rights reserved.
namespace Aron.Logic
{
    public enum DeathOutcome { Respawn, RestartStage }

    /// <summary>
    /// Where the hero comes back. Breaking a lantern box saves that spot; losing a life puts him at the last one
    /// broken. Losing the last life sends the whole stage back to the start.
    /// </summary>
    public class CheckpointTrail
    {
        public struct Spot
        {
            public float x, y, z;
            public Spot(float x, float y, float z) { this.x = x; this.y = y; this.z = z; }
            public bool Near(Spot o) => (x - o.x) * (x - o.x) + (y - o.y) * (y - o.y) + (z - o.z) * (z - o.z) < 0.01f;
            public override string ToString() => $"{x:0},{y:0},{z:0}";
        }

        public Spot Start { get; }
        public Spot Current { get; private set; }
        public int Saves { get; private set; }

        public CheckpointTrail(Spot start) { Start = start; Current = start; }

        /// <returns>False when this spot is already the one saved.</returns>
        public bool Save(Spot at)
        {
            if (at.Near(Current)) return false;
            Current = at;
            Saves++;
            return true;
        }

        public DeathOutcome Died(bool livesLeft)
        {
            if (livesLeft) return DeathOutcome.Respawn;
            Current = Start;
            Saves = 0;
            return DeathOutcome.RestartStage;
        }
    }
}
