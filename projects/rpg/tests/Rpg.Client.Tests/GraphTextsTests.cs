using Rpg.Core;

namespace Rpg.Client.Tests;

/// <summary>
/// The tree as a graph in the window (sprint 147, D82): each refusal said before the click, in French and in English; the
/// cascade of a talent given back, shown first, then done.
/// </summary>
public class GraphTextsTests
{
    private static readonly GameData Data = GameData.Embedded;

    private static TalentRefusal Why(int level, string id, params string[] taken) => TalentReasons.WhyNot(taken, id, level, Data)!;

    [Theory]
    [InlineData("fr", "Demande d'abord une porte : une paire ou un Cœur.")]
    [InlineData("en", "Asks for a door first: a pair or a Heart.")]
    public void ADoor_IsAskedFor(string lang, string said) => Assert.Equal(said, new Texts(lang).WhyNot(Why(30, "up-close"), Data));

    [Fact]
    public void EachRefusal_IsSaid_InFrenchAndInEnglish()
    {
        var fr = new Texts("fr");
        var en = new Texts("en");
        Assert.Equal("Demande d'abord Pas de côté ou Adossé ou Relais.", fr.WhyNot(Why(33, "seal", "bogging"), Data));
        Assert.Equal("Asks for Side Step or Backed or Relay first.", en.WhyNot(Why(33, "seal", "bogging"), Data));
        Assert.Equal("Une clé par style : Nomade est déjà prise.", fr.WhyNot(Why(60, "echo", "bogging", "side-step", "seal", "nomad"), Data));
        Assert.Equal("S'ouvre au niveau 45.", fr.WhyNot(Why(44, "nomad", "bogging", "side-step", "seal"), Data));
        Assert.Equal("Ne se prend pas avec Cœur : Feu.", fr.WhyNot(Why(30, "heart-air", "heart-fire"), Data));
        Assert.Equal("Not taken with Heart: Fire.", en.WhyNot(Why(30, "heart-air", "heart-fire"), Data));
        Assert.Equal("Paire impossible : l'Eau est déjà prise par Boue.", fr.WhyNot(Why(35, "chilling", "bogging"), Data));
        Assert.Equal("Pair refused: Water is already taken by Mud.", en.WhyNot(Why(35, "chilling", "bogging"), Data));
        Assert.Equal("Pas assez de points : ce talent en coûte 1.", fr.WhyNot(Why(30, "electrifying", "bogging", "up-close"), Data));
        Assert.Equal("The tree opens at level 30.", en.WhyNot(Why(29, "bogging"), Data));
        Assert.Equal("Demande d'abord Électrisation.", fr.WhyNot(Why(45, "convert-lightning"), Data));
    }

    [Fact]
    public void GivingBack_ShowsWhatGoesWithIt_ThenGivesItAllBack()
    {
        // I1 b: Electrifying takes its braid with it; the style stays, on Mud.
        string[] tree = ["electrifying", "bogging", "transmute-fire-lightning", "prism", "strong-signature", "up-close", "heavy-push"];
        var editor = new PointsEditor(new Hero("Ondine", "female-a", "sentinel", Level: 45, Talents: tree), Data);
        Assert.Equal(["transmute-fire-lightning", "prism", "strong-signature"], editor.Losing(Data.Talents["electrifying"]));
        Assert.Empty(editor.Losing(Data.Talents["heavy-push"]));
        Assert.True(editor.CanDrop(Data.Talents["electrifying"]));
        editor.Drop(Data.Talents["electrifying"]);
        Assert.Equal(["bogging", "up-close", "heavy-push"], editor.TalentsTaken);
        Assert.Equal("Le rendre rend aussi : Transmutation : Feu → Foudre", new Texts("fr")["talents.losing", Data.Talents["transmute-fire-lightning"].Name.Fr]);
    }

    [Fact]
    public void TheBranches_HaveTheirNames_InBothLanguages()
    {
        foreach (Branch b in Enum.GetValues<Branch>())
        {
            Assert.DoesNotContain("talents.branch", new Texts("fr")["talents.branch." + b], StringComparison.Ordinal);
            Assert.DoesNotContain("talents.branch", new Texts("en")["talents.branch." + b], StringComparison.Ordinal);
        }
        Assert.Equal(("Élément pur", "Pure element"), (new Texts("fr")["talents.branch.Pure"], new Texts("en")["talents.branch.Pure"]));
    }
}
