using T4E.App.Abstractions.Dtos;

namespace T4E.App.Abstractions.Ports
{
    /// <summary>
    /// Narrow port to avoid DI ambiguity with existing IContentRepository implementations.
    /// </summary>
    public interface ILeadRepository
    {
        bool TryGet(string id, out LeadDto lead);
        LeadDto Get(string id);
    }
}
