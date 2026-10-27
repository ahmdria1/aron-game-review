// (c) Ahmed Alamrani. All rights reserved.
using System.Collections.Generic;
using System.Linq;
using Aron.Characters;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
using UnityEngine.Rendering;

namespace Aron.EditorTools
{
    /// <summary>
    /// Model placement, material tinting, character prefabs and particle templates used by
    /// <see cref="AronLevelBuilder"/>. Models live in Assets/_Aron/Art (Kenney and Quaternius, CC0).
    /// </summary>
    internal static partial class AronArt
    {
        public const string ArtDir = "Assets/_Aron/Art";
        public const string Platformer = ArtDir + "/Kenney/Platformer/";
        public const string Castle = ArtDir + "/Kenney/Castle/";
        public const string Nature = ArtDir + "/Kenney/Nature/";
        public const string Characters = ArtDir + "/Characters/Quaternius/";
        public const string GeneratedDir = "Assets/_Aron/Generated";

        // Heights of the source meshes in metres, used to undo any unit scaling done by the importer.
        private static readonly Dictionary<string, float> NativeHeight = new Dictionary<string, float>
        {
            {"barrel", 0.48f}, {"block-grass-large", 1f}, {"block-grass-low-large", 0.5f}, {"chest", 0.3f},
            {"coin-gold", 0.4f}, {"crate", 0.5f}, {"crate-item", 0.5f}, {"crate-strong", 0.6f},
            {"fence-low-straight", 0.3f}, {"fence-straight", 0.4f}, {"flag", 0.9f}, {"platform", 0.2f},
            {"rocks", 0.4f}, {"sign", 0.6f}, {"spring", 0.33f}, {"stones", 0.05f},
            {"flag-banner-long", 2.17f}, {"gate", 0.91f}, {"stairs-stone", 0.67f}, {"tower-square-arch", 1.01f},
            {"tower-square-base", 1.01f}, {"tower-square-mid", 1.01f}, {"tower-square-mid-door", 1.01f},
            {"tower-square-mid-windows", 1.01f}, {"tower-square-top", 0.3f}, {"tower-square-top-color", 0.38f},
            {"wall", 1.31f}, {"wall-corner", 1.31f}, {"wall-doorway", 1.31f}, {"wall-half", 1.31f}, {"wall-pillar", 1.31f},
            {"cactus_short", 0.53f}, {"cactus_tall", 0.75f}, {"plant_bushSmall", 0.21f}, {"rock_largeA", 0.26f},
            {"rock_largeC", 0.32f}, {"rock_smallA", 0.19f}, {"rock_smallC", 0.12f}, {"rock_tallA", 1f},
            {"stone_tallB", 0.88f}, {"tree_palmBend", 1.38f}, {"tree_palmDetailedTall", 1.25f},
            {"tree_palmShort", 1.06f}, {"tree_palmTall", 1.36f},
        };

        private static readonly Dictionary<string, float> unitFix = new Dictionary<string, float>();
        private static readonly Dictionary<string, Material> tinted = new Dictionary<string, Material>();
        private static readonly Dictionary<Material, Material> converted = new Dictionary<Material, Material>();

        public static void Reset()
        {
            unitFix.Clear();
            tinted.Clear();
            converted.Clear();
        }

        private static bool UsingURP => GraphicsSettings.defaultRenderPipeline != null;

        public static Shader LitShader => (UsingURP ? Shader.Find("Universal Render Pipeline/Lit") : null) ?? Shader.Find("Standard");

        private static Shader ParticleShader => (UsingURP ? Shader.Find("Universal Render Pipeline/Particles/Unlit") : null)
                                                ?? Shader.Find("Particles/Standard Unlit") ?? LitShader;

        public static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;
            int slash = path.LastIndexOf('/');
            EnsureFolder(path.Substring(0, slash));
            AssetDatabase.CreateFolder(path.Substring(0, slash), path.Substring(slash + 1));
        }

