// (c) Ahmed Alamrani. All rights reserved.
using System;
using System.Collections.Generic;
using Aron.Config;
using Aron.Core;
using Aron.Fx;
using Aron.Data;
using Aron.Input;
using Aron.Localization;
using Aron.Logic.Heroes;
using Aron.Player;
using Aron.Store;
using Aron.UI;
using UnityEngine;

namespace Aron.Heroes
{
    /// <summary>
    /// Aron and Noura play together. One is controlled, the other follows a few steps behind on the same path.
    /// Tapping the partner swaps them over: no button, the tap on her (or him) is the switch.
    /// Noura's handbag goes out and back like a boomerang on her taps.
    /// </summary>
    [RequireComponent(typeof(PlayerController))]
    public class HeroTeam : MonoBehaviour
    {
        public static HeroTeam Instance { get; private set; }
        /// <summary>Raised after a swap with the new active hero id.</summary>
        public static event Action<string> Switched;

        [SerializeField] private float followGap = 1.8f;

        private PlayerController pc;
        private CharacterSetup setup;
        private HeroSwitch swap;
        private HeroFile heroes;
        private Transform partner;
        private Collider partnerTap;
        private Animator partnerAnim;
        private Boomerang bag;
        private readonly List<Vector3> trail = new List<Vector3>();
        private Vector3 partnerVel;
        private static readonly RaycastHit[] hits = new RaycastHit[12];

        public string Active => swap != null ? swap.Active : "aron";
        public bool NouraActive => Active == "noura";
        public Transform Partner => partner;
        public Transform ActiveTransform => transform;
        /// <summary>Wherever Noura is right now, played or following.</summary>
        public Transform Noura => NouraActive ? transform : partner;
        public HeroDef ActiveHero => heroes?.Get(Active);

        private void Awake()
        {
            Instance = this;
            pc = GetComponent<PlayerController>();
            setup = GetComponent<CharacterSetup>();
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        private void Start()
        {
            heroes = GameData.Heroes;
            string first = setup != null && setup.Active != null ? setup.Active.characterId : "aron";
            string other = first == "noura" ? "aron" : "noura";
            swap = new HeroSwitch(first, other);
            var otherDef = Def(other);
            if (otherDef != null && otherDef.modelPrefab != null)
            {
                BuildPartner(otherDef);
                partner.position = transform.position - transform.forward * followGap;
            }
            ApplyHero();
        }

        private CharacterDefinition Def(string id) => setup != null && setup.Roster != null ? setup.Roster.Get(id) : null;

        private void BuildPartner(CharacterDefinition def)
        {
            if (partner == null)
            {
                partner = new GameObject("Partner").transform;
                var cap = partner.gameObject.AddComponent<CapsuleCollider>();
                cap.isTrigger = true; cap.radius = 0.55f; cap.height = 1.9f; cap.center = new Vector3(0f, 0.95f, 0f);
                partnerTap = cap;
                Fx.DropShadow.Ensure(partner.gameObject, 0.55f);
                var rb = partner.gameObject.AddComponent<Rigidbody>();
                rb.isKinematic = true; rb.useGravity = false;
            }
            for (int i = partner.childCount - 1; i >= 0; i--) Destroy(partner.GetChild(i).gameObject);
            var model = Instantiate(def.modelPrefab, partner);
            model.transform.localPosition = Vector3.zero;
            model.transform.localRotation = Quaternion.identity;
            partnerAnim = model.GetComponentInChildren<Animator>();
            if (partnerAnim != null && def.animatorController != null) partnerAnim.runtimeAnimatorController = def.animatorController;
            CharacterCosmetics.Attach(model, def.characterId);
        }

        private void ApplyHero()
        {
            var h = ActiveHero;
            pc.Hero = h;
            var cc = GetComponent<CharacterController>();
            if (cc != null && h != null) cc.radius = h.radius;
            bag = h != null && h.throwRange > 0f ? new Boomerang(h.throwRange, h.throwTime) : null;
        }

        /// <summary>Taps go through here first; a tap on the partner is the swap.</summary>
        public static bool ClaimTap(float x, float y)
        {
            var t = Instance;
            if (t == null || t.partnerTap == null || StoreScreen.IsShowing) return false;
            var cam = Camera.main;
            if (cam == null) return false;
            var ray = cam.ScreenPointToRay(new Vector3(x, y, 0f));
            int n = Physics.RaycastNonAlloc(ray, hits, 80f, ~0, QueryTriggerInteraction.Collide);
            for (int i = 0; i < n; i++)
                if (hits[i].collider == t.partnerTap) { t.TrySwitch(); return true; }
            return false;
        }

        public bool TrySwitch()
        {
            if (partner == null) return false;
            float d = Vector3.Distance(transform.position, partner.position);
            bool busy = pc.IsRiding || !pc.IsGrounded || pc.IsSliding || pc.IsDead;
            if (!swap.TrySwitch(Time.time, d, busy)) return false;

            Vector3 here = transform.position, there = partner.position;
            Quaternion look = transform.rotation;
            setup.Apply(Def(swap.Active));
            BuildPartner(Def(swap.Partner));
            pc.Teleport(there + Vector3.up * 0.05f);
            partner.SetPositionAndRotation(here, look);
            trail.Clear();
            ApplyHero();
            FxPlayer.Play("sparkle", there);
            GameEvents.PlaySfx("bounce");
            var h = ActiveHero;
            if (h != null) GameUI.Banner(Strings.Format("hero_now", h.nameAr), 1.4f);
            Switched?.Invoke(swap.Active);
            return true;
        }

        private void Update()
        {
            Follow(Time.deltaTime);
            if (bag == null || pc.IsRiding) return;
            var intent = InputRouter.Instance != null ? InputRouter.Instance.Current : default;
            if (intent.attackPressed && pc.IsGrounded && bag.TryThrow(Time.time))
            {
                Vector3 dir = transform.forward;
                if (intent.hasAim) { dir = intent.aimPoint - transform.position; dir.y = 0f; dir = dir.sqrMagnitude > 0.01f ? dir.normalized : transform.forward; }
                HandbagThrow.Launch(this, bag, dir);
            }
        }

        // The partner walks the player's own path, a couple of steps behind.
        private void Follow(float dt)
        {
            if (partner == null) return;
            Vector3 p = transform.position;
            if (trail.Count == 0 || (trail[trail.Count - 1] - p).sqrMagnitude > 0.09f) trail.Add(p);
            if (trail.Count > 200) trail.RemoveAt(0);

            Vector3 target = trail[0];
            float walked = 0f;
            for (int i = trail.Count - 1; i > 0; i--)
            {
                walked += Vector3.Distance(trail[i], trail[i - 1]);
                if (walked >= followGap) { target = trail[i - 1]; trail.RemoveRange(0, i - 1); break; }
            }
            if ((target - partner.position).sqrMagnitude > 400f) partner.position = target;  // fell far behind, e.g. a respawn
            Vector3 next = Vector3.SmoothDamp(partner.position, target, ref partnerVel, 0.12f);
            Vector3 flat = next - partner.position; flat.y = 0f;
            if (flat.sqrMagnitude > 1e-5f) partner.rotation = Quaternion.Slerp(partner.rotation, Quaternion.LookRotation(flat), 10f * dt);
            partner.position = next;
            if (partnerAnim != null)
            {
                partnerAnim.SetFloat(PlayerAnimationHooks.Speed, Mathf.Clamp01(partnerVel.magnitude / 6f));
                partnerAnim.SetBool(PlayerAnimationHooks.Grounded, true);
            }
        }
    }
}
