// (c) Ahmed Alamrani. All rights reserved.
using Aron.Localization;
using Aron.Logic.Telemetry;
using Aron.Save;
using Aron.Telemetry;
using UnityEngine;
using UnityEngine.UI;

namespace Aron.UI
{
    /// <summary>
    /// Usage statistics and crash reports. Both are off until a grown-up turns them on (the grown-up check comes first), either can be
    /// turned off again at any time, and one button deletes what was collected.
    /// </summary>
    public class PrivacySettingsScreen : MonoBehaviour
    {
        public static PrivacySettingsScreen Open(Transform parent)
        {
            var go = new GameObject("PrivacySettings");
            go.transform.SetParent(parent, false);
            var s = go.AddComponent<PrivacySettingsScreen>();
            s.Build();
            return s;
        }

        private Button stats, crash;

        private void Build()
        {
            var canvas = UIFactory.CreateCanvas("PrivacyCanvas", 30);
            canvas.transform.SetParent(transform, false);
            UIFactory.Panel(canvas.transform, "Bg", new Color(0.08f, 0.05f, 0.03f, 0.97f));
            var safe = UIFactory.SafeRoot(canvas);
            var title = UIFactory.Label(safe, "Title", Strings.Get("privacy_title"), 56f, UIFactory.Sand);
            UIFactory.Anchor(title.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -20f), new Vector2(1400f, 90f));
            title.rectTransform.pivot = new Vector2(0.5f, 1f);
            var back = UIFactory.CreateButton(safe, "Back", Strings.Get("back"), () => { SaveSystem.Save(); Destroy(gameObject); });
            UIFactory.Anchor((RectTransform)back.transform, new Vector2(0f, 1f), new Vector2(30f, -20f), new Vector2(300f, 100f));

            var g = UIFactory.VerticalGroup(safe, "Rows", 20f);
            UIFactory.Anchor(g, new Vector2(0.5f, 0.5f), new Vector2(0f, -40f), new Vector2(1100f, 600f));
            UIFactory.Label(g, "Note", Strings.Get("privacy_note"), 38f, UIFactory.Sand);
            stats = UIFactory.CreateButton(g, "Stats", "", () => Toggle(Channel.Analytics));
            crash = UIFactory.CreateButton(g, "Crash", "", () => Toggle(Channel.Diagnostics));
            UIFactory.CreateButton(g, "Erase", Strings.Get("privacy_erase"), () => { TelemetryService.EraseAll(); Refresh(); });
            Refresh();
        }

        private void Toggle(Channel channel)
        {
            var c = SaveSystem.Data.privacy;
            bool on = ConsentGate.Allowed(c, channel);
            if (on) { TelemetryService.Choose(channel, false, false); Refresh(); return; }
            // Turning on always goes through the grown-up check.
            ParentalGateDialog.Ask(() => { TelemetryService.Choose(channel, true, true); Refresh(); }, Strings.Get("privacy_note"));
        }

        private void Refresh()
        {
            var c = SaveSystem.Data.privacy;
            UIFactory.SetButtonText(stats, Strings.Format("privacy_stats", Strings.Get(ConsentGate.Allowed(c, Channel.Analytics) ? "on" : "off")));
            UIFactory.SetButtonText(crash, Strings.Format("privacy_crash", Strings.Get(ConsentGate.Allowed(c, Channel.Diagnostics) ? "on" : "off")));
        }
    }
}
