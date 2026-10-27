"""Writes Assets/_Aron/Resources/Weather/weather.json.

Weights are rough chances (out of 100) of each sky during a play session, by season, per country, with month and
region overrides where the climate has a strong local pattern (Japan's tsuyu in June, the Indian monsoon, Asir fog,
Tabuk winter snow). They are a game simplification of climate normals, not a forecast.

Order of each weight list: clear, clouds, haze, rain, thunder, fog, sandstorm, snow.
Run: python3 tools/weather_table.py
"""
import json
import os

K = ["clear", "clouds", "haze", "rain", "thunder", "fog", "sandstorm", "snow"]
OUT = os.path.join(os.path.dirname(__file__), "..", "Assets", "_Aron", "Resources", "Weather", "weather.json")


def w(*v):
    assert len(v) == 8, v
    assert not (v[4] > 0 and v[3] == 0), ("thunder without rain", v)
    return dict(zip(K, [float(x) for x in v]))


def seasons(winter=None, spring=None, summer=None, autumn=None):
    out = []
    for name, v in (("winter", winter), ("spring", spring), ("summer", summer), ("autumn", autumn)):
        if v is not None:
            out.append({"season": name, "w": w(*v)})
    return out


def months(**m):
    return [{"month": int(k[1:]), "w": w(*v)} for k, v in sorted(m.items(), key=lambda kv: int(kv[0][1:]))]


def region(rid, lat, lon, s=None, m=None, water="", river="", wind=-1.0):
    return {"region": rid, "latitude": lat, "longitude": lon, "seasons": s or [], "months": m or [],
            "water": water, "riverWater": river, "wind": wind}


def country(cid, lat, lon, utc, climate, s, water, river, wind=0.3, m=None, regions=None, source=""):
    return {"country": cid, "hemisphere": "south" if lat < 0 else "north", "climate": climate, "latitude": lat,
            "longitude": lon, "utcOffset": utc, "seasons": s, "months": m or [], "regions": regions or [],
            "water": water, "riverWater": river, "wind": wind, "source": source}


NORMALS = "Climate normals (WMO 1991-2020 summaries as published by national met services), simplified for play"

WATER = [
    {"id": "red_sea", "kind": "sea", "shallow": "#3FC9CF", "deep": "#0A4A6E", "foam": "#F4F8F6", "waveHeight": 0.35, "waveLength": 9, "flow": 0, "clarity": 0.9},
    {"id": "gulf", "kind": "sea", "shallow": "#6CC9BC", "deep": "#0D5868", "foam": "#F2F5EE", "waveHeight": 0.25, "waveLength": 7, "flow": 0, "clarity": 0.6},
    {"id": "arabian_sea", "kind": "sea", "shallow": "#3AA9B2", "deep": "#0B3E5E", "foam": "#F2F5F2", "waveHeight": 0.6, "waveLength": 12, "flow": 0, "clarity": 0.65},
    {"id": "mediterranean", "kind": "sea", "shallow": "#4FB6DB", "deep": "#0A3D78", "foam": "#F5F8FA", "waveHeight": 0.4, "waveLength": 10, "flow": 0, "clarity": 0.85},
    {"id": "atlantic", "kind": "sea", "shallow": "#4A93AA", "deep": "#0B2F4A", "foam": "#F0F3F3", "waveHeight": 0.9, "waveLength": 18, "flow": 0, "clarity": 0.5},
    {"id": "atlantic_tropical", "kind": "sea", "shallow": "#45BAC6", "deep": "#0B4A70", "foam": "#F4F7F6", "waveHeight": 0.7, "waveLength": 14, "flow": 0, "clarity": 0.7},
    {"id": "atlantic_cold", "kind": "sea", "shallow": "#3E848F", "deep": "#0A2A38", "foam": "#EEF2F2", "waveHeight": 1.0, "waveLength": 20, "flow": 0, "clarity": 0.55},
    {"id": "north_sea", "kind": "sea", "shallow": "#5E8283", "deep": "#1A3238", "foam": "#E8ECEB", "waveHeight": 0.8, "waveLength": 14, "flow": 0, "clarity": 0.35},
    {"id": "pacific", "kind": "sea", "shallow": "#3CA0BC", "deep": "#0A3355", "foam": "#F2F5F6", "waveHeight": 0.9, "waveLength": 18, "flow": 0, "clarity": 0.65},
    {"id": "tropical_sea", "kind": "sea", "shallow": "#3CCEC6", "deep": "#0A5478", "foam": "#F6F9F7", "waveHeight": 0.4, "waveLength": 10, "flow": 0, "clarity": 0.85},
    {"id": "coral_sea", "kind": "sea", "shallow": "#35D2CC", "deep": "#0A5C86", "foam": "#F6F9F8", "waveHeight": 0.55, "waveLength": 13, "flow": 0, "clarity": 0.9},
    {"id": "nile", "kind": "river", "shallow": "#6E8F6A", "deep": "#2E4A3A", "foam": "#E6EBE2", "waveHeight": 0.08, "waveLength": 3, "flow": 0.8, "clarity": 0.25},
    {"id": "amazon", "kind": "river", "shallow": "#A07E50", "deep": "#4A3420", "foam": "#E9E2D2", "waveHeight": 0.1, "waveLength": 4, "flow": 1.4, "clarity": 0.08},
    {"id": "iraq_marshes", "kind": "marsh", "shallow": "#6F9166", "deep": "#2F4A34", "foam": "#E4EADF", "waveHeight": 0.03, "waveLength": 2, "flow": 0, "clarity": 0.4},
    {"id": "river", "kind": "river", "shallow": "#5F8C8A", "deep": "#24423F", "foam": "#E8EDEA", "waveHeight": 0.06, "waveLength": 3, "flow": 0.9, "clarity": 0.35},
    {"id": "mountain_river", "kind": "river", "shallow": "#6FB5B3", "deep": "#2A5A62", "foam": "#F4F8F8", "waveHeight": 0.15, "waveLength": 2, "flow": 2.0, "clarity": 0.75},
]

