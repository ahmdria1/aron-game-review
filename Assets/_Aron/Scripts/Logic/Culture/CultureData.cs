// (c) Ahmed Alamrani. All rights reserved.
using System;
using System.Collections.Generic;
using Aron.Logic.Localization;

namespace Aron.Logic.Culture
{
    /// <summary>A line in every UI language.</summary>
    [Serializable]
    public class LocalText
    {
        public string ar = "", en = "", tr = "", de = "", ur = "", hi = "", es = "", pt = "", id = "", ms = "", ja = "", fr = "";

        public string Get(string lang)
        {
            string v = lang switch
            {
                "ar" => ar, "en" => en, "tr" => tr, "de" => de, "ur" => ur, "hi" => hi,
                "es" => es, "pt" => pt, "id" => id, "ms" => ms, "ja" => ja, "fr" => fr, _ => "",
            };
            return string.IsNullOrEmpty(v) ? en : v;
        }

        public IEnumerable<string> Missing()
        {
            foreach (var l in UiLanguages.Codes)
            {
                string v = l switch
                {
                    "ar" => ar, "en" => en, "tr" => tr, "de" => de, "ur" => ur, "hi" => hi,
                    "es" => es, "pt" => pt, "id" => id, "ms" => ms, "ja" => ja, "fr" => fr, _ => "",
                };
                if (string.IsNullOrWhiteSpace(v)) yield return l;
            }
        }

        public IEnumerable<string> All() { yield return ar; yield return en; yield return tr; yield return de; yield return ur; yield return hi; yield return es; yield return pt; yield return id; yield return ms; yield return ja; yield return fr; }
    }

    [Serializable]
    public class Food
    {
        public string id;
        /// <summary>The dish's name in the country's own language.</summary>
        public string local;
        public LocalText name = new LocalText();
        /// <summary>One short, respectful line about it.</summary>
        public LocalText fact = new LocalText();
        /// <summary>Pickup shape: bowl, plate, bun, cup, roll, cake, skewer, wrap, ball, stick.</summary>
        public string shape = "plate";
        public string color = "#C8A060", color2 = "#7A4A22";
        /// <summary>What eating it from the stall gives for a while: strength, speed, energy or magnet.</summary>
        public string buff = "energy";
        /// <summary>Stall price in coins.</summary>
        public int price = 60;
    }

    [Serializable]
    public class Greeting
    {
        public string words;
        public string wordsEn;
        public string wordsAr;
        /// <summary>handshake, hand_on_chest, nose_kiss, namaste, bow_nod, cheek_kiss, wave, salute_hand, hand_to_heart_shake.</summary>
        public string gesture;
    }

    [Serializable]
    public class Custom
    {
        /// <summary>What an NPC acts out: pour_coffee, offer_dates, pour_tea, share_bread, bargain, play_game, invite_in, wave_hello.</summary>
        public string act;
        public string en, ar;
    }

    [Serializable]
    public class MusicStyle
    {
        public string style;
        public string[] instruments = Array.Empty<string>();
    }

    [Serializable]
    public class StreetLife
    {
        public string[] vendors = Array.Empty<string>();
        public string cafe = "";
        public string[] kidsGames = Array.Empty<string>();
        public string crowd = "";
    }

    /// <summary>Resources/Culture/&lt;country&gt;.json: how a country sounds, eats, greets and looks.</summary>
    [Serializable]
    public class CultureFile
    {
        public string country;
        /// <summary>Dialect or language id the NPC barks are written in (see CultureRules.Dialects).</summary>
        public string dialect;
        /// <summary>UI language a phone from here would get.</summary>
        public string uiLanguage = "en";
        public Greeting greeting = new Greeting();
        public LocalText hospitality = new LocalText();
        public Custom[] customs = Array.Empty<Custom>();
        public string architecture = "";
        public string[] palette = Array.Empty<string>();
        public MusicStyle music = new MusicStyle();
        public StreetLife street = new StreetLife();
        public Food[] foods = Array.Empty<Food>();
        /// <summary>Ids from Resources/Companions/local_animals.json.</summary>
        public string[] animals = Array.Empty<string>();
        /// <summary>The trail Aron's agal and Noura's handbag leave in this country (cherry petals, marigolds, sand swirl...).</summary>
        public string attackTrailVfx = "";
        /// <summary>The bored idle where Aron and Noura eat the country's signature dish.</summary>
        public EatIdle eatIdle = new EatIdle();
    }

