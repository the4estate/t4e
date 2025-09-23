using System.Collections.Generic;
using T4E.Domain.Core.CET; // Domain CET Effect lives here

namespace T4E.App.Abstractions.Dtos
{
    // The 3 tones we support in MVP. JSON uses these as strings; loader will map to enum.
    public enum Tone { Supportive, Neutral, Critical }

    // Who provided the story. MVP keeps it simple.
    public enum SourceType { Anonymous, Persona }

    // The per-tone content (headline + short + effects). Body is shared across tones.
    public sealed class NewsToneDetailsDto
    {
        // LocalizedText is just string for MVP; upgrade later when you add localization tables.
        public string Headline { get; set; } = "";
        public string Short { get; set; } = "";

        // IMPORTANT: reuse Domain Effect so we don’t duplicate effect shapes.
        public List<Effect> Effects { get; set; } = new();
    }

    // Source info. If Type == Persona, PersonaId must be set (enforced by schema & loader).
    public sealed class SourceDto
    {
        public SourceType Type { get; set; } = SourceType.Anonymous;
        public string? PersonaId { get; set; }
    }

    // The News data the game uses everywhere (UI, UseCases).
    public sealed class NewsDto
    {
        // Content identity
        public string Id { get; set; } = "";
        public string Era { get; set; } = "";

        // Top line in the newspaper index
        public string Subject { get; set; } = "";

        // Optional flavor/categorization
        public List<string> Tags { get; set; } = new();
        public List<string> Flags { get; set; } = new();

        // Which tones are valid for this News (schema: 1..3)
        public List<Tone> ToneAllowed { get; set; } = new();

        // Tone -> { headline, short, effects }
        // We use string keys ("Supportive"/"Neutral"/"Critical") to match JSON easily.
        public Dictionary<string, NewsToneDetailsDto> ToneVariants { get; set; } = new();

        // The immersive article body shown after publishing (shared across tones)
        public string BodyGeneric { get; set; } = "";

        // Who’s involved (optional, 0..3)
        public List<string> PersonasInvolved { get; set; } = new();

        // Source (Anonymous or a Persona id)
        public SourceDto Source { get; set; } = new();
    }
}
