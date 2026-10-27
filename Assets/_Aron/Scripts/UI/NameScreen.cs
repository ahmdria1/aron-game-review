// (c) Ahmed Alamrani. All rights reserved.
using System;
using Aron.Localization;
using Aron.Logic;
using Aron.Logic.Names;
using Aron.Logic.Safety;
using Aron.Naming;
using Aron.Save;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Aron.UI
{
    /// <summary>
    /// "Choose your name". Shown once at first launch and from Settings. Children (and anyone whose age band is not known yet) pick from safe names;
    /// teens and adults type one. The hint says not to use a real full name. Rules and word lists: docs/player_names.md.
    /// </summary>
    public class NameScreen : MonoBehaviour
    {
        public static NameScreen Open(Transform parent, Action onDone = null)
        {
            var go = new GameObject("NameScreen");
            go.transform.SetParent(parent, false);
            var s = go.AddComponent<NameScreen>();
            s.done = onDone;
            s.Build();
            return s;
        }

        private Action done;
        private Canvas canvas;
        private RectTransform safe;
        private TMP_InputField input;
        private TextMeshProUGUI preview, status;
        private int birthYear;
        private int seed;
        private bool busy;

        private void Build()
        {
            canvas = UIFactory.CreateCanvas("NameCanvas", 40);
            canvas.transform.SetParent(transform, false);
            UIFactory.Panel(canvas.transform, "Bg", new Color(0.08f, 0.05f, 0.03f, 0.97f));
            safe = UIFactory.SafeRoot(canvas);
            seed = Environment.TickCount;
            if (string.IsNullOrEmpty(SaveSystem.Data.ageBand)) ShowAge(); else ShowName();
        }

        private void Clear()
        {
            for (int i = safe.childCount - 1; i >= 0; i--) Destroy(safe.GetChild(i).gameObject);
            input = null; preview = null; status = null;
        }

        // ---- step 1: age band (asked once, only the band is kept) ---------------------------------------------

        private void ShowAge()
        {
            Clear();
            if (birthYear == 0) birthYear = DateTime.UtcNow.Year - 10;
            var title = UIFactory.Label(safe, "Title", Strings.Get("age_title"), 60f, UIFactory.Sand);
            UIFactory.Anchor(title.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -80f), new Vector2(1500f, 100f));
            title.rectTransform.pivot = new Vector2(0.5f, 1f);
            var g = UIFactory.VerticalGroup(safe, "Rows", 24f);
            UIFactory.Anchor(g, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(1100f, 700f));
            var year = UIFactory.Label(g, "Year", birthYear.ToString(), 120f, Color.white);
            var row = new GameObject("Steps", typeof(RectTransform), typeof(HorizontalLayoutGroup));
            row.transform.SetParent(g, false);
            ((RectTransform)row.transform).sizeDelta = new Vector2(900f, 120f);
            var h = row.GetComponent<HorizontalLayoutGroup>();
            h.spacing = 24f; h.childAlignment = TextAnchor.MiddleCenter;
            h.childControlWidth = h.childControlHeight = false; h.childForceExpandWidth = h.childForceExpandHeight = false;
            Action<int> step = d =>
            {
                birthYear = Mathf.Clamp(birthYear + d, 1930, DateTime.UtcNow.Year);
                ArabicTMP.SetText(year, birthYear.ToString());
            };
            SmallButton(row.transform, "-10", () => step(-10));
            SmallButton(row.transform, "-1", () => step(-1));
            SmallButton(row.transform, "+1", () => step(1));
            SmallButton(row.transform, "+10", () => step(10));
            UIFactory.Label(g, "Note", Strings.Get("age_note"), 36f, UIFactory.Sand);
            UIFactory.CreateButton(g, "Ok", Strings.Get("name_confirm"), () =>
            {
                SaveSystem.Data.ageBand = AgeGate.Band(birthYear, DateTime.UtcNow.Year);
                SaveSystem.Save();
                ShowName();
            });
        }

        private static void SmallButton(Transform parent, string text, Action click)
        {
            var b = UIFactory.CreateButton(parent, "B" + text, text, click);
            ((RectTransform)b.transform).sizeDelta = new Vector2(190f, 110f);
        }

        // ---- step 2: the name ---------------------------------------------------------------------------------

        private void ShowName()
        {
            Clear();
            bool type = NameFlow.ModeFor(SaveSystem.Data.ageBand) == NameFlow.Mode.Type;
            var title = UIFactory.Label(safe, "Title", Strings.Get(type ? "name_title" : "name_pick_title"), 60f, UIFactory.Sand);
            UIFactory.Anchor(title.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -60f), new Vector2(1500f, 100f));
            title.rectTransform.pivot = new Vector2(0.5f, 1f);
            if (SaveSystem.Data.nameMustChange)
            {
                var warn = UIFactory.Label(safe, "MustChange", Strings.Get("name_must_change"), 38f, new Color(1f, 0.75f, 0.45f));
                UIFactory.Anchor(warn.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -170f), new Vector2(1500f, 70f));
                warn.rectTransform.pivot = new Vector2(0.5f, 1f);
            }
            if (SaveSystem.Data.playerName != "" && !SaveSystem.Data.nameMustChange)
            {
                var back = UIFactory.CreateButton(safe, "Back", Strings.Get("back"), Close);
                UIFactory.Anchor((RectTransform)back.transform, new Vector2(0f, 1f), new Vector2(30f, -20f), new Vector2(300f, 100f));
            }

            var g = UIFactory.VerticalGroup(safe, "Rows", 20f);
            UIFactory.Anchor(g, new Vector2(0.5f, 0.5f), new Vector2(0f, -60f), new Vector2(1200f, 820f));
            if (type) BuildTyped(g); else BuildPicker(g);
            UIFactory.Label(g, "Hint", Strings.Get("name_hint"), 38f, UIFactory.Sand);
            status = UIFactory.Label(g, "Status", "", 38f, new Color(1f, 0.6f, 0.5f));
        }

        private void BuildPicker(Transform g)
        {
            string lang = Strings.Language;
            foreach (var n in SafeNames.Options(seed, lang, 5))
            {
                string pick = n;
                UIFactory.CreateButton(g, "Pick", pick, () => Submit(pick));
            }
            UIFactory.CreateButton(g, "Reroll", Strings.Get("name_reroll"), () => { seed += 101; ShowName(); });
        }

        private void BuildTyped(Transform g)
        {
            var go = new GameObject("Input", typeof(RectTransform), typeof(Image), typeof(TMP_InputField));
            go.transform.SetParent(g, false);
            ((RectTransform)go.transform).sizeDelta = new Vector2(900f, 120f);
            go.GetComponent<Image>().color = new Color(1f, 1f, 1f, 0.12f);
            var vp = new GameObject("Viewport", typeof(RectTransform), typeof(RectMask2D));
            vp.transform.SetParent(go.transform, false);
            var vrt = (RectTransform)vp.transform;
            vrt.anchorMin = Vector2.zero; vrt.anchorMax = Vector2.one; vrt.offsetMin = new Vector2(20f, 8f); vrt.offsetMax = new Vector2(-20f, -8f);
            var text = UIFactory.Label(vp.transform, "Text", "", 56f, Color.white, TextAlignmentOptions.Center);
            var ph = UIFactory.Label(vp.transform, "Placeholder", Strings.Get("name_placeholder"), 46f, new Color(1f, 1f, 1f, 0.4f));
            foreach (var t in new[] { text, ph })
            {
                var r = t.rectTransform; r.anchorMin = Vector2.zero; r.anchorMax = Vector2.one; r.offsetMin = r.offsetMax = Vector2.zero;
            }
            input = go.GetComponent<TMP_InputField>();
            input.textViewport = vrt;
            input.textComponent = text;
            input.placeholder = ph;
            input.fontAsset = ArabicTMP.Font;
            input.lineType = TMP_InputField.LineType.SingleLine;
            input.characterLimit = PlayerNameRules.Max;
            input.richText = false;
            input.onValidateInput = (t, i, c) => Typeable(c) ? c : '\0';
            input.onValueChanged.AddListener(OnChanged);
            // TMP does not join Arabic letters while typing, so the typed text is also shown shaped below the field.
            preview = UIFactory.Label(g, "Preview", "", 64f, Color.white);
            UIFactory.CreateButton(g, "Ok", Strings.Get("name_confirm"), () => Submit(input.text));
        }

        public static bool Typeable(char c) =>
            PlayerNameRules.IsLatinLetter(c) || PlayerNameRules.IsArabicLetter(c) || PlayerNameRules.IsDigit(c) || PlayerNameRules.IsSeparator(c) || c == '\u0640';

        private void OnChanged(string text)
        {
            input.textComponent.isRightToLeftText = ArabicText.ContainsArabic(text);
            ArabicTMP.SetText(preview, text);
            var v = PlayerNames.Checker.Check(text);
            if (status != null) ArabicTMP.SetText(status, string.IsNullOrEmpty(text) || v.Ok || v.Code == NameCode.TooShort ? "" : ErrorText(v.Code));
        }

        private static string ErrorText(string code) => Strings.Get("name_err_" + code);

        private async void Submit(string name)
        {
            if (busy) return;
            busy = true;
            try
            {
                bool changing = !string.IsNullOrEmpty(SaveSystem.Data.playerName) && !SaveSystem.Data.nameMustChange;
                if (changing && !NameFlow.CooldownPassed(SaveSystem.Data.nameChangedTicks, DateTime.UtcNow.Ticks))
                {
                    Say(NameCode.Cooldown);
                    return;
                }
                var r = changing ? await PlayerNames.Service.ChangeAsync(name, true) : await PlayerNames.Service.ReserveAsync(name, true);
                if (r.Status == NameStatus.Ok || r.Status == NameStatus.Offline)
                {
                    PlayerNames.Apply(r);
                    if (r.Status == NameStatus.Offline) Say("offline_saved", false);
                    Close();
                    return;
                }
                Say(r.Code ?? NameCode.Error);
            }
            catch (Exception) { Say(NameCode.Error); }
            finally { busy = false; }
        }

        private void Say(string code, bool error = true)
        {
            if (status == null) return;
            ArabicTMP.SetText(status, Strings.Get("name_err_" + code));
        }

        private void Close()
        {
            done?.Invoke();
            Destroy(gameObject);
        }
    }
}
