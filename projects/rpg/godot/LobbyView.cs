using Godot;
using Rpg.Client;
using Rpg.Core;

namespace Rpg.Desktop;

/// <summary>
/// The screens before the game, as in the games of the genre: without the launcher's token, a word
/// to open the launcher (or play offline); then the server choice (skipped when there is only one);
/// then the characters as cards (3D portrait, name, class, level), one chosen to play, deleted after
/// a confirmation, or created in a panel. Everything they can do is Rpg.Client's <see cref="Lobby"/>.
/// </summary>
public partial class LobbyView : CanvasLayer
{
    /// <summary>The palette's coloured columns, green to purple: a swatch shows what the look's main colour becomes.</summary>
    private static readonly Color[] SwatchColours =
        [new("5fc98a"), new("ffbf45"), new("ff7f45"), new("cc5252"), new("6496d8"), new("cfe4ff"), new("a876e0")];

    private readonly List<Button> _swatches = [];
    private readonly List<Button> _cards = [];
    private Lobby _lobby = null!;
    private VBoxContainer _signedOut = null!, _serverScreen = null!, _serverList = null!, _characterScreen = null!;
    private HBoxContainer _cardRow = null!;
    private Label _title = null!, _launcherText = null!, _serversTitle = null!, _serverName = null!, _message = null!, _classText = null!, _colourLabel = null!;
    private Button _lang = null!, _offline = null!, _changeServer = null!, _delete = null!, _create = null!, _close = null!;
    private PanelContainer _createPanel = null!;
    private Portrait _preview = null!;
    private ConfirmationDialog _confirm = null!;
    private Guid? _selected;
    private bool _busy;

    public Texts Texts { get; private set; } = new("en");

    public Button PlayButton { get; private set; } = null!;
    public LineEdit CharacterField { get; private set; } = null!;
    public OptionButton LookChoice { get; private set; } = null!;
    public OptionButton ClassChoice { get; private set; } = null!;

    /// <summary>The outfit colour chosen (0 to 6): see <see cref="Looks.Paint"/>.</summary>
    public int Colour { get; private set; }

    /// <summary>The character to play (null: offline, the scenario's own hero).</summary>
    public event Action<CharacterSummary?>? Chosen;

    public string Look => Hero.Looks[Math.Max(0, LookChoice.Selected)];

    public HeroClass Class => GameData.Embedded.Classes[Math.Max(0, ClassChoice.Selected)];

    public string MessageText => _message.Text;

    public bool CreateOpen => _createPanel.Visible;

    /// <summary>What each character card says (name, class and level), in order.</summary>
    public IEnumerable<string> CardTexts =>
        _cards.Where(c => (string)c.GetMeta("id") != Guid.Empty.ToString())
            .Select(c => string.Join(" · ", c.FindChildren("*", nameof(Label), true, false).OfType<Label>().Select(l => l.Text)));

