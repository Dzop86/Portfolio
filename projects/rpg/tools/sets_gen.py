# Writes the families' sets (sprints 95 and 96, D71, docs/suite-apres-d69.md §3): five pieces a family, one
# left by each of its four common monsters (8 %), the weapon by its chief (30 %, and the other pieces
# at 8 % each); the pieces in data/items.json (the family items of before keep their ids), the sets
# and their bonuses in data/sets.json, the drops in data/monsters.json. A piece spends about 8 + its
# level in characteristic points, a point of initiative costing 0.6 of them; initiative only on boots,
# capes and rings. Since sprint 132 the Forest of the Ancestors' families have four pieces: one by each common monster
# but the support, and the chief's weapon (docs/familles-foret.md §2.4). Deterministic and idempotent. Run from
# projects/rpg after tools/dungeon_gen.py:
#   python3 tools/sets_gen.py data
import collections, json, sys

root = sys.argv[1]
O = collections.OrderedDict
PIECE_PERCENT, WEAPON_PERCENT = 8, 30
# The characteristics of the pieces and of the bonuses: half the design's budget (8 + level), after T41's ceilings
# measured with the "fast" kit (sprint 95, T63); the initiative stays whole.
SCALE = 0.5


def scaled(stats, two_handed=False):
    """A two-handed weapon counts as two pieces (sprint 109, D76): twice the budget of one."""
    return O((k, max(1, round(v * SCALE * (2 if two_handed else 1)))) for k, v in stats.items())


def text(fr, en): return O([("fr", fr), ("en", en)])


def piece(id, fr, en, slot, level, monster, stats, initiative=0):
    return dict(id=id, name=text(fr, en), slot=slot, level=level, monster=monster, stats=stats, initiative=initiative)


