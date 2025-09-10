using System;
using T4E.Domain;
namespace T4E.Domain
{
    public static class GameSignals
    {
        public static event Action<GameDate> SegmentAdvanced;
        public static event Action<GameDate> DayAdvanced;
        public static event Action<GameDate> WeekAdvanced;
        public static event Action<TimelineItem, GameDate> ItemDue;

        public static void RaiseSegment(GameDate d) => SegmentAdvanced?.Invoke(d);
        public static void RaiseDay(GameDate d) => DayAdvanced?.Invoke(d);
        public static void RaiseWeek(GameDate d) => WeekAdvanced?.Invoke(d);
        public static void RaiseItemDue(TimelineItem item, GameDate d) => ItemDue?.Invoke(item, d);
    }
}
