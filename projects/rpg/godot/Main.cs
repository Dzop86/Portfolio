using System.Globalization;
using Godot;
using Rpg.Client;
using Rpg.Core;

namespace Rpg.Desktop;

/// <summary>
/// The login and character screen, the town (Main.Town.cs), the fights. Options after "--": --lang
/// fr|en, --scenario ID, --seed N, --server URL (default http://localhost:8002/), --offline (straight
/// to the town); --selftest [--report FILE] plays a whole fight through the controls and checks the
/// views; --town-selftest walks the town and talks to everyone first; --lobby-selftest signs up,
/// creates and chooses a character on the login screen against the server, then does both;
/// --screenshot FILE [--shot move|spell|lobby|town|banner|zone|world] saves a picture; --demo lets the AI play the
/// hero too, animations and all, and quits after the fight (for the video of the project page).
/// </summary>
public partial class Main : Node3D
{
    private enum Screen
    {
        Lobby,
        Town,
        Fight,
    }

    // Kept when the scene reloads ("play again", "back to town"): where the player is, the seed, the
    // session, the character chosen (null offline) and its hero, the fight's scenario.
    private static Screen _screen = Screen.Lobby;
    private static ulong? _nextSeed;

    // The fight the server started (signed in), for the scene about to be built; then the one played.
    private static FightTicket? _pendingTicket;
    private FightTicket? _ticket;
    private Task<string?>? _report;
    private static Lobby? _lobby;
    private static CharacterSummary? _character;
    private static Hero? _hero;
    private static string? _scenario;
    private static string? _launcherToken;

    private readonly Dictionary<int, FighterView> _views = [];
    private FightController _controller = null!;
    private BoardView _board = null!;
    private Hud _hud = null!;
    private Camera3D _camera = null!;
    private Options _options = null!;
    private Cell? _hovered;
    private int _seen;
    private bool _busy;
    private LobbyView? _lobbyView;
    private string? _selfTestNote;
    private Sound? _sound;
    private MenuPanel? _menuPanel;
    private WindowState _windows = new();

    public override void _Ready()
    {
        _options = Options.Parse(OS.GetCmdlineUserArgs(), OS.GetLocaleLanguage());
        // The launcher passes the server and the player's token in the environment (RPG_SERVER, RPG_TOKEN).
        string server = System.Environment.GetEnvironmentVariable("RPG_SERVER") is { Length: > 0 } fromLauncher && !_options.ServerGiven
            ? fromLauncher.TrimEnd('/') + "/"
            : _options.Server;
        if (_lobby is null)
        {
            _lobby = new Lobby(new GameServer(new System.Net.Http.HttpClient { BaseAddress = new Uri(server), Timeout = TimeSpan.FromSeconds(10) }));
            _launcherToken = System.Environment.GetEnvironmentVariable("RPG_TOKEN");
        }
        AddEnvironment();
        ItemIcon.RightClicked += ItemRightClicked;
        if (_options.SelfTest || _options.Demo)
            StartFight(_hero, _options.Scenario);
        else if (_options.Shot is "move" or "spell" && _options.Screenshot is not null)
        {
            // The pictures of the project page: a tenth-level sentinel and its deck, icons and colours shown.
            StartFight(new Hero("Aubépine", "female-b", "sentinel", 5, 5, 4, -1, 1, Level: 10, Stats: new Characteristics(Earth: 30, Fire: 40)), _options.Scenario);
        }
        else if (_options.TownSelfTest || _options.Shot is "town" or "banner" or "menu" or "zone" or "world")
        {
            // The pictures of the world: in the woods, a few steps from the way north.
            if (_options.Shot is "zone" or "world")
                (_zone, _place) = ("woods-2-2", new Cell(8, 5));
            ShowTown();
        }
        else if (_options.LobbySelfTest || _options.Shot is "lobby" or "create")
            ShowLobby();
        else if (_screen == Screen.Fight)
            StartFight(_hero, _scenario ?? _options.Scenario);
        else if (_screen == Screen.Town || _options.Offline)
            ShowTown();
        else
            ShowLobby();
    }

    /// <summary>Empties the scene for the next screen, without reloading it (the self-tests go on).</summary>
    private void ClearScene()
    {
        foreach (Node child in GetChildren())
        {
            RemoveChild(child);
            child.QueueFree();
        }
        _lobbyView = null;
        _townView = null;
        _tracker = null;
        _trailArrow = null;
        _miniMap = null;
        _talkPanel = null;
        _mapPanel = null;
        _pointsPanel = null;
        _inventoryPanel = null;
        _menuPanel = null;
        _bar = null;
        _chatBox = null;
        _partyBanner = null;
        _askPanel = null;
        _tradePanel = null;
        _journalPanel = null;
        _itemSheet = null;
        // A new screen opens with no window and no menu.
        _windows = new WindowState();
        AddEnvironment();
    }

    private void ShowLobby()
    {
        _camera = new Camera3D();
        AddChild(_camera);
        _lobbyView = new LobbyView();
        // The self-test remembers its sign-in apart from the player's.
        if (_options.LobbySelfTest)
            LobbyView.MemoryPath = "user://selftest-" + SignInMemory.FileName;
        _lobbyView.Chosen += character =>
        {
            _character = character;
            _hero = character?.Hero;
            _place = null;
            ClearScene();
            ShowTown();
        };
        AddChild(_lobbyView);
        _lobbyView.Build(_lobby!, new Texts(_options.Lang));
        if (_launcherToken is string token && !_lobby!.SignedIn && !_options.LobbySelfTest)
        {
            // Used once: a token run out sends back to the launcher.
            _launcherToken = null;
            _ = UseLauncherToken(token);
        }
        if (_options.LobbySelfTest)
            _ = RunLobbySelfTest();
        else if (_options.Shot is "lobby" or "create")
            _ = LobbyScreenshot();
    }

    private async Task UseLauncherToken(string token)
    {
        await _lobby!.UseToken(token);
        _lobbyView?.Refresh();
    }

    private void StartFight(Hero? hero, string scenario)
    {
        _screen = Screen.Fight;
        _scenario = scenario;
        _ticket = _pendingTicket;
        _pendingTicket = null;
        ulong seed = _ticket?.Seed ?? _nextSeed ?? _options.Seed ?? (ulong)Time.GetUnixTimeFromSystem();
        // The hero's life to start with, on the server's word (sprint 101, D73; a dungeon's rooms since sprint 86).
        int? hp = hero?.Class is not null ? _ticket?.HeroHp : null;
        _controller = new FightController(new Fight(GameData.Embedded, scenario, seed, hero, heroHp: hp));
        BuildWorld();
        if (_options.SelfTest || _options.LobbySelfTest || _options.TownSelfTest)
            CallDeferred(MethodName.RunSelfTest);
        else if (_options.Screenshot is not null)
            _ = Screenshot();
        else
            _busy = false;
    }

