// (c) Ahmed Alamrani. All rights reserved.
using System;
using System.Collections.Generic;
using Aron.Logic.Regions;

namespace Aron.Logic.Content
{
    /// <summary>
    /// Which music ships inside the app and which is downloaded on demand, one group per country.
    /// The base install carries the menus, the stage-type tracks, the stingers and Riyadh/Diriyah's theme; every other regional or
    /// country theme belongs to the group of the country that uses it (<c>country_egypt</c>, <c>country_saudi</c>, ...).
    /// </summary>
    public sealed class ContentMap
    {
        public const string GroupPrefix = "country_";

        private readonly Dictionary<string, string> trackToGroup = new Dictionary<string, string>(StringComparer.Ordinal);
        private readonly SortedDictionary<string, List<string>> groupTracks = new SortedDictionary<string, List<string>>(StringComparer.Ordinal);
        private readonly List<string> problems = new List<string>();

        /// <summary>Mistakes in the data (a track used by two countries, a region with no country). Empty when the map is sound.</summary>
        public IReadOnlyList<string> Problems => problems;

        public static string GroupId(string country) => GroupPrefix + country;

        /// <summary>True for the tracks that are part of the base install.</summary>
        public static bool IsBaseTrack(string track)
        {
            if (string.IsNullOrEmpty(track)) return true;
            foreach (var g in MusicCues.Generic) if (g == track) return true;
            return track == MusicCues.SaudiNajd;
        }

        public static ContentMap Build(RegionCatalog catalog)
        {
            var map = new ContentMap();
            foreach (var r in catalog.regions)
            {
                string country = string.IsNullOrEmpty(r.country) ? r.id : r.country;
                map.Assign(r.music, country, r.id);
                foreach (var s in r.stages) map.Assign(s.music, country, s.id);
            }
            return map;
        }

        private void Assign(string track, string country, string where)
        {
            if (IsBaseTrack(track)) return;
            string group = GroupId(country);
            if (trackToGroup.TryGetValue(track, out var existing))
            {
                if (existing != group) problems.Add(where + ": " + track + " is used by " + existing + " and " + group);
                return;
            }
            trackToGroup[track] = group;
            if (!groupTracks.TryGetValue(group, out var list)) groupTracks[group] = list = new List<string>();
            list.Add(track);
        }

        /// <summary>Every downloadable group id, sorted.</summary>
        public IReadOnlyCollection<string> Groups => groupTracks.Keys;

        public IReadOnlyList<string> TracksOf(string group) => groupTracks.TryGetValue(group, out var l) ? l : (IReadOnlyList<string>)Array.Empty<string>();

        /// <summary>The group a track is downloaded with, or null when it is in the base install.</summary>
        public string GroupOfTrack(string track) => !string.IsNullOrEmpty(track) && trackToGroup.TryGetValue(track, out var g) ? g : null;

        /// <summary>The groups a stage needs before it plays with its own music: its track and, for a boss, the boss track.</summary>
        public string[] GroupsForStage(StageDef stage, RegionDef region)
        {
            var set = new SortedSet<string>(StringComparer.Ordinal);
            var a = GroupOfTrack(MusicCues.ForStage(stage, region)); if (a != null) set.Add(a);
            if (stage != null && (stage.boss || stage.type == "boss"))
            {
                var b = GroupOfTrack(MusicCues.ForBoss(stage)); if (b != null) set.Add(b);
            }
            var arr = new string[set.Count];
            set.CopyTo(arr);
            return arr;
        }

        /// <summary>What plays while a track's group is not on the device: the track for the stage type, else Riyadh's theme.</summary>
        public static string Fallback(string track)
        {
            if (track == MusicCues.TabukBoss) return MusicCues.StageBoss;
            return MusicCues.SaudiNajd;
        }

        /// <summary>
        /// Groups to fetch ahead of time (on Wi-Fi, from the map, never in a stage): those the frontier stage and the next few stages need
        /// and the stage the player is in right now does not.
        /// </summary>
        public string[] GroupsToPrefetch(RegionCatalog catalog, StageDef frontier, int lookahead)
        {
            var set = new SortedSet<string>(StringComparer.Ordinal);
            var stage = frontier;
            for (int i = 0; i <= lookahead && stage != null; i++)
            {
                catalog.Stage(stage.id, out int r, out _);
                if (r >= 0) foreach (var g in GroupsForStage(stage, catalog.regions[r])) set.Add(g);
                stage = catalog.Next(stage.id);
            }
            var arr = new string[set.Count];
            set.CopyTo(arr);
            return arr;
        }
    }
}
