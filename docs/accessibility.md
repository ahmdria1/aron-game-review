# Accessibility and comfort

Everything here is reached from **Settings → Accessibility and comfort** (`UI/AccessibilityScreen.cs`). The rules are plain C# in `Assets/_Aron/Scripts/Logic/Access` and `Logic/Safety` (no Unity types, unit-tested in `Tests/Aron.Logic.Tests`); the Unity side only reads the saved `AccessSettings` and draws. Every row is one button that cycles its value and saves at once, like the rest of Settings.

## Captions

- **What is captioned.** Every line in `Resources/Audio/VoiceLines.json` (what Aron, Noura and the bosses say) and every sound cue (`AudioCues.Sfx`, every file in `Resources/Audio/Sfx` except the button click and the player's own jump, and the library sounds: breaking, thunder, boss hits). The text lives in `Resources/Config/captions.json`, in all 12 languages, one entry per id. A test fails if a voice line or a sound has no caption, or if one language is missing, or if a caption is longer than 60 characters.
- **Speech vs sounds.** A spoken line is plain text. A sound is written in brackets, `[ Gate opens ]`, so it is never taken for speech. A boss grunt with no words is captioned as a sound.
- **Which sounds.** Each sound is marked *important* when missing it can cost the player something: a warning, a hit, a timer, a wrong answer, a checkpoint. The sound-caption setting is **Important only** (default), **All sounds** or **Off**. Speech captions follow the main Captions switch, which is on by default.
- **How it looks.** At most two lines at the bottom of the screen, speech above sounds. The same cue is not repeated for 1.5 s (a footstep is one caption, not ten), a sound never pushes speech off the screen, and a line stays for its reading time (12 characters a second, between 1.6 and 6 seconds). The size has five steps (80 %, 100 %, 125 %, 150 %, 200 %), the background darkness four (40 %, 55 %, 75 %, 100 %), and the settings screen shows a live sample.
- **How it is wired.** `UI/CaptionPresenter.cs` listens to `GameEvents.SfxRequested`, `GameEvents.SoundPlayed` (raised from `SfxPlayer.Play`) and `GameEvents.VoiceSpoken` (raised from `AudioManager.PlayVoice`). The voice event is raised even when no recording exists, so captions work while the recordings are still being made. No gameplay code calls the presenter.
- **Adding a sound or a line.** Add the entry to `captions.json` with all 12 languages. The tests tell you if you forgot.

## Text size

One multiplier (90 %, 100 %, 115 %, 130 %, 160 %) applied by `UIFactory.Label` to every label created after it is set, so it takes effect the next time a screen opens. A larger label may shrink to fit its box instead of spilling out of a button. Captions have their own size.

## Colour-blind modes

**Colour vision** on the Accessibility screen cycles Normal, Red-blind (protanopia), Green-blind (deuteranopia) and Blue-blind (tritanopia). It changes two things.

- **The HUD colours.** The timer, the bisht counter and the boss bar take their colour from a role (`ColourRole`: Danger, Warning, Safe, Reward, Info) through `UI/AccessColours.cs`. With Normal they are the colours the game always had. With a mode each has its own palette from the Okabe-Ito set. `Logic/Access/ColourVision.cs` simulates each kind of colour blindness (the Machado, Oliveira and Fernandes 2009 matrices at full severity, in linear light) and measures the distance between two colours in Lab; the tests require every pair of roles to be at least 24 apart as that player sees them. For comparison, the usual warning orange and safe green are only 6.5 apart for a red-blind player. The red-blind and green-blind palettes also hold at least 15 for each other.
- **Hazard markers.** With any mode on, a small triangle floats above every enemy, the boss, the boss's shots and the rolling boulder, in the Danger colour. Each role has its own shape (triangle, diamond, circle, star, square), so colour is never the only signal, and the shapes have a dark rim so they read on any background. The five role shapes are shown on the settings screen so the player can see what they look like.
- **When it applies.** HUD colours are read when the game screen is built, and markers when something is created, so a change applies from the next stage.
- **Not a screen filter.** This does not recolour the 3D world. A full-screen filter needs a render pass that cannot be checked from here.

## Controls

Accessibility has four control rows: left-handed layout, button size, button opacity and Move buttons.

The game is played with touch gestures, so there are no jump or attack buttons to place. The two on-screen buttons are Recenter (camera) and Pause (shown in one-hand mode). Those are the buttons these settings move. Left-handed layout also puts the camera-orbit touch on the left half of the screen.

- Defaults: right-hand side; mirrored when left-handed; pulled toward the thumb in one-hand mode.
- Size steps: 80, 100, 120, 140, 160 %. A button is never drawn shorter than 96 reference px, whatever the step.
- Opacity steps: 40, 60, 80, 100 %.
- Move buttons opens a screen where each button is dragged to where the thumb reaches. A position chosen by hand wins over the left-handed and one-hand defaults. The button centre is kept 7 % in from the screen edge, and a drop on top of the other button slides up or down until it is clear. Put buttons back clears every position.
- Positions are saved with the other accessibility settings. Older saves have none and keep the defaults.

The logic is `Logic/Access/ControlLayout.cs`, tested in `ControlLayoutTests`.

## Assist mode

One switch on the Accessibility screen (off by default). It does three things:

- Lantern checkpoints come sooner: a spacing of about 54 s instead of 90 s. The extra lanterns are added to the finished stage, on open ground at least 10 m long and clear of enemies, puzzles, chases, ambushes, conversations and mount posts. No other piece moves, so every coin, crate, extra-life crate, bisht box, enemy and the goal are exactly where they are without assist. The original lanterns stay.
- The rolling boulder in a chase section runs at 80 % of its normal speed. The hero's own speed and jump are not changed.
- The landing shadow is bigger and darker (`DropShadow.AssistBoost`, kept in step with the saved setting by `AssistRuntime`).

It never locks a stage, medal, coin, costume or unlock and never changes what a finished stage is worth. Progress and rewards are the same with it on or off. This is separate from the quiet helper in `difficulty.json`, which stays as it was.

The logic is `Logic/Access/AssistRules.cs`, tested in `AssistModeTests` over every stage in the catalog.

## Flashing and motion

Three switches on the Accessibility screen, all off by default and kept for old saves:

- **Reduce flashing**: lightning becomes a slow, faint brightening with no flash, and every blink (the hero after a hit, the golden bisht, the boss warning) runs at 2 a second or less.
- **Reduce motion**: camera shake is held to a quarter, the hit freeze goes away, and wind sway on trees and on the hero is cut to the `reduceMotionWindShare` share.
- **Flash-safe mode**: turns both on at once.

With none of them on, no blink in the game runs faster than 3 a second, and lightning never makes more than 3 flashes in any second, which is the common photosensitivity guidance. The camera-shake slider in Settings still applies; the reduced cap is the smaller of the two.

The logic is `Logic/Access/FlashSafety.cs`, tested in `FlashSafetyTests` (every blink rate against a one-second window, lightning, shake scaling, old saves).

## Not verified in Unity

The rules and the data are tested. The on-screen parts (caption layout, the settings screen, the text-size shrink, the hazard marker sprites, the Move buttons drag screen, the lantern boxes added by assist mode) compile in a stub harness but have not been seen in the editor or on a phone.
