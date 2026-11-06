using Rpg.Core;

namespace Rpg.Client.Tests;

/// <summary>The Specialisation page (sprint 191, D95, docs/maquette-specialisation.md): its layout, the states of its tiles, the foot of its panel, its links and its confirmation.</summary>
public class SpecPageTests
{
    private static GameData Data => GameData.Embedded;

    private static readonly SpecContext Free = new(QuestDone: true, Shards: 100_000, InVillage: true);

    private static readonly Texts Fr = new("fr");

    private static Hero Hero(string cls, int level, string? spec = null, string? fork = null) => new("Ondine", "female-a", cls, Level: level, Spec: spec, Fork: fork);

    private static SpecPage Page(Hero hero, SpecContext? context = null, string? preview = null, bool readOnly = false, string? open = null) =>
        new(new PointsEditor(hero, Data, readOnly), context ?? Free, preview, open);

    private static SpecPage Sentinel(int level, string? spec = null, string? fork = null, SpecContext? context = null, string? preview = null) =>
        Page(Hero("archer", level, spec, fork), context, preview);

    private static SpecState State(SpecPage page, string key) => page[key].State;

    // ---- The layout (§4) ----

    private static (float X, float Y, float W, float H)[] Rects()
    {
        var rects = new List<(float, float, float, float)>
        {
            (0, SpecTree.RootY, SpecTree.RootWidth, SpecTree.RootHeight),
            (0, SpecTree.ButtonY, SpecTree.RootWidth, SpecTree.ButtonHeight),
        };
        for (int b = 0; b < 3; b++)
        {
            rects.Add((SpecTree.BranchX, SpecTree.BranchY(b), SpecTree.TileWidth, SpecTree.TileHeight));
            for (int f = 0; f < 2; f++)
                rects.Add((SpecTree.ForkX, SpecTree.ForkY((b * 2) + f), SpecTree.TileWidth, SpecTree.TileHeight));
        }
        // The column 100 of the open branch (sprint 196, §14.2): two final classes or one evolution by lineage, at the same places whichever branch is open.
        foreach (float top in new[] { SpecTree.FinalY(0, 0), SpecTree.FinalY(0, 1), SpecTree.FinalY(1, 0), SpecTree.FinalY(1, 1) })
            rects.Add((SpecTree.FinalX, top, SpecTree.FinalWidth, SpecTree.TileHeight));
        return [.. rects];
    }

    [Fact]
    public void TheTree_HasNoOverlap_NoTargetUnder44_AndHoldsInItsRoom()
    {
        var rects = Rects();
        Assert.Equal(15, rects.Length);
        Assert.Equal([SpecTree.SoleY(0), SpecTree.SoleY(1)], [SpecTree.FinalY(0, 0), SpecTree.FinalY(1, 1)]);
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
    }

    [Fact]
    public void EachBranch_IsCentredOnItsTwoLineages_AndThePageMatchesTheWorkshop()
    {
        foreach (int b in new[] { 0, 1, 2 })
        {
            float top = SpecTree.ForkY(b * 2), bottom = SpecTree.ForkY((b * 2) + 1) + SpecTree.TileHeight;
            Assert.Equal((top + bottom) / 2, SpecTree.BranchY(b) + (SpecTree.TileHeight / 2));
        }
        Assert.Equal(SpecTree.Height, SpecTree.ForkY(5) + SpecTree.TileHeight);
        Assert.Equal(SpecTree.FriezeWidth, ElementsAtelier.PanelX);
        Assert.Equal(SpecTree.RootCentre, SpecTree.RootY + (SpecTree.RootHeight / 2));
    }

