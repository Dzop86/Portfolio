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
    public void ALineageWithoutFinals_KeepsItsEvolutionAt100_UntilItsClassIsConverted()
    {
        Assert.All(Real.Specs.Values.SelectMany(s => s.Forks), f => Assert.Null(f.Finals));
        Hero hero = new("Ondine", "female-a", "warrior", Level: 100, Spec: "palisade", Fork: Lineage);
        Assert.Equal([30, 50, 100], Specs.TiersOf(hero, Real).Select(t => t.Level));
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
}
