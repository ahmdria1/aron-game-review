# إعداد المتجر

في شارع الرياض محلان حقيقيان لأحمد:

- **متجر المبرمج أحمد** (الإلكتروني): مظاهر أرون ونورة، رفيق الصحراء، العملات، المميزات، وتبويب «منتجات المبرمج أحمد».
- **متجر أحمد للمواد الغذائية**: تصفح وسلة، و«إتمام الطلب» يفتح موقع أحمد. ما فيه شراء داخل اللعبة.

اللاعب يدخل لما يضغط على المحل أو يمشي لين الباب. اللعبة تتوقف وهو داخل المتجر.

كل شي قابل للتعديل في ملف واحد:

```
Assets/_Aron/Resources/Store/StoreCatalog.json
```

بعد أي تعديل شغّل `dotnet test Tests/Aron.Logic.Tests`، وفيه اختبار يتأكد إن الملف سليم.

## منتجات الشراء الحقيقي

تنشئها بنفس المعرّف بالضبط في Google Play Console (منتجات داخل التطبيق) وفي App Store Connect (In-App Purchases).

| المعرّف | النوع في اللعبة | Google Play | App Store | المحتوى | السعر المقترح (USD) |
|---|---|---|---|---|---|
| `com.ahmdria1.aron.shemagh_gold` | non_consumable | منتج مُدار، لا يُستهلك | Non-Consumable | شماغ بحاشية ذهبية | تحدده أنت |
| `com.ahmdria1.aron.thobe_national` | non_consumable | منتج مُدار، لا يُستهلك | Non-Consumable | ثوب اليوم الوطني | تحدده أنت |
| `com.ahmdria1.aron.agal_fire` | non_consumable | منتج مُدار، لا يُستهلك | Non-Consumable | عقال النار | تحدده أنت |
| `com.ahmdria1.aron.agal_lightning` | non_consumable | منتج مُدار، لا يُستهلك | Non-Consumable | عقال البرق | تحدده أنت |
| `com.ahmdria1.aron.handbag_sadu` | non_consumable | منتج مُدار، لا يُستهلك | Non-Consumable | شنطة السدو | تحدده أنت |
| `com.ahmdria1.aron.camel_golden` | non_consumable | منتج مُدار، لا يُستهلك | Non-Consumable | ناقة ذهبية | تحدده أنت |
| `com.ahmdria1.aron.horse_royal` | non_consumable | منتج مُدار، لا يُستهلك | Non-Consumable | حصان بالعدة السعودية | تحدده أنت |
| `com.ahmdria1.aron.falcon_hur` | non_consumable | منتج مُدار، لا يُستهلك | Non-Consumable | صقر حر أبيض | تحدده أنت |
| `com.ahmdria1.aron.coins_small` | consumable | منتج مُدار، يُستهلك | Consumable | كيس عملات | تحدده أنت |
| `com.ahmdria1.aron.coins_medium` | consumable | منتج مُدار، يُستهلك | Consumable | كيس عملات | تحدده أنت |
| `com.ahmdria1.aron.coins_large` | consumable | منتج مُدار، يُستهلك | Consumable | خزنة عملات | تحدده أنت |
| `com.ahmdria1.aron.helper_bisht_start` | consumable | منتج مُدار، يُستهلك | Consumable | بشت ذهبي من البداية | تحدده أنت |
| `com.ahmdria1.aron.helper_coin_magnet` | consumable | منتج مُدار، يُستهلك | Consumable | مغناطيس العملات | تحدده أنت |
| `com.ahmdria1.aron.helper_early_lantern` | consumable | منتج مُدار، يُستهلك | Consumable | فانوس مبكر | تحدده أنت |
| `com.ahmdria1.aron.travel_ticket` | consumable | منتج مُدار، يُستهلك | Consumable | تذكرة سفر | تحدده أنت |
| `com.ahmdria1.aron.founder_pack` | non_consumable | منتج مُدار، لا يُستهلك | Non-Consumable | باقة المؤسس | تحدده أنت |
| `com.ahmdria1.aron.mount_camel` | non_consumable | منتج مُدار، لا يُستهلك | Non-Consumable | الجمل | $1.99 |
| `com.ahmdria1.aron.mount_ostrich` | non_consumable | منتج مُدار، لا يُستهلك | Non-Consumable | النعامة | $2.99 |
| `com.ahmdria1.aron.mount_giraffe` | non_consumable | منتج مُدار، لا يُستهلك | Non-Consumable | الزرافة | $3.99 |
| `com.ahmdria1.aron.mount_falcon_glider` | non_consumable | منتج مُدار، لا يُستهلك | Non-Consumable | الصقر الشراعي | $4.99 |
| `com.ahmdria1.aron.breed_shuqah` | non_consumable | منتج مُدار، لا يُستهلك | Non-Consumable | هجين الشقح | $1.99 |
| `com.ahmdria1.aron.breed_shaol` | non_consumable | منتج مُدار، لا يُستهلك | Non-Consumable | هجين الشعل | $2.99 |
| `com.ahmdria1.aron.breed_humr` | non_consumable | منتج مُدار، لا يُستهلك | Non-Consumable | هجين الحمر | $2.99 |
| `com.ahmdria1.aron.breed_maghateer` | non_consumable | منتج مُدار، لا يُستهلك | Non-Consumable | هجين المغاتير (الوضح) | $3.99 |
| `com.ahmdria1.aron.breed_majaheem` | non_consumable | منتج مُدار، لا يُستهلك | Non-Consumable | هجين المجاهيم | $3.99 |
| `com.ahmdria1.aron.boat_dhow` | non_consumable | منتج مُدار، لا يُستهلك | Non-Consumable | الداو الشراعي | $1.99 |
| `com.ahmdria1.aron.boat_speed` | non_consumable | منتج مُدار، لا يُستهلك | Non-Consumable | القارب السريع | $2.99 |
| `com.ahmdria1.aron.boat_water_scooter` | non_consumable | منتج مُدار، لا يُستهلك | Non-Consumable | الدراجة المائية | $3.99 |
| `com.ahmdria1.aron.car_sports` | non_consumable | منتج مُدار، لا يُستهلك | Non-Consumable | الرياضية الفيروزية | $3.99 |
| `com.ahmdria1.aron.car_convertible` | non_consumable | منتج مُدار، لا يُستهلك | Non-Consumable | الكلاسيكية المرجانية المكشوفة | $2.99 |
| `com.ahmdria1.aron.car_buggy` | non_consumable | منتج مُدار، لا يُستهلك | Non-Consumable | عربة الكثبان | $2.99 |
| `com.ahmdria1.aron.car_4x4` | non_consumable | منتج مُدار، لا يُستهلك | Non-Consumable | دفع رباعي الصحراء | $3.99 |
| `com.ahmdria1.aron.bike_road` | non_consumable | منتج مُدار، لا يُستهلك | Non-Consumable | دراجة السباق | $0.99 |
| `com.ahmdria1.aron.bike_mountain` | non_consumable | منتج مُدار، لا يُستهلك | Non-Consumable | دراجة الجبال | $1.99 |
| `com.ahmdria1.aron.bike_fat` | non_consumable | منتج مُدار، لا يُستهلك | Non-Consumable | دراجة الصحراء العريضة | $1.99 |
| `com.ahmdria1.aron.bike_bmx` | non_consumable | منتج مُدار، لا يُستهلك | Non-Consumable | دراجة بي إم إكس | $0.99 |
| `com.ahmdria1.aron.moto_dirt` | non_consumable | منتج مُدار، لا يُستهلك | Non-Consumable | الدبّاب | $1.99 |
| `com.ahmdria1.aron.moto_sport` | non_consumable | منتج مُدار، لا يُستهلك | Non-Consumable | الدراجة الرياضية | $3.99 |
| `com.ahmdria1.aron.moto_cruiser` | non_consumable | منتج مُدار، لا يُستهلك | Non-Consumable | دراجة الطريق الطويل | $2.99 |
| `com.ahmdria1.aron.moto_quad` | non_consumable | منتج مُدار، لا يُستهلك | Non-Consumable | الرباعية الصحراوية | $2.99 |
| `com.ahmdria1.aron.saddle_royal_horse` | non_consumable | منتج مُدار، لا يُستهلك | Non-Consumable | سرج ملكي (الخيل) | $0.99 |
| `com.ahmdria1.aron.saddle_royal_camel` | non_consumable | منتج مُدار، لا يُستهلك | Non-Consumable | سرج ملكي (الجمل) | $0.99 |
| `com.ahmdria1.aron.saddle_royal_ostrich` | non_consumable | منتج مُدار، لا يُستهلك | Non-Consumable | سرج ملكي (النعامة) | $0.99 |
| `com.ahmdria1.aron.saddle_royal_giraffe` | non_consumable | منتج مُدار، لا يُستهلك | Non-Consumable | سرج ملكي (الزرافة) | $0.99 |
| `com.ahmdria1.aron.saddle_royal_falcon_glider` | non_consumable | منتج مُدار، لا يُستهلك | Non-Consumable | سرج ملكي (الصقر الشراعي) | $0.99 |
| `com.ahmdria1.aron.thobe_c_cream` | non_consumable | منتج مُدار، لا يُستهلك | Non-Consumable | ثوب كريمي | $0.99 |
| `com.ahmdria1.aron.thobe_c_light_grey` | non_consumable | منتج مُدار، لا يُستهلك | Non-Consumable | ثوب رمادي فاتح | $0.99 |
| `com.ahmdria1.aron.thobe_c_sky_blue` | non_consumable | منتج مُدار، لا يُستهلك | Non-Consumable | ثوب أزرق سماوي | $0.99 |
| `com.ahmdria1.aron.thobe_c_olive` | non_consumable | منتج مُدار، لا يُستهلك | Non-Consumable | ثوب زيتي | $0.99 |
| `com.ahmdria1.aron.thobe_c_navy` | non_consumable | منتج مُدار، لا يُستهلك | Non-Consumable | ثوب كحلي | $0.99 |
| `com.ahmdria1.aron.thobe_c_brown` | non_consumable | منتج مُدار، لا يُستهلك | Non-Consumable | ثوب بني | $0.99 |
| `com.ahmdria1.aron.thobe_c_black` | non_consumable | منتج مُدار، لا يُستهلك | Non-Consumable | ثوب أسود | $0.99 |

