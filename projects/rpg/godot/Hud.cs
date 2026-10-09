using Godot;
using Rpg.Client;
using Rpg.Core;

namespace Rpg.Desktop;

/// <summary>
/// The screen around the board: round and whose turn it is, the timeline of the next turns in
/// portraits, the hover panel (a fighter's card, a spell's forecast), the player's points and
/// spells, the end-turn button, a message line, the fight log, the end screen.
/// </summary>
public partial class Hud : CanvasLayer
{
    /// <summary>Turns shown on the timeline.</summary>
    public const int TimelineLength = 8;

    private readonly List<Button> _spells = [];
    private readonly List<string> _spellNames = [];
    private readonly List<(PanelContainer Card, Portrait Face, Label Name, ProgressBar Hp)> _timeline = [];
    private PanelContainer _info = null!;
    private Label _infoText = null!;
    private readonly Queue<string> _log = new();
    private Label _turn = null!, _order = null!, _stats = null!, _message = null!, _help = null!, _logLabel = null!, _endTitle = null!;
    private Button _endTurn = null!, _again = null!, _back = null!, _lang = null!;
    private HBoxContainer _spellBar = null!;
    private PanelContainer _end = null!;
    private FightController _controller = null!;

    public Texts Texts { get; private set; } = new("en");

    public event Action<int>? SpellChosen;
    public event Action? EndTurnPressed;
    public event Action? AgainPressed;
    public event Action? BackPressed;

    public void Build(FightController controller, Texts texts)
    {
        _controller = controller;
        Texts = texts;
        var root = new Control();
        root.SetAnchorsPreset(Control.LayoutPreset.FullRect);
        root.MouseFilter = Control.MouseFilterEnum.Ignore;
        AddChild(root);

        _turn = Text(root, 22, 0, 0, 20, 14, 600, 32);
        var timeline = new HBoxContainer { MouseFilter = Control.MouseFilterEnum.Ignore };
        timeline.AddThemeConstantOverride("separation", 6);
        Anchor(timeline, 1, 0, -8 - TimelineLength * 78, 10, TimelineLength * 78, 100);
        timeline.Alignment = BoxContainer.AlignmentMode.End;
        root.AddChild(timeline);
        for (int i = 0; i < TimelineLength; i++)
        {
            var card = new PanelContainer { CustomMinimumSize = new Vector2(72, 0), MouseFilter = Control.MouseFilterEnum.Ignore };
            var box = new VBoxContainer { MouseFilter = Control.MouseFilterEnum.Ignore };
            box.AddThemeConstantOverride("separation", 2);
            card.AddChild(box);
            Portrait face = Portrait.Make(new Vector2(68, 56), turning: false, distance: 1.7f);
            box.AddChild(face);
            var name = new Label { HorizontalAlignment = HorizontalAlignment.Center, ClipText = true, CustomMinimumSize = new Vector2(68, 0), MouseFilter = Control.MouseFilterEnum.Ignore };
            name.AddThemeFontSizeOverride("font_size", 12);
            box.AddChild(name);
            var hp = new ProgressBar { ShowPercentage = false, CustomMinimumSize = new Vector2(68, 6), MouseFilter = Control.MouseFilterEnum.Ignore };
            box.AddChild(hp);
            timeline.AddChild(card);
            _timeline.Add((card, face, name, hp));
        }
        _order = Text(root, 1, 1, 0, 0, 0, 1, 1);
        _order.Visible = false;
        _info = new PanelContainer { Visible = false, MouseFilter = Control.MouseFilterEnum.Ignore };
        Anchor(_info, 1, 0, -340, 124, 330, 0);
        root.AddChild(_info);
        _infoText = new Label { AutowrapMode = TextServer.AutowrapMode.WordSmart, CustomMinimumSize = new Vector2(310, 0), MouseFilter = Control.MouseFilterEnum.Ignore };
        _infoText.AddThemeFontSizeOverride("font_size", 15);
        _info.AddChild(_infoText);
        _logLabel = Text(root, 15, 0, 1, 20, -270, 520, 170);
        _logLabel.VerticalAlignment = VerticalAlignment.Bottom;
        _message = Text(root, 18, 0.5f, 1, -300, -136, 600, 26);
        _message.HorizontalAlignment = HorizontalAlignment.Center;
        _message.AddThemeColorOverride("font_color", new Color("ffb4a2"));
        _help = Text(root, 14, 0.5f, 1, -400, -106, 800, 22);
        _help.HorizontalAlignment = HorizontalAlignment.Center;
        _help.Modulate = new Color(1, 1, 1, 0.7f);

        var bar = new PanelContainer();
        Anchor(bar, 0.5f, 1, -480, -78, 960, 64);
        root.AddChild(bar);
        var row = new HBoxContainer();
        row.AddThemeConstantOverride("separation", 12);
        bar.AddChild(row);
        _stats = new Label { CustomMinimumSize = new Vector2(250, 0), VerticalAlignment = VerticalAlignment.Center };
        _stats.AddThemeFontSizeOverride("font_size", 20);
        row.AddChild(_stats);
        _spellBar = new HBoxContainer();
        _spellBar.AddThemeConstantOverride("separation", 8);
        row.AddChild(_spellBar);
        _endTurn = new Button { CustomMinimumSize = new Vector2(140, 48) };
        _endTurn.Pressed += () => EndTurnPressed?.Invoke();
        row.AddChild(_endTurn);
        _lang = new Button { CustomMinimumSize = new Vector2(56, 48), TooltipText = "Français / English" };
        _lang.Pressed += () =>
        {
            Texts = new Texts(Texts.Lang == "fr" ? "en" : "fr");
            Refresh();
        };
        row.AddChild(_lang);

        _end = new PanelContainer { Visible = false };
        Anchor(_end, 0.5f, 0.5f, -180, -120, 360, 240);
        root.AddChild(_end);
        var endBox = new VBoxContainer { Alignment = BoxContainer.AlignmentMode.Center };
        endBox.AddThemeConstantOverride("separation", 20);
        _end.AddChild(endBox);
        _endTitle = new Label { HorizontalAlignment = HorizontalAlignment.Center };
        _endTitle.AddThemeFontSizeOverride("font_size", 36);
        endBox.AddChild(_endTitle);
        _again = new Button { CustomMinimumSize = new Vector2(200, 48), SizeFlagsHorizontal = Control.SizeFlags.ShrinkCenter };
        _again.Pressed += () => AgainPressed?.Invoke();
        endBox.AddChild(_again);
        _back = new Button { CustomMinimumSize = new Vector2(200, 48), SizeFlagsHorizontal = Control.SizeFlags.ShrinkCenter, Visible = false };
        _back.Pressed += () => BackPressed?.Invoke();
        endBox.AddChild(_back);
        Refresh();
    }

