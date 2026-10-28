# The interface's art in relief (sprint 157, D84, docs/graphisme/s157-maquettes.md): the menu's icons and the theme's frames,
# written as SVG without any filter (Godot draws them with ThorVG: gradients, clip paths and group opacity only).
# - Icons: godot/assets/icons/bar/<name>.svg from palette.json, a game-icons.net silhouette (CC BY 3.0, its path kept in
#   bar/shapes/<author>_<name>.path) in layers: a drop shadow, a dark outline under the fill, a diagonal gradient, a
#   highlight clipped to the shape, and an optional accent in the top right corner with the same layers.
# - Frames: godot/assets/ui/frames/<name>.svg, 9-patches of a raised tile (shadow, vertical gradient, highlight, edge,
#   an optional halo), stretched by the theme (OsmoseTheme.cs) with a texture margin of 12.
# Deterministic; run from projects/rpg:  python3 tools/ui_art.py
import json, os

here = os.path.dirname(os.path.abspath(__file__))
bar = os.path.join(here, "..", "godot", "assets", "icons", "bar")
frames = os.path.join(here, "..", "godot", "assets", "ui", "frames")

PLACE = "translate(40 28) scale(0.84375)"  # 432 units wide: room for the outline and the shadow
ACCENT = "translate(272 8) scale(0.44)"     # the top right corner (the badge takes the top left, the key the bottom right)


def shape(name):
    return open(os.path.join(bar, "shapes", name.replace("/", "_") + ".path")).read().strip()


def layers(key, tone, place, stroke, shadow):
    """The four layers of a silhouette (shadow, outline, gradient, highlight), in the group that places it."""
    d = shape(tone["shape"])
    sx, sy = shadow
    return (
        f'<defs><linearGradient id="g-{key}" gradientUnits="userSpaceOnUse" x1="96" y1="32" x2="416" y2="480">'
        f'<stop offset="0" stop-color="{tone["light"]}"/><stop offset="0.5" stop-color="{tone["main"]}"/><stop offset="1" stop-color="{tone["dark"]}"/></linearGradient>'
        f'<linearGradient id="r-{key}" gradientUnits="userSpaceOnUse" x1="0" y1="0" x2="0" y2="512">'
        f'<stop offset="0" stop-color="#fff" stop-opacity="0.45"/><stop offset="0.22" stop-color="#fff" stop-opacity="0.2"/><stop offset="0.48" stop-color="#fff" stop-opacity="0"/></linearGradient>'
        f'<clipPath id="c-{key}"><path d="{d}"/></clipPath></defs>'
        f'<g transform="{place}">'
        f'<g opacity="0.45" transform="translate({sx} {sy})"><path d="{d}" fill="#000" stroke="#000" stroke-width="{stroke}" stroke-linejoin="round"/></g>'
        f'<path d="{d}" fill="{tone["outline"]}" stroke="{tone["outline"]}" stroke-width="{stroke}" stroke-linejoin="round" stroke-linecap="round"/>'
        f'<path d="{d}" fill="url(#g-{key})"/>'
        f'<rect x="0" y="0" width="512" height="512" clip-path="url(#c-{key})" fill="url(#r-{key})"/>'
        f'</g>'
    )


def icon(tone):
    body = layers("main", tone, PLACE, 48, (12, 28))
    if "accent" in tone:
        body += layers("acc", tone["accent"], ACCENT, 64, (16, 36))
    return f'<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 512 512">{body}</svg>\n'


