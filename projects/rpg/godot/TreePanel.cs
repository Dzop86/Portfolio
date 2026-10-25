using Godot;
using Rpg.Client;
using Rpg.Core;

namespace Rpg.Desktop;

/// <summary>
/// A window that edits the hero's points and saves them (the characteristics and spells, the tree): what
/// <c>SavePoints</c> needs to save the draft and say how it went.
/// </summary>
public interface IPointsWindow
{
    PointsEditor Editor { get; }

    Texts Texts { get; }

    void Saved(Hero hero, string message);

    void ShowMessage(string message);
}

/// <summary>
/// The talent tree in its own window (sprint 148, D82, K1 d after the mock-up of sprint 146, K2 b: the T key, and a button
/// from the Talents tab): read from the bottom up, the trunk, the fork, the Lightning braid and the six styles, the one looked
/// at deployed (<see cref="TreeLayout"/>); each node in one of five states before the click (L5 b), the links taken in
/// pistachio, those that can be in grey, the others dotted (L6 a). On the right, the panel (K7 b): the points, the elements
/// beyond the base, the big switch, the node under the mouse or the keyboard, the legend, save (Ctrl+S too), undo and close.
/// A click or Enter takes a node or gives it back; giving one back that others hang on shows them in red first and asks
/// (I1 b). It only draws <see cref="PointsEditor"/>, <see cref="TreeLayout"/> and <see cref="Texts"/>.
/// </summary>
public partial class TreePanel : CanvasLayer, IPointsWindow
{
    private const float PanelWidth = 280;
    private TreeCanvas _canvas = null!;
    private Label _title = null!, _message = null!, _keys = null!, _confirmText = null!, _legendTitle = null!, _buildTitle = null!, _glossary = null!;
    private RichTextLabel _detail = null!;
    private readonly List<Label> _summary = [], _build = [];
    private Control _detailBox = null!, _legendBox = null!;
    private Button _help = null!;
    private PanelContainer? _bubble;
    private Label _bubbleText = null!;
    private Button _bubbleNext = null!;
    private readonly List<(NodeState? State, Label Text)> _legend = [];
    private Button _save = null!, _undo = null!, _close = null!, _yes = null!, _no = null!;
    private Control _confirm = null!;
    private string? _hovered;

    public PointsEditor Editor { get; private set; } = null!;

    public Texts Texts { get; private set; } = new("en");

    public TreeLayout Layout { get; private set; } = null!;

    /// <summary>The node the keyboard is on.</summary>
    public string Focused { get; private set; } = "";

    /// <summary>The talent whose giving back waits for a yes, with those it would give back too (I1 b).</summary>
    public (string Talent, IReadOnlyList<string> Losing)? Asking { get; private set; }

    /// <summary>The window itself (for the self-test).</summary>
    public Control Window { get; private set; } = null!;

    public event Action<Points>? SavePressed;
    public event Action? ClosePressed;

    /// <summary>The three guided bubbles read to the end (sprint 149, M6 a): the account marks them seen.</summary>
    public event Action? TutorialDone;

    /// <summary>The bubble of the tutorial shown, from 0; -1 without one.</summary>
    public int TutorialStep { get; private set; } = -1;

    /// <summary>The legend and the glossary shown in place of the node's card (M5 b, behind "?").</summary>
    public bool LegendShown => _legendBox.Visible;

    /// <summary>The node's card as the panel shows it, without its formatting (for the self-test).</summary>
    public string CardText => _detail.GetParsedText();

