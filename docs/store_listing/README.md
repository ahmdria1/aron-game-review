# Store listings (Google Play and App Store)

Texts for all 12 game languages: `ar en tr de ur hi es pt id ms ja fr`. `listings.json` is the source; each `<lang>.md` is the same text laid out for copy and paste.
`Tests/Aron.Logic.Tests/StoreListingTests.cs` checks every limit below and that the `.md` files match the JSON.

## Limits and what is used

| Field | Store | Limit | Notes |
|---|---|---|---|
| Title / name | Play, App Store | 30 | Brand first, then what the game is. |
| Short description | Play | 80 | Genre, the place, and the two hooks (bosses, mounts). |
| Full description | Play, App Store | 4000 | About 1000 characters: intro, 8 short feature lines, languages. Main words repeat naturally 3 to 5 times. |
| Subtitle | App Store | 30 | A second line of keywords that reads as a phrase. |
| Keywords | App Store | 100 | Comma-separated, no spaces after commas, none of the words already in the name or subtitle. |
| Promotional text | App Store | 170 | Can change without a new version. |
| What's new | both | 500 | First release text. |
| Screenshot captions | both | 8 per language | Order below. Short enough for one or two lines on the picture. |

**Category.** Play: Game > Adventure. App Store: primary Games > Adventure, secondary Games > Racing.
**Play tags** (choose from Play's own list): Platformer, Adventure, Racing, Single player, Offline.
**Rating target: 7+.** The cartoon swing and handbag throw are mild fantasy violence; there is no blood, chat, gambling or ads (`docs/iarc_draft.md`). The questionnaires decide the final rating, so do not print an age in the listing.

## Keyword research (summary)

Searched on 27 Oct 2026. Sources are third-party guides and public charts, so treat them as direction, not exact volumes. Real volumes come from Play Console's search terms and App Store Connect's search data after launch.

- Saudi users search in both Arabic and English, so the Arabic and English sets both carry the other's main words (RespectASO, "App Store Optimization for Saudi Arabia"; Salman Gul, "ASO for MENA").
- Arabic genre phrases that match this game: لعبة مغامرة، لعبة ركض، لعبة سباق، لعبة منصات، لعبة مغامرات للأطفال، سباق هجن، سباق جمال. Use root variants and forms with and without ال, and write Gulf wording in the body text, not in keyword fields.
- Saudi racing charts are led by car, drift and motorcycle games (AppInsider, Saudi racing chart, 3 Oct 2026). A platform adventure with camel and falcon races is a different niche, so the title leads with the adventure and the mounts, and racing is the second hook.
- Google Play ranks mainly by title, short description and the first half of the full description; keep the main phrases there (Admiral Media, "Google Play ASO 2026"; it also reports that from July 2026 listings are judged by click-through, which favours a clear first line and good first screenshots).
- English: platformer, adventure, saudi, riyadh, camel race, falcon, arabic game, 3d runner, family, kids, racing, jump.

**Rules kept in every language:** no brand or title of another game or a company, no "free" or price words (prices differ per country and the store shows them), no claims that cannot be checked in the game, no mention of how the game was made, no store names inside the text, and no emoji.

## Screenshot order and captions

Eight pictures, landscape, the same order in every language. The caption text for each language is in its `.md` file and in `screenshot_captions`.

1. Running and jumping in Riyadh (hero, a date trail, a lantern).
2. A boss fight with the hit effect.
3. A camel, falcon or ostrich race.
4. The vehicle tabs: boat, car, bicycle, motorcycle.
5. The world map with regions unlocked.
6. The passport and museum.
7. The online race lobby (nicknames only).
8. The one-hand mode and settings.

Take the pictures from a release build on a real phone. Play wants at least 2 and up to 8 phone screenshots, a 512 px icon and a 1024x500 feature graphic. The App Store wants the sizes for the largest iPhone and iPad it supports.

## Store locale codes

| Game | Play | App Store |
|---|---|---|
| ar | ar | ar-SA |
| en | en-US | en-US |
| tr | tr-TR | tr |
| de | de-DE | de-DE |
| ur | ur | ur-PK |
| hi | hi-IN | hi |
| es | es-419 | es-ES (add es-MX with the same text if needed) |
| pt | pt-BR | pt-BR |
| id | id | id |
| ms | ms | ms |
| ja | ja-JP | ja |
| fr | fr-FR | fr-FR |

App Store Connect has supported Urdu metadata since 31 March 2026 (Apple Developer news), so `ur-PK` can be filled in.

## Before sending to the stores

- Have a native speaker read each language once. The Arabic is written in a light Gulf tone; the other eleven were written directly in their language and are not machine-checked for local idiom.
- Check each claim against the build you submit (stage length, up to 4 online players, no ads, offline play).
- The data-safety form and privacy labels must match `docs/privacy_policy.md`.
- Needs Ahmed's accounts: Play Console and App Store Connect entries, screenshots, icon and feature graphic.
