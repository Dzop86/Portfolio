# The items' tiles in relief (sprint 166, D87, docs/graphisme/s165-retours-30-oct.md §4): the spells' recipe (tools/spell_art.py)
# on a neutral tile, the silhouette in the colour of what the item is made of, not of its element. Layers: a drop shadow, a
# dark edge, a rim in the colour of where the item comes from (grey, light chocolate for a craftsman's, violet for a set's
# piece, gold for a chief's), a neutral diagonal body, a bevel, the item's game-icons.net drawing (CC BY 3.0, the white
# shapes of tools/icons_gen.py in godot/assets/icons/items/) with the menus' layers (tools/ui_art.py) in its matter's
# tones, and for equipment a dot of its strongest characteristic's element in the top right corner.
# Files: godot/assets/icons/items/tiles/<id>.svg and <id>-locked.svg (flat: an item that cannot be worn now), slot-<slot>.svg
# for an empty slot (its kind's drawing, flat, in a hollow); items/palette.json the matter's colour of each item.
# The matter (§4.4): a gathered resource its trade's, nuanced by species; a craftsman's work that of its first ingredient;
# a monster's loot its nature; a set's piece its family's; food the bread's, a potion the heal's, a scroll parchment.
# Within one trade's recipes and among the items of one slot and level, no two share a drawing and a colour (§4.7): the
# later ones of a pair are nuanced, lighter then darker. Neither the pistachio nor an element's own colour is a matter.
# Deterministic, no <filter> (Godot draws with ThorVG). Run from projects/rpg after tools/icons_gen.py:
#   python3 tools/item_art.py
import json, os, re, sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from ui_art import layers, mix  # noqa: E402

here = os.path.dirname(os.path.abspath(__file__))
root = os.path.join(here, "..")
drawings = os.path.join(root, "godot", "assets", "icons", "items")
out = os.path.join(drawings, "tiles")

# The elements' colours (tokens.css, --osmose-*): the dot of an item's strongest characteristic, Vitality the heal's.
ELEMENT = {"vitality": "#ff7eb6", "earth": "#b07a45", "fire": "#ef4b4b", "water": "#3d9cff", "air": "#52c45a"}
FORBIDDEN = {"#bef374", "#b0b0b0", *ELEMENT.values()} - {"#ff7eb6"}

# Where it comes from (ItemStyle.Frame): the rim's colour; a chief's a gold gradient.
FRAME = {"Common": "#5c5c5c", "Craft": "#c9a27f", "Set": "#b48cff", "Chief": ("#ffe6ab", "#c9962e")}

# The matter of what is gathered or left by monsters, and of the pieces made by no one.
MATTER = {
    # Wood.
    "oak-log": "#c08a50", "hazel-log": "#d9b07a", "blackwood-log": "#6e5846", "yew-log": "#8a5a36", "willow-log": "#c9b48a",
    "alder-log": "#d4a46a", "ash-log": "#e0c9a0", "linden-log": "#e6cf96", "elm-log": "#9c6b43", "hornbeam-log": "#b8a07a",
    # Ore and stone.
    "copper-ore": "#d0804a", "tin-ore": "#b9c2c9", "iron-ore": "#8f9aa6", "sulphur": "#e8d84a", "blue-clay": "#6f9fc8",
    "zinc-ore": "#9fb3b0", "silver-ore": "#d8dde3", "quartz": "#e8e4f4", "granite": "#a39a94", "malachite": "#3fae7f",
    # Fish.
    "gudgeon": "#a8b8a0", "crayfish": "#c25a3a", "pool-eel": "#7d8a56", "tench": "#9aa860", "perch": "#b0c070",
    "carp": "#d0a050", "pike": "#7f9a7a", "zander": "#a0a8b0", "trout": "#c7d3dc", "catfish": "#8a8070",
    # Crops.
    "wild-flax": "#9fb4d8", "wild-oats": "#e3c27a", "pool-hemp": "#8fae6a", "rye": "#c9a860", "shore-sedge": "#a8b870",
    "reed-rice": "#efe2b8", "clairval-wheat": "#ecc35c", "buckwheat": "#b88f5c", "spelt": "#d8b070", "sorghum": "#c0703c",
    # Herbs.
    "nettle": "#4f9a4a", "glowmoss": "#7fd0a0", "water-lily": "#f0a8c8", "hive-flower": "#ffcf5a", "sage": "#9fb8a0",
    "mandrake": "#b08a6a", "lavender": "#a08ad8", "clubmoss": "#5f8f4f", "horsetail": "#7fa86a", "wood-mint": "#6fcf9f",
    # What the monsters leave, and what is bought.
    "feather": "#f0e6cc", "leather": "#a0522d", "orc-fang": "#e8dcc0", "bee-wing": "#d8eef8", "boar-bristle": "#7a5c48",
    "boar-tusk": "#e8dcc0", "crab-claw": "#c25a3a", "crab-shell": "#d0704a", "earthworm": "#e08a8a", "frog-leg": "#7fae5a",
    "honeycomb": "#f2b84a", "keeper-moss": "#6f9a50", "mole-fur": "#6a5a58", "mossy-cobble": "#8a9a7a", "mushroom-cap": "#c86a4a",
    "old-bone": "#e0d6c0", "rotten-cloth": "#8a8a6a", "slime": "#7fd0b0", "spore-dust": "#c8b8a8", "stag-velvet": "#a07850",
    "stone-antler": "#b0a898", "wolf-fang": "#f0e8d8", "wolf-pelt": "#8f8f98",
    # The pieces made by no one, nor of a set.
    "copper-ring": "#d0804a", "ember-ring": "#e8803d", "pebble-amulet": "#a39a94", "leather-belt": "#a0522d",
    "orc-breastplate": "#7a8a6a", "orc-pauldrons": "#7a8a6a", "dagger": "#8f9aa6", "orc-club": "#8a6a42",
    "wooden-shield": "#c08a50", "kitten": "#e8a860", "donkey": "#a0928a",
}
# A set's pieces: its family's colour.
SET = {"poacher": "#6f8f5a", "wolf": "#9aa3ad", "boar": "#8a6a52", "frog": "#5fa88a", "bee": "#f2c24a", "crab": "#d0603a",
       "mole": "#7a6a60", "mushroom": "#c8705a", "zombie": "#8fa08a", "stag": "#a8743f", "golem": "#8a9a8a"}
