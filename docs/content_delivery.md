# Country content on demand

The first install holds Riyadh and Diriyah, the menus and the core sound. Each country's music and regional assets live in their own remote group and are downloaded the first time they are needed.

## What is where

| Part | Where |
|---|---|
| Base music (11 tracks, menu and the three base stage themes) | `Assets/_Aron/Resources/Audio/Music/` (in the APK) |
| Country and regional music (40 tracks, 35 groups) | `Assets/_Aron/Remote/Music/country_<id>/` (Addressables group `country_<id>`, remote) |
| Which stage needs which group, and the base fallback track | `Aron.Logic` `ContentMap` (pure code, unit tested) |
| Wi-Fi / mobile data rules and progress model | `Aron.Logic` `DownloadPolicy`, `DownloadSession` |
| Unity side (coroutines, screen) | `Scripts/Content/` (`ContentService`, `ContentProgressScreen`) |
| Addressables backend (optional package) | `Scripts/Content/Addressables/` (compiled only if `com.unity.addressables` is installed) |
| Group set-up and build | `Editor/Content/AronContentSetup.cs`, menu **Aron > Content** |

Assets in a `Resources` folder cannot be Addressable, which is why the country music moved out of `Resources`.

## Rules the player sees

* Wi-Fi: downloads go ahead. A quiet prefetch of the next countries on the map starts from the map or the main menu, on Wi-Fi only.
* Mobile data: nothing downloads unless the player ticks **Settings > Use mobile data for content**, or taps *Use mobile data* on the progress screen for that download.
* Never during gameplay. A prefetch is cancelled when a stage starts, and nothing starts from inside a stage.
* The progress screen offers *Use mobile data*, *Wait* (back to the map), *Skip* (play with the base music) and *Retry* after an error. Skipping never blocks a stage.
* A track whose group is not on the phone plays the base track for that kind of stage, never silence.

## Hosting (needs a decision from Ahmed)

1. Build the content: **Aron > Content > Build** (or the Android build, which calls it). Output: `ServerData/Android/` (catalog, hash and bundles). It is not committed (`.gitignore`).
2. Upload the folder `ServerData/Android/` to any static host or CDN (HTTPS, no special server), as `<base>/Android/`.
3. Put the base address in `Assets/_Aron/Resources/Config/content.json`: `{ "baseUrl": "https://cdn.example/aron/v1" }`. While it is empty, `ContentHosting.Configured` is false, no download is offered, and every country theme falls back to base music.
4. Content is tied to the app's catalog hash: a new app build that changes the groups needs a new upload to a new versioned folder (`v1`, `v2`, …); old installs keep using their own folder.

## Not verified yet

* A download on a real phone, over a real CDN, on Wi-Fi and on mobile data. The rules, the screen and the fall-back are tested with a fake backend; the Addressables backend is compiled and its group set-up checked in EditMode, but the network path is not exercised.
