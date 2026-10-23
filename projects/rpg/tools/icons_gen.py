# The items' icons (sprint 98, D72, docs/suite-apres-d69.md §3 bis): a game-icons.net drawing (CC BY 3.0) for
# every item, written as its "icon" in data/items.json; the drawings in godot/assets/icons/items/<name>.svg,
# their black square taken off and the shape in white (the game tints it with the item's colour), their
# authors in credits.json. Items of a kind may share a drawing. Deterministic; --fetch downloads the drawings
# missing (from the game-icons repository on GitHub), otherwise every one must be there.
# Run from projects/rpg last, after tools/dungeon_gen.py, tools/sets_gen.py, tools/trades_gen.py and tools/craft_gen.py (which write items again):
#   python3 tools/icons_gen.py data [--fetch]
import json, os, re, sys, urllib.request

root = sys.argv[1]
fetch = "--fetch" in sys.argv
out = os.path.join(os.path.dirname(os.path.abspath(root)), "godot", "assets", "icons", "items")

ICONS = {
    # Equipment: rings, amulets, belts, capes, hats, chests, shoulders, weapons, shields, pets, mounts, boots.
    "copper-ring": "delapouite/ring", "ember-ring": "delapouite/power-ring", "lily-ring": "delapouite/diamond-ring",
    "acorn-ring": "delapouite/ring", "howl-ring": "delapouite/globe-ring", "puffball-ring": "delapouite/diamond-ring",
    "bile-ring": "lorc/skull-ring", "bubble-ring": "delapouite/frozen-ring", "wax-ring": "delapouite/power-ring", "clod-ring": "delapouite/ring",
    "pebble-amulet": "delapouite/tribal-pendant", "tusk-amulet": "delapouite/primitive-necklace", "pack-amulet": "lorc/gem-pendant",
    "morel-amulet": "delapouite/primitive-necklace", "croak-amulet": "delapouite/tribal-pendant", "jelly-amulet": "lorc/gem-pendant",
    "leather-belt": "lucasms/belt", "grave-belt": "delapouite/belt-armor", "bristle-belt": "lucasms/belt",
    "mycelium-belt": "delapouite/belt-armor", "claw-belt": "delapouite/belt-armor", "burrow-belt": "lucasms/belt",
    "poacher-cape": "delapouite/cape", "wolf-cape": "lucasms/cloak", "bone-cape": "delapouite/vampire-cape",
    "hermit-cape": "delapouite/cape", "wing-cape": "lorc/wing-cloak",
    "poacher-hat": "delapouite/robin-hood-hat", "cap-hat": "lorc/mushroom-gills", "stitched-hat": "delapouite/pilgrim-hat",
    "dew-hat": "delapouite/flower-hat", "mole-hat": "delapouite/outback-hat",
    "orc-breastplate": "lorc/breastplate", "orc-pauldrons": "skoll/pauldrons",
    "dagger": "lorc/plain-dagger", "stinger-dagger": "lorc/stiletto", "leader-fang": "lorc/bone-knife",
    "overlord-cleaver": "lorc/meat-cleaver", "toad-sceptre": "delapouite/winged-scepter",
    "orc-club": "delapouite/wood-club", "king-tusk": "delapouite/ivory-tusks", "elder-staff": "lorc/wizard-staff",
    "figurehead-axe": "lorc/battle-axe", "debris-pick": "delapouite/war-pick",
    "wooden-shield": "delapouite/viking-shield", "shell-shield": "delapouite/spiked-shield",
    "kitten": "lorc/cat", "donkey": "skoll/donkey",
    # The sylvan stags' set (sprint 132).
    "antler-circlet": "lorc/crown-of-thorns", "hind-cape": "delapouite/cape", "stonehoof-boots": "delapouite/metal-boot", "elder-antler-staff": "lorc/wizard-staff",
    # The mossy keepers' set (sprint 133).
    "moss-ring": "delapouite/ring", "cobble-belt": "delapouite/belt-armor", "slab-shield": "delapouite/viking-shield", "menhir-mace": "delapouite/wood-club",
    # The craftsmen's pieces (sprint 109): the smith's and the tailor's, a drawing already there for each slot.
    "copper-billhook": "lorc/plain-dagger", "hazel-roundshield": "delapouite/viking-shield", "blackwood-maul": "delapouite/wood-club",
    "yew-targe": "delapouite/viking-shield", "clay-quenched-cutlass": "lorc/meat-cleaver", "alder-greataxe": "lorc/battle-axe",
    "ash-shield": "delapouite/viking-shield", "reaper-falchion": "lorc/meat-cleaver",
    "linen-tunic": "lorc/breastplate", "oat-straw-hat": "delapouite/outback-hat", "hemp-shoulders": "skoll/pauldrons",
    "rye-pelerine": "delapouite/cape", "sedge-gambeson": "lorc/breastplate", "rice-sunhat": "delapouite/flower-hat",
    "wheat-spaulders": "skoll/pauldrons", "harvest-mantle": "lucasms/cloak",
    # The jeweller's and the cobbler's (sprint 110).
    "copper-band": "delapouite/ring", "tin-pendant": "delapouite/tribal-pendant", "blackened-iron-ring": "delapouite/power-ring",
    "sulphur-medallion": "lorc/gem-pendant", "blue-faience-ring": "delapouite/frozen-ring", "zinc-torc": "delapouite/primitive-necklace",
    "silver-signet": "delapouite/globe-ring", "quartz-pendant": "lorc/gem-pendant",
    "gudgeon-shoes": "lorc/leather-boot", "crayfish-belt": "lucasms/belt", "eel-skin-boots": "delapouite/rubber-boot",
    "tench-swordbelt": "delapouite/belt-armor", "perch-waders": "delapouite/rubber-boot", "carp-belt": "lucasms/belt",
    "pike-skin-boots": "delapouite/fur-boot", "zander-baldric": "delapouite/belt-armor",
    "poacher-boots": "lorc/leather-boot", "digger-boots": "delapouite/fur-boot", "boar-boots": "delapouite/metal-boot",
    "wolf-boots": "delapouite/fur-boot", "frog-boots": "delapouite/rubber-boot", "bee-boots": "delapouite/sticky-boot",
    # Consumables; since sprint 111 the alchemist's potions and the baker's breads.
    "bread": "delapouite/bread", "healing-potion": "delapouite/health-potion",
    "strong-healing-potion": "delapouite/health-potion", "greater-healing-potion": "delapouite/health-potion", "healing-elixir": "delapouite/health-potion",
    **{b: "delapouite/bread" for b in ("oat-bread", "hemp-bread", "rye-bread", "sedge-flatbread", "reed-rice-bread", "clairval-loaf", "buckwheat-galette")},
    # Resources the monsters leave.
    "leather": "delapouite/animal-hide", "orc-fang": "lorc/fangs-circle", "feather": "lorc/feather",
    "boar-tusk": "lorc/boar-tusks", "boar-bristle": "delapouite/hair-strands", "wolf-fang": "skoll/fangs",
    "wolf-pelt": "delapouite/animal-hide", "spore-dust": "lorc/fluffy-cloud", "mushroom-cap": "delapouite/mushrooms",
    "rotten-cloth": "delapouite/rolled-cloth", "old-bone": "lorc/broken-bone", "frog-leg": "delapouite/frog-foot",
    "slime": "delapouite/slime", "crab-shell": "lorc/spiked-shell", "crab-claw": "lorc/crab-claw",
    "honeycomb": "lorc/honeycomb", "bee-wing": "delapouite/fairy-wings", "mole-fur": "delapouite/animal-hide",
    "earthworm": "cathelineau/earth-worm",
    # The sylvan stags' (sprint 132).
    "stone-antler": "lorc/stag-head", "stag-velvet": "delapouite/animal-hide",
    # The mossy keepers' (sprint 133).
    "mossy-cobble": "lorc/stone-block", "keeper-moss": "lorc/ground-sprout",
    # The gathered resources (sprint 99): wood, ore, fish, crops, herbs.
    "oak-log": "delapouite/log", "hazel-log": "delapouite/half-log", "blackwood-log": "delapouite/wood-pile", "yew-log": "delapouite/log",
    "willow-log": "delapouite/half-log", "alder-log": "delapouite/wood-pile", "ash-log": "delapouite/axe-in-log", "linden-log": "delapouite/log",
    "copper-ore": "faithtoken/ore", "tin-ore": "faithtoken/minerals", "iron-ore": "faithtoken/ore", "sulphur": "lorc/crystal-growth",
    "blue-clay": "delapouite/clay-brick", "zinc-ore": "faithtoken/minerals", "silver-ore": "delapouite/gold-nuggets", "quartz": "lorc/crystal-cluster",
    "gudgeon": "delapouite/tropical-fish", "crayfish": "delapouite/shrimp", "pool-eel": "delapouite/circling-fish", "tench": "delapouite/flatfish",
    "perch": "delapouite/tropical-fish", "carp": "delapouite/double-fish", "pike": "delapouite/piranha", "zander": "various-artists/salmon",
    "wild-flax": "delapouite/flax", "wild-oats": "delapouite/grain", "pool-hemp": "delapouite/hemp", "rye": "delapouite/grain-bundle",
    "shore-sedge": "lorc/sprout", "reed-rice": "delapouite/grain", "clairval-wheat": "lorc/wheat", "buckwheat": "delapouite/grain-bundle",
    "nettle": "delapouite/herbs-bundle", "wood-mint": "delapouite/oak-leaf", "glowmoss": "lorc/ground-sprout", "clubmoss": "delapouite/fern",
    "water-lily": "lorc/lotus-flower", "horsetail": "lorc/sprout", "hive-flower": "delapouite/sunflower", "lavender": "delapouite/dandelion-flower",
    # The Forest of the Ancestors' (sprint 131): levels 40 and 45.
    "hornbeam-log": "delapouite/wood-beam", "elm-log": "delapouite/tree-roots", "granite": "lorc/stone-block", "malachite": "lorc/emerald",
    "trout": "delapouite/fish-escape", "catfish": "delapouite/fish-bucket", "spelt": "delapouite/sesame", "sorghum": "delapouite/corn",
    "sage": "lorc/three-leaves", "mandrake": "delapouite/plant-roots",
    # The dungeons' keys, and the quest items.
    "boar-wallow-key": "delapouite/boss-key", "wolf-den-key": "delapouite/boss-key", "mushroom-cellar-key": "delapouite/boss-key",
    "zombie-crypt-key": "delapouite/boss-key", "frog-grotto-key": "delapouite/boss-key", "crab-reef-key": "delapouite/boss-key",
    "royal-hive-key": "delapouite/boss-key", "mole-burrows-key": "delapouite/boss-key",
    "bellowing-combe-key": "delapouite/boss-key", "mother-tree-roots-key": "delapouite/boss-key",
    "garance-badge": "andymeneely/police-badge", "rat-tail": "cathelineau/earth-worm", "marked-fang": "skoll/fangs", "registry-page": "lorc/scroll-unfurled", "last-verse": "lorc/spiked-shell", "painted-shard": "delapouite/clay-brick", "humming-debris": "faithtoken/ore", "palisade-stake": "delapouite/log", "admin-keyring": "delapouite/keyring",
    # The recall scroll (sprint 127, left out of this list then: added sprint 131).
    "recall-scroll": "lorc/scroll-unfurled",
}


