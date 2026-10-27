# Power and data

Everything here is a design target or a computed estimate. Real heat, battery drain and mobile-data use can only be confirmed on phones;
the numbers below say what the game is built to, and `docs/perf_checklist.md` says how to measure them.

## Frame rate and power

| Screen | Loop | Drawn | Why |
|---|---|---|---|
| Gameplay, any tier | 30 fps | 30 fps | Cooler and longer battery life than 60. |
| Gameplay, High tier, "60 fps" switched on in Settings > Power and data | 60 fps | 60 fps | Only while the battery is not low, the phone is not warm and the OS is not in power-save mode. If the frame governor finds it cannot hold 60, it drops to 30 and stays there until the next launch. |
| Menus, pause, loading | 30 fps | 15 fps | The loop stays at 30 so taps feel instant; only every second frame is drawn (`OnDemandRendering`). |
| Phone very hot (critical) | 20 fps | 20 fps | Last resort before the phone throttles itself. |
| Game in the background | 1 fps | nothing | Audio, network and timers are stopped; the freeze watchdog is suspended. |

Decision code: `FrameCapPolicy` in `Logic/Performance/Power.cs` (tests: `PerformanceTests.cs`).

### Heat

The phone reports heat; the game maps it to four levels and gets cheaper in this order, one step per level:

| Level | iOS thermal state | Android thermal status | Resolution | Shadows | Foliage kept | Particles kept |
|---|---|---|---|---|---|---|
| Nominal | nominal | none, light | 100% | soft | 100% | 100% |
| Fair | fair | moderate | 85% | hard | 85% | 80% |
| Serious | serious | severe | 75% | off | 70% | 60% |
| Critical | critical | critical and above | 65% | off | 40% | 35% |

Getting hotter is obeyed at once. Getting cooler is believed only after 60 seconds at the cooler level, one level at a time, so quality does not flicker (`ThermalGovernor`).
The same step 1 is applied when battery saver is on, the OS is in power-save mode or the battery is under 20% and not charging.

### Battery saver

Settings > Power and data > Battery saver. 30 fps, plus the step-1 quality above. When the battery drops below 20% (not charging) or the OS power-save mode is on,
the menu asks once whether to turn it on; a "no" is remembered for the rest of that day.

### Idle work

- The phone's battery and heat are sampled every 5 s in play and every 15 s in menus. This is cheap, but it is sampling, not an event.
- Animated props (falcon, lantern flicker, spinning landmarks, bobbing dates) switch themselves off while no camera sees them (`VisibilityGroup`).
- The score and timer text refresh 4 times a second, not every frame.
- Loading uses `SceneLoader` (async, with a loading bar); it pauses the freeze watchdog and draws at 15 fps.
- There are no polling loops; waits use coroutines or events.

## Design budgets per tier

Per frame, at the target rate. CPU is main-thread work and is kept well under the frame so the processor can idle.

| Tier | Target | Frame | CPU | GPU | Draw calls | Triangles | Memory |
|---|---|---|---|---|---|---|---|
| Low | 30 fps | 33.3 ms | 14 ms | 20 ms | 150 | 200k | 700 MB |
| Medium | 30 fps | 33.3 ms | 14 ms | 22 ms | 250 | 400k | 1100 MB |
| High | 30 fps | 33.3 ms | 12 ms | 20 ms | 400 | 800k | 1600 MB |
| High | 60 fps | 16.7 ms | 8 ms | 12 ms | 400 | 800k | 1600 MB |

`PerfBudgets.Check` compares a measurement with these (5% slack on average frame time, 80% of the target for the 1% low).
The debug overlay shows the live numbers. Textures are compressed with ASTC (6x6 colour, 5x5 normal maps, 4x4 UI) by `TextureImportDefaults`.

## Memory

On a memory warning the game frees caches in this order: sign textures and other rebuildable images first, then pooled effects, then (only when critical) idle audio.
Registration happens through `Caches.Registry`; `TrimAll` is covered by tests.

## Data

| Traffic | Rule |
|---|---|
| Race state | Compact binary: positions quantised (5 cm, 10 cm, 0.25 m/s), only what changed since the last packet, a full snapshot every 3 s. Sent 10 times a second, 8 with Data saver. |
| Downloads | Wi-Fi only unless "Downloads on mobile data" is on. Never during gameplay. Nothing is streamed while playing. |
| Leaderboard and profile | `If-None-Match` with a stored ETag; a "not modified" answer costs a few hundred bytes. |
| Sync (saves, config) | Wi-Fi, or mobile data when Data saver is off. |
| Error reports and statistics | Gzipped and batched (60 items or 24 KB, oldest dropped). Wi-Fi, or once per session on mobile data when Data saver is off. Never in the background. |
| Ads and background trackers | None. |

### Online race estimate

Computed by `BandwidthEstimator` (tests: `DataTests.cs`) for a 10-minute simulated race, scaled to one hour. Each packet pays 57 bytes of headers
(IPv4 20, UDP 8, DTLS 13, tag 16), which is more than the payload, so fewer, fuller packets matter more than smaller fields.

| Players | 10 Hz total | 8 Hz total (Data saver) |
|---|---|---|
| 2 | 4.46 MB/h | 3.58 MB/h |
| 3 | 4.60 MB/h | 3.70 MB/h |
| 4 | 4.75 MB/h | 3.82 MB/h |
| 6 | 5.02 MB/h | 4.06 MB/h |
| 8 | 5.32 MB/h | 4.31 MB/h |

The goal is 2 to 5 MB per hour, so online races on phones are capped at 4 players (`RaceNetRate.MaxPlayersOnMobile`). These figures are for continuous racing
packets and exclude the matchmaking handshake. The simulation assumes steady play with packets at an even rate and no loss; real mobile networks add retries.

## Error reports

`CrashReporter` records managed errors and exceptions, and main-thread freezes longer than 2 s (the kind that become an ANR), with the last 30 things the player did.
Names and email addresses are scrubbed first. Reports are queued on the phone (at most 30, 20 new per session) and sent as one gzipped Sentry envelope only if:

1. the build has the project address (CI writes `Resources/Config/crash_reporting.json` from the secret `ARON_CRASH_DSN` for the build and deletes it afterwards),
2. the player has allowed diagnostics (Settings > Privacy, off until a grown-up turns it on), and
3. the data rules above allow it.

Hard native crashes cannot be caught from C#. Use Google Play's Android vitals and Xcode's Organizer, which cost nothing. In Sentry's project settings, turn on "prevent storing IP addresses".

## Usage statistics (opt-in)

Off by default. When a grown-up turns them on they add a handful of events per play session, sent in batches of 20 (or after 15 minutes) when `DataGate` allows telemetry, never during play. Estimate: about 60 bytes an event plus 400 bytes of headers, so a long evening of 40 events is roughly 3 kB; this is an estimate, not a measurement. Details in `docs/analytics.md`.

## Pooling

Effects played through `FxPlayer` come from pools (`Pool<T>`, with a delayed-release scheduler), so hits, dust and sparks create no objects during play.
Not yet pooled, and good next candidates: date drops from enemies and the boss shock wave. A unit test (`AllocationCheck`) guards the pure logic for per-frame allocations;
the Unity side needs the profiler on a device.

## Not verified here

- Nothing in the Unity project was compiled or run in this environment; only the pure-C# logic is covered by the unit-test suite.
- Heat, battery drain, frame times and real data use need a phone. Use `docs/perf_checklist.md`.
