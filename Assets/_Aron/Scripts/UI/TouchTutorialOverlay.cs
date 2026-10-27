// (c) Ahmed Alamrani. All rights reserved.
using Aron.Core;
using Aron.Input;
using Aron.Localization;
using Aron.Logic;
using Aron.Player;
using Aron.Save;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Aron.UI
{
    /// <summary>
    /// First-play gesture lessons: a caption and an animated finger acting the gesture out on the character.
    /// Nothing here can be pressed; every card goes away once the player has done the move for real.
    /// </summary>
    public class TouchTutorialOverlay : MonoBehaviour
    {
        private const int SpriteSize = 256;

        private TouchTutorial tutorial;
        private RectTransform canvasRect, card, hand, hand2;
        private Image handImage, hand2Image, ring, ring2;
        private CanvasGroup group;
        private TextMeshProUGUI caption;
        private bool female;
        private float stepTime, cardPop;
        private bool finishing;

        private void Start()
        {
            var save = SaveSystem.Data;
            if (save.touchTutorialStep >= (int)TutorialStep.Done || !HasTouch()) { enabled = false; return; }
            tutorial = new TouchTutorial(save.touchTutorialStep);
            female = save.selectedCharacterId == "noura";
            Build();
            ShowStep();
        }

        public static bool HasTouch()
        {
#if ENABLE_INPUT_SYSTEM
            return UnityEngine.InputSystem.Touchscreen.current != null;
#else
            return UnityEngine.Input.touchSupported;
#endif
        }

        private void Build()
        {
            var canvas = UIFactory.CreateCanvas("TutorialCanvas", 55);
            canvas.transform.SetParent(transform, false);
            canvas.GetComponent<GraphicRaycaster>().enabled = false;
            canvasRect = (RectTransform)canvas.transform;
            group = canvas.gameObject.AddComponent<CanvasGroup>();
            group.blocksRaycasts = false;
            group.interactable = false;

            card = UIFactory.Panel(UIFactory.SafeRoot(canvas), "Card", new Color(0f, 0f, 0f, 0.55f), false);
            card.GetComponent<Image>().raycastTarget = false;
            UIFactory.Anchor(card, new Vector2(0.5f, 1f), new Vector2(0f, -190f), new Vector2(1250f, 100f));
            caption = UIFactory.Label(card, "Caption", "", 44f, UIFactory.Sand);
            var crt = caption.rectTransform;
            crt.anchorMin = Vector2.zero; crt.anchorMax = Vector2.one; crt.offsetMin = crt.offsetMax = Vector2.zero;

            var ringSprite = MakeSprite(FingerShape.Ring, new Vector2(0.5f, 0.5f));
            var handSprite = MakeSprite(FingerShape.Hand, new Vector2(0.5f, FingerArt.TipV));
            ring = MakeImage("Ring", ringSprite, 130f);
            ring2 = MakeImage("Ring2", ringSprite, 130f);
            handImage = MakeImage("Finger", handSprite, 190f);
            hand2Image = MakeImage("Finger2", handSprite, 190f);
            hand = handImage.rectTransform;
            hand2 = hand2Image.rectTransform;
        }

        private Image MakeImage(string name, Sprite sprite, float size)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(canvasRect, false);
            var img = go.GetComponent<Image>();
            img.sprite = sprite;
            img.raycastTarget = false;
            var rt = img.rectTransform;
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = sprite.pivot / sprite.rect.size;
            rt.sizeDelta = new Vector2(size, size);
            return img;
        }

        private static Sprite MakeSprite(FingerShape shape, Vector2 pivot)
        {
            float[] px = FingerArt.Render(shape, SpriteSize);
            var colors = new Color[SpriteSize * SpriteSize];
            for (int i = 0; i < colors.Length; i++) colors[i] = new Color(px[i * 4], px[i * 4 + 1], px[i * 4 + 2], px[i * 4 + 3]);
            var tex = new Texture2D(SpriteSize, SpriteSize, TextureFormat.RGBA32, false) { name = "Tutorial_" + shape, wrapMode = TextureWrapMode.Clamp };
            tex.SetPixels(colors);
            tex.Apply(false, true);
            return Sprite.Create(tex, new Rect(0f, 0f, SpriteSize, SpriteSize), pivot, 100f);
        }

        private string Text(TutorialStep step)
        {
            string key = "tut_" + step.ToString().ToLowerInvariant();
            if (female)
            {
                string f = Strings.Get(key + "_f");
                if (f != key + "_f") return f;
            }
            return Strings.Get(key);
        }

        private void ShowStep()
        {
            stepTime = 0f;
            cardPop = 0f;
            ArabicTMP.SetText(caption, Text(tutorial.Step));
        }

        private void Update()
        {
            if (tutorial == null) return;
            bool running = Time.timeScale > 0f;

            if (finishing)
            {
                group.alpha = Mathf.MoveTowards(group.alpha, 0f, Time.unscaledDeltaTime * 2.5f);
                if (group.alpha <= 0f) { Destroy(canvasRect.gameObject); enabled = false; }
                return;
            }

            group.alpha = Mathf.MoveTowards(group.alpha, running ? 1f : 0f, Time.unscaledDeltaTime * 4f);
            if (!running && tutorial.Step != TutorialStep.Pause) return;

            float dt = Time.unscaledDeltaTime;
            if (tutorial.Update(TouchGestureInput.Last, dt))
            {
                SaveSystem.Data.touchTutorialStep = (int)tutorial.Step;
                SaveSystem.Save();
                GameEvents.PlaySfx("click");
                if (tutorial.IsDone) { finishing = true; return; }
                ShowStep();
            }

            stepTime += dt;
            cardPop = Mathf.MoveTowards(cardPop, 1f, dt * 5f);
            float s = 1f + Mathf.Sin(cardPop * Mathf.PI) * 0.08f;
            card.localScale = new Vector3(s, s, 1f);
            Animate();
        }

        private Vector2 CharacterPoint()
        {
            var player = PlayerController.Active;
            var cam = Camera.main;
            Vector2 screen = new Vector2(Screen.width * 0.5f, Screen.height * 0.35f);
            if (player != null && cam != null)
            {
                Vector3 p = cam.WorldToScreenPoint(player.transform.position + Vector3.up * 0.9f);
                if (p.z > 0f) screen = p;
            }
            RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, screen, null, out var local);
            return local;
        }

        private void Animate()
        {
            Vector2 at = CharacterPoint();
            hand2Image.enabled = false;
            ring2.enabled = false;
            float t = stepTime;

            switch (tutorial.Step)
            {
                case TutorialStep.Steer:
                {
                    float p = Mathf.Repeat(t, 2.4f) / 2.4f;
                    Place(hand, handImage, at + new Vector2(Mathf.Sin(p * Mathf.PI * 2f) * 150f, 0f), 1f, 1f);
                    Pulse(ring, at, Mathf.Repeat(t, 2.4f));
                    break;
                }
                case TutorialStep.Jump:
                {
                    float c = Mathf.Repeat(t, 1.5f);
                    float up = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.35f, 0.55f, c));
                    float fade = 1f - Mathf.InverseLerp(0.6f, 0.95f, c);
                    Place(hand, handImage, at + new Vector2(0f, up * 280f), fade, c < 0.35f ? 0.92f : 1f);
                    Pulse(ring, at, c - 0.05f);
                    break;
                }
                case TutorialStep.Brake:
                {
                    float c = Mathf.Repeat(t, 2.2f);
                    float down = Mathf.InverseLerp(0.3f, 1.5f, c);
                    float fade = 1f - Mathf.InverseLerp(1.7f, 2.1f, c);
                    Place(hand, handImage, at - new Vector2(0f, down * 190f), fade, 0.92f);
                    Pulse(ring, at, c - 0.05f);
                    break;
                }
                case TutorialStep.Attack:
                {
                    float c = Mathf.Repeat(t, 1.2f);
                    Vector2 spot = at + new Vector2(330f, 150f);
                    Place(hand, handImage, spot, 1f, c > 0.25f && c < 0.4f ? 0.88f : 1f);
                    Pulse(ring, spot, c - 0.3f);
                    break;
                }
                case TutorialStep.Sprint:
                {
                    float c = Mathf.Repeat(t, 1.6f);
                    bool down = (c > 0.15f && c < 0.25f) || (c > 0.4f && c < 0.5f);
                    Place(hand, handImage, at, 1f, down ? 0.88f : 1f);
                    Pulse(ring, at, c - 0.2f);
                    ring2.enabled = true;
                    Pulse(ring2, at, c - 0.45f);
                    break;
                }
                case TutorialStep.Pause:
                {
                    float c = Mathf.Repeat(t, 1.4f);
                    float press = c > 0.3f && c < 0.45f ? 0.88f : 1f;
                    Vector2 mid = new Vector2(0f, -150f);
                    Place(hand, handImage, mid + new Vector2(-75f, 0f), 1f, press);
                    hand2Image.enabled = true;
                    Place(hand2, hand2Image, mid + new Vector2(75f, 0f), 1f, press);
                    ring2.enabled = true;
                    Pulse(ring, mid + new Vector2(-75f, 0f), c - 0.35f);
                    Pulse(ring2, mid + new Vector2(75f, 0f), c - 0.35f);
                    break;
                }
            }
        }

        private static void Place(RectTransform rt, Image img, Vector2 pos, float alpha, float scale)
        {
            img.enabled = alpha > 0.01f;
            rt.anchoredPosition = pos;
            rt.localScale = new Vector3(scale, scale, 1f);
            img.color = new Color(1f, 1f, 1f, alpha);
        }

        /// <summary>A ripple that grows and fades for 0.4 s after <paramref name="age"/> passes zero.</summary>
        private static void Pulse(Image img, Vector2 pos, float age)
        {
            bool on = age >= 0f && age <= 0.4f;
            img.enabled = on;
            if (!on) return;
            float k = age / 0.4f;
            img.rectTransform.anchoredPosition = pos;
            float s = Mathf.Lerp(0.3f, 1.3f, k);
            img.rectTransform.localScale = new Vector3(s, s, 1f);
            img.color = new Color(1f, 1f, 1f, 1f - k);
        }
    }
}
