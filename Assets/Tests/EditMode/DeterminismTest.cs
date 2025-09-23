using NUnit.Framework;
using T4E.App.UseCases;
using T4E.Domain;
using T4E.Infrastructure;

public class DeterminismTests
{
    [Test]
    public void SameSeedSameWeekProducesSameCounts()
    {
        (int news, int leads) Run()
        {
            var world = new InMemoryWorld();
            var time = new TimeService(new AppLogger(), new GameDate(1850, 1, Weekday.Monday, DaySegment.Morning));
            var timelineScheduler = new TimelineScheduler();
            var rng = new DeterministicRandom(); rng.Reseed(12345);

            timelineScheduler.Enqueue(new TimelineItem("e1", new GameDate(1850, 1, Weekday.Sunday, DaySegment.Evening), "event",
                spawnNewsIds: new[] { "n1" }));
            time.AdvanceWeek();
            return (world.News.Count, world.Leads.Count);
        }

        var a = Run();
        var b = Run();
        Assert.AreEqual(a.news, b.news);
        Assert.AreEqual(a.leads, b.leads);
    }
}
