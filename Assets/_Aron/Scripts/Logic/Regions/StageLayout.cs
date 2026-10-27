// (c) Ahmed Alamrani. All rights reserved.
using System;
using System.Collections.Generic;

namespace Aron.Logic.Regions
{
    public enum PieceKind
    {
        Ground, Coin, Patrol, Chaser, CrateBasic, CrateBounce, CrateLife, Mover, SlideBeam, Hurdle,
        Checkpoint, MountPost, RideEnd, Landmark, Talk, Goal, BossArena,
        CrateBisht, PuzzleGate, PuzzlePart, ChaseRun, Ambush,
    }

    /// <summary>
    /// One thing in a stage. Ground: (x, y top, z centre), size = (half width, length). Mover: z is where it starts,
    /// size.x is how far it travels. Landmark: tag is the kind, x is the side it stands on.
    /// PuzzleGate: tag is the puzzle kind, sizeA its setting (levers, runes, open seconds), sizeB the puzzle number.
    /// PuzzlePart: a lever, rune plate, switch, crate or plate of puzzle sizeB; sizeA is its place in the order.
    /// Checkpoint: the lantern box. ChaseRun: z is where the chase starts, sizeB how long it is. CrateBisht: tag says where it sits
    /// ("path", "side" or "puzzle").
    /// <see cref="time"/> is roughly how many seconds into the stage a player gets here.
    /// </summary>
    public struct Piece
    {
        public PieceKind kind;
        public float x, y, z;
        public float sizeA, sizeB;
        public string tag;
        public float time;

        public float Start => z - sizeB * 0.5f;
        public float End => z + sizeB * 0.5f;
        public override string ToString() => $"{kind} ({x:0.#},{y:0.#},{z:0.#}) {tag}";
    }

    /// <summary>
    /// Builds a stage from its definition: a run of ground with gaps, steps, moving platforms, enemies, puzzles,
    /// rides, chases and pick-ups along the z axis. The same stage always comes out the same. Distances are in
    /// metres and are kept inside what the player (or the mount) can actually jump. Every section also adds the
    /// seconds it takes to play, so a stage can be made to last as long as it should.
    /// </summary>
    public class StageLayout
    {
        public const float FootGap = 4.2f;      // single jump at run speed clears about 5.4 m
        public const float FootStep = 1.2f;
        public const float CamelGap = 6f, CamelStep = 1.6f;
        public const float HorseGap = 5.5f, HorseStep = 1.3f;
        /// <summary>The giraffe's long stride clears wide gaps and its height reaches over taller steps.</summary>
        public const float GiraffeGap = 6.2f, GiraffeStep = 2.0f;
        /// <summary>Average forward speed while actually playing (stops, jumps, looking around), in m/s.</summary>
        public const float FootPace = 5.5f, RidePace = 8.5f;
        /// <summary>A checkpoint box after this many seconds of play since the last one.</summary>
        public const float CheckpointSeconds = 90f;
        /// <summary>Every stage plays 5 to 8 minutes (see <see cref="StageTiming"/>).</summary>
        public const float MinStageSeconds = 5f * 60f, MaxStageSeconds = 8f * 60f;
        public const int MinBishtBoxes = 4, MaxBishtBoxes = 6;
        public const float ArenaOffset = 20.5f;   // arena centre past the last ground

        public readonly List<Piece> Pieces = new List<Piece>();
        public float Length { get; private set; }
        public float HalfWidth { get; private set; }
        public StageDef Stage { get; private set; }
        /// <summary>How long the stage takes to play through, in seconds.</summary>
        public float EstimatedSeconds => clock;

        public IEnumerable<Piece> Of(PieceKind k)
        {
            foreach (var p in Pieces) if (p.kind == k) yield return p;
        }

        public int Count(PieceKind k)
        {
            int n = 0;
            foreach (var p in Pieces) if (p.kind == k) n++;
            return n;
        }

        /// <summary>Every box the player can break, for the end screen tally.</summary>
        public int BoxCount => Count(PieceKind.CrateBasic) + Count(PieceKind.CrateBounce) + Count(PieceKind.CrateLife)
            + Count(PieceKind.CrateBisht) + Count(PieceKind.Checkpoint);

