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
/// Since sprint 175 (D87, B3 to B5) the left column searches and filters (<see cref="BestiaryQuery"/>), the middle one lists
/// what is found across the families; the card's magnifier opens the world map on the zone, its loot the items' cards, and
/// it says which of the deck's spells hit the weakness; a mastered monster tells its story in a golden frame, a family met
/// whole its paragraph.
/// </summary>
public partial class BestiaryPanel : CanvasLayer
{
    private VBoxContainer _families = null!, _monsters = null!, _card = null!;
    private Label _count = null!, _familyHead = null!, _monsterFoot = null!;
    private ProgressBar _progress = null!;
    private Texts _texts = null!;
    private IReadOnlyList<BestiaryFamily> _page = [];
    private IReadOnlySet<string>? _quests;
    private IReadOnlyList<string> _deck = [];
    private IReadOnlyList<BestiaryHit> _hits = [];
    private readonly Dictionary<string, Button> _familyRows = [], _monsterRows = [];
    private readonly Dictionary<string, Control> _zoneHeads = [];
    private LineEdit _search = null!;
    private OptionButton _zone = null!, _role = null!, _step = null!;
    private SpinBox _from = null!, _to = null!;
    private IReadOnlyList<(string Id, string Name)> _zones = [];
    private static readonly int Highest = GameData.Embedded.Monsters.Values.Max(m => m.Level);

    // While the filters are set from the code, their signals wait.
    private bool _setting;

    public event Action? ClosePressed;

    /// <summary>The card's magnifier: the monster's zone and the maps it lives on (none while it is unknown).</summary>
    public event Action<string, IReadOnlyList<string>>? LocatePressed;

    /// <summary>A tile of loot pressed: the item's card.</summary>
    public event Action<Item>? ItemPressed;

    /// <summary>Whether the magnifier may open the map (not in a fight, where the map is not).</summary>
    public bool CanLocate { get; set; } = true;

    /// <summary>What the list asks, the monsters it found, and the golden frames shown (for the self-test).</summary>
    public BestiaryFilter Filter { get; private set; } = new();

    public int Found => _hits.Count;

    /// <summary>The ids of the monsters found.</summary>
    public IEnumerable<string> FoundIds => _hits.Select(h => h.Monster.Id);
    public int Framed => _rowsFramed + (_cardFramed ? 1 : 0);

    private int _rowsFramed;
    private bool _cardFramed;

    /// <summary>The card's line on the deck's spells, its story and the shown family's (for the self-test).</summary>
    public string? SpellsLine { get; private set; }
    public string? Story { get; private set; }
    public string? FamilyStory { get; private set; }

    /// <summary>The family shown in the middle, and its monsters' names (for the self-test).</summary>
    public string? Shown { get; private set; }

    public List<string> Names { get; } = [];

    /// <summary>The monster on the card, its blocks reached and its loot's tiles (for the self-test).</summary>
    public string? Chosen { get; private set; }
    public int BlocksReached { get; private set; }
    public int LootTiles { get; private set; }

    /// <summary>The look turning on the card, none for a silhouette (for the self-test).</summary>
    public string? Turning { get; private set; }

    /// <summary>The pictures shown, the rows' then the card's silhouette, each loaded or not (for the self-test).</summary>
    public List<(string Path, bool Loaded)> Pictures { get; } = [];

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
        // The left column: the search and the filters over the zones and their families.
        var left = new VBoxContainer { CustomMinimumSize = new Vector2(240, 0), SizeFlagsVertical = Control.SizeFlags.ExpandFill };
        left.AddThemeConstantOverride("separation", 4);
        left.AddChild(Filters(texts));
        left.AddChild(Column(240, out _families, out Label familyFoot));
        familyFoot.Text = texts["bestiary.families", GameData.Embedded.Families.Count];
        body.AddChild(left);
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