    public void Build(PointsEditor editor, Texts texts)
    {
        // Over the mini-map (layer 6), with the other windows.
        Layer = 15;
        Editor = editor;
        Texts = texts;
        Vector2 screen = GetViewport()?.GetVisibleRect().Size ?? new Vector2(1280, 720);
        // As large as the game allows, never larger (Charles, after 1.0.134: the save button out of reach).
        var size = new Vector2(Mathf.Min(1250, screen.X - 20), Mathf.Min(700, screen.Y - 16));
        var panel = new PanelContainer { Position = (screen - size) / 2, Size = size, CustomMinimumSize = size };
        panel.AddThemeStyleboxOverride("panel", InventoryPanel.Background());
        AddChild(panel);
        WindowDrag.Make(panel, "tree");
        Window = panel;
        var margin = new MarginContainer();
        foreach (string side in new[] { "left", "right", "top", "bottom" })
            margin.AddThemeConstantOverride("margin_" + side, 12);
        panel.AddChild(margin);
        var row = new HBoxContainer();
        row.AddThemeConstantOverride("separation", 12);
        margin.AddChild(row);

        var left = new VBoxContainer { SizeFlagsHorizontal = Control.SizeFlags.ExpandFill };
        row.AddChild(left);
        _title = new Label();
        _title.AddThemeFontSizeOverride("font_size", 20);
        left.AddChild(_title);
        var area = new Vector2(size.X - PanelWidth - 12 - 24, size.Y - 24 - 34);
        _canvas = new TreeCanvas { Panel = this, CustomMinimumSize = area, ClipContents = true };
        left.AddChild(_canvas);

        var side2 = new VBoxContainer { CustomMinimumSize = new Vector2(PanelWidth, 0) };
        side2.AddThemeConstantOverride("separation", 6);
        row.AddChild(side2);
        for (int i = 0; i < 3; i++)
        {
            var line = new Label { AutowrapMode = TextServer.AutowrapMode.WordSmart, CustomMinimumSize = new Vector2(PanelWidth, 0) };
            line.AddThemeFontSizeOverride("font_size", i == 0 ? 17 : 14);
            if (i == 0)
                line.AddThemeColorOverride("font_color", new Color(ElementStyle.Taken));
            side2.AddChild(line);
            _summary.Add(line);
        }
        // The build: its title, what it pays, its best hit against the ceiling, and the pair advised (N8 a, N1 a, C5 b).
        _buildTitle = new Label { AutowrapMode = TextServer.AutowrapMode.WordSmart, CustomMinimumSize = new Vector2(PanelWidth, 0) };
        _buildTitle.AddThemeFontSizeOverride("font_size", 16);
        side2.AddChild(_buildTitle);
        for (int i = 0; i < 3; i++)
        {
            var line = new Label { AutowrapMode = TextServer.AutowrapMode.WordSmart, CustomMinimumSize = new Vector2(PanelWidth, 0), Modulate = new Color(1, 1, 1, 0.8f) };
            line.AddThemeFontSizeOverride("font_size", 12);
            side2.AddChild(line);
            _build.Add(line);
        }
        // The legend behind "?" (M5 b), in place of the node's card.
        var top = new HBoxContainer();
        side2.AddChild(top);
        top.AddChild(new HSeparator { SizeFlagsHorizontal = Control.SizeFlags.ExpandFill, SizeFlagsVertical = Control.SizeFlags.ShrinkCenter });
        _help = new Button { ToggleMode = true, CustomMinimumSize = new Vector2(44, 44) };
        _help.Toggled += shown => ShowLegend(shown);
        top.AddChild(_help);
        // The node under the mouse or the keyboard: its card, the glossary's words lit (M7 a).
        var detailBox = new ScrollContainer { CustomMinimumSize = new Vector2(PanelWidth, 120), SizeFlagsVertical = Control.SizeFlags.ExpandFill, HorizontalScrollMode = ScrollContainer.ScrollMode.Disabled };
        _detailBox = detailBox;
        side2.AddChild(detailBox);
        _detail = new RichTextLabel { BbcodeEnabled = true, FitContent = true, ScrollActive = false, CustomMinimumSize = new Vector2(PanelWidth - 12, 0), MetaUnderlined = false };
        _detail.AddThemeFontSizeOverride("normal_font_size", 13);
        _detail.AddThemeFontSizeOverride("italics_font_size", 13);
        _detail.AddThemeFontSizeOverride("bold_font_size", 14);
        _detail.MetaHoverStarted += meta => _detail.TooltipText = Texts.TreeGlossary.FirstOrDefault(g => g.Term == meta.AsString()).Meaning ?? "";
        _detail.MetaHoverEnded += _ => _detail.TooltipText = "";
        detailBox.AddChild(_detail);
        // Giving back a node others hang on: asked first (I1 b).
        var confirm = new VBoxContainer { Visible = false };
        _confirm = confirm;
        side2.AddChild(confirm);
        _confirmText = new Label { AutowrapMode = TextServer.AutowrapMode.WordSmart, CustomMinimumSize = new Vector2(PanelWidth, 0) };
        _confirmText.AddThemeColorOverride("font_color", new Color(ElementStyle.Losing));
        confirm.AddChild(_confirmText);
        var yesNo = new HBoxContainer();
        yesNo.AddThemeConstantOverride("separation", 8);
        confirm.AddChild(yesNo);
        _yes = new Button { CustomMinimumSize = new Vector2(130, 44) };
        _yes.Pressed += Confirm;
        yesNo.AddChild(_yes);
        _no = new Button { CustomMinimumSize = new Vector2(130, 44) };
        _no.Pressed += () =>
        {
            Asking = null;
            Refresh();
        };
        yesNo.AddChild(_no);
        _message = new Label { AutowrapMode = TextServer.AutowrapMode.WordSmart, CustomMinimumSize = new Vector2(PanelWidth, 0) };
        _message.AddThemeFontSizeOverride("font_size", 13);
        side2.AddChild(_message);
        // The legend: the five states and what is to come (L5 b, L10 c), then the glossary (M7 a).
        var legendScroll = new ScrollContainer { CustomMinimumSize = new Vector2(PanelWidth, 120), SizeFlagsVertical = Control.SizeFlags.ExpandFill, HorizontalScrollMode = ScrollContainer.ScrollMode.Disabled, Visible = false };
        _legendBox = legendScroll;
        side2.AddChild(legendScroll);
        var legend = new VBoxContainer();
        legendScroll.AddChild(legend);
        _legendTitle = new Label();
        _legendTitle.AddThemeFontSizeOverride("font_size", 14);
        legend.AddChild(_legendTitle);
        foreach (NodeState? state in Enum.GetValues<NodeState>().Select(s => (NodeState?)s).Append(null))
        {
            var line = new HBoxContainer();
            line.AddThemeConstantOverride("separation", 8);
            legend.AddChild(line);
            line.AddChild(new LegendDot { State = state, CustomMinimumSize = new Vector2(20, 20) });
            var text = new Label();
            text.AddThemeFontSizeOverride("font_size", 12);
            line.AddChild(text);
            _legend.Add((state, text));
        }
        _glossary = new Label { AutowrapMode = TextServer.AutowrapMode.WordSmart, CustomMinimumSize = new Vector2(PanelWidth - 12, 0) };
        _glossary.AddThemeFontSizeOverride("font_size", 12);
        legend.AddChild(_glossary);
        _keys = new Label { AutowrapMode = TextServer.AutowrapMode.WordSmart, CustomMinimumSize = new Vector2(PanelWidth, 0), Modulate = new Color(1, 1, 1, 0.6f) };
        _keys.AddThemeFontSizeOverride("font_size", 11);
        side2.AddChild(_keys);
        var buttons = new HBoxContainer();
        buttons.AddThemeConstantOverride("separation", 8);
        side2.AddChild(buttons);
        _save = new Button { CustomMinimumSize = new Vector2(136, 44) };
        _save.Pressed += Save;
        buttons.AddChild(_save);
        _close = new Button { CustomMinimumSize = new Vector2(136, 44) };
        _close.Pressed += () => ClosePressed?.Invoke();
        buttons.AddChild(_close);
        _undo = new Button { CustomMinimumSize = new Vector2(PanelWidth, 44) };
        _undo.Pressed += Undo;
        side2.AddChild(_undo);

        Lay(TreeLayout.FocusOf(editor.TalentsTaken, editor.Data));
        Focused = Layout.Start(editor.TalentsTaken, editor.Data);
        Refresh();
    }

