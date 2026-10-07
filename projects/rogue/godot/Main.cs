using System.Globalization;
using Godot;
using Rogue.Client;
using Rogue.Core;
using HttpClient = System.Net.Http.HttpClient;

namespace Rogue.Desktop;

/// <summary>
/// The desktop client: a menu, the game (keyboard or autopilot), ranked runs on the score API. The
/// rules, texts and HTTP client come from the C# libraries; this class only wires them to Godot.
/// Command-line options after <c>--</c>: <c>--lang fr|en</c>, <c>--seed N</c>,
/// <c>--selftest [--report FILE]</c> (plays a whole run without a screen and checks it, for the CI) and
/// <c>--screenshot FILE --turns N</c> (saves the window after N autopilot turns or more, once a monster is in sight).
/// </summary>
public partial class Main : Control
{
    private enum Mode
    {
        Free,
        Ranked,
        Watch,
    }

    private const double AutopilotStep = 0.06;
    private const int LogLines = 4;
    private const string DefaultServer = "http://localhost:8001/";

    private readonly List<string> _messages = [];
    private Texts _texts = new(Language.French);
    private Game? _game;
    private Mode _mode;
    private double _sinceStep;
    private RunTicket? _ticket;
    private HttpClient? _http;
    private ScoresClient? _scores;

    private MapView _map = null!;
    private Label _status = null!;
    private Label _banner = null!;
    private Label _log = null!;
    private Label _keys = null!;
    private Control _menu = null!;
    private Control _login = null!;
    private Control _end = null!;
    private Label _endText = null!;
    private Label _loginMessage = null!;
    private LineEdit _server = null!;
    private LineEdit _name = null!;
    private LineEdit _password = null!;
    private readonly Dictionary<Control, Func<Texts, string>> _captions = [];

    public override void _Ready()
    {
        BuildInterface();
        string[] args = OS.GetCmdlineUserArgs();
        string? Arg(string name) => Array.IndexOf(args, name) is int i and >= 0 && i + 1 < args.Length ? args[i + 1] : null;

        Language language = (Arg("--lang") ?? OS.GetLocaleLanguage()) == "fr" ? Language.French : Language.English;
        SetLanguage(language);
        ulong seed = ulong.TryParse(Arg("--seed"), NumberStyles.None, CultureInfo.InvariantCulture, out ulong s) ? s : (ulong)Random.Shared.NextInt64();

        if (args.Contains("--selftest"))
            SelfTest(seed, Arg("--report"));
        else if (Arg("--screenshot") is string path)
            Screenshot(path, seed, int.TryParse(Arg("--turns"), CultureInfo.InvariantCulture, out int turns) ? turns : 100);
        else
            ShowOnly(_menu);
    }

    public override void _Process(double delta)
    {
        if (_mode != Mode.Watch || _game is not { State: GameState.Playing } || OverlayShown)
            return;
        _sinceStep += delta;
        if (_sinceStep < AutopilotStep)
            return;
        _sinceStep = 0;
        Play(Autopilot.Choose(_game));
    }

    public override void _UnhandledInput(InputEvent @event)
    {
        if (@event is InputEventKey { Pressed: true } key && HandleKey(key))
            GetViewport().SetInputAsHandled();
    }

    /// <summary>Turns a key into an action of the game in progress; returns whether the key was used.</summary>
    private bool HandleKey(InputEventKey key)
    {
        if (_game is null || OverlayShown)
            return false;
        Command command = key.Keycode switch
        {
            Key.Up => new Command(GameAction.North),
            Key.Down => new Command(GameAction.South),
            Key.Right => new Command(GameAction.East),
            Key.Left => new Command(GameAction.West),
            Key.Enter or Key.KpEnter => new Command(GameAction.Descend),
            Key.Escape => Command.Stop,
            _ => Keys.FromChar((char)key.Unicode),
        };
        if (command.Quit)
        {
            // A ranked run left this way stays open on the server until it expires.
            _game = null;
            _map.Game = null;
            ShowOnly(_menu);
            return true;
        }
        if (command.Action is not GameAction action || _mode == Mode.Watch)
            return false;
        Play(action);
        return true;
    }

