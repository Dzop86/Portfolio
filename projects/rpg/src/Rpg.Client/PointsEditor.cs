using Rpg.Core;

namespace Rpg.Client;

/// <summary>The five characteristics, in the order the screen lists them.</summary>
public enum Characteristic
{
    Vitality,
    Earth,
    Fire,
    Water,
    Air,
}

/// <summary>
/// The characteristics and spells screens, without any engine: a draft of the hero's points that
/// the + and − buttons change, never beyond what the level gives (<see cref="Progression"/>), and
/// that the server receives as <see cref="Points"/> when the player saves. Points already saved
/// can be taken back too: the server only checks the totals.
/// </summary>
public sealed class PointsEditor
{
    private readonly int[] _stats = new int[5];
    private readonly Dictionary<string, int> _ranks = new(StringComparer.Ordinal);
    private readonly List<string> _deck;
    private readonly List<string> _talents;

    /// <param name="readOnly">In a fight (sprint 93): the points and the deck are shown, never changed.</param>
    public PointsEditor(Hero hero, GameData data, bool readOnly = false)
    {
        ReadOnly = readOnly;
        Hero = hero ?? throw new ArgumentNullException(nameof(hero));
        Data = data ?? throw new ArgumentNullException(nameof(data));
        Characteristics st = hero.Stats ?? Characteristics.None;
        (_stats[0], _stats[1], _stats[2], _stats[3], _stats[4]) = (st.Vitality, st.Earth, st.Fire, st.Water, st.Air);
        foreach ((string spell, int rank) in hero.RanksGiven)
            _ranks[spell] = rank;
        _deck = [.. hero.DeckOf(data)];
        _talents = [.. hero.Talents ?? []];
        _compounds = [.. hero.Compounds ?? []];
    }

    private readonly List<Element> _compounds;

    /// <summary>The compounds of the draft (sprint 182, D93), in the order they were created.</summary>
    public IReadOnlyList<Element> CompoundsCreated => _compounds;

    /// <summary>A compound can be created when the level opens it (a pair from 30, a trio from 50) and it is not yet; for free.</summary>
    public bool CanCreate(Element compound) => !ReadOnly && Elements.IsCompound(compound) && !_compounds.Contains(compound) && Hero.Level >= Elements.CreatedFrom(compound);

    /// <summary>A compound created can be undone, for free (Q10 a): its twins leave the grimoire, and the deck.</summary>
    public bool CanDissolve(Element compound) => !ReadOnly && _compounds.Contains(compound);

    /// <summary>Creates a compound: its twins join the grimoire, the deck is left as it is.</summary>
    public void Create(Element compound)
    {
        if (!CanCreate(compound))
            return;
        _compounds.Add(compound);
        FitDeck();
    }

    /// <summary>Undoes a compound: its twins leave the grimoire, and the deck (<see cref="DeckChanges"/> says which).</summary>
    public void Dissolve(Element compound)
    {
        if (!CanDissolve(compound))
            return;
        _compounds.Remove(compound);
        FitDeck();
    }

    /// <summary>The draft's compounds differ from the hero's: something to save.</summary>
    public bool CompoundsChanged => !_compounds.Order().SequenceEqual((Hero.Compounds ?? []).Order());

    /// <summary>The talents of the draft (sprint 137, D80), in the order they were taken.</summary>
    public IReadOnlyList<string> TalentsTaken => _talents;

    /// <summary>The talent points the level gives that the draft leaves.</summary>
    public int TalentPointsLeft => Talents.Points(Hero.Level) - _talents.Sum(id => Data.Talents[id].Cost);

    public bool HasTalent(TalentSpec talent) => _talents.Contains((talent ?? throw new ArgumentNullException(nameof(talent))).Id);

    /// <summary>A talent can be taken when the tree's rules allow it with those already taken (<see cref="Talents.Problem"/>).</summary>
    public bool CanTake(TalentSpec talent) => !ReadOnly && !HasTalent(talent) && Talents.Problem([.. _talents, talent.Id], Hero.Level, Data) is null;

    /// <summary>
    /// A talent can be given back, with those that need it (sprint 147, D82, I1 b: the cascade, <see cref="Losing"/>), as
    /// long as what is left holds.
    /// </summary>
    public bool CanDrop(TalentSpec talent) => !ReadOnly && HasTalent(talent) && Talents.Problem(Talents.Without(_talents, talent.Id, Data), Hero.Level, Data) is null;

