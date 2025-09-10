#if UNITY_EDITOR
using System.IO;
using T4E.App.UseCases;                 // TimeService, TimelineScheduler, TimelineDispatcher, CadenceRules
using T4E.Domain;                      // GameDate, Weekday, DaySegment, TimelineItem
using T4E.Infrastructure;              // InMemoryWorld, UnityLogger, DeterministicRandom
using T4E.Infrastructure.Content;      // ContentLoader  <-- add this
using UnityEditor;
using UnityEngine;

public static class PreviewRunner
{
    private const int Weeks = 20;
    private const int Seed = 12345;
    private const string CsvOut = "Scripts/ci/Artifacts/preview.csv";
    private const string EventsJson = "Assets/The Fourth Estate/Infrastructure/Content/events.json";

    [MenuItem("T4E/Preview/Run (20 weeks, seed 12345)")]
    public static void RunMenu() => Run();

    // Unity CLI: -executeMethod "PreviewRunner.Run"
    public static void Run()
    {
        Directory.CreateDirectory(Path.GetDirectoryName(CsvOut) ?? ".");

        var log = new UnityLogger();
        var world = new InMemoryWorld();
        var time = new TimeService(log, new GameDate(1850, 1, Weekday.Monday, DaySegment.Morning));

        var scheduler = new TimelineScheduler();           // listens to time signals
        var dispatcher = new TimelineDispatcher(world, log);
        var cadence = new CadenceRules(world);
        var rng = new DeterministicRandom(); rng.Reseed(Seed);

        // Load authored events from JSON and enqueue them
        ContentLoader.EnqueueEventsFromJson(EventsJson, scheduler);


        // Run deterministic preview
        for (int i = 0; i < Weeks; i++) time.AdvanceWeek();

        // Minimal CSV
        File.WriteAllText(CsvOut,
            $"weeks,news,leads,flags{System.Environment.NewLine}" +
            $"{Weeks},{world.News.Count},{world.Leads.Count},{world.Flags.Count}{System.Environment.NewLine}");

        Debug.Log($"[PreviewRunner] Done. Weeks={Weeks}, Seed={Seed}, News={world.News.Count}, Leads={world.Leads.Count}. CSV: {CsvOut}");
    }
}
#endif
