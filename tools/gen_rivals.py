"""Writes Resources/Rivals/rivals.json: one recurring rival per country (35), each with 8 short barks in the country's
own language or dialect. Every line keeps its text, a language/dialect tag, an Arabic subtitle, and a string key whose
12 translations give the subtitle in the player's UI language (UiStringTables: rival_bark_<kind>).

All spoken lines are DRAFTS for native-speaker review (review = "native" or "dialect"); English lines need none.
Run from the project root: python3 tools/gen_rivals.py
"""
import json

KINDS = ["start", "start2", "overtake", "overtaken", "boost", "crash", "win", "lose"]
SUB_AR = ["بالتوفيق! خلّنا نسوي سباق حلو!", "ليفز الأفضل!", "نشوفك عند خط النهاية!", "ما شاء الله، ما أسرعك!",
          "هذا أنا أنطلق!", "أوه! أنا بخير!", "كان سباقًا ممتعًا!", "أحسنت، أنت فزت!"]

# id: (nameEn, nameAr, female, mount, style, outfit, lang, dialect, lines[8])
R = {}
def r(cid, en, ar, female, mount, style, outfit, lang, dialect, lines):
    assert len(lines) == 8, cid
    R[cid] = dict(nameEn=en, nameAr=ar, female=female, mount=mount, style=style, outfit=outfit, lang=lang, dialect=dialect, lines=lines)

r("saudi","Sahm","سهم",False,"horse","clean","white thobe, red-and-white head cloth, racing vest","ar","najdi",
  ["بالتوفيق يا بطل، يلا نتسابق!","الله يوفّق اللي يستاهل!","نلتقي عند خط النهاية!","وش هالسرعة! ما شاء الله!","أبشر، هذي أنا طالع!","يا ساتر! أنا بخير!","سباق حلو، يعطيك العافية!","مبروك عليك، تستاهل!"])
r("kuwait","Dana","دانة",True,"camel","steady","long sea-blue tunic over trousers, soft scarf under a racing helmet","ar","kuwaiti",
  ["الله يوفقك، يلا نتسابق!","اللي يستاهل يفوز!","نشوفك عند النهاية!","شنو هالسرعة! ما شاء الله!","هذا أنا طالعة، شدّ حيلك!","أووه! أنا زينة، لا تحاتي!","سباق حلو، يعطيك العافية!","مبروك، تستاهل الفوز!"])
r("uae","Saqr","صقر",False,"falcon_glider","aggressive","white kandura, light head cloth, falcon-grey racing vest","ar","emirati",
  ["بالتوفيق، يالله نتسابق!","الله يوفّق اللي يستاهل!","نشوفك عند خط النهاية!","ما شاء الله، شو هالسرعة!","هذا آنا طالع!","أوه! آنا بخير، لا تخاف!","سباق حلو، مشكور!","مبروك، تستاهل الفوز!"])
r("bahrain","Lulwa","لولوة",True,"horse","clean","pearl-white long tunic with trousers, pale scarf under a helmet","ar","bahraini",
  ["الله يوفقك، يالله نتسابق!","اللي يستاهل يفوز!","نلتقي عند النهاية!","شنو هالسرعة! ما شاء الله!","هذا أنا طالعة!","أوه! أنا زينة، لا تخاف!","سباق حلو، مشكور!","مبروك، تستاهل!"])
r("qatar","Najm","نجم",False,"camel","steady","white thobe, maroon-and-white head cloth, racing vest","ar","qatari",
  ["بالتوفيق، يالله نتسابق!","الله يوفق اللي يستاهل!","نشوفك عند النهاية!","ما شاء الله، شو هالسرعة!","هذا أنا طالع، شد حيلك!","أوه! أنا بخير، لا تخاف!","سباق حلو، مشكور!","مبروك، تستاهل الفوز!"])
r("oman","Mazin","مازن",False,"camel","clean","white dishdasha, embroidered cap, green sash","ar","omani",
  ["بالتوفيق، يا الله نتسابق!","اللي يستاهل يفوز!","نلتقي عند النهاية!","ما شاء الله، شو هالسرعة!","ها أنا طالع!","أوه! أنا بخير، لا تخاف!","سباق جميل، شكرًا لك!","مبروك، تستاهل!"])
r("yemen","Amal","أمل",True,"camel","steady","long patterned tunic with trousers, soft scarf under a helmet","ar","yemeni",
  ["بالتوفيق، يا الله نتسابق!","الله يوفق اللي يستحق!","نشوفك عند النهاية!","إيش هالسرعة، ما شاء الله!","هذي أنا طالعة!","أوه! أنا بخير، لا تخافي!","سباق حلو، يعطيك العافية!","مبروك، تستاهلي الفوز!"])
