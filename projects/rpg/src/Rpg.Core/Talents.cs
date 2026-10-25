using System.Text.Json.Serialization;

namespace Rpg.Core;

/// <summary>The two families of the talent tree (D80): Matter changes what the hero strikes with, Manner the rules it strikes by.</summary>
[JsonConverter(typeof(JsonStringEnumConverter<TalentFamily>))]
public enum TalentFamily
{
    Matter,
    Manner,
}

/// <summary>
/// Where a talent stands in the tree (since sprint 147, D82, a graph): the fork (the six pairs, each giving its reaction),
/// Pure element (its Hearts, the fork's seventh door), the Lightning braid, and the six styles shared by every pair
/// (Alchemy, Melee, Distance, Terrain, Tempo, Support). Matter is the braid and Pure element; the rest is Manner.
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter<Branch>))]
public enum Branch
{
    Fork,
    Pure,
    Braids,
    Alchemy,
    Melee,
    Distance,
    Terrain,
    Tempo,
    Support,
}

/// <summary>A talent's floor in its branch: the first is open, the second asks 2 points spent below it, the key 3 and costs 2.</summary>
[JsonConverter(typeof(JsonStringEnumConverter<Tier>))]
public enum Tier
{
    First = 1,
    Second = 2,
    Key = 3,
}

/// <summary>What a talent changes (sprint 137): each kind is a piece of the engine, read before the fight (compiled) or during it (living).</summary>
[JsonPolymorphic(TypeDiscriminatorPropertyName = "kind")]
[JsonDerivedType(typeof(ReactionRule), "reaction")]
[JsonDerivedType(typeof(RangeRule), "range")]
[JsonDerivedType(typeof(DistanceRule), "distance")]
[JsonDerivedType(typeof(TransmuteRule), "transmute")]
[JsonDerivedType(typeof(ConvertRule), "convert")]
[JsonDerivedType(typeof(SignatureRule), "signature")]
[JsonDerivedType(typeof(ElementRule), "element")]
[JsonDerivedType(typeof(CompoundRule), "compound")]
[JsonDerivedType(typeof(PrismRule), "prism")]
[JsonDerivedType(typeof(RangeCapRule), "range-cap")]
[JsonDerivedType(typeof(MinRangeRule), "min-range")]
[JsonDerivedType(typeof(SightRule), "sight")]
[JsonDerivedType(typeof(ZoneRule), "zone")]
[JsonDerivedType(typeof(PushRule), "push")]
[JsonDerivedType(typeof(CooldownRule), "cooldown")]
[JsonDerivedType(typeof(CostRule), "cost")]
[JsonDerivedType(typeof(OncePerTurnRule), "once-per-turn")]
[JsonDerivedType(typeof(DeckRule), "deck")]
[JsonDerivedType(typeof(PointsRule), "points")]
[JsonDerivedType(typeof(ReserveRule), "reserve")]
[JsonDerivedType(typeof(BonusRule), "bonus")]
[JsonDerivedType(typeof(StillRule), "still")]
[JsonDerivedType(typeof(DanceRule), "dance")]
[JsonDerivedType(typeof(ApCostRule), "ap-cost")]
[JsonDerivedType(typeof(FirstCastRule), "first-cast")]
[JsonDerivedType(typeof(GrantRule), "grant")]
[JsonDerivedType(typeof(SideStepRule), "side-step")]
[JsonDerivedType(typeof(BackedRule), "backed")]
[JsonDerivedType(typeof(RemanenceRule), "remanence")]
[JsonDerivedType(typeof(GlyphTargetRule), "glyph-target")]
[JsonDerivedType(typeof(HpRule), "hp")]
[JsonDerivedType(typeof(EchoRule), "echo")]
[JsonDerivedType(typeof(SupportRule), "support")]
[JsonDerivedType(typeof(BannerRule), "banner")]
[JsonDerivedType(typeof(ShareRule), "share")]
[JsonDerivedType(typeof(ReachRule), "reach")]
[JsonDerivedType(typeof(HarmonyRule), "harmony")]
[JsonDerivedType(typeof(RelieveRule), "relieve")]
[JsonDerivedType(typeof(MarkRule), "mark")]
[JsonDerivedType(typeof(CatalysisRule), "catalysis")]
[JsonDerivedType(typeof(LingerRule), "linger")]
[JsonDerivedType(typeof(WaveRule), "wave")]
[JsonDerivedType(typeof(SharedMarkRule), "shared-mark")]
[JsonDerivedType(typeof(VirtuosoRule), "virtuoso")]
public abstract record TalentRule;

// The second floor and the keys of Reactions (sprint 144, D80, docs/talents-arbre.md §4.3).

/// <summary>Living: the bearer's marks last <see cref="ExtraTurns"/> more of its turns (Lasting Mark).</summary>
public sealed record MarkRule(int ExtraTurns) : TalentRule;

/// <summary>Living: a reaction also deals <see cref="Percent"/> % of the hit that set it off, neutral; one reaction a turn at most (Catalysis).</summary>
public sealed record CatalysisRule(int Percent) : TalentRule;

/// <summary>Living: the states the bearer sets off last <see cref="ExtraTurns"/> more; its hits that set one off, <see cref="TriggerPercent"/> % (Lingering Reaction).</summary>
public sealed record LingerRule(int ExtraTurns, int TriggerPercent) : TalentRule;

/// <summary>Living: an area sets reactions off on <see cref="Extra"/> more marked fighters; the areas, <see cref="ZonePercent"/> % (Wave).</summary>
public sealed record WaveRule(int Extra, int ZonePercent) : TalentRule;

/// <summary>Living: the bearer's marks carry its reactions: an ally completing one sets it off without the talent (Shared Mark).</summary>
public sealed record SharedMarkRule : TalentRule;

