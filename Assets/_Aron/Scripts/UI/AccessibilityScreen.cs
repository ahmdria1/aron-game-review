// (c) Ahmed Alamrani. All rights reserved.
using System;
using System.Collections.Generic;
using Aron.Localization;
using Aron.Logic.Access;
using Aron.Logic.Safety;
using Aron.Save;
using UnityEngine;
using UnityEngine.UI;

namespace Aron.UI
{
    /// <summary>
    /// Accessibility and comfort. Like Settings, every row is one button that cycles its value and saves at once. Rows sit in three
    /// columns so the list fits a phone held sideways even at the largest text size.
    /// </summary>
    public class AccessibilityScreen : MonoBehaviour
    {
        public static AccessibilityScreen Open(Transform parent)
        {
            var go = new GameObject("Accessibility");
            go.transform.SetParent(parent, false);
            var s = go.AddComponent<AccessibilityScreen>();
            s.Build();
            return s;
        }

        private static readonly float[] Backdrops = { 0.4f, 0.55f, 0.75f, 1f };

        private readonly List<(Button button, Func<AccessSettings, string> text)> rows = new List<(Button, Func<AccessSettings, string>)>();
        private readonly RectTransform[] columns = new RectTransform[3];
        private int added;
        private TMPro.TextMeshProUGUI sample;
        private RectTransform sampleBg;
        private Image[] swatches;
        private TMPro.TextMeshProUGUI hint;

        private static AccessSettings A
        {
            get
            {
                var d = SaveSystem.Data;
                if (d.access == null) d.access = new AccessSettings();
                return d.access;
            }
        }

        private void Build()
        {
            var canvas = UIFactory.CreateCanvas("AccessibilityCanvas", 25);
            canvas.transform.SetParent(transform, false);
            UIFactory.Panel(canvas.transform, "Bg", new Color(0.08f, 0.05f, 0.03f, 0.97f));
            var safe = UIFactory.SafeRoot(canvas);
            var title = UIFactory.Label(safe, "Title", Strings.Get("access_title"), 56f, UIFactory.Sand);
            UIFactory.Anchor(title.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -20f), new Vector2(1400f, 90f));
            title.rectTransform.pivot = new Vector2(0.5f, 1f);
            hint = UIFactory.Label(safe, "Hint", "", 34f, new Color(1f, 1f, 1f, 0.8f));
            UIFactory.Anchor(hint.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -105f), new Vector2(1700f, 60f));
            hint.rectTransform.pivot = new Vector2(0.5f, 1f);
            var back = UIFactory.CreateButton(safe, "Back", Strings.Get("back"), () => { SaveSystem.Save(); Destroy(gameObject); });
            UIFactory.Anchor((RectTransform)back.transform, new Vector2(0f, 1f), new Vector2(30f, -20f), new Vector2(300f, 100f));

            for (int c = 0; c < columns.Length; c++)
            {
                columns[c] = UIFactory.VerticalGroup(safe, "Column" + c, 6f);
                UIFactory.Anchor(columns[c], new Vector2(0.5f, 0.5f), new Vector2((c - 1) * 620f, -60f), new Vector2(600f, 880f));
            }

