# Race mounts, points and the shop

Data: `Resources/Mounts/race_mounts.json`, `Resources/Mounts/race_vehicles.json` (sea, cars, bicycles, motorcycles), `Resources/Mounts/race_points.json`, `Resources/Store/StoreCatalog.json` (tab "Mounts" and the saddle, decoration and thobe-colour items). Logic: `Scripts/Logic/Races/RaceMounts.cs`, `RacePoints.cs`. Tests: `MountTests`, `MonetizationTests`.

## The five mounts

Every mount is a trade-off. Raw power (the weighted sum of its stats) sits inside the same band around the budget 8.1 (±4%, checked by `MountFile.Validate`), so no mount is simply better.

| Mount | Top speed | Accel | Turning | Jump | Sand resist | Heat resist | Ability | Trade-off |
|---|---|---|---|---|---|---|---|---|
| Arabian horse (free) | 1.00 | 1.00 | 1.00 | 1.00 | 0.10 | 0.10 | steady | The baseline: no weakness, no strength. |
| Camel | 0.92 | 0.92 | 0.95 | 0.90 | 0.55 | 0.45 | sand walker | Slower on clean ground, but sand and heat barely slow it. Best on desert tracks. |
| Ostrich | 1.20 | 1.05 | 0.70 | 0.95 | 0.05 | 0.05 | sprint | Fastest on the straight, hard to steer, and sand and heat hurt it. |
| Giraffe | 1.00 | 0.80 | 0.85 | 1.40 | 0.10 | 0.10 | high path | Slow to start, jumps over obstacles and can take the high shortcuts. |
| Falcon glider | 1.02 | 1.10 | 1.10 | 0.80 | 0.00 | 0.00 | glide (1.2 s) | Nimble and glides over gaps, but cannot collect ground pickups while gliding. |

**Story and stage races** use the raw stats. **Ranked and online races** scale all stats of a mount by one factor so its power equals the budget exactly (`MountFile.StatsFor`, `AbilityAmount`). The shape of the mount stays, so ranked is about choosing a style, not about what was paid for. The computer racers use the same stats and are clamped to their mount's top speed (see `docs/computer_racers.md`).

## Prices and how a mount or vehicle is owned

**Permanent ownership is by real purchase only.** There is no way to own a mount or vehicle for good with race points, gold medals or coins (`StoreCatalog.Validate` refuses it, and `MountTests.NoPathBuysAMountOrVehicleForGoodWithPointsOrGold` checks the code and the data). Race points only **rent** (100 points, 48 hours, see below).

| Mount | USD tier | Product id |
|---|---|---|
| Horse | free | — |
| Camel | $1.99 | `com.ahmdria1.aron.mount_camel` |
| Ostrich | $2.99 | `com.ahmdria1.aron.mount_ostrich` |
| Giraffe | $3.99 | `com.ahmdria1.aron.mount_giraffe` |
| Falcon glider | $4.99 | `com.ahmdria1.aron.mount_falcon_glider` |

- The USD tier is only a guide for Ahmed. The real price is set in Google Play Console and App Store Connect, and the game shows the local price the store returns. There is no price in riyals in code.
- There are no random boxes and no paid advantage in ranked.
- **Rent any premium item** (a mount, a vehicle, a saddle, a thobe colour or an older look) for **100 points for 48 hours** (`rentPoints`, `rentHours`). Renting again **adds 48 hours onto the end of the running rental**. Paying for the item while it is rented buys it for good and ends the rental. Free defaults can't be rented.

## The four other vehicle classes

Shop tabs: Sea, Cars, Bicycles, Motorcycles (`tab_sea`, `tab_cars`, `tab_bicycles`, `tab_motorcycles`, 12 languages). All vehicles are original, with no brands and no copies of real models ("Water scooter", not a trademarked name). Names are in 12 languages (`r<id>` strings, e.g. `rboat_dhow`); the one-line trade-off text is Arabic only (`descAr`) for now.

