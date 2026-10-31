using Rpg.Client;
using Rpg.Core;

namespace Rpg.Desktop;

/// <summary>
/// The bestiary's window (sprint 154, D83): key B, its tile in the Menus panel; what the victories tell, since sprint 168
/// (D87, B7) those of the whole account: each monster at the best count of the player's characters (<see cref="Bestiary.Best"/>).
/// Since sprint 175 (B4) it leads elsewhere and back: its magnifier to the world map on the zone (a button there comes back
/// to the same card, filters kept), its loot to the items' cards, whose Bestiary tab leads to the monsters' cards.
/// </summary>
public partial class Main
{
    private BestiaryPanel? _bestiaryPanel;

    // The card and the filters the magnifier left, for the map's way back.
    private (BestiaryFilter Filter, string Monster)? _bestiaryBack;

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
        // The map is only out of a fight.
        _bestiaryPanel.CanLocate = _town is not null;
        _bestiaryPanel.LocatePressed += (zone, maps) =>
        {
            BestiaryPanel panel = _bestiaryPanel!;
            (BestiaryFilter Filter, string Monster)? back = panel.Chosen is string chosen ? (panel.Filter, chosen) : null;
            _windows.Toggle(GameWindow.Map);
            ShowWindows();
            _bestiaryBack = back;
            if (_mapPanel is null)
                return;
            _mapPanel.Focus(zone, maps);
            _mapPanel.BackPressed += BackToBestiary;
        };
        _bestiaryPanel.ItemPressed += item => OpenItemSheet(item, ItemAction.Bestiary);
        // A quest's item shows in the loot once the quest is under way or done; the hero's deck against the weakness.
        _bestiaryPanel.Show(AccountLog(), new HashSet<string>([.. _character?.Quests ?? [], .. (_character?.QuestLog ?? []).Select(p => p.Quest)], StringComparer.Ordinal),
            _hero?.DeckOf(GameData.Embedded));
    }

    /// <summary>The map's "Bestiary": the bestiary again, on the card and with the filters the magnifier left.</summary>
    private void BackToBestiary()
    {
        (BestiaryFilter Filter, string Monster)? back = _bestiaryBack;
        _windows.Toggle(GameWindow.Bestiary);
        ShowWindows();
        if (back is not (BestiaryFilter filter, string monster) || _bestiaryPanel is null)
            return;
        _bestiaryPanel.Apply(filter);
        ShowCard(monster);
    }

    /// <summary>A monster's card in the bestiary, opened if it is not (an item's Bestiary tab).</summary>
    private void ShowCard(string monster)
    {
        if (_bestiaryPanel is null)
        {
            if (_hero?.Class is null)
                return;
            _windows.Toggle(GameWindow.Bestiary);
            ShowWindows();
        }
        if (_bestiaryPanel is null || !GameData.Embedded.Monsters.ContainsKey(monster))
            return;
        // Shown in its family, unless the list found it.
        if (!_bestiaryPanel.Filter.IsEmpty && _bestiaryPanel.FoundIds.Contains(monster))
            _bestiaryPanel.ShowMonster(monster);
        else
        {
            _bestiaryPanel.Apply(new BestiaryFilter());
            _bestiaryPanel.ShowFamily(GameData.Embedded.Families.Values.First(f => f.Monsters.Any(m => m.Id == monster)).Id);
            _bestiaryPanel.ShowMonster(monster);
        }
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
