using Godot;
using Rpg.Client;
using Rpg.Core;

namespace Rpg.Desktop;

/// <summary>
/// The bestiary (sprint 154, D83). Since sprint 171 (D87, <c>docs/graphisme/s165-retours-30-oct.md</c> §6 and
/// <c>docs/bestiaire-encyclopedie.md</c> §4) the theme's window in three columns: on the left the zones and their families,
/// each with its emblem in relief and how many of its monsters were met; in the middle the chosen family's monsters, a row
/// each with its role, level and loot; on the right the chosen monster's card, a block for each step of the hunting log,
/// those not reached yet saying what they open and the victories they still ask. What it shows is <see cref="BestiaryPage"/>.
/// </summary>
public partial class BestiaryPanel : CanvasLayer
{
    private VBoxContainer _families = null!, _monsters = null!, _card = null!;
    private Label _count = null!, _familyHead = null!, _monsterFoot = null!;
    private ProgressBar _progress = null!;
    private Texts _texts = null!;
    private IReadOnlyList<BestiaryFamily> _page = [];
    private readonly Dictionary<string, Button> _familyRows = [], _monsterRows = [];

    public event Action? ClosePressed;

    /// <summary>The family shown in the middle, and its monsters' names (for the self-test).</summary>
    public string? Shown { get; private set; }

    public List<string> Names { get; } = [];

    /// <summary>The monster on the card, its blocks reached and its loot's tiles (for the self-test).</summary>
    public string? Chosen { get; private set; }
    public int BlocksReached { get; private set; }
    public int LootTiles { get; private set; }

    public void Build(Texts texts)
    {
        _texts = texts;
        Layer = 15;
        var panel = new PanelContainer { AnchorLeft = 0.5f, AnchorRight = 0.5f, AnchorTop = 0.5f, AnchorBottom = 0.5f, OffsetLeft = -480, OffsetRight = 480, OffsetTop = -300, OffsetBottom = 300, ThemeTypeVariation = OsmoseTheme.WindowPanel };
        AddChild(panel);
        WindowDrag.Make(panel, "bestiary");
        var column = new VBoxContainer();
        column.AddThemeConstantOverride("separation", 0);
        panel.AddChild(column);
        column.AddChild(TitleBar(texts));
        var margin = new MarginContainer { SizeFlagsVertical = Control.SizeFlags.ExpandFill };
        foreach (string side in new[] { "left", "right", "top", "bottom" })
            margin.AddThemeConstantOverride("margin_" + side, 16);
        column.AddChild(margin);
        var body = new HBoxContainer();
        body.AddThemeConstantOverride("separation", 16);
        margin.AddChild(body);
        body.AddChild(Column(240, out _families, out Label familyFoot));
        familyFoot.Text = texts["bestiary.families", GameData.Embedded.Families.Count];
        var middle = new VBoxContainer { CustomMinimumSize = new Vector2(384, 0) };
        _familyHead = Text("", 16, UiStyle.TextStrong, bold: true);
        middle.AddChild(_familyHead);
        middle.AddChild(Column(384, out _monsters, out _monsterFoot));
        body.AddChild(middle);
        var right = new ScrollContainer { CustomMinimumSize = new Vector2(272, 0), HorizontalScrollMode = ScrollContainer.ScrollMode.Disabled, SizeFlagsVertical = Control.SizeFlags.ExpandFill };
        _card = new VBoxContainer { SizeFlagsHorizontal = Control.SizeFlags.ExpandFill };
        _card.AddThemeConstantOverride("separation", 6);
        right.AddChild(_card);
        body.AddChild(right);
    }

