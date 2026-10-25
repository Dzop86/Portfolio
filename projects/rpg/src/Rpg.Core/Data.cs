using System.Text.Json;
using System.Text.Json.Serialization;

namespace Rpg.Core;

/// <summary>A text shown to the player, in both languages of the site.</summary>
public sealed record LocalizedText(string Fr, string En)
{
    public string In(string lang) => lang == "fr" ? Fr : En;
}

/// <summary>
/// A spell, as described in <c>data/spells.json</c>. Ranges count steps (see <see cref="Cell"/>);
/// a spell may be cast on an empty cell, it then hits nobody. Its damage strikes the enemies in its
/// <see cref="Area"/> (one cell by default) in its <see cref="Element"/>; its other
/// <see cref="Effects"/> follow, in order. After a cast, the caster waits <see cref="Cooldown"/> of
/// its turns before casting it again (0: no wait, only the limit per turn).
/// </summary>
public sealed record Spell(
    string Id,
    LocalizedText Name,
    int ApCost,
    int MinRange,
    int MaxRange,
    bool LineOfSight,
    bool InLine,
    int DamageMin,
    int DamageMax,
    int PerTurn,
    Element Element = Element.Neutral,
    Area? Area = null,
    IReadOnlyList<SpellEffect>? Effects = null,
    int Level = 1,
    IReadOnlyList<SpellRank>? Ranks = null,
    int Cooldown = 0,
    int Crit = 0,
    string? Icon = null,
    string? Twin = null)
{
    /// <summary>The class spell a compound's spell is transposed from (sprint 138), or the spell itself: whose rank it has.</summary>
    public string RankKey => Twin ?? Id;

    /// <summary>
    /// What a critical hit adds to both ends of a range: a fifth of its top, rounded (Charles's examples:
    /// 5 to 10 becomes 7 to 12, 22 to 26 becomes 27 to 31).
    /// </summary>
    public static int CritBonus(int max) => (int)Math.Round(max * 0.2, MidpointRounding.AwayFromZero);

    /// <summary>The highest rank: 1 plus the ranks listed.</summary>
    public int MaxRank => 1 + (Ranks?.Count ?? 0);

    /// <summary>The spell at a rank (1 is as described; above, the rank's numbers replace the base ones).</summary>
    public Spell AtRank(int rank)
    {
        if (rank <= 1 || Ranks is null)
            return this;
        SpellRank r = Ranks[Math.Min(rank, MaxRank) - 2];
        return this with
        {
            DamageMin = r.DamageMin,
            DamageMax = r.DamageMax,
            ApCost = r.ApCost ?? ApCost,
            MaxRange = r.MaxRange ?? MaxRange,
            PerTurn = r.PerTurn ?? PerTurn,
        };
    }

    public double AverageDamage => (DamageMin + DamageMax) / 2.0;

    public Area Zone => Area ?? Core.Area.One;

    public IReadOnlyList<SpellEffect> AllEffects => Effects ?? [];
}

/// <summary>What a spell's rank 2 to 5 changes: its damage, and if given its cost, range and casts per turn.</summary>
public sealed record SpellRank(int DamageMin, int DamageMax, int? ApCost = null, int? MaxRange = null, int? PerTurn = null);

/// <summary>
/// The five characteristics: Vitality adds hit points, each other one adds 1 % of damage per point
/// in its element (Earth: earth and neutral; Fire: fire, and healing; Water: water;
/// Air: air).
/// </summary>
public sealed record Characteristics(int Vitality = 0, int Earth = 0, int Fire = 0, int Water = 0, int Air = 0)
{
    public static readonly Characteristics None = new();

    /// <summary>What points in a characteristic add to each hit of its element: 2 for every 10 (D61), and nothing else (D62).</summary>
    public static int Bonus(int points) => points / 10 * 2;

    /// <summary>
    /// The characteristic that strengthens an element's damage. A compound strikes on the highest of its parents', or on
    /// twice their average when <paramref name="converted"/> (sprint 144, docs/elements.md §3.4: "with the sum, a mere
    /// opening would give everyone full strength for 1 point").
    /// </summary>
    public int For(Element element, bool converted = false) => element switch
    {
        Element.Earth => Earth,
        Element.Fire => Fire,
        Element.Water => Water,
        Element.Air => Air,
        // Neutral damage grows with no characteristic (D62).
        _ => Elements.Parents(element) is { Count: > 0 } parents
            ? converted ? 2 * parents.Sum(e => For(e)) / parents.Count : parents.Max(e => For(e))
            : 0,
    };
}

/// <summary>
/// A quest, as described in <c>data/quests.json</c>, worth <see cref="Xp"/> (and <see cref="Shards"/>,
/// <see cref="Items"/>) once. Either done the first time the hero wins a fight of its
/// <see cref="Scenario"/>, which the server checks by replaying that fight (the first lesson); or, since
/// sprint 88, given by an inhabitant (<see cref="Giver"/>, "town/npc") from a level and after other
/// quests (<see cref="After"/>), its <see cref="Objectives"/> followed all at once, then handed back to
/// the giver. <see cref="Zone"/> sorts it in the journal, <see cref="Kind"/> gives it its icon there (sprint 116, D77).
/// </summary>
public sealed record Quest(string Id, LocalizedText Name, long Xp, string? Scenario = null, IReadOnlyList<ItemCount>? Items = null,
    string? Giver = null, string? Zone = null, LocalizedText? Text = null, LocalizedText? Thanks = null, IReadOnlyList<QuestObjective>? Objectives = null,
    long Shards = 0, IReadOnlyList<string>? After = null, int Level = 1, IReadOnlyList<ItemCount>? Grants = null, string? HandIn = null,
    QuestKind Kind = QuestKind.Side)
{
    /// <summary>Who takes the quest back ("town/npc"): <see cref="HandIn"/> when set (sprint 107: Aubin's welcome ends with Garance), else its giver.</summary>
    public string? Taker => HandIn ?? Giver;

    /// <summary>Given by someone and handed back, rather than done by a fight.</summary>
    public bool IsGiven => Giver is not null;
}

/// <summary>
/// What sort of quest it is, for the journal (sprint 116, D77): the village's first steps, a step of the main quest,
/// a side quest, a trade's first piece, a dungeon to finish.
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter<QuestKind>))]
public enum QuestKind
{
    Tutorial,
    Main,
    Side,
    Trade,
    Dungeon,
}

/// <summary>
/// What a quest asks (sprint 88): to talk to someone ("town/npc"), to defeat monsters of a family, to bring
/// items (taken when handed back), to finish a dungeon; since sprint 105 (D75), to examine a clue on a map
/// (a <see cref="Prop"/>), to win a fight (a scenario, started from a talk), to wear an item.
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter<QuestGoal>))]
public enum QuestGoal
{
    Talk,
    Defeat,
    Bring,
    Dungeon,
    Interact,
    Win,
    Wear,
}

/// <summary>
/// One thing a quest asks: its goal, its target (an inhabitant, a family, an item, a dungeon, a prop, a scenario), how many;
/// for a dungeon, the room to win (sprint 95, D71: 0 = finish it), so that the main quest is done alone; for a talk,
/// what the one talked to says while the objective is under way (sprint 107, <c>docs/textes-v5.md</c> §10.1).
/// </summary>
public sealed record QuestObjective(QuestGoal Goal, string Target, int Count = 1, int Room = 0, LocalizedText? Line = null);

