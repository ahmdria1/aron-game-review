// (c) Ahmed Alamrani. All rights reserved.
using System;
using System.Collections.Generic;
using System.Globalization;
using Aron.Logic.Culture;
using Aron.Logic.Regions;

namespace Aron.Logic.Passport
{
    /// <summary>One page of Aron's passport (جواز سفر أرون): one per country, in the player's ring order.</summary>
    public class PassportPage
    {
        public string country;
        public string localName;
        public bool stamped;
        public string stampDate = "";
        public bool visa;
        public string visaDate = "";
        /// <summary>"" none yet, "auto" the after-boss souvenir, or "photo:&lt;file&gt;" a Photo Mode shot.</summary>
        public string photo = "";
        public int foods, foodsTotal;
        /// <summary>The country's cultural artifact has been found (a small museum sticker on the page).</summary>
        public bool artifact;
        public string artifactDate = "";
        /// <summary>Best medal of the country's race track, "" when not raced.</summary>
        public string raceMedal = "";
        /// <summary>Time medal per finished stage of the country, in stage order ("" where none yet).</summary>
        public List<string> timeMedals = new List<string>();
        public StampArt stamp;
    }

    /// <summary>
    /// The look of an entry stamp. Original designs only: a frame shape, an ink colour, the country's name in its own
    /// script, the date and one of the game's own motif icons. Never a flag, coat of arms, seal or government emblem.
    /// </summary>
    public class StampArt
    {
        public string shape, ink, motif, text, date;
        public float tilt;

        public static readonly string[] Shapes = { "circle", "oval", "hexagon", "octagon", "rectangle", "shield_round", "star8", "ticket" };
        public static readonly string[] Inks = { "#B3262E", "#1F4E9A", "#2E7D4F", "#6A3D9A", "#B5651D", "#3B3B3B", "#0F7C86", "#9C2E6B" };
        /// <summary>Motifs drawn for the game (landmark silhouettes, animals, food); none is an official symbol.</summary>
        public static readonly string[] Motifs = { "landmark", "animal", "dish", "palm", "dhow", "camel", "falcon", "lantern", "sun", "wave" };
        /// <summary>Words a stamp must never carry, in any case: they belong to real state symbols.</summary>
        public static readonly string[] Banned = { "coat of arms", "emblem", "seal of", "ministry", "government", "republic of", "kingdom of", "شعار", "وزارة", "ختم الدولة" };

        public static StampArt For(string country, string localName, string date)
        {
            int h = Hash(country);
            return new StampArt
            {
                shape = Shapes[h % Shapes.Length],
                ink = Inks[(h / 7) % Inks.Length],
                motif = Motifs[(h / 53) % Motifs.Length],
                text = localName,
                date = date,
                tilt = ((h / 13) % 17) - 8,
            };
        }

        public bool Original()
        {
            string t = (text ?? "").ToLowerInvariant();
            foreach (var b in Banned) if (t.Contains(b)) return false;
            return Array.IndexOf(Motifs, motif) >= 0 && Array.IndexOf(Shapes, shape) >= 0;
        }

        private static int Hash(string s)
        {
            unchecked
            {
                int h = 23;
                foreach (char c in s ?? "") h = h * 31 + c;
                return h & 0x7fffffff;
            }
        }
    }

    public static class PassportBook
    {
        /// <summary>Feedback the stamp moment fires: the thunk sound, a firm haptic and the ink-splat effect.</summary>
        public const string StampSfx = "passport_stamp", StampHaptic = "passport_stamp", StampFx = "stamp_ink";
        public const string VisaSfx = "passport_visa", VisaHaptic = "passport_visa";

