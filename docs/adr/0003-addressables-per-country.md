# 0003. Ship each country's content as its own Addressables group

Status: accepted. Content is still loaded from `Resources/` today; the move is planned.

## Decision
Per-country assets (stage art, costume textures, music, rival model, museum artifact) are grouped by country and delivered with Addressables. A small base build carries the menus, the first region and shared assets. Other countries download when the player first travels there.

## Why
- The game covers 35 countries; shipping all of them in the first install would be large and slow to download on mobile data.
- Players use only a few countries in a session, so storage and memory stay low, which helps low-end devices.
- A country can be updated or corrected (for example after a cultural review) without a full app release.

## Rejected alternatives
- Everything in `Resources/`: simplest, but every byte is in the install and in the startup index.
- One scene per country inside the app bundle: still a large install, and no independent updates.
- Platform asset packs only (Play Asset Delivery, on-demand resources): different on each platform and not usable for updates outside store review.

## Consequences
- Loading goes through one loader interface (`AudioManager.LoadClip` is the first place to change) so the switch touches few files.
- Code must handle a country that is not downloaded yet: a progress state, a retry, and a way to play offline with what is already stored.
- Group layout and remote catalog hosting need setting up before the first release that uses them.
