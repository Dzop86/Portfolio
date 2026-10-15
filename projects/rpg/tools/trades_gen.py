# The gathering trades (sprint 99, D71 and D72, docs/suite-apres-d69.md §5.2 to §5.4): woodcutter, miner,
# fisher, farmer and herbalist, each with a new resource every 5 levels; Clairval's zones carry the levels
# 1 to 35, two a zone (the first near the way in, the second at the far end). The resources go in
# data/items.json (before the dungeons' keys), the trades in data/jobs.json with what they gather and on
# which kind of spot. Deterministic and idempotent. Run from projects/rpg after tools/sets_gen.py, before
# tools/icons_gen.py:  python3 tools/trades_gen.py data
import collections, json, sys

root = sys.argv[1]
O = collections.OrderedDict


def text(fr, en): return O([("fr", fr), ("en", en)])


LEVELS = [1, 5, 10, 15, 20, 25, 30, 35]
# trade: (names, kind of spot, [(id, fr, en)] for the levels above)
TRADES = O([
    ("woodcutter", (text("Bûcheron", "Woodcutter"), "Tree", [
        ("oak-log", "Chêne", "Oak"), ("hazel-log", "Noisetier", "Hazel"), ("blackwood-log", "Bois noir", "Blackwood"),
        ("yew-log", "If", "Yew"), ("willow-log", "Saule", "Willow"), ("alder-log", "Aulne", "Alder"),
        ("ash-log", "Frêne", "Ash"), ("linden-log", "Tilleul", "Linden")])),
    ("miner", (text("Mineur", "Miner"), "Rock", [
        ("copper-ore", "Cuivre", "Copper"), ("tin-ore", "Étain", "Tin"), ("iron-ore", "Fer", "Iron"),
        ("sulphur", "Soufre", "Sulphur"), ("blue-clay", "Argile bleue", "Blue Clay"), ("zinc-ore", "Zinc", "Zinc"),
        ("silver-ore", "Argent", "Silver"), ("quartz", "Quartz", "Quartz")])),
    ("fisher", (text("Pêcheur", "Fisher"), "Water", [
        ("gudgeon", "Goujon", "Gudgeon"), ("crayfish", "Écrevisse", "Crayfish"), ("pool-eel", "Anguille des mares", "Pool Eel"),
        ("tench", "Tanche", "Tench"), ("perch", "Perche", "Perch"), ("carp", "Carpe", "Carp"),
        ("pike", "Brochet", "Pike"), ("zander", "Sandre", "Zander")])),
    ("farmer", (text("Paysan", "Farmer"), "Plot", [
        ("wild-flax", "Lin sauvage", "Wild Flax"), ("wild-oats", "Avoine folle", "Wild Oats"), ("pool-hemp", "Chanvre des mares", "Pool Hemp"),
        ("rye", "Seigle", "Rye"), ("shore-sedge", "Laîche des rives", "Shore Sedge"), ("reed-rice", "Riz des joncs", "Reed Rice"),
        ("clairval-wheat", "Blé de Clairval", "Clairval Wheat"), ("buckwheat", "Sarrasin", "Buckwheat")])),
    ("herbalist", (text("Herboriste", "Herbalist"), "Herb", [
        ("nettle", "Ortie", "Nettle"), ("wood-mint", "Menthe des bois", "Wood Mint"), ("glowmoss", "Mousse-lumière", "Glowmoss"),
        ("clubmoss", "Pied-de-loup", "Clubmoss"), ("water-lily", "Nénuphar", "Water Lily"), ("horsetail", "Prêle", "Horsetail"),
        ("hive-flower", "Fleur de ruche", "Hive Flower"), ("lavender", "Lavande", "Lavender")])),
])


def inline(v):
    """One item per line, as tools/dungeon_gen.py writes the file."""
    if isinstance(v, dict):
        return "{ " + ", ".join(f"{json.dumps(k)}: {inline(x)}" for k, x in v.items()) + " }"
    if isinstance(v, list):
        return "[" + ", ".join(inline(x) for x in v) + "]"
    return json.dumps(v, ensure_ascii=False)


items = json.load(open(f"{root}/items.json"))
jobs = [j for j in json.load(open(f"{root}/jobs.json")) if j["id"] not in TRADES]
for trade, (name, node, resources) in TRADES.items():
    assert len(resources) == len(LEVELS), trade
    harvests = []
    for level, (rid, fr, en) in zip(LEVELS, resources):
        item = O([("id", rid), ("name", text(fr, en)), ("kind", "Resource"), ("level", level)])
        at = next((i for i, x in enumerate(items) if x["id"] == rid), None)
        if at is None:
            at = next(i for i, x in enumerate(items) if x["id"].endswith("-key") or x["id"] == "admin-keyring")
            items.insert(at, item)
        else:
            items[at] = item | ({"icon": items[at]["icon"]} if "icon" in items[at] else {})
        harvests.append(O([("item", rid), ("level", level), ("node", node)]))
    jobs.append(O([("id", trade), ("name", name), ("recipes", []), ("harvests", harvests)]))
with open(f"{root}/items.json", "w") as out:
    out.write("[\n" + ",\n".join("  " + inline(i) for i in items) + "\n]\n")
open(f"{root}/jobs.json", "w").write(json.dumps(jobs, ensure_ascii=False, indent=2) + "\n")
print(f"{len(TRADES)} gathering trades, {len(TRADES) * len(LEVELS)} resources")
