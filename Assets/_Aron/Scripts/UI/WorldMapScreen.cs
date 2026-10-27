// (c) Ahmed Alamrani. All rights reserved.
using Aron.Audio;
using Aron.Core;
using Aron.Localization;
using Aron.Logic.Regions;
using Aron.Save;
using Aron.Stages;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Aron.UI
{
    /// <summary>
    /// Region map: one card per region, stages laid out right to left, the boss as a bigger red node at the end.
    /// Locked stages are dimmed and can't be pressed. Tapping an open stage shows its details and a play button.
    /// </summary>
    public class WorldMapScreen : MonoBehaviour
    {
        private static readonly Color CardColor = new Color(0.16f, 0.1f, 0.06f, 0.9f);
        private static readonly Color DoneColor = new Color(0.85f, 0.65f, 0.2f);
        private static readonly Color BossColor = new Color(0.72f, 0.12f, 0.12f);
        private static readonly Color LockedTint = new Color(1f, 1f, 1f, 0.3f);

        private RectTransform detail;
        private TextMeshProUGUI detailTitle, detailInfo;
        private Button detailPlay;
        private string pickedStage, musicBefore;

        public static WorldMapScreen Open(Transform parent)
        {
            var go = new GameObject("WorldMap");
            go.transform.SetParent(parent, false);
            var map = go.AddComponent<WorldMapScreen>();
            map.musicBefore = AudioManager.CurrentTrack;
            AudioManager.PlayMusic(Aron.Logic.MusicCues.Map);
            map.Build();
            Aron.Content.ContentService.PrefetchFrontier();   // quietly, on Wi-Fi only (or with the saved permission), and never from a stage
            return map;
        }

        // Back to what was playing when the map closes without a stage starting (a stage start brings its own music).
        private void OnDestroy()
        {
            if (!string.IsNullOrEmpty(musicBefore) && AudioManager.CurrentTrack == Aron.Logic.MusicCues.Map)
                AudioManager.PlayMusic(musicBefore);
        }

        private void Build()
        {
            var catalog = StageSession.Catalog;
            var data = SaveSystem.Data;
            var canvas = UIFactory.CreateCanvas("MapCanvas", 20);
            canvas.transform.SetParent(transform, false);
            UIFactory.Panel(canvas.transform, "Bg", new Color(0.08f, 0.05f, 0.03f, 0.96f));
            var safe = UIFactory.SafeRoot(canvas);

            var title = UIFactory.Label(safe, "Title", Strings.Get("map_title"), 60f, UIFactory.Sand, TextAlignmentOptions.Right);
            UIFactory.Anchor(title.rectTransform, new Vector2(1f, 1f), new Vector2(-40f, -20f), new Vector2(1200f, 90f));
            var back = UIFactory.CreateButton(safe, "Back", Strings.Get("back"), Close);
            UIFactory.Anchor((RectTransform)back.transform, new Vector2(0f, 1f), new Vector2(30f, -20f), new Vector2(300f, 100f));

            // Vertical list of region cards, dragged with a finger.
            var view = new GameObject("View", typeof(RectTransform), typeof(RectMask2D), typeof(Image), typeof(ScrollRect));
            view.transform.SetParent(safe, false);
            var vrt = (RectTransform)view.transform;
            vrt.anchorMin = new Vector2(0f, 0f); vrt.anchorMax = new Vector2(1f, 1f);
            vrt.offsetMin = new Vector2(30f, 30f); vrt.offsetMax = new Vector2(-30f, -130f);
            view.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.01f);
            var content = new GameObject("Content", typeof(RectTransform), typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));
            content.transform.SetParent(view.transform, false);
            var crt = (RectTransform)content.transform;
            crt.anchorMin = new Vector2(0f, 1f); crt.anchorMax = new Vector2(1f, 1f); crt.pivot = new Vector2(0.5f, 1f);
            crt.offsetMin = crt.offsetMax = Vector2.zero;
            var vl = content.GetComponent<VerticalLayoutGroup>();
            vl.spacing = 24f; vl.childControlHeight = true; vl.childControlWidth = true; vl.childForceExpandHeight = false;
            vl.padding = new RectOffset(0, 0, 0, 340);
            content.GetComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            var scroll = view.GetComponent<ScrollRect>();
            scroll.content = crt; scroll.horizontal = false; scroll.vertical = true; scroll.viewport = vrt;
            scroll.movementType = ScrollRect.MovementType.Elastic;

            RectTransform focus = null;
            var frontier = StageProgress.Frontier(catalog, data);
            for (int r = 0; r < catalog.regions.Length; r++)
            {
                var card = RegionCard(content.transform, catalog, data, r, frontier);
                if (frontier != null && catalog.Stage(frontier.id, out int fr, out _) != null && fr == r) focus = card;
            }

            BuildDetail(safe);
            if (focus != null) StartCoroutine(ScrollTo(scroll, crt, focus));
        }

        private System.Collections.IEnumerator ScrollTo(ScrollRect scroll, RectTransform content, RectTransform card)
        {
            yield return null;
            Canvas.ForceUpdateCanvases();
            float h = content.rect.height - ((RectTransform)scroll.transform).rect.height;
            if (h > 0f) scroll.verticalNormalizedPosition = 1f - Mathf.Clamp01(-card.anchoredPosition.y / h);
        }

        private RectTransform RegionCard(Transform parent, RegionCatalog catalog, Aron.Logic.SaveData data, int r, StageDef frontier)
        {
            var region = catalog.regions[r];
            bool open = StageProgress.IsRegionUnlocked(catalog, data, r);
            var card = UIFactory.Panel(parent, "Region_" + region.id, CardColor, false);
            card.gameObject.AddComponent<LayoutElement>().preferredHeight = 330f;
            var accent = StageSession.Hex(region.accent, UIFactory.Green);

            var stripe = UIFactory.Panel(card, "Stripe", accent, false);
            stripe.anchorMin = new Vector2(1f, 0f); stripe.anchorMax = new Vector2(1f, 1f); stripe.pivot = new Vector2(1f, 0.5f);
            stripe.sizeDelta = new Vector2(16f, 0f); stripe.anchoredPosition = Vector2.zero;

            int done = 0;
            foreach (var s in region.stages) if (StageProgress.IsDone(data, s.id)) done++;
            var name = UIFactory.Label(card, "Name", region.nameAr, 54f, open ? UIFactory.Sand : new Color(0.6f, 0.55f, 0.5f), TextAlignmentOptions.Right);
            UIFactory.Anchor(name.rectTransform, new Vector2(1f, 1f), new Vector2(-40f, -16f), new Vector2(1000f, 80f));
            var count = UIFactory.Label(card, "Count", Strings.Format("stages_done", done, region.stages.Length), 36f, Color.white, TextAlignmentOptions.Left);
            UIFactory.Anchor(count.rectTransform, new Vector2(0f, 1f), new Vector2(30f, -24f), new Vector2(400f, 60f));

            // Nodes run right to left, like the text.
            int n = region.stages.Length;
            float step = 190f;
            for (int i = 0; i < n; i++)
            {
                var st = region.stages[i];
                bool unlocked = StageProgress.IsUnlocked(catalog, data, st.id);
                bool isDone = StageProgress.IsDone(data, st.id);
                float size = st.boss ? 150f : 116f;
                var node = new GameObject("Stage_" + st.id, typeof(RectTransform), typeof(Image), typeof(Button));
                node.transform.SetParent(card, false);
                var nrt = (RectTransform)node.transform;
                UIFactory.Anchor(nrt, new Vector2(1f, 0.5f), new Vector2(-60f - i * step, -40f), new Vector2(size, size));
                nrt.pivot = new Vector2(0.5f, 0.5f);
                nrt.anchoredPosition = new Vector2(-60f - size * 0.5f - i * step, -40f);
                if (st.boss) nrt.localRotation = Quaternion.Euler(0f, 0f, 45f);
                var img = node.GetComponent<Image>();
                Color c = st.boss ? BossColor : isDone ? DoneColor : UIFactory.Green;
                img.color = unlocked ? c : c * LockedTint;
                var btn = node.GetComponent<Button>();
                btn.interactable = unlocked;
                string id = st.id;
                btn.onClick.AddListener(() => { GameEvents.PlaySfx("click"); Pick(id); });

                var lbl = UIFactory.Label(node.transform, "Num", st.boss ? Strings.Get("boss_node") : (i + 1).ToString(), st.boss ? 40f : 52f,
                    unlocked ? Color.white : new Color(1f, 1f, 1f, 0.4f));
                var lrt = lbl.rectTransform;
                lrt.anchorMin = Vector2.zero; lrt.anchorMax = Vector2.one; lrt.offsetMin = lrt.offsetMax = Vector2.zero;
                if (st.boss) lrt.localRotation = Quaternion.Euler(0f, 0f, -45f);
                if (frontier != null && frontier.id == st.id)
                {
                    var ring = UIFactory.Panel(node.transform, "Here", new Color(1f, 1f, 1f, 0.9f), false);
                    UIFactory.Anchor(ring, new Vector2(0.5f, 0f), new Vector2(0f, -26f), new Vector2(size * 0.6f, 10f));
                }

                if (i < n - 1)
                {
                    var link = UIFactory.Panel(card, "Link", unlocked ? new Color(1f, 1f, 1f, 0.35f) : new Color(1f, 1f, 1f, 0.1f), false);
                    UIFactory.Anchor(link, new Vector2(1f, 0.5f), new Vector2(-60f - size - i * step, -40f), new Vector2(step - size, 8f));
                    link.pivot = new Vector2(1f, 0.5f);
                }
            }

            if (!open && r > 0)
            {
                var lockLbl = UIFactory.Label(card, "Locked", Strings.Format("region_locked", catalog.regions[r - 1].nameAr), 32f,
                    new Color(1f, 0.85f, 0.6f), TextAlignmentOptions.Right);
                UIFactory.Anchor(lockLbl.rectTransform, new Vector2(1f, 0f), new Vector2(-40f, 10f), new Vector2(1200f, 50f));
            }
            else if (region.id == "riyadh" && Application.CanStreamedLevelBeLoaded(SceneNames.Riyadh))
            {
                var full = UIFactory.CreateButton(card, "FullRiyadh", Strings.Get("full_riyadh"), () =>
                {
                    StageSession.Clear();
                    SceneLoader.Load(SceneNames.Riyadh);
                });
                UIFactory.Anchor((RectTransform)full.transform, new Vector2(0f, 1f), new Vector2(30f, -90f), new Vector2(360f, 90f));
            }
            return card;
        }

        private void BuildDetail(RectTransform safe)
        {
            detail = UIFactory.Panel(safe, "Detail", new Color(0.12f, 0.08f, 0.05f, 0.97f), false);
            detail.anchorMin = new Vector2(0f, 0f); detail.anchorMax = new Vector2(1f, 0f); detail.pivot = new Vector2(0.5f, 0f);
            detail.sizeDelta = new Vector2(0f, 300f); detail.anchoredPosition = Vector2.zero;
            detailTitle = UIFactory.Label(detail, "Title", "", 52f, UIFactory.Sand, TextAlignmentOptions.Right);
            UIFactory.Anchor(detailTitle.rectTransform, new Vector2(1f, 1f), new Vector2(-40f, -20f), new Vector2(1300f, 80f));
            detailInfo = UIFactory.Label(detail, "Info", "", 34f, Color.white, TextAlignmentOptions.TopRight);
            UIFactory.Anchor(detailInfo.rectTransform, new Vector2(1f, 1f), new Vector2(-40f, -105f), new Vector2(1300f, 180f));
            detailInfo.textWrappingMode = TextWrappingModes.Normal;
            detailPlay = UIFactory.CreateButton(detail, "Play", Strings.Get("play_stage"), () => { if (pickedStage != null) StageSession.Play(pickedStage); });
            UIFactory.Anchor((RectTransform)detailPlay.transform, new Vector2(0f, 0.5f), new Vector2(40f, 0f), new Vector2(420f, 130f));
            detail.gameObject.SetActive(false);
        }

        private void Pick(string stageId)
        {
            var catalog = StageSession.Catalog;
            var st = catalog.Stage(stageId, out int r, out _);
            if (st == null) return;
            pickedStage = stageId;
            var rec = StageProgress.Get(SaveSystem.Data, stageId);
            var lines = new System.Text.StringBuilder();
            if (!string.IsNullOrEmpty(st.lineAr)) lines.AppendLine(st.lineAr);
            lines.Append(Strings.Format("difficulty", st.difficulty));
            if (st.mount == "camel") lines.Append(" • ").Append(Strings.Get("mount_camel"));
            if (st.mount == "horse") lines.Append(" • ").Append(Strings.Get("mount_horse"));
            if (st.boss) lines.Append(" • ").Append(Strings.Format("boss_stage", catalog.regions[r].boss.nameAr));
            if (rec != null && rec.done)
                lines.AppendLine().Append(Strings.Format("best_record", StageProgress.FormatTime(rec.bestTime), rec.bestDates));
            ArabicTMP.SetText(detailTitle, catalog.regions[r].nameAr + " - " + st.nameAr);
            ArabicTMP.SetText(detailInfo, lines.ToString());
            detail.gameObject.SetActive(true);
        }

        private void Close() => Destroy(gameObject);
    }
}
