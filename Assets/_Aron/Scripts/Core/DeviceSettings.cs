// (c) Ahmed Alamrani. All rights reserved.
using System;
using System.Collections;
using System.Collections.Generic;
using Aron.Logic;
using Aron.Logic.Performance;
using Aron.Save;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

namespace Aron.Core
{
    /// <summary>
    /// Owns how hard the phone works. It picks the graphics tier once, then keeps frame rate and quality in line with
    /// the screen (play, menu, pause, loading, background), the battery, the system's power saving and the phone's heat.
    /// The decisions themselves live in Aron.Logic.Performance (FrameCapPolicy, ThermalGovernor, QualityLadder) and are unit-tested.
    /// Per frame this does a few comparisons; the heat and battery readings happen every 5 s in play and every 15 s in menus.
    /// </summary>
    public class DeviceSettings : MonoBehaviour
    {
        public static DeviceSettings Instance { get; private set; }
        public static QualityTier Tier { get; private set; } = QualityTier.High;
        public static ScreenMode Mode => Instance != null ? Instance.mode : ScreenMode.Gameplay;
        public static ThermalLevel Thermal => Instance != null ? Instance.thermal.Applied : ThermalLevel.Nominal;
        public static event Action<bool> BackgroundChanged;

        private const float PausedAfter = 0.5f;

        private readonly ThermalGovernor thermal = new ThermalGovernor();
        private readonly Dictionary<UniversalRenderPipelineAsset, float> shadowStart = new Dictionary<UniversalRenderPipelineAsset, float>();
        private FrameRateGovernor governor;
        private ScreenMode mode = ScreenMode.Menu;
        private bool inGame, background, heldTo30, hintShown;
        private int step = -1;
        private float pausedFor;
        private int battery = -1;
        private bool charging, osPowerSave;
        private FrameCap cap = new FrameCap(0, 0);

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Boot()
        {
            var go = new GameObject("DeviceSettings");
            DontDestroyOnLoad(go);
            go.AddComponent<DeviceSettings>();
        }

        private void Awake()
        {
            Instance = this;
            QualitySettings.vSyncCount = 0;
            Tier = Application.isEditor ? QualityTier.High : DeviceTiers.Pick(ReadSpecs());
            governor = new FrameRateGovernor(30);
            ApplyTier();
            OnSceneChanged(default, SceneManager.GetActiveScene());
            SceneManager.activeSceneChanged += OnSceneChanged;
            Application.lowMemory += OnLowMemory;
            global::Aron.Logic.Diagnostics.Log.Info($"[Aron] {SystemInfo.deviceModel}, {SystemInfo.systemMemorySize} MB, {SystemInfo.graphicsDeviceName} -> {Tier}");
        }

        private void Start() => StartCoroutine(Sample());

        private void OnDestroy()
        {
            SceneManager.activeSceneChanged -= OnSceneChanged;
            Application.lowMemory -= OnLowMemory;
            if (Instance == this) Instance = null;
        }

        private static DeviceSpecs ReadSpecs() => new DeviceSpecs
        {
            memoryMB = SystemInfo.systemMemorySize,
            cpuCores = SystemInfo.processorCount,
            gpuName = SystemInfo.graphicsDeviceName,
            isIOS = Application.platform == RuntimePlatform.IPhonePlayer,
            screenWidth = Screen.width,
            screenHeight = Screen.height,
        };

        // ------------------------------------------------------------------ tier and quality

        private void ApplyTier()
        {
            // Quality levels 0/1/2 are Low/Medium/High, each with its own URP asset (see Aron > Apply Phone Settings).
            if (QualitySettings.names.Length > (int)Tier) QualitySettings.SetQualityLevel((int)Tier, true);
            step = -1;
            Reapply();
        }

        private static UniversalRenderPipelineAsset Urp()
        {
            var pipeline = QualitySettings.renderPipeline != null ? QualitySettings.renderPipeline : GraphicsSettings.defaultRenderPipeline;
            return pipeline as UniversalRenderPipelineAsset;
        }

        private void ApplyQuality(in PowerInputs p)
        {
            int wanted = QualityLadder.StepFor(thermal.Applied, p.batterySaver, p.osPowerSave, FrameCapPolicy.LowBattery(p));
            if (wanted == step) return;
            step = wanted;
            var plan = QualityLadder.Plan(step);

            // Assets are only changed in memory on the device; in the editor they'd be saved to disk.
            if (!Application.isEditor)
            {
                var urp = Urp();
                if (urp != null)
                {
                    if (!shadowStart.TryGetValue(urp, out float shadow)) { shadow = urp.shadowDistance; shadowStart[urp] = shadow; }
                    urp.renderScale = Mathf.Max(0.5f, DeviceTiers.RenderScale(Tier, Screen.width, Screen.height) * plan.resolution);
                    urp.shadowDistance = plan.shadows == ShadowMode.Off ? 0f : plan.shadows == ShadowMode.Hard ? shadow * 0.6f : shadow;
                }
            }
            var terrains = Terrain.activeTerrains;
            for (int i = 0; i < terrains.Length; i++) terrains[i].detailObjectDensity = plan.foliage;
            QualityBus.Set(plan);
        }

