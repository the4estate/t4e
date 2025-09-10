using Newtonsoft.Json.Linq;
using System;
using System.IO;
using T4E.App.Abstractions;   
using T4E.Domain;
#nullable enable
namespace T4E.Infrastructure.Content
{
    public static class ContentLoader
    {
        public static void EnqueueEventsFromJson(string eventsJsonPath, ITimelineScheduler scheduler)
        {
            if (!File.Exists(eventsJsonPath))
                return;

            var doc = JObject.Parse(File.ReadAllText(eventsJsonPath));
            var items = (JArray?)doc["items"] ?? new JArray();

            foreach (var token in items)
            {
                var e = token as JObject;
                if (e == null) continue;

                var id = e.Value<string>("id") ?? string.Empty;
                var sched = e["schedule"] as JObject;
                if (string.IsNullOrWhiteSpace(id) || sched == null) continue;

                var dow = sched.Value<string>("dayOfWeek") ?? "Monday";
                var seg = sched.Value<string>("segment")   ?? "Morning";

                // GameDate has ctor (year, week, day, segment)
                var when = new GameDate(1850, 1, ParseWeekDay(dow), ParseSegment(seg));

                var spawns = e["spawns"] as JObject ?? new JObject();
                var newsIds = ((JArray?)spawns["news_ids"] ?? new JArray()).ToObject<string[]>() ?? Array.Empty<string>();
                var leadIds = ((JArray?)spawns["lead_ids"] ?? new JArray()).ToObject<string[]>() ?? Array.Empty<string>();

                // Matches your TimelineItem signature
                var item = new TimelineItem(id, when, "event", newsIds, leadIds);

                scheduler.Enqueue(item);
            }
        }

        private static Weekday ParseWeekDay(string s)
        {
            return s switch
            {
                "Monday" => Weekday.Monday,
                "Tuesday" => Weekday.Tuesday,
                "Wednesday" => Weekday.Wednesday,
                "Thursday" => Weekday.Thursday,
                "Friday" => Weekday.Friday,
                "Saturday" => Weekday.Saturday,
                "Sunday" => Weekday.Sunday,
                _ => Weekday.Monday
            };
        }

        private static DaySegment ParseSegment(string s)
        {
            return s switch
            {
                "Morning" => DaySegment.Morning,
                "Afternoon" => DaySegment.Afternoon,
                "Evening" => DaySegment.Evening,
                "Night" => DaySegment.Night,
                _ => DaySegment.Morning
            };
        }
    }
}
