// (c) Ahmed Alamrani. All rights reserved.
using System;
using System.Collections.Generic;

namespace Aron.Logic.Races
{
    /// <summary>One race track. Resources/Races/tracks.json; a race stage points at it with <c>StageDef.race</c>.</summary>
    [Serializable]
    public class RaceTrack
    {
        public string id;
        /// <summary>The region whose race stage runs on this track.</summary>
        public string region;
        public string nameAr, nameEn;
        /// <summary>
        /// What everyone rides: camel (a small robot jockey on each camel, the hero rides alongside), horse, car,
        /// ostrich, bull (pacu jawi pairs), boat (rowed or paddled), sail (dhow), ski, surf, sky (falcon-led glider),
        /// bicycle or motorcycle.
        /// </summary>
        public string kind;
        /// <summary>
        /// The shop class of vehicle the player races with here: mounts, sea, cars, bicycles, motorcycles, or gear (skis, a surfboard).
        /// Always the one <see cref="VehicleFile.ClassForKind"/> gives for the kind; <see cref="RaceFile.Validate"/> checks it.
        /// </summary>
        public string vehicleClass = "mounts";
        /// <summary>3 for a lap race, 0 for point to point.</summary>
        public int laps = 3;
        /// <summary>Metres per lap, or the whole run when point to point.</summary>
        public float length = 900f;
        /// <summary>sand, asphalt, savanna or air: what the track is built from.</summary>
        public string surface = "sand";
        public int boosts = 8, coins = 40, shields = 2;
        /// <summary>Leaderboard placeholder; the real ids come from Play Console / App Store Connect (see README).</summary>
        public string leaderboard;
        public string time = "day";
        /// <summary>
        /// The one weather this track always has, so every time on its leaderboard is set in the same conditions:
        /// clear, clouds, haze or snow (see <see cref="Sky.WeatherPolicy.RaceWeathers"/>). Dynamic weather never runs here.
        /// </summary>
        public string weather = "clear";
        public string[] landmarks = Array.Empty<string>();

        /// <summary>The real community tradition the race comes from (sources in docs/culture_sources.md).</summary>
        public string tradition = "";

        public float TotalLength => laps > 0 ? length * laps : length;
    }

    [Serializable]
    public class RacerStats
    {
        public string hero = "aron";
        /// <summary>Top speed share, cornering grip and extra boosts per race (Noura's handbag dash).</summary>
        public float top = 1f, grip = 0.85f;
        public int handbagBoosts;
    }

    /// <summary>How hard the five rivals push for one difficulty.</summary>
    [Serializable]
    public class RubberBand
    {
        public string difficulty;
        /// <summary>Rival top speed as a share of the player's top speed.</summary>
        public float rivalTop = 1f;
        /// <summary>Most a rival speeds up when far behind the player, and slows down when far ahead (shares of top speed).</summary>
        public float catchUp = 0.1f, holdBack = 0.1f;
        /// <summary>Metres of gap at which the catch-up or hold-back is full.</summary>
        public float fullGap = 80f;
        /// <summary>Finish places that count as a win (1..3). Easy also lets any finish through.</summary>
        public int passPlace = 3;
        public bool anyFinishPasses;
    }

    [Serializable]
    public class RaceFile
    {
        public const int Rivals = 5;
        public static readonly string[] Kinds = { "camel", "horse", "car", "ostrich", "bull", "boat", "sail", "ski", "surf", "sky", "bicycle", "motorcycle" };
        public static readonly string[] Surfaces = { "sand", "asphalt", "dirt", "grass", "savanna", "mud", "water", "snow", "air" };

        /// <summary>Who can race. Aron is faster on the straights, Noura corners better and has her handbag boost.</summary>
        public RacerStats[] racers = Array.Empty<RacerStats>();

        public RacerStats Racer(string hero)
        {
            foreach (var r in racers) if (r.hero == hero) return r;
            return racers.Length > 0 ? racers[0] : new RacerStats();
        }