    /// <summary>The talents giving <paramref name="talent"/> back would give back with it, for the window to show first.</summary>
    public IReadOnlyList<string> Losing(TalentSpec talent)
    {
        ArgumentNullException.ThrowIfNull(talent);
        List<string> left = Talents.Without(_talents, talent.Id, Data);
        return [.. _talents.Where(id => id != talent.Id && !left.Contains(id))];
    }

    public void Take(TalentSpec talent)
    {
        if (!CanTake(talent))
            return;
        _talents.Add(talent.Id);
        FitDeck();
    }

    /// <summary>
    /// What the last change of talents or compounds did to the deck (sprint 150, F7 a): the spells that left it, a spell
    /// the grimoire no longer holds (since sprint 182, D93, a twin takes no spell's place any more).
    /// </summary>
    public IReadOnlyList<string> DeckChanges { get; private set; } = [];

    public void Drop(TalentSpec talent)
    {
        if (!CanDrop(talent))
            return;
        List<string> left = Talents.Without(_talents, talent.Id, Data);
        _talents.Clear();
        _talents.AddRange(left);
        FitDeck();
    }

    /// <summary>
    /// A build loaded in the draft (sprint 151, N3 a, N4 a): the talents taken in turn while the rules allow them at the
    /// hero's level, as a player would; the deck fitted. The draft stays a draft until saved. Returns those left out.
    /// </summary>
    public IReadOnlyList<string> LoadTalents(IEnumerable<string> build)
    {
        ArgumentNullException.ThrowIfNull(build);
        if (ReadOnly)
            return [.. build];
        var taken = new List<string>();
        var left = new List<string>();
        foreach (string id in build)
        {
            if (Data.Talents.ContainsKey(id) && Talents.Problem([.. taken, id], Hero.Level, Data) is null)
                taken.Add(id);
            else
                left.Add(id);
        }
        _talents.Clear();
        _talents.AddRange(taken);
        FitDeck();
        return left;
    }

    /// <summary>The draft's talents differ from the hero's (sprint 148): something to save, or to undo.</summary>
    public bool TalentsChanged => !_talents.Order(StringComparer.Ordinal).SequenceEqual((Hero.Talents ?? []).Order(StringComparer.Ordinal), StringComparer.Ordinal);

    /// <summary>
    /// Undoes the draft's talents (sprint 148, I4 a): the hero's saved talents back, and its saved deck with them, since a
    /// change of talents may have fitted the deck to the draft's grimoire.
    /// </summary>
    public void UndoTalents()
    {
        if (ReadOnly || !TalentsChanged)
            return;
        _talents.Clear();
        _talents.AddRange(Hero.Talents ?? []);
        _deck.Clear();
        _deck.AddRange(Hero.DeckOf(Data));
    }

    /// <summary>
    /// What the draft gains and loses against the saved talents (sprint 148, N6 a), each in the draft's order or the hero's,
    /// to be lit before the save.
    /// </summary>
    public (IReadOnlyList<string> Gained, IReadOnlyList<string> Lost) TalentsDiff
    {
        get
        {
            IReadOnlyList<string> saved = Hero.Talents ?? [];
            return ([.. _talents.Where(id => !saved.Contains(id))], [.. saved.Where(id => !_talents.Contains(id))]);
        }
    }

    /// <summary>
    /// The spells only the draft's talents give (sprint 146, D82, R2): shown dotted as a preview, out of the filter and of
    /// the deck until the talents are saved.
    /// </summary>
    public IReadOnlyList<Spell> Previewed
    {
        get
        {
            HashSet<string> saved = [.. Talents.GrimoireOf(Hero, Data).Spells];
            return [.. Talents.GrimoireOf(Draft, Data).Spells.Where(id => !saved.Contains(id)).Select(id => Data.Spells[id])];
        }
    }

