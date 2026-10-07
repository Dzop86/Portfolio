using Rogue.Client;
using Rogue.Core;

namespace Rogue.Cli.Tests;

public class TextsTests
{
    private static readonly Texts French = new(Language.French);
    private static readonly Texts English = new(Language.English);

    /// <summary>One instance of every kind of event the library can raise, found by reflection.</summary>
    public static TheoryData<GameEvent> AllEvents()
    {
        var data = new TheoryData<GameEvent>();
        foreach (Type type in typeof(GameEvent).Assembly.GetTypes().Where(t => t.IsSubclassOf(typeof(GameEvent))))
        {
            var constructor = type.GetConstructors().Single(c => c.GetParameters().All(p => p.ParameterType != type));
            object?[] arguments = constructor.GetParameters().Select(p => Activator.CreateInstance(p.ParameterType)).ToArray();
            data.Add((GameEvent)constructor.Invoke(arguments));
        }
        return data;
    }

    [Theory]
    [MemberData(nameof(AllEvents))]
    public void EveryEvent_IsWordedInBothLanguages(GameEvent e)
    {
        string fr = French.Describe(e), en = English.Describe(e);
        Assert.False(string.IsNullOrWhiteSpace(fr));
        Assert.False(string.IsNullOrWhiteSpace(en));
        Assert.NotEqual(fr, en);
    }

    [Fact]
    public void EveryEventType_IsCovered() => Assert.Equal(12, AllEvents().Count);

    [Fact]
    public void EveryReplayError_IsWordedInBothLanguages()
    {
        foreach (ReplayError error in Enum.GetValues<ReplayError>().Where(e => e != ReplayError.None))
            Assert.NotEqual(French.Error(error), English.Error(error));
    }

    [Fact]
    public void MonsterNames_StartSentencesWithACapital()
    {
        Assert.Equal("L'orque vous manque.", French.Describe(new MonsterMissed(MonsterKind.Orc)));
        Assert.Equal("The troll kills you.", English.Describe(new PlayerDied(MonsterKind.Troll)));
        Assert.Equal("Vous frappez le gobelin (3).", French.Describe(new PlayerAttacked(MonsterKind.Goblin, 3)));
    }

    [Theory]
    [InlineData('z', GameAction.North)]
    [InlineData('w', GameAction.North)]
    [InlineData('k', GameAction.North)]
    [InlineData('q', GameAction.West)]
    [InlineData('a', GameAction.West)]
    [InlineData('h', GameAction.West)]
    [InlineData('s', GameAction.South)]
    [InlineData('j', GameAction.South)]
    [InlineData('d', GameAction.East)]
    [InlineData('l', GameAction.East)]
    [InlineData('.', GameAction.Wait)]
    [InlineData(' ', GameAction.Wait)]
    [InlineData('>', GameAction.Descend)]
    [InlineData('p', GameAction.Drink)]
    public void Keys_MapToActions(char key, GameAction expected) => Assert.Equal(new Command(expected), Keys.FromChar(key));

    [Fact]
    public void ArrowsMove_AndEscapeQuits()
    {
        Assert.Equal(new Command(GameAction.West), KeyMap.FromKey(new ConsoleKeyInfo('\0', ConsoleKey.LeftArrow, false, false, false)));
        Assert.True(KeyMap.FromKey(new ConsoleKeyInfo('\u001b', ConsoleKey.Escape, false, false, false)).Quit);
        Assert.True(Keys.FromChar('x').Quit);
        Assert.Equal(Command.Ignored, Keys.FromChar('?'));
    }
}
