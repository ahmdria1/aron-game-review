# Culture sources and review notes

Each country's culture file (`Assets/_Aron/Resources/Culture/<country>.json`) holds the greeting, hospitality line, customs, palette, street scene, foods, local animals, the swing trail and the eating idle. Each country's race is in `Assets/_Aron/Resources/Races/tracks.json`.

These are general, well-known traditions taken from public sources such as tourism boards, national museums and encyclopaedia entries. No expert has checked them. **Before release, every country needs a review by a native speaker or someone from that culture**, especially the lines in all 12 languages.

## Food rules

- Every food is meant to be halal. `CultureRules` rejects pork, alcohol and similar words in all 12 languages.
- Where the best-known version of a dish is not halal, the game uses a halal version and names it that way: chicken shio ramen (Japan), halal beef burger (USA), halal boerewors (South Africa). The fact line never says the halal version is the traditional one.
- Dishes that are often made with lard, alcohol or pork stock were swapped out (for example tamales became quesadillas, a yakitori grill became a dango stall, Turkish cologne was dropped).
- Whether a dish is halal depends on how it is made. The game does not certify anything. Mark as **needs religious/legal review** if store text ever says "halal".
- Prices and buffs are game values only.

## Races

| Country | Region | Race | Ride | Tradition |
|---|---|---|---|---|
| saudi | qassim | Qassim Camel Race | camel on sand | Camel racing at Saudi camel festivals, such as the Crown Prince Camel Festival |
| kuwait | kuwait | Dhow Sailing Race | sail on water | Kuwait's traditional dhow sailing races |
| uae | uae | Falcon Glider Race | sky on air | Falconry contests at the desert festivals |
| bahrain | bahrain | Pearl Boat Race | boat on water | Rowing races of the old pearl-diving boats |
| qatar | qatar | Camel Track Race | camel on sand | Camel racing at the Al-Shahaniya track |
| oman | oman | Omani Horse Race | horse on sand | Traditional horse races at Omani festivals |
| yemen | yemen | Tihama Camel Race | camel on sand | Camel races of the Tihama coast |
| iraq | iraq | Marsh Boat Race | boat on water | Mashoof reed-boat races of the southern marshes |
| syria | syria | Arabian Horse Race | horse on grass | Arabian horse shows and races |
| lebanon | lebanon | Cedars Ski Race | ski on snow | Winter ski races in the Cedars mountains |
| jordan | jordan | Wadi Rum Desert Rally | car on sand | Desert rally driving in Wadi Rum |
| egypt | egypt | Nile Felucca Race | sail on water | Felucca sailing on the Nile |
| sudan | sudan | Butana Camel Race | camel on sand | Camel racing of the Butana plains |
| libya | libya | Great Desert Rally | car on dirt | Desert off-road rallies in the Libyan Sahara |
| tunisia | tunisia | Douz Camel Race | camel on sand | Camel racing at the Sahara Festival of Douz |
| algeria | algeria | Fantasia Horse Race | horse on dirt | Fantasia horse displays at festivals |
| morocco | morocco | Tbourida Horse Race | horse on sand | Tbourida horse festivals |
| mauritania | mauritania | Desert Camel Race | camel on sand | Camel racing at desert festivals |
| portugal | portugal | River Rabelo Race | boat on water | Rabelo boat regatta on the Douro |
| spain | spain | Andalusian Horse Race | horse on dirt | Andalusian horse fairs |
| france | france | Soapbox Kart Race | car on asphalt | Soapbox kart races (caisses à savon) in village summer fêtes |
| uk | uk | River Rowing Race | boat on water | Rowing regattas on the river |
| germany | germany | Mountain Road Race | car on asphalt | Classic mountain road rallies |
| turkey | turkey | Steppe Horse Race | horse on grass | Traditional rahvan horse racing |
| uzbekistan | uzbekistan | Steppe Horse Race | horse on grass | Horse races at Navruz festivals |
| pakistan | pakistan | Bull Race | bull on mud | Bail-dor bull racing in rural Punjab |
| india | india | Snake Boat Race | boat on water | Vallam Kali snake-boat races of Kerala |
| malaysia | malaysia | Sailing Regatta | sail on water | Traditional perahu sailing regattas |
| indonesia | indonesia | Mud Bull Race | bull on mud | Pacu Jawi bull racing in West Sumatra |
| japan | japan | Mountain Ski Race | ski on snow | Winter ski races in the northern Alps |
| usa | usa | Desert Highway Race | car on asphalt | Desert road rallies |
| mexico | mexico | Charro Horse Race | horse on dirt | Charrería horse races at rural fairs |
| brazil | brazil | Beach Surf Race | surf on water | Surf contests on the beaches of the south |
| south_africa | south_africa | Ostrich Race | ostrich on savanna | Ostrich races of the Little Karoo |
| australia | australia | Surf Race | surf on water | Surf carnival races on Australian beaches |
## Australia: review notes

- Country 35. The stages are original shapes: red plains, eucalyptus forest, coral coast, a harbour city (generic shell-roof hall and steel arch, no names), rainforest.
- The skyline is `red_rock_ridges`, a generic layered red sandstone range that does not resemble any single real rock. A sacred rock with image restrictions was removed on review, and `uluru` and `red_monolith` are on the forbidden-landmark list. Nothing is climbed or named. Mark as **needs lawyer / cultural advisers** if any real place is ever added.
- No national day is included on purpose: 26 January is contested.
- No Indigenous art, stories, names or instruments. The museum artifact is an opal, not a boomerang.
- The palette uses green and gold as plain colours, with no flag or emblem.
- The world map is a stage list, not a geographic map, so Australia is only a latitude and longitude in `Resources/Weather/weather.json`.
- Review needed: the 12-language lines, the rival (a woman, modest outfit), and the wildlife (hazards only affect the hero).

