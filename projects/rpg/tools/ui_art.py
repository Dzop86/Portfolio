# The interface's art in relief (sprint 157, D84, docs/graphisme/s157-maquettes.md): the menu's icons and the theme's frames,
# written as SVG without any filter (Godot draws them with ThorVG: gradients, clip paths and group opacity only).
# - Icons: godot/assets/icons/bar/<name>.svg from palette.json, a game-icons.net silhouette (CC BY 3.0, its path kept in
#   bar/shapes/<author>_<name>.path) in layers: a drop shadow, a dark outline under the fill, a diagonal gradient, a
#   highlight clipped to the shape, and an optional accent in the top right corner with the same layers.
# - Frames: godot/assets/ui/frames/<name>.svg, 9-patches of a raised tile (shadow, vertical gradient, highlight, edge,
#   an optional halo), stretched by the theme (OsmoseTheme.cs) with a texture margin of 12.
# - Since sprint 158 (docs/graphisme/s158-succes.md): godot/assets/icons/relief/ holds the icons in relief that are not on the
#   menu (relief/palette.json: the dungeons'), a grey "-locked" variant of each icon an achievement is drawn with, the
#   search field's magnifier; godot/assets/ui/achievements/banner-<Category>.svg the categories' banners (704 x 192, a
#   diagonal gradient from the category's colour, its silhouette as a watermark, a veil, its icon); two icon frames.
# - Since sprint 164: relief/palette.json also holds the twelve trades' icons ("job-<id>", a colour each from its matter);
#   since sprint 165 the quests' five kinds ("quest-<kind>") and the tick of an objective reached ("check"); since sprint 171
#   the ten families' emblems ("family-<id>", greyed while none of the family is met).
# Deterministic; run from projects/rpg:  python3 tools/ui_art.py
import json, os

here = os.path.dirname(os.path.abspath(__file__))
bar = os.path.join(here, "..", "godot", "assets", "icons", "bar")
frames = os.path.join(here, "..", "godot", "assets", "ui", "frames")
relief = os.path.join(here, "..", "godot", "assets", "icons", "relief")
banners = os.path.join(here, "..", "godot", "assets", "ui", "achievements")

# The grey of an achievement not yet earned (--osmose-locked and its tones).
LOCKED = {"main": "#8a8a8a", "light": "#bfbfbf", "dark": "#4c4c4c", "outline": "#1c1c1c"}

# Each category's icon and the colour its banner is drawn from (s158-succes.md §2.2).
CATEGORIES = {
    "Exploration": ("map", "#52c45a"), "Hunt": ("bestiary", "#ef4b4b"), "Dungeons": ("dungeons", "#b48cff"), "Jobs": ("jobs", "#a9bccd"),
    "Quests": ("journal", "#b89a6a"), "Tree": ("talents", "#bef374"), "Together": ("contacts", "#ff7eb6"),
}

PLACE = "translate(40 28) scale(0.84375)"  # 432 units wide: room for the outline and the shadow
ACCENT = "translate(272 8) scale(0.44)"     # the top right corner (the badge takes the top left, the key the bottom right)


def shape(name):
    return open(os.path.join(bar, "shapes", name.replace("/", "_") + ".path")).read().strip()


def layers(key, tone, place, stroke, shadow):
    """The four layers of a silhouette (shadow, outline, gradient, highlight), in the group that places it; its path is
    tone["d"] when given (the items' drawings, tools/item_art.py), else that of tone["shape"]."""
    d = tone.get("d") or shape(tone["shape"])
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


def icon_body(tone, place=PLACE, scale=1.0, suffix=""):
    body = layers("main" + suffix, tone, place, 48, (12, 28))
    if "accent" in tone:
        body += layers("acc" + suffix, tone["accent"], place + " " + ACCENT if scale != 1.0 else ACCENT, 64, (16, 36))
    return body


def icon(tone):
    return f'<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 512 512">{icon_body(tone)}</svg>\n'


def grey(tone):
    """The same icon in the greys of what is locked, its accent too."""
    out = dict(tone, **LOCKED)
    if "accent" in tone:
        out["accent"] = dict(tone["accent"], **LOCKED)
    return out


def mix(a, b, t):
    """a moved a share t toward b, as #rrggbb."""
    pa, pb = [int(a[i:i + 2], 16) for i in (1, 3, 5)], [int(b[i:i + 2], 16) for i in (1, 3, 5)]
    return "#" + "".join(f"{round(x + (y - x) * t):02x}" for x, y in zip(pa, pb))


