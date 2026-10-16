# The zones' people (sprint 105, docs/suite-apres-d69.md §5 ter.4: "the givers live where the quests are done"),
# each on a map by the way in from the woods, on the free grass nearest a place of the map, reachable from its
# arrival. Their words are in data/dialogues (tools/village_dialogues.py). Run from projects/rpg after
# tools/world_gen.py and tools/dungeon_gen.py, which write the maps without anybody:
#   python3 tools/zone_people.py data
import collections, json, sys

root = sys.argv[1]
O = collections.OrderedDict


def text(fr, en): return O([("fr", fr), ("en", en)])


# id, name, look, outfit colour, map, near which cell, dialogue (docs/textes-v5.md §1.4).
PEOPLE = [
    ("mathurin", text("Mathurin", "Mathurin"), "male-d", 2, "woods-3-2", (27, 14), "mathurin"),
    ("barnabe", text("Barnabé", "Barnabé"), "male-e", 6, "mouldy-4-1", (27, 14), "barnabe"),
    ("liane", text("Liane", "Liane"), "female-a", 4, "lake-3-4", (27, 14), "liane"),
    ("eglantine", text("Églantine", "Églantine"), "female-e", 3, "fields-3-0", (27, 14), "eglantine"),
]
WALK = '.=,'


def reachable(rows, start):
    seen, todo = {start}, [start]
    while todo:
        x, y = todo.pop()
        for n in ((x + 1, y), (x - 1, y), (x, y + 1), (x, y - 1)):
            if n not in seen and 0 <= n[1] < len(rows) and 0 <= n[0] < len(rows[0]) and rows[n[1]][n[0]] in WALK:
                seen.add(n)
                todo.append(n)
    return seen


for (pid, name, look, colour, map_id, near, dialogue) in PEOPLE:
    path = f"{root}/towns/{map_id}.json"
    town = json.load(open(path), object_pairs_hook=O)
    rows = town["rows"]
    seen = reachable(rows, (town["spawn"]["x"], town["spawn"]["y"]))
    taken = {(l["at"]["x"], l["at"]["y"]) for l in town.get("links", [])} | {(town["spawn"]["x"], town["spawn"]["y"])}
    # Grass of its own, all four sides walkable, so that nobody is ever walled in by them.
    cells = [(x, y) for (x, y) in seen if rows[y][x] == '.' and (x, y) not in taken
             and all(n in seen for n in ((x + 1, y), (x - 1, y), (x, y + 1), (x, y - 1)))]
    at = min(cells, key=lambda c: (abs(c[0] - near[0]) + abs(c[1] - near[1]), c))
    npc = O([("id", pid), ("name", name), ("look", look), ("colour", colour), ("at", O([("x", at[0]), ("y", at[1])])), ("dialogue", dialogue)])
    town["npcs"] = [n for n in town["npcs"] if n["id"] != pid] + [npc]
    open(path, "w").write(json.dumps(town, ensure_ascii=False, indent=2) + "\n")
    print(f"{pid} on {map_id} at {at}")
