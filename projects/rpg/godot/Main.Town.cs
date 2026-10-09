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
    private PointsPanel? _pointsPanel;
    private InventoryPanel? _inventoryPanel;
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
        _talkPanel.SetProgress(_hero?.Class is null ? null : _hero.Level, _character?.Xp ?? 0);
        _talkPanel.PointsPressed += OpenPoints;
        _talkPanel.InventoryPressed += () => OpenInventory(_character?.Inventory ?? []);
        _talkPanel.Answered += Answer;
        _talkPanel.LanguageChanged += () => _townView.SetLanguage(_talkPanel.Texts.Lang);
        _talkPanel.CharactersPressed += () =>
        {
            _screen = Screen.Lobby;
            GetTree().ReloadCurrentScene();
        };
        if (_options.TownSelfTest || _options.LobbySelfTest)
            Callable.From(() => { _ = RunTownSelfTest(); }).CallDeferred();
        else if (_options.Shot is "town" or "banner" or "inventory")
            _ = TownScreenshot();
    }

    private void TownInput(InputEvent @event)
    {
        if (_town is null || _townView is null || _walking || _pointsPanel is not null || _inventoryPanel is not null)
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

    /// <summary>The characteristics and spells screen, over the town.</summary>
    private void OpenPoints()
    {
        if (_pointsPanel is not null || _hero is null)
            return;
        _pointsPanel = new PointsPanel();
        AddChild(_pointsPanel);
        _pointsPanel.Build(new PointsEditor(_hero, GameData.Embedded), _talkPanel!.Texts);
        _pointsPanel.SavePressed += p => _ = SavePoints(p);
        _pointsPanel.ClosePressed += () =>
        {
            _pointsPanel.QueueFree();
            _pointsPanel = null;
        };
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
            _inventoryPanel.QueueFree();
            _inventoryPanel = null;
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
        if (_options.Shot == "inventory")
        {
            // A showcase for the project page: a tenth-level guard with what training fights leave.
            _hero = new Hero("Aubépine", "female-b", "guard", 5, 5, 4, -1, 1, Level: 10);
            _talkPanel!.SetProgress(10, 5_000);
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