    /// <summary>The search field and its reset, the zone, the role, the step, the levels from and to: 44 pixels each.</summary>
    private VBoxContainer Filters(Texts texts)
    {
        var box = new VBoxContainer();
        box.AddThemeConstantOverride("separation", 4);
        var top = new HBoxContainer();
        top.AddThemeConstantOverride("separation", 4);
        var field = new Control { CustomMinimumSize = new Vector2(0, 44), SizeFlagsHorizontal = Control.SizeFlags.ExpandFill };
        _search = new LineEdit { PlaceholderText = texts["bestiary.search"], ClearButtonEnabled = true, ThemeTypeVariation = OsmoseTheme.SearchField };
        _search.SetAnchorsPreset(Control.LayoutPreset.FullRect);
        _search.TextChanged += _ => Changed();
        field.AddChild(_search);
        field.AddChild(new TextureRect { Texture = GD.Load<Texture2D>("res://assets/icons/relief/search.svg"), ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize, StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered, Position = new Vector2(12, 12), Size = new Vector2(20, 20), MouseFilter = Control.MouseFilterEnum.Ignore });
        top.AddChild(field);
        var reset = new Button { Text = "↺", CustomMinimumSize = new Vector2(44, 44), TooltipText = texts["bestiary.reset"], FocusMode = Control.FocusModeEnum.None };
        reset.Pressed += () => Apply(new BestiaryFilter());
        top.AddChild(reset);
        box.AddChild(top);
        _zones = BestiaryQuery.Zones(GameData.Embedded, texts);
        _zone = Choice([texts["bestiary.zone-all"], .. _zones.Select(z => z.Name)]);
        _role = Choice([texts["bestiary.role-all"], .. Enum.GetValues<MonsterRole>().Select(r => texts["role." + r])]);
        _step = Choice([.. Enum.GetValues<BestiaryStepFilter>().Select(f => texts["bestiary.filter." + f])]);
        box.AddChild(_zone);
        var choices = new HBoxContainer();
        choices.AddThemeConstantOverride("separation", 4);
        (_role.SizeFlagsHorizontal, _step.SizeFlagsHorizontal) = (Control.SizeFlags.ExpandFill, Control.SizeFlags.ExpandFill);
        choices.AddChild(_role);
        choices.AddChild(_step);
        box.AddChild(choices);
        var levels = new HBoxContainer();
        levels.AddThemeConstantOverride("separation", 4);
        levels.AddChild(Text(texts["bestiary.levels"], 14, UiStyle.TextDim));
        _from = Level(1);
        levels.AddChild(_from);
        levels.AddChild(Text(texts["bestiary.level-to"], 14, UiStyle.TextDim));
        _to = Level(Highest);
        levels.AddChild(_to);
        box.AddChild(levels);
        return box;
    }

    private OptionButton Choice(IEnumerable<string> items)
    {
        var choice = new OptionButton { CustomMinimumSize = new Vector2(0, 44), FocusMode = Control.FocusModeEnum.None, ClipText = true };
        foreach (string item in items)
            choice.AddItem(item);
        choice.ItemSelected += _ => Changed();
        return choice;
    }

    private SpinBox Level(int value)
    {
        var box = new SpinBox { MinValue = 1, MaxValue = Highest, Value = value, CustomMinimumSize = new Vector2(80, 44), SizeFlagsHorizontal = Control.SizeFlags.ExpandFill };
        box.ValueChanged += _ => Changed();
        return box;
    }

    /// <summary>The filter the controls say: a bound at its end of the levels is none.</summary>
    private void Changed()
    {
        if (_setting)
            return;
        Refresh(new BestiaryFilter(_search.Text, _zone.Selected > 0 ? _zones[_zone.Selected - 1].Id : null, (int)_from.Value > 1 ? (int)_from.Value : 0,
            (int)_to.Value < Highest ? (int)_to.Value : 0, _role.Selected > 0 ? Enum.GetValues<MonsterRole>()[_role.Selected - 1] : null, (BestiaryStepFilter)Math.Max(0, _step.Selected)));
    }

    /// <summary>Sets the filters to <paramref name="filter"/> (the reset, the way back from the map) and lists what it finds.</summary>
    public void Apply(BestiaryFilter filter)
    {
        ArgumentNullException.ThrowIfNull(filter);
        _setting = true;
        _search.Text = filter.Search;
        _zone.Selected = filter.Zone is string zone ? 1 + _zones.ToList().FindIndex(z => z.Id == zone) : 0;
        _role.Selected = filter.Role is MonsterRole role ? 1 + (int)role : 0;
        _step.Selected = (int)filter.Step;
        _from.Value = filter.MinLevel > 0 ? filter.MinLevel : 1;
        _to.Value = filter.MaxLevel > 0 ? filter.MaxLevel : Highest;
        _setting = false;
        Refresh(filter);
    }