    /// <summary>The legend and the glossary in place of the node's card, or the card again.</summary>
    public void ShowLegend(bool shown)
    {
        _legendBox.Visible = shown;
        _detailBox.Visible = !shown;
        _help.SetPressedNoSignal(shown);
    }

    /// <summary>
    /// The three guided bubbles (sprint 149, M6 a), at the tree's first opening from level 30: the trunk, the fork, the
    /// styles, each pointing at its layer; the legend opened with them (M5 b).
    /// </summary>
    public void StartTutorial()
    {
        if (_bubble is not null)
            return;
        _bubble = new PanelContainer { Size = new Vector2(320, 0) };
        _bubble.AddThemeStyleboxOverride("panel", new StyleBoxFlat
        {
            BgColor = new Color(ElementStyle.OpenFill),
            BorderColor = new Color(ElementStyle.Taken),
            BorderWidthLeft = 2,
            BorderWidthRight = 2,
            BorderWidthTop = 2,
            BorderWidthBottom = 2,
            CornerRadiusTopLeft = 8,
            CornerRadiusTopRight = 8,
            CornerRadiusBottomLeft = 8,
            CornerRadiusBottomRight = 8,
            ContentMarginLeft = 12,
            ContentMarginRight = 12,
            ContentMarginTop = 10,
            ContentMarginBottom = 10,
        });
        AddChild(_bubble);
        var column = new VBoxContainer();
        _bubble.AddChild(column);
        _bubbleText = new Label { AutowrapMode = TextServer.AutowrapMode.WordSmart, CustomMinimumSize = new Vector2(296, 0) };
        column.AddChild(_bubbleText);
        _bubbleNext = new Button { CustomMinimumSize = new Vector2(120, 44), SizeFlagsHorizontal = Control.SizeFlags.ShrinkEnd };
        _bubbleNext.Pressed += NextBubble;
        column.AddChild(_bubbleNext);
        TutorialStep = -1;
        ShowLegend(true);
        NextBubble();
    }

    /// <summary>The next bubble, or the end of the tutorial after the third.</summary>
    public void NextBubble()
    {
        if (_bubble is null)
            return;
        TutorialStep++;
        IReadOnlyList<string> said = Texts.TreeTutorial;
        if (TutorialStep >= said.Count)
        {
            _bubble.QueueFree();
            _bubble = null;
            TutorialStep = -1;
            TutorialDone?.Invoke();
            return;
        }
        _bubbleText.Text = said[TutorialStep];
        _bubbleNext.Text = Texts[TutorialStep == said.Count - 1 ? "tree.tuto.done" : "tree.tuto.next"];
        // Over the trunk, then the fork, then the styles.
        Vector2 at = TutorialStep switch
        {
            0 => new Vector2(Layout.Nodes.First(n => n.IsTrunk).Box.X, Layout.Nodes.First(n => n.IsTrunk).Box.Y - 150),
            1 => new Vector2(Layout.Nodes.First(n => n.Branch == Branch.Fork).Box.X + 160, Layout.ForkTop - 150),
            _ => new Vector2(Layout.Columns.Values.Min(c => c.X) + 120, Layout.Top + 30),
        };
        _bubble.Position = _canvas.GetGlobalRect().Position + at;
    }

    /// <summary>Lays the tree out again, <paramref name="focus"/> deployed (a click on a folded style).</summary>
    public void Lay(Branch focus)
    {
        Layout = new TreeLayout(Editor.Data, focus, _canvas.CustomMinimumSize.X, _canvas.CustomMinimumSize.Y);
        _canvas.Rebuild();
        if (Layout[Focused] is null)
            Focused = Layout.Nodes.First(n => n.Navigable && n.Branch == focus).Id;
    }

    /// <summary>The state a node shows (L5 b); null for what is still to come.</summary>
    public NodeState? StateOf(TreeNode node)
    {
        ArgumentNullException.ThrowIfNull(node);
        if (node.IsTrunk)
            return TreeLayout.TrunkState(int.Parse(node.Id["trunk:".Length..], System.Globalization.CultureInfo.InvariantCulture), Editor.Hero.Level);
        if (node.IsFold)
            return TreeLayout.FoldState(Editor.TalentsTaken, node.Branch!.Value, node.Tier!.Value, Editor.Hero.Level, Editor.Data);
        return node.IsTalent ? TreeLayout.State(Editor.TalentsTaken, node.Id, Editor.Hero.Level, Editor.Data) : null;
    }

    /// <summary>How many nodes of the layout show each state (for the self-test).</summary>
    public IReadOnlyDictionary<NodeState, int> Counted =>
        Layout.Nodes.Select(StateOf).OfType<NodeState>().GroupBy(s => s).ToDictionary(g => g.Key, g => g.Count());

