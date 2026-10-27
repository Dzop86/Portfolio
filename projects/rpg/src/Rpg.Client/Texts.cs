using System.Globalization;
using Rpg.Core;

namespace Rpg.Client;

/// <summary>Every text of the client, in French and English; a test checks that both have the same keys and placeholders.</summary>
public sealed class Texts
{
    internal static readonly IReadOnlyDictionary<string, string> Fr = new Dictionary<string, string>
    {
        ["round"] = "Tour {0}",
        ["your-turn"] = "À vous de jouer",
        ["their-turn"] = "{0} joue",
        ["hp"] = "PV",
        ["ap"] = "PA",
        ["mp"] = "PM",
        ["end-turn"] = "Fin du tour",
        ["item.food"] = "Rend {0} PV, hors combat.",
        ["victory"] = "Victoire !",
        ["defeat"] = "Défaite…",
        ["draw"] = "Match nul",
        ["help"] = "Clic : se déplacer · 1 à 6 et Ctrl+1 à 6 : choisir un sort · Échap : annuler, puis menu · Espace : fin du tour",
        ["spell"] = "{0} ({1} PA, portée {2}-{3})",
        ["inventory.button"] = "Inventaire",
        ["inventory.title"] = "Inventaire et équipement",
        ["inventory.search"] = "Rechercher un objet",
        ["inventory.hint"] = "Survolez un objet pour voir sa fiche ; un clic l'équipe, un clic sur un emplacement l'enlève.",
        ["inventory.empty"] = "Rien ici.",
        ["inventory.free"] = "{0} libre(s)",
        ["inventory.saved"] = "Équipement enregistré.",
        ["inventory.level"] = "niveau {0}",
        ["inventory.set"] = "{0} : {1}",
        ["inventory.pieces"] = "{0} pièces {1}",
        ["result.loot"] = "Butin : {0}",
        ["page.All"] = "Tout",
        ["page.Equipment"] = "Équipement",
        ["page.Consumable"] = "Consommables",
        ["page.Resource"] = "Ressources",
        ["page.Quest"] = "Quête",
        ["kind.Consumable"] = "consommable",
        ["kind.Resource"] = "ressource",
        ["kind.Quest"] = "objet de quête",
        ["slot.Ring1"] = "Anneau 1",
        ["slot.Ring2"] = "Anneau 2",
        ["slot.Amulet"] = "Amulette",
        ["slot.Belt"] = "Ceinture",
        ["slot.Cape"] = "Cape",
        ["slot.Hat"] = "Coiffe",
        ["slot.Chest"] = "Plastron",
        ["slot.Shoulders"] = "Épaulettes",
        ["slot.OneHanded"] = "Arme à une main",
        ["slot.TwoHanded"] = "Arme à deux mains",
        ["slot.Shield"] = "Bouclier",
        ["slot.Pet"] = "Familier",
        ["slot.Mount"] = "Monture",
        ["slot.Boots"] = "Bottes",
        ["slot.Ring"] = "Anneau",
        ["points.button"] = "Personnage",
        ["points.title"] = "Caractéristiques et sorts",
        ["points.characteristics"] = "Caractéristiques : {0} point(s) à répartir",
        ["points.spells"] = "Sorts : {0} point(s) de sort",
        ["points.talents"] = "Talents",
        ["talents.left"] = "Talents : {0} point(s) à dépenser",
        ["talents.closed"] = "L'arbre des talents s'ouvre au niveau {0}.",
        ["talents.hint"] = "Un clic prend un talent ou le rend (avec ceux qui en dépendent). D'abord une porte : une paire (sa réaction) ou un Cœur (Élément pur) ; puis un style, un étage après l'autre, sa clé au niveau 45, une clé par style ; deux paires au plus, sans élément commun ; une seule grande bascule (la Conversion). La Foudre passe par Électrisation, puis une Transmutation (du Feu ou de l'Air). Rendre un talent coûte 10 × le niveau en Éclats après le niveau 40, dans un village.",
        ["talents.tile"] = "{0} · {1}, {2}, {3} point(s)",
        ["talents.branch.Fork"] = "Paires",
        ["talents.branch.Pure"] = "Élément pur",
        ["talents.branch.Braids"] = "Tresse : Foudre",
        ["talents.branch.Alchemy"] = "Alchimie",
        ["talents.branch.Melee"] = "Mêlée",
        ["talents.branch.Distance"] = "Distance",
        ["talents.branch.Terrain"] = "Terrain",
        ["talents.branch.Tempo"] = "Tempo",
        ["talents.branch.Support"] = "Soutien",
        ["talents.tier.First"] = "étage 1",
        ["talents.tier.Second"] = "étage 2",
        ["talents.tier.Key"] = "clé",
        ["talents.trunk"] = "Tronc, gratuit : {0}",
        ["talents.trunk.Damage"] = "+{0} % de dégâts (niv. {1})",
        ["talents.trunk.Resistance"] = "+{0} % de résistances (niv. {1})",
        ["talents.trunk.Hp"] = "+{0} % de PV (niv. {1})",
        ["talents.trunk.unlit"] = "{0}, à venir",
        ["talents.taken.Earth"] = "la Terre est déjà prise par {0}",
        ["talents.taken.Fire"] = "le Feu est déjà pris par {0}",
        ["talents.taken.Water"] = "l'Eau est déjà prise par {0}",
        ["talents.taken.Air"] = "l'Air est déjà pris par {0}",
        ["talents.replaced"] = "La Conversion remplace {0} : son point revient.",
        ["talents.losing"] = "Le rendre rend aussi : {0}",
        ["talents.why.Opens"] = "L'arbre s'ouvre au niveau {0}.",
        ["talents.why.Level"] = "S'ouvre au niveau {0}.",
        ["talents.why.Door"] = "Demande d'abord une porte : une paire ou un Cœur.",
        ["talents.why.Parent"] = "Demande d'abord {0}.",
        ["talents.why.or"] = " ou ",
        ["talents.why.Excludes"] = "Ne se prend pas avec {0}.",
        ["talents.why.Pair"] = "Paire impossible : {0}.",
        ["talents.why.KeyPerStyle"] = "Une clé par style : {0} est déjà prise.",
        ["talents.why.Switch"] = "Une seule grande bascule : {0} est déjà prise.",
        ["talents.why.Elements"] = "Deux éléments hors base au plus.",
        ["talents.why.Points"] = "Pas assez de points : ce talent en coûte {0}.",
        ["tree.title"] = "Arbre des talents · niveau {0}",
        ["tree.open"] = "Ouvrir l'arbre (T)",
        ["tree.left"] = "{0} point(s) à dépenser sur {1}",
        ["tree.closed"] = "L'arbre s'ouvre au niveau {0} : le tronc s'y allume tout seul.",
        ["tree.elements"] = "Éléments hors base : {0} / {1}",
        ["tree.switch"] = "Grande bascule : {0}",
        ["tree.switch.none"] = "aucune",
        ["tree.styles"] = "Styles : une clé par style, au niveau 45",
        ["tree.fork"] = "La fourche : une paire (sa réaction) ou un Cœur (Élément pur)",
        ["tree.trunk"] = "Tronc, gratuit",
        ["tree.trunk.Damage"] = "+{0} % dégâts",
        ["tree.trunk.Resistance"] = "+{0} % résist.",
        ["tree.trunk.Hp"] = "+{0} % PV",
        ["tree.trunk.detail"] = "Le tronc s'allume tout seul, sans point : {0}.",
        ["tree.fold"] = "{0} · {1}",
        ["tree.fold.detail"] = "{0} : clique pour déployer ce style.",
        ["tree.legend"] = "Légende",
        ["tree.plan"] = "Planifier au niveau {0}",
        ["tree.plan.leave"] = "Revenir à mon arbre",
        ["tree.plan.on"] = "Planification au niveau {0} : les nœuds en pointillés pistache sont un plan, rien ne s'enregistre.",
        ["tree.path"] = "Chemin : {0} ({1} point(s))",
        ["tree.path.none"] = "Aucun chemin n'y mène avec tes nœuds.",
        ["tree.suggested"] = "Build suggéré…",
        ["tree.suggested.loaded"] = "{0} chargé en brouillon : enregistre pour le garder.",
        ["tree.code.copy"] = "Copier",
        ["tree.code.copied"] = "Code copié : {0}",
        ["tree.code.load"] = "Charger",
        ["tree.code.placeholder"] = "Code de build",
        ["tree.code.Unreadable"] = "Ce n'est pas un code de build.",
        ["tree.code.OldVersion"] = "Ce code vient d'une ancienne version de l'arbre ({0}) : il ne se charge plus.",
        ["tree.code.Level"] = "Ce build demande le niveau {0}.",
        ["tree.code.loaded"] = "Build chargé en brouillon : enregistre pour le garder.",
        ["tree.badge"] = "{0} point(s) de talent à dépenser",
        ["spell.origin"] = "Donné par {0}",
        ["spell.replaces"] = "Prend la place de {0}",
        ["spell.modified"] = "Modifié par l'arbre : {0}",
        ["spell.new"] = "nouveau",
        ["deck.swap"] = "{0} prend la place de {1} dans ton deck.",
        ["deck.left"] = "{0} quitte ton deck jusqu'à l'enregistrement.",
        ["tree.before-after"] = "Sur un coup type (à 2 cases, sans bouger, premier sort de 3 PA) : {0} % → {1} %",
        ["fight.lights"] = "Talents du tour : {0}",
        ["fight.light.on"] = "{0} allumé ({1} %)",
        ["fight.light.off"] = "{0} éteint ({1} %)",
        ["tree.layer.Foundations"] = "Fondations",
        ["tree.layer.Affinities"] = "Paires",
        ["tree.layer.Pure"] = "Élément pur",
        ["tree.layer.Fusion"] = "Tresse",
        ["tree.layer.Styles"] = "Styles",
        ["tree.banner.Foundations"] = "Le tronc s'allume tout seul avec le niveau : rien à choisir, tout à gagner.",
        ["tree.banner.Affinities"] = "Une paire associe deux éléments : frapper l'un sur l'empreinte de l'autre déclenche sa réaction. Deux paires au plus, sans élément commun.",
        ["tree.banner.Pure"] = "Un élément poussé à fond, les autres un peu moins : un Cœur ouvre les styles sans toucher à tes éléments.",
        ["tree.banner.Fusion"] = "Les deux éléments d'une paire courent ensemble sans se confondre : d'abord une Transmutation, puis la Conversion.",
        ["tree.banner.Styles"] = "Une manière de se battre, ouverte à toutes les portes : on la monte un étage après l'autre.",
        ["tree.banner.Melee"] = "Au contact : plus fort de près, au prix de la portée.",
        ["tree.banner.Distance"] = "De loin : plus de portée, moins de mordant au contact.",
        ["tree.banner.Support"] = "Pour les autres : tes sorts aident un allié au lieu de le frapper. Fait pour jouer à plusieurs.",
        ["tree.banner.Terrain"] = "Le terrain : glyphes, obstacles, et chaque pas qui compte.",
        ["tree.banner.Tempo"] = "Le rythme : PA, relances et place dans le deck.",
        ["tree.banner.Alchemy"] = "Les réactions : des empreintes qui durent, se partagent ou se propagent. Demande une paire.",
        ["tree.banner.Key"] = "Une clé par style, au niveau 45 : la plus forte, et elle coûte 2 points.",
        ["tree.banner.Choice"] = "Un choix : un seul des nœuds encadrés.",
        ["tree.equation"] = "{0} = {1} + {2}",
        ["tree.counterpart"] = "En échange : {0}",
        ["tree.title.heart.Earth"] = "Cœur de Terre",
        ["tree.title.heart.Fire"] = "Cœur de Feu",
        ["tree.title.heart.Water"] = "Cœur d'Eau",
        ["tree.title.heart.Air"] = "Cœur d'Air",
        ["tree.term.line"] = "{0} : {1}",
        ["tree.title.and"] = "{0} et {1}",
        ["tree.title.converted"] = "Conversion : {0}",
        ["tree.title.none"] = "Sans talent",
        ["tree.summary.counterparts"] = "Contreparties : {0}",
        ["tree.summary.none"] = "Aucune contrepartie.",
        ["tree.summary.best"] = "Au mieux, +{0} % sur un coup.",
        ["tree.summary.ceiling"] = "Au mieux, +{0} % sur un coup : au-delà de +{1} %, le surplus est perdu.",
        ["tree.advice"] = "Conseil : {0} et {1}, tes caractéristiques les plus hautes, désignent {2}.",
        ["tree.helps"] = "Avec tes éléments : {0}",
        ["tree.help.Earth"] = "Terre, un bouclier de 60 % des dégâts",
        ["tree.help.Fire"] = "Feu, un soin de 40 % des dégâts (15 % de ses PV au plus)",
        ["tree.help.Water"] = "Eau, +5 % de résistance par PA (20 au plus)",
        ["tree.help.Air"] = "Air, 1 PM tous les 2 PA (2 au plus) et 1 PO",
        ["tree.group"] = "Pour jouer à plusieurs.",
        ["tree.help.button"] = "?",
        ["tree.glossary"] = "Glossaire",
        ["tree.term.mark"] = "empreinte",
        ["tree.term.mark.def"] = "le coup direct d'un élément de base laisse sa marque sur l'ennemi jusqu'à la fin de ton prochain tour",
        ["tree.term.reaction"] = "réaction",
        ["tree.term.reaction.def"] = "frapper une empreinte avec l'autre élément de la paire déclenche son état",
        ["tree.term.door"] = "porte",
        ["tree.term.door.def"] = "une paire ou un Cœur : il en faut une avant le premier étage d'un style",
        ["tree.term.key"] = "clé",
        ["tree.term.key.def"] = "le dernier étage d'un style, au niveau 45, une par style",
        ["tree.term.switch"] = "grande bascule",
        ["tree.term.switch.def"] = "un talent qui change tes éléments pour de bon, comme la Conversion ; une seule dans l'arbre",
        ["tree.term.beyond"] = "hors base",
        ["tree.term.beyond.def"] = "un élément autre que Terre, Feu, Eau et Air, comme la Foudre ; deux au plus",
        ["tree.tuto.1"] = "Le tronc, en bas, s'allume tout seul avec le niveau.",
        ["tree.tuto.2"] = "Choisis d'abord une porte : une paire (deux éléments et leur réaction) ou un Cœur (un seul élément).",
        ["tree.tuto.3"] = "Puis monte un style, un étage après l'autre. Survole un nœud pour tout savoir, la légende est derrière « ? ».",
        ["tree.tuto.next"] = "Suivant",
        ["tree.tuto.done"] = "Compris",
        ["tree.choice.transmute"] = "Transmutation (au choix)",
        ["tree.choice.heart"] = "Élément pur : un seul Cœur",
        ["tree.state.Taken"] = "pris",
        ["tree.state.Open"] = "disponible",
        ["tree.state.Locked"] = "verrouillé (porte, parent, points)",
        ["tree.state.Excluded"] = "exclu (raison au survol)",
        ["tree.state.Level"] = "niveau trop bas",
        ["tree.coming"] = "à venir (autre paire)",
        ["tree.cost"] = "{0} pts",
        ["tree.taken"] = "Pris.",
        ["tree.can"] = "Disponible : un clic ou Entrée le prend.",
        ["tree.confirm"] = "Rendre {0} rend aussi : {1}.",
        ["tree.confirm.yes"] = "Rendre",
        ["tree.confirm.no"] = "Garder",
        ["tree.gained"] = "Gagnés : {0}",
        ["tree.lost"] = "Perdus : {0}",
        ["tree.undo"] = "Annuler les changements",
        ["tree.undone"] = "Changements annulés : les talents enregistrés sont revenus.",
        ["tree.keys"] = "Flèches : se déplacer · Entrée : prendre ou rendre · Ctrl+S : enregistrer",
        ["bar.Talents"] = "Talents",
        ["talents.reset"] = "L'arbre des talents a changé : les talents de {0} lui sont rendus, gratuitement, une fois.",
        ["pair.bogged"] = "Boue",
        ["pair.chilled"] = "Glace",
        ["pair.cracked"] = "Magma",
        ["pair.electrified"] = "Foudre",
        ["pair.fogged"] = "Vapeur",
        ["pair.blinded"] = "Sable",
        ["points.rank"] = "rang {0}/{1}",
        ["points.next"] = "rang suivant : {0} point(s)",
        ["points.max"] = "rang maximum",
        ["points.save"] = "Enregistrer",
        ["points.close"] = "Fermer",
        ["points.saved"] = "Points enregistrés.",
        ["points.offline"] = "Hors ligne : rien n'est enregistré.",
        ["window.in-fight"] = "En combat : consultation seulement.",
        ["sd.cost"] = "Coût : {0} PA",
        ["sd.range"] = "Portée : {0} à {1}",
        ["sd.per-turn"] = "{0} lancer(s) par tour",
        ["sd.crit-chance"] = "Critique : {0} %",
        ["sd.damage"] = "{0} à {1} dégâts {2}",
        ["sd.crit"] = "{0} à {1} en critique",
        ["sd.unlock"] = "Se débloque au niveau {0}",
        ["sd.next"] = "Rang suivant ({0} point(s) de sort) : {1} à {2}",
        ["sd.next-cost"] = "Rang suivant : {0} point(s) de sort",
        ["sd.hint"] = "Survolez un sort pour voir sa fiche ; les dégâts comptent vos caractéristiques et votre équipement.",
        ["deck.add"] = "Ajouter au deck",
        ["deck.remove"] = "Retirer du deck",
        ["deck.in"] = "dans le deck",
        ["deck.count"] = "Deck {0}/{1}",
        ["deck.clear"] = "Vider le deck",
        ["deck.copy"] = "Copier le code",
        ["deck.copied"] = "Code copié : {0}",
        ["deck.code"] = "Code de deck",
        ["deck.use"] = "Utiliser",
        ["deck.code-used"] = "Deck repris du code.",
        ["deck.code-partly"] = "Deck repris du code ; {0} sort(s) pas encore débloqué(s) laissé(s) de côté.",
        ["deck.bad-code"] = "Ce n'est pas un code de deck de votre classe.",
        ["deck.help"] = "Glissez un sort sur le deck ; glissez-le hors du deck pour le retirer.",
        ["sd.all"] = "Tous",
        ["sd.rank"] = "Rang {0}/{1}",
        ["sd.locked"] = "Niv. {0}",
        ["sd.preview"] = "Aperçu : enregistre tes talents",
        ["sheet.tab.stats"] = "Caractéristiques",
        ["sheet.tab.spells"] = "Sorts",
        ["sheet.level"] = "Niveau {0} · {1}",
        ["sheet.hp"] = "Points de vie",
        ["sheet.tree"] = "Arbre : +{0} % de dégâts · +{1} % de résistances · +{2} % de PV",
        ["sheet.ap"] = "PA",
        ["sheet.mp"] = "PM",
        ["sheet.initiative"] = "Initiative",
        ["card.initiative"] = "Initiative : {0}",
        ["sheet.before"] = "Vous jouez avant : {0}",
        ["sheet.before-none"] = "Vous jouez après toutes les familles : l'initiative se trouve sur les bottes, les capes et les anneaux.",
        ["sheet.left"] = "Points à répartir",
        ["sheet.col.stat"] = "Caractéristique",
        ["sheet.col.invested"] = "Investis",
        ["sheet.col.worn"] = "Équipement",
        ["sheet.col.total"] = "Total",
        ["sheet.col.bonus"] = "Effet",
        ["sheet.col.resist"] = "Résistance",
        ["sheet.bonus.hp"] = "+{0} PV",
        ["sheet.bonus.damage"] = "+{0} dégâts {1}",
        ["sheet.help"] = "10 points dans un élément = +2 dégâts dans cet élément, et rien d'autre · tapez une valeur, ou Maj+clic : 10 points d'un coup",
        ["sheet.reset"] = "Réinitialiser",
        ["sheet.min"] = "MIN",
        ["sheet.max"] = "MAX",
        ["char.Vitality"] = "Vitalité",
        ["char.Earth"] = "Terre",
        ["char.Fire"] = "Feu",
        ["char.Water"] = "Eau",
        ["char.Air"] = "Air",
        ["char.Vitality.help"] = "+1 PV par point",
        ["char.Earth.help"] = "+2 dégâts Terre par tranche de 10 points, et rien d'autre",
        ["char.Fire.help"] = "+2 dégâts Feu par tranche de 10 points, et rien d'autre",
        ["char.Water.help"] = "+2 dégâts Eau par tranche de 10 points, et rien d'autre",
        ["char.Air.help"] = "+2 dégâts Air par tranche de 10 points, et rien d'autre",
        ["xp.line"] = "Niveau {0} · {1} / {2} XP",
        ["xp.max"] = "Niveau {0} · {1} XP",
        ["result.xp"] = "+{0} XP",
        ["result.shards"] = "+{0} Éclats",
        ["end.title"] = "Combat terminé",
        ["end.results"] = "Résultats",
        ["end.stats"] = "Statistiques",
        ["end.winners"] = "Gagnants",
        ["end.losers"] = "Perdants",
        ["end.close"] = "Fermer",
        ["end.name"] = "Nom",
        ["end.level"] = "Niv.",
        ["end.xp"] = "XP",
        ["end.shards"] = "Éclats",
        ["end.loot"] = "Butin",
        ["end.dealt"] = "Dégâts infligés",
        ["end.taken"] = "Dégâts reçus",
        ["end.healing"] = "Soins",
        ["end.shielding"] = "Boucliers",
        ["end.kills"] = "Achevés",
        ["result.quest"] = "Quête terminée : {0}",
        ["result.level"] = "Niveau {0} !",
        ["result.offline"] = "Hors ligne : l'expérience n'est pas gardée.",
        ["result.refused"] = "Le serveur n'a pas compté ce combat : {0}",
        ["card.fighter"] = "{0} · {1}/{2} PV · {3} PA · {4} PM",
        ["card.shield"] = " · bouclier {0}",
        ["card.resist"] = "Résistances : {0}",
        ["card.resist-one"] = "{0} {1} %",
        ["card.effects"] = "Effets : {0}",
        ["card.passive"] = "{0} : {1}",
        ["card.turns"] = "{0} ({1} tour(s))",
        ["card.spell"] = "{0} · {1} PA · portée {2}-{3}",
        ["card.in-line"] = ", en ligne",
        ["card.no-sight"] = ", sans ligne de vue",
        ["card.per-turn"] = " · {0} fois par tour",
        ["card.cooldown"] = " · relance {0} tour(s)",
        ["card.damage"] = "{0} à {1} dégâts, {2}",
        ["card.crit"] = " ({0} à {1} en critique, {2} %)",
        ["card.area"] = "zone : {0} de {1}",
        ["area.Cross"] = "croix",
        ["area.Circle"] = "cercle",
        ["area.Line"] = "ligne",
        ["effect.heal"] = "soin {0} à {1}",
        ["effect.shield"] = "bouclier {0} ({1} tour(s))",
        ["effect.push"] = "poussée de {0}",
        ["effect.pull"] = "attirance de {0}",
        ["effect.summon"] = "invoque {0}",
        ["effect.leap"] = "bond sur la case",
        ["effect.swap"] = "échange de place",
        ["effect.glyph"] = "glyphe de {0} tour(s) : {1}",
        ["effect.glyph-damage"] = "{0} à {1} dégâts, {2}",
        ["card.glyph"] = "Glyphe de {0} ({1}) : {2}, {3} ; encore {4} tour(s)",
        ["glyph.Enemies"] = "frappe ses ennemis",
        ["glyph.Allies"] = "touche ses alliés",
        ["glyph.All"] = "touche tout le monde",
        ["log.glyph"] = "{0} : un glyphe frappe !",
        ["affects.Caster"] = " sur soi",
        ["affects.Allies"] = " aux alliés",
        ["affects.All"] = " à tous",
        ["affects.Enemies"] = "",
        ["affects.Attacker"] = " à l'attaquant",
        ["forecast.damage"] = "{0} : {1} à {2} dégâts",
        ["forecast.heal"] = "{0} : +{1} à {2} PV",
        ["forecast.kill"] = " (mortel)",
        ["forecast.crit"] = ", {0} à {1} en critique",
        ["log.move"] = "{0} se déplace de {1} case(s).",
        ["log.cast"] = "{0} lance {1}.",
        ["log.damage"] = "{0} perd {1} PV.",
        ["log.died"] = "{0} est vaincu(e).",
        ["log.heal"] = "{0} récupère {1} PV.",
        ["log.summon"] = "{0} invoque {1}.",
        ["log.shield"] = "{0} gagne un bouclier de {1}.",
        ["log.absorbed"] = "Le bouclier de {0} absorbe {1}.",
        ["log.pushed"] = "{0} est déplacé(e) de {1} case(s).",
        ["log.blocked"] = "{0} heurte un obstacle.",
        ["log.status"] = "{0} : {1} pendant {2} tour(s).",
        ["log.status-ended"] = "{0} : fin de l'effet {1}.",
        ["log.passive"] = "{0} : {1} !",
        ["log.leap"] = "{0} bondit.",
        ["log.swap"] = "{0} et {1} échangent leurs places.",
        ["log.marked"] = "{0} porte l'empreinte {1}.",
        ["log.reaction"] = "{0} : {1} !",
        ["log.reaction-status"] = "{0} : {1}, {2} pendant {3} tour(s).",
        ["forecast.reaction"] = " — {0} !",
        ["forecast.tree"] = "   arbre : {0}",
        ["forecast.trunk"] = "tronc +{0} %",
        ["forecast.share"] = "{0} {1} %",
        ["forecast.ceiling"] = " (plafonné à +{0} %)",
        ["stat.Poison"] = "poison {0}{1}",
        ["stat.Ap"] = "{0} PA",
        ["stat.Mp"] = "{0} PM",
        ["stat.Damage"] = "{0} % de dégâts",
        ["stat.Resistance"] = "{0} % de résistance{1}",
        ["stat.Range"] = "{0} PO",
        ["stat.Electrified"] = "électrisé, {0} % de chaque coup saute",
        ["element.Neutral"] = "neutre",
        ["element.Earth"] = "Terre",
        ["element.Fire"] = "Feu",
        ["element.Water"] = "Eau",
        ["element.Air"] = "Air",
        ["element.Lightning"] = "Foudre",
        ["card.arc"] = "Arc : {0} % des dégâts sautent sur l'ennemi le plus proche à {1} cases",
        ["log.arc"] = "L'arc saute de {0} à {1}.",
        ["log.support"] = "{0} soutient {1}.",
        ["log.echo"] = "L'écho de {0} frappe {1}.",
        ["glyph.remanence"] = "Rémanence",
        ["error.NotEnoughMp"] = "Pas assez de PM.",
        ["error.NotEnoughAp"] = "Pas assez de PA.",
        ["error.CastLimit"] = "Sort déjà lancé le nombre de fois permis ce tour-ci.",
        ["error.OutOfRange"] = "Hors de portée.",
        ["error.NotInLine"] = "Ce sort se lance en ligne droite.",
        ["error.NoLineOfSight"] = "Pas de ligne de vue.",
        ["error.Unreachable"] = "Case inaccessible.",
        ["error.Occupied"] = "Case occupée.",
        ["error.NotFloor"] = "On ne peut pas aller ou viser là.",
        ["error.OffBoard"] = "Hors du plateau.",
        ["error.UnknownSpell"] = "Sort inconnu.",
        ["error.FightOver"] = "Le combat est fini.",
        ["error.TooManySummons"] = "Déjà autant d'invocations que permis.",
        ["error.Cooldown"] = "Sort encore en relance.",
        ["error.TargetNotFree"] = "Il faut une case libre pour atterrir.",
        ["error.NoValidTarget"] = "Ce sort vise un autre combattant, du bon camp.",
        ["error.SupportSpent"] = "Plus de soutien ce tour : un sort sur un allié attendra le prochain.",
        ["lobby.title"] = "Osmose",
        ["lobby.name"] = "Nom de compte",
        ["lobby.password"] = "Mot de passe",
        ["lobby.sign-in"] = "Se connecter",
        ["lobby.sign-up"] = "Créer le compte",
        ["lobby.remember-name"] = "Se souvenir du nom de compte",
        ["lobby.remember-password"] = "Se souvenir du mot de passe",
        ["lobby.server"] = "Serveur : {0}",
        ["lobby.characters"] = "Vos personnages ({0}/{1})",
        ["lobby.none"] = "Aucun personnage : créez-en un.",
        ["lobby.character-name"] = "Nom du personnage",
        ["lobby.create"] = "Créer",
        ["lobby.play"] = "Jouer",
        ["lobby.delete"] = "Supprimer",
        ["lobby.sign-out"] = "Se déconnecter",
        ["lobby.wait"] = "Un instant…",
        ["lobby.back"] = "Personnages",
        ["lobby.choose-server"] = "Choisissez d'abord un serveur.",
        ["lobby.servers"] = "Choisissez un serveur",
        ["lobby.server-characters"] = "{0} personnage(s)",
        ["lobby.launcher"] = "Connectez-vous, ou ouvrez Osmose depuis son launcher.",
        ["lobby.level"] = "Niveau {0}",
        ["lobby.new"] = "Créer un personnage",
        ["lobby.change-server"] = "Changer de serveur",
        ["lobby.confirm-delete"] = "Supprimer {0} pour toujours ?",
        ["lobby.cancel"] = "Annuler",
        ["lobby.close"] = "Fermer",
        ["look.female"] = "Femme {0}",
        ["lobby.colour"] = "Tenue",
        ["lobby.class"] = "Classe",
        ["lobby.spells"] = "Ses sorts, du niveau 1 au niveau 100",
        ["lobby.hair"] = "Cheveux",
        ["lobby.female"] = "Femme",
        ["lobby.male"] = "Homme",
        ["lobby.skin"] = "Peau",
        ["lobby.height"] = "Taille",
        ["lobby.build"] = "Carrure",
        ["town.help"] = "Clic : marcher · clic sur un habitant : lui parler · clic droit sur des monstres : attaquer · bord de la carte : changer de carte · molette : zoom · Échap : menu",
        ["bar.Characteristics"] = "Caractéristiques",
        ["bar.Spells"] = "Sorts",
        ["bar.Inventory"] = "Inventaire",
        ["bar.Map"] = "Carte",
        ["bar.Journal"] = "Journal",
        ["bar.Jobs"] = "Métiers",
        ["bar.Contacts"] = "Contacts",
        ["bar.Guild"] = "Guilde",
        ["bar.Bestiary"] = "Bestiaire",
        ["jobs.title"] = "Métiers",
        ["jobs.gathering"] = "Récolte",
        ["jobs.making"] = "Fabrication",
        ["jobs.level"] = "niv. {0}",
        ["jobs.xp"] = "{0} / {1}",
        ["jobs.recipes"] = "Recettes",
        ["jobs.resources"] = "Ressources",
        ["jobs.search"] = "Rechercher…",
        ["jobs.from"] = "Niveau",
        ["jobs.to"] = "à",
        ["jobs.all-kinds"] = "Toutes catégories",
        ["jobs.order.Highest"] = "Niveau le plus élevé",
        ["jobs.order.Lowest"] = "Niveau le plus bas",
        ["jobs.order.Name"] = "Nom",
        ["jobs.possible"] = "Recettes possibles seulement",
        ["jobs.none"] = "Aucune recette.",
        ["jobs.recipe"] = "{0} · niv. {1} · {2} XP",
        ["jobs.soon"] = "bientôt",
        ["jobs.near"] = "{0}, près de l'entrée",
        ["jobs.far"] = "{0}, au fond",
        ["jobs.deep"] = "{0}, en eau profonde",
        ["jobs.made"] = "fabriqué",
        ["jobs.owned"] = "vous en avez {0}",
        ["jobs.look"] = "Consultation : on fabrique devant l'atelier du métier.",
        ["quest.Talk"] = "Parler à {0}",
        ["quest.Defeat"] = "Vaincre : {0} ({1}/{2})",
        ["quest.Bring"] = "Rapporter : {0} ({1}/{2})",
        ["quest.Dungeon"] = "Finir {0}",
        ["quest.DungeonRoom"] = "Gagner la salle {3} de {0}",
        ["quest.Interact"] = "Examiner : {0}",
        ["quest.Win"] = "Gagner : {0}",
        ["quest.Wear"] = "Porter : {0}",
        ["quest.found"] = "Quête « {0} » : un indice de plus.",
        ["journal.where"] = "Où : {0}",
        ["prop.seen"] = "{0} : {1}",
        ["journal.follow"] = "Suivre",
        ["journal.followed"] = "Suivie",
        ["journal.inventory"] = "l'inventaire (I)",
        ["quest.offer"] = "Quête : {0}",
        ["quest.handin"] = "Rendre la quête : {0}",
        ["quest.accept"] = "J'accepte.",
        ["quest.later"] = "Plus tard.",
        ["quest.give"] = "Avec plaisir.",
        ["quest.taken"] = "Quête acceptée : {0}. Le journal (Q) la suit.",
        ["quest.done"] = "Quête terminée : {0} · +{1} XP · +{2} Éclats",
        ["quest.talked"] = "Quête « {0} » : c'est noté.",
        ["journal.title"] = "Journal des quêtes",
        ["journal.active"] = "En cours",
        ["journal.done"] = "Terminées",
        ["journal.village"] = "Clairval",
        ["journal.empty"] = "Aucune quête pour l'instant : parlez aux habitants.",
        ["journal.giver"] = "Donnée par {0}",
        ["journal.ready"] = "À rendre à {0}",
        ["journal.hidden"] = "Masquées",
        ["journal.tab"] = "{0} ({1})",
        ["journal.search"] = "Rechercher une quête…",
        ["journal.levels"] = "Niveaux",
        ["journal.to"] = "à",
        ["journal.level"] = "Niveau {0}",
        ["journal.nomatch"] = "Aucune quête ne correspond à la recherche ou aux filtres.",
        ["journal.hide"] = "Masquer",
        ["journal.show"] = "Afficher",
        ["journal.objectives"] = "Objectifs",
        ["journal.rewards"] = "Récompenses",
        ["journal.xp"] = "{0} XP",
        ["journal.shards"] = "{0} Éclats",
        ["journal.item"] = "{0} ×{1}",
        ["journal.finished"] = "Terminée",
        ["item.Equip"] = "Équiper",
        ["item.TakeOff"] = "Retirer",
        ["item.Use"] = "Utiliser",
        ["item.Recipes"] = "Recettes",
        ["item.Bestiary"] = "Bestiaire",
        ["item.Set"] = "Panoplie",
        ["item.Share"] = "Partager dans le chat",
        ["item.hint"] = "Clic droit : plus d'options",
        ["item.makes"] = "{0}, niveau {1} : {2}",
        ["item.needs"] = "{0} ({1}, niveau {2}) : en demande {3}",
        ["item.drop"] = "{0}, niveau {1} ({2}) : {3} %",
        ["item.drop-quest"] = "{0}, niveau {1} ({2}) : {3} %, pendant sa quête",
        ["item.piece"] = "{0} · {1} · niveau {2}",
        ["item.none"] = "Rien à montrer pour cet objet.",
        ["item.card"] = "Fiche",
        ["questkind.Tutorial"] = "Premiers pas",
        ["questkind.Main"] = "Quête principale",
        ["questkind.Side"] = "Quête secondaire",
        ["questkind.Trade"] = "Quête de métier",
        ["questkind.Dungeon"] = "Quête de donjon",
        ["bar.menu"] = "Menu",
        ["bar.title"] = "MENUS",
        ["bar.tip"] = "{0} ({1})",
        ["menu.title"] = "Menu",
        ["menu.sound"] = "Son",
        ["menu.master"] = "Volume général",
        ["menu.music"] = "Musique",
        ["menu.effects"] = "Effets",
        ["menu.mute"] = "Couper le son",
        ["menu.Resume"] = "Revenir au jeu",
        ["menu.Servers"] = "Changer de serveur",
        ["menu.Characters"] = "Changer de personnage",
        ["menu.Quit"] = "Quitter",
        ["menu.key"] = "Échap",
        ["group.member"] = "{0} (niv. {1})",
        ["group.attack"] = "Attaquer",
        ["group.horde"] = "{0} ×{1} (niv. {2} à {3})",
        ["map.button"] = "Carte",
        ["map.title"] = "Carte du monde",
        ["chat.map"] = "Proximité",
        ["chat.line"] = "[{0}] {1} : {2}",
        ["chat.refused"] = "Non envoyé : {0}",
        ["chat.placeholder"] = "Entrée pour parler (/g pour le groupe, /gu pour la guilde, /w Nom en privé), Échap pour quitter",
        ["chat.party"] = "Groupe",
        ["bestiary.title"] = "Bestiaire",
        ["bestiary.unknown"] = "???",
        ["bestiary.fight-it"] = "Jamais combattu : une silhouette.",
        ["bestiary.met"] = "{0}, niveau {1}, {2}",
        ["bestiary.wins"] = "Victoires : {0}",
        ["bestiary.hp"] = "Points de vie : {0}",
        ["bestiary.resistances"] = "Résistances : {0}",
        ["bestiary.none"] = "aucune",
        ["bestiary.weakness"] = "Faiblesse : {0}",
        ["bestiary.no-weakness"] = "Aucune faiblesse",
        ["bestiary.spells"] = "Sorts : {0}",
        ["bestiary.lesson"] = "Leçon de la famille, {0} : {1}",
        ["bestiary.loot"] = "Butin : {0}",
        ["bestiary.next"] = "Encore {0} victoire(s) pour en savoir plus.",
        ["bestiary.known"] = "{0} monstres sur {1} connus",
        ["role.Melee"] = "Mêlée",
        ["role.Ranged"] = "Distance",
        ["role.Tank"] = "Rempart",
        ["role.Support"] = "Soutien",
        ["role.Boss"] = "Chef",
        ["rarity.Common"] = "commun",
        ["rarity.Rare"] = "rare",
        ["rarity.VeryRare"] = "très rare",
        ["contracts.window"] = "Tableau d'avis",
        ["contracts.tab"] = "Contrats",
        ["contracts.handIn"] = "Rendre",
        ["contracts.swapFree"] = "Changer cet avis (gratuit)",
        ["contracts.swapPaid"] = "Changer cet avis ({0} Éclats)",
        ["contracts.group"] = "Groupe",
        ["contracts.pending"] = "{0} avis en attente",
        ["contracts.empty"] = "Aucun avis pour l'instant : revenez demain.",
        ["contracts.done"] = "Contrat rempli : +{0} XP, +{1} Éclats.",
        ["contracts.renewal"] = "Le tableau se renouvelle chaque jour à l'aube.",
        ["contracts.progress"] = "{0} / {1}",
        ["contracts.reward"] = "+{0} XP, +{1} Éclats",
        ["contracts.needs-server"] = "Les contrats ne fonctionnent qu'en ligne.",
        ["contract.hunt.title"] = "Chasse aux {1}",
        ["contract.hunt.text"] = "Les {1} s'enhardissent et rôdent près des chemins. Prime à qui en vaincra {0}. Secteur : {2}.",
        ["contract.gather.title"] = "Commande : {0} {1}",
        ["contract.gather.text"] = "Les réserves baissent et les routes sont vides. Cherche {0} {1}, au plus vite. Payé comptant, sans marchander.",
        ["contract.craft.title"] = "Sur commande : {1}",
        ["contract.craft.text"] = "Cherche une main habile pour fabriquer ceci : {1}. Travail vérifié avant paiement, comme il se doit.",
        ["contract.dungeon.title"] = "Reconnaissance : {1}",
        ["contract.dungeon.text"] = "Ce qu'il y a après l'entrée, plus personne n'ose le dire. Franchir la deuxième salle, puis revenir raconter. Lieu : {1}.",
        ["contract.horde.title"] = "Horde en vue : {1}",
        ["contract.horde.text"] = "Les {1} font bande, six ou plus à la fois. Seul, c'est folie : on y va à plusieurs, ou on n'y va pas.",
        ["contract.boss.title"] = "Prime au chef : {1}",
        ["contract.boss.text"] = "Tout ce qui sort de là obéit à quelque chose, tout au fond. Il faut l'abattre, et pas seul. Lieu : {1}.",
        ["waystone.title"] = "Pierres de passage",
        ["waystone.travel"] = "Voyager ({0} Éclats)",
        ["waystone.here"] = "Vous y êtes",
        ["waystone.unknown"] = "{0} · pas encore découverte",
        ["waystone.known"] = "{0}",
        ["waystone.home"] = "{0} · votre foyer",
        ["waystone.make-home"] = "En faire mon foyer",
        ["waystone.shards"] = "Éclats : {0}",
        ["waystone.arrived"] = "Vous voici : {0}.",
        ["waystone.recalled"] = "Le parchemin vous ramène : {0}.",
        ["waystone.needs-server"] = "Les pierres de passage demandent le serveur.",
        ["chat.whisper"] = "Privé",
        ["chat.guild"] = "Guilde",
        ["chat.whisper-line"] = "[Privé] {0} → {1} : {2}",
        ["contacts.title"] = "Contacts",
        ["contacts.friends"] = "Amis ({0})",
        ["contacts.ignored"] = "Ignorés ({0})",
        ["contacts.online"] = "{0} (niv. {1}) · {2}",
        ["contacts.offline"] = "{0} (niv. {1}) · hors ligne",
        ["contacts.name"] = "Nom d'un personnage",
        ["contacts.add"] = "Ajouter un ami",
        ["contacts.ignore"] = "Ignorer",
        ["contacts.invite"] = "Inviter",
        ["contacts.whisper"] = "Écrire",
        ["contacts.remove"] = "Retirer {0}",
        ["contacts.notify"] = "Prévenir quand un ami se connecte",
        ["contacts.came"] = "{0} est en jeu : {1}.",
        ["contacts.none"] = "Personne pour l'instant.",
        ["contacts.room"] = "Contacts : {0}/{1}",
        ["contacts.col.status"] = "Statut",
        ["contacts.col.name"] = "Personnage",
        ["contacts.col.level"] = "Niveau",
        ["contacts.col.class"] = "Classe",
        ["contacts.col.zone"] = "Zone",
        ["contacts.in-play"] = "EN JEU ({0})",
        ["contacts.away"] = "HORS LIGNE ({0})",
        ["contacts.offline-zone"] = "Hors ligne",
        ["contacts.needs-server"] = "Les contacts demandent le serveur.",
        ["guild.title"] = "Guilde",
        ["guild.none"] = "Vous n'êtes dans aucune guilde. Edmée, la greffière, sur la place de Clairval, en inscrit une nouvelle pour {0} Éclats ; un meneur ou un officier peut aussi vous inviter.",
        ["guild.room"] = "Membres : {0}/{1}",
        ["guild.rank.Leader"] = "Meneur",
        ["guild.rank.Officer"] = "Officier",
        ["guild.rank.Member"] = "Membre",
        ["guild.col.name"] = "Personnage",
        ["guild.col.rank"] = "Rang",
        ["guild.col.level"] = "Niveau",
        ["guild.col.class"] = "Classe",
        ["guild.col.where"] = "Où / vu",
        ["guild.seen.days"] = "il y a {0} j",
        ["guild.seen.hours"] = "il y a {0} h",
        ["guild.seen.minutes"] = "il y a {0} min",
        ["guild.seen.never"] = "jamais vu",
        ["guild.online"] = "{0} (niv. {1}, {2}) · {3}",
        ["guild.offline"] = "{0} (niv. {1}, {2}) · {3}",
        ["guild.name"] = "Nom d'un personnage en jeu",
        ["guild.invite"] = "Inviter",
        ["guild.invite-into"] = "Inviter dans {0}",
        ["guild.leave"] = "Quitter la guilde",
        ["guild.promote"] = "Nommer {0} officier",
        ["guild.demote"] = "Rendre {0} simple membre",
        ["guild.kick"] = "Exclure {0}",
        ["guild.hand-over"] = "Transmettre le titre de meneur à {0}",
        ["guild.invited"] = "{0} vous invite dans la guilde {1}.",
        ["guild.join"] = "Rejoindre",
        ["guild.decline"] = "Refuser",
        ["guild.found.title"] = "Fonder une guilde pour {0} Éclats",
        ["guild.found.name"] = "Nom de la guilde (3 à 20 caractères)",
        ["guild.found"] = "Fonder",
        ["guild.cancel"] = "Annuler",
        ["guild.founded"] = "La guilde {0} est fondée.",
        ["guild.joined"] = "Vous voilà dans la guilde {0}.",
        ["guild.left"] = "Vous n'êtes plus dans une guilde.",
        ["guild.needs-server"] = "Les guildes ne fonctionnent qu'en ligne.",
        ["battle.opened"] = "{0} attaque {1} : le rejoindre ?",
        ["battle.join"] = "Rejoindre",
        ["battle.join-line"] = "{0} attaque : rejoindre ({1} s)",
        ["battle.absent"] = "absent",
        ["battle.seconds"] = "{0} s",
        ["party.kick"] = "Exclure {0} du groupe",
        ["battle.waiting"] = "Combat ouvert : vos alliés ont 15 secondes pour vous rejoindre.",
        ["battle.go"] = "Lancer",
        ["battle.wait"] = "Attendre",
        ["party.invite"] = "Inviter dans le groupe",
        ["party.trade"] = "Proposer un échange",
        ["party.invited"] = "{0} vous invite dans son groupe.",
        ["party.join"] = "Rejoindre",
        ["party.decline"] = "Refuser",
        ["party.leave"] = "Quitter le groupe",
        ["party.title"] = "Groupe ({0}/{1})",
        ["party.member"] = "{0} · niv. {1}",
        ["party.leader"] = "{0} · niv. {1} · chef",
        ["party.offline"] = "{0} · hors ligne",
        ["party.left"] = "Vous n'êtes plus dans un groupe.",
        ["party.sent"] = "Invitation envoyée à {0}.",
        ["trade.asked"] = "{0} vous propose un échange.",
        ["trade.open"] = "Échanger",
        ["trade.title"] = "Échange avec {0}",
        ["trade.mine"] = "Votre offre",
        ["trade.theirs"] = "Offre de {0}",
        ["trade.ready"] = "Valider",
        ["trade.waiting"] = "Validé : en attente de {0}",
        ["trade.they-ready"] = "{0} a validé.",
        ["trade.cancel"] = "Annuler",
        ["trade.done"] = "Échange fait.",
        ["trade.cancelled"] = "Échange annulé.",
        ["trade.sent"] = "Échange proposé à {0}.",
        ["trade.hint"] = "Un clic sur un objet de votre inventaire l'ajoute, un clic sur votre offre le retire.",
        ["trade.nothing"] = "Rien pour l'instant.",
        ["chat.offline"] = "Le chat demande le serveur.",
        ["chat.closed"] = "Le chat est fermé : changez de carte pour le rouvrir.",
        ["chat.open"] = "Chat de proximité : les joueurs de cette carte vous lisent.",
        ["map.dungeon"] = "Donjon : {0}",
        ["map.grid"] = "Quadrillage",
        ["map.level"] = "niveau {0}",
        ["map.here"] = "vous êtes ici",
        ["minimap.tip"] = "Mini-carte (N)",
        ["minimap.title"] = "Mini-carte",
        ["minimap.you"] = "Vous",
        ["minimap.goal"] = "Objectif de la quête suivie",
        ["minimap.spot"] = "{0} · {1} niv. {2}",
        ["minimap.in"] = "Rapprocher",
        ["minimap.out"] = "Éloigner",
        ["minimap.size"] = "Taille",
        ["town.back"] = "Retour en ville",
        ["craft.make"] = "Fabriquer",
        ["craft.make-all"] = "Tout ({0})",
        ["craft.made"] = "Fabriqué : {0}.",
        ["craft.offline"] = "Les métiers demandent le serveur.",
        ["craft.close"] = "Fermer",
        ["shop.title"] = "L'étal de Rose · {0} Éclats",
        ["gather.doing"] = "{0} : récolte de {1}…",
        ["gather.done"] = "+{0} {1} (+{2} XP de {3})",
        ["gather.offline"] = "La récolte demande le serveur.",
        ["end.life"] = "Vie : {0} / {1}",
        ["life.line"] = "Vie : {0} / {1}",
        ["life.rate"] = "+{0} PV/s, ×2 au village ; rien en combat.",
        ["life.low"] = "Vous n'avez que {0} PV sur {1}. Attaquer quand même ?",
        ["life.attack"] = "Attaquer",
        ["life.later"] = "Plus tard",
        ["life.eaten"] = "{0} mangé : {1} PV.",
        ["gather.growing"] = "Ce point repousse.",
        ["job.levelup"] = "{0} : niveau {1} !",
        ["inventory.shards"] = "{0} Éclats",
        ["shop.buy"] = "Acheter",
        ["shop.sell"] = "Vendre",
        ["shop.row"] = "{0} · {1} Éclats",
        ["shop.row-owned"] = "{0} · {1} Éclats · vous en avez {2}",
        ["shop.poor"] = "Pas assez d'Éclats.",
        ["shop.worn"] = "Porté.",
        ["shop.bought"] = "Acheté : {0}.",
        ["shop.sold"] = "Vendu : {0}.",
        ["shop.nothing"] = "Rien que Rose achète.",
        ["points.reset-cost"] = "Reprendre des points coûtera {0} Éclats.",
        ["dungeon.enter"] = "Entrer dans {0} ?",
        ["dungeon.key-used"] = "Cela utilise une clef : {0}.",
        ["dungeon.keyring"] = "Le trousseau d'administrateur ouvre la porte.",
        ["dungeon.no-key"] = "{0} : il faut une clef pour entrer ({1}).",
        ["dungeon.resume"] = "Retourner dans {0}, salle {1} sur 3 ?",
        ["dungeon.go-in"] = "Entrer",
        ["dungeon.stay"] = "Rester dehors",
        ["dungeon.room"] = "{0}, salle {1} sur 3",
        ["dungeon.done"] = "Donjon terminé !",
        ["dungeon.out"] = "Vous voilà dehors.",
        ["hall.shut"] = "La porte du fond reste fermée : battez d'abord le groupe de la salle.",
        ["way.shut"] = "La porte de l'Est est barrée de grosses planches. Personne à Clairval ne se souvient de l'avoir vue ouverte.",
        ["hall.rested"] = "L'eau de la fontaine referme vos blessures : {0} points de vie.",
        ["hall.hp"] = "Points de vie : {0} / {1}. La fontaine soigne.",
        ["town.exit"] = "Vers : {0}",
        ["town.saved"] = "Position enregistrée sur le serveur.",
        ["lobby.colour-n"] = "Couleur {0}",
        ["class"] = "{0} {1} PV · {2} PA · {3} PM · {4}",
        ["class.level"] = "Niv. {0} : {1}",
        ["look.male"] = "Homme {0}",
        ["lobby.fill-in"] = "Indiquez un nom et un mot de passe.",
        ["lobby.password-short"] = "Le mot de passe compte au moins 10 caractères.",
        ["lobby.account-invalid"] = "Nom de compte : 3 à 20 lettres sans accent, chiffres, « - » ou « _ ».",
        ["lobby.account-taken"] = "Ce nom de compte est déjà pris.",
        ["lobby.wrong-password"] = "Nom inconnu ou mot de passe faux.",
        ["lobby.name-invalid"] = "Nom du personnage : 3 à 20 lettres et rien d'autre, la première en majuscule.",
        ["lobby.name-taken"] = "Ce nom de personnage est déjà pris sur le serveur.",
        ["lobby.class-invalid"] = "Choisissez une classe, une apparence et une couleur.",
        ["lobby.too-many-characters"] = "Cinq personnages au plus par compte.",
        ["lobby.session-expired"] = "Session expirée : reconnectez-vous.",
        ["lobby.too-many-tries"] = "Trop d'essais : attendez une minute.",
        ["lobby.server-error"] = "Le serveur a rencontré une erreur.",
        ["lobby.unreachable"] = "Serveur injoignable : réessayez dans un moment.",
    };

