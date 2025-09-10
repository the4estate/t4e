// Domain/Core/CET/TriggerContext.cs
using System.Collections.Generic;
using T4E.Domain;

namespace T4E.Domain.Core.CET
{
    /// <summary>
    /// Immutable input for CET evaluation. Keep it POCO (no Unity refs).
    /// </summary>
    public sealed class TriggerContext
    {
        public TriggerType Type { get; }
        public GameDate Date { get; }                 // your existing domain type
        public DaySegment Segment { get; }            // existing enum
        public Weekday DayOfWeek { get; }             // existing enum

        /// <summary>Keyed IDs from the originating system (persona_id, lead_id, evidence_type,…)</summary>
        public IReadOnlyDictionary<string, string> ContextMap { get; }

        /// <summary>
        /// A stable, unique id for this specific firing (e.g., "w12-d3-Afternoon#publish-42").
        /// Used for idempotency.
        /// </summary>
        public string TriggerInstanceId { get; }

        public TriggerContext(
            TriggerType type,
            GameDate date,
            DaySegment segment,
            Weekday dayOfWeek,
            IReadOnlyDictionary<string, string> contextMap,
            string triggerInstanceId)
        {
            Type = type;
            Date = date;
            Segment = segment;
            DayOfWeek = dayOfWeek;
            ContextMap = contextMap ?? new Dictionary<string, string>();
            TriggerInstanceId = triggerInstanceId;
        }
    }
}