    /// <summary>The talents lit red: those giving back the one asked would give back too.</summary>
    public bool Losing(string id) => Asking is { } a && (a.Talent == id || a.Losing.Contains(id));

    /// <summary>A node clicked, or Enter on the focused one: a folded style deployed, a talent taken or given back.</summary>
    public void Press(string id)
    {
        TreeNode? node = Layout[id];
        if (node is null || !node.Navigable)
            return;
        Focused = id;
        if (node.IsFold)
        {
            Lay(node.Branch!.Value);
            Focused = Layout.Nodes.First(n => n.IsTalent && n.Branch == node.Branch && n.Tier == node.Tier).Id;
        }
        else if (node.IsTalent && !Editor.ReadOnly)
        {
            TalentSpec t = Editor.Data.Talents[id];
            Asking = null;
            if (Editor.HasTalent(t))
            {
                if (Editor.Losing(t) is { Count: > 0 } lost)
                    Asking = (id, lost);
                else
                    Editor.Drop(t);
            }
            else if (Editor.CanTake(t))
                Editor.Take(t);
            // What the change did besides: a Conversion in its Transmutation's place, twins in the deck (sprint 150, F7 a).
            IEnumerable<string> said = (Editor.Replaced is string replaced ? [Texts["talents.replaced", Editor.Data.Talents[replaced].Name.In(Texts.Lang)]] : Array.Empty<string>())
                .Concat(Texts.DeckChanges(Editor));
            _message.Text = string.Join("\n", said);
            if (_message.HasMeta("diff"))
                _message.RemoveMeta("diff");
        }
        Refresh();
    }

    /// <summary>The giving back asked for, confirmed: the talent and those hanging on it.</summary>
    public void Confirm()
    {
        if (Asking is { } a)
            Editor.Drop(Editor.Data.Talents[a.Talent]);
        Asking = null;
        Refresh();
    }

    /// <summary>Undoes the draft's talents (I4 a).</summary>
    public void Undo()
    {
        bool changed = Editor.TalentsChanged;
        Editor.UndoTalents();
        Asking = null;
        _message.Text = changed ? Texts["tree.undone"] : "";
        Refresh();
    }

    /// <summary>The draft saved, as the button or Ctrl+S would.</summary>
    public void Save()
    {
        if (!_save.Disabled)
            SavePressed?.Invoke(Editor.ToPoints());
    }

    /// <summary>The keyboard moved one node a way (K5 a).</summary>
    public void Move(Direction direction)
    {
        if (Layout.Neighbour(Focused, direction) is TreeNode next)
        {
            Focused = next.Id;
            Refresh();
        }
    }

    public void Hover(string? id)
    {
        _hovered = id;
        ShowDetail();
    }

    public override void _Input(InputEvent @event)
    {
        if (@event is not InputEventKey { Pressed: true } key || Window is null || !Window.IsVisibleInTree())
            return;
        switch (key.Keycode)
        {
            case Key.S when key.CtrlPressed:
                Save();
                break;
            case Key.Up:
                Move(Direction.Up);
                break;
            case Key.Down:
                Move(Direction.Down);
                break;
            case Key.Left:
                Move(Direction.Left);
                break;
            case Key.Right:
                Move(Direction.Right);
                break;
            case Key.Enter or Key.KpEnter or Key.Space when !key.Echo:
                Press(Focused);
                break;
            default:
                return;
        }
        GetViewport().SetInputAsHandled();
    }

    /// <summary>After a save: a new draft from what the server kept, and a message.</summary>
    public void Saved(Hero hero, string message)
    {
        Editor = new PointsEditor(hero, Editor.Data, Editor.ReadOnly);
        Asking = null;
        _message.Text = message;
        Refresh();
    }

    public void ShowMessage(string message) => _message.Text = message;

    public void Refresh()
    {
        string cls = Editor.Data.Class(Editor.Hero.Class)?.Name.In(Texts.Lang) ?? "";
        _title.Text = $"{Texts["tree.title", Editor.Hero.Level]} · {Editor.Hero.Name}, {cls}";
        IReadOnlyList<string> summary = Texts.TreeSummary(Editor);
        for (int i = 0; i < _summary.Count; i++)
            _summary[i].Text = summary[i];
        _legendTitle.Text = Texts["tree.legend"];
        _help.Text = Texts["tree.help.button"];
        _glossary.Text = Texts["tree.glossary"] + "\n" + string.Join("\n", Texts.TreeGlossary.Select(g => Texts["tree.term.line", g.Term, g.Meaning]));
        _buildTitle.Text = Texts.TreeTitle(Editor.TalentsTaken, Editor.Data);
        IReadOnlyList<string> build = [.. Texts.TreeBuildSummary(Editor.TalentsTaken, Editor.Data), Texts.TreeAdvice(Editor) ?? ""];
        for (int i = 0; i < _build.Count; i++)
            _build[i].Text = build[i];
        foreach ((NodeState? state, Label text) in _legend)
            text.Text = state is NodeState s ? Texts["tree.state." + s] : Texts["tree.coming"];
        _keys.Text = Texts["tree.keys"];
        _save.Text = Texts["points.save"];
        _close.Text = Texts["points.close"];
        _undo.Text = Texts["tree.undo"];
        _yes.Text = Texts["tree.confirm.yes"];
        _no.Text = Texts["tree.confirm.no"];
        _save.Disabled = Editor.ReadOnly || !Editor.TalentsChanged;
        _undo.Disabled = Editor.ReadOnly || !Editor.TalentsChanged;
        _confirm.Visible = Asking is not null;
        if (Asking is { } a)
            _confirmText.Text = Texts["tree.confirm", Editor.Data.Talents[a.Talent].Name.In(Texts.Lang), string.Join(", ", a.Losing.Select(id => Editor.Data.Talents[id].Name.In(Texts.Lang)))];
        // What the save would gain and lose, under the last message (N6 a).
        IReadOnlyList<string> diff = Texts.TreeDiff(Editor);
        if (diff.Count > 0 && (_message.Text.Length == 0 || _message.HasMeta("diff")))
        {
            _message.Text = string.Join("\n", diff);
            _message.SetMeta("diff", true);
        }
        else if (diff.Count == 0 && _message.HasMeta("diff"))
        {
            _message.Text = "";
            _message.RemoveMeta("diff");
        }
        ShowDetail();
        _canvas.Refresh();
    }

