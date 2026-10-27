// (c) Ahmed Alamrani. All rights reserved.
using System;
using System.Collections.Generic;

namespace Aron.Logic.Combat
{
    public enum TargetKind { Enemy, Boss, Breakable, Animal, Bird, Companion, Hero, Civilian }

    /// <summary>A thing near the tap or in front of the hero, as the input layer sees it. Screen position is in pixels.</summary>
    public struct AimCandidate
    {
        public string id;
        public TargetKind kind;
        public float screenX, screenY;
        /// <summary>Metres from the hero, on the ground plane.</summary>
        public float distance;
        /// <summary>Degrees between where the hero faces and the direction to this thing (0 = dead ahead, sign ignored).</summary>
        public float angle;
    }

    [Serializable]
    public class AimAssistFile
    {
        /// <summary>Melee: targets inside this range and cone get a turn-to-face snap.</summary>
        public float meleeRange = 2.6f, meleeConeDeg = 70f, meleeMaxSnapDeg = 35f;
        /// <summary>Slingshot: a tap this close (in dp, so phones match) to a target counts as a hit on it.</summary>
        public float slingRadiusDp = 48f, slingMaxRange = 14f;
        /// <summary>Strength multiplier by difficulty. Hard keeps a little help so touch stays fair.</summary>
        public float easyScale = 1.4f, normalScale = 1f, hardScale = 0.6f;
        /// <summary>Tie-breaks: a boss or a real enemy wins over a crate that is equally close.</summary>
        public float bossBonus = 0.2f, enemyBonus = 0.1f;

        public float Scale(string difficulty) => difficulty == "easy" ? easyScale : difficulty == "hard" ? hardScale : normalScale;

        public List<string> Validate()
        {
            var e = new List<string>();
            if (meleeRange < 1f || meleeRange > 4f) e.Add("melee range 1..4 m");
            if (meleeConeDeg < 20f || meleeConeDeg > 120f) e.Add("melee cone 20..120");
            if (meleeMaxSnapDeg < 5f || meleeMaxSnapDeg > 60f) e.Add("melee snap 5..60");
            if (slingRadiusDp < 20f || slingRadiusDp > 90f) e.Add("sling radius 20..90 dp");
            if (hardScale <= 0f || hardScale > normalScale || normalScale > easyScale) e.Add("scale order hard <= normal <= easy, hard above zero");
            if (easyScale > 2f) e.Add("assist stays light: easy scale at most 2");
            return e;
        }
    }

    /// <summary>
    /// Light aim assist on touch. Melee: a short turn toward the nearest target in front. Slingshot: a tap near a target counts as on it.
    /// Only enemies, bosses and breakables can be picked. Animals, birds, companions, the heroes and bystanders never are, however close.
    /// </summary>
    public static class AimAssist
    {
        public static bool Targetable(TargetKind k) => k == TargetKind.Enemy || k == TargetKind.Boss || k == TargetKind.Breakable;

        private static float Bonus(AimAssistFile f, TargetKind k) => k == TargetKind.Boss ? f.bossBonus : k == TargetKind.Enemy ? f.enemyBonus : 0f;

        /// <summary>Index of the candidate a slingshot tap should land on, or -1. <paramref name="strength"/> is the difficulty scale times one-hand scale.</summary>
        public static int PickSling(IList<AimCandidate> list, float tapX, float tapY, float dpi, AimAssistFile f, float strength)
        {
            if (list == null || strength <= 0f) return -1;
            float radius = f.slingRadiusDp * Math.Max(dpi, 1f) / 160f * strength;
            int best = -1; float bestScore = float.MaxValue;
            for (int i = 0; i < list.Count; i++)
            {
                var c = list[i];
                if (!Targetable(c.kind) || c.distance > f.slingMaxRange) continue;
                float dx = c.screenX - tapX, dy = c.screenY - tapY;
                float d = (float)Math.Sqrt(dx * dx + dy * dy);
                if (d > radius) continue;
                float score = d / radius - Bonus(f, c.kind) + c.distance * 0.002f;
                if (score < bestScore) { bestScore = score; best = i; }
            }
            return best;
        }