    private PanelContainer TitleBar(Texts texts)
    {
        var bar = new PanelContainer { ThemeTypeVariation = OsmoseTheme.TitleBar, CustomMinimumSize = new Vector2(0, 44) };
        var row = new HBoxContainer();
        row.AddThemeConstantOverride("separation", 8);
        bar.AddChild(row);
        row.AddChild(Picture("res://assets/icons/bar/bestiary.svg", 24));
        row.AddChild(Text(texts["bestiary.title"], 18, UiStyle.TextStrong, bold: true));
        row.AddChild(new Control { SizeFlagsHorizontal = Control.SizeFlags.ExpandFill });
        // The whole log's progress, the window's focus.
        _progress = new ProgressBar { CustomMinimumSize = new Vector2(200, 16), ShowPercentage = false, SizeFlagsVertical = Control.SizeFlags.ShrinkCenter, ThemeTypeVariation = OsmoseTheme.ProgressBarCompact };
        row.AddChild(_progress);
        _count = Text("", 14, UiStyle.TextStrong, bold: true);
        row.AddChild(_count);
        var close = new Button { Text = "×", CustomMinimumSize = new Vector2(44, 44), ThemeTypeVariation = OsmoseTheme.CloseButton, FocusMode = Control.FocusModeEnum.None };
        close.Pressed += () => ClosePressed?.Invoke();
        row.AddChild(close);
        return bar;
    }

    /// <summary>A column that scrolls, its list, and a foot that counts it.</summary>
    private static VBoxContainer Column(float width, out VBoxContainer list, out Label foot)
    {
        var column = new VBoxContainer { CustomMinimumSize = new Vector2(width, 0), SizeFlagsVertical = Control.SizeFlags.ExpandFill };
        var scroll = new ScrollContainer { SizeFlagsVertical = Control.SizeFlags.ExpandFill, HorizontalScrollMode = ScrollContainer.ScrollMode.Disabled };
        list = new VBoxContainer { SizeFlagsHorizontal = Control.SizeFlags.ExpandFill };
        list.AddThemeConstantOverride("separation", 2);
        scroll.AddChild(list);
        column.AddChild(scroll);
        foot = Text("", 14, UiStyle.TextDim);
        column.AddChild(foot);
        return column;
    }

    /// <summary>The bestiary for these victories; the first family that has a monster met is shown, else the first.</summary>
    public void Show(IReadOnlyDictionary<string, int> victories, IReadOnlySet<string>? quests = null)
    {
        ArgumentNullException.ThrowIfNull(victories);
        _page = BestiaryPage.Families(GameData.Embedded, victories, _texts, quests);
        foreach (Node n in _families.GetChildren())
            n.QueueFree();
        _familyRows.Clear();
        int known = _page.Sum(f => f.Met), all = _page.Sum(f => f.Monsters.Count);
        _count.Text = _texts["bestiary.known", known, all];
        (_progress.MaxValue, _progress.Value) = (all, known);
        string? zone = null;
        foreach (BestiaryFamily family in _page)
        {
            if (family.Zone != zone)
            {
                zone = family.Zone;
                var head = Text(zone.ToUpperInvariant(), 14, UiStyle.ChocolateLight, bold: true);
                head.CustomMinimumSize = new Vector2(0, 32);
                head.VerticalAlignment = VerticalAlignment.Bottom;
                _families.AddChild(head);
            }
            _families.AddChild(FamilyRow(family));
        }
        if (_page.Count > 0)
            ShowFamily((_page.FirstOrDefault(f => f.Met > 0) ?? _page[0]).Id);
    }

    private Button FamilyRow(BestiaryFamily family)
    {
        // The emblem, the name and "3/5", a thin bar under them; greyed while none of it is met.
        var row = new Button { CustomMinimumSize = new Vector2(0, 56), ThemeTypeVariation = OsmoseTheme.TreeRow, ToggleMode = true, FocusMode = Control.FocusModeEnum.None };
        var line = new HBoxContainer { MouseFilter = Control.MouseFilterEnum.Ignore };
        line.SetAnchorsPreset(Control.LayoutPreset.FullRect);
        (line.OffsetLeft, line.OffsetRight) = (8, -8);
        line.AddThemeConstantOverride("separation", 8);
        line.AddChild(Picture($"res://assets/icons/relief/family-{family.Id}{(family.Met == 0 ? "-locked" : "")}.svg", 32));
        var words = new VBoxContainer { SizeFlagsHorizontal = Control.SizeFlags.ExpandFill, Alignment = BoxContainer.AlignmentMode.Center, MouseFilter = Control.MouseFilterEnum.Ignore };
        var top = new HBoxContainer { MouseFilter = Control.MouseFilterEnum.Ignore };
        var name = Text(family.Name, 14, family.Met == 0 ? UiStyle.TextDim : UiStyle.Text);
        name.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
        top.AddChild(name);
        top.AddChild(Text($"{family.Met}/{family.Monsters.Count}", 14, UiStyle.TextDim));
        words.AddChild(top);
        words.AddChild(new ProgressBar { MaxValue = family.Monsters.Count, Value = family.Met, ShowPercentage = false, CustomMinimumSize = new Vector2(0, 8), ThemeTypeVariation = OsmoseTheme.ProgressBarCompact, MouseFilter = Control.MouseFilterEnum.Ignore });
        line.AddChild(words);
        row.AddChild(line);
        row.Pressed += () => ShowFamily(family.Id);
        _familyRows[family.Id] = row;
        return row;
    }