        public static float MaxGap(string mount) => mount == "camel" ? CamelGap : mount == "horse" ? HorseGap : mount == "giraffe" ? GiraffeGap : FootGap;
        public static float MaxStep(string mount) => mount == "camel" ? CamelStep : mount == "horse" ? HorseStep : mount == "giraffe" ? GiraffeStep : FootStep;

        /// <summary>What the generator aims for, in seconds: by stage type and difficulty (see <see cref="StageTiming"/>); on a boss stage the walk to the arena.</summary>
        public static float TargetSeconds(StageDef s) => StageTiming.LayoutTargetSeconds(s);

        private Random rnd;
        private int salt, movers, chases, locks;
        private float z, y, clock;
        private float checkpointEvery = CheckpointSeconds;
        /// <summary>Seconds of play between lanterns this layout was built with.</summary>
        public float CheckpointEvery => checkpointEvery;
        private float sinceCheckpoint, sincePuzzle, sinceRide, sinceChase, sinceAmbush, sinceBisht;
        private int puzzles, bishtPlanned;

        private string riding = "";
        private bool rideStage;

        private float R(float a, float b) => a + (float)rnd.NextDouble() * (b - a);
        private bool Chance(float p) => rnd.NextDouble() < p;
        private float Pace => riding.Length > 0 ? RidePace : FootPace;

        public static StageLayout Generate(StageDef stage) => Generate(stage, CheckpointSeconds);

        /// <summary>Same stage with lanterns spaced for a difficulty level (see DifficultyLevel.checkpointSeconds).</summary>
        public static StageLayout Generate(StageDef stage, float checkpointSeconds)
        {
            // A section can run long and the run-out adds time, so build, measure, and nudge the aim until the stage lands on its
            // target (5..8 minutes, by type and difficulty). The same stage always converges the same way.
            float desired = StageTiming.Minutes(stage) * 60f;
            StageLayout best = null;
            float bestErr = float.MaxValue;
            // Pieces come in whole sections, so the length is not smooth in the aim. Close in from both sides (bisection); if one
            // arrangement of sections cannot hit the target, try the next arrangement (salt). The same stage always ends up the same.
            for (int salt = 0; salt < 8 && Math.Abs(bestErr) > 12f; salt++)
            {
                float aim = StageTiming.LayoutTargetSeconds(stage);
                float low = float.NaN, high = float.NaN;   // the largest aim that came out short, the smallest that came out long
                for (int attempt = 0; attempt < 30; attempt++)
                {
                    var l = new StageLayout { checkpointEvery = Math.Max(20f, checkpointSeconds), salt = salt };
                    l.Build(stage, aim);
                    float err = StageTiming.TotalSeconds(stage, l) - desired;
                    if (Math.Abs(err) < Math.Abs(bestErr)) { best = l; bestErr = err; }
                    if (Math.Abs(err) <= 10f) break;
                    if (err < 0f) low = float.IsNaN(low) ? aim : Math.Max(low, aim);
                    else high = float.IsNaN(high) ? aim : Math.Min(high, aim);
                    if (!float.IsNaN(low) && !float.IsNaN(high)) { if (high - low < 1f) break; aim = (low + high) * 0.5f; }
                    else aim -= err;
                }
            }
            return best;
        }

