# Compliance review: رحلة أرون (internal)

Internal working document, updated 27 Oct 2026. It is **not legal advice**. Anything marked *needs lawyer* has to be checked by a qualified lawyer before release. Consent ages, the privacy policy and the terms are drafts.

Status key: **OK** means it already meets the requirement. **fixed** means it was changed in this branch. **needs Ahmed action** means a console, account or questionnaire step only the owner can take. **needs lawyer** means legal judgement is needed.

## 1. Store policies

| Area | Requirement | Source | Status |
|---|---|---|---|
| Google Play: Families | The game appeals to children, so it may be in scope as a mixed audience. That requires a neutral age screen, no behavioural ads, self-certified SDKs only, and a grown-up gate on links that leave the app and on purchases outside Play. | https://support.google.com/googleplay/android-developer/answer/9893335 | **fixed**: `ParentalGateDialog` now guards every `Application.OpenURL` (web shops). The game has no ads and no ad SDK. **needs Ahmed action**: pick the target age groups in Play Console. Choosing "13+ only" avoids the Families programme, but the cartoon art may still be judged as appealing to kids. |
| Google Play: Data safety | Declare collection of email (account), purchase history, gameplay and crash data (both optional, see `docs/analytics.md` for the exact answers incl. Unity Analytics and Sentry), and say they are encrypted in transit and deletable. | https://support.google.com/googleplay/android-developer/answer/10787469 | **needs Ahmed action**: fill the form from `docs/privacy_policy.md` §3. |
| Google Play: account deletion | Deletion inside the app plus a web URL that works without reinstalling. | https://support.google.com/googleplay/android-developer/answer/13327111 | **fixed** (logic): `AccountData.DeleteSteps` now also covers the email link, consent record, code state and passport photos. A test checks this. **needs Ahmed action**: host the deletion web page and enter its URL. |
| Google Play: Payments | Digital goods (coins, costumes, travel ticket, founder pack) go through Play Billing. Physical goods (grocery and developer web shops) may use other payment. | https://support.google.com/googleplay/android-developer/answer/9858738 | **OK**: Unity IAP 5.4.4 is used for all digital items. The web shops sell physical goods and services only. *needs lawyer*: whether the developer shop's "apps" and "services" count as digital goods. |
| Google Play: target API | New apps and updates must target the current required level. | https://developer.android.com/google/play/requirements/target-sdk | **OK**: target 36, min 26 (ProjectSettings). |
| Apple 4.8: Login services | Sign in with Apple, or an equivalent, is required only when a third-party or social login is offered. | https://developer.apple.com/app-store/review/guidelines/#login-services | **OK for now**: the app offers only anonymous play and its own email code. **needs Ahmed action**: if Google, Facebook or similar sign-in is added later, add Sign in with Apple. |
| Apple 5.1.1(v): account deletion | Account creation means deletion must be offered in the app. | https://developer.apple.com/support/offering-account-deletion-in-your-app/ | **fixed** (logic, see above). The UI button lives in the settings screen. |
| Apple 3.1.1: IAP | Digital content must use IAP. Loot-box odds must be disclosed. | https://developer.apple.com/app-store/review/guidelines/#in-app-purchase | **OK**: no paid random items (see §3). |
| Apple 1.3: Kids category | Not selected. If selected, it would bring stricter rules (no third-party analytics, gate on links and purchases). | https://developer.apple.com/app-store/review/guidelines/#kids-category | **needs Ahmed action**: leave Kids unselected in App Store Connect. A grown-up gate exists anyway. |
| Apple privacy labels | Nutrition labels must match the data collected. | https://developer.apple.com/app-store/app-privacy-details/ | **needs Ahmed action**: email (account, linked, not for tracking), purchases, gameplay, crash data. |
| Apple ATT | Needed only when tracking across apps or sites. | https://developer.apple.com/documentation/apptrackingtransparency | **OK**: no tracking and no IDFA, so no prompt. |
| Apple photo library | Saving needs `NSPhotoLibraryAddUsageDescription` (add-only). | https://developer.apple.com/documentation/bundleresources/information_property_list/nsphotolibraryaddusagedescription | **needs Ahmed action**: check the Info.plist string in the iOS build. The logic asks only at the first save. |

## 2. Privacy laws

Consent ages are data (`Resources/Config/consent_ages.json`). Below that age the email is linked only after a parent approves, and the child plays as a guest until then.

