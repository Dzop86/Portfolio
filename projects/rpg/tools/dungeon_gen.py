# Writes the dungeons (sprint 80, D65): one per family of monsters, its door on the family's boss map,
# its three rooms on battle maps of their own (data/maps/dungeon-<family>-<room>.json), its key and the
# operator's keyring in data/items.json, the list in data/dungeons.json; since sprint 81, the key among
# the loot of the family's monsters (rarely) and its recipe for the locksmith in data/jobs.json, made of
# the family's two resources at a level that follows the zone's. Deterministic: the same data
# gives the same files; every room is checked connected, with room for the hero and five monsters.
# Run from projects/rpg after tools/world_gen.py:  python3 tools/dungeon_gen.py data
import collections, json, random, sys

root = sys.argv[1]
O = collections.OrderedDict
SIZE = 11
DISTANCE = 3  # MonsterGroups.Distance: a door stays that far from the ways, the exits and the arrival.


def text(fr, en): return O([("fr", fr), ("en", en)])


# family: (dungeon id, name, key name)
DUNGEONS = O([
    ("boar", ("boar-wallow", text("Bauge du Roi sanglier", "The Boar King's Wallow"), text("Clef de la bauge", "Wallow Key"))),
    ("wolf", ("wolf-den", text("Tanière de la meute", "The Pack's Den"), text("Clef de la tanière", "Den Key"))),
    ("mushroom", ("mushroom-cellar", text("Champignonnière", "The Mushroom Cellar"), text("Clef de la champignonnière", "Cellar Key"))),
    ("zombie", ("zombie-crypt", text("Crypte oubliée", "The Forgotten Crypt"), text("Clef de la crypte", "Crypt Key"))),
    ("frog", ("frog-grotto", text("Grotte du Roi grenouille", "The Frog King's Grotto"), text("Clef de la grotte", "Grotto Key"))),
    ("crab", ("crab-reef", text("Récif des pinces", "The Pincer Reef"), text("Clef du récif", "Reef Key"))),
    ("bee", ("royal-hive", text("Ruche royale", "The Royal Hive"), text("Clef de la ruche", "Hive Key"))),
    ("mole", ("mole-burrows", text("Galeries des taupes", "The Mole Burrows"), text("Clef des galeries", "Burrow Key"))),
])
ROOMS = (text("Antichambre", "Antechamber"), text("Grande salle", "Great Hall"), text("Salle du chef", "Chief's Hall"))


def connected(rows):
    cells = {(x, y) for y, r in enumerate(rows) for x, c in enumerate(r) if c != '#'}
    start = next(iter(cells))
    seen, todo = {start}, [start]
    while todo:
        x, y = todo.pop()
        for n in ((x + 1, y), (x - 1, y), (x, y + 1), (x, y - 1)):
            if n in cells and n not in seen:
                seen.add(n)
                todo.append(n)
    return seen == cells


