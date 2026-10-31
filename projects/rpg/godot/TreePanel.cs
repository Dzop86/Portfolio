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
/// from the Talents tab): read from the bottom up, the trunk, the fork, the Lightning braid and the six styles; since sprint
/// 176 (D87) the whole espalier deployed (<see cref="TreeLayout"/>) under a camera that zooms and is dragged
/// (<see cref="TreeCamera"/>), its wood alive in pistachio, reachable in grey, bare dotted, never crossing; each node in one
/// of four looks before the click (sprint 164). On the right, the panel (K7 b): the points, the elements
/// beyond the base, the big switch, the node under the mouse or the keyboard, the legend, save (Ctrl+S too), undo and close.
/// A click or Enter takes a node or gives it back; giving one back that others hang on shows them in red first and asks
/// (I1 b). It only draws <see cref="PointsEditor"/>, <see cref="TreeLayout"/> and <see cref="Texts"/>.
/// </summary>
public partial class TreePanel : CanvasLayer, IPointsWindow
{
    private const float PanelWidth = 304, FoldWidth = 28;
    private TreeCanvas _canvas = null!;
    private Label _title = null!, _counter = null!, _message = null!, _keys = null!, _confirmText = null!, _legendTitle = null!, _buildTitle = null!, _glossary = null!;
    private RichTextLabel _detail = null!;
    private readonly List<Label> _summary = [], _build = [];
    private readonly List<(string Key, Label Head)> _heads = [];
    private Control _detailBox = null!, _legendBox = null!, _side = null!, _codeRow = null!;
    private Button _help = null!, _fold = null!, _load = null!;
    private MenuButton _tools = null!;
    private PanelContainer? _bubble;
    private LineEdit _code = null!;
    private Vector2 _narrow;

    // The tools' menu: a suggested build each from 0, then planning, copying and loading a code.
    private const int ToolPlan = 100, ToolCopy = 101, ToolCode = 102;
    private PointsEditor? _real;
    private Label _bubbleText = null!;
    private Button _bubbleNext = null!;
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

    private readonly List<(NodeLook Look, Label Text)> _looks = [];

    /// <summary>The legend's lines shown (for the self-test: the four looks, always there).</summary>
    public int LooksShown => _looks.Count(l => l.Text.IsVisibleInTree() && l.Text.Text.Length > 0);

    /// <summary>The legend and the glossary shown in place of the node's card (M5 b, behind "?").</summary>
    public bool LegendShown => _legendBox.Visible;

    /// <summary>Planning at <see cref="TreePlan.PlanLevel"/> (sprint 151, N2 a): the real draft kept aside, nothing saved.</summary>
    public bool Planning => _real is not null;

    /// <summary>The nodes of the plan beyond the real draft, drawn as ghosts.</summary>
    public IReadOnlyList<string> Ghosts => _real is null ? [] : TreePlan.Ghosts(Editor.TalentsTaken, _real.TalentsTaken);

    /// <summary>The path the node under the mouse asks (L8 a), lit on the tree.</summary>
    public IReadOnlyList<string> Path { get; private set; } = [];

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
        foreach (string edge in new[] { "left", "right", "top", "bottom" })
            margin.AddThemeConstantOverride("margin_" + edge, 12);
        panel.AddChild(margin);
        var column = new VBoxContainer();
        column.AddThemeConstantOverride("separation", 8);
        margin.AddChild(column);

        // The top bar (sprint 177, A6, s164-arbre-maquette.md §3.12): the title, the points to place in large, then the tools,
        // "?", undo, save and close.
        var bar = new HBoxContainer { CustomMinimumSize = new Vector2(0, 44) };
        bar.AddThemeConstantOverride("separation", 8);
        column.AddChild(bar);
        _title = new Label { SizeFlagsHorizontal = Control.SizeFlags.ExpandFill, ClipText = true, TextOverrunBehavior = TextServer.OverrunBehavior.TrimEllipsis, VerticalAlignment = VerticalAlignment.Center };
        _title.AddThemeFontSizeOverride("font_size", 18);
        _title.AddThemeFontOverride("font", OsmoseTheme.Bold());
        bar.AddChild(_title);
        _counter = new Label { VerticalAlignment = VerticalAlignment.Center };
        _counter.AddThemeFontSizeOverride("font_size", 17);
        _counter.AddThemeColorOverride("font_color", new Color(ElementStyle.Taken));
        bar.AddChild(_counter);
        _tools = new MenuButton { CustomMinimumSize = new Vector2(110, 44), Flat = false, FocusMode = Control.FocusModeEnum.None };
        _tools.GetPopup().IdPressed += id => Tool((int)id);
        bar.AddChild(_tools);
        _help = new Button { ToggleMode = true, CustomMinimumSize = new Vector2(44, 44), FocusMode = Control.FocusModeEnum.None };
        _help.Toggled += shown => ShowLegend(shown);
        bar.AddChild(_help);
        _undo = new Button { CustomMinimumSize = new Vector2(120, 44), FocusMode = Control.FocusModeEnum.None };
        _undo.Pressed += Undo;
        bar.AddChild(_undo);
        _save = new Button { CustomMinimumSize = new Vector2(120, 44), ThemeTypeVariation = OsmoseTheme.Primary, FocusMode = Control.FocusModeEnum.None };
        _save.Pressed += Save;
        bar.AddChild(_save);
        _close = new Button { Text = "×", CustomMinimumSize = new Vector2(44, 44), ThemeTypeVariation = OsmoseTheme.CloseButton, FocusMode = Control.FocusModeEnum.None };
        _close.Pressed += () => ClosePressed?.Invoke();
        bar.AddChild(_close);

        // The tree, the fold's button, and the right panel (it folds to give the tree the window's width).
        var row = new HBoxContainer { SizeFlagsVertical = Control.SizeFlags.ExpandFill };
        row.AddThemeConstantOverride("separation", 4);
        column.AddChild(row);
        _narrow = new Vector2(size.X - 24 - PanelWidth - FoldWidth - 8, size.Y - 24 - 44 - 8);
        _canvas = new TreeCanvas { Panel = this, CustomMinimumSize = CanvasSize(WindowState.TreeSideFolded), ClipContents = true };
        row.AddChild(_canvas);
        _fold = new Button { CustomMinimumSize = new Vector2(FoldWidth, 44), SizeFlagsVertical = Control.SizeFlags.ShrinkCenter, FocusMode = Control.FocusModeEnum.None };
        _fold.Pressed += () => Fold(!WindowState.TreeSideFolded);
        row.AddChild(_fold);
        var side = new VBoxContainer { CustomMinimumSize = new Vector2(PanelWidth, 0), Visible = !WindowState.TreeSideFolded };
        side.AddThemeConstantOverride("separation", 6);
        _side = side;
        row.AddChild(side);

