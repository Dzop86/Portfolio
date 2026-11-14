# Writes data/spells.json, the spell lists of data/classes.json and data/summons.json (sprint 56).
# The JSON files are what the game reads; this script keeps the damage rule of decision T27 in one
# place. Run from projects/rpg: python3 tools/spells_gen.py data
import json, collections, sys
root = sys.argv[1]
O = collections.OrderedDict
def S(id, fr, en, ap, mn, mx, dmin, dmax, per, el, level, los=True, line=False, area=None, effects=None, ranks=True, cd=0, adaptive=False, needs=None, aims=None, spends=0, hp=0, form=False, corpse=False):
    d = O([("id", id), ("name", O([("fr", fr), ("en", en)])), ("apCost", ap), ("minRange", mn), ("maxRange", mx),
           ("lineOfSight", los), ("inLine", line), ("damageMin", dmin), ("damageMax", dmax), ("perTurn", per), ("element", el)])
    if area: d["area"] = O([("shape", area[0]), ("radius", area[1])])
    if effects: d["effects"] = effects
    if level != 1: d["level"] = level
    if cd: d["cooldown"] = cd
    # A path's spell (D95): in the hero's strongest element; bound to a creature (sprint 191, brick F).
    if adaptive: d["adaptive"] = True
    if needs: d["needs"] = needs
    if aims: d["aims"] = aims
    if spends: d["spendsShield"] = spends
    # The Berserker's (sprint 206, docs/classes-berserker.md §1): paid in hit points; asking for its Rage.
    if hp: d["hpCost"] = hp
    if form: d["form"] = True
    # The Necromancer's (sprint 214, docs/classes-necromancien.md §1): a fallen enemy's corpse on the target cell, taken.
    if corpse: d["corpse"] = True
    if ranks and dmax > 0:
        d["ranks"] = [O([("damageMin", round(dmin * f)), ("damageMax", round(dmax * f))]) for f in (1.2, 1.4, 1.6, 1.8)]
    return d
def heal(a, mn, mx, overflow=False): return O([("kind", "heal"), ("affects", a), ("min", mn), ("max", mx)] + ([("overflow", True), ("grows", True)] if overflow else []))
# The Priest's (sprint 201, docs/classes-pretre.md §2): an ally's ailments taken, an enemy's boons and shields.
def purge(a): return O([("kind", "purge"), ("affects", a)])
# The Cleric's (sprint 202, docs/classes-pretre-2.md §2): a fallen ally raised with a share of its hit points, once a fight.
def revive(a, percent): return O([("kind", "revive"), ("affects", a), ("percent", percent)])
# The Berserker's (sprint 206): a share of the damage done to enemies, back as hit points.
def drain(percent): return O([("kind", "drain"), ("affects", "Caster"), ("percent", percent)])
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
def glyph(a, turns, dmin=0, dmax=0, el=None, stat=None, v=0, st=0, marks=False, welcomes=False):
    d = O([("kind", "glyph"), ("affects", a), ("turns", turns)])
    if dmax: d["damageMin"], d["damageMax"] = dmin, dmax
    if el: d["element"] = el
    if stat: d["status"], d["value"], d["statusTurns"] = stat, v, st
    # A Seal's glyph (sprint 142) leaves its element's mark.
    if marks: d["marks"] = True
    # The Druid's (sprint 216, docs/classes-druide.md §1): an allies' glyph that helps at once those already on it.
    if welcomes: d["welcomes"] = True
    return d
# The Druid's shared glyph (sprint 216): on the same cells, one for the enemies and one, welcoming, for the allies.
def shared(turns, ally, foe=None):
    out = [glyph("Enemies", turns, **foe)] if foe else []
    return out + [glyph("Allies", turns, stat=ally[0], v=ally[1], st=ally[2], welcomes=True)]
