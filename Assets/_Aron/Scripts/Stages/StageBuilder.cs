// (c) Ahmed Alamrani. All rights reserved.
using System.Collections.Generic;
using Aron.Config;
using Aron.Core;
using Aron.Dialogue;
using Aron.Enemies;
using Aron.Level;
using Aron.Logic.Regions;
using UnityEngine;

namespace Aron.Stages
{
    /// <summary>
    /// Puts a region stage together when the Stage scene loads: ground, gaps and steps from <see cref="StageLayout"/>,
    /// pick-ups and enemies copied from the templates the editor left in the scene, landmarks, the boss arena, and the
    /// stage's time of day, weather and type. Runs before the game manager so the level config is in place.
    /// </summary>
    [DefaultExecutionOrder(-200)]
    public class StageBuilder : MonoBehaviour
    {
        [Header("Templates (inactive scene objects)")]
        [SerializeField] private GameObject coin;
        [SerializeField] private GameObject crateBasic, crateBounce, crateLife;
        [SerializeField] private GameObject checkpoint, mover, slideBeam, hurdle, rideEnd, goal, talk;
        [SerializeField] private Enemy patrol, chaser;
        [SerializeField] private GameObject camelPost, horsePost;
        [SerializeField] private GameObject bossArena;
        [Tooltip("soldier, ninja, robe")]
        [SerializeField] private GameObject[] bossModels;
        [SerializeField] private GameObject[] kit;
        [SerializeField] private Material baseMaterial;
        [Tooltip("Unlit particle material for rain and sand")]
        [SerializeField] private Material particleMaterial;

        [Header("Scene")]
        [SerializeField] private Transform player;
        [SerializeField] private Light sun;
        [SerializeField] private Transform killZone, floor;

        public StageDef Stage { get; private set; }
        public RegionDef Region { get; private set; }
        public StageLayout Layout { get; private set; }
        public static StageBuilder Current { get; private set; }
        public Material ParticleMaterial => particleMaterial;
        public Transform World => world;
        public Transform Player => player;
        public Light Sun => sun;

        private StageArt art;
        private Transform world, into;
        private StageChunks chunks;
        private Transform[] built, scenery;
        private bool[] wasBuilt;
        private readonly Dictionary<int, PuzzleGate> gates = new Dictionary<int, PuzzleGate>();
        private GameManager gm;
        private Color ground, accent, sky;

        private void Awake()
        {
            Current = this;
            if (!StageSession.Active)
            {
                // Opened straight from the editor: play the first stage.
                var first = StageSession.Catalog.regions.Length > 0 ? StageSession.Catalog.regions[0].stages[0] : null;
                if (first == null) { Debug.LogError("[Stage] no region files in Resources/Regions"); return; }
                StageSession.Select(first.id);
            }
            Stage = StageSession.Stage;
            Region = StageSession.Region;
            Layout = StageLayout.Generate(Stage);
            var access = Save.SaveSystem.Data.access;
            if (Logic.Access.AssistRules.On(access))
                Logic.Access.AssistRules.AddCheckpoints(Layout, Logic.Access.AssistRules.CheckpointSeconds(access, StageLayout.CheckpointSeconds));
            art = new StageArt(baseMaterial, kit);
            ground = StageSession.Hex(Region.ground, new Color(0.85f, 0.72f, 0.54f));
            accent = StageSession.Hex(Region.accent, new Color(0.63f, 0.15f, 0.17f));
            sky = StageSession.Hex(Region.sky, new Color(0.62f, 0.78f, 0.91f));

            var level = ScriptableObject.CreateInstance<LevelConfig>();
            level.levelIndex = 0;
            level.levelNameAr = Stage.nameAr;
            level.musicTrack = StageSession.MusicFor(Stage, Region);
            level.bossMusicTrack = StageSession.BossMusicFor(Stage);
            gm = FindFirstObjectByType<GameManager>(FindObjectsInactive.Include);
            if (gm != null) { gm.SetLevel(level); gm.SetBoxTotal(Layout.BoxCount); }

            world = StageArt.Group("Stage_" + Stage.id, null, Vector3.zero);
            into = world;
            chunks = new StageChunks(Layout.Pieces, Layout.Length);
            built = new Transform[chunks.Count];
            wasBuilt = new bool[chunks.Count];
            StageConditions.Apply(this, art, Stage, sun, sky, ground);
            StageDressing.Dress(art, world, Layout, Stage.terrain, ground, accent);
            GatherScenery();
            Skyline();
            Stream(1f, 1f);

            float mid = Layout.Length * 0.5f;
            if (killZone != null) { killZone.position = new Vector3(0f, -4.5f, mid); killZone.localScale = new Vector3(1f, 1f, (Layout.Length + 80f) / 400f); }
            if (floor != null)
            {
                floor.position = new Vector3(0f, -6.5f, mid);
                floor.localScale = new Vector3(400f, 1f, Layout.Length + 120f);
                foreach (var r in floor.GetComponentsInChildren<Renderer>()) r.sharedMaterial = art.Mat(ground * 0.85f);
            }
            if (player != null) player.position = new Vector3(0f, 0.1f, 1f);
        }