        // ------------------------------------------------------------------ import settings
        public static void ConfigureImporters()
        {
            foreach (var guid in AssetDatabase.FindAssets("t:Model", new[] { ArtDir }))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                if (!(AssetImporter.GetAtPath(path) is ModelImporter mi)) continue;
                bool character = path.StartsWith(Characters);
                bool animal = path.StartsWith(AnimalsDir);
                bool dirty = false;
                if (character || animal)
                {
                    if (mi.animationType != ModelImporterAnimationType.Generic) { mi.animationType = ModelImporterAnimationType.Generic; dirty = true; }
                    if (!mi.importAnimation) { mi.importAnimation = true; dirty = true; }
                    var clips = mi.clipAnimations;
                    if (clips == null || clips.Length == 0)
                    {
                        var seen = new HashSet<string>();
                        var kept = new List<ModelImporterClipAnimation>();
                        foreach (var c in mi.defaultClipAnimations)
                        {
                            c.name = ShortClipName(c.name);
                            // The eagle ships its idle twice, once on the mesh; keep the armature one.
                            if (!seen.Add(c.name)) continue;
                            c.loopTime = LoopedClips.Contains(c.name);
                            kept.Add(c);
                        }
                        mi.clipAnimations = kept.ToArray();
                        dirty = true;
                    }
                }
                else
                {
                    if (mi.animationType != ModelImporterAnimationType.None) { mi.animationType = ModelImporterAnimationType.None; dirty = true; }
                    if (mi.importAnimation) { mi.importAnimation = false; dirty = true; }
                }
                if (mi.importCameras || mi.importLights) { mi.importCameras = false; mi.importLights = false; dirty = true; }
                if (dirty) mi.SaveAndReimport();
            }
        }

        private static readonly HashSet<string> LoopedClips = new HashSet<string>
        {
            "Idle", "Run", "Walk", "Gallop", "Flying", "Eating", "Idle_2", "Idle_Headlow",
        };

        private static string ShortClipName(string n)
        {
            int bar = n.LastIndexOf('|');
            return bar >= 0 ? n.Substring(bar + 1) : n;
        }

        // ------------------------------------------------------------------ placement
        /// <summary>
        /// Places a model under a holder object. The holder carries position, yaw and scale so the model's own
        /// import transform (axis conversion) stays untouched. Scale is in metres-per-native-unit.
        /// </summary>
        public static GameObject Place(string path, Transform parent, Vector3 pos, float yaw, Vector3 scale, string name = null)
        {
            var asset = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            var holder = new GameObject(name ?? System.IO.Path.GetFileNameWithoutExtension(path));
            holder.transform.SetParent(parent, false);
            holder.transform.localPosition = pos;
            holder.transform.localRotation = Quaternion.Euler(0f, yaw, 0f);
            if (asset == null)
            {
                Debug.LogWarning("[Aron] Missing model " + path);
                return holder;
            }
            var model = (GameObject)PrefabUtility.InstantiatePrefab(asset, holder.transform);
            model.transform.localPosition = Vector3.zero;
            holder.transform.localScale = scale * UnitFix(path, asset);
            FixMaterials(holder);
            return holder;
        }

        public static GameObject Place(string path, Transform parent, Vector3 pos, float yaw = 0f, float scale = 1f, string name = null)
            => Place(path, parent, pos, yaw, Vector3.one * scale, name);

        private static float UnitFix(string path, GameObject asset)
        {
            if (unitFix.TryGetValue(path, out var f)) return f;
            f = 1f;
            string key = System.IO.Path.GetFileNameWithoutExtension(path);
            if (NativeHeight.TryGetValue(key, out var native))
            {
                var probe = (GameObject)PrefabUtility.InstantiatePrefab(asset);
                var b = Bounds(probe);
                Object.DestroyImmediate(probe);
                if (b.size.y > 0.0001f)
                {
                    float ratio = native / b.size.y;
                    // Only correct real unit mismatches (cm vs m), not small modelling differences.
                    if (ratio > 1.5f || ratio < 0.67f) f = ratio;
                }
            }
            unitFix[path] = f;
            return f;
        }

        public static Bounds Bounds(GameObject go)
        {
            var rs = go.GetComponentsInChildren<Renderer>();
            if (rs.Length == 0) return new Bounds(go.transform.position, Vector3.zero);
            var b = rs[0].bounds;
            foreach (var r in rs) b.Encapsulate(r.bounds);
            return b;
        }

        /// <summary>Replaces non-pipeline materials (e.g. Standard on URP) with Lit copies keeping colour and texture.</summary>
        public static void FixMaterials(GameObject root)
        {
            if (!UsingURP) return;
            foreach (var r in root.GetComponentsInChildren<Renderer>())
            {
                var mats = r.sharedMaterials;
                bool changed = false;
                for (int i = 0; i < mats.Length; i++)
                {
                    var m = mats[i];
                    if (m == null || m.shader == null || m.shader.name.StartsWith("Universal Render Pipeline")) continue;
                    if (!converted.TryGetValue(m, out var lit))
                    {
                        lit = SaveMaterial("Converted_" + m.name, LitShader, m.HasProperty("_Color") ? m.color : Color.white,
                                           m.HasProperty("_MainTex") ? m.mainTexture : null);
                        converted[m] = lit;
                    }
                    mats[i] = lit;
                    changed = true;
                }
                if (changed) r.sharedMaterials = mats;
            }
        }

        public static Material SaveMaterial(string name, Shader shader, Color color, Texture tex = null)
        {
            EnsureFolder(GeneratedDir + "/Materials");
            string path = GeneratedDir + "/Materials/" + name + ".mat";
            var m = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (m == null) { m = new Material(shader); AssetDatabase.CreateAsset(m, path); }
            else if (m.shader != shader) m.shader = shader;
            m.color = color;
            if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", color);
            if (tex != null)
            {
                m.mainTexture = tex;
                if (m.HasProperty("_BaseMap")) m.SetTexture("_BaseMap", tex);
            }
            if (m.HasProperty("_Smoothness")) m.SetFloat("_Smoothness", 0.15f);
            if (m.HasProperty("_Glossiness")) m.SetFloat("_Glossiness", 0.15f);
            EditorUtility.SetDirty(m);
            return m;
        }

        /// <summary>
        /// Multiplies every material on the object by a colour. When <paramref name="onlyNamed"/> is set, only
        /// materials whose name contains that text are changed (e.g. "Clothes" on a character).
        /// </summary>
        public static void Tint(GameObject root, Color color, string onlyNamed = null, string tag = null)
        {
            foreach (var r in root.GetComponentsInChildren<Renderer>())
            {
                var mats = r.sharedMaterials;
                for (int i = 0; i < mats.Length; i++)
                {
                    var src = mats[i];
                    if (src == null) continue;
                    string baseName = src.name.Replace("Tinted_", "");
                    if (onlyNamed != null && baseName.IndexOf(onlyNamed, System.StringComparison.OrdinalIgnoreCase) < 0) continue;
                    string key = (tag ?? ColorUtility.ToHtmlStringRGB(color)) + "_" + baseName;
                    if (!tinted.TryGetValue(key, out var m))
                    {
                        Texture tex = src.HasProperty("_BaseMap") ? src.GetTexture("_BaseMap") : src.HasProperty("_MainTex") ? src.mainTexture : null;
                        Color baseColor = onlyNamed != null ? Color.white : src.HasProperty("_BaseColor") ? src.GetColor("_BaseColor") : src.color;
                        m = SaveMaterial("Tinted_" + key, LitShader, baseColor * color, tex);
                        tinted[key] = m;
                    }
                    mats[i] = m;
                }
                r.sharedMaterials = mats;
            }
        }

        // ------------------------------------------------------------------ characters
        public class CharacterLook
        {
            public string prefabName;
            public string fbx;
            public float height = 1.8f;
            public (string material, Color color)[] tints = System.Array.Empty<(string, Color)>();
            public bool shemagh;
            public Color shemaghColor = new Color(0.8f, 0.08f, 0.1f);
            /// <summary>Plain dark head scarf over the hair, no agal (abaya look).</summary>
            public bool hijab;
            public Color hijabColor = new Color(0.07f, 0.07f, 0.08f);
            /// <summary>Used where the model's own skin material is close to black.</summary>
            public Color skinColor = new Color(0.8f, 0.56f, 0.42f);
            public string weapon;
            public Color weaponColor = Color.black;
            public string id;
            public bool mouth = true;
            public float faceForward;
            public Color browColor = new Color(0.12f, 0.08f, 0.06f);
            public bool thobe;
            public Color thobeColor = new Color(0.97f, 0.97f, 0.95f);
            /// <summary>Short trimmed beard and moustache along the jaw.</summary>
            public bool beard;
            public Color beardColor = new Color(0.05f, 0.04f, 0.04f);
        }

        public static GameObject BuildCharacterPrefab(CharacterLook look, out AnimatorController controller)
        {
            EnsureFolder(GeneratedDir + "/Characters");
            controller = BuildController(look.fbx, GeneratedDir + "/Characters/" + look.prefabName + ".controller");

            var root = new GameObject(look.prefabName);
            var asset = AssetDatabase.LoadAssetAtPath<GameObject>(look.fbx);
            if (asset == null)
            {
                Debug.LogWarning("[Aron] Missing character model " + look.fbx);
                Object.DestroyImmediate(root);
                return null;
            }
            var model = (GameObject)PrefabUtility.InstantiatePrefab(asset, root.transform);
            model.transform.localPosition = Vector3.zero;
            PrefabUtility.UnpackPrefabInstance(model, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);

            float h = Bounds(model).size.y;
            if (h > 0.05f && h < 5000f) model.transform.localScale *= look.height / h;
            var b = Bounds(model);
            model.transform.position += Vector3.up * (root.transform.position.y - b.min.y);

            var rig = AddFace(model.transform, look);
            FixMaterials(model);
            if (DarkSkin(model)) Tint(model, look.skinColor, "Skin", look.prefabName + "_Skin");
            if (look.shemagh) HideParts(model, "Band", look.prefabName);
            if (look.hijab) { HideParts(model, "Hair", look.prefabName); HideParts(model, "Band", look.prefabName); }
            foreach (var (mat, color) in look.tints) Tint(model, color, mat, look.prefabName + "_" + mat);

            var animator = model.GetComponent<Animator>();
            if (animator == null) animator = model.AddComponent<Animator>();
            animator.runtimeAnimatorController = controller;
            animator.applyRootMotion = false;
            animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;

            if (look.shemagh) AddShemagh(model.transform, look.shemaghColor, look.height);
            if (look.hijab) AddHijab(model.transform, look.hijabColor, look.height);
            if (!string.IsNullOrEmpty(look.weapon)) AddWeapon(model.transform, look.weapon, look.weaponColor, look.height);

            if (look.thobe) AddThobe(root.transform, model.transform, rig, look);

            CharacterFace face = null;
            if (rig != null)
            {
                face = root.AddComponent<CharacterFace>();
                face.Assign(rig);
            }
            var feel = new SerializedObject(root.AddComponent<CharacterEmotion>());
            feel.FindProperty("characterId").stringValue = look.id ?? "";
            feel.FindProperty("face").objectReferenceValue = face;
            feel.ApplyModifiedPropertiesWithoutUndo();

            string path = GeneratedDir + "/Characters/" + look.prefabName + ".prefab";
            var prefab = PrefabUtility.SaveAsPrefabAsset(root, path);
            Object.DestroyImmediate(root);
            return prefab;
        }

        private static Transform Bone(Transform root, string name)
        {
            foreach (var t in root.GetComponentsInChildren<Transform>(true)) if (t.name == name) return t;
            return null;
        }

        private static GameObject Piece(PrimitiveType type, string name, Transform parent, Vector3 worldPos, Quaternion worldRot, Vector3 worldSize, Material mat)
        {
            var go = GameObject.CreatePrimitive(type);
            go.name = name;
            Object.DestroyImmediate(go.GetComponent<Collider>());
            go.GetComponent<Renderer>().sharedMaterial = mat;
            go.transform.SetParent(parent, true);
            go.transform.position = worldPos;
            go.transform.rotation = worldRot;
            Vector3 ps = parent.lossyScale;
            go.transform.localScale = new Vector3(worldSize.x / ps.x, worldSize.y / ps.y, worldSize.z / ps.z);
            return go;
        }

        /// <summary>True when the imported skin material is near black (some packs ship it that way).</summary>
        // Drops the submesh drawn with the named material (the headband under a shemagh, hair under a hijab).
        private static void HideParts(GameObject model, string material, string owner)
        {
            foreach (var r in model.GetComponentsInChildren<SkinnedMeshRenderer>())
            {
                var mats = r.sharedMaterials;
                int index = System.Array.FindIndex(mats, m => m != null && m.name.Contains(material));
                if (index < 0 || r.sharedMesh == null || index >= r.sharedMesh.subMeshCount) continue;
                string dir = GeneratedDir + "/Meshes";
                if (!AssetDatabase.IsValidFolder(dir)) AssetDatabase.CreateFolder(GeneratedDir, "Meshes");
                string path = dir + "/" + owner + "_" + r.name + ".asset";
                var mesh = Object.Instantiate(r.sharedMesh);
                mesh.name = owner + "_" + r.name;
                mesh.SetTriangles(System.Array.Empty<int>(), index);
                AssetDatabase.DeleteAsset(path);
                AssetDatabase.CreateAsset(mesh, path);
                r.sharedMesh = mesh;
            }
        }

        private static bool DarkSkin(GameObject model)
        {
            foreach (var r in model.GetComponentsInChildren<Renderer>())
                foreach (var m in r.sharedMaterials)
                {
                    if (m == null || m.name.IndexOf("Skin", System.StringComparison.OrdinalIgnoreCase) < 0) continue;
                    Color c = m.HasProperty("_BaseColor") ? m.GetColor("_BaseColor") : m.color;
                    if (c.grayscale < 0.1f) return true;
                }
            return false;
        }

        /// <summary>Red (or any colour) on white houndstooth-like checks, the classic shemagh weave.</summary>
        private static Texture2D ShemaghPattern(Color check)
        {
            EnsureFolder(GeneratedDir + "/Textures");
            string path = GeneratedDir + "/Textures/Shemagh_" + ColorUtility.ToHtmlStringRGB(check) + ".png";
            const int n = 64, cell = 4;
            var tex = new Texture2D(n, n, TextureFormat.RGB24, false);
            Color white = new Color(0.97f, 0.96f, 0.94f);
            for (int y = 0; y < n; y++)
                for (int x = 0; x < n; x++)
                {
                    int cx = x / cell, cy = y / cell;
                    bool a = cx % 2 == 0, b = cy % 2 == 0;
                    Color c = a && b ? check : a || b ? Color.Lerp(check, white, 0.55f) : white;
                    tex.SetPixel(x, y, c);
                }
            System.IO.File.WriteAllBytes(path, tex.EncodeToPNG());
            Object.DestroyImmediate(tex);
            AssetDatabase.ImportAsset(path);
            if (AssetImporter.GetAtPath(path) is TextureImporter ti)
            {
                ti.filterMode = FilterMode.Point; ti.mipmapEnabled = true; ti.wrapMode = TextureWrapMode.Repeat;
                ti.SaveAndReimport();
            }
            return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        }

        /// <summary>
        /// Box around the visible head geometry (every drawn triangle above the head bone), in world space.
        /// The chibi heads are far bigger than their head bone, so cloth is fitted to this instead.
        /// </summary>
        private static bool HeadBox(Transform model, Transform head, out Vector3 centre, out Vector3 size)
        {
            centre = head.position; size = Vector3.zero;
            Bounds best = default; float bestOff = float.MaxValue; bool found = false;
            foreach (var smr in model.GetComponentsInChildren<SkinnedMeshRenderer>())
            {
                if (smr.sharedMesh == null) continue;
                var baked = new Mesh();
                smr.BakeMesh(baked, true);
                var verts = baked.vertices;
                var full = smr.transform.localToWorldMatrix;
                var noScale = Matrix4x4.TRS(smr.transform.position, smr.transform.rotation, Vector3.one);
                foreach (var m in new[] { full, noScale })
                {
                    bool any = false; var bb = new Bounds();
                    for (int sub = 0; sub < baked.subMeshCount; sub++)
                        foreach (int idx in baked.GetTriangles(sub))
                        {
                            var p = m.MultiplyPoint3x4(verts[idx]);
                            if (p.y < head.position.y) continue;
                            if (!any) { bb = new Bounds(p, Vector3.zero); any = true; } else bb.Encapsulate(p);
                        }
                    if (!any) continue;
                    var flat = bb.center - head.position; flat.y = 0f;
                    float off = flat.magnitude + Mathf.Abs(bb.min.y - head.position.y);
                    if (off < bestOff) { bestOff = off; best = bb; found = true; }
                }
                Object.DestroyImmediate(baked);
            }
            if (!found || best.size.y < 0.05f) return false;
            centre = best.center; size = best.size;
            return true;
        }

        private static void FallbackHead(Transform model, Transform head, float height, out Vector3 centre, out Vector3 size)
        {
            var top = Bone(model, "Head_end");
            float len = top != null ? Mathf.Max(0.12f, Vector3.Distance(head.position, top.position)) : height * 0.14f;
            centre = head.position + Vector3.up * len * 0.5f; size = new Vector3(len, len, len);
        }

        private static void AddShemagh(Transform model, Color color, float height)
        {
            var head = Bone(model, "Head");
            if (head == null) return;
            if (!HeadBox(model, head, out Vector3 c, out Vector3 s)) FallbackHead(model, head, height, out c, out s);
            Vector3 up = Vector3.up, fwd = model.forward, right = model.right;
            float W = s.x, H = s.y, D = s.z;
            // One material per colour: a shared asset would take the colour of whoever was built last.
            bool plain = color.r > 0.85f && color.g > 0.85f && color.b > 0.85f;
            var cloth = plain
                ? SaveMaterial("Shemagh_" + ColorUtility.ToHtmlStringRGB(color), LitShader, color)
                : SaveMaterial("Shemagh_" + ColorUtility.ToHtmlStringRGB(color), LitShader, Color.white, ShemaghPattern(color));
            if (!plain && cloth.HasProperty("_BaseMap")) cloth.SetTextureScale("_BaseMap", new Vector2(4f, 4f));
            var agal = SaveMaterial("Agal", LitShader, new Color(0.04f, 0.04f, 0.04f));
            var cap = new GameObject("Shemagh").transform;
            cap.SetParent(head, false);
            cap.position = c;
            var look = Quaternion.LookRotation(fwd);
            // The cap covers the crown, forehead and sides and stops just above the brows; the face stays open.
            Piece(PrimitiveType.Sphere, "Cap", cap, c + up * H * 0.1f - fwd * D * 0.1f, look, new Vector3(W * 1.12f, H * 0.95f, D * 1.08f), cloth);
            Piece(PrimitiveType.Cube, "Drape", cap, c - fwd * D * 0.5f - up * H * 0.45f,
                  look * Quaternion.Euler(-8f, 0f, 0f), new Vector3(W * 1.05f, H * 1.1f, D * 0.06f), cloth);
            foreach (float side in new[] { -1f, 1f })
                Piece(PrimitiveType.Cube, side < 0 ? "DrapeL" : "DrapeR", cap, c + right * side * W * 0.55f - fwd * D * 0.12f - up * H * 0.45f,
                      look * Quaternion.Euler(0f, 0f, side * 4f), new Vector3(W * 0.06f, H * 1.05f, D * 0.75f), cloth);
            // Single black agal: a thick ring resting on the top of the cap.
            Piece(PrimitiveType.Cylinder, "AgalRing", cap, c + up * H * 0.47f - fwd * D * 0.06f, Quaternion.identity,
                  new Vector3(W * 0.95f, H * 0.035f, D * 0.95f), agal);
        }

        /// <summary>Scarf that wraps the head and hair, framing the face, and falls over the shoulders.</summary>
        private static void AddHijab(Transform model, Color color, float height)
        {
            var head = Bone(model, "Head");
            if (head == null) return;
            if (!HeadBox(model, head, out Vector3 c, out Vector3 s)) FallbackHead(model, head, height, out c, out s);
            Vector3 up = Vector3.up, fwd = model.forward;
            float W = s.x, H = s.y, D = s.z;
            var cloth = SaveMaterial("Hijab_" + ColorUtility.ToHtmlStringRGB(color), LitShader, color);
            var wrap = new GameObject("Hijab").transform;
            wrap.SetParent(head, false);
            wrap.position = c;
            var rot = Quaternion.LookRotation(fwd);
            // Crown and sides wrap the head just behind the face plane; the chin piece closes it under the face.
            Piece(PrimitiveType.Sphere, "Crown", wrap, c + up * H * 0.08f - fwd * D * 0.1f, rot, new Vector3(W * 1.14f, H * 1.06f, D * 1.08f), cloth);
            Piece(PrimitiveType.Sphere, "Chin", wrap, c - up * H * 0.5f, rot, new Vector3(W * 0.95f, H * 0.32f, D * 0.92f), cloth);
            Piece(PrimitiveType.Sphere, "Back", wrap, c - fwd * D * 0.25f - up * H * 0.55f, rot, new Vector3(W * 1.05f, H * 0.9f, D * 0.8f), cloth);
        }

        /// <summary>
        /// Swaps the painted-on eyes for movable ones: eyes, brows, mouth, cheeks, tears and a sweat drop on a rig
        /// parented to the head bone. The rig is scaled in "eye units" (half the distance between the eyes).
        /// </summary>
        private static FaceRig AddFace(Transform model, CharacterLook look)
        {
            var head = Bone(model, "Head");
            if (head == null) return null;
            Vector3 fwd = model.forward, right = model.right, up = Vector3.up;

            bool measured = MeasureEyes(model, head, look.height, out Vector3 centre, out float u);
            if (!measured)
            {
                var top = Bone(model, "Head_end");
                float len = top != null ? Mathf.Max(0.12f, Vector3.Distance(head.position, top.position)) : look.height * 0.14f;
                u = len * 0.24f;
                centre = head.position + up * len * 0.45f + fwd * len * 0.5f;
            }
            centre += fwd * look.faceForward;

            var anchor = new GameObject("FaceRig").transform;
            anchor.SetParent(head, false);
            anchor.position = centre;
            anchor.rotation = Quaternion.LookRotation(fwd, up);
            Vector3 hs = head.lossyScale;
            anchor.localScale = new Vector3(u / hs.x, u / hs.y, u / hs.z);

            var eyeMat = SaveMaterial("Face_Eye", LitShader, new Color(0.07f, 0.05f, 0.05f));
            var shineMat = SaveMaterial("Face_Shine", LitShader, Color.white);
            var browMat = SaveMaterial("Face_Brow_" + look.prefabName, LitShader, look.browColor);
            var mouthMat = SaveMaterial("Face_Mouth", LitShader, new Color(0.42f, 0.1f, 0.1f));
            var innerMat = SaveMaterial("Face_MouthInner", LitShader, new Color(0.22f, 0.04f, 0.05f));
            var blushMat = SaveMaterial("Face_Blush", LitShader, new Color(1f, 0.45f, 0.5f));
            var tearMat = SaveMaterial("Face_Tear", LitShader, new Color(0.55f, 0.82f, 1f));

            var rig = new FaceRig();
            rig.eyeL = FacePart(PrimitiveType.Sphere, "EyeL", anchor, new Vector3(-1f, 0f, 0.05f), new Vector3(0.62f, 0.85f, 0.25f), eyeMat);
            rig.eyeR = FacePart(PrimitiveType.Sphere, "EyeR", anchor, new Vector3(1f, 0f, 0.05f), new Vector3(0.62f, 0.85f, 0.25f), eyeMat);
            foreach (var eye in new[] { rig.eyeL, rig.eyeR })
                FacePart(PrimitiveType.Sphere, "Shine", eye, new Vector3(0.22f, 0.25f, 0.42f), new Vector3(0.32f, 0.25f, 0.4f), shineMat);
            rig.browL = FacePart(PrimitiveType.Cube, "BrowL", anchor, new Vector3(-1f, 0.85f, 0.06f), new Vector3(0.72f, 0.14f, 0.12f), browMat);
            rig.browR = FacePart(PrimitiveType.Cube, "BrowR", anchor, new Vector3(1f, 0.85f, 0.06f), new Vector3(0.72f, 0.14f, 0.12f), browMat);
            if (look.mouth)
            {
                rig.mouthC = FacePart(PrimitiveType.Cube, "MouthC", anchor, new Vector3(0f, -1.15f, 0.04f), new Vector3(0.32f, 0.11f, 0.1f), mouthMat);
                rig.mouthL = MouthCorner("MouthL", anchor, -1f, mouthMat);
                rig.mouthR = MouthCorner("MouthR", anchor, 1f, mouthMat);
                rig.mouthOpen = FacePart(PrimitiveType.Sphere, "MouthOpen", anchor, new Vector3(0f, -1.22f, 0.02f), new Vector3(0.5f, 0.45f, 0.12f), innerMat);
                rig.blushL = FacePart(PrimitiveType.Sphere, "BlushL", anchor, new Vector3(-1.25f, -0.62f, 0f), new Vector3(0.45f, 0.24f, 0.1f), blushMat);
                rig.blushR = FacePart(PrimitiveType.Sphere, "BlushR", anchor, new Vector3(1.25f, -0.62f, 0f), new Vector3(0.45f, 0.24f, 0.1f), blushMat);
            }
            if (look.beard) AddBeard(anchor, SaveMaterial("Face_Beard_" + look.prefabName, LitShader, look.beardColor));
            rig.tearL = FacePart(PrimitiveType.Sphere, "TearL", anchor, new Vector3(-1f, -0.55f, 0.12f), new Vector3(0.16f, 0.26f, 0.16f), tearMat);
            rig.tearR = FacePart(PrimitiveType.Sphere, "TearR", anchor, new Vector3(1f, -0.55f, 0.12f), new Vector3(0.16f, 0.26f, 0.16f), tearMat);
            rig.sweat = FacePart(PrimitiveType.Sphere, "Sweat", anchor, new Vector3(1.65f, 1f, 0.05f), new Vector3(0.26f, 0.4f, 0.26f), tearMat);
            return rig;
        }

        private static void AddThobe(Transform root, Transform model, FaceRig rig, CharacterLook look)
        {
            var hips = Bone(model, "Hips");
            if (hips == null) return;
            var go = new GameObject("Thobe", typeof(MeshFilter), typeof(MeshRenderer));
            go.transform.SetParent(root, false);
            var mr = go.GetComponent<MeshRenderer>();
            mr.sharedMaterial = SaveMaterial("Thobe_" + look.prefabName, LitShader, look.thobeColor);
            var skirt = go.AddComponent<ThobeSkirt>();
            float h = look.height;
            skirt.Setup(root, hips, Bone(model, "Head"), rig != null ? rig.mouthC : null, Bone(model, "Foot.L"), Bone(model, "Foot.R"),
                        h * 0.43f, h * 0.05f, h * 0.135f, h * 0.17f);
            AddSirwal(model, h);
        }

        // Loose white sirwal on the legs and brown leather shoes, seen when the thobe is held up on a sprint.
        private static void AddSirwal(Transform model, float h)
        {
            var cotton = SaveMaterial("Sirwal_White", LitShader, new Color(0.95f, 0.95f, 0.93f));
            var leather = SaveMaterial("Shoe_Leather", LitShader, new Color(0.42f, 0.25f, 0.13f));
            float k = h / 1.8f;
            foreach (var side in new[] { "L", "R" })
            {
                var thigh = Bone(model, "UpperLeg." + side);
                var shin = Bone(model, "LowerLeg." + side);
                var foot = Bone(model, "Foot." + side);
                if (thigh != null && shin != null) Tube("Sirwal_Thigh." + side, thigh, thigh.position, shin.position, 0.21f * k, cotton);
                if (shin != null && foot != null) Tube("Sirwal_Shin." + side, shin, shin.position, foot.position + Vector3.up * 0.04f * k, 0.18f * k, cotton);
                if (foot != null)
                {
                    var toe = Vector3.ProjectOnPlane(model.forward, Vector3.up).normalized;
                    var shoe = GameObject.CreatePrimitive(PrimitiveType.Capsule);
                    shoe.name = "Shoe." + side;
                    Object.DestroyImmediate(shoe.GetComponent<Collider>());
                    shoe.GetComponent<MeshRenderer>().sharedMaterial = leather;
                    var ground = foot.position; ground.y = model.position.y;
                    shoe.transform.position = ground + Vector3.up * 0.045f * k + toe * 0.05f * k;
                    shoe.transform.rotation = Quaternion.LookRotation(Vector3.up, toe);
                    shoe.transform.localScale = new Vector3(0.1f, 0.13f, 0.09f) * k;
                    shoe.transform.SetParent(foot, true);
                }
            }
        }

        private static void Tube(string name, Transform bone, Vector3 from, Vector3 to, float width, Material mat)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            go.name = name;
            Object.DestroyImmediate(go.GetComponent<Collider>());
            go.GetComponent<MeshRenderer>().sharedMaterial = mat;
            Vector3 d = to - from;
            go.transform.position = (from + to) * 0.5f;
            go.transform.rotation = Quaternion.FromToRotation(Vector3.up, d.normalized);
            go.transform.localScale = new Vector3(width, d.magnitude * 0.5f + width * 0.25f, width);
            go.transform.SetParent(bone, true);
        }

        // Kept short and close to the skin: chin, two cheek strips up to the ears and a thin moustache.
        private static void AddBeard(Transform anchor, Material mat)
        {
            var beard = new GameObject("Beard").transform;
            beard.SetParent(anchor, false);
            FacePart(PrimitiveType.Sphere, "Chin", beard, new Vector3(0f, -1.85f, -0.3f), new Vector3(1.55f, 0.62f, 0.75f), mat);
            foreach (float side in new[] { -1f, 1f })
            {
                var jaw = FacePart(PrimitiveType.Sphere, side < 0f ? "JawL" : "JawR", beard, new Vector3(side * 1.3f, -1.3f, -0.6f), new Vector3(0.5f, 1.15f, 0.8f), mat);
                jaw.localRotation = Quaternion.Euler(0f, 0f, side * -18f);
                var tip = FacePart(PrimitiveType.Cube, side < 0f ? "MoustacheL" : "MoustacheR", beard, new Vector3(side * 0.24f, -0.9f, 0.05f), new Vector3(0.46f, 0.13f, 0.12f), mat);
                tip.localRotation = Quaternion.Euler(0f, 0f, side * -10f);
            }
        }

        private static Transform MouthCorner(string name, Transform anchor, float side, Material mat)
        {
            var pivot = new GameObject(name).transform;
            pivot.SetParent(anchor, false);
            pivot.localPosition = new Vector3(side * 0.15f, -1.15f, 0.04f);
            FacePart(PrimitiveType.Cube, "Line", pivot, new Vector3(side * 0.15f, 0f, 0f), new Vector3(0.32f, 0.11f, 0.1f), mat);
            return pivot;
        }

        private static Transform FacePart(PrimitiveType type, string name, Transform parent, Vector3 localPos, Vector3 localScale, Material mat)
        {
            var go = GameObject.CreatePrimitive(type);
            go.name = name;
            Object.DestroyImmediate(go.GetComponent<Collider>());
            var r = go.GetComponent<Renderer>();
            r.sharedMaterial = mat;
            r.shadowCastingMode = ShadowCastingMode.Off;
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPos;
            go.transform.localRotation = Quaternion.identity;
            go.transform.localScale = localScale;
            return go.transform;
        }

        /// <summary>
        /// Finds the painted eyes (the "Face" material on the Quaternius characters), returns the point between them
        /// on the face surface and half their spacing, then paints that slot with the skin material.
        /// </summary>
        private static bool MeasureEyes(Transform model, Transform head, float height, out Vector3 centre, out float halfGap)
        {
            centre = Vector3.zero;
            halfGap = 0f;
            Vector3 fwd = model.forward, right = model.right;
            var scaled = new List<Vector3>();
            var unscaled = new List<Vector3>();
            foreach (var smr in model.GetComponentsInChildren<SkinnedMeshRenderer>())
            {
                var mats = smr.sharedMaterials;
                int faceSlot = -1, skinSlot = -1;
                for (int i = 0; i < mats.Length; i++)
                {
                    if (mats[i] == null) continue;
                    if (mats[i].name.IndexOf("Face", System.StringComparison.OrdinalIgnoreCase) >= 0) faceSlot = i;
                    else if (mats[i].name.IndexOf("Skin", System.StringComparison.OrdinalIgnoreCase) >= 0) skinSlot = i;
                }
                if (faceSlot < 0 || smr.sharedMesh == null || faceSlot >= smr.sharedMesh.subMeshCount) continue;

                var baked = new Mesh();
                smr.BakeMesh(baked, true);
                var verts = baked.vertices;
                var full = smr.transform.localToWorldMatrix;
                var noScale = Matrix4x4.TRS(smr.transform.position, smr.transform.rotation, Vector3.one);
                foreach (int idx in baked.GetTriangles(faceSlot))
                {
                    scaled.Add(full.MultiplyPoint3x4(verts[idx]));
                    unscaled.Add(noScale.MultiplyPoint3x4(verts[idx]));
                }
                Object.DestroyImmediate(baked);
                if (skinSlot >= 0) { mats[faceSlot] = mats[skinSlot]; smr.sharedMaterials = mats; }
            }

            // BakeMesh scale handling has changed between Unity versions, so take whichever reading sits on the head.
            bool ok = false;
            float best = float.MaxValue;
            foreach (var pts in new[] { scaled, unscaled })
            {
                if (pts.Count < 6) continue;
                Vector3 left = Vector3.zero, rightEye = Vector3.zero;
                int nl = 0, nr = 0;
                Vector3 mid = Vector3.zero;
                foreach (var p in pts) mid += p;
                mid /= pts.Count;
                float front = float.MinValue;
                foreach (var p in pts)
                {
                    if (Vector3.Dot(p - mid, right) < 0f) { left += p; nl++; } else { rightEye += p; nr++; }
                    front = Mathf.Max(front, Vector3.Dot(p - mid, fwd));
                }
                if (nl == 0 || nr == 0) continue;
                left /= nl; rightEye /= nr;
                float gap = Vector3.Dot(rightEye - left, right) * 0.5f;
                Vector3 c = (left + rightEye) * 0.5f;
                c += fwd * (front - Vector3.Dot(c - mid, fwd));
                float miss = Vector3.Distance(c, head.position);
                if (gap < height * 0.01f || gap > height * 0.12f || miss > height * 0.3f || miss >= best) continue;
                best = miss;
                centre = c;
                halfGap = gap;
                ok = true;
            }
            return ok;
        }

        private static void AddWeapon(Transform model, string weapon, Color color, float height)
        {
            var hand = Bone(model, "Fist.R") ?? Bone(model, "LowerArm.R");
            if (hand == null) return;
            var pivot = new GameObject("Weapon").transform;
            pivot.SetParent(hand, false);
            pivot.localPosition = Vector3.zero;
            var mat = SaveMaterial("Weapon_" + weapon, LitShader, color);
            Vector3 down = -Vector3.up;
            Vector3 fwd = model.forward;
            Transform tip;
            if (weapon == "agal")
            {
                float l = height * 0.32f;
                Piece(PrimitiveType.Cylinder, "Cord", pivot, hand.position + fwd * l * 0.5f, Quaternion.LookRotation(Vector3.up, fwd), new Vector3(0.035f, l * 0.5f, 0.035f), mat);
                Piece(PrimitiveType.Cylinder, "Loop", pivot, hand.position + fwd * l, Quaternion.LookRotation(Vector3.up, fwd), new Vector3(0.14f, 0.012f, 0.14f), mat);
                tip = new GameObject("Tip").transform;
                tip.SetParent(pivot, false);
                tip.position = hand.position + fwd * l;
            }
            else
            {
                Piece(PrimitiveType.Cylinder, "Strap", pivot, hand.position + down * 0.12f, Quaternion.identity, new Vector3(0.02f, 0.12f, 0.02f), mat);
                Piece(PrimitiveType.Cube, "Bag", pivot, hand.position + down * 0.32f, Quaternion.LookRotation(fwd), new Vector3(0.3f, 0.24f, 0.12f), mat);
                tip = new GameObject("Tip").transform;
                tip.SetParent(pivot, false);
                tip.position = hand.position + down * 0.4f;
            }
            var trail = tip.gameObject.AddComponent<TrailRenderer>();
            trail.time = 0.18f;
            trail.minVertexDistance = 0.03f;
            trail.widthCurve = new AnimationCurve(new Keyframe(0f, 0.22f), new Keyframe(1f, 0f));
            trail.colorGradient = Gradient(new Color(1f, 1f, 1f), new Color(1f, 0.85f, 0.5f));
            trail.sharedMaterial = ParticleMaterial();
            trail.shadowCastingMode = ShadowCastingMode.Off;
            trail.emitting = false;
        }

        private static AnimationClip Clip(Object[] all, string name)
        {
            foreach (var o in all)
                if (o is AnimationClip c && !c.name.StartsWith("__preview__") && (c.name == name || c.name.EndsWith("|" + name))) return c;
            return null;
        }

        public static AnimatorController BuildController(string fbx, string path)
        {
            AssetDatabase.DeleteAsset(path);
            var ctrl = AnimatorController.CreateAnimatorControllerAtPath(path);
            var all = AssetDatabase.LoadAllAssetsAtPath(fbx);
            var idle = Clip(all, "Idle");
            var walk = Clip(all, "Walk");
            var run = Clip(all, "Run");

            ctrl.AddParameter("Speed", AnimatorControllerParameterType.Float);
            ctrl.AddParameter("Grounded", AnimatorControllerParameterType.Bool);
            ctrl.AddParameter("Sliding", AnimatorControllerParameterType.Bool);
            ctrl.AddParameter("Riding", AnimatorControllerParameterType.Bool);
            foreach (var t in new[] { "Jump", "DoubleJump", "Spin", "Attack", "AttackAgal", "AttackHandbag", "Hurt", "Die", "Cheer", "Sad" })
                ctrl.AddParameter(t, AnimatorControllerParameterType.Trigger);
            ctrl.parameters = ctrl.parameters.Select(p => { if (p.name == "Grounded") p.defaultBool = true; return p; }).ToArray();

            var sm = ctrl.layers[0].stateMachine;
            var loco = ctrl.CreateBlendTreeInController("Locomotion", out var tree, 0);
            tree.blendParameter = "Speed";
            tree.useAutomaticThresholds = false;
            if (idle != null) tree.AddChild(idle, 0f);
            if (walk != null) tree.AddChild(walk, 0.4f);
            if (run != null) tree.AddChild(run, 1f);
            sm.defaultState = loco;

            var jump = OneShot(sm, "Jump", Clip(all, "Jump"), loco, 0.7f);
            AnyTrigger(sm, jump, "Jump", true);
            AnyTrigger(sm, jump, "DoubleJump", true);
            var back = jump.transitions[0];
            back.AddCondition(AnimatorConditionMode.If, 0f, "Grounded");

            var attack = OneShot(sm, "Attack", Clip(all, "SwordSlash") ?? Clip(all, "Punch"), loco, 0.8f);
            foreach (var t in new[] { "Attack", "AttackAgal", "AttackHandbag", "Spin" }) AnyTrigger(sm, attack, t, true);

            var hurt = OneShot(sm, "Hurt", Clip(all, "RecieveHit"), loco, 0.85f);
            AnyTrigger(sm, hurt, "Hurt", false);
            var die = OneShot(sm, "Die", Clip(all, "Death"), loco, 1f);
            AnyTrigger(sm, die, "Die", false);

            var victory = Clip(all, "Victory");
            if (victory != null) AnyTrigger(sm, OneShot(sm, "Cheer", victory, loco, 0.9f), "Cheer", false);
            var defeat = Clip(all, "Defeat");
            if (defeat != null) AnyTrigger(sm, OneShot(sm, "Sad", defeat, loco, 0.9f), "Sad", false);

            var slide = sm.AddState("Slide");
            slide.motion = Clip(all, "Roll");
            var toSlide = sm.AddAnyStateTransition(slide);
            toSlide.AddCondition(AnimatorConditionMode.If, 0f, "Sliding");
            toSlide.canTransitionToSelf = false;
            toSlide.duration = 0.05f;
            var fromSlide = slide.AddTransition(loco);
            fromSlide.AddCondition(AnimatorConditionMode.IfNot, 0f, "Sliding");
            fromSlide.duration = 0.1f;

            // In the saddle: the first frame of StandUp is a seated pose, held still.
            var sit = Clip(all, "StandUp") ?? Clip(all, "SitDown");
            if (sit != null)
            {
                var ride = sm.AddState("Ride");
                ride.motion = sit;
                ride.speed = 0f;
                ride.cycleOffset = sit.name.EndsWith("SitDown") ? 0.97f : 0f;
                var toRide = sm.AddAnyStateTransition(ride);
                toRide.AddCondition(AnimatorConditionMode.If, 0f, "Riding");
                toRide.canTransitionToSelf = false;
                toRide.duration = 0.2f;
                var fromRide = ride.AddTransition(loco);
                fromRide.AddCondition(AnimatorConditionMode.IfNot, 0f, "Riding");
                fromRide.duration = 0.15f;
            }

            EditorUtility.SetDirty(ctrl);
            return ctrl;
        }

        private static AnimatorState OneShot(AnimatorStateMachine sm, string name, Motion clip, AnimatorState back, float exitTime)
        {
            var s = sm.AddState(name);
            s.motion = clip;
            var t = s.AddTransition(back);
            t.hasExitTime = true;
            t.exitTime = exitTime;
            t.duration = 0.15f;
            return s;
        }

        private static void AnyTrigger(AnimatorStateMachine sm, AnimatorState to, string trigger, bool toSelf)
        {
            var t = sm.AddAnyStateTransition(to);
            t.AddCondition(AnimatorConditionMode.If, 0f, trigger);
            t.duration = 0.05f;
            t.canTransitionToSelf = toSelf;
        }

        // ------------------------------------------------------------------ particles
        private static Material particleMat;

        public static Material ParticleMaterial()
        {
            if (particleMat != null) return particleMat;
            particleMat = SaveMaterial("FxParticle", ParticleShader, Color.white);
            return particleMat;
        }

        private static Mesh BuiltinMesh(PrimitiveType type)
        {
            var tmp = GameObject.CreatePrimitive(type);
            var mesh = tmp.GetComponent<MeshFilter>().sharedMesh;
            Object.DestroyImmediate(tmp);
            return mesh;
        }

        public static Gradient Gradient(Color a, Color b)
        {
            var g = new Gradient();
            g.SetKeys(new[] { new GradientColorKey(a, 0f), new GradientColorKey(b, 1f) },
                      new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(1f, 1f) });
            return g;
        }

        public static ParticleSystem Burst(string name, Transform parent, int count, PrimitiveType shape, Color a, Color b,
                                           float size, float speed, float life, float gravity, float radius)
        {
            var go = new GameObject("Fx_" + name);
            go.SetActive(false);
            go.transform.SetParent(parent, false);
            var ps = go.AddComponent<ParticleSystem>();

            var main = ps.main;
            main.duration = 0.5f;
            main.loop = false;
            main.playOnAwake = true;
            main.startLifetime = new ParticleSystem.MinMaxCurve(life * 0.6f, life);
            main.startSpeed = new ParticleSystem.MinMaxCurve(speed * 0.5f, speed);
            main.startSize = new ParticleSystem.MinMaxCurve(size * 0.6f, size);
            main.startRotation3D = true;
            main.startRotationX = new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2f);
            main.startRotationY = new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2f);
            main.startRotationZ = new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2f);
            main.startColor = new ParticleSystem.MinMaxGradient(a, b);
            main.gravityModifier = gravity;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.maxParticles = count * 2;

            var em = ps.emission;
            em.rateOverTime = 0f;
            em.SetBursts(new[] { new ParticleSystem.Burst(0f, (short)count) });

            var sh = ps.shape;
            sh.shapeType = ParticleSystemShapeType.Sphere;
            sh.radius = radius;

            var sol = ps.sizeOverLifetime;
            sol.enabled = true;
            sol.size = new ParticleSystem.MinMaxCurve(1f, new AnimationCurve(new Keyframe(0f, 1f), new Keyframe(0.7f, 0.8f), new Keyframe(1f, 0f)));

            var rol = ps.rotationOverLifetime;
            rol.enabled = true;
            rol.z = new ParticleSystem.MinMaxCurve(-6f, 6f);

            var r = go.GetComponent<ParticleSystemRenderer>();
            r.renderMode = ParticleSystemRenderMode.Mesh;
            r.mesh = BuiltinMesh(shape);
            r.sharedMaterial = ParticleMaterial();
            r.shadowCastingMode = ShadowCastingMode.Off;
            return ps;
        }

        // ------------------------------------------------------------------ procedural meshes
        /// <summary>Closed loft through rectangular cross-sections (y, xMin, xMax, halfDepth). Flat shaded.</summary>
        public static Mesh Loft(string name, IList<Vector4> sections)
        {
            var v = new List<Vector3>();
            var tris = new List<int>();
            void Quad(Vector3 a, Vector3 b, Vector3 c, Vector3 d)
            {
                int i = v.Count;
                v.Add(a); v.Add(b); v.Add(c); v.Add(d);
                tris.AddRange(new[] { i, i + 1, i + 2, i, i + 2, i + 3 });
            }
            Vector3[] Ring(Vector4 s) => new[]
            {
                new Vector3(s.y, s.x, -s.w), new Vector3(s.z, s.x, -s.w), new Vector3(s.z, s.x, s.w), new Vector3(s.y, s.x, s.w),
            };
            for (int i = 0; i < sections.Count - 1; i++)
            {
                var lo = Ring(sections[i]);
                var hi = Ring(sections[i + 1]);
                for (int k = 0; k < 4; k++)
                {
                    int n = (k + 1) % 4;
                    Quad(lo[k], hi[k], hi[n], lo[n]);
                }
            }
            var bottom = Ring(sections[0]);
            Quad(bottom[0], bottom[1], bottom[2], bottom[3]);
            var top = Ring(sections[sections.Count - 1]);
            Quad(top[3], top[2], top[1], top[0]);

            var mesh = new Mesh { name = name };
            mesh.SetVertices(v);
            mesh.SetTriangles(tris, 0);
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            EnsureFolder(GeneratedDir + "/Meshes");
            string path = GeneratedDir + "/Meshes/" + name + ".asset";
            AssetDatabase.DeleteAsset(path);
            AssetDatabase.CreateAsset(mesh, path);
            return mesh;
        }
    }
}
