namespace T4E.App.Abstractions
{
    // Keeps track of fired id's
    public interface IFiredLedger
    {
        bool HasFired(string eventId, int ruleIndex, string triggerInstanceId);
        void MarkFired(string eventId, int ruleIndex, string triggerInstanceId);
    }
}

