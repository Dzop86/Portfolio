using Rpg.Core;

namespace Rpg.Api.Data;

/// <summary>A player's account: a name, a password hash (PBKDF2, ASP.NET Core Identity's hasher), its characters.</summary>
public sealed class Account
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public required string Name { get; set; }

    /// <summary>The name in upper case: two accounts cannot differ only by case.</summary>
    public required string NormalizedName { get; set; }

    public string PasswordHash { get; set; } = "";
    public DateTimeOffset CreatedAt { get; set; }

    /// <summary>What the player has seen once, comma-separated (sprint 149: the tree's tutorial), so as not to show it again.</summary>
    public string Seen { get; set; } = "";

    public List<Character> Characters { get; } = [];
}

/// <summary>
/// A character: unique name over the whole server (case aside), a look among the playable ones, a
/// class of <c>data/classes.json</c> and an outfit colour.
/// </summary>
public sealed class Character
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid AccountId { get; set; }
    public Account? Account { get; set; }
    public required string Name { get; set; }
    public required string NormalizedName { get; set; }
    public required string Look { get; set; }
    public required string Class { get; set; }
    public int Colour { get; set; }

    /// <summary>The game server the character lives on.</summary>
    public string Server { get; set; } = Servers.Default;

    public int Level { get; set; } = 1;

    /// <summary>The appearance beyond look and colour: hair colour, skin tone, height, build (0: as drawn).</summary>
    public int Hair { get; set; }
    public int Skin { get; set; }
    public int Height { get; set; }
    public int Build { get; set; }

    /// <summary>Where the character stands: a town and a cell, or nothing before their first walk.</summary>
    public string? Town { get; set; }
    public int? X { get; set; }
    public int? Y { get; set; }
    public DateTimeOffset CreatedAt { get; set; }

    /// <summary>When its game last connected (sprint 145: the guild's tab tells it); null before.</summary>
    public DateTimeOffset? LastSeenAt { get; set; }

    /// <summary>Experience, earned only from fights the server replayed; the level follows it.</summary>
    public long Xp { get; set; }

    /// <summary>The characteristic points spent (sprint 58).</summary>
    public int Vitality { get; set; }
    public int Earth { get; set; }
    public int Fire { get; set; }
    public int Water { get; set; }
    public int Air { get; set; }

    /// <summary>The ranks of the spells above 1, as "spell:rank" separated by commas.</summary>
    public string Ranks { get; set; } = "";

    /// <summary>The quests done, their ids separated by commas.</summary>
    public string Quests { get; set; } = "";

    /// <summary>The quests under way and their progress (sprint 88), as <see cref="Rpg.Core.Quests.Write"/> writes them.</summary>
    public string QuestLog { get; set; } = "";

    // Fitted to the quests as they are now (sprint 105): a quest given more objectives since keeps its counts.
    public List<QuestProgress> ActiveQuests => Rpg.Core.Quests.Fit(GameData.Embedded, Rpg.Core.Quests.Read(QuestLog));

    /// <summary>Everything owned, worn included, as "item:count" separated by commas.</summary>
    public string Inventory { get; set; } = "";

    /// <summary>What is worn, as "Slot:item" separated by commas.</summary>
    public string Worn { get; set; } = "";

    /// <summary>The spells fought with, in the order of the bar, their ids separated by commas (sprint 68); empty: the default deck.</summary>
    public string Deck { get; set; } = "";

    /// <summary>The talents of the tree taken (sprint 137, D80), their ids in order separated by commas.</summary>
    public string Talents { get; set; } = "";

    /// <summary>
    /// The version of the tree its talents were taken in (sprint 146, D82): those of an older one are given back once, for
    /// free (<see cref="Rpg.Core.Talents.Version"/>); characters made before have the first.
    /// </summary>
    public int TalentsVersion { get; set; } = Rpg.Core.Talents.Version;

    /// <summary>The talents, without those the tree no longer has.</summary>
    public string[] TalentList => [.. Talents.Split(',', StringSplitOptions.RemoveEmptyEntries).Where(GameData.Embedded.Talents.ContainsKey)];

    /// <summary>The experience of each trade (sprint 81), as "job:xp" separated by commas.</summary>
    public string Jobs { get; set; } = "";

    public Dictionary<string, long> JobList => Jobs.Split(',', StringSplitOptions.RemoveEmptyEntries)
        .Select(j => j.Split(':')).ToDictionary(j => j[0], j => long.Parse(j[1], System.Globalization.CultureInfo.InvariantCulture), StringComparer.Ordinal);

    /// <summary>The waystones it discovered (sprint 127), their ids separated by commas.</summary>
    public string Waystones { get; set; } = "";

    /// <summary>
    /// The feats the achievements read that nothing else keeps (sprint 155, D83): a fight in a group, a trade, a great horde
    /// at four; their ids separated by commas.
    /// </summary>
    public string Feats { get; set; } = "";

    public string[] FeatList => Feats.Split(',', StringSplitOptions.RemoveEmptyEntries);

    /// <summary>A feat done (sprint 155): kept once.</summary>
    public void Feat(string feat)
    {
        if (!FeatList.Contains(feat))
            Feats = string.Join(',', FeatList.Append(feat));
    }

    /// <summary>The fights won with others (sprint 155).</summary>
    public int SharedWins { get; set; }

    /// <summary>The notice board's contracts handed back (sprint 155).</summary>
    public int ContractsDone { get; set; }

    /// <summary>The title worn (sprint 155): the id of the achievement that gives it, or none.</summary>
    public string? Title { get; set; }

    /// <summary>Its bestiary (sprint 154, D83): the victories over each monster, a monster met at 0 (<see cref="Rpg.Core.Bestiary.Write"/>).</summary>
    public string Bestiary { get; set; } = "";

    public Dictionary<string, int> BestiaryList => Rpg.Core.Bestiary.Read(Bestiary);

    /// <summary>Its notice board (sprint 153, D83): the contracts offered and how far they went, as JSON (<see cref="Rpg.Core.Contracts.Write"/>).</summary>
    public string Contracts { get; set; } = "";

    public IReadOnlyList<string> WaystoneList => Waystones.Split(',', StringSplitOptions.RemoveEmptyEntries);

    /// <summary>The waystone the recall scroll takes it to (sprint 127); null: the village's.</summary>
    public string? Home { get; set; }

    /// <summary>The waystones within reach of where it stands now added to those it knows (sprint 127).</summary>
    public void Discover()
    {
        if (Town is string town && X is int x && Y is int y)
            Waystones = string.Join(',', WaystoneList.Union(Rpg.Core.Waystones.Near(town, new Cell(x, y), GameData.Embedded)).Order(StringComparer.Ordinal));
    }

    /// <summary>The dungeon the character is in (sprint 80), and the room it is to fight there; null and 0 outside.</summary>
    public string? Dungeon { get; set; }
    public int DungeonRoom { get; set; }

    /// <summary>The Éclats it owns, the money (sprint 87).</summary>
    public long Shards { get; set; }

    /// <summary>
    /// Its life (sprint 101, D73): the hit points last set (null: all of them), when, and whether a fight was
    /// taken since (the life frozen until its report). It grows back from there (<see cref="Life.Now"/>).
    /// </summary>
    public int? Hp { get; set; }
    public DateTimeOffset? HpAt { get; set; }
    public bool InFight { get; set; }

    /// <summary>The hit points now.</summary>
    public int LifeNow(DateTimeOffset now) => Life.Now(Life.MaxHp(Hero, GameData.Embedded), Hp, HpAt, InFight, Town, now);

    /// <summary>Sets the life at a moment (out of a fight): all of them as null; to the millisecond, as the database keeps it.</summary>
    public void SetLife(int hp, DateTimeOffset now)
    {
        Hp = hp >= Life.MaxHp(Hero, GameData.Embedded) ? null : Math.Max(1, hp);
        HpAt = Hp is null ? null : new DateTimeOffset(now.Ticks - now.Ticks % TimeSpan.TicksPerMillisecond, now.Offset);
        InFight = false;
    }

    /// <summary>Sets the life where it has grown to now (before the map, the equipment or the level changes it).</summary>
    public void KeepLife(DateTimeOffset now) => SetLife(LifeNow(now), now);

    public DungeonRun? Run => Dungeon is string d ? new DungeonRun(d, DungeonRoom) : null;

    /// <summary>The deck, without the spells the class no longer has.</summary>
    /// <summary>"" when the character never chose (the class's first spells), "-" for a deck chosen empty (D66).</summary>
    public const string EmptyDeck = "-";

    public string[]? DeckList => Deck.Length == 0 ? null
        : [.. Deck.Split(',', StringSplitOptions.RemoveEmptyEntries).Where(id => GameData.Embedded.Spells.TryGetValue(id, out Spell? s)
            && (GameData.Embedded.Class(Class)?.Spells.Contains(s.RankKey) == true || GameData.Embedded.Talents.Values.Any(t => t.Rules.OfType<GrantRule>().Any(g => g.Spell == id))))];

    public List<ItemCount> InventoryList => [.. Inventory.Split(',', StringSplitOptions.RemoveEmptyEntries)
        .Select(i => i.Split(':')).Select(i => new ItemCount(i[0], int.Parse(i[1], System.Globalization.CultureInfo.InvariantCulture)))];

    public Dictionary<Slot, string> WornList => Worn.Split(',', StringSplitOptions.RemoveEmptyEntries)
        .Select(w => w.Split(':')).ToDictionary(w => Enum.Parse<Slot>(w[0]), w => w[1]);

    /// <summary>Takes items out of the inventory (a key used up); those down to none go away.</summary>
    public void Spend(ItemCount used)
    {
        ArgumentNullException.ThrowIfNull(used);
        var all = InventoryList.ToDictionary(i => i.Item, i => i.Count, StringComparer.Ordinal);
        all[used.Item] = all.GetValueOrDefault(used.Item) - used.Count;
        Inventory = string.Join(',', all.Where(i => i.Value > 0).OrderBy(i => i.Key, StringComparer.Ordinal).Select(i => $"{i.Key}:{i.Value.ToString(System.Globalization.CultureInfo.InvariantCulture)}"));
    }

    /// <summary>Adds items to the inventory, in the order of the items' ids.</summary>
    public void Receive(IEnumerable<ItemCount> items)
    {
        var all = InventoryList.ToDictionary(i => i.Item, i => i.Count, StringComparer.Ordinal);
        foreach (ItemCount i in items)
            all[i.Item] = all.GetValueOrDefault(i.Item) + i.Count;
        Inventory = string.Join(',', all.OrderBy(i => i.Key, StringComparer.Ordinal).Select(i => $"{i.Key}:{i.Value.ToString(System.Globalization.CultureInfo.InvariantCulture)}"));
    }

    public Characteristics Stats => new(Vitality, Earth, Fire, Water, Air);

    /// <summary>
    /// The spell ranks bought, those of spells the class still has only: when the classes' spells were
    /// redone (D62), the points put in a spell that went away came back to be spent again.
    /// </summary>
    public Dictionary<string, int> RankList => Ranks.Split(',', StringSplitOptions.RemoveEmptyEntries)
        .Select(r => r.Split(':'))
        .Where(r => GameData.Embedded.Class(Class)?.Spells.Contains(r[0]) == true)
        .ToDictionary(r => r[0], r => int.Parse(r[1], System.Globalization.CultureInfo.InvariantCulture), StringComparer.Ordinal);

    public string[] QuestList => Quests.Split(',', StringSplitOptions.RemoveEmptyEntries);

    /// <summary>The character as the rules see it.</summary>
    public Hero Hero => new(Name, Look, Class, Colour, Hair, Skin, Height, Build, Level, Stats, RankList, WornList, DeckList, TalentList.Length == 0 ? null : TalentList);
}