    public void Build(Lobby lobby, Texts texts)
    {
        _lobby = lobby;
        Texts = texts;
        var root = new Control { MouseFilter = Control.MouseFilterEnum.Ignore };
        root.SetAnchorsPreset(Control.LayoutPreset.FullRect);
        AddChild(root);
        _title = new Label { Position = new Vector2(32, 18) };
        _title.AddThemeFontSizeOverride("font_size", 40);
        root.AddChild(_title);
        _lang = new Button { CustomMinimumSize = new Vector2(56, 44), TooltipText = "Français / English", AnchorLeft = 1, AnchorRight = 1, OffsetLeft = -80, OffsetRight = -24, OffsetTop = 24, OffsetBottom = 68 };
        _lang.Pressed += () =>
        {
            Texts = new Texts(Texts.Lang == "fr" ? "en" : "fr");
            Refresh();
        };
        root.AddChild(_lang);

        var centre = new CenterContainer { MouseFilter = Control.MouseFilterEnum.Ignore };
        centre.SetAnchorsPreset(Control.LayoutPreset.FullRect);
        root.AddChild(centre);
        var screens = new VBoxContainer { Alignment = BoxContainer.AlignmentMode.Center };
        screens.AddThemeConstantOverride("separation", 16);
        centre.AddChild(screens);

        // Not signed in: the launcher signs in.
        _signedOut = Column(screens);
        _launcherText = Text(_signedOut, 22);
        _offline = Button(_signedOut, () => Chosen?.Invoke(null));
        _offline.SizeFlagsHorizontal = Control.SizeFlags.ShrinkCenter;

        // The servers.
        _serverScreen = Column(screens);
        _serversTitle = Text(_serverScreen, 28);
        _serverList = Column(_serverScreen);

        // The characters.
        _characterScreen = Column(screens);
        var heading = new HBoxContainer { Alignment = BoxContainer.AlignmentMode.Center };
        heading.AddThemeConstantOverride("separation", 16);
        _characterScreen.AddChild(heading);
        _serverName = new Label();
        _serverName.AddThemeFontSizeOverride("font_size", 26);
        heading.AddChild(_serverName);
        _changeServer = Button(heading, () =>
        {
            _lobby.ChooseServer(null);
            Refresh();
        });
        _cardRow = new HBoxContainer { Alignment = BoxContainer.AlignmentMode.Center };
        _cardRow.AddThemeConstantOverride("separation", 14);
        _characterScreen.AddChild(_cardRow);
        var actions = new HBoxContainer { Alignment = BoxContainer.AlignmentMode.Center };
        actions.AddThemeConstantOverride("separation", 14);
        _characterScreen.AddChild(actions);
        PlayButton = Button(actions, () =>
        {
            if (_lobby.Here.FirstOrDefault(c => c.Id == _selected) is CharacterSummary c)
                Chosen?.Invoke(c);
        });
        PlayButton.CustomMinimumSize = new Vector2(220, 52);
        PlayButton.AddThemeFontSizeOverride("font_size", 22);
        _delete = Button(actions, () =>
        {
            if (_lobby.Here.FirstOrDefault(c => c.Id == _selected) is CharacterSummary c)
            {
                _confirm.DialogText = Texts["lobby.confirm-delete", c.Name];
                _confirm.PopupCentered();
            }
        });
        _confirm = new ConfirmationDialog { Exclusive = true };
        _confirm.Confirmed += () => _ = DeleteSelected();
        AddChild(_confirm);

        _message = new Label { AutowrapMode = TextServer.AutowrapMode.WordSmart, HorizontalAlignment = HorizontalAlignment.Center, CustomMinimumSize = new Vector2(700, 30) };
        _message.AddThemeColorOverride("font_color", new Color("ffb4a2"));
        screens.AddChild(_message);

        BuildCreatePanel(root);
        Refresh();
    }

    /// <summary>The creation panel (until its own screen): a turning preview, name, look, class, colour.</summary>
    private void BuildCreatePanel(Control root)
    {
        _createPanel = new PanelContainer { Visible = false };
        _createPanel.SetAnchorsPreset(Control.LayoutPreset.Center);
        _createPanel.GrowHorizontal = Control.GrowDirection.Both;
        _createPanel.GrowVertical = Control.GrowDirection.Both;
        root.AddChild(_createPanel);
        var margin = new MarginContainer();
        foreach (string side in new[] { "left", "right", "top", "bottom" })
            margin.AddThemeConstantOverride("margin_" + side, 20);
        _createPanel.AddChild(margin);
        var row = new HBoxContainer();
        row.AddThemeConstantOverride("separation", 20);
        margin.AddChild(row);
        _preview = Portrait.Make(new Vector2(260, 330), turning: true);
        row.AddChild(_preview);
        var form = new VBoxContainer { CustomMinimumSize = new Vector2(480, 0) };
        form.AddThemeConstantOverride("separation", 12);
        row.AddChild(form);
        CharacterField = new LineEdit { MaxLength = 20, CustomMinimumSize = new Vector2(0, 44) };
        CharacterField.TextSubmitted += text => _ = Create();
        form.AddChild(CharacterField);
        LookChoice = new OptionButton { CustomMinimumSize = new Vector2(0, 44) };
        foreach (string look in Hero.Looks)
            LookChoice.AddItem(look);
        LookChoice.Select(0);
        LookChoice.ItemSelected += _ => ShowPreview();
        form.AddChild(LookChoice);
        ClassChoice = new OptionButton { CustomMinimumSize = new Vector2(0, 44) };
        foreach (HeroClass c in GameData.Embedded.Classes)
            ClassChoice.AddItem(c.Id);
        ClassChoice.Select(0);
        ClassChoice.ItemSelected += _ => Refresh();
        form.AddChild(ClassChoice);
        _classText = new Label { AutowrapMode = TextServer.AutowrapMode.WordSmart };
        _classText.AddThemeFontSizeOverride("font_size", 14);
        form.AddChild(_classText);
        var colourRow = new HBoxContainer();
        colourRow.AddThemeConstantOverride("separation", 6);
        form.AddChild(colourRow);
        _colourLabel = new Label { VerticalAlignment = VerticalAlignment.Center };
        colourRow.AddChild(_colourLabel);
        for (int i = 0; i < Hero.Colours; i++)
        {
            int colour = i;
            var swatch = new Button { CustomMinimumSize = new Vector2(44, 44), ToggleMode = true };
            var box = new StyleBoxFlat { BgColor = SwatchColours[i], CornerRadiusTopLeft = 6, CornerRadiusTopRight = 6, CornerRadiusBottomLeft = 6, CornerRadiusBottomRight = 6 };
            var chosen = (StyleBoxFlat)box.Duplicate();
            chosen.BorderColor = Colors.White;
            chosen.SetBorderWidthAll(4);
            swatch.AddThemeStyleboxOverride("normal", box);
            swatch.AddThemeStyleboxOverride("hover", box);
            swatch.AddThemeStyleboxOverride("pressed", chosen);
            swatch.Pressed += () => ChooseColour(colour);
            colourRow.AddChild(swatch);
            _swatches.Add(swatch);
        }
        var buttons = new HBoxContainer();
        buttons.AddThemeConstantOverride("separation", 12);
        form.AddChild(buttons);
        _create = Button(buttons, () => _ = Create());
        _close = Button(buttons, () =>
        {
            _createPanel.Visible = false;
            Refresh();
        });
    }

