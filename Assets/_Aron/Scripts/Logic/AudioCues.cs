// (c) Ahmed Alamrani. All rights reserved.
namespace Aron.Logic
{
    /// <summary>
    /// Names of every sound the game asks for. The clip with this name in Resources/Audio/Sfx, Resources/Audio/Music
    /// or Resources/Audio/Voice gets played. Music ids are in <see cref="MusicCues"/>.
    /// Voice lines look for "&lt;id&gt;_noura" first when Noura is the one talking.
    /// </summary>
    public static class AudioCues
    {
        // Sound effects (Audio/Sfx)
        public const string BishtPickup = "bisht_pickup";
        public const string BishtLost = "bisht_lost";
        public const string BishtBox = "bisht_box";
        public const string EnemyStomp = "enemy_stomp";
        public const string ExtraLife = "life";
        public const string PuzzleStep = "puzzle_step";
        public const string PuzzleWrong = "puzzle_wrong";
        public const string PuzzleSolved = "puzzle_solved";
        public const string GateOpen = "gate_open";
        public const string Lever = "lever";
        public const string TimerTick = "timer_tick";
        public const string CratePush = "crate_push";
        public const string ChaseStart = "chase_start";
        public const string DeathSpin = "death_spin";
        public const string DeathFall = "death_fall";
        public const string DeathSplash = "death_splash";
        public const string DeathSit = "death_sit";

        // Music (Audio/Music)
        public const string InvincibleMusic = MusicCues.Invincible;

        // Voice lines (Audio/Voice)
        public const string VoBisht = "vo_bisht";
        public const string VoBishtFull = "vo_bisht_full";
        public const string VoMercy = "vo_mercy";
        public const string VoPuzzleSolved = "vo_puzzle_solved";
        public const string VoPuzzleWrong = "vo_puzzle_wrong";
        public const string VoLockHint = "vo_lock_hint";
        public const string VoChase = "vo_chase";
        public const string VoExtraLife = "vo_extra_life";
        public const string VoCheckpoint = "vo_checkpoint";
        // Action barks
        public const string VoAronHelp = "vo_aron_help";
        public const string VoAronNearMiss = "vo_aron_near_miss";
        public const string VoAronFalling = "vo_aron_falling";
        public const string VoAronBishtCheer = "vo_aron_bisht_cheer";
        public const string VoAronCheckpoint = "vo_aron_checkpoint";
        public const string VoAronStomp = "vo_aron_stomp";
        public const string VoNouraUff = "vo_noura_uff";
        public const string VoNouraCallOryx = "vo_noura_call_oryx";
        public const string VoNouraPuzzleSolved = "vo_noura_puzzle_solved";
        public const string VoNouraBagThrow = "vo_noura_bag_throw";
        public const string VoUff = VoNouraUff;
        public const string VoFallWave = VoAronFalling;

        /// <summary>Boss barks: vo_boss_&lt;model&gt;_taunt, _hurt and _defeat.</summary>
        public static string Boss(string model, string kind) => "vo_boss_" + (string.IsNullOrEmpty(model) ? "scientist" : model) + "_" + kind;
        public const string BossTaunt = "taunt", BossHurt = "hurt", BossDefeat = "defeat";

        public static readonly string[] Sfx =
        {
            BishtPickup, BishtLost, BishtBox, EnemyStomp, ExtraLife, PuzzleStep, PuzzleWrong, PuzzleSolved, GateOpen, Lever,
            TimerTick, CratePush, ChaseStart, DeathSpin, DeathFall, DeathSplash, DeathSit,
        };
        public static readonly string[] Music = MusicCues.Generic;
        public static readonly string[] Voice =
        {
            VoBisht, VoBishtFull, VoMercy, VoPuzzleSolved, VoPuzzleWrong, VoLockHint, VoChase, VoExtraLife, VoCheckpoint,
            VoAronHelp, VoAronNearMiss, VoAronFalling, VoAronBishtCheer, VoAronCheckpoint, VoAronStomp,
            VoNouraUff, VoNouraCallOryx, VoNouraPuzzleSolved, VoNouraBagThrow,
        };

        /// <summary>The sound for a knock-out, by cause and hero.</summary>
        public static string ForDeath(DeathCause cause, string hero)
        {
            switch (cause)
            {
                case DeathCause.Pit: return DeathFall;
                case DeathCause.Water: return DeathSplash;
                default: return hero == "noura" ? DeathSit : DeathSpin;
            }
        }
    }
}
