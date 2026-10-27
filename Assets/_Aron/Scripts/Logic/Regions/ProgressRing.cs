// (c) Ahmed Alamrani. All rights reserved.
using System.Collections.Generic;

namespace Aron.Logic.Regions
{
    /// <summary>
    /// The order countries open in. The catalog lists every country once, in a fixed order (Saudi Arabia's regions
    /// first, as one block with Riyadh leading, then the Gulf, the Arab world and the rest of the world). A player
    /// starts in their home country and goes round that list from there, wrapping back to the start, so every
    /// country is still visited exactly once.
    /// </summary>
    public static class ProgressRing
    {
        public const string DefaultHome = "saudi";

        /// <summary>The block key a region belongs to: its country, or its own id when it has none.</summary>
        public static string CountryOf(RegionDef r) => string.IsNullOrEmpty(r.country) ? r.id : r.country;

        /// <summary>Region indexes in play order for a player whose home is <paramref name="home"/>.</summary>
        public static List<int> RegionOrder(RegionCatalog catalog, string home)
        {
            var order = new List<int>(catalog.regions.Length);
            int start = -1;
            string want = string.IsNullOrEmpty(home) ? DefaultHome : home;
            for (int i = 0; i < catalog.regions.Length; i++)
                if (CountryOf(catalog.regions[i]) == want) { start = i; break; }
            if (start < 0)
                for (int i = 0; i < catalog.regions.Length; i++)
                    if (CountryOf(catalog.regions[i]) == DefaultHome) { start = i; break; }
            if (start < 0) start = 0;
            for (int k = 0; k < catalog.regions.Length; k++) order.Add((start + k) % catalog.regions.Length);
            return order;
        }

        /// <summary>Countries in play order, one entry per block.</summary>
        public static List<string> CountryOrder(RegionCatalog catalog, string home)
        {
            var list = new List<string>();
            foreach (int i in RegionOrder(catalog, home))
            {
                string c = CountryOf(catalog.regions[i]);
                if (list.Count == 0 || list[list.Count - 1] != c) list.Add(c);
            }
            return list;
        }

        /// <summary>The stage after this one in play order, or null at the very end.</summary>
        public static StageDef Next(RegionCatalog catalog, string home, string stageId)
        {
            if (catalog.Stage(stageId, out int r, out int s) == null) return null;
            var stages = catalog.regions[r].stages;
            if (s + 1 < stages.Length) return stages[s + 1];
            var order = RegionOrder(catalog, home);
            int pos = order.IndexOf(r);
            return pos + 1 < order.Count ? catalog.regions[order[pos + 1]].stages[0] : null;
        }

        /// <summary>The home country can be changed until the first stage is finished.</summary>
        public static bool CanChangeHome(SaveData data)
        {
            if (data.stages != null) foreach (var st in data.stages) if (st.done) return false;
            return true;
        }

        public static bool SetHome(SaveData data, string country)
        {
            if (!CanChangeHome(data) || string.IsNullOrEmpty(country)) return false;
            data.homeCountry = country;
            return true;
        }
    }

    /// <summary>Works out a new player's home country from the phone.</summary>
    public static class HomeCountry
    {
        /// <summary>ISO 3166 two-letter codes for each country in the game.</summary>
        public static readonly Dictionary<string, string> ByIso = new Dictionary<string, string>
        {
            {"SA", "saudi"}, {"KW", "kuwait"}, {"AE", "uae"}, {"BH", "bahrain"}, {"QA", "qatar"}, {"OM", "oman"},
            {"YE", "yemen"}, {"IQ", "iraq"}, {"SY", "syria"}, {"LB", "lebanon"}, {"JO", "jordan"},
            {"EG", "egypt"}, {"SD", "sudan"}, {"LY", "libya"}, {"TN", "tunisia"}, {"DZ", "algeria"}, {"MA", "morocco"},
            {"IN", "india"}, {"PK", "pakistan"}, {"TR", "turkey"}, {"US", "usa"}, {"GB", "uk"}, {"DE", "germany"},
            {"MX", "mexico"}, {"BR", "brazil"}, {"PT", "portugal"}, {"ID", "indonesia"}, {"MY", "malaysia"}, {"ZA", "south_africa"}, {"AU", "australia"},
        };

        /// <summary>
        /// When the phone gives no usable region, a language that belongs to one game country picks it. English,
        /// Spanish and Portuguese are spoken in several, so for those only a region tag counts ("en-GB", "pt-PT").
        /// Arabic goes to Saudi Arabia.
        /// </summary>
        public static readonly Dictionary<string, string> ByLanguage = new Dictionary<string, string>
        {
            {"ar", "saudi"}, {"hi", "india"}, {"ur", "pakistan"}, {"tr", "turkey"}, {"de", "germany"},
            {"id", "indonesia"}, {"ms", "malaysia"},
        };

        /// <param name="regionIso">Device region, e.g. "IN". May be empty.</param>
        /// <param name="cultureName">Locale name, e.g. "pt-BR" or "ar_EG". May be empty.</param>
        /// <param name="uiLanguage">Two-letter UI language picked for the player.</param>
        public static string Detect(string regionIso, string cultureName, string uiLanguage)
        {
            string iso = Norm(regionIso);
            if (iso != null && ByIso.TryGetValue(iso, out var c)) return c;
            if (!string.IsNullOrEmpty(cultureName))
            {
                var parts = cultureName.Replace('_', '-').Split('-');
                for (int i = parts.Length - 1; i > 0; i--)
                    if (parts[i].Length == 2 && ByIso.TryGetValue(parts[i].ToUpperInvariant(), out c)) return c;
                string lang = parts[0].ToLowerInvariant();
                if (lang == "in") lang = "id";
                if (ByLanguage.TryGetValue(lang, out c)) return c;
            }
            if (!string.IsNullOrEmpty(uiLanguage) && ByLanguage.TryGetValue(uiLanguage, out var byUi)) return byUi;
            return ProgressRing.DefaultHome;
        }

        private static string Norm(string iso)
        {
            if (string.IsNullOrEmpty(iso)) return null;
            iso = iso.Trim().ToUpperInvariant();
            return iso.Length == 2 ? iso : null;
        }
    }
}
