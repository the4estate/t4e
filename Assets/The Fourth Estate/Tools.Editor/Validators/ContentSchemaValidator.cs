#if UNITY_EDITOR
#nullable enable
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

namespace T4E.Tools.Editor.Validators
{
    public static class ContentSchemaValidator
    {
        // Where your JSON lives
        private const string ContentDir = "Assets/The Fourth Estate/Infrastructure/Content";

        // common.schema.json -> IdString.pattern
        private static readonly Regex IdPattern =
            new Regex("^[a-z]{3,8}\\.(news|lead|event|persona|faction|manifest|trial|location|state|scenario|source)\\.[a-z0-9_]+(?:_\\d{3})?$",
                      RegexOptions.Compiled);

        // Simple helper
        private static void Must(bool cond, string msg, List<string> errs)
        {
            if (!cond) errs.Add(msg);
        }
        private static string SafeTok(JToken tok) => tok?.ToString() ?? string.Empty;

        private static bool NonEmpty(string s) => !string.IsNullOrWhiteSpace(s);

        // Use in Must() calls to check “set contains tok” safely
        private static bool SetHas(HashSet<string> set, JToken tok, out string val)
        {
            val = SafeTok(tok);
            return NonEmpty(val) && set.Contains(val);
        }

        [MenuItem("T4E/Validators/Content Schema")]
        public static void RunMenu()
        {
            try { Run(); EditorUtility.DisplayDialog("Content Schema", "All content OK.", "OK"); }
            catch (Exception ex) { Debug.LogError(ex.Message); EditorUtility.DisplayDialog("Content Schema", ex.Message, "OK"); throw; }
        }

