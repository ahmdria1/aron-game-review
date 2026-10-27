// (c) Ahmed Alamrani. All rights reserved.
using Aron.Animals;
using Aron.Audio;
using Aron.Config;
using Aron.Core;
using Aron.Fx;
using Aron.Input;
using Aron.Logic.GameFeel;
using Aron.Save;
using UnityEngine;

namespace Aron.Player
{
    /// <summary>
    /// Third-person platformer controller: camera-relative run, jump (coyote + buffer + variable height),
    /// sprint, double jump, air spin, ground slide, melee weapon swing, damage/respawn.
    /// Every number that decides the feel (accel, skid, air control, jump arc, corner and ledge help, landing, spin and melee frames, bounces) is in
    /// <see cref="FeelSettings"/> (Resources/Config/game_feel.json, tuned live with the hidden panel), and the maths is in Aron.Logic.GameFeel
    /// where it is unit-tested; this class only reads the world (grounded, hits, camera) and feeds it in. See docs/game_feel.md.
    /// </summary>
    [RequireComponent(typeof(CharacterController))]
    public class PlayerController : MonoBehaviour
    {
        [SerializeField] private PlayerConfig config;
        [SerializeField] private Transform cameraTransform;
        [SerializeField] private Transform visualRoot;
        [SerializeField] private MeleeAttack melee;
        [SerializeField] private PlayerAnimationHooks anim;

        private CharacterController cc;
        private Vector3 velocity;                 // only .y is used: the vertical speed (the horizontal run lives in <see cref="run"/>)
        private readonly RunState run = new RunState();
        private JumpState jump;
        private readonly InputBuffers buffers = new InputBuffers();
        private readonly FixedStepper stepper = new FixedStepper();
        private readonly AttackTimeline spin = new AttackTimeline();
        private int spinCooldown;
        private LandLag landLag;
        private bool usedDoubleJump;
        private float slideTimer, slideCooldownTimer, invulnTimer;
        private float standHeight;
        private Vector3 standCenter;
        private Vector3 slideDir;
        private bool dead;
        private readonly Collider[] spinBuffer = new Collider[24];
        /// <summary>Upward speed (m/s) above which he counts as in the air for the attack button, even if the ground check has not caught up yet.</summary>
        private const float TakeOffRisingSpeed = 0.5f;
        private Renderer[] bodyRenderers;
        private bool wasGrounded = true;
        private float airPeakY;
        private float flatOutTime;
        private float aimLock;
        private float brakeTimer;
        private Rider rider;
        private ProceduralAnimator proc;
        private Vector3 groundNormal = Vector3.up;
        private float groundNormalTime = -10f, lastLiftTime = -10f;
        private bool liftPending;
        private float liftHeight;
        private Vector3 liftInward;
        private bool bounced;
        private bool lastJumpHeld, noCut, wasSkidding, lastFrozen;
        private double lastBounceTime = -100;
        private float lastBounceBase;
        private bool bounceBoosted, bounceBoostable;
        private int comboIndex;
        private readonly Collider[] overlapBuffer = new Collider[8];
        private float jumpSpeedHeight = -1f, jumpSpeedValue;
        private int jumpSpeedVersion = -1;
        private bool hasHitFrom;
        private Vector3 hitFrom;

        public bool IsSpinning => spin.Running;
        public bool IsSkidding => run.Skidding;
        public bool IsSliding => slideTimer > 0f;
        public bool IsAttacking => IsSpinning || IsSliding || (melee != null && melee.IsSwinging) || (rider != null && rider.IsCharging);
        public bool IsFalling => velocity.y < 0f && !cc.isGrounded;
        public bool IsInvulnerable => invulnTimer > 0f;
        public Vector3 Velocity => new Vector3(run.Velocity.X, velocity.y, run.Velocity.Z);
        public bool IsSprinting { get; private set; }
        public bool IsRiding => rider != null && rider.IsMounted;
        public float BodyHeight => cc != null ? cc.height : 1.8f;
        public Transform VisualRoot => visualRoot;
        public PlayerAnimationHooks Anim => anim;

        /// <summary>The player currently in the scene, so touch input can find him on screen.</summary>
        public static PlayerController Active { get; private set; }