    public override void _Process(double delta)
    {
        if (_townView is not null)
        {
            DrainChat();
            FollowPlayer(delta);
            RoamGroups(delta);
            RoamBeasts(delta);
            PointTrail();
            FollowMiniMap();
            GrowBack();
            ShowJoins();
            return;
        }
        // A fight with the party (sprint 121): the server plays everything but the player's hero; its actions, as told, are shown in turn.
        if (_controller?.Shared is SharedFightSync shared && _lobbyView is null)
        {
            DrainChat();
            ShowTurnClock();
            if (shared.OutOfStep && !_resyncing)
                _ = Resync();
            else if (!_busy && _seen < _controller.Fight.Events.Count)
                _ = Play(instant: false);
            return;
        }
        if (_lobbyView is not null || _controller is null || _busy || _options.SelfTest || _options.LobbySelfTest || _options.TownSelfTest || _options.Screenshot is not null)
            return;
        if (_controller.Fight.IsOver)
        {
            if (_options.Demo)
            {
                _busy = true;
                _ = QuitSoon();
            }
            return;
        }
        if (_controller.IsPlayerTurn && !_options.Demo)
            return;
        _busy = true;
        _ = _controller.IsPlayerTurn ? DemoStep() : AiStep();
    }

    public override void _UnhandledInput(InputEvent @event)
    {
        if (InTown)
        {
            TownInput(@event);
            return;
        }
        if (_lobbyView is not null || _controller is null)
            return;
        // Escape cancels the spell chosen; otherwise, on any turn, it opens or closes the menu.
        if (@event is InputEventKey { Pressed: true, Echo: false, Keycode: Key.Escape } && (_windows.MenuOpen || _windows.Open is not null || _controller.SelectedSpell is null || _busy || !_controller.IsPlayerTurn))
        {
            Escape(menuOnly: false);
            return;
        }
        if (_windows.MenuOpen)
            return;
        // C and I (sprint 93, Charles): the characteristics and the inventory, to look at only, on any turn.
        if (@event is InputEventKey { Pressed: true, Echo: false, CtrlPressed: false, AltPressed: false } pressed
            && WindowState.ForKey((char)pressed.Keycode) is GameWindow window && WindowState.OpensInFight(window))
        {
            if (_hero?.Class is not null)
            {
                _windows.Toggle(window);
                ShowWindows();
            }
            return;
        }
        if (_windows.Open is not null)
            return;
        // The hover panel works on every turn, the AI's included.
        if (@event is InputEventMouseMotion motion)
        {
            _hovered = BoardView.Pick(_camera, motion.Position);
            if (_busy || !_controller.IsPlayerTurn)
                _hud.ShowInfo(_controller.Info(_hovered, _hud.Texts));
            else
                ShowPreview();
            return;
        }
        if (_busy || !_controller.IsPlayerTurn)
            return;
        switch (@event)
        {
            case InputEventMouseButton { Pressed: true, ButtonIndex: MouseButton.Left } click:
                if (BoardView.Pick(_camera, click.Position) is Cell cell)
                    Act(_controller.Click(cell));
                break;
            case InputEventMouseButton { Pressed: true, ButtonIndex: MouseButton.Right }:
                _controller.CancelSpell();
                ShowPreview();
                break;
            case InputEventKey { Pressed: true, Echo: false } key:
                // 1 to 6 for the first row of the deck, Ctrl+1 to Ctrl+6 for the second (sprint 68).
                if (key.Keycode >= Key.Key1 && key.Keycode <= Key.Key6)
                    ChooseSpell((int)(key.Keycode - Key.Key1) + (key.CtrlPressed ? 6 : 0));
                else if (key.Keycode == Key.Escape)
                    ChooseSpell(-1);
                else if (key.Keycode is Key.Space or Key.Enter)
                    Act(_controller.EndTurn());
                break;
        }
    }

    /// <summary>
    /// Escape (sprint 69): in town, closes the menu, else the window, else opens the menu; the menu's
    /// icon (<paramref name="menuOnly"/>) opens or closes the menu alone. In a fight, the menu.
    /// </summary>
    public override void _ExitTree() => ItemIcon.RightClicked -= ItemRightClicked;

    private void Escape(bool menuOnly)
    {
        // An item's sheet first (sprint 117): it is over the windows.
        if (!menuOnly && _itemSheet is not null)
        {
            CloseItemSheet();
            return;
        }
        if (menuOnly)
            _windows.ToggleMenu();
        else
            _windows.Escape();
        ShowWindows();
        ShowMenu();
    }

    /// <summary>The menu shown or not, as <see cref="WindowState.MenuOpen"/> says.</summary>
    private void ShowMenu()
    {
        if (_windows.MenuOpen == _menuPanel is not null)
            return;
        _sound?.Play("click");
        if (!_windows.MenuOpen)
        {
            _menuPanel!.QueueFree();
            _menuPanel = null;
            return;
        }
        _menuPanel = new MenuPanel();
        AddChild(_menuPanel);
        // The picture of the project page shows the menu as when signed in.
        _menuPanel.Build(_talkPanel?.Texts ?? _hud?.Texts ?? new Texts(_options.Lang), _lobby?.SignedIn == true || _options.Shot == "menu");
        _menuPanel.Chosen += Choose;
    }

    private void Choose(MenuChoice choice)
    {
        _sound?.Play("click");
        switch (choice)
        {
            case MenuChoice.Resume:
                _windows.CloseMenu();
                ShowMenu();
                break;
            case MenuChoice.Servers or MenuChoice.Characters:
                // Another character: its own chat.
                _ = CloseChat();
                // The lobby again: on the server list, or on this server's characters.
                if (choice == MenuChoice.Servers)
                    _lobby?.ChooseServer(null);
                _screen = Screen.Lobby;
                GetTree().ReloadCurrentScene();
                break;
            case MenuChoice.Quit:
                _ = QuitCleanly(0);
                break;
        }
    }

    private void AddEnvironment()
    {
        _sound = new Sound();
        AddChild(_sound);
        var env = new Godot.Environment
        {
            BackgroundMode = Godot.Environment.BGMode.Color,
            BackgroundColor = new Color("1f1f1f"),
            AmbientLightSource = Godot.Environment.AmbientSource.Color,
            AmbientLightColor = Colors.White,
            AmbientLightEnergy = 0.45f,
            TonemapMode = Godot.Environment.ToneMapper.Filmic,
        };
        AddChild(new WorldEnvironment { Environment = env });
        AddChild(new DirectionalLight3D
        {
            RotationDegrees = new Vector3(-55, -30, 0),
            ShadowEnabled = true,
            LightColor = new Color(1f, 0.96f, 0.88f),
            LightEnergy = 0.85f,
        });
    }

