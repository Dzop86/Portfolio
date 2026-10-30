# The spells' icons in relief (sprint 163, docs/graphisme/s163-icones-arbre.md §1): a tile in the colour of the spell's element
# with the frames' relief (shadow, dark edge, rim, diagonal body, bevel), its game-icons.net silhouette (CC BY 3.0) with the
# menus' layers (tools/ui_art.py), a dark element taking a light silhouette and a light one (Lightning, Ice, Sand) an engraved
# dark one; a compound's rim a gradient of its parents; a twin's origin dot, a Seal's element accent. Three files a spell:
# godot/assets/icons/spells/<id>.svg, <id>-locked.svg (flat: not unlocked, or not castable now), <id>-small.svg (no marks).
# The spells drawn: the classes', their twins, the tree's (data/spells.json, its "icon" the silhouette); their authors in
# spells/credits.json. The silhouettes' paths in godot/assets/icons/bar/shapes/; --fetch downloads those missing.
# Deterministic, no <filter> (Godot draws with ThorVG). Run from projects/rpg after tools/spells_gen.py:
#   python3 tools/spell_art.py [--fetch]
import json, os, re, sys, urllib.request

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from ui_art import layers, mix, bar  # noqa: E402

here = os.path.dirname(os.path.abspath(__file__))
root = os.path.join(here, "..")
out = os.path.join(root, "godot", "assets", "icons", "spells")
shapes = os.path.join(bar, "shapes")

# Each element's colour (src/assets/tokens.css, --osmose-*), and a compound's parents in the order of its reaction.
MAIN = {"Earth": "#b07a45", "Fire": "#ef4b4b", "Water": "#3d9cff", "Air": "#52c45a", "Neutral": "#b0b0b0", "Lightning": "#f2d03b",
        "Ice": "#8fe0f5", "Mud": "#8c7a3a", "Magma": "#ff7a2e", "Steam": "#c3a6e0", "Sand": "#d6bc84"}
PARENTS = {"Lightning": ("Fire", "Air"), "Ice": ("Water", "Air"), "Mud": ("Earth", "Water"), "Magma": ("Earth", "Fire"),
           "Steam": ("Fire", "Water"), "Sand": ("Earth", "Air")}
# A Seal's accent: its element's Heart (§1.9).
SEALS = {"seal-earth": ("Earth", "lorc/stone-sphere"), "seal-fire": ("Fire", "lorc/fire-ring"), "seal-water": ("Water", "lorc/drop"),
         "seal-air": ("Air", "lorc/whirlwind")}

SILHOUETTE = "translate(106 96) scale(0.586)"
ACCENT = "translate(300 36) scale(0.34)"


def luminance(c):
    """The relative luminance of #rrggbb (WCAG)."""
    def lin(v):
        v /= 255
        return v / 12.92 if v <= 0.03928 else ((v + 0.055) / 1.055) ** 2.4
    r, g, b = (int(c[i:i + 2], 16) for i in (1, 3, 5))
    return 0.2126 * lin(r) + 0.7152 * lin(g) + 0.0722 * lin(b)


def light(element):
    """A light element takes a light tile and an engraved silhouette (§1.3)."""
    return luminance(MAIN[element]) > 0.45


def shape(name, fetch):
    path = os.path.join(shapes, name.replace("/", "_") + ".path")
    if not os.path.exists(path):
        if not fetch:
            sys.exit(f"missing silhouette {name}: run with --fetch")
        svg = urllib.request.urlopen(f"https://raw.githubusercontent.com/game-icons/icons/master/{name}.svg", timeout=30).read().decode()
        paths = [d for d in re.findall(r'<path[^>]* d="([^"]+)"', svg) if d != "M0 0h512v512H0z"]
        open(path, "w").write(" ".join(paths) + "\n")
    return name


def tile(key, element, flat):
    """The tile T1 to T5 (§1.2): its colours from the element, flat when locked (§1.6)."""
    main = MAIN[element]
    rim = main if not light(element) else mix(main, "#000000", 0.45)
    if flat:
        rim, top, bottom = mix(main, "#1f1f1f", 0.60), mix(main, "#141414", 0.85), mix(main, "#141414", 0.85)
    elif light(element):
        top, bottom = mix(main, "#ffffff", 0.20), mix(main, "#000000", 0.20)
    else:
        top, bottom = mix(main, "#000000", 0.35), mix(main, "#000000", 0.70)
    defs = (f'<linearGradient id="body-{key}" gradientUnits="userSpaceOnUse" x1="56" y1="52" x2="456" y2="452">'
            f'<stop offset="0" stop-color="{top}"/><stop offset="1" stop-color="{bottom}"/></linearGradient>'
            f'<linearGradient id="bevel-{key}" gradientUnits="userSpaceOnUse" x1="0" y1="52" x2="0" y2="452">'
            f'<stop offset="0" stop-color="#fff" stop-opacity="0.22"/><stop offset="0.45" stop-color="#fff" stop-opacity="0"/></linearGradient>')
    rim_fill = rim
    if element in PARENTS and not flat:
        a, b = (MAIN[p] for p in PARENTS[element])
        defs += (f'<linearGradient id="rim-{key}" gradientUnits="userSpaceOnUse" x1="36" y1="32" x2="476" y2="472">'
                 f'<stop offset="0" stop-color="{a}"/><stop offset="1" stop-color="{b}"/></linearGradient>')
        rim_fill = f"url(#rim-{key})"
    body = ('' if flat else '<rect x="24" y="36" width="464" height="464" rx="80" fill="#000" opacity="0.45"/>')
    body += ('<rect x="24" y="20" width="464" height="464" rx="80" fill="#0d0d0d"/>'
             f'<rect x="36" y="32" width="440" height="440" rx="68" fill="{rim_fill}"/>'
             f'<rect x="56" y="52" width="400" height="400" rx="48" fill="url(#body-{key})"/>')
    if not flat:
        body += f'<rect x="56" y="52" width="400" height="400" rx="48" fill="url(#bevel-{key})"/>'
    return f"<defs>{defs}</defs>{body}"


