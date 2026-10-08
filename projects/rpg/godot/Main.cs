using System.Globalization;
using Godot;
using Rpg.Client;
using Rpg.Core;

namespace Rpg.Desktop;

/// <summary>
/// The fight screen. Options after "--": --lang fr|en, --scenario ID, --seed N; --selftest [--report FILE]
/// plays a whole fight through the controls and checks the views; --screenshot FILE [--shot move|spell]
/// saves a picture of the board.
/// </summary>
public partial class Main : Node3D
{
    private static ulong? _nextSeed;

    private readonly Dictionary<int, FighterView> _views = [];
    private FightController _controller = null!;
    private BoardView _board = null!;
    private Hud _hud = null!;
    private Camera3D _camera = null!;
    private Options _options = null!;
    private Cell? _hovered;
    private int _seen;
    private bool _busy;

    public override void _Ready()
    {
        _options = Options.Parse(OS.GetCmdlineUserArgs(), OS.GetLocaleLanguage());
        ulong seed = _nextSeed ?? _options.Seed ?? (ulong)Time.GetUnixTimeFromSystem();
        _controller = new FightController(new Fight(GameData.Embedded, _options.Scenario, seed));
        BuildWorld();
        if (_options.SelfTest)
            CallDeferred(MethodName.RunSelfTest);
        else if (_options.Screenshot is not null)
            _ = Screenshot();
        else
            _busy = false;
    }

    public override void _Process(double delta)
    {
        if (_busy || _options.SelfTest || _options.Screenshot is not null || _controller.Fight.IsOver || _controller.IsPlayerTurn)
            return;
        _busy = true;
        _ = AiStep();
    }

    public override void _UnhandledInput(InputEvent @event)
    {
        if (_busy || !_controller.IsPlayerTurn)
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

    private void BuildWorld()
    {
        Fight fight = _controller.Fight;
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

    private sealed record Options(string Lang, string Scenario, ulong? Seed, bool SelfTest, string? Report, string? Screenshot, string Shot)
    {
        public static Options Parse(string[] args, string locale)
        {
            var o = new Options(locale == "fr" ? "fr" : "en", "training", null, false, null, null, "move");
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
                    _ => o,
                };
            }
            return o;
        }
    }
}
