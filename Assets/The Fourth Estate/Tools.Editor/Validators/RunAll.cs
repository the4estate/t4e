#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace T4E.Tools.Editor.Validators
{
    public static class RunAll
    {
        [MenuItem("T4E/Validators/Run All")]
        public static void RunMenu() => RunAndExit(false);

        public static void RunAndExit() => RunAndExit(true);

        private static void RunAndExit(bool exitOnError)
        {
            try {
                DeterminismGuards.RunFromMenu(); // this throws if violations exist
                AsmdefGuard.Run();
                ContentSchemaValidator.Run();
                Debug.Log("[Validators] All checks passed.");
            } catch {
                if (exitOnError) throw;
                throw;
            }
        }
    }
}
#endif
