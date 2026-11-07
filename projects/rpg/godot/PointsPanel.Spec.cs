using Godot;
using Rpg.Client;
using Rpg.Core;

namespace Rpg.Desktop;

/// <summary>
/// The Specialisation tab (sprint 191, D95, docs/maquette-specialisation.md): the class, its three branches, their six lineages and their last evolutions, read from left to
/// right, under the frieze of tiers, and the panel of the tile shown. It only draws <see cref="SpecPage"/>: a choice is written at once, through <see cref="SpecChosen"/>.
/// </summary>
public partial class PointsPanel
{
    private sealed record SpecTile(Button Button, SpecSigil Sigil, Label Title, Panel Breath);

    /// <summary>The root's key: the summary of the hero's road.</summary>
    private const string RootKey = "root";

    private readonly Dictionary<string, SpecTile> _specTiles = [];
    private TierFrieze _specFrieze = null!;
    private SpecWoodView _specWood = null!;
    private Button _specRoot = null!;
    private SpecSigil _rootSigil = null!;
    private Label _rootName = null!;
    private PanelContainer _specCard = null!;
    private SpecSigil _cardSigil = null!;
    private Label _cardTitle = null!, _cardSub = null!, _cardSignature = null!, _cardText = null!, _cardLore = null!, _cardFirst = null!, _cardSecond = null!;
    private Button _cardAction = null!;
    private string? _specPicked, _specHovered, _specPreview, _specOpen;
    private Control _specTree = null!;
    private readonly List<string> _lastKeys = [];
    private SpecAction? _shownAction;
    private bool _offerBook;
    private Control? _confirm;
    private Button? _confirmYes;

    /// <summary>What the page reads of the character: the quest, the Éclats, the place. The server checks it all again.</summary>
    public SpecContext SpecContext { get; set; } = new(QuestDone: false, Shards: 0, InVillage: false);

    /// <summary>A road confirmed: its branch, its lineage and its final class; the game sends it and answers with <see cref="SpecSaved"/> or <see cref="SpecRefused"/>.</summary>
    public event Action<string, string?, string?>? SpecChosen;

    private SpecPage SpecView() => new(Editor, SpecContext, _specPreview, _specOpen);

    private Control BuildSpec()
    {
        var page = new Control { CustomMinimumSize = new Vector2(ElementsAtelier.Width, SpecTree.TreeY + SpecTree.Height) };
        _specFrieze = new TierFrieze { Size = new Vector2(SpecTree.FriezeWidth, ElementsAtelier.FriezeHeight), CustomMinimumSize = new Vector2(SpecTree.FriezeWidth, ElementsAtelier.FriezeHeight) };
        page.AddChild(_specFrieze);
        var tree = new Control { Position = new Vector2(0, SpecTree.TreeY), Size = new Vector2(SpecTree.Width, SpecTree.Height) };
        page.AddChild(tree);
        _specTree = tree;
        _specWood = new SpecWoodView { Panel = this, Size = tree.Size, MouseFilter = Control.MouseFilterEnum.Ignore };
        tree.AddChild(_specWood);

        _specRoot = new Button { Position = new Vector2(0, SpecTree.RootY), Size = new Vector2(SpecTree.RootWidth, SpecTree.RootHeight), ToggleMode = true, ThemeTypeVariation = OsmoseTheme.CompoundOpen };
        _specRoot.Pressed += () => PickSpec(RootKey);
        HoverSpec(_specRoot, RootKey);
        tree.AddChild(_specRoot);
        _rootSigil = new SpecSigil { Position = new Vector2(20, 8), Size = new Vector2(52, 52), MouseFilter = Control.MouseFilterEnum.Ignore };
        _specRoot.AddChild(_rootSigil);
        _rootName = new Label { Position = new Vector2(2, 64), Size = new Vector2(88, 24), HorizontalAlignment = HorizontalAlignment.Center, ClipText = true, MouseFilter = Control.MouseFilterEnum.Ignore };
        _rootName.AddThemeFontSizeOverride("font_size", 14);
        _rootName.AddThemeFontOverride("font", OsmoseTheme.Bold());
        _specRoot.AddChild(_rootName);

        // The tree of the talents stays a key and a button away (Charles, 3 November, Q1).
        _openTree = new Button { Position = new Vector2(0, SpecTree.ButtonY), Size = new Vector2(SpecTree.RootWidth, SpecTree.ButtonHeight), AutowrapMode = TextServer.AutowrapMode.WordSmart };
        _openTree.AddThemeFontSizeOverride("font_size", 14);
        _openTree.Pressed += () => TreePressed?.Invoke();
        tree.AddChild(_openTree);
        // Side margins of 4 (§5.2): at the theme's, its words took three lines and left the button.
        foreach (string state in new[] { "normal", "hover", "pressed", "hover_pressed", "disabled" })
        {
            var box = (StyleBox)_openTree.GetThemeStylebox(state).Duplicate();
            box.ContentMarginLeft = box.ContentMarginRight = 4;
            box.ContentMarginTop = box.ContentMarginBottom = 2;
            _openTree.AddThemeStyleboxOverride(state, box);
        }

        // By the class's branches of the data: the same three once built; the column 100 follows the open branch (§14.1), at each refresh.
        var view = new SpecPage(Editor, SpecContext);
        IReadOnlyList<Specialisation> branches = view.Branches;
        for (int b = 0; b < branches.Count; b++)
        {
            Specialisation branch = branches[b];
            AddSpecTile(tree, branch.Id, new Vector2(SpecTree.BranchX, SpecTree.BranchY(b)));
            for (int f = 0; f < branch.Forks.Count; f++)
                AddSpecTile(tree, branch.Forks[f].Id, new Vector2(SpecTree.ForkX, SpecTree.ForkY((b * 2) + f)));
        }
        page.AddChild(BuildSpecCard());
        return page;
    }

