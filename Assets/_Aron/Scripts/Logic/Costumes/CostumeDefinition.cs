// (c) Ahmed Alamrani. All rights reserved.
using System;
using System.Collections.Generic;

namespace Aron.Logic.Costumes
{
    /// <summary>All costumes of one country: Resources/Costumes/&lt;country&gt;.json.</summary>
    [Serializable]
    public class CostumeFile
    {
        public string country;
        public CostumeDefinition[] costumes = Array.Empty<CostumeDefinition>();
    }

    /// <summary>
    /// One outfit for Aron or Noura. Material swaps recolour parts of the character model by material name,
    /// mesh swaps replace a body slot, and accessories are added on bones.
    /// </summary>
    [Serializable]
    public class CostumeDefinition
    {
        public string id;
        public string country;
        /// <summary>aron or noura.</summary>
        public string hero = "aron";
        public string nameAr, nameEn;
        /// <summary>Worn by default in this country.</summary>
        public bool isDefault;
        /// <summary>Coins to own it for free picking in the wardrobe. 0 means it's unlocked by reaching the country.</summary>
        public int price;
        /// <summary>Empty, "hard_all" (beat every country on hard) or "gift" (put together from gift-box pieces). Never sold.</summary>
        public string unlock = "";
        public bool IsReward => !string.IsNullOrEmpty(unlock);
        public MaterialSwap[] materials = Array.Empty<MaterialSwap>();
        public MeshSwap[] meshes = Array.Empty<MeshSwap>();
        public Accessory[] accessories = Array.Empty<Accessory>();
        public string footwear = "leather_shoes";
        public string footwearColor = "#5A3A22";
        /// <summary>The country-flavoured bored idle this outfit adds (Animator trigger Bored_N, N 20 and up). Empty id for none.</summary>
        public Animation.BoredClip boredIdle = new Animation.BoredClip();
        /// <summary>
        /// Where Aron's agal sits: "head" on a headdress (socket AgalHead), "wrist" coiled on the right wrist
        /// like a bracelet when the outfit has no headdress agal (socket AgalWrist). It is his weapon either way:
        /// on attack it unwinds, swings and snaps back. Noura's outfits say "none"; her handbag is her weapon.
        /// </summary>
        public string agalMount = "";

        /// <summary>head when the outfit has a headdress agal, wrist otherwise; none for Noura.</summary>
        public string ExpectedAgalMount => hero == "noura" ? "none" : (Has("agal") || Has("agal_cords")) ? "head" : "wrist";
        /// <summary>The socket the runtime uses: the saved choice, or the expected one when it is empty.</summary>
        public string AgalSocket => (string.IsNullOrEmpty(agalMount) ? ExpectedAgalMount : agalMount) == "head" ? "AgalHead" : "AgalWrist";

        public bool Has(string accessoryKind)
        {
            foreach (var a in accessories) if (a.kind == accessoryKind) return true;
            return false;
        }
    }

    [Serializable]
    public class MaterialSwap
    {
        /// <summary>Part of the material name to match, e.g. "Clothes" or "Skin".</summary>
        public string material;
        public string color = "#FFFFFF";
    }

    [Serializable]
    public class MeshSwap
    {
        /// <summary>body, head or feet.</summary>
        public string slot;
        /// <summary>Name of a mesh the editor ships in the costume kit, e.g. thobe_long.</summary>
        public string mesh;
    }

    [Serializable]
    public class Accessory
    {
        public string kind;
        public string color = "#FFFFFF";
        /// <summary>Second colour for patterned cloth (the red shemagh's check).</summary>
        public string pattern = "";
        /// <summary>Bone it hangs from; empty uses the kind's usual place.</summary>
        public string bone = "";
    }

    public static class CostumeRules
    {
        public static readonly string[] Heroes = { "aron", "noura" };
        /// <summary>Closed shoes only. Sneakers, sandals and bare feet aren't in the list on purpose.</summary>
        public static readonly string[] Footwear = { "leather_shoes", "closed_flats", "leather_boots" };
        public static readonly string[] Slots = { "body", "head", "feet" };
        /// <summary>Country bored idles sit after the shared ones in the Animator (Bored_20 and up).</summary>
        public const int BoredCountryFirst = 20;