    public void OpenCreate()
    {
        _createPanel.Visible = true;
        CharacterField.Text = "";
        ShowPreview();
        Refresh();
        CharacterField.GrabFocus();
    }

    public void ChooseColour(int colour)
    {
        Colour = colour;
        for (int i = 0; i < _swatches.Count; i++)
            _swatches[i].SetPressedNoSignal(i == colour);
        ShowPreview();
    }

    private void ShowPreview()
    {
        int main = Looks.MainColumn(Look);
        for (int i = 0; i < _swatches.Count; i++)
        {
            Color colour = SwatchColours[(main - 1 + i) % Hero.Colours];
            foreach (string state in new[] { "normal", "hover", "pressed" })
                ((StyleBoxFlat)_swatches[i].GetThemeStylebox(state)).BgColor = colour;
        }
        _preview.Show(Look, Colour);
    }

    /// <summary>The create button: a character with the name typed and the look shown, then chosen.</summary>
    public async Task Create()
    {
        if (_busy)
            return;
        string name = CharacterField.Text.Trim();
        if (await Busy(() => _lobby.Create(name, Look, Class.Id, Colour)))
        {
            _createPanel.Visible = false;
            _selected = _lobby.Here.FirstOrDefault(c => c.Name == name)?.Id;
            Refresh();
        }
    }

    /// <summary>Chooses a card, as a click does.</summary>
    public void Select(Guid id)
    {
        _selected = id;
        Refresh();
    }

    private async Task DeleteSelected()
    {
        if (_selected is Guid id && await Busy(() => _lobby.Delete(id)))
        {
            _selected = null;
            Refresh();
        }
    }

    private async Task<bool> Busy(Func<Task<bool>> call)
    {
        _busy = true;
        _message.Text = Texts["lobby.wait"];
        bool done = await call();
        _busy = false;
        Refresh();
        return done;
    }

    /// <summary>Redraws the screen from the lobby's state and the language.</summary>
    public void Refresh()
    {
        _title.Text = Texts["lobby.title"];
        _lang.Text = Texts.Lang == "fr" ? "EN" : "FR";
        bool chosen = _lobby.SignedIn && _lobby.ChosenServer is not null;
        _signedOut.Visible = !_lobby.SignedIn;
        _serverScreen.Visible = _lobby.SignedIn && !chosen;
        _characterScreen.Visible = chosen && !_createPanel.Visible;
        _launcherText.Text = Texts["lobby.launcher"];
        _offline.Text = Texts["lobby.offline"];
        _serversTitle.Text = Texts["lobby.servers"];
        RefreshServers();
        if (chosen)
            RefreshCards();
        _changeServer.Visible = _lobby.Servers.Count > 1;
        _changeServer.Text = Texts["lobby.change-server"];
        PlayButton.Text = Texts["lobby.play"];
        PlayButton.Disabled = _busy || _lobby.Here.All(c => c.Id != _selected);
        _delete.Text = Texts["lobby.delete"];
        _delete.Disabled = PlayButton.Disabled;
        _confirm.OkButtonText = Texts["lobby.delete"];
        _confirm.CancelButtonText = Texts["lobby.cancel"];
        _confirm.Title = Texts["lobby.title"];

        CharacterField.PlaceholderText = Texts["lobby.character-name"];
        for (int i = 0; i < Hero.Looks.Count; i++)
            LookChoice.SetItemText(i, Texts.Look(Hero.Looks[i]));
        for (int i = 0; i < GameData.Embedded.Classes.Count; i++)
            ClassChoice.SetItemText(i, GameData.Embedded.Classes[i].Name.In(Texts.Lang));
        _classText.Text = Texts.Class(Class);
        _colourLabel.Text = Texts["lobby.colour"];
        for (int i = 0; i < _swatches.Count; i++)
        {
            _swatches[i].TooltipText = Texts["lobby.colour-n", i + 1];
            _swatches[i].SetPressedNoSignal(i == Colour);
        }
        _create.Text = Texts["lobby.create"];
        _create.Disabled = _busy;
        _close.Text = Texts["lobby.close"];
        _message.Text = _busy ? Texts["lobby.wait"] : _lobby.Problem is string key ? Texts[key] : "";
    }

