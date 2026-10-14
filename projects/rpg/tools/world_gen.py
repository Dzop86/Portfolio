# Writes the zones of the world (sprint 70, D63): a zone is a grid of 5 x 5 maps of 16 x 12 cells, the
# size of the village, each bordered (trees, or hedges) and open towards its neighbours; walking onto an
# opening leads to the next map. The woods west of Clairval (sprint 70), the mouldy woods west of them
# (sprint 74), the lake north of them (sprint 75), the fields south of them (sprint 76), each zone
# opening on the woods by two maps (data/towns/<zone>-<col>-<row>.json); Clairval's west way is written
# by hand in clairval.json. Deterministic: the same seed gives the same
# maps; every opening and exit is checked reachable from the map's arrival. Run from projects/rpg:
#   python3 tools/world_gen.py data [mouldy] [lake] [fields]   (the woods always)
import collections, glob, json, os, random, sys

root = sys.argv[1]
O = collections.OrderedDict
W, H = 16, 12
SIZE = 5
CENTRE = (8, 6)


def cell(x, y): return O([("x", x), ("y", y)])
def text(fr, en): return O([("fr", fr), ("en", en)])


WALK = '.=,'  # grass, road, flowers

SIDES = {  # direction: (step on the grid of maps, name)
    "east": ((1, 0), text("Est", "East")),
    "west": ((-1, 0), text("Ouest", "West")),
    "south": ((0, 1), text("Sud", "South")),
    "north": ((0, -1), text("Nord", "North")),
}


def edge_cells(side, start, width):
    """The cells of an opening on a side of a map, and the cell one step inside for each."""
    if side in ("east", "west"):
        x, inner = (W - 1, W - 2) if side == "east" else (0, 1)
        return [((x, y), (inner, y)) for y in range(start, start + width)]
    y, inner = (H - 1, H - 2) if side == "south" else (0, 1)
    return [((x, y), (x, inner)) for x in range(start, start + width)]


def mirror(side, c):
    """Where one arrives on the neighbour when leaving by cell c of this side: one step inside its facing edge."""
    x, y = c
    return {"east": (1, y), "west": (W - 2, y), "south": (x, 1), "north": (x, H - 2)}[side]


OPPOSITE = {"east": "west", "west": "east", "south": "north", "north": "south"}


def reachable(g, start):
    seen, todo = {start}, [start]
    while todo:
        x, y = todo.pop()
        for (dx, dy) in ((1, 0), (-1, 0), (0, 1), (0, -1)):
            n = (x + dx, y + dy)
            if 0 <= n[0] < W and 0 <= n[1] < H and n not in seen and g[n[1]][n[0]] in WALK:
                seen.add(n); todo.append(n)
    return seen


def lpath(a, b):
    """The cells from a to b, horizontally then vertically."""
    (x, y), (tx, ty) = a, b
    cells = [(x, y)]
    while x != tx:
        x += 1 if tx > x else -1; cells.append((x, y))
    while y != ty:
        y += 1 if ty > y else -1; cells.append((x, y))
    return cells


