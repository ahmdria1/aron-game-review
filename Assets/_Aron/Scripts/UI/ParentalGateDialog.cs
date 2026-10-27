// (c) Ahmed Alamrani. All rights reserved.
using System;
using Aron.Localization;
using Aron.Logic;
using Aron.Logic.Account;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Aron.UI
{
    /// <summary>
    /// The grown-up check shown before anything leaves the game (web shops, links) and before a parent's email is asked
    /// for: birth year plus a times-table question. Passing it lets the grown-up through for five minutes.
    /// </summary>
    public class ParentalGateDialog : MonoBehaviour
    {
        private static readonly System.Random Rnd = new System.Random();
        private static readonly ParentalGate Gate = new ParentalGate(Rnd);
        private static float passedUntil = -1f;
        private const float PassSeconds = 300f;

        private Action onPass;
        private TextMeshProUGUI question, message;
        private TMP_InputField year, answer;

        /// <summary>Runs <paramref name="onPass"/> once a grown-up has answered (straight away if they did so recently).</summary>
        public static void Ask(Action onPass, string why = null)
        {
            if (Time.unscaledTime < passedUntil) { onPass?.Invoke(); return; }
            UIFactory.EnsureEventSystem();
            var canvas = UIFactory.CreateCanvas("ParentalGate", 900);
            var dlg = canvas.gameObject.AddComponent<ParentalGateDialog>();
            dlg.onPass = onPass;
            dlg.Build(UIFactory.SafeRoot(canvas), why);
        }

        private void Build(RectTransform root, string why)
        {
            var dim = UIFactory.Panel(root, "Dim", new Color(0f, 0f, 0f, 0.75f));
            var box = UIFactory.Panel(dim, "Box", UIFactory.Sand, false);
            UIFactory.Anchor(box, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(980f, 980f));
            var col = UIFactory.VerticalGroup(box, "Col", 24f);

            UIFactory.Label(col, "Title", Strings.Get("gate_title"), 60f, Color.black);
            UIFactory.Label(col, "Body", why ?? Strings.Get("gate_body"), 38f, Color.black);
            year = Field(col, "Year", Strings.Get("gate_birth_year"), 4);
            question = UIFactory.Label(col, "Question", "", 44f, Color.black);
            answer = Field(col, "Answer", "?", 3);
            message = UIFactory.Label(col, "Message", "", 34f, new Color(0.6f, 0.1f, 0.1f));
            UIFactory.CreateButton(col, "Ok", Strings.Get("gate_ok"), Submit);
            UIFactory.CreateButton(col, "Back", Strings.Get("gate_cancel"), Close);
            ShowQuestion();
        }

        private static TMP_InputField Field(Transform parent, string name, string hint, int max)
        {
            var go = TMP_DefaultControls.CreateInputField(new TMP_DefaultControls.Resources());
            go.name = name;
            go.transform.SetParent(parent, false);
            ((RectTransform)go.transform).sizeDelta = new Vector2(560f, 100f);
            var f = go.GetComponent<TMP_InputField>();
            f.contentType = TMP_InputField.ContentType.IntegerNumber;
            f.characterLimit = max;
            f.pointSize = 44f;
            if (f.placeholder is TMP_Text ph) ArabicTMP.SetText(ph, hint);
            return f;
        }

        private void ShowQuestion()
        {
            ArabicTMP.SetText(question, Strings.Format("gate_question", Gate.a, Gate.b));
            if (answer != null) answer.text = "";
        }

        private void Submit()
        {
            var now = DateTime.UtcNow;
            if (Gate.Locked(now)) { ArabicTMP.SetText(message, Strings.Get("gate_locked")); return; }
            int.TryParse(year.text, out int y);
            int.TryParse(answer.text, out int p);
            if (Gate.Answer(y, p, now.Year, now, Rnd))
            {
                passedUntil = Time.unscaledTime + PassSeconds;
                var cb = onPass;
                Close();
                cb?.Invoke();
                return;
            }
            ArabicTMP.SetText(message, Strings.Get(Gate.Locked(now) ? "gate_locked" : "gate_wrong"));
            ShowQuestion();
        }

        private void Close()
        {
            onPass = null;
            Destroy(gameObject);
        }
    }
}
