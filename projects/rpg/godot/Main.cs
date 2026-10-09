using System.Globalization;
using Godot;
using Rpg.Client;
using Rpg.Core;

namespace Rpg.Desktop;

/// <summary>
/// The login and character screen, then the fight. Options after "--": --lang fr|en, --scenario ID,
/// --seed N, --server URL (default http://localhost:8002/), --offline (straight to the fight);
/// --selftest [--report FILE] plays a whole fight through the controls and checks the views;
/// --lobby-selftest does the same after signing up, creating a character and choosing it on the
/// login screen, against the server; --screenshot FILE [--shot move|spell|lobby] saves a picture.
/// </summary>
public partial class Main : Node3D
{
    // Kept across "play again" (the scene reloads): the seed, the session and the hero chosen.
    private static ulong? _nextSeed;
    private static Lobby? _lobby;
    private static Hero? _hero;
    private static bool _fighting;

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
    private Node3D? _stage;
    private string? _selfTestNote;

    public override void _Ready()
    {
        _options = Options.Parse(OS.GetCmdlineUserArgs(), OS.GetLocaleLanguage());
        _lobby ??= new Lobby(new GameServer(new System.Net.Http.HttpClient { BaseAddress = new Uri(_options.Server), Timeout = TimeSpan.FromSeconds(10) }));
        AddEnvironment();
        bool lobbyFirst = _options.LobbySelfTest || _options.Shot == "lobby"
            || (!_fighting && !_options.Offline && !_options.SelfTest && _options.Screenshot is null);
        if (lobbyFirst)
            ShowLobby();
        else
            StartFight(_hero);
    }

    private void ShowLobby()
    {
        _camera = new Camera3D { Fov = 40, HOffset = -0.75f };
        AddChild(_camera);
        _camera.Position = new Vector3(0, 1.1f, 3.0f);
        _camera.LookAt(new Vector3(0, 0.4f, 0));
        _lobbyView = new LobbyView();
        _lobbyView.LookShown += ShowModel;
        _lobbyView.Chosen += hero =>
        {
            _hero = hero;
            _fighting = true;
            RemoveChild(_lobbyView);
            _lobbyView.QueueFree();
            _lobbyView = null;
            foreach (Node3D n in new Node3D?[] { _stage, _camera }.OfType<Node3D>())
            {
                RemoveChild(n);
                n.QueueFree();
            }
            _stage = null;
            StartFight(hero);
        };
        AddChild(_lobbyView);
        _lobbyView.Build(_lobby!, _options.Server, new Texts(_options.Lang));
        if (_options.LobbySelfTest)
            _ = RunLobbySelfTest();
        else if (_options.Shot == "lobby")
            _ = LobbyScreenshot();
    }

    /// <summary>The model of a look, turning on a patch of grass next to the form.</summary>
    private void ShowModel(string look)
    {
        _stage?.QueueFree();
        _stage = new Node3D();
        AddChild(_stage);
        _stage.AddChild(new MeshInstance3D
        {
            Mesh = new CylinderMesh { TopRadius = 0.7f, BottomRadius = 0.7f, Height = 0.06f, RadialSegments = 48 },
            MaterialOverride = new StandardMaterial3D { AlbedoColor = new Color("37602a"), Roughness = 1 },
            Position = new Vector3(0, -0.03f, 0),
        });
        var model = Looks.Fighter(look).Instantiate<Node3D>();
        _stage.AddChild(model);
        if (model.FindChild("AnimationPlayer", true, false) is AnimationPlayer anim && anim.HasAnimation("idle"))
        {
            anim.GetAnimation("idle").LoopMode = Animation.LoopModeEnum.Linear;
            anim.Play("idle");
        }
    }

    private void StartFight(Hero? hero)
    {
        ulong seed = _nextSeed ?? _options.Seed ?? (ulong)Time.GetUnixTimeFromSystem();
        _controller = new FightController(new Fight(GameData.Embedded, _options.Scenario, seed, hero));
        BuildWorld();
        if (_options.SelfTest || _options.LobbySelfTest)
            CallDeferred(MethodName.RunSelfTest);
        else if (_options.Screenshot is not null)
            _ = Screenshot();
        else
            _busy = false;
    }

    public override void _Process(double delta)
    {
        if (_stage is not null)
            _stage.RotateY((float)delta * 0.6f);
        if (_lobbyView is not null || _controller is null || _busy || _options.SelfTest || _options.LobbySelfTest || _options.Screenshot is not null || _controller.Fight.IsOver || _controller.IsPlayerTurn)
            return;
        _busy = true;
        _ = AiStep();
    }

