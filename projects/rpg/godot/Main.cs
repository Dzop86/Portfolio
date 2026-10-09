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
/// --screenshot FILE [--shot move|spell|lobby|town|banner] saves a picture; --demo lets the AI play the
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
        if (_options.SelfTest || _options.Demo || _options.Shot is "move" or "spell" && _options.Screenshot is not null)
            StartFight(_hero, _options.Scenario);
        else if (_options.TownSelfTest || _options.Shot is "town" or "banner")
            ShowTown();
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
        ulong seed = _nextSeed ?? _options.Seed ?? (ulong)Time.GetUnixTimeFromSystem();
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
        _hud.AgainPressed += () =>
        {
            _nextSeed = _controller.Fight.Seed + 1;
            GetTree().ReloadCurrentScene();
        };
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
            case Pushed p when p.Path.Count > 0:
                return _views[p.Fighter].Walk(p.Path, instant ? 0 : 0.12);
            case Healed h:
                _views[h.Fighter].ShowNumber($"+{h.Amount}", new Color(0.55f, 0.95f, 0.45f), instant ? 0 : 0.9);
                return Task.CompletedTask;
            case ShieldAbsorbed a:
                _views[a.Fighter].ShowNumber($"-{a.Amount}", new Color(0.55f, 0.75f, 1f), instant ? 0 : 0.9);
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
        if (_controller.Fight.Hero is Hero hero)
        {
            if (!_hud.OrderText.Contains(hero.Name, StringComparison.Ordinal))
                problems.Add($"the turn order does not show the hero {hero.Name}");
            FighterView heroView = _views.Values.Single(v => v.Fighter.Name.Fr == hero.Name);
            if (heroView.Painted != hero.Colour)
                problems.Add($"the hero is painted {heroView.Painted?.ToString(CultureInfo.InvariantCulture) ?? "not at all"}, not {hero.Colour}");
            if (GameData.Embedded.Class(hero.Class) is HeroClass c && !_hud.SpellsText.SequenceEqual(c.Spells.Select(id => GameData.Embedded.Spells[id].Name.In(_hud.Texts.Lang))))
                problems.Add($"the spell bar shows {string.Join(", ", _hud.SpellsText)}, not the {c.Id}'s spells");
        }
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
            view.ChooseColour(3);
            view.ChooseHair(4);
            view.ChooseSkin(2);
            view.SetShape(1, -1);
            await view.Create();
            string expected = $"{hero} · {GameData.Embedded.Class("mage")!.Name.In(view.Texts.Lang)} · {view.Texts["lobby.level", 1]}";
            problem = view.CreateOpen ? $"the creation panel stayed open: {view.MessageText}"
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
        foreach (Hero h in new[] { new Hero("Margaux", "female-e", "sentinel", 0, 3, 0, 0, 0), new Hero("Élise", "female-c", "mage", 4, 7, 1, -1, 0), new Hero("Bastien", "male-c", "guard", 2, 1, 4, 2, 2) })
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