        private void OnDestroy() { if (Current == this) Current = null; }

        // ------------------------------------------------------------------ streaming
        // Only the stretch around the player is standing: up to 150 m ahead is built, anything 90 m behind both him
        // and the last checkpoint is taken down. Scenery is built once and just switched on and off.
        private float nextStream;

        private void Update()
        {
            if (chunks == null || player == null || Time.time < nextStream) return;
            nextStream = Time.time + 0.25f;
            float anchor = gm != null ? gm.RespawnPoint.z : player.position.z;
            Stream(player.position.z, anchor);
        }

        private void Stream(float playerZ, float anchorZ)
        {
            for (int i = 0; i < chunks.Count; i++)
            {
                bool keep = StageChunks.Keep(i, playerZ, anchorZ);
                if (keep && built[i] == null) BuildChunk(i);
                else if (!keep && built[i] != null) TakeDown(i);
                if (scenery[i] != null)
                {
                    bool show = StageChunks.Show(i, playerZ);
                    if (scenery[i].gameObject.activeSelf != show) scenery[i].gameObject.SetActive(show);
                }
            }
        }

        private void BuildChunk(int i)
        {
            bool again = wasBuilt[i];
            built[i] = StageArt.Group("Chunk_" + i, world, Vector3.zero);
            into = built[i];
            foreach (int idx in chunks.PiecesIn(i))
            {
                var p = Layout.Pieces[idx];
                if (!again || StageChunks.Rebuilt(p.kind)) Build(p);
            }
            into = world;
            wasBuilt[i] = true;
        }

        private void TakeDown(int i)
        {
            var c = built[i];
            built[i] = null;
            // Anything that wandered in and must outlive the stretch: the player on a platform, a mount he rides.
            foreach (var pc in c.GetComponentsInChildren<Aron.Player.PlayerController>(true)) pc.transform.SetParent(null, true);
            foreach (var m in c.GetComponentsInChildren<Animals.MountAnimal>(true)) m.transform.SetParent(world, true);
            Destroy(c.gameObject);
        }

        // Lamps, props and the rest of the dressing, sorted into the same stretches.
        private void GatherScenery()
        {
            scenery = new Transform[chunks.Count];
            var loose = new List<Transform>();
            foreach (Transform t in world) loose.Add(t);
            foreach (var t in loose)
            {
                if (KeepsRunning(t)) continue;
                int i = chunks.ChunkOf(t.position.z);
                if (scenery[i] == null) scenery[i] = StageArt.Group("Scenery_" + i, world, Vector3.zero);
                t.SetParent(scenery[i], true);
            }
        }

