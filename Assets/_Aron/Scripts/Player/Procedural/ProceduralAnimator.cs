// (c) Ahmed Alamrani. All rights reserved.
using System.Collections.Generic;
using Aron.Config;
using Aron.Core;
using Aron.Logic;
using Aron.Logic.GameFeel;
using Aron.Save;
using UnityEngine;

namespace Aron.Player
{
    /// <summary>
    /// The procedural layer on top of whatever the clips (or the greybox) do: lean into turns and speed, breathing, squash and stretch on
    /// jump and landing, an additive flinch on a hit, and the head and eyes turning to Noura or a point of interest. All the maths is in
    /// Aron.Logic.GameFeel (tested); this component only feeds it and applies the result.
    /// It works on a "ProcRoot" it makes under the visual root (the model is moved into it), so it never fights the spin or the slide squash on the
    /// visual root itself. Foot IK and the humanoid head/eye pass are in <see cref="ProceduralIk"/>, the cloth in <see cref="SecondaryMotion"/>.
    /// The budget (<see cref="ProcBudget"/>): Low tier keeps lean, breath, squash and flinch only; High adds foot IK; reduce-motion scales every amplitude to
    /// <c>reduceMotionScale</c>. Everything reads <see cref="FeelSettings.V"/> each frame, so the tuning panel changes it live.
    /// </summary>
    [DefaultExecutionOrder(100)]
    public class ProceduralAnimator : MonoBehaviour
    {
        private static readonly List<Transform> Interests = new List<Transform>();

        /// <summary>A point he may glance at (a shopkeeper, a falcon, a sign...). Remove it again when it goes away.</summary>
        public static void AddInterest(Transform t) { if (t != null && !Interests.Contains(t)) Interests.Add(t); }
        public static void RemoveInterest(Transform t) => Interests.Remove(t);

        private PlayerController player;
        private Transform visualRoot, procRoot, head;
        private readonly LeanModel lean = new LeanModel();
        private readonly BreathModel breath = new BreathModel();
        private readonly SquashStretchModel squash = new SquashStretchModel();
        private readonly FlinchModel flinch = new FlinchModel();
        private readonly LookAtModel look = new LookAtModel();
        private ProcBudget budget;
        private float nextBudget, nextLookPick, lastYaw;
        private bool hadYaw;
        private bool hasLookTarget;
        private Vector3 lookTargetPos;
        private float pendingYaw, pendingPitch;
        private readonly float[] dx = new float[8], dy = new float[8], dz = new float[8];
        private readonly Transform[] cand = new Transform[8];
        private Quaternion headRest = Quaternion.identity;
        private bool headRestKnown, humanoid;

        public ProcBudget Budget => budget;
        public LookAtModel Look => look;
        public Transform ProcRoot => procRoot;
        /// <summary>A world point in the look direction (for the Animator's look-at IK), valid while <see cref="Look"/>.Weight is above zero.</summary>
        public Vector3 LookPoint { get; private set; }
        public Vector3 EyePosition => player != null ? player.transform.position + Vector3.up * (player.BodyHeight * 0.92f) : transform.position;

        private void Awake() { player = GetComponent<PlayerController>(); }

        private void Start()
        {
            if (player != null && visualRoot == null) visualRoot = player.VisualRoot;
            Rebind();
        }

        /// <summary>Called by CharacterSetup before it clears the visual root.</summary>
        public void Release() { procRoot = null; head = null; headRestKnown = false; humanoid = false; }