/// <summary>Living: completing its own mark, <see cref="Percent"/> % more; the allies' marks no longer react for it (Virtuoso).</summary>
public sealed record VirtuosoRule(int Percent) : TalentRule;

// The branch Support (sprint 143, D80, docs/talents-arbre.md §4.3 and §4.6, the idea of Charles).

/// <summary>
/// Living: the bearer's element spells cast on an ally (a summon too, never itself) help it instead of hurting it, by
/// their element (<see cref="Support"/>): <see cref="PerTurn"/> a turn, each at <see cref="Percent"/> % (Helping Hand,
/// Double Hand, Devotion add up: the most supports, the product of the shares).
/// </summary>
public sealed record SupportRule(int PerTurn, int Percent) : TalentRule;

/// <summary>Living: at the start of the bearer's turn, the allies within <see cref="Cells"/> cells deal <see cref="Percent"/> % more for a turn (Banner).</summary>
public sealed record BannerRule(int Cells, int Percent) : TalentRule;

/// <summary>Living: the ally helped deals <see cref="Ally"/> % more for a turn, the bearer <see cref="Self"/> % (Shared Momentum).</summary>
public sealed record ShareRule(int Ally, int Self) : TalentRule;

/// <summary>Living: spells cast on an ally reach <see cref="Ally"/> cells more, on an enemy <see cref="Enemy"/> (Long Hand).</summary>
public sealed record ReachRule(int Ally, int Enemy) : TalentRule;

/// <summary>Living: a support also gives <see cref="Percent"/> % of resistance to the spell's element for a turn, and costs <see cref="ApCost"/> more (Harmony).</summary>
public sealed record HarmonyRule(int Percent, int ApCost) : TalentRule;

/// <summary>Living: once a fight, a support on an ally under <see cref="Below"/> % of its life is doubled and costs no action point; the bearer's next turn, <see cref="Penalty"/> fewer (Relief).</summary>
public sealed record RelieveRule(int Below, int Penalty) : TalentRule;

// The branch Terrain (sprint 142, D80, docs/talents-arbre.md §4.3 and §4.6).

/// <summary>Compiled: a spell joins the grimoire (Relay, Seal, Decoy): a spell of the tree, no class's.</summary>
public sealed record GrantRule(string Spell) : TalentRule;

/// <summary>Living: after a spell, one movement point more, once per turn (Side Step).</summary>
public sealed record SideStepRule : TalentRule;

/// <summary>Living: next to an obstacle, <see cref="Damage"/> % of damage and <see cref="Resistance"/> % of resistance; elsewhere <see cref="Otherwise"/> % of damage (Backed).</summary>
public sealed record BackedRule(int Damage, int Resistance, int Otherwise) : TalentRule;

/// <summary>
/// Living: a damage spell striking nobody lays a glyph of its element for <see cref="Turns"/> of the bearer's turns, a
/// diamond of the spell's cost in radius (at most <see cref="MaxRadius"/>; each action point beyond, 10 % more), of
/// <see cref="Percent"/> % of the spell's damage, falling away from the centre; one at a time (Remanence, §4.6).
/// </summary>
public sealed record RemanenceRule(int Percent, int Turns, int MaxRadius) : TalentRule;

/// <summary>Living: the hits on a fighter standing in one of the bearer's glyphs <see cref="Inside"/> %, on the others <see cref="Outside"/> % (Scorched Earth).</summary>
public sealed record GlyphTargetRule(int Inside, int Outside) : TalentRule;

/// <summary>The bearer's maximum hit points <see cref="Percent"/> % more (or fewer) in a fight (Nomad).</summary>
public sealed record HpRule(int Percent) : TalentRule;

/// <summary>
/// Living: having walked at least <see cref="Cells"/> cells in a turn, the bearer leaves on the cell it started from an
/// echo of its last damage spell, which strikes the same target at <see cref="Percent"/> % at the start of its next turn,
/// if the target is within the spell's range of the echo (Echo).
/// </summary>
public sealed record EchoRule(int Percent, int Cells) : TalentRule;

// The branches Distance and Rhythm (sprint 141, D80, docs/talents-arbre.md §4.3).

/// <summary>Compiled: the spells reach at most <see cref="Max"/> cells (Close Quarters).</summary>
public sealed record RangeCapRule(int Max) : TalentRule;

/// <summary>Compiled: the damage spells reach no closer than <see cref="Min"/> cells (Artillery), and at least that far.</summary>
public sealed record MinRangeRule(int Min) : TalentRule;

/// <summary>Compiled: the spells cast in line need no line of sight; the others reach <see cref="OthersChange"/> cells more (Clear Line).</summary>
public sealed record SightRule(int OthersChange) : TalentRule;

/// <summary>Compiled: the areas shrink a notch, their damage brought back up by the budget's area factors (Tight Zone).</summary>
public sealed record ZoneRule : TalentRule;

/// <summary>Compiled and living: pushes go <see cref="Cells"/> farther and cost <see cref="ApCost"/> more; a collision hurts <see cref="Collision"/> a cell (Heavy Push).</summary>
public sealed record PushRule(int Cells, int ApCost, int Collision) : TalentRule;

/// <summary>Compiled: the cooldowns <see cref="Change"/> turns shorter, never under 0, the damage <see cref="PercentPerTurn"/> % lower for each (Short Cooldown).</summary>
public sealed record CooldownRule(int Change, int PercentPerTurn) : TalentRule;

/// <summary>Compiled: the spells cost <see cref="Change"/> action points more (or fewer), never under <see cref="Floor"/> (Thrift).</summary>
public sealed record CostRule(int Change, int Floor) : TalentRule;

/// <summary>Compiled: each spell once per turn at most (One Gesture per Spell).</summary>
public sealed record OncePerTurnRule : TalentRule;

/// <summary>The deck holds <see cref="Size"/> spells at most (Tight Deck, Thrift): the smallest such rule wins.</summary>
public sealed record DeckRule(int Size) : TalentRule;