# Najd and the north: hot dry summers with haze and dust, short cool winters with some rain.
NAJD = seasons((55, 20, 8, 8, 2, 2, 5, 0), (45, 15, 15, 6, 4, 0, 15, 0), (55, 3, 32, 0, 0, 0, 10, 0), (65, 8, 18, 2, 1, 0, 6, 0))

SAUDI_REGIONS = [
    region("riyadh", 24.71, 46.68),
    region("qassim", 26.33, 43.97),
    region("hail", 27.52, 41.69, seasons(winter=(50, 22, 6, 12, 2, 3, 5, 0))),
    region("jawf", 29.97, 40.21, seasons(winter=(50, 24, 6, 12, 2, 3, 3, 0))),
    region("northern", 30.98, 41.02, seasons(winter=(50, 24, 6, 12, 2, 3, 3, 0))),
    region("madinah", 24.47, 39.61),
    region("najran", 17.49, 44.13, seasons(summer=(55, 12, 20, 4, 4, 0, 5, 0))),
    # Jeddah coast: humid, hazy summers, rare winter downpours.
    region("makkah", 21.49, 39.19, seasons((60, 18, 10, 8, 2, 0, 2, 0), (55, 10, 25, 2, 2, 0, 6, 0), (50, 5, 40, 1, 1, 0, 3, 0), (60, 10, 22, 4, 2, 0, 2, 0)), water="red_sea"),
    # Dammam and Al-Ahsa: very humid Gulf summers, morning fog, shamal dust in early summer.
    region("eastern", 26.43, 50.10, seasons((55, 22, 8, 8, 2, 4, 1, 0), (45, 12, 20, 4, 3, 2, 14, 0), (42, 3, 38, 0, 0, 5, 12, 0), (60, 8, 25, 2, 1, 2, 2, 0)), water="gulf"),
    # Tabuk: Jabal al-Lawz gets snow on some winter days.
    region("tabuk", 28.38, 36.57, seasons(winter=(48, 22, 5, 8, 2, 4, 4, 7)), water="red_sea"),
    # Asir and Al-Bahah: highland fog (dabab) most of the year, summer monsoon showers. No sand to blow.
    region("asir", 18.22, 42.50, seasons((35, 25, 0, 10, 4, 26, 0, 0), (30, 25, 0, 15, 10, 20, 0, 0), (28, 30, 0, 14, 12, 16, 0, 0), (45, 20, 0, 8, 4, 23, 0, 0)), river="mountain_river", wind=0.35),
    region("bahah", 20.01, 41.47, seasons((40, 25, 0, 10, 3, 22, 0, 0), (35, 25, 0, 14, 8, 18, 0, 0), (40, 25, 5, 10, 8, 12, 0, 0), (50, 20, 0, 8, 4, 18, 0, 0)), river="mountain_river"),
    # Jazan: hot humid Tihama coast; the hills behind it get summer storms.
    region("jazan", 16.89, 42.55, seasons((60, 20, 15, 3, 1, 1, 0, 0), (50, 20, 25, 3, 2, 0, 0, 0), (40, 20, 30, 5, 5, 0, 0, 0), (55, 20, 20, 3, 2, 0, 0, 0)), water="red_sea"),
]