- **ما تنشئه أنت:** كل معرّف في الجدول لازم ينشأ بنفس الاسم بالضبط في المتجرين. ما أُنشئ أي حساب ولا منتج ولا مفتاح تلقائياً، وكلها عليك.
- **لا تنشئ** `com.ahmdria1.aron.remove_ads`. اللعبة ما فيها إعلانات، وبيع «إزالة الإعلانات» تضليل، وانشال من المتجر وحزمة المؤسس (فيه اختبار يمنعه). لو أنشأته قبل كذا، عطّله.
- المراكب (`mount_*`) ومركبات الأقسام الأربعة (`boat_*` و`car_*` و`bike_*` و`moto_*`) والسروج الملكية (`saddle_royal_*`) وألوان الثوب (`thobe_c_*`) كلها «لا تُستهلك». **المراكب والمركبات تُمتلك بالشراء الحقيقي فقط**: ما فيه شراء دائم بالنقاط ولا بالميداليات ولا بالعملات. نقاط السباق تستأجر أي غرض مميّز ٤٨ ساعة بـ ١٠٠ نقطة (والتجديد يزيد على الوقت المتبقي). التفاصيل في `docs/mounts.md`.
- المركبات المجانية (ما تحتاج منتج): الخيل، قارب اللؤلؤ، سيارة الرحلة، دراجة المدينة، السكوتر. الأسماء بالـ١٢ لغة جاهزة (`r<id>` في الجداول)؛ أنشئ الباقي (١٤ منتج جديد) بنفس المعرّف بالضبط. سرج السدو والشرّابات «شكل فقط» وتُشترى بالعملات (٣٠٠ و٥٠٠)، ولا منتج لها.
- ما فيه اسم تجاري في أي مركبة: الدراجة المائية اسمها «Water scooter» وليس «Jet Ski» (علامة تجارية).
- عمود «السعر المقترح» فئة تقريبية بالدولار للمراكب والسروج وألوان الثوب بس. الباقي تحدد سعره أنت.
- السعر يُكتب في المتجرين فقط. اللعبة تعرض السعر المحلي اللي يرجع من Google أو Apple، وما فيه أسعار ريال مكتوبة في الكود.
- باقي المظاهر تنشرى بالعملات بس (`coinPrice` في الملف). والمظاهر اللي لها معرّف تنشرى بالعملات أو بالفلوس.
- العملات تجي كمان من التمر اللي يجمعه اللاعب في المرحلة.
- زر «استرجاع المشتريات» في المتجر يرجّع المنتجات اللي ما تُستهلك (مطلوب في آيفون).
- المشتريات تنحفظ في ملف الحفظ، وكل طلب ينحسب مرة وحدة.

