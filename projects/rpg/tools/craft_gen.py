# The trades that make things (sprints 109 to 111, D76): first the equipment trades (sprints 109 and 110, D76, docs/suite-apres-d69.md §5 quater): the smith (one- and two-handed
# weapons, shields), the tailor (chest, shoulders, hat, cape), the jeweller (rings, amulet) and the cobbler (boots,
# belt), a recipe every 5 levels from 1 to 35; the names as the scenarist reread them (docs/textes-v6.md §4). A piece's
# characteristics come from the rule of §5 quater.4, not by hand: its level follows the recipe's, its budget is
# that of the loot, (8 + level) / 2 rounded up, the element taking the upper half and Vitality the lower (a shield
# the other way round), a two-handed weapon counting as two pieces; never initiative, action or movement points,
# nor a set. Its ingredients: the main gatherer's resource and the second one's of the recipe's level, and a
# monster's resource, in the quantities of the table; its Éclats, the item's level (twice for two hands).
# The palisade stake of the tutorial moves from the locksmith to the smith. The pieces go in data/items.json
# (before the dungeons' keys), the trades in data/jobs.json, keeping the others. Deterministic and idempotent.
# Run from projects/rpg after tools/trades_gen.py, before tools/icons_gen.py:  python3 tools/craft_gen.py data
import collections, json, math, sys

root = sys.argv[1]
O = collections.OrderedDict


def text(fr, en): return O([("fr", fr), ("en", en)])


