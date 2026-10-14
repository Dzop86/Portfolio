using Godot;
using Rpg.Client;

namespace Rpg.Desktop;

/// <summary>
/// The proximity chat on screen (sprint 84, D65), bottom left over the town: the last lines, then a
/// field. Enter opens the field, Enter sends and closes it; Escape, or a click on the map, leaves it
/// without sending. While the field is open, the game's keys (windows, menu) do not answer.
/// </summary>
public partial class ChatBox : CanvasLayer
{
    private RichTextLabel _log = null!;
    private LineEdit _field = null!;
    private ChatLog _lines = null!;
    private Texts _texts = null!;

    /// <summary>A line to send, cleaned by the rules first.</summary>
    public event Action<string>? Said;

    public void Build(ChatLog lines, Texts texts)
    {
        _lines = lines;
        _texts = texts;
        // Under the windows (inventory, spells...), which are in front (D67).
        Layer = 0;
        var box = new VBoxContainer { AnchorTop = 1, AnchorBottom = 1, OffsetLeft = 12, OffsetRight = 452, OffsetTop = -232, OffsetBottom = -60, MouseFilter = Control.MouseFilterEnum.Ignore };
        box.AddThemeConstantOverride("separation", 4);
        AddChild(box);
        _log = new RichTextLabel { BbcodeEnabled = false, ScrollFollowing = true, SizeFlagsVertical = Control.SizeFlags.ExpandFill, MouseFilter = Control.MouseFilterEnum.Ignore };
        _log.AddThemeFontSizeOverride("normal_font_size", 14);
        _log.AddThemeConstantOverride("outline_size", 4);
        _log.AddThemeColorOverride("font_outline_color", new Color(0, 0, 0, 0.8f));
        box.AddChild(_log);
        _field = new LineEdit { CustomMinimumSize = new Vector2(0, 36), MaxLength = Rpg.Core.ChatRules.MaxLength, PlaceholderText = texts["chat.placeholder"], Modulate = new Color(1, 1, 1, 0.55f) };
        _field.TextSubmitted += Submit;
        _field.FocusEntered += () => _field.Modulate = Colors.White;
        _field.FocusExited += () => _field.Modulate = new Color(1, 1, 1, 0.55f);
        _field.GuiInput += e =>
        {
            if (e is InputEventKey { Pressed: true, Keycode: Key.Escape })
            {
                Leave();
                _field.AcceptEvent();
            }
        };
        box.AddChild(_field);
        _field.Visible = false;
        _log.Visible = false;
        Refresh();
    }

    /// <summary>How long new lines stay on screen before the chat hides again (D67: "invisible unless something scrolls").</summary>
    public const double ShownFor = 8;

    private double _shown;

    public override void _Process(double delta)
    {
        if (Typing)
            return;
        _shown -= delta;
        _log.Visible = _shown > 0;
        _log.Modulate = new Color(1, 1, 1, (float)Math.Clamp(_shown, 0, 1));
    }

    /// <summary>Whether the lines are on screen (for the self-test).</summary>
    public bool LinesShown => _log.Visible;

    public bool Typing => _field.HasFocus();

    /// <summary>Enter, from the town: the field opens.</summary>
    public void Open()
    {
        _field.Visible = true;
        _log.Visible = true;
        _log.Modulate = Colors.White;
        _field.GrabFocus();
    }

    /// <summary>Escape, or a click elsewhere: the field closes and hides, what was typed stays for later.</summary>
    public void Leave()
    {
        _field.ReleaseFocus();
        _field.Visible = false;
        _shown = Math.Max(_shown, 2);
    }

    /// <summary>As the player would type a line and press Enter (for the self-test).</summary>
    public void Type(string text)
    {
        Open();
        _field.Text = text;
        Submit(text);
    }

    /// <summary>The lines again; new ones show the chat for a few seconds.</summary>
    public void Refresh(bool fresh = false)
    {
        _log.Text = string.Join('\n', _lines.Lines);
        if (fresh)
            _shown = ShownFor;
    }

    /// <summary>The lines shown (for the self-test).</summary>
    public string LogText => _log.Text;

    private void Submit(string text)
    {
        if (Rpg.Core.ChatRules.Clean(text) is string clean)
            Said?.Invoke(clean);
        _field.Text = "";
        Leave();
    }
}
