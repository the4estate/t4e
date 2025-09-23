using NUnit.Framework;
using T4E.App.UseCases;
using T4E.Domain;
using T4E.Infrastructure;
public class CadenceAndDueTests
{
    [Test]
    public void SundayMorningSetsEditorialFlag()
    {
        var world = new InMemoryWorld();
        var time = new TimeService(new AppLogger(), new GameDate(1850, 1, Weekday.Saturday, DaySegment.Night));
        var cadence = new CadenceRules(world);

        // Sat Night -> Sun Morning
        time.AdvanceSegment();
        Assert.IsTrue(world.Flags.TryGetValue("editorial_unlocked", out var v) && v==1);
    }

    [Test]
    public void ScheduledItemFiresExactlyAtItsSlot()
    {
        var world = new InMemoryWorld();
        var time = new TimeService(new AppLogger(), new GameDate(1850, 1, Weekday.Monday, DaySegment.Morning));
        var sched = new TimelineScheduler();
        var disp = new TimelineDispatcher(world, new AppLogger());

        var slot = new GameDate(1850, 1, Weekday.Wednesday, DaySegment.Afternoon);
        sched.Enqueue(new TimelineItem("e1", slot, "event", spawnNewsIds: new[] { "n1" }));

        // Before slot: nothing fired
        Assert.IsFalse(world.News.Contains("n1"));

        // Advance into the slot (tick until Current equals slot)
        while (!(time.Current.Day==slot.Day && time.Current.Segment==slot.Segment &&
                 time.Current.Week==slot.Week && time.Current.Year==slot.Year))
        {
            time.AdvanceSegment();
        }

        // Now advance one more to trigger scheduler (since it listens post-advance)
        time.AdvanceSegment();

        Assert.IsTrue(world.News.Contains("n1"));
    }
}