        // Call this from RunAll
        public static void Run()
        {
            var errors = new List<string>();
            var root = ContentDir.Replace('\\', '/');
            if (!Directory.Exists(root))
            {
                Debug.Log($"[Schema] No content folder at {root}. Skipping.");
                return;
            }

            // Load individual files if present
            var newsPath = Path.Combine(root, "news.json").Replace('\\', '/');
            var leadsPath = Path.Combine(root, "leads.json").Replace('\\', '/');
            var eventsPath = Path.Combine(root, "events.json").Replace('\\', '/');
            var personasPath = Path.Combine(root, "personas.json").Replace('\\', '/');
            var factionsPath = Path.Combine(root, "factions.json").Replace('\\', '/');
            var statePath = Path.Combine(root, "state.json").Replace('\\', '/');
            var trialPath = Path.Combine(root, "trials.json").Replace('\\', '/');
            var locPath = Path.Combine(root, "locations.json").Replace('\\', '/');
            var scenarioPath = Path.Combine(root, "scenario.json").Replace('\\', '/');
            var sourcesPath = Path.Combine(root, "sources.json").Replace('\\', '/');

            // Index of existing IDs by type for xref checks
            var newsIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var leadIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var eventIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var personaIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var factionIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var locationIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var stateIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var trialIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var sourceIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            // Local function to validate ContentHeader on an item
            void CheckHeader(JObject o, string file, int rootVer, List<string> errs)
            {
                string id = o.Value<string>("id")  ?? "";
                string era = o.Value<string>("era") ?? "";

                Must(!string.IsNullOrWhiteSpace(id), $"[{file}] missing id", errs);
                Must(IdPattern.IsMatch(id), $"[{file}] bad id '{id}' (must match era.type.slug[_NNN])", errs);
                Must(!string.IsNullOrWhiteSpace(era), $"[{file}:{id}] missing era", errs);

                int itemVer = o.Value<int?>("content_version") ?? 0;
                int effectiveVer = itemVer != 0 ? itemVer : rootVer;
                Must(effectiveVer >= 1, $"[{file}:{id}] content_version missing/0 (item or file root)", errs);
            }

            // -------- SOURCES --------
            if (File.Exists(sourcesPath))
            {
                var doc = JObject.Parse(File.ReadAllText(sourcesPath));
                int rootVer = doc.Value<int?>("content_version") ?? 0;
                foreach (JObject s in (JArray?)doc["items"] ?? new JArray())
                {
                    CheckHeader(s, "source", rootVer, errors);
                    var id = s.Value<string>("id") ?? "unknown";
                    sourceIds.Add(id);

                    // type
                    var type = s.Value<string>("type");
                    var validTypes = new[] { "Primary", "Official", "Secondary", "Rumor", "Anonymous" };
                    Must(validTypes.Contains(type ?? ""), $"[source:{id}] type invalid '{type}'", errors);

                    // weight
                    var w = s.Value<int?>("weight");
                    Must(w.HasValue && w.Value >= 1 && w.Value <= 5, $"[source:{id}] weight must be 1..5", errors);

                    // supports_news_ids
                    foreach (var nidTok in (JArray?)s["supports_news_ids"] ?? new JArray())
                    {
                        var nid = SafeTok(nidTok);
                        if (!NonEmpty(nid)) errors.Add($"[source:{id}] supports_news_ids has null/empty");
                        else newsIds.Add(nid); // will check back later
                    }

                    // conflicts_news_ids
                    foreach (var nidTok in (JArray?)s["conflicts_news_ids"] ?? new JArray())
                    {
                        var nid = SafeTok(nidTok);
                        if (!NonEmpty(nid)) errors.Add($"[source:{id}] conflicts_news_ids has null/empty");
                        else newsIds.Add(nid); // will check back later
                    }
                }
            }

            // -------- NEWS --------
            if (File.Exists(newsPath))
            {
                var doc = JObject.Parse(File.ReadAllText(newsPath));
                int rootVer = doc.Value<int?>("content_version") ?? 0;
                var items = (JArray?)doc["items"] ?? new JArray();
                foreach (JObject n in items)
                {
                    CheckHeader(n, "news", rootVer, errors);
                    var id = n.Value<string>("id") ?? "unknown";
                    newsIds.Add(id);

                    // tone_allowed / tone_variants
                    var allowed = (JArray?)n["tone_allowed"] ?? new JArray();
                    Must(allowed.Count >= 1 && allowed.Count <= 3, $"[news:{id}] tone_allowed must have 1..3 items", errors);

                    var tv = (JObject?)n["tone_variants"];
                    Must(tv != null, $"[news:{id}] tone_variants missing", errors);
                    bool Need(string tone) => allowed.Any(t => string.Equals((string?)t, tone, StringComparison.OrdinalIgnoreCase));
                    void CheckTone(string tone)
                    {
                        if (!Need(tone)) return;
                        var node = tv![tone] as JObject;
                        Must(node != null, $"[news:{id}] tone_variants missing '{tone}'", errors);
                        if (node != null)
                        {
                            Must(node["headline"] != null, $"[news:{id}] {tone}.headline missing", errors);
                            Must(node["short"]    != null, $"[news:{id}] {tone}.short missing", errors);
                        }
                    }
                    CheckTone("Supportive"); CheckTone("Neutral"); CheckTone("Critical");

                    // sources block
                    var sourcesBlock = (JObject?)n["sources"];
                    if (sourcesBlock != null)
                    {
                        foreach (var supTok in (JArray?)sourcesBlock["supports"] ?? new JArray())
                        {
                            var sid = SafeTok(supTok);
                            if (!NonEmpty(sid)) errors.Add($"[news:{id}] sources.supports has null/empty");
                            else if (!sourceIds.Contains(sid)) errors.Add($"[news:{id}] sources.supports references missing source '{sid}'");
                        }

                        foreach (var conTok in (JArray?)sourcesBlock["conflicts"] ?? new JArray())
                        {
                            var sid = SafeTok(conTok);
                            if (!NonEmpty(sid)) errors.Add($"[news:{id}] sources.conflicts has null/empty");
                            else if (!sourceIds.Contains(sid)) errors.Add($"[news:{id}] sources.conflicts references missing source '{sid}'");
                        }

                        var minToPub = sourcesBlock.Value<int?>("min_to_publish") ?? 1;
                        Must(minToPub >= 0 && minToPub <= 5, $"[news:{id}] sources.min_to_publish must be 0..5", errors);
                    }
                }
            }

            // -------- LEADS --------
            if (File.Exists(leadsPath))
            {
                var doc = JObject.Parse(File.ReadAllText(leadsPath));
                int rootVer = doc.Value<int?>("content_version") ?? 0;

                foreach (JObject l in (JArray?)doc["items"] ?? new JArray())
                {
                    CheckHeader(l, "lead", rootVer, errors);
                    var id = l.Value<string>("id") ?? "unknown";
                    leadIds.Add(id);

                    Must(l["title"] != null, $"[lead:{id}] title missing", errors);
                    Must(l["expose_text"] != null, $"[lead:{id}] expose_text missing", errors);

                    var diff = l.Value<int?>("difficulty");
                    var diffOk = diff.HasValue && diff.Value >= 1 && diff.Value <= 5;
                    Must(diffOk, $"[lead:{id}] difficulty must be 1..5", errors);

                    // --- evidence_requirement { min_total(0..3), allow[Witness|Document|Object]+ } ---
                    var er = l["evidence_requirement"] as JObject;
                    Must(er != null, $"[lead:{id}] evidence_requirement missing", errors);

                    if (er != null)
                    {
                        var minTotal = er.Value<int?>("min_total") ?? -1;
                        Must(minTotal >= 0 && minTotal <= 3, $"[lead:{id}] evidence_requirement.min_total must be 0..3", errors);

                        var allow = er["allow"] as JArray ?? new JArray();
                        Must(allow.Count >= 1, $"[lead:{id}] evidence_requirement.allow must have at least 1 item", errors);

                        var validEvidence = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "Witness", "Document", "Object" };
                        foreach (var a in allow)
                        {
                            var val = SafeTok(a);
                            Must(NonEmpty(val), $"[lead:{id}] evidence_requirement.allow has null/empty item", errors);
                            if (NonEmpty(val) && !validEvidence.Contains(val))
                                errors.Add($"[lead:{id}] evidence_requirement.allow invalid type '{val}' (use Witness|Document|Object)");
                        }

                        // Optional sanity warning (not an error): min_total > number of allowed types
                        if (minTotal > allow.Count)
                            Debug.LogWarning($"[lead:{id}] evidence_requirement.min_total({minTotal}) > allow.Count({allow.Count}). Is that intentional?");
                    }

                    // personas[] — optional; if present, must be valid + exist
                    foreach (var pTok in (JArray?)l["personas"] ?? new JArray())
                    {
                        var pid = SafeTok(pTok);
                        if (!NonEmpty(pid)) { errors.Add($"[lead:{id}] personas has null/empty entry"); continue; }
                        Must(IdPattern.IsMatch(pid), $"[lead:{id}] personas contains bad id '{pid}'", errors);
                        if (!personaIds.Contains(pid))
                            errors.Add($"[lead:{id}] personas references missing persona '{pid}'");
                    }

                    // on_expose_effects[] — ensure 'type' matches allowed set from common.schema
                    var effArr = (JArray?)l["on_expose_effects"] ?? new JArray();
                    var validEffectTypes = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "FlagSet","FlagClear","MoodDelta","FactionMoodDelta","CredibilityDelta","SuspicionDelta",
            "SpawnEvent","SpawnNews","AddLead","RemoveLead","ScheduleMeeting","AddEvidence","Arrest",
            "Fine","UnlockLayout","AddMemory","ChangeRegimePressure","ChangeFactionRelation"
        };
                    foreach (var eTok in effArr)
                    {
                        var eo = eTok as JObject;
                        Must(eo != null, $"[lead:{id}] on_expose_effects contains non-object item", errors);
                        if (eo == null) continue;

                        var typeStr = eo.Value<string>("type") ?? "";
                        Must(NonEmpty(typeStr), $"[lead:{id}] on_expose_effects item missing 'type'", errors);
                        if (NonEmpty(typeStr) && !validEffectTypes.Contains(typeStr))
                            errors.Add($"[lead:{id}] on_expose_effects invalid type '{typeStr}'");
                    }
                }
            }


