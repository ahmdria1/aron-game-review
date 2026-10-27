# Player names

How a player gets a name, what is allowed, and where each rule lives. Everything the rules decide is pure C# in
`Assets/_Aron/Scripts/Logic/Names`, so the game and the server run the same code and `dotnet test` covers it.

## The flow

1. First time the main menu opens (and again if a report forces a rename) the name screen opens. There is no separate language picker yet:
   the game follows the phone language or the saved language.
2. If the age group is unknown, one step asks for the **year of birth** with a stepper. Only the result (child, teen or adult) is saved, never the year.
3. **Teens and adults** type a name. **Children and unknown ages** pick from five generated names (an adjective and an animal, in Arabic or English) with a button for five more.
4. The screen always shows the hint «لا تكتب اسمك الحقيقي الكامل» (in all 12 languages). No real name is ever required.
5. The name can be changed later in Settings. A change is allowed once every 24 hours.

Arabic in a typing field: Unity's text field does not join Arabic letters, so the field is set to right-to-left and a correctly shaped preview line is shown under it. Unverified on a device.

## Rules (`PlayerNameRules`)

- 3 to 16 characters. Arabic and Latin letters, digits, and at most one separator (a space or an underscore), never at the start or end.
- At least 2 letters, at most 4 digits.
- No emoji, zero-width or bidirectional control characters, no other scripts, no mixing Arabic and Latin letters in one name (look-alike tricks).
- Arabic tatweel is removed. For comparing names the game folds alef forms, taa marbuta and alef maqsura, strips diacritics, maps Arabic-Indic digits to 0-9.

## Word lists (`Resources/Names/blocklist_<lang>.json`)

One file per language (ar, en, tr, de, es, pt, id, ms, fr, ur, hi, ja), each with five categories: profanity, insults, hate (racist, ethnic, tribal, sectarian), sexual, figures (political and religious figures).
Entries are of three kinds, to avoid blocking innocent names:

- `terms`: blocked anywhere inside the name (only for long, unambiguous words)
- `words`: blocked only as a whole word or the whole name
- `exact`: blocked only when the whole name is the word (for example holy names, so "عبد الله" stays allowed)

Names are compared after folding: lowercase, accents removed, look-alike digits and symbols read as letters (0 as o, 3 as e, 4 as a, 5 as s, 7 as t, 1 and ! as i, rn as m), spaces and separators removed, repeated letters collapsed. So "sh1t", "s h i t" and "shiiit" are all caught.

Trade-offs, decided on purpose:

- Names of religious and political figures are blocked, as asked. Common given names that happen to be figures' names (محمد, أسامة, Aaron) are **not** blocked.
- The lists are a **starter set**. They need a review by a native speaker for each language before launch, and will need updating over time.
- Only Arabic and Latin scripts can be typed. Hindi, Urdu (Arabic script is fine) and Japanese speakers can use Latin letters or pick a generated name.

## Reserved names

The game's own characters and staff words cannot be used: Aron / أرون, Noura / نورة, Dr. Duhaim / دهيم, the companion names (البرق, غزيّل, سهم, أصيل, بيضا) and words like admin, moderator, official, support, الدعم, المشرف. Matching ignores case, accents and trailing digits (so "admin2" is also reserved). Data: `Resources/Names/reserved.json`.

## Uniqueness (`IPlayerNameService`, `NameRegistry`)

- `CheckAsync`, `ReserveAsync`, `ChangeAsync` and `ReportAsync` are the only calls the game makes. The server (Cloud Code, see `cloud/AronNames/README.md`) is authoritative and repeats the rules and the word lists.
- Two names count as the same when their comparison keys match (so "Ahmed", "ahmed" and "Ahmed1" are one family). The second player gets a number: `أحمد#1234` (1000-9999, with unlucky and offensive numbers skipped).
- Limits: 30 checks per minute and 6 reservations per minute per player, 10 reports per day.
- Offline or no server: the name is kept on the device and marked not confirmed. Single-player works fully. Online races need a confirmed name.
- **The server module is not deployed and has not been run against Unity Gaming Services.** That needs Ahmed's UGS setup.

## Reporting (`ReportNameDialog`)

Another player's name can be reported with a reason button (offensive, pretending to be someone, a full real name, other). No free text. When three different players report a name, the name is replaced by "Racer" plus a number and that player must pick a new one before racing online. The in-race screen that would open the dialog does not exist yet; the dialog and the server side do.

## Children and privacy

- Rated 7+, no chat, no free text. Children never type a name that other players see.
- No real name is asked for, and the screen asks players not to use one.
- The age group (child, teen, adult) is saved, not the birth year.
- `docs/privacy_policy.md` and `docs/iarc_draft.md` describe this. Showing typed names to other players is a questionnaire answer that must be decided before submitting to the stores, and may matter for the Kids categories.

## Tests

`Tests/Aron.Logic.Tests/PlayerNameTests.cs`: shape rules, hidden and bidi characters, scripts, Arabic folding, every language's list, spelling tricks, innocent names that must stay allowed, reserved names, uniqueness and numbers, cooldown, rate limits, reports, the offline client, child modes, the Cloud Save store with simulated write-lock conflicts, the UI strings in 12 languages, and this documentation.
