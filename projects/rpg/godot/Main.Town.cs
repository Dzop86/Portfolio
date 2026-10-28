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
    private TreePanel? _treePanel;

    // The draft of the characteristics window, carried to the tree's when its Talents tab opens it (sprint 148).
    private PointsEditor? _carried;
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
        // The other players and the groups away (sprint 118): what the server said of this map, forgotten on another.
        if (_crowdMap != _zone)
        {
            _crowd.Clear();
            _crowdMap = _zone;
        }
        DateTimeOffset now = DateTimeOffset.UtcNow;
        _town = new TownController(GameData.Embedded, _zone, _place, Presence.Away(now).Concat(_crowd.Away(now)), Run);
        // The groups where every player sees them now (sprint 118, D74), not at their homes.
        _slot = -1;
        if (Roams)
            Regroup(Roam.Places(GameData.Embedded, _town.Town, _town.Board, Roam.Slot(now)), walk: false);
        // The character's quests (sprint 88), offered and taken back in the talks; online only.
        if (_character is not null)
            _town.Book = QuestBook.Of(_character, GameData.Embedded);
        _townView = new TownView();
        _beastAt.Clear();
        AddChild(_townView);
        _townView.Build(_town, _options.Lang, _hero ?? new Hero("", "female-d"));
        _townView.ShowOthers(_crowd.Others, WalkSpeed);
        _townView.ShowOwnGuild(_guild?.Name, _guild?.Standing?.Crest);
        // A word for an achievement earned since the last map (sprint 155): after a fight, a contract, a trade.
        CheckAchievements();
        _townView.ShowMarks(_town.Book);
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
        _talkPanel.Living = _character;
        _talkPanel.Answered += Answer;
        _talkPanel.LanguageChanged += () =>
        {
            _townView.SetLanguage(_talkPanel.Texts.Lang);
            _bar?.SetTexts(_talkPanel.Texts);
            _menuPanel?.SetTexts(_talkPanel.Texts);
            _miniMap?.SetTexts(_talkPanel.Texts);
        };
        ShowBar();
        ShowMiniMap();
        ShowChat();
        ShowParty();
        // The quest followed, once the talk panel gives its words (sprint 115: drawn before it, it vanished at each change of map).
        ShowTrail();
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
        else if (_options.Shot is "town" or "banner" or "menu" or "inventory" or "points" or "spells" or "tree" or "zone" or "world" or "achievements" or "achievements-list")
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

    // The draws of the beasts' walks.
    private readonly Rng _roam = new((ulong)System.Environment.TickCount64);

    /// <summary>A group walks this many cells a second.</summary>
    private const double GroupSpeed = 1.6;

    // The map the crowd's news are of, the other players and the groups away there (sprint 118); the clock's slot the groups stand in.
    private readonly MapCrowd _crowd = new();
    private string? _crowdMap;
    private long _slot = -1;

    /// <summary>Whether the groups wander: not during the self-tests, which find them at home.</summary>
    private bool Roams => !_options.TownSelfTest && !_options.LobbySelfTest && _options.Screenshot is null;

    /// <summary>
    /// Each frame on a map (sprint 118, D74): at each new slot of the clock, the groups away leave, those back come back,
    /// and each group walks to where <see cref="Roam"/> puts it, the same for every player; but for the one the player
    /// is walking up to. Still during the self-tests.
    /// </summary>
    private void RoamGroups(double delta)
    {
        if (_town is null || _townView is null || !Roams)
            return;
        long slot = Roam.Slot(DateTimeOffset.UtcNow);
        if (slot == _slot)
            return;
        _slot = slot;
        ShowGroupsAway();
        Regroup(Roam.Places(GameData.Embedded, _town.Town, _town.Board, slot), walk: true);
    }

    // The groups to their places: two rounds, so that a group whose place another one leaves gets there too.
    private void Regroup(IReadOnlyDictionary<string, Cell> places, bool walk)
    {
        for (int round = 0; round < 2; round++)
        {
            foreach (string id in _town!.Groups.Keys.ToList())
            {
                Cell from = _town.Groups[id];
                if (_town.FightGroup?.Id == id || !places.TryGetValue(id, out Cell to) || to == from || to == _town.Position || !_town.IsFree(to))
                    continue;
                IReadOnlyList<Cell>? path = Wander.FindPath(_town.Board, from, to, c => c == from || _town.IsFree(c));
                if (path is not { Count: > 0 })
                    continue;
                _town.MoveGroup(id, to);
                if (walk)
                    _ = _townView!.MoveGroup(id, path, GroupSpeed);
            }
        }
    }

    /// <summary>The groups fought or beaten by someone off the map, those back on it (sprint 118).</summary>
    private void ShowGroupsAway()
    {
        if (_town is null || _townView is null)
            return;
        DateTimeOffset now = DateTimeOffset.UtcNow;
        IReadOnlyDictionary<string, Cell>? places = null;
        foreach (MonsterGroup g in GameData.Embedded.GroupsOn(_town.Town.Id))
        {
            bool away = _crowd.IsAway(g.Id, now) || Presence.IsAway(g.Id, now);
            if (away && _town.Groups.ContainsKey(g.Id) && _town.FightGroup?.Id != g.Id)
            {
                _town.RemoveGroup(g.Id);
                _townView.HideGroup(g.Id);
            }
            else if (!away && !_town.Groups.ContainsKey(g.Id) && _town.Town.Dungeon is null)
            {
                places ??= Roam.Places(GameData.Embedded, _town.Town, _town.Board, Roam.Slot(now));
                if (_town.AddGroup(g.Id, places[g.Id]))
                    _townView.ShowGroup(g.Id, _town.Groups[g.Id]);
            }
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
        _bar.MiniMapPressed += ToggleMiniMap;
        _bar.ShowMiniMap(_miniMap?.Visible ?? false);
    }

    private MiniMapPanel? _miniMap;

    /// <summary>The mini-map (sprint 108): as the last game left it, shown by default; the self-tests keep nothing.</summary>
    private void ShowMiniMap()
    {
        bool selfTest = _options.TownSelfTest || _options.LobbySelfTest;
        _miniMap = new MiniMapPanel();
        AddChild(_miniMap);
        _miniMap.Build(_talkPanel!.Texts, selfTest ? new MiniMapSettings() : MiniMapPanel.Load(), MiniScene, keep: !selfTest);
        _miniMap.WalkTo += cell => _ = Walk(cell);
        _bar?.ShowMiniMap(_miniMap.Visible);
    }

    /// <summary>What the mini-map shows of the map: its groups where they are, the spots growing back, the character's trades and quests, the goal followed.</summary>
    private MiniScene MiniScene() => new(_town!.Town, _town.Groups, _growing.Ids, _character, _town.Book, FollowedGoal());

    /// <summary>N or its icon: shows or hides the mini-map.</summary>
    private void ToggleMiniMap()
    {
        _miniMap?.Toggle();
        _bar?.ShowMiniMap(_miniMap?.Visible ?? false);
    }

    /// <summary>Each frame on a map: the mini-map on the player as drawn, facing where they walk.</summary>
    private void FollowMiniMap()
    {
        if (_miniMap is not { Visible: true } || _townView is null || _town is null)
            return;
        Vector3 at = _townView.Player.Position;
        float turn = _townView.Player.GetChildCount() > 0 && _townView.Player.GetChild(0) is Node3D model ? model.Rotation.Y : 0;
        _miniMap.Follow(_zone, at.X, at.Z, new Vector2(Mathf.Sin(turn), Mathf.Cos(turn)));
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
        bool before = _mapPanel is not null || _pointsPanel is not null || _treePanel is not null || _inventoryPanel is not null || _journalPanel is not null || _jobsPanel is not null || _contactsPanel is not null || _guildPanel is not null || _bestiaryPanel is not null || _achievementsPanel is not null;
        if (open != GameWindow.Map && _mapPanel is not null)
            ToggleMap();
        bool? spells = open switch { GameWindow.Spells => true, GameWindow.Characteristics => false, _ => null };
        if (_pointsPanel is not null && (_pointsPanel.Only != spells || open == GameWindow.Talents))
            ClosePoints();
        if (open != GameWindow.Inventory && _inventoryPanel is not null)
            CloseInventory();
        if (open != GameWindow.Journal && _journalPanel is not null)
            CloseJournal();
        if (open != GameWindow.Jobs && _jobsPanel is not null && _town?.Bench is null)
            CloseJobs();
        if (open != GameWindow.Contacts && _contactsPanel is not null)
            CloseContacts();
        if (open != GameWindow.Talents && _treePanel is not null)
            CloseTree();
        if (open != GameWindow.Guild && _guildPanel is not null)
            CloseGuild();
        if (open != GameWindow.Bestiary && _bestiaryPanel is not null)
            CloseBestiary();
        if (open != GameWindow.Achievements && _achievementsPanel is not null)
            CloseAchievements();
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
            case GameWindow.Contacts when _contactsPanel is null:
                OpenContacts();
                break;
            case GameWindow.Talents when _treePanel is null:
                OpenTree();
                break;
            case GameWindow.Guild when _guildPanel is null:
                OpenGuild();
                break;
            case GameWindow.Bestiary when _bestiaryPanel is null:
                OpenBestiary();
                break;
            case GameWindow.Achievements when _achievementsPanel is null:
                OpenAchievements();
                break;
        }
        _bar?.Show(open);
        // The talent points left to spend, on the tree's icon (sprint 151, G9 a).
        if (_hero is not null && _bar is not null)
        {
            int left = Talents.Points(_hero.Level) - (_hero.Talents ?? []).Where(GameData.Embedded.Talents.ContainsKey).Sum(id => GameData.Embedded.Talents[id].Cost);
            _bar.SetBadge(GameWindow.Talents, left, (_talkPanel?.Texts ?? _hud.Texts)["tree.badge", left]);
        }
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
        bool covered = _windows.Open is not null || _shopPanel is not null || _jobsPanel is not null || _chestPanel is not null;
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
            case InputEventKey { Pressed: true, Echo: false, CtrlPressed: false, AltPressed: false, Keycode: Key.N }:
                ToggleMiniMap();
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
            // A right click on another player (sprint 119): invite them, offer a trade.
            case InputEventMouseButton { Pressed: true, ButtonIndex: MouseButton.Right } click when BoardView.Pick(_camera, click.Position) is Cell cell
                && _townView.OtherAt(cell) is Guid someone && _crowd.Others.FirstOrDefault(o => o.Id == someone) is OtherPlayer other:
                OpenPlayerMenu(other, click.Position);
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

    /// <summary>The player's walking pace, in cells a second: 20 % faster since sprint 83 (D66), 10 % more since sprint 115.</summary>
    private const double WalkSpeed = 5.544;

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
        _chatBox.LinkClicked += id =>
        {
            if (GameData.Embedded.Items.TryGetValue(id, out Item? item))
                OpenItemSheet(item, ItemAction.Recipes);
        };
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
            // Talents of an older tree given back at the sign-in (sprint 146, D82): said once, as it comes into play.
            if (_lobby.TakeTalentsGivenBack(character) && _character is not null)
            {
                _chatLog.Note(texts["talents.reset", _character.Name]);
                _talkPanel?.ShowNotice(texts["talents.reset", _character.Name]);
            }
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
        // "/gu" first: to the guild (sprint 145); "/g" or "/p": to the party (sprint 119).
        (string channel, string text) = ChatCommands.Route(line);
        await _chat.Say(text, channel);
    }

    /// <summary>What the chat brought since the last frame, on screen.</summary>
    private void DrainChat()
    {
        bool any = false, news = false;
        while (_chat is not null && _chat.TryRead(out ChatEvent? e))
        {
            // The party and the trades (sprint 119).
            if (e!.Party is PartyNews party)
            {
                OnParty(party);
                continue;
            }
            if (e.Trade is TradeNews trade)
            {
                OnTrade(trade);
                continue;
            }
            // The guild (sprint 145).
            if (e.Guild is GuildNews guild)
            {
                OnGuild(guild);
                continue;
            }
            // A friend came into play (sprint 126): a note, unless the player asked not to.
            if (e.Contact is ContactNews contact)
            {
                if (NotifyFriends)
                {
                    Texts t = _talkPanel?.Texts ?? _hud.Texts;
                    _chatLog!.Note(t["contacts.came", contact.Name, contact.Zone is string z ? t.ZoneName(z, GameData.Embedded) : ""]);
                    any = true;
                }
                continue;
            }
            // The fights with the party (sprint 121).
            if (e.Shared is SharedFightNews shared)
            {
                OnShared(shared);
                continue;
            }
            // The map's news (sprint 118): who is here and where, the groups away, the spots taken.
            if (e.News is MapNews n)
            {
                if (_character is not null)
                    _crowd.Apply(n, _character.Id, _growing);
                news = true;
                continue;
            }
            _chatLog!.Add(e);
            any = true;
        }
        if (any)
            _chatBox?.Refresh(fresh: true);
        if (news && _townView is not null)
        {
            _townView.ShowOthers(_crowd.Others, WalkSpeed);
            _townView.ShowGrowing(_growing.Ids);
            ShowGroupsAway();
        }
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
        // The map's other players see where the walk goes (sprint 118).
        if (step.Waypoints.Count > 0 && _chat is { IsOpen: true } chat)
            _ = TellWalk(chat, step.Waypoints[^1]);
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
        else if (_town.Shut is ZoneLink shut)
        {
            _sound?.Play("error");
            // A hall's far door, or since sprint 129 a way that waits for a quest (Clairval's east gate, docs/textes-v7.md §5.4).
            _talkPanel.ShowMessage(_talkPanel.Texts[shut.Quest is null ? "hall.shut" : "way.shut"]);
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

    // The quests hidden in the journal (sprint 116, D77), kept for each character like the quest followed; and what
    // the journal shows (its tab, search, filters, folded zones), kept between two openings.
    private HashSet<string> _hidden = [];
    private const string HiddenFile = "user://hidden-quests.txt";
    private readonly JournalView _journalView = new();
    private QuestTracker? _tracker;
    private EdgeArrow? _trailArrow;

    /// <summary>The quest followed, as it is under way, or none.</summary>
    private QuestProgress? Followed()
    {
        Kept();
        if (_character is null || _town?.Book is not QuestBook book)
            return null;
        // A hidden quest is never followed (sprint 116): the first under way that is not.
        return book.Active.FirstOrDefault(p => p.Quest == _followed && !_hidden.Contains(p.Quest)) ?? book.Active.FirstOrDefault(p => !_hidden.Contains(p.Quest));
    }

    // The quest followed and the quests hidden, as kept on this computer for the character, read once for it.
    private void Kept()
    {
        if (_character is null || _followedFor == _character.Id)
            return;
        _followedFor = _character.Id;
        _followed = null;
        _hidden = [];
        if (_options.TownSelfTest || _options.LobbySelfTest)
            return;
        try
        {
            if (Godot.FileAccess.FileExists(FollowedFile))
            {
                using var file = Godot.FileAccess.Open(FollowedFile, Godot.FileAccess.ModeFlags.Read);
                string[] kept = file.GetAsText().Split(' ', StringSplitOptions.RemoveEmptyEntries);
                if (kept.Length == 2 && kept[0] == _character.Id.ToString())
                    _followed = kept[1];
            }
            if (Godot.FileAccess.FileExists(HiddenFile))
            {
                using var file = Godot.FileAccess.Open(HiddenFile, Godot.FileAccess.ModeFlags.Read);
                _hidden = HiddenQuests.Read(file.GetAsText(), _character.Id);
            }
        }
        catch (IOException)
        {
            // Nothing kept: the first quest under way, none hidden.
        }
    }

    /// <summary>Hides a quest in the journal, or shows it again (sprint 116), kept for the character.</summary>
    private void Hide(string quest, bool hidden)
    {
        Kept();
        if (!(hidden ? _hidden.Add(quest) : _hidden.Remove(quest)))
            return;
        if (_character is not null && !_options.TownSelfTest && !_options.LobbySelfTest)
        {
            string? kept = null;
            if (Godot.FileAccess.FileExists(HiddenFile))
            {
                using var old = Godot.FileAccess.Open(HiddenFile, Godot.FileAccess.ModeFlags.Read);
                kept = old?.GetAsText();
            }
            using var file = Godot.FileAccess.Open(HiddenFile, Godot.FileAccess.ModeFlags.Write);
            file?.StoreString(HiddenQuests.Write(kept, _character.Id, _hidden));
        }
        ShowTrail();
        ShowJournal();
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
        // A waystone (sprint 127): its window, to travel.
        if (prop.Look == Waystones.Look)
        {
            await OpenWaystones();
            return;
        }
        // A notice board (sprint 153): its window, the day's contracts.
        if (prop.Look == "notice-board")
        {
            await OpenContracts();
            return;
        }
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

    // The contacts (sprint 126): the window, and whether to be told of a friend coming into play (user://contacts.txt).
    private ContactsPanel? _contactsPanel;
    private const string NotifyFile = "user://contacts.txt";

    private static bool NotifyFriends
    {
        get => !Godot.FileAccess.FileExists(NotifyFile) || Godot.FileAccess.GetFileAsString(NotifyFile).Trim() != "off";
        set
        {
            using Godot.FileAccess? f = Godot.FileAccess.Open(NotifyFile, Godot.FileAccess.ModeFlags.Write);
            f?.StoreString(value ? "on" : "off");
        }
    }

    /// <summary>The contacts window (sprint 126): the server's lists, each change asked of it and shown as it answers.</summary>
    private void OpenContacts()
    {
        Texts texts = _talkPanel?.Texts ?? _hud.Texts;
        _contactsPanel = new ContactsPanel();
        AddChild(_contactsPanel);
        _contactsPanel.Build(texts, NotifyFriends);
        _contactsPanel.ClosePressed += () =>
        {
            _windows.Close();
            ShowWindows();
        };
        _contactsPanel.NotifyChanged += on => NotifyFriends = on;
        _contactsPanel.AddPressed += (name, ignore) => _ = Contacts(s => s.AddContact(_character!.Id, name, ignore));
        _contactsPanel.RemovePressed += other => _ = Contacts(s => s.RemoveContact(_character!.Id, other));
        _contactsPanel.InvitePressed += name => _ = Social(s => s.Invite(_character!.Id, name));
        _contactsPanel.WhisperPressed += name =>
        {
            _windows.Close();
            ShowWindows();
            _chatBox?.WhisperTo(name);
        };
        _ = Contacts(s => s.Contacts(_character!.Id));
    }

    // One call about the contacts; the window shows the lists it gives back, or what went wrong.
    private async Task Contacts(Func<GameServer, Task<ContactList>> call)
    {
        Texts texts = _talkPanel?.Texts ?? _hud.Texts;
        if (_lobby?.SignedIn != true || _character is null)
        {
            _contactsPanel?.Tell(texts["contacts.needs-server"]);
            return;
        }
        try
        {
            ContactList list = await call(_lobby.Server);
            _contactsPanel?.Show(list);
            _contactsPanel?.Tell("");
        }
        catch (Exception e) when (e is ServerException or HttpRequestException)
        {
            _contactsPanel?.Tell(e is ServerException refused ? texts["result.refused", refused.Message] : texts["lobby.unreachable"]);
        }
    }

    // The notice board's window (sprint 153), opened by examining a board.
    private ContractsPanel? _contractsPanel;

    private async Task OpenContracts()
    {
        Texts texts = _talkPanel!.Texts;
        if (_lobby?.SignedIn != true || _character is null)
        {
            _talkPanel.ShowMessage(texts["contracts.needs-server"]);
            return;
        }
        _contractsPanel?.QueueFree();
        _contractsPanel = new ContractsPanel();
        AddChild(_contractsPanel);
        _contractsPanel.Build(texts);
        _contractsPanel.ClosePressed += CloseContracts;
        _contractsPanel.HandInPressed += index => _ = Contract(async s =>
        {
            ContractReward reward = await s.HandInContract(_character.Id, index);
            KeepCharacter(reward.Character);
            ShowContracts(reward.Board);
            _contractsPanel?.Tell(texts["contracts.done", reward.Xp, reward.Shards]);
        });
        _contractsPanel.ChangePressed += index => _ = Contract(async s =>
        {
            ShowContracts(await s.ChangeContract(_character.Id, index));
            KeepCharacter((await s.Characters()).Single(c => c.Id == _character.Id));
        });
        await Contract(async s => ShowContracts(await s.Contracts(_character.Id)));
    }

    private void ShowContracts(ContractBoard board) => _contractsPanel?.Show(board, _character?.Level ?? 1, _character?.Inventory ?? []);

    // One call about the contracts; what went wrong said in their window.
    private async Task Contract(Func<GameServer, Task> call)
    {
        Texts texts = _talkPanel!.Texts;
        try
        {
            await call(_lobby!.Server);
        }
        catch (Exception e) when (e is ServerException or HttpRequestException)
        {
            _contractsPanel?.Tell(e is ServerException refused ? texts["result.refused", refused.Message] : texts["lobby.unreachable"]);
        }
    }

    private void CloseContracts()
    {
        _contractsPanel?.QueueFree();
        _contractsPanel = null;
    }

    // A waystone's window (sprint 127), opened by examining one.
    private WaystonePanel? _waystonePanel;

    private async Task OpenWaystones()
    {
        Texts texts = _talkPanel!.Texts;
        if (_lobby?.SignedIn != true || _character is null)
        {
            _talkPanel.ShowMessage(texts["waystone.needs-server"]);
            return;
        }
        _waystonePanel?.QueueFree();
        _waystonePanel = new WaystonePanel();
        AddChild(_waystonePanel);
        _waystonePanel.Build(texts, _zone);
        _waystonePanel.ClosePressed += CloseWaystones;
        _waystonePanel.TravelPressed += id => _ = Waystone(async s =>
        {
            CharacterSummary moved = await s.TravelTo(_character.Id, id);
            KeepCharacter(moved);
            CloseWaystones();
            GoToPlace(moved.Place!, texts["waystone.arrived", texts.ZoneName(Rpg.Core.Contacts.ZoneOf(moved.Place!.Town, GameData.Embedded), GameData.Embedded)]);
        });
        _waystonePanel.HomePressed += id => _ = Waystone(async s => _waystonePanel?.Show(await s.MakeHome(_character.Id, id), _character.Shards));
        await Waystone(async s => _waystonePanel?.Show(await s.Waystones(_character.Id), _character.Shards));
    }

    // One call about the waystones; what went wrong said in their window.
    private async Task Waystone(Func<GameServer, Task> call)
    {
        Texts texts = _talkPanel!.Texts;
        try
        {
            await call(_lobby!.Server);
        }
        catch (Exception e) when (e is ServerException or HttpRequestException)
        {
            _waystonePanel?.Tell(e is ServerException refused ? texts["result.refused", refused.Message] : texts["lobby.unreachable"]);
        }
    }

    private void CloseWaystones()
    {
        _waystonePanel?.QueueFree();
        _waystonePanel = null;
    }

    /// <summary>
    /// Taken somewhere by the server (a waystone, the recall scroll, sprint 127): the map built there, the player on its
    /// cell, a word about it.
    /// </summary>
    private void GoToPlace(Place place, string said)
    {
        (_zone, _place) = (place.Town, place.Cell);
        ClearScene();
        ShowTown();
        _talkPanel?.ShowNotice(said);
        _ = RefreshGrowing();
    }

    private void CloseContacts()
    {
        _contactsPanel?.QueueFree();
        _contactsPanel = null;
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
        _pointsPanel.SavePressed += p => _ = SavePoints(p, _pointsPanel);
        _pointsPanel.ClosePressed += () =>
        {
            _windows.Close();
            ShowWindows();
        };
        // The Talents tab's button: the tree's window, with the draft as it is (sprint 148, K2 b).
        _pointsPanel.TreePressed += () =>
        {
            _carried = _pointsPanel?.Editor;
            _windows.Toggle(GameWindow.Talents);
            ShowWindows();
        };
    }

    /// <summary>The talent tree's window (sprint 148, K2 b), over the town: the T key, the Menus panel or the Talents tab.</summary>
    private void OpenTree()
    {
        if (_treePanel is not null || _hero is null)
            return;
        PointsEditor editor = _carried ?? new PointsEditor(_hero, GameData.Embedded, readOnly: !InTown);
        _carried = null;
        _treePanel = new TreePanel();
        AddChild(_treePanel);
        _treePanel.Build(editor, _talkPanel?.Texts ?? _hud.Texts);
        _treePanel.SavePressed += p => _ = SavePoints(p, _treePanel);
        _treePanel.ClosePressed += () =>
        {
            _windows.Close();
            ShowWindows();
        };
        // The three guided bubbles at the first opening from level 30, once an account (sprint 149, M6 a).
        _treePanel.TutorialDone += () => _ = MarkSeen(Seen.TreeTutorial);
        if (editor.Hero.Level >= Talents.Opens && !editor.ReadOnly)
            _ = TutorialIfUnseen(_treePanel);
    }

    // What this game has seen without a server (offline, or before the server answers).
    private static readonly HashSet<string> SeenHere = [];

    private async Task TutorialIfUnseen(TreePanel panel)
    {
        bool seen = SeenHere.Contains(Seen.TreeTutorial);
        if (!seen && _lobby?.SignedIn == true)
        {
            try
            {
                seen = (await _lobby.Server.SeenFlags()).Contains(Seen.TreeTutorial);
            }
            catch (Exception e) when (e is ServerException or HttpRequestException)
            {
                // Without the server's word, shown: better twice than never.
            }
        }
        if (!seen && _treePanel == panel && IsInstanceValid(panel))
            panel.StartTutorial();
    }

    private static async Task MarkSeen(string flag)
    {
        SeenHere.Add(flag);
        if (_lobby?.SignedIn != true)
            return;
        try
        {
            await _lobby.Server.MarkSeen(flag);
        }
        catch (Exception e) when (e is ServerException or HttpRequestException)
        {
            // Kept here for this game; asked again next time.
        }
    }

    private void CloseTree()
    {
        _treePanel?.QueueFree();
        _treePanel = null;
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

    /// <summary>Food eaten from the inventory (sprint 101), or from its right click anywhere (sprint 117), on the server's word.</summary>
    private async Task Eat(Item item)
    {
        InventoryPanel? panel = _inventoryPanel;
        if (_lobby?.SignedIn != true || _character is null)
            return;
        Texts texts = panel?.Texts ?? _talkPanel?.Texts ?? _hud.Texts;
        void Tell(string text)
        {
            if (panel is not null)
                panel.ShowMessage(text);
            else
                _talkPanel?.ShowNotice(text);
        }
        try
        {
            CharacterSummary after = await _lobby.Server.Eat(_character.Id, item.Id);
            KeepCharacter(after);
            // The recall scroll (sprint 127): home, wherever it is.
            if (item.Id == Waystones.Recall && after.Place is Place home)
            {
                CloseInventory();
                _windows.Close();
                GoToPlace(home, texts["waystone.recalled", texts.ZoneName(Rpg.Core.Contacts.ZoneOf(home.Town, GameData.Embedded), GameData.Embedded)]);
                return;
            }
            string eaten = texts["life.eaten", item.Name.In(texts.Lang), item.Heal];
            if (panel is not null)
                panel.Saved(_hero!, _character.Inventory ?? [], eaten);
            else
                Tell(eaten);
        }
        catch (Exception e) when (e is ServerException or HttpRequestException)
        {
            Tell(e is ServerException refused ? texts["result.refused", refused.Message] : texts["lobby.unreachable"]);
        }
    }

    // The party (sprint 119): as the server last said, its banner, the questions of the other players, the trade open.
    private static PartyNews? _party;
    private static Guid? _partyOf;
    private PartyBanner? _partyBanner;
    private AskPanel? _askPanel;
    private TradePanel? _tradePanel;
    private PopupMenu? _playerMenu;

    private void ShowParty()
    {
        Texts texts = _talkPanel!.Texts;
        _partyBanner = new PartyBanner();
        AddChild(_partyBanner);
        _partyBanner.Build(texts);
        _partyBanner.LeavePressed += () => _ = Social(s => s.LeaveParty(_character!.Id));
        _partyBanner.KickPressed += member => _ = Social(s => s.KickFromParty(_character!.Id, member));
        _partyBanner.JoinPressed += JoinShared;
        _askPanel = new AskPanel();
        AddChild(_askPanel);
        _askPanel.Build();
        if (_character is not null && _partyOf != _character.Id)
        {
            (_party, _partyOf) = (null, _character.Id);
            _ = RefreshParty();
        }
        _partyBanner.Show(_party, _character?.Id);
        ShowGuild();
    }

    private async Task RefreshParty()
    {
        if (_lobby?.SignedIn != true || _character is null)
            return;
        try
        {
            OnParty(await _lobby.Server.Party(_character.Id));
        }
        catch (Exception e) when (e is ServerException or HttpRequestException)
        {
            // No party shown; the next news will say.
        }
    }

    /// <summary>A call about the party or a trade, on the server's word; its refusal said in the chat.</summary>
    private async Task Social(Func<GameServer, Task> call)
    {
        if (_lobby?.SignedIn != true || _character is null)
            return;
        try
        {
            await call(_lobby.Server);
        }
        catch (Exception e) when (e is ServerException or HttpRequestException)
        {
            Texts texts = _talkPanel?.Texts ?? _hud.Texts;
            _chatLog?.Note(e is ServerException refused ? texts["result.refused", refused.Message] : texts["lobby.unreachable"]);
            _chatBox?.Refresh(fresh: true);
        }
    }

    /// <summary>The menu of another player (sprint 119): invite them into the party, offer them a trade.</summary>
    private PopupMenu OpenPlayerMenu(OtherPlayer other, Vector2 at)
    {
        _playerMenu?.QueueFree();
        Texts texts = _talkPanel!.Texts;
        _playerMenu = new PopupMenu();
        _playerMenu.AddItem(other.Name, 0);
        _playerMenu.SetItemDisabled(0, true);
        _playerMenu.AddSeparator();
        _playerMenu.AddItem(texts["party.invite"], 1);
        _playerMenu.AddItem(texts["party.trade"], 2);
        // Into one's guild (sprint 145), for its leader and officers.
        if (_guild is not null && _character is not null && GuildPage.CanInvite(_guild, _character.Id))
            _playerMenu.AddItem(texts["guild.invite-into", GuildName ?? ""], 3);
        _playerMenu.IdPressed += id =>
        {
            if (id == 1)
                _ = Social(async s =>
                {
                    await s.Invite(_character!.Id, other.Name);
                    Note(texts["party.sent", other.Name]);
                });
            else if (id == 2)
                _ = Social(async s =>
                {
                    await s.AskTrade(_character!.Id, other.Id);
                    Note(texts["trade.sent", other.Name]);
                });
            else if (id == 3)
                _ = GuildCall(async s =>
                {
                    await s.InviteToGuild(_character!.Id, other.Name);
                    Note(texts["party.sent", other.Name]);
                });
        };
        AddChild(_playerMenu);
        _playerMenu.Popup(new Rect2I((Vector2I)at, Vector2I.Zero));
        return _playerMenu;
    }

    private void Note(string text)
    {
        _chatLog?.Note(text);
        _chatBox?.Refresh(fresh: true);
    }

    /// <summary>What the server says of the party: someone invites, the party now, no party any more.</summary>
    private void OnParty(PartyNews news)
    {
        Texts texts = _talkPanel?.Texts ?? _hud.Texts;
        switch (news.Party)
        {
            case PartyNewsKind.Invited when news is { From: Guid from, Name: string name }:
                _askPanel?.Ask(texts["party.invited", name], texts["party.join"], texts["party.decline"], () => _ = Social(s => s.JoinParty(_character!.Id, from)));
                break;
            case PartyNewsKind.Changed:
                _party = news;
                _partyBanner?.Show(news, _character?.Id);
                break;
            default:
                if (_party is not null)
                    Note(texts["party.left"]);
                _party = null;
                _partyBanner?.Show(null);
                break;
        }
    }

    /// <summary>What the server says of a trade: asked, opened or changed (the window), made or called off.</summary>
    private void OnTrade(TradeNews news)
    {
        Texts texts = _talkPanel?.Texts ?? _hud.Texts;
        switch (news.Trade)
        {
            case TradeNewsKind.Asked when news is { From: Guid from, Name: string name }:
                _askPanel?.Ask(texts["trade.asked", name], texts["trade.open"], texts["party.decline"], () => _ = Social(s => s.OpenTrade(_character!.Id, from)));
                break;
            case TradeNewsKind.Opened or TradeNewsKind.Changed when news.View is TradeView view:
                if (_tradePanel is null && _character is not null)
                    OpenTrade();
                _tradePanel?.Show(view);
                break;
            case TradeNewsKind.Done:
                CloseTrade();
                Note(texts["trade.done"]);
                _ = Social(async s =>
                {
                    if ((await s.Characters()).FirstOrDefault(c => c.Id == _character!.Id) is CharacterSummary now)
                        KeepCharacter(now);
                });
                break;
            default:
                CloseTrade();
                Note(texts["trade.cancelled"]);
                break;
        }
    }

    private void OpenTrade()
    {
        CharacterSummary me = _character!;
        _tradePanel = new TradePanel();
        AddChild(_tradePanel);
        _tradePanel.Build(new TradeDraft(me.Inventory ?? [], me.Worn ?? new Dictionary<Slot, string>(), me.Shards), me.Inventory ?? [], _talkPanel?.Texts ?? _hud.Texts);
        _tradePanel.OfferChanged += offer => _ = Social(s => s.Offer(me.Id, offer));
        _tradePanel.ReadyPressed += () => _ = Social(async s => KeepCharacter(await s.ReadyTrade(me.Id)));
        _tradePanel.CancelPressed += () => _ = Social(s => s.CancelTrade(me.Id));
    }

    private void CloseTrade()
    {
        _tradePanel?.QueueFree();
        _tradePanel = null;
    }

    // The fight with the party under way (sprint 121): which one, the game's copy of it, the results once over.
    private static Guid? _sharedFight;
    private static SharedFightSync? _sharedSync;
    private static SharedFightNews? _sharedOver;

    // The turn's clock and the fights an ally opened, still to join (sprint 124), kept across the scenes.
    private static readonly TurnClock _turnClock = new();
    private static readonly JoinOffers _joinOffers = new();

    /// <summary>Joins a fight an ally opened, from its question or its line on the party's banner.</summary>
    private void JoinShared(Guid fight)
    {
        _joinOffers.Remove(fight);
        _askPanel?.Hide();
        _ = Social(async s =>
        {
            await s.JoinBattle(_character!.Id, fight);
            _sharedFight = fight;
        });
    }

    /// <summary>
    /// Each frame on a map (sprint 124): a line on the party's banner for each fight an ally opened, with its seconds
    /// left, and crossed swords with them where the group stood on this map.
    /// </summary>
    private void ShowJoins()
    {
        if (_partyBanner is null || _townView is null || _town is null)
            return;
        Texts texts = _talkPanel!.Texts;
        DateTimeOffset now = DateTimeOffset.UtcNow;
        IReadOnlyList<JoinOffer> open = _joinOffers.Open(now);
        _partyBanner.ShowJoins([.. open.Select(o => (o.Fight, texts["battle.join-line", o.Name, JoinOffers.SecondsLeft(o, now)]))]);
        _townView.MarkFights(open.Where(o => o.Group is not null && GameData.Embedded.Groups.TryGetValue(o.Group, out MonsterGroup? g) && g.Map == _town.Town.Id)
            .ToDictionary(o => o.Group!, o => texts["battle.seconds", JoinOffers.SecondsLeft(o, now)]));
    }

    /// <summary>Each frame of a fight with the party (sprint 124): the turn's seconds on the timeline, a sound at five.</summary>
    private void ShowTurnClock()
    {
        DateTimeOffset now = DateTimeOffset.UtcNow;
        _hud.ShowClock(_turnClock.Fighter, _turnClock.SecondsLeft(now), _turnClock.TotalSeconds, _turnClock.Alert(now), _turnClock.Absent);
        if (_turnClock.Sound(now))
            _sound?.Play("open");
    }
    private bool _resyncing;

    /// <summary>Opens a fight with the party; false when the server refuses (the fight goes on alone, as before).</summary>
    private async Task<bool> OpenPartyFight(string scenario)
    {
        Texts texts = _talkPanel!.Texts;
        try
        {
            SharedFightNews opened = await _lobby!.Server.OpenBattle(_character!.Id, scenario);
            _sharedFight = opened.Fight;
            _askPanel?.Ask(texts["battle.waiting"], texts["battle.go"], texts["battle.wait"], () => _ = Social(s => s.GoBattle(_character.Id, opened.Fight)));
            return true;
        }
        catch (Exception e) when (e is ServerException or HttpRequestException)
        {
            return false;
        }
    }

    /// <summary>What the server says of a fight with the party: one opened to join, started, an action, over.</summary>
    private void OnShared(SharedFightNews news)
    {
        Texts texts = _talkPanel?.Texts ?? _hud.Texts;
        // The server's time sets the turn's clock; an opened fight stays to join on the banner (sprint 124).
        DateTimeOffset now = DateTimeOffset.UtcNow;
        _turnClock.Receive(news, now);
        _joinOffers.Receive(news, now);
        switch (news.Shared)
        {
            case SharedFightKind.Opened when news.Name is string name:
                string what = news.Scenario is string sc && GameData.Embedded.Groups.Values.FirstOrDefault(g => g.Scenario == sc) is MonsterGroup g
                    ? texts.GroupLabel(g, GameData.Embedded) : news.Scenario ?? "";
                _askPanel?.Ask(texts["battle.opened", name, what], texts["battle.join"], texts["party.decline"], () => JoinShared(news.Fight));
                break;
            case SharedFightKind.Started when news.Start is SharedFightStart start && _character is not null && _sharedSync?.Start.Fight != news.Fight:
                _askPanel?.Hide();
                StartSharedFight(new SharedFightSync(start, _character.Id, GameData.Embedded, news.Actions), news.Fight);
                break;
            case SharedFightKind.Acted when news is { Number: int n, Action: FightAction action } && _sharedSync?.Start.Fight == news.Fight:
                _sharedSync.Receive(n, action);
                break;
            case SharedFightKind.Over when _sharedSync?.Start.Fight == news.Fight || _sharedFight == news.Fight:
                _sharedOver = news;
                _turnClock.Stop();
                break;
        }
    }

    /// <summary>The fight with the party on screen, from what the server said (the actions so far played at once).</summary>
    private void StartSharedFight(SharedFightSync sync, Guid fight)
    {
        if (InTown)
            _place = _town!.Position;
        (_sharedSync, _sharedFight, _sharedOver) = (sync, fight, null);
        _screen = Screen.Fight;
        _scenario = sync.Start.Scenario;
        _ticket = null;
        _report = null;
        ClearScene();
        _controller = new FightController(sync);
        BuildWorld();
        _busy = false;
    }

    /// <summary>Out of step with the server: the fight built again from its start and every action it played.</summary>
    private async Task Resync()
    {
        if (_sharedFight is not Guid fight || _character is null || _lobby?.SignedIn != true)
            return;
        _resyncing = true;
        try
        {
            SharedFightNews now = await _lobby.Server.Battle(_character.Id, fight);
            if (now.Start is SharedFightStart start)
                StartSharedFight(new SharedFightSync(start, _character.Id, GameData.Embedded, now.Actions), fight);
        }
        catch (Exception e) when (e is ServerException or HttpRequestException)
        {
            // Over meanwhile: the results will say.
        }
        finally
        {
            _resyncing = false;
        }
    }

    private static async Task TellWalk(ChatClient chat, Cell to)
    {
        try
        {
            await chat.WalkTo(to);
        }
        catch (Exception e) when (e is System.Net.WebSockets.WebSocketException or ObjectDisposedException or InvalidOperationException)
        {
            // The socket closed meanwhile: the place saved at the walk's end tells the server anyway.
        }
    }

    private ItemSheet? _itemSheet;
    private PopupMenu? _itemMenu;

    /// <summary>What the last right click on an item offered (for the self-test).</summary>
    private IReadOnlyList<ItemAction> _itemActions = [];

    /// <summary>
    /// An item's tile right-clicked anywhere (sprint 117, D77): its menu at the mouse, with what the item and the place
    /// allow: put on or taken off in the inventory, used, its recipes, its monsters, its set, shared in the chat.
    /// </summary>
    private void ItemRightClicked(Item item, Control tile)
    {
        Texts texts = _talkPanel?.Texts ?? _hud.Texts;
        InventoryPanel? inventory = _inventoryPanel is not null && _inventoryPanel.IsAncestorOf(tile) ? _inventoryPanel : null;
        IReadOnlyList<ItemCount> owned = inventory?.Editor.Owned ?? _character?.Inventory ?? [];
        var place = new ItemPlace(
            owned.Where(i => i.Item == item.Id).Sum(i => i.Count),
            inventory?.Editor.Worn.Values.Contains(item.Id) == true,
            inventory is not null,
            !InTown,
            _chatBox is not null && _lobby?.SignedIn == true && _character is not null);
        _itemActions = new ItemInfo(GameData.Embedded).Actions(item, place);
        if (_itemActions.Count > 0)
            _itemMenu = ItemMenu.Open(this, _itemActions, texts, GetViewport().GetMousePosition(), action => DoItem(action, item));
    }

    private void DoItem(ItemAction action, Item item)
    {
        switch (action)
        {
            case ItemAction.Equip:
                _inventoryPanel?.Put(item);
                break;
            case ItemAction.TakeOff:
                _inventoryPanel?.TakeOff(item);
                break;
            case ItemAction.Use:
                _ = Eat(item);
                break;
            case ItemAction.Share:
                _chatBox?.Share(item);
                break;
            default:
                OpenItemSheet(item, action);
                break;
        }
    }

    /// <summary>An item's sheet, on one of its tabs (sprint 117): over every window, Escape or "Close" closes it.</summary>
    private void OpenItemSheet(Item item, ItemAction tab)
    {
        CloseItemSheet();
        _itemSheet = new ItemSheet();
        AddChild(_itemSheet);
        _itemSheet.Build(item, tab, _talkPanel?.Texts ?? _hud.Texts);
        _itemSheet.ClosePressed += CloseItemSheet;
    }

    private void CloseItemSheet()
    {
        _itemSheet?.QueueFree();
        _itemSheet = null;
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
        // An item shared (sprint 117, D77): its right click puts its name in the field, the line goes as its link, comes
        // back in its colour, and the link opens its sheet.
        Item ring = GameData.Embedded.Items["copper-ring"];
        DoItem(ItemAction.Share, ring);
        string typed = box.FieldText;
        box.Type(typed);
        for (int i = 0; i < 100 && !box.LogCode.Contains("[url=copper-ring]", StringComparison.Ordinal); i++)
        {
            await ToSignal(GetTree().CreateTimer(0.05), SceneTreeTimer.SignalName.Timeout);
            DrainChat();
        }
        string shown = ChatMarkup.Shown(ring, _talkPanel.Texts.Lang);
        if (typed.Trim() != shown || !box.LogCode.Contains("[url=copper-ring]", StringComparison.Ordinal) || !box.LogText.Contains(shown, StringComparison.Ordinal))
        {
            problems.Add($"the ring shared shows \"{box.LogText}\"");
            return " Chat: a line heard back.";
        }
        box.ClickLink("copper-ring");
        if (_itemSheet?.Item != ring)
            problems.Add("the ring's link does not open its sheet");
        CloseItemSheet();
        return " Chat: a line heard back, an item shared as a link that opens its sheet.";
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
            if (_journalPanel is null || !_journalPanel.ListedNames.SequenceEqual([welcome.Name.In(texts.Lang)]) || _journalPanel.ObjectiveLines.Count != welcome.Objectives!.Count)
                problems.Add($"the journal shows {string.Join(", ", _journalPanel?.ListedNames ?? [])}");
            else
                CheckJournal(_journalPanel, welcome, texts, problems);
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
            return " Quests: Aubin's welcome taken, followed in the journal (its card, searched, filtered, folded, hidden and shown again), Garance seen and given it back, her lesson offered.";
        }
        finally
        {
            _town = town;
            _town.Book = QuestBook.Of(_character, GameData.Embedded);
        }
    }

    /// <summary>
    /// The journal as in the games of the kind (sprint 116, D77): the welcome's card with its kind and rewards; found by a
    /// word of its giver, gone for another word or with its kind left out or levels above it; its zone folded; hidden,
    /// under the hidden tab and no longer followed, then shown again.
    /// </summary>
    private void CheckJournal(JournalPanel journal, Quest welcome, Texts texts, List<string> problems)
    {
        string name = welcome.Name.In(texts.Lang);
        if (journal.CardQuest?.Id != welcome.Id || journal.CardQuest.Kind != QuestKind.Tutorial || journal.CardQuest.Rewards?.Contains(texts["journal.xp", welcome.Xp]) != true
            || journal.FollowButton is not { Disabled: true } || journal.HideButton is null)
            problems.Add("the journal's card does not show the welcome, its kind, its rewards, Follow and Hide");
        journal.Search("aubin");
        bool found = journal.ListedNames.SequenceEqual([name]);
        journal.Search("zzzz");
        bool gone = journal.ListedNames.Count == 0 && journal.CardQuest is null;
        journal.Search("");
        journal.KindButtons[QuestKind.Tutorial].ButtonPressed = false;
        bool kindOut = journal.ListedNames.Count == 0;
        journal.KindButtons[QuestKind.Tutorial].ButtonPressed = true;
        journal.MinLevel.Value = 2;
        bool levelOut = journal.ListedNames.Count == 0;
        journal.MinLevel.Value = 1;
        if (!found || !gone || !kindOut || !levelOut || !journal.ListedNames.SequenceEqual([name]))
            problems.Add($"the journal's search and filters: found {found}, gone {gone}, kind left out {kindOut}, level above {levelOut}");
        journal.SectionButtons[texts["journal.village"]].EmitSignal(BaseButton.SignalName.Pressed);
        bool folded = journal.ListedNames.Count == 0 && journal.SectionButtons.ContainsKey(texts["journal.village"]);
        journal.SectionButtons[texts["journal.village"]].EmitSignal(BaseButton.SignalName.Pressed);
        if (!folded || !journal.ListedNames.SequenceEqual([name]))
            problems.Add("the journal's village does not fold and open again");
        journal.HideButton!.EmitSignal(BaseButton.SignalName.Pressed);
        bool hidden = journal.ListedNames.Count == 0 && Followed() is null && _tracker?.TitleText == "";
        journal.TabButtons[JournalTab.Hidden].EmitSignal(BaseButton.SignalName.Pressed);
        bool underHidden = journal.ListedNames.SequenceEqual([name]) && journal.FollowButton is null && journal.HideButton?.Text == texts["journal.show"];
        journal.HideButton?.EmitSignal(BaseButton.SignalName.Pressed);
        journal.TabButtons[JournalTab.Active].EmitSignal(BaseButton.SignalName.Pressed);
        if (!hidden || !underHidden || !journal.ListedNames.SequenceEqual([name]) || Followed()?.Quest != welcome.Id)
            problems.Add($"the journal's hide: hidden {hidden}, under its tab {underHidden}, shown again {journal.ListedNames.Count}");
    }

    private async Task<bool> SavePoints(Points points, IPointsWindow? window = null)
    {
        IPointsWindow panel = window ?? _pointsPanel!;
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
        else if (_town.GuildAsked)
        {
            _town.GuildSeenTo();
            OfferFounding();
        }
        else if (_town.ChestAsked)
        {
            _town.ChestSeenTo();
            OpenChest();
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
        string? followed = Followed()?.Quest;
        _journalPanel.Build(JournalBook(), _talkPanel!.Texts, _journalView, _hidden, followed);
        _journalPanel.FollowPressed += id =>
        {
            Follow(id);
            ShowJournal();
        };
        _journalPanel.HidePressed += Hide;
        _journalPanel.ClosePressed += () =>
        {
            _windows.Close();
            ShowWindows();
        };
    }

    private QuestBook JournalBook() => _town?.Book ?? new QuestBook(GameData.Embedded, 1, [], [], []);

    /// <summary>The journal, if open, as the character's quests now are.</summary>
    private void ShowJournal()
    {
        string? followed = Followed()?.Quest;
        _journalPanel?.Show(JournalBook(), _hidden, followed);
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
        ShowJournal();
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
        // In a party (sprint 121): a fight with it, which the members on the map may join for 15 seconds.
        if (_party?.Members is { Count: > 1 } && _lobby?.SignedIn == true && _character is not null && await OpenPartyFight(scenario))
            return;
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
        // People, not talks: Edmée is talked to twice for her two branches (after sprint 151).
        var talked = new HashSet<string>(StringComparer.Ordinal);
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
            if (step.TalkTo is Npc talkTo)
                talked.Add(talkTo.Id);
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
            // With every compound's twin of them (sprint 138), shown with the talents only.
            int twins = GameData.Embedded.Spells.Values.Count(sp => sp.Twin is string t && GameData.Embedded.Class(_hero.Class)?.Spells.Contains(t) == true);
            if (_pointsPanel.SpellTiles != (GameData.Embedded.Class(_hero.Class)?.Spells.Count ?? 0) + twins)
                problems.Add($"the spells tab shows {_pointsPanel.SpellTiles} spells");
            // An element to filter by only when the book has its spells: no Lightning before the talent (Charles, after sprint 144).
            if (!_pointsPanel.ChipsShown.SequenceEqual(new SpellBook(editor).Elements))
                problems.Add($"the spells tab filters by {string.Join(", ", _pointsPanel.ChipsShown)}");
            // Lightning shown only as the saved talents give it (sprint 146, D82, R1): a Transmutation or the Conversion.
            int lightning = Talents.GrimoireOf(_hero, GameData.Embedded).Spells.Count(id => GameData.Embedded.Spells[id].Element == Element.Lightning);
            if (_pointsPanel.OwnTiles(Element.Lightning) != lightning)
                problems.Add($"the spells tab shows {_pointsPanel.OwnTiles(Element.Lightning)} Lightning spells, the talents give {lightning}");
            await CheckDeck(problems);
            // The talents tab sums the hero's talents up, its button opens the tree's window with the draft (sprint 148, K2 b).
            _pointsPanel.ShowTalents();
            if ((_hero.Talents ?? []).Any(id => !_pointsPanel.TalentsSummary.Contains(GameData.Embedded.Talents[id].Name.In(_pointsPanel.Texts.Lang), StringComparison.Ordinal)))
                problems.Add($"the talents tab sums up '{_pointsPanel.TalentsSummary}'");
            // Each tab inside the game's window, its save button in reach (Charles, after 1.0.134).
            foreach (Action show in new Action[] { () => _pointsPanel.ShowTab(spells: false), () => _pointsPanel.ShowTab(spells: true), _pointsPanel.ShowTalents })
            {
                show();
                await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
                await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
                if (!GetViewport().GetVisibleRect().Encloses(_pointsPanel.Window.GetGlobalRect()))
                    problems.Add($"the character's window ({_pointsPanel.Window.GetGlobalRect()}) leaves the game's ({GetViewport().GetVisibleRect()})");
            }
            _pointsPanel.ShowTalents();
            _pointsPanel.PressOpenTree();
            if (_treePanel is null || _pointsPanel is not null || _treePanel.Editor.Hero != _hero)
                problems.Add("the talents tab's button does not open the tree's window alone");
            _windows.Close();
            ShowWindows();
            _pointsPanel?.QueueFree();
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
        string note = $"Town: {talked.Count} people, {lines} lines";
        if (_lobby?.SignedIn == true && _character is not null)
        {
            bool saved = await SavePlace();
            Place? back = (await _lobby.Server.Characters()).Single(c => c.Id == _character.Id).Place;
            if (!saved || back != new Place(_zone, _town!.Position.X, _town.Position.Y))
                problems.Add($"the server keeps the place {back}, not {_town!.Position}");
            else
                note += $", place {_town.Position} saved on the server";
        }
        try
        {
            await CheckTree(problems);
            CheckTransmuted(problems);
        }
        catch (Exception e) when (e is InvalidOperationException or ArgumentException or KeyNotFoundException or NullReferenceException)
        {
            problems.Add($"the tree's window threw {e.GetType().Name}: {e.Message} {e.StackTrace}");
        }
        string world = CheckWorld(problems) + " " + CheckBarAndMenu(problems) + CheckItems(problems) + CheckAchievementsWindow(problems) + CheckTogether(problems) + await CheckChat(problems) + await CheckQuests(problems);
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
    /// <summary>
    /// The achievements' window (sprint 158, D84) fed a character's progress: a tile per category in the general progress,
    /// the dungeons' groups in the tree once opened, a group's cards and the first one not earned in the detail, the cards
    /// earned hidden by the box, a search across the categories, and nothing found for words that are nowhere.
    /// </summary>
    private string CheckAchievementsWindow(List<string> problems)
    {
        Texts texts = _talkPanel!.Texts;
        var view = new AchievementView([.. Achievements.All.Select(a => new AchievementProgress(a.Id, a.Id is "dungeon-boar-wallow" or "waystones-1" ? a.Count : 0, a.Count))], 15, "dungeon-boar-wallow");
        var panel = new AchievementsPanel();
        AddChild(panel);
        panel.Build(texts);
        panel.Show(view);
        int categories = AchievementsPage.Overview(view, texts).Categories.Count;
        if (panel.Shown != AchievementsPanel.Overall || panel.Tiles != categories || panel.TreeRows != 1 + categories)
            problems.Add($"the achievements' overview shows {panel.Tiles} tiles and {panel.TreeRows} rows");
        panel.ShowCategory("Dungeons", "levels-1-15");
        if (panel.TreeRows != 1 + categories + 4 || panel.Names.Count != 4 || panel.Chosen != "dungeon-wolf-den")
            problems.Add($"the dungeons' first group shows {string.Join(", ", panel.Names)}, {panel.Chosen} chosen, in {panel.TreeRows} rows");
        panel.HideEarned(true);
        if (panel.Names.Count != 3)
            problems.Add("the box does not hide the achievements unlocked");
        panel.HideEarned(false);
        panel.Search(texts.Lang == "fr" ? "arbre-mere" : "mother tree");
        int found = panel.Names.Count;
        panel.Search("zzzz");
        if (found != 2 || panel.Names.Count != 0)
            problems.Add($"the search finds {found}, then {panel.Names.Count} for nothing");
        panel.QueueFree();
        return " Achievements: overview, groups, cards, hiding and search checked.";
    }

    /// <summary>
    /// An item's right click and sheet (sprint 117, D77): the boar's bristle offers its recipes and its monsters (and to
    /// share it, signed in); its bestiary lists every monster that leaves it; the poacher's cape shows its set; Escape
    /// closes the sheet.
    /// </summary>
    private string CheckItems(List<string> problems)
    {
        var info = new ItemInfo(GameData.Embedded);
        Item bristle = GameData.Embedded.Items["boar-bristle"], cape = GameData.Embedded.Items["poacher-cape"];
        (Button tile, ItemIcon icon) = ItemIcon.Tile(40);
        icon.Item = bristle;
        AddChild(tile);
        ItemIcon.RightClick(bristle, tile);
        tile.QueueFree();
        bool signedIn = _lobby?.SignedIn == true && _character is not null;
        ItemAction[] expected = signedIn ? [ItemAction.Recipes, ItemAction.Bestiary, ItemAction.Share] : [ItemAction.Recipes, ItemAction.Bestiary];
        if (!_itemActions.SequenceEqual(expected) || _itemMenu is null || _itemMenu.ItemCount != expected.Length)
            problems.Add($"the bristle's right click offers {string.Join(", ", _itemActions)}");
        _itemMenu?.Hide();
        _itemMenu?.EmitSignal(PopupMenu.SignalName.IdPressed, (long)ItemAction.Bestiary);
        if (_itemSheet is not { Tab: ItemAction.Bestiary } sheet || sheet.Lines[ItemAction.Bestiary].Count != info.Drops(bristle.Id).Count
            || sheet.Lines[ItemAction.Recipes].Count != info.Recipes(bristle.Id).Count || sheet.Lines[ItemAction.Set].Count != 0)
            problems.Add("the bristle's sheet does not list its monsters and its recipes");
        OpenItemSheet(cape, ItemAction.Set);
        (ItemSet set, IReadOnlyList<Item> pieces) = info.Set(cape)!.Value;
        if (_itemSheet?.Lines[ItemAction.Set].Count != 1 + pieces.Count + set.Bonuses.Count)
            problems.Add("the cape's sheet does not show its set");
        TownInput(new InputEventKey { Pressed = true, Keycode = Key.Escape });
        if (_itemSheet is not null || _windows.MenuOpen)
            problems.Add("Escape does not close the item's sheet alone");
        CloseItemSheet();
        return " Items: right click, recipes, bestiary and set checked.";
    }

    /// <summary>
    /// Playing together (sprint 119, D74), without a second player: the party's banner with its four lines, the leader
    /// said; an invitation asked and answered; the trade's window, an item put on the table and taken back, the other's
    /// offer and acceptance shown.
    /// </summary>
    private string CheckTogether(List<string> problems)
    {
        Texts texts = _talkPanel?.Texts ?? _hud.Texts;
        var banner = new PartyBanner();
        AddChild(banner);
        banner.Build(texts);
        Guid lead = Guid.NewGuid();
        banner.Show(new PartyNews(PartyNewsKind.Changed, Leader: lead, Members: [new PartyMember(lead, "Plop", 12, "guard", "clairval"), new PartyMember(Guid.NewGuid(), "Plouf", 9, "mage", null)]));
        if (!banner.Shown || banner.Lines.Count != 3 || banner.Lines[1] != texts["party.leader", "Plop", 12] || banner.Lines[2] != texts["party.offline", "Plouf"])
            problems.Add($"the party's banner shows {string.Join(" | ", banner.Lines)}");
        // Leading it (sprint 124): a cross to send Plouf out.
        banner.Show(new PartyNews(PartyNewsKind.Changed, Leader: lead, Members: [new PartyMember(lead, "Plop", 12, "guard", "clairval"), new PartyMember(Guid.NewGuid(), "Plouf", 9, "mage", null)]), lead);
        if (banner.Kicks is not ["Plouf"])
            problems.Add($"the leader can send out {string.Join(", ", banner.Kicks)}");
        banner.Show(null);
        if (banner.Shown)
            problems.Add("the party's banner stays without a party");
        banner.QueueFree();
        var ask = new AskPanel();
        AddChild(ask);
        ask.Build();
        bool joined = false;
        ask.Ask(texts["party.invited", "Plop"], texts["party.join"], texts["party.decline"], () => joined = true);
        bool asked = ask.Question == texts["party.invited", "Plop"];
        ask.Answer(true);
        if (!asked || !joined || ask.Question != "")
            problems.Add("an invitation is not asked and answered");
        ask.QueueFree();
        var trade = new TradePanel();
        AddChild(trade);
        trade.Build(new TradeDraft([new ItemCount("bread", 2)], new Dictionary<Slot, string>(), 10), [new ItemCount("bread", 2)], texts);
        TradeOffer? sent = null;
        trade.OfferChanged += o => sent = o;
        trade.Put("bread");
        trade.Put("bread");
        trade.Put("bread");
        trade.TakeBack("bread");
        trade.Show(new TradeView(Guid.NewGuid(), "Plop", TradeOffer.Nothing, new TradeOffer([new ItemCount("copper-ring", 1)], 5), false, true));
        if (sent?.Items is not [{ Item: "bread", Count: 1 }] || trade.Theirs?.Shards != 5 || trade.Status != texts["trade.they-ready", "Plop"])
            problems.Add($"the trade's window: offer {string.Join(", ", sent?.Items ?? [])}, status '{trade.Status}'");
        trade.QueueFree();
        // An ally attacks a group (sprint 121): asked to join, the group named.
        MonsterGroup group = GameData.Embedded.Groups.Values.First(g => GameData.Embedded.Towns[g.Map].Dungeon is null);
        OnShared(new SharedFightNews(SharedFightKind.Opened, Guid.NewGuid(), "Plop", group.Scenario));
        if (_askPanel?.Question != texts["battle.opened", "Plop", texts.GroupLabel(group, GameData.Embedded)])
            problems.Add($"an ally's fight asks '{_askPanel?.Question}'");
        _askPanel?.Hide();
        // The contacts' window (sprint 126): a friend in play with its zone's name, one offline, one ignored.
        var contacts = new ContactsPanel();
        AddChild(contacts);
        contacts.Build(texts, true);
        Town woods = GameData.Embedded.Towns.Values.First(t => t.Zone is not null && !Shop.InVillage(t.Id));
        contacts.Show(new ContactList([new Contact(Guid.NewGuid(), "Plop", 12, "guard", woods.Zone), new Contact(Guid.NewGuid(), "Plouf", 9, "mage", null)],
            [new Contact(Guid.NewGuid(), "Grincheux", 3, "mage", null)]));
        string[] expected = [texts["contacts.online", "Plop", 12, woods.Name.In(texts.Lang)], texts["contacts.offline", "Plouf", 9], "Grincheux"];
        if (!contacts.Lines.SequenceEqual(expected))
            problems.Add($"the contacts show {string.Join(" | ", contacts.Lines)}");
        contacts.QueueFree();
        // The guild's window (sprint 145): its leader sees the members by rank, its officer offline since two hours, and
        // may name officers, make members, hand its title over and send out; nothing to itself.
        Guid me = _character?.Id ?? Guid.NewGuid();
        DateTimeOffset now = DateTimeOffset.UtcNow;
        var guildPanel = new GuildPanel();
        AddChild(guildPanel);
        guildPanel.Build(texts);
        guildPanel.Show(new GuildView(Guid.NewGuid(), "Les Veilleurs", GuildRules.Ordered([new GuildMember(Guid.NewGuid(), "Plouf", GuildRank.Member, 9, "mage", woods.Zone, now),
            new GuildMember(me, "Moi", GuildRank.Leader, 30, "guard", woods.Zone, now), new GuildMember(Guid.NewGuid(), "Plop", GuildRank.Officer, 12, "guard", null, now.AddHours(-2))])), me, now);
        string zone = woods.Name.In(texts.Lang);
        string[] members = [texts["guild.online", "Moi", 30, texts["guild.rank.Leader"], zone], texts["guild.offline", "Plop", 12, texts["guild.rank.Officer"], texts["guild.seen.hours", 2]],
            texts["guild.online", "Plouf", 9, texts["guild.rank.Member"], zone]];
        if (!guildPanel.Lines.SequenceEqual(members) || guildPanel.Icons["Moi"].Count != 0 || !guildPanel.Icons["Plop"].SequenceEqual(["demote", "hand-over", "kick"])
            || !guildPanel.Icons["Plouf"].SequenceEqual(["promote", "hand-over", "kick"]))
            problems.Add($"the guild shows {string.Join(" | ", guildPanel.Lines)}");
        // Where it stands (sprint 156): level 3 with 50 of 450 points, 85 of the week's 120, the leader's message and crest;
        // the leader's pencil, an officer's none.
        var wolf = new GuildCrest("banner", "wolf-head", "gris-fond", "osmose-cost");
        GuildView standing = new(Guid.NewGuid(), "Les Veilleurs", GuildRules.Ordered([new GuildMember(me, "Moi", GuildRank.Leader, 30, "guard", woods.Zone, now)]),
            GuildRules.Standing(500, 85, 1, 1, 120, "Ce soir, la Combe !", wolf));
        guildPanel.Show(standing, me, now);
        guildPanel.OpenEditor();
        string expectedStanding = $"{texts["guild.level", 3]} · {texts["guild.points", 50, 450, 4]} | {texts["guild.week", 85, 120]} | « Ce soir, la Combe ! »";
        if (guildPanel.StandingText != expectedStanding || guildPanel.CrestShown != wolf || !guildPanel.CanEdit || guildPanel.Composed != wolf)
            problems.Add($"the guild stands as {guildPanel.StandingText}, crest {guildPanel.CrestShown}, editor {guildPanel.CanEdit}");
        guildPanel.Show(standing with { Members = [new GuildMember(me, "Moi", GuildRank.Officer, 30, "guard", woods.Zone, now)] }, me, now);
        if (guildPanel.CanEdit)
            problems.Add("an officer may write the guild's message");
        // The chest at Edmée's (sprint 156): a member sees its two tabs (the level's), takes nothing out of a closed tab,
        // deposits from its bag; the journal says who.
        var chestPanel = new GuildChestPanel();
        AddChild(chestPanel);
        chestPanel.Build(texts);
        string log = GameData.Embedded.Items.Keys.First();
        var chest = new GuildChest([new GuildTab(false, [new ItemCount(log, 5)]), new GuildTab(true, [])], [new GuildMove(now.AddHours(-2), "Plop", log, 5, 0)]);
        string? deposited = null, withdrawn = null;
        chestPanel.DepositPressed += (item, count, tab) => deposited = $"{item}:{count}:{tab}";
        chestPanel.WithdrawPressed += (item, count, tab) => withdrawn = $"{item}:{count}:{tab}";
        chestPanel.Show(new ChestView(chest, GuildRank.Member), "Les Veilleurs", 3, [new ItemCount(log, 2)], now);
        chestPanel.Click(inBag: false, log);
        chestPanel.Click(inBag: true, log);
        if (chestPanel.TabsOpen != 2 || chestPanel.Shown is not [var held] || held != $"{log}:5" || withdrawn is not null || deposited != $"{log}:1:0"
            || chestPanel.Journal is not [var line] || !line.Contains("Plop", StringComparison.Ordinal))
            problems.Add($"the chest shows {chestPanel.TabsOpen} tab(s), {string.Join(", ", chestPanel.Shown)}, deposited {deposited}, took {withdrawn}, journal {string.Join(" | ", chestPanel.Journal)}");
        chestPanel.QueueFree();
        // Out of a guild, after Edmée's answer: the name to found one.
        guildPanel.Show(null, me, now);
        guildPanel.OfferFounding();
        if (guildPanel.Lines.Count != 0 || !guildPanel.FoundingOffered || guildPanel.GuildName.Length != 0)
            problems.Add("out of a guild, the window does not offer to found one");
        guildPanel.QueueFree();
        // Its line on the banner with its seconds, the question closed or not (sprint 124); swords over a group of this map.
        ShowJoins();
        if (_partyBanner is not null && !_partyBanner.JoinLines.Any(l => l == texts["battle.join-line", "Plop", 15] || l == texts["battle.join-line", "Plop", 14]))
            problems.Add($"the banner offers to join: {string.Join(" | ", _partyBanner.JoinLines)}");
        if (_townView!.Groups.Keys.FirstOrDefault() is string here)
        {
            _townView.MarkFights(new Dictionary<string, string> { [here] = texts["battle.seconds", 12] });
            bool marked = _townView.FightMarks.TryGetValue(here, out string? shown) && shown == texts["battle.seconds", 12];
            _townView.MarkFights(new Dictionary<string, string>());
            if (!marked || _townView.FightMarks.Count != 0)
                problems.Add("no swords over a group an ally attacks");
        }
        // A waystone's window (sprint 127): the village's known and home, the lake's to travel to, the others not yet.
        var stones = new WaystonePanel();
        AddChild(stones);
        stones.Build(texts, Shop.Village);
        stones.Show(new WaystoneView(["waystone-clairval", "waystone-lake"], null), 100);
        if (stones.Lines.Count != Waystones.All(GameData.Embedded).Count || stones.Travels is not ["waystone-lake"]
            || stones.Lines.Count(l => l.Contains(texts["waystone.unknown", "@"].Split('@')[1].Trim(), StringComparison.Ordinal)) != Waystones.All(GameData.Embedded).Count - 2)
            problems.Add($"the waystones show {string.Join(" | ", stones.Lines)}, travel to {string.Join(", ", stones.Travels)}");
        stones.QueueFree();
        foreach (JoinOffer o in _joinOffers.Open(DateTimeOffset.UtcNow))
            _joinOffers.Remove(o.Fight);
        ShowJoins();
        return " Together: party banner, invitation, trade window, an ally's fight and its line to join, contacts, guild (standing, crest, chest) and waystones checked.";
    }

    /// <summary>
    /// The tree's window (sprint 148, D82): for each reference build at 45, every node inside the window and the window inside
    /// the game's, each node in the state the build gives it, a node taken then undone, a pair given back asking first for
    /// what hangs on it, Ctrl+S saving nothing unchanged.
    /// </summary>
    private async Task CheckTree(List<string> problems)
    {
        // The town's self-test has no hero of its own: a sentinel stands in.
        Hero hero = _hero?.Class is null ? new Hero("Ondine", "female-a", "sentinel") : _hero;
        Texts texts = _talkPanel?.Texts ?? _hud.Texts;
        foreach (string id in Progression.Builds.Keys)
        {
            // The build as a player takes it, a Conversion in its Transmutation's place.
            Hero built = Progression.Talented(hero with { Level = 45 }, id, GameData.Embedded);
            IReadOnlyList<string> build = built.Talents ?? [];
            var tree = new TreePanel();
            AddChild(tree);
            tree.Build(new PointsEditor(built, GameData.Embedded), texts);
            bool saved = false;
            tree.SavePressed += _ => saved = true;
            await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
            if (!GetViewport().GetVisibleRect().Encloses(tree.Window.GetGlobalRect()))
                problems.Add($"the tree's window ({tree.Window.GetGlobalRect()}) leaves the game's");
            TreeLayout layout = tree.Layout;
            if (layout.Nodes.Any(n => !n.Box.Inside(layout.Width, layout.Height)))
                problems.Add($"{id}: a node of the tree leaves its area");
            int expected = layout.Nodes.Count(n => n.IsTalent ? build.Contains(n.Id) : n.IsTrunk || (n.IsFold && build.Any(b => GameData.Embedded.Talents[b].Branch == n.Branch && GameData.Embedded.Talents[b].Tier == n.Tier)));
            if (tree.Counted.GetValueOrDefault(NodeState.Taken) != expected)
                problems.Add($"{id}: the tree shows {tree.Counted.GetValueOrDefault(NodeState.Taken)} nodes taken, not {expected}");
            if (layout.Nodes.Where(n => n.IsTalent).Any(n => tree.StateOf(n) != TreeLayout.State(build, n.Id, 45, GameData.Embedded)))
                problems.Add($"{id}: a node of the tree shows another state than the rules give");
            // A node taken, then the changes undone: the draft is the build again (I4 a).
            if (layout.Nodes.FirstOrDefault(n => n.IsTalent && tree.StateOf(n) == NodeState.Open) is TreeNode open)
            {
                tree.Press(open.Id);
                if (!tree.Editor.TalentsTaken.Contains(open.Id))
                    problems.Add($"{id}: pressing {open.Id} does not take it");
                tree.Undo();
                if (tree.Editor.TalentsChanged)
                    problems.Add($"{id}: undoing leaves {string.Join(", ", tree.Editor.TalentsTaken)}");
            }
            // A door others hang on asks before it is given back with them (I1 b).
            // (With two pairs, no door alone holds anything up.)
            if (build.FirstOrDefault(b => GameData.Embedded.Talents[b].IsFork && tree.Editor.Losing(GameData.Embedded.Talents[b]).Count > 0) is string door)
            {
                tree.Press(door);
                if (tree.Asking?.Talent != door || !tree.Editor.TalentsTaken.Contains(door))
                    problems.Add($"{id}: giving back {door} does not ask first");
                tree.Confirm();
                if (tree.Editor.TalentsTaken.Contains(door) || tree.Editor.TalentsTaken.Any(b => !Talents.Without(build, door, GameData.Embedded).Contains(b)))
                    problems.Add($"{id}: giving back {door} leaves {string.Join(", ", tree.Editor.TalentsTaken)}");
                tree.Undo();
            }
            // Ctrl+S saves only a draft that changed.
            tree._Input(new InputEventKey { Pressed = true, Keycode = Key.S, CtrlPressed = true });
            if (saved)
                problems.Add($"{id}: Ctrl+S saves a tree that did not change");
            // A node of each kind hovered: its card says its layer and its banner (sprint 149, M2 a); each talent has its icon.
            foreach (TreeNode n in layout.Nodes.Where(n => !n.IsComing).GroupBy(n => (TreeGuide.LayerOf(n, GameData.Embedded), n.Shape, n.IsFold)).Select(g => g.First()))
            {
                tree.Hover(n.Id);
                TreeCard card = texts.TreeCard(n, tree.Editor);
                if (!tree.CardText.Contains(card.Layer, StringComparison.Ordinal) || !tree.CardText.Contains(card.Banner, StringComparison.Ordinal))
                    problems.Add($"{id}: the card of {n.Id} says '{tree.CardText}'");
            }
            tree.Hover(null);
            if (layout.Nodes.FirstOrDefault(n => (n.IsTalent || n.IsTrunk) && TreeNodeView.IconOf(n) is null) is TreeNode bare)
                problems.Add($"{id}: {bare.Id} has no icon");
            // The tutorial: three bubbles, the legend open with them, then done (M6 a, M5 b).
            bool done = false;
            tree.TutorialDone += () => done = true;
            tree.StartTutorial();
            if (tree.TutorialStep != 0 || !tree.LegendShown)
                problems.Add($"{id}: the tutorial starts on bubble {tree.TutorialStep}, the legend {(tree.LegendShown ? "open" : "shut")}");
            for (int b = 0; b < 3; b++)
                tree.NextBubble();
            if (!done || tree.TutorialStep != -1)
                problems.Add($"{id}: the tutorial does not end after its three bubbles");
            tree.ShowLegend(false);
            // Sprint 151: the build's code there and back, a plan never saved, a suggested build a draft, a path under the mouse.
            string code = tree.CopyCode();
            tree.Undo();
            tree.LoadCode(code);
            if (!tree.Editor.TalentsTaken.Order(StringComparer.Ordinal).SequenceEqual(build.Order(StringComparer.Ordinal)))
                problems.Add($"{id}: the code {code} loads {string.Join(", ", tree.Editor.TalentsTaken)}");
            tree.Plan(true);
            TreeNode? planned = tree.Layout.Nodes.FirstOrDefault(n => n.IsTalent && tree.StateOf(n) == NodeState.Open);
            if (planned is not null)
                tree.Press(planned.Id);
            tree.Save();
            if (saved || (tree.Ghosts.Count == 0 && planned is not null))
                problems.Add($"{id}: a plan saves, or shows no ghost");
            tree.Plan(false);
            tree.Suggest(1);
            if (tree.Editor.Hero.Talents is not { } kept || !kept.SequenceEqual(build))
                problems.Add($"{id}: a suggested build changes the saved talents");
            tree.Undo();
            if (layout.Nodes.FirstOrDefault(n => n.IsTalent && n.Tier == Tier.Key && !build.Contains(n.Id)) is TreeNode far)
            {
                tree.Hover(far.Id);
                if (tree.Path.Count == 0 && TreePlan.Path(build, far.Id, GameData.Embedded) is { Count: > 0 })
                    problems.Add($"{id}: hovering {far.Id} lights no path");
                tree.Hover(null);
            }
            // The arrow up goes to the neighbour above where the keyboard starts (K5 a).
            string start = tree.Focused;
            tree.Move(Direction.Up);
            if (tree.Focused != (layout.Neighbour(start, Direction.Up)?.Id ?? start))
                problems.Add($"{id}: the arrow up goes from {start} to {tree.Focused}");
            tree.QueueFree();
        }
    }

    /// <summary>
    /// The spells window after a Transmutation saved (sprint 150, D82): the Fire spells it replaced gone, their Lightning
    /// twins there and marked new until looked at, each saying where it comes from.
    /// </summary>
    private void CheckTransmuted(List<string> problems)
    {
        Texts texts = _talkPanel?.Texts ?? _hud.Texts;
        Hero hero = Progression.Talented(new Hero("Ondine", "female-a", "sentinel", Level: 35), ["electrifying", "transmute-fire-lightning"], GameData.Embedded);
        var panel = new PointsPanel();
        AddChild(panel);
        panel.Build(new PointsEditor(hero, GameData.Embedded), texts, only: true);
        panel.Refresh();
        Grimoire grimoire = Talents.GrimoireOf(hero, GameData.Embedded);
        if (grimoire.Replaced.Count == 0 || grimoire.Replaced.Keys.Any(panel.ShownSpells.Contains))
            problems.Add($"the spells window still shows the spells a Transmutation replaced: {string.Join(", ", grimoire.Replaced.Keys.Where(panel.ShownSpells.Contains))}");
        if (!grimoire.Replaced.Values.All(panel.NewSpells.Contains))
            problems.Add($"the twins are not marked new: {string.Join(", ", panel.NewSpells)}");
        var book = new SpellBook(panel.Editor);
        if (grimoire.Replaced.Values.Any(t => texts.SpellOrigin(book.Spells.Single(b => b.Spell.Id == t), GameData.Embedded).Count < 2))
            problems.Add("a twin does not say where it comes from and what it replaced");
        panel.QueueFree();
    }

    private string CheckBarAndMenu(List<string> problems)
    {
        Texts t = _talkPanel!.Texts;
        bool character = _hero?.Class is not null;
        var tips = WindowState.Bar.Where(b => character || b.Window == GameWindow.Map).Select(b => t["bar.tip", t["bar." + b.Window], b.Key]).Append(t["minimap.tip"]).Append(t["bar.tip", t["bar.menu"], t["menu.key"]]);
        if (!_bar!.Tips.SequenceEqual(tips))
            problems.Add($"the side bar says {string.Join(", ", _bar.Tips)}");
        // A small grid, the way Dofus does it (Charles, after sprint 144): three icons a row, four rows since the tree's (sprint 148) and the guild's (sprint 145), five since the bestiary's (sprint 154) and the achievements' (sprint 155).
        if (_bar.Rows > 5)
            problems.Add($"the menus take {_bar.Rows} rows");
        // Sprint 157 (D84): the tiles in relief of the interface's one theme, every variation of it defined.
        if (_bar.TileStyles.Any(v => v != OsmoseTheme.MenuTile))
            problems.Add($"a menu's tile is not the theme's: {string.Join(", ", _bar.TileStyles)}");
        Theme theme = ThemeDB.GetProjectTheme();
        if (theme is null || OsmoseTheme.Variations.Any(v => theme.GetTypeVariationBase(v).IsEmpty))
            problems.Add("the interface's theme lacks a variation");
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
                GameWindow.Contacts => _contactsPanel is not null && _jobsPanel is null && _journalPanel is null && _inventoryPanel is null && _mapPanel is null,
                GameWindow.Talents => _treePanel is not null && _contactsPanel is null && _pointsPanel is null && _mapPanel is null,
                GameWindow.Guild => _guildPanel is not null && _contactsPanel is null && _jobsPanel is null && _inventoryPanel is null && _mapPanel is null,
                GameWindow.Bestiary => _bestiaryPanel is not null && _guildPanel is null && _inventoryPanel is null && _mapPanel is null,
                GameWindow.Achievements => _achievementsPanel is not null && _bestiaryPanel is null && _inventoryPanel is null && _mapPanel is null,
                _ => _inventoryPanel is not null && _pointsPanel is null && _mapPanel is null && _journalPanel is null && _jobsPanel is null,
            };
            if (!shown || !_bar.Lit(window))
                problems.Add($"{key} does not open the {window} window alone");
            windows++;
        }
        TownInput(new InputEventKey { Pressed = true, Keycode = Key.Escape });
        if (_mapPanel is not null || _pointsPanel is not null || _treePanel is not null || _inventoryPanel is not null || _journalPanel is not null || _contactsPanel is not null || _guildPanel is not null || _bestiaryPanel is not null || _achievementsPanel is not null || _menuPanel is not null)
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
            // The Forest of the Ancestors' own trees (sprint 131): giant trunks among its pines.
            if (t.Zone == "ancestors" && view.Giants == 0)
                problems.Add($"{id} draws no giant trunk");
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
        CheckMiniMap(problems);
        CheckMonsters(problems);
        CheckEdges(problems);
        int doors = CheckDungeons(problems);
        return $"World: {zones.Count} maps, {groups} groups of monsters, {doors} dungeons, camera, map and mini-map checked." + CheckFourWays(problems, hero);
    }

    /// <summary>
    /// Quatre-Chemins (sprint 129): Clairval's east gate shut without "Four Trails", and said why; each of the four maps
    /// drawn with its people and its things to examine (the signpost, the waystone, what shuts the three roads, and since
    /// sprint 153 the notice board).
    /// </summary>
    private string CheckFourWays(List<string> problems, Hero hero)
    {
        var gate = new TownController(GameData.Embedded, "clairval-3-1");
        ZoneLink way = gate.Town.Links!.First(l => l.To == Shop.Hamlet);
        if (gate.Click(way.At) is null || gate.Shut != way || gate.Travel is not null || string.IsNullOrEmpty(_talkPanel!.Texts["way.shut"]))
            problems.Add("Clairval's east gate opens without Four Trails");
        int people = 0, things = 0;
        foreach (Town t in GameData.Embedded.Towns.Values.Where(t => Shop.VillageOf(t.Id) == Shop.Hamlet).OrderBy(t => t.Id, StringComparer.Ordinal))
        {
            var view = new TownView();
            AddChild(view);
            view.Build(new TownController(GameData.Embedded, t.Id), _options.Lang, hero);
            if (view.PropsDrawn != (t.Props ?? []).Count || view.Tiles != t.Rows.Count * t.Rows[0].Length)
                problems.Add($"{t.Id} is drawn with {view.PropsDrawn} things to examine and {view.Tiles} tiles");
            people += t.Npcs.Count;
            things += view.PropsDrawn;
            RemoveChild(view);
            view.QueueFree();
        }
        return $" Quatre-Chemins: {people} people, {things} things to examine, its gate shut until Four Trails.";
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
        // The workshops of Clairval's craftsmen's quarter (sprints 81 and 102): the locksmith's bench, and since sprint 109 the
        // forge and the tailor's workshop. Walked onto, each opens its recipes, none to make without the ingredients; closed,
        // the player walks on.
        _town = new TownController(GameData.Embedded, "clairval-3-1");
        foreach (Workshop bench in _town.Town.Workshops!)
        {
            if (_town.Click(bench.At)?.Path is null || _town.Bench != bench)
            {
                problems.Add($"walking onto the workshop of the {bench.Job} does not open it");
                continue;
            }
            // Since sprint 100, the trades window on the bench's trade, its recipes listed.
            JobsPanel panel = OpenBench(bench);
            var expected = new JobBook(GameData.Embedded, _character, _talkPanel!.Texts, bench.Job);
            IReadOnlyList<RecipeLine> lines = expected.Recipes(GameData.Embedded.Jobs[bench.Job]);
            if (panel.Shown.Id != bench.Job || panel.JobsListed != expected.Jobs.Count || panel.Lines != lines.Count)
                problems.Add($"the {bench.Job}'s workshop opens the trades window on {panel.Shown.Id}, {panel.JobsListed} trades and {panel.Lines} lines");
            // The forge's first piece teaches 165 (sprint 109).
            if (bench.Job == "smith" && lines.SingleOrDefault(l => l.Item.Id == "copper-billhook")?.Xp != 165)
                problems.Add("the forge does not offer the copper billhook for 165 experience");
            CloseJobs();
            _town.CloseDoor();
            if (_town.Click(_town.Town.Spawn) is null)
                problems.Add($"after the {bench.Job}'s workshop, the player cannot walk on");
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
            // The old stone, since sprint 127 the village's waystone, and since sprint 153 the notice board.
            if (view.PropsDrawn != 3 || fresh != "!" || idle || waiting != "?" || !glint || view.ClueShown("old-stone") || view.MarkShown("ysaline") != "?")
                problems.Add($"the square shows {view.PropsDrawn} props, '{fresh}' over Aubin, the stone glinting {idle}/{glint}, '{waiting}' over Ysaline");
            // The life in a heart (sprint 115): the life over the most, the share below; half filled at half.
            var heart = new HeartGauge();
            AddChild(heart);
            heart.Show(30, 100, false);
            string heartText = heart.Text;
            Vector2[] outline = HeartGauge.Outline(new Vector2(96, 88));
            if (heartText != "30/100 30%" || outline.Min(p => p.X) < 0 || outline.Max(p => p.X) > 96 || outline.Min(p => p.Y) < 0 || outline.Max(p => p.Y) > 88)
                problems.Add($"the heart says '{heartText}', drawn from {outline.Min(p => p.Y)} to {outline.Max(p => p.Y)}");
            heart.QueueFree();
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
            journal.Build(book, t, new JournalView(), new HashSet<string>(), stone.Quest);
            if (journal.FollowButton is not { Disabled: true } || journal.CardQuest?.Kind != QuestKind.Main || journal.Rows.Count != 1)
                problems.Add("the journal does not say the stone's quest, a main quest, is followed");
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
        // Where the clock puts it (sprint 118, D74), the same for every player.
        var stay = new TownController(GameData.Embedded, "woods-2-2");
        if (Enumerable.Range(0, 20).Select(slot => (Cell?)Roam.Places(GameData.Embedded, stay.Town, stay.Board, slot)[group.Id])
            .FirstOrDefault(c => c != group.Home && stay.IsFree(c!.Value) && c != stay.Position) is Cell roamed)
        {
            stay.MoveGroup(group.Id, roamed);
            view.PlaceGroup(group.Id, roamed);
            if (view.Groups[group.Id].Position != BoardView.ToWorld(roamed))
                problems.Add($"the group {group.Id} is not drawn where it roamed");
        }
        else
            problems.Add($"the group {group.Id} cannot roam");
        // Another player (sprint 118): drawn where the server says, walking on, gone; a group fought hidden, then back.
        var plop = new OtherPlayer(Guid.NewGuid(), "Plop", new Hero("Plop", "male-a", "guard"), stay.Town.Spawn);
        view.ShowOthers([plop], 4);
        bool drawn = view.Others.TryGetValue(plop.Id, out Node3D? other) && other.Position == BoardView.ToWorld(plop.At);
        view.ShowOthers([plop with { At = plop.At.Neighbours().First(stay.IsFree) }], 4);
        bool walking = view.Others.Count == 1;
        // In a guild now (sprint 145): its name under Plop's, drawn again; out of it, gone.
        view.ShowOthers([plop with { Guild = "Les Veilleurs" }], 4);
        bool guild = view.GuildShownFor(plop.Id) == "‹Les Veilleurs›" && !view.CrestShownFor(plop.Id);
        // With its crest beside it (sprint 156), drawn again.
        view.ShowOthers([plop with { Guild = "Les Veilleurs", Crest = GuildCrests.Write(GuildCrests.Default) }], 4);
        guild &= view.GuildShownFor(plop.Id) == "‹Les Veilleurs›" && view.CrestShownFor(plop.Id);
        view.ShowOthers([plop], 4);
        guild &= view.GuildShownFor(plop.Id) is null && view.Others.Count == 1;
        // Its own guild under its own character too (sprint 152), and gone out of it.
        view.ShowOwnGuild("Les Veilleurs", GuildCrests.Default);
        guild &= view.OwnGuildShown == "‹Les Veilleurs›" && view.OwnCrestShown;
        view.ShowOwnGuild(null);
        guild &= view.OwnGuildShown is null && !view.OwnCrestShown;
        view.ShowOthers([], 4);
        if (!drawn || !walking || !guild || view.Others.Count != 0)
            problems.Add($"another player: drawn {drawn}, walking {walking}, guild's name {guild}, {view.Others.Count} left");
        view.HideGroup(group.Id);
        bool hidden = !view.Groups[group.Id].Visible;
        view.ShowGroup(group.Id, group.Home);
        if (!hidden || !view.Groups[group.Id].Visible)
            problems.Add($"the group {group.Id} is not hidden while fought, then back");
        RemoveChild(view);
        view.QueueFree();
    }

    /// <summary>
    /// The world map of sprint 71: the picture of the whole world, the player's ring on their cell, the
    /// map under it named; zooming keeps the point under the pointer still, between the whole world and
    /// four times; the picture cannot be dragged away.
    /// </summary>
    /// <summary>
    /// The mini-map (sprint 108): shown by default at the top right, its icon lit; on the player, who is its centre
    /// and its legend there; the inhabitants in view named; a click a few cells away walks there, on this map only;
    /// its zoom, sizes and moves kept in their bounds; N hides it and shows it again.
    /// </summary>
    private void CheckMiniMap(List<string> problems)
    {
        MiniMapPanel? panel = _miniMap;
        if (panel is null || !panel.Visible || _bar?.MiniMapLit != true)
        {
            problems.Add("the mini-map is not shown by default, its icon lit");
            return;
        }
        Vector2 screen = GetViewport().GetVisibleRect().Size;
        if (panel.PanelAt.X + panel.ViewSize.X > screen.X - 64 || panel.PanelAt.Y < 100 || panel.PanelAt.Y > screen.Y / 2)
            problems.Add($"the mini-map is at {panel.PanelAt}, not at the top right");
        FollowMiniMap();
        MiniMap mini = panel.Mini;
        Vector2 centre = panel.ViewSize / 2;
        Texts texts = _talkPanel!.Texts;
        if (panel.Marks.Count == 0 || panel.Marks[^1].Kind != MiniMarkKind.Player || panel.LegendAt(centre) != texts["minimap.you"])
            problems.Add($"the mini-map says \"{panel.LegendAt(centre)}\" under the player");
        foreach (MiniMark m in panel.Marks.Where(m => m.Kind == MiniMarkKind.Inhabitant))
        {
            if (!GameData.Embedded.Towns.Values.SelectMany(t => t.Npcs).Any(n => n.Name.In(texts.Lang) == m.Legend))
                problems.Add($"the mini-map shows an inhabitant \"{m.Legend}\"");
        }
        if (_town!.Town.Npcs.Count(n => n.At.DistanceTo(_town.Position) <= 6) is int near && near > 0
            && panel.Marks.Count(m => m.Kind == MiniMarkKind.Inhabitant) < near)
            problems.Add($"the mini-map shows {panel.Marks.Count(m => m.Kind == MiniMarkKind.Inhabitant)} inhabitants, {near} being near");
        // A cell three to the right of the player, a click there.
        Vector2 right = centre + new Vector2(3 * WorldPicture.CellPixels * (float)mini.Scale, 0);
        Cell? walked = mini.WalkTo(right.X, right.Y);
        var standing = new Cell((int)Math.Round(_townView!.Player.Position.X), (int)Math.Round(_townView.Player.Position.Z));
        if (walked != standing + new Cell(3, 0))
            problems.Add($"a click on the mini-map walks to {walked}, not {standing + new Cell(3, 0)}");
        // The zoom, from about 16 cells across to 3 maps, and back.
        int zoom = mini.Zoom;
        while (mini.ZoomIn())
        {
        }
        int tight = mini.Span;
        while (mini.ZoomOut())
        {
        }
        if (tight != MiniMap.Spans[0] || mini.Span != MiniMap.Spans[^1])
            problems.Add($"the mini-map zooms from {tight} to {mini.Span} cells across");
        mini.Zoom = zoom;
        // The three sizes, then the first again; a move kept on screen.
        Vector2 size = panel.ViewSize;
        var sizes = new HashSet<float>();
        for (int i = 0; i < 3; i++)
        {
            panel.NextSize();
            sizes.Add(panel.ViewSize.X);
        }
        if (sizes.Count != 3 || panel.ViewSize != size)
            problems.Add($"the mini-map's sizes are {string.Join(", ", sizes)}");
        Vector2 at = panel.PanelAt;
        panel.MoveBy(new Vector2(-10000, -10000));
        if (panel.PanelAt.Y < 0 || panel.PanelAt.X + panel.ViewSize.X < 0)
            problems.Add($"the mini-map is moved off the screen, to {panel.PanelAt}");
        panel.MoveBy(at - panel.PanelAt);
        TownInput(new InputEventKey { Pressed = true, Keycode = Key.N });
        bool hidden = !panel.Visible && !_bar.MiniMapLit;
        TownInput(new InputEventKey { Pressed = true, Keycode = Key.N });
        if (!hidden || !panel.Visible || !_bar.MiniMapLit)
            problems.Add("N does not hide and show the mini-map");
    }

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
        if (_options.Shot is "achievements" or "achievements-list")
        {
            // The achievements' window (sprint 158): a character of the end of chapter 1, its overview or its first dungeons.
            var progress = Achievements.All.Select((a, i) => new AchievementProgress(a.Id, a.Category is "Exploration" || i % 3 == 0 ? a.Count : a.Count / 2, a.Count)).ToList();
            var view = new AchievementView(progress, Achievements.Points(progress.Where(p => p.Earned).Select(p => p.Id)), "dungeon-boar-wallow");
            _achievementsPanel = new AchievementsPanel();
            AddChild(_achievementsPanel);
            _achievementsPanel.Build(_talkPanel!.Texts);
            _achievementsPanel.Show(view);
            if (_options.Shot == "achievements-list")
                _achievementsPanel.ShowCategory("Dungeons", "levels-1-15");
            _talkPanel.Visible = false;
            _townView!.ShowPath(null);
            for (int i = 0; i < 30; i++)
                await ToSignal(RenderingServer.Singleton, RenderingServer.SignalName.FramePostDraw);
            Error shot = GetViewport().GetTexture().GetImage().SavePng(_options.Screenshot!);
            GD.Print($"SCREENSHOT {shot} {_options.Screenshot}");
            GetTree().Quit(shot == Error.Ok ? 0 : 1);
            return;
        }
        if (_options.Shot == "tree")
        {
            // The tree's window (sprint 148): a guard at 45 with its build of Matter, the Lightning braid taken.
            _hero = Progression.Talented(new Hero("Aubépine", "female-b", "guard", 5, 5, 4, -1, 1, Level: 45), "guard-matter", GameData.Embedded);
            ShowBar();
            ToggleWindow(GameWindow.Talents);
            _townView!.ShowPath(null);
            for (int i = 0; i < 30; i++)
                await ToSignal(RenderingServer.Singleton, RenderingServer.SignalName.FramePostDraw);
            Error tree = GetViewport().GetTexture().GetImage().SavePng(_options.Screenshot!);
            GD.Print($"SCREENSHOT {tree} {_options.Screenshot}");
            GetTree().Quit(tree == Error.Ok ? 0 : 1);
            return;
        }
        if (_options.Shot is "points" or "spells")
        {
            // The character sheet for the project page: a tenth-level guard, points spent, a few items worn.
            _hero = new Hero("Aubépine", "female-b", "guard", 5, 5, 4, -1, 1, Level: 10,
                Stats: new Characteristics(Vitality: 20, Earth: 40, Fire: 0, Water: 9, Air: 10),
                Worn: new Dictionary<Slot, string> { [Slot.Ring1] = "copper-ring", [Slot.Amulet] = "pebble-amulet", [Slot.TwoHanded] = "orc-club", [Slot.Boots] = "poacher-boots", [Slot.Pet] = "kitten" });
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