Each class has its own power budget (8.0, ±4% in the data, `VehicleFile.Validate`) and ranked races scale every vehicle to its class budget (`VehicleFile.StatsFor`), so a vehicle chooses the shape of its strength, not how strong it is. The stats feed the same `TerrainRules` as mounts (`terrain` = sand/mud resist, `weather` = heat/rain resist).

| Class | Id | Name | Price | Top / accel / turn / jump | Terrain / weather | Stamina | Ability |
|---|---|---|---|---|---|---|---|
| Sea | `boat_pearl` | Pearl rowing boat | free | 1.00 / 1.00 / 1.00 / 1.00 | 0.15 / 0.10 | — | steady |
| Sea | `boat_dhow` | Sail dhow | $1.99 | 0.85 / 0.75 / 0.70 / 0.85 | 0.80 / 0.80 | — | tailwind |
| Sea | `boat_speed` | Speed boat | $2.99 | 1.25 / 1.05 / 0.65 / 0.80 | 0.10 / 0.10 | — | planing |
| Sea | `boat_water_scooter` | Water scooter | $3.99 | 0.98 / 1.20 / 1.10 / 0.70 | 0.10 / 0.00 | — | wave_hop |
| Cars | `car_starter` | Trip car | free | 1.00 / 1.00 / 1.00 / 1.00 | 0.15 / 0.10 | — | steady |
| Cars | `car_sports` | Teal wedge sports car | $3.99 | 1.20 / 1.10 / 0.95 / 0.60 | 0.00 / 0.05 | — | slipstream |
| Cars | `car_convertible` | Coral classic convertible | $2.99 | 0.95 / 0.95 / 1.10 / 0.90 | 0.35 / 0.05 | — | cruise |
| Cars | `car_buggy` | Dune buggy | $2.99 | 0.85 / 0.90 / 0.90 / 1.10 | 0.55 / 0.10 | — | bounce |
| Cars | `car_4x4` | Desert 4x4 | $3.99 | 0.85 / 0.70 / 0.75 / 0.85 | 0.90 / 0.70 | — | low_gear |
| Bicycles | `bike_city` | City bike | free | 0.90 / 0.90 / 0.95 / 0.80 | 0.10 / 0.05 | 0.90 | pedal_rhythm |
| Bicycles | `bike_road` | Road bike | $0.99 | 1.10 / 0.80 / 0.85 / 0.60 | 0.00 / 0.05 | 1.05 | pedal_rhythm |
| Bicycles | `bike_mountain` | Mountain bike | $1.99 | 0.80 / 0.80 / 0.90 / 0.95 | 0.40 / 0.10 | 0.85 | pedal_rhythm |
| Bicycles | `bike_fat` | Fat-tire desert bike | $1.99 | 0.80 / 0.65 / 0.75 / 0.70 | 0.90 / 0.50 | 0.80 | pedal_rhythm |
| Bicycles | `bike_bmx` | BMX bike | $0.99 | 0.80 / 1.00 / 1.10 / 1.10 | 0.10 / 0.00 | 0.50 | pedal_rhythm |
| Motorcycles | `moto_scooter` | Scooter | free | 1.00 / 1.00 / 1.00 / 1.00 | 0.10 / 0.10 | — | steady |
| Motorcycles | `moto_dirt` | Dirt bike | $1.99 | 0.85 / 1.00 / 1.00 / 1.10 | 0.40 / 0.10 | — | bump_absorb |
| Motorcycles | `moto_sport` | Sport bike | $3.99 | 1.25 / 1.10 / 0.95 / 0.60 | 0.00 / 0.00 | — | slipstream |
| Motorcycles | `moto_cruiser` | Cruiser | $2.99 | 1.05 / 0.75 / 0.70 / 0.80 | 0.40 / 0.60 | — | cruise |
| Motorcycles | `moto_quad` | Desert quad | $2.99 | 0.85 / 0.85 / 0.80 / 0.90 | 0.90 / 0.40 | — | all_terrain |

Trade-offs in one line each:

