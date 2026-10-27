// (c) Ahmed Alamrani. All rights reserved.
using System.Collections.Generic;
using Aron.Animals;
using Aron.Audio;
using Aron.Characters;
using Aron.Config;
using Aron.Core;
using Aron.Dialogue;
using Aron.Enemies;
using Aron.Fx;
using Aron.Input;
using Aron.Level;
using Aron.Logic;
using Aron.Player;
using Aron.RemoteConfig;
using Aron.UI;
using Aron.Voice;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

namespace Aron.EditorTools
{
    /// <summary>
    /// Generates config assets, character prefabs, MainMenu and Level01_Riyadh, and registers the scenes in
    /// Build Settings. Menu: Aron > 2. Build Everything. Re-running overwrites the generated scenes and prefabs.
    /// Art comes from Assets/_Aron/Art (Kenney and Quaternius packs, CC0); gameplay colliders are plain boxes
    /// so the visuals can be swapped without touching the level logic.
    /// </summary>
    public static partial class AronLevelBuilder
    {
        private const string Root = "Assets/_Aron";
        private const string ConfigDir = Root + "/Resources/Config";
        private const string SceneDir = Root + "/Scenes";
        private const string MatDir = Root + "/Materials";
        private const string MenuPath = SceneDir + "/" + SceneNames.MainMenu + ".unity";
        private const string RiyadhPath = SceneDir + "/" + SceneNames.Riyadh + ".unity";

        private static readonly Dictionary<string, Material> Mats = new Dictionary<string, Material>();

        // ------------------------------------------------------------------ entry points
        [MenuItem("Aron/2. Build Everything (Configs + Scenes)", priority = 2)]
        public static void BuildAll()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            MobileSetup.Apply();
            Prepare();
            var cfg = BuildConfigs();
            BuildMenuScene(cfg);
            BuildRiyadhScene(cfg);
            BuildStageScene(cfg);
            EditorBuildSettings.scenes = new[]
            {
                new EditorBuildSettingsScene(MenuPath, true),
                new EditorBuildSettingsScene(RiyadhPath, true),
                new EditorBuildSettingsScene(StagePath, true),
            };
            AssetDatabase.SaveAssets();
            EditorSceneManager.OpenScene(MenuPath);
            Debug.Log("[Aron] Built configs, MainMenu, Level01_Riyadh and Stage. Press Play.");
        }