r("iraq","Dijla","دجلة",True,"horse","steady","long teal tunic with trousers, light scarf under a helmet","ar","iraqi",
  ["بالتوفيق، يلا نتسابق!","الله يوفّق اللي يستاهل!","نشوفك عند خط النهاية!","شكد سريع! ماشاء الله!","هذي آني أطلع، شدّ حيلك!","أوف! آني زينة، لا تخاف!","سباق حلو، تسلم!","مبروك، تستاهل الفوز!"])
r("syria","Yasmin","ياسمين",True,"horse","clean","long white-and-green tunic with trousers, soft scarf under a helmet","ar","levantine",
  ["بالتوفيق، يلا نتسابق!","الله يوفّق يلي بيستاهل!","منشوفك عند خط النهاية!","يا الله شو سريع!","هاني طالعة، شدّ حيلك!","أوف! أنا منيحة، لا تخاف!","سباق حلو، يسلمو!","مبروك، بتستاهل!"])
r("lebanon","Nour","نور",False,"horse","steady","green-and-white racing jacket with a stylised tree badge","ar","levantine",
  ["الله معك، يلا نتسابق!","يربح يلي بيستاهل!","منتلاقى عالنهاية!","يا الله شو سريع!","هيدا أنا طالع!","أوف! أنا منيح، ما تخاف!","سباق حلو، يسلمو!","مبروك، بتستاهل!"])
r("jordan","Hani","هاني",False,"camel","aggressive","red-and-white head cloth, desert-sand jacket","ar","jordanian",
  ["بالتوفيق، يلا نتسابق!","الله يوفّق اللي بستاهل!","منشوفك عند النهاية!","يا زلمة شو سريع!","هاي أنا طالع!","أوف! أنا تمام، لا تخاف!","سباق حلو، يعطيك العافية!","مبروك، بتستاهل!"])
r("egypt","Mido","ميدو",False,"camel","aggressive","sky-blue galabeya jacket, sun-yellow scarf","ar","egyptian",
  ["بالتوفيق يا صاحبي، يلا نتسابق!","اللي يستاهل هو اللي يكسب!","هنتقابل عند خط النهاية!","إيه السرعة دي! ما شاء الله!","أهو أنا طالع!","آه! أنا كويس، متخافش!","سباق جميل، تسلم!","مبروك، تستاهل!"])
r("sudan","Asim","عاصم",False,"camel","steady","long white jalabiya, white turban cloth, green sash","ar","sudanese",
  ["بالتوفيق يا زول، يلا نتسابق!","الزول البستاهل هو الفايز!","نتلاقى في النهاية!","ياخي دي سرعة شنو! ما شاء الله!","ها أنا طالع!","آي! أنا كويس، ما تخاف!","سباق حلو، تسلم!","مبروك، بتستاهل!"])
r("libya","Salma","سلمى",True,"horse","clean","long olive tunic with trousers, soft scarf under a helmet","ar","libyan",
  ["بالتوفيق، يا لله نتسابقوا!","اللي يستاهل يفوز!","نشوفوك في النهاية!","يا سلام، شن هالسرعة!","هاني طالعة!","أوه! أنا مليحة، ما تخافش!","سباق مليح، يعطيك الصحة!","مبروك، تستاهل!"])
r("tunisia","Zitouna","زيتونة",True,"horse","steady","long olive-green tunic with trousers, soft scarf under a helmet","ar","tunisian",
  ["بالتوفيق يا صاحبي، يلا نتسابقو!","اللي يستاهل يربح!","نلقاوك في النهاية!","يا سلام، قدّاش سريع!","هاني طالعة!","أوه! أنا باهية، ما تخافش!","سباق باهي، عيشك!","مبروك، تستاهل!"])
r("algeria","Amine","أمين",False,"camel","aggressive","white-and-green racing jacket, light cap","ar","algerian",
  ["بالتوفيق يا خويا، يلا نتسابقو!","اللي يستاهل يربح!","نتلاقاو عند النهاية!","واش هاد السرعة! ما شاء الله!","هاني طالع!","أوه! أنا بخير، ماتخافش!","سباق زوين، صحّة!","مبروك، تستاهل!"])
r("morocco","Zineb","زينب",True,"camel","clean","long terracotta tunic with trousers, soft scarf under a helmet","ar","darija",
  ["بالتوفيق يا صاحبي، يالله نتسابقو!","اللي يستاهل هو اللي يربح!","غادي نتلاقاو فالنهاية!","واو، شحال ديال السرعة!","هانا طالعة!","آخ! أنا بخير، ما تخافش!","سباق زوين، شكرا بزاف!","مبروك، تستاهل!"])