        /// <summary>Which surfaces fit a kind of race.</summary>
        public static bool Fits(string kind, string surface)
        {
            switch (kind)
            {
                case "sky": return surface == "air";
                case "boat": case "sail": case "surf": return surface == "water";
                case "ski": return surface == "snow";
                case "bull": return surface == "mud";
                case "car": return surface == "sand" || surface == "asphalt" || surface == "dirt";
                case "bicycle": return surface == "asphalt" || surface == "dirt" || surface == "grass";
                case "motorcycle": return surface == "asphalt" || surface == "dirt" || surface == "sand";
                default: return surface == "sand" || surface == "dirt" || surface == "grass" || surface == "savanna";
            }
        }

        public RaceTrack[] tracks = Array.Empty<RaceTrack>();
        public RubberBand[] rubberBand = Array.Empty<RubberBand>();
        /// <summary>Speed share a boost pad gives, and for how long.</summary>
        public float boostSpeed = 1.35f, boostSeconds = 1.6f;
        /// <summary>Hits a Golden Bisht shield takes in a race (a hit otherwise slows the racer).</summary>
        public int shieldHits = 1;
        public float hitSlowSeconds = 1.2f;

        public RaceTrack Track(string id)
        {
            foreach (var t in tracks) if (t.id == id) return t;
            return null;
        }

        public RubberBand Band(string difficulty)
        {
            foreach (var b in rubberBand) if (b.difficulty == difficulty) return b;
            foreach (var b in rubberBand) if (b.difficulty == "normal") return b;
            return rubberBand.Length > 0 ? rubberBand[0] : new RubberBand();
        }

        public static string LeaderboardId(string trackId) => "aron_race_" + trackId;

        public List<string> Validate()
        {
            var e = new List<string>();
            var ids = new HashSet<string>();
            foreach (var t in tracks)
            {
                string w = "track " + t.id;
                if (string.IsNullOrEmpty(t.id) || !ids.Add(t.id)) e.Add(w + ": missing or duplicate id");
                if (string.IsNullOrEmpty(t.region)) e.Add(w + ": needs a region");
                if (string.IsNullOrEmpty(t.nameAr) || string.IsNullOrEmpty(t.nameEn)) e.Add(w + ": names");
                if (Array.IndexOf(Kinds, t.kind) < 0) e.Add(w + ": unknown kind " + t.kind);
                if (Array.IndexOf(VehicleFile.ClassIds, t.vehicleClass) < 0) e.Add(w + ": unknown vehicle class " + t.vehicleClass);
                else if (t.vehicleClass != VehicleFile.ClassForKind(t.kind)) e.Add(w + ": a " + t.kind + " race uses the " + VehicleFile.ClassForKind(t.kind) + " class");
                if (Array.IndexOf(Surfaces, t.surface) < 0) e.Add(w + ": unknown surface " + t.surface);
                if (!Fits(t.kind, t.surface)) e.Add(w + ": a " + t.kind + " race doesn't run on " + t.surface);
                if (string.IsNullOrEmpty(t.tradition)) e.Add(w + ": say which real tradition it comes from");
                if (t.laps != 0 && t.laps != 3) e.Add(w + ": 3 laps or point to point");
                if (t.length < 400f || t.length > 3000f) e.Add(w + ": lap length 400..3000 m");
                if (t.TotalLength > 4500f) e.Add(w + ": too long for a phone race");
                if (t.boosts < 3 || t.boosts > 20) e.Add(w + ": 3..20 boosts");
                if (t.shields < 1 || t.shields > 4) e.Add(w + ": 1..4 Golden Bisht shields");
                if (t.coins < 0 || t.coins > 100) e.Add(w + ": coins 0..100");
                if (t.leaderboard != LeaderboardId(t.id)) e.Add(w + ": leaderboard id must be " + LeaderboardId(t.id));
                if (Array.IndexOf(Regions.RegionCatalog.Times, t.time) < 0) e.Add(w + ": time");
                if (Array.IndexOf(Sky.WeatherPolicy.RaceWeathers, t.weather) < 0) e.Add(w + ": fixed weather must be one of " + string.Join(", ", Sky.WeatherPolicy.RaceWeathers));
                if (t.weather == "snow" && t.kind != "ski") e.Add(w + ": only ski races run in snow");
                foreach (var l in t.landmarks) if (Array.IndexOf(Regions.LandmarkKinds.All, l) < 0) e.Add(w + ": unknown landmark " + l);
            }
            foreach (var d in new[] { "easy", "normal", "hard" })
            {
                RubberBand b = null;
                foreach (var x in rubberBand) if (x.difficulty == d) b = x;
                if (b == null) { e.Add("rubber band missing for " + d); continue; }
                if (b.rivalTop < 0.7f || b.rivalTop > 1.1f) e.Add(d + ": rival top speed 0.7..1.1");
                if (b.catchUp < 0f || b.catchUp > 0.3f || b.holdBack < 0f || b.holdBack > 0.3f) e.Add(d + ": rubber band 0..0.3");
                if (b.fullGap < 10f) e.Add(d + ": full gap at least 10 m");
                if (b.passPlace < 1 || b.passPlace > 3) e.Add(d + ": pass place 1..3");
            }
            if (boostSpeed <= 1f || boostSpeed > 2f) e.Add("boost speed 1..2");
            foreach (var h in new[] { "aron", "noura" })
            {
                RacerStats r = null;
                foreach (var x in racers) if (x.hero == h) r = x;
                if (r == null) { e.Add("racer missing: " + h); continue; }
                if (r.top < 0.8f || r.top > 1.1f || r.grip < 0.6f || r.grip > 1.2f) e.Add(h + ": racer stats out of range");
                if (r.handbagBoosts < 0 || r.handbagBoosts > 3) e.Add(h + ": 0..3 handbag boosts");
            }
            var a = Racer("aron"); var n = Racer("noura");
            if (!(a.top > n.top)) e.Add("Aron is the faster racer");
            if (!(n.grip > a.grip) || n.handbagBoosts < 1) e.Add("Noura corners better and has her handbag boost");
            return e;
        }
    }

