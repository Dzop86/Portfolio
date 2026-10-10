using Godot;
using Rpg.Client;
using Rpg.Core;

namespace Rpg.Desktop;

/// <summary>
/// The character screen, over the town (sprint 64): the hero's portrait, name, level and class; on the
/// "Characteristics" tab, hit points, action and movement points, initiative and the points left, then
/// each characteristic in the colour of its element with the points put in, those of the equipment,
/// the total and what it gives, and its − and + buttons (+10 with Shift); on the "Spells" tab, the
/// spells the level has unlocked with their rank. It only draws <see cref="PointsEditor"/> and
/// <see cref="CharacterSheet"/>.
/// </summary>
public partial class PointsPanel : CanvasLayer
{
    private readonly List<(Characteristic Stat, Label[] Cells, Button Minus, Button Plus)> _stats = [];
    private readonly List<(Spell Spell, Label Rank, Button Minus, Button Plus)> _spells = [];
    private readonly Dictionary<string, Label> _tiles = [];
    private Label _name = null!, _level = null!, _spellsLeft = null!, _message = null!, _help = null!;
    private Button _save = null!, _close = null!, _statsTab = null!, _spellsTab = null!;
    private Control _statsPage = null!, _spellsPage = null!;
    private Portrait? _portrait;

    public PointsEditor Editor { get; private set; } = null!;
    public Texts Texts { get; private set; } = new("en");

    public event Action<Points>? SavePressed;
    public event Action? ClosePressed;

    public void Build(PointsEditor editor, Texts texts)
    {
        Editor = editor;
        Texts = texts;
        var panel = new PanelContainer { AnchorLeft = 0.5f, AnchorRight = 0.5f, AnchorTop = 0.5f, AnchorBottom = 0.5f, OffsetLeft = -500, OffsetRight = 500, OffsetTop = -270, OffsetBottom = 290 };
        panel.AddThemeStyleboxOverride("panel", InventoryPanel.Background());
        AddChild(panel);
        var margin = new MarginContainer();
        foreach (string side in new[] { "left", "right", "top", "bottom" })
            margin.AddThemeConstantOverride("margin_" + side, 16);
        panel.AddChild(margin);
        var column = new VBoxContainer();
        column.AddThemeConstantOverride("separation", 10);
        margin.AddChild(column);

        // The header: portrait, name, level and class, the two tabs.
        var header = new HBoxContainer();
        header.AddThemeConstantOverride("separation", 16);
        column.AddChild(header);
        _portrait = Portrait.Make(new Vector2(96, 112), turning: false, distance: 1.5f);
        header.AddChild(_portrait);
        _portrait.Show(editor.Hero);
        var who = new VBoxContainer { SizeFlagsHorizontal = Control.SizeFlags.ExpandFill, Alignment = BoxContainer.AlignmentMode.Center };
        header.AddChild(who);
        _name = new Label();
        _name.AddThemeFontSizeOverride("font_size", 26);
        who.AddChild(_name);
        _level = new Label { Modulate = new Color(1, 1, 1, 0.8f) };
        who.AddChild(_level);
        var tabs = new HBoxContainer { SizeFlagsVertical = Control.SizeFlags.ShrinkCenter };
        tabs.AddThemeConstantOverride("separation", 6);
        header.AddChild(tabs);
        _statsTab = Tab(tabs, () => ShowTab(spells: false));
        _spellsTab = Tab(tabs, () => ShowTab(spells: true));

        _statsPage = BuildStats();
        column.AddChild(_statsPage);
        _spellsPage = BuildSpells(editor);
        column.AddChild(_spellsPage);

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
        ShowTab(spells: false);
    }

