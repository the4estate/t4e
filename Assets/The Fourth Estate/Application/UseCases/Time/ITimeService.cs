using T4E.Domain;
namespace T4E.App.UseCases
{
    public interface ITimeService
    {
        GameDate Current { get; }
        void AdvanceSegment(); 
        void AdvanceDay();
        void AdvanceWeek();
    }
}
