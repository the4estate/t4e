using System.Collections.Generic;
using T4E.App.Abstractions;
using T4E.Domain;
using T4E.App.UseCases;

namespace T4E.App.UseCases
{
    public sealed class TimeService : ITimeService
    {
        private readonly IAppLogger _log;
        public GameDate Current { get; private set; }

        public TimeService(IAppLogger log, GameDate start) { _log = log; Current = start; }

        public void AdvanceSegment()
        {
            var next = Current.NextSegment();
            bool dayChanged = next.Day != Current.Day;
            bool weekChanged = next.Week != Current.Week;
            Current = next;

            GameSignals.RaiseSegment(Current);
            if (dayChanged) GameSignals.RaiseDay(Current);
            if (weekChanged) GameSignals.RaiseWeek(Current);
        }

        public void AdvanceDay()  { for (int i = 0; i < 4; i++) AdvanceSegment(); }
        public void AdvanceWeek() { for (int i = 0; i < 7; i++) AdvanceDay(); }
    }
}
