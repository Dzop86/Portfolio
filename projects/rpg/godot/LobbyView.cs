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
    private readonly List<Button> _swatches = [], _hairSwatches = [], _skinSwatches = [], _classButtons = [];
    private readonly List<Button> _cards = [];
    private Lobby _lobby = null!;
    private VBoxContainer _signedOut = null!, _serverScreen = null!, _serverList = null!, _characterScreen = null!;
    private HBoxContainer _cardRow = null!;
    private Label _title = null!, _launcherText = null!, _serversTitle = null!, _serverName = null!, _message = null!, _classText = null!;
    private Label _createTitle = null!, _lookName = null!, _classLabel = null!, _colourLabel = null!, _hairLabel = null!, _skinLabel = null!, _heightLabel = null!, _buildLabel = null!, _nameLabel = null!;
    private HSlider _height = null!, _build = null!;
    private int _look, _class, _hair, _skin;
    private Button _lang = null!, _offline = null!, _changeServer = null!, _delete = null!, _create = null!, _close = null!;
    private Control _createPanel = null!;
    private Portrait _preview = null!;
    private ConfirmationDialog _confirm = null!;
    private Guid? _selected;
    private bool _busy;

    public Texts Texts { get; private set; } = new("en");

    public Button PlayButton { get; private set; } = null!;
    public LineEdit CharacterField { get; private set; } = null!;

    /// <summary>The outfit colour chosen (0 to 6): see <see cref="Looks.Paint"/>.</summary>
    public int Colour { get; private set; }

    /// <summary>The character being created, as the screen shows it.</summary>
    public Hero Appearance => new(CharacterField.Text.Trim(), Look, Class.Id, Colour, _hair, _skin, (int)_height.Value, (int)_build.Value);

    /// <summary>The character to play (null: offline, the scenario's own hero).</summary>
    public event Action<CharacterSummary?>? Chosen;

    public string Look => Hero.Looks[_look];

    public HeroClass Class => GameData.Embedded.Classes[_class];

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

    /// <summary>
    /// The creation screen, full screen: the model turning on the left with the look's arrows; on the
    /// right the class, the colours of outfit, hair and skin, height and build, the name.
    /// </summary>
    private void BuildCreatePanel(Control root)
    {
        _createPanel = new Control { Visible = false, MouseFilter = Control.MouseFilterEnum.Ignore };
        _createPanel.SetAnchorsPreset(Control.LayoutPreset.FullRect);
        root.AddChild(_createPanel);
        var row = new HBoxContainer { AnchorRight = 1, AnchorBottom = 1, OffsetLeft = 32, OffsetTop = 84, OffsetRight = -32, OffsetBottom = -24 };
        row.AddThemeConstantOverride("separation", 28);
        _createPanel.AddChild(row);

        var left = new VBoxContainer { Alignment = BoxContainer.AlignmentMode.Center };
        left.AddThemeConstantOverride("separation", 10);
        row.AddChild(left);
        _preview = Portrait.Make(new Vector2(420, 520), turning: true, distance: 2.9f);
        left.AddChild(_preview);
        var looks = new HBoxContainer { Alignment = BoxContainer.AlignmentMode.Center };
        looks.AddThemeConstantOverride("separation", 12);
        left.AddChild(looks);
        Button(looks, () => SelectLook(Hero.Looks[(_look + Hero.Looks.Count - 1) % Hero.Looks.Count])).Text = "◀";
        _lookName = new Label { CustomMinimumSize = new Vector2(160, 0), HorizontalAlignment = HorizontalAlignment.Center };
        _lookName.AddThemeFontSizeOverride("font_size", 20);
        looks.AddChild(_lookName);
        Button(looks, () => SelectLook(Hero.Looks[(_look + 1) % Hero.Looks.Count])).Text = "▶";

        var form = new VBoxContainer { SizeFlagsHorizontal = Control.SizeFlags.ExpandFill };
        form.AddThemeConstantOverride("separation", 6);
        row.AddChild(form);
        _createTitle = new Label();
        _createTitle.AddThemeFontSizeOverride("font_size", 28);
        form.AddChild(_createTitle);
        _classLabel = Caption(form);
        var classes = new HBoxContainer();
        classes.AddThemeConstantOverride("separation", 8);
        form.AddChild(classes);
        foreach (HeroClass c in GameData.Embedded.Classes)
        {
            Button b = Button(classes, () => SelectClass(c.Id));
            b.ToggleMode = true;
            b.CustomMinimumSize = new Vector2(150, 44);
            _classButtons.Add(b);
        }
        _classText = new Label { AutowrapMode = TextServer.AutowrapMode.WordSmart, CustomMinimumSize = new Vector2(0, 40) };
        _classText.AddThemeFontSizeOverride("font_size", 14);
        form.AddChild(_classText);
        _colourLabel = Caption(form);
        Swatches(form, _swatches, Hero.Colours, ChooseColour);
        _hairLabel = Caption(form);
        Swatches(form, _hairSwatches, Hero.HairColours, ChooseHair);
        _skinLabel = Caption(form);
        Swatches(form, _skinSwatches, Hero.SkinTones, ChooseSkin);
        var shape = new HBoxContainer();
        shape.AddThemeConstantOverride("separation", 24);
        form.AddChild(shape);
        var heightBox = new VBoxContainer();
        shape.AddChild(heightBox);
        _heightLabel = Caption(heightBox);
        _height = Slider(heightBox);
        var buildBox = new VBoxContainer();
        shape.AddChild(buildBox);
        _buildLabel = Caption(buildBox);
        _build = Slider(buildBox);
        _nameLabel = Caption(form);
        var buttons = new HBoxContainer();
        buttons.AddThemeConstantOverride("separation", 12);
        form.AddChild(buttons);
        CharacterField = new LineEdit { MaxLength = 20, CustomMinimumSize = new Vector2(260, 48) };
        CharacterField.TextSubmitted += text => _ = Create();
        buttons.AddChild(CharacterField);
        _create = Button(buttons, () => _ = Create());
        _create.CustomMinimumSize = new Vector2(160, 48);
        _close = Button(buttons, () =>
        {
            _createPanel.Visible = false;
            Refresh();
        });
    }

    private static Label Caption(Container parent)
    {
        var label = new Label();
        label.AddThemeFontSizeOverride("font_size", 15);
        label.Modulate = new Color(1, 1, 1, 0.8f);
        parent.AddChild(label);
        return label;
    }

    private HSlider Slider(Container parent)
    {
        var slider = new HSlider { MinValue = -Hero.Shape, MaxValue = Hero.Shape, Step = 1, TickCount = 2 * Hero.Shape + 1, TicksOnBorders = true, CustomMinimumSize = new Vector2(240, 28), SizeFlagsHorizontal = Control.SizeFlags.ShrinkBegin };
        slider.ValueChanged += _ => ShowPreview();
        parent.AddChild(slider);
        return slider;
    }

    private static void Swatches(Container parent, List<Button> list, int count, Action<int> choose)
    {
        var line = new HBoxContainer();
        line.AddThemeConstantOverride("separation", 6);
        parent.AddChild(line);
        for (int i = 0; i < count; i++)
        {
            int index = i;
            var swatch = new Button { CustomMinimumSize = new Vector2(44, 44), ToggleMode = true };
            var box = new StyleBoxFlat();
            box.SetCornerRadiusAll(6);
            var chosen = (StyleBoxFlat)box.Duplicate();
            chosen.BorderColor = Colors.White;
            chosen.SetBorderWidthAll(4);
            swatch.AddThemeStyleboxOverride("normal", box);
            swatch.AddThemeStyleboxOverride("hover", box);
            swatch.AddThemeStyleboxOverride("pressed", chosen);
            swatch.Pressed += () => choose(index);
            line.AddChild(swatch);
            list.Add(swatch);
        }
    }

    public void SelectLook(string look)
    {
        _look = Math.Max(0, Hero.Looks.ToList().IndexOf(look));
        ShowPreview();
    }

    public void SelectClass(string id)
    {
        _class = Math.Max(0, GameData.Embedded.Classes.ToList().FindIndex(c => c.Id == id));
        Refresh();
    }

    public void ChooseHair(int hair)
    {
        _hair = hair;
        ShowPreview();
    }

    public void ChooseSkin(int skin)
    {
        _skin = skin;
        ShowPreview();
    }

    public void SetShape(int height, int build)
    {
        _height.Value = height;
        _build.Value = build;
        ShowPreview();
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
        ShowPreview();
    }

    /// <summary>The model as the choices say, and each swatch the colour it gives this look.</summary>
    private void ShowPreview()
    {
        int main = Looks.MainColumn(Look);
        (Vector2 hair, Vector2 skin) = Looks.CellsOf(Look);
        for (int i = 0; i < _swatches.Count; i++)
            Paint(_swatches[i], Looks.CellColour(new Vector2(1 + (main - 1 + i) % Hero.Colours, 2)), i == Colour);
        for (int i = 0; i < _hairSwatches.Count; i++)
            Paint(_hairSwatches[i], Looks.CellColour(i == 0 ? hair : Looks.HairCells[i]), i == _hair);
        for (int i = 0; i < _skinSwatches.Count; i++)
            Paint(_skinSwatches[i], Looks.CellColour(i == 0 ? skin : Looks.SkinCells[i]), i == _skin);
        _lookName.Text = Texts.Look(Look);
        _preview.Show(Appearance);
    }

    private static void Paint(Button swatch, Color colour, bool chosen)
    {
        foreach (string state in new[] { "normal", "hover", "pressed" })
            ((StyleBoxFlat)swatch.GetThemeStylebox(state)).BgColor = colour;
        swatch.SetPressedNoSignal(chosen);
    }

    /// <summary>The create button: a character with the name typed and the look shown, then chosen.</summary>
    public async Task Create()
    {
        if (_busy)
            return;
        string name = CharacterField.Text.Trim();
        if (await Busy(() => _lobby.Create(Appearance)))
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

        _createTitle.Text = Texts["lobby.new"];
        _classLabel.Text = Texts["lobby.class"];
        for (int i = 0; i < _classButtons.Count; i++)
        {
            _classButtons[i].Text = GameData.Embedded.Classes[i].Name.In(Texts.Lang);
            _classButtons[i].SetPressedNoSignal(i == _class);
        }
        _classText.Text = Texts.Class(Class);
        _colourLabel.Text = Texts["lobby.colour"];
        _hairLabel.Text = Texts["lobby.hair"];
        _skinLabel.Text = Texts["lobby.skin"];
        _heightLabel.Text = Texts["lobby.height"];
        _buildLabel.Text = Texts["lobby.build"];
        _nameLabel.Text = Texts["lobby.character-name"];
        _lookName.Text = Texts.Look(Look);
        CharacterField.PlaceholderText = Texts["lobby.character-name"];
        _create.Text = Texts["lobby.create"];
        _create.Disabled = _busy;
        _close.Text = Texts["lobby.cancel"];
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
        portrait.Show(c.Hero);
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