لو تبي تضيف منتج جديد: أضفه في `items` بالملف وحط `productId` و`productType`، وبعدين أنشئه بنفس المعرّف في المتجرين.

## روابط أحمد

### متجر المبرمج أحمد (تبويب «منتجات المبرمج أحمد»)

في الملف تحت `developerShop`:

```json
"developerShop": {
  "baseUrl": "https://موقعك.com",
  "products": [
    { "id": "my_app", "category": "apps", "nameAr": "اسم التطبيق", "descAr": "وصف قصير",
      "priceText": "١٩ ر.س", "imageUrl": "https://موقعك.com/img/app.png", "url": "products/my-app" }
  ]
}
```

- `baseUrl` فاضي الحين، وطول ما هو فاضي يطلع للاعب «قريباً».
- `url` يا رابط كامل (`https://...`) يا مسار يتركّب بعد `baseUrl`.
- `imageUrl` اختياري. لو فاضي يطلع أول حرف من الاسم.
- `categories` هي التبويبات الصغيرة (تطبيقات، ألعاب، خدمات). غيّرها مثل ما تبي.
- المنتجات الموجودة الحين أمثلة مكتوب عليها «(مثال)». بدّلها بمنتجاتك.

### متجر أحمد للمواد الغذائية

في الملف تحت `groceryShop`:

