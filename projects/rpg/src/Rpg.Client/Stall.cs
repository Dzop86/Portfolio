using Rpg.Core;

namespace Rpg.Client;

/// <summary>A line of Rose's stall: the item, its title with the price, how many can be bought or sold now (0: why not).</summary>
public sealed record StallRow(Item Item, string Title, long Price, int Most, string? Refusal);

/// <summary>
/// Rose's stall (sprint 97, D71) without an engine: the Éclats owned, what she sells and what she would buy
/// of the character's, each with its price and how many the Éclats or the free items allow. The Godot
/// client draws it; the server decides (<see cref="GameServer.Buy"/>, <see cref="GameServer.Sell"/>).
/// </summary>
public sealed class Stall(CharacterSummary? character, Texts texts)
{
    public long Shards => character?.Shards ?? 0;

    public string Title => texts["shop.title", Shards];

    /// <summary>Rose's stock: the monsters' resources, at five times what she pays.</summary>
    public IReadOnlyList<StallRow> ToBuy =>
        [.. Shop.Stock(GameData.Embedded).Select(item =>
        {
            long price = Shop.SellPrice(item, GameData.Embedded)!.Value;
            int most = (int)Math.Min(Shop.MostAtOnce, Shards / price);
            string? refusal = character is null ? texts["craft.offline"] : most == 0 ? texts["shop.poor"] : null;
            return new StallRow(item, texts["shop.row", item.Name.In(texts.Lang), price], price, refusal is null ? most : 0, refusal);
        })];

    /// <summary>What she would buy of the character's: resources and equipment, the worn ones kept.</summary>
    public IReadOnlyList<StallRow> ToSell
    {
        get
        {
            IReadOnlyList<ItemCount> owned = character?.Inventory ?? [];
            IReadOnlyDictionary<Slot, string> worn = character?.Worn ?? new Dictionary<Slot, string>();
            return [.. owned.Where(o => GameData.Embedded.Items.TryGetValue(o.Item, out Item? i) && Shop.BuyPrice(i, GameData.Embedded) is not null).Select(o =>
            {
                Item item = GameData.Embedded.Items[o.Item];
                long price = Shop.BuyPrice(item, GameData.Embedded)!.Value;
                int free = Math.Min(Shop.MostAtOnce, o.Count - worn.Values.Count(w => w == o.Item));
                return new StallRow(item, texts["shop.row-owned", item.Name.In(texts.Lang), price, o.Count], price, Math.Max(0, free), free > 0 ? null : texts["shop.worn"]);
            })];
        }
    }
}
