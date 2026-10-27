# Computer racers, 35 rivals and the ghost default

Data: `Resources/Races/ai_racers.json`, `Resources/Rivals/rivals.json`, `Resources/Races/race_setup.json`. Logic: `Scripts/Logic/Races/AiRacers.cs`, `Rivals.cs`, `RaceSetup.cs`. Tests: `AiRivalTests`, `RaceSetupTests`.

## Difficulty

| | Easy | Normal | Hard |
|---|---|---|---|
| Share of the mount's top speed on a clean run | 84% | 94% | 96% |
| Slips per minute | 6 | 3 | 1 |
| Speed kept while slipping | 60% | 70% | 80% |
| Dodges an obstacle in time | 72% | 88% | 97% |
| Takes an open shortcut | 10% | 50% | 90% |
| Uses its mount's ability | 30% | 70% | 100% |
| Reaction time | 0.7 s | 0.4 s | 0.25 s |

"Hard" means better driving, never cheating. `AiDriver` clamps every racer to its mount's top speed, so a computer racer is never faster than the same mount driven well. A light catch-up and hold-back (at most 4–6% of top speed) keeps the race close and is capped at the mount's speed. Up to 7 computer racers offline (8 seats with the player). On sand a mount without sand resistance loses 30%, the same rule as for the player. A racer uses a mount other than its own 25% of the time.

## The 35 rivals

One rival per country (35 in all, Australia is the 35th), each with a name in English and Arabic, a mount, a driving style, and 8 lines (start, start2, overtake, overtaken, boost, crash, win, lose). Mounts: 18 horse, 11 camel, 2 ostrich, 2 giraffe, 2 falcon glider.

- The Arabic lines are written in the rival's dialect (144 lines). Other lines are in the country's own language (104) or English (32). **Every dialect line and every non-Arabic line is marked for a native-speaker review** (`review` field in the data).
- **16 rivals are women** (kuwait, bahrain, yemen, iraq, syria, libya, tunisia, morocco, mauritania, spain, uk, turkey, uzbekistan, malaysia, brazil, australia). Each is drawn in modest clothing: long sleeves, long trousers or tunic, and a helmet (with a scarf where it fits the country). `modest` is true for all 35 and a test enforces it. All 16 outfits have `outfitReview: true`: **someone from each culture should look at the outfit and the colours**.
- Colours come from the country's palette. No flags, emblems or brand names appear on a rival.
- Bark lines are subtitled (`rival_bark_*` strings, 12 languages) and can be switched off with the "rival chatter" option.

## Online

When fewer than 2 real racers are in a match, the lobby fills the field with computer racers. They are **labelled** as computer racers, never shown as real players, and a match with fewer than 2 real racers gives no ranked bonus and no online multiplier.

## Ghost race as the default offline mode

`RaceSetup.DefaultMode`: offline = ghost race, online with at least 2 real racers = ranked. `RaceSetup.Choose` picks the ghost to race against, in this order:

1. A friend's share code, if it decodes and is for the same track.
2. The player's own best run on this track.
3. Otherwise a **pace ghost** (first time): a ghost that drives the track at a reference speed times 0.7 / 0.85 / 1.0 (easy / normal / hard).

The reference speeds in `race_setup.json` (camel 14, horse 20, car 34, ostrich 18, bull 10, boat 8, sail 9, ski 22, surf 10, sky 18 m/s) are **placeholders to tune with a real play test**.

## Honest status

The ghost recorder, the race screen and the race runtime are **not in this repo**. What exists and is tested: the ghost codec, local and cloud store, share codes, race points (5/3/1, the same in ghost races), `RaceSetup`, and the computer-racer driver logic. The strings for the race screen (`ghost_race`, `ghost_own`, `ghost_friend`, `ghost_pace`, `ghost_enter_code`, `ghost_bad_code`, `ghost_share`) are in all 12 languages and ready.
