// (c) Ahmed Alamrani. All rights reserved.
using System;
using System.Collections.Generic;

namespace Aron.Logic.Regions
{
    /// <summary>
    /// Every region, its stages and its boss. Each region is its own file, Resources/Regions/&lt;id&gt;.json,
    /// and <see cref="RegionDef.order"/> decides where it sits: Saudi 100-199, Gulf 200-299, Arab 300-399, the wider world 400-499.
    /// </summary>
    [Serializable]
    public class RegionCatalog
    {
        public RegionDef[] regions = Array.Empty<RegionDef>();

        public const int MinStages = 5, MaxStages = 8;
        public static readonly string[] Groups = { "saudi", "gulf", "arab", "world" };
        public static readonly string[] Terrains = { "street", "souq", "desert", "coast", "mountain", "oasis", "ruins", "forest", "river" };
        public static readonly string[] Mounts = { "", "camel", "horse", "giraffe" };
        public static readonly string[] Attacks = { "charge", "wave", "summon", "volley", "leap", "dash" };
        /// <summary>
        /// The first three are the costumed fighters, captain and mummy dress the robe one, the rest are built
        /// from scratch (see BossBodies).
        /// </summary>
        public static readonly string[] BossModels =
        {
            "soldier", "ninja", "robe", "captain", "mummy",
            "scientist", "shark", "golem", "wolf", "eagle", "clam", "barrel", "marid", "croc", "crab", "ghoul", "tree",
        };
        /// <summary>Bosses who come back in several regions (Arabic names).</summary>
        public static readonly string[] RecurringBosses = { "د. دحيم" };
        public static readonly string[] Headgear = { "helmet", "crown", "turban", "ghutra", "fez", "hood", "horns", "none" };

        /// <summary>Which of the three costumed bodies a boss model is dressed on, or -1 when it is a creature.</summary>
        public static int FighterBody(string model)
        {
            switch (model)
            {
                case "soldier": return 0;
                case "ninja": return 1;
                case "robe": case "captain": case "mummy": return 2;
                default: return -1;
            }
        }
        public static readonly string[] Times = { "day", "sunset", "night" };
        public static readonly string[] Weathers = { "clear", "rain", "sandstorm" };
        public static readonly string[] StageTypes = { "normal", "action", "chase", "boss", "race" };
        /// <summary>Race stages are extra: a region has 5 to 8 other stages and at most one race.</summary>
        public const int MaxRaces = 1;

        /// <summary>Builds a catalog from loose region files, sorted by order (then id, so ties stay stable).</summary>
        public static RegionCatalog FromRegions(IEnumerable<RegionDef> regions)
        {
            var list = new List<RegionDef>();
            foreach (var r in regions) if (r != null) list.Add(r);
            list.Sort((a, b) => a.order != b.order ? a.order.CompareTo(b.order) : string.CompareOrdinal(a.id, b.id));
            return new RegionCatalog { regions = list.ToArray() };
        }

        public RegionDef Region(string id)
        {
            foreach (var r in regions) if (r.id == id) return r;
            return null;
        }

        /// <summary>Finds a stage anywhere in the catalog. Region and stage index are -1 when it isn't there.</summary>
        public StageDef Stage(string stageId, out int regionIndex, out int stageIndex)
        {
            for (int r = 0; r < regions.Length; r++)
                for (int s = 0; s < regions[r].stages.Length; s++)
                    if (regions[r].stages[s].id == stageId) { regionIndex = r; stageIndex = s; return regions[r].stages[s]; }
            regionIndex = stageIndex = -1;
            return null;
        }

        /// <summary>The stage after this one: the next in the region, or the first of the next region.</summary>
        public StageDef Next(string stageId)
        {
            if (Stage(stageId, out int r, out int s) == null) return null;
            if (s + 1 < regions[r].stages.Length) return regions[r].stages[s + 1];
            return r + 1 < regions.Length ? regions[r + 1].stages[0] : null;
        }