    private void BuildWorld()
    {
        Fight fight = _controller.Fight;
        // An orthographic camera at 30 degrees: each cell is a diamond twice as wide as it is high.
        var centre = new Vector3((fight.Board.Width - 1) / 2f, 0, (fight.Board.Height - 1) / 2f);
        _camera = new Camera3D { Projection = Camera3D.ProjectionType.Orthogonal, Size = 11.5f, RotationDegrees = new Vector3(-30, 45, 0) };
        AddChild(_camera);
        _camera.Position = centre + _camera.Transform.Basis.Z * 30f + new Vector3(0, -0.6f, 0);
        _board = new BoardView();
        AddChild(_board);
        _board.Build(fight.Board);
        foreach (Fighter f in fight.Fighters)
        {
            var view = new FighterView();
            AddChild(view);
            view.Setup(f, f.Team == _controller.PlayerTeam);
            if (fight.Hero is Hero hero && f.Name.Fr == hero.Name)
                view.Paint(hero);
            // The party's heroes in their own outfits (sprint 121).
            else if (fight.Allies.FirstOrDefault(a => f.Team == 0 && f.Name.Fr == a.Name) is Hero ally)
                view.Paint(ally);
            _views[f.Id] = view;
        }
        _hud = new Hud();
        AddChild(_hud);
        _hud.Build(_controller, new Texts(_options.Lang));
        _hud.SpellChosen += ChooseSpell;
        _hud.EndTurnPressed += () =>
        {
            if (!_busy)
                Act(_controller.EndTurn());
        };
        // "Close": where the player stood before the fight, never the same fight again (sprint 87, D68); in a
        // dungeon's hall, or out of it when the room was lost or the last (sprint 86).
        _hud.ClosePressed += () =>
        {
            _screen = Screen.Town;
            _nextSeed = null;
            (_sharedSync, _sharedOver) = (null, null);
            GetTree().ReloadCurrentScene();
        };
        _seen = fight.Events.Count;
        RefreshViews();
    }

    /// <summary>Offline, where the player is in a dungeon (sprint 86); signed in, the server keeps it.</summary>
    private static DungeonRun? _offlineRun;

    /// <summary>Where the player is in a dungeon: the server's word, or offline the one kept in memory.</summary>
    private static DungeonRun? Run => _character is not null ? _character.Run : _offlineRun;

    /// <summary>Asks the server for a fight, signed in with a character; null offline or when it cannot be reached.</summary>
    private static async Task<FightTicket?> RequestTicket(string scenario)
    {
        if (_lobby?.SignedIn != true || _character is null)
            return null;
        try
        {
            return await _lobby.Server.StartFight(_character.Id, scenario);
        }
        catch (Exception e) when (e is ServerException or HttpRequestException)
        {
            return null;
        }
    }

    /// <summary>
    /// At the end of a fight the server started: hands it the record and keeps what it earned (the
    /// character's experience, level, Éclats and items, <see cref="_lastResult"/>). Once per fight.
    /// Returns what went wrong, if anything; null when it is kept, or offline.
    /// </summary>
    private Task<string?> ReportFight() => _report ??= _controller.Shared is not null ? Task.FromResult<string?>(null) : ReportFightAsync();

    /// <summary>No server keeps this fight: offline, or no ticket drawn.</summary>
    private bool Offline => _ticket is null || _lobby?.SignedIn != true || _character is null;

    private async Task<string?> ReportFightAsync()
    {
        Texts texts = _hud.Texts;
        _lastResult = null;
        if (_ticket is not FightTicket ticket || Offline)
            return null;
        try
        {
            FightResult result = await _lobby!.Server.ReportFight(_character!.Id, ticket.Id, FightRecord.Of(_controller.Fight));
            _lastResult = result;
            _character = (await _lobby.Server.Characters()).Single(c => c.Id == _character.Id);
            _hero = _character.Hero;
            return null;
        }
        catch (ServerException e)
        {
            return texts["result.refused", e.Message];
        }
        catch (HttpRequestException)
        {
            return texts["lobby.unreachable"];
        }
    }

    private void ChooseSpell(int index)
    {
        if (_busy)
            return;
        if (index < 0)
            _controller.CancelSpell();
        else
        {
            _controller.SelectSpell(index);
            // A spell that cannot be cast now (points, cooldown) is refused with a sound.
            if (_controller.LastError is not null)
                _sound?.Play("error");
        }
        ShowPreview();
    }

    private void ShowPreview()
    {
        _board.Show(_controller.Hover(_hovered));
        _hud.Refresh();
        _hud.ShowInfo(_controller.Info(_hovered, _hud.Texts));
    }

    private void Act(FightAction? action)
    {
        if (action is null)
        {
            ShowPreview();
            return;
        }
        // With the party, the server is told; it checks it by the same rules (sprint 121).
        if (_controller.Shared is not null && _sharedFight is Guid battle)
            _ = Social(s => s.Act(_character!.Id, battle, action));
        _busy = true;
        _ = Play(instant: false);
    }

    /// <summary>The demo: the AI chooses the hero's action, shown like the player's.</summary>
    private async Task DemoStep()
    {
        await ToSignal(GetTree().CreateTimer(0.5), SceneTreeTimer.SignalName.Timeout);
        _controller.Fight.Apply(Ai.Decide(_controller.Fight));
        await Play(instant: false);
    }

    private async Task QuitSoon()
    {
        await ToSignal(GetTree().CreateTimer(2.5), SceneTreeTimer.SignalName.Timeout);
        GetTree().Quit();
    }

    private async Task AiStep()
    {
        await ToSignal(GetTree().CreateTimer(0.35), SceneTreeTimer.SignalName.Timeout);
        _controller.PlayAiStep();
        await Play(instant: false);
    }

    /// <summary>Animates the events not shown yet, in order, then gives the hand back.</summary>
    private async Task Play(bool instant)
    {
        _busy = true;
        _board.Show(Preview.None);
        _hud.Refresh(busy: true);
        IReadOnlyList<FightEvent> events = _controller.Fight.Events;
        while (_seen < events.Count)
        {
            FightEvent e = events[_seen++];
            _hud.Log(e);
            await Animate(e, instant);
        }
        RefreshViews();
        _busy = false;
        if (_controller.Fight.IsOver && _report is null)
        {
            _sound?.Play(_controller.Fight.WinningTeam == _controller.PlayerTeam ? "victory" : "defeat");
            // A group of monsters beaten leaves its map for a while (sprint 72).
            if (_controller.Fight.WinningTeam == _controller.PlayerTeam && _controller.Fight.Scenario.Id.StartsWith(MonsterGroup.Prefix, StringComparison.Ordinal))
                Presence.Beaten(_controller.Fight.Scenario.Id[MonsterGroup.Prefix.Length..], DateTimeOffset.UtcNow);
            _ = ShowResult();
        }
        if (_controller.IsPlayerTurn)
            ShowPreview();
    }