        /// <summary>Index of the candidate melee should turn toward, or -1.</summary>
        public static int PickMelee(IList<AimCandidate> list, AimAssistFile f, float strength)
        {
            if (list == null || strength <= 0f) return -1;
            float range = f.meleeRange * (0.7f + 0.3f * strength), half = f.meleeConeDeg * 0.5f * Math.Min(strength, 1.5f);
            int best = -1; float bestScore = float.MaxValue;
            for (int i = 0; i < list.Count; i++)
            {
                var c = list[i];
                if (!Targetable(c.kind) || c.distance > range || Math.Abs(c.angle) > half) continue;
                float score = Math.Abs(c.angle) / half + c.distance / range - Bonus(f, c.kind);
                if (score < bestScore) { bestScore = score; best = i; }
            }
            return best;
        }

        /// <summary>The new facing (degrees) after a snap toward a target: never turns more than the cap, so it nudges instead of jerks.</summary>
        public static float SnapYaw(float yaw, float targetYaw, AimAssistFile f, float strength)
        {
            float diff = ((targetYaw - yaw + 540f) % 360f) - 180f;
            float cap = f.meleeMaxSnapDeg * Math.Min(strength, 1.5f);
            if (Math.Abs(diff) > cap) return yaw;   // too far off: the player meant something else
            return yaw + diff;
        }
    }

    [Serializable]
    public class HitStopFile
    {
        /// <summary>The freeze on a finishing blow or a boss phase break, 60 to 80 ms.</summary>
        public float finalHitMs = 80f, phaseBreakMs = 70f;
        /// <summary>No second freeze this soon after one, so a flurry never feels stuck.</summary>
        public float cooldownSeconds = 0.35f;
        /// <summary>Time scale while frozen (a near stop, not a hard stop).</summary>
        public float timeScale = 0.02f;

        public List<string> Validate()
        {
            var e = new List<string>();
            if (finalHitMs < 60f || finalHitMs > 80f) e.Add("final hit freeze 60..80 ms");
            if (phaseBreakMs < 60f || phaseBreakMs > 80f) e.Add("phase break freeze 60..80 ms");
            if (cooldownSeconds < 0.1f) e.Add("cooldown at least 0.1 s");
            if (timeScale < 0f || timeScale > 0.1f) e.Add("time scale 0..0.1");
            return e;
        }
    }

    /// <summary>When to freeze the game for a beat on the hit that matters.</summary>
    public class HitStop
    {
        public const string FinalHit = "final_hit", PhaseBreak = "phase_break";
        /// <summary>Haptic fired with the freeze (see Config/haptics.json).</summary>
        public const string Haptic = "hit_stop";

        private readonly HitStopFile f;
        private double lastAt = -100.0;
        public HitStop(HitStopFile file) { f = file ?? new HitStopFile(); }

        /// <summary>
        /// What to do on a hit that matters: freeze for Seconds (0 for none) and tick the haptic. Paused games and the cooldown give nothing;
        /// reduce-motion keeps the haptic but drops the freeze.
        /// </summary>
        public HitStopResult Trigger(string kind, double now, bool paused, bool reduceMotion)
        {
            if (paused || now - lastAt < f.cooldownSeconds) return default;
            float ms = kind == FinalHit ? f.finalHitMs : kind == PhaseBreak ? f.phaseBreakMs : 0f;
            if (ms <= 0f) return default;
            lastAt = now;
            return new HitStopResult { seconds = reduceMotion ? 0f : ms / 1000f, haptic = true };
        }
    }

    public struct HitStopResult { public float seconds; public bool haptic; }

    /// <summary>Resources/Config/combat_feel.json.</summary>
    [Serializable]
    public class CombatFeelFile
    {
        public AimAssistFile aim = new AimAssistFile();
        public HitStopFile hitStop = new HitStopFile();
        public List<string> Validate() { var e = aim.Validate(); e.AddRange(hitStop.Validate()); return e; }
    }
}