    /// <summary>
    /// The list for a filter: nothing asked, the families as they are; else the families with a monster found, the middle
    /// listing every monster found, the first one on the card.
    /// </summary>
    private void Refresh(BestiaryFilter filter)
    {
        Filter = filter;
        if (_page.Count == 0)
            return;
        _hits = BestiaryQuery.Find(GameData.Embedded, _page, filter, _texts, _quests);
        HashSet<string> families = [.. _hits.Select(h => h.Family.Id)];
        foreach ((string id, Button row) in _familyRows)
            row.Visible = filter.IsEmpty || families.Contains(id);
        foreach ((string zone, Control head) in _zoneHeads)
            head.Visible = _page.Any(f => f.Zone == zone && _familyRows[f.Id].Visible);
        if (filter.IsEmpty)
        {
            ShowFamily(Shown is string shown && _page.Any(f => f.Id == shown) ? shown : (_page.FirstOrDefault(f => f.Met > 0) ?? _page[0]).Id);
            return;
        }
        Shown = null;
        foreach ((_, Button row) in _familyRows)
            row.SetPressedNoSignal(false);
        _familyHead.Text = _texts["bestiary.found", _hits.Count];
        List(_hits);
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
    public void Show(IReadOnlyDictionary<string, int> victories, IReadOnlySet<string>? quests = null, IReadOnlyList<string>? deck = null)
    {
        ArgumentNullException.ThrowIfNull(victories);
        (_quests, _deck) = (quests, deck ?? []);
        _page = BestiaryPage.Families(GameData.Embedded, victories, _texts, quests);
        foreach (Node n in _families.GetChildren())
            n.QueueFree();
        _familyRows.Clear();
        _zoneHeads.Clear();
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
                _zoneHeads[zone] = head;
            }
            _families.AddChild(FamilyRow(family));
        }
        if (_page.Count > 0)
            Refresh(Filter);
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

    /// <summary>A family's monsters, under its story; while the list is filtered, those of it that were found.</summary>
    public void ShowFamily(string id)
    {
        Shown = id;
        foreach ((string family, Button row) in _familyRows)
            row.SetPressedNoSignal(family == id);
        BestiaryFamily shown = _page.First(f => f.Id == id);
        _familyHead.Text = _texts["bestiary.family-count", shown.Name, shown.Met, shown.Monsters.Count];
        List(Filter.IsEmpty ? [.. shown.Monsters.Select(m => new BestiaryHit(shown, m, []))] : [.. _hits.Where(h => h.Family.Id == id)], shown);
    }

    /// <summary>The middle column: the family's paragraph when one is shown, then a row a monster, the first met on the card.</summary>
    private void List(IReadOnlyList<BestiaryHit> hits, BestiaryFamily? family = null)
    {
        foreach (Node n in _monsters.GetChildren())
            n.QueueFree();
        _monsterRows.Clear();
        Names.Clear();
        Pictures.Clear();
        _rowsFramed = 0;
        FamilyStory = family?.Story;
        if (family is not null)
        {
            // Its story once its five monsters are met; a family mastered whole says so in gold.
            if (family.Hunted)
                _monsters.AddChild(Text(_texts["bestiary.hunted"], 14, UiStyle.Gold, bold: true));
            _monsters.AddChild(Text(family.Story ?? _texts["bestiary.family-story-locked", family.Met], 14, family.Story is null ? UiStyle.TextOff : UiStyle.Text, wrap: true));
        }
        _monsterFoot.Text = _texts["bestiary.monsters", hits.Count];
        foreach (BestiaryHit hit in hits)
        {
            _monsters.AddChild(MonsterRow(hit.Monster, family is null ? hit : null));
            Names.Add(hit.Monster.Name);
        }
        if (hits.Count == 0)
        {
            _monsters.AddChild(Text(_texts["bestiary.nothing"], 14, UiStyle.TextDim, wrap: true));
            foreach (Node child in _card.GetChildren())
                child.QueueFree();
            Chosen = null;
            return;
        }
        ShowMonster((hits.FirstOrDefault(h => h.Monster.Known != Knowledge.Unknown) ?? hits[0]).Monster.Id);
    }