    public override void _UnhandledInput(InputEvent @event)
    {
        if (_lobbyView is not null || _controller is null || _busy || !_controller.IsPlayerTurn)
            return;
        switch (@event)
        {
            case InputEventMouseMotion motion:
                _hovered = BoardView.Pick(_camera, motion.Position);
                ShowPreview();
                break;
            case InputEventMouseButton { Pressed: true, ButtonIndex: MouseButton.Left } click:
                if (BoardView.Pick(_camera, click.Position) is Cell cell)
                    Act(_controller.Click(cell));
                break;
            case InputEventMouseButton { Pressed: true, ButtonIndex: MouseButton.Right }:
                _controller.CancelSpell();
                ShowPreview();
                break;
            case InputEventKey { Pressed: true, Echo: false } key:
                if (key.Keycode >= Key.Key1 && key.Keycode <= Key.Key9)
                    ChooseSpell((int)(key.Keycode - Key.Key1));
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
        _hud.AgainPressed += () =>
        {
            _nextSeed = _controller.Fight.Seed + 1;
            GetTree().ReloadCurrentScene();
        };
        // Back to the characters, when signed in: the session is kept, the next fight starts after a choice.
        _hud.SetBackShown(_lobby?.SignedIn == true);
        _hud.BackPressed += () =>
        {
            _fighting = false;
            _nextSeed = null;
            GetTree().ReloadCurrentScene();
        };
        _seen = fight.Events.Count;
        RefreshViews();
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
        if (_controller.IsPlayerTurn)
            ShowPreview();
    }

    private Task Animate(FightEvent e, bool instant)
    {
        switch (e)
        {
            case Moved m:
                return _views[m.Fighter].Walk(m.Path, instant ? 0 : 0.22);
            case SpellCast c:
                Spell spell = _controller.Fight.Fighters[c.Fighter].Spells.First(s => s.Id == c.Spell);
                return _views[c.Fighter].Attack(c.Target, spell.MaxRange <= 1, instant ? 0 : 0.55);
            case Damaged d:
                _views[d.Fighter].ShowDamage(d.Amount, instant ? 0 : 0.9);
                return Task.CompletedTask;
            case Died d:
                _views[d.Fighter].Die(animate: !instant);
                return Task.CompletedTask;
            default:
                RefreshViews();
                return Task.CompletedTask;
        }
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
        if (_controller.Fight.Hero is Hero hero && !_hud.OrderText.Contains(hero.Name, StringComparison.Ordinal))
            problems.Add($"the turn order does not show the hero {hero.Name}");
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
                if (v.HpText != $"{f.Hp}/{f.Spec.Hp}")
                    problems.Add($"{f.Name.En} shows {v.HpText}, has {f.Hp}");
                checks++;
            }
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
        string report = SelfPlay.Report(_controller.Fight);
        report += problems.Count == 0
            ? $" Views checked {checks} times, {_board.Tiles} tiles."
            : $" VIEW PROBLEMS: {string.Join("; ", problems.Distinct().Take(5))}";
        if (_selfTestNote is not null)
            report += " " + _selfTestNote;
        if (problems.Count > 0)
            report = report.Replace("SELFTEST OK", "SELFTEST FAILED", StringComparison.Ordinal);
        GD.Print(report);
        if (_options.Report is string path)
            File.WriteAllText(path, report + "\n");
        GetTree().Quit(report.StartsWith("SELFTEST OK", StringComparison.Ordinal) ? 0 : 1);
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
        view.NameField.Text = "selftest_" + suffix;
        view.PasswordField.Text = "selftest password";
        await view.SignIn(create: true);
        // Letters only: the hex digits of the suffix become letters a to p.
        string hero = "Essai" + string.Concat(suffix.Select(c => (char)('a' + Convert.ToInt32(c.ToString(), 16))));
        view.CharacterField.Text = hero;
        view.LookChoice.Select(Hero.Looks.ToList().IndexOf("male-b"));
        await view.Create();
        if (_lobby!.Characters.Count != 1 || _lobby.Characters[0].Name != hero)
        {
            string report = $"SELFTEST FAILED lobby: {view.MessageText} ({_lobby.Problem})";
            GD.Print(report);
            if (_options.Report is string path)
                File.WriteAllText(path, report + "\n");
            GetTree().Quit(1);
            return;
        }
        _selfTestNote = $"Hero {hero} (male-b) made on the server and chosen on the login screen.";
        // The character's "fight" button, pressed like a click.
        Button play = view.FindChildren("*", nameof(Button), true, false).OfType<Button>().Single(b => b.Text == view.Texts["lobby.play"]);
        play.EmitSignal(BaseButton.SignalName.Pressed);
    }

    /// <summary>The character screen for the project page: signs up, creates two characters if the names are free.</summary>
    private async Task LobbyScreenshot()
    {
        LobbyView view = _lobbyView!;
        view.NameField.Text = "capture_" + Guid.NewGuid().ToString("N")[..8];
        view.PasswordField.Text = "screenshot password";
        await view.SignIn(create: true);
        foreach ((string name, string look) in new[] { ("Margaux", "female-e"), ("Élise", "female-c") })
        {
            view.CharacterField.Text = name;
            view.LookChoice.Select(Hero.Looks.ToList().IndexOf(look));
            await view.Create();
        }
        view.LookChoice.Select(Hero.Looks.ToList().IndexOf("male-c"));
        ShowModel("male-c");
        for (int i = 0; i < 30; i++)
            await ToSignal(RenderingServer.Singleton, RenderingServer.SignalName.FramePostDraw);
        Error saved = GetViewport().GetTexture().GetImage().SavePng(_options.Screenshot!);
        GD.Print($"SCREENSHOT {saved} {_options.Screenshot}");
        GetTree().Quit(saved == Error.Ok ? 0 : 1);
    }

    private sealed record Options(string Lang, string Scenario, ulong? Seed, bool SelfTest, string? Report, string? Screenshot, string Shot, string Server, bool Offline, bool LobbySelfTest)
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
                    "--server" => o with { Server = Next().TrimEnd('/') + "/" },
                    "--offline" => o with { Offline = true },
                    "--lobby-selftest" => o with { LobbySelfTest = true },
                    _ => o,
                };
            }
            return o;
        }
    }
}
