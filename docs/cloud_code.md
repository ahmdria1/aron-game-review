# Server module design (Unity Cloud Code, C# module)

One module, `AronServer`, built from the same `Aron.Logic` source the game uses (`Scripts/Logic/**`, no Unity references), so
offline and server rules cannot drift apart. Cloud Save keeps one record per player. Secrets live in Cloud Code secrets, never in the client.

## Endpoints

| Function | Input | Does | Limit key |
|---|---|---|---|
| `getTime` | none | Returns server UTC; the client calls `GameClock.SyncServer`. | none (cheap) |
| `claimRaceResult` | `RaceClaim`, `RaceReport`, vehicle id, integrity token | `RateLimiter("race_claim")`; vehicle speed cap from `race_vehicles.json` via `SpeedCaps`; `ServerEconomy.GrantOnlineRace` with server time; returns the award and the server's point balance. | player id |
| `rentItem` | item id | `ServerEconomy.Rent` with server time; returns the end time. Online rentals are stored server side; offline ones are checked by `TrustedClock`. | player id |
| `verifyPurchase` | `PurchaseReceipt` | Google: verify the signed JSON with the Play public key (`GooglePlaySignature`), then optionally the Play Developer API for the purchase token. Apple: App Store Server API transaction lookup. Then `ReceiptPolicy.Check`; on `Valid` write the entitlement and return `Valid`. | player id |
| `checkName` | name | `NameFilter.Allowed` then uniqueness. | player id + IP |
| `reportPlayer` | target id, reason code | Stores the report (no free text from children). | player id |
| `attest` | token, nonce | Play Integrity: decode the verdict with Google; App Attest: verify the assertion with Apple. Returns `DeviceTrust`. | player id |

## Data (Cloud Save, per player)

`SaveData`-shaped record (points, `pointLog` for daily caps, rentals, handled orders), `claims` (match ids seen, 7 days), flags from `SoftFlags`.
The record is written only by the module. The game keeps its own offline copy and takes the server's balance when they differ.

## Rules the module enforces (all covered by unit tests in `IntegrityTests.cs`)

- Daily caps per UTC day on server time: per track and source, online total, and the overall points cap.
- A match id is paid once. The claim signature, the report and the claim must agree.
- Lap and total times cannot be faster than the vehicle's speed ceiling; laps must add up to the total.
- A receipt is valid once; the product and package must match.
- Rate limits per endpoint with escalating lockouts.

## Payload size and data use

Requests are small JSON (under 1 KB; race report is a handful of floats). No polling: calls happen on race end, purchase, and app start (`getTime`).
See `docs/power_and_data.md` for the full data budget.