    /// <summary>A monster's row: its portrait (framed in gold once mastered), name, level, role and loot; found by a search, its family and the items found.</summary>
    private Button MonsterRow(BestiaryMonster m, BestiaryHit? found = null)
    {
        bool unknown = m.Known == Knowledge.Unknown;
        var row = new Button { CustomMinimumSize = new Vector2(0, 64), ThemeTypeVariation = OsmoseTheme.TreeRow, ToggleMode = true, FocusMode = Control.FocusModeEnum.None };
        var line = new HBoxContainer { MouseFilter = Control.MouseFilterEnum.Ignore };
        line.SetAnchorsPreset(Control.LayoutPreset.FullRect);
        (line.OffsetLeft, line.OffsetRight) = (8, -8);
        line.AddThemeConstantOverride("separation", 10);
        line.AddChild(Framing(Still(m, 52), m.Known == Knowledge.Mastered, 6));
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
        string role = found is null ? _texts["role." + m.Role] : found.Family.Name;
        under.AddChild(Text(unknown && found is null ? _texts["bestiary.fight-it"] : role, 14, unknown ? UiStyle.TextOff : UiStyle.TextDim));
        if (found is { Items.Count: > 0 })
            under.AddChild(Text(_texts["bestiary.drops", string.Join(", ", found.Items.Select(i => GameData.Embedded.Items[i].Name.In(_texts.Lang)))], 14, UiStyle.Cost));
        // Its loot once studied, a small tile each.
        else
            foreach (BestiaryLoot loot in (m.Loot ?? []).Take(5))
                under.AddChild(LootTile(loot, 24, pressable: false));
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
        _cardFramed = false;
        (SpellsLine, Story) = (null, null);
        // The rows' portraits stay, the card's silhouette goes with the card.
        Pictures.RemoveRange(_monsterRows.Count, Pictures.Count - _monsterRows.Count);
        foreach ((string monster, Button row) in _monsterRows)
            row.SetPressedNoSignal(monster == id);
        BestiaryFamily family = _page.First(f => f.Monsters.Any(m => m.Id == id));
        BestiaryMonster m = family.Monsters.First(x => x.Id == id);
        bool unknown = m.Known == Knowledge.Unknown;
        // In a hollow, its model as in a fight, turning slowly; while unknown, its silhouette, still (sprint 172, D88).
        // Mastered, its hollow framed in gold.
        _cardFramed = m.Known == Knowledge.Mastered;
        var well = new PanelContainer { CustomMinimumSize = new Vector2(272, 160) };
        well.AddThemeStyleboxOverride("panel", OsmoseTheme.Flat(UiStyle.Well, _cardFramed ? UiStyle.Gold : null, _cardFramed ? 3 : 0, 8));
        if (unknown)
        {
            TextureRect shadow = Still(m, 140);
            shadow.SizeFlagsHorizontal = Control.SizeFlags.ShrinkCenter;
            well.AddChild(shadow);
            Turning = null;
        }
        else
        {
            Portrait model = Portrait.Make(new Vector2(272, 160), turning: true).Fit();
            model.Show(new Hero("", m.Look), paint: false);
            well.AddChild(model);
            Turning = m.Look;
        }
        _card.AddChild(well);
        _card.AddChild(Text(m.Name, 18, unknown ? UiStyle.TextDim : UiStyle.TextStrong, bold: true, wrap: true));
        // Its zone, with the magnifier that opens the map on it.
        var where = new HBoxContainer();
        where.AddThemeConstantOverride("separation", 4);
        Label about = Text($"{(unknown ? _texts["role." + m.Role] : m.Family)} · {_texts["bestiary.level", m.Level]} · {m.Zone}", 14, UiStyle.TextDim, wrap: true);
        about.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
        where.AddChild(about);
        if (CanLocate)
        {
            var locate = new Button { Icon = GD.Load<Texture2D>("res://assets/icons/relief/search.svg"), ExpandIcon = true, CustomMinimumSize = new Vector2(44, 44), TooltipText = _texts["bestiary.locate"], FocusMode = Control.FocusModeEnum.None };
            locate.Pressed += () => Locate(m);
            where.AddChild(locate);
        }
        _card.AddChild(where);
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
            {
                _card.AddChild(Resistances(resistances, m.Weakness));
                // Which of the deck's spells hit its weakness, and which meet its resistances.
                if (BestiaryPage.YourSpells(GameData.Embedded, m, _deck) is BestiarySpells spells)
                {
                    SpellsLine = spells.Hurting == 0 ? _texts["bestiary.your-spells.none"]
                        : spells.Weakness is Element weak ? _texts["bestiary.your-spells", spells.Weak.Count, spells.Hurting, _texts["element." + weak]]
                        : _texts["bestiary.your-spells.no-weakness"];
                    if (spells.Resisted.Count > 0)
                        SpellsLine += " · " + _texts["bestiary.your-resisted", spells.Resisted.Count];
                    _card.AddChild(Text(SpellsLine, 14, spells.Weak.Count > 0 ? UiStyle.Pistachio : UiStyle.Text, wrap: true));
                }
            }
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
        // Mastered: its line of story.
        if (Block("bestiary.section.story", m.Sections[3]) && m.Story is string story)
        {
            Story = story;
            _card.AddChild(Text(story, 14, UiStyle.Text, wrap: true));
        }
    }

