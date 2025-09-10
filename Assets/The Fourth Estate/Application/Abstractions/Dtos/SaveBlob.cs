using T4E.App.Abstractions;
namespace T4E.App.Abstractions
{
    // v1: minimal state for M0/M1 loop
    public sealed class SaveBlob
    {
        public int Version { get; set; } = 1;

        // Time
        public int Year { get; set; }
        public int Week { get; set; }           // 1..52
        public int Day { get; set; }            // enum int (Weekday)
        public int Segment { get; set; }        // enum int (DaySegment)

        // Timeline queue (flattened)
        public TimelineItemDto[] Queue { get; set; } = System.Array.Empty<TimelineItemDto>();

        // RNG state (so sims are deterministic across loads)
        public uint RngState { get; set; }      // from DeterministicRandom

        // World state (MVP)
        public string[] News { get; set; } = System.Array.Empty<string>();
        public string[] Leads { get; set; } = System.Array.Empty<string>();
        public KV[] Flags { get; set; } = System.Array.Empty<KV>();
    }
    public sealed class KV { public string Key = ""; public int Value; }
}
