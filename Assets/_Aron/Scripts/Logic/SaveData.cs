// (c) Ahmed Alamrani. All rights reserved.
using System;
using System.Collections.Generic;
using System.IO;

namespace Aron.Logic
{
    [Serializable]
    public class SaveData
    {
        /// <summary>Studio signature, kept first in the file header. Not shown anywhere in the UI.</summary>
        public string sig = Signature.StudioMark.Value;
        /// <summary>What the trusted clock remembers between sessions (see Integrity/TrustedClock.cs).</summary>
        public Aron.Logic.Integrity.ClockMemory clock = new Aron.Logic.Integrity.ClockMemory();
        public int version = 4;
        public string selectedCharacterId = "aron";
        public int highestLevelCompleted;
        public int totalDates;
        public string lastCheckpointLevel = "";
        public bool musicEnabled = true;
        public float musicVolume = 0.7f;
        public float sfxVolume = 1f;
        /// <summary>UI language code; empty means follow the phone.</summary>
        public string language = "";
        public int touchTutorialStep;

        // Player name (version 4, additive). Only a chosen name and an age band are kept, never a birth date or a real name.
        /// <summary>The name shown to other players, without the number.</summary>
        public string playerName = "";
        /// <summary>The number after the name (1000-9999) when the server had to tell two players apart; 0 when none.</summary>
        public int playerNameTag;
        /// <summary>True when the server has confirmed the name is unique. Online races need this.</summary>
        public bool nameReserved;
        /// <summary>True after enough reports: the player has to pick a new name before online races.</summary>
        public bool nameMustChange;
        public long nameChangedTicks;
        /// <summary>"child", "teen", "adult" or "" when not asked yet. See Safety.AgeGate.</summary>
        public string ageBand = "";

        // Store (version 2). Coins are the wallet; totalDates stays as the all-time count.
        public int coins;
        public List<string> ownedItems = new List<string>();
        /// <summary>"slot=itemId" pairs.</summary>
        public List<string> equippedItems = new List<string>();
        public bool adsRemoved;
        /// <summary>Recent store order ids, so a purchase that is delivered twice is only granted once.</summary>
        public List<string> handledOrders = new List<string>();

        // Regions: one record per stage that has been finished.
        public List<Regions.StageRecord> stages = new List<Regions.StageRecord>();
        public string lastStageId = "";

        // Companions and costumes.
        /// <summary>"id=level" pairs for the camel, horse and falcon upgrades.</summary>
        public List<string> companionLevels = new List<string>();
        /// <summary>Costume picked in the wardrobe. Empty means Aron wears the costume of the region he's in.</summary>
        public string costumeId = "";
        /// <summary>Noura's wardrobe pick, kept apart from Aron's.</summary>
        public string nouraCostumeId = "";
        public List<string> ownedCostumes = new List<string>();
        /// <summary>Who the player last ran with: aron or noura.</summary>
        public string hero = "aron";

        // World (version 3).
        /// <summary>Country the ring of countries starts from. Empty until set; saves from before version 3 are saudi.</summary>
        public string homeCountry = "";
        /// <summary>Food ids picked up at least once, for the food album.</summary>
        public List<string> foodsCollected = new List<string>();
        /// <summary>UTC day (yyyy-MM-dd) of the last finished daily challenge.</summary>
        public string dailyDone = "";
        public int dailyStreak;
        /// <summary>Achievement ids already reported, so each is only sent once.</summary>
        public List<string> achievements = new List<string>();
        /// <summary>easy, normal or hard (see DifficultyRules).</summary>
        public string difficulty = "normal";