r("mauritania","Mariem","مريم",True,"camel","steady","light-blue long wrap over trousers, soft scarf under a helmet","ar","hassaniya",
  ["بالتوفيق، يالله نتسابقو!","اللي يستاهل يفوز!","نتلاقو فالنهاية!","ماشاء الله، شكثر سريع!","هاني طالعة!","أوه! أنا بخير، ما تخاف!","سباق زين، شكرا!","مبروك، تستاهل!"])
r("portugal","Tiago","تياغو",False,"horse","clean","red-and-green racing jacket, simple cap","pt","pt-PT",
  ["Boa sorte, vamos correr!","Que ganhe o melhor!","Vemo-nos na meta!","Uau, és mesmo rápido!","Lá vou eu!","Ups! Estou bem!","Que corrida tão divertida!","Muito bem, ganhaste!"])
r("spain","Pilar","بيلار",True,"horse","aggressive","red-and-yellow racing jacket with long sleeves, helmet over a scarf","es","es-ES",
  ["¡Buena suerte, vamos a correr!","¡Que gane el mejor!","¡Nos vemos en la meta!","¡Vaya, qué rápido eres!","¡Allá voy!","¡Ups! ¡Estoy bien!","¡Qué carrera tan divertida!","¡Bien hecho, has ganado!"])
r("france","Camille","كاميل",False,"horse","clean","blue-white-red striped sweater, flat cap","fr","fr",
  ["Bonne chance, on y va !","Que le meilleur gagne !","On se retrouve à l'arrivée !","Waouh, tu es rapide !","C'est parti !","Oups ! Ça va !","Quelle course amusante !","Bravo, tu as gagné !"])
r("uk","Poppy","بوبي",True,"horse","steady","tweed jacket, long trousers, riding helmet","en","en-GB",
  ["Good luck, let's race!","May the best racer win!","See you at the finish line!","Blimey, you're quick!","Here I go!","Oops! I'm all right!","What a cracking race!","Well done, you won!"])
r("germany","Lukas","لوكاس",False,"horse","clean","grey-and-red racing jacket, practical cap","de","de",
  ["Viel Glück, los geht's!","Möge der Beste gewinnen!","Wir sehen uns im Ziel!","Wow, bist du schnell!","Jetzt geht's los!","Hoppla! Mir geht's gut!","Was für ein tolles Rennen!","Gut gemacht, du hast gewonnen!"])
r("turkey","Elif","إليف",True,"horse","steady","long red tunic with trousers, soft scarf under a helmet","tr","tr",
  ["İyi şanslar, hadi yarışalım!","En iyisi kazansın!","Bitişte görüşürüz!","Vay, çok hızlısın!","İşte başlıyorum!","Hay aksi! İyiyim!","Ne güzel bir yarış!","Aferin, sen kazandın!"])
r("uzbekistan","Dilnoza","دِلنوزا",True,"horse","clean","long patterned silk-style tunic with trousers, soft scarf under a helmet","uz","uz",
  ["Omad tilayman, poygaga!","Eng yaxshisi g'alaba qilsin!","Marra chizig'ida ko'rishamiz!","Voy, qanday tezsan!","Mana, ketdim!","Voy! Men yaxshiman!","Qanday qiziq poyga!","Barakalla, sen yutding!"])
r("pakistan","Bilal","بلال",False,"camel","steady","long green shalwar kameez, embroidered cap","ur","ur",
  ["اچھی قسمت، چلو ریس لگاتے ہیں!","جو بہترین ہو وہ جیتے!","فنش لائن پر ملتے ہیں!","واہ، آپ تو بہت تیز ہیں!","لو، میں چلا!","اوہ! میں ٹھیک ہوں!","کیا مزے کی ریس تھی!","شاباش، آپ جیت گئے!"])
r("india","Aarav","آراف",False,"giraffe","clean","saffron-and-cream kurta jacket, simple headband","hi","hi",
  ["शुभकामनाएँ, चलो रेस लगाएँ!","जो सबसे अच्छा हो, वही जीते!","फ़िनिश लाइन पर मिलते हैं!","वाह, तुम तो बहुत तेज़ हो!","लो, मैं चला!","अरे! मैं ठीक हूँ!","कितनी मज़ेदार रेस थी!","शाबाश, तुम जीत गए!"])
