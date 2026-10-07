using Rogue.Core;

namespace Rogue.Cli;

public enum Language
{
    French,
    English,
}

/// <summary>Every sentence the terminal shows, in French and in English.</summary>
internal sealed class Texts(Language language)
{
    private bool Fr => language == Language.French;

    public Language Language => language;

    public string Monster(MonsterKind kind) => (kind, Fr) switch
    {
        (MonsterKind.Rat, true) => "le rat",
        (MonsterKind.Rat, false) => "the rat",
        (MonsterKind.Goblin, true) => "le gobelin",
        (MonsterKind.Goblin, false) => "the goblin",
        (MonsterKind.Orc, true) => "l'orque",
        (MonsterKind.Orc, false) => "the orc",
        (MonsterKind.Troll, true) => "le troll",
        (MonsterKind.Troll, false) => "the troll",
        _ => throw new ArgumentOutOfRangeException(nameof(kind)),
    };

    private string Capitalized(MonsterKind kind)
    {
        string name = Monster(kind);
        return char.ToUpperInvariant(name[0]) + name[1..];
    }

    public string Describe(GameEvent e) => e switch
    {
        PlayerAttacked a => Fr ? $"Vous frappez {Monster(a.Target)} ({a.Damage})." : $"You hit {Monster(a.Target)} ({a.Damage}).",
        PlayerMissed m => Fr ? $"Vous manquez {Monster(m.Target)}." : $"You miss {Monster(m.Target)}.",
        MonsterKilled k => Fr ? $"{Capitalized(k.Kind)} meurt (+{k.Value})." : $"{Capitalized(k.Kind)} dies (+{k.Value}).",
        MonsterAttacked a => Fr ? $"{Capitalized(a.Attacker)} vous frappe ({a.Damage})." : $"{Capitalized(a.Attacker)} hits you ({a.Damage}).",
        MonsterMissed m => Fr ? $"{Capitalized(m.Attacker)} vous manque." : $"{Capitalized(m.Attacker)} misses you.",
        GoldPicked g => Fr ? $"Vous ramassez {g.Amount} pièces d'or." : $"You pick up {g.Amount} gold coins.",
        PotionPicked => Fr ? "Vous ramassez une potion." : "You pick up a potion.",
        PotionDrunk d => Fr ? $"Vous buvez une potion (+{d.Healed} PV)." : $"You drink a potion (+{d.Healed} HP).",
        LevelGained l => Fr ? $"Vous passez au niveau {l.Level} !" : $"You reach level {l.Level}!",
        FloorReached f => Fr ? $"Vous descendez à l'étage {f.Depth}." : $"You go down to floor {f.Depth}.",
        PlayerDied d => Fr ? $"{Capitalized(d.Killer)} vous tue." : $"{Capitalized(d.Killer)} kills you.",
        DungeonEscaped => Fr ? "Vous sortez du donjon !" : "You escape from the dungeon!",
        _ => throw new ArgumentOutOfRangeException(nameof(e), e.GetType().Name),
    };

    public string Status(Game game)
    {
        Player p = game.Player;
        return Fr
            ? $"Étage {game.Depth}/{Game.MaxDepth}  PV {p.Hp}/{p.MaxHp}  Niv {p.Level} ({p.Xp}/{p.XpToNextLevel})  Att {p.Attack}  Déf {p.Defense}  Potions {p.Potions}  Or {game.Gold}  Score {game.Score}"
            : $"Floor {game.Depth}/{Game.MaxDepth}  HP {p.Hp}/{p.MaxHp}  Lvl {p.Level} ({p.Xp}/{p.XpToNextLevel})  Att {p.Attack}  Def {p.Defense}  Potions {p.Potions}  Gold {game.Gold}  Score {game.Score}";
    }

    public string Keys => Fr
        ? "Flèches, zqsd ou hjkl : bouger ou attaquer   . : attendre   > : descendre   p : potion   x : quitter"
        : "Arrows, wasd or hjkl: move or attack   .: wait   >: go down   p: potion   x: quit";

    public string Legend => Fr
        ? "@ vous   r rat   g gobelin   o orque   T troll   ! potion   $ or   > escalier"
        : "@ you   r rat   g goblin   o orc   T troll   ! potion   $ gold   > stairs";

    public string Impossible(GameAction action) => (action, Fr) switch
    {
        (GameAction.Descend, true) => "Il n'y a pas d'escalier ici.",
        (GameAction.Descend, false) => "There are no stairs here.",
        (GameAction.Drink, true) => "Vous n'avez pas de potion.",
        (GameAction.Drink, false) => "You have no potion.",
        (_, true) => "Un mur vous barre la route.",
        (_, false) => "A wall blocks the way.",
    };

    public string Seed(ulong seed) => Fr ? $"Graine {seed}" : $"Seed {seed}";

