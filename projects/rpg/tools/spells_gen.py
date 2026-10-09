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
sentinel = [
 S("recoil-shot","Tir de recul","Recoil Shot",3,1,5,4,6,2,"Air",1,effects=[push(2)]),
 S("frost-arrow","Flèche de givre","Frost Arrow",4,2,6,5,8,2,"Water",6,effects=[status("Enemies","Mp",-1,1)]),
 S("volley","Volée","Volley",5,3,7,5,8,1,"Air",9,area=("Cross",1)),
 S("hawk","Faucon","Hawk",4,1,3,0,0,1,"Neutral",13,los=False,effects=[summon("hawk")],cd=3),
 S("piercing-arrow","Flèche perçante","Piercing Arrow",4,2,8,6,9,2,"Air",17,line=True,area=("Line",2)),
 S("snare","Collet","Snare",3,1,4,0,0,1,"Water",21,effects=[status("Enemies","Mp",-2,1)],cd=2),
 S("arrow-rain","Pluie de flèches","Arrow Rain",5,3,8,6,9,1,"Water",26,los=False,area=("Circle",1)),
 S("focus","Concentration","Focus",2,0,0,0,0,1,"Neutral",31,los=False,effects=[status("Caster","Damage",30,2)],cd=3),
 S("venom-arrow","Flèche empoisonnée","Venom Arrow",3,2,6,3,4,2,"Water",36,effects=[status("Enemies","Poison",4,3,"Water")]),
 S("grapple","Grappin","Grapple",2,2,5,0,0,1,"Neutral",42,line=True,effects=[pull(3)],cd=1),
 S("precise-shot","Tir précis","Precise Shot",5,4,9,12,16,1,"Air",48),
 S("tide-arrow","Flèche de marée","Tide Arrow",4,2,6,8,11,2,"Water",54,effects=[push(1)]),
 S("sidestep","Dérobade","Sidestep",1,0,0,0,0,1,"Neutral",60,los=False,effects=[status("Caster","Mp",2,1)],cd=2),
 S("storm","Tempête","Storm",6,2,6,9,12,1,"Air",67,los=False,area=("Circle",2)),
 S("crippling-shot","Tir handicapant","Crippling Shot",4,2,7,7,9,1,"Air",75,effects=[status("Enemies","Ap",-2,1)],cd=2),
 S("deluge","Déluge","Deluge",6,3,8,10,13,1,"Water",85,los=False,area=("Cross",2)),
 S("skyfall","Chute céleste","Skyfall",6,3,9,24,30,1,"Air",100,area=("Circle",1),cd=2),
]
guard = [
 S("shield-bash","Coup de bouclier","Shield Bash",3,1,1,5,7,2,"Earth",1,effects=[push(2)]),
 S("bulwark","Rempart","Bulwark",2,0,0,0,0,1,"Neutral",6,los=False,effects=[shield("Caster",10,2)],cd=3),
 S("taunt","Provocation","Taunt",2,2,5,0,0,1,"Neutral",9,line=True,effects=[pull(3)],cd=1),
 S("cleave","Fauchage","Cleave",4,1,1,7,10,1,"Earth",13,area=("Cross",1)),
 S("burning-blade","Lame ardente","Burning Blade",4,1,1,9,13,2,"Fire",17),
 S("stone-skin","Peau de pierre","Stone Skin",2,0,0,0,0,1,"Neutral",21,los=False,effects=[status("Caster","Resistance",15,2)],cd=4),
 S("quake","Séisme","Quake",5,1,2,8,11,1,"Earth",26,area=("Circle",1)),
 S("war-cry","Cri de guerre","War Cry",3,0,0,0,0,1,"Neutral",31,los=False,area=("Circle",2),effects=[status("Allies","Damage",20,2)],cd=3),
 S("fire-lunge","Fente ardente","Fire Lunge",4,1,3,10,13,2,"Fire",36,line=True),
 S("iron-grip","Poigne de fer","Iron Grip",3,1,1,6,8,2,"Earth",42,effects=[status("Enemies","Mp",-2,1)],cd=2),
 S("second-wind","Second souffle","Second Wind",3,0,0,0,0,1,"Neutral",48,los=False,effects=[heal("Caster",10,14)],cd=4),
 S("smite","Châtiment","Smite",5,1,1,15,19,1,"Fire",54),
 S("shockwave","Onde de choc","Shockwave",4,1,3,9,12,1,"Earth",60,line=True,area=("Line",2),effects=[push(1)]),
 S("aegis","Égide","Aegis",4,0,3,0,0,1,"Neutral",67,los=False,area=("Circle",1),effects=[shield("Allies",20,2)],cd=3),
 S("inferno-blade","Lame infernale","Inferno Blade",5,1,1,14,18,1,"Fire",75,area=("Cross",1)),
 S("rampart-breaker","Brise-rempart","Rampart Breaker",5,1,1,12,15,1,"Earth",85,effects=[status("Enemies","Resistance",-20,2)]),
 S("titan-blow","Coup de titan","Titan Blow",7,1,1,28,34,1,"Earth",100,effects=[push(3)],cd=2),
]
mage = [
 S("ice-shard","Éclat de glace","Ice Shard",3,1,5,5,7,2,"Water",1),
 S("mend","Soin","Mend",3,0,5,0,0,1,"Neutral",6,effects=[heal("Allies",7,10)],cd=1),
 S("frost-ward","Bouclier de givre","Frost Ward",3,0,4,0,0,1,"Neutral",9,effects=[shield("Allies",10,1)],cd=2),
 S("elemental","Élémentaire","Elemental",4,1,2,0,0,1,"Neutral",13,los=False,effects=[summon("ember")],cd=3),
 S("burn","Brûlure","Burn",3,1,5,3,5,2,"Fire",17,effects=[status("Enemies","Poison",5,2,"Fire")]),
 S("tidal-wave","Raz-de-marée","Tidal Wave",5,2,5,8,11,1,"Water",21,line=True,area=("Line",2),effects=[push(1)]),
 S("meteor","Météore","Meteor",6,3,7,10,14,1,"Fire",26,los=False,area=("Circle",1)),
 S("renewal","Renouveau","Renewal",4,0,5,0,0,1,"Neutral",31,area=("Circle",1),effects=[heal("Allies",10,14)],cd=2),
 S("freeze","Gel","Freeze",4,1,5,6,8,1,"Water",36,effects=[status("Enemies","Mp",-2,1)],cd=1),
 S("ignite","Embrasement","Ignite",4,2,6,9,12,1,"Fire",42,area=("Cross",1)),
 S("mana-surge","Afflux","Mana Surge",1,0,0,0,0,1,"Neutral",48,los=False,effects=[status("Caster","Ap",2,1)],cd=3),
 S("maelstrom","Maelström","Maelstrom",5,2,6,10,13,1,"Water",54,area=("Circle",1),effects=[status("Enemies","Ap",-1,1)],cd=1),
 S("sanctuary","Sanctuaire","Sanctuary",4,0,4,0,0,1,"Neutral",60,los=False,area=("Circle",2),effects=[status("Allies","Resistance",15,2)],cd=3),
 S("pyre","Bûcher","Pyre",6,2,6,13,17,1,"Fire",67,effects=[status("Enemies","Poison",4,2,"Fire")]),
 S("glacier","Glacier","Glacier",6,2,6,12,15,1,"Water",75,los=False,area=("Cross",2)),
 S("rebirth","Renaissance","Rebirth",6,0,5,0,0,1,"Neutral",85,los=False,area=("Circle",2),effects=[heal("Allies",22,28)],cd=4),
 S("cataclysm","Cataclysme","Cataclysm",8,3,8,22,28,1,"Fire",100,los=False,area=("Circle",2),cd=2),
]
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
# The first spell of each class keeps its numbers, tuned on the level-1 duels.
for sp in sentinel + guard + mage:
    if sp["damageMax"] > 0 and sp.get("level", 1) > 1:
        budget(sp)
