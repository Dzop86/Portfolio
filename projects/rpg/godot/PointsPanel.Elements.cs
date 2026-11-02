using Godot;
using Rpg.Client;
using Rpg.Core;

namespace Rpg.Desktop;

/// <summary>
/// The Elements tab, the workshop of fusions (sprint 189, D94, docs/maquette-elements.md): from left to right the base elements, the
/// strands to the pairs by disjoint couples, the trios, the reading panel, under the frieze of tiers. It only draws
/// <see cref="ElementsPage"/>; what is created is part of the draft, saved like the rest.
/// </summary>
public partial class PointsPanel
{
    private sealed record ElementRow(Button Button, ElementSigil Sigil, Label Name, Label Value, ProgressBar Meter, ColorRect Bar, ElementSigil Holder);

    private sealed record FusionTile(Button Button, ElementSigil Sigil, Label Name, Label Amount, Label Reaction, Label? Without, Button Action, Label LevelNote);

    private readonly Dictionary<Element, ElementRow> _rows = [];
    private readonly Dictionary<string, FusionTile> _fusionTiles = [];
    private TierFrieze _frieze = null!;
    private ElementStrands _strands = null!;
    private AtelierBraces _braces = null!;
    private ElementSigil _readSigil = null!;
    private Label _readName = null!, _readParents = null!, _readReactionTitle = null!, _readReaction = null!, _readReactionText = null!, _readTrigger = null!,
        _readSignatureTitle = null!, _readSignature = null!, _readFirst = null!, _readSecond = null!;
    private PanelContainer _reading = null!;
    private Button _toStats = null!;
    private string _picked = "";
    private string? _hoveredFusion;

    private static string IdOf(Element e) => e.ToString().ToLowerInvariant();

    private static Texture2D SigilIcon(string id, bool grey) => GD.Load<Texture2D>($"res://assets/icons/relief/element-{id}{(grey ? "-locked" : "")}.svg");

    private VBoxContainer BuildElements()
    {
        var page = new VBoxContainer();
        page.AddThemeConstantOverride("separation", 8);
        _frieze = new TierFrieze { CustomMinimumSize = new Vector2(ElementsAtelier.Width, ElementsAtelier.FriezeHeight) };
        page.AddChild(_frieze);
        var body = new Control { CustomMinimumSize = new Vector2(ElementsAtelier.Width, ElementsAtelier.Height) };
        page.AddChild(body);
        _strands = new ElementStrands { Panel = this, Position = new Vector2(ElementsAtelier.StrandsX, 0), Size = new Vector2(ElementsAtelier.StrandsWidth, ElementsAtelier.Height), MouseFilter = Control.MouseFilterEnum.Ignore };
        body.AddChild(_strands);
        _braces = new AtelierBraces { Panel = this, Position = new Vector2(ElementsAtelier.BraceX, 0), Size = new Vector2(16, ElementsAtelier.Height), MouseFilter = Control.MouseFilterEnum.Ignore };
        body.AddChild(_braces);
        for (int i = 0; i < ElementsAtelier.ElementOrder.Count; i++)
            BuildRow(body, ElementsAtelier.ElementOrder[i], ElementsAtelier.ElementY(i));
        _toStats = new Button
        {
            Position = new Vector2(0, ElementsAtelier.Height - ElementsAtelier.StatsButtonHeight),
            Size = new Vector2(ElementsAtelier.ElementWidth, ElementsAtelier.StatsButtonHeight),
            ClipText = true,
        };
        _toStats.AddThemeFontSizeOverride("font_size", 14);
        _toStats.Pressed += () => ShowTab(spells: false);
        body.AddChild(_toStats);
        for (int i = 0; i < ElementsAtelier.PairOrder.Count; i++)
            BuildTile(body, ElementsAtelier.PairOrder[i], new Vector2(ElementsAtelier.PairX, ElementsAtelier.PairY(i)), new Vector2(ElementsAtelier.PairWidth, ElementsAtelier.PairHeight), 36, trio: false);
        for (int i = 0; i < ElementsAtelier.TrioOrder.Count; i++)
            BuildTile(body, ElementsAtelier.TrioOrder[i], new Vector2(ElementsAtelier.TrioX, ElementsAtelier.TrioY(i)), new Vector2(ElementsAtelier.TrioWidth, ElementsAtelier.TrioHeight), 40, trio: true);
        body.AddChild(BuildReading());
        return page;
    }

