using System;


namespace T4E.Domain.Core.Leads
{
    /// <summary>
    /// Value object describing required evidence for a lead. Using strings keeps JSON-friendly shape.
    /// </summary>
    public readonly struct EvidenceRequirement : IEquatable<EvidenceRequirement>
    {
        public readonly string Type; // "witness" | "document" | "object"
        public readonly string Id; // stable ID of the evidence item/persona/object


        public EvidenceRequirement(string type, string id)
        {
            Type = type ?? string.Empty;
            Id = id ?? string.Empty;
        }


        public bool Equals(EvidenceRequirement other)
        => string.Equals(Type, other.Type, StringComparison.OrdinalIgnoreCase)
        && string.Equals(Id, other.Id, StringComparison.OrdinalIgnoreCase);


        public override bool Equals(object obj) => obj is EvidenceRequirement other && Equals(other);
        public override int GetHashCode() => HashCode.Combine(Type?.ToLowerInvariant(), Id?.ToLowerInvariant());
        public override string ToString() => $"{Type}:{Id}";
    }
}