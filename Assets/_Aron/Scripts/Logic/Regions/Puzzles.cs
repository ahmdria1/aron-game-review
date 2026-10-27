// (c) Ahmed Alamrani. All rights reserved.
using System;
using System.Collections.Generic;

namespace Aron.Logic.Regions
{
    public static class PuzzleKinds
    {
        /// <summary>Pull every lever (hit it or walk into it) and the gate lifts.</summary>
        public const string Lever = "lever";
        /// <summary>Plates light up in an order; step on them in that order.</summary>
        public const string Runes = "runes";
        /// <summary>A switch opens the gate for a few seconds; run for it.</summary>
        public const string Timed = "timed";
        /// <summary>Push the crate along until it sits on the plate.</summary>
        public const string Crate = "crate";
        /// <summary>A locked gate only Noura can pick.</summary>
        public const string Lock = "lock";

        public static readonly string[] All = { Lever, Runes, Timed, Crate, Lock };
    }

    public enum PuzzleStep { Nothing, Progress, Wrong, Solved }

    /// <summary>Every lever has to be pulled. Pulling one twice does nothing.</summary>
    public class LeverPuzzle
    {
        private readonly bool[] pulled;
        public bool Solved { get; private set; }
        public int Pulled { get; private set; }
        public int Count => pulled.Length;

        public LeverPuzzle(int count) => pulled = new bool[Math.Max(1, count)];

        public PuzzleStep Pull(int index)
        {
            if (Solved || index < 0 || index >= pulled.Length || pulled[index]) return PuzzleStep.Nothing;
            pulled[index] = true;
            Pulled++;
            if (Pulled < pulled.Length) return PuzzleStep.Progress;
            Solved = true;
            return PuzzleStep.Solved;
        }
    }

    /// <summary>Plates stepped on in order 0, 1, 2... A wrong one puts the whole thing back to the start.</summary>
    public class RunePuzzle
    {
        public int Count { get; }
        public int Next { get; private set; }
        public bool Solved { get; private set; }

        public RunePuzzle(int count) => Count = Math.Max(1, count);

        /// <param name="rank">The plate's place in the order.</param>
        public PuzzleStep Step(int rank)
        {
            if (Solved) return PuzzleStep.Nothing;
            // Standing on the last right plate again is fine.
            if (rank < Next) return rank == Next - 1 ? PuzzleStep.Nothing : Reset();
            if (rank != Next) return Reset();
            Next++;
            if (Next < Count) return PuzzleStep.Progress;
            Solved = true;
            return PuzzleStep.Solved;
        }

        private PuzzleStep Reset() { Next = 0; return PuzzleStep.Wrong; }
    }

    /// <summary>A gate that stays open for a while after its switch is hit, and can be hit again.</summary>
    public class TimedDoor
    {
        public float OpenSeconds { get; }
        private float closesAt = float.NegativeInfinity;

        public TimedDoor(float openSeconds) => OpenSeconds = openSeconds;
        public void Trigger(float now) => closesAt = now + OpenSeconds;
        public bool IsOpen(float now) => now < closesAt;
        public float Left(float now) => Math.Max(0f, closesAt - now);
    }

    /// <summary>The crate only moves forward, and stops when it reaches the plate.</summary>
    public class CratePush
    {
        public float Position { get; private set; }
        public float Plate { get; }
        public bool Solved => Position >= Plate - 0.01f;

        public CratePush(float start, float plate) { Position = start; Plate = Math.Max(start, plate); }

        public PuzzleStep Push(float metres)
        {
            if (Solved || metres <= 0f) return PuzzleStep.Nothing;
            Position = Math.Min(Plate, Position + metres);
            return Solved ? PuzzleStep.Solved : PuzzleStep.Progress;
        }
    }