    /// <summary>
    /// The deck after a change of talents or compounds (sprint 138): its spells once, without those the grimoire no longer
    /// holds nor a preview's.
    /// </summary>
    private void FitDeck()
    {
        Grimoire grimoire = Talents.GrimoireOf(Draft, Data);
        HashSet<string> previewed = [.. Previewed.Select(s => s.Id)];
        string[] before = [.. _deck];
        string[] fitted = [.. _deck.Where(id => grimoire.Spells.Contains(id) && !previewed.Contains(id)).Distinct().Take(DeckLimit)];
        _deck.Clear();
        _deck.AddRange(fitted);
        DeckChanges = [.. before.Where(id => !fitted.Contains(id))];
    }

    /// <summary>Shown only (in a fight, sprint 93): nothing can change.</summary>
    public bool ReadOnly { get; }

    /// <summary>The spells of the deck, in the order of the bar (sprint 68).</summary>
    public IReadOnlyList<string> Deck => _deck;

    public bool InDeck(Spell spell) => _deck.Contains((spell ?? throw new ArgumentNullException(nameof(spell))).Id);

    /// <summary>A spell can join the deck when the level has unlocked it, it is not there yet, and there is room.</summary>
    public bool CanAddToDeck(Spell spell) => !ReadOnly && Spells.Contains(spell) && !InDeck(spell) && _deck.Count < DeckLimit;

    /// <summary>The spells the draft's deck holds at most (sprint 141: fewer with Tight Deck or Thrift).</summary>
    public int DeckLimit => Draft.DeckLimit(Data);

    public void AddToDeck(Spell spell)
    {
        if (CanAddToDeck(spell))
            _deck.Add(spell.Id);
    }

    public void RemoveFromDeck(Spell spell)
    {
        ArgumentNullException.ThrowIfNull(spell);
        if (!ReadOnly)
            _deck.Remove(spell.Id);
    }

    /// <summary>
    /// A spell dropped on place <paramref name="index"/> of the deck (sprint 82, D66): moved there if it is
    /// in the deck already, else put there if there is room; places after the last spell mean the end.
    /// </summary>
    public bool PlaceInDeck(string spellId, int index)
    {
        if (ReadOnly || !Data.Spells.TryGetValue(spellId ?? "", out Spell? spell) || !Spells.Contains(spell))
            return false;
        bool moving = _deck.Remove(spellId!);
        if (!moving && _deck.Count >= DeckLimit)
            return false;
        _deck.Insert(Math.Clamp(index, 0, _deck.Count), spellId!);
        return true;
    }

    /// <summary>The spell at a place of the deck taken out (dragged off the bar).</summary>
    public void RemoveFromDeckAt(int index)
    {
        if (!ReadOnly && index >= 0 && index < _deck.Count)
            _deck.RemoveAt(index);
    }

    /// <summary>An empty deck (D66: a choice like any other).</summary>
    public void ClearDeck()
    {
        if (!ReadOnly)
            _deck.Clear();
    }

    /// <summary>
    /// The deck of a code (<see cref="DeckCode"/>): the class's spells this hero has unlocked, in order, up to
    /// twelve; the others are left out. Null if the code is not one, or of another class; else how many were left out.
    /// </summary>
    public int? UseCode(string code)
    {
        if (ReadOnly || DeckCode.Decode(code, Data) is not (string cls, IReadOnlyList<string> spells) || cls != Hero.Class)
            return null;
        HashSet<string> unlocked = [.. Spells.Select(s => s.Id)];
        string[] kept = [.. spells.Where(unlocked.Contains).Distinct().Take(DeckLimit)];
        _deck.Clear();
        _deck.AddRange(kept);
        return spells.Count - kept.Length;
    }

    /// <summary>This deck's code, to give to someone else.</summary>
    public string Code => DeckCode.Encode(Hero.Class ?? "", _deck, Data);

    /// <summary>The hero as saved.</summary>
    public Hero Hero { get; }

    public GameData Data { get; }

    public int this[Characteristic stat] => _stats[(int)stat];

    public int CharacteristicPointsLeft => Progression.CharacteristicPoints(Hero.Level) - _stats.Sum();

    public int SpellPointsLeft => Progression.SpellPoints(Hero.Level) - _ranks.Values.Sum(Progression.RankCost);

    /// <summary>The spells the draft may fight with (sprint 138, <see cref="Talents.GrimoireOf"/>): the class's the level has unlocked, in the class's order, then the compounds'.</summary>
    public IReadOnlyList<Spell> Spells
    {
        get
        {
            HashSet<string> previewed = [.. Previewed.Select(s => s.Id)];
            return [.. Talents.GrimoireOf(Draft, Data).Spells.Where(id => !previewed.Contains(id)).Select(id => Data.Spells[id])];
        }
    }

