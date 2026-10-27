// (c) Ahmed Alamrani. All rights reserved.
using Aron.Audio;
using Aron.Config;
using Aron.Logic;
using Aron.Player;
using Aron.Save;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Aron.Core
{
    /// <summary>Per-level game state: lives, dates, checkpoints, pause, win/lose.</summary>
    [DefaultExecutionOrder(-100)]
    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }

        [SerializeField] private LevelConfig levelConfig;
        [SerializeField] private PlayerController player;

        private PlayerProgress progress;

        public int Lives => progress.Lives;
        public int Dates => progress.Dates;
        public bool IsPaused { get; private set; }
        /// <summary>A store screen is open: time is stopped but the pause menu stays hidden.</summary>
        public bool InStore { get; private set; }
        public bool IsOver { get; private set; }
        public Vector3 RespawnPoint { get; private set; }
        public LevelConfig Level => levelConfig;
        /// <summary>Seconds of actual play in this run; stops while paused or in a store.</summary>
        public float RunSeconds { get; private set; }
        /// <summary>Boxes in this stage and how many got broken, for the end screen.</summary>
        public BoxTally Boxes { get; } = new BoxTally();
        /// <summary>Stars earned for the stage just finished (0..3 total) and how many of them are new this run.</summary>
        public Stages.StageSession.StarResult LastRunStars { get; private set; }
        private readonly MercyRule mercy = new MercyRule();

        /// <summary>Stage scenes pick their level at load time, before this wakes up.</summary>
        public void SetLevel(LevelConfig config) => levelConfig = config;

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            Time.timeScale = 1f;
            progress = new PlayerProgress(levelConfig != null ? levelConfig.startLives : 3,
                                          levelConfig != null ? levelConfig.datesPerExtraLife : 100);
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        private void Start()
        {
            if (player == null) player = FindFirstObjectByType<PlayerController>();
            if (player != null) RespawnPoint = player.transform.position;
            trail = new CheckpointTrail(ToSpot(RespawnPoint));
            // Stage scenes set the total from their layout; a hand-built level counts what it has.
            if (Boxes.Total == 0) Boxes.SetTotal(FindObjectsByType<Level.Crate>(FindObjectsInactive.Exclude, FindObjectsSortMode.None).Length);
            mercy.Reached(CheckpointKey);
            if (levelConfig != null) AudioManager.PlayMusic(levelConfig.musicTrack);
            GameEvents.RaiseLivesChanged(Lives);
            GameEvents.RaiseDatesChanged(Dates);
        }

        private void Update()
        {
            if (!IsOver) RunSeconds += Time.deltaTime;
        }

        private void OnApplicationPause(bool paused)
        {
            if (!paused) return;
            SaveSystem.Save();
            if (!IsOver && !InStore) SetPaused(true);
        }

        public void AddDates(int amount)
        {
            // A timed event from the server settings (never more than double, and only on a trusted clock).
            if (amount > 0) amount = Mathf.RoundToInt(amount * RemoteConfig.GameSettingsService.DatesMultiplier());
            int earned = progress.AddDates(amount);
            if (amount > 0) SaveSystem.Data.coins += amount;
            GameEvents.PlaySfx(earned > 0 ? AudioCues.ExtraLife : "coin");
            if (earned > 0) { GameEvents.RaiseLivesChanged(Lives); GameEvents.RaiseExtraLife(earned); }
            GameEvents.RaiseDatesChanged(Dates);
        }

        public void AddLife()
        {
            progress.AddLife();
            GameEvents.PlaySfx(AudioCues.ExtraLife);
            GameEvents.RaiseLivesChanged(Lives);
            GameEvents.RaiseExtraLife(1);
        }

        /// <summary>Called by the stage builder with the number of boxes in the layout.</summary>
        public void SetBoxTotal(int total)
        {
            Boxes.SetTotal(total);
            GameEvents.RaiseBoxesChanged(Boxes.Broken, Boxes.Total);
        }

        public void BoxBroken(bool bisht = false)
        {
            Boxes.Break(bisht);
            GameEvents.RaiseBoxesChanged(Boxes.Broken, Boxes.Total);
        }

        private CheckpointTrail trail = new CheckpointTrail(new CheckpointTrail.Spot(0f, 0f, 0f));
        private static CheckpointTrail.Spot ToSpot(Vector3 v) => new CheckpointTrail.Spot(v.x, v.y, v.z);

        private string CheckpointKey => $"{RespawnPoint.x:0},{RespawnPoint.y:0},{RespawnPoint.z:0}";

        public void SetCheckpoint(Vector3 position)
        {
            if (!trail.Save(ToSpot(position))) return;
            RespawnPoint = position;
            mercy.Reached(CheckpointKey);
            GameEvents.PlaySfx("checkpoint");
            AudioManager.Bark(AudioCues.VoAronCheckpoint, null);
            GameEvents.RaiseCheckpointReached(position);
            SaveSystem.Data.lastCheckpointLevel = SceneManager.GetActiveScene().name;
            SaveSystem.Save();
        }

        public void PlayerDied(PlayerController p)
        {
            if (IsOver) return;
            bool last = progress.LoseLife();
            GameEvents.RaiseLivesChanged(Lives);
            GameEvents.RaisePlayerDied();
            if (trail.Died(!last) == DeathOutcome.RestartStage)
            {
                // No lives left: the saved lanterns are gone and "try again" starts the stage over.
                IsOver = true;
                RespawnPoint = new Vector3(trail.Start.x, trail.Start.y, trail.Start.z);
                SaveSystem.Data.lastCheckpointLevel = "";
                SaveSystem.Save();
                if (Stages.StageSession.Active) Aron.Telemetry.TelemetryService.StageFail(Stages.StageSession.Stage.id, RunSeconds);
                if (!AudioManager.PlayStinger(MusicCues.Defeat)) GameEvents.PlaySfx("gameover");
                GameEvents.RaiseGameOver();
                return;
            }
            bool help = mercy.Died(CheckpointKey);
            p.Respawn(RespawnPoint);
            // Losing a life takes the bisht; dying three times at the same checkpoint brings one back.
            if (p.Bisht != null)
            {
                p.Bisht.Clear();
                if (help) p.Bisht.Mercy();
            }
        }

        public void CompleteLevel()
        {
            if (IsOver) return;
            IsOver = true;
            if (!AudioManager.PlayStinger(MusicCues.Victory)) GameEvents.PlaySfx("finish");
            var data = SaveSystem.Data;
            data.totalDates += Dates;
            if (levelConfig != null && data.highestLevelCompleted < levelConfig.levelIndex)
                data.highestLevelCompleted = levelConfig.levelIndex;
            bool stats = Stages.StageSession.Active && Aron.Telemetry.TelemetryService.AnalyticsAllowed;
            var regionsBefore = stats ? Aron.Logic.Telemetry.RegionWatch.Reached(Stages.StageSession.Catalog, data) : null;
            LastRunStars = Stages.StageSession.RecordFinish(RunSeconds, Dates, Boxes.Broken, Boxes.BishtBroken);
            if (stats)
            {
                Aron.Telemetry.TelemetryService.StageComplete(Stages.StageSession.Stage.id, RunSeconds);
                Aron.Telemetry.TelemetryService.RegionsOpened(Aron.Logic.Telemetry.RegionWatch.Opened(regionsBefore, Aron.Logic.Telemetry.RegionWatch.Reached(Stages.StageSession.Catalog, data)));
            }
            if (Stages.StageSession.Active) Stages.MuseumService.OnStageFinished(Stages.StageSession.Stage.id, LastRunStars.hidden);
            SaveSystem.Save();
            GameEvents.RaiseLevelCompleted();
        }

        public void TogglePause()
        {
            if (!InStore) SetPaused(!IsPaused);
        }

        public void SetPaused(bool paused)
        {
            if (IsOver && paused) return;
            IsPaused = paused;
            Time.timeScale = paused || InStore ? 0f : 1f;
            GameEvents.RaisePauseChanged(paused);
        }

        /// <summary>Stops the world while a store is open, without showing the pause menu.</summary>
        public void SetInStore(bool open)
        {
            InStore = open;
            Time.timeScale = open || IsPaused ? 0f : 1f;
            if (!open) SaveSystem.Save();
        }

        public void Restart()
        {
            Time.timeScale = 1f;
            SceneLoader.Load(SceneManager.GetActiveScene().name);
        }

        public void LoadMenu()
        {
            Time.timeScale = 1f;
            if (Stages.StageSession.Active) Stages.StageSession.OpenMap = true;
            SceneLoader.Load(SceneNames.MainMenu);
        }
    }
}