| Law | Requirement | Source | Status |
|---|---|---|---|
| Saudi PDPL | Lawful basis, a privacy notice in Arabic, guardian consent for minors who lack capacity, cross-border transfer rules, possible registration with SDAIA. | https://sdaia.gov.sa/en/SDAIA/about/Pages/RegulationsAndPolicies.aspx | **needs lawyer**: SA is set to 18 as a cautious choice. Transfer to UGS or Firebase servers outside KSA needs a legal basis. |
| GDPR Art. 8 and GDPR-K | Parental consent below the national age (13–16), data minimisation, rights of access and deletion, DPA with processors. | https://eur-lex.europa.eu/eli/reg/2016/679/oj | **fixed**: per-country ages in the config. **needs lawyer**: confirm each member state's age. **needs Ahmed action**: sign DPAs with Unity, Google and the mail provider. |
| UK GDPR and the Children's Code (AADC) | High privacy by default, no nudges, geolocation off, profiling off, child-friendly notices. | https://ico.org.uk/for-organisations/uk-gdpr-guidance-and-resources/childrens-information/childrens-code-guidance-and-resources/ | **OK** by design: no location, no profiling, no ads, gift box with no pressure. **needs lawyer**: a DPIA. |
| COPPA (US) | Verifiable parental consent before collecting personal info from under-13s, and direct notice to parents. The amended rule applies from 2025–26. | https://www.ecfr.gov/current/title-16/chapter-I/subchapter-C/part-312 | **fixed**: gate, parent email and Approve/Decline before any OTP to the child, revocable. **needs lawyer**: whether "email plus" is enough for the use (internal only, no disclosure). |
| CCPA/CPRA | Notice at collection, no sale or sharing, opt-in for minors under 16 before sale. | https://cppa.ca.gov/regulations/ | **OK**: no sale or sharing. The notice is in the privacy policy. **needs lawyer**: thresholds may not apply to a small developer. |
| Brazil LGPD Art. 14 | Children's data only with specific consent of a parent, in the child's best interest. | https://www.planalto.gov.br/ccivil_03/_ato2015-2018/2018/lei/l13709.htm | **fixed**: BR is 13 in the config, parent flow below that. **needs lawyer**: ANPD guidance on 12–18. |
| India DPDP Act 2023 and Rules 2025 | Verifiable parental consent for everyone under 18; no tracking or behavioural monitoring of children. | https://www.meity.gov.in/data-protection-framework | **fixed**: IN is 18. **needs lawyer**: the method of verifying the parent's identity. |
| Indonesia PDP Law 27/2022 | Parental consent for children (under 18). | https://peraturan.bpk.go.id/Details/229798/uu-no-27-tahun-2022 | **fixed**: ID is 18. **needs lawyer**. |
| Malaysia PDPA 2010 (2024 amendments) | Consent and notice in Malay and English; parental consent for minors. | https://www.pdp.gov.my/ | **needs lawyer**: MY is set to 13 for now. Ms text exists. |
| Turkey KVKK | Explicit consent and a disclosure notice; the parent acts for minors. | https://www.kvkk.gov.tr/ | **needs lawyer**: TR is set to 18. VERBİS registration may be needed. |
| South Africa POPIA | A child (under 18) needs a competent person's consent. | https://inforegulator.org.za/ | **fixed**: ZA is 18. **needs lawyer**. |
| Japan APPI | Consent from a legal representative for children too young to judge (PPC guidance: about 12–15). Third-country transfer notice. | https://www.ppc.go.jp/en/legal/ | **needs lawyer**: JP is set to 15. |
| Logs and crash reports | No email or code in plain text. | internal | **fixed**: `EmailAddress.ScrubLog` and `ErrorReportScrub` mask addresses. Tests cover this. |
| Fixed 13 in `AgeGate.ChildUnder` | The older age band check (name typing and online play) uses a fixed 13. | internal | **needs Ahmed action / lawyer**: decide whether online play should also follow `consent_ages.json`. Name typing stays off for children (they pick generated names, see docs/player_names.md). |

## 3. Monetization

