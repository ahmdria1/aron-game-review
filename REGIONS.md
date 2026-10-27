# المناطق والمراحل

كل منطقة ملف مستقل في `Assets/_Aron/Resources/Regions/<regionId>.json`. اللعبة تقرأ كل الملفات اللي في المجلد وترتبها بحقل `order`، فما تحتاج تعدل أي كود عشان تضيف منطقة.

## الترتيب

| المجموعة | `group` | `order` |
|---|---|---|
| السعودية | `saudi` | 100–199 |
| الخليج | `gulf` | 200–299 |
| الدول العربية | `arab` | 300–399 |
| العالم | `world` | 400–499 |

اللعبة فيها ٣٥ دولة في ٤٧ منطقة. **أستراليا هي الدولة ٣٥** (`australia`، الترتيب 470، مجموعة `world`): السهول الحمراء، غابة الأوكالبتوس، ساحل الشعاب، مدينة الميناء، الغابة المطيرة، سهول الغروب، سباق ركوب الأمواج، وزعيم على سطح الميناء. سلاسل الصخور الحمراء (طبقات رملية عامة، مو نسخة من صخرة حقيقية) تظهر **بعيدة فقط في الأفق**: ما تُلعب ولا تُتسلق ولا تنكسر ولا ينكتب اسمها. ما أضفنا يوماً وطنياً لأن 26 يناير فيه خلاف. التفاصيل في `docs/culture_sources.md` و`docs/compliance_review.md`.

مناطق السعودية تمشي بخطوة 5 (الرياض 100، مكة 105، المدينة 110، ...). خلّ فراغات بين الأرقام عشان تقدر تدخّل منطقة بالنص بعدين. لازم الرقم يكون فريد، واسم الملف نفس `id`.

## شكل الملف

```json
{
  "order": 100, "id": "riyadh", "nameAr": "الرياض", "nameEn": "Riyadh",
  "group": "saudi", "country": "saudi", "costume": "",
  "music": "music_saudi_najd", "ground": "#D9B98A", "accent": "#A1262B", "sky": "#9EC7E8",
  "stages": [ ... ],
  "boss": { ... }
}
```

- `country`: يحدد لبس أرون ونورة من `Resources/Costumes/<country>.json`.
- `costume`: نسخة لبس للمنطقة كلها (مثل `saudi_hijazi`). فاضي = الافتراضي للدولة. نورة تلبس النسخة المقابلة لها تلقائياً (`saudi_hijazi` ← `saudi_noura_hijazi`).
- `music`: ثيم المنطقة، واحد من مقاطع `Resources/Audio/Music`. كل دولة لها ثيمها (`music_country_<country>`)، والسعودية لها خمسة ثيمات إقليمية (`music_saudi_najd` و`_hejaz` و`_south` و`_eastern` و`_north`). المراحل العادية تشغّل ثيم المنطقة، ومراحل المطاردة والحركة والسباق تشغّل مقطع نوعها (`music_stage_chase` و`_action` و`_race`)، ومعركة الزعيم `music_stage_boss`.
- `music` داخل المرحلة (اختياري): مقطع لهذه المرحلة وحدها. في مرحلة الزعيم يكون مقطع المعركة (مثل `music_tabuk_boss`)، وفي غيرها يغلب مقطع النوع (مثل `music_kuwait_sea` في مراحل الكويت ١ و٣ والسباق).

### المرحلة

```json
{
  "id": "riyadh_1", "nameAr": "الديرة والمصمك", "nameEn": "Al Dirah and Masmak",
  "terrain": "street", "landmarks": ["masmak", "mud_town"], "mount": "",
  "difficulty": 1, "boss": false,
  "type": "normal", "time": "day", "weather": "clear", "costume": "",
  "lineAr": "المصمك! هنا بدأت الحكاية.", "extra": ""
}
```

- `terrain`: street, souq, desert, coast, mountain, oasis, ruins, forest, river
- `landmarks`: من القائمة في `LandmarkKinds.All` (RegionCatalog.cs)
- `mount`: فاضي أو camel أو horse. الجمل للمراحل الصحراوية.
- `type`: normal، action (موجات أعداء وانفجارات وهروب بوقت)، chase (شي يلحقك من ورا)، boss
- `time`: day, sunset, night
- `weather`: clear, rain, sandstorm (العجاج بس على desert/ruins/street/souq)
  - هذا هو **طقس المرحلة المكتوب**. إذا كان مطر أو عجاج يبقى ثابتاً. إذا كان clear فالطقس الحي يأخذ مكانه حسب جدول الدولة والشهر في `Resources/Weather/weather.json` (لطيف في القصة، كامل في اللعب الحر)، ومرحلة الزعيم دائماً بطقسها المكتوب. التفاصيل في [docs/weather.md](docs/weather.md).
  - مراحل السباق تأخذ طقسها من `weather` في `Resources/Races/tracks.json` (clear, clouds, haze، وsnow لسباقات التزلج فقط) وما يتغير أبداً.
- `difficulty`: 1–10 وما ينزل داخل المنطقة

### الزعيم

`model` (soldier, ninja, robe)، `headgear`، ألوان `body/trim/projectile`، `health` (6–20)، و`phases` بعتبات تنزل من 1.0، كل مرحلة فيها `attacks` من charge, wave, summon, volley, leap, dash و`tauntAr`.

