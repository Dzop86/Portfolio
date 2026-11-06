using Rpg.Core;

namespace Rpg.Client.Tests;

/// <summary>
/// The final classes on the Specialisation page (sprint 196, D96, docs/maquette-specialisation.md §14): the column 100 of the open branch, its places and links, the states,
/// the foot, the confirmation, the counter. No lineage of the data has final classes before S197: the Archer's are made up here, Sightline's first lineage with two and its
/// second without (the mixed case), Ambush's two with two each, Trail's none.
/// </summary>
public class SpecFinalsPageTests
{
    private static readonly SpecContext Free = new(QuestDone: true, Shards: 100_000, InVillage: true);

    private static readonly Texts Fr = new("fr");

    private static SpecFinal Final(string id, FinalTemplate template) =>
        new(id, template, new SpecTier(100, new LocalizedText($"Titre {id}", $"Title {id}"), new LocalizedText("Texte", "Text")), new LocalizedText($"Dit {id}", $"Said {id}"));

    private static readonly GameData Data = Made();

    private static GameData Made()
    {
        GameData real = GameData.Embedded;
        static SpecFork Ends(SpecFork fork, string a, string b) =>
            fork with { Tiers = [.. fork.Tiers.Where(t => t.Level == Specs.Forks)], Finals = [Final(a, FinalTemplate.Extends), Final(b, FinalTemplate.Faster)] };
        Specialisation sightline = real.Specs["sightline"], ambush = real.Specs["ambush"];
        // In the order of the data, which is the page's.
        Specialisation Changed(Specialisation s) => s.Id switch
        {
            "sightline" => sightline with { Forks = [Ends(sightline.Forks[0], "eye", "relentless"), sightline.Forks[1]] },
            "ambush" => ambush with { Forks = [Ends(ambush.Forks[0], "hawk-lord", "two-hawks"), Ends(ambush.Forks[1], "snare-lord", "two-snares")] },
            _ => s,
        };
        return new GameData(real.Spells.Values, real.Maps.Values, real.Scenarios.Values, real.Classes, real.Towns.Values, real.Dialogues.Values, real.Summons.Values,
            real.Quests.Values, real.Items.Values, real.Sets.Values, real.Families.Values, real.Dungeons.Values, real.Jobs.Values, passives: real.Passives.Values,
            talents: real.Talents.Values, reactions: real.Reactions.Values, specs: real.Specs.Values.Select(Changed));
    }

    private static Hero Archer(int level, string? spec = null, string? fork = null, string? final = null) =>
        new("Ondine", "female-a", "archer", Level: level, Spec: spec, Fork: fork, Final: final);

    private static SpecPage Page(Hero hero, SpecContext? context = null, string? preview = null, string? open = null) =>
        new(new PointsEditor(hero, Data), context ?? Free, preview, open);

    private static SpecState State(SpecPage page, string key) => page[key].State;

    // ---- The column 100 (§14.1, §14.2) ----

    [Fact]
    public void TheMadeUpData_IsValid_AndTheRealDataHasNoFinalYet()
    {
        Assert.Equal(2, Data.Specs["sightline"].Forks[0].Finals!.Count);
        Assert.DoesNotContain(GameData.Embedded.Specs.Values.SelectMany(s => s.Forks), f => f.Finals is { Count: > 0 });
    }

    [Theory]
    [InlineData("sightline", new[] { "marksman:eye", "marksman:relentless", "barrage@100" })]
    [InlineData("ambush", new[] { "falconer:hawk-lord", "falconer:two-hawks", "trapper:snare-lord", "trapper:two-snares" })]
    [InlineData("trail", new[] { "leaper@100", "runner@100" })]
    public void TheColumn100_ShowsTheOpenBranch_FourThreeOrTwoTiles(string open, string[] keys)
    {
        SpecPage page = Page(Archer(100), open: open);
        Assert.Equal(keys, page.Last.Select(n => n.Key));
        Assert.Equal(9 + keys.Length, page.Nodes.Count);
        Assert.Equal(open != "trail", page.OpenHasFinals);
    }

    [Fact]
    public void TheOpenBranch_IsTheOneClicked_ElseTheOneLookedAt_TheHeros_OrTheFirst()
    {
        Assert.Equal("sightline", Page(Archer(60)).OpenBranch);
        Assert.Equal("trail", Page(Archer(60, "trail", "leaper")).OpenBranch);
        Assert.Equal("ambush", Page(Archer(60, "trail", "leaper"), preview: "ambush").OpenBranch);
        Assert.Equal("sightline", Page(Archer(60, "trail", "leaper"), preview: "ambush", open: "sightline").OpenBranch);
        Assert.Equal("trail", Page(Archer(60, "trail"), open: "nowhere").OpenBranch);
    }

