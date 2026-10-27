// (c) Ahmed Alamrani. All rights reserved.
using System;

namespace Aron.Logic
{
    public enum DeathCause { Enemy, Pit, Water, Hazard }

    /// <summary>
    /// The little comic act played when a hero loses a life, before he comes back at the last lantern. Each beat
    /// is something the scene shows; nothing hurts, it's slapstick.
    /// </summary>
    public static class DeathActs
    {
        // Aron
        public const string Spin = "spin", Topple = "topple", Stars = "stars", AgalRolls = "agal_rolls";
        public const string ShemaghUp = "shemagh_up", Wave = "wave", Drop = "drop";
        public const string Bubbles = "bubbles", ShemaghFloats = "shemagh_floats", Sink = "sink";
        // Noura
        public const string AbayaSwirl = "abaya_swirl", SitDown = "sit_down", BagBeside = "bag_beside", Uff = "uff", OryxNudge = "oryx_nudge";

        public static string[] Beats(string hero, DeathCause cause)
        {
            bool noura = hero == "noura";
            switch (cause)
            {
                case DeathCause.Pit:
                    return noura ? new[] { AbayaSwirl, Drop, Uff } : new[] { ShemaghUp, Wave, Drop };
                case DeathCause.Water:
                    return noura ? new[] { AbayaSwirl, Bubbles, Uff } : new[] { Bubbles, Sink, ShemaghFloats };
                default:
                    return noura ? new[] { AbayaSwirl, SitDown, Stars, BagBeside, Uff, OryxNudge } : new[] { Spin, Topple, Stars, AgalRolls };
            }
        }

        /// <summary>How long the act runs before the respawn.</summary>
        public static float Seconds(string hero, DeathCause cause)
        {
            switch (cause)
            {
                case DeathCause.Pit: return 1.6f;
                case DeathCause.Water: return 1.9f;
                default: return hero == "noura" ? 2.4f : 2f;
            }
        }

        /// <summary>Falling off the track: water stages (coast, river) drop him in the water, the rest in a pit.</summary>
        public static DeathCause ForFall(string terrain) => terrain == "coast" || terrain == "river" ? DeathCause.Water : DeathCause.Pit;

        /// <summary>What Noura says sitting down.</summary>
        public const string UffAr = "أفّ";

        public static bool Has(string hero, DeathCause cause, string beat) => Array.IndexOf(Beats(hero, cause), beat) >= 0;
    }
}