    /// <summary>A spell's rank; a compound's twin has its class spell's (sprint 138).</summary>
    public int Rank(Spell spell) => _ranks.GetValueOrDefault((spell ?? throw new ArgumentNullException(nameof(spell))).RankKey, 1);

    /// <summary>The spell points the next rank of this spell costs; 0 at its last rank.</summary>
    public int NextRankCost(Spell spell) => Rank(spell) >= spell.MaxRank ? 0 : Progression.RankCost(Rank(spell) + 1) - Progression.RankCost(Rank(spell));

    public bool CanAdd(Characteristic stat, int amount = 1) => !ReadOnly && amount > 0 && amount <= CharacteristicPointsLeft;

    public bool CanRemove(Characteristic stat, int amount = 1) => !ReadOnly && amount > 0 && _stats[(int)stat] >= amount;

    public bool CanRaise(Spell spell) => !ReadOnly && Spells.Contains(spell) && NextRankCost(spell) is > 0 and var cost && cost <= SpellPointsLeft;

    public bool CanLower(Spell spell) => !ReadOnly && Rank(spell) > 1;

    /// <summary>The most a characteristic can hold now: what it has and every point left.</summary>
    public int Max(Characteristic stat) => _stats[(int)stat] + CharacteristicPointsLeft;

    /// <summary>Puts a characteristic at a value, kept between 0 and <see cref="Max"/> (the typed value of the screen).</summary>
    public void Set(Characteristic stat, int value)
    {
        if (!ReadOnly)
            _stats[(int)stat] = Math.Clamp(value, 0, Max(stat));
    }

    /// <summary>Takes back every characteristic point, to spend them again (D62); the server only checks totals.</summary>
    public void ResetCharacteristics()
    {
        if (!ReadOnly)
            Array.Clear(_stats);
    }

    /// <summary>Adds points to a characteristic (as many as asked and left).</summary>
    public void Add(Characteristic stat, int amount = 1)
    {
        if (CanAdd(stat, Math.Min(amount, CharacteristicPointsLeft)))
            _stats[(int)stat] += Math.Min(amount, CharacteristicPointsLeft);
    }

    public void Remove(Characteristic stat, int amount = 1)
    {
        if (CanRemove(stat, Math.Min(amount, _stats[(int)stat])))
            _stats[(int)stat] -= Math.Min(amount, _stats[(int)stat]);
    }

    public void Raise(Spell spell)
    {
        if (CanRaise(spell))
            _ranks[spell.RankKey] = Rank(spell) + 1;
    }

    public void Lower(Spell spell)
    {
        if (!CanLower(spell))
            return;
        if (Rank(spell) == 2)
            _ranks.Remove(spell.RankKey);
        else
            _ranks[spell.RankKey] = Rank(spell) - 1;
    }

    /// <summary>The draft as the hero would have it.</summary>
    public Hero Draft => Hero with
    {
        Stats = new Characteristics(_stats[0], _stats[1], _stats[2], _stats[3], _stats[4]),
        Ranks = new Dictionary<string, int>(_ranks, StringComparer.Ordinal),
        Deck = [.. _deck],
        Talents = _talents.Count == 0 ? null : [.. _talents],
        Compounds = _compounds.Count == 0 ? null : [.. _compounds],
    };

    /// <summary>Whether the draft differs from what is saved (a deck left as it comes is no change).</summary>
    public bool Changed => Draft != Hero with { Deck = Hero.DeckOf(Data) };

    /// <summary>What saving this draft costs in Éclats (sprint 97): taking back points, from level 11; giving back talents, from level 41 (sprint 140).</summary>
    public long ResetCost => (Shop.TakesBack(Hero.Stats ?? Characteristics.None, Draft.Stats!) ? Shop.ResetCost(Hero.Level) : 0)
        + (Talents.GivesBack(Hero.Talents, _talents) ? Talents.ChangeCost(Hero.Level) : 0);

    /// <summary>What the server receives.</summary>
    public Points ToPoints() => new(Draft.Stats, Draft.Ranks, Draft.Deck, [.. _talents], [.. _compounds]);
}