## Museum artifacts

One artifact per country in `Resources/Museum/artifacts.json` (35). Names are in 12 languages; the one-line facts are in Arabic and English only, so other languages show the English fact. Deliberately avoided: flags, seals, weapons, religious objects and Indigenous sacred objects. **Every name, fact and object choice needs a check by someone from that culture.**


## Per country

### Saudi Arabia (`saudi`)

- Greeting: "هلا والله" (Hello and welcome), gesture `hand_on_chest`, dialect `najdi`.
- Hospitality: In Saudi Arabia, guests are welcomed with coffee and dates.
- Customs: `pour_coffee`: Saudi coffee is poured with the left hand into a small cup, held in the right. `offer_dates`: Dates always come with the coffee.
- Foods: Kabsa (Spiced rice with chicken): Often called the national dish of Saudi Arabia.; Jareesh (Cracked wheat porridge): Shared from one big plate with family and guests in Saudi Arabia.; Mutabbaq (Stuffed pan pastry): Sold hot at street stalls all over Saudi Arabia.; Kleija (Cardamom date cookies): A sweet treat in Saudi Arabia, best with tea or coffee.; Gahwa (Saudi coffee with cardamom): A drink offered to guests in Saudi Arabia.
- Animals: Camel (mount), Falcon (flier), Arabian oryx (ground), Spiny-tailed lizard (ground), Arabian sand viper (hazard), Arabian wolf (ground)
- Street: vendors dates seller, coffee roaster; majlis by the street; kids play kairam, seven stones.
- Swing trail `date_sparkles`; eats `saudi_kabsa` when idle.
- Review: native-speaker check of all 12 languages, plus a cultural check of the customs and gesture.

### Kuwait (`kuwait`)

- Greeting: "هلا والله" (Hello and welcome), gesture `hand_on_chest`, dialect `kuwaiti`.
- Hospitality: In Kuwait, guests are welcomed with coffee and dates.
- Customs: `invite_in`: The diwaniya is open for friends every evening. `pour_coffee`: Coffee goes round the diwaniya.
- Foods: Machboos (Spiced rice with fish): Often called the national dish of Kuwait.; Murabyan (Shrimp rice): From the sea and coast of Kuwait.; Gers ogaili (Saffron cardamom cake): A sweet treat in Kuwait, best with tea or coffee.; Balaleet (Sweet vermicelli with egg): A favourite breakfast in Kuwait.; Luqaimat (Date-syrup dumplings): A sweet treat in Kuwait, best with tea or coffee.
- Animals: Falcon (flier), Dolphin (ground)
- Street: vendors fish seller, bakery; diwaniya; kids play kairam, dama.
- Swing trail `pearl_glints`; eats `kuwait_machboos` when idle.
- Review: native-speaker check of all 12 languages, plus a cultural check of the customs and gesture.

### UAE (`uae`)

- Greeting: "هلا والله، مرحبا" (Hello, welcome), gesture `hand_on_chest`, dialect `emirati`.
- Hospitality: In the UAE, guests are welcomed with coffee and dates.
- Customs: `pour_coffee`: Coffee is served until the guest shakes the cup. `offer_dates`: Fresh dates are offered with it.
- Foods: Harees (Slow-cooked wheat and meat): Cooked for weddings, Eid and big family days in the UAE.; Luqaimat (Date-syrup dumplings): A sweet treat in the UAE, best with tea or coffee.; Chebab (Saffron pancakes): A favourite breakfast in the UAE.; Madrouba (Mashed rice with chicken): Shared from one big plate with family and guests in the UAE.; Karak (Spiced milk tea): A drink offered to guests in the UAE.
- Animals: Falcon (flier), Arabian oryx (ground), Camel (mount), Arabian sand viper (hazard)
- Street: vendors luqaimat stall, karak tea; karak cafeteria; kids play kairam, al-teela marbles.
- Swing trail `palm_fronds`; eats `uae_luqaimat` when idle.
- Review: native-speaker check of all 12 languages, plus a cultural check of the customs and gesture.

### Bahrain (`bahrain`)

- Greeting: "هلا ومرحبا" (Hello and welcome), gesture `hand_on_chest`, dialect `bahraini`.
- Hospitality: In Bahrain, guests are welcomed with coffee and dates.
- Customs: `offer_sweets`: Bahraini halwa comes with the coffee. `invite_in`: Old houses open around a courtyard.
- Foods: Machboos (Spiced rice with chicken): Often called the national dish of Bahrain.; Halwa bahraini (Saffron rosewater halwa): A sweet treat in Bahrain, best with tea or coffee.; Muhammar (Sweet date rice): Shared from one big plate with family and guests in Bahrain.; Samboosa (Fried pastry triangles): A Ramadan iftar favourite in Bahrain.; Khubz tannour (Clay-oven bread): Daily bread on tables across Bahrain.
- Animals: Camel (mount), Flamingo (flier), Dolphin (ground)
- Street: vendors halwa shop, bread oven; seaside coffee house; kids play dama, kairam.
- Swing trail `pearl_glints`; eats `bahrain_halwa` when idle.
- Review: native-speaker check of all 12 languages, plus a cultural check of the customs and gesture.

### Qatar (`qatar`)

- Greeting: "هلا والله" (Hello and welcome), gesture `hand_on_chest`, dialect `qatari`.
- Hospitality: In Qatar, guests are welcomed with coffee and dates.
- Customs: `pour_coffee`: Three small cups is polite; then shake the cup. `play_game`: Kids play in the souq lanes at night.
- Foods: Machboos (Spiced rice with lamb): Often called the national dish of Qatar.; Thareed (Bread and vegetable stew): A Ramadan iftar favourite in Qatar.; Balaleet (Sweet vermicelli with egg): A favourite breakfast in Qatar.; Luqaimat (Date-syrup dumplings): A sweet treat in Qatar, best with tea or coffee.; Madrooba (Mashed rice with chicken): Shared from one big plate with family and guests in Qatar.
- Animals: Falcon (flier), Arabian oryx (ground), Camel (mount)
- Street: vendors spice seller, falcon shop; souq coffee house; kids play kairam, stone game.
- Swing trail `sand_swirl`; eats `qatar_machboos` when idle.
- Review: native-speaker check of all 12 languages, plus a cultural check of the customs and gesture.