    public void Log(FightEvent e)
    {
        if (Texts.Describe(e, _controller.Fight) is not string line)
            return;
        _log.Enqueue(line);
        while (_log.Count > 7)
            _log.Dequeue();
    }

    /// <summary>Redraws every text from the fight's state (and the language chosen).</summary>
    public void Refresh(bool busy = false)
    {
        Fight fight = _controller.Fight;
        Fighter current = fight.Current;
        Fighter me = _controller.IsPlayerTurn ? current : fight.Fighters.First(f => f.Team == _controller.PlayerTeam);
        _turn.Text = $"{Texts["round", fight.Round]} · {(_controller.IsPlayerTurn ? Texts["your-turn"] : Texts["their-turn", Texts.Name(current)])}";
        IReadOnlyList<Fighter> next = fight.NextTurns(TimelineLength);
        for (int i = 0; i < _timeline.Count; i++)
        {
            (PanelContainer card, Portrait face, Label name, ProgressBar hp) = _timeline[i];
            card.Visible = i < next.Count;
            if (i >= next.Count)
                continue;
            Fighter f = next[i];
            // The player's hero in its own colours, the others as they come.
            bool hero = fight.Hero is Hero h && f.Team == _controller.PlayerTeam && !f.IsSummon && f.Name.Fr == h.Name;
            face.Show(hero ? fight.Hero! : new Hero(Texts.Name(f), f.Spec.Look), paint: hero);
            name.Text = Texts.Name(f);
            hp.MaxValue = Math.Max(1, f.MaxHp);
            hp.Value = f.Hp;
            Color team = f.Team == _controller.PlayerTeam ? FighterView.PlayerColour : FighterView.EnemyColour;
            hp.AddThemeStyleboxOverride("fill", new StyleBoxFlat { BgColor = team });
            // The one playing now is outlined in its team's colour.
            card.AddThemeStyleboxOverride("panel", new StyleBoxFlat
            {
                BgColor = new Color(0.12f, 0.12f, 0.12f, 0.85f),
                BorderColor = team,
                BorderWidthBottom = i == 0 ? 3 : 1,
                BorderWidthTop = i == 0 ? 3 : 1,
                BorderWidthLeft = i == 0 ? 3 : 1,
                BorderWidthRight = i == 0 ? 3 : 1,
                ContentMarginLeft = 2,
                ContentMarginRight = 2,
                ContentMarginTop = 2,
                ContentMarginBottom = 2,
            });
        }
        _order.Text = string.Join('\n', next.Select(f => Texts.Name(f)));
        _stats.Text = $"{Texts["hp"]} {me.Hp}/{me.MaxHp}   {Texts["ap"]} {me.Ap}   {Texts["mp"]} {me.Mp}";
        _message.Text = _controller.LastError is ActionError e ? Texts.Error(e) : "";
        _help.Text = Texts["help"];
        _logLabel.Text = string.Join('\n', _log);
        bool canAct = _controller.IsPlayerTurn && !busy;
        // Up to five spells show their names; more (a class has twenty) are narrower, the card on hover.
        bool narrow = me.Spells.Count > 5;
        while (_spells.Count < me.Spells.Count)
        {
            int index = _spells.Count;
            var b = new Button { CustomMinimumSize = new Vector2(44, 48), ToggleMode = true, ClipText = true };
            b.Pressed += () => SpellChosen?.Invoke(index);
            _spellBar.AddChild(b);
            _spells.Add(b);
        }
        _spellNames.Clear();
        float width = narrow ? Math.Max(44, 560f / me.Spells.Count) : 150;
        for (int i = 0; i < me.Spells.Count; i++)
        {
            Spell s = me.Spells[i];
            _spellNames.Add(s.Name.In(Texts.Lang));
            _spells[i].CustomMinimumSize = new Vector2(width, 48);
            _spells[i].Text = narrow ? $"{(i < 9 ? $"{i + 1}. " : "")}{s.Name.In(Texts.Lang)}" : $"{i + 1}. {s.Name.In(Texts.Lang)} · {s.ApCost} {Texts["ap"]}";
            _spells[i].TooltipText = Texts.SpellCard(s);
            _spells[i].Disabled = !canAct || !_controller.CanUse(s);
            _spells[i].SetPressedNoSignal(_controller.SelectedSpell == s);
        }
        _endTurn.Text = Texts["end-turn"];
        _endTurn.Disabled = !canAct;
        _lang.Text = Texts.Lang == "fr" ? "EN" : "FR";
        _end.Visible = fight.IsOver;
        _endTitle.Text = fight.WinningTeam is null ? Texts["draw"] : fight.WinningTeam == _controller.PlayerTeam ? Texts["victory"] : Texts["defeat"];
        _again.Text = Texts["again"];
        _back.Text = Texts["town.back"];
    }