    [Theory]
    [InlineData("archer", "sightline", "marksman", "barrage")]
    [InlineData("warrior", "palisade", "rampart", "bastion")]
    [InlineData("mage", "archmage", "high-archmage", "patient")]
    public void TheNodes_FollowTheData_ElevenByClass_TheColumn100OfTheOpenBranchOnly(string cls, string first, string fork1, string fork2)
    {
        SpecPage page = Page(Hero(cls, 60));
        Assert.Equal(cls == "warrior" ? 13 : 11, page.Nodes.Count);
        Assert.Equal(first, page.OpenBranch);
        Assert.Equal([first, fork1, fork2], page.Nodes.Take(3).Select(n => n.Key));
        // The Warrior's lineages end in two final classes since sprint 197; the others keep their evolution at 100.
        Assert.Equal(cls == "warrior" ? 4 : 2, page.Last.Count);
        Assert.Equal(cls == "warrior" ? [fork1 + ":crusader", fork1 + ":sentry"] : [fork1 + "@100", fork2 + "@100"], page.Last.Take(2).Select(n => n.Key));
        Assert.Equal([30, 50, 50], page.Nodes.Take(3).Select(n => n.Level));
        Assert.All(page.Nodes, n => Assert.NotNull(n.Icon));
        Assert.Null(page.Find(fork1 + ":x"));
    }

    // ---- The states (§6, the seven situations of §7.1) ----

    [Fact]
    public void BelowThirty_AllIsLocked_NoChoiceWaits()
    {
        SpecPage page = Sentinel(20);
        Assert.All(page.Nodes, n => Assert.Equal(SpecState.Locked, n.State));
        Assert.Equal((0, 0), page.Choices);
        Assert.False(page.Pending);
    }

    [Fact]
    public void AtThirty_WithoutTheQuest_TheBranchesWait_TheFootSaysWhere()
    {
        var noQuest = new SpecContext(false, 0, true);
        SpecPage page = Sentinel(35, context: noQuest);
        Assert.Equal(SpecState.Waiting, State(page, "sightline"));
        Assert.Equal(SpecState.Locked, State(page, "marksman"));
        Assert.False(page.Pending);
        SpecFoot foot = page.Foot(page["sightline"], Fr);
        Assert.Equal(Fr["spec.quest"], foot.First);
        Assert.Null(foot.Action);
    }

    [Fact]
    public void QuestDone_AtThirtyToFortyNine_TheBranchesOpen_TheRestLocked()
    {
        SpecPage page = Sentinel(40);
        Assert.Equal(SpecState.Open, State(page, "ambush"));
        Assert.Equal(SpecState.Locked, State(page, "falconer"));
        Assert.Equal(SpecState.Locked, State(Page(Hero("archer", 40), open: "ambush"), "falconer@100"));
        Assert.True(page.Pending);
        Assert.Equal((0, 1), page.Choices);
    }

    [Fact]
    public void QuestDone_FromFifty_TheLineagesWait_AndALookOpensABranchs()
    {
        SpecPage page = Sentinel(60);
        Assert.Equal(SpecState.Open, State(page, "sightline"));
        Assert.Equal(SpecState.Waiting, State(page, "marksman"));
        Assert.Equal(SpecState.Locked, State(page, "marksman@100"));
        SpecPage looking = Sentinel(60, preview: "sightline");
        Assert.Equal(SpecState.Open, State(looking, "sightline"));
        Assert.Equal(SpecState.Open, State(looking, "marksman"));
        Assert.Equal(SpecState.Locked, State(looking, "marksman@100"));
        Assert.Equal(SpecState.Waiting, State(looking, "ambush"));
        Assert.Equal(SpecState.Waiting, State(looking, "falconer"));
        Assert.Equal(SpecState.Open, State(Sentinel(100, preview: "sightline"), "marksman@100"));
    }

    [Fact]
    public void ABranchWithoutALineage_LeavesTheOthersCrossed_ItsOwnLockedOrOpen()
    {
        SpecPage young = Sentinel(40, "sightline");
        Assert.Equal(SpecState.Chosen, State(young, "sightline"));
        Assert.Equal(SpecState.Locked, State(young, "marksman"));
        Assert.Equal(SpecState.Crossed, State(young, "ambush"));
        Assert.Equal(SpecState.Crossed, State(young, "falconer"));
        Assert.False(young.Pending);
        SpecPage old = Sentinel(55, "sightline");
        Assert.Equal(SpecState.Open, State(old, "marksman"));
        Assert.Equal(SpecState.Open, State(old, "barrage"));
        Assert.True(old.Pending);
        Assert.Equal((1, 2), old.Choices);
        Assert.Equal("marksman", old.Opening);
    }

