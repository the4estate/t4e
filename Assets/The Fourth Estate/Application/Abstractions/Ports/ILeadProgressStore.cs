namespace T4E.App.Abstractions.Ports
{
    using T4E.Domain.Core.Leads;

    /// <summary>
    /// Save/load LeadProgressState by lead id (world-saveable + deterministic).
    /// </summary>
    public interface ILeadProgressStore
    {
        bool TryGet(string leadId, out LeadProgressState state);
        void Upsert(LeadProgressState state);
    }
}
