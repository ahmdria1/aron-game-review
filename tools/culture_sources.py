"""Writes docs/culture_sources.md from the culture, race and animal files.

Run from the project root: python3 tools/culture_sources.py
"""
import json, glob

RES = "Assets/_Aron/Resources"
regions = [json.load(open(f, encoding="utf-8")) for f in glob.glob(f"{RES}/Regions/*.json") if not f.endswith("RegionCatalog.json")]
order = {r["id"]: r["order"] for r in regions}
first = {}
for r in sorted(regions, key=lambda r: r["order"]): first.setdefault(r["country"], r)
tracks = json.load(open(f"{RES}/Races/tracks.json", encoding="utf-8"))["tracks"]
animals = {a["id"]: a for a in json.load(open(f"{RES}/Companions/local_animals.json", encoding="utf-8"))["animals"]}
cultures = sorted((json.load(open(f, encoding="utf-8")) for f in glob.glob(f"{RES}/Culture/*.json")), key=lambda c: first[c["country"]]["order"])

L = ["# Culture sources and review notes", "",
     "Each country's culture file (`Assets/_Aron/Resources/Culture/<country>.json`) holds the greeting, hospitality line, customs, palette, street scene, foods, local animals, the swing trail and the eating idle. Each country's race is in `Assets/_Aron/Resources/Races/tracks.json`.", "",
     "These are general, well-known traditions taken from public sources such as tourism boards, national museums and encyclopaedia entries. No expert has checked them. **Before release, every country needs a review by a native speaker or someone from that culture**, especially the lines in all 12 languages.", "",
     "## Food rules", "",
     "- Every food is meant to be halal. `CultureRules` rejects pork, alcohol and similar words in all 12 languages.",
     "- Where the best-known version of a dish is not halal, the game uses a halal version and names it that way: chicken shio ramen (Japan), halal beef burger (USA), halal boerewors (South Africa). The fact line never says the halal version is the traditional one.",
     "- Dishes that are often made with lard, alcohol or pork stock were swapped out (for example tamales became quesadillas, a yakitori grill became a dango stall, Turkish cologne was dropped).",
     "- Whether a dish is halal depends on how it is made. The game does not certify anything. Mark as **needs religious/legal review** if store text ever says \"halal\".",
     "- Prices and buffs are game values only.", "",
     "## Races", "",
     "| Country | Region | Race | Ride | Tradition |", "|---|---|---|---|---|"]
for t in sorted(tracks, key=lambda t: order[t["region"]]):
    L.append(f"| {t['id'][:-len(t['kind']) - 1]} | {t['region']} | {t['nameEn']} | {t['kind']} on {t['surface']} | {t['tradition']} |")
L += ["## Australia: review notes", "",
      "- Country 35. The stages are original shapes: red plains, eucalyptus forest, coral coast, a harbour city (generic shell-roof hall and steel arch, no names), rainforest.",
      "- The skyline is `red_rock_ridges`, a generic layered red sandstone range that does not resemble any single real rock. A sacred rock with image restrictions was removed on review, and `uluru` and `red_monolith` are on the forbidden-landmark list. Nothing is climbed or named. Mark as **needs lawyer / cultural advisers** if any real place is ever added.",
      "- No national day is included on purpose: 26 January is contested.",
      "- No Indigenous art, stories, names or instruments. The museum artifact is an opal, not a boomerang.",
      "- The palette uses green and gold as plain colours, with no flag or emblem.",
      "- The world map is a stage list, not a geographic map, so Australia is only a latitude and longitude in `Resources/Weather/weather.json`.",
      "- Review needed: the 12-language lines, the rival (a woman, modest outfit), and the wildlife (hazards only affect the hero).", "",
      "## Museum artifacts", "",
      "One artifact per country in `Resources/Museum/artifacts.json` (35). Names are in 12 languages; the one-line facts are in Arabic and English only, so other languages show the English fact. Deliberately avoided: flags, seals, weapons, religious objects and Indigenous sacred objects. **Every name, fact and object choice needs a check by someone from that culture.**", ""]
L += ["", "## Per country", ""]
for c in cultures:
    r = first[c["country"]]
    name = "Saudi Arabia" if c["country"] == "saudi" else r["nameEn"]
    g = c["greeting"]
    L += [f"### {name} (`{c['country']}`)", "",
          f"- Greeting: \"{g['words']}\" ({g['wordsEn']}), gesture `{g['gesture']}`, dialect `{c['dialect']}`.",
          f"- Hospitality: {c['hospitality']['en']}",
          "- Customs: " + " ".join(f"`{x['act']}`: {x['en']}" for x in c["customs"]),
          "- Foods: " + "; ".join(f"{f['local']} ({f['name']['en']}): {f['fact']['en']}" for f in c["foods"]),
          "- Animals: " + ", ".join(f"{animals[a]['nameEn']} ({animals[a]['role']})" for a in c["animals"]),
          f"- Street: vendors {', '.join(c['street']['vendors'])}; {c['street']['cafe']}; kids play {', '.join(c['street']['kidsGames'])}.",
          f"- Swing trail `{c['attackTrailVfx']}`; eats `{c['eatIdle']['dishId']}` when idle.",
          "- Review: native-speaker check of all 12 languages, plus a cultural check of the customs and gesture.", ""]
open("docs/culture_sources.md", "w", encoding="utf-8").write("\n".join(L) + "\n")
print(len(cultures), "countries")
