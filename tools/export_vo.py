"""Writes docs/vo_lines.csv: every line that needs a recording, with its character count for budgeting.

Run from the project root: python3 tools/export_vo.py
"""
import csv, json, glob, os

RES = "Assets/_Aron/Resources"
rows = []

def add(line_id, speaker, voice, lang, text, source):
    if text and text.strip():
        rows.append(dict(id=line_id, speaker=speaker, voice=voice, lang=lang, text=text.strip(), chars=len(text.strip()), source=source))

for l in json.load(open(f"{RES}/Audio/VoiceLines.json", encoding="utf-8"))["lines"]:
    add(l["id"], l["character"], l["character"], "ar", l["textAr"], "Audio/VoiceLines.json")

regions = sorted((json.load(open(f, encoding="utf-8")) for f in glob.glob(f"{RES}/Regions/*.json") if not f.endswith("RegionCatalog.json")), key=lambda r: r["order"])
for r in regions:
    src = f"Regions/{r['id']}.json"
    for s in r["stages"]:
        add(f"vo_stage_{s['id']}", "hero", "aron", "ar", s.get("lineAr", ""), src)
    b = r.get("boss") or {}
    voice = f"boss_{r['id']}"
    for k in ("introAr", "stunAr", "defeatAr"):
        add(f"vo_boss_{r['id']}_{k[:-2]}", b.get("nameEn", ""), voice, "ar", b.get(k, ""), src)
    for i, p in enumerate(b.get("phases", [])):
        add(f"vo_boss_{r['id']}_taunt{i + 1}", b.get("nameEn", ""), voice, "ar", p.get("tauntAr", ""), src)

for f in sorted(glob.glob(f"{RES}/Culture/*.json")):
    c = json.load(open(f, encoding="utf-8"))
    g = c["greeting"]
    add(f"vo_greet_{c['country']}", "local host", f"host_{c['country']}", c["uiLanguage"] if c["uiLanguage"] != "ar" else "ar", g["words"], f"Culture/{c['country']}.json")

# The 35 recurring rivals: 8 short barks each, spoken in the country's own language or dialect (drafts, native review needed).
for rv in json.load(open(f"{RES}/Rivals/rivals.json", encoding="utf-8"))["rivals"]:
    for l in rv["lines"]:
        tag = ("ar-" + l["dialect"]) if l["lang"] == "ar" else l["dialect"]
        add(l["id"], rv["nameEn"], f"rival_{rv['country']}", tag, l["text"], "Rivals/rivals.json")

os.makedirs("docs", exist_ok=True)
with open("docs/vo_lines.csv", "w", newline="", encoding="utf-8-sig") as out:
    w = csv.DictWriter(out, fieldnames=["id", "speaker", "voice", "lang", "text", "chars", "source"], lineterminator="\n")
    w.writeheader()
    w.writerows(rows)

total = sum(r["chars"] for r in rows)
voices = {}
for r in rows: voices[r["voice"]] = voices.get(r["voice"], 0) + r["chars"]
print(f"{len(rows)} lines, {total} characters, {len(voices)} voices")
for v in ("aron", "noura", "any"): print(v, voices.get(v, 0))
print("bosses", sum(n for v, n in voices.items() if v.startswith("boss_")))
print("rivals", sum(n for v, n in voices.items() if v.startswith("rival_")))
print("hosts", sum(n for v, n in voices.items() if v.startswith("host_")))
