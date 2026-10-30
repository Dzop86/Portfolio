namespace Rpg.Client.Tests;

/// <summary>The states of a spell's tile (sprint 163, <c>docs/graphisme/s163-icones-arbre.md</c> §1.6).</summary>
public class SpellIconStatesTests
{
    [Fact]
    public void ATile_IsWhole_FlatWhenNotNow_AndWithoutMarksWhenSmall()
    {
        Assert.Equal(new SpellLook("", SpellRing.None, 0), SpellIconStates.Look(faint: false, size: 44));
        Assert.Equal("-locked", SpellIconStates.Look(faint: true, size: 44).Variant);
        // Flat wins over small: a locked spell in a small place is still flat, never at 40 %.
        Assert.Equal("-locked", SpellIconStates.Look(faint: true, size: 24).Variant);
        Assert.Equal("-small", SpellIconStates.Look(faint: false, size: 31.5f).Variant);
        Assert.Equal("", SpellIconStates.Look(faint: false, size: SpellIconStates.SmallBelow).Variant);
    }

    [Fact]
    public void TheChosenSpell_IsRingedInPistachio_EvenUnderThePointer_AndTheHoveredOne_InChocolate()
    {
        Assert.Equal(SpellRing.Hover, SpellIconStates.Look(false, 44, hovered: true).Ring);
        Assert.Equal(SpellRing.Selected, SpellIconStates.Look(false, 44, selected: true).Ring);
        Assert.Equal(SpellRing.Selected, SpellIconStates.Look(false, 44, hovered: true, selected: true).Ring);
        // A spell not castable now may still be looked at: its card shows under the pointer.
        Assert.Equal(SpellRing.Hover, SpellIconStates.Look(true, 44, hovered: true).Ring);
        Assert.Equal("#c9a27f", SpellIconStates.HoverRing);
        Assert.Equal("#bef374", SpellIconStates.SelectedRing);
    }

    [Fact]
    public void ATile_RisesUnderThePointer_AndGoesDownWhenPressed()
    {
        Assert.Equal(-1, SpellIconStates.Look(false, 44, hovered: true).Lift);
        Assert.Equal(1, SpellIconStates.Look(false, 44, hovered: true, pressed: true).Lift);
        Assert.Equal(0, SpellIconStates.Look(false, 44, selected: true).Lift);
    }
}
