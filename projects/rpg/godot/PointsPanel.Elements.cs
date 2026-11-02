using Godot;
using Rpg.Client;
using Rpg.Core;

namespace Rpg.Desktop;

/// <summary>
/// The Elements tab (sprint 183, D93, docs/elements-specialisations.md §v3.3): on the left the base elements and the threshold
/// of D85, in the middle the wheel of the fusions, on the right the fusion picked with its Create or Dissolve, above them the
/// frieze of the tiers. It only draws <see cref="ElementsPage"/>; what is created is part of the draft, saved like the rest.
/// </summary>
public partial class PointsPanel
{
    private readonly List<(ElementTier Tier, Label Text)> _tiers = [];
    private readonly List<(Element Element, Label Name, Label Value, ProgressBar Bar)> _bars = [];
    private readonly Dictionary<string, Button> _wheelNodes = [];
    private Label _elementsMine = null!, _elementsNote = null!, _fusionName = null!, _fusionParents = null!, _fusionSignature = null!, _fusionReaction = null!, _fusionSpells = null!, _fusionNote = null!;
    private Button _fusionPress = null!, _toStats = null!;
    private WheelLinks _links = null!;
    private string _picked = "lightning";

    private VBoxContainer BuildElements()
    {
        var page = new VBoxContainer();
        page.AddThemeConstantOverride("separation", 10);
        var frieze = new HBoxContainer();
        frieze.AddThemeConstantOverride("separation", 6);
        page.AddChild(frieze);
        foreach (ElementTier tier in new ElementsPage(Editor).Tiers)
        {
            var step = new Label { SizeFlagsHorizontal = Control.SizeFlags.ExpandFill, HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center, CustomMinimumSize = new Vector2(0, 34) };
            step.AddThemeFontSizeOverride("font_size", 14);
            frieze.AddChild(step);
            _tiers.Add((tier, step));
        }
        var columns = new HBoxContainer();
        columns.AddThemeConstantOverride("separation", 16);
        page.AddChild(columns);
        columns.AddChild(BuildBars());
        columns.AddChild(BuildWheel());
        columns.AddChild(BuildFusionCard());
        return page;
    }

    private VBoxContainer BuildBars()
    {
        var left = new VBoxContainer { CustomMinimumSize = new Vector2(232, 0) };
        left.AddThemeConstantOverride("separation", 6);
        _elementsMine = new Label();
        _elementsMine.AddThemeFontSizeOverride("font_size", 18);
        left.AddChild(_elementsMine);
        foreach (ElementBar bar in new ElementsPage(Editor).Bars)
        {
            var row = new HBoxContainer();
            left.AddChild(row);
            var name = new Label { SizeFlagsHorizontal = Control.SizeFlags.ExpandFill };
            name.AddThemeColorOverride("font_color", new Color(ElementStyle.Colour(bar.Element)));
            row.AddChild(name);
            var value = new Label();
            value.AddThemeFontSizeOverride("font_size", 14);
            row.AddChild(value);
            var meter = new ProgressBar { MinValue = 0, MaxValue = 1, Step = 0.001, ShowPercentage = false, CustomMinimumSize = new Vector2(0, 10) };
            meter.AddThemeStyleboxOverride("background", Flat(ElementStyle.LockedFill, null));
            meter.AddThemeStyleboxOverride("fill", Flat(ElementStyle.Colour(bar.Element), null));
            left.AddChild(meter);
            _bars.Add((bar.Element, name, value, meter));
        }
        _elementsNote = new Label { AutowrapMode = TextServer.AutowrapMode.WordSmart, Modulate = new Color(1, 1, 1, 0.75f) };
        _elementsNote.AddThemeFontSizeOverride("font_size", 13);
        left.AddChild(_elementsNote);
        _toStats = new Button { CustomMinimumSize = new Vector2(0, 44), ClipText = true };
        _toStats.AddThemeFontSizeOverride("font_size", 14);
        _toStats.Pressed += () => ShowTab(spells: false);
        left.AddChild(_toStats);
        return left;
    }

    private Control BuildWheel()
    {
        var wheel = new Control { CustomMinimumSize = new Vector2(ElementsWheel.Width, ElementsWheel.Height), MouseFilter = Control.MouseFilterEnum.Pass };
        _links = new WheelLinks { Panel = this, MouseFilter = Control.MouseFilterEnum.Ignore };
        _links.SetAnchorsPreset(Control.LayoutPreset.FullRect);
        wheel.AddChild(_links);
        foreach ((string id, float x, float y) in ElementsWheel.Nodes)
        {
            var node = new Button
            {
                Position = new Vector2(x - ElementsWheel.NodeWidth / 2, y - ElementsWheel.NodeHeight / 2),
                Size = new Vector2(ElementsWheel.NodeWidth, ElementsWheel.NodeHeight),
                ClipText = true,
            };
            node.AddThemeFontSizeOverride("font_size", 14);
            node.Pressed += () => PickFusion(id);
            wheel.AddChild(node);
            _wheelNodes[id] = node;
        }
        return wheel;
    }