    private void AddSpecTile(Control tree, string key, Vector2 at, float width = SpecTree.TileWidth)
    {
        var tile = new Button { Position = at, Size = new Vector2(width, SpecTree.TileHeight), ToggleMode = true, ThemeTypeVariation = OsmoseTheme.CompoundOpen };
        tile.Pressed += () => PickSpec(key);
        HoverSpec(tile, key);
        tree.AddChild(tile);
        var sigil = new SpecSigil { Position = new Vector2(8, 8), Size = new Vector2(36, 36), MouseFilter = Control.MouseFilterEnum.Ignore };
        tile.AddChild(sigil);
        var title = new Label { Position = new Vector2(52, 0), Size = new Vector2(width - 60, SpecTree.TileHeight), VerticalAlignment = VerticalAlignment.Center, AutowrapMode = TextServer.AutowrapMode.WordSmart, MouseFilter = Control.MouseFilterEnum.Ignore };
        title.AddThemeFontSizeOverride("font_size", 14);
        title.AddThemeFontOverride("font", OsmoseTheme.Bold());
        title.AddThemeConstantOverride("line_spacing", 0);
        tile.AddChild(title);
        // The lineages to choose breathe slowly, their edge from wood to pistachio (§6); no blinking.
        var breath = new Panel { Size = tile.Size, MouseFilter = Control.MouseFilterEnum.Ignore, Visible = false, Modulate = new Color(1, 1, 1, 0) };
        breath.AddThemeStyleboxOverride("panel", OsmoseTheme.Flat("#00000000", UiStyle.Pistachio, 2, 8));
        tile.AddChild(breath);
        _specTiles[key] = new SpecTile(tile, sigil, title, breath);
    }

    // The column 100 (§14.2): the open branch's two final classes by lineage, or its one evolution, rebuilt when the branch changes.
    private void SyncLastColumn(SpecPage page)
    {
        IReadOnlyList<SpecNode> last = page.Last;
        if (last.Select(n => n.Key).SequenceEqual(_lastKeys))
            return;
        foreach (string key in _lastKeys)
        {
            _specTiles[key].Button.QueueFree();
            _specTiles.Remove(key);
        }
        _lastKeys.Clear();
        Specialisation branch = Editor.Data.Specs[page.OpenBranch];
        foreach (SpecNode node in last)
        {
            AddSpecTile(_specTree, node.Key, new Vector2(SpecTree.FinalX, SpecTree.LastY(branch, node)), SpecTree.FinalWidth);
            _lastKeys.Add(node.Key);
        }
    }

    private void HoverSpec(Control control, string key)
    {
        control.MouseEntered += () => SetSpecHovered(key);
        control.MouseExited += () =>
        {
            if (_specHovered == key)
                SetSpecHovered(null);
        };
    }

    private void SetSpecHovered(string? key)
    {
        _specHovered = key;
        RefreshSpecCard(SpecView());
        _specWood.QueueRedraw();
    }

    private PanelContainer BuildSpecCard()
    {
        _specCard = new PanelContainer
        {
            Position = new Vector2(ElementsAtelier.PanelX, 0),
            Size = new Vector2(ElementsAtelier.PanelWidth, SpecTree.TreeY + SpecTree.Height),
            CustomMinimumSize = new Vector2(ElementsAtelier.PanelWidth, 0),
            ThemeTypeVariation = OsmoseTheme.DetailPanel,
        };
        var column = new VBoxContainer();
        column.AddThemeConstantOverride("separation", 4);
        _specCard.AddChild(column);
        var head = new HBoxContainer();
        head.AddThemeConstantOverride("separation", 10);
        column.AddChild(head);
        _cardSigil = new SpecSigil { CustomMinimumSize = new Vector2(52, 52), MouseFilter = Control.MouseFilterEnum.Ignore };
        head.AddChild(_cardSigil);
        var who = new VBoxContainer { SizeFlagsHorizontal = Control.SizeFlags.ExpandFill };
        who.AddThemeConstantOverride("separation", 0);
        head.AddChild(who);
        _cardTitle = new Label { ClipText = true };
        _cardTitle.AddThemeFontSizeOverride("font_size", 22);
        _cardTitle.AddThemeFontOverride("font", OsmoseTheme.Bold());
        _cardTitle.AddThemeColorOverride("font_color", new Color(UiStyle.TextStrong));
        who.AddChild(_cardTitle);
        _cardTitle.Resized += FitCardTitle;
        _cardSub = SpecLine(UiStyle.TextDim);
        _cardSub.AutowrapMode = TextServer.AutowrapMode.Off;
        _cardSub.ClipText = true;
        who.AddChild(_cardSub);
        column.AddChild(SpecRule());
        // A final class's signature, before its words: what tells two sisters apart (§14.6).
        _cardSignature = new Label { ThemeTypeVariation = OsmoseTheme.SectionLabel, Visible = false };
        column.AddChild(_cardSignature);
        _cardText = SpecLine(UiStyle.Text);
        column.AddChild(_cardText);
        _cardLore = SpecLine(UiStyle.TextDim);
        column.AddChild(_cardLore);
        column.AddChild(new Control { SizeFlagsVertical = Control.SizeFlags.ExpandFill });
        column.AddChild(SpecRule());
        _cardFirst = SpecLine(UiStyle.Text);
        column.AddChild(_cardFirst);
        _cardSecond = SpecLine(UiStyle.TextDim);
        column.AddChild(_cardSecond);
        _cardAction = new Button { CustomMinimumSize = new Vector2(0, 44), ClipText = true };
        _cardAction.Pressed += PressSpecAction;
        column.AddChild(_cardAction);
        return _specCard;
    }

