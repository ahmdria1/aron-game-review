// (c) Ahmed Alamrani. All rights reserved.
using System;
using System.Collections.Generic;
using System.Text;

namespace Aron.Logic.Safety
{
    /// <summary>Asked once at first launch: birth year only, nothing stored but the band.</summary>
    public static class AgeGate
    {
        public const int ChildUnder = 13;

        public static string Band(int birthYear, int thisYear)
        {
            int age = thisYear - birthYear;
            if (birthYear <= 1900 || age < 0) return "";
            return age < ChildUnder ? "child" : age < 18 ? "teen" : "adult";
        }

        /// <summary>Children race online only with a generated safe name, never one they type.</summary>
        public static bool CanTypeName(string band) => band == "teen" || band == "adult";
        /// <summary>Every purchase asks to confirm; children also get the grown-up check (a sum to solve).</summary>
        public static bool NeedsGrownUpCheck(string band) => band == "child" || band == "";
        public static bool OnlineAllowed(string band, bool parentAllowed) => band != "child" || parentAllowed;

        /// <summary>A sum a young child can't easily do but a parent can: two two-digit numbers.</summary>
        public static int[] GrownUpSum(Random rnd) => new[] { rnd.Next(12, 49), rnd.Next(12, 49) };
    }

    /// <summary>Cloud save vs the phone: the newer progress wins, nothing bought or earned is ever lost.</summary>
    public static class SaveMerge
    {
        public static int Score(SaveData d) => (d.stages?.Count ?? 0) * 10 + (d.foodsCollected?.Count ?? 0) + (d.ownedCostumes?.Count ?? 0);

        /// <summary>
        /// Keeps the copy with more progress (newer on a tie) and folds in from the other everything that only grows:
        /// owned costumes, food cards, secrets, race tracks and medals. Coins and tickets come from the kept copy.
        /// </summary>
        public static SaveData Merge(SaveData local, long localSavedTicks, SaveData cloud, long cloudSavedTicks)
        {
            if (cloud == null) return local;
            if (local == null) return cloud;
            int ls = Score(local), cs = Score(cloud);
            var keep = ls > cs || (ls == cs && localSavedTicks >= cloudSavedTicks) ? local : cloud;
            var other = keep == local ? cloud : local;
            Union(keep.ownedCostumes, other.ownedCostumes);
            Union(keep.foodsCollected, other.foodsCollected);
            Union(keep.secretsFound, other.secretsFound);
            Union(keep.raceTracks, other.raceTracks);
            Union(keep.achievements, other.achievements);
            // Things that were paid for or earned must never vanish when two devices merge.
            Union(keep.ownedItems, other.ownedItems);
            keep.racePointsLifetime = Math.Max(keep.racePointsLifetime, other.racePointsLifetime);
            return keep;
        }

        private static void Union(List<string> into, List<string> from)
        {
            if (into == null || from == null) return;
            foreach (var x in from) if (!into.Contains(x)) into.Add(x);
        }
    }

    /// <summary>Account and data deletion (Google Play and Apple require it) and a readable export.</summary>
    public static class AccountData
    {
        /// <summary>What deletion removes: the save on the phone, the cloud save, ghosts and the anonymous player id.</summary>
        public static readonly string[] DeleteSteps = { "local_save", "cloud_save", "ghosts", "leaderboard_entries", "email_link", "consent_record", "otp_state", "passport_photos", "analytics_data", "player_id" };

        /// <summary>The save as JSON text the player can keep (no secrets in it: the save holds none).</summary>
        public static string Export(string saveJson) => saveJson ?? "{}";

