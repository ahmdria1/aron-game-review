// (c) Ahmed Alamrani. All rights reserved.
using System.Collections.Generic;
using System.Linq;
using Aron.Animals;
using Aron.Logic;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

namespace Aron.EditorTools
{
    /// <summary>
    /// The camel, horse and falcon. Horse and alpaca come from Quaternius' Ultimate Animated Animal pack and the
    /// eagle from his Animal Pack Vol.2 (all CC0). There is no free low-poly camel, so the camel is the alpaca
    /// with a hump, a shadad saddle and a Sadu blanket built on top.
    /// </summary>
    internal static partial class AronArt
    {
        public const string AnimalsDir = ArtDir + "/Animals/";
        private const string HorseFbx = AnimalsDir + "Quaternius/Horse.fbx";
        private const string AlpacaFbx = AnimalsDir + "Quaternius/Alpaca.fbx";
        private const string EagleFbx = AnimalsDir + "QuaterniusVol2/Eagle.fbx";

        private static readonly Color Gold = new Color(0.95f, 0.75f, 0.25f);

        public static GameObject BuildCamelPrefab()
        {
            var root = AnimalRoot(AlpacaFbx, "Camel", 2.05f, out var model);
            if (root == null) return null;
            // Main goes first: its filter also catches Main_Light and Main_Dark, which are then tinted again.
            Tint(model.gameObject, new Color(0.76f, 0.6f, 0.38f), "Main", "Camel_Main");
            Tint(model.gameObject, new Color(0.82f, 0.66f, 0.43f), "Main_Light", "Camel_Main_Light");
            Tint(model.gameObject, new Color(0.63f, 0.47f, 0.29f), "Main_Dark", "Camel_Main_Dark");
            Tint(model.gameObject, new Color(0.55f, 0.42f, 0.3f), "Muzzle", "Camel_Muzzle");
            Tint(model.gameObject, new Color(0.25f, 0.2f, 0.17f), "Hooves", "Camel_Hooves");
            var coat = ModelMaterial(model, "_Main") ?? SaveMaterial("CamelCoat", LitShader, new Color(0.76f, 0.6f, 0.38f));

            var torso = Bone(model, "Torso") ?? model;
            Vector3 at = torso.position;
            float back = TopNear(model, at, 0.2f);
            Vector3 humpC = new Vector3(at.x, back + 0.08f, at.z);
            Piece(PrimitiveType.Sphere, "Hump", torso, humpC, Quaternion.identity, new Vector3(0.56f, 0.5f, 0.85f), coat);

            var blanket = SaveMaterial("CamelBlanket", LitShader, new Color(0.63f, 0.15f, 0.17f));
            var wood = SaveMaterial("SaddleWood", LitShader, new Color(0.42f, 0.27f, 0.15f));
            Piece(PrimitiveType.Sphere, "Saddle_Blanket", torso, humpC + Vector3.up * 0.13f, Quaternion.identity, new Vector3(0.62f, 0.3f, 0.72f), blanket);
            float top = humpC.y + 0.28f;
            foreach (float z in new[] { -0.26f, 0.26f })
                Piece(PrimitiveType.Cylinder, "Saddle_Wood", torso, new Vector3(at.x, top + 0.05f, at.z + z), Quaternion.identity, new Vector3(0.07f, 0.12f, 0.07f), wood);
            Piece(PrimitiveType.Cube, "Saddle_Wood", torso, new Vector3(at.x, top + 0.16f, at.z), Quaternion.identity, new Vector3(0.06f, 0.05f, 0.6f), wood);

            // Golden camel: a tasselled halter on the neck.
            var neck = Bone(model, "Neck2") ?? torso;
            var deco = Group(neck, "Deco_gold");
            var gold = SaveMaterial("AnimalGold", LitShader, Gold);
            gold.SetFloat("_Smoothness", 0.8f);
            var red = SaveMaterial("TasselRed", LitShader, new Color(0.7f, 0.1f, 0.12f));
            Piece(PrimitiveType.Cylinder, "Band", deco, neck.position, Quaternion.identity, new Vector3(0.34f, 0.04f, 0.34f), gold);
            for (int i = 0; i < 4; i++)
            {
                float a = (i - 1.5f) * 0.09f;
                Piece(PrimitiveType.Sphere, "Tassel", deco, neck.position + new Vector3(a, -0.14f, 0.12f), Quaternion.identity, Vector3.one * 0.07f, i % 2 == 0 ? red : gold);
            }

            var seat = Seat(torso, new Vector3(at.x, top + 0.1f, at.z));
            return FinishMount(root, model, MountKind.Camel, "camel", seat, AlpacaFbx);
        }

