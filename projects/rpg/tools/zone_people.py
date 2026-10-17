# The zones' people (sprint 105, docs/suite-apres-d69.md §5 ter.4: "the givers live where the quests are done"),
# each on a map by the way in from the woods, on the free grass nearest a place of the map, reachable from its
# arrival; since sprint 112 the things to examine of the zones' quests (docs/textes-v5.md §9.1), placed the same way,
# on the floor of a dungeon's hall for the crypt's seal. Their words are in data/dialogues (tools/village_dialogues.py). Run from projects/rpg after
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
# The things to examine: id, look, name, what one reads, map, near which cell (docs/textes-v5.md §9.1).
PROPS = [
    ("claw-marks-1", "claws", text("Des griffures", "Claw marks"),
     text("Des griffures fraîches, toutes à la même hauteur, sur des troncs qui dessinent un arc de cercle.",
          "Fresh claw marks, all at the same height, on trunks that form an arc."), "woods-2-1", (24, 10)),
    ("claw-marks-2", "claws", text("Un sentier tassé", "A trodden path"),
     text("Le sol est tassé en un sentier courbe. Les loups ont tourné ici des nuits entières, autour de quelque chose.",
          "The ground is trodden into a curving path. The wolves have circled here for nights on end, around something."), "woods-2-3", (24, 20)),
    ("root-circle", "roots", text("Le cercle de racines", "The root circle"),
     text("Des racines épaisses comme des bras forment un cercle parfait, plus serré qu'aucune main ne l'aurait tressé. Au centre, la terre est bombée, comme si elle couvrait une pierre.",
          "Roots as thick as arms form a perfect circle, tighter than any hand could weave. In the middle the earth is mounded, as if covering a stone."), "woods-1-2", (24, 16)),
    ("mycelium-knot-1", "mycelium", text("Un nœud de filaments", "A knot of threads"),
     text("Des filaments blancs tiennent un éclat de pierre gravée sans l'entamer. La pierre reste pierre, le filament reste filament.",
          "White threads hold a shard of carved stone without biting into it. The stone stays stone, the thread stays thread."), "mouldy-3-1", (24, 16)),
    ("mycelium-knot-2", "mycelium", text("Un filament dans la mare", "A thread in the pool"),
     text("Un filament plonge dans une mare et en ressort sec. Autour, l'eau est claire comme de la source.",
          "A thread dips into a pool and comes out dry. Around it the water is clear as spring water."), "mouldy-2-2", (24, 16)),
    ("mycelium-knot-3", "mycelium", text("Une racine et un filament", "A root and a thread"),
     text("Une racine et un filament s'enroulent l'un autour de l'autre sans se mêler, comme deux mains qui se tiennent.",
          "A root and a thread wind around each other without merging, like two hands holding."), "mouldy-3-3", (24, 16)),
    ("crypt-seal", "seal", text("Le sceau", "The seal"),
     text("Un disque d'un métal sombre que personne ne sait nommer, posé là où les quatre ailes se rejoignent. Chaque signe regarde une aile. Une fissure le traverse, et elle est tiède.",
          "A disc of a dark metal nobody can name, set where the four wings meet. Each sign faces one wing. A crack runs through it, and it is warm."), "zombie-crypt-2", (10, 4)),
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

for (pid, look, name, said, map_id, near) in PROPS:
    path = f"{root}/towns/{map_id}.json"
    town = json.load(open(path), object_pairs_hook=O)
    rows = town["rows"]
    seen = reachable(rows, (town["spawn"]["x"], town["spawn"]["y"]))
    hall = town.get("dungeon") is not None
    taken = {(l["at"]["x"], l["at"]["y"]) for l in town.get("links", [])} | {(town["spawn"]["x"], town["spawn"]["y"])} \
        | {(n["at"]["x"], n["at"]["y"]) for n in town.get("npcs", [])}
    # Floor of its own (grass in the zones, the hall's floor in a dungeon), all four sides walkable.
    cells = [(x, y) for (x, y) in seen if rows[y][x] == ('=' if hall else '.') and (x, y) not in taken
             and all(n in seen and n not in taken for n in ((x + 1, y), (x - 1, y), (x, y + 1), (x, y - 1)))]
    at = min(cells, key=lambda c: (abs(c[0] - near[0]) + abs(c[1] - near[1]), c))
    prop = O([("id", pid), ("at", O([("x", at[0]), ("y", at[1])])), ("look", look), ("name", name), ("text", said)])
    town["props"] = [p for p in town.get("props", []) if p["id"] != pid] + [prop]
    open(path, "w").write(json.dumps(town, ensure_ascii=False, indent=2) + "\n")
    print(f"{pid} on {map_id} at {at}")