        [MenuItem("Aron/3. Rebuild Riyadh Level Only", priority = 3)]
        public static void BuildRiyadhOnly()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            Prepare();
            BuildRiyadhScene(BuildConfigs());
            AssetDatabase.SaveAssets();
        }

        private static void Prepare()
        {
            EnsureFolders();
            Mats.Clear();
            AronArt.Reset();
            AronArt.ConfigureImporters();
        }

        private static void EnsureFolders()
        {
            foreach (var f in new[] { "Resources", "Resources/Config", "Scenes", "Materials" })
            {
                string full = Root + "/" + f;
                if (AssetDatabase.IsValidFolder(full)) continue;
                int slash = full.LastIndexOf('/');
                AssetDatabase.CreateFolder(full.Substring(0, slash), full.Substring(slash + 1));
            }
        }

        // ------------------------------------------------------------------ configs
        private class Configs
        {
            public GameObject aronModel, nouraModel, guardModel, chaserModel, bossModel, shopkeeperModel;
            public GameObject camelModel, horseModel, falconModel, oryxModel;
            public PlayerConfig player;
            public CharacterRoster roster;
            public EnemyConfig patrol, chaser, minion;
            public BossConfig boss;
            public LevelConfig level;
            public DialogueSequence intro, masmak, kingdom, bossIntro, desert, horseTrack;
        }

        private static T Asset<T>(string name) where T : ScriptableObject
        {
            string path = ConfigDir + "/" + name + ".asset";
            var a = AssetDatabase.LoadAssetAtPath<T>(path);
            if (a == null) { a = ScriptableObject.CreateInstance<T>(); AssetDatabase.CreateAsset(a, path); }
            return a;
        }

        private static Configs BuildConfigs()
        {
            var c = new Configs();
            c.player = Asset<PlayerConfig>("PlayerConfig");

            var agal = Asset<WeaponDefinition>("Weapon_Agal");
            agal.weaponId = "agal"; agal.displayNameAr = "العقال"; agal.hitboxRadius = 1.1f; agal.hitboxOffset = new Vector3(0f, 1f, 1.2f);
            agal.animatorTrigger = "AttackAgal"; agal.sfxId = "swing";
            var bag = Asset<WeaponDefinition>("Weapon_Handbag");
            bag.weaponId = "handbag"; bag.displayNameAr = "الشنطة"; bag.hitboxRadius = 1.0f; bag.hitboxOffset = new Vector3(0f, 1f, 1.0f);
            bag.activeStart = 0.1f; bag.activeEnd = 0.3f; bag.totalDuration = 0.45f; bag.animatorTrigger = "AttackHandbag"; bag.sfxId = "swing";
            EditorUtility.SetDirty(agal); EditorUtility.SetDirty(bag);

            var aron = Asset<CharacterDefinition>("Character_Aron");
            aron.characterId = "aron"; aron.displayNameAr = "أرون"; aron.weapon = agal;
            aron.greyboxColor = new Color(0.97f, 0.97f, 0.95f); aron.accessoryColor = new Color(0.05f, 0.05f, 0.05f);
            var noura = Asset<CharacterDefinition>("Character_Noura");
            noura.characterId = "noura"; noura.displayNameAr = "نورة"; noura.weapon = bag;
            noura.greyboxColor = new Color(0.08f, 0.08f, 0.1f); noura.accessoryColor = new Color(0.48f, 0.29f, 0.16f);
            c.aronModel = AronArt.BuildCharacterPrefab(new AronArt.CharacterLook
            {
                prefabName = "Character_Aron", fbx = AronArt.Characters + "Kimono_Male.fbx", height = 1.8f,
                tints = new[] { ("Clothes", new Color(0.97f, 0.97f, 0.95f)), ("Band", new Color(0.92f, 0.92f, 0.9f)) },
                shemagh = true, weapon = "agal", weaponColor = new Color(0.05f, 0.05f, 0.05f),
                id = "aron", thobe = true, beard = true, browColor = new Color(0.05f, 0.04f, 0.04f),
            }, out var aronCtrl);
            c.nouraModel = AronArt.BuildCharacterPrefab(new AronArt.CharacterLook
            {
                prefabName = "Character_Noura", fbx = AronArt.Characters + "Kimono_Female.fbx", height = 1.7f,
                tints = new[] { ("Clothes", new Color(0.08f, 0.08f, 0.1f)), ("Band", new Color(0.61f, 0.42f, 0.19f)), ("Hair", new Color(0.06f, 0.05f, 0.05f)) },
                weapon = "handbag", weaponColor = new Color(0.48f, 0.29f, 0.16f), hijab = true,
                id = "noura", browColor = new Color(0.06f, 0.05f, 0.05f),
            }, out var nouraCtrl);
            aron.modelPrefab = c.aronModel; aron.animatorController = aronCtrl;
            noura.modelPrefab = c.nouraModel; noura.animatorController = nouraCtrl;
            EditorUtility.SetDirty(aron); EditorUtility.SetDirty(noura);

            c.guardModel = AronArt.BuildCharacterPrefab(new AronArt.CharacterLook
            {
                prefabName = "Enemy_Guard", fbx = AronArt.Characters + "Ninja_Sand.fbx", height = 1.6f,
                mouth = false, faceForward = 0.03f,
            }, out _);
            c.chaserModel = AronArt.BuildCharacterPrefab(new AronArt.CharacterLook
            {
                prefabName = "Enemy_Chaser", fbx = AronArt.Characters + "Ninja_Sand.fbx", height = 1.7f,
                tints = new[] { ("Main", new Color(0.55f, 0.12f, 0.35f)) },
                mouth = false, faceForward = 0.03f,
            }, out _);
            c.bossModel = AronArt.BuildCharacterPrefab(new AronArt.CharacterLook
            {
                prefabName = "Boss_Guardian", fbx = AronArt.Characters + "BlueSoldier_Male.fbx", height = 3.4f,
                tints = new[] { ("Main", new Color(0.3f, 0.32f, 0.25f)), ("Helmet", new Color(0.85f, 0.7f, 0.25f)) },
                id = "boss", faceForward = 0.06f, browColor = new Color(0.05f, 0.04f, 0.03f),
            }, out _);
            c.shopkeeperModel = AronArt.BuildCharacterPrefab(new AronArt.CharacterLook
            {
                prefabName = "Npc_Shopkeeper", fbx = AronArt.Characters + "Kimono_Male.fbx", height = 1.75f,
                tints = new[] { ("Clothes", new Color(0.55f, 0.4f, 0.25f)), ("Band", new Color(0.2f, 0.15f, 0.1f)) },
                shemagh = true, shemaghColor = new Color(0.95f, 0.95f, 0.95f),
                id = "shopkeeper", thobe = true, thobeColor = new Color(0.82f, 0.74f, 0.6f), browColor = new Color(0.5f, 0.5f, 0.5f),
            }, out _);

            c.camelModel = AronArt.BuildCamelPrefab();
            c.horseModel = AronArt.BuildHorsePrefab();
            c.falconModel = AronArt.BuildFalconPrefab();
            c.oryxModel = AronArt.BuildOryxPrefab();

            c.roster = Asset<CharacterRoster>("CharacterRoster");
            c.roster.characters = new[] { aron, noura };
            EditorUtility.SetDirty(c.roster);

            c.patrol = Asset<EnemyConfig>("Enemy_Patrol"); c.patrol.canChase = false; c.patrol.patrolSpeed = 2f;
            c.chaser = Asset<EnemyConfig>("Enemy_Chaser"); c.chaser.canChase = true; c.chaser.chaseSpeed = 4.2f; c.chaser.health = 2;
            c.minion = Asset<EnemyConfig>("Enemy_BossMinion"); c.minion.canChase = true; c.minion.chaseSpeed = 3.5f; c.minion.detectRadius = 20f; c.minion.loseRadius = 30f;
            EditorUtility.SetDirty(c.patrol); EditorUtility.SetDirty(c.chaser); EditorUtility.SetDirty(c.minion);

            c.boss = Asset<BossConfig>("Boss_Riyadh");
            c.boss.bossNameAr = "د. دحيم";
            c.level = Asset<LevelConfig>("Level_01_Riyadh");
            c.level.levelIndex = 1; c.level.levelNameAr = "الرياض"; c.level.musicTrack = Logic.MusicCues.SaudiNajd; c.level.bossMusicTrack = Logic.MusicCues.StageBoss;
            EditorUtility.SetDirty(c.level);

            c.intro = Dialogue("Dialogue_Intro",
                L("player", "يا هلا بالرياض! خلني أجمع العملات.", Emotion.Joy),
                L("guide", "انتبه يا أرون، الحرس منتشرين بالدرعية!"),
                L("player", "الله يستر...", Emotion.Worry));
            c.masmak = Dialogue("Dialogue_Masmak",
                L("player", "قصر المصمك! هنا بدأت الحكاية.", Emotion.Admiration),
                L("guide", "اكسر الصناديق وخذ العملات."));
            c.kingdom = Dialogue("Dialogue_Kingdom",
                L("player", "برج المملكة قدامي... الطريق للزعيم قريب.", Emotion.Admiration),
                L("shopkeeper", "تفضل يا الغالي، القهوة على حسابنا!", Emotion.Joy));
            c.desert = Dialogue("Dialogue_Desert",
                L("player", "نفود الثمامة! وش هالذلول الحلوة؟", Emotion.Joy),
                L("guide", "اركبها وتنط أعلى. ولو صدمك أحد تطيح بس ما تخسر."));
            c.horseTrack = Dialogue("Dialogue_Horse",
                L("guide", "ميدان الفروسية! الحصان أسرع، نط الحواجز."),
                L("player", "يلا يا أصيل!", Emotion.Joy));
            c.bossIntro = Dialogue("Dialogue_Boss",
                L("boss", "وقف عندك! ما أحد يعدي من هنا.", Emotion.Anger),
                L("player", "بنشوف يا حارس!", Emotion.Anger));
            return c;
        }

        private static DialogueLine L(string speaker, string text, Emotion emotion = Emotion.Neutral)
            => new DialogueLine { speakerId = speaker, textAr = text, duration = 2.6f, delayBefore = 0.2f, emotion = emotion };

        private static DialogueSequence Dialogue(string name, params DialogueLine[] lines)
        {
            var d = Asset<DialogueSequence>(name);
            d.lines = lines;
            EditorUtility.SetDirty(d);
            return d;
        }

        // ------------------------------------------------------------------ helpers
        private static bool UsingURP => GraphicsSettings.defaultRenderPipeline != null;

        private static Material Mat(string name, Color color)
        {
            if (Mats.TryGetValue(name, out var m)) return m;
            string path = MatDir + "/" + name + ".mat";
            m = AssetDatabase.LoadAssetAtPath<Material>(path);
            Shader sh = UsingURP ? Shader.Find("Universal Render Pipeline/Lit") : Shader.Find("Standard");
            if (sh == null) sh = Shader.Find("Standard");
            if (m == null) { m = new Material(sh); AssetDatabase.CreateAsset(m, path); }
            else if (m.shader != sh) m.shader = sh;
            m.color = color;
            if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", color);
            EditorUtility.SetDirty(m);
            Mats[name] = m;
            return m;
        }

        private static GameObject Block(string name, Transform parent, Vector3 pos, Vector3 scale, Material mat,
                                        PrimitiveType type = PrimitiveType.Cube, bool collider = true)
        {
            var go = GameObject.CreatePrimitive(type);
            go.name = name;
            go.transform.SetParent(parent, false);
            go.transform.localPosition = pos;
            go.transform.localScale = scale;
            go.GetComponent<Renderer>().sharedMaterial = mat;
            if (!collider) Object.DestroyImmediate(go.GetComponent<Collider>());
            return go;
        }

        private static GameObject Group(string name, Transform parent = null, Vector3 pos = default)
        {
            var go = new GameObject(name);
            if (parent != null) go.transform.SetParent(parent, false);
            go.transform.localPosition = pos;
            return go;
        }

        private static void Set(Object target, string field, object value)
        {
            var so = new SerializedObject(target);
            var p = so.FindProperty(field);
            if (p == null) { Debug.LogWarning($"[Aron] Field '{field}' not found on {target.GetType().Name}"); return; }
            switch (value)
            {
                case Object o: p.objectReferenceValue = o; break;
                case float f: p.floatValue = f; break;
                case int i: p.intValue = i; break;
                case bool b: p.boolValue = b; break;
                case string s: p.stringValue = s; break;
                case Vector3 v: p.vector3Value = v; break;
                case Color c: p.colorValue = c; break;
                case System.Enum e: p.enumValueIndex = System.Convert.ToInt32(e); break;
            }
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void SetArray(Object target, string field, Object[] values)
        {
            var so = new SerializedObject(target);
            var p = so.FindProperty(field);
            p.arraySize = values.Length;
            for (int i = 0; i < values.Length; i++) p.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static BoxCollider Trigger(GameObject go, Vector3 size, Vector3 center = default)
        {
            var b = go.AddComponent<BoxCollider>();
            b.isTrigger = true; b.size = size; b.center = center;
            return b;
        }

        private static void Kinematic(GameObject go)
        {
            var rb = go.AddComponent<Rigidbody>();
            rb.isKinematic = true; rb.useGravity = false;
        }

        // ------------------------------------------------------------------ shared scene dressing
        private static Light SetupSkyAndSun()
        {
            RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = new Color(0.78f, 0.8f, 0.85f);
            RenderSettings.ambientEquatorColor = new Color(0.8f, 0.68f, 0.5f);
            RenderSettings.ambientGroundColor = new Color(0.5f, 0.38f, 0.26f);
            RenderSettings.fog = true;
            RenderSettings.fogColor = new Color(0.93f, 0.84f, 0.7f);
            RenderSettings.fogMode = FogMode.Linear;
            RenderSettings.fogStartDistance = 55f;
            RenderSettings.fogEndDistance = 190f;

            var sky = AssetDatabase.LoadAssetAtPath<Material>(MatDir + "/Sky.mat");
            var skyShader = Shader.Find("Skybox/Procedural");
            if (sky == null && skyShader != null) { sky = new Material(skyShader); AssetDatabase.CreateAsset(sky, MatDir + "/Sky.mat"); }
            if (sky != null)
            {
                sky.SetColor("_SkyTint", new Color(0.55f, 0.65f, 0.85f));
                sky.SetColor("_GroundColor", new Color(0.82f, 0.68f, 0.48f));
                sky.SetFloat("_AtmosphereThickness", 0.85f);
                sky.SetFloat("_Exposure", 1.25f);
                sky.SetFloat("_SunSize", 0.05f);
                EditorUtility.SetDirty(sky);
                RenderSettings.skybox = sky;
            }

            var sun = new GameObject("Sun", typeof(Light));
            var light = sun.GetComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = 1.35f;
            light.color = new Color(1f, 0.93f, 0.8f);
            light.shadows = LightShadows.Soft;
            light.shadowStrength = 0.75f;
            sun.transform.rotation = Quaternion.Euler(42f, -35f, 0f);
            RenderSettings.sun = light;
            return light;
        }

        private static Camera MakeCamera(Vector3 pos)
        {
            var camGo = new GameObject("Main Camera", typeof(Camera), typeof(AudioListener));
            camGo.tag = "MainCamera";
            camGo.transform.position = pos;
            var cam = camGo.GetComponent<Camera>();
            cam.clearFlags = RenderSettings.skybox != null ? CameraClearFlags.Skybox : CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.62f, 0.78f, 0.92f);
            cam.fieldOfView = 55f;
            cam.farClipPlane = 300f;
            return cam;
        }

        private static GameObject PlaceCharacter(GameObject prefab, Transform parent, Vector3 pos, float yaw, string name)
        {
            if (prefab == null) return Group(name, parent, pos);
            var go = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent);
            go.name = name;
            go.transform.localPosition = pos;
            go.transform.localRotation = Quaternion.Euler(0f, yaw, 0f);
            return go;
        }

        // ------------------------------------------------------------------ menu scene
        private static void BuildMenuScene(Configs cfg)
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            SetupSkyAndSun();
            var cam = MakeCamera(new Vector3(2.4f, 1.5f, -4.5f));
            cam.transform.rotation = Quaternion.Euler(6f, 0f, 0f);

            var set = Group("Backdrop").transform;
            var mSand = Mat("Sand", new Color(0.86f, 0.72f, 0.5f));
            Block("Ground", set, new Vector3(0f, -0.5f, 10f), new Vector3(80f, 1f, 60f), mSand, PrimitiveType.Cube, false);
            PlaceCharacter(cfg.aronModel, set, new Vector3(-1.1f, 0f, 0f), 160f, "Aron");
            PlaceCharacter(cfg.nouraModel, set, new Vector3(1.3f, 0f, 0.4f), 200f, "Noura");
            if (cfg.camelModel != null) PlaceCharacter(cfg.camelModel, set, new Vector3(-4f, 0f, 2.5f), 120f, "Camel");
            AronArt.Place(AronArt.Nature + "tree_palmTall.fbx", set, new Vector3(-4.5f, 0f, 4f), 20f, 4.5f);
            AronArt.Place(AronArt.Nature + "tree_palmBend.fbx", set, new Vector3(4.8f, 0f, 5f), 200f, 4f);
            AronArt.Place(AronArt.Nature + "rock_largeA.fbx", set, new Vector3(3f, 0f, 2.5f), 40f, 2f);
            BuildMudHouse(set, new Vector3(-7f, 0f, 12f), 2, 0f);
            BuildMudHouse(set, new Vector3(7.5f, 0f, 14f), 3, 0f);
            BuildMasmak(set, new Vector3(0f, 0f, 30f));

            var menu = new GameObject("MainMenu").AddComponent<MainMenuController>();
            Set(menu, "roster", cfg.roster);
            EditorSceneManager.SaveScene(scene, MenuPath);
        }

        // ------------------------------------------------------------------ Riyadh level
        private static readonly Color MudColor = new Color(0.86f, 0.62f, 0.42f);
        private static readonly Color MudDark = new Color(0.72f, 0.5f, 0.34f);

        private static void BuildRiyadhScene(Configs cfg)
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            SetupSkyAndSun();

            var mSand = Mat("Sand", new Color(0.86f, 0.72f, 0.5f));
            var mGlass = Mat("Glass", new Color(0.42f, 0.56f, 0.66f));
            var mSteel = Mat("Steel", new Color(0.62f, 0.64f, 0.68f));
            var mWave = Mat("Shockwave", new Color(1f, 0.6f, 0.1f));
            var mSign = Mat("SignBoard", new Color(0.45f, 0.28f, 0.16f));
            if (mGlass.HasProperty("_Smoothness")) mGlass.SetFloat("_Smoothness", 0.8f);
            if (mSteel.HasProperty("_Metallic")) mSteel.SetFloat("_Metallic", 0.6f);

            var rig = BuildRig(cfg, cfg.level);
            var systems = rig.systems;
            var player = rig.player;

            // ---- Templates (inactive scene objects used as prefabs at runtime)
            var templates = Group("_Templates");
            var dateTpl = MakeCoin(templates.transform, Vector3.zero);
            dateTpl.SetActive(false);
            var waveTpl = Block("ShockwaveTemplate", templates.transform, Vector3.zero, new Vector3(1f, 0.05f, 1f), mWave, PrimitiveType.Cylinder, false);
            waveTpl.AddComponent<BossShockwave>();
            waveTpl.SetActive(false);
            var minionTpl = MakeEnemy(templates.transform, "MinionTemplate", Vector3.zero, cfg.minion, cfg.chaserModel, Vector3.zero, dateTpl);
            minionTpl.gameObject.SetActive(false);
            BuildFx(systems, templates.transform);

            // ---- World
            var world = Group("World");
            var kill = Group("KillZone", world.transform, new Vector3(0f, -4.5f, 150f));
            Trigger(kill, new Vector3(400f, 2f, 520f));
            kill.AddComponent<KillZone>();
            Block("DesertFloor", world.transform, new Vector3(0f, -6.5f, 150f), new Vector3(400f, 1f, 540f), mSand, PrimitiveType.Cube, false);

            // (startZ, endZ, y) segments; gaps between them are jumps.
            float[,] segs = { { -4, 22, 0 }, { 26, 44, 0 }, { 47, 52, 1.2f }, { 55, 80, 0 }, { 84, 100, 0 }, { 114, 140, 0 } };
            BuildCorridor(world.transform, segs);

            var path = Group("Path", world.transform).transform;
            var mover = Group("MovingPlatform", path, new Vector3(0f, -0.25f, 102.5f));
            var moverCol = mover.AddComponent<BoxCollider>();
            moverCol.size = new Vector3(4f, 0.5f, 4f);
            Kinematic(mover);
            Trigger(mover, new Vector3(3.6f, 1.6f, 3.6f), new Vector3(0f, 1f, 0f));
            AronArt.Place(AronArt.Platformer + "block-grass-low-large.fbx", mover.transform, new Vector3(0f, -0.25f, 0f), 0f, new Vector3(4f / 2.08f, 1f, 4f / 2.08f));
            var mp = mover.AddComponent<MovingPlatform>();
            Set(mp, "offset", new Vector3(0f, 0f, 9f));
            Set(mp, "period", 5f);

            // Low beam to slide under
            var beam = Group("SlideBeam", path, new Vector3(0f, 1.6f, 70f));
            beam.AddComponent<BoxCollider>().size = new Vector3(8f, 1.2f, 1f);
            var beamArt = AronArt.Place(AronArt.Castle + "wall.fbx", beam.transform, new Vector3(0f, -0.6f, 0f), 0f, new Vector3(8.4f, 1.2f / 1.31f, 1f));
            AronArt.Tint(beamArt, MudDark);
            foreach (float x in new[] { -4.3f, 4.3f })
            {
                var post = AronArt.Place(AronArt.Castle + "wall-pillar.fbx", path, new Vector3(x, 0f, 70f), 0f, new Vector3(0.7f, 2.3f / 1.31f, 0.7f), "SlideBeamPost");
                AronArt.Tint(post, MudDark);
            }

            // ---- Landmarks
            BuildDiriyah(world.transform, new Vector3(-16f, 0f, 14f));
            BuildMasmak(world.transform, new Vector3(18f, 0f, 60f));
            BuildArchTower(world.transform, new Vector3(-26f, 0f, 125f), mGlass, mSteel);
            BuildTown(world.transform, segs);

            // ---- Collectibles & crates
            var items = Group("Coins", world.transform).transform;
            for (float z = 5f; z < 140f; z += 3f)
            {
                if (InGap(segs, z)) continue;
                float y = GroundY(segs, z);
                MakeCoin(items, new Vector3(Mathf.Sin(z * 0.3f) * 2f, y + 0.8f, z));
            }
            var crates = Group("Crates", world.transform).transform;
            MakeCrate(crates, new Vector3(2f, 0f, 10f), Crate.CrateType.Basic);
            MakeCrate(crates, new Vector3(-2f, 0f, 18f), Crate.CrateType.Bounce);
            MakeCrate(crates, new Vector3(-2f, 0f, 31f), Crate.CrateType.Basic);
            MakeCrate(crates, new Vector3(-2f, 1f, 31f), Crate.CrateType.Basic);
            MakeCrate(crates, new Vector3(2.5f, 0f, 36f), Crate.CrateType.Life);
            MakeCrate(crates, new Vector3(0f, 1.2f, 49f), Crate.CrateType.Basic);
            MakeCrate(crates, new Vector3(-2f, 0f, 62f), Crate.CrateType.Basic);
            MakeCrate(crates, new Vector3(2f, 0f, 77f), Crate.CrateType.Bounce);
            MakeCrate(crates, new Vector3(0f, 0f, 92f), Crate.CrateType.Basic);
            MakeCrate(crates, new Vector3(-2.5f, 0f, 118f), Crate.CrateType.Life);
            MakeCrate(crates, new Vector3(2.5f, 0f, 124f), Crate.CrateType.Basic);
            MakeCrate(crates, new Vector3(2.5f, 1f, 124f), Crate.CrateType.Basic);

            // ---- Enemies
            var enemies = Group("Enemies", world.transform).transform;
            MakeEnemy(enemies, "Patrol_Diriyah", new Vector3(-3f, 0f, 28f), cfg.patrol, cfg.guardModel, new Vector3(6f, 0f, 0f), dateTpl);
            MakeEnemy(enemies, "Patrol_Masmak", new Vector3(-3f, 0f, 58f), cfg.patrol, cfg.guardModel, new Vector3(6f, 0f, 0f), dateTpl);
            MakeEnemy(enemies, "Chaser_Masmak", new Vector3(2f, 0f, 75f), cfg.chaser, cfg.chaserModel, new Vector3(0f, 0f, -4f), dateTpl);
            MakeEnemy(enemies, "Chaser_Kingdom", new Vector3(0f, 0f, 90f), cfg.chaser, cfg.chaserModel, new Vector3(-3f, 0f, 0f), dateTpl);
            MakeEnemy(enemies, "Patrol_Kingdom", new Vector3(-3f, 0f, 122f), cfg.patrol, cfg.guardModel, new Vector3(6f, 0f, 0f), dateTpl);

            // ---- Checkpoints
            MakeCheckpoint(world.transform, new Vector3(3.2f, 0f, 40f));
            MakeCheckpoint(world.transform, new Vector3(3.2f, 0f, 86f));
            MakeCheckpoint(world.transform, new Vector3(3.2f, 0f, 136f));

            // ---- Shop signs (remote-configurable)
            DressShop(MakeShopSign(world.transform, "shop_1", new Vector3(-6.5f, 0f, 24f), mSign), ShopKind.Shawarma);
            DressShop(MakeShopSign(world.transform, "shop_2", new Vector3(6.5f, 0f, 46f), mSign), ShopKind.Grocery);
            DressShop(MakeShopSign(world.transform, "shop_3", new Vector3(-6.5f, 0f, 96f), mSign), ShopKind.Oud);
            var shop4 = MakeShopSign(world.transform, "shop_4", new Vector3(6.5f, 0f, 128f), mSign);
            DressShop(shop4, ShopKind.Qahwa);
            BuildStreetLife(world.transform, mSign);
            var shopkeeper = PlaceCharacter(cfg.shopkeeperModel, shop4.transform, new Vector3(0f, 0f, -1.6f), 180f, "Shopkeeper");
            Set(shopkeeper.AddComponent<DialogueSpeaker>(), "speakerId", "shopkeeper");
            var guide = Group("Guide", world.transform, new Vector3(-5.5f, 5.5f, 8f));
            Set(guide.AddComponent<DialogueSpeaker>(), "speakerId", "guide");

            // ---- Dialogue triggers
            MakeDialogueTrigger(world.transform, "Talk_Intro", new Vector3(0f, 1f, 4f), cfg.intro);
            MakeDialogueTrigger(world.transform, "Talk_Masmak", new Vector3(0f, 1f, 56f), cfg.masmak);
            MakeDialogueTrigger(world.transform, "Talk_Kingdom", new Vector3(0f, 1f, 116f), cfg.kingdom);

            BuildDesert(world.transform, cfg, dateTpl);
            BuildBossArena(world.transform, cfg, waveTpl, minionTpl, DesertEnd + 20.5f);
            // Dr. Duhaim himself rather than the stage scene's stand-in soldier.
            var duhaim = world.GetComponentInChildren<RiyadhBoss>(true);
            if (duhaim != null)
            {
                var look = duhaim.gameObject.AddComponent<BossLook>();
                Set(look, "model", "scientist");
                Set(look, "body", new Color(0.3f, 0.32f, 0.25f));
                Set(look, "trim", new Color(0.85f, 0.7f, 0.25f));
                Set(look, "glow", new Color(1f, 0.6f, 0.1f));
                Set(look, "material", Mat("BossBody", Color.white));
            }
            if (cfg.falconModel != null) PlaceCharacter(cfg.falconModel, null, player.transform.position + new Vector3(0.6f, 2f, -0.3f), 0f, "Falcon");
            if (cfg.oryxModel != null) PlaceCharacter(cfg.oryxModel, null, player.transform.position + new Vector3(-1.4f, 0f, -2f), 0f, "Oryx");

            EditorSceneManager.SaveScene(scene, RiyadhPath);
        }

        private struct Rig
        {
            public GameObject systems, player;
            public GameManager gm;
            public PlayerController pc;
            public Camera cam;
        }

        /// <summary>Game systems, HUD, Aron and the follow camera: the same in every playable scene.</summary>
        private static Rig BuildRig(Configs cfg, LevelConfig level)
        {
            // ---- Systems
            var systems = Group("_Systems");
            var gm = systems.AddComponent<GameManager>();
            Set(gm, "levelConfig", level);
            systems.AddComponent<VoiceCommandRouter>();
            systems.AddComponent<InputRouter>();
            systems.AddComponent<DialogueDirector>();
            systems.AddComponent<EmotionDirector>();
            var rc = systems.AddComponent<RemoteConfigService>();
            Set(rc, "url", level.remoteConfigUrl ?? "");
            Group("_UI").AddComponent<GameUI>();

            // ---- Player
            var player = Group("Player", null, new Vector3(0f, 0.1f, 1f));
            player.tag = "Player";
            var cc = player.AddComponent<CharacterController>();
            cc.height = 1.8f; cc.radius = 0.4f; cc.center = new Vector3(0f, 0.92f, 0f); cc.stepOffset = 0.4f; cc.skinWidth = 0.05f;
            var visual = Group("Visual", player.transform);
            var anim = player.AddComponent<PlayerAnimationHooks>();
            var melee = player.AddComponent<MeleeAttack>();
            var setup = player.AddComponent<CharacterSetup>();
            var pc = player.AddComponent<PlayerController>();
            player.AddComponent<Rider>();
            player.AddComponent<CompanionAbilityRunner>();
            player.AddComponent<Aron.Heroes.HeroTeam>();
            var speaker = player.AddComponent<DialogueSpeaker>();
            Set(speaker, "speakerId", "player");
            Set(speaker, "bubbleOffset", new Vector3(0f, 2.4f, 0f));
            Set(setup, "roster", cfg.roster);
            Set(setup, "visualRoot", visual.transform);
            Set(setup, "melee", melee);
            Set(setup, "anim", anim);
            Set(pc, "config", cfg.player);
            Set(pc, "visualRoot", visual.transform);
            Set(pc, "melee", melee);
            Set(pc, "anim", anim);
            Set(gm, "player", pc);

            var cam = MakeCamera(player.transform.position + new Vector3(0f, 4.2f, -7f));
            var follow = cam.gameObject.AddComponent<FollowCamera>();
            Set(follow, "target", player.transform);
            Set(pc, "cameraTransform", cam.transform);

            return new Rig { systems = systems, player = player, gm = gm, pc = pc, cam = cam };
        }

        private static bool InGap(float[,] segs, float z)
        {
            for (int i = 0; i < segs.GetLength(0); i++) if (z >= segs[i, 0] + 0.5f && z <= segs[i, 1] - 0.5f) return false;
            return true;
        }

        private static float GroundY(float[,] segs, float z)
        {
            for (int i = 0; i < segs.GetLength(0); i++) if (z >= segs[i, 0] && z <= segs[i, 1]) return segs[i, 2];
            return 0f;
        }

        // ------------------------------------------------------------------ corridor
        private const float HalfWidth = 4f;

        private static void BuildCorridor(Transform world, float[,] segs)
        {
            var path = Group("Corridor", world).transform;
            for (int i = 0; i < segs.GetLength(0); i++)
            {
                float z0 = segs[i, 0], z1 = segs[i, 1], y = segs[i, 2];
                float len = z1 - z0;
                var seg = Group("Segment_" + i, path, new Vector3(0f, y, (z0 + z1) * 0.5f));
                var col = seg.AddComponent<BoxCollider>();
                col.center = new Vector3(0f, -0.5f, 0f);
                col.size = new Vector3(HalfWidth * 2f, 1f, len);

                int rows = Mathf.Max(1, Mathf.RoundToInt(len / 2f));
                float rowLen = len / rows;
                var tiles = Group("Tiles", seg.transform).transform;
                for (int r = 0; r < rows; r++)
                    for (int c = 0; c < 4; c++)
                    {
                        var pos = new Vector3(-HalfWidth + 1f + c * 2f, -1f, -len * 0.5f + rowLen * (r + 0.5f));
                        AronArt.Place(AronArt.Platformer + "block-grass-large.fbx", tiles, pos, 0f, new Vector3(2f / 2.08f, 1f, rowLen / 2.08f));
                    }
                // Earth under the walkway so gaps read as trenches rather than floating slabs.
                Block("Earth", seg.transform, new Vector3(0f, -3.5f, 0f), new Vector3(HalfWidth * 2f - 0.2f, 5f, len - 0.2f), Mat("Earth", MudDark), PrimitiveType.Cube, false);

                foreach (float side in new[] { -1f, 1f })
                {
                    var wall = Group("EdgeWall", seg.transform, new Vector3(side * (HalfWidth + 0.35f), 1.2f, 0f));
                    var bc = wall.AddComponent<BoxCollider>();
                    bc.size = new Vector3(0.7f, 3.4f, len);
                    int pieces = Mathf.Max(1, Mathf.RoundToInt(len / 2f));
                    float pieceLen = len / pieces;
                    for (int p = 0; p < pieces; p++)
                    {
                        var w = AronArt.Place(AronArt.Castle + "wall-half.fbx", seg.transform,
                            new Vector3(side * (HalfWidth + 0.35f), 0f, -len * 0.5f + pieceLen * (p + 0.5f)), 90f,
                            new Vector3(pieceLen, 0.75f / 1.31f, 0.7f));
                        AronArt.Tint(w, MudColor);
                    }
                }
            }
        }

        private static void BuildTown(Transform world, float[,] segs)
        {
            var town = Group("Town", world).transform;
            var mGround = Mat("TownGround", new Color(0.83f, 0.68f, 0.47f));
            foreach (float side in new[] { -1f, 1f })
                Block("TownGround", town, new Vector3(side * 34f, -3.3f, 68f), new Vector3(59f, 6f, 160f), mGround, PrimitiveType.Cube, false);

            var rnd = new System.Random(11);
            float R(float a, float b) => a + (float)rnd.NextDouble() * (b - a);
            for (float z = -6f; z < 142f; z += R(5f, 8f))
            {
                foreach (float side in new[] { -1f, 1f })
                {
                    bool nearDiriyah = side < 0 && z > 2f && z < 28f;
                    bool nearMasmak = side > 0 && z > 48f && z < 74f;
                    bool nearKingdom = side < 0 && z > 112f;
                    if (nearDiriyah || nearMasmak || nearKingdom) continue;
                    if (rnd.NextDouble() < 0.65)
                        BuildMudHouse(town, new Vector3(side * R(9f, 12f), 0f, z + R(-1f, 1f)), rnd.Next(1, 4), R(-8f, 8f));
                    else
                        AronArt.Place(AronArt.Nature + (rnd.NextDouble() < 0.5 ? "tree_palmTall.fbx" : "tree_palmBend.fbx"),
                            town, new Vector3(side * R(7f, 9f), 0f, z), R(0f, 360f), R(4f, 5.5f));
                }
            }
            string[] props = { "rock_largeA", "rock_largeC", "rock_tallA", "cactus_tall", "cactus_short", "plant_bushSmall", "stone_tallB" };
            for (int i = 0; i < 90; i++)
            {
                float side = rnd.NextDouble() < 0.5 ? -1f : 1f;
                var pos = new Vector3(side * R(14f, 55f), 0f, R(-10f, 150f));
                var p = props[rnd.Next(props.Length)];
                AronArt.Place(AronArt.Nature + p + ".fbx", town, pos, R(0f, 360f), p.StartsWith("rock_large") ? R(3f, 6f) : R(2f, 4f));
            }
            for (int i = 0; i < 26; i++)
            {
                float side = rnd.NextDouble() < 0.5 ? -1f : 1f;
                AronArt.Place(AronArt.Nature + "tree_palmDetailedTall.fbx", town, new Vector3(side * R(16f, 50f), 0f, R(-5f, 145f)), R(0f, 360f), R(4.5f, 6.5f));
            }
            // Barrels and fences close to the walkway give it a market-street feel.
            for (float z = 6f; z < 138f; z += R(9f, 14f))
            {
                if (InGap(segs, z)) continue;
                float side = rnd.NextDouble() < 0.5 ? -1f : 1f;
                AronArt.Place(AronArt.Platformer + "barrel.fbx", town, new Vector3(side * R(5.2f, 6f), 0f, z), R(0f, 360f), 2f);
            }
        }

        private static void BuildMudHouse(Transform parent, Vector3 pos, int floors, float yaw)
        {
            var house = Group("MudHouse", parent, pos);
            house.transform.localRotation = Quaternion.Euler(0f, yaw, 0f);
            float w = 4.2f;
            AronArt.Place(AronArt.Castle + "tower-square-base.fbx", house.transform, Vector3.zero, 0f, w);
            for (int f = 1; f <= floors; f++)
            {
                string piece = f == 1 ? "tower-square-mid-door.fbx" : "tower-square-mid-windows.fbx";
                AronArt.Place(AronArt.Castle + piece, house.transform, new Vector3(0f, f * w * 1.01f, 0f), f == 1 ? 180f : 0f, w);
            }
            AronArt.Place(AronArt.Castle + "tower-square-top.fbx", house.transform, new Vector3(0f, (floors + 1) * w * 1.01f, 0f), 0f, w);
            AronArt.Tint(house, floors % 2 == 0 ? MudColor : MudDark);
        }

        // ------------------------------------------------------------------ landmarks
        private static void BuildMasmak(Transform parent, Vector3 pos)
        {
            // Masmak: square mud-brick fort, four tall corner towers, crenellated walls, gate facing the road.
            var root = Group("Landmark_Masmak", parent, pos);
            Set(root.AddComponent<Landmark>(), "focusHeight", 6f);
            var t = root.transform;
            const float half = 8f, seg = 2f;
            for (float a = -half + seg * 0.5f; a < half; a += seg)
            {
                var size = new Vector3(seg, 4.5f, 1.5f);
                AronArt.Place(AronArt.Castle + "wall.fbx", t, new Vector3(a, 0f, half), 0f, size);
                AronArt.Place(AronArt.Castle + "wall.fbx", t, new Vector3(a, 0f, -half), 180f, size);
                AronArt.Place(AronArt.Castle + "wall.fbx", t, new Vector3(half, 0f, a), 90f, size);
                if (Mathf.Abs(a) > 2f) AronArt.Place(AronArt.Castle + "wall.fbx", t, new Vector3(-half, 0f, a), -90f, size);
            }
            var gate = AronArt.Place(AronArt.Castle + "gate.fbx", t, new Vector3(-half, 0f, 0f), 0f, new Vector3(4f, 5f, 6f));
            AronArt.Tint(gate, new Color(0.45f, 0.3f, 0.18f), null, "MasmakGate");
            foreach (var c in new[] { new Vector3(-half, 0, -half), new Vector3(half, 0, -half), new Vector3(-half, 0, half), new Vector3(half, 0, half) })
            {
                float w = 3.6f;
                AronArt.Place(AronArt.Castle + "tower-square-base.fbx", t, c, 0f, w);
                for (int f = 1; f <= 3; f++)
                    AronArt.Place(AronArt.Castle + (f == 2 ? "tower-square-mid-windows.fbx" : "tower-square-mid.fbx"), t, c + Vector3.up * f * w * 1.01f, 0f, w);
                AronArt.Place(AronArt.Castle + "tower-square-top.fbx", t, c + Vector3.up * 4f * w * 1.01f, 0f, w);
            }
            var keep = Group("Keep", t, new Vector3(2f, 0f, 2f));
            for (int f = 0; f < 3; f++)
                AronArt.Place(AronArt.Castle + (f == 0 ? "tower-square-base.fbx" : "tower-square-mid-windows.fbx"), keep.transform, Vector3.up * f * 5.05f, 0f, 5f);
            AronArt.Place(AronArt.Castle + "tower-square-top.fbx", keep.transform, Vector3.up * 3f * 5.05f, 0f, 5f);
            AronArt.Tint(root, MudColor);
            var flag = AronArt.Place(AronArt.Castle + "flag-banner-long.fbx", t, new Vector3(-half - 0.8f, 6f, -3.5f), 0f, 1.6f);
            AronArt.Tint(flag, new Color(0.1f, 0.5f, 0.25f), null, "SaudiGreen");
        }

        private static void BuildArchTower(Transform parent, Vector3 pos, Material glass, Material steel)
        {
            // Arch tower: tapered tower that splits into two arms around a parabolic opening, with a skybridge.
            var root = Group("Landmark_ArchTower", parent, pos);
            var lm = root.AddComponent<Landmark>();
            Set(lm, "focusHeight", 25f);
            Set(lm, "viewDistance", 70f);
            const float baseHalf = 7f, depth = 4f, split = 34f, top = 66f;
            var trunk = new List<Vector4>();
            for (int i = 0; i <= 6; i++)
            {
                float y = split * i / 6f;
                float hw = Mathf.Lerp(baseHalf, baseHalf * 0.86f, i / 6f);
                trunk.Add(new Vector4(y, -hw, hw, Mathf.Lerp(depth, depth * 0.85f, i / 6f)));
            }
            var arm = new List<Vector4>();
            var armR = new List<Vector4>();
            float splitHalf = baseHalf * 0.86f;
            for (int i = 0; i <= 12; i++)
            {
                float k = i / 12f;
                float y = Mathf.Lerp(split, top, k);
                float outer = Mathf.Lerp(splitHalf, baseHalf * 0.42f, k * k);
                float inner = Mathf.Lerp(0f, baseHalf * 0.42f - 0.8f, Mathf.Sqrt(k));
                inner = Mathf.Min(inner + 0.05f, outer - 0.5f);
                float d = Mathf.Lerp(depth * 0.85f, depth * 0.5f, k);
                arm.Add(new Vector4(y, -outer, -inner, d));
                armR.Add(new Vector4(y, inner, outer, d));
            }
            MeshPart("Trunk", root.transform, AronArt.Loft("ArchTower_Trunk", trunk), glass);
            MeshPart("ArmLeft", root.transform, AronArt.Loft("ArchTower_ArmL", arm), glass);
            MeshPart("ArmRight", root.transform, AronArt.Loft("ArchTower_ArmR", armR), glass);
            var bridge = new List<Vector4> { new Vector4(top - 13f, -4.5f, 4.5f, 1.6f), new Vector4(top - 11.5f, -4.5f, 4.5f, 1.6f) };
            MeshPart("Skybridge", root.transform, AronArt.Loft("ArchTower_Bridge", bridge), steel);
            var plinth = new List<Vector4> { new Vector4(0f, -baseHalf - 3f, baseHalf + 3f, depth + 3f), new Vector4(1.2f, -baseHalf - 2.5f, baseHalf + 2.5f, depth + 2.5f) };
            MeshPart("Plinth", root.transform, AronArt.Loft("ArchTower_Plinth", plinth), steel);
            root.transform.localRotation = Quaternion.Euler(0f, 20f, 0f);
        }

        private static void MeshPart(string name, Transform parent, Mesh mesh, Material mat)
        {
            var go = new GameObject(name, typeof(MeshFilter), typeof(MeshRenderer));
            go.transform.SetParent(parent, false);
            go.GetComponent<MeshFilter>().sharedMesh = mesh;
            go.GetComponent<MeshRenderer>().sharedMaterial = mat;
        }

        private static void BuildDiriyah(Transform parent, Vector3 pos)
        {
            // At-Turaif: tight cluster of Najdi mud-brick houses with palms between them.
            var root = Group("Landmark_Diriyah", parent, pos);
            Set(root.AddComponent<Landmark>(), "focusHeight", 5f);
            var rnd = new System.Random(7);
            for (int i = 0; i < 9; i++)
            {
                var p = new Vector3((i % 3) * 5f - 5f, 0f, (i / 3) * 6f - 6f);
                BuildMudHouse(root.transform, p, 1 + rnd.Next(3), rnd.Next(4) * 90f);
            }
            for (int i = 0; i < 5; i++)
                AronArt.Place(AronArt.Nature + "tree_palmTall.fbx", root.transform, new Vector3(8f, 0f, i * 6f - 10f), i * 70f, 4.5f + i % 2);
        }

        // ------------------------------------------------------------------ boss arena
        private static void BuildBossArena(Transform world, Configs cfg, GameObject waveTpl, Enemy minionTpl, float arenaZ)
        {
            var arena = Group("BossArena", world, new Vector3(0f, 0f, arenaZ));
            var t = arena.transform;
            var floor = Group("ArenaFloor", t, new Vector3(0f, -1f, 0f));
            var fc = floor.AddComponent<MeshCollider>();
            fc.sharedMesh = PrimitiveMesh(PrimitiveType.Cylinder);
            floor.transform.localScale = new Vector3(30f, 1f, 30f);
            for (int x = -7; x <= 7; x++)
                for (int z = -7; z <= 7; z++)
                {
                    var p = new Vector3(x * 2f, -1f, z * 2f);
                    if (p.x * p.x + p.z * p.z > 15f * 15f) continue;
                    AronArt.Place(AronArt.Platformer + "block-grass-large.fbx", t, p, 0f, new Vector3(2f / 2.08f, 1f, 2f / 2.08f));
                }
            Block("ArenaBase", t, new Vector3(0f, -3.6f, 0f), new Vector3(31f, 2.5f, 31f), Mat("Earth", MudDark), PrimitiveType.Cylinder, false);

            var bridge = Group("ArenaBridge", t, new Vector3(0f, 0f, -17.5f));
            bridge.AddComponent<BoxCollider>().center = new Vector3(0f, -0.5f, 0f);
            bridge.GetComponent<BoxCollider>().size = new Vector3(8f, 1f, 6f);
            for (int c = 0; c < 4; c++)
                for (int r = 0; r < 3; r++)
                    AronArt.Place(AronArt.Platformer + "block-grass-large.fbx", bridge.transform, new Vector3(-3f + c * 2f, -1f, -2f + r * 2f), 0f, 2f / 2.08f);

            for (int i = 0; i < 16; i++)
            {
                float a = i / 16f * Mathf.PI * 2f;
                if (Mathf.Abs(Mathf.DeltaAngle(a * Mathf.Rad2Deg, 180f)) < 20f) continue;
                if (Mathf.Abs(Mathf.DeltaAngle(a * Mathf.Rad2Deg, 0f)) < 20f) continue;
                var w = Group("ArenaWall_" + i, t, new Vector3(Mathf.Sin(a) * 15.5f, 0f, Mathf.Cos(a) * 15.5f));
                w.transform.localRotation = Quaternion.Euler(0f, a * Mathf.Rad2Deg, 0f);
                var bc = w.AddComponent<BoxCollider>();
                bc.center = new Vector3(0f, 1.5f, 0f);
                bc.size = new Vector3(6.2f, 3f, 1f);
                var art = AronArt.Place(AronArt.Castle + "wall.fbx", w.transform, Vector3.zero, 0f, new Vector3(6.2f, 3.2f / 1.31f, 1.2f));
                AronArt.Tint(art, MudColor);
            }
            var gateIn = MakeGate(t, "Gate_Entrance", new Vector3(0f, 0f, -15.5f));
            gateIn.SetActive(false);
            var gateOut = MakeGate(t, "Gate_Exit", new Vector3(0f, 0f, 15.5f));

            var bossGo = Group("RiyadhBoss", t, new Vector3(0f, 1.6f, 4f));
            var body = bossGo.AddComponent<CapsuleCollider>();
            body.radius = 1.2f; body.height = 3.2f;
            Kinematic(bossGo);
            var bossHurt = Group("HurtTrigger", bossGo.transform);
            var sc = bossHurt.AddComponent<SphereCollider>(); sc.isTrigger = true; sc.radius = 1.8f;
            var bossModel = PlaceCharacter(cfg.bossModel, bossGo.transform, new Vector3(0f, -1.6f, 0f), 0f, "Model");
            var boss = bossGo.AddComponent<RiyadhBoss>();
            var bossSpeaker = bossGo.AddComponent<DialogueSpeaker>();
            Set(bossSpeaker, "speakerId", "boss");
            Set(bossSpeaker, "bubbleOffset", new Vector3(0f, 2.6f, 0f));
            Set(boss, "config", cfg.boss);
            Set(boss, "shockwavePrefab", waveTpl);
            Set(boss, "minionPrefab", minionTpl);
            Set(boss, "arenaRadius", 13.5f);
            Set(boss, "speaker", bossSpeaker);
            Set(boss, "animator", bossModel.GetComponentInChildren<Animator>());
            var sp1 = Group("MinionSpawn_L", t, new Vector3(-9f, 0f, 6f));
            var sp2 = Group("MinionSpawn_R", t, new Vector3(9f, 0f, 6f));
            SetArray(boss, "minionSpawns", new Object[] { sp1.transform, sp2.transform });

            var arenaTrig = Group("ArenaTrigger", t, new Vector3(0f, 1f, -11f));
            Trigger(arenaTrig, new Vector3(8f, 3f, 2f));
            var at = arenaTrig.AddComponent<BossArenaTrigger>();
            Set(at, "boss", boss);
            SetArray(at, "gatesToClose", new Object[] { gateIn });
            MakeDialogueTrigger(t, "Talk_Boss", new Vector3(0f, 1f, -12.5f), cfg.bossIntro);

            // Exit bridge + goal (unlocked when boss is defeated)
            var exit = Group("ExitBridge", t, new Vector3(0f, 0f, 19f));
            var ec = exit.AddComponent<BoxCollider>();
            ec.center = new Vector3(0f, -0.5f, 0f); ec.size = new Vector3(8f, 1f, 8f);
            for (int c = 0; c < 4; c++)
                for (int r = 0; r < 4; r++)
                    AronArt.Place(AronArt.Platformer + "block-grass-large.fbx", exit.transform, new Vector3(-3f + c * 2f, -1f, -3f + r * 2f), 0f, 2f / 2.08f);
            var goal = Group("LevelGoal", t, new Vector3(0f, 0f, 21f));
            Trigger(goal, new Vector3(3f, 2f, 6f), new Vector3(0f, 1.5f, 0f));
            var arch = AronArt.Place(AronArt.Castle + "tower-square-arch.fbx", goal.transform, Vector3.zero, 0f, new Vector3(4f, 4f, 2f));
            AronArt.Tint(arch, new Color(1f, 0.85f, 0.35f), null, "Gold");
            var lg = goal.AddComponent<LevelGoal>();
            Set(lg, "requiresBossDefeat", true);
            Set(lg, "lockedVisual", gateOut);
        }

        private static GameObject MakeGate(Transform parent, string name, Vector3 pos)
        {
            var g = Group(name, parent, pos);
            var bc = g.AddComponent<BoxCollider>();
            bc.center = new Vector3(0f, 1.6f, 0f);
            bc.size = new Vector3(6f, 3.2f, 1f);
            var art = AronArt.Place(AronArt.Castle + "gate.fbx", g.transform, Vector3.zero, 90f, new Vector3(6f, 3.5f, 9f));
            AronArt.Tint(art, new Color(0.45f, 0.3f, 0.18f), null, "GateWood");
            return g;
        }

        private static Mesh PrimitiveMesh(PrimitiveType type)
        {
            var tmp = GameObject.CreatePrimitive(type);
            var mesh = tmp.GetComponent<MeshFilter>().sharedMesh;
            Object.DestroyImmediate(tmp);
            return mesh;
        }

        // ------------------------------------------------------------------ effects
        private static void BuildFx(GameObject systems, Transform templates)
        {
            var wood = new Color(0.62f, 0.4f, 0.2f);
            var fx = new List<FxPlayer.Entry>
            {
                new FxPlayer.Entry { id = "crate", prefab = AronArt.Burst("Crate", templates, 16, PrimitiveType.Cube, wood, new Color(0.45f, 0.28f, 0.12f), 0.28f, 7f, 0.9f, 2.2f, 0.4f) },
                new FxPlayer.Entry { id = "coin", prefab = AronArt.Burst("Coin", templates, 10, PrimitiveType.Cube, new Color(1f, 0.9f, 0.3f), new Color(1f, 1f, 0.8f), 0.12f, 3.5f, 0.45f, -0.3f, 0.2f) },
                new FxPlayer.Entry { id = "hit", prefab = AronArt.Burst("Hit", templates, 12, PrimitiveType.Cube, Color.white, new Color(1f, 0.8f, 0.3f), 0.16f, 6f, 0.3f, 0f, 0.15f) },
                new FxPlayer.Entry { id = "poof", prefab = AronArt.Burst("Poof", templates, 14, PrimitiveType.Sphere, new Color(0.95f, 0.9f, 0.8f), new Color(0.75f, 0.68f, 0.58f), 0.5f, 2.5f, 0.6f, -0.2f, 0.5f) },
                new FxPlayer.Entry { id = "dust", prefab = AronArt.Burst("Dust", templates, 8, PrimitiveType.Sphere, new Color(0.9f, 0.8f, 0.62f), new Color(0.8f, 0.68f, 0.5f), 0.3f, 1.6f, 0.4f, -0.1f, 0.3f) },
            };
            var player = systems.AddComponent<FxPlayer>();
            var so = new SerializedObject(player);
            var arr = so.FindProperty("effects");
            arr.arraySize = fx.Count;
            for (int i = 0; i < fx.Count; i++)
            {
                var e = arr.GetArrayElementAtIndex(i);
                e.FindPropertyRelative("id").stringValue = fx[i].id;
                e.FindPropertyRelative("prefab").objectReferenceValue = fx[i].prefab;
            }
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        // ------------------------------------------------------------------ gameplay objects
        private static GameObject MakeCoin(Transform parent, Vector3 pos)
        {
            var d = Group("Coin", parent, pos);
            var art = AronArt.Place(AronArt.Platformer + "coin-gold.fbx", d.transform, new Vector3(0f, -0.3f, 0f), 0f, 1.5f);
            var sc = d.AddComponent<SphereCollider>(); sc.isTrigger = true; sc.radius = 0.9f;
            d.AddComponent<DateCollectible>();
            return d;
        }

        private static void MakeCrate(Transform parent, Vector3 basePos, Crate.CrateType type)
        {
            var c = Group("Crate_" + type, parent, basePos);
            var bc = c.AddComponent<BoxCollider>();
            bc.center = new Vector3(0f, 0.5f, 0f);
            bc.size = Vector3.one;
            string model = type == Crate.CrateType.Basic ? "crate.fbx" : "crate-strong.fbx";
            float s = type == Crate.CrateType.Basic ? 2f : 1f / 0.6f;
            var art = AronArt.Place(AronArt.Platformer + model, c.transform, Vector3.zero, 0f, s, "Visual");
            if (type == Crate.CrateType.Bounce)
            {
                AronArt.Tint(art, new Color(0.45f, 0.7f, 1f), null, "CrateBounce");
                AronArt.Place(AronArt.Platformer + "spring.fbx", art.transform, new Vector3(0f, 0.6f, 0f), 0f, 0.6f);
            }
            else if (type == Crate.CrateType.Life)
                AronArt.Tint(art, new Color(0.55f, 1f, 0.6f), null, "CrateLife");
            var crate = c.AddComponent<Crate>();
            Set(crate, "type", type);
            Set(crate, "visual", art.transform);
        }

        private static Enemy MakeEnemy(Transform parent, string name, Vector3 pos, EnemyConfig cfg, GameObject model, Vector3 patrol, GameObject date)
        {
            var e = Group(name, parent, pos);
            var art = PlaceCharacter(model, e.transform, Vector3.zero, 0f, "Model");
            var col = e.AddComponent<CapsuleCollider>();
            col.isTrigger = true; col.center = new Vector3(0f, 0.85f, 0f); col.radius = 0.55f; col.height = 1.7f;
            Kinematic(e);
            var en = e.AddComponent<Enemy>();
            Set(en, "config", cfg);
            Set(en, "patrolOffset", patrol);
            Set(en, "datePrefab", date);
            Set(en, "animator", art.GetComponentInChildren<Animator>());
            return en;
        }

        private static void MakeCheckpoint(Transform parent, Vector3 pos)
        {
            var cp = Group("Checkpoint", parent, pos);
            var flag = AronArt.Place(AronArt.Platformer + "flag.fbx", cp.transform, Vector3.zero, -90f, 3f);
            Trigger(cp, new Vector3(8f, 4f, 1.5f), new Vector3(-3f, 2f, 0f));
            var c = cp.AddComponent<Checkpoint>();
            Set(c, "flag", flag.GetComponentInChildren<Renderer>());
        }

        private static GameObject MakeShopSign(Transform parent, string id, Vector3 pos, Material mat)
        {
            var shop = Group("Shop_" + id, parent, pos);
            var stall = AronArt.Place(AronArt.Castle + "tower-square-mid-door.fbx", shop.transform, Vector3.zero, 180f, new Vector3(2.6f, 2.6f, 2.2f), "Stall");
            AronArt.Tint(stall, new Color(0.95f, 0.85f, 0.7f), null, "Stall");
            AronArt.Place(AronArt.Platformer + "barrel.fbx", shop.transform, new Vector3(1.6f, 0f, -0.9f), 30f, 1.8f);
            AronArt.Place(AronArt.Platformer + "chest.fbx", shop.transform, new Vector3(-1.5f, 0f, -1f), -20f, 1.8f);
            var board = Block("SignBoard", shop.transform, new Vector3(0f, 3.2f, -1.2f), new Vector3(3f, 0.9f, 0.12f), mat, PrimitiveType.Cube, false);
            var txtGo = new GameObject("SignText");
            txtGo.transform.SetParent(shop.transform, false);
            txtGo.transform.localPosition = new Vector3(0f, 3.2f, -1.3f);
            var tmp = txtGo.AddComponent<TextMeshPro>();
            tmp.fontSize = 6f; tmp.alignment = TextAlignmentOptions.Center; tmp.color = Color.white;
            tmp.rectTransform.sizeDelta = new Vector2(3f, 1f);
            tmp.text = id;
            shop.transform.rotation = Quaternion.Euler(0f, pos.x < 0 ? -55f : 55f, 0f);
            var sign = shop.AddComponent<ShopSign>();
            Set(sign, "signId", id);
            Set(sign, "label", tmp);
            Set(sign, "boardRenderer", board.GetComponent<Renderer>());
            return shop;
        }

        private static GameObject MakeDialogueTrigger(Transform parent, string name, Vector3 pos, DialogueSequence seq)
        {
            var t = Group(name, parent, pos);
            Trigger(t, new Vector3(8f, 3f, 1.5f));
            var dt = t.AddComponent<DialogueTrigger>();
            Set(dt, "sequence", seq);
            return t;
        }
    }
}
