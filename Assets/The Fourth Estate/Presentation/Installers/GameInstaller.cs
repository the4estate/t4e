using T4E.App.Abstractions;                 // IWorldCommands, IRandom, IClock, IAppLogger
using T4E.App.UseCases;
using T4E.Domain;                           // GameDate, Weekday, DaySegment, WorldSnapshot if you log it
using T4E.Domain.Core.CET;                  // TriggerType, TriggerContext
using T4E.Infrastructure;                   // DeterministicRandom, SystemClock, UnityLogger, InMemoryWorld
using UnityEngine;
using UnityEngine.InputSystem;

public sealed class GameInstaller : MonoBehaviour
{
    public int Seed = 12345;

    private IRandom _rng;
    private IClock _clock;
    private IAppLogger _log;
    private InMemoryWorld _world;
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
    private TriggerDispatcher _cetDispatcher; // CET dispatcher (different from your existing TimelineDispatcher)

    private InputAction _advanceAction;
    void Awake()
    {
        Application.targetFrameRate = 60;

        // Core
        _rng = new DeterministicRandom(); _rng.Reseed(Seed);
        _clock = new SystemClock();
        _log = new UnityLogger();
        _world = new InMemoryWorld();

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
        var sundayEve = new GameDate(1850, 1, Weekday.Sunday, DaySegment.Evening);
        _scheduler.Enqueue(new TimelineItem("vic.event.welcome_001", sundayEve, "event",
            spawnNewsIds: new[] { "vic.news.bread_riots_001" }));

        // ===== CET v1 wiring =====
        _bus     = new TriggerBus();
        _fired   = new FiredLedger();
        _content = new StubContentRepository();     // returns no rules yet; replace with real repo once ready
        _eval    = new CetEngine(_content, _fired); // pure evaluator
        _apply   = new EffectApplier(_world, _log); // thin adapter: forwards Effect to IWorldCommands.Apply (your InMemoryWorld implements IWorldCommands)

        // Subscribe the CET dispatcher to the bus
        _cetDispatcher = new TriggerDispatcher(
            bus: _bus,
            eval: _eval,
            apply: _apply,
            world: _world,           // needs IWorldQuery.Snapshot(); your InMemoryWorld can expose that
            cmds: _world,            // IWorldCommands lives on InMemoryWorld in your setup
            fired: _fired
        );

        // Bridge the time signal → CET trigger bus
        GameSignals.SegmentAdvanced += OnSegmentAdvancedCET;

        // Simple log to see ticks
        GameSignals.SegmentAdvanced += d => Debug.Log($"Advanced to {d}");
    }

    void Update()
    {
        if (_advanceAction.WasPressedThisFrame())
            _time.AdvanceSegment();
    }

    // ==== CET trigger publisher ====
    private void OnSegmentAdvancedCET(GameDate date)
    {
        // Build a stable trigger-instance id for idempotency
        // Example: "y1850-w1-Monday-Morning#seg"
        string trigId = $"y{date.Year}-w{date.Week}-{date.Day}-{date.Segment}#seg";

        var ctx = new TriggerContext(
            type: TriggerType.OnSegmentStart,
            date: date,
            segment: date.Segment,
            dayOfWeek: (T4E.Domain.Weekday)date.Day,   // adjust cast if your TriggerContext expects your Weekday/WeekDay type
            contextMap: null,                              // no extra context for time triggers
            triggerInstanceId: trigId
        );

        _bus.Publish(ctx);
    }
}
