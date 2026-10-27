// (c) Ahmed Alamrani. All rights reserved.
using System;
using System.Collections;
using System.Collections.Generic;
using Aron.Localization;
using Aron.Logic.Content;
using Aron.UI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Aron.Content
{
    /// <summary>
    /// The screen between "play" and the stage when a country's music is not on the phone yet. It says what is being downloaded and how big it is,
    /// shows the progress, and asks before using mobile data (Wi-Fi only is the default). It is only ever opened from the menu or the map
    /// (never in a stage), and the player can always skip it: the stage then plays with the base music and nothing is lost.
    /// </summary>
    public sealed class ContentProgressScreen : MonoBehaviour
    {
        private readonly DownloadSession session = new DownloadSession();
        private readonly ContentService.Consent consent = new ContentService.Consent();
        private string[] groups;
        private Action proceed, cancel;
        private TextMeshProUGUI status, size;
        private Image fill;
        private Button useCellular, wait, skip, retry;
        private bool built, closed;

        /// <summary>Phase the screen is in, for tests.</summary>
        public DownloadPhase Phase => session.Phase;
        public float Progress => session.Progress;
        public static ContentProgressScreen Current { get; private set; }

        /// <param name="proceed">Called when the stage may start: the music arrived, or the player chose to play without it.</param>
        /// <param name="cancel">Called when the player chose to wait for Wi-Fi and go back.</param>
        public static ContentProgressScreen Open(Transform parent, string[] groups, Action proceed, Action cancel)
        {
            var go = new GameObject("ContentProgress");
            if (parent != null) go.transform.SetParent(parent, false);
            var s = go.AddComponent<ContentProgressScreen>();
            s.groups = groups; s.proceed = proceed; s.cancel = cancel;
            Current = s;
            s.StartCoroutine(s.Run());
            return s;
        }

        private void OnDestroy() { if (Current == this) Current = null; }

        private IEnumerator Run()
        {
            // 1. how much is there to fetch (groups already on the phone only need loading)
            long total = 0; var toFetch = new List<string>(); var toLoad = new List<string>();
            var backend = ContentService.Backend;
            foreach (var g in groups)
            {
                long bytes = -1;
                if (backend != null && backend.Available) yield return StartCoroutine(backend.SizeOf(g, b => bytes = b));
                if (bytes > 0) { toFetch.Add(g); total += bytes; }
                else if (bytes == 0) toLoad.Add(g);
                else toFetch.Add(g);   // unknown size (offline first start): the policy decides below
            }

            foreach (var g in toLoad)
            {
                string e = null; bool fin = false;
                yield return StartCoroutine(ContentService.LoadGroup(g, err => { e = err; fin = true; }));
                while (!fin) yield return null;
            }
            if (toFetch.Count == 0) { Finish(true); yield break; }

            // 2. may we download now?
            while (true)
            {
                var verdict = DownloadPolicy.Decide(true, ContentService.Network(), ContentService.CellularOptIn(), consent.Cellular, ContentService.InGameplay(), true);
                session.Start(verdict, total);
                Build();
                size.text = Strings.Format("content_size", DownloadPolicy.FormatBytes(total));
                RefreshUi();

                while (session.Phase == DownloadPhase.AskingCellular)
                {
                    if (ContentService.Network() == NetworkKind.Wifi) break;      // Wi-Fi came back while the question was open
                    yield return null;
                }
                if (session.Phase == DownloadPhase.Skipped) { Finish(true); yield break; }
                if (session.Phase == DownloadPhase.Failed)
                {
                    yield return WaitForChoice();
                    if (closed) yield break;
                    if (session.Phase == DownloadPhase.Skipped) { Finish(true); yield break; }
                    continue;
                }
                // Wi-Fi came back while the question was open: no consent needed, so just start (ConfirmCellular is the "start" transition).
                if (session.Phase == DownloadPhase.AskingCellular && ContentService.Network() == NetworkKind.Wifi) session.ConfirmCellular();

                // 3. download group by group
                bool allOk = true; long doneBefore = 0; long doneNow = 0;
                foreach (var g in toFetch)
                {
                    string err = null; bool fin = false;
                    long groupBytes = 0; if (backend != null) yield return StartCoroutine(backend.SizeOf(g, b => groupBytes = Math.Max(0, b)));
                    StartCoroutine(ContentService.Download(g, true, consent, p => { doneNow = doneBefore + (long)(p * groupBytes); session.Report(doneNow); RefreshUi(); }, e => { err = e; fin = true; }));
                    while (!fin)
                    {
                        if (session.Phase == DownloadPhase.Skipped) { ContentService.Backend?.Cancel(); Finish(true); yield break; }
                        yield return null;
                    }
                    doneBefore += groupBytes;
                    if (err != null) { allOk = false; session.Fail(err); break; }
                }
                if (allOk) { session.Complete(); RefreshUi(); Finish(true); yield break; }
                RefreshUi();
                yield return WaitForChoice();
                if (closed) yield break;
                if (session.Phase == DownloadPhase.Skipped) { Finish(true); yield break; }
            }
        }

        // After a failure: stay until the player retries, skips or goes back.
        private IEnumerator WaitForChoice()
        {
            retryRequested = false;
            while (!closed && !retryRequested && session.Phase != DownloadPhase.Skipped) yield return null;
        }
        private bool retryRequested;

        private void Finish(bool play)
        {
            if (closed) return;
            closed = true;
            var p = proceed;
            Destroy(gameObject);
            if (play) p?.Invoke();
        }

        private void GoBack()
        {
            if (closed) return;
            closed = true;
            ContentService.Backend?.Cancel();
            var c = cancel;
            Destroy(gameObject);
            c?.Invoke();
        }

        private void Build()
        {
            if (built) return;
            built = true;
            var canvas = UIFactory.CreateCanvas("ContentCanvas", 40);
            canvas.transform.SetParent(transform, false);
            UIFactory.Panel(canvas.transform, "Bg", new Color(0.08f, 0.05f, 0.03f, 0.97f));
            var safe = UIFactory.SafeRoot(canvas);
            var g = UIFactory.VerticalGroup(safe, "Group", 24f);
            UIFactory.Anchor(g, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(1100f, 900f));
            UIFactory.Label(g, "Title", Strings.Get("content_title"), 64f, UIFactory.Sand);
            size = UIFactory.Label(g, "Size", "", 40f, Color.white);
            // progress bar
            var bar = UIFactory.Panel(g, "Bar", new Color(1f, 1f, 1f, 0.15f), false);
            var le = bar.gameObject.AddComponent<LayoutElement>(); le.preferredHeight = 36f; le.minHeight = 36f;
            var fillGo = new GameObject("Fill", typeof(RectTransform), typeof(Image));
            fillGo.transform.SetParent(bar, false);
            fill = fillGo.GetComponent<Image>(); fill.color = UIFactory.Green;
            var rt = (RectTransform)fillGo.transform; rt.anchorMin = Vector2.zero; rt.anchorMax = new Vector2(0f, 1f); rt.offsetMin = rt.offsetMax = Vector2.zero; rt.pivot = new Vector2(0f, 0.5f);
            status = UIFactory.Label(g, "Status", "", 40f, Color.white);
            useCellular = UIFactory.CreateButton(g, "UseCellular", Strings.Get("content_use_cellular"), () => { consent.Cellular = true; session.ConfirmCellular(); RefreshUi(); });
            wait = UIFactory.CreateButton(g, "Wait", Strings.Get("content_wait_wifi"), GoBack);
            retry = UIFactory.CreateButton(g, "Retry", Strings.Get("content_retry"), () => { retryRequested = true; });
            skip = UIFactory.CreateButton(g, "Skip", Strings.Get("content_skip"), () => { session.Skip(); RefreshUi(); });
        }

        private void RefreshUi()
        {
            if (!built) return;
            switch (session.Phase)
            {
                case DownloadPhase.AskingCellular: status.text = Strings.Get("content_ask_cellular"); break;
                case DownloadPhase.Downloading: status.text = Strings.Format("content_progress", Mathf.RoundToInt(session.Progress * 100f)); break;
                case DownloadPhase.Failed: status.text = Strings.Get("content_offline"); break;
                default: status.text = ""; break;
            }
            var r = (RectTransform)fill.transform;
            r.anchorMax = new Vector2(Mathf.Clamp01(session.Progress), 1f);
            useCellular.gameObject.SetActive(session.Phase == DownloadPhase.AskingCellular);
            wait.gameObject.SetActive(session.Phase == DownloadPhase.AskingCellular || session.Phase == DownloadPhase.Downloading);
            retry.gameObject.SetActive(session.Phase == DownloadPhase.Failed);
            skip.gameObject.SetActive(!session.Finished || session.Phase == DownloadPhase.Failed);
        }
    }
}
