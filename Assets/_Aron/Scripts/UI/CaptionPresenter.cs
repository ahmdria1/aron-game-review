// (c) Ahmed Alamrani. All rights reserved.
using System.Text;
using Aron.Core;
using Aron.Localization;
using Aron.Logic.Access;
using Aron.Save;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Aron.UI
{
    /// <summary>
    /// Shows a caption for every spoken line and every sound that has an entry in Resources/Config/captions.json. It listens to the
    /// game events, so no gameplay code calls it. The rules (what is shown, how long, how many lines) are in Aron.Logic.Access.
    /// </summary>
    public class CaptionPresenter : MonoBehaviour
    {
        private const string FilePath = "Config/captions";

        private static CaptionPresenter instance;
        private static CaptionFile file;

        private readonly CaptionQueue queue = new CaptionQueue();
        private Image[] slots;
        private TextMeshProUGUI[] labels;
        private string shown = "";

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Boot()
        {
            if (instance != null) return;
            var go = new GameObject("[Captions]");
            DontDestroyOnLoad(go);
            instance = go.AddComponent<CaptionPresenter>();
        }

        /// <summary>The caption table, loaded once (empty when the file is missing, which just means no captions).</summary>
        public static CaptionFile File
        {
            get
            {
                if (file != null) return file;
                var asset = Resources.Load<TextAsset>(FilePath);
                try { file = asset != null ? JsonUtility.FromJson<CaptionFile>(asset.text) : null; }
                catch (System.Exception e) { Aron.Logic.Diagnostics.Log.Warn("[Captions] captions.json: " + e.Message); }
                if (file == null) file = new CaptionFile();
                return file;
            }
        }

        private void OnEnable()
        {
            GameEvents.SfxRequested += OnSound;
            GameEvents.SoundPlayed += OnSound;
            GameEvents.VoiceSpoken += OnVoice;
        }

        private void OnDisable()
        {
            GameEvents.SfxRequested -= OnSound;
            GameEvents.SoundPlayed -= OnSound;
            GameEvents.VoiceSpoken -= OnVoice;
        }

        private void OnDestroy() { if (instance == this) instance = null; }

        private void OnSound(string id) => Cue(id, null);

        private void OnVoice(string id, string speaker) => Cue(id, speaker);

        private void Cue(string id, string speaker)
        {
            var access = SaveSystem.Data != null ? SaveSystem.Data.access : null;
            if (access == null) access = new Aron.Logic.Safety.AccessSettings();
            var entry = File.Find(id);
            if (!CaptionPolicy.Show(access, entry)) return;
            string text = File.Text(id, Strings.Language);
            queue.Request(id, text, entry.GetKind(), string.IsNullOrEmpty(speaker) ? entry.speaker : speaker, Time.unscaledTimeAsDouble);
        }

        private void Update()
        {
            queue.Tick(Time.unscaledTimeAsDouble);
            var access = SaveSystem.Data != null ? SaveSystem.Data.access : null;
            float size = access != null ? access.subtitleSize : 1f;
            float backdrop = access != null ? access.captionBackdrop : 0.75f;
            var sig = new StringBuilder();
            foreach (var l in queue.Lines) sig.Append(l.Id).Append('|');
            sig.Append(size).Append('|').Append(backdrop).Append('|').Append(Strings.Language);
            string now = sig.ToString();
            if (now == shown) return;
            shown = now;
            Render(size, backdrop);
        }

        private void Build()
        {
            var canvas = UIFactory.CreateCanvas("CaptionCanvas", 200);
            canvas.transform.SetParent(transform, false);
            var safe = UIFactory.SafeRoot(canvas);
            slots = new Image[CaptionRules.MaxLines];
            labels = new TextMeshProUGUI[CaptionRules.MaxLines];
            for (int i = 0; i < slots.Length; i++)
            {
                var rt = UIFactory.Panel(safe, "Caption" + i, Color.black, false);
                slots[i] = rt.GetComponent<Image>();
                slots[i].raycastTarget = false;
                labels[i] = UIFactory.Label(rt, "Text", "", CaptionRules.BaseFontSize, Color.white);
                labels[i].raycastTarget = false;
                var lrt = labels[i].rectTransform;
                lrt.anchorMin = Vector2.zero; lrt.anchorMax = Vector2.one;
                lrt.offsetMin = new Vector2(20f, 6f); lrt.offsetMax = new Vector2(-20f, -6f);
                rt.gameObject.SetActive(false);
            }
        }

        private void Render(float size, float backdrop)
        {
            if (slots == null) Build();
            float font = CaptionRules.FontSize(size);
            float height = font * 2.5f;
            int n = queue.Lines.Count;
            for (int i = 0; i < slots.Length; i++)
            {
                bool on = i < n;
                slots[i].gameObject.SetActive(on);
                if (!on) continue;
                var line = queue.Lines[i];
                var rt = (RectTransform)slots[i].transform;
                // speech is listed first, so it sits on top; the newest line is at the bottom of its kind
                rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0f);
                rt.pivot = new Vector2(0.5f, 0f);
                rt.sizeDelta = new Vector2(1500f, height);
                rt.anchoredPosition = new Vector2(0f, 40f + (n - 1 - i) * (height + 8f));
                slots[i].color = new Color(0f, 0f, 0f, backdrop);
                labels[i].color = line.Kind == CaptionKind.Sound ? new Color(0.85f, 0.9f, 1f) : line.Speaker == "noura" ? new Color(1f, 0.85f, 0.95f) : Color.white;
                labels[i].enableAutoSizing = false;
                labels[i].fontSize = font;
                ArabicTMP.SetText(labels[i], line.Text);
            }
        }
    }
}
