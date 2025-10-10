using System;
using Newtonsoft.Json.Linq; 
using System.Collections.Generic;
using System.IO;
using T4E.App.Abstractions;
using T4E.App.Abstractions.Dtos;
using T4E.App.Abstractions.Ports;
using T4E.Domain.Core.CET;

namespace T4E.Infrastructure.Content
{
    /// <summary>
    /// Minimal JSON-backed repository that only knows how to load News.
    /// Reads once on construction. Good enough for EA; later swap for Addressables.
    /// </summary>
    public sealed class JsonNewsRepository : IContentRepository
    {
        private readonly Dictionary<string, object> _byId = new();

        public JsonNewsRepository(string newsJsonPath)
        {
            if (File.Exists(newsJsonPath))
                LoadNewsFile(newsJsonPath);
        }

        private void LoadNewsFile(string path)
        {
            var text = File.ReadAllText(path);
            var root = JObject.Parse(text);
            var items = (JArray?)root["items"] ?? new JArray();

            foreach (var token in items)
            {
                var o = (JObject)token;

                var dto = new NewsDto
                {
                    Id = (string)o["id"]!,
                    Era = (string?)o["era"] ?? "",
                    Subject = (string?)o["subject"] ?? "",
                    BodyGeneric = (string?)o["body_generic"] ?? "",
                    Tags = new List<string>(),
                    Flags = new List<string>(),
                    PersonasInvolved = new List<string>(),
                    ToneAllowed = new List<Tone>(),
                    ToneVariants = new Dictionary<string, NewsToneDetailsDto>(),
                    Source = new NewsSourcesDto()
                };

                // tags
                if (o["tags"] is JArray tagsArr)
                    foreach (var t in tagsArr) dto.Tags.Add((string)t!);

                // flags
                if (o["flags"] is JArray flagsArr)
                    foreach (var f in flagsArr) dto.Flags.Add((string)f!);

                // personas_involved
                if (o["personas_involved"] is JArray persArr)
                    foreach (var p in persArr) dto.PersonasInvolved.Add((string)p!);

                // sources block
                if (o["sources"] is JObject sourcesObj)
                {
                    var ns = new NewsSourcesDto();

                    // supports[]
                    if (sourcesObj["supports"] is JArray supArr)
                        foreach (var s in supArr)
                            if (!string.IsNullOrWhiteSpace((string?)s))
                                ns.Supports.Add((string)s!);

                    // conflicts[] (optional)
                    if (sourcesObj["conflicts"] is JArray conArr)
                        foreach (var c in conArr)
                            if (!string.IsNullOrWhiteSpace((string?)c))
                            {
                                ns.Conflicts ??= new List<string>();
                                ns.Conflicts.Add((string)c!);
                            }

                    // min_to_publish (defaults to 1 if missing)
                    ns.MinToPublish = sourcesObj.Value<int?>("min_to_publish") ?? 1;

                    dto.Source = ns;
                }

                // tone_allowed
                if (o["tone_allowed"] is JArray tones)
                {
                    foreach (var t in tones)
                    {
                        var s = (string)t!;
                        if (s == "Supportive") dto.ToneAllowed.Add(Tone.Supportive);
                        else if (s == "Neutral") dto.ToneAllowed.Add(Tone.Neutral);
                        else if (s == "Critical") dto.ToneAllowed.Add(Tone.Critical);
                    }
                }

                // tone_variants
                if (o["tone_variants"] is JObject tv)
                {
                    void AddVariant(string name)
                    {
                        if (tv[name] is JObject v)
                        {
                            var details = new NewsToneDetailsDto
                            {
                                Headline = (string?)v["headline"] ?? "",
                                Short = (string?)v["short"] ?? "",
                                Effects = new List<Effect>()
                            };

                            // effects (optional; empty in your sample — mapping left simple)
                            if (v["effects"] is JArray ef)
                            {
                                // If you later add effect objects in news.json, map them to Domain Effect here.
                                // For now, your sample has [], so we skip.
                            }

                            dto.ToneVariants[name] = details;
                        }
                    }
                    AddVariant("Supportive");
                    AddVariant("Neutral");
                    AddVariant("Critical");
                }

                _byId[dto.Id] = dto;
            }
        }

        // --- IContentRepository ---

        public Rule[] GetRulesByTrigger(TriggerType trigger)
        {
            // This repository is news-only; no rules backed by JSON yet.
            return Array.Empty<Rule>();
        }

        public T? Load<T>(string id) where T : class
        {
            if (_byId.TryGetValue(id, out var obj) && obj is T t) return t;
            return null;
        }

        public IEnumerable<T> LoadAll<T>() where T : class
        {
            foreach (var kv in _byId)
                if (kv.Value is T t) yield return t;
        }
    }
}