        // Things that work along the whole stage and must stay switched on.
        private static bool KeepsRunning(Transform t)
            => t.name == "Roller" || t.name == "Ambush" || t.name == "EscapeStart" || t.GetComponentInChildren<ParticleSystem>(true) != null;

        private GameObject Spawn(GameObject tpl, Vector3 pos, string name = null)
        {
            if (tpl == null) return null;
            var go = Instantiate(tpl, into, false);
            go.name = name ?? tpl.name.Replace("Template", "");
            go.transform.localPosition = pos;
            go.SetActive(true);
            return go;
        }

        private void Build(Piece p)
        {
            var pos = new Vector3(p.x, p.y, p.z);
            switch (p.kind)
            {
                case PieceKind.Ground: Ground(p); break;
                case PieceKind.Coin: Spawn(coin, pos); break;
                case PieceKind.CrateBasic: Spawn(crateBasic, pos); break;
                case PieceKind.CrateBounce: Spawn(crateBounce, pos); break;
                case PieceKind.CrateLife: Spawn(crateLife, pos); break;
                case PieceKind.Checkpoint: Box(Crate.CrateType.Checkpoint, pos, "Checkpoint"); break;
                case PieceKind.CrateBisht: Box(Crate.CrateType.Bisht, pos, "BishtCrate"); break;
                case PieceKind.PuzzleGate:
                {
                    var g = StagePuzzles.Gate(art, into, Layout, p);
                    if (g != null) gates[(int)p.sizeB] = g;
                    break;
                }
                case PieceKind.PuzzlePart:
                    if (gates.TryGetValue((int)p.sizeB, out var gate) && gate != null) StagePuzzles.Part(art, into, p, gate);
                    break;
                case PieceKind.Patrol: Enemy(patrol, pos, new Vector3(p.sizeA, 0f, 0f)); break;
                case PieceKind.Chaser: Enemy(chaser, pos, new Vector3(0f, 0f, -p.sizeA)); break;
                case PieceKind.Mover:
                {
                    var m = Spawn(mover, pos);
                    var mp = m != null ? m.GetComponent<MovingPlatform>() : null;
                    if (mp != null) mp.Setup(new Vector3(0f, 0f, p.sizeA), 3f + p.sizeA * 0.35f);
                    break;
                }
                case PieceKind.SlideBeam:
                {
                    var b = Spawn(slideBeam, pos);
                    if (b != null) b.transform.localScale = new Vector3(Layout.HalfWidth / 4f, 1f, 1f);
                    break;
                }
                case PieceKind.Hurdle: Widen(Spawn(hurdle, pos)); break;
                case PieceKind.RideEnd: Widen(Spawn(rideEnd, pos)); break;
                case PieceKind.MountPost: Spawn(p.tag == "horse" ? horsePost : camelPost, pos); break;
                case PieceKind.Goal:
                {
                    var g = Spawn(goal, pos);
                    var lg = g != null ? g.GetComponentInChildren<LevelGoal>() : null;
                    if (lg != null) lg.SetRequiresBoss(false);
                    break;
                }
                case PieceKind.Talk: Talk(pos); break;
                case PieceKind.Landmark: Landmark(p); break;
                case PieceKind.BossArena: Arena(pos); break;
            }
        }

        // A plain crate dressed as a lantern box or a golden bisht box.
        private void Box(Crate.CrateType type, Vector3 pos, string name)
        {
            var go = Spawn(crateBasic, pos, name);
            var c = go != null ? go.GetComponent<Crate>() : null;
            if (c != null) c.Setup(type, 0);
        }

        private void Widen(GameObject go)
        {
            if (go != null) go.transform.localScale = new Vector3(Layout.HalfWidth / 5f, 1f, 1f);
        }

        /// <summary>Drops a patrol enemy into the stage (action waves use this).</summary>
        public Enemy SpawnPatrol(Vector3 pos, float sweep) => Enemy(patrol, pos, new Vector3(sweep, 0f, 0f));