    public string End(Game game) => (game.State, Fr) switch
    {
        (GameState.Escaped, true) => $"Victoire : vous vous échappez du donjon en {game.Turn} tours. Score : {game.Score}.",
        (GameState.Escaped, false) => $"Victory: you escape from the dungeon in {game.Turn} turns. Score: {game.Score}.",
        (GameState.Dead, true) => $"Vous mourez à l'étage {game.Depth}, au tour {game.Turn}. Score : {game.Score}.",
        (GameState.Dead, false) => $"You die on floor {game.Depth}, on turn {game.Turn}. Score: {game.Score}.",
        (_, true) => $"Partie abandonnée à l'étage {game.Depth}. Score : {game.Score}.",
        (_, false) => $"Run abandoned on floor {game.Depth}. Score: {game.Score}.",
    };

    public string Saved(string path) => Fr ? $"Partie enregistrée dans {path}." : $"Run saved to {path}.";

    public string Replayed(ReplayResult r)
    {
        if (!r.IsValid)
        {
            string where = r.ActionIndex is int i ? (Fr ? $" (action n° {i + 1})" : $" (action #{i + 1})") : "";
            return (Fr ? "Partie refusée : " : "Run rejected: ") + Error(r.Error) + where + ".";
        }
        string state = (r.State, Fr) switch
        {
            (GameState.Escaped, true) => "sortie du donjon",
            (GameState.Escaped, false) => "escaped from the dungeon",
            (GameState.Dead, true) => "mort",
            (GameState.Dead, false) => "died",
            (_, true) => "non terminée",
            (_, false) => "unfinished",
        };
        return Fr
            ? $"Partie valide : {state}, étage {r.Depth}, {r.Turns} tours, {r.Kills} monstres tués, score {r.Score}."
            : $"Valid run: {state}, floor {r.Depth}, {r.Turns} turns, {r.Kills} monsters killed, score {r.Score}.";
    }

    public string Error(ReplayError error) => (error, Fr) switch
    {
        (ReplayError.BadFormat, true) => "ce n'est pas une partie enregistrée",
        (ReplayError.BadFormat, false) => "this is not a recorded run",
        (ReplayError.UnsupportedVersion, true) => "version du format inconnue",
        (ReplayError.UnsupportedVersion, false) => "unknown format version",
        (ReplayError.BadSeed, true) => "graine invalide",
        (ReplayError.BadSeed, false) => "invalid seed",
        (ReplayError.TooLong, true) => "partie trop longue",
        (ReplayError.TooLong, false) => "run too long",
        (ReplayError.UnknownAction, true) => "action inconnue",
        (ReplayError.UnknownAction, false) => "unknown action",
        (ReplayError.IllegalAction, true) => "action impossible à ce moment",
        (ReplayError.IllegalAction, false) => "action not possible at that point",
        (ReplayError.ActionAfterEnd, true) => "action après la fin de la partie",
        (ReplayError.ActionAfterEnd, false) => "action after the end of the run",
        _ => throw new ArgumentOutOfRangeException(nameof(error)),
    };

    public string Stats(int runs, int escaped, double averageScore, double averageTurns, int[] deathsByFloor)
    {
        string deaths = string.Join(", ", deathsByFloor.Select((n, i) => Fr ? $"étage {i + 1} : {n}" : $"floor {i + 1}: {n}"));
        return Fr
            ? $"Pilote automatique, {runs} parties : {escaped} sorties du donjon ({100.0 * escaped / runs:F1} %), score moyen {averageScore:F0}, {averageTurns:F0} tours en moyenne.\nMorts par étage : {deaths}."
            : $"Autopilot, {runs} runs: {escaped} escaped ({100.0 * escaped / runs:F1}%), average score {averageScore:F0}, {averageTurns:F0} turns on average.\nDeaths by floor: {deaths}.";
    }

    public string Usage => Fr
        ? """
          Usage : rogue [options]
            --seed N         graine du donjon (au hasard sinon)
            --lang fr|en     langue (fr par défaut)
            --save FICHIER   enregistre la partie à la fin (graine et actions, JSON)
            --bot            laisse jouer le pilote automatique
            --replay FICHIER rejoue une partie enregistrée et vérifie son score
            --stats N        fait jouer N parties au pilote automatique (graines 0 à N-1)
            --help           cette aide
          """
        : """
          Usage: rogue [options]
            --seed N         dungeon seed (random otherwise)
            --lang fr|en     language (fr by default)
            --save FILE      saves the run at the end (seed and actions, JSON)
            --bot            lets the autopilot play
            --replay FILE    replays a recorded run and checks its score
            --stats N        has the autopilot play N runs (seeds 0 to N-1)
            --help           this help
          """;

    public string BadOption(string detail) => (Fr ? "Option invalide : " : "Invalid option: ") + detail;

    public string CannotRead(string path, string reason) => Fr ? $"Lecture impossible de {path} : {reason}" : $"Cannot read {path}: {reason}";
}