/// <summary>Living: action and movement points more (or fewer) at the start of the bearer's turns <see cref="FromTurn"/> to <see cref="UntilTurn"/> (0: on and on).</summary>
public sealed record PointsRule(int Ap = 0, int Mp = 0, int FromTurn = 1, int UntilTurn = 0) : TalentRule;

/// <summary>Living: up to <see cref="Max"/> action points left at the end of a turn are kept for the next (Reserve).</summary>
public sealed record ReserveRule(int Max) : TalentRule;

/// <summary>Living: every hit <see cref="Percent"/> % harder (or softer).</summary>
public sealed record BonusRule(int Percent) : TalentRule;

/// <summary>Living: <see cref="Still"/> % if the bearer spent no movement point this turn, <see cref="Moved"/> % otherwise (Firm Footing).</summary>
public sealed record StillRule(int Still, int Moved) : TalentRule;

/// <summary>Living: <see cref="PerCell"/> % for every cell walked since the last spell, at most <see cref="Max"/> % (Dancing Step).</summary>
public sealed record DanceRule(int PerCell, int Max) : TalentRule;

/// <summary>Living: the spells costing <see cref="MinAp"/> to <see cref="MaxAp"/> action points hit <see cref="Percent"/> % harder (or softer).</summary>
public sealed record ApCostRule(int MinAp, int MaxAp, int Percent) : TalentRule;

/// <summary>Living: the turn's first damage spell <see cref="First"/> %, the following ones <see cref="Others"/> % (First Gesture).</summary>
public sealed record FirstCastRule(int First, int Others) : TalentRule;

/// <summary>Compiled: the spells of the base element <see cref="From"/> become their twins of the compound <see cref="To"/>.</summary>
public sealed record TransmuteRule(Element From, Element To) : TalentRule;

/// <summary>Compiled: the spells of both parents become their twins of the compound (a key of Matter).</summary>
public sealed record ConvertRule(Element Element) : TalentRule;

/// <summary>Living: the compounds' signatures (the Arc) <see cref="Percent"/> % stronger.</summary>
public sealed record SignatureRule(int Percent) : TalentRule;

/// <summary>Living: the spells of an element hit <see cref="Percent"/> % harder, the other elements' (not the neutral) <see cref="Others"/> %.</summary>
public sealed record ElementRule(Element Element, int Percent, int Others = 0) : TalentRule;

/// <summary>Living: the compounds' spells hit <see cref="Percent"/> % harder (or softer).</summary>
public sealed record CompoundRule(int Percent) : TalentRule;

/// <summary>Living: a pair's hit leaves the mark of its first parent; it still sets off nothing.</summary>
public sealed record PrismRule : TalentRule;

/// <summary>The bearer's hit of another base element on a fighter it did not mark itself sets off this reaction (<see cref="ReactionSpec"/>).</summary>
public sealed record ReactionRule(string Reaction) : TalentRule;

/// <summary>
/// Compiled: every spell reaching farther than 1 cell reaches <see cref="Change"/> cells more (or fewer), never under
/// its minimum range nor under 1.
/// </summary>
/// <param name="AtLeast">Only the spells reaching that far already (Long Sight: 4).</param>
public sealed record RangeRule(int Change, int AtLeast = 0) : TalentRule;

/// <summary>Living: the bearer's spells hit <see cref="Percent"/> % harder (or softer) on a target <see cref="MinCells"/> to <see cref="MaxCells"/> cells away.</summary>
public sealed record DistanceRule(int MinCells, int MaxCells, int Percent) : TalentRule;

/// <summary>
/// A talent of the tree shared by every class (D80), as described in <c>data/talents.json</c>: no talent names a class
/// or a spell. <see cref="Excludes"/> cannot be taken with it; <see cref="Requires"/> must be taken first. Since sprint 146
/// (D82), one of <see cref="RequiresAny"/> must be (a parent among several, G7 a), and not before <see cref="Level"/>; a
/// talent with a parent asks no points below it in its branch (G4 a).
/// </summary>
/// <remarks>
/// Since sprint 149 (D82, M4 a, M8 b): its <see cref="Counterpart"/> said apart, a line of <see cref="Lore"/>, and its
/// <see cref="Icon"/> (a game-icons.net drawing, credited in <c>godot/assets/icons/talents/credits.json</c>).
/// </remarks>
public sealed record TalentSpec(string Id, LocalizedText Name, LocalizedText Description, Branch Branch, Tier Tier, IReadOnlyList<TalentRule> Rules,
    IReadOnlyList<string>? Excludes = null, IReadOnlyList<string>? Requires = null, IReadOnlyList<string>? RequiresAny = null, int Level = 0,
    LocalizedText? Counterpart = null, LocalizedText? Lore = null, string? Icon = null)
{
    /// <summary>Whether a talent it asks for comes first (<see cref="Requires"/> or <see cref="RequiresAny"/>).</summary>
    public bool HasParent => Requires is { Count: > 0 } || RequiresAny is { Count: > 0 };

    /// <summary>A pair's node (sprint 146, C2): the talent that gives a reaction, which takes its two elements.</summary>
    public bool IsPair => Rules.OfType<ReactionRule>().Any();

    public TalentFamily Family => Branch is Branch.Braids or Branch.Pure ? TalentFamily.Matter : TalentFamily.Manner;

    /// <summary>A big switch (sprint 147, E10 a): a talent that changes the whole grimoire; one in the whole tree.</summary>
    public bool IsSwitch => Rules.OfType<ConvertRule>().Any();

    /// <summary>A node of the fork (sprint 147): a pair or a Heart, the doors of the styles.</summary>
    public bool IsFork => Branch is Branch.Fork or Branch.Pure;

    /// <summary>1 point, 2 for a key.</summary>
    public int Cost => Tier == Tier.Key ? 2 : 1;
}