        public List<string> Validate()
        {
            var errors = new List<string>();
            var regionIds = new HashSet<string>();
            var stageIds = new HashSet<string>();
            var bossNames = new HashSet<string>();
            var bossLooks = new HashSet<string>();
            var bossPatterns = new HashSet<string>();
            int lastGroup = 0, lastOrder = int.MinValue;
            if (regions == null || regions.Length == 0) { errors.Add("no regions"); return errors; }

            foreach (var r in regions)
            {
                string where = "region " + r.id;
                if (string.IsNullOrEmpty(r.id) || !regionIds.Add(r.id)) errors.Add(where + ": missing or duplicate id");
                if (string.IsNullOrEmpty(r.nameAr) || string.IsNullOrEmpty(r.nameEn)) errors.Add(where + ": needs Arabic and English names");
                int g = Array.IndexOf(Groups, r.group);
                if (g < 0) errors.Add(where + ": unknown group " + r.group);
                else if (g < lastGroup) errors.Add(where + ": Saudi regions first, then the Gulf, then the Arab countries, then the world");
                else lastGroup = g;
                if (r.order <= lastOrder) errors.Add(where + ": order must be unique and rising, got " + r.order);
                else if (g >= 0 && (r.order < (g + 1) * 100 || r.order > (g + 1) * 100 + 99)) errors.Add(where + ": order for " + r.group + " is " + (g + 1) * 100 + "-" + ((g + 1) * 100 + 99));
                lastOrder = r.order;
                if (!Hex(r.ground) || !Hex(r.accent) || !Hex(r.sky)) errors.Add(where + ": colours must be #RRGGBB");
                if (!MusicId(r.music)) errors.Add(where + ": music must be a track id such as music_country_egypt, got " + r.music);

                var stages = r.stages ?? Array.Empty<StageDef>();
                int races = 0;
                foreach (var st in stages) if (st.type == "race") races++;
                int story = stages.Length - races;
                if (story < MinStages || story > MaxStages) errors.Add(where + ": needs 5 to 8 stages besides races, has " + story);
                if (races > MaxRaces) errors.Add(where + ": at most one race stage");
                int lastDifficulty = 0;
                bool night = false, action = false, weather = false;
                for (int i = 0; i < stages.Length; i++)
                {
                    var s = stages[i];
                    string sw = where + " stage " + s.id;
                    if (string.IsNullOrEmpty(s.id) || !stageIds.Add(s.id)) errors.Add(sw + ": missing or duplicate id");
                    if (string.IsNullOrEmpty(s.nameAr) || string.IsNullOrEmpty(s.nameEn)) errors.Add(sw + ": needs names");
                    if (Array.IndexOf(Terrains, s.terrain) < 0) errors.Add(sw + ": unknown terrain " + s.terrain);
                    if (Array.IndexOf(Mounts, s.mount ?? "") < 0) errors.Add(sw + ": unknown mount " + s.mount);
                    if (s.difficulty < 1 || s.difficulty > 10) errors.Add(sw + ": difficulty 1..10");
                    if (s.difficulty < lastDifficulty) errors.Add(sw + ": difficulty goes down");
                    lastDifficulty = s.difficulty;
                    if (!string.IsNullOrEmpty(s.music) && !MusicId(s.music)) errors.Add(sw + ": music must be a track id such as music_tabuk_boss, got " + s.music);
                    if (s.landmarks == null || s.landmarks.Length == 0) errors.Add(sw + ": needs a landmark");
                    else foreach (var l in s.landmarks)
                    {
                        if (Array.IndexOf(LandmarkKinds.All, l) < 0) errors.Add(sw + ": unknown landmark " + l);
                        if (Array.IndexOf(LandmarkKinds.Sacred, l) >= 0) errors.Add(sw + ": " + l + " is a place of worship; skyline only");
                    }
                    if (RespectRules.NamesAHolyPlace(s.nameEn) || RespectRules.NamesAHolyPlace(s.nameAr)) errors.Add(sw + ": no stage is set at a mosque or holy site");
                    // The iron tower's night lighting is a protected work: the tower only appears by day or at sunset.
                    if (s.landmarks != null && Array.IndexOf(s.landmarks, "iron_tower") >= 0 && s.time == "night") errors.Add(sw + ": the iron tower is never shown at night");
                    if (RespectRules.NamesABrand(s.nameEn)) errors.Add(sw + ": no real brands or building trade names in stage names");
                    bool last = i == stages.Length - 1;
                    if (s.boss != last) errors.Add(sw + (last ? ": the last stage must be the boss" : ": only the last stage is a boss"));
                    if (s.boss && !string.IsNullOrEmpty(s.mount)) errors.Add(sw + ": no mounts in a boss arena");
                    if (Array.IndexOf(Times, s.time) < 0) errors.Add(sw + ": time is day, sunset or night");
                    if (Array.IndexOf(Weathers, s.weather) < 0) errors.Add(sw + ": weather is clear, rain or sandstorm");
                    if (Array.IndexOf(StageTypes, s.type) < 0) errors.Add(sw + ": unknown stage type " + s.type);
                    else if ((s.type == "boss") != s.boss) errors.Add(sw + ": the boss stage and only the boss stage has type boss");
                    if (s.weather == "sandstorm" && s.terrain != "desert" && s.terrain != "ruins" && s.terrain != "street" && s.terrain != "souq")
                        errors.Add(sw + ": a sandstorm needs sand around, not " + s.terrain);
                    if (!string.IsNullOrEmpty(s.costume) && !CostumeId(s.costume)) errors.Add(sw + ": costume id looks wrong: " + s.costume);
                    if (s.type == "race")
                    {
                        if (string.IsNullOrEmpty(s.race) || !CostumeId(s.race)) errors.Add(sw + ": a race stage names its track");
                        if (!string.IsNullOrEmpty(s.mount)) errors.Add(sw + ": a race brings its own ride, no mount");
                        if (i == 0) errors.Add(sw + ": a race is never the first stage");
                        continue;
                    }
                    if (!string.IsNullOrEmpty(s.race)) errors.Add(sw + ": only race stages name a track");
                    night |= s.time == "night";
                    action |= s.type == "action" || s.type == "chase";
                    weather |= s.weather == "rain" || s.weather == "sandstorm";
                }
                if (stages.Length > 0)
                {
                    if (!night) errors.Add(where + ": needs at least one night stage");
                    if (!action) errors.Add(where + ": needs at least one action or chase stage");
                    if (!weather) errors.Add(where + ": needs a rain or sandstorm stage");
                }
                foreach (var k in r.skyline ?? Array.Empty<string>()) if (Array.IndexOf(LandmarkKinds.All, k) < 0) errors.Add(where + ": unknown skyline " + k);
                if (string.IsNullOrEmpty(r.country)) errors.Add(where + ": needs a country for Aron's costume");
                if (!string.IsNullOrEmpty(r.costume) && !CostumeId(r.costume)) errors.Add(where + ": costume id looks wrong: " + r.costume);

                var b = r.boss;
                if (b == null) { errors.Add(where + ": no boss"); continue; }
                // Dr. Duhaim is the recurring villain: he may be the boss of more than one region, but never looks or fights the same way twice.
                if (string.IsNullOrEmpty(b.nameAr) || (!bossNames.Add(b.nameAr) && Array.IndexOf(RecurringBosses, b.nameAr) < 0)) errors.Add(where + ": boss name missing or used twice");
                if (Array.IndexOf(BossModels, b.model) < 0) errors.Add(where + ": unknown boss model " + b.model);
                if (Array.IndexOf(Headgear, b.headgear) < 0) errors.Add(where + ": unknown headgear " + b.headgear);
                if (!Hex(b.body) || !Hex(b.trim) || !Hex(b.projectile)) errors.Add(where + ": boss colours must be #RRGGBB");
                if (!bossLooks.Add(b.model + b.headgear + b.body.ToUpperInvariant())) errors.Add(where + ": boss looks like another boss");
                if (b.health < 6 || b.health > 20) errors.Add(where + ": boss health 6..20");
                if (b.phases == null || b.phases.Length < 2) { errors.Add(where + ": boss needs at least two phases"); continue; }
                float lastThreshold = 2f;
                foreach (var p in b.phases)
                {
                    if (p.threshold >= lastThreshold) errors.Add(where + ": phase thresholds must go down");
                    lastThreshold = p.threshold;
                    if (p.attacks == null || p.attacks.Length == 0) errors.Add(where + ": a phase with no attacks");
                    else foreach (var a in p.attacks) if (Array.IndexOf(Attacks, a) < 0) errors.Add(where + ": unknown attack " + a);
                    if (string.IsNullOrEmpty(p.tauntAr)) errors.Add(where + ": every phase needs a taunt");
                    if (p.attacks != null && Array.IndexOf(p.attacks, "charge") < 0 && Array.IndexOf(p.attacks, "volley") < 0
                        && Array.IndexOf(p.attacks, "leap") < 0 && Array.IndexOf(p.attacks, "dash") < 0)
                        errors.Add(where + ": a phase where the boss never gets dizzy can't be won");
                }
                if (b.phases[0].threshold < 0.999f) errors.Add(where + ": the first phase starts at full health");
                if (!bossPatterns.Add(b.Pattern)) errors.Add(where + ": boss fights the same way as another boss");
            }
            return errors;
        }