    /// <summary>The magnifier: the monster's zone, and the maps it lives on once it is met.</summary>
    private void Locate(BestiaryMonster m)
    {
        string zone = GameData.Embedded.Families.Values.First(f => f.Monsters.Any(x => x.Id == m.Id)).Zone;
        LocatePressed?.Invoke(zone, m.Known == Knowledge.Unknown ? [] : BestiaryPage.MapsOf(GameData.Embedded, m.Id));
    }

    /// <summary>Presses the card's magnifier (for the self-test).</summary>
    public void PressLocate()
    {
        if (Chosen is string id && _page.SelectMany(f => f.Monsters).FirstOrDefault(m => m.Id == id) is BestiaryMonster m)
            Locate(m);
    }

    /// <summary>Types a search (for the self-test), as the field would.</summary>
    public void Search(string words)
    {
        _search.Text = words;
        Changed();
    }

    /// <summary>A row's portrait, in a golden frame when <paramref name="gold"/>; counted among <see cref="Framed"/>.</summary>
    private Control Framing(Control picture, bool gold, int corner)
    {
        if (!gold)
            return picture;
        var frame = new PanelContainer { MouseFilter = Control.MouseFilterEnum.Ignore, SizeFlagsVertical = Control.SizeFlags.ShrinkCenter };
        frame.AddThemeStyleboxOverride("panel", OsmoseTheme.Flat("#00000000", UiStyle.Gold, 2, corner));
        frame.AddChild(picture);
        _rowsFramed++;
        return frame;
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

    /// <summary>A tile of loot, its rate in its tooltip; <paramref name="pressable"/>, a click opens the item's card.</summary>
    private Button LootTile(BestiaryLoot loot, float size, bool pressable = true)
    {
        Item item = GameData.Embedded.Items[loot.Item];
        (Button tile, ItemIcon icon) = ItemIcon.Tile(size);
        (icon.Item, icon.Count) = (item, loot.Count);
        tile.TooltipText = ItemMenu.Tooltip(item, _texts) + $"\n{loot.Percent} %" + (loot.Quest is string quest && GameData.Embedded.Quests.TryGetValue(quest, out Quest? q) ? $" · {_texts["bestiary.for-quest"]} « {q.Name.In(_texts.Lang)} »" : "")
            + (pressable ? "\n" + _texts["bestiary.open-item"] : "");
        if (pressable)
            tile.Pressed += () => ItemPressed?.Invoke(item);
        else
            tile.MouseFilter = Control.MouseFilterEnum.Pass;
        return tile;
    }

    /// <summary>A monster's portrait rendered from its model (<see cref="MonsterPortraits"/>), its silhouette while unknown.</summary>
    private TextureRect Still(BestiaryMonster m, float size)
    {
        string path = "res://" + MonsterPortraits.Picture(m.Id, m.Known);
        TextureRect picture = Picture(path, size);
        Pictures.Add((path, picture.Texture is not null));
        return picture;
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
