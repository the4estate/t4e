// Assets/The Fourth Estate/Tests/CET/CetSmokeTests.cs
using NUnit.Framework;
using T4E.App.Abstractions;
using T4E.App.UseCases;
using T4E.Domain;
using T4E.Domain.Core.CET;
using T4E.Infrastructure;


public sealed class NullLogger : IAppLogger
{
    public void Info(string m) { }
    public void Warn(string m) { }
    public void Error(string m) { }
}

[TestFixture]
public class CetSmokeTests
{
    [Test]
    public void MorningStubRuleAddsNews()
    {
        // world + infra
        var world = new InMemoryWorld();                   // IWorldQuery + IWorldCommands
        var fired = new FiredLedger();                     // idempotency
        var repo = new StubContentRepository();           // returns the demo rule(s)
        var eval = new CetEngine(repo, fired);            // pure evaluation
        var apply = new EffectApplier(world, new NullLogger()); // effects -> world.Apply
        var bus = new TriggerBus();
        var disp = new TriggerDispatcher(bus, eval, apply, world, world, fired);

        // Simulate a Monday Morning segment trigger
        var date = new GameDate(1850, 1, Weekday.Monday, DaySegment.Morning);
        var ctx = new TriggerContext(
            TriggerType.OnSegmentStart, date, date.Segment, date.Day, null,
            triggerInstanceId: "test-y1850-w1-mon-morn"
        );

        bus.Publish(ctx);

        Assert.IsTrue(world.News.Contains("demo.news.hello"),
            "Expected stub rule to AddNews demo.news.hello on Monday Morning.");
    }
}
