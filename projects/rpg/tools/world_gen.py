# Writes the zones of the world (sprint 70, D63): a zone is a grid of 5 x 5 maps of 16 x 12 cells, the
# size of the village, each bordered with trees and open towards its neighbours; walking onto an
# opening leads to the next map. For now the woods, west of Clairval (data/towns/woods-<col>-<row>.json);
# Clairval's west way is written by hand in clairval.json. Deterministic: the same seed gives the same
# maps; every opening and exit is checked reachable from the map's arrival. Run from projects/rpg:
#   python3 tools/world_gen.py data
import collections, glob, json, os, random, sys

root = sys.argv[1]
O = collections.OrderedDict
W, H = 16, 12
SIZE = 5
CENTRE = (8, 6)


def cell(x, y): return O([("x", x), ("y", y)])
def text(fr, en): return O([("fr", fr), ("en", en)])


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
            if 0 <= n[0] < W and 0 <= n[1] < H and n not in seen and g[n[1]][n[0]] in '.=':
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


def woods_map(seed, openings, exit_at):
    """A map of the woods: tree clusters, rocks, sometimes a pond; the ways in kept free to the centre."""
    for attempt in range(50):
        rnd = random.Random(seed * 100 + attempt)
        g = [['T' if x in (0, W - 1) or y in (0, H - 1) else '.' for x in range(W)] for y in range(H)]
        keep = {CENTRE}
        for cells in openings.values():
            for (edge, inner) in cells:
                g[edge[1]][edge[0]] = '.'
                keep |= {edge, inner}
            # A free trail from the middle of the opening to the centre.
            keep |= set(lpath(cells[len(cells) // 2][1], CENTRE))
        if exit_at:
            keep |= set(lpath(exit_at, CENTRE))
        near = {(x + dx, y + dy) for (x, y) in keep for (dx, dy) in ((0, 0), (1, 0), (-1, 0), (0, 1), (0, -1))}
        decor = [('T', 18, 6), ('R', 3, 1)] + ([('~', 1, 5)] if rnd.random() < 0.4 else [])
        for (ch, count, size) in decor:
            for _ in range(count):
                x, y = rnd.randrange(2, W - 2), rnd.randrange(2, H - 2)
                for _ in range(size):
                    if 1 <= x < W - 1 and 1 <= y < H - 1 and (x, y) not in near:
                        g[y][x] = ch
                    x, y = x + rnd.randint(-1, 1), y + rnd.randint(-1, 1)
        rows = [''.join(r) for r in g]
        r = reachable(rows, CENTRE)
        musts = [e for cells in openings.values() for (e, i) in cells] + ([exit_at] if exit_at else [])
        if all(m in r for m in musts):
            return rows
    raise SystemExit(f"no map found for seed {seed}")


def opening(rnd, side):
    """Where an opening of three cells starts along a side, away from the corners."""
    return rnd.randrange(2, H - 4) if side in ("east", "west") else rnd.randrange(2, W - 4)


def zone(zid, name, level, origin, seed, exits=None, outside=None):
    """
    The 5 x 5 maps of a zone whose north-west map is at `origin` in the world. `outside` gives the ways
    out of the zone: (col, row, side) -> (map id, [(edge cell, arrival)...]); `exits` (col, row) -> exit.
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
                    to, ways = outside[(col, row, side)]
                    links += [O([("at", cell(*e)), ("to", to), ("arrival", cell(*a)), ("name", OUTSIDE_NAMES[to])]) for (e, a) in ways]
                    openings[side] = [(e, (e[0] - dx, e[1] - dy)) for (e, _) in ways]
            exit_spec = exits.get((col, row))
            rows = woods_map(seed * 1000 + row * SIZE + col, openings, exit_spec[0] if exit_spec else None)
            maps[mid] = O([("id", mid), ("zone", zid), ("name", name), ("level", level),
                           ("mapAt", cell(origin[0] + col, origin[1] + row)), ("rows", rows), ("spawn", cell(*CENTRE)), ("npcs", []),
                           ("exits", [O([("at", cell(*exit_spec[0])), ("scenario", exit_spec[1]), ("name", exit_spec[2])])] if exit_spec else []),
                           ("links", links)])
    return maps


OUTSIDE_NAMES = {"clairval": text("Est : Clairval", "East: Clairval")}

# The woods, west of the village (at 0, 0): their east middle map opens on Clairval's west road (rows 5 and 6).
woods = zone("woods", text("Bois de Clairval", "Clairval Woods"), 3, (-5, -2), 70,
             exits={(2, 2): ((5, 6), "training", text("Clairière : entraînement", "Glade: training"))},
             outside={(4, 2, "east"): ("clairval", [((15, 5), (1, 5)), ((15, 6), (1, 6))])})

for old in glob.glob(f"{root}/towns/woods-*.json") + [f"{root}/towns/clairval-woods.json", f"{root}/towns/misty-heath.json"]:
    if os.path.exists(old):
        os.remove(old)
for mid, m in woods.items():
    open(f"{root}/towns/{mid}.json", "w").write(json.dumps(m, ensure_ascii=False, indent=2) + "\n")
print(f"{len(woods)} maps written")
