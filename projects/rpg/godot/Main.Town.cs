using Godot;
using Rpg.Client;
using Rpg.Core;

namespace Rpg.Desktop;

/// <summary>The town between the login screen and the fights: walking, talking, the gate.</summary>
public partial class Main
{
    private const string Village = "clairval";

    // Where the player stood in town, for the way back from a fight when no server keeps it.
    private static Cell? _place;

    private TownController? _town;
    private TownView? _townView;
    private TalkPanel? _talkPanel;
    private bool _walking;

    private void ShowTown()
    {
        _screen = Screen.Town;
        Cell? at = _character?.Place is Place p && p.Town == Village ? p.Cell : _place;
        _town = new TownController(GameData.Embedded, Village, at);
        _townView = new TownView();
        AddChild(_townView);
        _townView.Build(_town, _options.Lang, _hero ?? new Hero("", "female-d"));
        var centre = new Vector3((_town.Board.Width - 1) / 2f, 0, (_town.Board.Height - 1) / 2f);
        _camera = new Camera3D { Projection = Camera3D.ProjectionType.Orthogonal, Size = 13.5f, RotationDegrees = new Vector3(-30, 45, 0) };
        AddChild(_camera);
        _camera.Position = centre + _camera.Transform.Basis.Z * 30f + new Vector3(0, -0.4f, 0);
        _talkPanel = new TalkPanel();
        AddChild(_talkPanel);
        _talkPanel.Build(_town, new Texts(_options.Lang), _lobby?.SignedIn == true && _character is not null);
        _talkPanel.Answered += Answer;
        _talkPanel.LanguageChanged += () => _townView.SetLanguage(_talkPanel.Texts.Lang);
        _talkPanel.CharactersPressed += () =>
        {
            _screen = Screen.Lobby;
            GetTree().ReloadCurrentScene();
        };
        if (_options.TownSelfTest || _options.LobbySelfTest)
            Callable.From(() => { _ = RunTownSelfTest(); }).CallDeferred();
        else if (_options.Shot is "town" or "banner")
            _ = TownScreenshot();
    }

    private void TownInput(InputEvent @event)
    {
        if (_town is null || _townView is null || _walking)
            return;
        switch (@event)
        {
            case InputEventMouseMotion motion when BoardView.Pick(_camera, motion.Position) is Cell over:
                _townView.ShowPath(_town.PathTo(over));
                break;
            case InputEventMouseButton { Pressed: true, ButtonIndex: MouseButton.Left } click when BoardView.Pick(_camera, click.Position) is Cell cell:
                _ = Walk(cell);
                break;
            case InputEventKey { Pressed: true, Echo: false } key when _town.Talk is not null && key.Keycode >= Key.Key1 && key.Keycode <= Key.Key4:
                int index = (int)(key.Keycode - Key.Key1);
                if (index < _town.Talk.Line.Answers.Count)
                    Answer(index);
                break;
        }
    }

    private async Task Walk(Cell target)
    {
        if (_town!.Click(target) is not TownStep step)
            return;
        _walking = true;
        await _townView!.Walk(step, 0.18);
        _walking = false;
        _talkPanel!.Refresh();
        await SavePlace();
        if (_town.Fight is string scenario)
            EnterFight(scenario);
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
        var place = new Place(Village, _town.Position.X, _town.Position.Y);
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

    private void EnterFight(string scenario)
    {
        _place = _town!.Position;
        _scenario = scenario;
        _screen = Screen.Fight;
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
        string note = $"Town: {talks} people, {lines} lines";
        if (_lobby?.SignedIn == true && _character is not null)
        {
            bool saved = await SavePlace();
            Place? back = (await _lobby.Server.Characters()).Single(c => c.Id == _character.Id).Place;
            if (!saved || back != new Place(Village, _town!.Position.X, _town.Position.Y))
                problems.Add($"the server keeps the place {back}, not {_town!.Position}");
            else
                note += $", place {_town.Position} saved on the server";
        }
        note += problems.Count == 0 ? ", views checked." : $". TOWN PROBLEMS: {string.Join("; ", problems.Distinct().Take(5))}";
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
    /// The town for the project page: the player has walked up to Aubin, who talks; or, for the
    /// launcher's banner, the village alone, without the screen's texts.
    /// </summary>
    private async Task TownScreenshot()
    {
        Npc aubin = _town!.Town.Npcs[0];
        await _townView!.Walk(_town.Click(aubin.At)!, 0);
        if (_options.Shot == "banner")
        {
            _talkPanel!.Visible = false;
            _townView.HideExitLabels();
            _townView.HideNames();
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