def room(seed, number):
    """A room of 11 x 11: pillars, team A's four starts on the west, team B's six on the east."""
    for attempt in range(100):
        rnd = random.Random(seed * 100 + attempt)
        g = [['.'] * SIZE for _ in range(SIZE)]
        # Pillars in pairs, mirrored north-south: a hall rather than a wood.
        for _ in range(3 + number):
            x, y = rnd.randrange(3, SIZE - 3), rnd.randrange(1, SIZE // 2)
            g[y][x] = g[SIZE - 1 - y][x] = '#'
        for (x, y) in ((1, 2), (0, 5), (1, 8), (2, 5)):
            g[y][x] = 'A'
        for (x, y) in ((9, 1), (10, 3), (8, 5), (10, 5), (10, 7), (9, 9)):
            g[y][x] = 'B'
        rows = [''.join(r) for r in g]
        if connected(rows):
            return rows
    raise SystemExit(f"no room found for seed {seed}")


def reachable(rows, start):
    floor = lambda x, y: 0 <= y < len(rows) and 0 <= x < len(rows[0]) and rows[y][x] in '.=,'
    seen, todo = {start}, [start]
    while todo:
        x, y = todo.pop()
        for n in ((x + 1, y), (x - 1, y), (x, y + 1), (x, y - 1)):
            if n not in seen and floor(*n):
                seen.add(n)
                todo.append(n)
    return seen


def door(town):
    """A cell of the map north of its centre, reachable, away from its ways, exits and arrival."""
    spawn = (town["spawn"]["x"], town["spawn"]["y"])
    away = [spawn] + [(l["at"]["x"], l["at"]["y"]) for l in town["links"]] + [(e["at"]["x"], e["at"]["y"]) for e in town["exits"]]
    hex_distance = lambda a, b: abs(a[0] - b[0]) + abs(a[1] - b[1])
    goal = (spawn[0], spawn[1] - 7)
    cells = [c for c in reachable(town["rows"], spawn) if all(hex_distance(c, a) >= DISTANCE + 1 for a in away)]
    return min(cells, key=lambda c: (hex_distance(c, goal), c[1], c[0]))


families = json.load(open(f"{root}/monsters.json"))
items = json.load(open(f"{root}/items.json"))
items = [i for i in items if not i["id"].endswith("-key") and i["id"] != "admin-keyring"]
dungeons = []
for n, f in enumerate(families):
    did, name, key_name = DUNGEONS[f["id"]]
    rooms = []
    for r in range(1, 4):
        mid = f"dungeon-{f['id']}-{r}"
        rooms.append(mid)
        m = O([("id", mid), ("name", O([("fr", f"{name['fr']} : {ROOMS[r - 1]['fr'].lower()}"), ("en", f"{name['en']}: {ROOMS[r - 1]['en']}")])), ("rows", room(800 + n * 10 + r, r))])
        open(f"{root}/maps/{mid}.json", "w").write(json.dumps(m, ensure_ascii=False, indent=2) + "\n")
    town = json.load(open(f"{root}/towns/{f['bossMap']}.json"))
    dx, dy = door(town)
    dungeons.append(O([("id", did), ("name", name), ("family", f["id"]), ("map", f["bossMap"]), ("door", O([("x", dx), ("y", dy)])),
                       ("key", f"{did}-key"), ("rooms", rooms)]))
    items.append(O([("id", f"{did}-key"), ("name", key_name), ("kind", "Resource")]))
items.append(O([("id", "admin-keyring"), ("name", text("Trousseau d'administrateur", "Operator's Keyring")), ("kind", "Quest")]))
# The key, rarely left by the family's monsters (sprint 81): one in fifty, one in ten for the chief.
KEY_PERCENT, CHIEF_KEY_PERCENT = 2, 10
# The locksmith's recipes: the key of each dungeon, from the family's two resources.
RECIPE_LEVEL = {"woods": 1, "mouldy": 10, "lake": 20, "fields": 30}
kinds = {i["id"]: i["kind"] for i in items}
recipes = []
for f, d in zip(families, dungeons):
    for m in f["monsters"]:
        m["loot"] = [l for l in m.get("loot", []) if not l["item"].endswith("-key")]
        m["loot"].append(O([("item", d["key"]), ("percent", CHIEF_KEY_PERCENT if m["role"] == "Boss" else KEY_PERCENT)]))
    resources = list(O.fromkeys(l["item"] for m in f["monsters"] for l in m["loot"] if kinds.get(l["item"]) == "Resource"))
    recipes.append(O([("item", d["key"]), ("level", RECIPE_LEVEL[f["zone"]]),
                      ("ingredients", [O([("item", resources[0]), ("count", 6)]), O([("item", resources[1]), ("count", 4)])])]))
open(f"{root}/monsters.json", "w").write(json.dumps(families, ensure_ascii=False, indent=2) + "\n")
jobs = [O([("id", "locksmith"), ("name", text("Serrurier", "Locksmith")), ("workshop", text("Établi du serrurier", "Locksmith's bench")), ("recipes", recipes)])]
open(f"{root}/jobs.json", "w").write(json.dumps(jobs, ensure_ascii=False, indent=2) + "\n")
open(f"{root}/dungeons.json", "w").write(json.dumps(dungeons, ensure_ascii=False, indent=2) + "\n")


def inline(v):
    """One item per line, as the file was written by hand: { "id": "x", "name": { "fr": "...", "en": "..." } }."""
    if isinstance(v, dict):
        return "{ " + ", ".join(f"{json.dumps(k)}: {inline(x)}" for k, x in v.items()) + " }"
    if isinstance(v, list):
        return "[" + ", ".join(inline(x) for x in v) + "]"
    return json.dumps(v, ensure_ascii=False)


with open(f"{root}/items.json", "w") as out:
    out.write("[\n" + ",\n".join("  " + inline(i) for i in items) + "\n]\n")
print(f"{len(dungeons)} dungeons, {3 * len(dungeons)} rooms written")
