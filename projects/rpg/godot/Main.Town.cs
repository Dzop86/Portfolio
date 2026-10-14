using Godot;
using Rpg.Client;
using Rpg.Core;

namespace Rpg.Desktop;

/// <summary>The town between the login screen and the fights: walking, talking, the gate.</summary>
public partial class Main
{
    private const string Village = "clairval";

    // The zone the player is in, and where they stood, for the way back from a fight when no server keeps it.
    private static string _zone = Village;
    private static Cell? _place;
    private ExploreCamera? _explore;
    private WorldMapPanel? _mapPanel;

    private TownController? _town;
    private TownView? _townView;
    private TalkPanel? _talkPanel;
    private PointsPanel? _pointsPanel;
    private InventoryPanel? _inventoryPanel;
    private SideBar? _bar;
    private bool _walking;

    private void ShowTown()
    {
        _screen = Screen.Town;
        // The server's place first (its zone included), then the one kept in memory.
        if (_character?.Place is Place p && GameData.Embedded.Towns.ContainsKey(p.Town))
            (_zone, _place) = (p.Town, p.Cell);
        _town = new TownController(GameData.Embedded, _zone, _place, Presence.Away(DateTimeOffset.UtcNow));
        _roamAt.Clear();
        _townView = new TownView();
        AddChild(_townView);
        _townView.Build(_town, _options.Lang, _hero ?? new Hero("", "female-d"));
        _camera = new Camera3D { Projection = Camera3D.ProjectionType.Orthogonal, Size = (float)ExploreCamera.DefaultZoom };
        AddChild(_camera);
        _explore = new ExploreCamera(_town.Position.X, _town.Position.Y);
        PlaceCamera();
        _talkPanel = new TalkPanel();
        AddChild(_talkPanel);
        _talkPanel.Build(_town, new Texts(_options.Lang));
        _talkPanel.SetProgress(_hero?.Class is null ? null : _hero.Level, _character?.Xp ?? 0);
        _talkPanel.Answered += Answer;
        _talkPanel.LanguageChanged += () =>
        {
            _townView.SetLanguage(_talkPanel.Texts.Lang);
            _bar?.SetTexts(_talkPanel.Texts);
            _menuPanel?.SetTexts(_talkPanel.Texts);
        };
        ShowBar();
        _townView.Footstep += () => _sound?.Step();
        _sound?.PlayMusic();
        if (_options.TownSelfTest || _options.LobbySelfTest)
            Callable.From(() => { _ = RunTownSelfTest(); }).CallDeferred();
        else if (_options.Shot is "town" or "banner" or "menu" or "inventory" or "points" or "spells" or "zone" or "world")
            _ = TownScreenshot();
    }

    /// <summary>The camera where <see cref="ExploreCamera"/> says: looking down at 30°, turned by its quarter, from afar (orthographic).</summary>
    private void PlaceCamera()
    {
        ExploreCamera cam = _explore!;
        _camera.Size = (float)cam.Zoom;
        _camera.RotationDegrees = new Vector3(-30, (float)cam.Yaw, 0);
        _camera.Position = new Vector3((float)cam.X, 0, (float)cam.Z) + _camera.Transform.Basis.Z * 30f;
    }

    /// <summary>The groups of monsters beaten in this session, away for a while (sprint 72).</summary>
    private static readonly MonsterPresence Presence = new();

    // When each group of the map moves next, in seconds from now; the draws of their walks.
    private readonly Dictionary<string, double> _roamAt = [];
    private readonly Rng _roam = new((ulong)System.Environment.TickCount64);

    /// <summary>A group walks this many cells a second.</summary>
    private const double GroupSpeed = 1.6;

    /// <summary>
    /// Each frame on a map: every few seconds, each group of monsters walks a few steps around its
    /// home (<see cref="Roaming"/>), but for the one the player is walking up to. Still during the self-tests.
    /// </summary>
    private void RoamGroups(double delta)
    {
        if (_town is null || _townView is null || _options.TownSelfTest || _options.LobbySelfTest || _options.Screenshot is not null)
            return;
        foreach (string id in _town.Groups.Keys.ToList())
        {
            double left = _roamAt.TryGetValue(id, out double t) ? t - delta : 2 + _roam.Next(0, 4000) / 1000.0;
            _roamAt[id] = left;
            if (left > 0)
                continue;
            _roamAt[id] = 4 + _roam.Next(0, 5000) / 1000.0;
            if (_town.FightGroup?.Id == id || Roaming.Next(_town, id, _roam) is not { Count: > 0 } path)
                continue;
            _town.MoveGroup(id, path[^1]);
            _ = _townView.MoveGroup(id, path, GroupSpeed);
        }
    }

    /// <summary>Each frame in a zone: the camera follows the player as drawn.</summary>
    private void FollowPlayer(double delta)
    {
        if (_explore is null || _townView is null)
            return;
        Vector3 at = _townView.Player.Position;
        _explore.Follow(at.X, at.Z, delta);
        PlaceCamera();
    }

