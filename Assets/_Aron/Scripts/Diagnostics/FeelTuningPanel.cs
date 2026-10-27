// (c) Ahmed Alamrani. All rights reserved.
using System.Collections.Generic;
using System.Reflection;
using Aron.Config;
using Aron.Logic.GameFeel;
using Aron.UI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Aron.Diagnostics
{
    /// <summary>
    /// Hidden on-device tuning panel for the game feel (docs/game_feel.md). It is opened from the performance overlay (five taps on the main menu's
    /// version label, then the "Feel tuning" button) and, once opened, leaves a small "FEEL" chip on screen so it can be opened and closed in the middle of
    /// a stage. One slider per value in <see cref="FeelValues"/> (built from the field list, so a new value shows up by itself), a minus and plus button for
    /// fine steps, and a highlight on every value that differs from the shipped one.
    /// Save keeps the numbers on this phone (loaded again on the next start); Reset goes back to the shipped values; Export writes the JSON to
    /// game_feel_export.json in the app's data folder and copies it to the clipboard, ready to paste over Resources/Config/game_feel.json in the repo.
    /// It is a developer tool: English only, never reachable by a player who does not know the secret, and it is not part of the localised UI.
    /// While it is open, touches on the panel do not steer the hero (<see cref="BlocksTouch"/>).
    /// </summary>
    public class FeelTuningPanel : MonoBehaviour
    {
        private static FeelTuningPanel instance;

        private RectTransform panel, chip, content;
        private TextMeshProUGUI status;
        private readonly List<Row> rows = new List<Row>();
        private readonly Dictionary<string, GameObject> groupBodies = new Dictionary<string, GameObject>();
        private readonly Dictionary<string, TextMeshProUGUI> groupTitles = new Dictionary<string, TextMeshProUGUI>();

        private static readonly Color Amber = new Color(1f, 0.78f, 0.25f);
        private static readonly Color Calm = new Color(0.9f, 0.9f, 0.9f);

        private class Row
        {
            public FieldInfo Field;
            public FeelAttribute Info;
            public Slider Slider;
            public TextMeshProUGUI Name, Value;
        }

        public static bool IsOpen => instance != null && instance.panel != null && instance.panel.gameObject.activeSelf;

        /// <summary>Opens (building it the first time) or closes the panel.</summary>
        public static void Toggle()
        {
            if (instance == null) instance = Build();
            instance.panel.gameObject.SetActive(!instance.panel.gameObject.activeSelf);
            instance.chip.gameObject.SetActive(true);
            if (instance.panel.gameObject.activeSelf) instance.RefreshAll();
        }

        /// <summary>True when this screen point is on the open panel or on the chip, so the touch input leaves it alone.</summary>
        public static bool BlocksTouch(Vector2 screenPoint)
        {
            if (instance == null) return false;
            if (instance.chip != null && instance.chip.gameObject.activeSelf && RectTransformUtility.RectangleContainsScreenPoint(instance.chip, screenPoint, null)) return true;
            return instance.panel != null && instance.panel.gameObject.activeSelf && RectTransformUtility.RectangleContainsScreenPoint(instance.panel, screenPoint, null);
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics() => instance = null;

        private static FeelTuningPanel Build()
        {
            var go = new GameObject("FeelTuningPanel");
            DontDestroyOnLoad(go);
            var p = go.AddComponent<FeelTuningPanel>();
            p.BuildUi();
            return p;
        }

        // ------------------------------------------------------------------ UI
        private void BuildUi()
        {
            var canvas = UIFactory.CreateCanvas("FeelCanvas", 5001);
            canvas.transform.SetParent(transform, false);

            chip = (RectTransform)UIFactory.CreateButton(canvas.transform, "Chip", "FEEL", () => panel.gameObject.SetActive(!panel.gameObject.activeSelf)).transform;
            UIFactory.Anchor(chip, new Vector2(0f, 0.5f), new Vector2(8f, 0f), new Vector2(150f, 64f));
            chip.GetComponentInChildren<TMP_Text>().fontSize = 30f;

            panel = UIFactory.Panel(canvas.transform, "Panel", new Color(0.04f, 0.05f, 0.07f, 0.88f), false);
            UIFactory.Anchor(panel, new Vector2(1f, 0.5f), new Vector2(-8f, 0f), new Vector2(860f, 1040f));

            var title = UIFactory.Label(panel, "Title", "Game feel tuning", 38f, Color.white, TextAlignmentOptions.Left);
            UIFactory.Anchor((RectTransform)title.transform, new Vector2(0f, 1f), new Vector2(16f, -10f), new Vector2(560f, 60f));

            // Scroll area
            var scrollGo = new GameObject("Scroll", typeof(RectTransform), typeof(ScrollRect));
            scrollGo.transform.SetParent(panel, false);
            var scrollRt = (RectTransform)scrollGo.transform;
            scrollRt.anchorMin = Vector2.zero; scrollRt.anchorMax = Vector2.one;
            scrollRt.offsetMin = new Vector2(8f, 150f); scrollRt.offsetMax = new Vector2(-8f, -76f);
            var viewport = UIFactory.Panel(scrollRt, "Viewport", new Color(1f, 1f, 1f, 0.02f));
            viewport.gameObject.AddComponent<RectMask2D>();
            content = new GameObject("Content", typeof(RectTransform), typeof(VerticalLayoutGroup), typeof(ContentSizeFitter)).GetComponent<RectTransform>();
            content.SetParent(viewport, false);
            content.anchorMin = new Vector2(0f, 1f); content.anchorMax = new Vector2(1f, 1f); content.pivot = new Vector2(0.5f, 1f);
            content.offsetMin = new Vector2(0f, 0f); content.offsetMax = new Vector2(0f, 0f);
            var vlg = content.GetComponent<VerticalLayoutGroup>();
            vlg.spacing = 6f; vlg.childControlHeight = true; vlg.childControlWidth = true; vlg.childForceExpandHeight = false; vlg.childForceExpandWidth = true;
            vlg.padding = new RectOffset(6, 6, 6, 6);
            content.GetComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            var scroll = scrollGo.GetComponent<ScrollRect>();
            scroll.viewport = viewport; scroll.content = content; scroll.horizontal = false; scroll.vertical = true;
            scroll.movementType = ScrollRect.MovementType.Clamped; scroll.scrollSensitivity = 40f;

            bool first = true;
            foreach (var group in FeelValues.Groups())
            {
                BuildGroup(group, first);
                first = false;
            }

            status = UIFactory.Label(panel, "Status", "", 26f, new Color(0.7f, 0.95f, 0.7f), TextAlignmentOptions.Left);
            var srt = (RectTransform)status.transform;
            srt.anchorMin = new Vector2(0f, 0f); srt.anchorMax = new Vector2(1f, 0f); srt.pivot = new Vector2(0f, 0f);
            srt.offsetMin = new Vector2(16f, 84f); srt.offsetMax = new Vector2(-16f, 146f);
            status.textWrappingMode = TMPro.TextWrappingModes.Normal;

            AddButton("Save", new Vector2(0f, 0f), new Vector2(12f, 12f), 200f, Save);
            AddButton("Reset", new Vector2(0f, 0f), new Vector2(220f, 12f), 200f, Reset);
            AddButton("Export", new Vector2(0f, 0f), new Vector2(428f, 12f), 200f, ExportJson);
            AddButton("Close", new Vector2(1f, 0f), new Vector2(-12f, 12f), 200f, () => panel.gameObject.SetActive(false));

            panel.gameObject.SetActive(false);
        }

        private void AddButton(string text, Vector2 anchor, Vector2 pos, float width, System.Action onClick)
        {
            var b = UIFactory.CreateButton(panel, text, text, onClick);
            UIFactory.Anchor((RectTransform)b.transform, anchor, pos, new Vector2(width, 64f));
            b.GetComponentInChildren<TMP_Text>().fontSize = 30f;
        }

        private void BuildGroup(string group, bool open)
        {
            var header = UIFactory.CreateButton(content, "H_" + group, "", null);
            var hl = header.gameObject.AddComponent<LayoutElement>(); hl.preferredHeight = 64f;
            header.GetComponent<Image>().color = new Color(0.12f, 0.3f, 0.2f, 1f);
            var title = header.GetComponentInChildren<TextMeshProUGUI>();
            title.fontSize = 32f; title.alignment = TextAlignmentOptions.Left;
            title.margin = new Vector4(16f, 0f, 0f, 0f);
            groupTitles[group] = title;

            var body = new GameObject("G_" + group, typeof(RectTransform), typeof(VerticalLayoutGroup));
            body.transform.SetParent(content, false);
            var bl = body.GetComponent<VerticalLayoutGroup>();
            bl.spacing = 4f; bl.childControlHeight = true; bl.childControlWidth = true; bl.childForceExpandHeight = false; bl.childForceExpandWidth = true;
            groupBodies[group] = body;
            header.onClick.AddListener(() => { body.SetActive(!body.activeSelf); SetGroupTitle(group); });

            foreach (var f in FeelValues.Fields)
            {
                var info = FeelValues.Info(f);
                if (info.Group != group) continue;
                rows.Add(BuildRow(body.transform, f, info));
            }
            body.SetActive(open);
            SetGroupTitle(group);
        }

        private void SetGroupTitle(string group)
        {
            bool open = groupBodies[group].activeSelf;
            int changed = 0;
            foreach (var r in rows) if (r.Info.Group == group && IsChanged(r)) changed++;
            groupTitles[group].text = (open ? "v  " : ">  ") + group + (changed > 0 ? "   (" + changed + " changed)" : "");
        }

        private Row BuildRow(Transform parent, FieldInfo f, FeelAttribute info)
        {
            var rowGo = new GameObject("R_" + f.Name, typeof(RectTransform), typeof(LayoutElement));
            rowGo.transform.SetParent(parent, false);
            rowGo.GetComponent<LayoutElement>().preferredHeight = 112f;
            var rt = (RectTransform)rowGo.transform;

            var row = new Row { Field = f, Info = info };
            row.Name = UIFactory.Label(rt, "Name", f.Name, 28f, Calm, TextAlignmentOptions.Left);
            var nrt = (RectTransform)row.Name.transform;
            nrt.anchorMin = new Vector2(0f, 1f); nrt.anchorMax = new Vector2(0.62f, 1f); nrt.pivot = new Vector2(0f, 1f);
            nrt.offsetMin = new Vector2(10f, -44f); nrt.offsetMax = new Vector2(0f, 0f);
            row.Value = UIFactory.Label(rt, "Value", "", 28f, Color.white, TextAlignmentOptions.Right);
            var vrt = (RectTransform)row.Value.transform;
            vrt.anchorMin = new Vector2(0.62f, 1f); vrt.anchorMax = new Vector2(1f, 1f); vrt.pivot = new Vector2(1f, 1f);
            vrt.offsetMin = new Vector2(0f, -44f); vrt.offsetMax = new Vector2(-10f, 0f);

            bool whole = f.FieldType == typeof(int);
            float step = whole ? 1f : (info.Max - info.Min) / 200f;
            row.Slider = BuildSlider(rt, info.Min, info.Max, whole);
            var srt = (RectTransform)row.Slider.transform;
            srt.anchorMin = new Vector2(0f, 0f); srt.anchorMax = new Vector2(1f, 0f); srt.pivot = new Vector2(0.5f, 0f);
            srt.offsetMin = new Vector2(96f, 6f); srt.offsetMax = new Vector2(-96f, 66f);

            var minus = UIFactory.CreateButton(rt, "Minus", "-", () => Nudge(row, -step));
            UIFactory.Anchor((RectTransform)minus.transform, new Vector2(0f, 0f), new Vector2(8f, 4f), new Vector2(80f, 62f));
            var plus = UIFactory.CreateButton(rt, "Plus", "+", () => Nudge(row, step));
            UIFactory.Anchor((RectTransform)plus.transform, new Vector2(1f, 0f), new Vector2(-8f, 4f), new Vector2(80f, 62f));

            row.Slider.onValueChanged.AddListener(v => { FeelSettings.Set(f.Name, v); UpdateRow(row); SetGroupTitle(info.Group); });
            return row;
        }

        private static Slider BuildSlider(Transform parent, float min, float max, bool whole)
        {
            var go = new GameObject("Slider", typeof(RectTransform), typeof(Slider));
            go.transform.SetParent(parent, false);
            var bg = UIFactory.Panel(go.transform, "Background", new Color(1f, 1f, 1f, 0.18f), false);
            bg.anchorMin = new Vector2(0f, 0.5f); bg.anchorMax = new Vector2(1f, 0.5f); bg.sizeDelta = new Vector2(0f, 14f); bg.anchoredPosition = Vector2.zero;
            var fillArea = new GameObject("FillArea", typeof(RectTransform)).GetComponent<RectTransform>();
            fillArea.SetParent(go.transform, false);
            fillArea.anchorMin = new Vector2(0f, 0.5f); fillArea.anchorMax = new Vector2(1f, 0.5f); fillArea.sizeDelta = new Vector2(-30f, 14f); fillArea.anchoredPosition = Vector2.zero;
            var fill = UIFactory.Panel(fillArea, "Fill", new Color(0.2f, 0.7f, 0.4f, 1f), false);
            fill.anchorMin = Vector2.zero; fill.anchorMax = new Vector2(0f, 1f); fill.sizeDelta = new Vector2(10f, 0f);
            var handleArea = new GameObject("HandleArea", typeof(RectTransform)).GetComponent<RectTransform>();
            handleArea.SetParent(go.transform, false);
            handleArea.anchorMin = Vector2.zero; handleArea.anchorMax = Vector2.one; handleArea.offsetMin = new Vector2(30f, 0f); handleArea.offsetMax = new Vector2(-30f, 0f);
            var handle = UIFactory.Panel(handleArea, "Handle", Color.white, false);
            handle.anchorMin = handle.anchorMax = new Vector2(0f, 0.5f); handle.sizeDelta = new Vector2(52f, 52f);

            var s = go.GetComponent<Slider>();
            s.fillRect = fill; s.handleRect = handle; s.targetGraphic = handle.GetComponent<Image>();
            s.direction = Slider.Direction.LeftToRight;
            s.minValue = min; s.maxValue = max; s.wholeNumbers = whole;
            return s;
        }

        // ------------------------------------------------------------------ Values
        private static void Nudge(Row row, float delta) => row.Slider.value = Mathf.Clamp(row.Slider.value + delta, row.Info.Min, row.Info.Max);

        private static bool IsChanged(Row r)
        {
            float live = FeelSettings.V.Get(r.Field), shipped = FeelSettings.Shipped.Get(r.Field);
            return Mathf.Abs(live - shipped) > (r.Info.Max - r.Info.Min) * 0.0005f;
        }

        private void UpdateRow(Row r)
        {
            float v = FeelSettings.V.Get(r.Field);
            string unit = string.IsNullOrEmpty(r.Info.Unit) ? "" : " " + r.Info.Unit;
            r.Value.text = (r.Field.FieldType == typeof(int) ? ((int)v).ToString() : v.ToString("0.###")) + unit;
            bool changed = IsChanged(r);
            r.Name.color = changed ? Amber : Calm;
            r.Value.color = changed ? Amber : Color.white;
        }

        private void RefreshAll()
        {
            foreach (var r in rows)
            {
                r.Slider.SetValueWithoutNotify(FeelSettings.V.Get(r.Field));
                UpdateRow(r);
            }
            foreach (var g in groupBodies.Keys) SetGroupTitle(g);
        }

        private void Say(string text) => status.text = text;

        private void Save() => Say(FeelSettings.Save() ? "Saved on this phone. It loads again on the next start." : "Could not save (see the log).");

        private void Reset()
        {
            FeelSettings.ResetToShipped();
            RefreshAll();
            Say("Back to the shipped values; the saved tuning was deleted.");
        }

        private void ExportJson()
        {
            string path = FeelSettings.Export();
            Say(path != null ? "Copied to the clipboard and written to " + path + ". Paste over Resources/Config/game_feel.json." : "Copied to the clipboard (the file could not be written).");
        }
    }
}
