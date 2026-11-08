using System.Text.Json.Serialization;

namespace Rpg.Core;

/// <summary>
/// Where a talent stands in the tree: one of the six styles (Alchemy, Melee, Distance, Terrain, Tempo, Support). Since
/// sprint 182 (D93) the elements left the tree for the Elements page: no pair, no Heart, no braid.
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter<Branch>))]
public enum Branch
{
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
[JsonDerivedType(typeof(RangeRule), "range")]
[JsonDerivedType(typeof(DistanceRule), "distance")]
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
[JsonDerivedType(typeof(LureTargetRule), "lure-target")]
[JsonDerivedType(typeof(HealRule), "heal")]
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
[JsonDerivedType(typeof(PatchRule), "patch")]
[JsonDerivedType(typeof(SummonRule), "summon-share")]
[JsonDerivedType(typeof(ShieldRule), "ally-shield")]
[JsonDerivedType(typeof(PullRule), "pull")]
[JsonDerivedType(typeof(StatusTurnsRule), "status-turns")]
[JsonDerivedType(typeof(TargetStateRule), "target-state")]
[JsonDerivedType(typeof(LeapSightRule), "leap-sight")]
[JsonDerivedType(typeof(GlyphTurnsRule), "glyph-turns")]
[JsonDerivedType(typeof(LingerRule), "linger")]
[JsonDerivedType(typeof(WaveRule), "wave")]
[JsonDerivedType(typeof(SharedMarkRule), "shared-mark")]
[JsonDerivedType(typeof(VirtuosoRule), "virtuoso")]
[JsonDerivedType(typeof(IsolatedRule), "isolated")]
[JsonDerivedType(typeof(StackRule), "stack")]
[JsonDerivedType(typeof(ShadowRule), "shadow")]
[JsonDerivedType(typeof(FuryRule), "fury")]
[JsonDerivedType(typeof(ExecuteRule), "execute")]
[JsonDerivedType(typeof(FlankRule), "flank")]
[JsonDerivedType(typeof(WardRule), "ward")]
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

/// <summary>Living, the Illusionist's (sprint 200): <see cref="Percent"/> % more on a target within <see cref="Cells"/> cells of one of the bearer's lures (Image, Double).</summary>
public sealed record LureTargetRule(int Percent, int Cells = 2) : TalentRule;

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

/// <summary>
/// Compiled: every spell reaching farther than 1 cell reaches <see cref="Change"/> cells more (or fewer), never under
/// its minimum range nor under 1.
/// </summary>
/// <param name="AtLeast">Only the spells reaching that far already (Long Sight: 4).</param>
/// <param name="Sight">Only the spells that ask for a line of sight (sprint 186, the Sightline).</param>
public sealed record RangeRule(int Change, int AtLeast = 0, bool Sight = false) : TalentRule;

/// <summary>
/// A path's (sprint 190, D95, brick C): what changes one spell, its twins too (by <see cref="Spell.RankKey"/>), the ranks following.
/// </summary>
/// <param name="MaxSummons">Sprint 191: how many of its creatures at once (Twin Wings).</param>
/// <param name="MaxGlyphs">Sprint 191, brick G: how many of its glyphs at once (Deadfall).</param>
public sealed record PatchRule(string Spell, int? Cooldown = null, int? ApCost = null, int? MaxRange = null, int? PerTurn = null, Area? Area = null, int DamagePercent = 100,
    IReadOnlyList<SpellEffect>? AddEffects = null, int? MaxSummons = null, int? MaxGlyphs = null) : TalentRule;

/// <summary>
/// A path's (sprint 191, D95, brick E): this creature, summoned by the bearer, has <see cref="HpPercent"/> % of its maximum hit points and
/// <see cref="StatsPercent"/> % of its element characteristics, and strikes in its element. The highest of these rules wins; without one, the creature's own numbers.
/// </summary>
public sealed record SummonRule(string Summon, int HpPercent, int StatsPercent) : TalentRule;

/// <summary>Living, a path's (sprint 192, D95, brick J): the bearer's shields on its allies, not on itself, are worth <see cref="Percent"/> % more.</summary>
public sealed record ShieldRule(int Percent) : TalentRule;

/// <summary>Compiled, a path's (the Cleric's, sprint 202): the bearer's heals <see cref="Percent"/> % bigger.</summary>
public sealed record HealRule(int Percent) : TalentRule;

/// <summary>Compiled, a path's (sprint 192, brick N): the spells that pull an enemy pull it <see cref="Cells"/> farther.</summary>
public sealed record PullRule(int Cells) : TalentRule;

/// <summary>Compiled, a path's (sprint 192, brick O): what the spells set on enemies among <see cref="Stats"/> lasts <see cref="Extra"/> turns more, on all but a compound's spells.</summary>
public sealed record StatusTurnsRule(int Extra, IReadOnlyList<Stat> Stats) : TalentRule;

/// <summary>
/// Living, a path's (sprint 192, brick P): <see cref="Percent"/> % more on a target with no movement point for its next turn, or under <see cref="States"/> states or more
/// when given; with <see cref="Spell"/>, on that spell alone. Past <see cref="Talents.Ceiling"/>: a path's reward for a hard condition (the Crush's +50 %).
/// </summary>
public sealed record TargetStateRule(int Percent, int States = 0, string? Spell = null) : TalentRule;

/// <summary>A path's (sprint 186, the Trail): its spells that leap ask for no line of sight.</summary>
public sealed record LeapSightRule : TalentRule;

/// <summary>A path's (sprint 186, the Ambush): the glyphs its spells lay last that many turns more.</summary>
public sealed record GlyphTurnsRule(int Extra) : TalentRule;

/// <summary>Living, the Rogue's engine (sprint 204, docs/classes-voleur.md §2): <see cref="Percent"/> % more on a target with no fighter of its side within <see cref="Cells"/> cells.</summary>
public sealed record IsolatedRule(int Cells, int Percent) : TalentRule;

/// <summary>
/// Living, the Rogue's (sprint 205, docs/classes-voleur-2.md §1): the bearer's poisons stack. A poison of an element it already left on a fighter adds
/// its value to that one's, up to <see cref="Max"/> times the new one's, and lasts the longer of the two.
/// </summary>
public sealed record StackRule(int Max) : TalentRule;

/// <summary>
/// Living, the Berserker's engine (sprint 206, docs/classes-berserker.md §1): <see cref="Ratio"/> % of the share of its hit points it misses, in percent more
/// damage, <see cref="Max"/> at most.
/// </summary>
public sealed record FuryRule(int Ratio, int Max) : TalentRule;

/// <summary>
/// Living, the Gladiator's (sprint 207, docs/classes-berserker-2.md §1): <see cref="Percent"/> % more on a target under <see cref="Below"/> % of its hit points,
/// beyond the talents' ceiling, as a target's state.
/// </summary>
public sealed record ExecuteRule(int Below, int Percent) : TalentRule;

/// <summary>
/// The Paladin's engine (sprint 208, docs/classes-paladin.md §1): a blow from next to its target shields the most wounded fighter of its side within
/// <see cref="Cells"/> cells of it, itself too, for <see cref="Percent"/> % of the damage done to enemies, for 2 turns.
/// </summary>
public sealed record WardRule(int Percent, int Cells) : TalentRule;

/// <summary>Living, the Barbarian's (sprint 207): <see cref="Percent"/> % more on a target another fighter of the bearer's side stands next to (the clan up close).</summary>
public sealed record FlankRule(int Percent) : TalentRule;

/// <summary>Living, the Shadowmage's (sprint 205): <see cref="Percent"/> % more on a hit cast while the bearer is hidden.</summary>
public sealed record ShadowRule(int Percent) : TalentRule;

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

