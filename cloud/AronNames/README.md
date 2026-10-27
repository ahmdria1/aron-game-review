# AronNames: the player-name service (Unity Cloud Code module)

**Status: written, not built, not deployed, not verified.** It cannot be deployed until Unity Gaming Services (UGS) is set up for the project
(a UGS project, Authentication with anonymous sign-in, Cloud Code and Cloud Save enabled). Until then the game keeps names on the device only
(`nameReserved = false`) and online races stay off.

## What is already tested

All the decisions are in `Assets/_Aron/Scripts/Logic/Names` and run in `dotnet test Tests/Aron.Logic.Tests`:

- name shape rules, Arabic normalisation, the 12 word lists, reserved names (`PlayerNameRules`, `NameChecker`)
- uniqueness, the `Name#1234` fallback, the 24 hour change cooldown, rate limits and report handling (`NameRegistry`)
- the Cloud Save storage logic, including a simulated write-lock conflict (`CloudNameStore` over `InMemoryVersionedKv`)

`AronNamesModule.cs` only connects those to Cloud Code and Cloud Save. That connection is the unverified part: the Cloud Save call shapes
(`GetCustomItemsAsync`, `SetCustomItemAsync`, write locks, the 409 conflict) were taken from the UGS documentation and never compiled.

## Deploy steps (when Unity Gaming Services is ready)

1. Link the project to UGS in the Unity editor (Project Settings > Services).
2. `dotnet add cloud/AronNames package Unity.Services.CloudCode.Core` and `... Unity.Services.CloudCode.Apis` (and `Microsoft.Extensions.Logging.Abstractions`);
   replace the `Version="*"` placeholders in `AronNames.csproj` with the versions you get. Fix any compile errors in `AronNamesModule.cs`
   (most likely the Cloud Save model types) and confirm `dotnet build cloud/AronNames` passes.
3. Package and deploy the module with the UGS CLI or the Cloud Code deployment window (the module needs the `Names/*.json` files next to the assembly, the csproj copies them).
4. Run `InitNameShards` once. It creates the 256 shard items. This matters: a Cloud Save write without a lock overwrites silently, so a shard must exist before the first claim, otherwise two first claims could both win. `CloudNameStore` refuses to claim until the shards exist.
5. Check by hand with two test accounts: claim the same name (the second must get a number or `taken`), change a name twice within 24 hours (`cooldown`), send 31 checks in a minute (`rate_limited`), report one name from three accounts (`MustRename`).
6. In the game: set `PlayerNames.UseRemote` to the Cloud Code client (the client class is the small adapter you add next to the UGS SDK; the game already calls only `IPlayerNameService`).

## Safety notes

- The caller is always `ctx.PlayerId`. The client never sends its own player id except as the *target* of a report.
- The client checks first for fast feedback, the server checks again with the same code. A modified client cannot skip the word lists.
- Nothing secret is in the client or in this repo. Cloud Code uses its own service token.
- Stored per player: the chosen name, its number, the time it was changed, and a flag. Per reported name: the ids of the players who reported it. Both are removed on account deletion (see `docs/privacy_policy.md`).
- The word lists are a starter set and need a native-speaker review per language before launch.

## Known limits

- First-time creation of a report list or a counter item has no lock, so in the rare case of two simultaneous first writes one report is lost. Acceptable for reports, never for name claims.
- Shards are plain text items; at a few hundred thousand names each shard is about a few hundred KB. Revisit if the game grows far beyond that.
