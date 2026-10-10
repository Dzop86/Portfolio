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
    private Task<string>? _report;
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
        if (_options.SelfTest || _options.Demo)
            StartFight(_hero, _options.Scenario);
        else if (_options.Shot is "move" or "spell" && _options.Screenshot is not null)
        {
            // The pictures of the project page: a tenth-level sentinel and its deck, icons and colours shown.
            StartFight(new Hero("Aubépine", "female-b", "sentinel", 5, 5, 4, -1, 1, Level: 10, Stats: new Characteristics(Earth: 30, Fire: 40)), _options.Scenario);
        }
        else if (_options.TownSelfTest || _options.Shot is "town" or "banner" or "zone" or "world")
        {
            // The pictures of the world: in the woods, a few steps from the way north.
            if (_options.Shot is "zone" or "world")
                (_zone, _place) = ("clairval-woods", new Cell(15, 3));
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
        _talkPanel = null;
        _mapPanel = null;
        _pointsPanel = null;
        _inventoryPanel = null;
        AddEnvironment();
    }

    private void ShowLobby()
    {
        _camera = new Camera3D();
        AddChild(_camera);
        _lobbyView = new LobbyView();
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
        _controller = new FightController(new Fight(GameData.Embedded, scenario, seed, hero));
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
            FollowPlayer(delta);
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
        if (_town is not null && _townView is not null)
        {
            TownInput(@event);
            return;
        }
        if (_lobbyView is not null || _controller is null)
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

    private void AddEnvironment()
    {
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
        _hud.AgainPressed += () => _ = Again();
        // Back to town, where the player stood before the fight.
        _hud.SetBackShown(true);
        _hud.BackPressed += () =>
        {
            _screen = Screen.Town;
            _nextSeed = null;
            GetTree().ReloadCurrentScene();
        };
        _seen = fight.Events.Count;
        RefreshViews();
    }

    private async Task Again()
    {
        _nextSeed = _controller.Fight.Seed + 1;
        _pendingTicket = await RequestTicket(_controller.Fight.Scenario.Id);
        GetTree().ReloadCurrentScene();
    }

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
    /// At the end of a fight the server started: hands it the record, keeps what it earned (the
    /// character's experience and level) and says it on the end screen. Once per fight.
    /// </summary>
    private Task<string> ReportFight() => _report ??= ReportFightAsync();

    private async Task<string> ReportFightAsync()
    {
        Texts texts = _hud.Texts;
        if (_ticket is not FightTicket ticket || _lobby?.SignedIn != true || _character is null)
            return _hero?.Class is null ? "" : texts["result.offline"];
        try
        {
            int before = _character.Level;
            FightResult result = await _lobby.Server.ReportFight(_character.Id, ticket.Id, FightRecord.Of(_controller.Fight));
            _character = (await _lobby.Server.Characters()).Single(c => c.Id == _character.Id);
            _hero = _character.Hero;
            return texts.Result(result, before);
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
            _controller.SelectSpell(index);
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
            _ = ShowResult();
        if (_controller.IsPlayerTurn)
            ShowPreview();
    }

    private async Task ShowResult() => _hud.ShowResult(await ReportFight());

    private Task Animate(FightEvent e, bool instant)
    {
        switch (e)
        {
            case Moved m:
                return _views[m.Fighter].Walk(m.Path, instant ? 0 : 0.22);
            case SpellCast c:
                return Cast(c, instant);
            case Damaged d:
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
            // The class's spells that the hero's level has unlocked, in order.
            if (GameData.Embedded.Class(hero.Class) is HeroClass c
                && !_hud.SpellsText.SequenceEqual(c.Spells.Select(id => GameData.Embedded.Spells[id]).Where(sp => sp.Level <= hero.Level).Select(sp => sp.Name.In(_hud.Texts.Lang))))
                problems.Add($"the spell bar shows {string.Join(", ", _hud.SpellsText)}, not the {c.Id}'s spells of level {hero.Level}");
        }
        int seenNumbers = 0;
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
                if (v.HpText != $"{f.Hp}/{f.MaxHp}")
                    problems.Add($"{f.Name.En} shows {v.HpText}, has {f.Hp}");
                checks++;
            }
            CheckReadability(problems, ref seenNumbers);
            if (_controller.IsPlayerTurn)
            {
                Preview preview = _controller.Hover(null);
                _board.Show(preview);
                if (!_board.Shown().Keys.ToHashSet().SetEquals(preview.Reachable))
                    problems.Add("the board does not show the reachable cells");
                if (!_hud.StatsText.Contains($"{_controller.Fight.Current.Mp}", StringComparison.Ordinal))
                    problems.Add("the points shown are not the fighter's");
                checks++;
            }
        });
        if (!_hud.EndShown)
            problems.Add("no end screen");
        _ = FinishSelfTest(problems, checks);
    }

    /// <summary>
    /// The end of the fight's self-test: signed in, the fight goes to the server, which must keep
    /// exactly the experience the rules give, and the end screen must say it; then the report.
    /// </summary>
    private async Task FinishSelfTest(List<string> problems, int checks)
    {
        string? progress = null;
        if (_ticket is not null && _character is not null)
        {
            long before = _character.Xp;
            bool firstWin = _controller.Fight.WinningTeam == 0 && !(_character.Quests ?? []).Contains("first-lesson");
            string shown = await ReportFight();
            long earned = Progression.FightXp(_controller.Fight) + (firstWin ? GameData.Embedded.Quests["first-lesson"].Xp : 0);
            if (_character.Xp != before + earned)
                problems.Add($"the server keeps {_character.Xp} experience, not {before + earned} ({shown})");
            if (_hud.ResultText != shown || !shown.StartsWith(_hud.Texts["result.xp", earned], StringComparison.Ordinal))
                problems.Add($"the end screen says '{_hud.ResultText}'");
            progress = $"Experience: +{earned} kept by the server, level {_character.Level}.";
            // The loot and the quest's items are in the inventory the server keeps.
            var loot = Equipment.Loot(_controller.Fight).Concat(firstWin ? GameData.Embedded.Quests["first-lesson"].Items! : []).ToList();
            if (loot.Any(l => (_character.Inventory ?? []).FirstOrDefault(i => i.Item == l.Item)?.Count < l.Count || !(_character.Inventory ?? []).Any(i => i.Item == l.Item)))
                problems.Add($"the inventory lacks the loot {_hud.Texts.Items(loot)}");
            else if (loot.Count > 0)
                progress += $" Loot: {loot.Count} kind(s) of item.";
        }
        string report = SelfPlay.Report(_controller.Fight);
        report += problems.Count == 0
            ? $" Views checked {checks} times, {_board.Tiles} tiles."
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
        GetTree().Quit(report.StartsWith("SELFTEST OK", StringComparison.Ordinal) ? 0 : 1);
    }

    /// <summary>
    /// The self-test of sprint 57: the timeline shows the next turns; hovering each fighter shows its
    /// card, and aiming a spell at an enemy its forecast; every number shown above a fighter since
    /// the last check has its colour (the last one per fighter is compared).
    /// </summary>
    private void CheckReadability(List<string> problems, ref int seenNumbers)
    {
        Fight fight = _controller.Fight;
        Texts texts = _hud.Texts;
        if (!_hud.TimelineText.SequenceEqual(fight.NextTurns(Hud.TimelineLength).Select(texts.Name)))
            problems.Add($"the timeline shows {string.Join(", ", _hud.TimelineText)}");
        if (!_hud.TimelineLooks.SequenceEqual(fight.NextTurns(Hud.TimelineLength).Select(f => f.Spec.Look)))
            problems.Add("the timeline's portraits are not the fighters'");
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
            if (_hud.InfoText?.EndsWith(texts.FighterCard(f, fight), StringComparison.Ordinal) != true)
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
        // The launcher signs up and in; here the lobby does it, then the screens take over.
        await _lobby!.SignIn("selftest_" + suffix, "selftest password", create: true);
        view.Refresh();
        // Letters only: the hex digits of the suffix become letters a to p.
        string hero = "Essai" + string.Concat(suffix.Select(c => (char)('a' + Convert.ToInt32(c.ToString(), 16))));
        string? problem = _lobby.ChosenServer is null ? $"no server chosen among {_lobby.Servers.Count}" : null;
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
