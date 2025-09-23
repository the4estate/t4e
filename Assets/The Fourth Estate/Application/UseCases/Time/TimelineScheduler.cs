using System.Collections.Generic;
using T4E.Domain;
using T4E.App.Abstractions.Ports;
namespace T4E.App.UseCases
{
    // Listens to SegmentAdvanced and emits ItemDue for all items scheduled exactly at that slot.
    public sealed class TimelineScheduler : ITimelineScheduler
    {
        // multimap: multiple items can share the same time-slot key
        private readonly SortedList<string, List<TimelineItem>> _queue = new();

        public int Count => _queue.Count;

        public TimelineScheduler()
        {
            GameSignals.SegmentAdvanced += OnSegmentAdvanced;
        }

        public void Enqueue(TimelineItem item)
        {
            var key = Key(item.When, item.Id);
            if (!_queue.TryGetValue(key, out var list))
            {
                list = new List<TimelineItem>();
                _queue.Add(key, list);
            }
            list.Add(item);
        }

        private void OnSegmentAdvanced(GameDate now)
        {
            var prefix = Prefix(now);
            var dueKeys = new List<string>();

            // collect first to avoid mutating while enumerating
            foreach (var kv in _queue)
            {
                if (kv.Key.StartsWith(prefix))
                    dueKeys.Add(kv.Key);
                // early exit: keys are sorted; break once we pass the prefix range
                else if (kv.Key.CompareTo(prefix) > 0 && !kv.Key.StartsWith(prefix))
                    break;
            }

            foreach (var key in dueKeys)
            {
                foreach (var item in _queue[key])
                    GameSignals.RaiseItemDue(item, now);
                _queue.Remove(key);
            }
        }

        private static string Prefix(GameDate d) =>
            $"{d.Year:D4}-{d.Week:D2}-{(int)d.Day}-{(int)d.Segment}-";

        private static string Key(GameDate d, string id) => Prefix(d) + id;
    }
}