# The raised frames: (top, bottom, edge, edge width, radius, highlight?, shadow?, halo).
FRAMES = {
    "tile": ("#3a3a3d", "#262628", "#0d0d0d", 1, 8, True, True, None),
    "tile-hover": ("#46464a", "#2e2e31", "#c9a27f", 1, 8, True, True, None),
    "tile-lit": ("#3d4f24", "#25301a", "#bef374", 2, 8, True, True, "#bef374"),
    "tile-pressed": ("#262628", "#3a3a3d", "#0d0d0d", 1, 8, False, False, None),
    "primary": ("#d4f79e", "#a3d95a", "#4d7a1c", 1, 8, True, True, None),
    "primary-hover": ("#e6f8c9", "#bef374", "#4d7a1c", 1, 8, True, True, None),
    "primary-pressed": ("#a3d95a", "#d4f79e", "#4d7a1c", 1, 8, False, False, None),
    "primary-disabled": ("#3a3a3a", "#3a3a3a", "#0d0d0d", 1, 8, False, False, None),
    "secondary": ("#3a3a3d", "#262628", "#0d0d0d", 1, 8, True, True, None),
    "secondary-hover": ("#46464a", "#2e2e31", "#c9a27f", 1, 8, True, True, None),
    "secondary-pressed": ("#262628", "#3a3a3d", "#0d0d0d", 1, 8, False, False, None),
    "fill-progress": ("#d4f79e", "#a3d95a", "#000000", 1, 6, True, False, None),
    "fill-life": ("#f69c9c", "#c23a3a", "#000000", 1, 6, True, False, None),
    "fill-gold": ("#ffe6ab", "#c9962e", "#000000", 1, 6, True, False, None),
    "gold-band": ("#ffe6ab", "#c9962e", "#5a3a22", 2, 4, True, True, None),
}


def frame(top, bottom, edge, width, r, highlight, shadow, halo):
    """A 56 × 56 nine-patch: the body is the 48 × 48 rectangle at (4, 3), its shadow 2 lower."""
    parts = [
        '<defs><linearGradient id="b" x1="0" y1="0" x2="0" y2="1">'
        f'<stop offset="0" stop-color="{top}"/><stop offset="1" stop-color="{bottom}"/></linearGradient>'
        '<linearGradient id="h" x1="0" y1="0" x2="0" y2="1"><stop offset="0" stop-color="#fff" stop-opacity="0.18"/>'
        '<stop offset="1" stop-color="#fff" stop-opacity="0"/></linearGradient></defs>'
    ]
    if shadow:
        parts.append(f'<rect x="4" y="5" width="48" height="48" rx="{r}" fill="#000" opacity="0.45"/>')
    if halo:
        parts.append(f'<rect x="2" y="1" width="52" height="52" rx="{r + 2}" fill="none" stroke="{halo}" stroke-width="3" opacity="0.35"/>')
    parts.append(f'<rect x="4" y="3" width="48" height="48" rx="{r}" fill="url(#b)"/>')
    if highlight:
        parts.append(f'<rect x="5" y="4" width="46" height="20" rx="{r - 1}" fill="url(#h)"/>')
    w = width / 2
    parts.append(f'<rect x="{4 + w:g}" y="{3 + w:g}" width="{48 - width:g}" height="{48 - width:g}" rx="{r - w:g}" fill="none" stroke="{edge}" stroke-width="{width}"/>')
    return '<svg xmlns="http://www.w3.org/2000/svg" width="56" height="56" viewBox="0 0 56 56">' + "".join(parts) + "</svg>\n"


def main():
    palette = json.load(open(os.path.join(bar, "palette.json")))
    for name, tone in palette.items():
        with open(os.path.join(bar, name + ".svg"), "w") as f:
            f.write(icon(tone))
    os.makedirs(frames, exist_ok=True)
    for name, spec in FRAMES.items():
        with open(os.path.join(frames, name + ".svg"), "w") as f:
            f.write(frame(*spec))
    # The Menus panel's title pictogram: four squares of 6, 2 apart, in light chocolate.
    with open(os.path.join(frames, "..", "grid.svg"), "w") as f:
        f.write('<svg xmlns="http://www.w3.org/2000/svg" width="14" height="14" viewBox="0 0 14 14">'
                + "".join(f'<rect x="{x}" y="{y}" width="6" height="6" rx="1" fill="#c9a27f"/>' for y in (0, 8) for x in (0, 8)) + "</svg>\n")
    print(f"{len(palette)} icons, {len(FRAMES)} frames")


if __name__ == "__main__":
    main()
