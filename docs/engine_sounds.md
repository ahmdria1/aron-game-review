# Motorcycle sounds

The 18 files in `Assets/_Aron/Resources/AronSfx/engine_*.ogg` are the final engine sounds, original to Aron and not taken from or imitating any existing game.
The loops are 3 s, seamless, Ogg Vorbis, with only the owner's tags. The tuning (pitch range, whine, gears, voice limits) lives in `Resources/Audio/engine_audio.json`.

Character: funny and cartoonish "putt-putt-putt", pitch and speed rise with the revs, higher and whinier at speed.

| File(s) | What | Character |
|---|---|---|
| engine_scooter_low / mid / high | loops, 3 rev bands | tiny high buzz, fast small putts |
| engine_dirt_low / mid / high | loops | rattly, loose-chain clatter over a putt |
| engine_cruiser_low / mid / high | loops | goofy deep "blub-blub", slow and round |
| engine_sport_low / mid / high | loops | whiny zoom, a thin whine that grows at high revs |
| engine_sputter_1 | start | cough, sputter, splutter, then settles into idle (about 1 s) |
| engine_backfire_1, _2 | boost | cartoon backfire pop with a short crackle |
| engine_squeak_1 | hard braking | squeaky brake, rubber-duck-like but not a duck |
| engine_boing_1, _2 | landing | comic "boing" spring, two takes |

Note: the desert quad (`moto_quad`) reuses the `dirt_bike` engine until it gets its own loops. The motorcycles link their engine in `race_vehicles.json` (`engineId`).

Neighbouring loops of one bike should be recorded at matching base pitch so the crossfade (equal power) does not jump.