/// <summary>
/// An elemental reaction (D80, <c>docs/talents-arbre.md</c> §6), as described in <c>data/reactions.json</c>: a hit of
/// <see cref="First"/> on a fighter marked by <see cref="Second"/>, or the other way round, by a hero with the talent,
/// sets <see cref="Statuses"/> on it, named after the reaction.
/// </summary>
public sealed record ReactionSpec(string Id, LocalizedText Name, LocalizedText Description, Element First, Element Second, IReadOnlyList<StatusEffect> Statuses)
{
    public bool Joins(Element a, Element b) => (a == First && b == Second) || (a == Second && b == First);
}

/// <summary>
/// The mark a hero's direct hit of a base element leaves on an enemy (sprint 137): one per fighter, until the end of its
/// <see cref="Source"/>'s next turn (<see cref="TurnsLeft"/> of its turns' ends).
/// </summary>
public sealed record Mark(Element Element, int Source, int TurnsLeft);

/// <summary>
/// What a living talent reads of a hit (sprint 141): whether the caster walked this turn, the cells it walked since its last
/// spell, the damage spells it cast before this one this turn, and the spell's cost in action points.
/// </summary>
/// <param name="Backed">The caster stands next to an obstacle (sprint 142).</param>
/// <param name="InMyGlyph">The target stands in one of the caster's glyphs (sprint 142).</param>
/// <param name="Triggers">The hit sets a reaction off (sprint 144); <paramref name="OwnMark"/>, on the caster's own mark; <paramref name="Zone"/>, a spell with an area.</param>
public sealed record HitContext(bool Walked = false, int CellsSinceSpell = 0, int DamageCasts = 0, int ApCost = 0, bool Backed = false, bool InMyGlyph = false,
    bool Triggers = false, bool OwnMark = false, bool Zone = false);

/// <summary>
/// What a spell cast on an ally does instead of hurting it (sprint 143, §4.6): a heal at most the maximum's share, a
/// shield, a resistance to every element, movement points and range; for a turn.
/// </summary>
public sealed record SupportEffect(int Heal, int Shield, int Resistance, int Mp, int Range)
{
    /// <summary>
    /// The table by element of §4.6 on <paramref name="d"/>, the spell's average damage with its characteristic: Fire heals
    /// 40 % of it (at most 15 % of the ally's maximum), Earth shields 60 %, Water gives 5 % of resistance a point of
    /// action (20 at most), Air a movement point every two (2 at most) and a cell of range; a pair, both its parents at
    /// 60 %; all of it times <paramref name="percent"/> %.
    /// </summary>
    public static SupportEffect Of(Element element, double d, int apCost, int allyMaxHp, int percent)
    {
        IReadOnlyList<Element> parents = Elements.IsCompound(element) ? Elements.Parents(element) : [element];
        double share = percent / 100.0 * (parents.Count > 1 ? 0.6 : 1);
        double heal = 0, shield = 0, resistance = 0, mp = 0, range = 0;
        foreach (Element e in parents)
        {
            switch (e)
            {
                case Element.Fire:
                    heal += 0.4 * d * share;
                    break;
                case Element.Earth:
                    shield += 0.6 * d * share;
                    break;
                case Element.Water:
                    resistance += Math.Min(20, 5 * apCost) * share;
                    break;
                case Element.Air:
                    mp += Math.Min(2, apCost / 2) * share;
                    range += share;
                    break;
            }
        }
        return new SupportEffect((int)Math.Round(Math.Min(heal, allyMaxHp * 0.15)), (int)Math.Round(shield), (int)Math.Round(resistance),
            (int)Math.Round(mp), (int)Math.Round(range));
    }
}

/// <summary>A hero's grimoire (sprint 138): the spells it may fight with, and the class spells its talents changed into twins.</summary>
public sealed record Grimoire(IReadOnlyList<string> Spells, IReadOnlyDictionary<string, string> Replaced);

/// <summary>The tree's rules (D80): its points by level, what a choice of talents may hold, the bonuses it gives.</summary>
public static class Talents
{
    /// <summary>The tree opens at this level: nothing changes before it (chapter 1 and the balance of the fields).</summary>
    public const int Opens = 30;

    /// <summary>
    /// The version of the tree (sprint 146, D82): 1 the tree of sprints 137 to 144, 2 since the trunk and the arbitrated
    /// Lightning. Talents taken in an older one are given back once, for free, with a word at the connection (I5 a).
    /// </summary>
    public const int Version = 3;

    /// <summary>The ceiling of the tree's conditional bonuses on one hit, in percent (§4.4).</summary>
    public const int Ceiling = 35;

    /// <summary>2 points at level 30, then one every 3 levels (sprint 146, D82, G1 b): 7 at 45, 12 at 60, 25 at 100.</summary>
    public static int Points(int level) => level < Opens ? 0 : 2 + (level - Opens) / 3;

    /// <summary>
    /// The trunk (sprint 146, D82, B1 a and B2 b): three nodes that light up by themselves, no point spent, the same for
    /// every class: more damage at 30 (every spell, neutral included, outside <see cref="Ceiling"/>), more resistance at 35
    /// (points added to each base element, under the ceiling of 90), more hit points at 40 (of the maximum, Vitality
    /// included). If they make the fights too easy, the trunk is lowered, not the monsters (J7 b).
    /// </summary>
    public static readonly IReadOnlyList<TrunkNode> Trunk = [new(30, TrunkGain.Damage, 3), new(35, TrunkGain.Resistance, 3), new(40, TrunkGain.Hp, 5)];

    public static int TrunkDamage(int level) => TrunkOf(level, TrunkGain.Damage);

    public static int TrunkResistance(int level) => TrunkOf(level, TrunkGain.Resistance);

    public static int TrunkHp(int level) => TrunkOf(level, TrunkGain.Hp);