    public void ShowFamily(string id)
    {
        foreach (Node n in _monsters.GetChildren())
            n.QueueFree();
        _monsterRows.Clear();
        Names.Clear();
        Shown = id;
        foreach ((string family, Button row) in _familyRows)
            row.SetPressedNoSignal(family == id);
        BestiaryFamily shown = _page.First(f => f.Id == id);
        _familyHead.Text = _texts["bestiary.family-count", shown.Name, shown.Met, shown.Monsters.Count];
        _monsterFoot.Text = _texts["bestiary.monsters", shown.Monsters.Count];
        foreach (BestiaryMonster m in shown.Monsters)
        {
            _monsters.AddChild(MonsterRow(id, m));
            Names.Add(m.Name);
        }
        ShowMonster((shown.Monsters.FirstOrDefault(m => m.Known != Knowledge.Unknown) ?? shown.Monsters[0]).Id);
    }

    private Button MonsterRow(string family, BestiaryMonster m)
    {
        bool unknown = m.Known == Knowledge.Unknown;
        var row = new Button { CustomMinimumSize = new Vector2(0, 64), ThemeTypeVariation = OsmoseTheme.TreeRow, ToggleMode = true, FocusMode = Control.FocusModeEnum.None };
        var line = new HBoxContainer { MouseFilter = Control.MouseFilterEnum.Ignore };
        line.SetAnchorsPreset(Control.LayoutPreset.FullRect);
        (line.OffsetLeft, line.OffsetRight) = (8, -8);
        line.AddThemeConstantOverride("separation", 10);
        line.AddChild(Picture($"res://assets/icons/relief/family-{family}{(unknown ? "-locked" : "")}.svg", 44));
        var words = new VBoxContainer { SizeFlagsHorizontal = Control.SizeFlags.ExpandFill, Alignment = BoxContainer.AlignmentMode.Center, MouseFilter = Control.MouseFilterEnum.Ignore };
        words.AddThemeConstantOverride("separation", 2);
        var top = new HBoxContainer { MouseFilter = Control.MouseFilterEnum.Ignore };
        var name = Text(m.Name, 16, unknown ? UiStyle.TextDim : UiStyle.TextStrong, bold: !unknown);
        name.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
        top.AddChild(name);
        top.AddChild(Text(_texts["bestiary.level", m.Level], 14, UiStyle.TextDim));
        words.AddChild(top);
        var under = new HBoxContainer { MouseFilter = Control.MouseFilterEnum.Ignore };
        under.AddThemeConstantOverride("separation", 4);
        under.AddChild(Text(unknown ? _texts["bestiary.fight-it"] : _texts["role." + m.Role], 14, unknown ? UiStyle.TextOff : UiStyle.TextDim));
        // Its loot once studied, a small tile each.
        foreach (BestiaryLoot loot in (m.Loot ?? []).Take(5))
            under.AddChild(LootTile(loot, 24));
        words.AddChild(under);
        line.AddChild(words);
        row.AddChild(line);
        row.Pressed += () => ShowMonster(m.Id);
        _monsterRows[m.Id] = row;
        return row;
    }