    /// <summary>Aron and Noura eating the signature dish while they wait (Noura eats modestly, small bites, scarf in place).</summary>
    [Serializable]
    public class EatIdle
    {
        /// <summary>A food id from this file's foods.</summary>
        public string dishId;
        /// <summary>Resources/Props/Food/&lt;propModel&gt;; the generic plate is used until the model exists.</summary>
        public string propModel;
        /// <summary>yum (rubs belly), spicy (fans face), sweet (happy wiggle), crunchy (loud crunch), hot (blows on it), sticky (licks fingers... then wipes them).</summary>
        public string reaction = "yum";

        public static readonly string[] Reactions = { "yum", "spicy", "sweet", "crunchy", "hot", "sticky" };
        public const string GenericProp = "food_generic_plate";
        /// <summary>Animator trigger index for the eating idle on both heroes.</summary>
        public const int Trigger = 9;
    }

    public static class CultureRules
    {
        public static readonly string[] Dialects =
        {
            "najdi", "hijazi", "kuwaiti", "emirati", "bahraini", "qatari", "omani", "yemeni", "iraqi", "levantine",
            "jordanian", "egyptian", "sudanese", "libyan", "tunisian", "algerian", "darija",
            "hi", "ur", "tr", "en-US", "en-GB", "de", "es-MX", "pt-BR", "pt-PT", "id", "ms", "en-ZA", "en-AU",
            "ja", "fr", "es-ES", "uz", "hassaniya",
        };

        public static readonly string[] Gestures =
        {
            "handshake", "hand_on_chest", "nose_kiss", "namaste", "bow_nod", "cheek_kiss", "wave", "hand_to_heart_shake",
        };

        public static readonly string[] Acts =
        {
            "pour_coffee", "offer_dates", "pour_tea", "share_bread", "bargain", "play_game", "invite_in", "wave_hello", "offer_sweets",
        };

        public static readonly string[] Shapes = { "bowl", "plate", "bun", "cup", "roll", "cake", "skewer", "wrap", "ball", "stick" };

        /// <summary>Words that must never show up: the game is halal throughout.</summary>
        public static readonly string[] Forbidden =
        {
            "pork", "bacon", "ham", "chouriço", "chorizo", "salami", "beer", "wine", "alcohol", "liquor", "rum", "lard",
            "pig", "pigs", "swine", "schwein", "domuz", "cerdo", "porco", "babi", "सूअर", "शराब",
        };

        /// <summary>Arabic words matched inside other words too (attached articles and prefixes).</summary>
        public static readonly string[] ForbiddenArabic = { "خنزير", "خمر", "بيرة", "نبيذ", "كحول" };

        public const int MinFoods = 5, MaxFoods = 8;

        /// <summary>Tint for an attack trail (FX prefab id is "trail_" + the trail id).</summary>
        public static string TrailColour(string trail)
        {
            switch (trail)
            {
                case "cherry_petals": return "#F6B8C8";
                case "carnival_sparks": return "#2FA84F";
                case "savanna_dust": return "#C9A26B";
                case "red_dust": return "#B8562E";
                case "papel_picado": return "#E8457A";
                case "drizzle": return "#9FC3DD";
                case "autumn_leaves": return "#D2691E";
                case "marigold_petals": return "#F5A623";
                case "lavender": return "#9B7FD1";
                case "orange_blossom": return "#FFF4E0";
                case "suzani_glints": return "#C2343E";
                case "batik_glints": return "#8A5A2B";
                case "tulip_petals": return "#E0303C";
                case "stars": return "#F4F4FF";
                case "truck_art_sparkles": return "#FFD23F";
                case "azulejo_shards": return "#2C5FA8";
                case "pearl_glints": return "#F2EEE6";
                case "palm_fronds": return "#4E8B3A";
                case "lotus_petals": return "#F3D0E0";
                case "jasmine_petals": return "#FFFFF4";
                case "cedar_needles": return "#2F5D3A";
                case "zellige_shards": return "#1E7F78";
                default: return "#E8C878";
            }
        }

