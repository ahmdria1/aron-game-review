// (c) Ahmed Alamrani. All rights reserved.
using System;
using System.Collections.Generic;
using Aron.Logic.Regions;
using Aron.Logic.Safety;

namespace Aron.Logic.Access
{
    /// <summary>
    /// Assist mode: three small helps for a player who finds a run hard. Lantern checkpoints come sooner, the boulder in a chase runs
    /// slower, and the landing shadow is bigger and darker. That is all it does. It never locks a stage, a medal, a coin, a costume or any
    /// reward, never shortens the stage, and never changes what a finished stage is worth, so progress and rewards are the same with it on or off.
    /// </summary>
    public static class AssistRules
    {
        /// <summary>Checkpoint spacing is multiplied by this (90 s becomes 54 s).</summary>
        public const float CheckpointFactor = 0.6f;
        /// <summary>No two lanterns closer than this.</summary>
        public const float MinCheckpointSeconds = 20f;
        /// <summary>The chase boulder's speed is multiplied by this.</summary>
        public const float ChaseSpeedFactor = 0.8f;

        public static bool On(AccessSettings a) => a != null && a.assistMode;

        /// <summary>Seconds of play between lantern checkpoints.</summary>
        public static float CheckpointSeconds(AccessSettings a, float normal)
            => On(a) ? Math.Max(MinCheckpointSeconds, normal * CheckpointFactor) : normal;

        /// <summary>
        /// Adds lantern checkpoints to a finished layout so that no stretch of play is longer than about <paramref name="targetSeconds"/>.
        /// Nothing else in the layout changes: the same pieces in the same places, so every coin, crate, bisht box and the goal stay exactly as
        /// they are without assist. A checkpoint goes only on open ground at least <see cref="MinGround"/> m long and well clear of enemies,
        /// puzzles, chases, ambushes, conversations and mount posts. Returns how many were added.
        /// </summary>
        public static int AddCheckpoints(StageLayout layout, float targetSeconds)
        {
            if (layout == null) return 0;
            targetSeconds = Math.Max(MinCheckpointSeconds, targetSeconds);
            int added = 0;
            float last = 0f;
            for (int i = 0; i < layout.Pieces.Count; i++)
            {
                var p = layout.Pieces[i];
                if (p.kind == PieceKind.Checkpoint) { last = p.time; continue; }
                if (p.kind != PieceKind.Ground || i == 0 || p.sizeB < MinGround) continue;
                if (p.time - last < targetSeconds) continue;
                // try the middle of the ground first, then spots either side of it
                float at = float.NaN;
                for (float off = 0f; off <= p.sizeB * 0.5f - 1.5f && float.IsNaN(at); off += 1.5f)
                {
                    if (Clear(layout, p.z + off)) at = p.z + off;
                    else if (off > 0f && Clear(layout, p.z - off)) at = p.z - off;
                }
                if (float.IsNaN(at)) continue;
                var cp = new Piece { kind = PieceKind.Checkpoint, x = 0f, y = p.y, z = at, tag = null, time = p.time };
                layout.Pieces.Insert(i + 1, cp);
                last = p.time;
                added++;
                i++;
            }
            return added;
        }

        /// <summary>Shortest ground that can hold an extra lantern box.</summary>
        public const float MinGround = 10f;

        private static bool Clear(StageLayout layout, float z)
        {
            foreach (var o in layout.Pieces)
            {
                switch (o.kind)
                {
                    case PieceKind.Patrol: case PieceKind.Chaser: case PieceKind.Mover: case PieceKind.SlideBeam: case PieceKind.Hurdle:
                        if (Math.Abs(o.z - z) < 3.5f) return false; break;
                    case PieceKind.Talk: case PieceKind.MountPost: case PieceKind.RideEnd:
                        if (Math.Abs(o.z - z) < 8f) return false; break;
                    case PieceKind.Checkpoint:
                        if (Math.Abs(o.z - z) < 12f) return false; break;
                    case PieceKind.CrateBasic: case PieceKind.CrateBounce: case PieceKind.CrateLife: case PieceKind.CrateBisht:
                        if (Math.Abs(o.z - z) < 2.5f) return false; break;
                    case PieceKind.PuzzleGate: case PieceKind.PuzzlePart:
                        if (Math.Abs(o.z - z) < 14f) return false; break;
                    case PieceKind.Ambush:
                        if (z > o.z - 26f && z < o.z + 8f) return false; break;
                    case PieceKind.ChaseRun:
                        if (z > o.Start - 8f && z < o.End + 8f) return false; break;
                    case PieceKind.Goal: case PieceKind.BossArena:
                        if (Math.Abs(o.z - z) < 12f) return false; break;
                }
            }
            return true;
        }

        /// <summary>Multiplier on the chase boulder's speed.</summary>
        public static float ChaseSpeed(AccessSettings a) => On(a) ? ChaseSpeedFactor : 1f;

        /// <summary>Whether the landing shadow is boosted (see <c>DropShadow.AssistBoost</c>).</summary>
        public static bool ShadowBoost(AccessSettings a) => On(a);

        /// <summary>The player's own speed, jump and every reward are untouched; these are the things assist leaves alone.</summary>
        public static readonly string[] Untouched = { "progress", "unlocks", "coins", "bisht", "medals", "costumes", "player speed", "jump" };
    }
}
