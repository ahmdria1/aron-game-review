// (c) Ahmed Alamrani. All rights reserved.
using System;
using System.Collections.Generic;

namespace Aron.Logic
{
    /// <summary>One difficulty level's tuning. Loaded from Resources/Config/difficulty.json.</summary>
    [Serializable]
    public class DifficultyLevel
    {
        public string id = "normal";
        /// <summary>Lives added to the normal starting count.</summary>
        public int extraLives;
        /// <summary>Seconds of play between lantern checkpoints.</summary>
        public float checkpointSeconds = 75f;
        /// <summary>Multiplier on enemy walk and chase speed.</summary>
        public float enemySpeed = 1f;
        /// <summary>Multiplier on boss health.</summary>
        public float bossHealth = 1f;
        /// <summary>Deaths at the same checkpoint before the golden bisht is handed over; 0 turns it off.</summary>
        public int mercyDeaths = MercyRule.DeathsBeforeHelp;
        /// <summary>Bosses get one more attack in every phase.</summary>
        public bool bossExtraAttack;
        /// <summary>The quiet helper that eases a spot the player keeps dying at.</summary>
        public bool assist = true;
    }

    /// <summary>The hidden helper's tuning, shared by every level that has it switched on.</summary>
    [Serializable]
    public class AssistTuning
    {
        /// <summary>Deaths at one checkpoint before anything changes.</summary>
        public int deathsBeforeAssist = 2;
        /// <summary>Chase hazards slow by this much per extra death...</summary>
        public float chaseSlowPerDeath = 0.05f;
        /// <summary>...down to this multiplier at most.</summary>
        public float minChaseSpeed = 0.8f;
        /// <summary>Extra seconds of "coyote time" for a jump just after leaving a ledge, per extra death.</summary>
        public float jumpGracePerDeath = 0.03f;
        public float maxJumpGrace = 0.12f;
    }

    [Serializable]
    public class DifficultyFile
    {
        public DifficultyLevel[] levels = Array.Empty<DifficultyLevel>();
        public AssistTuning assist = new AssistTuning();
        /// <summary>Costume ids given for beating every country on hard.</summary>
        public string hardRewardAron = "", hardRewardNoura = "";

        public static readonly string[] Ids = { "easy", "normal", "hard" };

        public DifficultyLevel Level(string id)
        {
            foreach (var l in levels) if (l.id == id) return l;
            foreach (var l in levels) if (l.id == "normal") return l;
            return new DifficultyLevel();
        }

        public List<string> Validate()
        {
            var e = new List<string>();
            foreach (var id in Ids)
            {
                int n = 0;
                foreach (var l in levels) if (l.id == id) n++;
                if (n != 1) e.Add("need exactly one level called " + id);
            }
            foreach (var l in levels)
            {
                if (Array.IndexOf(Ids, l.id) < 0) e.Add("unknown level " + l.id);
                if (l.checkpointSeconds < 30f || l.checkpointSeconds > 150f) e.Add(l.id + ": checkpoint spacing 30..150 s");
                if (l.enemySpeed < 0.5f || l.enemySpeed > 1.5f) e.Add(l.id + ": enemy speed 0.5..1.5");
                if (l.bossHealth < 0.5f || l.bossHealth > 1.5f) e.Add(l.id + ": boss health 0.5..1.5");
                if (l.extraLives < 0 || l.extraLives > 5) e.Add(l.id + ": extra lives 0..5");
                if (l.mercyDeaths < 0) e.Add(l.id + ": mercy deaths can't be negative");
            }
            if (assist == null) e.Add("assist tuning missing");
            else if (assist.minChaseSpeed < 0.5f || assist.minChaseSpeed > 1f) e.Add("assist min chase speed 0.5..1");
            if (string.IsNullOrEmpty(hardRewardAron) || string.IsNullOrEmpty(hardRewardNoura)) e.Add("hard reward costumes missing");
            return e;
        }

        /// <summary>Boss health after the level's multiplier, never below 1.</summary>
        public static int BossHealth(int baseHealth, DifficultyLevel l) => Math.Max(1, (int)Math.Round(baseHealth * l.bossHealth));

        /// <summary>
        /// The phase's attacks on this level. Hard adds the first attack the phase doesn't already use, so every
        /// phase gets one new move without changing how the boss looks.
        /// </summary>
        public static string[] BossAttacks(string[] phaseAttacks, DifficultyLevel l)
        {
            phaseAttacks = phaseAttacks ?? Array.Empty<string>();
            if (!l.bossExtraAttack) return phaseAttacks;
            foreach (var a in new[] { "wave", "volley", "dash", "leap", "charge", "summon" })
                if (Array.IndexOf(phaseAttacks, a) < 0)
                {
                    var list = new List<string>(phaseAttacks) { a };
                    return list.ToArray();
                }
            return phaseAttacks;
        }

        /// <summary>True once every region's boss has been beaten on hard.</summary>
        public static bool HardRewardEarned(Regions.RegionCatalog catalog, SaveData data)
        {
            if (catalog.regions.Length == 0) return false;
            foreach (var r in catalog.regions)
            {
                var last = r.stages[r.stages.Length - 1];
                var rec = Regions.StageProgress.Get(data, last.id);
                if (rec == null || !rec.doneHard) return false;
            }
            return true;
        }
    }

    /// <summary>
    /// The quiet helper. Counts deaths at the current checkpoint; after a couple it eases chase hazards a little and
    /// gives a touch more time to jump after running off a ledge. Moving on to a new checkpoint resets it. Off on hard.
    /// </summary>
    public class AdaptiveAssist
    {
        private readonly AssistTuning t;
        private readonly bool on;
        private string spot = "";
        public int Deaths { get; private set; }

        public AdaptiveAssist(DifficultyLevel level, AssistTuning tuning)
        {
            on = level != null && level.assist;
            t = tuning ?? new AssistTuning();
        }

        public void Reached(string checkpointId)
        {
            checkpointId = checkpointId ?? "";
            if (checkpointId == spot) return;
            spot = checkpointId;
            Deaths = 0;
        }

        public void Died(string checkpointId)
        {
            Reached(checkpointId);
            Deaths++;
        }

        private int Extra => on ? Math.Max(0, Deaths - t.deathsBeforeAssist + 1) : 0;

        /// <summary>Multiplier on chase hazard speed, 1 when the helper isn't doing anything.</summary>
        public float ChaseSpeed => Math.Max(t.minChaseSpeed, 1f - Extra * t.chaseSlowPerDeath);

        /// <summary>Extra seconds a jump is still allowed after leaving the ground.</summary>
        public float JumpGrace => Math.Min(t.maxJumpGrace, Extra * t.jumpGracePerDeath);
    }
}