        private void Build(StageDef s, float target)
        {
            Stage = s;
            rnd = new Random(s.Seed + salt * 7919);
            string mount = s.mount ?? "";
            rideStage = mount.Length > 0;
            float d = Math.Max(1, Math.Min(10, s.difficulty));
            float t = (d - 1f) / 9f;
            HalfWidth = rideStage || s.terrain == "desert" ? 5f : 4f;

            // Start pad: talk, the first landmark in view, and the mount tied up if there is one.
            z = -4f; y = 0f;
            Ground(26f);
            Add(PieceKind.Talk, 0f, y + 1f, 4f);
            if (rideStage) { Add(PieceKind.MountPost, -1.5f, y, 12f, 0f, 0f, mount); riding = mount; }

            // Golden boxes are spread over the stage; boss stages have none.
            bishtPlanned = s.boss ? 0 : MinBishtBoxes + rnd.Next(MaxBishtBoxes - MinBishtBoxes + 1);
            // Where each one goes, in order: out in the open, behind a puzzle gate, tucked against a wall.
            string[] spots = { "path", "puzzle", "side", "path", "puzzle", "side" };
            for (int i = 0; i < bishtPlanned; i++) bishtPlan.Enqueue(spots[i]);
            float bishtEvery = target / (bishtPlanned + 1);
            int sections = 0;
            while (clock < target)
            {
                if (s.boss) Section(s, t, sections++);
                else if ((riding.Length == 0 || rideStage) && sincePuzzle > Lerp(60f, 45f, t)) Puzzle(s, t);
                else if (!rideStage && riding.Length == 0 && sinceRide > 110f && clock > 45f && clock < target - 70f) Ride(s, t);
                else if (((sinceChase > (s.type == "chase" ? 70f : 190f) && clock > 40f) || (chases == 0 && clock > target * 0.45f)) && clock < target - 40f) Chase(t);
                else if (sinceAmbush > (s.type == "action" ? 60f : 150f) && clock > 30f) AmbushSection(t);
                else Section(s, t, sections++);

                if (sinceBisht > bishtEvery && bishtPlan.Count > 0 && bishtPlan.Peek() != "puzzle") OpenBisht();
                if (riding.Length > 0 && !rideStage && sinceRide > 35f) EndRide();
            }
            if (riding.Length > 0 && !rideStage) EndRide();
            // Make up any golden boxes the clock ran out on.
            while (bishtPlan.Count > 0)
            {
                if (bishtPlan.Peek() == "puzzle") Puzzle(s, t);
                else OpenBisht();
            }

            // Run-out.
            if (rideStage) { Ground(18f); Add(PieceKind.RideEnd, 0f, y, z - 12f); }
            else Ground(12f);

            if (s.boss)
            {
                // The arena brings its own bridge, so it lines up with the end of the last ground at height 0.
                while (y > 0.01f) { Gap(2.5f, -Math.Min(y, 3f)); Ground(10f); }
                Add(PieceKind.BossArena, 0f, 0f, z + ArenaOffset);
                Length = z + ArenaOffset + 22f;
            }
            else
            {
                Add(PieceKind.Goal, 0f, y, z - 4f);
                Length = z;
            }

            Landmarks(s);
        }

        private readonly Queue<string> bishtPlan = new Queue<string>();

        /// <summary>A golden box out on the main path, or tucked against a wall off the coin line.</summary>
        private void OpenBisht()
        {
            bool side = bishtPlan.Peek() == "side";
            Ground(10f);
            Bisht(side ? (HalfWidth - 0.9f) * (Chance(0.5f) ? 1f : -1f) : 0f, z - 5f);
        }

        private void Bisht(float x, float at)
        {
            Add(PieceKind.CrateBisht, x, y, at, 0f, 0f, bishtPlan.Dequeue());
            sinceBisht = 0f;
        }