    private void Play(GameAction action)
    {
        if (_game is not { State: GameState.Playing } game)
            return;
        if (!game.CanApply(action))
            Say([_texts.Impossible(action)]);
        else
            Say(game.Apply(action).Select(_texts.Describe));
        Refresh();
        if (game.State != GameState.Playing)
            GameOver(game);
    }

    private void Say(IEnumerable<string> messages)
    {
        _messages.AddRange(messages);
        if (_messages.Count > LogLines)
            _messages.RemoveRange(0, _messages.Count - LogLines);
    }

    private void StartGame(ulong seed, Mode mode)
    {
        _game = new Game(seed);
        _mode = mode;
        _map.Game = _game;
        _messages.Clear();
        ShowOnly(null);
        Refresh();
    }

    private void Refresh()
    {
        if (_game is null)
            return;
        _status.Text = $"{_texts.Status(_game)}   {_texts.Seed(_game.Seed)}";
        _banner.Text = _mode switch
        {
            Mode.Ranked => _texts.Ranked,
            Mode.Watch => _texts.Watching,
            _ => "",
        };
        _log.Text = string.Join('\n', _messages);
        _keys.Text = _texts.WindowKeys;
        _map.QueueRedraw();
    }

    private async void GameOver(Game game)
    {
        var lines = new List<string> { _texts.End(game) };
        _endText.Text = string.Join('\n', lines);
        ShowOnly(_end);
        if (_mode != Mode.Ranked || _scores is null || _ticket is null)
            return;
        _endText.Text += "\n\n" + _texts.Submitting;
        try
        {
            RunVerdict verdict = await _scores.SubmitAsync(_ticket, RunRecord.From(game));
            lines.Add("");
            lines.Add(_texts.Verdict(verdict));
            IReadOnlyList<ScoreLine> board = await _scores.LeaderboardAsync(10);
            lines.Add("");
            lines.Add(_texts.Leaderboard);
            lines.AddRange(board.Select(_texts.ScoreLine));
        }
        catch (ScoresException e)
        {
            lines.Add("");
            lines.Add(_texts.Problem(e));
        }
        catch (Exception e) when (e is HttpRequestException or TaskCanceledException)
        {
            lines.Add("");
            lines.Add(_texts.Unreachable);
        }
        _ticket = null;
        _endText.Text = string.Join('\n', lines);
    }

    private async void SignInAndPlay()
    {
        if (!Connect())
            return;
        try
        {
            await _scores!.SignInAsync(_name.Text.Trim(), _password.Text);
            _ticket = await _scores.StartRunAsync();
            StartGame(_ticket.SeedValue, Mode.Ranked);
        }
        catch (ScoresException e)
        {
            _loginMessage.Text = _texts.Problem(e);
        }
        catch (Exception e) when (e is HttpRequestException or TaskCanceledException)
        {
            _loginMessage.Text = _texts.Unreachable;
        }
    }

    private async void SignUp()
    {
        if (!Connect())
            return;
        try
        {
            await _scores!.SignUpAsync(_name.Text.Trim(), _password.Text);
            _loginMessage.Text = _texts.AccountCreated;
        }
        catch (ScoresException e)
        {
            _loginMessage.Text = _texts.Problem(e);
        }
        catch (Exception e) when (e is HttpRequestException or TaskCanceledException)
        {
            _loginMessage.Text = _texts.Unreachable;
        }
    }

    /// <summary>A fresh HTTP client for the server typed in the form (signing in again forgets the previous token).</summary>
    private bool Connect()
    {
        string address = _server.Text.Trim();
        if (!address.EndsWith('/'))
            address += "/";
        if (!Uri.TryCreate(address, UriKind.Absolute, out Uri? server) || server.Scheme is not ("http" or "https"))
        {
            _loginMessage.Text = _texts.Unreachable;
            return false;
        }
        _http?.Dispose();
        _http = new HttpClient { BaseAddress = server, Timeout = TimeSpan.FromSeconds(10) };
        _scores = new ScoresClient(_http);
        _loginMessage.Text = "";
        return true;
    }

