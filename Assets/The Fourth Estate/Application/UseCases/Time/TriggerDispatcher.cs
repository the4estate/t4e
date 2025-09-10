// Application/UseCases/Time/TriggerDispatcher.cs
using System.Collections.Generic;
using T4E.Domain;
using T4E.App.Abstractions;
using T4E.Domain.Core.CET;


namespace T4E.App.UseCases
{
    /// <summary>
    /// Subscribes to bus, runs CET (evaluate->apply), logs + marks fired.
    /// </summary>
    public sealed class TriggerDispatcher
    {
        private readonly ITriggerBus _bus;
        private readonly IEvaluator _eval;
        private readonly IEffectApplier _apply;
        private readonly IWorldQuery _world;
        private readonly IWorldCommands _cmds;
        private readonly IFiredLedger _fired;

        public TriggerDispatcher(ITriggerBus bus, IEvaluator eval, IEffectApplier apply,
                                 IWorldQuery world, IWorldCommands cmds, IFiredLedger fired)
        {
            _bus = bus; _eval = eval; _apply = apply; _world = world; _cmds = cmds; _fired = fired;
            _bus.OnTriggered += OnTriggered;
        }

        private void OnTriggered(TriggerContext ctx)
        {
            var snapshot = _world.Snapshot(ctx.Date);                 // read-only snapshot for evaluation
            var staged = _eval.Evaluate(ctx, snapshot);       // pure
            var count = _apply.Apply(staged);                 // calls IWorldCommands.Apply(effect)

            // mark idempotency only after apply succeeds
            for (int i = 0; i < staged.Count; i++)
            {
                var r = staged[i].SourceRule;
                _fired.MarkFired(r.EventId, r.RuleIndex, ctx.TriggerInstanceId);
            }
        }
    }
}