        /// <summary>Attack trail effects the VFX library builds.</summary>
        public static readonly string[] TrailVfx =
        {
            "sand_swirl", "pearl_glints", "palm_fronds", "frankincense_smoke", "coffee_steam", "date_sparkles", "jasmine_petals",
            "cedar_needles", "lotus_petals", "zellige_shards", "bougainvillea", "cherry_petals", "carnival_sparks", "savanna_dust", "red_dust",
            "papel_picado", "drizzle", "autumn_leaves", "marigold_petals", "lavender", "orange_blossom", "suzani_glints",
            "batik_glints", "tulip_petals", "stars", "truck_art_sparkles", "azulejo_shards", "henna_swirl", "reed_fluff", "olive_leaves",
        };

        public static bool Clean(string text)
        {
            if (string.IsNullOrEmpty(text)) return true;
            string t = text.ToLowerInvariant();
            foreach (var w in ForbiddenArabic) if (t.Contains(w)) return false;
            var word = new System.Text.StringBuilder();
            for (int i = 0; i <= t.Length; i++)
            {
                char c = i < t.Length ? t[i] : ' ';
                if (char.IsLetter(c) || char.GetUnicodeCategory(c) == System.Globalization.UnicodeCategory.NonSpacingMark || char.GetUnicodeCategory(c) == System.Globalization.UnicodeCategory.SpacingCombiningMark) { word.Append(c); continue; }
                if (word.Length > 0 && Array.IndexOf(Forbidden, word.ToString()) >= 0) return false;
                word.Clear();
            }
            return true;
        }