r("malaysia","Siti","سيتي",True,"horse","steady","bright yellow baju kurung with a headscarf under a helmet","ms","ms",
  ["Semoga berjaya, jom berlumba!","Biar yang terbaik menang!","Jumpa di garisan penamat!","Wah, laju betul kamu!","Ini aku pergi!","Alamak! Aku okey!","Seronoknya perlumbaan ini!","Tahniah, kamu menang!"])
r("indonesia","Budi","بودي",False,"horse","aggressive","batik-pattern racing shirt, black cap","id","id",
  ["Semoga sukses, ayo balapan!","Semoga yang terbaik menang!","Sampai jumpa di garis finis!","Wah, kamu cepat sekali!","Aku berangkat!","Aduh! Aku baik-baik saja!","Seru sekali balapannya!","Selamat, kamu menang!"])
r("japan","Haruto","هاروتو",False,"falcon_glider","clean","indigo happi-style jacket, headband","ja","ja",
  ["がんばろうね、レースしよう！","いちばんの人が勝つよ！","ゴールで会おうね！","わあ、速いね！","いくよー！","あいたた！だいじょうぶ！","たのしいレースだったね！","おめでとう、きみの勝ちだよ！"])
r("usa","Jake","جيك",False,"horse","aggressive","denim jacket, baseball cap","en","en-US",
  ["Good luck, let's race!","May the best racer win!","See you at the finish line!","Whoa, you're fast!","Here I go!","Oops! I'm okay!","That was a blast!","Nice job, you won!"])
r("mexico","Mateo","ماتيو",False,"horse","steady","embroidered colourful vest, straw hat","es","es-MX",
  ["¡Mucha suerte, vamos a correr!","¡Que gane el mejor!","¡Nos vemos en la meta!","¡Órale, qué rápido eres!","¡Ahí voy!","¡Ay! ¡Estoy bien!","¡Qué carrera tan padre!","¡Bien hecho, ganaste!"])
r("brazil","Luiza","لويزا",True,"giraffe","clean","green-and-yellow tracksuit with long sleeves and trousers, helmet","pt","pt-BR",
  ["Boa sorte, vamos correr!","Que vença o melhor!","A gente se vê na linha de chegada!","Nossa, como você é rápido!","Lá vou eu!","Opa! Estou bem!","Que corrida divertida!","Muito bem, você ganhou!"])
r("south_africa","Thabo","تابو",False,"ostrich","steady","bright patterned shirt, bush hat","en","en-ZA",
  ["Good luck, let's race!","May the best racer win!","See you at the finish!","Hey, you're quick!","Here we go!","Eish! I'm fine!","What a lekker race!","Well done, you won!"])
r("australia","Tilly","تيلي",True,"ostrich","clean","outback hat, long-sleeved khaki shirt and trousers, helmet","en","en-AU",
  ["Good luck, let's race!","May the best racer win!","See you at the finish line!","Wow, you're fast!","Here I go!","Oops! I'm all good!","What a ripper of a race!","Good on you, you won!"])

ORDER = ["saudi","kuwait","uae","bahrain","qatar","oman","yemen","iraq","syria","lebanon","jordan","egypt","sudan","libya","tunisia","algeria","morocco","mauritania",
         "portugal","spain","france","uk","germany","turkey","uzbekistan","pakistan","india","malaysia","indonesia","japan","usa","mexico","brazil","south_africa","australia"]
assert sorted(ORDER) == sorted(R), set(R) ^ set(ORDER)

def review(lang, dialect):
    if lang == "en": return "none"
    if lang == "ar": return "dialect"
    return "native"

out = dict(version=1, kinds=KINDS, rivals=[])
for cid in ORDER:
    d = R[cid]
    lines = []
    for i, k in enumerate(KINDS):
        lines.append(dict(id=f"rival_{cid}_{k}", kind=k, lang=d["lang"], dialect=d["dialect"], text=d["lines"][i],
                          subtitleAr=SUB_AR[i], subtitleKey=f"rival_bark_{k}", review=review(d["lang"], d["dialect"])))
    out["rivals"].append(dict(id=f"rival_{cid}", country=cid, nameEn=d["nameEn"], nameAr=d["nameAr"], female=d["female"], modest=True,
                              mount=d["mount"], style=d["style"], model=f"rival_{cid}", outfit=d["outfit"],
                              outfitReview=bool(d["female"]), lines=lines))
json.dump(out, open("Assets/_Aron/Resources/Rivals/rivals.json", "w", encoding="utf-8"), ensure_ascii=False, indent=1)
print(len(out["rivals"]), "rivals,", sum(len(x["lines"]) for x in out["rivals"]), "lines,", sum(len(l["text"]) for x in out["rivals"] for l in x["lines"]), "chars")