    private static Label Text(Control parent, float x, float y, float width, int size, bool bold = false)
    {
        var label = new Label { Position = new Vector2(x, y), Size = new Vector2(width, size + 6), ClipText = true, MouseFilter = Control.MouseFilterEnum.Ignore };
        label.AddThemeFontSizeOverride("font_size", size);
        if (bold)
            label.AddThemeFontOverride("font", OsmoseTheme.Bold());
        parent.AddChild(label);
        return label;
    }

    private void BuildRow(Control body, Element element, float y)
    {
        var row = new Button { Position = new Vector2(0, y), Size = new Vector2(ElementsAtelier.ElementWidth, ElementsAtelier.ElementHeight), ToggleMode = true, ThemeTypeVariation = OsmoseTheme.CompoundOpen };
        row.Pressed += () => PickFusion(element.ToString());
        Hover(row, element.ToString());
        body.AddChild(row);
        var bar = new ColorRect { Size = new Vector2(4, ElementsAtelier.ElementHeight), MouseFilter = Control.MouseFilterEnum.Ignore, Visible = false };
        row.AddChild(bar);
        var sigil = new ElementSigil { Position = new Vector2(8, 14), Size = new Vector2(40, 40), MouseFilter = Control.MouseFilterEnum.Ignore };
        row.AddChild(sigil);
        var holder = new ElementSigil { Position = new Vector2(32, 38), Size = new Vector2(20, 20), MouseFilter = Control.MouseFilterEnum.Ignore, Visible = false };
        row.AddChild(holder);
        Label name = Text(row, 56, 6, 124, 16, bold: true);
        Label value = Text(row, 56, 28, 124, 14);
        var meter = new ProgressBar { Position = new Vector2(56, 52), Size = new Vector2(124, 8), CustomMinimumSize = new Vector2(124, 8), MinValue = 0, MaxValue = 1, Step = 0.001, ShowPercentage = false, MouseFilter = Control.MouseFilterEnum.Ignore };
        meter.AddThemeStyleboxOverride("background", OsmoseTheme.Flat(UiStyle.Well, null, 0, 3));
        meter.AddThemeStyleboxOverride("fill", OsmoseTheme.Flat(ElementStyle.Colour(element), null, 0, 3));
        row.AddChild(meter);
        _rows[element] = new ElementRow(row, sigil, name, value, meter, bar, holder);
    }

    private void BuildTile(Control body, string id, Vector2 at, Vector2 size, float sigilSize, bool trio)
    {
        var tile = new Button { Position = at, Size = size, ToggleMode = true, ThemeTypeVariation = OsmoseTheme.CompoundOpen };
        tile.Pressed += () => PickFusion(id);
        Hover(tile, id);
        body.AddChild(tile);
        var sigil = new ElementSigil { Position = new Vector2(8, (size.Y - sigilSize) / 2), Size = new Vector2(sigilSize, sigilSize), MouseFilter = Control.MouseFilterEnum.Ignore };
        tile.AddChild(sigil);
        float textX = sigilSize + 16, textWidth = size.X - textX - ElementsAtelier.ActionWidth - 12;
        Label name = Text(tile, textX, trio ? 6 : 4, textWidth, 16, bold: true);
        Label amount = Text(tile, textX, trio ? 6 : 4, textWidth, 14);
        amount.HorizontalAlignment = HorizontalAlignment.Right;
        amount.MouseFilter = Control.MouseFilterEnum.Pass;
        Label reaction = Text(tile, textX, trio ? 30 : 28, textWidth, 14);
        Label? without = trio ? Text(tile, textX, 52, textWidth, 14) : null;
        var action = new Button
        {
            Position = new Vector2(size.X - ElementsAtelier.ActionWidth - 8, (size.Y - ElementsAtelier.ActionHeight) / 2),
            Size = new Vector2(ElementsAtelier.ActionWidth, ElementsAtelier.ActionHeight),
            ClipText = true,
            ThemeTypeVariation = OsmoseTheme.CompoundCreate,
        };
        action.Pressed += () => PressFusion(id);
        Hover(action, id);
        tile.AddChild(action);
        Label note = Text(tile, size.X - ElementsAtelier.ActionWidth - 8, (size.Y - 20) / 2, ElementsAtelier.ActionWidth, 14);
        note.HorizontalAlignment = HorizontalAlignment.Center;
        _fusionTiles[id] = new FusionTile(tile, sigil, name, amount, reaction, without, action, note);
    }

