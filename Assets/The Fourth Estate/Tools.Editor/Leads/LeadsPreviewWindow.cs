#if UNITY_EDITOR
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

// T4E deps
using T4E.App.Abstractions.Ports;
using T4E.App.Abstractions.Dtos;
using T4E.App.UseCases.Leads;
using T4E.Domain.Core.Leads;
using T4E.Domain.Core.CET;
using T4E.App.UseCases;

using T4E.Infrastructure;               // InMemoryWorld
using T4E.Infrastructure.Content;
using T4E.Infrastructure.Systems;      // JsonLeadsRepository

/// <summary>
/// Designer utility to preview and simulate the Leads loop (collect → expose).
/// Runs entirely in the Editor. Uses an in-memory world so it won't touch saves.
/// </summary>
public sealed class LeadsPreviewWindow : EditorWindow
{
    private const string DefaultLeadsPath = "Assets/The Fourth Estate/Infrastructure/Content/leads.json";

    // Content / selection
    private string _leadsJsonPath = DefaultLeadsPath;
    private JsonLeadsRepository _repo;
    private List<LeadDto> _items = new();
    private int _selectedIndex = -1;

    // Mini "runtime" for preview
    private InMemoryWorld _world;
    private AppLogger _log;
    private MemoryLog _memory;
    private EffectApplier _effect;
    private CollectEvidence _collect;
    private ExposeLead _expose;


    // UI input for collecting evidence
    private int _evidenceTypeIndex = 0; // 0=Witness,1=Document,2=Object
    private string _evidenceId = "debug.sample_001";

    // Last effects applied (preview feedback)
    private readonly List<EffectInvocation> _lastApplied = new();

    [MenuItem("T4E/Preview/Leads…")]
    private static void Open()
    {
        var win = GetWindow<LeadsPreviewWindow>("Leads Preview");
        win.minSize = new Vector2(640, 520);
        win.RefreshAll();
    }

    private void OnEnable() => RefreshAll();

    private void RefreshAll()
    {
        // Load repo
        try
        {
            _repo = new JsonLeadsRepository(_leadsJsonPath);
            // NOTE: your leads repo exposes Get(id); for listing, re-read file quickly here
            _items = LoadAllLeads(_leadsJsonPath);
            if (_items.Count > 0 && (_selectedIndex < 0 || _selectedIndex >= _items.Count))
                _selectedIndex = 0;
        }
        catch
        {
            _items = new List<LeadDto>();
            _selectedIndex = -1;
        }

        // Reset mini-world for deterministic preview
        _world = new InMemoryWorld();
        _log = new AppLogger();
        _effect = new EffectApplier(_world, _log);

        // Wire use cases (world doubles as ILeadProgressStore)
        _collect = new CollectEvidence(_repo, _world, _effect, _memory, _log);
        _expose  = new ExposeLead(_repo, _world, _effect, _memory, _log);

        _lastApplied.Clear();
        Repaint();
    }

    private List<LeadDto> LoadAllLeads(string path)
    {
        // Re-parse quickly to get a list (your repo is id->dto oriented)
        // This mirrors JsonLeadsRepository logic enough to present a list.
        // If you later add a LoadAll() API to the repo, switch to that.
        try
        {
            var tmp = new JsonLeadsRepository(path);
            var result = new List<LeadDto>();

            // Small hack: we can’t enumerate repo internals, so re-read file here:
            var text = System.IO.File.ReadAllText(path);
            var root = Newtonsoft.Json.Linq.JObject.Parse(text);
            var items = (Newtonsoft.Json.Linq.JArray?)root["items"] ?? new Newtonsoft.Json.Linq.JArray();
            foreach (var t in items)
            {
                var id = (string)((Newtonsoft.Json.Linq.JObject)t)["id"];
                if (!string.IsNullOrWhiteSpace(id) && tmp.TryGet(id, out var dto))
                    result.Add(dto);
            }
            return result.OrderBy(x => x.Id).ToList();
        }
        catch
        {
            return new List<LeadDto>();
        }
    }