        public static GameObject BuildHorsePrefab()
        {
            var root = AnimalRoot(HorseFbx, "Horse", 1.75f, out var model);
            if (root == null) return null;
            Tint(model.gameObject, new Color(0.54f, 0.35f, 0.2f), "Main", "Horse_Main");
            Tint(model.gameObject, new Color(0.62f, 0.4f, 0.24f), "Main_Light", "Horse_Main_Light");
            Tint(model.gameObject, new Color(0.42f, 0.26f, 0.15f), "Main_Dark", "Horse_Main_Dark");
            Tint(model.gameObject, new Color(0.18f, 0.12f, 0.08f), "Hair", "Horse_Hair");

            var torso = Bone(model, "Torso") ?? model;
            var torso2 = Bone(model, "Torso2") ?? torso;
            Vector3 at = Vector3.Lerp(torso.position, torso2.position, 0.5f);
            float back = TopNear(model, at, 0.16f);
            var blanket = SaveMaterial("HorseBlanket", LitShader, new Color(0.35f, 0.23f, 0.14f));
            var leather = SaveMaterial("SaddleLeather", LitShader, new Color(0.24f, 0.14f, 0.08f));
            Vector3 c = new Vector3(at.x, back, at.z);
            Piece(PrimitiveType.Sphere, "Saddle_Blanket", torso, c - Vector3.up * 0.05f, Quaternion.identity, new Vector3(0.6f, 0.2f, 0.62f), blanket);
            Piece(PrimitiveType.Sphere, "Saddle_Leather", torso, c + Vector3.up * 0.05f, Quaternion.identity, new Vector3(0.42f, 0.12f, 0.44f), leather);
            Piece(PrimitiveType.Cylinder, "Saddle_Leather", torso, c + new Vector3(0f, 0.11f, 0.18f), Quaternion.identity, new Vector3(0.07f, 0.06f, 0.07f), leather);
            foreach (float x in new[] { -0.3f, 0.3f })
                Piece(PrimitiveType.Cylinder, "Saddle_Leather", torso, c + new Vector3(x, -0.3f, 0.05f), Quaternion.identity, new Vector3(0.025f, 0.3f, 0.025f), leather);

            var head = Bone(model, "Head") ?? torso;
            var gold = SaveMaterial("AnimalGold", LitShader, Gold);
            gold.SetFloat("_Smoothness", 0.8f);
            var bridle = Group(head, "Deco_gold_saddle");
            Piece(PrimitiveType.Cylinder, "Bridle", bridle, head.position, Quaternion.Euler(90f, 0f, 0f), new Vector3(0.26f, 0.02f, 0.26f), gold);

            // The Saudi parade tack: green and red tassels down the neck, a breastplate and a fringed blanket.
            var royal = Group(torso, "Deco_royal");
            var green = SaveMaterial("TasselGreen", LitShader, new Color(0.04f, 0.45f, 0.24f));
            var red = SaveMaterial("TasselRed", LitShader, new Color(0.7f, 0.1f, 0.12f));
            int k = 0;
            foreach (var n in new[] { "Neck1", "Neck2", "Neck3" })
            {
                var b = Bone(model, n);
                if (b == null) continue;
                var g = Group(b, "Deco_royal");
                Piece(PrimitiveType.Sphere, "Tassel", g, b.position + new Vector3(0.18f, -0.08f, 0f), Quaternion.identity, Vector3.one * 0.08f, k % 2 == 0 ? red : green);
                Piece(PrimitiveType.Sphere, "Tassel", g, b.position + new Vector3(-0.18f, -0.08f, 0f), Quaternion.identity, Vector3.one * 0.08f, k % 2 == 0 ? green : red);
                k++;
            }
            var chest = Bone(model, "Torso3") ?? torso;
            Piece(PrimitiveType.Cylinder, "Breastplate", royal, chest.position, Quaternion.Euler(90f, 0f, 0f), new Vector3(0.62f, 0.03f, 0.62f), gold);
            for (int i = 0; i < 7; i++)
            {
                float z = (i - 3f) * 0.09f;
                foreach (float x in new[] { -0.31f, 0.31f })
                    Piece(PrimitiveType.Cube, "Fringe", royal, c + new Vector3(x, -0.16f, z), Quaternion.identity, new Vector3(0.02f, 0.14f, 0.03f), i % 2 == 0 ? gold : red);
            }

            var seat = Seat(torso, c + Vector3.up * 0.12f);
            return FinishMount(root, model, MountKind.Horse, "horse", seat, HorseFbx);
        }

