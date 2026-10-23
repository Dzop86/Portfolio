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
def push(n, affects="Enemies"): return O([("kind", "push"), ("affects", affects), ("cells", n)])
def pull(n, affects="Enemies"): return O([("kind", "pull"), ("affects", affects), ("cells", n)])
# The caster lands on the target cell; trades places with the fighter there (sprint 90).
def leap(): return O([("kind", "leap"), ("affects", "Caster")])
def swap(affects="Allies"): return O([("kind", "swap"), ("affects", affects)])
def status(a, stat, v, t, el=None):
    d = O([("kind", "status"), ("affects", a), ("stat", stat), ("value", v), ("turns", t)])
    if el: d["element"] = el
    return d
# A glyph (sprint 130, T98): the floor cells of the spell's area marked for some of the caster's turns; damage, a status, or both.
def glyph(a, turns, dmin=0, dmax=0, el=None, stat=None, v=0, st=0):
    d = O([("kind", "glyph"), ("affects", a), ("turns", turns)])
    if dmax: d["damageMin"], d["damageMax"] = dmin, dmax
    if el: d["element"] = el
    if stat: d["status"], d["value"], d["statusTurns"] = stat, v, st
    return d
def summon(id, mx=1, count=1):
    d = O([("kind", "summon"), ("affects", "All"), ("summon", id), ("max", mx)])
    if count != 1: d["count"] = count
    return d

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
# The monsters' spells (sprint 72, D63): fixed numbers, set by simulation (T39).
beasts = [
 S("gore","Coup de boutoir","Gore",3,1,1,6,9,2,"Earth",1,ranks=False),
 S("charge","Charge","Charge",4,2,4,6,9,1,"Earth",1,line=True,effects=[push(2)],ranks=False),
 S("acorn-throw","Jet de glands","Acorn Throw",3,2,5,4,7,2,"Air",1,ranks=False),
 S("nip","Mordillement","Nip",2,1,1,3,5,2,"Earth",1,ranks=False),
 S("soothing-grunt","Grognement apaisant","Soothing Grunt",3,0,4,0,0,1,"Neutral",1,effects=[heal("Allies",6,9)],ranks=False,cd=1),
 S("royal-gore","Boutoir royal","Royal Gore",4,1,1,10,14,1,"Earth",1,ranks=False),
 S("stampede","Ruée","Stampede",4,1,2,7,10,1,"Earth",1,area=("Cross",1),ranks=False,cd=1),
 # The boars of D69 (sprint 91, docs/design-familles.md §4.1): charges into walls, a tank that tramples, a sow that calls her young back.
 S("trample","Piétinement","Trample",3,1,1,3,5,1,"Earth",1,area=("Line",1),effects=[status("Enemies","Mp",-1,1)],ranks=False,cd=1),
 S("sow-call","Rappel des petits","Call the Young",2,2,4,0,0,1,"Neutral",1,effects=[pull(2,"Allies")],ranks=False,cd=2),
 S("royal-charge","Charge royale","Royal Charge",4,2,5,8,11,1,"Earth",1,line=True,effects=[push(3)],ranks=False,cd=2),
 # The wolves (sprint 73): quick bites, a howl from afar, thick fur, a guard for the pack.
 S("bite","Morsure","Bite",3,1,1,5,8,2,"Earth",1,ranks=False),
 S("lunge","Bond","Lunge",2,2,4,0,0,1,"Neutral",1,effects=[leap()],ranks=False,cd=1),
 S("howl","Hurlement glaçant","Chilling Howl",3,2,5,3,6,1,"Air",1,effects=[status("Enemies","Mp",-1,1)],ranks=False),
 S("pack-guard","Garde de la meute","Pack Guard",3,0,4,0,0,1,"Neutral",1,effects=[shield("Allies",8,2)],ranks=False,cd=1),
 S("savage-bite","Morsure sauvage","Savage Bite",4,1,1,10,14,1,"Earth",1,ranks=False),
 S("pack-howl","Hurlement de meute","Pack Howl",4,1,3,6,9,1,"Air",1,area=("Circle",1),ranks=False,cd=1),
 # The wolves of D69 (sprint 91, §4.2): a tank that takes a hurt wolf's place, a she-wolf who rallies, the leader's cubs.
 S("intercept","S'interposer","Intercept",2,1,3,0,0,1,"Neutral",1,effects=[swap("Allies"),shield("Caster",10,2)],ranks=False,cd=2),
 S("rally-howl","Appel de la louve","Rallying Howl",3,0,0,0,0,1,"Neutral",1,area=("Circle",2),effects=[status("Allies","Mp",1,1)],ranks=False,cd=2),
 S("cub-bite","Mordillage de louveteau","Cub Bite",3,1,1,3,5,2,"Earth",1,ranks=False),
 # The mouldy woods (sprint 74): mushrooms and zombies, made for heroes of about level 10.
 S("cap-slam","Coup de chapeau","Cap Slam",3,1,1,8,12,2,"Earth",1,ranks=False),
 S("spore-burst","Nuage de spores","Spore Cloud",3,2,5,5,7,2,"Water",1,area=("Circle",1),effects=[status("Enemies","Poison",3,2,"Water")],ranks=False),
 S("mycelium-mend","Réseau mycélien","Mycelial Web",3,0,4,0,0,1,"Neutral",1,area=("Circle",1),effects=[heal("Allies",7,10)],ranks=False,cd=1),
 S("rot-spores","Spores pourries","Rot Spores",4,1,3,10,14,1,"Water",1,area=("Circle",1),ranks=False,cd=1),
 S("elder-slam","Coup ancestral","Elder Slam",4,1,1,15,20,1,"Earth",1,ranks=False),
 # The mushrooms of D69 (sprint 92, §4.3): the network, to be finished from afar.
 S("sticky-spore","Spore collante","Sticky Spore",3,1,2,5,7,1,"Water",1,effects=[status("Enemies","Mp",-1,1)],ranks=False),
 S("umbrella","Ombrelle","Umbrella",3,0,0,0,0,1,"Neutral",1,area=("Circle",1),effects=[shield("Allies",8,2)],ranks=False,cd=2),
 S("cutting","Bouture","Cutting",3,1,2,0,0,1,"Neutral",1,effects=[summon("spore-sprout",2)],ranks=False,cd=3),
 S("claw","Griffure","Claw",3,1,1,8,12,2,"Fire",1,ranks=False),
 S("bile-spit","Bile","Bile",3,2,5,6,9,2,"Water",1,effects=[status("Enemies","Poison",3,2,"Water")],ranks=False),
 S("stitch","Recousu de travers","Crooked Stitch",3,0,4,0,0,1,"Neutral",1,effects=[heal("Allies",12,16),status("Allies","Mp",-1,1)],ranks=False,cd=1),
 S("grave-smash","Coup de tombe","Grave Smash",4,1,1,15,20,1,"Fire",1,ranks=False),
 # The zombies of D69 (sprint 92, §4.4): each strikes in an element of its own; their overlord in all four.
 S("grip","Étreinte","Grip",3,1,1,4,6,1,"Fire",1,effects=[status("Enemies","Mp",-2,1)],ranks=False,cd=1),
 S("rot-fist","Poing pourri","Rotten Fist",3,1,1,8,12,2,"Earth",1,ranks=False),
 S("foul-breath","Haleine fétide","Foul Breath",2,0,0,0,0,1,"Neutral",1,area=("Circle",1),effects=[status("Enemies","Damage",-10,2)],ranks=False,cd=1),
 S("gust-claw","Griffe du vent","Gust Claw",3,1,1,7,10,2,"Air",1,ranks=False),
 S("tomb-vomit","Vomi de tombe","Grave Vomit",4,2,3,10,14,1,"Water",1,area=("Cross",1),ranks=False,cd=1),
 S("stone-fist","Poing de pierre","Stone Fist",3,1,1,12,16,1,"Earth",1,effects=[push(1)],ranks=False),
 S("wing-gust","Souffle des ailes","Wing Gust",4,1,3,10,14,1,"Air",1,area=("Line",2),ranks=False,cd=1),
 # The lake (sprint 75): frogs and crabs, made for heroes of about level 18.
 S("tongue-lash","Coup de langue","Tongue Lash",3,1,2,12,17,2,"Water",1,ranks=False),
 S("venom-spit","Crachat venimeux","Venom Spit",3,2,5,7,10,2,"Water",1,effects=[status("Enemies","Poison",4,2,"Water")],ranks=False),
 S("dew-mend","Rosée","Dew Mend",3,0,4,0,0,1,"Neutral",1,effects=[heal("Allies",11,15)],ranks=False,cd=1),
 S("tongue-crush","Langue écrasante","Tongue Crush",4,1,2,22,28,1,"Water",1,effects=[pull(2)],ranks=False),
 S("royal-croak","Coassement royal","Royal Croak",4,1,3,15,20,1,"Air",1,area=("Circle",1),ranks=False,cd=1),
 S("pinch","Pince","Pinch",3,1,1,12,17,2,"Earth",1,ranks=False),
 S("bubble-shot","Tir de bulles","Bubble Shot",3,2,5,9,13,2,"Water",1,effects=[push(1)],ranks=False),
 S("shell-up","Rentrer dans sa coquille","Shell Up",2,0,0,0,0,1,"Neutral",1,effects=[shield("Caster",20,2),status("Caster","Mp",-4,2)],ranks=False,cd=3),
 S("shell-guard","Garde de coquille","Shell Guard",3,0,4,0,0,1,"Neutral",1,effects=[shield("Allies",12,2)],ranks=False,cd=1),
 S("giant-pinch","Pince géante","Giant Pinch",4,1,1,22,28,1,"Earth",1,ranks=False),
 S("tide-sweep","Balayage de marée","Tide Sweep",4,1,2,15,20,1,"Water",1,area=("Cross",1),effects=[push(1)],ranks=False,cd=1),
 # The frogs of D69 (sprint 93, §4.5): they heal, leap and pull; the crabs (§4.6): armour to go round, a molt.
 S("frog-leap","Bond","Leap",2,2,4,0,0,1,"Neutral",1,effects=[leap()],ranks=False,cd=1),
 S("lasso-tongue","Langue-lasso","Lasso Tongue",3,2,4,6,9,1,"Water",1,effects=[pull(2)],ranks=False,cd=1),
 S("holding-pinch","Pince qui tient","Holding Pinch",3,1,1,8,11,1,"Earth",1,effects=[status("Enemies","Mp",-2,1)],ranks=False,cd=1),
 S("shell-swap","Changer de coquille","Shell Swap",2,1,4,0,0,1,"Neutral",1,effects=[swap()],ranks=False,cd=2),
 # The fields (sprint 76): bees and moles, made for heroes of about level 26.
 S("sting","Dard","Sting",3,1,2,16,22,2,"Air",1,ranks=False),
 S("stinger-shot","Tir de dard","Stinger Shot",3,2,6,13,19,2,"Air",1,ranks=False),
 S("royal-jelly","Gelée royale","Royal Jelly",3,0,4,0,0,1,"Neutral",1,effects=[heal("Allies",13,18)],ranks=False,cd=1),
 S("queen-sting","Dard de la reine","Queen's Sting",4,1,2,28,36,1,"Air",1,ranks=False),
 S("swarm","Essaim","Swarm",4,1,3,19,25,1,"Air",1,area=("Circle",1),ranks=False,cd=1),
 S("dig-claw","Griffe fouisseuse","Digging Claw",3,1,1,16,22,2,"Earth",1,ranks=False),
 S("clod-throw","Motte dans l'œil","Clod in the Eye",3,2,5,10,14,1,"Earth",1,effects=[status("Enemies","Ap",-1,1)],ranks=False),
 S("earth-ward","Rempart de terre","Earth Ward",3,0,4,0,0,1,"Neutral",1,effects=[shield("Allies",12,2)],ranks=False,cd=1),
 S("crushing-claws","Griffes broyeuses","Crushing Claws",4,1,1,28,36,1,"Earth",1,ranks=False),
 S("tremor","Secousse","Tremor",4,1,2,19,25,1,"Earth",1,area=("Cross",1),ranks=False,cd=1),
 # The bees of D69 (sprint 94, §4.7): the tempo, a swarm not to wake; the moles (§4.8): the ground, an enemy that surfaces.
 S("fire-sting","Piqûre brûlante","Burning Sting",3,1,2,16,22,2,"Fire",1,ranks=False),
 S("hot-wax","Cire brûlante","Hot Wax",3,1,1,6,9,1,"Fire",1,area=("Line",1),effects=[status("Enemies","Mp",-1,1)],ranks=False,cd=1),
 S("foraging","Butinage","Foraging",2,0,4,0,0,1,"Neutral",1,effects=[status("Allies","Ap",1,1)],ranks=False,cd=2),
 S("egg-laying","Ponte","Egg-Laying",3,1,2,0,0,1,"Neutral",1,effects=[summon("worker-spawn",2)],ranks=False,cd=3),
 S("surface","Surgir","Surface",2,2,5,0,0,1,"Neutral",1,los=False,effects=[leap()],ranks=False,cd=2),
 S("cave-in","Effondrement","Cave-in",3,2,3,6,9,1,"Earth",1,area=("Circle",1),effects=[status("Enemies","Mp",-2,1)],ranks=False,cd=2),
 # The Forest of the Ancestors' sylvan stags (sprint 132, docs/familles-foret.md §3): reading the ground, glyphs that burn and weigh.
 S("antler-jab","Coup d'andouiller","Antler Jab",3,1,1,19,25,2,"Earth",1,ranks=False),
 S("head-toss","Coup de tête","Head Toss",3,1,1,10,13,1,"Earth",1,effects=[push(2)],ranks=False,cd=1),
 S("antler-splinter","Jet de gravier","Gravel Throw",3,2,6,16,20,2,"Earth",1,ranks=False),
 S("scrape","Grattis","Scrape",2,2,5,0,0,1,"Neutral",1,area=("Circle",1),effects=[glyph("Enemies",2,7,9,"Earth")],ranks=False,cd=2),
 S("antler-butt","Coup de ramure","Antler Butt",3,1,1,19,25,2,"Earth",1,ranks=False),
 S("heavy-ground","Terre lourde","Heavy Ground",3,1,3,0,0,1,"Neutral",1,area=("Cross",1),effects=[glyph("Enemies",2,stat="Mp",v=-2,st=1)],ranks=False,cd=2),
 S("warm-breath","Souffle tiède","Warm Breath",3,0,4,0,0,1,"Neutral",1,effects=[heal("Allies",16,20)],ranks=False,cd=1),
 S("doe-call","Appel de la meneuse","Lead Hind's Call",2,2,4,0,0,1,"Neutral",1,effects=[pull(2,"Allies")],ranks=False,cd=2),
 S("hoof-kick","Ruade","Hoof Kick",3,1,2,19,25,1,"Earth",1,ranks=False),
 S("elder-antlers","Massacre de l'aïeul","Ancestor's Antlers",4,1,1,38,50,1,"Earth",1,effects=[push(1)],ranks=False),
 S("antler-hook","Crochet d'andouiller","Antler Hook",3,2,4,14,18,1,"Earth",1,effects=[pull(2)],ranks=False,cd=1),
 S("ancestors-ring","Brame de l'aïeul","Ancestor's Bellow",3,0,0,0,0,1,"Neutral",1,area=("Circle",2),effects=[glyph("Enemies",2,10,13,"Earth")],ranks=False,cd=2),
 # The mossy keepers (sprint 133, docs/familles-foret.md §4): dislodging, glyphs that strengthen the keepers standing in them.
 S("cobble-punch","Poing de moellon","Cobble Punch",3,1,1,21,27,2,"Earth",1,ranks=False),
 S("stone-grip","Prise de pierre","Stone Grip",3,1,2,12,15,1,"Earth",1,effects=[pull(1)],ranks=False,cd=1),
 S("pebble-shot","Galet","Pebble Shot",3,2,6,17,21,2,"Earth",1,ranks=False),
 S("rolled-rock","Roche roulée","Rolled Rock",3,2,4,10,13,1,"Earth",1,line=True,effects=[push(1)],ranks=False,cd=1),
 S("slab-slam","Coup de dalle","Slab Slam",3,1,1,20,26,2,"Earth",1,ranks=False),
 S("moss-circle","Cercle de mousse","Moss Circle",3,0,0,0,0,1,"Neutral",1,area=("Circle",1),effects=[glyph("Allies",2,stat="Resistance",v=20,st=2)],ranks=False,cd=2),
 S("moss-graft","Greffe de mousse","Moss Graft",3,0,4,0,0,1,"Neutral",1,effects=[heal("Allies",17,21)],ranks=False,cd=1),
 S("standing-ring","Mousse hérissée","Bristling Moss",3,1,4,0,0,1,"Neutral",1,area=("Cross",1),effects=[glyph("Allies",2,stat="Damage",v=20,st=2)],ranks=False,cd=2),
 S("mossy-fist","Poing moussu","Mossy Fist",3,1,2,20,26,1,"Earth",1,ranks=False),
 S("menhir-crush","Écrasement du menhir","Menhir Crush",4,1,1,38,50,1,"Earth",1,ranks=False),
 S("oath-ring","Cercle du serment","Oath Ring",3,0,0,0,0,1,"Neutral",1,area=("Circle",2),effects=[glyph("Allies",2,stat="Resistance",v=20,st=2)],ranks=False,cd=2),
 S("seismic-step","Pas sismique","Seismic Step",4,1,2,24,30,1,"Earth",1,area=("Cross",1),effects=[push(1)],ranks=False,cd=1),
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
for sp in base + sentinel + guard + mage + creatures + beasts:
    sp["crit"] = 10
spells = base + sentinel + guard + mage + creatures + beasts
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
# The young of the families' chiefs (D69): a monster's look, drawn smaller, and passives.
def young(id, fr, en, look, scale, hp, ap, mp, ini, spells, passives=None):
    d = O([("id",id),("name",O([("fr",fr),("en",en)])),("look",look),("hp",hp),("ap",ap),("mp",mp),("initiative",ini),("spells",spells)])
    if passives: d["passives"] = passives
    d["scale"] = scale
    return d
summons += [
 young("wolf-cub","Louveteau","Wolf Cub","wolf-melee",70,12,6,4,90,["cub-bite"]),
 young("spore-sprout","Pousse de vesse","Puffball Sprout","mushroom-ranged",70,20,6,2,80,["spore-burst"],["final-spores"]),
 young("bloated-spawn","Bouffi recousu","Stitched Bloater","zombie-tank",85,40,6,2,40,["rot-fist"],["explosion"]),
 young("hermit-spawn","Jeune bernard","Young Hermit","crab-support",70,30,6,3,55,["shell-guard","pinch"]),
 young("worker-spawn","Jeune ouvrière","Young Worker","bee-melee",70,30,6,4,95,["sting"],["alarm"]),
 young("digger-spawn","Jeune fouisseuse","Young Digger","mole-melee",70,35,6,3,60,["dig-claw","surface"]),
 # The menhir keeper's risen stones (sprint 133, docs/familles-foret.md §4.4).
 young("pebbling","Caillou éveillé","Pebbling","golem-melee",70,40,6,3,100,["cobble-punch"]),
]
open(f"{root}/summons.json", "w").write(json.dumps(summons, ensure_ascii=False, indent=2) + "\n")

# The monsters' passives (D69, data/passives.json): a trigger, its numbers, spell effects with the bearer as caster.
def P(id, fr, en, dfr, den, trigger, effects=None, area=None, **kw):
    d = O([("id",id),("name",O([("fr",fr),("en",en)])),("description",O([("fr",dfr),("en",den)])),("trigger",trigger)])
    if effects: d["effects"] = effects
    if area: d["area"] = O([("shape", area[0]), ("radius", area[1])])
    for k in ("melee", "percent", "range", "damageMin", "damageMax", "element"):
        if k in kw: d[k] = kw[k]
    return d
passives = [
 # The boars (§4.1): they avenge one another, the piglet flees, the loner's mud, the king's etched shard.
 P("herd-grudge","Rancune de la harde","Herd Grudge","Quand un sanglier tombe à 3 cases ou moins, gagne 1 PM et 15 % de dégâts pendant 2 tours.",
   "When a boar falls 3 cells away or less, gains 1 MP and 15% damage for 2 turns.","AllyDies",[status("Caster","Mp",1,2),status("Caster","Damage",15,2)],range=3),
 P("scamper","Détale","Scamper","Frappé au contact, gagne 2 PM pour fuir.","Struck up close, gains 2 MP to flee.","Hit",[status("Caster","Mp",2,1)],melee=True),
 P("mud-hide","Peau de boue","Mud Hide","Qui le frappe au contact perd 1 PM.","Whoever strikes it up close loses 1 MP.","Hit",[status("Attacker","Mp",-1,1)],melee=True),
 P("etched-shard","Éclat gravé","Etched Shard","Sous la moitié de ses PV, le signe de sa défense s'allume : +1 PA et +20 % de résistance Terre pendant 3 tours.",
   "Below half its HP, the sign on its tusk lights up: +1 AP and +20% Earth resistance for 3 turns.","LowHp",[status("Caster","Ap",1,3),status("Caster","Resistance",20,3,"Earth")],percent=50),
 # The wolves (§4.2): the pack's instinct, and the leader's cubs.
 P("pack-instinct","Instinct de meute","Pack Instinct","Au début de son tour, les loups à 2 cases ou moins gagnent 10 % de dégâts. Plusieurs loups cumulent leurs instincts.",
   "At the start of its turn, the wolves 2 cells away or less gain 10% damage. Several wolves add up their instincts.","TurnStart",[status("Allies","Damage",10,1)],area=("Circle",2)),
 P("pack-answers","La meute répond","The Pack Answers","Sous la moitié de ses PV, appelle deux louveteaux.","Below half its HP, calls two wolf cubs.","LowHp",[summon("wolf-cub",count=2)],percent=50),
 # The mushrooms (§4.3): spores at their death, the elder that roots itself.
 P("final-spores","Sporée finale","Final Spores","À sa mort, empoisonne ceux qui sont au contact (3 par tour, 2 tours).",
   "At its death, poisons those next to it (3 a turn, 2 turns).","Death",[status("Enemies","Poison",3,2,"Water")],area=("Circle",1)),
 P("puffball-burst","Éclate","Puffball Burst","À sa mort, éclate et empoisonne à 2 cases (4 par tour, 2 tours).",
   "At its death, bursts and poisons 2 cells around (4 a turn, 2 turns).","Death",[status("Enemies","Poison",4,2,"Water")],area=("Circle",2)),
 P("network-retracts","Le réseau se rétracte","The Web Retracts","Sous 30 % de ses PV, s'enracine : soigné de 30, +30 % de résistances, −2 PM pendant 2 tours.",
   "Below 30% of its HP, it roots itself: healed by 30, +30% resistances, −2 MP for 2 turns.","LowHp",[heal("Caster",30,30),status("Caster","Resistance",30,2),status("Caster","Mp",-2,2)],percent=30),
 # The zombies (§4.4): a contagion in each one's element, the bloater that explodes, the overlord's bloater.
 *[P(f"contagion-{el.lower()}","Contagion","Contagion","Qui le frappe au contact est empoisonné (2 par tour, 2 tours).",
   "Whoever strikes it up close is poisoned (2 a turn, 2 turns).","Hit",[status("Attacker","Poison",2,2,el)],melee=True) for el in ("Fire","Water","Earth","Air")],
 P("explosion","Explosion","Explosion","À sa mort, explose : 10 à 14 dégâts Terre à 2 cases, alliés compris.",
   "At its death, it explodes: 10 to 14 Earth damage 2 cells around, allies included.","Death",area=("Circle",2),damageMin=10,damageMax=14,element="Earth"),
 # The frogs (§4.5): damp skin that heals, the bullfrog that bounces, the toad king's lullaby.
 P("damp-skin","Peau humide","Damp Skin","Se soigne de 4 PV au début de son tour.","Heals itself by 4 HP at the start of its turn.","TurnStart",[heal("Caster",4,4)]),
 P("bounce","Rebond","Bounce","Qui le frappe au contact est repoussé d'une case.","Whoever strikes it up close is pushed back one cell.","Hit",[push(1,"Attacker")],melee=True),
 P("lullaby","Berceuse","Lullaby","Sous la moitié de ses PV, chante : les ennemis à 3 cases perdent 2 PA pendant un tour (tout de suite pour qui est en train de jouer).",
   "Below half its HP, it sings: the enemies 3 cells away lose 2 AP for a turn (at once for whoever is playing).","LowHp",[status("Enemies","Ap",-2,1)],area=("Circle",3),percent=50),
 # The crabs (§4.6): a molt that trades armour for speed, the giant's figurehead.
 P("molt","Mue","Molt","Sous 40 % de ses PV, mue : −30 % de résistances, mais +2 PM et +20 % de dégâts jusqu'à la fin du combat.",
   "Below 40% of its HP, it molts: −30% resistances, but +2 MP and +20% damage until the end of the fight.","LowHp",
   [status("Caster","Resistance",-30,10),status("Caster","Mp",2,10),status("Caster","Damage",20,10)],percent=40),
 P("figurehead","Proue gravée","Carved Figurehead","Sous la moitié de ses PV, se couvre de sa proue (bouclier de 40) et un bernard-l'ermite le rejoint.",
   "Below half its HP, it shields itself with its figurehead (40) and a hermit crab joins it.","LowHp",[shield("Caster",40,2),summon("hermit-spawn")],percent=50),
 # The bees (§4.7): the alarm that wakes the swarm, the overheated hive; the moles (§4.8): burrowing, the debris.
 P("alarm","Alarme","Alarm","Frappée, alerte les abeilles à 3 cases : +20 % de dégâts pendant 1 tour.",
   "Struck, it alerts the bees 3 cells away: +20% damage for 1 turn.","Hit",[status("Allies","Damage",20,1)],area=("Circle",3)),
 P("overheat","Ruche en surchauffe","Overheated Hive","Sous la moitié de ses PV, la ruche surchauffe : +30 % de dégâts, −15 % de résistances jusqu'à la fin.",
   "Below half its HP, the hive overheats: +30% damage, −15% resistances until the end.","LowHp",[status("Caster","Damage",30,10),status("Caster","Resistance",-15,10)],percent=50),
 P("bury","S'enfouir","Burrow Down","Sous la moitié de ses PV, s'enfouit : +40 % de résistances et +2 PM pendant 1 tour.",
   "Below half its HP, it burrows down: +40% resistances and +2 MP for 1 turn.","LowHp",[status("Caster","Resistance",40,1),status("Caster","Mp",2,1)],percent=50),
 P("the-debris","Le débris","The Debris","Sous la moitié de ses PV, déterre un éclat de métal inconnu : +2 PA pendant 2 tours, et deux fouisseuses surgissent.",
   "Below half its HP, it digs up a shard of unknown metal: +2 AP for 2 turns, and two diggers surface.","LowHp",[status("Caster","Ap",2,2),summon("digger-spawn",count=2)],percent=50),
 # The sylvan stags (docs/familles-foret.md §3.3, words of docs/familles-foret-textes.md): skittish when struck up close, the elder's antlers cracking open.
 P("skittish","Ombrageux","Skittish","Frappé au contact, gagne 2 PM à son prochain tour.","Struck up close, gains 2 MP on its next turn.","Hit",[status("Caster","Mp",2,1)],melee=True),
 P("cracked-antlers","Ramure fendue","Cracked Antlers","Sous la moitié de ses PV, sa ramure se fend : entre quatre signes, un creux gratté. +2 PA et −10 % de résistances jusqu'à la fin du combat.",
   "Below half its HP, its antlers split: between four signs, a scraped-out hollow. +2 AP and −10% resistances until the end of the fight.","LowHp",[status("Caster","Ap",2,10),status("Caster","Resistance",-10,10)],percent=50),
 # The mossy keepers (docs/familles-foret.md §4.3): a fallen keeper's stones shelter the others, the menhir's carved heart.
 P("cairn","Cairn","Cairn","Quand un gardien tombe à 3 cases ou moins, s'abrite derrière ses pierres : bouclier de 15 pendant 2 tours.",
   "When a keeper falls 3 cells away or less, it shelters behind the fallen stones: a shield of 15 for 2 turns.","AllyDies",[shield("Caster",15,2)],range=3),
 P("carved-heart","Cœur gravé","Carved Heart","Sous la moitié de ses PV, sa mousse tombe et laisse voir un cœur gravé ; deux pierres se lèvent à ses pieds. +20 % de dégâts et −15 % de résistances jusqu'à la fin du combat.",
   "Below half its HP, its moss falls away to show a carved heart, and two stones rise at its feet. +20% damage and −15% resistances until the end of the fight.","LowHp",
   [status("Caster","Damage",20,10),status("Caster","Resistance",-15,10),summon("pebbling",count=2)],percent=50),
 P("four-wings","Les quatre ailes","The Four Wings","Sous la moitié de ses PV, un bouffi se détache de lui.","Below half its HP, a bloater comes away from it.","LowHp",[summon("bloated-spawn")],percent=50),
 # The Disjoint (design-familles.md §4.9, sprint 114): four beasts forced into one, wearing itself out, bursting as it falls.
 P("unstable","Instable","Unstable","Il s'use tout seul : perd 15 PV au début de chacun de ses tours.","It wears itself out: loses 15 HP at the start of each of its turns.",
   "TurnStart",damageMin=15,damageMax=15,element="Neutral"),
 # A death that ends the fight sets off no passive: alone, it bursts as it is about to fall, below a tenth of its hit points.
 P("rupture","Rupture","Rupture","Sous un dixième de ses PV, se fend : 15 à 20 dégâts neutres à 2 cases, lui compris.","Below a tenth of its HP, it splits: 15 to 20 neutral damage 2 cells around, itself included.",
   "LowHp",area=("Circle",2),damageMin=15,damageMax=20,element="Neutral",percent=10),
]
open(f"{root}/passives.json", "w").write(json.dumps(passives, ensure_ascii=False, indent=2) + "\n")
print(len(spells), {k: len(v) for k, v in lists.items()})
