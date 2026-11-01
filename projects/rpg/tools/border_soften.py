# Natural borders (sprint 180, D87 §7.C): the straight row of trees or hedges round each map of the five zones
# (tools/world_gen.py) becomes an irregular band, 1 to 3 cells deep, of obstacles mixed by style; the sides
# between two maps of a zone open wider (a half to three quarters of the side, sometimes all of it, never more
# than 8 closed cells in a row), both ways. Applied to the maps as they are (their people, props, exits and ways
# were added by other tools): only the band changes, never a cell holding something or next to it; every way,
# exit, person and prop stays reachable from the arrival (a cell the band would cut off is given back). The
# villages (Clairval, Quatre-Chemins) and the dungeons are left alone. Deterministic and idempotent: running it
# twice changes nothing more. Run from projects/rpg, after tools/world_gen.py:
#   python3 tools/border_soften.py data
import collections, glob, hashlib, json, random, sys

O = collections.OrderedDict
ZONES = ("woods", "mouldy", "lake", "fields", "ancestors")
WALK = '.=,'
DEPTH = 3          # the band's deepest cells
CLOSED_RUN = 8     # the most closed cells in a row on a side between two maps of a zone
# The obstacles of the band, by style (weights): trees and rocks in the woods, mushrooms in the mouldy woods,
# reeds by the lake, hedges in the fields, trunks, boulders and ferns in the Forest of the Ancestors.
MIX = {
    "woods": "TTTTTTTTTTTTTTBBBRRR",
    "mouldy": "TTTTTTTTTTTTMMMMMRRR",
    "lake": "BBBBBBBBBBBBTTTTTRRR",
    "fields": "BBBBBBBBBBBBBBBTTTRR",
    "ancestors": "TTTTTTTTTTTRRRRRBBBB",
}
SIDES = {"east": (1, 0), "west": (-1, 0), "south": (0, 1), "north": (0, -1)}
OPPOSITE = {"east": "west", "west": "east", "south": "north", "north": "south"}


def rng(*key):
    """A generator seeded by a name, the same on every machine (no hash() of Python)."""
    return random.Random(int(hashlib.sha256("/".join(map(str, key)).encode()).hexdigest()[:16], 16))


def length(m, side):
    return len(m["rows"]) if side in ("east", "west") else len(m["rows"][0])


def at(m, side, p, d):
    """The cell at position p along a side, d cells inside (d = 0: on the edge)."""
    w, h = len(m["rows"][0]), len(m["rows"])
    return {"east": (w - 1 - d, p), "west": (d, p), "south": (p, h - 1 - d), "north": (p, d)}[side]


def side_of(m, c):
    w, h = len(m["rows"][0]), len(m["rows"])
    x, y = c
    return "west" if x == 0 else "east" if x == w - 1 else "north" if y == 0 else "south" if y == h - 1 else None


def reach(g, start):
    h, w = len(g), len(g[0])
    seen, todo = {start}, [start]
    while todo:
        x, y = todo.pop()
        for (dx, dy) in ((1, 0), (-1, 0), (0, 1), (0, -1)):
            n = (x + dx, y + dy)
            if 0 <= n[0] < w and 0 <= n[1] < h and n not in seen and g[n[1]][n[0]] in WALK:
                seen.add(n); todo.append(n)
    return seen


def path(g, start, goal):
    """The shortest walk from start to goal (goal itself may be blocked: one stands beside it)."""
    h, w = len(g), len(g[0])
    back, todo = {start: None}, collections.deque([start])
    while todo:
        c = todo.popleft()
        if c == goal:
            out = []
            while c is not None:
                out.append(c); c = back[c]
            return out
        for (dx, dy) in ((1, 0), (-1, 0), (0, 1), (0, -1)):
            n = (c[0] + dx, c[1] + dy)
            if 0 <= n[0] < w and 0 <= n[1] < h and n not in back and (g[n[1]][n[0]] in WALK or n == goal):
                back[n] = c; todo.append(n)
    return None