    // Entering a tile's button leaves the tile: both keep the panel, so that it does not flicker.
    private void Hover(Control control, string id)
    {
        control.MouseEntered += () => SetHovered(id);
        control.MouseExited += () =>
        {
            if (_hoveredFusion == id)
                SetHovered(null);
        };
    }

    private void SetHovered(string? id)
    {
        _hoveredFusion = id;
        RefreshReading(new ElementsPage(Editor));
        _strands.QueueRedraw();
    }

    private PanelContainer BuildReading()
    {
        _reading = new PanelContainer
        {
            Position = new Vector2(ElementsAtelier.PanelX, 0),
            Size = new Vector2(ElementsAtelier.PanelWidth, ElementsAtelier.Height),
            CustomMinimumSize = new Vector2(ElementsAtelier.PanelWidth, 0),
            ThemeTypeVariation = OsmoseTheme.DetailPanel,
        };
        var column = new VBoxContainer();
        column.AddThemeConstantOverride("separation", 4);
        _reading.AddChild(column);
        var head = new HBoxContainer();
        head.AddThemeConstantOverride("separation", 10);
        column.AddChild(head);
        _readSigil = new ElementSigil { CustomMinimumSize = new Vector2(52, 52), MouseFilter = Control.MouseFilterEnum.Ignore };
        head.AddChild(_readSigil);
        var who = new VBoxContainer { SizeFlagsHorizontal = Control.SizeFlags.ExpandFill };
        who.AddThemeConstantOverride("separation", 0);
        head.AddChild(who);
        _readName = new Label();
        _readName.AddThemeFontSizeOverride("font_size", 22);
        _readName.AddThemeFontOverride("font", OsmoseTheme.Bold());
        who.AddChild(_readName);
        _readParents = new Label { ClipText = true };
        _readParents.AddThemeFontSizeOverride("font_size", 14);
        who.AddChild(_readParents);
        column.AddChild(Rule());
        _readReactionTitle = new Label { ThemeTypeVariation = OsmoseTheme.SectionLabel };
        column.AddChild(_readReactionTitle);
        _readReaction = Line(UiStyle.TextStrong, bold: true);
        column.AddChild(_readReaction);
        _readReactionText = Line(UiStyle.Text);
        column.AddChild(_readReactionText);
        _readTrigger = Line(UiStyle.TextDim);
        column.AddChild(_readTrigger);
        _readSignatureTitle = new Label { ThemeTypeVariation = OsmoseTheme.SectionLabel };
        column.AddChild(_readSignatureTitle);
        _readSignature = Line(UiStyle.Text);
        column.AddChild(_readSignature);
        column.AddChild(new Control { SizeFlagsVertical = Control.SizeFlags.ExpandFill });
        column.AddChild(Rule());
        _readFirst = Line(UiStyle.Text);
        column.AddChild(_readFirst);
        _readSecond = Line(UiStyle.TextDim);
        column.AddChild(_readSecond);
        return _reading;

        static ColorRect Rule() => new() { Color = new Color("#ffffff0f"), CustomMinimumSize = new Vector2(0, 1), MouseFilter = Control.MouseFilterEnum.Ignore };

        static Label Line(string colour, bool bold = false)
        {
            var label = new Label { AutowrapMode = TextServer.AutowrapMode.WordSmart };
            label.AddThemeFontSizeOverride("font_size", 14);
            label.AddThemeColorOverride("font_color", new Color(colour));
            if (bold)
                label.AddThemeFontOverride("font", OsmoseTheme.Bold());
            return label;
        }
    }

    /// <summary>The Elements tab, as the player opens it or the self-test does.</summary>
    public void ShowElements()
    {
        _statsPage.Visible = _spellsPage.Visible = _talentsPage.Visible = false;
        _elementsPage.Visible = true;
        _statsTab.SetPressedNoSignal(false);
        _spellsTab.SetPressedNoSignal(false);
        _talentsTab.SetPressedNoSignal(false);
        _elementsTab.SetPressedNoSignal(true);
        var page = new ElementsPage(Editor);
        // On opening, the first compound created in the columns' order, or else the first open, or else the first (§5.7).
        Fusion[] all = [.. page.Pairs, .. page.Trios];
        _picked = (all.FirstOrDefault(f => f.State == FusionState.Created) ?? all.FirstOrDefault(f => f.State == FusionState.Open) ?? all[0]).Id;
        Refresh();
        _message.Text = Texts[Editor.ReadOnly ? "elements.hint.fight" : "elements.hint"];
    }

    /// <summary>Shows this compound or element in the reading panel.</summary>
    public void PickFusion(string id)
    {
        _picked = id;
        RefreshElements();
    }

