// (c) Ahmed Alamrani. All rights reserved.
using System.Collections.Generic;
using Aron.Logic.Animation;
using UnityEngine;

namespace Aron.Player
{
    /// <summary>
    /// Safe bridge to an Animator. Every call is a no-op if no Animator / parameter exists, so greybox works today and a real rig can be dropped in later.
    /// The parameters are the ones in <see cref="AnimParams.All"/> (Logic/Animation/AnimParams.cs, generated from tools/gen_animation_list.py with
    /// docs/animation_list.md): typed <c>SetFloat</c>, <c>SetBool</c>, <c>SetInt</c> and <c>Trigger</c> by name or hash, each checked against the
    /// controller's real parameter type so a wrong-type call never logs a warning every frame.
    /// <see cref="MissingP0Parameters"/> lists what an animator controller still lacks for the vertical-slice (P0) clips;
    /// the high-level events (<see cref="OnLanded"/>, <see cref="OnSkid"/>, <see cref="OnHit"/>, ...) fire the right trigger and let the procedural layer react too.
    /// </summary>
    public class PlayerAnimationHooks : MonoBehaviour
    {
        [SerializeField] private Animator animator;
        private readonly Dictionary<int, AnimatorControllerParameterType> parameters = new Dictionary<int, AnimatorControllerParameterType>();
        private static AnimClipFile clipFile;

        public static readonly int Speed = Animator.StringToHash(AnimParams.Speed);
        public static readonly int Grounded = Animator.StringToHash(AnimParams.Grounded);
        public static readonly int Sliding = Animator.StringToHash(AnimParams.Sliding);
        public static readonly int VerticalSpeed = Animator.StringToHash(AnimParams.VerticalSpeed);
        public static readonly int TurnAngle = Animator.StringToHash(AnimParams.TurnAngle);
        public static readonly int SlopeAngle = Animator.StringToHash(AnimParams.SlopeAngle);
        public static readonly int Sprinting = Animator.StringToHash(AnimParams.Sprinting);
        public static readonly int Skidding = Animator.StringToHash(AnimParams.Skidding);
        public static readonly int Gliding = Animator.StringToHash(AnimParams.Gliding);
        public static readonly int FastFall = Animator.StringToHash(AnimParams.FastFall);
        public static readonly int Riding = Animator.StringToHash(AnimParams.Riding);
        public static readonly int Dead = Animator.StringToHash(AnimParams.Dead);
        public static readonly int Stunned = Animator.StringToHash(AnimParams.Stunned);
        public static readonly int Wet = Animator.StringToHash(AnimParams.Wet);
        public static readonly int Wind = Animator.StringToHash(AnimParams.Wind);

        public Animator Animator => animator;
        public bool HasAnimator => animator != null && animator.runtimeAnimatorController != null;

        public void Bind(Animator a)
        {
            animator = a;
            parameters.Clear();
            if (animator == null || animator.runtimeAnimatorController == null) return;
            foreach (var p in animator.parameters) parameters[p.nameHash] = p.type;
        }

        private void Awake() { if (animator == null) animator = GetComponentInChildren<Animator>(); Bind(animator); }

        // ---- typed setters (by hash for the per-frame ones, by name for the rest)
        public void SetFloat(int hash, float v) { if (Is(hash, AnimatorControllerParameterType.Float)) animator.SetFloat(hash, v); }
        public void SetFloat(string name, float v) => SetFloat(Animator.StringToHash(name), v);
        public void SetBool(int hash, bool v) { if (Is(hash, AnimatorControllerParameterType.Bool)) animator.SetBool(hash, v); }
        public void SetBool(string name, bool v) => SetBool(Animator.StringToHash(name), v);
        public void SetInt(int hash, int v) { if (Is(hash, AnimatorControllerParameterType.Int)) animator.SetInteger(hash, v); }
        public void SetInt(string name, int v) => SetInt(Animator.StringToHash(name), v);

        public void Trigger(string name)
        {
            if (string.IsNullOrEmpty(name)) return;
            int h = Animator.StringToHash(name);
            if (Is(h, AnimatorControllerParameterType.Trigger)) animator.SetTrigger(h);
        }

        /// <summary>Like <see cref="Trigger"/>, but says whether the controller had that trigger (so a caller can fall back to another one).</summary>
        public bool TryTrigger(string name)
        {
            if (string.IsNullOrEmpty(name)) return false;
            int h = Animator.StringToHash(name);
            if (!Is(h, AnimatorControllerParameterType.Trigger)) return false;
            animator.SetTrigger(h);
            return true;
        }

        public void ResetTrigger(string name)
        {
            int h = Animator.StringToHash(name);
            if (Is(h, AnimatorControllerParameterType.Trigger)) animator.ResetTrigger(h);
        }

