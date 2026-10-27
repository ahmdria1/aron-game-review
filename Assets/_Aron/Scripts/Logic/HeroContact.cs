// (c) Ahmed Alamrani. All rights reserved.
namespace Aron.Logic
{
    public enum ContactResult { Nothing, EnemyDefeated, HeroHurt }

    /// <summary>
    /// What happens when the hero and an ordinary enemy touch. Landing on its head or hitting it while
    /// attacking finishes it in one go; walking into it from the side or the front costs the hero a life.
    /// </summary>
    public static class HeroContact
    {
        /// <summary>How far the hero's feet must be above the enemy's feet to count as landing on its head.</summary>
        public const float StompHeight = 0.6f;

        public static ContactResult Resolve(bool falling, float feetAboveEnemy, bool attacking, bool invincible, bool blinking)
        {
            if (falling && feetAboveEnemy > StompHeight) return ContactResult.EnemyDefeated;
            if (attacking || invincible) return ContactResult.EnemyDefeated;
            if (blinking) return ContactResult.Nothing;
            return ContactResult.HeroHurt;
        }
    }
}
