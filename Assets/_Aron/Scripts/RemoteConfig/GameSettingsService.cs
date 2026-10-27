// (c) Ahmed Alamrani. All rights reserved.
using System;
using System.Collections;
using System.IO;
using Aron.Core;
using Aron.Logic;
using Aron.Logic.Config;
using Aron.Logic.Online;
using Aron.Save;
using UnityEngine;

namespace Aron.RemoteConfig
{
    /// <summary>Where the server answer comes from (Unity Remote Config in the shipped game). Calls <c>done(json)</c>, or <c>done(null)</c> on failure.</summary>
    public interface IRemoteSettingsSource
    {
        IEnumerator Fetch(Action<string> done);
    }

    /// <summary>
    /// Kill switches, difficulty tuning and timed events. Settings in force come from <see cref="SettingsResolver"/>:
    /// the server answer, else the last good answer saved on the phone, else Resources/Config/remote_defaults.json, else the built-in defaults.
    /// The answer is asked for once after start and again after coming back from the background if the last ask was over 6 hours ago;
    /// there is no polling, and it waits for a network the data rules allow (Wi-Fi, or mobile data with Data saver off).
    /// </summary>
    public class GameSettingsService : MonoBehaviour
    {
        public const string DefaultsResource = "Config/remote_defaults";
        public const float RefreshAfterSeconds = 6f * 3600f;

        public static GameSettingsService Instance { get; private set; }
        /// <summary>Set by the Unity Remote Config glue when that package is installed. Null means "ship with the file only".</summary>
        public static IRemoteSettingsSource Source;
        public static event Action<GameSettings> Changed;

        private static GameSettings current = GameSettings.Defaults();
        /// <summary>Always usable, even before the service has started.</summary>
        public static GameSettings Current => current;
        public static bool IsOn(Feature f) => current.IsOn(f);
        public static SettingsSource Origin { get; private set; } = SettingsSource.Defaults;

        private static string CachePath => Path.Combine(Application.persistentDataPath, "remote_settings.json");
        private string bundled = "", cached = "";
        private float lastAsk = -1e9f;
        private bool asking;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Boot()
        {
            if (Instance != null) return;
            var go = new GameObject("GameSettings");
            DontDestroyOnLoad(go);
            go.AddComponent<GameSettingsService>();
        }

        /// <summary>Parses one settings file. Used by the service and by tests.</summary>
        public static GameSettings Parse(string json) => JsonUtility.FromJson<GameSettings>(json);

        private void Awake()
        {
            Instance = this;
            var ta = Resources.Load<TextAsset>(DefaultsResource);
            bundled = ta != null ? ta.text : "";
            try { cached = File.Exists(CachePath) ? File.ReadAllText(CachePath) : ""; } catch (Exception) { cached = ""; }
            Apply(SettingsResolver.Resolve(bundled, cached, null, Parse));
        }

        private void OnDestroy() { if (Instance == this) Instance = null; }

        private void Start() => StartCoroutine(Ask());

        private void OnApplicationPause(bool paused)
        {
            if (!paused && Time.realtimeSinceStartup - lastAsk > RefreshAfterSeconds && !asking) StartCoroutine(Ask());
        }

        private IEnumerator Ask()
        {
            if (Source == null) yield break;
            asking = true;
            // Let the first scene finish loading, then wait until the data rules allow a small request.
            yield return new WaitForSecondsRealtime(5f);
            while (!NetworkState.Allow(Traffic.Sync)) yield return new WaitForSecondsRealtime(60f);
            string answer = null;
            yield return Source.Fetch(a => answer = a);
            lastAsk = Time.realtimeSinceStartup;
            asking = false;
            if (string.IsNullOrWhiteSpace(answer)) yield break;
            var resolved = SettingsResolver.Resolve(bundled, cached, answer, Parse);
            if (resolved.source != SettingsSource.Remote) yield break;   // invalid or older than what the phone has
            cached = answer;
            try { AtomicFile.WriteAllText(CachePath, answer); } catch (Exception) { /* next start asks again */ }
            Apply(resolved);
        }

        private static void Apply(ResolvedSettings r)
        {
            current = r.settings;
            Origin = r.source;
            Changed?.Invoke(current);
        }

        // ------------------------------------------------------------------ helpers used across the game

        /// <summary>Dates multiplier for the current moment (1 when no event is on or the game's clock cannot be trusted).</summary>
        public static float DatesMultiplier()
        {
            var now = new DateTimeOffset(DateTime.SpecifyKind(GameClock.Now(), DateTimeKind.Utc)).ToUnixTimeSeconds();
            return current.DatesMultiplier(now, GameClock.CanEarn);
        }

        public static DifficultyLevel Tuned(DifficultyLevel level) => current.difficulty.Apply(level);
    }
}
