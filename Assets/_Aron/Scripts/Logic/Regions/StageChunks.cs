// (c) Ahmed Alamrani. All rights reserved.
using System;
using System.Collections.Generic;

namespace Aron.Logic.Regions
{
    /// <summary>
    /// Splits a stage into short stretches along z so only the part around the player is built. A stretch is built
    /// when the player gets within <see cref="Ahead"/> of it and taken down once it is well behind both the player and
    /// the last checkpoint. Puzzle parts go with their gate so a gate never waits on a lever that isn't there.
    /// </summary>
    public class StageChunks
    {
        public const float Size = 40f;
        public const float Ahead = 150f;
        public const float Behind = 90f;
        public const float SceneryAhead = 170f;
        public const float SceneryBehind = 100f;

        private readonly List<int>[] pieces;
        public int Count => pieces.Length;

        public StageChunks(IReadOnlyList<Piece> all, float length)
        {
            int n = Math.Max(1, (int)Math.Ceiling(Math.Max(length, 1f) / Size) + 1);
            pieces = new List<int>[n];
            for (int i = 0; i < n; i++) pieces[i] = new List<int>();

            var gateChunk = new Dictionary<int, int>();
            for (int i = 0; i < all.Count; i++)
                if (all[i].kind == PieceKind.PuzzleGate) gateChunk[(int)all[i].sizeB] = ChunkOf(all[i].z);

            // Gates first so their parts can find them.
            for (int pass = 0; pass < 2; pass++)
                for (int i = 0; i < all.Count; i++)
                {
                    var p = all[i];
                    bool gate = p.kind == PieceKind.PuzzleGate;
                    if ((pass == 0) != gate) continue;
                    int c = p.kind == PieceKind.PuzzlePart && gateChunk.TryGetValue((int)p.sizeB, out int g) ? g : ChunkOf(Key(p));
                    pieces[c].Add(i);
                }
        }

        /// <summary>Where a piece counts as being: the start of a ground run, otherwise its z.</summary>
        public static float Key(Piece p) => p.kind == PieceKind.Ground ? p.Start : p.z;

        public int ChunkOf(float z) => Math.Max(0, Math.Min(pieces.Length - 1, (int)Math.Floor(z / Size)));

        public IReadOnlyList<int> PiecesIn(int chunk) => pieces[chunk];

        public static float From(int chunk) => chunk * Size;
        public static float To(int chunk) => (chunk + 1) * Size;

        /// <summary>Should this stretch be standing, with the player at z and the last checkpoint at anchor?</summary>
        public static bool Keep(int chunk, float playerZ, float anchorZ)
            => From(chunk) <= playerZ + Ahead && To(chunk) >= Math.Min(playerZ, anchorZ) - Behind;

        /// <summary>Should the scenery of this stretch be switched on?</summary>
        public static bool Show(int chunk, float playerZ)
            => From(chunk) <= playerZ + SceneryAhead && To(chunk) >= playerZ - SceneryBehind;

        /// <summary>
        /// When a stretch that was taken down has to go up again (he walked back), only the solid parts come back:
        /// no second helping of coins, boxes, enemies or puzzles.
        /// </summary>
        public static bool Rebuilt(PieceKind k)
        {
            switch (k)
            {
                case PieceKind.Ground:
                case PieceKind.Mover:
                case PieceKind.SlideBeam:
                case PieceKind.Hurdle:
                case PieceKind.RideEnd:
                case PieceKind.Landmark:
                    return true;
                default:
                    return false;
            }
        }
    }
}