        public void SetCamera(Transform cam) => cameraTransform = cam;
        /// <summary>Grip on the ground. Wet stone and mud in the rain take it down, so turns and stops drift.</summary>
        public float Traction { get; set; } = 1f;
        public PlayerConfig Config => config;
        /// <summary>Extra speed from companion abilities (camel sprint, horse charge). 1 is normal.</summary>
        public float SpeedBoost { get; set; } = 1f;
        /// <summary>Weather's share of speed (puddles, heat); 1 is normal. Kept apart from SpeedBoost so companions and weather never overwrite each other.</summary>
        public float WeatherSpeed { get; set; } = 1f;
        /// <summary>Wind leaning on him, in m/s (x sideways, z along the track); zero outside a storm.</summary>
        public Vector3 WindPush { get; set; }
        /// <summary>Asked before a hit lands; return true when something soaked it up (the camel's shield).</summary>
        public System.Func<bool> AbsorbHit { get; set; }
        public Rider Rider => rider;
        /// <summary>The golden bisht he (or Noura) is wearing.</summary>
        public GoldenBisht Bisht { get; private set; }

        private Logic.Heroes.HeroDef hero;
        private Logic.Heroes.JumpGlide glide;
        /// <summary>Who is being played. Sets the number of jumps, Noura's glide and her speed.</summary>
        public Logic.Heroes.HeroDef Hero
        {
            get => hero;
            set { hero = value; glide = value != null ? new Logic.Heroes.JumpGlide(value) : null; }
        }
        public bool IsGliding => glide != null && glide.Gliding;

        private void Awake()
        {
            cc = GetComponent<CharacterController>();
            standHeight = cc.height;
            standCenter = cc.center;
            if (melee == null) melee = GetComponent<MeleeAttack>();
            if (anim == null) anim = GetComponent<PlayerAnimationHooks>();
            rider = GetComponent<Rider>();
            proc = GetComponent<ProceduralAnimator>();
            if (proc == null) proc = gameObject.AddComponent<ProceduralAnimator>();
            Bisht = GetComponent<GoldenBisht>();
            if (Bisht == null) Bisht = gameObject.AddComponent<GoldenBisht>();
            if (config == null) config = ScriptableObject.CreateInstance<PlayerConfig>();
            Fx.DropShadow.Ensure(gameObject, 0.55f, 0f, true);
            if (cameraTransform == null && Camera.main != null) cameraTransform = Camera.main.transform;
        }

        private void OnEnable()
        {
            Active = this;
            if (melee == null) melee = GetComponent<MeleeAttack>();
            if (melee != null) melee.SwingStarted += OnBufferedSwing;
        }

        private void OnDisable()
        {
            if (Active == this) Active = null;
            if (melee != null) melee.SwingStarted -= OnBufferedSwing;
        }

