# Definition of done

A feature, fix or piece of content is done only when every line below is true. If one cannot be met, say which one and why in the change, so the decision is visible.

## 1. Works on low, mid and high devices
- Tried, or simulated with the device tier (`DeviceTiers`), on all three quality levels: Low, Medium and High.
- Nothing needs a feature that is turned off on Low (real-time shadows, MSAA, full resolution). A cheaper fallback exists and looks acceptable.
- Safe areas, notches and aspect ratios from 4:3 to 21:9 are respected; text at the largest text size still fits.

## 2. Covered by tests
- Rules and data are in the logic assembly and have tests in `Tests/Aron.Logic.Tests` (see `docs/adr/0001-pure-logic-split.md`).
- Shipped JSON files are loaded and validated by a test.
- `dotnet test Tests/Aron.Logic.Tests` passes with no failures and no skipped tests added.
- The lint step passes (`python3 tools/lint_check.py`).
- Anything that can only be seen in the editor or on a phone is listed as "not verified in Unity" in the matching document.

## 3. Documented
- The behaviour, the data fields and the reason for the choice are written in `docs/` (or in `README.md`). A decision that is hard to reverse gets a record in `docs/adr/`.
- New settings are described where the player finds them and in `docs/accessibility.md` if they affect access or comfort.

## 4. Never below 30 fps
- Holds at least 30 fps on the Low tier in the busiest scene for the feature: a race, a storm, a boss fight.
- No per-frame allocations in hot paths, and no work in `Update` that can be done on change.
- The frame-rate governor (`FrameRateGovernor`) is not used to hide a cost; it is the safety net, not the plan.

## 5. No thermal or battery regressions
- A ten-minute session on a mid device is no hotter and drains no more than the build before the change (`PerfStats` and the device's own battery and temperature readings).
- No extra post-processing, no HDR, no new always-on effects; background work stops when the app is paused.
- Menus stay at 30 fps and the screen stays awake only during play.

## 6. Localized in 12 languages
- Every player-facing string exists in all 12 languages: ar, en, tr, de, ur, hi, es, pt, id, ms, ja, fr.
- Arabic and Urdu read right to left and render correctly (`ArabicText`); no text is cut off in the longest language.
- Strings with a number or a name use format arguments, not joined pieces.
- A native-speaker review is marked in the data where one is still needed.

## 7. No tool traces
- No tool, service or product that helped make the work is named in code, data, comments, documents, file names, commit messages or metadata.
- Every new `.cs` file starts with `// (c) Ahmed Alamrani. All rights reserved.`
- Commits are authored as Ahmed Alamrani and have neutral messages.
- Art, audio and text are original or properly licensed, and the source of any third-party item is recorded in `ThirdParty/`.