    private VBoxContainer BuildFusionCard()
    {
        var card = new VBoxContainer { SizeFlagsHorizontal = Control.SizeFlags.ExpandFill, CustomMinimumSize = new Vector2(300, 0) };
        card.AddThemeConstantOverride("separation", 6);
        _fusionName = new Label();
        _fusionName.AddThemeFontSizeOverride("font_size", 22);
        card.AddChild(_fusionName);
        _fusionParents = new Label { Modulate = new Color(1, 1, 1, 0.8f) };
        card.AddChild(_fusionParents);
        foreach (Label text in new[] { _fusionSignature = new Label(), _fusionReaction = new Label(), _fusionSpells = new Label() })
        {
            text.AutowrapMode = TextServer.AutowrapMode.WordSmart;
            text.AddThemeFontSizeOverride("font_size", 14);
            card.AddChild(text);
        }
        _fusionPress = new Button { CustomMinimumSize = new Vector2(0, 44) };
        _fusionPress.Pressed += PressPicked;
        card.AddChild(_fusionPress);
        _fusionNote = new Label { AutowrapMode = TextServer.AutowrapMode.WordSmart, Modulate = new Color(1, 1, 1, 0.7f) };
        _fusionNote.AddThemeFontSizeOverride("font_size", 13);
        card.AddChild(_fusionNote);
        return card;
    }

    private static StyleBoxFlat Flat(string fill, string? border)
    {
        var box = new StyleBoxFlat { BgColor = new Color(fill) };
        box.SetCornerRadiusAll(8);
        if (border is not null)
        {
            box.SetBorderWidthAll(2);
            box.BorderColor = new Color(border);
        }
        return box;
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
        Refresh();
    }

    public void PickFusion(string id)
    {
        _picked = id;
        RefreshElements();
    }

    /// <summary>Create or Dissolve on the fusion picked, as the button does; the message says what changed.</summary>
    public void PressPicked()
    {
        var page = new ElementsPage(Editor);
        if (page.Fusions.SingleOrDefault(f => f.Id == _picked) is not Fusion fusion || !page.Press(fusion))
            return;
        string name = Texts[$"fusion.{fusion.Id}.name"];
        _message.Text = fusion.State != FusionState.Created ? (Editor.DeckChanges.Count > 0 ? Texts["elements.created.deck", name, Editor.DeckChanges.Count] : Texts["elements.created", name])
            : Editor.DeckChanges.Count > 0 ? Texts["elements.dissolved.deck", name, Editor.DeckChanges.Count]
            : Texts["elements.dissolved", name];
        Refresh();
    }

    private void RefreshElements()
    {
        var page = new ElementsPage(Editor);
        foreach ((ElementTier tier, Label text) in _tiers)
        {
            bool reached = page.Tiers.Single(t => t.Level == tier.Level).Reached;
            text.Text = Texts[$"elements.tier.{tier.Level}"];
            text.AddThemeColorOverride("font_color", new Color(reached ? ElementStyle.Taken : ElementStyle.Locked));
            text.AddThemeStyleboxOverride("normal", Flat(reached ? ElementStyle.TakenFill : ElementStyle.LockedFill, null));
        }
        _elementsMine.Text = Texts["elements.mine"];
        IReadOnlyList<ElementBar> bars = page.Bars;
        foreach ((Element element, Label name, Label value, ProgressBar meter) in _bars)
        {
            ElementBar bar = bars.Single(b => b.Element == element);
            name.Text = Texts["element." + element];
            value.Text = Texts["elements.bar", bar.Total, bar.Threshold, bar.Bonus];
            value.AddThemeColorOverride("font_color", new Color(bar.Over ? ElementStyle.Losing : ElementStyle.Open));
            meter.Value = bar.Fill;
        }
        int threshold = bars[0].Threshold;
        _elementsNote.Text = Texts["elements.bar.note", threshold];
        _toStats.Text = Texts["elements.to-stats"];
        foreach ((string id, Button node) in _wheelNodes)
        {
            Fusion? fusion = page.Fusions.SingleOrDefault(f => f.Id == id);
            if (fusion is null)
                StyleBase(node, Enum.Parse<Element>(id), id == _picked);
            else
                StyleFusion(node, fusion, id == _picked);
        }
        _links.QueueRedraw();
        RefreshCard(page);
    }

    private void StyleBase(Button node, Element element, bool picked)
    {
        node.Text = Texts["element." + element];
        string colour = ElementStyle.Colour(element);
        foreach (string state in new[] { "normal", "hover", "pressed", "focus" })
            node.AddThemeStyleboxOverride(state, Flat(ElementStyle.OpenFill, picked ? ElementStyle.Taken : colour));
        node.AddThemeColorOverride("font_color", new Color(colour));
        node.AddThemeColorOverride("font_hover_color", new Color(colour));
    }