        private void Update()
        {
            if (Time.timeScale <= 0f || dead) return;
            var fv = FeelSettings.V;
            PlayerIntent intent = InputRouter.Instance != null ? InputRouter.Instance.Refresh() : new PlayerIntent();
            float dt = Time.deltaTime;
            double now = Time.timeAsDouble;
            var access = SaveSystem.Data != null ? SaveSystem.Data.access : null;
            bool oneHand = access != null && access.oneHand;
            bool riding = IsRiding;
            ApplyControllerShape(fv, riding);

            // ---- Ground state
            bool grounded = cc.isGrounded;
            if (grounded)
            {
                buffers.Grounded(now);
                usedDoubleJump = false;
            }

            // ---- Fixed-step things: attack frames, spin cooldown, land lag
            int steps = stepper.Advance(dt);
            for (int i = 0; i < steps; i++)
            {
                if (spin.Running && spin.Tick()) EndSpin(fv);
                else if (!spin.Running && spinCooldown > 0) spinCooldown--;
                landLag.Tick();
            }

            // ---- Buffers
            if (intent.jumpPressed) buffers.PressJump(now);
            if (intent.attackPressed) buffers.PressAttack(now);

            // ---- Camera-relative desired move (touch steering is already shaped by dead zone and curve)
            Vector3 fwd = Vector3.forward, right = Vector3.right;
            if (cameraTransform != null)
            {
                fwd = Vector3.ProjectOnPlane(cameraTransform.forward, Vector3.up).normalized;
                right = Vector3.ProjectOnPlane(cameraTransform.right, Vector3.up).normalized;
            }
            Vector3 moveDir = fwd * intent.move.y + right * intent.move.x;     // length 0..1

            // ---- Sprint: held button, or a long run at full tilt
            bool flatOut = intent.move.sqrMagnitude > 0.8f;
            flatOutTime = flatOut ? flatOutTime + dt : 0f;
            IsSprinting = flatOut && !IsSliding && (intent.sprintHeld || flatOutTime >= fv.autoSprintDelay);
            float mult = (IsSprinting ? fv.sprintMultiplier : 1f) * SpeedBoost * WeatherSpeed * (hero != null ? hero.speed : 1f);
            if (riding)
            {
                // Pulling down on the rider reins the animal in instead of sliding.
                if (intent.slidePressed) brakeTimer = 0.8f;
                brakeTimer -= dt;
                mult *= rider.SpeedMultiplier * (brakeTimer > 0f ? 0.4f : 1f);
                if (!rider.State.CanControl) moveDir = Vector3.zero;
            }
            if (melee != null && melee.IsSwinging && melee.Phase != AttackPhase.Recovery) mult *= fv.meleeMoveMult;   // planted for the swing, free again in recovery
            if (landLag.Active)
            {
                if (landLag.CanCancel(fv, intent.jumpPressed || buffers.JumpBuffered(now, InputBuffers.JumpWindow(fv, oneHand)), moveDir.sqrMagnitude > 0.04f)) landLag.Frame = landLag.Total;
                else mult *= landLag.SpeedMult(fv);
            }
            if (grounded && Time.time - groundNormalTime < 0.2f && moveDir.sqrMagnitude > 1e-4f)
            {
                var downhill = new Vector3(groundNormal.x, 0f, groundNormal.z);
                if (downhill.sqrMagnitude > 1e-6f) mult *= Slopes.SpeedScale(fv, groundNormal.y, Vector3.Dot(moveDir.normalized, downhill.normalized));
            }
            float topSpeed = Mathf.Max(0.1f, fv.runSpeed * mult);
            var desired = new V2(moveDir.x * topSpeed, moveDir.z * topSpeed);

            // ---- Slide
            slideCooldownTimer -= dt;
            if (intent.slidePressed && !riding && grounded && !IsSliding && slideCooldownTimer <= 0f) StartSlide(new Vector3(desired.X, 0f, desired.Z));
            if (IsSliding)
            {
                slideTimer -= dt;
                run.Velocity = V2.MoveTowards(run.Velocity, new V2(slideDir.x, slideDir.z) * config.slideSpeed, fv.accelGround * 3f * dt);
                run.Phase = RunPhase.Cruising;
                SpinOrSlideHits(DamageKind.Slide, 0.9f);
                if (slideTimer <= 0f) EndSlide();
            }
            else if (grounded)
            {
                // A rider's acceleration multiplier speeds up or slows down every rate the same way, so it scales the step.
                Locomotion.StepGround(fv, run, desired, dt * (riding ? rider.AccelerationMultiplier : 1f), topSpeed, Traction);
            }
            else Locomotion.StepAir(fv, run, desired, dt, JumpArc.AtApex(fv, velocity.y));

            bool skidding = run.Skidding;
            if (skidding != wasSkidding)
            {
                if (skidding) FxPlayer.Play("dust", transform.position);
                anim?.OnSkid(skidding);
                wasSkidding = skidding;
            }

            // ---- Facing: turns to where he wants to go (so a reversal reads at once), holds while skidding or aiming
            aimLock -= dt;
            V2 face = desired.SqrLength > 0.01f ? desired : run.Velocity;
            if (face.SqrLength > 0.01f && aimLock <= 0f && !skidding)
            {
                Quaternion target = Quaternion.LookRotation(new Vector3(face.X, 0f, face.Z));
                transform.rotation = Quaternion.RotateTowards(transform.rotation, target, Locomotion.TurnRate(fv, run.Phase, grounded) * dt);
            }

            // ---- Jump / double jump
            float jumpWindow = InputBuffers.JumpWindow(fv, oneHand), coyoteWindow = InputBuffers.CoyoteWindow(fv, oneHand);
            // A late press right after a bounce upgrades it to the high one (and is used up doing so).
            if (intent.jumpPressed && bounceBoostable && velocity.y > 0f && BounceMath.LateBoostAllowed(fv, (float)(now - lastBounceTime), bounceBoosted))
            {
                bounceBoosted = true;
                buffers.ConsumeJump();
                LaunchTo(fv, BounceMath.Height(fv, lastBounceBase, true, 0f));
            }
            bool attackHoldsJump = (spin.Running && !spin.CanJumpCancel) || (melee != null && melee.IsSwinging && !melee.CanJumpCancel);
            if (buffers.JumpBuffered(now, jumpWindow) && !attackHoldsJump)
            {
                if (buffers.CoyoteOpen(now, coyoteWindow))
                {
                    if (IsSliding) EndSlide();
                    CancelAttacksForJump(fv);
                    DoJump(fv, riding ? rider.JumpMultiplier : 1f);
                    buffers.Left();
                    glide?.TryJump(true);
                    FxPlayer.Play("dust", transform.position);
                    if (riding) rider.Jump(); else anim?.Trigger("Jump");
                    GameEvents.PlaySfx("jump");
                }
                else if ((hero != null ? hero.jumps >= 2 : config.allowDoubleJump) && !riding && !usedDoubleJump && !grounded)
                {
                    usedDoubleJump = true;
                    CancelAttacksForJump(fv);
                    DoJump(fv, fv.doubleJumpMultiplier);
                    // Twice, so walking off a ledge and jumping once in the air still counts as her last jump.
                    if (glide != null) { glide.TryJump(false); glide.TryJump(false); }
                    anim?.Trigger("DoubleJump"); GameEvents.PlaySfx("doublejump");
                }
            }

            // ---- Attack: weapon on ground, spin in the air
            if (intent.attackPressed) GameEvents.RaisePlayerAttacked();
            if (intent.attackPressed && riding)
            {
                if (intent.hasAim) FaceTowards(intent.aimPoint);
                rider.Attack();
            }
            else if (intent.attackPressed && !IsSliding)
            {
                // Airborne also covers the first frames of a jump: the ground check still says "grounded" on the frame he takes off (or the next),
                // and a press then used to start a ground swing that the jump cancelled, so neither the spin nor the swing hit anything.
                bool airborne = !grounded || velocity.y > TakeOffRisingSpeed;
                if (airborne && !spin.Running && spinCooldown <= 0)
                {
                    spin.Start(AttackFrames.Spin(fv));
                    if (velocity.y < 0f) velocity.y *= fv.spinFallDamping; // brief hang time while spinning
                    jump.VelocityY = velocity.y;
                    anim?.Trigger("Spin"); GameEvents.PlaySfx("spin");
                }
                else if (!airborne && melee != null)
                {
                    if (melee.TryStartSwing())
                    {
                        comboIndex = 0;
                        if (intent.hasAim) FaceTowards(intent.aimPoint);
                        FireSwingAnim();
                    }
                }
            }
            if (spin.Running)
            {
                if (spin.Phase == AttackPhase.Active && !spin.Frozen)
                {
                    if (visualRoot != null) visualRoot.Rotate(0f, fv.spinTurnSpeed * dt, 0f, Space.Self);
                    SpinOrSlideHits(DamageKind.Spin, fv.spinRadius);
                }
            }
            bool frozen = melee != null && melee.Frozen;
            if (frozen != lastFrozen) { anim?.SetSpeed(frozen ? 0f : 1f); lastFrozen = frozen; }

            // ---- Vertical: the shaped jump arc, in sub-steps so the peak does not depend on the frame rate
            bool held = intent.jumpHeld || noCut;
            float frameDy;                                 // how far he moves up this frame, summed over the sub-steps
            if (grounded && velocity.y <= 0f)
            {
                velocity.y = -StickSpeed(fv);
                jump = new JumpState { VelocityY = velocity.y };
                noCut = false; bounceBoostable = false;
                frameDy = velocity.y * dt;
            }
            else
            {
                jump.VelocityY = velocity.y;
                bool fast = intent.slidePressed && !riding && velocity.y <= 0.5f;
                jump = JumpArc.Advance(fv, jump, dt, held, fast, out frameDy);
                velocity.y = jump.VelocityY;
                if (velocity.y <= 0f) noCut = false;
            }
            if (glide != null)
            {
                // Noura opens her abaya after her second jump and floats down while a finger stays on the screen.
                float? cap = riding ? null : glide.Tick(dt, cc.isGrounded, intent.fingerDown, velocity.y);
                if (cap.HasValue && velocity.y < cap.Value) { velocity.y = cap.Value; jump.VelocityY = velocity.y; frameDy = velocity.y * dt; }
            }

            // ---- Integrate
            Vector3 move = new Vector3(run.Velocity.X, 0f, run.Velocity.Z) + WindPush + SteepSlope(fv);
            Vector3 frameMove = move * dt;
            frameMove.y = frameDy;                         // not speed x frame time: the sub-stepped distance, so the height is the same at any frame rate
            var flags = cc.Move(frameMove);
            if (liftPending) ApplyLedgeLift(fv);
            if ((flags & CollisionFlags.Above) != 0 && velocity.y > 0f) HeadBonk(fv);
            if (cc.isGrounded) bounced = false;
            TrackLanding(fv);
            lastJumpHeld = intent.jumpHeld;

            // ---- Damage blink
            if (invulnTimer > 0f)
            {
                invulnTimer -= dt;
                SetVisible(invulnTimer <= 0f || Aron.Logic.Access.FlashSafety.BlinkOn(Aron.Save.SaveSystem.Data.access, invulnTimer, 10f, 0.7f));
            }

            if (anim != null) DriveAnimator(fv, desired, riding);
        }