    private void RefreshServers()
    {
        foreach (Node n in _serverList.GetChildren())
        {
            _serverList.RemoveChild(n);
            n.QueueFree();
        }
        foreach (ServerInfo s in _lobby.Servers)
        {
            int count = _lobby.Characters.Count(c => c.Server == s.Id);
            Button b = Button(_serverList, () =>
            {
                _lobby.ChooseServer(s.Id);
                _selected = null;
                Refresh();
            });
            b.Text = $"{s.Name}  ·  {Texts["lobby.server-characters", count]}";
            b.CustomMinimumSize = new Vector2(380, 52);
        }
    }

    /// <summary>One card per character of the chosen server, then "create" while there is room.</summary>
    private void RefreshCards()
    {
        _serverName.Text = _lobby.ChosenServer!.Name;
        IReadOnlyList<CharacterSummary> here = _lobby.Here;
        string[] ids = [.. here.Select(c => c.Id.ToString()), .. _lobby.CanCreate ? [Guid.Empty.ToString()] : Array.Empty<string>()];
        if (!_cards.Select(c => (string)c.GetMeta("id")).SequenceEqual(ids))
        {
            foreach (Button card in _cards)
            {
                _cardRow.RemoveChild(card);
                card.QueueFree();
            }
            _cards.Clear();
            foreach (CharacterSummary c in here)
                _cards.Add(Card(c));
            if (_lobby.CanCreate)
            {
                var add = new Button { CustomMinimumSize = new Vector2(170, 270) };
                add.AddThemeFontSizeOverride("font_size", 18);
                add.Pressed += OpenCreate;
                add.SetMeta("id", Guid.Empty.ToString());
                _cardRow.AddChild(add);
                _cards.Add(add);
            }
        }
        for (int i = 0; i < here.Count; i++)
        {
            CharacterSummary c = here[i];
            Label[] labels = [.. _cards[i].FindChildren("*", nameof(Label), true, false).OfType<Label>()];
            labels[0].Text = c.Name;
            labels[1].Text = GameData.Embedded.Class(c.Class)?.Name.In(Texts.Lang) ?? c.Class;
            labels[2].Text = Texts["lobby.level", c.Level];
            _cards[i].SetPressedNoSignal(c.Id == _selected);
        }
        if (_lobby.CanCreate)
            _cards[^1].Text = "+\n" + Texts["lobby.new"];
    }

    private Button Card(CharacterSummary c)
    {
        var card = new Button { CustomMinimumSize = new Vector2(170, 270), ToggleMode = true };
        card.SetMeta("id", c.Id.ToString());
        var selected = new StyleBoxFlat { BgColor = new Color(0.75f, 0.95f, 0.45f, 0.18f), BorderColor = FighterView.PlayerColour };
        selected.SetBorderWidthAll(3);
        selected.SetCornerRadiusAll(8);
        card.AddThemeStyleboxOverride("pressed", selected);
        card.Pressed += () => Select(c.Id);
        var column = new VBoxContainer { MouseFilter = Control.MouseFilterEnum.Ignore, Alignment = BoxContainer.AlignmentMode.Center };
        column.SetAnchorsPreset(Control.LayoutPreset.FullRect);
        card.AddChild(column);
        Portrait portrait = Portrait.Make(new Vector2(150, 180), turning: false);
        portrait.SizeFlagsHorizontal = Control.SizeFlags.ShrinkCenter;
        column.AddChild(portrait);
        portrait.Show(c.Look, c.Colour);
        foreach (int size in new[] { 18, 14, 14 })
        {
            var label = new Label { HorizontalAlignment = HorizontalAlignment.Center, MouseFilter = Control.MouseFilterEnum.Ignore };
            label.AddThemeFontSizeOverride("font_size", size);
            column.AddChild(label);
        }
        _cardRow.AddChild(card);
        return card;
    }

    private static VBoxContainer Column(Container parent)
    {
        var column = new VBoxContainer { Alignment = BoxContainer.AlignmentMode.Center };
        column.AddThemeConstantOverride("separation", 14);
        parent.AddChild(column);
        return column;
    }

    private static Label Text(Container parent, int size)
    {
        var label = new Label { HorizontalAlignment = HorizontalAlignment.Center };
        label.AddThemeFontSizeOverride("font_size", size);
        parent.AddChild(label);
        return label;
    }

    private static Button Button(Container parent, Action pressed)
    {
        var button = new Button { CustomMinimumSize = new Vector2(140, 44) };
        button.Pressed += pressed;
        parent.AddChild(button);
        return button;
    }
}