        // ------------------------------------------------------------------ frame rate and screen mode

        private PowerInputs Inputs() => new PowerInputs
        {
            tier = Tier, mode = mode,
            batterySaver = NetworkState.Settings.batterySaver,
            prefer60 = NetworkState.Settings.prefer60,
            osPowerSave = osPowerSave, thermal = thermal.Applied,
            batteryPercent = battery, charging = charging, heldTo30 = heldTo30,
        };

        /// <summary>Re-evaluates everything. Call after a settings change.</summary>
        public void Reapply()
        {
            var p = Inputs();
            var want = FrameCapPolicy.Decide(p);
            if (want.target != cap.target) Application.targetFrameRate = want.target;
            if (want.renderEvery != cap.renderEvery) OnDemandRendering.renderFrameInterval = want.renderEvery;
            cap = want;
            Screen.sleepTimeout = mode == ScreenMode.Gameplay || mode == ScreenMode.Loading ? SleepTimeout.NeverSleep : SleepTimeout.SystemSetting;
            ApplyQuality(p);
        }

        private void OnSceneChanged(Scene from, Scene to)
        {
            inGame = to.name != SceneNames.MainMenu;
            governor.Reset();
            pausedFor = 0f;
            mode = inGame ? ScreenMode.Gameplay : ScreenMode.Menu;
            Reapply();
        }

        private void Update()
        {
            if (background) return;
            ScreenMode want;
            if (SceneLoader.IsLoading) want = ScreenMode.Loading;
            else if (!inGame) want = ScreenMode.Menu;
            else
            {
                // A freeze-frame of a few frames is not a pause: only a timeScale of 0 that lasts counts.
                pausedFor = Time.timeScale <= 0f ? pausedFor + Time.unscaledDeltaTime : 0f;
                want = pausedFor >= PausedAfter ? ScreenMode.Paused : ScreenMode.Gameplay;
            }
            if (want != mode) { mode = want; Reapply(); }
            if (mode != ScreenMode.Gameplay) return;

            switch (governor.Update(Time.unscaledDeltaTime))
            {
                case FrameRateChange.DropTo30:
                    global::Aron.Logic.Diagnostics.Log.Info("[Aron] Can't hold 60 fps here, settling at 30.");
                    heldTo30 = true;
                    Reapply();
                    break;
                case FrameRateChange.LowerTier when Tier > QualityTier.Low:
                    Tier--;
                    global::Aron.Logic.Diagnostics.Log.Info("[Aron] Still slow at 30 fps, dropping to " + Tier);
                    ApplyTier();
                    break;
            }
        }

        // ------------------------------------------------------------------ background, heat, battery, memory

        private void OnApplicationPause(bool paused)
        {
            background = paused;
            AudioListener.pause = paused;
            if (paused) { mode = ScreenMode.Background; Reapply(); }
            else
            {
                mode = inGame ? ScreenMode.Gameplay : ScreenMode.Menu;
                governor.Reset();
                ReadSensors();
                Reapply();
            }
            BackgroundChanged?.Invoke(paused);
        }

        private void ReadSensors()
        {
            battery = PowerProbe.BatteryPercent();
            charging = PowerProbe.Charging();
            osPowerSave = PowerProbe.OsPowerSave();
            thermal.Report(PowerProbe.Thermal(), Time.unscaledTime);
        }

        private IEnumerator Sample()
        {
            var inPlay = new WaitForSecondsRealtime(5f);
            var inMenu = new WaitForSecondsRealtime(15f);
            while (true)
            {
                ReadSensors();
                Reapply();
                MaybeSuggestSaver();
                yield return mode == ScreenMode.Gameplay ? inPlay : inMenu;
            }
        }

        private void MaybeSuggestSaver()
        {
            if (hintShown || mode != ScreenMode.Menu) return;
            var s = NetworkState.Settings;
            int today = (DateTime.UtcNow.Date - new DateTime(2020, 1, 1)).Days;
            if (!BatterySaverPolicy.ShouldSuggest(Inputs(), s, today)) return;
            hintShown = true;
            UI.PowerHint.Show(
                () => { s.batterySaver = true; SaveSystem.Save(); Reapply(); },
                () => { s.saverHintDeclinedDay = today; SaveSystem.Save(); });
        }

        private static void OnLowMemory()
        {
            Caches.Registry.TrimAll(TrimLevel.Critical);
            Resources.UnloadUnusedAssets();
        }
    }
}