            // -------- EVENTS --------
            if (File.Exists(eventsPath))
            {
                var doc = JObject.Parse(File.ReadAllText(eventsPath));
                int rootVer = doc.Value<int?>("content_version") ?? 0;
                foreach (JObject e in (JArray?)doc["items"] ?? new JArray())
                {
                    CheckHeader(e, "event", rootVer, errors);
                    var id = e.Value<string>("id") ?? "unknown";
                    eventIds.Add(id);

                    var trigger = e.Value<string>("trigger");
                    Must(!string.IsNullOrWhiteSpace(trigger), $"[event:{id}] trigger missing", errors);
                }
            }

            // -------- PERSONAS --------
            if (File.Exists(personasPath))
            {
                var doc = JObject.Parse(File.ReadAllText(personasPath));
                int rootVer = doc.Value<int?>("content_version") ?? 0;
                foreach (JObject p in (JArray?)doc["items"] ?? new JArray())
                {
                    CheckHeader(p, "persona", rootVer, errors);
                    var id = p.Value<string>("id") ?? "unknown";
                    personaIds.Add(id);
                }
            }

            // -------- FACTIONS --------
            if (File.Exists(factionsPath))
            {
                var doc = JObject.Parse(File.ReadAllText(factionsPath));
                int rootVer = doc.Value<int?>("content_version") ?? 0;
                foreach (JObject f in (JArray?)doc["items"] ?? new JArray())
                {
                    CheckHeader(f, "faction", rootVer, errors);
                    var id = f.Value<string>("id") ?? "unknown";
                    factionIds.Add(id);
                }
            }

