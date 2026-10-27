# Security notes

What protects the game, what each layer can and cannot do, and what Ahmed has to set up on his own accounts.
The honest rule: nothing running on the player's phone can be made unbreakable. The client layers raise the effort;
anything that matters for fairness or money is decided on the server.

## 1. Saved game (encryption and tamper detection)

Code: `Scripts/Logic/Security/SaveVault.cs` (pure C#, unit-tested), `Scripts/Save/SaveSystem.cs`, `Scripts/Save/DeviceKeyStore.cs`,
`Plugins/Android/KeyVault.java`, `Plugins/iOS/AronKeychain.mm`.

| Part | What it does |
|---|---|
| File format | `ARN` + format byte (2) + algorithm byte + nonce + tag + ciphertext. The 5 header bytes are authenticated, so they can't be swapped. |
| Cipher | AES-256-GCM where the runtime has it (tests, Cloud Code). On the Unity player the default is AES-256-CBC with an HMAC-SHA256 tag (encrypt-then-MAC), because Unity's runtime does not reliably ship `AesGcm`. Both are covered by the same tests, the algorithm id is in the header, so a later move to GCM (for instance a native plugin) reads old files. |
| Key | 256 bits derived with PBKDF2-SHA256 (20,000 rounds) from a random per-install secret + the device id + a masked salt compiled into the game. |
| Where the secret lives | Android: a non-exportable Android Keystore AES key wraps it (`KeyVault.java`). iOS: Keychain, `AfterFirstUnlockThisDeviceOnly`, not synced. Fallback (editor, or a native call fails): PlayerPrefs + device id + salt. The fallback is obfuscation, not strong protection. |
| Tamper detection | Any changed byte fails the tag. The game then loads the backup slot instead. Nothing is shown to the player except a normal load. |
| Atomic write | Sealed bytes go to `.tmp`, the current file is copied to `.bak` only if it still opens, then the temp file replaces the save. A crash at any point leaves either the old or the new file. A damaged main file never overwrites a good backup. |
| Backup slot | `aron_save.json.bak`; restored automatically and the game re-saves. If both are broken the game starts fresh instead of crashing. |
| Versioning | The envelope has a format number (an older game refuses a newer file instead of misreading it). `SaveData.version` migrations still run in `StoreInventory.Upgrade`. |
| Migration | An old plaintext save is read once, copied to `.plain.old`, and rewritten sealed. After the first sealed save a `.sealed` marker exists and a plaintext file is no longer accepted (it would be a downgrade). |
| Secrets in the client | None. The salt is not a secret, only a speed bump. No API keys or signing keys ship in the game (see section 3). |

Limits to know: someone with root access can read the secret, copy the whole save folder back to an older state, or edit memory.
The save file protects against casual edits and corruption. It does not stop a determined cheater, which is why section 2 puts
points, purchases and online results on the server.

## 2. Anti-cheat and economy integrity

Principle: the phone asks, the server decides. Offline play keeps working exactly as before (it is a single-player game first);
only points that count online, purchases and leaderboard results need the server.

Code: `Scripts/Logic/Integrity/` (all pure C#, shared with the Cloud Code module, tests in `IntegrityTests.cs`).

| Risk | Client layer (slows cheaters down) | Server layer (the real check) |
|---|---|---|
| Edited coins or points in memory | `GuardedInt` (masked, re-masked on every write, self-checking). `WalletWatch` audits the plain saved numbers at each save and raises a soft flag on a jump no honest play can make. | Server keeps the authoritative race points per player; `ServerEconomy.Reconcile` always takes the server number. |
| Edited save file | Sealed, authenticated save (section 1). | Same. |
| Clock changes (48 h rentals, daily caps, gifts) | `TrustedClock` / `GameClock`: server time + monotonic clock online; offline the device clock is accepted only while it moves in step with the monotonic clock (Android `elapsedRealtime`, iOS `CLOCK_MONOTONIC`, both keep counting in sleep). A jump is ignored and flagged. After a reboot the clock is "unverified": time never goes back, and nothing new is earned until a check passes. | Rentals and daily caps for online points use server time only (`ServerEconomy.Rent`, `GrantOnlineRace`). |
| Impossible race results | The client check only shows the result early. | `RaceResultValidator`: total time, per-lap minimum from the vehicle's speed ceiling (`SpeedCaps`), laps add up to the total, `ClaimValidator` (signature, duplicate match, field size, place). |
| Fake purchases | None needed. | `ReceiptPolicy` (known product, right package, purchased state, no repeat transaction) + store signature check (`GooglePlaySignature`, RSA-SHA1) or the App Store Server API. The game grants only after the server says `Valid` (`PurchaseGate`, hooked in `UnityIapBackend`). If the server is unreachable the order stays pending and is retried next launch, so a real payment is never lost. |
| Name and report spam | n/a | `RateLimiter.Default()`: name check 10 burst / 30 per hour, name change 3 / 3, report 5 / 10, race claim 12 / 30, purchase verify 10 / 20; repeated refusals lock the key out for 5 min doubling up to 24 h. |
| Modified or emulated devices | `IIntegrityProvider` hook (Play Integrity / App Attest), soft flags. | Server verifies the token with Google or Apple and decides `DeviceTrust`. Ranked points need `Verified`, or `Unknown` while no flags are set. |

Soft flags (`SoftFlags`): rooted device, debugger, clock tampered, wallet jump, memory edit, modified build. They are sent with online
requests so the server can keep a flagged phone out of ranked results. They never block offline play and never change the save.

### What needs Ahmed's accounts (not done, interfaces and server rules are ready)

1. **Unity Gaming Services project** with Authentication, Cloud Save and Cloud Code enabled (the Cloud Code module is described in `docs/cloud_code.md`).
2. **Google Play**: Play Console app, in-app products with the ids from `StoreCatalog.json`, the app's licence public key (for `GooglePlaySignature`), and a Google Cloud project linked for **Play Integrity**.
3. **Apple**: App Store Connect in-app purchases, an App Store Server API key (for transaction checks), and the **App Attest** capability on the App ID.
4. Product ids in `StoreCatalog.json` start with `com.ahmdria1.aron.` while the bundle id is `com.ahmedalamrani.aron`. Both are legal, but the ids created in the stores must match the catalog exactly.
5. The race-claim signing salt must be a server secret (Cloud Code secret). The client's own copy only proves a claim was not casually edited; the server checks the time and speed itself.

Not done in the game: native Play Integrity and App Attest calls (need the accounts above), and `IPurchaseVerifier` has no HTTP implementation until the module is deployed (until then `PurchaseGate.Verifier` is null and the stores' own signed result is trusted).

## 3. Build hardening

Details are in `docs/build_hardening.md`. In short: shipping builds use IL2CPP and ARM64, high managed stripping and R8 minification; development and profiler
flags are refused by `BuildHardening.Verify` in CI; `Log.Info` and `Log.Warn` are compiled out of release builds (errors stay);
`link.xml` and `proguard-user.txt` keep the code that must survive stripping. Device checks (root, debugger, modified build) only raise soft flags.
Certificate pinning is report-only until two pins are set, so a rotated certificate can never lock players out. Names inside IL2CPP metadata stay readable unless obfuscation is switched on, and it is off by default.

### Pin mode from the server

The pin mode may be moved between report-only and enforce from the server settings (see `docs/remote_config.md`). Enforce needs two or more pins, and "off" cannot be requested remotely.
