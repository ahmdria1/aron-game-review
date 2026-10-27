# Usage statistics

Short version: off until a grown-up turns it on, a fixed list of events, no personal text, small batches, and a delete button.

## The rule

1. **No collection before consent.** `ConsentGate` is the only door. Until a grown-up turns statistics on (Settings > Privacy, behind the grown-up check `ParentalGateDialog`), `EventBuffer` keeps nothing, writes nothing and sends nothing. The default check in the buffer is "no", so a missing wire-up fails closed.
2. **Turning on needs the grown-up check; turning off never does.** A saved answer counts only for the privacy-notice version it was given under. A newer notice means off until answered again.
3. **Crash reports are a separate switch** (`Channel.Diagnostics`), with the same rules. They go to Sentry only. Unity Analytics never receives crash details, only the count.
4. **Server kill switch.** `features.analytics` in the server settings (`docs/remote_config.md`) can stop statistics for everyone. It can never turn them on without a grown-up's choice.
5. **Children.** The game is aimed at players of 7 and up, and young players are the normal case, so every opt-in goes through the grown-up check and there is no age question to get wrong. There is no profile, no custom player ID (`SetAnalyticsUserId` is never called), no advertising ID and no tracking permission anywhere. A test scans the project for the common advertising-ID names and permissions.

## The events

Nothing outside this list can be sent: `EventSchema.Check` refuses unknown names, extra or missing keys, ids with anything but lowercase letters, digits and `_ . -` (so no names, emails or free text can fit), and numbers out of range.

| Event | Keys | When |
|---|---|---|
| `first_open` | none | Once, ten seconds after the first start that has consent |
| `region_unlocked` | `region` (country id) | A stage result that opens a country for good |
| `stage_start` | `stage` (stage id) | A stage is chosen to play |
| `stage_complete` | `stage`, `seconds` (0 to 86400) | A stage is finished |
| `stage_fail` | `stage`, `seconds` | Out of lives |
| `boss_defeated` | `stage` | A boss falls |
| `purchase_completed` | `product` (store product id) | A real-money purchase was granted (not a restore). No price, receipt or order number |
| `crash_count` | `count` (0 to 100000) | Once per session, with the crash records not yet counted (`CrashReporter.TotalRecorded`) |

## Sending

- Events wait in a local queue (`analytics_queue.json`, at most 200, oldest dropped first). The queue is deleted at start if there is no consent, and when consent is withdrawn.
- A batch is due at 20 events or when the oldest is 15 minutes old. It goes out only when `DataGate` allows telemetry (Wi-Fi, or mobile data once per session with Data saver off), never during play, loading or in the background.
- A batch of 20 is under 6 kB of JSON before Unity's own packaging; the data meter counts about 60 bytes an event plus 400 bytes of headers. These are estimates; real traffic needs a device and a proxy to confirm.
- The batch is handed to Unity Analytics (`UgsEventSink`) with the SDK's consent state set to granted only for the length of that send and set back to denied straight after. Between batches the SDK is off, so nothing else leaves the phone. Because the events are recorded at send time, Unity's timestamp is the send time, not the play time. For day-level numbers that does not matter.
- Without the `com.unity.services.analytics` package (6.1 or newer, Unity 6.2 or newer) the sink is not compiled and nothing is ever sent; events only wait in the queue.

## What Unity adds on its own

Unity's service stores, for each event: the IP address, an ID for the installation, the anonymous player ID if sign-in is on, and the iOS vendor ID. That is why the privacy policy names Unity Analytics and the IP address (`docs/privacy_policy.md` §2.1) and why the data-safety form lists it (below).

## Delete my statistics data

`TelemetryService.EraseAll()`: switches both off, empties and deletes the queue, and asks Unity Analytics to delete what it holds (`RequestDataDeletion`, which retries across sessions and needs consent denied first, as the sink does). The same call belongs in the account deletion flow (`AccountData.DeleteSteps` has `analytics_data`); the account deletion screen does not exist in the runtime yet.

## Data-safety form and privacy labels (Ahmed to enter)

Google Play data safety, for the statistics and the crash reports, both optional (the player can turn them off):

| Item | Answer |
|---|---|
| App activity: app interactions | Collected, optional, analytics, not shared. (Unity is a service provider.) |
| App info and performance: crash logs | Collected, optional, app functionality and analytics, not shared (Sentry is a service provider) |
| Device or other IDs | Collected: installation ID and anonymous player ID, for analytics. Not the advertising ID |
| Approximate location from IP | Unity receives the IP address with each batch. Declare it if the form asks about IP-derived data |
| Encrypted in transit | Yes |
| Deletion | Yes: Settings > Privacy > Delete my statistics data, and account deletion |
| Advertising ID | Not used. Answer the Play advertising-ID declaration with "no" |

App Store privacy labels: Product Interaction and Crash Data, linked to the player ID only through the anonymous ID, not used for tracking. Do not add the App Tracking Transparency prompt: nothing here tracks across other companies' apps. Leave the Kids category unselected (see `docs/compliance_review.md`).

## Setting it up (needs Ahmed's Unity account)

1. Install `com.unity.services.analytics` (6.1 or newer) in the Package Manager and link the project to the Unity dashboard.
2. Check the merged Android manifest does not contain `com.google.android.gms.permission.AD_ID`. If it does, remove it with `tools:node="remove"` in a custom main manifest.
3. In the Unity dashboard, Analytics, set data retention as short as the plan allows and leave "Unity ID" linking off.
4. Build once on a phone, turn statistics on in Settings > Privacy, play a stage, wait for the menu, and check the events arrive in the dashboard's event browser. Then turn it off and check nothing new arrives.
5. Do not set a Unity "Analytics user ID" and do not enable Unity Ads or any other advertising package: that would change every statement above.

## Tests

`TelemetryTests`: default off (also for old saves), grown-up requirement, notice version, channel independence, schema (every event, only the agreed keys, no personal text, number limits), buffer (no consent keeps nothing, consent checked at every event, batch due rules, cap, saved queue handling), crash count, region-opened detection, account deletion step, the docs naming every event, and a project-wide scan for advertising-ID names. The runtime classes (`TelemetryService`, `UgsEventSink`, the screen) need Unity and are not compiled by the test project.