    private HBoxContainer BuildStats()
    {
        var page = new HBoxContainer { SizeFlagsVertical = Control.SizeFlags.ExpandFill };
        page.AddThemeConstantOverride("separation", 18);
        // The tiles: hit points, action and movement points, initiative, points left.
        var tiles = new VBoxContainer { CustomMinimumSize = new Vector2(200, 0) };
        tiles.AddThemeConstantOverride("separation", 8);
        page.AddChild(tiles);
        foreach ((string key, string colour) in new[] { ("hp", ElementStyle.Heal), ("ap", "#6fb6ff"), ("mp", "#9be36b"), ("initiative", "#e0e0e0"), ("left", "#bef374") })
            _tiles[key] = Tile(tiles, colour, big: key is "hp" or "left");

        var right = new VBoxContainer { SizeFlagsHorizontal = Control.SizeFlags.ExpandFill };
        right.AddThemeConstantOverride("separation", 8);
        page.AddChild(right);
        var grid = new GridContainer { Columns = 8 };
        grid.AddThemeConstantOverride("h_separation", 14);
        grid.AddThemeConstantOverride("v_separation", 10);
        right.AddChild(grid);
        foreach (string col in new[] { "stat", "invested", "worn", "total", "bonus", "resist" })
        {
            var h = new Label { Text = "", Modulate = new Color(1, 1, 1, 0.65f), HorizontalAlignment = col == "stat" ? HorizontalAlignment.Left : HorizontalAlignment.Right };
            h.AddThemeFontSizeOverride("font_size", 13);
            h.SetMeta("key", "sheet.col." + col);
            grid.AddChild(h);
        }
        grid.AddChild(new Control());
        grid.AddChild(new Control());
        foreach (Characteristic stat in Enum.GetValues<Characteristic>())
        {
            Color colour = new(Colour(stat));
            var name = new Label { CustomMinimumSize = new Vector2(150, 0), MouseFilter = Control.MouseFilterEnum.Stop };
            name.AddThemeColorOverride("font_color", colour);
            name.AddThemeFontSizeOverride("font_size", 18);
            grid.AddChild(name);
            var cells = new Label[6];
            cells[0] = name;
            for (int i = 1; i < 6; i++)
            {
                cells[i] = new Label { HorizontalAlignment = HorizontalAlignment.Right, CustomMinimumSize = new Vector2(i == 4 ? 210 : 70, 0) };
                if (i == 3)
                {
                    cells[i].AddThemeFontSizeOverride("font_size", 18);
                    cells[i].AddThemeColorOverride("font_color", colour);
                }
                grid.AddChild(cells[i]);
            }
            Button minus = Small(grid, "−", () => Editor.Remove(stat, Input.IsKeyPressed(Key.Shift) ? 10 : 1));
            Button plus = Small(grid, "+", () => Editor.Add(stat, Input.IsKeyPressed(Key.Shift) ? 10 : 1));
            _stats.Add((stat, cells, minus, plus));
        }
        _help = new Label { AutowrapMode = TextServer.AutowrapMode.WordSmart, Modulate = new Color(1, 1, 1, 0.65f) };
        _help.AddThemeFontSizeOverride("font_size", 13);
        right.AddChild(_help);
        return page;
    }

    private VBoxContainer BuildSpells(PointsEditor editor)
    {
        var page = new VBoxContainer { SizeFlagsVertical = Control.SizeFlags.ExpandFill };
        page.AddThemeConstantOverride("separation", 6);
        _spellsLeft = new Label();
        page.AddChild(_spellsLeft);
        var scroll = new ScrollContainer { SizeFlagsVertical = Control.SizeFlags.ExpandFill, HorizontalScrollMode = ScrollContainer.ScrollMode.Disabled };
        page.AddChild(scroll);
        var list = new VBoxContainer { SizeFlagsHorizontal = Control.SizeFlags.ExpandFill };
        scroll.AddChild(list);
        foreach (Spell spell in editor.Spells)
        {
            var row = new HBoxContainer();
            list.AddChild(row);
            var name = new Label { CustomMinimumSize = new Vector2(220, 0), ClipText = true, MouseFilter = Control.MouseFilterEnum.Stop, Text = spell.Name.In(Texts.Lang) };
            name.AddThemeColorOverride("font_color", new Color(ElementStyle.Colour(spell.Element)));
            row.AddChild(name);
            var rank = new Label { SizeFlagsHorizontal = Control.SizeFlags.ExpandFill };
            rank.AddThemeFontSizeOverride("font_size", 13);
            row.AddChild(rank);
            Button minus = Small(row, "−", () => Editor.Lower(spell));
            Button plus = Small(row, "+", () => Editor.Raise(spell));
            _spells.Add((spell, rank, minus, plus));
        }
        return page;
    }

    /// <summary>The colour of a characteristic: its element's, the heal colour for Vitality.</summary>
    private static string Colour(Characteristic stat) => stat switch
    {
        Characteristic.Strength => ElementStyle.Colour(Element.Earth),
        Characteristic.Intelligence => ElementStyle.Colour(Element.Fire),
        Characteristic.Chance => ElementStyle.Colour(Element.Water),
        Characteristic.Agility => ElementStyle.Colour(Element.Air),
        _ => ElementStyle.Heal,
    };

    private static Label Tile(VBoxContainer parent, string colour, bool big)
    {
        var box = new PanelContainer { CustomMinimumSize = new Vector2(200, big ? 78 : 54) };
        box.AddThemeStyleboxOverride("panel", new StyleBoxFlat
        {
            BgColor = new Color("2a2a2a"),
            BorderColor = new Color(colour),
            BorderWidthLeft = 4,
            CornerRadiusTopLeft = 6,
            CornerRadiusTopRight = 6,
            CornerRadiusBottomLeft = 6,
            CornerRadiusBottomRight = 6,
            ContentMarginLeft = 12,
            ContentMarginRight = 12,
            ContentMarginTop = 6,
            ContentMarginBottom = 6,
        });
        parent.AddChild(box);
        var label = new Label { VerticalAlignment = VerticalAlignment.Center };
        label.AddThemeFontSizeOverride("font_size", big ? 20 : 17);
        box.AddChild(label);
        return label;
    }

