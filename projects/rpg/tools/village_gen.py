# Clairval in twelve maps (sprint 102, D74, docs/suite-apres-d69.md §5 ter.3): 4 x 3 maps of 48 x 32 cells at
# x 0 to 3, y -1 to 1 of the world, closed by a palisade ('P') with three gates west towards the woods
# (woods-4-1, woods-4-2, woods-4-3), wide ways between its own maps, a theme each (orchards, gardens, the mill
# pond, the quarry, the west gate, the square, the market, the craftsmen's quarter, the south gate, the houses,
# the chapel, the pastures). The square keeps the id "clairval"; the others are clairval-<col>-<row>. No way
# east to a training fight any more (Garance offers it in her talk). Deterministic; every way and every gate
# checked reachable from the arrival. Run from projects/rpg before tools/world_gen.py (which opens the woods'
# east edges in front of the gates):  python3 tools/village_gen.py data
import collections, glob, json, os, random, sys

root = sys.argv[1]
O = collections.OrderedDict
W, H = 48, 32
COLS, ROWS = 4, 3
CENTRE = (24, 16)
WALK = '.=,'
GATE = (15, 16, 17)          # the west gates' rows, as the woods' east openings
SIDE_GAP = range(13, 20)     # the ways east and west between the village's maps
DOWN_GAP = range(21, 28)     # the ways north and south between them


def cell(x, y): return O([("x", x), ("y", y)])
def text(fr, en): return O([("fr", fr), ("en", en)])


def mid(c, r): return "clairval" if (c, r) == (1, 1) else f"clairval-{c}-{r}"


NAMES = {
    (0, 0): text("Clairval, porte du Nord", "Clairval, North Gate"),
    (1, 0): text("Clairval, les potagers", "Clairval, the Gardens"),
    (2, 0): text("Clairval, le moulin", "Clairval, the Mill"),
    (3, 0): text("Clairval, la carrière", "Clairval, the Quarry"),
    (0, 1): text("Clairval, porte de l'Ouest", "Clairval, West Gate"),
    (1, 1): text("Clairval", "Clairval"),
    (2, 1): text("Clairval, le marché", "Clairval, the Market"),
    (3, 1): text("Clairval, quartier des artisans", "Clairval, Craftsmen's Quarter"),
    (0, 2): text("Clairval, porte du Sud", "Clairval, South Gate"),
    (1, 2): text("Clairval, les maisons", "Clairval, the Houses"),
    (2, 2): text("Clairval, la chapelle", "Clairval, the Chapel"),
    (3, 2): text("Clairval, les pâturages", "Clairval, the Pastures"),
}
WOODS = text("Bois de Clairval", "Clairval Woods")

# The people of the old single map, moved (their ids and dialogues kept: "clairval/<npc>" still finds them on the square).
PEOPLE = {
    (1, 1): [("aubin", text("Aubin", "Aubin"), "male-a", 0, (26, 18), "guide"),
             ("ysaline", text("Ysaline", "Ysaline"), "female-d", 6, (20, 12), "scholar")],
    (0, 1): [("garance", text("Garance", "Garance"), "female-f", 5, (10, 13), "trainer")],
    (2, 1): [("rose", text("Rose", "Rose"), "female-b", 3, (22, 12), "merchant")],
}
BENCHES = {(3, 1): [((24, 12), "locksmith")]}


def lpath(a, b):
    """An L-shaped path, along x then along y."""
    (x, y), (tx, ty) = a, b
    cells = [(x, y)]
    while x != tx:
        x += 1 if tx > x else -1; cells.append((x, y))
    while y != ty:
        y += 1 if ty > y else -1; cells.append((x, y))
    return cells


def reachable(rows, start):
    seen, todo = {start}, [start]
    while todo:
        x, y = todo.pop()
        for n in ((x + 1, y), (x - 1, y), (x, y + 1), (x, y - 1)):
            if n not in seen and 0 <= n[0] < W and 0 <= n[1] < H and rows[n[1]][n[0]] in WALK:
                seen.add(n)
                todo.append(n)
    return seen


