#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System.Linq;
using T4E.Infrastructure.Content;
using T4E.App.Abstractions.Dtos;

namespace T4E.Tools.Editor.Content
{
    public static class TestSourcesRepo
    {
        [MenuItem("T4E/Debug/Test Sources Repository")]
        public static void RunTest()
        {
            // Adjust path to your real file
            string path = "Assets/The Fourth Estate/Infrastructure/Content/sources.json";

            try
            {
                var repo = new JsonSourcesRepository(path);
                var all = repo.LoadAll<SourceDto>().ToList();

                Debug.Log($"[SourcesRepo] Loaded {all.Count} sources.");

                foreach (var s in all)
                {
                    string supports = s.SupportsNewsIds.Count > 0
                        ? string.Join(", ", s.SupportsNewsIds)
                        : "(none)";

                    string conflicts = s.ConflictsNewsIds != null && s.ConflictsNewsIds.Count > 0
                        ? string.Join(", ", s.ConflictsNewsIds)
                        : "(none)";

                    Debug.Log(
                        $" - {s.Id} | {s.Type} | w:{s.Weight} " +
                        $"supports:[{supports}] conflicts:[{conflicts}] ref:{s.ContentRef}"
                    );
                }

                // Direct lookup test
                var sampleId = all.FirstOrDefault()?.Id;
                if (sampleId != null)
                {
                    var single = repo.Load<SourceDto>(sampleId);
                    Debug.Log($"[SourcesRepo] Single load OK: {single?.Id}");
                }

                EditorUtility.DisplayDialog("Sources Repository", "All tests passed. Check Console for details.", "OK");
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[SourcesRepo] Test failed:\n{ex}");
                EditorUtility.DisplayDialog("Sources Repository", $"Test failed:\n{ex.Message}", "OK");
            }
        }
    }
}
#endif