    internal static readonly IReadOnlyDictionary<string, string> En = new Dictionary<string, string>
    {
        ["round"] = "Round {0}",
        ["your-turn"] = "Your turn",
        ["their-turn"] = "{0} is playing",
        ["hp"] = "HP",
        ["ap"] = "AP",
        ["mp"] = "MP",
        ["end-turn"] = "End turn",
        ["item.food"] = "Gives back {0} HP, out of a fight.",
        ["victory"] = "Victory!",
        ["defeat"] = "Defeat…",
        ["draw"] = "Draw",
        ["help"] = "Click: move · 1 to 6 and Ctrl+1 to 6: choose a spell · Esc: cancel, then menu · Space: end turn",
        ["spell"] = "{0} ({1} AP, range {2}-{3})",
        ["inventory.button"] = "Inventory",
        ["inventory.title"] = "Inventory and equipment",
        ["inventory.search"] = "Search for an item",
        ["inventory.hint"] = "Hover over an item to see its card; a click puts it on, a click on a slot takes it off.",
        ["inventory.empty"] = "Nothing here.",
        ["inventory.free"] = "{0} free",
        ["inventory.saved"] = "Equipment saved.",
        ["inventory.level"] = "level {0}",
        ["inventory.set"] = "{0}: {1}",
        ["inventory.pieces"] = "{0} pieces {1}",
        ["result.loot"] = "Loot: {0}",
        ["page.All"] = "All",
        ["page.Equipment"] = "Equipment",
        ["page.Consumable"] = "Consumables",
        ["page.Resource"] = "Resources",
        ["page.Quest"] = "Quest",
        ["kind.Consumable"] = "consumable",
        ["kind.Resource"] = "resource",
        ["kind.Quest"] = "quest item",
        ["slot.Ring1"] = "Ring 1",
        ["slot.Ring2"] = "Ring 2",
        ["slot.Amulet"] = "Amulet",
        ["slot.Belt"] = "Belt",
        ["slot.Cape"] = "Cape",
        ["slot.Hat"] = "Hat",
        ["slot.Chest"] = "Breastplate",
        ["slot.Shoulders"] = "Pauldrons",
        ["slot.OneHanded"] = "One-handed weapon",
        ["slot.TwoHanded"] = "Two-handed weapon",
        ["slot.Shield"] = "Shield",
        ["slot.Pet"] = "Pet",
        ["slot.Mount"] = "Mount",
        ["slot.Boots"] = "Boots",
        ["slot.Ring"] = "Ring",
        ["points.button"] = "Character",
        ["points.title"] = "Characteristics and spells",
        ["points.characteristics"] = "Characteristics: {0} point(s) to spend",
        ["points.spells"] = "Spells: {0} spell point(s)",
        ["points.talents"] = "Talents",
        ["talents.left"] = "Talents: {0} point(s) to spend",
        ["talents.closed"] = "The talent tree opens at level {0}.",
        ["talents.hint"] = "A click takes a talent or gives it back (with those that depend on it). First a door: a pair (its reaction) or a Heart (Pure element); then a style, one floor after the other, its key at level 45, one key a style; two pairs at most, with no element in common; one big switch only (the Conversion). Lightning comes through Electrifying, then a Transmutation (of Fire or of Air). Giving a talent back costs 10 × the level in Éclats after level 40, in a village.",
        ["talents.tile"] = "{0} · {1}, {2}, {3} point(s)",
        ["talents.branch.Fork"] = "Pairs",
        ["talents.branch.Pure"] = "Pure element",
        ["talents.branch.Braids"] = "Braid: Lightning",
        ["talents.branch.Alchemy"] = "Alchemy",
        ["talents.branch.Melee"] = "Melee",
        ["talents.branch.Distance"] = "Distance",
        ["talents.branch.Terrain"] = "Terrain",
        ["talents.branch.Tempo"] = "Tempo",
        ["talents.branch.Support"] = "Support",
        ["talents.tier.First"] = "first floor",
        ["talents.tier.Second"] = "second floor",
        ["talents.tier.Key"] = "key",
        ["talents.trunk"] = "Trunk, free: {0}",
        ["talents.trunk.Damage"] = "+{0}% damage (lv {1})",
        ["talents.trunk.Resistance"] = "+{0}% resistances (lv {1})",
        ["talents.trunk.Hp"] = "+{0}% HP (lv {1})",
        ["talents.trunk.unlit"] = "{0}, to come",
        ["talents.taken.Earth"] = "Earth is already taken by {0}",
        ["talents.taken.Fire"] = "Fire is already taken by {0}",
        ["talents.taken.Water"] = "Water is already taken by {0}",
        ["talents.taken.Air"] = "Air is already taken by {0}",
        ["talents.replaced"] = "The Conversion replaces {0}: its point comes back.",
        ["talents.losing"] = "Giving it back also gives back: {0}",
        ["talents.why.Opens"] = "The tree opens at level {0}.",
        ["talents.why.Level"] = "Opens at level {0}.",
        ["talents.why.Door"] = "Asks for a door first: a pair or a Heart.",
        ["talents.why.Parent"] = "Asks for {0} first.",
        ["talents.why.or"] = " or ",
        ["talents.why.Excludes"] = "Not taken with {0}.",
        ["talents.why.Pair"] = "Pair refused: {0}.",
        ["talents.why.KeyPerStyle"] = "One key a style: {0} is taken already.",
        ["talents.why.Switch"] = "One big switch only: {0} is taken already.",
        ["talents.why.Elements"] = "Two elements out of the four at most.",
        ["talents.why.Points"] = "Not enough points: this talent costs {0}.",
        ["tree.title"] = "Talent tree · level {0}",
        ["tree.open"] = "Open the tree (T)",
        ["tree.left"] = "{0} of {1} point(s) left to spend",
        ["tree.closed"] = "The tree opens at level {0}: its trunk lights up on its own.",
        ["tree.elements"] = "Elements beyond the base: {0} / {1}",
        ["tree.switch"] = "Great shift: {0}",
        ["tree.switch.none"] = "none",
        ["tree.styles"] = "Styles: one key per style, at level 45",
        ["tree.fork"] = "The fork: a pair (its reaction) or a Heart (Pure element)",
        ["tree.trunk"] = "Trunk, free",
        ["tree.trunk.Damage"] = "+{0}% damage",
        ["tree.trunk.Resistance"] = "+{0}% resist.",
        ["tree.trunk.Hp"] = "+{0}% HP",
        ["tree.trunk.detail"] = "The trunk lights up on its own, without spending a point: {0}.",
        ["tree.fold"] = "{0} · {1}",
        ["tree.fold.detail"] = "{0}: click to expand this style.",
        ["tree.legend"] = "Legend",
        ["tree.plan"] = "Plan at level {0}",
        ["tree.plan.leave"] = "Back to my tree",
        ["tree.plan.on"] = "Planning at level {0}: nodes ringed in dotted pistachio are only a plan; nothing is saved.",
        ["tree.path"] = "Path: {0} ({1} point(s))",
        ["tree.path.none"] = "No path leads there from your nodes.",
        ["tree.suggested"] = "Suggested build…",
        ["tree.suggested.loaded"] = "{0} loaded as a draft: save to keep it.",
        ["tree.code.copy"] = "Copy",
        ["tree.code.copied"] = "Code copied: {0}",
        ["tree.code.load"] = "Load",
        ["tree.code.placeholder"] = "Build code",
        ["tree.code.Unreadable"] = "This is not a build code.",
        ["tree.code.OldVersion"] = "This code comes from an older version of the tree ({0}): it no longer loads.",
        ["tree.code.Level"] = "This build needs level {0}.",
        ["tree.code.loaded"] = "Build loaded as a draft: save to keep it.",
        ["tree.badge"] = "{0} talent point(s) to spend",
        ["spell.origin"] = "Given by {0}",
        ["spell.replaces"] = "Takes the place of {0}",
        ["spell.modified"] = "Changed by the tree: {0}",
        ["spell.new"] = "new",
        ["deck.swap"] = "{0} takes the place of {1} in your deck.",
        ["deck.left"] = "{0} leaves your deck until the save.",
        ["tree.before-after"] = "On a typical hit (2 cells away, standing still, first spell of 3 AP): {0}% → {1}%",
        ["fight.lights"] = "Talents this turn: {0}",
        ["fight.light.on"] = "{0} lit ({1}%)",
        ["fight.light.off"] = "{0} off ({1}%)",
        ["tree.layer.Foundations"] = "Foundations",
        ["tree.layer.Affinities"] = "Pairs",
        ["tree.layer.Pure"] = "Pure element",
        ["tree.layer.Fusion"] = "Braid",
        ["tree.layer.Styles"] = "Styles",
        ["tree.banner.Foundations"] = "The trunk lights up on its own as you level: nothing to choose, everything to gain.",
        ["tree.banner.Affinities"] = "A pair teams up two elements: strike one on the other's mark to set off its reaction. Two pairs at most, sharing no element.",
        ["tree.banner.Pure"] = "One element pushed hard, the others a little less: a Heart opens the styles without touching your elements.",
        ["tree.banner.Fusion"] = "A pair's two elements run together without blending: first a Transmutation, then the Conversion.",
        ["tree.banner.Styles"] = "A way of fighting, open to every door: climb it one floor at a time.",
        ["tree.banner.Melee"] = "Up close: harder hits at short range, at the cost of reach.",
        ["tree.banner.Distance"] = "From afar: more range, less bite up close.",
        ["tree.banner.Support"] = "For the others: your spells help an ally instead of hitting them. Made for group play.",
        ["tree.banner.Terrain"] = "The ground: glyphs, obstacles, and every step that counts.",
        ["tree.banner.Tempo"] = "The rhythm: AP, cooldowns and room in the deck.",
        ["tree.banner.Alchemy"] = "The reactions: marks that last, are shared or spread. Needs a pair.",
        ["tree.banner.Key"] = "One key per style, from level 45: the strongest node, and it costs 2 points.",
        ["tree.banner.Choice"] = "Pick one: only one framed node can be taken.",
        ["tree.equation"] = "{0} = {1} + {2}",
        ["tree.counterpart"] = "In return: {0}",
        ["tree.title.heart.Earth"] = "Heart of Earth",
        ["tree.title.heart.Fire"] = "Heart of Fire",
        ["tree.title.heart.Water"] = "Heart of Water",
        ["tree.title.heart.Air"] = "Heart of Air",
        ["tree.term.line"] = "{0}: {1}",
        ["tree.title.and"] = "{0} and {1}",
        ["tree.title.converted"] = "Conversion: {0}",
        ["tree.title.none"] = "No talent",
        ["tree.summary.counterparts"] = "Counterparts: {0}",
        ["tree.summary.none"] = "No counterpart.",
        ["tree.summary.best"] = "At best, +{0}% on a hit.",
        ["tree.summary.ceiling"] = "At best, +{0}% on a hit: anything above +{1}% is lost.",
        ["tree.advice"] = "Tip: {0} and {1}, your highest characteristics, point to {2}.",
        ["tree.helps"] = "With your elements: {0}",
        ["tree.help.Earth"] = "Earth, a shield of 60% of the damage",
        ["tree.help.Fire"] = "Fire, a heal of 40% of the damage (15% of its HP at most)",
        ["tree.help.Water"] = "Water, +5% resistance per AP (20 at most)",
        ["tree.help.Air"] = "Air, 1 MP every 2 AP (2 at most) and 1 range",
        ["tree.group"] = "Made for group play.",
        ["tree.help.button"] = "?",
        ["tree.glossary"] = "Glossary",
        ["tree.term.mark"] = "mark",
        ["tree.term.mark.def"] = "a direct hit of a base element leaves its mark on the enemy until the end of your next turn",
        ["tree.term.reaction"] = "reaction",
        ["tree.term.reaction.def"] = "hitting a mark with the pair's other element sets its state off",
        ["tree.term.door"] = "door",
        ["tree.term.door.def"] = "a pair or a Heart: one is needed before a style's first floor",
        ["tree.term.key"] = "key",
        ["tree.term.key.def"] = "a style's last floor, at level 45, one per style",
        ["tree.term.switch"] = "great shift",
        ["tree.term.switch.def"] = "a talent that changes your elements for good, like the Conversion; one in the whole tree",
        ["tree.term.beyond"] = "beyond the base",
        ["tree.term.beyond.def"] = "an element other than Earth, Fire, Water and Air, like Lightning; two at most",
        ["tree.tuto.1"] = "The trunk, at the bottom, lights up by itself with the level.",
        ["tree.tuto.2"] = "First choose a door: a pair (two elements and their reaction) or a Heart (one element).",
        ["tree.tuto.3"] = "Then climb a style, one floor at a time. Hover over a node for every detail; the legend is behind \"?\".",
        ["tree.tuto.next"] = "Next",
        ["tree.tuto.done"] = "Got it",
        ["tree.choice.transmute"] = "Transmutation (pick one)",
        ["tree.choice.heart"] = "Pure element: one Heart",
        ["tree.state.Taken"] = "taken",
        ["tree.state.Open"] = "available",
        ["tree.state.Locked"] = "locked (door, parent, points)",
        ["tree.state.Excluded"] = "excluded (reason on hover)",
        ["tree.state.Level"] = "level too low",
        ["tree.coming"] = "coming later (another pair)",
        ["tree.cost"] = "{0} pts",
        ["tree.taken"] = "Taken.",
        ["tree.can"] = "Available: click or press Enter to take it.",
        ["tree.confirm"] = "Giving back {0} also gives back: {1}.",
        ["tree.confirm.yes"] = "Give back",
        ["tree.confirm.no"] = "Keep",
        ["tree.gained"] = "Gained: {0}",
        ["tree.lost"] = "Lost: {0}",
        ["tree.undo"] = "Undo the changes",
        ["tree.undone"] = "Changes undone: the saved talents are back.",
        ["tree.keys"] = "Arrows: move · Enter: take or give back · Ctrl+S: save",
        ["bar.Talents"] = "Talents",
        ["talents.reset"] = "The talent tree changed: {0}'s talents are given back, for free, once.",
        ["pair.bogged"] = "Mud",
        ["pair.chilled"] = "Ice",
        ["pair.cracked"] = "Magma",
        ["pair.electrified"] = "Lightning",
        ["pair.fogged"] = "Steam",
        ["pair.blinded"] = "Sand",
        ["points.rank"] = "rank {0}/{1}",
        ["points.next"] = "next rank: {0} point(s)",
        ["points.max"] = "highest rank",
        ["points.save"] = "Save",
        ["points.close"] = "Close",
        ["points.saved"] = "Points saved.",
        ["points.offline"] = "Offline: nothing is saved.",
        ["window.in-fight"] = "In a fight: to look at only.",
        ["sd.cost"] = "Cost: {0} AP",
        ["sd.range"] = "Range: {0} to {1}",
        ["sd.per-turn"] = "{0} cast(s) per turn",
        ["sd.crit-chance"] = "Critical: {0}%",
        ["sd.damage"] = "{0} to {1} {2} damage",
        ["sd.crit"] = "{0} to {1} on a critical hit",
        ["sd.unlock"] = "Unlocked at level {0}",
        ["sd.next"] = "Next rank ({0} spell point(s)): {1} to {2}",
        ["sd.next-cost"] = "Next rank: {0} spell point(s)",
        ["sd.hint"] = "Hover over a spell to see its card; damage counts your characteristics and equipment.",
        ["deck.add"] = "Add to the deck",
        ["deck.remove"] = "Take out of the deck",
        ["deck.in"] = "in the deck",
        ["deck.count"] = "Deck {0}/{1}",
        ["deck.clear"] = "Empty the deck",
        ["deck.copy"] = "Copy the code",
        ["deck.copied"] = "Code copied: {0}",
        ["deck.code"] = "Deck code",
        ["deck.use"] = "Use",
        ["deck.code-used"] = "Deck taken from the code.",
        ["deck.code-partly"] = "Deck taken from the code; {0} spell(s) not unlocked yet left out.",
        ["deck.bad-code"] = "This is not a deck code of your class.",
        ["deck.help"] = "Drag a spell onto the deck; drag it off the deck to take it out.",
        ["sd.all"] = "All",
        ["sd.rank"] = "Rank {0}/{1}",
        ["sd.locked"] = "Lv {0}",
        ["sd.preview"] = "Preview: save your talents",
        ["sheet.tab.stats"] = "Characteristics",
        ["sheet.tab.spells"] = "Spells",
        ["sheet.level"] = "Level {0} · {1}",
        ["sheet.hp"] = "Hit points",
        ["sheet.tree"] = "Tree: +{0}% damage · +{1}% resistances · +{2}% HP",
        ["sheet.ap"] = "AP",
        ["sheet.mp"] = "MP",
        ["sheet.initiative"] = "Initiative",
        ["card.initiative"] = "Initiative: {0}",
        ["sheet.before"] = "You play before: {0}",
        ["sheet.before-none"] = "You play after every family: initiative is found on boots, capes and rings.",
        ["sheet.left"] = "Points to spend",
        ["sheet.col.stat"] = "Characteristic",
        ["sheet.col.invested"] = "Put in",
        ["sheet.col.worn"] = "Equipment",
        ["sheet.col.total"] = "Total",
        ["sheet.col.bonus"] = "Effect",
        ["sheet.col.resist"] = "Resistance",
        ["sheet.bonus.hp"] = "+{0} HP",
        ["sheet.bonus.damage"] = "+{0} {1} damage",
        ["sheet.help"] = "10 points in an element = +2 damage in that element, and nothing else · type a value, or Shift+click: 10 points at once",
        ["sheet.reset"] = "Reset",
        ["sheet.min"] = "MIN",
        ["sheet.max"] = "MAX",
        ["char.Vitality"] = "Vitality",
        ["char.Earth"] = "Earth",
        ["char.Fire"] = "Fire",
        ["char.Water"] = "Water",
        ["char.Air"] = "Air",
        ["char.Vitality.help"] = "+1 HP a point",
        ["char.Earth.help"] = "+2 Earth damage for every 10 points, and nothing else",
        ["char.Fire.help"] = "+2 Fire damage for every 10 points, and nothing else",
        ["char.Water.help"] = "+2 Water damage for every 10 points, and nothing else",
        ["char.Air.help"] = "+2 Air damage for every 10 points, and nothing else",
        ["xp.line"] = "Level {0} · {1} / {2} XP",
        ["xp.max"] = "Level {0} · {1} XP",
        ["result.xp"] = "+{0} XP",
        ["result.shards"] = "+{0} Éclats",
        ["end.title"] = "Fight over",
        ["end.results"] = "Results",
        ["end.stats"] = "Statistics",
        ["end.winners"] = "Winners",
        ["end.losers"] = "Losers",
        ["end.close"] = "Close",
        ["end.name"] = "Name",
        ["end.level"] = "Lvl",
        ["end.xp"] = "XP",
        ["end.shards"] = "Éclats",
        ["end.loot"] = "Loot",
        ["end.dealt"] = "Damage dealt",
        ["end.taken"] = "Damage taken",
        ["end.healing"] = "Healing",
        ["end.shielding"] = "Shields",
        ["end.kills"] = "Finished off",
        ["result.quest"] = "Quest done: {0}",
        ["result.level"] = "Level {0}!",
        ["result.offline"] = "Offline: the experience is not kept.",
        ["result.refused"] = "The server did not count this fight: {0}",
        ["card.fighter"] = "{0} · {1}/{2} HP · {3} AP · {4} MP",
        ["card.shield"] = " · shield {0}",
        ["card.resist"] = "Resistances: {0}",
        ["card.resist-one"] = "{0} {1}%",
        ["card.effects"] = "Effects: {0}",
        ["card.passive"] = "{0}: {1}",
        ["card.turns"] = "{0} ({1} turn(s))",
        ["card.spell"] = "{0} · {1} AP · range {2}-{3}",
        ["card.in-line"] = ", in line",
        ["card.no-sight"] = ", no line of sight",
        ["card.per-turn"] = " · {0} per turn",
        ["card.cooldown"] = " · cooldown {0} turn(s)",
        ["card.damage"] = "{0} to {1} damage, {2}",
        ["card.crit"] = " ({0} to {1} on a critical hit, {2}%)",
        ["card.area"] = "area: {0} of {1}",
        ["area.Cross"] = "cross",
        ["area.Circle"] = "circle",
        ["area.Line"] = "line",
        ["effect.heal"] = "heal {0} to {1}",
        ["effect.shield"] = "shield {0} ({1} turn(s))",
        ["effect.push"] = "push by {0}",
        ["effect.pull"] = "pull by {0}",
        ["effect.summon"] = "summons {0}",
        ["effect.leap"] = "leap onto the cell",
        ["effect.swap"] = "swap places",
        ["effect.glyph"] = "glyph for {0} turn(s): {1}",
        ["effect.glyph-damage"] = "{0} to {1} damage, {2}",
        ["card.glyph"] = "{0}'s glyph ({1}): {2}, {3}; {4} turn(s) left",
        ["glyph.Enemies"] = "strikes its enemies",
        ["glyph.Allies"] = "touches its allies",
        ["glyph.All"] = "touches everyone",
        ["log.glyph"] = "{0}: a glyph strikes!",
        ["affects.Caster"] = " on self",
        ["affects.Allies"] = " on allies",
        ["affects.All"] = " on everyone",
        ["affects.Enemies"] = "",
        ["affects.Attacker"] = " on the attacker",
        ["forecast.damage"] = "{0}: {1} to {2} damage",
        ["forecast.heal"] = "{0}: +{1} to {2} HP",
        ["forecast.kill"] = " (lethal)",
        ["forecast.crit"] = ", {0} to {1} on a critical hit",
        ["log.move"] = "{0} moves {1} cell(s).",
        ["log.cast"] = "{0} casts {1}.",
        ["log.damage"] = "{0} loses {1} HP.",
        ["log.died"] = "{0} is defeated.",
        ["log.heal"] = "{0} recovers {1} HP.",
        ["log.summon"] = "{0} summons {1}.",
        ["log.shield"] = "{0} gets a shield of {1}.",
        ["log.absorbed"] = "{0}'s shield absorbs {1}.",
        ["log.pushed"] = "{0} is moved {1} cell(s).",
        ["log.blocked"] = "{0} hits an obstacle.",
        ["log.status"] = "{0}: {1} for {2} turn(s).",
        ["log.status-ended"] = "{0}: {1} wears off.",
        ["log.passive"] = "{0}: {1}!",
        ["log.leap"] = "{0} leaps.",
        ["log.swap"] = "{0} and {1} swap places.",
        ["log.marked"] = "{0} bears the {1} mark.",
        ["log.reaction"] = "{0}: {1}!",
        ["log.reaction-status"] = "{0}: {1}, {2} for {3} turn(s).",
        ["forecast.reaction"] = " — {0}!",
        ["forecast.tree"] = "   tree: {0}",
        ["forecast.trunk"] = "trunk +{0}%",
        ["forecast.share"] = "{0} {1}%",
        ["forecast.ceiling"] = " (capped at +{0}%)",
        ["stat.Poison"] = "poison {0}{1}",
        ["stat.Ap"] = "{0} AP",
        ["stat.Mp"] = "{0} MP",
        ["stat.Damage"] = "{0}% damage",
        ["stat.Resistance"] = "{0}% resistance{1}",
        ["stat.Range"] = "{0} range",
        ["stat.Electrified"] = "electrified, {0}% of every hit jumps",
        ["element.Neutral"] = "neutral",
        ["element.Earth"] = "Earth",
        ["element.Fire"] = "Fire",
        ["element.Water"] = "Water",
        ["element.Air"] = "Air",
        ["element.Lightning"] = "Lightning",
        ["card.arc"] = "Arc: {0}% of the damage jumps to the nearest enemy within {1} cells",
        ["log.arc"] = "The arc jumps from {0} to {1}.",
        ["log.support"] = "{0} supports {1}.",
        ["log.echo"] = "{0}'s echo strikes {1}.",
        ["glyph.remanence"] = "Remanence",
        ["error.NotEnoughMp"] = "Not enough MP.",
        ["error.NotEnoughAp"] = "Not enough AP.",
        ["error.CastLimit"] = "Spell already cast as often as allowed this turn.",
        ["error.OutOfRange"] = "Out of range.",
        ["error.NotInLine"] = "This spell is cast in a straight line.",
        ["error.NoLineOfSight"] = "No line of sight.",
        ["error.Unreachable"] = "Cell out of reach.",
        ["error.Occupied"] = "Cell taken.",
        ["error.NotFloor"] = "You cannot go or aim there.",
        ["error.OffBoard"] = "Off the board.",
        ["error.UnknownSpell"] = "Unknown spell.",
        ["error.FightOver"] = "The fight is over.",
        ["error.TooManySummons"] = "Already as many summons as allowed.",
        ["error.Cooldown"] = "Spell still on cooldown.",
        ["error.TargetNotFree"] = "A free cell is needed to land on.",
        ["error.NoValidTarget"] = "This spell aims at another fighter, of the right side.",
        ["error.SupportSpent"] = "No more support this turn: a spell on an ally will wait for the next.",
        ["lobby.title"] = "Osmose",
        ["lobby.name"] = "Account name",
        ["lobby.password"] = "Password",
        ["lobby.sign-in"] = "Sign in",
        ["lobby.sign-up"] = "Create the account",
        ["lobby.remember-name"] = "Remember my account name",
        ["lobby.remember-password"] = "Remember my password",
        ["lobby.server"] = "Server: {0}",
        ["lobby.characters"] = "Your characters ({0}/{1})",
        ["lobby.none"] = "No character yet: create one.",
        ["lobby.character-name"] = "Character name",
        ["lobby.create"] = "Create",
        ["lobby.play"] = "Play",
        ["lobby.delete"] = "Delete",
        ["lobby.sign-out"] = "Sign out",
        ["lobby.wait"] = "One moment…",
        ["lobby.back"] = "Characters",
        ["lobby.choose-server"] = "Choose a server first.",
        ["lobby.servers"] = "Choose a server",
        ["lobby.server-characters"] = "{0} character(s)",
        ["lobby.launcher"] = "Sign in, or open Osmose from its launcher.",
        ["lobby.level"] = "Level {0}",
        ["lobby.new"] = "Create a character",
        ["lobby.change-server"] = "Change server",
        ["lobby.confirm-delete"] = "Delete {0} for ever?",
        ["lobby.cancel"] = "Cancel",
        ["lobby.close"] = "Close",
        ["look.female"] = "Woman {0}",
        ["lobby.colour"] = "Outfit",
        ["lobby.class"] = "Class",
        ["lobby.spells"] = "Its spells, from level 1 to level 100",
        ["lobby.hair"] = "Hair",
        ["lobby.female"] = "Woman",
        ["lobby.male"] = "Man",
        ["lobby.skin"] = "Skin",
        ["lobby.height"] = "Height",
        ["lobby.build"] = "Build",
        ["town.help"] = "Click: walk · click on someone: talk to them · right click on monsters: attack · edge of the map: next map · wheel: zoom · Escape: menu",
        ["bar.Characteristics"] = "Characteristics",
        ["bar.Spells"] = "Spells",
        ["bar.Inventory"] = "Inventory",
        ["bar.Map"] = "Map",
        ["bar.Journal"] = "Journal",
        ["bar.Jobs"] = "Trades",
        ["bar.Contacts"] = "Contacts",
        ["bar.Guild"] = "Guild",
        ["bar.Bestiary"] = "Bestiary",
        ["jobs.title"] = "Trades",
        ["jobs.gathering"] = "Gathering",
        ["jobs.making"] = "Making",
        ["jobs.level"] = "lv. {0}",
        ["jobs.xp"] = "{0} / {1}",
        ["jobs.recipes"] = "Recipes",
        ["jobs.resources"] = "Resources",
        ["jobs.search"] = "Search…",
        ["jobs.from"] = "Level",
        ["jobs.to"] = "to",
        ["jobs.all-kinds"] = "All kinds",
        ["jobs.order.Highest"] = "Highest level",
        ["jobs.order.Lowest"] = "Lowest level",
        ["jobs.order.Name"] = "Name",
        ["jobs.possible"] = "Possible recipes only",
        ["jobs.none"] = "No recipe.",
        ["jobs.recipe"] = "{0} · lv. {1} · {2} XP",
        ["jobs.soon"] = "soon",
        ["jobs.near"] = "{0}, near the way in",
        ["jobs.far"] = "{0}, at the far end",
        ["jobs.deep"] = "{0}, in deep water",
        ["jobs.made"] = "made",
        ["jobs.owned"] = "you have {0}",
        ["jobs.look"] = "To look at: one makes at the trade's workshop.",
        ["quest.Talk"] = "Talk to {0}",
        ["quest.Defeat"] = "Defeat: {0} ({1}/{2})",
        ["quest.Bring"] = "Bring: {0} ({1}/{2})",
        ["quest.Dungeon"] = "Finish {0}",
        ["quest.DungeonRoom"] = "Win room {3} of {0}",
        ["quest.Interact"] = "Examine: {0}",
        ["quest.Win"] = "Win: {0}",
        ["quest.Wear"] = "Wear: {0}",
        ["quest.found"] = "Quest \"{0}\": one more clue.",
        ["journal.where"] = "Where: {0}",
        ["prop.seen"] = "{0}: {1}",
        ["journal.follow"] = "Follow",
        ["journal.followed"] = "Followed",
        ["journal.inventory"] = "the inventory (I)",
        ["quest.offer"] = "Quest: {0}",
        ["quest.handin"] = "Hand the quest back: {0}",
        ["quest.accept"] = "I accept.",
        ["quest.later"] = "Later.",
        ["quest.give"] = "With pleasure.",
        ["quest.taken"] = "Quest taken: {0}. The journal (Q) follows it.",
        ["quest.done"] = "Quest done: {0} · +{1} XP · +{2} Éclats",
        ["quest.talked"] = "Quest \"{0}\": noted.",
        ["journal.title"] = "Quest journal",
        ["journal.active"] = "Under way",
        ["journal.done"] = "Done",
        ["journal.village"] = "Clairval",
        ["journal.empty"] = "No quest yet: talk to the inhabitants.",
        ["journal.giver"] = "Given by {0}",
        ["journal.ready"] = "To hand back to {0}",
        ["journal.hidden"] = "Hidden",
        ["journal.tab"] = "{0} ({1})",
        ["journal.search"] = "Search a quest…",
        ["journal.levels"] = "Levels",
        ["journal.to"] = "to",
        ["journal.level"] = "Level {0}",
        ["journal.nomatch"] = "No quest matches the search or the filters.",
        ["journal.hide"] = "Hide",
        ["journal.show"] = "Show",
        ["journal.objectives"] = "Objectives",
        ["journal.rewards"] = "Rewards",
        ["journal.xp"] = "{0} XP",
        ["journal.shards"] = "{0} Éclats",
        ["journal.item"] = "{0} ×{1}",
        ["journal.finished"] = "Done",
        ["item.Equip"] = "Equip",
        ["item.TakeOff"] = "Take off",
        ["item.Use"] = "Use",
        ["item.Recipes"] = "Recipes",
        ["item.Bestiary"] = "Bestiary",
        ["item.Set"] = "Set",
        ["item.Share"] = "Share in the chat",
        ["item.hint"] = "Right click: more options",
        ["item.makes"] = "{0}, level {1}: {2}",
        ["item.needs"] = "{0} ({1}, level {2}): needs {3}",
        ["item.drop"] = "{0}, level {1} ({2}): {3} %",
        ["item.drop-quest"] = "{0}, level {1} ({2}): {3} %, while its quest is under way",
        ["item.piece"] = "{0} · {1} · level {2}",
        ["item.none"] = "Nothing to show for this item.",
        ["item.card"] = "Card",
        ["questkind.Tutorial"] = "First steps",
        ["questkind.Main"] = "Main quest",
        ["questkind.Side"] = "Side quest",
        ["questkind.Trade"] = "Trade quest",
        ["questkind.Dungeon"] = "Dungeon quest",
        ["bar.menu"] = "Menu",
        ["bar.title"] = "MENUS",
        ["bar.tip"] = "{0} ({1})",
        ["menu.title"] = "Menu",
        ["menu.sound"] = "Sound",
        ["menu.master"] = "Overall volume",
        ["menu.music"] = "Music",
        ["menu.effects"] = "Effects",
        ["menu.mute"] = "Mute",
        ["menu.Resume"] = "Back to the game",
        ["menu.Servers"] = "Change server",
        ["menu.Characters"] = "Change character",
        ["menu.Quit"] = "Quit",
        ["menu.key"] = "Esc",
        ["group.member"] = "{0} (lvl {1})",
        ["group.attack"] = "Attack",
        ["group.horde"] = "{0} ×{1} (lvl {2} to {3})",
        ["map.button"] = "Map",
        ["map.title"] = "World map",
        ["chat.map"] = "Nearby",
        ["chat.line"] = "[{0}] {1}: {2}",
        ["chat.refused"] = "Not sent: {0}",
        ["chat.placeholder"] = "Enter to talk (/p for the party, /gu for the guild, /w Name to whisper), Escape to leave",
        ["chat.party"] = "Party",
        ["bestiary.title"] = "Bestiary",
        ["bestiary.unknown"] = "???",
        ["bestiary.fight-it"] = "Never fought: a silhouette.",
        ["bestiary.met"] = "{0}, level {1}, {2}",
        ["bestiary.wins"] = "Victories: {0}",
        ["bestiary.hp"] = "Hit points: {0}",
        ["bestiary.resistances"] = "Resistances: {0}",
        ["bestiary.none"] = "none",
        ["bestiary.weakness"] = "Weakness: {0}",
        ["bestiary.no-weakness"] = "No weakness",
        ["bestiary.spells"] = "Spells: {0}",
        ["bestiary.lesson"] = "The family's lesson, {0}: {1}",
        ["bestiary.loot"] = "Loot: {0}",
        ["bestiary.next"] = "{0} more victory(ies) to learn more.",
        ["bestiary.known"] = "{0} of {1} monsters known",
        ["role.Melee"] = "Melee",
        ["role.Ranged"] = "Ranged",
        ["role.Tank"] = "Bulwark",
        ["role.Support"] = "Support",
        ["role.Boss"] = "Chief",
        ["rarity.Common"] = "common",
        ["rarity.Rare"] = "rare",
        ["rarity.VeryRare"] = "very rare",
        ["contracts.window"] = "Notice Board",
        ["contracts.tab"] = "Contracts",
        ["contracts.handIn"] = "Hand in",
        ["contracts.swapFree"] = "Swap this notice (free)",
        ["contracts.swapPaid"] = "Swap this notice ({0} Éclats)",
        ["contracts.group"] = "Group",
        ["contracts.pending"] = "Notices waiting: {0}",
        ["contracts.empty"] = "No notices for now: come back tomorrow.",
        ["contracts.done"] = "Contract fulfilled: +{0} XP, +{1} Éclats.",
        ["contracts.renewal"] = "New notices go up every day at dawn.",
        ["contracts.progress"] = "{0} / {1}",
        ["contracts.reward"] = "+{0} XP, +{1} Éclats",
        ["contracts.needs-server"] = "Contracts only work online.",
        ["contract.hunt.title"] = "Hunt: {1}",
        ["contract.hunt.text"] = "The {1} are growing bold and prowling near the paths. A bounty to whoever sees off {0} of them. Area: {2}.",
        ["contract.gather.title"] = "Wanted: {0} {1}",
        ["contract.gather.text"] = "Stores are running low and the roads are empty. Wanted: {0} {1}, soon as can be. Paid on the nail, no haggling.",
        ["contract.craft.title"] = "Made to order: {1}",
        ["contract.craft.text"] = "Wanted: a skilled pair of hands to make this: {1}. Work checked before payment, as is only right.",
        ["contract.dungeon.title"] = "Scouting: {1}",
        ["contract.dungeon.text"] = "Nobody dares say any more what lies past the entrance. Get through the second room, then come back and tell. Place: {1}.",
        ["contract.horde.title"] = "Horde sighted: {1}",
        ["contract.horde.text"] = "The {1} are banding together, six or more at a time. Alone, it's madness: go in numbers, or don't go at all.",
        ["contract.boss.title"] = "Bounty on the leader: {1}",
        ["contract.boss.text"] = "Whatever comes out of there answers to something deep inside. It must be brought down, and not alone. Place: {1}.",
        ["waystone.title"] = "Waystones",
        ["waystone.travel"] = "Travel ({0} Éclats)",
        ["waystone.here"] = "You are here",
        ["waystone.unknown"] = "{0} · not discovered yet",
        ["waystone.known"] = "{0}",
        ["waystone.home"] = "{0} · your home",
        ["waystone.make-home"] = "Make it my home",
        ["waystone.shards"] = "Éclats: {0}",
        ["waystone.arrived"] = "Here you are: {0}.",
        ["waystone.recalled"] = "The scroll takes you back: {0}.",
        ["waystone.needs-server"] = "Waystones need the server.",
        ["chat.whisper"] = "Whisper",
        ["chat.guild"] = "Guild",
        ["chat.whisper-line"] = "[Whisper] {0} → {1}: {2}",
        ["contacts.title"] = "Contacts",
        ["contacts.friends"] = "Friends ({0})",
        ["contacts.ignored"] = "Ignored ({0})",
        ["contacts.online"] = "{0} (lvl {1}) · {2}",
        ["contacts.offline"] = "{0} (lvl {1}) · offline",
        ["contacts.name"] = "A character's name",
        ["contacts.add"] = "Add a friend",
        ["contacts.ignore"] = "Ignore",
        ["contacts.invite"] = "Invite",
        ["contacts.whisper"] = "Write",
        ["contacts.remove"] = "Take {0} out",
        ["contacts.notify"] = "Tell me when a friend comes into play",
        ["contacts.came"] = "{0} is in play: {1}.",
        ["contacts.none"] = "Nobody yet.",
        ["contacts.room"] = "Contacts: {0}/{1}",
        ["contacts.col.status"] = "Status",
        ["contacts.col.name"] = "Character",
        ["contacts.col.level"] = "Level",
        ["contacts.col.class"] = "Class",
        ["contacts.col.zone"] = "Zone",
        ["contacts.in-play"] = "IN PLAY ({0})",
        ["contacts.away"] = "OFFLINE ({0})",
        ["contacts.offline-zone"] = "Offline",
        ["contacts.needs-server"] = "Contacts need the server.",
        ["guild.title"] = "Guild",
        ["guild.none"] = "You are not in a guild. Edmée, the town clerk on Clairval's square, will enter a new one for {0} Éclats; a leader or an officer can also invite you.",
        ["guild.room"] = "Members: {0}/{1}",
        ["guild.rank.Leader"] = "Leader",
        ["guild.rank.Officer"] = "Officer",
        ["guild.rank.Member"] = "Member",
        ["guild.col.name"] = "Character",
        ["guild.col.rank"] = "Rank",
        ["guild.col.level"] = "Level",
        ["guild.col.class"] = "Class",
        ["guild.col.where"] = "Where / last seen",
        ["guild.seen.days"] = "{0} d ago",
        ["guild.seen.hours"] = "{0} h ago",
        ["guild.seen.minutes"] = "{0} min ago",
        ["guild.seen.never"] = "never seen",
        ["guild.online"] = "{0} (lvl {1}, {2}) · {3}",
        ["guild.offline"] = "{0} (lvl {1}, {2}) · {3}",
        ["guild.name"] = "Name of a character in play",
        ["guild.invite"] = "Invite",
        ["guild.invite-into"] = "Invite into {0}",
        ["guild.leave"] = "Leave the guild",
        ["guild.promote"] = "Make {0} an officer",
        ["guild.demote"] = "Demote {0} to member",
        ["guild.kick"] = "Remove {0} from the guild",
        ["guild.hand-over"] = "Hand leadership to {0}",
        ["guild.invited"] = "{0} invites you to join {1}.",
        ["guild.join"] = "Join",
        ["guild.decline"] = "Decline",
        ["guild.found.title"] = "Found a guild for {0} Éclats",
        ["guild.found.name"] = "Guild name (3 to 20 characters)",
        ["guild.found"] = "Found",
        ["guild.cancel"] = "Cancel",
        ["guild.founded"] = "The guild {0} has been founded.",
        ["guild.joined"] = "You have joined {0}.",
        ["guild.left"] = "You are no longer in a guild.",
        ["guild.needs-server"] = "Guilds only work online.",
        ["battle.opened"] = "{0} attacks {1}: join them?",
        ["battle.join"] = "Join",
        ["battle.join-line"] = "{0} attacks: join ({1} s)",
        ["battle.absent"] = "away",
        ["battle.seconds"] = "{0} s",
        ["party.kick"] = "Send {0} out of the party",
        ["battle.waiting"] = "Fight opened: your allies have 15 seconds to join you.",
        ["battle.go"] = "Start",
        ["battle.wait"] = "Wait",
        ["party.invite"] = "Invite to the party",
        ["party.trade"] = "Offer a trade",
        ["party.invited"] = "{0} invites you to their party.",
        ["party.join"] = "Join",
        ["party.decline"] = "Decline",
        ["party.leave"] = "Leave the party",
        ["party.title"] = "Party ({0}/{1})",
        ["party.member"] = "{0} · lv {1}",
        ["party.leader"] = "{0} · lv {1} · leader",
        ["party.offline"] = "{0} · offline",
        ["party.left"] = "You are no longer in a party.",
        ["party.sent"] = "Invitation sent to {0}.",
        ["trade.asked"] = "{0} offers you a trade.",
        ["trade.open"] = "Trade",
        ["trade.title"] = "Trade with {0}",
        ["trade.mine"] = "Your offer",
        ["trade.theirs"] = "{0}'s offer",
        ["trade.ready"] = "Accept",
        ["trade.waiting"] = "Accepted: waiting for {0}",
        ["trade.they-ready"] = "{0} has accepted.",
        ["trade.cancel"] = "Call off",
        ["trade.done"] = "Trade made.",
        ["trade.cancelled"] = "Trade called off.",
        ["trade.sent"] = "Trade offered to {0}.",
        ["trade.hint"] = "A click on an item of your inventory adds it, a click on your offer takes it back.",
        ["trade.nothing"] = "Nothing yet.",
        ["chat.offline"] = "The chat needs the server.",
        ["chat.closed"] = "The chat is closed: change maps to open it again.",
        ["chat.open"] = "Proximity chat: the players of this map read you.",
        ["map.dungeon"] = "Dungeon: {0}",
        ["map.grid"] = "Grid",
        ["map.level"] = "level {0}",
        ["map.here"] = "you are here",
        ["minimap.tip"] = "Mini-map (N)",
        ["minimap.title"] = "Mini-map",
        ["minimap.you"] = "You",
        ["minimap.goal"] = "Goal of the quest followed",
        ["minimap.spot"] = "{0} · {1} lvl {2}",
        ["minimap.in"] = "Zoom in",
        ["minimap.out"] = "Zoom out",
        ["minimap.size"] = "Size",
        ["town.back"] = "Back to town",
        ["craft.make"] = "Make",
        ["craft.make-all"] = "All ({0})",
        ["craft.made"] = "Made: {0}.",
        ["craft.offline"] = "Trades need the server.",
        ["craft.close"] = "Close",
        ["shop.title"] = "Rose's stall · {0} Éclats",
        ["gather.doing"] = "{0}: gathering {1}…",
        ["gather.done"] = "+{0} {1} (+{2} {3} XP)",
        ["gather.offline"] = "Gathering needs the server.",
        ["end.life"] = "Life: {0} / {1}",
        ["life.line"] = "Life: {0} / {1}",
        ["life.rate"] = "+{0} HP/s, ×2 in the village; nothing in a fight.",
        ["life.low"] = "You have only {0} HP of {1}. Attack anyway?",
        ["life.attack"] = "Attack",
        ["life.later"] = "Later",
        ["life.eaten"] = "{0} eaten: {1} HP.",
        ["gather.growing"] = "This spot is growing back.",
        ["job.levelup"] = "{0}: level {1}!",
        ["inventory.shards"] = "{0} Éclats",
        ["shop.buy"] = "Buy",
        ["shop.sell"] = "Sell",
        ["shop.row"] = "{0} · {1} Éclats",
        ["shop.row-owned"] = "{0} · {1} Éclats · you have {2}",
        ["shop.poor"] = "Not enough Éclats.",
        ["shop.worn"] = "Worn.",
        ["shop.bought"] = "Bought: {0}.",
        ["shop.sold"] = "Sold: {0}.",
        ["shop.nothing"] = "Nothing Rose buys.",
        ["points.reset-cost"] = "Taking points back will cost {0} Éclats.",
        ["dungeon.enter"] = "Go into {0}?",
        ["dungeon.key-used"] = "This uses up a key: {0}.",
        ["dungeon.keyring"] = "The operator's keyring opens the door.",
        ["dungeon.no-key"] = "{0}: a key is needed to go in ({1}).",
        ["dungeon.resume"] = "Go back into {0}, room {1} of 3?",
        ["dungeon.go-in"] = "Go in",
        ["dungeon.stay"] = "Stay outside",
        ["dungeon.room"] = "{0}, room {1} of 3",
        ["dungeon.done"] = "Dungeon cleared!",
        ["dungeon.out"] = "You are outside again.",
        ["hall.shut"] = "The far door stays shut: beat this room's group first.",
        ["way.shut"] = "The East Gate is barred with heavy planks. Nobody in Clairval remembers ever seeing it open.",
        ["hall.rested"] = "The fountain's water closes your wounds: {0} hit points.",
        ["hall.hp"] = "Hit points: {0} / {1}. The fountain heals.",
        ["town.exit"] = "To: {0}",
        ["town.saved"] = "Position saved on the server.",
        ["lobby.colour-n"] = "Colour {0}",
        ["class"] = "{0} {1} HP · {2} AP · {3} MP · {4}",
        ["class.level"] = "Lv {0}: {1}",
        ["look.male"] = "Man {0}",
        ["lobby.fill-in"] = "Enter a name and a password.",
        ["lobby.password-short"] = "A password has at least 10 characters.",
        ["lobby.account-invalid"] = "Account name: 3 to 20 unaccented letters, digits, '-' or '_'.",
        ["lobby.account-taken"] = "This account name is already taken.",
        ["lobby.wrong-password"] = "Unknown name or wrong password.",
        ["lobby.name-invalid"] = "Character name: 3 to 20 letters and nothing else, the first a capital.",
        ["lobby.name-taken"] = "This character name is already taken on the server.",
        ["lobby.class-invalid"] = "Choose a class, a look and a colour.",
        ["lobby.too-many-characters"] = "At most five characters per account.",
        ["lobby.session-expired"] = "Session expired: sign in again.",
        ["lobby.too-many-tries"] = "Too many tries: wait a minute.",
        ["lobby.server-error"] = "The server ran into an error.",
        ["lobby.unreachable"] = "Server unreachable: try again in a moment.",
    };