    private void ToggleMap()
    {
        if (_mapPanel is not null)
        {
            _mapPanel.QueueFree();
            _mapPanel = null;
            return;
        }
        _mapPanel = new WorldMapPanel();
        AddChild(_mapPanel);
        _mapPanel.Build(GameData.Embedded, _zone, _town!.Position, _talkPanel!.Texts);
    }

    /// <summary>The side bar, the character's icons only when there is a character (built again when the hero changes).</summary>
    private void ShowBar()
    {
        _bar?.QueueFree();
        _bar = new SideBar();
        AddChild(_bar);
        _bar.Build(_talkPanel!.Texts, _hero?.Class is not null);
        _bar.WindowPressed += ToggleWindow;
        _bar.MenuPressed += () => Escape(menuOnly: true);
    }

    /// <summary>A window's icon or key: opens it, or closes it when it is the open one (sprint 69).</summary>
    private void ToggleWindow(GameWindow window)
    {
        if (window != GameWindow.Map && _hero?.Class is null)
            return;
        _windows.Toggle(window);
        ShowWindows();
    }

    /// <summary>
    /// The windows as <see cref="WindowState"/> says: the one open built (the characteristics and the
    /// spells each alone), the others closed, its icon lit.
    /// </summary>
    private void ShowWindows()
    {
        GameWindow? open = _windows.Open;
        bool before = _mapPanel is not null || _pointsPanel is not null || _inventoryPanel is not null;
        if (open != GameWindow.Map && _mapPanel is not null)
            ToggleMap();
        bool? spells = open switch { GameWindow.Spells => true, GameWindow.Characteristics => false, _ => null };
        if (_pointsPanel is not null && _pointsPanel.Only != spells)
            ClosePoints();
        if (open != GameWindow.Inventory && _inventoryPanel is not null)
            CloseInventory();
        switch (open)
        {
            case GameWindow.Map when _mapPanel is null:
                ToggleMap();
                break;
            case GameWindow.Characteristics or GameWindow.Spells when _pointsPanel is null:
                OpenPoints(spells);
                break;
            case GameWindow.Inventory when _inventoryPanel is null:
                OpenInventory(_character?.Inventory ?? []);
                break;
        }
        _bar?.Show(open);
        if (before || open is not null)
            _sound?.Play(open is null ? "close" : "open");
    }

    /// <summary>Through a way: the next zone is built, the player on its arrival, the place saved there.</summary>
    private async Task TravelTo(ZoneLink link)
    {
        (_zone, _place) = (link.To, link.Arrival);
        _character = _character is null ? null : _character with { Place = new Place(link.To, link.Arrival.X, link.Arrival.Y) };
        ClearScene();
        ShowTown();
        await SavePlace();
    }

    private void TownInput(InputEvent @event)
    {
        if (_town is null || _townView is null)
            return;
        if (@event is InputEventKey { Pressed: true, Echo: false, Keycode: Key.Escape })
        {
            Escape(menuOnly: false);
            return;
        }
        if (_windows.MenuOpen)
            return;
        // The camera and the windows answer even during a walk.
        switch (@event)
        {
            case InputEventMouseButton { Pressed: true, ButtonIndex: MouseButton.WheelUp }:
                _explore?.ZoomBy(1);
                return;
            case InputEventMouseButton { Pressed: true, ButtonIndex: MouseButton.WheelDown }:
                _explore?.ZoomBy(-1);
                return;
            case InputEventKey { Pressed: true, Echo: false, Keycode: Key.Q }:
                _explore?.Turn(-1);
                return;
            case InputEventKey { Pressed: true, Echo: false, Keycode: Key.E }:
                _explore?.Turn(1);
                return;
            case InputEventKey { Pressed: true, Echo: false, CtrlPressed: false, AltPressed: false } key when WindowState.ForKey((char)key.Keycode) is GameWindow window:
                ToggleWindow(window);
                return;
        }
        if (_windows.Open is not null)
            return;
        // Free walking (sprint 69): no cells shown under the mouse, a new click turns the walk.
        switch (@event)
        {
            case InputEventMouseButton { Pressed: true, ButtonIndex: MouseButton.Left } click when BoardView.Pick(_camera, click.Position) is Cell cell:
                _ = Walk(cell);
                break;
            // A right click on monsters: their menu, "Attack" in it (sprint 78, D64).
            case InputEventMouseButton { Pressed: true, ButtonIndex: MouseButton.Right } click when BoardView.Pick(_camera, click.Position) is Cell cell && _town.GroupAt(cell) is not null:
                OpenGroupMenu(cell, click.Position);
                break;
            case InputEventKey { Pressed: true, Echo: false } key when _town.Talk is not null && key.Keycode >= Key.Key1 && key.Keycode <= Key.Key4:
                int index = (int)(key.Keycode - Key.Key1);
                if (index < _town.Talk.Line.Answers.Count)
                    Answer(index);
                break;
        }
    }

    /// <summary>The player's walking pace, in cells a second.</summary>
    private const double WalkSpeed = 4.2;

    private PopupMenu? _groupMenu;