    [Fact]
    public void ABranchAndALineage_LightTheirRoad_TheLastEvolutionToCome()
    {
        SpecPage page = Sentinel(62, "sightline", "marksman");
        Assert.Equal(SpecState.Chosen, State(page, "sightline"));
        Assert.Equal(SpecState.Chosen, State(page, "marksman"));
        Assert.Equal(SpecState.Ahead, State(page, "marksman@100"));
        Assert.Equal(SpecState.Crossed, State(page, "barrage"));
        Assert.Equal(SpecState.Crossed, State(page, "barrage@100"));
        Assert.Equal(SpecState.Crossed, State(page, "ambush"));
        Assert.Equal((2, 2), page.Choices);
        Assert.False(page.Pending);
        Assert.Equal("marksman", page.Opening);
        Assert.Equal("marksman@100", Sentinel(100, "sightline", "marksman").Opening);
        Assert.Equal(SpecState.Chosen, State(Sentinel(100, "sightline", "marksman"), "marksman@100"));
    }

    [Fact]
    public void ThePencil_MarksOnlyTheHeadsOfTheRoadsLeft()
    {
        SpecPage page = Sentinel(62, "sightline", "marksman");
        Assert.Equal(["barrage", "ambush", "trail"], page.Nodes.Where(n => n.Head).Select(n => n.Key));
        Assert.DoesNotContain(Sentinel(62).Nodes, n => n.Head);
    }

    // ---- The foot (§7.3) ----

    [Fact]
    public void AFirstChoice_IsFree_AndBelowFifty_HasItsButton()
    {
        SpecPage page = Sentinel(40);
        SpecFoot foot = page.Foot(page["ambush"], Fr);
        Assert.Equal(Fr["spec.free"], foot.First);
        Assert.Equal(Fr["spec.then-cost", SpecPage.Price(1200, Fr)], foot.Second);
        Assert.Equal((Fr["spec.take"], true, "ambush", (string?)null, 0L), (foot.Action!.Text, foot.Action.Enabled, foot.Action.Spec, foot.Action.Fork, foot.Action.Cost));
    }

    [Fact]
    public void FromFifty_ABranch_OnlyPointsToItsLineages_WhichCarryTheButton()
    {
        SpecPage page = Sentinel(60, preview: "ambush");
        SpecFoot branch = page.Foot(page["ambush"], Fr);
        Assert.Equal(Fr["spec.needs-fork"], branch.First);
        Assert.Null(branch.Action);
        SpecFoot fork = page.Foot(page["trapper"], Fr);
        Assert.Equal(Fr["spec.at-once"], fork.Second);
        Assert.Equal((Fr["spec.take.fork"], "ambush", "trapper"), (fork.Action!.Text, fork.Action.Spec, fork.Action.Fork));
    }

    [Fact]
    public void ACrossing_ShowsThePricesOfSpecsChangeCost_AtEachPlaceOfTheRoad()
    {
        Hero hero = Hero("archer", 62, "sightline", "marksman");
        SpecPage page = Page(hero);
        SpecFoot chosen = page.Foot(page["marksman"], Fr);
        Assert.Equal(Fr["spec.price.fork", "620"], chosen.First);
        Assert.Equal(Fr["spec.price.branch", "1 860"], chosen.Second);
        Assert.Null(chosen.Action);
        // The sister lineage.
        SpecFoot sister = page.Foot(page["barrage"], Fr);
        Assert.Equal(Fr["spec.rank.one"], sister.Second);
        Assert.Equal(Specs.ChangeCost(hero, "sightline", "barrage"), sister.Action!.Cost);
        Assert.Equal(Fr["spec.cross", "620"], sister.Action.Text);
        // Another branch: a lineage named, one price; the branch itself only points to it.
        SpecFoot other = page.Foot(page["falconer"], Fr);
        Assert.Equal(Specs.ChangeCost(hero, "ambush", "falconer"), other.Action!.Cost);
        Assert.Equal(Fr["spec.rank.many"], other.Second);
        SpecFoot head = page.Foot(page["ambush"], Fr);
        Assert.Equal(Fr["spec.needs-fork.paid"], head.First);
        Assert.Null(head.Action);
        // A last evolution not reached.
        SpecPage otherOpen = Page(hero, open: "ambush");
        SpecFoot later = otherOpen.Foot(otherOpen["falconer@100"], Fr);
        Assert.Equal((Fr["spec.opens", "100"], Fr["spec.pick-at-50"]), (later.First, later.Second));
        Assert.Null(later.Action);
        // To come on its road.
        SpecFoot ahead = page.Foot(page["marksman@100"], Fr);
        Assert.Equal((Fr["spec.ahead", 38], Fr["spec.ahead.alone"]), (ahead.First, ahead.Second));
        Assert.Equal(Fr["spec.ahead.one"], Sentinel(99, "sightline", "marksman").Foot(Sentinel(99, "sightline", "marksman")["marksman@100"], Fr).First);
    }