        public static readonly string[] AccessoryKinds =
        {
            "shemagh", "ghutra", "agal", "kufi", "bisht", "turban", "fez", "keffiyeh", "sash", "vest", "jambiya_belt",
            "hijab", "abaya", "embroidery", "headband", "flower_crown", "shawl", "belt",
            "kumma", "massar", "khanjar", "futa", "imma", "chechia", "jard", "yashmagh", "tarboosha", "agal_cords",
            "pagri", "jinnah_cap", "cowboy_hat", "top_hat", "bavarian_hat", "sombrero", "songkok", "fisherman_cap", "straw_hat",
            "kurta", "shirt", "shalwar", "jeans", "lederhosen", "long_coat", "charro_jacket", "dupatta", "sari_drape",
            "rebozo", "apron", "samping", "batik", "kebaya", "bush_hat", "doek", "shweshwe", "safari_vest", "outback_hat",
        };

        public static List<string> Validate(CostumeFile file)
        {
            var errors = new List<string>();
            if (file == null || file.costumes == null || file.costumes.Length == 0) { errors.Add("no costumes"); return errors; }
            var ids = new HashSet<string>();
            var defaults = new Dictionary<string, int>();
            foreach (var c in file.costumes)
            {
                string w = "costume " + c.id;
                if (string.IsNullOrEmpty(c.id) || !ids.Add(c.id)) errors.Add(w + ": missing or duplicate id");
                if (c.country != file.country) errors.Add(w + ": country must be " + file.country);
                if (string.IsNullOrEmpty(c.id) || !c.id.StartsWith(file.country + "_", StringComparison.Ordinal)) errors.Add(w + ": id starts with the country");
                if (Array.IndexOf(Heroes, c.hero) < 0) errors.Add(w + ": hero is aron or noura");
                if (string.IsNullOrEmpty(c.nameAr)) errors.Add(w + ": needs an Arabic name");
                if (Array.IndexOf(Footwear, c.footwear) < 0) errors.Add(w + ": closed shoes only, not " + c.footwear);
                if (!Hex(c.footwearColor)) errors.Add(w + ": footwear colour");
                foreach (var m in c.materials) if (string.IsNullOrEmpty(m.material) || !Hex(m.color)) errors.Add(w + ": bad material swap");
                foreach (var m in c.meshes) if (Array.IndexOf(Slots, m.slot) < 0 || string.IsNullOrEmpty(m.mesh)) errors.Add(w + ": bad mesh swap");
                foreach (var a in c.accessories)
                {
                    if (Array.IndexOf(AccessoryKinds, a.kind) < 0) errors.Add(w + ": unknown accessory " + a.kind);
                    if (!Hex(a.color) || (!string.IsNullOrEmpty(a.pattern) && !Hex(a.pattern))) errors.Add(w + ": accessory colour");
                }
                if (c.hero == "noura" && (!c.Has("abaya") || !c.Has("hijab"))) errors.Add(w + ": Noura always wears an abaya and a hijab");
                if (c.Has("agal") && !c.Has("shemagh") && !c.Has("ghutra") && !c.Has("keffiyeh") && !c.Has("yashmagh")) errors.Add(w + ": an agal sits on a shemagh, ghutra or keffiyeh");
                // Shipped files always say it (see the data tests); costumes built in code may leave it empty.
                if (!string.IsNullOrEmpty(c.agalMount) && c.agalMount != c.ExpectedAgalMount) errors.Add(w + ": agalMount must be " + c.ExpectedAgalMount);
                if (c.boredIdle != null && !string.IsNullOrEmpty(c.boredIdle.id))
                {
                    if (c.boredIdle.trigger < BoredCountryFirst) errors.Add(w + ": country bored idles use trigger " + BoredCountryFirst + " and up");
                    if (c.boredIdle.seconds <= 0.3f || c.boredIdle.seconds > 10f) errors.Add(w + ": bored idle 0.3..10 s");
                }
                if (c.IsReward && (c.isDefault || (c.unlock != "hard_all" && c.unlock != "gift"))) errors.Add(w + ": a reward costume is never the default and unlocks with hard_all or gift");
                if (c.isDefault) defaults[c.hero] = defaults.TryGetValue(c.hero, out int n) ? n + 1 : 1;
            }
            foreach (var h in Heroes)
            {
                defaults.TryGetValue(h, out int n);
                if (n != 1) errors.Add(file.country + ": needs exactly one default costume for " + h + ", has " + n);
            }
            if (file.country == "saudi") errors.AddRange(SaudiRules(file));
            return errors;
        }