        /// <summary>Track ids are lower case words joined by underscores and start with music_.</summary>
        private static bool MusicId(string s) => !string.IsNullOrEmpty(s) && s.StartsWith("music_", StringComparison.Ordinal) && CostumeId(s);

        /// <summary>Costume ids are lower case words joined by underscores, e.g. saudi_hijazi.</summary>
        private static bool CostumeId(string s)
        {
            foreach (char c in s) if (!(c == '_' || (c >= 'a' && c <= 'z') || (c >= '0' && c <= '9'))) return false;
            return s.Length > 0;
        }

        private static bool Hex(string s)
        {
            if (s == null || s.Length != 7 || s[0] != '#') return false;
            for (int i = 1; i < 7; i++) if (Uri.IsHexDigit(s[i]) == false) return false;
            return true;
        }
    }

    [Serializable]
    public class RegionDef
    {
        public string id;
        public string nameAr, nameEn;
        /// <summary>Position in the game: Saudi 100-199, Gulf 200-299, Arab 300-399, world 400-499.</summary>
        public int order;
        /// <summary>saudi, gulf, arab or world.</summary>
        public string group;
        /// <summary>Country id; Aron wears that country's costume (Resources/Costumes/&lt;country&gt;.json).</summary>
        public string country = "";
        /// <summary>Costume variant for the whole region, e.g. saudi_hijazi. Empty means the country's default.</summary>
        public string costume = "";
        /// <summary>Theme track id (Resources/Audio/Music), played in this region's normal stages. See <see cref="MusicCues"/>.</summary>
        public string music = MusicCues.SaudiNajd;
        public string ground = "#D9B98A", accent = "#A1262B", sky = "#9EC7E8";
        public StageDef[] stages = Array.Empty<StageDef>();
        /// <summary>
        /// Distant backdrop only (see <see cref="LandmarkKinds.Sacred"/>): houses of worship and holy places are
        /// shown far off, never as a stage landmark where enemies, crates or a boss could be.
        /// </summary>
        public string[] skyline = Array.Empty<string>();
        public BossDef boss;
    }