        public static List<string> Validate(CultureFile f, string fileName)
        {
            var e = new List<string>();
            string w = "culture " + fileName;
            if (f.country != fileName) e.Add(w + ": country must match the file name");
            if (Array.IndexOf(Dialects, f.dialect) < 0) e.Add(w + ": unknown dialect " + f.dialect);
            if (!UiLanguages.IsKnown(f.uiLanguage)) e.Add(w + ": unknown UI language " + f.uiLanguage);
            if (f.greeting == null || string.IsNullOrEmpty(f.greeting.words) || string.IsNullOrEmpty(f.greeting.wordsEn) || string.IsNullOrEmpty(f.greeting.wordsAr))
                e.Add(w + ": greeting words missing");
            else if (Array.IndexOf(Gestures, f.greeting.gesture) < 0) e.Add(w + ": unknown gesture " + f.greeting.gesture);
            foreach (var l in f.hospitality.Missing()) e.Add(w + ": hospitality missing in " + l);
            if (f.customs.Length < 2 || f.customs.Length > 3) e.Add(w + ": 2 or 3 everyday customs");
            foreach (var c in f.customs)
            {
                if (Array.IndexOf(Acts, c.act) < 0) e.Add(w + ": unknown custom act " + c.act);
                if (string.IsNullOrEmpty(c.en) || string.IsNullOrEmpty(c.ar)) e.Add(w + ": custom text missing");
            }
            if (string.IsNullOrEmpty(f.architecture)) e.Add(w + ": architecture notes missing");
            if (f.palette.Length < 3 || f.palette.Length > 6) e.Add(w + ": palette has 3 to 6 colours");
            foreach (var p in f.palette) if (!Hex(p)) e.Add(w + ": bad palette colour " + p);
            if (f.music == null || string.IsNullOrEmpty(f.music.style) || f.music.instruments.Length == 0) e.Add(w + ": music style missing");
            if (f.street == null || f.street.vendors.Length == 0 || f.street.kidsGames.Length == 0 || string.IsNullOrEmpty(f.street.crowd)) e.Add(w + ": street life missing");
            if (f.foods.Length < MinFoods || f.foods.Length > MaxFoods) e.Add(w + ": " + MinFoods + " to " + MaxFoods + " foods");
            var ids = new HashSet<string>();
            foreach (var food in f.foods)
            {
                string fw = w + " food " + food.id;
                if (string.IsNullOrEmpty(food.id) || !food.id.StartsWith(f.country + "_", StringComparison.Ordinal)) e.Add(fw + ": id must start with the country");
                if (!ids.Add(food.id ?? "")) e.Add(fw + ": duplicate");
                if (string.IsNullOrEmpty(food.local)) e.Add(fw + ": local name missing");
                foreach (var l in food.name.Missing()) e.Add(fw + ": name missing in " + l);
                foreach (var l in food.fact.Missing()) e.Add(fw + ": fact missing in " + l);
                if (Array.IndexOf(Shapes, food.shape) < 0) e.Add(fw + ": unknown shape " + food.shape);
                if (!Hex(food.color) || !Hex(food.color2)) e.Add(fw + ": colour");
                if (!Clean(food.local)) e.Add(fw + ": not halal");
                foreach (var t in food.name.All()) if (!Clean(t)) e.Add(fw + ": not halal: " + t);
                foreach (var t in food.fact.All()) if (!Clean(t)) e.Add(fw + ": not halal: " + t);
            }
            foreach (var food in f.foods)
            {
                if (Array.IndexOf(Profile.FoodBuffs.Kinds, food.buff) < 0) e.Add(w + " food " + food.id + ": unknown buff " + food.buff);
                if (food.price < 20 || food.price > 200) e.Add(w + " food " + food.id + ": stall price 20..200 coins");
            }
            foreach (var t in f.hospitality.All()) if (!Clean(t)) e.Add(w + ": hospitality not halal");
            if (f.animals.Length < 2) e.Add(w + ": at least two local animals");
            if (Array.IndexOf(TrailVfx, f.attackTrailVfx ?? "") < 0) e.Add(w + ": unknown attack trail " + f.attackTrailVfx);
            if (f.eatIdle == null || string.IsNullOrEmpty(f.eatIdle.dishId)) e.Add(w + ": no eating idle");
            else
            {
                Food dish = null;
                foreach (var food in f.foods) if (food.id == f.eatIdle.dishId) dish = food;
                if (dish == null) e.Add(w + ": eating idle dish " + f.eatIdle.dishId + " isn't one of the country's foods");
                if (string.IsNullOrEmpty(f.eatIdle.propModel) || !f.eatIdle.propModel.StartsWith("food_", StringComparison.Ordinal)) e.Add(w + ": eating idle prop is food_<name>");
                if (Array.IndexOf(EatIdle.Reactions, f.eatIdle.reaction) < 0) e.Add(w + ": unknown eating reaction " + f.eatIdle.reaction);
            }
            return e;
        }

        public static bool Hex(string s)
        {
            if (string.IsNullOrEmpty(s) || s.Length != 7 || s[0] != '#') return false;
            for (int i = 1; i < 7; i++) if (Uri.IsHexDigit(s[i]) == false) return false;
            return true;
        }
    }

    /// <summary>The food album: every food the player has picked up.</summary>
    public static class FoodAlbum
    {
        public static bool Has(SaveData d, string foodId) => d.foodsCollected != null && d.foodsCollected.Contains(foodId);

        /// <summary>Adds a food; true the first time.</summary>
        public static bool Collect(SaveData d, string foodId)
        {
            if (string.IsNullOrEmpty(foodId)) return false;
            if (d.foodsCollected == null) d.foodsCollected = new List<string>();
            if (d.foodsCollected.Contains(foodId)) return false;
            d.foodsCollected.Add(foodId);
            return true;
        }

        public static int Count(SaveData d, IEnumerable<CultureFile> all, out int total)
        {
            int n = 0; total = 0;
            foreach (var f in all)
                foreach (var food in f.foods) { total++; if (Has(d, food.id)) n++; }
            return n;
        }

        /// <summary>The food a stage's food pickup gives: the country's foods in turn through its stages.</summary>
        public static Food ForStage(CultureFile f, int stageIndex, int pickup)
        {
            if (f == null || f.foods.Length == 0) return null;
            return f.foods[(stageIndex + pickup) % f.foods.Length];
        }
    }
}