    private static Button Tab(HBoxContainer parent, Action act)
    {
        var b = new Button { ToggleMode = true, CustomMinimumSize = new Vector2(170, 44) };
        b.Pressed += act;
        parent.AddChild(b);
        return b;
    }

    public void ShowTab(bool spells)
    {
        _statsPage.Visible = !spells;
        _spellsPage.Visible = spells;
        _statsTab.SetPressedNoSignal(!spells);
        _spellsTab.SetPressedNoSignal(spells);
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
        var sheet = new CharacterSheet(Editor.Draft, Editor.Data);
        string cls = Editor.Data.Class(Editor.Hero.Class)?.Name.In(Texts.Lang) ?? "";
        _name.Text = Editor.Hero.Name;
        _level.Text = Texts["sheet.level", Editor.Hero.Level, cls];
        _statsTab.Text = Texts["sheet.tab.stats"];
        _spellsTab.Text = Texts["sheet.tab.spells"];
        _tiles["hp"].Text = $"{Texts["sheet.hp"]}   {sheet.MaxHp}";
        _tiles["ap"].Text = $"{Texts["sheet.ap"]}   {sheet.Ap}";
        _tiles["mp"].Text = $"{Texts["sheet.mp"]}   {sheet.Mp}";
        _tiles["initiative"].Text = $"{Texts["sheet.initiative"]}   {sheet.Initiative}";
        _tiles["left"].Text = $"{Texts["sheet.left"]}   {sheet.PointsLeft}";
        foreach (Label h in _statsPage.FindChildren("*", "Label", true, false).OfType<Label>().Where(l => l.HasMeta("key")))
            h.Text = Texts[(string)h.GetMeta("key")];
        foreach ((Characteristic stat, Label[] cells, Button minus, Button plus) in _stats)
        {
            SheetLine line = sheet[stat];
            cells[0].Text = Texts[$"char.{stat}"];
            cells[0].TooltipText = Texts[$"char.{stat}.help"];
            cells[1].Text = Num(line.Invested);
            cells[2].Text = line.Worn == 0 ? "—" : "+" + Num(line.Worn);
            cells[3].Text = Num(line.Total);
            // Below the first 10 points, nothing yet: a dash rather than "+0".
            cells[4].Text = line.Bonus == 0 ? "—" : line.Element is Element e
                ? Texts["sheet.bonus.damage", line.Bonus, Texts["element." + e]] + (stat == Characteristic.Intelligence ? Texts["sheet.bonus.heal", line.Bonus] : "")
                : Texts["sheet.bonus.hp", line.Bonus];
            cells[5].Text = line.Element is null ? "—" : $"{line.Resistance} %";
            minus.Disabled = !Editor.CanRemove(stat);
            plus.Disabled = !Editor.CanAdd(stat);
        }
        _help.Text = Texts["sheet.help"];
        _spellsLeft.Text = Texts["points.spells", Editor.SpellPointsLeft];
        foreach ((Spell spell, Label rank, Button minus, Button plus) in _spells)
        {
            int next = Editor.NextRankCost(spell);
            rank.Text = $"{Texts["points.rank", Editor.Rank(spell), spell.MaxRank]} · {(next > 0 ? Texts["points.next", next] : Texts["points.max"])}";
            rank.GetParent<HBoxContainer>().GetChild<Label>(0).TooltipText = Texts.SpellCard(spell.AtRank(Editor.Rank(spell)), sheet.Hero.Stats is null ? null : Fight.HeroTotals(sheet.Hero, Editor.Data)?.Stats);
            minus.Disabled = !Editor.CanLower(spell);
            plus.Disabled = !Editor.CanRaise(spell);
        }
        _save.Text = Texts["points.save"];
        _save.Disabled = !Editor.Changed;
        _close.Text = Texts["points.close"];
    }

    private static string Num(int n) => n.ToString(System.Globalization.CultureInfo.InvariantCulture);

    public void ShowMessage(string text) => _message.Text = text;

    /// <summary>The totals shown, as the self-test reads them.</summary>
    public IEnumerable<string> StatsText => _stats.Select(s => s.Cells[1].Text);

    public string MessageText => _message.Text;

    private Button Small(Container parent, string text, Action act)
    {
        var b = new Button { Text = text, CustomMinimumSize = new Vector2(44, 44) };
        b.Pressed += () =>
        {
            act();
            Refresh();
        };
        parent.AddChild(b);
        return b;
    }
}
