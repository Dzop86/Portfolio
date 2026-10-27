using Rpg.Client;
using Rpg.Core;

namespace Rpg.Desktop;

/// <summary>The bestiary's window (sprint 154, D83): key B, its tile in the Menus panel; what the character's victories tell.</summary>
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
        _bestiaryPanel.Show(_character?.Bestiary ?? new Dictionary<string, int>());
    }

    private void CloseBestiary()
    {
        _bestiaryPanel?.QueueFree();
        _bestiaryPanel = null;
    }
}