            // -------- LOCATIONS --------
            if (File.Exists(locPath))
            {
                var doc = JObject.Parse(File.ReadAllText(locPath));
                int rootVer = doc.Value<int?>("content_version") ?? 0;
                foreach (JObject l in (JArray?)doc["items"] ?? new JArray())
                {
                    CheckHeader(l, "location", rootVer, errors);
                    var id = l.Value<string>("id") ?? "unknown";
                    locationIds.Add(id);
                }
            }

            // -------- STATE --------
            if (File.Exists(statePath))
            {
                var doc = JObject.Parse(File.ReadAllText(statePath));
                int rootVer = doc.Value<int?>("content_version") ?? 0;
                foreach (JObject s in (JArray?)doc["items"] ?? new JArray())
                {
                    CheckHeader(s, "state", rootVer, errors);
                    var id = s.Value<string>("id") ?? "unknown";
                    stateIds.Add(id);
                }
            }

            // -------- TRIALS --------
            if (File.Exists(trialPath))
            {
                var doc = JObject.Parse(File.ReadAllText(trialPath));
                int rootVer = doc.Value<int?>("content_version") ?? 0;
                foreach (JObject t in (JArray?)doc["items"] ?? new JArray())
                {
                    CheckHeader(t, "trial", rootVer, errors);
                    var id = t.Value<string>("id") ?? "unknown";
                    trialIds.Add(id);
                }
            }

            // -------- SCENARIO --------
            if (File.Exists(scenarioPath))
            {
                var scen = JObject.Parse(File.ReadAllText(scenarioPath));
                int rootVer = scen.Value<int?>("content_version") ?? 0;

                var items = (JArray?)scen["items"] ?? new JArray();
                foreach (JObject s in items)
                {
                    CheckHeader(s, "scenario", rootVer, errors);
                    var id = s.Value<string>("id") ?? "unknown";

                    // include_ids xrefs
                    var inc = (JObject?)s["include_ids"] ?? new JObject();
                    void CheckArray(string prop, HashSet<string> set)
                    {
                        foreach (var tok in (JArray?)inc[prop] ?? new JArray())
                        {
                            var val = SafeTok(tok);
                            if (!NonEmpty(val))
                            {
                                errors.Add($"[scenario:{id}] include_ids.{prop} has null/empty entry");
                                continue;
                            }
                            if (!set.Contains(val))
                                errors.Add($"[scenario:{id}] include_ids.{prop} references missing id '{val}'");
                        }
                    }

                    CheckArray("news", newsIds);
                    CheckArray("leads", leadIds);
                    CheckArray("events", eventIds);
                    CheckArray("personas", personaIds);
                    CheckArray("factions", factionIds);
                    CheckArray("locations", locationIds);
                    CheckArray("trials", trialIds);

                    // regime_state_id must exist in state set
                    var regime = s.Value<string>("regime_state_id");
                    if (!string.IsNullOrWhiteSpace(regime))
                        Must(stateIds.Contains(regime), $"[scenario:{id}] regime_state_id '{regime}' not found in state.json", errors);
                }
            }

            if (errors.Count > 0)
                throw new Exception("Content schema validation failed:\n" + string.Join("\n", errors));

            Debug.Log("[Schema] All content OK.");
        }
    }
}
#endif
