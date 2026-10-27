// (c) Ahmed Alamrani. All rights reserved.
using System.Collections;
using Aron.Core;
using Aron.Level;
using Aron.Logic;
using Aron.Player;
using UnityEngine;

namespace Aron.Characters
{
    /// <summary>
    /// Turns gameplay into feelings for the player character: streaks make them happy, losing a life makes them sad,
    /// pits and the boss gate scare them, landmarks impress them and the last life keeps them worried.
    /// </summary>
    public class EmotionDirector : MonoBehaviour
    {
        [SerializeField] private float pitLookAhead = 1.4f;
        [SerializeField] private float pitDepth = 6f;
        [SerializeField] private float landmarkCheckInterval = 0.3f;

        private readonly StreakCounter coinStreak = new StreakCounter(2f, 4);
        private readonly StreakCounter crateCombo = new StreakCounter(2.5f, 3);
        private PlayerController player;
        private CharacterEmotion hero;
        private Camera cam;
        private int lastDates, lastLives = -1;
        private float pitCooldown, landmarkTimer, worryNag;
        private bool over;

        private void OnEnable()
        {
            GameEvents.DatesChanged += OnDates;
            GameEvents.LivesChanged += OnLives;
            GameEvents.PlayerDied += OnPlayerDied;
            GameEvents.GameOver += OnGameOver;
            GameEvents.LevelCompleted += OnLevelCompleted;
            GameEvents.CrateBroken += OnCrate;
            GameEvents.BossEncounterStarted += OnBossEncounter;
            GameEvents.BossHealthChanged += OnBossHit;
            GameEvents.BossDefeated += OnBossDefeated;
            GameEvents.MountChanged += OnMount;
        }

        private void OnDisable()
        {
            GameEvents.DatesChanged -= OnDates;
            GameEvents.LivesChanged -= OnLives;
            GameEvents.PlayerDied -= OnPlayerDied;
            GameEvents.GameOver -= OnGameOver;
            GameEvents.LevelCompleted -= OnLevelCompleted;
            GameEvents.CrateBroken -= OnCrate;
            GameEvents.BossEncounterStarted -= OnBossEncounter;
            GameEvents.BossHealthChanged -= OnBossHit;
            GameEvents.BossDefeated -= OnBossDefeated;
            GameEvents.MountChanged -= OnMount;
        }

        private CharacterEmotion Hero
        {
            get
            {
                if (hero != null) return hero;
                if (player == null) player = FindFirstObjectByType<PlayerController>();
                if (player != null) hero = player.GetComponentInChildren<CharacterEmotion>();
                return hero;
            }
        }

        private void Feel(Emotion e, float duration, bool big = false)
        {
            var h = Hero;
            if (h != null) h.Feel(e, duration, big);
        }

        private void OnDates(int dates)
        {
            if (dates > lastDates && lastLives >= 0 && coinStreak.Hit(Time.time)) Feel(Emotion.Joy, 1.6f);
            lastDates = dates;
        }

        private void OnMount(bool mounted, bool thrown)
        {
            if (mounted) Feel(Emotion.Joy, 2f, true);
            else if (thrown) Feel(Emotion.Fear, 1.4f, true);
        }

        private void OnCrate(Vector3 _)
        {
            if (crateCombo.Hit(Time.time)) Feel(Emotion.Joy, 2f);
        }

        private void OnLives(int lives)
        {
            var h = Hero;
            if (h != null)
            {
                if (lives == 1 && lastLives > 1) h.Feel(Emotion.Fear, 1.6f);
                h.SetMood(lives == 1 && !over ? Emotion.Worry : Emotion.Neutral);
            }
            lastLives = lives;
        }

        private void OnPlayerDied()
        {
            coinStreak.Reset();
            crateCombo.Reset();
            StartCoroutine(HitThenSad());
        }

        private IEnumerator HitThenSad()
        {
            Feel(Emotion.Anger, 0.5f);
            yield return new WaitForSeconds(0.45f);
            if (!over) Feel(Emotion.Sadness, 2.4f);
        }

        private void OnGameOver()
        {
            over = true;
            var h = Hero;
            if (h == null) return;
            h.SetMood(Emotion.Sadness);
            StartCoroutine(GameOverSlump(h));
        }

        private IEnumerator GameOverSlump(CharacterEmotion h)
        {
            yield return new WaitForSecondsRealtime(1.2f);
            if (h != null) h.Feel(Emotion.Sadness, 999f, true);
        }

        private void OnLevelCompleted()
        {
            over = true;
            var h = Hero;
            if (h == null) return;
            h.SetMood(Emotion.Joy);
            h.Feel(Emotion.Joy, 999f, true);
        }

        private void OnBossEncounter() => Feel(Emotion.Fear, 2.2f);

        private void OnBossHit(float health)
        {
            if (health > 0f && health < 1f) Feel(Emotion.Anger, 1.4f);
        }

        private void OnBossDefeated() => Feel(Emotion.Joy, 3f, true);

        private void Update()
        {
            if (over || Time.timeScale <= 0f || Hero == null || player == null) return;
            float dt = Time.deltaTime;
            pitCooldown -= dt;
            if (pitCooldown <= 0f && NearPit())
            {
                Feel(Emotion.Fear, 1.2f);
                pitCooldown = 6f;
            }

            landmarkTimer -= dt;
            if (landmarkTimer <= 0f)
            {
                landmarkTimer = landmarkCheckInterval;
                CheckLandmarks();
            }

            if (hero.Mood == Emotion.Worry)
            {
                worryNag -= dt;
                if (worryNag <= 0f) { hero.Feel(Emotion.Worry, 1.8f); worryNag = 12f; }
            }
        }

        private bool NearPit()
        {
            var cc = player.GetComponent<CharacterController>();
            if (cc == null || !cc.isGrounded) return false;
            Vector3 v = player.Velocity;
            v.y = 0f;
            if (v.sqrMagnitude < 1f) return false;
            Vector3 probe = player.transform.position + v.normalized * pitLookAhead + Vector3.up * 0.5f;
            return !Physics.Raycast(probe, Vector3.down, pitDepth + 0.5f, ~0, QueryTriggerInteraction.Ignore);
        }

        private void CheckLandmarks()
        {
            if (cam == null) cam = Camera.main;
            if (cam == null) return;
            foreach (var l in Landmark.All)
            {
                if (l == null || l.Seen) continue;
                Vector3 focus = l.Focus;
                if ((focus - player.transform.position).sqrMagnitude > l.ViewDistance * l.ViewDistance) continue;
                Vector3 vp = cam.WorldToViewportPoint(focus);
                if (vp.z <= 0f || vp.x < 0.1f || vp.x > 0.9f || vp.y < 0.05f || vp.y > 0.95f) continue;
                l.Seen = true;
                Feel(Emotion.Admiration, 2.5f);
                break;
            }
        }
    }
}