    private void ShowDetail()
    {
        TreeNode? node = Layout[_hovered ?? Focused];
        _detail.Text = node is null ? "" : Card(Texts.TreeCard(node, Editor));
    }

    /// <summary>
    /// A node's card in BBCode: its layer in the style's colour, the banner, the lore in italics (M8 b), then its lines; the
    /// glossary's words are links whose meaning shows on hover (M7 a).
    /// </summary>
    private string Card(TreeCard card)
    {
        static string Escape(string s) => s.Replace("[", "[lb]", StringComparison.Ordinal);
        string Linked(string s)
        {
            string text = Escape(s);
            foreach ((string term, _) in Texts.TreeGlossary)
            {
                int at = text.IndexOf(term, StringComparison.OrdinalIgnoreCase);
                if (at >= 0)
                    text = text[..at] + $"[url={term}][color={ElementStyle.Cost}]" + text.Substring(at, term.Length) + "[/color][/url]" + text[(at + term.Length)..];
            }
            return text;
        }
        var lines = new List<string> { $"[color={ElementStyle.Taken}][b]{Escape(card.Layer)}[/b][/color]", $"[color={ElementStyle.Locked}]{Linked(card.Banner)}[/color]" };
        if (card.Lore.Length > 0)
            lines.Add($"[i]{Escape(card.Lore)}[/i]");
        lines.AddRange(card.Lines.Select(Linked));
        return string.Join("\n", lines);
    }
}

/// <summary>The tree's area: the background, the links and the names drawn, one <see cref="TreeNodeView"/> a node.</summary>
public partial class TreeCanvas : Control
{
    public TreePanel Panel { get; set; } = null!;

    private readonly Dictionary<string, (TreeNodeView View, Label Name)> _views = [];
    private Label _styles = null!, _fork = null!, _trunk = null!;
    private readonly List<(Branch Branch, Label Name)> _columns = [];
    private readonly List<Label> _choices = [];

    /// <summary>The tree's choices, framed together: the Transmutation's two, the four Hearts of Pure element.</summary>
    private static IEnumerable<(string Key, string[] Ids)> Choices(GameData data)
    {
        yield return ("tree.choice.transmute", ["transmute-air-lightning", "transmute-fire-lightning"]);
        yield return ("tree.choice.heart", [.. data.Talents.Values.Where(t => t.Branch == Branch.Pure).Select(t => t.Id)]);
    }

    public void Rebuild()
    {
        foreach (Node child in GetChildren())
        {
            RemoveChild(child);
            child.QueueFree();
        }
        _views.Clear();
        _columns.Clear();
        _choices.Clear();
        TreeLayout layout = Panel.Layout;
        _styles = Caption(new Vector2(layout.Columns.Values.Min(c => c.X), layout.Top - 24), 13, new Color(ElementStyle.Open));
        _fork = Caption(new Vector2(0, layout.ForkTop + TreeLayout.Large + TreeLayout.Label + 8), 12, new Color(1, 1, 1, 0.6f));
        // The two choices' captions: the Transmutation over its two, Pure element over its Hearts.
        foreach ((string key, string[] ids) in Choices(Panel.Editor.Data))
        {
            Box[] boxes = [.. ids.Select(id => layout[id]).OfType<TreeNode>().Select(n => n.Box)];
            if (boxes.Length == 0)
                continue;
            // The Hearts' under their names, out of the bus's way; the Transmutation's over its two.
            bool hearts = key == "tree.choice.heart";
            float y = hearts ? boxes.Max(b => b.Y + b.H) + TreeLayout.Label + 6 : boxes.Min(b => b.Y) - 22;
            Label choice = Caption(new Vector2(Mathf.Max(0, boxes.Min(b => b.X) - 6), y), 11, new Color(1, 1, 1, 0.75f));
            choice.SetMeta("key", key);
            _choices.Add(choice);
        }
        TreeNode first = layout.Nodes.First(n => n.IsTrunk);
        _trunk = Caption(new Vector2(Mathf.Max(0, first.Box.X - 150), first.Box.Y + 18), 12, new Color(1, 1, 1, 0.6f));
        foreach ((Branch branch, Box column) in layout.Columns)
        {
            Label name = Caption(new Vector2(column.X - 10, layout.Top), 13, new Color(ElementStyle.StyleColour(branch)));
            name.Size = new Vector2(column.W + 20, 20);
            name.HorizontalAlignment = HorizontalAlignment.Center;
            _columns.Add((branch, name));
        }
        foreach (TreeNode n in layout.Nodes)
        {
            var view = new TreeNodeView { Node = n, Panel = Panel, Position = new Vector2(n.Box.X, n.Box.Y), Size = new Vector2(n.Box.W, n.Box.H) };
            AddChild(view);
            float labelWidth = n.Shape == NodeShape.Trunk ? 150 : n.Box.W + 8;
            var name = new Label
            {
                Position = new Vector2(n.Box.CenterX - (labelWidth / 2), n.Box.Y + n.Box.H + 1),
                Size = new Vector2(labelWidth, TreeLayout.Label),
                HorizontalAlignment = HorizontalAlignment.Center,
                ClipText = true,
                TextOverrunBehavior = TextServer.OverrunBehavior.TrimEllipsis,
                MouseFilter = MouseFilterEnum.Ignore,
            };
            name.AddThemeFontSizeOverride("font_size", 11);
            AddChild(name);
            _views[n.Id] = (view, name);
        }
    }