| Area | Requirement | Source | Status |
|---|---|---|---|
| Paid randomness (Belgium, Netherlands) | No paid loot boxes. Odds disclosure where any random item is sold. | https://www.gamingcommission.be/ ; https://kansspelautoriteit.nl/ | **fixed and tested**: `MonetizationTests` checks that nothing for sale is random, that every pack lists fixed contents, and that the gift box (free, every 6 h) cannot be bought or skipped. "صندوق عملات" was renamed "كيس عملات" so it doesn't read as a box. |
| Remove-ads item | Selling "remove ads" in a game with no ads is misleading. | EU UCPD 2005/29/EC https://eur-lex.europa.eu/eli/dir/2005/29/oj | **fixed**: removed from the catalog and from the founder pack. A test checks this. **needs Ahmed action**: don't create `com.ahmdria1.aron.remove_ads` in the consoles. If it already exists, deactivate it. |
| Local prices | Show the store's localized price string. | Play Billing / StoreKit docs | **OK**: `PurchaseBackend.PriceOf` uses the store price. The editor test backend labels its prices "(تجربة)". |
| Purchase confirmation and refunds | The store's own sheet confirms. Refunds are per store policy. | https://support.google.com/googleplay/answer/2479637 ; https://support.apple.com/118223 | **OK**. The terms point to each store's refund process. |
| Dark patterns and countdowns | No fake timers, no pressure, no "only X left", especially for kids. | https://www.ftc.gov/reports/bringing-dark-patterns-light ; ICO AADC std 13 | **OK**: the only countdown is the free gift timer and the travel ticket's 24 h window, which is shown before buying. No sale timers. |
| Outside links | Grown-up gate. | Play Families | **fixed** (see §1). |

## 4. Age ratings

| Area | Requirement | Source | Status |
|---|---|---|---|
| IARC | The questionnaire is done in each console. | https://www.globalratings.com/ | **needs Ahmed action**: draft answers in `docs/iarc_draft.md`. |
| Expected PEGI / ESRB | PEGI 7 (mild fantasy and cartoon violence) or PEGI 3. ESRB E (Everyone), possibly "Mild Cartoon Violence"; In-Game Purchases. | https://pegi.info/ ; https://www.esrb.org/ | Expected, not guaranteed. |
| Saudi GCAM | Age classification for games sold in KSA. Saudi content norms (modest dress, no gambling, respect for religion). | https://gcam.gov.sa/ | **needs Ahmed action**: check whether store IARC ratings are accepted or a GCAM filing is needed. Content was already designed to these norms. |

## 5. Religious and national respect

| Area | Requirement | Source | Status |
|---|---|---|---|
| Makkah / Madinah | No combat, enemies, explosions or bosses in or near the Haram. The Kaaba and the Prophet's Mosque are never shown up close. | internal policy | **fixed and tested**: Makkah stages are Jeddah, Taif and Okaz. Madinah's Mount Uhud stage became AlUla Old Town. `RespectTests` checks for holy words. |
| Mosques and holy sites | No stage set at a mosque or a living place of worship. Worship buildings appear only as a far skyline with no colliders, and no prophets are depicted. | internal | **fixed**: `LandmarkKinds.Sacred` is blocked in stage landmarks and allowed only in `RegionDef.skyline`. UAE "Sheikh Zayed Mosque Area" became Al Ain Oasis. Iraq's Malwiya became Babylon (the minaret is skyline only). Ancient ruins (Karnak, Baalbek) are kept as archaeology, and their stages are now named "Ancient Ruins" (not "Temple"). |
| No religious landmarks in Japan and Indonesia | Ahmed's rule: no landmark with a religious symbol or religious origin. | internal | **fixed**: Japan's skyline is now `castle_keep`, `snow_mountain`, `bamboo_grove`, `red_wooden_bridge`; Indonesia's is `volcano_rice_terraces`, `volcano`. The pagoda, torii and stupa builders were deleted. `RespectTests` checks that no landmark id contains stupa, pagoda, torii, church, cathedral, cross, redeemer, temple_active, shrine, arms_statue, domed_basilica, uluru or red_monolith. Also dropped on Ahmed's follow-up: France's island abbey (`island_abbey`, stage renamed "Tidal Island", skyline empty), Spain's mosque-cathedral arches (none in the data; the culture line no longer says horseshoe arches; the Alhambra-style `palace_arches` courtyard stays), Japan's stone lantern (none in the data; the bamboo stage's `stone_pillars` became `mountains`; cherry trees and the unnamed Fuji-style `snow_mountain` stay), Mauritania's manuscript shelf (`manuscript_town` became `mud_town`, line says "old mud town"). Mosques, minarets and madrasa portals stay allowed and are not in the forbidden list. Also removed on the latest review: `domed_basilica` and `arms_statue` (no region used them; the factory cases and the open-arms builder are deleted). **Skyline-only sacred shapes that stay, by Ahmed's decision:** `spiral_minaret`, `tent_mosque`, `marble_mausoleum`, `blue_domes`. |
| Quranic text and the Shahada | Never on breakable, stompable or floor items. | internal | **fixed**: `RespectRules.NeverOnProps` / `PropTextAllowed`. A test scans all data files. |
| Flags | Saudi and Iraqi flags (religious text) and all others are never on boxes, floors, lower clothing or trash, and never destroyed. | internal | **fixed**: no national flags are drawn anywhere. The profile header "flag" became "home_badge". A test bans flag art ids. Kenney's checkpoint "flag" prop is a plain pennant. Brazil's trail renamed `carnival_sparks`. |
| National symbols | Respect all national symbols and emblems. | internal | **OK**: the passport uses only original stamp art, no government emblems (`StampArt.Banned`). |