/// <summary>
/// A creature a spell or a passive can summon, as described in <c>data/summons.json</c>; a monster's
/// young have passives and are drawn at <see cref="Scale"/> percent of their size (sprint 90).
/// </summary>
/// <param name="HpOfSummoner">A Decoy's (sprint 142): its hit points, this percent of its summoner's maximum (0: <paramref name="Hp"/>).</param>
public sealed record SummonSpec(string Id, LocalizedText Name, string Look, int Hp, int Ap, int Mp, int Initiative, IReadOnlyList<string> Spells,
    IReadOnlyList<string>? Passives = null, int Scale = 100, int HpOfSummoner = 0);

/// <summary>
/// A fighter of a scenario: its look (the 3D model the client shows; the rules ignore it), its team
/// (0 or 1), its characteristics and the index of its starting cell. A monster's carries its passives
/// and its family (sprint 90), which an ally's death reads; a hero's, its talents (sprint 137); a family's
/// chief, <see cref="Chief"/>: the reactions take it at most 2 action and 2 movement points.
/// </summary>
public sealed record FighterSpec(
    LocalizedText Name,
    string Look,
    int Team,
    int Hp,
    int Ap,
    int Mp,
    int Initiative,
    int Start,
    IReadOnlyList<string> Spells,
    IReadOnlyDictionary<Element, int>? Resistances = null,
    Characteristics? Stats = null,
    IReadOnlyDictionary<string, int>? SpellRanks = null,
    int Level = 1,
    IReadOnlyList<LootDrop>? Loot = null,
    int Power = 100,
    IReadOnlyList<string>? Passives = null,
    string? Family = null,
    IReadOnlyList<string>? Talents = null,
    bool Chief = false,
    int TreeDamage = 0)
{
    public Characteristics Characteristics => Stats ?? Characteristics.None;

    /// <summary>
    /// The fighter's resistance to an element, in percent (neutral damage ignores resistances); to a compound it is not
    /// given, its parents' average (sprint 138).
    /// </summary>
    public int Resistance(Element element) => element == Element.Neutral ? 0
        : Resistances?.TryGetValue(element, out int r) == true ? r
        : Elements.Parents(element) is { Count: > 0 } parents ? parents.Sum(Resistance) / parents.Count
        : 0;
}

/// <summary>
/// A class the player can choose for their hero, as described in <c>data/classes.json</c>: its
/// characteristics and spells replace those of the scenario's hero; the hero gains
/// <see cref="HpPerLevel"/> hit points with each level and gets the spells its level has unlocked.
/// </summary>
public sealed record HeroClass(
    string Id,
    LocalizedText Name,
    LocalizedText Description,
    int Hp,
    int Ap,
    int Mp,
    int Initiative,
    IReadOnlyList<string> Spells,
    int HpPerLevel = Fight.HpPerLevel);

/// <summary>A board, as described in <c>data/maps/*.json</c>.</summary>
public sealed record MapSpec(string Id, LocalizedText Name, IReadOnlyList<string> Rows);

/// <summary>A fight to play: a map and its fighters, as described in <c>data/scenarios/*.json</c>.</summary>
public sealed record Scenario(string Id, LocalizedText Name, string Map, IReadOnlyList<FighterSpec> Fighters);

