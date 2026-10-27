# 0006. Passwordless accounts

Status: accepted

## Decision
Players can play as a guest with a random ID. To keep progress across devices they link an account by a typed one-time code sent to their email, or by Google or Apple sign-in. There are no passwords. The client sends only an address and the code; the server makes, hashes, checks and mails codes, and keeps the mail provider key in its secrets.

## Why
- Nothing to remember, nothing to leak, and no password reset flow.
- Children and casual players are more likely to finish a short code than to invent a password.
- Guest first means no sign-up wall before the first race.
- The server stores only a salted hash of the address, and logs show it masked.

## Rejected alternatives
- Email and password: weak passwords, reuse, and a breach risk we would own.
- Phone number codes: cost per message, and a more sensitive identifier.
- Social sign-in only: excludes players without those accounts and adds provider dependence.
- Clickable magic links only: break when the mail opens in another app or device; a typed code is more reliable on mobile.

## Consequences
- Codes need expiry, attempt limits and rate limits, and are tested in the shared logic.
- Email delivery becomes a dependency and needs a provider with a signed data agreement.
- Account deletion must remove the link, consent record and code state as well as the save.
