using T4E.Domain;

namespace T4E.App.Abstractions
{
    /// <summary>
    /// Agenda service that holds scheduled timeline items
    /// and emits them when their time arrives.
    /// </summary>
    public interface ITimelineScheduler
    {
        /// <summary>Schedule a new item to run at its GameDate.</summary>
        void Enqueue(TimelineItem item);

        /// <summary>Number of items currently waiting.</summary>
        int Count { get; }
    }
}