    [Fact]
    public void TheFinalTiles_HaveTheirTitleTemplateLoreAndKey()
    {
        SpecNode eye = Page(Archer(100, "sightline", "marksman"))["marksman:eye"];
        Assert.Equal(("sightline", "marksman", "eye", FinalTemplate.Extends, 100), (eye.Spec, eye.Fork, eye.Final, eye.Template, eye.Level));
        Assert.Equal("Dit eye", eye.Lore!.Fr);
        Assert.Equal(Data.Specs["sightline"].Forks[0].Icon, eye.Icon);
        Assert.Equal(["sightline", "marksman", "marksman:eye"], SpecPage.RoadTo(eye));
    }

    private static (float X, float Y, float W, float H)[] Tiles(Specialisation branch, SpecPage page)
    {
        var rects = new List<(float, float, float, float)> { (0, SpecTree.RootY, SpecTree.RootWidth, SpecTree.RootHeight), (0, SpecTree.ButtonY, SpecTree.RootWidth, SpecTree.ButtonHeight) };
        for (int b = 0; b < 3; b++)
        {
            rects.Add((SpecTree.BranchX, SpecTree.BranchY(b), SpecTree.TileWidth, SpecTree.TileHeight));
            for (int f = 0; f < 2; f++)
                rects.Add((SpecTree.ForkX, SpecTree.ForkY((b * 2) + f), SpecTree.TileWidth, SpecTree.TileHeight));
        }
        rects.AddRange(page.Last.Select(n => (SpecTree.FinalX, SpecTree.LastY(branch, n), SpecTree.FinalWidth, SpecTree.TileHeight)));
        return [.. rects];
    }

    [Theory]
    [InlineData("sightline")]
    [InlineData("ambush")]
    [InlineData("trail")]
    public void EachOpenBranch_HoldsInItsRoom_WithoutOverlap_NorATargetUnder44(string open)
    {
        SpecPage page = Page(Archer(100), open: open);
        var rects = Tiles(Data.Specs[open], page);
        Assert.All(rects, r =>
        {
            Assert.True(r.H >= 44 && r.W >= 44);
            Assert.True(r.X >= 0 && r.Y >= 0 && r.X + r.W <= SpecTree.Width && r.Y + r.H <= SpecTree.Height, $"{r}");
        });
        for (int i = 0; i < rects.Length; i++)
        {
            for (int j = i + 1; j < rects.Length; j++)
            {
                var (a, b) = (rects[i], rects[j]);
                Assert.False(a.X < b.X + b.W && b.X < a.X + a.W && a.Y < b.Y + b.H && b.Y < a.Y + a.H, $"{a} over {b}");
            }
        }
        Assert.Equal(SpecTree.Width, SpecTree.FinalX + SpecTree.FinalWidth);
        Assert.Equal(40, SpecTree.FinalX - (SpecTree.ForkX + SpecTree.TileWidth));
    }

    [Fact]
    public void NoEntryOfTheColumn100_IsWithin28OfALineagesCentre()
    {
        float[] lineages = [.. Enumerable.Range(0, 6).Select(i => SpecTree.ForkY(i) + (SpecTree.TileHeight / 2))];
        float[] entries = [.. new[] { SpecTree.FinalY(0, 0), SpecTree.FinalY(0, 1), SpecTree.FinalY(1, 0), SpecTree.FinalY(1, 1) }.Select(y => y + (SpecTree.TileHeight / 2))];
        Assert.All(entries, e => Assert.All(lineages, c => Assert.True(Math.Abs(e - c) >= 28, $"{e} near {c}")));
    }

    private static IEnumerable<((float X, float Y) A, (float X, float Y) B)> Segments((float X, float Y)[] line) => line.Zip(line.Skip(1));

    // Axis-aligned segments, as the links are: they meet if their boxes do, a touch included.
    private static bool Meet(((float X, float Y) A, (float X, float Y) B) s, ((float X, float Y) A, (float X, float Y) B) t) =>
        Math.Min(s.A.X, s.B.X) <= Math.Max(t.A.X, t.B.X) && Math.Min(t.A.X, t.B.X) <= Math.Max(s.A.X, s.B.X)
        && Math.Min(s.A.Y, s.B.Y) <= Math.Max(t.A.Y, t.B.Y) && Math.Min(t.A.Y, t.B.Y) <= Math.Max(s.A.Y, s.B.Y);