# Since sprint 135 (D79), the levels 40 and 45 of the Forest of the Ancestors, with its resources and its two families'.
LEVELS = [1, 5, 10, 15, 20, 25, 30, 35, 40, 45]
ITEM_LEVEL = {1: 3, 5: 6, 10: 10, 15: 14, 20: 18, 25: 22, 30: 26, 35: 30, 40: 34, 45: 38}
# Quantities by recipe level: main gatherer, second gatherer, monster's resource.
COUNTS = {1: (6, 2, 2), 5: (6, 3, 2), 10: (8, 3, 3), 15: (8, 4, 3), 20: (10, 4, 3), 25: (10, 5, 3), 30: (12, 5, 4), 35: (12, 6, 4), 40: (14, 6, 4), 45: (14, 7, 5)}
ELEMENT = {"Te": "earth", "Fe": "fire", "Ea": "water", "Ai": "air"}
# trade: (name, workshop, [(recipe level, id, fr, en, slot, element, [main, second, monster], icon)])
TRADES = O([
    ("smith", (text("Forgeron", "Smith"), text("Forge", "Forge"), [
        (1, "copper-billhook", "Serpe de cuivre", "Copper Billhook", "OneHanded", "Ai", ["oak-log", "copper-ore", "wolf-fang"]),
        (5, "hazel-roundshield", "Rondache de noisetier", "Hazel Roundshield", "Shield", "Te", ["hazel-log", "tin-ore", "boar-tusk"]),
        (10, "blackwood-maul", "Merlin de bois noir", "Blackwood Maul", "TwoHanded", "Fe", ["blackwood-log", "iron-ore", "old-bone"]),
        (15, "yew-targe", "Targe d'if", "Yew Targe", "Shield", "Ea", ["yew-log", "sulphur", "mushroom-cap"]),
        (20, "clay-quenched-cutlass", "Coutelas trempé à l'argile", "Clay-Quenched Cutlass", "OneHanded", "Ea", ["willow-log", "blue-clay", "crab-claw"]),
        (25, "alder-greataxe", "Grande hache d'aulne", "Alder Greataxe", "TwoHanded", "Te", ["alder-log", "zinc-ore", "crab-shell"]),
        (30, "ash-shield", "Écu de frêne", "Ash Heater Shield", "Shield", "Ai", ["ash-log", "silver-ore", "mole-fur"]),
        (35, "reaper-falchion", "Fauchon de moissonneur", "Reaper's Falchion", "OneHanded", "Fe", ["linden-log", "quartz", "honeycomb"]),
        (40, "granite-warhammer", "Marteau de granit", "Granite Warhammer", "TwoHanded", "Fe", ["hornbeam-log", "granite", "stone-antler"]),
        (45, "elm-pavise", "Pavois d'orme", "Elm Pavise", "Shield", "Ai", ["elm-log", "malachite", "mossy-cobble"])])),
    ("tailor", (text("Tailleur", "Tailor"), text("Atelier de couture", "Tailor's workshop"), [
        (1, "linen-tunic", "Tunique de lin", "Linen Tunic", "Chest", "Ea", ["wild-flax", "nettle", "wolf-pelt"]),
        (5, "oat-straw-hat", "Chapeau de paille d'avoine", "Oat-Straw Hat", "Hat", "Fe", ["wild-oats", "wood-mint", "boar-bristle"]),
        (10, "hemp-shoulders", "Épaulières de chanvre", "Hemp Shoulder Guards", "Shoulders", "Te", ["pool-hemp", "glowmoss", "rotten-cloth"]),
        (15, "rye-pelerine", "Pèlerine de seigle", "Rye Pelerine", "Cape", "Ai", ["rye", "clubmoss", "spore-dust"]),
        (20, "sedge-gambeson", "Gambison de laîche", "Sedge Gambeson", "Chest", "Fe", ["shore-sedge", "water-lily", "crab-shell"]),
        (25, "rice-sunhat", "Capeline de riz des joncs", "Reed-Rice Sunhat", "Hat", "Ai", ["reed-rice", "horsetail", "slime"]),
        (30, "wheat-spaulders", "Épaulières de blé tressé", "Braided Wheat Spaulders", "Shoulders", "Ea", ["clairval-wheat", "hive-flower", "bee-wing"]),
        (35, "harvest-mantle", "Manteau de moisson", "Harvest Mantle", "Cape", "Te", ["buckwheat", "lavender", "mole-fur"]),
        (40, "spelt-doublet", "Pourpoint d'épeautre", "Spelt Doublet", "Chest", "Ai", ["spelt", "sage", "stag-velvet"]),
        (45, "sorghum-hood", "Capuche de sorgho", "Sorghum Hood", "Hat", "Fe", ["sorghum", "mandrake", "keeper-moss"])])),
    ("jeweller", (text("Bijoutier", "Jeweller"), text("Établi de bijouterie", "Jeweller's bench"), [
        (1, "copper-band", "Jonc de cuivre", "Copper Band", "Ring", "Te", ["copper-ore", "gudgeon", "boar-tusk"]),
        (5, "tin-pendant", "Pendentif d'étain", "Tin Pendant", "Amulet", "Ea", ["tin-ore", "crayfish", "wolf-fang"]),
        (10, "blackened-iron-ring", "Anneau de fer noirci", "Blackened Iron Ring", "Ring", "Fe", ["iron-ore", "pool-eel", "old-bone"]),
        (15, "sulphur-medallion", "Médaillon de soufre", "Sulphur Medallion", "Amulet", "Ai", ["sulphur", "tench", "spore-dust"]),
        (20, "blue-faience-ring", "Bague de faïence bleue", "Blue Faience Ring", "Ring", "Ea", ["blue-clay", "perch", "crab-claw"]),
        (25, "zinc-torc", "Torque de zinc", "Zinc Torc", "Amulet", "Te", ["zinc-ore", "carp", "frog-leg"]),
        (30, "silver-signet", "Chevalière d'argent", "Silver Signet", "Ring", "Ai", ["silver-ore", "pike", "honeycomb"]),
        (35, "quartz-pendant", "Pendeloque de quartz", "Quartz Drop Pendant", "Amulet", "Fe", ["quartz", "zander", "bee-wing"]),
        (40, "granite-ring", "Anneau de granit", "Granite Ring", "Ring", "Te", ["granite", "trout", "stone-antler"]),
        (45, "malachite-amulet", "Amulette de malachite", "Malachite Amulet", "Amulet", "Ea", ["malachite", "catfish", "mossy-cobble"])])),
    ("cobbler", (text("Cordonnier", "Cobbler"), text("Banc de cordonnerie", "Cobbler's bench"), [
        (1, "gudgeon-shoes", "Souliers en peau de goujon", "Gudgeon-Skin Shoes", "Boots", "Ai", ["gudgeon", "oak-log", "wolf-pelt"]),
        (5, "crayfish-belt", "Ceinture à carapaces d'écrevisse", "Crayfish-Shell Belt", "Belt", "Fe", ["crayfish", "hazel-log", "boar-bristle"]),
        (10, "eel-skin-boots", "Bottes en peau d'anguille", "Eel-Skin Boots", "Boots", "Te", ["pool-eel", "blackwood-log", "mushroom-cap"]),
        (15, "tench-swordbelt", "Ceinturon de tanche", "Tench-Leather Swordbelt", "Belt", "Ea", ["tench", "yew-log", "rotten-cloth"]),
        (20, "perch-waders", "Cuissardes en peau de perche", "Perch-Skin Waders", "Boots", "Fe", ["perch", "willow-log", "frog-leg"]),
        (25, "carp-belt", "Ceinture en cuir de carpe", "Carp-Leather Belt", "Belt", "Ai", ["carp", "alder-log", "slime"]),
        (30, "pike-skin-boots", "Bottes en peau de brochet", "Pike-Skin Boots", "Boots", "Ea", ["pike", "ash-log", "mole-fur"]),
        (35, "zander-baldric", "Baudrier de sandre", "Zander Baldric", "Belt", "Te", ["zander", "linden-log", "honeycomb"]),
        (40, "trout-skin-boots", "Bottes en peau de truite", "Trout-Skin Boots", "Boots", "Ea", ["trout", "hornbeam-log", "stag-velvet"]),
        (45, "catfish-belt", "Ceinture en cuir de silure", "Catfish-Leather Belt", "Belt", "Te", ["catfish", "elm-log", "keeper-moss"])])),
])
# The consumables' trades (sprint 111, D71, §5.5 and §5 bis.5; out of a fight only since sprint 115, D77): the alchemist's
# healing potions (50 + 20 × the item's level hit points at once, recipes 1, 10, 20, 30), the baker's breads (30 + 10 × the
# recipe's level hit points), all of them eaten or drunk out of a fight; the first bread is the village's loaf that Rose
# sells. Their ingredients: the herbalist's plant of the recipe's level and a fish of that level (potions); three of the
# farmer's grain and a plant (breads). The oils of sprint 111 went with the consumables in a fight.
HERBS = {1: "nettle", 5: "wood-mint", 10: "glowmoss", 15: "clubmoss", 20: "water-lily", 25: "horsetail", 30: "hive-flower", 35: "lavender", 40: "sage", 45: "mandrake"}
FISH = {1: "gudgeon", 10: "pool-eel", 20: "perch", 30: "pike", 40: "trout"}
GRAIN = {1: "wild-flax", 5: "wild-oats", 10: "pool-hemp", 15: "rye", 20: "shore-sedge", 25: "reed-rice", 30: "clairval-wheat", 35: "buckwheat", 40: "spelt", 45: "sorghum"}
POTIONS = [(1, "healing-potion", "Potion de soin", "Healing Potion", (4, 2)), (10, "strong-healing-potion", "Potion de soin forte", "Strong Healing Potion", (5, 2)),
           (20, "greater-healing-potion", "Grande potion de soin", "Greater Healing Potion", (6, 3)), (30, "healing-elixir", "Élixir de soin", "Healing Elixir", (7, 3)),
           (40, "greater-healing-elixir", "Grand élixir de soin", "Greater Healing Elixir", (8, 4))]
