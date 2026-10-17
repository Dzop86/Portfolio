using Godot;
using Rpg.Client;
using Rpg.Core;

namespace Rpg.Desktop;

/// <summary>
/// The screen over the town: its name and the help line, the language button, the character's life in a
/// heart, a message line, and the talk: who speaks, what they say, the answers. The windows
/// open from the side bar (<see cref="SideBar"/>), the way back to the characters from the menu.
/// </summary>
public partial class TalkPanel : CanvasLayer
{
    private readonly List<Button> _answers = [];
    private Label _title = null!, _help = null!, _message = null!, _speaker = null!, _line = null!;
    private HeartGauge _life = null!;

    /// <summary>The character whose life is shown, growing back as <see cref="Rpg.Core.Life"/> says (sprint 101); null: none.</summary>
    public CharacterSummary? Living { get; set; }

    /// <summary>The life in the heart as shown (for the self-test).</summary>
    public string LifeText => _life.Visible ? _life.Text : "";

    public override void _Process(double delta)
    {
        if (Living is not CharacterSummary c || c.Hero.Class is null)
        {
            _life.Show(0, 0, false);
            return;
        }
        int max = Rpg.Core.Life.MaxHp(c.Hero, Rpg.Core.GameData.Embedded);
        int now = c.LifeAt(DateTimeOffset.UtcNow, Rpg.Core.GameData.Embedded);
        _life.Show(now, max, Rpg.Core.Life.Low(now, max));
        _life.TooltipText = Texts["life.line", now, max] + "\n" + Texts["life.rate", Math.Round(Rpg.Core.Life.PerSecond(max), 1)];
    }
    private Button _lang = null!;
    private PanelContainer _talk = null!;
    private VBoxContainer _answerBox = null!;
    private ScrollContainer _answerScroll = null!;

    /// <summary>The most answers seen at once; the others are scrolled to.</summary>
    public const int AnswersSeen = 6;
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
        // The gathering's bar (sprint 103): centred low on the screen, filling over its three seconds.
        _gather = new VBoxContainer { MouseFilter = Control.MouseFilterEnum.Ignore, AnchorLeft = 0.5f, AnchorRight = 0.5f, AnchorTop = 1, AnchorBottom = 1, OffsetLeft = -160, OffsetRight = 160, OffsetTop = -190, OffsetBottom = -140, Visible = false };
        _gatherLabel = new Label { HorizontalAlignment = HorizontalAlignment.Center, MouseFilter = Control.MouseFilterEnum.Ignore };
        _gatherLabel.AddThemeColorOverride("font_outline_color", Colors.Black);
        _gatherLabel.AddThemeConstantOverride("outline_size", 6);
        _gather.AddChild(_gatherLabel);
        _gatherBar = new ProgressBar { MaxValue = 1, Step = 0.001, ShowPercentage = false, CustomMinimumSize = new Vector2(320, 14), MouseFilter = Control.MouseFilterEnum.Ignore };
        _gatherBar.AddThemeStyleboxOverride("background", new StyleBoxFlat { BgColor = new Color(0.12f, 0.12f, 0.12f, 0.9f), CornerRadiusTopLeft = 7, CornerRadiusTopRight = 7, CornerRadiusBottomLeft = 7, CornerRadiusBottomRight = 7 });
        _gatherBar.AddThemeStyleboxOverride("fill", new StyleBoxFlat { BgColor = new Color("bef374"), CornerRadiusTopLeft = 7, CornerRadiusTopRight = 7, CornerRadiusBottomLeft = 7, CornerRadiusBottomRight = 7 });
        _gather.AddChild(_gatherBar);
        root.AddChild(_gather);
        // A notice in the middle of the screen (sprint 103: a trade's new level), fading after a while.
        _notice = new Label { HorizontalAlignment = HorizontalAlignment.Center, MouseFilter = Control.MouseFilterEnum.Ignore, AnchorLeft = 0.5f, AnchorRight = 0.5f, AnchorTop = 0.3f, AnchorBottom = 0.3f, OffsetLeft = -400, OffsetRight = 400, OffsetBottom = 48, Visible = false };
        _notice.AddThemeFontSizeOverride("font_size", 30);
        _notice.AddThemeColorOverride("font_color", new Color("bef374"));
        _notice.AddThemeColorOverride("font_outline_color", Colors.Black);
        _notice.AddThemeConstantOverride("outline_size", 8);
        root.AddChild(_notice);

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

        // The life in a heart (sprint 115), where the experience line was: it said nothing useful in the town.
        _life = new HeartGauge { AnchorLeft = 1, AnchorRight = 1, OffsetLeft = -116, OffsetRight = -20, OffsetTop = 62, OffsetBottom = 150, Visible = false };
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
        _answerBox = new VBoxContainer { SizeFlagsHorizontal = Control.SizeFlags.ExpandFill };
        _answerBox.AddThemeConstantOverride("separation", 4);
        // At most six answers seen at once, the others scrolled to (sprint 102: a talk no longer leaves the screen).
        var answers = new ScrollContainer { HorizontalScrollMode = ScrollContainer.ScrollMode.Disabled, CustomMinimumSize = new Vector2(0, 0) };
        answers.AddChild(_answerBox);
        _answerScroll = answers;
        column.AddChild(answers);
        Refresh();
    }

    public void ShowMessage(string text) => _message.Text = text;

    public string MessageText => _message.Text;

    private VBoxContainer _gather = null!;
    private Label _gatherLabel = null!, _notice = null!;
    private ProgressBar _gatherBar = null!;
    private Tween? _noticeFade;

    /// <summary>The gathering's bar at <paramref name="progress"/> (0 to 1) under its label; null hides it.</summary>
    public void ShowGathering(double? progress, string label = "")
    {
        _gather.Visible = progress is not null;
        _gatherBar.Value = progress ?? 0;
        _gatherLabel.Text = label;
    }

    /// <summary>The gathering bar's fill, null when hidden (for the self-test).</summary>
    public double? GatheringShown => _gather.Visible ? _gatherBar.Value : null;

    /// <summary>A notice in the middle of the screen for a few seconds.</summary>
    public void ShowNotice(string text)
    {
        _noticeFade?.Kill();
        _notice.Text = text;
        _notice.Visible = true;
        _notice.Modulate = Colors.White;
        _noticeFade = CreateTween();
        _noticeFade.TweenInterval(2.5);
        _noticeFade.TweenProperty(_notice, "modulate:a", 0f, 0.8);
        _noticeFade.TweenCallback(Callable.From(() => _notice.Visible = false));
    }

    public string NoticeText => _notice.Visible ? _notice.Text : "";

    /// <summary>Redraws the texts and the talk from the town's state and the language.</summary>
    public void Refresh()
    {
        _title.Text = Texts.MapTitle(_town.Town);
        _help.Text = Texts["town.help"];
        _lang.Text = Texts.Lang == "fr" ? "EN" : "FR";
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
                _answers[i].Text = i < 9 ? $"{i + 1}. {talk.Line.Answers[i].Text.In(Texts.Lang)}" : talk.Line.Answers[i].Text.In(Texts.Lang);
        }
        _answerScroll.CustomMinimumSize = new Vector2(0, Math.Min(talk.Line.Answers.Count, AnswersSeen) * 48);
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