    private static ColorRect SpecRule() => new() { Color = new Color("#ffffff0f"), CustomMinimumSize = new Vector2(0, 1), MouseFilter = Control.MouseFilterEnum.Ignore };

    private static Label SpecLine(string colour)
    {
        var label = new Label { AutowrapMode = TextServer.AutowrapMode.WordSmart };
        label.AddThemeFontSizeOverride("font_size", 14);
        label.AddThemeColorOverride("font_color", new Color(colour));
        return label;
    }

    /// <summary>The Specialisation tab, as the player opens it or the self-test does.</summary>
    public void ShowSpec()
    {
        _statsPage.Visible = _spellsPage.Visible = _elementsPage.Visible = false;
        _specPage.Visible = true;
        _statsTab.SetPressedNoSignal(false);
        _spellsTab.SetPressedNoSignal(false);
        _elementsTab.SetPressedNoSignal(false);
        _specTab.SetPressedNoSignal(true);
        _specPreview = null;
        _specOpen = null;
        _offerBook = false;
        _specPicked = SpecView().Opening;
        Refresh();
        _message.RemoveThemeColorOverride("font_color");
        _message.Text = SpecHint();
    }

    private string SpecHint()
    {
        Hero hero = Editor.Hero;
        return Editor.ReadOnly ? Texts["elements.hint.fight"]
            : hero.Level < Specs.Opens ? Texts["spec.hint.closed", Specs.Opens]
            : !SpecContext.QuestDone && hero.Spec is null ? Texts["spec.quest"]
            : hero.Spec is not null && hero.Fork is null && hero.Level >= Specs.Forks ? Texts["spec.splits"]
            : SpecView().FinalDue ? Texts["spec.final.due"]
            : Texts["spec.hint"];
    }

    /// <summary>
    /// Shows a tile in the panel; from 50, a branch looked at opens its lineages (§7.2) before its road is written. Its branch becomes the open one, whose column 100 shows
    /// (§14.1); the root brings back the hero's.
    /// </summary>
    public void PickSpec(string key)
    {
        Hero hero = Editor.Hero;
        SpecNode? picked = key == RootKey ? null : SpecView().Find(key);
        if (key != RootKey && picked is null)
            return;
        _specPicked = key;
        _offerBook = false;
        _specOpen = picked?.Spec ?? hero.Spec ?? _specOpen;
        if (key == RootKey)
        {
            _specPreview = null;
        }
        else if (hero.Level >= Specs.Forks && !Editor.ReadOnly)
        {
            SpecNode node = picked!;
            // A branch: the look it opens. A lineage of the branch looked at keeps it; of another, the look follows.
            if (node.Spec != hero.Spec && node.Fork is null)
                _specPreview = _specPreview == node.Spec ? null : node.Spec;
            else if (node.Spec == hero.Spec && node.Fork is null)
                _specPreview = null;
            else if (node.Spec != hero.Spec && _specPreview != node.Spec && hero.Spec is not null)
                _specPreview = node.Spec;
        }
        RefreshSpec();
    }

    private void RefreshSpec()
    {
        if (_specFrieze is null)
            return;
        SpecPage page = SpecView();
        string cls = Editor.Data.Class(Editor.Hero.Class)?.Name.In(Texts.Lang) ?? "";
        _specFrieze.Set(page, cls, Texts);
        _openTree.Text = Texts["tree.open"];
        _rootName.Text = cls;
        _rootSigil.Paint(RootIcon(), new SpecLook(new Color(Editor.Hero.Spec is null ? ElementStyle.WoodLight : UiStyle.Pistachio), Editor.Hero.Spec is null ? 2 : 3, Halo: Editor.Hero.Spec is null ? default : new Color(UiStyle.Pistachio)));
        _specRoot.ThemeTypeVariation = Editor.Hero.Spec is null ? OsmoseTheme.CompoundOpen : OsmoseTheme.CompoundCreated;
        _specRoot.SetPressedNoSignal(_specPicked == RootKey);
        SyncLastColumn(page);
        foreach (SpecNode node in page.Nodes)
            RefreshSpecTile(node);
        _specWood.QueueRedraw();
        RefreshSpecCard(page);
        // The point on the tab (§5.7).
        _specDot ??= AddSpecDot();
        _specDot.Visible = page.Pending;
    }

