// (c) Ahmed Alamrani. All rights reserved.
using Aron.Core;
using Aron.Logic;
using Aron.Player;
using UnityEngine;

namespace Aron.Animals
{
    /// <summary>
    /// Sits on the player and owns the riding state. The player keeps the same gestures; this only changes
    /// speed, jump height and what a hit does, and puts Aron in the saddle.
    /// </summary>
    [RequireComponent(typeof(PlayerController))]
    public class Rider : MonoBehaviour
    {
        [SerializeField] private float hipHeight = 0.48f;
        [SerializeField] private float riderTorso = 1.15f;

        private static readonly int RidingId = Animator.StringToHash("Riding");

        private readonly MountRide ride = new MountRide();
        private readonly Collider[] hits = new Collider[16];
        private PlayerController player;
        private CharacterController cc;
        private Transform visual;
        private MountAnimal mount;
        private float standHeight, standRadius, standStep;
        private Vector3 standCenter;
        private float attackAt = -1f, nextAttack;

        public MountRide State => ride;
        public bool IsMounted => ride.IsMounted && mount != null;
        public float SpeedMultiplier => IsMounted ? ride.Stats.speed : 1f;
        public float JumpMultiplier => IsMounted ? ride.Stats.jump : 1f;
        public float AccelerationMultiplier => IsMounted ? ride.Stats.acceleration : 1f;
        public bool IsCharging => attackAt > 0f || Time.time < nextAttack - 0.25f;

        private void Awake()
        {
            player = GetComponent<PlayerController>();
            cc = GetComponent<CharacterController>();
            standHeight = cc.height; standCenter = cc.center; standRadius = cc.radius; standStep = cc.stepOffset;
        }

        private void Start() => visual = player.VisualRoot;

        public void TryRide(MountAnimal animal)
        {
            if (animal == null || animal == mount) return;
            if (!ride.TryMount(animal.Kind, Time.time)) return;
            if (mount != null) mount.Release(false);
            mount = animal;
            mount.Carry(transform);
            var s = MountStats.For(animal.Kind);
            float h = s.seatHeight + riderTorso;
            cc.height = h; cc.radius = 0.6f; cc.stepOffset = 0.6f;
            cc.center = new Vector3(0f, h * 0.5f + 0.02f, 0f);
            player.Anim?.SetBool(RidingId, true);
            GameEvents.PlaySfx("bounce");
            GameEvents.RaiseMountChanged(true, false);
        }

        /// <summary>Called by the player when something hurts him.</summary>
        public HitOutcome Hit()
        {
            var o = ride.TakeHit(Time.time);
            if (o == HitOutcome.ThrownOff) GetOff(true);
            return o;
        }

        public void Dismount()
        {
            if (!ride.Dismount(Time.time)) return;
            GetOff(false);
        }

        /// <summary>Death, respawn or a scene change: the animal goes back to its post.</summary>
        public void ForceOff()
        {
            ride.Reset();
            if (mount == null) return;
            var m = mount;
            GetOff(false);
            m.SendHome();
        }

        private void GetOff(bool thrown)
        {
            if (mount != null) mount.Release(thrown);
            mount = null;
            attackAt = -1f;
            cc.height = standHeight; cc.center = standCenter; cc.radius = standRadius; cc.stepOffset = standStep;
            if (visual != null) visual.localPosition = Vector3.zero;
            player.Anim?.SetBool(RidingId, false);
            if (thrown) player.Bounce(1.4f);
            GameEvents.RaiseMountChanged(false, thrown);
        }

        public void Jump() { if (mount != null) mount.Jump(); }

        /// <summary>A tap while riding: the animal butts whatever is in front of it.</summary>
        public bool Attack()
        {
            if (!IsMounted || Time.time < nextAttack) return false;
            nextAttack = Time.time + 0.7f;
            attackAt = Time.time + 0.18f;
            mount.Attack();
            GameEvents.PlaySfx("swing");
            return true;
        }

        private void Update()
        {
            ride.Tick(Time.time);
            if (ride.Phase == MountPhase.OnFoot && mount != null) GetOff(false);
            if (attackAt > 0f && Time.time >= attackAt)
            {
                attackAt = -1f;
                Vector3 c = transform.position + transform.forward * 1.7f + Vector3.up * 1f;
                int n = Physics.OverlapSphereNonAlloc(c, 1.4f, hits, ~0, QueryTriggerInteraction.Collide);
                for (int i = 0; i < n; i++)
                {
                    if (hits[i].transform.IsChildOf(transform)) continue;
                    var d = hits[i].GetComponentInParent<IDamageable>();
                    if (d != null && d.CanBeDamaged) d.TakeDamage(new DamageInfo(1, DamageKind.Melee, transform.position, gameObject));
                }
            }
            if (mount != null)
            {
                var v = player.Velocity;
                v.y = 0f;
                mount.Animate(v.magnitude / (7f * ride.Stats.speed), cc.isGrounded);
            }
        }

        private void LateUpdate()
        {
            if (mount == null || visual == null) return;
            // Follow the saddle so Aron rocks with the gait.
            visual.position = mount.Seat.position - Vector3.up * hipHeight;
        }

    }
}