        /// <summary>Each country's name in its own script, for its stamp.</summary>
        public static readonly Dictionary<string, string> LocalNames = new Dictionary<string, string>
        {
            ["saudi"] = "السعودية", ["kuwait"] = "الكويت", ["uae"] = "الإمارات", ["bahrain"] = "البحرين", ["qatar"] = "قطر",
            ["oman"] = "عُمان", ["yemen"] = "اليمن", ["iraq"] = "العراق", ["syria"] = "سوريا", ["lebanon"] = "لبنان",
            ["jordan"] = "الأردن", ["egypt"] = "مصر", ["sudan"] = "السودان", ["libya"] = "ليبيا", ["tunisia"] = "تونس",
            ["algeria"] = "الجزائر", ["morocco"] = "المغرب ⵍⵎⵖⵔⵉⴱ", ["mauritania"] = "موريتانيا",
            ["turkey"] = "Türkiye", ["pakistan"] = "پاکستان", ["india"] = "भारत", ["indonesia"] = "Indonesia",
            ["malaysia"] = "Malaysia", ["uk"] = "United Kingdom", ["germany"] = "Deutschland", ["usa"] = "United States",
            ["mexico"] = "México", ["brazil"] = "Brasil", ["portugal"] = "Portugal", ["south_africa"] = "South Africa",
            ["japan"] = "日本", ["france"] = "France", ["spain"] = "España", ["uzbekistan"] = "Oʻzbekiston", ["australia"] = "Australia",
        };

        public static string Day(DateTime utc) => utc.ToUniversalTime().ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);

        /// <summary>The date as printed on a stamp: day.month.year with Western digits, the same in every language.</summary>
        public static string Printed(string day)
        {
            if (!DateTime.TryParseExact(day, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var d)) return "";
            return d.ToString("dd.MM.yyyy", CultureInfo.InvariantCulture);
        }

        /// <summary>True when every boss stage of the country is done.</summary>
        public static bool CountryComplete(RegionCatalog catalog, SaveData data, string country)
        {
            bool any = false;
            foreach (var r in catalog.regions)
            {
                if (ProgressRing.CountryOf(r) != country) continue;
                foreach (var s in r.stages)
                    if (s.boss) { any = true; if (!StageProgress.IsDone(data, s.id)) return false; }
            }
            return any;
        }

        /// <summary>
        /// Called after a stage is recorded as finished. Stamps the country's page when <paramref name="stageId"/> is a
        /// boss stage and it was the country's last boss still standing. True when a new stamp went down (play the
        /// stamp animation, sound and haptic, then the souvenir photo).
        /// </summary>
        public static bool OnStageFinished(RegionCatalog catalog, SaveData data, string stageId, DateTime utcNow)
        {
            var s = catalog.Stage(stageId, out int r, out _);
            if (s == null || !s.boss || !StageProgress.IsDone(data, stageId)) return false;
            string country = ProgressRing.CountryOf(catalog.regions[r]);
            if (Has(data.passportStamps, country) || !CountryComplete(catalog, data, country)) return false;
            Set(ref data.passportStamps, country, Day(utcNow));
            if (string.IsNullOrEmpty(Get(data.passportPhotos, country))) Set(ref data.passportPhotos, country, "auto");
            return true;
        }

        /// <summary>A travel-ticket visit puts a small visa sticker on the page, once. A stamped page still gets one.</summary>
        public static bool OnTicketVisit(SaveData data, string country, DateTime utcNow)
        {
            if (string.IsNullOrEmpty(country) || Has(data.passportVisas, country)) return false;
            Set(ref data.passportVisas, country, Day(utcNow));
            return true;
        }

        /// <summary>Puts a Photo Mode shot on a page in place of the auto souvenir. Only stamped or visa pages have a photo slot.</summary>
        public static bool SetPhoto(SaveData data, string country, string fileName)
        {
            if (!Has(data.passportStamps, country) && !Has(data.passportVisas, country)) return false;
            if (string.IsNullOrEmpty(fileName) || fileName.IndexOfAny(new[] { '/', '\\', '=' }) >= 0) return false;
            Set(ref data.passportPhotos, country, "photo:" + fileName);
            return true;
        }

