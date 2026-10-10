using Godot;
using Rpg.Client;
using Rpg.Core;

namespace Rpg.Desktop;

/// <summary>
/// The screen over the town: its name and the help line, the language button, the way back to the
/// characters (signed in only), the character's level and experience and its characteristics and
/// spells screen, a message line, and the talk: who speaks, what they say, the answers.
/// </summary>
public partial class TalkPanel : CanvasLayer
{
    private readonly List<Button> _answers = [];
    private Label _title = null!, _help = null!, _message = null!, _speaker = null!, _line = null!, _xp = null!;
    private Button _lang = null!, _characters = null!, _points = null!, _inventory = null!, _map = null!;
    private (int Level, long Xp)? _progress;
    private PanelContainer _talk = null!;
    private VBoxContainer _answerBox = null!;
    private TownController _town = null!;

    public Texts Texts { get; private set; } = new("en");

    public event Action<int>? Answered;
    public event Action? CharactersPressed;
    public event Action? PointsPressed;
    public event Action? InventoryPressed;
    public event Action? MapPressed;
    public event Action? LanguageChanged;

    public void Build(TownController town, Texts texts, bool signedIn)
    {
        _town = town;
        Texts = texts;
        var root = new Control { MouseFilter = Control.MouseFilterEnum.Ignore };
        root.SetAnchorsPreset(Control.LayoutPreset.FullRect);
        AddChild(root);
        _title = Text(root, 26, 20, 14, 600, 36);
        _help = Text(root, 15, 20, 52, 900, 24);
        _help.AddThemeColorOverride("font_color", new Color(1, 1, 1, 0.85f));
        _message = Text(root, 16, 20, 78, 900, 24);

        var buttons = new HBoxContainer { AnchorLeft = 1, AnchorRight = 1, OffsetLeft = -800, OffsetRight = -20, OffsetTop = 14, OffsetBottom = 58, Alignment = BoxContainer.AlignmentMode.End };
        buttons.AddThemeConstantOverride("separation", 10);
        root.AddChild(buttons);
        _map = new Button { CustomMinimumSize = new Vector2(100, 44) };
        _map.Pressed += () => MapPressed?.Invoke();
        buttons.AddChild(_map);
        _inventory = new Button { CustomMinimumSize = new Vector2(150, 44), Visible = false };
        _inventory.Pressed += () => InventoryPressed?.Invoke();
        buttons.AddChild(_inventory);
        _points = new Button { CustomMinimumSize = new Vector2(150, 44), Visible = false };
        _points.Pressed += () => PointsPressed?.Invoke();
        buttons.AddChild(_points);
        _characters = new Button { CustomMinimumSize = new Vector2(150, 44), Visible = signedIn };
        _characters.Pressed += () => CharactersPressed?.Invoke();
        buttons.AddChild(_characters);
        _lang = new Button { CustomMinimumSize = new Vector2(56, 44), TooltipText = "Français / English" };
        _lang.Pressed += () =>
        {
            Texts = new Texts(Texts.Lang == "fr" ? "en" : "fr");
            Refresh();
            LanguageChanged?.Invoke();
        };
        buttons.AddChild(_lang);

        _xp = new Label { AnchorLeft = 1, AnchorRight = 1, OffsetLeft = -520, OffsetRight = -20, OffsetTop = 62, OffsetBottom = 86, HorizontalAlignment = HorizontalAlignment.Right, MouseFilter = Control.MouseFilterEnum.Ignore };
        _xp.AddThemeFontSizeOverride("font_size", 16);
        _xp.AddThemeColorOverride("font_outline_color", Colors.Black);
        _xp.AddThemeConstantOverride("outline_size", 6);
        root.AddChild(_xp);

        // Anchored at the bottom, it grows upwards as high as its line and answers.
        _talk = new PanelContainer
        {
            Visible = false,
            AnchorLeft = 0.5f,
            AnchorRight = 0.5f,
            AnchorTop = 1,
            AnchorBottom = 1,
            OffsetLeft = -380,
            OffsetRight = 380,
            OffsetTop = -20,
            OffsetBottom = -20,
            GrowVertical = Control.GrowDirection.Begin,
        };
        root.AddChild(_talk);
        var margin = new MarginContainer();
        foreach (string side in new[] { "left", "right", "top", "bottom" })
            margin.AddThemeConstantOverride("margin_" + side, 18);
        _talk.AddChild(margin);
        var column = new VBoxContainer();
        column.AddThemeConstantOverride("separation", 10);
        margin.AddChild(column);
        _speaker = new Label();
        _speaker.AddThemeFontSizeOverride("font_size", 22);
        _speaker.AddThemeColorOverride("font_color", FighterView.PlayerColour);
        column.AddChild(_speaker);
        _line = new Label { AutowrapMode = TextServer.AutowrapMode.WordSmart };
        _line.AddThemeFontSizeOverride("font_size", 18);
        column.AddChild(_line);
        _answerBox = new VBoxContainer();
        _answerBox.AddThemeConstantOverride("separation", 6);
        column.AddChild(_answerBox);
        Refresh();
    }

