using static Rpg.Core.Tests.Arena;

namespace Rpg.Core.Tests;

/// <summary>
/// The AI of the party spells (sprint 223, D102, docs/sorts-de-groupe.md): what a hero counts an ally's shield, a glyph that welcomes its allies, a Feint,
/// the Recall and a leap out of the blows for. No fight is played out here (D96): each worth is read on a fight set up by hand.
/// </summary>
public class PartySpellsAiTests
{
    private static Fight Duo(Hero hero)
    {
        var fight = new Fight(Real, "training", 1, hero, allies: [new Hero("Brume", "male-b", "warrior", Level: hero.Level)]);
        while (fight.Current != fight.HeroFighter)
            fight.Apply(new EndTurnAction());
        return fight;
    }

    private static Hero Of(string cls, int level, string spec, string? fork = null, params string[] deck) =>
        new("Ondine", "female-c", cls, Level: level, Stats: new Characteristics(Earth: 90), Deck: deck.Length == 0 ? null : deck) { Spec = spec, Fork = fork };

    private static Fighter Foe(Fight f) => f.Fighters.First(x => x.IsAlive && x.Team != f.HeroFighter!.Team);

    /// <summary>Every enemy far away but <paramref name="foe"/>, set on <paramref name="cell"/>.</summary>
    private static void Only(Fight f, Fighter foe, Cell cell)
    {
        int k = 0;
        foreach (Fighter e in f.Fighters.Where(x => x.Team != f.HeroFighter!.Team && x != foe))
            e.Cell = new Cell(200 + (10 * k++), 200);
        foe.Cell = cell;
    }

    [Fact]
    public void AnAllysShield_IsWorthWhatTheCastersRulesMakeIt_AsTheRulesGiveIt()
    {
        // The Grove Keeper's shields are worth 25 % more on an ally (ShieldRule): the rules gave it, the AI did not count it.
        Fight f = Duo(Of("druid", 60, "nature-warden", "grove-keeper"));
        Fighter me = f.HeroFighter!, ally = f.HeroFighters[1], foe = Foe(f);
        Assert.Equal(25, Fight.ShieldOn(me, ally, 20));
        Assert.Equal(20, Fight.ShieldOn(me, me, 20));
        Assert.Equal(20, Fight.ShieldOn(me, foe, 20));
        Only(f, foe, ally.Cell + new Cell(1, 0));
        double incoming = Ai.Incoming(f, ally);
        Assert.True(incoming > 0);
        Assert.Equal(Math.Min(31, Math.Max(0, (incoming * 2) - ally.Shield)), Ai.Value(f, me, Real.Spells["path-shield-of-faith"], me.Cell, ally.Cell), 6);
    }

    [Fact]
    public void AGlyphThatWelcomesAllies_IsWorthItsBoon_AsTheSameStatusCastOnThem()
    {
        // The Hedge's +20 % resistance for a turn, on a Warrior one enemy can strike: a fifth of that blow; until then 1, whatever came.
        Fight f = Duo(Of("druid", 35, "nature-warden"));
        Fighter me = f.HeroFighter!, ally = f.HeroFighters[1], foe = Foe(f);
        Only(f, foe, ally.Cell + new Cell(2, 0));
        me.Cell = ally.Cell + new Cell(0, 3);
        double incoming = Ai.Incoming(f, ally);
        Assert.True(incoming > 5);
        Assert.Equal(0.2 * incoming, Ai.Value(f, me, Real.Spells["path-hedge"], me.Cell, ally.Cell), 6);
        // The Rumbling Ground's +10 % damage: half of a tenth of the ally's round, as the Shaman's same bonus cast on it.
        Assert.Equal(Ai.AllyBoon(f, me, ally, new StatusEffect(Affects.Allies, Stat.Damage, 10, 1))!.Value, Ai.Value(f, me, Real.Spells["path-rumbling-ground"], me.Cell, ally.Cell));
        Assert.Null(Ai.AllyBoon(f, me, ally, new StatusEffect(Affects.Allies, Stat.Mp, 1, 1)));
    }

    [Fact]
    public void AFeint_IsWorthTheMovementItWouldTake_FromAnEnemyWhoCanComeStrikeInMelee()
    {
        Fight f = Duo(Of("rogue", 50, "scoundrel", "swashbuckler"));
        Fighter me = f.HeroFighter!, foe = Foe(f);
        Spell feint = Real.Spells["path-feint"];
        Assert.Contains(foe.Spells, s => s.DamageMax > 0 && s.MinRange <= 1);
        Only(f, foe, me.Cell + new Cell(1, 0));
        Assert.True(Ai.Value(f, me, feint, me.Cell, me.Cell) >= 4);
        Assert.Equal(Ai.FeintWorth(f, me, 2), Ai.Value(f, me, feint, me.Cell, me.Cell));
        Only(f, foe, me.Cell + new Cell(100, 0));
        Assert.Equal(0, Ai.Value(f, me, feint, me.Cell, me.Cell));
    }

    [Fact]
    public void TheRecall_IsWorthTheBlowsItsCasterEscapes_AndIsCastThen()
    {
        // The Tamer trades places with its Familiar: until then only to rescue the Familiar, hurt, which it seldom is.
        Fight f = Duo(Of("summoner", 60, "familiarist", "tamer", "path-familiar", "path-recall"));
        Fighter me = f.HeroFighter!, foe = Foe(f);
        Assert.Equal(ActionError.None, f.Apply(new CastAction("path-familiar", me.Cell.Neighbours().First(f.IsFree))));
        Fighter familiar = f.Fighters[^1];
        Spell recall = me.Spells.Single(s => s.Id == "path-recall");
        familiar.Cell = me.Cell + new Cell(0, 5);
        Only(f, foe, me.Cell + new Cell(1, 0));
        double escaped = Ai.Escape(f, me, familiar);
        Assert.True(escaped > 0);
        Assert.Equal(Ai.Incoming(f, me) - Ai.IncomingAt(f, me, familiar.Cell), escaped, 6);
        Assert.Equal(escaped, Ai.Value(f, me, recall, me.Cell, familiar.Cell), 6);
        Assert.Contains(familiar.Cell, Ai.BestCast(f, me, me.Cell, recall) is var (_, cell, _) ? [cell] : Array.Empty<Cell>());
        // Nothing to escape: no Recall.
        Only(f, foe, me.Cell + new Cell(100, 0));
        Assert.Null(Ai.BestCast(f, me, me.Cell, recall));
    }

    [Fact]
    public void AHeroWhoCanStrikeNoMore_VaultsOutOfTheBlows()
    {
        // The Scoundrel's Vault: a leap that strikes nothing was taken only to strike from where it lands, so never.
        Fight f = Duo(Of("rogue", 35, "scoundrel", null, "path-vault"));
        Fighter me = f.HeroFighter!, foe = Foe(f);
        Only(f, foe, me.Cell + new Cell(1, 0));
        me.Mp = 0;
        Assert.True(Ai.Incoming(f, me) > 0);
        FightAction action = Ai.Decide(f);
        CastAction vault = Assert.IsType<CastAction>(action);
        Assert.Equal("path-vault", vault.Spell);
        Assert.True(Ai.IncomingAt(f, me, vault.Target) < Ai.Incoming(f, me));
        Assert.Equal(ActionError.None, f.Apply(action));
    }
}