        public static GameObject BuildFalconPrefab()
        {
            var root = AnimalRoot(EagleFbx, "Falcon", 0f, out var model, "Beak");
            if (root == null) return null;
            // A falcon, not an eagle: about 0.9 m across the wings.
            float span = Bounds(model.gameObject).size.x;
            if (span > 0.01f) model.localScale *= 0.9f / span;
            Ground(root.transform, model);
            MaterialCentre(model, "Head", out var headC);
            Tint(model.gameObject, new Color(0.36f, 0.39f, 0.44f), "Wings", "Falcon_Wings");
            Tint(model.gameObject, new Color(0.91f, 0.88f, 0.82f), "Head", "Falcon_Head");
            Tint(model.gameObject, new Color(0.9f, 0.7f, 0.2f), "Beak", "Falcon_Beak");
            Tint(model.gameObject, new Color(0.95f, 0.8f, 0.25f), "Claws", "Falcon_Claws");

            // Leather hood for the gold-hood look, on whichever bone sits nearest the head.
            var anchor = model.GetComponentsInChildren<Transform>().Where(t => t != model)
                              .OrderBy(t => (t.position - headC).sqrMagnitude).FirstOrDefault() ?? model;
            var hood = Group(anchor, "Deco_hood");
            var gold = SaveMaterial("AnimalGold", LitShader, Gold);
            var leather = SaveMaterial("HoodLeather", LitShader, new Color(0.35f, 0.2f, 0.1f));
            Piece(PrimitiveType.Sphere, "Hood", hood, headC + Vector3.up * 0.01f, Quaternion.identity, new Vector3(0.085f, 0.075f, 0.095f), gold);
            Piece(PrimitiveType.Cylinder, "Plume", hood, headC + Vector3.up * 0.06f, Quaternion.identity, new Vector3(0.015f, 0.025f, 0.015f), leather);

            var ctrl = FalconController(GeneratedDir + "/Characters/Falcon.controller");
            var animator = SetupAnimator(model, ctrl);
            var fc = new SerializedObject(root.AddComponent<FalconCompanion>());
            fc.FindProperty("animator").objectReferenceValue = animator;
            fc.ApplyModifiedPropertiesWithoutUndo();
            Slot(root, "falcon");
            return Save(root);
        }

        /// <summary>
        /// Arabian oryx for Noura, from the horse: smaller, white with dark legs and face, and the long straight horns.
        /// The horse's headbutt is its horn charge.
        /// </summary>
        public static GameObject BuildOryxPrefab()
        {
            var root = AnimalRoot(HorseFbx, "Oryx", 1.15f, out var model);
            if (root == null) return null;
            Tint(model.gameObject, new Color(0.95f, 0.93f, 0.88f), "Main", "Oryx_Main");
            Tint(model.gameObject, new Color(0.98f, 0.97f, 0.94f), "Main_Light", "Oryx_Main_Light");
            Tint(model.gameObject, new Color(0.3f, 0.24f, 0.2f), "Main_Dark", "Oryx_Main_Dark");
            Tint(model.gameObject, new Color(0.22f, 0.18f, 0.15f), "Hair", "Oryx_Hair");
            Tint(model.gameObject, new Color(0.12f, 0.1f, 0.09f), "Hooves", "Oryx_Hooves");

            var head = Bone(model, "Head") ?? model;
            var horn = SaveMaterial("OryxHorn", LitShader, new Color(0.16f, 0.13f, 0.11f));
            var horns = Group(head, "Horns");
            Vector3 back = Vector3.back, up = Vector3.up;   // the root faces +Z
            foreach (float x in new[] { -0.05f, 0.05f })
            {
                Vector3 baseP = head.position + up * 0.12f + Vector3.right * x;
                Vector3 dir = (back * 0.85f + up * 0.55f).normalized;
                Piece(PrimitiveType.Cylinder, "Horn", horns, baseP + dir * 0.28f, Quaternion.FromToRotation(Vector3.up, dir), new Vector3(0.025f, 0.28f, 0.025f), horn);
            }

            var ctrl = MountController(HorseFbx, GeneratedDir + "/Characters/Oryx.controller");
            var animator = SetupAnimator(model, ctrl);
            var oc = new SerializedObject(root.AddComponent<OryxCompanion>());
            oc.FindProperty("animator").objectReferenceValue = animator;
            oc.ApplyModifiedPropertiesWithoutUndo();
            return Save(root);
        }

        // ------------------------------------------------------------------ shared

