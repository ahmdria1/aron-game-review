# Feel, comfort and systems

One page for the systems added with the race-mounts work. Every system has its numbers in a JSON file under `Assets/_Aron/Resources/`, its rules in `Scripts/Logic/` (tested with `dotnet test Tests/Aron.Logic.Tests`), and a thin runtime part in `Scripts/`. **The runtime parts compile only in Unity: they were syntax-checked but never built or played.**

## Stage length, par time and stars

`Logic/Regions/StageTiming.cs`, `StageLayout.cs`. Every stage is generated to play for 5–8 minutes at normal pace.

| Stage type | Minutes |
|---|---|
| normal | 6–8 |
| chase | 5–6 |
| action | 5–7 |
| race | 5–6 |
| boss | 6–8 (walk-in plus the fight) |

Par time is 0.9 × the stage's total play time, in 5-second steps. Three stars per stage: **1** finish, **2** every golden and bisht box broken in one run (the "hidden" star), **3** finish faster than par. Stars show on the stage-end screen (`GameUI`) and are saved per stage. The lantern checkpoints come about every 90 s.

## Aim assist and hit-stop

`Logic/Combat/CombatFeel.cs`, `Resources/Config/combat_feel.json`, `Combat/AimAssistRuntime.cs`, `HitStopRuntime.cs`.

- **Aim assist:** a melee swing turns up to 35° toward an enemy or box inside a 70° cone and 2.6 m. A slingshot tap snaps to the nearest target within 48 dp (14 m). Strength by difficulty: easy ×1.4, normal ×1.0, hard ×0.6 (never zero). Bosses and enemies get a small extra pull. One-hand mode widens it ×1.5. It can be turned off in Settings.
- **Hit-stop:** 80 ms on a boss's final hit and 70 ms on a phase break, with a 0.35 s cooldown. Time drops to 0.02 (a near stop), ends on a real-time clock, is skipped while paused, and with "reduce motion" only the haptic plays.

## Haptics (vibration)

`Resources/Config/haptics.json` (25 patterns: box break, coin, land, stomp, hit, bisht lost, death, explosion, boss hit and defeat, race boost, drift, collision, gallop, sleeve tick, passport stamp and visa, checkpoint, collect, companion dash, hit-stop, race win, star, UI confirm, and more), `Logic/Feedback/Haptics.cs`, `Feedback/HapticService.cs`, `Feedback/Haptics.cs`, `Plugins/iOS/AronHaptics.mm`.

- Settings: on/off, Low/Medium/High, and "off in battery saver" (all saved in `SaveData.haptics`).
- A gate stops vibration when it is off, the app is in the background, the OS has haptics off, or battery saver is on. Each pattern has a per-second limit.
- **Android:** `VibrationEffect` waveforms, with `Handheld.Vibrate` as the fallback. The `VIBRATE` permission is normal and is granted at install: nothing to declare in the Play Console, but check the merged manifest after the first build.
- **iOS:** `UIImpactFeedbackGenerator` through the `.mm` plugin (light/medium/heavy). It is only compiled for iOS builds.
- Wired to game events (`Feedback/Haptics.cs` hooks `GameEvents`: coins, hits, stomps, crates, checkpoints, boss events, death) and to the passport and museum moments. `Haptics.OnCue(id)` is ready for emotion cues, but **the emotion controller has no runtime driver in the repo yet**, so cues do not vibrate until one exists.

## Camera (العدسة)

`Logic/CameraRig/CameraRig.cs`, `Resources/Config/camera.json`, `Player/FollowCamera.cs`, `Input/CameraOrbitInput.cs`.

- A drag on the **right half** of the screen orbits the camera (55° per cm, up to 150° each side, 15° down and 35° up). Pinch zooms (0.6–1.5×). After about 1.5 s with no input the camera swings back behind the hero over 0.6 s, or the **Recenter** button does it at once (the button shows only when orbit is possible).
- A sphere cast keeps the camera out of walls (0.3 m margin, never closer than 1.4 m).
- Modes per stage type (`stageModes`): normal and action = free, chase = chase, race = race (the field of view and distance widen with speed), boss = boss (frames hero and boss).
- Settings: shake 0–100% (steps of 25), invert-Y, auto-recenter. Reduce-motion caps the shake at 25%.

## One-hand mode

`Logic/Safety/Safety.cs` (`OneHandMode`), Settings toggle. With it on: orbit and pinch are disabled (they need two fingers), the camera follows by itself and no recenter button shows, a **Pause button** appears on the thumb's side (two-finger pause is otherwise the only way), screen buttons move to the thumb's side (mirrored for left-handed play), and aim assist is 1.5× wider.

## Sleeve roll-up (يشمر عن يده)

`Logic/Heroes/SleeveState.cs` (`SleeveDirector`, tested), `Player/SleeveRoll.cs`.