        private void DriveAnimator(FeelValues fv, V2 desired, bool riding)
        {
            float speed = run.Velocity.Length;
            anim.SetFloat(PlayerAnimationHooks.Speed, riding ? 0f : speed / Mathf.Max(0.1f, fv.runSpeed));
            anim.SetBool(PlayerAnimationHooks.Grounded, cc.isGrounded);
            anim.SetBool(PlayerAnimationHooks.Sliding, IsSliding);
            anim.SetFloat(PlayerAnimationHooks.VerticalSpeed, velocity.y);
            anim.SetBool(PlayerAnimationHooks.Sprinting, IsSprinting);
            anim.SetBool(PlayerAnimationHooks.Gliding, IsGliding);
            anim.SetBool(PlayerAnimationHooks.FastFall, jump.FastFall && !cc.isGrounded);
            float turn = 0f;
            if (desired.SqrLength > 0.01f) turn = Vector3.SignedAngle(transform.forward, new Vector3(desired.X, 0f, desired.Z), Vector3.up);
            anim.SetFloat(PlayerAnimationHooks.TurnAngle, turn);
            anim.SetFloat(PlayerAnimationHooks.SlopeAngle, cc.isGrounded && Time.time - groundNormalTime < 0.2f ? Slopes.AngleDegrees(groundNormal.y) : 0f);
            anim.SetFloat(PlayerAnimationHooks.Wind, Mathf.Clamp01(WindPush.magnitude / 6f));
        }