        private Enemy Enemy(Enemy tpl, Vector3 pos, Vector3 patrolOffset)
        {
            if (tpl == null) return null;
            var e = Instantiate(tpl, into, false);
            e.name = tpl.name.Replace("Template", "");
            e.transform.localPosition = pos;
            e.SetPatrol(patrolOffset);
            e.gameObject.SetActive(true);
            return e;
        }

        private void Talk(Vector3 pos)
        {
            var go = Spawn(talk, pos, "Talk_Intro");
            if (go == null || string.IsNullOrEmpty(Stage.lineAr)) return;
            var seq = ScriptableObject.CreateInstance<DialogueSequence>();
            seq.lines = new[] { new DialogueLine { speakerId = "player", textAr = Stage.lineAr, duration = 2.8f } };
            var dt = go.GetComponent<DialogueTrigger>();
            if (dt != null) dt.SetSequence(seq);
        }

        // ------------------------------------------------------------------ ground
        private void Ground(Piece p)
        {
            float hw = p.sizeA, len = p.sizeB;
            var seg = StageArt.Group("Ground", into, new Vector3(0f, p.y, p.z));
            var col = seg.gameObject.AddComponent<BoxCollider>();
            col.center = new Vector3(0f, -0.5f, 0f);
            col.size = new Vector3(hw * 2f, 1f, len);

            Color top = Surface(Stage.terrain);
            art.Box(seg, new Vector3(0f, -0.15f, 0f), new Vector3(hw * 2f, 0.3f, len), top);
            // Paving joints or ripples so a long run doesn't read as one flat slab.
            for (float z = -len * 0.5f + 1f; z < len * 0.5f; z += 4f)
                art.Box(seg, new Vector3(0f, 0.005f, z), new Vector3(hw * 2f, 0.01f, 0.08f), top * 0.88f);
            art.Box(seg, new Vector3(0f, -3f, 0f), new Vector3(hw * 2f - 0.2f, 5.4f, len - 0.2f), ground * 0.7f);

            // Edge walls keep the run on the track. Water stages get a low quay instead of mud walls.
            bool wet = Stage.terrain == "coast" || Stage.terrain == "river";
            Color wall = wet ? new Color(0.8f, 0.78f, 0.72f) : Stage.terrain == "forest" ? new Color(0.36f, 0.27f, 0.18f) : ground * 0.92f;
            foreach (float side in new[] { -1f, 1f })
            {
                var w = StageArt.Group("EdgeWall", seg, new Vector3(side * (hw + 0.35f), 1.2f, 0f));
                w.gameObject.AddComponent<BoxCollider>().size = new Vector3(0.7f, 3.4f, len);
                art.Box(seg, new Vector3(side * (hw + 0.35f), wet ? 0.2f : 0.4f, 0f), new Vector3(0.7f, wet ? 0.4f : 0.8f, len), wall);
                // Shoulder: no collider, sits just under the track so it frames the walk without catching the player.
                float sh = Stage.terrain == "desert" || Stage.terrain == "oasis" ? 14f : 10f;
                var shoulderPos = new Vector3(side * (hw + 0.7f + sh * 0.5f), -0.3f - 0.5f, 0f);
                var shoulderSize = new Vector3(sh, 1f, len + 0.5f);
                if (wet) art.Water(seg, shoulderPos, shoulderSize, new Color(0.18f, 0.45f, 0.62f));
                else art.Box(seg, shoulderPos, shoulderSize, ground * 0.95f);
            }
        }

        private Color Surface(string terrain)
        {
            switch (terrain)
            {
                case "street": return new Color(0.78f, 0.7f, 0.6f);
                case "souq": return new Color(0.72f, 0.58f, 0.42f);
                case "desert": return ground;
                case "coast": return new Color(0.92f, 0.86f, 0.7f);
                case "mountain": return new Color(0.55f, 0.5f, 0.45f);
                case "oasis": return new Color(0.55f, 0.66f, 0.36f);
                case "forest": return new Color(0.36f, 0.5f, 0.26f);
                case "ruins": return new Color(0.74f, 0.66f, 0.54f);
                case "river": return new Color(0.62f, 0.66f, 0.5f);
                default: return ground;
            }
        }

