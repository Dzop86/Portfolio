# Writes the dungeons (sprint 80, D65): one per family of monsters, its door on the family's boss map,
# its three rooms on battle maps of their own (data/maps/dungeon-<family>-<room>.json), its key and the
# operator's keyring in data/items.json, the list in data/dungeons.json; since sprint 81, the key among
# the loot of the family's monsters (rarely) and its recipe for the locksmith in data/jobs.json, made of
# the family's two resources at a level that follows the zone's; since sprint 86, its three halls
# (data/towns/<dungeon>-<room>.json: stone walls, a fountain, a keeper, the room's group, the far door
# and the way out) and its keep, 3 x 3 cells over the door on the chief's map (stamped into that map,
# so run after tools/world_gen.py). Deterministic: the same data
# gives the same files; every room is checked connected, with room for the hero and five monsters; since
# sprint 94, the second room on the drawing of the family's second battle map.
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


def keep_cells(d):
    """The 3 x 3 cells of the keep over a door, its top-left corner first."""
    return [(d[0] - 1 + dx, d[1] - 3 + dy) for dy in range(3) for dx in range(3)]


def stamped(rows, d):
    """The rows with the keep over door d: 'K' on its top-left corner, 'k' on the rest."""
    g = [list(r) for r in rows]
    for i, (x, y) in enumerate(keep_cells(d)):
        g[y][x] = 'K' if i == 0 else 'k'
    return [''.join(r) for r in g]


def door(town):
    """
    A cell of the map north of its centre, reachable, away from its ways, exits and arrival, with the
    keep's 3 x 3 cells of grass over it and a cell to stand on under it; the keep must cut nothing.
    """
    rows = town["rows"]
    spawn = (town["spawn"]["x"], town["spawn"]["y"])
    links = [(l["at"]["x"], l["at"]["y"]) for l in town["links"]]
    away = [spawn] + links + [(e["at"]["x"], e["at"]["y"]) for e in town["exits"]]
    hex_distance = lambda a, b: abs(a[0] - b[0]) + abs(a[1] - b[1])
    goal = (spawn[0], spawn[1] - 7)
    reach = reachable(rows, spawn)
    cells = sorted((c for c in reach if all(hex_distance(c, a) >= DISTANCE + 1 for a in away)), key=lambda c: (hex_distance(c, goal), c[1], c[0]))
    for c in cells:
        under = (c[0], c[1] + 1)
        keep = keep_cells(c)
        if under not in reach or any(k not in reach or any(hex_distance(k, a) < DISTANCE for a in away) for k in keep):
            continue
        after = reachable(stamped(rows, c), spawn)
        if all(m in after for m in links + [c, under]):
            return c
    raise SystemExit(f"no door for {town['id']}")


# A hall: 20 x 14 cells of stone, walled; the way out at the bottom, the far door at the top (none in the
# last), a fountain on the left, the keeper on the right, the group in the middle.
HW, HH = 20, 14
HALL_GROUP = (10, 5)
HALL_SPAWN = (10, 11)


def hall(hid, did, name, room, level, entrance, outside):
    g = [['R' if x in (0, HW - 1) or y in (0, HH - 1) else '=' for x in range(HW)] for y in range(HH)]
    for (x, y) in ((5, 3), (14, 3), (5, 9), (14, 9)):
        g[y][x] = 'R'
    for (x, y), ch in (((2, 6), 'F'), ((3, 6), 'f'), ((2, 7), 'f'), ((3, 7), 'f')):
        g[y][x] = ch
    out = [(x, HH - 1) for x in (9, 10, 11)]
    up = [(x, 0) for x in (9, 10, 11)] if room < 3 else []
    for (x, y) in out + up:
        g[y][x] = '='
    links = [O([("at", O([("x", x), ("y", y)])), ("to", entrance), ("arrival", O([("x", outside[0]), ("y", outside[1])])), ("name", text("Sortie", "Way out"))]) for (x, y) in out]
    links += [O([("at", O([("x", x), ("y", y)])), ("to", f"{did}-{room + 1}"), ("arrival", O([("x", x), ("y", HH - 2)])), ("name", text(f"Salle {room + 1}", f"Room {room + 1}"))]) for (x, y) in up]
    return O([("id", hid), ("dungeon", did), ("name", O([("fr", f"{name['fr']}, salle {room}"), ("en", f"{name['en']}, room {room}")])), ("level", level),
              ("rows", [''.join(r) for r in g]), ("spawn", O([("x", HALL_SPAWN[0]), ("y", HALL_SPAWN[1])])),
              ("npcs", [O([("id", "keeper"), ("name", text("Gardienne", "Keeper")), ("look", "female-e"), ("colour", 2), ("at", O([("x", 16), ("y", 7)])), ("dialogue", "dungeon-keeper")])]),
              ("exits", []), ("links", links)])


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
        # Since sprint 94 (D69 §5.3), the great hall is fought on the drawing of the family's second battle map.
        rows = json.load(open(f"{root}/maps/{f['battles'][1]}.json"))["rows"] if r == 2 else room(800 + n * 10 + r, r)
        m = O([("id", mid), ("name", O([("fr", f"{name['fr']} : {ROOMS[r - 1]['fr'].lower()}"), ("en", f"{name['en']}: {ROOMS[r - 1]['en']}")])), ("rows", rows)])
        open(f"{root}/maps/{mid}.json", "w").write(json.dumps(m, ensure_ascii=False, indent=2) + "\n")
    town = json.load(open(f"{root}/towns/{f['bossMap']}.json"))
    # The keep of the last run comes off first: the door is chosen on the map as world_gen wrote it.
    town["rows"] = [r.replace('K', '.').replace('k', '.') for r in town["rows"]]
    dx, dy = door(town)
    town["rows"] = stamped(town["rows"], (dx, dy))
    open(f"{root}/towns/{f['bossMap']}.json", "w").write(json.dumps(town, ensure_ascii=False, indent=2) + "\n")
    halls = []
    for r in range(1, 4):
        hid = f"{did}-{r}"
        open(f"{root}/towns/{hid}.json", "w").write(json.dumps(hall(hid, did, name, r, town["level"], f["bossMap"], (dx, dy + 1)), ensure_ascii=False, indent=2) + "\n")
        halls.append(O([("map", hid), ("group", O([("x", HALL_GROUP[0]), ("y", HALL_GROUP[1])]))]))
    dungeons.append(O([("id", did), ("name", name), ("family", f["id"]), ("map", f["bossMap"]), ("door", O([("x", dx), ("y", dy)])),
                       ("key", f"{did}-key"), ("rooms", rooms), ("halls", halls)]))
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
    # Since sprint 97 (D71), a key also costs 5 Éclats a level of the dungeon's chief.
    chief = next(m for m in f["monsters"] if m["role"] == "Boss")
    recipes.append(O([("item", d["key"]), ("level", RECIPE_LEVEL[f["zone"]]),
                      ("ingredients", [O([("item", resources[0]), ("count", 6)]), O([("item", resources[1]), ("count", 4)])]),
                      ("shards", 5 * chief["level"])]))
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
