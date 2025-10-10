using System.Collections.Generic;
using T4E.Domain.Core.CET;

namespace T4E.App.Abstractions.Dtos
{
    public sealed class LeadDto
    {
        public string Id { get; set; } = "";
        public string Era { get; set; } = "";
        public string Title { get; set; } = "";
        public string ExposeText { get; set; } = "";
        public int Difficulty { get; set; } = 1;
        public List<string> Personas { get; set; } = new();
        public EvidenceRequirementDto EvidenceRequirement { get; set; } = new();
        public string SpawnViaTriggerId { get; set; } = "";
        public List<Effect> OnExposeEffects { get; set; } = new();
    }
}