        private void Section(StageDef s, float t, int index)
        {
            string mount = riding;
            bool onMount = mount.Length > 0;
            float roll = (float)rnd.NextDouble();
            // Shorter stages still get at least one moving platform on foot.
            if (!onMount && !rideStage && movers == 0 && clock > 30f && index > 0) roll = 0.6f;
            float maxGap = MaxGap(rideStage ? mount : ""), maxStep = MaxStep(rideStage ? mount : "");
            float gapW = Lerp(2.4f, maxGap * (onMount ? 0.95f : 1f), t) * R(0.85f, 1f);

            if (index == 0 || roll < 0.18f)
            {
                // Flat run with enemies.
                Ground(R(16f, 22f) + (onMount ? 6f : 0f));
                Enemies(t, z - 14f, z - 3f);
                Crates(z - 10f);
            }
            else if (roll < 0.40f)
            {
                Gap(gapW, 0f);
                Ground(R(12f, 18f));
                Enemies(t * 0.7f, z - 8f, z - 3f);
            }
            else if (roll < 0.55f)
            {
                // Step up or down. A bounce crate helps on the bigger ones.
                float dy = (Chance(0.6f) || y < 0.5f) ? R(0.6f, maxStep) : -R(0.6f, Math.Min(y, 1.6f));
                if (y + dy > 3.2f) dy = -dy;
                if (dy > 0.9f && !onMount) { Add(PieceKind.CrateBounce, -HalfWidth + 1.5f, y, z - 3f); clock += 0.6f; }
                Gap(Math.Min(gapW, 2.6f), dy);
                Ground(R(12f, 16f));
                Enemies(t * 0.5f, z - 8f, z - 3f);
            }
            else if (roll < 0.70f && !onMount)
            {
                // Moving platform over a long gap: wait for it, ride it over.
                float gap = Lerp(8f, 11f, t);
                float startZ = z + 2f;
                Add(PieceKind.Mover, 0f, y - 0.25f, startZ, gap - 4f);
                movers++;
                ArcCoins(z, z + gap, 1.5f);
                z += gap;
                Tick(4f + gap * 0.3f);
                Ground(R(12f, 16f));
            }
            else if (roll < 0.80f && !onMount)
            {
                Ground(R(16f, 20f));
                Add(PieceKind.SlideBeam, 0f, y + 1.6f, z - 9f);
                Tick(0.5f);
                Enemies(t * 0.5f, z - 5f, z - 2f);
            }
            else if ((mount == "horse" || mount == "giraffe") && rideStage)
            {
                Ground(34f);
                for (int i = 0; i < 3; i++) Add(PieceKind.Hurdle, 0f, y, z - 28f + i * 9f);
                Tick(1.5f);
                if (Chance(t)) Enemy(true, 0f, z - 6f);
            }
            else
            {
                // Crate stack and a life crate now and then; more enemies later in the region.
                Ground(R(14f, 20f));
                Crates(z - 12f);
                if (Chance(0.25f)) { Add(PieceKind.CrateLife, R(-2.5f, 2.5f), y, z - 5f); Tick(0.6f); }
                Enemies(t, z - 10f, z - 3f);
            }
        }

        // ------------------------------------------------------------------ puzzles
        /// <summary>
        /// A gate across the track and what opens it, all on one flat stretch so nothing can be missed. The gate's
        /// sizeB is the puzzle number its parts share.
        /// </summary>
        private void Puzzle(StageDef s, float t)
        {
            // Save just before a puzzle when the last checkpoint is a while back, so a wrong try costs little.
            if (sinceCheckpoint > checkpointEvery * 0.6f) Ground(6f, true, true);
            var kinds = new List<string> { PuzzleKinds.Lever, PuzzleKinds.Runes, PuzzleKinds.Timed, PuzzleKinds.Crate };
            // Noura's lock picking needs her on foot; a ride stage keeps both heroes in the saddle.
            if (!rideStage) kinds.Add(PuzzleKinds.Lock);
            // Each stage starts its puzzles from a different kind, then goes round them.
            string kind = kinds[(puzzles + s.Seed % kinds.Count + kinds.Count) % kinds.Count];
            // Every on-foot stage gives Noura a lock, even the short ones.
            if (!rideStage && locks == 0 && puzzles >= 2) kind = PuzzleKinds.Lock;
            if (kind == PuzzleKinds.Lock) locks++;
            int id = ++puzzles;
            float hw = HalfWidth;
            float from = z;
            switch (kind)
            {
                case PuzzleKinds.Lever:
                {
                    int n = 1 + (t > 0.3f ? 1 : 0) + (t > 0.7f ? 1 : 0);
                    float len = 14f + n * 6f;
                    Ground(len, false);
                    for (int i = 0; i < n; i++)
                        Add(PieceKind.PuzzlePart, (i % 2 == 0 ? -1f : 1f) * (hw - 1.2f), y, from + 4f + i * 6f, i, id, kind);
                    Gate(kind, n, id, from + len - 3f);
                    Tick(8f + n * 6f);
                    break;
                }
                case PuzzleKinds.Runes:
                {
                    int n = 3 + (int)Math.Round(t * 2f);
                    Ground(22f, false);
                    var order = new List<int>();
                    for (int i = 0; i < n; i++) order.Add(i);
                    for (int i = n - 1; i > 0; i--) { int j = rnd.Next(i + 1); (order[i], order[j]) = (order[j], order[i]); }
                    float span = hw * 2f - 2f;
                    for (int i = 0; i < n; i++)
                        Add(PieceKind.PuzzlePart, -span * 0.5f + span * i / (n - 1), y, from + 10f, order[i], id, kind);
                    Gate(kind, n, id, from + 19f);
                    Tick(6f + n * 4f);
                    break;
                }
                case PuzzleKinds.Timed:
                {
                    float dist = Lerp(14f, 24f, t);
                    float open = TimedOpenSeconds(dist, Pace);
                    Ground(dist + 8f, false);
                    Add(PieceKind.PuzzlePart, 0f, y, from + 3f, 0f, id, kind);
                    Gate(kind, open, id, from + 3f + dist);
                    Tick(12f);
                    break;
                }
                case PuzzleKinds.Crate:
                {
                    float push = Lerp(5f, 9f, t);
                    float x = (Chance(0.5f) ? -1f : 1f) * (hw - 1.5f);
                    Ground(push + 12f, false);
                    Add(PieceKind.PuzzlePart, x, y, from + 4f, 0f, id, kind);           // the crate
                    Add(PieceKind.PuzzlePart, x, y, from + 4f + push, 1f, id, kind);    // the plate
                    Gate(kind, push, id, from + push + 9f);
                    Tick(10f + push);
                    break;
                }
                default:
                {
                    Ground(18f, false);
                    Gate(PuzzleKinds.Lock, 0f, id, from + 12f);
                    Tick(15f);
                    break;
                }
            }
            // The reward behind the gate: now and then a golden box only a solved puzzle gets to.
            if (bishtPlan.Count > 0 && bishtPlan.Peek() == "puzzle" && sinceBisht > 60f)
            {
                Ground(8f);
                Bisht(0f, z - 4f);
            }
            else Ground(6f);
            sincePuzzle = 0f;
        }

