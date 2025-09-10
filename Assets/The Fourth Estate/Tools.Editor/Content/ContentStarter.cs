#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEngine;

namespace TheFourthEstate.Tools.Editor.Content
{
    public static class ContentStarter
    {
        // Where we’ll write the initial content JSONs
        private const string OutDir = "Assets/The Fourth Estate/Infrastructure/Content";

        [MenuItem("T4E/Content/Create Starter Pack")]
        public static void CreateStarterPack()
        {
            Directory.CreateDirectory(OutDir);

            void WriteIfMissing(string file, string json)
            {
                var path = Path.Combine(OutDir, file);
                if (File.Exists(path))
                {
                    Debug.Log($"[Starter] Skipped (exists): {path}");
                    return;
                }
                File.WriteAllText(path, json.Trim() + "\n");
                Debug.Log($"[Starter] Wrote: {path}");
            }

            // Minimal, valid content_version=1 for each type
            WriteIfMissing("news.json", @"
{
  ""content_version"": 1,
  ""items"": [
    {
      ""id"": ""vic.news.demo_001"",
      ""era"": ""victorian"",
      ""subject"": ""Demonstration in the Square"",
      ""tags"": [""politics""],
      ""tone_allowed"": [""Supportive"", ""Neutral"", ""Critical""],
      ""tone_variants"": {
        ""Supportive"": { ""headline"": ""Citizens Rally for Reform"", ""short"": ""Large peaceful march."", ""effects"": [] },
        ""Neutral"":    { ""headline"": ""Crowds Gather Downtown"",   ""short"": ""Demonstration reported."", ""effects"": [] },
        ""Critical"":   { ""headline"": ""Unrest in the Capital"",    ""short"": ""Authorities uneasy."", ""effects"": [] }
      },
      ""body_generic"": ""A sizeable crowd gathered in the capital's main square..."",
      ""personas_involved"": [],
      ""source"": { ""type"": ""Anonymous"" },
      ""flags"": []
    }
  ]
}
");

            WriteIfMissing("leads.json", @"
{
  ""content_version"": 1,
  ""items"": [
    {
      ""id"": ""vic.lead.mill_003"",
      ""era"": ""victorian"",
      ""title"": ""Whispers from the Mill"",
      ""expose_text"": ""A pattern of dangerous neglect emerges."",
      ""difficulty"": 2,
      ""personas"": [],
      ""evidence_requirement"": { ""min_total"": 2, ""allow"": [""witness"", ""document""] },
      ""spawn_via_trigger_id"": ""vic.event.demo_002"",
      ""on_expose_effects"": []
    }
  ]
}
");

            WriteIfMissing("events.json", @"
{
  ""content_version"": 1,
  ""items"": [
    {
      ""id"": ""vic.event.demo_001"",
      ""era"": ""victorian"",
      ""trigger"": ""segment"",
      ""schedule"": { ""dayOfWeek"": ""Tuesday"", ""segment"": ""Morning"" },
      ""effects"": [],
      ""spawns"": { ""news_ids"": [""vic.news.demo_001""], ""lead_ids"": [], ""narrative_ids"": [] }
    },
    {
      ""id"": ""vic.event.demo_002"",
      ""era"": ""victorian"",
      ""trigger"": ""segment"",
      ""schedule"": { ""dayOfWeek"": ""Sunday"", ""segment"": ""Evening"" },
      ""effects"": [],
      ""spawns"": { ""news_ids"": [], ""lead_ids"": [""vic.lead.mill_003""], ""narrative_ids"": [] }
    }
  ]
}
");

            WriteIfMissing("personas.json", @"
{
  ""content_version"": 1,
  ""items"": [
    {
      ""id"": ""vic.persona.editor_001"",
      ""era"": ""victorian"",
      ""name"": ""The Editor"",
      ""role"": ""editor"",
      ""faction_id"": ""vic.faction.liberals"",
      ""stats"": { ""power"": 20, ""influence"": 40, ""suspicion"": 10 },
      ""traits"": [],
      ""opinion_modifiers"": [],
      ""relations"": {}
    }
  ]
}
");

            WriteIfMissing("factions.json", @"
{
  ""content_version"": 1,
  ""items"": [
    {
      ""id"": ""vic.faction.liberals"",
      ""era"": ""victorian"",
      ""ideology_tags"": [""reform""],
      ""stats"": { ""influence"": 50, ""hostility_to_press"": 10, ""stability"": 60 },
      ""mood_bar"": 0,
      ""suspicion_of_player"": 0
    }
  ]
}
");

            WriteIfMissing("locations.json", @"
{
  ""content_version"": 1,
  ""items"": [
    {
      ""id"": ""vic.location.city_square"",
      ""era"": ""victorian"",
      ""name"": ""City Square"",
      ""type"": ""public"",
      ""tags"": [""gatherings""]
    }
  ]
}
");

            WriteIfMissing("state.json", @"
{
  ""content_version"": 1,
  ""items"": [
    {
      ""id"": ""vic.state.const_monarchy"",
      ""era"": ""victorian"",
      ""type"": ""Constitutional Monarchy"",
      ""institutions"": [""Parliament"", ""Crown"", ""Courts""],
      ""base_laws"": [""Press Law 1850""],
      ""press_rules"": ""moderate"",
      ""police_pressure_curve"": ""medium""
    }
  ]
}
");

            WriteIfMissing("trials.json", @"
{
  ""content_version"": 1,
  ""items"": [
    {
      ""id"": ""vic.trial.minor_sedition_001"",
      ""era"": ""victorian"",
      ""title"": ""Hearing on Sedition"",
      ""steps"": [""arraignment"", ""hearing"", ""verdict""],
      ""verdict_effects"": []
    }
  ]
}
");

            WriteIfMissing("scenario.json", @"
{
  ""content_version"": 1,
  ""items"": [
    {
      ""id"": ""vic.scenario.demo_start"",
      ""era"": ""victorian"",
      ""title"": ""Demo Scenario"",
      ""regime_state_id"": ""vic.state.const_monarchy"",
      ""include_ids"": {
        ""news"":     [""vic.news.demo_001""],
        ""leads"":    [""vic.lead.mill_003""],
        ""events"":   [""vic.event.demo_001"", ""vic.event.demo_002""],
        ""personas"": [""vic.persona.editor_001""],
        ""factions"": [""vic.faction.liberals""],
        ""locations"": [""vic.location.city_square""],
        ""trials"":   [""vic.trial.minor_sedition_001""]
      }
    }
  ]
}
");

            AssetDatabase.Refresh();
            EditorUtility.DisplayDialog("Content Starter", "Starter pack created (or already present).", "OK");
        }
    }
}
#endif
