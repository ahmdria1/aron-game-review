# Assets still needed (counts for 35 countries)

What the data and code already name, and what still has to be made or recorded. Everything listed as "greybox" works today as a stand-in.

## Models and rigs

| Asset | Count | Notes |
|---|---|---|
| Rival racers | 35 | One per country, model id `rival_<country>` in `Resources/Rivals/rivals.json`. 16 are women: modest outfits (see `docs/computer_racers.md`), and the outfits need a cultural review. |
| Race mounts | 5 | Horse, camel, ostrich, giraffe, falcon glider. Plus the royal and sadu saddles and the tassels for each. |
| Wildlife | 115 | Entries in `Resources/Companions/local_animals.json`: 51 ground, 25 mount, 21 flier, 16 hazard, 2 pointer. Many reuse a shared body (`body`, `accent`, `size` in the data). |
| Museum artifacts | 35 | One per country in `Resources/Museum/artifacts.json`. A coloured swatch stands in until they are modelled. Needs a cultural review. |
| Aron's rig | 1 | A blendshape `sleeve_roll` (0 down, 1 rolled to the forearm) or a material float `_SleeveRoll`; animator triggers `sleeve_roll_up`, `sleeve_roll_down`. Noura gets none: her sleeves never roll. |
| Face wrap (تلثم) for Aron | 1 | Only the style data exists. It must be put on after the sleeves are up (`SleeveRoll.WrapMayStart`). |

## Sounds

- **Motorcycle engines:** the 18 files in `Resources/AronSfx/engine_*.ogg` are final (list in `docs/engine_sounds.md`).
- **Passport and museum:** `passport_stamp_1`, `passport_stamp_2` in `Resources/AronSfx` (used for every stamp and every artifact found).
- **Voice lines:** none yet; the `vo_*` ids in `Resources/Audio/VoiceLines.json` stay silent until recordings are added.
- Every audio file carries only the owner's artist and copyright tags and no other metadata (the test `EveryAudioFileCarriesOnlyTheOwnersTags` checks it).

## Effects

The museum uses the existing `sparkle` effect from `FxPlayer`. `stamp_ink` is named in `Passport.cs` for a passport screen that does not exist yet.

## Plugins and settings to check on a device

- iOS haptics plugin `Assets/_Aron/Plugins/iOS/AronHaptics.mm`: build for iOS and check light, medium and heavy.
- Android: the `VIBRATE` permission in the merged manifest (Unity adds it because `HapticService` references `Handheld.Vibrate`), and how the patterns feel on a low-end phone.
