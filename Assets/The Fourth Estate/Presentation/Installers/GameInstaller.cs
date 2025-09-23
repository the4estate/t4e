using System.ComponentModel;
using T4E.App.Abstractions;
using T4E.App.Abstractions.Dtos;
using T4E.App.Abstractions.Ports;
using T4E.App.UseCases;
using T4E.App.UseCases.News;
using T4E.Domain;                           
using T4E.Domain.Core.CET;                 
using T4E.Infrastructure;
using T4E.Infrastructure.Content;
using T4E.Infrastructure.Systems;
using UnityEngine;
using UnityEngine.InputSystem;

public sealed class GameInstaller : MonoBehaviour
{
    public int Seed = 12345;
    private InputAction _publishAction;

    private IRandom _rng;
    private IClock _clock;
    private IAppLogger _log;
    private InMemoryWorld _world;
    private IMemoryLog _memoryLog;
    private TimeService _time;

    // your existing systems
    private TimelineScheduler _scheduler;
    private TimelineDispatcher _dispatcher;
    private CadenceRules _cadence;

    // CET v1 pieces
    private ITriggerBus _bus;
    private IFiredLedger _fired;
    private IContentRepository _content;
    private IEvaluator _eval;
    private IEffectApplier _apply;
    private TriggerDispatcher _cetDispatcher;
    private PublishNews _publishNews;

    private InputAction _advanceAction;
    void Awake()
    {
        Application.targetFrameRate = 60;
        var newsPath = "Assets/The Fourth Estate/Infrastructure/Content/news.json";
        // Core
        _rng = new DeterministicRandom(); _rng.Reseed(Seed);
        _clock = new SystemClock();
        _log = new AppLogger();
        _world = new InMemoryWorld();
        _memoryLog = new MemoryLog();

        // Start date (adjust Weekday/WeekDay enum to your actual type name)
        _time = new TimeService(_log, new GameDate(1850, 1, Weekday.Monday, DaySegment.Morning));
        //Testing Input
        _advanceAction = new InputAction("Advance", binding: "<Keyboard>/space");
        _advanceAction.Enable();

        // ===== Existing listeners/systems you already had =====
        _scheduler  = new TimelineScheduler();
        _dispatcher = new TimelineDispatcher(_world, _log);
        _cadence    = new CadenceRules(_world);

        // Demo content: schedule one item on first Sunday Evening
       // var sundayEve = new GameDate(1850, 1, Weekday.Sunday, DaySegment.Evening);
       // _scheduler.Enqueue(new TimelineItem("vic.event.welcome_001", sundayEve, "event",
         //   spawnNewsIds: new[] { "vic.news.bread_riots_001" }));

        // ===== CET v1 wiring =====
        _bus     = new TriggerBus();
        _fired   = new FiredLedger();
        _content = new JsonNewsRepository(newsPath);    // returns no rules yet; replace with real repo once ready
        _eval    = new CetEngine(_content, _fired); // pure evaluator
        _apply   = new EffectApplier(_world, _log); // thin adapter: forwards Effect to IWorldCommands.Apply (your InMemoryWorld implements IWorldCommands)
        _publishNews = new PublishNews(_content, _apply, _memoryLog, _log);

        // Subscribe the CET dispatcher to the bus
        _cetDispatcher = new TriggerDispatcher(
            bus: _bus,
            eval: _eval,
            apply: _apply,
            world: _world,           // needs IWorldQuery.Snapshot(); InMemoryWorld can expose that
            cmds: _world,            // IWorldCommands lives on InMemoryWorld 
            fired: _fired
        );

        // Bridge the time signal → CET trigger bus
        GameSignals.SegmentAdvanced += OnSegmentAdvancedCET;

        // Simple log to see ticks
        GameSignals.SegmentAdvanced += d => Debug.Log($"Advanced to {d}");
        _publishAction = new InputAction("PublishDemo", binding: "<Keyboard>/p");
        _publishAction.Enable();
    }

    void Update()
    {
        if (_advanceAction.WasPressedThisFrame())
            _time.AdvanceSegment();

        if (_publishAction.WasPressedThisFrame())
        {
            var result = _publishNews.Execute("vic.news.demo_001", Tone.Critical);
            Debug.Log($"[NEWS] {result.Headline} — {result.Short}\n{result.Body}");
        }
    }

    // ==== CET trigger publisher ====
    private void OnSegmentAdvancedCET(GameDate date)
    {
        // Build a stable trigger-instance id idempotency
        string trigId = $"y{date.Year}-w{date.Week}-{date.Day}-{date.Segment}#seg";

        var ctx = new TriggerContext(
            type: TriggerType.OnSegmentStart,
            date: date,
            segment: date.Segment,
            dayOfWeek: (T4E.Domain.Weekday)date.Day,   
            contextMap: null,                              // no extra context for time triggers
            triggerInstanceId: trigId
        );

        _bus.Publish(ctx);
    }

}
