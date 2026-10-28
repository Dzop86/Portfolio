using Godot;
using Rpg.Client;

namespace Rpg.Desktop;

/// <summary>
/// The Escape menu (sprint 69), over everything: the sound (general volume, music, effects, a cut),
/// applied as the sliders move and kept in the settings file; then back to the game, another server,
/// another character (signed in only), quit.
/// </summary>
public partial class MenuPanel : CanvasLayer
{
    private readonly Dictionary<string, (Label Name, HSlider Slider, Label Value)> _sliders = [];
    private readonly Dictionary<MenuChoice, Button> _choices = [];
    private Label _title = null!, _sound = null!;
    private CheckBox _mute = null!;

    public Texts Texts { get; private set; } = new("en");

    public event Action<MenuChoice>? Chosen;

    public void Build(Texts texts, bool signedIn)
    {
        Texts = texts;
        Layer = 20;
        // A veil over the game: clicks do not go through.
        var veil = new ColorRect { Color = new Color(0, 0, 0, 0.45f), MouseFilter = Control.MouseFilterEnum.Stop };
        veil.SetAnchorsPreset(Control.LayoutPreset.FullRect);
        AddChild(veil);
        var panel = new PanelContainer { AnchorLeft = 0.5f, AnchorRight = 0.5f, AnchorTop = 0.5f, AnchorBottom = 0.5f, OffsetLeft = -230, OffsetRight = 230, OffsetTop = 0, OffsetBottom = 0, GrowVertical = Control.GrowDirection.Both };
        panel.AddThemeStyleboxOverride("panel", InventoryPanel.Background());
        AddChild(panel);
        var margin = new MarginContainer();
        foreach (string side in new[] { "left", "right", "top", "bottom" })
            margin.AddThemeConstantOverride("margin_" + side, 20);
        panel.AddChild(margin);
        var column = new VBoxContainer();
        column.AddThemeConstantOverride("separation", 10);
        margin.AddChild(column);
        _title = new Label { HorizontalAlignment = HorizontalAlignment.Center };
        _title.AddThemeFontSizeOverride("font_size", 26);
        column.AddChild(_title);
        _sound = new Label { Modulate = new Color(1, 1, 1, 0.7f) };
        column.AddChild(_sound);

        AudioSettings s = Sound.Settings;
        var grid = new GridContainer { Columns = 3 };
        grid.AddThemeConstantOverride("h_separation", 12);
        column.AddChild(grid);
        foreach ((string key, int value) in new[] { ("master", s.Master), ("music", s.Music), ("effects", s.Effects) })
        {
            var name = new Label { CustomMinimumSize = new Vector2(150, 0) };
            grid.AddChild(name);
            var slider = new HSlider { MinValue = 0, MaxValue = 100, Step = 5, Value = value, CustomMinimumSize = new Vector2(180, 44), SizeFlagsHorizontal = Control.SizeFlags.ExpandFill };
            grid.AddChild(slider);
            var shown = new Label { CustomMinimumSize = new Vector2(40, 0), HorizontalAlignment = HorizontalAlignment.Right, Text = value.ToString(System.Globalization.CultureInfo.InvariantCulture) };
            grid.AddChild(shown);
            slider.ValueChanged += v =>
            {
                shown.Text = ((int)v).ToString(System.Globalization.CultureInfo.InvariantCulture);
                Sound.Apply(Current);
            };
            _sliders[key] = (name, slider, shown);
        }
        _mute = new CheckBox { ButtonPressed = s.Muted, CustomMinimumSize = new Vector2(0, 44) };
        _mute.Toggled += _ => Sound.Apply(Current);
        column.AddChild(_mute);
        column.AddChild(new HSeparator());

        foreach (MenuChoice choice in Enum.GetValues<MenuChoice>())
        {
            // Back to the game is the one to press (sprint 157: the theme's primary button), the others secondary.
            var b = new Button { CustomMinimumSize = new Vector2(0, 44), Visible = signedIn || choice is MenuChoice.Resume or MenuChoice.Quit, ThemeTypeVariation = choice == MenuChoice.Resume ? OsmoseTheme.Primary : "" };
            b.Pressed += () => Chosen?.Invoke(choice);
            column.AddChild(b);
            _choices[choice] = b;
        }
        Refresh();
    }

    /// <summary>The settings as the menu shows them.</summary>
    public AudioSettings Current => new(
        (int)_sliders["master"].Slider.Value,
        (int)_sliders["music"].Slider.Value,
        (int)_sliders["effects"].Slider.Value,
        _mute.ButtonPressed);

    /// <summary>Moves a slider as the player would (for the self-test).</summary>
    public void SetVolume(string key, int value) => _sliders[key].Slider.Value = value;

    public void SetMuted(bool muted) => _mute.ButtonPressed = muted;

    /// <summary>The texts of the choices shown, top to bottom (for the self-test).</summary>
    public IEnumerable<string> ChoicesText => _choices.Values.Where(b => b.Visible).Select(b => b.Text);

    public void SetTexts(Texts texts)
    {
        Texts = texts;
        Refresh();
    }

    private void Refresh()
    {
        _title.Text = Texts["menu.title"];
        _sound.Text = Texts["menu.sound"];
        foreach ((string key, (Label name, _, _)) in _sliders)
            name.Text = Texts["menu." + key];
        _mute.Text = Texts["menu.mute"];
        foreach ((MenuChoice choice, Button b) in _choices)
            b.Text = Texts["menu." + choice];
    }
}