    /// <summary>
    /// The menu of a group of monsters, where the right click was: who they are (not a choice), then
    /// "Attack", which walks up to them and starts the fight.
    /// </summary>
    private PopupMenu OpenGroupMenu(Cell cell, Vector2 at)
    {
        _groupMenu?.QueueFree();
        MonsterGroup group = _town!.GroupAt(cell)!;
        Texts texts = _talkPanel!.Texts;
        _groupMenu = new PopupMenu();
        _groupMenu.AddItem(texts.GroupLabel(group, GameData.Embedded), 0);
        _groupMenu.SetItemDisabled(0, true);
        _groupMenu.AddSeparator();
        _groupMenu.AddItem(texts["group.attack"], 1);
        _groupMenu.IdPressed += id =>
        {
            if (id == 1)
                _ = Walk(_town!.Groups.TryGetValue(group.Id, out Cell now) ? now : cell, attack: true);
        };
        AddChild(_groupMenu);
        _groupMenu.Position = (Vector2I)at;
        _groupMenu.Popup();
        return _groupMenu;
    }

    private async Task Walk(Cell target, bool attack = false)
    {
        if (_walking)
        {
            // Mid-walk: the player stops where they are drawn, then walks on from there.
            Cell here = _townView!.Standing;
            _townView.Stop();
            _town!.Interrupt(here);
            _walking = false;
        }
        if ((attack ? _town!.Attack(target) : _town!.Click(target)) is not TownStep step)
            return;
        _walking = true;
        if (!await _townView!.Walk(step, WalkSpeed))
            return;
        _walking = false;
        _talkPanel!.Refresh();
        await SavePlace();
        if (_town.Fight is string scenario)
            EnterFight(scenario);
        else if (_town.Travel is ZoneLink link)
            await TravelTo(link);
    }

    /// <summary>The characteristics window, or the spells window, over the town; both tabs when <paramref name="spells"/> is null.</summary>
    private void OpenPoints(bool? spells = null)
    {
        if (_pointsPanel is not null || _hero is null)
            return;
        _pointsPanel = new PointsPanel();
        AddChild(_pointsPanel);
        _pointsPanel.Build(new PointsEditor(_hero, GameData.Embedded), _talkPanel!.Texts, spells);
        _pointsPanel.SavePressed += p => _ = SavePoints(p);
        _pointsPanel.ClosePressed += () =>
        {
            _windows.Close();
            ShowWindows();
        };
    }

    private void ClosePoints()
    {
        _pointsPanel?.QueueFree();
        _pointsPanel = null;
    }

    private void CloseInventory()
    {
        _inventoryPanel?.QueueFree();
        _inventoryPanel = null;
    }

    /// <summary>The inventory and equipment screen, over the town.</summary>
    private void OpenInventory(IReadOnlyList<ItemCount> owned)
    {
        if (_inventoryPanel is not null || _hero is null)
            return;
        _inventoryPanel = new InventoryPanel();
        AddChild(_inventoryPanel);
        _inventoryPanel.Build(new InventoryEditor(_hero, owned, GameData.Embedded), _talkPanel!.Texts);
        _inventoryPanel.SavePressed += worn => _ = SaveEquipment(worn);
        _inventoryPanel.ClosePressed += () =>
        {
            _windows.Close();
            ShowWindows();
        };
    }

    /// <summary>Saves what the hero wears on the server (which checks it owns it); offline, for this game only.</summary>
    private async Task SaveEquipment(IReadOnlyDictionary<Slot, string> worn)
    {
        InventoryPanel panel = _inventoryPanel!;
        if (_lobby?.SignedIn != true || _character is null)
        {
            _hero = panel.Editor.Draft;
            panel.Saved(_hero, panel.Editor.Owned, panel.Texts["points.offline"]);
            return;
        }
        try
        {
            _character = await _lobby.Server.SaveEquipment(_character.Id, worn);
            _hero = _character.Hero;
            panel.Saved(_hero, _character.Inventory ?? [], panel.Texts["inventory.saved"]);
        }
        catch (ServerException e)
        {
            panel.ShowMessage(e.Message);
        }
        catch (HttpRequestException)
        {
            panel.ShowMessage(panel.Texts["lobby.unreachable"]);
        }
    }

    /// <summary>Saves the points on the server (which checks them); offline, for this game only.</summary>
    private async Task<bool> SavePoints(Points points)
    {
        PointsPanel panel = _pointsPanel!;
        if (_lobby?.SignedIn != true || _character is null)
        {
            _hero = panel.Editor.Draft;
            panel.Saved(_hero, panel.Texts["points.offline"]);
            return false;
        }
        try
        {
            _character = await _lobby.Server.SavePoints(_character.Id, points);
            _hero = _character.Hero;
            panel.Saved(_hero, panel.Texts["points.saved"]);
            return true;
        }
        catch (ServerException e)
        {
            panel.ShowMessage(e.Message);
        }
        catch (HttpRequestException)
        {
            panel.ShowMessage(panel.Texts["lobby.unreachable"]);
        }
        return false;
    }

    private void Answer(int index)
    {
        _town!.Answer(index);
        _talkPanel!.Refresh();
        if (_town.Fight is string scenario)
            EnterFight(scenario);
    }

