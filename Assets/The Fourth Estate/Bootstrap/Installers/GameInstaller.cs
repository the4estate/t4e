using System.IO;
using T4E.App.Abstractions;
using T4E.App.Abstractions.Dtos;
using T4E.App.Abstractions.Ports;
using T4E.App.UseCases;          // TimeService, CetEngine, EffectApplier, TriggerDispatcher, CadenceRules, TimelineDispatcher, TimelineScheduler, EffectInvocation
using T4E.App.UseCases.News;     // PublishNews // CoreContainer, InfrastructureInstaller
using T4E.Domain;                // GameDate, Weekday, DaySegment        // Effect, EffectType, TriggerContext, TriggerType
using T4E.Domain.Core.CET;
using T4E.Infrastructure;
using UnityEngine;
using UnityEngine.InputSystem;

namespace T4E.Bootstrap.Installers
{
    public sealed class GameInstaller : MonoBehaviour
    {
        [Header("Bootstrap")]
        public int Seed = 12345;

        private CoreContainer _c;

        private InputAction _advanceAction;
        private InputAction _publishAction;

        // Systems
        private ITimelineScheduler _scheduler;
        private TimelineDispatcher _dispatcher;
        private CadenceRules _cadence;

        private ITriggerBus _bus;
        private IFiredLedger _fired;
        private IEvaluator _eval;
        private IEffectApplier _apply;
        private TriggerDispatcher _cetDispatcher;
        private TimeService _time;
        private PublishNews _publishNews;

        void Awake()
        {
            Application.targetFrameRate = 60;

            // ---- Paths (Editor vs Player) ----
#if UNITY_EDITOR
            var newsPath = "Assets/The Fourth Estate/Infrastructure/Content/news.json";
            var sourcesPath = "Assets/The Fourth Estate/Infrastructure/Content/sources.json";
            var leadsPath = "Assets/The Fourth Estate/Infrastructure/Content/leads.json";
#else
            var newsPath    = Path.Combine(Application.streamingAssetsPath, "news.json");
            var sourcesPath = Path.Combine(Application.streamingAssetsPath, "sources.json");
            var leadsPath   = Path.Combine(Application.streamingAssetsPath, "leads.json");
#endif
            // ----------------------------------

            // Build core container (adapters + world + logger + repos)
            _c = InfrastructureInstaller.MakeContainer(newsPath, sourcesPath, leadsPath, Seed);

            // Timeline & cadence (UseCases)
            _scheduler  = new TimelineScheduler();
            _dispatcher = new TimelineDispatcher(_c.WorldC, _c.Log); // expects IWorldCommands
            _cadence    = new CadenceRules(_c.WorldC);               // expects IWorldCommands

            // CET core (UseCases)
            _bus   = new TriggerBus();
            _fired = new FiredLedger();
            _eval  = new CetEngine(_c.NewsRepo, _fired);
            _apply = _c.Effects;

            _cetDispatcher = new TriggerDispatcher(
                bus: _bus,
                eval: _eval,
                apply: _apply,
                world: _c.WorldQ,
                cmds: _c.WorldC,
                fired: _fired
            );

            // Time service (UseCases)
            _time = new TimeService(_c.Log, new GameDate(1850, 1, Weekday.Monday, DaySegment.Morning));

            // Publish News (UseCases.News)
            _publishNews = _c.MakePublishNews();

            // Signals → CET
            GameSignals.SegmentAdvanced += OnSegmentAdvancedCET;

            // Input (use named 'binding:' arg — second param is an enum otherwise)
            _advanceAction = new InputAction("Advance", binding: "<Keyboard>/space"); _advanceAction.Enable();
            _publishAction = new InputAction("PublishDemo", binding: "<Keyboard>/p"); _publishAction.Enable();
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

            // Debug key: unlock a source then publish
            if (Keyboard.current.uKey.wasPressedThisFrame)
            {
                var e = new Effect(EffectType.UnlockSource, "vic.source.clara_letter_001");
                _apply.Apply(new[] { new EffectInvocation(null, e) });

                var result = _publishNews.Execute("vic.news.demo_001", Tone.Critical);
                Debug.Log("[WORLD] Debug unlock executed.");
            }
        }

        private void OnSegmentAdvancedCET(GameDate date)
        {
            string trigId = $"y{date.Year}-w{date.Week}-{date.Day}-{date.Segment}#seg";
            var ctx = new TriggerContext(
                type: TriggerType.OnSegmentStart,
                date: date,
                segment: date.Segment,
                dayOfWeek: date.Day,
                contextMap: null,
                triggerInstanceId: trigId
            );
            _bus.Publish(ctx);
        }
    }
}
