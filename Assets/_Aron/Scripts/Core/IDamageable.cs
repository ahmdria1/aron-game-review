// (c) Ahmed Alamrani. All rights reserved.
using UnityEngine;

namespace Aron.Core
{
    public enum DamageKind { Melee, Spin, Slide, Stomp, Hazard }

    public struct DamageInfo
    {
        public int amount;
        public DamageKind kind;
        public Vector3 sourcePosition;
        public GameObject instigator;

        public DamageInfo(int amount, DamageKind kind, Vector3 sourcePosition, GameObject instigator)
        {
            this.amount = amount; this.kind = kind; this.sourcePosition = sourcePosition; this.instigator = instigator;
        }
    }

    /// <summary>Anything the player can hit: enemies, crates, boss.</summary>
    public interface IDamageable
    {
        bool CanBeDamaged { get; }
        void TakeDamage(DamageInfo info);
    }
}