OILS = [size.format(e=e) for size in ("{e}-oil", "concentrated-{e}-oil", "{e}-essence", "{e}-quintessence") for e in ("earth", "fire", "water", "air")]
BREADS = [(1, "bread", "Pain", "Bread"), (5, "oat-bread", "Pain d'avoine", "Oat Bread"), (10, "hemp-bread", "Pain de chanvre", "Hemp Bread"),
          (15, "rye-bread", "Pain de seigle", "Rye Bread"), (20, "sedge-flatbread", "Galette de laîche", "Sedge Flatbread"),
          (25, "reed-rice-bread", "Pain de riz des joncs", "Reed-Rice Bread"), (30, "clairval-loaf", "Miche de Clairval", "Clairval Loaf"),
          (35, "buckwheat-galette", "Galette de sarrasin", "Buckwheat Galette"), (40, "spelt-bread", "Pain d'épeautre", "Spelt Bread"),
          (45, "sorghum-flatbread", "Galette de sorgho", "Sorghum Flatbread")]
CONSUMABLES = {"alchemist": (text("Alchimiste", "Alchemist"), text("Alambic", "Still")), "baker": (text("Boulanger", "Baker"), text("Four", "Oven"))}

# The crayfish lives at the bottom of the woods' waters, on 40 % of its maps only (§5 quater.4): fewer of them.
FEWER = {"crayfish-belt": 4}
STAKE = O([("item", "palisade-stake"), ("level", 1), ("ingredients", [O([("item", "oak-log"), ("count", 3)]), O([("item", "copper-ore"), ("count", 2)])])])


def stats(level, slot, element):
    """The rule of §5 quater.4: the budget of the loot, halved between the element and Vitality."""
    budget = math.ceil((8 + level) / 2) * (2 if slot == "TwoHanded" else 1)
    high, low = budget - budget // 2, budget // 2
    element_points, vitality = (low, high) if slot == "Shield" else (high, low)
    return O([("vitality", vitality), (ELEMENT[element], element_points)])


