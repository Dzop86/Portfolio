# Writes the large zones of sprint 60 (data/towns/clairval-woods.json, misty-heath.json) and opens
# Clairval's west way. Deterministic: the same seed gives the same zones. Run from projects/rpg:
#   python3 tools/zones_gen.py data
import json, random, sys, collections
root = sys.argv[1]
O = collections.OrderedDict

def zone(w, h, seed, openings, path, decor):
    """A grid bordered with trees, open at the openings; the path cells kept free; decor scattered."""
    rnd = random.Random(seed)
    g = [['.'] * w for _ in range(h)]
    for x in range(w):
        g[0][x] = g[h - 1][x] = 'T'
    for y in range(h):
        g[y][0] = g[y][w - 1] = 'T'
    for (x, y) in openings:
        g[y][x] = '='
    keep = set(path) | set(openings)
    for (ch, count, size) in decor:
        for _ in range(count):
            cx, cy = rnd.randrange(2, w - 2), rnd.randrange(2, h - 2)
            for _ in range(size):
                x, y = cx + rnd.randint(-1, 1), cy + rnd.randint(-1, 1)
                if 1 <= x < w - 1 and 1 <= y < h - 1 and (x, y) not in keep and all(abs(x - px) + abs(y - py) > 1 for (px, py) in keep):
                    g[y][x] = ch
    for (x, y) in path:
        g[y][x] = '='
    return [''.join(r) for r in g]

def walk(points):
    """The cells of straight runs between the points, horizontal then vertical."""
    cells = []
    for (a, b) in zip(points, points[1:]):
        (x, y), (tx, ty) = a, b
        while x != tx:
            cells.append((x, y)); x += 1 if tx > x else -1
        while y != ty:
            cells.append((x, y)); y += 1 if ty > y else -1
    cells.append(points[-1])
    return cells

def reachable(rows, start):
    seen, todo = {start}, [start]
    while todo:
        x, y = todo.pop()
        for (dx, dy) in ((1, 0), (-1, 0), (0, 1), (0, -1)):
            n = (x + dx, y + dy)
            if 0 <= n[1] < len(rows) and 0 <= n[0] < len(rows[0]) and n not in seen and rows[n[1]][n[0]] in '.=':
                seen.add(n); todo.append(n)
    return seen

def cell(x, y): return O([("x", x), ("y", y)])
def text(fr, en): return O([("fr", fr), ("en", en)])

woods_path = walk([(29, 11), (20, 11), (20, 6), (12, 6), (12, 3), (15, 3), (15, 0)])
woods = zone(30, 22, 60, [(29, 11), (15, 0)], woods_path + walk([(20, 11), (20, 16), (8, 16), (8, 6), (12, 6)]),
             [('T', 26, 6), ('R', 10, 2), ('~', 3, 5)])
heath_path = walk([(15, 25), (15, 18), (6, 18), (6, 8), (22, 8), (22, 14), (15, 14)])
heath = zone(30, 26, 61, [(15, 25)], heath_path, [('R', 22, 3), ('~', 6, 6), ('T', 8, 3)])
for name, rows, start, must in (("woods", woods, (28, 11), [(15, 1), (8, 6)]), ("heath", heath, (15, 24), [(6, 8), (22, 14)])):
    r = reachable(rows, start)
    assert all(m in r for m in must), name

zones = {
    "clairval-woods": O([("id", "clairval-woods"), ("name", text("Bois de Clairval", "Clairval Woods")), ("level", 3), ("mapAt", cell(1, 1)),
        ("rows", woods), ("spawn", cell(28, 11)), ("npcs", []),
        ("exits", [O([("at", cell(8, 6)), ("scenario", "training"), ("name", text("Clairière : entraînement", "Glade: training"))])]),
        ("links", [O([("at", cell(29, 11)), ("to", "clairval"), ("arrival", cell(1, 5)), ("name", text("Est : Clairval", "East: Clairval"))]),
                   O([("at", cell(15, 0)), ("to", "misty-heath"), ("arrival", cell(15, 24)), ("name", text("Nord : Lande des Brumes", "North: Misty Heath"))])])]),
    "misty-heath": O([("id", "misty-heath"), ("name", text("Lande des Brumes", "Misty Heath")), ("level", 10), ("mapAt", cell(1, 0)),
        ("rows", heath), ("spawn", cell(15, 24)), ("npcs", []), ("exits", []),
        ("links", [O([("at", cell(15, 25)), ("to", "clairval-woods"), ("arrival", cell(15, 1)), ("name", text("Sud : Bois de Clairval", "South: Clairval Woods"))])])]),
}

def dump(z):
    lines = json.dumps(z, ensure_ascii=False, indent=2).split('\n')
    return '\n'.join(lines) + '\n'

for zid, z in zones.items():
    open(f"{root}/towns/{zid}.json", "w").write(dump(z))

print("zones written (Clairval's west way and map place are written by hand in clairval.json)")
