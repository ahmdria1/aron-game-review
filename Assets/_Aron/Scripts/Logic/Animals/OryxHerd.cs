// (c) Ahmed Alamrani. All rights reserved.
using System;

namespace Aron.Logic.Animals
{
    public enum OryxState { Graze, Alert, Flee }

    /// <summary>
    /// How a wild herd of Arabian oryx reacts to the hero: it grazes, lifts its head when someone comes near and trots calmly away.
    /// It never panics, never charges and is never hurt. The distances match the clips oryx_wild_idle_graze, _alert, _walk, _trot and _flee.
    /// </summary>
    public static class OryxHerd
    {
        public const float AlertDistance = 22f, FleeDistance = 13f, CalmDistance = 26f;
        public const float WalkSpeed = 1.2f, TrotSpeed = 3.6f, MaxFleeMetres = 40f;

        /// <summary>The next state from the distance to the hero. Hysteresis: once fleeing, it stays on its way until the hero is far.</summary>
        public static OryxState Next(OryxState now, float distance)
        {
            if (distance <= FleeDistance) return OryxState.Flee;
            if (now == OryxState.Flee) return distance >= CalmDistance ? OryxState.Alert : OryxState.Flee;
            return distance <= AlertDistance ? OryxState.Alert : OryxState.Graze;
        }

        /// <summary>Metres per second in a state: still while grazing or alert, a gentle trot when fleeing.</summary>
        public static float Speed(OryxState s) => s == OryxState.Flee ? TrotSpeed : 0f;

        /// <summary>The herd only runs as far as it needs to, never off the stage and never at the hero.</summary>
        public static float FleeMetres(float distance) => Math.Max(0f, Math.Min(MaxFleeMetres, CalmDistance - distance));
    }
}
