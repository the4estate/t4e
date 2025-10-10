namespace T4E.Domain.Core.CET
{
    public enum EffectType
    {
        SetFlag,
        ClearFlag,
        MoodDelta,
        CredibilityDelta,
        PersonaSuspicionDelta,
        FactionMoodDelta,
        AddLead,
        RemoveLead,
        AddEvidence,
        AddNews,
        ScheduleItem,
        Fine,
        Arrest,
        RegimePressureDelta,
        AddMemoryLog,
        UnlockSource
    }

    // Scheduling shape used by effects and/or rules
    public struct ScheduleSpec
    {
        public readonly int? WeekRelative;            // +0 current, +1 next week
        public readonly int? OffsetDays;              // +n days
        public readonly string DayOfWeek;             // "Monday"... optional
        public readonly string Segment;               // "Morning"/"Afternoon"/"Evening"/"Night"
        public readonly int? ExpiresAfterSegments;    // if >0, drop after N segs

        public ScheduleSpec(int? weekRelative = null, int? offsetDays = null,
                            string dayOfWeek = null, string segment = null,
                            int? expiresAfterSegments = null)
        {
            WeekRelative = weekRelative;
            OffsetDays = offsetDays;
            DayOfWeek = dayOfWeek;
            Segment = segment;
            ExpiresAfterSegments = expiresAfterSegments;
        }
    }

    // Immutable effect description
    public struct Effect
    {
        public readonly EffectType Kind;
        public readonly string A;             // id/name
        public readonly int I1;
        public readonly string B;             // aux value

        // Optional schedule shaping attached at rule/effect level
        public readonly System.Nullable<ScheduleSpec> Schedule;
        public readonly string SlotKind;
        public readonly int Priority;
        public readonly bool Background;
        public readonly string ExclusiveFlag;

        public Effect(EffectType kind, string a = null, int i1 = 0, string b = null,
                      System.Nullable<ScheduleSpec> schedule = null, string slotKind = null,
                      int priority = 0, bool background = false, string exclusiveFlag = null)
        {
            Kind = kind; A = a; I1 = i1; B = b;
            Schedule = schedule;
            SlotKind = slotKind;
            Priority = priority;
            Background = background;
            ExclusiveFlag = exclusiveFlag;
        }
    }

    // Rule is a reference type; no 'init' (C# 7.3 compatible)
    public sealed class Rule
    {
        public string EventId { get; private set; }              // content id
        public int RuleIndex { get; private set; }               // stable within event
        public TriggerType Trigger { get; private set; }
        public int Priority { get; private set; }
        public Condition[] Conditions { get; private set; }
        public Effect[] Effects { get; private set; }
        public string ExclusiveFlag { get; private set; }        // applies to scheduled items if set
        public string SlotKind { get; private set; }
        public bool Background { get; private set; }

        // Constructor for parsed content
        public Rule(string eventId,
                    int ruleIndex,
                    TriggerType trigger,
                    int priority,
                    Condition[] conditions,
                    Effect[] effects,
                    string exclusiveFlag,
                    string slotKind,
                    bool background)
        {
            EventId = eventId;
            RuleIndex = ruleIndex;
            Trigger = trigger;
            Priority = priority;
            Conditions = conditions ?? new Condition[0];
            Effects = effects ?? new Effect[0];
            ExclusiveFlag = exclusiveFlag;
            SlotKind = slotKind;
            Background = background;
        }

        // Parameterless for serializers if needed
        public Rule() { }
    }

    // Invocation is a lightweight immutable pair
    public struct EffectInvocation
    {
        public readonly Rule SourceRule;
        public readonly Effect Effect;

        public EffectInvocation(Rule src, Effect effect)
        {
            SourceRule = src;
            Effect = effect;
        }

        public string StableKey
        {
            get { return SourceRule.EventId + "#" + SourceRule.RuleIndex; }
        }
    }
}