    private Label Caption(Vector2 at, int size, Color colour)
    {
        var label = new Label { Position = at, MouseFilter = MouseFilterEnum.Ignore };
        label.AddThemeFontSizeOverride("font_size", size);
        label.AddThemeColorOverride("font_color", colour);
        AddChild(label);
        return label;
    }

    public void Refresh()
    {
        TreeLayout layout = Panel.Layout;
        Texts texts = Panel.Texts;
        _styles.Text = texts["tree.styles"];
        _fork.Text = texts["tree.fork"];
        _trunk.Text = texts["tree.trunk"];
        foreach ((Branch branch, Label name) in _columns)
            name.Text = texts["talents.branch." + branch];
        foreach (Label choice in _choices)
            choice.Text = texts[(string)choice.GetMeta("key")];
        foreach ((string id, (TreeNodeView view, Label name)) in _views)
        {
            TreeNode n = layout[id]!;
            name.Text = texts.TreeName(n, Panel.Editor.Data);
            name.Modulate = Panel.StateOf(n) is NodeState.Taken or NodeState.Open ? Colors.White : new Color(1, 1, 1, 0.6f);
            view.TooltipText = n.IsComing ? "" : name.Text;
            view.QueueRedraw();
        }
        QueueRedraw();
    }

    public override void _Draw()
    {
        TreeLayout layout = Panel.Layout;
        // An abstract background in the chart's colours (K9 a): a dark ground, two faint rings round the fork.
        DrawRect(new Rect2(Vector2.Zero, Size), new Color(ElementStyle.Ground));
        var heart = new Vector2((layout.ForkRight / 2) + 30, layout.ForkTop + 36);
        DrawArc(heart, 240, 0, Mathf.Tau, 96, new Color(0.745f, 0.953f, 0.455f, 0.05f), 40);
        DrawArc(heart, 420, 0, Mathf.Tau, 128, new Color(0.353f, 0.227f, 0.133f, 0.12f), 60);
        GameData data = Panel.Editor.Data;
        IReadOnlyList<string> taken = Panel.Editor.TalentsTaken;
        // The trunk to the fork.
        TreeNode top = layout.Nodes.Where(n => n.IsTrunk).OrderBy(n => n.Box.Y).First();
        foreach (TreeNode t in layout.Nodes.Where(n => n.IsTrunk))
            Dashed(Centre(t), new Vector2(Centre(t).X, layout.ForkTop + TreeLayout.Large + TreeLayout.Label + 18), new Color(1, 1, 1, 0.12f), 1);
        // A style's first floor asks for any door: one bus over the fork, the doors taken up to it, each style down from it
        // (pistachio from a door taken, dotted without one; L6 a). Alchemy asks for a pair.
        float bus = layout.ForkTop - 8;
        TreeNode[] doorsTaken = [.. taken.Where(id => data.Talents[id].IsFork && layout[id] is not null).Select(id => layout[id]!)];
        bool pair = taken.Any(id => data.Talents[id].IsPair);
        TreeNode[] firsts = [.. layout.Nodes.Where(n => n.Tier == Tier.First && n.Branch is Branch b && b > Branch.Braids)];
        float busLeft = firsts.Select(n => n.Box.CenterX).Concat(doorsTaken.Select(d => d.Box.CenterX)).Min();
        float busRight = firsts.Select(n => n.Box.CenterX).Concat(doorsTaken.Select(d => d.Box.CenterX)).Max();
        Color lit = new(ElementStyle.Taken);
        if (doorsTaken.Length > 0)
            DrawLine(new Vector2(busLeft, bus), new Vector2(busRight, bus), lit, 2, true);
        else
            Dashed(new Vector2(busLeft, bus), new Vector2(busRight, bus), new Color(1, 1, 1, 0.25f), 2);
        foreach (TreeNode d in doorsTaken)
            DrawLine(new Vector2(d.Box.CenterX, d.Box.Y), new Vector2(d.Box.CenterX, bus), lit, 2, true);
        foreach (TreeNode n in firsts)
        {
            var from = new Vector2(n.Box.CenterX, bus);
            var to = new Vector2(n.Box.CenterX, n.Box.Y + n.Box.H);
            bool open = doorsTaken.Length > 0 && (n.Branch != Branch.Alchemy || pair);
            if (open && Panel.StateOf(n) == NodeState.Taken)
                DrawLine(from, to, lit, 3, true);
            else if (open)
                DrawLine(from, to, new Color(0.88f, 0.88f, 0.88f, 0.6f), 2, true);
            else
                Dashed(from, to, new Color(1, 1, 1, 0.25f), 2);
        }
        // The links: both ends taken in pistachio, the child open in grey, the rest dotted (L6 a); red what would be given back.
        foreach ((string from, string to) in layout.Links(taken, data))
        {
            // A door to a style goes by the bus; a parent above its child (the Conversion over Prism) is not drawn back down.
            if (data.Talents.TryGetValue(from, out TalentSpec? f) && f.IsFork && layout[to]!.Branch is Branch s && s > Branch.Braids)
                continue;
            if (layout[from]!.Box.Y < layout[to]!.Box.Y)
                continue;
            TreeNode a = layout[from]!, b = layout[to]!;
            NodeState? sa = Panel.StateOf(a), sb = Panel.StateOf(b);
            Vector2 p = new(a.Box.CenterX, a.Box.Y), q = new(b.Box.CenterX, b.Box.Y + b.Box.H);
            if (Panel.Losing(to))
                DrawLine(p, q, new Color(ElementStyle.Losing), 3, true);
            else if (sa == NodeState.Taken && sb == NodeState.Taken)
                DrawLine(p, q, lit, 3, true);
            else if (sa == NodeState.Taken && sb == NodeState.Open)
                DrawLine(p, q, new Color(0.88f, 0.88f, 0.88f, 0.7f), 2, true);
            else
                Dashed(p, q, new Color(1, 1, 1, 0.3f), 2);
        }
        // The choices (the Transmutation, the Hearts) framed together.
        foreach ((_, string[] choice) in Choices(data))
        {
            Box[] boxes = [.. choice.Select(id => layout[id]).OfType<TreeNode>().Select(n => n.Box)];
            if (boxes.Length < 2)
                continue;
            float x0 = boxes.Min(b => b.X) - 5, y0 = boxes.Min(b => b.Y) - 5, x1 = boxes.Max(b => b.X + b.W) + 5, y1 = boxes.Max(b => b.Y + b.H) + 5;
            var frame = new Rect2(x0, y0, x1 - x0, y1 - y0);
            DrawStyleBox(new StyleBoxFlat { BgColor = new Color(1, 1, 1, 0.03f), BorderColor = new Color(1, 1, 1, 0.25f), BorderWidthLeft = 1, BorderWidthRight = 1, BorderWidthTop = 1, BorderWidthBottom = 1, CornerRadiusTopLeft = 8, CornerRadiusTopRight = 8, CornerRadiusBottomLeft = 8, CornerRadiusBottomRight = 8 }, frame);
        }
        // The couples side by side (C7): a brace of chocolate under each.
        for (int i = 0; i + 1 < TreeLayout.Pairs.Count; i += 2)
        {
            Box l = layout[TreeLayout.Pairs[i]]!.Box, r = layout[TreeLayout.Pairs[i + 1]]!.Box;
            float y = l.Y + l.H + TreeLayout.Label + 1;
            DrawPolyline([new Vector2(l.X, y - 5), new Vector2(l.X, y), new Vector2(r.X + r.W, y), new Vector2(r.X + r.W, y - 5)], new Color(ElementStyle.Brace), 2);
        }
    }