        // ---- Collision helpers (the decisions are in Aron.Logic.GameFeel; here only the probes)
        private void ApplyControllerShape(FeelValues fv, bool riding)
        {
            if (!riding) cc.stepOffset = Mathf.Min(fv.stepUp, Mathf.Max(0.01f, cc.height - 0.01f));
            cc.slopeLimit = fv.slopeLimit;
        }

        private bool CapsuleClear(Vector3 offset)
        {
            Vector3 c = transform.position + cc.center + offset;
            float half = Mathf.Max(0f, cc.height * 0.5f - cc.radius);
            int n = Physics.OverlapCapsuleNonAlloc(c + Vector3.up * half, c - Vector3.up * half, cc.radius * 0.9f, overlapBuffer, ~0, QueryTriggerInteraction.Ignore);
            for (int i = 0; i < n; i++) if (!overlapBuffer[i].transform.IsChildOf(transform)) return false;
            return true;
        }

        /// <summary>Jumped up into the very edge of a ceiling: slide around the corner if there is room within the nudge limit, else bounce off it.</summary>
        private void HeadBonk(FeelValues fv)
        {
            Vector3[] dirs = { Vector3.right, Vector3.left, Vector3.forward, Vector3.back };
            float best = 0f; Vector3 bestDir = Vector3.zero;
            for (float o = 0.05f; o <= fv.headCornerNudge + 0.001f && best <= 0f; o += 0.05f)
            {
                foreach (var d in dirs)
                {
                    if (!CapsuleClear(d * o + Vector3.up * 0.12f)) continue;
                    float shift = CornerCorrection.Nudge(fv, o, 0.02f, velocity.y);
                    if (shift > 0f) { best = shift; bestDir = d; break; }
                }
            }
            if (best > 0f)
            {
                cc.Move(bestDir * best);            // keep the upward speed: the jump just carries on past the corner
                anim?.OnBonk();
                return;
            }
            velocity.y = CornerCorrection.AfterBonk(fv, velocity.y);
            jump.VelocityY = velocity.y;
            noCut = false;
            anim?.OnBonk();
            FxPlayer.Play("dust", transform.position + Vector3.up * cc.height);
        }

        private void ApplyLedgeLift(FeelValues fv)
        {
            liftPending = false;
            if (!CapsuleClear(Vector3.up * (liftHeight + 0.05f) + liftInward)) return;
            cc.Move(Vector3.up * liftHeight + liftInward);
            if (velocity.y < 0f) { velocity.y = 0f; jump.VelocityY = 0f; }
            run.Velocity = new V2(LedgeForgiveness.KeepSpeed(fv, run.Velocity.X), LedgeForgiveness.KeepSpeed(fv, run.Velocity.Z));
            lastLiftTime = Time.time;
            anim?.OnLedgePop();
            FxPlayer.Play("dust", transform.position);
        }

        private Vector3 SteepSlope(FeelValues fv)
        {
            if (Time.time - groundNormalTime > 0.15f) return Vector3.zero;
            float s = Slopes.SlideSpeed(fv, groundNormal.y);
            if (s <= 0f) return Vector3.zero;
            var downhill = new Vector3(groundNormal.x, 0f, groundNormal.z);
            return downhill.sqrMagnitude > 1e-6f ? downhill.normalized * s : Vector3.zero;
        }

        /// <summary>How hard he is pressed onto the ground: more when running down a slope (see Slopes.StickSpeed).</summary>
        private float StickSpeed(FeelValues fv)
        {
            if (Time.time - groundNormalTime >= 0.2f) return fv.groundStickSpeed;
            var downhill = new Vector3(groundNormal.x, 0f, groundNormal.z);
            float speed = run.Velocity.Length;
            if (downhill.sqrMagnitude < 1e-6f || speed < 0.1f) return fv.groundStickSpeed;
            float dot = (run.Velocity.X * downhill.normalized.x + run.Velocity.Z * downhill.normalized.z) / speed;
            return Slopes.StickSpeed(fv, speed, groundNormal.y, dot);
        }

