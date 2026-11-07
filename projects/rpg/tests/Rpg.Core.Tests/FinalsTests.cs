using static Rpg.Core.Tests.Arena;

namespace Rpg.Core.Tests;

/// <summary>The final classes, the fourth rank of a path (sprint 194, D96, docs/classes-conception.md §2.2): two by lineage, at level 100, a signature without a spell.</summary>
public class FinalsTests
{
    private static readonly LocalizedText Text = new("Texte", "Text");

    private static SpecFinal Final(string id, string? title = null, string? spell = null) =>
        new(id, id == "one" ? FinalTemplate.Kill : FinalTemplate.Extends, new SpecTier(100, new LocalizedText(title ?? id, title ?? id), Text, Spells: spell is null ? null : [spell]));

    /// <summary>The real data, the Palisade's first lineage ending in the given final classes, its evolution at 100 gone.</summary>
    private static GameData With(params SpecFinal[] finals)
    {
        Specialisation path = Real.Specs["palisade"];
        SpecFork first = path.Forks[0];
        Specialisation changed = path with { Forks = [first with { Tiers = [.. first.Tiers.Where(t => t.Level == Specs.Forks)], Finals = finals }, path.Forks[1]] };
        return new GameData(Real.Spells.Values, [new MapSpec("m", Text, ["AB"])], [new Scenario("s", Text, "m", [Spec(0), Spec(1)])], Real.Classes,
            summons: Real.Summons.Values, passives: Real.Passives.Values, talents: Real.Talents.Values, reactions: Real.Reactions.Values,
            specs: [.. Real.Specs.Values.Where(s => s.Id != path.Id), changed]);
    }

    private static string Lineage => Real.Specs["palisade"].Forks[0].Id;

    private static Hero Guard(int level, string? final) =>
        new("Ondine", "female-a", "warrior", Level: level, Spec: "palisade", Fork: Lineage, Final: final);

    [Fact]
    public void AFinalClass_GivesItsTierAndTitle_AtLevel100()
    {
        GameData data = With(Final("one", "Le Premier"), Final("two"));
        Hero hero = Guard(100, "one");
        Assert.Equal("one", Specs.FinalOf(hero, data)!.Id);
        Assert.Equal("Le Premier", Specs.TitleOf(hero, data)!.Fr);
        Assert.Equal(3, Specs.TiersOf(hero, data).Count);
    }

    [Fact]
    public void WithoutAChoice_ALineageWithFinals_StopsAtItsEvolutionAt50()
    {
        GameData data = With(Final("one"), Final("two"));
        Assert.Null(Specs.FinalOf(Guard(100, null), data));
        Assert.Equal([30, 50], Specs.TiersOf(Guard(100, null), data).Select(t => t.Level));
    }

    [Fact]
    public void BelowLevel100_AFinalClass_GivesNothing_AndTheHeroIsRefused()
    {
        GameData data = With(Final("one"), Final("two"));
        Hero hero = Guard(99, "one");
        Assert.Null(Specs.FinalOf(hero, data));
        Assert.Equal(2, Specs.TiersOf(hero, data).Count);
        Assert.Contains("level 100", hero.Problem(data), StringComparison.Ordinal);
    }

    [Fact]
    public void AnUnknownFinalClass_IsRefused()
    {
        GameData data = With(Final("one"), Final("two"));
        Assert.Contains("is not a final class", Guard(100, "nowhere").Problem(data), StringComparison.Ordinal);
    }

    [Fact]
    public void AFinalClass_NeedsItsLineage_AndItsBranch()
    {
        GameData data = With(Final("one"), Final("two"));
        Assert.Contains("needs its lineage", (Guard(100, "one") with { Fork = null }).Problem(data), StringComparison.Ordinal);
        Assert.Contains("needs its branch", (Guard(100, "one") with { Spec = null, Fork = null }).Problem(data), StringComparison.Ordinal);
    }

    [Fact]
    public void AFinalClass_OfAnotherLineage_IsRefused()
    {
        GameData data = With(Final("one"), Final("two"));
        Hero other = Guard(100, "one") with { Fork = Real.Specs["palisade"].Forks[1].Id };
        Assert.Contains("is not a final class", other.Problem(data), StringComparison.Ordinal);
    }

    [Fact]
    public void AValidFinalClass_LeavesNoProblemOfItsOwn()
    {
        GameData data = With(Final("one"), Final("two"));
        Hero hero = Progression.Built(new Hero("Ondine", "female-a", "warrior", Level: 100), data) with { Spec = "palisade", Fork = Lineage, Final = "one", Deck = null };
        Assert.Null(hero.Problem(data));
    }

