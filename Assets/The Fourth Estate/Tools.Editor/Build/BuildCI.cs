#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine; 

namespace T4E.Tools.Editor.Build
{
    public static class BuildCI
    {
        /// <summary>
        /// Run a Windows IL2CPP build from CLI/CI.
        /// CLI example:
        ///   -batchmode -nographics -quit -projectPath "..."
        ///   -executeMethod T4E.Tools.Editor.Build.BuildCI.BuildWindowsIL2CPP
        ///   -customBuildPath Builds/WindowsIL2CPP
        /// </summary>
        public static void BuildWindowsIL2CPP()
        {
            var group = BuildTargetGroup.Standalone;
            var target = BuildTarget.StandaloneWindows64;
            var nbt = NamedBuildTarget.Standalone;

            string arg = GetArg("-customBuildPath");
            string outPath = string.IsNullOrEmpty(arg) ? "Builds/WindowsIL2CPP/T4E.exe" : NormalizeOutputPath(arg);

            // Check if IL2CPP module is installed for this editor
            if (!BuildPipeline.IsBuildTargetSupported(group, target))
            {
                FailAndExit("Windows Standalone (IL2CPP) module is not installed. Add via Unity Hub → Installs → Add Modules.");
                return;
            }

            // Save current settings
            var prevTarget = EditorUserBuildSettings.activeBuildTarget;
            var prevBackend = PlayerSettings.GetScriptingBackend(nbt);
            var prevApi = PlayerSettings.GetApiCompatibilityLevel(nbt);
            var prevDev = EditorUserBuildSettings.development;
            var prevDefines = PlayerSettings.GetScriptingDefineSymbols(nbt);

            try
            {
                PlayerSettings.SetScriptingBackend(nbt, ScriptingImplementation.IL2CPP);
                PlayerSettings.SetApiCompatibilityLevel(nbt, ApiCompatibilityLevel.NET_Unity_4_8);

                EditorUserBuildSettings.SwitchActiveBuildTarget(group, target);
                EditorUserBuildSettings.development     = false;
                EditorUserBuildSettings.connectProfiler = false;

                // Scenes
                var scenes = EditorBuildSettings.scenes.Where(s => s.enabled).Select(s => s.path).ToArray();
                if (scenes.Length == 0)
                {
                    FailAndExit("No scenes are enabled in File → Build Settings.");
                    return;
                }

                var opts = new BuildPlayerOptions
                {
                    scenes           = scenes,
                    target           = target,
                    locationPathName = outPath,
                    options          = BuildOptions.None
                };

                var report = BuildPipeline.BuildPlayer(opts);
                if (report.summary.result != BuildResult.Succeeded)
                {
                    FailAndExit(report);
                    return;
                }

                Console.WriteLine($"✅ Build succeeded → {outPath}");
            }
            finally
            {
                // Restore previous settings
                PlayerSettings.SetScriptingBackend(nbt, prevBackend);
                PlayerSettings.SetApiCompatibilityLevel(nbt, prevApi);
                PlayerSettings.SetScriptingDefineSymbols(nbt, prevDefines);
                EditorUserBuildSettings.development = prevDev;
                if (prevTarget != EditorUserBuildSettings.activeBuildTarget)
                {
                    EditorUserBuildSettings.SwitchActiveBuildTarget(
                        BuildPipeline.GetBuildTargetGroup(prevTarget), prevTarget);
                }
            }
        }

        
        static void FailAndExit(string message)
        {
            throw new Exception($"Build failed: {message}");
        }

        // Extract first error from the BuildReport when available
        static void FailAndExit(BuildReport report)
        {
            string detail = report.steps
                .SelectMany(s => s.messages)
                .Where(m => m.type == LogType.Error)
                .Select(m => m.content)
                .FirstOrDefault() ?? "Unknown error";

            throw new Exception(
                $"Build failed: {report.summary.result} (errors: {report.summary.totalErrors})\nFirst error: {detail}"
            );
        }

        private static string NormalizeOutputPath(string path)
        {
            if (string.IsNullOrEmpty(path)) return "Builds/WindowsIL2CPP/T4E.exe";

            if (Directory.Exists(path) || !path.EndsWith(".exe", StringComparison.OrdinalIgnoreCase))
                return Path.Combine(path, "T4E.exe");

            return path;
        }

        private static string GetArg(string name)
        {
            var args = Environment.GetCommandLineArgs();
            for (int i = 0; i < args.Length - 1; i++)
                if (string.Equals(args[i], name, StringComparison.OrdinalIgnoreCase))
                    return args[i + 1];
            return null;
        }
    }
}
#endif