    [Theory]
    [InlineData(true, true)]
    [InlineData(true, false)]
    [InlineData(false, true)]
    [InlineData(false, false)]
    public void TheLinksOfTwoLineages_NeverMeet_WhicheverBranchIsOpen(bool firstEnds, bool secondEnds)
    {
        for (int b = 0; b < 3; b++)
        {
            (float X, float Y)[][] Lines(int fork, bool ends) => ends
                ? [SpecTree.ToFinal(b, fork, SpecTree.FinalY(fork, 0)), SpecTree.ToFinal(b, fork, SpecTree.FinalY(fork, 1))]
                : [SpecTree.ToFinal(b, fork, SpecTree.SoleY(fork))];
            var first = Lines(0, firstEnds).SelectMany(Segments).ToArray();
            var second = Lines(1, secondEnds).SelectMany(Segments).ToArray();
            foreach (var s in first)
                Assert.All(second, t => Assert.False(Meet(s, t), $"branch {b}: {s} meets {t}"));
            // The elbows stand in the gutter, at 8 px at least from each column.
            Assert.All(first.Concat(second), s => Assert.InRange(s.A.X, SpecTree.ForkX + SpecTree.TileWidth, SpecTree.FinalX));
        }
        Assert.Equal((SpecTree.ElbowOut, SpecTree.ElbowIn), (SpecTree.Elbow(0, 0), SpecTree.Elbow(0, 1)));
        Assert.Equal((SpecTree.ElbowIn, SpecTree.ElbowOut), (SpecTree.Elbow(2, 0), SpecTree.Elbow(2, 1)));
    }

    // ---- The states (§14.5) ----

    [Fact]
    public void BelowHundred_TheSistersOfTheHerosLineage_AreAhead_TheOthersCrossedOrLocked()
    {
        SpecPage page = Page(Archer(62, "sightline", "marksman"));
        Assert.Equal(SpecState.Ahead, State(page, "marksman:eye"));
        Assert.Equal(SpecState.Ahead, State(page, "marksman:relentless"));
        Assert.Equal(SpecState.Crossed, State(page, "barrage@100"));
        Assert.Equal(SpecState.Locked, State(Page(Archer(62)), "marksman:eye"));
        SpecPage other = Page(Archer(62, "sightline", "marksman"), open: "ambush");
        Assert.All(other.Last, n => Assert.Equal(SpecState.Crossed, n.State));
        Assert.DoesNotContain(other.Last, n => n.Head);
    }

    [Fact]
    public void AtHundred_ALineageWithoutItsFinal_OpensBoth_ThatBreathe_AndTheTabHasItsPoint()
    {
        SpecPage page = Page(Archer(100, "sightline", "marksman"));
        Assert.Equal(SpecState.Open, State(page, "marksman:eye"));
        Assert.Equal(SpecState.Open, State(page, "marksman:relentless"));
        Assert.True(page.Pending);
        Assert.Equal((2, 3), page.Choices);
        Assert.Equal("marksman:eye", page.Opening);
        Assert.Equal(Fr["spec.final.due"], page.Foot(page["marksman"], Fr).First);
    }

    [Fact]
    public void AFinalTaken_IsChosen_ItsSisterCrossed_WithAFourthStroke()
    {
        SpecPage page = Page(Archer(100, "sightline", "marksman", "eye"));
        Assert.Equal(SpecState.Chosen, State(page, "marksman:eye"));
        Assert.Equal(SpecState.Crossed, State(page, "marksman:relentless"));
        Assert.Equal(["barrage", "ambush", "trail", "marksman:relentless"], page.Nodes.Where(n => n.Head).Select(n => n.Key));
        Assert.Equal((3, 3), page.Choices);
        Assert.False(page.Pending);
        Assert.Equal("marksman:eye", page.Opening);
        Assert.Equal(SpecWood.Alive, page.WoodTo(page["marksman:eye"]));
    }

    [Fact]
    public void TheSisterLineage_AndAnotherBranch_HaveTheirFinalsCrossed_ALookOpensThem()
    {
        SpecPage page = Page(Archer(100, "sightline", "barrage"), open: "ambush");
        Assert.All(page.Last, n => Assert.Equal(SpecState.Crossed, n.State));
        Assert.Equal(SpecState.Crossed, State(Page(Archer(100, "sightline", "barrage")), "marksman:eye"));
        SpecPage looking = Page(Archer(100, "sightline", "barrage"), preview: "ambush");
        Assert.All(looking.Last, n => Assert.Equal(SpecState.Open, n.State));
    }