    /// <summary>The end screen's way back to town.</summary>
    public void SetBackShown(bool shown) => _back.Visible = shown;

    public string TurnText => _turn.Text;
    public string StatsText => _stats.Text;
    public string OrderText => _order.Text;

    /// <summary>The names on the timeline, the one playing first.</summary>
    public IEnumerable<string> TimelineText => _timeline.Where(t => t.Card.Visible).Select(t => t.Name.Text);

    /// <summary>The looks drawn on the timeline's portraits.</summary>
    public IEnumerable<string> TimelineLooks => _timeline.Where(t => t.Card.Visible).Select(t => t.Face.Look);

    /// <summary>The hover panel's text, or null when it is hidden.</summary>
    public string? InfoText => _info.Visible ? _infoText.Text : null;

    /// <summary>Shows the hover panel with this text, or hides it (null).</summary>
    public void ShowInfo(string? text)
    {
        _info.Visible = text is not null;
        _infoText.Text = text ?? "";
        // The panel grows with its text from the top.
        _info.ResetSize();
    }

    /// <summary>The spells' names on the bar, in order.</summary>
    public IEnumerable<string> SpellsText => _spellNames;
    public bool EndShown => _end.Visible;

    /// <summary>A label whose box is offset from an anchor point of the screen (0 to 1 on each axis).</summary>
    private static Label Text(Control parent, int size, float ax, float ay, float x, float y, float w, float h)
    {
        var label = new Label { MouseFilter = Control.MouseFilterEnum.Ignore };
        label.AddThemeFontSizeOverride("font_size", size);
        label.AddThemeColorOverride("font_outline_color", Colors.Black);
        label.AddThemeConstantOverride("outline_size", 6);
        Anchor(label, ax, ay, x, y, w, h);
        parent.AddChild(label);
        return label;
    }

    private static void Anchor(Control c, float ax, float ay, float x, float y, float w, float h)
    {
        c.AnchorLeft = c.AnchorRight = ax;
        c.AnchorTop = c.AnchorBottom = ay;
        c.OffsetLeft = x;
        c.OffsetTop = y;
        c.OffsetRight = x + w;
        c.OffsetBottom = y + h;
    }
}
