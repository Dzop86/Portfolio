using Rpg.Client;
using Rpg.Core;

namespace Rpg.Desktop;

/// <summary>
/// The bestiary's window (sprint 154, D83): key B, its tile in the Menus panel; what the victories tell, since sprint 168
/// (D87, B7) those of the whole account: each monster at the best count of the player's characters (<see cref="Bestiary.Best"/>).
/// </summary>
public partial class Main
{
    private BestiaryPanel? _bestiaryPanel;

    private void OpenBestiary()
    {
        Texts texts = _talkPanel?.Texts ?? _hud.Texts;
        _bestiaryPanel = new BestiaryPanel();
        AddChild(_bestiaryPanel);
        _bestiaryPanel.Build(texts);
        _bestiaryPanel.ClosePressed += () =>
        {
            _windows.Close();
            ShowWindows();
        };
        _bestiaryPanel.Show(AccountLog());
    }

    /// <summary>The account's hunting log: the character played (its count up to date) and the others the lobby listed.</summary>
    private static Dictionary<string, int> AccountLog() =>
        Bestiary.Best([_character?.Bestiary, .. (_lobby?.Characters ?? []).Where(c => c.Id != _character?.Id).Select(c => c.Bestiary)]);

    private void CloseBestiary()
    {
        _bestiaryPanel?.QueueFree();
        _bestiaryPanel = null;
    }
}