### Oman (`oman`)

- Greeting: "هلا وغلا" (Hello and welcome), gesture `hand_on_chest`, dialect `omani`.
- Hospitality: In Oman, guests are welcomed with coffee and dates.
- Customs: `offer_sweets`: Omani halwa and coffee for every guest. `pour_coffee`: The coffee pot (dallah) is always ready.
- Foods: Shuwa (Slow-roasted lamb): Cooked for weddings, Eid and big family days in Oman.; Halwa omani (Omani halwa): A sweet treat in Oman, best with tea or coffee.; Mashuai (Kingfish with lemon rice): From the sea and coast of Oman.; Harees (Slow-cooked wheat and meat): Shared from one big plate with family and guests in Oman.; Khubz rakhal (Thin flatbread): Daily bread on tables across Oman.
- Animals: Arabian oryx (ground), Sea turtle (ground), Camel (mount)
- Street: vendors halwa maker, frankincense seller; souq coffee corner; kids play kairam, al-mahbas.
- Swing trail `frankincense_smoke`; eats `oman_halwa` when idle.
- Review: native-speaker check of all 12 languages, plus a cultural check of the customs and gesture.

### Yemen (`yemen`)

- Greeting: "أهلاً وسهلاً" (Welcome), gesture `handshake`, dialect `yemeni`.
- Hospitality: In Yemen, guests are welcomed with a cup of coffee.
- Customs: `pour_coffee`: Qishr coffee from the husk with ginger. `share_bread`: Bint al-sahn is shared hot with honey.
- Foods: Saltah (Bubbling stew with fenugreek): Often called the national dish of Yemen.; Bint al-sahn (Honey layered bread): A sweet treat in Yemen, best with tea or coffee.; Mandi (Pit-cooked rice and chicken): Cooked for weddings, Eid and big family days in Yemen.; Fahsa (Shredded meat stew): Shared from one big plate with family and guests in Yemen.; Qishr (Coffee-husk ginger drink): A drink offered to guests in Yemen.
- Animals: Nubian ibex (ground), Camel (mount), Falcon (flier)
- Street: vendors honey seller, bread oven; mafraj with a view; kids play kairam, stone game.
- Swing trail `coffee_steam`; eats `yemen_saltah` when idle.
- Review: native-speaker check of all 12 languages, plus a cultural check of the customs and gesture.

### Iraq (`iraq`)

- Greeting: "هلا بيك" (Hello to you), gesture `hand_on_chest`, dialect `iraqi`.
- Hospitality: In Iraq, the first thing a guest gets is a glass of tea.
- Customs: `pour_tea`: Strong tea in small glasses (istikan). `share_bread`: Bread straight from the clay oven.
- Foods: Masgouf (Grilled river fish): Often called the national dish of Iraq.; Dolma (Stuffed vine leaves): Shared from one big plate with family and guests in Iraq.; Kleicha (Date-filled cookies): A sweet treat in Iraq, best with tea or coffee.; Tashreeb (Bread soaked in stew): Cooked for weddings, Eid and big family days in Iraq.; Samoon (Diamond-shaped bread): Daily bread on tables across Iraq.
- Animals: Water buffalo (ground), Ibis (flier), Falcon (flier)
- Street: vendors tea seller, masgouf grill; riverside tea house; kids play mheibes ring game, kairam.
- Swing trail `reed_fluff`; eats `iraq_masgouf` when idle.
- Review: native-speaker check of all 12 languages, plus a cultural check of the customs and gesture.

### Syria (`syria`)

- Greeting: "أهلين، تفضلوا" (Hello, come in), gesture `hand_on_chest`, dialect `levantine`.
- Hospitality: In Syria, no guest leaves without something sweet.
- Customs: `offer_sweets`: Damascene sweets are offered on every visit. `invite_in`: Houses open onto a courtyard with a jasmine tree.
- Foods: Shawarma (Chicken shawarma wrap): Sold hot at street stalls all over Syria.; Kibbeh (Bulgur and meat croquettes): Often called the national dish of Syria.; Fattoush (Bread salad): Shared from one big plate with family and guests in Syria.; Halawet el-jibn (Cheese rolls with cream): A sweet treat in Syria, best with tea or coffee.; Barazek (Sesame pistachio cookies): A favourite snack for the road in Syria.
- Animals: Horse (mount), Goat (ground)
- Street: vendors juice seller, bread bakery; old courtyard café; kids play marbles, hopscotch.
- Swing trail `jasmine_petals`; eats `syria_shawarma` when idle.
- Review: native-speaker check of all 12 languages, plus a cultural check of the customs and gesture.

### Lebanon (`lebanon`)