        /// <summary>The timed gate stays open long enough to walk there at a steady pace, with room to spare.</summary>
        public static float TimedOpenSeconds(float distance, float pace) => distance / pace * 1.6f + 1.5f;

        private void Gate(string kind, float setting, int id, float at)
            => Add(PieceKind.PuzzleGate, 0f, y, at, setting, id, kind);

        // ------------------------------------------------------------------ ride, chase, ambush
        private void Ride(StageDef s, float t)
        {
            string mount = s.terrain == "desert" || s.terrain == "oasis" ? "camel" : "horse";
            Ground(14f);
            Add(PieceKind.MountPost, -1.5f, y, z - 8f, 0f, 0f, mount);
            Tick(2f);
            riding = mount;
            sinceRide = 0f;
        }

        private void EndRide()
        {
            Ground(16f);
            Add(PieceKind.RideEnd, 0f, y, z - 10f);
            riding = "";
            sinceRide = 0f;
        }

        /// <summary>Something big rolls after the hero down a clear stretch: small gaps, nothing to stop for.</summary>
        private void Chase(float t)
        {
            chases++;
            float start = z;
            Ground(10f);
            int legs = 3 + (int)(t * 2f);
            for (int i = 0; i < legs; i++)
            {
                Gap(R(2f, 3.2f), 0f);
                Ground(R(18f, 26f));
            }
            Ground(8f);
            Add(PieceKind.ChaseRun, 0f, 0f, start + (z - start) * 0.5f, 0f, z - start);
            Tick(3f);
            sinceChase = 0f;
        }

        private void AmbushSection(float t)
        {
            Ground(26f);
            Add(PieceKind.Ambush, 0f, y, z - 20f, 2 + (int)(t * 3f));
            Tick(8f + t * 6f);
            sinceAmbush = 0f;
        }

        // ------------------------------------------------------------------ pieces
        private void Tick(float seconds)
        {
            clock += seconds;
            sinceCheckpoint += seconds;
            sincePuzzle += seconds;
            sinceRide += seconds;
            sinceChase += seconds;
            sinceAmbush += seconds;
            sinceBisht += seconds;
        }