    private ColorRect? _specDot;

    private ColorRect AddSpecDot()
    {
        var dot = new ColorRect { Size = new Vector2(8, 8), Position = new Vector2(_specTab.Size.X - 14, 6), Color = new Color(UiStyle.Pistachio), MouseFilter = Control.MouseFilterEnum.Ignore };
        _specTab.AddChild(dot);
        _specTab.Resized += () => dot.Position = new Vector2(_specTab.Size.X - 14, 6);
        return dot;
    }

    private Texture2D RootIcon() => GD.Load<Texture2D>($"res://assets/icons/relief/class-{Editor.Hero.Class}.svg");

    private static Texture2D? SpecIcon(string? icon, bool grey) => icon is null ? null : GD.Load<Texture2D>($"res://assets/icons/relief/{icon}{(grey ? "-locked" : "")}.svg");

    /// <summary>A tile's seal by its state (§5.4).</summary>
    private static SpecLook LookOf(SpecNode node) => node.State switch
    {
        SpecState.Open => new SpecLook(new Color(ElementStyle.WoodLight), 2),
        SpecState.Chosen when node.Level == 100 => new SpecLook(new Color(UiStyle.Gold), 3, Halo: new Color(UiStyle.Gold), Check: true),
        SpecState.Chosen => new SpecLook(new Color(UiStyle.Pistachio), 3, Halo: new Color(UiStyle.Pistachio), Check: true),
        SpecState.Ahead => new SpecLook(new Color(ElementStyle.WoodLight), 2, Grey: true, Padlock: true),
        SpecState.Locked => new SpecLook(new Color(ElementStyle.OutOfLevel), 2, Grey: true, Padlock: true),
        SpecState.Crossed => new SpecLook(new Color(ElementStyle.OutOfLevel), 2, Grey: true, Stroke: node.Head),
        _ => new SpecLook(new Color(ElementStyle.OutOfLevel), 2, Grey: true),
    };

    private void RefreshSpecTile(SpecNode node)
    {
        SpecTile tile = _specTiles[node.Key];
        tile.Button.ThemeTypeVariation = node.State switch
        {
            SpecState.Open => OsmoseTheme.CompoundOpen,
            SpecState.Chosen => OsmoseTheme.CompoundCreated,
            SpecState.Crossed => OsmoseTheme.CompoundBlocked,
            SpecState.Ahead => OsmoseTheme.SpecAhead,
            _ => OsmoseTheme.CompoundLocked,
        };
        tile.Button.SetPressedNoSignal(_specPicked == node.Key);
        tile.Title.Text = node.Fork is null ? Editor.Data.Specs[node.Spec].Name.In(Texts.Lang) : node.Tier.Title.In(Texts.Lang);
        tile.Title.AddThemeColorOverride("font_color", new Color(node.State switch
        {
            SpecState.Open => UiStyle.TextStrong,
            SpecState.Chosen => "#ffffff",
            SpecState.Crossed or SpecState.Ahead => UiStyle.TextDim,
            _ => ElementStyle.Locked,
        }));
        SpecLook look = LookOf(node);
        tile.Sigil.Paint(SpecIcon(node.Icon, look.Grey), look);
        // The lineages due breathe, then the final classes due (§14.5); a final class taken directly from a branch does not.
        bool breathes = !Editor.ReadOnly && node.State == SpecState.Open && Editor.Hero.Spec == node.Spec
            && (node.Final is null ? node.Fork is not null && node.Level == Specs.Forks && Editor.Hero.Fork is null : Editor.Hero.Fork == node.Fork && Editor.Hero.Final is null);
        if (breathes && !tile.Breath.Visible)
        {
            tile.Breath.Visible = true;
            Tween tween = tile.Breath.CreateTween().SetLoops();
            tween.TweenProperty(tile.Breath, "modulate:a", 1f, 1.6).SetTrans(Tween.TransitionType.Sine);
            tween.TweenProperty(tile.Breath, "modulate:a", 0f, 1.6).SetTrans(Tween.TransitionType.Sine);
        }
        else if (!breathes)
        {
            tile.Breath.Visible = false;
        }
    }