    private readonly IReadOnlyDictionary<string, string> _table;

    public Texts(string lang)
    {
        Lang = lang == "fr" ? "fr" : "en";
        _table = Lang == "fr" ? Fr : En;
    }

    public string Lang { get; }

    public string this[string key, params object[] args] =>
        string.Format(CultureInfo.InvariantCulture, _table[key], args);

    public string Name(Fighter f) => (f ?? throw new ArgumentNullException(nameof(f))).Name.In(Lang);

    /// <summary>A playable look as the player reads it: "female-c" is "Femme C" or "Woman C".</summary>
    public string Look(string look)
    {
        ArgumentNullException.ThrowIfNull(look);
        int dash = look.LastIndexOf('-');
        return dash < 0 ? look : this["look." + look[..dash], look[(dash + 1)..].ToUpperInvariant()];
    }

    /// <summary>A class as the creation screen describes it: what it does, its points at level 1 and the elements it hits in.</summary>
    public string Class(HeroClass c)
    {
        ArgumentNullException.ThrowIfNull(c);
        string elements = string.Join(", ", SpellsOf(c).Where(s => s.DamageMax > 0 && s.Element != Element.Neutral)
            .Select(s => s.Element).Distinct().Select(e => this["element." + e]));
        return this["class", c.Description.In(Lang), c.Hp, c.Ap, c.Mp, elements];
    }