BREAD, HEAL, PARCHMENT = "#f2cc94", "#ff7eb6", "#f0e6cc"

# An empty slot: its kind's drawing.
SLOTS = {"Ring": "ring", "Amulet": "gem-pendant", "Belt": "belt", "Cape": "cape", "Hat": "robin-hood-hat", "Chest": "breastplate",
         "Shoulders": "pauldrons", "OneHanded": "plain-dagger", "TwoHanded": "battle-axe", "Shield": "viking-shield", "Pet": "cat",
         "Mount": "donkey", "Boots": "leather-boot"}

SILHOUETTE = "translate(106 96) scale(0.586)"
STATS = ["vitality", "earth", "fire", "water", "air"]


def load(name):
    return json.load(open(os.path.join(root, "data", name)))


def drawing(icon):
    """The white drawing of tools/icons_gen.py, its paths joined."""
    svg = open(os.path.join(drawings, icon.split("/")[-1] + ".svg")).read()
    return " ".join(re.findall(r'<path[^>]* d="([^"]+)"', svg))


def provenance(item, items, monsters, jobs):
    """ItemStyle.ProvenanceOf: a chief's piece (left by chiefs alone), a set's, a craftsman's work, or common."""
    droppers = [m for f in monsters for m in f["monsters"] if any(l["item"] == item["id"] for l in m.get("loot", []))]
    if item["kind"] == "Equipment" and droppers and all(m.get("role") == "Boss" for m in droppers):
        return "Chief"
    if item.get("set"):
        return "Set"
    return "Craft" if any(r["item"] == item["id"] for j in jobs for r in j["recipes"]) else "Common"


def element(item):
    """ItemStyle.Element: the strongest characteristic's colour (ties in the sheet's order), none without one."""
    stats = item.get("stats") or {}
    best = max(STATS, key=lambda s: (stats.get(s, 0), -STATS.index(s)))
    return ELEMENT[best] if item["kind"] == "Equipment" and stats.get(best, 0) > 0 else None


def matter(item, first):
    """The colour of what the item is made of (§4.4)."""
    i = item["id"]
    if item["kind"] == "Quest":
        return PARCHMENT
    if item["kind"] == "Consumable":
        return PARCHMENT if "scroll" in i else HEAL if "potion" in i or "elixir" in i else BREAD
    if item.get("set"):
        return SET[item["set"]]
    if i in MATTER:
        return MATTER[i]
    return MATTER[first[i]]


def tones(main):
    """The menus' tones of a colour (s157 §1.3)."""
    return {"main": main, "light": mix(main, "#ffffff", 0.45), "dark": mix(main, "#000000", 0.45),
            "outline": "#" + "".join(f"{int(main[k:k + 2], 16) * 20 // 100:02x}" for k in (1, 3, 5))}


def nuance(colours, groups):
    """Within each group, a drawing shared by two items keeps two colours: the later ones lighter, then darker (§4.7)."""
    for group in groups:
        seen = set()
        for i, icon in group:
            step = 0
            while (icon, colours[i]) in seen:
                step += 1
                base = colours[i]
                colours[i] = mix(base, "#ffffff", 0.22) if step % 2 else mix(base, "#000000", 0.22)
            seen.add((icon, colours[i]))
    return colours


