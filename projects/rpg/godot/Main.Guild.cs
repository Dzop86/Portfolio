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

    // Whether the player wants a word when a member comes into play (sprint 160): the window's box, kept while the game runs.
    private static bool _notifyOnline = true;

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
                _townView?.ShowOwnGuild(view.Name, view.Standing?.Crest);
                break;
            case GuildNewsKind.Online when news.Name is string came:
                // A member comes into play (sprint 160), said if the player asked.
                if (_notifyOnline)
                    Note(texts["guild.online-now", came]);
                break;
            case GuildNewsKind.Applied when news.Name is string applicant:
                // Someone applies (sprint 160): the leader told, the recruitment's tab refreshed if open.
                Note(texts["guild.applied", applicant]);
                if (_guildPanel?.Shown == GuildPanel.Tab.Recruitment)
                    _ = GuildCall(async s => _guildPanel?.ShowApplications(await s.GuildApplications(_character!.Id)));
                break;
            default:
                if (!quiet && _guild is not null)
                    Note(texts["guild.left"]);
                _guild = null;
                _guildPanel?.Show(null, _character?.Id ?? Guid.Empty, DateTimeOffset.UtcNow);
                _townView?.ShowOwnGuild(null);
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
        _guildPanel.NoticePressed += (message, goal) => _ = GuildCall(async s =>
        {
            await s.SetGuildNotice(_character!.Id, message, goal);
            _guildPanel?.Tell(texts["guild.saved"]);
        });
        _guildPanel.CrestPressed += crest => _ = GuildCall(async s =>
        {
            await s.SetGuildCrest(_character!.Id, crest);
            _guildPanel?.Tell(texts["guild.saved"]);
        });
        // Sprint 160: notes, applications, the tabs' contents asked of the server when opened.
        _guildPanel.NotePressed += (member, note) => _ = GuildCall(s => s.SetGuildNote(_character!.Id, member, note));
        _guildPanel.ApplyPressed += (name, word) => _ = GuildCall(async s =>
        {
            await s.ApplyToGuild(_character!.Id, name, word);
            _guildPanel?.Tell(texts["guild.apply.sent", name]);
            _guildPanel?.ShowApplications(await s.GuildApplications(_character!.Id));
        });
        _guildPanel.WithdrawPressed += guild => _ = GuildCall(async s =>
        {
            await s.WithdrawApplication(_character!.Id, guild);
            _guildPanel?.ShowApplications(await s.GuildApplications(_character!.Id));
        });
        _guildPanel.AnswerPressed += (applicant, accept) => _ = GuildCall(async s =>
        {
            await s.AnswerApplication(_character!.Id, applicant, accept);
            _guildPanel?.ShowApplications(await s.GuildApplications(_character!.Id));
        });
        _guildPanel.TabOpened += tab => _ = GuildCall(async s =>
        {
            _notifyOnline = _guildPanel?.NotifyOnline ?? _notifyOnline;
            switch (tab)
            {
                case GuildPanel.Tab.Chest:
                    _guildPanel?.ShowChest(await s.GuildChest(_character!.Id));
                    break;
                case GuildPanel.Tab.Log:
                    _guildPanel?.ShowLog(await s.GuildLog(_character!.Id));
                    break;
                case GuildPanel.Tab.Recruitment when _guild is not null && GuildPage.CanLead(_guild, _character!.Id):
                    _guildPanel?.ShowApplications(await s.GuildApplications(_character!.Id));
                    break;
            }
        });
        _guildPanel.Show(_guild, _character?.Id ?? Guid.Empty, DateTimeOffset.UtcNow);
        if (_lobby?.SignedIn != true || _character is null)
            _guildPanel.Tell(texts["guild.needs-server"]);
        else
        {
            _ = RefreshGuild();
            // Out of a guild, one's applications waiting.
            if (_guild is null)
                _ = GuildCall(async s => _guildPanel?.ShowApplications(await s.GuildApplications(_character!.Id)));
        }
    }

    private void CloseGuild()
    {
        _notifyOnline = _guildPanel?.NotifyOnline ?? _notifyOnline;
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

    private GuildChestPanel? _chestPanel;

    /// <summary>
    /// Edmée's other answer (sprint 156): the guild's chest, asked of the server; each movement asked of it too, the bag
    /// beside the chest kept as it answers. Out of a guild or offline, the reason in the window.
    /// </summary>
    private GuildChestPanel OpenChest()
    {
        _chestPanel?.QueueFree();
        Texts texts = _talkPanel?.Texts ?? _hud.Texts;
        _chestPanel = new GuildChestPanel();
        AddChild(_chestPanel);
        _chestPanel.Build(texts);
        _chestPanel.ClosePressed += CloseChest;
        _chestPanel.DepositPressed += (item, count, tab) => _ = ChestCall(s => s.Deposit(_character!.Id, item, count, tab));
        _chestPanel.WithdrawPressed += (item, count, tab) => _ = ChestCall(s => s.Withdraw(_character!.Id, item, count, tab));
        _chestPanel.OpenPressed += (tab, open) => _ = ChestCall(s => s.OpenChestTab(_character!.Id, tab, open));
        _ = ChestCall(s => s.GuildChest(_character!.Id));
        return _chestPanel;
    }

    private void CloseChest()
    {
        _chestPanel?.QueueFree();
        _chestPanel = null;
    }

    private async Task ChestCall(Func<GameServer, Task<ChestView>> call)
    {
        Texts texts = _talkPanel?.Texts ?? _hud.Texts;
        if (_lobby?.SignedIn != true || _character is null)
        {
            _chestPanel?.Tell(texts["guild.needs-server"]);
            return;
        }
        try
        {
            ChestView chest = await call(_lobby.Server);
            if (chest.Character is CharacterSummary after)
                KeepCharacter(after);
            _chestPanel?.Tell("");
            _chestPanel?.Show(chest, _guild?.Name ?? "", _guild?.Standing?.Level ?? 1, _character.Inventory ?? [], DateTimeOffset.UtcNow);
        }
        catch (Exception e) when (e is ServerException or HttpRequestException)
        {
            _chestPanel?.Tell(e is ServerException refused ? texts["result.refused", refused.Message] : texts["lobby.unreachable"]);
        }
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
            _guildPanel?.Tell("");
            await call(_lobby.Server);
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
