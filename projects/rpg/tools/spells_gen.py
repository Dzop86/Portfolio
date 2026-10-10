# Writes data/spells.json, the spell lists of data/classes.json and data/summons.json (sprint 56).
# The JSON files are what the game reads; this script keeps the damage rule of decision T27 in one
# place. Run from projects/rpg: python3 tools/spells_gen.py data
import json, collections, sys
root = sys.argv[1]
O = collections.OrderedDict
def S(id, fr, en, ap, mn, mx, dmin, dmax, per, el, level, los=True, line=False, area=None, effects=None, ranks=True, cd=0):
    d = O([("id", id), ("name", O([("fr", fr), ("en", en)])), ("apCost", ap), ("minRange", mn), ("maxRange", mx),
           ("lineOfSight", los), ("inLine", line), ("damageMin", dmin), ("damageMax", dmax), ("perTurn", per), ("element", el)])
    if area: d["area"] = O([("shape", area[0]), ("radius", area[1])])
    if effects: d["effects"] = effects
    if level != 1: d["level"] = level
    if cd: d["cooldown"] = cd
    if ranks and dmax > 0:
        d["ranks"] = [O([("damageMin", round(dmin * f)), ("damageMax", round(dmax * f))]) for f in (1.2, 1.4, 1.6, 1.8)]
    return d
def heal(a, mn, mx): return O([("kind", "heal"), ("affects", a), ("min", mn), ("max", mx)])
def shield(a, n, t): return O([("kind", "shield"), ("affects", a), ("amount", n), ("turns", t)])
def push(n): return O([("kind", "push"), ("affects", "Enemies"), ("cells", n)])
def pull(n): return O([("kind", "pull"), ("affects", "Enemies"), ("cells", n)])
def status(a, stat, v, t, el=None):
    d = O([("kind", "status"), ("affects", a), ("stat", stat), ("value", v), ("turns", t)])
    if el: d["element"] = el
    return d
def summon(id): return O([("kind", "summon"), ("affects", "All"), ("summon", id), ("max", 1)])

# The nine spells of before, unchanged at rank 1 (monsters and recorded fights use them).
base = [
 S("strike","Frappe","Strike",3,1,1,8,12,2,"Earth",1),
 S("arrow","Flèche","Arrow",4,2,6,6,10,2,"Air",1),
 S("spear","Lance","Spear",3,1,3,7,9,2,"Earth",1,line=True),
 S("club","Massue","Club",3,1,1,5,9,3,"Earth",1,ranks=False),
 S("sling","Fronde","Sling",3,1,4,3,6,1,"Air",1,los=False,ranks=False),
 S("axe","Hache","Axe",4,1,1,9,12,1,"Earth",1),
 S("staff","Bâton","Staff",3,1,1,5,8,2,"Fire",1),
 S("spark","Étincelle","Spark",2,1,4,4,6,2,"Fire",1,los=False),
 S("fireball","Boule de feu","Fireball",4,2,5,9,13,1,"Fire",1),
]
# D62: 24 spells per class, four per element and eight neutral ones; level 1 gives one per element and
# one neutral, then one every five levels: a neutral, Earth, Fire, Water, Air, three times, then the last
# four neutral ones. "icon" is a game-icons.net icon (CC BY 3.0, authors credited on the project page).
LEVELS = [1, 1, 1, 1, 1] + list(range(5, 100, 5))
def C(*rows):
    out = []
    for lvl, (args, kw, icon) in zip(LEVELS, rows):
        sp = S(*args, lvl, **kw)
        sp["icon"] = icon
        out.append(sp)
    return out
