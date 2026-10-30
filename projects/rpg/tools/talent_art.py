# The talents' icons in relief (sprint 164, docs/graphisme/s163-icones-arbre.md §3.5, s164-arbre-maquette.md §3.4): each
# talent's game-icons.net silhouette (CC BY 3.0, its author in talents/credits.json) with the menus' layers (tools/ui_art.py),
# no tile (the tree's node is the tile), in three files: godot/assets/icons/talents/relief/<id>.svg in its family's colour
# (taken), <id>-open.svg in the light greys of the menu (open), <id>-locked.svg flat grey (not yet, closed).
# The colour of a family, never pistachio (pistachio says "taken"): a Fork's choice in its compound's, a Heart in its element's,
# the Braids (the Fusion) in Lightning's, a style in its branch's (ElementStyle.StyleColour), the trunk in the achievements'
# gold, anything else in the journal's parchment. The silhouettes' paths in godot/assets/icons/bar/shapes/, taken from the
# white drawings of sprint 149 (talents/<id>.svg) when missing. Deterministic, no <filter>. Run from projects/rpg:
#   python3 tools/talent_art.py
import json, os, re, sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from ui_art import layers, mix, bar, PLACE  # noqa: E402
from spell_art import MAIN  # noqa: E402

here = os.path.dirname(os.path.abspath(__file__))
root = os.path.join(here, "..")
talents = os.path.join(root, "godot", "assets", "icons", "talents")
out = os.path.join(talents, "relief")

# A Fork's choice and the compound it makes (data/reactions: Earth + Water is Mud, and so on).
FORK = {"bogging": "Mud", "chilling": "Ice", "cracking": "Magma", "electrifying": "Lightning", "fogging": "Steam", "blinding": "Sand"}
STYLE = {"Melee": "#e8a33d", "Distance": "#6bb8f2", "Support": "#ff7eb6", "Terrain": "#c9a27f", "Tempo": "#b48cff", "Alchemy": "#4fd1c5"}
GOLD = {"main": "#ffd166", "light": "#ffe6ab", "dark": "#8c7338", "outline": "#332a14"}
PARCHMENT = "#f0e6cc"
# The open node's icon: the menu's light greys (palette "menu").
OPEN = {"light": "#e5e5e5", "main": "#cfcfcf", "dark": "#727272", "outline": "#292929"}
LOCKED = "#7a7a7a"


def tones(main):
    """The relief's tones from one colour (s157 §1.3)."""
    return {"main": main, "light": mix(main, "#ffffff", 0.45), "dark": mix(main, "#000000", 0.45), "outline": mix(main, "#000000", 0.80)}


def colour(talent):
    """Its family's tones (§3.5)."""
    branch, tid = talent.get("branch"), talent["id"]
    if tid.startswith("trunk-"):
        return GOLD
    if tid in FORK:
        return tones(MAIN[FORK[tid]])
    if branch == "Pure":
        return tones(MAIN[next(r["element"] for r in talent["rules"] if r.get("kind") == "element")])
    if branch == "Braids":
        return tones(MAIN["Lightning"])
    return tones(STYLE.get(branch, PARCHMENT))


def ensure_shape(name, tid):
    """The silhouette's path, taken from the white drawing of the same talent when it is not kept yet."""
    path = os.path.join(bar, "shapes", name.replace("/", "_") + ".path")
    if not os.path.exists(path):
        white = open(os.path.join(talents, tid + ".svg")).read()
        paths = [d for d in re.findall(r'<path[^>]* d="([^"]+)"', white) if d != "M0 0h512v512H0z"]
        if not paths:
            sys.exit(f"no path in {tid}.svg for {name}")
        open(path, "w").write(" ".join(paths) + "\n")


def svg(body):
    return f'<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 512 512">{body}</svg>\n'


def main():
    data = json.load(open(os.path.join(root, "data", "talents.json")))
    credits = json.load(open(os.path.join(talents, "credits.json")))
    # The trunk's nodes are not talents: their family is the trunk's.
    every = data + [{"id": t, "branch": "Trunk", "rules": []} for t in sorted(credits) if t.startswith("trunk-")]
    os.makedirs(out, exist_ok=True)
    for t in every:
        tid = t["id"]
        name = t.get("icon") or credits[tid]
        if not tid.startswith("trunk-"):
            credits[tid] = name
        ensure_shape(name, tid)
        tone = dict(colour(t), shape=name)
        open(os.path.join(out, tid + ".svg"), "w").write(svg(layers("t", tone, PLACE, 48, (12, 28))))
        open(os.path.join(out, tid + "-open.svg"), "w").write(svg(layers("t", dict(OPEN, shape=name), PLACE, 48, (12, 28))))
        d = open(os.path.join(bar, "shapes", name.replace("/", "_") + ".path")).read().strip()
        open(os.path.join(out, tid + "-locked.svg"), "w").write(svg(f'<g transform="{PLACE}"><path d="{d}" fill="{LOCKED}"/></g>'))
    json.dump(credits, open(os.path.join(talents, "credits.json"), "w"), indent=1, ensure_ascii=False)
    open(os.path.join(talents, "credits.json"), "a").write("\n")
    print(f"{len(every)} talents, {3 * len(every)} files")


if __name__ == "__main__":
    main()