        // Free gift every few hours (see GiftTimer). Times are UTC ticks.
        public long giftLastClaim;
        /// <summary>Latest clock time ever seen, so winding the clock back can't bring the gift early.</summary>
        public long giftLatestSeen;
        public int giftStreak;
        /// <summary>UTC day (yyyy-MM-dd) of the last claim, for the daily streak.</summary>
        public string giftLastDay = "";
        /// <summary>"costumeId=pieces" for costumes being put together from gift pieces.</summary>
        public List<string> costumePieces = new List<string>();
        /// <summary>0 not asked yet, 1 wants the "gift ready" reminder, 2 said no.</summary>
        public int giftReminder;
        /// <summary>"kind=count" for the one-stage helpers (see Helpers).</summary>
        public List<string> helpers = new List<string>();
        /// <summary>Travel tickets in hand (they never run out), and "country=endTicks" for running 24-hour passes.</summary>
        public int tickets;
        public List<string> ticketCountries = new List<string>();
        /// <summary>Gems toward the next ticket.</summary>
        public int gems;
        /// <summary>Race tracks opened at least once ("track") and best times ("track=seconds").</summary>
        public List<string> raceTracks = new List<string>();
        public List<string> raceBest = new List<string>();
        /// <summary>1 shows subtitles (the default), 0 hides them.</summary>
        public int subtitles = 1;
        /// <summary>Name shown in online races (checked by the name filter). Empty until the player picks one.</summary>
        public string displayName = "";
        /// <summary>The hero picked for races: aron or noura.</summary>
        public string raceHero = "aron";
        /// <summary>Foods carried from the stalls, "foodId=count", at most five stacks.</summary>
        public List<string> carriedFoods = new List<string>();
        /// <summary>Friendship with each companion, "oryx=40" (0..100, cosmetic).</summary>
        public List<string> companionLove = new List<string>();
        /// <summary>Medals per race track, "track=1" (1 gold, 2 silver, 3 bronze); online wins and podiums; ghost wins.</summary>
        public List<string> raceMedals = new List<string>();
        public int onlineWins, onlinePodiums, ghostWins;
        public List<string> secretsFound = new List<string>();
        /// <summary>Passport entry stamps, "country=yyyy-MM-dd" (UTC day the country's last boss fell).</summary>
        /// <summary>Cultural artifacts found, "country=yyyy-MM-dd": one per country, shown in the museum and on the passport page.</summary>
        public List<string> artifactsFound = new List<string>();
        public List<string> passportStamps = new List<string>();
        /// <summary>Passport visa stickers from travel-ticket visits, "country=yyyy-MM-dd".</summary>
        public List<string> passportVisas = new List<string>();
        /// <summary>Souvenir photo per page, "country=auto|photo:&lt;file name&gt;". Files live in the app's own folder.</summary>
        public List<string> passportPhotos = new List<string>();
        /// <summary>Gallery add permission: 0 never asked, 1 granted, 2 denied.</summary>
        public int galleryPermission;
        /// <summary>Linked sign-in providers ("email", "google", "apple") and the masked email shown in the profile.</summary>
        public List<string> linkedProviders = new List<string>();
        public string emailMasked = "";

        // Race mounts and points (version 4).
        /// <summary>Spendable race points (48-hour rentals only), and the lifetime total (a statistic: it opens nothing, ownership is by purchase).</summary>
        public int racePoints, racePointsLifetime;
        /// <summary>"day|source|track=count" so the daily limit holds (today's entries only).</summary>
        public List<string> pointLog = new List<string>();
        /// <summary>Running rentals, "itemId=endTicks" (UTC).</summary>
        public List<string> rentals = new List<string>();
        /// <summary>Latest clock time seen by the rental code, so winding the clock back can't extend one.</summary>
        public long rentLatestSeen;
        /// <summary>1 lets rival characters speak (the default), 0 mutes them. Subtitles use <see cref="subtitles"/>.</summary>
        public int rivalChatter = 1;
        /// <summary>Last difficulty picked for computer racers: easy, normal or hard.</summary>
        public string aiDifficulty = "normal";
        /// <summary>Country music and other on-demand content may download over mobile data. Off by default: Wi-Fi only (additive field, absent in older saves).</summary>
        public bool cellularContent;

        /// <summary>Accessibility choices (button size, colour filter, reduce motion, reduce flashing).</summary>
        public Safety.AccessSettings access = new Safety.AccessSettings();
        /// <summary>Haptics: on/off, Low/Medium/High, battery saver.</summary>
        public Feedback.HapticSettings haptics = new Feedback.HapticSettings();
        /// <summary>Camera: shake strength, invert-Y, orbit speed (see CameraSettings).</summary>
        public CameraRig.CameraSettings camera = new CameraRig.CameraSettings();
        /// <summary>Dynamic weather on or off, free-play weather effects, water reflections.</summary>
        public Sky.WeatherSettings weather = new Sky.WeatherSettings();
        /// <summary>Battery saver, 60 fps choice, data saver and mobile-data downloads.</summary>
        public Performance.PowerSettings power = new Performance.PowerSettings();
        /// <summary>What a grown-up allowed for usage statistics and crash reports. Everything is off until chosen.</summary>
        public Telemetry.PrivacyChoices privacy = new Telemetry.PrivacyChoices();
    }

    /// <summary>Power-loss-safe text file storage: writes to a temp file, then swaps it in.</summary>
    public static class AtomicFile
    {
        public static void WriteAllText(string path, string contents)
        {
            string tmp = path + ".tmp";
            File.WriteAllText(tmp, contents);
            if (File.Exists(path)) File.Replace(tmp, path, null);
            else File.Move(tmp, path);
        }

        public static bool TryReadAllText(string path, out string contents)
        {
            contents = null;
            if (!File.Exists(path)) return false;
            try { contents = File.ReadAllText(path); return true; }
            catch (IOException) { return false; }
        }
    }
}