        /// <summary>Makes sure the ProcRoot exists and holds the current model. Safe to call every time the model changes.</summary>
        public void Rebind()
        {
            if (player == null) player = GetComponent<PlayerController>();
            if (visualRoot == null && player != null) visualRoot = player.VisualRoot;
            if (visualRoot == null) return;
            if (procRoot == null)
            {
                procRoot = new GameObject("ProcRoot").transform;
                procRoot.SetParent(visualRoot, false);
            }
            for (int i = visualRoot.childCount - 1; i >= 0; i--)
            {
                var c = visualRoot.GetChild(i);
                if (c != procRoot) c.SetParent(procRoot, false);
            }
            head = FindDeep(procRoot, "Head");
            headRestKnown = false;
            var animator = procRoot.GetComponentInChildren<Animator>();
            humanoid = animator != null && animator.isHuman;
            if (humanoid)
            {
                var ik = animator.GetComponent<ProceduralIk>();
                if (ik == null) ik = animator.gameObject.AddComponent<ProceduralIk>();
                ik.Bind(this, player);
            }
            // secondary motion: every chain of shemagh-tail / hem bones in the model
            var sm = GetComponent<SecondaryMotion>();
            if (sm == null) sm = gameObject.AddComponent<SecondaryMotion>();
            sm.Rebuild(procRoot, player);
        }

        private static Transform FindDeep(Transform root, string name)
        {
            if (root.name == name) return root;
            for (int i = 0; i < root.childCount; i++) { var r = FindDeep(root.GetChild(i), name); if (r != null) return r; }
            return null;
        }

        // ---- events from the controller
        public void OnJump() { if (budget.SquashStretch) squash.Impulse(FeelSettings.V.stretchJump * budget.Amplitude); }
        public void OnLanded(float squashAmount) { if (budget.SquashStretch) { squash.Impulse(-squashAmount * budget.Amplitude); breath.Exertion = Mathf.Min(1f, breath.Exertion + 0.3f); } }

        /// <summary>A hit that did not end the run. <paramref name="fromWorld"/> is where it came from; power is 0..1.</summary>
        public void OnHit(Vector3 fromWorld, float power)
        {
            if (!budget.Flinch) return;
            Vector3 local = transform.InverseTransformPoint(fromWorld);
            flinch.Hit(local.x, local.z, power);
        }

        public void ResetAll()
        {
            lean.Reset(); breath.Reset(); squash.Reset(); look.Reset();
            ApplyIdentity();
        }

        private void ApplyIdentity()
        {
            if (procRoot == null) return;
            procRoot.localPosition = Vector3.zero; procRoot.localRotation = Quaternion.identity; procRoot.localScale = Vector3.one;
        }

        private void RefreshBudget()
        {
            if (Time.unscaledTime < nextBudget) return;
            nextBudget = Time.unscaledTime + 1f;
            var d = SaveSystem.Data;
            bool reduce = d != null && d.access != null && d.access.reduceMotion;
            budget = ProcBudget.For(FeelSettings.V, DeviceSettings.Tier, reduce);
        }

        private void LateUpdate()
        {
            if (player == null) return;
            RefreshBudget();
            if (procRoot == null) Rebind();
            if (procRoot == null) return;
            if (player.IsDead || Time.timeScale <= 0f) { if (player.IsDead) ApplyIdentity(); return; }
            var v = FeelSettings.V;
            float dt = Time.deltaTime;
            if (dt <= 0f) return;
            float amp = budget.Amplitude;

            // yaw rate of the body, for the lean
            float yaw = transform.eulerAngles.y;
            float turnRate = hadYaw ? Mathf.DeltaAngle(lastYaw, yaw) / dt : 0f;
            lastYaw = yaw; hadYaw = true;
            Vector3 vel = player.Velocity; vel.y = 0f;
            float topSpeed = v.runSpeed * v.sprintMultiplier;
            float speed01 = Mathf.Clamp01(vel.magnitude / Mathf.Max(0.1f, topSpeed));
            bool grounded = player.IsGrounded;
            bool riding = player.IsRiding;

            if (budget.Lean && !riding) lean.Step(v, turnRate, speed01, player.IsSkidding, grounded, dt, amp); else lean.Reset();
            if (budget.Breath) breath.Step(v, dt, player.IsSprinting || speed01 > 0.85f, amp); else breath.Reset();
            if (budget.SquashStretch) squash.Step(v, dt, player.Velocity.y, grounded, amp); else squash.Reset();
            if (budget.Flinch) flinch.Step(v, dt, amp);
            UpdateLook(v, dt, amp);

            // compose: scale about the feet, rotate about the hips (so the head swings, the feet stay), then the flinch shove
            float hip = Mathf.Max(0.5f, player.BodyHeight * 0.5f);
            float h = squash.Height * (1f + breath.Value), w = squash.Width * (1f + breath.Value * 0.4f);
            // flinch roll is mirrored on purpose: the model's roll is "positive = away" for a right-handed reader, Unity's Z is the other way
            Quaternion rot = Quaternion.Euler(lean.Pitch + flinch.Pitch, 0f, lean.Roll - flinch.Roll);
            procRoot.localScale = new Vector3(w, h, w);
            procRoot.localRotation = rot;
            procRoot.localPosition = Vector3.up * hip - rot * (Vector3.up * hip) + Vector3.forward * flinch.Shove;

            ApplyHeadLook(v);
        }