/// <summary>
/// A fight the server allowed a character to play: the scenario and the seed it drew. Its record
/// comes back once, is replayed, and the ticket is gone: the same fight cannot earn twice.
/// </summary>
public sealed class PendingFight
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid CharacterId { get; set; }
    public Character? Character { get; set; }
    public required string Scenario { get; set; }
    public long Seed { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}

/// <summary>
/// A fight with the party under way (sprint 125, D79), written as it goes so that a restart of the server loses no
/// fight: its start as each game got it (<see cref="Start"/>, JSON), the build of the rules that played it
/// (<see cref="Engine"/>), and its actions, numbered, added one by one. Gone once the fight is over.
/// </summary>
public sealed class BattleRecord
{
    public Guid Id { get; set; }
    public required string Scenario { get; set; }
    public required string Map { get; set; }
    public Guid Starter { get; set; }
    public required string Start { get; set; }
    public required string Engine { get; set; }
    public DateTimeOffset StartedAt { get; set; }
    public List<BattleActionRecord> Actions { get; } = [];
}

/// <summary>One action of a fight with the party (sprint 125), by its number from 0, as JSON.</summary>
public sealed class BattleActionRecord
{
    public Guid BattleId { get; set; }
    public int Number { get; set; }
    public required string Action { get; set; }
}

/// <summary>
/// One of an account's contacts (sprint 126, D79): a character it follows as a friend, or ignores, whichever of its
/// own characters it plays.
/// </summary>
public sealed class ContactRecord
{
    public Guid AccountId { get; set; }
    public Guid CharacterId { get; set; }
    public bool Ignore { get; set; }
}

/// <summary>A guild (sprint 145, D79): its name, unique over the server (case aside), and when it was founded.</summary>
public sealed class GuildRecord
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public required string Name { get; set; }
    public required string NormalizedName { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}

/// <summary>A character in a guild (sprint 145): one guild at most, its rank, when it joined (who leads after the leader).</summary>
public sealed class GuildMemberRecord
{
    public Guid CharacterId { get; set; }
    public Guid GuildId { get; set; }
    public GuildRank Rank { get; set; } = GuildRank.Member;
    public DateTimeOffset JoinedAt { get; set; }
}
