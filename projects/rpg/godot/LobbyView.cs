using Godot;
using Rpg.Client;
using Rpg.Core;

namespace Rpg.Desktop;

/// <summary>
/// The screen before the fight: sign in or create an account, then pick, create or delete a character
/// (its model turns next to the form). Everything it can do is Rpg.Client's <see cref="Lobby"/>; this
/// only draws it. <see cref="Chosen"/> gives the hero to fight with, or null to play offline.
/// </summary>
public partial class LobbyView : CanvasLayer
{
    private Lobby _lobby = null!;
    private string _serverUrl = "";
    private VBoxContainer _signInForm = null!, _charactersForm = null!, _list = null!;
    private Label _title = null!, _server = null!, _message = null!, _listTitle = null!, _classText = null!, _colourLabel = null!;
    private readonly List<Button> _swatches = [];
    private Button _signIn = null!, _signUp = null!, _offline = null!, _create = null!, _signOut = null!, _lang = null!;
    private bool _busy;

    public Texts Texts { get; private set; } = new("en");

    public LineEdit NameField { get; private set; } = null!;
    public LineEdit PasswordField { get; private set; } = null!;
    public LineEdit CharacterField { get; private set; } = null!;
    public OptionButton LookChoice { get; private set; } = null!;
    public OptionButton ClassChoice { get; private set; } = null!;

    /// <summary>The outfit colour chosen (0 to 6): see <see cref="Looks.Paint"/>.</summary>
    public int Colour { get; private set; }

    /// <summary>The character to play (null: offline, the scenario's own hero).</summary>
    public event Action<CharacterSummary?>? Chosen;

    /// <summary>The model to show next to the form: a look and an outfit colour.</summary>
    public event Action<string, int>? PreviewShown;

    public string Look => Hero.Looks[Math.Max(0, LookChoice.Selected)];

    public HeroClass Class => GameData.Embedded.Classes[Math.Max(0, ClassChoice.Selected)];

    /// <summary>The palette's coloured columns, green to purple: a swatch shows what the look's main colour becomes.</summary>
    private static readonly Color[] SwatchColours =
        [new("5fc98a"), new("ffbf45"), new("ff7f45"), new("cc5252"), new("6496d8"), new("cfe4ff"), new("a876e0")];

    public string MessageText => _message.Text;

    public void Build(Lobby lobby, string serverUrl, Texts texts)
    {
        _lobby = lobby;
        _serverUrl = serverUrl;
        Texts = texts;
        var panel = new PanelContainer();
        panel.AnchorLeft = panel.AnchorRight = 0.5f;
        panel.AnchorTop = panel.AnchorBottom = 0.5f;
        panel.OffsetLeft = -560;
        panel.OffsetRight = -40;
        panel.OffsetTop = -345;
        panel.OffsetBottom = 345;
        AddChild(panel);
        var margin = new MarginContainer();
        foreach (string side in new[] { "left", "right", "top", "bottom" })
            margin.AddThemeConstantOverride("margin_" + side, 24);
        panel.AddChild(margin);
        var column = new VBoxContainer();
        column.AddThemeConstantOverride("separation", 14);
        margin.AddChild(column);

        var header = new HBoxContainer();
        column.AddChild(header);
        _title = new Label { SizeFlagsHorizontal = Control.SizeFlags.ExpandFill };
        _title.AddThemeFontSizeOverride("font_size", 34);
        header.AddChild(_title);
        _lang = Button(header, () =>
        {
            Texts = new Texts(Texts.Lang == "fr" ? "en" : "fr");
            Refresh();
        });
        _lang.CustomMinimumSize = new Vector2(56, 44);
        _lang.TooltipText = "Français / English";
        _server = new Label { Modulate = new Color(1, 1, 1, 0.7f) };
        column.AddChild(_server);

        _signInForm = new VBoxContainer();
        _signInForm.AddThemeConstantOverride("separation", 12);
        column.AddChild(_signInForm);
        NameField = Field(_signInForm, 20);
        PasswordField = Field(_signInForm, 128);
        PasswordField.Secret = true;
        PasswordField.TextSubmitted += text => _ = SignIn(create: false);
        var buttons = new HBoxContainer();
        buttons.AddThemeConstantOverride("separation", 10);
        _signInForm.AddChild(buttons);
        _signIn = Button(buttons, () => _ = SignIn(create: false));
        _signUp = Button(buttons, () => _ = SignIn(create: true));
        _offline = Button(_signInForm, () => Chosen?.Invoke(null));

        _charactersForm = new VBoxContainer { Visible = false };
        _charactersForm.AddThemeConstantOverride("separation", 12);
        column.AddChild(_charactersForm);
        _listTitle = new Label();
        _listTitle.AddThemeFontSizeOverride("font_size", 22);
        _charactersForm.AddChild(_listTitle);
        _list = new VBoxContainer();
        _list.AddThemeConstantOverride("separation", 6);
        _charactersForm.AddChild(_list);
        // The new character: name and look, class, outfit colour.
        var create = new HBoxContainer();
        create.AddThemeConstantOverride("separation", 10);
        _charactersForm.AddChild(create);
        CharacterField = Field(create, 20);
        CharacterField.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
        CharacterField.TextSubmitted += text => _ = Create();
        LookChoice = new OptionButton { CustomMinimumSize = new Vector2(140, 44) };
        foreach (string look in Hero.Looks)
            LookChoice.AddItem(look);
        LookChoice.Select(0);
        LookChoice.ItemSelected += _ => ShowPreview();
        create.AddChild(LookChoice);
        var classRow = new HBoxContainer();
        classRow.AddThemeConstantOverride("separation", 10);
        _charactersForm.AddChild(classRow);
        ClassChoice = new OptionButton { CustomMinimumSize = new Vector2(150, 44) };
        foreach (HeroClass c in GameData.Embedded.Classes)
            ClassChoice.AddItem(c.Id);
        ClassChoice.Select(0);
        ClassChoice.ItemSelected += _ => Refresh();
        classRow.AddChild(ClassChoice);
        _classText = new Label { AutowrapMode = TextServer.AutowrapMode.WordSmart, SizeFlagsHorizontal = Control.SizeFlags.ExpandFill, VerticalAlignment = VerticalAlignment.Center };
        _classText.AddThemeFontSizeOverride("font_size", 14);
        classRow.AddChild(_classText);
        var colourRow = new HBoxContainer();
        colourRow.AddThemeConstantOverride("separation", 6);
        _charactersForm.AddChild(colourRow);
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
        colourRow.AddChild(new Control { SizeFlagsHorizontal = Control.SizeFlags.ExpandFill });
        _create = Button(colourRow, () => _ = Create());
        _signOut = Button(_charactersForm, () =>
        {
            _lobby.SignOut();
            Refresh();
        });

        _message = new Label { AutowrapMode = TextServer.AutowrapMode.WordSmart, CustomMinimumSize = new Vector2(0, 48) };
        _message.AddThemeColorOverride("font_color", new Color("ffb4a2"));
        column.AddChild(_message);
        Refresh();
        ShowPreview();
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
        PreviewShown?.Invoke(Look, Colour);
    }