def house(g, x, y):
    g[y][x], g[y][x + 1], g[y + 1][x], g[y + 1][x + 1] = 'H', 'h', 'h', 'h'


def theme(rnd, g, c, r, free):
    """What stands on a map of the village, on the cells left free by the ways."""
    def put(ch, count, size=1):
        for _ in range(count):
            x, y = rnd.randrange(3, W - 3), rnd.randrange(3, H - 3)
            for _ in range(size):
                if free(x, y):
                    g[y][x] = ch
                x, y = x + rnd.randint(-1, 1), y + rnd.randint(-1, 1)

    def houses(count):
        for _ in range(count * 4):
            if count == 0:
                break
            x, y = rnd.randrange(3, W - 5), rnd.randrange(3, H - 5)
            if all(free(x + dx, y + dy) for dx in range(-1, 3) for dy in range(-1, 3)):
                house(g, x, y)
                count -= 1

    if (c, r) == (0, 0):            # North gate, the orchards: rows of fruit trees.
        for y in range(4, 12, 3):
            for x in range(26, 44, 3):
                if free(x, y):
                    g[y][x] = 'T'
        houses(2)
    elif (c, r) == (1, 0):          # The gardens: flower beds, plots of grass, houses.
        put(',', 10, 6)
        houses(4)
    elif (c, r) == (2, 0):          # The mill and its pond.
        for y in range(5, 11):
            for x in range(30, 40):
                if ((x - 35) / 5) ** 2 + ((y - 8) / 3) ** 2 < 1 and free(x, y):
                    g[y][x] = '~'
        houses(3)
        put('T', 4, 2)
    elif (c, r) == (3, 0):          # The quarry: rocks against the palisade.
        put('R', 14, 2)
        houses(1)
    elif (c, r) == (0, 1):          # The west gate's yard: lanterns, a cart.
        for (x, y) in ((6, 13), (6, 19), (12, 20)):
            if free(x, y):
                g[y][x] = 'L'
        put('C', 1)
        houses(3)
    elif (c, r) == (1, 1):          # The square: the fountain, lanterns, the old houses.
        for (x, y) in ((22, 13), (23, 13), (22, 14), (23, 14)):
            g[y][x] = 'F' if (x, y) == (22, 13) else 'f'
        for (x, y) in ((18, 15), (29, 15), (18, 18), (29, 18)):
            if free(x, y):
                g[y][x] = 'L'
        houses(6)
        put('T', 3, 2)
    elif (c, r) == (2, 1):          # The market: stalls round the street.
        for x in range(14, 36, 4):
            for y in (13, 19):
                if free(x, y):
                    g[y][x] = 'S'
        put('C', 2)
        houses(4)
    elif (c, r) == (3, 1):          # The craftsmen's quarter: workshops are houses, the benches in front.
        houses(6)
        put('C', 1)
    elif (c, r) == (0, 2):          # The south gate: a cart, hedges.
        put('B', 6, 4)
        put('C', 1)
        houses(2)
    elif (c, r) == (1, 2):          # The houses.
        houses(9)
        put('T', 3, 2)
    elif (c, r) == (2, 2):          # The chapel and the graves.
        houses(2)
        for y in range(22, 28, 2):
            for x in range(30, 40, 2):
                if free(x, y):
                    g[y][x] = 'R'
        put('T', 4, 2)
    else:                           # The pastures: hedges, flowers.
        put('B', 8, 5)
        put(',', 6, 5)
        put('T', 3, 2)


