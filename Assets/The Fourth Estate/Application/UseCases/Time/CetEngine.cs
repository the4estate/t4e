using System;
using System.Collections.Generic;
using System.Linq;
using T4E.App.Abstractions;
using T4E.Domain;
using T4E.Domain.Core.CET;

namespace T4E.App.UseCases
{
    /// <summary>
    /// Evaluates rules for a trigger and returns staged effects. No state mutation here.
    /// </summary>
    public sealed class CetEngine : IEvaluator
    {
        private readonly IContentRepository _content;     // already in Abstractions
        private readonly IFiredLedger _fired;             // we’ll add this port below

        // Pre-indexed lookup: TriggerType -> rules[] 
        private readonly Dictionary<TriggerType, Rule[]> _byTrigger;

        public CetEngine(IContentRepository content, IFiredLedger fired)
        {
            _content = content;
            _fired = fired;
            _byTrigger = BuildIndex(_content);
        }

        public IReadOnlyList<EffectInvocation> Evaluate(TriggerContext trigger, in WorldSnapshot snapshot)
        {
            if (!_byTrigger.TryGetValue(trigger.Type, out var rules) || rules.Length == 0)
                return Array.Empty<EffectInvocation>();

            // Filter + condition check
            var staged = new List<EffectInvocation>(8);

            for (int i = 0; i < rules.Length; i++)
            {
                var rule = rules[i];
                // Idempotency: has this rule fired for THIS trigger instance?
                if (_fired.HasFired(rule.EventId, rule.RuleIndex, trigger.TriggerInstanceId))
                    continue;

                if (AllTrue(rule.Conditions, trigger, snapshot))
                {
                    // Stage effects in author order (determinism within rule)
                    foreach (var eff in rule.Effects)
                        staged.Add(new EffectInvocation(rule, eff));
                }
            }

            // Deterministic order across rules:
            // 1) rule.Priority desc, 2) EventId asc, 3) author order already preserved
            staged.Sort((x, y) =>
            {
                int p = y.SourceRule.Priority.CompareTo(x.SourceRule.Priority);
                if (p != 0) return p;
                return string.CompareOrdinal(x.SourceRule.EventId, y.SourceRule.EventId);
            });

            return staged;
        }

        private static bool AllTrue(Condition[] conditions, TriggerContext t, in WorldSnapshot s)
        {
            // Tight loop; avoid LINQ/allocs for hot path
            for (int i = 0; i < conditions.Length; i++)
                if (!Eval(conditions[i], t, s)) return false;
            return true;
        }

        // Minimal subset shown; extend with your remaining operators.
        private static bool Eval(in Condition c, TriggerContext t, in WorldSnapshot s)
        {
            switch (c.Kind)
            {
                case ConditionKind.WeekAtLeast: return s.GameDate.Week >= c.I1;
                case ConditionKind.WeekInRange: return s.GameDate.Week >= c.I1 && s.GameDate.Week <= c.I2;
                case ConditionKind.SegmentIs: return t.Segment.ToString().Equals(c.A, StringComparison.OrdinalIgnoreCase);
                case ConditionKind.DayOfWeekIs: return t.DayOfWeek.ToString().Equals(c.A, StringComparison.OrdinalIgnoreCase);
                case ConditionKind.FlagExists: return s.Flags.ContainsKey(c.A);
                case ConditionKind.FlagIs: return s.Flags.TryGetValue(c.A, out var val) && string.Equals(val, c.B, StringComparison.Ordinal);
                case ConditionKind.ContextEquals:
                    return t.ContextMap.TryGetValue(c.A, out var v) && string.Equals(v, c.B, StringComparison.Ordinal);
                case ConditionKind.ContextIn:
                    if (!t.ContextMap.TryGetValue(c.A, out var cur)) return false;
                    // B is comma-separated list "a,b,c"
                    var span = c.B.Split(',');
                    for (int i = 0; i < span.Length; i++)
                        if (string.Equals(cur, span[i].Trim(), StringComparison.Ordinal)) return true;
                    return false;
                default:
                    // TODO: implement the rest incrementally
                    return false;
            }
        }

        private static Dictionary<TriggerType, Rule[]> BuildIndex(IContentRepository content)
        {
            // Adapter parses JSON -> Rule[] (IL2CPP-safe).
            // Here we just ask the repo for already-parsed rules by trigger.
            var map = new Dictionary<TriggerType, Rule[]>();
            foreach (TriggerType t in Enum.GetValues(typeof(TriggerType)))
            {
                var arr = content.GetRulesByTrigger(t); // add this to IContentRepository
                if (arr != null && arr.Length > 0) map[t] = arr;
            }
            return map;
        }
    }
}