    private void RefreshSpecCard(SpecPage page)
    {
        string shown = _specHovered ?? _specPicked ?? page.Opening;
        if (shown == RootKey)
        {
            RefreshSpecRoot(page);
            return;
        }
        // A tile of a column 100 left behind: the opening, else the open branch.
        SpecNode node = page.Find(shown) ?? page.Find(page.Opening) ?? page[page.OpenBranch];
        Specialisation branch = Editor.Data.Specs[node.Spec];
        SpecLook look = LookOf(node);
        _cardSignature.Visible = node.Template is not null;
        _cardSignature.Text = node.Template is FinalTemplate template ? Texts["spec.signature", SpecPage.TemplateName(template, Texts).ToUpper(System.Globalization.CultureInfo.GetCultureInfo(Texts.Lang))] : "";
        _cardSigil.Paint(SpecIcon(node.Icon, look.Grey), look);
        string level = Texts["spec.level", node.Level], lang = Texts.Lang;
        if (node.Fork is null)
        {
            SetCardTitle(branch.Name.In(lang));
            string title = node.Tier.Title.In(lang);
            _cardSub.Text = $"{level} · {(title == branch.Name.In(lang) ? Editor.Data.Class(branch.Class)?.Name.In(lang) : title)}";
        }
        else if (node.Final is not null)
        {
            // Under a final class, the lineage it ends (§14.6).
            SetCardTitle(node.Tier.Title.In(lang));
            _cardSub.Text = $"{level} · {branch.ForkOf(node.Fork)?.Tiers[0].Title.In(lang)}";
        }
        else
        {
            SetCardTitle(node.Tier.Title.In(lang));
            _cardSub.Text = $"{level} · {branch.Name.In(lang)}";
        }
        _cardText.Text = node.Tier.Description.In(lang);
        _cardLore.Text = node.Lore is null ? "" : lang == "fr" ? $"« {node.Lore.In(lang)} »" : $"“{node.Lore.In(lang)}”";
        SpecFoot foot = page.Foot(node, Texts);
        ShowSpecFoot(foot.First, foot.Second, foot.Action);
    }

    private void RefreshSpecRoot(SpecPage page)
    {
        Hero hero = Editor.Hero;
        string lang = Texts.Lang, cls = Editor.Data.Class(hero.Class)?.Name.In(lang) ?? "";
        _cardSigil.Paint(RootIcon(), new SpecLook(new Color(hero.Spec is null ? ElementStyle.WoodLight : UiStyle.Pistachio), 3));
        SetCardTitle(Specs.TitleOf(hero, Editor.Data)?.In(lang) ?? cls);
        _cardSub.Text = Texts["sheet.level", hero.Level, cls];
        _cardLore.Text = "";
        _cardSignature.Visible = false;
        if (hero.Spec is null || !Editor.Data.Specs.TryGetValue(hero.Spec, out Specialisation? branch))
        {
            _cardText.Text = Texts["spec.summary.none"];
            ShowSpecFoot("", "", null);
            return;
        }
        var road = new List<string> { Texts["spec.summary.road"], branch.Name.In(lang) };
        if (branch.ForkOf(hero.Fork) is SpecFork fork)
        {
            foreach (SpecTier tier in fork.Tiers)
                road.Add(tier.Level <= hero.Level ? tier.Title.In(lang) : Texts["spec.summary.later", tier.Title.In(lang), tier.Level]);
            // Its third line, on a lineage that ends in final classes (§14.6).
            if (fork.Finals is { Count: > 0 })
            {
                road.Add(hero.Level < Specs.Finals ? Texts["spec.summary.later", Texts["spec.summary.final"], Specs.Finals]
                    : fork.FinalOf(hero.Final) is SpecFinal final ? final.Tier.Title.In(lang) : Texts["spec.summary.final.due"]);
            }
        }
        _cardText.Text = string.Join("\n", road);
        SpecFoot foot = page.Foot(page.Find(page.Opening) ?? page[page.OpenBranch], Texts);
        ShowSpecFoot(foot.First, foot.Second, null);
    }

    private void ShowSpecFoot(string first, string second, SpecAction? action)
    {
        _cardFirst.Text = first;
        _cardSecond.Text = second;
        _cardSecond.Visible = second.Length > 0;
        _shownAction = action;
        bool book = _offerBook && action is null;
        _cardAction.Visible = !Editor.ReadOnly && (action is not null || book);
        _cardAction.Text = book ? Texts["spec.open-book"] : action?.Text ?? "";
        _cardAction.Disabled = !book && action is { Enabled: false };
        _cardAction.TooltipText = book ? "" : action?.Tip ?? "";
        _cardAction.ThemeTypeVariation = action is { First: true, Enabled: true } ? OsmoseTheme.Primary : "";
        if (action is { First: false, Enabled: true })
            _cardAction.AddThemeColorOverride("font_color", new Color(UiStyle.Cost));
        else
            _cardAction.RemoveThemeColorOverride("font_color");
    }

    private void PressSpecAction()
    {
        if (_offerBook && _shownAction is null)
        {
            _offerBook = false;
            ShowTab(spells: true);
            return;
        }
        if (_shownAction is { Enabled: true } action)
            OpenConfirm(action);
    }