def inline(v):
    """One item per line, as tools/dungeon_gen.py writes the file."""
    if isinstance(v, dict):
        return "{ " + ", ".join(f"{json.dumps(k)}: {inline(x)}" for k, x in v.items()) + " }"
    if isinstance(v, list):
        return "[" + ", ".join(inline(x) for x in v) + "]"
    return json.dumps(v, ensure_ascii=False)


items = json.load(open(f"{root}/items.json"))
known = {i["id"]: i for i in items}
jobs = [j for j in json.load(open(f"{root}/jobs.json")) if j["id"] not in TRADES and j["id"] not in CONSUMABLES]
# The stake leaves the locksmith for the forge (T75 said "until the forge").
for j in jobs:
    j["recipes"] = [r for r in j["recipes"] if r["item"] != STAKE["item"]]
pieces = 0
for trade, (name, workshop, recipes) in TRADES.items():
    assert [r[0] for r in recipes] == LEVELS, trade
    made = [STAKE] if trade == "smith" else []
    for level, rid, fr, en, slot, element, (main, second, monster) in recipes:
        assert all(i in known for i in (main, second, monster)), rid
        two = slot == "TwoHanded"
        a, b, m = COUNTS[level]
        if rid in FEWER:
            a = FEWER[rid]
        counts = (math.ceil(a * 1.5), math.ceil(b * 1.5), m + 1) if two else (a, b, m)
        item = O([("id", rid), ("name", text(fr, en)), ("kind", "Equipment"), ("slot", slot), ("level", ITEM_LEVEL[level]),
                  ("stats", stats(ITEM_LEVEL[level], slot, element))])
        at = next((i for i, x in enumerate(items) if x["id"] == rid), None)
        if at is None:
            at = next(i for i, x in enumerate(items) if x["id"].endswith("-key") or x["id"] == "admin-keyring")
            items.insert(at, item)
        else:
            items[at] = item | ({"icon": items[at]["icon"]} if "icon" in items[at] else {})
        made.append(O([("item", rid), ("level", level),
                       ("ingredients", [O([("item", i), ("count", c)]) for i, c in zip((main, second, monster), counts)]),
                       ("shards", ITEM_LEVEL[level] * (2 if two else 1))]))
        pieces += 1
    jobs.append(O([("id", trade), ("name", name), ("workshop", workshop), ("recipes", made)]))


def put(item):
    """An item written in place, keeping its icon; a new one before the dungeons' keys."""
    at = next((i for i, x in enumerate(items) if x["id"] == item["id"]), None)
    if at is None:
        at = next(i for i, x in enumerate(items) if x["id"].endswith("-key") or x["id"] == "admin-keyring")
        items.insert(at, item)
    else:
        items[at] = item | ({"icon": items[at]["icon"]} if "icon" in items[at] else {})


def recipe(item, level, ingredients, shards=0):
    assert all(i in known for i, _ in ingredients), item
    return O([("item", item), ("level", level), ("ingredients", [O([("item", i), ("count", c)]) for i, c in ingredients])]
             + ([("shards", shards)] if shards else []))


brews = []
for level, iid, fr, en, (herbs, fish) in POTIONS:
    lv = ITEM_LEVEL[level]
    put(O([("id", iid), ("name", text(fr, en)), ("kind", "Consumable"), ("level", lv), ("heal", 50 + 20 * lv)]))
    brews.append(recipe(iid, level, [(HERBS[level], herbs), (FISH[level], fish)], max(1, lv // 2)))
items[:] = [i for i in items if i["id"] not in OILS]
loaves = []
for level, iid, fr, en in BREADS:
    put(O([("id", iid), ("name", text(fr, en)), ("kind", "Consumable"), ("level", level), ("heal", 30 + 10 * level)]))
    loaves.append(recipe(iid, level, [(GRAIN[level], 3), (HERBS[level], 1)]))
for trade, made in (("alchemist", brews), ("baker", loaves)):
    name, workshop = CONSUMABLES[trade]
    jobs.append(O([("id", trade), ("name", name), ("workshop", workshop), ("recipes", made)]))
with open(f"{root}/items.json", "w") as out:
    out.write("[\n" + ",\n".join("  " + inline(i) for i in items) + "\n]\n")
open(f"{root}/jobs.json", "w").write(json.dumps(jobs, ensure_ascii=False, indent=2) + "\n")
print(f"{len(TRADES)} equipment trades, {pieces} pieces; {len(brews)} potions, {len(loaves)} breads")
