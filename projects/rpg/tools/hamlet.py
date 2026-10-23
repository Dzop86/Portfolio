# Quatre-Chemins (sprint 129, D79, docs/suite-apres-d69.md §11.6, its words in docs/textes-v7.md): 2 x 2 maps of 48 x 32
# cells at x 4 and 5, y -1 and 0 of the world, east of Clairval, whose east gate (Craftsmen's Row) leads to the square,
# the south-west map. The roads leaving Clairval part here: the north road (Élyon, the Air) is shut by a rockfall, the
# east road (Kaor, the Fire) by a blaze, the south-west road to the river (Névara, the Water) by a flooded ford, each
# a thing to examine; the south road (Dhoram, the Earth) is open: it ran to the palisade until sprint 131, and goes
# through it into the Forest of the Ancestors since (tools/world_gen.py). Called by tools/village_gen.py, with its helpers.
import random

COLS, ROWS = 2, 2
AT = (4, -1)            # the world's place of the north-west map

# The scriptwriter's words (docs/textes-v7.md §1 to §5), its ids "fourways" written "four-ways" as the code's.
def text(fr, en): return {"fr": fr, "en": en}


NAMES = {
    (0, 0): text("Quatre-Chemins, le chemin creux", "Four Ways, the Sunken Lane"),
    (1, 0): text("Quatre-Chemins, la lande fumante", "Four Ways, the Smoking Heath"),
    (0, 1): text("Quatre-Chemins, la place du relais", "Four Ways, Inn Square"),
    (1, 1): text("Quatre-Chemins, la maison des routes", "Four Ways, the Road House"),
}
# id, name, look, outfit colour, cell, dialogue (§2).
PEOPLE = {
    (0, 1): [("lucien", text("Lucien", "Lucien"), "male-e", 3, (30, 12), "lucien"),
             ("josette", text("Josette", "Josette"), "female-e", 1, (18, 12), "josette"),
             ("aristide", text("Aristide", "Aristide"), "male-f", 4, (28, 24), "aristide")],
    (0, 0): [("remi", text("Rémi", "Rémi"), "male-d", 2, (26, 7), "remi")],
    (1, 0): [("ghislaine", text("Ghislaine", "Ghislaine"), "female-c", 0, (38, 12), "ghislaine")],
    (1, 1): [("anatole", text("Anatole", "Anatole"), "male-c", 6, (28, 10), "anatole"),
             ("romane", text("Romane", "Romane"), "female-a", 5, (26, 27), "romane")],
}
# Their houses: the inn by Josette, Ghislaine's hut, Anatole's house (the door faces south, its master before it).
HOMES = {(0, 1): [(17, 9)], (1, 0): [(37, 9)], (1, 1): [(28, 8)]}
# Things to examine (§3, §5.2): the signpost on the square; the waystone is zone_people.py's, by the arrival.
PROPS = {(0, 1): [('four-armed-signpost', (27, 12), 'signpost', text('Le poteau aux quatre bras', 'The four-armed signpost'), text("Un poteau de chêne à quatre bras, sans un seul nom : seulement des signes. La graine montre le sud, la goutte le sud-ouest, le marteau l'est, la plume le nord. Le bois est neuf ; les signes ont été recopiés d'un poteau plus vieux, et celui-là d'un plus vieux encore.", 'An oak signpost with four arms and not a single name: only signs. The seed points south, the drop south-west, the hammer east, the feather north. The wood is new; the signs were copied from an older post, and that one from an older one still.'))]}
# What shuts each road, standing on the road's last cell (§5.2: "on the road, at the edge of its map").
BARRIERS = {
    (0, 0): ('north-rockfall', (24, 5), 'rockfall', text("L'éboulis", 'The rockfall'), text("Un pan du chemin creux s'est couché en travers de la route du nord : des blocs plus gros qu'une charrette, empilés jusqu'en haut du talus. Entre eux, le vent siffle sans arrêt. Personne ne passera par ici, ni à pied ni à cheval.", "A whole bank of the sunken lane has come down across the north road: blocks bigger than a cart, heaped to the top of the bank. The wind whistles between them without stopping. Nobody's getting through here, on foot or on horseback.")),
    (1, 0): ('east-blaze', (42, 16), 'blaze', text('Le brasier', 'The blaze'), text("La lande brûle en travers de la route de l'est, sur cent pas de large. Le feu ne gagne pas la bruyère d'à côté, et il ne baisse pas non plus : il reste sur la route. On sent sa chaleur d'ici.", "The heath is burning right across the east road, a hundred paces wide. The fire doesn't spread to the heather beside it, and it doesn't die down either: it stays on the road. You can feel the heat from here.")),
    (0, 1): ('flooded-ford', (24, 26), 'ford', text('Le gué en crue', 'The flooded ford'), text("D'habitude, on traverse ici avec de l'eau aux genoux. Aujourd'hui, la rivière roule large et brune, et charrie des joncs arrachés au Lac. Le piquet qui marquait le gué a disparu sous l'eau.", 'Usually you cross here with the water at your knees. Today the river runs wide and brown, carrying reeds torn from the lake. The post that marked the ford has gone under.')),
}
SQUARE = (0, 1)
# The south road's cells through the road house's palisade, onto the forest's map at [5, 1] (tools/world_gen.py).
ROAD_SOUTH = (23, 24, 25)


