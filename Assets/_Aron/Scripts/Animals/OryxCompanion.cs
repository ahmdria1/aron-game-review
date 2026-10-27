// (c) Ahmed Alamrani. All rights reserved.
using System.Collections.Generic;
using Aron.Data;
using Aron.Enemies;
using Aron.Heroes;
using Aron.Localization;
using Aron.Logic;
using Aron.Save;
using Aron.Stages;
using Aron.UI;
using UnityEngine;

namespace Aron.Animals
{
    /// <summary>
    /// Noura's Arabian oryx. Trots beside her, horn-charges jars and mud walls in her way, runs ahead toward hidden
    /// dates and secret paths, and snorts when an enemy is close.
    /// </summary>
    public class OryxCompanion : MonoBehaviour
    {
        private static readonly int SpeedId = Animator.StringToHash("Speed");
        private static readonly int AttackId = Animator.StringToHash("Attack");

        [SerializeField] private Animator animator;
        [SerializeField] private float trotSpeed = 9f;

        private OryxSense sense;
        private Breakable target;
        private Vector3 vel, lastPos;
        private float nextSnort, leadShownAt = -10f;
        private readonly List<float> leadZ = new List<float>();
        private readonly List<Vector3> leadAt = new List<Vector3>();
        private readonly Collider[] near = new Collider[24];

        private void Start()
        {
            Load();
            Fx.DropShadow.Ensure(gameObject);
            lastPos = transform.position;
        }

        public void Load() => sense = new OryxSense(CompanionUpgrades.Current(GameData.Companions, SaveSystem.Data, CompanionIds.Oryx));

        private float nudgeUntil = -1f;

        /// <summary>Noura has sat down after a knock: the oryx comes over and nudges her with its head.</summary>
        public void Nudge(float seconds) => nudgeUntil = Time.time + seconds;

        private void Update()
        {
            var team = HeroTeam.Instance;
            var noura = team != null ? team.Noura : null;
            if (noura == null || sense == null) return;
            float now = Time.time, dt = Time.deltaTime;

            if (now < nudgeUntil)
            {
                Vector3 beside = noura.position + noura.right * 0.95f + noura.forward * 0.2f;
                transform.position = Vector3.MoveTowards(transform.position, beside, trotSpeed * 1.5f * dt);
                Vector3 look = noura.position - transform.position; look.y = 0f;
                // Head down and up against her shoulder.
                float bob = Mathf.Max(0f, Mathf.Sin(now * 7f)) * 18f;
                if (look.sqrMagnitude > 0.01f) transform.rotation = Quaternion.LookRotation(look) * Quaternion.Euler(bob, 0f, 0f);
                Snap();
                return;
            }

            Vector3 spot = noura.position + noura.right * OryxSense.SideOffset(noura.position.x, 0f) - noura.forward * 0.5f;
            if (target != null && !target.CanBeDamaged) target = null;
            if (target == null && sense.Horn.Ready(now)) target = FindTarget(noura);

            if (target != null)
            {
                // Horn charge: straight at it, quick.
                if (!sense.Horn.Active(now)) { sense.Horn.TryStart(now); if (animator != null) animator.SetTrigger(AttackId); }
                Vector3 to = target.transform.position - transform.position; to.y = 0f;
                transform.position = Vector3.MoveTowards(transform.position, target.transform.position, trotSpeed * 1.8f * dt);
                if (to.sqrMagnitude > 0.01f) transform.rotation = Quaternion.LookRotation(to);
                if (to.magnitude < 1.1f) { target.Smash(); target = null; }
                else if (!sense.Horn.Active(now)) target = null;
            }
            else
            {
                Vector3 lead = Lead(noura);
                if (lead != Vector3.zero) spot = Vector3.Lerp(spot, lead, 0.35f);
                if ((spot - transform.position).sqrMagnitude > 900f) transform.position = spot;
                Vector3 next = Vector3.SmoothDamp(transform.position, spot, ref vel, 0.18f, trotSpeed * 1.4f);
                Vector3 flat = next - transform.position; flat.y = 0f;
                if (flat.sqrMagnitude > 1e-5f) transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(flat), 8f * dt);
                transform.position = next;
            }
            Snap();
            Sniff(noura, now, team.NouraActive);

            if (animator != null)
            {
                float speed = dt > 0f ? (transform.position - lastPos).magnitude / dt : 0f;
                animator.SetFloat(SpeedId, Mathf.Clamp01(speed / trotSpeed));
            }
            lastPos = transform.position;
        }

        private Breakable FindTarget(Transform noura)
        {
            int n = Physics.OverlapSphereNonAlloc(noura.position + noura.forward * 3f, 3.5f, near, ~0, QueryTriggerInteraction.Collide);
            Breakable best = null; float bestD = float.MaxValue;
            for (int i = 0; i < n; i++)
            {
                var b = near[i].GetComponentInParent<Breakable>();
                if (b == null || !b.CanBeDamaged || (b.Kind != BreakableKind.Wall && b.Kind != BreakableKind.Jar)) continue;
                float d = (b.transform.position - noura.position).sqrMagnitude;
                if (d < bestD) { best = b; bestD = d; }
            }
            return best;
        }

        // Somewhere worth going: a hidden basket of dates or a secret route.
        private Vector3 Lead(Transform noura)
        {
            leadZ.Clear(); leadAt.Clear();
            foreach (var b in DateBasket.Open) { leadZ.Add(b.transform.position.z); leadAt.Add(b.transform.position); }
            foreach (var s in SecretRoute.All) { leadZ.Add(s.transform.position.z); leadAt.Add(s.transform.position); }
            int i = sense.Lead(noura.position.z, leadZ);
            if (i < 0) return Vector3.zero;
            if (Time.time - leadShownAt > 15f && HeroTeam.Instance != null && HeroTeam.Instance.NouraActive)
            {
                leadShownAt = Time.time;
                GameUI.Banner(Strings.Get("oryx_lead"), 1.6f);
                Audio.AudioManager.PlayVoice(AudioCues.VoNouraCallOryx, "noura");
            }
            return leadAt[i];
        }

        private void Sniff(Transform noura, float now, bool nouraPlayed)
        {
            if (now < nextSnort) return;
            float nearest = -1f;
            foreach (var e in Enemy.All)
            {
                if (e == null || !e.CanBeDamaged) continue;
                float d = Vector3.Distance(e.transform.position, noura.position);
                if (nearest < 0f || d < nearest) nearest = d;
            }
            if (!sense.SensesEnemy(nearest)) return;
            nextSnort = now + 8f;
            Core.GameEvents.PlaySfx("enemy");
            if (nouraPlayed) GameUI.Banner(Strings.Get("oryx_enemy"), 1.4f);
        }

        private void Snap()
        {
            var p = transform.position;
            if (Physics.Raycast(p + Vector3.up * 2f, Vector3.down, out var hit, 6f, ~0, QueryTriggerInteraction.Ignore)
                && hit.collider.GetComponentInParent<Player.PlayerController>() == null)
                transform.position = new Vector3(p.x, hit.point.y, p.z);
        }
    }
}