    /// <summary>The confirmation (§5.6): over the whole window, Escape or its first button to refuse.</summary>
    private void OpenConfirm(SpecAction action)
    {
        SpecConfirm words = SpecView().Confirm(action.Spec, action.Fork, action.Final, Texts);
        var veil = new ColorRect { Color = new Color(0, 0, 0, 0.5f), Position = Window.Position, Size = Window.Size, MouseFilter = Control.MouseFilterEnum.Stop };
        AddChild(veil);
        _confirm = veil;
        // A centre container sizes it to what it holds: a free panel keeps the height its wrapped lines first asked for.
        var centre = new CenterContainer { Size = veil.Size };
        veil.AddChild(centre);
        var box = new PanelContainer { ThemeTypeVariation = OsmoseTheme.WindowPanel };
        centre.AddChild(box);
        _confirmBox = box;
        _confirmWidth = Mathf.Min(528, Window.Size.X - 52);
        var margin = new MarginContainer();
        foreach (string side in new[] { "left", "right", "top", "bottom" })
            margin.AddThemeConstantOverride("margin_" + side, 16);
        box.AddChild(margin);
        var column = new VBoxContainer();
        column.AddThemeConstantOverride("separation", 6);
        margin.AddChild(column);
        var title = new Label { Text = words.Title };
        title.AddThemeFontSizeOverride("font_size", 22);
        title.AddThemeFontOverride("font", OsmoseTheme.Bold());
        title.AddThemeColorOverride("font_color", new Color(UiStyle.TextStrong));
        column.AddChild(title);
        if (words.Leaves is string leaves)
            column.AddChild(ConfirmLine(leaves, UiStyle.Text));
        column.AddChild(new Label { Text = words.Section, ThemeTypeVariation = OsmoseTheme.SectionLabel });
        foreach (SpecGift gift in words.Gifts)
            column.AddChild(GiftRow(gift, _confirmWidth - 42));
        foreach (string rank in words.Ranks)
            column.AddChild(ConfirmLine(rank, UiStyle.Text));
        column.AddChild(ConfirmLine(words.Price, words.First ? UiStyle.Pistachio : UiStyle.Cost));
        if (words.Later is string later)
            column.AddChild(ConfirmLine(later, UiStyle.TextDim));
        if (words.Deck is string deck)
            column.AddChild(ConfirmLine(deck, UiStyle.Text));
        var buttons = new HBoxContainer { Alignment = BoxContainer.AlignmentMode.End };
        buttons.AddThemeConstantOverride("separation", 10);
        column.AddChild(buttons);
        var no = new Button { Text = words.No, CustomMinimumSize = new Vector2(200, 44) };
        no.Pressed += CloseConfirm;
        buttons.AddChild(no);
        _confirmYes = new Button { Text = words.Yes, CustomMinimumSize = new Vector2(260, 44) };
        if (words.First)
            _confirmYes.ThemeTypeVariation = OsmoseTheme.Primary;
        else
            _confirmYes.AddThemeColorOverride("font_color", new Color(UiStyle.Cost));
        _offerBook = words.Deck is not null;
        _confirmYes.Pressed += () =>
        {
            CloseConfirm();
            SpecChosen?.Invoke(action.Spec, action.Fork, action.Final);
        };
        buttons.AddChild(_confirmYes);
    }

    private float _confirmWidth;
    private PanelContainer? _confirmBox;

    /// <summary>Whether the confirmation holds in the window, as the self-test reads it (§5.6).</summary>
    public bool ConfirmFits => _confirmBox is not null && _confirmBox.Size.Y <= Window.Size.Y && _confirmBox.Size.Y >= 150;

    /// <summary>What the confirmation takes, for the self-test's message.</summary>
    public string ConfirmRoom => $"{_confirmBox?.Size} in {Window.Size}";

    // A wrapped line needs a width of its own: inside a container that follows its content, it would ask for one letter a line.
    private Label ConfirmLine(string text, string colour)
    {
        Label line = Set(SpecLine(colour), text);
        line.CustomMinimumSize = new Vector2(_confirmWidth, 0);
        return line;
    }

    private static Label Set(Label label, string text)
    {
        label.Text = text;
        return label;
    }

    private static HBoxContainer GiftRow(SpecGift gift, float textWidth)
    {
        var row = new HBoxContainer { CustomMinimumSize = new Vector2(0, 44) };
        row.AddThemeConstantOverride("separation", 10);
        var sigil = new SpecSigil { CustomMinimumSize = new Vector2(32, 32), SizeFlagsVertical = Control.SizeFlags.ShrinkCenter, MouseFilter = Control.MouseFilterEnum.Ignore };
        sigil.Paint(SpecIcon(gift.Icon, false), new SpecLook(new Color(ElementStyle.WoodLight), 2));
        row.AddChild(sigil);
        var lines = new VBoxContainer { SizeFlagsHorizontal = Control.SizeFlags.ExpandFill, Alignment = BoxContainer.AlignmentMode.Center };
        lines.AddThemeConstantOverride("separation", 0);
        row.AddChild(lines);
        Label title = Set(SpecLine(UiStyle.TextStrong), gift.Title);
        title.AddThemeFontOverride("font", OsmoseTheme.Bold());
        lines.AddChild(title);
        Label line = Set(SpecLine(UiStyle.TextDim), gift.Line);
        line.CustomMinimumSize = new Vector2(textWidth, 0);
        lines.AddChild(line);
        return row;
    }

    private void CloseConfirm()
    {
        _confirm?.QueueFree();
        _confirm = null;
        _confirmYes = null;
        _confirmBox = null;
    }

