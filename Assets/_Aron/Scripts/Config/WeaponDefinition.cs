// (c) Ahmed Alamrani. All rights reserved.
using UnityEngine;

namespace Aron.Config
{
    /// <summary>Melee weapon tuning (Aron's agal, the heroine's handbag, ...).</summary>
    [CreateAssetMenu(menuName = "Aron/Weapon", fileName = "Weapon")]
    public class WeaponDefinition : ScriptableObject
    {
        public string weaponId = "agal";
        public string displayNameAr = "العقال";
        public int damage = 1;
        [Tooltip("Hitbox centre, local to the character (forward = +Z).")]
        public Vector3 hitboxOffset = new Vector3(0f, 1f, 1.1f);
        public float hitboxRadius = 1.0f;
        [Tooltip("No longer used: the swing timing is in frames in Game Feel (Melee group).")]
        public float activeStart = 0.08f;
        public float activeEnd = 0.25f;
        public float totalDuration = 0.4f;
        [Tooltip("Animator trigger fired on swing; ignored if the Animator lacks it.")]
        public string animatorTrigger = "Attack";
        public string sfxId = "spin";
    }
}
