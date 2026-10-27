// (c) Ahmed Alamrani. All rights reserved.
using System;

namespace Aron.Logic.Integrity
{
    [Flags]
    public enum SoftFlag
    {
        None = 0,
        /// <summary>Root or jailbreak signs (see docs/security.md): informational only.</summary>
        RootedDevice = 1,
        DebuggerAttached = 2,
        ClockTampered = 4,
        WalletJump = 8,
        MemoryEdit = 16,
        ModifiedBuild = 32,
    }

    /// <summary>
    /// Flags found on this device. They never block offline play or change a save. They are sent with online requests
    /// so the server can keep a flagged phone out of ranked results and the leaderboard.
    /// </summary>
    public static class SoftFlags
    {
        public static SoftFlag Current { get; private set; }
        public static void Raise(SoftFlag f) { Current |= f; }
        public static void Clear() { Current = SoftFlag.None; }
        public static bool Any => Current != SoftFlag.None;
        public static bool OnlinePlayFlagged => Current != SoftFlag.None;
    }

    /// <summary>
    /// Keeps masked copies of coins and race points and checks the plain saved numbers against them each time the game saves.
    /// Honest play moves the numbers by small steps (a stage, a race, a gift); an edit from a memory editor does not.
    /// SaveData stays plain because JsonUtility needs plain fields, so this is detection, not prevention.
    /// </summary>
    public class WalletWatch
    {
        private readonly GuardedInt coins = new GuardedInt(), points = new GuardedInt(), lifetime = new GuardedInt();
        private bool primed;
        /// <summary>Most coins that can honestly arrive between two saves (a boss-stage payout, a pack, a gift streak).</summary>
        public int MaxCoinGain = 3000;
        /// <summary>Most race points between two saves: a day's cap.</summary>
        public int MaxPointGain = 100;

        public int Alerts { get; private set; }
        public string LastAlert { get; private set; } = "";

        /// <summary>True when the numbers look honest. A false answer raises the soft flag and the baseline is moved on.</summary>
        public bool Audit(SaveData d)
        {
            if (d == null) return true;
            bool ok = true;
            if (primed)
            {
                if (d.coins - coins.Value > MaxCoinGain) { ok = false; LastAlert = "coins"; }
                if (d.racePoints - points.Value > MaxPointGain) { ok = false; LastAlert = "racePoints"; }
                if (d.racePointsLifetime < lifetime.Value) { ok = false; LastAlert = "lifetime_went_down"; }
                if (d.coins < 0 || d.racePoints < 0) { ok = false; LastAlert = "negative"; }
            }
            if (coins.Tampered || points.Tampered || lifetime.Tampered) { ok = false; LastAlert = "memory"; SoftFlags.Raise(SoftFlag.MemoryEdit); }
            if (!ok) { Alerts++; SoftFlags.Raise(SoftFlag.WalletJump); }
            coins.Value = d.coins; points.Value = d.racePoints; lifetime.Value = d.racePointsLifetime;
            primed = true;
            return ok;
        }
    }
}
