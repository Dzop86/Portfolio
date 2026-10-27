using Godot;
using Rpg.Client;
using Rpg.Core;

namespace Rpg.Desktop;

/// <summary>
/// The guild in the game (sprint 145, D79): its window (G), the server's news of it (an invitation asked at the top of the
/// screen, the guild as it now is, no guild any more), founding one after Edmée's answer, and the calls the window makes.
/// </summary>
public partial class Main
{
    private static Guid? _guildOf;
    private static GuildView? _guild;
    private GuildPanel? _guildPanel;

    /// <summary>The guild's name the player carries, or null (for the self-test and the player's menu).</summary>
    private static string? GuildName => _guild?.Name;

    // The guild of the character now played: asked of the server once a character, then kept by the news.
    private void ShowGuild()
    {
        if (_character is not null && _guildOf != _character.Id)
        {
            (_guild, _guildOf) = (null, _character.Id);
            _ = RefreshGuild();
        }
    }

    private async Task RefreshGuild()
    {
        if (_lobby?.SignedIn != true || _character is null)
            return;
        try
        {
            OnGuild(await _lobby.Server.Guild(_character.Id), quiet: true);
        }
        catch (Exception e) when (e is ServerException or HttpRequestException)
        {
            // No guild shown; the next news will say.
        }
    }

    /// <summary>What the server says of the guild: someone invites, the guild now, no guild any more.</summary>
    private void OnGuild(GuildNews news, bool quiet = false)
    {
        Texts texts = _talkPanel?.Texts ?? _hud.Texts;
        switch (news.Guild)
        {
            case GuildNewsKind.Invited when news is { From: Guid from, Name: string name, GuildName: string guild }:
                _askPanel?.Ask(texts["guild.invited", name, guild], texts["guild.join"], texts["guild.decline"], () => _ = GuildCall(s => s.JoinGuild(_character!.Id, from)));
                break;
            case GuildNewsKind.Changed when news.View is GuildView view:
                if (!quiet && _guild is null)
                    Note(texts[view.Members.Count == 1 ? "guild.founded" : "guild.joined", view.Name]);
                _guild = view;
                _guildPanel?.Show(view, _character?.Id ?? Guid.Empty, DateTimeOffset.UtcNow);
                break;
            default:
                if (!quiet && _guild is not null)
                    Note(texts["guild.left"]);
                _guild = null;
                _guildPanel?.Show(null, _character?.Id ?? Guid.Empty, DateTimeOffset.UtcNow);
                break;
        }
    }

    /// <summary>The guild's window (sprint 145): the guild as last told, each change asked of the server.</summary>
    private void OpenGuild()
    {
        Texts texts = _talkPanel?.Texts ?? _hud.Texts;
        _guildPanel = new GuildPanel();
        AddChild(_guildPanel);
        _guildPanel.Build(texts);
        _guildPanel.ClosePressed += () =>
        {
            _windows.Close();
            ShowWindows();
        };
        _guildPanel.FoundPressed += name => _ = GuildCall(async s => KeepCharacter(await s.FoundGuild(_character!.Id, name)));
        _guildPanel.InvitePressed += name => _ = GuildCall(async s =>
        {
            await s.InviteToGuild(_character!.Id, name);
            Note(texts["party.sent", name]);
        });
        _guildPanel.LeavePressed += () => _ = GuildCall(s => s.LeaveGuild(_character!.Id));
        _guildPanel.KickPressed += member => _ = GuildCall(s => s.KickFromGuild(_character!.Id, member));
        _guildPanel.RankPressed += (member, rank) => _ = GuildCall(s => s.SetGuildRank(_character!.Id, member, rank));
        _guildPanel.Show(_guild, _character?.Id ?? Guid.Empty, DateTimeOffset.UtcNow);
        if (_lobby?.SignedIn != true || _character is null)
            _guildPanel.Tell(texts["guild.needs-server"]);
        else
            _ = RefreshGuild();
    }

    private void CloseGuild()
    {
        _guildPanel?.QueueFree();
        _guildPanel = null;
    }

    /// <summary>Edmée's answer (sprint 145): the guild's window, with the name to found one.</summary>
    private void OfferFounding()
    {
        if (_windows.Open != GameWindow.Guild)
        {
            _windows.Toggle(GameWindow.Guild);
            ShowWindows();
        }
        _guildPanel?.OfferFounding();
    }

    // One call about the guild; the news on the socket shows the guild as it is, a refusal is said in the window.
    private async Task GuildCall(Func<GameServer, Task> call)
    {
        Texts texts = _talkPanel?.Texts ?? _hud.Texts;
        if (_lobby?.SignedIn != true || _character is null)
        {
            _guildPanel?.Tell(texts["guild.needs-server"]);
            return;
        }
        try
        {
            await call(_lobby.Server);
            _guildPanel?.Tell("");
        }
        catch (Exception e) when (e is ServerException or HttpRequestException)
        {
            string said = e is ServerException refused ? texts["result.refused", refused.Message] : texts["lobby.unreachable"];
            if (_guildPanel is not null)
                _guildPanel.Tell(said);
            else
                Note(said);
        }
    }
}
