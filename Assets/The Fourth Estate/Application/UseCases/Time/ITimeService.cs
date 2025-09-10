using T4E.Domain;
namespace T4E.App.UseCases
{
    public interface ITimeService
    {
        GameDate Current { get; }
        void AdvanceSegment(); // raises signals + runs timeline for the segment
        void AdvanceDay();
        void AdvanceWeek();
    }
}
