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


NAMES = {  # The scriptwriter's names (docs/textes-v5.md §2, sprint 104), after the village's.
    (0, 0): text("Clairval, les vergers", "Clairval, the Orchards"),
    (1, 0): text("Clairval, les potagers", "Clairval, the Kitchen Gardens"),
    (2, 0): text("Clairval, le moulin de l'étang", "Clairval, the Pond Mill"),
    (3, 0): text("Clairval, la petite carrière", "Clairval, the Little Quarry"),
    (0, 1): text("Clairval, la cour d'armes", "Clairval, the Drill Yard"),
    (1, 1): text("Clairval", "Clairval"),
    (2, 1): text("Clairval, le marché", "Clairval, the Market"),
    (3, 1): text("Clairval, la rue des artisans", "Clairval, Craftsmen's Row"),
    (0, 2): text("Clairval, l'écurie", "Clairval, the Stables"),
    (1, 2): text("Clairval, les greniers", "Clairval, the Granaries"),
    (2, 2): text("Clairval, le clos de la chapelle", "Clairval, Chapel Close"),
    (3, 2): text("Clairval, le pré aux moutons", "Clairval, Sheep Meadow"),
}
WOODS = text("Bois de Clairval", "Clairval Woods")

# The people of the old single map, moved (their ids and dialogues kept: "clairval/<npc>" still finds them on the
# square), and since sprint 104 the scriptwriter's (docs/textes-v5.md §1.3), each by their house or their gate.
PEOPLE = {
    (1, 1): [("aubin", text("Aubin", "Aubin"), "male-a", 0, (26, 18), "guide"),
             ("ysaline", text("Ysaline", "Ysaline"), "female-d", 6, (20, 12), "scholar")],
    (0, 0): [("thibaut", text("Thibaut", "Thibaut"), "male-b", 1, (3, 13), "thibaut"),
             ("hortense", text("Hortense", "Hortense"), "female-e", 2, (12, 13), "hortense")],
    (2, 0): [("firmin", text("Firmin", "Firmin"), "male-c", 4, (29, 10), "firmin")],
    (3, 0): [("gaspard", text("Gaspard", "Gaspard"), "male-d", 6, (31, 11), "gaspard")],
    (0, 1): [("garance", text("Garance", "Garance"), "female-f", 5, (10, 13), "trainer"),
             ("mahaut", text("Mahaut", "Mahaut"), "female-a", 0, (3, 13), "mahaut"),
             ("guard", text("Garde", "Guard"), "male-e", 1, (3, 19), "guard")],
    (2, 1): [("rose", text("Rose", "Rose"), "female-b", 3, (22, 12), "merchant")],
    (3, 1): [("anselme", text("Anselme", "Anselme"), "male-d", 0, (26, 5), "anselme"),
             ("perrine", text("Perrine", "Perrine"), "female-b", 6, (26, 10), "perrine"),
             ("gedeon", text("Gédéon", "Gédéon"), "male-a", 5, (21, 11), "gedeon"),
             ("sidonie", text("Sidonie", "Sidonie"), "female-f", 4, (26, 21), "sidonie"),
             ("basile", text("Basile", "Basile"), "male-c", 3, (26, 26), "basile"),
             # The jeweller and the cobbler (sprint 110, D76): Gédéon's pattern, west of the road, south of the way west.
             ("eudes", text("Eudes", "Eudes"), "male-f", 2, (21, 22), "eudes"),
             ("apolline", text("Apolline", "Apolline"), "female-c", 1, (21, 28), "apolline")],
    (0, 2): [("gautier", text("Gautier", "Gautier"), "male-f", 3, (3, 13), "gautier")],
    (1, 2): [("leonie", text("Léonie", "Léonie"), "female-c", 5, (21, 22), "leonie")],
    (2, 2): [("odalric", text("Odalric", "Odalric"), "male-b", 2, (27, 21), "odalric")],
}
# Their houses (top-left corner; the door faces south), set before anything else of the map.
HOMES = {
    (0, 0): [(10, 11)],
    (2, 0): [(27, 7)],
    (3, 0): [(32, 9)],
    (3, 1): [(27, 4), (27, 9), (19, 8), (27, 20), (27, 25), (19, 19), (19, 25)],
    (1, 2): [(23, 25)],
    (2, 2): [(28, 19)],
}
# A workshop beside its craftsman's house, never on the road (Charles, D75): the locksmith's bench by Gédéon's door.
# The forge and the tailor's workshop since sprint 109, the jeweller's and the cobbler's benches since sprint 110, Sidonie's
# still and Basile's oven since sprint 111.
BENCHES = {(3, 1): [((19, 10), "locksmith"), ((27, 6), "smith"), ((27, 11), "tailor"), ((19, 21), "jeweller"), ((19, 27), "cobbler"),
                    ((27, 22), "alchemist"), ((27, 27), "baker")]}