    /// <summary>Plays each puzzle in a layout the way a player would and lists any that can't be finished.</summary>
    public static class PuzzleCheck
    {
        public static List<string> Problems(StageLayout l)
        {
            var errors = new List<string>();
            var parts = new Dictionary<int, List<Piece>>();
            foreach (var p in l.Of(PieceKind.PuzzlePart))
            {
                int id = (int)p.sizeB;
                if (!parts.TryGetValue(id, out var list)) parts[id] = list = new List<Piece>();
                list.Add(p);
            }
            bool riding = !string.IsNullOrEmpty(l.Stage.mount);
            foreach (var g in l.Of(PieceKind.PuzzleGate))
            {
                int id = (int)g.sizeB;
                string where = $"{g.tag} #{id} at {g.z:0}";
                parts.TryGetValue(id, out var mine);
                mine = mine ?? new List<Piece>();
                if (float.IsNaN(l.GroundAt(g.z))) { errors.Add(where + ": gate over a gap"); continue; }
                foreach (var p in mine)
                {
                    if (p.z >= g.z - 0.5f) errors.Add(where + ": part behind its own gate");
                    if (!SameGround(l, p.z, g.z)) errors.Add(where + ": part not on the gate's ground");
                    if (Math.Abs(p.x) > l.HalfWidth - 0.5f) errors.Add(where + ": part inside the wall");
                }
                switch (g.tag)
                {
                    case PuzzleKinds.Lever:
                    {
                        var lp = new LeverPuzzle((int)g.sizeA);
                        if (mine.Count != lp.Count) { errors.Add(where + ": lever count"); break; }
                        foreach (var p in mine) lp.Pull((int)p.sizeA);
                        if (!lp.Solved) errors.Add(where + ": levers don't open it");
                        break;
                    }
                    case PuzzleKinds.Runes:
                    {
                        var rp = new RunePuzzle((int)g.sizeA);
                        if (mine.Count != rp.Count) { errors.Add(where + ": plate count"); break; }
                        mine.Sort((a, b) => a.sizeA.CompareTo(b.sizeA));
                        foreach (var p in mine) rp.Step((int)p.sizeA);
                        if (!rp.Solved) errors.Add(where + ": order can't be followed");
                        break;
                    }
                    case PuzzleKinds.Timed:
                    {
                        if (mine.Count != 1) { errors.Add(where + ": needs one switch"); break; }
                        float run = (g.z - mine[0].z) / StageLayout.FootPace;
                        var door = new TimedDoor(g.sizeA);
                        door.Trigger(0f);
                        if (!door.IsOpen(run + 1f)) errors.Add(where + ": shuts before a walker gets there");
                        break;
                    }
                    case PuzzleKinds.Crate:
                    {
                        if (mine.Count != 2) { errors.Add(where + ": needs a crate and a plate"); break; }
                        mine.Sort((a, b) => a.sizeA.CompareTo(b.sizeA));
                        var crate = mine[0]; var plate = mine[1];
                        if (Math.Abs(crate.x - plate.x) > 0.01f) errors.Add(where + ": plate not in the crate's lane");
                        var push = new CratePush(crate.z, plate.z);
                        for (int i = 0; i < 100 && !push.Solved; i++) push.Push(0.25f);
                        if (!push.Solved) errors.Add(where + ": crate never reaches the plate");
                        if (plate.z + 1f > g.z) errors.Add(where + ": crate would block the gate");
                        break;
                    }
                    case PuzzleKinds.Lock:
                        if (riding) errors.Add(where + ": Noura can't pick a lock from the saddle");
                        if (mine.Count != 0) errors.Add(where + ": a lock has no parts");
                        break;
                    default:
                        errors.Add(where + ": unknown puzzle");
                        break;
                }
            }
            foreach (var id in parts.Keys)
            {
                bool found = false;
                foreach (var g in l.Of(PieceKind.PuzzleGate)) if ((int)g.sizeB == id) found = true;
                if (!found) errors.Add("puzzle parts with no gate: #" + id);
            }
            return errors;
        }

        private static bool SameGround(StageLayout l, float a, float b)
        {
            foreach (var p in l.Of(PieceKind.Ground))
                if (a >= p.Start - 0.01f && a <= p.End + 0.01f) return b >= p.Start - 0.01f && b <= p.End + 0.01f;
            return false;
        }
    }
}