    [Fact]
    public void ABranchWithoutItsLineage_OpensItsFinalsDirectly_WithoutBreath_ElseTheyWait()
    {
        SpecPage page = Page(Archer(100, "sightline"));
        Assert.Equal(SpecState.Open, State(page, "marksman:eye"));
        Assert.Equal(SpecState.Waiting, State(Page(Archer(100)), "marksman:eye"));
        Assert.Equal(SpecState.Waiting, State(Page(Archer(100, "sightline"), new SpecContext(false, 0, true)), "marksman:eye"));
        Assert.Equal(SpecState.Open, State(Page(Archer(100), preview: "sightline"), "marksman:eye"));
    }

    [Fact]
    public void WithoutAnyFinal_TheCounterStaysAtTwo_AtHundred()
    {
        Assert.Equal((2, 2), Page(Archer(100, "trail", "leaper")).Choices);
        Assert.Equal((2, 2), Page(Archer(100, "sightline", "barrage")).Choices);
        Assert.Equal((0, 3), Page(Archer(100)).Choices);
        Assert.Equal((1, 3), Page(Archer(100, "sightline")).Choices);
        Assert.Equal((0, 2), new SpecPage(new PointsEditor(Archer(100), GameData.Embedded), Free).Choices);
        Assert.False(Page(Archer(100, "sightline", "barrage")).Pending);
    }

    // ---- The foot (§14.7), at level 100: 500, 1 000 and 3 000 Éclats ----

    [Fact]
    public void TheFoot_ReadsEachLineOfTheMockup()
    {
        SpecPage due = Page(Archer(100, "sightline", "marksman"));
        SpecFoot first = due.Foot(due["marksman:eye"], Fr);
        Assert.Equal((Fr["spec.free"], Fr["spec.final.then-cost", "500"]), (first.First, first.Second));
        Assert.Equal((Fr["spec.take.final"], true, "eye", 0L), (first.Action!.Text, first.Action.Enabled, first.Action.Final, first.Action.Cost));

        SpecPage taken = Page(Archer(100, "sightline", "marksman", "eye"));
        SpecFoot chosen = taken.Foot(taken["marksman:eye"], Fr);
        Assert.Equal((Fr["spec.price.final", "500"], Fr["spec.price.fork", "1 000"], (SpecAction?)null), (chosen.First, chosen.Second, chosen.Action));
        SpecFoot sister = taken.Foot(taken["marksman:relentless"], Fr);
        Assert.Equal((Fr["spec.price.final", "500"], Fr["spec.final.same"], Fr["spec.cross", "500"], 500L), (sister.First, sister.Second, sister.Action!.Text, sister.Action.Cost));

        SpecPage fromSister = Page(Archer(100, "sightline", "barrage"));
        SpecFoot withFork = fromSister.Foot(fromSister["marksman:eye"], Fr);
        Assert.Equal((Fr["spec.price.fork", "1 000"], Fr["spec.final.with-fork"], 1000L, "marksman"), (withFork.First, withFork.Second, withFork.Action!.Cost, withFork.Action.Fork));

        SpecPage other = Page(Archer(100, "trail", "leaper"), open: "ambush");
        SpecFoot withBranch = other.Foot(other["trapper:two-snares"], Fr);
        Assert.Equal((Fr["spec.price.branch", "3 000"], Fr["spec.final.with-branch"], Fr["spec.cross", "3 000"]), (withBranch.First, withBranch.Second, withBranch.Action!.Text));

        SpecPage atOnce = Page(Archer(100, "sightline"));
        Assert.Equal(Fr["spec.at-once"], atOnce.Foot(atOnce["marksman:eye"], Fr).Second);

        SpecPage young = Page(Archer(62, "sightline", "marksman"));
        SpecFoot ahead = young.Foot(young["marksman:eye"], Fr);
        Assert.Equal((Fr["spec.ahead", 38], Fr["spec.final.ahead"]), (ahead.First, ahead.Second));
        SpecPage low = Page(Archer(62));
        Assert.Equal((Fr["spec.opens", "100"], Fr["spec.final.pick"]), (low.Foot(low["marksman:eye"], Fr).First, low.Foot(low["marksman:eye"], Fr).Second));
    }