# The Chronomancer's time (sprint 199, docs/classes-mage.md §3): points borrowed from the next turn, a place in the order, cooldowns shortened.
def borrow(n): return O([("kind", "borrow"), ("affects", "Caster"), ("ap", n)])
def delay(a, n): return O([("kind", "delay"), ("affects", a), ("places", n)])
def haste(a, n): return O([("kind", "haste"), ("affects", a), ("turns", n)])
# The Fusionist's (sprint 212, docs/classes-invocateur-2.md §2): the caster's creature aimed at dies into it.
def fuse(turns, percent): return O([("kind", "fuse"), ("affects", "Caster"), ("turns", turns), ("percent", percent)])
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
 R("rockfall","Éboulis","Rockfall",6,3,8,1,1,1,"Earth","lorc/falling-boulder",los=False,area=("Circle",1)),
 R("searing-shot","Tir brûlant","Searing Shot",3,2,6,1,1,2,"Fire","lorc/burning-round-shot",effects=[status("Enemies","Poison",4,3,"Fire")]),
 R("venom-arrow","Flèche empoisonnée","Venom Arrow",3,2,6,1,1,2,"Water","sbed/poison",effects=[status("Enemies","Poison",4,3,"Water")]),
 R("gale","Rafale","Gale",5,3,8,1,1,1,"Air","lorc/whirlwind",los=False,area=("Circle",1)),
 R("focus","Concentration","Focus",2,0,0,0,0,1,"Neutral","delapouite/eye-target",los=False,effects=[status("Caster","Damage",30,2)],cd=3),
 R("stake","Pieu","Stake",4,2,7,1,1,1,"Earth","lorc/barbed-spear",effects=[status("Enemies","Mp",-2,1)],cd=2),
 R("ember-rain","Pluie de braises","Ember Rain",6,2,6,1,1,1,"Fire","lorc/burning-meteor",los=False,area=("Circle",2)),
 R("deluge","Déluge","Deluge",6,3,8,1,1,1,"Water","lorc/big-wave",los=False,area=("Cross",2)),
 R("skyfall","Chute céleste","Skyfall",6,3,9,1,1,1,"Air","lorc/twister",area=("Circle",1),cd=2),
 R("snare","Collet","Snare",3,1,4,0,0,1,"Neutral","lorc/mantrap",effects=[status("Enemies","Mp",-2,1)],cd=2),
 R("camouflage","Camouflage","Camouflage",2,0,0,0,0,1,"Neutral","lorc/hood",los=False,effects=[status("Caster","Resistance",20,2)],cd=4),
 R("warning-shot","Tir de semonce","Warning Shot",3,2,6,0,0,1,"Neutral","delapouite/human-target",effects=[status("Enemies","Ap",-2,1)],cd=2),
 R("field-dressing","Pansement","Field Dressing",3,0,0,0,0,1,"Neutral","lorc/bandage-roll",los=False,effects=[heal("Caster",12,16)],cd=3),
)
guard = C(
 R("shield-bash","Coup de bouclier","Shield Bash",3,1,1,1,1,2,"Earth","delapouite/shield-bash",effects=[push(2)]),
 R("burning-blade","Lame ardente","Burning Blade",4,1,1,1,1,2,"Fire","delapouite/swords-power"),
 R("ice-blade","Lame de glace","Ice Blade",3,1,2,1,1,2,"Water","lorc/ice-spear",effects=[status("Enemies","Mp",-1,1)]),
 R("slash","Taillade","Slash",2,1,2,1,1,3,"Air","lorc/sword-slice"),
 # The guard fights up close: at level 1 it can already bring an enemy to it.
 R("taunt","Provocation","Taunt",2,2,5,0,0,1,"Neutral","lorc/grab",line=True,effects=[pull(3)],cd=1),
 R("bulwark","Rempart","Bulwark",2,0,0,0,0,1,"Neutral","lorc/shield-reflect",los=False,effects=[shield("Caster",10,2)],cd=3),
 R("cleave","Fauchage","Cleave",4,0,0,1,1,1,"Earth","lorc/sword-spin",area=("Cross",1)),
 R("fire-lunge","Fente ardente","Fire Lunge",4,1,3,1,1,2,"Fire","lorc/fire-dash",line=True),
 R("undertow","Ressac","Undertow",3,1,3,1,1,2,"Water","delapouite/high-tide",line=True,effects=[pull(1)]),
 R("wind-blade","Lame du vent","Wind Blade",3,1,2,1,1,2,"Air","lorc/wind-slap"),
 R("stone-skin","Peau de pierre","Stone Skin",2,0,0,0,0,1,"Neutral","delapouite/rock-golem",los=False,effects=[status("Caster","Resistance",15,2)],cd=4),
 R("quake","Séisme","Quake",5,1,2,1,1,1,"Earth","lorc/quake-stomp",area=("Circle",1)),
 R("smite","Châtiment","Smite",5,1,1,1,1,1,"Fire","lorc/fire-punch"),
 R("groundswell","Vague de fond","Groundswell",4,1,3,1,1,1,"Water","lorc/water-splash",line=True,area=("Line",2),effects=[push(1)]),
 R("cyclone-blade","Lame cyclone","Cyclone Blade",4,1,3,1,1,1,"Air","lorc/tornado",area=("Circle",1)),
 R("war-cry","Cri de guerre","War Cry",3,0,0,0,0,1,"Neutral","lorc/shouting",los=False,area=("Circle",2),effects=[status("Allies","Damage",20,2)],cd=3),
 R("titan-blow","Coup de titan","Titan Blow",7,1,1,1,1,1,"Earth","lorc/punch-blast",effects=[push(3)],cd=2),
 R("inferno-blade","Lame infernale","Inferno Blade",5,0,0,1,1,1,"Fire","lorc/fire-axe",area=("Cross",1)),
 R("torrent","Torrent","Torrent",5,1,3,1,1,1,"Water","sbed/water-drop",area=("Circle",1)),
 R("hurricane","Ouragan","Hurricane",6,1,3,1,1,1,"Air","lorc/tornado-discs",area=("Circle",1),cd=1),
 R("iron-grip","Poigne de fer","Iron Grip",3,1,1,0,0,1,"Neutral","lorc/mailed-fist",effects=[status("Enemies","Mp",-2,1)],cd=2),
 R("second-wind","Second souffle","Second Wind",3,0,0,0,0,1,"Neutral","delapouite/healing-shield",los=False,effects=[heal("Caster",10,14)],cd=4),
 # Sprint 208 (docs/classes-paladin.md §3): the Aegis went to the Paladin; the Warrior draws the enemies around it in its place.
 R("mass-taunt","Provocation de masse","Mass Taunt",4,0,0,0,0,1,"Neutral","lorc/divert",los=False,area=("Circle",2),effects=[pull(2)],cd=3),
 R("momentum","Élan","Momentum",1,0,0,0,0,1,"Neutral","lorc/run",los=False,effects=[status("Caster","Mp",2,1)],cd=2),
)
mage = C(
 R("pebble","Caillou","Pebble",3,1,5,1,1,2,"Earth","delapouite/stone-pile"),
 R("flare","Flammèche","Flare",3,1,5,1,1,2,"Fire","lorc/candle-flame"),
 R("ice-shard","Éclat de glace","Ice Shard",3,1,5,1,1,2,"Water","lorc/frozen-orb"),
 R("gust","Bourrasque","Gust",3,1,5,1,1,2,"Air","lorc/wind-hole",effects=[push(1)]),
 # The Priest took its heals (sprint 201, docs/classes-pretre.md §4): four neutral spells of its own in their places.
 R("blink","Clignement","Blink",2,1,3,0,0,1,"Neutral","lorc/teleport",effects=[leap()],cd=2),
 R("frost-ward","Bouclier de givre","Frost Ward",3,0,4,0,0,1,"Neutral","lorc/ice-shield",effects=[shield("Allies",10,1)],cd=2),
 R("roots","Racines","Roots",4,1,5,1,1,1,"Earth","delapouite/tree-roots",effects=[status("Enemies","Mp",-2,1)],cd=1),
 R("burn","Brûlure","Burn",3,1,5,1,1,2,"Fire","lorc/flame-spin",effects=[status("Enemies","Poison",5,2,"Fire")]),
 R("tidal-wave","Raz-de-marée","Tidal Wave",5,2,5,1,1,1,"Water","lorc/wave-strike",line=True,area=("Line",2),effects=[push(1)]),
 R("bolt","Trait de vent","Wind Dart",4,2,6,1,1,2,"Air","lorc/crossed-air-flows",line=True),
 R("elemental","Élémentaire","Elemental",4,1,2,0,0,1,"Neutral","lorc/fire-silhouette",los=False,effects=[summon("ember")],cd=3),
 R("landslide","Éboulement","Landslide",5,2,6,1,1,1,"Earth","delapouite/falling-rocks",los=False,area=("Circle",1)),
 R("meteor","Météore","Meteor",6,3,7,1,1,1,"Fire","lorc/meteor-impact",los=False,area=("Circle",1)),
 R("freeze","Gel","Freeze",4,1,5,1,1,1,"Water","lorc/frostfire",effects=[status("Enemies","Mp",-2,1)],cd=1),
 R("cyclone","Cyclone","Cyclone",5,2,6,1,1,1,"Air","lorc/stomp-tornado",area=("Circle",1),effects=[status("Enemies","Ap",-1,1)],cd=1),
 R("slow","Lenteur","Slow",3,1,6,0,0,1,"Neutral","lorc/snail",area=("Circle",1),effects=[status("Enemies","Mp",-2,1),status("Enemies","Ap",-1,1)],cd=2),
 R("fault-line","Faille","Fault Line",5,1,5,1,1,1,"Earth","lorc/earth-crack",line=True,area=("Line",2)),
 R("cataclysm","Cataclysme","Cataclysm",8,3,8,1,1,1,"Fire","lorc/bright-explosion",los=False,area=("Circle",2),cd=2),
 R("glacier","Glacier","Glacier",6,2,6,1,1,1,"Water","lorc/icicles-aura",los=False,area=("Cross",2)),
 R("tempest","Tempête","Tempest",6,2,6,1,1,1,"Air","lorc/entangled-typhoon",los=False,area=("Circle",2)),
 R("mana-surge","Afflux","Mana Surge",1,0,0,0,0,1,"Neutral","delapouite/sparkles",los=False,effects=[status("Caster","Ap",2,1)],cd=3),
 R("mana-barrier","Barrière de mana","Mana Barrier",3,0,0,0,0,1,"Neutral","lorc/magic-shield",los=False,effects=[shield("Caster",30,2)],cd=3),
 R("siphon","Siphon","Siphon",2,1,5,0,0,1,"Neutral","lorc/magic-swirl",effects=[status("Enemies","Ap",-2,1),status("Caster","Ap",2,1)],cd=3),
 R("light-step","Pas léger","Light Step",1,0,0,0,0,1,"Neutral","delapouite/running-shoe",los=False,effects=[status("Caster","Mp",2,1)],cd=2),
)
# The Priest (sprint 201, docs/classes-pretre.md §3): heals whose excess shields, never a blow from next to its target, damage at 0.8 of the
# budget. Mend, Renewal, Sanctuary and Grace (the Mage's Rebirth, renamed) came from the Mage. Working names, for the scriptwriter.
priest = C(
 R("blessed-stone","Pierre bénie","Blessed Stone",3,2,5,1,1,2,"Earth","lorc/stone-tablet"),
 R("holy-flame","Flamme sainte","Holy Flame",3,2,5,1,1,2,"Fire","lorc/candlebright"),
 R("holy-water","Eau bénite","Holy Water",3,2,5,1,1,2,"Water","lorc/splash"),
 R("sacred-breath","Souffle sacré","Sacred Breath",3,2,5,1,1,2,"Air","lorc/feathered-wing",effects=[push(1)]),
 R("mend","Soin","Mend",3,0,5,0,0,1,"Neutral","delapouite/healing",effects=[heal("Allies",7,10,overflow=True)],cd=1),
 R("prayer","Prière","Prayer",2,0,5,0,0,1,"Neutral","lorc/prayer",effects=[status("Allies","Regen",5,3)],cd=2),
 R("stone-verdict","Verdict de pierre","Stone Verdict",4,2,5,1,1,1,"Earth","lorc/scales",effects=[status("Enemies","Mp",-2,1)],cd=1),
 R("censer","Encensoir","Censer",3,2,5,1,1,2,"Fire","lorc/incense",effects=[status("Enemies","Poison",4,3,"Fire")]),
 R("blessed-tide","Marée bénite","Blessed Tide",4,2,5,1,1,1,"Water","delapouite/water-fountain",area=("Circle",1),effects=[heal("Allies",5,7,overflow=True)]),
 R("zephyr","Zéphyr","Zephyr",4,2,6,1,1,2,"Air","lorc/fluffy-cloud",line=True,area=("Line",2)),
 R("purify","Purification","Purify",2,0,5,0,0,1,"Neutral","lorc/beams-aura",los=False,area=("Circle",1),effects=[purge("Allies"),heal("Allies",6,8,overflow=True)],cd=2),
 R("stone-column","Colonne de pierre","Stone Column",5,2,6,1,1,1,"Earth","lorc/stone-tower",los=False,area=("Circle",1)),
 R("sacred-pyre","Bûcher sacré","Sacred Pyre",5,2,6,1,1,1,"Fire","lorc/fire-bowl",cd=1),
 R("baptism","Baptême","Baptism",5,2,5,1,1,1,"Water","lorc/holy-grail",area=("Cross",1),effects=[status("Enemies","Mp",-1,1)]),
 R("halo","Auréole","Halo",5,2,6,1,1,1,"Air","lorc/sun-radiations",area=("Circle",1),effects=[status("Enemies","Ap",-1,1)],cd=1),
 R("renewal","Renouveau","Renewal",4,0,5,0,0,1,"Neutral","sbed/health-increase",area=("Circle",1),effects=[heal("Allies",10,14,overflow=True)],cd=2),
 R("judgement","Jugement","Judgement",6,2,6,1,1,1,"Earth","lorc/broken-tablet",los=False,area=("Circle",1)),
 R("heavenly-blaze","Brasier céleste","Heavenly Blaze",6,2,6,1,1,1,"Fire","lorc/sunbeams",los=False,area=("Circle",2)),
 R("holy-flood","Crue sacrée","Holy Flood",6,2,6,1,1,1,"Water","lorc/at-sea",los=False,area=("Cross",2)),
 R("wrath-of-heaven","Courroux du ciel","Wrath of Heaven",6,2,6,1,1,1,"Air","lorc/cloudy-fork",area=("Circle",1),cd=2),
 R("sanctuary","Sanctuaire","Sanctuary",4,0,4,0,0,1,"Neutral","lorc/aura",los=False,area=("Circle",2),effects=[status("Allies","Resistance",15,2)],cd=3),
 R("grace","Grâce","Grace",6,0,5,0,0,1,"Neutral","lorc/angel-wings",los=False,area=("Circle",2),effects=[heal("Allies",22,28,overflow=True)],cd=4),
 R("blessing","Bénédiction","Blessing",3,0,5,0,0,1,"Neutral","lorc/glowing-hands",area=("Circle",1),effects=[status("Allies","Damage",20,2)],cd=3),
 R("serene-step","Pas serein","Serene Step",1,0,0,0,0,1,"Neutral","lorc/dove",los=False,effects=[status("Caster","Mp",2,1)],cd=2),
)
# The Rogue (Voleur) (sprint 204, docs/classes-voleur.md §4): one target, from next to it, then away; Stealth; at 1.05 of the budget. Working names.
rogue = C(
 R("pommel-strike","Coup de pommeau","Pommel Strike",3,1,1,1,1,2,"Earth","lorc/sword-hilt"),
 R("ember-dagger","Dague ardente","Ember Dagger",4,1,1,1,1,2,"Fire","lorc/curvy-knife"),
 R("gash","Entaille","Gash",3,1,2,1,1,2,"Water","lorc/bleeding-wound",effects=[status("Enemies","Poison",3,2,"Water")]),
 R("thrown-knife","Couteau lancé","Thrown Knife",3,1,3,1,1,2,"Air","lorc/thrown-knife"),
 R("dodge","Esquive","Dodge",2,1,3,0,0,1,"Neutral","delapouite/jump-across",effects=[leap()],cd=1),
 R("fade","Fondu","Fade",2,0,0,0,0,1,"Neutral","lorc/cloak-dagger",los=False,effects=[status("Caster","Hidden",1,2)],cd=3),
 R("trip","Croc-en-jambe","Trip",3,1,1,1,1,1,"Earth","lorc/foot-trip",effects=[status("Enemies","Mp",-2,1)],cd=1),
 R("fire-vial","Fiole ardente","Fire Vial",3,1,3,1,1,2,"Fire","lorc/bubbling-flask",effects=[status("Enemies","Poison",4,3,"Fire")]),
 R("bleed","Saignée","Bleed",4,1,1,1,1,2,"Water","lorc/bloody-sword",effects=[status("Enemies","Poison",5,3,"Water")]),
 R("pounce","Bond fendant","Pounce",4,2,3,1,1,1,"Air","lorc/backstab",line=True,area=("Cross",1),effects=[leap()],cd=1),
 R("smoke-bomb","Fumigène","Smoke Bomb",3,0,0,0,0,1,"Neutral","lorc/ninja-mask",los=False,area=("Circle",1),effects=[status("Enemies","Range",-2,1),status("Caster","Hidden",1,2)],cd=3),
 R("cosh","Assommoir","Cosh",5,1,1,1,1,1,"Earth","lorc/mace-head",effects=[status("Enemies","Ap",-1,1)]),
 R("red-blade","Lame rouge","Red Blade",5,1,1,1,1,1,"Fire","lorc/bowie-knife",cd=1),
 R("slow-venom","Venin lent","Slow Venom",4,1,2,1,1,1,"Water","lorc/poison-bottle",effects=[status("Enemies","Poison",6,3,"Water"),status("Enemies","Mp",-1,1)]),
 R("volte","Volte","Volte",5,1,2,1,1,1,"Air","lorc/spinning-sword",effects=[status("Caster","Mp",2,1)]),
 R("contract","Contrat","Contract",2,1,4,0,0,1,"Neutral","lorc/scroll-unfurled",effects=[status("Enemies","Resistance",-15,2)],cd=3),
 R("kneecap","Brise-genou","Kneecap",6,1,1,1,1,1,"Earth","lorc/broken-bone",effects=[status("Enemies","Mp",-2,1)],cd=2),
 R("greek-fire","Feu grégeois","Greek Fire",6,1,2,1,1,1,"Fire","lorc/fire-bottle",effects=[status("Enemies","Poison",8,3,"Fire")]),
 R("drown","Noyade","Drown",6,1,1,1,1,1,"Water","lorc/drowning",cd=1),
 R("blade-storm","Tempête de lames","Blade Storm",6,1,3,1,1,1,"Air","lorc/spinning-blades",cd=2),
 R("shadow-swap","Ombre portée","Shadow Swap",2,1,4,0,0,1,"Neutral","lorc/shadow-follower",effects=[swap("Enemies")],cd=2),
 R("antidote","Antidote","Antidote",3,0,0,0,0,1,"Neutral","lorc/round-bottom-flask",los=False,effects=[purge("Caster"),heal("Caster",12,16)],cd=4),
 R("killers-eye","Œil du tueur","Killer's Eye",2,0,0,0,0,1,"Neutral","lorc/eyeball",los=False,effects=[status("Caster","Damage",30,2)],cd=3),
 R("velvet-step","Pas de velours","Velvet Step",1,0,0,0,0,1,"Neutral","lorc/boot-prints",los=False,effects=[status("Caster","Mp",2,1)],cd=2),
)
# The Berserker (sprint 206, docs/classes-berserker.md §2): raw damage up close, paid in hit points; its Rage, a form for the rest of the fight, opens
# its biggest blows; its areas all around it spare it (Fight.Spares); no shield, no pull, no movement taken (§1.4, what sets it apart from the Warrior). At 1.1 of the budget. Working names.
berserker = C(
 R("cleaver","Fendoir","Cleaver",4,1,1,1,1,2,"Earth","lorc/meat-cleaver"),
 R("blood-axe","Hache de sang","Blood Axe",3,1,1,1,1,2,"Fire","lorc/bloody-sword",hp=6),
 R("tide-blow","Coup de ressac","Tide Blow",3,1,2,1,1,2,"Water","lorc/wave-strike"),
 R("whirl","Moulinet","Whirl",4,0,0,1,1,1,"Air","lorc/whirlwind",area=("Circle",1)),
 R("rush","Ruée","Rush",2,2,4,0,0,1,"Neutral","lorc/run",line=True,effects=[leap()],cd=1),
 R("rage","Rage","Rage",2,0,0,0,0,1,"Neutral","lorc/screaming",los=False,effects=[status("Caster","Rage",25,99)],hp=10),
 R("bone-breaker","Brise-os","Bone Breaker",4,1,1,1,1,1,"Earth","lorc/broken-skull",hp=6),
 R("blood-fury","Furie sanglante","Blood Fury",5,1,1,1,1,1,"Fire","lorc/burning-passion",hp=10,form=True),
 R("breaker","Déferlante","Breaker",4,1,2,1,1,1,"Water","lorc/water-splash",line=True,area=("Line",2)),
 R("war-whirl","Tourbillon de guerre","War Whirl",5,0,0,1,1,1,"Air","lorc/tornado",area=("Circle",1),form=True),
 R("war-howl","Hurlement","War Howl",3,0,0,0,0,1,"Neutral","lorc/wolf-howl",los=False,area=("Circle",2),effects=[status("Enemies","Damage",-15,2)],cd=3),
 R("rend","Déchirure","Rend",5,1,1,1,1,1,"Earth","lorc/claw-slashes",effects=[drain(30)]),
 R("ember-cleave","Fendoir ardent","Ember Cleave",5,0,0,1,1,1,"Fire","lorc/fire-axe",area=("Cross",1),hp=8),
 R("blood-tide","Marée de sang","Blood Tide",5,1,2,1,1,1,"Water","lorc/heart-drop",effects=[drain(30)],form=True),
 R("storm-axe","Hache-tempête","Storm Axe",5,1,3,1,1,1,"Air","lorc/axe-swing",line=True),
 R("blood-pact","Pacte de sang","Blood Pact",2,0,0,0,0,1,"Neutral","lorc/scar-wound",los=False,effects=[status("Caster","Ap",2,1)],hp=15,cd=3),
 R("earthshaker","Ébranlement","Earthshaker",6,0,0,1,1,1,"Earth","lorc/punch-blast",area=("Circle",1),hp=10,form=True,cd=1),
 R("inferno-axe","Hache infernale","Inferno Axe",6,1,1,1,1,1,"Fire","lorc/fire-breath",hp=12,cd=1),
 R("maelstrom","Maelström","Maelstrom",6,0,0,1,1,1,"Water","lorc/drop",area=("Cross",1),effects=[drain(30)]),
 R("tempest-blow","Coup de tempête","Tempest Blow",6,1,1,1,1,1,"Air","lorc/crossed-axes",hp=12,form=True,cd=2),
 R("blood-rush","Ivresse du sang","Blood Rush",3,0,0,0,0,1,"Neutral","lorc/bone-gnawer",los=False,effects=[heal("Caster",15,20)],cd=4),
 R("unbreakable","Inébranlable","Unbreakable",3,0,0,0,0,1,"Neutral","lorc/muscle-up",los=False,effects=[status("Caster","Resistance",20,2)],hp=15,cd=4),
 R("frenzy","Frénésie","Frenzy",2,0,0,0,0,1,"Neutral","lorc/strong",los=False,effects=[status("Caster","Damage",30,2)],cd=3),
 R("onrush","Élan furieux","Onrush",1,0,0,0,0,1,"Neutral","lorc/sprint",los=False,effects=[status("Caster","Mp",2,1)],cd=2),
)
# The Paladin (sprint 208, docs/classes-paladin.md §3): protects by striking, up close, its effects within 2 cells; no heal but on a blow, none further
# than 2 cells (§1.4, what sets it apart from the Priest). The Aegis came from the Warrior. At 0.85 of the budget. Working names.
paladin = C(
 R("holy-hammer","Marteau consacré","Hallowed Hammer",4,1,1,1,1,2,"Earth","lorc/hammer-drop"),
 R("radiant-blade","Lame radieuse","Radiant Blade",3,1,1,1,1,2,"Fire","lorc/winged-sword",effects=[heal("Caster",4,6)]),
 R("purifying-strike","Coup purifiant","Purifying Strike",3,1,2,1,1,2,"Water","lorc/beam-wake"),
 R("light-lance","Lance de lumière","Light Lance",3,1,2,1,1,2,"Air","lorc/spear-hook",line=True),
 R("steadfast","Constance","Steadfast",2,0,0,0,0,1,"Neutral","lorc/checked-shield",los=False,effects=[status("Caster","Resistance",10,2)],cd=2),
 R("aegis","Égide","Aegis",4,0,2,0,0,1,"Neutral","delapouite/cross-shield",los=False,area=("Circle",1),effects=[shield("Allies",20,2)],cd=3),
 R("rampart-strike","Frappe du rempart","Rampart Strike",4,1,1,1,1,1,"Earth","lorc/shield-reflect",effects=[shield("Caster",10,2)]),
 R("sun-strike","Frappe solaire","Sun Strike",4,1,2,1,1,1,"Fire","lorc/sun",effects=[heal("Caster",6,8)]),
 R("tide-of-faith","Flot de foi","Tide of Faith",4,1,2,1,1,1,"Water","lorc/ringed-beam",effects=[status("Enemies","Damage",-15,2)]),
 R("judgement-lance","Lance du jugement","Judgement Lance",4,1,2,1,1,1,"Air","lorc/halberd",line=True),
 R("oath","Serment","Oath",3,0,0,0,0,1,"Neutral","lorc/aura",los=False,area=("Circle",2),effects=[status("Allies","Resistance",10,2)],cd=3),
 R("crushing-faith","Foi écrasante","Crushing Faith",5,1,1,1,1,1,"Earth","lorc/hammer-nails"),
 R("dawn-blade","Lame de l'aube","Dawn Blade",5,1,1,1,1,1,"Fire","lorc/sunrise",effects=[heal("Caster",8,10)],cd=1),
 R("mercy-strike","Frappe de miséricorde","Mercy Strike",5,1,2,1,1,1,"Water","lorc/magic-palm",effects=[purge("Enemies")]),
 R("heavens-spear","Lance céleste","Heaven's Spear",5,1,3,1,1,1,"Air","lorc/sunbeams",line=True),
 R("sacred-guard","Garde sacrée","Sacred Guard",3,0,2,0,0,1,"Neutral","delapouite/heart-shield",los=False,effects=[shield("Allies",25,2)],cd=3),
 R("judgement-hammer","Marteau du jugement","Judgement Hammer",6,1,1,1,1,1,"Earth","lorc/crowned-explosion",effects=[status("Enemies","Ap",-1,1)],cd=1),
 R("blazing-oath","Serment ardent","Blazing Oath",6,1,1,1,1,1,"Fire","lorc/barbed-sun",effects=[heal("Caster",12,16)],cd=1),
 R("grace-flood","Flot de grâce","Flood of Grace",6,1,2,1,1,1,"Water","lorc/holy-symbol",effects=[status("Enemies","Damage",-20,2)],cd=1),
 R("zenith","Zénith","Zenith",6,1,2,1,1,1,"Air","lorc/templar-eye",cd=2),
 R("divine-shield","Bouclier divin","Divine Shield",3,0,0,0,0,1,"Neutral","lorc/winged-shield",los=False,effects=[shield("Caster",30,2)],cd=4),
 R("rally","Ralliement","Rally",3,0,0,0,0,1,"Neutral","lorc/medal",los=False,area=("Circle",2),effects=[status("Allies","Ap",1,1)],cd=4),
 R("penance","Pénitence","Penance",3,1,2,0,0,1,"Neutral","lorc/scales",effects=[status("Enemies","Damage",-25,2)],cd=3),
 R("guarded-step","Pas gardé","Guarded Step",1,0,0,0,0,1,"Neutral","lorc/boot-prints",los=False,effects=[status("Caster","Mp",2,1)],cd=2),
)
# The Summoner (Invocateur) (sprint 211, docs/classes-invocateur.md §4): plays through its creatures, fed by its class's rules; an order marks their target;
# weak blows of 1 to 5 cells, at 0.75 of the budget; no heal (what sets it apart from the Priest). Working names.
summoner = C(
 R("clod","Motte","Clod",3,1,5,1,1,2,"Earth","delapouite/stone-pile"),
 R("cinder","Escarbille","Cinder",3,1,5,1,1,2,"Fire","lorc/small-fire"),
 R("droplet","Goutte","Droplet",3,1,5,1,1,2,"Water","lorc/drop"),
 R("murmur","Murmure","Murmur",3,1,5,1,1,2,"Air","lorc/swirl-string",effects=[push(1)]),
 R("call-hound","Appel du molosse","Call the Hound",3,1,2,0,0,1,"Neutral","lorc/wolf-head",los=False,effects=[summon("hound",2)],cd=1),
 R("command","Ordre","Command",2,1,6,0,0,1,"Neutral","lorc/pointing",los=False,effects=[status("Enemies","Ordered",25,2)],cd=1),
 R("binding-stone","Pierre de lien","Binding Stone",4,1,4,1,1,2,"Earth","lorc/stone-sphere",effects=[status("Enemies","Mp",-1,1)]),
 R("searing-brand","Marque ardente","Searing Brand",4,1,4,1,1,2,"Fire","lorc/brandy-bottle",effects=[status("Enemies","Poison",4,3,"Fire")]),
 R("mire","Bourbier","Mire",4,1,4,1,1,2,"Water","lorc/bubbles",effects=[status("Enemies","Mp",-1,1)]),
 R("breath","Souffle","Breath",4,1,5,1,1,2,"Air","lorc/wind-slap",line=True),
 R("call-wisp","Feu follet","Will-o'-Wisp",3,1,3,0,0,1,"Neutral","lorc/spark-spirit",los=False,effects=[summon("wisp")],cd=2),
 R("rumble","Grondement","Rumble",5,2,5,1,1,1,"Earth","lorc/earth-spit",area=("Circle",1)),
 R("pyre","Bûcher","Pyre",5,2,5,1,1,1,"Fire","lorc/campfire",area=("Cross",1)),
 R("far-undertow","Ressac lointain","Far Undertow",5,1,4,1,1,1,"Water","lorc/waves",line=True,effects=[pull(1)]),
 R("squall","Grain","Squall",5,2,5,1,1,1,"Air","lorc/fluffy-cloud",area=("Circle",1),effects=[status("Enemies","Ap",-1,1)],cd=1),
 R("call-colossus","Colosse","Colossus",5,1,2,0,0,1,"Neutral","delapouite/rock-golem",los=False,effects=[summon("colossus")],cd=4),
 R("obelisk","Obélisque","Obelisk",6,2,5,1,1,1,"Earth","delapouite/obelisk",cd=1),
 R("firestorm","Tempête de feu","Firestorm",6,2,5,1,1,1,"Fire","lorc/fire-wave",area=("Circle",1),cd=1),
 R("deep-call","Appel des fonds","Call of the Deep",6,2,5,1,1,1,"Water","lorc/at-sea",area=("Cross",1),cd=1),
 R("vortex","Vortex","Vortex",6,2,6,1,1,1,"Air","lorc/vortex",cd=2),
 R("rouse","Exhortation","Rouse",3,0,5,0,0,1,"Neutral","lorc/shouting",los=False,area=("Circle",1),effects=[status("Allies","Damage",25,2)],cd=3),
 R("switch","Permutation","Switch",2,1,6,0,0,1,"Neutral","lorc/magic-portal",los=False,effects=[swap()],cd=2),
 R("veil","Voile","Veil",3,0,0,0,0,1,"Neutral","lorc/magic-shield",los=False,effects=[shield("Caster",30,2)],cd=4),
 R("calling-step","Pas d'appel","Calling Step",1,0,0,0,0,1,"Neutral","lorc/footprint",los=False,effects=[status("Caster","Mp",2,1)],cd=2),
)
# The Necromancer (Nécromancien) (sprint 214, docs/classes-necromancien.md §3): what falls feeds it; a corpse raises its dead, blows up or
# is eaten; one dead at a time (docs/classes-canon.md). Blows of 1 to 6 cells, poisons, at 0.85 of the budget. Working names.
necromancer = C(
 R("grave-dust","Poussière de tombe","Grave Dust",3,1,5,1,1,2,"Earth","lorc/broken-skull"),
 R("pale-flame","Flamme blême","Pale Flame",3,1,5,1,1,2,"Fire","lorc/spectre"),
 R("black-bile","Bile noire","Black Bile",3,1,5,1,1,2,"Water","lorc/vomiting",effects=[status("Enemies","Poison",4,3,"Water")]),
 R("death-rattle","Râle","Death Rattle",3,1,5,1,1,2,"Air","lorc/despair"),
 R("raise-dead","Lever un mort","Raise the Dead",3,1,4,0,0,1,"Neutral","delapouite/shambling-zombie",los=False,effects=[summon("risen")],cd=1,corpse=True),
 R("corpse-blast","Détonation","Detonation",3,1,6,1,1,1,"Neutral","lorc/internal-organ",los=False,area=("Circle",1),cd=1,corpse=True,adaptive=True),
 R("bone-shard","Écharde d'os","Bone Sliver",4,1,5,1,1,2,"Earth","lorc/bone-knife",effects=[status("Enemies","Mp",-1,1)]),
 R("fever","Fièvre","Fever",4,1,5,1,1,2,"Fire","lorc/dripping-goo",effects=[status("Enemies","Poison",4,3,"Fire")]),
 R("drowned-hand","Main du noyé","Drowned Hand",4,1,4,1,1,2,"Water","lorc/drowning",line=True,effects=[pull(1)]),
 R("wail","Lamentation","Wail",4,1,5,1,1,2,"Air","lorc/screaming",effects=[status("Enemies","Ap",-1,1)]),
 R("feast","Festin","Feast",2,1,3,0,0,1,"Neutral","lorc/carrion",los=False,effects=[heal("Caster",18,24)],cd=1,corpse=True),
 R("ossuary","Ossuaire","Ossuary",5,2,5,1,1,1,"Earth","lorc/crossed-bones",area=("Circle",1)),
 R("funeral-blaze","Brasier funèbre","Funeral Blaze",5,2,5,1,1,1,"Fire","lorc/coffin",area=("Cross",1)),
 R("black-tide","Marée noire","Black Tide",5,2,5,1,1,1,"Water","lorc/skull-in-jar",area=("Circle",1),effects=[status("Enemies","Poison",4,3,"Water")]),
 R("soul-wind","Vent des âmes","Soul Wind",5,2,6,1,1,1,"Air","lorc/ghost-ally",area=("Circle",1),effects=[status("Enemies","Ap",-1,1)],cd=1),
 R("bone-armor","Armure d'os","Bone Armour",2,1,4,0,0,1,"Neutral","lorc/skull-shield",los=False,effects=[shield("Caster",30,2)],cd=2,corpse=True),
 R("tombstone","Pierre tombale","Tombstone",6,2,5,1,1,1,"Earth","lorc/tombstone",cd=1),
 R("tomb-flames","Flammes du tombeau","Tomb Flames",6,2,5,1,1,1,"Fire","lorc/dread-skull",area=("Circle",1),cd=1),
 R("dead-waters","Eaux mortes","Dead Waters",6,2,5,1,1,1,"Water","lorc/dead-wood",area=("Cross",1),cd=1),
 R("last-breath","Dernier souffle","Last Breath",6,2,6,1,1,1,"Air","lorc/grim-reaper",cd=2),
 R("funeral-march","Marche funèbre","Funeral March",3,0,5,0,0,1,"Neutral","lorc/raven",los=False,area=("Circle",1),effects=[status("Allies","Mp",2,1),status("Allies","Damage",15,2)],cd=3),
 R("dread","Effroi","Dread",3,1,5,0,0,1,"Neutral","lorc/terror",effects=[status("Enemies","Ap",-2,1)],cd=2),
 R("grave-chill","Froid du tombeau","Grave Chill",3,1,5,0,0,1,"Neutral","lorc/frozen-orb",effects=[status("Enemies","Resistance",-15,2)],cd=2),
 R("grave-step","Pas funèbre","Grave Step",1,0,0,0,0,1,"Neutral","delapouite/bat",los=False,effects=[status("Caster","Mp",2,1)],cd=2),
)
# The Druid (Druide) (sprint 216, docs/classes-druide.md §2): it shapes a place for all who stand there; its shared glyphs strike or hinder the
# enemies and, on the same cells, help the allies. Blows of 1 to 4 cells at 1 of the budget (0.85 in the design, docs/classes-druide.md §4), 0.8 of that with a glyph. Working names.
druid = C(
 R("thorn","Épine","Thorn",3,1,4,1,1,2,"Earth","lorc/thorny-vine"),
 R("stump-ember","Tison","Firebrand",3,1,4,1,1,2,"Fire","lorc/burning-embers"),
 R("drizzle","Bruine","Drizzle",3,1,4,1,1,2,"Water","lorc/raining"),
 R("rustle","Bruissement","Rustle",3,1,4,1,1,2,"Air","lorc/falling-leaf"),
 R("bramble-ring","Cercle de ronces","Bramble Ring",3,1,4,0,0,1,"Neutral","lorc/vine-whip",los=False,area=("Circle",1),effects=shared(2,("Resistance",15,1),dict(stat="Mp",v=-1,st=1)),cd=2),
 R("grove","Bosquet","Grove",3,0,3,0,0,1,"Neutral","lorc/pine-tree",los=False,area=("Circle",1),effects=shared(2,("Regen",5,1),dict(stat="Damage",v=-10,st=1)),cd=2),
 R("bramble-lash","Fouet de ronces","Bramble Lash",4,1,4,1,1,2,"Earth","lorc/thorn-helix",effects=[status("Enemies","Mp",-1,1)]),
 R("brushfire","Feu de brousse","Brushfire",4,1,4,1,1,2,"Fire","lorc/burning-tree",effects=shared(2,("Damage",10,1),dict(dmin=1,dmax=1,el="Fire"))),
 R("spring","Source","Spring",4,1,4,1,1,2,"Water","delapouite/water-fountain",effects=shared(2,("Regen",5,1))),
 R("pollen","Pollen","Pollen",4,1,4,1,1,2,"Air","delapouite/flower-emblem",effects=[status("Enemies","Ap",-1,1)]),
 R("mending-sap","Sève","Mending Sap",3,0,4,0,0,1,"Neutral","lorc/sprout",los=False,effects=[heal("Allies",14,18)],cd=2),
 R("mound","Tertre","Mound",5,2,4,1,1,1,"Earth","lorc/mountaintop",area=("Circle",1)),
 R("slash-and-burn","Brûlis","Slash and Burn",5,2,4,1,1,1,"Fire","delapouite/burning-forest",area=("Cross",1),effects=shared(2,("Damage",10,1),dict(dmin=1,dmax=1,el="Fire"))),
 R("marsh","Marécage","Marsh",5,2,4,1,1,1,"Water","delapouite/swamp",area=("Circle",1),effects=shared(2,("Regen",5,1),dict(stat="Mp",v=-1,st=1))),
 R("canopy-wind","Vent des cimes","Canopy Wind",5,2,4,1,1,1,"Air","lorc/wind-hole",area=("Circle",1),effects=[push(1)],cd=1),
 R("fertile-ground","Terre nourricière","Fertile Ground",3,0,4,0,0,1,"Neutral","delapouite/tree-roots",los=False,area=("Circle",1),effects=shared(3,("Resistance",15,1),dict(stat="Resistance",v=-15,st=1)),cd=3),
 R("felled-oak","Chêne abattu","Felled Oak",6,2,4,1,1,1,"Earth","lorc/oak",cd=1),
 R("wildfire","Feu de forêt","Wildfire",6,2,4,1,1,1,"Fire","lorc/fire-zone",area=("Circle",1),effects=shared(2,("Damage",10,1),dict(dmin=1,dmax=1,el="Fire")),cd=1),
 R("thaw-flood","Débâcle","Thaw Flood",6,2,4,1,1,1,"Water","delapouite/wave-surfer",area=("Cross",1),cd=1),
 R("leaf-storm","Tourbillon de feuilles","Leaf Storm",6,2,4,1,1,1,"Air","lorc/beech",area=("Circle",1),effects=[status("Enemies","Ap",-1,1)],cd=2),
 R("tangle","Enchevêtrement","Tangle",3,1,4,0,0,1,"Neutral","lorc/root-tip",los=False,area=("Cross",1),effects=shared(2,("Mp",1,1),dict(stat="Mp",v=-2,st=1)),cd=2),
 R("forest-call","Appel de la forêt","Call of the Forest",3,0,4,0,0,1,"Neutral","delapouite/forest",los=False,area=("Circle",2),effects=[status("Allies","Damage",20,2)],cd=3),
 R("moss-step","Pas de mousse","Moss Step",1,0,0,0,0,1,"Neutral","lorc/footprint",los=False,effects=[status("Caster","Mp",2,1)],cd=2),
 R("sacred-grove","Cercle sacré","Sacred Grove",4,0,3,0,0,1,"Neutral","delapouite/circle-forest",los=False,area=("Circle",2),effects=shared(3,("Resistance",20,1),dict(stat="Ap",v=-1,st=1)),cd=4),
)
creatures = [
 # Adaptive for a Hawk its summoner's path feeds (sprint 191, brick E): the class's has no element to follow, so strikes in Air as before.
 S("peck","Coup de bec","Peck",3,1,1,5,8,2,"Air",1,ranks=False,adaptive=True),
 S("ember-bolt","Trait de braise","Ember Bolt",3,1,3,4,7,1,"Fire",1,ranks=False),
 # The Illusionist's Double (sprint 200, docs/classes-mage-2.md §2): in its summoner's strongest element, fed or not.
 S("mirror-strike","Frappe du reflet","Mirror Strike",3,1,2,6,9,2,"Neutral",1,ranks=False,adaptive=True),
 # The Summoner's creatures (sprint 211, docs/classes-invocateur.md §2): in its strongest element, fed by its class's rules.
 S("hound-fang","Croc","Fang",3,1,1,6,9,2,"Earth",1,ranks=False,adaptive=True),
 S("wisp-glow","Lueur","Glow",3,1,4,5,8,2,"Fire",1,ranks=False,adaptive=True),
 S("colossus-fist","Poing du colosse","Colossus Fist",4,1,1,10,14,1,"Earth",1,effects=[push(1)],ranks=False,adaptive=True),
 # The Familiarist's Familiar (sprint 212).
 S("familiar-claw","Griffe du familier","Familiar's Claw",3,1,1,7,10,2,"Earth",1,ranks=False,adaptive=True),
 # The Demonist's Demon and the Conjurer's Pillar (sprint 213, docs/classes-invocateur-3.md §1-2): the one up close, the other where it was laid.
 S("demon-rend","Lacération","Rend",3,1,1,8,12,2,"Fire",1,ranks=False,adaptive=True),
 S("pillar-shard","Éclat du pilier","Pillar Shard",3,1,4,5,8,2,"Earth",1,ranks=False,adaptive=True),
 # The Necromancer's dead (sprint 214, docs/classes-necromancien.md §2): up close, in its raiser's strongest element.
 S("risen-claw","Griffe du mort","Dead Claw",3,1,1,7,10,2,"Earth",1,ranks=False,adaptive=True),
 # The Macabre Caller's ghouls (sprint 215, docs/classes-necromancien-2.md §1): many, frail, thrown away.
 S("ghoul-bite","Morsure de goule","Ghoul Bite",3,1,1,5,8,2,"Earth",1,ranks=False,adaptive=True),
 # The Shaman's spirits (sprint 218, docs/classes-chaman.md §1): from 1 to 3 cells, in their caller's strongest element.
 S("spirit-touch","Toucher de l'esprit","Spirit Touch",3,1,3,5,8,2,"Earth",1,ranks=False,adaptive=True),
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
AREA = {("Cross", 1): 0.8, ("Line", 2): 0.8, ("Line", 3): 0.7, ("Circle", 1): 0.7, ("Circle", 2): 0.6, ("Cross", 2): 0.6}
def budget(sp, scale=1):
    f = BUDGET * (1 + GROWTH * sp.get("level", 1) / 100) * scale
    if "area" in sp: f *= AREA[(sp["area"]["shape"], sp["area"]["radius"])]
    kinds = [(e["kind"], e.get("stat")) for e in sp.get("effects", [])]
    if any(k in ("push", "pull") for k, _ in kinds): f *= 0.85
    if any(st in ("Mp", "Ap") for _, st in kinds): f *= 0.8
    if any(st == "Poison" for _, st in kinds): f *= 0.6
    if sp["maxRange"] >= 6: f *= 0.9
    if sp["maxRange"] == 1: f *= 1.1
    f *= 1 + 0.25 * sp.get("cooldown", 0)
    # Blood (sprint 206): a hit point paid is worth a tenth of an action point; a form asked, 15 %; life drawn costs 15 %.
    f *= 1 + sp.get("hpCost", 0) / (10 * sp["apCost"])
    if sp.get("form"): f *= 1.15
    if any(k == "drain" for k, _ in kinds): f *= 0.85
    # A corpse taken (sprint 214, the Necromancer's): a resource the fight gives, worth half the blow more.
    if sp.get("corpse"): f *= 1.5
    # A glyph laid with the blow (sprint 216, the Druid's shared ground): it pays a fifth of the blow.
    if any(k == "glyph" for k, _ in kinds): f *= 0.8
    avg = f * sp["apCost"]
    sp["damageMin"], sp["damageMax"] = max(1, round(avg * 0.85)), max(1, round(avg * 1.15))
    sp["ranks"] = [O([("damageMin", round(sp["damageMin"] * g)), ("damageMax", round(sp["damageMax"] * g))]) for g in (1.2, 1.4, 1.6, 1.8)]
# Every damaging class spell gets its numbers from the budget rule (the placeholders 1 to 1 above).
for sp in sentinel + guard + mage:
    if sp["damageMax"] > 0:
        budget(sp)
# The Priest strikes at 0.8 of the budget (docs/classes-conception.md §1.2): its worth is in its heals.
PRIEST_BUDGET = 0.8
for sp in priest:
    if sp["damageMax"] > 0:
        budget(sp, PRIEST_BUDGET)
# The Rogue (Voleur) at 1.15 (docs/classes-voleur.md §6): one target, close by, with little to protect it. 1.05 in the design (§1.2);
# at 1.1 it beat the Disjoint alone 17 times in 100 (the CI asks 30).
ROGUE_BUDGET = 1.15
for sp in rogue:
    if sp["damageMax"] > 0:
        budget(sp, ROGUE_BUDGET)
# The Berserker at 1.1 (docs/classes-conception.md §1.2): raw damage, paid in hit points.
BERSERKER_BUDGET = 1.1
for sp in berserker:
    if sp["damageMax"] > 0:
        budget(sp, BERSERKER_BUDGET)
# The Paladin at 0.85 (docs/classes-conception.md §1.2): its worth is in what it shields.
PALADIN_BUDGET = 0.85
for sp in paladin:
    if sp["damageMax"] > 0:
        budget(sp, PALADIN_BUDGET)
# The Summoner at 0.75 (docs/classes-conception.md §1.2): its worth is in its creatures.
SUMMONER_BUDGET = 0.75
for sp in summoner:
    if sp["damageMax"] > 0:
        budget(sp, SUMMONER_BUDGET)
# The Necromancer at 0.85 (docs/classes-conception.md §1.2): its worth is in what falls.
NECROMANCER_BUDGET = 0.85
for sp in necromancer:
    if sp["damageMax"] > 0:
        budget(sp, NECROMANCER_BUDGET)
# The Druid at 1, measured (docs/classes-druide.md §4; 0.85 in the design): alone at 0.85 it won 18 % of the bench. Its worth is in the ground it shares. A glyph that burns the enemies does a third of the blow.
DRUID_BUDGET = 1.0
def burns(sp):
    for e in sp.get("effects", []):
        if e["kind"] == "glyph" and e["affects"] == "Enemies" and e.get("damageMax"):
            e["damageMin"], e["damageMax"] = max(1, sp["damageMin"] // 3), max(1, sp["damageMax"] // 3)
for sp in druid:
    if sp["damageMax"] > 0:
        budget(sp, DRUID_BUDGET)
        burns(sp)
# One exception (sprint 162, D85, T129): Rockfall at 6 AP a step above the budget, 13-17, so that the Sentinel of 41 keeps
# its 7 in 10 in the Combe's great hall; +1 helps the mixed hero (+12 of bonus) more than the one of one element (+34).
for sp in sentinel:
    if sp["id"] == "rockfall":
        sp["damageMin"], sp["damageMax"] = sp["damageMin"] + 1, sp["damageMax"] + 1
        sp["ranks"] = [O([("damageMin", round(sp["damageMin"] * g)), ("damageMax", round(sp["damageMax"] * g))]) for g in (1.2, 1.4, 1.6, 1.8)]
# The compounds (sprint 138, D80, docs/elements.md): a pair's spells are its parents' class spells transposed, "twin"
# naming the spell they come from: the same cost, range, area, effects and level, the compound's element (a poison too),
# damage from the budget at 80 % for a pair (its signature is paid for). Working names, for the scriptwriter to read.
COMPOUNDS = {"Lightning": ("Fire", "Air"), "Ice": ("Water", "Air"), "Mud": ("Earth", "Water"), "Magma": ("Earth", "Fire"), "Steam": ("Fire", "Water"), "Sand": ("Earth", "Air"),
 "Ether": ("Fire", "Water", "Air"), "Ash": ("Earth", "Fire", "Air"), "Sap": ("Earth", "Fire", "Water"), "Salt": ("Earth", "Water", "Air")}
# A pair at 80 % of the budget, a trio at 75 % (sprint 185); a trio takes all its parents' spells, 12 a class (sprint 188, D94), for a book that stays at 24.
SCALE = {el: 0.8 if len(p) == 2 else 0.75 for el, p in COMPOUNDS.items()}
# The five pairs after Lightning (sprint 184, D93, docs/elements-specialisations.md §v3.2): what a hit leaves on the enemies it strikes.
SIGNATURES = {"Ice": ("Ap", -1), "Mud": ("Mp", -1), "Magma": ("Resistance", -8), "Steam": ("Damage", -10), "Sand": ("Range", -1)}
MATTER = {"Ice": ("de glace", "Ice"), "Mud": ("de boue", "Mud"), "Magma": ("de magma", "Magma"), "Steam": ("de vapeur", "Steam"), "Sand": ("de sable", "Sand"),
 "Ether": ("d'éther", "Ether"), "Ash": ("de cendre", "Ash"), "Sap": ("de sève", "Sap"), "Salt": ("de sel", "Salt")}
# Each spell's gesture, to which a pair gives its matter: "Carreau de glace", "Ice Bolt". Working names, for the scriptwriter to read in batches.
GESTURES = {
 "stone-arrow": ("Carreau", "Bolt"), "fire-arrow": ("Flèche", "Arrow"), "frost-arrow": ("Dard", "Dart"), "recoil-shot": ("Recul", "Recoil"),
 "heavy-shot": ("Boulet", "Ball"), "ember-volley": ("Salve", "Volley"), "tide-arrow": ("Flot", "Flow"), "piercing-arrow": ("Percée", "Pierce"),
 "rockfall": ("Éboulis", "Rockfall"), "searing-shot": ("Tir", "Shot"), "venom-arrow": ("Venin", "Venom"), "gale": ("Souffle", "Breath"),
 "stake": ("Pieu", "Stake"), "ember-rain": ("Pluie", "Rain"), "deluge": ("Déluge", "Deluge"), "skyfall": ("Chute", "Fall"),
 "shield-bash": ("Heurt", "Bash"), "burning-blade": ("Lame", "Blade"), "ice-blade": ("Tranchant", "Edge"), "slash": ("Taillade", "Slash"),
 "cleave": ("Fauchage", "Cleave"), "fire-lunge": ("Fente", "Lunge"), "undertow": ("Ressac", "Undertow"), "wind-blade": ("Sabre", "Sabre"),
 "quake": ("Secousse", "Tremor"), "smite": ("Châtiment", "Smite"), "groundswell": ("Houle", "Swell"), "cyclone-blade": ("Tourbillon", "Whirl"),
 "titan-blow": ("Frappe", "Strike"), "inferno-blade": ("Grande lame", "Greatblade"), "torrent": ("Torrent", "Torrent"), "hurricane": ("Ouragan", "Hurricane"),
 "pebble": ("Caillou", "Pebble"), "flare": ("Étincelle", "Spark"), "ice-shard": ("Éclat", "Shard"), "gust": ("Bourrasque", "Gust"),
 "roots": ("Entrave", "Bind"), "burn": ("Brûlure", "Burn"), "tidal-wave": ("Vague", "Wave"), "bolt": ("Trait", "Shaft"),
 "landslide": ("Glissement", "Slide"), "meteor": ("Météore", "Meteor"), "freeze": ("Prise", "Grip"), "cyclone": ("Cyclone", "Cyclone"),
 "fault-line": ("Faille", "Rift"), "cataclysm": ("Cataclysme", "Cataclysm"), "glacier": ("Bloc", "Block"), "tempest": ("Tempête", "Tempest"),
 # The Priest's (sprint 201).
 "blessed-stone": ("Pierre", "Stone"), "holy-flame": ("Flamme", "Flame"), "holy-water": ("Onction", "Unction"), "sacred-breath": ("Haleine", "Exhale"),
 "stone-verdict": ("Verdict", "Verdict"), "censer": ("Encens", "Incense"), "blessed-tide": ("Marée", "Tide"), "zephyr": ("Zéphyr", "Zephyr"),
 "stone-column": ("Colonne", "Column"), "sacred-pyre": ("Bûcher", "Pyre"), "baptism": ("Baptême", "Baptism"), "halo": ("Auréole", "Halo"),
 "judgement": ("Jugement", "Judgement"), "heavenly-blaze": ("Brasier", "Blaze"), "holy-flood": ("Crue", "Flood"), "wrath-of-heaven": ("Courroux", "Wrath"),
 # The Rogue (Voleur)'s (sprint 204).
 "pommel-strike": ("Pommeau", "Pommel"), "ember-dagger": ("Dague", "Dagger"), "gash": ("Entaille", "Gash"), "thrown-knife": ("Couteau", "Knife"),
 "trip": ("Croche-pied", "Trip"), "fire-vial": ("Fiole", "Vial"), "bleed": ("Saignée", "Bleed"), "pounce": ("Bond", "Pounce"),
 "cosh": ("Gourdin", "Cudgel"), "red-blade": ("Estoc", "Thrust"), "slow-venom": ("Toxine", "Toxin"), "volte": ("Volte", "Volte"),
 "kneecap": ("Brise-genou", "Kneecap"), "greek-fire": ("Brûlot", "Firepot"), "drown": ("Noyade", "Drowning"), "blade-storm": ("Rafale", "Flurry"),
 # The Berserker's (sprint 206).
 "cleaver": ("Fendoir", "Cleaver"), "blood-axe": ("Hache", "Axe"), "tide-blow": ("Reflux", "Ebb"), "whirl": ("Moulinet", "Windmill"),
 "bone-breaker": ("Brise-os", "Bonebreak"), "blood-fury": ("Furie", "Fury"), "breaker": ("Déferlante", "Breaker"), "war-whirl": ("Tournoiement", "Spin"),
 "rend": ("Déchirure", "Rend"), "ember-cleave": ("Taille", "Hew"), "blood-tide": ("Marée rouge", "Red Tide"), "storm-axe": ("Hache volante", "Flying Axe"),
 "earthshaker": ("Ébranlement", "Quake"), "inferno-axe": ("Hachereau", "Hatchet"), "maelstrom": ("Maelström", "Maelstrom"), "tempest-blow": ("Coup", "Blow"),
 # The Paladin's (sprint 208).
 "holy-hammer": ("Marteau", "Hammer"), "radiant-blade": ("Lame claire", "Bright Blade"), "purifying-strike": ("Purification", "Cleansing"), "light-lance": ("Lance", "Lance"),
 "rampart-strike": ("Rempart", "Rampart"), "sun-strike": ("Soleil", "Sunstrike"), "tide-of-faith": ("Onde", "Ripple"), "judgement-lance": ("Pique", "Pike"),
 "crushing-faith": ("Foi", "Faith"), "dawn-blade": ("Aube", "Dawn"), "mercy-strike": ("Miséricorde", "Mercy"), "heavens-spear": ("Épieu", "Boar Spear"),
 "judgement-hammer": ("Maillet", "Mallet"), "blazing-oath": ("Serment", "Oath Blow"), "grace-flood": ("Grâce", "Grace"), "zenith": ("Zénith", "Zenith"),
 # The Summoner's (sprint 211).
 "clod": ("Motte", "Clod"), "cinder": ("Escarbille", "Cinder"), "droplet": ("Goutte", "Droplet"), "murmur": ("Murmure", "Murmur"),
 "binding-stone": ("Lien", "Bond"), "searing-brand": ("Marque", "Brand"), "mire": ("Enlisement", "Sinking"), "breath": ("Exhalaison", "Exhalation"),
 "rumble": ("Grondement", "Rumble"), "pyre": ("Fournaise", "Furnace"), "far-undertow": ("Reflux lointain", "Far Ebb"), "squall": ("Grain", "Squall"),
 "obelisk": ("Obélisque", "Obelisk"), "firestorm": ("Embrasement", "Blaze Storm"), "deep-call": ("Abîme", "Abyss"), "vortex": ("Vortex", "Vortex"),
 # The Necromancer's (sprint 214).
 "grave-dust": ("Poudre des tombes", "Grave Powder"), "pale-flame": ("Blêmissure", "Pallor"), "black-bile": ("Bile", "Bile"), "death-rattle": ("Râle", "Rattle"),
 "bone-shard": ("Écharde", "Sliver"), "fever": ("Fièvre", "Fever"), "drowned-hand": ("Main noyée", "Drowned Grip"), "wail": ("Plainte", "Lament"),
 "ossuary": ("Ossuaire", "Ossuary"), "funeral-blaze": ("Bûcher funèbre", "Funeral Pyre"), "black-tide": ("Reflux noir", "Black Ebb"), "soul-wind": ("Souffle des âmes", "Soul Breath"),
 "tombstone": ("Stèle", "Stele"), "tomb-flames": ("Feu tombal", "Tomb Fire"), "dead-waters": ("Eaux mortes", "Dead Waters"), "last-breath": ("Dernier soupir", "Last Sigh"),
 # The Druid's (sprint 216).
 "thorn": ("Aiguille", "Needle"), "stump-ember": ("Tisonnier", "Poker"), "drizzle": ("Ondée", "Shower"), "rustle": ("Frisson", "Shiver"),
 "bramble-lash": ("Fouet", "Lash"), "brushfire": ("Brousse", "Bush"), "spring": ("Résurgence", "Resurgence"), "pollen": ("Poussière", "Dust"),
 "mound": ("Butte", "Knoll"), "slash-and-burn": ("Écobuage", "Burn-off"), "marsh": ("Fondrière", "Quagmire"), "canopy-wind": ("Ramée", "Boughs"),
 "felled-oak": ("Tronc", "Trunk"), "wildfire": ("Incendie", "Wildfire"), "thaw-flood": ("Dégel", "Thaw"), "leaf-storm": ("Feuillée", "Foliage"),
}
# Where a gesture and a matter would give the name of a spell of the game.
NAME_CLASHES = {("Ice", "ice-shard"): ("Esquille de glace", "Ice Splinter"), ("Ice", "burning-blade"): ("Lame de givre", "Rime Blade")}
NAMES = {
 "Lightning": {
  # The Druid's (sprint 216).
  "stump-ember": ("Tison d'orage", "Storm Firebrand"), "rustle": ("Bruissement d'orage", "Storm Rustle"), "brushfire": ("Brousse foudroyée", "Thunderstruck Bush"),
  "pollen": ("Pollen électrique", "Electric Pollen"), "slash-and-burn": ("Brûlis d'orage", "Storm Burn-off"), "canopy-wind": ("Cimes d'orage", "Storm Canopy"),
  "wildfire": ("Feu du ciel", "Sky Fire"), "leaf-storm": ("Feuilles d'orage", "Storm Leaves"),
  # The Necromancer's (sprint 214).
  "pale-flame": ("Blême d'orage", "Storm Pallor"), "death-rattle": ("Râle d'orage", "Storm Rattle"), "fever": ("Fièvre électrique", "Electric Fever"),
  "wail": ("Plainte d'orage", "Storm Lament"), "funeral-blaze": ("Brasier foudroyant", "Thunder Blaze"), "soul-wind": ("Vent foudroyé", "Thunderstruck Wind"),
  "tomb-flames": ("Foudre tombale", "Tomb Lightning"), "last-breath": ("Dernier éclair", "Last Bolt"),
  # The Summoner's (sprint 211).
  "cinder": ("Grésil d'orage", "Storm Sleet"), "murmur": ("Grondement sourd", "Low Thunder"), "searing-brand": ("Marque d'orage", "Storm Brand"),
  "breath": ("Bise d'orage", "Thunder Breeze"), "pyre": ("Bûcher d'éclairs", "Lightning Pyre"), "squall": ("Grain d'orage", "Thundersquall"),
  "firestorm": ("Tempête d'éclairs", "Lightning Storm"), "vortex": ("Vortex d'orage", "Storm Vortex"),
  "fire-arrow": ("Flèche d'orage", "Storm Arrow"), "ember-volley": ("Salve d'éclairs", "Lightning Volley"),
  "searing-shot": ("Tir fulgurant", "Flash Shot"), "ember-rain": ("Pluie d'éclairs", "Rain of Bolts"),
  "recoil-shot": ("Tir tonnant", "Thunder Shot"), "piercing-arrow": ("Flèche filante", "Streaking Arrow"),
  "gale": ("Grain orageux", "Storm Squall"), "skyfall": ("Chute de foudre", "Lightning Fall"),
  # The guard's and the mage's (sprint 139).
  "burning-blade": ("Lame d'orage", "Storm Blade"), "slash": ("Taillade fulgurante", "Flash Slash"),
  "fire-lunge": ("Fente foudroyante", "Thunder Lunge"), "wind-blade": ("Lame crépitante", "Crackling Blade"),
  "smite": ("Coup de tonnerre", "Thunderclap"), "cyclone-blade": ("Tourbillon d'orage", "Storm Whirl"),
  "inferno-blade": ("Lame de foudre", "Lightning Blade"), "hurricane": ("Fureur d'orage", "Storm Fury"),
  "flare": ("Crépitement", "Crackle"), "gust": ("Souffle d'orage", "Storm Breath"),
  "burn": ("Électrocution", "Electrocution"), "bolt": ("Éclair", "Lightning Bolt"),
  "meteor": ("Foudre céleste", "Heaven's Lightning"), "cyclone": ("Œil de l'orage", "Eye of the Storm"),
  "cataclysm": ("Fulgurance", "Fulguration"), "tempest": ("Orage", "Thunderstorm"),
  # The Priest's (sprint 201).
  "holy-flame": ("Flamme d'orage", "Storm Flame"), "sacred-breath": ("Haleine d'orage", "Storm Exhale"),
  "censer": ("Encens crépitant", "Crackling Incense"), "zephyr": ("Zéphyr électrique", "Electric Zephyr"),
  "sacred-pyre": ("Bûcher foudroyé", "Thunderstruck Pyre"), "halo": ("Auréole d'éclairs", "Lightning Halo"),
  "heavenly-blaze": ("Foudre divine", "Divine Lightning"), "wrath-of-heaven": ("Courroux de l'orage", "Wrath of the Storm"),
  # The Rogue (Voleur)'s (sprint 204).
  "ember-dagger": ("Dague d'orage", "Storm Dagger"), "thrown-knife": ("Couteau foudroyant", "Thunder Knife"),
  "fire-vial": ("Fiole d'éclairs", "Lightning Vial"), "pounce": ("Bond fulgurant", "Flash Pounce"),
  "red-blade": ("Lame électrique", "Electric Blade"), "volte": ("Volte d'orage", "Storm Volte"),
  "greek-fire": ("Feu de foudre", "Lightning Fire"), "blade-storm": ("Rafale d'éclairs", "Lightning Flurry"),
  # The Berserker's (sprint 206).
  "blood-axe": ("Hache d'orage", "Storm Axe Blow"), "blood-fury": ("Furie électrique", "Electric Fury"),
  "ember-cleave": ("Fendoir foudroyant", "Thunder Cleave"), "inferno-axe": ("Hache de foudre", "Lightning Axe"),
  "whirl": ("Moulinet d'orage", "Storm Whirl Blow"), "war-whirl": ("Tourbillon fulgurant", "Flash Vortex"),
  "storm-axe": ("Hache crépitante", "Crackling Axe"), "tempest-blow": ("Coup de foudre", "Lightning Strike"),
  # The Paladin's (sprint 208).
  "radiant-blade": ("Lame d'éclairs", "Storm Edge"), "sun-strike": ("Soleil d'orage", "Storm Sun"), "dawn-blade": ("Aube électrique", "Electric Dawn"),
  "blazing-oath": ("Serment foudroyant", "Thunder Oath"), "light-lance": ("Lance d'orage", "Storm Lance"), "judgement-lance": ("Pique foudroyante", "Thunder Pike"),
  "heavens-spear": ("Épieu de foudre", "Lightning Spear"), "zenith": ("Zénith d'orage", "Storm Zenith"),
 },
}
# A twin's own silhouette, the gesture of its spell in its compound's matter (sprint 163, docs/graphisme/s163-icones-arbre.md §1.8).
TWIN_ICONS = {
 "lightning-fire-arrow": "lorc/charged-arrow", "lightning-recoil-shot": "lorc/lightning-bow", "lightning-ember-volley": "lorc/lightning-trio",
 "lightning-piercing-arrow": "lorc/sonic-lightning", "lightning-searing-shot": "lorc/thunderball", "lightning-gale": "lorc/heavy-lightning",
 "lightning-ember-rain": "lorc/lightning-branches", "lightning-skyfall": "lorc/thunder-struck", "lightning-burning-blade": "lorc/thunder-blade",
 "lightning-slash": "lorc/lightning-slashes", "lightning-fire-lunge": "lorc/deadly-strike", "lightning-wind-blade": "lorc/lightning-saber",
 "lightning-smite": "lorc/thor-fist", "lightning-cyclone-blade": "lorc/lightning-helix", "lightning-inferno-blade": "lorc/zeus-sword",
 "lightning-hurricane": "lorc/lightning-dissipation", "lightning-flare": "lorc/static", "lightning-gust": "lorc/static-waves",
 "lightning-burn": "lorc/lightning-shadow", "lightning-bolt": "lorc/focused-lightning", "lightning-meteor": "lorc/power-lightning",
 "lightning-cyclone": "lorc/bolt-eye", "lightning-cataclysm": "delapouite/bolt-bomb", "lightning-tempest": "willdabeast/chain-lightning",
}
twins = []
for el, parents in COMPOUNDS.items():
    for sp in sentinel + guard + mage + priest + rogue + berserker + paladin + summoner + necromancer + druid:
        if sp["element"] in parents and (el == "Lightning" and sp["id"] in NAMES[el] or el in MATTER):
            t = json.loads(json.dumps(sp), object_pairs_hook=O)
            named = NAMES[el][sp["id"]] if el == "Lightning" else NAME_CLASHES.get((el, sp["id"])) or (f"{GESTURES[sp['id']][0]} {MATTER[el][0]}", f"{MATTER[el][1]} {GESTURES[sp['id']][1]}")
            t["id"], t["name"] = f"{el.lower()}-{sp['id']}", O([("fr", named[0]), ("en", named[1])])
            t["element"] = el
            for e in t.get("effects", []):
                if e.get("stat") == "Poison":
                    e["element"] = el
                # The Druid's burning ground (sprint 216) burns in the compound's element.
                if e["kind"] == "glyph" and e.get("element"):
                    e["element"] = el
            budget(t, SCALE[el] * (PRIEST_BUDGET if sp in priest else ROGUE_BUDGET if sp in rogue else BERSERKER_BUDGET if sp in berserker else PALADIN_BUDGET if sp in paladin else SUMMONER_BUDGET if sp in summoner else NECROMANCER_BUDGET if sp in necromancer else DRUID_BUDGET if sp in druid else 1))
            burns(t)
            # The signature is paid for by the 80 %, after the budget; a spell that takes this already keeps its own, the
            # stronger (one caster's statuses of a kind do not add up).
            if el in SIGNATURES:
                stat, value = SIGNATURES[el]
                if not any(e.get("stat") == stat and e.get("affects") == "Enemies" for e in t.get("effects", [])):
                    t.setdefault("effects", []).append(status("Enemies", stat, value, 1))
            # The trios' signatures (sprint 185): Sap's Bud is in the fight, the others in the spell.
            if el == "Ether":
                t["lineOfSight"] = False
            # Ash's glyph; a spell that already lays one for the enemies (the Druid's) has it burn in Ash, one glyph a side.
            if el == "Ash" and any(e["kind"] == "glyph" and e["affects"] == "Enemies" for e in t.get("effects", [])):
                for e in t["effects"]:
                    if e["kind"] == "glyph" and e["affects"] == "Enemies":
                        e["damageMin"], e["damageMax"], e["element"] = max(1, t["damageMin"] // 3), max(1, t["damageMax"] // 3), "Ash"
            elif el == "Ash":
                t.setdefault("effects", []).append(glyph("Enemies", 2, max(1, t["damageMin"] // 3), max(1, t["damageMax"] // 3), el="Ash"))
            if el == "Salt":
                t.setdefault("effects", []).append(status("Enemies", "Healing", -50, 2))
            # Lightning has its own silhouettes; the five pairs keep their origin's, in the colours of their tile, for now.
            t["icon"] = TWIN_ICONS.get(t["id"], sp["icon"])
            t["twin"] = sp["id"]
            twins.append(t)
# The spells of the tree (sprint 142, the branch Terrain, docs/talents-arbre.md §4.3): given by a talent, of no class.
EL_FR = {"Earth": "Terre", "Fire": "Feu", "Water": "Eau", "Air": "Air"}
tree = [
 S("relay","Relais","Relay",1,1,2,0,0,1,"Neutral",1,los=False,effects=[swap(),status("Caster","Range",-1,1)],ranks=False),
 *[S(f"seal-{el.lower()}",f"Sceau de {EL_FR[el]}" if el != "Air" else "Sceau d'Air",f"{el} Seal",3,1,5,0,0,1,"Neutral",1,effects=[glyph("Enemies",2,el=el,marks=True)],ranks=False,cd=3)
   for el in ("Earth","Fire","Water","Air")],
 S("decoy","Leurre","Decoy",2,1,3,0,0,1,"Neutral",1,los=False,effects=[summon("decoy")],ranks=False,cd=4),
]
# Their icons (sprint 163, §1.9): the Seals share the rune stone, their element as an accent drawn by tools/spell_art.py.
for sp in tree:
    sp["icon"] = {"relay": "lorc/back-forth", "decoy": "lorc/scarecrow"}.get(sp["id"], "lorc/rune-stone")
twins += tree
# The paths' spells (sprint 191, D95, docs/elements-specialisations.md §v4.4, names of docs/textes-specialisations.md §2): of no class, at the
# budget of their level, 2 at 30 and 1 at 50 a leaf. The Sentinel's first; the Guard's in sprint 192, the Mage's in 193.
def path(id, fr, en, ap, mn, mx, level, icon, damage=False, **kw):
    sp = S(f"path-{id}", fr, en, ap, mn, mx, 1 if damage else 0, 1 if damage else 0, kw.pop("per", 1), "Neutral", level, adaptive=True, **kw)
    sp["icon"] = icon
    return sp
paths = [
 # The Sightline.
 path("aimed-shot","Tir visé","Aimed Shot",5,4,9,30,"lorc/target-arrows",damage=True,cd=2),
 path("weak-spot","Point faible","Weak Spot",2,1,9,30,"delapouite/convergence-target",effects=[status("Enemies","Resistance",-15,2)],cd=3),
 path("bullseye","Coup au but","Bullseye",6,5,10,50,"lorc/high-shot",damage=True,cd=3),
 # The Bounty Hunter's (sprint 198, docs/classes-archer.md §2): the quarry carries two states of its own, Weak Spot's resistance a third.
 path("prey","Désigner la proie","Mark the Quarry",2,1,8,50,"delapouite/human-target",effects=[status("Enemies","Mp",-1,3),status("Enemies","Damage",-10,3)],cd=2),
 # The Ambush: its glyph at the budget by hand, the budget does not read a glyph's status (3 AP at level 30, cooldown 2, a loss of points).
 path("jaws","Mâchoires","Jaw Trap",3,1,5,30,"lorc/mantrap",effects=[glyph("Enemies",2,11,15,stat="Mp",v=-3,st=1)],cd=2),
 path("dive","Piqué","Dive",3,1,7,30,"lorc/feather",damage=True,los=False,cd=1,needs="hawk"),
 path("changeover","Chassé-croisé","Changeover",2,1,6,50,"lorc/magic-portal",los=False,effects=[swap()],cd=2,aims="hawk"),
 path("bait","Appât","Bait",2,1,4,50,"delapouite/bread",effects=[glyph("Enemies",2,stat="Ap",v=-1,st=1)],cd=3),
 # The Artillerist (sprint 198, docs/classes-archer.md §3.3): Barrage comes down from 50 to 30, its budget redone; the Crossbowman's heavy bolt.
 path("barrage","Tir de barrage","Barrage",4,3,8,30,"delapouite/stone-wall",damage=True,area=("Line",2),effects=[status("Enemies","Mp",-1,1)],cd=2),
 path("heavy-bolt","Carreau lourd","Heavy Bolt",5,3,8,50,"lorc/arrowhead",damage=True,cd=1),
 # The Trail's of before: Vault and Feint have no path since sprint 198, kept for the Rogue's Malandrin (Voleur, sprint 205).
 path("vault","Cabriole","Vault",2,2,4,30,"delapouite/quick-man",effects=[leap()],cd=1),
 path("heel-kick","Coup de talon","Heel Kick",3,1,1,30,"lorc/boot-stomp",damage=True,effects=[push(2)]),
 path("feint","Feinte","Feint",2,0,0,50,"lorc/dodging",los=False,effects=[status("Caster","Feint",2,2)],cd=3),
 path("running-shot","Tir en course","Running Shot",3,2,5,50,"lorc/run",damage=True,effects=[status("Caster","Mp",2,1)]),
 # The Palisade (sprint 192, D95): Stand Fast lasts through the enemies' turns, hence 2 of its own, as the Feint.
 path("interpose","Interposition","Interpose",2,1,3,30,"lorc/back-forth",los=False,effects=[swap(),shield("Caster",15,2)],cd=2),
 path("stand-fast","Tenir bon","Stand Fast",2,0,0,30,"lorc/checked-shield",los=False,effects=[status("Caster","Anchored",1,2),status("Caster","Resistance",15,2)],cd=2),
 path("shield-wall","Mur de boucliers","Shield Wall",4,0,0,50,"lorc/bordered-shield",los=False,area=("Cross",1),effects=[shield("Allies",30,2)],cd=3),
 path("retaliation","Rétorsion","Retaliation",3,1,1,50,"lorc/punch",damage=True,cd=1,spends=19),
 # The Breach.
 path("assault","Assaut","Assault",3,2,4,30,"lorc/quick-slash",damage=True,line=True,area=("Cross",1),effects=[leap()],cd=2),
 path("wrench","Arrachement","Wrench",3,1,4,30,"delapouite/push",effects=[pull(2),status("Enemies","Mp",-1,1)]),
 path("breakthrough","Trouée","Breakthrough",4,1,1,50,"lorc/anvil-impact",damage=True,area=("Line",2),effects=[push(2)],cd=1),
 path("harpoon","Harpon","Harpoon",3,2,6,50,"lorc/barbed-spear",damage=True,line=True,effects=[pull(3)],cd=1),
 # The Grip: the Lock takes all the movement any monster has.
 path("hold","Prise","Hold",3,1,1,30,"lorc/fist",damage=True,effects=[status("Enemies","Mp",-2,2)]),
 path("lock","Verrou","Lock",2,1,1,30,"lorc/skeleton-key",effects=[status("Enemies","Mp",-9,1)],cd=3),
 path("crush","Écrasement","Crush",5,1,1,50,"lorc/hammer-drop",damage=True),
 path("iron-ring","Cercle de fer","Iron Ring",3,0,0,50,"lorc/linked-rings",los=False,area=("Circle",1),effects=[status("Enemies","Mp",-2,1)],cd=2),
 # The Mage's (sprint 199, docs/classes-mage.md §2, designed in docs/textes-specialisations.md): the Arcanist prepares, its rune marks the ground.
 path("comet","Comète","Comet",7,3,7,30,"lorc/burning-meteor",damage=True,area=("Circle",1),cd=3),
 path("composure","Recueillement","Composure",2,0,0,30,"lorc/candle-flame",los=False,effects=[status("Caster","Damage",25,1)],cd=3),
 path("rune","Rune","Rune",3,1,6,50,"lorc/rune-stone",effects=[glyph("Enemies",2,10,14,marks=True)],cd=2),
 path("ray","Rayon","Ray",5,1,5,50,"lorc/fire-ray",damage=True,los=False,line=True,area=("Line",3),cd=2),
 # The Chronomancer plays with turns: points borrowed, an enemy put back, an ally hurried, the big spell they make room for.
 path("borrowed-time","Temps emprunté","Borrowed Time",1,0,0,30,"lorc/sands-of-time",los=False,effects=[borrow(3)],cd=3),
 path("offbeat","Contretemps","Offbeat",3,1,6,30,"lorc/stopwatch",damage=True,effects=[delay("Enemies",1)],cd=1),
 path("quicken","Accélération","Quicken",2,1,5,50,"lorc/hourglass",los=False,effects=[haste("Allies",1),delay("Allies",-1)],cd=3),
 path("crescendo","Point d'orgue","Crescendo",10,3,7,50,"lorc/echo-ripples",damage=True,los=False,area=("Circle",2),cd=2),
 # The Illusionist (sprint 200, docs/classes-mage-2.md §2): an image that draws the blows, a dazzle; the Double that strikes; the mind's grip.
 path("image","Image","Image",2,1,4,30,"lorc/two-shadows",los=False,effects=[summon("mirror-image")],cd=3),
 path("dazzle","Éblouissement","Dazzle",3,1,6,30,"lorc/sun",damage=True,effects=[status("Enemies","Range",-2,1)],cd=1),
 path("double","Double","Double",3,1,4,50,"lorc/ghost",los=False,effects=[summon("mirror-double")],cd=3),
 path("grip","Emprise","Mind Grip",3,1,6,50,"lorc/psychic-waves",damage=True,effects=[status("Enemies","Ap",-2,1),status("Enemies","Damage",-15,2)],cd=2),
 # The Priest (sprint 202, docs/classes-pretre-2.md §2), never a blow from next to its target. The Cleric raises and heals.
 path("resurrect","Résurrection","Resurrection",3,1,4,30,"lorc/sunrise",los=False,effects=[revive("Allies",30)]),
 path("laying-hands","Imposition des mains","Laying On of Hands",2,0,2,30,"lorc/hand",effects=[heal("Allies",12,16,overflow=True)],cd=1),
 path("healing-wave","Vague de soin","Healing Wave",4,0,5,50,"lorc/beam-wake",los=False,area=("Circle",2),effects=[heal("Allies",10,14,overflow=True)],cd=2),
 path("remedy","Remède","Remedy",2,0,5,50,"lorc/bandaged",effects=[purge("Allies"),status("Allies","Regen",8,2)],cd=1),
 # The Inquisitor strips and punishes.
 path("censure","Censure","Censure",3,2,6,30,"lorc/hammer-drop",damage=True,effects=[purge("Enemies")],cd=1),
 path("brand","Marque de l'hérétique","Heretic's Brand",3,2,6,30,"lorc/bleeding-eye",damage=True,effects=[status("Enemies","Resistance",-15,2),status("Enemies","Healing",-50,2)],cd=2),
 path("exorcism","Exorcisme","Exorcism",4,2,5,50,"lorc/haunting",damage=True,area=("Circle",1),effects=[purge("Enemies")],cd=2),
 path("zeal","Zèle","Zeal",1,0,0,50,"lorc/fire-ray",los=False,effects=[status("Caster","Ap",2,1),status("Caster","Damage",25,2),status("Caster","Resistance",-10,2)],cd=3),
 # The Oracle lays now what strikes or heals at the target's next turn: a poison or a heal of one turn.
 path("omen","Présage","Omen",3,2,6,30,"lorc/comet-spark",effects=[status("Enemies","Poison",42,1)],cd=1),
 path("foresight","Prévoyance","Foresight",2,0,5,30,"lorc/star-swirl",effects=[status("Allies","Regen",20,1)],cd=1),
 path("prophecy","Prophétie","Prophecy",4,2,6,50,"lorc/death-zone",los=False,area=("Circle",1),effects=[status("Enemies","Poison",56,1)],cd=2),
 path("vision","Vision","Vision",1,0,5,50,"lorc/moon",area=("Circle",1),effects=[status("Allies","Range",2,1),status("Allies","Regen",16,1)],cd=2),
 # The Rogue (sprint 204, docs/classes-voleur.md §5). The Assassin isolates, poisons, finishes off.
 path("backstab","Coup bas","Low Blow",3,1,1,30,"lorc/dripping-knife",damage=True,cd=1),
 path("envenom","Lame enduite","Coated Blade",2,1,2,30,"lorc/poison-gas",effects=[status("Enemies","Poison",7,3)],cd=1),
 path("execute","Exécution","Execution",5,1,1,50,"lorc/decapitation",damage=True,cd=2),
 path("plague","Fiole de peste","Plague Vial",3,1,3,50,"lorc/bottled-bolt",effects=[status("Enemies","Poison",12,3)],cd=2),
 # The Malandrin leaps, feints and lays traps where it stands: the Vault and the Feint of before, the Hamstring, the Caltrops (their glyph by hand, as the Jaws').
 path("hamstring","Coup de jarret","Hamstring",3,1,1,30,"delapouite/achilles-heel",damage=True,effects=[status("Enemies","Mp",-1,1)]),
 path("caltrops","Chausse-trappes","Caltrops",3,0,1,50,"delapouite/caltrops",area=("Cross",1),effects=[glyph("Enemies",2,10,13,stat="Mp",v=-2,st=1)],cd=2),
 # The Shadowmage (sprint 205, docs/classes-voleur-2.md §2) vanishes and strikes from the shadow; one target, close by, no wide area.
 path("shadow-step","Pas de l'ombre","Shadow Step",2,1,4,30,"delapouite/invisible",effects=[leap(),status("Caster","Hidden",1,1)],cd=2),
 path("shadow-blade","Lame d'ombre","Shadow Blade",3,1,2,30,"lorc/shadow-grasp",damage=True,cd=1),
 path("waylay","Guet-apens","Waylay",5,1,1,50,"lorc/moon-claws",damage=True,cd=2),
 path("false-front","Faux-semblant","False Front",2,1,4,50,"lorc/mirror-mirror",los=False,effects=[summon("mirror-image"),status("Caster","Hidden",1,2)],cd=3),
 # The Berserker's Ravager (sprint 206, docs/classes-berserker.md §3): areas, hit points spent.
 path("ravage","Ravage","Ravage",5,0,0,30,"lorc/slashed-shield",damage=True,area=("Circle",1),cd=1,hp=10),
 path("gore-charge","Charge sanglante","Bloody Charge",3,2,4,30,"lorc/sword-wound",damage=True,line=True,area=("Cross",1),effects=[leap()],cd=2,hp=6),
 path("crushing-blow","Coup écrasant","Crushing Blow",6,1,1,50,"lorc/skull-crack",damage=True,cd=1,hp=12),
 path("devour","Dévorer","Devour",4,1,1,50,"lorc/carnivore-mouth",damage=True,effects=[drain(50)],cd=1),
 # The Barbarian (sprint 207, docs/classes-berserker-2.md §2): the clan up close, or the beast.
 path("clan-cry","Cri du clan","Clan Cry",3,0,0,30,"lorc/wolf-howl",los=False,area=("Circle",2),effects=[status("Allies","Damage",15,2)],cd=3),
 path("savage-blow","Coup sauvage","Savage Blow",4,1,1,30,"lorc/paw",damage=True,cd=1),
 path("war-standard","Étendard de guerre","War Standard",3,0,0,50,"lorc/totem-mask",los=False,area=("Circle",2),effects=[status("Allies","Resistance",15,2)],cd=3),
 path("feral-leap","Bond sauvage","Feral Leap",4,2,4,50,"lorc/beast-eye",damage=True,line=True,area=("Cross",1),effects=[leap()],cd=2),
 # The Gladiator: one target, the execution, paid in blood.
 path("challenge","Défi","Challenge",2,1,4,30,"lorc/crossed-swords",effects=[status("Enemies","Resistance",-10,2)],cd=2),
 path("arena-strike","Coup d'arène","Arena Strike",4,1,1,30,"lorc/fist",damage=True,cd=1),
 path("blood-duel","Duel sanglant","Blood Duel",5,1,1,50,"lorc/bloody-stash",damage=True,effects=[drain(30)],cd=1,hp=10),
 path("behead","Décapitation","Beheading",6,1,1,50,"lorc/guillotine",damage=True,cd=2,hp=12),
 # The Paladin's Protector (sprint 208, docs/classes-paladin.md §4): holding a place around its allies.
 path("hallowed-ground","Terre sacrée","Hallowed Ground",3,0,0,30,"lorc/rosa-shield",los=False,area=("Circle",1),effects=[glyph("Allies",2,stat="Resistance",v=15,st=1)],cd=3),
 path("guardian-strike","Frappe gardienne","Guardian Strike",4,1,1,30,"lorc/shield-echoes",damage=True,effects=[shield("Caster",12,2)],cd=1),
 path("wall-of-faith","Muraille de foi","Wall of Faith",4,0,0,50,"lorc/castle",los=False,area=("Circle",1),effects=[shield("Allies",20,2)],cd=3),
 path("shield-of-faith","Bouclier de la foi","Shield of Faith",2,1,2,50,"lorc/surrounded-shield",effects=[shield("Allies",25,2)],cd=1),
 # The Templar (sprint 209, docs/classes-paladin-2.md §2): a blow that marks, a blow that gives back.
 path("seal-of-light","Sceau de lumière","Seal of Light",3,1,2,30,"lorc/sun-radiations",damage=True,effects=[status("Enemies","Resistance",-10,2)],cd=1),
 path("sacred-return","Retour sacré","Sacred Return",4,1,1,30,"lorc/shining-sword",damage=True,effects=[drain(30)],cd=1),
 path("sentence","Sentence","Sentence",4,1,2,50,"lorc/gavel",damage=True,effects=[purge("Enemies")],cd=1),
 path("crusade","Croisade","Crusade",3,2,3,50,"delapouite/knight-banner",damage=True,line=True,area=("Cross",1),effects=[leap()],cd=2),
 # The Avenger: the more its own have suffered, the harder.
 path("retribution","Rétribution","Retribution",4,1,1,30,"lorc/flaming-claw",damage=True,cd=1),
 path("vow","Vœu de vengeance","Vow of Vengeance",2,0,0,30,"lorc/bleeding-heart",los=False,effects=[status("Caster","Damage",20,2)],cd=3),
 path("verdict-blow","Coup du verdict","Verdict Blow",5,1,1,50,"lorc/cross-flare",damage=True,cd=2),
 path("holy-fury","Fureur sacrée","Holy Fury",1,0,0,50,"lorc/flame-spin",los=False,effects=[status("Caster","Ap",2,1),status("Caster","Resistance",-10,2)],cd=3),
 # The Summoner's Familiarist (sprint 212, docs/classes-invocateur-2.md §1-2): one beast raised, that grows; the Tamer keeps it, the Fusionist takes it in.
 path("familiar","Familier","Familiar",3,1,2,30,"lorc/fox-head",los=False,effects=[summon("familiar")],cd=4),
 path("titbit","Becquée","Titbit",2,1,6,30,"lorc/meat",los=False,effects=[heal("Allies",15,20),status("Allies","Damage",20,2)],cd=2,aims="familiar"),
 path("recall","Rappel","Recall",2,1,6,50,"lorc/return-arrow",los=False,effects=[swap()],cd=2,aims="familiar"),
 path("fusion","Fusion","Fusion",3,1,6,50,"lorc/interlaced-tentacles",los=False,effects=[fuse(3,35)],cd=3,aims="familiar"),
 # The Summoner's Demonist (sprint 213, docs/classes-invocateur-3.md §1): its hit points for power; the Demon Caller feeds its Demon, the Pactbearer pays for its own blows.
 path("demon","Démon","Demon",4,1,2,30,"lorc/horned-skull",los=False,effects=[summon("demon")],cd=4),
 path("blood-pact","Pacte de sang","Blood Pact",1,0,0,30,"lorc/evil-moon",los=False,area=("Circle",2),effects=[status("Allies","Damage",25,2)],cd=3,hp=8),
 path("unleash","Déchaînement","Unleash",2,1,6,50,"lorc/burning-eye",los=False,effects=[status("Allies","Damage",30,2),status("Allies","Mp",2,1)],cd=2,aims="demon",hp=6),
 path("blood-price","Prix du sang","Blood Price",4,1,5,50,"lorc/heart-drop",damage=True,cd=1,hp=10),
 # The Conjurer (sprint 213, §2): creatures laid, that do not move; the Elemental Caller makes them strike, the Astral Caller draws its figures on the ground.
 path("pillar","Pilier","Pillar",3,1,3,30,"lorc/crystal-growth",los=False,effects=[summon("pillar",mx=2)],cd=2),
 path("anchor","Ancrage","Anchoring",2,1,6,30,"lorc/guarded-tower",los=False,area=("Circle",1),effects=[shield("Allies",20,2)],cd=2,aims="pillar"),
 path("overload","Surcharge","Overload",2,1,6,50,"lorc/lightning-storm",los=False,area=("Circle",1),effects=[status("Allies","Damage",35,2)],cd=2,aims="pillar"),
 path("constellation","Constellation","Constellation",3,2,6,50,"lorc/star-prominences",los=False,area=("Circle",1),effects=[glyph("Enemies",2,9,12,stat="Mp",v=-1,st=1)],cd=2),
 # The Necromancer's Master of the Dead (sprint 214, docs/classes-necromancien.md §4): one great dead, kept and grown; the Lich takes it in, the Bone Lord drives it.
 path("stitch","Ravaudage","Stitching",2,1,6,30,"lorc/sewing-needle",los=False,effects=[heal("Allies",20,26),status("Allies","Resistance",15,2)],cd=2,aims="risen"),
 path("bone-rampart","Rempart d'os","Bone Rampart",2,1,5,30,"lorc/bone-gnawer",los=False,area=("Circle",1),effects=[shield("Allies",20,2)],cd=2,corpse=True),
 path("unlife","Non-vie","Unlife",3,1,6,50,"lorc/skull-ring",los=False,effects=[fuse(3,30)],cd=3,aims="risen"),
 path("marrow","Moelle","Marrow",2,1,6,50,"lorc/broken-bone",los=False,effects=[status("Allies","Damage",30,2),status("Allies","Mp",2,1)],cd=2,aims="risen"),
 # The Macabre Caller (sprint 215, docs/classes-necromancien-2.md §1): many dead, thrown away; the Ghoul Master feeds them, the Commander of the Dead spends them.
 path("ghoul","Goule","Ghoul",2,1,4,30,"lorc/carnivore-mouth",los=False,effects=[summon("ghoul",mx=3)],cd=2,corpse=True),
 path("quarry","Curée","Quarry",2,1,6,30,"lorc/grasping-claws",los=False,effects=[status("Enemies","Ordered",25,2)],cd=1),
 path("gorging","Ripaille","Gorging",2,1,5,50,"lorc/cauldron",los=False,area=("Circle",1),effects=[heal("Allies",15,20),status("Allies","Damage",20,2)],cd=2,corpse=True),
 path("offering","Sacrifice","Offering",3,1,6,50,"lorc/sacrificial-dagger",damage=True,los=False,area=("Circle",1),cd=2,aims="ghoul"),
 # The Occultist (sprint 215, §2): no creature at all, blood and souls; its poisons stack. The Sanguimancer pays in blood, the Thanaturge finishes off.
 path("hex","Malédiction","Hex",3,1,6,30,"lorc/cursed-star",effects=[status("Enemies","Resistance",-15,2),status("Enemies","Damage",-15,2)],cd=2),
 path("blight","Flétrissure","Blight",3,1,6,30,"lorc/dripping-goo",damage=True,effects=[status("Enemies","Poison",6,3)],cd=1),
 # Boiling Blood as heavy as Reaping (sprint 215, measured): 4 AP from 1 to 5 cells and cooldown 1 put the Sanguimancer 15 points over its class at 60.
 path("boiling-blood","Sang bouillant","Boiling Blood",4,1,4,50,"lorc/heartburn",damage=True,effects=[drain(20)],cd=2,hp=10),
 path("reaping","Moisson","Reaping",5,1,4,50,"lorc/scythe",damage=True,cd=2),
 # The Druid's Nature Warden (sprint 217, docs/classes-druide-2.md §1): ground that shelters, its glyphs lasting a turn more; the Sylvan strikes from it,
 # the Grove Keeper shields those in it and itself.
 path("hedge","Haie","Hedge",3,1,4,30,"delapouite/vines",area=("Cross",1),effects=shared(2,("Resistance",20,1),dict(stat="Mp",v=-1,st=1)),cd=2),
 path("barkskin","Écorce","Barkskin",2,0,4,30,"delapouite/log",los=False,effects=[shield("Allies",20,2)],cd=2),
 path("woods-wrath","Colère des bois","Wrath of the Woods",5,2,4,50,"lorc/tree-branch",damage=True,area=("Circle",1),cd=2),
 # The Grove Keeper's blow shields its caster (sprint 217, measured): a shield of 18 in a circle, for allies the bench has not, was never cast (−10 from the class at 60).
 path("guarding-bough","Branche gardienne","Guarding Bough",4,1,4,50,"delapouite/barricade",damage=True,effects=[shield("Caster",12,2)],cd=2),
 # The Shapeshifter (§2): a form of 3 turns, no hit point paid, that ends and comes back (the Berserker's lasts the fight and is paid for, D96 §1.4);
 # its blows up close ask for it. The Changeform leaps in, the Wild Druid tramples the ground it keeps.
 path("wild-shape","Forme sauvage","Wild Shape",2,0,0,30,"lorc/sharp-smile",los=False,effects=[status("Caster","Rage",20,3)],cd=4),
 path("claws","Griffes","Claws",3,1,1,30,"lorc/wolverine-claws",damage=True,form=True,per=2),
 path("pounce","Bond du fauve","Pounce",4,2,4,50,"lorc/saber-tooth",damage=True,line=True,area=("Cross",1),effects=[leap()],cd=2,form=True),
 path("trample","Piétinement","Trample",4,0,0,50,"lorc/hoof",damage=True,los=False,area=("Circle",1),effects=shared(2,("Resistance",15,1),dict(stat="Mp",v=-1,st=1)),cd=2,form=True),
 # The Druid's Shaman (sprint 218, docs/classes-chaman.md §1-2): spirits without a body, that last two turns, and ground that strikes the enemies and
 # spurs the allies. The Spirit Caller calls more of them and spurs them; the Nature Elementalist strikes from the ground.
 path("spirit","Esprit","Spirit",3,1,3,30,"lorc/totem-head",los=False,effects=[summon("spirit",mx=2)],cd=3),
 path("rumbling-ground","Sol grondant","Rumbling Ground",3,1,4,30,"lorc/volcano",area=("Cross",1),effects=shared(2,("Damage",10,1),dict(dmin=6,dmax=9)),cd=2),
 path("communion","Communion","Communion",2,0,0,50,"lorc/third-eye",los=False,area=("Circle",2),effects=[status("Allies","Damage",20,2),status("Allies","Mp",1,1)],cd=3),
 path("living-storm","Tempête vivante","Living Storm",4,2,5,50,"lorc/lightning-tree",damage=True,area=("Circle",1),effects=[glyph("Enemies",2,3,4)],cd=2),
]
# The Inquisitor is the Priest's striker (docs/classes-pretre-2.md §2): its blows at the full budget, as every path's.
# The Living Storm at 0.45 of it (sprint 218, measured, docs/classes-chaman.md §4): at the full budget, a blow of the Shaman whose spirits already
# bring it to the class's mean, it put the Nature Elementalist 14 to 17 points over it at 60 and 100.
for sp in paths:
    if sp["damageMax"] > 0:
        budget(sp, 0.45 if sp["id"] == "path-living-storm" else 1)
_names = [sp["name"]["fr"] for sp in base + sentinel + guard + mage + priest + rogue + berserker + paladin + summoner + necromancer + druid + creatures + beasts + twins]
assert not [t for t in twins if t.get("twin") and _names.count(t["name"]["fr"]) > 1], [t["name"]["fr"] for t in twins if t.get("twin") and _names.count(t["name"]["fr"]) > 1]
_names = [sp["name"]["en"] for sp in base + sentinel + guard + mage + priest + rogue + berserker + paladin + summoner + necromancer + druid + creatures + beasts + twins]
assert not [t for t in twins if t.get("twin") and _names.count(t["name"]["en"]) > 1], [t["name"]["en"] for t in twins if t.get("twin") and _names.count(t["name"]["en"]) > 1]
# Every spell of the game may land a critical hit, one time in ten (D61); "crit" left out means never.
for sp in base + sentinel + guard + mage + priest + rogue + berserker + paladin + summoner + necromancer + druid + creatures + beasts + [t for t in twins if t not in tree] + paths:
    sp["crit"] = 10
spells = base + sentinel + guard + mage + priest + rogue + berserker + paladin + summoner + necromancer + druid + creatures + beasts + twins + paths
# One id a spell (sprint 206: the Berserker's Howl took a monster's id, and its class listed the monster's spell).
_ids = [sp["id"] for sp in spells]
assert len(_ids) == len(set(_ids)), [i for i in set(_ids) if _ids.count(i) > 1]
lines = ",\n".join("  " + json.dumps(s, ensure_ascii=False, separators=(", ", ": ")).replace('{"', '{ "').replace('}', ' }').replace('[ {', '[{').replace('} ]', '}]') for s in spells)
open(f"{root}/spells.json", "w").write("[\n" + lines + "\n]\n")
classes = json.load(open(f"{root}/classes.json"), object_pairs_hook=O)
lists = {"archer": [s["id"] for s in sentinel], "warrior": [s["id"] for s in guard], "mage": [s["id"] for s in mage], "priest": [s["id"] for s in priest], "rogue": [s["id"] for s in rogue], "berserker": [s["id"] for s in berserker], "paladin": [s["id"] for s in paladin], "summoner": [s["id"] for s in summoner], "necromancer": [s["id"] for s in necromancer], "druid": [s["id"] for s in druid]}
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
# The Decoy of the talent (sprint 142): it does not act; a tenth of its summoner's hit points.
# It draws the blows of the monsters near it (a lure, data since sprint 200).
summons.append(O([("id","decoy"),("name",O([("fr","Leurre"),("en","Decoy")])),("look","female-d"),("hp",1),("ap",0),("mp",0),("initiative",0),("spells",[]),("hpOfSummoner",10),("lure",True)]))
# The Illusionist's (sprint 200, docs/classes-mage-2.md §2-3): its summoner's look, lures both; the Image does not act, the Double strikes.
summons.append(O([("id","mirror-image"),("name",O([("fr","Image"),("en","Image")])),("look","female-d"),("hp",1),("ap",0),("mp",0),("initiative",0),("spells",[]),("hpOfSummoner",30),("mirror",True),("lure",True)]))
summons.append(O([("id","mirror-double"),("name",O([("fr","Double"),("en","Double")])),("look","female-d"),("hp",1),("ap",3),("mp",3),("initiative",80),("spells",["mirror-strike"]),("hpOfSummoner",15),("mirror",True),("lure",True)]))
# The Familiarist's (sprint 212, docs/classes-invocateur-2.md §1): fed by its branch, it grows.
summons.append(young("familiar","Familier","Familiar","wolf-melee",90,40,6,4,65,["familiar-claw"]))
# The Demonist's and the Conjurer's (sprint 213, docs/classes-invocateur-3.md): fed by their branch; the Pillar never moves.
summons.append(young("demon","Démon","Demon","stag-melee",85,50,6,4,55,["demon-rend"]))
summons.append(young("pillar","Pilier","Pillar","golem-ranged",75,40,6,0,30,["pillar-shard"]))
# The Necromancer's (sprint 214, docs/classes-necromancien.md §2): it rises from a corpse and wears it, fed by its raiser's class.
summons.append(O(list(young("risen","Mort levé","Risen","zombie-tank",85,40,6,3,45,["risen-claw"]).items()) + [("wearsCorpse", True)]))
summons.append(O(list(young("ghoul","Goule","Ghoul","zombie-tank",70,20,6,4,70,["ghoul-bite"]).items()) + [("wearsCorpse", True)]))
# The Summoner's (sprint 211, docs/classes-invocateur.md §2): their hit points and characteristics a share of its own (its class's rules).
summons += [
 young("hound","Molosse","Hound","wolf-melee",80,30,6,4,60,["hound-fang"]),
 young("wisp","Feu follet","Will-o'-Wisp","bee-melee",60,20,6,3,70,["wisp-glow"]),
 young("colossus","Colosse","Colossus","golem-melee",85,60,4,3,40,["colossus-fist"]),
]
summons += [
 young("wolf-cub","Louveteau","Wolf Cub","wolf-melee",70,12,6,4,90,["cub-bite"]),
 young("spore-sprout","Pousse de vesse","Puffball Sprout","mushroom-ranged",70,20,6,2,80,["spore-burst"],["final-spores"]),
 young("bloated-spawn","Bouffi recousu","Stitched Bloater","zombie-tank",85,40,6,2,40,["rot-fist"],["explosion"]),
 young("hermit-spawn","Jeune bernard","Young Hermit","crab-support",70,30,6,3,55,["shell-guard","pinch"]),
 young("worker-spawn","Jeune ouvrière","Young Worker","bee-melee",70,30,6,4,95,["sting"],["alarm"]),
 young("digger-spawn","Jeune fouisseuse","Young Digger","mole-melee",70,35,6,3,60,["dig-claw","surface"]),
 # The Shaman's spirit (sprint 218, docs/classes-chaman.md §1): no body (it holds no cell, hides no line of sight) and two turns of play, then it fades.
 # A wolf's model, drawn small, until the artistic overhaul (no spirit model exists).
 O(list(young("spirit","Esprit","Spirit","wolf-melee",65,20,6,3,75,["spirit-touch"]).items()) + [("lasts", 2), ("spirit", True)]),
 # The menhir keeper's risen stones (sprint 133, docs/familles-foret.md §4.4).
 young("pebbling","Caillou éveillé","Pebbling","golem-melee",70,40,6,3,100,["cobble-punch"]),
]
open(f"{root}/summons.json", "w").write(json.dumps(summons, ensure_ascii=False, indent=2) + "\n")

# The monsters' passives (D69, data/passives.json): a trigger, its numbers, spell effects with the bearer as caster.
def P(id, fr, en, dfr, den, trigger, effects=None, area=None, **kw):
    d = O([("id",id),("name",O([("fr",fr),("en",en)])),("description",O([("fr",dfr),("en",den)])),("trigger",trigger)])
    if effects: d["effects"] = effects
    if area: d["area"] = O([("shape", area[0]), ("radius", area[1])])
    for k in ("melee", "percent", "range", "damageMin", "damageMax", "element", "clear", "perFight", "oncePerTurn", "still"):
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
 # The Sentinel's paths (sprint 191, D95, words of docs/textes-specialisations.md §3.1); those of one name take each other's place (Replaces).
 P("spec-calm-eye","Œil calme","Calm Eye","Aucun ennemi à 3 cases au début de ton tour : +15 % de dégâts pendant 1 tour.","No enemy within 3 cells at the start of your turn: +15% damage for 1 turn.",
   "TurnStart",[status("Caster","Damage",15,1)],clear=3),
 P("spec-calm-eye-near","Œil calme","Calm Eye","Aucun ennemi à 2 cases au début de ton tour : +15 % de dégâts pendant 1 tour.","No enemy within 2 cells at the start of your turn: +15% damage for 1 turn.",
   "TurnStart",[status("Caster","Damage",15,1)],clear=2),
 P("spec-calm-eye-long","Œil calme","Calm Eye","Aucun ennemi à 3 cases au début de ton tour : +25 % de dégâts pendant 2 tours.","No enemy within 3 cells at the start of your turn: +25% damage for 2 turns.",
   "TurnStart",[status("Caster","Damage",25,2)],clear=3),
 # The Guard's (sprint 192).
 P("spec-close-ranks-9","Serrez les rangs","Close Ranks","Au début de ton tour, bouclier de 9 sur toi et tes alliés à 1 case.","At the start of your turn, a shield of 9 on you and allies 1 cell away.","TurnStart",[shield("Allies",9,2)],area=("Circle",1)),
 P("spec-close-ranks-12","Serrez les rangs","Close Ranks","Au début de ton tour, bouclier de 12 sur toi et tes alliés à 2 cases.","At the start of your turn, a shield of 12 on you and allies within 2 cells.","TurnStart",[shield("Allies",12,2)],area=("Circle",2)),
 P("spec-close-ranks-17","Serrez les rangs","Close Ranks","Au début de ton tour, bouclier de 17 sur toi et tes alliés à 2 cases.","At the start of your turn, a shield of 17 on you and allies within 2 cells.","TurnStart",[shield("Allies",17,2)],area=("Circle",2)),
 P("spec-not-today","Pas aujourd'hui","Not Today","Une fois par combat, un allié à 2 cases qui tomberait reste à 1 PV.","Once per fight, an ally within 2 cells who would fall stays at 1 HP.","AllyFalls",range=2,perFight=1),
 P("spec-thorns","Épines","Thorns","Qui te frappe au contact est empoisonné : 8 pendant 2 tours, neutre.","Whoever strikes you in melee is poisoned: 8 for 2 turns, neutral.","Hit",[status("Attacker","Poison",8,2)],melee=True),
 P("spec-carried-through-1","Sur la lancée","Carried Through","Un ennemi achevé : +1 PM.","An enemy finished off: +1 MP.","Kill",[status("Caster","Mp",1,1)]),
 P("spec-carried-through-2","Sur la lancée","Carried Through","Un ennemi achevé : +1 PM et +1 PA.","An enemy finished off: +1 MP and +1 AP.","Kill",[status("Caster","Mp",1,1),status("Caster","Ap",1,1)]),
 P("spec-carried-through-3","Sur la lancée","Carried Through","Un ennemi achevé : +1 PM et +2 PA.","An enemy finished off: +1 MP and +2 AP.","Kill",[status("Caster","Mp",1,1),status("Caster","Ap",2,1)]),
 P("spec-hands-off","Qui s'y frotte","Hands Off","Qui te frappe au contact perd 1 PM.","Whoever strikes you in melee loses 1 MP.","Hit",[status("Attacker","Mp",-1,1)],melee=True),
 P("spec-hands-off-ap","Qui s'y frotte","Hands Off","Qui te frappe au contact perd aussi 1 PA, une fois par tour.","Whoever strikes you in melee also loses 1 AP, once per turn.","Hit",[status("Attacker","Ap",-1,1)],melee=True,oncePerTurn=True),
 P("spec-no-way-out","Personne ne sort","No Way Out","Les ennemis à ton contact au début de ton tour perdent 1 PM à leur prochain tour.","Enemies next to you at the start of your turn lose 1 MP on their next turn.","TurnStart",[status("Enemies","Mp",-1,1)],area=("Circle",1)),
 # The Warrior's finals (sprint 197, docs/classes-guerrier.md §3): a post held, the charge sounded, a fighter on the ropes.
 P("spec-sentry","Poste tenu","At My Post","Au début de ton tour, si tu n'as pas marché au tour précédent : bouclier de 12 sur toi et tes alliés à 2 cases.","At the start of your turn, if you did not walk last turn: a shield of 12 on you and allies within 2 cells.","TurnStart",[shield("Allies",12,2)],area=("Circle",2),still=True),
 P("spec-charge","Sonner la charge","Sound the Charge","Au premier tour du combat : toi et tes alliés à 2 cases, +2 PM pendant 1 tour et +15 % de dégâts pendant 2 tours.","On the first turn of the fight: you and allies within 2 cells, +2 MP for 1 turn and +15% damage for 2 turns.","TurnStart",[status("Allies","Mp",2,1),status("Allies","Damage",15,2)],area=("Circle",2),perFight=1),
 P("spec-on-the-ropes","Dans les cordes","On the Ropes","Une fois par combat, sous la moitié de tes PV : les ennemis à ton contact reculent de 2 cases, et +2 PA pendant 1 tour.","Once per fight, below half your HP: enemies next to you are pushed back 2 cells, and +2 AP for 1 turn.","LowHp",[push(2),status("Caster","Ap",2,1)],area=("Circle",1),percent=50),
 # The Arcanist's (sprint 199, docs/classes-mage.md §1): still, it gathers a point.
 P("spec-steady-mind","Esprit posé","Steady Mind","Au début de ton tour, si tu n'as pas marché au tour précédent : +1 PA.","At the start of your turn, if you did not walk last turn: +1 AP.","TurnStart",[status("Caster","Ap",1,1)],still=True),
 # The Archer's final (sprint 198, docs/classes-archer.md §3.1): the bounty paid.
 # The Rogue's Phantom (sprint 205, docs/classes-voleur-2.md §2): it vanishes once a fight when nearly down.
 P("spec-vanish","Évanouissement","Vanishing","Une fois par combat, sous 30 % de tes PV : Discrétion pendant 2 tours et +2 PM pendant 1 tour.","Once per fight, below 30% of your HP: Stealth for 2 turns and +2 MP for 1 turn.","LowHp",[status("Caster","Hidden",1,2),status("Caster","Mp",2,1)],percent=30),
 P("spec-bounty","Prime touchée","Bounty Collected","Un ennemi achevé : +2 PA pendant 1 tour et +15 % de dégâts pendant 2 tours.","An enemy finished off: +2 AP for 1 turn and +15% damage for 2 turns.","Kill",[status("Caster","Ap",2,1),status("Caster","Damage",15,2)]),
 # The Sentinel's.
 P("spec-quick-heels","Talons vifs","Quick Heels","Si on te frappe au contact : +2 PM pendant 1 tour.","If you are struck in melee: +2 MP for 1 turn.","Hit",[status("Caster","Mp",2,1)],melee=True),
 P("spec-quick-heels-3","Talons vifs","Quick Heels","Si on te frappe au contact : +3 PM pendant 1 tour.","If you are struck in melee: +3 MP for 1 turn.","Hit",[status("Caster","Mp",3,1)],melee=True),
 P("spec-saw-it-coming","Vu venir","Saw It Coming","Une fois par combat, un coup au contact te donne un bouclier de 40.","Once per fight, a melee blow gives you a shield of 40.",
   "Hit",[shield("Caster",40,2)],melee=True,perFight=1),
]
open(f"{root}/passives.json", "w").write(json.dumps(passives, ensure_ascii=False, indent=2) + "\n")
print(len(spells), {k: len(v) for k, v in lists.items()})
