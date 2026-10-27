// (c) Ahmed Alamrani. All rights reserved.
using System;
using System.Collections.Generic;
using Aron.Config;
using Aron.Core;
using Aron.Fx;
using Aron.Logic.GameFeel;
using Aron.Save;
using UnityEngine;

namespace Aron.Player
{
    /// <summary>
    /// Timed sphere hitbox in front of the character (agal swing / handbag swing).
    /// Each target is hit at most once per swing. The timing is in 60 Hz frames (startup, active, recovery; see Game Feel > Melee), counted by a
    /// <see cref="FixedStepper"/> so it is the same on a 30 fps phone and a 120 Hz one. A hit freezes the attacker only for a few frames
    /// (hit-stop), a second tap near the end of the swing is buffered and starts the next one, and a jump cancels the recovery.
    /// The weapon definition's own time fields are no longer used for timing.
    /// </summary>
    public class MeleeAttack : MonoBehaviour
    {
        [SerializeField] private WeaponDefinition weapon;
        [SerializeField] private LayerMask hitMask = ~0;
        [Tooltip("Optional visual (agal / handbag mesh) rotated during the swing.")]
        [SerializeField] private Transform weaponVisual;
        [Tooltip("Rotate the weapon visual through an arc. Off when the character animation already swings it.")]
        [SerializeField] private bool proceduralSwing = true;

        private readonly Collider[] buffer = new Collider[24];
        private readonly HashSet<IDamageable> hitThisSwing = new HashSet<IDamageable>();
        private readonly AttackTimeline line = new AttackTimeline();
        private readonly FixedStepper stepper = new FixedStepper();
        private bool hitStopGiven;
        private Quaternion visualRest;
        private TrailRenderer trail;

        public bool IsSwinging => line.Running;
        public AttackPhase Phase => line.Phase;
        /// <summary>True from the jump-cancel frame to the end of the swing.</summary>
        public bool CanJumpCancel => line.CanJumpCancel;
        /// <summary>The attacker is frozen for the hit-stop (the world is not).</summary>
        public bool Frozen => line.Frozen;
        /// <summary>Raised when a swing begins, including a buffered one that follows the last.</summary>
        public event Action SwingStarted;
        public WeaponDefinition Weapon => weapon;

        public void Configure(WeaponDefinition def, Transform visual, bool animated = false)
        {
            weapon = def;
            weaponVisual = visual;
            proceduralSwing = !animated;
            CacheVisual();
        }

        private void Awake() => CacheVisual();

        private void CacheVisual()
        {
            trail = null;
            if (weaponVisual == null) return;
            visualRest = weaponVisual.localRotation;
            trail = weaponVisual.GetComponentInChildren<TrailRenderer>(true);
            if (trail != null) trail.emitting = false;
        }

        /// <summary>Starts a swing; while one is running, a press in the last few frames is remembered and starts the next one at once.</summary>
        public bool TryStartSwing()
        {
            if (weapon == null) return false;
            if (IsSwinging) { line.PressAttack(FeelSettings.V.meleeBufferFrames); return false; }
            Begin();
            return true;
        }

        /// <summary>A jump during recovery ends the swing at once.</summary>
        public void CancelForJump()
        {
            if (!IsSwinging) return;
            End();
        }

        private void Begin()
        {
            line.Start(AttackFrames.Melee(FeelSettings.V));
            stepper.Reset();
            hitThisSwing.Clear();
            hitStopGiven = false;
            GameEvents.PlaySfx(weapon.sfxId);
            if (trail != null) { trail.Clear(); trail.emitting = true; }
            CountryTrail();
        }

        private void End()
        {
            line.Cancel();
            if (weaponVisual != null && proceduralSwing) weaponVisual.localRotation = visualRest;
            if (trail != null) trail.emitting = false;
        }

        private void Update()
        {
            if (!IsSwinging) return;
            int steps = stepper.Advance(Time.deltaTime);
            for (int i = 0; i < steps && IsSwinging; i++)
            {
                if (line.Phase == AttackPhase.Active && !line.Frozen) DoHits();
                bool buffered = line.BufferedNext;
                if (line.Tick())
                {
                    End();
                    if (buffered) { Begin(); SwingStarted?.Invoke(); }
                    break;
                }
            }
            if (IsSwinging && weaponVisual != null && proceduralSwing) weaponVisual.localRotation = visualRest * Quaternion.Euler(0f, SwingAngle(), 0f);
        }

        // Wind back during startup, sweep through during the active frames, ease home in recovery.
        private float SwingAngle()
        {
            var f = line.Frames;
            int n = line.Frame;
            if (n < f.Startup) return Mathf.Lerp(-120f, -150f, f.Startup > 0 ? n / (float)f.Startup : 1f);
            n -= f.Startup;
            if (n < f.Active) return Mathf.Lerp(-150f, 120f, n / (float)Mathf.Max(1, f.Active));
            n -= f.Active;
            return Mathf.Lerp(120f, 0f, f.Recovery > 0 ? n / (float)f.Recovery : 1f);
        }

        // The country's attack trail (cherry petals in Japan, marigolds in India...): tints the swing trail and
        // plays the matching particle burst when the FX library has one.
        private void CountryTrail()
        {
            string id = Stages.StageSession.TrailVfx;
            if (string.IsNullOrEmpty(id)) return;
            if (trail != null && ColorUtility.TryParseHtmlString(Logic.Culture.CultureRules.TrailColour(id), out var c))
            {
                trail.startColor = c;
                trail.endColor = new Color(c.r, c.g, c.b, 0f);
            }
            var at = weaponVisual != null ? weaponVisual.position : transform.position + transform.forward * 0.6f + Vector3.up;
            FxPlayer.Play("trail_" + id, at, transform.rotation);
        }

        private void DoHits()
        {
            Vector3 centre = transform.TransformPoint(weapon.hitboxOffset);
            int n = Physics.OverlapSphereNonAlloc(centre, weapon.hitboxRadius, buffer, hitMask, QueryTriggerInteraction.Collide);
            for (int i = 0; i < n; i++)
            {
                if (buffer[i].transform.IsChildOf(transform)) continue;
                var d = buffer[i].GetComponentInParent<IDamageable>();
                if (d == null || !d.CanBeDamaged || hitThisSwing.Contains(d)) continue;
                hitThisSwing.Add(d);
                FxPlayer.Play("hit", buffer[i].ClosestPoint(centre));
                FollowCamera.Shake(0.2f);
                if (!hitStopGiven)
                {
                    // Attacker-only freeze: the swing holds for a few frames, the enemy and the world carry on.
                    hitStopGiven = true;
                    var save = SaveSystem.Data;
                    bool reduce = save != null && save.access != null && save.access.reduceMotion;
                    line.Hit(Mathf.RoundToInt(FeelSettings.V.hitStopFrames * (reduce ? FeelSettings.V.reduceMotionScale : 1f)));
                }
                d.TakeDamage(new DamageInfo(weapon.damage, DamageKind.Melee, transform.position, gameObject));
            }
        }

        private void OnDrawGizmosSelected()
        {
            if (weapon == null) return;
            Gizmos.color = IsSwinging ? Color.red : new Color(1f, 0.5f, 0f, 0.5f);
            Gizmos.DrawWireSphere(transform.TransformPoint(weapon.hitboxOffset), weapon.hitboxRadius);
        }
    }
}
