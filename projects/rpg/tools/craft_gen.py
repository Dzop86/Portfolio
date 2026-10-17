# The equipment trades (sprint 109, D76, docs/suite-apres-d69.md §5 quater): the smith (one- and two-handed
# weapons, shields) and the tailor (chest, shoulders, hat, cape), a recipe every 5 levels from 1 to 35. A piece's
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


LEVELS = [1, 5, 10, 15, 20, 25, 30, 35]
ITEM_LEVEL = {1: 3, 5: 6, 10: 10, 15: 14, 20: 18, 25: 22, 30: 26, 35: 30}
# Quantities by recipe level: main gatherer, second gatherer, monster's resource.
COUNTS = {1: (6, 2, 2), 5: (6, 3, 2), 10: (8, 3, 3), 15: (8, 4, 3), 20: (10, 4, 3), 25: (10, 5, 3), 30: (12, 5, 4), 35: (12, 6, 4)}
ELEMENT = {"Te": "earth", "Fe": "fire", "Ea": "water", "Ai": "air"}
# trade: (name, workshop, [(recipe level, id, fr, en, slot, element, [main, second, monster], icon)])
TRADES = O([
    ("smith", (text("Forgeron", "Smith"), text("Forge", "Forge"), [
        (1, "copper-billhook", "Serpe de cuivre", "Copper Billhook", "OneHanded", "Ai", ["oak-log", "copper-ore", "wolf-fang"]),
        (5, "hazel-pavise", "Pavois de noisetier", "Hazel Pavise", "Shield", "Te", ["hazel-log", "tin-ore", "boar-tusk"]),
        (10, "blackwood-maul", "Merlin de bois noir", "Blackwood Maul", "TwoHanded", "Fe", ["blackwood-log", "iron-ore", "old-bone"]),
        (15, "yew-targe", "Targe d'if", "Yew Targe", "Shield", "Ea", ["yew-log", "sulphur", "mushroom-cap"]),
        (20, "willow-cutlass", "Coutelas de saule", "Willow Cutlass", "OneHanded", "Ea", ["willow-log", "blue-clay", "crab-claw"]),
        (25, "alder-greataxe", "Grande hache d'aulne", "Alder Greataxe", "TwoHanded", "Te", ["alder-log", "zinc-ore", "crab-shell"]),
        (30, "ash-shield", "Écu de frêne", "Ash Heater Shield", "Shield", "Ai", ["ash-log", "silver-ore", "mole-fur"]),
        (35, "linden-falchion", "Fauchon de tilleul", "Linden Falchion", "OneHanded", "Fe", ["linden-log", "quartz", "honeycomb"])])),
    ("tailor", (text("Tailleur", "Tailor"), text("Atelier de couture", "Tailor's workshop"), [
        (1, "linen-tunic", "Tunique de lin", "Linen Tunic", "Chest", "Ea", ["wild-flax", "nettle", "wolf-pelt"]),
        (5, "oat-straw-hat", "Chapeau de paille d'avoine", "Oat-Straw Hat", "Hat", "Fe", ["wild-oats", "wood-mint", "boar-bristle"]),
        (10, "hemp-shoulders", "Épaulières de chanvre", "Hemp Shoulder Guards", "Shoulders", "Te", ["pool-hemp", "glowmoss", "rotten-cloth"]),
        (15, "rye-pelerine", "Pèlerine de seigle", "Rye Pelerine", "Cape", "Ai", ["rye", "clubmoss", "spore-dust"]),
        (20, "sedge-gambeson", "Gambison de laîche", "Sedge Gambeson", "Chest", "Fe", ["shore-sedge", "water-lily", "crab-shell"]),
        (25, "rice-sunhat", "Capeline de riz des joncs", "Reed-Rice Sunhat", "Hat", "Ai", ["reed-rice", "horsetail", "slime"]),
        (30, "wheat-spaulders", "Épaulières de blé tressé", "Braided Wheat Spaulders", "Shoulders", "Ea", ["clairval-wheat", "hive-flower", "bee-wing"]),
        (35, "harvest-mantle", "Mante de moisson", "Harvest Mantle", "Cape", "Te", ["buckwheat", "lavender", "mole-fur"])])),
])
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
jobs = [j for j in json.load(open(f"{root}/jobs.json")) if j["id"] not in TRADES]
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
with open(f"{root}/items.json", "w") as out:
    out.write("[\n" + ",\n".join("  " + inline(i) for i in items) + "\n]\n")
open(f"{root}/jobs.json", "w").write(json.dumps(jobs, ensure_ascii=False, indent=2) + "\n")
print(f"{len(TRADES)} equipment trades, {pieces} pieces")
