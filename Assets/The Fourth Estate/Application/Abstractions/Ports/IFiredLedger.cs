// Application/Abstractions/Ports/IFiredLedger.cs
namespace T4E.App.Abstractions
{
    public interface IFiredLedger
    {
        bool HasFired(string eventId, int ruleIndex, string triggerInstanceId);
        void MarkFired(string eventId, int ruleIndex, string triggerInstanceId);
    }
}