    private static Vector2 Centre(TreeNode n) => new(n.Box.CenterX, n.Box.CenterY);

    private void Dashed(Vector2 from, Vector2 to, Color colour, float width) => DrawDashedLine(from, to, colour, width, 6);
}

/// <summary>One node of the tree, drawn by its shape and its state; a click presses it, the mouse over it shows it in the panel.</summary>
public partial class TreeNodeView : Control
{
    public TreeNode Node { get; set; } = null!;

    public TreePanel Panel { get; set; } = null!;

    public override void _Ready()
    {
        MouseFilter = Node.IsComing ? MouseFilterEnum.Ignore : MouseFilterEnum.Stop;
        MouseDefaultCursorShape = Node.IsComing ? CursorShape.Arrow : CursorShape.PointingHand;
        MouseEntered += () => Panel.Hover(Node.Id);
        MouseExited += () => Panel.Hover(null);
    }

    public override void _GuiInput(InputEvent @event)
    {
        if (@event is InputEventMouseButton { Pressed: true, ButtonIndex: MouseButton.Left })
        {
            Panel.Press(Node.Id);
            AcceptEvent();
        }
    }

    // Kept for the whole game, as the spells' icons are (D67: a texture held by nothing was freed, a white square drawn).
    private static readonly Dictionary<string, Texture2D?> Icons = [];

    /// <summary>A node's drawing (sprint 149, P3 b): the talent's, or the trunk's node's (<c>trunk-30</c>); null without one.</summary>
    public static Texture2D? IconOf(TreeNode node)
    {
        ArgumentNullException.ThrowIfNull(node);
        return node.IsTalent ? IconOf(node.Id) : node.IsTrunk ? IconOf(node.Id.Replace(':', '-')) : null;
    }

    /// <summary>A talent's drawing by its id (the fight's lit talents too, sprint 150).</summary>
    public static Texture2D? IconOf(string name)
    {
        if (Icons.TryGetValue(name, out Texture2D? known))
            return known;
        string path = $"res://assets/icons/talents/{name}.svg";
        return Icons[name] = ResourceLoader.Exists(path) ? GD.Load<Texture2D>(path) : null;
    }