def R(id, fr, en, ap, mn, mx, dmin, dmax, per, el, icon, **kw): return ((id, fr, en, ap, mn, mx, dmin, dmax, per, el), kw, icon)
sentinel = C(
 R("stone-arrow","Flèche de pierre","Stone Arrow",3,2,6,1,1,2,"Earth","delapouite/plain-arrow"),
 R("fire-arrow","Flèche ardente","Burning Arrow",4,2,6,1,1,2,"Fire","lorc/flaming-arrow"),
 R("frost-arrow","Flèche de givre","Frost Arrow",4,2,6,1,1,2,"Water","lorc/ice-bolt",effects=[status("Enemies","Mp",-1,1)]),
 R("recoil-shot","Tir de recul","Recoil Shot",3,1,5,1,1,2,"Air","lorc/arrow-flights",effects=[push(2)]),
 R("sidestep","Dérobade","Sidestep",1,0,0,0,0,1,"Neutral","lorc/sprint",los=False,effects=[status("Caster","Mp",2,1)],cd=2),
 R("grapple","Grappin","Grapple",2,2,5,0,0,1,"Neutral","lorc/grapple",line=True,effects=[pull(3)],cd=1),
 R("heavy-shot","Tir lourd","Heavy Shot",4,2,5,1,1,1,"Earth","lorc/cannon-shot",effects=[push(1)]),
 R("ember-volley","Salve ardente","Ember Volley",5,3,7,1,1,1,"Fire","lorc/fire-ring",area=("Cross",1)),
 R("tide-arrow","Flèche de marée","Tide Arrow",4,2,6,1,1,2,"Water","lorc/wave-crest",effects=[push(1)]),
 R("piercing-arrow","Flèche perçante","Piercing Arrow",4,2,8,1,1,2,"Air","lorc/arrow-cluster",line=True,area=("Line",2)),
 R("hawk","Faucon","Hawk",4,1,3,0,0,1,"Neutral","lorc/hawk-emblem",los=False,effects=[summon("hawk")],cd=3),
 R("rockfall","Éboulis","Rockfall",5,3,8,1,1,1,"Earth","lorc/falling-boulder",los=False,area=("Circle",1)),
 R("searing-shot","Tir brûlant","Searing Shot",3,2,6,1,1,2,"Fire","lorc/burning-round-shot",effects=[status("Enemies","Poison",4,3,"Fire")]),
 R("venom-arrow","Flèche empoisonnée","Venom Arrow",3,2,6,1,1,2,"Water","sbed/poison",effects=[status("Enemies","Poison",4,3,"Water")]),
 R("gale","Rafale","Gale",5,3,8,1,1,1,"Air","lorc/whirlwind",los=False,area=("Circle",1)),
 R("focus","Concentration","Focus",2,0,0,0,0,1,"Neutral","delapouite/eye-target",los=False,effects=[status("Caster","Damage",30,2)],cd=3),
 R("stake","Pieu","Stake",4,2,7,1,1,1,"Earth","lorc/barbed-spear",effects=[status("Enemies","Mp",-2,1)],cd=2),
 R("ember-rain","Pluie de braises","Ember Rain",6,2,6,1,1,1,"Fire","lorc/burning-meteor",los=False,area=("Circle",2)),
 R("deluge","Déluge","Deluge",6,3,8,1,1,1,"Water","lorc/big-wave",los=False,area=("Cross",2)),
 R("skyfall","Chute céleste","Skyfall",6,3,9,1,1,1,"Air","lorc/lightning-arc",area=("Circle",1),cd=2),
 R("snare","Collet","Snare",3,1,4,0,0,1,"Neutral","lorc/mantrap",effects=[status("Enemies","Mp",-2,1)],cd=2),
 R("camouflage","Camouflage","Camouflage",2,0,0,0,0,1,"Neutral","lorc/hood",los=False,effects=[status("Caster","Resistance",20,2)],cd=4),
 R("warning-shot","Tir de semonce","Warning Shot",3,2,6,0,0,1,"Neutral","delapouite/human-target",effects=[status("Enemies","Ap",-2,1)],cd=2),
 R("field-dressing","Pansement","Field Dressing",3,0,0,0,0,1,"Neutral","delapouite/healing",los=False,effects=[heal("Caster",12,16)],cd=3),
)
guard = C(
 R("shield-bash","Coup de bouclier","Shield Bash",3,1,1,1,1,2,"Earth","delapouite/shield-bash",effects=[push(2)]),
 R("burning-blade","Lame ardente","Burning Blade",4,1,1,1,1,2,"Fire","delapouite/swords-power"),
 R("ice-blade","Lame de glace","Ice Blade",3,1,2,1,1,2,"Water","lorc/ice-spear",effects=[status("Enemies","Mp",-1,1)]),
 R("slash","Taillade","Slash",2,1,2,1,1,3,"Air","lorc/sword-slice"),
 # The guard fights up close: at level 1 it can already bring an enemy to it.
 R("taunt","Provocation","Taunt",2,2,5,0,0,1,"Neutral","lorc/grab",line=True,effects=[pull(3)],cd=1),
 R("bulwark","Rempart","Bulwark",2,0,0,0,0,1,"Neutral","lorc/shield-reflect",los=False,effects=[shield("Caster",10,2)],cd=3),
 R("cleave","Fauchage","Cleave",4,1,1,1,1,1,"Earth","lorc/sword-spin",area=("Cross",1)),
 R("fire-lunge","Fente ardente","Fire Lunge",4,1,3,1,1,2,"Fire","lorc/fire-dash",line=True),
 R("undertow","Ressac","Undertow",3,1,3,1,1,2,"Water","delapouite/high-tide",line=True,effects=[pull(1)]),
 R("wind-blade","Lame du vent","Wind Blade",3,1,2,1,1,2,"Air","lorc/wind-slap"),
 R("stone-skin","Peau de pierre","Stone Skin",2,0,0,0,0,1,"Neutral","delapouite/rock-golem",los=False,effects=[status("Caster","Resistance",15,2)],cd=4),
 R("quake","Séisme","Quake",5,1,2,1,1,1,"Earth","lorc/quake-stomp",area=("Circle",1)),
 R("smite","Châtiment","Smite",5,1,1,1,1,1,"Fire","lorc/fire-punch"),
 R("groundswell","Vague de fond","Groundswell",4,1,3,1,1,1,"Water","lorc/water-splash",line=True,area=("Line",2),effects=[push(1)]),
 R("cyclone-blade","Cyclone","Cyclone",4,1,3,1,1,1,"Air","lorc/tornado",area=("Circle",1)),
 R("war-cry","Cri de guerre","War Cry",3,0,0,0,0,1,"Neutral","lorc/shouting",los=False,area=("Circle",2),effects=[status("Allies","Damage",20,2)],cd=3),
 R("titan-blow","Coup de titan","Titan Blow",7,1,1,1,1,1,"Earth","lorc/thor-fist",effects=[push(3)],cd=2),
 R("inferno-blade","Lame infernale","Inferno Blade",5,1,1,1,1,1,"Fire","lorc/fire-ring",area=("Cross",1)),
 R("torrent","Torrent","Torrent",5,1,3,1,1,1,"Water","sbed/water-drop",area=("Circle",1)),
 R("hurricane","Ouragan","Hurricane",6,1,3,1,1,1,"Air","lorc/tornado-discs",area=("Circle",1),cd=1),
 R("iron-grip","Poigne de fer","Iron Grip",3,1,1,0,0,1,"Neutral","lorc/mailed-fist",effects=[status("Enemies","Mp",-2,1)],cd=2),
 R("second-wind","Second souffle","Second Wind",3,0,0,0,0,1,"Neutral","delapouite/healing-shield",los=False,effects=[heal("Caster",10,14)],cd=4),
 R("aegis","Égide","Aegis",4,0,3,0,0,1,"Neutral","delapouite/cross-shield",los=False,area=("Circle",1),effects=[shield("Allies",20,2)],cd=3),
 R("momentum","Élan","Momentum",1,0,0,0,0,1,"Neutral","lorc/run",los=False,effects=[status("Caster","Mp",2,1)],cd=2),
)
mage = C(
 R("pebble","Caillou","Pebble",3,1,5,1,1,2,"Earth","delapouite/stone-pile"),
 R("flare","Flammèche","Flare",3,1,5,1,1,2,"Fire","lorc/candle-flame"),
 R("ice-shard","Éclat de glace","Ice Shard",3,1,5,1,1,2,"Water","lorc/frozen-orb"),
 R("gust","Bourrasque","Gust",3,1,5,1,1,2,"Air","lorc/wind-hole",effects=[push(1)]),
 R("mend","Soin","Mend",3,0,5,0,0,1,"Neutral","delapouite/healing",effects=[heal("Allies",7,10)],cd=1),
 R("frost-ward","Bouclier de givre","Frost Ward",3,0,4,0,0,1,"Neutral","lorc/ice-shield",effects=[shield("Allies",10,1)],cd=2),
 R("roots","Racines","Roots",4,1,5,1,1,1,"Earth","delapouite/tree-roots",effects=[status("Enemies","Mp",-2,1)],cd=1),
 R("burn","Brûlure","Burn",3,1,5,1,1,2,"Fire","lorc/flame-spin",effects=[status("Enemies","Poison",5,2,"Fire")]),
 R("tidal-wave","Raz-de-marée","Tidal Wave",5,2,5,1,1,1,"Water","lorc/wave-strike",line=True,area=("Line",2),effects=[push(1)]),
 R("bolt","Éclair","Bolt",4,2,6,1,1,2,"Air","lorc/focused-lightning",line=True),
 R("elemental","Élémentaire","Elemental",4,1,2,0,0,1,"Neutral","lorc/fire-silhouette",los=False,effects=[summon("ember")],cd=3),
 R("landslide","Éboulement","Landslide",5,2,6,1,1,1,"Earth","delapouite/falling-rocks",los=False,area=("Circle",1)),
 R("meteor","Météore","Meteor",6,3,7,1,1,1,"Fire","lorc/meteor-impact",los=False,area=("Circle",1)),
 R("freeze","Gel","Freeze",4,1,5,1,1,1,"Water","lorc/frostfire",effects=[status("Enemies","Mp",-2,1)],cd=1),
 R("cyclone","Cyclone","Cyclone",5,2,6,1,1,1,"Air","lorc/stomp-tornado",area=("Circle",1),effects=[status("Enemies","Ap",-1,1)],cd=1),
 R("renewal","Renouveau","Renewal",4,0,5,0,0,1,"Neutral","sbed/health-increase",area=("Circle",1),effects=[heal("Allies",10,14)],cd=2),
 R("fault-line","Faille","Fault Line",5,1,5,1,1,1,"Earth","lorc/earth-crack",line=True,area=("Line",2)),
 R("cataclysm","Cataclysme","Cataclysm",8,3,8,1,1,1,"Fire","lorc/bright-explosion",los=False,area=("Circle",2),cd=2),
 R("glacier","Glacier","Glacier",6,2,6,1,1,1,"Water","lorc/icicles-aura",los=False,area=("Cross",2)),
 R("tempest","Tempête","Tempest",6,2,6,1,1,1,"Air","lorc/lightning-storm",los=False,area=("Circle",2)),
 R("mana-surge","Afflux","Mana Surge",1,0,0,0,0,1,"Neutral","delapouite/sparkles",los=False,effects=[status("Caster","Ap",2,1)],cd=3),
 R("sanctuary","Sanctuaire","Sanctuary",4,0,4,0,0,1,"Neutral","lorc/aura",los=False,area=("Circle",2),effects=[status("Allies","Resistance",15,2)],cd=3),
 R("rebirth","Renaissance","Rebirth",6,0,5,0,0,1,"Neutral","lorc/angel-wings",los=False,area=("Circle",2),effects=[heal("Allies",22,28)],cd=4),
 R("light-step","Pas léger","Light Step",1,0,0,0,0,1,"Neutral","delapouite/running-shoe",los=False,effects=[status("Caster","Mp",2,1)],cd=2),
)
creatures = [
 S("peck","Coup de bec","Peck",3,1,1,5,8,2,"Air",1,ranks=False),
 S("ember-bolt","Trait de braise","Ember Bolt",3,1,3,4,7,1,"Fire",1,ranks=False),
]
# Damage budget: average damage per action point grows with the unlocking level; areas, effects,
# long range and poison cost some of it, melee and cooldowns give some back.
BUDGET, GROWTH = 3, 0.6
AREA = {("Cross", 1): 0.8, ("Line", 2): 0.8, ("Circle", 1): 0.7, ("Circle", 2): 0.6, ("Cross", 2): 0.6}
def budget(sp):
    f = BUDGET * (1 + GROWTH * sp.get("level", 1) / 100)
    if "area" in sp: f *= AREA[(sp["area"]["shape"], sp["area"]["radius"])]
    kinds = [(e["kind"], e.get("stat")) for e in sp.get("effects", [])]
    if any(k in ("push", "pull") for k, _ in kinds): f *= 0.85
    if any(st in ("Mp", "Ap") for _, st in kinds): f *= 0.8
    if any(st == "Poison" for _, st in kinds): f *= 0.6
    if sp["maxRange"] >= 6: f *= 0.9
    if sp["maxRange"] == 1: f *= 1.1
    f *= 1 + 0.25 * sp.get("cooldown", 0)
    avg = f * sp["apCost"]
    sp["damageMin"], sp["damageMax"] = max(1, round(avg * 0.85)), max(1, round(avg * 1.15))
    sp["ranks"] = [O([("damageMin", round(sp["damageMin"] * g)), ("damageMax", round(sp["damageMax"] * g))]) for g in (1.2, 1.4, 1.6, 1.8)]