    /// <summary>The road the server wrote: a new draft, the new context, the message; the tile lights up.</summary>
    public void SpecSaved(Hero hero, SpecContext context, long paid)
    {
        ArgumentNullException.ThrowIfNull(hero);
        SpecContext = context;
        _specPreview = null;
        _specOpen = null;
        string evolved = Texts["path.evolved", "", Specs.TitleOf(hero, Editor.Data)?.In(Texts.Lang) ?? ""].TrimStart();
        Saved(hero, paid > 0 ? Texts["spec.crossed", SpecPage.Price(paid, Texts), evolved] : evolved);
        _message.RemoveThemeColorOverride("font_color");
        _specPicked = SpecView().Opening;
        RefreshSpec();
        if (_specTiles.TryGetValue(_specPicked, out SpecTile? lit))
        {
            lit.Button.Modulate = new Color(1.25f, 1.25f, 1.25f);
            CreateTween().TweenProperty(lit.Button, "modulate", Colors.White, 0.3);
        }
    }

    /// <summary>The server refused (§7.4): nothing moved, and the message says so.</summary>
    public void SpecRefused()
    {
        _offerBook = false;
        _message.Text = Texts["spec.refused"];
        _message.AddThemeColorOverride("font_color", new Color(UiStyle.Losing));
    }

    /// <summary>Whether the page fits its window without scrolling, as the self-test reads it once laid out.</summary>
    public bool SpecFits => _specPage.GetChild<Control>(0).GetCombinedMinimumSize() is Vector2 need && need.X <= _specPage.Size.X + 0.5f && need.Y <= _specPage.Size.Y + 0.5f;

    /// <summary>What the page needs and has, for the self-test's message.</summary>
    public string SpecRoom => $"{_specPage.GetChild<Control>(0).GetCombinedMinimumSize()} in {_specPage.Size}";

    /// <summary>The tiles, as the self-test reads them.</summary>
    public IReadOnlyDictionary<string, Button> SpecButtons => _specTiles.ToDictionary(t => t.Key, t => t.Value.Button);

    /// <summary>The titles that take more than two lines, for the self-test.</summary>
    public IEnumerable<string> SpecLongTitles => _specTiles.Values.Where(t => t.Title.GetLineCount() > 2).Select(t => t.Title.Text);

    /// <summary>Whether the tree's button holds its words, as the self-test reads it (§5.2).</summary>
    public bool SpecTreeButtonFits => _openTree.GetCombinedMinimumSize().Y <= SpecTree.ButtonHeight + 0.5f;

    /// <summary>The panel's button, as the self-test reads it.</summary>
    public Button SpecCardAction => _cardAction;

    /// <summary>Whether the panel holds what it shows, as the self-test reads it.</summary>
    public bool SpecCardFits => _specCard.GetCombinedMinimumSize().Y <= SpecTree.TreeY + SpecTree.Height + 0.5f;

    /// <summary>What the panel needs, for the self-test's message.</summary>
    public string SpecCardRoom => $"{_specPicked}: {_specCard.GetCombinedMinimumSize().Y}";

    /// <summary>Whether the panel's title fits its width, for the self-test.</summary>
    public bool SpecTitleFits => _cardTitle.GetThemeFont("font").GetStringSize(_cardTitle.Text, HorizontalAlignment.Left, -1, _cardTitle.GetThemeFontSize("font_size")).X <= _cardTitle.Size.X + 0.5f;

    /// <summary>The card's title, at 22 points, smaller down to 17 when a long one would be cut (sprint 199: « Manipulateur arcanique »).</summary>
    private void SetCardTitle(string text)
    {
        _cardTitle.Text = text;
        FitCardTitle();
    }

    private void FitCardTitle()
    {
        Font font = _cardTitle.GetThemeFont("font");
        float room = _cardTitle.Size.X + 0.5f;
        int size = 22;
        while (size > 17 && room > 1f && font.GetStringSize(_cardTitle.Text, HorizontalAlignment.Left, -1, size).X > room)
            size--;
        _cardTitle.AddThemeFontSizeOverride("font_size", size);
    }

    /// <summary>The confirmation's yes, pressed as the player would.</summary>
    public void PressConfirmYes() => _confirmYes?.EmitSignal(BaseButton.SignalName.Pressed);

    /// <summary>Whether the confirmation holds the window.</summary>
    public bool Confirming => _confirm is not null;

    /// <summary>A seal's look (§5.4); the mark says the state beside its colour.</summary>
    private readonly record struct SpecLook(Color Ring, float Width, Color Halo = default, bool Grey = false, bool Check = false, bool Padlock = false, bool Stroke = false);

    /// <summary>The seal (§5.4): a hollow, a ring of one colour, its silhouette, its mark.</summary>
    private sealed partial class SpecSigil : Control
    {
        private Texture2D? _icon;
        private SpecLook _look;

        public void Paint(Texture2D? icon, SpecLook look)
        {
            (_icon, _look) = (icon, look);
            QueueRedraw();
        }