def inline(v):
    """One item per line, as tools/dungeon_gen.py writes the file."""
    if isinstance(v, dict):
        return "{ " + ", ".join(f"{json.dumps(k)}: {inline(x)}" for k, x in v.items()) + " }"
    if isinstance(v, list):
        return "[" + ", ".join(inline(x) for x in v) + "]"
    return json.dumps(v, ensure_ascii=False)


def white(svg):
    """The drawing without its black square, its shape in white, as the spells' are."""
    svg = re.sub(r'<path d="M0 0h512v512H0z"\s*/>', "", svg)
    return re.sub(r'fill="#[0-9a-fA-F]{3,6}"', 'fill="#ffffff"', svg)


items = json.load(open(f"{root}/items.json"))
missing = [i["id"] for i in items if i["id"] not in ICONS]
assert not missing, f"no icon for {missing}"
os.makedirs(out, exist_ok=True)
credits = {}
for icon in sorted(set(ICONS.values())):
    name = icon.split("/")[1]
    assert credits.get(name, icon) == icon, f"two drawings named {name}"
    credits[name] = icon
    path = os.path.join(out, f"{name}.svg")
    if not os.path.exists(path):
        assert fetch, f"{path} missing: run with --fetch"
        url = f"https://raw.githubusercontent.com/game-icons/icons/master/{icon}.svg"
        open(path, "w").write(white(urllib.request.urlopen(url, timeout=30).read().decode()))
for i in items:
    i["icon"] = ICONS[i["id"]]
with open(f"{root}/items.json", "w") as f:
    f.write("[\n" + ",\n".join("  " + inline(i) for i in items) + "\n]\n")
open(os.path.join(out, "credits.json"), "w").write(json.dumps(credits, ensure_ascii=False, indent=1, sort_keys=True) + "\n")
print(f"{len(items)} items, {len(credits)} drawings")
