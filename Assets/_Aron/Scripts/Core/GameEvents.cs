// (c) Ahmed Alamrani. All rights reserved.
using System;
using UnityEngine;

namespace Aron.Core
{
    /// <summary>
    /// Central, decoupled event hub. Systems publish here; UI/audio/save listen.
    /// Subscribers must unsubscribe in OnDisable/OnDestroy. <see cref="ClearAll"/> is for tests only.
    /// </summary>
    public static class GameEvents
    {
        public static event Action<int> DatesChanged;
        public static event Action<int> LivesChanged;
        public static event Action<Vector3> CheckpointReached;
        public static event Action PlayerDied;
        public static event Action GameOver;
        public static event Action LevelCompleted;
        public static event Action<bool> PauseChanged;
        public static event Action<int> BossPhaseChanged;
        public static event Action<float> BossHealthChanged; // normalized 0..1
        public static event Action BossDefeated;
        public static event Action BossEncounterStarted;
        public static event Action<Vector3> CrateBroken;
        public static event Action<string> SfxRequested;
        /// <summary>A spoken line started: (voice line id, speaker). Raised even when no recording exists, so captions still show.</summary>
        public static event Action<string, string> VoiceSpoken;
        /// <summary>A world sound from the sound library was asked for (crates, thunder, boss hits).</summary>
        public static event Action<string> SoundPlayed;
        /// <summary>(mounted, thrown): climbed on, got off, or got thrown.</summary>
        public static event Action<bool, bool> MountChanged;
        public static event Action PlayerAttacked;
        /// <summary>Lives earned in one go (every 100 dates, or a life box).</summary>
        public static event Action<int> ExtraLife;
        /// <summary>Boxes broken so far and how many the stage has.</summary>
        public static event Action<int, int> BoxesChanged;
        /// <summary>Golden bisht layers on the hero and whether he is untouchable.</summary>
        public static event Action<int, bool> BishtChanged;
        /// <summary>A puzzle gate opened; the argument is the puzzle kind.</summary>
        public static event Action<string> PuzzleSolved;

        public static void RaiseDatesChanged(int v) => DatesChanged?.Invoke(v);
        public static void RaiseLivesChanged(int v) => LivesChanged?.Invoke(v);
        public static void RaiseCheckpointReached(Vector3 p) => CheckpointReached?.Invoke(p);
        public static void RaisePlayerDied() => PlayerDied?.Invoke();
        public static void RaiseGameOver() => GameOver?.Invoke();
        public static void RaiseLevelCompleted() => LevelCompleted?.Invoke();
        public static void RaisePauseChanged(bool p) => PauseChanged?.Invoke(p);
        public static void RaiseBossPhaseChanged(int phase) => BossPhaseChanged?.Invoke(phase);
        public static void RaiseBossHealthChanged(float n) => BossHealthChanged?.Invoke(n);
        public static void RaiseBossDefeated() => BossDefeated?.Invoke();
        public static void RaiseBossEncounterStarted() => BossEncounterStarted?.Invoke();
        public static void RaiseCrateBroken(Vector3 p) => CrateBroken?.Invoke(p);
        public static void PlaySfx(string id) => SfxRequested?.Invoke(id);
        public static void RaiseVoiceSpoken(string id, string speaker) => VoiceSpoken?.Invoke(id, speaker);
        public static void RaiseSoundPlayed(string id) => SoundPlayed?.Invoke(id);
        public static void RaiseMountChanged(bool mounted, bool thrown) => MountChanged?.Invoke(mounted, thrown);
        public static void RaisePlayerAttacked() => PlayerAttacked?.Invoke();
        public static void RaiseExtraLife(int n) => ExtraLife?.Invoke(n);
        public static void RaiseBoxesChanged(int broken, int total) => BoxesChanged?.Invoke(broken, total);
        public static void RaiseBishtChanged(int layers, bool invincible) => BishtChanged?.Invoke(layers, invincible);
        public static void RaisePuzzleSolved(string kind) => PuzzleSolved?.Invoke(kind);

        public static void ClearAll()
        {
            DatesChanged = null; LivesChanged = null; CheckpointReached = null; PlayerDied = null;
            GameOver = null; LevelCompleted = null; PauseChanged = null; BossPhaseChanged = null;
            BossHealthChanged = null; BossDefeated = null; BossEncounterStarted = null; CrateBroken = null;
            MountChanged = null; PlayerAttacked = null; ExtraLife = null; BoxesChanged = null; BishtChanged = null;
            PuzzleSolved = null; VoiceSpoken = null; SoundPlayed = null;
        }
    }
}