    /// <summary>Keeps where the player stands: on the server when signed in, in memory otherwise.</summary>
    private async Task<bool> SavePlace()
    {
        _place = _town!.Position;
        if (_lobby?.SignedIn != true || _character is null)
            return false;
        var place = new Place(_zone, _town.Position.X, _town.Position.Y);
        try
        {
            await _lobby.Server.SavePlace(_character.Id, place);
            _character = _character with { Place = place };
            _talkPanel?.ShowMessage(_talkPanel.Texts["town.saved"]);
            return true;
        }
        catch (Exception e) when (e is ServerException or HttpRequestException or TaskCanceledException)
        {
            // Offline or signed out meanwhile: the town goes on, the place stays in memory.
            _talkPanel?.ShowMessage(_talkPanel.Texts["lobby.unreachable"]);
            return false;
        }
    }

    private void EnterFight(string scenario) => _ = EnterFightAsync(scenario);

    /// <summary>Signed in, the server draws the fight's seed first: only that fight earns experience.</summary>
    private async Task EnterFightAsync(string scenario)
    {
        _place = _town!.Position;
        _scenario = scenario;
        _screen = Screen.Fight;
        _pendingTicket = await RequestTicket(scenario);
        ClearScene();
        StartFight(_hero, scenario);
    }

    /// <summary>
    /// The town tour (Rpg.Client.TownTour) through the views: after each click the player stands where
    /// the town says, and each line of a talk is the one on screen with its answers; then, signed in,
    /// the place is saved on the server and read back; then the gate's fight and its self-test.
    /// </summary>
    private async Task RunTownSelfTest()
    {
        var problems = new List<string>();
        string lang = _options.Lang;
        int talks = 0;
        await CheckFreeWalk(problems);
        int lines = TownTour.Run(_town!, step =>
        {
            if (!_townView!.Walk(step, 0).IsCompleted)
                problems.Add("an instant walk waited");
            if (_townView.Player.Position != BoardView.ToWorld(_town!.Position))
                problems.Add($"the player is drawn at {_townView.Player.Position}, stands on {_town.Position}");
            if (step.TalkTo is not null)
                talks++;
            foreach (Npc npc in _town.Town.Npcs)
            {
                if (_townView.NameShown(npc.Id) != npc.At.DistanceTo(_town.Position) <= TownView.NameDistance)
                    problems.Add($"{npc.Id}'s name is {(_townView.NameShown(npc.Id) ? "shown" : "hidden")} {npc.At.DistanceTo(_town.Position)} steps away");
            }
        }, talk =>
        {
            _talkPanel!.Refresh();
            if (_talkPanel.SpeakerText != talk.Npc.Name.In(lang) || _talkPanel.LineText != talk.Line.Text.In(lang))
                problems.Add($"the talk shows '{_talkPanel.LineText}', not line {talk.LineId} of {talk.Npc.Id}");
            if (_talkPanel.AnswersText.Count() != talk.Line.Answers.Count)
                problems.Add($"line {talk.LineId} shows {_talkPanel.AnswersText.Count()} answers");
        });
        // The characteristics and spells screen shows the hero's points, and gives none it has not.
        if (_hero?.Class is not null)
        {
            OpenPoints();
            PointsEditor editor = _pointsPanel!.Editor;
            if (!_pointsPanel.StatsText.SequenceEqual(Enum.GetValues<Characteristic>().Select(c => editor[c].ToString(System.Globalization.CultureInfo.InvariantCulture))))
                problems.Add("the characteristics screen does not show the hero's");
            if (_hero.Level == 1 && (editor.CharacteristicPointsLeft != 0 || editor.SpellPointsLeft != 0 || editor.Changed))
                problems.Add("a first-level hero has points to spend");
            // The spells tab shows every spell of the class, the locked ones too.
            _pointsPanel.ShowTab(spells: true);
            if (_pointsPanel.SpellTiles != (GameData.Embedded.Class(_hero.Class)?.Spells.Count ?? 0))
                problems.Add($"the spells tab shows {_pointsPanel.SpellTiles} spells");
            _pointsPanel.QueueFree();
            _pointsPanel = null;
            // The inventory screen: fourteen slots, what is worn in them, the items of each page.
            OpenInventory(_character?.Inventory ?? []);
            InventoryPanel inv = _inventoryPanel!;
            if (inv.SlotsText.Count() != 14 || inv.SlotsText.Zip(Enum.GetValues<Slot>()).Any(z => !z.First.StartsWith(inv.Texts["slot." + z.Second], StringComparison.Ordinal)))
                problems.Add("the equipment screen does not show the fourteen slots");
            foreach (InventoryPage page in Enum.GetValues<InventoryPage>())
            {
                inv.ShowPage(page);
                int listed = inv.Editor.Page(page).Count;
                if (inv.ListText.Count() != listed)
                    problems.Add($"the {page} page lists {inv.ListText.Count()} items, not {listed}");
            }
            inv.QueueFree();
            _inventoryPanel = null;
        }
        string note = $"Town: {talks} people, {lines} lines";
        if (_lobby?.SignedIn == true && _character is not null)
        {
            bool saved = await SavePlace();
            Place? back = (await _lobby.Server.Characters()).Single(c => c.Id == _character.Id).Place;
            if (!saved || back != new Place(_zone, _town!.Position.X, _town.Position.Y))
                problems.Add($"the server keeps the place {back}, not {_town!.Position}");
            else
                note += $", place {_town.Position} saved on the server";
        }
        string world = CheckWorld(problems) + " " + CheckBarAndMenu(problems);
        note += problems.Count == 0 ? ", views checked." : $". TOWN PROBLEMS: {string.Join("; ", problems.Distinct().Take(5))}";
        if (problems.Count == 0)
            note += " " + world;
        _selfTestNote = _selfTestNote is null ? note : $"{_selfTestNote} {note}";
        if (problems.Count > 0 || _town!.Fight is not string scenario)
        {
            string report = $"SELFTEST FAILED {note}";
            GD.Print(report);
            if (_options.Report is string path)
                File.WriteAllText(path, report + "\n");
            GetTree().Quit(1);
            return;
        }
        EnterFight(scenario);
    }