        // 1. My build: its title, beyond the base, the big switch, what it pays, its best hit, the pair advised (N8 a, N1 a, C5 b).
        side.AddChild(Head("tree.block.build"));
        _buildTitle = new Label { AutowrapMode = TextServer.AutowrapMode.WordSmart, CustomMinimumSize = new Vector2(PanelWidth, 0) };
        _buildTitle.AddThemeFontSizeOverride("font_size", 16);
        _buildTitle.AddThemeFontOverride("font", OsmoseTheme.Bold());
        side.AddChild(_buildTitle);
        for (int i = 0; i < 2; i++)
        {
            var line = new Label { AutowrapMode = TextServer.AutowrapMode.WordSmart, CustomMinimumSize = new Vector2(PanelWidth, 0) };
            line.AddThemeFontSizeOverride("font_size", 13);
            side.AddChild(line);
            _summary.Add(line);
        }
        for (int i = 0; i < 3; i++)
        {
            var line = new Label { AutowrapMode = TextServer.AutowrapMode.WordSmart, CustomMinimumSize = new Vector2(PanelWidth, 0), Modulate = new Color(1, 1, 1, 0.8f) };
            line.AddThemeFontSizeOverride("font_size", 12);
            side.AddChild(line);
            _build.Add(line);
        }
        // 2. The node under the mouse or the keyboard: its card, the glossary's words lit (M7 a); behind "?", the glossary.
        side.AddChild(Head("tree.block.node"));
        var detailBox = new ScrollContainer { CustomMinimumSize = new Vector2(PanelWidth, 64), SizeFlagsVertical = Control.SizeFlags.ExpandFill, HorizontalScrollMode = ScrollContainer.ScrollMode.Disabled };
        _detailBox = detailBox;
        side.AddChild(detailBox);
        _detail = new RichTextLabel { BbcodeEnabled = true, FitContent = true, ScrollActive = false, CustomMinimumSize = new Vector2(PanelWidth - 12, 0), MetaUnderlined = false };
        _detail.AddThemeFontSizeOverride("normal_font_size", 13);
        _detail.AddThemeFontSizeOverride("italics_font_size", 13);
        _detail.AddThemeFontSizeOverride("bold_font_size", 14);
        _detail.MetaHoverStarted += meta => _detail.TooltipText = Texts.TreeGlossary.FirstOrDefault(g => g.Term == meta.AsString()).Meaning ?? "";
        _detail.MetaHoverEnded += _ => _detail.TooltipText = "";
        detailBox.AddChild(_detail);
        var legendScroll = new ScrollContainer { CustomMinimumSize = new Vector2(PanelWidth, 64), SizeFlagsVertical = Control.SizeFlags.ExpandFill, HorizontalScrollMode = ScrollContainer.ScrollMode.Disabled, Visible = false };
        _legendBox = legendScroll;
        side.AddChild(legendScroll);
        _glossary = new Label { AutowrapMode = TextServer.AutowrapMode.WordSmart, CustomMinimumSize = new Vector2(PanelWidth - 12, 0) };
        _glossary.AddThemeFontSizeOverride("font_size", 12);
        legendScroll.AddChild(_glossary);
        // 3. To save: what the save would gain and lose (N6 a), a message, the giving back asked (I1 b), a build's code to load.
        side.AddChild(Head("tree.block.save"));
        _message = new Label { AutowrapMode = TextServer.AutowrapMode.WordSmart, CustomMinimumSize = new Vector2(PanelWidth, 0) };
        _message.AddThemeFontSizeOverride("font_size", 13);
        side.AddChild(_message);
        var confirm = new VBoxContainer { Visible = false };
        _confirm = confirm;
        side.AddChild(confirm);
        _confirmText = new Label { AutowrapMode = TextServer.AutowrapMode.WordSmart, CustomMinimumSize = new Vector2(PanelWidth, 0) };
        _confirmText.AddThemeColorOverride("font_color", new Color(ElementStyle.Losing));
        confirm.AddChild(_confirmText);
        var yesNo = new HBoxContainer();
        yesNo.AddThemeConstantOverride("separation", 8);
        confirm.AddChild(yesNo);
        _yes = new Button { CustomMinimumSize = new Vector2(140, 44) };
        _yes.Pressed += Confirm;
        yesNo.AddChild(_yes);
        _no = new Button { CustomMinimumSize = new Vector2(140, 44) };
        _no.Pressed += () =>
        {
            Asking = null;
            Refresh();
        };
        yesNo.AddChild(_no);
        var codeRow = new HBoxContainer { Visible = false };
        codeRow.AddThemeConstantOverride("separation", 6);
        _codeRow = codeRow;
        side.AddChild(codeRow);
        _code = new LineEdit { CustomMinimumSize = new Vector2(0, 44), SizeFlagsHorizontal = Control.SizeFlags.ExpandFill };
        codeRow.AddChild(_code);
        _load = new Button { CustomMinimumSize = new Vector2(90, 44) };
        _load.Pressed += () => LoadCode(_code.Text);
        codeRow.AddChild(_load);