    public void ShowMessage(string text) => _message.Text = text;

    /// <summary>The character's level and experience, and its points screen; null: no character (a classless hero).</summary>
    public void SetProgress(int? level, long xp)
    {
        _progress = level is int l ? (l, xp) : null;
        Refresh();
    }

    public string XpText => _xp.Text;

    /// <summary>Redraws the texts and the talk from the town's state and the language.</summary>
    public void Refresh()
    {
        _title.Text = _town.Town.Name.In(Texts.Lang);
        _help.Text = Texts["town.help"];
        _lang.Text = Texts.Lang == "fr" ? "EN" : "FR";
        _characters.Text = Texts["lobby.back"];
        _points.Text = Texts["points.button"];
        _points.Visible = _progress is not null;
        _inventory.Text = Texts["inventory.button"];
        _map.Text = Texts["map.button"];
        _inventory.Visible = _progress is not null;
        _xp.Text = _progress is (int level, long xp) ? Texts.XpLine(level, xp) : "";
        _talk.Visible = _town.Talk is not null;
        if (_town.Talk is not Conversation talk)
            return;
        _speaker.Text = talk.Npc.Name.In(Texts.Lang);
        _line.Text = talk.Line.Text.In(Texts.Lang);
        while (_answers.Count < talk.Line.Answers.Count)
        {
            int index = _answers.Count;
            var b = new Button { CustomMinimumSize = new Vector2(0, 44), Alignment = HorizontalAlignment.Left };
            b.Pressed += () => Answered?.Invoke(index);
            _answerBox.AddChild(b);
            _answers.Add(b);
        }
        for (int i = 0; i < _answers.Count; i++)
        {
            _answers[i].Visible = i < talk.Line.Answers.Count;
            if (_answers[i].Visible)
                _answers[i].Text = $"{i + 1}. {talk.Line.Answers[i].Text.In(Texts.Lang)}";
        }
    }

    public string SpeakerText => _talk.Visible ? _speaker.Text : "";
    public string LineText => _talk.Visible ? _line.Text : "";
    public IEnumerable<string> AnswersText => _answers.Where(b => b.Visible).Select(b => b.Text);

    private static Label Text(Control parent, int size, float x, float y, float w, float h)
    {
        var label = new Label { MouseFilter = Control.MouseFilterEnum.Ignore, OffsetLeft = x, OffsetTop = y, OffsetRight = x + w, OffsetBottom = y + h };
        label.AddThemeFontSizeOverride("font_size", size);
        label.AddThemeColorOverride("font_outline_color", Colors.Black);
        label.AddThemeConstantOverride("outline_size", 6);
        parent.AddChild(label);
        return label;
    }
}
