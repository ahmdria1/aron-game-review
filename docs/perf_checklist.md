# Performance checklist

Run on a real phone, a release (not development) build, in airplane mode first, then with Wi-Fi on. Write the numbers in the release notes.

## Devices
- One Low phone (3 GB RAM or less), one Medium, one High. Add one iPhone.
- Start at 100% battery and room temperature; let the phone cool between runs.

## Frame time (each tier, each of the 8 stages, 5 minutes each)
- [ ] Debug overlay: average frame, 1% low, draw calls, triangles and memory are inside the table in `docs/power_and_data.md`.
- [ ] No frame longer than 100 ms after the first 3 seconds of a stage.
- [ ] Switch quality: heat steps (Settings > Power and data > Battery saver) change the picture without a hitch.
- [ ] Menus and pause really draw at 15 fps (overlay).

## Power
- [ ] 30 minutes of play at 30 fps: battery drop and back-of-phone warmth noted. Repeat with 60 fps on High and with battery saver on.
- [ ] Android: `adb shell dumpsys thermalservice` while playing; the game steps down when the status reaches moderate.
- [ ] iOS: Xcode Energy gauge stays low in menus.
- [ ] Put the game in the background for 5 minutes: no audio, CPU near zero, no network (Android Profiler or Instruments).
- [ ] Low battery (under 20%, unplugged) shows the battery-saver question once.

## Memory
- [ ] Android: `adb shell dumpsys meminfo` after 30 minutes stays under the tier budget and does not climb.
- [ ] Simulate a memory warning (`adb shell am send-trim-memory ... RUNNING_CRITICAL`): game keeps running and caches shrink.

## Allocations
- [ ] Unity Profiler, GC Alloc column during a stage: no allocation per frame in gameplay scripts.
- [ ] Pooled effects: instance count stays flat during a long fight.

## Data
- [ ] Online race of 10 minutes on mobile data: total from the carrier counter or Android Network Profiler is close to the estimate in the data table (about 0.8 MB per 10 minutes for 4 players).
- [ ] Data saver on: race is at 8 Hz and leaderboard answers 304 on repeat visits.
- [ ] No large download starts on mobile data unless allowed in Settings.
- [ ] Error reports: with consent and Wi-Fi, one gzipped upload per session; none in airplane mode.

## Freeze and error reports
- [ ] Block the main thread for 3 seconds in a test build: a "Main thread frozen" report appears in the queue file.
- [ ] Throw an exception in a test build: it is recorded once per kind, name and email scrubbed.

## Loading
- [ ] Scene changes show the loading bar and never freeze the screen.
- [ ] Textures show ASTC in the platform override on the Android and iOS tabs.
