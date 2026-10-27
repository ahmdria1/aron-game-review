// (c) Ahmed Alamrani. All rights reserved.
using System;
using System.Collections.Generic;

namespace Aron.Logic.Races
{
    /// <summary>The four numbers a racing camel is made of. 1 is the baseline camel; the free breed sits at 1 on all of them.</summary>
    [Serializable]
    public class CamelStats
    {
        /// <summary>Top speed on the straights, how fast it gets there, how long it holds its pace, how well it takes a bend.</summary>
        public float topSpeed = 1f, accel = 1f, stamina = 1f, handling = 1f;

        public CamelStats Copy() => (CamelStats)MemberwiseClone();
        public CamelStats Scaled(float k) => new CamelStats { topSpeed = topSpeed * k, accel = accel * k, stamina = stamina * k, handling = handling * k };
    }

    /// <summary>One real Saudi / Gulf camel type (Resources/Races/camel_race.json). The names live in the UI string tables under <see cref="NameKey"/>.</summary>
    [Serializable]
    public class CamelBreed
    {
        public string id, nameAr, nameEn;
        /// <summary>The other Arabic name people use for the type, when there is one (the white type is called المغاتير and الوضح).</summary>
        public string aliasAr = "";
        /// <summary>Coat colour and the colour of the saddle cloth.</summary>
        public string coat = "#FFFFFF", accent = "#222222";
        public CamelStats stats = new CamelStats();
        /// <summary>Direct price tier in US dollars. 0 only for the free breed.</summary>
        public float usd;
        public string productId = "";
        public bool isDefault;

        /// <summary>The store item id ("breed_sufr"). The product id is "com.ahmdria1.aron." + this.</summary>
        public string ItemId => "breed_" + id;
        /// <summary>UI string keys: the name and the one-line style.</summary>
        public string NameKey => "rbreed_" + id;
        public string StyleKey => "breed_style_" + id;
    }

    /// <summary>A cup, named after the age group of the camels that run in it. Harder from the first to the last.</summary>
    [Serializable]
    public class CamelAgeClass
    {
        public string id, nameAr, nameEn;
        /// <summary>1 (حقايق) to 5 (حيل).</summary>
        public int tier = 1;
        /// <summary>Rival top speed as a share of the player's, the catch-up and hold-back share, and the gap (metres) where they are full.</summary>
        public float rivalTop = 1f, catchUp = 0.1f, holdBack = 0.1f, fullGap = 80f;
        /// <summary>Metres of the run and the number of bends on it.</summary>
        public float length = 1000f;
        public int bends = 2;
        /// <summary>Breed ids the five robot-ridden rivals are drawn from.</summary>
        public string[] rivalBreeds = Array.Empty<string>();
        /// <summary>Class id whose podium opens this one (empty for the first).</summary>
        public string unlockedBy = "";

        public string NameKey => "camel_class_" + id;
    }

    /// <summary>The playful, Crash-style numbers: boost pads, dune jumps, comic stumbles and speed lines. Never hurts anyone.</summary>
    [Serializable]
    public class CamelArcade
    {
        public float boostPadsPer1000 = 8f, duneJumpsPer1000 = 4f;
        public float boostSeconds = 2.2f, boostMultiplier = 1.25f, boostStamina = 0.02f;
        public float stumbleSeconds = 0.9f, stumbleSpeedLoss = 0.35f;
        /// <summary>Speed lines start at this share of top speed and reach full at 1.</summary>
        public float speedLinesFrom = 0.6f;
        public float jumpAirSeconds = 0.8f;
    }

    /// <summary>
    /// How the camel race looks. The art follows this data, so the style can change without code: "cartoon" is the fun one that is shipped,
    /// "storybook" is a calmer alternative kept for the final decision.
    /// </summary>
    [Serializable]
    public class CamelStyle
    {
        public string id;
        public bool outline = true;
        public float outlineWidth = 0.02f, squashStretch = 1f, saturation = 1f, speedLineAlpha = 0.8f;
    }

    /// <summary>The desert camel racetrack (ميدان هجن): a long sand strip with stands, palms and the road beside it where the cars follow.</summary>
    [Serializable]
    public class CamelVenue
    {
        public string id = "hijn_maydan", nameAr = "", nameEn = "";
        public string surface = "sand", time = "sunset", weather = "clear";
        public string[] landmarks = Array.Empty<string>();
    }