    /// <summary>Create or Dissolve on a tile, as its button does; the message says what changed.</summary>
    public void PressFusion(string id)
    {
        var page = new ElementsPage(Editor);
        _picked = id;
        if (page.Fusions.SingleOrDefault(f => f.Id == id) is not Fusion fusion || !page.Press(fusion))
        {
            RefreshElements();
            return;
        }
        string name = Texts[$"fusion.{fusion.Id}.name"];
        bool created = fusion.State != FusionState.Created;
        _message.Text = created
            ? (Editor.DeckChanges.Count > 0 ? Texts["elements.created.deck", name, Editor.DeckChanges.Count] : Texts["elements.created", name])
            : Editor.DeckChanges.Count > 0 ? Texts["elements.dissolved.deck", name, Editor.DeckChanges.Count]
            : Texts["elements.dissolved", name];
        string said = _message.Text;
        Refresh();
        if (!Editor.ReadOnly)
            _message.Text = Editor.ResetCost > 0 ? _message.Text : said;
        // The moment of creating (§7): the tile lights up for 200 ms; dissolving has no flash.
        if (created)
        {
            Button tile = _fusionTiles[id].Button;
            tile.Modulate = new Color(1.25f, 1.25f, 1.25f);
            CreateTween().TweenProperty(tile, "modulate", Colors.White, 0.2);
        }
    }

    private void RefreshElements()
    {
        var page = new ElementsPage(Editor);
        _frieze.Set(page, Texts);
        IReadOnlyList<ElementBar> bars = page.Bars;
        foreach (ElementBar bar in bars)
            RefreshRow(bar);
        _toStats.Text = Texts["elements.to-stats"];
        foreach (Fusion fusion in page.Fusions)
            RefreshTile(fusion, page);
        _strands.QueueRedraw();
        _braces.QueueRedraw();
        RefreshReading(page);
    }

    private void RefreshRow(ElementBar bar)
    {
        ElementRow row = _rows[bar.Element];
        string id = IdOf(bar.Element);
        bool consumed = bar.ConsumedBy is not null;
        row.Button.ThemeTypeVariation = consumed ? OsmoseTheme.ElementConsumed : OsmoseTheme.CompoundOpen;
        row.Button.SetPressedNoSignal(_picked == bar.Element.ToString());
        row.Button.TooltipText = Texts["elements.bar.note", bar.Threshold];
        row.Name.Text = Texts["element." + bar.Element];
        row.Name.AddThemeColorOverride("font_color", new Color(consumed ? UiStyle.TextDim : UiStyle.TextStrong));
        row.Value.Text = consumed ? Texts["elements.consumed", Texts[$"fusion.{IdOf(bar.ConsumedBy!.Value)}.the"]] : Texts["elements.bar", bar.Total, bar.Threshold, bar.Bonus];
        row.Value.AddThemeColorOverride("font_color", new Color(consumed ? UiStyle.TextStrong : bar.Over ? UiStyle.Losing : UiStyle.Text));
        row.Meter.Value = bar.Fill;
        row.Meter.Modulate = new Color(1, 1, 1, consumed ? 0.6f : 1);
        row.Bar.Visible = consumed;
        row.Sigil.Paint(SigilIcon(id, consumed), [consumed ? new Color(ElementStyle.OutOfLevel) : new Color(ElementStyle.Colour(bar.Element))]);
        row.Holder.Visible = consumed;
        if (bar.ConsumedBy is Element holder)
        {
            row.Bar.Color = new Color(ElementStyle.Colour(holder));
            row.Holder.Paint(SigilIcon(IdOf(holder), false), [new Color(ElementStyle.Colour(holder))]);
        }
    }