    /// <summary>A class's spells by the level that unlocks them: "Niv. 1 : Frappe, Flèche · Niv. 6 : Flèche de givre".</summary>
    public string ClassSpells(HeroClass c)
    {
        ArgumentNullException.ThrowIfNull(c);
        return string.Join(" · ", SpellsOf(c).GroupBy(s => s.Level)
            .Select(g => this["class.level", g.Key, string.Join(", ", g.Select(s => s.Name.In(Lang)))]));
    }

    private static IEnumerable<Spell> SpellsOf(HeroClass c) =>
        c.Spells.Select(id => GameData.Embedded.Spells.GetValueOrDefault(id)).OfType<Spell>();

    public string Error(ActionError error) => this["error." + error];

    /// <summary>
    /// What hovering a fighter tells: name, hit points, points (what is left for the one playing),
    /// shield; resistances other than zero; the statuses running and their turns.
    /// </summary>
    public string FighterCard(Fighter f, Fight fight)
    {
        ArgumentNullException.ThrowIfNull(f);
        ArgumentNullException.ThrowIfNull(fight);
        bool playing = !fight.IsOver && fight.Current == f;
        var lines = new List<string> { this["card.fighter", Name(f), f.Hp, f.MaxHp, playing ? f.Ap : f.Spec.Ap, playing ? f.Mp : f.Spec.Mp] + (f.Shield > 0 ? this["card.shield", f.Shield] : "") };
        string[] resist = [.. new[] { Element.Earth, Element.Fire, Element.Water, Element.Air }
            .Where(e => f.Resistance(e) != 0).Select(e => this["card.resist-one", this["element." + e], f.Resistance(e)])];
        if (resist.Length > 0)
            lines.Add(this["card.resist", string.Join(", ", resist)]);
        if (f.Statuses.Count > 0)
            lines.Add(this["card.effects", string.Join(", ", f.Statuses.Select(st => this["card.turns", Status(st.Stat, st.Value, st.Element), st.TurnsLeft]))]);
        // Its passives (sprint 90): what each does, in a line.
        lines.AddRange(f.Passives.Select(p => this["card.passive", p.Name.In(Lang), p.Description.In(Lang)]));
        // Its initiative (sprint 95): who plays first, under its points.
        lines.Insert(1, this["card.initiative", f.Spec.Initiative]);
        return string.Join('\n', lines);
    }