# Fenced yards (sprint 104): the corners of the fence, what stands inside against it, and its beasts.
PENS = {
    (0, 2): ((8, 19), (19, 28), [("E", (9, 20))], [("horse", (13, 23)), ("horse", (16, 25)), ("horse", (12, 26))]),
    (1, 2): ((10, 19), (19, 27), [("Q", (11, 20))], [("hen", (14, 22)), ("hen", (16, 21)), ("hen", (13, 25)), ("hen", (17, 24)), ("hen", (15, 26))]),
    (3, 2): ((29, 5), (44, 26), [], [("sheep", (33, 9)), ("sheep", (38, 12)), ("sheep", (35, 18)), ("sheep", (41, 21)), ("sheep", (32, 23)), ("sheep", (40, 8))]),
}
# Things to examine (sprint 105): the old stone Ysaline studies, on the square (its quest comes with the tutorial).
PROPS = {
    (1, 1): [("old-stone", (18, 12), "stone", text("La vieille pierre", "The old stone"),
              text("Une pierre grise plus haute qu'un homme, fendue de haut en bas : une graine, une goutte, un marteau, une plume. Sur la face lisse, usée par les mains, un cinquième signe affleure : cinq traits en cercle, qui se font face sans se toucher.",
                   "A grey stone taller than a man, split from top to bottom: a seed, a drop, a hammer, a feather. On the smooth face, worn by hands, a fifth sign shows through: five strokes in a ring, facing each other without touching."), None)],
    # Sprint 107, the tutorial (docs/textes-v5.md §9.1): the loft's hatch starts the rats' fight, two old graves at the chapel.
    (1, 2): [("loft-hatch", (25, 25), "hatch", text("La trappe du grenier", "The loft hatch"),
              text("Une trappe en bois qui gratte de l'intérieur. Quelque chose a rongé le loquet.",
                   "A wooden hatch, scratching from the inside. Something has gnawed through the latch."), "loft-rats")],
    (2, 2): [("old-grave-1", (42, 26), "grave", text("Une vieille tombe", "An old grave"),
              text("Une dalle mangée de mousse. Le nom est effacé, mais on lit encore une date : trois cents ans avant la fondation de Clairval.",
                   "A slab eaten by moss. The name is gone, but a date can still be read: three hundred years before Clairval was founded."), None),
             ("old-grave-2", (28, 27), "grave", text("Une tombe grattée", "A scraped grave"),
              text("Quatre signes gravés autour d'un creux gratté au burin. Celui qui l'a gratté a pris soin de ne rien abîmer d'autre.",
                   "Four signs carved around a hollow scraped out with a chisel. Whoever scraped it took care to damage nothing else."), None)],
}
# Pétronille, Léonie's nosiest hen, out of the yard by her side.
LOOSE = {(1, 2): [("hen", (22, 24))]}


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


def house(g, x, y, k='H'):
    g[y][x], g[y][x + 1], g[y + 1][x], g[y + 1][x + 1] = k, k.lower(), k.lower(), k.lower()


def pen(g, a, b, inside):
    """A fence round the rectangle from a to b, what stands inside it."""
    (x0, y0), (x1, y1) = a, b
    for x in range(x0, x1 + 1):
        g[y0][x] = g[y1][x] = 'X'
    for y in range(y0, y1 + 1):
        g[y][x0] = g[y][x1] = 'X'
    for (k, (x, y)) in inside:
        house(g, x, y, k)