    /// <summary>1 point, 2 for a key.</summary>
    public int Cost => Tier == Tier.Key ? 2 : 1;
}

/// <summary>
/// An elemental reaction (D80, <c>docs/talents-arbre.md</c> §6), as described in <c>data/reactions.json</c>: a hit of
/// <see cref="First"/> on a fighter marked by <see cref="Second"/>, or the other way round, sets <see cref="Statuses"/> on
/// it, named after the reaction. Since sprint 182 (D93), every hero of level 30 or more sets the six off, no talent asked.
/// </summary>
public sealed record ReactionSpec(string Id, LocalizedText Name, LocalizedText Description, Element First, Element Second, IReadOnlyList<StatusEffect> Statuses, Element? Trio = null)
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
/// <param name="TargetStill">The target has no movement point for its next turn (sprint 192); <paramref name="TargetStates"/>, the states on it; <paramref name="Spell"/>, the spell's rank key.</param>
/// <param name="LureCells">Sprint 200: the cells between the target and the nearest of the caster's lures (<see cref="int.MaxValue"/>: none).</param>
/// <param name="AllyCells">Sprint 204: the cells between the target and the nearest fighter of its side (<see cref="int.MaxValue"/>: none).</param>
/// <param name="Hidden">Sprint 205: the caster is hidden as it casts (the Shadowmage's).</param>
/// <param name="Missing">Sprint 206: the share of its hit points the caster misses, in percent (the Berserker's fury).</param>
/// <param name="TargetHp">Sprint 207: the share of its hit points the target has left, in percent (the Gladiator's execution).</param>
/// <param name="Flanked">Sprint 207: another fighter of the caster's side stands next to the target (the Barbarian's).</param>
public sealed record HitContext(bool Walked = false, int CellsSinceSpell = 0, int DamageCasts = 0, int ApCost = 0, bool Backed = false, bool InMyGlyph = false,
    bool Triggers = false, bool OwnMark = false, bool Zone = false, bool TargetStill = false, int TargetStates = 0, string? Spell = null, int LureCells = int.MaxValue,
    int AllyCells = int.MaxValue, bool Hidden = false, int Missing = 0, int TargetHp = 100, bool Flanked = false);

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