## 6. Maps

| Area | Requirement | Source | Status |
|---|---|---|---|
| Disputed borders | Draw no borders (Kashmir, Western Sahara, Halayeb, …). | internal | **OK**: the world map is region cards with stage nodes, with no geography or outlines. Any later map art must use stylized islands or pins only. No political claims in text: Palestine and Jerusalem content was removed in an earlier commit. |

## 7. Intellectual property

| Area | Requirement | Source | Status |
|---|---|---|---|
| Eiffel Tower at night | The night light show is protected. | https://www.toureiffel.paris/en/business/use-image-of-eiffel-tower | **fixed**: the generic `iron_tower` kind is rejected on night stages by validation (tested). **needs lawyer**: whether using the shape at all is fine (generally yes for the 1889 structure). |
| Burj Khalifa / Kingdom Centre | Building names and shapes can be trademarks. | — | **fixed**: `kingdom_centre` became the generic `arch_tower`. Dubai stage is "Downtown Dubai" with a generic `needle_tower`. Validation now rejects brand and building trade names in stage names, and the iron tower at night (tested). **needs lawyer**. |
| Brands, clubs, real people | None. | — | **OK**: grep is clean. Cars, mounts and vehicles are generic. Bosses are original characters. |
| Font licences | | ThirdParty/ | **OK**: Noto Naskh Arabic, Noto Sans Devanagari and Noto Sans JP, all OFL 1.1, with files in ThirdParty/. |
| Music and SFX licences | | Assets/_Aron/Audio/SFX/CREDITS.txt | **OK**: all music and the game sounds are original work of the project owner. The few third-party effects left (rain, thunder, breakables) are CC0 and credited in `Audio/SFX/CREDITS.txt`. **needs Ahmed action**: keep the licence proof for the music and sounds in private records, outside this repository. |
| 3D models | | ThirdParty/ART_CREDITS.md | **OK**: Quaternius and Kenney, CC0. Any commissioned models: **needs Ahmed action** to keep the commercial-use licence record on file. |
| Original-character check | No Crash IP: no bandicoot, Aku Aku-like mask, or Wumpa-like fruit. | docs | **OK** (see the earlier IP review). |

## 8. Accessibility, consumer law, terms

| Area | Requirement | Source | Status |
|---|---|---|---|
| Accessibility | Remappable or resizable buttons, colour filters, subtitles, reduced motion and flash, one-thumb mode. | https://gameaccessibilityguidelines.com/ | **OK**: `AccessSettings`. Hindi shaping on device is to be checked (see ART_CREDITS). |
| Consumer law: clear pricing, digital-content withdrawal right (EU) | Waiver at purchase for immediate delivery. | https://eur-lex.europa.eu/eli/dir/2011/83/oj | **needs lawyer**: the stores handle checkout. The terms mention it. |
| Saudi E-Commerce Law | Seller identity and contact details shown. | https://mc.gov.sa/ | **needs Ahmed action**: put the contact email and legal name in the store listing and the terms. |
| Terms of service | Governing law placeholder (KSA). | docs/terms_of_service.md | Draft, **needs lawyer**. |
| Privacy policy | Online, email and children sections. | docs/privacy_policy.md | Draft, **needs lawyer**. Host it at a public URL (**needs Ahmed action**). |

## 9. Mounts, racing, Australia and the museum (added with the race-mounts work)