    private void StyleFusion(Button node, Fusion fusion, bool picked)
    {
        node.Text = Texts[$"fusion.{fusion.Id}.name"];
        (string fill, string border, string text) = fusion.State switch
        {
            FusionState.Created => (ElementStyle.TakenFill, ElementStyle.Taken, ElementStyle.Taken),
            FusionState.Open => (ElementStyle.OpenFill, ElementStyle.Open, ElementStyle.Open),
            _ => (ElementStyle.OutOfLevelFill, ElementStyle.OutOfLevel, ElementStyle.Locked),
        };
        foreach (string state in new[] { "normal", "hover", "pressed", "focus" })
            node.AddThemeStyleboxOverride(state, Flat(fill, picked ? ElementStyle.Taken : border));
        node.AddThemeColorOverride("font_color", new Color(text));
        node.AddThemeColorOverride("font_hover_color", new Color(text));
    }

    private void RefreshCard(ElementsPage page)
    {
        if (page.Fusions.SingleOrDefault(f => f.Id == _picked) is not Fusion fusion)
        {
            ElementBar bar = page.Bars.Single(b => b.Element.ToString() == _picked);
            _fusionName.Text = Texts["element." + bar.Element];
            _fusionParents.Text = Texts["elements.bar", bar.Total, bar.Threshold, bar.Bonus];
            _fusionSignature.Text = Texts["elements.reaction.base"];
            _fusionReaction.Text = _fusionSpells.Text = _fusionNote.Text = "";
            _fusionPress.Visible = false;
            return;
        }
        _fusionName.Text = Texts[$"fusion.{fusion.Id}.name"];
        _fusionParents.Text = string.Join(" + ", fusion.Parents.Select(e => Texts["element." + e]));
        _fusionSignature.Text = Texts["elements.signature", Texts[$"fusion.{fusion.Id}.signature"]];
        _fusionReaction.Text = fusion.Reaction is ReactionSpec r ? Texts["elements.reaction", r.Name.In(Texts.Lang), r.Description.In(Texts.Lang)] : "";
        int spells = page.SpellsOf(fusion);
        _fusionSpells.Text = spells > 0 ? Texts["elements.spells", spells, string.Join(" + ", fusion.Parents.Select(e => Texts["element." + e])), _fusionName.Text] : "";
        _fusionPress.Visible = !Editor.ReadOnly;
        _fusionPress.Disabled = fusion.State is FusionState.Locked or FusionState.Blocked;
        _fusionPress.Text = fusion.State switch
        {
            FusionState.Created => Texts["elements.dissolve"],
            FusionState.Open => Texts["elements.create"],
            FusionState.Blocked => Texts["elements.blocked"],
            _ => Texts["elements.locked", fusion.Level],
        };
        _fusionNote.Text = fusion.State == FusionState.Blocked ? Texts["elements.blocked.by", string.Join(", ", fusion.BlockedBy.Select(e => Texts[$"fusion.{e.ToString().ToLowerInvariant()}.name"]))]
            : fusion.State is FusionState.Open or FusionState.Created && !Editor.ReadOnly ? Texts["elements.free"] : "";
    }

    /// <summary>Whether the Elements page fits its window without scrolling, as the self-test reads it once laid out.</summary>
    public bool ElementsFit => _elementsPage.GetChild<Control>(0).GetCombinedMinimumSize() is Vector2 need && need.X <= _elementsPage.Size.X + 0.5f && need.Y <= _elementsPage.Size.Y + 0.5f;

    /// <summary>What the Elements page needs and has, for the self-test's message.</summary>
    public string ElementsRoom => $"{_elementsPage.GetChild<Control>(0).GetCombinedMinimumSize()} in {_elementsPage.Size}";

    /// <summary>The wheel's nodes, as the self-test reads them.</summary>
    public IReadOnlyDictionary<string, Button> WheelNodes => _wheelNodes;

    /// <summary>The button of the card, as the self-test reads it.</summary>
    public Button FusionButton => _fusionPress;

    /// <summary>The lines of the wheel: a pair on the line of its parents, what is picked lit to its parents.</summary>
    private sealed partial class WheelLinks : Control
    {
        public PointsPanel Panel = null!;

        public override void _Draw()
        {
            var page = new ElementsPage(Panel.Editor);
            var at = ElementsWheel.Nodes.ToDictionary(n => n.Id, n => new Vector2(n.X, n.Y));
            foreach (Fusion fusion in page.Fusions.Where(f => f.Parents.Count == 2))
            {
                string colour = fusion.State == FusionState.Created ? ElementStyle.Taken : fusion.State == FusionState.Locked ? ElementStyle.OutOfLevel : ElementStyle.Coming;
                DrawLine(at[fusion.Parents[0].ToString()], at[fusion.Parents[1].ToString()], new Color(colour), 2, true);
            }
            if (page.Fusions.SingleOrDefault(f => f.Id == Panel._picked) is Fusion picked)
                foreach (Element parent in picked.Parents)
                    DrawLine(at[picked.Id], at[parent.ToString()], new Color(ElementStyle.Taken), 3, true);
        }
    }
}