    [Fact]
    public void BelowFifty_AnotherBranch_HasItsButton_AtThirtyTimesTheLevel()
    {
        SpecPage page = Sentinel(35, "sightline");
        SpecFoot foot = page.Foot(page["trail"], Fr);
        Assert.Equal(Fr["spec.price.branch", "1 050"], foot.First);
        Assert.Equal(1050, foot.Action!.Cost);
        Assert.Equal(Fr["spec.opens", "50"], page.Foot(page["leaper"], Fr).First);
    }

    [Fact]
    public void TheButton_SaysWhatStopsIt_ByPriority()
    {
        Hero hero = Hero("archer", 62, "sightline", "marksman");
        static SpecAction Of(SpecPage p) => p.Foot(p["barrage"], Fr).Action!;
        SpecAction away = Of(Page(hero, new SpecContext(true, 100_000, InVillage: false)));
        Assert.Equal((Fr["spec.need.village"], false), (away.Text, away.Enabled));
        SpecAction poor = Of(Page(hero, new SpecContext(true, 500, InVillage: true)));
        Assert.Equal((Fr["spec.need.shards", "120"], false, Fr["spec.need.shards.tip", "620"]), (poor.Text, poor.Enabled, poor.Tip));
        // A draft first: the server's answer would rebuild it.
        var editor = new PointsEditor(hero with { Stats = null }, Data);
        editor.Add(Characteristic.Earth, 1);
        SpecPage drafted = new(editor, new SpecContext(true, 0, false));
        SpecAction draft = drafted.Foot(drafted["barrage"], Fr).Action!;
        Assert.Equal((Fr["spec.need.save"], false), (draft.Text, draft.Enabled));
        // The first choice asks neither a village nor Éclats, but a draft stops it too.
        SpecPage poorFirst = Sentinel(40, context: new SpecContext(true, 0, false));
        Assert.True(poorFirst.Foot(poorFirst["trail"], Fr).Action!.Enabled);
        var firstEditor = new PointsEditor(Hero("archer", 40), Data);
        firstEditor.Add(Characteristic.Earth, 1);
        SpecPage firstDraft = new(firstEditor, Free);
        Assert.Equal(Fr["spec.need.save"], firstDraft.Foot(firstDraft["trail"], Fr).Action!.Text);
    }

    [Fact]
    public void BeforeItsSprint_TheMage_ReadsItsTree_ButIsToldSoon()
    {
        SpecPage page = Page(Hero("mage", 40));
        Assert.False(SpecPage.Playable(Data.Specs["archmage"]));
        Assert.True(SpecPage.Playable(Data.Specs["palisade"]));
        Assert.True(SpecPage.Playable(Data.Specs["sightline"]));
        SpecAction action = page.Foot(page["archmage"], Fr).Action!;
        Assert.Equal((Fr["spec.soon"], false), (action.Text, action.Enabled));
        Assert.False(page.Pending);
    }

    [Theory]
    [InlineData("fr", 1860, "1 860")]
    [InlineData("en", 1860, "1,860")]
    [InlineData("fr", 620, "620")]
    public void ThePrices_AreWrittenAsTheGameWritesThem(string lang, long amount, string written) =>
        Assert.Equal(written, SpecPage.Price(amount, new Texts(lang)));

    // ---- The links (§5.3) ----