- **Pearl rowing boat** (free): قارب تجديف مجاني: متوازن وهادي، ما فيه شي يميّزه.
- **Sail dhow** ($1.99): شراع يستفيد من الريح ويتحمل الموج، بس بطيء في الانعطاف والتسارع.
- **Speed boat** ($2.99): الأسرع على الماء، لكن صعب التوجيه ويتأثر بالموج.
- **Water scooter** ($3.99): تنعطف بسرعة وتقفز فوق الموج، بس سرعتها القصوى متوسطة.
- **Trip car** (free): سيارة مجانية متوازنة وسهلة القيادة.
- **Teal wedge sports car** ($3.99): سريعة جداً وتتسارع بقوة، لكنها منخفضة وتتعثر في الرمل والقفزات.
- **Coral classic convertible** ($2.99): تنعطف بسلاسة وتتعافى بسرعة من الصدمات، لكن سرعتها متوسطة.
- **Dune buggy** ($2.99): تقفز وتتحمل الكثبان، لكن سرعتها القصوى أقل.
- **Desert 4x4** ($3.99): تتخطى الرمل والمطر بثبات، لكنها بطيئة في الانطلاق والانعطاف.
- **City bike** (free): دراجة مجانية: متوازنة، والإيقاع يعطيها دفعة صغيرة.
- **Road bike** ($0.99): الأسرع على الأسفلت ونفسها طويل، بس ضعيفة في الرمل والقفزات.
- **Mountain bike** ($1.99): تتحمل الطرق الوعرة والقفزات، لكنها أبطأ على الطريق المعبّد.
- **Fat-tire desert bike** ($1.99): إطارات عريضة تمشي على الرمل والحر بسهولة، لكنها بطيئة وثقيلة.
- **BMX bike** ($0.99): خفيفة تنعطف وتقفز بخفة، بس نفسها قصير وسرعتها قليلة.
- **Scooter** (free): سكوتر مجاني متوازن وسهل.
- **Dirt bike** ($1.99): يقفز ويتحمل الطرق الترابية، لكنه أبطأ على الإسفلت.
- **Sport bike** ($3.99): سريعة جداً على الإسفلت، لكنها صعبة في الرمل والقفزات.
- **Cruiser** ($2.99): ثابتة ومريحة ضد الريح والحر، لكنها ثقيلة في الانعطاف.
- **Desert quad** ($2.99): تمشي على الرمل بثبات، لكنها أبطأ وأصعب في الانعطاف.

Class rules:

