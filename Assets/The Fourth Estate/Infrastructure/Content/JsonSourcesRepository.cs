using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json.Linq;
using T4E.App.Abstractions.Dtos;
using T4E.App.Abstractions.Ports;
using T4E.Domain.Core.CET;

namespace T4E.Infrastructure.Content
{
    /// <summary>
    /// JSON-backed repository that loads all SourceDto objects from sources.json.
    /// Mirrors JsonNewsRepository pattern for simplicity.
    /// </summary>
    public sealed class JsonSourcesRepository : IContentRepository
    {
        private readonly Dictionary<string, object> _byId = new();

        public JsonSourcesRepository(string sourcesJsonPath)
        {
            if (File.Exists(sourcesJsonPath))
                LoadSourcesFile(sourcesJsonPath);
            else
                throw new FileNotFoundException($"Sources file not found at {sourcesJsonPath}");
        }

        private void LoadSourcesFile(string path)
        {
            var text = File.ReadAllText(path);
            var root = JObject.Parse(text);
            var items = (JArray?)root["items"] ?? new JArray();

            foreach (JObject s in items)
            {
                var dto = new SourceDto
                {
                    Id = (string)s["id"]!,
                    Type = ParseType((string?)s["type"]),
                    Weight = (int?)s["weight"] ?? 1,
                    SupportsNewsIds = new List<string>(),
                    ConflictsNewsIds = new List<string>(),
                    ContentRef = (string?)s["content_ref"]
                };

                // supports_news_ids
                if (s["supports_news_ids"] is JArray supArr)
                    foreach (var idTok in supArr)
                        if (!string.IsNullOrWhiteSpace((string?)idTok))
                            dto.SupportsNewsIds.Add((string)idTok!);

                // conflicts_news_ids (optional)
                if (s["conflicts_news_ids"] is JArray conArr)
                    foreach (var idTok in conArr)
                        if (!string.IsNullOrWhiteSpace((string?)idTok))
                            dto.ConflictsNewsIds?.Add((string)idTok!);

                _byId[dto.Id] = dto;
            }
        }

        private static SourceType ParseType(string? raw)
        {
            return raw?.Trim() switch
            {
                "Primary" => SourceType.Primary,
                "Official" => SourceType.Official,
                "Secondary" => SourceType.Secondary,
                "Rumor" => SourceType.Rumor,
                "Anonymous" => SourceType.Anonymous,
                _ => SourceType.Anonymous
            };
        }

        // --- IContentRepository implementation ---

        public Rule[] GetRulesByTrigger(TriggerType trigger)
        {
            // Sources do not define CET rules; placeholder for interface compliance.
            return Array.Empty<Rule>();
        }

        public T? Load<T>(string id) where T : class
        {
            if (_byId.TryGetValue(id, out var obj) && obj is T t)
                return t;
            return null;
        }

        public IEnumerable<T> LoadAll<T>() where T : class
        {
            foreach (var kv in _byId)
                if (kv.Value is T t)
                    yield return t;
        }
    }
}
