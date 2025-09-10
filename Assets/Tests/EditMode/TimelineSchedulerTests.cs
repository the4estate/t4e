using NUnit.Framework;
using T4E.App.UseCases;
using T4E.Domain;
using T4E.Infrastructure;


public class TimelineSchedulerTests
{
    [Test]
    public void ItemsInSameSlotFireDeterministically()
    {
        var log = new UnityLogger();
        var world = new InMemoryWorld();

        var time = new TimeService(log, new GameDate(1850, 1, Weekday.Monday, DaySegment.Morning));
        var sched = new TimelineScheduler();
        var disp = new TimelineDispatcher(world, log);

        var slot = new GameDate(1850, 1, Weekday.Tuesday, DaySegment.Evening);
        sched.Enqueue(new TimelineItem("a", slot, "event", spawnNewsIds: new[] { "A" }));
        sched.Enqueue(new TimelineItem("b", slot, "event", spawnNewsIds: new[] { "B" }));

        // Walk to Tuesday Evening
        while (!(time.Current.Day==Weekday.Tuesday && time.Current.Segment==DaySegment.Evening))
            time.AdvanceSegment();

        // Fire the slot
        time.AdvanceSegment(); // move past Evening -> triggers scheduler for that slot before segment advanced in this design? (We used listener on post-advance; so ensure we advanced INTO the slot first)

    }
}