    [Serializable]
    public class CamelRaceFile
    {
        public int version = 1;
        /// <summary>Ranked races give every breed this much overall power, like the mounts.</summary>
        public float powerBudget = 7.5f, powerTolerance = 0.03f;
        /// <summary>Who rides the rival camels. Always a small robot, in every race and every mode; no child jockeys.</summary>
        public string rivalJockey = "robot";
        public string musicId = "music_race_camel";
        public string cameraMode = "camel_race";
        public CamelVenue venue = new CamelVenue();
        public string activeStyle = "cartoon";
        public CamelStyle[] styles = Array.Empty<CamelStyle>();
        public CamelArcade arcade = new CamelArcade();
        public CamelBreed[] breeds = Array.Empty<CamelBreed>();
        public CamelAgeClass[] classes = Array.Empty<CamelAgeClass>();

        public const float WTop = 3f, WAccel = 1.5f, WStamina = 1.5f, WHandling = 1.5f;
        public static readonly float[] PriceTiers = { 1.99f, 2.99f, 3.99f };
        public const string ProductPrefix = "com.ahmdria1.aron.";

        public CamelBreed Find(string id) { foreach (var b in breeds) if (b != null && b.id == id) return b; return null; }
        public CamelBreed Default { get { foreach (var b in breeds) if (b != null && b.isDefault) return b; return breeds.Length > 0 ? breeds[0] : null; } }
        public CamelAgeClass Class(string id) { foreach (var c in classes) if (c != null && c.id == id) return c; return null; }
        public CamelStyle Style(string id) { foreach (var s in styles) if (s != null && s.id == id) return s; return null; }

        /// <summary>One number for how strong a breed is overall.</summary>
        public static float Power(CamelStats s) => WTop * s.topSpeed + WAccel * s.accel + WStamina * s.stamina + WHandling * s.handling;

        /// <summary>The stats a racer really gets. Ranked: every stat is scaled by one factor so the power equals the budget; the shape stays.</summary>
        public CamelStats StatsFor(CamelBreed b, RaceMode mode)
        {
            if (b == null) return new CamelStats();
            if (mode != RaceMode.Ranked) return b.stats.Copy();
            float p = Power(b.stats);
            return p <= 0f ? b.stats.Copy() : b.stats.Scaled(powerBudget / p);
        }

        /// <summary>True when a is at least as good as b on every stat and better on one: then nobody would ever pick b.</summary>
        public static bool Dominates(CamelStats a, CamelStats b)
        {
            bool ge = a.topSpeed >= b.topSpeed && a.accel >= b.accel && a.stamina >= b.stamina && a.handling >= b.handling;
            bool gt = a.topSpeed > b.topSpeed || a.accel > b.accel || a.stamina > b.stamina || a.handling > b.handling;
            return ge && gt;
        }

        public List<string> Validate()
        {
            var e = new List<string>();
            if (breeds == null || breeds.Length == 0) { e.Add("no breeds"); return e; }
            var ids = new HashSet<string>(); var products = new HashSet<string>();
            int defaults = 0;
            foreach (var b in breeds)
            {
                string w = "breed " + b.id;
                if (string.IsNullOrEmpty(b.id) || !ids.Add(b.id)) e.Add(w + ": missing or duplicate id");
                if (string.IsNullOrEmpty(b.nameAr) || string.IsNullOrEmpty(b.nameEn)) e.Add(w + ": names");
                if (!Culture.CultureRules.Hex(b.coat) || !Culture.CultureRules.Hex(b.accent)) e.Add(w + ": colours");
                var s = b.stats;
                if (s.topSpeed < 0.9f || s.topSpeed > 1.15f) e.Add(w + ": top speed 0.9..1.15");
                if (s.accel < 0.7f || s.accel > 1.4f) e.Add(w + ": accel 0.7..1.4");
                if (s.stamina < 0.55f || s.stamina > 1.5f) e.Add(w + ": stamina 0.55..1.5");
                if (s.handling < 0.8f || s.handling > 1.35f) e.Add(w + ": handling 0.8..1.35");
                if (Math.Abs(Power(s) - powerBudget) > powerBudget * powerTolerance) e.Add(w + ": power " + Power(s).ToString("0.00") + " is too far from the budget");
                if (b.isDefault)
                {
                    defaults++;
                    if (b.usd != 0f || !string.IsNullOrEmpty(b.productId)) e.Add(w + ": the default breed is free");
                }
                else
                {
                    if (Array.IndexOf(PriceTiers, b.usd) < 0) e.Add(w + ": price must be 1.99, 2.99 or 3.99");
                    if (b.productId != ProductPrefix + b.ItemId || !products.Add(b.productId)) e.Add(w + ": product id");
                }
            }
            if (defaults != 1) e.Add("exactly one free default breed");
            for (int i = 0; i < breeds.Length; i++)
                for (int j = 0; j < breeds.Length; j++)
                    if (i != j && Dominates(breeds[i].stats, breeds[j].stats)) e.Add(breeds[i].id + " is strictly better than " + breeds[j].id);
            if (rivalJockey != "robot") e.Add("rival jockeys are robots");
            if (string.IsNullOrEmpty(musicId) || string.IsNullOrEmpty(cameraMode) || venue == null || string.IsNullOrEmpty(venue.id)) e.Add("music, camera and venue ids");
            else if (Array.IndexOf(RaceFile.Surfaces, venue.surface) < 0 || string.IsNullOrEmpty(venue.nameAr) || string.IsNullOrEmpty(venue.nameEn)) e.Add("venue surface and names");
            if (Style(activeStyle) == null) e.Add("active style");
            if (classes == null || classes.Length != 5) { e.Add("five age classes"); return e; }
            string[] order = { "hiqaq", "laqaya", "jidhaa", "thanaya", "hayl" };
            for (int i = 0; i < classes.Length; i++)
            {
                var c = classes[i]; string w = "class " + c.id;
                if (c.id != order[i] || c.tier != i + 1) e.Add(w + ": order and tier");
                if (string.IsNullOrEmpty(c.nameAr) || string.IsNullOrEmpty(c.nameEn)) e.Add(w + ": names");
                if (c.rivalTop < 0.8f || c.rivalTop > 1.15f || c.length < 400f || c.bends < 0 || c.fullGap <= 0f) e.Add(w + ": numbers");
                if (i > 0 && (c.rivalTop <= classes[i - 1].rivalTop || c.length <= classes[i - 1].length || c.bends < classes[i - 1].bends)) e.Add(w + ": each cup is harder than the one before");
                if (c.unlockedBy != (i == 0 ? "" : classes[i - 1].id)) e.Add(w + ": unlocked by the cup before it");
                if (c.rivalBreeds == null || c.rivalBreeds.Length < 2) e.Add(w + ": rival breeds");
                else foreach (var r in c.rivalBreeds) if (Find(r) == null) e.Add(w + ": unknown rival breed " + r);
            }
            return e;
        }
    }