    private void RefreshTile(Fusion fusion, ElementsPage page)
    {
        FusionTile tile = _fusionTiles[fusion.Id];
        tile.Button.ThemeTypeVariation = fusion.State switch
        {
            FusionState.Created => OsmoseTheme.CompoundCreated,
            FusionState.Open => OsmoseTheme.CompoundOpen,
            FusionState.Blocked => OsmoseTheme.CompoundBlocked,
            _ => OsmoseTheme.CompoundLocked,
        };
        tile.Button.SetPressedNoSignal(_picked == fusion.Id);
        (string nameColour, string textColour) = fusion.State switch
        {
            FusionState.Created => ("#ffffff", UiStyle.Text),
            FusionState.Open => (UiStyle.TextStrong, UiStyle.TextDim),
            FusionState.Blocked => (UiStyle.TextDim, UiStyle.TextDim),
            _ => (ElementStyle.Locked, ElementStyle.Locked),
        };
        tile.Name.Text = Texts[$"fusion.{fusion.Id}.name"];
        tile.Name.AddThemeColorOverride("font_color", new Color(nameColour));
        int spells = page.SpellsOf(fusion);
        string name = tile.Name.Text;
        tile.Amount.Visible = fusion.State != FusionState.Locked && spells > 0;
        tile.Amount.Text = Texts["elements.amount", spells];
        tile.Amount.TooltipText = Texts["elements.spells.tip", spells, name];
        tile.Amount.AddThemeColorOverride("font_color", new Color(fusion.State == FusionState.Created ? UiStyle.Pistachio : UiStyle.TextDim));
        string blockers = string.Join(", ", fusion.BlockedBy.Select(c => Texts[$"fusion.{IdOf(c)}.name"]));
        string reaction = fusion.Reaction?.Name.In(Texts.Lang) ?? "";
        bool blocked = fusion.State == FusionState.Blocked;
        // A pair blocked says by what on its line; a trio on the line of the element it lacks.
        tile.Reaction.Text = blocked && tile.Without is null ? blockers : reaction;
        tile.Reaction.AddThemeColorOverride("font_color", new Color(blocked && tile.Without is null ? UiStyle.Cost : textColour));
        if (tile.Without is Label without)
        {
            Element lacking = ElementsAtelier.ElementOrder.First(e => !fusion.Parents.Contains(e));
            without.Text = blocked ? blockers : Texts["elements.without", Texts["element." + lacking]];
            without.AddThemeColorOverride("font_color", new Color(blocked ? UiStyle.Cost : textColour));
        }
        Color[] arcs = [.. fusion.Parents.Select(parent =>
        {
            if (fusion.State == FusionState.Locked)
                return new Color(ElementStyle.OutOfLevel);
            Element? holder = fusion.State == FusionState.Blocked ? fusion.BlockedBy.Where(c => Elements.Parents(c).Contains(parent)).Cast<Element?>().FirstOrDefault() : null;
            return new Color(ElementStyle.Colour(holder ?? parent));
        })];
        tile.Sigil.Paint(SigilIcon(fusion.Id, fusion.State == FusionState.Locked), arcs, created: fusion.State == FusionState.Created, faded: blocked, halo: new Color(ElementStyle.Colour(fusion.Compound)));
        bool locked = fusion.State == FusionState.Locked;
        tile.Action.Visible = !locked && !Editor.ReadOnly;
        tile.LevelNote.Visible = locked;
        tile.LevelNote.Text = Texts["elements.locked", fusion.Level];
        tile.LevelNote.AddThemeColorOverride("font_color", new Color(ElementStyle.Locked));
        tile.Action.Disabled = blocked;
        // A blocked button still lets a click reach its tile, and says what unblocks it.
        tile.Action.MouseFilter = blocked ? Control.MouseFilterEnum.Pass : Control.MouseFilterEnum.Stop;
        tile.Action.ThemeTypeVariation = fusion.State == FusionState.Created ? OsmoseTheme.CompoundDissolve : OsmoseTheme.CompoundCreate;
        tile.Action.Text = Texts[fusion.State == FusionState.Created ? "elements.dissolve" : blocked ? "elements.blocked" : "elements.create"];
        tile.Action.TooltipText = blocked ? Texts["elements.blocked.tip", string.Join(", ", fusion.BlockedBy.Select(c => Texts[$"fusion.{IdOf(c)}.the"]))] : "";
    }