def theme(rnd, g, c, r, free, road_cells):
    """What stands on a map of the village, on the cells left free by the ways."""
    def put(ch, count, size=1):
        for _ in range(count):
            x, y = rnd.randrange(3, W - 3), rnd.randrange(3, H - 3)
            for _ in range(size):
                if free(x, y):
                    g[y][x] = ch
                x, y = x + rnd.randint(-1, 1), y + rnd.randint(-1, 1)

    def houses(count):
        # Along the streets first (sprint 104, "a real village"): the closest free places to a road, a little shuffled.
        spots = [(x, y) for x in range(2, W - 3) for y in range(2, H - 3)]
        rnd.shuffle(spots)
        spots.sort(key=lambda p: min(abs(p[0] - rx) + abs(p[1] - ry) for (rx, ry) in road_cells) // 3)
        for (x, y) in spots:
            if count == 0:
                break
            if all(free(x + dx, y + dy) for dx in range(-1, 3) for dy in range(-1, 3)):
                house(g, x, y)
                count -= 1

    if (c, r) == (0, 0):            # North gate, the orchards: rows of fruit trees.
        for y in range(4, 12, 3):
            for x in range(26, 44, 3):
                if free(x, y):
                    g[y][x] = 'T'
        houses(5)
    elif (c, r) == (1, 0):          # The gardens: flower beds, plots of grass, houses.
        put(',', 10, 6)
        houses(8)
    elif (c, r) == (2, 0):          # The mill and its pond.
        for y in range(5, 11):
            for x in range(30, 40):
                if ((x - 35) / 5) ** 2 + ((y - 8) / 3) ** 2 < 1 and free(x, y):
                    g[y][x] = '~'
        houses(5)
        put('T', 4, 2)
    elif (c, r) == (3, 0):          # The quarry: rocks against the palisade.
        put('R', 14, 2)
        houses(3)
    elif (c, r) == (0, 1):          # The west gate's yard: lanterns, a cart.
        for (x, y) in ((6, 13), (6, 19), (12, 20)):
            if free(x, y):
                g[y][x] = 'L'
        put('C', 1)
        houses(5)
    elif (c, r) == (1, 1):          # The square: the fountain, lanterns, the old houses.
        for (x, y) in ((22, 13), (23, 13), (22, 14), (23, 14)):
            g[y][x] = 'F' if (x, y) == (22, 13) else 'f'
        for (x, y) in ((18, 15), (29, 15), (18, 18), (29, 18)):
            if free(x, y):
                g[y][x] = 'L'
        houses(10)
        put('T', 3, 2)
    elif (c, r) == (2, 1):          # The market: stalls round the street.
        for x in range(14, 36, 4):
            for y in (13, 19):
                if free(x, y):
                    g[y][x] = 'S'
        put('C', 2)
        houses(8)
    elif (c, r) == (3, 1):          # Craftsmen's row: their houses along the street, a bench by the locksmith's.
        houses(6)
        put('C', 2)
    elif (c, r) == (0, 2):          # The south gate: the stables and their paddock, a cart, hedges.
        put('C', 1)
        houses(6)
        put('B', 4, 3)
    elif (c, r) == (1, 2):          # The granaries, Léonie's hen house and its yard.
        houses(8)
        put('T', 3, 2)
    elif (c, r) == (2, 2):          # The chapel and the graves.
        houses(6)
        for y in range(22, 28, 2):
            for x in range(30, 40, 2):
                if free(x, y):
                    g[y][x] = 'R'
        put('T', 4, 2)
    else:                           # The sheep meadow, fenced; hedges, flowers.
        houses(3)
        put('B', 4, 3)
        put(',', 4, 4)
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
        for (x, y) in HOMES.get((c, r), []):
            assert all(g[y + dy][x + dx] == '.' and (x + dx, y + dy) not in near for dx in (0, 1) for dy in (0, 1)), ((c, r), x, y)
            house(g, x, y)
        if (c, r) in PENS:
            (a, b, inside, _) = PENS[(c, r)]
            assert not any((x, y) in near for x in range(a[0], b[0] + 1) for y in range(a[1], b[1] + 1)), (c, r)
            pen(g, a, b, inside)
        beasts = PENS.get((c, r), (0, 0, 0, []))[3] + LOOSE.get((c, r), [])
        people = [p[4] for p in PEOPLE.get((c, r), [])] + [b[0] for b in BENCHES.get((c, r), [])] + [p[1] for p in PROPS.get((c, r), [])]
        assert all(g[y][x] == '.' and (x, y) not in road for (x, y) in people), (c, r)
        assert all(g[y][x] == '.' for (_, (x, y)) in beasts), (c, r)
        keep = near | {(x + dx, y + dy) for (x, y) in people for dx in (-2, -1, 0, 1, 2) for dy in (-2, -1, 0, 1, 2)} \
            | {(x + dx, y + dy) for (_, (x, y)) in beasts for dx in (-1, 0, 1) for dy in (-1, 0, 1)}

        def free(x, y, g=g, keep=keep):
            return 1 <= x < W - 1 and 1 <= y < H - 1 and g[y][x] == '.' and (x, y) not in keep

        theme(rnd, g, c, r, free, sorted(road))
        rows = [''.join(row) for row in g]
        seen = reachable(rows, CENTRE)
        musts = [(l["at"]["x"], l["at"]["y"]) for l in links] + people
        assert all(m in seen or any(n in seen for n in ((m[0] + 1, m[1]), (m[0] - 1, m[1]), (m[0], m[1] + 1), (m[0], m[1] - 1))) for m in musts), (c, r)
        town = O([("id", mid(c, r)), ("name", NAMES[(c, r)]), ("level", 1), ("mapAt", cell(c, r - 1)), ("zone", "clairval"),
                  ("rows", rows), ("spawn", cell(*CENTRE)),
                  ("npcs", [O([("id", i), ("name", n), ("look", look), ("colour", col), ("at", cell(*at)), ("dialogue", d)])
                            for (i, n, look, col, at, d) in PEOPLE.get((c, r), [])]),
                  ("links", links), ("exits", [])])
        if (c, r) in PROPS:
            town["props"] = [O([("id", i), ("at", cell(*at)), ("look", look), ("name", n), ("text", t)] + ([("fight", f)] if f else [])) for (i, at, look, n, t, f) in PROPS[(c, r)]]
        if beasts:
            town["animals"] = [O([("kind", k), ("at", cell(*at))]) for (k, at) in beasts]
        if (c, r) in BENCHES:
            town["workshops"] = [O([("at", cell(*at)), ("job", job)]) for (at, job) in BENCHES[(c, r)]]
        maps[mid(c, r)] = town

for old in glob.glob(f"{root}/towns/clairval*.json"):
    os.remove(old)
for i, m in maps.items():
    open(f"{root}/towns/{i}.json", "w").write(json.dumps(m, ensure_ascii=False, indent=2) + "\n")
print(f"{len(maps)} maps of the village written")