    private void OnGUI()
    {
        EditorGUILayout.Space(6);
        EditorGUILayout.LabelField("Leads Preview (simulated)", EditorStyles.boldLabel);

        // Path + reload
        EditorGUILayout.BeginHorizontal();
        _leadsJsonPath = EditorGUILayout.TextField("leads.json path", _leadsJsonPath);
        if (GUILayout.Button("Reload", GUILayout.Width(80))) RefreshAll();
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space(6);

        if (_items.Count == 0)
        {
            EditorGUILayout.HelpBox("No leads loaded. Check the json path and Reload.", MessageType.Info);
            if (GUILayout.Button("Use default path")) { _leadsJsonPath = DefaultLeadsPath; RefreshAll(); }
            return;
        }

        EditorGUILayout.BeginHorizontal();

        // LEFT: list of lead IDs
        using (new EditorGUILayout.VerticalScope(GUILayout.Width(260)))
        {
            EditorGUILayout.LabelField($"Items: {_items.Count}", EditorStyles.miniBoldLabel);
            var listView = EditorGUILayout.BeginScrollView(new Vector2(), GUILayout.Width(260),
                GUILayout.Height(position.height - 80));
            for (int i = 0; i < _items.Count; i++)
            {
                var id = _items[i].Id;
                var sel = (i == _selectedIndex);
                if (GUILayout.Toggle(sel, id, "Button"))
                    _selectedIndex = i;
            }
            EditorGUILayout.EndScrollView();
        }

        // RIGHT: details & simulation controls
        using (new EditorGUILayout.VerticalScope())
        {
            if (_selectedIndex < 0 || _selectedIndex >= _items.Count)
            {
                EditorGUILayout.HelpBox("Select a lead on the left.", MessageType.Info);
            }
            else
            {
                var lead = _items[_selectedIndex];

                // Header
                EditorGUILayout.LabelField(lead.Id, EditorStyles.boldLabel);
                EditorGUILayout.LabelField("Era:", string.IsNullOrEmpty(lead.Era) ? "-" : lead.Era);
                EditorGUILayout.LabelField("Difficulty:", lead.Difficulty.ToString());

                // Personas
                if (lead.Personas != null && lead.Personas.Count > 0)
                {
                    EditorGUILayout.LabelField("Personas:", EditorStyles.miniBoldLabel);
                    foreach (var p in lead.Personas)
                        EditorGUILayout.LabelField("• " + p);
                }

                EditorGUILayout.Space(4);

                // Evidence policy
                var allow = lead.EvidenceRequirement?.Allow ?? new List<string>();
                var min = lead.EvidenceRequirement?.MinTotal ?? 0;

                EditorGUILayout.LabelField("Evidence Requirement", EditorStyles.boldLabel);
                EditorGUILayout.LabelField($"Min total: {min}");
                EditorGUILayout.LabelField("Allowed types:");
                if (allow.Count == 0)
                    EditorGUILayout.HelpBox("(none) — check your content or validator.", MessageType.Warning);
                else
                    EditorGUILayout.LabelField("• " + string.Join(", ", allow));

                EditorGUILayout.Space(4);

                // Current progress (from world store)
                LeadProgressState state;
                bool hasState = ((ILeadProgressStore)_world).TryGet(lead.Id, out state);
                LeadProgress lp = hasState
                    ? LeadProgress.FromState(state)
                    : new LeadProgress(lead.Id, allow, min);

                EditorGUILayout.LabelField("Progress", EditorStyles.boldLabel);
                EditorGUILayout.LabelField($"Collected: {lp.Collected.Count(e => lp.Allow.Contains(e.Type))} / {lp.MinTotal}");
                EditorGUILayout.LabelField($"State: {lp.State}");

                if (lp.Collected.Any())
                {
                    EditorGUILayout.LabelField("Collected items:");
                    foreach (var ev in lp.Collected)
                        EditorGUILayout.LabelField($"• {ev.Type}:{ev.Id}");
                }

                EditorGUILayout.Space(8);

                // Collect evidence controls
                EditorGUILayout.LabelField("Collect Evidence", EditorStyles.boldLabel);
                string[] types = new[] { "Witness", "Document", "Object" };
                _evidenceTypeIndex = GUILayout.Toolbar(_evidenceTypeIndex, types);
                _evidenceId = EditorGUILayout.TextField("Evidence Id", _evidenceId);

                using (new EditorGUILayout.HorizontalScope())
                {
                    GUI.enabled = allow.Count > 0;
                    if (GUILayout.Button("Collect"))
                    {
                        var chosenType = types[Mathf.Clamp(_evidenceTypeIndex, 0, 2)];
                        var cmd = new CollectEvidence.Command(lead.Id, chosenType, _evidenceId);
                        var res = _collect.Execute(cmd);
                        Debug.Log($"[LeadsPreview] Collect => added={res.Added} state={res.State}");
                    }
                    GUI.enabled = true;

                    if (GUILayout.Button("Reset Lead Progress"))
                    {
                        // Simple reset: re-create the window's world
                        RefreshAll();
                    }
                }

                EditorGUILayout.Space(6);

                // Expose controls
                EditorGUILayout.LabelField("Expose", EditorStyles.boldLabel);
                EditorGUILayout.HelpBox(lead.ExposeText ?? "", MessageType.None);

                if (GUILayout.Button("Expose Lead"))
                {
                    try
                    {
                        // Wrap a tiny effect listener to show feedback
                        _lastApplied.Clear();
                        // EffectApplier in your project doesn't expose a hook to capture,
                        // so we infer success by not throwing and by world side-effects.

                        var r = _expose.Execute(lead.Id);
                        Debug.Log($"[LeadsPreview] EXPOSÉ: {r.Title}\n{r.ExposeText}");
                    }
                    catch (System.SystemException ex)
                    {
                        Debug.LogWarning(ex.Message);
                    }
                }

                EditorGUILayout.Space(6);
                EditorGUILayout.HelpBox("Preview uses an isolated in-editor world. It won't affect runtime saves.", MessageType.Info);
            }
        }

        EditorGUILayout.EndHorizontal();
    }
}
#endif
