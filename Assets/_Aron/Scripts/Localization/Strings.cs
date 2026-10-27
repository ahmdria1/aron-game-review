// (c) Ahmed Alamrani. All rights reserved.
using Aron.Logic.Localization;

namespace Aron.Localization
{
    /// <summary>
    /// UI strings in the player's language. The tables live in the logic assembly (UiStringTables) so tests can
    /// check every language has every key.
    /// </summary>
    public static class Strings
    {
        /// <summary>Two-letter code from <see cref="UiLanguages.Codes"/>.</summary>
        public static string Language = "ar";

        public static bool IsRtl => UiLanguages.IsRtl(Language);

        public static string Get(string key) => UiLanguages.Get(Language, key);

        public static string Format(string key, params object[] args) => string.Format(Get(key), args);

        /// <summary>The saved choice, or the device language when the player never picked one.</summary>
        public static string Resolve(string saved)
        {
            if (UiLanguages.IsKnown(saved)) return saved;
            return UiLanguages.Detect(UnityEngine.Application.systemLanguage.ToString(),
                System.Globalization.CultureInfo.CurrentUICulture?.Name);
        }
    }
}