        public static bool Confirmed(string typed, string word) => string.Equals((typed ?? "").Trim(), word, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>Accessibility choices, saved with the game.</summary>
    [Serializable]
    public class AccessSettings
    {
        public float buttonSize = 1f, buttonOpacity = 0.8f;
        public bool leftHanded;
        /// <summary>Assist mode (see <see cref="Aron.Logic.Access.AssistRules"/>): sooner checkpoints, a slower chase, a stronger landing shadow. Off by default.</summary>
        public bool assistMode;
        /// <summary>Photosensitivity-safe: reduce flashing and reduce motion together (see <see cref="Aron.Logic.Access.FlashSafety"/>).</summary>
        public bool photoSafe;
        /// <summary>Buttons the player moved by hand (see <see cref="Aron.Logic.Access.ControlLayout"/>). Empty means the default places.</summary>
        public List<Aron.Logic.Access.ButtonPlacement> placements = new List<Aron.Logic.Access.ButtonPlacement>();
        /// <summary>none, protanopia, deuteranopia, tritanopia.</summary>
        public string colourFilter = "none";
        public float subtitleSize = 1f, textSize = 1f;
        /// <summary>Captions for what the heroes and bosses say (and for sounds, see <see cref="soundCaptions"/>). On by default: the voices are short and a child who cannot hear them still gets the line.</summary>
        public bool captions = true;
        /// <summary>Which sounds get a caption: only the ones that warn or reward (the default), all of them, or none.</summary>
        public Aron.Logic.Access.SoundCaptionMode soundCaptions = Aron.Logic.Access.SoundCaptionMode.Important;
        /// <summary>How dark the box behind a caption is, 0.4..1.</summary>
        public float captionBackdrop = 0.75f;
        public bool reduceMotion, reduceFlash;
        public bool oneThumb;
        /// <summary>One-hand mode (see <see cref="OneHandMode"/>): everything is reachable with one thumb. Allowed on every difficulty.</summary>
        public bool oneHand;
        /// <summary>Light aim assist for the slingshot and melee on touch (see <see cref="Combat.AimAssist"/>). On by default; never aims at animals or birds.</summary>
        public bool aimAssist = true;

        public static readonly string[] Filters = { "none", "protanopia", "deuteranopia", "tritanopia" };

        public List<string> Validate()
        {
            var e = new List<string>();
            if (buttonSize < 0.7f || buttonSize > 1.6f) e.Add("button size 0.7..1.6");
            if (buttonOpacity < 0.3f || buttonOpacity > 1f) e.Add("button opacity 0.3..1");
            if (subtitleSize < 0.8f || subtitleSize > 2f || textSize < 0.8f || textSize > 1.6f) e.Add("text sizes");
            if (Array.IndexOf(Filters, colourFilter) < 0) e.Add("unknown colour filter");
            if (captionBackdrop < 0.4f || captionBackdrop > 1f) e.Add("caption backdrop 0.4..1");
            e.AddRange(Aron.Logic.Access.ControlLayout.Validate(this));
            return e;
        }

        /// <summary>One-thumb assist is offered on easy only.</summary>
        public static bool OneThumbAllowed(string difficulty) => difficulty == "easy";

        /// <summary>Mirrors a button's x (0..1 across the screen) for left-handed play.</summary>
        public float ButtonX(float x) => leftHanded ? 1f - x : x;
    }

    /// <summary>
    /// One-hand mode: the camera follows by itself (no orbit or pinch, which need a second finger), the pause button is on screen
    /// (two-finger tap pauses otherwise), the screen buttons sit on the thumb's side, and tap aim help is wider.
    /// </summary>
    public static class OneHandMode
    {
        public const float AimRadiusScale = 1.5f;

        public static bool OrbitAllowed(AccessSettings a) => a == null || !a.oneHand;
        public static bool PinchAllowed(AccessSettings a) => a == null || !a.oneHand;
        public static bool PauseButtonShown(AccessSettings a) => a != null && a.oneHand;
        public static float AimScale(AccessSettings a) => a != null && a.oneHand ? AimRadiusScale : 1f;
        /// <summary>The recenter button is not needed: the camera does it by itself.</summary>
        public static bool RecenterButtonShown(AccessSettings a) => OrbitAllowed(a);
        /// <summary>x (0..1) for a screen button: mirrored for left-handed play, and pulled in toward the thumb in one-hand mode.</summary>
        public static float ButtonX(AccessSettings a, float x)
        {
            if (a == null) return x;
            float v = a.ButtonX(x);
            if (a.oneHand) v = a.leftHanded ? Math.Min(v, 0.2f) : Math.Max(v, 0.8f);
            return v;
        }
    }

    /// <summary>Graphics tier, frame cap and battery saver.</summary>
    [Serializable]
    public class GraphicsSettings
    {
        public string tier = "auto";
        public int fps = 60;
        public bool batterySaver;
        public static readonly string[] Tiers = { "auto", "low", "medium", "high" };

        public int TargetFps => batterySaver ? 30 : (fps == 30 ? 30 : 60);
        public bool Valid => Array.IndexOf(Tiers, tier) >= 0 && (fps == 30 || fps == 60);
    }

    /// <summary>
    /// Leaderboard sanity checks, done again on the server (Cloud Code): a time can't beat the fastest
    /// possible run of the track, and every submission carries a signature over its fields.
    /// </summary>
    public static class ScoreCheck
    {
        /// <summary>The fastest believable time: the whole length at the top speed with every boost.</summary>
        public static float MinimumSeconds(Races.RaceTrack t, Races.RaceFile f, float topSpeed)
        {
            float boostTime = t.boosts * f.boostSeconds * (t.laps > 0 ? t.laps : 1);
            float boostedDistance = boostTime * topSpeed * f.boostSpeed;
            float rest = Math.Max(0f, t.TotalLength - boostedDistance);
            return Math.Min(boostTime, t.TotalLength / (topSpeed * f.boostSpeed)) + rest / topSpeed;
        }

        public static bool Plausible(float seconds, Races.RaceTrack t, Races.RaceFile f, float topSpeed) =>
            seconds >= MinimumSeconds(t, f, topSpeed) * 0.98f && seconds < 3600f;

        /// <summary>FNV-1a over the submission with a per-build salt. Not secret-proof, it stops casual edits; the server re-checks.</summary>
        public static string Sign(string playerId, string track, float seconds, string salt)
        {
            string s = playerId + "|" + track + "|" + seconds.ToString("0.000", System.Globalization.CultureInfo.InvariantCulture) + "|" + salt;
            uint h = 2166136261;
            foreach (byte b in Encoding.UTF8.GetBytes(s)) { h ^= b; h *= 16777619; }
            return h.ToString("x8");
        }
    }

    /// <summary>Error reports carry no personal data: names, ids and emails are scrubbed before sending.</summary>
    public static class ErrorReportScrub
    {
        public static string Scrub(string text, IEnumerable<string> secrets)
        {
            if (string.IsNullOrEmpty(text)) return text;
            string t = text;
            foreach (var s in secrets ?? Array.Empty<string>()) if (!string.IsNullOrEmpty(s)) t = t.Replace(s, "[x]");
            t = System.Text.RegularExpressions.Regex.Replace(t, @"[A-Za-z0-9._%+-]+@[A-Za-z0-9.-]+\.[A-Za-z]{2,}", "[email]");
            return t;
        }
    }

    public static class Credits
    {
        public const string Creator = "Ahmed Alamrani";
    }
}