        /// <summary>Animator playback speed: 0 freezes the pose for the hit-stop, 1 resumes.</summary>
        public void SetSpeed(float s) { if (HasAnimator) animator.speed = s; }

        // ---- events: the trigger the P0 clips use, plus a note for the procedural layer
        /// <summary>Landed after a fall. Hard (a long fall) plays LandHard, otherwise Land.</summary>
        public void OnLanded(bool hard) => Trigger(hard ? AnimParams.LandHard : AnimParams.Land);

        // ---- camel race (the clips camel_racer_*, robot_jockey_*, rider_camel_race_*): cartoon, nobody is ever hurt
        /// <summary>The camel kneels at the gate so the rider can climb on.</summary>
        public void OnKneel() => Trigger(AnimParams.Kneel);
        /// <summary>The camel gets up, ready for the start.</summary>
        public void OnRise() => Trigger(AnimParams.Rise);
        /// <summary>A comic stumble: a wobble and a short slowdown, never an injury.</summary>
        public void OnStumble() => Trigger(AnimParams.Stumble);
        /// <summary>A boost started (the one-off burst) or ended; Boosting stays true while it lasts.</summary>
        public void OnBoost(bool started) { if (started) Trigger(AnimParams.Boost); SetBool(AnimParams.Boosting, started); }
        /// <summary>Past the finish line: the winner celebrates, everyone else cheers or slumps with good humour.</summary>
        public void OnFinishCelebrate(bool won) => Trigger(won ? AnimParams.Celebrate : AnimParams.Lose);

        /// <summary>A skid started (Skidding stays true until it ends; the trigger is for one-off blends).</summary>
        public void OnSkid(bool started) => SetBool(Skidding, started);

        /// <summary>
        /// Took a hit that did not end the run. <paramref name="localDirX"/>/<paramref name="localDirZ"/> is where it came from in the hero's own space
        /// (+z in front, +x on his right): front hits play Hurt, hits from behind HurtBack, and from the sides HurtLeft / HurtRight.
        /// </summary>
        public void OnHit(float localDirX, float localDirZ)
        {
            if (Mathf.Abs(localDirX) > Mathf.Abs(localDirZ)) Trigger(localDirX > 0f ? AnimParams.HurtRight : AnimParams.HurtLeft);
            else Trigger(localDirZ < 0f ? AnimParams.HurtBack : AnimParams.Hurt);
        }

        public void OnBonk() => Trigger(AnimParams.Bonk);
        public void OnLedgePop() => Trigger(AnimParams.LedgePop);
        public void OnBounce() => Trigger(AnimParams.Bounce);
        public void OnStomp() => Trigger(AnimParams.Stomp);

        // ---- validation against the clip list
        /// <summary>The clip list (Resources/Config/animation_clips.json), loaded once.</summary>
        public static AnimClipFile ClipList
        {
            get
            {
                if (clipFile != null) return clipFile;
                var t = Resources.Load<TextAsset>("Config/animation_clips");
                clipFile = t != null ? JsonUtility.FromJson<AnimClipFile>(t.text) : new AnimClipFile();
                return clipFile;
            }
        }

        /// <summary>
        /// The P0 parameters that this animator controller does not have, or has with another type, as "Name (Type)". Pass an actor
        /// ("aron", "noura", ...) to check only that actor's P0 clips; null checks every P0 clip. Empty when the controller is complete (or there is none).
        /// </summary>
        public List<string> MissingP0Parameters(string actor = null)
        {
            var missing = new List<string>();
            if (!HasAnimator) return missing;
            var seen = new HashSet<string>();
            foreach (var c in ClipList.P0())
            {
                if (actor != null && c.actor != actor) continue;
                if (!seen.Add(c.param)) continue;
                foreach (var p in AnimParams.All)
                {
                    if (p.Name != c.param) continue;
                    int h = Animator.StringToHash(p.Name);
                    if (!parameters.TryGetValue(h, out var type) || type != ToUnity(p.Type)) missing.Add(p.Name + " (" + p.Type + ")");
                }
            }
            return missing;
        }

        private static AnimatorControllerParameterType ToUnity(AnimParamType t)
        {
            switch (t)
            {
                case AnimParamType.Float: return AnimatorControllerParameterType.Float;
                case AnimParamType.Int: return AnimatorControllerParameterType.Int;
                case AnimParamType.Bool: return AnimatorControllerParameterType.Bool;
                default: return AnimatorControllerParameterType.Trigger;
            }
        }

        private bool Is(int hash, AnimatorControllerParameterType type) => animator != null && parameters.TryGetValue(hash, out var t) && t == type;
    }
}
