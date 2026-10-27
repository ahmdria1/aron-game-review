// (c) Ahmed Alamrani. All rights reserved.
using System.Collections;
using Aron.Core;
using Aron.Feedback;
using Aron.Logic.Combat;
using Aron.Save;
using UnityEngine;

namespace Aron.Combat
{
    /// <summary>
    /// A 60 to 80 ms freeze with a haptic tick on the hit that matters: the boss's last hit and each boss phase break. Real-time, so it
    /// ends on schedule whatever the time scale is; skipped while paused; reduce-motion keeps the haptic only.
    /// </summary>
    public class HitStopRuntime : MonoBehaviour
    {
        private static HitStopRuntime instance;
        private static HitStop rules;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Hook()
        {
            GameEvents.BossDefeated -= OnDefeated; GameEvents.BossDefeated += OnDefeated;
            GameEvents.BossPhaseChanged -= OnPhase; GameEvents.BossPhaseChanged += OnPhase;
        }

        private static void OnDefeated() => Fire(HitStop.FinalHit);
        private static void OnPhase(int phase) { if (phase > 0) Fire(HitStop.PhaseBreak); }

        public static void Fire(string kind)
        {
            if (rules == null) rules = new HitStop(CombatFeelConfig.File.hitStop);
            bool paused = Time.timeScale <= 0.001f && Time.unscaledTime > 0f;
            var access = SaveSystem.Data.access;
            var r = rules.Trigger(kind, Time.unscaledTimeAsDouble, paused, Aron.Logic.Access.FlashSafety.MotionReduced(access));
            if (r.haptic) Haptics.Play(HitStop.Haptic);
            if (r.seconds <= 0f) return;
            if (instance == null) { var go = new GameObject("HitStop"); DontDestroyOnLoad(go); instance = go.AddComponent<HitStopRuntime>(); }
            instance.StopAllCoroutines();
            instance.StartCoroutine(instance.Freeze(r.seconds));
        }

        private IEnumerator Freeze(float seconds)
        {
            float before = Time.timeScale;
            Time.timeScale = Mathf.Min(before, CombatFeelConfig.File.hitStop.timeScale);
            yield return new WaitForSecondsRealtime(seconds);
            // Someone may have paused or opened a store while frozen: only restore a running game.
            if (Time.timeScale <= CombatFeelConfig.File.hitStop.timeScale + 0.001f) Time.timeScale = before;
        }

        private void OnDisable() { if (Time.timeScale < 0.05f && Time.timeScale > 0f) Time.timeScale = 1f; }
    }
}