def banner(main, tone):
    """A category's banner, 704 x 192: its colour darkened along the diagonal, its silhouette in white at 10 %, a veil
    on the lower 40 %, a dark line under it, and its icon in relief at the bottom left; the top corners rounded (14)."""
    top, bottom = mix(main, "#1f1f1f", 0.55), mix(main, "#1f1f1f", 0.85)
    d = shape(tone["shape"])
    return (
        '<svg xmlns="http://www.w3.org/2000/svg" width="704" height="192" viewBox="0 0 704 192">'
        '<defs><clipPath id="round"><path d="M14 0H690A14 14 0 0 1 704 14V192H0V14A14 14 0 0 1 14 0Z"/></clipPath>'
        f'<linearGradient id="ground" x1="0" y1="0" x2="1" y2="1"><stop offset="0" stop-color="{top}"/><stop offset="1" stop-color="{bottom}"/></linearGradient>'
        '<linearGradient id="veil" x1="0" y1="0" x2="0" y2="1"><stop offset="0" stop-color="#000" stop-opacity="0"/><stop offset="1" stop-color="#000" stop-opacity="0.55"/></linearGradient></defs>'
        '<g clip-path="url(#round)">'
        '<rect width="704" height="192" fill="url(#ground)"/>'
        f'<g transform="translate(520 -24) scale(0.46875)" opacity="0.1"><path d="{d}" fill="#fff"/></g>'
        '<rect y="115" width="704" height="77" fill="url(#veil)"/>'
        '<rect y="190" width="704" height="2" fill="#0d0d0d"/>'
        '</g>'
        f'<g transform="translate(20 76) scale(0.1875)">{icon_body(tone, suffix="-b")}</g>'
        '</svg>\n'
    )


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
    # The achievements' icon frames (sprint 158): raised and edged in gold when earned, a flat hollow when not.
    "frame-gold": ("#3a3a3d", "#262628", "#ffd166", 2, 8, True, False, None),
    "frame-well": ("#141414", "#141414", "#0d0d0d", 1, 8, False, False, None),
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
    extra = json.load(open(os.path.join(relief, "palette.json")))
    for name, tone in extra.items():
        with open(os.path.join(relief, name + ".svg"), "w") as f:
            f.write(icon(tone))
    every = {**palette, **extra}
    # The trades' icons (sprint 164, docs/graphisme/s163-icones-arbre.md §2): "job-<id>", greyed for a trade not practised;
    # the quests' kinds (sprint 165, docs/graphisme/s165-retours-30-oct.md §3.2): "quest-<kind>", greyed for a kind filtered out.
    jobs = {n for n in extra if n.startswith(("job-", "quest-", "family-"))}
    for name in sorted({c[0] for c in CATEGORIES.values()} | {"achievements"} | jobs):
        with open(os.path.join(relief, name + "-locked.svg"), "w") as f:
            f.write(icon(grey(every[name])))
    # The search field's magnifier: the silhouette alone, in the dim text's grey.
    with open(os.path.join(relief, "search.svg"), "w") as f:
        f.write(f'<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 512 512"><path d="{shape("lorc/magnifying-glass")}" fill="#9d9d9d"/></svg>\n')
    os.makedirs(banners, exist_ok=True)
    for category, (name, main) in CATEGORIES.items():
        with open(os.path.join(banners, f"banner-{category}.svg"), "w") as f:
            f.write(banner(main, every[name]))
    os.makedirs(frames, exist_ok=True)
    for name, spec in FRAMES.items():
        with open(os.path.join(frames, name + ".svg"), "w") as f:
            f.write(frame(*spec))
    # The Menus panel's title pictogram: four squares of 6, 2 apart, in light chocolate.
    with open(os.path.join(frames, "..", "grid.svg"), "w") as f:
        f.write('<svg xmlns="http://www.w3.org/2000/svg" width="14" height="14" viewBox="0 0 14 14">'
                + "".join(f'<rect x="{x}" y="{y}" width="6" height="6" rx="1" fill="#c9a27f"/>' for y in (0, 8) for x in (0, 8)) + "</svg>\n")
    # The guild's level in a gold hexagon (sprint 160, docs/graphisme/s160-guilde.md): its relief, its edge in chocolate.
    with open(os.path.join(frames, "..", "level-hex.svg"), "w") as f:
        f.write('<svg xmlns="http://www.w3.org/2000/svg" width="40" height="44" viewBox="0 0 40 44"><defs><linearGradient id="g" x1="0" y1="0" x2="0" y2="1">'
                '<stop offset="0" stop-color="#ffe6ab"/><stop offset="0.5" stop-color="#ffd166"/><stop offset="1" stop-color="#c9962e"/></linearGradient></defs>'
                '<path d="M20 3L36 12V32L20 41L4 32V12Z" fill="#000" opacity="0.45" transform="translate(0 2)"/>'
                '<path d="M20 2L36 11V31L20 40L4 31V11Z" fill="url(#g)" stroke="#5a3a22" stroke-width="2" stroke-linejoin="round"/></svg>\n')
    # The check box (sprint 158): a hollow edged in the dim grey, or pistachio ticked in chocolate.
    for name, body in {
        "check-off": '<rect x="1" y="1" width="18" height="18" rx="4" fill="#141414" stroke="#9d9d9d" stroke-width="2"/>',
        "check-on": '<rect x="0" y="0" width="20" height="20" rx="4" fill="#bef374"/><path d="M5 10.5l3.2 3.2L15 6.5" fill="none" stroke="#5a3a22" stroke-width="3" stroke-linecap="round" stroke-linejoin="round"/>',
    }.items():
        with open(os.path.join(frames, "..", name + ".svg"), "w") as f:
            f.write(f'<svg xmlns="http://www.w3.org/2000/svg" width="20" height="20" viewBox="0 0 20 20">{body}</svg>\n')
    # An objective still to reach (sprint 165, s165-retours-30-oct.md §1.3): a hollow ring in the dim grey; the chevrons
    # that fold the panels laid on the map, two strokes in light chocolate.
    for name, body in {
        "todo": '<circle cx="8" cy="8" r="6" fill="none" stroke="#9d9d9d" stroke-width="2"/>',
        "chevron-down": '<path d="M3 6l5 5 5-5" fill="none" stroke="#c9a27f" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"/>',
        "chevron-up": '<path d="M3 10l5-5 5 5" fill="none" stroke="#c9a27f" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"/>',
    }.items():
        with open(os.path.join(frames, "..", name + ".svg"), "w") as f:
            f.write(f'<svg xmlns="http://www.w3.org/2000/svg" width="16" height="16" viewBox="0 0 16 16">{body}</svg>\n')
    print(f"{len(palette)} icons, {len(FRAMES)} frames")


if __name__ == "__main__":
    main()