    public void ShowMonster(string id)
    {
        foreach (Node child in _card.GetChildren())
            child.QueueFree();
        Chosen = id;
        LootTiles = 0;
        foreach ((string monster, Button row) in _monsterRows)
            row.SetPressedNoSignal(monster == id);
        BestiaryFamily family = _page.First(f => f.Monsters.Any(m => m.Id == id));
        BestiaryMonster m = family.Monsters.First(x => x.Id == id);
        bool unknown = m.Known == Knowledge.Unknown;
        // The picture in a hollow: the family's emblem until the portraits of sprint 172.
        var well = new PanelContainer { CustomMinimumSize = new Vector2(272, 120) };
        well.AddThemeStyleboxOverride("panel", OsmoseTheme.Flat(UiStyle.Well, corner: 8));
        var emblem = Picture($"res://assets/icons/relief/family-{family.Id}{(unknown ? "-locked" : "")}.svg", 88);
        emblem.SizeFlagsHorizontal = Control.SizeFlags.ShrinkCenter;
        well.AddChild(emblem);
        _card.AddChild(well);
        _card.AddChild(Text(m.Name, 18, unknown ? UiStyle.TextDim : UiStyle.TextStrong, bold: true, wrap: true));
        _card.AddChild(Text($"{(unknown ? _texts["role." + m.Role] : m.Family)} · {_texts["bestiary.level", m.Level]} · {m.Zone}", 14, UiStyle.TextDim, wrap: true));
        // The victories towards the next step.
        if (m.Next is (Knowledge step, int left))
        {
            _card.AddChild(Text(_texts["bestiary.next-step", _texts["bestiary.step." + step], left], 14, UiStyle.Text));
            (int studied, int known, int mastered) = Bestiary.Steps(GameData.Embedded.Monsters[id]);
            int goal = step switch { Knowledge.Studied => studied, Knowledge.Known => known, _ => mastered };
            _card.AddChild(new ProgressBar { MaxValue = Math.Max(1, goal), Value = m.Victories, ShowPercentage = false, CustomMinimumSize = new Vector2(0, 8), ThemeTypeVariation = OsmoseTheme.ProgressBarCompact });
        }
        else
            _card.AddChild(Text(_texts["bestiary.step.Mastered"] + " · " + _texts["bestiary.victories", m.Victories], 14, UiStyle.Gold, bold: true));
        BlocksReached = 0;
        // Met: its numbers and where it lives.
        if (Block("bestiary.section.fight", m.Sections[0]) && m.Numbers is BestiaryNumbers n)
        {
            _card.AddChild(Text(_texts["bestiary.numbers", n.Hp, n.Ap, n.Mp, n.Initiative], 14, UiStyle.TextStrong, bold: true));
            if (m.Resistances is { } resistances)
                _card.AddChild(Resistances(resistances, m.Weakness));
        }
        // Studied: its loot, a tile each with its rate.
        if (Block("bestiary.section.loot", m.Sections[1]))
        {
            var grid = new HFlowContainer();
            grid.AddThemeConstantOverride("h_separation", 6);
            grid.AddThemeConstantOverride("v_separation", 6);
            foreach (BestiaryLoot loot in m.Loot ?? [])
            {
                var one = new VBoxContainer();
                one.AddThemeConstantOverride("separation", 0);
                one.AddChild(LootTile(loot, 40));
                var rate = Text($"{loot.Percent} %", 12, loot.Quest is null ? UiStyle.Text : UiStyle.Cost);
                (rate.HorizontalAlignment, rate.CustomMinimumSize) = (HorizontalAlignment.Center, new Vector2(40, 0));
                one.AddChild(rate);
                grid.AddChild(one);
                LootTiles++;
            }
            _card.AddChild(grid);
        }
        // Known: its spells, its family's lesson, how it fights.
        if (Block("bestiary.section.spells", m.Sections[2]))
        {
            var spells = new HFlowContainer();
            spells.AddThemeConstantOverride("h_separation", 4);
            foreach (string spellId in m.Spells ?? [])
            {
                Spell spell = GameData.Embedded.Spells[spellId];
                var icon = new TextureRect { Texture = SpellIcon.TextureOf(spell, "-small"), ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize, StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered, CustomMinimumSize = new Vector2(32, 32), TooltipText = spell.Name.In(_texts.Lang) };
                spells.AddChild(icon);
            }
            _card.AddChild(spells);
            foreach (string lesson in m.Lessons ?? [])
                _card.AddChild(Text(_texts["bestiary.lesson", GameData.Embedded.Passives[lesson].Name.In(_texts.Lang), GameData.Embedded.Passives[lesson].Description.In(_texts.Lang)], 14, UiStyle.Text, wrap: true));
            if (m.Behaviour is string behaviour)
                _card.AddChild(Text(behaviour, 14, UiStyle.TextDim, wrap: true));
        }
        // Where to find it, once met.
        if (m.Maps is { Count: > 0 } maps)
        {
            Heading("bestiary.section.where", reached: true);
            _card.AddChild(Text(string.Join(" · ", maps.Take(4)) + (maps.Count > 4 ? " …" : ""), 14, UiStyle.Text, wrap: true));
        }
        Block("bestiary.section.story", m.Sections[3]);
    }

