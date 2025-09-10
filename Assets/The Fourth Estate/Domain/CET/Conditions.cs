// Domain/Core/CET/Conditions.cs
using System;

namespace T4E.Domain.Core.CET
{
    public enum ConditionKind
    {
        WeekAtLeast,
        WeekInRange,          // [min, max] inclusive
        SegmentIs,            // Morning/Afternoon/Evening/Night
        DayOfWeekIs,          // Monday..Sunday
        FlagExists,           // flag id in world
        FlagIs,               // flag id == value
        GlobalMoodAtLeast,    // e.g., Credibility >= x
        GlobalMoodAtMost,
        RegimeIs,
        RegimePressureAtLeast,
        PersonaSuspicionAtLeast,
        PersonaSuspicionAtMost,
        FactionMoodAtLeast,
        FactionMoodAtMost,
        ContextEquals,        // key, value
        ContextIn             // key, [v1,v2,...]
    }

    public readonly struct Condition
    {
        public ConditionKind Kind { get; }
        public readonly string A;     // id/key/enum name
        public readonly int I1;       // numeric 1
        public readonly int I2;       // numeric 2
        public readonly string B;     // value/enum or comma list

        public Condition(ConditionKind kind, string a = null, int i1 = 0, int i2 = 0, string b = null)
        {
            Kind = kind; A = a; I1 = i1; I2 = i2; B = b;
        }
    }
}
