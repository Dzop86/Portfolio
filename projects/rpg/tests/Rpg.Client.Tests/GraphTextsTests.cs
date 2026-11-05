using Rpg.Core;

namespace Rpg.Client.Tests;

/// <summary>
/// The tree as a graph in the window (sprint 147, D82): each refusal said before the click, in French and in English; the
/// cascade of a talent given back, shown first, then done (since sprint 182, D93, a tree without doors).
/// </summary>
public class GraphTextsTests
{
    private static readonly GameData Data = GameData.Embedded;

    private static TalentRefusal Why(int level, string id, params string[] taken) => TalentReasons.WhyNot(taken, id, level, Data)!;

    [Fact]
    public void EachRefusal_IsSaid_InFrenchAndInEnglish()
    {
        var fr = new Texts("fr");
        var en = new Texts("en");
        Assert.Equal("Demande d'abord Pas de côté ou Adossé ou Relais.", fr.WhyNot(Why(33, "seal"), Data));
        Assert.Equal("Asks for Side Step or Backed or Relay first.", en.WhyNot(Why(33, "seal"), Data));
        Assert.Equal("Une clé par style : Nomade est déjà prise.", fr.WhyNot(Why(60, "echo", "side-step", "seal", "nomad"), Data));
        Assert.Equal("S'ouvre au niveau 45.", fr.WhyNot(Why(44, "nomad", "side-step", "seal"), Data));
        string heavy = Data.Talents["heavy-blows"].Name.Fr;
        Assert.Equal($"Ne se prend pas avec {heavy}.", fr.WhyNot(Why(33, "light-blows", "heavy-blows"), Data));
        Assert.Equal($"Not taken with {Data.Talents["heavy-blows"].Name.En}.", en.WhyNot(Why(33, "light-blows", "heavy-blows"), Data));
        Assert.Equal("Pas assez de points : ce talent en coûte 1.", fr.WhyNot(Why(30, "side-step", "up-close"), Data));
        Assert.Equal("The tree opens at level 30.", en.WhyNot(Why(29, "up-close"), Data));
    }

    [Fact]
    public void GivingBack_ShowsWhatGoesWithIt_ThenGivesItAllBack()
    {
        // I1 b: Up close takes Heavy Push with it, which nothing else holds up; the Terrain stays.
        string[] tree = ["side-step", "seal", "up-close", "heavy-push"];
        var editor = new PointsEditor(new Hero("Ondine", "female-a", "archer", Level: 45, Talents: tree), Data);
        Assert.Equal(["heavy-push"], editor.Losing(Data.Talents["up-close"]));
        Assert.Empty(editor.Losing(Data.Talents["heavy-push"]));
        Assert.True(editor.CanDrop(Data.Talents["up-close"]));
        editor.Drop(Data.Talents["up-close"]);
        Assert.Equal(["side-step", "seal"], editor.TalentsTaken);
        Assert.Equal("Le rendre rend aussi : Sceau", new Texts("fr")["talents.losing", Data.Talents["seal"].Name.Fr]);
    }

    [Fact]
    public void TheBranches_HaveTheirNames_InBothLanguages()
    {
        foreach (Branch b in Enum.GetValues<Branch>())
        {
            Assert.DoesNotContain("talents.branch", new Texts("fr")["talents.branch." + b], StringComparison.Ordinal);
            Assert.DoesNotContain("talents.branch", new Texts("en")["talents.branch." + b], StringComparison.Ordinal);
        }
        Assert.Equal(("Mêlée", "Melee"), (new Texts("fr")["talents.branch.Melee"], new Texts("en")["talents.branch.Melee"]));
    }
}
