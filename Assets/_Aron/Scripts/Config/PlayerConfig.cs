// (c) Ahmed Alamrani. All rights reserved.
using UnityEngine;

namespace Aron.Config
{
    /// <summary>
    /// Player settings that are not game feel: whether double jump exists, the slide, and the damage blink. Everything that decides how the hero
    /// moves (run, jump, air control, spin, bounce...) moved to <see cref="GameFeelConfig"/> / Resources/Config/game_feel.json.
    /// </summary>
    [CreateAssetMenu(menuName = "Aron/Player Config", fileName = "PlayerConfig")]
    public class PlayerConfig : ScriptableObject
    {
        [Header("Jump")] public bool allowDoubleJump = true;
        [Header("Slide")] public float slideDuration = 0.6f;
        public float slideSpeed = 11f;
        public float slideCooldown = 0.4f;
        public float slideHeight = 0.9f;
        [Header("Damage")] public float invulnerableTime = 1.5f;
    }
}
