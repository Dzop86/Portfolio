# Writes the families' sets (sprint 95, D71, docs/suite-apres-d69.md §3): five pieces a family, one
# left by each of its four common monsters (8 %), the weapon by its chief (30 %, and the other pieces
# at 8 % each); the pieces in data/items.json (the family items of before keep their ids), the sets
# and their bonuses in data/sets.json, the drops in data/monsters.json. A piece spends about 8 + its
# level in characteristic points, a point of initiative costing 0.6 of them; initiative only on boots,
# capes and rings. Deterministic and idempotent. Run from projects/rpg after tools/dungeon_gen.py:
#   python3 tools/sets_gen.py data
import collections, json, sys

root = sys.argv[1]
O = collections.OrderedDict
PIECE_PERCENT, WEAPON_PERCENT = 8, 30
# The characteristics of the pieces and of the bonuses: half the design's budget (8 + level), after T41's ceilings
# measured with the "fast" kit (sprint 95, T63); the initiative stays whole.
SCALE = 0.5


def scaled(stats): return O((k, max(1, round(v * SCALE))) for k, v in stats.items())


def text(fr, en): return O([("fr", fr), ("en", en)])


def piece(id, fr, en, slot, level, monster, stats, initiative=0):
    return dict(id=id, name=text(fr, en), slot=slot, level=level, monster=monster, stats=stats, initiative=initiative)


# family: (set name, bonuses for 2, 3 and 5 pieces, pieces); a bonus is (stats, ap, mp, initiative), the best one worn counts.
SETS = O([
    ("boar", (text("Panoplie du sanglier", "Boar set"),
              [({"vitality": 10}, 0, 0, 0), ({"vitality": 10, "earth": 10}, 0, 0, 0), ({"vitality": 25, "earth": 10}, 0, 0, 10)],
              [piece("boar-boots", "Bottes du chargeur", "Charger's Boots", "Boots", 2, "boar-charger", {"earth": 4}, 10),
               piece("acorn-ring", "Anneau de gland", "Acorn Ring", "Ring", 1, "acorn-piglet", {"vitality": 9}),
               piece("tusk-amulet", "Amulette de défenses", "Tusk Amulet", "Amulet", 3, "old-loner", {"earth": 6, "vitality": 5}),
               piece("bristle-belt", "Ceinture de soies", "Bristle Belt", "Belt", 2, "boar-sow", {"vitality": 6, "earth": 4}),
               piece("king-tusk", "Défense du Roi sanglier", "Boar King's Tusk", "TwoHanded", 6, "boar-king", {"earth": 10, "vitality": 4})])),
    ("wolf", (text("Panoplie de la meute", "Pack set"),
              [({}, 0, 0, 10), ({"air": 10}, 0, 0, 10), ({"air": 10}, 0, 1, 10)],
              [piece("wolf-boots", "Bottes de loup gris", "Grey Wolf Boots", "Boots", 2, "grey-wolf", {"air": 4}, 10),
               piece("howl-ring", "Anneau du hurleur", "Howler's Ring", "Ring", 2, "howling-wolf", {"air": 4}, 10),
               piece("wolf-cape", "Cape en peau de loup", "Wolf Pelt Cape", "Cape", 4, "great-wolf", {"air": 6}, 10),
               piece("pack-amulet", "Amulette de la meute", "Pack Amulet", "Amulet", 2, "she-wolf", {"air": 6, "vitality": 4}),
               piece("leader-fang", "Croc du chef de meute", "Pack Leader's Fang", "OneHanded", 6, "pack-leader", {"air": 10, "vitality": 4})])),
    ("mushroom", (text("Panoplie du mycélium", "Mycelium set"),
                  [({"vitality": 10}, 0, 0, 0), ({"vitality": 10, "water": 10}, 0, 0, 0), ({"vitality": 30, "water": 10}, 0, 0, 0)],
                  [piece("mycelium-belt", "Ceinture de mycélium", "Mycelium Belt", "Belt", 9, "bruiser-toadstool", {"water": 8, "vitality": 9}),
                   piece("puffball-ring", "Anneau de vesse", "Puffball Ring", "Ring", 8, "spore-puffer", {"water": 10, "vitality": 6}),
                   piece("cap-hat", "Chapeau-champignon", "Toadstool Hat", "Hat", 10, "great-cap", {"water": 10, "vitality": 8}),
                   piece("morel-amulet", "Amulette de morille", "Morel Amulet", "Amulet", 9, "healing-morel", {"water": 9, "vitality": 8}),
                   piece("elder-staff", "Bâton de l'ancien", "Elder's Staff", "TwoHanded", 13, "elder-fungus", {"water": 14, "vitality": 7})])),
    ("zombie", (text("Panoplie de la crypte", "Crypt set"),
                [({"fire": 10}, 0, 0, 0), ({"fire": 10, "vitality": 10}, 0, 0, 0), ({"fire": 20, "vitality": 10, "earth": 10, "water": 10, "air": 10}, 0, 0, 0)],
                [piece("stitched-hat", "Bonnet recousu", "Stitched Cap", "Hat", 9, "shambling-brute", {"fire": 9, "vitality": 8}),
                 piece("bile-ring", "Anneau de bile", "Bile Ring", "Ring", 8, "bile-spitter", {"fire": 7}, 15),
                 piece("grave-belt", "Ceinture de tombe", "Grave Belt", "Belt", 12, "bloated-zombie", {"fire": 10, "vitality": 8}),
                 piece("bone-cape", "Cape d'os", "Bone Cape", "Cape", 9, "bonesetter", {"fire": 9, "vitality": 8}),
                 piece("overlord-cleaver", "Fendoir du seigneur", "Overlord's Cleaver", "OneHanded", 13, "zombie-overlord", {"fire": 14, "vitality": 7})])),
])
PIECES = 5