            AddRows();
            // the five role colours and their shapes, in the palette that is chosen
            var strip = UIFactory.Panel(safe, "Roles", Color.clear, false);
            UIFactory.Anchor(strip, new Vector2(0f, 0f), new Vector2(30f, 20f), new Vector2(760f, 90f));
            swatches = new Image[AccessPalette.Roles.Length];
            for (int i = 0; i < swatches.Length; i++)
            {
                var sw = UIFactory.Panel(strip, "Role" + i, Color.white, false);
                UIFactory.Anchor(sw, new Vector2(0f, 0.5f), new Vector2(5f + i * 150f, 0f), new Vector2(90f, 90f));
                swatches[i] = sw.GetComponent<Image>();
                swatches[i].sprite = AccessColours.Shape(AccessPalette.Shape(AccessPalette.Roles[i]));
                swatches[i].raycastTarget = false;
            }
            sampleBg = UIFactory.Panel(safe, "SampleBg", Color.black, false);
            UIFactory.Anchor(sampleBg, new Vector2(1f, 0f), new Vector2(-30f, 20f), new Vector2(1000f, 90f));
            sampleBg.GetComponent<Image>().raycastTarget = false;
            sample = UIFactory.Label(sampleBg, "Sample", "", CaptionRules.BaseFontSize, Color.white);
            sample.rectTransform.anchorMin = Vector2.zero; sample.rectTransform.anchorMax = Vector2.one;
            sample.rectTransform.offsetMin = new Vector2(20f, 6f); sample.rectTransform.offsetMax = new Vector2(-20f, -6f);
            Refresh();
        }

        /// <summary>One row: a button whose label comes from <paramref name="text"/> and whose click edits the settings.</summary>
        private void Row(Func<AccessSettings, string> text, Action<AccessSettings> edit)
        {
            var column = columns[added++ % columns.Length];
            var b = UIFactory.CreateButton(column, "Row" + added, "", () => { edit(A); SaveSystem.Save(); Refresh(); });
            ((RectTransform)b.transform).sizeDelta = new Vector2(600f, 84f);
            rows.Add((b, text));
        }

        private static string OnOff(bool on) => Strings.Get(on ? "on" : "off");

        private void AddRows()
        {
            Row(a => Strings.Format("captions_on", OnOff(a.captions)), a => a.captions = !a.captions);
            Row(a => Strings.Format("caption_sounds", Strings.Get(ModeKey(a.soundCaptions))), a => a.soundCaptions = (SoundCaptionMode)(((int)a.soundCaptions + 1) % 3));
            Row(a => Strings.Format("caption_size", Mathf.RoundToInt(a.subtitleSize * 100f)), a => a.subtitleSize = CaptionRules.NextSize(a.subtitleSize));
            Row(a => Strings.Format("caption_backdrop", Mathf.RoundToInt(a.captionBackdrop * 100f)), a => a.captionBackdrop = NextBackdrop(a.captionBackdrop));
            Row(a => Strings.Format("text_size", TextSizeRules.Percent(a.textSize)), a => a.textSize = TextSizeRules.Next(a.textSize));
            Row(a => Strings.Format("left_handed", OnOff(a.leftHanded)), a => a.leftHanded = !a.leftHanded);
            Row(a => Strings.Format("button_size", Mathf.RoundToInt(a.buttonSize * 100f)), a => a.buttonSize = ControlLayout.NextSize(a.buttonSize));
            Row(a => Strings.Format("button_opacity", Mathf.RoundToInt(a.buttonOpacity * 100f)), a => a.buttonOpacity = ControlLayout.NextOpacity(a.buttonOpacity));
            Row(a => Strings.Get("move_buttons"), a => ControlLayoutScreen.Open(transform));
            Row(a => Strings.Format("assist_mode", OnOff(a.assistMode)), a => a.assistMode = !a.assistMode);
            Row(a => Strings.Format("photo_safe", OnOff(a.photoSafe)), a => a.photoSafe = !a.photoSafe);
            Row(a => Strings.Format("reduce_flash", OnOff(a.reduceFlash)), a => a.reduceFlash = !a.reduceFlash);
            Row(a => Strings.Format("reduce_motion", OnOff(a.reduceMotion)), a => a.reduceMotion = !a.reduceMotion);
            Row(a => Strings.Format("cvd_row", Strings.Get(ColourVision.LabelKey(ColourVision.Parse(a.colourFilter)))),
                a => a.colourFilter = ColourVision.Name(ColourVision.Next(ColourVision.Parse(a.colourFilter))));
        }

        private static string ModeKey(SoundCaptionMode m) => m == SoundCaptionMode.Off ? "cap_mode_off" : m == SoundCaptionMode.All ? "cap_mode_all" : "cap_mode_important";

        private static float NextBackdrop(float current)
        {
            int best = 0;
            for (int i = 0; i < Backdrops.Length; i++) if (Mathf.Abs(Backdrops[i] - current) < Mathf.Abs(Backdrops[best] - current)) best = i;
            return Backdrops[(best + 1) % Backdrops.Length];
        }

        private void Refresh()
        {
            var a = A;
            Localization.ArabicTMP.SetText(hint, a.assistMode ? Strings.Get("assist_hint") : "");
            foreach (var r in rows) UIFactory.SetButtonText(r.button, r.text(a));
            // a live preview of the caption, so the size and background can be judged without playing
            Localization.ArabicTMP.SetText(sample, Strings.Get("caption_sample"));
            sample.fontSize = CaptionRules.FontSize(a.subtitleSize);
            sample.enableAutoSizing = false;
            for (int i = 0; i < swatches.Length; i++) swatches[i].color = AccessColours.Of(AccessPalette.Roles[i]);
            sampleBg.sizeDelta = new Vector2(1000f, sample.fontSize * 2f);
            sample.color = Color.white;
            sampleBg.GetComponent<Image>().color = new Color(0f, 0f, 0f, a.captionBackdrop);
        }
    }
}
