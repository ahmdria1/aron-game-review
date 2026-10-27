// (c) Ahmed Alamrani. All rights reserved.
using Aron.Logic;
using TMPro;
using UnityEngine;

namespace Aron.Localization
{
    /// <summary>Helpers to put Arabic into any TMP_Text correctly, and to provide the Arabic font asset.</summary>
    public static class ArabicTMP
    {
        public const string FontResourcePath = "Fonts/NotoNaskhArabic";
        /// <summary>Fallback fonts: Devanagari for Hindi, Japanese (JIS level 1 kanji, kana and every kanji the game's text uses).</summary>
        public static readonly string[] FallbackPaths = { "Fonts/NotoSansDevanagari", "Fonts/NotoSansJP" };
        private static TMP_FontAsset font;

        /// <summary>Dynamic TMP font asset built at runtime from the bundled OFL TTF (no baking needed).</summary>
        public static TMP_FontAsset Font
        {
            get
            {
                if (font != null) return font;
                var ttf = Resources.Load<UnityEngine.Font>(FontResourcePath);
                if (ttf != null)
                {
                    font = TMP_FontAsset.CreateFontAsset(ttf);
                    if (font != null)
                    {
                        font.name = "NotoNaskhArabic (Dynamic)";
                        if (font.fallbackFontAssetTable == null) font.fallbackFontAssetTable = new System.Collections.Generic.List<TMP_FontAsset>();
                        // Latin first (default asset), then Hindi and Japanese, so every UI language has glyphs.
                        var def = TMP_Settings.defaultFontAsset;
                        if (def != null) font.fallbackFontAssetTable.Add(def);
                        foreach (var path in FallbackPaths)
                        {
                            var f = Resources.Load<UnityEngine.Font>(path);
                            var fa = f != null ? TMP_FontAsset.CreateFontAsset(f) : null;
                            if (fa != null) { fa.name = path + " (Dynamic)"; font.fallbackFontAssetTable.Add(fa); }
                        }
                    }
                }
                if (font == null) font = TMP_Settings.defaultFontAsset;
                return font;
            }
        }

        public static void SetText(TMP_Text t, string text)
        {
            if (t == null) return;
            if (Font != null && t.font != Font) t.font = Font;
            bool ar = ArabicText.ContainsArabic(text);
            t.isRightToLeftText = ar;
            t.text = ar ? ArabicText.Fix(text) : text;
        }
    }
}
