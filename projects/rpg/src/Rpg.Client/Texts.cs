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
        ["again"] = "Rejouer",
        ["help"] = "Clic : se déplacer · 1, 2, 3 : choisir un sort · Échap : annuler · Espace : fin du tour",
        ["spell"] = "{0} ({1} PA, portée {2}-{3})",
        ["inventory.button"] = "Inventaire",
        ["inventory.title"] = "Inventaire et équipement",
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
        ["page.Quest"] = "Objets de quête",
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
        ["char.Vitality"] = "Vitalité",
        ["char.Strength"] = "Force",
        ["char.Intelligence"] = "Intelligence",
        ["char.Chance"] = "Chance",
        ["char.Agility"] = "Agilité",
        ["char.Vitality.help"] = "+1 PV par point",
        ["char.Strength.help"] = "+1 % de dégâts Terre et neutres",
        ["char.Intelligence.help"] = "+1 % de dégâts Feu et de soins",
        ["char.Chance.help"] = "+1 % de dégâts Eau",
        ["char.Agility.help"] = "+1 % de dégâts Air",
        ["xp.line"] = "Niveau {0} · {1} / {2} XP",
        ["xp.max"] = "Niveau {0} · {1} XP",
        ["result.xp"] = "+{0} XP",
        ["result.quest"] = "Quête terminée : {0}",
        ["result.level"] = "Niveau {0} !",
        ["result.offline"] = "Hors ligne : l'expérience n'est pas gardée.",
        ["result.refused"] = "Le serveur n'a pas compté ce combat : {0}",
        ["card.fighter"] = "{0} · {1}/{2} PV · {3} PA · {4} PM",
        ["card.shield"] = " · bouclier {0}",
        ["card.resist"] = "Résistances : {0}",
        ["card.resist-one"] = "{0} {1} %",
        ["card.effects"] = "Effets : {0}",
        ["card.turns"] = "{0} ({1} tour(s))",
        ["card.spell"] = "{0} · {1} PA · portée {2}-{3}",
        ["card.in-line"] = ", en ligne",
        ["card.no-sight"] = ", sans ligne de vue",
        ["card.per-turn"] = " · {0} fois par tour",
        ["card.cooldown"] = " · relance {0} tour(s)",
        ["card.damage"] = "{0} à {1} dégâts, {2}",
        ["card.area"] = "zone : {0} de {1}",
        ["area.Cross"] = "croix",
        ["area.Circle"] = "cercle",
        ["area.Line"] = "ligne",
        ["effect.heal"] = "soin {0} à {1}",
        ["effect.shield"] = "bouclier {0} ({1} tour(s))",
        ["effect.push"] = "poussée de {0}",
        ["effect.pull"] = "attirance de {0}",
        ["effect.summon"] = "invoque {0}",
        ["affects.Caster"] = " sur soi",
        ["affects.Allies"] = " aux alliés",
        ["affects.All"] = " à tous",
        ["affects.Enemies"] = "",
        ["forecast.damage"] = "{0} : {1} à {2} dégâts",
        ["forecast.heal"] = "{0} : +{1} à {2} PV",
        ["forecast.kill"] = " (mortel)",
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
        ["lobby.title"] = "Osmose",
        ["lobby.name"] = "Nom de compte",
        ["lobby.password"] = "Mot de passe",
        ["lobby.sign-in"] = "Se connecter",
        ["lobby.sign-up"] = "Créer le compte",
        ["lobby.offline"] = "Jouer hors ligne",
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
        ["lobby.launcher"] = "Ouvrez Osmose depuis son launcher pour vous connecter.",
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
        ["town.help"] = "Clic : marcher · clic sur un habitant : lui parler · une porte mène à un combat",
        ["town.back"] = "Retour en ville",
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
        ["lobby.unreachable"] = "Serveur injoignable : vous pouvez jouer hors ligne.",
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
        ["again"] = "Play again",
        ["help"] = "Click: move · 1, 2, 3: choose a spell · Esc: cancel · Space: end turn",
        ["spell"] = "{0} ({1} AP, range {2}-{3})",
        ["inventory.button"] = "Inventory",
        ["inventory.title"] = "Inventory and equipment",
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
        ["page.Quest"] = "Quest items",
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
        ["char.Vitality"] = "Vitality",
        ["char.Strength"] = "Strength",
        ["char.Intelligence"] = "Intelligence",
        ["char.Chance"] = "Chance",
        ["char.Agility"] = "Agility",
        ["char.Vitality.help"] = "+1 HP a point",
        ["char.Strength.help"] = "+1% Earth and neutral damage",
        ["char.Intelligence.help"] = "+1% Fire damage and healing",
        ["char.Chance.help"] = "+1% Water damage",
        ["char.Agility.help"] = "+1% Air damage",
        ["xp.line"] = "Level {0} · {1} / {2} XP",
        ["xp.max"] = "Level {0} · {1} XP",
        ["result.xp"] = "+{0} XP",
        ["result.quest"] = "Quest done: {0}",
        ["result.level"] = "Level {0}!",
        ["result.offline"] = "Offline: the experience is not kept.",
        ["result.refused"] = "The server did not count this fight: {0}",
        ["card.fighter"] = "{0} · {1}/{2} HP · {3} AP · {4} MP",
        ["card.shield"] = " · shield {0}",
        ["card.resist"] = "Resistances: {0}",
        ["card.resist-one"] = "{0} {1}%",
        ["card.effects"] = "Effects: {0}",
        ["card.turns"] = "{0} ({1} turn(s))",
        ["card.spell"] = "{0} · {1} AP · range {2}-{3}",
        ["card.in-line"] = ", in line",
        ["card.no-sight"] = ", no line of sight",
        ["card.per-turn"] = " · {0} per turn",
        ["card.cooldown"] = " · cooldown {0} turn(s)",
        ["card.damage"] = "{0} to {1} damage, {2}",
        ["card.area"] = "area: {0} of {1}",
        ["area.Cross"] = "cross",
        ["area.Circle"] = "circle",
        ["area.Line"] = "line",
        ["effect.heal"] = "heal {0} to {1}",
        ["effect.shield"] = "shield {0} ({1} turn(s))",
        ["effect.push"] = "push by {0}",
        ["effect.pull"] = "pull by {0}",
        ["effect.summon"] = "summons {0}",
        ["affects.Caster"] = " on self",
        ["affects.Allies"] = " on allies",
        ["affects.All"] = " on everyone",
        ["affects.Enemies"] = "",
        ["forecast.damage"] = "{0}: {1} to {2} damage",
        ["forecast.heal"] = "{0}: +{1} to {2} HP",
        ["forecast.kill"] = " (lethal)",
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
        ["lobby.title"] = "Osmose",
        ["lobby.name"] = "Account name",
        ["lobby.password"] = "Password",
        ["lobby.sign-in"] = "Sign in",
        ["lobby.sign-up"] = "Create the account",
        ["lobby.offline"] = "Play offline",
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
        ["lobby.launcher"] = "Open Osmose from its launcher to sign in.",
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
        ["town.help"] = "Click: walk · click on someone: talk to them · a gate leads to a fight",
        ["town.back"] = "Back to town",
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
        ["lobby.unreachable"] = "Server unreachable: you can play offline.",
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
        return string.Join('\n', lines);
    }

    /// <summary>What hovering a spell tells: cost, range and its limits; damage, area and effects.</summary>
    public string SpellCard(Spell s)
    {
        ArgumentNullException.ThrowIfNull(s);
        string head = this["card.spell", s.Name.In(Lang), s.ApCost, s.MinRange, s.MaxRange]
            + (s.InLine ? this["card.in-line"] : "") + (s.LineOfSight ? "" : this["card.no-sight"])
            + this["card.per-turn", s.PerTurn] + (s.Cooldown > 0 ? this["card.cooldown", s.Cooldown] : "");
        var parts = new List<string>();
        if (s.DamageMax > 0)
            parts.Add(this["card.damage", s.DamageMin, s.DamageMax, this["element." + s.Element]]);
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
                _ => "",
            };
            parts.Add(text + (e is SummonEffect ? "" : this["affects." + e.Affects]));
        }
        return parts.Count == 0 ? head : head + "\n" + string.Join(", ", parts);
    }

    /// <summary>A character's level and experience: "Niveau 3 · 340 / 600 XP" (the total for the next level).</summary>
    public string XpLine(int level, long xp) =>
        level >= Hero.MaxLevel ? this["xp.max", level, xp] : this["xp.line", level, xp, Progression.XpToReach(level + 1)];

    /// <summary>What a fight earned, as the end screen says it: experience, the quest done, the new level.</summary>
    public string Result(FightResult result, int levelBefore)
    {
        ArgumentNullException.ThrowIfNull(result);
        var parts = new List<string> { this["result.xp", result.Xp] };
        if (result.Quest is string id && GameData.Embedded.Quests.TryGetValue(id, out Quest? quest))
            parts.Add(this["result.quest", quest.Name.In(Lang)]);
        if (result.Level > levelBefore)
            parts.Add(this["result.level", result.Level]);
        string text = string.Join(" · ", parts);
        return result.Loot is { Count: > 0 } loot ? text + "\n" + this["result.loot", Items(loot)] : text;
    }

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
        var parts = new (int Value, string Name)[] { (st.Vitality, this["char.Vitality"]), (st.Strength, this["char.Strength"]), (st.Intelligence, this["char.Intelligence"]), (st.Chance, this["char.Chance"]), (st.Agility, this["char.Agility"]), (ap, this["ap"]), (mp, this["mp"]) };
        return string.Join(", ", parts.Where(p => p.Value != 0).Select(p => $"{p.Value.ToString("+0;-0", CultureInfo.InvariantCulture)} {p.Name}"));
    }

    /// <summary>What a cast would do, one line per fighter of its area (<see cref="Fight.Foresee"/>).</summary>
    public string Forecast(IEnumerable<Forecast> forecasts)
    {
        ArgumentNullException.ThrowIfNull(forecasts);
        return string.Join('\n', forecasts.SelectMany(fc => new[]
        {
            fc.DamageMax > 0 ? this["forecast.damage", Name(fc.Fighter), fc.DamageMin, fc.DamageMax] + (fc.SureKill ? this["forecast.kill"] : "") : null,
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
            _ => null,
        };
    }
}
