using Rpg.Core;

namespace Rpg.Client;

/// <summary>One notice of the board as the game shows it (sprint 153): its title and text, how far it went, its reward, what one may do.</summary>
public sealed record ContractLine(string Title, string Text, string Progress, string Reward, bool Group, bool CanHandIn, string Change);

/// <summary>
/// The notice board as the game shows it (sprint 153, D83), without any engine: each contract written from the
/// screenwriter's models (docs/textes-contrats.md), its target named in the player's language, the items owned counted for
/// a gathering or a making, the price of a change.
/// </summary>
public static class ContractsPage
{
    public static IReadOnlyList<ContractLine> Lines(ContractBoard board, int level, IReadOnlyList<ItemCount> owned, Texts texts, GameData data)
    {
        ArgumentNullException.ThrowIfNull(board);
        ArgumentNullException.ThrowIfNull(owned);
        ArgumentNullException.ThrowIfNull(texts);
        ArgumentNullException.ThrowIfNull(data);
        return [.. board.Offered.Select(c =>
        {
            string kind = c.Kind.ToString().ToLowerInvariant();
            string target = TargetName(c, texts.Lang, data), zone = ZoneOf(c, texts, data);
            long price = Contracts.ChangePrice(board, c, level);
            return new ContractLine(texts[$"contract.{kind}.title", c.Count, target], texts[$"contract.{kind}.text", c.Count, target, zone],
                texts["contracts.progress", Contracts.Reached(c, owned), c.Count], texts["contracts.reward", c.Xp, c.Shards], c.IsGroup,
                Contracts.IsComplete(c, owned), price == 0 ? texts["contracts.swapFree"] : texts["contracts.swapPaid", price]);
        })];
    }

    /// <summary>
    /// The contract's target in a sentence: a family's name lowered (« Les sangliers »), an item's lowered, a dungeon's as
    /// it is (a proper name).
    /// </summary>
    public static string TargetName(Contract contract, string lang, GameData data)
    {
        ArgumentNullException.ThrowIfNull(contract);
        ArgumentNullException.ThrowIfNull(data);
        string Lowered(string s) => s.Length == 0 ? s : char.ToLowerInvariant(s[0]) + s[1..];
        return contract.Kind switch
        {
            ContractKind.Hunt or ContractKind.Horde => data.Families.TryGetValue(contract.Target, out MonsterFamily? f) ? Lowered(f.Name.In(lang)) : contract.Target,
            ContractKind.Dungeon or ContractKind.Boss => data.Dungeons.TryGetValue(contract.Target, out Dungeon? d) ? d.Name.In(lang) : contract.Target,
            _ => data.Items.TryGetValue(contract.Target, out Item? i) ? Lowered(i.Name.In(lang)) : contract.Target,
        };
    }

    // The zone of a hunt's family, named; of a dungeon's, too; none for the others.
    private static string ZoneOf(Contract c, Texts texts, GameData data) =>
        c.Kind is ContractKind.Hunt or ContractKind.Horde && data.Families.TryGetValue(c.Target, out MonsterFamily? f) ? texts.ZoneName(f.Zone, data)
        : c.Kind is ContractKind.Dungeon or ContractKind.Boss && data.Dungeons.TryGetValue(c.Target, out Dungeon? d) ? texts.ZoneName(data.Families[d.Family].Zone, data)
        : "";
}
