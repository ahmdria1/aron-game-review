// (c) Ahmed Alamrani. All rights reserved.
using Aron.Animals;
using Aron.Audio;
using Aron.Core;
using Aron.Fx;
using UnityEngine;

namespace Aron.Stages
{
    public enum BreakableKind { Jar, Wall, Metal, Glass }

    /// <summary>
    /// Clay jars, mud walls, metal barriers and shop glass. Jars and glass break to any hit. Mud walls need a charge
    /// (the horse, the camel or the oryx horn); metal barriers need a mount at full charge.
    /// </summary>
    public class Breakable : MonoBehaviour, IDamageable
    {
        [SerializeField] private BreakableKind kind = BreakableKind.Jar;
        [SerializeField] private int dates;
        private bool broken;

        public BreakableKind Kind => kind;
        public bool CanBeDamaged => !broken;

        public void Setup(BreakableKind k, int datesInside) { kind = k; dates = datesInside; }

        public static string SoundFor(BreakableKind k)
        {
            switch (k)
            {
                case BreakableKind.Wall: return "wall_crumble";
                case BreakableKind.Metal: return "metal_break";
                case BreakableKind.Glass: return "glass_break";
                default: return "jar_break";
            }
        }

        /// <summary>Whether a hit from this source is strong enough.</summary>
        public static bool Breaks(BreakableKind k, bool charging, bool mounted)
        {
            if (k == BreakableKind.Jar || k == BreakableKind.Glass) return true;
            if (k == BreakableKind.Wall) return charging;
            return charging && mounted;
        }

        public void TakeDamage(DamageInfo info)
        {
            var src = info.instigator;
            var rider = src != null ? src.GetComponentInParent<Rider>() : null;
            bool mounted = rider != null && rider.IsMounted;
            bool charging = (rider != null && rider.IsCharging) || info.kind == DamageKind.Hazard;
            if (!Breaks(kind, charging, mounted)) { FxPlayer.Play("hit", info.sourcePosition); return; }
            Smash();
        }

        /// <summary>Break it no matter what hit it (explosions, the oryx horn).</summary>
        public void Smash()
        {
            if (broken) return;
            broken = true;
            var c = transform.position + Vector3.up * 0.6f;
            Sfx.Play(SoundFor(kind), c);
            FxPlayer.Play(kind == BreakableKind.Jar || kind == BreakableKind.Glass ? "crate" : "dust", c);
            if (dates > 0 && GameManager.Instance != null) GameManager.Instance.AddDates(dates);
            Destroy(gameObject);
        }
    }
}
