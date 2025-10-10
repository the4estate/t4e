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
    /// Loads Infrastructure/Content/leads.json and exposes LeadDto objects.
    /// Mirrors your JsonNewsRepository style (File + JObject).
    /// </summary>
    public sealed class JsonLeadsRepository : ILeadRepository
    {
        private readonly Dictionary<string, LeadDto> _byId = new(256, StringComparer.Ordinal);

        public JsonLeadsRepository(string leadsJsonPath)
        {
            if (File.Exists(leadsJsonPath))
                LoadLeadsFile(leadsJsonPath);
        }

        public bool TryGet(string id, out LeadDto lead) => _byId.TryGetValue(id ?? string.Empty, out lead);

        public LeadDto Get(string id)
        {
            if (id == null) throw new ArgumentNullException(nameof(id));
            if (_byId.TryGetValue(id, out var lead)) return lead;
            throw new KeyNotFoundException("Lead not found: " + id);
        }

        private void LoadLeadsFile(string path)
        {
            var text = File.ReadAllText(path);
            var root = JObject.Parse(text);
            var items = (JArray?)root["items"] ?? new JArray();

            foreach (var token in items)
            {
                var o = (JObject)token;

                var dto = new LeadDto
                {
                    Id         = (string)o["id"]!,
                    Era        = (string?)o["era"] ?? string.Empty,
                    Title      = ReadLocalized(o["title"]),
                    ExposeText = ReadLocalized(o["expose_text"]),
                    Difficulty = o.Value<int?>("difficulty") ?? 1,
                    Personas   = new List<string>(),
                    EvidenceRequirement = new EvidenceRequirementDto(),
                    SpawnViaTriggerId   = (string?)o["spawn_via_trigger_id"] ?? string.Empty,
                    OnExposeEffects     = new List<Effect>()
                };

                // personas[]
                if (o["personas"] is JArray persArr)
                    foreach (var p in persArr)
                        if (!string.IsNullOrWhiteSpace((string?)p))
                            dto.Personas.Add((string)p!);

                // evidence_requirement { min_total, allow[] }
                if (o["evidence_requirement"] is JObject er)
                {
                    dto.EvidenceRequirement.MinTotal = er.Value<int?>("min_total") ?? 0;

                    if (er["allow"] is JArray allowArr)
                    {
                        foreach (var a in allowArr)
                        {
                            var s = ((string?)a)?.Trim();
                            if (!string.IsNullOrEmpty(s))
                                dto.EvidenceRequirement.Allow.Add(s);
                        }
                    }
                }

                // on_expose_effects[]
                if (o["on_expose_effects"] is JArray effectsArr)
                {
                    foreach (var e in effectsArr)
                    {
                        if (e is JObject eo && TryMapEffect(eo, out var effect))
                            dto.OnExposeEffects.Add(effect);
                    }
                }

                // basic guardrails (your editor validator does the strict checks)
                if (string.IsNullOrWhiteSpace(dto.Id)) continue;
                if (dto.Difficulty < 1 || dto.Difficulty > 5)
                    dto.Difficulty = Math.Max(1, Math.Min(5, dto.Difficulty));
                if (dto.EvidenceRequirement.Allow.Count == 0)
                    dto.EvidenceRequirement.Allow.Add("Witness"); // match schema casing

                _byId[dto.Id] = dto;
            }
        }

        // LocalizedText per common.schema: either { "text": "..."} or { "key": "loc:..." }, or a plain string.
        private static string ReadLocalized(JToken token)
        {
            if (token == null) return string.Empty;
            if (token.Type == JTokenType.String) return (string)token!;
            if (token is JObject o)
            {
                var txt = (string?)o["text"];
                if (!string.IsNullOrEmpty(txt)) return txt;
                var key = (string?)o["key"];
                return key ?? string.Empty;
            }
            return token.ToString();
        }

        // Accept schema PascalCase ("FlagSet") and a few snake_case fallbacks ("set_flag")
        private static bool TryMapEffect(JObject obj, out Effect effect)
        {
            effect = default;

            string type = ((string?)obj["type"])?.Trim() ?? "";
            if (type.Length == 0) return false;

            string norm = type.Replace("_", "").ToLowerInvariant();

            switch (norm)
            {
                case "flagset":     // schema: type="FlagSet" { key, value:bool }
                case "setflag":
                    {
                        var key = (string?)obj["key"] ?? (string?)obj["id"] ?? "";
                        var vTok = obj["value"];
                        int i1 = vTok?.Type == JTokenType.Boolean ? ((bool)vTok ? 1 : 0) : (obj.Value<int?>("value") ?? 1);
                        effect = new Effect(EffectType.SetFlag, a: key, i1: i1);
                        return true;
                    }
                case "flagclear":
                case "clearflag":
                    effect = new Effect(EffectType.ClearFlag, a: (string?)obj["key"] ?? (string?)obj["id"] ?? "");
                    return true;

                case "mooddelta":
                    effect = new Effect(EffectType.MoodDelta, a: (string?)obj["axis"] ?? "", i1: obj.Value<int?>("amount") ?? 0);
                    return true;

                case "credibilitydelta":
                    effect = new Effect(EffectType.CredibilityDelta, i1: obj.Value<int?>("amount") ?? 0);
                    return true;

                case "personasuspiciondelta":
                case "suspiciondelta":
                    effect = new Effect(EffectType.PersonaSuspicionDelta, a: (string?)obj["persona_id"] ?? (string?)obj["personaId"] ?? "", i1: obj.Value<int?>("amount") ?? 0);
                    return true;

                case "factionmooddelta":
                    effect = new Effect(EffectType.FactionMoodDelta, a: (string?)obj["faction_id"] ?? "", i1: obj.Value<int?>("amount") ?? 0);
                    return true;

                case "addlead":
                    effect = new Effect(EffectType.AddLead, a: (string?)obj["id"] ?? "");
                    return true;

                case "removelead":
                    effect = new Effect(EffectType.RemoveLead, a: (string?)obj["id"] ?? "");
                    return true;

                case "addnews":
                case "spawnnews":
                    effect = new Effect(EffectType.AddNews, a: (string?)obj["id"] ?? "");
                    return true;

                case "addevidence":
                    effect = new Effect(EffectType.AddEvidence, a: (string?)obj["id"] ?? "");
                    return true;

                case "scheduleitem":
                case "schedulemeeting":
                    effect = new Effect(EffectType.ScheduleItem, a: (string?)obj["id"] ?? "");
                    return true;

                case "fine":
                    effect = new Effect(EffectType.Fine, i1: obj.Value<int?>("amount") ?? 0);
                    return true;


                case "addmemory":
                case "addmemorylog":
                    effect = new Effect(EffectType.AddMemoryLog, a: (string?)obj["note"] ?? "");
                    return true;

                case "changeregimepressure":
                case "regimepressuredelta":
                    effect = new Effect(EffectType.RegimePressureDelta, i1: obj.Value<int?>("amount") ?? 0);
                    return true;


                default:
                    return false;
            }
        }
    }
}