## شروط الفحص

`RegionCatalog.Validate()` واختبار `RegionFileTests` يرفضون الملف إذا:

- عدد المراحل أقل من 5 أو أكثر من 8، أو الزعيم مو آخر مرحلة، أو `type` = boss في غير مرحلة الزعيم
- ما فيه مرحلة ليل واحدة على الأقل
- ما فيه مرحلة action أو chase
- ما فيه مرحلة مطر أو عجاج
- الزعيم يشبه زعيم ثاني بالشكل أو بطريقة القتال، أو فيه مرحلة قتال ما يدوخ فيها (لازم charge أو leap أو dash)
- `country` فاضي، أو `order` خارج نطاق المجموعة أو مكرر

شغّل `dotnet test Tests/Aron.Logic.Tests` بعد أي ملف جديد.

## اللبس

`Resources/Costumes/<country>.json`:

```json
{ "country": "saudi", "costumes": [
  { "id": "saudi_najdi", "country": "saudi", "hero": "aron", "nameAr": "الثوب والشماغ", "isDefault": true, "price": 0,
    "materials": [{"material": "Clothes", "color": "#F7F7F2"}],
    "meshes": [{"slot": "body", "mesh": "thobe_long"}],
    "accessories": [{"kind": "shemagh", "color": "#C8141E", "pattern": "#F4F1EA"}, {"kind": "agal", "color": "#111111"}],
    "footwear": "leather_shoes", "footwearColor": "#5A3A22" }
]}
```

- كل `id` يبدأ باسم الدولة، وفيه لبس افتراضي واحد لأرون وواحد لنورة.
- الحذاء مقفل دائماً: leather_shoes أو closed_flats أو leather_boots. لا جزم رياضية ولا حافي.
- لبس نورة فيه `abaya` و`hijab` دائماً.
- العقال لازم يكون فوق شماغ أو غترة أو كوفية، وما ينرسم على الراس وباليد بنفس الوقت (`CostumeRules.Agal`).

### قطع اللبس

كل قطعة تنبني بالكود من `CostumeKit` (أشكال بسيطة قليلة المضلعات، وعدد الأضلاع ينزل على الأجهزة الضعيفة)، و`CostumeDresser` يركبها على عظام الراس والورك والذراع والقدم.

- `meshes` للجسم: thobe_long، abaya_long، kandura، galabeya_wide، jalabiya، djellaba_hooded، burnous_cape، jebba، sherwal، abaya_embroidered.
- `meshes` للقدم: shoes_closed، أو babouche_closed (بلغة مقفلة من قدام ومن ورا، بني جلد، مو افتراضية).
- قطع جديدة في `accessories`: kumma، massar، khanjar، futa، imma، chechia، jard، yashmagh، tarboosha، agal_cords.
- agal_cords تتعلق من العقال، فلازم معها `agal`، وتختفي معه لما يكون العقال باليد.
- نورة في مراحل البشت تلبس `<country>_noura_bisht`: عباية مطرزة محتشمة بنفس ألوان الدولة.

## الرفقاء والأبطال

- `Resources/Companions/companions.json`: الجمل والخيل والصقر والمها، ولكل واحد مستويات تنشرى بالعملات من تبويب الرفقاء. المستوى الأول مجاني، وكل مستوى أغلى وما يكون أضعف من اللي قبله.
- `Resources/Heroes/heroes.json`: أرون ونورة. نورة أخف وأسرع، تقفز مرتين وتنزلق بالعباية، تفتح الأبواب المقفلة، وترمي الشنطة وترجع لها.

## محلات المنطقة

`Resources/RegionShops/<regionId>.json`: محلات بأسماء محلية تنبني على جنب الشارع لما تبدأ المرحلة (`RegionShopFronts`). ما فيه أسماء ماركات ولا شعارات.

```json
{ "region": "kuwait", "shops": [ { "stage": "kuwait_2", "nameAr": "بقالة أبو فهد", "kind": "grocery" } ] }
```

- `stage` لازم تكون مرحلة من نفس المنطقة ومو مرحلة الزعيم.
- `kind` يحدد لون المظلة: grocery, fish, gold, silver, sweets, bakery, spices, coffee, perfume, frankincense، وغيرها تاخذ اللون الأحمر.

## مدة المرحلة والنجوم

كل مرحلة تُولَّد لتُلعب من ٥ إلى ٨ دقائق: عادية ٦–٨، مطاردة ٥–٦، أكشن ٥–٧، سباق ٥–٦، زعيم ٦–٨ (المشي مع القتال). وقت الهدف = ٠٫٩ من الوقت الكامل. ثلاث نجوم: نجمة للإنهاء، نجمة لكسر كل الصناديق الذهبية في محاولة واحدة، ونجمة للإنهاء أسرع من وقت الهدف. التفاصيل في `docs/feel_and_systems.md`.

## ملاحظة عن الخريطة

شاشة «خريطة المناطق» قائمة بطاقات ومراحل، **ما فيها خريطة جغرافية ولا حدود**، فما فيه ما يُرسم لأستراليا ولا لغيرها. موقع أستراليا (خط العرض والطول) موجود فقط في `Resources/Weather/weather.json` للطقس والشمس. لو رُسمت خريطة بعدين: جزر مبسطة بدبوس فقط، بدون حدود سياسية.
