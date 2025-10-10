using System.Collections.Generic;

namespace T4E.App.Abstractions.Dtos
{
    public enum SourceType
    {
        Primary,
        Official,
        Secondary,
        Rumor,
        Anonymous
    }

    public sealed class SourceDto
    {
        public string Id { get; set; } = "";
        public SourceType Type { get; set; }
        public int Weight { get; set; } // 1..5
        public List<string> SupportsNewsIds { get; set; } = new();
        public List<string>? ConflictsNewsIds { get; set; }
        public string? ContentRef { get; set; }
    }
}