def woods_map(seed, openings, exit_at, style="woods", water=None):
    """
    A map of a zone, in its style: tree clusters, rocks, sometimes a pond (the woods); trees, mushrooms
    and pools (the mouldy woods); hedges and reeds round the water of the lake; flowers and hedges (the
    fields). The ways in kept free to the centre, as trails (on the lake, pontoons over the water).
    """
    border, decor_of = STYLES[style]
    for attempt in range(50):
        rnd = random.Random(seed * 100 + attempt)
        g = [[border if x in (0, W - 1) or y in (0, H - 1) else '.' for x in range(W)] for y in range(H)]
        if water:
            for y in range(1, H - 1):
                for x in range(1, W - 1):
                    if water(x, y):
                        g[y][x] = '~'
        keep = {CENTRE}
        for cells in openings.values():
            for (edge, inner) in cells:
                g[edge[1]][edge[0]] = '.'
                keep |= {edge, inner}
            # A free trail from the middle of the opening to the centre.
            keep |= set(lpath(cells[len(cells) // 2][1], CENTRE))
        if exit_at:
            keep |= set(lpath(exit_at, CENTRE))
        for (x, y) in keep:
            if g[y][x] == '~':
                g[y][x] = '='
        near = {(x + dx, y + dy) for (x, y) in keep for (dx, dy) in ((0, 0), (1, 0), (-1, 0), (0, 1), (0, -1))}
        for (ch, count, size) in decor_of(rnd):
            for _ in range(count):
                x, y = rnd.randrange(2, W - 2), rnd.randrange(2, H - 2)
                for _ in range(size):
                    if 1 <= x < W - 1 and 1 <= y < H - 1 and (x, y) not in near and g[y][x] != '~':
                        g[y][x] = ch
                    x, y = x + rnd.randint(-1, 1), y + rnd.randint(-1, 1)
        rows = [''.join(r) for r in g]
        r = reachable(rows, CENTRE)
        musts = [e for cells in openings.values() for (e, i) in cells] + ([exit_at] if exit_at else [])
        if all(m in r for m in musts):
            return rows
    raise SystemExit(f"no map found for seed {seed}")


# Each style: its border, and its decor (character, clusters, cells per cluster) drawn per map.
STYLES = {
    "woods": ('T', lambda rnd: [('T', 18, 6), ('R', 3, 1)] + ([('~', 1, 5)] if rnd.random() < 0.4 else [])),
    "mouldy": ('T', lambda rnd: [('T', 14, 6), ('M', 9, 3), ('~', 2, 4), ('R', 2, 1)]),
    "lake": ('B', lambda rnd: [('B', 7, 3), ('T', 4, 3), ('R', 2, 1)]),
    "fields": ('B', lambda rnd: [(',', 12, 7), ('B', 6, 4), ('T', 3, 2), ('C', 1, 1)]),
}


def opening(rnd, side):
    """Where an opening of three cells starts along a side, away from the corners."""
    return rnd.randrange(2, H - 4) if side in ("east", "west") else rnd.randrange(2, W - 4)


def zone(zid, name, level, origin, seed, style="woods", exits=None, outside=None, water=None):
    """
    The 5 x 5 maps of a zone whose north-west map is at `origin` in the world. `outside` gives the ways
    out of the zone: (col, row, side) -> (map id, [(edge cell, arrival)...], name); `exits` (col, row) ->
    exit; `water(gx, gy)`, cells of the zone (counted across its maps) under water.
    """
    exits, outside = exits or {}, outside or {}
    rnd = random.Random(seed)
    starts = {}
    for row in range(SIZE):
        for col in range(SIZE):
            for side in ("east", "south"):
                (dx, dy), _ = SIDES[side]
                if col + dx < SIZE and row + dy < SIZE:
                    starts[(col, row, side)] = opening(rnd, side)
    maps = {}
    for row in range(SIZE):
        for col in range(SIZE):
            mid = f"{zid}-{col}-{row}"
            openings, links = {}, []
            for side, ((dx, dy), side_name) in SIDES.items():
                key = (col, row, side) if side in ("east", "south") else (col + dx, row + dy, OPPOSITE[side])
                if key in starts:
                    cells = edge_cells(side, starts[key], 3)
                    to = f"{zid}-{col + dx}-{row + dy}"
                    links += [O([("at", cell(*e)), ("to", to), ("arrival", cell(*mirror(side, e))), ("name", side_name)]) for (e, _) in cells]
                    openings[side] = cells
                elif (col, row, side) in outside:
                    to, ways, way_name = outside[(col, row, side)]
                    links += [O([("at", cell(*e)), ("to", to), ("arrival", cell(*a)), ("name", way_name)]) for (e, a) in ways]
                    openings[side] = [(e, (e[0] - dx, e[1] - dy)) for (e, _) in ways]
            exit_spec = exits.get((col, row))
            here = (lambda x, y, c=col, r=row: water(c * W + x, r * H + y)) if water else None
            rows = woods_map(seed * 1000 + row * SIZE + col, openings, exit_spec[0] if exit_spec else None, style, here)
            maps[mid] = O([("id", mid), ("zone", zid), ("name", name), ("level", level),
                           ("mapAt", cell(origin[0] + col, origin[1] + row)), ("rows", rows), ("spawn", cell(*CENTRE)), ("npcs", []),
                           ("exits", [O([("at", cell(*exit_spec[0])), ("scenario", exit_spec[1]), ("name", exit_spec[2])])] if exit_spec else []),
                           ("links", links)])
    return maps


def bridge(outsides, a, b, side, start, names):
    """
    A way of three cells between map a = (zone, col, row) and the map b next to it on `side`, both
    ways: `outsides` gathers each zone's ways out; `names` (to b, to a).
    """
    back = OPPOSITE[side]
    for (here, there, s, name) in ((a, b, side, names[0]), (b, a, back, names[1])):
        cells = edge_cells(s, start, 3)
        outsides.setdefault(here[0], {})[(here[1], here[2], s)] = (f"{there[0]}-{there[1]}-{there[2]}", [(e, mirror(s, e)) for (e, _) in cells], name)


NAMES = {
    "woods": text("Bois de Clairval", "Clairval Woods"),
    "mouldy": text("Bois moisi", "Mouldy Woods"),
    "lake": text("Lac des Joncs", "Reed Lake"),
    "fields": text("Champs de Clairval", "Clairval Fields"),
}


def towards(side, zid):
    d = SIDES[side][1]
    return text(f"{d['fr']} : {NAMES[zid]['fr']}", f"{d['en']}: {NAMES[zid]['en']}")


outsides = {"woods": {(4, 2, "east"): ("clairval", [((15, 5), (1, 5)), ((15, 6), (1, 6))], text("Est : Clairval", "East: Clairval"))}}
# Each zone opens on the woods by two maps (sprints 74 to 76).
for (row, start) in ((1, 5), (3, 4)):
    bridge(outsides, ("woods", 0, row), ("mouldy", 4, row), "west", start, (towards("west", "mouldy"), towards("east", "woods")))
for (col, start) in ((1, 6), (3, 6)):
    bridge(outsides, ("woods", col, 0), ("lake", col, 4), "north", start, (towards("north", "lake"), towards("south", "woods")))
    bridge(outsides, ("woods", col, 4), ("fields", col, 0), "south", start, (towards("south", "fields"), towards("north", "woods")))


def lake_water(gx, gy):
    """The lake: an ellipse over the middle of the zone (80 x 60 cells), its shore a little ragged."""
    dx, dy = (gx - 40) / 27, (gy - 30) / 19
    wobble = 0.08 * ((gx * 7 + gy * 13) % 5 - 2)
    return dx * dx + dy * dy < 1 + wobble


# Only the zones written now: a way to one not written yet is left out, with its opening.
built = {"woods", *sys.argv[2:]}
outsides = {z: {k: v for k, v in ways.items() if v[0] == "clairval" or v[0].rsplit("-", 2)[0] in built} for z, ways in outsides.items()}

zones = [
    # The woods, west of the village (at 0, 0): their east middle map opens on Clairval's west road (rows 5 and 6).
    zone("woods", NAMES["woods"], 3, (-5, -2), 70,
         exits={(2, 2): ((5, 6), "training", text("Clairière : entraînement", "Glade: training"))}, outside=outsides["woods"]),
]
ZONES = {"mouldy": (10, (-10, -2), 74), "lake": (18, (-5, -7), 75), "fields": (26, (-5, 3), 76)}
for zid in sys.argv[2:]:
    level, origin, seed = ZONES[zid]
    zones.append(zone(zid, NAMES[zid], level, origin, seed, style=zid, outside=outsides.get(zid), water=lake_water if zid == "lake" else None))
for old in [f for z in built for f in glob.glob(f"{root}/towns/{z}-*.json")]:
    os.remove(old)
for maps in zones:
    for mid, m in maps.items():
        open(f"{root}/towns/{mid}.json", "w").write(json.dumps(m, ensure_ascii=False, indent=2) + "\n")
print(f"{sum(len(m) for m in zones)} maps written: {', '.join(sorted(built))}")
