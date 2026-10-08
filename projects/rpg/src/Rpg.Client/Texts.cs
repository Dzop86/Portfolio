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
        ["log.move"] = "{0} se déplace de {1} case(s).",
        ["log.cast"] = "{0} lance {1}.",
        ["log.damage"] = "{0} perd {1} PV.",
        ["log.died"] = "{0} est vaincu(e).",
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
        ["log.move"] = "{0} moves {1} cell(s).",
        ["log.cast"] = "{0} casts {1}.",
        ["log.damage"] = "{0} loses {1} HP.",
        ["log.died"] = "{0} is defeated.",
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

    public string Error(ActionError error) => this["error." + error];

    public string Spell(Spell s) =>
        this["spell", (s ?? throw new ArgumentNullException(nameof(s))).Name.In(Lang), s.ApCost, s.MinRange, s.MaxRange];

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
            _ => null,
        };
    }
}
