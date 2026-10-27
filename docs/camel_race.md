# سباق الهجن | Camel race

A cartoon, arcade camel race on a desert camel racetrack (ميدان هجن). Everything below is data in
`Assets/_Aron/Resources/Races/camel_race.json` and the logic in `Scripts/Logic/Races/CamelRace.cs`; the art follows the data,
so a breed, a colour or a cup can change without touching code.

## The track and the riders

- Venue `hijn_maydan` (ميدان الهجن): a long sand strip, dunes, stands, palms and the road beside it. Sunset, clear weather.
- Aron rides his own camel. Noura cheers beside the track (fully covered, as always) or rides her own camel in 2-player and ghost modes.
- Every rival camel carries a small **robot jockey**. There are never child jockeys, in any mode. The file validator rejects anything else.
- Arcade layer: boost pads, dune jumps, comic stumbles and speed lines. A stumble slows the camel (35 %) for under a second; nobody is ever hurt.
- Camera: the `camel_race` mode in `camera.json`. It widens and backs off with speed, like the race camera.
- Music id: `music_race_camel`. There is no music registry in the project (audio loads by file name from `Audio/Music/`), so the id is only
  named in `camel_race.json`. The track itself still has to be added to `Audio/Music/`.

## The six breeds

| Breed | الاسم | Coat | Top speed | Accel | Stamina | Handling | Price |
|---|---|---|---|---|---|---|---|
| Sufr | الصفر | yellowish | 1.00 | 1.00 | 1.00 | 1.00 | free (default) |
| Shuqah | الشقح | light sandy | 1.02 | 0.98 | 1.02 | 1.00 | $1.99 |
| Shaol | الشعل | reddish blond | 0.96 | 0.91 | 1.04 | 1.27 | $2.99 |
| Humr | الحمر | red brown | 1.00 | 1.31 | 0.91 | 0.89 | $2.99 |
| Maghateer | المغاتير (الوضح) | white | 1.07 | 1.12 | 0.64 | 0.96 | $3.99 |
| Majaheem | المجاهيم | black | 1.00 | 0.84 | 1.21 | 0.99 | $3.99 |

Balance rules, all enforced by tests:

- Overall power `3 x top + 1.5 x (accel + stamina + handling)` is 7.5 for every breed, within 3 %. In ranked races each breed is scaled to exactly 7.5.
- No breed is better than another on every stat.
- Different races have different winners (simulation in `CamelSim`): the white type wins a 500 m sprint, the agile type wins twisty ground,
  the black type wins a long race, the all-rounder wins the mixed distance. Tired camels keep less of their speed the shorter their breath.
- The existing camel mount ($1.99, `com.ahmdria1.aron.mount_camel`) is not changed. Breeds are a separate store slot (`camel_breed`) with ids `breed_<id>`.

## Age classes (cups)

حقايق، لقايا، جذاع، ثنايا، حيل: five cups, each opened by a podium in the one before. Each is longer, has more bends and faster rivals
(rival top speed 0.90, 0.94, 0.98, 1.02, 1.06 of the player's). Points are the same in every cup.

## Economy

- Points: 5 / 3 / 1, hard x1.5, online x2 with at least two real racers, easy pays nothing. Unchanged.
- Rent any breed for 100 race points for 48 hours. Rented breeds go back to Sufr when the time is up.
- A breed is owned only by a real purchase (non-consumable). No coin price, no loot boxes, no ads.
- Store rows are in `STORE_SETUP.md` (`com.ahmdria1.aron.breed_shuqah`, `_shaol`, `_humr`, `_maghateer`, `_majaheem`).

## Companions

`Resources/Companions/companion_names.json` lists who goes with whom. Each entry has an owner, a species and the string key
`companion_name_<owner>_<species>`, which is looked up in all twelve languages. Until a name exists the species name is shown.

| Companion | Owner | Name |
|---|---|---|
| Falcon | Aron | not chosen yet |
| Oryx | Noura | بيضا (Bayda) |
| Camel | Aron | not chosen yet |
| Horse | Aron | not chosen yet |

## The wild Arabian oryx

The المها (`saudi_oryx` in `local_animals.json`) lives in herds in the AlUla and northern desert stages: `madinah_5`, `madinah_6`,
`tabuk_3`, `tabuk_5`, `northern_3`, `northern_4`. It is peaceful scenery with the behaviour `flee_gently`: it grazes, lifts its head when the hero comes within
22 m, and trots calmly away inside 13 m (`OryxHerd`). It is never a hazard, never hurt by the hero, cannot be targeted by the aim assist and cannot be ridden.
Animation clips: idle graze, walk, trot, flee, alert (`oryx_wild_*` in `animation_list.md`).

## Animation hooks

`PlayerAnimationHooks`: `OnKneel`, `OnRise`, `OnStumble`, `OnBoost(bool)`, `OnFinishCelebrate(bool won)`. The clips are listed in
`docs/animation_list.md` under `camel_racer`, `robot_jockey`, `rider` (camel race) and `noura`. All are P1 or P2.

## Not yet checked in Unity

The art is placeholder data (colours and styles); the scene, the models, the animator wiring, the track music and the on-device feel of the race
have still to be built and tried on a phone. Prices are proposed and still have to be created in the store consoles.
