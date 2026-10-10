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
    private readonly List<(Characteristic Stat, Label[] Cells, LineEdit Value, Button[] Buttons)> _stats = [];
    private Button _reset = null!;
    private readonly List<(Spell Spell, Button Tile, Label Name, Label Sub)> _tiles2 = [];
    private readonly List<(Element? Element, Button Chip)> _chips = [];
    private Element? _filter;
    private string? _selected, _hovered;
    private Label _detailName = null!, _detailRank = null!;
    private SpellIcon _detailIcon = null!;
    private RichTextLabel _detail = null!;
    private Button _rankMinus = null!, _rankPlus = null!;
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
        var panel = new PanelContainer { AnchorLeft = 0.5f, AnchorRight = 0.5f, AnchorTop = 0.5f, AnchorBottom = 0.5f, OffsetLeft = -570, OffsetRight = 570, OffsetTop = -300, OffsetBottom = 320 };
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
        // Every point back, to spend again (D62).
        _reset = Small(tiles, "", () => Editor.ResetCharacteristics(), 200);

        var right = new VBoxContainer { SizeFlagsHorizontal = Control.SizeFlags.ExpandFill };
        right.AddThemeConstantOverride("separation", 8);
        page.AddChild(right);
        var grid = new GridContainer { Columns = 10 };
        grid.AddThemeConstantOverride("h_separation", 8);
        grid.AddThemeConstantOverride("v_separation", 10);
        right.AddChild(grid);
        foreach (string col in new[] { "stat", "invested", "worn", "total", "bonus", "resist" })
        {
            var h = new Label { Text = "", Modulate = new Color(1, 1, 1, 0.65f), HorizontalAlignment = col == "stat" ? HorizontalAlignment.Left : HorizontalAlignment.Right };
            h.AddThemeFontSizeOverride("font_size", 13);
            h.SetMeta("key", "sheet.col." + col);
            grid.AddChild(h);
        }
        for (int k = 0; k < 4; k++)
            grid.AddChild(new Control());
        foreach (Characteristic stat in Enum.GetValues<Characteristic>())
        {
            Color colour = new(Colour(stat));
            var name = new Label { CustomMinimumSize = new Vector2(110, 0), MouseFilter = Control.MouseFilterEnum.Stop };
            name.AddThemeColorOverride("font_color", colour);
            name.AddThemeFontSizeOverride("font_size", 18);
            grid.AddChild(name);
            // The points put in, typed or changed by the buttons; a typed value is kept within what is left.
            var value = new LineEdit { CustomMinimumSize = new Vector2(70, 40), Alignment = HorizontalAlignment.Right };
            void Apply(string text)
            {
                if (int.TryParse(text, System.Globalization.NumberStyles.Integer, System.Globalization.CultureInfo.InvariantCulture, out int n))
                    Editor.Set(stat, n);
                Refresh();
            }
            value.TextSubmitted += Apply;
            value.FocusExited += () => Apply(value.Text);
            grid.AddChild(value);
            var cells = new Label[5];
            cells[0] = name;
            for (int i = 1; i < 5; i++)
            {
                cells[i] = new Label { HorizontalAlignment = HorizontalAlignment.Right, CustomMinimumSize = new Vector2(i == 3 ? 150 : 56, 0) };
                if (i == 2)
                {
                    cells[i].AddThemeFontSizeOverride("font_size", 18);
                    cells[i].AddThemeColorOverride("font_color", colour);
                }
                grid.AddChild(cells[i]);
            }
            Button min = Small(grid, "", () => Editor.Set(stat, 0), 56);
            Button minus = Small(grid, "−", () => Editor.Remove(stat, Input.IsKeyPressed(Key.Shift) ? 10 : 1));
            Button plus = Small(grid, "+", () => Editor.Add(stat, Input.IsKeyPressed(Key.Shift) ? 10 : 1));
            Button max = Small(grid, "", () => Editor.Set(stat, Editor.Max(stat)), 56);
            _stats.Add((stat, cells, value, [min, minus, plus, max]));
        }
        _help = new Label { AutowrapMode = TextServer.AutowrapMode.WordSmart, Modulate = new Color(1, 1, 1, 0.65f) };
        _help.AddThemeFontSizeOverride("font_size", 13);
        right.AddChild(_help);
        return page;
    }

    private VBoxContainer BuildSpells(PointsEditor editor)
    {
        var page = new VBoxContainer { SizeFlagsVertical = Control.SizeFlags.ExpandFill };
        page.AddThemeConstantOverride("separation", 8);
        var top = new HBoxContainer();
        top.AddThemeConstantOverride("separation", 6);
        page.AddChild(top);
        HeroClass? c = editor.Data.Class(editor.Hero.Class);
        Element[] present = c is null ? [] : [.. c.Spells.Select(id => editor.Data.Spells[id].Element).Distinct().Order()];
        foreach (Element? e in new Element?[] { null }.Concat(present.Select(x => (Element?)x)))
        {
            var chip = new Button { ToggleMode = true, CustomMinimumSize = new Vector2(84, 40) };
            if (e is Element el)
                chip.AddThemeColorOverride("font_color", new Color(ElementStyle.Colour(el)));
            chip.Pressed += () =>
            {
                _filter = e;
                Refresh();
            };
            top.AddChild(chip);
            _chips.Add((e, chip));
        }
        _spellsLeft = new Label { SizeFlagsHorizontal = Control.SizeFlags.ExpandFill, HorizontalAlignment = HorizontalAlignment.Right };
        _spellsLeft.AddThemeFontSizeOverride("font_size", 17);
        top.AddChild(_spellsLeft);

        var body = new HBoxContainer { SizeFlagsVertical = Control.SizeFlags.ExpandFill };
        body.AddThemeConstantOverride("separation", 14);
        page.AddChild(body);
        var scroll = new ScrollContainer { SizeFlagsHorizontal = Control.SizeFlags.ExpandFill, HorizontalScrollMode = ScrollContainer.ScrollMode.Disabled };
        body.AddChild(scroll);
        var grid = new GridContainer { Columns = 3 };
        grid.AddThemeConstantOverride("h_separation", 6);
        grid.AddThemeConstantOverride("v_separation", 6);
        scroll.AddChild(grid);
        foreach (Spell spell in c?.Spells.Select(id => editor.Data.Spells[id]) ?? [])
        {
            var tile = new Button { CustomMinimumSize = new Vector2(208, 56), ClipText = true };
            var colour = new Color(ElementStyle.Colour(spell.Element));
            foreach ((string state, float mix) in new[] { ("normal", 0.78f), ("hover", 0.62f), ("pressed", 0.55f), ("disabled", 0.9f), ("focus", 0.62f) })
            {
                tile.AddThemeStyleboxOverride(state, new StyleBoxFlat
                {
                    BgColor = colour.Lerp(new Color("1c1c1c"), mix),
                    BorderColor = colour,
                    BorderWidthLeft = 5,
                    CornerRadiusTopLeft = 6,
                    CornerRadiusTopRight = 6,
                    CornerRadiusBottomLeft = 6,
                    CornerRadiusBottomRight = 6,
                });
            }
            tile.AddChild(new SpellIcon { Spell = spell, Position = new Vector2(10, 6), Size = new Vector2(44, 44), MouseFilter = Control.MouseFilterEnum.Ignore });
            var name = new Label { Position = new Vector2(62, 6), MouseFilter = Control.MouseFilterEnum.Ignore, Text = spell.Name.In(Texts.Lang) };
            name.AddThemeFontSizeOverride("font_size", 16);
            tile.AddChild(name);
            var sub = new Label { Position = new Vector2(62, 30), MouseFilter = Control.MouseFilterEnum.Ignore, Modulate = new Color(1, 1, 1, 0.75f) };
            sub.AddThemeFontSizeOverride("font_size", 12);
            tile.AddChild(sub);
            string id = spell.Id;
            tile.MouseEntered += () =>
            {
                _hovered = id;
                ShowDetail();
            };
            tile.MouseExited += () =>
            {
                _hovered = null;
                ShowDetail();
            };
            tile.Pressed += () =>
            {
                _selected = id;
                ShowDetail();
            };
            grid.AddChild(tile);
            _tiles2.Add((spell, tile, name, sub));
        }

        var card = new PanelContainer { CustomMinimumSize = new Vector2(330, 0) };
        card.AddThemeStyleboxOverride("panel", new StyleBoxFlat { BgColor = new Color("262626"), CornerRadiusTopLeft = 8, CornerRadiusTopRight = 8, CornerRadiusBottomLeft = 8, CornerRadiusBottomRight = 8, ContentMarginLeft = 14, ContentMarginRight = 14, ContentMarginTop = 10, ContentMarginBottom = 10 });
        body.AddChild(card);
        var inside = new VBoxContainer();
        inside.AddThemeConstantOverride("separation", 6);
        card.AddChild(inside);
        var head = new HBoxContainer();
        head.AddThemeConstantOverride("separation", 12);
        inside.AddChild(head);
        _detailIcon = new SpellIcon { CustomMinimumSize = new Vector2(56, 56) };
        head.AddChild(_detailIcon);
        _detailName = new Label { VerticalAlignment = VerticalAlignment.Center };
        _detailName.AddThemeFontSizeOverride("font_size", 22);
        head.AddChild(_detailName);
        _detailRank = new Label { Modulate = new Color(1, 1, 1, 0.8f) };
        inside.AddChild(_detailRank);
        _detail = new RichTextLabel { BbcodeEnabled = true, FitContent = true, SizeFlagsVertical = Control.SizeFlags.ExpandFill, ScrollActive = false };
        _detail.AddThemeFontSizeOverride("normal_font_size", 15);
        inside.AddChild(_detail);
        var ranks = new HBoxContainer { Alignment = BoxContainer.AlignmentMode.End };
        ranks.AddThemeConstantOverride("separation", 8);
        inside.AddChild(ranks);
        _rankMinus = Small(ranks, "−", () =>
        {
            if (Shown() is Spell sp)
                Editor.Lower(sp);
        });
        _rankPlus = Small(ranks, "+", () =>
        {
            if (Shown() is Spell sp)
                Editor.Raise(sp);
        });
        return page;
    }

    /// <summary>Shows a spell's details as if clicked (the capture of the project page).</summary>
    public void Select(string spell)
    {
        _selected = spell;
        ShowDetail();
    }

    /// <summary>The spell the details show: the one under the mouse, else the one clicked, else the first.</summary>
    private Spell? Shown()
    {
        string? id = _hovered ?? _selected ?? _tiles2.FirstOrDefault().Spell?.Id;
        return id is null ? null : Editor.Data.Spells[id];
    }

    private void ShowDetail()
    {
        if (Shown() is not Spell spell)
            return;
        var book = new SpellBook(Editor);
        BookSpell b = book.Spells.Single(x => x.Spell.Id == spell.Id);
        string colour = ElementStyle.Colour(spell.Element);
        _detailName.Text = spell.Name.In(Texts.Lang);
        _detailIcon.Spell = spell;
        _detailName.AddThemeColorOverride("font_color", new Color(colour));
        _detailRank.Text = $"{Texts["sd.rank", b.Rank, spell.MaxRank]} · {Texts["element." + spell.Element]}";
        Characteristics? stats = Fight.HeroTotals(Editor.Draft, Editor.Data)?.Stats;
        // The damage line in the element's colour, the critical line in it and in bold (D62), the rest plain.
        string min = b.Damage.Min.ToString(System.Globalization.CultureInfo.InvariantCulture) + " ";
        string critMin = b.Critical.Min.ToString(System.Globalization.CultureInfo.InvariantCulture) + " ";
        _detail.Text = string.Join('\n', Texts.SpellDetails(b, stats).Select(Capital).Select(line =>
            b.Critical.Max > 0 && line.StartsWith(critMin, StringComparison.Ordinal) && !line.Contains(Texts["element." + spell.Element], StringComparison.Ordinal) ? $"[color={colour}][b]{Escape(line)}[/b][/color]"
            : b.Damage.Max > 0 && line.StartsWith(min, StringComparison.Ordinal) ? $"[color={colour}]{Escape(line)}[/color]"
            : Escape(line)));
        _rankMinus.Disabled = !Editor.CanLower(spell);
        _rankPlus.Disabled = !Editor.CanRaise(spell);
    }

    private static string Capital(string text) => text.Length == 0 ? text : char.ToUpperInvariant(text[0]) + text[1..];

    private static string Escape(string text) => text.Replace("[", "[lb]", StringComparison.Ordinal);

    /// <summary>The colour of a characteristic: its element's, the heal colour for Vitality.</summary>
    private static string Colour(Characteristic stat) => stat switch
    {
        Characteristic.Earth => ElementStyle.Colour(Element.Earth),
        Characteristic.Fire => ElementStyle.Colour(Element.Fire),
        Characteristic.Water => ElementStyle.Colour(Element.Water),
        Characteristic.Air => ElementStyle.Colour(Element.Air),
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
        _tiles["left"].Text = $"{Texts["sheet.left"]}   {sheet.PointsLeft} / {Progression.CharacteristicPoints(Editor.Hero.Level)}";
        foreach (Label h in _statsPage.FindChildren("*", "Label", true, false).OfType<Label>().Where(l => l.HasMeta("key")))
            h.Text = Texts[(string)h.GetMeta("key")];
        foreach ((Characteristic stat, Label[] cells, LineEdit value, Button[] buttons) in _stats)
        {
            SheetLine line = sheet[stat];
            cells[0].Text = Texts[$"char.{stat}"];
            cells[0].TooltipText = Texts[$"char.{stat}.help"];
            if (!value.HasFocus())
                value.Text = Num(line.Invested);
            cells[1].Text = line.Worn == 0 ? "—" : "+" + Num(line.Worn);
            cells[2].Text = Num(line.Total);
            // Below the first 10 points, nothing yet: a dash rather than "+0".
            cells[3].Text = line.Bonus == 0 ? "—" : line.Element is Element e
                ? Texts["sheet.bonus.damage", line.Bonus, Texts["element." + e]]
                : Texts["sheet.bonus.hp", line.Bonus];
            cells[4].Text = line.Element is null ? "—" : $"{line.Resistance} %";
            buttons[0].Text = Texts["sheet.min"];
            buttons[3].Text = Texts["sheet.max"];
            buttons[0].Disabled = buttons[1].Disabled = !Editor.CanRemove(stat);
            buttons[2].Disabled = buttons[3].Disabled = !Editor.CanAdd(stat);
        }
        _reset.Text = Texts["sheet.reset"];
        _reset.Disabled = Editor.CharacteristicPointsLeft == Progression.CharacteristicPoints(Editor.Hero.Level);
        _help.Text = Texts["sheet.help"];
        _spellsLeft.Text = Texts["points.spells", Editor.SpellPointsLeft];
        foreach ((Element? e, Button chip) in _chips)
        {
            chip.Text = Capital(e is Element el ? Texts["element." + el] : Texts["sd.all"]);
            chip.SetPressedNoSignal(e == _filter);
        }
        var bookNow = new SpellBook(Editor);
        foreach ((Spell spell, Button tile, Label name, Label sub) in _tiles2)
        {
            BookSpell b = bookNow.Spells.Single(x => x.Spell.Id == spell.Id);
            tile.Visible = _filter is null || spell.Element == _filter;
            sub.Text = b.Unlocked ? Texts["sd.rank", b.Rank, spell.MaxRank] : Texts["sd.locked", spell.Level];
            tile.Modulate = b.Unlocked ? Colors.White : new Color(1, 1, 1, 0.45f);
        }
        ShowDetail();
        _save.Text = Texts["points.save"];
        _save.Disabled = !Editor.Changed;
        _close.Text = Texts["points.close"];
    }

    private static string Num(int n) => n.ToString(System.Globalization.CultureInfo.InvariantCulture);

    public void ShowMessage(string text) => _message.Text = text;

    /// <summary>The totals shown, as the self-test reads them.</summary>
    public IEnumerable<string> StatsText => _stats.Select(s => s.Value.Text);

    /// <summary>The spells on the spells tab, unlocked or not, as the self-test counts them.</summary>
    public int SpellTiles => _tiles2.Count;

    public string MessageText => _message.Text;

    private Button Small(Container parent, string text, Action act, float width = 44)
    {
        var b = new Button { Text = text, CustomMinimumSize = new Vector2(width, 44) };
        b.Pressed += () =>
        {
            act();
            Refresh();
        };
        parent.AddChild(b);
        return b;
    }
}