# Every damaging class spell gets its numbers from the budget rule (the placeholders 1 to 1 above).
for sp in sentinel + guard + mage:
    if sp["damageMax"] > 0:
        budget(sp)
# Every spell of the game may land a critical hit, one time in ten (D61); "crit" left out means never.
for sp in base + sentinel + guard + mage + creatures:
    sp["crit"] = 10
spells = base + sentinel + guard + mage + creatures
lines = ",\n".join("  " + json.dumps(s, ensure_ascii=False, separators=(", ", ": ")).replace('{"', '{ "').replace('}', ' }').replace('[ {', '[{').replace('} ]', '}]') for s in spells)
open(f"{root}/spells.json", "w").write("[\n" + lines + "\n]\n")
classes = json.load(open(f"{root}/classes.json"), object_pairs_hook=O)
lists = {"sentinel": [s["id"] for s in sentinel], "guard": [s["id"] for s in guard], "mage": [s["id"] for s in mage]}
for c in classes:
    c["spells"] = lists[c["id"]]
open(f"{root}/classes.json", "w").write(json.dumps(classes, ensure_ascii=False, indent=2) + "\n")
summons = [
 O([("id","hawk"),("name",O([("fr","Faucon"),("en","Hawk")])),("look","female-a"),("hp",30),("ap",6),("mp",4),("initiative",130),("spells",["peck"])]),
 O([("id","ember"),("name",O([("fr","Braise"),("en","Ember")])),("look","male-e"),("hp",30),("ap",6),("mp",3),("initiative",90),("spells",["ember-bolt"])]),
]
open(f"{root}/summons.json", "w").write(json.dumps(summons, ensure_ascii=False, indent=2) + "\n")
print(len(spells), {k: len(v) for k, v in lists.items()})
