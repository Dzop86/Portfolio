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
        ["points.rank"] = "rang {0}/{1}",
        ["points.next"] = "rang suivant : {0} point(s)",
        ["points.max"] = "rang maximum",
        ["points.save"] = "Enregistrer",
        ["points.close"] = "Fermer",
        ["points.saved"] = "Points enregistrés.",
        ["points.offline"] = "Hors ligne : rien n'est enregistré.",
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
        ["sheet.tab.stats"] = "Caractéristiques",
        ["sheet.tab.spells"] = "Sorts",
        ["sheet.level"] = "Niveau {0} · {1}",
        ["sheet.hp"] = "Points de vie",
        ["sheet.ap"] = "PA",
        ["sheet.mp"] = "PM",
        ["sheet.initiative"] = "Initiative",
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
        ["stat.Poison"] = "poison {0}{1}",
        ["stat.Ap"] = "{0} PA",
        ["stat.Mp"] = "{0} PM",
        ["stat.Damage"] = "{0} % de dégâts",
        ["stat.Resistance"] = "{0} % de résistance{1}",
        ["element.Neutral"] = "neutre",
        ["element.Earth"] = "Terre",
        ["element.Fire"] = "Feu",
        ["element.Water"] = "Eau",
        ["element.Air"] = "Air",
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
        ["quest.Talk"] = "Parler à {0}",
        ["quest.Defeat"] = "Vaincre : {0} ({1}/{2})",
        ["quest.Bring"] = "Rapporter : {0} ({1}/{2})",
        ["quest.Dungeon"] = "Finir {0}",
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
        ["bar.menu"] = "Menu",
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
        ["chat.placeholder"] = "Entrée pour parler, Échap pour quitter",
        ["chat.offline"] = "Le chat demande le serveur.",
        ["chat.closed"] = "Le chat est fermé : changez de carte pour le rouvrir.",
        ["chat.open"] = "Chat de proximité : les joueurs de cette carte vous lisent.",
        ["map.dungeon"] = "Donjon : {0}",
        ["map.grid"] = "Quadrillage",
        ["map.level"] = "niveau {0}",
        ["map.here"] = "vous êtes ici",
        ["town.back"] = "Retour en ville",
        ["craft.title"] = "{0} · niveau {1}",
        ["craft.xp"] = "{0} / {1} points d'expérience de métier",
        ["craft.recipe"] = "{0} · niveau {1}",
        ["craft.make"] = "Fabriquer",
        ["craft.make-all"] = "Tout ({0})",
        ["craft.made"] = "Fabriqué : {0}.",
        ["craft.level"] = "Il faut le niveau {0} de métier.",
        ["craft.missing"] = "Il manque des ingrédients.",
        ["craft.offline"] = "Les métiers demandent le serveur.",
        ["craft.close"] = "Fermer",
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
        ["lobby.name-invalid"] = "Nom du personnage : 3 à 20 lettres, avec des traits d'union ou des apostrophes à l'intérieur.",
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
        ["points.rank"] = "rank {0}/{1}",
        ["points.next"] = "next rank: {0} point(s)",
        ["points.max"] = "highest rank",
        ["points.save"] = "Save",
        ["points.close"] = "Close",
        ["points.saved"] = "Points saved.",
        ["points.offline"] = "Offline: nothing is saved.",
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
        ["sheet.tab.stats"] = "Characteristics",
        ["sheet.tab.spells"] = "Spells",
        ["sheet.level"] = "Level {0} · {1}",
        ["sheet.hp"] = "Hit points",
        ["sheet.ap"] = "AP",
        ["sheet.mp"] = "MP",
        ["sheet.initiative"] = "Initiative",
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
        ["stat.Poison"] = "poison {0}{1}",
        ["stat.Ap"] = "{0} AP",
        ["stat.Mp"] = "{0} MP",
        ["stat.Damage"] = "{0}% damage",
        ["stat.Resistance"] = "{0}% resistance{1}",
        ["element.Neutral"] = "neutral",
        ["element.Earth"] = "Earth",
        ["element.Fire"] = "Fire",
        ["element.Water"] = "Water",
        ["element.Air"] = "Air",
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
        ["quest.Talk"] = "Talk to {0}",
        ["quest.Defeat"] = "Defeat: {0} ({1}/{2})",
        ["quest.Bring"] = "Bring: {0} ({1}/{2})",
        ["quest.Dungeon"] = "Finish {0}",
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
        ["bar.menu"] = "Menu",
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
        ["chat.placeholder"] = "Enter to talk, Escape to leave",
        ["chat.offline"] = "The chat needs the server.",
        ["chat.closed"] = "The chat is closed: change maps to open it again.",
        ["chat.open"] = "Proximity chat: the players of this map read you.",
        ["map.dungeon"] = "Dungeon: {0}",
        ["map.grid"] = "Grid",
        ["map.level"] = "level {0}",
        ["map.here"] = "you are here",
        ["town.back"] = "Back to town",
        ["craft.title"] = "{0} · level {1}",
        ["craft.xp"] = "{0} / {1} trade experience points",
        ["craft.recipe"] = "{0} · level {1}",
        ["craft.make"] = "Make",
        ["craft.make-all"] = "All ({0})",
        ["craft.made"] = "Made: {0}.",
        ["craft.level"] = "Trade level {0} is needed.",
        ["craft.missing"] = "Some ingredients are missing.",
        ["craft.offline"] = "Trades need the server.",
        ["craft.close"] = "Close",
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
        ["lobby.name-invalid"] = "Character name: 3 to 20 letters, with hyphens or apostrophes inside.",
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
        return string.Join('\n', lines);
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
        parts.AddRange(Effects(s));
        return parts.Count == 0 ? head : head + "\n" + string.Join(", ", parts);
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
        return map.Zone is null ? name : string.Create(CultureInfo.InvariantCulture, $"{name} [{map.MapAt.X}, {map.MapAt.Y}]");
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
            QuestGoal.Defeat => data.Families.TryGetValue(objective.Target, out MonsterFamily? f) ? f.Name.In(Lang) : objective.Target,
            QuestGoal.Bring => data.Items.TryGetValue(objective.Target, out Item? i) ? i.Name.In(Lang) : objective.Target,
            _ => data.Dungeons.TryGetValue(objective.Target, out Dungeon? d) ? d.Name.In(Lang) : objective.Target,
        };
        return this["quest." + objective.Goal, target, Math.Min(reached, objective.Count), objective.Count];
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
        string gives = Gives(item.Stats, item.Ap, item.Mp);
        if (gives.Length > 0)
            lines.Add(gives);
        if (item.Set is string id && GameData.Embedded.Sets.TryGetValue(id, out ItemSet? set))
            lines.Add(this["inventory.set", set.Name.In(Lang), string.Join(" ; ", set.Bonuses.Select(b => this["inventory.pieces", b.Pieces, Gives(b.Stats, b.Ap, b.Mp)]))]);
        return string.Join('\n', lines);
    }

    /// <summary>"+5 Vitalité, +1 PM": the characteristics, action and movement points given.</summary>
    private string Gives(Characteristics? stats, int ap, int mp)
    {
        Characteristics st = stats ?? Characteristics.None;
        var parts = new (int Value, string Name)[] { (st.Vitality, this["char.Vitality"]), (st.Earth, this["char.Earth"]), (st.Fire, this["char.Fire"]), (st.Water, this["char.Water"]), (st.Air, this["char.Air"]), (ap, this["ap"]), (mp, this["mp"]) };
        return string.Join(", ", parts.Where(p => p.Value != 0).Select(p => $"{p.Value.ToString("+0;-0", CultureInfo.InvariantCulture)} {p.Name}"));
    }

    /// <summary>What a cast would do, one line per fighter of its area (<see cref="Fight.Foresee"/>).</summary>
    public string Forecast(IEnumerable<Forecast> forecasts)
    {
        ArgumentNullException.ThrowIfNull(forecasts);
        return string.Join('\n', forecasts.SelectMany(fc => new[]
        {
            fc.DamageMax > 0 ? this["forecast.damage", Name(fc.Fighter), fc.DamageMin, fc.DamageMax] + (fc.CritMax > 0 ? this["forecast.crit", fc.CritMin, fc.CritMax] : "") + (fc.SureKill ? this["forecast.kill"] : "") : null,
            fc.HealMax > 0 ? this["forecast.heal", Name(fc.Fighter), fc.HealMin, fc.HealMax] : null,
        }).OfType<string>());
    }

    public string Spell(Spell s) =>
        this["spell", (s ?? throw new ArgumentNullException(nameof(s))).Name.In(Lang), s.ApCost, s.MinRange, s.MaxRange];

    /// <summary>A status as the log shows it: "-2 PA", "+50 % de dégâts", "poison 6 (Eau)".</summary>
    public string Status(Stat stat, int value, Element element)
    {
        string signed = stat == Stat.Poison ? value.ToString(CultureInfo.InvariantCulture) : value.ToString("+0;-0", CultureInfo.InvariantCulture);
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
            StatusAdded st => this["log.status", Name(fight.Fighters[st.Fighter]), Status(st.Stat, st.Value, st.Element), st.Turns],
            StatusEnded st => this["log.status-ended", Name(fight.Fighters[st.Fighter]), this["stat." + st.Stat, "", ""].Trim()],
            PassiveTriggered p => this["log.passive", Name(fight.Fighters[p.Fighter]), fight.Fighters[p.Fighter].Passives.First(x => x.Id == p.Passive).Name.In(Lang)],
            Leaped l => this["log.leap", Name(fight.Fighters[l.Fighter])],
            Swapped w => this["log.swap", Name(fight.Fighters[w.A]), Name(fight.Fighters[w.B])],
            _ => null,
        };
    }
}
