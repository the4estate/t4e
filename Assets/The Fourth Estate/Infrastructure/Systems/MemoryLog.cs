using System.Collections.Generic;
using T4E.App.Abstractions.Dtos;
using T4E.App.Abstractions.Ports;

namespace T4E.Infrastructure.Systems
{
    /// <summary>
    /// Minimal in-memory footprint store
    /// Later  persist via ISaveRepository; for nowjust keep it in RAM.
    /// </summary>
    public sealed class MemoryLog : IMemoryLog
    {
        public readonly struct PublishedNewsEntry
        {
            public readonly string NewsId;
            public readonly Tone Tone;

            public PublishedNewsEntry(string newsId, Tone tone)
            {
                NewsId = newsId;
                Tone = tone;
            }

            public override string ToString() => $"Published {NewsId} [{Tone}]";
        }

        private readonly List<PublishedNewsEntry> _published = new List<PublishedNewsEntry>(64);

        // --- IMemoryLog ---

        public void RecordPublishedNews(string newsId, Tone tone)
        {
            _published.Add(new PublishedNewsEntry(newsId, tone));
        }

        // --- Optional read API for later conditions/UI (safe to have even if unused now) ---

        public IReadOnlyList<PublishedNewsEntry> GetPublished() => _published;
        public bool HasPublished(string newsId) => _published.Exists(e => e.NewsId == newsId);
    }
}
