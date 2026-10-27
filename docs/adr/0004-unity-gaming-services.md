# 0004. Use Unity Gaming Services instead of a custom server for now

Status: accepted, to be revisited if the game outgrows it

## Decision
Online features (anonymous player ID, cloud save, lobbies and matches, remote config, server-side checks) use Unity Gaming Services: Authentication, Cloud Save, Lobby, Remote Config and Cloud Code. Server rules are written as C# modules that reference the same logic assembly as the client (`cloud/`). There is no server of our own to run.

## Why
- A small team: nothing to host, patch, scale or be on call for.
- The services cover the needs: identity, storage, matchmaking and server-side code.
- Cloud Code modules can share the tested C# rules, so the server and client cannot drift.
- Secrets (mail provider key, peppers) stay in the service's secret store, never in the client or the repository.

## Rejected alternatives
- A custom backend (own API, database, hosting): full control, but cost in time and operations that is not justified before there are players.
- Firebase: good service, but it adds a second vendor and SDK to a project that already uses Unity's.
- A multiplayer server per match: not needed, since races are ghost-first (see 0005).

## Consequences
- Cloud Code limits and pricing shape the design: calls must be few and small.
- Data may be processed outside the player's country; the privacy policy and the compliance review say so and need a legal check per region.
- If we leave later, the logic assembly keeps the rules portable; only the thin service wrappers need replacing.
