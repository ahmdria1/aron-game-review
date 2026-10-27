// (c) Ahmed Alamrani. All rights reserved.
using System;

namespace Aron.Logic.GameFeel
{
    /// <summary>
    /// Turns a variable frame time into whole 60 Hz steps, so attack timings count the same on a 30 fps phone and a 120 Hz one.
    /// At most <see cref="MaxStepsPerFrame"/> steps are handed out per call (a hitch never fast-forwards a swing), and the rest is dropped.
    /// </summary>
    public class FixedStepper
    {
        public const float Rate = 60f;
        public const float Step = 1f / Rate;
        public const int MaxStepsPerFrame = 4;
        private float acc;

        /// <summary>Whole steps due after adding <paramref name="dt"/>.</summary>
        public int Advance(float dt)
        {
            acc += Math.Max(0f, dt);
            int n = (int)(acc / Step + 1e-4f);
            if (n > MaxStepsPerFrame) { n = MaxStepsPerFrame; acc = 0f; }
            else acc -= n * Step;
            if (acc < 0f) acc = 0f;
            return n;
        }

        public void Reset() => acc = 0f;
    }

    /// <summary>Jump-press buffer and coyote time, on a caller-supplied clock (so they test without an engine).</summary>
    public class InputBuffers
    {
        private double lastJumpPress = -100, lastGrounded = -100, lastAttackPress = -100, lastBouncePress = -100;

        public void PressJump(double now) { lastJumpPress = now; lastBouncePress = now; }
        public void PressAttack(double now) => lastAttackPress = now;
        public void Grounded(double now) => lastGrounded = now;
        public void Left() => lastGrounded = -100;

        public bool JumpBuffered(double now, float window) => now - lastJumpPress <= window + 1e-6;
        public void ConsumeJump() => lastJumpPress = -100;
        public bool AttackBuffered(double now, float window) => now - lastAttackPress <= window + 1e-6;
        public void ConsumeAttack() => lastAttackPress = -100;

        /// <summary>True while a ground jump is still allowed (standing, or within the coyote window after walking off).</summary>
        public bool CoyoteOpen(double now, float window) => now - lastGrounded <= window + 1e-6;

        /// <summary>Seconds since the jump button went down (large when it never did).</summary>
        public float SinceJumpPress(double now) => (float)Math.Min(100.0, now - lastJumpPress);

        public static float JumpWindow(FeelValues v, bool oneHand) => v.jumpBuffer + (oneHand ? v.oneHandJumpBufferAdd : 0f);
        public static float CoyoteWindow(FeelValues v, bool oneHand) => v.coyoteTime + (oneHand ? v.oneHandCoyoteAdd : 0f);
    }

    public enum AttackPhase { None, Startup, Active, Recovery }

    /// <summary>Startup, active and recovery lengths of an attack, in 60 Hz frames, and the frame from which a jump may cancel it.</summary>
    public struct AttackFrames
    {
        public int Startup, Active, Recovery, JumpCancelFrame;
        public int Total => Startup + Active + Recovery;

        public static AttackFrames Melee(FeelValues v) => new AttackFrames { Startup = v.meleeStartupFrames, Active = v.meleeActiveFrames, Recovery = v.meleeRecoveryFrames, JumpCancelFrame = v.meleeJumpCancelFrame };
        public static AttackFrames Spin(FeelValues v) => new AttackFrames { Startup = v.spinStartupFrames, Active = v.spinActiveFrames, Recovery = v.spinRecoveryFrames, JumpCancelFrame = v.spinJumpCancelFrame };

        /// <summary>The phase of the frame with this zero-based index (frame 0 is the first frame after the press).</summary>
        public AttackPhase PhaseAt(int frame)
        {
            if (frame < 0 || frame >= Total) return AttackPhase.None;
            if (frame < Startup) return AttackPhase.Startup;
            if (frame < Startup + Active) return AttackPhase.Active;
            return AttackPhase.Recovery;
        }

        /// <summary>Jump may cancel once this frame is reached and the attack has not ended (never during startup: the swing must commit first).</summary>
        public bool CanJumpCancel(int frame) => frame >= Math.Max(JumpCancelFrame, Startup) && frame < Total;

        public static AttackFrames FromSeconds(float activeStart, float activeEnd, float total, int jumpCancelFrame = 0)
        {
            int s = (int)Math.Round(activeStart * FixedStepper.Rate), e = (int)Math.Round(activeEnd * FixedStepper.Rate), t = (int)Math.Round(total * FixedStepper.Rate);
            return new AttackFrames { Startup = s, Active = Math.Max(1, e - s), Recovery = Math.Max(0, t - e), JumpCancelFrame = jumpCancelFrame > 0 ? jumpCancelFrame : e };
        }
    }

    /// <summary>
    /// Plays an <see cref="AttackFrames"/> out, one fixed step at a time. A hit freezes the attacker for <c>hitStopFrames</c> (the attacker only;
    /// the world keeps moving), and a second press during the last <c>bufferFrames</c> is remembered and starts the next swing at once.
    /// </summary>
    public class AttackTimeline
    {
        public AttackFrames Frames { get; private set; }
        public int Frame { get; private set; } = -1;
        public bool Running => Frame >= 0;
        public int FreezeLeft { get; private set; }
        public bool BufferedNext { get; private set; }
        public AttackPhase Phase => Running ? Frames.PhaseAt(Frame) : AttackPhase.None;
        public bool Frozen => FreezeLeft > 0;

        public void Start(AttackFrames frames) { Frames = frames; Frame = 0; FreezeLeft = 0; BufferedNext = false; }
        public void Cancel() { Frame = -1; FreezeLeft = 0; BufferedNext = false; }
        public bool CanJumpCancel => Running && Frames.CanJumpCancel(Frame);

        /// <summary>Called when the swing connects. Only the first hit of a swing freezes it, and a longer freeze never shortens a running one.</summary>
        public void Hit(int hitStopFrames) { if (hitStopFrames > FreezeLeft) FreezeLeft = hitStopFrames; }

        /// <summary>Attack pressed while swinging: remembered if it is in the buffer window before the end.</summary>
        public void PressAttack(int bufferFrames)
        {
            if (!Running) return;
            if (Frames.Total - Frame <= bufferFrames) BufferedNext = true;
        }

        /// <summary>Advance one fixed step. Returns true on the step the attack ends.</summary>
        public bool Tick()
        {
            if (!Running) return false;
            if (FreezeLeft > 0) { FreezeLeft--; return false; }
            Frame++;
            if (Frame >= Frames.Total) { Frame = -1; return true; }
            return false;
        }
    }

    /// <summary>The short stumble after a long fall: slower movement for a few frames, cancelled at once by a jump and early by a move.</summary>
    public struct LandLag
    {
        public int Total, Frame;
        public bool Active => Frame < Total;

        public static LandLag For(FeelValues v, float fallDistance, bool hardSurface = true)
            => new LandLag { Total = hardSurface && fallDistance >= v.landLagMinFall ? v.landLagFrames : 0, Frame = 0 };

        /// <summary>The speed multiplier right now (1 once it is over).</summary>
        public float SpeedMult(FeelValues v) => Active ? v.landLagSpeedMult : 1f;

        public bool CanCancel(FeelValues v, bool jumpPressed, bool moveInput) => Active && (jumpPressed || (moveInput && Frame >= v.landLagMoveCancelFrame));

        public LandLag Tick() { if (Frame < Total) Frame++; return this; }
    }
}