        // ------------------------------------------------------------------ landmarks
        private void Landmark(Piece p)
        {
            float side = p.x >= 0f ? 1f : -1f;
            float gy = Layout.GroundAt(p.z);
            if (float.IsNaN(gy)) gy = 0f;
            var root = StageArt.Group("Landmark_" + p.tag, into, new Vector3(side * (Layout.HalfWidth + 9f), Mathf.Min(gy, 0f) - 0.3f, p.z), side > 0f ? -90f : 90f);
            art.Box(root, new Vector3(0f, -0.4f, 0f), new Vector3(12f, 0.8f, 12f), ground * 0.9f);
            float height = LandmarkFactory.Build(art, root, p.tag, accent, ground);
            var lm = root.gameObject.AddComponent<Landmark>();
            lm.Setup(Mathf.Clamp(height * 3f, 18f, 45f), height * 0.6f);
        }

        // Holy places and houses of worship are only ever a far-off silhouette: big, behind the hills, out of reach,
        // no enemies or crates near them and nothing the camera stops to look at.
        private void Skyline()
        {
            if (Region == null || Region.skyline == null) return;
            for (int i = 0; i < Region.skyline.Length; i++)
            {
                float side = i % 2 == 0 ? 1f : -1f;
                var root = StageArt.Group("Skyline_" + Region.skyline[i], world, new Vector3(side * 140f, -2f, Layout.Length * (Region.skyline.Length <= 3 ? 0.35f + 0.3f * i : 0.2f + 0.7f * i / (Region.skyline.Length - 1))), side > 0f ? -90f : 90f);
                root.localScale = Vector3.one * 3f;
                LandmarkFactory.Build(art, root, Region.skyline[i], accent, ground);
                foreach (var c in root.GetComponentsInChildren<Collider>()) Destroy(c);
            }
        }

        // ------------------------------------------------------------------ boss
        private void Arena(Vector3 pos)
        {
            if (bossArena == null) return;
            var a = Instantiate(bossArena, into, false);
            a.name = "BossArena";
            a.transform.localPosition = pos;
            var boss = a.GetComponentInChildren<RiyadhBoss>(true);
            if (boss != null && Region.boss != null) DressBoss(boss, Region.boss);
            var talkBoss = a.transform.Find("Talk_Boss");
            if (talkBoss != null && Region.boss != null && !string.IsNullOrEmpty(Region.boss.introAr))
            {
                var seq = ScriptableObject.CreateInstance<DialogueSequence>();
                seq.lines = new[] { new DialogueLine { speakerId = "boss", textAr = Region.boss.introAr, duration = 3f } };
                var dt = talkBoss.GetComponent<DialogueTrigger>();
                if (dt != null) dt.SetSequence(seq);
            }
            a.SetActive(true);
        }

