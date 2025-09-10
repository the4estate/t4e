using System;

namespace T4E.App.Abstractions
{
    [Serializable]
    public sealed class TimelineItemDto
    {
        public string Id = "";
        public int Year;
        public int Week;
        public int Day;      // (int)Weekday
        public int Segment;  // (int)DaySegment
        public string PayloadType = "event";
        public string[] SpawnNewsIds = Array.Empty<string>();
        public string[] SpawnLeadIds = Array.Empty<string>();
    }
}
