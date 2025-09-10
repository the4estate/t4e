#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace T4E.Tools.Editor.Validators
{
    /// <summary>
    /// Pre-build determinism guard. Fails the build if banned, non-deterministic APIs are used
    /// in the deterministic layers (Domain/App). You can:
    ///  - pass -skipDeterminism on the command line to skip
    ///  - or add scripting define SKIP_DETERMINISM_GUARDS
    /// </summary>
    public class DeterminismGuards : IPreprocessBuildWithReport
    {
        public int callbackOrder => 0;

        // Only scan these roots (relative to project root). 
        static readonly string[] ScanRoots =
        {
            "Assets/The Fourth Estate/Domain",
            "Assets/The Fourth Estate/Application",
            "Assets/T4E.Domain",
            "Assets/T4E.App.Abstractions",
            "Assets/T4E.App.UseCases",
        };

        // Files that are allowed to touch wall-clock / non-deterministic APIs.
        static readonly string[] AllowFiles =
        {
            "Assets/The Fourth Estate/Infrastructure/SystemClock.cs"
        };

        // Banned API patterns inside deterministic layers.
        static readonly Regex[] BannedPatterns =
        {
            // Wall-clock time
            new(@"\bDateTime\.Now\b", RegexOptions.Compiled),
            new(@"\bDateTime\.UtcNow\b", RegexOptions.Compiled),
            new(@"\bDateTimeOffset\.Now\b", RegexOptions.Compiled),
            new(@"\bDateTimeOffset\.UtcNow\b", RegexOptions.Compiled),

            // Unseeded/ambient RNG
            new(@"\bnew\s+Random\s*\(\s*\)", RegexOptions.Compiled), // parameterless
            new(@"\bRandom\.Shared\b", RegexOptions.Compiled),
            new(@"\bUnityEngine\.Random\.", RegexOptions.Compiled),

            // Non-deterministic ids
            new(@"\bGuid\.NewGuid\s*\(", RegexOptions.Compiled),

            // Time sources often used accidentally
            new(@"\bEnvironment\.TickCount\b", RegexOptions.Compiled),
            new(@"\bStopwatch\.StartNew\s*\(", RegexOptions.Compiled),
        };

        [MenuItem("T4E/Validate/Determinism Guards")]
        public static void RunFromMenu()
        {
            try
            {
                ValidateOrThrow();
                EditorUtility.DisplayDialog("Determinism Guards", "No banned APIs found.", "OK");
            }
            catch (Exception ex)
            {
                Debug.LogError(ex.Message);
                EditorUtility.DisplayDialog("Determinism Guards", ex.Message, "OK");
                throw;
            }
        }

        public void OnPreprocessBuild(BuildReport report) => ValidateOrThrow();

        static bool ShouldSkip()
        {
            var args = Environment.GetCommandLineArgs();
            if (args.Any(a => string.Equals(a, "-skipDeterminism", StringComparison.OrdinalIgnoreCase)))
                return true;
#if SKIP_DETERMINISM_GUARDS
            return true;
#else
            return false;
#endif
        }

        static void ValidateOrThrow()
        {
            if (ShouldSkip())
                return;

            string projRoot = Path.GetDirectoryName(Application.dataPath)!.Replace('\\','/');
            string AssetsRoot = Application.dataPath.Replace('\\','/');

            // Collect files from the configured scan roots 
            var candidates = ScanRoots
                .Select(root => Path.Combine(projRoot, root).Replace('\\','/'))
                .Where(Directory.Exists)
                .SelectMany(root => Directory.GetFiles(root, "*.cs", SearchOption.AllDirectories))
                .ToList();

            // Normalize allow-list to absolute normalized paths for quick comparison
            var allowAbs = AllowFiles
                .Select(rel => Path.Combine(projRoot, rel).Replace('\\','/'))
                .ToArray();

            // Always skip validator code itself and anything under Tools.Editor
            bool IsExcluded(string p)
            {
                var np = p.Replace('\\','/');
                if (np.Contains("/Tools.Editor/")) return true;
                if (allowAbs.Any(a => np.EndsWith(a, StringComparison.OrdinalIgnoreCase))) return true;
                return false;
            }

            var offenders = candidates
                .Where(p => !IsExcluded(p))
                .SelectMany(path =>
                {
                    string text = File.ReadAllText(path);
                    return BannedPatterns
                        .Where(rx => rx.IsMatch(text))
                        .Select(rx => (path, pattern: rx.ToString()));
                })
                .ToList();

            if (offenders.Count > 0)
            {
                // Group by file for a cleaner message
                var grouped = offenders
                    .GroupBy(o => o.path.Replace('\\','/'))
                    .Select(g => $"{g.Key}\n    {string.Join("\n    ", g.Select(o => o.pattern).Distinct())}");

                string msg = "Banned APIs detected (use IClock/IRandom/etc. via ports instead):\n" + string.Join("\n", grouped);
                throw new BuildFailedException(msg);
            }
        }
    }
}
#endif