        // The four looks' legend, always there, over the tree at its bottom right (docs/arbre-lisible.md §2.8), the keys under it.
        var legendCard = new PanelContainer { MouseFilter = Control.MouseFilterEnum.Ignore };
        legendCard.AddThemeStyleboxOverride("panel", OsmoseTheme.Flat(UiStyle.Well, corner: 8, margin: 10));
        var looks = new VBoxContainer { MouseFilter = Control.MouseFilterEnum.Ignore };
        looks.AddThemeConstantOverride("separation", 2);
        legendCard.AddChild(looks);
        _legendTitle = new Label();
        _legendTitle.AddThemeFontSizeOverride("font_size", 13);
        _legendTitle.AddThemeColorOverride("font_color", new Color(UiStyle.ChocolateLight));
        looks.AddChild(_legendTitle);
        foreach (NodeLook look in Enum.GetValues<NodeLook>())
        {
            var line = new HBoxContainer { MouseFilter = Control.MouseFilterEnum.Ignore };
            line.AddThemeConstantOverride("separation", 8);
            line.AddChild(new LegendDot { Look = look, CustomMinimumSize = new Vector2(18, 18), SizeFlagsVertical = Control.SizeFlags.ShrinkCenter });
            var text = new Label();
            text.AddThemeFontSizeOverride("font_size", 12);
            text.AddThemeColorOverride("font_color", new Color(UiStyle.Text));
            line.AddChild(text);
            looks.AddChild(line);
            _looks.Add((look, text));
        }
        _keys = new Label { AutowrapMode = TextServer.AutowrapMode.WordSmart, CustomMinimumSize = new Vector2(260, 0), Modulate = new Color(1, 1, 1, 0.6f) };
        _keys.AddThemeFontSizeOverride("font_size", 11);
        looks.AddChild(_keys);
        Layout = new TreeLayout(editor.Data);
        Focused = Layout.Start(editor.TalentsTaken, editor.Data);
        Refresh();
        _canvas.Rebuild();
        _canvas.Legend = legendCard;
        Refresh();
    }

    /// <summary>The canvas's size, the panel folded or not.</summary>
    private Vector2 CanvasSize(bool folded) => folded ? _narrow + new Vector2(PanelWidth + 4, 0) : _narrow;

    private Label Head(string key)
    {
        var head = new Label();
        head.AddThemeFontSizeOverride("font_size", 13);
        head.AddThemeFontOverride("font", OsmoseTheme.Bold());
        head.AddThemeColorOverride("font_color", new Color(UiStyle.ChocolateLight));
        _heads.Add((key, head));
        return head;
    }

    /// <summary>The right panel folded or not (sprint 177, A6), for the whole game: the tree gets the width, the overview recomputed.</summary>
    public void Fold(bool folded)
    {
        WindowState.TreeSideFolded = folded;
        _side.Visible = !folded;
        _canvas.Resize(CanvasSize(folded));
        Refresh();
    }

    /// <summary>Whether the right panel is folded (for the self-test).</summary>
    public bool Folded => !_side.Visible;

    /// <summary>An item of the tools' menu: a suggested build, planning, the code copied, the code's field.</summary>
    public void Tool(int id)
    {
        switch (id)
        {
            case ToolPlan:
                Plan(!Planning);
                break;
            case ToolCopy:
                CopyCode();
                _codeRow.Visible = true;
                break;
            case ToolCode:
                _codeRow.Visible = true;
                _code.GrabFocus();
                break;
            default:
                Suggest(id + 1);
                break;
        }
    }

    /// <summary>
    /// Planning at level 60 (N2 a), or back to the real draft: the plan starts from the draft, its nodes beyond it are
    /// ghosts, and nothing in it can be saved.
    /// </summary>
    public void Plan(bool on)
    {
        if (on == Planning)
            return;
        if (on)
        {
            _real = Editor;
            Editor = new PointsEditor(_real.Draft with { Level = Math.Max(_real.Hero.Level, TreePlan.PlanLevel), Talents = [.. _real.TalentsTaken] }, _real.Data);
            _message.Text = Texts["tree.plan.on", Editor.Hero.Level];
        }
        else
        {
            Editor = _real!;
            _real = null;
            _message.Text = "";
        }
        Asking = null;
        Refresh();
    }

    /// <summary>A suggested build of the hero's class loaded in the draft (N3 a), the list's first line being its title.</summary>
    public void Suggest(int index)
    {
        IReadOnlyList<string> builds = TreePlan.Suggested(Editor.Hero.Class);
        if (index < 1 || index > builds.Count)
            return;
        Editor.LoadTalents(Progression.Builds[builds[index - 1]]);
        _message.Text = Texts["tree.suggested.loaded", Texts.TreeTitle(Progression.Builds[builds[index - 1]], Editor.Data)];
        Asking = null;
        Refresh();
    }

    /// <summary>A build's code loaded in the draft (N4 a), or why it is refused.</summary>
    public void LoadCode(string code)
    {
        CodeRead read = BuildCode.Read(code, Editor.Hero.Level, Editor.Data);
        if (read.Talents is IReadOnlyList<string> talents)
            Editor.LoadTalents(talents);
        _message.Text = Texts.CodeRefused(read);
        Asking = null;
        Refresh();
    }

    /// <summary>The draft's code, to the clipboard.</summary>
    public string CopyCode()
    {
        string code = BuildCode.Write(Editor.TalentsTaken, Editor.Data);
        _code.Text = code;
        if (DisplayServer.GetName() != "headless")
            DisplayServer.ClipboardSet(code);
        _message.Text = Texts["tree.code.copied", code];
        return code;
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
        // The doors first, then the branches, then the trunk (§2.9): the camera brings each to the middle, the bubble over the
        // doors' crown (never on a door), under the first floor, right of the trunk.
        Vector2 at = TutorialStep switch
        {
            0 => _canvas.Show(Layout.Crown.X, TreeLayout.DoorsY) - new Vector2(160, (TreeLayout.DoorsY - TreeLayout.Foot + 8) * _canvas.Camera.Zoom),
            1 => _canvas.Show(Layout.Columns[Branch.Tempo].CenterX, TreeLayout.SecondY) + new Vector2(-160, (TreeLayout.Foot - TreeLayout.SecondY + 8) * _canvas.Camera.Zoom),
            _ => _canvas.Show(Layout.Crown.X, TreeLayout.TrunkTop) + new Vector2(150, 0),
        };
        _bubble.ResetSize();
        Vector2 room = _canvas.Size - _bubble.Size;
        if (TutorialStep == 0)
            at.Y -= _bubble.Size.Y - 60;
        _bubble.Position = _canvas.GetGlobalRect().Position + new Vector2(Math.Clamp(at.X, 0, Math.Max(0, room.X)), Math.Clamp(at.Y, 0, Math.Max(0, room.Y)));
    }

    /// <summary>The bubble shown, in the window (for the self-test: never over a door).</summary>
    public Rect2? BubbleRect => _bubble?.GetGlobalRect();

    /// <summary>The camera on the tree (for the self-test).</summary>
    public TreeCamera Camera => _canvas.Camera;

    /// <summary>The tree's area (for the self-test).</summary>
    public TreeCanvas Canvas => _canvas;

    /// <summary>The wood for the draft (sprint 176, D87), as <see cref="TreeLayout.Wood"/> gives it.</summary>
    public IReadOnlyList<WoodPiece> Wood { get; private set; } = [];

    /// <summary>The state a node shows (L5 b).</summary>
    public NodeState? StateOf(TreeNode node)
    {
        ArgumentNullException.ThrowIfNull(node);
        if (node.IsTrunk)
            return TreeLayout.TrunkState(int.Parse(node.Id["trunk:".Length..], System.Globalization.CultureInfo.InvariantCulture), Editor.Hero.Level);
        return node.IsTalent ? TreeLayout.State(Editor.TalentsTaken, node.Id, Editor.Hero.Level, Editor.Data) : null;
    }

    /// <summary>How many nodes of the layout show each state (for the self-test).</summary>
    public IReadOnlyDictionary<NodeState, int> Counted =>
        Layout.Nodes.Select(StateOf).OfType<NodeState>().GroupBy(s => s).ToDictionary(g => g.Key, g => g.Count());

    /// <summary>The talents lit red: those giving back the one asked would give back too.</summary>
    public bool Losing(string id) => Asking is { } a && (a.Talent == id || a.Losing.Contains(id));

    /// <summary>A node clicked, or Enter on the focused one: a talent taken or given back.</summary>
    public void Press(string id)
    {
        TreeNode? node = Layout[id];
        if (node is null)
            return;
        Focused = id;
        if (node.IsTalent && !Editor.ReadOnly)
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
            else if (TalentReasons.WhyNot(Editor.TalentsTaken, id, Editor.Hero.Level, Editor.Data) is TalentRefusal no)
            {
                // An open node short of points, or any node not to take yet: said at the click (docs/arbre-lisible.md §2.6).
                _message.Text = Texts.WhyNot(no, Editor.Data);
                Refresh();
                return;
            }
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
            // The camera follows the keyboard (sprint 177, A4).
            _canvas.Follow(next.Box);
        }
    }

    public void Hover(string? id)
    {
        _hovered = id;
        // The path a node not taken asks, lit, its cost in the card (L8 a).
        Path = id is not null && Layout[id] is { IsTalent: true } && !Editor.TalentsTaken.Contains(id) ? TreePlan.Path(Editor.TalentsTaken, id, Editor.Data) ?? [] : [];
        // What taking it would close, shown before the click (sprint 177, A6).
        Closing = Path.Count > 0 ? TreePlan.Closes(Editor.TalentsTaken, id!, Editor.Data) : [];
        ShowDetail();
        Wood = Layout.Wood(Editor.TalentsTaken, StateOf, Path);
        _canvas.Refresh();
    }

    /// <summary>The nodes taking the one under the mouse would close: slashed in light chocolate, named in the panel.</summary>
    public IReadOnlyList<string> Closing { get; private set; } = [];

    /// <summary>The node under the mouse (its name shows at any zoom).</summary>
    public string? Hovered => _hovered;

    /// <summary>The pieces of wood a giving back would lose, lit red: none unless one is asked (for the self-test).</summary>
    public int LostPieces => Wood.Count(p => TreeLayout.Lost(p, Editor.TalentsTaken, Losing()));

    /// <summary>The talents a giving back asked would lose: the one asked and those hanging on it.</summary>
    public IReadOnlyList<string> Losing() => Asking is { } a ? [a.Talent, .. a.Losing] : [];

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
            // The camera (sprint 177, A4): + and − at the middle, 0 the whole tree, Home back to the build.
            case Key.Plus or Key.Equal or Key.KpAdd:
                _canvas.Step(closer: true);
                break;
            case Key.Minus or Key.KpSubtract:
                _canvas.Step(closer: false);
                break;
            case Key.Key0 or Key.Kp0:
                _canvas.Overview();
                break;
            case Key.Home:
                _canvas.Recentre();
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
        // The points to place, the focus of the bar; the rest in the panel.
        _counter.Text = Planning ? Texts["tree.plan.on", Editor.Hero.Level] : summary[0];
        for (int i = 0; i < _summary.Count; i++)
            _summary[i].Text = summary[i + 1];
        foreach ((string key, Label head) in _heads)
            head.Text = Texts[key].ToUpperInvariant();
        _legendTitle.Text = Texts["tree.legend"].ToUpperInvariant();
        foreach ((NodeLook look, Label text) in _looks)
            text.Text = Texts["tree.look." + look];
        _help.Text = Texts["tree.help.button"];
        _glossary.Text = Texts["tree.glossary"] + "\n" + string.Join("\n", Texts.TreeGlossary.Select(g => Texts["tree.term.line", g.Term, g.Meaning]));
        _buildTitle.Text = Texts.TreeTitle(Editor.TalentsTaken, Editor.Data);
        IReadOnlyList<string> build = [.. Texts.TreeBuildSummary(Editor.TalentsTaken, Editor.Data), Texts.TreeAdvice(Editor) ?? ""];
        for (int i = 0; i < _build.Count; i++)
            _build[i].Text = build[i];
        _keys.Text = Texts["tree.keys"];
        _save.Text = Texts["points.save"];
        _undo.Text = Texts["tree.undo"];
        _yes.Text = Texts["tree.confirm.yes"];
        _no.Text = Texts["tree.confirm.no"];
        _fold.Text = _side.Visible ? "›" : "‹";
        _fold.TooltipText = Texts[_side.Visible ? "tree.fold" : "tree.unfold"];
        // A plan is never saved (N2 a).
        _save.Disabled = Editor.ReadOnly || Planning || !Editor.TalentsChanged;
        // The tools: the class's suggested builds, planning at 60, the code copied or loaded.
        bool readOnly = (_real ?? Editor).ReadOnly;
        _tools.Text = Texts["tree.tools"];
        PopupMenu tools = _tools.GetPopup();
        tools.Clear();
        IReadOnlyList<string> suggested = TreePlan.Suggested(Editor.Hero.Class);
        for (int i = 0; i < suggested.Count; i++)
        {
            tools.AddItem(Texts["tree.tools.suggested", Texts.TreeTitle(Progression.Builds[suggested[i]], Editor.Data)], i);
            tools.SetItemDisabled(i, Editor.ReadOnly);
        }
        tools.AddSeparator();
        tools.AddCheckItem(Planning ? Texts["tree.plan.leave"] : Texts["tree.plan", TreePlan.PlanLevel], ToolPlan);
        tools.SetItemChecked(tools.GetItemIndex(ToolPlan), Planning);
        tools.SetItemDisabled(tools.GetItemIndex(ToolPlan), readOnly);
        tools.AddItem(Texts["tree.code.copy"], ToolCopy);
        tools.AddItem(Texts["tree.tools.code"], ToolCode);
        tools.SetItemDisabled(tools.GetItemIndex(ToolCode), Editor.ReadOnly);
        _load.Text = Texts["tree.code.load"];
        _load.Disabled = Editor.ReadOnly;
        _code.PlaceholderText = Texts["tree.code.placeholder"];
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
        if (Layout is not null && _canvas.Camera is not null)
        {
            Wood = Layout.Wood(Editor.TalentsTaken, StateOf, Path);
            _canvas.Refresh();
        }
    }

    private void ShowDetail()
    {
        TreeNode? node = Layout[_hovered ?? Focused];
        _detail.Text = node is null ? "" : Card(Texts.TreeCard(node, Editor))
            + (node is { IsTalent: true } && _hovered == node.Id && !Editor.TalentsTaken.Contains(node.Id) ? "\n" + Texts.TreePath(TreePlan.Path(Editor.TalentsTaken, node.Id, Editor.Data), Editor.Data) : "")
            + (Closing.Count > 0 ? "\n" + Texts["tree.closes", string.Join(", ", Closing.Select(id => Editor.Data.Talents[id].Name.In(Texts.Lang)))] : "");
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

/// <summary>
/// The tree's area (sprint 176, D87): the dark ground and, over it, the tree's world as the camera
/// (<see cref="TreeCamera"/>) shows it: the wood drawn under one <see cref="TreeNodeView"/> a node and its name, the
/// branches' names and the choices' frames; the gutter's names of the lines at the left edge, at the screen's size. The
/// wheel zooms around the mouse, a drag on the ground or a node moves the tree (a press that moved less than 6 px is a click).
/// </summary>
public partial class TreeCanvas : Control
{
    public TreePanel Panel { get; set; } = null!;

    /// <summary>The camera on the tree's world.</summary>
    public TreeCamera Camera { get; private set; } = null!;

    private readonly Dictionary<string, (TreeNodeView View, Label Name)> _views = [];
    private readonly List<(Branch Branch, Label Name)> _columns = [];
    private readonly List<Label> _choices = [];
    private readonly List<(TreeRow Row, Label Name)> _gutter = [];
    private Control _world = null!;
    private TreeWood _wood = null!;
    private Label _trunk = null!;
    private Vector2? _pressedAt;
    private Vector2 _last;
    private bool _dragging;
    private ((float Zoom, float X, float Y) From, (float Zoom, float X, float Y) To)? _move;
    private float _moved;

    /// <summary>How long a button's or a key's move of the camera takes (§7.4: the wheel and the drag are immediate).</summary>
    public const float MoveTime = 0.15f;

    /// <summary>The camera's buttons, bottom left: closer, further, the whole tree, back to the build (for the self-test).</summary>
    public Dictionary<string, Button> Buttons { get; } = [];

    private ColorRect _strip = null!;
    private HBoxContainer _buttons = null!;
    private Control? _legend;

    /// <summary>The legend, over the tree at its bottom right, at the screen's size.</summary>
    public Control? Legend
    {
        get => _legend;
        set
        {
            _legend = value;
            if (value is not null)
                AddChild(value);
            Place();
        }
    }

    /// <summary>The tree's choices, framed together: the Fusion's first floor (one of three), the four Hearts of Pure element.</summary>
    public static IEnumerable<(string Key, string[] Ids)> Choices(GameData data)
    {
        yield return ("tree.choice.transmute", ["transmute-air-lightning", "transmute-fire-lightning", "convert-lightning"]);
        yield return ("tree.choice.heart", [.. data.Talents.Values.Where(t => t.Branch == Branch.Pure).Select(t => t.Id)]);
    }

    /// <summary>A node's view (for the self-test: a click through it).</summary>
    public TreeNodeView? ViewOf(string id) => _views.TryGetValue(id, out (TreeNodeView View, Label Name) v) ? v.View : null;

    /// <summary>Whether the names under the nodes show (for the self-test).</summary>
    public int NamesShown => _views.Values.Count(v => v.Name.Visible);

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
        _gutter.Clear();
        TreeLayout layout = Panel.Layout;
        Camera = new TreeCamera(layout.Width, layout.Height, CustomMinimumSize.X, CustomMinimumSize.Y);
        _world = new Control { Size = new Vector2(layout.Width, layout.Height), MouseFilter = MouseFilterEnum.Ignore };
        AddChild(_world);
        _wood = new TreeWood { Panel = Panel, Size = _world.Size, MouseFilter = MouseFilterEnum.Ignore };
        _world.AddChild(_wood);
        // Each branch's name and count over its lane.
        foreach ((Branch branch, Box column) in layout.Columns)
        {
            Label name = Caption(new Vector2(column.X - 12, column.Y), 14, new Color(ElementStyle.StyleColour(branch)));
            name.Size = new Vector2(column.W + 24, 20);
            name.HorizontalAlignment = HorizontalAlignment.Center;
            _columns.Add((branch, name));
        }
        // The two choices' captions, under their names.
        foreach ((string key, string[] ids) in Choices(Panel.Editor.Data))
        {
            Box[] boxes = [.. ids.Select(id => layout[id]).OfType<TreeNode>().Select(n => n.Box)];
            if (boxes.Length == 0)
                continue;
            Label choice = Caption(new Vector2(boxes.Min(b => b.X) - 6, boxes.Max(b => b.Y + b.H) + TreeLayout.Label + 2), 11, new Color(1, 1, 1, 0.75f));
            choice.SetMeta("key", key);
            _choices.Add(choice);
        }
        TreeNode top = layout.Nodes.Where(n => n.IsTrunk).MinBy(n => n.Box.Y)!;
        _trunk = Caption(new Vector2(top.Box.X + top.Box.W + 160, top.Box.Y + 12), 12, new Color(1, 1, 1, 0.6f));
        foreach (TreeNode n in layout.Nodes)
        {
            var view = new TreeNodeView { Node = n, Panel = Panel, Canvas = this, Position = new Vector2(n.Box.X, n.Box.Y), Size = new Vector2(n.Box.W, n.Box.H) };
            _world.AddChild(view);
            // Two lines at most under a node, never an ellipsis; a ring's gain at its right.
            float labelWidth = n.IsTrunk ? 150 : n.Box.W + TreeLayout.NodeGap;
            var name = new Label
            {
                Position = n.IsTrunk ? new Vector2(n.Box.X + n.Box.W + 8, n.Box.CenterY - 14) : new Vector2(n.Box.CenterX - (labelWidth / 2), n.Box.Y + n.Box.H + 1),
                Size = new Vector2(labelWidth, TreeLayout.Label - 4),
                HorizontalAlignment = n.IsTrunk ? HorizontalAlignment.Left : HorizontalAlignment.Center,
                VerticalAlignment = n.IsTrunk ? VerticalAlignment.Center : VerticalAlignment.Top,
                AutowrapMode = TextServer.AutowrapMode.WordSmart,
                MaxLinesVisible = 2,
                MouseFilter = MouseFilterEnum.Ignore,
            };
            name.AddThemeFontSizeOverride("font_size", 11);
            name.AddThemeConstantOverride("line_spacing", -2);
            _world.AddChild(name);
            _views[n.Id] = (view, name);
        }
        // The camera's buttons, bottom left over the tree, at the screen's size.
        var buttons = new HBoxContainer();
        _buttons = buttons;
        buttons.AddThemeConstantOverride("separation", 4);
        foreach ((string key, string text, string tip, Action act) in new (string, string, string, Action)[]
        {
            ("in", "+", "tree.camera.in", () => Step(closer: true)),
            ("out", "−", "tree.camera.out", () => Step(closer: false)),
            ("all", Panel.Texts["tree.camera.all"], "tree.camera.all.tip", Overview),
            ("build", Panel.Texts["tree.camera.build"], "tree.camera.build.tip", Recentre),
        })
        {
            var button = new Button { Text = text, TooltipText = Panel.Texts[tip], CustomMinimumSize = new Vector2(44, 44), FocusMode = FocusModeEnum.None };
            button.Pressed += act;
            buttons.AddChild(button);
            Buttons[key] = button;
        }
        // The gutter: each line's name at the left edge, the screen's size whatever the zoom, on a strip of the ground.
        _strip = new ColorRect { Color = new Color(ElementStyle.Ground, 0.85f), MouseFilter = MouseFilterEnum.Ignore };
        AddChild(_strip);
        foreach (TreeRow row in Enum.GetValues<TreeRow>())
        {
            var name = new Label { Size = new Vector2(84, 32), AutowrapMode = TextServer.AutowrapMode.WordSmart, VerticalAlignment = VerticalAlignment.Center, MouseFilter = MouseFilterEnum.Ignore };
            name.AddThemeFontSizeOverride("font_size", 11);
            name.AddThemeColorOverride("font_color", new Color(UiStyle.TextDim));
            name.AddThemeColorOverride("font_outline_color", new Color(ElementStyle.Ground));
            name.AddThemeConstantOverride("outline_size", 4);
            AddChild(name);
            _gutter.Add((row, name));
        }
        AddChild(buttons);
        FrameBuild();
    }

    private Label Caption(Vector2 at, int size, Color colour)
    {
        var label = new Label { Position = at, MouseFilter = MouseFilterEnum.Ignore };
        label.AddThemeFontSizeOverride("font_size", size);
        label.AddThemeColorOverride("font_color", colour);
        _world.AddChild(label);
        return label;
    }

    /// <summary>Framed on the build (§7.4): its nodes taken and open, at 80 % at least; the doors, trunk and first floor without any.</summary>
    public void FrameBuild()
    {
        Camera.Frame(Panel.Layout.Build(Panel.Editor.TalentsTaken, Panel.StateOf));
        Place();
    }

    /// <summary>
    /// The world where the camera says; the gutter's names at their lines' heights; under 65 % the names under the nodes
    /// hidden but the one under the mouse (§7.4, the level of detail), the branches' names kept.
    /// </summary>
    public void Place()
    {
        _world.Position = new Vector2(Camera.X, Camera.Y);
        _world.Scale = new Vector2(Camera.Zoom, Camera.Zoom);
        foreach ((TreeRow row, Label name) in _gutter)
            name.Position = new Vector2(4, Camera.ToView(0, TreeLayout.RowY(row)).Y - 16);
        bool names = Camera.ShowsNames;
        foreach ((string id, (_, Label name)) in _views)
            name.Visible = names || Panel.Hovered == id;
        foreach (Label choice in _choices)
            choice.Visible = names;
        // The screen's pieces: the gutter's strip, the buttons bottom left, the legend bottom right.
        var view = new Vector2(Camera.ViewWidth, Camera.ViewHeight);
        _strip.Size = new Vector2(88, view.Y);
        _buttons.Position = new Vector2(96, view.Y - 52);
        if (_legend is not null)
        {
            _legend.Size = Vector2.Zero;
            _legend.Position = view - _legend.GetCombinedMinimumSize() - new Vector2(8, 8);
        }
    }

    /// <summary>
    /// The canvas at a new size (the right panel folded or not): the same point of the tree in the middle at the same zoom, or
    /// the overview again if it was on it.
    /// </summary>
    public void Resize(Vector2 size)
    {
        Settle();
        bool overview = Math.Abs(Camera.Zoom - Camera.MinZoom) < 0.001f;
        (float cx, float cy) = Camera.ToWorld(Camera.ViewWidth / 2, Camera.ViewHeight / 2);
        float zoom = Camera.Zoom;
        (CustomMinimumSize, Size) = (size, size);
        Camera = new TreeCamera(Camera.WorldWidth, Camera.WorldHeight, size.X, size.Y);
        if (!overview)
        {
            Camera.Set((zoom, 0, 0));
            Camera.Centre(cx, cy);
        }
        Place();
    }

    /// <summary>A point of the tree brought to the middle of the view at once (a bubble of the tutorial points at it), where it then stands.</summary>
    public Vector2 Show(float x, float y)
    {
        Settle();
        Camera.Centre(x, y);
        Place();
        return ToView(x, y);
    }

    /// <summary>The camera moved by <paramref name="change"/> in <see cref="MoveTime"/> (a button, a key, the keyboard followed).</summary>
    private void Animate(Action<TreeCamera> change)
    {
        Settle();
        (float, float, float) from = Camera.State;
        change(Camera);
        (float, float, float) to = Camera.State;
        Camera.Set(from);
        (_move, _moved) = ((from, to), 0);
    }

    /// <summary>The move under way finished at once (for the self-test).</summary>
    public void Settle()
    {
        if (_move is not { } move)
            return;
        Camera.Set(move.To);
        _move = null;
        Place();
    }

    public override void _Process(double delta)
    {
        if (_move is not { } move)
            return;
        _moved += (float)delta;
        Camera.Set(TreeCamera.Between(move.From, move.To, _moved / MoveTime));
        if (_moved >= MoveTime)
            _move = null;
        Place();
    }

    /// <summary>A step closer or further, around the view's middle (+, −).</summary>
    public void Step(bool closer) => Animate(c => c.ZoomAt(c.ViewWidth / 2, c.ViewHeight / 2, closer ? 1 : -1));

    /// <summary>The whole tree (0).</summary>
    public void Overview() => Animate(c => c.Fit());

    /// <summary>Back to the build (Home).</summary>
    public void Recentre() => Animate(c => c.Frame(Panel.Layout.Build(Panel.Editor.TalentsTaken, Panel.StateOf)));

    /// <summary>The node the keyboard went to, kept 48 px from the edges.</summary>
    public void Follow(Box box) => Animate(c => c.Follow(box));

    /// <summary>The wheel and the touchpad, on the ground or a node: whether the event was one of them.</summary>
    public bool Gesture(InputEvent @event)
    {
        switch (@event)
        {
            case InputEventMouseButton { Pressed: true, ButtonIndex: MouseButton.WheelUp or MouseButton.WheelDown } wheel:
                Wheel(wheel.ButtonIndex == MouseButton.WheelUp);
                return true;
            case InputEventMagnifyGesture pinch:
                Vector2 at = GetLocalMousePosition();
                Settle();
                Camera.ZoomAt(at.X, at.Y, MathF.Log(pinch.Factor) / MathF.Log(TreeCamera.Step));
                Place();
                return true;
            case InputEventPanGesture pan:
                Settle();
                Camera.PanBy(-pan.Delta.X * 16, -pan.Delta.Y * 16);
                Place();
                return true;
            default:
                return false;
        }
    }

    /// <summary>A point of the world, in the canvas.</summary>
    public Vector2 ToView(float x, float y) => Camera.ToView(x, y) is (float vx, float vy) ? new Vector2(vx, vy) : default;

    /// <summary>A notch of the wheel at the mouse: closer up, or further out.</summary>
    public void Wheel(bool closer)
    {
        Settle();
        Vector2 at = GetLocalMousePosition();
        Camera.ZoomAt(at.X, at.Y, closer ? 1 : -1);
        Place();
    }

    /// <summary>A press on the tree, on its ground or a node: a drag starts only once it has moved 6 px.</summary>
    public void PointerDown()
    {
        _pressedAt = _last = GetLocalMousePosition();
        _dragging = false;
    }

    public void PointerMove()
    {
        if (_pressedAt is not Vector2 at)
            return;
        Vector2 now = GetLocalMousePosition();
        _dragging |= TreeCamera.IsDrag(now.X - at.X, now.Y - at.Y);
        if (_dragging)
        {
            Settle();
            Camera.PanBy(now.X - _last.X, now.Y - _last.Y);
            Place();
        }
        _last = now;
    }

    /// <summary>The press let go: whether it was a drag (else a click).</summary>
    public bool PointerUp()
    {
        bool dragged = _dragging;
        (_pressedAt, _dragging) = (null, false);
        return dragged;
    }

    public override void _GuiInput(InputEvent @event)
    {
        if (Gesture(@event))
        {
            AcceptEvent();
            return;
        }
        switch (@event)
        {
            case InputEventMouseButton { ButtonIndex: MouseButton.Left or MouseButton.Middle or MouseButton.Right } button:
                if (button.Pressed)
                    PointerDown();
                else
                    PointerUp();
                break;
            case InputEventMouseMotion:
                PointerMove();
                break;
            default:
                return;
        }
        AcceptEvent();
    }

    public void Refresh()
    {
        TreeLayout layout = Panel.Layout;
        Texts texts = Panel.Texts;
        GameData data = Panel.Editor.Data;
        _trunk.Text = texts["tree.trunk"];
        foreach ((Branch branch, Label name) in _columns)
        {
            TreeNode[] lane = [.. layout.Nodes.Where(n => n.Branch == branch)];
            name.Text = $"{texts["talents.branch." + branch]} · {lane.Count(n => Panel.Editor.TalentsTaken.Contains(n.Id))}/{lane.Length}";
        }
        foreach ((TreeRow row, Label name) in _gutter)
            name.Text = texts["tree.row." + row];
        foreach (Label choice in _choices)
            choice.Text = texts[(string)choice.GetMeta("key")];
        foreach ((string id, (TreeNodeView view, Label name)) in _views)
        {
            TreeNode n = layout[id]!;
            name.Text = texts.TreeName(n, data);
            // A colour a look, no longer an opacity (s164-arbre-maquette.md §3.4).
            NodeLook look = Panel.StateOf(n) is NodeState s ? TreeLayout.Look(s) : NodeLook.NotYet;
            name.AddThemeColorOverride("font_color", new Color(look switch
            {
                NodeLook.Taken => UiStyle.TextStrong,
                NodeLook.Open => ElementStyle.Open,
                NodeLook.NotYet => UiStyle.TextDim,
                _ => ElementStyle.Locked,
            }));
            view.TooltipText = name.Text;
            view.QueueRedraw();
        }
        _wood.QueueRedraw();
        Place();
        QueueRedraw();
    }

    // The dark ground of the chart (K9 a), under the world.
    public override void _Draw() => DrawRect(new Rect2(Vector2.Zero, Size), new Color(ElementStyle.Ground));
}

