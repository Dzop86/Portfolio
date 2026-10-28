# The crests' pictograms (sprint 156, D83, question 82 a): thirty game-icons.net drawings (CC BY 3.0), their ids those of
# GuildCrests.Icons in src/Rpg.Core/GuildHall.cs, in godot/assets/icons/crest/<id>.svg, the black square taken off and the
# shape in white (the game tints it with the crest's charge), their authors in credits.json. The art direction may redraw
# them under the same ids. Deterministic; --fetch downloads the drawings missing, otherwise every one must be there.
#   python3 tools/crest_icons.py [--fetch]
import json, os, re, sys, urllib.request

fetch = "--fetch" in sys.argv
out = os.path.join(os.path.dirname(os.path.abspath(__file__)), "..", "godot", "assets", "icons", "crest")

ICONS = {
    "oak-leaf": "delapouite/oak-leaf", "pine-tree": "lorc/pine-tree", "acorn": "lorc/acorn", "wheat": "lorc/wheat",
    "rose": "lorc/rose", "mushroom": "lorc/mushroom", "wolf-head": "lorc/wolf-head", "boar": "caro-asercion/boar",
    "stag-head": "lorc/stag-head", "bear-head": "delapouite/bear-head", "owl": "caro-asercion/barn-owl",
    "eagle-head": "delapouite/eagle-head", "fox-head": "lorc/fox-head", "bee": "lorc/bee", "fish": "delapouite/circling-fish",
    "snake": "lorc/snake", "dragon-head": "lorc/dragon-head", "crown": "lorc/crown", "castle": "lorc/castle", "key": "lorc/key",
    "swords": "lorc/crossed-swords", "axe": "lorc/battle-axe", "bow": "delapouite/bow-arrow", "shield": "lorc/checked-shield",
    "hammer": "delapouite/thor-hammer", "anvil": "lorc/anvil", "book": "delapouite/book-cover", "lantern": "lorc/lantern-flame",
    "sun": "caro-asercion/heraldic-sun", "moon": "lorc/moon",
}


def white(svg):
    """The drawing without its black square, its shape in white."""
    svg = re.sub(r'<path d="M0 0h512v512H0z"\s*/>', "", svg)
    return re.sub(r'fill="#[0-9a-fA-F]{3,6}"', 'fill="#ffffff"', svg)


core = open(os.path.join(os.path.dirname(out), "..", "..", "..", "src", "Rpg.Core", "GuildHall.cs")).read()
listed = re.findall(r'"([a-z-]+)"', core[core.index("Icons ="):core.index("];", core.index("Icons ="))])
assert listed == list(ICONS), f"GuildCrests.Icons and this list differ: {set(listed) ^ set(ICONS)}"
os.makedirs(out, exist_ok=True)
for name, icon in ICONS.items():
    path = os.path.join(out, f"{name}.svg")
    if not os.path.exists(path):
        assert fetch, f"{path} missing: run with --fetch"
        url = f"https://raw.githubusercontent.com/game-icons/icons/master/{icon}.svg"
        open(path, "w").write(white(urllib.request.urlopen(url, timeout=30).read().decode()))
open(os.path.join(out, "credits.json"), "w").write(json.dumps(ICONS, ensure_ascii=False, indent=1, sort_keys=True) + "\n")
print(f"{len(ICONS)} pictograms")
