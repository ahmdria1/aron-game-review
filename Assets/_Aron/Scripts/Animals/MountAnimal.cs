// (c) Ahmed Alamrani. All rights reserved.
using Aron.Fx;
using Aron.Logic;
using Aron.Player;
using UnityEngine;

namespace Aron.Animals
{
    /// <summary>
    /// A camel or horse waiting at its post. Walk into it to climb on. When it throws you it skips aside and
    /// waits nearby so you can climb back on; run on without it and it goes back to its post.
    /// </summary>
    public class MountAnimal : MonoBehaviour
    {
        [SerializeField] private MountKind kind = MountKind.Camel;
        [SerializeField] private Transform seat;
        [SerializeField] private Animator animator;
        [SerializeField] private Collider pickup;

        private static readonly int SpeedId = Animator.StringToHash("Speed");
        private static readonly int GroundedId = Animator.StringToHash("Grounded");
        private static readonly int JumpId = Animator.StringToHash("Jump");
        private static readonly int HitId = Animator.StringToHash("Hit");
        private static readonly int AttackId = Animator.StringToHash("Attack");

        private Vector3 homePos;
        private Quaternion homeRot;
        private Transform homeParent;
        private Vector3 boltDir;
        private float thrownAt = -1f, thrownX;

        public MountKind Kind => kind;
        public Transform Seat => seat != null ? seat : transform;
        public bool Ridden { get; private set; }

        private void Awake()
        {
            homeParent = transform.parent;
            Fx.DropShadow.Ensure(gameObject);
            homePos = transform.position;
            homeRot = transform.rotation;
            if (animator == null) animator = GetComponentInChildren<Animator>();
        }

        private void OnTriggerEnter(Collider other) => TryCarry(other);
        private void OnTriggerStay(Collider other) => TryCarry(other);

        private void TryCarry(Collider other)
        {
            if (Ridden || thrownAt > 0f && LooseMount.At(Time.time - thrownAt, 0f) != LooseState.Ready) return;
            var rider = other.GetComponentInParent<Rider>();
            if (rider != null) rider.TryRide(this);
        }

        public void Carry(Transform player)
        {
            Ridden = true;
            thrownAt = -1f;
            if (pickup != null) pickup.enabled = false;
            transform.SetParent(player, false);
            transform.localPosition = Vector3.zero;
            transform.localRotation = Quaternion.identity;
        }

        /// <summary>Off the rider. Thrown: it rears, runs off and comes home later. Otherwise it stays put.</summary>
        public void Release(bool thrown)
        {
            Ridden = false;
            transform.SetParent(homeParent, true);
            transform.rotation = Quaternion.Euler(0f, transform.eulerAngles.y, 0f);
            if (thrown)
            {
                Trigger(HitId);
                boltDir = Quaternion.Euler(0f, Random.value > 0.5f ? 60f : -60f, 0f) * transform.forward;
                thrownAt = Time.time;
                thrownX = transform.position.x;
            }
            else if (pickup != null) pickup.enabled = true;
            Animate(0f, true);
        }

        public void Animate(float speed01, bool grounded)
        {
            if (animator == null || animator.runtimeAnimatorController == null) return;
            animator.SetFloat(SpeedId, speed01);
            animator.SetBool(GroundedId, grounded);
        }

        public void Jump() => Trigger(JumpId);
        public void Attack() => Trigger(AttackId);

        private void Trigger(int id)
        {
            if (animator != null && animator.runtimeAnimatorController != null) animator.SetTrigger(id);
        }

        private void Update()
        {
            if (Ridden || thrownAt < 0f) return;
            var p = PlayerController.Active;
            float ahead = p != null ? p.transform.position.z - transform.position.z : 0f;
            switch (LooseMount.At(Time.time - thrownAt, ahead))
            {
                case LooseState.Bolting:
                    var pos = transform.position + boltDir * 4f * Time.deltaTime;
                    pos.x = thrownX + LooseMount.ClampSide(pos.x - thrownX);
                    transform.position = pos;
                    transform.rotation = Quaternion.RotateTowards(transform.rotation, Quaternion.LookRotation(boltDir), 360f * Time.deltaTime);
                    Animate(1f, true);
                    break;
                case LooseState.GoHome:
                    FxPlayer.Play("poof", transform.position + Vector3.up);
                    SendHome();
                    FxPlayer.Play("poof", homePos + Vector3.up);
                    break;
                case LooseState.Ready:
                    // Standing by, facing down the track, ready to be climbed on again.
                    transform.rotation = Quaternion.RotateTowards(transform.rotation, Quaternion.Euler(0f, homeRot.eulerAngles.y, 0f), 180f * Time.deltaTime);
                    if (pickup != null && !pickup.enabled) pickup.enabled = true;
                    Animate(0f, true);
                    break;
                default:
                    Animate(0f, true);
                    break;
            }
        }

        /// <summary>Death or restart: straight back to the post.</summary>
        public void SendHome()
        {
            Ridden = false;
            transform.SetParent(homeParent, true);
            transform.SetPositionAndRotation(homePos, homeRot);
            thrownAt = -1f;
            if (pickup != null) pickup.enabled = true;
            Animate(0f, true);
        }
    }
}