/// <summary>
/// The tree's wood (sprint 176, D87), drawn under the nodes in the world; since sprint 177 (A5, s164-arbre-maquette.md §3.3)
/// in layers: alive, a shadow, a glow, the wood and its pistachio sap; reachable, the wood and its highlight; bare, a dotted
/// chocolate line. The path to the node under the mouse carries sap at 60 %; what a giving back would lose, red sap. The
/// crown's collar a disc of wood, its heart lit once a door is taken; the choices' frames over it.
/// </summary>
public partial class TreeWood : Control
{
    public TreePanel Panel { get; set; } = null!;

    /// <summary>A piece's width by its kind, from the trunk, the thickest, to the twigs (§3.3).</summary>
    public static float Width(WoodKind kind) => kind switch
    {
        WoodKind.Trunk => 18,
        WoodKind.Root => 6,
        WoodKind.Axis => 8,
        WoodKind.Twig => 4,
        _ => 10,
    };

    public override void _Draw()
    {
        TreeLayout layout = Panel.Layout;
        GameData data = Panel.Editor.Data;
        IReadOnlyList<string> taken = Panel.Editor.TalentsTaken, losing = Panel.Losing();
        Color sap = new(ElementStyle.Taken), wood = new(ElementStyle.Wood), light = new(ElementStyle.WoodLight), shadow = new(Colors.Black, 0.35f);
        foreach (WoodPiece piece in Panel.Wood)
        {
            Vector2[] points = [.. piece.Points.Select(p => new Vector2(p.X, p.Y))];
            float w = Width(piece.Kind);
            bool thick = w >= 8, lost = TreeLayout.Lost(piece, taken, losing);
            Vector2[] Moved(Vector2 by) => [.. points.Select(p => p + by)];
            if (piece.Life == WoodLife.Bare && !piece.Sap)
            {
                for (int i = 0; i + 1 < points.Length; i++)
                    DrawDashedLine(points[i], points[i + 1], new Color(UiStyle.Chocolate), 2, 6);
                continue;
            }
            if (piece.Kind is WoodKind.Twig or WoodKind.Root && piece.Life != WoodLife.Alive)
                DrawPolyline(points, light, w, true);
            else if (piece.Life == WoodLife.Alive && piece.Kind == WoodKind.Twig)
                DrawPolyline(points, lost ? new Color(ElementStyle.Losing) : sap, w, true);
            else
            {
                if (thick)
                    DrawPolyline(Moved(new Vector2(0, 2)), shadow, w + 4, true);
                if (thick && piece.Life == WoodLife.Alive)
                    DrawPolyline(points, new Color(sap, 0.12f), w + 6, true);
                DrawPolyline(points, wood, w, true);
                if (piece.Life == WoodLife.Alive)
                    DrawPolyline(points, lost ? new Color(ElementStyle.Losing) : sap, Math.Max(3, w / 2), true);
                else
                    DrawPolyline(Moved(new Vector2(-0.2f * w, 0)), light, 0.35f * w, true);
            }
            // The path under the mouse: sap at 60 % up to the node.
            if (piece.Sap && piece.Life != WoodLife.Alive)
                DrawPolyline(points, new Color(sap, 0.6f), Math.Max(3, w / 2), true);
        }
        // The crown's collar: a disc of wood, its heart lit once a door is taken.
        var crown = new Vector2(layout.Crown.X, layout.Crown.Y);
        DrawCircle(crown, 10, light);
        DrawArc(crown, 10, 0, Mathf.Tau, 32, new Color(ElementStyle.WoodDark), 2, true);
        if (taken.Any(id => data.Talents.TryGetValue(id, out TalentSpec? t) && t.IsFork))
            DrawCircle(crown, 4, sap);
        // The choices (the Fusion's first floor, the Hearts) framed together.
        foreach ((_, string[] choice) in TreeCanvas.Choices(data))
        {
            Box[] boxes = [.. choice.Select(id => layout[id]).OfType<TreeNode>().Select(n => n.Box)];
            if (boxes.Length < 2)
                continue;
            float x0 = boxes.Min(b => b.X) - 5, y0 = boxes.Min(b => b.Y) - 5, x1 = boxes.Max(b => b.X + b.W) + 5, y1 = boxes.Max(b => b.Y + b.H) + 5;
            DrawStyleBox(new StyleBoxFlat { DrawCenter = false, BorderColor = wood, BorderWidthLeft = 1, BorderWidthRight = 1, BorderWidthTop = 1, BorderWidthBottom = 1, CornerRadiusTopLeft = 10, CornerRadiusTopRight = 10, CornerRadiusBottomLeft = 10, CornerRadiusBottomRight = 10 }, new Rect2(x0, y0, x1 - x0, y1 - y0));
        }
    }
}