    private void RefreshReading(ElementsPage page)
    {
        string shown = _hoveredFusion ?? _picked;
        _readReactionTitle.Text = Texts["elements.section.reaction"];
        _readSignatureTitle.Text = Texts["elements.section.signature"];
        if (page.Fusions.SingleOrDefault(f => f.Id == shown) is Fusion fusion)
        {
            _readSigil.Paint(SigilIcon(fusion.Id, fusion.State == FusionState.Locked), [.. fusion.Parents.Select(e => new Color(ElementStyle.Colour(e)))],
                created: fusion.State == FusionState.Created, faded: fusion.State == FusionState.Blocked, halo: new Color(ElementStyle.Colour(fusion.Compound)));
            _readName.Text = Texts[$"fusion.{fusion.Id}.name"];
            string parents = string.Join(" + ", fusion.Parents.Select(e => Texts["element." + e]));
            _readParents.Text = fusion.Parents.Count == 3
                ? Texts["elements.parents.trio", parents, Texts["element." + ElementsAtelier.ElementOrder.First(e => !fusion.Parents.Contains(e))]]
                : parents;
            _readReaction.Text = fusion.Reaction?.Name.In(Texts.Lang) ?? "";
            _readReactionText.Text = fusion.Reaction?.Description.In(Texts.Lang) ?? "";
            _readTrigger.Text = Texts["elements.reaction.trigger"];
            _readSignatureTitle.Visible = _readSignature.Visible = true;
            _readSignature.Text = Texts[$"fusion.{fusion.Id}.signature"];
            (string first, string second, bool blocked) = page.Foot(fusion, Texts);
            _readFirst.Text = first;
            _readFirst.AddThemeColorOverride("font_color", new Color(blocked ? UiStyle.Cost : UiStyle.Text));
            _readSecond.Text = second;
            return;
        }
        // A base element.
        if (page.Bars.SingleOrDefault(b => b.Element.ToString() == shown) is not ElementBar bar)
            return;
        string name = Texts["element." + bar.Element];
        _readSigil.Paint(SigilIcon(IdOf(bar.Element), bar.ConsumedBy is not null), [new Color(ElementStyle.Colour(bar.Element))]);
        _readName.Text = name;
        _readParents.Text = Texts["elements.bar", bar.Total, bar.Threshold, bar.Bonus];
        _readReactionTitle.Text = name.ToUpperInvariant();
        _readReaction.Text = bar.ConsumedBy is Element by ? Texts["elements.consumed", Texts[$"fusion.{IdOf(by)}.the"]] : "";
        _readReactionText.Text = bar.ConsumedBy is null ? Texts["elements.reaction.base"] : Texts["elements.base.consumed"];
        _readTrigger.Text = "";
        _readSignatureTitle.Visible = _readSignature.Visible = false;
        _readFirst.Text = Texts["elements.bar.note", bar.Threshold];
        _readFirst.AddThemeColorOverride("font_color", new Color(UiStyle.TextDim));
        _readSecond.Text = bar.ConsumedBy is null
            ? Texts["elements.found-in", string.Join(", ", page.Fusions.Where(f => f.Parents.Contains(bar.Element)).Select(f => Texts[$"fusion.{f.Id}.name"]))]
            : "";
    }

    /// <summary>Whether the Elements page fits its window without scrolling, as the self-test reads it once laid out.</summary>
    public bool ElementsFit => _elementsPage.GetChild<Control>(0).GetCombinedMinimumSize() is Vector2 need && need.X <= _elementsPage.Size.X + 0.5f && need.Y <= _elementsPage.Size.Y + 0.5f;

    /// <summary>What the Elements page needs and has, for the self-test's message.</summary>
    public string ElementsRoom => $"{_elementsPage.GetChild<Control>(0).GetCombinedMinimumSize()} in {_elementsPage.Size}";

    /// <summary>The fusions' tiles, as the self-test reads them.</summary>
    public IReadOnlyDictionary<string, Button> FusionTiles => _fusionTiles.ToDictionary(t => t.Key, t => t.Value.Button);

    /// <summary>A tile's button, as the self-test reads it.</summary>
    public Button FusionAction(string id) => _fusionTiles[id].Action;

    /// <summary>Whether the reading panel holds what it shows, as the self-test reads it.</summary>
    public bool ReadingFits => _reading.GetCombinedMinimumSize().Y <= ElementsAtelier.Height + 0.5f;

    /// <summary>What the reading panel needs, for the self-test's message.</summary>
    public string ReadingRoom => $"{_picked}: {_reading.GetCombinedMinimumSize().Y}";

    /// <summary>The frieze of tiers (§5.1), shared with the Specialisation page: a root, then a chevron per tier.</summary>
    private sealed partial class TierFrieze : Control
    {
        private (string Title, string Line, string Value) _root;
        private readonly List<(float X, float Width, string Level, string Title, string Line, bool Reached, bool Next, string Tip)> _cells = [];

        public void Set(ElementsPage page, Texts texts)
        {
            _root = (texts["elements.mine"], texts["elements.threshold", page.Bars[0].Threshold], texts["elements.free-count", page.FreeElements, ElementsAtelier.ElementOrder.Count]);
            _cells.Clear();
            float[] xs = [ElementsAtelier.PairX, ElementsAtelier.TrioX, ElementsAtelier.PanelX, ElementsAtelier.Width];
            IReadOnlyList<ElementTier> tiers = page.Tiers;
            for (int i = 0; i < tiers.Count; i++)
            {
                ElementTier t = tiers[i];
                string level = t.Level.ToString(System.Globalization.CultureInfo.InvariantCulture);
                _cells.Add((xs[i], xs[i + 1] - xs[i], level, texts[$"tier.{level}.name"], texts[$"elements.tier.{level}.short"], t.Reached, t.Next, texts[$"elements.tier.{level}"]));
            }
            QueueRedraw();
        }