    /// <summary>
    /// The families a hero of this initiative plays before (sprint 95, D71): those whose every monster has
    /// as much or less, the heroes playing first on a tie.
    /// </summary>
    public string PlaysBefore(int initiative)
    {
        string[] before = [.. GameData.Embedded.Families.Values.Where(f => f.Monsters.All(m => m.Initiative <= initiative)).Select(f => f.Name.In(Lang))];
        return before.Length == 0 ? this["sheet.before-none"] : this["sheet.before", string.Join(", ", before)];
    }

    /// <summary>What hovering a spell tells: cost, range and its limits; damage, area and effects.</summary>
    /// <summary>
    /// A spell's card; with the caster's characteristics, its damage counts them (2 more for every 10 points
    /// of its element), as the fight will.
    /// </summary>
    public string SpellCard(Spell s, Characteristics? stats = null)
    {
        ArgumentNullException.ThrowIfNull(s);
        int bonus = Characteristics.Bonus((stats ?? Characteristics.None).For(s.Element));
        string head = this["card.spell", s.Name.In(Lang), s.ApCost, s.MinRange, s.MaxRange]
            + (s.InLine ? this["card.in-line"] : "") + (s.LineOfSight ? "" : this["card.no-sight"])
            + this["card.per-turn", s.PerTurn] + (s.Cooldown > 0 ? this["card.cooldown", s.Cooldown] : "");
        var parts = new List<string>();
        if (s.DamageMax > 0)
        {
            int crit = Core.Spell.CritBonus(s.DamageMax);
            parts.Add(this["card.damage", s.DamageMin + bonus, s.DamageMax + bonus, this["element." + s.Element]]
                + (s.Crit > 0 ? this["card.crit", s.DamageMin + crit + bonus, s.DamageMax + crit + bonus, s.Crit] : ""));
        }
        // Lightning's signature (sprint 138).
        if (s.DamageMax > 0 && s.Element == Element.Lightning)
            parts.Add(this["card.arc", Elements.Arc, Elements.ArcCells]);
        parts.AddRange(Effects(s));
        return parts.Count == 0 ? head : head + "\n" + string.Join(", ", parts);
    }

