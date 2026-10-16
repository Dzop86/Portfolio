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

    /// <summary>The zoom chosen, for every map until changed.</summary>
    private static double _zoom = ExploreCamera.DefaultZoom;

    private void ShowTown()
    {
        _screen = Screen.Town;
        // The server's place first (its zone included), then the one kept in memory.
        if (_character?.Place is Place p && GameData.Embedded.Towns.ContainsKey(p.Town))
            (_zone, _place) = (p.Town, p.Cell);
        bool outOfHall = LeaveShutHall();
        _town = new TownController(GameData.Embedded, _zone, _place, Presence.Away(DateTimeOffset.UtcNow), Run);
        // The character's quests (sprint 88), offered and taken back in the talks; online only.
        if (_character is not null)
            _town.Book = QuestBook.Of(_character, GameData.Embedded);
        _roamAt.Clear();
        _townView = new TownView();
        _beastAt.Clear();
        AddChild(_townView);
        _townView.Build(_town, _options.Lang, _hero ?? new Hero("", "female-d"));
        _townView.ShowMarks(_town.Book);
        ShowTrail();
        _ = RefreshGrowing();
        _camera = new Camera3D { Projection = Camera3D.ProjectionType.Orthogonal, Size = (float)ExploreCamera.DefaultZoom };
        AddChild(_camera);
        // The zoom of the map left, kept (sprint 83, D66).
        _explore = new ExploreCamera(_town.Position.X, _town.Position.Y, _zoom);
        PlaceCamera();
        _talkPanel = new TalkPanel();
        AddChild(_talkPanel);
        _talkPanel.Build(_town, new Texts(_options.Lang));
        _edgeArrow = new EdgeArrow();
        _talkPanel.AddChild(_edgeArrow);
        _talkPanel.SetProgress(_hero?.Class is null ? null : _hero.Level, _character?.Xp ?? 0);
        _talkPanel.Living = _character;
        _talkPanel.Answered += Answer;
        _talkPanel.LanguageChanged += () =>
        {
            _townView.SetLanguage(_talkPanel.Texts.Lang);
            _bar?.SetTexts(_talkPanel.Texts);
            _menuPanel?.SetTexts(_talkPanel.Texts);
        };
        ShowBar();
        ShowChat();
        _townView.Footstep += () => _sound?.Step();
        _sound?.PlayMusic();
        // In a hall, the hit points left; just out of one (lost, or the last room won), there under the keep.
        if (outOfHall)
        {
            _talkPanel.ShowMessage(_talkPanel.Texts["dungeon.out"]);
            _ = SavePlace();
        }
        else if (_town.Town.IsHall)
            ShowHallHp();
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
        _camera.RotationDegrees = new Vector3(-30, (float)ExploreCamera.Yaw, 0);
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

    // When each beast of the map moves next (sprint 104), in seconds from now.
    private readonly Dictionary<int, double> _beastAt = [];

    /// <summary>A beast walks this many cells a second, slower than the monsters.</summary>
    private const double BeastSpeed = 0.9;

    /// <summary>Each frame on a map: every few seconds, each beast of the village strolls a few steps in its pasture. Still during the self-tests.</summary>
    private void RoamBeasts(double delta)
    {
        if (_townView?.Herd is not Herd herd || _options.TownSelfTest || _options.LobbySelfTest || _options.Screenshot is not null)
            return;
        for (int i = 0; i < herd.Animals.Count; i++)
        {
            double left = _beastAt.TryGetValue(i, out double t) ? t - delta : 1 + _roam.Next(0, 5000) / 1000.0;
            _beastAt[i] = left;
            if (left > 0)
                continue;
            _beastAt[i] = 3 + _roam.Next(0, 6000) / 1000.0;
            if (herd.Roam(i, _roam) is { Count: > 0 } path)
                _ = _townView.MoveBeast(i, path, BeastSpeed);
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
        _mapPanel.Build(GameData.Embedded, _zone, _town!.Position, _talkPanel!.Texts, FollowedGoal());
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
        bool before = _mapPanel is not null || _pointsPanel is not null || _inventoryPanel is not null || _journalPanel is not null || _jobsPanel is not null;
        if (open != GameWindow.Map && _mapPanel is not null)
            ToggleMap();
        bool? spells = open switch { GameWindow.Spells => true, GameWindow.Characteristics => false, _ => null };
        if (_pointsPanel is not null && _pointsPanel.Only != spells)
            ClosePoints();
        if (open != GameWindow.Inventory && _inventoryPanel is not null)
            CloseInventory();
        if (open != GameWindow.Journal && _journalPanel is not null)
            CloseJournal();
        if (open != GameWindow.Jobs && _jobsPanel is not null && _town?.Bench is null)
            CloseJobs();
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
            case GameWindow.Journal when _journalPanel is null:
                OpenJournal();
                break;
            case GameWindow.Jobs when _jobsPanel is null:
                OpenJobs();
                break;
        }
        _bar?.Show(open);
        if (before || open is not null)
            _sound?.Play(open is null ? "close" : "open");
    }

    /// <summary>
    /// A dungeon's hall the player may not be in (sprint 86): out of the dungeon (a room lost, the last one
    /// won, or left meanwhile), or past the room it is at. The player is then outside, under the keep's door.
    /// </summary>
    private static bool LeaveShutHall()
    {
        if (!GameData.Embedded.Towns.TryGetValue(_zone, out Town? hall) || hall.Dungeon is not string id)
            return false;
        Dungeon dungeon = GameData.Embedded.Dungeons[id];
        if (Run is DungeonRun run && run.Dungeon == id && dungeon.RoomOf(hall.Id) <= run.Room)
            return false;
        (_zone, _place) = (dungeon.Map, dungeon.Door + new Cell(0, 1));
        if (_character is not null)
            _character = _character with { Place = new Place(_zone, _place.Value.X, _place.Value.Y) };
        return true;
    }

    /// <summary>In a hall: the hit points the hero has for the next room.</summary>
    private void ShowHallHp()
    {
        if (_hero?.Class is null)
            return;
        int max = Life.MaxHp(_hero, GameData.Embedded);
        _talkPanel!.ShowMessage(_talkPanel.Texts["hall.hp", _character?.LifeAt(DateTimeOffset.UtcNow, GameData.Embedded) ?? max, max]);
    }

    /// <summary>At a hall's fountain (sprint 86): all the hit points back, on the server's word when signed in.</summary>
    private async Task Rest()
    {
        Texts texts = _talkPanel!.Texts;
        _town?.CloseDoor();
        if (_lobby?.SignedIn == true && _character is not null)
        {
            try
            {
                _character = await _lobby.Server.Rest(_character.Id);
            }
            catch (Exception e) when (e is ServerException or HttpRequestException)
            {
                _talkPanel.ShowMessage(e is ServerException refused ? texts["result.refused", refused.Message] : texts["lobby.unreachable"]);
                return;
            }
        }
        else if (_offlineRun is DungeonRun run)
            _offlineRun = run with { Hp = null };
        _sound?.Play("open");
        _talkPanel.ShowMessage(texts["hall.rested", _hero?.Class is null ? 0 : new CharacterSheet(_hero, GameData.Embedded).MaxHp]);
    }

    /// <summary>Through a way: the next zone is built, the player on its arrival, the place saved there.</summary>
    private async Task TravelTo(ZoneLink link)
    {
        (_zone, _place) = (link.To, link.Arrival);
        _character = _character is null ? null : _character with { Place = new Place(link.To, link.Arrival.X, link.Arrival.Y) };
        ClearScene();
        ShowTown();
        await SavePlace();
        // The new map's spots growing back, now the server knows the character is on it.
        await RefreshGrowing();
    }

    // The gathering spots of the map growing back (sprint 99), as the server says.
    private readonly GrowingSpots _growing = new();

    private async Task RefreshGrowing()
    {
        _growing.Reset(new Dictionary<string, DateTimeOffset>());
        if (_lobby?.SignedIn == true && _character is not null)
        {
            try
            {
                _growing.Reset((await _lobby.Server.Growing(_character.Id)).Spots);
            }
            catch (Exception e) when (e is ServerException or HttpRequestException)
            {
                // Drawn ready: the server refuses a spot growing back anyway.
            }
        }
        _townView?.ShowGrowing(_growing.Ids);
    }

    /// <summary>The spots whose minute is over drawn again (sprint 103), checked as the town runs.</summary>
    private void GrowBack()
    {
        if (_growing.Ready(DateTimeOffset.UtcNow).Count > 0)
            _townView?.ShowGrowing(_growing.Ids);
    }

    // A new walk cancels a gathering under way: the gathering waits for this ticket to be the last.
    private int _gatherTicket;

    /// <summary>A gathering (sprint 99): three seconds standing by the spot, then the server gives the resource and the trade's experience.</summary>
    private async Task Gather(HarvestNode node)
    {
        _town!.GatheringSeenTo();
        Texts texts = _talkPanel!.Texts;
        if (_lobby?.SignedIn != true || _character is null)
        {
            _talkPanel.ShowMessage(texts["gather.offline"]);
            return;
        }
        if (_growing.Contains(node.Id))
        {
            _talkPanel.ShowMessage(texts["gather.growing"]);
            return;
        }
        int ticket = ++_gatherTicket;
        string trade = GameData.Embedded.Jobs[node.Job].Name.In(texts.Lang);
        string doing = texts["gather.doing", trade, texts.Items([new ItemCount(node.Item, 1)])];
        // The bar fills over the gathering's three seconds (Charles, sprint 103); a new walk stops it.
        ulong start = Time.GetTicksMsec();
        double progress = 0;
        while (progress < 1)
        {
            progress = GrowingSpots.Progress(TimeSpan.FromMilliseconds(Time.GetTicksMsec() - start));
            if (ticket != _gatherTicket || _town is null)
            {
                _talkPanel.ShowGathering(null);
                return;
            }
            _talkPanel.ShowGathering(progress, doing);
            await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
        }
        _talkPanel.ShowGathering(null);
        if (ticket != _gatherTicket || _town is null)
            return;
        try
        {
            Gathered got = await _lobby.Server.Harvest(_character.Id, node.Id, _town.Position);
            KeepCharacter(got.Character);
            _growing.Taken(node.Id, DateTimeOffset.UtcNow);
            _townView?.ShowGrowing(_growing.Ids);
            _talkPanel.ShowMessage(texts["gather.done", got.Item.Count, texts.Items([got.Item with { Count = 1 }]), got.Xp, trade]);
            _sound?.Play("click");
        }
        catch (Exception e) when (e is ServerException or HttpRequestException)
        {
            _talkPanel.ShowMessage(e is ServerException refused ? texts["result.refused", refused.Message] : texts["lobby.unreachable"]);
        }
    }

    /// <summary>Walking a map (its town and its view built); otherwise in a fight, even one started from the town.</summary>
    private bool InTown => _town is not null && _townView is not null;

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
        // The camera and the windows answer even during a walk; the wheel zooms the map only with no window
        // open over it (Charles: with the spells or the inventory open, the wheel is theirs).
        bool covered = _windows.Open is not null || _shopPanel is not null || _jobsPanel is not null;
        switch (@event)
        {
            case InputEventMouseButton { ButtonIndex: MouseButton.WheelUp or MouseButton.WheelDown } when covered:
                return;
            case InputEventMouseButton { Pressed: true, ButtonIndex: MouseButton.WheelUp }:
                _explore?.ZoomBy(1);
                _zoom = _explore?.Zoom ?? _zoom;
                return;
            case InputEventMouseButton { Pressed: true, ButtonIndex: MouseButton.WheelDown }:
                _explore?.ZoomBy(-1);
                _zoom = _explore?.Zoom ?? _zoom;
                return;
            // Enter: the chat's field (sprint 84); a click on the map leaves it.
            case InputEventKey { Pressed: true, Echo: false, Keycode: Key.Enter or Key.KpEnter } when _chatBox is not null:
                _chatBox.Open();
                return;
            case InputEventMouseButton { Pressed: true } when _chatBox?.Typing == true:
                _chatBox.Leave();
                break;
            case InputEventKey { Pressed: true, Echo: false, CtrlPressed: false, AltPressed: false } key when WindowState.ForKey((char)key.Keycode) is GameWindow window:
                ToggleWindow(window);
                return;
        }
        if (_windows.Open is not null)
            return;
        // Free walking (sprint 69): no cells shown under the mouse, a new click turns the walk; near an
        // edge, an arrow to the next map (sprint 83).
        switch (@event)
        {
            case InputEventMouseMotion motion:
                ShowEdge(motion.Position);
                break;
            case InputEventMouseButton { Pressed: true, ButtonIndex: MouseButton.Left } click when (SpotUnder(click.Position) ?? BoardView.Pick(_camera, click.Position)) is Cell cell:
                _ = Walk(cell);
                break;
            // A right click on monsters: their menu, "Attack" in it (sprint 78, D64).
            case InputEventMouseButton { Pressed: true, ButtonIndex: MouseButton.Right } click when GroupUnder(click.Position) is Cell cell:
                OpenGroupMenu(cell, click.Position);
                break;
            case InputEventKey { Pressed: true, Echo: false } key when _town.Talk is not null && key.Keycode >= Key.Key1 && key.Keycode <= Key.Key9:
                int index = (int)(key.Keycode - Key.Key1);
                if (index < _town.Talk.Line.Answers.Count)
                    Answer(index);
                break;
        }
    }

    /// <summary>The player's walking pace, in cells a second: 20 % faster since sprint 83 (D66).</summary>
    private const double WalkSpeed = 5.04;

    // The proximity chat (sprint 84): one socket for the character in play, kept from map to map (the
    // server follows the place it saves), and its lines.
    private static ChatClient? _chat;
    private static Guid? _chatOf;
    private static ChatLog? _chatLog;
    private ChatBox? _chatBox;

    private void ShowChat()
    {
        Texts texts = _talkPanel!.Texts;
        _chatLog ??= new ChatLog(texts);
        _chatBox = new ChatBox();
        AddChild(_chatBox);
        _chatBox.Build(_chatLog, texts);
        _chatBox.Said += line => _ = Say(line);
        if (_lobby?.SignedIn != true || _character is null)
        {
            if (_chatLog.Lines.Count == 0)
                _chatLog.Note(texts["chat.offline"]);
            _chatBox.Refresh();
        }
        else if (_chat is null || !_chat.IsOpen || _chatOf != _character.Id)
            _ = OpenChat(_character.Id);
    }

    private async Task OpenChat(Guid character)
    {
        Texts texts = _talkPanel?.Texts ?? new Texts(_options.Lang);
        if (_chat is not null)
            await _chat.DisposeAsync();
        _chat = null;
        try
        {
            _chat = await _lobby!.Server.OpenChat(character);
            _chatOf = character;
            _chatLog!.Note(texts["chat.open"]);
        }
        catch (Exception e) when (e is System.Net.WebSockets.WebSocketException or HttpRequestException or InvalidOperationException)
        {
            _chatLog!.Note(texts["chat.closed"]);
        }
        _chatBox?.Refresh();
    }

    private async Task Say(string line)
    {
        if (_chat is null || !_chat.IsOpen)
        {
            _chatLog!.Note(_talkPanel!.Texts[_lobby?.SignedIn == true ? "chat.closed" : "chat.offline"]);
            _chatBox?.Refresh();
            return;
        }
        await _chat.Say(line);
    }

    /// <summary>What the chat brought since the last frame, on screen.</summary>
    private void DrainChat()
    {
        bool any = false;
        while (_chat is not null && _chat.TryRead(out ChatEvent? e))
        {
            _chatLog!.Add(e!);
            any = true;
        }
        if (any)
            _chatBox?.Refresh(fresh: true);
    }

    private EdgeArrow? _edgeArrow;

    /// <summary>The arrow under the mouse when it is near an edge with a way out on that side; hidden elsewhere.</summary>
    private void ShowEdge(Vector2 at)
    {
        if (_edgeArrow is null || _town is null)
            return;
        if (SpotUnder(at) is not null || BoardView.Pick(_camera, at) is not Cell cell || _town.EdgeWay(cell) is not ({ } way, Cell step))
        {
            _edgeArrow.Hide();
            return;
        }
        Vector2 direction = _camera.UnprojectPosition(BoardView.ToWorld(cell + step)) - _camera.UnprojectPosition(BoardView.ToWorld(cell));
        _edgeArrow.Point(at, direction, way.Name?.In(_talkPanel!.Texts.Lang));
    }

    /// <summary>How far from a gathering spot's resource or gem on screen a click still reaches it, in pixels.</summary>
    public const float SpotReach = 32;

    /// <summary>
    /// The gathering spot a click is on (Charles, sprint 103: a spot by the edge could not be clicked, the edge's
    /// way taking the click): the nearest whose foot, middle or gem is within <see cref="SpotReach"/> pixels on screen.
    /// </summary>
    private Cell? SpotUnder(Vector2 click)
    {
        if (_town is null || _townView is null)
            return null;
        (Cell At, float Distance)? best = null;
        foreach (HarvestNode node in _town.Nodes)
        {
            if (_growing.Contains(node.Id))
                continue;
            Vector3 feet = BoardView.ToWorld(node.At);
            float d = new[] { 0f, 0.4f, TownView.GemHeight(node, _town.Town) }.Min(h => _camera.UnprojectPosition(feet + new Vector3(0, h, 0)).DistanceTo(click));
            if (d <= SpotReach && (best is null || d < best.Value.Distance))
                best = (node.At, d);
        }
        return best?.At;
    }

    /// <summary>How far from a monster's body on screen a right click still reaches it, in pixels (D68: "the click is too precise").</summary>
    public const float GroupReach = 56;

    /// <summary>The heights looked at on a monster: its feet, its middle, its head.</summary>
    private static readonly float[] BodyHeights = [0f, 0.6f, 1.2f];

    /// <summary>
    /// The group of monsters a right click is on: the one whose body (its feet, its middle, its head on
    /// screen) is nearest the click, within <see cref="GroupReach"/> pixels; else the one on the cell.
    /// </summary>
    private Cell? GroupUnder(Vector2 click)
    {
        if (_town is null || _townView is null)
            return null;
        (Cell At, float Distance)? best = null;
        foreach ((string id, Cell at) in _town.Groups)
        {
            Vector3 feet = BoardView.ToWorld(at);
            float d = BodyHeights.Min(h => _camera.UnprojectPosition(feet + new Vector3(0, h, 0)).DistanceTo(click));
            if (d <= GroupReach && (best is null || d < best.Value.Distance))
                best = (at, d);
        }
        if (best is not null)
            return best.Value.At;
        return BoardView.Pick(_camera, click) is Cell cell && _town.GroupAt(cell) is not null ? cell : null;
    }

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
            if (id != 1)
                return;
            void Go() => _ = Walk(_town!.Groups.TryGetValue(group.Id, out Cell now) ? now : cell, attack: true);
            // Little life left (sprint 101): asked first, never refused.
            if (_character is CharacterSummary c && _hero?.Class is not null)
            {
                int max = Life.MaxHp(_hero, GameData.Embedded), life = c.LifeAt(DateTimeOffset.UtcNow, GameData.Embedded);
                if (Life.Low(life, max))
                {
                    var ask = new ConfirmationDialog { DialogText = texts["life.low", life, max], OkButtonText = texts["life.attack"], CancelButtonText = texts["life.later"] };
                    ask.Confirmed += Go;
                    AddChild(ask);
                    ask.PopupCentered();
                    return;
                }
            }
            Go();
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
        _gatherTicket++;
        _walking = true;
        if (!await _townView!.Walk(step, WalkSpeed))
            return;
        _walking = false;
        _talkPanel!.Refresh();
        await TellTalks();
        await SavePlace();
        if (_town.Fight is string scenario)
            EnterFight(scenario);
        else if (_town.Travel is ZoneLink link)
            await TravelTo(link);
        else if (_town.Door is DungeonDoor door)
            AskDungeon(door);
        else if (_town.Bench is Workshop bench)
            OpenBench(bench);
        else if (_town.Shut is not null)
        {
            _sound?.Play("error");
            _talkPanel.ShowMessage(_talkPanel.Texts["hall.shut"]);
            _town.CloseDoor();
        }
        else if (_town.Spring)
            await Rest();
        else if (_town.Gathering is HarvestNode node)
            await Gather(node);
        else if (_town.Examining is Prop prop)
            await Examine(prop);
    }

    // The quest followed (sprint 106): chosen in the journal, kept for the character between two games; the
    // first under way when none is chosen.
    private string? _followed;
    private Guid? _followedFor;
    private const string FollowedFile = "user://followed.txt";
    private QuestTracker? _tracker;
    private EdgeArrow? _trailArrow;

    /// <summary>The quest followed, as it is under way, or none.</summary>
    private QuestProgress? Followed()
    {
        if (_character is null || _town?.Book is not QuestBook book || book.Active.Count == 0)
            return null;
        if (_followedFor != _character.Id)
        {
            _followedFor = _character.Id;
            _followed = null;
            try
            {
                if (!_options.TownSelfTest && !_options.LobbySelfTest && Godot.FileAccess.FileExists(FollowedFile))
                {
                    using var file = Godot.FileAccess.Open(FollowedFile, Godot.FileAccess.ModeFlags.Read);
                    string[] kept = file.GetAsText().Split(' ', StringSplitOptions.RemoveEmptyEntries);
                    if (kept.Length == 2 && kept[0] == _character.Id.ToString())
                        _followed = kept[1];
                }
            }
            catch (IOException)
            {
                // Nothing kept: the first quest under way.
            }
        }
        return book.Active.FirstOrDefault(p => p.Quest == _followed) ?? book.Active[0];
    }

    /// <summary>Follows a quest under way, kept for the character.</summary>
    private void Follow(string quest)
    {
        _followed = quest;
        if (_character is not null && !_options.TownSelfTest && !_options.LobbySelfTest)
        {
            using var file = Godot.FileAccess.Open(FollowedFile, Godot.FileAccess.ModeFlags.Write);
            file?.StoreString($"{_character.Id} {quest}");
        }
        ShowTrail();
    }

    /// <summary>Where the quest followed leads, for the world map.</summary>
    private TrailGoal? FollowedGoal() => Followed() is QuestProgress p && _town?.Book is QuestBook book ? QuestTrail.Goal(book, p) : null;

    /// <summary>The quest followed on the left, its ring on the map: on its goal, or on the way out towards it.</summary>
    private void ShowTrail()
    {
        if (_talkPanel is null || _town is null)
            return;
        Texts texts = _talkPanel.Texts;
        if (_tracker is null)
        {
            _tracker = new QuestTracker();
            AddChild(_tracker);
        }
        QuestProgress? followed = Followed();
        JournalQuest? entry = followed is null ? null
            : _town.Book!.Journal(texts).SelectMany(z => z.Active).FirstOrDefault(q => q.Id == followed.Quest);
        _tracker.Show(entry, texts);
        TrailGoal? goal = FollowedGoal();
        _townView?.ShowTrail(goal is null ? null : QuestTrail.Toward(GameData.Embedded, _town.Town, goal));
    }

    /// <summary>
    /// Each frame on a map: the trail's ring off the screen, a chevron at the screen's edge points to it (sprint 106).
    /// </summary>
    private void PointTrail()
    {
        if (_townView?.TrailAt is not Cell at || _camera is null || _talkPanel is null || _screen != Screen.Town)
        {
            _trailArrow?.Hide();
            return;
        }
        if (_trailArrow is null)
        {
            _trailArrow = new EdgeArrow();
            _talkPanel.AddChild(_trailArrow);
        }
        Vector2 size = GetViewport().GetVisibleRect().Size;
        Vector2 screen = _camera.UnprojectPosition(BoardView.ToWorld(at));
        var inside = new Rect2(new Vector2(40, 40), size - new Vector2(80, 80));
        if (inside.HasPoint(screen) && !_camera.IsPositionBehind(BoardView.ToWorld(at)))
        {
            _trailArrow.Hide();
            return;
        }
        Vector2 centre = size / 2, direction = (screen - centre).Normalized();
        float reach = Math.Min(Math.Abs(inside.Size.X / 2 / (direction.X == 0 ? 1e-6f : direction.X)), Math.Abs(inside.Size.Y / 2 / (direction.Y == 0 ? 1e-6f : direction.Y)));
        _trailArrow.Point(centre + direction * reach, direction, null);
    }

    /// <summary>
    /// A thing examined (sprint 105): what one sees in it, and for each quest under way that asks for it, the
    /// server counts it.
    /// </summary>
    private async Task Examine(Prop prop)
    {
        _town!.ExaminingSeenTo();
        Texts texts = _talkPanel!.Texts;
        _talkPanel.ShowMessage(texts["prop.seen", prop.Name.In(texts.Lang), prop.Text.In(texts.Lang)]);
        if (_lobby?.SignedIn != true || _character is null || _town.Book is not QuestBook book)
            return;
        // A thing that starts a fight (sprint 107: the loft's hatch), for the quest that asks to win it.
        if (book.FightFor(prop) is not null && prop.Fight is string scenario)
        {
            EnterFight(scenario);
            return;
        }
        try
        {
            foreach (Quest q in book.Interacts(prop.Id))
            {
                KeepCharacter(await _lobby.Server.QuestInteract(_character.Id, q.Id, prop.Id));
                _talkPanel.ShowNotice(texts["quest.found", q.Name.In(texts.Lang)]);
                _sound?.Play("click");
            }
        }
        catch (Exception e) when (e is ServerException or HttpRequestException)
        {
            _talkPanel.ShowMessage(e is ServerException refused ? texts["result.refused", refused.Message] : texts["lobby.unreachable"]);
        }
    }

    private JobsPanel? _jobsPanel;

    /// <summary>
    /// The trades window (sprint 100, D72): opened by J to look at, or at a trade's workshop on that trade,
    /// to make its recipes (it replaced the workshop's panel of sprint 81); the server makes what is asked.
    /// </summary>
    private JobsPanel OpenJobs(Workshop? bench = null)
    {
        _jobsPanel?.QueueFree();
        Texts texts = _talkPanel?.Texts ?? _hud.Texts;
        _jobsPanel = new JobsPanel();
        AddChild(_jobsPanel);
        _jobsPanel.Build(new JobBook(GameData.Embedded, _character, texts, bench?.Job), texts, bench?.Job);
        _jobsPanel.ClosePressed += () =>
        {
            if (bench is not null)
            {
                CloseJobs();
                _town?.CloseDoor();
            }
            else
            {
                _windows.Close();
                ShowWindows();
            }
        };
        _jobsPanel.MakePressed += (job, item, count) => _ = Make(job, item, count, bench);
        return _jobsPanel;
    }

    private void CloseJobs()
    {
        _jobsPanel?.QueueFree();
        _jobsPanel = null;
    }

    private JobsPanel OpenBench(Workshop bench) => OpenJobs(bench);

    private async Task Make(Job job, string item, int count, Workshop? bench)
    {
        if (_jobsPanel is null || _lobby?.SignedIn != true || _character is null)
            return;
        Texts texts = _jobsPanel.Texts;
        try
        {
            KeepCharacter(await _lobby.Server.Craft(_character.Id, job.Id, item, count));
            _jobsPanel?.Show(new JobBook(GameData.Embedded, _character, texts, bench?.Job));
            _jobsPanel?.ShowMessage(texts["craft.made", texts.Items([new ItemCount(item, count)])]);
        }
        catch (Exception e) when (e is ServerException or HttpRequestException)
        {
            _jobsPanel?.ShowMessage(e is ServerException refused ? texts["result.refused", refused.Message] : texts["lobby.unreachable"]);
        }
    }

    private ConfirmationDialog? _doorDialog;

    /// <summary>
    /// On a dungeon's door (sprint 80): what opens it, then in, on the server's word (it uses the key up);
    /// without its key, a word and the player stays outside.
    /// </summary>
    private ConfirmationDialog? AskDungeon(DungeonDoor door)
    {
        Texts texts = _talkPanel!.Texts;
        Dungeon dungeon = GameData.Embedded.Dungeons[door.Dungeon];
        (string text, bool canEnter) = texts.DoorText(dungeon, _character?.Inventory ?? (_character is null ? null : []), _character?.Run);
        if (!canEnter)
        {
            _talkPanel.ShowMessage(text);
            _town!.CloseDoor();
            return null;
        }
        _doorDialog?.QueueFree();
        _doorDialog = new ConfirmationDialog { Title = dungeon.Name.In(texts.Lang), DialogText = text, Exclusive = true, OkButtonText = texts["dungeon.go-in"], CancelButtonText = texts["dungeon.stay"] };
        _doorDialog.Confirmed += () => _ = EnterDungeon(dungeon);
        _doorDialog.Canceled += () => _town?.CloseDoor();
        AddChild(_doorDialog);
        _doorDialog.PopupCentered();
        return _doorDialog;
    }

    /// <summary>
    /// Into the dungeon: the server takes the key and says the room; offline, the first room, or the one
    /// reached. The player walks in that room's hall (sprint 86), at its way in.
    /// </summary>
    private async Task EnterDungeon(Dungeon dungeon)
    {
        int room;
        if (_lobby?.SignedIn == true && _character is not null)
        {
            try
            {
                _character = await _lobby.Server.EnterDungeon(_character.Id, dungeon.Id);
                room = _character.Run?.Room ?? 1;
            }
            catch (Exception e) when (e is ServerException or HttpRequestException)
            {
                _talkPanel?.ShowMessage(e is ServerException refused ? _talkPanel.Texts["result.refused", refused.Message] : _talkPanel.Texts["lobby.unreachable"]);
                _town?.CloseDoor();
                return;
            }
        }
        else
        {
            _offlineRun = _offlineRun?.Dungeon == dungeon.Id ? _offlineRun : new DungeonRun(dungeon.Id, 1);
            room = _offlineRun.Room;
        }
        _town!.CloseDoor();
        Town hall = GameData.Embedded.Towns[dungeon.Halls![room - 1].Map];
        await TravelTo(new ZoneLink(dungeon.Door, hall.Id, hall.Spawn));
    }

    /// <summary>The characteristics window, or the spells window, over the town; both tabs when <paramref name="spells"/> is null.</summary>
    private void OpenPoints(bool? spells = null)
    {
        if (_pointsPanel is not null || _hero is null)
            return;
        _pointsPanel = new PointsPanel();
        AddChild(_pointsPanel);
        // In a fight, to look at only (sprint 93).
        _pointsPanel.Build(new PointsEditor(_hero, GameData.Embedded, readOnly: !InTown), _talkPanel?.Texts ?? _hud.Texts, spells);
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
        _inventoryPanel.Build(new InventoryEditor(_hero, owned, GameData.Embedded, readOnly: !InTown), _talkPanel?.Texts ?? _hud.Texts);
        _inventoryPanel.SetShards(_character?.Shards ?? 0);
        _inventoryPanel.SavePressed += worn => _ = SaveEquipment(worn);
        _inventoryPanel.EatPressed += item => _ = Eat(item);
        _inventoryPanel.ClosePressed += () =>
        {
            _windows.Close();
            ShowWindows();
        };
    }

    /// <summary>Food eaten from the inventory (sprint 101), on the server's word.</summary>
    private async Task Eat(Item item)
    {
        InventoryPanel? panel = _inventoryPanel;
        if (panel is null || _lobby?.SignedIn != true || _character is null)
            return;
        try
        {
            KeepCharacter(await _lobby.Server.Eat(_character.Id, item.Id));
            panel.Saved(_hero!, _character.Inventory ?? [], panel.Texts["life.eaten", item.Name.In(panel.Texts.Lang), item.Heal]);
        }
        catch (Exception e) when (e is ServerException or HttpRequestException)
        {
            panel.ShowMessage(e is ServerException refused ? panel.Texts["result.refused", refused.Message] : panel.Texts["lobby.unreachable"]);
        }
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
    /// <summary>
    /// The deck of sprint 82 (D66): emptied, a spell dragged onto it, then dragged off; its code pasted back;
    /// every place shows the spell it holds; signed in, a deck of one spell saved and read back.
    /// </summary>
    private async Task CheckDeck(List<string> problems)
    {
        PointsPanel panel = _pointsPanel!;
        PointsEditor editor = panel.Editor;
        bool Shows() => panel.DeckShown.SequenceEqual(editor.Deck.Select(id => (string?)id).Concat(Enumerable.Repeat<string?>(null, Hero.DeckSize - editor.Deck.Count)));
        string code = editor.Code;
        string first = editor.Spells[0].Id;
        panel.PressClearDeck();
        if (editor.Deck.Count != 0 || !Shows())
            problems.Add("emptying the deck leaves spells on it");
        panel.Drop("spell:" + first, 0);
        if (!editor.Deck.SequenceEqual([first]) || !Shows())
            problems.Add($"a spell dropped on the deck shows {string.Join(", ", panel.DeckShown)}");
        if (_lobby?.SignedIn == true && _character is not null)
        {
            await SavePoints(editor.ToPoints());
            if (!(_character.Deck ?? ["default"]).SequenceEqual([first]))
                problems.Add($"a deck of one spell comes back from the server as {string.Join(", ", _character.Deck ?? ["the class's"])}");
            panel = _pointsPanel!;
            editor = panel.Editor;
        }
        panel.Drop("deck:0", null);
        if (editor.Deck.Count != 0 || !Shows())
            problems.Add("a spell dragged off the deck stays on it");
        panel.PasteCode(code);
        if (editor.Code != code || !Shows())
            problems.Add($"the code {code} gives the deck {editor.Code}");
        // The icons' textures outlive a garbage collection (D67: white squares on Charles's screen).
        foreach (Spell spell in editor.Spells)
            SpellIcon.TextureOf(spell);
        GC.Collect();
        GC.WaitForPendingFinalizers();
        if (!SpellIcon.Kept.Any() || SpellIcon.Kept.Any(t => !IsInstanceValid(t) || !t.GetRid().IsValid))
            problems.Add("a spell icon's texture is gone after a garbage collection");
        // The window moved by its header, as the mouse would (D67).
        Control window = panel.Window;
        // In the corner of the header, away from its buttons.
        Vector2 before = window.Position, grab = window.GlobalPosition + new Vector2(6, 6);
        // Headless, the mouse does not reach the interface: the events go to the window's handler itself.
        Vector2 local = grab - window.GlobalPosition;
        foreach (InputEvent e in new InputEvent[]
        {
            new InputEventMouseButton { ButtonIndex = MouseButton.Left, Pressed = true, Position = local },
            new InputEventMouseMotion { Relative = new Vector2(-40, 30), Position = local + new Vector2(-40, 30) },
            new InputEventMouseButton { ButtonIndex = MouseButton.Left, Pressed = false, Position = local + new Vector2(-40, 30) },
        })
            window.EmitSignal(Control.SignalName.GuiInput, e);
        if (window.Position != before + new Vector2(-40, 30))
            problems.Add($"the spells window dragged by its header went from {before} to {window.Position}");
        // The deck it had, kept again.
        if (_lobby?.SignedIn == true && _character is not null)
            await SavePoints(editor.ToPoints());
    }

    /// <summary>
    /// The chat of sprint 84: Enter opens the field, Escape leaves it; signed in, a line typed comes back
    /// from the server, as everyone on the map sees it.
    /// </summary>
    private async Task<string> CheckChat(List<string> problems)
    {
        ChatBox box = _chatBox!;
        if (box.LinesShown)
            problems.Add("the chat shows with nothing new");
        box.Open();
        bool opened = box.Typing;
        box.Leave();
        if (!opened || box.Typing)
            problems.Add("the chat's field does not open and close");
        if (_lobby?.SignedIn != true || _character is null)
            return "";
        for (int i = 0; i < 100 && _chat is null; i++)
            await ToSignal(GetTree().CreateTimer(0.05), SceneTreeTimer.SignalName.Timeout);
        string said = "Bonjour depuis l'auto-test";
        box.Type(said);
        string expected = _talkPanel!.Texts["chat.line", _talkPanel.Texts["chat.map"], _character.Name, said];
        for (int i = 0; i < 100 && !box.LogText.Contains(expected, StringComparison.Ordinal); i++)
        {
            await ToSignal(GetTree().CreateTimer(0.05), SceneTreeTimer.SignalName.Timeout);
            DrainChat();
        }
        if (!box.LogText.Contains(expected, StringComparison.Ordinal))
        {
            problems.Add($"the chat shows \"{box.LogText}\", not the line said");
            return "";
        }
        // A line come in shows the chat (hidden otherwise, D67).
        await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
        if (!box.LinesShown)
            problems.Add("a line came in and the chat stays hidden");
        return " Chat: a line heard back.";
    }

    /// <summary>
    /// The quests, signed in (sprints 88 and 107): Aubin offers his welcome, accepted on the server; the journal (Q)
    /// shows it; at the West Gate, Garance says her line for it, the talk counts on the server and she takes the quest
    /// back herself; then she offers her first lesson.
    /// </summary>
    private async Task<string> CheckQuests(List<string> problems)
    {
        if (_lobby?.SignedIn != true || _character is null)
            return "";
        Quest welcome = GameData.Embedded.Quests["welcome"];
        Texts texts = _talkPanel!.Texts;
        TownController town = _town!;
        _town = new TownController(GameData.Embedded, Village) { Book = QuestBook.Of(_character, GameData.Embedded) };
        try
        {
            async Task Until(Func<bool> done)
            {
                for (int i = 0; i < 100 && !done(); i++)
                    await ToSignal(GetTree().CreateTimer(0.05), SceneTreeTimer.SignalName.Timeout);
            }
            Npc Inhabitant(string id) => _town.Town.Npcs.Single(n => n.Id == id);
            _town.Click(Inhabitant("aubin").At);
            if (_town.Talk?.Line.Answers[0].Text.In(texts.Lang) != texts["quest.offer", welcome.Name.In(texts.Lang)])
            {
                problems.Add("Aubin does not offer his welcome");
                return "";
            }
            Answer(0);
            Answer(0);
            await Until(() => _character.QuestLog is { Count: > 0 });
            if (_character.QuestLog?.SingleOrDefault()?.Quest != welcome.Id)
                problems.Add("the quest taken is not under way on the server");
            TownInput(new InputEventKey { Pressed = true, Keycode = Key.Q });
            if (_journalPanel is null || !_journalPanel.ActiveNames.SequenceEqual([welcome.Name.In(texts.Lang)]) || _journalPanel.ObjectiveLines.Count != welcome.Objectives!.Count)
                problems.Add($"the journal shows {string.Join(", ", _journalPanel?.ActiveNames ?? [])}");
            TownInput(new InputEventKey { Pressed = true, Keycode = Key.Q });
            if (_journalPanel is not null)
                problems.Add("Q does not close the journal");
            // Garance in the drill yard by the West Gate (sprint 102): her line for the newcomer, the talk counted, the quest hers to take back.
            _town = new TownController(GameData.Embedded, "clairval-0-1") { Book = _town.Book };
            _town.Click(Inhabitant("garance").At);
            await TellTalks();
            if (_town.Talk?.Line.Text != welcome.Objectives![0].Line)
                problems.Add("Garance does not say her line for the welcome");
            if (_character.QuestLog?.SingleOrDefault()?.Counts[0] != 1)
                problems.Add("talking to Garance is not counted");
            if (_town.Talk?.Line.Answers[0].Text.In(texts.Lang) != texts["quest.handin", welcome.Name.In(texts.Lang)])
            {
                problems.Add("Garance does not take the welcome back");
                return "";
            }
            Answer(0);
            Answer(0);
            await Until(() => _character.Quests?.Contains(welcome.Id) == true);
            if (_character.Quests?.Contains(welcome.Id) != true || _character.QuestLog is { Count: > 0 })
                problems.Add("the welcome handed back is not done on the server");
            _town.Interrupt(_town.Position);
            _town.Click(Inhabitant("garance").At);
            Quest lesson = GameData.Embedded.Quests["first-lesson"];
            if (_town.Talk?.Line.Answers[0].Text.In(texts.Lang) != texts["quest.offer", lesson.Name.In(texts.Lang)])
                problems.Add("Garance does not offer her first lesson");
            _town.Interrupt(_town.Position);
            return " Quests: Aubin's welcome taken, followed in the journal, Garance seen and given it back, her lesson offered.";
        }
        finally
        {
            _town = town;
            _town.Book = QuestBook.Of(_character, GameData.Embedded);
        }
    }

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
        else if (_town.QuestTaken is not null || _town.QuestHandedIn is not null)
            _ = SeeToQuest();
        else if (_town.ShopOpened)
        {
            _town.ShopSeenTo();
            OpenShop();
        }
    }

    private ShopPanel? _shopPanel;

    /// <summary>Rose's stall (sprint 97): her stock and what she buys; the server makes each deal.</summary>
    private ShopPanel OpenShop()
    {
        _shopPanel?.QueueFree();
        Texts texts = _talkPanel!.Texts;
        _shopPanel = new ShopPanel();
        AddChild(_shopPanel);
        _shopPanel.Build(new Stall(_character, texts), texts);
        _shopPanel.ClosePressed += () =>
        {
            _shopPanel?.QueueFree();
            _shopPanel = null;
        };
        _shopPanel.DealPressed += (item, count, buying) => _ = Deal(item, count, buying);
        return _shopPanel;
    }

    private async Task Deal(string item, int count, bool buying)
    {
        if (_shopPanel is null || _lobby?.SignedIn != true || _character is null)
            return;
        Texts texts = _talkPanel!.Texts;
        try
        {
            KeepCharacter(buying ? await _lobby.Server.Buy(_character.Id, item, count) : await _lobby.Server.Sell(_character.Id, item, count));
            _shopPanel?.Show(new Stall(_character, texts));
            _shopPanel?.ShowMessage(texts[buying ? "shop.bought" : "shop.sold", texts.Items([new ItemCount(item, count)])]);
        }
        catch (Exception e) when (e is ServerException or HttpRequestException)
        {
            _shopPanel?.ShowMessage(e is ServerException refused ? texts["result.refused", refused.Message] : texts["lobby.unreachable"]);
        }
    }

    private JournalPanel? _journalPanel;

    private void OpenJournal()
    {
        _journalPanel = new JournalPanel();
        AddChild(_journalPanel);
        _journalPanel.Build(_town?.Book ?? new QuestBook(GameData.Embedded, 1, [], [], []), _talkPanel!.Texts, Followed()?.Quest);
        _journalPanel.FollowPressed += id =>
        {
            Follow(id);
            CloseJournal();
            OpenJournal();
        };
        _journalPanel.ClosePressed += () =>
        {
            _windows.Close();
            ShowWindows();
        };
    }

    private void CloseJournal()
    {
        _journalPanel?.QueueFree();
        _journalPanel = null;
    }

    /// <summary>The character as the server now says, its quests in the town's book (and the journal, if open).</summary>
    private void KeepCharacter(CharacterSummary character)
    {
        // A trade's new level, said in the middle of the screen (Charles, sprint 103).
        if (_talkPanel is not null && JobBook.LevelsGained(_character, character) is { Count: > 0 } gained)
        {
            _talkPanel.ShowNotice(string.Join("  ", gained.Select(g => _talkPanel.Texts["job.levelup", GameData.Embedded.Jobs[g.Job].Name.In(_talkPanel.Texts.Lang), g.Level])));
            _sound?.Play("victory");
        }
        _character = character;
        _hero = character.Hero;
        _inventoryPanel?.SetShards(character.Shards);
        if (_town is not null)
            _town.Book = QuestBook.Of(character, GameData.Embedded);
        _townView?.ShowMarks(_town?.Book);
        ShowTrail();
        if (_journalPanel is not null)
        {
            CloseJournal();
            OpenJournal();
        }
        _talkPanel?.SetProgress(_hero.Level, character.Xp);
        if (_talkPanel is not null)
            _talkPanel.Living = character;
    }

    /// <summary>A quest taken or handed back in a talk (sprint 88): on the server's word.</summary>
    private async Task SeeToQuest()
    {
        Texts texts = _talkPanel!.Texts;
        (string? taken, string? handedIn) = (_town!.QuestTaken, _town.QuestHandedIn);
        _town.QuestSeenTo();
        if (_lobby?.SignedIn != true || _character is null)
            return;
        try
        {
            if (taken is not null)
            {
                KeepCharacter(await _lobby.Server.TakeQuest(_character.Id, taken));
                _talkPanel.ShowMessage(texts["quest.taken", GameData.Embedded.Quests[taken].Name.In(texts.Lang)]);
            }
            else if (handedIn is not null)
            {
                QuestReward reward = await _lobby.Server.HandInQuest(_character.Id, handedIn);
                KeepCharacter(reward.Character);
                _talkPanel.ShowMessage(texts["quest.done", GameData.Embedded.Quests[handedIn].Name.In(texts.Lang), reward.Xp, reward.Shards]);
                _sound?.Play("victory");
            }
        }
        catch (Exception e) when (e is ServerException or HttpRequestException)
        {
            _talkPanel.ShowMessage(e is ServerException refused ? texts["result.refused", refused.Message] : texts["lobby.unreachable"]);
        }
    }

    /// <summary>Someone the quests under way asked to see (sprint 88): the server counts the talk.</summary>
    private async Task TellTalks()
    {
        IReadOnlyList<Quest> quests = _town!.QuestTalks;
        if (quests.Count == 0 || _lobby?.SignedIn != true || _character is null || _town.Talk is not Conversation talk)
            return;
        string npc = GameData.Embedded.RefOf(_town.Town, talk.Npc);
        Texts texts = _talkPanel!.Texts;
        try
        {
            foreach (Quest q in quests)
            {
                KeepCharacter(await _lobby.Server.QuestTalk(_character.Id, q.Id, npc));
                _talkPanel.ShowMessage(texts["quest.talked", q.Name.In(texts.Lang)]);
            }
        }
        catch (Exception e) when (e is ServerException or HttpRequestException)
        {
            _talkPanel.ShowMessage(e is ServerException refused ? texts["result.refused", refused.Message] : texts["lobby.unreachable"]);
        }
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
    /// the place is saved on the server and read back; then Garance's training fight and its self-test.
    /// </summary>
    private async Task RunTownSelfTest()
    {
        var problems = new List<string>();
        string lang = _options.Lang;
        int talks = 0;
        await CheckFreeWalk(problems);
        // The tour reads the inhabitants' own lines; their quests are checked apart (CheckQuests).
        QuestBook? book = _town!.Book;
        _town.Book = null;
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
        _town.Book = book;
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
            await CheckDeck(problems);
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
            // The Éclats owned, by the title (sprint 103).
            if (inv.ShardsText != inv.Texts["inventory.shards", _character?.Shards ?? 0])
                problems.Add($"the inventory says '{inv.ShardsText}' of the Éclats");
            inv.QueueFree();
            _inventoryPanel = null;
        }
        // The wheel over an open window is the window's, not the map's (Charles, sprint 101).
        if (_explore is not null && _hero?.Class is not null)
        {
            double zoom = _explore.Zoom;
            ToggleWindow(GameWindow.Inventory);
            TownInput(new InputEventMouseButton { Pressed = true, ButtonIndex = MouseButton.WheelUp });
            if (_explore.Zoom != zoom)
                problems.Add("the wheel zooms the map with the inventory open");
            ToggleWindow(GameWindow.Inventory);
        }
        // Every item's drawing (sprint 98), imported with the game.
        string[] bare = [.. GameData.Embedded.Items.Values.Where(i => ItemIcon.TextureOf(i) is null).Select(i => i.Id)];
        if (bare.Length > 0)
            problems.Add($"no drawing for {string.Join(", ", bare.Take(5))}");
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
        string world = CheckWorld(problems) + " " + CheckBarAndMenu(problems) + await CheckChat(problems) + await CheckQuests(problems);
        note += problems.Count == 0 ? ", views checked." : $". TOWN PROBLEMS: {string.Join("; ", problems.Distinct().Take(5))}";
        if (problems.Count == 0)
            note += " " + world;
        _selfTestNote = _selfTestNote is null ? note : $"{_selfTestNote} {note}";
        // Garance's lesson, which her talk at the west gate starts since sprint 102 (no gate to it on the square).
        string scenario = _town!.Fight ?? "training";
        if (problems.Count > 0)
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
            .Where(c => town.Town.IsFree(town.Board, c) && town.Town.NpcAt(c) is null && town.PathTo(c) is not null && town.EdgeWay(c) is null)
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
                GameWindow.Journal => _journalPanel is not null && _inventoryPanel is null && _mapPanel is null,
                GameWindow.Jobs => _jobsPanel is not null && _journalPanel is null && _inventoryPanel is null && _mapPanel is null,
                _ => _inventoryPanel is not null && _pointsPanel is null && _mapPanel is null && _journalPanel is null && _jobsPanel is null,
            };
            if (!shown || !_bar.Lit(window))
                problems.Add($"{key} does not open the {window} window alone");
            windows++;
        }
        TownInput(new InputEventKey { Pressed = true, Keycode = Key.Escape });
        if (_mapPanel is not null || _pointsPanel is not null || _inventoryPanel is not null || _journalPanel is not null || _menuPanel is not null)
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
            if (view.Tiles != t.Rows.Count * t.Rows[0].Length || view.WayLabels != t.Exits.Count + t.Doors.Count + (t.Workshops ?? []).Count + (t.Links ?? []).Where(l => l.Name is not null).Select(l => l.To).Distinct().Count())
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
        foreach (int zoom in new[] { 0, 3 })
        {
            _explore!.ZoomBy(zoom);
            _explore.Jump(player.X, player.Z);
            PlaceCamera();
            if (_camera.UnprojectPosition(player).DistanceTo(middle) > 2)
                problems.Add($"zoomed {_explore.Zoom:0.0}, the camera shows the player at {_camera.UnprojectPosition(player)}");
            _explore.ZoomBy(-zoom);
        }
        ToggleMap();
        CheckWorldMap(problems);
        ToggleMap();
        CheckMonsters(problems);
        CheckEdges(problems);
        int doors = CheckDungeons(problems);
        return $"World: {zones.Count} maps, {groups} groups of monsters, {doors} dungeons, camera and map checked.";
    }

    /// <summary>
    /// The edges of sprint 83: the mouse over a cell of the edge where a way leaves shows the arrow,
    /// pointing outwards on screen; in the middle of the map, none.
    /// </summary>
    private void CheckEdges(List<string> problems)
    {
        // A fresh walk on this map: the tour has chosen the gate's fight by now, and a busy player gets no arrow.
        var town = _town;
        _town = new TownController(GameData.Embedded, town!.Town.Id, town.Position);
        Town map = _town.Town;
        ZoneLink? link = map.Links is { Count: > 0 } links ? links[0] : null;
        if (link is null)
        {
            _town = town;
            return;
        }
        Vector2 Screen(Cell c) => _camera.UnprojectPosition(BoardView.ToWorld(c));
        ShowEdge(Screen(link.At));
        (ZoneLink, Cell)? expected = _town.EdgeWay(link.At);
        if (expected is not ({ } way, Cell step) || !_edgeArrow!.Visible || _edgeArrow.ZoneText != (way.Name?.In(_talkPanel!.Texts.Lang) ?? ""))
            problems.Add($"no arrow over the way to {link.To}");
        else
        {
            // Pointing outwards: towards where the cell beyond the edge is drawn.
            Vector2 outwards = Screen(link.At + step) - Screen(link.At);
            Vector2 drawn = Vector2.Up.Rotated(_edgeArrow.Rotation);
            if (drawn.Dot(outwards.Normalized()) < 0.99f)
                problems.Add($"the arrow over the way to {link.To} points {drawn}, not {outwards.Normalized()}");
        }
        ShowEdge(Screen(new Cell(map.Width / 2, map.Rows.Count / 2)));
        if (_edgeArrow!.Visible)
            problems.Add("an arrow in the middle of the map");
        _edgeArrow.Hide();
        _town = town;
    }

    /// <summary>
    /// The dungeons of sprint 80: each door drawn with its name on its map; walking onto it asks to go in
    /// (offline here: the door opens), staying outside lets the player walk on; every room's fighters can
    /// be drawn on its board.
    /// </summary>
    private int CheckDungeons(List<string> problems)
    {
        var town = _town;
        int checkedDoors = 0;
        foreach (Dungeon d in GameData.Embedded.Dungeons.Values)
        {
            _town = new TownController(GameData.Embedded, d.Map);
            if (_town.Click(d.Door)?.Door?.Dungeon != d.Id)
            {
                problems.Add($"walking onto the door of {d.Id} does not ask to go in");
                continue;
            }
            // Offline the door opens; signed in, it wants the key the character may not have.
            Texts texts = _talkPanel!.Texts;
            (string expected, bool canEnter) = texts.DoorText(d, _character?.Inventory ?? (_character is null ? null : []), _character?.Run);
            ConfirmationDialog? dialog = AskDungeon(_town.Door!);
            if (canEnter ? dialog is null || dialog.DialogText != expected || dialog.Title != d.Name.In(texts.Lang) : dialog is not null || _talkPanel.MessageText != expected)
                problems.Add($"the door of {d.Id} says \"{dialog?.DialogText ?? _talkPanel.MessageText}\", not \"{expected}\"");
            dialog?.EmitSignal(AcceptDialog.SignalName.Canceled);
            if (_town.Door is not null || _town.Click(_town.Town.Spawn) is null)
                problems.Add($"staying outside {d.Id} does not let the player walk on");
            dialog?.QueueFree();
            _doorDialog = null;
            for (int room = 1; room <= Dungeons.RoomCount; room++)
            {
                var fight = new Fight(GameData.Embedded, d.Scenario(room), 1);
                if (fight.Fighters.Count(f => f.Team == 1) != Dungeons.RolesIn(room).Count)
                    problems.Add($"room {room} of {d.Id} has {fight.Fighters.Count - 1} monsters");
                CheckHall(d, room, problems);
            }
            checkedDoors++;
        }
        // The locksmith's bench in Clairval's craftsmen's quarter (sprints 81 and 102): walked onto, it opens its recipes, none to make
        // without the ingredients; closed, the player walks on.
        _town = new TownController(GameData.Embedded, "clairval-3-1");
        Workshop bench = _town.Town.Workshops![0];
        if (_town.Click(bench.At)?.Path is null || _town.Bench != bench)
            problems.Add("walking onto the locksmith's bench does not open it");
        else
        {
            // Since sprint 100, the trades window on the bench's trade, its recipes listed.
            JobsPanel panel = OpenBench(bench);
            var expected = new JobBook(GameData.Embedded, _character, _talkPanel!.Texts, bench.Job);
            if (panel.Shown.Id != bench.Job || panel.JobsListed != expected.Jobs.Count || panel.Lines != expected.Recipes(GameData.Embedded.Jobs[bench.Job]).Count)
                problems.Add($"the bench opens the trades window on {panel.Shown.Id}, {panel.JobsListed} trades and {panel.Lines} lines");
            CloseJobs();
            _town.CloseDoor();
            if (_town.Click(_town.Town.Spawn) is null)
                problems.Add("after the bench, the player cannot walk on");
        }
        // Rose's stall on the market (sprints 97 and 102): her answer opens it, her stock as the rules say; closed, the player walks on.
        _town = new TownController(GameData.Embedded, "clairval-2-1");
        Npc rose = _town.Town.Npcs.First(n => n.Dialogue == "merchant");
        Dialogue merchant = GameData.Embedded.Dialogues["merchant"];
        int shopAnswer = merchant.Lines[merchant.Start].Answers.ToList().FindIndex(a => a.Shop);
        if (_town.Click(rose.At)?.TalkTo != rose || shopAnswer < 0)
            problems.Add("Rose's talk has no answer opening her stall");
        else
        {
            Answer(shopAnswer);
            var expectedStall = new Stall(_character, _talkPanel!.Texts);
            if (_shopPanel is null || _shopPanel.TitleText != expectedStall.Title || !_shopPanel.Buying.SequenceEqual(expectedStall.ToBuy.Select(r => (r.Item.Id, r.Most > 0))))
                problems.Add($"Rose's answer does not open her stall as the rules say ({_shopPanel?.TitleText ?? "nothing"})");
            _shopPanel?.QueueFree();
            _shopPanel = null;
            if (_town.Click(_town.Town.Spawn) is null)
                problems.Add("after Rose's stall, the player cannot walk on");
        }
        // Drawn on its map: the door's name over it.
        Dungeon first = GameData.Embedded.Dungeons.Values.First();
        var view = new TownView();
        AddChild(view);
        view.Build(new TownController(GameData.Embedded, first.Map), _options.Lang, _hero ?? new Hero("", "female-d"));
        if (!view.WayNames.Contains(first.Name.In(_options.Lang)))
            problems.Add($"the door of {first.Id} has no name over it");
        if (view.Keeps != 1)
            problems.Add($"the map of {first.Id} shows {view.Keeps} keeps");
        // Its gathering spots (sprint 99), drawn, and grey once growing back.
        int spots = GameData.Embedded.Nodes.Values.Count(n => n.Map == first.Map);
        view.ShowGrowing([GameData.Embedded.Nodes.Values.First(n => n.Map == first.Map).Id]);
        if (spots == 0 || view.NodesShown != (spots, 1))
            problems.Add($"the map of {first.Id} draws {view.NodesShown.Drawn} of its {spots} gathering spots");
        RemoveChild(view);
        view.QueueFree();
        // The square's marks and its old stone (sprint 105): "!" over Aubin for a new hero; the stone glinting and a
        // grey "?" over Ysaline while her quest is under way, a "?" once it is done.
        {
            view = new TownView();
            AddChild(view);
            var square = new TownController(GameData.Embedded, "clairval");
            view.Build(square, _options.Lang, _hero ?? new Hero("", "female-d"));
            QuestProgress stone = Quests.Start(GameData.Embedded.Quests["whispering-stone"]);
            view.ShowMarks(new QuestBook(GameData.Embedded, 1, [], [], []));
            string fresh = view.MarkShown("aubin");
            bool idle = view.ClueShown("old-stone");
            view.ShowMarks(new QuestBook(GameData.Embedded, 1, [], [stone], []));
            (string waiting, bool glint) = (view.MarkShown("ysaline"), view.ClueShown("old-stone"));
            view.ShowMarks(new QuestBook(GameData.Embedded, 1, [], [stone with { Counts = [1, 1, 1] }], []));
            if (view.PropsDrawn != 1 || fresh != "!" || idle || waiting != "?" || !glint || view.ClueShown("old-stone") || view.MarkShown("ysaline") != "?")
                problems.Add($"the square shows {view.PropsDrawn} props, '{fresh}' over Aubin, the stone glinting {idle}/{glint}, '{waiting}' over Ysaline");
            // Following it (sprint 106): on the left, its name and its three objectives and where; the ring on the stone;
            // "Followed" in the journal; a ring on the world map.
            var book = new QuestBook(GameData.Embedded, 1, [], [stone], []);
            Texts t = _talkPanel?.Texts ?? _hud.Texts;
            var tracker = new QuestTracker();
            AddChild(tracker);
            tracker.Show(book.Journal(t).SelectMany(z => z.Active).Single(), t);
            TrailGoal goal = QuestTrail.Goal(book, stone)!;
            view.ShowTrail(QuestTrail.Toward(GameData.Embedded, square.Town, goal));
            if (tracker.TitleText != GameData.Embedded.Quests["whispering-stone"].Name.In(t.Lang) || tracker.Lines.Count != 4 || view.TrailAt != GameData.Embedded.FindProp("old-stone")!.Value.Prop.At)
                problems.Add($"following the stone shows '{tracker.TitleText}' with {tracker.Lines.Count} lines, the ring on {view.TrailAt}");
            tracker.QueueFree();
            var journal = new JournalPanel();
            AddChild(journal);
            journal.Build(book, t, stone.Quest);
            if (!journal.FollowButtons[stone.Quest].Disabled)
                problems.Add("the journal does not say the stone's quest is followed");
            journal.QueueFree();
            var world = new WorldMapPanel();
            AddChild(world);
            world.Build(GameData.Embedded, "clairval-3-2", new Cell(24, 16), t, goal);
            if (world.GoalCentre is null)
                problems.Add("the world map does not show where the quest followed leads");
            world.QueueFree();
            RemoveChild(view);
            view.QueueFree();
        }
        // The village's yards (sprint 104): each beast drawn with its two animations, the hen house, the stables and the fences.
        foreach ((string map, int sheds) in new[] { ("clairval-1-2", 1), ("clairval-0-2", 1), ("clairval-3-2", 0) })
        {
            view = new TownView();
            AddChild(view);
            view.Build(new TownController(GameData.Embedded, map), _options.Lang, _hero ?? new Hero("", "female-d"));
            int beasts = GameData.Embedded.Towns[map].Animals?.Count ?? 0;
            if (beasts == 0 || view.Beasts.Count != beasts || view.Sheds != sheds || view.Fences == 0)
                problems.Add($"{map} draws {view.Beasts.Count} of its {beasts} beasts, {view.Sheds} sheds and {view.Fences} fences");
            if (view.Beasts.Any(b => b.FindChild("AnimationPlayer", true, false) is not AnimationPlayer anim || !anim.HasAnimation("idle") || !anim.HasAnimation("walk")))
                problems.Add($"a beast of {map} has no idle or walk");
            RemoveChild(view);
            view.QueueFree();
        }
        // A hall drawn (sprint 86): its keeper, its group, its two ways named.
        Town hall = GameData.Embedded.Towns[first.Halls![0].Map];
        view = new TownView();
        AddChild(view);
        view.Build(new TownController(GameData.Embedded, hall.Id, null, null, new DungeonRun(first.Id, 1)), _options.Lang, _hero ?? new Hero("", "female-d"));
        if (view.People.Count != 1 || view.WayNames.Count() != 2)
            problems.Add($"the hall {hall.Id} shows {view.People.Count} people and the ways {string.Join(", ", view.WayNames)}");
        RemoveChild(view);
        view.QueueFree();
        _town = town;
        return checkedDoors;
    }

    /// <summary>
    /// A dungeon's hall (sprint 86): at its room, the group stands, the far door is shut and says why,
    /// the fountain heals; the room behind, no group and the far door leads to the next hall.
    /// </summary>
    private static void CheckHall(Dungeon d, int room, List<string> problems)
    {
        Town hall = GameData.Embedded.Towns[d.Halls![room - 1].Map];
        var here = new TownController(GameData.Embedded, hall.Id, null, null, new DungeonRun(d.Id, room));
        if (!here.Groups.ContainsKey(d.GroupOf(room)))
            problems.Add($"no group guards {hall.Id}");
        Cell fountain = hall.Rows.Select((r, y) => new Cell(r.IndexOf('F', StringComparison.Ordinal), y)).First(c => c.X >= 0);
        if (here.Click(fountain) is null || !here.Spring)
            problems.Add($"the fountain of {hall.Id} does not heal");
        here.CloseDoor();
        ZoneLink? far = (hall.Links ?? []).FirstOrDefault(l => GameData.Embedded.Towns[l.To].IsHall);
        if (far is null)
            return;
        if (here.Click(far.At) is null || here.Shut != far || here.Travel is not null)
            problems.Add($"the far door of {hall.Id} opens before its group is beaten");
        var behind = new TownController(GameData.Embedded, hall.Id, null, null, new DungeonRun(d.Id, room + 1));
        if (behind.Groups.Count != 0 || behind.Click(far.At) is null || behind.Travel != far)
            problems.Add($"{hall.Id}, its group beaten, does not lead on");
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
        // A right click on the monster's head, a little off, still finds it (D68).
        Vector2 head = _camera.UnprojectPosition(BoardView.ToWorld(group.Home) + new Vector3(0, 1.2f, 0)) + new Vector2(20, -10);
        if (GroupUnder(head) != group.Home)
            problems.Add($"a right click near the head of the group {group.Id} does not reach it");
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
        // The grid of the maps (sprint 85): shown by default.
        if (!map.GridShown)
            problems.Add("the world map has no grid");
        // The eight dungeons' doors, each named under the pointer (sprint 83).
        if (map.Doors.Count() != GameData.Embedded.Dungeons.Count)
            problems.Add($"the world map shows {map.Doors.Count()} dungeons");
        foreach ((string id, Vector2 centre) in map.Doors.Take(1))
        {
            if (map.HoverAt(centre) != _talkPanel!.Texts["map.dungeon", GameData.Embedded.Dungeons[id].Name.In(_talkPanel.Texts.Lang)])
                problems.Add($"the door of {id} on the world map says \"{map.HoverAt(centre)}\"");
        }
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