def inline(v):
    """One item per line, as tools/dungeon_gen.py writes the file."""
    if isinstance(v, dict):
        return "{ " + ", ".join(f"{json.dumps(k)}: {inline(x)}" for k, x in v.items()) + " }"
    if isinstance(v, list):
        return "[" + ", ".join(inline(x) for x in v) + "]"
    return json.dumps(v, ensure_ascii=False)


items = json.load(open(f"{root}/items.json"))
sets = [s for s in json.load(open(f"{root}/sets.json")) if s["id"] not in SETS]
families = {f["id"]: f for f in json.load(open(f"{root}/monsters.json"))}
for family, (name, bonuses, pieces) in SETS.items():
    assert len(pieces) == PIECES and len(bonuses) == 3, family
    sets.append(O([("id", family), ("name", name),
                   ("bonuses", [O([("pieces", n), ("stats", scaled(stats))] + ([("ap", ap)] if ap else []) + ([("mp", mp)] if mp else [])
                                  + ([("initiative", ini)] if ini else []))
                                for n, (stats, ap, mp, ini) in zip((2, 3, PIECES), bonuses)])]))
    for p in pieces:
        item = O([("id", p["id"]), ("name", p["name"]), ("kind", "Equipment"), ("slot", p["slot"]), ("level", p["level"]), ("stats", scaled(p["stats"]))])
        if p["initiative"]:
            item["initiative"] = p["initiative"]
        item["set"] = family
        at = next((i for i, x in enumerate(items) if x["id"] == p["id"]), None)
        if at is None:
            # New pieces go before the dungeons' keys, which tools/dungeon_gen.py writes last.
            at = next(i for i, x in enumerate(items) if x["id"].endswith("-key") or x["id"] == "admin-keyring")
            items.insert(at, item)
        else:
            items[at] = item
    # The drops: each common monster its piece, the chief its weapon and the others; before the dungeon's key.
    ids = {p["id"] for p in pieces}
    weapon = next(p for p in pieces if families[family]["monsters"][4]["id"] == p["monster"])
    for m in families[family]["monsters"]:
        loot = [l for l in m.get("loot", []) if l["item"] not in ids]
        drops = ([O([("item", weapon["id"]), ("percent", WEAPON_PERCENT)])] + [O([("item", p["id"]), ("percent", PIECE_PERCENT)]) for p in pieces if p is not weapon]
                 if m["role"] == "Boss" else [O([("item", p["id"]), ("percent", PIECE_PERCENT)]) for p in pieces if p["monster"] == m["id"]])
        assert drops, m["id"]
        key = next(i for i, l in enumerate(loot) if l["item"].endswith("-key"))
        m["loot"] = loot[:key] + drops + loot[key:]

with open(f"{root}/items.json", "w") as out:
    out.write("[\n" + ",\n".join("  " + inline(i) for i in items) + "\n]\n")
open(f"{root}/sets.json", "w").write(json.dumps(sets, ensure_ascii=False, indent=2) + "\n")
open(f"{root}/monsters.json", "w").write(json.dumps(list(families.values()), ensure_ascii=False, indent=2) + "\n")
print(f"{len(SETS)} family sets, {PIECES * len(SETS)} pieces")