    private static int TrunkOf(int level, TrunkGain gain) => Trunk.Where(t => t.Gain == gain && level >= t.Level).Sum(t => t.Percent);

    /// <summary>Earth, Fire, Water and Air: the elements that leave marks and react.</summary>
    public static bool IsBase(Element element) => element is Element.Earth or Element.Fire or Element.Water or Element.Air;

    /// <summary>
    /// Why these talents cannot be held at this level, or null: known and each once, from level 30, within its points;
    /// a second floor after 2 points in the branch's first, a key after 3 below it; one key per branch and one of Matter
    /// in the whole tree; nothing it excludes, everything it requires. Since sprint 146 (D82): a talent with a parent asks
    /// no points below it, one of its parents among several, its level; two pairs share no element (C2).
    /// </summary>
    public static string? Problem(IReadOnlyList<string> talents, int level, GameData data)
    {
        ArgumentNullException.ThrowIfNull(talents);
        ArgumentNullException.ThrowIfNull(data);
        if (talents.Count == 0)
            return null;
        if (talents.FirstOrDefault(id => !data.Talents.ContainsKey(id)) is string unknown)
            return $"Unknown talent '{unknown}'.";
        if (talents.Distinct(StringComparer.Ordinal).Count() != talents.Count)
            return "A talent is taken once.";
        if (level < Opens)
            return $"The talents open at level {Opens}.";
        TalentSpec[] taken = [.. talents.Select(id => data.Talents[id])];
        if (taken.Sum(t => t.Cost) > Points(level))
            return $"At most {Points(level)} talent points at level {level}.";
        foreach (TalentSpec t in taken)
        {
            int below = taken.Where(o => o.Branch == t.Branch && o.Tier < t.Tier).Sum(o => o.Cost);
            int asked = t.HasParent ? 0 : t.Tier == Tier.Key ? 3 : t.Tier == Tier.Second ? SecondFloor(t.Branch) : 0;
            if (below < asked)
                return $"'{t.Id}' asks {asked} points spent below it in its branch.";
            if (taken.FirstOrDefault(o => o != t && ((t.Excludes ?? []).Contains(o.Id) || (o.Excludes ?? []).Contains(t.Id))) is TalentSpec other)
                return $"'{t.Id}' and '{other.Id}' exclude each other.";
            if ((t.Requires ?? []).FirstOrDefault(r => !talents.Contains(r)) is string missing)
                return $"'{t.Id}' asks for '{missing}'.";
            if (t.RequiresAny is { Count: > 0 } any && !any.Any(talents.Contains))
                return $"'{t.Id}' asks for one of {string.Join(", ", any.Select(a => $"'{a}'"))}.";
            if (level < t.Level)
                return $"'{t.Id}' opens at level {t.Level}.";
        }
        if (Clash(taken, data) is (Element shared, TalentSpec pair))
            return $"{shared} is already taken by '{pair.Id}'.";
        if (taken.Where(t => t.Tier == Tier.Key).GroupBy(t => t.Branch).Any(g => g.Count() > 1))
            return "One key per branch.";
        if (ElementsOpened(taken) > MaxOpened)
            return $"At most {MaxOpened} elements opened.";
        return taken.Count(t => t.IsSwitch) > 1 ? "One big switch in the whole tree." : null;
    }

    /// <summary>The elements a hero may open, at most (sprint 140, §4.1): compounds, and later Light or Darkness.</summary>
    public const int MaxOpened = 2;

    /// <summary>
    /// The first pair that shares an element with one taken before it (sprint 146, D82, C2: "if it has Water-Fire it cannot
    /// have Water-Air, Water is already taken"): the element shared and the pair that took it first; null when none does.
    /// </summary>
    public static (Element Element, TalentSpec Pair)? Clash(IEnumerable<TalentSpec> taken, GameData data)
    {
        ArgumentNullException.ThrowIfNull(taken);
        ArgumentNullException.ThrowIfNull(data);
        var by = new Dictionary<Element, TalentSpec>();
        foreach (TalentSpec t in taken.Where(t => t.IsPair))
        {
            foreach (ReactionRule r in t.Rules.OfType<ReactionRule>())
            {
                ReactionSpec reaction = data.Reactions[r.Reaction];
                foreach (Element e in new[] { reaction.First, reaction.Second })
                {
                    if (by.TryGetValue(e, out TalentSpec? first) && first != t)
                        return (e, first);
                    by[e] = t;
                }
            }
        }
        return null;
    }

    /// <summary>The elements these talents open: each compound transmuted or converted (since sprint 146), once.</summary>
    public static int ElementsOpened(IEnumerable<TalentSpec> talents)
    {
        ArgumentNullException.ThrowIfNull(talents);
        TalentRule[] rules = [.. talents.SelectMany(t => t.Rules)];
        return rules.OfType<TransmuteRule>().Select(r => r.To).Concat(rules.OfType<ConvertRule>().Select(r => r.Element)).Distinct().Count();
    }

    /// <summary>Talents are taken back for free up to this level; above, it costs <see cref="ChangeCost"/> Éclats, in a village (§4.2).</summary>
    public const int FreeChangeUpTo = 40;

    /// <summary>What giving back saved talents costs at a level, in Éclats: nothing up to 40, then 10 × the level.</summary>
    public static long ChangeCost(int level) => level <= FreeChangeUpTo ? 0 : 10L * level;

    /// <summary>
    /// Whether new talents give back any saved one (taking more is free). A Transmutation the Conversion of its compound
    /// replaced is not given back (sprint 146, D82, I2 a): it is growing stronger, not a regret.
    /// </summary>
    public static bool GivesBack(IReadOnlyList<string>? before, IReadOnlyList<string>? after, GameData data)
    {
        ArgumentNullException.ThrowIfNull(data);
        Element[] converted = [.. (after ?? []).Where(data.Talents.ContainsKey).SelectMany(id => data.Talents[id].Rules).OfType<ConvertRule>().Select(r => r.Element)];
        return (before ?? []).Except(after ?? [], StringComparer.Ordinal)
            .Any(id => !(data.Talents.TryGetValue(id, out TalentSpec? t) && t.Rules.OfType<TransmuteRule>().Any(r => converted.Contains(r.To))));
    }

