// (c) Ahmed Alamrani. All rights reserved.
using Aron.Core;
using Aron.Enemies;
using UnityEngine;

namespace Aron.Audio
{
    /// <summary>Plays the impact layers for game events that already exist, so gameplay code stays untouched.</summary>
    public static class SfxHooks
    {
        private static float lastBossHealth = 1f;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Hook()
        {
            GameEvents.BossHealthChanged -= OnBossHealth;
            GameEvents.BossHealthChanged += OnBossHealth;
            GameEvents.BossEncounterStarted -= OnBossStart;
            GameEvents.BossEncounterStarted += OnBossStart;
            GameEvents.BossDefeated -= OnBossDefeated;
            GameEvents.BossDefeated += OnBossDefeated;
        }

        private static void OnBossStart() => lastBossHealth = 1f;

        private static void OnBossHealth(float health)
        {
            if (health < lastBossHealth) Sfx.Play("boss_hit", BossPosition());
            lastBossHealth = health;
        }

        private static void OnBossDefeated() => Sfx.Play("explosion", BossPosition());

        private static Vector3 BossPosition()
        {
            var boss = Object.FindFirstObjectByType<RiyadhBoss>();
            if (boss != null) return boss.transform.position + Vector3.up;
            var cam = Camera.main;
            return cam != null ? cam.transform.position : Vector3.zero;
        }
    }
}
