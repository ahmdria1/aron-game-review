// (c) Ahmed Alamrani. All rights reserved.
using System.Collections;
using Aron.Localization;
using Aron.UI;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Aron.Core
{
    /// <summary>
    /// Loads scenes in the background behind a small loading screen, so the game never looks frozen and the frame-rate
    /// and watchdog logic know a load is under way. Replaces SceneManager.LoadScene in game code.
    /// </summary>
    public class SceneLoader : MonoBehaviour
    {
        private const float MinShown = 0.35f;
        private static SceneLoader instance;

        public static bool IsLoading { get; private set; }

        public static void Load(string scene)
        {
            if (IsLoading) return;
            if (instance == null)
            {
                var go = new GameObject("SceneLoader");
                DontDestroyOnLoad(go);
                instance = go.AddComponent<SceneLoader>();
            }
            instance.StartCoroutine(instance.Run(scene));
        }

        private RectTransform fill;
        private GameObject screen;

        private IEnumerator Run(string scene)
        {
            var op = SceneManager.LoadSceneAsync(scene);
            if (op == null)
            {
                global::Aron.Logic.Diagnostics.Log.Error("[Aron] Scene not found: " + scene);
                yield break;
            }
            IsLoading = true;
            Diagnostics.CrashReporter.Crumb("load:" + scene);
            Diagnostics.CrashReporter.SuspendWatchdog();
            op.allowSceneActivation = false;
            Show();
            float start = Time.unscaledTime;
            while (op.progress < 0.9f) { SetProgress(op.progress / 0.9f); yield return null; }
            SetProgress(1f);
            while (Time.unscaledTime - start < MinShown) yield return null;
            op.allowSceneActivation = true;
            yield return op;
            Hide();
            IsLoading = false;
            Diagnostics.CrashReporter.ResumeWatchdog();
        }

        private void Show()
        {
            var canvas = UIFactory.CreateCanvas("LoadingCanvas", 500);
            canvas.transform.SetParent(transform, false);
            screen = canvas.gameObject;
            UIFactory.Panel(canvas.transform, "Bg", new Color(0.08f, 0.05f, 0.03f, 1f));
            var safe = UIFactory.SafeRoot(canvas);
            var label = UIFactory.Label(safe, "Text", Strings.Get("loading"), 56f, UIFactory.Sand);
            UIFactory.Anchor(label.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0f, 60f), new Vector2(1200f, 100f));
            label.rectTransform.pivot = new Vector2(0.5f, 0.5f);
            var track = UIFactory.Panel(safe, "Track", new Color(1f, 1f, 1f, 0.15f), false);
            UIFactory.Anchor(track, new Vector2(0.5f, 0.5f), new Vector2(0f, -40f), new Vector2(800f, 24f));
            track.pivot = new Vector2(0.5f, 0.5f);
            fill = UIFactory.Panel(track, "Fill", UIFactory.Green, true);
            SetProgress(0f);
        }

        private void SetProgress(float p)
        {
            if (fill != null) fill.anchorMax = new Vector2(Mathf.Clamp01(p), 1f);
        }

        private void Hide()
        {
            if (screen != null) Destroy(screen);
            screen = null; fill = null;
        }
    }
}