/// <summary>A hero's grimoire (sprint 138): the spells it may fight with. Since sprint 182 (D93), a compound adds its twins and replaces nothing.</summary>
public sealed record Grimoire(IReadOnlyList<string> Spells);

/// <summary>The tree's rules (D80): its points by level, what a choice of talents may hold, the bonuses it gives.</summary>
public static class Talents
{
    /// <summary>The tree opens at this level: nothing changes before it (chapter 1 and the balance of the fields).</summary>
    public const int Opens = 30;

    /// <summary>
    /// The version of the tree (sprint 146, D82): 1 the tree of sprints 137 to 144, 2 since the trunk and the arbitrated
    /// Lightning. Talents taken in an older one are given back once, for free, with a word at the connection (I5 a).
    /// Version 4 (sprint 182, D93): the elements left the tree; a hero of version 3 keeps its styles that still hold.
    /// </summary>
    public const int Version = 5;

    /// <summary>The ceiling of the tree's conditional bonuses on one hit, in percent (§4.4).</summary>
    public const int Ceiling = 35;

    /// <summary>
    /// 1 point at level 30, then one every 3 levels (sprint 182, D93): 6 at 45, 11 at 60, 24 at 100; the point a door cost
    /// is gone with the doors (before, 2 at 30, sprint 146, D82, G1 b).
    /// </summary>
    public static int Points(int level) => level < Opens ? 0 : 1 + (level - Opens) / 3;

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
    /// a second floor after 2 points in the branch's first, a key after 3 below it; one key per branch; nothing it
    /// excludes, everything it requires. Since sprint 146 (D82): a talent with a parent asks no points below it, one of
    /// its parents among several, its level.
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
            int asked = t.HasParent ? 0 : t.Tier == Tier.Key ? 3 : t.Tier == Tier.Second ? 2 : 0;
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
        return taken.Where(t => t.Tier == Tier.Key).GroupBy(t => t.Branch).Any(g => g.Count() > 1) ? "One key per branch." : null;
    }

    /// <summary>Talents are taken back for free up to this level; above, it costs <see cref="ChangeCost"/> Éclats, in a village (§4.2).</summary>
    public const int FreeChangeUpTo = 40;

    /// <summary>What giving back saved talents costs at a level, in Éclats: nothing up to 40, then 10 × the level.</summary>
    public static long ChangeCost(int level) => level <= FreeChangeUpTo ? 0 : 10L * level;

    /// <summary>Whether new talents give back any saved one (taking more is free).</summary>
    public static bool GivesBack(IReadOnlyList<string>? before, IReadOnlyList<string>? after) =>
        (before ?? []).Except(after ?? [], StringComparer.Ordinal).Any();

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
    /// The spells a hero may put in its deck (sprint 138): its class's that its level has unlocked, in the class's order;
    /// since sprint 182 (D93), then for each compound it created (<see cref="Hero.Compounds"/>, in that order) the twins of
    /// those of its parents' elements, in the class's order, which take the place of their parents' spells (sprint 187, D94); then its path's (sprint 186); then the spells of the tree.
    /// </summary>
    public static Grimoire GrimoireOf(Hero hero, GameData data)
    {
        ArgumentNullException.ThrowIfNull(hero);
        ArgumentNullException.ThrowIfNull(data);
        if (data.Class(hero.Class) is not HeroClass c)
            return new Grimoire([]);
        List<string> spells = [.. c.Spells.Where(id => data.Spells[id].Level <= hero.Level)];
        string[] unlocked = [.. spells];
        foreach (Element compound in (hero.Compounds ?? []).Where(e => Elements.IsCompound(e) && hero.Level >= Elements.CreatedFrom(e)).Distinct())
        {
            IReadOnlyList<Element> parents = Elements.Parents(compound);
            foreach (string id in unlocked.Where(id => parents.Contains(data.Spells[id].Element)))
            {
                if (data.Spells.Values.FirstOrDefault(s => s.Twin == id && s.Element == compound) is Spell twin && !spells.Contains(twin.Id))
                    spells.Add(twin.Id);
            }
        }
        // A compound consumes its parents (sprint 187, D94): their spells leave the grimoire.
        IReadOnlyList<Element> consumed = ConsumedBy(hero, data);
        spells.RemoveAll(id => unlocked.Contains(id) && consumed.Contains(data.Spells[id].Element));
        // Its path's (sprint 186): after the compounds', before the tree's.
        spells.AddRange(Specs.SpellsOf(hero, data).Where(id => !spells.Contains(id)));
        // The spells of the tree (sprint 142): after the class's.
        foreach (GrantRule g in (hero.Talents ?? []).Where(data.Talents.ContainsKey).SelectMany(id => data.Talents[id].Rules).OfType<GrantRule>().Where(g => !spells.Contains(g.Spell)))
            spells.Add(g.Spell);
        return new Grimoire(spells);
    }

    /// <summary>The base elements the compounds this hero has at its level consume (sprint 187, D94).</summary>
    public static IReadOnlyList<Element> ConsumedBy(Hero hero, GameData data)
    {
        ArgumentNullException.ThrowIfNull(hero);
        ArgumentNullException.ThrowIfNull(data);
        return Elements.ConsumedBy((hero.Compounds ?? []).Where(e => Elements.IsCompound(e) && hero.Level >= Elements.CreatedFrom(e)));
    }

    /// <summary>
    /// A deck fitted to this hero's grimoire (sprint 187, D94), in its order: a spell it has stays, a spell its compounds took
    /// gives its place to its twin, a twin of a compound undone to its origin; what has no heir leaves.
    /// </summary>
    public static IReadOnlyList<string> FitDeck(IEnumerable<string> deck, Hero hero, GameData data)
    {
        ArgumentNullException.ThrowIfNull(deck);
        IReadOnlyList<string> grimoire = GrimoireOf(hero, data).Spells;
        string? Heir(string id) => grimoire.Contains(id) ? id
            : data.Spells.TryGetValue(id, out Spell? s) && grimoire.FirstOrDefault(g => data.Spells[g].RankKey == s.RankKey) is string other ? other
            : null;
        return [.. deck.Select(Heir).OfType<string>().Distinct(StringComparer.Ordinal)];
    }

    /// <summary>The resistance in percent the talents give a fighter next to an obstacle (Backed, sprint 142); 0 elsewhere.</summary>
    public static int Guard(IReadOnlyList<TalentSpec> talents, bool backed) => backed ? talents.SelectMany(t => t.Rules).OfType<BackedRule>().Sum(r => r.Resistance) : 0;

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
        int change = rules.OfType<RangeRule>().Where(r => spell.MaxRange >= r.AtLeast && (!r.Sight || spell.LineOfSight)).Sum(r => r.Change)
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
        // The paths' (sprint 186).
        if (rules.OfType<LeapSightRule>().Any() && s.AllEffects.Any(e => e is LeapEffect))
            s = s with { LineOfSight = false };
        // The paths' (sprint 192, bricks N and O).
        int pulled = rules.OfType<PullRule>().Sum(r => r.Cells);
        if (pulled > 0)
            s = s with { Effects = s.Effects?.Select(e => e is PullEffect { Affects: Affects.Enemies } p ? p with { Cells = p.Cells + pulled } : e).ToList() };
        if (s.Twin is null)
        {
            foreach (StatusTurnsRule longer in rules.OfType<StatusTurnsRule>())
                // The enemies' statuses; and Stealth on oneself (the Night Stalker's, sprint 205), no other boon.
                s = s with { Effects = s.Effects?.Select(e => e is StatusEffect st && (st.Affects == Affects.Enemies || st is { Affects: Affects.Caster, Stat: Stat.Hidden }) && longer.Stats.Contains(st.Stat) ? st with { Turns = st.Turns + longer.Extra } : e).ToList() };
        }
        // A compound's glyph, Ash's signature, does not last longer (sprint 191, §v4.6).
        int extraTurns = rules.OfType<GlyphTurnsRule>().Sum(r => r.Extra);
        if (extraTurns > 0 && s.Twin is null && s.AllEffects.Any(e => e is GlyphEffect))
            s = s with { Effects = [.. s.AllEffects.Select(e => e is GlyphEffect g ? g with { Turns = g.Turns + extraTurns } : e)] };
        foreach (PatchRule p in rules.OfType<PatchRule>().Where(p => p.Spell == spell.RankKey))
        {
            s = s with
            {
                Cooldown = p.Cooldown ?? s.Cooldown,
                ApCost = p.ApCost ?? s.ApCost,
                MaxRange = p.MaxRange ?? s.MaxRange,
                PerTurn = p.PerTurn ?? s.PerTurn,
                Area = p.Area ?? s.Area,
                DamageMin = s.DamageMin * p.DamagePercent / 100,
                DamageMax = s.DamageMax * p.DamagePercent / 100,
                Effects = p.AddEffects is null ? s.Effects : [.. s.AllEffects, .. p.AddEffects],
            };
            if (p.MaxSummons is int summons)
                s = s with { Effects = [.. s.AllEffects.Select(e => e is SummonEffect m ? m with { Max = summons } : e)] };
            if (p.MaxGlyphs is int glyphs)
                s = s with { Effects = [.. s.AllEffects.Select(e => e is GlyphEffect g ? g with { Max = glyphs } : e)] };
        }
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
            + rules.OfType<BonusRule>().Sum(r => r.Percent)
            + rules.OfType<StillRule>().Sum(r => c.Walked ? r.Moved : r.Still)
            + rules.OfType<DanceRule>().Sum(r => Math.Min(r.Max, r.PerCell * c.CellsSinceSpell))
            + rules.OfType<ApCostRule>().Where(r => c.ApCost >= r.MinAp && c.ApCost <= r.MaxAp).Sum(r => r.Percent)
            + rules.OfType<FirstCastRule>().Sum(r => c.DamageCasts == 0 ? r.First : r.Others)
            + rules.OfType<BackedRule>().Sum(r => c.Backed ? r.Damage : r.Otherwise)
            + rules.OfType<GlyphTargetRule>().Sum(r => c.InMyGlyph ? r.Inside : r.Outside)
            + rules.OfType<LureTargetRule>().Sum(r => c.LureCells <= r.Cells ? r.Percent : 0)
            + rules.OfType<IsolatedRule>().Sum(r => c.AllyCells > r.Cells ? r.Percent : 0)
            + rules.OfType<ShadowRule>().Sum(r => c.Hidden ? r.Percent : 0)
            + rules.OfType<FlankRule>().Sum(r => c.Flanked ? r.Percent : 0)

            + (c.Triggers ? rules.OfType<LingerRule>().Sum(r => r.TriggerPercent) : 0)
            + (c.Triggers && c.OwnMark ? rules.OfType<VirtuosoRule>().Sum(r => r.Percent) : 0)
            + (c.Zone ? rules.OfType<WaveRule>().Sum(r => r.ZonePercent) : 0);
        int beyond = rules.OfType<TargetStateRule>().Where(r => (r.Spell is null || r.Spell == c.Spell) && (r.States > 0 ? c.TargetStates >= r.States : c.TargetStill)).Sum(r => r.Percent)
            // The Berserker's fury (sprint 206), its engine: beyond the talents' ceiling, as a target's state.
            + rules.OfType<FuryRule>().Sum(r => Math.Min(r.Max, c.Missing * r.Ratio / 100))
            // The Gladiator's execution (sprint 207), beyond the ceiling too.
            + rules.OfType<ExecuteRule>().Where(r => c.TargetHp < r.Below).Sum(r => r.Percent);
        return Math.Clamp(sum, -100, Ceiling) + beyond;
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

    /// <summary>The reaction between two different base elements (since sprint 182, D93, no talent asked), or null.</summary>
    public static ReactionSpec? ReactionOf(Element a, Element b, GameData data)
    {
        ArgumentNullException.ThrowIfNull(data);
        if (a == b || !IsBase(a) || !IsBase(b))
            return null;
        return data.Reactions.Values.FirstOrDefault(r => r.Trio is null && r.Joins(a, b));
    }

    /// <summary>The reaction a trio's spell sets off on the mark of one of its parents (sprint 185, D93), in place of a pair's.</summary>
    public static ReactionSpec? TrioReactionOf(Element trio, GameData data)
    {
        ArgumentNullException.ThrowIfNull(data);
        return data.Reactions.Values.FirstOrDefault(r => r.Trio == trio);
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