    /// <summary>
    /// The end-of-fight window (sprint 87): at once with what the rules give, then with what the server
    /// kept, or why it did not.
    /// </summary>
    private async Task ShowResult()
    {
        Fight fight = _controller.Fight;
        int team = _controller.PlayerTeam;
        // With the party (sprint 121): each hero's line, as the server says once it has rewarded them.
        if (_controller.Shared is SharedFightSync shared)
        {
            _hud.ShowSummary(t => new FightSummary(fight, team, null, t));
            for (int i = 0; i < 100 && _sharedOver is null; i++)
            {
                await ToSignal(GetTree().CreateTimer(0.1), SceneTreeTimer.SignalName.Timeout);
                DrainChat();
            }
            if (_sharedOver?.Results is IReadOnlyList<SharedFightResult> results)
            {
                _hud.ShowSummary(t => new FightSummary(fight, team, null, t, party: (results, shared.Me)));
                await Social(async s =>
                {
                    if ((await s.Characters()).FirstOrDefault(c => c.Id == _character!.Id) is CharacterSummary now)
                        (_character, _hero) = (now, now.Hero);
                });
            }
            return;
        }
        bool offline = Offline;
        _hud.ShowSummary(t => new FightSummary(fight, team, null, t, offline));
        string? problem = await ReportFight();
        FightResult? result = _lastResult;
        _hud.ShowSummary(t => new FightSummary(fight, team, result, t, offline), problem);
        // In a dungeon: won and not the last, back to its hall, the far door open; offline, the run kept here.
        if (Dungeons.Parse(_controller.Fight.Scenario.Id) is (string dungeon, int room))
        {
            if (_character is null)
            {
                Fighter? me = _controller.Fight.HeroFighter;
                _offlineRun = _controller.Fight.WinningTeam == _controller.PlayerTeam && room < Dungeons.RoomCount && me is not null
                    ? new DungeonRun(dungeon, room + 1, me.Hp < me.MaxHp ? me.Hp : null)
                    : null;
            }
        }
    }

    /// <summary>What the server gave for the last fight reported.</summary>
    private static FightResult? _lastResult;

    private Task Animate(FightEvent e, bool instant)
    {
        switch (e)
        {
            case Moved m:
                return _views[m.Fighter].Walk(m.Path, instant ? 0 : 0.22);
            case SpellCast c:
                if (!instant)
                    _sound?.Play("cast");
                return Cast(c, instant);
            case Damaged d:
                if (!instant)
                    _sound?.Play("hit");
                _views[d.Fighter].ShowDamage(d.Amount, d.Element, instant ? 0 : 0.9);
                return Task.CompletedTask;
            case Died d:
                _views[d.Fighter].Die(animate: !instant);
                return Task.CompletedTask;
            case Summoned m:
                Fighter summoned = _controller.Fight.Fighters[m.Fighter];
                var view = new FighterView();
                AddChild(view);
                view.Setup(summoned, summoned.Team == _controller.PlayerTeam);
                _views[summoned.Id] = view;
                return Task.CompletedTask;
            case Pushed p when p.Path.Count > 0:
                return _views[p.Fighter].Walk(p.Path, instant ? 0 : 0.12);
            // A passive's name floats above its bearer; leaps arc, swaps slide across (sprint 90).
            case PassiveTriggered p:
                _views[p.Fighter].ShowNumber(_controller.Fight.Fighters[p.Fighter].Passives.First(x => x.Id == p.Passive).Name.In(_options.Lang), new Color(ElementStyle.Colour(Element.Neutral)), instant ? 0 : 1.2);
                return Task.CompletedTask;
            case Leaped l:
                return _views[l.Fighter].Leap(l.To, instant ? 0 : 0.35);
            case Swapped w:
                Vector3 atA = _views[w.A].Position, atB = _views[w.B].Position;
                return Task.WhenAll(_views[w.A].Glide(atB, instant ? 0 : 0.3), _views[w.B].Glide(atA, instant ? 0 : 0.3));
            // A glyph's rings on the board (sprint 130), in its element's colour; the damage that follows a strike shows as any.
            case GlyphPlaced g:
                // A Remanence (sprint 142) is laid by no spell of its own: its element comes with the event.
                _board.AddGlyph(g.Glyph, g.Cells, new Color(ElementStyle.Colour(g.Element)));
                return Task.CompletedTask;
            case GlyphEnded g:
                _board.RemoveGlyph(g.Glyph);
                return Task.CompletedTask;
            // The marks and the reactions (sprint 137): a disc of the element above the fighter; the reaction's name floats up.
            case Marked m:
                _views[m.Fighter].ShowMark(m.Element);
                return Task.CompletedTask;
            case MarkEnded m:
                _views[m.Fighter].ShowMark(null);
                return Task.CompletedTask;
            case ReactionTriggered r:
                _views[r.Fighter].ShowNumber(_controller.Fight.Data.Reactions[r.Reaction].Name.In(_options.Lang) + (_options.Lang == "fr" ? " !" : "!"), new Color("bef374"), instant ? 0 : 1.2);
                return Task.CompletedTask;
            case Healed h:
                _views[h.Fighter].ShowNumber($"+{h.Amount}", new Color(ElementStyle.Heal), instant ? 0 : 0.9);
                return Task.CompletedTask;
            case ShieldAbsorbed a:
                _views[a.Fighter].ShowNumber($"-{a.Amount}", new Color(ElementStyle.Shield), instant ? 0 : 0.9);
                return Task.CompletedTask;
            default:
                RefreshViews();
                return Task.CompletedTask;
        }
    }

    /// <summary>
    /// A cast: the caster strikes or throws; at range, a bolt of the element's colour flies to the
    /// target; then every cell of the area flashes in that colour.
    /// </summary>
    private async Task Cast(SpellCast c, bool instant)
    {
        Fighter caster = _controller.Fight.Fighters[c.Fighter];
        Spell spell = caster.Spells.First(s => s.Id == c.Spell);
        Task attack = _views[c.Fighter].Attack(c.Target, spell.MaxRange <= 1, instant ? 0 : 0.55);
        if (instant)
        {
            await attack;
            return;
        }
        var colour = new Color(ElementStyle.Colour(spell.Element));
        // The fight is ahead of the animation: the caster stands where its view is drawn.
        Vector3 at = _views[c.Fighter].Position;
        var casterCell = new Cell((int)Math.Round(at.X), (int)Math.Round(at.Z));
        var glow = new StandardMaterial3D { AlbedoColor = colour, EmissionEnabled = true, Emission = colour, EmissionEnergyMultiplier = 2, ShadingMode = BaseMaterial3D.ShadingModeEnum.Unshaded };
        Vector3 from = _views[c.Fighter].Position + new Vector3(0, 0.7f, 0);
        Vector3 to = BoardView.ToWorld(c.Target) + new Vector3(0, 0.45f, 0);
        await ToSignal(GetTree().CreateTimer(0.2), SceneTreeTimer.SignalName.Timeout);
        if (spell.MaxRange > 1 && c.Target != casterCell)
        {
            var bolt = new MeshInstance3D { Mesh = new SphereMesh { Radius = 0.11f, Height = 0.22f }, MaterialOverride = glow, Position = from };
            AddChild(bolt);
            Tween fly = CreateTween();
            fly.TweenProperty(bolt, "position", to, 0.08 * Math.Max(2, from.DistanceTo(to)));
            await ToSignal(fly, Tween.SignalName.Finished);
            bolt.QueueFree();
        }
        foreach (Cell cell in spell.Zone.Cells(casterCell, c.Target).Where(_controller.Fight.Board.Contains))
        {
            var flash = new MeshInstance3D
            {
                Mesh = new CylinderMesh { TopRadius = 0.45f, BottomRadius = 0.45f, Height = 0.03f },
                MaterialOverride = new StandardMaterial3D { AlbedoColor = colour with { A = 0.75f }, Transparency = BaseMaterial3D.TransparencyEnum.Alpha, ShadingMode = BaseMaterial3D.ShadingModeEnum.Unshaded },
                Position = BoardView.ToWorld(cell) + new Vector3(0, 0.06f, 0),
                Scale = new Vector3(0.3f, 1, 0.3f),
            };
            AddChild(flash);
            Tween burst = CreateTween();
            burst.TweenProperty(flash, "scale", new Vector3(1.1f, 1, 1.1f), 0.3);
            burst.Parallel().TweenProperty(flash, "transparency", 1f, 0.3);
            burst.TweenCallback(Callable.From(flash.QueueFree));
        }
        await attack;
    }