    /// <summary>
    /// Free walking (sprint 69): a walk goes in straight lines of eight directions, shorter than on
    /// four; a new click mid-walk stops the first where the player is drawn, on a free cell. The player
    /// is then put back where they started, for the town tour.
    /// </summary>
    private async Task CheckFreeWalk(List<string> problems)
    {
        TownController town = _town!;
        Cell start = town.Position;
        Cell far = Enumerable.Range(0, town.Board.Width).SelectMany(x => Enumerable.Range(0, town.Board.Height).Select(y => new Cell(x, y)))
            .Where(c => town.Town.IsFree(town.Board, c) && town.Town.NpcAt(c) is null && town.PathTo(c) is not null)
            .MaxBy(c => Wander.Distance(start, c));
        TownStep step = town.Click(far)!;
        if (step.Waypoints.Count > step.Path.Count || step.Waypoints[^1] != far || Wander.Length(start, step.Waypoints) > Wander.Length(start, step.Path) + 1e-9)
            problems.Add($"the walk to {far} is {step.Waypoints.Count} straight lines for {step.Path.Count} cells");
        Task<bool> first = _townView!.Walk(step, WalkSpeed);
        for (int i = 0; i < 6; i++)
            await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
        Cell here = _townView.Standing;
        _townView.Stop();
        town.Interrupt(here);
        await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
        if (!first.IsCompleted || first.Result || here == far || _townView.Player.Position.DistanceTo(BoardView.ToWorld(here)) > 0.71f)
            problems.Add($"a walk cut short at {here} went on, or the player is drawn at {_townView.Player.Position}");
        town.Interrupt(start);
        _townView.Player.Position = BoardView.ToWorld(start);
    }

    /// <summary>
    /// The side bar and the menu (sprint 69): each icon names its window and key; each key opens its
    /// window alone and lit, and closes it; Escape closes the window, then opens the menu, whose
    /// sliders set the buses and the file; every sound of the game is there.
    /// </summary>
    private string CheckBarAndMenu(List<string> problems)
    {
        Texts t = _talkPanel!.Texts;
        bool character = _hero?.Class is not null;
        var tips = WindowState.Bar.Where(b => character || b.Window == GameWindow.Map).Select(b => t["bar.tip", t["bar." + b.Window], b.Key]).Append(t["bar.tip", t["bar.menu"], t["menu.key"]]);
        if (!_bar!.Tips.SequenceEqual(tips))
            problems.Add($"the side bar says {string.Join(", ", _bar.Tips)}");
        int windows = 0;
        foreach ((GameWindow window, char key) in WindowState.Bar.Where(b => character || b.Window == GameWindow.Map))
        {
            TownInput(new InputEventKey { Pressed = true, Keycode = (Key)key });
            bool shown = window switch
            {
                GameWindow.Map => _mapPanel is not null && _pointsPanel is null && _inventoryPanel is null,
                GameWindow.Characteristics => _pointsPanel?.Only == false && _mapPanel is null,
                GameWindow.Spells => _pointsPanel?.Only == true && _mapPanel is null,
                _ => _inventoryPanel is not null && _pointsPanel is null && _mapPanel is null,
            };
            if (!shown || !_bar.Lit(window))
                problems.Add($"{key} does not open the {window} window alone");
            windows++;
        }
        TownInput(new InputEventKey { Pressed = true, Keycode = Key.Escape });
        if (_mapPanel is not null || _pointsPanel is not null || _inventoryPanel is not null || _menuPanel is not null)
            problems.Add("Escape does not close the window");
        TownInput(new InputEventKey { Pressed = true, Keycode = Key.M });
        TownInput(new InputEventKey { Pressed = true, Keycode = Key.M });
        if (_mapPanel is not null)
            problems.Add("M again does not close the map");
        TownInput(new InputEventKey { Pressed = true, Keycode = Key.Escape });
        if (_menuPanel is null)
            problems.Add("Escape does not open the menu");
        else
        {
            int choices = _lobby?.SignedIn == true ? 4 : 2;
            if (_menuPanel.ChoicesText.Count() != choices)
                problems.Add($"the menu offers {string.Join(", ", _menuPanel.ChoicesText)}");
            AudioSettings before = Sound.Settings;
            _menuPanel.SetVolume("music", 40);
            if (Sound.Settings.Music != 40 || Math.Abs(AudioServer.GetBusVolumeDb(AudioServer.GetBusIndex(Sound.MusicBus)) - AudioSettings.Decibels(40)) > 0.01)
                problems.Add("the music slider does not set the music bus");
            _menuPanel.SetMuted(true);
            if (AudioServer.GetBusVolumeDb(0) > -79)
                problems.Add("the sound is not cut");
            using (var file = Godot.FileAccess.Open("user://" + AudioSettings.FileName, Godot.FileAccess.ModeFlags.Read))
            {
                if (AudioSettings.Parse(file?.GetAsText()) != Sound.Settings)
                    problems.Add("the settings file does not keep the sound");
            }
            Sound.Apply(before);
            // Keys do nothing behind the menu.
            TownInput(new InputEventKey { Pressed = true, Keycode = Key.I });
            if (_inventoryPanel is not null || _mapPanel is not null)
                problems.Add("a window opens behind the menu");
            TownInput(new InputEventKey { Pressed = true, Keycode = Key.Escape });
            if (_menuPanel is not null)
                problems.Add("Escape does not close the menu");
        }
        string[] effects = ["step-1", "step-2", "step-3", "step-4", "hit", "cast", "open", "close", "click", "error", "victory", "defeat"];
        string[] missing = [.. effects.Where(e => !ResourceLoader.Exists($"res://assets/kenney/sounds/{e}.ogg"))];
        if (missing.Length > 0 || !ResourceLoader.Exists("res://assets/music/village.wav"))
            problems.Add($"sounds missing: {string.Join(", ", missing)}");
        return $"Bar: {windows} window(s), menu and sound checked.";
    }