GULF_SUMMER = (45, 3, 40, 0, 0, 3, 9, 0)

COUNTRIES = [
    country("saudi", 24.71, 46.68, 3, "desert", NAJD, "red_sea", "river", 0.35, regions=SAUDI_REGIONS, source=NORMALS + "; NCM Saudi Arabia"),
    country("kuwait", 29.37, 47.98, 3, "desert", seasons((55, 20, 10, 8, 2, 3, 2, 0), (40, 15, 20, 6, 4, 0, 15, 0), (50, 2, 33, 0, 0, 0, 15, 0), (65, 8, 17, 3, 2, 0, 5, 0)), "gulf", "river", 0.45, source=NORMALS + "; Kuwait Met Department"),
    country("uae", 25.20, 55.27, 4, "desert", seasons((60, 18, 10, 5, 1, 4, 2, 0), (50, 10, 25, 3, 2, 2, 8, 0), GULF_SUMMER, (60, 8, 25, 1, 1, 4, 1, 0)), "gulf", "river", 0.3, source=NORMALS + "; UAE NCM"),
    country("bahrain", 26.23, 50.59, 3, "desert", seasons((55, 22, 10, 6, 2, 3, 2, 0), (45, 12, 28, 3, 2, 2, 8, 0), GULF_SUMMER, (60, 8, 26, 2, 1, 1, 2, 0)), "gulf", "river", 0.35, source=NORMALS + "; Bahrain Met Directorate"),
    country("qatar", 25.29, 51.53, 3, "desert", seasons((55, 22, 10, 6, 2, 3, 2, 0), (45, 12, 26, 3, 2, 2, 10, 0), GULF_SUMMER, (60, 8, 26, 2, 1, 1, 2, 0)), "gulf", "river", 0.35, source=NORMALS + "; Qatar Met Department"),
    country("oman", 23.59, 58.41, 4, "desert", seasons((60, 22, 8, 6, 1, 1, 2, 0), (55, 12, 22, 3, 2, 0, 6, 0), (45, 10, 30, 3, 2, 4, 6, 0), (65, 10, 20, 2, 1, 0, 2, 0)), "arabian_sea", "mountain_river", 0.35,
            m=months(m7=(30, 30, 15, 12, 0, 10, 3, 0), m8=(30, 30, 15, 12, 0, 10, 3, 0)), source=NORMALS + "; Oman Met (khareef in Dhofar Jul-Aug)"),
    country("yemen", 15.37, 44.19, 3, "highland", seasons((65, 18, 8, 3, 1, 3, 2, 0), (45, 20, 10, 12, 8, 2, 3, 0), (40, 22, 12, 12, 10, 1, 3, 0), (70, 12, 10, 4, 2, 1, 1, 0)), "red_sea", "mountain_river", 0.3, source=NORMALS),
    country("iraq", 33.31, 44.37, 3, "desert", seasons((45, 25, 5, 15, 3, 7, 0, 0), (40, 15, 15, 8, 5, 0, 17, 0), (55, 2, 25, 0, 0, 0, 18, 0), (60, 10, 18, 4, 2, 1, 5, 0)), "gulf", "iraq_marshes", 0.4, source=NORMALS + "; Iraqi Met Organization"),
    country("syria", 33.51, 36.29, 3, "semi_arid", seasons((35, 30, 3, 20, 3, 5, 0, 4), (50, 20, 8, 10, 4, 1, 7, 0), (75, 5, 15, 0, 0, 0, 5, 0), (60, 18, 10, 7, 3, 0, 2, 0)), "mediterranean", "river", 0.3, source=NORMALS),
    country("lebanon", 33.89, 35.50, 3, "mediterranean", seasons((30, 25, 0, 28, 7, 5, 0, 5), (50, 25, 5, 12, 3, 5, 0, 0), (80, 8, 10, 0, 0, 2, 0, 0), (55, 20, 8, 12, 4, 1, 0, 0)), "mediterranean", "mountain_river", 0.3, source=NORMALS),
    country("jordan", 31.95, 35.93, 3, "semi_arid", seasons((40, 28, 3, 18, 3, 4, 0, 4), (50, 18, 10, 6, 3, 1, 12, 0), (75, 3, 17, 0, 0, 0, 5, 0), (65, 15, 10, 5, 2, 0, 3, 0)), "red_sea", "river", 0.3, source=NORMALS + "; Jordan Met Department"),
    country("egypt", 30.04, 31.24, 2, "desert", seasons((65, 20, 8, 3, 1, 3, 0, 0), (50, 10, 20, 2, 1, 2, 15, 0), (70, 2, 25, 0, 0, 1, 2, 0), (70, 8, 18, 1, 1, 1, 1, 0)), "mediterranean", "nile", 0.3, source=NORMALS + "; EMA (khamsin in spring)"),
    country("sudan", 15.50, 32.56, 2, "desert", seasons((70, 5, 20, 0, 0, 0, 5, 0), (55, 5, 25, 0, 0, 0, 15, 0), (30, 20, 15, 12, 8, 0, 15, 0), (55, 15, 20, 5, 3, 0, 2, 0)), "red_sea", "nile", 0.4, source=NORMALS + " (haboobs in the summer rains)"),
    country("libya", 32.89, 13.19, 2, "desert", seasons((45, 25, 5, 15, 3, 3, 4, 0), (50, 15, 15, 5, 2, 1, 12, 0), (70, 5, 20, 0, 0, 1, 4, 0), (60, 15, 12, 7, 3, 1, 2, 0)), "mediterranean", "river", 0.4, source=NORMALS + " (ghibli in spring)"),
    country("tunisia", 36.81, 10.18, 1, "mediterranean", seasons((35, 28, 2, 25, 4, 4, 2, 0), (50, 22, 8, 10, 3, 2, 5, 0), (75, 5, 15, 1, 1, 1, 2, 0), (55, 20, 8, 10, 4, 1, 2, 0)), "mediterranean", "river", 0.35, source=NORMALS),
    country("algeria", 36.75, 3.06, 1, "mediterranean", seasons((35, 25, 2, 25, 4, 4, 2, 3), (50, 22, 8, 10, 3, 2, 5, 0), (75, 5, 14, 1, 1, 1, 3, 0), (55, 20, 8, 10, 4, 1, 2, 0)), "mediterranean", "river", 0.35, source=NORMALS),
    country("morocco", 34.02, -6.83, 1, "mediterranean", seasons((40, 25, 2, 20, 3, 5, 2, 3), (50, 22, 8, 10, 2, 4, 4, 0), (70, 5, 15, 1, 0, 6, 3, 0), (55, 20, 8, 10, 3, 3, 1, 0)), "atlantic", "mountain_river", 0.35, source=NORMALS),
    country("mauritania", 18.08, -15.98, 0, "desert", seasons((55, 10, 25, 0, 0, 2, 8, 0), (45, 8, 30, 0, 0, 2, 15, 0), (40, 15, 25, 8, 5, 0, 7, 0), (55, 12, 22, 5, 3, 1, 2, 0)), "atlantic", "river", 0.45, source=NORMALS + " (harmattan dust)"),
    country("portugal", 38.72, -9.14, 0, "mediterranean", seasons((30, 30, 0, 30, 3, 6, 0, 1), (45, 25, 3, 18, 3, 6, 0, 0), (75, 10, 8, 2, 1, 4, 0, 0), (45, 25, 3, 20, 3, 4, 0, 0)), "atlantic", "river", 0.4, source=NORMALS + "; IPMA"),
    country("spain", 40.42, -3.70, 1, "mediterranean", seasons((40, 28, 2, 20, 2, 6, 0, 2), (50, 25, 5, 14, 4, 2, 0, 0), (75, 5, 12, 2, 3, 0, 3, 0), (50, 25, 5, 15, 4, 1, 0, 0)), "mediterranean", "river", 0.3, source=NORMALS + "; AEMET (calima in summer)"),
    country("france", 48.86, 2.35, 1, "oceanic", seasons((20, 35, 0, 30, 1, 10, 0, 4), (35, 30, 2, 25, 4, 4, 0, 0), (55, 20, 5, 12, 6, 2, 0, 0), (25, 30, 2, 30, 3, 10, 0, 0)), "atlantic", "river", 0.35, source=NORMALS + "; Meteo-France"),
    country("uk", 51.51, -0.13, 0, "oceanic", seasons((15, 35, 0, 35, 1, 10, 0, 4), (25, 35, 0, 30, 2, 8, 0, 0), (40, 30, 2, 22, 4, 2, 0, 0), (20, 35, 0, 33, 2, 10, 0, 0)), "north_sea", "river", 0.45, source=NORMALS + "; Met Office"),
    country("germany", 52.52, 13.40, 1, "temperate", seasons((15, 40, 0, 20, 1, 14, 0, 10), (35, 30, 2, 25, 4, 4, 0, 0), (45, 25, 3, 15, 10, 2, 0, 0), (25, 35, 2, 25, 2, 11, 0, 0)), "north_sea", "river", 0.35, source=NORMALS + "; DWD"),
    country("turkey", 41.01, 28.98, 3, "mediterranean", seasons((25, 30, 2, 25, 3, 7, 0, 8), (45, 25, 4, 18, 4, 4, 0, 0), (75, 10, 10, 2, 2, 1, 0, 0), (45, 25, 5, 18, 4, 3, 0, 0)), "mediterranean", "river", 0.4, source=NORMALS + "; MGM"),
    country("uzbekistan", 39.65, 66.96, 5, "continental", seasons((40, 30, 5, 10, 1, 6, 0, 8), (45, 22, 5, 15, 5, 2, 6, 0), (80, 3, 10, 1, 0, 0, 6, 0), (65, 15, 8, 6, 2, 2, 2, 0)), "river", "river", 0.3, source=NORMALS + "; Uzhydromet (Samarkand)"),
    country("pakistan", 33.69, 73.05, 5, "semi_arid", seasons((50, 20, 10, 6, 2, 10, 0, 2), (55, 10, 15, 6, 6, 0, 8, 0), (35, 15, 15, 18, 12, 0, 5, 0), (60, 12, 15, 5, 3, 5, 0, 0)), "arabian_sea", "river", 0.3,
            m=months(m7=(20, 20, 5, 35, 18, 2, 0, 0), m8=(20, 20, 5, 35, 18, 2, 0, 0)), source=NORMALS + "; PMD (monsoon Jul-Aug)"),
    country("india", 28.61, 77.21, 5.5, "monsoon", seasons((55, 15, 10, 3, 1, 16, 0, 0), (55, 10, 20, 3, 4, 0, 8, 0), (30, 20, 10, 25, 15, 0, 0, 0), (55, 20, 10, 10, 4, 1, 0, 0)), "arabian_sea", "river", 0.3,
            m=months(m6=(20, 25, 5, 32, 16, 2, 0, 0), m7=(10, 25, 0, 45, 18, 2, 0, 0), m8=(10, 25, 0, 45, 18, 2, 0, 0), m9=(25, 25, 3, 32, 13, 2, 0, 0)), source=NORMALS + "; IMD (southwest monsoon Jun-Sep)"),
    country("malaysia", 3.14, 101.69, 8, "tropical", seasons(*[(30, 30, 5, 20, 12, 3, 0, 0)] * 4), "tropical_sea", "river", 0.2,
            m=months(m11=(15, 30, 0, 35, 15, 5, 0, 0), m12=(15, 30, 0, 35, 15, 5, 0, 0), m1=(20, 30, 0, 32, 13, 5, 0, 0), m8=(25, 25, 25, 12, 10, 3, 0, 0), m9=(25, 25, 25, 12, 10, 3, 0, 0)),
            source=NORMALS + "; MetMalaysia (northeast monsoon Nov-Jan, haze Aug-Sep)"),
    # Southern hemisphere: winter is June to August. Jakarta's wet season is the southern summer.
    country("indonesia", -6.21, 106.85, 7, "tropical", seasons((50, 25, 8, 10, 5, 2, 0, 0), (35, 30, 8, 15, 10, 2, 0, 0), (15, 30, 0, 35, 18, 2, 0, 0), (30, 30, 3, 22, 12, 3, 0, 0)), "tropical_sea", "river", 0.2, source=NORMALS + "; BMKG"),
    country("japan", 35.68, 139.69, 9, "subtropical", seasons((55, 25, 0, 8, 1, 3, 0, 8), (45, 30, 2, 18, 2, 3, 0, 0), (35, 25, 5, 20, 13, 2, 0, 0), (45, 25, 2, 20, 6, 2, 0, 0)), "pacific", "mountain_river", 0.3,
            m=months(m6=(10, 30, 0, 48, 4, 8, 0, 0), m7=(30, 25, 3, 25, 12, 5, 0, 0), m9=(30, 25, 0, 30, 12, 3, 0, 0)), source=NORMALS + "; JMA (tsuyu in June, typhoons in September)"),
    country("usa", 36.10, -112.11, -7, "continental", seasons((35, 30, 0, 15, 1, 6, 0, 13), (45, 25, 3, 15, 5, 2, 5, 0), (50, 15, 5, 10, 12, 1, 7, 0), (55, 22, 3, 12, 4, 4, 0, 0)), "atlantic", "river", 0.35, source=NORMALS + "; NOAA (Southwest monsoon storms and haboobs in summer)"),
    country("mexico", 19.43, -99.13, -6, "highland", seasons((70, 15, 10, 2, 0, 3, 0, 0), (60, 12, 15, 5, 5, 0, 3, 0), (25, 30, 3, 25, 17, 0, 0, 0), (45, 25, 5, 15, 8, 2, 0, 0)), "atlantic_tropical", "river", 0.3, source=NORMALS + "; SMN"),
    country("brazil", -22.91, -43.17, -3, "tropical", seasons((60, 20, 5, 10, 2, 3, 0, 0), (40, 28, 3, 18, 9, 2, 0, 0), (25, 30, 0, 28, 17, 0, 0, 0), (45, 25, 3, 18, 7, 2, 0, 0)), "atlantic_tropical", "amazon", 0.3, source=NORMALS + "; INMET"),
    country("south_africa", -26.20, 28.05, 2, "subtropical", seasons((75, 10, 8, 2, 0, 4, 0, 1), (55, 20, 10, 8, 7, 0, 0, 0), (35, 25, 0, 22, 18, 0, 0, 0), (60, 20, 5, 8, 4, 3, 0, 0)), "atlantic_cold", "river", 0.35, source=NORMALS + "; SAWS (summer storms on the Highveld)"),
    # Southern hemisphere again. One table for the whole country: a mix of the dry red interior, the humid east coast and
    # the tropical north. The wet season of the tropical north (November to April) gets more rain and thunder, the
    # interior is dry and dusty in the southern spring. Snow falls only in the Australian Alps, which no stage shows.
    country("australia", -33.87, 151.21, 10, "subtropical", seasons((60, 20, 3, 8, 0, 2, 0, 0), (55, 18, 5, 12, 4, 0, 4, 0), (45, 15, 10, 18, 8, 0, 4, 0), (55, 22, 5, 12, 3, 2, 1, 0)), "coral_sea", "river", 0.4,
            m=months(m9=(60, 12, 8, 6, 1, 0, 13, 0), m10=(58, 14, 8, 8, 2, 0, 10, 0), m11=(30, 25, 3, 28, 14, 0, 0, 0), m12=(22, 25, 3, 30, 18, 2, 0, 0), m1=(18, 25, 2, 33, 20, 2, 0, 0),
                     m2=(16, 25, 2, 36, 21, 0, 0, 0), m3=(22, 25, 2, 32, 19, 0, 0, 0), m4=(34, 25, 3, 24, 14, 0, 0, 0)),
            source=NORMALS + "; Bureau of Meteorology (tropical north wet season Nov-Apr, dry interior)"),
]


def main():
    table = {"changeMinMinutes": 8, "changeMaxMinutes": 20, "blendSeconds": 30, "countries": COUNTRIES, "water": WATER}
    os.makedirs(os.path.dirname(OUT), exist_ok=True)
    with open(OUT, "w", encoding="utf-8") as f:
        json.dump(table, f, ensure_ascii=False, indent=1)
        f.write("\n")
    print("wrote", os.path.normpath(OUT), len(COUNTRIES), "countries", len(WATER), "water profiles")


if __name__ == "__main__":
    main()