    [Fact]
    public void TheLivingWood_CoversExactlyTheRoad()
    {
        SpecPage page = Sentinel(62, "sightline", "marksman");
        Assert.Equal(["sightline", "marksman"], page.Nodes.Where(n => page.WoodTo(n) == SpecWood.Alive).Select(n => n.Key));
        Assert.Equal(["marksman@100"], page.Nodes.Where(n => page.WoodTo(n) == SpecWood.Bare).Select(n => n.Key));
        Assert.DoesNotContain(page.Nodes, n => page.WoodTo(n) == SpecWood.Open);
        SpecPage young = Sentinel(40, "sightline");
        Assert.Equal(4, young.Nodes.Count(n => young.WoodTo(n) == SpecWood.Bare));
        SpecPage free = Sentinel(40);
        Assert.Equal(["sightline", "ambush", "trail"], free.Nodes.Where(n => free.WoodTo(n) == SpecWood.Open).Select(n => n.Key));
        Assert.Equal(["sightline", "marksman", "marksman@100"], SpecPage.RoadTo(page["marksman@100"]));
    }

    [Fact]
    public void TheLinks_JoinTheColumns()
    {
        Assert.Equal((SpecTree.RootWidth, SpecTree.RootCentre), SpecTree.ToBranch(1)[0]);
        Assert.Equal(SpecTree.BranchX, SpecTree.ToBranch(2)[^1].X);
        Assert.Equal((SpecTree.ForkX, SpecTree.ForkY(3) + (SpecTree.TileHeight / 2)), SpecTree.ToFork(1, 1)[^1]);
        Assert.Equal((SpecTree.FinalX, SpecTree.SoleY(1) + (SpecTree.TileHeight / 2)), SpecTree.ToFinal(2, 1, SpecTree.SoleY(1))[^1]);
        Assert.Equal(SpecTree.ForkX + SpecTree.TileWidth, SpecTree.ToFinal(0, 0, SpecTree.FinalY(0, 0))[0].X);
    }

    // ---- The confirmation (§5.6) ----

    [Fact]
    public void AFirstRoad_GivesAllThatItsLevelOpens_Free()
    {
        SpecConfirm late = Sentinel(100).Confirm("sightline", "marksman", Fr);
        Assert.Equal(3, late.Gifts.Count);
        Assert.Equal(Fr["spec.confirm.first"], late.Title);
        Assert.Equal(Fr["spec.confirm.free"], late.Price);
        Assert.True(late.First);
        Assert.Contains("Tir visé", late.Gifts[0].Line, StringComparison.Ordinal);
        Assert.Contains("Coup au but", late.Gifts[1].Line, StringComparison.Ordinal);
        Assert.Contains(Fr["spec.confirm.no-spell"], late.Gifts[2].Line, StringComparison.Ordinal);
        Assert.Equal(Fr["spec.confirm.later", "1 000", "3 000"], late.Later);
        SpecConfirm young = Sentinel(40).Confirm("trail", null, Fr);
        Assert.Single(young.Gifts);
        Assert.Equal(Fr["spec.confirm.later.branch", "1 200"], young.Later);
        // The lineage due brings its own alone.
        Assert.Single(Sentinel(55, "sightline").Confirm("sightline", "barrage", Fr).Gifts);
    }

    [Fact]
    public void ACrossingOut_SaysWhatLeaves_WhichRanksFollow_AndThePrice()
    {
        Hero hero = Hero("archer", 62, "sightline", "marksman") with { Ranks = new Dictionary<string, int> { ["path-bullseye"] = 3, ["path-aimed-shot"] = 2 } };
        SpecConfirm fork = Page(hero).Confirm("sightline", "barrage", Fr);
        Assert.Equal(Fr["spec.confirm.cross", "Longue-vue"], fork.Title);
        Assert.Null(fork.Leaves);
        Assert.Equal([Fr["spec.confirm.rank", "Coup au but", 3, "Tir de barrage"]], fork.Ranks);
        Assert.Equal(Fr["spec.confirm.cost", "620", "99 380"], fork.Price);
        Assert.Equal(Fr["spec.confirm.keep"], fork.No);
        Assert.False(fork.First);
        SpecConfirm branch = Page(hero).Confirm("ambush", "falconer", Fr);
        Assert.Equal(Fr["spec.confirm.cross", "Mire"], branch.Title);
        Assert.Equal(Fr["spec.confirm.leave", "Mire, Longue-vue"], branch.Leaves);
        Assert.Equal(2, branch.Ranks.Count);
    }