    /// <summary>
    /// The self-test of sprint 60: every zone reached by the world tour is drawn whole with its ways;
    /// the camera keeps the player in the middle of the screen at every quarter turn and zoom; the map
    /// shows every zone, the current one marked.
    /// </summary>
    private string CheckWorld(List<string> problems)
    {
        Hero hero = _hero ?? new Hero("", "female-d");
        IReadOnlyList<string> zones = TownTour.World(new TownController(GameData.Embedded, Village));
        int groups = 0;
        foreach (string id in zones)
        {
            var controller = new TownController(GameData.Embedded, id);
            groups += controller.Groups.Count;
            // Maps of 48 x 32 since sprint 77: the village and the middle map of each zone are drawn, the
            // others are toured above without views (drawing a hundred of them took minutes).
            if (id != Village && !id.EndsWith("-2-2", StringComparison.Ordinal))
                continue;
            var view = new TownView();
            AddChild(view);
            view.Build(controller, _options.Lang, hero);
            Town t = controller.Town;
            if (view.Tiles != t.Rows.Count * t.Rows[0].Length || view.WayLabels != t.Exits.Count + (t.Links ?? []).Where(l => l.Name is not null).Select(l => l.To).Distinct().Count())
                problems.Add($"{id} is drawn with {view.Tiles} tiles and {view.WayLabels} names of ways");
            // Its groups of monsters, each with its monsters on its cell (sprint 72).
            foreach ((string g, Cell at) in controller.Groups)
            {
                int models = view.Groups.TryGetValue(g, out Node3D? node) ? node.GetChildren().OfType<Node3D>().Count(n => n.FindChild("AnimationPlayer", true, false) is not null) : 0;
                // Three at most on a cell; the label tells how many more.
                if (models != Math.Min(3, GameData.Embedded.Groups[g].Monsters.Count) || node!.Position != BoardView.ToWorld(at))
                    problems.Add($"the group {g} is drawn with {models} monsters");
            }
            RemoveChild(view);
            view.QueueFree();
        }
        Vector2 middle = GetViewport().GetVisibleRect().Size / 2;
        Vector3 player = _townView!.Player.Position;
        for (int quarter = 0; quarter < 4; quarter++)
        {
            foreach (int zoom in new[] { 0, 3 })
            {
                _explore!.ZoomBy(zoom);
                _explore.Jump(player.X, player.Z);
                PlaceCamera();
                if (_camera.UnprojectPosition(player).DistanceTo(middle) > 2)
                    problems.Add($"turned {quarter} and zoomed {_explore.Zoom:0.0}, the camera shows the player at {_camera.UnprojectPosition(player)}");
                _explore.ZoomBy(-zoom);
            }
            _explore!.Turn(1);
        }
        ToggleMap();
        CheckWorldMap(problems);
        ToggleMap();
        CheckMonsters(problems);
        return $"World: {zones.Count} maps, {groups} groups of monsters, camera and map checked.";
    }