        /// <summary>The Saudi look: white thobe, red shemagh, agal, brown closed leather shoes.</summary>
        private static IEnumerable<string> SaudiRules(CostumeFile file)
        {
            foreach (var c in file.costumes)
            {
                if (c.hero != "aron") continue;
                if (c.footwear != "leather_shoes") yield return "costume " + c.id + ": Saudi costumes wear leather shoes";
                if (c.isDefault)
                {
                    if (!c.Has("agal") || !c.Has("shemagh")) yield return "costume " + c.id + ": the default Saudi costume has the shemagh and agal";
                    bool redShemagh = false;
                    foreach (var a in c.accessories) if (a.kind == "shemagh" && IsReddish(a.color)) redShemagh = true;
                    if (!redShemagh) yield return "costume " + c.id + ": the default Saudi costume has the red shemagh";
                    bool whiteThobe = false;
                    foreach (var m in c.materials) if (m.material == "Clothes" && IsWhitish(m.color)) whiteThobe = true;
                    if (!whiteThobe) yield return "costume " + c.id + ": the default Saudi costume has a white thobe";
                }
            }
        }

        /// <summary>
        /// Where the agal is drawn. It is only ever in one place: while Aron swings it the head or wrist one hides.
        /// Outfits without a headdress agal wear it coiled on the right wrist between swings; it unwinds on attack
        /// and snaps back after.
        /// </summary>
        public static AgalSpot Agal(bool costumeHasAgal, bool weaponIsAgal, bool attacking)
        {
            if (weaponIsAgal && attacking) return AgalSpot.Hand;
            if (costumeHasAgal) return AgalSpot.Head;
            return weaponIsAgal ? AgalSpot.Wrist : AgalSpot.None;
        }

        /// <summary>
        /// Picks the costume to wear: the wardrobe pick if it's unlocked, then the stage's, then the region's,
        /// then the country default. Ids that don't exist are skipped.
        /// </summary>
        public static CostumeDefinition Pick(IReadOnlyList<CostumeDefinition> all, string hero, string country, string regionCostume,
                                             string stageCostume, string wardrobePick, ICollection<string> owned)
        {
            CostumeDefinition Find(string id)
            {
                if (string.IsNullOrEmpty(id)) return null;
                string want = ForHero(id, hero);
                foreach (var c in all) if (c.id == want && c.hero == hero) return c;
                return null;
            }
            var pick = Find(wardrobePick);
            if (pick != null && ((pick.price == 0 && !pick.IsReward) || (owned != null && owned.Contains(pick.id)))) return pick;
            return Find(stageCostume) ?? Find(regionCostume) ?? Default(all, hero, country) ?? Default(all, hero, "saudi");
        }

        /// <summary>
        /// Region and stage data name Aron's variant (saudi_hijazi); Noura's matching one is saudi_noura_hijazi.
        /// </summary>
        public static string ForHero(string id, string hero)
        {
            if (hero != "noura" || id.Contains("_noura")) return id;
            int cut = id.IndexOf('_');
            return cut < 0 ? id + "_noura" : id.Substring(0, cut) + "_noura" + id.Substring(cut);
        }

        public static CostumeDefinition Default(IReadOnlyList<CostumeDefinition> all, string hero, string country)
        {
            foreach (var c in all) if (c.country == country && c.hero == hero && c.isDefault) return c;
            return null;
        }

        private static bool IsReddish(string hex) => Rgb(hex, out int r, out int g, out int b) && r > 150 && g < 90 && b < 90;
        private static bool IsWhitish(string hex) => Rgb(hex, out int r, out int g, out int b) && r > 225 && g > 225 && b > 215;

        private static bool Rgb(string hex, out int r, out int g, out int b)
        {
            r = g = b = 0;
            if (!Hex(hex)) return false;
            r = Convert.ToInt32(hex.Substring(1, 2), 16);
            g = Convert.ToInt32(hex.Substring(3, 2), 16);
            b = Convert.ToInt32(hex.Substring(5, 2), 16);
            return true;
        }

        private static bool Hex(string s)
        {
            if (s == null || s.Length != 7 || s[0] != '#') return false;
            for (int i = 1; i < 7; i++) if (!Uri.IsHexDigit(s[i])) return false;
            return true;
        }
    }

    public enum AgalSpot { None, Head, Hand, Wrist }
}