    private void SetLanguage(Language language)
    {
        _texts = new Texts(language);
        foreach ((Control control, Func<Texts, string> caption) in _captions)
        {
            if (control is Button button)
                button.Text = caption(_texts);
            else if (control is Label label)
                label.Text = caption(_texts);
            else if (control is LineEdit edit)
                edit.PlaceholderText = caption(_texts);
        }
        Refresh();
    }

    private bool OverlayShown => _menu.Visible || _login.Visible || _end.Visible;

    private void ShowOnly(Control? overlay)
    {
        foreach (Control panel in new[] { _menu, _login, _end })
            panel.Visible = panel == overlay;
        // The first button of a panel takes the focus: the menus work with the keyboard alone.
        overlay?.FindChildren("*", nameof(Button), owned: false).OfType<Button>().FirstOrDefault()?.CallDeferred(Control.MethodName.GrabFocus);
    }

    /// <summary>For the CI: plays a whole run through the same path as the keyboard, then checks it.</summary>
    private void SelfTest(ulong seed, string? report)
    {
        StartGame(seed, Mode.Free);
        var failures = new List<string>();
        int turn = _game!.Turn;
        HandleKey(new InputEventKey { Keycode = Key.Space, Unicode = ' ', Pressed = true });
        if (_game.Turn != turn + 1)
            failures.Add("the space bar did not wait");
        while (_game.State == GameState.Playing)
            Play(Autopilot.Choose(_game));
        ReplayResult replay = Replay.Run(RunRecord.From(_game));
        if (!replay.IsValid || replay.Score != _game.Score)
            failures.Add($"replay gives {replay.Error}, score {replay.Score} instead of {_game.Score}");
        if (!_end.Visible || !_endText.Text.Contains(_game.Score.ToString(CultureInfo.InvariantCulture), StringComparison.Ordinal))
            failures.Add("the end panel does not show the score");
        string before = NewGameButton.Text;
        Language other = _texts.Language == Language.French ? Language.English : Language.French;
        SetLanguage(other);
        if (NewGameButton.Text == before || NewGameButton.Text != new Texts(other).NewGame)
            failures.Add("the menu did not switch language");

        string summary = failures.Count == 0
            ? $"SELFTEST OK seed {seed}: {_game.State}, floor {_game.Depth}, {_game.Turn} turns, score {_game.Score}"
            : "SELFTEST FAILED: " + string.Join("; ", failures);
        GD.Print(summary);
        // On Windows, Godot's window executable writes nothing to the console: the CI reads this file.
        if (report is not null)
            File.WriteAllText(report, summary + "\n");
        GetTree().Quit(failures.Count == 0 ? 0 : 1);
    }

    private async void Screenshot(string path, ulong seed, int turns)
    {
        StartGame(seed, Mode.Free);
        // At least the given number of turns, then on until a monster is in sight: a picture with a fight in it.
        bool MonsterInSight() => _game!.Monsters.Any(m => _game.IsVisible(m.Position));
        for (int i = 0; _game!.State == GameState.Playing && (i < turns || !MonsterInSight()) && i < turns + 1000; i++)
            Play(Autopilot.Choose(_game));
        ShowOnly(null);
        for (int i = 0; i < 3; i++)
            await ToSignal(RenderingServer.Singleton, RenderingServer.SignalName.FramePostDraw);
        Error saved = GetViewport().GetTexture().GetImage().SavePng(path);
        GD.Print($"Screenshot {path}: {saved}");
        GetTree().Quit(saved == Error.Ok ? 0 : 1);
    }

    private Button NewGameButton { get; set; } = null!;

