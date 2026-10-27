// (c) Ahmed Alamrani. All rights reserved.
using System;
using System.Collections.Generic;
using Aron.Logic.Culture;
using Aron.Logic.Regions;

namespace Aron.Logic.Profile
{
    /// <summary>A food buff running in the current stage.</summary>
    public class ActiveBuff
    {
        public string stat;
        public float bonus;
        public float secondsLeft;
    }

    /// <summary>
    /// Local food from the country's stall: bought with coins, carried (five stacks at most, nine a stack),
    /// eaten for a short boost. Never on hard, never in a boss fight, never lives or progress.
    /// </summary>
    public static class FoodBuffs
    {
        public static readonly string[] Kinds = { "strength", "speed", "energy", "magnet" };
        public const int MaxStacks = 5, MaxPerStack = 9;
        public const float Seconds = 60f, Bonus = 0.15f;

        public enum Result { Ok, NotLocal, Poor, Full, HardMode, BossFight, NoneCarried }

        /// <summary>The stall only stocks the country's own foods.</summary>
        public static Food[] Stock(CultureFile country) => country?.foods ?? Array.Empty<Food>();

        public static int Carried(SaveData d, string foodId)
        {
            foreach (var e in d.carriedFoods ?? new List<string>())
                if (e.StartsWith(foodId + "=", StringComparison.Ordinal) && int.TryParse(e.Substring(foodId.Length + 1), out int n)) return n;
            return 0;
        }

        private static void Set(SaveData d, string foodId, int n)
        {
            if (d.carriedFoods == null) d.carriedFoods = new List<string>();
            d.carriedFoods.RemoveAll(e => e.StartsWith(foodId + "=", StringComparison.Ordinal));
            if (n > 0) d.carriedFoods.Add(foodId + "=" + n);
        }

        public static Result Buy(SaveData d, CultureFile stall, string foodId)
        {
            Food food = null;
            foreach (var f in Stock(stall)) if (f.id == foodId) food = f;
            if (food == null) return Result.NotLocal;
            int have = Carried(d, foodId);
            if (have == 0 && (d.carriedFoods?.Count ?? 0) >= MaxStacks) return Result.Full;
            if (have >= MaxPerStack) return Result.Full;
            if (d.coins < food.price) return Result.Poor;
            d.coins -= food.price;
            Set(d, foodId, have + 1);
            return Result.Ok;
        }

        public static Result CanEat(SaveData d, string foodId, bool bossFight)
        {
            if (d.difficulty == "hard") return Result.HardMode;
            if (bossFight) return Result.BossFight;
            if (Carried(d, foodId) <= 0) return Result.NoneCarried;
            return Result.Ok;
        }

        /// <summary>Eats one carried food and returns its buff, or null with the reason.</summary>
        public static ActiveBuff Eat(SaveData d, Food food, bool bossFight, out Result why)
        {
            why = CanEat(d, food.id, bossFight);
            if (why != Result.Ok) return null;
            Set(d, food.id, Carried(d, food.id) - 1);
            Culture.FoodAlbum.Collect(d, food.id);
            return new ActiveBuff { stat = food.buff, bonus = Bonus, secondsLeft = Seconds };
        }
    }

    public class StatBar
    {
        public string stat;
        public float baseValue, bonus, bonusSeconds;
        public float Total => Math.Min(1f, baseValue + bonus);
    }

    /// <summary>Everything the profile panel shows, worked out from the save and the game data.</summary>
    public class ProfileStats
    {
        public static readonly string[] Stats = { "strength", "speed", "energy", "shield", "magnet" };

        public int level, xp, xpIntoLevel, xpForNext;
        public readonly List<StatBar> bars = new List<StatBar>();
        public bool buffsOff;
        public int countriesDone, countriesTotal, stagesDone, stagesTotal, bossesBeaten, foodsHave, foodsTotal, costumesOwned, secrets;
        public float mapPercent, albumPercent;
        public int gold, silver, bronze;

        /// <summary>XP: 100 a stage, 60 a race finished, 20 a food card. Level n needs 100·n² XP in total.</summary>
        public static int Xp(SaveData d, RegionCatalog catalog)
        {
            int xp = 0;
            foreach (var r in catalog.regions) foreach (var s in r.stages) if (StageProgress.IsDone(d, s.id)) xp += s.IsRace ? 60 : 100;
            xp += 20 * (d.foodsCollected?.Count ?? 0);
            return xp;
        }

        public static int LevelFor(int xp) => 1 + (int)Math.Floor(Math.Sqrt(Math.Max(0, xp) / 100.0));