    private void RefreshViews()
    {
        foreach (FighterView v in _views.Values)
            v.Refresh(current: !_controller.Fight.IsOver && v.Fighter == _controller.Fight.Current);
        _hud.Refresh(_busy);
    }

    /// <summary>
    /// Plays a whole fight through the controls (Rpg.Client.SelfPlay), every event through the views,
    /// and checks after each action that the views match the fight; writes one line and quits.
    /// </summary>
    private void RunSelfTest()
    {
        var problems = new List<string>();
        int checks = 0;
        if (_controller.Fight.Hero is Hero hero)
        {
            if (!_hud.OrderText.Contains(hero.Name, StringComparison.Ordinal))
                problems.Add($"the turn order does not show the hero {hero.Name}");
            FighterView heroView = _views.Values.Single(v => v.Fighter.Name.Fr == hero.Name);
            if (heroView.Painted != hero.Colour)
                problems.Add($"the hero is painted {heroView.Painted?.ToString(CultureInfo.InvariantCulture) ?? "not at all"}, not {hero.Colour}");
            // The hero's deck, in its order (by default, the class's spells its level has unlocked).
            if (GameData.Embedded.Class(hero.Class) is HeroClass c
                && !_hud.SpellsText.SequenceEqual(hero.DeckOf(GameData.Embedded).Select(id => GameData.Embedded.Spells[id].Name.In(_hud.Texts.Lang))))
                problems.Add($"the spell bar shows {string.Join(", ", _hud.SpellsText)}, not the {c.Id}'s deck of level {hero.Level}");
        }
        CheckFightWindows(problems);
        checks++;
        int seenNumbers = 0, seenWaits = 0;
        SelfPlay.Run(_controller, _ =>
        {
            Task played = Play(instant: true);
            if (!played.IsCompleted)
                problems.Add("an instant animation waited");
            foreach (FighterView v in _views.Values)
            {
                Fighter f = v.Fighter;
                if (f.IsAlive && v.Position != BoardView.ToWorld(f.Cell))
                    problems.Add($"{f.Name.En} drawn at {v.Position}, standing on {f.Cell}");
                // The fallen leave the board (sprint 79).
                if (f.IsAlive != v.Visible)
                    problems.Add($"{f.Name.En} {(f.IsAlive ? "alive and hidden" : "fallen and still drawn")}");
                if (v.HpText != $"{f.Hp}/{f.MaxHp}")
                    problems.Add($"{f.Name.En} shows {v.HpText}, has {f.Hp}");
                // The mark drawn is the fighter's (sprint 137).
                if (v.MarkShown != f.Mark?.Element)
                    problems.Add($"{f.Name.En} shows the mark {v.MarkShown}, bears {f.Mark?.Element}");
                _markChecks += f.Mark is null ? 0 : 1;
                checks++;
            }
            // The glyphs drawn are the fight's, each on all its cells (sprint 130); counted for the report (sprint 132).
            _glyphChecks += _controller.Fight.Glyphs.Count;
            if (!_board.GlyphsShown().OrderBy(g => g.Key).SequenceEqual(_controller.Fight.Glyphs.Select(g => KeyValuePair.Create(g.Id, g.Cells.Count))))
                problems.Add($"the board draws glyphs {string.Join(",", _board.GlyphsShown().Keys)}, the fight has {string.Join(",", _controller.Fight.Glyphs.Select(g => g.Id))}");
            CheckReadability(problems, ref seenNumbers);
            if (_controller.IsPlayerTurn)
            {
                Preview preview = _controller.Hover(null);
                _board.Show(preview);
                if (!_board.Shown().Keys.ToHashSet().SetEquals(preview.Reachable))
                    problems.Add("the board does not show the reachable cells");
                if (!_hud.StatsText.Contains($"{_controller.Fight.Current.Mp}", StringComparison.Ordinal))
                    problems.Add("the points shown are not the fighter's");
                // A spell cast lately shows the turns it still waits (sprint 115).
                Fighter me = _controller.Fight.Current;
                int[] waits = [.. me.Spells.Select(me.CooldownLeft)];
                if (!_hud.SpellWaits.SequenceEqual(waits))
                    problems.Add($"the spell tiles wait {string.Join(",", _hud.SpellWaits)}, the spells {string.Join(",", waits)}");
                if (waits.Any(w => w > 0))
                    seenWaits++;
                checks++;
            }
        });
        if (seenWaits == 0 && _controller.Fight.Fighters.Any(f => f.Team == _controller.PlayerTeam && f.Spells.Any(sp => sp.Cooldown > 0)))
            GD.Print("SELFTEST note: no spell with a cooldown was cast this fight");
        if (!_hud.EndShown)
            problems.Add("no end screen");
        _ = FinishSelfTest(problems, checks);
    }

    /// <summary>
    /// C and I in a fight (sprint 93): the characteristics, then the inventory instead, both to look at
    /// only; Escape closes the window before the menu. Without a character, a guard of level 10 lends hers.
    /// </summary>
    private void CheckFightWindows(List<string> problems)
    {
        Hero? before = _hero;
        _hero ??= _controller.Fight.Hero ?? new Hero("Essai", "female-b", "guard", Level: 10);
        _UnhandledInput(new InputEventKey { Pressed = true, Keycode = Key.C });
        if (_pointsPanel?.Editor.ReadOnly != true)
            problems.Add("C does not show the characteristics, to look at only, in a fight");
        _UnhandledInput(new InputEventKey { Pressed = true, Keycode = Key.I });
        if (_inventoryPanel?.Editor.ReadOnly != true || _pointsPanel is not null)
            problems.Add("I does not show the inventory alone, to look at only, in a fight");
        _UnhandledInput(new InputEventKey { Pressed = true, Keycode = Key.Escape });
        if (_inventoryPanel is not null || _windows.MenuOpen)
            problems.Add("Escape does not close the inventory first in a fight");
        _hero = before;
    }

