using Godot;
using Rpg.Client;
using Rpg.Core;

namespace Rpg.Desktop;

/// <summary>
/// The characteristics and spells screen, over the town: each characteristic with its value and its
/// − and + buttons (+10 with Shift), the spells the level has unlocked with their rank and what the
/// next one costs, the points left, "Save" and "Close". It only draws <see cref="PointsEditor"/>.
/// </summary>
public partial class PointsPanel : CanvasLayer
{
    private readonly List<(Characteristic Stat, Label Value, Button Minus, Button Plus)> _stats = [];
    private readonly List<(Spell Spell, Label Rank, Button Minus, Button Plus)> _spells = [];
    private Label _title = null!, _statsLeft = null!, _spellsLeft = null!, _message = null!;
    private Button _save = null!, _close = null!;

    public PointsEditor Editor { get; private set; } = null!;
    public Texts Texts { get; private set; } = new("en");

    public event Action<Points>? SavePressed;
    public event Action? ClosePressed;

    public void Build(PointsEditor editor, Texts texts)
    {
        Editor = editor;
        Texts = texts;
        var panel = new PanelContainer { AnchorLeft = 0.5f, AnchorRight = 0.5f, AnchorTop = 0.5f, AnchorBottom = 0.5f, OffsetLeft = -430, OffsetRight = 430, OffsetTop = -300, OffsetBottom = 300 };
        AddChild(panel);
        var margin = new MarginContainer();
        foreach (string side in new[] { "left", "right", "top", "bottom" })
            margin.AddThemeConstantOverride("margin_" + side, 18);
        panel.AddChild(margin);
        var column = new VBoxContainer();
        column.AddThemeConstantOverride("separation", 8);
        margin.AddChild(column);
        _title = new Label();
        _title.AddThemeFontSizeOverride("font_size", 24);
        column.AddChild(_title);
        var halves = new HBoxContainer { SizeFlagsVertical = Control.SizeFlags.ExpandFill };
        halves.AddThemeConstantOverride("separation", 24);
        column.AddChild(halves);

        var left = new VBoxContainer { CustomMinimumSize = new Vector2(330, 0) };
        left.AddThemeConstantOverride("separation", 6);
        halves.AddChild(left);
        _statsLeft = new Label();
        left.AddChild(_statsLeft);
        foreach (Characteristic stat in Enum.GetValues<Characteristic>())
        {
            var row = new HBoxContainer();
            left.AddChild(row);
            var name = new Label { CustomMinimumSize = new Vector2(130, 0), MouseFilter = Control.MouseFilterEnum.Stop, TooltipText = Texts[$"char.{stat}.help"], Text = Texts[$"char.{stat}"] };
            row.AddChild(name);
            var value = new Label { CustomMinimumSize = new Vector2(60, 0), HorizontalAlignment = HorizontalAlignment.Right };
            row.AddChild(value);
            Button minus = Small(row, "−", () => Editor.Remove(stat, Input.IsKeyPressed(Key.Shift) ? 10 : 1));
            Button plus = Small(row, "+", () => Editor.Add(stat, Input.IsKeyPressed(Key.Shift) ? 10 : 1));
            _stats.Add((stat, value, minus, plus));
        }

        var right = new VBoxContainer { SizeFlagsHorizontal = Control.SizeFlags.ExpandFill };
        right.AddThemeConstantOverride("separation", 6);
        halves.AddChild(right);
        _spellsLeft = new Label();
        right.AddChild(_spellsLeft);
        var scroll = new ScrollContainer { SizeFlagsVertical = Control.SizeFlags.ExpandFill, HorizontalScrollMode = ScrollContainer.ScrollMode.Disabled };
        right.AddChild(scroll);
        var list = new VBoxContainer { SizeFlagsHorizontal = Control.SizeFlags.ExpandFill };
        scroll.AddChild(list);
        foreach (Spell spell in editor.Spells)
        {
            var row = new HBoxContainer();
            list.AddChild(row);
            var name = new Label { CustomMinimumSize = new Vector2(170, 0), ClipText = true, MouseFilter = Control.MouseFilterEnum.Stop, TooltipText = Texts.SpellCard(spell), Text = spell.Name.In(Texts.Lang) };
            row.AddChild(name);
            var rank = new Label { SizeFlagsHorizontal = Control.SizeFlags.ExpandFill };
            rank.AddThemeFontSizeOverride("font_size", 13);
            row.AddChild(rank);
            Button minus = Small(row, "−", () => Editor.Lower(spell));
            Button plus = Small(row, "+", () => Editor.Raise(spell));
            _spells.Add((spell, rank, minus, plus));
        }

        _message = new Label();
        column.AddChild(_message);
        var buttons = new HBoxContainer { Alignment = BoxContainer.AlignmentMode.End };
        buttons.AddThemeConstantOverride("separation", 10);
        column.AddChild(buttons);
        _save = new Button { CustomMinimumSize = new Vector2(150, 44) };
        _save.Pressed += () => SavePressed?.Invoke(Editor.ToPoints());
        buttons.AddChild(_save);
        _close = new Button { CustomMinimumSize = new Vector2(150, 44) };
        _close.Pressed += () => ClosePressed?.Invoke();
        buttons.AddChild(_close);
        Refresh();
    }

    /// <summary>After a save: a new draft from what the server kept, and a message.</summary>
    public void Saved(Hero hero, string message)
    {
        Editor = new PointsEditor(hero, Editor.Data);
        _message.Text = message;
        Refresh();
    }

    public void Refresh()
    {
        _title.Text = Texts["points.title"];
        _statsLeft.Text = Texts["points.characteristics", Editor.CharacteristicPointsLeft];
        _spellsLeft.Text = Texts["points.spells", Editor.SpellPointsLeft];
        foreach ((Characteristic stat, Label value, Button minus, Button plus) in _stats)
        {
            value.Text = Editor[stat].ToString(System.Globalization.CultureInfo.InvariantCulture);
            minus.Disabled = !Editor.CanRemove(stat);
            plus.Disabled = !Editor.CanAdd(stat);
        }
        foreach ((Spell spell, Label rank, Button minus, Button plus) in _spells)
        {
            int next = Editor.NextRankCost(spell);
            rank.Text = $"{Texts["points.rank", Editor.Rank(spell), spell.MaxRank]} · {(next > 0 ? Texts["points.next", next] : Texts["points.max"])}";
            minus.Disabled = !Editor.CanLower(spell);
            plus.Disabled = !Editor.CanRaise(spell);
        }
        _save.Text = Texts["points.save"];
        _save.Disabled = !Editor.Changed;
        _close.Text = Texts["points.close"];
    }

    public void ShowMessage(string text) => _message.Text = text;

    /// <summary>The values shown, as the self-test reads them.</summary>
    public IEnumerable<string> StatsText => _stats.Select(s => s.Value.Text);

    public string MessageText => _message.Text;

    private Button Small(HBoxContainer row, string text, Action act)
    {
        var b = new Button { Text = text, CustomMinimumSize = new Vector2(44, 44) };
        b.Pressed += () =>
        {
            act();
            Refresh();
        };
        row.AddChild(b);
        return b;
    }
}