        /// <summary>Base stats come from the costume upgrades and companions; buffs show on top unless they are off.</summary>
        public static ProfileStats Build(SaveData d, RegionCatalog catalog, IEnumerable<CultureFile> cultures, IDictionary<string, float> baseStats,
            IEnumerable<ActiveBuff> buffs, bool bossFight)
        {
            var p = new ProfileStats();
            p.xp = Xp(d, catalog);
            p.level = LevelFor(p.xp);
            int floor = 100 * (p.level - 1) * (p.level - 1), next = 100 * p.level * p.level;
            p.xpIntoLevel = p.xp - floor; p.xpForNext = next - floor;
            p.buffsOff = d.difficulty == "hard" || bossFight;
            foreach (var st in Stats)
            {
                var bar = new StatBar { stat = st, baseValue = baseStats != null && baseStats.TryGetValue(st, out var v) ? v : 0.5f };
                if (!p.buffsOff && buffs != null)
                    foreach (var b in buffs) if (b.stat == st && b.secondsLeft > 0f) { bar.bonus += b.bonus; bar.bonusSeconds = Math.Max(bar.bonusSeconds, b.secondsLeft); }
                p.bars.Add(bar);
            }
            var countries = new Dictionary<string, bool>();
            foreach (var r in catalog.regions)
            {
                string c = ProgressRing.CountryOf(r);
                bool regionDone = true;
                foreach (var s in r.stages)
                {
                    p.stagesTotal++;
                    if (StageProgress.IsDone(d, s.id)) { p.stagesDone++; if (s.boss) p.bossesBeaten++; }
                    else regionDone = false;
                }
                countries[c] = (!countries.TryGetValue(c, out var prev) || prev) && regionDone;
            }
            p.countriesTotal = countries.Count;
            foreach (var kv in countries) if (kv.Value) p.countriesDone++;
            p.mapPercent = p.stagesTotal == 0 ? 0f : 100f * p.stagesDone / p.stagesTotal;
            p.foodsHave = FoodAlbum.Count(d, cultures ?? Array.Empty<CultureFile>(), out p.foodsTotal);
            p.albumPercent = p.foodsTotal == 0 ? 0f : 100f * p.foodsHave / p.foodsTotal;
            p.costumesOwned = d.ownedCostumes?.Count ?? 0;
            p.secrets = d.secretsFound?.Count ?? 0;
            foreach (var m in d.raceMedals ?? new List<string>())
            {
                if (m.EndsWith("=1", StringComparison.Ordinal)) p.gold++;
                else if (m.EndsWith("=2", StringComparison.Ordinal)) p.silver++;
                else if (m.EndsWith("=3", StringComparison.Ordinal)) p.bronze++;
            }
            return p;
        }

        /// <summary>Keeps the best medal per track: 1 gold, 2 silver, 3 bronze.</summary>
        public static void Medal(SaveData d, string track, int place)
        {
            if (place < 1 || place > 3) return;
            if (d.raceMedals == null) d.raceMedals = new List<string>();
            foreach (var m in d.raceMedals)
                if (m.StartsWith(track + "=", StringComparison.Ordinal) && int.TryParse(m.Substring(track.Length + 1), out int had) && had <= place) return;
            d.raceMedals.RemoveAll(m => m.StartsWith(track + "=", StringComparison.Ordinal));
            d.raceMedals.Add(track + "=" + place);
        }
    }

    /// <summary>Friendship with the companions, raised by feeding and petting in the idles. Purely cosmetic.</summary>
    public static class Friendship
    {
        public const int Max = 100, Feed = 5, Pet = 2;

        public static int Of(SaveData d, string companion)
        {
            foreach (var e in d.companionLove ?? new List<string>())
                if (e.StartsWith(companion + "=", StringComparison.Ordinal) && int.TryParse(e.Substring(companion.Length + 1), out int n)) return n;
            return 0;
        }

        public static int Add(SaveData d, string companion, int amount)
        {
            int n = Math.Max(0, Math.Min(Max, Of(d, companion) + amount));
            if (d.companionLove == null) d.companionLove = new List<string>();
            d.companionLove.RemoveAll(e => e.StartsWith(companion + "=", StringComparison.Ordinal));
            d.companionLove.Add(companion + "=" + n);
            return n;
        }
    }

    /// <summary>How the panel is laid out and behaves: it pauses a stage, and mirrors for right-to-left languages.</summary>
    public static class ProfilePanel
    {
        public static readonly string[] HeaderOrder = { "portrait", "name", "home_badge", "level" };
        public static readonly string[] Tabs = { "wallet", "power", "progress", "races", "badges", "companions", "inventory" };

        public static bool PausesGame(bool inStage) => inStage;

        /// <summary>Left-to-right order of the header pieces; reversed for Arabic and Urdu.</summary>
        public static string[] Header(string language)
        {
            var a = (string[])HeaderOrder.Clone();
            if (Localization.UiLanguages.IsRtl(language)) Array.Reverse(a);
            return a;
        }

        /// <summary>Text alignment for the panel: right for right-to-left languages.</summary>
        public static string Align(string language) => Localization.UiLanguages.IsRtl(language) ? "right" : "left";
    }
}