    [Fact]
    public void TheFinalsButton_SaysWhatStopsIt_AsALineagesDoes()
    {
        Hero hero = Archer(100, "sightline", "marksman", "eye");
        SpecAction Of(SpecContext context) => Page(hero, context).Foot(Page(hero, context)["marksman:relentless"], Fr).Action!;
        Assert.Equal((Fr["spec.need.village"], false), (Of(new SpecContext(true, 100_000, false)).Text, Of(new SpecContext(true, 100_000, false)).Enabled));
        Assert.Equal(Fr["spec.need.shards", "100"], Of(new SpecContext(true, 400, true)).Text);
        // The first choice of a final class asks neither a village nor Éclats.
        Assert.True(Page(Archer(100, "sightline", "marksman"), new SpecContext(true, 0, false)).Foot(Page(Archer(100, "sightline", "marksman"), new SpecContext(true, 0, false))["marksman:eye"], Fr).Action!.Enabled);
    }

    // ---- The confirmation (§14.8) ----

    [Fact]
    public void AFirstFinal_CompletesTheRoad_Free_AndMovesNoSpell()
    {
        SpecConfirm words = Page(Archer(100, "sightline", "marksman")).Confirm("sightline", "marksman", "eye", Fr);
        Assert.Equal(Fr["spec.confirm.final"], words.Title);
        SpecGift gift = Assert.Single(words.Gifts);
        Assert.Equal("Titre eye", gift.Title);
        Assert.Equal($"{Fr["spec.level", 100]} · {Fr["spec.confirm.signature", Fr["spec.template.Extends"]]}", gift.Line);
        Assert.Equal(Fr["spec.confirm.later.final", "500"], words.Later);
        Assert.Equal((Fr["spec.confirm.final.yes"], Fr["spec.confirm.no"], true), (words.Yes, words.No, words.First));
        Assert.Empty(words.Ranks);
        Assert.Null(words.Deck);
    }

    [Fact]
    public void ACrossedFinal_ToItsSister_SaysNothingElseMoves_AndThePrice()
    {
        SpecConfirm words = Page(Archer(100, "sightline", "marksman", "eye")).Confirm("sightline", "marksman", "relentless", Fr);
        Assert.Equal(Fr["spec.confirm.cross", "Titre eye"], words.Title);
        Assert.Null(words.Leaves);
        Assert.Equal([Fr["spec.confirm.final.same"]], words.Ranks);
        Assert.Equal(Fr["spec.confirm.cost", "500", "99 500"], words.Price);
        Assert.Equal((Fr["spec.cross", "500"], Fr["spec.confirm.final.keep"]), (words.Yes, words.No));
        Assert.Equal("Titre relentless", Assert.Single(words.Gifts).Title);
    }

    [Fact]
    public void ALineageCrossed_AtHundred_DropsTheFinal_OrBringsOne_AndSaysWhatLeaves()
    {
        Hero hero = Archer(100, "sightline", "marksman", "eye");
        SpecConfirm drop = Page(hero).Confirm("sightline", "barrage", Fr);
        Assert.Equal(Fr["spec.confirm.leave", "Longue-vue, Titre eye"], drop.Leaves);
        Assert.Equal(Fr["spec.confirm.final.after"], drop.Ranks[^1]);
        SpecConfirm branch = Page(hero).Confirm("ambush", "falconer", "two-hawks", Fr);
        Assert.Equal(Fr["spec.confirm.leave", "Mire, Longue-vue, Titre eye"], branch.Leaves);
        Assert.DoesNotContain(Fr["spec.confirm.final.after"], branch.Ranks);
        Assert.Equal(Fr["spec.confirm.cost", "3 000", "97 000"], branch.Price);
        Assert.Contains(branch.Gifts, g => g.Title == "Titre two-hawks");
        // A first road taken at once to its final class.
        SpecConfirm late = Page(Archer(100)).Confirm("ambush", "trapper", "snare-lord", Fr);
        Assert.Equal(3, late.Gifts.Count);
        Assert.Equal(Fr["spec.confirm.later.all", "500", "1 000", "3 000"], late.Later);
        Assert.Equal(Fr["spec.confirm.first"], late.Title);
    }

    [Fact]
    public void TheTemplates_HaveShortNames_InBothLanguages()
    {
        var en = new Texts("en");
        Assert.All(Enum.GetValues<FinalTemplate>(), t =>
        {
            Assert.InRange(SpecPage.TemplateName(t, Fr).Length, 3, 20);
            Assert.InRange(SpecPage.TemplateName(t, en).Length, 3, 20);
        });
        Assert.Equal(12, Enum.GetValues<FinalTemplate>().Select(t => SpecPage.TemplateName(t, Fr)).Distinct().Count());
    }
}