    /// <summary>
    /// The talents left once <paramref name="given"/> is given back (sprint 147, D82, I1 b): with it, every talent that
    /// asked for it and has no other parent taken, and so on down; in the order they were taken.
    /// </summary>
    public static List<string> Without(IReadOnlyList<string> talents, string given, GameData data)
    {
        ArgumentNullException.ThrowIfNull(talents);
        ArgumentNullException.ThrowIfNull(data);
        List<string> left = [.. talents.Where(id => id != given)];
        bool Stands(string id) => data.Talents[id] is TalentSpec t && (t.Requires ?? []).All(left.Contains) && (t.RequiresAny is not { Count: > 0 } any || any.Any(left.Contains));
        while (left.FirstOrDefault(id => !Stands(id)) is string fallen)
            left.Remove(fallen);
        return left;
    }

    /// <summary>
    /// The talents with <paramref name="taken"/> added, as the tree's window takes it (sprint 146, D82): a Conversion takes
    /// the place of the Transmutation of its compound, whose point comes back.
    /// </summary>
    public static List<string> With(IReadOnlyList<string> talents, string taken, GameData data)
    {
        ArgumentNullException.ThrowIfNull(talents);
        ArgumentNullException.ThrowIfNull(data);
        Element[] converts = [.. data.Talents[taken].Rules.OfType<ConvertRule>().Select(r => r.Element)];
        return [.. talents.Where(id => !(converts.Length > 0 && data.Talents.TryGetValue(id, out TalentSpec? t) && t.Rules.OfType<TransmuteRule>().Any(r => converts.Contains(r.To)))), taken];
    }

    /// <summary>
    /// The points below a second floor: 2, but 1 in Braids, whose first floor opens elements, two at most; the game
    /// designer's builds (§4.5) take a second floor of Braids after one opening (sprint 138).
    /// </summary>
    public static int SecondFloor(Branch branch) => branch == Branch.Braids ? 1 : 2;

    /// <summary>
    /// The spells a hero may put in its deck (sprint 138): its class's that its level has unlocked, in the class's order,
    /// those of a transmuted or converted element replaced by their twins; then the twins of the compounds it opened.
    /// <see cref="Grimoire.Replaced"/> says what became what, for a deck chosen before.
    /// </summary>
    public static Grimoire GrimoireOf(Hero hero, GameData data)
    {
        ArgumentNullException.ThrowIfNull(hero);
        ArgumentNullException.ThrowIfNull(data);
        if (data.Class(hero.Class) is not HeroClass c)
            return new Grimoire([], new Dictionary<string, string>());
        TalentRule[] rules = [.. (hero.Talents ?? []).Where(data.Talents.ContainsKey).SelectMany(id => data.Talents[id].Rules)];
        var into = new Dictionary<Element, Element>();
        foreach (TransmuteRule t in rules.OfType<TransmuteRule>())
            into[t.From] = t.To;
        foreach (ConvertRule cv in rules.OfType<ConvertRule>())
        {
            foreach (Element parent in Elements.Parents(cv.Element))
                into[parent] = cv.Element;
        }
        Spell? TwinOf(string origin, Element compound) => data.Spells.Values.FirstOrDefault(s => s.Twin == origin && s.Element == compound);
        var spells = new List<string>();
        var replaced = new Dictionary<string, string>(StringComparer.Ordinal);
        string[] unlocked = [.. c.Spells.Where(id => data.Spells[id].Level <= hero.Level)];
        foreach (string id in unlocked)
        {
            if (into.TryGetValue(data.Spells[id].Element, out Element compound) && TwinOf(id, compound) is Spell twin)
            {
                replaced[id] = twin.Id;
                spells.Add(twin.Id);
            }
            else
            {
                spells.Add(id);
            }
        }
        // The spells of the tree (sprint 142): after the class's.
        foreach (GrantRule g in rules.OfType<GrantRule>().Where(g => !spells.Contains(g.Spell)))
            spells.Add(g.Spell);
        return new Grimoire(spells, replaced);
    }

    /// <summary>The resistance in percent the talents give a fighter next to an obstacle (Backed, sprint 142); 0 elsewhere.</summary>
    public static int Guard(IReadOnlyList<TalentSpec> talents, bool backed) => backed ? talents.SelectMany(t => t.Rules).OfType<BackedRule>().Sum(r => r.Resistance) : 0;

    /// <summary>The compounds' signatures' bonus, in percent: the strongest, they do not add up (Pressed Signature and Conversion, §4.5: "Arc at 50 %").</summary>
    public static int SignatureBonus(IReadOnlyList<TalentSpec> talents) => talents.SelectMany(t => t.Rules).OfType<SignatureRule>().Select(r => r.Percent).DefaultIfEmpty(0).Max();

    /// <summary>Whether the talents convert their holder's spells to this compound (<see cref="ConvertRule"/>): it then strikes on twice its parents' average.</summary>
    public static bool Converts(IReadOnlyList<TalentSpec> talents, Element element) => talents.SelectMany(t => t.Rules).OfType<ConvertRule>().Any(r => r.Element == element);

    /// <summary>A pair's hit leaves its first parent's mark (<see cref="PrismRule"/>).</summary>
    public static bool HasPrism(IReadOnlyList<TalentSpec> talents) => talents.SelectMany(t => t.Rules).OfType<PrismRule>().Any();

