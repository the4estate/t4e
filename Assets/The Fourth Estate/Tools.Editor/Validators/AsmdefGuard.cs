#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace T4E.Tools.Editor.Validators
{
    [Serializable]
    class AsmdefJson
    {
        public string name;
        public string[] references;
        public bool noEngineReferences;
        public bool allowUnsafeCode;
    }

    public static class AsmdefGuard
    {
        // Menu for quick checks
        [MenuItem("T4E/Validators/Asmdef Guard")]
        public static void RunMenu()
        {
            try { Run(); EditorUtility.DisplayDialog("Asmdef Guard", "All good.", "OK"); }
            catch (Exception ex) { Debug.LogError(ex.Message); throw; }
        }

        // Call from RunAll (headless)
        public static void Run()
        {
            var assets = Application.dataPath.Replace('\\','/');
            var asmdefs = Directory.GetFiles(assets, "*.asmdef", SearchOption.AllDirectories);

            var violations = new List<string>();

            foreach (var path in asmdefs)
            {
                var json = File.ReadAllText(path);
                var data = JsonUtility.FromJson<AsmdefJson>(json) ?? new AsmdefJson();
                var norm = path.Replace('\\','/');

                // classify by folder
                var isDomain        = norm.Contains("/The Fourth Estate/Domain/") || norm.Contains("/T4E.Domain/");
                var isApp           = norm.Contains("/The Fourth Estate/Application/") || norm.Contains("/T4E.App.");
                var isInfrastructure= norm.Contains("/The Fourth Estate/Infrastructure/") || norm.Contains("/T4E.Infrastructure");
                var isPresentation  = norm.Contains("/The Fourth Estate/Presentation/") || norm.Contains("/T4E.Presentation");
                var isToolsEditor   = norm.Contains("/The Fourth Estate/Tools.Editor/") || norm.Contains("/Tools.Editor/");
                var isTests         = norm.Contains("/Tests/");

                // read refs (by-name; GUID refs arrive as 'GUID:...' which we leave alone)
                var refs = (data.references ?? Array.Empty<string>())
                    .Select(r => r.Split(new[] {"::"}, StringSplitOptions.None).Last())
                    .ToArray();

                // 1) Domain/App must be noEngineReferences = true
                if ((isDomain || isApp) && !data.noEngineReferences)
                    violations.Add($"{Rel(norm)}  -> must set \"noEngineReferences\": true");

                // 2) Layer arrows
                if (isDomain)
                {
                    // Domain should not reference any T4E.* except Domain itself
                    if (refs.Any(r => r.StartsWith("T4E.", StringComparison.OrdinalIgnoreCase) &&
                                      !r.StartsWith("T4E.Domain", StringComparison.OrdinalIgnoreCase)))
                        violations.Add($"{Rel(norm)}  -> Domain cannot reference {string.Join(", ", refs)}");
                }
                else if (isApp)
                {
                    // App can reference Domain + App.Abstractions + App.UseCases (self)
                    bool Bad(string r) =>
                        r.StartsWith("T4E.Infrastructure", StringComparison.OrdinalIgnoreCase) ||
                        r.StartsWith("T4E.Presentation",   StringComparison.OrdinalIgnoreCase);
                    var bad = refs.Where(Bad).ToArray();
                    if (bad.Length > 0) violations.Add($"{Rel(norm)}  -> Application cannot reference: {string.Join(", ", bad)}");
                }
                else if (isInfrastructure || isPresentation || isToolsEditor || isTests)
                {
                    
                }
            }

            if (violations.Count > 0)
            {
                var msg = "Asmdef guard violations:\n" + string.Join("\n", violations);
                throw new Exception(msg);
            }
        }

        static string Rel(string abs)
        {
            var proj = Path.GetDirectoryName(Application.dataPath)!.Replace('\\','/');
            return abs.Replace(proj + "/", "");
        }
    }
}
#endif
