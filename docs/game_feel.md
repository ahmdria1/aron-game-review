# Game feel

How the hero controls, why every number is what it is, and how to tune it on a phone. The code is in `Assets/_Aron/Scripts/Logic/GameFeel` (the maths, no Unity types, unit-tested in `Tests/Aron.Logic.Tests/GameFeelTests.cs`) and the Unity side in `Player/PlayerController.cs`, `Player/MeleeAttack.cs`, `Player/FollowCamera.cs`, `Player/Procedural/*` and `Diagnostics/FeelTuningPanel.cs`.

## How it is built

- **One place for every value.** `FeelValues` (`Logic/GameFeel/FeelValues.cs`) holds all of them, each with a panel group, a slider range and a unit. At run time they live in the `GameFeelConfig` ScriptableObject (`Resources/Config/GameFeelConfig` if one exists, otherwise `Resources/Config/game_feel.json`), loaded by the static `FeelSettings`. A tuning saved from the phone is loaded over them (`game_feel_tuning.json` in the app's data folder). Nothing about the feel is left in a scene or in `PlayerConfig` any more (that keeps only the slide numbers, the double-jump switch and the invulnerability time).
- **Pure maths, thin controller.** `Locomotion`, `JumpArc`, `InputBuffers`, `AttackTimeline`, `LandLag`, `CornerCorrection`, `LedgeForgiveness`, `Slopes`, `LandingFeel`, `BounceMath`, `LookAheadModel`, `VerticalFollowModel`, `BossFraming` and `InputShaping` take the values and numbers in, and return numbers; the controller only measures the world (grounded, hits, camera, probes) and feeds them in.
- **Time.** Jump physics integrates in sub-steps of at most 1/120 s, so the peak height does not depend on the frame rate. Attack frames run on a 60 Hz `FixedStepper` (at most 4 steps per rendered frame); frame counts in the table are 60 Hz frames. Animation clips are authored at 30 fps (see `docs/animation_list.md`).
- **Reduce-motion and the Low tier** (`access.reduceMotion`, `DeviceSettings.Tier`) cut the optional motion: hit-stop, squash, flinch and breath scale by `reduceMotionScale`, and the Low tier turns off foot IK and cloth.

## References behind the defaults

These are the games the numbers were reasoned from. They are starting points to tune by hand, not measurements of those games.

- **Celeste** (Maddy Thorson's published notes on its movement): jump buffer, coyote time, corner correction, half-gravity at the apex, a different air acceleration, a variable jump height from releasing the button, fast fall.
- **Super Mario World / Odyssey**: a skid when reversing at speed, look-ahead in the run direction, a short hold on the camera after stopping, a higher bounce when jump is held on a stomp.
- **Crash Bandicoot**: a fixed chase camera that follows the floor and not every hop; spring crates that always bounce to the same height; precise falls.
- **Fighting-game frame data**: startup, active and recovery frames, cancel windows and input buffering, and a short hit-stop on contact.

## Values by group

The defaults are in `Resources/Config/game_feel.json`; the table lists the slider range and unit. Panel group names are the headings.

### Run

Acceleration is a curve, not a constant: it is `accelGround` at a standstill and falls to `accelGround × accelTopShare` near top speed (`accelCurve` shapes it), so the first steps are snappy and the last few m/s feel like building momentum (Mario's run-up, Celeste's `RunAccel`/`RunReduce`). Braking (`decelGround`) is stronger than accelerating, so letting go of the stick stops him in a few frames: on a touch screen there is no force feedback, so a hero that keeps sliding reads as lag. A reversal above `skidMinSpeed` and `skidAngle` is a skid (Mario's brake turn): input is ignored for at most `skidMaxTime`, he brakes at `skidDecel`, dust and the `Skidding` animation play, then he accelerates the other way. A slow reversal is a turn in place instead (`turnInPlaceSpeed`), so a tap in the other direction never costs a skid. Facing turns at `turnSpeed` toward the *wanted* direction, not the current velocity, so the model answers at once even while the body is still braking.

| Value | Default | Range | Unit |
|---|---:|---|---|
| `runSpeed` | 7 | 3 to 14 | m/s |
| `sprintMultiplier` | 1.3 | 1 to 2 | x |
| `autoSprintDelay` | 1.6 | 0 to 6 | s |
| `accelGround` | 46 | 10 to 140 | m/s2 |
| `accelTopShare` | 0.55 | 0.1 to 1 | x |
| `accelCurve` | 1.5 | 0.5 to 3 | - |
| `decelGround` | 60 | 10 to 160 | m/s2 |
| `turnSpeed` | 760 | 180 to 1600 | deg/s |
| `turnInPlaceSpeed` | 1.6 | 0 to 5 | m/s |
| `turnInPlaceAccel` | 90 | 40 to 160 | m/s2 |
| `skidMinSpeed` | 4.5 | 1 to 12 | m/s |
| `skidAngle` | 120 | 60 to 179 | deg |
| `skidDecel` | 36 | 5 to 120 | m/s2 |
| `skidExitSpeed` | 1.2 | 0 to 4 | m/s |
| `skidMaxTime` | 0.4 | 0.05 to 1 | s |
| `skidRecoverSeconds` | 0.08 | 0 to 1 | s |

### Air

Air control is separate from ground control (`airAccel`, `airDecel`): lower deceleration so a jump keeps its momentum, higher acceleration so a mid-air correction still works with a thumb that is not precise. `airReverseMult` helps when the stick is pushed against the motion (Celeste lets you cancel a jump's momentum this way). `apexAccelMult` raises air acceleration while at the apex, where the player is lining up a landing (Celeste's apex control).

| Value | Default | Range | Unit |
|---|---:|---|---|
| `airAccel` | 30 | 0 to 100 | m/s2 |
| `airDecel` | 9 | 0 to 60 | m/s2 |
| `airReverseMult` | 1.4 | 1 to 3 | x |
| `airSpeedMult` | 1 | 0.5 to 1.3 | x |
| `airTurnSpeed` | 560 | 180 to 1600 | deg/s |
| `apexAccelMult` | 1.35 | 1 to 3 | x |

### Jump

Height is what a designer cares about, so `jumpHeight` is the *peak of the whole arc*; the launch speed is solved from it by integrating the shaped gravity (`JumpArc.VelocityForHeight`), so changing the fall multiplier does not silently change how high he goes. Gravity is shaped as in Celeste and Super Mario World: `riseGravityMult` while rising, a cut multiplier (`jumpCutGravityMult` after `jumpCutDelay`) when the button is released, a low-gravity band `apexGravityMult` below `apexThreshold` m/s for the apex hang, a heavier `fallGravityMult` on the way down (falling faster than rising feels weighty and gives precise landings) and a `maxFallSpeed` cap. Down on the slide button in the air gives a fast fall (`fastFallGravityMult`, `fastFallMaxSpeed`). The arc is sub-stepped at 120 Hz or finer so the peak is the same at 30, 60 and 120 fps (tested). Coyote time (`coyoteTime`) and the jump buffer (`jumpBuffer`) are 0.12 s and 0.15 s (see [The tuning pass](#the-tuning-pass)); one-hand mode adds a little to both.

| Value | Default | Range | Unit |
|---|---:|---|---|
| `jumpHeight` | 2.2 | 0.5 to 5 | m |
| `doubleJumpMultiplier` | 0.85 | 0.5 to 1.2 | x |
| `gravity` | 30 | 10 to 90 | m/s2 |
| `riseGravityMult` | 1 | 0.4 to 1.5 | x |
| `jumpCutGravityMult` | 2.8 | 1 to 6 | x |
| `jumpCutDelay` | 0.08 | 0 to 0.2 | s |
| `apexThreshold` | 1.6 | 0 to 8 | m/s |
| `apexGravityMult` | 0.6 | 0.15 to 1 | x |
| `fallGravityMult` | 1.55 | 1 to 3 | x |
| `fastFallGravityMult` | 2.2 | 1 to 5 | x |
| `maxFallSpeed` | 28 | 8 to 60 | m/s |
| `fastFallMaxSpeed` | 38 | 8 to 80 | m/s |
| `coyoteTime` | 0.12 | 0 to 0.3 | s |
| `jumpBuffer` | 0.15 | 0 to 0.35 | s |

### Corners

Corner correction is Celeste's: a jump that clips the edge of a ceiling by less than `headCornerNudge` metres is slid around the corner instead of bonking; a real bonk sets a small downward speed (`headBonkDownSpeed`). Ledge forgiveness is *not* coyote time: coyote time lets you jump after leaving a ledge, ledge forgiveness lifts you *onto* a ledge when your feet hit its side within `ledgeForgiveHeight` of its top while rising slower than `ledgeForgiveMaxRise`, keeping the run speed (`ledgeForgiveKeepSpeed`) and with a `ledgeForgiveCooldown` so it cannot chain up a wall. Both are decided in `Collision.cs` from numbers the controller measures with a few overlap probes.

| Value | Default | Range | Unit |
|---|---:|---|---|
| `headCornerNudge` | 0.3 | 0 to 0.7 | m |
| `headBonkDownSpeed` | 2 | 0 to 8 | m/s |
| `ledgeForgiveHeight` | 0.35 | 0 to 0.9 | m |
| `ledgeForgiveMaxRise` | 3 | 0 to 10 | m/s |
| `ledgeForgiveCooldown` | 0.12 | 0 to 0.5 | s |
| `ledgeForgiveKeepSpeed` | 1 | 0 to 1 | x |

### Slopes

The character controller's `slopeLimit` and `stepOffset` are set from `slopeLimit` and `stepUp`. Uphill he slows by up to `slopeUphillSlow`, downhill he speeds up by `slopeDownhillBoost` (small, so a ramp never feels like ice). A slope steeper than the limit slides him down at a speed that grows with the angle, so he cannot stand on it.

| Value | Default | Range | Unit |
|---|---:|---|---|
| `stepUp` | 0.45 | 0 to 0.9 | m |
| `slopeLimit` | 50 | 25 to 75 | deg |
| `slopeUphillSlow` | 0.25 | 0 to 0.7 | x |
| `slopeDownhillBoost` | 0.1 | 0 to 0.5 | x |
| `slopeSlideSpeed` | 8 | 0 to 20 | m/s |
| `groundStickSpeed` | 6 | 0 to 15 | m/s |

### Landing

Landings communicate weight. Below `landFxMinFall` metres nothing happens; above it a squash (up to `landSquashMax`) and a dust burst scale with the fall; a long fall adds a few frames of landing lag (`landLagFrames`, at `landLagSpeedMult` speed) that a jump cancels immediately and a move cancels after `landLagMoveCancelFrame`, and a small camera thump. The lag never applies on a mount.

| Value | Default | Range | Unit |
|---|---:|---|---|
| `landFxMinFall` | 0.6 | 0 to 3 | m |
| `landSquashMax` | 0.26 | 0 to 0.5 | x |
| `landSquashFullFall` | 6 | 1 to 12 | m |
| `landSquashTime` | 0.2 | 0.05 to 0.6 | s |
| `landDustBursts` | 1 | 1 to 4 | n |
| `landLagMinFall` | 3.2 | 0.5 to 12 | m |
| `landLagFrames` | 6 | 0 to 24 | frames |
| `landLagSpeedMult` | 0.45 | 0 to 1 | x |
| `landLagMoveCancelFrame` | 3 | 0 to 24 | frames |

### Spin

Attacks are frame data in 60 Hz fixed steps (`FixedStepper`, at most 4 steps per rendered frame), as in a fighting game: startup (no hit box), active (hits), recovery. Spin hits only in its active frames and can be cancelled into a jump from `spinJumpCancelFrame`. `spinCooldownFrames` count from the end of the spin. Spinning slows a fall (`spinFallDamping`) so it also works as a short hover.

| Value | Default | Range | Unit |
|---|---:|---|---|
| `spinStartupFrames` | 2 | 0 to 12 | frames |
| `spinActiveFrames` | 16 | 4 to 40 | frames |
| `spinRecoveryFrames` | 6 | 0 to 24 | frames |
| `spinCooldownFrames` | 18 | 0 to 60 | frames |
| `spinJumpCancelFrame` | 10 | 0 to 60 | frames |
| `spinRadius` | 1.4 | 0.5 to 3 | m |
| `spinFallDamping` | 0.3 | 0 to 1 | x |
| `spinTurnSpeed` | 1440 | 360 to 2880 | deg/s |

### Melee

Melee is the same frame model: `meleeStartupFrames`/`meleeActiveFrames`/`meleeRecoveryFrames`, a jump cancel from `meleeJumpCancelFrame`, and a buffered next swing when the button is pressed in the last `meleeBufferFrames` of the swing (so rapid taps chain into the combo trigger `Attack2`). Movement is `meleeMoveMult` during startup and active frames and free again in recovery. The first hit freezes the swing and the animator for `hitStopFrames` (2-4 frames is the usual range in action games; reduce-motion scales it by `reduceMotionScale`).

| Value | Default | Range | Unit |
|---|---:|---|---|
| `meleeStartupFrames` | 5 | 0 to 20 | frames |
| `meleeActiveFrames` | 10 | 2 to 30 | frames |
| `meleeRecoveryFrames` | 9 | 0 to 30 | frames |
| `meleeJumpCancelFrame` | 15 | 0 to 60 | frames |
| `meleeBufferFrames` | 9 | 0 to 30 | frames |
| `meleeMoveMult` | 0.55 | 0 to 1 | x |
| `hitStopFrames` | 3 | 0 to 8 | frames |

### Bounce

A spring crate must bounce to the *same* height every time, so `crateBounceHeight` is a target height and the launch speed is solved from it. Holding jump at the impact, or pressing it up to `bounceBufferBefore` seconds before it, gives the higher `crateBounceHeldHeight`; a press up to `bounceBufferAfter` seconds after it upgrades the bounce too (the buffered bounce from Mario and Crash). The stomp bounce uses the same rule with `stompBounceHeight`.

| Value | Default | Range | Unit |
|---|---:|---|---|
| `stompBounceHeight` | 1.6 | 0.5 to 5 | m |
| `crateBounceHeight` | 3.2 | 1 to 8 | m |
| `crateBounceHeldHeight` | 4.4 | 1 to 10 | m |
| `bounceBufferBefore` | 0.14 | 0 to 0.35 | s |
| `bounceBufferAfter` | 0.1 | 0 to 0.3 | s |

### Camera

Look-ahead (Mario Odyssey, Mario World): the camera leads the run direction by up to `camLookAheadRun` metres, eased and held for `camLookAheadHold` after he stops. Vertical follow is a dead band of `camVertWindow` metres: hops and crate bounces stay inside it and do not move the screen, a new floor is settled onto slowly, and a drop longer than `camFallSnapDistance` is caught up at `camFallSnapSpeed`. The fixed chase view (Crash Bandicoot) uses a tighter window and a shorter look-ahead (`camChase*`). Boss framing leans toward the hero (`camBossHeroWeight`) and lifts the look point for a distant boss (`camBossLift`).

| Value | Default | Range | Unit |
|---|---:|---|---|
| `camLookAheadRun` | 2.5 | 0 to 8 | m |
| `camLookAheadSmooth` | 0.4 | 0.05 to 1.5 | s |
| `camLookAheadHold` | 0.35 | 0 to 1.5 | s |
| `camVertWindow` | 1.1 | 0 to 4 | m |
| `camVertSmooth` | 0.25 | 0.05 to 1.2 | s |
| `camVertSettleSmooth` | 0.4 | 0.05 to 1.5 | s |
| `camFallSnapDistance` | 2.6 | 1 to 12 | m |
| `camFallSnapSpeed` | 16 | 2 to 40 | m/s |
| `camChaseLookAheadMult` | 1.4 | 0.5 to 3 | x |
| `camChaseVertWindowMult` | 0.7 | 0 to 2 | x |
| `camBossHeroWeight` | 0.6 | 0.3 to 0.9 | x |
| `camBossLift` | 0.8 | 0 to 3 | m |

### Input

The touch recogniser already applies its own dead zone; `stickDeadZone`/`stickOuterZone`/`stickCurve` rescale the stick so there is no jump at the edge of the dead zone and a curve above 1 gives fine control near the middle. The keyboard is digital and is *not* shaped. One-hand mode widens the dead zone a little, flattens the curve, adds gain so one thumb reaches full speed in less travel, and lengthens the jump buffer and coyote window.

| Value | Default | Range | Unit |
|---|---:|---|---|
| `stickDeadZone` | 0.12 | 0 to 0.4 | - |
| `stickOuterZone` | 0.95 | 0.6 to 1 | - |
| `stickCurve` | 1.4 | 0.5 to 3 | - |
| `oneHandDeadZoneAdd` | 0.04 | 0 to 0.3 | - |
| `oneHandCurve` | 1.15 | 0.5 to 3 | - |
| `oneHandGain` | 1.15 | 0.6 to 2 | x |
| `oneHandJumpBufferAdd` | 0.05 | 0 to 0.2 | s |
| `oneHandCoyoteAdd` | 0.04 | 0 to 0.15 | s |
| `touchSteerResponse` | 14 | 2 to 40 | - |
| `touchFollowDeadzoneInches` | 0.06 | 0 to 0.3 | in |
| `touchRunDragInches` | 0.15 | 0.03 to 0.5 | in |

### Anim lean

The model leans into turns (roll), forward with speed and back in a skid, about a pivot at the hips, smoothed by `leanSmooth`. It is applied on `ProcRoot`, above whatever clip is playing.

| Value | Default | Range | Unit |
|---|---:|---|---|
| `leanTurnDegrees` | 14 | 0 to 30 | deg |
| `leanForwardDegrees` | 8 | 0 to 25 | deg |
| `leanSkidBackDegrees` | 10 | 0 to 25 | deg |
| `leanSmooth` | 0.12 | 0.02 to 0.5 | s |

### Anim look

Head and eyes turn to Noura (or the active partner) first, then to nearby points of interest within `lookRange`, limited by `lookMaxYaw`/`lookMaxPitch`, through the humanoid IK pass or the greybox head.

| Value | Default | Range | Unit |
|---|---:|---|---|
| `lookMaxYaw` | 70 | 0 to 120 | deg |
| `lookMaxPitch` | 28 | 0 to 60 | deg |
| `lookHeadWeight` | 0.7 | 0 to 1 | x |
| `lookEyeWeight` | 0.9 | 0 to 1 | x |
| `lookRange` | 8 | 1 to 20 | m |
| `lookSmooth` | 0.18 | 0.02 to 0.6 | s |

### Anim breath

A slow chest/scale breath that speeds up with exertion (`breathExertionMult`) and settles over `breathRecoverSeconds`.

| Value | Default | Range | Unit |
|---|---:|---|---|
| `breathRate` | 0.27 | 0.1 to 0.8 | Hz |
| `breathAmount` | 0.012 | 0 to 0.05 | - |
| `breathExertionMult` | 2.3 | 1 to 4 | x |
| `breathRecoverSeconds` | 3 | 0.5 to 8 | s |

### Anim squash

Stretch on take-off and fall, squash on landing, on a spring (`squashStiffness`, `squashDamping`), scaled about the feet so he never sinks into the floor.

| Value | Default | Range | Unit |
|---|---:|---|---|
| `stretchJump` | 0.16 | 0 to 0.4 | x |
| `stretchFall` | 0.08 | 0 to 0.4 | x |
| `squashStiffness` | 260 | 40 to 600 | - |
| `squashDamping` | 15 | 4 to 60 | - |

### Anim feet

Foot IK on slopes and steps (High tier only, humanoid rigs with *IK Pass* ticked): ground rays of `footIkRayLength`, feet planted while the animation says they are on the ground, pelvis lowered up to `footIkMaxDrop`.

| Value | Default | Range | Unit |
|---|---:|---|---|
| `footIkWeight` | 1 | 0 to 1 | x |
| `footIkMaxDrop` | 0.45 | 0 to 0.8 | m |
| `footIkRayLength` | 0.9 | 0.1 to 1.2 | m |
| `footIkSmooth` | 14 | 2 to 40 | - |
| `pelvisSmooth` | 9 | 2 to 40 | - |

### Anim cloth

Verlet chains on bones named `ShemaghTail*`, `ThobeHem*` and `AbayaHem*` (never a sleeve: sleeves stay rolled and are not simulated; Noura's abaya hem swing is capped at 25 degrees so it can never ride up). Off on the Low tier and under reduce-motion.

| Value | Default | Range | Unit |
|---|---:|---|---|
| `clothStiffness` | 0.3 | 0 to 1 | - |
| `clothDamping` | 0.9 | 0.5 to 0.99 | - |
| `clothGravity` | 7 | 0 to 30 | m/s2 |
| `clothInertia` | 0.9 | 0 to 2 | x |
| `clothWind` | 0.6 | 0 to 2 | x |
| `clothMaxAngle` | 55 | 5 to 90 | deg |

### Anim flinch

An additive flinch on a hit that does not end the run: lean away from the hit direction and a small shove, for `flinchDuration`.

| Value | Default | Range | Unit |
|---|---:|---|---|
| `flinchDegrees` | 14 | 0 to 35 | deg |
| `flinchDuration` | 0.28 | 0.05 to 0.8 | s |
| `flinchShove` | 0.18 | 0 to 1 | m |

### Anim budget

Reduce-motion scales every optional motion (hit-stop, squash, flinch, breath) by `reduceMotionScale`; the Low tier turns off foot IK and cloth.

| Value | Default | Range | Unit |
|---|---:|---|---|
| `reduceMotionScale` | 0.35 | 0 to 1 | x |

### Shadow

Every character gets a soft round shadow disc directly under its feet (`Fx/DropShadow.cs`): Aron, Noura, the falcon and oryx, mounts, enemies, bosses and crates. It exists so the player can judge where a jump will land, which on a flat-lit cartoon world is otherwise hard (this is the classic "blob shadow" of the platformers). It is on in all three quality tiers. The Low tier has real shadows off, so the disc is the only shadow there; on Medium and High it is weaker while the character stands (`shadowRealBlend`) so it never doubles the real one into something too dark, and returns to full strength as the character rises over `shadowRealBlendHeight`, because the real shadow then lies away from the feet while the disc stays under them. The disc does not follow reduce-motion, since it is a landing aid and not motion.

How it works: one downward ray per character per frame (no allocations, at most 24 characters per frame, the player and bosses first) finds the surface under the feet. The disc lies on that surface and takes its tilt from the surface normal, so it sits flat on slopes and moves with moving platforms. It shrinks to `shadowMinScale` and fades as the height approaches `shadowMaxDistance`, and it is hidden when the ray finds no ground within that distance (over a pit). It is shown while jumping, falling, gliding and spinning. A character standing on another character does not use that character as ground, and a rider on a mount has no disc of their own because the mount carries it. The assist mode will set `DropShadow.AssistBoost` for a larger, stronger disc (`shadowAssistOpacityMult`, `shadowAssistSizeMult`); the switch exists, the assist setting that flips it comes with the accessibility options.

| Value | Default | Range | Unit |
|---|---:|---|---|
| `shadowOpacity` | 0.55 | 0 to 1 | x |
| `shadowSize` | 1 | 0.5 to 2 | x |
| `shadowMaxDistance` | 14 | 2 to 40 | m |
| `shadowFadeCurve` | 1.6 | 0.3 to 4 | - |
| `shadowMinScale` | 0.4 | 0.1 to 1 | x |
| `shadowRealBlend` | 0.6 | 0 to 1 | x |
| `shadowRealBlendHeight` | 1.5 | 0.2 to 6 | m |
| `shadowAssistOpacityMult` | 1.4 | 1 to 2.5 | x |
| `shadowAssistSizeMult` | 1.3 | 1 to 2 | x |

The maths is `DropShadowMath` (Logic/Presentation) and is unit-tested. The placement, the soft texture and the blending with the real shadow have not been seen in the Unity editor or on a phone yet.

## The hidden tuning panel

**Open it:** five quick taps on the version label in the main menu open the performance overlay; its **Feel tuning** button opens the panel. A small **FEEL** chip then stays at the left edge of the screen, in the menu and in every stage, to open and close the panel while playing. Touches on the panel and on the chip never steer the hero.

- **Sliders.** One per value, grouped (tap a group header to open or close it; it shows how many values in it are changed). **-** and **+** step by 1/200 of the range (1 for whole numbers). A value that differs from the shipped one is shown in amber.
- **Save** keeps the numbers on this phone and loads them again at the next start. **Reset** returns to the shipped values and deletes the saved tuning.
- **Export** copies the JSON to the clipboard and writes `game_feel_export.json` in the app's data folder (the path is shown). Paste it over `Assets/_Aron/Resources/Config/game_feel.json` and run the tests: they check every value is inside its slider range and that the settings do not contradict each other.
- It is a developer and QA tool, not a player setting: English only, not localised, not reachable without the secret taps, and nothing in the shipped game depends on it. The shipped numbers are the ones in `game_feel.json`; a value changed here only lives on that phone until it is exported, pasted into the JSON and checked by the simulation tests below.

## Procedural animation layer

`ProceduralAnimator` puts the model under a `ProcRoot` below the visual root and adds, on top of whatever clip is playing: lean into turns and speed, breathing, squash and stretch, an additive flinch scaled by hit direction, head and eye look-at, foot IK, and secondary motion on the shemagh tails and thobe hem. The sleeves stay rolled and are never simulated. `CharacterSetup` releases it before it replaces the model and rebinds it afterwards.

- Foot IK needs a humanoid rig with **IK Pass** ticked on the base layer of the Animator controller (the greybox body has none, so it gets only the head turn).
- Cloth needs bones named `ShemaghTail_*`, `ThobeHem_*` or `AbayaHem_*` in a chain (root first). A rig without them just has no secondary motion.

## Animator hooks (P0)

`PlayerAnimationHooks` exposes typed setters (`SetFloat`, `SetBool`, `SetInt`, `Trigger`, `TryTrigger`, `SetSpeed`) and the events `OnLanded`, `OnSkid`, `OnHit`, `OnBonk`, `OnLedgePop`, `OnBounce`, `OnStomp`. `MissingP0Parameters(actor)` lists the P0 parameters an Animator controller lacks (name or type), so a controller can be checked the moment it is imported. The full list of all 112 parameters is `AnimParams.All` (generated from `Resources/Config/animation_clips.json` by `tools/gen_animation_list.py`).

There are 39 distinct parameters used by the 83 P0 clips. **Wired** means a script in the game already sets it; **defined only** means the parameter, the clip and the hook exist but no gameplay script sets it yet.

### Wired

| Parameter | Type | Driven by | P0 clips |
|---|---|---|---:|
| `Attack` | trigger | PlayerController (the weapon definition's trigger); Oryx and MountAnimal their own | 3 |
| `Attack2` | trigger | PlayerController, on every second chained swing, only if the controller has the trigger (falls back to `Attack`) | 1 |
| `Bounce` | trigger | PlayerController (`OnBounce`) | 1 |
| `Die` | trigger | PlayerController; Enemy its own | 3 |
| `DoubleJump` | trigger | PlayerController | 2 |
| `Gliding` | bool | PlayerController (Noura) | 1 |
| `Hurt` | trigger | PlayerController via `OnHit` (+ `HurtBack`, `HurtLeft`, `HurtRight`); Enemy, MountAnimal their own | 4 |
| `Jump` | trigger | PlayerController (and `MountAnimal.Jump` when riding) | 3 |
| `Land` | trigger | PlayerController via `OnLanded` | 4 |
| `LandHard` | trigger | PlayerController via `OnLanded` | 1 |
| `Perched` | bool | FalconCompanion | 1 |
| `Skidding` | bool | PlayerController (via `OnSkid`) | 2 |
| `Slide` | trigger | PlayerController | 2 |
| `Sliding` | bool | PlayerController | 2 |
| `Speed` | float | PlayerController (Aron, Noura, partner); Enemy, Oryx, Boss and MountAnimal drive their own `Speed` | 13 |
| `Spin` | trigger | PlayerController | 2 |
| `Sprinting` | bool | PlayerController | 3 |
| `Stunned` | bool | defined in the hooks; the boss script does not set it yet | 1 |
| `VerticalSpeed` | float | PlayerController | 6 |

### Defined only (not set by any script yet)

| Parameter | Type | P0 clips |
|---|---|---|
| `Aggro` | bool | `duhaim_idle` |
| `Celebrate` | trigger | `aron_celebrate_stage_clear`, `noura_celebrate_stage_clear` |
| `CelebrateBoss` | trigger | `aron_celebrate_boss_win` |
| `Collect` | trigger | `aron_collect_date`, `noura_collect_date` |
| `DiePit` | trigger | `aron_die_pit_wave` |
| `Dismount` | trigger | `rider_dismount` |
| `Dive` | trigger | `falcon_dive` |
| `Flying` | bool | `falcon_fly_flap` |
| `GlideOpen` | trigger | `noura_glide_open` |
| `Mount` | trigger | `camel_kneel_mount`, `rider_mount_up` |
| `MountKind` | int | `rider_camel_ride_idle`, `rider_camel_ride_walk`, `rider_camel_ride_run` |
| `MountSpeed` | float | `camel_idle`, `camel_walk`, `camel_run` |
| `Nudge` | trigger | `oryx_nudge_noura` |
| `PhaseBreak` | trigger | `duhaim_phase_break_roar` |
| `SleeveRoll` | trigger | `aron_sleeve_roll` |
| `Stun` | trigger | `duhaim_stun_enter` |
| `TakeOff` | trigger | `falcon_take_off` |
| `Telegraph` | trigger | `duhaim_telegraph_windup` |
| `ThrownOff` | trigger | `rider_thrown_off` |
| `Wave` | trigger | `aron_wave`, `noura_wave` |

Note: `MountAnimal` still drives its own `Speed` / `Hit` / `Jump` / `Attack` names; the mount clips in the list use `MountSpeed`. `SleeveRoll` uses the `sleeve_roll_up` / `sleeve_roll_down` names from `SleeveRules`. Both need a decision when the mount and sleeve controllers are built.

## The tuning pass

The first numbers in this file were reasoned from the games above. This pass measured them instead: a headless copy of the player's frame (`Tests/Aron.Logic.Tests/FeelSim.cs`) plays scripted input over the shapes the stage builder lays out, at 30, 60 and 120 fps, and `FeelSimulationTests` holds the results against the targets below. Change a value in `game_feel.json` and those tests say whether the game still plays.

### What the simulation does and does not know

- It runs the same pure pieces as the controller in the same order: input buffers, the 60 Hz stepper for spin and landing stagger, slope speed, `Locomotion`, `JumpArc`, coyote time and the jump buffer, the spin with its cooldown, the downhill stick, ledge forgiveness, bounces and mount multipliers (`MountStats`). The camera is `LookAheadModel` and `VerticalFollowModel` fed by the hero's recorded height.
- The hero is a single point. The real capsule has a width, so edges and corners are slightly more forgiving in the game than in the simulation; every clearance number below is therefore on the safe side.
- It does not know Unity's physics, the animation, input latency or what a thumb does. See "Not verified in Unity".

### A bug the simulation found

The jump velocity was integrated in sub-steps, but the character was then moved by the *end-of-frame* speed times the frame time. That is a different arc at every frame rate: a held jump peaked at 1.84 m at 30 fps, 2.13 m at 60 fps and 2.20 m at 120 fps, and the old test allowed 0.25 m of difference, which hid it. `JumpArc.Advance` now returns the distance summed over the sub-steps, and the controller moves by that. A held jump peaks at `jumpHeight` (within 1.5%) at every frame rate from 20 to 144 fps, and the take-off speed is solved with the same sub-step the game uses (`JumpArc.SubStep`, 1/120 s), so the peak is the requested 2.2 m and not 2.18 m.

### Targets and results

| Target | Result with the shipped values |
|---|---|
| Held jump peaks at `jumpHeight`, at every frame rate | 2.20 m (within 1.5%), 20 to 144 fps |
| Time to the top 0.38 to 0.48 s | 0.425 s |
| Hang at the top 0.2 s or less (it was 0.26 s: floaty) | 0.175 s |
| The way down is at least 10% quicker than the way up | 0.358 s down, 0.425 s up |
| Whole jump 0.68 to 0.85 s; landing speed above 12 m/s | 0.77 s; 14.1 m/s |
| A tap still climbs the biggest ordinary step (`StageLayout.FootStep`, 1.2 m) | 1.30 m |
| A standing jump clears the widest gap on foot (4.2 m) by 12% or more | 4.80 m |
| Two metres of run-up clears it by 25% or more | 5.45 m |
| A full run-up | 6.25 m |
| Highest platform one jump reaches over a 2.6 m gap | 2.5 m (builder uses up to 1.2 m) |
| Camel and horse clear their gap constants (6 and 5.5 m) by 20% or more | 10.5 m and 11.3 m |
| Press up to 0.1 s after leaving an edge still jumps; 0.2 s does not | at 30, 60 and 120 fps |
| Press up to 0.12 s before landing jumps on landing; 0.3 s does not | at 30, 60 and 120 fps |
| 90% of run speed within 0.3 s and 1.5 m | 0.17 s, 0.6 to 0.7 m |
| No airborne frame running down slopes up to 40 degrees | none, at any frame rate (one frame at 30 fps on the 50 degree limit) |
| Camera: he is never farther than 70% of the visible half-height (2.8 m) from the middle | 2.56 to 2.73 m (was 3.57 m) |
| Camera: settles to 10 cm of a new floor within 1.1 s | 1.00 to 1.03 s |
| Camera: look-ahead at 90% within 0.55 s | about 0.4 s |
| Crate, held and stomp bounces reach their set height at every frame rate | within 3% |
| Every gap, step and drop in every generated stage is within what the jump clears | scanned for all stages |

### What changed

| Value | Before | After | Why |
|---|---:|---:|---|
| `apexThreshold` | 2.2 | 1.6 | The low-gravity band was wide enough to hold him up for 0.26 s of a 0.82 s jump. Narrowed, the hang is 0.175 s: still a place to aim a landing, no longer a float. |
| `apexGravityMult` | 0.55 | 0.6 | Slightly less hang, and the top of the arc closer to a parabola. |
| `coyoteTime` | 0.10 | 0.12 | At 30 fps 0.1 s is three frames. Touch input on a phone typically adds tens of milliseconds of latency, so a press that felt on time can arrive after the edge. One-hand mode still adds to it. |
| `jumpBuffer` | 0.13 | 0.15 | The same argument for a press just before landing. Still short enough that a press clearly early is not a surprise jump later. |
| `jumpCutDelay` | 0.06 | 0.08 | A quick tap on glass is typically a few hundredths of a second. With 0.06 s the shortest hop was 1.19 m, under the 1.2 m step the builder uses; at 0.08 s it is 1.30 m. A held jump is unchanged and the range from a tap to a full jump is still 1.3 to 2.2 m. |
| `camVertSmooth` | 0.30 | 0.25 | A jump over a gap with a drop took the picture 3.6 m away from him, close to the edge of the frame. Now 2.6 to 2.7 m. |
| `camVertSettleSmooth` | 0.55 | 0.40 | After landing on a new floor the camera was slower to catch up; it now settles in about 1.0 s. |
| `camFallSnapDistance` | 4.5 | 2.6 | The snap that stops the camera losing him in a long fall now starts well inside the frame. |

Also changed in code: `Slopes.StickSpeed`. Running down a slope the ground falls away at speed times the tangent of the angle, and a fixed `groundStickSpeed` of 6 m/s let him leave the ground on every frame of a slope of about 35 degrees or more at a run (the animation flickers, landing dust fires). The stick is now faster than the slope falls away while he is running downhill, and unchanged on the flat and when he walks off a ledge, so the start of a fall still looks the same.

### What it means for the stage builder

Gaps and steps in `StageLayout` were set for a jump that is cautious: the widest gap on foot (4.2 m) is 12% inside a standing jump and 23% inside a jump with two metres of run-up, and the mount constants (camel 6 m, horse 5.5 m) are about half of what the simulated mounts clear (10.5 m and 11.3 m). That is deliberate headroom for a thumb on a phone and for the hero's capsule, and a design choice, not a measured limit. The constants were not changed in this pass. A scan test fails if a stage ever asks for more than the constants allow, and if the jump is retuned until it can no longer clear them.

### Not verified

All of the numbers above are simulation results. None of them has been felt on a phone. The feel of the hang, the weight of the landing, whether 0.12 s of coyote time is generous or sloppy, and whether the camera follows too slowly, can only be judged on a device with a thumb on the screen. Use the hidden tuning panel for that (it is a developer and QA tool), and when a value is settled export it into `game_feel.json` and run the tests: they will say which target moved.

## Not verified in Unity

The Logic assembly is compiled and tested with NUnit (the whole suite, see the commit message for the count). The Unity side was only compiled against the Unity 2021 API reference assemblies, not in the Unity 6 editor, and has **not been run** in Unity or on a device:

- the controller wiring (all of `PlayerController`, `MeleeAttack`, `FollowCamera`), including the ledge and head-bonk probes and the slope slide;
- the tuning panel layout (sliders, scrolling, the chip) and the clipboard export;
- foot IK, head look-at and the cloth chains (they need rigs that do not exist yet);
- the numbers themselves: they were reasoned, then checked in a headless simulation (see the tuning pass), but they have not been felt on a phone and must be tuned there;
- the animation clips: `docs/animation_list.md` is a production list, none of the clips are made.

