using Godot;
using Rpg.Client;
using Rpg.Core;

namespace Rpg.Desktop;

/// <summary>
/// The screen over the town: its name and the help line, the language button, the character's level
/// and experience, a message line, and the talk: who speaks, what they say, the answers. The windows
/// open from the side bar (<see cref="SideBar"/>), the way back to the characters from the menu.
/// </summary>
public partial class TalkPanel : CanvasLayer
{
    private readonly List<Button> _answers = [];
    private Label _title = null!, _help = null!, _message = null!, _speaker = null!, _line = null!, _xp = null!, _life = null!;

    /// <summary>The character whose life is shown, growing back as <see cref="Rpg.Core.Life"/> says (sprint 101); null: none.</summary>
    public CharacterSummary? Living { get; set; }

    /// <summary>The life line as shown (for the self-test).</summary>
    public string LifeText => _life.Text;

    public override void _Process(double delta)
    {
        if (Living is not CharacterSummary c || c.Hero.Class is null)
        {
            _life.Text = "";
            return;
        }
        int max = Rpg.Core.Life.MaxHp(c.Hero, Rpg.Core.GameData.Embedded);
        int now = c.LifeAt(DateTimeOffset.UtcNow, Rpg.Core.GameData.Embedded);
        _life.Text = Texts["life.line", now, max];
        _life.TooltipText = Texts["life.rate", Math.Round(Rpg.Core.Life.PerSecond(max), 1)];
        _life.Modulate = Rpg.Core.Life.Low(now, max) ? new Color("ff8a8a") : new Color(ElementStyle.Heal);
    }
    private Button _lang = null!;
    private (int Level, long Xp)? _progress;
    private PanelContainer _talk = null!;
    private VBoxContainer _answerBox = null!;
    private TownController _town = null!;

    public Texts Texts { get; private set; } = new("en");

    public event Action<int>? Answered;
    public event Action? LanguageChanged;

    public void Build(TownController town, Texts texts)
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
        _life = new Label { AnchorLeft = 1, AnchorRight = 1, OffsetLeft = -520, OffsetRight = -20, OffsetTop = 86, OffsetBottom = 110, HorizontalAlignment = HorizontalAlignment.Right, MouseFilter = Control.MouseFilterEnum.Stop };
        _life.AddThemeFontSizeOverride("font_size", 16);
        _life.AddThemeColorOverride("font_outline_color", Colors.Black);
        _life.AddThemeConstantOverride("outline_size", 6);
        root.AddChild(_life);

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

    public string MessageText => _message.Text;

    /// <summary>The character's level and experience; null: no character (a classless hero).</summary>
    public void SetProgress(int? level, long xp)
    {
        _progress = level is int l ? (l, xp) : null;
        Refresh();
    }

    public string XpText => _xp.Text;

    /// <summary>Redraws the texts and the talk from the town's state and the language.</summary>
    public void Refresh()
    {
        _title.Text = Texts.MapTitle(_town.Town);
        _help.Text = Texts["town.help"];
        _lang.Text = Texts.Lang == "fr" ? "EN" : "FR";
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
