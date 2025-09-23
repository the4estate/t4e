using System;
using T4E.App.Abstractions.Ports;
using T4E.Domain.Core.CET;

namespace T4E.Infrastructure
{
    public sealed class StubContentRepository : IContentRepository
    {
        public T Load<T>(string id) where T : class => null;
        public System.Collections.Generic.IEnumerable<T> LoadAll<T>() where T : class
            => Array.Empty<T>();

        public Rule[] GetRulesByTrigger(TriggerType trigger)
        {
            if (trigger != TriggerType.OnSegmentStart) return Array.Empty<Rule>();

            // Rule: If Segment == Morning, then AddNews("demo.news.hello")
            var conds = new[] { new Condition(ConditionKind.SegmentIs, a: "Morning") };
            var effs = new[] { new Effect(EffectType.AddNews, a: "demo.news.hello") };

            var r = new Rule(
                eventId: "demo.event.hello",
                ruleIndex: 0,
                trigger: TriggerType.OnSegmentStart,
                priority: 1,
                conditions: conds,
                effects: effs,
                exclusiveFlag: null,
                slotKind: null,
                background: false
            );
            return new[] { r };
        }
    }
}
