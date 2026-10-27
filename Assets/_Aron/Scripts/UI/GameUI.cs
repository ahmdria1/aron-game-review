// (c) Ahmed Alamrani. All rights reserved.
using Aron.Core;
using Aron.Localization;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Aron.UI
{
    /// <summary>In-level HUD (text only, nothing to press while playing) + pause / game over / level complete panels.</summary>
    public class GameUI : MonoBehaviour
    {
        [SerializeField] private float hintSeconds = 7f;

        private TextMeshProUGUI datesText, livesText, hintText, bossLabel, bannerText, timerText, abilityText;
        private float bannerUntil;
        private string abilityShown;

        public static GameUI Instance { get; private set; }

        /// <summary>A line across the middle of the screen for a few seconds (storm warning, escape, waves).</summary>
        public static void Banner(string textAr, float seconds = 2.5f)
        {
            if (Instance == null || Instance.bannerText == null) return;
            ArabicTMP.SetText(Instance.bannerText, textAr);
            Instance.bannerText.gameObject.SetActive(true);
            Instance.bannerUntil = Time.time + seconds;
        }

        /// <summary>Companion ability state in the bottom corner; null hides it.</summary>
        public static void Ability(string textAr)
        {
            if (Instance == null || Instance.abilityText == null) return;
            bool show = !string.IsNullOrEmpty(textAr);
            if (Instance.abilityText.gameObject.activeSelf != show) Instance.abilityText.gameObject.SetActive(show);
            if (show && Instance.abilityShown != textAr) { Instance.abilityShown = textAr; ArabicTMP.SetText(Instance.abilityText, textAr); }
        }

        /// <summary>Countdown shown under the coins; empty text hides it.</summary>
        public static void Timer(string textAr)
        {
            if (Instance == null || Instance.timerText == null) return;
            bool show = !string.IsNullOrEmpty(textAr);
            Instance.timerText.gameObject.SetActive(show);
            if (show) ArabicTMP.SetText(Instance.timerText, textAr);
        }
        private RectTransform bossBar, bossFill;
        private GameObject pausePanel, gameOverPanel, completePanel;
        private TextMeshProUGUI bishtText;
        private Coroutine livesPop;
        private Transform canvasRoot;

        private void Awake()
        {
            Instance = this;
            Build();
            gameObject.AddComponent<TouchTutorialOverlay>();
        }

        private void OnEnable()
        {
            GameEvents.DatesChanged += OnDates;
            GameEvents.LivesChanged += OnLives;
            GameEvents.PauseChanged += OnPause;
            GameEvents.GameOver += OnGameOver;
            GameEvents.LevelCompleted += OnComplete;
            GameEvents.BossPhaseChanged += OnBossPhase;
            GameEvents.BossHealthChanged += OnBossHealth;
            GameEvents.BossDefeated += OnBossDefeated;
            GameEvents.ExtraLife += OnExtraLife;
            GameEvents.BishtChanged += OnBisht;
        }

        private void OnDisable()
        {
            GameEvents.DatesChanged -= OnDates;
            GameEvents.LivesChanged -= OnLives;
            GameEvents.PauseChanged -= OnPause;
            GameEvents.GameOver -= OnGameOver;
            GameEvents.LevelCompleted -= OnComplete;
            GameEvents.BossPhaseChanged -= OnBossPhase;
            GameEvents.BossHealthChanged -= OnBossHealth;
            GameEvents.BossDefeated -= OnBossDefeated;
            GameEvents.ExtraLife -= OnExtraLife;
            GameEvents.BishtChanged -= OnBisht;
        }

        private void Build()
        {
            var root = UIFactory.CreateCanvas("GameCanvas", 50);
            root.transform.SetParent(transform, false);
            var canvas = root.transform;
            canvasRoot = canvas;
            var safe = UIFactory.SafeRoot(root);

            // RTL layout: primary info on the top-right.
            datesText = UIFactory.Label(safe, "Dates", Strings.Format("dates", 0), 54f, UIFactory.Sand, TextAlignmentOptions.Right);
            UIFactory.Anchor(datesText.rectTransform, new Vector2(1f, 1f), new Vector2(-40f, -30f), new Vector2(600f, 80f));
            livesText = UIFactory.Label(safe, "Lives", Strings.Format("lives", 3), 54f, Color.white, TextAlignmentOptions.Right);
            UIFactory.Anchor(livesText.rectTransform, new Vector2(1f, 1f), new Vector2(-40f, -110f), new Vector2(600f, 80f));
#if UNITY_EDITOR
            // Key list for testing in the editor only; phones get the finger tutorial instead.
            if (!TouchTutorialOverlay.HasTouch())
            {
                hintText = UIFactory.Label(safe, "Hint", Strings.Get("hint_keys"), 32f, new Color(1f, 1f, 1f, 0.85f));
                UIFactory.Anchor(hintText.rectTransform, new Vector2(0.5f, 0f), new Vector2(0f, 30f), new Vector2(1800f, 60f));
            }
#endif

            BuildCameraButtons(safe);

            bannerText = UIFactory.Label(safe, "Banner", "", 60f, UIFactory.Sand);
            UIFactory.Anchor(bannerText.rectTransform, new Vector2(0.5f, 0.68f), Vector2.zero, new Vector2(1600f, 100f));
            bannerText.gameObject.SetActive(false);
            timerText = UIFactory.Label(safe, "Timer", "", 50f, AccessColours.Of(Logic.Access.ColourRole.Warning), TextAlignmentOptions.Right);
            UIFactory.Anchor(timerText.rectTransform, new Vector2(1f, 1f), new Vector2(-40f, -190f), new Vector2(600f, 70f));
            timerText.gameObject.SetActive(false);
            bishtText = UIFactory.Label(safe, "Bisht", "", 46f, AccessColours.Of(Logic.Access.ColourRole.Reward), TextAlignmentOptions.Right);
            UIFactory.Anchor(bishtText.rectTransform, new Vector2(1f, 1f), new Vector2(-40f, -270f), new Vector2(700f, 70f));
            bishtText.gameObject.SetActive(false);
            abilityText = UIFactory.Label(safe, "Ability", "", 40f, new Color(1f, 0.92f, 0.7f), TextAlignmentOptions.Right);
            UIFactory.Anchor(abilityText.rectTransform, new Vector2(1f, 0f), new Vector2(-40f, 40f), new Vector2(800f, 60f));
            abilityText.gameObject.SetActive(false);

            bossBar = UIFactory.Panel(safe, "BossBar", UIFactory.Dark, false);
            UIFactory.Anchor(bossBar, new Vector2(0.5f, 1f), new Vector2(0f, -40f), new Vector2(900f, 40f));
            bossFill = UIFactory.Panel(bossBar, "Fill", AccessColours.Of(Logic.Access.ColourRole.Danger));
            bossLabel = UIFactory.Label(bossBar, "BossName", Strings.Get("boss"), 34f, Color.white);
            UIFactory.Anchor(bossLabel.rectTransform, new Vector2(0.5f, 0f), new Vector2(0f, -50f), new Vector2(900f, 50f));
            bossBar.gameObject.SetActive(false);

            pausePanel = MakePanel(canvas, "Pause", Strings.Get("paused"),
                (Strings.Get("resume"), () => GameManager.Instance?.SetPaused(false)),
                (Strings.Get("restart"), () => GameManager.Instance?.Restart()),
                (Strings.Get("menu"), () => GameManager.Instance?.LoadMenu()));
            gameOverPanel = MakePanel(canvas, "GameOver", Strings.Get("gameover"),
                (Strings.Get("restart"), () => GameManager.Instance?.Restart()),
                (Strings.Get("menu"), () => GameManager.Instance?.LoadMenu()));
            completePanel = MakePanel(canvas, "Complete", Strings.Get("complete"),
                (Strings.Get("menu"), () => GameManager.Instance?.LoadMenu()),
                (Strings.Get("restart"), () => GameManager.Instance?.Restart()));
        }

        private GameObject recenterButton, pauseButton;

        /// <summary>
        /// Recenter (when the camera can be orbited) and, in one-hand mode, a pause button where the thumb is: a screen with one hand has no
        /// spare finger for the top corner. Both sit on the side OneHandMode picks for the player's hand.
        /// </summary>
        private void BuildCameraButtons(Transform safe)
        {
            var acc = Save.SaveSystem.Data.access;
            var rc = UIFactory.CreateButton(safe, "Recenter", Strings.Get("recenter"), Player.FollowCamera.Recenter);
            ButtonLayoutUi.Apply((RectTransform)rc.transform, Logic.Access.ControlLayout.Resolve(acc, Logic.Access.ControlLayout.Recenter));
            recenterButton = rc.gameObject; recenterButton.SetActive(false);
            var pb = UIFactory.CreateButton(safe, "PauseButton", Strings.Get("pause_button"), () => GameManager.Instance?.TogglePause());
            ButtonLayoutUi.Apply((RectTransform)pb.transform, Logic.Access.ControlLayout.Resolve(acc, Logic.Access.ControlLayout.Pause));
            pauseButton = pb.gameObject; pauseButton.SetActive(false);
        }

        private float nextButtonCheck;

        private void RefreshCameraButtons()
        {
            // These buttons change rarely; checking four times a second is plenty.
            if (Time.unscaledTime < nextButtonCheck) return;
            nextButtonCheck = Time.unscaledTime + 0.25f;
            var acc = Save.SaveSystem.Data.access;
            bool recenter = Logic.Safety.OneHandMode.RecenterButtonShown(acc) && Player.FollowCamera.CanOrbit;
            bool pause = Logic.Safety.OneHandMode.PauseButtonShown(acc);
            if (recenterButton != null && recenterButton.activeSelf != recenter) recenterButton.SetActive(recenter);
            if (pauseButton != null && pauseButton.activeSelf != pause) pauseButton.SetActive(pause);
        }

        private GameObject MakePanel(Transform canvas, string name, string title, params (string label, System.Action action)[] buttons)
        {
            var p = UIFactory.Panel(canvas, name, UIFactory.Dark);
            var g = UIFactory.VerticalGroup(p, "Group", 30f);
            UIFactory.Label(g, "Title", title, 80f, UIFactory.Sand);
            foreach (var b in buttons) UIFactory.CreateButton(g, b.label, b.label, b.action);
            p.gameObject.SetActive(false);
            return p.gameObject;
        }

        private void Update()
        {
            RefreshCameraButtons();
            if (hintText != null && hintText.gameObject.activeSelf && Time.timeSinceLevelLoad > hintSeconds) hintText.gameObject.SetActive(false);
            if (bannerText != null && bannerText.gameObject.activeSelf && Time.time > bannerUntil) bannerText.gameObject.SetActive(false);
        }

        private void OnDates(int v) => ArabicTMP.SetText(datesText, Strings.Format("dates", v));
        private void OnLives(int v) => ArabicTMP.SetText(livesText, Strings.Format("lives", v));
        private void OnPause(bool p) => pausePanel.SetActive(p);
        private void OnGameOver() => gameOverPanel.SetActive(true);
        private void OnComplete()
        {
            if (Stages.StageSession.Active) { ShowStageResult(); return; }
            var group = completePanel.transform.Find("Group");
            if (group != null && group.Find("Boxes") == null)
            {
                var line = BoxesLabel(group);
                if (line != null) line.transform.SetSiblingIndex(1);
            }
            completePanel.SetActive(true);
        }

        /// <summary>"Boxes: 37 / 42 — 5 left", or a cheer when none are left.</summary>
        private static TextMeshProUGUI BoxesLabel(Transform group)
        {
            var gm = GameManager.Instance;
            if (gm == null || gm.Boxes.Total == 0) return null;
            var t = UIFactory.Label(group, "Boxes", gm.Boxes.Line(Strings.Get("boxes_line")), 40f, Color.white);
            if (gm.Boxes.AllBroken) UIFactory.Label(group, "AllBoxes", Strings.Get("boxes_all"), 40f, new Color(1f, 0.85f, 0.3f));
            return t;
        }

        /// <summary>"Stars: 2 of 3" and one line per star earned this run (finish, every golden box, faster than par).</summary>
        private static void StarsLabel(Transform group, Stages.StageSession.StarResult r)
        {
            if (r.stars <= 0) return;
            var gold = new Color(1f, 0.85f, 0.3f);
            UIFactory.Label(group, "Stars", Strings.Format("stars_line", r.stars), 48f, gold);
            UIFactory.Label(group, "StarFinish", "★ " + Strings.Get("star_finish"), 32f, Color.white);
            if (r.hidden) UIFactory.Label(group, "StarHidden", "★ " + Strings.Get("star_hidden"), 32f, Color.white);
            if (r.beatPar) UIFactory.Label(group, "StarPar", "★ " + Strings.Format("star_par", Logic.Regions.StageProgress.FormatTime(r.par)), 32f, Color.white);
        }

        private void OnExtraLife(int n)
        {
            Banner(Strings.Format("life_up", n), 1.6f);
            if (livesPop != null) StopCoroutine(livesPop);
            livesPop = StartCoroutine(PopLives());
        }

        private System.Collections.IEnumerator PopLives()
        {
            var rt = livesText.rectTransform;
            for (float t = 0f; t < 0.6f; t += Time.unscaledDeltaTime)
            {
                float k = Mathf.Sin(t / 0.6f * Mathf.PI);
                rt.localScale = Vector3.one * (1f + 0.5f * k);
                livesText.color = Color.Lerp(Color.white, new Color(1f, 0.85f, 0.3f), k);
                yield return null;
            }
            rt.localScale = Vector3.one;
            livesText.color = Color.white;
            livesPop = null;
        }

        private void OnBisht(int layers, bool invincible)
        {
            if (bishtText == null) return;
            bool show = layers > 0 || invincible;
            bishtText.gameObject.SetActive(show);
            if (show) ArabicTMP.SetText(bishtText, invincible ? Strings.Get("bisht_hud_inv") : Strings.Format("bisht_hud", layers));
        }

        private void OnBossPhase(int phase)
        {
            var boss = Enemies.RiyadhBoss.Current;
            if (boss != null) ArabicTMP.SetText(bossLabel, boss.DisplayName);
            bossBar.gameObject.SetActive(true);
        }

        private void ShowStageResult()
        {
            var gm = GameManager.Instance;
            var stage = Stages.StageSession.Stage;
            var rec = Logic.Regions.StageProgress.Get(Save.SaveSystem.Data, stage.id);
            float time = gm != null ? gm.RunSeconds : 0f;
            int dates = gm != null ? gm.Dates : 0;
            bool best = rec != null && Mathf.Approximately(rec.bestTime, time);

            var p = UIFactory.Panel(canvasRoot, "StageComplete", UIFactory.Dark);
            var g = UIFactory.VerticalGroup(p, "Group", 24f);
            UIFactory.Label(g, "Title", Strings.Format("stage_done", stage.nameAr), 72f, UIFactory.Sand);
            UIFactory.Label(g, "Time", Strings.Format("stage_time", Logic.Regions.StageProgress.FormatTime(time),
                Logic.Regions.StageProgress.FormatTime(rec != null ? rec.bestTime : time)), 40f, Color.white);
            UIFactory.Label(g, "Dates", Strings.Format("stage_dates", dates, rec != null ? rec.bestDates : dates), 40f, Color.white);
            BoxesLabel(g);
            StarsLabel(g, gm != null ? gm.LastRunStars : default);
            var found = Stages.MuseumService.LastFound;
            if (found != null) UIFactory.Label(g, "Artifact", Strings.Format("artifact_found", Stages.MuseumService.NameOf(found)), 40f, new Color(1f, 0.85f, 0.3f));
            if (best) UIFactory.Label(g, "Best", Strings.Get("new_best"), 44f, new Color(1f, 0.85f, 0.3f));
            if (Stages.StageSession.NextUnlocked)
            {
                string next = Stages.StageSession.NextStage.id;
                UIFactory.CreateButton(g, "Next", Strings.Get("next_stage"), () => Stages.StageSession.Play(next));
            }
            UIFactory.CreateButton(g, "Map", Strings.Get("map"), () => gm?.LoadMenu());
            UIFactory.CreateButton(g, "Restart", Strings.Get("restart"), () => gm?.Restart());
        }
        private void OnDestroy() { if (Instance == this) Instance = null; }

        private void OnBossDefeated() => bossBar.gameObject.SetActive(false);

        private void OnBossHealth(float n)
        {
            bossBar.gameObject.SetActive(true);
            bossFill.anchorMax = new Vector2(Mathf.Clamp01(n), 1f);
        }
    }
}
