using System.Security.Cryptography;
using System.Text;
using static Rpg.Core.Tests.Arena;

namespace Rpg.Core.Tests;

/// <summary>
/// The heroes' AI plays as a team when at least two heroes fight (sprint 122, T90): the monsters that heal first,
/// the same monster for everyone, and a monster that would alert the others last; a hero alone plays as before.
/// </summary>
public class TeamAiTests
{
    private static readonly Spell Mend = new("mend", Text, 2, 0, 6, false, false, 0, 0, 9, Element.Neutral, null, [new HealEffect(Affects.Allies, 10, 10)]);

    private static readonly PassiveSpec Alarm = new("alarm", Text, Text, Trigger.Hit,
        [new StatusEffect(Affects.Allies, Stat.Damage, 20, 1)], new Area(AreaShape.Circle, 3));

    // A hero that cannot walk and strikes once (3 action points, 10 damage).
    private static readonly HeroClass Hunter = new("hunter", Text, Text, 50, 3, 0, 999, ["strike"]);

    private static GameData Data(string[] rows, params FighterSpec[] monsters) =>
        new([.. Spells, Mend], [new MapSpec("m", Text, rows)], [new Scenario("s", Text, "m", [Spec(0, spells: ["strike"]), .. monsters])],
            [Hunter], passives: [Alarm]);

    private static Hero Of(string name) => new(name, "female-c", "hunter");

    [Fact]
    public void AHero_BetweenANurseAndAWorkerOfTheSameWorth_StrikesTheNurse_WithAnAllyOnly()
    {
        // The worker (id 1) at (0,0), the nurse (id 2) at (2,0), the hero between them; the ally starts at (0,1).
        GameData data = Data(["BAB", "A.."], Spec(1, 0, hp: 50, initiative: 1), Spec(1, 1, hp: 50, initiative: 1, spells: ["mend"]));
        var alone = new Fight(data, "s", 1, Of("Aube"));
        Assert.Equal(alone.HeroFighter, alone.Current);
        Assert.Equal(new CastAction("strike", new Cell(0, 0)), Ai.Decide(alone));
        var party = new Fight(data, "s", 1, Of("Aube"), null, null, [Of("Brume")]);
        Assert.Equal(party.HeroFighter, party.Current);
        Assert.Equal(new CastAction("strike", new Cell(2, 0)), Ai.Decide(party));
        Assert.Equal(Ai.HealerFocus, Ai.Focus(party, party.HeroFighter!, party.Fighters[2]));
    }

    [Fact]
    public void TwoHeroes_FinishTheSameMonster_RatherThanTheWeakestOne()
    {
        // The hero reaches X (60 hit points) only; the ally, at (2,0), reaches X and Y (45).
        GameData data = Data(["ABA", "..B"], Spec(1, 0, hp: 60, initiative: 1), Spec(1, 1, hp: 45, initiative: 1));
        var f = new Fight(data, "s", 1, Of("Aube"), null, null, [Of("Brume")]);
        Fighter ally = f.HeroFighters[1];
        while (f.Current != ally)
            Assert.Equal(ActionError.None, f.Apply(Ai.Decide(f)));
        Assert.Equal(50, f.Fighters[1].Hp);
        // Alone, the ally would strike the weakest (Y); beside the hero, the one the hero hit this round.
        Assert.Equal(Ai.SharedTargetFocus, Ai.Focus(f, ally, f.Fighters[1]));
        Assert.Equal(1, Ai.Focus(f, ally, f.Fighters[2]));
        Assert.Equal(new CastAction("strike", new Cell(1, 0)), Ai.Decide(f));
    }

    [Fact]
    public void AMonsterThatWouldAlertOthers_IsWorthLess_ForEachOneItWouldAlert_UnlessTheBlowKillsIt()
    {
        FighterSpec bee = Spec(1, 0, hp: 50, initiative: 1) with { Passives = ["alarm"] };
        GameData data = Data(["BAB", "A.B"], bee, Spec(1, 1, hp: 50, initiative: 1), Spec(1, 2, hp: 50, initiative: 1));
        var f = new Fight(data, "s", 1, Of("Aube"), null, null, [Of("Brume")]);
        Fighter hero = f.HeroFighter!;
        // Two bees within 3 cells of the first one: 20 % less, and nothing when the blow kills it.
        Assert.Equal(0.8, Ai.Focus(f, hero, f.Fighters[1]), 6);
        Assert.Equal(1, Ai.Focus(f, hero, f.Fighters[1], kills: true));
        Assert.Equal(1, Ai.Focus(f, hero, f.Fighters[2]));
    }

    [Fact]
    public void AloneOrForTheMonsters_NothingChanges()
    {
        GameData data = Data(["BAB", "A.."], Spec(1, 0, hp: 50, initiative: 1), Spec(1, 1, hp: 50, initiative: 1, spells: ["mend"]));
        var alone = new Fight(data, "s", 1, Of("Aube"));
        Assert.Equal(1, Ai.Focus(alone, alone.HeroFighter!, alone.Fighters[2]));
        var party = new Fight(data, "s", 1, Of("Aube"), null, null, [Of("Brume")]);
        // A monster weighs nothing by team play, even in a fight with the party.
        Assert.Equal(1, Ai.Focus(party, party.Fighters[2], party.HeroFighter!));
    }

    /// <summary>
    /// The real fights of a hero alone, played by the AI before sprint 122, as a fingerprint: training, each class,
    /// ten seeds. A change to the solo AI, or to the training's data, changes it.
    /// </summary>
    [Fact]
    public void AHeroAlone_PlaysTheSameFightsAsBeforeSprint122()
    {
        var all = new StringBuilder();
        foreach (HeroClass c in Real.Classes)
        {
            for (ulong seed = 0; seed < 10; seed++)
            {
                var f = new Fight(Real, "training", seed, Progression.Built(new Hero("Aube", "female-c", c.Id, Level: 10), Real));
                Ai.PlayOut(f);
                all.Append(Fingerprint(f)).Append('\n');
            }
        }
        Assert.Equal(SoloFingerprint, Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(all.ToString())))[..16]);
    }

    // Sprint 137: the same fights, told with the marks of the hero's hits (Marked, MarkEnded) and a status's reaction (none).
    private const string SoloFingerprint = "02BD78F479FCD1A6";
}