        private void CancelAttacksForJump(FeelValues fv)
        {
            if (spin.Running) { spin.Cancel(); EndSpin(fv, false); }
            if (melee != null && melee.IsSwinging) melee.CancelForJump();
        }

        private void EndSpin(FeelValues fv, bool startCooldown = true)
        {
            if (visualRoot != null) visualRoot.localRotation = Quaternion.identity;
            if (startCooldown) spinCooldown = fv.spinCooldownFrames;
            anim?.Trigger("SpinEnd");
        }

        private void OnBufferedSwing() { comboIndex++; FireSwingAnim(); }

        private void FireSwingAnim()
        {
            if (anim == null || melee == null) return;
            string baseTrigger = melee.Weapon != null ? melee.Weapon.animatorTrigger : "Attack";
            if ((comboIndex & 1) == 1 && anim.TryTrigger("Attack2")) return;
            anim.Trigger(baseTrigger);
        }

        private void LaunchTo(FeelValues fv, float height)
        {
            float v0 = JumpSpeed(fv, height);
            velocity.y = v0;
            jump = JumpArc.Launch(v0);
            noCut = true;
        }

        /// <summary>The take-off speed that peaks at exactly this height with the shaped arc (cached until a value changes).</summary>
        private float JumpSpeed(FeelValues fv, float height)
        {
            if (jumpSpeedVersion != FeelSettings.Version || !Mathf.Approximately(jumpSpeedHeight, height))
            {
                jumpSpeedValue = JumpArc.VelocityForHeight(fv, height);
                jumpSpeedHeight = height; jumpSpeedVersion = FeelSettings.Version;
            }
            return jumpSpeedValue;
        }

        private void FaceTowards(Vector3 point)
        {
            Vector3 d = point - transform.position;
            d.y = 0f;
            if (d.sqrMagnitude < 0.01f) return;
            transform.rotation = Quaternion.LookRotation(d);
            aimLock = 0.3f; // hold the facing for the swing instead of turning back to the run direction
        }

        private void TrackLanding(FeelValues fv)
        {
            bool g = cc.isGrounded;
            if (!g) airPeakY = wasGrounded ? transform.position.y : Mathf.Max(airPeakY, transform.position.y);
            else if (!wasGrounded)
            {
                float fall = airPeakY - transform.position.y;
                var feel = LandingFeel.From(fv, fall);
                for (int i = 0; i < feel.DustBursts; i++)
                    FxPlayer.Play("dust", transform.position + new Vector3(Random.Range(-0.3f, 0.3f), 0.05f * i, Random.Range(-0.3f, 0.3f)));
                if (feel.Shake) FollowCamera.Shake(0.15f);
                if (feel.Squash > 0f) { proc?.OnLanded(feel.Squash); anim?.OnLanded(feel.Lag); }
                landLag = LandLag.For(fv, fall, !IsRiding);   // a stumble only after a long fall, not on a mount
                noCut = false;
            }
            wasGrounded = g;
        }

        private void SetVisible(bool visible)
        {
            if (visualRoot == null) return;
            if (bodyRenderers == null || bodyRenderers.Length == 0) bodyRenderers = visualRoot.GetComponentsInChildren<Renderer>(true);
            foreach (var r in bodyRenderers) if (r != null) r.enabled = visible;
        }

        private bool Squashes => visualRoot != null && (anim == null || !anim.HasAnimator);

        private void DoJump(FeelValues fv, float mult)
        {
            float v0 = JumpSpeed(fv, fv.jumpHeight * mult);
            velocity.y = v0;
            jump = JumpArc.Launch(v0);
            buffers.ConsumeJump();
            bounced = false;
            noCut = false;
            bounceBoostable = false;
            proc?.OnJump();
        }

        /// <summary>A plain bounce to this height (thrown off a mount...). Ignores jump-release damping.</summary>
        public void Bounce(float height) => BounceTo(FeelSettings.V, height, false);

        /// <summary>Spring crates: the same height every time (Game Feel > crateBounceHeight); jump held at the impact, or pressed just before or just after, gives the high bounce.</summary>
        public void BounceCrate() => BounceTo(FeelSettings.V, FeelSettings.V.crateBounceHeight, true);

        /// <summary>Landing on an enemy's head. Same rule as the crate: holding jump bounces higher.</summary>
        public void StompBounce() { BounceTo(FeelSettings.V, FeelSettings.V.stompBounceHeight, true); anim?.OnStomp(); }