    private void CheckEndWindow(List<string> problems)
    {
        Fight fight = _controller.Fight;
        Texts t = _hud.Texts;
        if (_hud.End.Summary is not FightSummary summary)
        {
            problems.Add("no end-of-fight window");
            return;
        }
        int people = fight.Fighters.Count(f => !f.IsSummon);
        string title = fight.WinningTeam is null ? t["draw"] : fight.WinningTeam == _controller.PlayerTeam ? t["victory"] : t["defeat"];
        if (summary.Title != title || summary.Winners.Count + summary.Losers.Count != people || _hud.End.Lines.Count != people)
            problems.Add($"the end window says {summary.Title} with {_hud.End.Lines.Count} lines for {people} fighters");
        int damage = fight.Events.OfType<Damaged>().Sum(d => d.Amount);
        if (summary.Stats.Sum(s => s.Stats.DamageTaken) != damage || summary.Stats.Sum(s => s.Stats.DamageDealt) > damage)
            problems.Add($"the statistics count {summary.Stats.Sum(s => s.Stats.DamageTaken)} damage taken, the fight {damage}");
        if (_hud.End.CloseText != t["end.close"])
            problems.Add($"the end window's button says {_hud.End.CloseText}");
    }

    /// <summary>
    /// The end of the fight's self-test: signed in, the fight goes to the server, which must keep
    /// exactly the experience the rules give, and the end screen must say it; then the report.
    /// </summary>
    // How many glyphs the self-test found drawn as the fight has them, over all its checks (sprint 132).
    private int _glyphChecks;

    /// <summary>The marks seen on the board during a self-test (sprint 137), for the report.</summary>
    private int _markChecks;

    private async Task FinishSelfTest(List<string> problems, int checks)
    {
        string? progress = null;
        if (_ticket is not null && _character is not null)
        {
            long before = _character.Xp, shardsBefore = _character.Shards;
            string? refused = await ReportFight();
            // Since sprint 107 the first lesson is Garance's quest, handed back to her: the fight gives its own experience only.
            long earned = Progression.FightXp(_controller.Fight);
            long shards = Progression.FightShards(_controller.Fight);
            if (_character.Xp != before + earned || _character.Shards != shardsBefore + shards)
                problems.Add($"the server keeps {_character.Xp} experience and {_character.Shards} Éclats, not {before + earned} and {shardsBefore + shards} ({refused})");
            // The end-of-fight window (sprint 87): the player's line says what the server kept, its loot as icons.
            await ShowResult();
            SummaryRow? mine = _hud.End.Summary?.Player;
            if (!_hud.EndShown || mine?.Xp != earned || mine.Shards != shards || mine.Level != _character.Level || mine.XpBar is null)
                problems.Add($"the end window shows {mine}");
            else if (_hud.End.LootIcons != (mine.Loot ?? []).Count(l => GameData.Embedded.Items.ContainsKey(l.Item)))
                problems.Add($"the end window shows {_hud.End.LootIcons} loot icons");
            progress = $"Experience: +{earned} kept by the server, level {_character.Level}. Éclats: +{shards}, {_character.Shards} in all.";
            // The loot and the quest's items are in the inventory the server keeps.
            var loot = Equipment.Loot(_controller.Fight).ToList();
            if (loot.Any(l => (_character.Inventory ?? []).FirstOrDefault(i => i.Item == l.Item)?.Count < l.Count || !(_character.Inventory ?? []).Any(i => i.Item == l.Item)))
                problems.Add($"the inventory lacks the loot {_hud.Texts.Items(loot)}");
            else if (loot.Count > 0)
                progress += $" Loot: {loot.Count} kind(s) of item.";
        }
        // The end-of-fight window (sprint 87): the winners and the losers, every fighter but the summons,
        // the statistics adding up to what the fight did, and "Close" alone.
        await ShowResult();
        CheckEndWindow(problems);
        _selfTestNote = ($"End window: {_hud.End.Lines.Count} lines, statistics checked. " + (_selfTestNote ?? "")).Trim();
        // A dungeon's room (sprint 80): its name and number above.
        if (Dungeons.Parse(_controller.Fight.Scenario.Id) is (_, int room))
        {
            Texts t = _hud.Texts;
            if (!_hud.TurnText.StartsWith(t["dungeon.room", _controller.Fight.Scenario.Name.In(t.Lang), room], StringComparison.Ordinal))
                problems.Add($"the top line says '{_hud.TurnText}'");
            _selfTestNote = ((_selfTestNote ?? "") + $" Dungeon room {room}: end screen checked.").Trim();
        }
        string report = SelfPlay.Report(_controller.Fight);
        report += problems.Count == 0
            ? $" Views checked {checks} times, {_board.Tiles} tiles." + (_glyphChecks > 0 ? $" Glyphs checked {_glyphChecks} times." : "") + (_markChecks > 0 ? $" Marks checked {_markChecks} times." : "")
            : $" VIEW PROBLEMS: {string.Join("; ", problems.Distinct().Take(5))}";
        if (_selfTestNote is not null)
            report += " " + _selfTestNote;
        if (progress is not null)
            report += " " + progress;
        if (problems.Count > 0)
            report = report.Replace("SELFTEST OK", "SELFTEST FAILED", StringComparison.Ordinal);
        GD.Print(report);
        if (_options.Report is string path)
            File.WriteAllText(path, report + "\n");
        _ = QuitCleanly(report.StartsWith("SELFTEST OK", StringComparison.Ordinal) ? 0 : 1);
    }

    /// <summary>
    /// Quits once the scene is emptied and the C# objects collected: quitting with many still alive
    /// (images, materials, animations built by the self-tests) crashed Godot's .NET module on its way
    /// out, now and then, after a self-test that had passed (CI, sprint 71).
    /// </summary>
    private static async Task CloseChat()
    {
        if (_chat is not null)
            await _chat.DisposeAsync();
        (_chat, _chatOf) = (null, null);
    }

    private async Task QuitCleanly(int code)
    {
        await CloseChat();
        SpellIcon.Forget();
        ItemIcon.Forget();
        foreach (Node child in GetChildren())
        {
            RemoveChild(child);
            child.QueueFree();
        }
        for (int i = 0; i < 3; i++)
            await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
        GC.Collect();
        GC.WaitForPendingFinalizers();
        await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
        GetTree().Quit(code);
    }

