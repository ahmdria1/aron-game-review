// (c) Ahmed Alamrani. All rights reserved.
using Aron.Core;
using Aron.Logic.Regions;
using Aron.Save;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Aron.Stages
{
    /// <summary>Which region stage is being played, and the catalog it came from.</summary>
    public static class StageSession
    {
        private static RegionCatalog catalog;

        public static RegionCatalog Catalog
        {
            get
            {
                if (catalog != null) return catalog;
                // One file per region, so regions can be added without touching each other's data.
                var files = Resources.LoadAll<TextAsset>("Regions");
                var defs = new System.Collections.Generic.List<RegionDef>();
                foreach (var f in files)
                {
                    try { defs.Add(JsonUtility.FromJson<RegionDef>(f.text)); }
                    catch (System.Exception e) { global::Aron.Logic.Diagnostics.Log.Warn("[Regions] " + f.name + ": " + e.Message); }
                }
                catalog = RegionCatalog.FromRegions(defs);
#if UNITY_EDITOR || DEVELOPMENT_BUILD
                foreach (var e in catalog.Validate()) global::Aron.Logic.Diagnostics.Log.Warn("[Regions] " + e);
#endif
                return catalog;
            }
        }

        public static StageDef Stage { get; private set; }
        public static RegionDef Region { get; private set; }
        public static int RegionIndex { get; private set; } = -1;
        public static int StageIndex { get; private set; } = -1;
        public static bool Active => Stage != null;
        private static string trailCountry, trailVfx = "";

        /// <summary>The current country's attack trail id from Resources/Culture/&lt;country&gt;.json, empty when unknown.</summary>
        public static string TrailVfx
        {
            get
            {
                string country = Region != null ? Region.country : "";
                if (country == trailCountry) return trailVfx;
                trailCountry = country;
                trailVfx = "";
                var asset = string.IsNullOrEmpty(country) ? null : UnityEngine.Resources.Load<UnityEngine.TextAsset>("Culture/" + country);
                if (asset != null)
                {
                    var f = UnityEngine.JsonUtility.FromJson<Aron.Logic.Culture.CultureFile>(asset.text);
                    if (f != null && f.attackTrailVfx != null) trailVfx = f.attackTrailVfx;
                }
                return trailVfx;
            }
        }

        /// <summary>Set when leaving a stage for the menu, so the menu opens on the map.</summary>
        public static bool OpenMap { get; set; }

        /// <summary>Running count of the stage across all regions, from 1. Used as the level number.</summary>
        public static int Number
        {
            get
            {
                int n = 0;
                for (int r = 0; r < RegionIndex; r++) n += Catalog.regions[r].stages.Length;
                return n + StageIndex + 1;
            }
        }

        public static bool Select(string stageId)
        {
            var s = Catalog.Stage(stageId, out int r, out int i);
            if (s == null) return false;
            Stage = s; Region = Catalog.regions[r]; RegionIndex = r; StageIndex = i;
            return true;
        }

        public static void Clear()
        {
            Stage = null; Region = null; RegionIndex = StageIndex = -1;
        }

        public static void Play(string stageId)
        {
            if (!Select(stageId)) return;
            // Country music that is not on the phone yet goes through the progress screen first (Wi-Fi only unless the player allows mobile data).
            // The screen is skippable and the stage then plays with the base music, so nothing here can block a stage for good.
            var missing = Aron.Content.ContentService.CanDownload ? Aron.Content.ContentService.MissingFor(Stage, Region) : System.Array.Empty<string>();
            if (missing.Length > 0)
            {
                Aron.Content.ContentProgressScreen.Open(null, missing, () => Begin(stageId), Clear);
                return;
            }
            Begin(stageId);
        }

        private static void Begin(string stageId)
        {
            if (Stage == null && !Select(stageId)) return;
            Aron.Content.ContentService.StageStarting();
            Diagnostics.CrashReporter.Crumb("stage:" + stageId);
            SaveSystem.Data.lastStageId = stageId;
            Aron.Telemetry.TelemetryService.StageStart(stageId);
            SaveSystem.Save();
            Time.timeScale = 1f;
            Aron.Core.SceneLoader.Load(SceneNames.Stage);
        }

        public static StageDef NextStage => Active ? Catalog.Next(Stage.id) : null;

        public static bool NextUnlocked
        {
            get { var n = NextStage; return n != null && StageProgress.IsUnlocked(Catalog, SaveSystem.Data, n.id); }
        }

        /// <summary>Called by the game manager when the goal is reached.</summary>
        public static void RecordFinish(float seconds, int dates) => RecordFinish(seconds, dates, -1, -1);

        public struct StarResult { public int stars, newStars; public bool hidden, beatPar; public float par; }

        /// <summary>Records a run and its stars: finished, every golden box found, under par.</summary>
        public static StarResult RecordFinish(float seconds, int dates, int boxes, int bisht)
        {
            if (!Active) return default;
            var data = SaveSystem.Data;
            StageProgress.Finish(data, Stage.id, seconds, dates, boxes, bisht);
            var layout = StageBuilder.Current != null ? StageBuilder.Current.Layout : StageLayout.Generate(Stage);
            float par = StageTiming.ParSeconds(Stage, layout);
            int total = layout.Count(PieceKind.CrateBisht);
            int added = StageStars.Record(data, Stage.id, seconds, par, bisht, total);
            return new StarResult { stars = StageStars.Count(StageProgress.Get(data, Stage.id)), newStars = added, hidden = StageStars.FoundHidden(bisht, total), beatPar = StageStars.BeatPar(seconds, par), par = par };
        }

        /// <summary>The track a stage starts with (see <see cref="Aron.Logic.MusicCues.ForStage"/>).</summary>
        public static string MusicFor(StageDef s, RegionDef r) => Aron.Logic.MusicCues.ForStage(s, r);

        /// <summary>The track for the boss fight at the end of a stage.</summary>
        public static string BossMusicFor(StageDef s) => Aron.Logic.MusicCues.ForBoss(s);

        public static Color Hex(string hex, Color fallback)
            => ColorUtility.TryParseHtmlString(hex, out var c) ? c : fallback;
    }
}