    /// <summary>What a glyph does when it strikes (sprint 130): its damage, then its status.</summary>
    private string GlyphDoes(GlyphEffect g)
    {
        var parts = new List<string>();
        if (g.DamageMax > 0)
            parts.Add(this["effect.glyph-damage", g.DamageMin, g.DamageMax, this["element." + g.Element]]);
        if (g.AsStatus is StatusEffect st)
            parts.Add(this["card.turns", Status(st.Stat, st.Value, st.Element), st.Turns]);
        return string.Join(", ", parts);
    }

    /// <summary>
    /// A glyph's card, on hovering one of its cells (sprint 130): its caster and spell, whom it strikes, what it
    /// does, and its caster's turns left.
    /// </summary>
    public string GlyphCard(Core.Glyph g, Fight fight)
    {
        ArgumentNullException.ThrowIfNull(g);
        ArgumentNullException.ThrowIfNull(fight);
        Fighter caster = fight.Fighters[g.Caster];
        string spell = caster.Spells.FirstOrDefault(s => s.Id == g.Spell)?.Name.In(Lang) ?? (g.Spell == Fight.RemanenceKey ? this["glyph.remanence"] : g.Spell);
        return this["card.glyph", Name(caster), spell, this["glyph." + g.Effect.Affects], GlyphDoes(g.Effect), g.TurnsLeft];
    }