    public static class RaceRules
    {
        /// <summary>
        /// A rival's speed. Behind the player (gap &gt; 0 means the player is ahead) it speeds up, ahead it eases
        /// off, both growing with the gap until <see cref="RubberBand.fullGap"/>. Never above 1.25x player top.
        /// </summary>
        public static float RivalSpeed(RubberBand b, float playerTop, float rivalSkill, float gapToPlayer)
        {
            float t = Math.Max(-1f, Math.Min(1f, gapToPlayer / Math.Max(1f, b.fullGap)));
            float band = t > 0f ? t * b.catchUp : t * b.holdBack;
            float speed = playerTop * b.rivalTop * rivalSkill * (1f + band);
            return Math.Min(speed, playerTop * 1.25f);
        }

        /// <summary>Each rival's own skill, spread a little so they don't move as one pack. Same every run.</summary>
        public static float Skill(int rival) => 0.94f + 0.03f * (rival % RaceFile.Rivals);

        /// <summary>Does this finish open the next stage?</summary>
        public static bool Passes(RubberBand b, int place, bool finished)
        {
            if (!finished) return false;
            if (b.anyFinishPasses) return true;
            return place >= 1 && place <= b.passPlace;
        }

        /// <summary>1-based finish place from everyone's distance (or finish time, smaller first).</summary>
        public static int Place(float playerTime, IList<float> rivalTimes)
        {
            int place = 1;
            foreach (var t in rivalTimes) if (t < playerTime) place++;
            return place;
        }

        public static int Lap(RaceTrack t, float distance)
        {
            if (t.laps <= 0) return 1;
            return Math.Min(t.laps, (int)(distance / t.length) + 1);
        }

        public static bool Finished(RaceTrack t, float distance) => distance >= t.TotalLength;
    }