def openings(seam, size):
    """
    Where a side between two maps of a zone opens: a third of the sides all along (but their corners), the
    others in runs of 3 to 12 cells between closed runs of 2 to 8, a half to three quarters of the side.
    """
    r = rng("open", *seam)
    if r.random() < 1 / 3:
        return set(range(2, size - 2))
    for _ in range(100):
        cells, p = set(), 2 + r.randint(0, 3)
        while p < size - 2:
            run = r.randint(3, 12)
            cells |= set(range(p, min(p + run, size - 2)))
            p += run + r.randint(2, CLOSED_RUN - 2)
        if size // 2 <= len(cells) <= 3 * size // 4:
            return cells
    return cells


def depths(m, side):
    """The band's depth along a side: a smooth noise of 1 to 3 cells, a control point every 5 cells."""
    r, n = rng("band", m["id"], side), length(m, side)
    knots = [r.randint(1, DEPTH) for _ in range(n // 5 + 2)]
    return [round(knots[p // 5] + (knots[p // 5 + 1] - knots[p // 5]) * (p % 5) / 5) for p in range(n)]


def guarded(m):
    """The cells the band never takes: anything placed on the map and the cells around it, the arrival, the trails."""
    out = set()
    def walk(v, key=None):
        if isinstance(v, dict):
            if "at" in v and isinstance(v["at"], dict):
                x, y = v["at"]["x"], v["at"]["y"]
                out.update((x + dx, y + dy) for dx in (-1, 0, 1) for dy in (-1, 0, 1))
            for k, w in v.items():
                if k not in ("rows", "mapAt"):
                    walk(w, k)
        elif isinstance(v, list):
            for w in v:
                walk(w, key)
    walk(m)
    s = (m["spawn"]["x"], m["spawn"]["y"])
    out.update((s[0] + dx, s[1] + dy) for dx in (-1, 0, 1) for dy in (-1, 0, 1))
    return out


def musts(m):
    """What must stay reachable from the arrival: the ways and exits, and a cell beside each person and prop."""
    ways = [(l["at"]["x"], l["at"]["y"]) for l in m["links"]] + [(e["at"]["x"], e["at"]["y"]) for e in m["exits"]]
    beside = [(x["at"]["x"], x["at"]["y"]) for x in m["npcs"] + m.get("props", [])]
    return ways, beside


def soften(maps):
    by_place = {(m["mapAt"]["x"], m["mapAt"]["y"]): m for m in maps.values()}
    # 1. The sides between two maps of a zone, and where they open (the gaps of world_gen.py kept).
    seams = {}
    for m in maps.values():
        for side in ("east", "south"):
            dx, dy = SIDES[side]
            o = by_place.get((m["mapAt"]["x"] + dx, m["mapAt"]["y"] + dy))
            if o is None or o["zone"] != m["zone"]:
                continue
            old = {(l["at"]["y"] if side == "east" else l["at"]["x"]) for l in m["links"] if l["to"] == o["id"]}
            seams[(m["id"], o["id"], side)] = (old, openings((m["id"], o["id"]), length(m, side)) | old)
    # 2. Open them on both maps, dropping the cells that lead nowhere on either side (a pond, a pocket).
    grids = {mid: [list(r) for r in m["rows"]] for mid, m in maps.items()}
    dropped = set()
    while True:
        opened = {mid: [r[:] for r in g] for mid, g in grids.items()}
        for (a, b, side), (old, cells) in seams.items():
            for (mid, s) in ((a, side), (b, OPPOSITE[side])):
                g = opened[mid]
                for p in cells:
                    if (a, b, side, p) in dropped:
                        continue
                    for d in (0, 1):
                        x, y = at(maps[mid], s, p, d)
                        if g[y][x] not in WALK + '~':
                            g[y][x] = '.'
                        elif d == 0 and g[y][x] == '~':
                            g[y][x] = '='  # a pontoon, as world_gen.py lays over the water of a way
        lost = set()
        for (a, b, side), (old, cells) in seams.items():
            for (mid, s) in ((a, side), (b, OPPOSITE[side])):
                g, m = opened[mid], maps[mid]
                r = reach(g, (m["spawn"]["x"], m["spawn"]["y"]))
                for p in cells - old:
                    if (a, b, side, p) not in dropped and not (at(m, s, p, 0) in r and at(m, s, p, 1) in r):
                        lost.add((a, b, side, p))
        if not lost:
            break
        dropped |= lost
    for (a, b, side), (old, cells) in seams.items():
        seams[(a, b, side)] = (old, {p for p in cells if p in old or (a, b, side, p) not in dropped})
    # The closed cells of an open side, back to the border they had (a cell given back by a drop).
    for (a, b, side), (old, cells) in seams.items():
        for (mid, s) in ((a, side), (b, OPPOSITE[side])):
            m = maps[mid]
            for p in range(length(m, s)):
                x, y = at(m, s, p, 0)
                if p not in cells and opened[mid][y][x] in WALK:
                    opened[mid][y][x] = grids[mid][y][x] if grids[mid][y][x] not in WALK else MIX[m["zone"]][0]
    # 3. The ways of the new gaps, both ways (inside a zone a way has no name, D64).
    for (a, b, side), (old, cells) in seams.items():
        for (mid, to, s) in ((a, b, side), (b, a, OPPOSITE[side])):
            m, o = maps[mid], maps[to]
            for p in sorted(cells - old):
                e, arrival = at(m, s, p, 0), at(o, OPPOSITE[s], p, 1)
                m["links"].append(O([("at", O([("x", e[0]), ("y", e[1])])), ("to", to), ("arrival", O([("x", arrival[0]), ("y", arrival[1])]))]))
    # 4. The band: on every side, obstacles from the edge inward to the side's depth, but on its gaps and what is guarded.
    for mid, m in maps.items():
        g, keep = opened[mid], guarded(m)
        start = (m["spawn"]["x"], m["spawn"]["y"])
        before = [r[:] for r in g]
        free = {side_of(m, (l["at"]["x"], l["at"]["y"])) + str(l["at"]["y"] if side_of(m, (l["at"]["x"], l["at"]["y"])) in ("east", "west") else l["at"]["x"]) for l in m["links"]}
        mix, r = MIX[m["zone"]], rng("mix", mid)
        for side in SIDES:
            n = length(m, side)
            for p, depth in enumerate(depths(m, side)):
                if side + str(p) in free:
                    continue
                for d in range(depth):
                    x, y = at(m, side, p, d)
                    ch = r.choice(mix)
                    if d == 0 and g[y][x] not in WALK + '~':
                        # The lake goes on to the edge where its water touches it: no wall of reeds across it.
                        ix, iy = at(m, side, p, 1)
                        g[y][x] = '~' if g[iy][ix] == '~' and 0 < p < n - 1 else mix[0] if p in (0, n - 1) else ch
                    elif (x, y) not in keep and g[y][x] in '.,':
                        g[y][x] = ch
        # 5. Give back what the band cut off: the cells it took on a walk to each way, exit, person or prop.
        ways, beside = musts(m)
        was = reach(before, start)
        # A way or exit one stands on, if one could before; otherwise (a door in a wall) one stands beside it.
        on = [w for w in ways if w in was]
        beside = beside + [w for w in ways if w not in was]
        for _ in range(len(ways) + len(beside) + 1):
            here = reach(g, start)
            cut = [w for w in on if w not in here] + [b for b in beside if not any((b[0] + dx, b[1] + dy) in here for dx, dy in ((1, 0), (-1, 0), (0, 1), (0, -1)))]
            if not cut:
                break
            for c in path(before, start, cut[0]) or []:
                if g[c[1]][c[0]] != before[c[1]][c[0]]:
                    g[c[1]][c[0]] = before[c[1]][c[0]]
        else:
            raise SystemExit(f"{mid}: a way, an exit, a person or a prop cut off")
        m["rows"] = [''.join(row) for row in g]
    return maps


if __name__ == "__main__":
    root = sys.argv[1]
    maps = {}
    for f in sorted(glob.glob(f"{root}/towns/*.json")):
        m = json.load(open(f), object_pairs_hook=O)
        if m.get("zone") in ZONES:
            maps[m["id"]] = m
    soften(maps)
    for mid, m in maps.items():
        open(f"{root}/towns/{mid}.json", "w").write(json.dumps(m, ensure_ascii=False, indent=2) + "\n")
    print(f"{len(maps)} maps softened")