    /// <summary>A spell's area and effects, each as a phrase ("zone : croix de 1", "soin 7 à 10 des alliés").</summary>
    private List<string> Effects(Spell s)
    {
        var parts = new List<string>();
        if (s.Area is Area area && area.Shape != AreaShape.Point)
            parts.Add(this["card.area", this["area." + area.Shape], area.Radius]);
        foreach (SpellEffect e in s.AllEffects)
        {
            string text = e switch
            {
                HealEffect h => this["effect.heal", h.Min, h.Max],
                ShieldEffect sh => this["effect.shield", sh.Amount, sh.Turns],
                PushEffect p => this["effect.push", p.Cells],
                PullEffect p => this["effect.pull", p.Cells],
                SummonEffect m => this["effect.summon", GameData.Embedded.Summons.TryGetValue(m.Summon, out SummonSpec? spec) ? spec.Name.In(Lang) : m.Summon],
                StatusEffect st => this["card.turns", Status(st.Stat, st.Value, st.Element), st.Turns],
                LeapEffect => this["effect.leap"],
                SwapEffect => this["effect.swap"],
                GlyphEffect g => this["effect.glyph", g.Turns, GlyphDoes(g)],
                _ => "",
            };
            parts.Add(text + (e is SummonEffect or LeapEffect ? "" : this["affects." + e.Affects]));
        }
        return parts;
    }

    /// <summary>
    /// The details of a spell on the spells screen, line by line (sprint 65): cost, range, casts and
    /// cooldown, critical chance; damage and critical damage with the hero's points; area and effects;
    /// then the level that unlocks it, or what the next rank gives and costs.
    /// </summary>
    public IReadOnlyList<string> SpellDetails(BookSpell b, Characteristics? stats)
    {
        ArgumentNullException.ThrowIfNull(b);
        Spell s = b.Spell.AtRank(b.Rank);
        var lines = new List<string>
        {
            this["sd.cost", s.ApCost],
            this["sd.range", s.MinRange, s.MaxRange] + (s.InLine ? this["card.in-line"] : "") + (s.LineOfSight ? "" : this["card.no-sight"]),
            this["sd.per-turn", s.PerTurn] + (s.Cooldown > 0 ? this["card.cooldown", s.Cooldown] : ""),
        };
        if (s.Crit > 0)
            lines.Add(this["sd.crit-chance", s.Crit]);
        if (s.DamageMax > 0)
        {
            lines.Add(this["sd.damage", b.Damage.Min, b.Damage.Max, this["element." + s.Element]]);
            if (s.Crit > 0)
                lines.Add(this["sd.crit", b.Critical.Min, b.Critical.Max]);
        }
        lines.AddRange(Effects(s));
        if (!b.Unlocked)
            lines.Add(this["sd.unlock", b.Spell.Level]);
        else if (b.NextCost == 0)
            lines.Add(this["points.max"]);
        else
            lines.Add(b.NextRank is (int min, int max) && max > 0 ? this["sd.next", b.NextCost, min, max] : this["sd.next-cost", b.NextCost]);
        return lines;
    }

    /// <summary>
    /// A map's title: its name, and for a map of a zone of several (sprint 70) its place in the world,
    /// "Bois de Clairval [-3, 0]", as in the games of the kind.
    /// </summary>
    public string MapTitle(Town map)
    {
        ArgumentNullException.ThrowIfNull(map);
        string name = map.Name.In(Lang);
        // The village's maps each have a name of their own (sprint 102): no place in the world after it.
        return map.Zone is null || Shop.InVillage(map.Id) ? name : string.Create(CultureInfo.InvariantCulture, $"{name} [{map.MapAt.X}, {map.MapAt.Y}]");
    }

    /// <summary>A zone's name, as a friend's is shown (sprint 126): the village's, or the name the maps of the zone share.</summary>
    public string ZoneName(string zone, GameData data)
    {
        ArgumentNullException.ThrowIfNull(data);
        Town? map = data.Towns.TryGetValue(zone, out Town? t) ? t : data.Towns.Values.FirstOrDefault(m => m.Zone == zone);
        return map?.Name.In(Lang) ?? zone;
    }

    /// <summary>
    /// A group of monsters as its label says it: up to three, each one with its level, "Laie (niv. 2),
    /// Sanglier chargeur (niv. 2)"; more, their family, how many and their levels, "Sangliers ×7 (niv. 1 à 5)".
    /// </summary>
    public string GroupLabel(MonsterGroup group, GameData data)
    {
        ArgumentNullException.ThrowIfNull(group);
        ArgumentNullException.ThrowIfNull(data);
        MonsterSpec[] members = [.. group.Monsters.Select(id => data.Monsters[id])];
        return members.Length <= 3
            ? string.Join(", ", members.Select(m => this["group.member", m.Name.In(Lang), m.Level]))
            : this["group.horde", data.Families[group.Family].Name.In(Lang), members.Length, members.Min(m => m.Level), members.Max(m => m.Level)];
    }

    /// <summary>A character's level and experience: "Niveau 3 · 340 / 600 XP" (the total for the next level).</summary>
    public string XpLine(int level, long xp) =>
        level >= Hero.MaxLevel ? this["xp.max", level, xp] : this["xp.line", level, xp, Progression.XpToReach(level + 1)];

    /// <summary>What a fight earned, as the end screen says it: experience, the quest done, the new level.</summary>
    public string Result(FightResult result, int levelBefore)
    {
        ArgumentNullException.ThrowIfNull(result);
        var parts = new List<string> { this["result.xp", result.Xp] };
        if (result.Shards > 0)
            parts.Add(this["result.shards", result.Shards]);
        if (result.Quest is string id && GameData.Embedded.Quests.TryGetValue(id, out Quest? quest))
            parts.Add(this["result.quest", quest.Name.In(Lang)]);
        if (result.Level > levelBefore)
            parts.Add(this["result.level", result.Level]);
        if (result.DungeonDone)
            parts.Insert(0, this["dungeon.done"]);
        string text = string.Join(" · ", parts);
        return result.Loot is { Count: > 0 } loot ? text + "\n" + this["result.loot", Items(loot)] : text;
    }

    /// <summary>
    /// What the door of a dungeon says (sprint 80), and whether one may go in: back to its room if the
    /// character is in it already; in with the keyring, or using up its key; shut without either.
    /// Without a character (offline, for tests), it opens.
    /// </summary>
    public (string Text, bool CanEnter) DoorText(Dungeon dungeon, IReadOnlyList<ItemCount>? owned, DungeonRun? run)
    {
        ArgumentNullException.ThrowIfNull(dungeon);
        string name = dungeon.Name.In(Lang), key = Items([new ItemCount(dungeon.Key, 1)]);
        if (run?.Dungeon == dungeon.Id)
            return (this["dungeon.resume", name, run.Room], true);
        if (owned is null)
            return (this["dungeon.enter", name], true);
        if (!Dungeons.CanEnter(dungeon, owned))
            return (this["dungeon.no-key", name, key], false);
        return (this["dungeon.enter", name] + "\n" + (Dungeons.UsedOnEntry(dungeon, owned) is null ? this["dungeon.keyring"] : this["dungeon.key-used", key]), true);
    }

    /// <summary>A quest's objective as the journal says it (sprint 88): "Vaincre : Sangliers (1/3)", "Parler à Rose".</summary>
    public string Objective(QuestObjective objective, int reached)
    {
        ArgumentNullException.ThrowIfNull(objective);
        GameData data = GameData.Embedded;
        string target = objective.Goal switch
        {
            QuestGoal.Talk => data.FindNpc(objective.Target) is (_, Npc n) ? n.Name.In(Lang) : objective.Target,
            QuestGoal.Defeat => data.Families.TryGetValue(objective.Target, out MonsterFamily? f) ? f.Name.In(Lang)
                : data.Monsters.TryGetValue(objective.Target, out MonsterSpec? m) ? m.Name.In(Lang) : objective.Target,
            QuestGoal.Bring or QuestGoal.Wear => data.Items.TryGetValue(objective.Target, out Item? i) ? i.Name.In(Lang) : objective.Target,
            QuestGoal.Interact => data.FindProp(objective.Target) is (_, Prop p) ? p.Name.In(Lang) : objective.Target,
            QuestGoal.Win => data.FindScenario(objective.Target) is Scenario sc ? sc.Name.In(Lang) : objective.Target,
            _ => data.Dungeons.TryGetValue(objective.Target, out Dungeon? d) ? d.Name.In(Lang) : objective.Target,
        };
        string key = objective.Goal == QuestGoal.Dungeon && objective.Room > 0 ? "quest.DungeonRoom" : "quest." + objective.Goal;
        return this[key, target, Math.Min(reached, objective.Count), objective.Count, objective.Room];
    }

    /// <summary>A text in both languages (the answers the game adds to a talk, sprint 88).</summary>
    public static LocalizedText Both(string key) => new(new Texts("fr")[key], new Texts("en")[key]);

    /// <summary>The same, around a name in both languages.</summary>
    public static LocalizedText Both(string key, LocalizedText name) =>
        new(new Texts("fr")[key, (name ?? throw new ArgumentNullException(nameof(name))).Fr], new Texts("en")[key, name.En]);

    /// <summary>Some items as a list: "Croc d'orque ×2, Pain".</summary>
    public string Items(IEnumerable<ItemCount> items)
    {
        ArgumentNullException.ThrowIfNull(items);
        return string.Join(", ", items.Select(i => (GameData.Embedded.Items.TryGetValue(i.Item, out Item? item) ? item.Name.In(Lang) : i.Item) + (i.Count > 1 ? $" ×{i.Count}" : "")));
    }

    /// <summary>
    /// An item as its card says it: name, slot or kind, the level it needs; what it gives; its set's
    /// bonuses: "Cape du braconnier · Cape · niveau 1\n+6 Agilité\nPanoplie du braconnier : 2 pièces +10 Agilité ; 3 pièces +20 Agilité, +1 PM".
    /// </summary>
    public string ItemCard(Item item)
    {
        ArgumentNullException.ThrowIfNull(item);
        string what = item.Slot is ItemSlot slot ? this["slot." + slot] : this["kind." + item.Kind];
        var lines = new List<string> { $"{item.Name.In(Lang)} · {what}" + (item.Kind == ItemKind.Equipment ? " · " + this["inventory.level", item.Level] : "") };
        string gives = Gives(item.Stats, item.Ap, item.Mp, item.Initiative);
        if (gives.Length > 0)
            lines.Add(gives);
        // Food and potions (sprints 101 and 111; potions out of a fight too since sprint 115): what they give back.
        if (item.Heal > 0)
            lines.Add(this["item.food", item.Heal]);
        if (item.Set is string id && GameData.Embedded.Sets.TryGetValue(id, out ItemSet? set))
            lines.Add(this["inventory.set", set.Name.In(Lang), string.Join(" ; ", set.Bonuses.Select(b => this["inventory.pieces", b.Pieces, Gives(b.Stats, b.Ap, b.Mp, b.Initiative)]))]);
        return string.Join('\n', lines);
    }

    /// <summary>"+5 Vitalité, +1 PM, +10 Initiative": the characteristics, action and movement points and initiative given.</summary>
    public string Gives(Characteristics? stats, int ap, int mp, int initiative = 0)
    {
        Characteristics st = stats ?? Characteristics.None;
        var parts = new (int Value, string Name)[] { (st.Vitality, this["char.Vitality"]), (st.Earth, this["char.Earth"]), (st.Fire, this["char.Fire"]), (st.Water, this["char.Water"]), (st.Air, this["char.Air"]), (ap, this["ap"]), (mp, this["mp"]), (initiative, this["sheet.initiative"]) };
        return string.Join(", ", parts.Where(p => p.Value != 0).Select(p => $"{p.Value.ToString("+0;-0", CultureInfo.InvariantCulture)} {p.Name}"));
    }

    /// <summary>What a cast would do, one line per fighter of its area (<see cref="Fight.Foresee"/>).</summary>
    public string Forecast(IEnumerable<Forecast> forecasts)
    {
        ArgumentNullException.ThrowIfNull(forecasts);
        return string.Join('\n', forecasts.SelectMany(fc => new[]
        {
            fc.DamageMax > 0 ? this["forecast.damage", Name(fc.Fighter), fc.DamageMin, fc.DamageMax] + (fc.CritMax > 0 ? this["forecast.crit", fc.CritMin, fc.CritMax] : "") + (fc.SureKill ? this["forecast.kill"] : "")
                + (fc.Reaction is string r && !fc.SureKill ? this["forecast.reaction", GameData.Embedded.Reactions[r].Name.In(Lang)] : "") : null,
            fc.DamageMax > 0 ? TreeShares(fc) : null,
            fc.HealMax > 0 ? this["forecast.heal", Name(fc.Fighter), fc.HealMin, fc.HealMax] : null,
        }).OfType<string>());
    }

    /// <summary>
    /// The tree's part of a hit (sprint 150, D82, O6 a): "arbre : tronc +3 %, Au plus près +10 %, Pied ferme +15 %", and the
    /// ceiling when the talents' parts pass it; null when the tree gives nothing.
    /// </summary>
    public string? TreeShares(Forecast fc)
    {
        ArgumentNullException.ThrowIfNull(fc);
        IReadOnlyList<TalentShare> shares = fc.Tree ?? [];
        if (shares.Count == 0 && fc.Trunk == 0)
            return null;
        var parts = new List<string>();
        if (fc.Trunk != 0)
            parts.Add(this["forecast.trunk", fc.Trunk]);
        parts.AddRange(shares.Select(sh => this["forecast.share", GameData.Embedded.Talents[sh.Talent].Name.In(Lang), sh.Percent.ToString("+0;-0", CultureInfo.InvariantCulture)]));
        int sum = shares.Sum(sh => sh.Percent);
        return this["forecast.tree", string.Join(", ", parts)] + (sum > Rpg.Core.Talents.Ceiling ? this["forecast.ceiling", Rpg.Core.Talents.Ceiling] : "");
    }

    /// <summary>A talent's tile (sprint 137): "Enlisement · Réactions, étage 1, 1 point(s)", then what it does.</summary>
    public string Talent(TalentSpec t)
    {
        ArgumentNullException.ThrowIfNull(t);
        return this["talents.tile", t.Name.In(Lang), this["talents.branch." + t.Branch], this["talents.tier." + t.Tier], t.Cost] + "\n" + t.Description.In(Lang);
    }

    /// <summary>
    /// Why a pair cannot join the talents taken (sprint 146, D82, C2), in the player's language: "l'Eau est déjà prise par
    /// Boue"; null when it shares no element with them.
    /// </summary>
    public string? PairTaken(IReadOnlyList<string> taken, TalentSpec pair, GameData data)
    {
        ArgumentNullException.ThrowIfNull(taken);
        ArgumentNullException.ThrowIfNull(pair);
        ArgumentNullException.ThrowIfNull(data);
        if (!pair.IsPair || Rpg.Core.Talents.Clash([.. taken.Where(id => id != pair.Id).Select(id => data.Talents[id]), pair], data) is not (Element shared, TalentSpec first))
            return null;
        return this["talents.taken." + shared, this["pair." + first.Rules.OfType<ReactionRule>().First().Reaction]];
    }

    /// <summary>A refusal of the tree (sprint 147, D82) in the player's language: "Demande d'abord Électrisation."</summary>
    public string WhyNot(TalentRefusal refusal, GameData data)
    {
        ArgumentNullException.ThrowIfNull(refusal);
        ArgumentNullException.ThrowIfNull(data);
        string Name(string id) => data.Talents[id].Name.In(Lang);
        return refusal.Kind switch
        {
            RefusalKind.Opens or RefusalKind.Level or RefusalKind.Points => this["talents.why." + refusal.Kind, refusal.Number],
            RefusalKind.Parent => this["talents.why.Parent", refusal.Other is string one ? Name(one) : string.Join(this["talents.why.or"], (refusal.Any ?? []).Select(Name))],
            RefusalKind.Pair => this["talents.why.Pair", this["talents.taken." + refusal.Element, this["pair." + data.Talents[refusal.Other!].Rules.OfType<ReactionRule>().First().Reaction]]],
            RefusalKind.Excludes or RefusalKind.KeyPerStyle or RefusalKind.Switch => this["talents.why." + refusal.Kind, Name(refusal.Other!)],
            _ => this["talents.why." + refusal.Kind],
        };
    }

    /// <summary>The trunk at a level (sprint 146, D82): "Tronc, gratuit : +3 % de dégâts (niv. 30), …", what is to come so said.</summary>
    public string Trunk(int level)
    {
        string Node(TrunkNode n) => this["talents.trunk." + n.Gain, n.Percent, n.Level];
        return this["talents.trunk", string.Join(" · ", Rpg.Core.Talents.Trunk.Select(n => level < n.Level ? this["talents.trunk.unlit", Node(n)] : Node(n)))];
    }