    public override void _Draw()
    {
        NodeState? state = Panel.StateOf(Node);
        bool focused = Panel.Focused == Node.Id;
        bool red = Panel.Losing(Node.Id);
        TreeShapes.Draw(this, Node.Shape, state, Size, red);
        if (focused)
            DrawRect(new Rect2(-4, -4, Size.X + 8, Size.Y + 8), new Color(ElementStyle.Taken), false, 2);
        if (Node.IsComing)
            return;
        GameData data = Panel.Editor.Data;
        if (state == NodeState.Excluded && !red)
        {
            float k = Size.X * 0.22f;
            DrawLine(new Vector2(k, k), Size - new Vector2(k, k), new Color(ElementStyle.Excluded), 3, true);
            DrawLine(new Vector2(Size.X - k, k), new Vector2(k, Size.Y - k), new Color(ElementStyle.Excluded), 3, true);
            return;
        }
        Color ink = state is NodeState.Taken ? new Color(ElementStyle.Taken) : state is NodeState.Open ? new Color(ElementStyle.Open) : new Color(ElementStyle.Open, 0.4f);
        if (IconOf(Node) is Texture2D icon)
        {
            // The drawing, white in its file, tinted by the state.
            float side = Size.X * 0.56f;
            DrawTextureRect(icon, new Rect2((Size - new Vector2(side, side)) / 2, new Vector2(side, side)), false, ink);
        }
        else
        {
            Font font = ThemeDB.FallbackFont;
            Vector2 measured = font.GetStringSize("···", HorizontalAlignment.Left, -1, 17);
            DrawString(font, new Vector2((Size.X - measured.X) / 2, (Size.Y / 2) + 6), "···", HorizontalAlignment.Left, -1, 17, ink);
        }
        if (!Node.IsTalent)
            return;
        TalentSpec t = data.Talents[Node.Id];
        Font small = ThemeDB.FallbackFont;
        // The cost only when it is 2 (L7 a); the level a key waits for.
        if (t.Cost == 2)
            DrawString(small, new Vector2(Size.X - 16, 14), "2", HorizontalAlignment.Left, -1, 12, new Color(ElementStyle.Cost));
        if (state == NodeState.Level && t.Level > 0)
            DrawString(small, new Vector2(4, Size.Y - 6), t.Level.ToString(System.Globalization.CultureInfo.InvariantCulture), HorizontalAlignment.Left, -1, 11, new Color(ElementStyle.Open, 0.6f));
        // The elements in colour as well as in words (L9 a): a pair's two, a Heart's one, at the foot of the node.
        Element[] elements = TreeGuide.PairOf(t, data) is ReactionSpec pair ? [pair.First, pair.Second]
            : [.. t.Rules.OfType<ElementRule>().Where(_ => t.Branch == Branch.Pure).Select(r => r.Element)];
        for (int i = 0; i < elements.Length; i++)
        {
            var at = new Vector2((Size.X / 2) + ((i - ((elements.Length - 1) / 2f)) * 14), Size.Y - 7);
            DrawCircle(at, 5, new Color(ElementStyle.Colour(elements[i])));
            DrawArc(at, 5, 0, Mathf.Tau, 16, new Color(ElementStyle.Ground), 1, true);
        }
        // A node made for playing together (A7 a): two small heads at the top left.
        if (t.Branch == Branch.Support || t.Id == "shared-mark")
        {
            Color head = new(ElementStyle.StyleColour(Branch.Support));
            DrawCircle(new Vector2(8, 9), 3.5f, head);
            DrawCircle(new Vector2(15, 9), 3.5f, head);
            DrawArc(new Vector2(11.5f, 17), 6, Mathf.Pi, Mathf.Tau, 8, head, 2);
        }
    }
}

/// <summary>A swatch of the legend: a small node in a state, or dotted for what is to come.</summary>
public partial class LegendDot : Control
{
    public NodeState? State { get; set; }

    public override void _Draw() => TreeShapes.Draw(this, NodeShape.Node, State, Size, false);
}

/// <summary>How a node is drawn: a circle (a trunk's node, a door, a node) or a rounded square (a key), by its state.</summary>
public static class TreeShapes
{
    public static void Draw(Control on, NodeShape shape, NodeState? state, Vector2 size, bool red)
    {
        (Color fill, Color line, float width, bool dotted) = red ? (new Color(ElementStyle.LosingFill), new Color(ElementStyle.Losing), 3f, false) : state switch
        {
            NodeState.Taken => (new Color(ElementStyle.TakenFill), new Color(ElementStyle.Taken), 3f, false),
            NodeState.Open => (new Color(ElementStyle.OpenFill), new Color(ElementStyle.Open), 2f, false),
            NodeState.Excluded => (new Color(ElementStyle.ExcludedFill), new Color(ElementStyle.Excluded), 2f, false),
            NodeState.Locked => (new Color(ElementStyle.LockedFill), new Color(ElementStyle.Locked), 2f, true),
            NodeState.Level => (new Color(ElementStyle.OutOfLevelFill), new Color(ElementStyle.OutOfLevel), 2f, true),
            _ => (new Color(0, 0, 0, 0), new Color(ElementStyle.Coming), 2f, true),
        };
        Vector2 c = size / 2;
        float r = (Mathf.Min(size.X, size.Y) / 2) - width;
        if (shape == NodeShape.Key)
        {
            var box = new Rect2(new Vector2(width, width), size - new Vector2(2 * width, 2 * width));
            on.DrawRect(box, fill);
            if (dotted)
            {
                Vector2[] corners = [box.Position, box.Position + new Vector2(box.Size.X, 0), box.End, box.Position + new Vector2(0, box.Size.Y)];
                for (int i = 0; i < 4; i++)
                    on.DrawDashedLine(corners[i], corners[(i + 1) % 4], line, width, 5);
            }
            else
                on.DrawRect(box, line, false, width);
            return;
        }
        on.DrawCircle(c, r, fill);
        if (!dotted)
        {
            on.DrawArc(c, r, 0, Mathf.Tau, 48, line, width, true);
            return;
        }
        // A dotted ring: short arcs with gaps.
        int dashes = Math.Max(8, (int)(r / 2.5f));
        for (int i = 0; i < dashes; i++)
        {
            float a = i * Mathf.Tau / dashes;
            on.DrawArc(c, r, a, a + (Mathf.Tau / dashes * 0.55f), 6, line, width, true);
        }
    }
}
