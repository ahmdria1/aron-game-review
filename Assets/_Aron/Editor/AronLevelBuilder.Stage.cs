// (c) Ahmed Alamrani. All rights reserved.
using Aron.Animals;
using Aron.Core;
using Aron.Enemies;
using Aron.Level;
using Aron.Stages;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Aron.EditorTools
{
    /// <summary>
    /// The one scene every region stage plays in. It holds the player rig and inactive templates; StageBuilder lays
    /// out the stage picked on the world map when the scene loads.
    /// </summary>
    public static partial class AronLevelBuilder
    {
        private const string StagePath = SceneDir + "/" + SceneNames.Stage + ".unity";

        private static void BuildStageScene(Configs cfg)
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var sun = SetupSkyAndSun();
            var rig = BuildRig(cfg, cfg.level);
            var templates = Group("_Templates").transform;

            var coin = MakeCoin(templates, Vector3.zero);
            coin.name = "CoinTemplate";
            var basic = CrateTemplate(templates, Crate.CrateType.Basic);
            var bounce = CrateTemplate(templates, Crate.CrateType.Bounce);
            var life = CrateTemplate(templates, Crate.CrateType.Life);

            MakeCheckpoint(templates, Vector3.zero);
            // Checkpoint pieces sit on the track centre; the flag stands at the side like in Riyadh.
            var checkpoint = templates.GetChild(templates.childCount - 1).gameObject;
            var checkpointRoot = Group("CheckpointTemplate", templates);
            checkpoint.transform.SetParent(checkpointRoot.transform, false);
            checkpoint.transform.localPosition = new Vector3(3.2f, 0f, 0f);

            var patrol = MakeEnemy(templates, "PatrolTemplate", Vector3.zero, cfg.patrol, cfg.guardModel, new Vector3(6f, 0f, 0f), coin);
            var chaser = MakeEnemy(templates, "ChaserTemplate", Vector3.zero, cfg.chaser, cfg.chaserModel, new Vector3(0f, 0f, -4f), coin);
            var minion = MakeEnemy(templates, "MinionTemplate", Vector3.zero, cfg.minion, cfg.chaserModel, Vector3.zero, coin);

            var mover = Group("MoverTemplate", templates);
            var moverCol = mover.AddComponent<BoxCollider>();
            moverCol.size = new Vector3(4f, 0.5f, 4f);
            moverCol.center = new Vector3(0f, -0.25f, 0f);
            Kinematic(mover);
            Trigger(mover, new Vector3(3.6f, 1.6f, 3.6f), new Vector3(0f, 1f, 0f));
            AronArt.Place(AronArt.Platformer + "block-grass-low-large.fbx", mover.transform, new Vector3(0f, -0.5f, 0f), 0f, new Vector3(4f / 2.08f, 1f, 4f / 2.08f));
            mover.AddComponent<MovingPlatform>();

            // Beam is 8 m wide at scale 1; StageBuilder stretches it to the track.
            var beam = Group("SlideBeamTemplate", templates);
            var beamBody = Group("Beam", beam.transform, new Vector3(0f, 1.6f, 0f));
            beamBody.AddComponent<BoxCollider>().size = new Vector3(8f, 1.2f, 1f);
            var beamArt = AronArt.Place(AronArt.Castle + "wall.fbx", beamBody.transform, new Vector3(0f, -0.6f, 0f), 0f, new Vector3(8.4f, 1.2f / 1.31f, 1f));
            AronArt.Tint(beamArt, MudDark);
            foreach (float x in new[] { -4.3f, 4.3f })
                AronArt.Tint(AronArt.Place(AronArt.Castle + "wall-pillar.fbx", beam.transform, new Vector3(x, 0f, 0f), 0f, new Vector3(0.7f, 2.3f / 1.31f, 0.7f), "Post"), MudDark);

            var fence = Mat("HurdleWood", new Color(0.92f, 0.9f, 0.85f));
            var hurdle = Group("HurdleTemplate", templates);
            var hb = hurdle.AddComponent<BoxCollider>();
            hb.center = new Vector3(0f, 0.4f, 0f); hb.size = new Vector3(10f, 0.8f, 0.3f);
            Block("Rail", hurdle.transform, new Vector3(0f, 0.7f, 0f), new Vector3(10f, 0.12f, 0.12f), fence, PrimitiveType.Cube, false);
            Block("Rail", hurdle.transform, new Vector3(0f, 0.35f, 0f), new Vector3(10f, 0.1f, 0.1f), Mat("HurdleRed", new Color(0.75f, 0.12f, 0.12f)), PrimitiveType.Cube, false);

            var wood = Mat("PostWood", new Color(0.42f, 0.27f, 0.15f));
            var rideEnd = Group("RideEndTemplate", templates);
            Trigger(rideEnd, new Vector3(10f, 4f, 1.5f), new Vector3(0f, 2f, 0f));
            rideEnd.AddComponent<RideZoneEnd>();
            foreach (float x in new[] { -4f, 4f })
                Block("Post", rideEnd.transform, new Vector3(x, 0.9f, 0f), new Vector3(0.25f, 0.9f, 0.25f), wood, PrimitiveType.Cylinder, false);
            Block("Bar", rideEnd.transform, new Vector3(0f, 1.6f, 0f), new Vector3(8.2f, 0.12f, 0.12f), wood, PrimitiveType.Cube, false);

            MountPost(templates, cfg.camelModel, Vector3.zero, "CamelPostTemplate");
            var camelPost = templates.GetChild(templates.childCount - 1).gameObject;
            MountPost(templates, cfg.horseModel, Vector3.zero, "HorsePostTemplate");
            var horsePost = templates.GetChild(templates.childCount - 1).gameObject;

            var goal = Group("GoalTemplate", templates);
            Trigger(goal, new Vector3(4f, 3f, 2f), new Vector3(0f, 1.5f, 0f));
            var arch = AronArt.Place(AronArt.Castle + "tower-square-arch.fbx", goal.transform, Vector3.zero, 0f, new Vector3(4f, 4f, 2f));
            AronArt.Tint(arch, new Color(1f, 0.85f, 0.35f), null, "Gold");
            var lg = goal.AddComponent<LevelGoal>();
            Set(lg, "requiresBossDefeat", false);

            var talk = MakeDialogueTrigger(templates, "TalkTemplate", Vector3.zero, null);

            // Boss arena: the Riyadh one, kept whole as a template. Three bodies to pick from.
            var mWave = Mat("Shockwave", new Color(1f, 0.6f, 0.1f));
            var waveTpl = Block("ShockwaveTemplate", templates, Vector3.zero, new Vector3(1f, 0.05f, 1f), mWave, PrimitiveType.Cylinder, false);
            waveTpl.AddComponent<BossShockwave>();
            BuildBossArena(templates, cfg, waveTpl, minion, 0f);
            var arena = templates.Find("BossArena").gameObject;
            arena.name = "BossArenaTemplate";
            var ninja = AronArt.BuildCharacterPrefab(new AronArt.CharacterLook
            {
                prefabName = "Boss_Ninja", fbx = AronArt.Characters + "Ninja_Sand.fbx", height = 3.4f,
                id = "boss", faceForward = 0.06f, browColor = new Color(0.05f, 0.04f, 0.03f),
            }, out _);
            var robe = AronArt.BuildCharacterPrefab(new AronArt.CharacterLook
            {
                prefabName = "Boss_Robe", fbx = AronArt.Characters + "Kimono_Male.fbx", height = 3.4f,
                id = "boss", faceForward = 0.06f, thobe = true, thobeColor = new Color(0.82f, 0.74f, 0.6f), browColor = new Color(0.05f, 0.04f, 0.03f),
            }, out _);

            var fxSystems = rig.systems;
            BuildFx(fxSystems, templates);

            var world = Group("World");
            var kill = Group("KillZone", world.transform, new Vector3(0f, -4.5f, 100f));
            Trigger(kill, new Vector3(400f, 2f, 400f));
            kill.AddComponent<KillZone>();
            var floor = Group("Floor", world.transform, new Vector3(0f, -6.5f, 100f));
            Block("FloorMesh", floor.transform, Vector3.zero, Vector3.one, Mat("Sand", new Color(0.86f, 0.72f, 0.5f)), PrimitiveType.Cube, false);

            var particle = AronArt.SaveMaterial("WeatherParticle",
                Shader.Find("Universal Render Pipeline/Particles/Unlit") ?? Shader.Find("Particles/Standard Unlit") ?? Shader.Find("Sprites/Default"),
                Color.white);

            foreach (Transform child in templates) child.gameObject.SetActive(false);

            var builder = Group("_StageBuilder").AddComponent<StageBuilder>();
            Set(builder, "coin", coin);
            Set(builder, "crateBasic", basic);
            Set(builder, "crateBounce", bounce);
            Set(builder, "crateLife", life);
            Set(builder, "checkpoint", checkpointRoot);
            Set(builder, "mover", mover);
            Set(builder, "slideBeam", beam);
            Set(builder, "hurdle", hurdle);
            Set(builder, "rideEnd", rideEnd);
            Set(builder, "goal", goal);
            Set(builder, "talk", talk);
            Set(builder, "patrol", patrol);
            Set(builder, "chaser", chaser);
            Set(builder, "camelPost", camelPost);
            Set(builder, "horsePost", horsePost);
            Set(builder, "bossArena", arena);
            SetArray(builder, "bossModels", new Object[] { cfg.bossModel, ninja, robe });
            Set(builder, "baseMaterial", Mat("StageBase", Color.white));
            Set(builder, "particleMaterial", particle);
            Set(builder, "player", rig.player.transform);
            Set(builder, "sun", sun);
            Set(builder, "killZone", kill.transform);
            Set(builder, "floor", floor.transform);
            if (cfg.falconModel != null) PlaceCharacter(cfg.falconModel, null, rig.player.transform.position + new Vector3(0.6f, 2f, -0.3f), 0f, "Falcon");
            if (cfg.oryxModel != null) PlaceCharacter(cfg.oryxModel, null, rig.player.transform.position + new Vector3(-1.4f, 0f, -2f), 0f, "Oryx");

            EditorSceneManager.SaveScene(scene, StagePath);
        }

        private static GameObject CrateTemplate(Transform parent, Crate.CrateType type)
        {
            MakeCrate(parent, Vector3.zero, type);
            var go = parent.GetChild(parent.childCount - 1).gameObject;
            go.name = "Crate" + type + "Template";
            return go;
        }
    }
}