        private void DressBoss(RiyadhBoss boss, BossDef def)
        {
            var t = boss.transform;
            boss.VoiceModel = def.model;
            Color body = StageSession.Hex(def.body, Color.gray), trim = StageSession.Hex(def.trim, Color.yellow);
            var old = t.Find("Model");
            GameObject model = old != null ? old.gameObject : null;
            int fighter = RegionCatalog.FighterBody(def.model);
            GameObject fresh = null;
            if (fighter < 0)
                fresh = BossBodies.Build(def.model, def.headgear, body, trim, StageSession.Hex(def.projectile, trim), baseMaterial);
            else if (bossModels != null && fighter > 0 && fighter < bossModels.Length && bossModels[fighter] != null)
                fresh = Instantiate(bossModels[fighter]);
            if (fresh != null)
            {
                fresh.name = "Model";
                fresh.transform.SetParent(t, false);
                fresh.transform.localPosition = new Vector3(0f, -1.6f, 0f);
                fresh.transform.localRotation = Quaternion.identity;
                fresh.SetActive(true);
                if (old != null) { old.gameObject.SetActive(false); Destroy(old.gameObject); }
                model = fresh;
            }
            if (model != null && fighter >= 0)
            {
                art.Tint(model, body, "Main", true);
                art.Tint(model, body, "Clothes", true);
                art.Tint(model, trim, "Helmet", true);
                art.Tint(model, trim, "Band", true);
                if (def.model == "mummy") art.Tint(model, Color.Lerp(body, Color.white, 0.2f), "Skin", true);
                if (def.model != "mummy") BossHeadgear(model.transform, def.headgear, body, trim);
                BossBodies.Dress(def.model, model.transform, body, trim, baseMaterial);
            }
            float s = Mathf.Clamp(def.scale, 0.8f, 1.8f);
            t.localScale = Vector3.one * s;
            t.localPosition = new Vector3(t.localPosition.x, 1.6f * s, t.localPosition.z);
            boss.Configure(ToConfig(def), model != null ? model.GetComponentInChildren<Animator>() : null);
        }

        private void BossHeadgear(Transform model, string kind, Color body, Color trim)
        {
            Transform head = null;
            foreach (var tr in model.GetComponentsInChildren<Transform>(true))
                if (tr.name.IndexOf("Head", System.StringComparison.OrdinalIgnoreCase) >= 0 && tr.name.IndexOf("Top", System.StringComparison.OrdinalIgnoreCase) < 0) { head = tr; break; }
            if (head == null || kind == "none" || kind == "helmet") return;
            var g = StageArt.Group("Headgear", head, new Vector3(0f, 0.25f, 0f));
            switch (kind)
            {
                case "crown":
                    art.Frustum(g, Vector3.zero, 8, 0.2f, 0.24f, 0.16f, trim);
                    break;
                case "turban":
                    art.Ball(g, new Vector3(0f, 0.05f, 0f), new Vector3(0.42f, 0.24f, 0.42f), Color.Lerp(trim, Color.white, 0.6f));
                    break;
                case "ghutra":
                    art.Box(g, new Vector3(0f, 0f, -0.05f), new Vector3(0.42f, 0.12f, 0.46f), Color.white);
                    art.Cyl(g, new Vector3(0f, 0.04f, 0f), 0.2f, 0.05f, Color.black);
                    break;
                case "fez":
                    art.Frustum(g, Vector3.zero, 12, 0.15f, 0.12f, 0.2f, new Color(0.7f, 0.1f, 0.1f));
                    break;
                case "hood":
                    art.Ball(g, new Vector3(0f, -0.05f, -0.04f), new Vector3(0.46f, 0.4f, 0.5f), body * 0.8f);
                    break;
            }
        }

        private static BossConfig ToConfig(BossDef def)
        {
            var c = ScriptableObject.CreateInstance<BossConfig>();
            c.bossNameAr = def.nameAr;
            c.stunLineAr = string.IsNullOrEmpty(def.stunAr) ? c.stunLineAr : def.stunAr;
            c.defeatLineAr = def.defeatAr;
            c.projectileColor = StageSession.Hex(def.projectile, c.projectileColor);
            c.maxHealth = def.health;
            var phases = new List<BossPhase>();
            foreach (var p in def.phases)
                phases.Add(new BossPhase
                {
                    healthThreshold = p.threshold, moveSpeed = p.move, chargeSpeed = p.charge, attackInterval = p.interval,
                    useShockwave = System.Array.IndexOf(p.attacks, "wave") >= 0,
                    summonMinions = System.Array.IndexOf(p.attacks, "summon") >= 0,
                    tauntLineAr = p.tauntAr, attacks = p.attacks,
                });
            c.phases = phases.ToArray();
            return c;
        }
    }
}