/// <summary>One node of the tree, drawn by its shape and its state; a click presses it, the mouse over it shows it in the panel.</summary>
public partial class TreeNodeView : Control
{
    public TreeNode Node { get; set; } = null!;

    public TreePanel Panel { get; set; } = null!;

    public TreeCanvas Canvas { get; set; } = null!;

    public override void _Ready()
    {
        MouseFilter = MouseFilterEnum.Stop;
        MouseDefaultCursorShape = CursorShape.PointingHand;
        MouseEntered += () => Panel.Hover(Node.Id);
        MouseExited += () => Panel.Hover(null);
    }

    /// <summary>A click takes or gives back the node when let go without a drag (sprint 176, R13); a drag from it moves the tree; the wheel zooms.</summary>
    public override void _GuiInput(InputEvent @event)
    {
        if (Canvas.Gesture(@event))
        {
            AcceptEvent();
            return;
        }
        switch (@event)
        {
            case InputEventMouseButton { ButtonIndex: MouseButton.Left } button:
                if (button.Pressed)
                    Canvas.PointerDown();
                else if (!Canvas.PointerUp())
                    Panel.Press(Node.Id);
                break;
            case InputEventMouseMotion:
                Canvas.PointerMove();
                break;
            default:
                return;
        }
        AcceptEvent();
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

    /// <summary>
    /// A node's drawing in relief (sprint 164, s164-arbre-maquette.md §3.4) by its look: in colour when taken (or about to be
    /// given back), in light greys when open, flat grey when not yet or closed; null without one.
    /// </summary>
    public static Texture2D? ReliefOf(TreeNode node, string variant)
    {
        ArgumentNullException.ThrowIfNull(node);
        string? name = node.IsTalent ? node.Id : node.IsTrunk ? node.Id.Replace(':', '-') : null;
        if (name is null)
            return null;
        string key = "relief/" + name + variant;
        if (Icons.TryGetValue(key, out Texture2D? known))
            return known;
        string path = $"res://assets/icons/talents/{key}.svg";
        return Icons[key] = ResourceLoader.Exists(path) ? GD.Load<Texture2D>(path) : null;
    }

    public override void _Draw()
    {
        NodeState? state = Panel.StateOf(Node);
        NodeLook look = state is NodeState s ? TreeLayout.Look(s) : NodeLook.NotYet;
        bool focused = Panel.Focused == Node.Id;
        bool red = Panel.Losing(Node.Id);
        TreeShapes.Draw(this, Node.Shape, look, Size, red);
        // A plan's ghost (N2 a): a dotted pistachio ring (the path under the mouse runs in the wood's sap since sprint 177).
        if (Panel.Ghosts.Contains(Node.Id))
        {
            for (int i = 0; i < 16; i++)
                DrawArc(Size / 2, (Size.X / 2) + 3, i * Mathf.Tau / 16, (i + 0.5f) * Mathf.Tau / 16, 4, new Color(ElementStyle.Taken), 2, true);
        }
        if (focused)
            DrawRect(new Rect2(-4, -4, Size.X + 8, Size.Y + 8), new Color(ElementStyle.Taken), false, 2);
        // What taking the node under the mouse would close: a light chocolate slash (sprint 177, A6).
        if (Panel.Closing.Contains(Node.Id))
            DrawLine(new Vector2(Size.X * 0.25f, Size.Y * 0.75f), new Vector2(Size.X * 0.75f, Size.Y * 0.25f), new Color(UiStyle.ChocolateLight), 3, true);
        GameData data = Panel.Editor.Data;
        // The icon, 57 % of the node: closed at 25 %, never crossed nor red (docs/arbre-lisible.md §2.6).
        string variant = red || look == NodeLook.Taken ? "" : look == NodeLook.Open ? "-open" : "-locked";
        Color through = look == NodeLook.Closed && !red ? new Color(1, 1, 1, 0.25f) : Colors.White;
        if (ReliefOf(Node, variant) is Texture2D icon)
        {
            float side = Size.X * 0.57f;
            DrawTextureRect(icon, new Rect2((Size - new Vector2(side, side)) / 2, new Vector2(side, side)), false, through);
        }
        else
        {
            Font font = ThemeDB.FallbackFont;
            Vector2 measured = font.GetStringSize("···", HorizontalAlignment.Left, -1, 17);
            Color ink = look == NodeLook.Taken ? new Color(ElementStyle.Taken) : look == NodeLook.Open ? new Color(ElementStyle.Open) : new Color(UiStyle.TextDim) * through;
            DrawString(font, new Vector2((Size.X - measured.X) / 2, (Size.Y / 2) + 6), "···", HorizontalAlignment.Left, -1, 17, ink);
        }
        if (!Node.IsTalent)
            return;
        TalentSpec t = data.Talents[Node.Id];
        Font bold = OsmoseTheme.Bold();
        if (look == NodeLook.Closed && !red)
            return;
        // The cost only when it is 2 (L7 a): a gold disc over the top right corner.
        if (t.Cost == 2)
        {
            var at = new Vector2(Size.X - 6, 6);
            DrawCircle(at, 10, new Color(UiStyle.Gold));
            DrawArc(at, 10, 0, Mathf.Tau, 24, new Color(UiStyle.Chocolate), 2, true);
            DrawString(bold, at + new Vector2(-4, 5), "2", HorizontalAlignment.Left, -1, 14, new Color(UiStyle.GoldText));
        }
        // The level a node waits for: a badge astride its foot (§3.4).
        if (state == NodeState.Level && TalentReasons.WhyNot(Panel.Editor.TalentsTaken, t.Id, Panel.Editor.Hero.Level, data) is { Number: int level and > 0 })
        {
            var badge = new Rect2((Size.X / 2) - 16, Size.Y - 10, 32, 20);
            DrawStyleBox(new StyleBoxFlat
            {
                BgColor = new Color(UiStyle.Well),
                BorderColor = new Color(ElementStyle.OutOfLevel),
                BorderWidthLeft = 1,
                BorderWidthRight = 1,
                BorderWidthTop = 1,
                BorderWidthBottom = 1,
                CornerRadiusTopLeft = 10,
                CornerRadiusTopRight = 10,
                CornerRadiusBottomLeft = 10,
                CornerRadiusBottomRight = 10,
            }, badge);
            string text = level.ToString(System.Globalization.CultureInfo.InvariantCulture);
            float w = bold.GetStringSize(text, HorizontalAlignment.Left, -1, 14).X;
            DrawString(bold, new Vector2(badge.Position.X + ((badge.Size.X - w) / 2), badge.Position.Y + 15), text, HorizontalAlignment.Left, -1, 14, new Color(UiStyle.TextDim));
            return;
        }
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

/// <summary>A swatch of the legend: a small node in one of the four looks.</summary>
public partial class LegendDot : Control
{
    public NodeLook Look { get; set; }

    public override void _Draw() => TreeShapes.Draw(this, NodeShape.Node, Look, Size, false);
}

/// <summary>
/// How a node is drawn (sprint 164, É1, s164-arbre-maquette.md §3.4): a disc (a trunk's node, a door, a node) or a rounded
/// square (a key), in one of four looks, from the most to the least salient: taken (lit, a thick pistachio ring and its halo),
/// open (raised, a plain light ring), not yet (a hollow, a dotted ring), closed (a faint hollow, no ring, a thin slash: never
/// red, never a cross). Red only for what a giving back would lose.
/// </summary>
public static class TreeShapes
{
    public static void Draw(Control on, NodeShape shape, NodeLook look, Vector2 size, bool red)
    {
        (Color fill, Color line, float width, bool dotted, bool raised) = red ? (new Color(ElementStyle.LosingFill), new Color(ElementStyle.Losing), 3f, false, true) : look switch
        {
            NodeLook.Taken => (new Color(UiStyle.LitTop), new Color(ElementStyle.Taken), 3f, false, true),
            NodeLook.Open => (new Color(UiStyle.RaisedTop), new Color(ElementStyle.Open), 2f, false, true),
            NodeLook.NotYet => (new Color(UiStyle.Well), new Color(ElementStyle.Locked), 2f, true, false),
            _ => (new Color(UiStyle.Well, 0.25f), Colors.Transparent, 0f, false, false),
        };
        Vector2 c = size / 2;
        float r = (Mathf.Min(size.X, size.Y) / 2) - Mathf.Max(width, 2);
        bool key = shape == NodeShape.Key;
        var box = new Rect2(c - new Vector2(r, r), new Vector2(2 * r, 2 * r));
        StyleBoxFlat Round(Color ground, Color edge, float w, float grow) => new()
        {
            BgColor = ground,
            BorderColor = edge,
            DrawCenter = ground.A > 0,
            BorderWidthLeft = (int)w,
            BorderWidthRight = (int)w,
            BorderWidthTop = (int)w,
            BorderWidthBottom = (int)w,
            CornerRadiusTopLeft = 14,
            CornerRadiusTopRight = 14,
            CornerRadiusBottomLeft = 14,
            CornerRadiusBottomRight = 14,
            ExpandMarginLeft = grow,
            ExpandMarginRight = grow,
            ExpandMarginTop = grow,
            ExpandMarginBottom = grow,
            AntiAliasing = true,
        };
        // Raised: a drop shadow; taken: a pistachio halo around it.
        if (raised)
        {
            if (key)
                on.DrawStyleBox(Round(new Color(0, 0, 0, 0.45f), Colors.Transparent, 0, 0), new Rect2(box.Position + new Vector2(0, 2), box.Size));
            else
                on.DrawCircle(c + new Vector2(0, 2), r, new Color(0, 0, 0, 0.45f));
        }
        if (look == NodeLook.Taken && !red)
        {
            if (key)
                on.DrawStyleBox(Round(Colors.Transparent, new Color(ElementStyle.Taken, 0.35f), 3, 4), box);
            else
                on.DrawArc(c, r + 4, 0, Mathf.Tau, 48, new Color(ElementStyle.Taken, 0.35f), 3, true);
        }
        if (key)
        {
            on.DrawStyleBox(Round(fill, dotted ? Colors.Transparent : line, dotted ? 0 : width, 0), box);
            if (dotted)
            {
                Vector2[] corners = [box.Position, box.Position + new Vector2(box.Size.X, 0), box.End, box.Position + new Vector2(0, box.Size.Y)];
                for (int i = 0; i < 4; i++)
                    on.DrawDashedLine(corners[i], corners[(i + 1) % 4], line, width, 5);
            }
        }
        else
        {
            on.DrawCircle(c, r, fill);
            if (raised)
            {
                // The relief's highlight on the top half.
                on.DrawArc(c, r - 3, Mathf.Pi * 1.15f, Mathf.Pi * 1.85f, 24, new Color(1, 1, 1, 0.18f), 2, true);
            }
            if (width > 0 && !dotted)
                on.DrawArc(c, r, 0, Mathf.Tau, 48, line, width, true);
            else if (dotted)
            {
                // A dotted ring: short arcs with gaps.
                int dashes = Math.Max(8, (int)(r / 2.5f));
                for (int i = 0; i < dashes; i++)
                {
                    float a = i * Mathf.Tau / dashes;
                    on.DrawArc(c, r, a, a + (Mathf.Tau / dashes * 0.55f), 6, line, width, true);
                }
            }
        }
        // Closed: a thin slash from 30 % to 70 % of the diagonal, bottom left to top right.
        if (look == NodeLook.Closed && !red)
            on.DrawLine(new Vector2(size.X * 0.3f, size.Y * 0.7f), new Vector2(size.X * 0.7f, size.Y * 0.3f), new Color(UiStyle.TextOff), 2, true);
    }
}
