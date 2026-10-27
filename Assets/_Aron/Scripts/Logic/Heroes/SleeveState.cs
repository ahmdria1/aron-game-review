// (c) Ahmed Alamrani. All rights reserved.
using System;

namespace Aron.Logic.Heroes
{
    public enum SleeveState { Down, RollingUp, Up, RollingDown }

    /// <summary>What the runtime should play right now (once per change).</summary>
    public enum SleeveCue { None, RollUp, RollDown }

    /// <summary>Names the rigged model, the animator and the haptics table share.</summary>
    public static class SleeveRules
    {
        /// <summary>Blendshape (0..1, 1 = rolled to the forearm) and material float "_SleeveRoll" the future rigged model must have.</summary>
        public const string Hook = "sleeve_roll";
        public const string MaterialProperty = "_SleeveRoll";
        /// <summary>Animator triggers and the haptic key (a light tick as the cloth folds).</summary>
        public const string AnimRollUp = "sleeve_roll_up", AnimRollDown = "sleeve_roll_down", Haptic = "sleeve_tick";

        public const float RollUpSeconds = 0.6f, RollDownSeconds = 0.9f;
        /// <summary>No combat for this long and the sleeves come down.</summary>
        public const float QuietSeconds = 6f;
        /// <summary>An enemy this close (metres) counts as combat.</summary>
        public const float EngageRange = 7f;
        /// <summary>After a boss win the face wrap comes off with a smile this long before the sleeves start down.</summary>
        public const float WrapOffSeconds = 1.2f;

        /// <summary>Only Aron rolls his sleeves up. Noura's abaya keeps her arms covered at all times.</summary>
        public static bool Allowed(string heroId) => heroId == "aron";
    }

    /// <summary>
    /// Aron's sleeves. They roll up (0.6 s, and an attack cancels the roll straight into the attack) when an enemy comes within
    /// range, he attacks for the first time, or a boss fight starts. They come down calmly after 6 s without combat, at stage clear
    /// and after a boss win. A boss start is sleeves first, then the face wrap (<see cref="WrapMayStart"/>); a boss win is the wrap
    /// off with a smile first, then the sleeves (<see cref="SleeveRules.WrapOffSeconds"/>). Noura's director never leaves Down.
    /// </summary>
    public class SleeveDirector
    {
        public readonly string HeroId;
        public bool Allowed { get; }
        public SleeveState State { get; private set; } = SleeveState.Down;
        /// <summary>0 down .. 1 rolled up: the value for the sleeve_roll blendshape.</summary>
        public float Progress { get; private set; }
        public bool BossActive { get; private set; }

        private float quiet, stateTime, lowerDelay;
        private bool pendingLower;
        private SleeveCue queued = SleeveCue.None;

        public SleeveDirector(string heroId) { HeroId = heroId; Allowed = SleeveRules.Allowed(heroId); }

        /// <summary>The face wrap (talathum) may go on once the sleeves are up. For a hero without sleeves it is never blocked.</summary>
        public bool WrapMayStart => !Allowed || State == SleeveState.Up;

        // ---- triggers ----

        /// <summary>An enemy is this far away (metres); inside the engage range it counts as combat.</summary>
        public void EnemyNear(float distance)
        {
            if (distance >= 0f && distance <= SleeveRules.EngageRange) Combat();
        }

        /// <summary>The first (or any) attack: the roll is cut short and the sleeves are up at once.</summary>
        public void Attacked()
        {
            if (!Allowed) return;
            if (State == SleeveState.Down || State == SleeveState.RollingDown) { Begin(SleeveState.RollingUp, SleeveCue.RollUp); }
            if (State == SleeveState.RollingUp) { State = SleeveState.Up; Progress = 1f; stateTime = 0f; }
            quiet = 0f; pendingLower = false;
        }

        /// <summary>Boss fight starts: sleeves up first, then the wrap.</summary>
        public void BossStart() { BossActive = true; Combat(); }

        /// <summary>The boss fight is over. On a win the wrap comes off with a smile first, then the sleeves.</summary>
        public void BossEnd(bool won)
        {
            BossActive = false;
            if (!Allowed) return;
            if (won) { pendingLower = true; lowerDelay = SleeveRules.WrapOffSeconds; }
            else quiet = SleeveRules.QuietSeconds * 0.5f;
        }

        /// <summary>Stage clear: lower them calmly.</summary>
        public void StageClear()
        {
            BossActive = false;
            if (!Allowed) return;
            pendingLower = true; lowerDelay = 0f;
        }

        /// <summary>Leaving the scene: sleeves down at once, no animation.</summary>
        public void Reset() { State = SleeveState.Down; Progress = 0f; quiet = 0f; stateTime = 0f; pendingLower = false; BossActive = false; queued = SleeveCue.None; }

        private void Combat()
        {
            if (!Allowed) return;
            quiet = 0f; pendingLower = false;
            if (State == SleeveState.Down || State == SleeveState.RollingDown) Begin(SleeveState.RollingUp, SleeveCue.RollUp);
        }

        private void Begin(SleeveState s, SleeveCue cue)
        {
            State = s; stateTime = 0f; queued = cue;
        }

        /// <summary>Advance by dt seconds. Returns the cue to play, if one started since the last call.</summary>
        public SleeveCue Tick(float dt)
        {
            var cue = queued; queued = SleeveCue.None;
            if (!Allowed) return SleeveCue.None;
            stateTime += dt;
            switch (State)
            {
                case SleeveState.RollingUp:
                    Progress = Math.Min(1f, Progress + dt / SleeveRules.RollUpSeconds);
                    if (Progress >= 1f) { State = SleeveState.Up; stateTime = 0f; }
                    break;
                case SleeveState.Up:
                    if (pendingLower)
                    {
                        lowerDelay -= dt;
                        if (lowerDelay <= 0f) { pendingLower = false; Begin(SleeveState.RollingDown, SleeveCue.RollDown); cue = cue != SleeveCue.None ? cue : SleeveCue.RollDown; queued = SleeveCue.None; }
                    }
                    else if (!BossActive)
                    {
                        quiet += dt;
                        if (quiet >= SleeveRules.QuietSeconds) { Begin(SleeveState.RollingDown, SleeveCue.RollDown); cue = SleeveCue.RollDown; queued = SleeveCue.None; }
                    }
                    break;
                case SleeveState.RollingDown:
                    Progress = Math.Max(0f, Progress - dt / SleeveRules.RollDownSeconds);
                    if (Progress <= 0f) { State = SleeveState.Down; stateTime = 0f; }
                    break;
            }
            return cue;
        }
    }
}
