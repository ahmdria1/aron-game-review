// (c) Ahmed Alamrani. All rights reserved.
using System;
using System.Collections.Generic;

namespace Aron.Logic.Regions
{
    /// <summary>
    /// Desert-rose crystals and time medals (ميداليات الزمن). A crystal for breaking every box in a stage, an
    /// eight-point star per country that opens its secret path, and a dallah or falcon time medal for beating the
    /// stage's time (bronze, silver, gold, platinum).
    /// </summary>
    public static class Secrets
    {
        /// <summary>Our own colour names for the eight-point stars.</summary>
        public static readonly string[] Colours = { "saffron", "palm", "gulf", "dune", "henna" };
        public static readonly string[] Medals = { "", "bronze", "silver", "gold", "platinum" };

        public static string BoxGemId(string stageId) => "crystal_" + stageId;
        public static string ColourGemId(string country) => "star_" + country;
        public static string SecretStageId(string country) => country + "_secret";

        /// <summary>The coloured gem of a country, the same every time.</summary>
        public static string ColourOf(string country)
        {
            int h = 0;
            foreach (char c in country ?? "") h = unchecked(h * 31 + c);
            return Colours[(h & 0x7fffffff) % Colours.Length];
        }

        /// <summary>Breaking every box gives the stage gem (once) and adds to the gem count the travel tickets use.</summary>
        public static bool AwardBoxGem(SaveData d, string stageId, int broken, int total)
        {
            if (total <= 0 || broken < total) return false;
            if (d.secretsFound == null) d.secretsFound = new List<string>();
            string id = BoxGemId(stageId);
            if (d.secretsFound.Contains(id)) return false;
            d.secretsFound.Add(id);
            TravelTickets.AddGems(d, 1);
            return true;
        }

        public static bool SecretPathOpen(SaveData d, string country) => d.secretsFound != null && d.secretsFound.Contains(ColourGemId(country));

        /// <summary>Time medal: platinum under 75 % of the target, gold under 85 %, silver under 95 %, bronze under the target.</summary>
        public static string TimeMedal(float seconds, float target)
        {
            if (seconds <= 0f || target <= 0f) return "";
            if (seconds <= target * 0.75f) return "platinum";
            if (seconds <= target * 0.85f) return "gold";
            if (seconds <= target * 0.95f) return "silver";
            if (seconds <= target) return "bronze";
            return "";
        }

        /// <summary>The time-medal target for a stage: 70 % of the layout's estimated time, rounded to 5 s.</summary>
        public static float MedalTarget(StageDef s) => (float)Math.Round(StageLayout.Generate(s).EstimatedSeconds * 0.7f / 5f) * 5f;
    }
}
