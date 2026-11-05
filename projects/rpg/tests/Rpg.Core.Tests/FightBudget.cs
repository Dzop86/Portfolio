using System.Diagnostics.Metrics;

[assembly: AssemblyFixture(typeof(Rpg.Core.Tests.FightBudget))]

namespace Rpg.Core.Tests;

/// <summary>
/// The fights the AI plays out during a whole run of this suite (D96, `docs/classes-conception.md` §7.2). Most balance tests loop on the
/// classes: from 3 to 10 classes they would multiply, and the suite takes half an hour already. The verdict falls at the end of the run,
/// so a run that goes over the budget fails even when every test passes.
/// </summary>
public sealed class FightBudget : IDisposable
{
    /// <summary>
    /// What a whole run may play out: the 244 635 fights measured at 3 classes (sprint 193, T153), less those the path fence gave up. It does not
    /// grow with the classes: a test that loops on them takes a sample instead. The 20 000 of the design counted only some of the tests, and is a goal, not yet held.
    /// </summary>
    public const long Limit = 240_000;

    private static long _played;
    private readonly MeterListener _listener = new();

    public FightBudget()
    {
        _listener.InstrumentPublished = (instrument, listener) =>
        {
            if (instrument.Meter.Name == Ai.MeterName)
                listener.EnableMeasurementEvents(instrument);
        };
        _listener.SetMeasurementEventCallback<long>((_, value, _, _) => Interlocked.Add(ref _played, value));
        _listener.Start();
    }

    /// <summary>The fights played out so far in this run.</summary>
    public static long Played => Interlocked.Read(ref _played);

    public void Dispose()
    {
        _listener.Dispose();
        string? report = Environment.GetEnvironmentVariable("RPG_FIGHT_BUDGET_REPORT");
        if (report is not null)
            File.WriteAllText(report, Played.ToString(System.Globalization.CultureInfo.InvariantCulture));
        if (Played > Limit)
            throw new InvalidOperationException($"This run played out {Played} fights, over the budget of {Limit} (D96).");
    }
}