    /// <summary>A spell as the talents compile it before the fight: its range changed (<see cref="RangeRule"/>).</summary>
    /// <remarks>
    /// Since sprint 141, every compiled rule of Distance and Rhythm, in this order: the minimum range of the damage spells,
    /// the range (never under 1 nor under the minimum, for a spell reaching farther than 1), its cap, the line of sight,
    /// the area, the push, the cooldown, the cost, the casts per turn.
    /// </remarks>
    public static Spell Compile(Spell spell, IReadOnlyList<TalentSpec> talents)
    {
        ArgumentNullException.ThrowIfNull(spell);
        ArgumentNullException.ThrowIfNull(talents);
        TalentRule[] rules = [.. talents.SelectMany(t => t.Rules)];
        if (rules.Length == 0)
            return spell;
        Spell s = spell;
        foreach (MinRangeRule m in rules.OfType<MinRangeRule>().Where(_ => s.DamageMax > 0 && s.MaxRange > 0))
            s = s with { MinRange = Math.Max(s.MinRange, m.Min), MaxRange = Math.Max(s.MaxRange, m.Min) };
        int change = rules.OfType<RangeRule>().Where(r => spell.MaxRange >= r.AtLeast).Sum(r => r.Change)
            + (s.InLine ? 0 : rules.OfType<SightRule>().Sum(r => r.OthersChange));
        if (change != 0 && s.MaxRange > 1)
            s = s with { MaxRange = Math.Max(Math.Max(1, s.MinRange), s.MaxRange + change) };
        foreach (RangeCapRule cap in rules.OfType<RangeCapRule>())
            s = s with { MaxRange = Math.Min(s.MaxRange, Math.Max(cap.Max, s.MinRange)) };
        if (s.InLine && rules.OfType<SightRule>().Any())
            s = s with { LineOfSight = false };
        if (rules.OfType<ZoneRule>().Any() && s.Area is Area area && area.Radius > 0)
        {
            Area smaller = area.Radius == 1 ? Area.One : area with { Radius = area.Radius - 1 };
            double ratio = AreaFactor(smaller) / AreaFactor(area);
            s = Scaled(s, ratio) with { Area = smaller.Shape == AreaShape.Point ? null : smaller };
        }
        if (rules.OfType<PushRule>().ToArray() is { Length: > 0 } pushes && s.AllEffects.Any(e => e is PushEffect))
        {
            s = s with
            {
                ApCost = s.ApCost + pushes.Sum(p => p.ApCost),
                Effects = [.. s.AllEffects.Select(e => e is PushEffect p ? p with { Cells = p.Cells + pushes.Sum(r => r.Cells) } : e)],
            };
        }
        foreach (CooldownRule cd in rules.OfType<CooldownRule>().Where(_ => s.Cooldown > 0))
        {
            int removed = Math.Min(s.Cooldown, -cd.Change);
            s = Scaled(s with { Cooldown = s.Cooldown - removed }, 1 - cd.PercentPerTurn * removed / 100.0);
        }
        foreach (CostRule c in rules.OfType<CostRule>().Where(c => s.ApCost > c.Floor))
            s = s with { ApCost = Math.Max(c.Floor, s.ApCost + c.Change) };
        if (rules.OfType<OncePerTurnRule>().Any())
            s = s with { PerTurn = 1 };
        return s;
    }

    /// <summary>The budget's factor of an area (<c>tools/spells_gen.py</c>): a single cell 1, the larger the less.</summary>
    private static double AreaFactor(Area a) => (a.Shape, a.Radius) switch
    {
        (AreaShape.Point, _) => 1,
        (AreaShape.Cross, 1) or (AreaShape.Line, 2) => 0.8,
        (AreaShape.Line, 1) => 0.9,
        (AreaShape.Circle, 1) => 0.7,
        _ => 0.6,
    };

    /// <summary>A spell's damage times a ratio, rounded, never under 1 for a spell that hurts.</summary>
    private static Spell Scaled(Spell s, double ratio) => s.DamageMax <= 0 ? s
        : s with { DamageMin = Math.Max(1, (int)Math.Round(s.DamageMin * ratio)), DamageMax = Math.Max(1, (int)Math.Round(s.DamageMax * ratio)) };

    /// <summary>The spells a deck holds with these talents (sprint 141): <see cref="Hero.DeckSize"/>, or fewer (Tight Deck, Thrift).</summary>
    public static int DeckSize(IEnumerable<TalentSpec> talents) =>
        talents.SelectMany(t => t.Rules).OfType<DeckRule>().Select(r => r.Size).Append(Hero.DeckSize).Min();

    /// <summary>The damage a collision does a cell, with these talents (Heavy Push).</summary>
    public static int Collision(IReadOnlyList<TalentSpec> talents) =>
        talents.SelectMany(t => t.Rules).OfType<PushRule>().Select(r => r.Collision).Append(Fight.CollisionDamage).Max();

    /// <summary>Action and movement points the talents add at the start of the bearer's turn number <paramref name="turn"/> (1 for its first).</summary>
    public static (int Ap, int Mp) TurnPoints(IReadOnlyList<TalentSpec> talents, int turn)
    {
        PointsRule[] rules = [.. talents.SelectMany(t => t.Rules).OfType<PointsRule>().Where(r => turn >= r.FromTurn && (r.UntilTurn == 0 || turn <= r.UntilTurn))];
        return (rules.Sum(r => r.Ap), rules.Sum(r => r.Mp));
    }

    /// <summary>The action points left at the end of a turn the talents keep for the next (Reserve).</summary>
    public static int Reserve(IReadOnlyList<TalentSpec> talents, int apLeft) =>
        Math.Min(apLeft, talents.SelectMany(t => t.Rules).OfType<ReserveRule>().Sum(r => r.Max));