        public override string _GetTooltip(Vector2 atPosition)
        {
            foreach (var cell in _cells)
            {
                if (atPosition.X >= cell.X && atPosition.X < cell.X + cell.Width)
                    return cell.Tip;
            }
            return "";
        }

        private void Chevron(float x, float width, Color top, Color bottom, bool first)
        {
            const float Tip = 10;
            float h = Size.Y, right = x + width - 2;
            Vector2[] points = first
                ? [new(x, 0), new(right - Tip, 0), new(right, h / 2), new(right - Tip, h), new(x, h)]
                : [new(x, 0), new(right - Tip, 0), new(right, h / 2), new(right - Tip, h), new(x, h), new(x + Tip, h / 2)];
            Color[] colours = [.. points.Select(p => p.Y <= 0 ? top : p.Y >= h ? bottom : top.Lerp(bottom, 0.5f))];
            DrawPolygon(points, colours);
        }

        public override void _Draw()
        {
            Font font = GetThemeDefaultFont(), bold = OsmoseTheme.Bold();
            var strong = new Color(UiStyle.TextStrong);
            Color top = new(UiStyle.LitTop), bottom = new(UiStyle.LitBottom), well = new(UiStyle.Well), dim = new(UiStyle.TextDim);
            Chevron(0, ElementsAtelier.PairX, top, bottom, first: true);
            DrawString(bold, new Vector2(10, 18), _root.Title, HorizontalAlignment.Left, 120, 14, strong);
            DrawString(font, new Vector2(10, 36), _root.Line, HorizontalAlignment.Left, 226, 14, new Color(UiStyle.Text));
            DrawString(bold, new Vector2(120, 18), _root.Value, HorizontalAlignment.Right, 112, 14, new Color(UiStyle.Pistachio));
            foreach (var c in _cells)
            {
                if (c.Reached)
                    Chevron(c.X, c.Width, top, bottom, first: false);
                else
                    Chevron(c.X, c.Width, well, well, first: false);
                var centre = new Vector2(c.X + 30, Size.Y / 2);
                Color ring = c.Next ? new Color(UiStyle.Pistachio) : new Color(ElementStyle.Locked);
                if (c.Reached)
                    DrawCircle(centre, 16, new Color(UiStyle.Pistachio));
                else
                    DrawArc(centre, 15, 0, Mathf.Tau, 48, ring, c.Next ? 2 : 1, true);
                Color number = c.Reached ? new Color(UiStyle.Chocolate) : c.Next ? strong : new Color(ElementStyle.Locked);
                DrawString(bold, new Vector2(c.X + 14, (Size.Y / 2) + 5), c.Level, HorizontalAlignment.Center, 32, 14, number);
                float textX = c.X + 52, textWidth = c.Width - 66;
                DrawString(bold, new Vector2(textX, 18), c.Title, HorizontalAlignment.Left, textWidth, 14, c.Reached || c.Next ? strong : dim);
                DrawString(font, new Vector2(textX, 36), c.Line, HorizontalAlignment.Left, textWidth, 14, c.Reached ? new Color(UiStyle.Text) : dim);
            }
        }
    }

    /// <summary>The strands (§5.3): a curve from each element to each of its pairs, in the element's colour, by the pair's state.</summary>
    private sealed partial class ElementStrands : Control
    {
        public PointsPanel Panel = null!;

