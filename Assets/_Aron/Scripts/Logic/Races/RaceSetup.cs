// (c) Ahmed Alamrani. All rights reserved.
using System;
using System.Collections.Generic;
using Aron.Logic.Online;

namespace Aron.Logic.Races
{
    public enum GhostPick { OwnBest, Friend, Pace }

    public class GhostChoice
    {
        public GhostPick pick;
        public Ghost ghost;
    }

    /// <summary>
    /// How a race starts when nobody has chosen anything (Resources/Races/race_setup.json). Offline the default is a ghost race: no internet,
    /// no account, no waiting, just you against your best time, a friend's pasted code, or (the first time) a pace ghost the game makes.
    /// Races against computer racers and online ranked races are one tap away.
    /// </summary>
    [Serializable]
    public class RaceSetupFile
    {
        public string defaultOffline = "ghost", defaultOnline = "ranked";
        public int minRealRacersForRanked = 2;
        public PaceFactors paceFactor = new PaceFactors();
        /// <summary>Metres per second of a steady pace per kind of race.</summary>
        public SpeedEntry[] referenceSpeed = Array.Empty<SpeedEntry>();

        [Serializable] public class PaceFactors { public float easy = 0.7f, normal = 0.85f, hard = 1f; }
        [Serializable] public class SpeedEntry { public string kind; public float speed; }

        public float SpeedOf(string kind)
        {
            foreach (var s in referenceSpeed) if (s.kind == kind) return s.speed;
            return 0f;
        }

        public float Factor(string difficulty)
            => difficulty == "easy" ? paceFactor.easy : difficulty == "hard" ? paceFactor.hard : paceFactor.normal;

        public List<string> Validate()
        {
            var e = new List<string>();
            if (defaultOffline != "ghost") e.Add("the offline default race is the ghost race");
            if (defaultOnline != "ranked") e.Add("the online default is ranked");
            if (minRealRacersForRanked < 2) e.Add("ranked needs at least 2 real racers");
            if (paceFactor.easy <= 0f || paceFactor.easy > paceFactor.normal || paceFactor.normal > paceFactor.hard || paceFactor.hard > 1.05f)
                e.Add("pace factors must rise easy < normal < hard and stay at or under the top speed");
            foreach (var k in RaceFile.Kinds) if (SpeedOf(k) <= 0f) e.Add("reference speed missing for " + k);
            return e;
        }
    }

    public static class RaceSetup
    {
        /// <summary>The race mode to start with: ranked only when online with enough real racers, otherwise the ghost race.</summary>
        public static RaceMode DefaultMode(RaceSetupFile f, bool online, int realRacers)
            => online && realRacers >= f.minRealRacersForRanked ? RaceMode.Ranked : RaceMode.Ghost;

        /// <summary>
        /// The ghost to line up against. A pasted friend code is used when it decodes and belongs to this track; else the player's own best;
        /// else a pace ghost, so the very first race has an opponent too.
        /// </summary>
        public static GhostChoice Choose(RaceSetupFile f, RaceTrack track, string difficulty, Ghost ownBest, string friendCode)
        {
            var friend = string.IsNullOrWhiteSpace(friendCode) ? null : Ghost.FromShareCode(friendCode);
            if (friend != null && friend.track == track.id && friend.time > 0f && friend.frames.Count > 1)
                return new GhostChoice { pick = GhostPick.Friend, ghost = friend };
            if (ownBest != null && ownBest.track == track.id && ownBest.time > 0f && ownBest.frames.Count > 1)
                return new GhostChoice { pick = GhostPick.OwnBest, ghost = ownBest };
            return new GhostChoice { pick = GhostPick.Pace, ghost = PaceGhost(f, track, difficulty) };
        }

        /// <summary>Seconds a steady pace takes: the track's total length at the kind's reference speed times the difficulty's share.</summary>
        public static float PaceSeconds(RaceSetupFile f, RaceTrack t, string difficulty)
        {
            float v = f.SpeedOf(t.kind) * f.Factor(difficulty);
            return v <= 0f ? 0f : t.TotalLength / v;
        }

        /// <summary>A ghost that runs the track at an even pace (z is the distance along the track, x the centre line).</summary>
        public static Ghost PaceGhost(RaceSetupFile f, RaceTrack t, string difficulty)
        {
            float secs = PaceSeconds(f, t, difficulty);
            var g = new Ghost { track = t.id, hero = "aron", costume = "pace", time = secs };
            if (secs <= 0f) return g;
            int n = Math.Min(Ghost.MaxFrames, (int)Math.Ceiling(secs * Ghost.Hz) + 1);
            for (int i = 0; i < n; i++)
            {
                float k = Math.Min(1f, i / (secs * Ghost.Hz));
                g.Add(new GhostFrame { x = 0f, y = 0f, z = t.TotalLength * k, yaw = 0f, anim = 1 });
            }
            return g;
        }
    }
}
