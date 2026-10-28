using Rpg.Client;
using Rpg.Core;

namespace Rpg.Desktop;

/// <summary>
/// The achievements in the game (sprint 155, D83): their window (key H), the title chosen there, and a word when one is
/// earned, checked after each fight and each contract handed back.
/// </summary>
public partial class Main
{
    private AchievementsPanel? _achievementsPanel;
    private static AchievementView? _achieved;

    private void OpenAchievements()
    {
        Texts texts = _talkPanel?.Texts ?? _hud.Texts;
        _achievementsPanel = new AchievementsPanel();
        AddChild(_achievementsPanel);
        _achievementsPanel.Build(texts);
        _achievementsPanel.ClosePressed += () =>
        {
            _windows.Close();
            ShowWindows();
        };
        _achievementsPanel.TitleChosen += id => _ = AchievementCall(async s => Achieved(await s.WearTitle(_character!.Id, id)));
        // A reward claimed (sprint 159): the character's experience and Éclats kept, a word said, the window shown again.
        _achievementsPanel.ClaimPressed += id => _ = AchievementCall(async s =>
        {
            AchievementClaim claim = await s.ClaimAchievement(_character!.Id, id);
            KeepCharacter(claim.Character);
            _achievementsPanel?.Tell(texts["achievements.claimed-note", claim.Xp, claim.Shards]);
            Achieved(claim.Achievements);
        });
        if (_lobby?.SignedIn != true || _character is null)
            _achievementsPanel.Tell(texts["achievements.needs-server"]);
        else
            _ = AchievementCall(async s => Achieved(await s.Achievements(_character.Id)));
    }

    private void CloseAchievements()
    {
        _achievementsPanel?.QueueFree();
        _achievementsPanel = null;
    }

    /// <summary>The achievements as the server now says: a word for each one earned since the last time, the window shown again.</summary>
    private void Achieved(AchievementView view)
    {
        Texts texts = _talkPanel?.Texts ?? _hud.Texts;
        foreach (string id in AchievementsPage.NewlyEarned(_achieved, view))
            Note(texts["achievements.earned", Achievements.All.First(a => a.Id == id).Name.In(texts.Lang)]);
        _achieved = view;
        _achievementsPanel?.Show(view);
        // The rewards waiting, on the menu's icon (sprint 159).
        int toClaim = AchievementsPage.ToClaim(view);
        _bar?.SetBadge(GameWindow.Achievements, toClaim, texts["achievements.to-claim", toClaim]);
    }

    /// <summary>After a fight or a contract: the achievements asked again, quietly (online only).</summary>
    private void CheckAchievements()
    {
        if (_lobby?.SignedIn == true && _character is not null)
            _ = AchievementCall(async s => Achieved(await s.Achievements(_character.Id)));
    }

    private async Task AchievementCall(Func<GameServer, Task> call)
    {
        Texts texts = _talkPanel?.Texts ?? _hud.Texts;
        try
        {
            await call(_lobby!.Server);
        }
        catch (Exception e) when (e is ServerException or HttpRequestException)
        {
            _achievementsPanel?.Tell(e is ServerException refused ? texts["result.refused", refused.Message] : texts["lobby.unreachable"]);
        }
    }
}
