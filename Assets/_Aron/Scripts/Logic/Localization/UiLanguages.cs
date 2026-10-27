// (c) Ahmed Alamrani. All rights reserved.
using System;
using System.Collections.Generic;

namespace Aron.Logic.Localization
{
    /// <summary>The twelve languages the menus and HUD can show, and how the game picks one for a new player.</summary>
    public static class UiLanguages
    {
        public static readonly string[] Codes = { "ar", "en", "tr", "de", "ur", "hi", "es", "pt", "id", "ms", "ja", "fr" };

        /// <summary>Each language written in itself, for the language button.</summary>
        public static readonly string[] NativeNames =
        {
            "العربية", "English", "Türkçe", "Deutsch", "اردو", "हिन्दी", "Español", "Português", "Bahasa Indonesia", "Bahasa Melayu", "日本語", "Français",
        };

        public const string Fallback = "en";

        public static bool IsKnown(string code) => Array.IndexOf(Codes, code) >= 0;

        /// <summary>Arabic and Urdu read right to left and go through the Arabic shaper.</summary>
        public static bool IsRtl(string code) => code == "ar" || code == "ur";

        public static string NativeName(string code)
        {
            int i = Array.IndexOf(Codes, code);
            return i < 0 ? code : NativeNames[i];
        }

        /// <summary>The language after this one, for a button that cycles through them.</summary>
        public static string Next(string code)
        {
            int i = Array.IndexOf(Codes, code);
            return Codes[(i + 1) % Codes.Length];
        }

        /// <summary>
        /// Picks a language from the device. <paramref name="systemLanguage"/> is Unity's SystemLanguage name
        /// ("Arabic", "Turkish"...). Unity has no entry for Urdu, Hindi or Malay, so those come from the
        /// culture name ("ur-PK", "hi-IN", "ms-MY"), which is checked first. Anything else gets English.
        /// </summary>
        public static string Detect(string systemLanguage, string cultureName)
        {
            if (!string.IsNullOrEmpty(cultureName))
            {
                string c = cultureName.Trim().Replace('_', '-').ToLowerInvariant();
                int dash = c.IndexOf('-');
                string two = dash > 0 ? c.Substring(0, dash) : c;
                if (two == "in") two = "id"; // old Java/Android code for Indonesian
                if (two == "zsm") two = "ms";
                if (IsKnown(two)) return two;
            }
            if (!string.IsNullOrEmpty(systemLanguage) && BySystemLanguage.TryGetValue(systemLanguage, out var code)) return code;
            return Fallback;
        }

        private static readonly Dictionary<string, string> BySystemLanguage = new Dictionary<string, string>
        {
            {"Arabic", "ar"}, {"English", "en"}, {"Turkish", "tr"}, {"German", "de"}, {"Spanish", "es"},
            {"Portuguese", "pt"}, {"Indonesian", "id"}, {"Hindi", "hi"}, {"Urdu", "ur"}, {"Malay", "ms"},
            {"Japanese", "ja"}, {"French", "fr"},
        };

        /// <summary>Looks a key up in the language, then English, then gives the key back.</summary>
        public static string Get(string language, string key)
        {
            if (UiStringTables.All.TryGetValue(language ?? "", out var t) && t.TryGetValue(key, out var v) && !string.IsNullOrEmpty(v)) return v;
            if (UiStringTables.All[Fallback].TryGetValue(key, out v)) return v;
            return key;
        }
    }
}