```json
"groceryShop": {
  "baseUrl": "https://بقالتك.com",
  "checkoutTemplate": "{base}/cart?add={items}",
  "categories": [ { "id": "rice", "nameAr": "أرز وحبوب" } ],
  "products": [
    { "id": "rice_basmati", "category": "rice", "nameAr": "أرز بسمتي ٥ كجم",
      "priceText": "٤٥ ر.س", "imageUrl": "https://...", "url": "products/rice-basmati" }
  ]
}
```

- `baseUrl` فاضي الحين، والمتجر يعرض «قريباً» وزر «إتمام الطلب» ما يشتغل.
- `checkoutTemplate` اختياري. لو فاضي، «إتمام الطلب» يفتح الصفحة الرئيسية. لو تبي السلة توصل لموقعك استخدم هذي:
  - `{base}` الرابط الأساسي
  - `{items}` مثل `rice_basmati:2,laban:1`
  - `{ids}` و`{qtys}` المعرّفات والكميات كل وحدة لحالها، مفصولة بفواصل
  - `{count}` عدد القطع كلها
- `id` المنتج هو اللي يروح لموقعك في السلة، فخلّه نفس رمز المنتج عندك.
- `priceText` نص يُعرض بس. الدفع كله في موقعك.
- الأقسام الحالية: أرز وحبوب، تمور وقهوة، ألبان، مشروبات، بهارات، منظفات. والمنتجات أمثلة.

## التجربة في المحرر

- قائمة **Aron ← Store ← Use Fake Store** (مفعّلة افتراضياً): الشراء في المحرر ينجح على طول بأسعار تجريبية، وما ينخصم شي.
- **Give 5000 Coins** يعطيك عملات تجرب فيها الشراء.
- **Forget Fake Purchases** يمسح مشتريات المتجر التجريبي.
- بعد ما تعدّل المتجر أو المحلات شغّل **Aron ← 2. Build Everything** عشان ينبنى الشارع من جديد.

## ملاحظات

- الحزمة `com.unity.purchasing` نسخة 5.4.4 في `Packages/manifest.json`. لو انشالت الحزمة يشتغل المتجر عادي والشراء بالفلوس يطلع «غير متاح».
- اللعبة ما فيها إعلانات حالياً. «إزالة الإعلانات» تنحفظ في الحفظ (`adsRemoved`) عشان تكون جاهزة لو انضافت إعلانات. Apple ممكن ترفض منتج يشيل إعلانات مو موجودة، فلا تفعّله في App Store Connect إلا بعد ما تضيف الإعلانات.
- تبويب «الرفاق» فيه الذلول والحصان والصقر. الذلول (طبيعي، أبيض، أسود، ذهبي) والحصان (أشقر، أبيض، أسود بسرج ذهبي، ملكي) تركبهم في نفود الثمامة وميدان الفروسية، والصقر (شاهين، بُرقع ذهبي، حر أبيض) يطير معك في كل المراحل.
- الأحذية كلها جلد كلاسيكي مقفل، وما فيه أحذية رياضية.
