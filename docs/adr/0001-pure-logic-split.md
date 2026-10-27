# 0001. Keep game rules in an engine-free logic assembly

Status: accepted

## Decision
Rules, data models and calculations live in `Assets/_Aron/Scripts/Logic` (assembly `Aron.Logic`, plain C#, no `UnityEngine` references). Scripts that touch the engine (scenes, input, rendering, audio, UI) live outside it and only call into the logic layer.

## Why
- The logic compiles and runs outside Unity: `dotnet test Tests/Aron.Logic.Tests` takes seconds and runs on any build machine, without an editor licence or a device.
- The same classes can run on the server (for example the Cloud Code modules in `cloud/` share the account, name and race-claim rules), so client and server cannot disagree.
- Rules such as flash limits, race points and name checks can be tested exhaustively instead of by playing the game.

## Rejected alternatives
- Rules inside MonoBehaviours, tested in Play Mode tests: slow, needs the editor, and cannot be shared with the server.
- A separate repository for the logic: harder to change a rule and its caller together.

## Consequences
- Logic code may not use `Mathf`, `Vector3`, `Debug` or `Resources`; it uses `System` types and small structs of its own.
- Engine scripts stay thin: read the state, ask the logic, apply the answer.
- Every new rule needs a test in `Tests/Aron.Logic.Tests` before it is considered finished.
