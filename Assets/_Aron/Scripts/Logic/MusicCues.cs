// (c) Ahmed Alamrani. All rights reserved.
using System;
using Aron.Logic.Regions;

namespace Aron.Logic
{
    /// <summary>
    /// Names of every music track and which one plays where. Files live in Resources/Audio/Music/&lt;id&gt;.
    /// A normal stage plays its region's theme (a country theme, or one of five Saudi regional themes); chase, action and race
    /// stages play the track for their type; the boss arena plays the boss track unless the stage names its own.
    /// A stage can name its own track in the region file (<see cref="StageDef.music"/>), which wins over the type track.
    /// </summary>
    public static class MusicCues
    {
        public const string Menu = "music_menu";
        public const string Map = "music_map";
        public const string Store = "music_store";
        public const string Victory = "music_victory";
        public const string Defeat = "music_defeat";
        public const string Invincible = "music_invincible";

        public const string StageChase = "music_stage_chase";
        public const string StageAction = "music_stage_action";
        public const string StageRace = "music_stage_race";
        public const string StageBoss = "music_stage_boss";

        public const string SaudiNajd = "music_saudi_najd";
        public const string SaudiHejaz = "music_saudi_hejaz";
        public const string SaudiSouth = "music_saudi_south";
        public const string SaudiEastern = "music_saudi_eastern";
        public const string SaudiNorth = "music_saudi_north";

        public const string TabukBoss = "music_tabuk_boss";
        public const string KuwaitSea = "music_kuwait_sea";

        public const string CountryPrefix = "music_country_";

        /// <summary>The theme of a country other than Saudi Arabia (whose regions each have their own).</summary>
        public static string Country(string countryId) => CountryPrefix + countryId;

        public static readonly string[] Saudi = { SaudiNajd, SaudiHejaz, SaudiSouth, SaudiEastern, SaudiNorth };

        public static readonly string[] Generic =
        {
            Menu, Map, Store, Victory, Defeat, Invincible, StageChase, StageAction, StageRace, StageBoss,
        };

        /// <summary>The track a stage starts with: its own, else the one for its type, else its region's theme.</summary>
        public static string ForStage(StageDef stage, RegionDef region)
        {
            if (stage == null) return region != null && !string.IsNullOrEmpty(region.music) ? region.music : SaudiNajd;
            bool boss = stage.boss || stage.type == "boss";
            if (!boss && !string.IsNullOrEmpty(stage.music)) return stage.music;
            switch (stage.type)
            {
                case "chase": return StageChase;
                case "action": return StageAction;
                case "race": return StageRace;
            }
            return region != null && !string.IsNullOrEmpty(region.music) ? region.music : SaudiNajd;
        }

        /// <summary>The track for the boss fight of a stage: the stage's own, else the shared boss track.</summary>
        public static string ForBoss(StageDef stage) => stage != null && !string.IsNullOrEmpty(stage.music) ? stage.music : StageBoss;

        /// <summary>Every track id the game can ask for, for the file checks.</summary>
        public static string[] All(RegionCatalog catalog)
        {
            var set = new System.Collections.Generic.SortedSet<string>(StringComparer.Ordinal);
            foreach (var g in Generic) set.Add(g);
            foreach (var r in catalog.regions)
            {
                if (!string.IsNullOrEmpty(r.music)) set.Add(r.music);
                foreach (var s in r.stages) { if (!string.IsNullOrEmpty(s.music)) set.Add(s.music); }
            }
            var arr = new string[set.Count];
            set.CopyTo(arr);
            return arr;
        }
    }
}