    /// <summary>
    /// The self-test of sprint 57: the timeline shows the round's order, the one playing outlined
    /// (sprint 78); hovering each fighter shows its
    /// card, and aiming a spell at an enemy its forecast; every number shown above a fighter since
    /// the last check has its colour (the last one per fighter is compared).
    /// </summary>
    private void CheckReadability(List<string> problems, ref int seenNumbers)
    {
        Fight fight = _controller.Fight;
        Texts texts = _hud.Texts;
        if (!_hud.TimelineText.SequenceEqual(fight.RoundOrder.Take(Hud.TimelineLength).Select(texts.Name)))
            problems.Add($"the timeline shows {string.Join(", ", _hud.TimelineText)}");
        if (!_hud.TimelineLooks.SequenceEqual(fight.RoundOrder.Take(Hud.TimelineLength).Select(f => f.Spec.Look)))
            problems.Add("the timeline's portraits are not the fighters'");
        if (_hud.RoundText != fight.Round.ToString(CultureInfo.InvariantCulture))
            problems.Add($"the clock shows round {_hud.RoundText}, not {fight.Round}");
        if (!fight.IsOver && _hud.CurrentCard != fight.RoundOrder.ToList().IndexOf(fight.Current))
            problems.Add($"the timeline outlines card {_hud.CurrentCard}, not the one playing");
        // The turn's clock of a fight with the party (sprint 124), shown as if the server said 8 seconds were left.
        if (!fight.IsOver)
        {
            int away = fight.Fighters.First(f => f.Team == 0).Id;
            _hud.ShowClock(fight.Current.Id, 8, 30, alert: true, [away]);
            if (_hud.ClockShown != 8 || !_hud.ClockInAlert || _hud.AbsentShown != fight.RoundOrder.Take(Hud.TimelineLength).Count(f => f.Id == away))
                problems.Add($"the turn's clock shows {_hud.ClockShown} (alert {_hud.ClockInAlert}), {_hud.AbsentShown} away");
            _hud.ShowClock(null, null, 30, alert: false, []);
            if (_hud.ClockShown is not null || _hud.AbsentShown != 0)
                problems.Add("the turn's clock stays without a fight with the party");
        }
        var last = new Dictionary<int, Color>();
        IReadOnlyList<FightEvent> events = fight.Events;
        for (; seenNumbers < events.Count; seenNumbers++)
        {
            switch (events[seenNumbers])
            {
                case Damaged d:
                    last[d.Fighter] = new Color(ElementStyle.Colour(d.Element));
                    break;
                case Healed h:
                    last[h.Fighter] = new Color(ElementStyle.Heal);
                    break;
                case ShieldAbsorbed a:
                    last[a.Fighter] = new Color(ElementStyle.Shield);
                    break;
                // A passive's name floats in the neutral colour (sprint 90).
                case PassiveTriggered p:
                    last[p.Fighter] = new Color(ElementStyle.Colour(Element.Neutral));
                    break;
            }
        }
        foreach ((int id, Color colour) in last)
        {
            if (_views[id].LastNumber?.Colour != colour)
                problems.Add($"the last number above {fight.Fighters[id].Name.En} is not in its colour");
        }
        foreach (Fighter f in fight.Fighters.Where(x => x.IsAlive))
        {
            _hovered = f.Cell;
            _hud.ShowInfo(_controller.Info(_hovered, texts));
            // Its card, then the glyphs it stands on (sprint 132: the first real board with glyphs).
            string card = string.Join("\n\n", [texts.FighterCard(f, fight), .. fight.GlyphsAt(f.Cell).Select(g => texts.GlyphCard(g, fight))]);
            if (_hud.InfoText?.EndsWith(card, StringComparison.Ordinal) != true)
                problems.Add($"hovering {f.Name.En} shows '{_hud.InfoText}'");
        }
        if (_controller.IsPlayerTurn && _controller.SelectedSpell is null)
        {
            Fighter me = fight.Current;
            for (int i = 0; i < me.Spells.Count; i++)
            {
                if (me.Spells[i].DamageMax == 0 || !_controller.CanUse(me.Spells[i]))
                    continue;
                _controller.SelectSpell(i);
                Fighter? aim = fight.Fighters.FirstOrDefault(x => x.IsAlive && x.Team != me.Team && _controller.Hover(x.Cell).Target == x.Cell);
                if (aim is not null)
                {
                    _hovered = aim.Cell;
                    ShowPreview();
                    string forecast = texts.Forecast(fight.Foresee(me.Spells[i], aim.Cell));
                    if (forecast.Length == 0 || _hud.InfoText?.StartsWith(forecast, StringComparison.Ordinal) != true)
                        problems.Add($"aiming {me.Spells[i].Id} at {aim.Name.En} shows '{_hud.InfoText}'");
                }
                _controller.CancelSpell();
                if (aim is not null)
                    break;
            }
        }
        _hovered = null;
        _hud.ShowInfo(null);
    }

    /// <summary>
    /// A picture for the project page: "move" shows the path to a cell at the start; "spell" lets the
    /// AI play until an enemy is in reach of the second spell, then aims at it.
    /// </summary>
    private async Task Screenshot()
    {
        _busy = true;
        Fight fight = _controller.Fight;
        if (_options.Shot == "spell")
        {
            for (int guard = 0; guard < 500 && !fight.IsOver; guard++)
            {
                if (_controller.IsPlayerTurn)
                {
                    _controller.SelectSpell(1);
                    Preview p = _controller.Hover(null);
                    Cell? enemy = fight.Fighters.Where(f => f.IsAlive && f.Team != _controller.PlayerTeam && p.Targetable.Contains(f.Cell)).Select(f => (Cell?)f.Cell).FirstOrDefault();
                    if (enemy is Cell target && fight.Fighters.All(f => f.IsAlive))
                    {
                        _hovered = target;
                        break;
                    }
                    _controller.CancelSpell();
                    fight.Apply(Ai.Decide(fight));
                }
                else
                {
                    _controller.PlayAiStep();
                }
            }
        }
        else
        {
            // Monsters with more initiative play first: up to the player's turn.
            for (int guard = 0; guard < 50 && !fight.IsOver && !_controller.IsPlayerTurn; guard++)
                _controller.PlayAiStep();
            Fighter me = fight.Current;
            _hovered = _controller.Hover(null).Reachable.OrderByDescending(c => c.DistanceTo(me.Cell)).ThenBy(c => c.Y).ThenBy(c => c.X).First();
        }
        await Play(instant: true);
        _busy = true;
        if (_options.Shot == "spell" && _controller.SelectedSpell is null)
            _controller.SelectSpell(1);
        _board.Show(_controller.Hover(_hovered));
        _hud.Refresh();
        for (int i = 0; i < 30; i++)
            await ToSignal(RenderingServer.Singleton, RenderingServer.SignalName.FramePostDraw);
        Error saved = GetViewport().GetTexture().GetImage().SavePng(_options.Screenshot!);
        GD.Print($"SCREENSHOT {saved} {_options.Screenshot}");
        GetTree().Quit(saved == Error.Ok ? 0 : 1);
    }