    [Serializable]
    public class StageDef
    {
        public string id;
        public string nameAr, nameEn;
        public string terrain = "street";
        public string[] landmarks = Array.Empty<string>();
        public string mount = "";
        public int difficulty = 1;
        public bool boss;
        /// <summary>What Aron says when the stage starts.</summary>
        public string lineAr = "";
        /// <summary>Optional extras: "shops" puts Ahmed's two shops on the street.</summary>
        public string extra = "";
        /// <summary>day, sunset or night.</summary>
        public string time = "day";
        /// <summary>clear, rain or sandstorm.</summary>
        public string weather = "clear";
        /// <summary>normal, action (enemy waves, explosions, a timed escape), chase (run from a hazard), race (see Races) or boss.</summary>
        public string type = "normal";
        /// <summary>Costume variant just for this stage, e.g. the bisht for a boss. Empty means the region's.</summary>
        public string costume = "";
        /// <summary>Race stages only: the track id in Resources/Races/tracks.json.</summary>
        public string race = "";
        /// <summary>Optional camera mode for this stage (free, chase, race, boss). Empty means by stage type (see Resources/Config/camera.json).</summary>
        public string camera = "";
        /// <summary>Optional play time in minutes (5..8, clamped to the stage type's range). 0 means by type and difficulty (Config/stage_timing.json).</summary>
        public float targetMinutes;
        /// <summary>Optional par time in seconds for the third star. 0 means a share of the estimated time.</summary>
        public float parSeconds;
        /// <summary>Optional track id for this stage alone (a boss fight names its own here). Empty means by stage type, then the region theme.</summary>
        public string music = "";
        public bool IsRace => type == "race";

