// (c) Ahmed Alamrani. All rights reserved.
using Aron.Core;
using Aron.Localization;
using Aron.Logic.Culture;
using Aron.Save;
using Aron.Stages;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Aron.UI
{
    /// <summary>
    /// Aron's museum: one shelf card per country, in the player's ring order. Found treasures show their colours, name and a short line;
    /// the rest are grey shadows with the hint on how to find them. Sources and review flags: docs/culture_sources.md.
    /// </summary>
    public class MuseumScreen : MonoBehaviour
    {
        public static MuseumScreen Open(Transform parent)
        {
            var go = new GameObject("Museum");
            go.transform.SetParent(parent, false);
            var m = go.AddComponent<MuseumScreen>();
            m.Build();
            return m;
        }

        private void Build()
        {
            var data = SaveSystem.Data;
            var file = MuseumService.Artifacts;
            var canvas = UIFactory.CreateCanvas("MuseumCanvas", 20);
            canvas.transform.SetParent(transform, false);
            UIFactory.Panel(canvas.transform, "Bg", new Color(0.08f, 0.05f, 0.03f, 0.96f));
            var safe = UIFactory.SafeRoot(canvas);

            var title = UIFactory.Label(safe, "Title", Strings.Get("museum_title"), 52f, UIFactory.Sand, TextAlignmentOptions.Right);
            UIFactory.Anchor(title.rectTransform, new Vector2(1f, 1f), new Vector2(-40f, -20f), new Vector2(1300f, 90f));
            int n = ArtifactBook.Count(data, file, out int total);
            var count = UIFactory.Label(safe, "Count", Strings.Format("museum_count", n, total) + (ArtifactBook.Complete(data, file) ? "  " + Strings.Get("museum_all") : ""), 38f, Color.white, TextAlignmentOptions.Right);
            UIFactory.Anchor(count.rectTransform, new Vector2(1f, 1f), new Vector2(-40f, -105f), new Vector2(1300f, 60f));
            var back = UIFactory.CreateButton(safe, "Back", Strings.Get("back"), () => Destroy(gameObject));
            UIFactory.Anchor((RectTransform)back.transform, new Vector2(0f, 1f), new Vector2(30f, -20f), new Vector2(300f, 100f));

            var view = new GameObject("View", typeof(RectTransform), typeof(RectMask2D), typeof(Image), typeof(ScrollRect));
            view.transform.SetParent(safe, false);
            var vrt = (RectTransform)view.transform;
            vrt.anchorMin = Vector2.zero; vrt.anchorMax = Vector2.one;
            vrt.offsetMin = new Vector2(30f, 100f); vrt.offsetMax = new Vector2(-30f, -180f);
            view.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.01f);
            var content = new GameObject("Content", typeof(RectTransform), typeof(GridLayoutGroup), typeof(ContentSizeFitter));
            content.transform.SetParent(view.transform, false);
            var crt = (RectTransform)content.transform;
            crt.anchorMin = new Vector2(0f, 1f); crt.anchorMax = new Vector2(1f, 1f); crt.pivot = new Vector2(0.5f, 1f);
            crt.offsetMin = crt.offsetMax = Vector2.zero;
            var grid = content.GetComponent<GridLayoutGroup>();
            grid.cellSize = new Vector2(560f, 300f); grid.spacing = new Vector2(24f, 24f);
            grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount; grid.constraintCount = 3;
            grid.startCorner = GridLayoutGroup.Corner.UpperRight;   // reads right to left like the rest of the game
            grid.childAlignment = TextAnchor.UpperCenter;
            content.GetComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            var scroll = view.GetComponent<ScrollRect>();
            scroll.content = crt; scroll.horizontal = false; scroll.vertical = true; scroll.viewport = vrt;
            scroll.movementType = ScrollRect.MovementType.Elastic;

            foreach (var e in ArtifactBook.Entries(StageSession.Catalog, data, file)) Card(content.transform, e);

            var hint = UIFactory.Label(safe, "Hint", Strings.Get("museum_hint"), 32f, new Color(1f, 1f, 1f, 0.8f));
            UIFactory.Anchor(hint.rectTransform, new Vector2(0.5f, 0f), new Vector2(0f, 20f), new Vector2(1800f, 60f));
            hint.rectTransform.pivot = new Vector2(0.5f, 0f);
        }

        private static void Card(Transform parent, MuseumEntry e)
        {
            var a = e.artifact;
            Color c1 = e.found ? StageSession.Hex(a.color, UIFactory.Sand) : new Color(0.3f, 0.3f, 0.3f);
            var card = UIFactory.Panel(parent, a.id, new Color(0.16f, 0.1f, 0.06f, 0.92f), false);
            // A simple shape stands in for the artifact until its model is made (the shape id is in the data).
            var swatch = UIFactory.Panel(card, "Swatch", c1, false);
            UIFactory.Anchor(swatch, new Vector2(0.5f, 1f), new Vector2(0f, -16f), new Vector2(120f, 120f));
            swatch.pivot = new Vector2(0.5f, 1f);
            var country = UIFactory.Label(card, "Country", e.localCountry, 34f, UIFactory.Sand);
            UIFactory.Anchor(country.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -144f), new Vector2(540f, 50f));
            country.rectTransform.pivot = new Vector2(0.5f, 1f);
            string line = e.found ? MuseumService.NameOf(a) : "؟ " + Strings.Get("museum_locked");
            var name = UIFactory.Label(card, "Name", line, 34f, e.found ? Color.white : new Color(1f, 1f, 1f, 0.5f));
            UIFactory.Anchor(name.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -192f), new Vector2(540f, 50f));
            name.rectTransform.pivot = new Vector2(0.5f, 1f);
            if (e.found)
            {
                var fact = UIFactory.Label(card, "Fact", MuseumService.FactOf(a), 22f, new Color(1f, 1f, 1f, 0.75f));
                UIFactory.Anchor(fact.rectTransform, new Vector2(0.5f, 0f), new Vector2(0f, 8f), new Vector2(540f, 56f));
                fact.rectTransform.pivot = new Vector2(0.5f, 0f);
                fact.textWrappingMode = TextWrappingModes.Normal;
            }
        }
    }
}
