# Build hardening

Applied by `Aron > Apply Phone Settings` and again before every CI build (`Editor/BuildHardening.cs`). CI stops if `BuildHardening.Verify()` finds any drift.

| Area | Setting | Where |
|---|---|---|
| Scripting | IL2CPP, release compiler configuration | `BuildHardening.Apply` |
| CPU | Android ARM64 only (Google Play needs 64-bit; ARMv7 dropped) | `MobileSetup`, `CI` |
| Managed code | Stripping level High; `Assets/_Aron/link.xml` keeps `Aron.Logic` (JsonUtility and reflection), Aron.Store.Iap and the crypto providers | `BuildHardening`, `link.xml` |
| Debug flags | No development build, no script debugging, no profiler connection, no wait-for-debugger; the options are masked off again at build time | `BuildHardening.ReleaseOptions` |
| Logs | Game code logs through `Aron.Logic.Diagnostics.Log`. `Info` and `Warn` are removed by the compiler (call and arguments) unless the build is the editor, a development build, or defines `ARON_LOGS`. Errors stay and feed the crash reporter. `Debug.unityLogger` is also limited to errors in release. A test fails if a raw `Debug.Log(` appears in runtime code. | `Log.cs`, `LogBridge.cs`, `HardeningTests` |
| Stack text | None for Log and Warning, script-only for Error and Exception | `BuildHardening.Apply` |
| Symbols | No PDB, map or native symbol files in the shipped app (a test checks the repo). For crash reports CI can build a separate symbols zip with `ARON_SYMBOLS=1`; it is uploaded to the crash reporter and never shipped. | `BuildHardening`, `HardeningTests` |
| Android Java | R8 minify on for release. `Assets/Plugins/Android/proguard-user.txt` keeps the JNI classes (`KeyVault`) and readable line numbers. **One manual click:** Player Settings > Android > Publishing Settings > tick *Custom Proguard File* (there is no script API for that box; without it R8 may strip `KeyVault`). | `BuildHardening`, `proguard-user.txt` |
| Signing | Upload key from CI secrets (`ANDROID_KEYSTORE_*`); the debug key is never accepted for Google Play | `CI.Sign` |

## C# name obfuscation (optional, off by default)

IL2CPP removes the IL but keeps type and member names in `global-metadata.dat`, so a dumper can read them. Free tooling exists:
**Obfuz** (MIT, open source, Unity-integrated, keeps JsonUtility/MonoBehaviour names alone) or **Obfuscar** (MIT) through an `IPostBuildPlayerScriptDLLs` hook.
It is *not* switched on because it cannot be validated without opening Unity here, and a wrong rule breaks saves or scene components.

To switch on (about 30 minutes, test on a real build): add the Obfuz package, tick `Aron.Logic` and `Aron.Runtime` in its assembly list,
keep the default serialization rules (names of `[Serializable]` types and their fields stay), build a *release* APK and play every stage, the store, and a save/load round trip.
Do not test obfuscated builds inside the editor. If anything goes wrong, turn it off; the other layers do not depend on it.

## Root, jailbreak and debugger signals (soft)

`DeviceProbe` reads hints once per launch (su paths, test-keys build, root manager apps, Cydia and friends, sandbox escape, debugger, debuggable build,
installer, repackaged signature) and `DeviceChecks.Evaluate` turns them into `SoftFlags`. One weak hint is ignored; side-loading and emulators are not flags alone.
Flags never block offline play and never alter the save. They travel with online requests; the server may keep a flagged phone out of ranked results.
Detection can be bypassed by someone who controls the phone, so it is a signal for the server, not a lock.

## Certificate pinning (design)

- Pin only **our own backend host**. Unity Gaming Services hosts are Unity's and rotate certificates, so they use the system trust store.
- Pin = base64 SHA-256 of the server certificate's public key bytes: `python3 tools/pin_hash.py api.example.com:443` (or a PEM file).
- Ship two or more pins (current and next key). `CertPins.SafeToEnforce` refuses a single pin.
- Start in `ReportOnly`; switch to `Enforce` from Remote Config once the mismatch count stays at zero. A bad pin can then be turned off without an app update.
- `PinnedCertificateHandler` plugs into `UnityWebRequest.certificateHandler`.
- There is no own backend host yet, so nothing is pinned today.

## Checklist before a store build

1. `Aron > Security > Check Release Settings` prints "hardened".
2. "Custom Proguard File" ticked.
3. Upload keystore secrets set in CI.
4. Install the release APK on a phone; check `adb logcat` shows no game text (only errors).
5. Play one stage offline, one race, restore a purchase (test account), kill the app mid-save and reopen.
