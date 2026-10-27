# Android Java names and R8

**Decision: no extra rule is needed. R8 already hides the Java source file names, and that is checked on the built APK.**

## What was lost, and why it does not matter

The old release rule used `-renamesourcefileattribute` to replace every Java file name in crash traces with a neutral word. Gradle (Android Gradle Plugin 9, R8 9.1.31) rejects that option in a consumer rules file such as `proguard-user.txt`, because it is a global option that only the app's own build file may set. A custom `mainTemplate.gradle` could set it, but it is not necessary:

* From R8 8.x on, with mapping file format 2.2, R8 writes the original file name into the **mapping file** and puts only an `r8-map-id-<hash>` string into the app in its place.
* `proguard-user.txt` keeps `SourceFile,LineNumberTable`, so crash lines stay readable once the mapping file is applied (the file name comes back from the mapping).

## Evidence (release build `aron_2026-10-27_full.apk`)

| Check | Result |
|---|---|
| Mapping header | `compiler: R8`, `compiler_version: 9.1.31`, `mapping 2.2` |
| `KeyVault` (kept by rule) in the mapping | `com.ahmedalamrani.aron.KeyVault -> com.ahmedalamrani.aron.KeyVault` with `"sourceFile","fileName":"KeyVault.java"` (the name lives only here) |
| Strings in the APK's `classes.dex` ending in `.java` or `.kt` | **0** |
| `r8-map-id` string in the APK | present, equal to the mapping's `pg_map_id` (`ed6c9ead…41ae1b`) |

So a person who unpacks the APK sees no Java file names, only the map id.

## What to do for each release

* Keep the `mapping.txt` of every released build (it is the only way to read a crash trace from the store). Upload it to the Play Console with the bundle, and archive it next to the build.
* Do not add `-renamesourcefileattribute` to `proguard-user.txt` again. It would break the Gradle build.
* If a future AGP stops replacing the names, re-run the check above: `strings classes.dex | grep -c '\.java$'` must print 0.
