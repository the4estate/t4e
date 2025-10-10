using System;
using System.Collections.Generic;

namespace T4E.App.Abstractions.Dtos
{
    /// <summary>
    /// Mirrors lead.schema.json → evidence_requirement { min_total, allow[] }.
    /// Types are strings per common.schema EvidenceType ("witness"|"document"|"object").
    /// </summary>
    [Serializable]
    public sealed class EvidenceRequirementDto
    {
        public int MinTotal;
        public List<string> Allow = new List<string>(); // evidence types allowed to count
    }
}