    /// <summary>The sign-in (or sign-up) button.</summary>
    public async Task SignIn(bool create)
    {
        if (_busy)
            return;
        await Busy(() => _lobby.SignIn(NameField.Text.Trim(), PasswordField.Text, create));
        if (_lobby.SignedIn)
            PasswordField.Text = "";
    }

    /// <summary>The create button: a character with the name typed and the look shown.</summary>
    public async Task Create()
    {
        if (_busy)
            return;
        if (await Busy(() => _lobby.Create(CharacterField.Text.Trim(), Look, Class.Id, Colour)))
            CharacterField.Text = "";
    }

    private async Task<bool> Busy(Func<Task<bool>> call)
    {
        _busy = true;
        _message.Text = Texts["lobby.wait"];
        SetDisabled(true);
        bool done = await call();
        _busy = false;
        SetDisabled(false);
        Refresh();
        return done;
    }

    private void SetDisabled(bool disabled)
    {
        foreach (Button b in new[] { _signIn, _signUp, _offline, _create, _signOut })
            b.Disabled = disabled;
        foreach (Button b in _list.FindChildren("*", nameof(Button), true, false).OfType<Button>())
            b.Disabled = disabled;
    }

    /// <summary>Redraws the screen from the lobby's state and the language.</summary>
    public void Refresh()
    {
        _title.Text = Texts["lobby.title"];
        _lang.Text = Texts.Lang == "fr" ? "EN" : "FR";
        _server.Text = Texts["lobby.server", _serverUrl];
        NameField.PlaceholderText = Texts["lobby.name"];
        PasswordField.PlaceholderText = Texts["lobby.password"];
        CharacterField.PlaceholderText = Texts["lobby.character-name"];
        _signIn.Text = Texts["lobby.sign-in"];
        _signUp.Text = Texts["lobby.sign-up"];
        _offline.Text = Texts["lobby.offline"];
        _create.Text = Texts["lobby.create"];
        _create.Disabled = _busy || !_lobby.CanCreate;
        _signOut.Text = Texts["lobby.sign-out"];
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
        _message.Text = _lobby.Problem is string key ? Texts[key] : "";
        _signInForm.Visible = !_lobby.SignedIn;
        _charactersForm.Visible = _lobby.SignedIn;
        _listTitle.Text = Texts["lobby.characters", _lobby.Characters.Count, Accounts.MaxCharacters];
        foreach (Node row in _list.GetChildren())
        {
            _list.RemoveChild(row);
            row.QueueFree();
        }
        if (_lobby.Characters.Count == 0)
            _list.AddChild(new Label { Text = Texts["lobby.none"], Modulate = new Color(1, 1, 1, 0.7f) });
        foreach (CharacterSummary c in _lobby.Characters)
        {
            var row = new HBoxContainer();
            row.AddThemeConstantOverride("separation", 10);
            _list.AddChild(row);
            string className = GameData.Embedded.Class(c.Class)?.Name.In(Texts.Lang) ?? c.Class;
            var name = new Button { Text = $"{c.Name}  ·  {className}  ·  {Texts.Look(c.Look)}", Flat = true, SizeFlagsHorizontal = Control.SizeFlags.ExpandFill, Alignment = HorizontalAlignment.Left, CustomMinimumSize = new Vector2(0, 44) };
            name.MouseEntered += () => PreviewShown?.Invoke(c.Look, c.Colour);
            name.MouseExited += ShowPreview;
            row.AddChild(name);
            Button(row, () => Chosen?.Invoke(c)).Text = Texts["lobby.play"];
            Button(row, async () =>
            {
                if (!_busy)
                    await Busy(() => _lobby.Delete(c.Id));
            }).Text = Texts["lobby.delete"];
        }
    }

    private static LineEdit Field(Container parent, int maxLength)
    {
        var field = new LineEdit { MaxLength = maxLength, CustomMinimumSize = new Vector2(0, 44) };
        parent.AddChild(field);
        return field;
    }

    private static Button Button(Container parent, Action pressed)
    {
        var button = new Button { CustomMinimumSize = new Vector2(120, 44) };
        button.Pressed += pressed;
        parent.AddChild(button);
        return button;
    }
}