    /// <summary>
    /// The short name under a node of the tree's window (sprint 148): a talent's name, the trunk's gain, a folded floor's
    /// style and floor; nothing under what is still to come (L10 c).
    /// </summary>
    public string TreeName(TreeNode node, GameData data)
    {
        ArgumentNullException.ThrowIfNull(node);
        ArgumentNullException.ThrowIfNull(data);
        if (node.IsTalent)
        {
            // A choice's halves by their element alone, the choice's name over them (the Transmutation, the Hearts).
            TalentSpec t = data.Talents[node.Id];
            if (t.Branch == Branch.Pure && t.Rules.OfType<ElementRule>().FirstOrDefault() is ElementRule heart)
                return this["element." + heart.Element];
            if (t.Id.StartsWith("transmute-", StringComparison.Ordinal) && t.Id.Split('-') is [_, string from, _])
                return this["element." + Enum.Parse<Element>(from, ignoreCase: true)];
            // A pair by its compound, its reaction's name in the panel (sprint 149, C4 c).
            if (TreeGuide.PairOf(t, data) is ReactionSpec pair)
                return this["pair." + pair.Id];
            return t.Name.In(Lang);
        }
        if (node.IsTrunk)
            return TrunkGain(int.Parse(node.Id["trunk:".Length..], CultureInfo.InvariantCulture));
        return node.IsFold ? this["tree.fold", this["talents.branch." + node.Branch], this["talents.tier." + node.Tier]] : "";
    }

    /// <summary>
    /// What the side panel says of a node (sprint 148, K7 b, H6 a, I1 b; sprint 149, M2 a to M8 b): its layer and the layer's
    /// banner, a line of lore, its name, place and cost, a pair's equation ("Foudre = Feu + Air"), what it does, its
    /// counterpart, then its state: taken (and what giving it back gives back with it), available, or the reason it is
    /// refused; for Helping Hand, what it does with the hero's elements (D9 a).
    /// </summary>
    public TreeCard TreeCard(TreeNode node, PointsEditor editor)
    {
        ArgumentNullException.ThrowIfNull(node);
        ArgumentNullException.ThrowIfNull(editor);
        GameData data = editor.Data;
        TreeLayer layer = TreeGuide.LayerOf(node, data);
        string banner = node.Branch is Branch b && TreeLayout.Styles.Contains(b) ? this["tree.banner." + b] : this["tree.banner." + layer];
        string title = this["tree.layer." + layer] + (node.Branch is Branch s && TreeLayout.Styles.Contains(s) ? " · " + this["talents.branch." + s] : "");
        if (node.IsTrunk)
        {
            int level = int.Parse(node.Id["trunk:".Length..], CultureInfo.InvariantCulture);
            string gain = this["tree.trunk.detail", TrunkGain(level)];
            return new(title, banner, "", this["tree.trunk"], null, gain, "", editor.Hero.Level < level ? this["talents.why.Level", level] : "", null);
        }
        if (!node.IsTalent)
            return new(title, banner, "", node.IsFold ? TreeName(node, data) : "", null, node.IsFold ? this["tree.fold.detail", this["talents.branch." + node.Branch]] : "", "", "", null);
        TalentSpec t = data.Talents[node.Id];
        if (t.Tier == Tier.Key)
            banner += " " + this["tree.banner.Key"];
        if (t.Branch is Branch.Pure || t.Id.StartsWith("transmute-", StringComparison.Ordinal))
            banner += " " + this["tree.banner.Choice"];
        string state;
        if (editor.HasTalent(t))
            state = editor.Losing(t) is { Count: > 0 } lost ? this["talents.losing", string.Join(", ", lost.Select(id => data.Talents[id].Name.In(Lang)))] : this["tree.taken"];
        else
            state = TalentReasons.WhyNot(editor.TalentsTaken, t.Id, editor.Hero.Level, data) is TalentRefusal no ? WhyNot(no, data) : this["tree.can"];
        string? equation = TreeGuide.PairOf(t, data) is ReactionSpec pair ? this["tree.equation", this["pair." + pair.Id], this["element." + pair.First], this["element." + pair.Second]] : null;
        string? extra = t.Rules.OfType<SupportRule>().Any() && t.Id == "helping-hand"
            ? this["tree.helps", string.Join(" ; ", TreeGuide.Helps(editor.TalentsTaken, data).Select(e => this["tree.help." + e]))]
            : t.Id == "shared-mark" || t.Branch == Branch.Support ? this["tree.group"] : null;
        // The line "before and after" (sprint 150, M4 b), after what the node says of itself.
        if (BeforeAfter(t.Id, editor) is string change)
            extra = extra is null ? change : extra + "\n" + change;
        string head = this["talents.tile", t.Name.In(Lang), this["talents.branch." + t.Branch], this["talents.tier." + t.Tier], t.Cost];
        return new(title, banner, t.Lore?.In(Lang) ?? "", head, equation, t.Description.In(Lang), t.Counterpart is { } c ? this["tree.counterpart", c.In(Lang)] : "", state, extra);
    }

    /// <summary>
    /// Where a spell of the book comes from and what the tree does to it (sprint 150, D82, F5 a, O3 a): the talent that
    /// gives it, the spell it took the place of, the talents that change it; nothing for a class's spell the tree leaves.
    /// </summary>
    public IReadOnlyList<string> SpellOrigin(BookSpell spell, GameData data)
    {
        ArgumentNullException.ThrowIfNull(spell);
        ArgumentNullException.ThrowIfNull(data);
        var lines = new List<string>();
        if (spell.Origin is string origin)
            lines.Add(this["spell.origin", data.Talents[origin].Name.In(Lang)]);
        if (spell.Replaces is string replaced)
            lines.Add(this["spell.replaces", data.Spells[replaced].Name.In(Lang)]);
        if (spell.ModifiedBy is { Count: > 0 } by)
            lines.Add(this["spell.modified", string.Join(", ", by.Select(id => data.Talents[id].Name.In(Lang)))]);
        return lines;
    }

    /// <summary>What the last change of talents did to the deck (F7 a), one sentence a spell.</summary>
    public IReadOnlyList<string> DeckChanges(PointsEditor editor)
    {
        ArgumentNullException.ThrowIfNull(editor);
        string Name(string id) => editor.Data.Spells[id].Name.In(Lang);
        (IReadOnlyList<(string From, string To)> swapped, IReadOnlyList<string> left) = editor.DeckChanges;
        return [.. swapped.Select(x => this["deck.swap", Name(x.To), Name(x.From)]), .. left.Select(id => this["deck.left", Name(id)])];
    }

    /// <summary>
    /// A node's line "before and after" (sprint 150, D82, M4 b): the bonus of the draft's talents on a typical hit without
    /// it, then with it; null when it changes nothing there.
    /// </summary>
    public string? BeforeAfter(string talent, PointsEditor editor)
    {
        ArgumentNullException.ThrowIfNull(editor);
        (int before, int after) = TreeGuide.BeforeAfter(editor.TalentsTaken, talent, editor.Draft, editor.Data);
        return before == after ? null : this["tree.before-after", before.ToString("+0;-0;0", CultureInfo.InvariantCulture), after.ToString("+0;-0;0", CultureInfo.InvariantCulture)];
    }

    /// <summary>The talents that light up with the turn, lit or not and what they give (sprint 150, O5 a); null without any.</summary>
    public string? Lights(IReadOnlyList<TalentLight> lights)
    {
        ArgumentNullException.ThrowIfNull(lights);
        if (lights.Count == 0)
            return null;
        return this["fight.lights", string.Join(", ", lights.Select(l => this[l.Lit ? "fight.light.on" : "fight.light.off", GameData.Embedded.Talents[l.Talent].Name.In(Lang), l.Percent.ToString("+0;-0;0", CultureInfo.InvariantCulture)]))];
    }

    /// <summary>The path a node asks and its cost (sprint 151, L8 a), or that none leads there.</summary>
    public string TreePath(IReadOnlyList<string>? path, GameData data)
    {
        ArgumentNullException.ThrowIfNull(data);
        return path is null ? this["tree.path.none"] : this["tree.path", string.Join(" → ", path.Select(id => data.Talents[id].Name.In(Lang))), TreePlan.Cost(path, data)];
    }

    /// <summary>Why a build's code is refused (sprint 151, N4 a).</summary>
    public string CodeRefused(CodeRead read)
    {
        ArgumentNullException.ThrowIfNull(read);
        return read.Refusal is CodeRefusal r ? this["tree.code." + r, read.Number] : this["tree.code.loaded"];
    }

    /// <summary>The panel's text of a node, all its lines (<see cref="TreeCard"/>) one after the other.</summary>
    public string TreeDetail(TreeNode node, PointsEditor editor) =>
        string.Join("\n", TreeCard(node, editor).Lines);

    /// <summary>The build's title (sprint 149, N8 a): "Foudre · Mêlée", "Cœur de Feu · Terrain", "Glace et Magma · Alchimie".</summary>
    public string TreeTitle(IReadOnlyList<string> taken, GameData data)
    {
        (IReadOnlyList<string> doors, Element? heart, bool converted, Branch? style) = TreeGuide.Title(taken, data);
        string? door = doors.Count switch
        {
            0 => heart is Element h ? this["tree.title.heart." + h] : null,
            1 => this["pair." + doors[0]],
            _ => this["tree.title.and", this["pair." + doors[0]], this["pair." + doors[1]]],
        };
        if (converted && doors.Contains("electrified"))
            door = this["tree.title.converted", this["element.Lightning"]] + (doors.Count > 1 ? " · " + this["pair." + doors.First(d => d != "electrified")] : "");
        string[] parts = [.. new[] { door, style is Branch s ? this["talents.branch." + s] : null }.OfType<string>()];
        return parts.Length == 0 ? this["tree.title.none"] : string.Join(" · ", parts);
    }

    /// <summary>
    /// The build's summary (sprint 149, N1 a): every counterpart it pays, then the best bonus a hit can reach, with a
    /// warning past the ceiling of +35 %.
    /// </summary>
    public IReadOnlyList<string> TreeBuildSummary(IReadOnlyList<string> taken, GameData data)
    {
        ArgumentNullException.ThrowIfNull(taken);
        ArgumentNullException.ThrowIfNull(data);
        string[] paid = [.. taken.Select(id => data.Talents[id]).Where(t => !t.IsPair && t.Branch != Branch.Fork).Select(t => $"{t.Name.In(Lang)} ({t.Counterpart?.In(Lang).TrimEnd('.')})")];
        int best = TreeGuide.Best(taken, data);
        return
        [
            paid.Length == 0 ? this["tree.summary.none"] : this["tree.summary.counterparts", string.Join(" ; ", paid)],
            best > Rpg.Core.Talents.Ceiling ? this["tree.summary.ceiling", best, Rpg.Core.Talents.Ceiling] : this["tree.summary.best", best],
        ];
    }

    /// <summary>The pair the hero's two highest characteristics point to (sprint 149, C5 b), while no pair is taken; null otherwise.</summary>
    public string? TreeAdvice(PointsEditor editor)
    {
        ArgumentNullException.ThrowIfNull(editor);
        GameData data = editor.Data;
        if (editor.TalentsTaken.Any(id => data.Talents[id].IsFork) || TreeGuide.Advice(editor.Draft, data) is not string pair)
            return null;
        ReactionSpec r = TreeGuide.PairOf(data.Talents[pair], data)!;
        return this["tree.advice", this["element." + r.First], this["element." + r.Second], this["pair." + r.Id]];
    }

    /// <summary>The words the tree uses, and what they mean (sprint 149, M7 a), for the legend and the hover.</summary>
    public IReadOnlyList<(string Term, string Meaning)> TreeGlossary =>
        [.. GlossaryKeys.Select(k => (this["tree.term." + k], this[$"tree.term.{k}.def"]))];

    private static readonly string[] GlossaryKeys = ["mark", "reaction", "door", "key", "switch", "beyond"];

    /// <summary>The three guided bubbles of the tree's first opening from level 30 (sprint 149, M6 a).</summary>
    public IReadOnlyList<string> TreeTutorial => [this["tree.tuto.1"], this["tree.tuto.2"], this["tree.tuto.3"]];

    /// <summary>The panel's summary of the draft (K7 b): the points left of those the level gives, the elements beyond the base, the big switch.</summary>
    public IReadOnlyList<string> TreeSummary(PointsEditor editor)
    {
        ArgumentNullException.ThrowIfNull(editor);
        GameData data = editor.Data;
        TalentSpec[] taken = [.. editor.TalentsTaken.Select(id => data.Talents[id])];
        string? big = taken.FirstOrDefault(t => t.IsSwitch)?.Name.In(Lang);
        return
        [
            editor.Hero.Level < Rpg.Core.Talents.Opens ? this["tree.closed", Rpg.Core.Talents.Opens] : this["tree.left", editor.TalentPointsLeft, Rpg.Core.Talents.Points(editor.Hero.Level)],
            this["tree.elements", Rpg.Core.Talents.ElementsOpened(taken), Rpg.Core.Talents.MaxOpened],
            this["tree.switch", big ?? this["tree.switch.none"]],
        ];
    }

    /// <summary>What the draft gains and loses against the saved talents (N6 a), one line each when there is any.</summary>
    public IReadOnlyList<string> TreeDiff(PointsEditor editor)
    {
        ArgumentNullException.ThrowIfNull(editor);
        (IReadOnlyList<string> gained, IReadOnlyList<string> lost) = editor.TalentsDiff;
        string Names(IEnumerable<string> ids) => string.Join(", ", ids.Select(id => editor.Data.Talents[id].Name.In(Lang)));
        var lines = new List<string>();
        if (gained.Count > 0)
            lines.Add(this["tree.gained", Names(gained)]);
        if (lost.Count > 0)
            lines.Add(this["tree.lost", Names(lost)]);
        return lines;
    }

    private string TrunkGain(int level)
    {
        TrunkNode n = Rpg.Core.Talents.Trunk.First(x => x.Level == level);
        return this["tree.trunk." + n.Gain, n.Percent];
    }

    public string Spell(Spell s) =>
        this["spell", (s ?? throw new ArgumentNullException(nameof(s))).Name.In(Lang), s.ApCost, s.MinRange, s.MaxRange];

    /// <summary>A status as the log shows it: "-2 PA", "+50 % de dégâts", "poison 6 (Eau)".</summary>
    public string Status(Stat stat, int value, Element element)
    {
        string signed = stat is Stat.Poison or Stat.Electrified ? value.ToString(CultureInfo.InvariantCulture) : value.ToString("+0;-0", CultureInfo.InvariantCulture);
        string elem = element == Element.Neutral ? "" : $" ({this["element." + element]})";
        return this["stat." + stat, signed, elem];
    }

    /// <summary>A line of the fight log for an event, or null for those the log does not show.</summary>
    public string? Describe(FightEvent e, Fight fight)
    {
        ArgumentNullException.ThrowIfNull(fight);
        return e switch
        {
            Moved m => this["log.move", Name(fight.Fighters[m.Fighter]), m.Path.Count],
            SpellCast c => this["log.cast", Name(fight.Fighters[c.Fighter]), fight.Fighters[c.Fighter].Spells.First(s => s.Id == c.Spell).Name.In(Lang)],
            Damaged d => this["log.damage", Name(fight.Fighters[d.Fighter]), d.Amount],
            Died d => this["log.died", Name(fight.Fighters[d.Fighter])],
            Healed h => this["log.heal", Name(fight.Fighters[h.Fighter]), h.Amount],
            Summoned m => this["log.summon", Name(fight.Fighters[m.Summoner]), Name(fight.Fighters[m.Fighter])],
            Shielded s => this["log.shield", Name(fight.Fighters[s.Fighter]), s.Amount],
            ShieldAbsorbed a => this["log.absorbed", Name(fight.Fighters[a.Fighter]), a.Amount],
            Pushed { Blocked: > 0 } p when p.Path.Count == 0 => this["log.blocked", Name(fight.Fighters[p.Fighter])],
            Pushed p => this["log.pushed", Name(fight.Fighters[p.Fighter]), p.Path.Count],
            StatusAdded { Reaction: string r } st => this["log.reaction-status", Name(fight.Fighters[st.Fighter]), fight.Data.Reactions[r].Name.In(Lang), Status(st.Stat, st.Value, st.Element), st.Turns],
            StatusAdded st => this["log.status", Name(fight.Fighters[st.Fighter]), Status(st.Stat, st.Value, st.Element), st.Turns],
            Echoed echo => this["log.echo", Name(fight.Fighters[echo.Fighter]), Name(fight.Fighters[echo.Target])],
            Supported sp => this["log.support", Name(fight.Fighters[sp.Caster]), Name(fight.Fighters[sp.Ally])],
            Arced a => this["log.arc", Name(fight.Fighters[a.From]), Name(fight.Fighters[a.To])],
            Marked m => this["log.marked", Name(fight.Fighters[m.Fighter]), this["element." + m.Element]],
            ReactionTriggered r => this["log.reaction", Name(fight.Fighters[r.Fighter]), fight.Data.Reactions[r.Reaction].Name.In(Lang)],
            StatusEnded st => this["log.status-ended", Name(fight.Fighters[st.Fighter]), this["stat." + st.Stat, "", ""].Trim()],
            PassiveTriggered p => this["log.passive", Name(fight.Fighters[p.Fighter]), fight.Fighters[p.Fighter].Passives.First(x => x.Id == p.Passive).Name.In(Lang)],
            Leaped l => this["log.leap", Name(fight.Fighters[l.Fighter])],
            Swapped w => this["log.swap", Name(fight.Fighters[w.A]), Name(fight.Fighters[w.B])],
            GlyphTriggered g => this["log.glyph", Name(fight.Fighters[g.Fighter])],
            _ => null,
        };
    }
}