| Area | Requirement | Source | Status |
|---|---|---|---|
| Paid mounts and "loot boxes" | Paid random rewards are restricted or banned in many places and are poor practice for a game children may play. | https://support.google.com/googleplay/android-developer/answer/9858738 | **OK**: every mount, saddle and colour has a fixed, direct price or an earn-by-play route. There are no random boxes. **needs lawyer**: rules for in-app purchases by minors per country. |
| Pay-to-win | Paid items must not decide ranked results. | internal | **OK**: in ranked and online races every mount is scaled to the same power budget (tested). Saddles and decorations are cosmetic only. |
| Points, gold medals, rentals | Points (5/3/1 per finish, daily cap 100) are earned by play and spent only on 48 h rentals; nothing can be owned for good with points, gold medals or coins (mounts and vehicles need a real purchase). They cannot be bought for money and cannot be cashed out. | internal | **needs lawyer**: whether they count as "virtual currency" for consumer-law and store rules in some countries. No real-money link exists in the code. |
| Computer racers in online races | Do not pretend a computer racer is a real person. | internal | **OK**: computer racers are labelled, and a match with fewer than 2 real racers gives no ranked bonus. |
| Online race points | Points must not be forgeable from the client. | internal | **needs Ahmed action**: the Cloud Code module that runs `ClaimValidator` is not created (see docs/mounts.md). The race runtime does not exist yet, so the client neither claims nor grants online points. The client signature is not a secret. |
| Women rivals' clothing | 16 of the 35 rivals are women. They are drawn modestly (long sleeves, long trousers or tunic, helmet, scarf where it fits). Noura's abaya never changes and she never rolls her sleeves. | internal | **needs cultural review**: someone from each of the 16 countries should look at the outfit and colours (`outfitReview` is true for all 16). |
| Rival lines and dialects | Arabic lines are written in a dialect per country, the rest in the country's language or English. | internal | **needs native-speaker review** for every dialect and non-Arabic line (marked in the data). |
| Australia: the red rock | The red rock formation in central Australia is sacred to its traditional owners, the Anangu, and has image restrictions. | internal, general knowledge | **fixed**: the `red_monolith` skyline shape was replaced by `red_rock_ridges`, a generic layered red sandstone range of several slabs that does not resemble that single dome. `uluru` and `red_monolith` are on the forbidden-landmark list, and a test scans all stage text for the name, the owners' name and "climb". **needs lawyer / cultural advisers** if a real place is ever shown, named or used in marketing. |
| Australia: national day | A national day card was considered. 26 January is contested and some Australians mark it as a day of mourning. | internal | **not added on purpose**. Do not add it without Aboriginal and Torres Strait Islander advice. |
| Australia: Indigenous culture | Art styles, stories and names belong to their communities. | internal | **OK**: no Indigenous art motifs, stories, names or instruments appear. The museum artifact is an opal, not a boomerang. **needs lawyer** if any such content is ever added (Indigenous Cultural and Intellectual Property). |
| Wildlife | Animals are never hurt by the player. Dangerous animals (for example the saltwater crocodile) only affect the hero, only where they really live, and always show a warning first. Only mounts can be ridden. | internal | **OK and tested** (`AustraliaWildlifeTests`, `HazardRulesTests`). Rating 7+ is unchanged: cartoon contact only, no blood. |
| Museum artifacts | Nothing religious, no flags, seals, weapons or emblems; objects shown with respect. | internal | **OK**: a test bans those words in names and facts. **needs native-speaker review**: facts are in Arabic and English only, other languages fall back to English. |
| Haptics | Vibration is not a permission problem on iOS. On Android `VIBRATE` is a normal permission. It must respect OS and battery settings and be switchable. | https://developer.android.com/reference/android/Manifest.permission#VIBRATE | **OK**: on/off, strength, off in battery saver. Hit-stop shows only the haptic when "reduce motion" is on. |
| Photosensitivity | Hit-stop is a 70–80 ms near-stop, not a flash. Lightning already respects "reduce flash". | https://gameaccessibilityguidelines.com/ | **OK**; **needs a play test** on device. |
| Maps | See section 6. | internal | **OK**: the world map is still a stage list. Australia has only a latitude and longitude in the weather data, no outline. If a real map screen is ever drawn, Australia may be shown only as a stylised island with a pin. |
| Settings stored | Haptics, camera and access choices are saved on the device only. | docs/privacy_policy.md | **OK**: nothing about them is sent to a server. |

## Owner and lawyer checklist

1. Play Console: target audience, Data safety, account-deletion URL, IARC questionnaire, and no remove_ads product.
2. App Store Connect: privacy labels, Kids category unselected, age rating, and the photo add-only string.
3. GCAM: check whether a filing is needed.
4. Backend: UGS project with Authentication, the OIDC provider `oidc-aron-email`, Cloud Code `AronAccount`, and Cloud Save. Secrets: mail-provider API key, OTP pepper, consent-token pepper. A public HTTPS domain for consent and magic links. DPAs with Unity and the mail provider.
5. Lawyer: consent ages per country, the parental verification method, PDPL transfers, the terms and privacy texts, Eiffel and Dubai building names, and the developer shop "apps" and "services".
6. Race mounts: create the in-app products listed in STORE_SETUP.md in both consoles, and the Cloud Code module for online race points (docs/mounts.md).
7. Lawyer (mounts and Australia): the virtual-currency status of race points and gold medals, in-app purchases by minors, and any future Indigenous-culture content.
8. Cultural review: the 16 women rivals' outfits, all rival dialect lines, the 35 museum artifacts and their fact lines, and the Australian wildlife and red rock ridges skyline.