    /// <summary>The player's best run on a track, replayed as a ghost in time trial. Kept small: one sample every half second.</summary>
    [Serializable]
    public class RaceGhost
    {
        public const float Step = 0.5f;
        public const int MaxSamples = 900;

        public string track;
        public float time;
        /// <summary>Distance along the track at each step.</summary>
        public List<float> distance = new List<float>();
        /// <summary>Sideways offset at each step, metres from the centre line.</summary>
        public List<float> side = new List<float>();

        public void Record(float elapsed, float dist, float lateral)
        {
            int want = (int)(elapsed / Step);
            while (distance.Count <= want && distance.Count < MaxSamples) { distance.Add(dist); side.Add(lateral); }
        }

        public void Sample(float elapsed, out float dist, out float lateral)
        {
            dist = lateral = 0f;
            if (distance.Count == 0) return;
            float f = elapsed / Step;
            int i = Math.Min(distance.Count - 1, (int)f);
            int j = Math.Min(distance.Count - 1, i + 1);
            float k = Math.Min(1f, f - i);
            dist = distance[i] + (distance[j] - distance[i]) * k;
            lateral = side[i] + (side[j] - side[i]) * k;
        }
    }

    /// <summary>Best times and the Races menu list.</summary>
    public static class RaceProgress
    {
        /// <summary>Tracks the player has opened in the story: the race stage is unlocked.</summary>
        public static List<RaceTrack> Unlocked(RaceFile races, Regions.RegionCatalog catalog, SaveData data)
        {
            var list = new List<RaceTrack>();
            foreach (var r in catalog.regions)
                foreach (var s in r.stages)
                {
                    if (s.type != "race" || string.IsNullOrEmpty(s.race)) continue;
                    var t = races.Track(s.race);
                    if (t != null && (Regions.StageProgress.IsDone(data, s.id) || Regions.StageProgress.IsUnlocked(catalog, data, s.id))) list.Add(t);
                }
            return list;
        }

        /// <summary>Opened once means always in the Races menu, even if a travel pass later runs out.</summary>
        public static List<RaceTrack> Menu(RaceFile races, Regions.RegionCatalog catalog, SaveData data)
        {
            var list = Unlocked(races, catalog, data);
            foreach (var id in data.raceTracks ?? new List<string>())
            {
                var t = races.Track(id);
                if (t != null && !list.Contains(t)) list.Add(t);
            }
            return list;
        }

        public static float Best(SaveData data, string trackId)
        {
            foreach (var e in data.raceBest ?? new List<string>())
            {
                int eq = e.IndexOf('=');
                if (eq > 0 && e.Substring(0, eq) == trackId &&
                    float.TryParse(e.Substring(eq + 1), System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out float v)) return v;
            }
            return 0f;
        }

        /// <summary>Stores a finish time; true when it is a new best.</summary>
        public static bool RecordTime(SaveData data, string trackId, float seconds)
        {
            if (seconds <= 0f) return false;
            if (data.raceBest == null) data.raceBest = new List<string>();
            if (data.raceTracks == null) data.raceTracks = new List<string>();
            if (!data.raceTracks.Contains(trackId)) data.raceTracks.Add(trackId);
            float best = Best(data, trackId);
            if (best > 0f && best <= seconds) return false;
            data.raceBest.RemoveAll(e => e.StartsWith(trackId + "=", StringComparison.Ordinal));
            data.raceBest.Add(trackId + "=" + seconds.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture));
            return true;
        }

        /// <summary>
        /// The story finish of a race stage: records the time, and marks the stage done (opening the next one)
        /// only when the place passes for this difficulty.
        /// </summary>
        public static bool FinishStage(SaveData data, string stageId, string trackId, float seconds, int place, RubberBand band)
        {
            RecordTime(data, trackId, seconds);
            if (!RaceRules.Passes(band, place, seconds > 0f)) return false;
            Regions.StageProgress.Finish(data, stageId, seconds, 0);
            return true;
        }
    }
}