def tile(rim, flat):
    """The tile (§4.3): shadow, edge, rim, neutral body, bevel; flat and grey when locked."""
    if flat:
        return ('<rect x="24" y="20" width="464" height="464" rx="80" fill="#0d0d0d"/>'
                '<rect x="36" y="32" width="440" height="440" rx="68" fill="#4c4c4c"/>'
                '<rect x="56" y="52" width="400" height="400" rx="48" fill="#141414"/>')
    rim_fill = rim
    defs = ('<linearGradient id="body" gradientUnits="userSpaceOnUse" x1="56" y1="52" x2="456" y2="452">'
            f'<stop offset="0" stop-color="#3a3a3d"/><stop offset="1" stop-color="{mix("#262628", "#000000", 0.2)}"/></linearGradient>'
            '<linearGradient id="bevel" gradientUnits="userSpaceOnUse" x1="0" y1="52" x2="0" y2="452">'
            '<stop offset="0" stop-color="#fff" stop-opacity="0.22"/><stop offset="0.45" stop-color="#fff" stop-opacity="0"/></linearGradient>')
    if isinstance(rim, tuple):
        defs += ('<linearGradient id="rim" gradientUnits="userSpaceOnUse" x1="0" y1="32" x2="0" y2="472">'
                 f'<stop offset="0" stop-color="{rim[0]}"/><stop offset="1" stop-color="{rim[1]}"/></linearGradient>')
        rim_fill = "url(#rim)"
    return (f'<defs>{defs}</defs><rect x="24" y="36" width="464" height="464" rx="80" fill="#000" opacity="0.45"/>'
            '<rect x="24" y="20" width="464" height="464" rx="80" fill="#0d0d0d"/>'
            f'<rect x="36" y="32" width="440" height="440" rx="68" fill="{rim_fill}"/>'
            '<rect x="56" y="52" width="400" height="400" rx="48" fill="url(#body)"/>'
            '<rect x="56" y="52" width="400" height="400" rx="48" fill="url(#bevel)"/>')


def dot(colour, flat):
    """The element's dot (§4.3 T7), top right."""
    if flat:
        return f'<circle cx="428" cy="84" r="44" fill="{mix(colour, "#141414", 0.6)}" stroke="#0d0d0d" stroke-width="12"/>'
    return ('<defs><linearGradient id="dot" x1="0" y1="0" x2="0" y2="1">'
            f'<stop offset="0" stop-color="{mix(colour, "#ffffff", 0.45)}"/><stop offset="1" stop-color="{mix(colour, "#000000", 0.45)}"/></linearGradient></defs>'
            '<circle cx="428" cy="84" r="44" fill="url(#dot)" stroke="#0d0d0d" stroke-width="12"/>'
            '<ellipse cx="428" cy="66" rx="28" ry="16" fill="#fff" opacity="0.35"/>')


def svg(body):
    return f'<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 512 512">{body}</svg>\n'


def icon(d, colour, rim, el, flat):
    body = tile(rim, flat)
    body += f'<g transform="{SILHOUETTE}"><path d="{d}" fill="#7a7a7a"/></g>' if flat else layers("m", {"d": d, **tones(colour)}, SILHOUETTE, 44, (12, 28))
    if el:
        body += dot(el, flat)
    return svg(body)


def empty(d):
    """An empty slot: its kind's drawing flat in a hollow."""
    return svg('<rect x="24" y="20" width="464" height="464" rx="80" fill="#0d0d0d"/>'
               '<rect x="36" y="32" width="440" height="440" rx="68" fill="#141414"/>'
               f'<g transform="{SILHOUETTE}"><path d="{d}" fill="#4c4c4c"/></g>')


def main():
    items = load("items.json")
    jobs, monsters = load("jobs.json"), load("monsters.json")
    first = {r["item"]: r["ingredients"][0]["item"] for j in jobs for r in j["recipes"] if r["ingredients"]}
    colours = {i["id"]: matter(i, first) for i in items}
    by_id = {i["id"]: i for i in items}
    groups = [[(r["item"], by_id[r["item"]]["icon"]) for r in j["recipes"]] for j in jobs]
    slots = {}
    for i in items:
        if i.get("slot"):
            slots.setdefault((i["slot"], i.get("level", 1)), []).append((i["id"], i["icon"]))
    colours = nuance(colours, groups + list(slots.values()))
    assert not FORBIDDEN & set(colours.values()), FORBIDDEN & set(colours.values())
    os.makedirs(out, exist_ok=True)
    for i in items:
        d, el = drawing(i["icon"]), element(i)
        rim = FRAME[provenance(i, items, monsters, jobs)]
        open(os.path.join(out, i["id"] + ".svg"), "w").write(icon(d, colours[i["id"]], rim, el, False))
        open(os.path.join(out, i["id"] + "-locked.svg"), "w").write(icon(d, colours[i["id"]], rim, el, True))
    for slot, name in SLOTS.items():
        open(os.path.join(out, f"slot-{slot.lower()}.svg"), "w").write(empty(drawing(name)))
    json.dump(colours, open(os.path.join(drawings, "palette.json"), "w"), indent=1, ensure_ascii=False, sort_keys=True)
    open(os.path.join(drawings, "palette.json"), "a").write("\n")
    print(f"{len(items)} items, {2 * len(items) + len(SLOTS)} files")


if __name__ == "__main__":
    main()