    /// <summary>
    /// The living talents' bonus on a hit in <paramref name="element"/> from <paramref name="from"/> on a fighter standing
    /// on <paramref name="at"/>, in percent: at most <see cref="Ceiling"/>, at least -100.
    /// </summary>
    /// <param name="context">The bearer's turn so far (sprint 141): whether it walked, the cells since its last spell, its damage spells cast; and the spell's cost.</param>
    public static int Percent(IReadOnlyList<TalentSpec> talents, Cell from, Cell at, Element element = Element.Neutral, HitContext? context = null)
    {
        ArgumentNullException.ThrowIfNull(talents);
        int distance = from.DistanceTo(at);
        TalentRule[] rules = [.. talents.SelectMany(t => t.Rules)];
        if (rules.Length == 0)
            return 0;
        HitContext c = context ?? new HitContext();
        int sum = rules.OfType<DistanceRule>().Where(r => distance >= r.MinCells && distance <= r.MaxCells).Sum(r => r.Percent)
            + rules.OfType<ElementRule>().Sum(r => r.Element == element ? r.Percent : element == Element.Neutral ? 0 : r.Others)
            + (Elements.IsCompound(element) ? rules.OfType<CompoundRule>().Sum(r => r.Percent) : 0)
            + rules.OfType<BonusRule>().Sum(r => r.Percent)
            + rules.OfType<StillRule>().Sum(r => c.Walked ? r.Moved : r.Still)
            + rules.OfType<DanceRule>().Sum(r => Math.Min(r.Max, r.PerCell * c.CellsSinceSpell))
            + rules.OfType<ApCostRule>().Where(r => c.ApCost >= r.MinAp && c.ApCost <= r.MaxAp).Sum(r => r.Percent)
            + rules.OfType<FirstCastRule>().Sum(r => c.DamageCasts == 0 ? r.First : r.Others)
            + rules.OfType<BackedRule>().Sum(r => c.Backed ? r.Damage : r.Otherwise)
            + rules.OfType<GlyphTargetRule>().Sum(r => c.InMyGlyph ? r.Inside : r.Outside)
            + (c.Triggers ? rules.OfType<LingerRule>().Sum(r => r.TriggerPercent) : 0)
            + (c.Triggers && c.OwnMark ? rules.OfType<VirtuosoRule>().Sum(r => r.Percent) : 0)
            + (c.Zone ? rules.OfType<WaveRule>().Sum(r => r.ZonePercent) : 0);
        return Math.Clamp(sum, -100, Ceiling);
    }

    /// <summary>
    /// Each talent's own part of <see cref="Percent"/> on a hit (sprint 150, D82, O6 a), those that give or take something:
    /// the forecast details them. Their sum is the hit's bonus while under <see cref="Ceiling"/> (and over -100).
    /// </summary>
    public static IReadOnlyList<TalentShare> Shares(IReadOnlyList<TalentSpec> talents, Cell from, Cell at, Element element = Element.Neutral, HitContext? context = null)
    {
        ArgumentNullException.ThrowIfNull(talents);
        return [.. talents.Select(t => new TalentShare(t.Id, Percent([t], from, at, element, context))).Where(s => s.Percent != 0)];
    }

    /// <summary>
    /// The talents whose bonus depends on how the turn went (sprint 150, D82, O5 a): Firm Footing (lit while the bearer has
    /// not moved), Dancing Step (lit by the cells walked), First Gesture (lit before the first damage spell), Backed (lit by
    /// an obstacle), Scorched Earth's glyphs aside; with what each gives now.
    /// </summary>
    public static IReadOnlyList<TalentLight> Lights(IReadOnlyList<TalentSpec> talents, HitContext context)
    {
        ArgumentNullException.ThrowIfNull(talents);
        ArgumentNullException.ThrowIfNull(context);
        var lights = new List<TalentLight>();
        foreach (TalentSpec t in talents)
        {
            foreach (TalentRule r in t.Rules)
            {
                TalentLight? light = r switch
                {
                    StillRule still => new(t.Id, !context.Walked, context.Walked ? still.Moved : still.Still),
                    DanceRule dance => new(t.Id, context.CellsSinceSpell > 0, Math.Min(dance.Max, dance.PerCell * context.CellsSinceSpell)),
                    FirstCastRule first => new(t.Id, context.DamageCasts == 0, context.DamageCasts == 0 ? first.First : first.Others),
                    BackedRule backed => new(t.Id, context.Backed, context.Backed ? backed.Damage : backed.Otherwise),
                    _ => null,
                };
                if (light is not null)
                    lights.Add(light);
            }
        }
        return lights;
    }

    /// <summary>The reaction these talents set off between two elements, or null.</summary>
    public static ReactionSpec? ReactionOf(IReadOnlyList<TalentSpec> talents, Element a, Element b, GameData data)
    {
        ArgumentNullException.ThrowIfNull(talents);
        ArgumentNullException.ThrowIfNull(data);
        if (a == b || !IsBase(a) || !IsBase(b))
            return null;
        return talents.SelectMany(t => t.Rules).OfType<ReactionRule>().Select(r => data.Reactions[r.Reaction]).FirstOrDefault(r => r.Joins(a, b));
    }
}

/// <summary>A talent's part of a hit's bonus, in percent (sprint 150, <see cref="Talents.Shares"/>).</summary>
public sealed record TalentShare(string Talent, int Percent);

/// <summary>A talent that lights up with the turn (sprint 150, <see cref="Talents.Lights"/>): lit or not, and what it gives now.</summary>
public sealed record TalentLight(string Talent, bool Lit, int Percent);

/// <summary>A node of the trunk (sprint 146, D82): lit from <see cref="Level"/>, that many <see cref="Percent"/> more of <see cref="Gain"/>.</summary>
public sealed record TrunkNode(int Level, TrunkGain Gain, int Percent);

/// <summary>What a node of the trunk adds: damage, resistance or hit points.</summary>
[JsonConverter(typeof(JsonStringEnumConverter<TrunkGain>))]
public enum TrunkGain
{
    Damage,
    Resistance,
    Hp,
}