- **Bicycles:** pedal-rhythm stamina (`PedalStamina`): sprinting is faster but drains the rider; tapping on the half-second beat gives a short speed pulse and a breather; out of breath is slower until recovered. **Helmets are always on** (`VehicleFile.Headgear`), and **Noura rides in her abaya** (`OutfitFor`).
- **Motorcycles:** helmets always on; **Noura rides in her abaya too** (my reading of "always modest", needs Ahmed's OK). A crash is a 7+ friendly cartoon tumble (1.2 s) and a quick respawn on the track (0.8 s), then 2 s of protection at half speed (`CrashState`). No injury, no blood. Each bike links an engine sound in `Resources/Audio/engine_audio.json` through `engineId`: scooter, dirt_bike (the dirt bike **and the desert quad**, which has no sound of its own yet), sport_bike, cruiser.
- **Sea and cars:** no helmet, no crash rule; Noura keeps her usual modest outfit.
- **Tracks** name a `vehicleClass` (`mounts`, `sea`, `cars`, `bicycles`, `motorcycles` or `gear` for skis and the surfboard) that must match the race kind (`RaceFile.Validate`). The kinds `bicycle` and `motorcycle` exist with their surfaces, but **none of the 35 shipped tracks uses them yet**; adding one is a data change.

## Cosmetics (no effect on stats)

| Item | Price |
|---|---|
| Royal saddle, one per mount (5) | $0.99 each, `com.ahmdria1.aron.saddle_royal_<mount>` |
| Plain saddle | free |
| Sadu saddle (looks only) | 300 coins |
| Tassels (decoration, looks only) | 500 coins |
| Thobe colours: cream, light grey, sky blue, olive, navy, brown, black (7 paid; white is the default) | $0.99 + 600 coins each, `com.ahmdria1.aron.thobe_c_<colour>` |

Coins are the only thing that can buy the sadu saddle and the tassels, and they never touch speed. The shop catalog has 96 items. Paid items are non-consumable and restorable.

## Race points (exactly 5 / 3 / 1)

| | 1st | 2nd | 3rd |
|---|---|---|---|
| Finish points (Normal) | 5 | 3 | 1 |
| Hard (x1.5, rounded down) | 7 | 4 | 1 |
| Online with at least 2 real racers (x2) | 10 | 6 | 2 |
| Online and Hard (x1.5 x2) | 15 | 9 | 3 |

- **There is no winner bonus and no ghost bonus.** The old 40/15/8 and 25/6/0 are gone (they made 100 points arrive in about three races).
- Easy gives 0. Ghost races pay the same as computer-racer races (5/3/1, x1.5 on Hard) and only on Normal and Hard. Computer racers never count as real racers.
- Anti-abuse caps, lowered with the smaller numbers (a proposal for Ahmed): **3 awards per track and source per day** (was 5), **10 online awards per day** (was 20), and **100 points per day from all races together** (new). The last race of the day pays only what is left. One day's play is therefore at most one 48-hour rental, never a pile of them. Change `dailyPerTrack`, `dailyOnline` and `dailyPoints` in `race_points.json` if it feels slow.
- `racePointsLifetime` is only a statistic now; it opens nothing.
- Points are saved in `SaveData.pointLog` (with the date, track and source, plus a daily total entry) and rentals in `SaveData.rentals`. Save version is 4.

## Server-side validation of online points

The client never grants online points by itself. It sends a `RaceClaim` (player id, match id, track, place, real and computer racer counts, seconds, signature) to a Cloud Code module, which runs `ClaimValidator.Check`. The class is plain C# and is shared with the module, so the same code runs in the tests and on the server. It refuses: missing ids, an unknown track, fewer than 2 real racers, a bad field size or place, an implausible time (`ScoreCheck.Plausible` against the mount's top speed), a bad signature, and a repeated match (one claim per player and match).

**Limits to be honest about:** the claim signature (`SignClaim`) uses a per-build salt that ships inside the client, so it only stops casual edits. It is not a secret and no API key is involved. The real protection is on the server: the plausible-time check, one claim per player and match, and the counts of real racers. For that last part to mean anything the server must know the real result of the match, so the `matchId` has to come from the lobby service and the module should compare the claim with what the lobby recorded. That lobby recording is **not built**.

**What Ahmed must create (none of this exists yet):**

1. In Unity Gaming Services: a Cloud Code module (suggested name `AronRaceClaims`) that references the Logic assembly, calls `ClaimValidator.Check` with the lobby's own record of the match, and on success writes the points to Cloud Save. Deploy it and put its endpoint in the client config.
2. If the lobby or matchmaking uses any provider key, keep it in UGS Secrets (server side only). Nothing secret goes in the client or in this repo.
3. Products in Play Console and App Store Connect for every product id above (see `STORE_SETUP.md`), and the price tiers.
4. Do **not** create `com.ahmdria1.aron.remove_ads`.

The offline race has no server. It grants points on this device only, with the same daily caps. Because the save file is on the device, offline points are for fun and for 48-hour rentals only; they never count in a ranking and never buy anything for good.

## Honest status

- All of the above is data and Logic with tests. The shop screen shows the mounts; the **race screen, the mount picker before a race and the race runtime do not exist in this repo yet**. Nothing in the client calls `ClaimValidator` until the race runtime is built.
- Mount and vehicle models and their animations are not made. The giraffe, ostrich, falcon glider and all 19 vehicles use placeholders. The desert quad has no engine sound of its own.
- No shipped track uses a bicycle or motorcycle race yet.
- Prices and points are a first proposal and need a play test.