    [Fact]
    public void TheFinalClass_MakesAnotherHero()
    {
        Assert.NotEqual(Guard(100, "one"), Guard(100, "two"));
        Assert.Equal(Guard(100, "one"), Guard(100, "one"));
    }

    [Fact]
    public void TheData_WantsTwoDifferentFinals_OfLevel100_WithoutASpell()
    {
        Assert.NotNull(With(Final("one"), Final("two")));
        Assert.Throws<InvalidDataException>(() => With(Final("one")));
        Assert.Throws<InvalidDataException>(() => With(Final("one"), Final("one")));
        Assert.Throws<InvalidDataException>(() => With(Final("one"), Final("two", spell: "heavy-shot")));
        Assert.Throws<InvalidDataException>(() => With(Final("one"), Final("two") with { Tier = new SpecTier(50, Text, Text) }));
    }

    [Fact]
    public void TwoSisters_OfTheSameTemplate_AreRefused() =>
        Assert.Throws<InvalidDataException>(() => With(Final("one"), Final("two") with { Template = FinalTemplate.Kill }));

    [Fact]
    public void AFinalClass_ReadsItsTemplateFromTheData()
    {
        string json = """{ "id": "x", "template": "StayOrMove", "tier": { "level": 100, "title": { "fr": "T", "en": "T" }, "description": { "fr": "D", "en": "D" } } }""";
        Assert.Equal(FinalTemplate.StayOrMove, System.Text.Json.JsonSerializer.Deserialize<SpecFinal>(json, GameData.Json)!.Template);
    }

    [Fact]
    public void AnotherFinalClass_Costs5TimesTheLevel_TheFirstNothing()
    {
        Assert.Equal(0, Specs.ChangeCost(Guard(100, null), "palisade", Lineage, "one"));
        Assert.Equal(0, Specs.ChangeCost(Guard(100, "one"), "palisade", Lineage, "one"));
        Assert.Equal(500, Specs.ChangeCost(Guard(100, "one"), "palisade", Lineage, "two"));
    }

    [Fact]
    public void AnotherLineageOrBranch_CostsItsOwnPrice_WhateverTheFinalClass()
    {
        string other = Real.Specs["palisade"].Forks[1].Id;
        Assert.Equal(1000, Specs.ChangeCost(Guard(100, "one"), "palisade", other, "two"));
        Assert.Equal(3000, Specs.ChangeCost(Guard(100, "one"), "breach", Real.Specs["breach"].Forks[0].Id));
    }

    [Fact]
    public void AFinalsPassive_IsCheckedLikeAnyTiers()
    {
        SpecFinal bad = new("two", FinalTemplate.Extends, new SpecTier(100, Text, Text, Passives: ["nowhere"]));
        Assert.Throws<InvalidDataException>(() => With(Final("one"), bad));
    }

    [Fact]
    public void EveryLineage_HasItsTwoFinals_SinceTheMage()
    {
        // The Warrior (sprint 197), the Archer (sprint 198), the Mage (sprint 200): no lineage keeps a waiting evolution.
        Assert.All(Real.Specs.Values.SelectMany(s => s.Forks), f => Assert.Equal(2, f.Finals!.Count));
        Assert.All(Real.Specs.Values.SelectMany(s => s.Forks), f => Assert.Equal([Specs.Forks], f.Tiers.Select(t => t.Level)));
        Hero hero = new("Ondine", "female-a", "mage", Level: 100, Spec: "arcanist", Fork: "scholar", Final: "archmage");
        Assert.Equal([30, 50, 100], Specs.TiersOf(hero, Real).Select(t => t.Level));
    }

    // ---- The Warrior's twelve final classes (sprint 197, docs/classes-guerrier.md) ----

    [Theory]
    [InlineData("rampart", "crusader", "sentry")]
    [InlineData("bastion", "duelist", "warlord")]
    [InlineData("jailer", "blade-master", "ghost-blade")]
    [InlineData("pincer", "strategist", "general")]
    [InlineData("ram", "executioner", "destroyer")]
    [InlineData("harpooner", "arena-champion", "pugilist")]
    public void EachWarriorLineage_EndsInItsTakenBackFinal_ThenANewOne_OfAnotherTemplate(string lineage, string takenBack, string fresh)
    {
        SpecFork fork = Real.Specs.Values.SelectMany(s => s.Forks).Single(f => f.Id == lineage);
        Assert.Equal([takenBack, fresh], fork.Finals!.Select(f => f.Id));
        Assert.NotEqual(fork.Finals![0].Template, fork.Finals[1].Template);
        Assert.Equal([Specs.Forks], fork.Tiers.Select(t => t.Level));
        Assert.All(fork.Finals, f => Assert.InRange(f.Tier.Description.Fr.Length, 20, 160));
    }