        public override void _Draw()
        {
            float size = Mathf.Min(Size.X, Size.Y);
            if (size <= 0)
                size = Mathf.Min(CustomMinimumSize.X, CustomMinimumSize.Y);
            var centre = new Vector2(size / 2, size / 2);
            float radius = (size / 2) - 2;
            if (_look.Halo != default)
                DrawCircle(centre, radius + 3, new Color(_look.Halo, 0.18f));
            DrawCircle(centre, radius, new Color(UiStyle.Well));
            DrawArc(centre, radius, 0, Mathf.Tau, 48, _look.Ring, _look.Width, true);
            if (_icon is not null)
            {
                float side = size * 0.64f;
                DrawTextureRect(_icon, new Rect2(centre - new Vector2(side / 2, side / 2), new Vector2(side, side)), false);
            }
            string? mark = _look.Check ? "check" : _look.Padlock ? "padlock" : null;
            if (mark is not null)
                DrawTextureRect(GD.Load<Texture2D>($"res://assets/icons/relief/{mark}.svg"), new Rect2(size - 14, size - 14, 16, 16), false);
            if (_look.Stroke)
                DrawLine(new Vector2(-2, size + 2), new Vector2(size + 2, -2), new Color(ElementStyle.Locked), 2, true);
        }
    }

    /// <summary>The links (§5.3): broken lines from the root, by what each leads to; the sap of a tile under the pointer; the dashes of a crossing-out.</summary>
    private sealed partial class SpecWoodView : Control
    {
        public PointsPanel Panel = null!;

        private static Vector2[] Points((float X, float Y)[] line) => [.. line.Select(p => new Vector2(p.X, p.Y))];

        public override void _Draw()
        {
            SpecPage page = Panel.SpecView();
            IReadOnlyList<Specialisation> branches = page.Branches;
            IReadOnlyList<SpecNode> nodes = page.Nodes;
            SpecNode Node(string key) => nodes.Single(n => n.Key == key);
            Specialisation open = Panel.Editor.Data.Specs[page.OpenBranch];
            List<(SpecWood Wood, Vector2[] Line)> links = [];
            Vector2[] LineTo(SpecNode node)
            {
                int b = branches.ToList().FindIndex(s => s.Id == node.Spec);
                if (node.Fork is null)
                    return Points(SpecTree.ToBranch(b));
                int f = branches[b].Forks.ToList().FindIndex(x => x.Id == node.Fork);
                return Points(node.Level == Specs.Forks ? SpecTree.ToFork(b, f) : SpecTree.ToFinal(b, f, SpecTree.LastY(open, node)));
            }
            links.AddRange(nodes.Select(n => (page.WoodTo(n), LineTo(n))));
            foreach ((SpecWood wood, Vector2[] line) in links.OrderByDescending(l => l.Wood))
                DrawLink(wood, line);
            string? shown = Panel._specHovered ?? Panel._specPicked;
            if (shown is null || shown == RootKey || page.Find(shown) is not SpecNode target)
                return;
            // Under the pointer: the road to it, in sap at 60 %.
            if (Panel._specHovered == shown && target.State != SpecState.Chosen)
            {
                foreach (string key in SpecPage.RoadTo(target).Where(k => Node(k).State != SpecState.Chosen))
                    Sap(LineTo(Node(key)), new Color(UiStyle.Pistachio, 0.6f), 3);
            }
            // Shown while it would be paid for: the road offered, dashed; the old one keeps its sap, nothing has changed.
            if (target.State is SpecState.Crossed or SpecState.Open && Specs.ChangeCost(Panel.Editor.Hero, target.Spec, target.Fork, target.Final) > 0)
            {
                foreach (string key in SpecPage.RoadTo(target).Where(k => Node(k).State != SpecState.Chosen))
                    Dashes(LineTo(Node(key)), new Color(UiStyle.Cost));
            }
        }

        private void DrawLink(SpecWood wood, Vector2[] line)
        {
            switch (wood)
            {
                case SpecWood.Alive:
                    DrawPolyline([.. line.Select(p => p + new Vector2(0, 2))], new Color(0, 0, 0, 0.35f), 8, true);
                    Sap(line, new Color(ElementStyle.Wood), 6);
                    Sap(line, new Color(UiStyle.Pistachio), 3);
                    break;
                case SpecWood.Open:
                    Sap(line, new Color(ElementStyle.WoodLight), 3);
                    break;
                case SpecWood.Bare:
                    Dashes(line, new Color(ElementStyle.WoodLight));
                    break;
                default:
                    DrawPolyline(line, new Color(ElementStyle.OutOfLevel), 1);
                    break;
            }
        }

        // DrawPolyline leaves its angles square: a disc of the line's width at each.
        private void Sap(Vector2[] line, Color colour, float width)
        {
            DrawPolyline(line, colour, width, true);
            foreach (Vector2 corner in line.Skip(1).SkipLast(1))
                DrawCircle(corner, width / 2, colour);
        }

        private void Dashes(Vector2[] line, Color colour)
        {
            for (int i = 0; i + 1 < line.Length; i++)
                DrawDashedLine(line[i], line[i + 1], colour, 2, 6);
        }
    }
}
