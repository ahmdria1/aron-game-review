# Server settings: kill switches, tuning and timed events

Everything here can be changed from the Unity dashboard without a new app version. Nothing here is needed to play: with no network the game runs on the file it ships with, and with no file on the built-in defaults.

## Where the settings come from

The settings in force are picked in this order (`SettingsResolver`):

1. The last answer from the server, if it parses and its `version` is not lower than what the phone already has.
2. The last good answer kept on the phone (`remote_settings.json` in the app's data folder).
3. `Assets/_Aron/Resources/Config/remote_defaults.json`, shipped inside the game.
4. The built-in defaults (every switch on, no tuning, no events).

An answer that is empty, broken, or older is ignored and the layer below it is used. A partial document is fine: keys that are missing keep their safe default. Every value is range-checked (`SettingsPolicy.Sanitize`) before it is used, so a typo in the dashboard cannot make the game impossible.

## Asking the server

- Once, five seconds after start, and again after coming back from the background if the last ask was more than 6 hours ago. There is no polling.
- Only when the data rules allow a small request (`NetworkState.Allow(Traffic.Sync)`): Wi-Fi, or mobile data with Data saver off. It waits and retries the check every minute, not the request.
- The source is `UgsSettingsSource`, compiled only when the packages `com.unity.remote-config` and `com.unity.services.authentication` are installed. It reads one JSON key named `settings`. The only identifier involved is the anonymous service player id. No advertising id, no personal data.
- Without those packages `GameSettingsService.Source` stays null and nothing is requested.

## The document

```json
{
  "version": 2,
  "features": {
    "onlineRaces": true,
    "store": true,
    "purchases": true,
    "events": true,
    "shopSigns": true,
    "vehicleSea": true,
    "vehicleCars": true,
    "vehicleBicycles": true,
    "vehicleMotorcycles": true
  },
  "difficulty": { "enemySpeedScale": 1.0, "bossHealthScale": 1.0, "extraLivesBonus": 0 },
  "events": [
    { "id": "weekend_dates", "startUnix": 1800000000, "endUnix": 1800172800, "datesMultiplier": 2.0, "nameKey": "" }
  ],
  "pinMode": "reportOnly"
}
```

Raise `version` with every change. The phone only replaces its copy with an equal or higher number.

### Kill switches (`features`)

All default to `true`. Setting one to `false` stops new use; it never removes anything a player owns.

| Key | What it does when `false` |
|---|---|
| `onlineRaces` | The entry point for online races reports "paused". The switch is read through `GameSettingsService.IsOn(Feature.OnlineRaces)`. At the time of writing there is no online-race screen in the runtime yet, so this is the gate to call from it. |
| `store` | Shop fronts show a short "paused" message instead of opening the shop. |
| `purchases` | Real-money buying is refused with the same message. Buying with coins, renting, equipping and **Restore purchases** keep working. |
| `events` | Every timed event is ignored. |
| `analytics` | Usage statistics stop for everyone: nothing is recorded or sent and the waiting queue is deleted. It can never turn statistics on; only a grown-up's choice in Settings > Privacy can (`docs/analytics.md`). |
| `shopSigns` | The shop-sign file is not downloaded; the shipped signs are used. |
| `vehicleSea`, `vehicleCars`, `vehicleBicycles`, `vehicleMotorcycles` | That vehicle tab is hidden in the shop. Vehicles already owned stay in the save. |

### Difficulty tuning (`difficulty`)

Applied on top of `difficulty.json` by `DifficultyTuning.Apply`, which returns a copy and never changes the loaded file.

| Key | Limits | Effect |
|---|---|---|
| `enemySpeedScale` | 0.8 to 1.2 | Multiplies enemy speed. |
| `bossHealthScale` | 0.8 to 1.2 | Multiplies boss health. |
| `extraLivesBonus` | -1 to 2 | Added to the level's extra lives (never below 0). |

`GameSettingsService.Tuned(level)` is the call to use where a level is read. At the time of writing `difficulty.json` is only read by the logic layer and its tests, so the runtime does not yet call it.

### Timed events (`events`)

Up to 8 events. Each needs a unique `id` (at most 40 characters), a start before its end, and a length of at most 60 days. `datesMultiplier` is between 1 and 2 and multiplies dates picked up in `GameManager.AddDates`. Overlapping events take the largest multiplier, not the product. The highest the extra coins per save can go is still far under the wallet audit limit (`WalletWatch.MaxCoinGain`).

Times are Unix seconds, UTC. An event counts only while the game's own clock is trusted (`GameClock.CanEarn`); a phone whose date was moved back or forward never starts or extends one.

### Certificate pinning (`pinMode`)

`reportOnly` or `enforce`. `enforce` is honoured only when at least two pins are shipped (`CertPins.SafeToEnforce`); otherwise it stays report-only. The server cannot ask for `off`: that needs a new app version. Moving from `enforce` back to `reportOnly` is the way out if a certificate was renewed by mistake.

## Setting it up (needs Ahmed's Unity account)

1. Install `com.unity.remote-config` (4.x) and `com.unity.services.authentication` in the Package Manager, and link the project to the Unity dashboard.
2. In the dashboard, Remote Config, add a **JSON** key named `settings` and paste the document above.
3. Change the document, raise `version`, publish.

## What is tested

`SettingsTests` covers: defaults, the shipped file, partial documents, every limit above, bad events, event windows with and without a trusted clock, the layer order, broken and older answers, a parser that throws, and pin mode. The runtime classes (`GameSettingsService`, `UgsSettingsSource`, the shop wiring) need Unity and are not compiled by the test project.