def mid(c, r):
    return "four-ways" if (c, r) == SQUARE else f"four-ways-{c}-{r}"


def maps(v):
    W, H, O, cell = v.W, v.H, v.O, v.cell
    out = {}
    for r in range(ROWS):
        for c in range(COLS):
            rnd = random.Random(1290 + 10 * r + c)
            g = [['.'] * W for _ in range(H)]
            for x in range(W):
                g[0][x] = g[H - 1][x] = 'X'
            for y in range(H):
                g[y][0] = g[y][W - 1] = 'X'
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
            links, ways, blocked = [], [], set()
            if (c, r) == SQUARE:
                # The way back to Clairval, its east gate's rows.
                for y in v.GATE:
                    g[y][0] = '='
                    links.append(O([("at", cell(0, y)), ("to", "clairval-3-1"), ("arrival", cell(W - 2, y)), ("name", v.text("Clairval", "Clairval"))]))
                ways.append((0, 16))
            for (dc, dr, edge) in ((-1, 0, "west"), (1, 0, "east"), (0, -1, "north"), (0, 1, "south")):
                nc, nr = c + dc, r + dr
                if not (0 <= nc < COLS and 0 <= nr < ROWS):
                    continue
                gap = v.SIDE_GAP if dr == 0 else v.DOWN_GAP
                for k in gap:
                    (x, y) = {"west": (0, k), "east": (W - 1, k), "north": (k, 0), "south": (k, H - 1)}[edge]
                    (ax, ay) = {"west": (W - 2, k), "east": (1, k), "north": (k, H - 2), "south": (k, 1)}[edge]
                    g[y][x] = '='
                    links.append(O([("at", cell(x, y)), ("to", mid(nc, nr)), ("arrival", cell(ax, ay))]))
                ways.append({"west": (0, 16), "east": (W - 1, 16), "north": (24, 0), "south": (24, H - 1)}[edge])
            # The roads out, each to where it stops: the rockfall, the blaze, the flooded ford; the south road goes on into the forest.
            if (c, r) == (0, 0):
                ways.append((24, 5))
                blocked = {(x, y) for x in range(19, 30) for y in range(1, 5)}
                for x in range(22, 27):
                    g[0][x] = '='
            elif (c, r) == (1, 0):
                ways.append((W - 6, 16))
                blocked = {(x, y) for x in range(W - 5, W - 1) for y in range(12, 21)}
                for y in range(15, 18):
                    g[y][W - 1] = '='
            elif (c, r) == SQUARE:
                ways.append((24, H - 6))
                for x in range(18, 31):
                    for y in range(H - 5, H - 1):
                        g[y][x] = '~'
                for x in range(22, 27):
                    g[H - 1][x] = '='
            elif (c, r) == (1, 1):
                for x in ROAD_SOUTH:
                    g[H - 1][x] = '='
                    links.append(O([("at", cell(x, H - 1)), ("to", "ancestors-1-0"), ("arrival", cell(x, 1)),
                                    ("name", v.text("Forêt des Aïeux", "Forest of the Ancestors"))]))
                ways.append((24, H - 1))
            road = set()
            for w in ways:
                road |= set(v.lpath(w, v.CENTRE))
            for (x, y) in road:
                g[y][x] = '='
            for (x, y) in blocked:
                g[y][x] = 'R'
            near = {(x + dx, y + dy) for (x, y) in road for dx in (-1, 0, 1) for dy in (-1, 0, 1)}
            for (x, y) in HOMES.get((c, r), []):
                assert all(g[y + dy][x + dx] == '.' and (x + dx, y + dy) not in near for dx in (0, 1) for dy in (0, 1)), ((c, r), x, y)
                v.house(g, x, y)
            people = [p[4] for p in PEOPLE.get((c, r), [])] + [p[1] for p in PROPS.get((c, r), [])]
            assert all(g[y][x] == '.' and (x, y) not in road for (x, y) in people), ((c, r), people)
            assert (c, r) not in BARRIERS or BARRIERS[(c, r)][1] in road, (c, r)
            keep = near | blocked | {(x + dx, y + dy) for (x, y) in people for dx in (-2, -1, 0, 1, 2) for dy in (-2, -1, 0, 1, 2)}

            def free(x, y, g=g, keep=keep):
                return 1 <= x < W - 1 and 1 <= y < H - 1 and g[y][x] == '.' and (x, y) not in keep

            theme(rnd, g, c, r, free, sorted(road), v)
            rows = [''.join(row) for row in g]
            seen = v.reachable(rows, v.CENTRE)
            musts = [(l["at"]["x"], l["at"]["y"]) for l in links] + people
            assert all(m in seen or any(n in seen for n in ((m[0] + 1, m[1]), (m[0] - 1, m[1]), (m[0], m[1] + 1), (m[0], m[1] - 1))) for m in musts), (c, r)
            town = O([("id", mid(c, r)), ("name", NAMES[(c, r)]), ("level", 30), ("mapAt", cell(AT[0] + c, AT[1] + r)), ("zone", "four-ways"),
                      ("rows", rows), ("spawn", cell(*v.CENTRE)),
                      ("npcs", [O([("id", i), ("name", n), ("look", look), ("colour", col), ("at", cell(*at)), ("dialogue", d)])
                                for (i, n, look, col, at, d) in PEOPLE.get((c, r), [])]),
                      ("links", links), ("exits", [])])
            shown = PROPS.get((c, r), []) + ([BARRIERS[(c, r)]] if (c, r) in BARRIERS else [])
            if shown:
                town["props"] = [O([("id", i), ("at", cell(*at)), ("look", look), ("name", n), ("text", t)]) for (i, at, look, n, t) in shown]
            out[mid(c, r)] = town
    return out