        private void Ground(float length, bool coins = true, bool save = false)
        {
            Add(PieceKind.Ground, 0f, y, z + length * 0.5f, HalfWidth, length);
            // The checkpoint box goes where the clock says it's due, on open ground (never among a puzzle's pieces).
            float due = (checkpointEvery - sinceCheckpoint) * Pace;
            if (coins && (save || due < length - 1f))
            {
                float at = z + Math.Max(1f, Math.Min(length - 1f, save ? length * 0.5f : due));
                float saved = clock;
                clock += (at - z) / Pace;
                Add(PieceKind.Checkpoint, 0f, y, at);
                clock = saved;
                sinceCheckpoint = -(at - z) / Pace;
            }
            if (coins) Coins(z + 2f, z + length - 2f);
            else Coins(z + 1f, z + 2.5f);
            z += length;
            Tick(length / Pace);
        }

        private void Gap(float width, float dy)
        {
            string m = rideStage ? riding : "";
            width = Math.Min(width, MaxGap(m));
            dy = Math.Max(-3f, Math.Min(dy, MaxStep(m)));
            ArcCoins(z, z + width, Math.Max(1.2f, dy + 1.2f));
            z += width;
            y += dy;
            Tick(width / Pace + 0.6f);
        }

        private void Coins(float from, float to)
        {
            for (float c = from; c <= to; c += 4f)
                Add(PieceKind.Coin, (float)Math.Sin(c * 0.3f) * (HalfWidth - 2f), y + 0.8f, c);
        }

        private void ArcCoins(float from, float to, float height)
        {
            int n = Math.Max(2, (int)((to - from) / 1.5f));
            for (int i = 1; i < n; i++)
            {
                float f = (float)i / n;
                Add(PieceKind.Coin, 0f, y + 0.8f + (float)Math.Sin(f * Math.PI) * height, from + (to - from) * f);
            }
        }

        private void Enemies(float t, float from, float to)
        {
            int n = (int)Math.Floor(t * 2.2f + R(0.3f, 0.9f));
            for (int i = 0; i < n; i++)
            {
                float ez = from + (to - from) * (n == 1 ? 0.5f : (float)i / (n - 1));
                Enemy(Chance(0.15f + t * 0.5f), R(-1.5f, 1.5f), ez);
            }
        }

        private void Enemy(bool chaser, float x, float ez)
        {
            if (chaser) Add(PieceKind.Chaser, x, y, ez, 3f);
            else Add(PieceKind.Patrol, -HalfWidth + 1.2f, y, ez, HalfWidth * 2f - 2.4f);
            Tick(1f);
        }

        private void Crates(float at)
        {
            float x = R(-2.5f, 2.5f);
            Add(PieceKind.CrateBasic, x, y, at);
            Tick(0.6f);
            if (Chance(0.5f)) { Add(PieceKind.CrateBasic, x, y + 1f, at); Tick(0.4f); }
        }

        private void Landmarks(StageDef s)
        {
            var kinds = s.landmarks ?? Array.Empty<string>();
            if (kinds.Length == 0) return;
            float end = Math.Max(30f, Length - (s.boss ? 40f : 10f));
            // One every 45 m on a short stage, thinning out on the long ones so there are never more than 40.
            int count = Math.Max(kinds.Length, Math.Min(40, (int)(end / 45f)));
            for (int i = 0; i < count; i++)
            {
                float lz = 10f + (end - 10f) * (count == 1 ? 0.3f : (float)i / (count - 1));
                float side = i % 2 == 0 ? 1f : -1f;
                Add(PieceKind.Landmark, side, 0f, lz, 0f, 0f, kinds[i % kinds.Length]);
            }
        }

        private void Add(PieceKind k, float x, float py, float pz, float a = 0f, float b = 0f, string tag = null)
            => Pieces.Add(new Piece { kind = k, x = x, y = py, z = pz, sizeA = a, sizeB = b, tag = tag, time = clock });

        private static float Lerp(float a, float b, float t) => a + (b - a) * Math.Max(0f, Math.Min(1f, t));

        /// <summary>Height of the ground under z, or NaN over a gap.</summary>
        public float GroundAt(float pz)
        {
            foreach (var p in Pieces)
                if (p.kind == PieceKind.Ground && pz >= p.Start - 0.01f && pz <= p.End + 0.01f) return p.y;
            return float.NaN;
        }
    }
}