        private void BounceTo(FeelValues fv, float baseHeight, bool boostable)
        {
            double now = Time.timeAsDouble;
            float h = boostable ? BounceMath.Height(fv, baseHeight, lastJumpHeld, buffers.SinceJumpPress(now)) : baseHeight;
            bounceBoosted = boostable && h > baseHeight + 1e-4f;
            if (bounceBoosted) buffers.ConsumeJump();
            lastBounceTime = now; lastBounceBase = baseHeight; bounceBoostable = boostable;
            LaunchTo(fv, h);
            usedDoubleJump = false;
            bounced = true;
            anim?.OnBounce();
            GameEvents.PlaySfx("bounce");
        }

        private void StartSlide(Vector3 desired)
        {
            slideDir = desired.sqrMagnitude > 0.01f ? desired.normalized : transform.forward;
            slideTimer = config.slideDuration;
            slideCooldownTimer = config.slideDuration + config.slideCooldown;
            cc.height = config.slideHeight;
            cc.center = new Vector3(standCenter.x, standCenter.y - (standHeight - config.slideHeight) * 0.5f, standCenter.z);
            if (Squashes) visualRoot.localScale = new Vector3(1f, config.slideHeight / standHeight, 1f);
            FxPlayer.Play("dust", transform.position);
            anim?.Trigger("Slide");
            GameEvents.PlaySfx("spin");
        }

        private void EndSlide()
        {
            slideTimer = 0f;
            // Only stand up if there is room above.
            Vector3 top = transform.position + Vector3.up * (standHeight - cc.radius);
            if (Physics.CheckSphere(top, cc.radius * 0.9f, ~0, QueryTriggerInteraction.Ignore))
            {
                slideTimer = 0.1f; // keep sliding under the obstacle
                return;
            }
            cc.height = standHeight;
            cc.center = standCenter;
            if (visualRoot != null) visualRoot.localScale = Vector3.one;
        }

        private void SpinOrSlideHits(DamageKind kind, float radius)
        {
            int n = Physics.OverlapSphereNonAlloc(transform.position + Vector3.up * 0.6f, radius, spinBuffer, ~0, QueryTriggerInteraction.Collide);
            for (int i = 0; i < n; i++)
            {
                if (spinBuffer[i].transform.IsChildOf(transform)) continue;
                var d = spinBuffer[i].GetComponentInParent<IDamageable>();
                if (d != null && d.CanBeDamaged) d.TakeDamage(new DamageInfo(1, kind, transform.position, gameObject));
            }
        }

        private void OnControllerColliderHit(ControllerColliderHit hit)
        {
            var fv = FeelSettings.V;
            float feetY = transform.position.y + cc.center.y - cc.height * 0.5f;
            if (hit.normal.y > 0.3f && hit.point.y <= feetY + 0.35f) { groundNormal = hit.normal; groundNormalTime = Time.time; }
            else if (!cc.isGrounded && Mathf.Abs(hit.normal.y) < 0.25f && !liftPending && !IsRiding && fv.ledgeForgiveHeight > 0f)
            {
                // The feet struck the side of a platform just under its top edge: lift him onto it (ledge forgiveness, not coyote time).
                Vector3 into = new Vector3(-hit.normal.x, 0f, -hit.normal.z);
                if (into.sqrMagnitude > 1e-4f && Vector3.Dot(new Vector3(run.Velocity.X, 0f, run.Velocity.Z), into.normalized) > 0.5f)
                {
                    float lift = LedgeForgiveness.Lift(fv, feetY, hit.collider.bounds.max.y, velocity.y, Time.time - lastLiftTime);
                    if (lift > 0f) { liftPending = true; liftHeight = lift; liftInward = into.normalized * 0.3f; }
                }
            }
            // Landing on top of something damageable = stomp.
            if (hit.normal.y > 0.6f && velocity.y <= 0f)
            {
                var d = hit.collider.GetComponentInParent<IDamageable>();
                if (d != null && d.CanBeDamaged)
                {
                    StompBounce(); // targets (spring crates) may override with a bigger bounce
                    d.TakeDamage(new DamageInfo(1, DamageKind.Stomp, transform.position, gameObject));
                }
            }
        }

        /// <summary>A hit that knows where it came from, so the flinch and the hurt animation lean the right way.</summary>
        public void Hurt(Vector3 fromWorld, bool ignoreAttackProtection = false)
        {
            hasHitFrom = true; hitFrom = fromWorld;
            Hurt(ignoreAttackProtection);
            hasHitFrom = false;
        }