        // ---- look-at
        private void UpdateLook(FeelValues v, float dt, float amp)
        {
            if (!budget.LookAt || player.IsRiding || player.IsSliding || player.IsDead) { look.Step(v, false, 0f, 0f, dt, amp); return; }
            if (Time.time >= nextLookPick)
            {
                nextLookPick = Time.time + 0.2f;
                PickTarget(v);
            }
            UpdateTargetAngles();
            look.Step(v, hasLookTarget, pendingYaw, pendingPitch, dt, amp);
            // the point the IK looks at: where the current look direction points, 6 m out from the eyes
            Quaternion q = transform.rotation * Quaternion.Euler(-look.Pitch, look.Yaw, 0f);
            LookPoint = EyePosition + q * Vector3.forward * 6f;
        }

        private Transform lookTarget;

        private void PickTarget(FeelValues v)
        {
            int n = 0; int preferred = -1;
            var team = Aron.Heroes.HeroTeam.Instance;
            Transform partner = team != null ? team.Partner : null;
            Vector3 eye = EyePosition;
            if (partner != null && partner.gameObject.activeInHierarchy) { Add(partner, eye, ref n); preferred = 0; }
            for (int i = Interests.Count - 1; i >= 0 && n < cand.Length; i--)
            {
                if (Interests[i] == null) { Interests.RemoveAt(i); continue; }
                Add(Interests[i], eye, ref n);
            }
            int pick = LookAtModel.Pick(v, dx, dy, dz, n, preferred);
            hasLookTarget = pick >= 0;
            lookTarget = hasLookTarget ? cand[pick] : null;
            if (hasLookTarget) lookTargetPos = lookTarget.position + Vector3.up * 1.5f;
        }

        private void Add(Transform t, Vector3 eye, ref int n)
        {
            Vector3 local = transform.InverseTransformPoint(t.position + Vector3.up * 1.5f) - transform.InverseTransformPoint(eye);
            cand[n] = t; dx[n] = local.x; dy[n] = local.y; dz[n] = local.z; n++;
        }

        private void UpdateTargetAngles()
        {
            if (lookTarget == null) { hasLookTarget = false; return; }
            lookTargetPos = lookTarget.position + Vector3.up * 1.5f;
            Vector3 local = transform.InverseTransformPoint(lookTargetPos) - transform.InverseTransformPoint(EyePosition);
            LookAtModel.AnglesTo(local.x, local.y, local.z, out pendingYaw, out pendingPitch);
        }

        // without a humanoid rig (greybox, or a rig without an avatar) the head object turns itself; humanoid rigs use ProceduralIk
        private void ApplyHeadLook(FeelValues v)
        {
            if (head == null || procRoot == null) return;
            if (humanoid) return;
            if (!headRestKnown) { headRest = head.localRotation; headRestKnown = true; }
            float share = LookAtModel.HeadShare(v) * look.Weight;
            head.localRotation = headRest * Quaternion.Euler(-look.Pitch * share, look.Yaw * share, 0f);
        }
    }
}