        public int Seed
        {
            get
            {
                unchecked
                {
                    int h = 17;
                    foreach (char c in id ?? "") h = h * 31 + c;
                    return h & 0x7fffffff;
                }
            }
        }
    }

    [Serializable]
    public class BossDef
    {
        public string nameAr, nameEn;
        public string model = "soldier";
        public string headgear = "helmet";
        public string body = "#4D5240", trim = "#D9B340", projectile = "#6B3A1E";
        public float scale = 1f;
        public int health = 9;
        public string introAr = "", stunAr = "آخ! راسي...", defeatAr = "";
        public BossPhaseDef[] phases = Array.Empty<BossPhaseDef>();

        /// <summary>The attacks phase by phase, used to keep every boss fight different.</summary>
        public string Pattern
        {
            get
            {
                var parts = new List<string>();
                foreach (var p in phases ?? Array.Empty<BossPhaseDef>()) parts.Add(string.Join("+", p.attacks ?? Array.Empty<string>()));
                return string.Join("|", parts);
            }
        }
    }

    [Serializable]
    public class BossPhaseDef
    {
        public float threshold = 1f;
        public float move = 3f, charge = 11f, interval = 3f;
        public string[] attacks = { "charge" };
        public string tauntAr = "";
    }

    /// <summary>Landmark builders the stage generator knows how to put up.</summary>
    public static class LandmarkKinds
    {
        public static readonly string[] All =
        {
            "fort", "round_fort", "mud_town", "tower_houses", "painted_houses", "blue_town", "rawasheen",
            "souq", "gate", "skyscraper", "needle_tower", "twin_spheres", "wind_towers", "dhows", "fountain",
            "causeway", "columns", "amphitheater", "rock_tomb", "rock_arch", "pyramids", "spiral_minaret",
            "big_arch", "waterwheel", "cedars", "palms", "dragon_trees", "monument", "volcano",
            "railway", "stone_pillars", "water_tower", "lone_tree", "salt_lake", "chimneys", "rings",
            "terraces", "reeds", "dunes", "mountains", "lake", "tents", "arch_tower", "masmak",
            // The wider world. Stylised shapes only, no copies of real buildings.
            "marble_mausoleum", "bazaar_arches", "tent_mosque", "snow_peaks", "covered_bazaar",
            "canyon", "torch_statue", "clock_tower", "red_bus", "row_houses", "fairytale_castle", "black_forest",
            "step_pyramid", "colour_plaza", "rainforest", "sea_tower", "tile_houses", "tram", "sea_cliffs",
            "volcano_rice_terraces", "twin_towers", "stilt_house", "houseboat", "palace_fort", "hill_station",
            "flat_mountain", "savanna", "township_market", "drakensberg", "acacia",
            "castle_keep", "snow_mountain", "bamboo_grove", "red_wooden_bridge", "bamboo_forest", "cherry_trees", "iron_tower", "lavender_fields",
            "palace_arches", "white_village", "windmills", "blue_domes", "madrasa_portal", "eye_rings",
            "shell_hall", "steel_arch", "gum_forest", "coral_reef", "red_rock_ridges",
        };

