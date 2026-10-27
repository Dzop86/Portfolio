using Rpg.Core;

namespace Rpg.Client.Tests;

/// <summary>
/// Planning and sharing the tree (sprint 151, D82): a build's code there and back, an unreadable, old or too high one refused
/// with its reason; the path a node asks and its cost; a plan's ghosts, never saved; a suggested build loaded as a draft; the
/// builds drawn for the measure out of CI, always legal, and the rare talents.
/// </summary>
public class TreePlanTests
{
    private static GameData Data => GameData.Embedded;

    private static Hero Mage(int level, params string[] talents) =>
        new("Ondine", "female-a", "mage", Level: level, Talents: talents.Length == 0 ? null : talents);

    [Theory]
    [InlineData("sentinel-matter")]
    [InlineData("guard-manner")]
    [InlineData("mage-pure")]
    public void ABuildsCode_GoesThereAndBack(string build)
    {
        IReadOnlyList<string> talents = Progression.Talented(Mage(45), build, Data).Talents!;
        string code = BuildCode.Write(talents, Data);
        Assert.StartsWith($"OSM{Talents.Version}-", code, StringComparison.Ordinal);
        CodeRead read = BuildCode.Read(code.ToLowerInvariant(), 45, Data);
        Assert.Null(read.Refusal);
        Assert.Equal(talents.Order(StringComparer.Ordinal), read.Talents!.Order(StringComparer.Ordinal));
        Assert.Equal(code, BuildCode.Write(read.Talents!, Data));
    }

    [Fact]
    public void ACodeRefused_SaysWhy()
    {
        var fr = new Texts("fr");
        Assert.Equal(CodeRefusal.Unreadable, BuildCode.Read("bonjour", 45, Data).Refusal);
        Assert.Equal(CodeRefusal.Unreadable, BuildCode.Read($"OSM{Talents.Version}-ZZZ", 45, Data).Refusal);
        CodeRead old = BuildCode.Read("OSM2-0102", 45, Data);
        Assert.Equal((CodeRefusal.OldVersion, 2), (old.Refusal, old.Number));
        Assert.Equal("Ce code vient d'une ancienne version de l'arbre (2) : il ne se charge plus.", fr.CodeRefused(old));
        // Matter's build at 45 read at 35: the Conversion asks 45.
        string matter = BuildCode.Write(Progression.Talented(Mage(45), "mage-matter", Data).Talents!, Data);
        CodeRead low = BuildCode.Read(matter, 35, Data);
        Assert.Equal((CodeRefusal.Level, 45), (low.Refusal, low.Number));
        Assert.Equal("This build needs level 45.", new Texts("en").CodeRefused(low));
    }

    [Fact]
    public void APath_IsWhatANodeAsks_ItsCostTheSumOfItsNodes()
    {
        // From nothing to Nomad: a door, a first floor, a second, the key.
        IReadOnlyList<string> path = TreePlan.Path([], "nomad", Data)!;
        Assert.Equal(4, path.Count);
        Assert.True(Data.Talents[path[0]].IsFork);
        Assert.Equal("nomad", path[^1]);
        Assert.Equal(path.Sum(id => Data.Talents[id].Cost), TreePlan.Cost(path, Data));
        Assert.Equal(5, TreePlan.Cost(path, Data));
        Assert.Null(Talents.Problem([.. path], Hero.MaxLevel, Data));
        // From a door and a first floor, only what is missing; nothing for a node taken; no way past an exclusion.
        Assert.Equal(["dancing-step", "nomad"], TreePlan.Path(["bogging", "side-step"], "nomad", Data));
        Assert.Empty(TreePlan.Path(["bogging"], "bogging", Data)!);
        Assert.Null(TreePlan.Path(["bogging", "up-close"], "long-sight", Data));
        Assert.Equal("Chemin : Sceau → Nomade (3 point(s))", new Texts("fr").TreePath(["seal", "nomad"], Data));
    }

    [Fact]
    public void APlansGhosts_AreNeverSaved_ASuggestedBuild_StaysADraft()
    {
        Hero hero = Mage(45, "bogging", "side-step");
        var editor = new PointsEditor(hero, Data);
        // A plan at 60 from the draft: what it adds is a ghost, and the server refuses it at 45 (TalentTests).
        var plan = new PointsEditor(editor.Draft with { Level = TreePlan.PlanLevel, Talents = [.. editor.TalentsTaken] }, Data);
        foreach (string id in TreePlan.Path(plan.TalentsTaken, "nomad", Data)!.Concat(["heavy-blows", "short-cooldown", "thrift", "seal"]))
            plan.Take(Data.Talents[id]);
        Assert.Equal(["dancing-step", "nomad", "heavy-blows", "short-cooldown", "thrift", "seal"], TreePlan.Ghosts(plan.TalentsTaken, editor.TalentsTaken));
        Assert.NotNull(Talents.Problem(plan.TalentsTaken, hero.Level, Data));
        // A suggested build: the class's three, loaded in the draft, the hero's talents untouched until the save.
        Assert.Equal(["mage-matter", "mage-manner", "mage-pure"], TreePlan.Suggested("mage"));
        Assert.Empty(editor.LoadTalents(Progression.Builds["mage-pure"]));
        Assert.Equal(["heart-fire", "side-step", "remanence", "scorched-earth"], editor.TalentsTaken);
        Assert.Equal(["bogging", "side-step"], editor.Hero.Talents);
        Assert.True(editor.TalentsChanged);
        // At 35, the key stays out.
        Assert.Equal(["scorched-earth"], new PointsEditor(Mage(35), Data).LoadTalents(Progression.Builds["mage-pure"]));
    }

    [Fact]
    public void TheBuildsDrawn_AreLegal_AndTheRareTalentsListed()
    {
        var builds = new List<IReadOnlyList<string>>();
        for (int i = 0; i < 60; i++)
        {
            IReadOnlyList<string> b = TreeDraws.Random("chilling", 45, new Random(i), Data);
            Assert.Null(Talents.Problem(b, 45, Data));
            Assert.Equal("chilling", b[0]);
            // Every point spent, or nothing left to take.
            Assert.True(Talents.Points(45) - b.Sum(id => Data.Talents[id].Cost) <= 1);
            builds.Add(b);
        }
        IReadOnlyList<(string Talent, double Share)> rare = TreeDraws.Rare(builds, Data);
        Assert.Contains(rare, r => r.Talent == "bogging" && r.Share == 0);
        Assert.DoesNotContain(rare, r => r.Talent == "chilling");
    }
}
