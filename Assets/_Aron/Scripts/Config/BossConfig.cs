// (c) Ahmed Alamrani. All rights reserved.
using System;
using UnityEngine;

namespace Aron.Config
{
    [Serializable]
    public class BossPhase
    {
        [Tooltip("Phase starts when health fraction drops to/below this.")]
        [Range(0f, 1f)] public float healthThreshold = 1f;
        public float moveSpeed = 3f;
        public float chargeSpeed = 12f;
        public float attackInterval = 3f;
        public bool useShockwave;
        public bool summonMinions;
        public string tauntLineAr = "";
        [Tooltip("charge, wave, volley, leap, dash, summon. Empty means the old Riyadh mix: charge, plus wave if useShockwave.")]
        public string[] attacks = new string[0];
    }

    [CreateAssetMenu(menuName = "Aron/Boss Config", fileName = "BossConfig")]
    public class BossConfig : ScriptableObject
    {
        public string bossNameAr = "د. دحيم";
        public string stunLineAr = "آخ! راسي...";
        public string defeatLineAr = "خلاص! الرياض لك يا بطل!";
        public Color projectileColor = new Color(1f, 0.6f, 0.1f);
        public int maxHealth = 9;
        public float stunDuration = 2.2f;
        public float invulnerableAfterHit = 0.6f;
        public float shockwaveRadius = 9f;
        public float shockwaveSpeed = 8f;
        public BossPhase[] phases =
        {
            new BossPhase { healthThreshold = 1f, moveSpeed = 3f, chargeSpeed = 11f, attackInterval = 3.2f, tauntLineAr = "ما راح تعدي الرياض يا أرون!" },
            new BossPhase { healthThreshold = 0.66f, moveSpeed = 4f, chargeSpeed = 14f, attackInterval = 2.6f, useShockwave = true, tauntLineAr = "الحين بتشوف قوتي!" },
            new BossPhase { healthThreshold = 0.33f, moveSpeed = 5f, chargeSpeed = 16f, attackInterval = 2f, useShockwave = true, summonMinions = true, tauntLineAr = "يا روبوتاتي! ساعدوني!" },
        };
    }
}