    /// <summary>A block's heading; reached, true; not yet, its one line and a padlock, false.</summary>
    private bool Block(string heading, BestiarySection section)
    {
        Heading(heading, section.Reached);
        if (section.Reached)
        {
            BlocksReached++;
            return true;
        }
        _card.AddChild(Text(section.Locked, 14, UiStyle.TextDim, wrap: true));
        return false;
    }

    private void Heading(string key, bool reached)
    {
        var row = new HBoxContainer();
        // Reached in light chocolate; not yet, in the dim grey.
        var label = Text(_texts[key].ToUpperInvariant(), 14, reached ? UiStyle.ChocolateLight : UiStyle.TextOff, bold: true);
        label.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
        row.AddChild(label);
        _card.AddChild(new HSeparator());
        _card.AddChild(row);
    }

    /// <summary>A pill per element, on its darkened colour, its value in white; the weakness edged in the hints' yellow.</summary>
    private HFlowContainer Resistances(IReadOnlyDictionary<Element, int> resistances, Element? weakness)
    {
        var row = new HFlowContainer();
        row.AddThemeConstantOverride("h_separation", 4);
        row.AddThemeConstantOverride("v_separation", 4);
        foreach ((Element element, int value) in resistances)
        {
            var pill = new PanelContainer { CustomMinimumSize = new Vector2(40, 24), TooltipText = _texts["element." + element] + (element == weakness ? " · " + _texts["bestiary.weakness", _texts["element." + element]] : "") };
            StyleBoxFlat box = OsmoseTheme.Flat(UiStyle.Well, element == weakness ? UiStyle.Cost : null, element == weakness ? 2 : 0, 6, 2);
            box.BgColor = new Color(ElementStyle.Colour(element)).Darkened(0.45f);
            pill.AddThemeStyleboxOverride("panel", box);
            var number = Text(value.ToString("+0;−0;0", System.Globalization.CultureInfo.InvariantCulture), 14, "#ffffff", bold: true);
            number.HorizontalAlignment = HorizontalAlignment.Center;
            pill.AddChild(number);
            row.AddChild(pill);
        }
        return row;
    }

    private Button LootTile(BestiaryLoot loot, float size)
    {
        Item item = GameData.Embedded.Items[loot.Item];
        (Button tile, ItemIcon icon) = ItemIcon.Tile(size);
        (icon.Item, icon.Count) = (item, loot.Count);
        tile.TooltipText = ItemMenu.Tooltip(item, _texts) + $"\n{loot.Percent} %" + (loot.Quest is string quest && GameData.Embedded.Quests.TryGetValue(quest, out Quest? q) ? $" · {_texts["bestiary.for-quest"]} « {q.Name.In(_texts.Lang)} »" : "");
        return tile;
    }

    private static TextureRect Picture(string path, float size) => new()
    {
        Texture = GD.Load<Texture2D>(path),
        ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
        StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered,
        CustomMinimumSize = new Vector2(size, size),
        SizeFlagsVertical = Control.SizeFlags.ShrinkCenter,
        MouseFilter = Control.MouseFilterEnum.Ignore,
    };

    /// <summary>A line of the window; <paramref name="wrap"/> for the card's sentences, which take the card's width.</summary>
    private static Label Text(string text, int size, string colour, bool bold = false, bool wrap = false)
    {
        var l = new Label { Text = text, AutowrapMode = wrap ? TextServer.AutowrapMode.WordSmart : TextServer.AutowrapMode.Off, MouseFilter = Control.MouseFilterEnum.Ignore, VerticalAlignment = VerticalAlignment.Center };
        l.AddThemeFontSizeOverride("font_size", size);
        l.AddThemeColorOverride("font_color", new Color(colour));
        if (bold)
            l.AddThemeFontOverride("font", OsmoseTheme.Bold());
        return l;
    }
}