    /// <summary>
    /// The monsters of sprint 72: every model of every family has its two animations; a left click on a
    /// group only walks up to it, "Attack" in its menu (a right click, sprint 78) chooses its fight, a fight whose fighters the board can draw; a
    /// group roams a few steps and is drawn there.
    /// </summary>
    private void CheckMonsters(List<string> problems)
    {
        foreach (MonsterSpec m in GameData.Embedded.Monsters.Values)
        {
            Node3D model = Looks.Model(m.Look);
            if (model.FindChild("AnimationPlayer", true, false) is not AnimationPlayer anim || !anim.HasAnimation("idle") || !anim.HasAnimation("walk"))
                problems.Add($"the model of {m.Id} has no idle or walk");
            model.Free();
        }
        MonsterGroup group = GameData.Embedded.GroupsOn("woods-2-2")[0];
        var woods = new TownController(GameData.Embedded, "woods-2-2");
        var view = new TownView();
        AddChild(view);
        view.Build(woods, _options.Lang, _hero ?? new Hero("", "female-d"));
        if (woods.Click(group.Home) is not { Group: null } || woods.Fight is not null)
            problems.Add($"a left click on the group {group.Id} starts a fight");
        woods.Interrupt(woods.Position);
        if (woods.Attack(group.Home) is not { Group: not null } || woods.Fight != group.Scenario)
            problems.Add($"attacking the group {group.Id} does not start its fight");
        // Its menu, on a right click: who they are, then "Attack".
        var town = _town;
        _town = new TownController(GameData.Embedded, "woods-2-2");
        PopupMenu menu = OpenGroupMenu(group.Home, new Vector2(100, 100));
        if (menu.ItemCount != 3 || menu.GetItemText(0) != _talkPanel!.Texts.GroupLabel(group, GameData.Embedded) || !menu.IsItemDisabled(0) || menu.GetItemText(2) != _talkPanel.Texts["group.attack"])
            problems.Add($"the menu of the group {group.Id} offers {menu.ItemCount} lines");
        menu.QueueFree();
        _groupMenu = null;
        _town = town;
        var fight = new Fight(GameData.Embedded, group.Scenario, 1);
        foreach (Fighter f in fight.Fighters)
        {
            var fv = new FighterView();
            AddChild(fv);
            fv.Setup(f, f.Team == 0);
            RemoveChild(fv);
            fv.QueueFree();
        }
        var stay = new TownController(GameData.Embedded, "woods-2-2");
        if (Roaming.Next(stay, group.Id, new Rng(3)) is { Count: > 0 } path)
        {
            stay.MoveGroup(group.Id, path[^1]);
            view.PlaceGroup(group.Id, path[^1]);
            if (view.Groups[group.Id].Position != BoardView.ToWorld(path[^1]))
                problems.Add($"the group {group.Id} is not drawn where it roamed");
        }
        else
            problems.Add($"the group {group.Id} cannot roam");
        RemoveChild(view);
        view.QueueFree();
    }

    /// <summary>
    /// The world map of sprint 71: the picture of the whole world, the player's ring on their cell, the
    /// map under it named; zooming keeps the point under the pointer still, between the whole world and
    /// four times; the picture cannot be dragged away.
    /// </summary>
    private void CheckWorldMap(List<string> problems)
    {
        WorldMapPanel map = _mapPanel!;
        var world = new WorldPicture(GameData.Embedded);
        (double px, double py) = world.PixelOf(_zone, _town!.Position);
        Vector2 expected = map.PictureAt + new Vector2((float)px, (float)py) * map.Zoom;
        if (map.PictureSize != new Vector2I(world.Width, world.Height) || map.Current != _zone || map.MarkerCentre.DistanceTo(expected) > 0.5f)
            problems.Add($"the world map is {map.PictureSize}, marks {map.Current} at {map.MarkerCentre} (expected {expected})");
        string here = _talkPanel!.Texts.MapTitle(_town.Town);
        if (!map.HoverText.StartsWith(here, StringComparison.Ordinal) || map.HoverAt(map.MarkerCentre) != map.HoverText)
            problems.Add($"the world map names \"{map.HoverText}\" under the player");
        var pointer = new Vector2(300, 200);
        Vector2 under = (pointer - map.PictureAt) / map.Zoom;
        // Zooming in (zooming out may meet an edge of the world, and the picture is kept in the view).
        map.ZoomAround(pointer, 1.25f);
        if (((pointer - map.PictureAt) / map.Zoom).DistanceTo(under) > 0.5f)
            problems.Add("zooming the world map moves the point under the pointer");
        map.ZoomAround(pointer, 0.01f);
        map.Drag(new Vector2(5000, 5000));
        if (Math.Abs(map.Zoom - map.MinZoom) > 1e-4f || map.PictureAt.X < 0 || map.PictureAt.Y < 0)
            problems.Add($"the world map zooms out to {map.Zoom} and is dragged to {map.PictureAt}");
        map.ZoomAround(pointer, 100);
        if (Math.Abs(map.Zoom - 4) > 1e-4f)
            problems.Add($"the world map zooms in to {map.Zoom}");
    }

