#if UNITY_EDITOR
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using T4E.App.Abstractions.Dtos;     // NewsDto, Tone
using T4E.Infrastructure.Content;    // JsonNewsRepository

public sealed class NewsPreviewWindow : EditorWindow
{
    // Default path to your authored news.json
    private const string DefaultNewsPath = "Assets/The Fourth Estate/Infrastructure/Content/news.json";

    private string _newsJsonPath = DefaultNewsPath;
    private JsonNewsRepository _repo;
    private List<NewsDto> _items = new();
    private int _selectedIndex = -1;
    private int _toneIndex = 1; // 0=Supportive, 1=Neutral, 2=Critical

    [MenuItem("T4E/Preview/News…")]
    private static void Open()
    {
        var win = GetWindow<NewsPreviewWindow>("News Preview");
        win.minSize = new Vector2(520, 420);
        win.Refresh();
    }

    private void OnEnable() => Refresh();

    private void Refresh()
    {
        try
        {
            _repo = new JsonNewsRepository(_newsJsonPath);
            _items = _repo.LoadAll<NewsDto>().ToList();
            if (_items.Count > 0 && (_selectedIndex < 0 || _selectedIndex >= _items.Count))
                _selectedIndex = 0;
        }
        catch
        {
            _items = new List<NewsDto>();
            _selectedIndex = -1;
        }
        Repaint();
    }

