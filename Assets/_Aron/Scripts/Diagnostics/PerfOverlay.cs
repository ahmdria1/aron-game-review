// (c) Ahmed Alamrani. All rights reserved.
using System.Text;
using Aron.Atmosphere;
using Aron.Core;
using Aron.Localization;
using Aron.Logic;
using Aron.Logic.Diagnostics;
using Aron.Logic.Signature;
using Aron.UI;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace Aron.Diagnostics
{
    /// <summary>
    /// Hidden performance overlay: five quick taps on the main menu's version label open it. Off by default and not
    /// even created until then; while hidden its object is inactive, so it costs nothing per frame.
    /// Shows FPS (now / average / 1% low), frame time, quality tier and target, memory, device and battery.
    /// "Run 60s benchmark" flies the camera along the current stage with rain on, then shows a summary to screenshot.
    /// Nothing here goes to analytics (see <see cref="DiagnosticsPolicy"/>), and nothing is saved.
    /// </summary>
    [DefaultExecutionOrder(10000)]
    public class PerfOverlay : MonoBehaviour
    {
        private static PerfOverlay instance;
        private static readonly TapCounter Taps = new TapCounter();

        private readonly FrameStats stats = new FrameStats(600);
        private TextMeshProUGUI body;
        private Button benchButton;
        private float nextRefresh;
        private Benchmark bench;
        private Camera benchCam;
        private Vector3 benchStart;
        private Quaternion benchRot;
        private BenchmarkSummary result;
        private string stageName = "";
        private float stageLength = 300f;
        private bool restoreRain;

        /// <summary>Call from the version label's click. Opens the overlay on the fifth quick tap.</summary>
        public static void Tap()
        {
            if (!Taps.Tap(Time.unscaledTime)) return;
            Toggle();
        }

        public static void Toggle()
        {
            if (instance == null) instance = Build();
            else instance.gameObject.SetActive(!instance.gameObject.activeSelf);
        }

        /// <summary>The small version label on the main menu that carries the secret taps.</summary>
        public static void AttachVersionLabel(Transform parent)
        {
            var b = UIFactory.CreateButton(parent, "Version", "v" + Application.version, Tap);
            var rt = (RectTransform)b.transform;
            rt.sizeDelta = new Vector2(260f, 70f);
            var img = b.GetComponent<Image>();
            if (img != null) img.color = new Color(0f, 0f, 0f, 0f);
            var hold = b.gameObject.AddComponent<VersionLongPress>();
            hold.Label = b.GetComponentInChildren<TextMeshProUGUI>();
            hold.Normal = "v" + Application.version;
        }

        private static PerfOverlay Build()
        {
            var go = new GameObject("PerfOverlay");
            DontDestroyOnLoad(go);
            var o = go.AddComponent<PerfOverlay>();
            o.BuildUi();
            return o;
        }

        private void BuildUi()
        {
            var canvas = UIFactory.CreateCanvas("PerfCanvas", 5000);
            canvas.transform.SetParent(transform, false);
            var panel = UIFactory.FadePanel(canvas.transform, "Panel", new Color(0f, 0f, 0f, 0.72f));
            UIFactory.Anchor(panel, new Vector2(0f, 1f), new Vector2(20f, -20f), new Vector2(900f, 640f));
            var t = UIFactory.Label(panel, "Body", "", 30f, Color.white, TextAlignmentOptions.TopLeft);
            var rt = (RectTransform)t.transform;
            rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one; rt.offsetMin = new Vector2(16f, 176f); rt.offsetMax = new Vector2(-16f, -16f);
            body = t;
            benchButton = UIFactory.CreateButton(panel, "Bench", Strings.Get("perf_run_bench"), StartBenchmark);
            UIFactory.Anchor((RectTransform)benchButton.transform, new Vector2(0f, 0f), new Vector2(16f, 16f), new Vector2(520f, 70f));
            // Developer tool, English only: the game-feel sliders (opens the panel and leaves a small FEEL chip on screen).
            var feel = UIFactory.CreateButton(panel, "Feel", "Feel tuning", FeelTuningPanel.Toggle);
            UIFactory.Anchor((RectTransform)feel.transform, new Vector2(0f, 0f), new Vector2(16f, 96f), new Vector2(520f, 70f));
            var hide = UIFactory.CreateButton(panel, "Hide", Strings.Get("perf_hide"), () => gameObject.SetActive(false));
            UIFactory.Anchor((RectTransform)hide.transform, new Vector2(1f, 0f), new Vector2(-16f, 16f), new Vector2(260f, 70f));
        }

        private void OnEnable() { stats.Clear(); nextRefresh = 0f; }

        private void OnDisable() { EndBenchmark(false); }

        private void Update()
        {
            stats.Add(Time.unscaledDeltaTime);
            if (bench != null && bench.Running)
            {
                if (bench.Frame(Time.unscaledDeltaTime)) FinishBenchmark();
            }
            if (Time.unscaledTime < nextRefresh) return;
            nextRefresh = Time.unscaledTime + 0.5f;   // text is rebuilt twice a second, not every frame
            Refresh();
        }

        private void LateUpdate()
        {
            if (bench == null || !bench.Running || benchCam == null) return;
            FlyThrough.At(bench.Elapsed, stageLength, out float x, out float h, out float yaw);
            benchCam.transform.position = benchStart + Vector3.right * x + Vector3.up * h;
            benchCam.transform.rotation = benchRot * Quaternion.Euler(0f, yaw, 0f);
        }

        private static string Tier() => DeviceSettings.Tier.ToString();
        private static int Target() => DeviceTiers.FrameRate(DeviceSettings.Tier);
        private static int Managed() => (int)(System.GC.GetTotalMemory(false) / (1024 * 1024));
        private static int Total() => (int)(UnityEngine.Profiling.Profiler.GetTotalAllocatedMemoryLong() / (1024 * 1024));

        private void Refresh()
        {
            var sb = new StringBuilder(512);
            if (result != null)
            {
                sb.AppendLine(Strings.Get("perf_result") + " — " + Strings.Get("perf_verdict_" + result.Verdict));
                sb.AppendLine(PerfFormat.Summary(result));
                sb.AppendLine(Strings.Format("perf_battery", BatteryPercent(), SystemInfo.batteryStatus));
            }
            else
            {
                sb.AppendLine(Strings.Format("perf_fps_now", PerfFormat.F1(stats.CurrentFps)));
                sb.AppendLine(Strings.Format("perf_fps_avg", PerfFormat.F1(stats.AverageFps)));
                sb.AppendLine(Strings.Format("perf_fps_low", PerfFormat.F1(stats.OnePercentLowFps)));
                sb.AppendLine(Strings.Format("perf_frame_ms", PerfFormat.F1(stats.LastMs)));
                sb.AppendLine(Strings.Format("perf_tier_target", Tier(), Target()));
                sb.AppendLine(Strings.Format("perf_memory", Managed(), Total()));
                sb.AppendLine(Strings.Format("perf_device", SystemInfo.deviceModel));
                sb.AppendLine(Strings.Format("perf_battery", BatteryPercent(), SystemInfo.batteryStatus));
                sb.AppendLine(BuildInfo.Line(Application.version));
                if (bench != null && bench.Running)
                    sb.AppendLine(Strings.Format("perf_running", Mathf.CeilToInt(Benchmark.Duration - bench.Elapsed)));
            }
            ArabicTMP.SetText(body, sb.ToString());
        }

        private static string BatteryPercent()
        {
            float l = SystemInfo.batteryLevel;
            return l < 0f ? "?" : Mathf.RoundToInt(l * 100f).ToString();
        }

        private void StartBenchmark()
        {
            if (bench != null && bench.Running) return;
            result = null;
            benchCam = Camera.main;
            if (benchCam == null) return;
            benchStart = benchCam.transform.position;
            benchRot = benchCam.transform.rotation;
            var stage = Stages.StageSession.Stage;
            stageName = stage != null ? stage.id : UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
            stageLength = 300f;
            WeatherDirector.BenchmarkRain = true;
            restoreRain = true;
            bench = new Benchmark(Target());
            bench.Start();
            benchButton.interactable = false;
        }

        private void FinishBenchmark()
        {
            result = bench.Summary(SystemInfo.deviceModel, Tier(), stageName, Managed(), Total(), true);
            EndBenchmark(true);
            nextRefresh = 0f;
        }

        private void EndBenchmark(bool keepResult)
        {
            if (restoreRain) WeatherDirector.BenchmarkRain = false;
            restoreRain = false;
            if (benchCam != null) { benchCam.transform.position = benchStart; benchCam.transform.rotation = benchRot; }
            benchCam = null;
            if (!keepResult) { bench = null; result = null; }
            if (benchButton != null) benchButton.interactable = true;
        }
    }

    /// <summary>
    /// Hidden credit: hold the version label for five seconds and it reads "Built by ..." for a few seconds.
    /// It uses a timed coroutine that only exists while a finger is down, so there is no Update work at idle.
    /// </summary>
    public class VersionLongPress : MonoBehaviour, UnityEngine.EventSystems.IPointerDownHandler, UnityEngine.EventSystems.IPointerUpHandler
    {
        public TextMeshProUGUI Label;
        public string Normal = "";
        private Coroutine hold;
        private Coroutine shown;

        public void OnPointerDown(UnityEngine.EventSystems.PointerEventData e)
        {
            if (hold != null) StopCoroutine(hold);
            hold = StartCoroutine(Hold());
        }

        public void OnPointerUp(UnityEngine.EventSystems.PointerEventData e)
        {
            if (hold != null) { StopCoroutine(hold); hold = null; }
        }

        private void OnDisable() { hold = null; shown = null; if (Label != null && !string.IsNullOrEmpty(Normal)) Label.text = Normal; }

        private System.Collections.IEnumerator Hold()
        {
            yield return new WaitForSecondsRealtime(HoldTimer.Seconds);
            hold = null;
            if (Label == null) yield break;
            if (shown != null) StopCoroutine(shown);
            shown = StartCoroutine(ShowCredit());
        }

        private System.Collections.IEnumerator ShowCredit()
        {
            Label.text = StudioMark.Credit;
            yield return new WaitForSecondsRealtime(4f);
            Label.text = Normal;
            shown = null;
        }
    }
}