spells = base + sentinel + guard + mage + creatures
lines = ",\n".join("  " + json.dumps(s, ensure_ascii=False, separators=(", ", ": ")).replace('{"', '{ "').replace('}', ' }').replace('[ {', '[{').replace('} ]', '}]') for s in spells)
open(f"{root}/spells.json", "w").write("[\n" + lines + "\n]\n")
classes = json.load(open(f"{root}/classes.json"), object_pairs_hook=O)
lists = {"sentinel": ["strike", "arrow", "spear"] + [s["id"] for s in sentinel],
         "guard": ["strike", "axe", "spear"] + [s["id"] for s in guard],
         "mage": ["staff", "spark", "fireball"] + [s["id"] for s in mage]}
for c in classes:
    c["spells"] = lists[c["id"]]
open(f"{root}/classes.json", "w").write(json.dumps(classes, ensure_ascii=False, indent=2) + "\n")
summons = [
 O([("id","hawk"),("name",O([("fr","Faucon"),("en","Hawk")])),("look","female-a"),("hp",30),("ap",6),("mp",4),("initiative",130),("spells",["peck"])]),
 O([("id","ember"),("name",O([("fr","Braise"),("en","Ember")])),("look","male-e"),("hp",30),("ap",6),("mp",3),("initiative",90),("spells",["ember-bolt"])]),
]
open(f"{root}/summons.json", "w").write(json.dumps(summons, ensure_ascii=False, indent=2) + "\n")
print(len(spells), {k: len(v) for k, v in lists.items()})