        private static GameObject AnimalRoot(string fbx, string name, float height, out Transform model, string frontMaterial = null)
        {
            model = null;
            var asset = AssetDatabase.LoadAssetAtPath<GameObject>(fbx);
            if (asset == null)
            {
                Debug.LogWarning("[Aron] Missing animal model " + fbx);
                return null;
            }
            EnsureFolder(GeneratedDir + "/Characters");
            var root = new GameObject(name);
            var go = (GameObject)PrefabUtility.InstantiatePrefab(asset, root.transform);
            PrefabUtility.UnpackPrefabInstance(go, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
            model = go.transform;
            model.localPosition = Vector3.zero;

            // Turn it so it faces +Z like the player does.
            Vector3 front;
            var head = Bone(model, "Head");
            if (frontMaterial != null && MaterialCentre(model, frontMaterial, out var m)) front = m;
            else if (head != null) front = head.position;
            else front = Vector3.forward;
            Vector3 d = front - Bounds(go).center;
            d.y = 0f;
            if (d.sqrMagnitude > 1e-6f)
            {
                float yaw = Vector3.SignedAngle(d, Vector3.forward, Vector3.up);
                yaw = Mathf.Round(yaw / 90f) * 90f;
                model.rotation = Quaternion.Euler(0f, yaw, 0f) * model.rotation;
            }

            if (height > 0f)
            {
                float h = Bounds(go).size.y;
                if (h > 0.01f && h < 5000f) model.localScale *= height / h;
            }
            Ground(root.transform, model);
            FixMaterials(go);
            return root;
        }

        private static void Ground(Transform root, Transform model)
        {
            var b = Bounds(model.gameObject);
            model.position += new Vector3(root.position.x - b.center.x, root.position.y - b.min.y, root.position.z - b.center.z);
        }

        private static GameObject FinishMount(GameObject root, Transform model, MountKind kind, string slot, Transform seat, string fbx)
        {
            var ctrl = MountController(fbx, GeneratedDir + "/Characters/" + root.name + ".controller");
            var animator = SetupAnimator(model, ctrl);
            var b = Bounds(model.gameObject);
            var box = root.AddComponent<BoxCollider>();
            box.isTrigger = true;
            box.center = root.transform.InverseTransformPoint(b.center);
            box.size = new Vector3(Mathf.Max(b.size.x, 1f), b.size.y, Mathf.Max(b.size.z, 1f));
            var rb = root.AddComponent<Rigidbody>();
            rb.isKinematic = true;
            rb.useGravity = false;
            var mount = new SerializedObject(root.AddComponent<MountAnimal>());
            mount.FindProperty("kind").enumValueIndex = (int)kind;
            mount.FindProperty("seat").objectReferenceValue = seat;
            mount.FindProperty("animator").objectReferenceValue = animator;
            mount.FindProperty("pickup").objectReferenceValue = box;
            mount.ApplyModifiedPropertiesWithoutUndo();
            Slot(root, slot);
            return Save(root);
        }

        private static Animator SetupAnimator(Transform model, AnimatorController ctrl)
        {
            var animator = model.GetComponent<Animator>();
            if (animator == null) animator = model.gameObject.AddComponent<Animator>();
            animator.runtimeAnimatorController = ctrl;
            animator.applyRootMotion = false;
            animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
            return animator;
        }

        private static void Slot(GameObject root, string slot)
        {
            var so = new SerializedObject(root.AddComponent<AnimalCosmetics>());
            so.FindProperty("slot").stringValue = slot;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static GameObject Save(GameObject root)
        {
            string path = GeneratedDir + "/Characters/" + root.name + ".prefab";
            var prefab = PrefabUtility.SaveAsPrefabAsset(root, path);
            Object.DestroyImmediate(root);
            return prefab;
        }

        private static Transform Group(Transform parent, string name)
        {
            var t = new GameObject(name).transform;
            t.SetParent(parent, false);
            return t;
        }

        private static Transform Seat(Transform parent, Vector3 world)
        {
            var t = new GameObject("Seat").transform;
            t.SetParent(parent, true);
            t.position = world;
            return t;
        }

        private static Material ModelMaterial(Transform model, string suffix)
        {
            foreach (var r in model.GetComponentsInChildren<SkinnedMeshRenderer>())
                foreach (var m in r.sharedMaterials)
                    if (m != null && m.name.EndsWith(suffix)) return m;
            return null;
        }

        /// <summary>World-space vertices of the posed skinned meshes, optionally only one material's.</summary>
        private static IEnumerable<Vector3> Vertices(Transform model, string material = null)
        {
            foreach (var smr in model.GetComponentsInChildren<SkinnedMeshRenderer>())
            {
                var baked = new Mesh();
                smr.BakeMesh(baked, true);
                var v = baked.vertices;
                var mats = smr.sharedMaterials;
                var t = smr.transform;
                // Baked with scale, so only position and rotation are left to apply.
                var m = Matrix4x4.TRS(t.position, t.rotation, Vector3.one);
                if (material == null)
                    foreach (var p in v) yield return m.MultiplyPoint3x4(p);
                else
                    for (int s = 0; s < baked.subMeshCount && s < mats.Length; s++)
                    {
                        if (mats[s] == null || mats[s].name.IndexOf(material, System.StringComparison.OrdinalIgnoreCase) < 0) continue;
                        foreach (int i in baked.GetIndices(s)) yield return m.MultiplyPoint3x4(v[i]);
                    }
                Object.DestroyImmediate(baked);
            }
        }

        private static bool MaterialCentre(Transform model, string material, out Vector3 centre)
        {
            Vector3 sum = Vector3.zero;
            int n = 0;
            foreach (var p in Vertices(model, material)) { sum += p; n++; }
            centre = n > 0 ? sum / n : Vector3.zero;
            return n > 0;
        }

        /// <summary>Highest point of the mesh within <paramref name="radius"/> of a spot, seen from above.</summary>
        private static float TopNear(Transform model, Vector3 at, float radius)
        {
            float best = float.NegativeInfinity;
            foreach (var p in Vertices(model))
            {
                float dx = p.x - at.x, dz = p.z - at.z;
                if (dx * dx + dz * dz <= radius * radius && p.y > best) best = p.y;
            }
            return float.IsNegativeInfinity(best) ? at.y : best;
        }

        private static AnimatorController MountController(string fbx, string path)
        {
            AssetDatabase.DeleteAsset(path);
            var ctrl = AnimatorController.CreateAnimatorControllerAtPath(path);
            var all = AssetDatabase.LoadAllAssetsAtPath(fbx);
            ctrl.AddParameter("Speed", AnimatorControllerParameterType.Float);
            ctrl.AddParameter("Grounded", AnimatorControllerParameterType.Bool);
            foreach (var t in new[] { "Jump", "Hit", "Attack" }) ctrl.AddParameter(t, AnimatorControllerParameterType.Trigger);
            ctrl.parameters = ctrl.parameters.Select(p => { if (p.name == "Grounded") p.defaultBool = true; return p; }).ToArray();

            var sm = ctrl.layers[0].stateMachine;
            var loco = ctrl.CreateBlendTreeInController("Locomotion", out var tree, 0);
            tree.blendParameter = "Speed";
            tree.useAutomaticThresholds = false;
            var idle = Clip(all, "Idle");
            var walk = Clip(all, "Walk");
            var gallop = Clip(all, "Gallop");
            if (idle != null) tree.AddChild(idle, 0f);
            if (walk != null) tree.AddChild(walk, 0.3f);
            if (gallop != null) tree.AddChild(gallop, 1f);
            sm.defaultState = loco;

            AnyTrigger(sm, OneShot(sm, "Jump", Clip(all, "Gallop_Jump"), loco, 0.85f), "Jump", true);
            AnyTrigger(sm, OneShot(sm, "Hit", Clip(all, "Idle_HitReact_Left"), loco, 0.9f), "Hit", false);
            AnyTrigger(sm, OneShot(sm, "Attack", Clip(all, "Attack_Headbutt"), loco, 0.8f), "Attack", false);
            EditorUtility.SetDirty(ctrl);
            return ctrl;
        }

        private static AnimatorController FalconController(string path)
        {
            AssetDatabase.DeleteAsset(path);
            var ctrl = AnimatorController.CreateAnimatorControllerAtPath(path);
            var all = AssetDatabase.LoadAllAssetsAtPath(EagleFbx);
            ctrl.AddParameter("Perched", AnimatorControllerParameterType.Bool);
            var sm = ctrl.layers[0].stateMachine;
            var fly = sm.AddState("Flying");
            fly.motion = Clip(all, "Flying");
            sm.defaultState = fly;
            var sit = sm.AddState("Perched");
            sit.motion = Clip(all, "Idle");
            var down = fly.AddTransition(sit);
            down.AddCondition(AnimatorConditionMode.If, 0f, "Perched");
            down.duration = 0.2f;
            var up = sit.AddTransition(fly);
            up.AddCondition(AnimatorConditionMode.IfNot, 0f, "Perched");
            up.duration = 0.1f;
            EditorUtility.SetDirty(ctrl);
            return ctrl;
        }
    }
}