    /// <summary>The bounce, the boost and the stumble of the arcade layer. Pure numbers; the Unity side only plays them.</summary>
    public static class CamelArcadeRules
    {
        public static float BoostedTop(CamelArcade a, float top) => top * a.boostMultiplier;
        /// <summary>Speed right after a comic stumble. Nobody is hurt; the camel wobbles and carries on.</summary>
        public static float AfterStumble(CamelArcade a, float speed) => speed * (1f - a.stumbleSpeedLoss);
        /// <summary>0 .. 1 strength of the speed lines for a speed as a share of top speed.</summary>
        public static float SpeedLines(CamelArcade a, float speed01)
        {
            if (speed01 <= a.speedLinesFrom) return 0f;
            return Math.Min(1f, (speed01 - a.speedLinesFrom) / Math.Max(0.01f, 1f - a.speedLinesFrom));
        }
        public static int BoostPads(CamelArcade a, float length) => (int)Math.Round(a.boostPadsPer1000 * length / 1000f);
        public static int DuneJumps(CamelArcade a, float length) => (int)Math.Round(a.duneJumpsPer1000 * length / 1000f);
    }

    /// <summary>
    /// A small deterministic run of one camel over a distance, to show what each stat is worth: the fast type wins a sprint,
    /// the stayer wins a long race, the agile one wins on twisty ground. Used by the balance tests.
    /// </summary>
    public static class CamelSim
    {
        public const float BaseSpeed = 18f, Tau = 3f, StaminaMetres = 1500f, TiredRamp = 600f, BendZone = 30f, MaxBendLoss = 0.5f;

        /// <summary>How much of its top speed a camel keeps once its breath is gone: a long-breathed breed keeps more, a short-breathed one drops further.</summary>
        public static float TiredFactor(float stamina) => Math.Max(0.45f, Math.Min(0.85f, 0.35f + 0.35f * stamina));

        public static float Seconds(CamelStats s, float length, int bends)
        {
            float v = 0f, d = 0f, t = 0f, pool = StaminaMetres * s.stamina, tired = TiredFactor(s.stamina);
            const float dt = 0.1f;
            float gap = bends > 0 ? length / (bends + 1) : 0f;
            int guard = 0;
            while (d < length && guard++ < 200000)
            {
                float vmax = BaseSpeed * s.topSpeed;
                if (pool <= 0f) vmax *= 1f - (1f - tired) * Math.Min(1f, -pool / TiredRamp);
                for (int k = 1; k <= bends; k++)
                    if (Math.Abs(d - gap * k) < BendZone) { vmax *= 1f - Math.Max(0f, Math.Min(MaxBendLoss, MaxBendLoss * (1.25f - s.handling) / 0.5f)); break; }
                v += (vmax - v) * Math.Min(1f, s.accel / Tau) * dt;
                float step = v * dt;
                d += step; t += dt; pool -= step;
            }
            return t;
        }
    }
}