def glyph(key, element, name, flat, place=SILHOUETTE, stroke=44, shadow=(12, 28)):
    """The silhouette (§1.3): flat grey when locked, engraved on a light tile, the menus' layers on a dark one."""
    from ui_art import shape as path_of
    d = path_of(name)
    main = MAIN[element]
    if flat:
        return f'<g transform="{place}"><path d="{d}" fill="#7a7a7a"/></g>'
    if light(element):
        return (f'<defs><linearGradient id="gf-{key}" gradientUnits="userSpaceOnUse" x1="96" y1="32" x2="416" y2="480">'
                f'<stop offset="0" stop-color="{mix(main, "#000000", 0.70)}"/><stop offset="1" stop-color="{mix(main, "#000000", 0.88)}"/></linearGradient></defs>'
                f'<g transform="{place}"><g opacity="0.5" transform="translate(10 18)"><path d="{d}" fill="#fff"/></g>'
                f'<path d="{d}" fill="url(#gf-{key})"/></g>')
    tone = {"shape": name, "light": "#ffffff", "main": mix(main, "#ffffff", 0.70), "dark": mix(main, "#ffffff", 0.45), "outline": mix(main, "#000000", 0.85)}
    return layers(key, tone, place, stroke, shadow)


def dot(key, element, flat):
    """A twin's origin (§1.5): a dot of its spell's element, bottom right."""
    main = MAIN[element]
    if flat:
        return f'<circle cx="424" cy="424" r="48" fill="{mix(main, "#141414", 0.6)}" stroke="#0d0d0d" stroke-width="12"/>'
    return (f'<defs><linearGradient id="dot-{key}" x1="0" y1="0" x2="0" y2="1"><stop offset="0" stop-color="{mix(main, "#ffffff", 0.45)}"/>'
            f'<stop offset="1" stop-color="{mix(main, "#000000", 0.45)}"/></linearGradient></defs>'
            f'<circle cx="424" cy="424" r="48" fill="url(#dot-{key})" stroke="#0d0d0d" stroke-width="12"/>'
            '<ellipse cx="424" cy="404" rx="30" ry="18" fill="#fff" opacity="0.35"/>')


def icon(spell, origin, flat, marks):
    element, key = spell["element"], "s"
    body = tile(key, element, flat) + glyph(key, element, spell["icon"], flat)
    if marks and spell["id"] in SEALS:
        el, accent = SEALS[spell["id"]]
        body += glyph("a", el, accent, flat, ACCENT, 64, (16, 36))
    if marks and origin is not None:
        body += dot(key, origin, flat)
    return f'<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 512 512">{body}</svg>\n'


def main():
    fetch = "--fetch" in sys.argv
    spells = {s["id"]: s for s in json.load(open(os.path.join(root, "data", "spells.json")))}
    classes = json.load(open(os.path.join(root, "data", "classes.json")))
    tree = ["relay", "seal-earth", "seal-fire", "seal-water", "seal-air", "decoy"]
    drawn = [i for c in classes for i in c["spells"]] + [i for i, s in spells.items() if "twin" in s] + tree
    credits = {}
    for i in drawn:
        s = spells[i]
        shape(s["icon"], fetch)
        if i in SEALS:
            shape(SEALS[i][1], fetch)
        origin = spells[s["twin"]]["element"] if "twin" in s else None
        for suffix, flat, marks in (("", False, True), ("-locked", True, True), ("-small", False, False)):
            open(os.path.join(out, f"{i}{suffix}.svg"), "w").write(icon(s, origin, flat, marks))
        credits[i] = s["icon"]
    json.dump(dict(sorted(credits.items())), open(os.path.join(out, "credits.json"), "w"), indent=1, ensure_ascii=False)
    open(os.path.join(out, "credits.json"), "a").write("\n")
    print(f"{len(drawn)} spells, {3 * len(drawn)} files")


if __name__ == "__main__":
    main()