# family: (set name, bonuses for 2, 3 and all its pieces, pieces); a bonus is (stats, ap, mp, initiative), the best one worn counts.
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
    # Sprint 96: the Lake and the Fields; the frogs and the bees are their quick sets.
    ("frog", (text("Panoplie des joncs", "Reed set"),
              [({}, 0, 0, 15), ({"water": 15}, 0, 0, 15), ({"water": 15}, 0, 1, 15)],
              [piece("frog-boots", "Bottes palmées", "Webbed Boots", "Boots", 17, "brawler-frog", {"water": 7, "air": 6}, 20),
               piece("croak-amulet", "Amulette du coassement", "Croaking Amulet", "Amulet", 16, "spitting-frog", {"water": 14, "air": 10}),
               piece("lily-ring", "Anneau de nénuphar", "Lily Ring", "Ring", 18, "bullfrog", {"water": 8, "air": 6}, 20),
               piece("dew-hat", "Chapeau de rosée", "Dew Hat", "Hat", 17, "healing-treefrog", {"water": 15, "vitality": 10}),
               piece("toad-sceptre", "Sceptre du crapaud-roi", "Toad King's Sceptre", "OneHanded", 22, "toad-king", {"water": 18, "air": 12})])),
    ("crab", (text("Panoplie du récif", "Reef set"),
              [({"vitality": 15}, 0, 0, 0), ({"vitality": 15, "earth": 15}, 0, 0, 0), ({"vitality": 45, "earth": 15}, 0, 0, 0)],
              [piece("claw-belt", "Ceinture de pinces", "Claw Belt", "Belt", 17, "pincer-crab", {"earth": 13, "vitality": 12}),
               piece("bubble-ring", "Anneau de bulle", "Bubble Ring", "Ring", 16, "bubble-crab", {"earth": 12, "vitality": 12}),
               piece("shell-shield", "Bouclier de carapace", "Shell Shield", "Shield", 20, "armoured-crab", {"earth": 14, "vitality": 14}),
               piece("hermit-cape", "Cape de bernard", "Hermit's Cape", "Cape", 17, "hermit-crab", {"earth": 12, "vitality": 13}),
               piece("figurehead-axe", "Hache de proue", "Figurehead Axe", "TwoHanded", 22, "giant-crab", {"earth": 18, "vitality": 12})])),
    ("bee", (text("Panoplie de la ruche", "Hive set"),
             [({}, 0, 0, 15), ({"air": 15}, 0, 0, 15), ({"air": 15}, 1, 0, 15)],
             [piece("bee-boots", "Bottes d'ouvrière", "Worker's Boots", "Boots", 25, "worker-bee", {"air": 10, "fire": 8}, 25),
              piece("wing-cape", "Cape d'ailes", "Wing Cape", "Cape", 24, "archer-bee", {"air": 10, "fire": 7}, 25),
              piece("wax-ring", "Anneau de cire", "Wax Ring", "Ring", 26, "drone", {"fire": 11, "air": 8}, 25),
              piece("jelly-amulet", "Amulette de gelée royale", "Royal Jelly Amulet", "Amulet", 25, "nurse-bee", {"air": 18, "vitality": 15}),
              piece("stinger-dagger", "Dague-dard", "Stinger Dagger", "OneHanded", 26, "queen-bee", {"air": 20, "fire": 14})])),
    ("mole", (text("Panoplie des galeries", "Burrow set"),
              [({"earth": 15}, 0, 0, 0), ({"earth": 15, "vitality": 15}, 0, 0, 0), ({"earth": 15, "vitality": 15}, 0, 1, 0)],
              [piece("mole-hat", "Bonnet de taupe", "Mole Cap", "Hat", 25, "digger-mole", {"earth": 18, "vitality": 15}),
               piece("clod-ring", "Anneau de motte", "Clod Ring", "Ring", 24, "clod-mole", {"earth": 17, "vitality": 15}),
               piece("digger-boots", "Bottes de fouisseur", "Digger Boots", "Boots", 28, "old-mole", {"earth": 20, "vitality": 16}),
               piece("burrow-belt", "Ceinture de terrier", "Burrow Belt", "Belt", 25, "mole-mother", {"earth": 18, "vitality": 15}),
               piece("debris-pick", "Pioche du débris", "Debris Pick", "TwoHanded", 30, "mole-king", {"earth": 24, "vitality": 14})])),
    # Sprint 132: the Forest of the Ancestors' sylvan stags, four pieces (docs/familles-foret.md §3.5); twice the values worn, the two-handed weapon once.
    ("stag", (text("Panoplie du sylvecerf", "Sylvan Stag set"),
              [({"earth": 20}, 0, 0, 0), ({"earth": 20, "vitality": 20}, 0, 0, 0), ({"earth": 20, "vitality": 30}, 0, 1, 0)],
              [piece("antler-circlet", "Couronne d'andouillers", "Antler Circlet", "Hat", 32, "brocket", {"earth": 22, "vitality": 18}),
               piece("hind-cape", "Cape en peau de biche", "Hindskin Cape", "Cape", 31, "carving-hind", {"earth": 18, "vitality": 16}, 10),
               piece("stonehoof-boots", "Bottes à sabots de pierre", "Stonehoof Boots", "Boots", 34, "old-hart", {"earth": 20, "vitality": 16}, 10),
               piece("elder-antler-staff", "Bâton de ramure", "Antler Staff", "TwoHanded", 45, "elder-stag", {"earth": 34, "vitality": 20})])),
])
# Clairval's families have five pieces, the forest's four.
PIECES = {"stag": 4}


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
    count = PIECES.get(family, 5)
    assert len(pieces) == count and len(bonuses) == 3, family
    sets.append(O([("id", family), ("name", name),
                   ("bonuses", [O([("pieces", n), ("stats", scaled(stats))] + ([("ap", ap)] if ap else []) + ([("mp", mp)] if mp else [])
                                  + ([("initiative", ini)] if ini else []))
                                for n, (stats, ap, mp, ini) in zip((2, 3, count), bonuses)])]))
    for p in pieces:
        item = O([("id", p["id"]), ("name", p["name"]), ("kind", "Equipment"), ("slot", p["slot"]), ("level", p["level"]), ("stats", scaled(p["stats"], p["slot"] == "TwoHanded"))])
        if p["initiative"]:
            item["initiative"] = p["initiative"]
        item["set"] = family
        at = next((i for i, x in enumerate(items) if x["id"] == p["id"]), None)
        if at is None:
            # New pieces go before the dungeons' keys, which tools/dungeon_gen.py writes last.
            at = next(i for i, x in enumerate(items) if x["id"].endswith("-key") or x["id"] == "admin-keyring")
            items.insert(at, item)
        else:
            # Its drawing kept (tools/icons_gen.py, sprint 132: it was lost until that script ran again).
            items[at] = item | ({"icon": items[at]["icon"]} if "icon" in items[at] else {})
    # The drops: each common monster its piece, the chief its weapon and the others; before the dungeon's key.
    ids = {p["id"] for p in pieces}
    chief = next(m for m in families[family]["monsters"] if m["role"] == "Boss")
    weapon = next(p for p in pieces if chief["id"] == p["monster"])
    for m in families[family]["monsters"]:
        loot = [l for l in m.get("loot", []) if l["item"] not in ids]
        drops = ([O([("item", weapon["id"]), ("percent", WEAPON_PERCENT)])] + [O([("item", p["id"]), ("percent", PIECE_PERCENT)]) for p in pieces if p is not weapon]
                 if m["role"] == "Boss" else [O([("item", p["id"]), ("percent", PIECE_PERCENT)]) for p in pieces if p["monster"] == m["id"]])
        # A forest family's support leaves no piece (docs/familles-foret.md §2.4).
        assert drops or (count == 4 and m["role"] == "Support"), m["id"]
        # Before the quests' items and the dungeon's key (sprint 132: after the quests' items, they moved); at the end without either.
        key = next((i for i, l in enumerate(loot) if "quest" in l or l["item"].endswith("-key")), len(loot))
        m["loot"] = loot[:key] + drops + loot[key:]

with open(f"{root}/items.json", "w") as out:
    out.write("[\n" + ",\n".join("  " + inline(i) for i in items) + "\n]\n")
open(f"{root}/sets.json", "w").write(json.dumps(sets, ensure_ascii=False, indent=2) + "\n")
open(f"{root}/monsters.json", "w").write(json.dumps(list(families.values()), ensure_ascii=False, indent=2) + "\n")
print(f"{len(SETS)} family sets, {sum(len(p) for _, _, p in SETS.values())} pieces")
