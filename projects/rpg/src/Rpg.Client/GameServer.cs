using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Net.WebSockets;
using System.Text.Json;
using Rpg.Core;

namespace Rpg.Client;

/// <summary>Why the server said no: the HTTP status, and the message to show (its title, then its field errors).</summary>
public sealed class ServerException(HttpStatusCode status, string message) : Exception(message)
{
    public HttpStatusCode Status { get; } = status;
}

/// <summary>
/// The game's side of the server's API (accounts and characters), used by the Godot client and by the
/// end-to-end tests, so both go through the same calls. Sign in once; the token goes with every call.
/// </summary>
public sealed class GameServer(HttpClient http)
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    public HttpClient Http { get; } = http ?? throw new ArgumentNullException(nameof(http));

    public bool SignedIn => Http.DefaultRequestHeaders.Authorization is not null;

    public async Task SignUp(string name, string password, CancellationToken cancel = default) =>
        await Check(await Http.PostAsJsonAsync("api/accounts", new Credentials(name, password), Json, cancel), cancel);

    public async Task SignIn(string name, string password, CancellationToken cancel = default)
    {
        HttpResponseMessage response = await Http.PostAsJsonAsync("api/tokens", new Credentials(name, password), Json, cancel);
        await Check(response, cancel);
        AccessToken token = (await response.Content.ReadFromJsonAsync<AccessToken>(Json, cancel))!;
        Http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token.Token);
        _credentials = new Credentials(name, password);
    }

    /// <summary>
    /// The name and password of this session's sign-in, kept in memory only: a token run out (the server
    /// restarted with another key, or twelve hours went by) is replaced without asking (sprint 82, D66).
    /// </summary>
    private Credentials? _credentials;

    public void SignOut()
    {
        Http.DefaultRequestHeaders.Authorization = null;
        _credentials = null;
    }

    /// <summary>Signed in by someone else: the launcher hands the game a token it got itself (no password to sign in again).</summary>
    public void UseToken(string token)
    {
        Http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        _credentials = null;
    }

    /// <summary>
    /// The proximity chat of a character (sprint 84): a WebSocket to the server, with this session's
    /// token; refused for a token run out, signed in again once. <paramref name="open"/> makes the socket
    /// (a <see cref="ClientWebSocket"/> by default; the tests' in-memory server otherwise).
    /// </summary>
    public async Task<ChatClient> OpenChat(Guid id, Func<Uri, AuthenticationHeaderValue?, CancellationToken, Task<WebSocket>>? open = null, CancellationToken cancel = default)
    {
        open ??= OpenSocket;
        var http = new Uri(Http.BaseAddress ?? throw new InvalidOperationException("No server address."), $"api/characters/{id}/chat");
        var uri = new UriBuilder(http) { Scheme = http.Scheme == Uri.UriSchemeHttps ? "wss" : "ws" }.Uri;
        try
        {
            return new ChatClient(await open(uri, Http.DefaultRequestHeaders.Authorization, cancel));
        }
        catch (WebSocketException) when (_credentials is { Name: string name, Password: string password })
        {
            await SignIn(name, password, cancel);
            return new ChatClient(await open(uri, Http.DefaultRequestHeaders.Authorization, cancel));
        }
    }

    private static async Task<WebSocket> OpenSocket(Uri uri, AuthenticationHeaderValue? auth, CancellationToken cancel)
    {
        var socket = new ClientWebSocket();
        if (auth is not null)
            socket.Options.SetRequestHeader("Authorization", auth.ToString());
        await socket.ConnectAsync(uri, cancel);
        return socket;
    }

    /// <summary>A call; refused for a token no longer valid, signed in again with this session's password and made once more.</summary>
    private async Task<HttpResponseMessage> Send(Func<Task<HttpResponseMessage>> call, CancellationToken cancel)
    {
        HttpResponseMessage response = await call();
        if (response.StatusCode != HttpStatusCode.Unauthorized || _credentials is not { Name: string name, Password: string password })
            return response;
        response.Dispose();
        await SignIn(name, password, cancel);
        return await call();
    }

    public async Task<IReadOnlyList<CharacterSummary>> Characters(CancellationToken cancel = default)
    {
        HttpResponseMessage response = await Send(() => Http.GetAsync(new Uri("api/characters", UriKind.Relative), cancel), cancel);
        await Check(response, cancel);
        return (await response.Content.ReadFromJsonAsync<CharacterSummary[]>(Json, cancel))!;
    }

    /// <summary>The game servers, to choose from before one's characters.</summary>
    public async Task<IReadOnlyList<ServerInfo>> Servers(CancellationToken cancel = default)
    {
        HttpResponseMessage response = await Send(() => Http.GetAsync(new Uri("api/servers", UriKind.Relative), cancel), cancel);
        await Check(response, cancel);
        return (await response.Content.ReadFromJsonAsync<ServerInfo[]>(Json, cancel))!;
    }

    public Task<CharacterSummary> CreateCharacter(string name, string look, string heroClass, int colour = 0, string? server = null, CancellationToken cancel = default) =>
        CreateCharacter(new Hero(name, look, heroClass, colour), server, cancel);

    /// <summary>Creates a character with the whole appearance of <paramref name="hero"/>.</summary>
    public async Task<CharacterSummary> CreateCharacter(Hero hero, string? server = null, CancellationToken cancel = default)
    {
        ArgumentNullException.ThrowIfNull(hero);
        var request = new NewCharacter(hero.Name, hero.Look, hero.Class, hero.Colour, server, hero.Hair, hero.Skin, hero.Height, hero.Build);
        HttpResponseMessage response = await Send(() => Http.PostAsJsonAsync("api/characters", request, Json, cancel), cancel);
        await Check(response, cancel);
        return (await response.Content.ReadFromJsonAsync<CharacterSummary>(Json, cancel))!;
    }

    /// <summary>The character's party (sprint 119): its leader and members, or <see cref="PartyNewsKind.Left"/>.</summary>
    public async Task<PartyNews> Party(Guid id, CancellationToken cancel = default)
    {
        HttpResponseMessage response = await Send(() => Http.GetAsync(new Uri($"api/characters/{id}/party", UriKind.Relative), cancel), cancel);
        await Check(response, cancel);
        return (await response.Content.ReadFromJsonAsync<PartyNews>(Json, cancel))!;
    }

    /// <summary>Invites a character in play into the party, by its name (sprint 119).</summary>
    public async Task Invite(Guid id, string name, CancellationToken cancel = default) =>
        await Check(await Send(() => Http.PostAsJsonAsync(new Uri($"api/characters/{id}/party/invite", UriKind.Relative), new PartyInvite(name), Json, cancel), cancel), cancel);

    /// <summary>Joins the party of a character who invited this one (sprint 119).</summary>
    public async Task JoinParty(Guid id, Guid from, CancellationToken cancel = default) =>
        await Check(await Send(() => Http.PostAsJsonAsync(new Uri($"api/characters/{id}/party/accept", UriKind.Relative), new PartyAnswer(from), Json, cancel), cancel), cancel);

    /// <summary>Leaves the party (sprint 119).</summary>
    public async Task LeaveParty(Guid id, CancellationToken cancel = default) =>
        await Check(await Send(() => Http.PostAsync(new Uri($"api/characters/{id}/party/leave", UriKind.Relative), null, cancel), cancel), cancel);

    /// <summary>The account's contacts (sprint 126): friends with their zone when in play, then those it ignores.</summary>
    public async Task<ContactList> Contacts(Guid id, CancellationToken cancel = default)
    {
        HttpResponseMessage response = await Send(() => Http.GetAsync(new Uri($"api/characters/{id}/contacts", UriKind.Relative), cancel), cancel);
        await Check(response, cancel);
        return (await response.Content.ReadFromJsonAsync<ContactList>(Json, cancel))!;
    }

    /// <summary>Adds a character, by its name, as a friend or to ignore (sprint 126); the contacts as they now are.</summary>
    public async Task<ContactList> AddContact(Guid id, string name, bool ignore = false, CancellationToken cancel = default)
    {
        HttpResponseMessage response = await Send(() => Http.PostAsJsonAsync(new Uri($"api/characters/{id}/contacts", UriKind.Relative), new NewContact(name, ignore), Json, cancel), cancel);
        await Check(response, cancel);
        return (await response.Content.ReadFromJsonAsync<ContactList>(Json, cancel))!;
    }

    /// <summary>Takes a character out of the contacts (sprint 126); the contacts as they now are.</summary>
    public async Task<ContactList> RemoveContact(Guid id, Guid other, CancellationToken cancel = default)
    {
        HttpResponseMessage response = await Send(() => Http.DeleteAsync(new Uri($"api/characters/{id}/contacts/{other}", UriKind.Relative), cancel), cancel);
        await Check(response, cancel);
        return (await response.Content.ReadFromJsonAsync<ContactList>(Json, cancel))!;
    }

    /// <summary>The party's leader sends a member out (sprint 124).</summary>
    public async Task KickFromParty(Guid id, Guid member, CancellationToken cancel = default) =>
        await Check(await Send(() => Http.PostAsJsonAsync(new Uri($"api/characters/{id}/party/kick", UriKind.Relative), new PartyKick(member), Json, cancel), cancel), cancel);

    /// <summary>Asks a character on the same map to trade (sprint 119).</summary>
    public async Task AskTrade(Guid id, Guid with, CancellationToken cancel = default) =>
        await Check(await Send(() => Http.PostAsJsonAsync(new Uri($"api/characters/{id}/trade/ask", UriKind.Relative), new PartyAnswer(with), Json, cancel), cancel), cancel);

    /// <summary>Opens the trade a character asked for (sprint 119).</summary>
    public async Task OpenTrade(Guid id, Guid with, CancellationToken cancel = default) =>
        await Check(await Send(() => Http.PostAsJsonAsync(new Uri($"api/characters/{id}/trade/accept", UriKind.Relative), new PartyAnswer(with), Json, cancel), cancel), cancel);

    /// <summary>Puts items and Éclats on the table (sprint 119); the server checks they are owned and not worn.</summary>
    public async Task Offer(Guid id, TradeOffer offer, CancellationToken cancel = default) =>
        await Check(await Send(() => Http.PutAsJsonAsync(new Uri($"api/characters/{id}/trade", UriKind.Relative), offer, Json, cancel), cancel), cancel);

    /// <summary>Accepts the offers as they stand (sprint 119): the character as it is after, the trade made if the other had accepted.</summary>
    public async Task<CharacterSummary> ReadyTrade(Guid id, CancellationToken cancel = default)
    {
        HttpResponseMessage response = await Send(() => Http.PostAsync(new Uri($"api/characters/{id}/trade/ready", UriKind.Relative), null, cancel), cancel);
        await Check(response, cancel);
        return (await response.Content.ReadFromJsonAsync<CharacterSummary>(Json, cancel))!;
    }

    /// <summary>Calls the trade off (sprint 119).</summary>
    public async Task CancelTrade(Guid id, CancellationToken cancel = default) =>
        await Check(await Send(() => Http.PostAsync(new Uri($"api/characters/{id}/trade/cancel", UriKind.Relative), null, cancel), cancel), cancel);

    /// <summary>Opens a fight with the party on a group of monsters (sprint 120); its members on the map may join for 15 seconds.</summary>
    public async Task<SharedFightNews> OpenBattle(Guid id, string scenario, CancellationToken cancel = default)
    {
        HttpResponseMessage response = await Send(() => Http.PostAsJsonAsync(new Uri($"api/characters/{id}/battles", UriKind.Relative), new NewSharedFight(scenario), Json, cancel), cancel);
        await Check(response, cancel);
        return (await response.Content.ReadFromJsonAsync<SharedFightNews>(Json, cancel))!;
    }

    /// <summary>Joins a fight a member of the party opened (sprint 120).</summary>
    public async Task JoinBattle(Guid id, Guid fight, CancellationToken cancel = default) =>
        await Check(await Send(() => Http.PostAsync(new Uri($"api/characters/{id}/battles/{fight}/join", UriKind.Relative), null, cancel), cancel), cancel);

    /// <summary>Starts the fight one opened without waiting (sprint 120).</summary>
    public async Task GoBattle(Guid id, Guid fight, CancellationToken cancel = default) =>
        await Check(await Send(() => Http.PostAsync(new Uri($"api/characters/{id}/battles/{fight}/go", UriKind.Relative), null, cancel), cancel), cancel);

    /// <summary>Plays an action of the character's hero in a fight with the party (sprint 120); the server checks it is its turn.</summary>
    public async Task Act(Guid id, Guid fight, FightAction action, CancellationToken cancel = default) =>
        await Check(await Send(() => Http.PostAsJsonAsync(new Uri($"api/characters/{id}/battles/{fight}/act", UriKind.Relative), action, Json, cancel), cancel), cancel);

    /// <summary>A fight with the party as it stands: its start and every action so far (sprint 120, coming back).</summary>
    public async Task<SharedFightNews> Battle(Guid id, Guid fight, CancellationToken cancel = default)
    {
        HttpResponseMessage response = await Send(() => Http.GetAsync(new Uri($"api/characters/{id}/battles/{fight}", UriKind.Relative), cancel), cancel);
        await Check(response, cancel);
        return (await response.Content.ReadFromJsonAsync<SharedFightNews>(Json, cancel))!;
    }

    /// <summary>Saves where a character stands in a town.</summary>
    public async Task SavePlace(Guid id, Place place, CancellationToken cancel = default) =>
        await Check(await Send(() => Http.PutAsJsonAsync(new Uri($"api/characters/{id}/place", UriKind.Relative), place, Json, cancel), cancel), cancel);

    /// <summary>Asks the server for a fight: it draws the seed, and only that fight will earn experience.</summary>
    public async Task<FightTicket> StartFight(Guid id, string scenario, CancellationToken cancel = default)
    {
        HttpResponseMessage response = await Send(() => Http.PostAsJsonAsync(new Uri($"api/characters/{id}/fights", UriKind.Relative), new NewFight(scenario), Json, cancel), cancel);
        await Check(response, cancel);
        return (await response.Content.ReadFromJsonAsync<FightTicket>(Json, cancel))!;
    }

    /// <summary>Hands the server the record of a fight it started; it replays it and says what it earned.</summary>
    public async Task<FightResult> ReportFight(Guid id, Guid fight, FightRecord record, CancellationToken cancel = default)
    {
        ArgumentNullException.ThrowIfNull(record);
        using var content = new StringContent(record.ToJson(), System.Text.Encoding.UTF8, "application/json");
        HttpResponseMessage response = await Send(() => Http.PostAsync(new Uri($"api/characters/{id}/fights/{fight}", UriKind.Relative), content, cancel), cancel);
        await Check(response, cancel);
        return (await response.Content.ReadFromJsonAsync<FightResult>(Json, cancel))!;
    }

    /// <summary>Puts items on a character; the server checks it owns them and they fit.</summary>
    public async Task<CharacterSummary> SaveEquipment(Guid id, IReadOnlyDictionary<Slot, string> worn, CancellationToken cancel = default)
    {
        HttpResponseMessage response = await Send(() => Http.PutAsJsonAsync(new Uri($"api/characters/{id}/equipment", UriKind.Relative), new Wear(worn), Json, cancel), cancel);
        await Check(response, cancel);
        return (await response.Content.ReadFromJsonAsync<CharacterSummary>(Json, cancel))!;
    }

    /// <summary>Spends a character's points; the server checks them against its level.</summary>
    public async Task<CharacterSummary> SavePoints(Guid id, Points points, CancellationToken cancel = default)
    {
        HttpResponseMessage response = await Send(() => Http.PutAsJsonAsync(new Uri($"api/characters/{id}/points", UriKind.Relative), points, Json, cancel), cancel);
        await Check(response, cancel);
        return (await response.Content.ReadFromJsonAsync<CharacterSummary>(Json, cancel))!;
    }

    /// <summary>Makes items at a trade's workshop (sprint 81); the server checks the level and the ingredients.</summary>
    /// <summary>The waystones the character discovered, and its home (sprint 127).</summary>
    public async Task<WaystoneView> Waystones(Guid id, CancellationToken cancel = default)
    {
        HttpResponseMessage response = await Send(() => Http.GetAsync(new Uri($"api/characters/{id}/waystones", UriKind.Relative), cancel), cancel);
        await Check(response, cancel);
        return (await response.Content.ReadFromJsonAsync<WaystoneView>(Json, cancel))!;
    }

    /// <summary>Travels to a waystone discovered (sprint 127); the character where it arrived, its Éclats spent.</summary>
    public async Task<CharacterSummary> TravelTo(Guid id, string waystone, CancellationToken cancel = default)
    {
        HttpResponseMessage response = await Send(() => Http.PostAsJsonAsync(new Uri($"api/characters/{id}/travel", UriKind.Relative), new Travel(waystone), Json, cancel), cancel);
        await Check(response, cancel);
        return (await response.Content.ReadFromJsonAsync<CharacterSummary>(Json, cancel))!;
    }

    /// <summary>Makes a waystone discovered the character's home (sprint 127).</summary>
    public async Task<WaystoneView> MakeHome(Guid id, string waystone, CancellationToken cancel = default)
    {
        HttpResponseMessage response = await Send(() => Http.PostAsJsonAsync(new Uri($"api/characters/{id}/home", UriKind.Relative), new Travel(waystone), Json, cancel), cancel);
        await Check(response, cancel);
        return (await response.Content.ReadFromJsonAsync<WaystoneView>(Json, cancel))!;
    }

    /// <summary>Eats food out of a fight (sprint 101); the server checks it is owned and heals; since sprint 127, reads the recall scroll.</summary>
    public async Task<CharacterSummary> Eat(Guid id, string item, CancellationToken cancel = default)
    {
        HttpResponseMessage response = await Send(() => Http.PostAsJsonAsync(new Uri($"api/characters/{id}/use", UriKind.Relative), new Use(item), Json, cancel), cancel);
        await Check(response, cancel);
        return (await response.Content.ReadFromJsonAsync<CharacterSummary>(Json, cancel))!;
    }

    /// <summary>Gathers a spot from where the character stands (sprint 99); the server checks the trade, the place and the spot.</summary>
    public async Task<Gathered> Harvest(Guid id, string node, Cell from, CancellationToken cancel = default)
    {
        HttpResponseMessage response = await Send(() => Http.PostAsJsonAsync(new Uri($"api/characters/{id}/harvest", UriKind.Relative), new Gather(node, from), Json, cancel), cancel);
        await Check(response, cancel);
        return (await response.Content.ReadFromJsonAsync<Gathered>(Json, cancel))!;
    }

    /// <summary>The spots of the character's map still growing back (sprint 99).</summary>
    public async Task<Growing> Growing(Guid id, CancellationToken cancel = default)
    {
        HttpResponseMessage response = await Send(() => Http.GetAsync(new Uri($"api/characters/{id}/harvest", UriKind.Relative), cancel), cancel);
        await Check(response, cancel);
        return (await response.Content.ReadFromJsonAsync<Growing>(Json, cancel))!;
    }

    /// <summary>Buys from Rose's stall (sprint 97); the server checks the place, the stock and the Éclats.</summary>
    public async Task<CharacterSummary> Buy(Guid id, string item, int count = 1, CancellationToken cancel = default) => await Trade(id, "buy", item, count, cancel);

    /// <summary>Sells Rose what she buys (sprint 97); the server checks the place and what is owned and not worn.</summary>
    public async Task<CharacterSummary> Sell(Guid id, string item, int count = 1, CancellationToken cancel = default) => await Trade(id, "sell", item, count, cancel);

    private async Task<CharacterSummary> Trade(Guid id, string way, string item, int count, CancellationToken cancel)
    {
        HttpResponseMessage response = await Send(() => Http.PostAsJsonAsync(new Uri($"api/characters/{id}/shop/{way}", UriKind.Relative), new Trade(item, count), Json, cancel), cancel);
        await Check(response, cancel);
        return (await response.Content.ReadFromJsonAsync<CharacterSummary>(Json, cancel))!;
    }

    public async Task<CharacterSummary> Craft(Guid id, string job, string item, int count = 1, CancellationToken cancel = default)
    {
        HttpResponseMessage response = await Send(() => Http.PostAsJsonAsync(new Uri($"api/characters/{id}/craft", UriKind.Relative), new Craft(job, item, count), Json, cancel), cancel);
        await Check(response, cancel);
        return (await response.Content.ReadFromJsonAsync<CharacterSummary>(Json, cancel))!;
    }

    /// <summary>Goes into a dungeon (sprint 80); the server checks the key and uses it up.</summary>
    public async Task<CharacterSummary> EnterDungeon(Guid id, string dungeon, CancellationToken cancel = default)
    {
        HttpResponseMessage response = await Send(() => Http.PostAsJsonAsync(new Uri($"api/characters/{id}/dungeon", UriKind.Relative), new EnterDungeon(dungeon), Json, cancel), cancel);
        await Check(response, cancel);
        return (await response.Content.ReadFromJsonAsync<CharacterSummary>(Json, cancel))!;
    }

    /// <summary>Takes a quest from its giver (sprint 88); the server checks its level and the quests before it.</summary>
    public async Task<CharacterSummary> TakeQuest(Guid id, string quest, CancellationToken cancel = default)
    {
        HttpResponseMessage response = await Send(() => Http.PostAsJsonAsync(new Uri($"api/characters/{id}/quests", UriKind.Relative), new TakeQuest(quest), Json, cancel), cancel);
        await Check(response, cancel);
        return (await response.Content.ReadFromJsonAsync<CharacterSummary>(Json, cancel))!;
    }

    /// <summary>Someone a quest under way asks to see, talked to ("town/npc").</summary>
    public async Task<CharacterSummary> QuestTalk(Guid id, string quest, string npc, CancellationToken cancel = default)
    {
        HttpResponseMessage response = await Send(() => Http.PostAsJsonAsync(new Uri($"api/characters/{id}/quests/{Uri.EscapeDataString(quest)}/talk", UriKind.Relative), new QuestTalk(npc), Json, cancel), cancel);
        await Check(response, cancel);
        return (await response.Content.ReadFromJsonAsync<CharacterSummary>(Json, cancel))!;
    }

    /// <summary>A thing on a map a quest under way asks to find, examined (sprint 105).</summary>
    public async Task<CharacterSummary> QuestInteract(Guid id, string quest, string prop, CancellationToken cancel = default)
    {
        HttpResponseMessage response = await Send(() => Http.PostAsJsonAsync(new Uri($"api/characters/{id}/quests/{Uri.EscapeDataString(quest)}/interact", UriKind.Relative), new QuestInteract(prop), Json, cancel), cancel);
        await Check(response, cancel);
        return (await response.Content.ReadFromJsonAsync<CharacterSummary>(Json, cancel))!;
    }

    /// <summary>Hands a quest back to its giver, its objectives reached; the server takes the items it asks and gives its rewards.</summary>
    public async Task<QuestReward> HandInQuest(Guid id, string quest, CancellationToken cancel = default)
    {
        HttpResponseMessage response = await Send(() => Http.PostAsync(new Uri($"api/characters/{id}/quests/{Uri.EscapeDataString(quest)}/done", UriKind.Relative), null, cancel), cancel);
        await Check(response, cancel);
        return (await response.Content.ReadFromJsonAsync<QuestReward>(Json, cancel))!;
    }

    /// <summary>At a hall's fountain (sprint 86): all the hit points back for the next room.</summary>
    public async Task<CharacterSummary> Rest(Guid id, CancellationToken cancel = default)
    {
        HttpResponseMessage response = await Send(() => Http.PostAsync(new Uri($"api/characters/{id}/dungeon/rest", UriKind.Relative), null, cancel), cancel);
        await Check(response, cancel);
        return (await response.Content.ReadFromJsonAsync<CharacterSummary>(Json, cancel))!;
    }

    public async Task LeaveDungeon(Guid id, CancellationToken cancel = default) =>
        await Check(await Send(() => Http.DeleteAsync(new Uri($"api/characters/{id}/dungeon", UriKind.Relative), cancel), cancel), cancel);

    public async Task DeleteCharacter(Guid id, CancellationToken cancel = default) =>
        await Check(await Send(() => Http.DeleteAsync(new Uri($"api/characters/{id}", UriKind.Relative), cancel), cancel), cancel);

    private static async Task Check(HttpResponseMessage response, CancellationToken cancel)
    {
        if (response.IsSuccessStatusCode)
            return;
        string message = response.StatusCode.ToString();
        try
        {
            using JsonDocument problem = JsonDocument.Parse(await response.Content.ReadAsStringAsync(cancel));
            if (problem.RootElement.TryGetProperty("title", out JsonElement title))
                message = title.GetString() ?? message;
            if (problem.RootElement.TryGetProperty("errors", out JsonElement errors))
                message += " " + string.Join(" ", errors.EnumerateObject().SelectMany(e => e.Value.EnumerateArray().Select(v => $"{e.Name}: {v.GetString()}")));
        }
        catch (JsonException)
        {
            // Not a problem document (a proxy, a crash): the status says enough.
        }
        throw new ServerException(response.StatusCode, message);
    }
}