maps = {}
for r in range(ROWS):
    for c in range(COLS):
        rnd = random.Random(1020 + 10 * r + c)
        g = [['.'] * W for _ in range(H)]
        # The palisade on the village's outer edges; inner edges are bushes but for their wide ways.
        for x in range(W):
            g[0][x] = g[H - 1][x] = 'B'
        for y in range(H):
            g[y][0] = g[y][W - 1] = 'B'
        # The palisade last, so that it holds the corners.
        for x in range(W):
            if r == 0:
                g[0][x] = 'P'
            if r == ROWS - 1:
                g[H - 1][x] = 'P'
        for y in range(H):
            if c == 0:
                g[y][0] = 'P'
            if c == COLS - 1:
                g[y][W - 1] = 'P'
        links = []
        ways = []
        if c == 0:
            for y in GATE:
                g[y][0] = '='
                links.append(O([("at", cell(0, y)), ("to", f"woods-4-{r + 1}"), ("arrival", cell(W - 2, y)), ("name", WOODS)]))
            ways.append((0, 16))
        for (dc, dr, edge) in ((-1, 0, "west"), (1, 0, "east"), (0, -1, "north"), (0, 1, "south")):
            nc, nr = c + dc, r + dr
            if not (0 <= nc < COLS and 0 <= nr < ROWS):
                continue
            gap = SIDE_GAP if dr == 0 else DOWN_GAP
            for k in gap:
                (x, y) = {"west": (0, k), "east": (W - 1, k), "north": (k, 0), "south": (k, H - 1)}[edge]
                (ax, ay) = {"west": (W - 2, k), "east": (1, k), "north": (k, H - 2), "south": (k, 1)}[edge]
                g[y][x] = '='
                # Inside the village as inside a zone, a way has no name (D64).
                links.append(O([("at", cell(x, y)), ("to", mid(nc, nr)), ("arrival", cell(ax, ay))]))
            ways.append({"west": (0, 16), "east": (W - 1, 16), "north": (24, 0), "south": (24, H - 1)}[edge])
        # Roads from every way to the centre, the cells round them kept free.
        road = set()
        for w in ways:
            road |= set(lpath(w, CENTRE))
        for (x, y) in road:
            g[y][x] = '='
        near = {(x + dx, y + dy) for (x, y) in road for dx in (-1, 0, 1) for dy in (-1, 0, 1)}
        people = [p[4] for p in PEOPLE.get((c, r), [])] + [b[0] for b in BENCHES.get((c, r), [])]
        keep = near | {(x + dx, y + dy) for (x, y) in people for dx in (-2, -1, 0, 1, 2) for dy in (-2, -1, 0, 1, 2)}

        def free(x, y, g=g, keep=keep):
            return 1 <= x < W - 1 and 1 <= y < H - 1 and g[y][x] == '.' and (x, y) not in keep

        theme(rnd, g, c, r, free)
        rows = [''.join(row) for row in g]
        seen = reachable(rows, CENTRE)
        musts = [(l["at"]["x"], l["at"]["y"]) for l in links] + people
        assert all(m in seen or any(n in seen for n in ((m[0] + 1, m[1]), (m[0] - 1, m[1]), (m[0], m[1] + 1), (m[0], m[1] - 1))) for m in musts), (c, r)
        town = O([("id", mid(c, r)), ("name", NAMES[(c, r)]), ("level", 1), ("mapAt", cell(c, r - 1)), ("zone", "clairval"),
                  ("rows", rows), ("spawn", cell(*CENTRE)),
                  ("npcs", [O([("id", i), ("name", n), ("look", look), ("colour", col), ("at", cell(*at)), ("dialogue", d)])
                            for (i, n, look, col, at, d) in PEOPLE.get((c, r), [])]),
                  ("links", links), ("exits", [])])
        if (c, r) in BENCHES:
            town["workshops"] = [O([("at", cell(*at)), ("job", job)]) for (at, job) in BENCHES[(c, r)]]
        maps[mid(c, r)] = town

for old in glob.glob(f"{root}/towns/clairval*.json"):
    os.remove(old)
for i, m in maps.items():
    open(f"{root}/towns/{i}.json", "w").write(json.dumps(m, ensure_ascii=False, indent=2) + "\n")
print(f"{len(maps)} maps of the village written")
