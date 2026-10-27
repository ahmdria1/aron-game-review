// (c) Ahmed Alamrani. All rights reserved.
using Aron.Localization;
using UnityEngine;

namespace Aron.UI
{
    /// <summary>One short message with a Back button, shown when a feature has been paused from the server settings.</summary>
    public static class FeatureOffNotice
    {
        private static GameObject open;

        public static void Show()
        {
            if (open != null) return;
            var canvas = UIFactory.CreateCanvas("FeatureOff", 60);
            open = canvas.gameObject;
            UIFactory.Panel(canvas.transform, "Dim", UIFactory.Dark);
            var safe = UIFactory.SafeRoot(canvas);
            var text = UIFactory.Label(safe, "Text", Strings.Get("feature_off"), 52f, UIFactory.Sand);
            UIFactory.Anchor(text.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0f, 120f), new Vector2(1400f, 160f));
            text.rectTransform.pivot = new Vector2(0.5f, 0.5f);
            var b = UIFactory.CreateButton(safe, "Back", Strings.Get("back"), () => { Object.Destroy(open); open = null; });
            UIFactory.Anchor((RectTransform)b.transform, new Vector2(0.5f, 0.5f), new Vector2(0f, -60f), new Vector2(520f, 110f));
            ((RectTransform)b.transform).pivot = new Vector2(0.5f, 0.5f);
        }
    }
}