        // The hit did not end the run: an additive flinch and the directional hurt clip (front when the source is unknown).
        private void HitReaction(float power)
        {
            Vector3 from = hasHitFrom ? hitFrom : transform.position + transform.forward;
            proc?.OnHit(from, power);
            Vector3 local = transform.InverseTransformPoint(from);
            anim?.OnHit(local.x, local.z);
        }

        /// <summary>Called by enemies / hazards. Spinning or sliding protects the player.</summary>
        public void Hurt(bool ignoreAttackProtection = false)
        {
            if (dead || invulnTimer > 0f) return;
            if (!ignoreAttackProtection && (IsSpinning || IsSliding)) return;
            if (Bisht != null && Bisht.Invincible) return;
            if (Bisht != null && Bisht.TakeHit())
            {
                invulnTimer = config.invulnerableTime;
                FollowCamera.Shake(0.15f);
                HitReaction(0.8f);
                return;
            }
            if (AbsorbHit != null && AbsorbHit())
            {
                invulnTimer = config.invulnerableTime;
                FollowCamera.Shake(0.12f);
                GameEvents.PlaySfx("bounce");
                HitReaction(0.5f);
                return;
            }
            if (rider != null)
            {
                // On a camel or horse the animal takes the hit and throws him off; no life lost.
                var outcome = rider.Hit();
                if (outcome == Aron.Logic.HitOutcome.Ignored) return;
                if (outcome == Aron.Logic.HitOutcome.ThrownOff)
                {
                    invulnTimer = config.invulnerableTime;
                    FollowCamera.Shake(0.2f);
                    GameEvents.PlaySfx("hit");
                    HitReaction(1f);
                    return;
                }
            }
            anim?.Trigger("Hurt");
            Die();
        }

        public bool IsDead => dead;

        public void Die() => Die(Aron.Logic.DeathCause.Enemy);

        /// <summary>Plays the knock-out act for the cause, then hands over to the game manager for the respawn.</summary>
        public void Die(Aron.Logic.DeathCause cause)
        {
            if (dead) return;
            dead = true;
            IsSprinting = false; flatOutTime = 0f;
            ResetMotion();
            if (rider != null) rider.ForceOff();
            anim?.SetBool(PlayerAnimationHooks.Dead, true);
            anim?.Trigger("Die");
            string hero = Heroes.HeroTeam.Instance != null ? Heroes.HeroTeam.Instance.Active : "aron";
            GameEvents.PlaySfx(Aron.Logic.AudioCues.ForDeath(cause, hero));
            if (cause == Aron.Logic.DeathCause.Pit && hero != "noura") AudioManager.PlayVoice(Aron.Logic.AudioCues.VoFallWave, hero);
            if (hero == "noura") AudioManager.PlayVoice(Aron.Logic.AudioCues.VoUff, hero);
            if (deathShow == null) deathShow = GetComponent<DeathShow>();
            if (deathShow == null) deathShow = gameObject.AddComponent<DeathShow>();
            deathShow.Play(visualRoot, hero, cause, AfterDeath);
        }

        private DeathShow deathShow;

        private void AfterDeath()
        {
            if (GameManager.Instance != null) GameManager.Instance.PlayerDied(this);
            else Respawn(transform.position + Vector3.up);
        }

        /// <summary>Moves him without the respawn blink, for swapping places with the partner.</summary>
        public void Teleport(Vector3 position)
        {
            cc.enabled = false;
            transform.position = position;
            cc.enabled = true;
            ResetMotion();
        }

        /// <summary>Stops all motion and the short-lived state (run, jump arc, attacks, buffers) for a respawn, a swap or a death.</summary>
        private void ResetMotion()
        {
            velocity = Vector3.zero;
            run.Velocity = default; run.Phase = RunPhase.Idle;
            jump = default; landLag = default;
            spin.Cancel(); spinCooldown = 0; wasSkidding = false;
            liftPending = false; noCut = false; bounceBoostable = false;
            buffers.ConsumeJump(); buffers.ConsumeAttack(); buffers.Left();
            stepper.Reset();
            if (visualRoot != null && !IsSliding) visualRoot.localRotation = Quaternion.identity;
        }

        public bool IsGrounded => cc != null && cc.isGrounded;

        public void Respawn(Vector3 position)
        {
            cc.enabled = false;
            transform.position = position;
            cc.enabled = true;
            ResetMotion();
            brakeTimer = 0f;
            anim?.SetBool(PlayerAnimationHooks.Dead, false);
            anim?.SetSpeed(1f); lastFrozen = false;
            proc?.ResetAll();
            if (rider != null) rider.ForceOff();
            if (IsSliding) { slideTimer = 0f; cc.height = standHeight; cc.center = standCenter; if (visualRoot != null) visualRoot.localScale = Vector3.one; }
            invulnTimer = config.invulnerableTime;
            dead = false;
        }
    }
}