- Greeting: "أهلا وسهلا، كيفك؟" (Welcome, how are you?), gesture `cheek_kiss`, dialect `levantine`.
- Hospitality: In Lebanon, a guest is always asked to stay and eat.
- Customs: `share_bread`: Mezze is shared on many small plates. `invite_in`: Guests are always asked to stay for lunch.
- Foods: Manakish (Za'atar flatbread): A favourite breakfast in Lebanon.; Tabbouleh (Parsley bulgur salad): Shared from one big plate with family and guests in Lebanon.; Hummus (Chickpea dip): Often called the national dish of Lebanon.; Ka'ak (Sesame bread ring): Sold hot at street stalls all over Lebanon.; Maamoul (Date-filled cookies): A sweet treat in Lebanon, best with tea or coffee.
- Animals: Goat (ground), Golden eagle (flier)
- Street: vendors manakish oven, ka'ak cart; mountain café; kids play marbles, jumping rope.
- Swing trail `cedar_needles`; eats `lebanon_manakish` when idle.
- Review: native-speaker check of all 12 languages, plus a cultural check of the customs and gesture.

### Jordan (`jordan`)

- Greeting: "هلا والله، يا هلا" (Hello and welcome), gesture `hand_on_chest`, dialect `jordanian`.
- Hospitality: In Jordan, guests are welcomed with coffee and dates.
- Customs: `pour_coffee`: Bitter Bedouin coffee is poured for the guest. `share_bread`: Mansaf is eaten from one big tray.
- Foods: Mansaf (Lamb in yogurt sauce with rice): Often called the national dish of Jordan.; Maqluba (Upside-down rice): Shared from one big plate with family and guests in Jordan.; Falafel (Chickpea fritters): Sold hot at street stalls all over Jordan.; Knafeh (Cheese pastry in syrup): A sweet treat in Jordan, best with tea or coffee.; Zarb (Bedouin underground barbecue): Cooked for weddings, Eid and big family days in Jordan.
- Animals: Camel (mount), Nubian ibex (ground)
- Street: vendors falafel stall, knafeh shop; hillside café; kids play seven stones, marbles.
- Swing trail `sand_swirl`; eats `jordan_mansaf` when idle.
- Review: native-speaker check of all 12 languages, plus a cultural check of the customs and gesture.

### Egypt (`egypt`)

- Greeting: "أهلاً وسهلاً، نورتونا" (Welcome, you light up our home), gesture `handshake`, dialect `egyptian`.
- Hospitality: In Egypt, the first thing a guest gets is a glass of tea.
- Customs: `pour_tea`: Tea with mint, on the street or at home. `bargain`: Bargaining in the bazaar is half the fun.
- Foods: Koshari (Rice, lentils and pasta): Often called the national dish of Egypt.; Ful medames (Stewed fava beans): A favourite breakfast in Egypt.; Ta'ameya (Fava bean falafel): Sold hot at street stalls all over Egypt.; Molokhia (Jute leaf stew): Shared from one big plate with family and guests in Egypt.; Basbousa (Semolina syrup cake): A sweet treat in Egypt, best with tea or coffee.
- Animals: Camel (mount), Ibis (flier), Nile crocodile (hazard), Egyptian cobra (hazard)
- Street: vendors koshari cart, sugarcane juice; ahwa street café; kids play football, hopscotch.
- Swing trail `lotus_petals`; eats `egypt_koshari` when idle.
- Review: native-speaker check of all 12 languages, plus a cultural check of the customs and gesture.

### Sudan (`sudan`)

- Greeting: "مرحب بيكم" (Welcome to you), gesture `handshake`, dialect `sudanese`.
- Hospitality: In Sudan, guests are welcomed with a cup of coffee.
- Customs: `pour_coffee`: Jabana coffee with ginger, poured from a clay pot. `invite_in`: Guests always eat first.
- Foods: Kisra (Thin sorghum flatbread): Daily bread on tables across Sudan.; Ful sudani (Stewed beans with oil and cheese): A favourite breakfast in Sudan.; Mullah bamia (Okra stew): Shared from one big plate with family and guests in Sudan.; Asida (Sorghum porridge): Cooked for weddings, Eid and big family days in Sudan.; Jabana (Ginger coffee): A drink offered to guests in Sudan.
- Animals: Camel (mount), Nile crocodile (hazard)
- Street: vendors tea lady, ful seller; riverside tea stall; kids play sija stone game, football.
- Swing trail `henna_swirl`; eats `sudan_kisra` when idle.
- Review: native-speaker check of all 12 languages, plus a cultural check of the customs and gesture.

### Libya (`libya`)

- Greeting: "مرحبتين، كيف حالك؟" (Welcome twice, how are you?), gesture `hand_on_chest`, dialect `libyan`.
- Hospitality: In Libya, the first thing a guest gets is a glass of tea.
- Customs: `pour_tea`: Strong red tea with peanuts or almonds. `share_bread`: Bazeen is eaten together by hand.
- Foods: Kuskusi (Couscous with lamb): Often called the national dish of Libya.; Bazeen (Barley dough in tomato stew): Shared from one big plate with family and guests in Libya.; Sharba libiya (Tomato soup with orzo): A Ramadan iftar favourite in Libya.; Asida (Sweet dough with date syrup): A sweet treat in Libya, best with tea or coffee.; Makroudh (Date semolina cookies): A favourite snack for the road in Libya.
- Animals: Camel (mount), Gazelle (ground)
- Street: vendors bread oven, olive seller; medina tea corner; kids play seven stones, marbles.
- Swing trail `olive_leaves`; eats `libya_couscous` when idle.
- Review: native-speaker check of all 12 languages, plus a cultural check of the customs and gesture.

### Tunisia (`tunisia`)

- Greeting: "عسلامة، لاباس؟" (Hello, all good?), gesture `handshake`, dialect `tunisian`.
- Hospitality: In Tunisia, the first thing a guest gets is a glass of tea.
- Customs: `pour_tea`: Mint tea with pine nuts on top. `bargain`: Bargain gently in the medina souqs.
- Foods: Kosksi (Couscous with fish): Often called the national dish of Tunisia.; Brik (Crispy egg pastry): Sold hot at street stalls all over Tunisia.; Lablabi (Chickpea soup): A favourite breakfast in Tunisia.; Bambalouni (Sugared doughnut): A sweet treat in Tunisia, best with tea or coffee.; Makroudh (Date semolina diamonds): A favourite snack for the road in Tunisia.
- Animals: Camel (mount), Horse (mount)
- Street: vendors bambalouni fryer, brik stall; café with mint tea; kids play marbles, football.
- Swing trail `jasmine_petals`; eats `tunisia_brik` when idle.
- Review: native-speaker check of all 12 languages, plus a cultural check of the customs and gesture.

### Algeria (`algeria`)

- Greeting: "صحيت، واش راك؟" (Hello, how are you?), gesture `handshake`, dialect `algerian`.
- Hospitality: In Algeria, guests are welcomed with mint tea.
- Customs: `pour_tea`: Mint tea poured from high up. `share_bread`: Kesra bread at every meal.
- Foods: Kesksou (Couscous with vegetables): Often called the national dish of Algeria.; Chakhchoukha (Torn bread in stew): Cooked for weddings, Eid and big family days in Algeria.; Kesra (Semolina flatbread): Daily bread on tables across Algeria.; Chorba frik (Green wheat soup): A Ramadan iftar favourite in Algeria.; Makroud (Date semolina cookies): A sweet treat in Algeria, best with tea or coffee.
- Animals: Camel (mount), Barbary macaque (ground), Fennec fox (ground)
- Street: vendors kesra baker, makroud seller; Casbah café; kids play marbles, hopscotch.
- Swing trail `olive_leaves`; eats `algeria_couscous` when idle.
- Review: native-speaker check of all 12 languages, plus a cultural check of the customs and gesture.

### Morocco (`morocco`)

- Greeting: "السلام، لاباس عليك؟" (Peace, are you well?), gesture `hand_on_chest`, dialect `darija`.
- Hospitality: In Morocco, guests are welcomed with mint tea.
- Customs: `pour_tea`: Atay is poured three times from high up. `bargain`: Bargaining is a friendly chat in the souq.
- Foods: Tajine (Slow-cooked stew in a clay pot): Often called the national dish of Morocco.; Seksu (Friday couscous): Shared from one big plate with family and guests in Morocco.; Harira (Tomato lentil soup): A Ramadan iftar favourite in Morocco.; Msemen (Square flaky pancake): A favourite breakfast in Morocco.; Chebakia (Sesame honey flower cookies): A sweet treat in Morocco, best with tea or coffee.
- Animals: Camel (mount), Barbary macaque (ground), White stork (flier)
- Street: vendors msemen seller, harira stall; terrace mint tea; kids play marbles, football.
- Swing trail `zellige_shards`; eats `morocco_tagine` when idle.
- Review: native-speaker check of all 12 languages, plus a cultural check of the customs and gesture.

### Mauritania (`mauritania`)

- Greeting: "Salam aleykum" (Peace be upon you), gesture `hand_on_chest`, dialect `hassaniya`.
- Hospitality: In Mauritania, the first thing a guest gets is a glass of tea.
- Customs: `pour_tea`: Three glasses of tea, each sweeter than the last. `invite_in`: Guests are welcomed into the tent first.
- Foods: Thieboudienne (Fish and rice): Often called the national dish of Mauritania.; Méchoui (Slow-roasted lamb): Cooked for weddings, Eid and big family days in Mauritania.; Couscous (Couscous): Shared from one big plate with family and guests in Mauritania.; Zrig (Sweet camel-milk drink): A drink offered to guests in Mauritania.; Tmar (Dates): A favourite snack for the road in Mauritania.
- Animals: Camel (mount), Fennec fox (ground), Gazelle (ground)
- Street: vendors tea seller, dates seller; tea corner; kids play dune sliding, stone game.
- Swing trail `sand_swirl`; eats `mauritania_thieboudienne` when idle.
- Review: native-speaker check of all 12 languages, plus a cultural check of the customs and gesture.

### Portugal (`portugal`)

- Greeting: "Olá, bom dia" (Hello, good morning), gesture `cheek_kiss`, dialect `pt-PT`.
- Hospitality: In Portugal, guests are welcomed with a cup of coffee.
- Customs: `offer_sweets`: A custard tart with coffee at the corner café. `wave_hello`: Neighbours greet each other from tiled balconies.
- Foods: Pastel de nata (Custard tart): A sweet treat in Portugal, best with tea or coffee.; Sardinhas assadas (Grilled sardines): From the sea and coast of Portugal.; Bacalhau à Brás (Salt-cod with egg and potato): Often called the national dish of Portugal.; Caldo verde (Kale and potato soup): Sold hot at street stalls all over Portugal.; Bolo de arroz (Rice-flour muffin): A favourite breakfast in Portugal.
- Animals: Rooster (ground), Horse (mount), Seagull (flier)
- Street: vendors chestnut roaster, pastry shop; pastelaria; kids play hopscotch, street football.
- Swing trail `azulejo_shards`; eats `portugal_pastel_nata` when idle.
- Review: native-speaker check of all 12 languages, plus a cultural check of the customs and gesture.

### Spain (`spain`)

- Greeting: "¡Hola, buenos días!" (Hello, good morning!), gesture `cheek_kiss`, dialect `es-ES`.
- Hospitality: In Spain, a guest is always asked to stay and eat.
- Customs: `share_bread`: Small plates are shared around the table. `play_game`: Kids play football in the plaza until late.
- Foods: Churros con chocolate (Churros with hot chocolate): A sweet treat in Spain, best with tea or coffee.; Tortilla de patatas (Potato omelette): Often called the national dish of Spain.; Paella de mariscos (Seafood rice pan): Shared from one big plate with family and guests in Spain.; Gazpacho (Cold tomato soup): Sold hot at street stalls all over Spain.; Turrón (Almond nougat): A sweet treat in Spain, best with tea or coffee.
- Animals: Bull (ground), Horse (mount), Stork (flier)
- Street: vendors churros stand, orange seller; café with terrace; kids play football, skipping rope.
- Swing trail `orange_blossom`; eats `spain_churros` when idle.
- Review: native-speaker check of all 12 languages, plus a cultural check of the customs and gesture.

### France (`france`)

- Greeting: "Bonjour !" (Good morning!), gesture `handshake`, dialect `fr`.
- Hospitality: In France, no guest leaves without something sweet.
- Customs: `share_bread`: A fresh baguette under the arm every morning. `wave_hello`: Everyone says bonjour when they walk into a shop.
- Foods: Croissant (Croissant): A favourite breakfast in France.; Crêpe (Crêpe): Sold hot at street stalls all over France.; Ratatouille (Stewed summer vegetables): Shared from one big plate with family and guests in France.; Baguette (Baguette): Daily bread on tables across France.; Macaron (Almond macaron): A sweet treat in France, best with tea or coffee.
- Animals: Rooster (ground), Snail (ground), Horse (mount)
- Street: vendors crêpe stand, baker; café terrace; kids play pétanque, hopscotch.
- Swing trail `lavender`; eats `france_croissant` when idle.
- Review: native-speaker check of all 12 languages, plus a cultural check of the customs and gesture.

### United Kingdom (`uk`)

- Greeting: "Hello, all right?" (Hello, how are you?), gesture `handshake`, dialect `en-GB`.
- Hospitality: In the United Kingdom, the first thing a guest gets is a glass of tea.
- Customs: `pour_tea`: A cup of tea fixes everything. `wave_hello`: People queue politely and say sorry a lot.
- Foods: Fish and chips (Fish and chips): Often called the national dish of the United Kingdom.; Scones with cream (Scones with jam and cream): A sweet treat in the United Kingdom, best with tea or coffee.; Cottage pie (Beef and mash pie): Shared from one big plate with family and guests in the United Kingdom.; Crumpets (Crumpets): A favourite breakfast in the United Kingdom.; Trifle (Fruit and custard trifle): A sweet treat in the United Kingdom, best with tea or coffee.
- Animals: Sheep (ground), Fox (ground), Robin (flier)
- Street: vendors fish and chips shop, bakery; tea room; kids play football, conkers.
- Swing trail `drizzle`; eats `uk_fish_chips` when idle.
- Review: native-speaker check of all 12 languages, plus a cultural check of the customs and gesture.

### Germany (`germany`)

- Greeting: "Guten Tag!" (Good day!), gesture `handshake`, dialect `de`.
- Hospitality: In Germany, no guest leaves without something sweet.
- Customs: `share_bread`: Bread comes in hundreds of kinds. `offer_sweets`: Afternoon cake and coffee with the family.
- Foods: Brezel (Pretzel): Daily bread on tables across Germany.; Apfelstrudel (Apple strudel): A sweet treat in Germany, best with tea or coffee.; Käsespätzle (Cheese noodles): Often called the national dish of Germany.; Kartoffelsalat (Potato salad): Shared from one big plate with family and guests in Germany.; Lebkuchen (Honey spice cake): A sweet treat in Germany, best with tea or coffee.
- Animals: Deer (ground), Stork (flier), Dachshund (pointer)
- Street: vendors pretzel stand, bakery; bakery café; kids play football, hide and seek.
- Swing trail `autumn_leaves`; eats `germany_pretzel` when idle.
- Review: native-speaker check of all 12 languages, plus a cultural check of the customs and gesture.

### Türkiye (`turkey`)

- Greeting: "Merhaba, hoş geldiniz!" (Hello, welcome!), gesture `hand_to_heart_shake`, dialect `tr`.
- Hospitality: In Türkiye, the first thing a guest gets is a glass of tea.
- Customs: `pour_tea`: Tulip-shaped glasses of tea all day long. `offer_sweets`: Guests get Turkish delight with their tea.
- Foods: Simit (Sesame bread ring): Sold hot at street stalls all over Türkiye.; Baklava (Pistachio baklava): A sweet treat in Türkiye, best with tea or coffee.; Adana kebap (Spiced grilled kebab): Often called the national dish of Türkiye.; Menemen (Eggs with tomato and pepper): A favourite breakfast in Türkiye.; Lokum (Turkish delight): A sweet treat in Türkiye, best with tea or coffee.
- Animals: Cat (ground), Stork (flier), Anatolian shepherd dog (pointer), Brown bear (hazard)
- Street: vendors simit cart, roasted corn; tea garden; kids play marbles, hopscotch.
- Swing trail `tulip_petals`; eats `turkey_simit` when idle.
- Review: native-speaker check of all 12 languages, plus a cultural check of the customs and gesture.

### Uzbekistan (`uzbekistan`)

- Greeting: "Assalomu alaykum" (Peace be upon you), gesture `hand_on_chest`, dialect `uz`.
- Hospitality: In Uzbekistan, the first thing a guest gets is a glass of tea.
- Customs: `share_bread`: Round non bread is never put upside down. `pour_tea`: Tea is poured back into the pot three times.
- Foods: Osh (plov) (Rice pilaf with carrot): Often called the national dish of Uzbekistan.; Samsa (Baked meat pastry): Sold hot at street stalls all over Uzbekistan.; Non (Round flatbread): Daily bread on tables across Uzbekistan.; Lagman (Hand-pulled noodle soup): Shared from one big plate with family and guests in Uzbekistan.; Halva (Sesame halva): A sweet treat in Uzbekistan, best with tea or coffee.
- Animals: Horse (mount), Camel (mount), Sheep (ground)
- Street: vendors non bread oven, melon seller; chaikhana teahouse; kids play kite flying, knuckle bones.
- Swing trail `suzani_glints`; eats `uzbekistan_plov` when idle.
- Review: native-speaker check of all 12 languages, plus a cultural check of the customs and gesture.

### Pakistan (`pakistan`)

- Greeting: "Assalam o alaikum" (Peace be upon you), gesture `hand_on_chest`, dialect `ur`.
- Hospitality: In Pakistan, guests are welcomed with a cup of milky chai.
- Customs: `pour_tea`: Doodh patti chai for every guest. `invite_in`: A guest is a blessing, so stay for dinner.
- Foods: Biryani (Spiced rice with chicken): Often called the national dish of Pakistan.; Nihari (Slow-cooked beef stew): A favourite breakfast in Pakistan.; Chapli kebab (Flat spiced kebab): Sold hot at street stalls all over Pakistan.; Jalebi (Syrup spirals): A sweet treat in Pakistan, best with tea or coffee.; Lassi (Yogurt drink): A drink offered to guests in Pakistan.
- Animals: Markhor (ground), Camel (mount), Water buffalo (ground)
- Street: vendors chaat stall, jalebi fryer; dhaba; kids play cricket, kite flying.
- Swing trail `truck_art_sparkles`; eats `pakistan_biryani` when idle.
- Review: native-speaker check of all 12 languages, plus a cultural check of the customs and gesture.

### India (`india`)

- Greeting: "Namaste" (Hello), gesture `namaste`, dialect `hi`.
- Hospitality: In India, guests are welcomed with a cup of milky chai.
- Customs: `pour_tea`: Masala chai from a street-side kettle. `offer_sweets`: Sweets are shared for every bit of good news.
- Foods: Samosa (Samosa): Sold hot at street stalls all over India.; Masala dosa (Crispy rice crêpe): A favourite breakfast in India.; Butter chicken (Butter chicken): Often called the national dish of India.; Gulab jamun (Syrup milk balls): A sweet treat in India, best with tea or coffee.; Thali (A platter of many small dishes): Shared from one big plate with family and guests in India.
- Animals: Elephant (ground), Peacock (ground), Cow (ground), Bengal tiger (hazard), Langur (ground), Indian cobra (hazard)
- Street: vendors chai wallah, samosa stall; chai stall; kids play cricket, kho-kho.
- Swing trail `marigold_petals`; eats `india_samosa` when idle.
- Review: native-speaker check of all 12 languages, plus a cultural check of the customs and gesture.

### Malaysia (`malaysia`)

- Greeting: "Assalamualaikum, selamat datang" (Peace be upon you, welcome), gesture `hand_to_heart_shake`, dialect `ms`.
- Hospitality: In Malaysia, the first thing a guest gets is a glass of tea.
- Customs: `pour_tea`: Teh tarik is pulled high between two cups. `invite_in`: On Hari Raya every house is open to visitors.
- Foods: Nasi lemak (Coconut rice with sambal): Often called the national dish of Malaysia.; Satay (Grilled skewers with peanut sauce): Sold hot at street stalls all over Malaysia.; Roti canai (Flaky flatbread): A favourite breakfast in Malaysia.; Teh tarik (Pulled milk tea): A drink offered to guests in Malaysia.; Kuih lapis (Layered sweet cake): A sweet treat in Malaysia, best with tea or coffee.
- Animals: Hornbill (flier), Orangutan (ground), Tapir (ground)
- Street: vendors satay grill, teh tarik stall; mamak stall; kids play sepak takraw, congkak.
- Swing trail `bougainvillea`; eats `malaysia_nasi_lemak` when idle.
- Review: native-speaker check of all 12 languages, plus a cultural check of the customs and gesture.

### Indonesia (`indonesia`)

- Greeting: "Assalamualaikum, apa kabar?" (Peace be upon you, how are you?), gesture `hand_on_chest`, dialect `id`.
- Hospitality: In Indonesia, no guest leaves without something sweet.
- Customs: `share_bread`: Families eat together on mats at home. `invite_in`: Guests are offered tea and snacks right away.
- Foods: Nasi goreng (Fried rice): Often called the national dish of Indonesia.; Rendang (Slow-cooked spiced beef): Cooked for weddings, Eid and big family days in Indonesia.; Sate ayam (Chicken skewers): Sold hot at street stalls all over Indonesia.; Martabak manis (Thick sweet pancake): A sweet treat in Indonesia, best with tea or coffee.; Es cendol (Coconut and palm-sugar drink): A drink offered to guests in Indonesia.
- Animals: Komodo dragon (hazard), Orangutan (ground), Water buffalo (ground)
- Street: vendors bakso cart, martabak stall; warung; kids play kite flying, gasing tops.
- Swing trail `batik_glints`; eats `indonesia_nasi_goreng` when idle.
- Review: native-speaker check of all 12 languages, plus a cultural check of the customs and gesture.

### Japan (`japan`)

- Greeting: "Konnichiwa" (Hello), gesture `bow_nod`, dialect `ja`.
- Hospitality: In Japan, the first thing a guest gets is a glass of tea.
- Customs: `pour_tea`: Green tea is served with both hands. `invite_in`: Shoes come off at the door when you go in.
- Foods: Onigiri (Rice ball): A favourite snack for the road in Japan.; Chicken shio ramen (Chicken noodle soup): Sold hot at street stalls all over Japan.; Salmon nigiri (Salmon nigiri): From the sea and coast of Japan.; Taiyaki (Fish-shaped bean cake): A sweet treat in Japan, best with tea or coffee.; Mochi (Rice cake): A sweet treat in Japan, best with tea or coffee.
- Animals: Deer (ground), Snow monkey (ground), Koi (ground), Asian black bear (hazard)
- Street: vendors taiyaki stall, dango stall; tea house; kids play kendama, daruma-san ga koronda.
- Swing trail `cherry_petals`; eats `japan_onigiri` when idle.
- Review: native-speaker check of all 12 languages, plus a cultural check of the customs and gesture.

### United States (`usa`)

- Greeting: "Hi there, howdy!" (Hi there!), gesture `wave`, dialect `en-US`.
- Hospitality: In the USA, a guest is always asked to stay and eat.
- Customs: `share_bread`: Big family dinners where everyone brings a dish. `wave_hello`: Neighbours wave from their porches.
- Foods: Halal beef burger (Beef burger): Often called the national dish of the USA.; Pancakes with maple syrup (Pancakes with maple syrup): A favourite breakfast in the USA.; Mac and cheese (Mac and cheese): Shared from one big plate with family and guests in the USA.; Cornbread (Cornbread): Daily bread on tables across USA.; Apple pie (Apple pie): A sweet treat in the USA, best with tea or coffee.
- Animals: Bald eagle (flier), Bison (ground), Horse (mount), Brown bear (hazard)
- Street: vendors pretzel cart, lemonade stand; diner; kids play baseball, basketball.
- Swing trail `stars`; eats `usa_burger` when idle.
- Review: native-speaker check of all 12 languages, plus a cultural check of the customs and gesture.

### Mexico (`mexico`)

- Greeting: "¡Hola! ¿Qué tal?" (Hello! How are you?), gesture `handshake`, dialect `es-MX`.
- Hospitality: In Mexico, a guest is always asked to stay and eat.
- Customs: `share_bread`: Fresh tortillas come to every table, warm in a cloth. `play_game`: Kids break a piñata at birthday parties.
- Foods: Tacos de pollo (Chicken tacos): Sold hot at street stalls all over Mexico.; Guacamole (Avocado dip): A favourite snack for the road in Mexico.; Quesadilla (Cheese-filled tortilla): A favourite breakfast in Mexico.; Elote (Grilled corn on the cob): Sold hot at street stalls all over Mexico.; Churros con cajeta (Churros with caramel): A sweet treat in Mexico, best with tea or coffee.
- Animals: Axolotl (ground), Horse (mount), Monarch butterfly (flier)
- Street: vendors elote cart, agua fresca stand; paletería ice-pop shop; kids play piñata, lotería.
- Swing trail `papel_picado`; eats `mexico_tacos` when idle.
- Review: native-speaker check of all 12 languages, plus a cultural check of the customs and gesture.

### Brazil (`brazil`)

- Greeting: "Oi, tudo bem?" (Hi, all good?), gesture `cheek_kiss`, dialect `pt-BR`.
- Hospitality: In Brazil, guests are welcomed with a cup of coffee.
- Customs: `share_bread`: Pão de queijo with a little coffee, any time. `play_game`: Football on every beach and street.
- Foods: Pão de queijo (Cheese bread balls): A favourite snack for the road in Brazil.; Feijoada de carne (Black bean and beef stew): Often called the national dish of Brazil.; Açaí na tigela (Açaí berry bowl): Sold hot at street stalls all over Brazil.; Coxinha (Chicken croquette): Sold hot at street stalls all over Brazil.; Brigadeiro (Chocolate fudge balls): A sweet treat in Brazil, best with tea or coffee.
- Animals: Toucan (flier), Macaw (flier), Jaguar (hazard), Green anaconda (hazard), Sloth (ground)
- Street: vendors coconut water stand, pastel fryer; padaria; kids play football, kite flying.
- Swing trail `carnival_sparks`; eats `brazil_pao_de_queijo` when idle.
- Review: native-speaker check of all 12 languages, plus a cultural check of the customs and gesture.

### South Africa (`south_africa`)

- Greeting: "Sawubona! Howzit?" (Hello! How are you?), gesture `handshake`, dialect `en-ZA`.
- Hospitality: In South Africa, a guest gets invited to the family braai.
- Customs: `share_bread`: Weekends mean a braai with the whole family. `play_game`: Kids play street football and rugby.
- Foods: Bunny chow (Bread bowl of curry): Sold hot at street stalls all over South Africa.; Bobotie (Spiced baked mince): Often called the national dish of South Africa.; Halal boerewors (Grilled spiced sausage): Cooked for weddings, Eid and big family days in South Africa.; Koeksister (Syrup-glazed plait): A sweet treat in South Africa, best with tea or coffee.; Pap en sous (Maize porridge with tomato relish): Shared from one big plate with family and guests in South Africa.
- Animals: Giraffe (mount), Elephant (ground), Springbok (ground), Lion (hazard), Zebra (ground), Rhinoceros (hazard)
- Street: vendors bunny chow shop, mielie grill; township café; kids play football, diketo.
- Swing trail `savanna_dust`; eats `south_africa_bunny_chow` when idle.
- Review: native-speaker check of all 12 languages, plus a cultural check of the customs and gesture.

### Australia (`australia`)

- Greeting: "G'day!" (Hello), gesture `wave`, dialect `en-AU`.
- Hospitality: In Australia, the first thing a guest gets is a glass of tea.
- Customs: `share_bread`: Weekend barbecues with neighbours and family. `wave_hello`: People greet each other with a friendly g'day.
- Foods: Lamington (Chocolate and coconut sponge cube): A sweet treat in Australia, best with tea or coffee.; Pavlova (Meringue dessert with fruit): A sweet treat in Australia, best with tea or coffee.; Anzac biscuit (Oat and coconut biscuit): A favourite snack for the road in Australia.; Halal meat pie (Beef pie with gravy): Sold hot at street stalls all over Australia.; Grilled barramundi (Grilled barramundi fillet): From the sea and coast of Australia.
- Animals: Kangaroo (ground), Koala (ground), Emu (ground), Saltwater crocodile (hazard)
- Street: vendors lamington stall, meat pie van; seaside cafe; kids play beach cricket, footy.
- Swing trail `red_dust`; eats `australia_lamington` when idle.
- Review: native-speaker check of all 12 languages, plus a cultural check of the customs and gesture.