        public static PassportPage Page(RegionCatalog catalog, SaveData data, string country, CultureFile culture)
        {
            string local = LocalNames.TryGetValue(country, out var n) ? n : country;
            var p = new PassportPage { country = country, localName = local };
            p.stampDate = Get(data.passportStamps, country);
            p.stamped = p.stampDate.Length > 0;
            p.visaDate = Get(data.passportVisas, country);
            p.visa = p.visaDate.Length > 0;
            p.photo = Get(data.passportPhotos, country);
            p.artifactDate = Get(data.artifactsFound, country);
            p.artifact = p.artifactDate.Length > 0;
            if (p.stamped) p.stamp = StampArt.For(country, local, Printed(p.stampDate));
            if (culture != null && culture.foods != null)
            {
                p.foodsTotal = culture.foods.Length;
                foreach (var f in culture.foods) if (FoodAlbum.Has(data, f.id)) p.foods++;
            }
            foreach (var r in catalog.regions)
            {
                if (ProgressRing.CountryOf(r) != country) continue;
                foreach (var s in r.stages)
                {
                    if (s.IsRace)
                    {
                        string m = RaceMedal(data, s.race);
                        if (Rank(m) > Rank(p.raceMedal)) p.raceMedal = m;
                        continue;
                    }
                    if (s.boss) continue;
                    var rec = StageProgress.Get(data, s.id);
                    p.timeMedals.Add(rec != null && rec.bestTime > 0f ? Secrets.TimeMedal(rec.bestTime, Secrets.MedalTarget(s)) : "");
                }
            }
            return p;
        }

        /// <summary>Every page, one per country in the player's ring order (home country first).</summary>
        public static List<PassportPage> Pages(RegionCatalog catalog, SaveData data, Func<string, CultureFile> culture)
        {
            var list = new List<PassportPage>();
            foreach (var c in ProgressRing.CountryOrder(catalog, data.homeCountry))
                list.Add(Page(catalog, data, c, culture?.Invoke(c)));
            return list;
        }

        /// <summary>Arabic and Urdu books open from the right and turn leftwards; the rest open from the left.</summary>
        public static bool OpensFromRight(string uiLanguage) => uiLanguage == "ar" || uiLanguage == "ur";

        /// <summary>The page a turn lands on: a forward swipe in reading direction moves on, clamped to the book.</summary>
        public static int Turn(int page, int pages, float swipeX, string uiLanguage)
        {
            if (pages <= 0) return 0;
            int dir = Math.Sign(swipeX);
            if (dir == 0) return Math.Max(0, Math.Min(pages - 1, page));
            int forward = OpensFromRight(uiLanguage) ? dir : -dir;
            return Math.Max(0, Math.Min(pages - 1, page + forward));
        }

        /// <summary>The file name a shared page gets. Only the country and day: no player name, id or place.</summary>
        public static string ShareFileName(string country, DateTime utcNow) =>
            "aron_passport_" + country + "_" + utcNow.ToUniversalTime().ToString("yyyyMMdd", CultureInfo.InvariantCulture) + ".png";

        /// <summary>The race medal kept for a track (stored as place 1, 2 or 3) as gold, silver or bronze.</summary>
        public static string RaceMedal(SaveData data, string track)
        {
            switch (Get(data.raceMedals, track))
            {
                case "1": return "gold";
                case "2": return "silver";
                case "3": return "bronze";
                default: return "";
            }
        }

        private static int Rank(string medal) => Math.Max(0, Array.IndexOf(Secrets.Medals, medal ?? ""));

        internal static bool Has(List<string> list, string key) => Get(list, key).Length > 0;

        internal static string Get(List<string> list, string key)
        {
            if (list == null) return "";
            foreach (var e in list)
                if (e.Length > key.Length && e.StartsWith(key + "=", StringComparison.Ordinal)) return e.Substring(key.Length + 1);
            return "";
        }

        internal static void Set(ref List<string> list, string key, string value)
        {
            if (list == null) list = new List<string>();
            list.RemoveAll(e => e.StartsWith(key + "=", StringComparison.Ordinal));
            list.Add(key + "=" + value);
        }
    }
}
