// (c) Ahmed Alamrani. All rights reserved.
using Aron.Localization;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Aron.Dialogue
{
    /// <summary>World-space bubble that follows a speaker and faces the camera. Never pauses gameplay.</summary>
    public class SpeechBubble : MonoBehaviour
    {
        private TextMeshProUGUI label;
        private CanvasGroup group;
        private float hideAt;
        private Camera cam;

        public static SpeechBubble Create(Transform parent, Vector3 offset)
        {
            var go = new GameObject("SpeechBubble", typeof(RectTransform));
            go.transform.SetParent(parent, false);
            go.transform.localPosition = offset;
            go.transform.localScale = Vector3.one * 0.01f;
            var canvas = go.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            canvas.sortingOrder = 10;
            var rt = (RectTransform)go.transform;
            rt.sizeDelta = new Vector2(420f, 140f);

            var bg = new GameObject("Bg", typeof(RectTransform), typeof(Image));
            bg.transform.SetParent(go.transform, false);
            var bgRt = (RectTransform)bg.transform;
            bgRt.anchorMin = Vector2.zero; bgRt.anchorMax = Vector2.one; bgRt.offsetMin = bgRt.offsetMax = Vector2.zero;
            bg.GetComponent<Image>().color = new Color(1f, 1f, 1f, 0.92f);

            var txt = new GameObject("Text", typeof(RectTransform));
            txt.transform.SetParent(go.transform, false);
            var tRt = (RectTransform)txt.transform;
            tRt.anchorMin = Vector2.zero; tRt.anchorMax = Vector2.one; tRt.offsetMin = new Vector2(16f, 8f); tRt.offsetMax = new Vector2(-16f, -8f);
            var b = go.AddComponent<SpeechBubble>();
            b.label = txt.AddComponent<TextMeshProUGUI>();
            b.label.fontSize = 34f;
            b.label.color = new Color(0.1f, 0.1f, 0.1f);
            b.label.alignment = TextAlignmentOptions.Center;
            b.group = go.AddComponent<CanvasGroup>();
            b.group.alpha = 0f;
            b.group.blocksRaycasts = false;
            return b;
        }

        public void Show(string text, float duration)
        {
            ArabicTMP.SetText(label, text);
            hideAt = Time.time + Mathf.Max(0.5f, duration);
            group.alpha = 1f;
        }

        private void LateUpdate()
        {
            if (cam == null) cam = Camera.main;
            if (cam != null) transform.rotation = Quaternion.LookRotation(transform.position - cam.transform.position, Vector3.up);
            if (group.alpha > 0f && Time.time > hideAt) group.alpha = Mathf.MoveTowards(group.alpha, 0f, Time.deltaTime * 4f);
        }
    }
}
