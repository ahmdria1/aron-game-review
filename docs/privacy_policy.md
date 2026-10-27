# Privacy policy: رحلة أرون (draft, needs lawyer)

Version 2026-10 (matches `ConsentFlow.NoticeVersion`). Controller: Ahmed Alamrani, [address and contact email to be added]. Arabic is the reference text, and translations are provided in all 12 game languages.

## 1. Playing without an account
You can play the whole game as a guest. Progress is saved on your device. We create a random player ID with our game backend (Unity Gaming Services) so cloud saves and races work. That ID is not your name, email or phone number.

## 2. What we collect
| Data | Why | Kept |
|---|---|---|
| Random player ID | Cloud save, races, leaderboards | Until you delete your account |
| Player name (a name you choose; children and anyone who has not told us their age group pick from a list of generated names) | Shown to other players in online races, and to tell players apart | Until you change it or delete your account |
| Age group: child, teen or adult (never your birth date or the year you were born) | To decide whether you type a name or pick one, and which purchase checks apply | Until you delete your account |
| Game progress, settings, passport stamps | So you don't lose your progress | Until deletion |
| Email address (only if you choose to link one) | **Only** to sign you in and recover your account. Never for marketing, never sold or shared | Until you unlink it or delete your account |
| One-time codes | Signing in. Stored only as a hash and gone after 10 minutes | 10 minutes |
| Purchase receipts (from Google Play or the App Store) | To give you what you bought | As tax law requires |
| Crash reports (only if a grown-up turns them on) | Fixing bugs. Emails and codes are masked before sending. Sent to Sentry, our crash-report provider, without IP addresses | 90 days |
| Usage statistics (only if a grown-up turns them on) | Seeing where players get stuck so we can fix it. Only the events listed in 2.1 | Up to 13 months, then deleted by Unity |

We do not use advertising, ad IDs, location, contacts, the microphone or the camera. We do not track you across other apps.

### 2.1 Usage statistics and crash reports
Both are **off** until a grown-up turns them on in Settings > Privacy, behind the grown-up check. Nothing is recorded, saved for later or sent before that, and turning them off again clears what was waiting on the phone. **Delete my statistics data** in the same place switches both off, deletes the waiting events and asks Unity Analytics to delete what it holds. If we change this notice, they stay off until a grown-up answers again.

What the statistics contain, and nothing more (the full list is in `docs/analytics.md`):
- first open of the game;
- a country opened for the first time (its id);
- stage started, finished or failed (stage id, and for finished or failed the seconds played);
- boss defeated (stage id);
- purchase completed (the store product id only: no price, no receipt, no order number);
- the number of crashes the game recorded.

They are saved on the phone and sent together in small batches: on Wi-Fi, or once per session on mobile data when Data saver is off, never while playing. We send no names, emails, photos, text, locations or advertising IDs.

We use **Unity Analytics** to receive them. Unity adds to each event the data its service needs to work: your **IP address**, an ID for this installation of the game, the anonymous player ID from section 1, and the iOS vendor ID (a number for all apps from us, which is not an advertising ID). Unity keeps this for 13 months by default. Crash reports go to **Sentry**, are masked, and are stored without IP addresses. [lawyer to confirm the wording on children and on transfers outside the country]

Photos you take in Photo Mode stay on your device. They go to your gallery only when you press Save (the app asks permission the first time) and are shared only when you press Share. Images carry a small game watermark and no personal data.

## 3. Online play
In online races other players see your player name, your outfit and your race time. There is no free chat.

**Player names.** Players aged 13 and over type their own name. Children (and anyone who has not told us their age group) pick from a list of generated names such as "Swift Falcon", so a child never types a name that other players can see. We ask everyone not to use their real full name, and the name screen says so. A name is 3 to 16 letters or numbers, is checked against word lists in all 12 game languages on your device and again on our server, and cannot copy the game's own names or staff words. If two players choose the same name, the second one gets a number after it (for example Ahmed#1234). You can change your name once every 24 hours.

**Reporting names.** In an online race you can report another player's name from a short list of reasons (offensive, pretending to be someone, a full real name, other). There is no free text. When three different players report the same name, the name is taken away and that player has to choose a new one before racing online again. The server keeps only who reported which name and the reason, to stop repeat reports and abuse, and deletes it when the name changes or the account is deleted.

Until the online service is switched on, your name stays on your device only.

## 4. Email sign-in
We send a 6-digit code or a sign-in link that works for 10 minutes. Our emails contain no tracking pixels and no marketing. Changing your email needs a code sent to the new address. In the app the address is shown masked (a***@e***.com).

## 5. Children
If you are younger than the age of digital consent where you live (for example 13 in the US, 13–16 in EU countries, 18 in India, Indonesia, South Africa and Saudi Arabia, according to our settings), you can still play as a guest with full progress. To link an email:
1. A grown-up completes a check in the app.
2. The grown-up enters their email, or confirms that the child's email is supervised.
3. We email the grown-up, in their language, with Approve and Decline buttons.
4. Only after approval do we send a code to link the child's email.

We keep a consent record: the date, the notice version and a salted hash of the parent's email (never the address itself). The parent can withdraw consent at any time from the link in the email or from Settings > Family. Withdrawing unlinks the child's email, and progress stays on the device. We do not profile children, show them ads, or sell their data.

## 6. Where data is processed
Unity Gaming Services, and our email provider [name to be added], may process data outside your country, including outside Saudi Arabia and the EU, under contracts with safeguards [lawyer to confirm the mechanism].

## 7. Your rights
You can see, export, correct or delete your data. **Delete account** is in Settings and on [deletion web page URL]. Deleting removes your local and cloud save, your player name and the reports about it, race ghosts, leaderboard entries, email link, consent record, code state, passport photos and finally the player ID. You may complain to your data protection authority (for example SDAIA in Saudi Arabia, or your EU supervisory authority).

## 8. Changes
We will show changes in the app before they apply.