    [Fact]
    public void AHandMadeDeck_IsToldItsNewSpellsWait_ForOneOrSeveral()
    {
        Hero hero = Hero("archer", 40) with { Deck = ["stone-arrow"] };
        Assert.Equal(Fr["spec.deck.many"], Page(hero).Confirm("trail", null, Fr).Deck);
        Assert.Equal(Fr["spec.deck"], Page(Hero("archer", 55, "trail") with { Deck = ["stone-arrow", "path-vault", "path-heel-kick"] }).Confirm("trail", "leaper", Fr).Deck);
        Assert.Null(Page(Hero("archer", 40)).Confirm("trail", null, Fr).Deck);
    }

    [Fact]
    public void TheCharacter_GivesTheContext()
    {
        var c = new CharacterSummary(Guid.NewGuid(), "Ondine", "female-a", "archer", 0, DateTimeOffset.UnixEpoch, new Place("clairval", 1, 1), Quests: [Specs.Quest], Shards: 42);
        SpecContext context = SpecContext.Of(c);
        Assert.True(context.QuestDone);
        Assert.Equal(42, context.Shards);
        Assert.False(SpecContext.Of(c with { Quests = [] }).QuestDone);
    }

    // ---- The book (§v4.1) ----

    [Fact]
    public void TheBook_ShowsThePathsSpells_AsTheyFight()
    {
        Hero hero = Hero("archer", 100, "sightline", "barrage") with { Stats = new Characteristics(Earth: 90) };
        var book = new SpellBook(new PointsEditor(hero, Data));
        BookSpell barrage = book.Spells.Single(b => b.Spell.Id == "path-barrage");
        Assert.Equal(Element.Earth, barrage.Element);
        Assert.Equal(1, barrage.Spell.Cooldown);
        Assert.Equal(27, book.Spells.Count);
        Assert.DoesNotContain(new SpellBook(new PointsEditor(Hero("archer", 100), Data)).Spells, b => b.Spell.Id.StartsWith("path-", StringComparison.Ordinal));
    }

    // The final classes' words (sprint 196, maquette §14.10).
    private static readonly string[] FinalKeys = ["spec.tier.100.finals", "spec.tier.100.tip", "spec.final.pick", "spec.final.ahead", "spec.final.same", "spec.final.with-fork",
        "spec.final.with-branch", "spec.final.due", "spec.take.final", "spec.confirm.final", "spec.confirm.final.yes", "spec.confirm.final.keep", "spec.confirm.final.same",
        "spec.confirm.final.after", "spec.summary.final", "spec.summary.final.due"];

    private static readonly string[] FinalFormats = ["spec.final.then-cost", "spec.price.final", "spec.confirm.signature", "spec.confirm.later.final"];

    [Fact]
    public void EveryKeyOfThePage_ExistsInBothLanguages()
    {
        var en = new Texts("en");
        foreach (string key in FinalKeys.Concat(Enum.GetNames<FinalTemplate>().Select(t => $"spec.template.{t}")))
        {
            Assert.NotEqual(key, Fr[key]);
            Assert.NotEqual(key, en[key]);
            Assert.NotEqual(Fr[key], en[key]);
        }
        foreach (string key in FinalFormats)
            Assert.NotEqual(Fr[key, 1], en[key, 1]);
        Assert.NotEqual(Fr["spec.confirm.later.all", 1, 2, 3], en["spec.confirm.later.all", 1, 2, 3]);
        foreach (string key in new[] { "sheet.tab.spec", "spec.hint", "spec.quest", "spec.splits", "spec.deck", "spec.deck.many", "spec.branch-first", "spec.open-book", "spec.refused", "error.NoSummon", "stat.Anchored", "stat.Mp.all" })
        {
            Assert.NotEqual(key, Fr[key]);
            Assert.NotEqual(key, en[key]);
            Assert.NotEqual(Fr[key], en[key]);
        }
        foreach (string key in new[] { "spec.level", "stat.Feint", "sd.needs", "sd.aims" })
            Assert.NotEqual(Fr[key, 1], en[key, 1]);
    }
}