        public override void _Draw()
        {
            var page = new ElementsPage(Panel.Editor);
            string? hot = Panel._hoveredFusion ?? Panel._picked;
            List<(int Order, Vector2[] Line, float Width, Color Colour, bool Under)> strands = [];
            foreach (Fusion pair in page.Pairs)
            {
                foreach (Element parent in pair.Parents)
                {
                    float from = ElementsAtelier.StrandFrom(parent, pair.Id), to = ElementsAtelier.StrandTo(pair.Id, parent);
                    Vector2[] line = Curve(from, to, Size.X);
                    Color own = new(ElementStyle.Colour(parent));
                    bool shown = pair.Id == hot;
                    switch (pair.State)
                    {
                        case FusionState.Created:
                            strands.Add((2, line, 5, new Color(UiStyle.Edge), true));
                            strands.Add((2, line, 3, own, false));
                            break;
                        case FusionState.Open when shown:
                            strands.Add((3, line, 2, new Color(own, 0.9f), false));
                            break;
                        case FusionState.Open:
                            strands.Add((1, line, 1.5f, new Color(own, 0.35f), false));
                            break;
                        default:
                            strands.Add((0, line, 1, new Color(ElementStyle.OutOfLevel), false));
                            break;
                    }
                }
            }
            foreach (var s in strands.OrderBy(s => s.Order))
            {
                DrawPolyline(s.Line, s.Colour, s.Width, true);
                if (!s.Under && s.Order > 0)
                {
                    DrawCircle(s.Line[0], 3, s.Colour);
                    DrawCircle(s.Line[^1], 3, s.Colour);
                }
            }
        }

        private static Vector2[] Curve(float from, float to, float width)
        {
            var points = new Vector2[25];
            Vector2 a = new(0, from), b = new(width / 2, from), c = new(width / 2, to), d = new(width, to);
            for (int i = 0; i <= 24; i++)
            {
                float t = i / 24f, u = 1 - t;
                points[i] = (u * u * u * a) + (3 * u * u * t * b) + (3 * u * t * t * c) + (t * t * t * d);
            }
            return points;
        }
    }

    /// <summary>The braces (§5.4): one per couple of disjoint pairs, pistachio when one of them is created: the other is the second possible.</summary>
    private sealed partial class AtelierBraces : Control
    {
        public PointsPanel Panel = null!;

        public override void _Draw()
        {
            var page = new ElementsPage(Panel.Editor);
            for (int i = 0; i < ElementsAtelier.PairOrder.Count; i += 2)
            {
                bool lit = page.Fusions.Any(f => f.State == FusionState.Created && (f.Id == ElementsAtelier.PairOrder[i] || f.Id == ElementsAtelier.PairOrder[i + 1]));
                var colour = new Color(lit ? UiStyle.Pistachio : UiStyle.Frame);
                float top = ElementsAtelier.PairY(i) + 2, bottom = ElementsAtelier.PairY(i + 1) + ElementsAtelier.PairHeight - 2;
                DrawPolyline([new Vector2(2, top), new Vector2(8, top), new Vector2(8, bottom), new Vector2(2, bottom)], colour, 2, true);
            }
        }
    }

    /// <summary>The seal (§5.5): a hollow, a ring in its parents' colours, its silhouette; created, a halo and a check.</summary>
    private sealed partial class ElementSigil : Control
    {
        private Texture2D? _icon;
        private Color[] _arcs = [];
        private bool _created, _faded;
        private Color _halo;

        public void Paint(Texture2D icon, Color[] arcs, bool created = false, bool faded = false, Color halo = default)
        {
            (_icon, _arcs, _created, _faded, _halo) = (icon, arcs, created, faded, halo);
            QueueRedraw();
        }

        public override void _Draw()
        {
            float size = Mathf.Min(Size.X, Size.Y);
            if (size <= 0)
                size = Mathf.Min(CustomMinimumSize.X, CustomMinimumSize.Y);
            var centre = new Vector2(size / 2, size / 2);
            float radius = (size / 2) - 2, width = size <= 20 ? 2 : 3;
            if (_created)
                DrawCircle(centre, radius + 3, new Color(_halo, 0.18f));
            DrawCircle(centre, radius, new Color(UiStyle.Well));
            if (_arcs.Length == 1)
                DrawArc(centre, radius, 0, Mathf.Tau, 48, _arcs[0], width, true);
            else
            {
                float gap = Mathf.DegToRad(4), part = Mathf.Tau / _arcs.Length;
                for (int i = 0; i < _arcs.Length; i++)
                {
                    float start = (-Mathf.Pi / 2) + (i * part) + (gap / 2);
                    DrawArc(centre, radius, start, start + part - gap, 32, _arcs[i], width, true);
                }
            }
            if (_icon is not null)
            {
                float side = size * 0.64f;
                DrawTextureRect(_icon, new Rect2(centre - new Vector2(side / 2, side / 2), new Vector2(side, side)), false, new Color(1, 1, 1, _faded ? 0.5f : 1));
            }
            if (_created)
                DrawTextureRect(GD.Load<Texture2D>("res://assets/icons/relief/check.svg"), new Rect2(size - 14, size - 14, 16, 16), false);
        }
    }
}