    private void OnGUI()
    {
        EditorGUILayout.Space(6);
        EditorGUILayout.LabelField("News Preview (read-only)", EditorStyles.boldLabel);

        // Path row
        EditorGUILayout.BeginHorizontal();
        _newsJsonPath = EditorGUILayout.TextField("news.json path", _newsJsonPath);
        if (GUILayout.Button("Reload", GUILayout.Width(80))) Refresh();
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space(6);

        if (_items.Count == 0)
        {
            EditorGUILayout.HelpBox("No news loaded. Check the json path and Reload.", MessageType.Info);
            if (GUILayout.Button("Use default path")) { _newsJsonPath = DefaultNewsPath; Refresh(); }
            return;
        }

        // Left: list of news IDs; Right: preview of tones
        EditorGUILayout.BeginHorizontal();

        // LEFT: list
        using (new EditorGUILayout.VerticalScope(GUILayout.Width(220)))
        {
            EditorGUILayout.LabelField($"Items: {_items.Count}", EditorStyles.miniBoldLabel);
            var view = EditorGUILayout.BeginScrollView(new Vector2(), GUILayout.Width(220), GUILayout.Height(position.height - 80));
            for (int i = 0; i < _items.Count; i++)
            {
                var id = _items[i].Id;
                var isSel = i == _selectedIndex;
                if (GUILayout.Toggle(isSel, id, "Button"))
                    _selectedIndex = i;
            }
            EditorGUILayout.EndScrollView();
        }

        // RIGHT: details
        using (new EditorGUILayout.VerticalScope())
        {
            if (_selectedIndex >= 0 && _selectedIndex < _items.Count)
            {
                var n = _items[_selectedIndex];

                EditorGUILayout.LabelField(n.Id, EditorStyles.boldLabel);
                EditorGUILayout.LabelField("Subject:", n.Subject);
                EditorGUILayout.LabelField("Era:", string.IsNullOrEmpty(n.Era) ? "-" : n.Era);

                // Tone selection
                string[] toneOptions = new[] { "Supportive", "Neutral", "Critical" };
                _toneIndex = GUILayout.Toolbar(_toneIndex, toneOptions);
                var toneKey = toneOptions[_toneIndex];

                // Variant
                if (n.ToneVariants != null && n.ToneVariants.TryGetValue(toneKey, out var v) && v != null)
                {
                    EditorGUILayout.Space(4);
                    EditorGUILayout.LabelField("Headline", EditorStyles.miniBoldLabel);
                    EditorGUILayout.HelpBox(v.Headline ?? "", MessageType.None);

                    EditorGUILayout.LabelField("Short", EditorStyles.miniBoldLabel);
                    EditorGUILayout.HelpBox(v.Short ?? "", MessageType.None);

                    EditorGUILayout.LabelField("Body (generic)", EditorStyles.miniBoldLabel);
                    var body = string.IsNullOrEmpty(n.BodyGeneric) ? "(empty)" : n.BodyGeneric;
                    EditorGUILayout.TextArea(body, GUILayout.Height(120));

                    // Effects
                    // Effects
                    int effectCount = v.Effects != null ? v.Effects.Count : 0;
                    EditorGUILayout.Space(4);
                    EditorGUILayout.LabelField($"Effects: {effectCount}", EditorStyles.miniBoldLabel);
                    if (effectCount == 0)
                    {
                        EditorGUILayout.HelpBox("No effects for this tone.", MessageType.Info);
                    }
                    else
                    {
                        for (int i = 0; i < v.Effects.Count; i++)
                        {
                            var eff = v.Effects[i];
                            EditorGUILayout.LabelField($"• {eff.Kind} (#{i+1})");
                        }
                    }


                    EditorGUILayout.Space(4);
                    using (new EditorGUILayout.HorizontalScope())
                    {
                        if (GUILayout.Button("Copy Headline")) EditorGUIUtility.systemCopyBuffer = v.Headline ?? "";
                        if (GUILayout.Button("Copy Short")) EditorGUIUtility.systemCopyBuffer = v.Short ?? "";
                        if (GUILayout.Button("Copy Body")) EditorGUIUtility.systemCopyBuffer = n.BodyGeneric ?? "";
                    }
                    // --- Sources section ---
                    if (n.Source != null)
                    {
                        EditorGUILayout.Space(6);
                        EditorGUILayout.LabelField("Sources", EditorStyles.boldLabel);

                        int supCount = n.Source.Supports?.Count ?? 0;
                        int conCount = n.Source.Conflicts?.Count ?? 0;
                        int minPub = n.Source.MinToPublish;

                        EditorGUILayout.LabelField($"Supports: {supCount}", EditorStyles.miniBoldLabel);
                        if (supCount > 0)
                            foreach (var sid in n.Source.Supports)
                                EditorGUILayout.LabelField($"• {sid}");

                        EditorGUILayout.LabelField($"Conflicts: {conCount}", EditorStyles.miniBoldLabel);
                        if (conCount > 0 && n.Source.Conflicts != null)
                            foreach (var sid in n.Source.Conflicts)
                                EditorGUILayout.LabelField($"• {sid}");

                        EditorGUILayout.LabelField($"Min to publish: {minPub}");

                        // Estimate potential credibility tier
                        string tier = "(Unknown)";
                        int totalWeight = supCount * 3; // assume average weight for preview
                        if (totalWeight <= 3) tier = "Weak";
                        else if (totalWeight <= 7) tier = "Solid";
                        else tier = "Corroborated";
                        EditorGUILayout.HelpBox($"Max potential tier (if all supports unlocked): {tier}", MessageType.None);
                    }
                    else
                    {
                        EditorGUILayout.HelpBox("No sources block in this news item.", MessageType.Info);
                    }

                    EditorGUILayout.HelpBox("This window is read-only preview. Actual publishing is done in Play Mode via PublishNews use case.", MessageType.None);
                    if (string.IsNullOrWhiteSpace(v.Headline) || string.IsNullOrWhiteSpace(v.Short))
                    {
                        EditorGUILayout.HelpBox(
                            $"Tone '{toneKey}' is allowed but missing Headline or Short.",
                            MessageType.Error);
                    }
                }
                else
                {
                    EditorGUILayout.HelpBox($"Tone variant '{toneKey}' missing for this item.", MessageType.Warning);
                }
 
            }
            else
            {
                EditorGUILayout.HelpBox("Select a news item on the left.", MessageType.Info);
            }

        }

        EditorGUILayout.EndHorizontal();
    }
}
#endif