    /// <summary>
    /// The town for the project page: the player has walked up to Aubin, who talks; or, for the
    /// launcher's banner, the village alone, without the screen's texts; or a zone of the world, its map open or not.
    /// </summary>
    private async Task TownScreenshot()
    {
        if (_options.Shot is "zone" or "world")
        {
            // A zone of the world seen from the explore camera; or the world map over it.
            if (_options.Shot == "world")
                ToggleWindow(GameWindow.Map);
            _townView!.ShowPath(null);
            for (int i = 0; i < 30; i++)
                await ToSignal(RenderingServer.Singleton, RenderingServer.SignalName.FramePostDraw);
            Error picture = GetViewport().GetTexture().GetImage().SavePng(_options.Screenshot!);
            GD.Print($"SCREENSHOT {picture} {_options.Screenshot}");
            GetTree().Quit(picture == Error.Ok ? 0 : 1);
            return;
        }
        if (_options.Shot is "points" or "spells")
        {
            // The character sheet for the project page: a tenth-level guard, points spent, a few items worn.
            _hero = new Hero("Aubépine", "female-b", "guard", 5, 5, 4, -1, 1, Level: 10,
                Stats: new Characteristics(Vitality: 20, Earth: 40, Fire: 0, Water: 9, Air: 10),
                Worn: new Dictionary<Slot, string> { [Slot.Ring1] = "copper-ring", [Slot.Amulet] = "pebble-amulet", [Slot.TwoHanded] = "orc-club", [Slot.Boots] = "poacher-boots", [Slot.Pet] = "kitten" });
            _talkPanel!.SetProgress(10, 5_000);
            ShowBar();
            ToggleWindow(_options.Shot == "spells" ? GameWindow.Spells : GameWindow.Characteristics);
            _pointsPanel!.Editor.Add(Characteristic.Earth, 5);
            if (_options.Shot == "spells")
            {
                _pointsPanel.Editor.Raise(GameData.Embedded.Spells["shield-bash"]);
                _pointsPanel.Select("shield-bash");
            }
            _pointsPanel.Refresh();
            _townView!.ShowPath(null);
            for (int i = 0; i < 30; i++)
                await ToSignal(RenderingServer.Singleton, RenderingServer.SignalName.FramePostDraw);
            Error sheet = GetViewport().GetTexture().GetImage().SavePng(_options.Screenshot!);
            GD.Print($"SCREENSHOT {sheet} {_options.Screenshot}");
            GetTree().Quit(sheet == Error.Ok ? 0 : 1);
            return;
        }
        if (_options.Shot == "inventory")
        {
            // A showcase for the project page: a tenth-level guard with what training fights leave.
            _hero = new Hero("Aubépine", "female-b", "guard", 5, 5, 4, -1, 1, Level: 10);
            _talkPanel!.SetProgress(10, 5_000);
            ShowBar();
            _bar!.Show(GameWindow.Inventory);
            ItemCount[] bag = [new("copper-ring", 2), new("pebble-amulet", 1), new("poacher-hat", 1), new("poacher-cape", 1), new("poacher-boots", 1), new("dagger", 1), new("wooden-shield", 1), new("orc-club", 1), new("kitten", 1), new("bread", 3), new("healing-potion", 2), new("orc-fang", 5), new("leather", 4), new("garance-badge", 1)];
            OpenInventory(bag);
            foreach (string id in new[] { "copper-ring", "copper-ring", "pebble-amulet", "poacher-hat", "poacher-cape", "poacher-boots", "dagger", "wooden-shield", "kitten" })
                _inventoryPanel!.Editor.Put(GameData.Embedded.Items[id]);
            _inventoryPanel!.ShowPage(InventoryPage.All);
            _townView!.ShowPath(null);
            for (int i = 0; i < 30; i++)
                await ToSignal(RenderingServer.Singleton, RenderingServer.SignalName.FramePostDraw);
            Error shot = GetViewport().GetTexture().GetImage().SavePng(_options.Screenshot!);
            GD.Print($"SCREENSHOT {shot} {_options.Screenshot}");
            GetTree().Quit(shot == Error.Ok ? 0 : 1);
            return;
        }
        Npc aubin = _town!.Town.Npcs[0];
        await _townView!.Walk(_town.Click(aubin.At)!, 0);
        if (_options.Shot == "banner")
        {
            _talkPanel!.Visible = false;
            _bar!.Visible = false;
            _townView.HideExitLabels();
            _townView.HideNames();
        }
        else if (_options.Shot == "menu")
        {
            // The Escape menu over the village, a character's four icons on the side bar.
            _hero = new Hero("Aubépine", "female-b", "guard", 5, 5, 4, -1, 1, Level: 10);
            ShowBar();
            Escape(menuOnly: false);
        }
        else
        {
            _town.Answer(0);
            _talkPanel!.Refresh();
        }
        _townView.ShowPath(null);
        for (int i = 0; i < 30; i++)
            await ToSignal(RenderingServer.Singleton, RenderingServer.SignalName.FramePostDraw);
        Error saved = GetViewport().GetTexture().GetImage().SavePng(_options.Screenshot!);
        GD.Print($"SCREENSHOT {saved} {_options.Screenshot}");
        GetTree().Quit(saved == Error.Ok ? 0 : 1);
    }
}