        /// <summary>
        /// Words no landmark id may contain (Ahmed's rule: no religious symbols or religious origin in landmarks of the
        /// regions that carry none). Checked by RespectTests against every stage and skyline landmark.
        /// </summary>
        public static readonly string[] Forbidden = { "stupa", "pagoda", "torii", "church", "cathedral", "cross", "redeemer", "temple_active", "shrine",
            // Dropped on review: a statue and a basilica shape, and a sacred rock that has image restrictions.
            "arms_statue", "domed_basilica", "uluru", "red_monolith" };

        /// <summary>Houses of worship and holy places: allowed only as a region's far skyline.</summary>
        public static readonly string[] Sacred = { "spiral_minaret", "tent_mosque", "marble_mausoleum", "blue_domes" };
    }

    /// <summary>Religious and national respect rules every stage, prop and piece of text is checked against.</summary>
    public static class RespectRules
    {
        /// <summary>Trade names of buildings and brands kept out of stage names (generic names instead).</summary>
        public static readonly string[] BrandWords = { "burj khalifa", "kingdom centre", "kingdom center", "eiffel", "louvre pyramid", "big ben",
            "disney", "real madrid", "barcelona fc", "manchester", "toyota", "ferrari", "coca", "mcdonald" };

        public static bool NamesABrand(string name)
        {
            string n = (name ?? "").ToLowerInvariant();
            foreach (var w in BrandWords) if (n.Contains(w)) return true;
            return false;
        }

        /// <summary>
        /// Words that put a stage at a living place of worship or a holy site (mosques, the two Holy Mosques, Uhud,
        /// churches, shrines). Ancient ruins no longer used for worship (Karnak, Baalbek) are archaeological sites and allowed.
        /// </summary>
        public static readonly string[] HolyPlaceWords =
        {
            "mosque", "masjid", "haram", "kaaba", "ka'bah", "uhud", "church", "cathedral", "basilica", "shrine",
            "synagogue", "مسجد", "الحرم", "الكعبة", "أحد", "الأموي", "كنيسة", "كاتدرائية", "ضريح",
        };

        public static bool NamesAHolyPlace(string name)
        {
            if (string.IsNullOrEmpty(name)) return false;
            string n = name.ToLowerInvariant();
            foreach (var w in HolyPlaceWords)
            {
                if (w == "أحد") { if (n == "جبل أحد" || n.Contains("جبل أحد")) return true; continue; }
                if (n.Contains(w)) return true;
            }
            // "Jemaa el-Fnaa" and "جامع الفنا" are a public square, not a mosque.
            if (n.Contains("جامع") && !n.Contains("جامع الفنا")) return true;
            return false;
        }

        /// <summary>
        /// Flags carrying sacred text (the Shahada on Saudi Arabia's, "Allahu Akbar" on Iraq's) and in fact every
        /// national flag: never on a crate, floor, rug, lower clothing, bin or anything that breaks, burns or is
        /// stepped on. The game simply draws no flags; colours appear only as abstract trims.
        /// </summary>
        public static readonly string[] NoFlagSurfaces = { "crate", "floor", "rug", "shoe", "sirwal", "bin", "breakable", "projectile", "doormat" };

        /// <summary>Texts that are never printed on anything in the world (they only appear in no place at all).</summary>
        public static readonly string[] NeverOnProps = { "لا إله إلا الله", "محمد رسول الله", "الله أكبر", "بسم الله", "الله" };

        public static bool PropTextAllowed(string text)
        {
            if (string.IsNullOrEmpty(text)) return true;
            foreach (var w in NeverOnProps) if (text.Contains(w)) return false;
            return true;
        }
    }
}
