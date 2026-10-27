// (c) Ahmed Alamrani. All rights reserved.
using System;
using System.Collections.Generic;
using System.Linq;
using Aron.Logic.Regions;

namespace Aron.Logic.Tests
{
    /// <summary>Walks a generated stage and lists anything the player couldn't get past.</summary>
    public static class StageCheck
    {
        public const float MaxCheckpointSeconds = 115f;

        public static List<string> Problems(StageLayout l)
        {
            var errors = new List<string>();
            string mount = l.Stage.mount ?? "";
            var ground = l.Of(PieceKind.Ground).OrderBy(p => p.z).ToList();
            if (ground.Count == 0) { errors.Add("no ground"); return errors; }
            var movers = l.Of(PieceKind.Mover).ToList();

            for (int i = 1; i < ground.Count; i++)
            {
                var a = ground[i - 1];
                var b = ground[i];
                float gap = b.Start - a.End;
                float step = b.y - a.y;
                bool bridged = movers.Any(m => m.z >= a.End - 0.1f && m.z <= a.End + 2.5f);
                if (gap < -0.05f) errors.Add($"ground overlaps at {b.Start:0.0}");
                if (!bridged && gap > StageLayout.MaxGap(mount) + 0.01f) errors.Add($"gap {gap:0.0} m at {a.End:0.0}");
                if (bridged && gap - (movers.First(m => m.z >= a.End - 0.1f).sizeA + 4f) > 0.6f) errors.Add($"moving platform falls short at {a.End:0.0}");
                if (step > StageLayout.MaxStep(mount) + 0.01f) errors.Add($"step {step:0.0} m at {b.Start:0.0}");
            }

            // Never much more than a minute and a half of play between checkpoints.
            float last = 0f;
            foreach (var c in l.Of(PieceKind.Checkpoint).Select(p => p.time).Concat(new[] { l.EstimatedSeconds }).OrderBy(t => t))
            {
                if (c - last > MaxCheckpointSeconds) errors.Add($"no checkpoint for {c - last:0} s before {c:0} s");
                last = c;
            }

            foreach (var p in l.Pieces)
            {
                bool onGround = p.kind == PieceKind.Patrol || p.kind == PieceKind.Chaser || p.kind == PieceKind.CrateBasic
                    || p.kind == PieceKind.CrateLife || p.kind == PieceKind.CrateBounce || p.kind == PieceKind.Checkpoint
                    || p.kind == PieceKind.Goal || p.kind == PieceKind.Hurdle || p.kind == PieceKind.MountPost
                    || p.kind == PieceKind.CrateBisht || p.kind == PieceKind.PuzzleGate || p.kind == PieceKind.PuzzlePart || p.kind == PieceKind.Ambush;
                if (onGround && float.IsNaN(l.GroundAt(p.z))) errors.Add($"{p.kind} floating at {p.z:0.0}");
                if (p.kind == PieceKind.SlideBeam && mount.Length > 0) errors.Add("slide beam in a riding stage");
            }

            if (l.Stage.boss)
            {
                if (l.Count(PieceKind.BossArena) != 1) errors.Add("boss stage needs one arena");
                if (l.Count(PieceKind.Goal) != 0) errors.Add("boss stage has a plain goal");
                if (Math.Abs(ground[ground.Count - 1].y) > 0.01f) errors.Add("arena bridge doesn't meet the ground");
            }
            else if (l.Count(PieceKind.Goal) != 1) errors.Add("stage needs one goal");

            errors.AddRange(PuzzleCheck.Problems(l));
            if (l.Count(PieceKind.MountPost) != l.Count(PieceKind.RideEnd)) errors.Add("every ride needs an end");
            if (mount.Length > 0 && (l.Count(PieceKind.MountPost) != 1 || l.Count(PieceKind.RideEnd) != 1))
                errors.Add("riding stage needs a post and a ride end");
            if (l.Count(PieceKind.Landmark) == 0) errors.Add("no landmark");
            return errors;
        }
    }
}
