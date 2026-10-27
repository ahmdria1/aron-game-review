// (c) Ahmed Alamrani. All rights reserved.
using Aron.Config;
using Aron.Save;
using UnityEngine;

namespace Aron.Player
{
    /// <summary>
    /// Applies the character chosen in the main menu (Aron with agal, or the heroine with handbag):
    /// spawns the model prefab if provided, otherwise builds a greybox body + weapon prop.
    /// </summary>
    [DefaultExecutionOrder(-60)]
    public class CharacterSetup : MonoBehaviour
    {
        [SerializeField] private CharacterRoster roster;
        [SerializeField] private Transform visualRoot;
        [SerializeField] private MeleeAttack melee;
        [SerializeField] private PlayerAnimationHooks anim;

        public CharacterDefinition Active { get; private set; }
        public CharacterRoster Roster => roster != null ? roster : (roster = CharacterRoster.Load());

        private void Awake()
        {
            if (roster == null) roster = CharacterRoster.Load();
            if (roster == null) return;
            Active = roster.Get(SaveSystem.Data.selectedCharacterId);
            if (Active == null) return;
            Apply(Active);
        }

        public void Apply(CharacterDefinition def)
        {
            Active = def;
            if (visualRoot == null) visualRoot = transform;
            // The procedural layer lives in a ProcRoot under the visual root; let it go before the model is replaced, and take the old ones out of the hierarchy
            // at once so the new rig is never mistaken for them during this frame.
            var proc = GetComponent<ProceduralAnimator>();
            if (proc != null) proc.Release();
            for (int i = visualRoot.childCount - 1; i >= 0; i--)
            {
                var old = visualRoot.GetChild(i);
                old.SetParent(null, false);
                Destroy(old.gameObject);
            }

            Transform weaponVisual;
            if (def.modelPrefab != null)
            {
                var model = Instantiate(def.modelPrefab, visualRoot);
                model.transform.localPosition = Vector3.zero;
                var animator = model.GetComponentInChildren<Animator>();
                if (animator != null && def.animatorController != null) animator.runtimeAnimatorController = def.animatorController;
                if (anim != null) anim.Bind(animator);
                weaponVisual = FindDeep(model.transform, "Weapon");
                Store.CharacterCosmetics.Attach(model, def.characterId);
            }
            else
            {
                weaponVisual = BuildGreybox(def);
            }
            if (melee != null) melee.Configure(def.weapon, weaponVisual, def.modelPrefab != null);
            BindSleeves(def);
            if (proc != null) proc.Rebind();   // the model goes under ProcRoot; the hem and shemagh-tail bones (if the rig has them) get their cloth chains
        }

        /// <summary>Aron gets the sleeve roll-up; Noura never does (her abaya stays fully covering).</summary>
        private void BindSleeves(CharacterDefinition def)
        {
            var sleeves = GetComponent<SleeveRoll>();
            if (!Logic.Heroes.SleeveRules.Allowed(def.characterId)) { if (sleeves != null) Destroy(sleeves); return; }
            if (sleeves == null) sleeves = gameObject.AddComponent<SleeveRoll>();
            var model = visualRoot != null && visualRoot.childCount > 0 && def.modelPrefab != null ? visualRoot.GetChild(visualRoot.childCount - 1).gameObject : null;
            sleeves.Bind(def.characterId, model);
        }

        private Transform BuildGreybox(CharacterDefinition def)
        {
            var body = Prim(PrimitiveType.Capsule, "Body", visualRoot, new Vector3(0f, 1f, 0f), new Vector3(0.8f, 1f, 0.8f), def.greyboxColor);
            Prim(PrimitiveType.Sphere, "Head", visualRoot, new Vector3(0f, 1.95f, 0f), Vector3.one * 0.55f, new Color(0.85f, 0.65f, 0.5f));
            // Nose marker shows facing direction.
            Prim(PrimitiveType.Cube, "Facing", visualRoot, new Vector3(0f, 1.95f, 0.3f), new Vector3(0.12f, 0.12f, 0.12f), new Color(0.85f, 0.6f, 0.45f));

            var pivot = new GameObject("Weapon").transform;
            pivot.SetParent(visualRoot, false);
            pivot.localPosition = new Vector3(0.45f, 1.3f, 0f);

            bool isAgal = def.weapon == null || def.weapon.weaponId == "agal";
            if (isAgal)
            {
                // Shemagh + agal ring on the head; a second agal cord is the swinging weapon.
                Prim(PrimitiveType.Cylinder, "Shemagh", visualRoot, new Vector3(0f, 2.1f, 0f), new Vector3(0.62f, 0.08f, 0.62f), new Color(0.85f, 0.1f, 0.1f));
                Prim(PrimitiveType.Cylinder, "AgalRing", visualRoot, new Vector3(0f, 2.17f, 0f), new Vector3(0.5f, 0.03f, 0.5f), def.accessoryColor);
                Prim(PrimitiveType.Cylinder, "AgalCord", pivot, new Vector3(0f, 0f, 0.55f), new Vector3(0.06f, 0.55f, 0.06f), def.accessoryColor)
                    .transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            }
            else
            {
                Prim(PrimitiveType.Cylinder, "Strap", pivot, new Vector3(0f, 0f, 0.35f), new Vector3(0.04f, 0.35f, 0.04f), def.accessoryColor)
                    .transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
                Prim(PrimitiveType.Cube, "Handbag", pivot, new Vector3(0f, -0.1f, 0.75f), new Vector3(0.35f, 0.3f, 0.15f), def.accessoryColor);
            }
            return pivot;
        }

        private static GameObject Prim(PrimitiveType t, string name, Transform parent, Vector3 pos, Vector3 scale, Color c)
        {
            var go = PrimitiveFactory.Create(t);
            go.name = name;
            var col = go.GetComponent<Collider>();
            if (col != null) Destroy(col);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = pos;
            go.transform.localScale = scale;
            var r = go.GetComponent<Renderer>();
            if (r != null) r.material.color = c;
            return go;
        }

        private static Transform FindDeep(Transform root, string name)
        {
            if (root.name == name) return root;
            foreach (Transform c in root) { var f = FindDeep(c, name); if (f != null) return f; }
            return null;
        }
    }
}