    /// <summary>
    /// Through the login screen's own fields and buttons, against the server: a new account, a new
    /// character, chosen; then the fight's self-test, with that hero (RunSelfTest writes the report).
    /// </summary>
    private async Task RunLobbySelfTest()
    {
        LobbyView view = _lobbyView!;
        string suffix = Guid.NewGuid().ToString("N")[..8];
        // Started without the launcher, the game asks to sign in itself (sprint 78): a password too short
        // is refused with its message, then the account is made through the same fields.
        string? signIn = view.SignInShown ? null : "the sign-in fields are not shown";
        view.TypeSignIn("selftest_" + suffix, "short");
        await view.SignIn(create: true);
        if (_lobby!.SignedIn || view.SignInMessage != view.Texts["lobby.password-short"])
            signIn ??= $"a short password gives \"{view.SignInMessage}\"";
        view.TypeSignIn("selftest_" + suffix, "selftest password");
        // Both boxes ticked (sprint 79): the name and the password come back, the password sealed.
        view.TickRemember(name: true, password: true);
        await view.SignIn(create: true);
        if (!view.RememberShown || LobbyView.Remembered != new RememberedSignIn("selftest_" + suffix, "selftest password", true, true))
            signIn ??= $"the sign-in remembered is {LobbyView.Remembered with { Password = "…" }}";
        // Letters only: the hex digits of the suffix become letters a to p.
        string hero = "Essai" + string.Concat(suffix.Select(c => (char)('a' + Convert.ToInt32(c.ToString(), 16))));
        string? problem = signIn ?? (_lobby.ChosenServer is null ? $"no server chosen among {_lobby.Servers.Count}" : null);
        if (problem is null)
        {
            view.OpenCreate();
            view.CharacterField.Text = hero;
            view.SelectLook("male-b");
            view.SelectClass("mage");
            // The creation screen lists the class's twenty spells, by level.
            string[] missing = [.. GameData.Embedded.Class("mage")!.Spells.Select(id => GameData.Embedded.Spells[id].Name.In(view.Texts.Lang)).Where(n => !view.SpellsText.Contains(n, StringComparison.Ordinal))];
            if (missing.Length > 0)
                problem = $"the creation screen does not list {string.Join(", ", missing)}";
            view.ChooseColour(3);
            view.ChooseHair(4);
            view.ChooseSkin(2);
            view.SetShape(1, -1);
            await view.Create();
            string expected = $"{hero} · {GameData.Embedded.Class("mage")!.Name.In(view.Texts.Lang)} · {view.Texts["lobby.level", 1]}";
            problem ??= view.CreateOpen ? $"the creation panel stayed open: {view.MessageText}"
                : !view.CardTexts.SequenceEqual([expected]) ? $"the cards show {string.Join(" | ", view.CardTexts)}, not {expected}"
                : view.PlayButton.Disabled ? "the new character is not chosen"
                : null;
        }
        if (problem is not null)
        {
            string report = $"SELFTEST FAILED lobby: {problem} ({_lobby.Problem})";
            GD.Print(report);
            if (_options.Report is string path)
                File.WriteAllText(path, report + "\n");
            GetTree().Quit(1);
            return;
        }
        Hero kept = _lobby.Here.Single().Hero;
        if (kept != new Hero(hero, "male-b", "mage", 3, 4, 2, 1, -1))
        {
            string report = $"SELFTEST FAILED lobby: the server keeps {kept}";
            GD.Print(report);
            if (_options.Report is string path)
                File.WriteAllText(path, report + "\n");
            GetTree().Quit(1);
            return;
        }
        _selfTestNote = $"Hero {hero} (male-b, mage, colour 3, hair 4, skin 2, height 1, build -1) made on server {_lobby.ChosenServer!.Name}, shown on its card and chosen.";
        view.PlayButton.EmitSignal(BaseButton.SignalName.Pressed);
    }

    /// <summary>The characters screen for the project page: signs up, creates three characters if the names are free.</summary>
    private async Task LobbyScreenshot()
    {
        LobbyView view = _lobbyView!;
        await _lobby!.SignIn("capture_" + Guid.NewGuid().ToString("N")[..8], "screenshot password", create: true);
        foreach (Hero h in new[] { new Hero("Ondine", "female-e", "sentinel", 0, 3, 0, 0, 0), new Hero("Élise", "female-c", "mage", 4, 7, 1, -1, 0), new Hero("Bastien", "male-c", "guard", 2, 1, 4, 2, 2) })
        {
            view.OpenCreate();
            view.CharacterField.Text = h.Name;
            view.SelectLook(h.Look);
            view.SelectClass(h.Class!);
            view.ChooseColour(h.Colour);
            view.ChooseHair(h.Hair);
            view.ChooseSkin(h.Skin);
            view.SetShape(h.Height, h.Build);
            await view.Create();
        }
        view.Select(_lobby.Here[1].Id);
        if (_options.Shot == "create")
        {
            // The creation screen itself, half filled.
            view.OpenCreate();
            view.CharacterField.Text = "Aubépine";
            view.SelectLook("female-b");
            view.SelectClass("guard");
            view.ChooseColour(5);
            view.ChooseHair(5);
            view.ChooseSkin(4);
            view.SetShape(-1, 1);
        }
        for (int i = 0; i < 30; i++)
            await ToSignal(RenderingServer.Singleton, RenderingServer.SignalName.FramePostDraw);
        Error saved = GetViewport().GetTexture().GetImage().SavePng(_options.Screenshot!);
        GD.Print($"SCREENSHOT {saved} {_options.Screenshot}");
        GetTree().Quit(saved == Error.Ok ? 0 : 1);
    }

    private sealed record Options(string Lang, string Scenario, ulong? Seed, bool SelfTest, string? Report, string? Screenshot, string Shot, string Server, bool Offline, bool LobbySelfTest, bool TownSelfTest = false, bool ServerGiven = false, bool Demo = false)
    {
        public static Options Parse(string[] args, string locale)
        {
            var o = new Options(locale == "fr" ? "fr" : "en", "training", null, false, null, null, "move", "http://localhost:8002/", false, false);
            for (int i = 0; i < args.Length; i++)
            {
                string Next() => i + 1 < args.Length ? args[++i] : "";
                o = args[i] switch
                {
                    "--lang" => o with { Lang = Next() == "fr" ? "fr" : "en" },
                    "--scenario" => o with { Scenario = Next() },
                    "--seed" => o with { Seed = ulong.Parse(Next(), CultureInfo.InvariantCulture) },
                    "--selftest" => o with { SelfTest = true },
                    "--report" => o with { Report = Next() },
                    "--screenshot" => o with { Screenshot = Next() },
                    "--shot" => o with { Shot = Next() },
                    "--server" => o with { Server = Next().TrimEnd('/') + "/", ServerGiven = true },
                    "--offline" => o with { Offline = true },
                    "--lobby-selftest" => o with { LobbySelfTest = true },
                    "--town-selftest" => o with { TownSelfTest = true },
                    "--demo" => o with { Demo = true },
                    _ => o,
                };
            }
            return o;
        }
    }
}