/// <summary>
/// Everything the rules read from the data folder: spells, maps, scenarios, the heroes' classes,
/// towns and dialogues. Adding a spell, a map, a monster, a class, a town or a line of dialogue is a
/// change of data, not of code; <see cref="Load"/> refuses inconsistent data.
/// </summary>
public sealed class GameData
{
    internal static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web)
    {
        RespectNullableAnnotations = true,
        RespectRequiredConstructorParameters = true,
        UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow,
        WriteIndented = true,
    };

    public IReadOnlyDictionary<string, Spell> Spells { get; }
    public IReadOnlyDictionary<string, MapSpec> Maps { get; }
    public IReadOnlyDictionary<string, Scenario> Scenarios { get; }

    /// <summary>The classes, in the order of the data file (the order the creation screen shows).</summary>
    public IReadOnlyList<HeroClass> Classes { get; }

    public IReadOnlyDictionary<string, Town> Towns { get; }
    public IReadOnlyDictionary<string, Dialogue> Dialogues { get; }
    public IReadOnlyDictionary<string, SummonSpec> Summons { get; }
    public IReadOnlyDictionary<string, Quest> Quests { get; }
    public IReadOnlyDictionary<string, Item> Items { get; }
    public IReadOnlyDictionary<string, ItemSet> Sets { get; }

    /// <summary>The families of monsters (sprint 72), each monster also by its id, and the groups they form on the maps, by id.</summary>
    public IReadOnlyDictionary<string, MonsterFamily> Families { get; }
    public IReadOnlyDictionary<string, MonsterSpec> Monsters { get; }
    public IReadOnlyDictionary<string, MonsterGroup> Groups { get; }

    /// <summary>The gathering spots of every map (sprint 99), by id.</summary>
    public IReadOnlyDictionary<string, HarvestNode> Nodes { get; }

    /// <summary>The trades (sprint 81), by id.</summary>
    public IReadOnlyDictionary<string, Job> Jobs { get; }

    /// <summary>The pieces of equipment a trade makes (sprint 109, D76): the craftsmen's pieces.</summary>
    public IEnumerable<Item> MadePieces => Jobs.Values.SelectMany(j => j.Recipes).Select(r => Items[r.Item]).Where(i => i.Kind == ItemKind.Equipment);

    /// <summary>The dungeons (sprint 80), by id.</summary>
    public IReadOnlyDictionary<string, Dungeon> Dungeons { get; }

    /// <summary>The monsters' passives (sprint 90), by id.</summary>
    public IReadOnlyDictionary<string, PassiveSpec> Passives { get; }

    /// <summary>The talents of the tree shared by every class (sprint 137, D80), by id, in the data file's order.</summary>
    public IReadOnlyDictionary<string, TalentSpec> Talents { get; }

    /// <summary>The elemental reactions (sprint 137), by id.</summary>
    public IReadOnlyDictionary<string, ReactionSpec> Reactions { get; }

    private readonly Dictionary<string, Scenario> _groupFights = new(StringComparer.Ordinal);

    /// <summary>The groups of monsters living on a map, in their order.</summary>
    public IReadOnlyList<MonsterGroup> GroupsOn(string mapId) => [.. Groups.Values.Where(g => g.Map == mapId)];

    /// <summary>
    /// A scenario of the data files, the fight against a group of monsters ("group:" and its id) or a
    /// dungeon's room ("dungeon:", its id, ":" and the room); null if none.
    /// </summary>
    public Scenario? FindScenario(string id) =>
        Scenarios.TryGetValue(id, out Scenario? s) ? s : _groupFights.GetValueOrDefault(id);

    public GameData(IEnumerable<Spell> spells, IEnumerable<MapSpec> maps, IEnumerable<Scenario> scenarios, IEnumerable<HeroClass>? classes = null,
        IEnumerable<Town>? towns = null, IEnumerable<Dialogue>? dialogues = null, IEnumerable<SummonSpec>? summons = null, IEnumerable<Quest>? quests = null,
        IEnumerable<Item>? items = null, IEnumerable<ItemSet>? sets = null, IEnumerable<MonsterFamily>? families = null,
        IEnumerable<Dungeon>? dungeons = null, IEnumerable<Job>? jobs = null, IEnumerable<PassiveSpec>? passives = null,
        IEnumerable<TalentSpec>? talents = null, IEnumerable<ReactionSpec>? reactions = null)
    {
        Items = Index(items ?? [], i => i.Id, "item");
        Sets = Index(sets ?? [], s => s.Id, "set");
        foreach (Item i in Items.Values)
        {
            if ((i.Kind == ItemKind.Equipment) != (i.Slot is not null) || i.Level is < 1 or > Hero.MaxLevel || (i.Set is not null && !Sets.ContainsKey(i.Set)))
                throw new InvalidDataException($"Item '{i.Id}': equipment has a slot and nothing else does, a level 1 to {Hero.MaxLevel}, a known set.");
            // Food (sprint 101): a consumable that heals out of a fight.
            if (i.Heal < 0 || (i.Heal > 0 && i.Kind != ItemKind.Consumable))
                throw new InvalidDataException($"Item '{i.Id}': only a consumable heals.");
            if (i.Initiative < 0 || (i.Initiative > 0 && (i.Slot is not ItemSlot slot || !Item.Quick.Contains(slot))))
                throw new InvalidDataException($"Item '{i.Id}': initiative on boots, a cape or a ring only.");
            // The budget (sprint 109, D76), checked on loading since sprint 128: every new piece up to level 45 keeps to it.
            if (!Templates.WithinBudget(i))
                throw new InvalidDataException($"Item '{i.Id}': {Templates.Spent(i)} points over its budget of {Templates.Budget(i.Level, i.Slot!.Value)} at level {i.Level}.");
        }
        foreach (ItemSet s in Sets.Values)
        {
            if (s.Bonuses.Count == 0 || s.Bonuses.Any(b => b.Pieces < 2 || b.Initiative < 0))
                throw new InvalidDataException($"Set '{s.Id}': bonuses from two pieces up.");
        }
        Spells = Index(spells, s => s.Id, "spell");
        Maps = Index(maps, m => m.Id, "map");
        Scenarios = Index(scenarios, s => s.Id, "scenario");
        Classes = [.. Index(classes ?? [], c => c.Id, "class").Values];
        Summons = Index(summons ?? [], s => s.Id, "summon");
        Passives = Index(passives ?? [], p => p.Id, "passive");
        foreach (PassiveSpec p in Passives.Values)
            Check(p);
        Reactions = Index(reactions ?? [], r => r.Id, "reaction");
        Talents = Index(talents ?? [], t => t.Id, "talent");
        CheckTalents();
        foreach (Spell s in Spells.Values)
        {
            Check(s);
            // A compound's spell (sprint 138): transposed from a spell of one of its parents, unlocked at the same level.
            if (s.Twin is string twin && (!Spells.TryGetValue(twin, out Spell? origin) || origin.Twin is not null
                || !Elements.Parents(s.Element).Contains(origin.Element) || origin.Level != s.Level))
                throw new InvalidDataException($"Spell '{s.Id}': the twin of a known spell of one of its element's parents, at its level.");
            foreach (SummonEffect summon in s.AllEffects.OfType<SummonEffect>())
            {
                if (!Summons.ContainsKey(summon.Summon))
                    throw new InvalidDataException($"Spell '{s.Id}': unknown summon '{summon.Summon}'.");
            }
        }
        foreach (SummonSpec m in Summons.Values)
        {
            if (m.Hp < 1 || m.Ap < 0 || m.Mp < 0 || m.Spells.Any(id => !Spells.ContainsKey(id)) || m.Scale is < 10 or > 200)
                throw new InvalidDataException($"Summon '{m.Id}': hit points, points, spells or scale out of bounds.");
            CheckPassives($"Summon '{m.Id}'", m.Passives);
        }
        foreach (Scenario s in Scenarios.Values)
            Check(s);
        foreach (HeroClass c in Classes)
            Check(c);
        Dialogues = Index(dialogues ?? [], d => d.Id, "dialogue");
        Towns = Index(towns ?? [], t => t.Id, "town");
        foreach (Dialogue d in Dialogues.Values)
            Check(d);
        foreach (Town t in Towns.Values)
            Check(t);
        // A thing to examine is the world's only one of its id (sprint 105).
        if (Towns.Values.SelectMany(t => t.Props ?? []).GroupBy(p => p.Id).FirstOrDefault(g => g.Count() > 1) is { } twice)
            throw new InvalidDataException($"Prop '{twice.Key}': on several maps.");
        // One map per place of the world.
        if (Towns.Values.Where(t => !t.IsHall).GroupBy(t => t.MapAt).FirstOrDefault(g => g.Count() > 1) is { } shared)
            throw new InvalidDataException($"Towns {string.Join(", ", shared.Select(t => $"'{t.Id}'"))}: the same place {shared.Key} of the world.");
        Families = Index(families ?? [], f => f.Id, "family");
        Monsters = Index(Families.Values.SelectMany(f => f.Monsters), m => m.Id, "monster");
        foreach (MonsterFamily f in Families.Values)
            Check(f);
        Jobs = Index(jobs ?? [], j => j.Id, "job");
        foreach (Job j in Jobs.Values)
        {
            // A gathering trade (sprint 99): known resources, a level each, no workshop.
            if (j.Gathers && (j.Workshop is not null || j.Harvests!.Any(h => !Items.TryGetValue(h.Item, out Item? r) || r.Kind != ItemKind.Resource || h.Level is < 1 or > Crafting.MaxLevel)))
                throw new InvalidDataException($"Job '{j.Id}': a gathering trade takes known resources, from a level 1 to {Crafting.MaxLevel}, without a workshop.");
            if ((j.Recipes.Count == 0 && !j.Gathers) || (!j.Gathers && j.Workshop is null) || j.Recipes.GroupBy(r => r.Item).Any(g => g.Count() > 1)
                || j.Recipes.Any(r => !Items.ContainsKey(r.Item) || r.Level is < 1 or > Crafting.MaxLevel || r.Ingredients.Count == 0
                    || r.Ingredients.Any(i => !Items.ContainsKey(i.Item) || i.Count < 1 || i.Item == r.Item)))
                throw new InvalidDataException($"Job '{j.Id}': recipes of known items, each once, a level 1 to {Crafting.MaxLevel}, known ingredients.");
            // A craftsman's piece (sprint 109, D76): never initiative, action or movement points, nor a set; those stay the loot's.
            if (j.Recipes.Select(r => Items[r.Item]).FirstOrDefault(i => i.Kind == ItemKind.Equipment && (i.Initiative != 0 || i.Ap != 0 || i.Mp != 0 || i.Set is not null)) is Item quick)
                throw new InvalidDataException($"Job '{j.Id}': '{quick.Id}' is made, so it gives no initiative, action or movement points, and has no set.");
        }
        foreach (Town t in Towns.Values)
        {
            foreach (Workshop w in t.Workshops ?? [])
            {
                if (!Jobs.ContainsKey(w.Job) || !t.CanStand(w.At) || t.ExitAt(w.At) is not null || t.LinkAt(w.At) is not null)
                    throw new InvalidDataException($"Town '{t.Id}': the workshop at {w.At} is of an unknown trade, or not on a free cell.");
            }
        }
        Dungeons = Index(dungeons ?? [], d => d.Id, "dungeon");
        foreach (Dungeon d in Dungeons.Values)
        {
            Check(d);
            for (int room = 1; room <= Core.Dungeons.RoomCount; room++)
            {
                Scenario fight = Core.Dungeons.Scenario(d, room, Families[d.Family]);
                Check(fight);
                _groupFights[fight.Id] = fight;
            }
        }
        // The doors go on their maps before the groups are placed: a group never stands on one.
        Towns = Towns.ToDictionary(t => t.Key, t => t.Value with { Doors = [.. Dungeons.Values.Where(d => d.Map == t.Key).Select(d => new DungeonDoor(d.Door, d.Id))] }, StringComparer.Ordinal);
        // The groups of the zones, then those guarding the dungeons' halls (sprint 86).
        IEnumerable<MonsterGroup> guards = Dungeons.Values.Where(d => d.Halls is not null)
            .SelectMany(d => Enumerable.Range(1, Core.Dungeons.RoomCount).Select(room => Core.Dungeons.Guard(d, room, Families[d.Family])));
        Groups = Index(Towns.Values.OrderBy(t => t.Id, StringComparer.Ordinal).SelectMany(t => MonsterGroups.For(t, [.. Families.Values], [.. Towns.Values])).Concat(guards), g => g.Id, "group");
        // The gathering spots of the zones (sprint 99), drawn like the groups.
        Nodes = Index(Towns.Values.OrderBy(t => t.Id, StringComparer.Ordinal).SelectMany(t => Harvesting.For(t, [.. Towns.Values], [.. Jobs.Values])), n => n.Id, "gathering spot");
        foreach (MonsterGroup g in Groups.Values.Where(g => g.Fight is null))
        {
            Scenario fight = MonsterGroups.Scenario(g, Families[g.Family], Monsters);
            Check(fight);
            _groupFights[fight.Id] = fight;
        }
        Quests = Index(quests ?? [], q => q.Id, "quest");
        foreach (Quest q in Quests.Values)
        {
            if (q.Xp < 1 || q.Shards < 0 || (q.Items ?? []).Concat(q.Grants ?? []).Any(st => !Items.ContainsKey(st.Item) || st.Count < 1))
                throw new InvalidDataException($"Quest '{q.Id}': no experience, Éclats below zero or an unknown item.");
            // A fight's quest, or a given one (sprint 88): never both.
            if (q.IsGiven == (q.Scenario is not null) || (q.Scenario is string sc && !Scenarios.ContainsKey(sc)))
                throw new InvalidDataException($"Quest '{q.Id}': a scenario or a giver, one of them, known.");
            if (!q.IsGiven)
                continue;
            if (FindNpc(q.Giver!) is null || (q.HandIn is not null && FindNpc(q.HandIn) is null) || q.Text is null || q.Thanks is null || q.Objectives is not { Count: > 0 } goals || q.Level is < 1 or > Hero.MaxLevel
                || (q.Zone is not null && !Towns.Values.Any(t => t.Zone == q.Zone)) || (q.After ?? []).Any(a => a == q.Id || !(quests ?? []).Any(o => o.Id == a)))
                throw new InvalidDataException($"Quest '{q.Id}': a known giver, its words, objectives, a level, a known zone and known quests before it.");
            foreach (QuestObjective o in goals)
            {
                bool known = o.Goal switch
                {
                    QuestGoal.Talk => FindNpc(o.Target) is not null,
                    QuestGoal.Defeat => Families.ContainsKey(o.Target) || Monsters.ContainsKey(o.Target),
                    QuestGoal.Bring => Items.ContainsKey(o.Target),
                    QuestGoal.Dungeon => Dungeons.ContainsKey(o.Target),
                    QuestGoal.Interact => FindProp(o.Target) is not null,
                    QuestGoal.Win => FindScenario(o.Target) is not null,
                    QuestGoal.Wear => Items.TryGetValue(o.Target, out Item? worn) && worn.Kind == ItemKind.Equipment,
                    _ => false,
                };
                if (!known || o.Count < 1 || (o.Goal is not (QuestGoal.Defeat or QuestGoal.Bring) && o.Count != 1))
                    throw new InvalidDataException($"Quest '{q.Id}': the objective {o.Goal} '{o.Target}' ×{o.Count} is unknown.");
                if ((o.Line is not null && o.Goal != QuestGoal.Talk) || o.Room is < 0 or > Rpg.Core.Dungeons.RoomCount || (o.Room != 0 && o.Goal != QuestGoal.Dungeon))
                    throw new InvalidDataException($"Quest '{q.Id}': a room of 1 to {Rpg.Core.Dungeons.RoomCount}, for a dungeon only.");
            }
            // A dungeon's quest asks only to finish it (sprint 116).
            if ((q.Kind == QuestKind.Dungeon) != goals is [{ Goal: QuestGoal.Dungeon, Room: 0 }])
                throw new InvalidDataException($"Quest '{q.Id}': a dungeon's quest, and only it, asks just to finish a dungeon.");
        }
        // A quest's loot (sprint 105): a quest item; when its quest is in the data, one that quest asks to bring (data
        // loaded without it, as some tests do, never lets it fall). Left by a monster, or by a fighter of a quest's own
        // fight (sprint 107: the loft's rats).
        foreach ((string who, LootDrop l) in Monsters.Values.SelectMany(m => (m.Loot ?? []).Select(l => (m.Id, l)))
            .Concat(Scenarios.Values.SelectMany(s => s.Fighters.SelectMany(f => (f.Loot ?? []).Select(l => (s.Id, l)))))
            .Where(x => x.l.Quest is not null))
        {
            if (Items[l.Item].Kind != ItemKind.Quest
                || (Quests.TryGetValue(l.Quest!, out Quest? wanting) && !(wanting.Objectives ?? []).Any(o => o.Goal == QuestGoal.Bring && o.Target == l.Item)))
                throw new InvalidDataException($"'{who}': the loot '{l.Item}' of the quest '{l.Quest}' is not a quest item it asks to bring.");
        }
        // A way shut until a quest is done (sprint 129): a known quest, when the quests are loaded (some tests load the maps alone).
        foreach ((Town t, ZoneLink l) in Towns.Values.SelectMany(t => (t.Links ?? []).Where(l => l.Quest is not null).Select(l => (t, l))))
        {
            if (Quests.Count > 0 && !Quests.ContainsKey(l.Quest!))
                throw new InvalidDataException($"Town '{t.Id}': the way to {l.To} waits for an unknown quest '{l.Quest}'.");
        }
        // A thing to examine that starts a fight (sprint 107): a known one.
        foreach (Prop p in Towns.Values.SelectMany(t => t.Props ?? []).Where(p => p.Fight is not null))
        {
            if (FindScenario(p.Fight!) is null)
                throw new InvalidDataException($"Prop '{p.Id}': an unknown fight '{p.Fight}'.");
        }
    }

    private Dictionary<string, string>? _gates;

    /// <summary>
    /// The quest a character must have done to be on a map (sprint 129), or null: walking from the village's square by
    /// the ways, the first shut way crossed to get there (Clairval's east gate before Quatre-Chemins). The server refuses
    /// a place beyond it.
    /// </summary>
    public string? QuestToReach(string map)
    {
        if (_gates is null)
        {
            var gates = new Dictionary<string, string>(StringComparer.Ordinal);
            var open = new HashSet<string>(StringComparer.Ordinal);
            // The maps reached by open ways first; then, a round for each shut way, those reached past it.
            var todo = new Queue<(string Map, string? Quest)>();
            if (Towns.ContainsKey(Shop.Village))
                todo.Enqueue((Shop.Village, null));
            var later = new List<(string Map, string Quest)>();
            while (todo.Count > 0 || later.Count > 0)
            {
                if (todo.Count == 0)
                {
                    foreach ((string m, string q) in later.Where(x => !open.Contains(x.Map)))
                        todo.Enqueue((m, q));
                    later.Clear();
                    continue;
                }
                (string here, string? quest) = todo.Dequeue();
                if (!open.Add(here))
                    continue;
                if (quest is not null)
                    gates[here] = quest;
                foreach (ZoneLink l in Towns[here].Links ?? [])
                {
                    if (l.Quest is null)
                        todo.Enqueue((l.To, quest));
                    else
                        later.Add((l.To, quest ?? l.Quest));
                }
            }
            _gates = gates;
        }
        return _gates.GetValueOrDefault(map ?? throw new ArgumentNullException(nameof(map)));
    }

    /// <summary>A thing to examine by its id (sprint 105), and its map.</summary>
    public (Town Town, Prop Prop)? FindProp(string id)
    {
        foreach (Town town in Towns.Values)
        {
            if (town.Props?.FirstOrDefault(p => p.Id == id) is Prop prop)
                return (town, prop);
        }
        return null;
    }

    /// <summary>An inhabitant by "town/npc" (sprint 88), and its town.</summary>
    public (Town Town, Npc Npc)? FindNpc(string reference)
    {
        int slash = (reference ?? throw new ArgumentNullException(nameof(reference))).IndexOf('/', StringComparison.Ordinal);
        if (slash <= 0)
            return null;
        string where = reference[..slash], who = reference[(slash + 1)..];
        // The map named, else any map of the zone named (sprint 102: "clairval/rose" finds Rose on the village's market).
        IEnumerable<Town> where2 = Towns.TryGetValue(where, out Town? named) ? [named] : [];
        foreach (Town town in where2.Concat(Towns.Values.Where(t => t.Zone == where && t.Id != where).OrderBy(t => t.Id, StringComparer.Ordinal)))
        {
            if (town.Npcs.FirstOrDefault(n => n.Id == who) is Npc npc)
                return (town, npc);
        }
        return null;
    }

    /// <summary>How the quests name an inhabitant: by its zone if that finds it (the village's, sprint 102), else by its map.</summary>
    public string RefOf(Town town, Npc npc)
    {
        ArgumentNullException.ThrowIfNull(town);
        ArgumentNullException.ThrowIfNull(npc);
        return town.Zone is string zone && FindNpc($"{zone}/{npc.Id}")?.Npc == npc ? $"{zone}/{npc.Id}" : $"{town.Id}/{npc.Id}";
    }

    /// <summary>
    /// The talents and reactions hold together (sprint 137): a reaction joins two different base elements, each pair once,
    /// and sets statuses on the enemy; a talent has rules, knows the reactions, talents it excludes and requires.
    /// </summary>
    private void CheckTalents()
    {
        foreach (ReactionSpec r in Reactions.Values)
        {
            if (r.First == r.Second || !Rpg.Core.Talents.IsBase(r.First) || !Rpg.Core.Talents.IsBase(r.Second) || r.Statuses.Count == 0
                || r.Statuses.Any(st => st.Affects != Affects.Enemies || st.Turns < 1)
                || Reactions.Values.Any(o => o != r && o.Joins(r.First, r.Second)))
                throw new InvalidDataException($"Reaction '{r.Id}': two different base elements, a pair no other joins, statuses on the enemy.");
        }
        foreach (TalentSpec t in Talents.Values)
        {
            if (t.Rules.Count == 0 || t.Rules.OfType<ReactionRule>().Any(r => !Reactions.ContainsKey(r.Reaction))
                || (t.Excludes ?? []).Concat(t.Requires ?? []).Concat(t.RequiresAny ?? []).Any(id => id == t.Id || !Talents.ContainsKey(id)))
                throw new InvalidDataException($"Talent '{t.Id}': rules, known reactions, other known talents it excludes or requires.");
            // Its texts in both languages (sprint 147): a key missing stops the game from loading, so the build.
            if (string.IsNullOrWhiteSpace(t.Name?.Fr) || string.IsNullOrWhiteSpace(t.Name.En) || string.IsNullOrWhiteSpace(t.Description?.Fr) || string.IsNullOrWhiteSpace(t.Description.En))
                throw new InvalidDataException($"Talent '{t.Id}': a name and a description in French and in English.");
            if (t.Rules.OfType<DistanceRule>().Any(r => r.MinCells < 0 || r.MaxCells < r.MinCells))
                throw new InvalidDataException($"Talent '{t.Id}': a distance from 0 up.");
            if (t.Rules.OfType<GrantRule>().Any(r => !Spells.ContainsKey(r.Spell)))
                throw new InvalidDataException($"Talent '{t.Id}': a spell given that is known.");
            if (t.Rules.OfType<ConvertRule>().Any(r => !Elements.IsCompound(r.Element))
                || t.Rules.OfType<TransmuteRule>().Any(r => !Elements.Parents(r.To).Contains(r.From)))
                throw new InvalidDataException($"Talent '{t.Id}': a compound converted, a parent transmuted into its compound.");
        }
    }

    public HeroClass? Class(string? id) => Classes.FirstOrDefault(c => c.Id == id);

    /// <summary>Reads <c>spells.json</c>, <c>classes.json</c> (if there is one), <c>maps/*.json</c> and <c>scenarios/*.json</c> from a folder.</summary>
    public static GameData Load(string folder)
    {
        T Read<T>(string path) =>
            JsonSerializer.Deserialize<T>(File.ReadAllText(path), Json)
            ?? throw new InvalidDataException($"{path} is empty.");
        IEnumerable<T> ReadAll<T>(string sub) =>
            Directory.GetFiles(Path.Combine(folder, sub), "*.json").Order(StringComparer.Ordinal).Select(Read<T>);
        return new GameData(
            Read<List<Spell>>(Path.Combine(folder, "spells.json")),
            ReadAll<MapSpec>("maps"),
            ReadAll<Scenario>("scenarios"),
            File.Exists(Path.Combine(folder, "classes.json")) ? Read<List<HeroClass>>(Path.Combine(folder, "classes.json")) : null,
            Directory.Exists(Path.Combine(folder, "towns")) ? ReadAll<Town>("towns") : null,
            Directory.Exists(Path.Combine(folder, "dialogues")) ? ReadAll<Dialogue>("dialogues") : null,
            File.Exists(Path.Combine(folder, "summons.json")) ? Read<List<SummonSpec>>(Path.Combine(folder, "summons.json")) : null,
            File.Exists(Path.Combine(folder, "quests.json")) ? Read<List<Quest>>(Path.Combine(folder, "quests.json")) : null,
            File.Exists(Path.Combine(folder, "items.json")) ? Read<List<Item>>(Path.Combine(folder, "items.json")) : null,
            File.Exists(Path.Combine(folder, "sets.json")) ? Read<List<ItemSet>>(Path.Combine(folder, "sets.json")) : null,
            File.Exists(Path.Combine(folder, "monsters.json")) ? Read<List<MonsterFamily>>(Path.Combine(folder, "monsters.json")) : null,
            File.Exists(Path.Combine(folder, "dungeons.json")) ? Read<List<Dungeon>>(Path.Combine(folder, "dungeons.json")) : null,
            File.Exists(Path.Combine(folder, "jobs.json")) ? Read<List<Job>>(Path.Combine(folder, "jobs.json")) : null,
            File.Exists(Path.Combine(folder, "passives.json")) ? Read<List<PassiveSpec>>(Path.Combine(folder, "passives.json")) : null,
            File.Exists(Path.Combine(folder, "talents.json")) ? Read<List<TalentSpec>>(Path.Combine(folder, "talents.json")) : null,
            File.Exists(Path.Combine(folder, "reactions.json")) ? Read<List<ReactionSpec>>(Path.Combine(folder, "reactions.json")) : null);
    }

    /// <summary>
    /// The data files built into this library (<c>data/</c> of the project, see Rpg.Core.csproj): what
    /// the game, the server and the tests read, whatever folder they run from.
    /// </summary>
    public static GameData Embedded { get; } = LoadEmbedded();

    private static GameData LoadEmbedded()
    {
        var assembly = typeof(GameData).Assembly;
        var files = assembly.GetManifestResourceNames()
            .Where(n => n.StartsWith("data/", StringComparison.Ordinal))
            .Order(StringComparer.Ordinal)
            .ToDictionary(n => n, n =>
            {
                using var reader = new StreamReader(assembly.GetManifestResourceStream(n)!);
                return reader.ReadToEnd();
            });
        T Parse<T>(string name, string json) =>
            JsonSerializer.Deserialize<T>(json, Json) ?? throw new InvalidDataException($"{name} is empty.");
        IEnumerable<T> All<T>(string prefix) =>
            files.Where(f => f.Key.StartsWith(prefix, StringComparison.Ordinal)).Select(f => Parse<T>(f.Key, f.Value));
        return new GameData(Parse<List<Spell>>("spells.json", files["data/spells.json"]), All<MapSpec>("data/maps/"), All<Scenario>("data/scenarios/"),
            Parse<List<HeroClass>>("classes.json", files["data/classes.json"]), All<Town>("data/towns/"), All<Dialogue>("data/dialogues/"),
            files.TryGetValue("data/summons.json", out string? summons) ? Parse<List<SummonSpec>>("summons.json", summons) : null,
            files.TryGetValue("data/quests.json", out string? quests) ? Parse<List<Quest>>("quests.json", quests) : null,
            files.TryGetValue("data/items.json", out string? items) ? Parse<List<Item>>("items.json", items) : null,
            files.TryGetValue("data/sets.json", out string? sets) ? Parse<List<ItemSet>>("sets.json", sets) : null,
            files.TryGetValue("data/monsters.json", out string? monsters) ? Parse<List<MonsterFamily>>("monsters.json", monsters) : null,
            files.TryGetValue("data/dungeons.json", out string? dungeons) ? Parse<List<Dungeon>>("dungeons.json", dungeons) : null,
            files.TryGetValue("data/jobs.json", out string? jobs) ? Parse<List<Job>>("jobs.json", jobs) : null,
            files.TryGetValue("data/passives.json", out string? passives) ? Parse<List<PassiveSpec>>("passives.json", passives) : null,
            files.TryGetValue("data/talents.json", out string? talents) ? Parse<List<TalentSpec>>("talents.json", talents) : null,
            files.TryGetValue("data/reactions.json", out string? reactions) ? Parse<List<ReactionSpec>>("reactions.json", reactions) : null);
    }

    public Board Board(string mapId) =>
        Maps.TryGetValue(mapId, out MapSpec? map)
            ? Core.Board.Parse(map.Rows)
            : throw new KeyNotFoundException($"Unknown map '{mapId}'.");

    private static Dictionary<string, T> Index<T>(IEnumerable<T> items, Func<T, string> id, string kind)
    {
        var index = new Dictionary<string, T>(StringComparer.Ordinal);
        foreach (T item in items)
        {
            if (!index.TryAdd(id(item), item))
                throw new InvalidDataException($"Two {kind}s are called '{id(item)}'.");
        }
        return index;
    }

    private void Check(Spell s)
    {
        if (s.ApCost < 1 || s.MinRange < 0 || s.MaxRange < s.MinRange || s.DamageMin < 0
            || s.DamageMax < s.DamageMin || s.PerTurn < 1 || s.Cooldown < 0 || s.Crit is < 0 or > 100)
            throw new InvalidDataException($"Spell '{s.Id}': cost, ranges, damage, casts per turn or critical chance out of bounds.");
        if (s.Level is < 1 or > 100 || (s.Ranks?.Count ?? 0) > 4
            || (s.Ranks ?? []).Any(r => r.DamageMin < 0 || r.DamageMax < r.DamageMin || r.ApCost < 1 || r.MaxRange < s.MinRange || r.PerTurn < 1))
            throw new InvalidDataException($"Spell '{s.Id}': level 1 to 100, at most 5 ranks, each consistent.");
        if (!ValidArea(s.Zone))
            throw new InvalidDataException($"Spell '{s.Id}': an area's radius is 1 to 5 (0 for a single cell).");
        foreach (SpellEffect e in s.AllEffects)
        {
            // A leap lands the caster, a swap trades places with one fighter: on a single cell (sprint 90).
            bool ok = e switch
            {
                LeapEffect l => l.Affects == Affects.Caster && s.Zone.Shape == AreaShape.Point,
                SwapEffect w => w.Affects is not (Affects.Caster or Affects.Attacker) && s.Zone.Shape == AreaShape.Point,
                SummonEffect sm => sm.Count == 1 && ValidEffect(e),
                GlyphEffect g => ValidGlyph(g),
                _ => e.Affects != Affects.Attacker && ValidEffect(e),
            };
            if (!ok)
                throw new InvalidDataException($"Spell '{s.Id}': effect {e} out of bounds.");
        }
    }

    /// <summary>
    /// A glyph (sprint 130): on enemies, allies or all, for 1 to 10 turns, with damage, a status or both, each in
    /// the bounds of a spell's; a spell's only, never a passive's.
    /// </summary>
    private bool ValidGlyph(GlyphEffect g) =>
        g.Affects is Affects.Enemies or Affects.Allies or Affects.All && g.Turns is >= 1 and <= 10
        && g.DamageMin >= 0 && g.DamageMax >= g.DamageMin && (g.DamageMax > 0 || g.Status is not null || g.Marks)
        && (g.AsStatus is not StatusEffect st ? g.Value == 0 && g.StatusTurns == 0 : ValidEffect(st));

    private static bool ValidArea(Area a) => a.Radius is >= 0 and <= 5 && (a.Shape == AreaShape.Point) == (a.Radius == 0);

    /// <summary>The bounds of an effect, for a spell or a passive.</summary>
    private bool ValidEffect(SpellEffect e) => e switch
    {
        HealEffect h => h.Min >= 0 && h.Max >= h.Min && h.Max > 0,
        ShieldEffect sh => sh.Amount > 0 && sh.Turns is >= 1 and <= 10,
        PushEffect p => p.Cells is >= 1 and <= 5 && p.Affects != Affects.Caster,
        PullEffect p => p.Cells is >= 1 and <= 5 && p.Affects != Affects.Caster,
        StatusEffect st => st.Value != 0 && st.Turns is >= 1 and <= 10 && (st.Stat != Stat.Poison || st.Value > 0),
        SummonEffect sm => sm.Max is >= 1 and <= 4 && sm.Count is >= 1 and <= 4 && Summons.ContainsKey(sm.Summon),
        _ => false,
    };

    /// <summary>
    /// A passive (sprint 90): a percentage for a low-health one only, "melee" and the attacker for a hit
    /// only, a range for an ally's death only, effects in the bounds of a spell's (no leap nor swap),
    /// damage in bounds, an area of 1 to 5.
    /// </summary>
    private void Check(PassiveSpec p)
    {
        bool ok = (p.Trigger == Trigger.LowHp ? p.Percent is >= 1 and <= 99 : p.Percent == 0)
            && (p.Trigger == Trigger.Hit || (!p.Melee && p.AllEffects.All(e => e.Affects != Affects.Attacker)))
            && (p.Trigger == Trigger.AllyDies ? p.Range >= 0 : p.Range == 0)
            && p.DamageMin >= 0 && p.DamageMax >= p.DamageMin && (p.Area is null || (p.Area.Shape != AreaShape.Point && ValidArea(p.Area)))
            && p.AllEffects.All(ValidEffect);
        if (!ok)
            throw new InvalidDataException($"Passive '{p.Id}': its numbers do not fit its trigger {p.Trigger}.");
    }

    /// <summary>Known passives, three at most (sprint 90).</summary>
    private void CheckPassives(string who, IReadOnlyList<string>? passives)
    {
        if (passives is null)
            return;
        if (passives.Count > 3 || passives.Distinct().Count() != passives.Count || passives.FirstOrDefault(p => !Passives.ContainsKey(p)) is string)
            throw new InvalidDataException($"{who}: at most 3 known passives, each once.");
    }

    private void Check(HeroClass c)
    {
        string who = $"Class '{c.Id}'";
        if (c.Hp < 1 || c.Ap < 0 || c.Mp < 0 || c.HpPerLevel < 0)
            throw new InvalidDataException($"{who}: hit points, action or movement points out of bounds.");
        if (c.Spells.Count is < 1 or > 30)
            throw new InvalidDataException($"{who}: 1 to 30 spells (the keys 1 to 9 choose the first nine of the bar).");
        foreach (string spell in c.Spells)
        {
            if (!Spells.ContainsKey(spell))
                throw new InvalidDataException($"{who}: unknown spell '{spell}'.");
        }
    }

    private void Check(Dialogue d)
    {
        string who = $"Dialogue '{d.Id}'";
        if (!d.Lines.ContainsKey(d.Start))
            throw new InvalidDataException($"{who}: no line '{d.Start}' to start with.");
        foreach ((string id, DialogueLine line) in d.Lines)
        {
            if (line.Answers.Count is < 1 or > 4)
                throw new InvalidDataException($"{who}, line '{id}': 1 to 4 answers.");
            foreach (DialogueAnswer a in line.Answers)
            {
                if (a.Next is not null && !d.Lines.ContainsKey(a.Next))
                    throw new InvalidDataException($"{who}, line '{id}': no line '{a.Next}'.");
                if (a.Fight is not null && (a.Next is not null || !Scenarios.ContainsKey(a.Fight)))
                    throw new InvalidDataException($"{who}, line '{id}': a fight ends the talk, in a known scenario.");
            }
        }
        // Every line can be reached from the start, and from every line the talk can end.
        var reached = Closure([d.Start], id => d.Lines[id].Answers.Select(a => a.Next).OfType<string>());
        if (d.Lines.Keys.FirstOrDefault(id => !reached.Contains(id)) is string lost)
            throw new InvalidDataException($"{who}: line '{lost}' cannot be reached.");
        var ending = Closure(d.Lines.Where(l => l.Value.Answers.Any(a => a.Next is null)).Select(l => l.Key),
            id => d.Lines.Where(l => l.Value.Answers.Any(a => a.Next == id)).Select(l => l.Key));
        if (d.Lines.Keys.FirstOrDefault(id => !ending.Contains(id)) is string endless)
            throw new InvalidDataException($"{who}: from line '{endless}' the talk never ends.");
    }

    private static HashSet<string> Closure(IEnumerable<string> start, Func<string, IEnumerable<string>> next)
    {
        var seen = new HashSet<string>(start, StringComparer.Ordinal);
        var queue = new Queue<string>(seen);
        while (queue.TryDequeue(out string? id))
        {
            foreach (string n in next(id))
            {
                if (seen.Add(n))
                    queue.Enqueue(n);
            }
        }
        return seen;
    }

    private void Check(Town t)
    {
        string who = $"Town '{t.Id}'";
        Board board;
        try
        {
            if (t.Rows.SelectMany(r => r).FirstOrDefault(c => !Town.Decor.ContainsKey(c)) is char unknown and not '\0')
                throw new InvalidDataException($"{who}: unknown cell '{unknown}'.");
            board = t.Board();
        }
        catch (FormatException e)
        {
            throw new InvalidDataException($"{who}: {e.Message}");
        }
        // Houses, fountains, hen houses and stables fill 2 × 2 cells: an upper-case corner, three lower-case cells.
        foreach (Cell c in board.Cells())
        {
            char k = t[c];
            if (k is 'H' or 'F' or 'Q' or 'E')
            {
                char rest = char.ToLowerInvariant(k);
                if (t[c + new Cell(1, 0)] != rest || t[c + new Cell(0, 1)] != rest || t[c + new Cell(1, 1)] != rest)
                    throw new InvalidDataException($"{who}: the {Town.Decor[k]} at {c} does not fill 2 × 2 cells.");
            }
            else if (k == 'K')
            {
                // A keep fills 3 × 3 cells (sprint 86).
                for (int dy = 0; dy < 3; dy++)
                {
                    for (int dx = 0; dx < 3; dx++)
                    {
                        if ((dx, dy) != (0, 0) && t[c + new Cell(dx, dy)] != 'k')
                            throw new InvalidDataException($"{who}: the keep at {c} does not fill 3 × 3 cells.");
                    }
                }
            }
            else if (k is 'h' or 'f' or 'q' or 'e')
            {
                char corner = char.ToUpperInvariant(k);
                if (!new[] { new Cell(-1, 0), new Cell(0, -1), new Cell(-1, -1) }.Any(d => t[c + d] == corner))
                    throw new InvalidDataException($"{who}: the {Town.Decor[k]} cell {c} has no corner.");
            }
        }
        if (!t.IsFree(board, t.Spawn))
            throw new InvalidDataException($"{who}: the arrival {t.Spawn} is not a free floor cell.");
        HashSet<Cell> reach = [.. Pathfinding.Reachable(board, t.Spawn, board.Width * board.Height, c => t.IsFree(board, c)).Keys];
        var cells = new HashSet<Cell>();
        foreach (Npc n in t.Npcs)
        {
            string npc = $"{who}, {n.Id}";
            if (!board.IsFloor(n.At) || !cells.Add(n.At))
                throw new InvalidDataException($"{npc}: not on a floor cell of its own.");
            if (!Dialogues.ContainsKey(n.Dialogue))
                throw new InvalidDataException($"{npc}: unknown dialogue '{n.Dialogue}'.");
            if (!Hero.Looks.Contains(n.Look) || n.Colour is < 0 or >= Hero.Colours)
                throw new InvalidDataException($"{npc}: unknown look or colour.");
            if (!n.At.Neighbours().Any(reach.Contains))
                throw new InvalidDataException($"{npc}: nobody can walk up to them from the arrival.");
        }
        // The things to examine (sprint 105): on floor of their own, named, walked up to from the arrival.
        foreach (Prop p in t.Props ?? [])
        {
            if (!board.IsFloor(p.At) || t.NpcAt(p.At) is not null || t.LinkAt(p.At) is not null || t.ExitAt(p.At) is not null || !cells.Add(p.At)
                || string.IsNullOrWhiteSpace(p.Id) || string.IsNullOrWhiteSpace(p.Look) || p.Name is null || p.Text is null || !p.At.Neighbours().Any(reach.Contains))
                throw new InvalidDataException($"{who}: the prop '{p.Id}' at {p.At} is not on a floor cell of its own one can walk up to.");
        }
        // The beasts (sprint 104): a kind the game draws, on grass or flowers of their own, off the roads.
        foreach (Animal a in t.Animals ?? [])
        {
            if (!Animal.Kinds.Contains(a.Kind) || !board.IsFloor(a.At) || t[a.At] == '=' || t.NpcAt(a.At) is not null || t.LinkAt(a.At) is not null)
                throw new InvalidDataException($"{who}: the {a.Kind} at {a.At} is of an unknown kind, or not on grass of its own.");
        }
        foreach (TownExit e in t.Exits)
        {
            if (!reach.Contains(e.At) || !cells.Add(e.At))
                throw new InvalidDataException($"{who}: the exit at {e.At} cannot be reached.");
            if (!Scenarios.ContainsKey(e.Scenario))
                throw new InvalidDataException($"{who}: the exit at {e.At} leads to an unknown scenario '{e.Scenario}'.");
        }
        if (t.Level is < 1 or > Hero.MaxLevel)
            throw new InvalidDataException($"{who}: a level of 1 to {Hero.MaxLevel}.");
        foreach (ZoneLink l in t.Links ?? [])
        {
            if (!reach.Contains(l.At) || !cells.Add(l.At))
                throw new InvalidDataException($"{who}: the way to {l.To} at {l.At} cannot be reached.");
            if (!Towns.TryGetValue(l.To, out Town? to) || !to.CanStand(l.Arrival))
                throw new InvalidDataException($"{who}: the way at {l.At} leads to an unknown zone or a cell nobody can stand on.");
            // A dungeon's hall leads to the next hall or out, wherever its doors are (sprint 86).
            if (t.IsHall || to.IsHall)
                continue;
            // A way leads to the map next door, from the edge that faces it, onto the edge that faces back.
            Cell d = to.MapAt - t.MapAt;
            bool facing = (d.X, d.Y) switch
            {
                (1, 0) => l.At.X == t.Width - 1 && l.Arrival.X <= 1,
                (-1, 0) => l.At.X == 0 && l.Arrival.X >= to.Width - 2,
                (0, 1) => l.At.Y == t.Rows.Count - 1 && l.Arrival.Y <= 1,
                (0, -1) => l.At.Y == 0 && l.Arrival.Y >= to.Rows.Count - 2,
                _ => false,
            };
            if (!facing)
                throw new InvalidDataException($"{who}: the way at {l.At} to {l.To} is not on the edge facing that map, or does not arrive on the edge facing back.");
        }
    }

    /// <summary>
    /// A dungeon (sprint 80): a known family; its door on a map of the family's zone where one can stand,
    /// away from the ways, exits and arrival; a key among the resources and the operator's keyring among
    /// the items; three rooms on battle maps with room for a hero and the chief's room.
    /// </summary>
    private void Check(Dungeon d)
    {
        string who = $"Dungeon '{d.Id}'";
        if (!Families.TryGetValue(d.Family, out MonsterFamily? family))
            throw new InvalidDataException($"{who}: unknown family '{d.Family}'.");
        if (!Towns.TryGetValue(d.Map, out Town? map) || map.Zone != family.Zone || !map.CanStand(d.Door)
            || map.Links?.Any(l => l.At.DistanceTo(d.Door) < MonsterGroups.Distance) == true || map.Exits.Any(e => e.At.DistanceTo(d.Door) < MonsterGroups.Distance)
            || map.Spawn.DistanceTo(d.Door) < MonsterGroups.Distance)
            throw new InvalidDataException($"{who}: its door {d.Door} is not on a map of the {family.Zone} where one can stand, away from its ways, exits and arrival.");
        if (!Items.TryGetValue(d.Key, out Item? key) || key.Kind != ItemKind.Resource || !Items.ContainsKey(Core.Dungeons.Keyring))
            throw new InvalidDataException($"{who}: its key '{d.Key}' is not a resource, or there is no '{Core.Dungeons.Keyring}'.");
        if (d.Rooms.Count != Core.Dungeons.RoomCount || d.Rooms.Any(r => !Maps.ContainsKey(r) || Board(r).Starts[0].Count < 1 || Board(r).Starts[1].Count < Core.Dungeons.RolesIn(Core.Dungeons.RoomCount).Count))
            throw new InvalidDataException($"{who}: {Core.Dungeons.RoomCount} rooms on known battle maps, each with room for a hero and {Core.Dungeons.RolesIn(Core.Dungeons.RoomCount).Count} monsters.");
        // Its halls (sprint 86): one map per room, of this dungeon, its group where one can stand.
        if (d.Halls is { } halls && (halls.Count != Core.Dungeons.RoomCount
            || halls.Any(h => !Towns.TryGetValue(h.Map, out Town? hall) || hall.Dungeon != d.Id || !hall.CanStand(h.Group) || hall.Spawn == h.Group)))
            throw new InvalidDataException($"{who}: {Core.Dungeons.RoomCount} halls, maps of this dungeon, each with its group on a cell one can stand on.");
    }

    /// <summary>
    /// A family: one monster of each role (D63), each with known spells and loot, a level and points
    /// in bounds; a zone of the world, a battle map with room for the largest group, a boss map in the zone;
    /// above level 30, the initiative of its role and level (sprint 128).
    /// </summary>
    private void Check(MonsterFamily f)
    {
        string who = $"Family '{f.Id}'";
        MonsterRole[] roles = [.. f.Monsters.Select(m => m.Role).Order()];
        if (!roles.SequenceEqual(Enum.GetValues<MonsterRole>()))
            throw new InvalidDataException($"{who}: one monster of each role ({string.Join(", ", Enum.GetValues<MonsterRole>())}), not {string.Join(", ", roles)}.");
        foreach (MonsterSpec m in f.Monsters)
        {
            if (m.Level is < 1 or > Hero.MaxLevel || m.Hp < 1 || m.Ap < 1 || m.Mp < 0 || m.Spells.Count == 0 || m.Spells.Any(id => !Spells.ContainsKey(id))
                || string.IsNullOrWhiteSpace(m.Look) || (m.Loot ?? []).Any(l => !Items.ContainsKey(l.Item) || l.Percent is < 1 or > 100))
                throw new InvalidDataException($"{who}, '{m.Id}': a level, points, known spells, a look and known loot.");
            if (!Templates.InitiativeFits(m))
                throw new InvalidDataException($"{who}, '{m.Id}': initiative {m.Initiative}, not about {Templates.Initiative(m.Role, m.Level)} for a {m.Role} of level {m.Level}.");
            CheckPassives($"{who}, '{m.Id}'", m.Passives);
        }
        Town[] zone = [.. Towns.Values.Where(t => t.Zone == f.Zone)];
        if (zone.Length == 0 || !zone.Any(t => t.Id == f.BossMap))
            throw new InvalidDataException($"{who}: unknown zone '{f.Zone}', or a boss map '{f.BossMap}' outside it.");
        if (f.Battles is { } battles && (battles.Count == 0 || battles[0] != f.Battle || battles.Distinct().Count() != battles.Count))
            throw new InvalidDataException($"{who}: its battle maps start with '{f.Battle}', each once.");
        foreach (string battle in f.AllBattles)
        {
            if (!Maps.ContainsKey(battle) || Board(battle).Starts[1].Count < MonsterGroups.Largest || Board(battle).Starts[0].Count < 1)
                throw new InvalidDataException($"{who}: the battle map '{battle}' is unknown, or has no room for a hero and {MonsterGroups.Largest} monsters.");
        }
    }

    private void Check(Scenario s)
    {
        Board board = Board(s.Map);
        var used = new HashSet<(int, int)>();
        foreach (FighterSpec f in s.Fighters)
        {
            string who = $"Scenario '{s.Id}', fighter '{f.Name.En}'";
            if (f.Team is not (0 or 1))
                throw new InvalidDataException($"{who}: team must be 0 or 1.");
            if (string.IsNullOrWhiteSpace(f.Look))
                throw new InvalidDataException($"{who}: no look.");
            if (f.Hp < 1 || f.Ap < 0 || f.Mp < 0 || f.Level is < 1 or > Hero.MaxLevel)
                throw new InvalidDataException($"{who}: hit points, action or movement points or level out of bounds.");
            if ((f.Loot ?? []).Any(l => !Items.ContainsKey(l.Item) || l.Percent is < 1 or > 100 || l.Count < 1))
                throw new InvalidDataException($"{who}: loot of unknown items, or chances out of 1 to 100 %.");
            if (f.Resistances is not null && f.Resistances.Any(r => r.Key == Element.Neutral || r.Value is < -100 or > 90))
                throw new InvalidDataException($"{who}: resistances are per element, from -100 to 90 percent.");
            if (f.Start < 0 || f.Start >= board.Starts[f.Team].Count || !used.Add((f.Team, f.Start)))
                throw new InvalidDataException($"{who}: no free starting cell {f.Start} for team {f.Team}.");
            foreach (string spell in f.Spells)
            {
                if (!Spells.ContainsKey(spell))
                    throw new InvalidDataException($"{who}: unknown spell '{spell}'.");
            }
            CheckPassives(who, f.Passives);
        }
        for (int team = 0; team < 2; team++)
        {
            if (!s.Fighters.Any(f => f.Team == team))
                throw new InvalidDataException($"Scenario '{s.Id}': team {team} has nobody.");
        }
    }
}
