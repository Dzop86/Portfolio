using Godot;

namespace Rpg.Desktop;

/// <summary>
/// A question from another player (sprint 119): an invitation to a party, a trade offered. At the top of the screen,
/// its words and two buttons; a new question takes the place of the one before.
/// </summary>
public partial class AskPanel : CanvasLayer
{
    private Label _text = null!;
    private Button _yes = null!, _no = null!;
    private Action? _onYes;

    public void Build()
    {
        Layer = 18;
        var panel = new PanelContainer { AnchorLeft = 0.5f, AnchorRight = 0.5f, OffsetLeft = -230, OffsetRight = 230, OffsetTop = 16 };
        panel.AddThemeStyleboxOverride("panel", InventoryPanel.Background());
        AddChild(panel);
        var margin = new MarginContainer();
        foreach (string side in new[] { "left", "right", "top", "bottom" })
            margin.AddThemeConstantOverride("margin_" + side, 12);
        panel.AddChild(margin);
        var column = new VBoxContainer();
        column.AddThemeConstantOverride("separation", 8);
        margin.AddChild(column);
        _text = new Label { AutowrapMode = TextServer.AutowrapMode.WordSmart, HorizontalAlignment = HorizontalAlignment.Center };
        column.AddChild(_text);
        var buttons = new HBoxContainer { Alignment = BoxContainer.AlignmentMode.Center };
        buttons.AddThemeConstantOverride("separation", 12);
        column.AddChild(buttons);
        _yes = new Button { CustomMinimumSize = new Vector2(150, 44) };
        _no = new Button { CustomMinimumSize = new Vector2(150, 44) };
        _yes.Pressed += () =>
        {
            Visible = false;
            _onYes?.Invoke();
        };
        _no.Pressed += () => Visible = false;
        buttons.AddChild(_yes);
        buttons.AddChild(_no);
        Visible = false;
    }

    /// <summary>Asks: the words, the two buttons' words, what "yes" does.</summary>
    public void Ask(string text, string yes, string no, Action onYes)
    {
        (_text.Text, _yes.Text, _no.Text, _onYes) = (text, yes, no, onYes);
        Visible = true;
    }

    /// <summary>Takes the question away, unanswered.</summary>
    public new void Hide() => Visible = false;

    /// <summary>The question asked now, or "" (for the self-test).</summary>
    public string Question => Visible ? _text.Text : "";

    /// <summary>Answers, as a click on a button would (for the self-test).</summary>
    public void Answer(bool yes) => (yes ? _yes : _no).EmitSignal(BaseButton.SignalName.Pressed);
}