    // The evolutions at 100 the Warrior had, written out since they left the data: a hero converted to its taken-back final fights as before.
    [Fact]
    public void TheTakenBackFinals_CarryExactlyTheOldEvolutionsAt100()
    {
        SpecFinal F(string id) => Real.Specs.Values.SelectMany(s => s.Forks).SelectMany(f => f.Finals ?? []).Single(f => f.Id == id);
        SpecTier crusader = F("crusader").Tier, duelist = F("duelist").Tier, master = F("blade-master").Tier, strategist = F("strategist").Tier,
            executioner = F("executioner").Tier, arena = F("arena-champion").Tier;
        Assert.Equal(["spec-close-ranks-17", "spec-not-today"], crusader.Passives);
        Assert.Equal(["spec-close-ranks-12"], crusader.Replaces);
        Assert.Equal([new PatchRule("path-interpose", Cooldown: 1)], crusader.Rules!);
        Assert.Equal(["spec-thorns"], duelist.Passives);
        Assert.Equal([new PatchRule("path-retaliation", Area: new Area(AreaShape.Line, 1), DamagePercent: 80)], duelist.Rules!, new RuleText());
        Assert.Equal([new TargetStateRule(20, Spell: "path-crush"), new PatchRule("path-lock", Cooldown: 2)], master.Rules!, new RuleText());
        Assert.Null(master.Passives);
        Assert.Equal(["spec-no-way-out"], strategist.Passives);
        Assert.Equal([new PatchRule("path-iron-ring", Cooldown: 1)], strategist.Rules!, new RuleText());
        Assert.Equal(["spec-carried-through-3"], executioner.Passives);
        Assert.Equal(["spec-carried-through-2"], executioner.Replaces);
        Assert.Equal([new PatchRule("path-assault", Cooldown: 1)], executioner.Rules!, new RuleText());
        Assert.Equal([new PushRule(0, 0, 15), new PatchRule("path-harpoon", Cooldown: 0, PerTurn: 1)], arena.Rules!, new RuleText());
        Assert.Null(arena.Passives);
    }

    // Rules hold lists, compared by reference: their text compares them by value.
    private sealed class RuleText : IEqualityComparer<TalentRule>
    {
        public bool Equals(TalentRule? x, TalentRule? y) => System.Text.Json.JsonSerializer.Serialize(x) == System.Text.Json.JsonSerializer.Serialize(y);

        public int GetHashCode(TalentRule obj) => System.Text.Json.JsonSerializer.Serialize(obj).GetHashCode(StringComparison.Ordinal);
    }

    // Sprint 196: the page's road written offline, as the server writes it; before, another lineage kept a final class it does not know.
    [Fact]
    public void AnotherLineage_DropsTheFinalClass_TheSameKeepsIt_AndOneNamedIsTaken()
    {
        GameData data = With(Final("one"), Final("two"));
        Hero hero = Guard(100, "one");
        string sister = Real.Specs["palisade"].Forks[1].Id;
        Assert.Null(Specs.Transferred(hero, "palisade", sister, data).Final);
        Assert.Equal("one", Specs.Transferred(hero, "palisade", Lineage, data).Final);
        Assert.Equal("two", Specs.Transferred(hero, "palisade", Lineage, data, "two").Final);
        Assert.Null(Specs.Transferred(hero, "palisade", sister, data).Problem(data));
        Assert.Equal(hero.Deck, Specs.Transferred(hero, "palisade", Lineage, data, "two").Deck);
    }

    [Fact]
    public void TwoFinalClassesOfOneId_AreRefused_EvenInTwoLineages()
    {
        Specialisation path = Real.Specs["palisade"];
        GameData Made(string sisterFirst)
        {
            SpecFork[] forks = [.. path.Forks.Select((f, i) => f with { Tiers = [.. f.Tiers.Where(t => t.Level == Specs.Forks)], Finals = i == 0 ? [Final("one"), Final("two")] : [Final(sisterFirst) with { Template = FinalTemplate.Kill }, Final("four")] })];
            return new GameData(Real.Spells.Values, [new MapSpec("m", Text, ["AB"])], [new Scenario("s", Text, "m", [Spec(0), Spec(1)])], Real.Classes,
                summons: Real.Summons.Values, passives: Real.Passives.Values, talents: Real.Talents.Values, reactions: Real.Reactions.Values,
                specs: [.. Real.Specs.Values.Where(s => s.Id != path.Id), path with { Forks = forks }]);
        }
        Assert.NotNull(Made("one-more"));
        Assert.Throws<InvalidDataException>(() => Made("one"));
        Assert.Equal(36, Real.Specs.Values.SelectMany(s => s.Forks).SelectMany(f => f.Finals ?? []).Select(f => f.Id).Distinct().Count());
    }
}