- Aron rolls both thobe sleeves to the forearm in about 0.6 s when an enemy comes within 7 m, on his first attack (the roll is cut short straight into the attack), or when a boss fight starts. At a boss start the sleeves go up first and then the face wrap (`SleeveRoll.WrapMayStart`).
- They come down (0.9 s) after 6 s without combat, at stage clear, or after a boss win, where the order is: face wrap off with a smile, 1.2 s later the sleeves.
- **Noura never rolls her sleeves.** Her abaya stays fully covering; `SleeveRules.Allowed` is true only for `aron`, `CharacterSetup` adds no `SleeveRoll` to her, and a test checks the rule.
- **For the rigged model** (not made yet): a blendshape named `sleeve_roll` on the body mesh (0 = down, 1 = rolled to the forearm) and/or a material float `_SleeveRoll`; animator triggers `sleeve_roll_up` and `sleeve_roll_down`. The greybox shows nothing. Each roll plays the `sleeve_tick` haptic.
- There is **no runtime for the face wrap (تلثم)** in the repo (only the style data in `ShemaghStyles`), so the "sleeves first, then the wrap" order is exposed as `WrapMayStart` for it to use.

## Weather that matters in play

`Logic/Weather/WeatherGameplay.cs`, `Resources/Config/weather_gameplay.json`, `Atmosphere/WeatherGameplayRuntime.cs`. Applies to **story stages** and to **free play only when "weather effects" is on**. Never in races, time trials or boss fights, so a result is never decided by the weather.

| Weather | Effect | Fairness limit |
|---|---|---|
| Dust and fog | Visibility shrinks (down to a floor of 14 m; full dust 24 m; clear 120 m) | Hazards never hide inside the floor |
| Wind | Gusts push sideways (period 6 s, up to 1.8) | Halved on a mount, cut to 40% by reduce-motion |
| Rain, snow | Traction ×0.55 wet, ×0.7 snow, puddles ×0.4 traction and ×0.7 speed | Never ice: there is a floor |
| Heat | In hot climates the hero slows to at most 85% after 30 s in the sun, recovers in 6 s in shade or after a drink, and a hint shows at 60% | Never below 85% |

Heat scale by climate: desert 1, semi-arid 0.8, tropical 0.6, subtropical 0.5, monsoon 0.5. The sky and weather system itself is in `docs/weather.md`.

## Companions: falcon scout and key

`Logic/Animals/FalconBrain.cs`, `Animals/FalconCompanion.cs`, `Heroes/NouraPieces.cs`. The oryx already smashed barriers, and the falcon already swooped at enemies and fetched coins. New:

- **Scout:** the falcon flies ahead and makes **sparkles appear over hidden paths** (secret routes) with a banner. It is only a hint: it does not make a path walkable.
- **Key fetch:** next to a locked gate, the falcon flies to fetch "the key" (range twice the coin range) so Aron can open Noura's lock without her. **There are no key items in the game.** The "key" is a hook above each lock gate. The lock-open banner says the falcon opened it.

## Ghost race as the default offline mode

See `docs/computer_racers.md`.

## Museum, artifacts and the passport stamp

`Resources/Museum/artifacts.json` (35 artifacts, one per country), `Logic/Culture/Museum.cs`, `Stages/MuseumService.cs`, `UI/MuseumScreen.cs`. Tests: `MuseumTests`.

- **Unlock:** the first "hidden" star (every golden box in one run) on any ordinary, non-race stage of the country gives that country's artifact. It shows in the stage-end screen, plays the passport stamp sound and haptic, and adds a sticker to the country's passport page. The passport stamp itself is recorded the same way when the country is finished (`PassportBook.OnStageFinished` is now called at stage end; **there is no passport screen at runtime**, so stamps are saved but not shown).
- **Screen:** Main menu → Museum: a grid of 35 shelf cards with the colour swatch, the country's local name, and the artifact name and a one-line fact when found. Missing ones are grey with a hint. Count "found x of 35". A coloured swatch stands in until the artifacts are modelled (`shape`, `color`, `color2` are in the data).
- **What the artifacts are** (nothing religious, no flags, seals, weapons or national emblems): Sadu weaving, Talli embroidery, boom dhow model, Bahraini pearl, falcon hood, frankincense burner, Sana'a qamariya window, oud, Damascus brocade, blown glass bottle, Madaba mosaic, papyrus painting, palm-leaf basket, Ghadames leather pouf, Nabeul pottery vase, mandole, zellige tile, brass teapot, Iznik plate, suzani, truck-art panel, block-print fabric, songket, batik, origami crane, opal gemstone (Australia; a boomerang was avoided on purpose), beadwork necklace, berimbau, alebrije figure, patchwork quilt, porcelain teacup, Calais lace, cuckoo clock, Spanish guitar, azulejo.
- **Review needed:** names are in 12 languages, but the fact lines are in **Arabic and English only** (other languages fall back to English). All names, facts and the choice of object per country need a check by someone from each culture.

## Settings screen

Main menu → Settings: vibration on/off, strength, battery saver; camera shake (cycles 0, 25, 50, 75, 100%), invert-Y, auto-recenter; one-hand mode; aim assist. All 12 languages. The UI kit has no slider, so each row is a button that cycles its value.