def theme(rnd, g, c, r, free, road_cells, v):
    """Houses along the streets, a few trees and hedges; lanterns on the square."""
    W, H = v.W, v.H
    spots = [(x, y) for x in range(2, W - 3) for y in range(2, H - 3)]
    rnd.shuffle(spots)
    spots.sort(key=lambda p: min(abs(p[0] - rx) + abs(p[1] - ry) for (rx, ry) in road_cells) // 3)
    count = 8 if (c, r) == SQUARE else 6
    for (x, y) in spots:
        if count == 0:
            break
        if all(free(x + dx, y + dy) for dx in range(-1, 3) for dy in range(-1, 3)):
            v.house(g, x, y)
            count -= 1
    # What each map shows (docs/textes-v7.md §1): the square's lanterns, Lucien's stall and Aristide's cart; the sunken
    # lane's fallen blocks; the heath's broom and cold charcoal mounds; the road house's old milestones in a row.
    extra = {
        SQUARE: [('L', (21, 19)), ('L', (27, 19)), ('S', (34, 11)), ('S', (34, 14)), ('C', (32, 24))],
        (0, 0): [('R', (8, 9)), ('R', (9, 10)), ('R', (36, 8)), ('R', (37, 20)), ('C', (30, 10))],
        (1, 0): [('R', (30, 22)), ('R', (34, 24)), ('R', (12, 22))],
        (1, 1): [('R', (34, 6)), ('R', (36, 6)), ('R', (38, 6)), ('R', (40, 6))],
    }
    for (ch, (x, y)) in extra.get((c, r), []):
        if free(x, y):
            g[y][x] = ch
    heath = (('B', 14, 4),) if (c, r) == (1, 0) else ()
    for ch, n, size in (('T', 6, 2), ('B', 4, 3)) + heath:
        for _ in range(n):
            x, y = rnd.randrange(3, W - 3), rnd.randrange(3, H - 3)
            for _ in range(size):
                if free(x, y):
                    g[y][x] = ch
                x, y = x + rnd.randint(-1, 1), y + rnd.randint(-1, 1)
