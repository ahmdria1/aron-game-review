// (c) Ahmed Alamrani. All rights reserved.
using Aron.Feedback;
using Aron.Localization;
using Aron.Logic;
using Aron.Logic.CameraRig;
using Aron.Logic.Feedback;
using Aron.Logic.Safety;
using Aron.Naming;
using Aron.Save;
using UnityEngine;
using UnityEngine.UI;

namespace Aron.UI
{
    /// <summary>
    /// Play settings (accessibility and comfort are on their own screen, opened from here): vibration (on/off, strength, battery saver), camera (shake, invert-Y, auto-recenter), one-hand mode, aim assist.
    /// Every row is one button that cycles its value (the UI kit has no slider), and every change is saved at once.
    /// </summary>
    public class SettingsScreen : MonoBehaviour
    {
        public static SettingsScreen Open(Transform parent)
        {
            var go = new GameObject("Settings");
            go.transform.SetParent(parent, false);
            var s = go.AddComponent<SettingsScreen>();
            s.Build();
            return s;
        }

        private Button cellularContent, playerName, hapticsOn, hapticsLevel, hapticsSaver, shake, invertY, autoRecenter, oneHand, aimAssist;

        private static SaveData D
        {
            get
            {
                var d = SaveSystem.Data;
                if (d.haptics == null) d.haptics = new HapticSettings();
                if (d.camera == null) d.camera = new CameraSettings();
                if (d.access == null) d.access = new AccessSettings();
                return d;
            }
        }

        private void Build()
        {
            var canvas = UIFactory.CreateCanvas("SettingsCanvas", 20);
            canvas.transform.SetParent(transform, false);
            UIFactory.Panel(canvas.transform, "Bg", new Color(0.08f, 0.05f, 0.03f, 0.96f));
            var safe = UIFactory.SafeRoot(canvas);
            var title = UIFactory.Label(safe, "Title", Strings.Get("settings_title"), 56f, UIFactory.Sand);
            UIFactory.Anchor(title.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -20f), new Vector2(1400f, 90f));
            title.rectTransform.pivot = new Vector2(0.5f, 1f);
            var back = UIFactory.CreateButton(safe, "Back", Strings.Get("back"), () => { SaveSystem.Save(); Destroy(gameObject); });
            UIFactory.Anchor((RectTransform)back.transform, new Vector2(0f, 1f), new Vector2(30f, -20f), new Vector2(300f, 100f));
            var power = UIFactory.CreateButton(safe, "Power", Strings.Get("power_title"), () => PowerSettingsScreen.Open(transform));
            UIFactory.Anchor((RectTransform)power.transform, new Vector2(1f, 1f), new Vector2(-30f, -20f), new Vector2(480f, 100f));
            var privacy = UIFactory.CreateButton(safe, "Privacy", Strings.Get("privacy_title"), () => PrivacySettingsScreen.Open(transform));
            UIFactory.Anchor((RectTransform)privacy.transform, new Vector2(1f, 1f), new Vector2(-30f, -130f), new Vector2(480f, 100f));

            // two columns, so the list still fits a phone held sideways
            var g = UIFactory.VerticalGroup(safe, "Rows", 14f);
            UIFactory.Anchor(g, new Vector2(0.5f, 0.5f), new Vector2(-330f, -40f), new Vector2(600f, 940f));
            var g2 = UIFactory.VerticalGroup(safe, "RowsRight", 14f);
            UIFactory.Anchor(g2, new Vector2(0.5f, 0.5f), new Vector2(330f, -40f), new Vector2(600f, 940f));
            playerName = UIFactory.CreateButton(g, "PlayerName", "", () => NameScreen.Open(transform, Refresh));
            cellularContent = UIFactory.CreateButton(g, "CellularContent", "", () => Change(d => d.cellularContent = !d.cellularContent));
            hapticsOn = UIFactory.CreateButton(g, "HapticsOn", "", () => Change(d => d.haptics.enabled = !d.haptics.enabled));
            hapticsLevel = UIFactory.CreateButton(g, "HapticsLevel", "", () => Change(d => d.haptics.CycleLevel()));
            hapticsSaver = UIFactory.CreateButton(g, "HapticsSaver", "", () => Change(d => d.haptics.offInBatterySaver = !d.haptics.offInBatterySaver));
            shake = UIFactory.CreateButton(g, "Shake", "", () => Change(d => d.camera.shake = d.camera.shake >= 100 ? 0 : d.camera.shake + 25));
            invertY = UIFactory.CreateButton(g2, "InvertY", "", () => Change(d => d.camera.invertY = !d.camera.invertY));
            autoRecenter = UIFactory.CreateButton(g2, "AutoRecenter", "", () => Change(d => d.camera.autoRecenter = !d.camera.autoRecenter));
            oneHand = UIFactory.CreateButton(g2, "OneHand", "", () => Change(d => d.access.oneHand = !d.access.oneHand));
            aimAssist = UIFactory.CreateButton(g2, "AimAssist", "", () => Change(d => d.access.aimAssist = !d.access.aimAssist));
            UIFactory.CreateButton(g2, "Accessibility", Strings.Get("access_open"), () => AccessibilityScreen.Open(transform));
            Refresh();
        }

        private void Change(System.Action<SaveData> edit)
        {
            edit(D);
            SaveSystem.Save();
            Refresh();
        }

        private static string OnOff(bool on) => Strings.Get(on ? "on" : "off");

        private void Refresh()
        {
            var d = D;
            UIFactory.SetButtonText(playerName, Strings.Format("name_row", PlayerNames.HasName ? PlayerNames.Display : "-"));
            string[] levels = { "level_low", "level_med", "level_high" };
            UIFactory.SetButtonText(cellularContent, Strings.Get("content_cellular_setting") + ": " + OnOff(d.cellularContent));
            UIFactory.SetButtonText(hapticsOn, Strings.Format("haptics_on", OnOff(d.haptics.enabled)));
            UIFactory.SetButtonText(hapticsLevel, Strings.Format("haptics_level", Strings.Get(levels[Mathf.Clamp(d.haptics.level, 0, 2)])));
            UIFactory.SetButtonText(hapticsSaver, Strings.Format("haptics_saver", OnOff(d.haptics.offInBatterySaver)));
            UIFactory.SetButtonText(shake, Strings.Format("shake_level", d.camera.shake));
            UIFactory.SetButtonText(invertY, Strings.Format("invert_y", OnOff(d.camera.invertY)));
            UIFactory.SetButtonText(autoRecenter, Strings.Format("auto_recenter", OnOff(d.camera.autoRecenter)));
            UIFactory.SetButtonText(oneHand, Strings.Format("one_hand", OnOff(d.access.oneHand)));
            UIFactory.SetButtonText(aimAssist, Strings.Format("aim_assist", OnOff(d.access.aimAssist)));
        }
    }
}