    private void BuildInterface()
    {
        DisplayServer.WindowSetTitle("Rogue");
        var margin = new MarginContainer();
        margin.SetAnchorsPreset(LayoutPreset.FullRect);
        foreach (string side in new[] { "left", "right", "top", "bottom" })
            margin.AddThemeConstantOverride($"margin_{side}", 16);
        AddChild(margin);
        var column = new VBoxContainer();
        margin.AddChild(column);

        var header = new HBoxContainer();
        column.AddChild(header);
        _status = new Label { SizeFlagsHorizontal = SizeFlags.ExpandFill };
        _status.AddThemeFontSizeOverride("font_size", 18);
        header.AddChild(_status);
        _banner = new Label();
        _banner.AddThemeColorOverride("font_color", new Color("#bef374"));
        _banner.AddThemeFontSizeOverride("font_size", 18);
        header.AddChild(_banner);

        _map = new MapView { SizeFlagsVertical = SizeFlags.ExpandFill };
        column.AddChild(_map);
        _log = new Label { CustomMinimumSize = new Vector2(0, 4 * 24) };
        _log.AddThemeFontSizeOverride("font_size", 17);
        column.AddChild(_log);
        _keys = new Label();
        _keys.AddThemeColorOverride("font_color", new Color("#a0a0a0"));
        column.AddChild(_keys);

        var menu = Panel(out _menu);
        var title = new Label { Text = "Rogue", HorizontalAlignment = HorizontalAlignment.Center };
        title.AddThemeFontSizeOverride("font_size", 40);
        title.AddThemeColorOverride("font_color", new Color("#bef374"));
        menu.AddChild(title);
        NewGameButton = AddButton(menu, t => t.NewGame, () => StartGame((ulong)Random.Shared.NextInt64(), Mode.Free));
        AddButton(menu, t => t.RankedGame, () => ShowOnly(_login));
        AddButton(menu, t => t.WatchAutopilot, () => StartGame((ulong)Random.Shared.NextInt64(), Mode.Watch));
        AddButton(menu, t => t.OtherLanguage, () => SetLanguage(_texts.Language == Language.French ? Language.English : Language.French));
        AddButton(menu, t => t.QuitGame, () => GetTree().Quit());

        var form = Panel(out _login);
        form.AddChild(Caption(t => t.Server));
        _server = new LineEdit { Text = DefaultServer, CustomMinimumSize = new Vector2(360, 0) };
        form.AddChild(_server);
        form.AddChild(Caption(t => t.Name));
        _name = new LineEdit { MaxLength = 20 };
        form.AddChild(_name);
        form.AddChild(Caption(t => t.Password));
        _password = new LineEdit { Secret = true, MaxLength = 128 };
        form.AddChild(_password);
        _loginMessage = new Label { AutowrapMode = TextServer.AutowrapMode.WordSmart, CustomMinimumSize = new Vector2(360, 0) };
        _loginMessage.AddThemeColorOverride("font_color", new Color("#ffb05c"));
        form.AddChild(_loginMessage);
        AddButton(form, t => t.SignIn, SignInAndPlay);
        AddButton(form, t => t.SignUp, SignUp);
        AddButton(form, t => t.Back, () => ShowOnly(_menu));

        var end = Panel(out _end);
        _endText = new Label();
        _endText.AddThemeFontOverride("font", new SystemFont { FontNames = ["DejaVu Sans Mono", "Consolas", "Menlo", "monospace"] });
        end.AddChild(_endText);
        AddButton(end, t => t.Menu, () => ShowOnly(_menu));
    }

    /// <summary>A centred panel over the map, holding a column of controls.</summary>
    private VBoxContainer Panel(out Control overlay)
    {
        var center = new CenterContainer();
        center.SetAnchorsPreset(LayoutPreset.FullRect);
        AddChild(center);
        var panel = new PanelContainer();
        center.AddChild(panel);
        var padding = new MarginContainer();
        foreach (string side in new[] { "left", "right", "top", "bottom" })
            padding.AddThemeConstantOverride($"margin_{side}", 24);
        panel.AddChild(padding);
        var column = new VBoxContainer();
        column.AddThemeConstantOverride("separation", 10);
        padding.AddChild(column);
        overlay = center;
        return column;
    }

    private Button AddButton(VBoxContainer parent, Func<Texts, string> caption, Action pressed)
    {
        var button = new Button { CustomMinimumSize = new Vector2(320, 44) };
        button.Pressed += pressed;
        _captions[button] = caption;
        parent.AddChild(button);
        return button;
    }

    private Label Caption(Func<Texts, string> caption)
    {
        var label = new Label();
        _captions[label] = caption;
        return label;
    }
}
