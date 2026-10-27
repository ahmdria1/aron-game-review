# 0002. Describe content in JSON data files

Status: accepted

## Decision
Content and tuning (regions, stages, costumes, camel breeds and prices, rivals, weather, strings, store items, feel numbers) is stored as JSON under `Assets/_Aron/Resources/`, read through typed classes in the logic assembly. Adding a region means adding a file, not changing code.

## Why
- Designers and translators can change numbers and text without touching C#.
- Data can be validated by tests (every language present, prices in range, no forbidden landmark ids) and checked on every build.
- Values can later be overridden from remote config without a new release.

## Rejected alternatives
- ScriptableObjects: tied to the editor, cannot be loaded or tested by plain .NET tests, awkward to review in a diff.
- Constants in code: every tweak needs a rebuild and a review of code.
- A database or spreadsheet at runtime: extra moving parts for a game that ships its content.

## Consequences
- Each data file has a class with a `Validate()` method and a test that loads the shipped file.
- A bad edit fails a test instead of crashing a device.
- JSON has no comments, so intent goes in the matching document under `docs/`.
