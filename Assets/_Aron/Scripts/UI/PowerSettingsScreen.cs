// (c) Ahmed Alamrani. All rights reserved.
using Aron.Core;
using Aron.Localization;
using Aron.Logic;
using Aron.Logic.Performance;
using Aron.Save;
using UnityEngine;
using UnityEngine.UI;

namespace Aron.UI
{
    /// <summary>Battery saver, 60 fps (High phones only), data saver and mobile-data downloads. Each row is one button.</summary>
    public class PowerSettingsScreen : MonoBehaviour
    {
        public static PowerSettingsScreen Open(Transform parent)
        {
            var go = new GameObject("PowerSettings");
            go.transform.SetParent(parent, false);
            var s = go.AddComponent<PowerSettingsScreen>();
            s.Build();
            return s;
        }

        private Button saver, sixty, dataSaver, mobile;

        private void Build()
        {
            var canvas = UIFactory.CreateCanvas("PowerCanvas", 30);
            canvas.transform.SetParent(transform, false);
            UIFactory.Panel(canvas.transform, "Bg", new Color(0.08f, 0.05f, 0.03f, 0.97f));
            var safe = UIFactory.SafeRoot(canvas);
            var title = UIFactory.Label(safe, "Title", Strings.Get("power_title"), 56f, UIFactory.Sand);
            UIFactory.Anchor(title.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -20f), new Vector2(1400f, 90f));
            title.rectTransform.pivot = new Vector2(0.5f, 1f);
            var back = UIFactory.CreateButton(safe, "Back", Strings.Get("back"), () => { SaveSystem.Save(); Destroy(gameObject); });
            UIFactory.Anchor((RectTransform)back.transform, new Vector2(0f, 1f), new Vector2(30f, -20f), new Vector2(300f, 100f));

            var g = UIFactory.VerticalGroup(safe, "Rows", 20f);
            UIFactory.Anchor(g, new Vector2(0.5f, 0.5f), new Vector2(0f, -40f), new Vector2(1100f, 600f));
            saver = UIFactory.CreateButton(g, "Saver", "", () => Change(p => p.batterySaver = !p.batterySaver));
            sixty = UIFactory.CreateButton(g, "Sixty", "", () => Change(p => p.prefer60 = !p.prefer60));
            dataSaver = UIFactory.CreateButton(g, "DataSaver", "", () => Change(p => p.dataSaver = !p.dataSaver));
            mobile = UIFactory.CreateButton(g, "Mobile", "", () => Change(p => p.mobileData = !p.mobileData));
            // 60 fps is only offered where the phone can hold it.
            sixty.gameObject.SetActive(DeviceSettings.Tier == QualityTier.High);
            Refresh();
        }

        private void Change(System.Action<PowerSettings> edit)
        {
            edit(NetworkState.Settings);
            SaveSystem.Save();
            if (DeviceSettings.Instance != null) DeviceSettings.Instance.Reapply();
            Refresh();
        }

        private static string OnOff(bool on) => Strings.Get(on ? "on" : "off");

        private void Refresh()
        {
            var p = NetworkState.Settings;
            UIFactory.SetButtonText(saver, Strings.Format("power_saver", OnOff(p.batterySaver)));
            UIFactory.SetButtonText(sixty, Strings.Format("fps_60", OnOff(p.prefer60)));
            UIFactory.SetButtonText(dataSaver, Strings.Format("data_saver", OnOff(p.dataSaver)));
            UIFactory.SetButtonText(mobile, Strings.Format("mobile_data", OnOff(p.mobileData)));
        }
    }

    /// <summary>A small question shown in the menu when the battery is low: turn on battery saver?</summary>
    public static class PowerHint
    {
        public static void Show(System.Action yes, System.Action no)
        {
            var canvas = UIFactory.CreateCanvas("PowerHint", 60);
            var root = canvas.gameObject;
            UIFactory.Panel(canvas.transform, "Dim", UIFactory.Dark);
            var safe = UIFactory.SafeRoot(canvas);
            var text = UIFactory.Label(safe, "Text", Strings.Get("power_hint"), 52f, UIFactory.Sand);
            UIFactory.Anchor(text.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0f, 120f), new Vector2(1400f, 120f));
            text.rectTransform.pivot = new Vector2(0.5f, 0.5f);
            var y = UIFactory.CreateButton(safe, "Yes", Strings.Get("yes"), () => { Object.Destroy(root); yes?.Invoke(); });
            UIFactory.Anchor((RectTransform)y.transform, new Vector2(0.5f, 0.5f), new Vector2(-300f, -60f), new Vector2(520f, 110f));
            ((RectTransform)y.transform).pivot = new Vector2(0.5f, 0.5f);
            var n = UIFactory.CreateButton(safe, "No", Strings.Get("no"), () => { Object.Destroy(root); no?.Invoke(); });
            UIFactory.Anchor((RectTransform)n.transform, new Vector2(0.5f, 0.5f), new Vector2(300f, -60f), new Vector2(520f, 110f));
            ((RectTransform)n.transform).pivot = new Vector2(0.5f, 0.5f);
        }
    }
}
